using TimeZoneConverter;

namespace Viagem.Common;

public record CurrencyItem(string Code, string Flag, string Name);

public static class AppConstants
{
    public static readonly IReadOnlyList<CurrencyItem> Currencies =
    [
        new("AED", "🇦🇪", "UAE Dirham"), new("AUD", "🇦🇺", "Australian Dollar"), new("BRL", "🇧🇷", "Brazilian Real"),
        new("CAD", "🇨🇦", "Canadian Dollar"), new("CHF", "🇨🇭", "Swiss Franc"), new("CNY", "🇨🇳", "Chinese Yuan"),
        new("CZK", "🇨🇿", "Czech Koruna"), new("DKK", "🇩🇰", "Danish Krone"), new("EUR", "🇪🇺", "Euro"),
        new("GBP", "🇬🇧", "British Pound"), new("HKD", "🇭🇰", "Hong Kong Dollar"), new("HUF", "🇭🇺", "Hungarian Forint"),
        new("IDR", "🇮🇩", "Indonesian Rupiah"), new("ILS", "🇮🇱", "Israeli Shekel"), new("INR", "🇮🇳", "Indian Rupee"),
        new("JPY", "🇯🇵", "Japanese Yen"), new("KRW", "🇰🇷", "South Korean Won"), new("MXN", "🇲🇽", "Mexican Peso"),
        new("MYR", "🇲🇾", "Malaysian Ringgit"), new("NOK", "🇳🇴", "Norwegian Krone"), new("NZD", "🇳🇿", "New Zealand Dollar"),
        new("PHP", "🇵🇭", "Philippine Peso"), new("PLN", "🇵🇱", "Polish Zloty"), new("RON", "🇷🇴", "Romanian Leu"),
        new("SAR", "🇸🇦", "Saudi Riyal"), new("SEK", "🇸🇪", "Swedish Krona"), new("SGD", "🇸🇬", "Singapore Dollar"),
        new("THB", "🇹🇭", "Thai Baht"), new("TRY", "🇹🇷", "Turkish Lira"), new("TWD", "🇹🇼", "New Taiwan Dollar"),
        new("UAH", "🇺🇦", "Ukrainian Hryvnia"), new("USD", "🇺🇸", "US Dollar"), new("VND", "🇻🇳", "Vietnamese Dong"),
        new("ZAR", "🇿🇦", "South African Rand")
    ];

    private static readonly string[] ValidTzPrefixes = 
    [
        "Africa/", "America/", "Antarctica/", "Arctic/", "Asia/", 
        "Atlantic/", "Australia/", "Europe/", "Indian/", "Pacific/"
    ];

    public static readonly IReadOnlyList<string> Timezones = TZConvert.KnownIanaTimeZoneNames
        .Where(tz => ValidTzPrefixes.Any(tz.StartsWith))
        .Where(tz => !tz.EndsWith("/Belfast") && 
                     !tz.EndsWith("/Rosario") &&
                     !tz.EndsWith("/Knox_IN") &&
                     !tz.Contains("Argentina/ComodRivadavia") &&
                     !tz.EndsWith("/Saigon") &&
                     !tz.EndsWith("/Calcutta") &&
                     !tz.EndsWith("/Macao") &&
                     !tz.EndsWith("/Katmandu") &&
                     !tz.EndsWith("/Tiraspol"))
        .OrderBy(tz => tz)
        .ToList();
}
