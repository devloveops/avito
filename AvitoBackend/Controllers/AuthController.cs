using AvitoBackend.DTOs;
using AvitoBackend.Services;
using Microsoft.AspNetCore.Mvc;

namespace AvitoBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    /// <summary>
    /// Регистрация нового пользователя
    /// </summary>
    [HttpPost("register")]
    public async Task<ActionResult> Register([FromBody] RegisterRequest dto)
    {
        try
        {
            var (jwtToken, refreshToken) = await _authService.RegisterAsync(dto);
            return Ok(new { token = jwtToken, refreshToken });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Вход по email/password
    /// </summary>
    [HttpPost("login")]
    public async Task<ActionResult> Login([FromBody] LoginRequest dto)
    {
        try
        {
            var (jwtToken, refreshToken) = await _authService.LoginAsync(dto.Email, dto.Password);
            return Ok(new { token = jwtToken, refreshToken });
        }
        catch (InvalidOperationException ex)
        {
            return Unauthorized(ex.Message);
        }
    }

    /// <summary>
    /// Обновление токена
    /// </summary>
    [HttpPost("refresh")]
    public async Task<ActionResult> Refresh([FromHeader] string refreshToken)
    {
        try
        {
            var (jwtToken, newRefreshToken) = await _authService.RefreshTokenAsync(refreshToken);
            return Ok(new { token = jwtToken, refreshToken = newRefreshToken });
        }
        catch (InvalidOperationException ex)
        {
            return Unauthorized(ex.Message);
        }
    }

    /// <summary>
    /// Выход
    /// </summary>
    [HttpPost("logout")]
    public async Task<ActionResult> Logout([FromHeader] string refreshToken)
    {
        await _authService.LogoutAsync(refreshToken);
        return Ok();
    }
}