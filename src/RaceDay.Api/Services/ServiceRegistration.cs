namespace RaceDay.Api.Services;

public static class ServiceRegistration
{
    public static IServiceCollection AddRaceDayServices(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IEventService, EventService>();
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<IWaypointService, WaypointService>();
        services.AddScoped<IEnrolmentService, EnrolmentService>();
        services.AddScoped<IResultService, ResultService>();
        services.AddScoped<IWeatherService, WeatherService>();
        services.AddScoped<IImageService, ImageService>();
        return services;
    }
}
