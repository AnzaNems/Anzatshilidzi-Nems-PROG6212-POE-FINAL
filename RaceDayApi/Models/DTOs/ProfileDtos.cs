using System.ComponentModel.DataAnnotations;

namespace RaceDayApi.Models.DTOs;

/// <summary>
/// Profile update request. ProfilePictureUrl is optional: the MVC app first
/// uploads the picture to /api/files (Azure Blob) and then sends the returned
/// URL here together with the updated name / email.
/// </summary>
public class UpdateProfileDto
{
    [Required, StringLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(255)]
    public string Email { get; set; } = string.Empty;

    [StringLength(500)]
    public string? ProfilePictureUrl { get; set; }
}
