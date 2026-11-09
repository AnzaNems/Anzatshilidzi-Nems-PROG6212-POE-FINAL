# RaceDay - Event Management System

[![RaceDay CI](https://github.com/AnzaNems/Anzatshilidzi-Nems-PROG6212-POE-FINAL/actions/workflows/dotnet-ci.yml/badge.svg)](https://github.com/AnzaNems/Anzatshilidzi-Nems-PROG6212-POE-FINAL/actions)

**PROG6212 Portfolio of Evidence - Part 2 (RESTful API) + Part 3 (MVC Client, Azure Blob Storage, Docker)**

RaceDay is a full-stack event management platform for the South African road-running, walking and cycling community. Organisers publish races, define entry categories, confirm enrolments and capture results; participants browse events, enter races and track their personal results.

| | |
|---|---|
| **Student** | Anzatshilidzi Nems |
| **Module** | PROG6212 - Programming 2C |
| **Repository** | [POE-FINAL](https://github.com/AnzaNems/Anzatshilidzi-Nems-PROG6212-POE-FINAL) |
| **Stack** | .NET 8, ASP.NET Core Web API + MVC, EF Core / SQL Server, JWT, xUnit, Azure Blob Storage, Docker, GitHub Actions |

---

## 1. Architecture

Two projects, one solution. The MVC client never touches the database: every request travels Browser -> MVC -> API -> SQL Server / Azure Blob.

| Project | Responsibility |
|---|---|
| `RaceDayApi` | RESTful API: EF Core DbContext, JWT authentication, business rules, Azure Blob uploads |
| `RaceDayMvc` | MVC client: Razor views, Bootstrap UI, session state, HttpClient calls to the API |
| `RaceDayApi.Tests` | 26 xUnit integration tests (auth, roles, events, enrolments, results) |
> Golden rule: the MVC project holds no DbContext and no connection strings. Security is enforced by the API on every protected endpoint.

## 2. Roles

| Role | Capabilities |
|---|---|
| **Organiser** | Create / edit / delete events, manage categories, confirm enrolments, capture results, upload event banners |
| **Participant** | Browse events with filters, enter events, view personal enrolments and results, upload profile picture |

## 3. Getting Started

### 3.1 Prerequisites
- Visual Studio 2022 (17.8+) with the .NET 8 SDK
- SQL Server Express + SSMS
- (Part 3) Azure free account and Docker Desktop

### 3.2 Run the API
1. Open `RaceDay.sln` in Visual Studio.
2. Set `RaceDayApi` as the startup project and press **F5**.
3. Migrations run and the database seeds automatically; Swagger opens at `http://localhost:7123/swagger`.

### 3.3 Run the MVC client
1. Set `RaceDayMvc` as the startup project and press **F5** (keep the API running).
2. The site opens at `http://localhost:7050`.

![MVC dashboard](mvc-dashboard.png)

### 3.4 Seeded accounts

| Email | Password | Role |
|---|---|---|
| thabo@raceday.co.za | Organ@123 | Organiser |
| lerato@raceday.co.za | Organ@123 | Organiser |
| sibusiso@raceday.co.za | Part@123 | Participant |
| amanda@raceday.co.za | Part@123 | Participant |
| pieter@raceday.co.za | Part@123 | Participant |

## 4. Quality: Unit Tests and CI/CD

- 26 integration tests run against the real API pipeline using `WebApplicationFactory` and an in-memory database.
- GitHub Actions builds both projects and executes the full suite on every push: [View workflow runs](https://github.com/AnzaNems/Anzatshilidzi-Nems-PROG6212-POE-FINAL/actions).

![CI pipeline green](ci-success.png)

## 5. Azure Blob Storage (Part 3)

Banner and profile-picture uploads stream through `POST /api/files` into the `raceday-images` container; only the blob reference is stored in SQL Server. The connection string lives in .NET User Secrets and never in source control:

```powershell
dotnet user-secrets set "AzureBlob:ConnectionString" "<your-connection-string>" --project RaceDayApi
<img width="1920" height="1020" alt="ci-success" src="https://github.com/user-attachments/assets/c1df8e14-a494-44dd-92f2-040f48da2bf8" />
```
6. Docker (Part 3)
docker build -t raceday-mvc .
docker run -d -p 8080:8080 --name raceday-mvc raceday-mvc
<img width="1920" height="1020" alt="dockerfile-proof" src="https://github.com/user-attachments/assets/a27ced4f-757b-47ad-86df-e1258c9b2b83" />
<img width="1920" height="1020" alt="dockerfile-code" src="https://github.com/user-attachments/assets/34f076eb-d466-4718-8697-c518d4d9e810" />
7. API Surface
Auth - POST /api/auth/register, POST /api/auth/login
Profile - GET /api/profile, PUT /api/profile
Events - GET /api/events, GET /api/events/{id}, POST /api/events, PUT /api/events/{id}, DELETE /api/events/{id}, GET /api/events/mine
Categories - GET /api/categories, POST /api/categories, PUT /api/categories/{id}, DELETE /api/categories/{id}
Enrolments - POST /api/enrolments, GET /api/enrolments/me, GET /api/enrolments/event/{id}, PUT /api/enrolments/{id}/status
Results - GET /api/results/me, GET /api/events/{id}/results, POST /api/results, PUT /api/results/{id}
Files - POST /api/files (Azure Blob upload)
8. Repository Structure
RaceDaySolution/
|-- RaceDay.sln              Solution file
|-- RaceDayApi/              Part 2: API, EF Core, JWT, Azure Blob service
|-- RaceDayApi.Tests/        26 xUnit integration tests
|-- RaceDayMvc/              Part 3: MVC client, session auth, Dockerfile
|-- .github/workflows/       GitHub Actions CI pipeline
|-- ci-success.png           Evidence: green CI run
|-- azure-upload.png         Evidence: Azure Blob upload (200 OK)
|-- mvc-dashboard.png        Evidence: MVC role dashboard
`-- docker-proof.png         Evidence: Dockerfile verification
9. Submission Checklist
Part 2 API with JWT auth, ownership rules and all 18 planned endpoints (+4 documented additions)
26/26 integration tests green locally and in CI
Part 3 MVC client with session auth and role-specific interfaces
Azure Blob Storage uploads wired through the API
Multi-stage Dockerfile for the MVC client
20+ meaningful commits on main

##  Video Walkthrough
[Click here to watch the POE Demonstration Video]([PASTE_YOUR_YOUTUBE_LINK_HERE](https://youtu.be/8xxHoRCR9PA))

