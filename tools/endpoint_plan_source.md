# RaceDay
## API Endpoint Plan

<!-- meta
Module|PROG6212
Assessment|Portfolio of Evidence, Part 1
Document|API Endpoint Plan - Section B
Author|Miguel Dias - ST10255804
Date Due|22 September 2026
-->

<!-- contents -->

## 1. INTRODUCTION

RaceDay is a web-based event management system for South African road running, walking and
cycling. Organisers publish events, set the distances on offer and capture results. Participants
find events, enter a distance, follow their own race history, and check the route and weather
before race day.

This is Section B of Part 1. It settles the API contract before any code exists, so that the Part 2
API and the Part 3 web application are built against something fixed rather than something
negotiated as work proceeds.

The design follows REST (Fielding, 2000). A URL names a resource, the verb says what to do with it,
and the status code reports what happened. Section 3 is the contract itself. Where Part 2 departs
from it, the repository README will record the change and the reason for it.

Database structure falls outside this document. That is covered by the Entity Relationship Diagram
in Section A and the SQL script in Section C.

## 2. API DESIGN CONVENTIONS

### 2.1 Base URL and payload format

Routes begin with `/api` and bodies are JSON. Timestamps follow ISO 8601. Money is a two-place
decimal in Rand. Anything returning a collection accepts `page` and `pageSize` and reports a total,
since a single popular event can carry tens of thousands of entrants.

### 2.2 Authentication and the role model

Login returns a JSON Web Token (Jones, Bradley & Sakimura, 2015), which the client sends as a
bearer token on later requests. The token carries the caller's id and role claim, and the
authorisation middleware checks both before the action runs (Microsoft, 2026b). Passwords are
hashed at registration by ASP.NET Core Identity and are never recoverable (Microsoft, 2026c).

There are two roles, Organiser and Participant. Every table in Section 3 names one of the values
below in its Role required column.

| Value | Meaning |
|---|---|
| **None** | Public. No token required. |
| **Any** | Any signed-in user, either role. |
| **Organiser** | The token must carry the `Organiser` role. |
| **Organiser (owner)** | Organiser role, and the caller created the parent event. |
| **Participant** | The token must carry the `Participant` role. |
| **Participant (own)** | Participant role, and the record belongs to the caller. |

Holding a role is not the same as owning a record. One Organiser cannot edit another Organiser's
event, so the tables separate **Organiser** from **Organiser (owner)**. Participants are bounded
the same way against their own enrolments.

### 2.3 Response codes

Codes follow RFC 9110 (Fielding, Nottingham & Reschke, 2022).

| Code | Meaning | Returned when |
|---|---|---|
| `200 OK` | Success, with a body | A read succeeded, or an update returned the modified record |
| `201 Created` | A record was created | A POST succeeded; the body carries the record and its new id |
| `202 Accepted` | Queued for processing | A bulk results file was taken for import |
| `204 No Content` | Success, no body | A delete, or a state change with nothing worth returning |
| `400 Bad Request` | Validation failed | A field is missing, malformed, or outside its allowed range |
| `401 Unauthorized` | Not signed in | The token is absent, expired or invalid |
| `403 Forbidden` | Signed in, not permitted | Wrong role, or the record belongs to someone else |
| `404 Not Found` | No such record | The id in the route does not exist |
| `409 Conflict` | Refused by a business rule | Duplicate entry, category full, or entries closed |
| `503 Service Unavailable` | A dependency is down | The weather provider or the database cannot be reached |

Two codes stay implicit in Section 3 so the tables remain narrow. `401 Unauthorized` applies
wherever the role is not **None**, and `403 Forbidden` wherever a named role appears. Both are
written out only where an endpoint turns on that distinction.

<!-- landscape -->

## 3. ENDPOINT SPECIFICATION

Forty-four endpoints follow, grouped by the resource they act on. This section runs landscape so
that all six columns stay readable. The document returns to portrait at Section 4.

### 3.1 Authentication

| HTTP method | Route | Description | Role required | Request body | Expected response |
|---|---|---|---|---|---|
| POST | `/api/auth/register` | Creates an Organiser or Participant account. | None | `{ firstName, lastName, email, password, confirmPassword, phoneNumber, dateOfBirth, gender, role }` | `201 Created`: the new profile and its id. `400`: validation failed. `409 Conflict`: email already registered. |
| POST | `/api/auth/login` | Exchanges credentials for a signed token. | None | `{ email, password }` | `200 OK`: `{ token, expiresAt, user }`. `401 Unauthorized`: bad credentials, or the account is deactivated. |
| POST | `/api/auth/refresh` | Issues a fresh access token. | Any | `{ refreshToken }` | `200 OK`: `{ token, expiresAt }`. `401 Unauthorized`: the refresh token expired or was revoked. |
| POST | `/api/auth/logout` | Revokes the caller's refresh token. | Any | None | `204 No Content`. |

### 3.2 User Profile

| HTTP method | Route | Description | Role required | Request body | Expected response |
|---|---|---|---|---|---|
| GET | `/api/users/me` | Returns the caller's own profile. | Any | None | `200 OK`: the profile. `401 Unauthorized`. |
| PUT | `/api/users/me` | Updates the caller's editable details. | Any | `{ firstName, lastName, phoneNumber, dateOfBirth, gender }` | `200 OK`: the updated profile. `400`: validation failed. |
| PUT | `/api/users/me/password` | Changes the caller's password. | Any | `{ currentPassword, newPassword, confirmNewPassword }` | `204 No Content`. `400`: the new passwords differ or fail policy. `401`: the current password is wrong. |
| DELETE | `/api/users/me` | Deactivates the caller's account. | Any | None | `204 No Content`. `409 Conflict`: the organiser still owns a published future event. |
| GET | `/api/users` | Lists accounts, filtered by role. | Organiser | None (query: `?role=&page=&pageSize=`) | `200 OK`: a paged list. `403 Forbidden`: the caller is a Participant. |
| GET | `/api/users/{id}` | Returns one profile by id. | Organiser | None | `200 OK`: the profile. `404 Not Found`. |

### 3.3 Events

| HTTP method | Route | Description | Role required | Request body | Expected response |
|---|---|---|---|---|---|
| GET | `/api/events` | Lists published events for browsing. | None | None (query: `?province=&discipline=&fromDate=&toDate=&search=&page=&pageSize=`) | `200 OK`: a paged list of summaries. |
| GET | `/api/events/{id}` | Returns one event with its categories. | None | None | `200 OK`: the event. `404 Not Found`. |
| POST | `/api/events` | Creates a draft event. | Organiser | `{ eventName, description, eventDate, startTime, province, city, venueName, latitude, longitude, registrationOpens, registrationCloses }` | `201 Created`: the new event and its id. `400`: validation failed. `409 Conflict`: that name is already used on that date. |
| PUT | `/api/events/{id}` | Updates an event the caller owns. | Organiser (owner) | Same fields as POST | `200 OK`: the updated event. `403 Forbidden`. `404 Not Found`. |
| PATCH | `/api/events/{id}/status` | Publishes, cancels or completes an event. | Organiser (owner) | `{ status }` | `200 OK`: the updated event. `400`: unknown status. `409 Conflict`: an event with no categories cannot be published. |
| DELETE | `/api/events/{id}` | Deletes an event and its categories. | Organiser (owner) | None | `204 No Content`. `404 Not Found`. `409 Conflict`: confirmed entries exist, so cancel it instead. |
| GET | `/api/events/mine` | Lists the caller's events, drafts included. | Organiser | None (query: `?status=&page=&pageSize=`) | `200 OK`: a paged list. |
| GET | `/api/events/{id}/enrolments` | Returns the start list for a whole event. | Organiser (owner) | None (query: `?status=&categoryId=&page=&pageSize=`) | `200 OK`: a paged start list. `403 Forbidden`. `404 Not Found`. |
| GET | `/api/events/{id}/results` | Returns event results by category. | None | None | `200 OK`: results grouped by category. `404 Not Found`. |
| GET | `/api/events/{id}/weather` | Returns cached weather for the venue. | None | None | `200 OK`: the latest snapshot. `404 Not Found`. `503 Service Unavailable`: the provider is down and nothing is cached. |

### 3.4 Event Categories

| HTTP method | Route | Description | Role required | Request body | Expected response |
|---|---|---|---|---|---|
| GET | `/api/events/{eventId}/categories` | Lists the distances an event offers. | None | None | `200 OK`: the categories. `404 Not Found`. |
| GET | `/api/categories/{id}` | Returns one category and its places left. | None | None | `200 OK`: the category. `404 Not Found`. |
| POST | `/api/events/{eventId}/categories` | Adds a distance to an event. | Organiser (owner) | `{ categoryName, discipline, distanceKm, entryFee, maxParticipants, startTime, minimumAge }` | `201 Created`: the new category. `400`: validation failed. `409 Conflict`: that name is already used on the event. |
| PUT | `/api/categories/{id}` | Updates a category. | Organiser (owner) | Same fields as POST | `200 OK`: the updated category. `403 Forbidden`. `404 Not Found`. `409 Conflict`: the new limit is below the number entered. |
| DELETE | `/api/categories/{id}` | Removes a category. | Organiser (owner) | None | `204 No Content`. `404 Not Found`. `409 Conflict`: entries exist. |

### 3.5 Route Waypoints

| HTTP method | Route | Description | Role required | Request body | Expected response |
|---|---|---|---|---|---|
| GET | `/api/categories/{id}/waypoints` | Returns the route in sequence order. | None | None | `200 OK`: the waypoints. `404 Not Found`. |
| POST | `/api/categories/{id}/waypoints` | Adds a point to a route. | Organiser (owner) | `{ sequenceNumber, waypointName, waypointType, latitude, longitude, distanceFromStartKm }` | `201 Created`: the new waypoint. `400`: coordinates or type out of range. `409 Conflict`: that sequence number is taken. |
| PUT | `/api/waypoints/{id}` | Updates one waypoint. | Organiser (owner) | Same fields as POST | `200 OK`: the updated waypoint. `403 Forbidden`. `404 Not Found`. |
| DELETE | `/api/waypoints/{id}` | Removes a waypoint from a route. | Organiser (owner) | None | `204 No Content`. `404 Not Found`. |

### 3.6 Enrolments

| HTTP method | Route | Description | Role required | Request body | Expected response |
|---|---|---|---|---|---|
| POST | `/api/categories/{categoryId}/enrolments` | Enters the caller into a category. | Participant | `{ tShirtSize, emergencyContactName, emergencyContactPhone }` | `201 Created`: the entry and its allocated race number. `400`: validation failed. `403 Forbidden`: the caller is an Organiser. `404 Not Found`. `409 Conflict`: already entered, category full, entries closed, or under the minimum age. |
| GET | `/api/enrolments/mine` | Lists the caller's own entries. | Participant | None (query: `?status=&upcoming=&page=&pageSize=`) | `200 OK`: a paged list with event and category detail. |
| GET | `/api/enrolments/{id}` | Returns one entry. | Participant (own) or Organiser (owner) | None | `200 OK`: the entry. `403 Forbidden`: it belongs to someone else. `404 Not Found`. |
| PUT | `/api/enrolments/{id}` | Updates the caller's own entry details. | Participant (own) | `{ tShirtSize, emergencyContactName, emergencyContactPhone }` | `200 OK`: the updated entry. `403 Forbidden`. `409 Conflict`: the event has been run. |
| PATCH | `/api/enrolments/{id}/payment` | Records payment and confirms the entry. | Participant (own) | `{ amountPaid, paymentReference }` | `200 OK`: the updated entry. `400`: the amount does not match the fee. `409 Conflict`: already confirmed. |
| DELETE | `/api/enrolments/{id}` | Withdraws the caller from a category. | Participant (own) | None | `204 No Content`. `403 Forbidden`. `409 Conflict`: the event has been run. |
| GET | `/api/categories/{id}/enrolments` | Returns the start list for one category. | Organiser (owner) | None (query: `?status=&page=&pageSize=`) | `200 OK`: a paged start list. `403 Forbidden`. `404 Not Found`. |

### 3.7 Results

| HTTP method | Route | Description | Role required | Request body | Expected response |
|---|---|---|---|---|---|
| POST | `/api/enrolments/{id}/result` | Captures the outcome of one entry. | Organiser (owner) | `{ startTime, finishTime, chipTimeSeconds, overallPosition, genderPosition, ageGroupPosition, resultStatus }` | `201 Created`: the new result. `400`: a finish time was sent for a DNF, or left out for a finisher. `403 Forbidden`. `404 Not Found`. `409 Conflict`: a result already exists. |
| POST | `/api/categories/{id}/results/bulk` | Imports a category from a timing file. | Organiser (owner) | `{ results: [ { raceNumber, finishTime, chipTimeSeconds, resultStatus } ] }` | `202 Accepted`: `{ imported, skipped, errors }`. `400`: malformed payload. `403 Forbidden`. |
| PUT | `/api/results/{id}` | Corrects a captured result. | Organiser (owner) | Same fields as POST | `200 OK`: the updated result. `403 Forbidden`. `404 Not Found`. |
| DELETE | `/api/results/{id}` | Removes a result so it can be recaptured. | Organiser (owner) | None | `204 No Content`. `404 Not Found`. |
| GET | `/api/results/{id}` | Returns one result with its context. | None | None | `200 OK`: the result. `404 Not Found`. |
| GET | `/api/results/mine` | Returns the caller's performance history. | Participant | None (query: `?fromDate=&toDate=&discipline=&page=&pageSize=`) | `200 OK`: a paged history with times and placings. |
| GET | `/api/categories/{id}/results` | Returns the leaderboard for a category. | None | None (query: `?gender=&page=&pageSize=`) | `200 OK`: a paged leaderboard. `404 Not Found`. |

### 3.8 System

| HTTP method | Route | Description | Role required | Request body | Expected response |
|---|---|---|---|---|---|
| GET | `/api/health` | Reports API and database status. | None | None | `200 OK`: `{ status, database, version }`. `503 Service Unavailable`: the database cannot be reached. |

<!-- portrait -->

## 4. DESIGN DECISIONS

### 4.1 Entry is per category, not per event

There is no `POST /api/events/{id}/enrolments`. Fee, start time, field limit and minimum age all
differ by distance, so a category is the smallest thing an athlete can meaningfully enter. That is
why `Enrolments` carries `CategoryId` rather than `EventId`.

### 4.2 Race numbers come from the server

Nothing in the enrolment body sets `raceNumber`. A client choosing its own would collide with
`UQ_Enrolments_RaceNumber` on the first duplicate.

### 4.3 Nested to create, flat to edit

A category is created at `POST /api/events/{eventId}/categories`, because it cannot exist without a
parent event, then edited at `PUT /api/categories/{id}`, where its own id is enough to find it.
Waypoints and results take the same shape.

### 4.4 Personal routes read the token, not the URL

`GET /api/enrolments/mine`, `/api/results/mine` and `/api/events/mine` take the user id from the
token. Putting it in the path would mean authorising it on every call, and one missed check there
is the broken object level authorisation flaw that OWASP ranks first among API risks (OWASP, 2023).

### 4.5 Literal segments need a route constraint

`GET /api/events/mine` and `GET /api/events/{id}` are ambiguous until the id routes are declared
`{id:int}`. With the constraint in place, `mine` cannot bind to the parameter and the literal route
wins (Microsoft, 2026a). The other two `/mine` routes rely on the same thing.

### 4.6 Withdrawal marks, it does not erase

`DELETE /api/enrolments/{id}` sets the status to `Cancelled`. Organisers reconcile entry fees
against who withdrew and when, so the row has to survive the withdrawal.

### 4.7 403 and 409 answer different questions

`403` says the caller was never allowed to make the request. `409` says the request was valid but
the state of the system refused it: the field is full, entries have closed, a result is already
captured. Keeping them apart gives the Part 2 unit tests something specific to assert.

### 4.8 Leaderboards are open, start lists are not

Finishing times are published at every road race, so results need no token. Start lists carry
emergency contact details, which are personal information under the Protection of Personal
Information Act (2013), so only the owning Organiser may read them.

## 5. ENDPOINT SUMMARY

| Group | Section | Endpoints |
|---|---|---|
| Authentication | 3.1 | 4 |
| User Profile | 3.2 | 6 |
| Events | 3.3 | 10 |
| Event Categories | 3.4 | 5 |
| Route Waypoints | 3.5 | 4 |
| Enrolments | 3.6 | 7 |
| Results | 3.7 | 7 |
| System | 3.8 | 1 |
| **Total** | | **44** |

## 6. REFERENCES

Fielding, R.T. 2000. Architectural styles and the design of network-based software architectures.
PhD dissertation. University of California, Irvine.

Fielding, R., Nottingham, M. and Reschke, J. 2022. RFC 9110: HTTP semantics. [Online]. Available
at: https://www.rfc-editor.org/rfc/rfc9110 [Accessed 22 September 2026].

Jones, M., Bradley, J. and Sakimura, N. 2015. RFC 7519: JSON web token (JWT). [Online]. Available
at: https://www.rfc-editor.org/rfc/rfc7519 [Accessed 22 September 2026].

Microsoft. 2026a. Routing in ASP.NET Core. [Online]. Available at:
https://learn.microsoft.com/en-us/aspnet/core/fundamentals/routing [Accessed 22 September 2026].

Microsoft. 2026b. Role-based authorization in ASP.NET Core. [Online]. Available at:
https://learn.microsoft.com/en-us/aspnet/core/security/authorization/roles
[Accessed 22 September 2026].

Microsoft. 2026c. Configure ASP.NET Core Identity. [Online]. Available at:
https://learn.microsoft.com/en-us/aspnet/core/security/authentication/identity-configuration
[Accessed 22 September 2026].

OWASP. 2023. API1:2023 broken object level authorization. [Online]. Available at:
https://owasp.org/API-Security/editions/2023/en/0xa1-broken-object-level-authorization/
[Accessed 22 September 2026].

Protection of Personal Information Act, No. 4 of 2013. 2013. Cape Town: Government Printer.
