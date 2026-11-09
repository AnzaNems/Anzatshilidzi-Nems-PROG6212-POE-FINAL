using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using RaceDayMvc.Models;

namespace RaceDayMvc.Controllers;

/// <summary>
/// One dashboard, two faces: Organisers see their event stats, Participants
/// see their enrolment summary. The session role decides which view renders.
/// </summary>
public class DashboardController : BaseController
{
    public DashboardController(IHttpClientFactory f) : base(f) { }

    public async Task<IActionResult> Index()
    {
        if (!IsLoggedIn) return RedirectToAction("Login", "Account");
        return Role == "Organiser" ? await Organiser() : await Participant();
    }

    /// <summary>Organiser dashboard: my events, total enrolments per event, upcoming dates.</summary>
    public async Task<IActionResult> Organiser()
    {
        var guard = Guard("Organiser");
        if (guard != null) return guard!;

        List<EventViewModel> events = new();
        try
        {
            var client = CreateClient();
            events = await client.GetFromJsonAsync<List<EventViewModel>>("/api/events/mine") ?? new();
        }
        catch (HttpRequestException)
        {
            ViewBag.ApiError = "The RaceDay API is not reachable. Start RaceDayApi first.";
        }

        var today = DateTime.Today;
        ViewBag.TotalEvents = events.Count;
        ViewBag.UpcomingEvents = events.Count(e => e.EventDate.Date >= today);
        ViewBag.TotalEnrolments = events.Sum(e => e.EnrolmentCount);
        ViewBag.NextEvent = events.Where(e => e.EventDate.Date >= today).OrderBy(e => e.EventDate).FirstOrDefault();

        return View("Organiser", events);
    }

    /// <summary>Participant dashboard: enrolment summary + next entered event.</summary>
    public async Task<IActionResult> Participant()
    {
        var guard = Guard("Participant");
        if (guard != null) return guard!;

        List<EnrolmentViewModel> enrolments = new();
        try
        {
            var client = CreateClient();
            enrolments = await client.GetFromJsonAsync<List<EnrolmentViewModel>>("/api/enrolments/me") ?? new();
        }
        catch (HttpRequestException)
        {
            ViewBag.ApiError = "The RaceDay API is not reachable. Start RaceDayApi first.";
        }

        var today = DateTime.Today;
        ViewBag.TotalEnrolments = enrolments.Count;
        ViewBag.ConfirmedCount = enrolments.Count(e => e.Status == "Confirmed");
        ViewBag.NextEvent = enrolments.Where(e => e.EventDate.Date >= today).OrderBy(e => e.EventDate).FirstOrDefault();

        return View("Participant", enrolments);
    }
}
