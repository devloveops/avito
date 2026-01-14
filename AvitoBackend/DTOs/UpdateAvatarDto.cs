using Microsoft.AspNetCore.Http;

namespace AvitoBackend.DTOs;

public class UpdateAvatarDto
{
    public IFormFile Avatar { get; set; } = default!;
}