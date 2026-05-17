namespace Viagem.Data.Models;

public class Airport
{
    public int Id { get; set; }
    public string IataCode { get; set; } = "";
    public string? IcaoCode { get; set; }
    public string Name { get; set; } = "";
    public string? Municipality { get; set; }
    public string? IsoCountry { get; set; }
    public string? Type { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
}
