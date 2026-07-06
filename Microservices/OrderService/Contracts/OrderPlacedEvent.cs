namespace OrderService.Contracts;

public record OrderPlacedEvent(
    string OrderId,
    string CustomerId,
    decimal TotalAmount,
    DateTime OrderDate,
    IReadOnlyCollection<OrderPlacedItemEvent> Items);

public record OrderPlacedItemEvent(
    string ProductId,
    int Quantity,
    decimal UnitPrice);
