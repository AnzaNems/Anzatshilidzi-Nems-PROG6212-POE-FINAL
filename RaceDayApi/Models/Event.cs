namespace RaceDayApi.Models;

/// <summary>
/// A road running, walking or cycling event created by an Organiser,
/// for example the Soweto Marathon or the Cape Town Cycle Tour.
/// Deleting an event is a SOFT delete (IsDeleted = true) so that past
/// enrolments and results are preserved - exactly as planned in Part 1.
/// </summary>
public class Event
{
    public int EventId { get; set; }

    public string EventName { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public DateOnly EventDate { get; set; }

    public string Location { get; set; } = string.Empty;

    /// <summary>Headline distance of the event in kilometres, e.g. 42.2.</summary>
    public decimal DistanceKm { get; set; }

    public int EventTypeId { get; set; }                   // FK -> EventTypes (Run / Walk / Cycle)

    /// <summary>Full Azure Blob URL of the banner image (Part 3, optional).</summary>
    public string? BannerImageUrl { get; set; }

    public bool IsDeleted { get; set; }                    // soft delete flag

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // ---- foreign keys ----
    public int OrganiserId { get; set; }                   // FK -> Users

    // ---- navigation properties ----
    public User Organiser { get; set; } = null!;
    public EventType EventType { get; set; } = null!;
    public ICollection<Category> Categories { get; set; } = new List<Category>();
    public ICollection<Enrolment> Enrolments { get; set; } = new List<Enrolment>();
}
