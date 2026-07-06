using BffService.Interfaces;
using BffService.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddHttpClient("OrderService", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["DownstreamServices:OrderServiceBaseUrl"]!);
});

builder.Services.AddHttpClient("ProductCatalogService", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["DownstreamServices:ProductCatalogServiceBaseUrl"]!);
});

builder.Services.AddScoped<IOrderDetailsAggregatorService, OrderDetailsAggregatorService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapControllers();

app.Run();
