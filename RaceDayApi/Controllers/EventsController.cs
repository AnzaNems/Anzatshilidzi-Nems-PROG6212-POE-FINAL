using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDayApi.Data;
using RaceDayApi.Models;
using RaceDayApi.Models.DTOs;

namespace RaceDayApi.Controllers;

/// <summary>
/// Event endpoints. Both roles (and anonymous visitors) can browse events;
/// only the Organiser who created an event may update or soft-delete it.
/// </summary>
[ApiController]
[Route("api/events")]
[Produces("application/json")]
public class EventsController : ControllerBase
{
    private readonly AppDbContext _db;

    public EventsController(AppDbContext db) => _db = db;

    /// <summary>
    /// Retrieves upcoming road events (run, walk, cycle). Optional query
    /// parameters filter by event type, location and free-text search -
    /// this powers the filtering on the MVC home page.
    /// </summary>
    /// <param name="eventTypeId">1 = Run, 2 = Walk, 3 = Cycle (optional).</param>
    /// <param name="location">Substring match on the location (optional).</param>
    /// <param name="search">Matches the event name or description (optional).</param>
    /// <param name="includePast">Set to true to include past events as well.</param>
    /// <returns>200 OK with an array of events (empty array if nothing matches).</returns>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(List<EventDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetEvents(
        [FromQuery] int? eventTypeId,
        [FromQuery] string? location,
        [FromQuery] string? search,
        [FromQuery] bool includePast = false)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var query = _db.Events
            .Include(e => e.Organiser)
            .Include(e => e.EventType)
            .Include(e => e.Categories)
            .Include(e => e.Enrolments)
            .Where(e => !e.IsDeleted);

        if (!includePast) query = query.Where(e => e.EventDate >= today);
        if (eventTypeId is > 0) query = query.Where(e => e.EventTypeId == eventTypeId);
        if (!string.IsNullOrWhiteSpace(location))
            query = query.Where(e => e.Location.Contains(location));
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(e => e.EventName.Contains(search) || e.Description.Contains(search));

        var events = await query
            .OrderBy(e => e.EventDate)
            .AsNoTracking()
            .ToListAsync();

        return Ok(events.Select(EventDto.From));
    }

    /// <summary>
    /// (Additional endpoint) Retrieves all events created by the logged-in
    /// Organiser, including past ones - this powers the Organiser dashboard
    /// and the results entry page for completed events.
    /// </summary>
    /// <returns>200 OK with the organiser's events; 401 if not logged in; 403 if the user is not an Organiser.</returns>
    [HttpGet("mine")]
    [Authorize(Roles = "Organiser")]
    [ProducesResponseType(typeof(List<EventDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> MyEvents()
    {
        var userId = User.GetUserId();
        var events = await _db.Events
            .Include(e => e.Organiser)
            .Include(e => e.EventType)
            .Include(e => e.Categories)
            .Include(e => e.Enrolments)
            .Where(e => e.OrganiserId == userId && !e.IsDeleted)
            .OrderBy(e => e.EventDate)
            .AsNoTracking()
            .ToListAsync();

        return Ok(events.Select(EventDto.From));
    }

    /// <summary>
    /// Retrieves full details for one event, including its available categories.
    /// </summary>
    /// <param name="id">The event id.</param>
    /// <returns>200 OK with the event and a Categories array; 404 if the event does not exist.</returns>
    [HttpGet("{id:int}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(EventDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEvent(int id)
    {
        var evt = await _db.Events
            .Include(e => e.Organiser)
            .Include(e => e.EventType)
            .Include(e => e.Categories)
                .ThenInclude(c => c.Enrolments)
            .Include(e => e.Enrolments)
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.EventId == id && !e.IsDeleted);

        if (evt == null) return NotFound(new { message = "Event not found." });

        var dto = new EventDetailDto();
        foreach (var prop in typeof(EventDto).GetProperties())
            prop.SetValue(dto, prop.GetValue(EventDto.From(evt))!);
        dto.Categories = evt.Categories.OrderBy(c => c.CategoryName).Select(CategoryDto.From).ToList();

        return Ok(dto);
    }

    /// <summary>
    /// Creates a new event. The organiser is taken from the JWT - it cannot be spoofed
    /// from the request body. Required fields: name, description, date, location,
    /// distance and event type (run / walk / cycle).
    /// </summary>
    /// <param name="dto">The event details, plus an optional banner image URL.</param>
    /// <returns>201 Created with the new event; 400 if validation fails; 401 if not logged in; 403 if the user is not an Organiser.</returns>
    [HttpPost]
    [Authorize(Roles = "Organiser")]
    [ProducesResponseType(typeof(EventDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateEvent(SaveEventDto dto)
    {
        if (!await _db.EventTypes.AnyAsync(t => t.EventTypeId == dto.EventTypeId))
            return BadRequest(new { message = "Unknown event type. Use 1 (Run), 2 (Walk) or 3 (Cycle)." });

        if (dto.EventDate < DateOnly.FromDateTime(DateTime.UtcNow))
            return BadRequest(new { message = "The event date cannot be in the past." });

        var evt = new Event
        {
            EventName = dto.EventName.Trim(),
            Description = dto.Description.Trim(),
            EventDate = dto.EventDate,
            Location = dto.Location.Trim(),
            DistanceKm = dto.DistanceKm,
            EventTypeId = dto.EventTypeId,
            BannerImageUrl = dto.BannerImageUrl,
            OrganiserId = User.GetUserId(),
            CreatedAt = DateTime.UtcNow
        };

        _db.Events.Add(evt);
        await _db.SaveChangesAsync();

        await _db.Entry(evt).Reference(e => e.Organiser).LoadAsync();
        await _db.Entry(evt).Reference(e => e.EventType).LoadAsync();

        return StatusCode(StatusCodes.Status201Created, EventDto.From(evt));
    }

    /// <summary>
    /// Updates an existing event. Only the Organiser who created the event may update it.
    /// </summary>
    /// <param name="id">The event id.</param>
    /// <param name="dto">The new event details.</param>
    /// <returns>200 OK with the updated event; 400 if validation fails; 401/403 if not the owning Organiser; 404 if the event does not exist.</returns>
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Organiser")]
    [ProducesResponseType(typeof(EventDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateEvent(int id, SaveEventDto dto)
    {
        var evt = await _db.Events
            .Include(e => e.Organiser)
            .Include(e => e.EventType)
            .Include(e => e.Categories)
            .Include(e => e.Enrolments)
            .FirstOrDefaultAsync(e => e.EventId == id && !e.IsDeleted);

        if (evt == null) return NotFound(new { message = "Event not found." });
        if (evt.OrganiserId != User.GetUserId()) return Forbid();

        if (!await _db.EventTypes.AnyAsync(t => t.EventTypeId == dto.EventTypeId))
            return BadRequest(new { message = "Unknown event type. Use 1 (Run), 2 (Walk) or 3 (Cycle)." });

        evt.EventName = dto.EventName.Trim();
        evt.Description = dto.Description.Trim();
        evt.EventDate = dto.EventDate;
        evt.Location = dto.Location.Trim();
        evt.DistanceKm = dto.DistanceKm;
        evt.EventTypeId = dto.EventTypeId;
        if (dto.BannerImageUrl != null)
            evt.BannerImageUrl = dto.BannerImageUrl;

        await _db.SaveChangesAsync();
        return Ok(EventDto.From(evt));
    }

    /// <summary>
    /// Soft-deletes an event (sets IsDeleted = true). Past enrolments and results
    /// are preserved in the database, and the event disappears from all listings.
    /// Only the Organiser who created the event may delete it.
    /// </summary>
    /// <param name="id">The event id.</param>
    /// <returns>204 No Content on success; 401/403 if not the owning Organiser; 404 if the event does not exist.</returns>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Organiser")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteEvent(int id)
    {
        var evt = await _db.Events.FirstOrDefaultAsync(e => e.EventId == id && !e.IsDeleted);
        if (evt == null) return NotFound(new { message = "Event not found." });
        if (evt.OrganiserId != User.GetUserId()) return Forbid();

        evt.IsDeleted = true;
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
