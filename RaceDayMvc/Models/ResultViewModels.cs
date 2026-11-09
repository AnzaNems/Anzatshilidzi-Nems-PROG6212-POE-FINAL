using System.ComponentModel.DataAnnotations;

namespace RaceDayMvc.Models;

/// <summary>Mirror of the API's ResultDto - powers the My Results page.</summary>
public class ResultViewModel
{
    public int ResultId { get; set; }
    public int EnrolmentId { get; set; }
    public int ParticipantId { get; set; }
    public string ParticipantName { get; set; } = string.Empty;
    public int EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string FinishTime { get; set; } = string.Empty;
    public int Position { get; set; }
    public bool IsDisqualified { get; set; }
    public int TotalFinishers { get; set; }
}

/// <summary>One row on the organiser's results entry page: an enrolment plus its captured result (if any).</summary>
public class ResultEntryRowViewModel
{
    public int EnrolmentId { get; set; }
    public string ParticipantName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int? ResultId { get; set; }                     // null = no result captured yet

    [Required, Display(Name = "Finish time")]
    [RegularExpression(@"^\d{1,2}:\d{2}:\d{2}$", ErrorMessage = "Use HH:MM:SS, e.g. 03:45:12")]
    public string FinishTime { get; set; } = string.Empty;

    [Required, Range(1, int.MaxValue), Display(Name = "Position")]
    public int Position { get; set; }

    public bool IsDisqualified { get; set; }
}
