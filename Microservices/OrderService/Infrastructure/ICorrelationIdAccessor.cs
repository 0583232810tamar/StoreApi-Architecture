namespace OrderService.Infrastructure;

public interface ICorrelationIdAccessor
{
    string CorrelationId { get; set; }
}
