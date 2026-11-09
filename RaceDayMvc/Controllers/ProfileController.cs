using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using RaceDayMvc.Models;

namespace RaceDayMvc.Controllers;

/// <summary>
/// Profile page for both roles: view personal info, update name / email and
/// upload a profile picture (stored in Azure Blob via the API).
/// </summary>
public class ProfileController : BaseController
{
    public ProfileController(IHttpClientFactory f) : base(f) { }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        if (!IsLoggedIn) return RedirectToAction("Login", "Account");

        UserProfileDto? profile = null;
        try
        {
            var client = CreateClient();
            profile = await client.GetFromJsonAsync<UserProfileDto>("/api/profile");
        }
        catch (HttpRequestException)
        {
            ViewBag.ApiError = "The RaceDay API is not reachable. Start RaceDayApi first.";
        }

        if (profile == null)
        {
            // The token expired - ask the user to log in again.
            HttpContext.Session.Clear();
            TempData["Error"] = "Your session expired. Please log in again.";
            return RedirectToAction("Login", "Account");
        }

        return View(new ProfileViewModel
        {
            UserId = profile.UserId,
            FullName = profile.FullName,
            Email = profile.Email,
            Role = profile.Role,
            ProfilePictureUrl = profile.ProfilePictureUrl,
            CreatedAt = profile.CreatedAt
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(ProfileViewModel model)
    {
        if (!IsLoggedIn) return RedirectToAction("Login", "Account");
        if (!ModelState.IsValid) return View(model);

        string? pictureUrl = model.ProfilePictureUrl;
        if (model.Picture is { Length: > 0 })
        {
            pictureUrl = await UploadPictureAsync(model.Picture);
            if (pictureUrl == null)
            {
                ModelState.AddModelError("", "The picture upload failed - is Azure Blob Storage configured on the API?");
                return View(model);
            }
        }

        HttpResponseMessage response;
        try
        {
            var client = CreateClient();
            response = await client.PutAsJsonAsync("/api/profile", new
            {
                fullName = model.FullName,
                email = model.Email,
                profilePictureUrl = pictureUrl
            });
        }
        catch (HttpRequestException)
        {
            ModelState.AddModelError("", "The RaceDay API is not reachable. Start RaceDayApi first.");
            return View(model);
        }

        if (response.IsSuccessStatusCode)
        {
            // Keep the navbar name in sync with the updated profile.
            HttpContext.Session.SetString("FullName", model.FullName);
            TempData["Success"] = "Profile updated.";
            return RedirectToAction("Index");
        }

        ModelState.AddModelError("", response.StatusCode == HttpStatusCode.BadRequest
            ? "The email may already be used by another account."
            : "The API rejected the change.");
        return View(model);
    }

    /// <summary>Uploads the picture to /api/files (Azure Blob) and returns the stored URL.</summary>
    private async Task<string?> UploadPictureAsync(IFormFile file)
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
