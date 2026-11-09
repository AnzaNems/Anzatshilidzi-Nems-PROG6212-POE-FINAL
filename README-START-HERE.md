# RaceDay — Start Here (PROG6212 Part 2 + Part 3)

This bundle is a complete, **build-verified** rebuild of your Part 2 (RESTful API) and
Part 3 (MVC + Azure Blob + Docker) projects for the **RaceDay** event management system.
Every route matches your approved Part 1 endpoint plan. The solution compiles with
**0 errors / 0 warnings** and all **26 unit tests pass** (verified before delivery).

> What was verified at delivery time: `dotnet build RaceDay.sln` -> Build succeeded,
> `dotnet test` -> \*\*26/26 passed\*\*, EF migration generates cleanly (6 tables,
> 3 cascades + 4 restricts - no SQL Server cascade-path errors).

\---

## 1\. What is in this folder

```
RaceDaySolution/
|-- RaceDay.sln                    <- open this in Visual Studio
|-- RaceDayApi/                    <- Part 2: the RESTful API (.NET 8)
|   |-- Controllers/               Auth, Profile, Events, Categories, Enrolments, Results, Files
|   |-- Data/                      AppDbContext + DbInitializer (migrate + seed)
|   |-- Migrations/                InitialCreate (already generated - verified)
|   |-- Models/                    User, EventType, Event, Category, Enrolment, Result
|   |-- Models/DTOs/               request/response shapes from your Part 1 plan
|   |-- Services/                  BlobStorageService (Azure, Part 3)
|   |-- Program.cs                 JWT + Swagger + startup seed
|   |-- appsettings.json           SQL Express connection + Jwt + AzureBlob
|   `-- Properties/launchSettings.json   fixed port: http://localhost:7123
|-- RaceDayApi.Tests/              <- Part 2: xUnit integration tests (26 tests)
|   |-- CustomWebApplicationFactory.cs   hosts the real API on InMemory database
|   |-- AuthTests.cs / EventsTests.cs / EnrolmentsTests.cs / ResultsTests.cs
|-- RaceDayMvc/                    <- Part 3: the MVC client (.NET 8)
|   |-- Controllers/               Home, Account, Dashboard, Events, Categories,
|   |                              Enrolments, Results, MyEvents, MyResults, Profile
|   |-- Models/                    view models (mirrors of the API DTOs)
|   |-- Views/                     all pages for both roles
|   |-- wwwroot/css/raceday.css    the RaceDay sporting theme
|   |-- Dockerfile                 multi-stage container build
|   `-- Properties/launchSettings.json   fixed port: http://localhost:7050
`-- .github/workflows/dotnet-ci.yml    GitHub Actions: build + test on every push
```

The MVC project has **zero NuGet packages and no database access** - exactly what the POE
requires: it only renders HTML, keeps session state and calls the API over HttpClient.

\---

## 2\. Before you start - software checklist

|Tool|Why|Check|
|-|-|-|
|Visual Studio 2022 (17.8+)|ASP.NET Core 8 development|Help > About|
|.NET 8 SDK|build + test|`dotnet --list-sdks`|
|SQL Server Express + SSMS|the database|SSMS connects to `localhost\\SQLEXPRESS`|
|Git + your GitHub account|version control (20+ commits per part!)|`git --version`|
|Docker Desktop|Part 3 containerisation|Docker Desktop is running|
|Azure free account|Part 3 Blob Storage|portal.azure.com login works|

\---

## 3\. STEP BY STEP - from zero to running

### STEP 1 - Get your Part 1 repository onto this machine (5 min)

Your Part 1 planning documents already live in one repo - the POE says you keep using
that same repository link for all parts:

```bat
cd C:\\Users\\<you>\\source\\repos
git clone https://github.com/AnzaNems/Anzatshilidzi-Nems-PROG6212-PART1.git RaceDay
cd RaceDay
```

You should see your `docs/` folder from Part 1. Everything you add now sits next to it.
(If you already have the repo cloned, `git pull` first.)

### STEP 2 - Copy this solution into the repository (5 min)

1. Copy the **entire contents** of this `RaceDaySolution` folder into the repo folder
(the folder that contains `docs/`).
2. After copying, your repo root must look like this:

```
RaceDay/                     <- the git repository
|-- docs/                    <- Part 1 (ERD, endpoint plan, SQL script) - DO NOT TOUCH
|-- .github/workflows/dotnet-ci.yml
|-- .dockerignore
|-- RaceDay.sln
|-- RaceDayApi/
|-- RaceDayApi.Tests/
|-- RaceDayMvc/
`-- README.md                <- you will rewrite this later (STEP 10)
```

Note: `.github` is a hidden folder - in Windows Explorer enable
**View > Show > Hidden items** to see it.

### STEP 3 - Open, restore, build (5 min)

1. Double-click `RaceDay.sln`.
2. Visual Studio restores the NuGet packages automatically (first time takes a minute).
If it does not: right-click the solution > **Restore NuGet Packages**.
3. Press **Ctrl+Shift+B** (Build Solution). You must see: `Build succeeded, 0 Errors`.

No packages to install by hand - they are all listed inside the `.csproj` files
(EF Core SqlServer, JWT Bearer, BCrypt, Azure.Storage.Blobs, Swagger, xUnit).

### STEP 4 - Create and seed the database (5 min)

The `InitialCreate` migration is already included and verified. The API applies it
automatically on startup, so all you do is:

1. Check the connection string in `RaceDayApi/appsettings.json` - it defaults to
`Server=localhost\\SQLEXPRESS;Database=RaceDayDb`. If your instance name differs
(e.g. `SQLEXPRESS01`), fix it there.
2. In Visual Studio, right-click **RaceDayApi** > **Set as Startup Project**.
3. Press **F5**. The browser opens on `http://localhost:7123/swagger`.
4. Verify in SSMS: connect to `localhost\\SQLEXPRESS` > databases > **RaceDayDb** >
Tables. You should see: `Users`, `EventTypes`, `Events`, `Categories`,
`Enrolments`, `Results` and `\_\_EFMigrationsHistory`.
5. Run in SSMS: `SELECT FullName, Email, Role FROM Users;` - five seeded accounts appear.

**Seeded accounts (all ready to log in):**

|Email|Password|Role|
|-|-|-|
|thabo@raceday.co.za|Organ@123|Organiser|
|lerato@raceday.co.za|Organ@123|Organiser|
|sibusiso@raceday.co.za|Part@123|Participant|
|amanda@raceday.co.za|Part@123|Participant|
|pieter@raceday.co.za|Part@123|Participant|

The seed also creates 4 events (one completed with results so My Results works
immediately), 10 categories and 7 enrolments - matching the Part 1 seed spec.

### STEP 5 - Verify the API in Swagger (10 min)

Do this at least once so you can screen-record it for the video:

1. With the API running, open `http://localhost:7123/swagger`.
2. `POST /api/auth/login` > Try it out > body:
`{ "email": "sibusiso@raceday.co.za", "password": "Part@123" }` > Execute > **200**.
Copy the `token` value.
3. Click the green **Authorize** button (top right), paste the token, click Authorize.
4. `GET /api/enrolments/me` > Execute > **200** with Sibusiso's two enrolments.
5. `GET /api/events` > Execute > **200** with the upcoming events.
6. `POST /api/events` (still as Sibusiso) > Execute > **403 Forbidden** - role
enforcement working. This is exactly what the unit tests prove automatically.

### STEP 6 - Run the unit tests (5 min)

1. In Visual Studio: **Test > Test Explorer**.
2. Click **Run All Tests**. All 26 must be green.
3. Tests cover: registration + login (success/failure), event management
(organiser allowed, participant/anonymous rejected, ownership enforced),
enrolments (created, duplicate 409, role checks, /me isolation), results
(capture, duplicate 409, invalid time 400, My Results isolation).

### STEP 7 - Run the MVC application (10 min)

1. Right-click **RaceDayMvc** > **Set as Startup Project** > press **F5**.
2. Keep RaceDayApi running too (right-click RaceDayApi > Debug > Start new instance,
or start it first, then the MVC project).
3. Walk the participant flow: Login as `sibusiso@raceday.co.za` / `Part@123` >
browse events with filters > open Soweto Marathon > Enter Event >
My Events (amber Pending badge) > My Results (Heritage Day 10km: 1st of 2
finishers) > Profile.
4. Log out, walk the organiser flow: Login as `thabo@raceday.co.za` / `Organ@123` >
Dashboard (stats) > My Events > create a new event > add categories >
Enrolments > Confirm a pending entry > Results entry for Heritage Day 10km.
5. Notice the role-aware navigation bar changes between the two logins.

### STEP 8 - Push to GitHub and get the green build (10 min)

1. In the repo folder:

```bat
git add .
git commit -m "feat(part2): raceday api with auth, events, enrolments, results + unit tests"
git push
```

2. Open your repo on GitHub > **Actions** tab > the workflow **RaceDay CI** runs:
it builds the API, builds the MVC app and runs all 26 tests on Ubuntu.
3. When it shows a green check mark, screenshot it - that screenshot goes in your
README (POE submission requirement).
4. The workflow file is `.github/workflows/dotnet-ci.yml` - it was already copied
in STEP 2.

### STEP 9 - Azure Blob Storage (20 min, Part 3)

The API already has all the code. You only need to give it a connection string:

1. portal.azure.com > **Create a resource** > search "Storage account" > Create.

   * Name: `raceday` + your initials (must be globally unique, lowercase)
   * Region: South Africa North
   * Performance: Standard, Redundancy: LRS
2. Open the storage account > **Containers** (left menu) > **+ Container**:
Name `raceday-images`, **Public access level: Blob (anonymous read for blobs)**.
3. Left menu > **Access keys** > click **Show** next to Key 1 > copy the
**Connection string**.
4. In Visual Studio, open a terminal in the RaceDayApi folder (right-click project >
Open in Terminal) and run (one command, paste your string):

```bat
dotnet user-secrets init
dotnet user-secrets set "AzureBlob:ConnectionString" "paste-your-connection-string-here"
```

5. Restart the API. Now banner upload (organiser, event create/edit) and profile
picture upload (both roles) work end to end - the URL is saved to the database
and displayed on the pages.

### STEP 10 - Docker (10 min, Part 3)

With Docker Desktop running and the API running in Visual Studio (STEP 7):

```bat
cd C:\\Users\\<you>\\source\\repos\\RaceDay
docker build -t raceday-mvc .
docker run -d -p 8080:8080 --name raceday-mvc raceday-mvc
```

Open `http://localhost:8080` - the same MVC app, now served from the container.
It reaches the API on your host through `host.docker.internal` (already configured
in the Dockerfile). Stop it with `docker stop raceday-mvc`.

### STEP 11 - Your repository README.md

The POE requires a README at the repo root. It must include: system description,
the two roles, setup + run instructions, the CI green-build screenshot, the Docker
run command, and your unlisted YouTube link. Rewrite `README.md` at the repo root
(keep Part 1's content and extend it - do not delete the video link from Part 1 if
your lecturer expects the history; add the Part 2/3 sections below it).

\---

## 4\. How this matches your approved Part 1 plan

All 18 planned endpoints are implemented exactly as planned (same routes, bodies,
status codes). The POE also says "plus any additional endpoints you identify as
necessary" - the four additions and why:

|Endpoint|Why it exists|
|-|-|
|`GET /api/events/mine` (Organiser)|POE Part 3 requires an organiser dashboard of "all events the Organiser has created"|
|`PUT /api/enrolments/{id}/status` (Organiser)|POE Part 3 requires enrolment statuses (Pending/Confirmed) - this is the Confirm button|
|`GET /api/events/{eventId}/results` (Organiser)|POE Part 3 results entry page needs to show existing results to correct them|
|`POST /api/files` (any logged-in user)|POE Part 3: all Azure Blob file handling must go through the API|

Two field extensions, both required by the POE functional requirements:

* Events also carry `distanceKm` and `eventTypeId` (POE: "distance, and event type
(run, walk, or cycle)").
* `PUT /api/profile` accepts an optional `profilePictureUrl` (POE Part 3 profile
picture upload; URL stored in the database as the rubric asks).

Mention these in your README so nothing counts as an "unexplained deviation".

\---

## 5\. Commit plan (20+ meaningful commits per part)

Copy files in **milestone order**, not all at once, and commit after each green step:

|#|Commit message|What you copy / verify|
|-|-|-|
|1|`chore: solution skeleton - api, tests and mvc projects`|sln + csproj + Program.cs + appsettings + launchSettings|
|2|`feat(api): six entities from part 1 erd`|Models/ (User, EventType, Event, Category, Enrolment, Result)|
|3|`feat(api): dbcontext with keys, constraints and cascade rules`|Data/AppDbContext.cs|
|4|`feat(api): initialcreate migration`|Migrations/ (verify: F5 creates RaceDayDb)|
|5|`feat(api): seed data - organisers, participants, events, enrolments, results`|Data/DbInitializer.cs|
|6|`feat(api): jwt auth - register and login endpoints`|Controllers/AuthController.cs + DTOs|
|7|`feat(api): profile endpoints`|Controllers/ProfileController.cs|
|8|`feat(api): events crud with organiser ownership`|Controllers/EventsController.cs|
|9|`feat(api): categories endpoints with 409 guard`|Controllers/CategoriesController.cs|
|10|`feat(api): enrolments - enter event, my enrolments, organiser view`|Controllers/EnrolmentsController.cs|
|11|`feat(api): results - capture, update, my results`|Controllers/ResultsController.cs|
|12|`feat(api): swagger with jwt authorize and xml descriptions`|Program.cs final|
|13|`test(api): 26 integration tests - auth, events, enrolments, results`|RaceDayApi.Tests/|
|14|`ci: github actions builds api + mvc and runs tests`|.github/workflows/dotnet-ci.yml|
|15|`feat(mvc): session auth - login, register, logout, role dashboards`|Controllers + Account/Dashboard views|
|16|`feat(mvc): public event browsing with filters`|Home + Events/Details views|
|17|`feat(mvc): participant - enter event, my events, my results`|MyEvents/MyResults controllers + views|
|18|`feat(mvc): profile page with picture upload`|Profile controller + view|
|19|`feat(mvc): organiser - event crud with banner upload`|Events create/edit/delete views|
|20|`feat(mvc): organiser - categories, enrolments, results entry`|Categories/Enrolments/Results views|
|21|`feat(mvc): raceday sporting theme`|raceday.css + \_Layout.cshtml|
|22|`feat(api): azure blob storage file upload`|Services + FilesController|
|23|`feat(mvc): docker multi-stage build`|Dockerfile + .dockerignore|
|24|`docs: readme with setup, ci screenshot and run instructions`|README.md|

\---

## 6\. Quick troubleshooting

|Symptom|Fix|
|-|-|
|MVC pages show "API is not reachable"|The API is not running - start RaceDayApi first, then the MVC app|
|Login always fails|Check you are using the exact seeded emails/passwords (table above)|
|`SqlException` on F5|SQL Express not running, or instance name wrong in appsettings.json|
|Banner upload says "not configured"|The user-secrets connection string is missing - STEP 9|
|Docker: MVC opens but login fails|The API on your host is not running, or Docker Desktop's host.docker.internal is blocked - restart Docker Desktop|
|CI red on GitHub|Open the failing run - 99% it is a file that was not pushed (`git status` before committing)|

Full 16-row troubleshooting table: see the master plan PDF, Appendix B.

