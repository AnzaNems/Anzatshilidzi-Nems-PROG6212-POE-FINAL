using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using RaceDayMvc.Models;

namespace RaceDayMvc.Controllers;

/// <summary>
/// Participant's personal race history: event, date, category, finish time and
/// finishing position out of the total number of finishers.
/// </summary>
public class MyResultsController : BaseController
{
    public MyResultsController(IHttpClientFactory f) : base(f) { }

    public async Task<IActionResult> Index()
    {
        var guard = Guard("Participant");
        if (guard != null) return guard!;

        List<ResultViewModel> results = new();
        try
        {
            var client = CreateClient();
            results = await client.GetFromJsonAsync<List<ResultViewModel>>("/api/results/me") ?? new();
        }
        catch (HttpRequestException)
        {
            ViewBag.ApiError = "The RaceDay API is not reachable. Start RaceDayApi first.";
        }
        return View(results);
    }
}
