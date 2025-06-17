using System;
using System.ComponentModel.DataAnnotations;

namespace PaymentsService.Data;

public class Account
{
    public Guid UserId { get; set; }
    public decimal Balance { get; set; }
}
