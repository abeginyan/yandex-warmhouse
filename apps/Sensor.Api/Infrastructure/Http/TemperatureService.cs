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

public interface ITemperatureService
{
    Task<TemperatureResponse> GetTemperatureAsync(string location, CancellationToken ct = default);
    Task<TemperatureResponse?> GetTemperatureByIdAsync(string sensorId, CancellationToken ct = default);
}

public class TemperatureService(HttpClient httpClient) : ITemperatureService
{
    // Snake_case options used for HttpClient JSON (not automatically inheriting app-level JsonOptions)
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
}
