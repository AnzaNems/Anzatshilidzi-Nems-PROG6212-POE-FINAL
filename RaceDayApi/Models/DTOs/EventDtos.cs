using System.ComponentModel.DataAnnotations;

namespace RaceDayApi.Models.DTOs;

/// <summary>
/// Create / update event request. Covers every field the POE requires:
/// name, description, date, location, distance and event type (Run / Walk / Cycle).
/// BannerImageUrl is optional and comes from the /api/files upload.
/// </summary>
public class SaveEventDto
{
    [Required, StringLength(150)]
    public string EventName { get; set; } = string.Empty;

    [Required]
    public string Description { get; set; } = string.Empty;

    [Required]
    public DateOnly EventDate { get; set; }

    [Required, StringLength(200)]
    public string Location { get; set; } = string.Empty;

    [Range(0.1, 1000)]
    public decimal DistanceKm { get; set; }

    [Range(1, int.MaxValue)]
    public int EventTypeId { get; set; }

    [StringLength(500)]
    public string? BannerImageUrl { get; set; }
}

/// <summary>Event data returned to clients.</summary>
public class EventDto
{
    public int EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateOnly EventDate { get; set; }
    public string Location { get; set; } = string.Empty;
    public decimal DistanceKm { get; set; }
    public int EventTypeId { get; set; }
    public string EventTypeName { get; set; } = string.Empty;
    public string? BannerImageUrl { get; set; }
    public int OrganiserId { get; set; }
    public string OrganiserName { get; set; } = string.Empty;
    public int CategoryCount { get; set; }
    public int EnrolmentCount { get; set; }
    public bool IsPast { get; set; }

    public static EventDto From(Event e)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return new EventDto
        {
            EventId = e.EventId,
            EventName = e.EventName,
            Description = e.Description,
            EventDate = e.EventDate,
            Location = e.Location,
            DistanceKm = e.DistanceKm,
            EventTypeId = e.EventTypeId,
            EventTypeName = e.EventType?.TypeName ?? string.Empty,
            BannerImageUrl = e.BannerImageUrl,
            OrganiserId = e.OrganiserId,
            OrganiserName = e.Organiser?.FullName ?? string.Empty,
            CategoryCount = e.Categories?.Count ?? 0,
            EnrolmentCount = e.Enrolments?.Count ?? 0,
            IsPast = e.EventDate < today
        };
    }
}

/// <summary>Event detail including its categories - response of GET /api/events/{id}.</summary>
public class EventDetailDto : EventDto
{
    public List<CategoryDto> Categories { get; set; } = new();
}
