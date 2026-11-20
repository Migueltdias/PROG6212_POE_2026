using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.ApiClient;
using RaceDay.Web.ApiClient.Contracts;
using RaceDay.Web.Controllers;
using RaceDay.Web.Session;

namespace RaceDay.Web.Areas.Participant.Controllers;

[Area("Participant")]
[RequireRole(Roles.Participant)]
public abstract class ParticipantController(IRaceDayApi api) : AppController
{
    protected IRaceDayApi Api { get; } = api;

    protected Task<List<Enrolment>> LoadAllMyEntriesAsync(CancellationToken cancellationToken) =>
        ApiPaging.CollectAllAsync(page => Api.GetMyEnrolmentsAsync(status: null, upcoming: null, page, ApiPaging.MaxPageSize, cancellationToken));
}
