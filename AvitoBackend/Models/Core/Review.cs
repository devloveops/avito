using AvitoBackend.Models.Core;

namespace AvitoBackend.Models;

public class Review
{
    public Guid Id { get; set; }
    public Guid AdvertisementId { get; set; } 
    public Guid AuthorId { get; set; }       
    public Guid TargetUserId { get; set; }
    public int Rating { get; set; } 
    public string Comment { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    

    public Advertisement Advertisement { get; set; } = null!;
    public AppUser Author { get; set; } = null!;
    public AppUser TargetUser { get; set; } = null!;
}