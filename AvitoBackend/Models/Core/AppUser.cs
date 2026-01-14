using Microsoft.AspNetCore.Identity;

namespace AvitoBackend.Models.Core;

public class AppUser : IdentityUser<Guid>
{
    public decimal Balance { get; set; } = 0.0m;
    public string? AvatarUrl { get; set; }
}