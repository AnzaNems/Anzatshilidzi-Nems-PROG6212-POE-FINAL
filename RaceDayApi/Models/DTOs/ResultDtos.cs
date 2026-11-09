using System.ComponentModel.DataAnnotations;

namespace RaceDayApi.Models.DTOs;

/// <summary>
/// Result capture / update request. FinishTime is a string in HH:MM:SS format
/// (e.g. "03:45:12") so it binds directly from HTML time inputs and Swagger.
/// </summary>
public class SaveResultDto
{
    [Required]
    public string FinishTime { get; set; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int Position { get; set; }

    public bool IsDisqualified { get; set; }
}

/// <summary>
/// Result data returned to clients. TotalFinishers powers the
/// "47th out of 312 finishers" display on the My Results page.
/// </summary>
public class ResultDto
{
    public int ResultId { get; set; }
    public int EnrolmentId { get; set; }
    public int ParticipantId { get; set; }
    public string ParticipantName { get; set; } = string.Empty;
    public int EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public DateOnly EventDate { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string FinishTime { get; set; } = string.Empty;
    public int Position { get; set; }
    public bool IsDisqualified { get; set; }
    public int TotalFinishers { get; set; }

    public static string Format(TimeSpan t) => t.ToString(@"hh\:mm\:ss");
}

/// <summary>Response of POST /api/files (Azure Blob upload).</summary>
public class BlobUploadResult
{
    public string Url { get; set; } = string.Empty;
    public string BlobName { get; set; } = string.Empty;
}
