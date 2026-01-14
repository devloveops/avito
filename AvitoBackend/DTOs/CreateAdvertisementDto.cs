namespace AvitoBackend.DTOs;

public class CreateAdvertisementDto
{
    public string Title { get; set; } = default!;
    public string Description { get; set; } = default!;
    public decimal Price { get; set; }
    public Guid? CategoryId { get; set; }
    public IFormFileCollection? Images { get; set; } 
}