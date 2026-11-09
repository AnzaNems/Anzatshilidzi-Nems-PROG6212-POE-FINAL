using Microsoft.EntityFrameworkCore;
using RaceDayApi.Models;

namespace RaceDayApi.Data;

/// <summary>
/// Runs at API startup: applies pending EF migrations and seeds the database
/// the first time it is empty. The seed matches the Part 1 SQL script spec:
/// 2 Organisers, 3 Participants, 4 Events (one already completed with results),
/// categories per event and sample enrolments.
/// Any failure is logged to the console only - the API still starts, so you
/// can diagnose the problem in Swagger instead of a crash loop.
/// </summary>
public static class DbInitializer
{
    public static async Task InitializeAsync(AppDbContext db)
    {
        try
        {
            // MigrateAsync only works on a real relational database (SQL Server).
            // The unit tests run on the EF InMemory provider, where it must be skipped.
            if (db.Database.IsRelational() && db.Database.GetMigrations().Any())
                await db.Database.MigrateAsync();

            if (!await db.EventTypes.AnyAsync())
            {
                await SeedAsync(db);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("[DbInitializer] Seeding skipped: " + ex.Message);
        }
    }

    private static async Task SeedAsync(AppDbContext db)
    {
        // ---------- lookup: event types ----------
        var run = new EventType { TypeName = "Run" };
        var walk = new EventType { TypeName = "Walk" };
        var cycle = new EventType { TypeName = "Cycle" };
        db.EventTypes.AddRange(run, walk, cycle);
        await db.SaveChangesAsync();

        // ---------- users (passwords are BCrypt-hashed, never plain text) ----------
        var thabo = new User { FullName = "Thabo Mokoena", Email = "thabo@raceday.co.za", HashPassword = BCrypt.Net.BCrypt.HashPassword("Organ@123"), Role = "Organiser" };
        var lerato = new User { FullName = "Lerato Dlamini", Email = "lerato@raceday.co.za", HashPassword = BCrypt.Net.BCrypt.HashPassword("Organ@123"), Role = "Organiser" };
        var sibusiso = new User { FullName = "Sibusiso Nkosi", Email = "sibusiso@raceday.co.za", HashPassword = BCrypt.Net.BCrypt.HashPassword("Part@123"), Role = "Participant" };
        var amanda = new User { FullName = "Amanda van Wyk", Email = "amanda@raceday.co.za", HashPassword = BCrypt.Net.BCrypt.HashPassword("Part@123"), Role = "Participant" };
        var pieter = new User { FullName = "Pieter Botha", Email = "pieter@raceday.co.za", HashPassword = BCrypt.Net.BCrypt.HashPassword("Part@123"), Role = "Participant" };
        db.Users.AddRange(thabo, lerato, sibusiso, amanda, pieter);
        await db.SaveChangesAsync();

        // ---------- events (1 completed + 3 upcoming) ----------
        var heritage = new Event
        {
            EventName = "Heritage Day 10km Run", Description = "A festive 10km road run through the Jacaranda-lined streets of Pretoria, held on Heritage Day morning.",
            EventDate = new DateOnly(2026, 9, 24), Location = "Union Buildings, Pretoria", DistanceKm = 10.0m,
            EventTypeId = run.EventTypeId, OrganiserId = thabo.UserId
        };
        var soweto = new Event
        {
            EventName = "Soweto Marathon 2026", Description = "The iconic 42.2km marathon through Soweto's historic route, plus a half and 10km option for the community.",
            EventDate = new DateOnly(2026, 11, 15), Location = "FNB Stadium, Soweto, Johannesburg", DistanceKm = 42.2m,
            EventTypeId = run.EventTypeId, OrganiserId = thabo.UserId
        };
        var walkDurban = new Event
        {
            EventName = "Durban Charity Walk", Description = "A relaxed beachfront charity walk raising funds for local schools. All fitness levels welcome.",
            EventDate = new DateOnly(2026, 12, 5), Location = "Durban Beachfront Promenade", DistanceKm = 10.0m,
            EventTypeId = walk.EventTypeId, OrganiserId = lerato.UserId
        };
        var cycleTour = new Event
        {
            EventName = "Cape Town Cycle Tour 2027", Description = "The world's largest timed cycle race - 109km around the Cape Peninsula with the scenic Chapman's Peak climb.",
            EventDate = new DateOnly(2027, 3, 8), Location = "Cape Town City Bowl", DistanceKm = 109.0m,
            EventTypeId = cycle.EventTypeId, OrganiserId = lerato.UserId
        };
        db.Events.AddRange(heritage, soweto, walkDurban, cycleTour);
        await db.SaveChangesAsync();

        // ---------- categories per event ----------
        var catHeritageU20 = new Category { EventId = heritage.EventId, CategoryName = "Under 20", DistanceKm = 10.0m, EntryFee = 120m };
        var catHeritageSenior = new Category { EventId = heritage.EventId, CategoryName = "Senior", DistanceKm = 10.0m, EntryFee = 180m };
        var catHeritageVet = new Category { EventId = heritage.EventId, CategoryName = "Veterans 40+", DistanceKm = 10.0m, EntryFee = 150m };

        var catSowetoU20 = new Category { EventId = soweto.EventId, CategoryName = "Under 20", DistanceKm = 42.2m, EntryFee = 250m };
        var catSowetoSenior = new Category { EventId = soweto.EventId, CategoryName = "Senior", DistanceKm = 42.2m, EntryFee = 450m };
        var catSowetoVet = new Category { EventId = soweto.EventId, CategoryName = "Veterans 40+", DistanceKm = 42.2m, EntryFee = 350m };

        var catWalk10 = new Category { EventId = walkDurban.EventId, CategoryName = "10km Fun Walk", DistanceKm = 10.0m, EntryFee = 100m };
        var catWalk5 = new Category { EventId = walkDurban.EventId, CategoryName = "5km Family Walk", DistanceKm = 5.0m, EntryFee = 60m };

        var catCycleElite = new Category { EventId = cycleTour.EventId, CategoryName = "109km Elite", DistanceKm = 109.0m, EntryFee = 550m };
        var catCycleSocial = new Category { EventId = cycleTour.EventId, CategoryName = "109km Social", DistanceKm = 109.0m, EntryFee = 350m };

        db.Categories.AddRange(catHeritageU20, catHeritageSenior, catHeritageVet,
                                catSowetoU20, catSowetoSenior, catSowetoVet,
                                catWalk10, catWalk5, catCycleElite, catCycleSocial);
        await db.SaveChangesAsync();

        // ---------- sample enrolments ----------
        var e1 = new Enrolment { ParticipantId = sibusiso.UserId, EventId = heritage.EventId, CategoryId = catHeritageU20.CategoryId, Status = EnrolmentStatus.Confirmed, EnrolmentDate = new DateTime(2026, 9, 10) };
        var e2 = new Enrolment { ParticipantId = amanda.UserId, EventId = heritage.EventId, CategoryId = catHeritageSenior.CategoryId, Status = EnrolmentStatus.Confirmed, EnrolmentDate = new DateTime(2026, 9, 12) };
        var e3 = new Enrolment { ParticipantId = pieter.UserId, EventId = heritage.EventId, CategoryId = catHeritageVet.CategoryId, Status = EnrolmentStatus.Confirmed, EnrolmentDate = new DateTime(2026, 9, 14) };

        var e4 = new Enrolment { ParticipantId = sibusiso.UserId, EventId = soweto.EventId, CategoryId = catSowetoSenior.CategoryId, Status = EnrolmentStatus.Pending };
        var e5 = new Enrolment { ParticipantId = amanda.UserId, EventId = soweto.EventId, CategoryId = catSowetoU20.CategoryId, Status = EnrolmentStatus.Confirmed };
        var e6 = new Enrolment { ParticipantId = pieter.UserId, EventId = walkDurban.EventId, CategoryId = catWalk10.CategoryId, Status = EnrolmentStatus.Pending };
        var e7 = new Enrolment { ParticipantId = amanda.UserId, EventId = cycleTour.EventId, CategoryId = catCycleSocial.CategoryId, Status = EnrolmentStatus.Confirmed };

        db.Enrolments.AddRange(e1, e2, e3, e4, e5, e6, e7);
        await db.SaveChangesAsync();

        // ---------- results for the completed event (Heritage Day) ----------
        db.Results.AddRange(
            new Result { EnrolmentId = e1.EnrolmentId, FinishTime = new TimeSpan(0, 52, 14), Position = 1, CapturedAt = new DateTime(2026, 9, 24, 12, 0, 0) },
            new Result { EnrolmentId = e2.EnrolmentId, FinishTime = new TimeSpan(0, 55, 3), Position = 2, CapturedAt = new DateTime(2026, 9, 24, 12, 0, 0) },
            new Result { EnrolmentId = e3.EnrolmentId, FinishTime = new TimeSpan(0, 58, 47), Position = 3, IsDisqualified = true, CapturedAt = new DateTime(2026, 9, 24, 12, 0, 0) });

        await db.SaveChangesAsync();
    }
}
