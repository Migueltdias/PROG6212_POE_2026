using RaceDay.Web.ApiClient.Contracts;

namespace RaceDay.Web.Session;

public static class RoleHome
{
    public static (string Action, string Controller, string Area) For(string role) => role switch
    {
        Roles.Organiser => ("Index", "Dashboard", "Organiser"),
        Roles.Participant => ("Index", "Events", "Participant"),
        _ => ("Index", "Home", "")
    };
}
