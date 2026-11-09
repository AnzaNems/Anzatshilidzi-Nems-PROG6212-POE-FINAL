using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDayApi.Data;
using RaceDayApi.Models;
using RaceDayApi.Models.DTOs;

namespace RaceDayApi.Controllers;

/// <summary>
/// Category endpoints: age or distance categories that belong to an event.
/// Both roles can view categories; only the owning Organiser can manage them.
/// A category with active enrolments cannot be deleted (409 Conflict).
/// </summary>
[ApiController]
[Route("api")]
[Produces("application/json")]
public class CategoriesController : ControllerBase
{
    private readonly AppDbContext _db;

    public CategoriesController(AppDbContext db) => _db = db;

    /// <summary>
    /// Retrieves all categories for a specific event, so a Participant can
    /// choose one when entering.
    /// </summary>
    /// <param name="eventId">The event id.</param>
    /// <returns>200 OK with an array of categories; 404 if the event does not exist.</returns>
    [HttpGet("events/{eventId:int}/categories")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(List<CategoryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCategories(int eventId)
    {
        if (!await _db.Events.AnyAsync(e => e.EventId == eventId && !e.IsDeleted))
            return NotFound(new { message = "Event not found." });

        var categories = await _db.Categories
            .Include(c => c.Enrolments)
            .Where(c => c.EventId == eventId)
            .OrderBy(c => c.CategoryName)
            .AsNoTracking()
            .ToListAsync();

        return Ok(categories.Select(CategoryDto.From));
    }

    /// <summary>
    /// Adds a new category to a specific event, for example "Under 20" or "10km".
    /// Only the Organiser who owns the event may add categories.
    /// </summary>
    /// <param name="eventId">The event id.</param>
    /// <param name="dto">Category name, distance in km and entry fee in Rand.</param>
    /// <returns>201 Created with the new category; 400 if validation fails; 401/403 if not the owning Organiser; 404 if the event does not exist.</returns>
    [HttpPost("events/{eventId:int}/categories")]
    [Authorize(Roles = "Organiser")]
    [ProducesResponseType(typeof(CategoryDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateCategory(int eventId, SaveCategoryDto dto)
    {
        var evt = await _db.Events.FirstOrDefaultAsync(e => e.EventId == eventId && !e.IsDeleted);
        if (evt == null) return NotFound(new { message = "Event not found." });
        if (evt.OrganiserId != User.GetUserId()) return Forbid();

        var category = new Category
        {
            EventId = eventId,
            CategoryName = dto.CategoryName.Trim(),
            DistanceKm = dto.DistanceKm,
            EntryFee = dto.EntryFee
        };

        _db.Categories.Add(category);
        await _db.SaveChangesAsync();

        return StatusCode(StatusCodes.Status201Created, CategoryDto.From(category));
    }

    /// <summary>
    /// Updates an existing category's name, distance or entry fee.
    /// Only the Organiser who owns the parent event may update it.
    /// </summary>
    /// <param name="id">The category id.</param>
    /// <param name="dto">The new category details.</param>
    /// <returns>200 OK with the updated category; 400 if validation fails; 401/403 if not the owning Organiser; 404 if the category does not exist.</returns>
    [HttpPut("categories/{id:int}")]
    [Authorize(Roles = "Organiser")]
    [ProducesResponseType(typeof(CategoryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateCategory(int id, SaveCategoryDto dto)
    {
        var category = await _db.Categories
            .Include(c => c.Event)
            .Include(c => c.Enrolments)
            .FirstOrDefaultAsync(c => c.CategoryId == id);

        if (category == null) return NotFound(new { message = "Category not found." });
        if (category.Event.OrganiserId != User.GetUserId()) return Forbid();

        category.CategoryName = dto.CategoryName.Trim();
        category.DistanceKm = dto.DistanceKm;
        category.EntryFee = dto.EntryFee;

        await _db.SaveChangesAsync();
        return Ok(CategoryDto.From(category));
    }

    /// <summary>
    /// Permanently deletes a category from an event - unless participants are
    /// already enrolled in it, in which case the API refuses with 409 Conflict
    /// so that enrolment history is never silently destroyed.
    /// Only the Organiser who owns the parent event may delete it.
    /// </summary>
    /// <param name="id">The category id.</param>
    /// <returns>204 No Content on success; 401/403 if not the owning Organiser; 404 if the category does not exist; 409 if the category has active enrolments.</returns>
    [HttpDelete("categories/{id:int}")]
    [Authorize(Roles = "Organiser")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteCategory(int id)
    {
        var category = await _db.Categories
            .Include(c => c.Event)
            .Include(c => c.Enrolments)
            .FirstOrDefaultAsync(c => c.CategoryId == id);

        if (category == null) return NotFound(new { message = "Category not found." });
        if (category.Event.OrganiserId != User.GetUserId()) return Forbid();

        if (category.Enrolments.Any())
            return Conflict(new { message = "This category has active enrolments and cannot be deleted." });

        _db.Categories.Remove(category);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
