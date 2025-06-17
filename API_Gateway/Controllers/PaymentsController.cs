
using System;
using System.IO;
using System.Net.Http;
using Microsoft.AspNetCore.Mvc;
using API_Gateway.DTOs;
using System.Security.Principal;
using System.Text;
using System.Text.Json;
using Objects.DTOs;

namespace API_Gateway.Controllers;


[ApiController]
[Route("api/[controller]")]
public class PaymentsController : ControllerBase
{
    private readonly IConfiguration Config_;
    private readonly HttpClient HttpClient_;

    public PaymentsController(IConfiguration config, HttpClient client)
    {
        Config_ = config;
        HttpClient_ = client;
    }


    [HttpPost("create_account")]
    [ProducesResponseType(typeof(AccountDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateAccount([FromBody] CreateAccountDto request)
    {
        var url = $"{Config_["PaymentsServiceUrl"]}/api/accounts/create";
        try
        {
            var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
            var response = await HttpClient_.PostAsync(url, content);
            var body = await response.Content.ReadAsStringAsync();
            if (response.IsSuccessStatusCode)
            {
                if (!string.IsNullOrEmpty(body))
                {
                    var account = JsonSerializer.Deserialize<AccountDto>(body, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    return StatusCode((int)response.StatusCode, account);
                }
                else
                {
                    return StatusCode((int)response.StatusCode);
                }
            }
            else
            {
                return StatusCode((int)response.StatusCode, body);
            }
        }
        catch (Exception exception)
        {
            return StatusCode(500, $"Error: {exception}.");
        }
    }


    [HttpPost("increase_balance")]
    [ProducesResponseType(typeof(AccountDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> IncreaseBalance([FromBody] IncreaseBalanceDto request)
    {
        var url = $"{Config_["PaymentsServiceUrl"]}/api/accounts/increase_balance";
        try
        {
            var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
            var response = await HttpClient_.PostAsync(url, content);
            var body = await response.Content.ReadAsStringAsync();
            if (response.IsSuccessStatusCode)
            {
                if (!string.IsNullOrEmpty(body))
                {
                    var account = JsonSerializer.Deserialize<AccountDto>(body, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    return StatusCode((int)response.StatusCode, account);
                }
                else
                {
                    return StatusCode((int)response.StatusCode);
                }
            }
            else
            {
                return StatusCode((int)response.StatusCode, body);
            }
        }
        catch (Exception exception)
        {
            return StatusCode(500, $"Error: {exception}");
        }
    }


    [HttpPost("get_balance")]
    [ProducesResponseType(typeof(BalanceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetBalance([FromQuery] Guid userId)
    {
        var url = $"{Config_["PaymentsServiceUrl"]}/api/accounts/get?userId={userId}";
        try
        {
            var response = await HttpClient_.GetAsync(url);
            var body = await response.Content.ReadAsStringAsync();
            if (response.IsSuccessStatusCode)
            {
                var account = JsonSerializer.Deserialize<AccountDto>(body, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                return Ok(new BalanceDto { Balance = account.Balance });
            }

            return StatusCode((int)response.StatusCode, $"Error: {body}");

        }
        catch (Exception exception)
        {
            return StatusCode(500, $"Error:{exception}");
        }
    }
}
