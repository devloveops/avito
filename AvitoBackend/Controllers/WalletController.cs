using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AvitoBackend.Data;
using AvitoBackend.Models.Core;
using Microsoft.AspNetCore.Authorization;

namespace AvitoBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class WalletController : ControllerBase
{
    private readonly AppDbContext _db;

    public WalletController(AppDbContext db)
    {
        _db = db;
    }

    // GET: api/wallet
    [HttpGet]
    public async Task<IActionResult> GetBalance()
    {
        var userId = Guid.Parse(User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")!.Value);
        var user = await _db.Users.FindAsync(userId);
        if (user == null) return NotFound();
        
        return Ok(new { Balance = user.Balance });
    }

    // POST: api/wallet/deposit
    [HttpPost("deposit")]
    public async Task<IActionResult> Deposit([FromBody] DepositRequest request)
    {
        var userId = Guid.Parse(User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")!.Value);
        var user = await _db.Users.FindAsync(userId);
        if (user == null) return NotFound();

        user.Balance += request.Amount;
        await _db.SaveChangesAsync();
        return Ok(new { Balance = user.Balance });
    }

    // POST: api/wallet/withdraw
    [HttpPost("withdraw")]
    public async Task<IActionResult> Withdraw([FromBody] WithdrawRequest request)
    {
        var userId = Guid.Parse(User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")!.Value);
        var user = await _db.Users.FindAsync(userId);
        if (user == null) return NotFound();

        if (user.Balance < request.Amount)
            return BadRequest("Insufficient funds");

        user.Balance -= request.Amount;
        await _db.SaveChangesAsync();
        return Ok(new { Balance = user.Balance });
    }
}

public class DepositRequest
{
    public decimal Amount { get; set; }
}

public class WithdrawRequest
{
    public decimal Amount { get; set; }
}