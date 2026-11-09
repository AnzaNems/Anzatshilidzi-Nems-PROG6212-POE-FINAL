using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using RaceDayMvc.Models;

namespace RaceDayMvc.Controllers;

/// <summary>
/// Everything about events in the MVC app: public details page, participant
/// entry, and the Organiser's create / edit / delete / manage screens.
/// </summary>
public class EventsController : BaseController
{
    public EventsController(IHttpClientFactory f) : base(f) { }

    // ---------------- DETAILS (public) ----------------

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        EventDetailViewModel? model = null;
        try
        {
            var client = CreateClient();
            model = await client.GetFromJsonAsync<EventDetailViewModel>($"/api/events/{id}");
        }
        catch (HttpRequestException)
        {
            ViewBag.ApiError = "The RaceDay API is not reachable. Start RaceDayApi first.";
            return View("Error");
        }

        if (model == null) return NotFound();

        ViewBag.CanManage = IsLoggedIn && Role == "Organiser" && model.OrganiserId == UserId;
        ViewBag.CanEnter = IsLoggedIn && Role == "Participant";
        return View(model);
    }

    // ---------------- ENTER EVENT (participant) ----------------

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Enter(int eventId, int categoryId)
    {
        var guard = Guard("Participant");
        if (guard != null) return guard!;

        HttpResponseMessage response;
        try
        {
            var client = CreateClient();
            response = await client.PostAsJsonAsync($"/api/events/{eventId}/enrolments",
                new { categoryId });
        }
        catch (HttpRequestException)
        {
            TempData["Error"] = "The RaceDay API is not reachable. Start RaceDayApi first.";
            return RedirectToAction("Details", new { id = eventId });
        }

        if (response.IsSuccessStatusCode)
        {
            TempData["Success"] = "You are in! Your entry is pending confirmation by the organiser.";
            return RedirectToAction("Index", "MyEvents");
        }

        TempData["Error"] = response.StatusCode switch
        {
            HttpStatusCode.Conflict => "You are already enrolled for this event.",
            HttpStatusCode.BadRequest => "That entry could not be accepted - the category or event may be closed.",
            HttpStatusCode.Unauthorized => "Please log in again.",
            _ => "Something went wrong while entering the event. Please try again."
        };
        return RedirectToAction("Details", new { id = eventId });
    }

    // ---------------- MY EVENTS (organiser) ----------------

    [HttpGet]
    public async Task<IActionResult> MyEvents()
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
        return View(events);
    }

    // ---------------- CREATE (organiser) ----------------

    [HttpGet]
    public IActionResult Create()
    {
        var guard = Guard("Organiser");
        if (guard != null) return guard!;
        return View(new SaveEventViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(SaveEventViewModel model)
    {
        var guard = Guard("Organiser");
        if (guard != null) return guard!;
        if (!ModelState.IsValid) return View(model);

        string? bannerUrl = null;
        if (model.BannerImage is { Length: > 0 })
        {
            bannerUrl = await UploadImageAsync(model.BannerImage);
            if (bannerUrl == null)
            {
                ModelState.AddModelError("", "The banner upload failed - is Azure Blob Storage configured on the API?");
                return View(model);
            }
        }

        HttpResponseMessage response;
        try
        {
            var client = CreateClient();
            response = await client.PostAsJsonAsync("/api/events", new
            {
                eventName = model.EventName,
                description = model.Description,
                eventDate = model.EventDate.ToString("yyyy-MM-dd"),
                location = model.Location,
                distanceKm = model.DistanceKm,
                eventTypeId = model.EventTypeId,
                bannerImageUrl = bannerUrl
            });
        }
        catch (HttpRequestException)
        {
            ModelState.AddModelError("", "The RaceDay API is not reachable. Start RaceDayApi first.");
            return View(model);
        }

        if (response.IsSuccessStatusCode)
        {
            TempData["Success"] = $"Event '{model.EventName}' created.";
            return RedirectToAction("MyEvents");
        }

        ModelState.AddModelError("", "The API rejected the event - check that all fields are valid.");
        return View(model);
    }

    // ---------------- EDIT (organiser) ----------------

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var guard = Guard("Organiser");
        if (guard != null) return guard!;

        EventDetailViewModel? evt;
        try
        {
            var client = CreateClient();
            evt = await client.GetFromJsonAsync<EventDetailViewModel>($"/api/events/{id}");
        }
        catch (HttpRequestException)
        {
            ViewBag.ApiError = "The RaceDay API is not reachable. Start RaceDayApi first.";
            return View("Error");
        }

        if (evt == null) return NotFound();
        if (evt.OrganiserId != UserId) return View("AccessDenied");

        return View(new SaveEventViewModel
        {
            EventId = evt.EventId,
            EventName = evt.EventName,
            Description = evt.Description,
            EventDate = evt.EventDate,
            Location = evt.Location,
            DistanceKm = evt.DistanceKm,
            EventTypeId = evt.EventTypeId,
            BannerImageUrl = evt.BannerImageUrl
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(SaveEventViewModel model)
    {
        var guard = Guard("Organiser");
        if (guard != null) return guard!;
        if (!ModelState.IsValid) return View(model);

        // Keep the existing banner unless a new one is uploaded.
        var bannerUrl = model.BannerImageUrl;
        if (model.BannerImage is { Length: > 0 })
        {
            bannerUrl = await UploadImageAsync(model.BannerImage);
            if (bannerUrl == null)
            {
                ModelState.AddModelError("", "The banner upload failed - is Azure Blob Storage configured on the API?");
                return View(model);
            }
        }

        HttpResponseMessage response;
        try
        {
            var client = CreateClient();
            response = await client.PutAsJsonAsync($"/api/events/{model.EventId}", new
            {
                eventName = model.EventName,
                description = model.Description,
                eventDate = model.EventDate.ToString("yyyy-MM-dd"),
                location = model.Location,
                distanceKm = model.DistanceKm,
                eventTypeId = model.EventTypeId,
                bannerImageUrl = bannerUrl
            });
        }
        catch (HttpRequestException)
        {
            ModelState.AddModelError("", "The RaceDay API is not reachable. Start RaceDayApi first.");
            return View(model);
        }

        if (response.IsSuccessStatusCode)
        {
            TempData["Success"] = $"Event '{model.EventName}' updated.";
            return RedirectToAction("MyEvents");
        }

        ModelState.AddModelError("", "The API rejected the change - check that all fields are valid.");
        return View(model);
    }

    // ---------------- DELETE (organiser, soft delete) ----------------

    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        var guard = Guard("Organiser");
        if (guard != null) return guard!;

        EventDetailViewModel? evt;
        try
        {
            var client = CreateClient();
            evt = await client.GetFromJsonAsync<EventDetailViewModel>($"/api/events/{id}");
        }
        catch (HttpRequestException)
        {
            ViewBag.ApiError = "The RaceDay API is not reachable. Start RaceDayApi first.";
            return View("Error");
        }

        if (evt == null) return NotFound();
        if (evt.OrganiserId != UserId) return View("AccessDenied");
        return View(evt);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var guard = Guard("Organiser");
        if (guard != null) return guard!;

        try
        {
            var client = CreateClient();
            var response = await client.DeleteAsync($"/api/events/{id}");
            TempData[response.IsSuccessStatusCode ? "Success" : "Error"] =
                response.IsSuccessStatusCode ? "Event deleted." : "The API refused to delete the event.";
        }
        catch (HttpRequestException)
        {
            TempData["Error"] = "The RaceDay API is not reachable. Start RaceDayApi first.";
        }
        return RedirectToAction("MyEvents");
    }

    // ---------------- banner upload helper ----------------

    /// <summary>Sends an image to /api/files (Azure Blob) and returns the stored URL, or null on failure.</summary>
    private async Task<string?> UploadImageAsync(IFormFile file)
    {
        try
        {
            var client = CreateClient();
            using var content = new MultipartFormDataContent();
            using var stream = new MemoryStream();
            await file.CopyToAsync(stream);
            stream.Position = 0;

            var fileContent = new StreamContent(stream);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(
                string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType);
            content.Add(fileContent, "file", file.FileName);

            var response = await client.PostAsync("/api/files", content);
            if (!response.IsSuccessStatusCode) return null;

            var blob = await response.Content.ReadFromJsonAsync<BlobUploadResult>();
            return blob?.Url;
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }
}
