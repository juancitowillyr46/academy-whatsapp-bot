using AcademyWhatsAppBot.Api.Common;

namespace AcademyWhatsAppBot.Tests.Unit;

public class TextNormalizerTests
{
    [Theory]
    [InlineData("SALDO", "saldo")]
    [InlineData("  ¿Cuánto   DEBO? ", "¿cuanto debo?")]
    [InlineData("Hola,\n¿cuál es mi saldo?", "hola, ¿cual es mi saldo?")]
    [InlineData("", "")]
    public void Normalize_LowercasesRemovesAccentsAndCollapsesSpaces(string input, string expected)
    {
        Assert.Equal(expected, TextNormalizer.Normalize(input));
    }
}
