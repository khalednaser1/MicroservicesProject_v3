using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Prometheus;

Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateLogger();

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog();
builder.Services.AddHealthChecks();

var app = builder.Build();
app.MapMetrics();
app.MapHealthChecks("/health");

var orders = new []
{
    new { id=10, userId=100, productId=1, quantity=2, status="Pending" },
    new { id=11, userId=101, productId=2, quantity=1, status="Pending" },
    new { id=12, userId=100, productId=3, quantity=1, status="Completed" }
};

app.MapGet("/api/orders", (int? userId) =>
{
    if (userId.HasValue)
        return Results.Ok(Array.FindAll(orders, o => o.userId == userId.Value));
    return Results.Ok(orders);
});

app.MapGet("/api/orders/{id:int}", (int id) =>
{
    var o = Array.Find(orders, x => x.id == id);
    return o is not null ? Results.Ok(o) : Results.NotFound();
});

app.Run();
