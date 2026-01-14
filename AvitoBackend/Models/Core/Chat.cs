using AvitoBackend.Models.Core;

namespace AvitoBackend.Models;

public class Chat
{
    public Guid Id { get; set; }
    public Guid AdvertisementId { get; set; }
    public Guid BuyerId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public Advertisement Advertisement { get; set; } = null!;
    public AppUser Buyer { get; set; } = null!;
}