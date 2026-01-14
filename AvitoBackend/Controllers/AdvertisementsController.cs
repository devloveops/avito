using AvitoBackend.Data;
using AvitoBackend.DTOs;
using AvitoBackend.Models;
using AvitoBackend.Models.Core;
using AvitoBackend.Models.NoSQL;
using AvitoBackend.Services;
using AvitoBackend.Services.Storage;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;
using System.Security.Claims;

namespace AvitoBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class AdvertisementsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IFileStorageService _fileStorage;
    private readonly IMongoCollection<AdvertisementDescription> _mongoDescriptions;

    public AdvertisementsController(
        AppDbContext context,
        IFileStorageService fileStorage,
        IMongoCollection<AdvertisementDescription> mongoDescriptions)
    {
        _context = context;
        _fileStorage = fileStorage;
        _mongoDescriptions = mongoDescriptions;
    }

    /// <summary>
    /// Получить все объявления
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<AdvertisementDto>>> GetAdvertisements(
        [FromQuery] string? titleContains = null,
        [FromQuery] decimal? maxPrice = null,
        [FromQuery] Guid? categoryId = null)
    {
        var query = _context.Advertisements
            .Include(a => a.Category) 
            .AsQueryable();

        if (!string.IsNullOrEmpty(titleContains))
            query = query.Where(a => a.Title.Contains(titleContains));

        if (maxPrice.HasValue)
            query = query.Where(a => a.Price <= maxPrice.Value);

        if (categoryId.HasValue)
            query = query.Where(a => a.CategoryId == categoryId.Value);

        var advertisements = await query
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();

        var dtos = new List<AdvertisementDto>();
        foreach (var ad in advertisements)
        {
            string? description = null;
            if (!string.IsNullOrEmpty(ad.MongoDescriptionId))
            {
                var desc = await _mongoDescriptions.Find(d => d.Id == ad.MongoDescriptionId).FirstOrDefaultAsync();
                description = desc?.Content;
            }

            dtos.Add(new AdvertisementDto
            {
                Id = ad.Id,
                Title = ad.Title,
                Description = description ?? string.Empty,
                Price = ad.Price,
                CategoryId = ad.CategoryId,
                CategoryName = ad.Category?.Name,
                ImageUrls = ad.ImageUrls,
                CreatedAt = ad.CreatedAt,
                AuthorEmail = (await _context.Users.FindAsync(ad.UserId))?.Email
            });
        }

        return Ok(dtos);
    }

    /// <summary>
    /// Получить объявление по ID
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AdvertisementDto>> GetAdvertisement(Guid id)
    {
        var advertisement = await _context.Advertisements
            .Include(a => a.Category)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (advertisement == null)
            return NotFound($"Объявление с ID {id} не найдено");

        string? description = null;
        if (!string.IsNullOrEmpty(advertisement.MongoDescriptionId))
        {
            var desc = await _mongoDescriptions.Find(d => d.Id == advertisement.MongoDescriptionId).FirstOrDefaultAsync();
            description = desc?.Content;
        }

        var dto = new AdvertisementDto
        {
            Id = advertisement.Id,
            Title = advertisement.Title,
            Description = description ?? string.Empty,
            Price = advertisement.Price,
            CategoryId = advertisement.CategoryId,
            CategoryName = advertisement.Category?.Name,
            ImageUrls = advertisement.ImageUrls,
            CreatedAt = advertisement.CreatedAt,
            AuthorEmail = (await _context.Users.FindAsync(advertisement.UserId))?.Email
        };

        return Ok(dto);
    }

    /// <summary>
    /// Создать новое объявление
    /// </summary>
 [HttpPost]
public async Task<ActionResult<AdvertisementDto>> CreateAdvertisement( [FromForm] CreateAdvertisementDto dto,     UserManager<AppUser> userManager){
    if (!User.Identity.IsAuthenticated)
        return Unauthorized();

    var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

    var user = await userManager.FindByIdAsync(userId.ToString());
    if (user == null)
        return NotFound("Пользователь не найден");

    if (string.IsNullOrWhiteSpace(dto.Title))
        return BadRequest("Заголовок обязателен");

    if (dto.Price <= 0)
        return BadRequest("Цена должна быть больше 0");

    if (dto.CategoryId.HasValue)
    {
        var categoryExists = await _context.Set<Category>()
            .AnyAsync(c => c.Id == dto.CategoryId.Value);
        
        if (!categoryExists)
            return BadRequest("Указанная категория не существует");
    }

    string? mongoDescriptionId = null;
    if (!string.IsNullOrEmpty(dto.Description))
    {
        var descriptionDoc = new AdvertisementDescription
        {
            Content = dto.Description
        };
        await _mongoDescriptions.InsertOneAsync(descriptionDoc);
        mongoDescriptionId = descriptionDoc.Id;
    }

    var imageUrls = new List<string>();
    if (dto.Images?.Count > 0)
    {
        foreach (var image in dto.Images)
        {
            if (image.Length > 0)
            {
                var fileName = $"{Guid.NewGuid()}_{image.FileName}";
                using var stream = image.OpenReadStream();
                var url = await _fileStorage.UploadFileAsync("advertisements", fileName, stream);
                imageUrls.Add(url);
            }
        }
    }

    var advertisement = new Advertisement
    {
        Title = dto.Title.Trim(),
        Price = dto.Price,
        UserId = userId,    
        CategoryId = dto.CategoryId,
        MongoDescriptionId = mongoDescriptionId,
        ImageUrls = imageUrls,
        CreatedAt = DateTime.UtcNow
    };

    _context.Advertisements.Add(advertisement);
    await _context.SaveChangesAsync();

    return CreatedAtAction(
        nameof(GetAdvertisement),
        new { id = advertisement.Id },
        new AdvertisementDto
        {
            Id = advertisement.Id,
            Title = advertisement.Title,
            Description = dto.Description ?? string.Empty,
            Price = advertisement.Price,
            CategoryId = advertisement.CategoryId,
            CategoryName = advertisement.Category?.Name,
            ImageUrls = advertisement.ImageUrls,
            CreatedAt = advertisement.CreatedAt,
            AuthorEmail = user.Email 
        });
}

    /// <summary>
    /// Удалить объявление
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteAdvertisement(Guid id)
    {
        if (!User.Identity.IsAuthenticated)
            return Unauthorized();

        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var advertisement = await _context.Advertisements.FindAsync(id);

        if (advertisement == null)
            return NotFound($"Объявление с ID {id} не найдено");

        if (advertisement.UserId != userId)
            return Forbid();

        _context.Advertisements.Remove(advertisement);
        await _context.SaveChangesAsync();

        return NoContent();
    }
    /// <summary>
/// Обновить объявление (частичное обновление)
/// </summary>
[HttpPut("{id:guid}")]
public async Task<ActionResult<AdvertisementDto>> UpdateAdvertisement(
    Guid id,
    [FromForm] UpdateAdvertisementDto dto)
{
    if (!User.Identity.IsAuthenticated)
        return Unauthorized();

    var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

    var advertisement = await _context.Advertisements
        .Include(a => a.Category)
        .FirstOrDefaultAsync(a => a.Id == id);

    if (advertisement == null)
        return NotFound($"Объявление с ID {id} не найдено");

    if (advertisement.UserId != userId)
        return Forbid();

    if (!string.IsNullOrWhiteSpace(dto.Title))
        advertisement.Title = dto.Title.Trim();

    if (dto.Price.HasValue && dto.Price > 0)
        advertisement.Price = dto.Price.Value;


    if (dto.CategoryId.HasValue)
    {
        if (dto.CategoryId.Value == Guid.Empty)
        {
            advertisement.CategoryId = null;
        }
        else
        {
            var categoryExists = await _context.Set<Category>()
                .AnyAsync(c => c.Id == dto.CategoryId.Value);
            
            if (!categoryExists)
                return BadRequest("Указанная категория не существует");
            
            advertisement.CategoryId = dto.CategoryId.Value;
        }
    }


    if (dto.Description != null) 
    {
        if (string.IsNullOrEmpty(dto.Description))
        {

            if (!string.IsNullOrEmpty(advertisement.MongoDescriptionId))
            {
                await _mongoDescriptions.DeleteOneAsync(d => d.Id == advertisement.MongoDescriptionId);
                advertisement.MongoDescriptionId = null;
            }
        }
        else
        {

            if (string.IsNullOrEmpty(advertisement.MongoDescriptionId))
            {

                var newDesc = new AdvertisementDescription { Content = dto.Description };
                await _mongoDescriptions.InsertOneAsync(newDesc);
                advertisement.MongoDescriptionId = newDesc.Id;
            }
            else
            {

                await _mongoDescriptions.ReplaceOneAsync(
                    d => d.Id == advertisement.MongoDescriptionId,
                    new AdvertisementDescription { Id = advertisement.MongoDescriptionId, Content = dto.Description });
            }
        }
    }


    if (dto.NewImages != null && dto.NewImages.Count > 0)
    {
        var newImageUrls = new List<string>();
        foreach (var image in dto.NewImages)
        {
            if (image.Length > 0)
            {
                var fileName = $"{Guid.NewGuid()}_{image.FileName}";
                using var stream = image.OpenReadStream();
                var url = await _fileStorage.UploadFileAsync("advertisements", fileName, stream);
                newImageUrls.Add(url);
            }
        }
        advertisement.ImageUrls.AddRange(newImageUrls);
    }


        if (dto.ImagesToDelete == null || dto.ImagesToDelete.Count <= 0)
        {
        }
        else
        {

            var validUrlsToDelete = dto.ImagesToDelete
                .Where(url => advertisement.ImageUrls.Contains(url))
                .ToList();

            foreach (var url in validUrlsToDelete)
            {
                advertisement.ImageUrls.Remove(url);
            }
        }

        advertisement.UpdatedAt = DateTime.UtcNow;
    await _context.SaveChangesAsync();

    string? description = null;
    if (!string.IsNullOrEmpty(advertisement.MongoDescriptionId))
    {
        var desc = await _mongoDescriptions.Find(d => d.Id == advertisement.MongoDescriptionId).FirstOrDefaultAsync();
        description = desc?.Content;
    }

    var resultDto = new AdvertisementDto
    {
        Id = advertisement.Id,
        Title = advertisement.Title,
        Description = description ?? string.Empty,
        Price = advertisement.Price,
        CategoryId = advertisement.CategoryId,
        CategoryName = advertisement.Category?.Name,
        ImageUrls = advertisement.ImageUrls,
        CreatedAt = advertisement.CreatedAt,
        UpdatedAt = advertisement.UpdatedAt,
        AuthorEmail = User.FindFirst(ClaimTypes.Email)?.Value
    };

    return Ok(resultDto);
}
}