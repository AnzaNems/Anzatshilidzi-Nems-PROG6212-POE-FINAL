using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using RaceDayMvc.Models;

namespace RaceDayMvc.Controllers;

/// <summary>
/// Organiser's enrolments page for one event: all participants with their
/// chosen category and status, plus the Confirm button that flips a pending
/// enrolment to Confirmed.
/// </summary>
public class EnrolmentsController : BaseController
{
    public EnrolmentsController(IHttpClientFactory f) : base(f) { }

    [HttpGet]
    public async Task<IActionResult> Index(int eventId)
    {
        var guard = Guard("Organiser");
        if (guard != null) return guard!;

        EventDetailViewModel? evt = null;
        List<EnrolmentViewModel> enrolments = new();
        try
        {
            var client = CreateClient();
            evt = await client.GetFromJsonAsync<EventDetailViewModel>($"/api/events/{eventId}");
            enrolments = await client.GetFromJsonAsync<List<EnrolmentViewModel>>($"/api/events/{eventId}/enrolments") ?? new();
        }
        catch (HttpRequestException)
        {
            ViewBag.ApiError = "The RaceDay API is not reachable. Start RaceDayApi first.";
            return View("Error");
        }

        if (evt == null) return NotFound();
        if (evt.OrganiserId != UserId) return View("AccessDenied");

        ViewBag.Event = evt;
        return View(enrolments);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Confirm(int enrolmentId, int eventId)
    {
        var guard = Guard("Organiser");
        if (guard != null) return guard!;

        try
        {
            var client = CreateClient();
            var response = await client.PutAsJsonAsync($"/api/enrolments/{enrolmentId}/status",
                new { status = "Confirmed" });

            TempData[response.IsSuccessStatusCode ? "Success" : "Error"] =
                response.IsSuccessStatusCode ? "Enrolment confirmed." : "Could not confirm the enrolment.";
        }
        catch (HttpRequestException)
        {
            TempData["Error"] = "The RaceDay API is not reachable. Start RaceDayApi first.";
        }
        return RedirectToAction("Index", new { eventId });
    }
}
