using IndoorFarmMonitoring.Data;
using IndoorFarmMonitoring.Models;
using IndoorFarmMonitoring.Services;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using DotNetEnv;

Env.Load();

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddEnvironmentVariables();

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var apiTimeoutSeconds = int.Parse(Environment.GetEnvironmentVariable("API_TIMEOUT_SECONDS") ?? "30");
builder.Services.AddHttpClient<IExternalApiService, ExternalApiService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(apiTimeoutSeconds);
});

// Configure logging with environment variables
var logLevel = Environment.GetEnvironmentVariable("LOG_LEVEL") ?? "Information";
var logToConsole = Environment.GetEnvironmentVariable("LOG_TO_CONSOLE")?.ToLower() == "true";
var logToDebug = Environment.GetEnvironmentVariable("LOG_TO_DEBUG")?.ToLower() == "true";

builder.Services.AddLogging(logging =>
{
    logging.SetMinimumLevel(Enum.Parse<LogLevel>(logLevel));

    if (logToConsole)
        logging.AddConsole();

    if (logToDebug)
        logging.AddDebug();
});

var storageType = Environment.GetEnvironmentVariable("STORAGE_TYPE")
                 ?? builder.Configuration.GetValue<string>("StorageType")
                 ?? "InMemory";

switch (storageType.ToLower())
{
    case "postgresql":
        var connectionString = Environment.GetEnvironmentVariable("POSTGRES_CONNECTION_STRING")
                             ?? builder.Configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException("PostgreSQL connection string is required when using PostgreSQL storage. Set POSTGRES_CONNECTION_STRING environment variable.");
        }

        builder.Services.AddDbContext<FarmDbContext>(options =>
            options.UseNpgsql(connectionString));
        builder.Services.AddScoped<IDataStorageService, PostgreSqlStorageService>();
        break;

    case "json":
        var jsonFilePath = Environment.GetEnvironmentVariable("JSON_FILE_PATH")
                          ?? builder.Configuration.GetValue<string>("JsonFilePath")
                          ?? "plant_data.json";

        builder.Services.AddSingleton<IDataStorageService>(provider =>
            new JsonFileStorageService(jsonFilePath, provider.GetRequiredService<ILogger<JsonFileStorageService>>()));
        break;

    case "inmemory":
    default:
        builder.Services.AddSingleton<IDataStorageService, InMemoryStorageService>();
        break;
}

builder.Services.AddScoped<IPlantSensorService, PlantSensorService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (storageType.ToLower() == "postgresql")
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<FarmDbContext>();
    context.Database.EnsureCreated();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();


app.MapGet("/api/env-info", () => new
{
    Environment = app.Environment.EnvironmentName,
    StorageType = storageType,
    ApiTimeout = $"{apiTimeoutSeconds}s",
    LogLevel = logLevel,
    Timestamp = DateTime.UtcNow
});

app.Run();