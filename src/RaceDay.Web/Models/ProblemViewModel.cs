namespace RaceDay.Web.Models;

public sealed record ProblemViewModel(int StatusCode, string Title, string Message, string Icon)
{
    public string? RequestId { get; init; }

    public static ProblemViewModel NotFound(string? message = null) =>
        new(404, "We couldn't find that", message ?? "The page or record you asked for does not exist, or it is no longer available.", "bi-signpost-split");

    public static ProblemViewModel Forbidden(string? message = null) =>
        new(403, "That isn't yours to open", message ?? "You can only open records that belong to you.", "bi-sign-stop-fill");

    public static ProblemViewModel Unavailable(string? message = null) =>
        new(503, "RaceDay is catching its breath", message ?? "We could not reach the RaceDay service just now. Please try again in a moment.", "bi-cloud-slash");

    public static ProblemViewModel Error() =>
        new(500, "Something went wrong", "An unexpected problem stopped that page from loading. It has been logged; please try again.", "bi-exclamation-octagon");

    public static ProblemViewModel ForStatus(int statusCode) => statusCode switch
    {
        404 => NotFound(),
        403 => Forbidden(),
        400 => new(400, "That request didn't look right", "Check the address or the details you entered, and try again.", "bi-question-octagon"),
        405 => new(405, "That action isn't available here", "Use the buttons on the page rather than typing the address.", "bi-hand-index"),
        503 => Unavailable(),
        _ when statusCode >= 500 => Error(),
        _ => new(statusCode, "Something went wrong", "That page could not be shown.", "bi-exclamation-circle")
    };
}
