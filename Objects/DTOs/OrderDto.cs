using System;
using Objects.Enums;

namespace Objects.DTOs;

public class OrderDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public StatusEnum Status { get; set; }
}
