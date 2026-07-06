namespace OrderService.Infrastructure;

public class CorrelationIdAccessor : ICorrelationIdAccessor
{
    private static readonly AsyncLocal<string?> CurrentCorrelationId = new();

    public string CorrelationId
    {
        get => CurrentCorrelationId.Value ?? string.Empty;
        set => CurrentCorrelationId.Value = value;
    }
}
