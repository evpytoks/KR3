using System;
namespace Objects.DTOs;

public class AccountDto
{
    public Guid UserId { get; set; }
    public decimal Balance { get; set; }
}
