using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814

namespace RaceDay.Api.Data.Migrations
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    RoleId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleName = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    Description = table.Column<string>(type: "varchar(150)", unicode: false, maxLength: 150, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.RoleId);
                    table.CheckConstraint("CK_Roles_RoleName", "[RoleName] IN ('Organiser', 'Participant')");
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleId = table.Column<int>(type: "int", nullable: false),
                    FirstName = table.Column<string>(type: "varchar(60)", unicode: false, maxLength: 60, nullable: false),
                    LastName = table.Column<string>(type: "varchar(60)", unicode: false, maxLength: 60, nullable: false),
                    Email = table.Column<string>(type: "varchar(150)", unicode: false, maxLength: 150, nullable: false),
                    PasswordHash = table.Column<string>(type: "varchar(256)", unicode: false, maxLength: 256, nullable: false),
                    PhoneNumber = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    DateOfBirth = table.Column<DateOnly>(type: "date", nullable: false),
                    Gender = table.Column<string>(type: "char(1)", unicode: false, fixedLength: true, maxLength: 1, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValueSql: "1")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.UserId);
                    table.CheckConstraint("CK_Users_DOB", "[DateOfBirth] < CAST(SYSUTCDATETIME() AS DATE)");
                    table.CheckConstraint("CK_Users_Email", "[Email] LIKE '%_@_%._%'");
                    table.CheckConstraint("CK_Users_Gender", "[Gender] IN ('M', 'F', 'O')");
                    table.ForeignKey(
                        name: "FK_Users_Roles",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "RoleId");
                });

            migrationBuilder.CreateTable(
                name: "Events",
                columns: table => new
                {
                    EventId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrganiserId = table.Column<int>(type: "int", nullable: false),
                    EventName = table.Column<string>(type: "varchar(120)", unicode: false, maxLength: 120, nullable: false),
                    Description = table.Column<string>(type: "varchar(1000)", unicode: false, maxLength: 1000, nullable: true),
                    EventDate = table.Column<DateOnly>(type: "date", nullable: false),
                    StartTime = table.Column<TimeOnly>(type: "time(0)", nullable: false),
                    Province = table.Column<string>(type: "varchar(40)", unicode: false, maxLength: 40, nullable: false),
                    City = table.Column<string>(type: "varchar(80)", unicode: false, maxLength: 80, nullable: false),
                    VenueName = table.Column<string>(type: "varchar(120)", unicode: false, maxLength: 120, nullable: false),
                    Latitude = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: false),
                    Longitude = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: false),
                    RegistrationOpens = table.Column<DateOnly>(type: "date", nullable: false),
                    RegistrationCloses = table.Column<DateOnly>(type: "date", nullable: false),
                    EventStatus = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false, defaultValue: "Draft"),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Events", x => x.EventId);
                    table.CheckConstraint("CK_Events_Latitude", "[Latitude] BETWEEN -90 AND 90");
                    table.CheckConstraint("CK_Events_Longitude", "[Longitude] BETWEEN -180 AND 180");
                    table.CheckConstraint("CK_Events_Province", "[Province] IN ('Eastern Cape', 'Free State', 'Gauteng', 'KwaZulu-Natal', 'Limpopo', 'Mpumalanga', 'North West', 'Northern Cape', 'Western Cape')");
                    table.CheckConstraint("CK_Events_RegBeforeRun", "[EventDate] >= [RegistrationCloses]");
                    table.CheckConstraint("CK_Events_RegWindow", "[RegistrationCloses] >= [RegistrationOpens]");
                    table.CheckConstraint("CK_Events_Status", "[EventStatus] IN ('Draft', 'Published', 'Cancelled', 'Completed')");
                    table.ForeignKey(
                        name: "FK_Events_Organiser",
                        column: x => x.OrganiserId,
                        principalTable: "Users",
                        principalColumn: "UserId");
                });

            migrationBuilder.CreateTable(
                name: "EventCategories",
                columns: table => new
                {
                    CategoryId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EventId = table.Column<int>(type: "int", nullable: false),
                    CategoryName = table.Column<string>(type: "varchar(80)", unicode: false, maxLength: 80, nullable: false),
                    Discipline = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: false),
                    DistanceKm = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: false),
                    EntryFee = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    MaxParticipants = table.Column<int>(type: "int", nullable: false),
                    StartTime = table.Column<TimeOnly>(type: "time(0)", nullable: false),
                    MinimumAge = table.Column<int>(type: "int", nullable: false, defaultValue: 12)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventCategories", x => x.CategoryId);
                    table.CheckConstraint("CK_EventCategories_Disc", "[Discipline] IN ('Run', 'Walk', 'Cycle')");
                    table.CheckConstraint("CK_EventCategories_Distance", "[DistanceKm] > 0");
                    table.CheckConstraint("CK_EventCategories_Fee", "[EntryFee] >= 0");
                    table.CheckConstraint("CK_EventCategories_Max", "[MaxParticipants] > 0");
                    table.CheckConstraint("CK_EventCategories_MinAge", "[MinimumAge] BETWEEN 0 AND 100");
                    table.ForeignKey(
                        name: "FK_EventCategories_Events",
                        column: x => x.EventId,
                        principalTable: "Events",
                        principalColumn: "EventId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WeatherSnapshots",
                columns: table => new
                {
                    SnapshotId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EventId = table.Column<int>(type: "int", nullable: false),
                    CapturedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    TemperatureC = table.Column<decimal>(type: "decimal(4,1)", precision: 4, scale: 1, nullable: false),
                    FeelsLikeC = table.Column<decimal>(type: "decimal(4,1)", precision: 4, scale: 1, nullable: true),
                    Conditions = table.Column<string>(type: "varchar(60)", unicode: false, maxLength: 60, nullable: false),
                    WindSpeedKph = table.Column<decimal>(type: "decimal(5,1)", precision: 5, scale: 1, nullable: true),
                    HumidityPercent = table.Column<byte>(type: "tinyint", nullable: true),
                    ChanceOfRainPct = table.Column<byte>(type: "tinyint", nullable: true),
                    Source = table.Column<string>(type: "varchar(60)", unicode: false, maxLength: 60, nullable: false, defaultValue: "OpenWeatherMap")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WeatherSnapshots", x => x.SnapshotId);
                    table.CheckConstraint("CK_WeatherSnapshots_Humidity", "[HumidityPercent] IS NULL OR [HumidityPercent] <= 100");
                    table.CheckConstraint("CK_WeatherSnapshots_Rain", "[ChanceOfRainPct] IS NULL OR [ChanceOfRainPct] <= 100");
                    table.CheckConstraint("CK_WeatherSnapshots_Temp", "[TemperatureC] BETWEEN -30 AND 60");
                    table.CheckConstraint("CK_WeatherSnapshots_Wind", "[WindSpeedKph] IS NULL OR [WindSpeedKph] >= 0");
                    table.ForeignKey(
                        name: "FK_WeatherSnapshots_Events",
                        column: x => x.EventId,
                        principalTable: "Events",
                        principalColumn: "EventId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Enrolments",
                columns: table => new
                {
                    EnrolmentId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ParticipantId = table.Column<int>(type: "int", nullable: false),
                    CategoryId = table.Column<int>(type: "int", nullable: false),
                    RaceNumber = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: false),
                    EnrolmentDate = table.Column<DateTime>(type: "datetime2(0)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    EnrolmentStatus = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false, defaultValue: "Pending"),
                    AmountPaid = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false, defaultValueSql: "0"),
                    PaymentReference = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    TShirtSize = table.Column<string>(type: "varchar(5)", unicode: false, maxLength: 5, nullable: true),
                    EmergencyContactName = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    EmergencyContactPhone = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Enrolments", x => x.EnrolmentId);
                    table.CheckConstraint("CK_Enrolments_AmountPaid", "[AmountPaid] >= 0");
                    table.CheckConstraint("CK_Enrolments_Status", "[EnrolmentStatus] IN ('Pending', 'Confirmed', 'Cancelled', 'Withdrawn')");
                    table.CheckConstraint("CK_Enrolments_TShirtSize", "[TShirtSize] IS NULL OR [TShirtSize] IN ('XS', 'S', 'M', 'L', 'XL', 'XXL')");
                    table.ForeignKey(
                        name: "FK_Enrolments_Categories",
                        column: x => x.CategoryId,
                        principalTable: "EventCategories",
                        principalColumn: "CategoryId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Enrolments_Users",
                        column: x => x.ParticipantId,
                        principalTable: "Users",
                        principalColumn: "UserId");
                });

            migrationBuilder.CreateTable(
                name: "RouteWaypoints",
                columns: table => new
                {
                    WaypointId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CategoryId = table.Column<int>(type: "int", nullable: false),
                    SequenceNumber = table.Column<int>(type: "int", nullable: false),
                    WaypointName = table.Column<string>(type: "varchar(80)", unicode: false, maxLength: 80, nullable: false),
                    WaypointType = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    Latitude = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: false),
                    Longitude = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: false),
                    DistanceFromStartKm = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RouteWaypoints", x => x.WaypointId);
                    table.CheckConstraint("CK_RouteWaypoints_Distance", "[DistanceFromStartKm] >= 0");
                    table.CheckConstraint("CK_RouteWaypoints_Latitude", "[Latitude] BETWEEN -90 AND 90");
                    table.CheckConstraint("CK_RouteWaypoints_Longitude", "[Longitude] BETWEEN -180 AND 180");
                    table.CheckConstraint("CK_RouteWaypoints_Sequence", "[SequenceNumber] > 0");
                    table.CheckConstraint("CK_RouteWaypoints_Type", "[WaypointType] IN ('Start', 'WaterPoint', 'Medical', 'Timing', 'Finish')");
                    table.ForeignKey(
                        name: "FK_RouteWaypoints_Categories",
                        column: x => x.CategoryId,
                        principalTable: "EventCategories",
                        principalColumn: "CategoryId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Results",
                columns: table => new
                {
                    ResultId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EnrolmentId = table.Column<int>(type: "int", nullable: false),
                    CapturedByUserId = table.Column<int>(type: "int", nullable: false),
                    StartTime = table.Column<TimeOnly>(type: "time(0)", nullable: false),
                    FinishTime = table.Column<TimeOnly>(type: "time(0)", nullable: true),
                    ChipTimeSeconds = table.Column<int>(type: "int", nullable: true),
                    OverallPosition = table.Column<int>(type: "int", nullable: true),
                    GenderPosition = table.Column<int>(type: "int", nullable: true),
                    AgeGroupPosition = table.Column<int>(type: "int", nullable: true),
                    ResultStatus = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: false),
                    CapturedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Results", x => x.ResultId);
                    table.CheckConstraint("CK_Results_ChipTime", "[ChipTimeSeconds] IS NULL OR [ChipTimeSeconds] > 0");
                    table.CheckConstraint("CK_Results_Consistency", "([ResultStatus] = 'Finished' AND [FinishTime] IS NOT NULL AND [ChipTimeSeconds] IS NOT NULL AND [OverallPosition] IS NOT NULL) OR ([ResultStatus] <> 'Finished' AND [FinishTime] IS NULL AND [ChipTimeSeconds] IS NULL AND [OverallPosition] IS NULL)");
                    table.CheckConstraint("CK_Results_Positions", "([OverallPosition] IS NULL OR [OverallPosition] > 0) AND ([GenderPosition] IS NULL OR [GenderPosition] > 0) AND ([AgeGroupPosition] IS NULL OR [AgeGroupPosition] > 0)");
                    table.CheckConstraint("CK_Results_Status", "[ResultStatus] IN ('Finished', 'DNF', 'DNS', 'DQ')");
                    table.ForeignKey(
                        name: "FK_Results_CapturedBy",
                        column: x => x.CapturedByUserId,
                        principalTable: "Users",
                        principalColumn: "UserId");
                    table.ForeignKey(
                        name: "FK_Results_Enrolments",
                        column: x => x.EnrolmentId,
                        principalTable: "Enrolments",
                        principalColumn: "EnrolmentId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "RoleId", "Description", "RoleName" },
                values: new object[,]
                {
                    { 1, "Creates and manages events, categories and results, and views all enrolments.", "Organiser" },
                    { 2, "Browses events, enters a category, and tracks personal enrolments and results.", "Participant" }
                });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "UserId", "CreatedAt", "DateOfBirth", "Email", "FirstName", "Gender", "IsActive", "LastName", "PasswordHash", "PhoneNumber", "RoleId" },
                values: new object[,]
                {
                    { 1, new DateTime(2025, 8, 1, 9, 14, 0, 0, DateTimeKind.Unspecified), new DateOnly(1985, 3, 12), "thandeka.mokoena@raceday.co.za", "Thandeka", "F", true, "Mokoena", "AQAAAAIAAYagAAAAEIYh2qVEAlSK/IZHvbqRTmtWeTX1T0hdVnFgWBMlekN9dGfeEpGdQzHC0zgtIt1Yzw==", "0821234567", 1 },
                    { 2, new DateTime(2025, 8, 3, 11, 40, 0, 0, DateTimeKind.Unspecified), new DateOnly(1979, 11, 2), "pieter.vanwyk@raceday.co.za", "Pieter", "M", true, "van Wyk", "AQAAAAIAAYagAAAAEN2dqAPP6dd/6qihG05QKUp6fNLdyK39tzBb58hHGueETOaheQRJO4gZHQqrsv7M2g==", "0834567890", 1 },
                    { 3, new DateTime(2025, 9, 10, 18, 22, 0, 0, DateTimeKind.Unspecified), new DateOnly(1994, 7, 21), "sipho.ndlovu@gmail.com", "Sipho", "M", true, "Ndlovu", "AQAAAAIAAYagAAAAEJk3CqPqLIh5qFkOExKAN+OGhp/tiEI7M1k5tk1gwRl+fe9qF/YpuAMl/rwO3S7Eyg==", "0736548921", 2 },
                    { 4, new DateTime(2025, 9, 12, 7, 5, 0, 0, DateTimeKind.Unspecified), new DateOnly(1998, 2, 14), "lerato.dlamini@outlook.com", "Lerato", "F", true, "Dlamini", "AQAAAAIAAYagAAAAEGVEKjpyZg9iVwEF/5fDxMcnEH6PQyCeSgsohakhE2axFYyIg2IRLXW9NzzPhYNNFg==", "0791472583", 2 },
                    { 5, new DateTime(2025, 10, 2, 20, 31, 0, 0, DateTimeKind.Unspecified), new DateOnly(1991, 9, 30), "ayesha.patel@gmail.com", "Ayesha", "F", true, "Patel", "AQAAAAIAAYagAAAAEBP7zct/VdEyskDacqmeRC4nwJBNWNd+aRmVFZO3WSHdC4JmgZKuDydQ91hUHZAaFw==", "0827418529", 2 },
                    { 6, new DateTime(2025, 10, 19, 13, 47, 0, 0, DateTimeKind.Unspecified), new DateOnly(1986, 5, 8), "johan.botha@webmail.co.za", "Johan", "M", true, "Botha", "AQAAAAIAAYagAAAAECpf8cLmv7uYGqgKZtaqM/MvnGAXaGXYQdP3kdfOb7z7aeYBBNCygjsieDKV/iisUA==", "0845962137", 2 },
                    { 7, new DateTime(2026, 1, 8, 16, 12, 0, 0, DateTimeKind.Unspecified), new DateOnly(2000, 1, 19), "nomsa.khumalo@gmail.com", "Nomsa", "F", true, "Khumalo", "AQAAAAIAAYagAAAAECjFhgu/cE1TbpRnsM5GZCf32Rvj+FDKgo4bcySpNljseBQmBzAYQT3vY3Q42Y89rA==", "0713698520", 2 },
                    { 8, new DateTime(2026, 2, 21, 8, 58, 0, 0, DateTimeKind.Unspecified), new DateOnly(1996, 12, 3), "daniel.oosthuizen@icloud.com", "Daniel", "M", true, "Oosthuizen", "AQAAAAIAAYagAAAAEH313Ipnz+CaDCymCQMDNujrv0MNPDOWk5fww4wI85PaYSjhiHKgUgWLnRyIypRILA==", "0765432109", 2 }
                });

            migrationBuilder.InsertData(
                table: "Events",
                columns: new[] { "EventId", "City", "CreatedAt", "Description", "EventDate", "EventName", "EventStatus", "Latitude", "Longitude", "OrganiserId", "Province", "RegistrationCloses", "RegistrationOpens", "StartTime", "VenueName" },
                values: new object[,]
                {
                    { 1, "Durban", new DateTime(2025, 8, 20, 10, 0, 0, 0, DateTimeKind.Unspecified), "The 2026 Up Run from Durban City Hall to Scottsville Racecourse in Pietermaritzburg. A qualifying marathon time is required for the ultra marathon category.", new DateOnly(2026, 6, 14), "Comrades Marathon 2026", "Completed", -29.858700m, 31.021800m, 1, "KwaZulu-Natal", new DateOnly(2026, 5, 15), new DateOnly(2025, 9, 1), new TimeOnly(5, 30, 0), "Durban City Hall" },
                    { 2, "Soweto", new DateTime(2026, 5, 18, 14, 25, 0, 0, DateTimeKind.Unspecified), "The People's Race. Three distances starting and finishing at FNB Stadium and taking in Orlando Towers, the Hector Pieterson Memorial and Vilakazi Street.", new DateOnly(2026, 11, 1), "Soweto Marathon 2026", "Published", -26.234800m, 27.982100m, 2, "Gauteng", new DateOnly(2026, 10, 20), new DateOnly(2026, 6, 1), new TimeOnly(6, 0, 0), "FNB Stadium" },
                    { 3, "Cape Town", new DateTime(2026, 7, 22, 9, 10, 0, 0, DateTimeKind.Unspecified), "The largest timed cycle race in the world. Routes leave the Grand Parade and run the Peninsula via Chapman's Peak Drive and Suikerbossie.", new DateOnly(2027, 3, 14), "Cape Town Cycle Tour 2027", "Published", -33.925800m, 18.423200m, 1, "Western Cape", new DateOnly(2027, 2, 20), new DateOnly(2026, 8, 1), new TimeOnly(6, 15, 0), "Grand Parade" }
                });

            migrationBuilder.InsertData(
                table: "EventCategories",
                columns: new[] { "CategoryId", "CategoryName", "Discipline", "DistanceKm", "EntryFee", "EventId", "MaxParticipants", "MinimumAge", "StartTime" },
                values: new object[,]
                {
                    { 1, "Up Run 87.7km Ultra Marathon", "Run", 87.70m, 1250.00m, 1, 25000, 20, new TimeOnly(5, 30, 0) },
                    { 2, "Comrades Community 10km", "Run", 10.00m, 180.00m, 1, 3000, 12, new TimeOnly(7, 0, 0) },
                    { 3, "42.2km Marathon", "Run", 42.20m, 450.00m, 2, 8000, 18, new TimeOnly(6, 0, 0) },
                    { 4, "21.1km Half Marathon", "Run", 21.10m, 300.00m, 2, 12000, 16, new TimeOnly(6, 30, 0) },
                    { 5, "10km Race Walk", "Walk", 10.00m, 180.00m, 2, 15000, 12, new TimeOnly(7, 0, 0) },
                    { 6, "109km Race Tour", "Cycle", 109.00m, 890.00m, 3, 30000, 16, new TimeOnly(6, 15, 0) },
                    { 7, "42km Lite Tour", "Cycle", 42.00m, 520.00m, 3, 5000, 12, new TimeOnly(9, 0, 0) },
                    { 8, "10km Junior Ride", "Cycle", 10.00m, 150.00m, 3, 2000, 8, new TimeOnly(11, 0, 0) }
                });

            migrationBuilder.InsertData(
                table: "WeatherSnapshots",
                columns: new[] { "SnapshotId", "CapturedAt", "ChanceOfRainPct", "Conditions", "EventId", "FeelsLikeC", "HumidityPercent", "Source", "TemperatureC", "WindSpeedKph" },
                values: new object[,]
                {
                    { 1, new DateTime(2026, 6, 14, 6, 0, 0, 0, DateTimeKind.Unspecified), (byte)5, "Clear", 1, 13.0m, (byte)72, "OpenWeatherMap", 14.2m, 8.5m },
                    { 2, new DateTime(2026, 6, 14, 10, 0, 0, 0, DateTimeKind.Unspecified), (byte)0, "Sunny", 1, 23.4m, (byte)48, "OpenWeatherMap", 22.8m, 14.2m },
                    { 3, new DateTime(2026, 9, 20, 7, 0, 0, 0, DateTimeKind.Unspecified), (byte)20, "Partly cloudy", 2, 18.0m, (byte)55, "OpenWeatherMap", 18.6m, 12.0m },
                    { 4, new DateTime(2026, 9, 22, 7, 0, 0, 0, DateTimeKind.Unspecified), (byte)10, "Sunny", 2, 21.0m, (byte)41, "OpenWeatherMap", 21.3m, 9.4m },
                    { 5, new DateTime(2026, 9, 22, 7, 0, 0, 0, DateTimeKind.Unspecified), (byte)65, "Light rain", 3, 16.8m, (byte)81, "OpenWeatherMap", 17.9m, 26.5m }
                });

            migrationBuilder.InsertData(
                table: "Enrolments",
                columns: new[] { "EnrolmentId", "AmountPaid", "CategoryId", "EmergencyContactName", "EmergencyContactPhone", "EnrolmentDate", "EnrolmentStatus", "ParticipantId", "PaymentReference", "RaceNumber", "TShirtSize" },
                values: new object[,]
                {
                    { 1, 1250.00m, 1, "Nandi Ndlovu", "0721112233", new DateTime(2026, 2, 10, 19, 24, 0, 0, DateTimeKind.Unspecified), "Confirmed", 3, "PAY-CM-000101", "CM1042", "L" },
                    { 2, 1250.00m, 1, "Thabo Dlamini", "0734445566", new DateTime(2026, 3, 2, 8, 11, 0, 0, DateTimeKind.Unspecified), "Confirmed", 4, "PAY-CM-000102", "CM2288", "S" },
                    { 3, 1250.00m, 1, "Marie Botha", "0827778899", new DateTime(2026, 1, 28, 21, 50, 0, 0, DateTimeKind.Unspecified), "Confirmed", 6, "PAY-CM-000103", "CM3315", "XL" },
                    { 4, 1250.00m, 1, "Elsa Oosthuizen", "0798887766", new DateTime(2026, 4, 5, 12, 35, 0, 0, DateTimeKind.Unspecified), "Confirmed", 8, "PAY-CM-000104", "CM4408", "M" },
                    { 5, 180.00m, 2, "Riaz Patel", "0836549871", new DateTime(2026, 4, 18, 17, 2, 0, 0, DateTimeKind.Unspecified), "Confirmed", 5, "PAY-CM-000105", "CC0071", "M" },
                    { 6, 180.00m, 2, "Zanele Khumalo", "0713214567", new DateTime(2026, 4, 19, 9, 47, 0, 0, DateTimeKind.Unspecified), "Confirmed", 7, "PAY-CM-000106", "CC0072", "S" },
                    { 7, 450.00m, 3, "Nandi Ndlovu", "0721112233", new DateTime(2026, 8, 5, 6, 30, 0, 0, DateTimeKind.Unspecified), "Confirmed", 3, "PAY-SM-000201", "SM0450", "L" },
                    { 8, 450.00m, 3, "Elsa Oosthuizen", "0798887766", new DateTime(2026, 8, 11, 14, 18, 0, 0, DateTimeKind.Unspecified), "Confirmed", 8, "PAY-SM-000202", "SM0451", "M" },
                    { 9, 300.00m, 4, "Thabo Dlamini", "0734445566", new DateTime(2026, 9, 1, 10, 9, 0, 0, DateTimeKind.Unspecified), "Confirmed", 4, "PAY-SM-000203", "SH1120", "S" },
                    { 10, 0.00m, 5, "Zanele Khumalo", "0713214567", new DateTime(2026, 9, 12, 19, 55, 0, 0, DateTimeKind.Unspecified), "Pending", 7, null, "SW2001", "S" },
                    { 11, 890.00m, 6, "Marie Botha", "0827778899", new DateTime(2026, 9, 5, 11, 23, 0, 0, DateTimeKind.Unspecified), "Confirmed", 6, "PAY-CT-000301", "CT0910", "XL" },
                    { 12, 890.00m, 6, "Riaz Patel", "0836549871", new DateTime(2026, 9, 15, 15, 41, 0, 0, DateTimeKind.Unspecified), "Confirmed", 5, "PAY-CT-000302", "CT0911", "M" },
                    { 13, 520.00m, 7, "Elsa Oosthuizen", "0798887766", new DateTime(2026, 9, 18, 7, 16, 0, 0, DateTimeKind.Unspecified), "Cancelled", 8, "PAY-CT-000303", "CL0330", "M" }
                });

            migrationBuilder.InsertData(
                table: "RouteWaypoints",
                columns: new[] { "WaypointId", "CategoryId", "DistanceFromStartKm", "Latitude", "Longitude", "SequenceNumber", "WaypointName", "WaypointType" },
                values: new object[,]
                {
                    { 1, 1, 0.00m, -29.858700m, 31.021800m, 1, "Durban City Hall", "Start" },
                    { 2, 1, 14.50m, -29.834100m, 30.886900m, 2, "Cowies Hill", "WaterPoint" },
                    { 3, 1, 22.30m, -29.802500m, 30.813700m, 3, "Fields Hill", "WaterPoint" },
                    { 4, 1, 33.80m, -29.769200m, 30.745200m, 4, "Bothas Hill", "Medical" },
                    { 5, 1, 43.85m, -29.755300m, 30.688900m, 5, "Drummond Halfway", "Timing" },
                    { 6, 1, 50.20m, -29.740600m, 30.652200m, 6, "Inchanga", "WaterPoint" },
                    { 7, 1, 64.70m, -29.732500m, 30.539000m, 7, "Camperdown", "Medical" },
                    { 8, 1, 79.60m, -29.649700m, 30.408300m, 8, "Polly Shortts", "WaterPoint" },
                    { 9, 1, 87.70m, -29.622200m, 30.380300m, 9, "Scottsville Racecourse", "Finish" },
                    { 10, 6, 0.00m, -33.925800m, 18.423200m, 1, "Grand Parade", "Start" },
                    { 11, 6, 26.40m, -34.108600m, 18.470000m, 2, "Muizenberg", "WaterPoint" },
                    { 12, 6, 38.90m, -34.193900m, 18.432500m, 3, "Simons Town", "Medical" },
                    { 13, 6, 46.20m, -34.250000m, 18.450000m, 4, "Smitswinkel Bay", "Timing" },
                    { 14, 6, 54.80m, -34.227800m, 18.390000m, 5, "Misty Cliffs", "WaterPoint" },
                    { 15, 6, 76.50m, -34.083300m, 18.358300m, 6, "Chapmans Peak Drive", "Medical" },
                    { 16, 6, 92.10m, -34.045000m, 18.358300m, 7, "Suikerbossie", "WaterPoint" },
                    { 17, 6, 109.00m, -33.903600m, 18.410600m, 8, "Green Point", "Finish" }
                });

            migrationBuilder.InsertData(
                table: "Results",
                columns: new[] { "ResultId", "AgeGroupPosition", "CapturedAt", "CapturedByUserId", "ChipTimeSeconds", "EnrolmentId", "FinishTime", "GenderPosition", "OverallPosition", "ResultStatus", "StartTime" },
                values: new object[,]
                {
                    { 1, 1, new DateTime(2026, 6, 14, 18, 40, 0, 0, DateTimeKind.Unspecified), 1, 20567, 1, new TimeOnly(11, 12, 47), 1, 1, "Finished", new TimeOnly(5, 30, 0) },
                    { 2, 1, new DateTime(2026, 6, 14, 18, 40, 0, 0, DateTimeKind.Unspecified), 1, 23283, 3, new TimeOnly(11, 58, 3), 2, 2, "Finished", new TimeOnly(5, 30, 0) },
                    { 3, 1, new DateTime(2026, 6, 14, 18, 41, 0, 0, DateTimeKind.Unspecified), 1, 26059, 2, new TimeOnly(12, 44, 19), 1, 3, "Finished", new TimeOnly(5, 30, 0) },
                    { 4, null, new DateTime(2026, 6, 14, 19, 5, 0, 0, DateTimeKind.Unspecified), 1, null, 4, null, null, null, "DNF", new TimeOnly(5, 30, 0) },
                    { 5, 1, new DateTime(2026, 6, 14, 12, 15, 0, 0, DateTimeKind.Unspecified), 1, 2892, 5, new TimeOnly(7, 48, 12), 1, 1, "Finished", new TimeOnly(7, 0, 0) },
                    { 6, 1, new DateTime(2026, 6, 14, 12, 15, 0, 0, DateTimeKind.Unspecified), 1, 3340, 6, new TimeOnly(7, 55, 40), 2, 2, "Finished", new TimeOnly(7, 0, 0) }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Enrolments_CategoryId",
                table: "Enrolments",
                column: "CategoryId")
                .Annotation("SqlServer:Include", new[] { "EnrolmentStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_Enrolments_ParticipantId",
                table: "Enrolments",
                column: "ParticipantId");

            migrationBuilder.CreateIndex(
                name: "UQ_Enrolments_OnePerCat",
                table: "Enrolments",
                columns: new[] { "ParticipantId", "CategoryId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_Enrolments_RaceNumber",
                table: "Enrolments",
                columns: new[] { "CategoryId", "RaceNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EventCategories_EventId",
                table: "EventCategories",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "UQ_EventCategories_Name",
                table: "EventCategories",
                columns: new[] { "EventId", "CategoryName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Events_EventDate",
                table: "Events",
                column: "EventDate")
                .Annotation("SqlServer:Include", new[] { "EventStatus", "Province" });

            migrationBuilder.CreateIndex(
                name: "IX_Events_OrganiserId",
                table: "Events",
                column: "OrganiserId");

            migrationBuilder.CreateIndex(
                name: "UQ_Events_NameDate",
                table: "Events",
                columns: new[] { "EventName", "EventDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Results_CapturedByUserId",
                table: "Results",
                column: "CapturedByUserId");

            migrationBuilder.CreateIndex(
                name: "UQ_Results_Enrolment",
                table: "Results",
                column: "EnrolmentId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_Roles_RoleName",
                table: "Roles",
                column: "RoleName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RouteWaypoints_CategoryId",
                table: "RouteWaypoints",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "UQ_RouteWaypoints_Sequence",
                table: "RouteWaypoints",
                columns: new[] { "CategoryId", "SequenceNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_RoleId",
                table: "Users",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "UQ_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WeatherSnapshots_EventId",
                table: "WeatherSnapshots",
                columns: new[] { "EventId", "CapturedAt" },
                descending: new[] { false, true });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Results");

            migrationBuilder.DropTable(
                name: "RouteWaypoints");

            migrationBuilder.DropTable(
                name: "WeatherSnapshots");

            migrationBuilder.DropTable(
                name: "Enrolments");

            migrationBuilder.DropTable(
                name: "EventCategories");

            migrationBuilder.DropTable(
                name: "Events");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Roles");
        }
    }
}
