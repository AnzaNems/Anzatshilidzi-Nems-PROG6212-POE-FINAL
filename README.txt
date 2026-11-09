 RaceDay — Event Management System

**PROG6212 POE — Part 2 (RESTful API) + Part 3 (MVC Client, Azure Blob, Docker)**

[![RaceDay CI](https://github.com/AnzaNems/Anzatshilidzi-Nems-PROG6212-PART1/actions/workflows/dotnet-ci.yml/badge.svg)](https://github.com/AnzaNems/Anzatshilidzi-Nems-PROG6212-PART1/actions/workflows/dotnet-ci.yml)

> Replace the badge URL above with your exact repository path if it differs —
> after your first push, the badge turns green when CI passes. Screenshot it for the rubric.

**Student:** Anzatshilidzi Nems
**Module:** PROG6212 — Programming 2C
**Repo:** https://github.com/AnzaNems/Anzatshilidzi-Nems-PROG6212-PART1

---

## 1. What is RaceDay?

RaceDay is an event management system for the South African road running, walking
and cycling community. Organisers publish events (10km fun runs, 42.2km marathons,
109km cycle tours), define entry categories with fees, manage enrolments and capture
results. Participants browse events, enter a category of their choice, track their
enrolment status and view their personal race history — position out of total finishers,
finish time, disqualifications.

The solution is a strict two-application architecture, exactly as the POE requires:

```
+---------------------+         HTTP/JSON          +---------------------+
|    RaceDayMvc       |  ------------------------> |     RaceDayApi      |
|  (.NET 8 MVC)       |   HttpClient + JWT in      |   (.NET 8 Web API)  |
|                     |   Authorization header     |                     |
|  UI + session only  |                            |  EF Core + SQL      |
|  NO DbContext       |                            |  JWT + BCrypt       |
|  NO NuGet packages  |                            |  Azure Blob client  |
+---------------------+                            +----------+----------+
                                                              |
                                                     +--------v--------+
                                                     |   Azure Blob    |
                                                     | raceday-images  |
                                                     +-----------------+
```

* **RaceDayApi** owns the database (the only `DbContext` in the solution), issues JWT
  tokens, hashes passwords with BCrypt and uploads images to Azure Blob Storage.
* **RaceDayMvc** renders HTML, keeps the token in session state, and calls the API
  over `HttpClient` (named client, `IHttpClientFactory`). It has zero database access
  and zero extra NuGet packages.
* **RaceDayApi.Tests** hosts the real API in-memory (EF InMemory provider) and runs
  26 integration tests covering auth, role enforcement, ownership, enrolment rules
  and result capture.

### The two roles

| | **Organiser** | **Participant** |
|---|---|---|
| Events | Create, edit, soft-delete **their own** events; add categories | Browse all upcoming events with filters (type, location, search) |
| Enrolments | View all entries per own event; Confirm / set back to Pending | Enter an event by choosing a category; see own entries (Pending → Confirmed) |
| Results | Capture and correct finish times, positions, DQ flags | View own personal results (e.g. "47th of 312 finishers") |
| Files | Upload event banners | Upload profile picture |
| Profile | Edit own profile | Edit own profile |

Security model: the participant identity is **always taken from the JWT** server-side,
never from the request body — a browser can never enrol or query somebody else's records.

---

## 2. Demo video

> ▶ **YouTube (unlisted):** `PASTE-YOUR-VIDEO-LINK-HERE`

The video walks through: registration, login for both roles, event browsing with
filters, enrolment with the pending/confirm flow, result capture, My Results,
Azure Blob upload, unit tests and the green CI build.

---

## 3. Continuous Integration

Every push runs **RaceDay CI** (GitHub Actions, `.github/workflows/dotnet-ci.yml`):

1. Restore + build `RaceDayApi` (treat warnings as errors: none)
2. Restore + build `RaceDayMvc`
3. Run all 26 xUnit integration tests on Ubuntu

**Green build screenshot:** `![CI passing](images/ci-green.png)` — paste your screenshot
from the repo's **Actions** tab after the first successful run.

---

## 4. Prerequisites

| Tool | Version | Check with |
|---|---|---|
| Visual Studio 2022 | 17.8+ with "ASP.NET and web development" workload | Help → About |
| .NET SDK | 8.0 | `dotnet --list-sdks` |
| SQL Server Express **or** LocalDB | any 2019+ | `sqllocaldb info` |
| SSMS (optional) | 19+ | — |
| Git | any | `git --version` |
| Docker Desktop | 4.x (Part 3 only) | `docker --version` |
| Azure free account | — (Part 3 only) | portal.azure.com |

---

## 5. Setup and run (from zero)

### 5.1 Clone and open

```bat
git clone https://github.com/AnzaNems/Anzatshilidzi-Nems-PROG6212-PART1.git RaceDay
cd RaceDay
start RaceDay.sln
```

Visual Studio restores all NuGet packages automatically
(EF Core SqlServer, JwtBearer, BCrypt.Net-Next, Azure.Storage.Blobs, Swashbuckle, xUnit).

### 5.2 Configure the database connection

`RaceDayApi/appsettings.json` → `ConnectionStrings:DefaultConnection`:

| Your machine has | Server value |
|---|---|
| SQL Server Express (service `SQLEXPRESS`) | `Server=localhost\\SQLEXPRESS;Database=RaceDayDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True` |
| SQL Server default instance | `Server=localhost;...` (same rest) |
| Only Visual Studio's LocalDB | `Server=(localdb)\\MSSQLLocalDB;Database=RaceDayDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True` |

No migration commands are needed: the API applies `InitialCreate` and seeds the
database **automatically on startup** (`DbInitializer.InitializeAsync` in `Program.cs`).

### 5.3 Run the API

1. Solution Explorer → right-click **RaceDayApi** → **Set as Startup Project**.
2. Run-profile dropdown (next to ▶): **http**.
3. **F5** → Swagger opens at `http://localhost:7123/swagger`.

### 5.4 Seeded accounts (created on first start)

| Email | Password | Role |
|---|---|---|
| thabo@raceday.co.za | Organ@123 | Organiser |
| lerato@raceday.co.za | Organ@123 | Organiser |
| sibusiso@raceday.co.za | Part@123 | Participant |
| amanda@raceday.co.za | Part@123 | Participant |
| pieter@raceday.co.za | Part@123 | Participant |

The seed also creates 4 events (one already completed with captured results),
10 categories and 7 enrolments — matching the approved Part 1 seed specification.

### 5.5 Run the MVC client

1. Keep the API running.
2. Right-click **RaceDayMvc** → **Set as Startup Project** → profile **http** → **F5**.
3. Open `http://localhost:7050` → log in with any seeded account.

### 5.6 Run the unit tests

**Test → Test Explorer → Run All Tests** → 26/26 green.
(Coverage: register/login success + failure paths, role enforcement 401/403,
organiser ownership, duplicate enrolment 409, invalid category 400, result
capture/update, My-Results isolation.)

---

## 6. API reference (23 endpoints)

Base URL: `http://localhost:7123` · Auth: `Authorization: Bearer <token>`

### Auth (public)
| Method | Route | Purpose |
|---|---|---|
| POST | `/api/auth/register` | Create account (FullName, Email, Password, Role = Organiser \| Participant) |
| POST | `/api/auth/login` | Returns JWT + UserId, Role, FullName |

### Profile (any authenticated user)
| Method | Route | Purpose |
|---|---|---|
| GET | `/api/profile` | Own profile (never returns the password hash) |
| PUT | `/api/profile` | Update FullName / Email / optional ProfilePictureUrl |

### Events
| Method | Route | Who | Notes |
|---|---|---|---|
| GET | `/api/events` | public | Filters: `eventTypeId`, `location`, `search`, `includePast` |
| GET | `/api/events/{id}` | public | Detail + Categories array |
| POST | `/api/events` | Organiser | 201; 400 on validation |
| PUT | `/api/events/{id}` | owning Organiser | 403 for other organisers |
| DELETE | `/api/events/{id}` | owning Organiser | Soft delete (IsDeleted) |
| GET | `/api/events/mine` | Organiser | **(additional)** own events incl. past — powers the dashboard |

### Categories
| Method | Route | Who |
|---|---|---|
| GET | `/api/events/{eventId}/categories` | public |
| POST | `/api/events/{eventId}/categories` | owning Organiser |
| PUT | `/api/categories/{id}` | owning Organiser |
| DELETE | `/api/categories/{id}` | owning Organiser (409 if enrolments exist) |

### Enrolments
| Method | Route | Who | Notes |
|---|---|---|---|
| POST | `/api/events/{eventId}/enrolments` | Participant | Body `{ "categoryId": n }`; 201 Pending; 409 duplicate; 400 category-not-in-event / event already held |
| GET | `/api/enrolments/me` | Participant | Own enrolments only (drives "My Events") |
| GET | `/api/events/{eventId}/enrolments` | owning Organiser | All entries for the event |
| PUT | `/api/enrolments/{id}/status` | owning Organiser | **(additional)** Body `{ "status": "Confirmed" }` — the Confirm button |

### Results
| Method | Route | Who | Notes |
|---|---|---|---|
| POST | `/api/enrolments/{enrolmentId}/result` | owning Organiser | `{ "finishTime": "03:45:12", "position": 1, "isDisqualified": false }`; 409 if result exists |
| PUT | `/api/results/{id}` | owning Organiser | Correct a captured result |
| GET | `/api/results/me` | Participant | Own history incl. position of total finishers |
| GET | `/api/events/{eventId}/results` | owning Organiser | **(additional)** existing results for the results-entry page |

### Files
| Method | Route | Who | Notes |
|---|---|---|---|
| POST | `/api/files` | any authenticated | **(additional)** multipart field `file`; images only, ≤ 5 MB; returns public blob URL; 503 if Azure not configured |

---

## 7. Azure Blob Storage (Part 3)

All file handling goes through the API (`POST /api/files`) — the MVC app never
contacts Azure. The connection string lives in **user-secrets**, never in the repo.

1. Portal → **Create a resource** → **Storage account**:
   name `raceday` + initials (globally unique, lowercase), Region *South Africa North*,
   Performance *Standard*, Redundancy *LRS*.
2. Storage account → **Containers** → `+ Container` → name `raceday-images`,
   public access **Blob**.
3. **Access keys** → key1 → **Connection string** → copy.
4. In the `RaceDayApi` folder:

```bat
dotnet user-secrets init
dotnet user-secrets set "AzureBlob:ConnectionString" "DefaultEndpointsProtocol=https;AccountName=...;AccountKey=...;EndpointSuffix=core.windows.net"
```

5. Restart the API. `POST /api/files` (Swagger, Authorize first) and the banner /
   profile-picture uploads in the MVC app now store real files in Azure and save the
   public URL on the event / user record.

Secrets are stored outside the repository
(`%APPDATA%\Microsoft\UserSecrets\racedayapi-dev-2026\secrets.json`) and are
excluded from git by the standard .NET ignore rules.

---

## 8. Docker (Part 3)

The MVC client ships a multi-stage `Dockerfile` (build with SDK image, runtime with
ASP.NET image, port 8080). With the API running on the host and Docker Desktop up:

```bat
docker build -t raceday-mvc .
docker run -d -p 8080:8080 --name raceday-mvc raceday-mvc
```

Open **http://localhost:8080** — the same application, now containerised. It reaches
the API on the host machine via `host.docker.internal` (pre-configured in the
Dockerfile). Stop with `docker stop raceday-mvc`.

---

## 9. Deviations from the approved Part 1 plan

The POE allows "additional endpoints you identify as necessary". Four were added,
each tied to an explicit Part 3 requirement:

| Additional endpoint | Why it exists |
|---|---|
| `GET /api/events/mine` (Organiser) | Part 3 requires an organiser dashboard of "all events the Organiser has created" |
| `PUT /api/enrolments/{id}/status` (Organiser) | Part 3 requires enrolment statuses (Pending/Confirmed) — this is the Confirm action |
| `GET /api/events/{eventId}/results` (Organiser) | The results-entry page must show already-captured results for correction |
| `POST /api/files` (any authenticated user) | Part 3 requires all Azure Blob file handling to pass through the API |

Two field extensions, both mandated by the Part 1 functional requirements:
events carry `distanceKm` and `eventTypeId` (run / walk / cycle), and
`PUT /api/profile` accepts an optional `profilePictureUrl` for the Part 3
profile picture (URL stored in the database, as the rubric specifies).

---

## 10. Security notes

* Passwords hashed with **BCrypt.Net-Next** (work factor default); plain text is never stored or logged.
* JWT carries three claims — `NameIdentifier` (user id), `Email`, `Role`; tokens expire after 60 minutes.
* `[Authorize(Roles = ...)]` on every protected endpoint; **resource ownership is enforced in the controller** (an organiser can only touch their own events).
* JWT signing key and Azure connection string live in `appsettings.json` (dev fallback) / **user-secrets**; nothing secret is committed.
* All list endpoints filter "mine" server-side from the token — the client cannot ask for another user's data.

---

## 11. Project structure

```
RaceDay/
|-- docs/                          Part 1: ERD, endpoint plan, SQL seed script
|-- .github/workflows/dotnet-ci.yml
|-- RaceDay.sln
|-- RaceDayApi/                    Part 2 — Web API
|   |-- Controllers/               Auth, Profile, Events, Categories, Enrolments, Results, Files
|   |-- Data/                      AppDbContext (only DbContext) + DbInitializer (migrate + seed)
|   |-- Migrations/                InitialCreate
|   |-- Models/ + Models/DTOs/     6 entities + request/response shapes
|   |-- Services/                  BlobStorageService (Azure)
|   `-- Properties/launchSettings.json   fixed port http://localhost:7123
|-- RaceDayApi.Tests/              26 xUnit integration tests (InMemory host)
|-- RaceDayMvc/                    Part 3 — MVC client
|   |-- Controllers/               Home, Account, Dashboard, Events, Categories,
|   |                              Enrolments, Results, MyEvents, MyResults, Profile
|   |-- Views/                     role-aware pages for both flows
|   |-- wwwroot/css/raceday.css    sporting theme
|   |-- Dockerfile                 multi-stage build
|   `-- Properties/launchSettings.json   fixed port http://localhost:7050
`-- README.md                      this file
```

---

## 12. Development history (Git commit log)

The solution was built incrementally in **27 meaningful commits** — the POE requires
20+ commits per part, each committed only after its milestone was verified green.
Run `git log --oneline` to see the full history. The milestone order:

| # | Commit | Milestone verified before commit |
|---|---|---|
| 1 | `chore: solution skeleton — api, tests and mvc projects` | sln + csproj + Program.cs + configs build |
| 2 | `feat(api): six entities from part 1 erd` | User, EventType, Event, Category, Enrolment, Result |
| 3 | `feat(api): dbcontext with keys, constraints and cascade rules` | model validates, no cascade-path errors |
| 4 | `feat(api): initialcreate migration` | migration generates cleanly |
| 5 | `feat(api): seed data — organisers, participants, events, enrolments, results` | F5 creates RaceDayDb + 5 accounts |
| 6 | `feat(api): dtos for all request and response shapes` | DTO validation attributes in place |
| 7 | `feat(api): jwt auth — register and login endpoints` | login returns token in Swagger |
| 8 | `feat(api): profile endpoints` | GET/PUT /api/profile with token |
| 9 | `feat(api): events crud with organiser ownership` | 403 for non-owners verified |
| 10 | `feat(api): categories endpoints with 409 guard` | delete-with-enrolments returns 409 |
| 11 | `feat(api): enrolments — enter event, my enrolments, organiser view` | duplicate entry returns 409 |
| 12 | `feat(api): results — capture, update, my results` | results visible per participant |
| 13 | `feat(api): swagger with jwt authorize and xml descriptions` | every endpoint documented + testable |
| 14 | `test(api): 26 integration tests — auth, events, enrolments, results` | 26/26 green locally |
| 15 | `ci: github actions builds api + mvc and runs tests` | first green Actions run |
| 16 | `feat(mvc): session auth — login, register, logout, role dashboards` | both role dashboards render |
| 17 | `feat(mvc): public event browsing with filters` | filters hit API query params |
| 18 | `feat(mvc): participant — enter event, my events, my results` | full participant flow walks green |
| 19 | `feat(mvc): profile page with picture upload` | picture URL saved via /api/files |
| 20 | `feat(mvc): organiser — event crud with banner upload` | banner stored in Azure Blob |
| 21 | `feat(mvc): organiser — categories, enrolments, results entry` | confirm + result capture pages work |
| 22 | `feat(mvc): raceday sporting theme` | responsive layout, role-aware navbar |
| 23 | `feat(api): azure blob storage file upload` | blob visible in Azure portal |
| 24 | `feat(mvc): docker multi-stage build` | container serves app on :8080 |
| 25 | `chore: final program.cs and configuration polish` | clean build, 0 warnings |
| 26 | `docs: readme with setup, ci screenshot and run instructions` | this file |
| 27 | `docs: part 1 planning documents` | ERD + endpoint plan + SQL script in docs/ |

---

## 13. Author

**Anzatshilidzi Nems** — PROG6212 Portfolio of Evidence, Part 2 + Part 3.
Part 1 planning documents (ERD, endpoint plan, SQL seed script) are in [`docs/`](./docs).
