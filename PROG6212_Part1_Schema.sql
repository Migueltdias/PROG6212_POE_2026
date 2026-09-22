-- RaceDay database script
-- PROG6212 POE Part 1, Section C
-- Written for SQL Server. Open in SSMS and press F5, it creates the database itself.
-- Can be run again, it drops and rebuilds everything each time.

-- Create the database if it is not there yet
USE master;
GO

IF DB_ID(N'RaceDay') IS NULL
BEGIN
    PRINT 'Creating database RaceDay...';
    CREATE DATABASE RaceDay;
END
ELSE
BEGIN
    PRINT 'Database RaceDay already exists - rebuilding its objects.';
END
GO

USE RaceDay;
GO

SET NOCOUNT ON;
GO

-- Drop children before parents, otherwise the foreign keys block it
DROP VIEW  IF EXISTS dbo.vw_CategoryLeaderboard;
DROP TABLE IF EXISTS dbo.Results;
DROP TABLE IF EXISTS dbo.Enrolments;
DROP TABLE IF EXISTS dbo.RouteWaypoints;
DROP TABLE IF EXISTS dbo.EventCategories;
DROP TABLE IF EXISTS dbo.WeatherSnapshots;
DROP TABLE IF EXISTS dbo.Events;
DROP TABLE IF EXISTS dbo.Users;
DROP TABLE IF EXISTS dbo.Roles;
GO

-- Tables

-- Roles in a table instead of just a CHECK, so Part 2 can read them from the database
CREATE TABLE dbo.Roles
(
    RoleId       INT           IDENTITY(1,1) NOT NULL,
    RoleName     VARCHAR(20)                 NOT NULL,
    Description  VARCHAR(150)                NOT NULL,

    CONSTRAINT PK_Roles          PRIMARY KEY (RoleId),
    CONSTRAINT UQ_Roles_RoleName UNIQUE      (RoleName),
    CONSTRAINT CK_Roles_RoleName CHECK       (RoleName IN ('Organiser', 'Participant'))
);
GO

-- One table for both roles. RoleId decides what the account can do.
CREATE TABLE dbo.Users
(
    UserId        INT           IDENTITY(1,1) NOT NULL,
    RoleId        INT                         NOT NULL,
    FirstName     VARCHAR(60)                 NOT NULL,
    LastName      VARCHAR(60)                 NOT NULL,
    Email         VARCHAR(150)                NOT NULL,
    PasswordHash  VARCHAR(256)                NOT NULL,
    PhoneNumber   VARCHAR(20)                 NULL,
    DateOfBirth   DATE                        NOT NULL,
    Gender        CHAR(1)                     NOT NULL,
    CreatedAt     DATETIME2(0)                NOT NULL
        CONSTRAINT DF_Users_CreatedAt DEFAULT (SYSUTCDATETIME()),
    IsActive      BIT                         NOT NULL
        CONSTRAINT DF_Users_IsActive  DEFAULT (1),

    CONSTRAINT PK_Users            PRIMARY KEY (UserId),
    CONSTRAINT FK_Users_Roles      FOREIGN KEY (RoleId)
        REFERENCES dbo.Roles (RoleId) ON DELETE NO ACTION,
    CONSTRAINT UQ_Users_Email      UNIQUE      (Email),
    CONSTRAINT CK_Users_Email      CHECK       (Email LIKE '%_@_%._%'),
    CONSTRAINT CK_Users_Gender     CHECK       (Gender IN ('M', 'F', 'O')),
    CONSTRAINT CK_Users_DOB        CHECK       (DateOfBirth < CAST(SYSUTCDATETIME() AS DATE))
);
GO

-- Lat/long are stored so Part 3 can do the weather and the map
CREATE TABLE dbo.Events
(
    EventId             INT            IDENTITY(1,1) NOT NULL,
    OrganiserId         INT                          NOT NULL,
    EventName           VARCHAR(120)                 NOT NULL,
    Description         VARCHAR(1000)                NULL,
    EventDate           DATE                         NOT NULL,
    StartTime           TIME(0)                      NOT NULL,
    Province            VARCHAR(40)                  NOT NULL,
    City                VARCHAR(80)                  NOT NULL,
    VenueName           VARCHAR(120)                 NOT NULL,
    Latitude            DECIMAL(9,6)                 NOT NULL,
    Longitude           DECIMAL(9,6)                 NOT NULL,
    RegistrationOpens   DATE                         NOT NULL,
    RegistrationCloses  DATE                         NOT NULL,
    EventStatus         VARCHAR(20)                  NOT NULL
        CONSTRAINT DF_Events_Status    DEFAULT ('Draft'),
    CreatedAt           DATETIME2(0)                 NOT NULL
        CONSTRAINT DF_Events_CreatedAt DEFAULT (SYSUTCDATETIME()),

    CONSTRAINT PK_Events              PRIMARY KEY (EventId),
    CONSTRAINT FK_Events_Organiser    FOREIGN KEY (OrganiserId)
        REFERENCES dbo.Users (UserId) ON DELETE NO ACTION,
    CONSTRAINT UQ_Events_NameDate     UNIQUE      (EventName, EventDate),
    CONSTRAINT CK_Events_Status       CHECK       (EventStatus IN ('Draft', 'Published', 'Cancelled', 'Completed')),
    CONSTRAINT CK_Events_Province     CHECK       (Province IN ('Eastern Cape', 'Free State', 'Gauteng',
                                                                'KwaZulu-Natal', 'Limpopo', 'Mpumalanga',
                                                                'North West', 'Northern Cape', 'Western Cape')),
    CONSTRAINT CK_Events_RegWindow    CHECK       (RegistrationCloses >= RegistrationOpens),
    CONSTRAINT CK_Events_RegBeforeRun CHECK       (EventDate >= RegistrationCloses),
    CONSTRAINT CK_Events_Latitude     CHECK       (Latitude  BETWEEN  -90 AND  90),
    CONSTRAINT CK_Events_Longitude    CHECK       (Longitude BETWEEN -180 AND 180)
);
GO

-- The distances on offer. Fee, start time and limits differ per distance,
-- so you enter a category and not the event itself.
CREATE TABLE dbo.EventCategories
(
    CategoryId       INT           IDENTITY(1,1) NOT NULL,
    EventId          INT                         NOT NULL,
    CategoryName     VARCHAR(80)                 NOT NULL,
    Discipline       VARCHAR(10)                 NOT NULL,
    DistanceKm       DECIMAL(6,2)                NOT NULL,
    EntryFee         DECIMAL(10,2)               NOT NULL,
    MaxParticipants  INT                         NOT NULL,
    StartTime        TIME(0)                     NOT NULL,
    MinimumAge       INT                         NOT NULL
        CONSTRAINT DF_EventCategories_MinAge DEFAULT (12),

    CONSTRAINT PK_EventCategories          PRIMARY KEY (CategoryId),
    CONSTRAINT FK_EventCategories_Events   FOREIGN KEY (EventId)
        REFERENCES dbo.Events (EventId) ON DELETE CASCADE,
    CONSTRAINT UQ_EventCategories_Name     UNIQUE      (EventId, CategoryName),
    CONSTRAINT CK_EventCategories_Disc     CHECK       (Discipline IN ('Run', 'Walk', 'Cycle')),
    CONSTRAINT CK_EventCategories_Distance CHECK       (DistanceKm > 0),
    CONSTRAINT CK_EventCategories_Fee      CHECK       (EntryFee >= 0),
    CONSTRAINT CK_EventCategories_Max      CHECK       (MaxParticipants > 0),
    CONSTRAINT CK_EventCategories_MinAge   CHECK       (MinimumAge BETWEEN 0 AND 100)
);
GO

-- Points along a route, in order
CREATE TABLE dbo.RouteWaypoints
(
    WaypointId           INT          IDENTITY(1,1) NOT NULL,
    CategoryId           INT                        NOT NULL,
    SequenceNumber       INT                        NOT NULL,
    WaypointName         VARCHAR(80)                NOT NULL,
    WaypointType         VARCHAR(20)                NOT NULL,
    Latitude             DECIMAL(9,6)               NOT NULL,
    Longitude            DECIMAL(9,6)               NOT NULL,
    DistanceFromStartKm  DECIMAL(6,2)               NOT NULL,

    CONSTRAINT PK_RouteWaypoints            PRIMARY KEY (WaypointId),
    CONSTRAINT FK_RouteWaypoints_Categories FOREIGN KEY (CategoryId)
        REFERENCES dbo.EventCategories (CategoryId) ON DELETE CASCADE,
    CONSTRAINT UQ_RouteWaypoints_Sequence   UNIQUE      (CategoryId, SequenceNumber),
    CONSTRAINT CK_RouteWaypoints_Type       CHECK       (WaypointType IN ('Start', 'WaterPoint', 'Medical',
                                                                          'Timing', 'Finish')),
    CONSTRAINT CK_RouteWaypoints_Sequence   CHECK       (SequenceNumber > 0),
    CONSTRAINT CK_RouteWaypoints_Latitude   CHECK       (Latitude  BETWEEN  -90 AND  90),
    CONSTRAINT CK_RouteWaypoints_Longitude  CHECK       (Longitude BETWEEN -180 AND 180),
    CONSTRAINT CK_RouteWaypoints_Distance   CHECK       (DistanceFromStartKm >= 0)
);
GO

-- Joins participants to categories, this is the many-to-many from the ERD.
-- The unique key on (ParticipantId, CategoryId) stops someone entering twice.
CREATE TABLE dbo.Enrolments
(
    EnrolmentId            INT           IDENTITY(1,1) NOT NULL,
    ParticipantId          INT                         NOT NULL,
    CategoryId             INT                         NOT NULL,
    RaceNumber             VARCHAR(10)                 NOT NULL,
    EnrolmentDate          DATETIME2(0)                NOT NULL
        CONSTRAINT DF_Enrolments_Date       DEFAULT (SYSUTCDATETIME()),
    EnrolmentStatus        VARCHAR(20)                 NOT NULL
        CONSTRAINT DF_Enrolments_Status     DEFAULT ('Pending'),
    AmountPaid             DECIMAL(10,2)               NOT NULL
        CONSTRAINT DF_Enrolments_AmountPaid DEFAULT (0),
    PaymentReference       VARCHAR(50)                 NULL,
    TShirtSize             VARCHAR(5)                  NULL,
    EmergencyContactName   VARCHAR(100)                NOT NULL,
    EmergencyContactPhone  VARCHAR(20)                 NOT NULL,

    CONSTRAINT PK_Enrolments              PRIMARY KEY (EnrolmentId),
    CONSTRAINT FK_Enrolments_Users        FOREIGN KEY (ParticipantId)
        REFERENCES dbo.Users (UserId) ON DELETE NO ACTION,
    CONSTRAINT FK_Enrolments_Categories   FOREIGN KEY (CategoryId)
        REFERENCES dbo.EventCategories (CategoryId) ON DELETE CASCADE,
    CONSTRAINT UQ_Enrolments_OnePerCat    UNIQUE      (ParticipantId, CategoryId),
    CONSTRAINT UQ_Enrolments_RaceNumber   UNIQUE      (CategoryId, RaceNumber),
    CONSTRAINT CK_Enrolments_Status       CHECK       (EnrolmentStatus IN ('Pending', 'Confirmed',
                                                                           'Cancelled', 'Withdrawn')),
    CONSTRAINT CK_Enrolments_AmountPaid   CHECK       (AmountPaid >= 0),
    CONSTRAINT CK_Enrolments_TShirtSize   CHECK       (TShirtSize IS NULL OR
                                                      TShirtSize IN ('XS', 'S', 'M', 'L', 'XL', 'XXL'))
);
GO

-- One result per entry, that is what the unique key on EnrolmentId does.
-- CK_Results_Consistency: only a finisher gets a time and a position.
CREATE TABLE dbo.Results
(
    ResultId          INT          IDENTITY(1,1) NOT NULL,
    EnrolmentId       INT                        NOT NULL,
    CapturedByUserId  INT                        NOT NULL,
    StartTime         TIME(0)                    NOT NULL,
    FinishTime        TIME(0)                    NULL,
    ChipTimeSeconds   INT                        NULL,
    OverallPosition   INT                        NULL,
    GenderPosition    INT                        NULL,
    AgeGroupPosition  INT                        NULL,
    ResultStatus      VARCHAR(10)                NOT NULL,
    CapturedAt        DATETIME2(0)               NOT NULL
        CONSTRAINT DF_Results_CapturedAt DEFAULT (SYSUTCDATETIME()),

    CONSTRAINT PK_Results             PRIMARY KEY (ResultId),
    CONSTRAINT FK_Results_Enrolments  FOREIGN KEY (EnrolmentId)
        REFERENCES dbo.Enrolments (EnrolmentId) ON DELETE CASCADE,
    CONSTRAINT FK_Results_CapturedBy  FOREIGN KEY (CapturedByUserId)
        REFERENCES dbo.Users (UserId) ON DELETE NO ACTION,
    CONSTRAINT UQ_Results_Enrolment   UNIQUE      (EnrolmentId),
    CONSTRAINT CK_Results_Status      CHECK       (ResultStatus IN ('Finished', 'DNF', 'DNS', 'DQ')),
    CONSTRAINT CK_Results_ChipTime    CHECK       (ChipTimeSeconds IS NULL OR ChipTimeSeconds > 0),
    CONSTRAINT CK_Results_Positions   CHECK       ((OverallPosition  IS NULL OR OverallPosition  > 0)
                                               AND (GenderPosition   IS NULL OR GenderPosition   > 0)
                                               AND (AgeGroupPosition IS NULL OR AgeGroupPosition > 0)),
    CONSTRAINT CK_Results_Consistency CHECK
    (
        (ResultStatus =  'Finished' AND FinishTime IS NOT NULL AND ChipTimeSeconds IS NOT NULL
                                    AND OverallPosition IS NOT NULL)
     OR (ResultStatus <> 'Finished' AND FinishTime IS     NULL AND ChipTimeSeconds IS     NULL
                                    AND OverallPosition IS     NULL)
    )
);
GO

-- Cached weather so Part 3 is not calling the API on every page load
CREATE TABLE dbo.WeatherSnapshots
(
    SnapshotId       INT           IDENTITY(1,1) NOT NULL,
    EventId          INT                         NOT NULL,
    CapturedAt       DATETIME2(0)                NOT NULL
        CONSTRAINT DF_WeatherSnapshots_Captured DEFAULT (SYSUTCDATETIME()),
    TemperatureC     DECIMAL(4,1)                NOT NULL,
    FeelsLikeC       DECIMAL(4,1)                NULL,
    Conditions       VARCHAR(60)                 NOT NULL,
    WindSpeedKph     DECIMAL(5,1)                NULL,
    HumidityPercent  TINYINT                     NULL,
    ChanceOfRainPct  TINYINT                     NULL,
    Source           VARCHAR(60)                 NOT NULL
        CONSTRAINT DF_WeatherSnapshots_Source DEFAULT ('OpenWeatherMap'),

    CONSTRAINT PK_WeatherSnapshots          PRIMARY KEY (SnapshotId),
    CONSTRAINT FK_WeatherSnapshots_Events   FOREIGN KEY (EventId)
        REFERENCES dbo.Events (EventId) ON DELETE CASCADE,
    CONSTRAINT CK_WeatherSnapshots_Temp     CHECK       (TemperatureC BETWEEN -30 AND 60),
    CONSTRAINT CK_WeatherSnapshots_Humidity CHECK       (HumidityPercent IS NULL OR HumidityPercent <= 100),
    CONSTRAINT CK_WeatherSnapshots_Rain     CHECK       (ChanceOfRainPct IS NULL OR ChanceOfRainPct <= 100),
    CONSTRAINT CK_WeatherSnapshots_Wind     CHECK       (WindSpeedKph IS NULL OR WindSpeedKph >= 0)
);
GO

-- Indexes. SQL Server does the primary keys already, these are for the
-- foreign keys and the columns I expect to filter on most.
CREATE NONCLUSTERED INDEX IX_Users_RoleId              ON dbo.Users (RoleId);
CREATE NONCLUSTERED INDEX IX_Events_OrganiserId        ON dbo.Events (OrganiserId);
CREATE NONCLUSTERED INDEX IX_Events_EventDate          ON dbo.Events (EventDate) INCLUDE (EventStatus, Province);
CREATE NONCLUSTERED INDEX IX_EventCategories_EventId   ON dbo.EventCategories (EventId);
CREATE NONCLUSTERED INDEX IX_RouteWaypoints_CategoryId ON dbo.RouteWaypoints (CategoryId);
CREATE NONCLUSTERED INDEX IX_Enrolments_ParticipantId  ON dbo.Enrolments (ParticipantId);
CREATE NONCLUSTERED INDEX IX_Enrolments_CategoryId     ON dbo.Enrolments (CategoryId) INCLUDE (EnrolmentStatus);
CREATE NONCLUSTERED INDEX IX_Results_CapturedByUserId  ON dbo.Results (CapturedByUserId);
CREATE NONCLUSTERED INDEX IX_WeatherSnapshots_EventId  ON dbo.WeatherSnapshots (EventId, CapturedAt DESC);
GO

-- Seed data. IDs are set explicitly so the foreign keys below are readable.
-- The PasswordHash values are placeholders, Part 2 hashes them properly.

-- Roles
SET IDENTITY_INSERT dbo.Roles ON;
INSERT INTO dbo.Roles (RoleId, RoleName, Description) VALUES
    (1, 'Organiser',   'Creates and manages events, categories and results, and views all enrolments.'),
    (2, 'Participant', 'Browses events, enters a category, and tracks personal enrolments and results.');
SET IDENTITY_INSERT dbo.Roles OFF;
GO

-- Users: 2 organisers, 6 participants
SET IDENTITY_INSERT dbo.Users ON;
INSERT INTO dbo.Users
    (UserId, RoleId, FirstName, LastName, Email, PasswordHash, PhoneNumber, DateOfBirth, Gender, CreatedAt, IsActive)
VALUES
    -- Organisers
    (1, 1, 'Thandeka', 'Mokoena',    'thandeka.mokoena@raceday.co.za',  'SEED-HASH-PLACEHOLDER-01', '0821234567', '1985-03-12', 'F', '2025-08-01T09:14:00', 1),
    (2, 1, 'Pieter',   'van Wyk',    'pieter.vanwyk@raceday.co.za',     'SEED-HASH-PLACEHOLDER-02', '0834567890', '1979-11-02', 'M', '2025-08-03T11:40:00', 1),
    -- Participants
    (3, 2, 'Sipho',    'Ndlovu',     'sipho.ndlovu@gmail.com',          'SEED-HASH-PLACEHOLDER-03', '0736548921', '1994-07-21', 'M', '2025-09-10T18:22:00', 1),
    (4, 2, 'Lerato',   'Dlamini',    'lerato.dlamini@outlook.com',      'SEED-HASH-PLACEHOLDER-04', '0791472583', '1998-02-14', 'F', '2025-09-12T07:05:00', 1),
    (5, 2, 'Ayesha',   'Patel',      'ayesha.patel@gmail.com',          'SEED-HASH-PLACEHOLDER-05', '0827418529', '1991-09-30', 'F', '2025-10-02T20:31:00', 1),
    (6, 2, 'Johan',    'Botha',      'johan.botha@webmail.co.za',       'SEED-HASH-PLACEHOLDER-06', '0845962137', '1986-05-08', 'M', '2025-10-19T13:47:00', 1),
    (7, 2, 'Nomsa',    'Khumalo',    'nomsa.khumalo@gmail.com',         'SEED-HASH-PLACEHOLDER-07', '0713698520', '2000-01-19', 'F', '2026-01-08T16:12:00', 1),
    (8, 2, 'Daniel',   'Oosthuizen', 'daniel.oosthuizen@icloud.com',    'SEED-HASH-PLACEHOLDER-08', '0765432109', '1996-12-03', 'M', '2026-02-21T08:58:00', 1);
SET IDENTITY_INSERT dbo.Users OFF;
GO

-- Events: one already run, two still open
SET IDENTITY_INSERT dbo.Events ON;
INSERT INTO dbo.Events
    (EventId, OrganiserId, EventName, Description, EventDate, StartTime, Province, City, VenueName,
     Latitude, Longitude, RegistrationOpens, RegistrationCloses, EventStatus, CreatedAt)
VALUES
    (1, 1, 'Comrades Marathon 2026',
        'The 2026 Up Run from Durban City Hall to Scottsville Racecourse in Pietermaritzburg. '
        + 'A qualifying marathon time is required for the ultra marathon category.',
        '2026-06-14', '05:30', 'KwaZulu-Natal', 'Durban', 'Durban City Hall',
        -29.858700, 31.021800, '2025-09-01', '2026-05-15', 'Completed', '2025-08-20T10:00:00'),

    (2, 2, 'Soweto Marathon 2026',
        'The People''s Race. Three distances starting and finishing at FNB Stadium and taking in '
        + 'Orlando Towers, the Hector Pieterson Memorial and Vilakazi Street.',
        '2026-11-01', '06:00', 'Gauteng', 'Soweto', 'FNB Stadium',
        -26.234800, 27.982100, '2026-06-01', '2026-10-20', 'Published', '2026-05-18T14:25:00'),

    (3, 1, 'Cape Town Cycle Tour 2027',
        'The largest timed cycle race in the world. Routes leave the Grand Parade and run the '
        + 'Peninsula via Chapman''s Peak Drive and Suikerbossie.',
        '2027-03-14', '06:15', 'Western Cape', 'Cape Town', 'Grand Parade',
        -33.925800, 18.423200, '2026-08-01', '2027-02-20', 'Published', '2026-07-22T09:10:00');
SET IDENTITY_INSERT dbo.Events OFF;
GO

-- Categories, every event has at least two
SET IDENTITY_INSERT dbo.EventCategories ON;
INSERT INTO dbo.EventCategories
    (CategoryId, EventId, CategoryName, Discipline, DistanceKm, EntryFee, MaxParticipants, StartTime, MinimumAge)
VALUES
    -- Comrades Marathon 2026
    (1, 1, 'Up Run 87.7km Ultra Marathon', 'Run',    87.70, 1250.00, 25000, '05:30', 20),
    (2, 1, 'Comrades Community 10km',      'Run',    10.00,  180.00,  3000, '07:00', 12),
    -- Soweto Marathon 2026
    (3, 2, '42.2km Marathon',              'Run',    42.20,  450.00,  8000, '06:00', 18),
    (4, 2, '21.1km Half Marathon',         'Run',    21.10,  300.00, 12000, '06:30', 16),
    (5, 2, '10km Race Walk',               'Walk',   10.00,  180.00, 15000, '07:00', 12),
    -- Cape Town Cycle Tour 2027
    (6, 3, '109km Race Tour',              'Cycle', 109.00,  890.00, 30000, '06:15', 16),
    (7, 3, '42km Lite Tour',               'Cycle',  42.00,  520.00,  5000, '09:00', 12),
    (8, 3, '10km Junior Ride',             'Cycle',  10.00,  150.00,  2000, '11:00',  8);
SET IDENTITY_INSERT dbo.EventCategories OFF;
GO

-- Routes for the Comrades and the Cycle Tour
SET IDENTITY_INSERT dbo.RouteWaypoints ON;
INSERT INTO dbo.RouteWaypoints
    (WaypointId, CategoryId, SequenceNumber, WaypointName, WaypointType, Latitude, Longitude, DistanceFromStartKm)
VALUES
    -- Category 1: Comrades Up Run, Durban to Pietermaritzburg
    ( 1, 1, 1, 'Durban City Hall',       'Start',      -29.858700, 31.021800,   0.00),
    ( 2, 1, 2, 'Cowies Hill',            'WaterPoint', -29.834100, 30.886900,  14.50),
    ( 3, 1, 3, 'Fields Hill',            'WaterPoint', -29.802500, 30.813700,  22.30),
    ( 4, 1, 4, 'Bothas Hill',            'Medical',    -29.769200, 30.745200,  33.80),
    ( 5, 1, 5, 'Drummond Halfway',       'Timing',     -29.755300, 30.688900,  43.85),
    ( 6, 1, 6, 'Inchanga',               'WaterPoint', -29.740600, 30.652200,  50.20),
    ( 7, 1, 7, 'Camperdown',             'Medical',    -29.732500, 30.539000,  64.70),
    ( 8, 1, 8, 'Polly Shortts',          'WaterPoint', -29.649700, 30.408300,  79.60),
    ( 9, 1, 9, 'Scottsville Racecourse', 'Finish',     -29.622200, 30.380300,  87.70),
    -- Category 6: Cape Town Cycle Tour 109km
    (10, 6, 1, 'Grand Parade',           'Start',      -33.925800, 18.423200,   0.00),
    (11, 6, 2, 'Muizenberg',             'WaterPoint', -34.108600, 18.470000,  26.40),
    (12, 6, 3, 'Simons Town',            'Medical',    -34.193900, 18.432500,  38.90),
    (13, 6, 4, 'Smitswinkel Bay',        'Timing',     -34.250000, 18.450000,  46.20),
    (14, 6, 5, 'Misty Cliffs',           'WaterPoint', -34.227800, 18.390000,  54.80),
    (15, 6, 6, 'Chapmans Peak Drive',    'Medical',    -34.083300, 18.358300,  76.50),
    (16, 6, 7, 'Suikerbossie',           'WaterPoint', -34.045000, 18.358300,  92.10),
    (17, 6, 8, 'Green Point',            'Finish',     -33.903600, 18.410600, 109.00);
SET IDENTITY_INSERT dbo.RouteWaypoints OFF;
GO

-- Enrolments
SET IDENTITY_INSERT dbo.Enrolments ON;
INSERT INTO dbo.Enrolments
    (EnrolmentId, ParticipantId, CategoryId, RaceNumber, EnrolmentDate, EnrolmentStatus, AmountPaid,
     PaymentReference, TShirtSize, EmergencyContactName, EmergencyContactPhone)
VALUES
    -- Comrades Up Run (category 1) - event already run
    ( 1, 3, 1, 'CM1042', '2026-02-10T19:24:00', 'Confirmed', 1250.00, 'PAY-CM-000101', 'L',  'Nandi Ndlovu',      '0721112233'),
    ( 2, 4, 1, 'CM2288', '2026-03-02T08:11:00', 'Confirmed', 1250.00, 'PAY-CM-000102', 'S',  'Thabo Dlamini',     '0734445566'),
    ( 3, 6, 1, 'CM3315', '2026-01-28T21:50:00', 'Confirmed', 1250.00, 'PAY-CM-000103', 'XL', 'Marie Botha',       '0827778899'),
    ( 4, 8, 1, 'CM4408', '2026-04-05T12:35:00', 'Confirmed', 1250.00, 'PAY-CM-000104', 'M',  'Elsa Oosthuizen',   '0798887766'),
    -- Comrades Community 10km (category 2)
    ( 5, 5, 2, 'CC0071', '2026-04-18T17:02:00', 'Confirmed',  180.00, 'PAY-CM-000105', 'M',  'Riaz Patel',        '0836549871'),
    ( 6, 7, 2, 'CC0072', '2026-04-19T09:47:00', 'Confirmed',  180.00, 'PAY-CM-000106', 'S',  'Zanele Khumalo',    '0713214567'),
    -- Soweto Marathon 42.2km (category 3) - upcoming
    ( 7, 3, 3, 'SM0450', '2026-08-05T06:30:00', 'Confirmed',  450.00, 'PAY-SM-000201', 'L',  'Nandi Ndlovu',      '0721112233'),
    ( 8, 8, 3, 'SM0451', '2026-08-11T14:18:00', 'Confirmed',  450.00, 'PAY-SM-000202', 'M',  'Elsa Oosthuizen',   '0798887766'),
    -- Soweto Half Marathon (category 4)
    ( 9, 4, 4, 'SH1120', '2026-09-01T10:09:00', 'Confirmed',  300.00, 'PAY-SM-000203', 'S',  'Thabo Dlamini',     '0734445566'),
    -- Soweto 10km Race Walk (category 5) - entered but not yet paid
    (10, 7, 5, 'SW2001', '2026-09-12T19:55:00', 'Pending',      0.00, NULL,            'S',  'Zanele Khumalo',    '0713214567'),
    -- Cape Town Cycle Tour 109km (category 6)
    (11, 6, 6, 'CT0910', '2026-09-05T11:23:00', 'Confirmed',  890.00, 'PAY-CT-000301', 'XL', 'Marie Botha',       '0827778899'),
    (12, 5, 6, 'CT0911', '2026-09-15T15:41:00', 'Confirmed',  890.00, 'PAY-CT-000302', 'M',  'Riaz Patel',        '0836549871'),
    -- Cape Town Cycle Tour 42km Lite Tour (category 7) - withdrawn after entering
    (13, 8, 7, 'CL0330', '2026-09-18T07:16:00', 'Cancelled',  520.00, 'PAY-CT-000303', 'M',  'Elsa Oosthuizen',   '0798887766');
SET IDENTITY_INSERT dbo.Enrolments OFF;
GO

-- Results, only for the Comrades since the other two have not happened yet.
-- Enrolment 4 is a DNF, so no time and no position.
SET IDENTITY_INSERT dbo.Results ON;
INSERT INTO dbo.Results
    (ResultId, EnrolmentId, CapturedByUserId, StartTime, FinishTime, ChipTimeSeconds,
     OverallPosition, GenderPosition, AgeGroupPosition, ResultStatus, CapturedAt)
VALUES
    -- Category 1: Up Run 87.7km
    (1, 1, 1, '05:30', '11:12:47', 20567,    1, 1, 1, 'Finished', '2026-06-14T18:40:00'),
    (2, 3, 1, '05:30', '11:58:03', 23283,    2, 2, 1, 'Finished', '2026-06-14T18:40:00'),
    (3, 2, 1, '05:30', '12:44:19', 26059,    3, 1, 1, 'Finished', '2026-06-14T18:41:00'),
    (4, 4, 1, '05:30', NULL,        NULL, NULL, NULL, NULL, 'DNF', '2026-06-14T19:05:00'),
    -- Category 2: Community 10km
    (5, 5, 1, '07:00', '07:48:12',  2892,    1, 1, 1, 'Finished', '2026-06-14T12:15:00'),
    (6, 6, 1, '07:00', '07:55:40',  3340,    2, 2, 1, 'Finished', '2026-06-14T12:15:00');
SET IDENTITY_INSERT dbo.Results OFF;
GO

-- Weather
SET IDENTITY_INSERT dbo.WeatherSnapshots ON;
INSERT INTO dbo.WeatherSnapshots
    (SnapshotId, EventId, CapturedAt, TemperatureC, FeelsLikeC, Conditions, WindSpeedKph,
     HumidityPercent, ChanceOfRainPct, Source)
VALUES
    (1, 1, '2026-06-14T06:00:00', 14.2, 13.0, 'Clear',          8.5, 72,  5, 'OpenWeatherMap'),
    (2, 1, '2026-06-14T10:00:00', 22.8, 23.4, 'Sunny',         14.2, 48,  0, 'OpenWeatherMap'),
    (3, 2, '2026-09-20T07:00:00', 18.6, 18.0, 'Partly cloudy', 12.0, 55, 20, 'OpenWeatherMap'),
    (4, 2, '2026-09-22T07:00:00', 21.3, 21.0, 'Sunny',          9.4, 41, 10, 'OpenWeatherMap'),
    (5, 3, '2026-09-22T07:00:00', 17.9, 16.8, 'Light rain',    26.5, 81, 65, 'OpenWeatherMap');
SET IDENTITY_INSERT dbo.WeatherSnapshots OFF;
GO

-- Leaderboard view. Three of the planned endpoints need this same join.
CREATE VIEW dbo.vw_CategoryLeaderboard
AS
SELECT
    ec.EventId,
    e.EventName,
    ec.CategoryId,
    ec.CategoryName,
    ec.DistanceKm,
    en.EnrolmentId,
    en.RaceNumber,
    u.UserId          AS ParticipantId,
    u.FirstName + ' ' + u.LastName AS ParticipantName,
    u.Gender,
    r.ResultStatus,
    r.FinishTime,
    r.ChipTimeSeconds,
    r.OverallPosition,
    r.GenderPosition,
    r.AgeGroupPosition
FROM dbo.Results          AS r
INNER JOIN dbo.Enrolments      AS en ON en.EnrolmentId = r.EnrolmentId
INNER JOIN dbo.Users           AS u  ON u.UserId       = en.ParticipantId
INNER JOIN dbo.EventCategories AS ec ON ec.CategoryId  = en.CategoryId
INNER JOIN dbo.Events          AS e  ON e.EventId      = ec.EventId;
GO

-- Check it worked. Expected: Roles 2, Users 8, Events 3, Categories 8,
-- Waypoints 17, Enrolments 13, Results 6, Weather 5.
PRINT '--- RaceDay build complete. Row counts: ---';

SELECT 'Roles' AS TableName, COUNT(*) AS RowCount_ FROM dbo.Roles
UNION ALL SELECT 'Users',            COUNT(*) FROM dbo.Users
UNION ALL SELECT 'Events',           COUNT(*) FROM dbo.Events
UNION ALL SELECT 'EventCategories',  COUNT(*) FROM dbo.EventCategories
UNION ALL SELECT 'RouteWaypoints',   COUNT(*) FROM dbo.RouteWaypoints
UNION ALL SELECT 'Enrolments',       COUNT(*) FROM dbo.Enrolments
UNION ALL SELECT 'Results',          COUNT(*) FROM dbo.Results
UNION ALL SELECT 'WeatherSnapshots', COUNT(*) FROM dbo.WeatherSnapshots;

-- Events with their categories and confirmed entry counts
SELECT
    e.EventName,
    e.EventDate,
    e.EventStatus,
    ec.CategoryName,
    ec.DistanceKm,
    ec.EntryFee,
    COUNT(en.EnrolmentId) AS ConfirmedEntries
FROM dbo.Events AS e
INNER JOIN dbo.EventCategories AS ec ON ec.EventId = e.EventId
LEFT  JOIN dbo.Enrolments      AS en ON en.CategoryId = ec.CategoryId
                                     AND en.EnrolmentStatus = 'Confirmed'
GROUP BY e.EventName, e.EventDate, e.EventStatus, ec.CategoryName, ec.DistanceKm, ec.EntryFee
ORDER BY e.EventDate, ec.DistanceKm DESC;

-- Comrades leaderboard
SELECT
    CategoryName,
    OverallPosition,
    RaceNumber,
    ParticipantName,
    Gender,
    FinishTime,
    ResultStatus
FROM dbo.vw_CategoryLeaderboard
WHERE CategoryId = 1
ORDER BY CASE WHEN OverallPosition IS NULL THEN 1 ELSE 0 END, OverallPosition;
GO
