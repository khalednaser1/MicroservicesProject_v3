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

var users = new []
{
    new { id=100, name="Ivan Petrov", email="ivan@example.com" },
    new { id=101, name="Masha Ivanova", email="masha@example.com" }
};

app.MapGet("/api/users/{id:int}", (int id) =>
{
    var u = Array.Find(users, x => x.id == id);
    return u is not null ? Results.Ok(u) : Results.NotFound();
});

app.Run();
