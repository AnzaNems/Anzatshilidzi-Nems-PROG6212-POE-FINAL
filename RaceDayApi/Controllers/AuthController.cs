using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using RaceDayApi.Data;
using RaceDayApi.Models;
using RaceDayApi.Models.DTOs;

namespace RaceDayApi.Controllers;

/// <summary>Current-user helper: reads the user id from the JWT NameIdentifier claim.</summary>
public static class ClaimsPrincipalExtensions
{
    public static int GetUserId(this ClaimsPrincipal user)
        => int.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;
}

/// <summary>
/// Authentication endpoints: registration and login for both roles.
/// A successful login returns a JWT token that must be sent as a Bearer
/// token on all protected endpoints.
/// </summary>
[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;

    public AuthController(AppDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    /// <summary>
    /// Registers a new user account (Organiser or Participant).
    /// The password is hashed with BCrypt before storage - it is never saved in plain text.
    /// </summary>
    /// <remarks>
    /// Sample request body:
    ///
    ///     POST /api/auth/register
    ///     {
    ///       "fullName": "Sibusiso Nkosi",
    ///       "email": "sibusiso@raceday.co.za",
    ///       "password": "Pass@123",
    ///       "role": "Participant"
    ///     }
    ///
    /// The role value is case-insensitive and must be "Organiser" or "Participant".
    /// </remarks>
    /// <param name="dto">Full name, email, password and role.</param>
    /// <returns>201 Created with the new user (no password hash); 400 if a required field is missing, the role is invalid, or the email is already registered.</returns>
    [HttpPost("register")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register(RegisterDto dto)
    {
        // Normalise the role: accept "participant" / "ORGANISER" etc.
        var role = dto.Role?.Trim() ?? string.Empty;
        role = char.ToUpperInvariant(role[0]) + role[1..].ToLowerInvariant();

        if (role is not ("Organiser" or "Participant"))
            return BadRequest(new { message = "Role must be either Organiser or Participant." });

        var email = dto.Email.Trim();
        if (await _db.Users.AnyAsync(u => u.Email == email))
            return BadRequest(new { message = "An account with this email already exists." });

        var user = new User
        {
            FullName = dto.FullName.Trim(),
            Email = email,
            HashPassword = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            Role = role,
            CreatedAt = DateTime.UtcNow
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return StatusCode(StatusCodes.Status201Created, UserDto.From(user));
    }

    /// <summary>
    /// Authenticates a user with email and password and returns a signed JWT token.
    /// The token carries the user id, email and role, and is valid for 6 hours.
    /// </summary>
    /// <param name="dto">Email and password.</param>
    /// <returns>200 OK with token, userId, role and fullName; 401 if the credentials are invalid.</returns>
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(LoginDto dto)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == dto.Email.Trim());

        if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.HashPassword))
            return Unauthorized(new { message = "Invalid email or password." });

        return Ok(new LoginResponse
        {
            Token = GenerateJwtToken(user),
            UserId = user.UserId,
            Role = user.Role,
            FullName = user.FullName
        });
    }

    /// <summary>Creates the signed JWT with the three claims used across the API: NameIdentifier, Email and Role.</summary>
    private string GenerateJwtToken(User user)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role)
        };

        // In development the key falls back to a fixed dev-only string so the API
        // runs out of the box. In production you would move this to user-secrets.
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
            _config["Jwt:Key"] ?? "RaceDayDevOnlySigningKey_MustBeAtLeast32Chars!"));

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"] ?? "raceday-api",
            audience: _config["Jwt:Audience"] ?? "raceday-client",
            claims: claims,
            expires: DateTime.UtcNow.AddHours(6),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
