using System.ComponentModel.DataAnnotations;

namespace RaceDayApi.Models.DTOs;

/// <summary>Enrolment request: the participant picks a category for an event.</summary>
public class CreateEnrolmentDto
{
    [Range(1, int.MaxValue)]
    public int CategoryId { get; set; }
}

/// <summary>Status update request used by Organisers (Pending / Confirmed).</summary>
public class UpdateEnrolmentStatusDto
{
    [Required]
    public string Status { get; set; } = string.Empty;
}

/// <summary>Enrolment data returned to clients.</summary>
public class EnrolmentDto
{
    public int EnrolmentId { get; set; }
    public int ParticipantId { get; set; }
    public string ParticipantName { get; set; } = string.Empty;
    public int EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public DateOnly EventDate { get; set; }
    public string Location { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime EnrolmentDate { get; set; }

    public static EnrolmentDto From(Enrolment x) => new()
    {
        EnrolmentId = x.EnrolmentId,
        ParticipantId = x.ParticipantId,
        ParticipantName = x.Participant?.FullName ?? string.Empty,
        EventId = x.EventId,
        EventName = x.Event?.EventName ?? string.Empty,
        EventDate = x.Event?.EventDate ?? default,
        Location = x.Event?.Location ?? string.Empty,
        CategoryId = x.CategoryId,
        CategoryName = x.Category?.CategoryName ?? string.Empty,
        Status = x.Status,
        EnrolmentDate = x.EnrolmentDate
    };
}
