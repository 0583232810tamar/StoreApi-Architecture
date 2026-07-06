using MassTransit;
using OrderService.Contracts;
using OrderService.Services;

namespace OrderService.Consumers;

public class InventoryRejectedConsumer : IConsumer<InventoryRejectedEvent>
{
    private readonly ILogger<InventoryRejectedConsumer> _logger;
    private readonly OrderStateStore _orderStateStore;

    public InventoryRejectedConsumer(ILogger<InventoryRejectedConsumer> logger, OrderStateStore orderStateStore)
    {
        _logger = logger;
        _orderStateStore = orderStateStore;
    }

    public Task Consume(ConsumeContext<InventoryRejectedEvent> context)
    {
        var message = context.Message;
        _orderStateStore.Cancel(message.OrderId, message.Reason);
        _logger.LogWarning("Compensation path triggered for OrderId {OrderId}: {Reason}", message.OrderId, message.Reason);
        return Task.CompletedTask;
    }
}
