using System;
using Microsoft.EntityFrameworkCore;
using System.Security.Principal;
using PaymentsService.Data;
using Objects.DTOs;
using Objects;

namespace PaymentsService.Services;

public class AccountsService
{
    private readonly PaymentsDbContext Context_;

    public AccountsService(PaymentsDbContext context)
	{
        Context_ = context;
    }


    public async Task<AccountDto> CreateAccountAsync(CreateAccountDto request)
    {

        if (await Context_.Accounts.AnyAsync(a => a.UserId == request.UserId))
        {
            throw new InvalidOperationException($"Error: can't create second account for user {request.UserId}.");
        }

        var account = new Account
        {
            UserId = request.UserId,
            Balance = 0
        };
        await Context_.Accounts.AddAsync(account);
        await Context_.SaveChangesAsync();

        return new AccountDto
        {
            UserId = account.UserId,
            Balance = account.Balance
        };
    }


    public async Task<AccountDto> GetAccountAsync(Guid userId)
    {
        var account = await Context_.Accounts.FirstOrDefaultAsync(e => e.UserId == userId);

        if (account == null)
        {
            throw new KeyNotFoundException($"Error: can't found account for user {userId}");
        }

        return new AccountDto
        {
            UserId = account.UserId,
            Balance = account.Balance
        };
    }


    public async Task<AccountDto> IncreaseBalanceAsync(IncreaseBalanceDto request)
    {
        if (request.Amount <= 0)
        {
            throw new ArgumentException("Error: price can't be negative.");
        }

        try
        {
            var account = await Context_.Accounts.FirstOrDefaultAsync(e => e.UserId == request.UserId);
            if (account == null)
            {
                throw new KeyNotFoundException($"Error: can't find user {request.UserId} account.");
            }

            var payment = new Payment
            {
                Id = Guid.NewGuid(),
                AccountId = account.UserId,
                Amount = request.Amount,
                CreatedAt = DateTime.UtcNow
            };

            await Context_.Payments.AddAsync(payment);

            account.Balance += request.Amount;

            await Context_.SaveChangesAsync();
            return new AccountDto
            {
                UserId = account.UserId,
                Balance = account.Balance
            };
        }
        catch (Exception exception)
        {
            throw new Exception($"Error: {exception}.");
        }
    }


    public async Task<bool> PaymentAsync(PaymentRequest request, PaymentsDbContext context)
    {
        if (request.Amount <= 0)
        {
            throw new ArgumentException("Error: price can't be negative.");
        }

        try
        {
            var account = await context.Accounts.FirstOrDefaultAsync(e => e.UserId == request.UserId);
            if (account == null)
            {
                throw new KeyNotFoundException($"Error: can't find account for user {request.UserId}");
            }
            if (account.Balance < request.Amount)
            {
                return false;
            }


            var payment = new Payment
            {
                Id = Guid.NewGuid(),
                AccountId = account.UserId,
                OrderId = request.OrderId,
                Amount = -request.Amount,
                CreatedAt = DateTime.UtcNow
            };
            await context.Payments.AddAsync(payment);
            account.Balance -= request.Amount;

            return true;
        }
        catch (Exception exception)
        {
            throw new Exception($"Error: {exception}");
        }
    }
}

