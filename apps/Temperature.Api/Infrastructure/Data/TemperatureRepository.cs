using Microsoft.EntityFrameworkCore;
using Temperature.Api.Models;

namespace Temperature.Api.Infrastructure.Data;

public interface ITemperatureRepository
{
    Task<TemperatureReading?> GetLatestBySensorIdAsync(string sensorId, CancellationToken ct = default);
    Task<TemperatureReading?> GetLatestByLocationAsync(string location, CancellationToken ct = default);
    Task<List<TemperatureReading>> GetAllAsync(CancellationToken ct = default);
    Task<TemperatureReading> CreateAsync(TemperatureReading reading, CancellationToken ct = default);
    Task<TemperatureReading?> UpdateLatestBySensorIdAsync(string sensorId, Action<TemperatureReading> apply, CancellationToken ct = default);
    Task<int> DeleteBySensorIdAsync(string sensorId, CancellationToken ct = default);
}

public class TemperatureRepository(TemperatureDbContext context) : ITemperatureRepository
{
    public async Task<TemperatureReading?> GetLatestBySensorIdAsync(string sensorId, CancellationToken ct = default)
    {
        //var temp = await context.Readings
        //            .AsNoTracking()
        //            .Where(r => r.SensorId == sensorId)
        //            .OrderByDescending(r => r.Timestamp)
        //            .FirstOrDefaultAsync(ct);

        var temp = new TemperatureReading
        {
            SensorId = sensorId
        };

        SetDefaulValues(temp?.Location, temp?.SensorId ?? "", temp);

        return temp;
    }

    public async Task<TemperatureReading?> GetLatestByLocationAsync(string location, CancellationToken ct = default)
    {
        //var temp = await context.Readings
        //        .AsNoTracking()
        //        .Where(r => r.Location == location)
        //        .OrderByDescending(r => r.Timestamp)
        //        .FirstOrDefaultAsync(ct);

        var temp = new TemperatureReading
        {
            Location = location
        };

        SetDefaulValues(location, temp?.SensorId ?? "", temp);

        return temp;
    }

    public async Task<List<TemperatureReading>> GetAllAsync(CancellationToken ct = default)
    {
        var temps = await context.Readings
                .AsNoTracking()
                .OrderByDescending(r => r.Timestamp)
                .ToListAsync(ct);

        temps.ForEach(temp => SetDefaulValues(temp.Location, temp.SensorId, temp));
        return temps;
    }

    public async Task<TemperatureReading> CreateAsync(TemperatureReading reading, CancellationToken ct = default)
    {
        context.Readings.Add(reading);
        await context.SaveChangesAsync(ct);
        return reading;
    }

    public async Task<TemperatureReading?> UpdateLatestBySensorIdAsync(
        string sensorId, Action<TemperatureReading> apply, CancellationToken ct = default)
    {
        var reading = await context.Readings
            .Where(r => r.SensorId == sensorId)
            .OrderByDescending(r => r.Timestamp)
            .FirstOrDefaultAsync(ct);

        if (reading is null) return null;

        apply(reading);
        await context.SaveChangesAsync(ct);
        return reading;
    }

    public Task<int> DeleteBySensorIdAsync(string sensorId, CancellationToken ct = default) =>
        context.Readings
            .Where(r => r.SensorId == sensorId)
            .ExecuteDeleteAsync(ct);

    private static void SetDefaulValues(string location, string sensorId, TemperatureReading? temp)
    {
        if (temp is null) return;

        temp.Value = Math.Round(Random.Shared.NextDouble() * 100, 2);
        if (location == "")
        {
            temp.Location = temp.SensorId switch
            {
                "1" => "Living Room",
                "2" => "Bedroom",
                "3" => "Kitchen",
                _ => "Unknown"
            };
        }

        if (sensorId == "")
        {
            temp.SensorId = temp.SensorId switch
            {
                "Living Room" => "1",
                "Bedroom" => "2",
                "Kitchen" => "3",
                _ => "Unknown"
            };
        }
    }
}
