namespace RaceDayApi.Models;

/// <summary>
/// The link between a Participant, an Event and the Category they chose.
/// This is the many-to-many junction table from the Part 1 ERD:
/// one User can enrol in many Events, and one Event has many enrolled Users.
/// A unique index on (ParticipantId, EventId) means a participant can only
/// enter an event once - a second attempt returns 409 Conflict.
/// </summary>
public class Enrolment
{
    public int EnrolmentId { get; set; }

    public int ParticipantId { get; set; }                 // FK -> Users

    public int EventId { get; set; }                       // FK -> Events

    public int CategoryId { get; set; }                    // FK -> Categories

    public DateTime EnrolmentDate { get; set; } = DateTime.UtcNow;

    /// <summary>"Pending" (amber) or "Confirmed" (green) - colour coded in the MVC UI.</summary>
    public string Status { get; set; } = EnrolmentStatus.Pending;

    // ---- navigation properties ----
    public User Participant { get; set; } = null!;
    public Event Event { get; set; } = null!;
    public Category Category { get; set; } = null!;
    public Result? Result { get; set; }
}

/// <summary>Allowed enrolment status values (displayed with colour coding in the MVC app).</summary>
public static class EnrolmentStatus
{
    public const string Pending = "Pending";
    public const string Confirmed = "Confirmed";

    public static bool IsValid(string? status)
        => status == Pending || status == Confirmed;
}
