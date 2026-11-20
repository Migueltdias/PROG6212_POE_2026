namespace RaceDay.Web.Models;

public sealed record PagerModel(int Page, int TotalPages, int TotalCount, string ItemName = "results");
