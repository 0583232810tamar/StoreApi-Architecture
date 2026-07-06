using MassTransit;
using Microsoft.AspNetCore.Mvc;
using OrderService.Contracts;
using OrderService.DTOs;
using OrderService.Infrastructure;
using OrderService.Middleware;
using OrderService.Services;

namespace OrderService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ICorrelationIdAccessor _correlationIdAccessor;
    private readonly OrderStateStore _orderStateStore;
    private readonly ILogger<OrdersController> _logger;

    public OrdersController(IPublishEndpoint publishEndpoint, ICorrelationIdAccessor correlationIdAccessor, OrderStateStore orderStateStore, ILogger<OrdersController> logger)
    {
        _publishEndpoint = publishEndpoint;
        _correlationIdAccessor = correlationIdAccessor;
        _orderStateStore = orderStateStore;
        _logger = logger;
    }

    [HttpGet("{orderId}")]
    [ProducesResponseType(typeof(OrderResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<OrderResponseDto> GetById(string orderId)
    {
        var order = _orderStateStore.GetOrder(orderId);
        if (order is null)
        {
            return NotFound(new { message = $"Order with ID '{orderId}' was not found." });
        }
        return Ok(order);
    }

    [HttpPost]
    [ProducesResponseType(typeof(OrderResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<OrderResponseDto>> Create([FromBody] CreateOrderDto createOrderDto, CancellationToken cancellationToken)
    {
        if (createOrderDto.Items.Count == 0)
        {
            return BadRequest(new { message = "Order must contain at least one item." });
        }

        var orderId = Guid.NewGuid().ToString("N");
        var orderDate = DateTime.UtcNow;
        var totalAmount = createOrderDto.Items.Sum(item => item.Quantity * item.UnitPrice);

        var items = createOrderDto.Items
            .Select(item => new OrderResponseItemDto
            {
                ProductId = item.ProductId,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                LineTotal = item.Quantity * item.UnitPrice
            })
            .ToList();

        var response = new OrderResponseDto
        {
            OrderId = orderId,
            CustomerId = createOrderDto.CustomerId,
            OrderDate = orderDate,
            Status = "Placed",
            TotalAmount = totalAmount,
            Items = items
        };

        _orderStateStore.SaveOrder(response);

        var orderPlacedEvent = new OrderPlacedEvent(
            orderId,
            createOrderDto.CustomerId,
            totalAmount,
            orderDate,
            createOrderDto.Items.Select(item => new OrderPlacedItemEvent(item.ProductId, item.Quantity, item.UnitPrice)).ToList());

        var correlationId = _correlationIdAccessor.CorrelationId;

        await _publishEndpoint.Publish(orderPlacedEvent, publishContext =>
        {
            publishContext.Headers.Set(CorrelationIdMiddleware.HeaderName, correlationId);
        }, cancellationToken);

        _logger.LogInformation("Published OrderPlacedEvent for OrderId {OrderId} with CorrelationId {CorrelationId}", orderId, correlationId);

        return CreatedAtAction(nameof(GetById), new { orderId }, response);
    }
}
