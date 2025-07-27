using IndoorFarmMonitoring.Data;
using IndoorFarmMonitoring.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;
using System.Text.Json;

namespace IndoorFarmMonitoring.Services
{
    public interface IDataStorageService
    {
        Task SavePlantSensorDataAsync(List<PlantSensorData> data);
        Task<List<PlantSensorData>> GetAllPlantSensorDataAsync();
        Task<PlantSensorData?> GetPlantSensorDataByTrayIdAsync(string trayId);
    }

    public class PostgreSqlStorageService : IDataStorageService
    {
        private readonly FarmDbContext _context;
        private readonly ILogger<PostgreSqlStorageService> _logger;

        public PostgreSqlStorageService(FarmDbContext context, ILogger<PostgreSqlStorageService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task SavePlantSensorDataAsync(List<PlantSensorData> data)
        {
            try
            {
                _logger.LogInformation("Saving {Count} plant sensor data records to PostgreSQL", data.Count);

                await _context.PlantSensorData.AddRangeAsync(data);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Successfully saved plant sensor data to PostgreSQL");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving plant sensor data to PostgreSQL");
                throw;
            }
        }

        public async Task<List<PlantSensorData>> GetAllPlantSensorDataAsync()
        {
            try
            {
                return await _context.PlantSensorData
                    .OrderByDescending(x => x.Timestamp)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving plant sensor data from PostgreSQL");
                throw;
            }
        }

        public async Task<PlantSensorData?> GetPlantSensorDataByTrayIdAsync(string trayId)
        {
            try
            {
                return await _context.PlantSensorData
                    .Where(x => x.TrayId == trayId)
                    .OrderByDescending(x => x.Timestamp)
                    .FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving plant sensor data for tray {TrayId} from PostgreSQL", trayId);
                throw;
            }
        }
    }

    public class InMemoryStorageService : IDataStorageService
    {
        private readonly ConcurrentBag<PlantSensorData> _data = new();
        private readonly ILogger<InMemoryStorageService> _logger;

        public InMemoryStorageService(ILogger<InMemoryStorageService> logger)
        {
            _logger = logger;
        }

        public Task SavePlantSensorDataAsync(List<PlantSensorData> data)
        {
            try
            {
                _logger.LogInformation("Saving {Count} plant sensor data records to in-memory storage", data.Count);

                foreach (var item in data)
                {
                    // Assign a simple ID for in-memory storage
                    item.Id = _data.Count + 1;
                    _data.Add(item);
                }

                _logger.LogInformation("Successfully saved plant sensor data to in-memory storage");
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving plant sensor data to in-memory storage");
                throw;
            }
        }

        public Task<List<PlantSensorData>> GetAllPlantSensorDataAsync()
        {
            try
            {
                var result = _data.OrderByDescending(x => x.Timestamp).ToList();
                return Task.FromResult(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving plant sensor data from in-memory storage");
                throw;
            }
        }

        public Task<PlantSensorData?> GetPlantSensorDataByTrayIdAsync(string trayId)
        {
            try
            {
                var result = _data
                    .Where(x => x.TrayId == trayId)
                    .OrderByDescending(x => x.Timestamp)
                    .FirstOrDefault();

                return Task.FromResult(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving plant sensor data for tray {TrayId} from in-memory storage", trayId);
                throw;
            }
        }
    }

    public class JsonFileStorageService : IDataStorageService
    {
        private readonly string _filePath;
        private readonly ILogger<JsonFileStorageService> _logger;
        private readonly SemaphoreSlim _fileLock = new(1, 1);

        public JsonFileStorageService(string filePath, ILogger<JsonFileStorageService> logger)
        {
            _filePath = filePath;
            _logger = logger;
        }

        public async Task SavePlantSensorDataAsync(List<PlantSensorData> data)
        {
            await _fileLock.WaitAsync();
            try
            {
                _logger.LogInformation("Saving {Count} plant sensor data records to JSON file: {FilePath}",
                    data.Count, _filePath);

                var existingData = await ReadDataFromFileAsync();

                var maxId = existingData.Any() ? existingData.Max(x => x.Id) : 0;
                foreach (var item in data)
                {
                    item.Id = ++maxId;
                }

                existingData.AddRange(data);

                var jsonOptions = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                var jsonString = JsonSerializer.Serialize(existingData, jsonOptions);
                await File.WriteAllTextAsync(_filePath, jsonString);

                _logger.LogInformation("Successfully saved plant sensor data to JSON file");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving plant sensor data to JSON file");
                throw;
            }
            finally
            {
                _fileLock.Release();
            }
        }

        public async Task<List<PlantSensorData>> GetAllPlantSensorDataAsync()
        {
            await _fileLock.WaitAsync();
            try
            {
                var data = await ReadDataFromFileAsync();
                return data.OrderByDescending(x => x.Timestamp).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving plant sensor data from JSON file");
                throw;
            }
            finally
            {
                _fileLock.Release();
            }
        }

        public async Task<PlantSensorData?> GetPlantSensorDataByTrayIdAsync(string trayId)
        {
            await _fileLock.WaitAsync();
            try
            {
                var data = await ReadDataFromFileAsync();
                return data
                    .Where(x => x.TrayId == trayId)
                    .OrderByDescending(x => x.Timestamp)
                    .FirstOrDefault();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving plant sensor data for tray {TrayId} from JSON file", trayId);
                throw;
            }
            finally
            {
                _fileLock.Release();
            }
        }

        private async Task<List<PlantSensorData>> ReadDataFromFileAsync()
        {
            if (!File.Exists(_filePath))
            {
                return new List<PlantSensorData>();
            }

            var jsonString = await File.ReadAllTextAsync(_filePath);
            if (string.IsNullOrWhiteSpace(jsonString))
            {
                return new List<PlantSensorData>();
            }

            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            var data = JsonSerializer.Deserialize<List<PlantSensorData>>(jsonString, jsonOptions);
            return data ?? new List<PlantSensorData>();
        }
    }
}