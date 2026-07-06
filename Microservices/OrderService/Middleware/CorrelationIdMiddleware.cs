using Serilog.Context;
using OrderService.Infrastructure;

namespace OrderService.Middleware;

public class CorrelationIdMiddleware
{
    public const string HeaderName = "X-Correlation-ID";
    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, ICorrelationIdAccessor correlationIdAccessor)
    {
        var correlationId = context.Request.Headers.TryGetValue(HeaderName, out var existingCorrelationId) && !string.IsNullOrWhiteSpace(existingCorrelationId)
            ? existingCorrelationId.ToString()
            : Guid.NewGuid().ToString("N");

        correlationIdAccessor.CorrelationId = correlationId;
        context.Response.Headers[HeaderName] = correlationId;
        context.Items[HeaderName] = correlationId;

        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await _next(context);
        }
    }
}
