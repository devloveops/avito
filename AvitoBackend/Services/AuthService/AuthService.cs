using AvitoBackend.Data;
using AvitoBackend.DTOs;
using AvitoBackend.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using AvitoBackend.Models.Core;
using AvitoBackend.Services.Cache;

namespace AvitoBackend.Services;

public interface IAuthService
{
    Task<(string JwtToken, string RefreshToken)> LoginAsync(string email, string password);
    Task<(string JwtToken, string RefreshToken)> LoginWithGoogleAsync(string email);
    Task<(string JwtToken, string RefreshToken)> RegisterAsync(RegisterRequest dto);
    Task<(string JwtToken, string RefreshToken)> RefreshTokenAsync(string refreshToken);
    Task<bool> LogoutAsync(string refreshToken);
}
public class AuthService : IAuthService
{
    private readonly UserManager<AppUser> _userManager;
    private readonly IConfiguration _config;
    private readonly ICacheService _cache;

    public AuthService(
        UserManager<AppUser> userManager,
        IConfiguration config,
        ICacheService cache)
    {
        _userManager = userManager;
        _config = config;
        _cache = cache;
    }
    
    public async Task<(string JwtToken, string RefreshToken)> RegisterAsync(RegisterRequest dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Password))
            throw new ArgumentException("Email and password are required");

        var existingUser = await _userManager.FindByEmailAsync(dto.Email);
        if (existingUser != null)
            throw new InvalidOperationException("User with this email already exists");

        var user = new AppUser
        {
            Email = dto.Email,
            UserName = dto.Email,
            EmailConfirmed = true 
        };

        var result = await _userManager.CreateAsync(user, dto.Password);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Registration failed: {errors}");
        }

        return await GenerateTokensAsync(user);
    }

public async Task<(string JwtToken, string RefreshToken)> LoginWithGoogleAsync(string email)
{
    var user = await _userManager.FindByEmailAsync(email);
    if (user == null)
        throw new InvalidOperationException("User not found");

    return await GenerateTokensAsync(user);
}


public async Task<(string JwtToken, string RefreshToken)> LoginAsync(string email, string password)
{
    var user = await _userManager.FindByEmailAsync(email);
    if (user == null || !await _userManager.CheckPasswordAsync(user, password))
        throw new InvalidOperationException("Invalid email or password");

    return await GenerateTokensAsync(user);
}
    public async Task<(string JwtToken, string RefreshToken)> RefreshTokenAsync(string refreshToken)
    {
        var userIdStr = await _cache.GetAsync($"refresh_token:{refreshToken}");
        if (string.IsNullOrEmpty(userIdStr))
            throw new InvalidOperationException("Invalid refresh token");

        var user = await _userManager.FindByIdAsync(userIdStr);
        if (user == null)
            throw new InvalidOperationException("User not found");

        return await GenerateTokensAsync(user);
    }

    public async Task<bool> LogoutAsync(string refreshToken)
    {
        await _cache.RemoveAsync($"refresh_token:{refreshToken}");
        return true;
    }

    private async Task<(string JwtToken, string RefreshToken)> GenerateTokensAsync(AppUser user)
    {
        var jwtToken = GenerateJwtToken(user);
        var refreshToken = Guid.NewGuid().ToString();
        await _cache.SetAsync($"refresh_token:{refreshToken}", user.Id.ToString(), TimeSpan.FromDays(30));

        return (jwtToken, refreshToken);
    }

    private string GenerateJwtToken(AppUser user)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email!),
            new Claim("auth_method", "password"),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(2),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}