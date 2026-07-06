namespace OrderService.Services;

public class OrderStateStore
{
    private readonly Dictionary<string, string> _orderStatuses = new(StringComparer.OrdinalIgnoreCase);

    public void MarkPlaced(string orderId) => _orderStatuses[orderId] = "Placed";

    public void Cancel(string orderId, string reason) => _orderStatuses[orderId] = $"Cancelled: {reason}";

    public string? GetStatus(string orderId)
    {
        return _orderStatuses.TryGetValue(orderId, out var status) ? status : null;
    }
}
