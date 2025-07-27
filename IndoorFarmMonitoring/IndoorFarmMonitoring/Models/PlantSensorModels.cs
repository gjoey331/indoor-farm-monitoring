using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace IndoorFarmMonitoring.Models
{
    public class SensorReading
    {
        private object? _trayIdValue;

        [JsonPropertyName("tray_id")]
        public object TrayIdValue
        {
            get => _trayIdValue ?? "";
            set => _trayIdValue = value;
        }

        public string TrayId => _trayIdValue?.ToString() ?? "";

        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; }

        [JsonPropertyName("temperature")]
        public double Temperature { get; set; }

        [JsonPropertyName("humidity")]
        public double Humidity { get; set; }

        [JsonPropertyName("light_intensity")]
        public double LightIntensity { get; set; }

        [JsonPropertyName("ph_level")]
        public double PhLevel { get; set; }
    }

    public class PlantConfiguration
    {
        private object? _trayIdValue;

        [JsonPropertyName("tray_id")]
        public object TrayIdValue
        {
            get => _trayIdValue ?? "";
            set => _trayIdValue = value;
        }

        public string TrayId => _trayIdValue?.ToString() ?? "";

        [JsonPropertyName("plant_type")]
        public string PlantType { get; set; } = string.Empty;

        [JsonPropertyName("target_temperature")]
        public double TargetTemperature { get; set; }

        [JsonPropertyName("target_humidity")]
        public double TargetHumidity { get; set; }

        [JsonPropertyName("target_light_intensity")]
        public double TargetLightIntensity { get; set; }

        [JsonPropertyName("target_ph_level")]
        public double TargetPhLevel { get; set; }

        [JsonPropertyName("tolerance_percentage")]
        public double TolerancePercentage { get; set; }
    }

    public class FlexibleSensorReading
    {
        [JsonPropertyName("tray_id")]
        [JsonConverter(typeof(StringOrNumberConverter))]
        public string TrayId { get; set; } = string.Empty;

        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; }

        [JsonPropertyName("temperature")]
        public double Temperature { get; set; }

        [JsonPropertyName("humidity")]
        public double Humidity { get; set; }

        [JsonPropertyName("light_intensity")]
        public double LightIntensity { get; set; }

        [JsonPropertyName("ph_level")]
        public double PhLevel { get; set; }
    }

    public class FlexiblePlantConfiguration
    {
        [JsonPropertyName("tray_id")]
        [JsonConverter(typeof(StringOrNumberConverter))]
        public string TrayId { get; set; } = string.Empty;

        [JsonPropertyName("plant_type")]
        public string PlantType { get; set; } = string.Empty;

        [JsonPropertyName("target_temperature")]
        public double TargetTemperature { get; set; }

        [JsonPropertyName("target_humidity")]
        public double TargetHumidity { get; set; }

        [JsonPropertyName("target_light_intensity")]
        public double TargetLightIntensity { get; set; }

        [JsonPropertyName("target_ph_level")]
        public double TargetPhLevel { get; set; }

        [JsonPropertyName("tolerance_percentage")]
        public double TolerancePercentage { get; set; }
    }

    public class StringOrNumberConverter : JsonConverter<string>
    {
        public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.String:
                    return reader.GetString() ?? "";
                case JsonTokenType.Number:
                    return reader.GetInt32().ToString();
                default:
                    throw new JsonException($"Cannot convert {reader.TokenType} to string");
            }
        }

        public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value);
        }
    }

    public class PlantSensorData
    {
        [Key]
        public int Id { get; set; }

        public string TrayId { get; set; } = string.Empty;
        public string PlantType { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }

        public double ActualTemperature { get; set; }
        public double ActualHumidity { get; set; }
        public double ActualLightIntensity { get; set; }
        public double ActualPhLevel { get; set; }

        public double TargetTemperature { get; set; }
        public double TargetHumidity { get; set; }
        public double TargetLightIntensity { get; set; }
        public double TargetPhLevel { get; set; }

        public double TolerancePercentage { get; set; }

        public bool IsTemperatureInRange { get; set; }
        public bool IsHumidityInRange { get; set; }
        public bool IsLightIntensityInRange { get; set; }
        public bool IsPhLevelInRange { get; set; }

        public bool IsAllMetricsInRange => IsTemperatureInRange && IsHumidityInRange &&
                                         IsLightIntensityInRange && IsPhLevelInRange;

        public double TemperatureDeviation { get; set; }
        public double HumidityDeviation { get; set; }
        public double LightIntensityDeviation { get; set; }
        public double PhLevelDeviation { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    public class ApiError
    {
        public string Code { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? Details { get; set; }
    }
}