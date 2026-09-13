namespace Temperature.Api.Models;

public class TemperatureReading
{
    public int Id { get; set; }
    public string SensorId { get; set; } = null!;
    public string SensorType { get; set; } = "temperature";
    public string Location { get; set; } = null!;
    public double Value { get; set; }
    public string Unit { get; set; } = "°C";
    public string Status { get; set; } = "active";
    public string Description { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}
