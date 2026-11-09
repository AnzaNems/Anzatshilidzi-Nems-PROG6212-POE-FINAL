using System.ComponentModel.DataAnnotations;

namespace RaceDayMvc.Models;

/// <summary>Profile page view data (from GET /api/profile).</summary>
public class ProfileViewModel
{
    public int UserId { get; set; }

    [Required, Display(Name = "Full name")]
    public string FullName { get; set; } = string.Empty;

    [Required, EmailAddress, Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;
    public string? ProfilePictureUrl { get; set; }
    public DateTime CreatedAt { get; set; }

    [Display(Name = "New profile picture (optional)")]
    public IFormFile? Picture { get; set; }
}
