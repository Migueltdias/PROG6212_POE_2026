using System.Text.RegularExpressions;

namespace RaceDay.Api.Services;

public static partial class RaceNumbers
{
    public static string PrefixFor(string eventName)
    {
        var words = Words().Matches(eventName).Select(m => m.Value).ToList();
        var prefix = words.Count switch
        {
            0 => "RD",
            1 => words[0].Length >= 2 ? words[0][..2] : words[0] + "R",
            _ => $"{words[0][0]}{words[1][0]}"
        };

        return prefix.ToUpperInvariant();
    }

    public static string Next(string prefix, IEnumerable<string> existingInCategory)
    {
        var highest = existingInCategory.Select(TrailingNumber).DefaultIfEmpty(0).Max();
        return $"{prefix}{highest + 1:D4}";
    }

    private static int TrailingNumber(string raceNumber)
    {
        var digits = TrailingDigits().Match(raceNumber).Value;
        return int.TryParse(digits, out var number) ? number : 0;
    }

    [GeneratedRegex(@"\b[A-Za-z]+")]
    private static partial Regex Words();

    [GeneratedRegex(@"\d+$")]
    private static partial Regex TrailingDigits();
}
