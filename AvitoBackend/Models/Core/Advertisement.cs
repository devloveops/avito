namespace AvitoBackend.Models.Core;

public class Advertisement
{
    public Guid Id { get; set; }
    public string Title { get; set; } = default!;
    public string? Description { get; set; } 
    public decimal Price { get; set; }
    public Guid UserId { get; set; }
    public AppUser User { get; set; } = default!;
    
    public Guid? CategoryId { get; set; }
    public Category? Category { get; set; } = default!;
    
    public string? MongoDescriptionId { get; set; }
    public List<string> ImageUrls { get; set; } = [];
    
    public bool IsPromoted { get; set; }
    public DateTime PromotedUntil { get; set; }
    
    public List<Message> Messages { get; set; } = [];
    public List<Review> Reviews { get; set; } = [];
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow; 
}