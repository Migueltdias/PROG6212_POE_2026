using System.ComponentModel.DataAnnotations;
using RaceDay.Api.Dtos;
using RaceDay.Api.Services;

namespace RaceDay.Api.Tests.Unit;

public class RequestValidationTests
{
    [Fact]
    public void A_complete_registration_is_valid() =>
        Assert.Empty(Validate(Registration()));

    [Fact]
    public void Registration_requires_matching_passwords()
    {
        var request = Registration();
        request.ConfirmPassword = "Different@2026";

        Assert.Contains(Validate(request), e => e.MemberNames.Contains(nameof(RegisterRequest.ConfirmPassword)));
    }

    [Theory]
    [InlineData("Admin")]
    [InlineData("Spectator")]
    public void Registration_only_accepts_the_two_roles(string role)
    {
        var request = Registration();
        request.Role = role;

        Assert.Contains(Validate(request), e => e.MemberNames.Contains(nameof(RegisterRequest.Role)));
    }

    [Theory]
    [InlineData("organiser")]
    [InlineData("PARTICIPANT")]
    public void Role_names_are_matched_without_regard_to_case(string role)
    {
        var request = Registration();
        request.Role = role;

        Assert.Empty(Validate(request));
    }

    [Theory]
    [InlineData("not-an-email")]
    [InlineData("a@b")]
    public void Registration_rejects_malformed_emails(string email)
    {
        var request = Registration();
        request.Email = email;

        Assert.Contains(Validate(request), e => e.MemberNames.Contains(nameof(RegisterRequest.Email)));
    }

    [Fact]
    public void An_event_cannot_close_registration_before_it_opens()
    {
        var request = Event();
        request.RegistrationOpens = new DateOnly(2027, 3, 1);
        request.RegistrationCloses = new DateOnly(2027, 2, 1);

        Assert.Contains(Validate(request), e => e.MemberNames.Contains("registrationCloses"));
    }

    [Fact]
    public void An_event_cannot_take_place_before_registration_closes()
    {
        var request = Event();
        request.EventDate = new DateOnly(2027, 1, 1);

        Assert.Contains(Validate(request), e => e.MemberNames.Contains("eventDate"));
    }

    [Fact]
    public void A_finisher_needs_a_finish_time_and_a_position()
    {
        var errors = Validate(new ResultRequest { ResultStatus = "Finished" });

        Assert.Contains(errors, e => e.MemberNames.Contains("finishTime"));
        Assert.Contains(errors, e => e.MemberNames.Contains("overallPosition"));
    }

    [Theory]
    [InlineData("DNF")]
    [InlineData("DNS")]
    [InlineData("DQ")]
    public void A_non_finisher_cannot_have_a_finish_time(string status)
    {
        var errors = Validate(new ResultRequest { ResultStatus = status, FinishTime = new TimeOnly(9, 0) });

        Assert.Contains(errors, e => e.MemberNames.Contains("finishTime"));
    }

    [Fact]
    public void A_non_finisher_without_a_time_is_valid() =>
        Assert.Empty(Validate(new ResultRequest { ResultStatus = "DNS" }));

    [Fact]
    public void A_category_must_be_a_known_discipline_with_a_positive_distance()
    {
        var errors = Validate(new CategoryRequest
        {
            CategoryName = "Swim", Discipline = "Swim", DistanceKm = 0m, EntryFee = 0m,
            MaxParticipants = 10, StartTime = new TimeOnly(6, 0)
        });

        Assert.Contains(errors, e => e.MemberNames.Contains(nameof(CategoryRequest.Discipline)));
        Assert.Contains(errors, e => e.MemberNames.Contains(nameof(CategoryRequest.DistanceKm)));
    }

    [Fact]
    public void Organisers_must_be_adults_but_participants_may_be_children()
    {
        var today = new DateOnly(2026, 10, 1);
        var fifteen = new DateOnly(2011, 1, 1);

        Assert.NotNull(UserRules.ValidateDateOfBirth(fifteen, "Organiser", today));
        Assert.Null(UserRules.ValidateDateOfBirth(fifteen, "Participant", today));
        Assert.NotNull(UserRules.ValidateDateOfBirth(today, "Participant", today));
    }

    private static RegisterRequest Registration() => new()
    {
        FirstName = "Ayanda",
        LastName = "Zulu",
        Email = "ayanda.zulu@example.co.za",
        Password = "RaceDay@2026",
        ConfirmPassword = "RaceDay@2026",
        PhoneNumber = "0821234567",
        DateOfBirth = new DateOnly(1995, 4, 12),
        Gender = "F",
        Role = "Participant"
    };

    private static EventRequest Event() => new()
    {
        EventName = "Durban Beachfront 10km 2027",
        EventDate = new DateOnly(2027, 5, 9),
        StartTime = new TimeOnly(6, 30),
        Province = "KwaZulu-Natal",
        City = "Durban",
        VenueName = "Suncoast Promenade",
        Latitude = -29.8395m,
        Longitude = 31.0352m,
        RegistrationOpens = new DateOnly(2026, 11, 1),
        RegistrationCloses = new DateOnly(2027, 4, 30)
    };

    private static List<ValidationResult> Validate(object request)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(request, new ValidationContext(request), results, validateAllProperties: true);
        return results;
    }
}
