using ProductCatalogService.Middleware;

namespace ProductCatalogService.Extensions;

public static class ApplicationBuilderExtensions
{
    public static IApplicationBuilder UseProductCatalogExceptionHandling(this IApplicationBuilder app)
    {
        app.UseMiddleware<GlobalExceptionMiddleware>();
        return app;
    }
}
