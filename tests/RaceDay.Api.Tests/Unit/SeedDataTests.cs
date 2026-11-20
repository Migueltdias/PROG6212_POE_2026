using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Models;

namespace RaceDay.Api.Tests.Unit;

public class SeedDataTests : IDisposable
{
    private readonly RaceDayDbContext _db = UnitDatabase.Create();

    [Fact]
    public async Task The_seed_has_the_same_row_counts_as_the_Part_1_script()
    {
        Assert.Equal(2, await _db.Roles.CountAsync());
        Assert.Equal(8, await _db.Users.CountAsync());
        Assert.Equal(3, await _db.Events.CountAsync());
        Assert.Equal(8, await _db.EventCategories.CountAsync());
        Assert.Equal(17, await _db.RouteWaypoints.CountAsync());
        Assert.Equal(13, await _db.Enrolments.CountAsync());
        Assert.Equal(6, await _db.Results.CountAsync());
        Assert.Equal(5, await _db.WeatherSnapshots.CountAsync());
    }

    [Fact]
    public async Task There_are_two_Organisers_and_six_Participants()
    {
        var users = await _db.Users.Include(u => u.Role).ToListAsync();
        var byRole = users.GroupBy(u => u.Role.RoleName).ToDictionary(g => g.Key, g => g.Count());

        Assert.Equal(2, byRole["Organiser"]);
        Assert.Equal(6, byRole["Participant"]);
    }

    [Fact]
    public async Task Every_seeded_account_has_a_real_hash_of_the_demo_password()
    {
        var hasher = new PasswordHasher<User>();

        foreach (var user in await _db.Users.ToListAsync())
        {
            Assert.DoesNotContain("PLACEHOLDER", user.PasswordHash);
            Assert.Equal(PasswordVerificationResult.Success, hasher.VerifyHashedPassword(user, user.PasswordHash, SeedData.DemoPassword));
        }
    }

    [Fact]
    public async Task Seeded_results_follow_the_consistency_rule()
    {
        foreach (var result in await _db.Results.ToListAsync())
        {
            var finished = result.ResultStatus == "Finished";
            Assert.Equal(finished, result.FinishTime is not null);
            Assert.Equal(finished, result.ChipTimeSeconds is not null);
            Assert.Equal(finished, result.OverallPosition is not null);
        }
    }

    public void Dispose() => _db.Dispose();
}

public static class UnitDatabase
{
    public static RaceDayDbContext Create()
    {
        var options = new DbContextOptionsBuilder<RaceDayDbContext>()
            .UseInMemoryDatabase($"RaceDayUnit-{Guid.NewGuid():N}")
            .Options;
        var db = new RaceDayDbContext(options);
        db.Database.EnsureCreated();
        return db;
    }
}
