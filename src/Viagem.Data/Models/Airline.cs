namespace Viagem.Data.Models;

public class Airline
{
    public int Id { get; set; }
    public string Code { get; set; } = "";   // IATA code (2-letter)
    public string Name { get; set; } = "";
    public string? LogoUrl { get; set; }
}
