using System.Net;
using System.Net.Http.Json;
using RaceDay.Api.Dtos;

namespace RaceDay.Api.Tests.Infrastructure;

public static class TestData
{
    public static object NewEvent(
        string? name = null,
        string eventDate = "2027-05-09",
        string registrationOpens = "2026-09-01",
        string registrationCloses = "2027-04-30",
        string province = "KwaZulu-Natal",
        object[]? categories = null) => new
    {
        eventName = name ?? $"Test Event {Guid.NewGuid():N}",
        description = "Created by an automated test.",
        eventDate,
        startTime = "06:30:00",
        province,
        city = "Durban",
        venueName = "Suncoast Promenade",
        latitude = -29.8395m,
        longitude = 31.0352m,
        registrationOpens,
        registrationCloses,
        categories
    };

    public static object NewCategory(
        string name = "10km Run",
        string discipline = "Run",
        decimal distanceKm = 10m,
        decimal entryFee = 150m,
        int maxParticipants = 500,
        int minimumAge = 12) => new
    {
        categoryName = name,
        discipline,
        distanceKm,
        entryFee,
        maxParticipants,
        startTime = "06:30:00",
        minimumAge
    };

    public static object NewWaypoint(int sequence, decimal distanceFromStartKm = 1m, string type = "WaterPoint") => new
    {
        sequenceNumber = sequence,
        waypointName = $"Point {sequence}",
        waypointType = type,
        latitude = -29.84m,
        longitude = 31.03m,
        distanceFromStartKm
    };

    public static object Entry(string? tShirtSize = "M") => new
    {
        tShirtSize,
        emergencyContactName = "Next Of Kin",
        emergencyContactPhone = "0821234567"
    };

    public static async Task<EventDetailDto> CreateEventAsync(HttpClient organiser, bool publish, params object[] categories)
    {
        var created = await organiser.PostAsJsonAsync("/api/events", NewEvent(categories: categories));
        await created.ShouldHaveStatusAsync(HttpStatusCode.Created);
        var ev = await created.ReadAsync<EventDetailDto>();

        if (publish)
        {
            var published = await organiser.PatchAsJsonAsync($"/api/events/{ev.EventId}/status", new { status = "Published" });
            await published.ShouldHaveStatusAsync(HttpStatusCode.OK);
            ev = await published.ReadAsync<EventDetailDto>();
        }

        return ev;
    }
}
