using IndoorFarmMonitoring.Models;
using System.Text.Json;

namespace IndoorFarmMonitoring.Services
{
    public interface IExternalApiService
    {
        Task<List<SensorReading>> GetSensorReadingsAsync();
        Task<List<PlantConfiguration>> GetPlantConfigurationsAsync();
    }
    public class ExternalApiService : IExternalApiService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<ExternalApiService> _logger;
        private readonly string _sensorApiUrl = "http://3.0.148.231:8010/sensor-readings";
        private readonly string _plantConfigApiUrl = "http://3.0.148.231:8020/plant-configurations";

        public ExternalApiService(HttpClient httpClient, ILogger<ExternalApiService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<List<SensorReading>> GetSensorReadingsAsync()
        {
            try
            {
                _logger.LogInformation("Fetching sensor readings from {Url}", _sensorApiUrl);

                var response = await _httpClient.GetAsync(_sensorApiUrl);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Failed to fetch sensor readings. Status: {StatusCode}", response.StatusCode);
                    throw new HttpRequestException($"Failed to fetch sensor readings: {response.StatusCode}");
                }

                var jsonContent = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("Raw sensor JSON: {Json}", jsonContent.Length > 500 ? jsonContent.Substring(0, 500) + "..." : jsonContent);

                if (string.IsNullOrWhiteSpace(jsonContent))
                {
                    return new List<SensorReading>();
                }

                var document = JsonDocument.Parse(jsonContent);
                var sensorReadings = new List<SensorReading>();

                foreach (var element in document.RootElement.EnumerateArray())
                {
                    var reading = new SensorReading();

                    if (element.TryGetProperty("tray_id", out var trayIdElement))
                    {
                        reading.TrayIdValue = trayIdElement.ValueKind == JsonValueKind.String
                            ? trayIdElement.GetString()
                            : trayIdElement.GetRawText();
                    }

                    if (element.TryGetProperty("timestamp", out var timestampElement))
                    {
                        if (timestampElement.ValueKind == JsonValueKind.String)
                        {
                            DateTime.TryParse(timestampElement.GetString(), out var parsedTime);
                            reading.Timestamp = parsedTime != default ? parsedTime : DateTime.UtcNow;
                        }
                        else
                        {
                            reading.Timestamp = DateTime.UtcNow;
                        }
                    }
                    else
                    {
                        reading.Timestamp = DateTime.UtcNow;
                    }

                    reading.Temperature = GetDoubleProperty(element, "temperature");
                    reading.Humidity = GetDoubleProperty(element, "humidity");
                    reading.LightIntensity = GetDoubleProperty(element, "light_intensity");
                    reading.PhLevel = GetDoubleProperty(element, "ph_level");

                    sensorReadings.Add(reading);
                }

                _logger.LogInformation("Successfully parsed {Count} sensor readings", sensorReadings.Count);
                return sensorReadings;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching sensor readings");
                throw;
            }
        }

        public async Task<List<PlantConfiguration>> GetPlantConfigurationsAsync()
        {
            try
            {
                _logger.LogInformation("Fetching plant configurations from {Url}", _plantConfigApiUrl);

                var response = await _httpClient.GetAsync(_plantConfigApiUrl);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Failed to fetch plant configurations. Status: {StatusCode}", response.StatusCode);
                    throw new HttpRequestException($"Failed to fetch plant configurations: {response.StatusCode}");
                }

                var jsonContent = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("Raw plant config JSON: {Json}", jsonContent.Length > 500 ? jsonContent.Substring(0, 500) + "..." : jsonContent);

                if (string.IsNullOrWhiteSpace(jsonContent))
                {
                    return new List<PlantConfiguration>();
                }

                var document = JsonDocument.Parse(jsonContent);
                var plantConfigurations = new List<PlantConfiguration>();

                foreach (var element in document.RootElement.EnumerateArray())
                {
                    var config = new PlantConfiguration();

                    if (element.TryGetProperty("tray_id", out var trayIdElement))
                    {
                        config.TrayIdValue = trayIdElement.ValueKind == JsonValueKind.String
                            ? trayIdElement.GetString()
                            : trayIdElement.GetRawText();
                    }

                    if (element.TryGetProperty("plant_type", out var plantTypeElement))
                    {
                        config.PlantType = plantTypeElement.GetString() ?? "";
                    }

                    config.TargetTemperature = GetDoubleProperty(element, "target_temperature");
                    config.TargetHumidity = GetDoubleProperty(element, "target_humidity");
                    config.TargetLightIntensity = GetDoubleProperty(element, "target_light_intensity");
                    config.TargetPhLevel = GetDoubleProperty(element, "target_ph_level");
                    config.TolerancePercentage = GetDoubleProperty(element, "tolerance_percentage");

                    plantConfigurations.Add(config);
                }

                _logger.LogInformation("Successfully parsed {Count} plant configurations", plantConfigurations.Count);
                return plantConfigurations;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching plant configurations");
                throw;
            }
        }

        private double GetDoubleProperty(JsonElement element, string propertyName)
        {
            if (element.TryGetProperty(propertyName, out var property))
            {
                if (property.ValueKind == JsonValueKind.Number)
                {
                    return property.GetDouble();
                }
                if (property.ValueKind == JsonValueKind.String &&
                    double.TryParse(property.GetString(), out var parsed))
                {
                    return parsed;
                }
            }
            return 0.0;
        }
    }
}