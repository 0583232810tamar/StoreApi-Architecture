namespace InventoryService.Services;

public class InventoryStore
{
    private readonly Dictionary<string, int> _stockByProductId = new(StringComparer.OrdinalIgnoreCase)
    {
        ["product-1"] = 10,
        ["product-2"] = 5,
        ["product-3"] = 0
    };

    public bool TryReserve(string productId, int quantity)
    {
        if (!_stockByProductId.TryGetValue(productId, out var currentStock))
        {
            return false;
        }

        if (currentStock < quantity)
        {
            return false;
        }

        _stockByProductId[productId] = currentStock - quantity;
        return true;
    }
}
