using System;
using System.ComponentModel.DataAnnotations;
using Objects.Enums;

namespace OrderService.Data;

public class Order
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public StatusEnum Status { get; set; }
}
