using System;
using Microsoft.AspNetCore.Mvc;
using Objects.DTOs;
using OrderService.Services;

namespace OrderService.Controllers;


[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly OrderService.Services.OrderService OrderService_;

    public OrdersController(OrderService.Services.OrderService service)
    {
        OrderService_ = service;
    }


    [HttpPost("create")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderDto request)
    {
        try
        {
            var order = await OrderService_.CreateOrderAsync(request);
            return Ok(order);
        }
        catch (Exception exception)
        {
            return StatusCode(500, $"Error: {exception}.");
        }
    }


    [HttpGet("{orderId:guid}")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetOrderById(Guid orderId, [FromQuery] Guid userId)
    {
        try
        {
            var order = await OrderService_.GetOrderByIdAsync(orderId, userId);
            return Ok(order);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (Exception exception)
        {
            return StatusCode(500, $"Error: {exception}.");
        }
    }


    [HttpGet("get_all")]
    [ProducesResponseType(typeof(IEnumerable<OrderDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetAllOrders([FromQuery] Guid userId)
    {
        try
        {
            var orders = await OrderService_.GetOrdersForUserAsync(userId);
            return Ok(orders);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (Exception exception)
        {
            return StatusCode(500, $"Error: {exception}.");
        }
    }
}
