using RaceDay.Api.Models;
using RaceDay.Api.Services;

namespace RaceDay.Api.Tests.Unit;

public class ResultRankingTests
{
    private static readonly DateOnly ComradesDay = new(2026, 6, 14);

    [Fact]
    public void Ranking_by_chip_time_reproduces_the_seeded_Comrades_positions()
    {
        var sipho = Finisher(1, 20567);
        var lerato = Finisher(2, 26059);
        var johan = Finisher(3, 23283);
        var daniel = new Result { EnrolmentId = 4, ResultStatus = "DNF", OverallPosition = 9, GenderPosition = 9, AgeGroupPosition = 9 };

        ResultRanking.Recalculate(
        [
            new(sipho, "M", new DateOnly(1994, 7, 21)),
            new(lerato, "F", new DateOnly(1998, 2, 14)),
            new(johan, "M", new DateOnly(1986, 5, 8)),
            new(daniel, "M", new DateOnly(1996, 12, 3))
        ], ComradesDay);

        Assert.Equal((1, 1, 1), Positions(sipho));
        Assert.Equal((2, 2, 1), Positions(johan));
        Assert.Equal((3, 1, 1), Positions(lerato));
        Assert.Null(daniel.OverallPosition);
        Assert.Null(daniel.GenderPosition);
        Assert.Null(daniel.AgeGroupPosition);
    }

    [Fact]
    public void Ranking_reproduces_the_seeded_community_10km_positions()
    {
        var ayesha = Finisher(5, 2892);
        var nomsa = Finisher(6, 3340);

        ResultRanking.Recalculate(
        [
            new(nomsa, "F", new DateOnly(2000, 1, 19)),
            new(ayesha, "F", new DateOnly(1991, 9, 30))
        ], ComradesDay);

        Assert.Equal((1, 1, 1), Positions(ayesha));
        Assert.Equal((2, 2, 1), Positions(nomsa));
    }

    [Fact]
    public void Runners_in_the_same_gender_and_age_band_share_an_age_group()
    {
        var first = Finisher(1, 3000);
        var second = Finisher(2, 3100);

        ResultRanking.Recalculate(
        [
            new(second, "M", new DateOnly(1992, 1, 1)),
            new(first, "M", new DateOnly(1990, 1, 1))
        ], ComradesDay);

        Assert.Equal(1, first.AgeGroupPosition);
        Assert.Equal(2, second.AgeGroupPosition);
    }

    [Theory]
    [InlineData(12, "U20")]
    [InlineData(19, "U20")]
    [InlineData(20, "20-29")]
    [InlineData(31, "30-39")]
    [InlineData(59, "50-59")]
    [InlineData(60, "60+")]
    [InlineData(84, "60+")]
    public void Age_groups_are_ten_year_bands(int age, string expected) =>
        Assert.Equal(expected, ResultRanking.AgeGroupFor(age));

    private static Result Finisher(int enrolmentId, int chipSeconds) => new()
    {
        EnrolmentId = enrolmentId,
        ResultStatus = "Finished",
        ChipTimeSeconds = chipSeconds,
        OverallPosition = 99,
        GenderPosition = 99,
        AgeGroupPosition = 99
    };

    private static (int?, int?, int?) Positions(Result r) => (r.OverallPosition, r.GenderPosition, r.AgeGroupPosition);
}
