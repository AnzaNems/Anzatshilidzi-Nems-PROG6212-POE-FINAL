namespace RaceDayApi.Models;

/// <summary>
/// A person who can log in to RaceDay. The Role property decides what the user
/// may do: "Organiser" manages events, categories, enrolments and results;
/// "Participant" browses events, enrols and views personal results.
/// Passwords are never stored in plain text - only the BCrypt hash.
/// </summary>
public class User
{
    public int UserId { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string HashPassword { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;   // "Organiser" or "Participant"

    /// <summary>Full Azure Blob URL of the profile picture (Part 3, optional).</summary>
    public string? ProfilePictureUrl { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // ---- navigation properties ----
    public ICollection<Event> Events { get; set; } = new List<Event>();      // events this user organises
    public ICollection<Enrolment> Enrolments { get; set; } = new List<Enrolment>();
}
