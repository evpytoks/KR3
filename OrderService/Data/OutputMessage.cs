using System;
using System.ComponentModel.DataAnnotations;

namespace OrderService.Data;

public class OutboxMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Data { get; set; } = string.Empty;
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessedUtc { get; set; }
    public string Error { get; set; } = string.Empty;
}
