using AcademyWhatsAppBot.Api.Common;

namespace AcademyWhatsAppBot.Tests.Unit;

public class FormattersTests
{
    [Theory]
    [InlineData(120000, "$120.000")]
    [InlineData(1500000, "$1.500.000")]
    [InlineData(0, "$0")]
    public void Currency_UsesColombianPesosWithoutDecimals(decimal amount, string expected)
    {
        Assert.Equal(expected, Formatters.Currency(amount));
    }

    [Fact]
    public void Date_UsesSpanishDayAndMonth()
    {
        Assert.Equal("5 de diciembre", Formatters.Date(new DateOnly(2026, 12, 5)));
    }
}
