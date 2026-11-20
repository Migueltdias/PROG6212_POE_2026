namespace RaceDay.Web.Infrastructure;

public static class Flash
{
    public const string Success = "Flash.Success";
    public const string Error = "Flash.Error";
    public const string Warning = "Flash.Warning";
    public const string Info = "Flash.Info";

    public static readonly (string Key, string Css, string Icon)[] All =
    [
        (Success, "alert-success", "bi-check-circle-fill"),
        (Error, "alert-danger", "bi-exclamation-octagon-fill"),
        (Warning, "alert-warning", "bi-exclamation-triangle-fill"),
        (Info, "alert-info", "bi-info-circle-fill")
    ];
}
