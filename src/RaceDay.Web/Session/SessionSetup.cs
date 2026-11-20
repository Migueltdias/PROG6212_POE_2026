namespace RaceDay.Web.Session;

public static class SessionSetup
{
    public const string CookieName = ".RaceDay.Session";

    public static IServiceCollection AddRaceDaySession(this IServiceCollection services, IConfiguration configuration)
    {
        var idleMinutes = Math.Clamp(configuration.GetValue("Session:IdleTimeoutMinutes", 30), 5, 24 * 60);

        services.AddHttpContextAccessor();
        services.AddDistributedMemoryCache();
        services.AddSession(options =>
        {
            options.IdleTimeout = TimeSpan.FromMinutes(idleMinutes);
            options.Cookie.Name = CookieName;
            options.Cookie.HttpOnly = true;
            options.Cookie.IsEssential = true;
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        });

        return services;
    }
}
