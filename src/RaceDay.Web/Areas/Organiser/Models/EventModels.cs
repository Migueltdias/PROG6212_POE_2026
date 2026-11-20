using System.ComponentModel.DataAnnotations;
using RaceDay.Web.ApiClient.Contracts;

namespace RaceDay.Web.Areas.Organiser.Models;

public sealed class EventFormViewModel
{
    public const long MaxBannerBytes = 5 * 1024 * 1024;

    public int? EventId { get; set; }

    [Required(ErrorMessage = "Give the event a name.")]
    [StringLength(120)]
    [Display(Name = "Event name")]
    public string EventName { get; set; } = string.Empty;

    [StringLength(1000)]
    [DataType(DataType.MultilineText)]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Choose the event date.")]
    [Display(Name = "Event date")]
    public DateOnly? EventDate { get; set; }

    [Required(ErrorMessage = "Enter the start time.")]
    [Display(Name = "Start time")]
    public TimeOnly? StartTime { get; set; }

    [Required(ErrorMessage = "Choose a province.")]
    public string Province { get; set; } = string.Empty;

    [Required(ErrorMessage = "Enter the town or city.")]
    [StringLength(80)]
    [Display(Name = "Town or city")]
    public string City { get; set; } = string.Empty;

    [Required(ErrorMessage = "Enter the venue.")]
    [StringLength(120)]
    [Display(Name = "Venue")]
    public string VenueName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Enter the latitude.")]
    [Range(-90, 90, ErrorMessage = "Latitude is between -90 and 90.")]
    public decimal? Latitude { get; set; }

    [Required(ErrorMessage = "Enter the longitude.")]
    [Range(-180, 180, ErrorMessage = "Longitude is between -180 and 180.")]
    public decimal? Longitude { get; set; }

    [Required(ErrorMessage = "Choose when entries open.")]
    [Display(Name = "Entries open")]
    public DateOnly? RegistrationOpens { get; set; }

    [Required(ErrorMessage = "Choose when entries close.")]
    [Display(Name = "Entries close")]
    public DateOnly? RegistrationCloses { get; set; }

    [Display(Name = "Banner image")]
    public IFormFile? Banner { get; set; }

    [Display(Name = "Remove the current banner")]
    public bool RemoveBanner { get; set; }

    public CategoryInput? FirstCategory { get; set; }

    public string? CurrentBannerUrl { get; set; }
    public string? EventStatus { get; set; }
    public List<Category> ExistingCategories { get; set; } = [];

    public bool IsEdit => EventId.HasValue;

    public static EventFormViewModel ForNewEvent(DateOnly today) => new()
    {
        EventDate = today.AddDays(60),
        StartTime = new TimeOnly(6, 0),
        RegistrationOpens = today,
        RegistrationCloses = today.AddDays(53),
        FirstCategory = CategoryInput.Default()
    };

    public static EventFormViewModel From(EventDetail ev) => new EventFormViewModel
    {
        EventId = ev.EventId,
        EventName = ev.EventName,
        Description = ev.Description,
        EventDate = ev.EventDate,
        StartTime = ev.StartTime,
        Province = ev.Province,
        City = ev.City,
        VenueName = ev.VenueName,
        Latitude = ev.Latitude,
        Longitude = ev.Longitude,
        RegistrationOpens = ev.RegistrationOpens,
        RegistrationCloses = ev.RegistrationCloses
    }.WithContext(ev);

    public EventFormViewModel WithContext(EventDetail ev)
    {
        EventId = ev.EventId;
        CurrentBannerUrl = ev.BannerImageUrl;
        EventStatus = ev.EventStatus;
        ExistingCategories = ev.Categories;
        return this;
    }

    public EventRequest ToRequest() => Fill(new EventRequest());

    public CreateEventRequest ToCreateRequest()
    {
        var request = Fill(new CreateEventRequest());
        request.Categories = FirstCategory is null ? null : [FirstCategory.ToRequest()];
        return request;
    }

    private T Fill<T>(T request) where T : EventRequest
    {
        request.EventName = EventName.Trim();
        request.Description = string.IsNullOrWhiteSpace(Description) ? null : Description.Trim();
        request.EventDate = EventDate;
        request.StartTime = StartTime;
        request.Province = Province;
        request.City = City.Trim();
        request.VenueName = VenueName.Trim();
        request.Latitude = Latitude;
        request.Longitude = Longitude;
        request.RegistrationOpens = RegistrationOpens;
        request.RegistrationCloses = RegistrationCloses;
        return request;
    }
}

public sealed record EventListViewModel(PagedResult<EventSummary> Page, string? Status, DateOnly Today);

public sealed record EventDetailsViewModel(EventDetail Event, DateOnly Today)
{
    public bool IsClosedForChanges => Event.EventStatus is EventStatuses.Cancelled or EventStatuses.Completed;
    public bool HasBeenRun => Event.EventDate <= Today;
    public int TotalPlaces => Event.Categories.Sum(c => c.MaxParticipants);
}
