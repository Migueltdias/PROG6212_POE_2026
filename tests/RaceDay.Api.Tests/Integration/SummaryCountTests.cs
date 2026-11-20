using System.Net.Http.Json;
using RaceDay.Api.Dtos;
using RaceDay.Api.Tests.Infrastructure;

namespace RaceDay.Api.Tests.Integration;

public class SummaryCountTests(RaceDayApiFactory factory) : IClassFixture<RaceDayApiFactory>
{
    [Fact]
    public async Task Event_summaries_count_entries_holding_a_place_and_confirmed_entries()
    {
        var page = await factory.CreateAnonymousClient().GetFromJsonAsync<PagedResult<EventSummaryDto>>("/api/events");

        var soweto = page!.Items.Single(e => e.EventId == 2);
        Assert.Equal(4, soweto.EnteredCount);
        Assert.Equal(3, soweto.ConfirmedCount);

        var capeTown = page.Items.Single(e => e.EventId == 3);
        Assert.Equal(2, capeTown.EnteredCount);
        Assert.Equal(2, capeTown.ConfirmedCount);
    }

    [Fact]
    public async Task An_organisers_own_events_carry_the_same_counts()
    {
        var pieter = await factory.CreateCachedClientForAsync(TestUsers.Pieter);

        var mine = await pieter.GetFromJsonAsync<PagedResult<EventSummaryDto>>("/api/events/mine");

        var soweto = Assert.Single(mine!.Items);
        Assert.Equal(4, soweto.EnteredCount);
        Assert.Equal(3, soweto.ConfirmedCount);
    }

    [Fact]
    public async Task Event_details_carry_the_counts_and_no_banner_until_one_is_uploaded()
    {
        var detail = await factory.CreateAnonymousClient().GetFromJsonAsync<EventDetailDto>("/api/events/1");

        Assert.Equal(6, detail!.EnteredCount);
        Assert.Equal(6, detail.ConfirmedCount);
        Assert.Null(detail.BannerImageUrl);
    }

    [Fact]
    public async Task A_participants_results_say_how_many_finished_the_category()
    {
        var sipho = await factory.CreateCachedClientForAsync(TestUsers.Sipho);

        var results = await sipho.GetFromJsonAsync<PagedResult<ResultDto>>("/api/results/mine");

        var comrades = Assert.Single(results!.Items);
        Assert.Equal(1, comrades.OverallPosition);
        Assert.Equal(3, comrades.FinishersInCategory);
    }

    [Fact]
    public async Task Leaderboard_rows_carry_their_result_id()
    {
        var board = await factory.CreateAnonymousClient()
            .GetFromJsonAsync<PagedResult<LeaderboardEntryDto>>("/api/categories/1/results");

        Assert.Equal([1, 2, 3, 4], board!.Items.Select(r => r.ResultId));
    }
}
