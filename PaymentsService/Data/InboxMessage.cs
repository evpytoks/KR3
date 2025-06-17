using System;
namespace PaymentsService.Data;

public class InboxMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Data { get; set; } = string.Empty;
    public Guid MessageId { get; set; }
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessedUtc { get; set; }
    public string Error { get; set; } = string.Empty;
}

