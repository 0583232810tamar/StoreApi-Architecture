using MassTransit;
using InventoryService.Contracts;
using InventoryService.Services;

namespace InventoryService.Consumers;

public class OrderPlacedConsumer : IConsumer<OrderPlacedEvent>
{
    private readonly InventoryStore _inventoryStore;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<OrderPlacedConsumer> _logger;

    public OrderPlacedConsumer(InventoryStore inventoryStore, IPublishEndpoint publishEndpoint, ILogger<OrderPlacedConsumer> logger)
    {
        _inventoryStore = inventoryStore;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<OrderPlacedEvent> context)
    {
        var order = context.Message;

        foreach (var item in order.Items)
        {
            var reserved = _inventoryStore.TryReserve(item.ProductId, item.Quantity);
            if (!reserved)
            {
                _logger.LogWarning(
                    "Inventory rejection for OrderId {OrderId}, ProductId {ProductId}, Quantity {Quantity}",
                    order.OrderId,
                    item.ProductId,
                    item.Quantity);

                await _publishEndpoint.Publish(new InventoryRejectedEvent(
                    order.OrderId,
                    $"Insufficient stock for product '{item.ProductId}'",
                    DateTime.UtcNow));

                return;
            }
        }

        _logger.LogInformation("Inventory reserved for OrderId {OrderId}", order.OrderId);
    }
}
