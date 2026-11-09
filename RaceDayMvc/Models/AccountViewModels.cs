using System.ComponentModel.DataAnnotations;

namespace RaceDayMvc.Models;

/// <summary>Login form data.</summary>
public class LoginViewModel
{
    [Required, EmailAddress]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Required, DataType(DataType.Password)]
    [Display(Name = "Password")]
    public string Password { get; set; } = string.Empty;
}

/// <summary>Registration form data - the role is picked with radio buttons.</summary>
public class RegisterViewModel
{
    [Required, Display(Name = "Full name")]
    public string FullName { get; set; } = string.Empty;

    [Required, EmailAddress, Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Required, MinLength(6), DataType(DataType.Password), Display(Name = "Password")]
    public string Password { get; set; } = string.Empty;

    [Required, Display(Name = "I am a(n)")]
    public string Role { get; set; } = "Participant";
}

/// <summary>Mirror of the API's login response (MVC never references the API project).</summary>
public class LoginResponse
{
    public string Token { get; set; } = string.Empty;
    public int UserId { get; set; }
    public string Role { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
}

/// <summary>Mirror of the API's user profile DTO.</summary>
public class UserProfileDto
{
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? ProfilePictureUrl { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>Minimal model for the shared error page.</summary>
public class ErrorViewModel
{
    public string? RequestId { get; set; }
    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
}
