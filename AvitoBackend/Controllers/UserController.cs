using System.Security.Claims;
using AvitoBackend.Data;
using AvitoBackend.DTOs;
using AvitoBackend.Models;
using AvitoBackend.Services;
using AvitoBackend.Services.Storage;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
namespace AvitoBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class UsersController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IFileStorageService _fileStorage;

    public UsersController(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Обновить аватарку пользователя
    /// </summary>
[HttpPost("avatar")]
public async Task<ActionResult> UpdateAvatar([FromForm] UpdateAvatarDto dto)
{

    var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
    if (userIdClaim == null) return Unauthorized();

    var userId = Guid.Parse(userIdClaim.Value);

    if (dto == null)
        return BadRequest("Некорректные данные запроса");

    if (dto.Avatar == null || dto.Avatar.Length == 0)
        return BadRequest("Файл аватарки обязателен");


    if (_fileStorage == null)
        throw new InvalidOperationException("FileStorage service not initialized");


    var user = await _context.Users.FindAsync(userId);
    if (user == null)
        return NotFound("Пользователь не найден");

    try
    {

        var fileName = $"avatars/{userId}_{Guid.NewGuid()}_{dto.Avatar.FileName}";
        using var stream = dto.Avatar.OpenReadStream();
        var url = await _fileStorage.UploadFileAsync("avatars", fileName, stream);
        
        user.AvatarUrl = url;
        await _context.SaveChangesAsync();

        return Ok(new { user.AvatarUrl });
    }
    catch (Exception ex)
    {

        return StatusCode(500, $"Ошибка загрузки аватарки: {ex.Message}");
    }
}

    /// <summary>
    /// Получить профиль пользователя
    /// </summary>
    [HttpGet("profile-one")]
    public async Task<ActionResult> GetProfile()
    {
        if (!User.Identity.IsAuthenticated)
            return Unauthorized();

        var userId = Guid.Parse(User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")!.Value);
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
            return NotFound();

        return Ok(new
        {
            user.Id,
            user.Email,
            user.AvatarUrl
        });
    }

    /// <summary>
    /// Получить список всех пользователей (только ID и Email)
    /// </summary>
    [HttpGet]
    public async Task<ActionResult> GetUsers()
    {
        if (!User.Identity.IsAuthenticated)
            return Unauthorized();

        var users = await _context.Users
            .Select(u => new
            {
                u.Id,
                u.Email,
                u.AvatarUrl
            })
            .ToListAsync();

        return Ok(users);
    }

    /// <summary>
    /// Получить профиль пользователя по ID
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult> GetUserProfile(Guid id)
    {
        if (!User.Identity.IsAuthenticated)
            return Unauthorized();


        var user = await _context.Users
            .Select(u => new
            {
                u.Id,
                u.Email,
                u.AvatarUrl,
            })
            .FirstOrDefaultAsync(u => u.Id == id);

        if (user == null)
            return NotFound($"Пользователь с ID {id} не найден");

        return Ok(user);
    }

    /// <summary>
    /// Получить свой профиль
    /// </summary>
    [HttpGet("profile")]
    public async Task<ActionResult> GetMyProfile()
    {
        if (!User.Identity.IsAuthenticated)
            return Unauthorized();

        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var user = await _context.Users
            .Select(u => new
            {
                u.Id,
                u.Email,
                u.AvatarUrl
            })
            .FirstOrDefaultAsync(u => u.Id == userId);

        return Ok(user);
    }
}