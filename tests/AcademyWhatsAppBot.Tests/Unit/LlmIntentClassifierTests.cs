using AcademyWhatsAppBot.Api.Intents;

using Microsoft.Extensions.AI;

namespace AcademyWhatsAppBot.Tests.Unit;

public class LlmIntentClassifierTests
{
    [Theory]
    [InlineData("""{"intent":"balance"}""", Intent.Balance)]
    [InlineData("""{"intent":"handoff"}""", Intent.Handoff)]
    [InlineData("""{"intent":"greeting"}""", Intent.Greeting)]
    [InlineData("""{"intent":"off_topic"}""", Intent.OffTopic)]
    public void ParseIntent_MapsKnownValues(string json, Intent expected)
    {
        Assert.Equal(expected, LlmIntentClassifier.ParseIntent(json));
    }

    [Theory]
    [InlineData("""{"intent":"transfer_money"}""")]
    [InlineData("""{"intent":42}""")]
    [InlineData("""{"other":"balance"}""")]
    [InlineData("balance")]
    [InlineData("")]
    public void ParseIntent_TreatsAnythingElseAsUnknown(string json)
    {
        Assert.Equal(Intent.Unknown, LlmIntentClassifier.ParseIntent(json));
    }

    [Fact]
    public async Task Classify_AsksForJsonConstrainedToTheIntents()
    {
        var model = FakeChatClient.Returning("""{"intent":"handoff"}""");

        var intent = await new LlmIntentClassifier(model).ClassifyAsync("ya pagué");

        Assert.Equal(Intent.Handoff, intent);
        var format = Assert.IsType<ChatResponseFormatJson>(model.LastOptions?.ResponseFormat);
        var schema = format.Schema!.Value.ToString();
        Assert.Contains("\"balance\"", schema);
        Assert.Contains("\"off_topic\"", schema);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    public async Task Classify_DoesNotCallTheModelWithoutText(string? text)
    {
        var model = FakeChatClient.Returning("""{"intent":"balance"}""");

        Assert.Equal(Intent.Unknown, await new LlmIntentClassifier(model).ClassifyAsync(text));
        Assert.Equal(0, model.Calls);
    }
}
