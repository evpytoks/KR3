using System;
namespace PaymentsService.Data;

public class Payment
{
    public Guid Id { get; set; }
    public Guid AccountId { get; set; }
    public Guid OrderId { get; set; }
    public decimal Amount { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
