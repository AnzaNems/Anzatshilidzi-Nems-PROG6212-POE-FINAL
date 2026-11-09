using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDayApi.Data;
using RaceDayApi.Models;
using RaceDayApi.Models.DTOs;

namespace RaceDayApi.Controllers;

/// <summary>
/// User profile endpoints. Both roles (Organiser and Participant) can view
/// and update their own profile information.
/// </summary>
[ApiController]
[Route("api/profile")]
[Authorize]                                  // any authenticated user
[Produces("application/json")]
public class ProfileController : ControllerBase
{
    private readonly AppDbContext _db;

    public ProfileController(AppDbContext db) => _db = db;

    /// <summary>
    /// Retrieves the profile of the currently logged-in user
    /// (identified from the JWT, not from a URL parameter).
    /// </summary>
    /// <returns>200 OK with the user's profile; 401 if not logged in.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetProfile()
    {
        var user = await _db.Users.FindAsync(User.GetUserId());
        if (user == null) return Unauthorized();

        return Ok(UserDto.From(user));
    }

    /// <summary>
    /// Updates the full name and email of the currently logged-in user,
    /// and optionally stores a profile picture URL (uploaded first via /api/files).
    /// </summary>
    /// <param name="dto">New full name, email and optional profile picture URL.</param>
    /// <returns>200 OK with the updated user; 400 if the email is already used by another account; 401 if not logged in.</returns>
    [HttpPut]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdateProfile(UpdateProfileDto dto)
    {
        var userId = User.GetUserId();
        var user = await _db.Users.FindAsync(userId);
        if (user == null) return Unauthorized();

        var email = dto.Email.Trim();
        if (await _db.Users.AnyAsync(u => u.Email == email && u.UserId != userId))
            return BadRequest(new { message = "That email is already used by another account." });

        user.FullName = dto.FullName.Trim();
        user.Email = email;
        if (dto.ProfilePictureUrl != null)
            user.ProfilePictureUrl = dto.ProfilePictureUrl;

        await _db.SaveChangesAsync();
        return Ok(UserDto.From(user));
    }
}
