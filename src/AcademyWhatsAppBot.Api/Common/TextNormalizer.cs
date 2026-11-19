using System.Globalization;
using System.Text;

namespace AcademyWhatsAppBot.Api.Common;

public static class TextNormalizer
{
    /// <summary>
    /// Lowercase, without accents and with single spaces, e.g. "  ¿Cuánto   DEBO? " → "¿cuanto debo?".
    /// </summary>
    public static string Normalize(string text)
    {
        var decomposed = text.ToLowerInvariant().Normalize(NormalizationForm.FormD);

        var builder = new StringBuilder(decomposed.Length);
        foreach (var c in decomposed)
        {
            // FormD splits "á" into "a" + combining accent; drop the accent.
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(c);
            }
        }

        var words = builder.ToString().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        return string.Join(' ', words).Normalize(NormalizationForm.FormC);
    }
}
