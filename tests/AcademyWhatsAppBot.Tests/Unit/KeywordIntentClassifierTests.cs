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
    [InlineData("¡Holaaa! 👋")]
    [InlineData("Buenas tardes")]
    [InlineData("hola, buenas noches")]
    [InlineData("Muchas gracias!")]
    [InlineData("ok gracias 🙏")]
    public void WholeGreetingsAndThanks_AreGreeting(string text)
    {
        Assert.Equal(Intent.Greeting, KeywordIntentClassifier.Classify(text));
    }

    [Fact]
    public void GreetingWithBalanceKeyword_IsBalance()
    {
        Assert.Equal(Intent.Balance, KeywordIntentClassifier.Classify("hola, ¿me dices mi saldo? gracias"));
    }

    [Theory]
    [InlineData("hola, una pregunta sobre los horarios")]
    [InlineData("¿a qué hora es el entrenamiento?")]
    [InlineData("")]
    [InlineData(null)]
    public void OtherTexts_AreUnknown(string? text)
    {
        Assert.Equal(Intent.Unknown, KeywordIntentClassifier.Classify(text));
    }
}
