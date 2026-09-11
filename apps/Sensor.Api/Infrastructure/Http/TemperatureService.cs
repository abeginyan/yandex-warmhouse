using System.Net;
using System.Text.Json;

namespace Sensor.Api.Infrastructure.Http;

public record TemperatureResponse(
    double Value,
    string Unit,
    DateTime Timestamp,
    string Location,
    string Status,
    string SensorId,
    string SensorType,
    string Description
);

public record CreateTemperatureReadingRequest(
    string SensorId,
    string Location,
    double Value,
    string SensorType,
    string Unit,
    string Status,
    string Description
);

public record UpdateTemperatureReadingRequest(
    double Value,
    string Location,
    string SensorType,
    string Unit,
    string Status,
    string Description
);

public interface ITemperatureService
{
    Task<TemperatureResponse> GetTemperatureAsync(string location, CancellationToken ct = default);
    Task<TemperatureResponse?> GetTemperatureByIdAsync(string sensorId, CancellationToken ct = default);
    Task CreateReadingAsync(CreateTemperatureReadingRequest request, CancellationToken ct = default);
    Task UpdateReadingAsync(string sensorId, UpdateTemperatureReadingRequest request, CancellationToken ct = default);
    Task DeleteBySensorIdAsync(string sensorId, CancellationToken ct = default);
}

public class TemperatureService(HttpClient httpClient) : ITemperatureService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    public async Task<TemperatureResponse> GetTemperatureAsync(string location, CancellationToken ct = default)
    {
        var response = await httpClient.GetAsync($"temperature?location={location}", ct);

        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"unexpected status code: {(int)response.StatusCode}");

        var result = await response.Content.ReadFromJsonAsync<TemperatureResponse>(JsonOptions, ct);
        if (result is null)
            throw new InvalidOperationException("error decoding temperature response");

        return result;
    }

    public async Task<TemperatureResponse?> GetTemperatureByIdAsync(string sensorId, CancellationToken ct = default)
    {
        var response = await httpClient.GetAsync($"temperature/{sensorId}", ct);

        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"unexpected status code: {(int)response.StatusCode}");

        return await response.Content.ReadFromJsonAsync<TemperatureResponse>(JsonOptions, ct);
    }

    public async Task CreateReadingAsync(CreateTemperatureReadingRequest request, CancellationToken ct = default)
    {
        var response = await httpClient.PostAsJsonAsync("temperature", request, JsonOptions, ct);

        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"unexpected status code: {(int)response.StatusCode}");
    }

    public async Task UpdateReadingAsync(string sensorId, UpdateTemperatureReadingRequest request, CancellationToken ct = default)
    {
        var response = await httpClient.PutAsJsonAsync($"temperature/{sensorId}", request, JsonOptions, ct);

        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"unexpected status code: {(int)response.StatusCode}");
    }

    public async Task DeleteBySensorIdAsync(string sensorId, CancellationToken ct = default)
    {
        var response = await httpClient.DeleteAsync($"temperature/{sensorId}", ct);

        // 404 is acceptable — sensor may never have had a temperature reading
        if (!response.IsSuccessStatusCode && response.StatusCode != HttpStatusCode.NotFound)
            throw new HttpRequestException($"unexpected status code: {(int)response.StatusCode}");
    }
}
