namespace RaceDay.Api.Domain;

public static class RoleNames
{
    public const string Organiser = "Organiser";
    public const string Participant = "Participant";

    public static readonly string[] All = [Organiser, Participant];
}

public static class EventStatuses
{
    public const string Draft = "Draft";
    public const string Published = "Published";
    public const string Cancelled = "Cancelled";
    public const string Completed = "Completed";

    public static readonly string[] All = [Draft, Published, Cancelled, Completed];
}

public static class Disciplines
{
    public const string Run = "Run";
    public const string Walk = "Walk";
    public const string Cycle = "Cycle";

    public static readonly string[] All = [Run, Walk, Cycle];
}

public static class EnrolmentStatuses
{
    public const string Pending = "Pending";
    public const string Confirmed = "Confirmed";
    public const string Cancelled = "Cancelled";
    public const string Withdrawn = "Withdrawn";

    public static readonly string[] All = [Pending, Confirmed, Cancelled, Withdrawn];

    public static readonly string[] Active = [Pending, Confirmed];
}

public static class ResultStatuses
{
    public const string Finished = "Finished";
    public const string DidNotFinish = "DNF";
    public const string DidNotStart = "DNS";
    public const string Disqualified = "DQ";

    public static readonly string[] All = [Finished, DidNotFinish, DidNotStart, Disqualified];
}

public static class WaypointTypes
{
    public static readonly string[] All = ["Start", "WaterPoint", "Medical", "Timing", "Finish"];
}

public static class Provinces
{
    public static readonly string[] All =
    [
        "Eastern Cape", "Free State", "Gauteng", "KwaZulu-Natal", "Limpopo",
        "Mpumalanga", "North West", "Northern Cape", "Western Cape"
    ];
}

public static class Genders
{
    public static readonly string[] All = ["M", "F", "O"];
}

public static class TShirtSizes
{
    public static readonly string[] All = ["XS", "S", "M", "L", "XL", "XXL"];
}

public static class DomainValue
{
    public static string? Canonical(string? value, IEnumerable<string> allowed) =>
        value is null
            ? null
            : allowed.FirstOrDefault(a => string.Equals(a, value.Trim(), StringComparison.OrdinalIgnoreCase));
}
