using AvitoBackend.Data;
using AvitoBackend.DTOs;
using AvitoBackend.Models;
using Microsoft.EntityFrameworkCore;

namespace AvitoBackend.Services.Payment;

public class PromotionService : IPromotionService
{
    private readonly AppDbContext _context;

    public PromotionService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<PromotionTransaction> CreateTransactionAsync(Guid advertisementId, Guid userId, decimal amount, string paymentSystem)
    {
        var transaction = new PromotionTransaction
        {
            AdvertisementId = advertisementId,
            UserId = userId,
            Amount = amount,
            PaymentSystem = paymentSystem,
            Status = "Pending"
        };

        _context.PromotionTransactions.Add(transaction);
        await _context.SaveChangesAsync();
        return transaction;
    }

    public async Task<PromotionTransaction> ConfirmTransactionAsync(ConfirmPaymentDto dto)
    {
        var transaction = await _context.PromotionTransactions
            .FirstOrDefaultAsync(t => t.Id == dto.TransactionId);

        if (transaction == null)
            throw new InvalidOperationException("Transaction not found");

        if (transaction.Status != "Pending")
            throw new InvalidOperationException("Transaction already processed");

        if (dto.Status != "Completed" && dto.Status != "Failed")
            throw new ArgumentException("Invalid status. Use 'Completed' or 'Failed'");

        transaction.Status = dto.Status;
        transaction.CompletedAt = DateTime.UtcNow;

        if (dto.Status == "Completed")
        {
            var advertisement = await _context.Advertisements.FindAsync(transaction.AdvertisementId);
            if (advertisement != null)
            {
                advertisement.IsPromoted = true;
                advertisement.PromotedUntil = DateTime.UtcNow.AddDays(7); 
            }
        }

        await _context.SaveChangesAsync();
        return transaction;
    }

    public async Task<List<PromotionTransaction>> GetUserTransactionsAsync(Guid userId)
    {
        return await _context.PromotionTransactions
            .Where(t => t.UserId == userId)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();
    }
}