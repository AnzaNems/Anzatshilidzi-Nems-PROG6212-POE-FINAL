using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using RaceDayMvc.Models;

namespace RaceDayMvc.Controllers;

/// <summary>
/// Organiser's results entry page: one row per enrolled participant where the
/// finish time, position and disqualification can be captured or corrected.
/// </summary>
public class ResultsController : BaseController
{
    public ResultsController(IHttpClientFactory f) : base(f) { }

    [HttpGet]
    public async Task<IActionResult> Entry(int eventId)
    {
        var guard = Guard("Organiser");
        if (guard != null) return guard!;

        EventDetailViewModel? evt = null;
        List<EnrolmentViewModel> enrolments = new();
        List<ResultViewModel> results = new();
        try
        {
            var client = CreateClient();
            evt = await client.GetFromJsonAsync<EventDetailViewModel>($"/api/events/{eventId}");
            enrolments = await client.GetFromJsonAsync<List<EnrolmentViewModel>>($"/api/events/{eventId}/enrolments") ?? new();
            results = await client.GetFromJsonAsync<List<ResultViewModel>>($"/api/events/{eventId}/results") ?? new();
        }
        catch (HttpRequestException)
        {
            ViewBag.ApiError = "The RaceDay API is not reachable. Start RaceDayApi first.";
            return View("Error");
        }

        if (evt == null) return NotFound();
        if (evt.OrganiserId != UserId) return View("AccessDenied");

        // Join enrolments with their results (if captured) into entry rows.
        var rows = enrolments
            .OrderBy(e => e.ParticipantName)
            .Select(e =>
            {
                var r = results.FirstOrDefault(x => x.EnrolmentId == e.EnrolmentId);
                return new ResultEntryRowViewModel
                {
                    EnrolmentId = e.EnrolmentId,
                    ParticipantName = e.ParticipantName,
                    CategoryName = e.CategoryName,
                    Status = e.Status,
                    ResultId = r?.ResultId,
                    FinishTime = r?.FinishTime ?? string.Empty,
                    Position = r?.Position ?? 0,
                    IsDisqualified = r?.IsDisqualified ?? false
                };
            })
            .ToList();

        ViewBag.Event = evt;
        return View(rows);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(ResultEntryRowViewModel row, int eventId)
    {
        var guard = Guard("Organiser");
        if (guard != null) return guard!;
        if (!ModelState.IsValid)
        {
            TempData["Error"] = $"Please give {row.ParticipantName} a valid time (HH:MM:SS) and position.";
            return RedirectToAction("Entry", new { eventId });
        }

        try
        {
            var client = CreateClient();
            var body = new
            {
                finishTime = row.FinishTime,
                position = row.Position,
                isDisqualified = row.IsDisqualified
            };

            // ResultId decides between capturing a new result and correcting an existing one.
            var response = row.ResultId is int id
                ? await client.PutAsJsonAsync($"/api/results/{id}", body)
                : await client.PostAsJsonAsync($"/api/enrolments/{row.EnrolmentId}/result", body);

            TempData[response.IsSuccessStatusCode ? "Success" : "Error"] =
                response.StatusCode == HttpStatusCode.Conflict
                    ? $"{row.ParticipantName} already has a result - use the row to correct it."
                    : response.IsSuccessStatusCode
                        ? $"Result saved for {row.ParticipantName}."
                        : "The API rejected the result - check the time format (HH:MM:SS).";
        }
        catch (HttpRequestException)
        {
            TempData["Error"] = "The RaceDay API is not reachable. Start RaceDayApi first.";
        }
        return RedirectToAction("Entry", new { eventId });
    }
}
