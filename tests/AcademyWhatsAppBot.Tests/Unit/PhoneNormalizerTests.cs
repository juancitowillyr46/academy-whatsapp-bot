using AcademyWhatsAppBot.Api.Common;

namespace AcademyWhatsAppBot.Tests.Unit;

public class PhoneNormalizerTests
{
    [Theory]
    [InlineData("573001234567", "573001234567")]
    [InlineData("+57 300 123 4567", "573001234567")]
    [InlineData("300-123-4567", "573001234567")]
    [InlineData("3001234567", "573001234567")]
    [InlineData("+1 555 184 6162", "15551846162")]
    public void Normalize_ReturnsDigitsWithCountryCode(string input, string expected)
    {
        Assert.Equal(expected, PhoneNormalizer.Normalize(input));
    }

    [Fact]
    public void Mask_HidesMiddleDigits()
    {
        Assert.Equal("57300***4567", PhoneNormalizer.Mask("573001234567"));
    }

    [Fact]
    public void Mask_HidesShortNumbersCompletely()
    {
        Assert.Equal("***", PhoneNormalizer.Mask("12345"));
    }
}
