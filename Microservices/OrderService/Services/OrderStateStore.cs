using OrderService.DTOs;

namespace OrderService.Services;

public class OrderStateStore
{
    private readonly Dictionary<string, OrderResponseDto> _orders = new(StringComparer.OrdinalIgnoreCase);

    public void SaveOrder(OrderResponseDto order)
    {
        _orders[order.OrderId] = order;
    }

    public void Cancel(string orderId, string reason)
    {
        if (_orders.TryGetValue(orderId, out var order))
        {
            order.Status = $"Cancelled: {reason}";
        }
    }

    public OrderResponseDto? GetOrder(string orderId)
    {
        return _orders.TryGetValue(orderId, out var order) ? order : null;
    }
}
