using MassTransit;
using OrderService.Infrastructure;
using OrderService.Consumers;
using OrderService.Middleware;
using OrderService.Services;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, loggerConfiguration) =>
    loggerConfiguration
        .Enrich.FromLogContext()
        .WriteTo.Console());

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<ICorrelationIdAccessor, CorrelationIdAccessor>();
builder.Services.AddSingleton<OrderStateStore>();

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<InventoryRejectedConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        var rabbitMqHost = builder.Configuration.GetConnectionString("RabbitMq") ?? "rabbitmq";
        var rabbitMqUsername = builder.Configuration.GetValue<string>("RabbitMq:Username") ?? "guest";
        var rabbitMqPassword = builder.Configuration.GetValue<string>("RabbitMq:Password") ?? "guest";

        cfg.Host(rabbitMqHost, "/", host =>
        {
            host.Username(rabbitMqUsername);
            host.Password(rabbitMqPassword);
        });

        cfg.ReceiveEndpoint("inventory-rejected", endpoint =>
        {
            endpoint.ConfigureConsumer<InventoryRejectedConsumer>(context);
        });
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseMiddleware<CorrelationIdMiddleware>();
app.MapGet("/health", () => Results.Ok(new { service = "OrderService", status = "Healthy" }));
app.MapControllers();

app.Run();
