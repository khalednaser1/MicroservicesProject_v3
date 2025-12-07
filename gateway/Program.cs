using System.Text.Json;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using StackExchange.Redis;
using Polly;
using Polly.Extensions.Http;
using Serilog;
using Prometheus;
using System.Threading.RateLimiting;

Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateLogger();

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog();

builder.Services.AddHttpClient("backend")
    .SetHandlerLifetime(TimeSpan.FromMinutes(5));

builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    var config = builder.Configuration.GetValue<string>("Redis:Connection") ?? "redis:6379";
    return ConnectionMultiplexer.Connect(config);
});

// Add health checks
builder.Services.AddHealthChecks()
    .AddCheck("self", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy());

var jwtKey = builder.Configuration.GetValue<string>("Jwt:Key") ?? "VerySecretDemoKey12345";
var keyBytes = Encoding.UTF8.GetBytes(jwtKey);

builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer("Bearer", options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(keyBytes)
        };
    });

builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("fixed", opts =>
    {
        opts.PermitLimit = 20;
        opts.Window = TimeSpan.FromSeconds(10);
        opts.QueueLimit = 5;
    });
});

var app = builder.Build();

app.UseSerilogRequestLogging();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapMetrics();
app.MapHealthChecks("/health");

// Token endpoint
app.MapPost("/token", (UserCredential cred) =>
{
    if (cred.Username == "ivan" || cred.Username == "masha")
    {
        var claims = new[]{
            new Claim(ClaimTypes.Name, cred.Username),
            new Claim("sub", cred.Username)
        };
        var key = new SymmetricSecurityKey(keyBytes);
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(30),
            signingCredentials: creds);
        return Results.Ok(new { access_token = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().WriteToken(token) });
    }
    return Results.BadRequest();
}).AllowAnonymous();

app.MapGet("/api/profile/{userId:int}", async (int userId, IHttpClientFactory http, IConnectionMultiplexer redis) =>
{
    var db = redis.GetDatabase();
    string cacheKey = $"profile_aggregate:{userId}";
    var cached = await db.StringGetAsync(cacheKey);
    if (cached.HasValue) return Results.Content(cached, "application/json");

    var retry = HttpPolicyExtensions.HandleTransientHttpError()
        .WaitAndRetryAsync(3, attempt => TimeSpan.FromMilliseconds(200 * attempt));

    var client = http.CreateClient("backend");
    var userResp = await retry.ExecuteAsync(() => client.GetAsync($"http://user-service:6003/api/users/{userId}"));
    if (!userResp.IsSuccessStatusCode) return Results.NotFound();

    var userJson = await userResp.Content.ReadAsStringAsync();
    var ordersResp = await retry.ExecuteAsync(() => client.GetAsync($"http://order-service:6002/api/orders?userId={userId}"));
    var ordersJson = await ordersResp.Content.ReadAsStringAsync();

    using var ordersDoc = JsonDocument.Parse(ordersJson);
    var orders = ordersDoc.RootElement.EnumerateArray().ToArray();

    var aggOrders = new List<object>();
    foreach (var o in orders)
    {
        var pid = o.GetProperty("productId").GetInt32();
        var prodResp = await retry.ExecuteAsync(() => client.GetAsync($"http://product-service:6001/api/products/{pid}"));
        var prodJson = prodResp.IsSuccessStatusCode
            ? await prodResp.Content.ReadAsStringAsync()
            : "{"id":0,"name":"unknown"}";
        aggOrders.Add(new {
            order = JsonSerializer.Deserialize<object>(o.GetRawText()),
            product = JsonSerializer.Deserialize<object>(prodJson)
        });
    }

    var result = JsonSerializer.Serialize(new {
        user = JsonSerializer.Deserialize<object>(userJson),
        orders = aggOrders
    }, new JsonSerializerOptions { WriteIndented = true });

    await db.StringSetAsync(cacheKey, result, TimeSpan.FromSeconds(30));
    return Results.Content(result, "application/json");
}).RequireAuthorization();

app.Run();

record UserCredential(string Username, string Password);
