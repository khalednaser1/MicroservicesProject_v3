using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Serilog;
using Prometheus;

Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateLogger();

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog();
builder.Services.AddHealthChecks();

var app = builder.Build();
app.MapMetrics();
app.MapHealthChecks("/health");

var products = new []
{
    new { id=1, name="Laptop", price=1200 },
    new { id=2, name="Phone", price=600 },
    new { id=3, name="Headphones", price=80 }
};

app.MapGet("/api/products", () => Results.Ok(products));
app.MapGet("/api/products/{id:int}", (int id) =>
{
    var p = Array.Find(products, x => x.id == id);
    return p is not null ? Results.Ok(p) : Results.NotFound();
});

app.Run();
