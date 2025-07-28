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
        private readonly string _sensorApiUrl;
        private readonly string _plantConfigApiUrl;

        public ExternalApiService(HttpClient httpClient, ILogger<ExternalApiService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;

            _sensorApiUrl = Environment.GetEnvironmentVariable("SENSOR_API_URL")
                           ?? "http://3.0.148.231:8010/sensor-readings";
            _plantConfigApiUrl = Environment.GetEnvironmentVariable("PLANT_CONFIG_API_URL")
                                ?? "http://3.0.148.231:8020/plant-configurations";

            _logger.LogInformation("Configured API URLs - Sensor: {SensorUrl}, Config: {ConfigUrl}",
                _sensorApiUrl, _plantConfigApiUrl);
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
                _logger.LogInformation("Raw sensor JSON length: {Length}", jsonContent.Length);

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
                        reading.TrayIdValue = ParseTrayIdToInt(trayIdElement);
                    }

                    if (element.TryGetProperty("timestamp", out var timestampElement))
                    {
                        reading.Timestamp = ParseDateTime(timestampElement);
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
                _logger.LogInformation("Raw plant config JSON length: {Length}", jsonContent.Length);

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
                        config.TrayIdValue = ParseTrayIdToInt(trayIdElement);
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

        private int ParseTrayIdToInt(JsonElement trayIdElement)
        {
            try
            {
                switch (trayIdElement.ValueKind)
                {
                    case JsonValueKind.Number:
                        return trayIdElement.GetInt32();

                    case JsonValueKind.String:
                        var stringValue = trayIdElement.GetString();
                        if (string.IsNullOrEmpty(stringValue))
                            return 0;

                        if (int.TryParse(stringValue, out var intValue))
                        {
                            return intValue;
                        }

                        if (stringValue.StartsWith("TRAY", StringComparison.OrdinalIgnoreCase))
                        {
                            var numberPart = stringValue.Substring(4);
                            if (int.TryParse(numberPart, out var trayNumber))
                            {
                                return trayNumber;
                            }
                        }

                        var digits = new string(stringValue.Where(char.IsDigit).ToArray());
                        if (!string.IsNullOrEmpty(digits) && int.TryParse(digits, out var digitValue))
                        {
                            return digitValue;
                        }

                        break;
                }

                _logger.LogWarning("Could not parse tray_id from value: {Value}", trayIdElement.GetRawText());
                return 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing tray_id: {Value}", trayIdElement.GetRawText());
                return 0;
            }
        }

        private DateTime ParseDateTime(JsonElement timestampElement)
        {
            try
            {
                if (timestampElement.ValueKind == JsonValueKind.String)
                {
                    var timestampString = timestampElement.GetString();
                    if (DateTime.TryParse(timestampString, out var parsedTime))
                    {
                        return parsedTime;
                    }
                }

                return DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing timestamp: {Value}", timestampElement.GetRawText());
                return DateTime.UtcNow;
            }
        }

        private double GetDoubleProperty(JsonElement element, string propertyName)
        {
            try
            {
                if (element.TryGetProperty(propertyName, out var property))
                {
                    switch (property.ValueKind)
                    {
                        case JsonValueKind.Number:
                            return property.GetDouble();

                        case JsonValueKind.String:
                            var stringValue = property.GetString();
                            if (double.TryParse(stringValue, out var parsed))
                            {
                                return parsed;
                            }
                            break;
                    }
                }

                return 0.0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing property {PropertyName}: {Value}",
                    propertyName, element.GetRawText());
                return 0.0;
            }
        }
    }
}