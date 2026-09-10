using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Sensor.Api.Endpoints;
using Sensor.Api.Infrastructure.Data;
using Sensor.Api.Infrastructure.Http;

var builder = WebApplication.CreateBuilder(args);

// --- Configuration ---
var connectionString =
    Environment.GetEnvironmentVariable("DATABASE_URL") ??
    builder.Configuration.GetConnectionString("DefaultConnection") ??
    "Host=localhost;Port=5432;Database=smarthome;Username=postgres;Password=postgres";

var temperatureApiUrl =
    Environment.GetEnvironmentVariable("TEMPERATURE_API_URL") ??
    builder.Configuration["TemperatureApi:BaseUrl"] ??
    "http://temperature-api:8081";

// PORT env var may include a leading colon (e.g. ":8080") to match the Go convention
var portEnv = Environment.GetEnvironmentVariable("PORT") ?? ":8080";
var port = portEnv.TrimStart(':');
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

// Match Go's 5-second graceful shutdown timeout
builder.Services.Configure<HostOptions>(o => o.ShutdownTimeout = TimeSpan.FromSeconds(5));

// --- JSON: use snake_case to preserve the Go API contract ---
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
    options.SerializerOptions.DefaultIgnoreCondition =
        System.Text.Json.Serialization.JsonIgnoreCondition.Never;
});

// --- Database ---
builder.Services.AddDbContext<SmartHomeDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddScoped<ISensorRepository, SensorRepository>();

// --- External temperature API (typed HttpClient with 10-second timeout) ---
builder.Services.AddHttpClient<ITemperatureService, TemperatureService>(client =>
{
    var baseUrl = temperatureApiUrl.TrimEnd('/') + '/';
    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromSeconds(10);
});

// --- OpenAPI / Swagger ---
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Validate database connectivity on startup (mirrors Go's Ping check)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<SmartHomeDbContext>();
    try
    {
        await db.Database.OpenConnectionAsync();
        await db.Database.CloseConnectionAsync();
        app.Logger.LogInformation("Connected to database successfully");
    }
    catch (Exception ex)
    {
        app.Logger.LogCritical(ex, "Unable to connect to database");
        throw;
    }
}

app.Logger.LogInformation("Temperature service initialized with API URL: {Url}", temperatureApiUrl);

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Health check — same response shape as Go: {"status":"ok"}
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.MapSensorEndpoints();

app.Logger.LogInformation("Server starting on :{Port}", port);
app.Run();
