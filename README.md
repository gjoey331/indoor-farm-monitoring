# Indoor Farm Monitoring System

A Web API project for monitoring indoor farm sensor data and plant configurations. 
This system fetches data from external APIs, combines and processes the information, and stores it using configurable storage options.

### Prerequisites

- .NET 8.0 SDK or later
- PostgreSQL (optional, can configure as JSON or in-memory storage type if you don't want to use PostgreSQL)
- Git

### Installation

1. **Clone the repository**
   ```bash
   git clone <repository-url>
   cd IndoorFarmMonitoring
   ```

2. **Restore dependencies**
   ```bash
   dotnet restore
   ```

3. **Run the application**
   ```bash
   dotnet run
   ```

4. **Access the API**
   - API Base URL: `https://localhost:7000` (or `http://localhost:5000`)
   - Swagger UI: `https://localhost:7000/swagger`

## Configuration

### Storage Options

Configure the storage type in `appsettings.json`:

#### 1. In-Memory Storage (Default)
```json
{
  "StorageType": "InMemory"
}
```

#### 2. JSON File Storage
```json
{
  "StorageType": "JSON",
  "JsonFilePath": "plant_data.json"
}
```

#### 3. PostgreSQL Storage
```json
{
  "StorageType": "PostgreSQL",
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=IndoorFarmDb;Username=postgres;Password=yourpassword"
  }
}
```

## API Endpoints

### 1. Get Plant Sensor Data
**Endpoint**: `GET /api/PlantSensor/plant-sensor-data`

Fetches data from external APIs, combines them, and stores the result.


### 2. Get Stored Data
**Endpoint**: `GET /api/PlantSensor/stored-data`

Retrieves all stored plant sensor data.

### 3. Get Data by Tray ID
**Endpoint**: `GET /api/PlantSensor/tray/{trayId}`

Retrieves the latest data for a specific tray.

**Example**: `GET /api/PlantSensor/tray/TRAY001`

### 4. Health Check
**Endpoint**: `GET /api/PlantSensor/health`

Returns API health status.

## Development

### Running Tests

```bash
# Run all tests
dotnet test

# Run specific test class
dotnet test --filter "PlantSensorServiceTests"
```

## Architecture Overview

The application follows clean architecture principles:

- **Controllers**: Handle HTTP requests and responses
- **Services**: Contain business logic and orchestrate data flow
- **Data**: Handle data persistence and retrieval
- **Models**: Define data structures and DTOs

### Key Design Patterns

- **Dependency Injection**: All services are registered and injected
- **Repository Pattern**: Abstracted data storage through `IDataStorageService`
- **Strategy Pattern**: Multiple storage implementations (PostgreSQL, JSON, In-Memory)
- **Async/Await**: Non-blocking operations throughout the application

## Error Handling

The API provides comprehensive error handling with specific error codes:

- **TIMEOUT_ERROR**: Request timeout (HTTP 408)
- **EXTERNAL_API_ERROR**: External API communication failure (HTTP 502)
- **DATA_PROCESSING_ERROR**: Data parsing/processing failure (HTTP 422)
- **STORAGE_ERROR**: Database or file storage error (HTTP 500)
- **NOT_FOUND**: Resource not found (HTTP 404)
- **INTERNAL_ERROR**: Unexpected server error (HTTP 500)

## Monitoring and Logging

The application includes comprehensive logging:

- **API Latency**: Tracks external API response times
- **Error Logging**: Detailed error information with stack traces
- **Request Tracking**: Logs all incoming requests and their outcomes
- **Performance Metrics**: Monitors data processing performance

## External APIs

The system integrates with these external services:

- **Sensor Data API**: `http://3.0.148.231:8010/sensor-readings`
  - Swagger: `http://3.0.148.231:8010/docs`
- **Plant Configuration API**: `http://3.0.148.231:8020/plant-configurations`
  - Swagger: `http://3.0.148.231:8020/docs`


