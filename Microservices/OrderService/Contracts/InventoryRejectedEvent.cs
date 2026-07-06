namespace OrderService.Contracts;

public record InventoryRejectedEvent(
    string OrderId,
    string Reason,
    DateTime RejectedAt);
