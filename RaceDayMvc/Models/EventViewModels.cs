using System.ComponentModel.DataAnnotations;

namespace RaceDayMvc.Models;

/// <summary>Mirror of the API's EventDto.</summary>
public class EventViewModel
{
    public int EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }          // DateOnly arrives as a date-only JSON value
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
}

/// <summary>Event detail including categories (GET /api/events/{id}).</summary>
public class EventDetailViewModel : EventViewModel
{
    public List<CategoryViewModel> Categories { get; set; } = new();
}

/// <summary>Mirror of the API's CategoryDto.</summary>
public class CategoryViewModel
{
    public int CategoryId { get; set; }
    public int EventId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public decimal DistanceKm { get; set; }
    public decimal EntryFee { get; set; }
    public int EnrolmentCount { get; set; }
}

/// <summary>Filter form on the home page.</summary>
public class EventFilterViewModel
{
    public string? Search { get; set; }
    public string? Location { get; set; }
    public int EventTypeId { get; set; }              // 0 = all types
}

/// <summary>Create / edit event form (with banner upload).</summary>
public class SaveEventViewModel
{
    public int EventId { get; set; }

    [Required, Display(Name = "Event name")]
    public string EventName { get; set; } = string.Empty;

    [Required, Display(Name = "Description")]
    public string Description { get; set; } = string.Empty;

    [Required, Display(Name = "Event date")]
    [DataType(DataType.Date)]
    public DateTime EventDate { get; set; } = DateTime.Today.AddMonths(1);

    [Required, Display(Name = "Location")]
    public string Location { get; set; } = string.Empty;

    [Required, Range(0.1, 1000), Display(Name = "Distance (km)")]
    public decimal DistanceKm { get; set; }

    [Required, Display(Name = "Event type")]
    public int EventTypeId { get; set; } = 1;         // 1 Run, 2 Walk, 3 Cycle

    [Display(Name = "Banner image (optional)")]
    public IFormFile? BannerImage { get; set; }

    public string? BannerImageUrl { get; set; }       // existing banner when editing
}

/// <summary>Category create / edit form.</summary>
public class SaveCategoryViewModel
{
    public int CategoryId { get; set; }
    public int EventId { get; set; }
    public string EventName { get; set; } = string.Empty;

    [Required, Display(Name = "Category name")]
    public string CategoryName { get; set; } = string.Empty;

    [Required, Range(0.1, 1000), Display(Name = "Distance (km)")]
    public decimal DistanceKm { get; set; }

    [Required, Range(0, 100000), Display(Name = "Entry fee (R)")]
    public decimal EntryFee { get; set; }
}

/// <summary>Response of POST /api/files.</summary>
public class BlobUploadResult
{
    public string Url { get; set; } = string.Empty;
    public string BlobName { get; set; } = string.Empty;
}

/// <summary>Small formatting helpers used by the views.</summary>
public static class FormatHelpers
{
    /// <summary>1 -> 1st, 2 -> 2nd, 3 -> 3rd, 4 -> 4th ... 47 -> 47th.</summary>
    public static string Ordinal(int n)
    {
        if (n <= 0) return n.ToString();
        return n % 100 is 11 or 12 or 13 ? $"{n}th"
             : n % 10 == 1 ? $"{n}st"
             : n % 10 == 2 ? $"{n}nd"
             : n % 10 == 3 ? $"{n}rd"
             : $"{n}th";
    }
}
