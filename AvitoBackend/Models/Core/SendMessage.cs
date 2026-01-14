using Microsoft.AspNetCore.Http;

namespace AvitoBackend.DTOs;

public class SendMessageDto
{
    public Guid ChatId { get; set; }
    
    public string? Content { get; set; }
    
    public IFormFile? Media { get; set; }
}