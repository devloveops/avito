using AvitoBackend.Models.Core;

namespace AvitoBackend.Models;

public class PromotionTransaction
{
    public Guid Id { get; set; }
    public Guid AdvertisementId { get; set; }
    public Guid UserId { get; set; }
    public decimal Amount { get; set; }
    public string PaymentSystem { get; set; } = string.Empty;
    
    public string Status { get; set; } = "Pending"; 
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    
    public Advertisement Advertisement { get; set; } = null!;
    public AppUser User { get; set; } = null!;
}