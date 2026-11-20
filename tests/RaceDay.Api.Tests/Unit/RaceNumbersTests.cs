using RaceDay.Api.Services;

namespace RaceDay.Api.Tests.Unit;

public class RaceNumbersTests
{
    [Theory]
    [InlineData("Comrades Marathon 2026", "CM")]
    [InlineData("Soweto Marathon 2026", "SM")]
    [InlineData("Cape Town Cycle Tour 2027", "CT")]
    [InlineData("Durban 10km Classic", "DC")]
    [InlineData("two-oceans", "TO")]
    [InlineData("Parkrun", "PA")]
    [InlineData("X", "XR")]
    [InlineData("2026", "RD")]
    public void The_prefix_comes_from_the_event_name(string eventName, string expected) =>
        Assert.Equal(expected, RaceNumbers.PrefixFor(eventName));

    [Fact]
    public void The_next_number_follows_the_highest_one_in_the_category() =>
        Assert.Equal("SM0452", RaceNumbers.Next("SM", ["SM0450", "SM0451"]));

    [Fact]
    public void The_first_number_in_a_category_is_0001() =>
        Assert.Equal("CT0001", RaceNumbers.Next("CT", []));

    [Fact]
    public void Existing_numbers_with_a_different_prefix_still_count() =>
        Assert.Equal("SM1121", RaceNumbers.Next("SM", ["SH1120"]));

    [Fact]
    public void Numbers_without_digits_are_ignored() =>
        Assert.Equal("RD0008", RaceNumbers.Next("RD", ["VIP", "RD0007"]));

    [Fact]
    public void A_withdrawn_entrys_number_is_never_handed_out_again()
    {
        Assert.Equal("CM0004", RaceNumbers.Next("CM", ["CM0001", "CM0003"]));
    }
}
