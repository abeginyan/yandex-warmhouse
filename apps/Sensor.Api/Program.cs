using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Sensor.Api.Infrastructure.Data;
using Sensor.Api.Infrastructure.Http;
using Sensor.Api.Services;

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

// Match Go's 5-second graceful shutdown timeout
builder.Services.Configure<HostOptions>(o => o.ShutdownTimeout = TimeSpan.FromSeconds(5));

// --- Controllers with snake_case JSON (preserves the Go API contract) ---
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.Never;
    });

// snake_case also for the minimal-API /health endpoint
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
});

// --- Database ---
builder.Services.AddDbContext<SensorDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddScoped<ISensorRepository, SensorRepository>();
builder.Services.AddScoped<ISensorService, SensorService>();

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

// Apply pending EF Core migrations on startup.
// Creates the database and schema automatically if they don't exist.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<SensorDbContext>();
    try
    {
        await db.Database.MigrateAsync();
        app.Logger.LogInformation("Database migrations applied successfully");
    }
    catch (Exception ex)
    {
        app.Logger.LogCritical(ex, "Failed to apply database migrations");
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

app.MapControllers();

app.Run();
