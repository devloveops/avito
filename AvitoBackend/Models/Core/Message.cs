using AvitoBackend.Models;
using AvitoBackend.Models.Core;

public class Message
{
    public Guid Id { get; set; } 
    public Guid ChatId { get; set; }
    public Guid SenderId { get; set; }
    public string? Content { get; set; }
    public string? MediaUrl { get; set; }
    public string? MediaType { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public Chat Chat { get; set; } = null!;
    public AppUser Sender { get; set; } = null!;
}