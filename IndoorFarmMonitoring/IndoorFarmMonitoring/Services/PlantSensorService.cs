using IndoorFarmMonitoring.Models;

namespace IndoorFarmMonitoring.Services
{
    public interface IPlantSensorService
    {
        Task<List<PlantSensorData>> GetPlantSensorDataAsync();
    }

    public class PlantSensorService : IPlantSensorService
    {
        private readonly IExternalApiService _externalApiService;
        private readonly IDataStorageService _dataStorageService;
        private readonly ILogger<PlantSensorService> _logger;

        public PlantSensorService(
            IExternalApiService externalApiService,
            IDataStorageService dataStorageService,
            ILogger<PlantSensorService> logger)
        {
            _externalApiService = externalApiService;
            _dataStorageService = dataStorageService;
            _logger = logger;
        }

        public async Task<List<PlantSensorData>> GetPlantSensorDataAsync()
        {
            try
            {
                _logger.LogInformation("Starting to fetch and combine plant sensor data");

                var sensorReadingsTask = _externalApiService.GetSensorReadingsAsync();
                var plantConfigurationsTask = _externalApiService.GetPlantConfigurationsAsync();

                await Task.WhenAll(sensorReadingsTask, plantConfigurationsTask);

                var sensorReadings = await sensorReadingsTask;
                var plantConfigurations = await plantConfigurationsTask;

                _logger.LogInformation("Fetched {SensorCount} sensor readings and {ConfigCount} plant configurations",
                    sensorReadings.Count, plantConfigurations.Count);

                var combinedData = CombineData(sensorReadings, plantConfigurations);

                _logger.LogInformation("Combined data for {CombinedCount} trays", combinedData.Count);

                await _dataStorageService.SavePlantSensorDataAsync(combinedData);

                _logger.LogInformation("Successfully saved combined plant sensor data");

                return combinedData;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while processing plant sensor data");
                throw;
            }
        }

        private List<PlantSensorData> CombineData(
            List<SensorReading> sensorReadings,
            List<PlantConfiguration> plantConfigurations)
        {
            var combinedData = new List<PlantSensorData>();

            var configDict = plantConfigurations.ToDictionary(pc => pc.TrayId, pc => pc);

            foreach (var sensorReading in sensorReadings)
            {
                if (!configDict.TryGetValue(sensorReading.TrayId, out var plantConfig))
                {
                    _logger.LogWarning("No plant configuration found for tray {TrayId}", sensorReading.TrayId);
                    continue;
                }

                var plantSensorData = new PlantSensorData
                {
                    TrayId = sensorReading.TrayId,
                    PlantType = plantConfig.PlantType,
                    Timestamp = sensorReading.Timestamp,

                    ActualTemperature = sensorReading.Temperature,
                    ActualHumidity = sensorReading.Humidity,
                    ActualLightIntensity = sensorReading.LightIntensity,
                    ActualPhLevel = sensorReading.PhLevel,

                    TargetTemperature = plantConfig.TargetTemperature,
                    TargetHumidity = plantConfig.TargetHumidity,
                    TargetLightIntensity = plantConfig.TargetLightIntensity,
                    TargetPhLevel = plantConfig.TargetPhLevel,

                    TolerancePercentage = plantConfig.TolerancePercentage
                };

                CalculateMetricsStatus(plantSensorData);

                combinedData.Add(plantSensorData);
            }

            return combinedData;
        }

        private void CalculateMetricsStatus(PlantSensorData data)
        {
            data.TemperatureDeviation = CalculateDeviation(data.ActualTemperature, data.TargetTemperature);
            data.IsTemperatureInRange = IsWithinTolerance(data.ActualTemperature, data.TargetTemperature, data.TolerancePercentage);

            data.HumidityDeviation = CalculateDeviation(data.ActualHumidity, data.TargetHumidity);
            data.IsHumidityInRange = IsWithinTolerance(data.ActualHumidity, data.TargetHumidity, data.TolerancePercentage);

            data.LightIntensityDeviation = CalculateDeviation(data.ActualLightIntensity, data.TargetLightIntensity);
            data.IsLightIntensityInRange = IsWithinTolerance(data.ActualLightIntensity, data.TargetLightIntensity, data.TolerancePercentage);

            data.PhLevelDeviation = CalculateDeviation(data.ActualPhLevel, data.TargetPhLevel);
            data.IsPhLevelInRange = IsWithinTolerance(data.ActualPhLevel, data.TargetPhLevel, data.TolerancePercentage);
        }

        private double CalculateDeviation(double actual, double target)
        {
            if (target == 0) return 0;
            return Math.Round(((actual - target) / target) * 100, 2);
        }

        private bool IsWithinTolerance(double actual, double target, double tolerancePercentage)
        {
            if (target == 0) return actual == 0;

            var deviation = Math.Abs((actual - target) / target) * 100;
            return deviation <= tolerancePercentage;
        }
    }
}