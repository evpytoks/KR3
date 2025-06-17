using System;
namespace Objects;

public class PaymentResponse
{
    public Guid OrderId { get; set; }
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public Guid MessageId { get; set; } = Guid.NewGuid();
    public Guid OriginalMessageId { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
