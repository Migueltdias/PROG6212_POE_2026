namespace RaceDay.Web.ApiClient.Contracts;

public sealed class Category
{
    public int CategoryId { get; set; }
    public int EventId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string Discipline { get; set; } = string.Empty;
    public decimal DistanceKm { get; set; }
    public decimal EntryFee { get; set; }
    public int MaxParticipants { get; set; }
    public TimeOnly StartTime { get; set; }
    public int MinimumAge { get; set; }
    public int EnteredCount { get; set; }
    public int PlacesLeft { get; set; }
}

public sealed class CategoryRequest
{
    public string CategoryName { get; set; } = string.Empty;
    public string Discipline { get; set; } = string.Empty;
    public decimal? DistanceKm { get; set; }
    public decimal? EntryFee { get; set; }
    public int? MaxParticipants { get; set; }
    public TimeOnly? StartTime { get; set; }
    public int? MinimumAge { get; set; }
}
