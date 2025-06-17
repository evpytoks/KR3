using System;
namespace Objects.DTOs;

public class CreateOrderDto
{
    public Guid UserId { get; set; }
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
}
