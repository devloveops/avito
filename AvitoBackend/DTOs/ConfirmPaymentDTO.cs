namespace AvitoBackend.DTOs;

public class ConfirmPaymentDto
{
    public Guid TransactionId { get; set; }
    public string Status { get; set; } = "Completed"; 
}