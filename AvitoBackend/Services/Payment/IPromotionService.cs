using AvitoBackend.DTOs;
using AvitoBackend.Models;

namespace AvitoBackend.Services.Payment;

public interface IPromotionService
{
    Task<PromotionTransaction> CreateTransactionAsync(Guid advertisementId, Guid userId, decimal amount, string paymentSystem);
    Task<PromotionTransaction> ConfirmTransactionAsync(ConfirmPaymentDto dto);
    Task<List<PromotionTransaction>> GetUserTransactionsAsync(Guid userId);
}