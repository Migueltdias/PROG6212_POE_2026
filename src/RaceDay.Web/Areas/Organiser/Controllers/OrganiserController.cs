using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.ApiClient;
using RaceDay.Web.ApiClient.Contracts;
using RaceDay.Web.Controllers;
using RaceDay.Web.Session;

namespace RaceDay.Web.Areas.Organiser.Controllers;

[Area("Organiser")]
[RequireRole(Roles.Organiser)]
public abstract class OrganiserController(IRaceDayApi api) : AppController
{
    protected IRaceDayApi Api { get; } = api;

    protected async Task<EventDetail> LoadOwnEventAsync(int eventId, CancellationToken cancellationToken)
    {
        var ev = (await Api.GetEventAsync(eventId, cancellationToken)).Require();
        if (ev.OrganiserId != CurrentUser.UserId)
        {
            throw new ApiRequestFailedException(new ApiResult
            {
                StatusCode = System.Net.HttpStatusCode.Forbidden,
                Error = "Only the Organiser who created this event can manage it."
            });
        }

        return ev;
    }
}
