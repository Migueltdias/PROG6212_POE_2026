# RaceDay – PROG6212 POE Part 3

Miguel Dias – ST10255804

ASP.NET Core MVC web app for RaceDay. It consumes the Part 2 API for all data, stores images in Azure Blob Storage through the API, and runs in Docker.

**Video:** YouTube link – _add here_

## Run with Docker

```bash
docker compose up --build
```

This starts SQL Server, Azurite (the Azure Blob Storage emulator), the API and the web app. The database is created and seeded on first start.

- Web app: <http://localhost:8080>
- API (Swagger): <http://localhost:5088/swagger>

Web app container on its own, against an API running on the host:

```bash
docker build -t raceday-web -f src/RaceDay.Web/Dockerfile .
docker run --rm -p 8080:8080 --add-host=host.docker.internal:host-gateway raceday-web
```

To use a real Azure Storage account, copy `.env.example` to `.env`, set `BLOB_CONNECTION_STRING` and leave `BLOB_PUBLIC_BASE_URL` empty.

## Run without Docker

Requirements: .NET 8 SDK, SQL Server Express or LocalDB, and Node.js. Run each command in its own terminal:

```bash
npx azurite-blob --silent --location .azurite --skipApiVersionCheck
dotnet run --project src/RaceDay.Api --launch-profile http
dotnet run --project src/RaceDay.Web --launch-profile http
```

Then open <http://localhost:5100>. The API connection string is `ConnectionStrings:RaceDay` in `src/RaceDay.Api/appsettings.json`.

## Demo accounts

Password `RaceDay@2026`:

| Email | Role |
|---|---|
| thandeka.mokoena@raceday.co.za | Organiser |
| pieter.vanwyk@raceday.co.za | Organiser |
| ayesha.patel@gmail.com | Participant |
| sipho.ndlovu@gmail.com | Participant |

## Features

**Organiser:** dashboard of their events with enrolments per event and upcoming dates; create, edit and delete events with a banner image; manage distance and age categories; view enrolments with colour-coded statuses; capture and publish results.

**Participant:** upcoming events with filters; full event details; enter a category and pay; My events with entry statuses; My results with finish times and positions (e.g. 47th out of 312 finishers); profile with a profile picture.

- **Sessions:** login stores the user ID and role in the session. Each role has its own navigation, unauthenticated users are redirected to login, and logout clears the session.
- **Blob Storage:** the MVC app posts images to the API, which stores them in Azure Blob Storage, saves the URL in the database and returns a read-only link for display. The MVC app has no database or Azure access of its own.

## Tests and CI

`dotnet test` runs 420 tests (API and web app). GitHub Actions builds the API and the MVC project, runs the tests, builds the Docker images and smoke-tests `docker compose`.

![CI build](docs/ci-green-build.png)
