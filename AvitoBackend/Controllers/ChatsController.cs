// Controllers/ChatsController.cs
using AvitoBackend.Data;
using AvitoBackend.Models;
using AvitoBackend.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using AvitoBackend.Services.Storage;

namespace AvitoBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChatsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IFileStorageService _fileStorage;

    public ChatsController(AppDbContext context, IFileStorageService fileStorage)
    {
        _context = context;
        _fileStorage = fileStorage;
    }

    /// <summary>
    /// Начать чат по объявлению
    /// </summary>
    [HttpPost("by-advertisement/{advertisementId:guid}")]
    public async Task<ActionResult> StartChat(Guid advertisementId)
    {
        if (!User.Identity.IsAuthenticated)
            return Unauthorized();

        var buyerId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        var advertisement = await _context.Advertisements
            .FirstOrDefaultAsync(a => a.Id == advertisementId);
        
        if (advertisement == null)
            return NotFound("Объявление не найдено");

        if (advertisement.UserId == buyerId)
            return BadRequest("Нельзя начать чат с своим объявлением");

        var existingChat = await _context.Chats
            .FirstOrDefaultAsync(c => c.AdvertisementId == advertisementId && 
                                    c.BuyerId == buyerId);

        if (existingChat != null)
            return Ok(new { ChatId = existingChat.Id });

        var chat = new Chat
        {
            AdvertisementId = advertisementId,
            BuyerId = buyerId
        };

        _context.Chats.Add(chat);
        await _context.SaveChangesAsync();

        return Ok(new { ChatId = chat.Id });
    }

    /// <summary>
    /// Отправить сообщение в чат
    /// </summary>
    [HttpPost("{chatId:guid}/messages")]
    public async Task<ActionResult> SendMessage(Guid chatId, [FromForm] SendMessageDto dto)
    {
        if (!User.Identity.IsAuthenticated)
            return Unauthorized();

        var senderId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var chat = await _context.Chats
            .Include(c => c.Advertisement)
            .FirstOrDefaultAsync(c => c.Id == chatId);

        if (chat == null)
            return NotFound("Чат не найден");

        bool isParticipant = 
            senderId == chat.BuyerId || 
            senderId == chat.Advertisement.UserId;

        if (!isParticipant)
            return Forbid("Вы не участник этого чата");

        if (string.IsNullOrWhiteSpace(dto.Content) && dto.Media == null)
            return BadRequest("Сообщение должно содержать текст или медиа");

        string? mediaUrl = null;
        string? mediaType = null;

        if (dto.Media != null && dto.Media.Length > 0)
        {
            var allowedTypes = new[]
            {
                "image/jpeg", "image/png", "image/gif",
                "video/mp4", "video/quicktime"
            };
            
            if (!allowedTypes.Contains(dto.Media.ContentType))
                return BadRequest("Неподдерживаемый тип файла");

            if (dto.Media.Length > 50 * 1024 * 1024)
                return BadRequest("Файл слишком большой (макс. 50 МБ)");

            var fileName = $"chats/{chatId}/{Guid.NewGuid()}_{dto.Media.FileName}";
            using var stream = dto.Media.OpenReadStream();
            mediaUrl = await _fileStorage.UploadFileAsync("chats", fileName, stream);
            mediaType = dto.Media.ContentType;
        }

        var message = new Message
        {
            ChatId = chatId,
            SenderId = senderId,
            Content = dto.Content?.Trim(),
            MediaUrl = mediaUrl,
            MediaType = mediaType
        };

        _context.Messages.Add(message);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            message.Id,
            message.ChatId,
            message.SenderId,
            message.Content,
            message.MediaUrl,
            message.MediaType,
            message.CreatedAt
        });
    }

    /// <summary>
    /// Получить список чатов пользователя
    /// </summary>
    [HttpGet]
    public async Task<ActionResult> GetUserChats()
    {
        if (!User.Identity.IsAuthenticated)
            return Unauthorized();

        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);


        var chats = await _context.Chats
            .Include(c => c.Advertisement)
            .Include(c => c.Buyer)
            .Where(c => c.Advertisement.UserId == userId || c.BuyerId == userId)
            .Select(c => new
            {
                c.Id,
                Advertisement = new
                {
                    c.Advertisement.Id,
                    c.Advertisement.Title,
                    c.Advertisement.Price,
                    c.Advertisement.ImageUrls
                },
                OtherUser = c.Advertisement.UserId == userId 
                    ? new { c.Buyer.Id, c.Buyer.Email, c.Buyer.AvatarUrl } 
                    : new { 
                          Id = c.Advertisement.UserId, 
                          Email = _context.Users.Where(u => u.Id == c.Advertisement.UserId).Select(u => u.Email).FirstOrDefault(),
                          AvatarUrl = _context.Users.Where(u => u.Id == c.Advertisement.UserId).Select(u => u.AvatarUrl).FirstOrDefault()
                      },
                LastMessage = _context.Messages
                    .Where(m => m.ChatId == c.Id)
                    .OrderByDescending(m => m.CreatedAt)
                    .Select(m => new { m.Content, m.MediaUrl, m.CreatedAt })
                    .FirstOrDefault(),
                CreatedAt = c.CreatedAt
            })
            .OrderBy(c => c.LastMessage != null ? c.LastMessage.CreatedAt : c.CreatedAt)
            .ToListAsync();

        return Ok(chats);
    }

    /// <summary>
    /// Получить сообщения чата
    /// </summary>
    [HttpGet("{chatId:guid}/messages")]
    public async Task<ActionResult> GetMessages(Guid chatId)
    {
        if (!User.Identity.IsAuthenticated)
            return Unauthorized();

        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        var chatExists = await _context.Chats
            .AnyAsync(c => c.Id == chatId && 
                          (c.Advertisement.UserId == userId || c.BuyerId == userId));

        if (!chatExists)
            return Forbid();

        var messages = await _context.Messages
            .Where(m => m.ChatId == chatId)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync();

        return Ok(messages);
    }
}