using RaceDay.Api.Models;

namespace RaceDay.Api.Services;

public static class Ownership
{
    public static ServiceError? RequireOwner(Event ev, int callerId) =>
        ev.OrganiserId == callerId
            ? null
            : Errors.Forbidden("Only the Organiser who created this event can manage it.");
}
