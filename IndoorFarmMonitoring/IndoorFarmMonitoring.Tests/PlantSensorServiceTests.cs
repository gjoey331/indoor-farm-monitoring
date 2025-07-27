using IndoorFarmMonitoring.Models;
using IndoorFarmMonitoring.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace IndoorFarmMonitoring.Tests
{
    public class PlantSensorServiceTests
    {
        private readonly Mock<IExternalApiService> _mockExternalApiService;
        private readonly Mock<IDataStorageService> _mockDataStorageService;
        private readonly Mock<ILogger<PlantSensorService>> _mockLogger;
        private readonly PlantSensorService _service;

        public PlantSensorServiceTests()
        {
            _mockExternalApiService = new Mock<IExternalApiService>();
            _mockDataStorageService = new Mock<IDataStorageService>();
            _mockLogger = new Mock<ILogger<PlantSensorService>>();
            _service = new PlantSensorService(_mockExternalApiService.Object, _mockDataStorageService.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task GetPlantSensorDataAsync_ShouldCombineDataCorrectly()
        {
            var sensorReadings = new List<SensorReading>
            {
                new SensorReading
                {
                    TrayIdValue = "TRAY001",
                    Timestamp = DateTime.UtcNow,
                    Temperature = 25.5,
                    Humidity = 60.0,
                    LightIntensity = 800.0,
                    PhLevel = 6.5
                }
            };

            var plantConfigurations = new List<PlantConfiguration>
            {
                new PlantConfiguration
                {
                    TrayIdValue = "TRAY001",
                    PlantType = "Lettuce",
                    TargetTemperature = 24.0,
                    TargetHumidity = 65.0,
                    TargetLightIntensity = 750.0,
                    TargetPhLevel = 6.0,
                    TolerancePercentage = 10.0
                }
            };

            _mockExternalApiService.Setup(x => x.GetSensorReadingsAsync())
                .ReturnsAsync(sensorReadings);
            _mockExternalApiService.Setup(x => x.GetPlantConfigurationsAsync())
                .ReturnsAsync(plantConfigurations);
            _mockDataStorageService.Setup(x => x.SavePlantSensorDataAsync(It.IsAny<List<PlantSensorData>>()))
                .Returns(Task.CompletedTask);

            var result = await _service.GetPlantSensorDataAsync();

            Assert.Single(result);
            var combinedData = result.First();
            Assert.Equal("TRAY001", combinedData.TrayId);
            Assert.Equal("Lettuce", combinedData.PlantType);
            Assert.Equal(25.5, combinedData.ActualTemperature);
            Assert.Equal(24.0, combinedData.TargetTemperature);

            _mockDataStorageService.Verify(x => x.SavePlantSensorDataAsync(It.IsAny<List<PlantSensorData>>()), Times.Once);
        }

        [Fact]
        public async Task GetPlantSensorDataAsync_ShouldCalculateDeviationsCorrectly()
        {
            var sensorReadings = new List<SensorReading>
            {
                new SensorReading
                {
                    TrayIdValue = "TRAY001",
                    Timestamp = DateTime.UtcNow,
                    Temperature = 26.4,
                    Humidity = 60.0,
                    LightIntensity = 800.0,
                    PhLevel = 6.5
                }
            };

            var plantConfigurations = new List<PlantConfiguration>
            {
                new PlantConfiguration
                {
                    TrayIdValue = "TRAY001",
                    PlantType = "Lettuce",
                    TargetTemperature = 24.0,
                    TargetHumidity = 65.0,
                    TargetLightIntensity = 750.0,
                    TargetPhLevel = 6.0,
                    TolerancePercentage = 5.0 
                }
            };

            _mockExternalApiService.Setup(x => x.GetSensorReadingsAsync())
                .ReturnsAsync(sensorReadings);
            _mockExternalApiService.Setup(x => x.GetPlantConfigurationsAsync())
                .ReturnsAsync(plantConfigurations);
            _mockDataStorageService.Setup(x => x.SavePlantSensorDataAsync(It.IsAny<List<PlantSensorData>>()))
                .Returns(Task.CompletedTask);

            var result = await _service.GetPlantSensorDataAsync();

            var combinedData = result.First();
            Assert.Equal(10.0, combinedData.TemperatureDeviation, 1); 
            Assert.False(combinedData.IsTemperatureInRange);
            Assert.False(combinedData.IsAllMetricsInRange);
        }

        [Fact]
        public async Task GetPlantSensorDataAsync_ShouldHandleMissingConfiguration()
        {
            var sensorReadings = new List<SensorReading>
            {
                new SensorReading { TrayIdValue = "TRAY001", Timestamp = DateTime.UtcNow },
                new SensorReading { TrayIdValue = "TRAY002", Timestamp = DateTime.UtcNow }
            };

            var plantConfigurations = new List<PlantConfiguration>
            {
                new PlantConfiguration { TrayIdValue = "TRAY001", PlantType = "Lettuce" }
            };

            _mockExternalApiService.Setup(x => x.GetSensorReadingsAsync())
                .ReturnsAsync(sensorReadings);
            _mockExternalApiService.Setup(x => x.GetPlantConfigurationsAsync())
                .ReturnsAsync(plantConfigurations);
            _mockDataStorageService.Setup(x => x.SavePlantSensorDataAsync(It.IsAny<List<PlantSensorData>>()))
                .Returns(Task.CompletedTask);

            var result = await _service.GetPlantSensorDataAsync();

            Assert.Single(result); 
            Assert.Equal("TRAY001", result.First().TrayId);
        }

        [Fact]
        public async Task GetPlantSensorDataAsync_ShouldHandleExternalApiException()
        {
            _mockExternalApiService.Setup(x => x.GetSensorReadingsAsync())
                .ThrowsAsync(new HttpRequestException("API error"));

            await Assert.ThrowsAsync<HttpRequestException>(() => _service.GetPlantSensorDataAsync());
        }
    }

    public class InMemoryStorageServiceTests
    {
        private readonly Mock<ILogger<InMemoryStorageService>> _mockLogger;
        private readonly InMemoryStorageService _service;

        public InMemoryStorageServiceTests()
        {
            _mockLogger = new Mock<ILogger<InMemoryStorageService>>();
            _service = new InMemoryStorageService(_mockLogger.Object);
        }

        [Fact]
        public async Task SaveAndRetrievePlantSensorData_ShouldWorkCorrectly()
        {
            var testData = new List<PlantSensorData>
            {
                new PlantSensorData
                {
                    TrayId = "TRAY001",
                    PlantType = "Lettuce",
                    Timestamp = DateTime.UtcNow,
                    ActualTemperature = 25.0,
                    TargetTemperature = 24.0
                }
            };

            await _service.SavePlantSensorDataAsync(testData);
            var result = await _service.GetAllPlantSensorDataAsync();

            Assert.Single(result);
            Assert.Equal("TRAY001", result.First().TrayId);
            Assert.Equal("Lettuce", result.First().PlantType);
        }

        [Fact]
        public async Task GetPlantSensorDataByTrayId_ShouldReturnCorrectData()
        {
            var testData = new List<PlantSensorData>
            {
                new PlantSensorData { TrayId = "TRAY001", PlantType = "Lettuce", Timestamp = DateTime.UtcNow },
                new PlantSensorData { TrayId = "TRAY002", PlantType = "Spinach", Timestamp = DateTime.UtcNow }
            };

            await _service.SavePlantSensorDataAsync(testData);

            var result = await _service.GetPlantSensorDataByTrayIdAsync("TRAY002");

            Assert.NotNull(result);
            Assert.Equal("TRAY002", result.TrayId);
            Assert.Equal("Spinach", result.PlantType);
        }

        [Fact]
        public async Task GetPlantSensorDataByTrayId_ShouldReturnNullForNonExistentTray()
        {
            var result = await _service.GetPlantSensorDataByTrayIdAsync("NONEXISTENT");

            Assert.Null(result);
        }
    }
}