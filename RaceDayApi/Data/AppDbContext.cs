using Microsoft.EntityFrameworkCore;
using RaceDayApi.Models;

namespace RaceDayApi.Data;

/// <summary>
/// The one and only DbContext in the solution. It owns the RaceDayDb database
/// and maps all six entities from the Part 1 ERD: User, EventType, Event,
/// Category, Enrolment (junction) and Result.
/// </summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<EventType> EventTypes => Set<EventType>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Enrolment> Enrolments => Set<Enrolment>();
    public DbSet<Result> Results => Set<Result>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        // ---------------- Users ----------------
        mb.Entity<User>(e =>
        {
            e.ToTable("Users");
            e.HasKey(u => u.UserId);
            e.Property(u => u.FullName).IsRequired().HasMaxLength(100);
            e.Property(u => u.Email).IsRequired().HasMaxLength(255);
            e.HasIndex(u => u.Email).IsUnique();               // one account per email
            e.Property(u => u.HashPassword).IsRequired();
            e.Property(u => u.Role).IsRequired().HasMaxLength(20);
        });

        // ---------------- EventTypes (lookup: Run / Walk / Cycle) ----------------
        mb.Entity<EventType>(e =>
        {
            e.ToTable("EventTypes");
            e.HasKey(t => t.EventTypeId);
            e.Property(t => t.TypeName).IsRequired().HasMaxLength(20);
            e.HasIndex(t => t.TypeName).IsUnique();
        });

        // ---------------- Events ----------------
        mb.Entity<Event>(e =>
        {
            e.ToTable("Events");
            e.HasKey(x => x.EventId);
            e.Property(x => x.EventName).IsRequired().HasMaxLength(150);
            e.Property(x => x.Description).IsRequired();
            e.Property(x => x.EventDate).HasColumnType("date");
            e.Property(x => x.Location).IsRequired().HasMaxLength(200);
            e.Property(x => x.DistanceKm).HasColumnType("decimal(5,2)");
            e.Property(x => x.BannerImageUrl).HasMaxLength(500);

            e.HasOne(x => x.Organiser)                          // User 1---N Event
             .WithMany(u => u.Events)
             .HasForeignKey(x => x.OrganiserId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.EventType)                          // EventType 1---N Event
             .WithMany(t => t.Events)
             .HasForeignKey(x => x.EventTypeId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasMany(x => x.Categories)                        // Event 1---N Category
             .WithOne(c => c.Event)
             .HasForeignKey(c => c.EventId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ---------------- Categories ----------------
        mb.Entity<Category>(e =>
        {
            e.ToTable("Categories");
            e.HasKey(c => c.CategoryId);
            e.Property(c => c.CategoryName).IsRequired().HasMaxLength(50);
            e.Property(c => c.DistanceKm).HasColumnType("decimal(5,2)");
            e.Property(c => c.EntryFee).HasColumnType("decimal(10,2)");
        });

        // ---------------- Enrolments (junction: User N---M Event) ----------------
        mb.Entity<Enrolment>(e =>
        {
            e.ToTable("Enrolments");
            e.HasKey(x => x.EnrolmentId);
            e.Property(x => x.Status).IsRequired().HasMaxLength(20);
            e.Property(x => x.EnrolmentDate).HasColumnType("datetime2");

            // One entry per participant per event -> duplicate POST returns 409.
            e.HasIndex(x => new { x.ParticipantId, x.EventId }).IsUnique();

            e.HasOne(x => x.Event)                              // Event 1---N Enrolment
             .WithMany(ev => ev.Enrolments)
             .HasForeignKey(x => x.EventId)
             .OnDelete(DeleteBehavior.Cascade);                 // the ONLY cascade into Enrolments

            e.HasOne(x => x.Participant)                        // User 1---N Enrolment
             .WithMany(u => u.Enrolments)
             .HasForeignKey(x => x.ParticipantId)
             .OnDelete(DeleteBehavior.Restrict);                // Restrict: avoids SQL Server
                                                               // "multiple cascade paths" error
            e.HasOne(x => x.Category)                           // Category 1---N Enrolment
             .WithMany(c => c.Enrolments)
             .HasForeignKey(x => x.CategoryId)
             .OnDelete(DeleteBehavior.Restrict);                // deleting a category with
                                                               // enrolments -> 409, not data loss
        });

        // ---------------- Results (1---1 with Enrolment) ----------------
        mb.Entity<Result>(e =>
        {
            e.ToTable("Results");
            e.HasKey(r => r.ResultId);
            e.Property(r => r.FinishTime).HasColumnType("time(7)");

            e.HasIndex(r => r.EnrolmentId).IsUnique();          // one result per enrolment -> 409

            e.HasOne(r => r.Enrolment)
             .WithOne(x => x.Result)
             .HasForeignKey<Result>(r => r.EnrolmentId)
             .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
