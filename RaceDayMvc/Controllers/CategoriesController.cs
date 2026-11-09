using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using RaceDayMvc.Models;

namespace RaceDayMvc.Controllers;

/// <summary>
/// Organiser's category management for one event: list, add, edit, delete.
/// </summary>
public class CategoriesController : BaseController
{
    public CategoriesController(IHttpClientFactory f) : base(f) { }

    [HttpGet]
    public async Task<IActionResult> Index(int eventId)
    {
        var guard = Guard("Organiser");
        if (guard != null) return guard!;

        EventDetailViewModel? evt = null;
        List<CategoryViewModel> categories = new();
        try
        {
            var client = CreateClient();
            evt = await client.GetFromJsonAsync<EventDetailViewModel>($"/api/events/{eventId}");
            categories = await client.GetFromJsonAsync<List<CategoryViewModel>>($"/api/events/{eventId}/categories") ?? new();
        }
        catch (HttpRequestException)
        {
            ViewBag.ApiError = "The RaceDay API is not reachable. Start RaceDayApi first.";
            return View("Error");
        }

        if (evt == null) return NotFound();
        if (evt.OrganiserId != UserId) return View("AccessDenied");

        ViewBag.Event = evt;
        return View(categories);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(SaveCategoryViewModel model)
    {
        var guard = Guard("Organiser");
        if (guard != null) return guard!;
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Please fill in a category name, distance and entry fee.";
            return RedirectToAction("Index", new { eventId = model.EventId });
        }

        try
        {
            var client = CreateClient();
            var response = await client.PostAsJsonAsync($"/api/events/{model.EventId}/categories", new
            {
                categoryName = model.CategoryName,
                distanceKm = model.DistanceKm,
                entryFee = model.EntryFee
            });

            TempData[response.IsSuccessStatusCode ? "Success" : "Error"] =
                response.IsSuccessStatusCode
                    ? $"Category '{model.CategoryName}' added."
                    : "The API rejected the category.";
        }
        catch (HttpRequestException)
        {
            TempData["Error"] = "The RaceDay API is not reachable. Start RaceDayApi first.";
        }
        return RedirectToAction("Index", new { eventId = model.EventId });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var guard = Guard("Organiser");
        if (guard != null) return guard!;

        CategoryViewModel? cat = null;
        try
        {
            var client = CreateClient();
            var events = await client.GetFromJsonAsync<List<EventViewModel>>("/api/events/mine") ?? new();
            foreach (var evt in events)
            {
                var cats = await client.GetFromJsonAsync<List<CategoryViewModel>>($"/api/events/{evt.EventId}/categories") ?? new();
                cat = cats.FirstOrDefault(c => c.CategoryId == id);
                if (cat != null) break;
            }
        }
        catch (HttpRequestException)
        {
            ViewBag.ApiError = "The RaceDay API is not reachable. Start RaceDayApi first.";
            return View("Error");
        }

        if (cat == null) return NotFound();

        return View(new SaveCategoryViewModel
        {
            CategoryId = cat.CategoryId,
            EventId = cat.EventId,
            CategoryName = cat.CategoryName,
            DistanceKm = cat.DistanceKm,
            EntryFee = cat.EntryFee
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(SaveCategoryViewModel model)
    {
        var guard = Guard("Organiser");
        if (guard != null) return guard!;
        if (!ModelState.IsValid) return View(model);

        try
        {
            var client = CreateClient();
            var response = await client.PutAsJsonAsync($"/api/categories/{model.CategoryId}", new
            {
                categoryName = model.CategoryName,
                distanceKm = model.DistanceKm,
                entryFee = model.EntryFee
            });

            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] = $"Category '{model.CategoryName}' updated.";
                return RedirectToAction("Index", new { eventId = model.EventId });
            }
            ModelState.AddModelError("", "The API rejected the change.");
        }
        catch (HttpRequestException)
        {
            ModelState.AddModelError("", "The RaceDay API is not reachable. Start RaceDayApi first.");
        }
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, int eventId)
    {
        var guard = Guard("Organiser");
        if (guard != null) return guard!;

        try
        {
            var client = CreateClient();
            var response = await client.DeleteAsync($"/api/categories/{id}");

            TempData[response.IsSuccessStatusCode ? "Success" : "Error"] = response.StatusCode switch
            {
                HttpStatusCode.OK or HttpStatusCode.NoContent => "Category deleted.",
                HttpStatusCode.Conflict => "This category has active enrolments and cannot be deleted.",
                _ => "The API refused to delete the category."
            };
        }
        catch (HttpRequestException)
        {
            TempData["Error"] = "The RaceDay API is not reachable. Start RaceDayApi first.";
        }
        return RedirectToAction("Index", new { eventId });
    }
}
