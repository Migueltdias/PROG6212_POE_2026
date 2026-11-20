using System.Net;
using System.Net.Http.Json;
using RaceDay.Api.Domain;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;
using RaceDay.Api.Tests.Infrastructure;

namespace RaceDay.Api.Tests.Integration;

public class ResultTests(RaceDayApiFactory factory) : IClassFixture<RaceDayApiFactory>
{
    private sealed record PastRace(int EventId, int CategoryId, IReadOnlyList<int> EnrolmentIds, IReadOnlyList<HttpClient> Runners);

    [Fact]
    public async Task An_Organiser_captures_a_finish_time_and_position_after_the_event()
    {
        var race = await SeedPastRaceAsync(("M", new DateOnly(1990, 1, 1)));
        var thandeka = await factory.CreateClientForAsync(TestUsers.Thandeka);

        var response = await thandeka.PostAsJsonAsync($"/api/enrolments/{race.EnrolmentIds[0]}/result", new
        {
            finishTime = "07:45:30",
            overallPosition = 1,
            genderPosition = 1,
            ageGroupPosition = 1,
            resultStatus = "Finished"
        });

        await response.ShouldHaveStatusAsync(HttpStatusCode.Created);
        var result = await response.ReadAsync<ResultDto>();
        Assert.Equal(new TimeOnly(7, 0), result.StartTime);
        Assert.Equal(new TimeOnly(7, 45, 30), result.FinishTime);
        Assert.Equal(2730, result.ChipTimeSeconds);
        Assert.Equal("00:45:30", result.ChipTime);
        Assert.Equal(1, result.OverallPosition);
        Assert.Equal(1, result.CapturedByUserId);
        var published = await factory.CreateAnonymousClient().GetAsync($"/api/results/{result.ResultId}");
        await published.ShouldHaveStatusAsync(HttpStatusCode.OK);
    }

    [Fact]
    public async Task A_Participant_can_view_their_own_results()
    {
        var race = await SeedPastRaceAsync(("F", new DateOnly(1993, 3, 3)));
        var thandeka = await factory.CreateClientForAsync(TestUsers.Thandeka);
        await thandeka.PostAsJsonAsync($"/api/enrolments/{race.EnrolmentIds[0]}/result",
            new { finishTime = "08:01:00", overallPosition = 4, resultStatus = "Finished" });

        var mine = await race.Runners[0].GetFromJsonAsync<PagedResult<ResultDto>>("/api/results/mine");

        var result = Assert.Single(mine!.Items);
        Assert.Equal(race.EventId, result.EventId);
        Assert.Equal(4, result.OverallPosition);
        Assert.Equal("01:01:00", result.ChipTime);
    }

    [Fact]
    public async Task A_seeded_Participants_history_can_be_filtered()
    {
        var sipho = await factory.CreateClientForAsync(TestUsers.Sipho);

        var all = await sipho.GetFromJsonAsync<PagedResult<ResultDto>>("/api/results/mine");
        var cycling = await sipho.GetFromJsonAsync<PagedResult<ResultDto>>("/api/results/mine?discipline=cycle");
        var sinceJuly = await sipho.GetFromJsonAsync<PagedResult<ResultDto>>("/api/results/mine?fromDate=2026-07-01");

        var comrades = Assert.Single(all!.Items);
        Assert.Equal("Comrades Marathon 2026", comrades.EventName);
        Assert.Equal(1, comrades.OverallPosition);
        Assert.Empty(cycling!.Items);
        Assert.Empty(sinceJuly!.Items);
    }

    [Fact]
    public async Task Only_one_result_can_be_captured_per_entry()
    {
        var race = await SeedPastRaceAsync(("M", new DateOnly(1988, 8, 8)));
        var thandeka = await factory.CreateClientForAsync(TestUsers.Thandeka);
        var url = $"/api/enrolments/{race.EnrolmentIds[0]}/result";
        await thandeka.PostAsJsonAsync(url, new { resultStatus = "DNS" });

        var response = await thandeka.PostAsJsonAsync(url, new { resultStatus = "DNF" });

        await response.ShouldHaveStatusAsync(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task A_non_finisher_is_recorded_without_a_time_or_position()
    {
        var race = await SeedPastRaceAsync(("F", new DateOnly(1999, 9, 9)));
        var thandeka = await factory.CreateClientForAsync(TestUsers.Thandeka);

        var response = await thandeka.PostAsJsonAsync($"/api/enrolments/{race.EnrolmentIds[0]}/result", new { resultStatus = "dnf" });

        await response.ShouldHaveStatusAsync(HttpStatusCode.Created);
        var result = await response.ReadAsync<ResultDto>();
        Assert.Equal("DNF", result.ResultStatus);
        Assert.Null(result.FinishTime);
        Assert.Null(result.ChipTimeSeconds);
        Assert.Null(result.OverallPosition);
    }

    [Fact]
    public async Task A_finish_time_sent_for_a_DNF_returns_400()
    {
        var race = await SeedPastRaceAsync(("M", new DateOnly(1990, 1, 1)));
        var thandeka = await factory.CreateClientForAsync(TestUsers.Thandeka);

        var response = await thandeka.PostAsJsonAsync($"/api/enrolments/{race.EnrolmentIds[0]}/result",
            new { finishTime = "08:00:00", resultStatus = "DNF" });

        await response.ShouldHaveStatusAsync(HttpStatusCode.BadRequest);
        Assert.Contains("finishTime", (await response.ReadValidationProblemAsync()).Errors.Keys);
    }

    [Fact]
    public async Task A_finisher_without_a_finish_time_or_position_returns_400()
    {
        var race = await SeedPastRaceAsync(("M", new DateOnly(1990, 1, 1)));
        var thandeka = await factory.CreateClientForAsync(TestUsers.Thandeka);

        var response = await thandeka.PostAsJsonAsync($"/api/enrolments/{race.EnrolmentIds[0]}/result", new { resultStatus = "Finished" });

        await response.ShouldHaveStatusAsync(HttpStatusCode.BadRequest);
        var errors = (await response.ReadValidationProblemAsync()).Errors.Keys;
        Assert.Contains("finishTime", errors);
        Assert.Contains("overallPosition", errors);
    }

    [Fact]
    public async Task A_finish_time_before_the_start_returns_400()
    {
        var race = await SeedPastRaceAsync(("M", new DateOnly(1990, 1, 1)));
        var thandeka = await factory.CreateClientForAsync(TestUsers.Thandeka);

        var response = await thandeka.PostAsJsonAsync($"/api/enrolments/{race.EnrolmentIds[0]}/result",
            new { finishTime = "06:30:00", overallPosition = 1, resultStatus = "Finished" });

        await response.ShouldHaveStatusAsync(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Results_cannot_be_captured_before_the_event_has_been_run()
    {
        var pieter = await factory.CreateClientForAsync(TestUsers.Pieter);

        var response = await pieter.PostAsJsonAsync("/api/enrolments/7/result",
            new { finishTime = "09:00:00", overallPosition = 1, resultStatus = "Finished" });

        await response.ShouldHaveStatusAsync(HttpStatusCode.Conflict);
        Assert.Contains("not been run", (await response.ReadProblemAsync()).Detail);
    }

    [Fact]
    public async Task Results_are_only_captured_for_confirmed_entries()
    {
        var race = await SeedPastRaceAsync(EnrolmentStatuses.Pending, ("F", new DateOnly(1990, 1, 1)));
        var thandeka = await factory.CreateClientForAsync(TestUsers.Thandeka);

        var response = await thandeka.PostAsJsonAsync($"/api/enrolments/{race.EnrolmentIds[0]}/result", new { resultStatus = "DNS" });

        await response.ShouldHaveStatusAsync(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Only_the_events_Organiser_can_capture_its_results()
    {
        var race = await SeedPastRaceAsync(("M", new DateOnly(1990, 1, 1)));
        var url = $"/api/enrolments/{race.EnrolmentIds[0]}/result";
        var body = new { finishTime = "07:50:00", overallPosition = 1, resultStatus = "Finished" };

        var otherOrganiser = await (await factory.CreateClientForAsync(TestUsers.Pieter)).PostAsJsonAsync(url, body);
        var theRunner = await race.Runners[0].PostAsJsonAsync(url, body);

        await otherOrganiser.ShouldHaveStatusAsync(HttpStatusCode.Forbidden);
        await theRunner.ShouldHaveStatusAsync(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task The_owner_can_correct_and_remove_a_result_so_it_can_be_recaptured()
    {
        var race = await SeedPastRaceAsync(("F", new DateOnly(1985, 5, 5)));
        var thandeka = await factory.CreateClientForAsync(TestUsers.Thandeka);
        var url = $"/api/enrolments/{race.EnrolmentIds[0]}/result";
        var captured = await (await thandeka.PostAsJsonAsync(url,
            new { finishTime = "07:40:00", overallPosition = 1, resultStatus = "Finished" })).ReadAsync<ResultDto>();

        var corrected = await thandeka.PutAsJsonAsync($"/api/results/{captured.ResultId}", new { resultStatus = "DQ" });
        var removed = await thandeka.DeleteAsync($"/api/results/{captured.ResultId}");
        var gone = await thandeka.GetAsync($"/api/results/{captured.ResultId}");
        var recaptured = await thandeka.PostAsJsonAsync(url, new { finishTime = "07:41:00", overallPosition = 1, resultStatus = "Finished" });

        await corrected.ShouldHaveStatusAsync(HttpStatusCode.OK);
        Assert.Equal("DQ", (await corrected.ReadAsync<ResultDto>()).ResultStatus);
        await removed.ShouldHaveStatusAsync(HttpStatusCode.NoContent);
        await gone.ShouldHaveStatusAsync(HttpStatusCode.NotFound);
        await recaptured.ShouldHaveStatusAsync(HttpStatusCode.Created);
    }

    [Fact]
    public async Task A_bulk_import_saves_new_rows_skips_existing_ones_reports_errors_and_ranks_by_chip_time()
    {
        var race = await SeedPastRaceAsync(
            ("M", new DateOnly(1990, 1, 1)),
            ("F", new DateOnly(1991, 1, 1)),
            ("M", new DateOnly(1978, 1, 1)));
        var thandeka = await factory.CreateClientForAsync(TestUsers.Thandeka);
        await thandeka.PostAsJsonAsync($"/api/enrolments/{race.EnrolmentIds[0]}/result",
            new { finishTime = "07:50:00", overallPosition = 1, resultStatus = "Finished" });

        var response = await thandeka.PostAsJsonAsync($"/api/categories/{race.CategoryId}/results/bulk", new
        {
            results = new object[]
            {
                new { raceNumber = "tr0002", chipTimeSeconds = 2900, resultStatus = "Finished" },
                new { raceNumber = "TR0003", finishTime = "07:55:00", resultStatus = "Finished" },
                new { raceNumber = "TR0001", finishTime = "07:49:00", resultStatus = "Finished" },
                new { raceNumber = "XX9999", resultStatus = "DNS" }
            }
        });

        await response.ShouldHaveStatusAsync(HttpStatusCode.Accepted);
        var summary = await response.ReadAsync<BulkImportResponse>();
        Assert.Equal(2, summary.Imported);
        Assert.Equal(1, summary.Skipped);
        Assert.Equal("XX9999", Assert.Single(summary.Errors).RaceNumber);

        var board = await factory.CreateAnonymousClient()
            .GetFromJsonAsync<PagedResult<LeaderboardEntryDto>>($"/api/categories/{race.CategoryId}/results");
        Assert.Equal(["TR0002", "TR0001", "TR0003"], board!.Items.Select(r => r.RaceNumber));
        Assert.Equal([1, 2, 3], board.Items.Select(r => r.OverallPosition!.Value));
        Assert.Equal([1, 1, 2], board.Items.Select(r => r.GenderPosition!.Value));
        Assert.Equal([1, 1, 1], board.Items.Select(r => r.AgeGroupPosition!.Value));
        Assert.Equal(new TimeOnly(7, 48, 20), board.Items[0].FinishTime);
    }

    [Fact]
    public async Task A_bulk_import_with_no_rows_or_an_unknown_status_returns_400()
    {
        var thandeka = await factory.CreateClientForAsync(TestUsers.Thandeka);

        var empty = await thandeka.PostAsJsonAsync("/api/categories/1/results/bulk", new { results = Array.Empty<object>() });
        var unknown = await thandeka.PostAsJsonAsync("/api/categories/1/results/bulk",
            new { results = new[] { new { raceNumber = "CM1042", resultStatus = "Lost" } } });

        await empty.ShouldHaveStatusAsync(HttpStatusCode.BadRequest);
        await unknown.ShouldHaveStatusAsync(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task The_category_leaderboard_is_public_and_lists_finishers_first()
    {
        var anonymous = factory.CreateAnonymousClient();

        var board = await anonymous.GetFromJsonAsync<PagedResult<LeaderboardEntryDto>>("/api/categories/1/results");
        var women = await anonymous.GetFromJsonAsync<PagedResult<LeaderboardEntryDto>>("/api/categories/1/results?gender=f");

        Assert.Equal(["CM1042", "CM3315", "CM2288", "CM4408"], board!.Items.Select(r => r.RaceNumber));
        Assert.Equal("Sipho Ndlovu", board.Items[0].ParticipantName);
        Assert.Equal("05:42:47", board.Items[0].ChipTime);
        Assert.Equal("DNF", board.Items[^1].ResultStatus);
        Assert.Null(board.Items[^1].OverallPosition);
        var lerato = Assert.Single(women!.Items);
        Assert.Equal("Lerato Dlamini", lerato.ParticipantName);
        Assert.Equal(1, lerato.GenderPosition);
    }

    [Fact]
    public async Task Event_results_are_grouped_by_category()
    {
        var results = await factory.CreateAnonymousClient().GetFromJsonAsync<EventResultsDto>("/api/events/1/results");

        Assert.Equal(["Up Run 87.7km Ultra Marathon", "Comrades Community 10km"], results!.Categories.Select(c => c.CategoryName));
        Assert.Equal(4, results.Categories[0].Results.Count);
        Assert.Equal(["CC0071", "CC0072"], results.Categories[1].Results.Select(r => r.RaceNumber));
    }

    private Task<PastRace> SeedPastRaceAsync(params (string Gender, DateOnly DateOfBirth)[] runners) =>
        SeedPastRaceAsync(EnrolmentStatuses.Confirmed, runners);

    private async Task<PastRace> SeedPastRaceAsync(string enrolmentStatus, params (string Gender, DateOnly DateOfBirth)[] runners)
    {
        var participants = new List<(HttpClient Client, UserProfileDto Profile)>();
        foreach (var (gender, dateOfBirth) in runners)
        {
            participants.Add(await factory.RegisterAndLoginAsync(dateOfBirth: dateOfBirth, gender: gender));
        }

        return await factory.WithDbAsync(async db =>
        {
            var category = new EventCategory
            {
                CategoryName = "10km Run", Discipline = "Run", DistanceKm = 10m, EntryFee = 100m,
                MaxParticipants = 100, StartTime = new TimeOnly(7, 0), MinimumAge = 12
            };
            var ev = new Event
            {
                OrganiserId = 1,
                EventName = $"Zoo Lake 10km {Guid.NewGuid():N}",
                EventDate = new DateOnly(2026, 9, 20),
                StartTime = new TimeOnly(7, 0),
                Province = "Gauteng", City = "Johannesburg", VenueName = "Zoo Lake",
                Latitude = -26.1550m, Longitude = 28.0300m,
                RegistrationOpens = new DateOnly(2026, 8, 1), RegistrationCloses = new DateOnly(2026, 9, 15),
                EventStatus = EventStatuses.Published,
                CreatedAt = new DateTime(2026, 7, 1, 9, 0, 0, DateTimeKind.Utc),
                Categories = { category }
            };

            var number = 1;
            foreach (var (_, profile) in participants)
            {
                category.Enrolments.Add(new Enrolment
                {
                    ParticipantId = profile.UserId,
                    RaceNumber = $"TR{number++:D4}",
                    EnrolmentDate = new DateTime(2026, 8, 10, 12, 0, 0, DateTimeKind.Utc),
                    EnrolmentStatus = enrolmentStatus,
                    AmountPaid = enrolmentStatus == EnrolmentStatuses.Confirmed ? 100m : 0m,
                    EmergencyContactName = "Next Of Kin",
                    EmergencyContactPhone = "0820000000"
                });
            }

            db.Events.Add(ev);
            await db.SaveChangesAsync();

            return new PastRace(ev.EventId, category.CategoryId,
                category.Enrolments.OrderBy(e => e.RaceNumber).Select(e => e.EnrolmentId).ToList(),
                participants.Select(p => p.Client).ToList());
        });
    }
}
