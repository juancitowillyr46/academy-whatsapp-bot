using System.Globalization;

namespace AcademyWhatsAppBot.Api.Common;

/// <summary>
/// User-facing formats for Colombia (es-CO).
/// </summary>
public static class Formatters
{
    private static readonly CultureInfo Colombia = CultureInfo.GetCultureInfo("es-CO");

    /// <summary>
    /// Colombian pesos without decimals, e.g. 120000 → "$120.000".
    /// </summary>
    public static string Currency(decimal amount) => "$" + amount.ToString("N0", Colombia);

    /// <summary>
    /// Day and month in Spanish, e.g. 2026-12-05 → "5 de diciembre".
    /// </summary>
    public static string Date(DateOnly date) => date.ToString("d 'de' MMMM", Colombia);
}
