namespace RaceDayApi.Models;

/// <summary>
/// An age or distance category that belongs to one event, for example
/// "Under 20", "Senior" or "10km". Participants pick one category when
/// they enter an event. A category with active enrolments cannot be
/// deleted - the API returns 409 Conflict instead.
/// </summary>
public class Category
{
    public int CategoryId { get; set; }

    public int EventId { get; set; }                       // FK -> Events

    public string CategoryName { get; set; } = string.Empty;

    /// <summary>Category distance in kilometres, e.g. 10.0 or 21.1.</summary>
    public decimal DistanceKm { get; set; }

    public decimal EntryFee { get; set; }                  // in Rand

    // ---- navigation properties ----
    public Event Event { get; set; } = null!;
    public ICollection<Enrolment> Enrolments { get; set; } = new List<Enrolment>();
}
