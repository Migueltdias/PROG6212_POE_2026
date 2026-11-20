using Microsoft.EntityFrameworkCore;

namespace RaceDay.Api.Data;

public static class DatabaseInitialiser
{
    public static async Task InitialiseDatabaseAsync(this WebApplication app)
    {
        if (!app.Configuration.GetValue("Database:MigrateOnStartup", true))
        {
            return;
        }

        var attempts = Math.Max(1, app.Configuration.GetValue("Database:StartupAttempts", 1));
        var delay = TimeSpan.FromSeconds(Math.Max(1, app.Configuration.GetValue("Database:StartupRetrySeconds", 5)));

        for (var attempt = 1; ; attempt++)
        {
            try
            {
                await MigrateAsync(app);
                return;
            }
            catch (Exception ex) when (attempt < attempts)
            {
                app.Logger.LogWarning("The database is not ready yet (attempt {Attempt} of {Attempts}): {Message}. Retrying in {Delay}s.",
                    attempt, attempts, ex.Message, delay.TotalSeconds);
                await Task.Delay(delay);
            }
            catch (Exception ex)
            {
                app.Logger.LogError(ex,
                    "The database could not be migrated. Check ConnectionStrings:RaceDay in appsettings.json.");
                return;
            }
        }
    }

    private static async Task MigrateAsync(WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RaceDayDbContext>();

        if (db.Database.IsRelational())
        {
            await db.Database.MigrateAsync();
        }
        else
        {
            await db.Database.EnsureCreatedAsync();
        }
    }
}
