using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using RaceDayMvc.Models;

namespace RaceDayMvc.Controllers;

/// <summary>Participant's "My Events" page: every event they entered, with status badges.</summary>
public class MyEventsController : BaseController
{
    public MyEventsController(IHttpClientFactory f) : base(f) { }

    public async Task<IActionResult> Index()
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
        return View(enrolments);
    }
}
