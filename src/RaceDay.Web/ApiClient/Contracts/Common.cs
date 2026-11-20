namespace RaceDay.Web.ApiClient.Contracts;

public sealed class PagedResult<T>
{
    public List<T> Items { get; set; } = [];
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
}

public static class Roles
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

public static class EnrolmentStatuses
{
    public const string Pending = "Pending";
    public const string Confirmed = "Confirmed";
    public const string Cancelled = "Cancelled";
    public const string Withdrawn = "Withdrawn";

    public static readonly string[] All = [Pending, Confirmed, Cancelled, Withdrawn];
}

public static class ResultStatuses
{
    public const string Finished = "Finished";
    public const string DidNotFinish = "DNF";
    public const string DidNotStart = "DNS";
    public const string Disqualified = "DQ";

    public static readonly string[] All = [Finished, DidNotFinish, DidNotStart, Disqualified];
}

public static class Disciplines
{
    public static readonly string[] All = ["Run", "Walk", "Cycle"];
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
    public static readonly (string Code, string Label)[] All = [("F", "Female"), ("M", "Male"), ("O", "Other")];
}

public static class TShirtSizes
{
    public static readonly string[] All = ["XS", "S", "M", "L", "XL", "XXL"];
}
