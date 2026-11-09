namespace RaceDayApi.Models;

/// <summary>
/// Lookup entity for the type of a road event: Run, Walk or Cycle.
/// Kept in its own table (instead of a free text field) so the value is
/// normalised and can be used for filtering.
/// </summary>
public class EventType
{
    public int EventTypeId { get; set; }

    public string TypeName { get; set; } = string.Empty;   // "Run", "Walk", "Cycle"

    // ---- navigation properties ----
    public ICollection<Event> Events { get; set; } = new List<Event>();
}
