using Registry.Domain;
using Registry.Domain.Catalogs;

namespace Registry.UnitTests.Catalogs;

public class PartialDateTests
{
    [Theory]
    [InlineData("2020", 2020, null, null)]
    [InlineData("2020-05", 2020, 5, null)]
    [InlineData(" 2020-05-17 ", 2020, 5, 17)]
    public void Year_month_and_day_are_optional_from_the_right(string text, int year, int? month, int? day)
    {
        var date = PartialDate.Parse(text);

        Assert.Equal((year, month, day), (date.Year, date.Month, date.Day));
        Assert.Equal(text.Trim(), date.ToString());
    }

    [Theory]
    [InlineData("")]
    [InlineData("20")]
    [InlineData("2020-5")]
    [InlineData("2020-13")]
    [InlineData("2021-02-29")]
    [InlineData("2020-05-17-01")]
    [InlineData("abcd")]
    public void Rejects_anything_else(string text)
    {
        var error = Assert.Throws<DomainException>(() => PartialDate.Parse(text));

        Assert.Equal("release_date_invalid", error.Code);
    }

    [Fact]
    public void Text_form_sorts_by_date()
    {
        string[] dates = ["2021", "2020-05-17", "2020", "2020-05"];

        Assert.Equal(["2020", "2020-05", "2020-05-17", "2021"], dates.Order(StringComparer.Ordinal));
    }
}
