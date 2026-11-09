using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using RaceDayMvc.Models;

namespace RaceDayMvc.Controllers;

/// <summary>
/// Public home page: hero + all upcoming events with filtering by event type,
/// location and free-text search (as required by the Participant interface).
/// </summary>
public class HomeController : BaseController
{
    public HomeController(IHttpClientFactory f) : base(f) { }

    public async Task<IActionResult> Index(EventFilterViewModel filter)
    {
        ViewBag.FullName = FullName;
        ViewBag.Role = Role;

        List<EventViewModel> events = new();
        try
        {
            var client = CreateClient();

            // Build the query string from the filter form (GET).
            var query = new List<string>();
            if (filter.EventTypeId > 0) query.Add($"eventTypeId={filter.EventTypeId}");
            if (!string.IsNullOrWhiteSpace(filter.Search))
                query.Add($"search={Uri.EscapeDataString(filter.Search.Trim())}");
            if (!string.IsNullOrWhiteSpace(filter.Location))
                query.Add($"location={Uri.EscapeDataString(filter.Location.Trim())}");

            var url = "/api/events" + (query.Count > 0 ? "?" + string.Join("&", query) : "");
            events = await client.GetFromJsonAsync<List<EventViewModel>>(url) ?? new();
        }
        catch (HttpRequestException)
        {
            ViewBag.ApiError = "The RaceDay API is not reachable. Make sure RaceDayApi is running (it should be at http://localhost:7123).";
        }

        ViewBag.Filter = filter;
        return View(events);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
        => View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
}
