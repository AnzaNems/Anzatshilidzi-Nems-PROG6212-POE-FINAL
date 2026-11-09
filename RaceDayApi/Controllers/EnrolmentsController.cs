using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDayApi.Data;
using RaceDayApi.Models;
using RaceDayApi.Models.DTOs;

namespace RaceDayApi.Controllers;

/// <summary>
/// Enrolment endpoints. A Participant enters an event by selecting a category;
/// the system records the link between the Participant, the Event and the
/// Category. Organisers view all enrolments for their own events and can
/// confirm a pending enrolment.
/// </summary>
[ApiController]
[Route("api")]
[Produces("application/json")]
public class EnrolmentsController : ControllerBase
{
    private readonly AppDbContext _db;

    public EnrolmentsController(AppDbContext db) => _db = db;

    /// <summary>
    /// Enters the logged-in Participant into an event with a chosen category.
    /// The participant is taken from the JWT - you can never enrol somebody else.
    /// One entry per participant per event: a second attempt returns 409 Conflict.
    /// </summary>
    /// <param name="eventId">The event to enter.</param>
    /// <param name="dto">The chosen categoryId (must belong to this event).</param>
    /// <returns>201 Created with the enrolment (status starts as Pending); 400 if the category does not belong to the event or the event already took place; 401 if not logged in; 403 if the user is not a Participant; 404 if the event does not exist; 409 if already enrolled.</returns>
    [HttpPost("events/{eventId:int}/enrolments")]
    [Authorize(Roles = "Participant")]
    [ProducesResponseType(typeof(EnrolmentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Enrol(int eventId, CreateEnrolmentDto dto)
    {
        var userId = User.GetUserId();

        var evt = await _db.Events.FirstOrDefaultAsync(e => e.EventId == eventId && !e.IsDeleted);
        if (evt == null) return NotFound(new { message = "Event not found." });

        if (evt.EventDate < DateOnly.FromDateTime(DateTime.UtcNow))
            return BadRequest(new { message = "This event has already taken place and is closed for entry." });

        var category = await _db.Categories
            .FirstOrDefaultAsync(c => c.CategoryId == dto.CategoryId && c.EventId == eventId);
        if (category == null)
            return BadRequest(new { message = "The selected category does not belong to this event." });

        if (await _db.Enrolments.AnyAsync(x => x.ParticipantId == userId && x.EventId == eventId))
            return Conflict(new { message = "You are already enrolled for this event." });

        var enrolment = new Enrolment
        {
            ParticipantId = userId,
            EventId = eventId,
            CategoryId = category.CategoryId,
            EnrolmentDate = DateTime.UtcNow,
            Status = EnrolmentStatus.Pending
        };

        _db.Enrolments.Add(enrolment);
        await _db.SaveChangesAsync();

        await _db.Entry(enrolment).Reference(x => x.Participant).LoadAsync();
        await _db.Entry(enrolment).Reference(x => x.Event).LoadAsync();
        await _db.Entry(enrolment).Reference(x => x.Category).LoadAsync();

        return StatusCode(StatusCodes.Status201Created, EnrolmentDto.From(enrolment));
    }

    /// <summary>
    /// Retrieves all enrolments (past and upcoming) for the logged-in Participant,
    /// including the event details and the chosen category - this powers the
    /// "My Events" page.
    /// </summary>
    /// <returns>200 OK with an array of enrolments; 401 if not logged in; 403 if the user is not a Participant.</returns>
    [HttpGet("enrolments/me")]
    [Authorize(Roles = "Participant")]
    [ProducesResponseType(typeof(List<EnrolmentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> MyEnrolments()
    {
        var userId = User.GetUserId();

        var enrolments = await _db.Enrolments
            .Include(x => x.Participant)
            .Include(x => x.Event)
            .Include(x => x.Category)
            .Where(x => x.ParticipantId == userId)
            .OrderByDescending(x => x.Event.EventDate)
            .AsNoTracking()
            .ToListAsync();

        return Ok(enrolments.Select(EnrolmentDto.From));
    }

    /// <summary>
    /// Retrieves all Participants enrolled in a specific event, including their
    /// chosen category and enrolment status - this powers the Organiser's
    /// enrolments page. Only the Organiser who owns the event may call this.
    /// </summary>
    /// <param name="eventId">The event id.</param>
    /// <returns>200 OK with an array of enrolments; 401 if not logged in; 403 if the user is not the owning Organiser; 404 if the event does not exist.</returns>
    [HttpGet("events/{eventId:int}/enrolments")]
    [Authorize(Roles = "Organiser")]
    [ProducesResponseType(typeof(List<EnrolmentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> EventEnrolments(int eventId)
    {
        var evt = await _db.Events.FirstOrDefaultAsync(e => e.EventId == eventId && !e.IsDeleted);
        if (evt == null) return NotFound(new { message = "Event not found." });
        if (evt.OrganiserId != User.GetUserId()) return Forbid();

        var enrolments = await _db.Enrolments
            .Include(x => x.Participant)
            .Include(x => x.Event)
            .Include(x => x.Category)
            .Where(x => x.EventId == eventId)
            .OrderBy(x => x.Participant.FullName)
            .AsNoTracking()
            .ToListAsync();

        return Ok(enrolments.Select(EnrolmentDto.From));
    }

    /// <summary>
    /// (Additional endpoint) Updates an enrolment status to Confirmed or Pending -
    /// this is what turns the amber badge green on the Organiser's enrolments page.
    /// Only the Organiser who owns the event may change a status.
    /// </summary>
    /// <param name="id">The enrolment id.</param>
    /// <param name="dto">The new status: "Confirmed" or "Pending".</param>
    /// <returns>200 OK with the updated enrolment; 400 if the status value is invalid; 401/403 if not the owning Organiser; 404 if the enrolment does not exist.</returns>
    [HttpPut("enrolments/{id:int}/status")]
    [Authorize(Roles = "Organiser")]
    [ProducesResponseType(typeof(EnrolmentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateStatus(int id, UpdateEnrolmentStatusDto dto)
    {
        var enrolment = await _db.Enrolments
            .Include(x => x.Participant)
            .Include(x => x.Event)
            .Include(x => x.Category)
            .FirstOrDefaultAsync(x => x.EnrolmentId == id);

        if (enrolment == null) return NotFound(new { message = "Enrolment not found." });
        if (enrolment.Event.OrganiserId != User.GetUserId()) return Forbid();

        if (!EnrolmentStatus.IsValid(dto.Status?.Trim()))
            return BadRequest(new { message = "Status must be either Confirmed or Pending." });

        enrolment.Status = dto.Status!.Trim();
        await _db.SaveChangesAsync();

        return Ok(EnrolmentDto.From(enrolment));
    }
}
