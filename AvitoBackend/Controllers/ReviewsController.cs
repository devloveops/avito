// Controllers/ReviewsController.cs
using System.Security.Claims;
using AvitoBackend.Data;
using AvitoBackend.Models;
using AvitoBackend.Models.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AvitoBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReviewsController : ControllerBase
{
    private readonly AppDbContext _context;

    public ReviewsController(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Оставить отзыв на объявление
    /// </summary>
    [HttpPost]
    public async Task<ActionResult> CreateReview([FromBody] CreateReviewDto dto)
    {
        if (!User.Identity.IsAuthenticated)
            return Unauthorized();

        var authorId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        var advertisement = await _context.Advertisements
            .FirstOrDefaultAsync(a => a.Id == dto.AdvertisementId);
        
        if (advertisement == null)
            return NotFound("Объявление не найдено");

        if (advertisement.UserId == authorId)
            return BadRequest("Нельзя оставить отзыв на своё объявление");
        var chatExists = await _context.Chats
            .AnyAsync(c => c.AdvertisementId == dto.AdvertisementId && c.BuyerId == authorId);
        
        if (!chatExists)
            return BadRequest("Можно оставить отзыв только после общения в чате");

        var existingReview = await _context.Reviews
            .AnyAsync(r => r.AdvertisementId == dto.AdvertisementId && r.AuthorId == authorId);
        
        if (existingReview)
            return BadRequest("Отзыв уже оставлен");


        if (dto.Rating < 1 || dto.Rating > 5)
            return BadRequest("Рейтинг должен быть от 1 до 5");

        var review = new Review
        {
            AdvertisementId = dto.AdvertisementId,
            AuthorId = authorId,
            TargetUserId = advertisement.UserId, 
            Rating = dto.Rating,
            Comment = dto.Comment?.Trim() ?? string.Empty
        };

        _context.Reviews.Add(review);
        await _context.SaveChangesAsync();

        return Ok(new { review.Id, review.Rating, review.Comment });
    }

    /// <summary>
    /// Получить отзывы пользователя
    /// </summary>
    [HttpGet("user/{userId:guid}")]
    public async Task<ActionResult> GetUserReviews(Guid userId)
    {
        var reviews = await _context.Reviews
            .Where(r => r.TargetUserId == userId)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new
            {
                r.Id,
                r.Rating,
                r.Comment,
                AuthorEmail = r.Author.Email,
                AdvertisementTitle = r.Advertisement.Title,
                r.CreatedAt
            })
            .ToListAsync();

        return Ok(reviews);
    }

    /// <summary>
    /// Получить средний рейтинг пользователя
    /// </summary>
    [HttpGet("user/{userId:guid}/rating")]
    public async Task<ActionResult> GetUserRating(Guid userId)
    {
        var rating = await _context.Reviews
            .Where(r => r.TargetUserId == userId)
            .AverageAsync(r => (double?)r.Rating) ?? 0;

        var count = await _context.Reviews
            .CountAsync(r => r.TargetUserId == userId);

        return Ok(new { AverageRating = Math.Round(rating, 1), ReviewCount = count });
    }
}

public class CreateReviewDto
{
    public Guid AdvertisementId { get; set; }
    public int Rating { get; set; }
    public string? Comment { get; set; }
}