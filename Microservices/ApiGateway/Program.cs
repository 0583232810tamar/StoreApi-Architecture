var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "ApiGateway v1");
        options.SwaggerEndpoint("/swagger/product-catalog/v1/swagger.json", "Product Catalog Service");
        options.SwaggerEndpoint("/swagger/orders/v1/swagger.json", "Orders Service");
        options.SwaggerEndpoint("/swagger/bff/v1/swagger.json", "BFF Service");
        options.SwaggerEndpoint("/swagger/storeapi/v1/swagger.json", "StoreApi Core & Auth");
        options.SwaggerEndpoint("/swagger/notification/v1/swagger.json", "Notification Service");
    });
}

app.MapGet("/health", () => Results.Ok(new { service = "ApiGateway", status = "Healthy" }));
app.MapReverseProxy();

app.Run();
