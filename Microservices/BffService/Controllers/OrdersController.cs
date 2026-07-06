using BffService.DTOs;
using BffService.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BffService.Controllers;

[ApiController]
[Route("api/bff/orders")]
public class OrdersController : ControllerBase
{
    private readonly IOrderDetailsAggregatorService _aggregatorService;

    public OrdersController(IOrderDetailsAggregatorService aggregatorService)
    {
        _aggregatorService = aggregatorService;
    }

    [HttpGet("{orderId}")]
    [ProducesResponseType(typeof(OrderDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderDetailsDto>> GetOrderDetails(string orderId, CancellationToken cancellationToken)
    {
        var details = await _aggregatorService.GetOrderDetailsAsync(orderId, cancellationToken);
        if (details is null)
        {
            return NotFound(new { message = $"Order with ID '{orderId}' was not found." });
        }

        return Ok(details);
    }
}
