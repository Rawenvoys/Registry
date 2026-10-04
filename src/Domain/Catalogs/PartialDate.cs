using System.Globalization;

namespace Registry.Domain.Catalogs;

/// <summary>
/// A date that may be known only to the year or month: a wine's vintage, a release date.
/// Written as <c>2020</c>, <c>2020-05</c> or <c>2020-05-17</c>, which also sorts correctly as text.
/// </summary>
public readonly record struct PartialDate
{
    public const int MaxLength = 10;

    private PartialDate(int year, int? month, int? day)
    {
        Year = year;
        Month = month;
        Day = day;
    }

    public int Year { get; }

    public int? Month { get; }

    public int? Day { get; }

    public static PartialDate Parse(string value) =>
        TryParse(value, out var date)
            ? date
            : throw new DomainException("release_date_invalid", "Podaj datę jako rok, rok-miesiąc albo rok-miesiąc-dzień, np. 2020 albo 2020-05-17.");

    public static bool TryParse(string? value, out PartialDate date)
    {
        date = default;
        var parts = value?.Trim().Split('-');
        if (parts is null || parts.Length > 3
            || parts[0].Length != 4 || !TryNumber(parts[0], 1, 9999, out var year))
        {
            return false;
        }

        int? month = null;
        int? day = null;
        if (parts.Length > 1)
        {
            if (parts[1].Length != 2 || !TryNumber(parts[1], 1, 12, out var m))
            {
                return false;
            }

            month = m;
        }

        if (parts.Length > 2)
        {
            if (parts[2].Length != 2 || !TryNumber(parts[2], 1, DateTime.DaysInMonth(year, month!.Value), out var d))
            {
                return false;
            }

            day = d;
        }

        date = new PartialDate(year, month, day);
        return true;
    }

    public override string ToString() => (Month, Day) switch
    {
        (null, _) => Year.ToString("D4", CultureInfo.InvariantCulture),
        (_, null) => string.Create(CultureInfo.InvariantCulture, $"{Year:D4}-{Month:D2}"),
        _ => string.Create(CultureInfo.InvariantCulture, $"{Year:D4}-{Month:D2}-{Day:D2}"),
    };

    private static bool TryNumber(string text, int min, int max, out int number) =>
        int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out number) && number >= min && number <= max;
}
