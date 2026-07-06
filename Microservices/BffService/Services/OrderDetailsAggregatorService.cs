using System.Net.Http.Json;
using BffService.DTOs;
using BffService.Interfaces;

namespace BffService.Services;

public class OrderDetailsAggregatorService : IOrderDetailsAggregatorService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<OrderDetailsAggregatorService> _logger;

    public OrderDetailsAggregatorService(IHttpClientFactory httpClientFactory, ILogger<OrderDetailsAggregatorService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<OrderDetailsDto?> GetOrderDetailsAsync(string orderId, CancellationToken cancellationToken = default)
    {
        var orderClient = _httpClientFactory.CreateClient("OrderService");
        var productClient = _httpClientFactory.CreateClient("ProductCatalogService");

    var orderTask = orderClient.GetFromJsonAsync<UpstreamOrderResponseDto>($"{orderClient.BaseAddress}api/Orders/{orderId}", cancellationToken);
var productsTask = productClient.GetFromJsonAsync<IReadOnlyCollection<UpstreamProductResponseDto>>($"{productClient.BaseAddress}api/Products", cancellationToken);
        await Task.WhenAll(orderTask, productsTask);

        var order = await orderTask;
        if (order is null)
        {
            return null;
        }

        var products = await productsTask ?? Array.Empty<UpstreamProductResponseDto>();
        var productLookup = products.ToDictionary(product => product.Id, product => product);

        var items = order.Items.Select(item =>
        {
            productLookup.TryGetValue(item.ProductId, out var product);

            return new OrderDetailsItemDto
            {
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                LineTotal = item.Quantity * item.UnitPrice,
                Product = new ProductSummaryDto
                {
                    ProductId = item.ProductId,
                    Name = product?.Name ?? string.Empty,
                    Description = product?.Description ?? string.Empty,
                    Price = product?.Price ?? item.UnitPrice
                }
            };
        }).ToList();

        _logger.LogInformation("Aggregated order details for OrderId {OrderId}", orderId);

        return new OrderDetailsDto
        {
            OrderId = order.Id,
            CustomerId = order.CustomerId,
            OrderDate = order.OrderDate,
            Status = order.Status,
            TotalAmount = order.TotalAmount,
            Items = items
        };
    }
}
