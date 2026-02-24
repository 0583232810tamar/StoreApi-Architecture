using StackExchange.Redis;

namespace StoreApi.Middleware;

// Redis-backed rate limiter using atomic INCR + EXPIRE.
// Why Redis instead of ConcurrentDictionary:
//   - In-memory state is lost on restart and is not shared across replicas.
//   - Redis INCR is atomic: no race conditions, no locking needed.
//   - Works correctly when the API is scaled horizontally behind a load balancer.
public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RateLimitingMiddleware> _logger;
    private readonly IConnectionMultiplexer _redis;
    private readonly int _requestLimit;
    private readonly TimeSpan _timeWindow;

    public RateLimitingMiddleware(
        RequestDelegate next,
        ILogger<RateLimitingMiddleware> logger,
        IConnectionMultiplexer redis,
        IConfiguration configuration)
    {
        _next = next;
        _logger = logger;
        _redis = redis;
        _requestLimit = configuration.GetValue<int>("RateLimiting:RequestLimit", 100);
        _timeWindow = TimeSpan.FromMinutes(configuration.GetValue<int>("RateLimiting:TimeWindowMinutes", 1));
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var clientId = GetClientIdentifier(context);
        var key = $"ratelimit:{clientId}";
        var db = _redis.GetDatabase();

        // INCR is atomic in Redis — no locks needed even under high concurrency.
        var count = await db.StringIncrementAsync(key);

        // On first request in window, set the TTL that defines the window boundary.
        if (count == 1)
            await db.KeyExpireAsync(key, _timeWindow);

        var ttl = await db.KeyTimeToLiveAsync(key);
        var resetTime = DateTimeOffset.UtcNow.Add(ttl ?? _timeWindow);
        var retryAfterSeconds = (int)(ttl?.TotalSeconds ?? _timeWindow.TotalSeconds);

        if (count > _requestLimit)
        {
            _logger.LogWarning("Rate limit exceeded for client: {ClientId}", clientId);

            context.Response.StatusCode = 429;
            context.Response.Headers["Retry-After"] = retryAfterSeconds.ToString();

            await context.Response.WriteAsJsonAsync(new
            {
                message = "Rate limit exceeded. Please try again later.",
                statusCode = 429,
                retryAfter = $"{retryAfterSeconds} seconds"
            });
            return;
        }

        context.Response.Headers["X-Rate-Limit-Limit"] = _requestLimit.ToString();
        context.Response.Headers["X-Rate-Limit-Remaining"] = Math.Max(0, _requestLimit - (int)count).ToString();
        context.Response.Headers["X-Rate-Limit-Reset"] = resetTime.ToString("o");

        await _next(context);
    }

    private string GetClientIdentifier(HttpContext context)
    {
        var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return ipAddress;
    }
}
