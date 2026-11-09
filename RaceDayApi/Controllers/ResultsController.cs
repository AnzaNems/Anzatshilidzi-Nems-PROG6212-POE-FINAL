using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDayApi.Data;
using RaceDayApi.Models;
using RaceDayApi.Models.DTOs;

namespace RaceDayApi.Controllers;

/// <summary>
/// Result endpoints. After an event concludes, the Organiser captures finish
/// times, finishing positions and disqualifications. Participants view their
/// own personal race history.
/// </summary>
[ApiController]
[Route("api")]
[Produces("application/json")]
public class ResultsController : ControllerBase
{
    private readonly AppDbContext _db;

    public ResultsController(AppDbContext db) => _db = db;

    /// <summary>
    /// Captures the finish time and position for one enrolment, and optionally
    /// disqualifies the participant. Each enrolment can have exactly one result -
    /// capturing a second returns 409 Conflict. Only the Organiser who owns the
    /// event may capture results.
    /// </summary>
    /// <param name="enrolmentId">The enrolment to record a result for.</param>
    /// <param name="dto">FinishTime in HH:MM:SS format (e.g. "03:45:12"), the finishing position and the disqualification flag.</param>
    /// <returns>201 Created with the result; 400 if the time format is invalid; 401/403 if not the owning Organiser; 404 if the enrolment does not exist; 409 if a result already exists.</returns>
    [HttpPost("enrolments/{enrolmentId:int}/result")]
    [Authorize(Roles = "Organiser")]
    [ProducesResponseType(typeof(ResultDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CaptureResult(int enrolmentId, SaveResultDto dto)
    {
        var enrolment = await _db.Enrolments
            .Include(x => x.Participant)
            .Include(x => x.Event)
            .Include(x => x.Category)
            .FirstOrDefaultAsync(x => x.EnrolmentId == enrolmentId);

        if (enrolment == null) return NotFound(new { message = "Enrolment not found." });
        if (enrolment.Event.OrganiserId != User.GetUserId()) return Forbid();

        if (await _db.Results.AnyAsync(r => r.EnrolmentId == enrolmentId))
            return Conflict(new { message = "A result already exists for this enrolment." });

        if (!TimeSpan.TryParse(dto.FinishTime, out var finishTime) || finishTime <= TimeSpan.Zero)
            return BadRequest(new { message = "FinishTime must be a valid time in HH:MM:SS format, e.g. 03:45:12." });

        var result = new Result
        {
            EnrolmentId = enrolmentId,
            FinishTime = finishTime,
            Position = dto.Position,
            IsDisqualified = dto.IsDisqualified,
            CapturedAt = DateTime.UtcNow
        };

        _db.Results.Add(result);
        await _db.SaveChangesAsync();

        return StatusCode(StatusCodes.Status201Created, ToDto(result, enrolment, 1));
    }

    /// <summary>
    /// Updates an existing race result, for example to correct a time that was
    /// captured wrongly. Only the Organiser who owns the event may update it.
    /// </summary>
    /// <param name="id">The result id.</param>
    /// <param name="dto">The corrected finish time, position and disqualification flag.</param>
    /// <returns>200 OK with the updated result; 400 if the time format is invalid; 401/403 if not the owning Organiser; 404 if the result does not exist.</returns>
    [HttpPut("results/{id:int}")]
    [Authorize(Roles = "Organiser")]
    [ProducesResponseType(typeof(ResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateResult(int id, SaveResultDto dto)
    {
        var result = await _db.Results
            .Include(r => r.Enrolment)
                .ThenInclude(x => x.Participant)
            .Include(r => r.Enrolment)
                .ThenInclude(x => x.Event)
            .Include(r => r.Enrolment)
                .ThenInclude(x => x.Category)
            .FirstOrDefaultAsync(r => r.ResultId == id);

        if (result == null) return NotFound(new { message = "Result not found." });
        if (result.Enrolment.Event.OrganiserId != User.GetUserId()) return Forbid();

        if (!TimeSpan.TryParse(dto.FinishTime, out var finishTime) || finishTime <= TimeSpan.Zero)
            return BadRequest(new { message = "FinishTime must be a valid time in HH:MM:SS format, e.g. 03:45:12." });

        result.FinishTime = finishTime;
        result.Position = dto.Position;
        result.IsDisqualified = dto.IsDisqualified;
        await _db.SaveChangesAsync();

        return Ok(ToDto(result, result.Enrolment, 1));
    }

    /// <summary>
    /// Retrieves the personal race history of the logged-in Participant across
    /// all completed events: event name, date, category, finish time and
    /// finishing position out of the total number of finishers - the data
    /// behind the "My Results" page (e.g. 47th out of 312 finishers).
    /// </summary>
    /// <returns>200 OK with an array of the participant's own results; 401 if not logged in; 403 if the user is not a Participant.</returns>
    [HttpGet("results/me")]
    [Authorize(Roles = "Participant")]
    [ProducesResponseType(typeof(List<ResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> MyResults()
    {
        var userId = User.GetUserId();

        var results = await _db.Results
            .Include(r => r.Enrolment)
                .ThenInclude(x => x.Participant)
            .Include(r => r.Enrolment)
                .ThenInclude(x => x.Event)
            .Include(r => r.Enrolment)
                .ThenInclude(x => x.Category)
            .Where(r => r.Enrolment.ParticipantId == userId)
            .OrderByDescending(r => r.Enrolment.Event.EventDate)
            .AsNoTracking()
            .ToListAsync();

        // Total (non-disqualified) finishers per event, computed in memory so it
        // works identically on SQL Server and the InMemory test provider.
        var totals = results
            .GroupBy(r => r.Enrolment.EventId)
            .ToDictionary(g => g.Key, g => g.Count(x => !x.IsDisqualified));

        return Ok(results.Select(r => ToDto(r, r.Enrolment, totals.GetValueOrDefault(r.Enrolment.EventId))));
    }

    /// <summary>
    /// (Additional endpoint) Retrieves all results captured for one event -
    /// this powers the Organiser's results entry page, which shows existing
    /// results next to enrolments that still need one. Only the Organiser who
    /// owns the event may call this.
    /// </summary>
    /// <param name="eventId">The event id.</param>
    /// <returns>200 OK with an array of results; 401/403 if not the owning Organiser; 404 if the event does not exist.</returns>
    [HttpGet("events/{eventId:int}/results")]
    [Authorize(Roles = "Organiser")]
    [ProducesResponseType(typeof(List<ResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> EventResults(int eventId)
    {
        var evt = await _db.Events.FirstOrDefaultAsync(e => e.EventId == eventId && !e.IsDeleted);
        if (evt == null) return NotFound(new { message = "Event not found." });
        if (evt.OrganiserId != User.GetUserId()) return Forbid();

        var results = await _db.Results
            .Include(r => r.Enrolment)
                .ThenInclude(x => x.Participant)
            .Include(r => r.Enrolment)
                .ThenInclude(x => x.Event)
            .Include(r => r.Enrolment)
                .ThenInclude(x => x.Category)
            .Where(r => r.Enrolment.EventId == eventId)
            .OrderBy(r => r.Position)
            .AsNoTracking()
            .ToListAsync();

        var totalFinishers = results.Count(r => !r.IsDisqualified);
        return Ok(results.Select(r => ToDto(r, r.Enrolment, totalFinishers)));
    }

    private static ResultDto ToDto(Result r, Enrolment x, int totalFinishers) => new()
    {
        ResultId = r.ResultId,
        EnrolmentId = r.EnrolmentId,
        ParticipantId = x.ParticipantId,
        ParticipantName = x.Participant?.FullName ?? string.Empty,
        EventId = x.EventId,
        EventName = x.Event?.EventName ?? string.Empty,
        EventDate = x.Event?.EventDate ?? default,
        CategoryName = x.Category?.CategoryName ?? string.Empty,
        FinishTime = ResultDto.Format(r.FinishTime),
        Position = r.Position,
        IsDisqualified = r.IsDisqualified,
        TotalFinishers = totalFinishers
    };
}
