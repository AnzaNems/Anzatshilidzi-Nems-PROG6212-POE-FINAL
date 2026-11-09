namespace RaceDayMvc.Models;

/// <summary>Mirror of the API's EnrolmentDto - used by both My Events and the organiser's enrolments page.</summary>
public class EnrolmentViewModel
{
    public int EnrolmentId { get; set; }
    public int ParticipantId { get; set; }
    public string ParticipantName { get; set; } = string.Empty;
    public int EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public string Location { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;   // Pending / Confirmed
    public DateTime EnrolmentDate { get; set; }
}
