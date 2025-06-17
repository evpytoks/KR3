using System;
using System.IO;
using System.Net.Http;
using Microsoft.AspNetCore.Mvc;
using API_Gateway.DTOs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;
using Objects.DTOs;

namespace API_Gateway.Controllers;


[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly HttpClient HttpClient_;
    private readonly IConfiguration Config_;


    public OrdersController(IConfiguration config, HttpClient client)
    {
        Config_ = config;
        HttpClient_ = client;
    }


    [HttpPost("create")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderDto request)
    {
        var url = $"{Config_["OrderServiceUrl"]}/api/orders/create";
        try
        {
            var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
            var response = await HttpClient_.PostAsync(url, content);
            var body = await response.Content.ReadAsStringAsync();
            return StatusCode((int)response.StatusCode, JsonSerializer.Deserialize<object>(body));
        }
        catch (Exception exception)
        {
            return StatusCode(500, $"Error: {exception}.");
        }
    }


    [HttpGet("get_orders")]
    [ProducesResponseType(typeof(IEnumerable<OrderDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetUserOrders([FromQuery] Guid userId)
    {
        var orderServiceUrl = Config_["OrderServiceUrl"];
        var url = $"{orderServiceUrl}/api/orders/get_all?userId={userId}";

        try
        {
            var response = await HttpClient_.GetAsync(url);
            var body = await response.Content.ReadAsStringAsync();
            return StatusCode((int)response.StatusCode, JsonSerializer.Deserialize<object>(body));
        }
        catch (Exception exception)
        {
            return StatusCode(500, $"Error: {exception}.");
        }
    }


    [HttpGet("get_status")]
    [ProducesResponseType(typeof(StatusDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetOrderStatus(Guid orderId, [FromQuery] Guid userId)
    {
        var url = $"{Config_["OrderServiceUrl"]}/api/orders/{orderId}?userId={userId}";
        try
        {
            var response = await HttpClient_.GetAsync(url);
            var body = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                return Ok(JsonSerializer.Deserialize<OrderDto>(body, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }).Status);
            }

            return StatusCode((int)response.StatusCode, body);
        }
        catch (Exception exception)
        {
            return StatusCode(500, $"Error: {exception}");
        }
    }
}
