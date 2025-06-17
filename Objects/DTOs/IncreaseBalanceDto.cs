using System;
namespace Objects.DTOs;

public class IncreaseBalanceDto
{
    public Guid UserId { get; set; }
    public decimal Amount { get; set; }
}
