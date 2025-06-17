using System;
using Microsoft.AspNetCore.Mvc;
using Objects.DTOs;
using PaymentsService.Services;

namespace PaymentsService.Controllers;


[ApiController]
[Route("api/[controller]")]
public class AccountsController : ControllerBase
{
    private readonly AccountsService AccountsService_;

    public AccountsController(AccountsService service)
    {
        AccountsService_ = service;
    }


    [HttpPost("create")]
    [ProducesResponseType(typeof(AccountDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> CreateAccount([FromBody] CreateAccountDto request)
    {
        try
        {
            var account = await AccountsService_.CreateAccountAsync(request);
            return Ok(account);
        }
        catch (Exception exception)
        {
            return StatusCode(500, $"Error: {exception}.");
        }

    }


    [HttpGet("get")]
    [ProducesResponseType(typeof(AccountDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> GetAccountById([FromQuery] Guid userId)
    {
        try
        {
            var account = await AccountsService_.GetAccountAsync(userId);
            return Ok(account);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"Error: can't find account for user {userId}.");
        }
        catch (Exception exception)
        {
            return StatusCode(500, $"Error: {exception}");
        }
    }


    [HttpPost("increase_balance")]
    [ProducesResponseType(typeof(AccountDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> IncreaseBalance([FromBody] IncreaseBalanceDto request)
    {
        try
        {
            var account = await AccountsService_.IncreaseBalanceAsync(request);
            return Ok(account);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"Error: can't find account for user {request.UserId}.");
        }
        catch (Exception exception)
        {
            return StatusCode(500, $"Error: {exception}");
        }
    }
}
