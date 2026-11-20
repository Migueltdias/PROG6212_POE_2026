using System.Net;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Razor.TagHelpers;
using RaceDay.Web.ApiClient;
using RaceDay.Web.Areas.Organiser.Models;
using RaceDay.Web.Infrastructure;

namespace RaceDay.Web.Tests.Unit;

public class DisplayTests
{
    [Theory]
    [InlineData(1, "1st")]
    [InlineData(2, "2nd")]
    [InlineData(3, "3rd")]
    [InlineData(4, "4th")]
    [InlineData(11, "11th")]
    [InlineData(12, "12th")]
    [InlineData(13, "13th")]
    [InlineData(21, "21st")]
    [InlineData(47, "47th")]
    [InlineData(102, "102nd")]
    [InlineData(111, "111th")]
    public void Positions_read_as_ordinals(int position, string expected) =>
        Assert.Equal(expected, Display.Ordinal(position));

    [Theory]
    [InlineData(2892, "48:12")]
    [InlineData(20567, "5:42:47")]
    [InlineData(59, "0:59")]
    [InlineData(null, "–")]
    public void Chip_times_read_as_race_times(int? seconds, string expected) =>
        Assert.Equal(expected, Display.Duration(seconds));

    [Fact]
    public void Money_distances_and_ranges_read_naturally()
    {
        Assert.Equal("R1,250.00", Display.Money(1250m));
        Assert.Equal("Free", Display.Money(0m));
        Assert.Equal("21.1 km", Display.Distance(21.10m));
        Assert.Equal("42 km", Display.Distance(42.00m));
        Assert.Equal("10 – 42.2 km", Display.DistanceRange(10m, 42.2m));
        Assert.Equal("87.7 km", Display.DistanceRange(87.7m, 87.7m));
        Assert.Equal("Distances to be announced", Display.DistanceRange(null, null));
    }

    [Fact]
    public void Dates_read_relative_to_today()
    {
        var today = new DateOnly(2026, 10, 1);

        Assert.Equal("Today", Display.Relative(today, today));
        Assert.Equal("Tomorrow", Display.Relative(today.AddDays(1), today));
        Assert.Equal("In 31 days", Display.Relative(new DateOnly(2026, 11, 1), today));
        Assert.Equal("109 days ago", Display.Relative(new DateOnly(2026, 6, 14), today));
    }

    [Fact]
    public void Each_event_type_has_its_own_icon_and_banner_colour()
    {
        Assert.Equal("bi-bicycle", Display.DisciplineIcon("Cycle"));
        Assert.Equal("bi-person-walking", Display.DisciplineIcon("Walk"));
        Assert.Equal("cycle", Display.BannerVariant(["Cycle"]));
        Assert.Equal("run", Display.BannerVariant(["Run", "Walk"]));
    }
}

public class StatusBadgeTests
{
    private static string Render(string value)
    {
        var helper = new StatusBadgeTagHelper { Value = value };
        var output = new TagHelperOutput("status-badge", [], (_, _) => Task.FromResult<TagHelperContent>(new DefaultTagHelperContent()));
        helper.Process(new TagHelperContext([], new Dictionary<object, object>(), "id"), output);
        return $"{output.Attributes["class"].Value}|{output.Content.GetContent()}";
    }

    [Theory]
    [InlineData("Confirmed", "rd-status--confirmed", "bi-check-circle-fill", "Confirmed")]
    [InlineData("Pending", "rd-status--pending", "bi-hourglass-split", "Pending payment")]
    [InlineData("Cancelled", "rd-status--cancelled", "bi-x-circle-fill", "Cancelled")]
    [InlineData("Withdrawn", "rd-status--withdrawn", "bi-dash-circle", "Withdrawn")]
    [InlineData("Published", "rd-status--published", "bi-megaphone-fill", "Published")]
    [InlineData("DNF", "rd-status--dnf", "bi-exclamation-triangle-fill", "Did not finish")]
    public void Each_status_has_its_own_colour_icon_and_label(string status, string css, string icon, string label)
    {
        var rendered = Render(status);

        Assert.Contains(css, rendered);
        Assert.Contains(icon, rendered);
        Assert.EndsWith(label, rendered);
    }
}

public class ApiErrorMappingTests
{
    private static ApiResult Validation(params (string Key, string Message)[] errors) => new()
    {
        StatusCode = HttpStatusCode.BadRequest,
        FieldErrors = errors.ToDictionary(e => e.Key, e => new[] { e.Message })
    };

    [Fact]
    public void Camel_case_api_fields_land_on_the_matching_form_fields()
    {
        var modelState = new ModelStateDictionary();

        modelState.AddApiErrors(Validation(("eventDate", "The event date cannot be in the past.")), typeof(EventFormViewModel));

        Assert.Equal("The event date cannot be in the past.", modelState["EventDate"]!.Errors.Single().ErrorMessage);
    }

    [Fact]
    public void Nested_category_errors_are_mapped_onto_the_first_category_fields()
    {
        var modelState = new ModelStateDictionary();
        var map = new Dictionary<string, string> { ["categories[0]."] = "FirstCategory." };

        modelState.AddApiErrors(Validation(("categories[0].distanceKm", "Too long.")), typeof(EventFormViewModel), map);

        Assert.Equal("Too long.", modelState["FirstCategory.DistanceKm"]!.Errors.Single().ErrorMessage);
    }

    [Fact]
    public void Fields_the_form_does_not_have_are_shown_above_the_form()
    {
        var modelState = new ModelStateDictionary();

        modelState.AddApiErrors(Validation(("$.somethingElse", "The request was malformed.")), typeof(EventFormViewModel));

        Assert.Equal("The request was malformed.", modelState[string.Empty]!.Errors.Single().ErrorMessage);
    }

    [Fact]
    public void A_failure_without_field_errors_becomes_a_form_level_message()
    {
        var modelState = new ModelStateDictionary();

        modelState.AddApiErrors(new ApiResult { StatusCode = HttpStatusCode.Conflict, Error = "This event is Completed and can no longer be edited." });

        Assert.Equal("This event is Completed and can no longer be edited.", modelState[string.Empty]!.Errors.Single().ErrorMessage);
    }

    [Fact]
    public void A_prefix_places_errors_inside_a_sub_form()
    {
        var modelState = new ModelStateDictionary();

        modelState.AddApiErrors(Validation(("newPassword", "Too weak.")), typeof(RaceDay.Web.Models.PasswordInput), prefix: "Password.");

        Assert.Equal("Too weak.", modelState["Password.NewPassword"]!.Errors.Single().ErrorMessage);
    }
}
