using IndoorFarmMonitoring.Models;
using IndoorFarmMonitoring.Services;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace IndoorFarmMonitoring.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class PlantSensorController : ControllerBase
    {
        private readonly IPlantSensorService _plantSensorService;
        private readonly IDataStorageService _dataStorageService;
        private readonly ILogger<PlantSensorController> _logger;

        public PlantSensorController(
            IPlantSensorService plantSensorService,
            IDataStorageService dataStorageService,
            ILogger<PlantSensorController> logger)
        {
            _plantSensorService = plantSensorService;
            _dataStorageService = dataStorageService;
            _logger = logger;
        }

        /// <summary>
        /// Fetches sensor data and plant configurations, combines them, and stores the result
        /// </summary>
        /// <returns>Combined plant sensor data</returns>
        [HttpGet("plant-sensor-data")]
        [ProducesResponseType(typeof(ApiResponse<List<PlantSensorData>>), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), (int)HttpStatusCode.InternalServerError)]
        [ProducesResponseType(typeof(ApiResponse<object>), (int)HttpStatusCode.RequestTimeout)]
        public async Task<ActionResult<ApiResponse<List<PlantSensorData>>>> GetPlantSensorData()
        {
            try
            {
                _logger.LogInformation("Received request for plant sensor data");

                var data = await _plantSensorService.GetPlantSensorDataAsync();

                var response = new ApiResponse<List<PlantSensorData>>
                {
                    Success = true,
                    Message = $"Successfully retrieved and stored data for {data.Count} trays",
                    Data = data
                };

                _logger.LogInformation("Successfully processed plant sensor data request");
                return Ok(response);
            }
            catch (TimeoutException ex)
            {
                _logger.LogError(ex, "Timeout occurred while processing plant sensor data");

                var errorResponse = new ApiResponse<object>
                {
                    Success = false,
                    Message = "Request timed out while fetching external API data",
                    Data = new ApiError
                    {
                        Code = "TIMEOUT_ERROR",
                        Message = "The request to external APIs timed out",
                        Details = ex.Message
                    }
                };

                return StatusCode((int)HttpStatusCode.RequestTimeout, errorResponse);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP error occurred while processing plant sensor data");

                var errorResponse = new ApiResponse<object>
                {
                    Success = false,
                    Message = "Failed to fetch data from external APIs",
                    Data = new ApiError
                    {
                        Code = "EXTERNAL_API_ERROR",
                        Message = "Error communicating with external APIs",
                        Details = ex.Message
                    }
                };

                return StatusCode((int)HttpStatusCode.BadGateway, errorResponse);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Data processing error occurred");

                var errorResponse = new ApiResponse<object>
                {
                    Success = false,
                    Message = "Failed to process the received data",
                    Data = new ApiError
                    {
                        Code = "DATA_PROCESSING_ERROR",
                        Message = "Error processing data from external APIs",
                        Details = ex.Message
                    }
                };

                return StatusCode((int)HttpStatusCode.UnprocessableEntity, errorResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred while processing plant sensor data");

                var errorResponse = new ApiResponse<object>
                {
                    Success = false,
                    Message = "An unexpected error occurred",
                    Data = new ApiError
                    {
                        Code = "INTERNAL_ERROR",
                        Message = "An unexpected error occurred while processing the request",
                        Details = ex.Message
                    }
                };

                return StatusCode((int)HttpStatusCode.InternalServerError, errorResponse);
            }
        }

        /// <summary>
        /// Gets all stored plant sensor data
        /// </summary>
        /// <returns>All stored plant sensor data</returns>
        [HttpGet("stored-data")]
        [ProducesResponseType(typeof(ApiResponse<List<PlantSensorData>>), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), (int)HttpStatusCode.InternalServerError)]
        public async Task<ActionResult<ApiResponse<List<PlantSensorData>>>> GetStoredData()
        {
            try
            {
                _logger.LogInformation("Received request for stored plant sensor data");

                var data = await _dataStorageService.GetAllPlantSensorDataAsync();

                var response = new ApiResponse<List<PlantSensorData>>
                {
                    Success = true,
                    Message = $"Successfully retrieved {data.Count} stored records",
                    Data = data
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving stored plant sensor data");

                var errorResponse = new ApiResponse<object>
                {
                    Success = false,
                    Message = "Failed to retrieve stored data",
                    Data = new ApiError
                    {
                        Code = "STORAGE_ERROR",
                        Message = "Error retrieving data from storage",
                        Details = ex.Message
                    }
                };

                return StatusCode((int)HttpStatusCode.InternalServerError, errorResponse);
            }
        }

        /// <summary>
        /// Gets plant sensor data for a specific tray
        /// </summary>
        /// <param name="trayId">The tray ID to search for</param>
        /// <returns>Plant sensor data for the specified tray</returns>
        [HttpGet("tray/{trayId}")]
        [ProducesResponseType(typeof(ApiResponse<PlantSensorData>), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), (int)HttpStatusCode.NotFound)]
        [ProducesResponseType(typeof(ApiResponse<object>), (int)HttpStatusCode.InternalServerError)]
        public async Task<ActionResult<ApiResponse<PlantSensorData>>> GetPlantSensorDataByTrayId(string trayId)
        {
            try
            {
                _logger.LogInformation("Received request for plant sensor data for tray {TrayId}", trayId);

                var data = await _dataStorageService.GetPlantSensorDataByTrayIdAsync(trayId);

                if (data == null)
                {
                    var notFoundResponse = new ApiResponse<object>
                    {
                        Success = false,
                        Message = $"No data found for tray {trayId}",
                        Data = new ApiError
                        {
                            Code = "NOT_FOUND",
                            Message = $"No plant sensor data found for tray ID: {trayId}"
                        }
                    };

                    return NotFound(notFoundResponse);
                }

                var response = new ApiResponse<PlantSensorData>
                {
                    Success = true,
                    Message = $"Successfully retrieved data for tray {trayId}",
                    Data = data
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving plant sensor data for tray {TrayId}", trayId);

                var errorResponse = new ApiResponse<object>
                {
                    Success = false,
                    Message = $"Failed to retrieve data for tray {trayId}",
                    Data = new ApiError
                    {
                        Code = "STORAGE_ERROR",
                        Message = "Error retrieving data from storage",
                        Details = ex.Message
                    }
                };

                return StatusCode((int)HttpStatusCode.InternalServerError, errorResponse);
            }
        }

        /// <summary>
        /// Health check endpoint
        /// </summary>
        /// <returns>API health status</returns>
        [HttpGet("health")]
        [ProducesResponseType(typeof(object), (int)HttpStatusCode.OK)]
        public IActionResult Health()
        {
            return Ok(new
            {
                Status = "Healthy",
                Timestamp = DateTime.UtcNow,
                Version = "1.0.0"
            });
        }
    }
}