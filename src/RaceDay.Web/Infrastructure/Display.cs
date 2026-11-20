using System.Globalization;

namespace RaceDay.Web.Infrastructure;

public static class Display
{
    private static readonly CultureInfo Culture = CultureInfo.InvariantCulture;

    public static DateOnly Today(this TimeProvider clock) => DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);

    public static string Date(DateOnly date) => date.ToString("d MMM yyyy", Culture);

    public static string LongDate(DateOnly date) => date.ToString("dddd, d MMMM yyyy", Culture);

    public static string Month(DateOnly date) => date.ToString("MMM", Culture);

    public static string Time(TimeOnly time) => time.ToString("HH:mm", Culture);

    public static string DateTime(DateTime value) => value.ToString("d MMM yyyy, HH:mm", Culture);

    public static string Money(decimal amount) => amount == 0 ? "Free" : "R" + amount.ToString("#,##0.00", Culture);

    public static string Distance(decimal km) => km.ToString("0.##", Culture) + " km";

    public static string DistanceNumber(decimal km) => km.ToString("0.##", Culture);

    public static string DistanceRange(decimal? shortest, decimal? longest) =>
        (shortest, longest) switch
        {
            (null, _) or (_, null) => "Distances to be announced",
            var (s, l) when s == l => Distance(s!.Value),
            var (s, l) => $"{DistanceNumber(s!.Value)} – {Distance(l!.Value)}"
        };

    public static string Ordinal(int number)
    {
        var lastTwo = Math.Abs(number) % 100;
        var suffix = lastTwo is >= 11 and <= 13
            ? "th"
            : (Math.Abs(number) % 10) switch { 1 => "st", 2 => "nd", 3 => "rd", _ => "th" };
        return number.ToString(Culture) + suffix;
    }

    public static string OrdinalSuffix(int number) => Ordinal(number)[number.ToString(Culture).Length..];

    public static string Duration(int? seconds)
    {
        if (seconds is not { } s || s < 0)
        {
            return "–";
        }

        var span = TimeSpan.FromSeconds(s);
        return span.TotalHours >= 1
            ? $"{(int)span.TotalHours}:{span.Minutes:00}:{span.Seconds:00}"
            : $"{span.Minutes}:{span.Seconds:00}";
    }

    public static string Relative(DateOnly date, DateOnly today)
    {
        var days = date.DayNumber - today.DayNumber;
        return days switch
        {
            0 => "Today",
            1 => "Tomorrow",
            -1 => "Yesterday",
            > 1 => $"In {days} days",
            _ => $"{-days} days ago"
        };
    }

    public static string DisciplineIcon(string? discipline) => discipline switch
    {
        "Walk" => "bi-person-walking",
        "Cycle" => "bi-bicycle",
        _ => "bi-lightning-charge-fill"
    };

    public static string BannerVariant(IEnumerable<string>? disciplines)
    {
        var list = disciplines?.ToList() ?? [];
        return list.Count == 1 && list[0] is "Walk" or "Cycle" ? list[0].ToLowerInvariant() : "run";
    }

    public static string Initials(string text) =>
        string.Concat(text.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(w => char.IsLetter(w[0]))
            .Take(2)
            .Select(w => char.ToUpperInvariant(w[0])));

    public static string Gender(string? code) => code switch
    {
        "F" => "Female",
        "M" => "Male",
        "O" => "Other",
        _ => "–"
    };

    public static string MapEmbedUrl(decimal latitude, decimal longitude)
    {
        string F(decimal value) => value.ToString("0.000000", Culture);
        return "https://www.openstreetmap.org/export/embed.html?layer=mapnik" +
               $"&bbox={F(longitude - 0.03m)},{F(latitude - 0.015m)},{F(longitude + 0.03m)},{F(latitude + 0.015m)}" +
               $"&marker={F(latitude)},{F(longitude)}";
    }

    public static string MapLinkUrl(decimal latitude, decimal longitude) =>
        $"https://www.openstreetmap.org/?mlat={latitude.ToString("0.000000", Culture)}&mlon={longitude.ToString("0.000000", Culture)}#map=14/{latitude.ToString("0.0000", Culture)}/{longitude.ToString("0.0000", Culture)}";

    public static string Percent(int part, int whole) =>
        whole <= 0 ? "0%" : Math.Clamp(part * 100.0 / whole, 0, 100).ToString("0.#", Culture) + "%";
}
