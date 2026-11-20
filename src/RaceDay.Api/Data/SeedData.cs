using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Models;

namespace RaceDay.Api.Data;

public static class SeedData
{
    public const string DemoPassword = "RaceDay@2026";

    public static void Apply(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Role>().HasData(
            new Role { RoleId = 1, RoleName = "Organiser", Description = "Creates and manages events, categories and results, and views all enrolments." },
            new Role { RoleId = 2, RoleName = "Participant", Description = "Browses events, enters a category, and tracks personal enrolments and results." });

        modelBuilder.Entity<User>().HasData(
            User(1, 1, "Thandeka", "Mokoena", "thandeka.mokoena@raceday.co.za", "AQAAAAIAAYagAAAAEIYh2qVEAlSK/IZHvbqRTmtWeTX1T0hdVnFgWBMlekN9dGfeEpGdQzHC0zgtIt1Yzw==", "0821234567", new(1985, 3, 12), "F", new(2025, 8, 1, 9, 14, 0)),
            User(2, 1, "Pieter", "van Wyk", "pieter.vanwyk@raceday.co.za", "AQAAAAIAAYagAAAAEN2dqAPP6dd/6qihG05QKUp6fNLdyK39tzBb58hHGueETOaheQRJO4gZHQqrsv7M2g==", "0834567890", new(1979, 11, 2), "M", new(2025, 8, 3, 11, 40, 0)),
            User(3, 2, "Sipho", "Ndlovu", "sipho.ndlovu@gmail.com", "AQAAAAIAAYagAAAAEJk3CqPqLIh5qFkOExKAN+OGhp/tiEI7M1k5tk1gwRl+fe9qF/YpuAMl/rwO3S7Eyg==", "0736548921", new(1994, 7, 21), "M", new(2025, 9, 10, 18, 22, 0)),
            User(4, 2, "Lerato", "Dlamini", "lerato.dlamini@outlook.com", "AQAAAAIAAYagAAAAEGVEKjpyZg9iVwEF/5fDxMcnEH6PQyCeSgsohakhE2axFYyIg2IRLXW9NzzPhYNNFg==", "0791472583", new(1998, 2, 14), "F", new(2025, 9, 12, 7, 5, 0)),
            User(5, 2, "Ayesha", "Patel", "ayesha.patel@gmail.com", "AQAAAAIAAYagAAAAEBP7zct/VdEyskDacqmeRC4nwJBNWNd+aRmVFZO3WSHdC4JmgZKuDydQ91hUHZAaFw==", "0827418529", new(1991, 9, 30), "F", new(2025, 10, 2, 20, 31, 0)),
            User(6, 2, "Johan", "Botha", "johan.botha@webmail.co.za", "AQAAAAIAAYagAAAAECpf8cLmv7uYGqgKZtaqM/MvnGAXaGXYQdP3kdfOb7z7aeYBBNCygjsieDKV/iisUA==", "0845962137", new(1986, 5, 8), "M", new(2025, 10, 19, 13, 47, 0)),
            User(7, 2, "Nomsa", "Khumalo", "nomsa.khumalo@gmail.com", "AQAAAAIAAYagAAAAECjFhgu/cE1TbpRnsM5GZCf32Rvj+FDKgo4bcySpNljseBQmBzAYQT3vY3Q42Y89rA==", "0713698520", new(2000, 1, 19), "F", new(2026, 1, 8, 16, 12, 0)),
            User(8, 2, "Daniel", "Oosthuizen", "daniel.oosthuizen@icloud.com", "AQAAAAIAAYagAAAAEH313Ipnz+CaDCymCQMDNujrv0MNPDOWk5fww4wI85PaYSjhiHKgUgWLnRyIypRILA==", "0765432109", new(1996, 12, 3), "M", new(2026, 2, 21, 8, 58, 0)));

        modelBuilder.Entity<Event>().HasData(
            new Event
            {
                EventId = 1, OrganiserId = 1, EventName = "Comrades Marathon 2026",
                Description = "The 2026 Up Run from Durban City Hall to Scottsville Racecourse in Pietermaritzburg. " +
                              "A qualifying marathon time is required for the ultra marathon category.",
                EventDate = new(2026, 6, 14), StartTime = new(5, 30), Province = "KwaZulu-Natal", City = "Durban",
                VenueName = "Durban City Hall", Latitude = -29.858700m, Longitude = 31.021800m,
                RegistrationOpens = new(2025, 9, 1), RegistrationCloses = new(2026, 5, 15),
                EventStatus = "Completed", CreatedAt = new(2025, 8, 20, 10, 0, 0)
            },
            new Event
            {
                EventId = 2, OrganiserId = 2, EventName = "Soweto Marathon 2026",
                Description = "The People's Race. Three distances starting and finishing at FNB Stadium and taking in " +
                              "Orlando Towers, the Hector Pieterson Memorial and Vilakazi Street.",
                EventDate = new(2026, 11, 1), StartTime = new(6, 0), Province = "Gauteng", City = "Soweto",
                VenueName = "FNB Stadium", Latitude = -26.234800m, Longitude = 27.982100m,
                RegistrationOpens = new(2026, 6, 1), RegistrationCloses = new(2026, 10, 20),
                EventStatus = "Published", CreatedAt = new(2026, 5, 18, 14, 25, 0)
            },
            new Event
            {
                EventId = 3, OrganiserId = 1, EventName = "Cape Town Cycle Tour 2027",
                Description = "The largest timed cycle race in the world. Routes leave the Grand Parade and run the " +
                              "Peninsula via Chapman's Peak Drive and Suikerbossie.",
                EventDate = new(2027, 3, 14), StartTime = new(6, 15), Province = "Western Cape", City = "Cape Town",
                VenueName = "Grand Parade", Latitude = -33.925800m, Longitude = 18.423200m,
                RegistrationOpens = new(2026, 8, 1), RegistrationCloses = new(2027, 2, 20),
                EventStatus = "Published", CreatedAt = new(2026, 7, 22, 9, 10, 0)
            });

        modelBuilder.Entity<EventCategory>().HasData(
            Category(1, 1, "Up Run 87.7km Ultra Marathon", "Run", 87.70m, 1250.00m, 25000, new(5, 30), 20),
            Category(2, 1, "Comrades Community 10km", "Run", 10.00m, 180.00m, 3000, new(7, 0), 12),
            Category(3, 2, "42.2km Marathon", "Run", 42.20m, 450.00m, 8000, new(6, 0), 18),
            Category(4, 2, "21.1km Half Marathon", "Run", 21.10m, 300.00m, 12000, new(6, 30), 16),
            Category(5, 2, "10km Race Walk", "Walk", 10.00m, 180.00m, 15000, new(7, 0), 12),
            Category(6, 3, "109km Race Tour", "Cycle", 109.00m, 890.00m, 30000, new(6, 15), 16),
            Category(7, 3, "42km Lite Tour", "Cycle", 42.00m, 520.00m, 5000, new(9, 0), 12),
            Category(8, 3, "10km Junior Ride", "Cycle", 10.00m, 150.00m, 2000, new(11, 0), 8));

        modelBuilder.Entity<RouteWaypoint>().HasData(
            Waypoint(1, 1, 1, "Durban City Hall", "Start", -29.858700m, 31.021800m, 0.00m),
            Waypoint(2, 1, 2, "Cowies Hill", "WaterPoint", -29.834100m, 30.886900m, 14.50m),
            Waypoint(3, 1, 3, "Fields Hill", "WaterPoint", -29.802500m, 30.813700m, 22.30m),
            Waypoint(4, 1, 4, "Bothas Hill", "Medical", -29.769200m, 30.745200m, 33.80m),
            Waypoint(5, 1, 5, "Drummond Halfway", "Timing", -29.755300m, 30.688900m, 43.85m),
            Waypoint(6, 1, 6, "Inchanga", "WaterPoint", -29.740600m, 30.652200m, 50.20m),
            Waypoint(7, 1, 7, "Camperdown", "Medical", -29.732500m, 30.539000m, 64.70m),
            Waypoint(8, 1, 8, "Polly Shortts", "WaterPoint", -29.649700m, 30.408300m, 79.60m),
            Waypoint(9, 1, 9, "Scottsville Racecourse", "Finish", -29.622200m, 30.380300m, 87.70m),
            Waypoint(10, 6, 1, "Grand Parade", "Start", -33.925800m, 18.423200m, 0.00m),
            Waypoint(11, 6, 2, "Muizenberg", "WaterPoint", -34.108600m, 18.470000m, 26.40m),
            Waypoint(12, 6, 3, "Simons Town", "Medical", -34.193900m, 18.432500m, 38.90m),
            Waypoint(13, 6, 4, "Smitswinkel Bay", "Timing", -34.250000m, 18.450000m, 46.20m),
            Waypoint(14, 6, 5, "Misty Cliffs", "WaterPoint", -34.227800m, 18.390000m, 54.80m),
            Waypoint(15, 6, 6, "Chapmans Peak Drive", "Medical", -34.083300m, 18.358300m, 76.50m),
            Waypoint(16, 6, 7, "Suikerbossie", "WaterPoint", -34.045000m, 18.358300m, 92.10m),
            Waypoint(17, 6, 8, "Green Point", "Finish", -33.903600m, 18.410600m, 109.00m));

        modelBuilder.Entity<Enrolment>().HasData(
            Enrolment(1, 3, 1, "CM1042", new(2026, 2, 10, 19, 24, 0), "Confirmed", 1250.00m, "PAY-CM-000101", "L", "Nandi Ndlovu", "0721112233"),
            Enrolment(2, 4, 1, "CM2288", new(2026, 3, 2, 8, 11, 0), "Confirmed", 1250.00m, "PAY-CM-000102", "S", "Thabo Dlamini", "0734445566"),
            Enrolment(3, 6, 1, "CM3315", new(2026, 1, 28, 21, 50, 0), "Confirmed", 1250.00m, "PAY-CM-000103", "XL", "Marie Botha", "0827778899"),
            Enrolment(4, 8, 1, "CM4408", new(2026, 4, 5, 12, 35, 0), "Confirmed", 1250.00m, "PAY-CM-000104", "M", "Elsa Oosthuizen", "0798887766"),
            Enrolment(5, 5, 2, "CC0071", new(2026, 4, 18, 17, 2, 0), "Confirmed", 180.00m, "PAY-CM-000105", "M", "Riaz Patel", "0836549871"),
            Enrolment(6, 7, 2, "CC0072", new(2026, 4, 19, 9, 47, 0), "Confirmed", 180.00m, "PAY-CM-000106", "S", "Zanele Khumalo", "0713214567"),
            Enrolment(7, 3, 3, "SM0450", new(2026, 8, 5, 6, 30, 0), "Confirmed", 450.00m, "PAY-SM-000201", "L", "Nandi Ndlovu", "0721112233"),
            Enrolment(8, 8, 3, "SM0451", new(2026, 8, 11, 14, 18, 0), "Confirmed", 450.00m, "PAY-SM-000202", "M", "Elsa Oosthuizen", "0798887766"),
            Enrolment(9, 4, 4, "SH1120", new(2026, 9, 1, 10, 9, 0), "Confirmed", 300.00m, "PAY-SM-000203", "S", "Thabo Dlamini", "0734445566"),
            Enrolment(10, 7, 5, "SW2001", new(2026, 9, 12, 19, 55, 0), "Pending", 0.00m, null, "S", "Zanele Khumalo", "0713214567"),
            Enrolment(11, 6, 6, "CT0910", new(2026, 9, 5, 11, 23, 0), "Confirmed", 890.00m, "PAY-CT-000301", "XL", "Marie Botha", "0827778899"),
            Enrolment(12, 5, 6, "CT0911", new(2026, 9, 15, 15, 41, 0), "Confirmed", 890.00m, "PAY-CT-000302", "M", "Riaz Patel", "0836549871"),
            Enrolment(13, 8, 7, "CL0330", new(2026, 9, 18, 7, 16, 0), "Cancelled", 520.00m, "PAY-CT-000303", "M", "Elsa Oosthuizen", "0798887766"));

        modelBuilder.Entity<Result>().HasData(
            Result(1, 1, new(5, 30), new(11, 12, 47), 20567, 1, 1, 1, "Finished", new(2026, 6, 14, 18, 40, 0)),
            Result(2, 3, new(5, 30), new(11, 58, 3), 23283, 2, 2, 1, "Finished", new(2026, 6, 14, 18, 40, 0)),
            Result(3, 2, new(5, 30), new(12, 44, 19), 26059, 3, 1, 1, "Finished", new(2026, 6, 14, 18, 41, 0)),
            Result(4, 4, new(5, 30), null, null, null, null, null, "DNF", new(2026, 6, 14, 19, 5, 0)),
            Result(5, 5, new(7, 0), new(7, 48, 12), 2892, 1, 1, 1, "Finished", new(2026, 6, 14, 12, 15, 0)),
            Result(6, 6, new(7, 0), new(7, 55, 40), 3340, 2, 2, 1, "Finished", new(2026, 6, 14, 12, 15, 0)));

        modelBuilder.Entity<WeatherSnapshot>().HasData(
            Weather(1, 1, new(2026, 6, 14, 6, 0, 0), 14.2m, 13.0m, "Clear", 8.5m, 72, 5),
            Weather(2, 1, new(2026, 6, 14, 10, 0, 0), 22.8m, 23.4m, "Sunny", 14.2m, 48, 0),
            Weather(3, 2, new(2026, 9, 20, 7, 0, 0), 18.6m, 18.0m, "Partly cloudy", 12.0m, 55, 20),
            Weather(4, 2, new(2026, 9, 22, 7, 0, 0), 21.3m, 21.0m, "Sunny", 9.4m, 41, 10),
            Weather(5, 3, new(2026, 9, 22, 7, 0, 0), 17.9m, 16.8m, "Light rain", 26.5m, 81, 65));
    }

    private static User User(int id, int roleId, string first, string last, string email, string hash,
        string phone, DateOnly dob, string gender, DateTime createdAt) => new()
    {
        UserId = id, RoleId = roleId, FirstName = first, LastName = last, Email = email, PasswordHash = hash,
        PhoneNumber = phone, DateOfBirth = dob, Gender = gender, CreatedAt = createdAt, IsActive = true
    };

    private static EventCategory Category(int id, int eventId, string name, string discipline, decimal distanceKm,
        decimal fee, int max, TimeOnly start, int minAge) => new()
    {
        CategoryId = id, EventId = eventId, CategoryName = name, Discipline = discipline, DistanceKm = distanceKm,
        EntryFee = fee, MaxParticipants = max, StartTime = start, MinimumAge = minAge
    };

    private static RouteWaypoint Waypoint(int id, int categoryId, int sequence, string name, string type,
        decimal lat, decimal lng, decimal fromStartKm) => new()
    {
        WaypointId = id, CategoryId = categoryId, SequenceNumber = sequence, WaypointName = name,
        WaypointType = type, Latitude = lat, Longitude = lng, DistanceFromStartKm = fromStartKm
    };

    private static Enrolment Enrolment(int id, int participantId, int categoryId, string raceNumber, DateTime enrolled,
        string status, decimal paid, string? paymentRef, string? shirt, string contactName, string contactPhone) => new()
    {
        EnrolmentId = id, ParticipantId = participantId, CategoryId = categoryId, RaceNumber = raceNumber,
        EnrolmentDate = enrolled, EnrolmentStatus = status, AmountPaid = paid, PaymentReference = paymentRef,
        TShirtSize = shirt, EmergencyContactName = contactName, EmergencyContactPhone = contactPhone
    };

    private static Result Result(int id, int enrolmentId, TimeOnly start, TimeOnly? finish, int? chipSeconds,
        int? overall, int? gender, int? ageGroup, string status, DateTime capturedAt) => new()
    {
        ResultId = id, EnrolmentId = enrolmentId, CapturedByUserId = 1, StartTime = start, FinishTime = finish,
        ChipTimeSeconds = chipSeconds, OverallPosition = overall, GenderPosition = gender,
        AgeGroupPosition = ageGroup, ResultStatus = status, CapturedAt = capturedAt
    };

    private static WeatherSnapshot Weather(int id, int eventId, DateTime capturedAt, decimal temp, decimal feelsLike,
        string conditions, decimal wind, byte humidity, byte rain) => new()
    {
        SnapshotId = id, EventId = eventId, CapturedAt = capturedAt, TemperatureC = temp, FeelsLikeC = feelsLike,
        Conditions = conditions, WindSpeedKph = wind, HumidityPercent = humidity, ChanceOfRainPct = rain,
        Source = "OpenWeatherMap"
    };
}
