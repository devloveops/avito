using Microsoft.AspNetCore.Http;

namespace AvitoBackend.DTOs;

public class UpdateAdvertisementDto
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public decimal? Price { get; set; }
    public Guid? CategoryId { get; set; } 
    public List<IFormFile> NewImages { get; set; } = [];
    public List<string> ImagesToDelete { get; set; } = []; 
}