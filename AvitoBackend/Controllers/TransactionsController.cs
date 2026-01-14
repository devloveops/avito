using AvitoBackend.DTOs;
using AvitoBackend.Services.Payment;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AvitoBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PaymentsController : ControllerBase
{
    private readonly IPromotionService _promotionService;

    public PaymentsController(IPromotionService promotionService)
    {
        _promotionService = promotionService;
    }

    /// <summary>
    /// Подтвердить транзакцию (для webhook'ов или админки)
    /// </summary>
    [HttpPost("confirm")]
    [Authorize(Roles = "Admin")] // Только администратор может подтверждать
    public async Task<ActionResult> ConfirmPayment([FromBody] ConfirmPaymentDto dto)
    {
        try
        {
            var transaction = await _promotionService.ConfirmTransactionAsync(dto);
            return Ok(new 
            { 
                transaction.Id, 
                transaction.Status, 
                transaction.CompletedAt 
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Получить историю транзакций пользователя
    /// </summary>
    [HttpGet("history")]
    public async Task<ActionResult> GetPaymentHistory()
    {
        var userId = Guid.Parse(User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")!.Value);
        var transactions = await _promotionService.GetUserTransactionsAsync(userId);
        
        return Ok(transactions.Select(t => new
        {
            t.Id,
            t.AdvertisementId,
            t.Amount,
            t.PaymentSystem,
            t.Status,
            t.CreatedAt,
            t.CompletedAt
        }));
    }

    /// <summary>
    /// Создать транзакцию продвижения (пример)
    /// </summary>
    [HttpPost("promote/{advertisementId:guid}")]
    public async Task<ActionResult> CreatePromotionTransaction(Guid advertisementId)
    {
        var userId = Guid.Parse(User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")!.Value);
        var amount = 100m; // Фиксированная цена продвижения
        
        var transaction = await _promotionService.CreateTransactionAsync(
            advertisementId, userId, amount, "Manual");
            
        return Ok(new { transaction.Id, transaction.Amount });
    }
}