using IndoorFarmMonitoring.Data;
using IndoorFarmMonitoring.Models;
using IndoorFarmMonitoring.Services;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddHttpClient<IExternalApiService, ExternalApiService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddLogging(logging =>
{
    logging.AddConsole();
    logging.AddDebug();
});

var storageType = builder.Configuration.GetValue<string>("StorageType") ?? "InMemory";

switch (storageType.ToLower())
{
    case "postgresql":
        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException("PostgreSQL connection string is required when using PostgreSQL storage");
        }
        builder.Services.AddDbContext<FarmDbContext>(options =>
            options.UseNpgsql(connectionString));
        builder.Services.AddScoped<IDataStorageService, PostgreSqlStorageService>();
        break;

    case "json":
        var jsonFilePath = builder.Configuration.GetValue<string>("JsonFilePath") ?? "plant_data.json";
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

app.Run();