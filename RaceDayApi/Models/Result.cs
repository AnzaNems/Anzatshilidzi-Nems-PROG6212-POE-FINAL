namespace RaceDayApi.Models;

/// <summary>
/// The race result captured by the Organiser after an event concludes:
/// finish time, finishing position and an optional disqualification flag.
/// One enrolment can have at most one result (unique index) - capturing
/// a second result for the same enrolment returns 409 Conflict.
/// </summary>
public class Result
{
    public int ResultId { get; set; }

    public int EnrolmentId { get; set; }                   // FK -> Enrolments (unique)

    /// <summary>Finish time, e.g. 03:45:12. Mapped to a SQL time(7) column.</summary>
    public TimeSpan FinishTime { get; set; }

    /// <summary>Finishing position within the event, e.g. 47.</summary>
    public int Position { get; set; }

    public bool IsDisqualified { get; set; }

    public DateTime CapturedAt { get; set; } = DateTime.UtcNow;

    // ---- navigation properties ----
    public Enrolment Enrolment { get; set; } = null!;
}
