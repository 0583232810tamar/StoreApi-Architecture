using BffService.DTOs;

namespace BffService.Interfaces;

public interface IOrderDetailsAggregatorService
{
    Task<OrderDetailsDto?> GetOrderDetailsAsync(string orderId, CancellationToken cancellationToken = default);
}
