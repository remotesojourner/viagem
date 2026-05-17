using System.ComponentModel.DataAnnotations;

namespace Viagem.Data.Models;

public class SiteSetting
{
    [Key, MaxLength(100)]
    public string Key { get; set; } = "";

    public string Value { get; set; } = "{}";
}
