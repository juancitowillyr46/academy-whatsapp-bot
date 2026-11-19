using AcademyWhatsAppBot.Api.Intents;

namespace AcademyWhatsAppBot.Tests.Unit;

public class KeywordIntentClassifierTests
{
    [Theory]
    [InlineData("saldo")]
    [InlineData("SALDO")]
    [InlineData("Hola, ¿cuál es mi saldo?")]
    [InlineData("¿Cuánto debo?")]
    [InlineData("tengo alguna deuda")]
    public void BalanceKeywords_AreBalance(string text)
    {
        Assert.Equal(Intent.Balance, KeywordIntentClassifier.Classify(text));
    }

    [Theory]
    [InlineData("hola")]
    [InlineData("¿a qué hora es el entrenamiento?")]
    [InlineData("")]
    [InlineData(null)]
    public void OtherTexts_AreUnknown(string? text)
    {
        Assert.Equal(Intent.Unknown, KeywordIntentClassifier.Classify(text));
    }
}
