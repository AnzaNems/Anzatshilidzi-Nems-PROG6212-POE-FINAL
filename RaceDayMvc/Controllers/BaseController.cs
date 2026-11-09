using System.Net.Http.Headers;
using Microsoft.AspNetCore.Mvc;

namespace RaceDayMvc.Controllers;

/// <summary>
/// Shared plumbing for every controller: session access and a HttpClient
/// with the Bearer token already attached. All API calls go through
/// CreateClient() so a logged-in user's requests are always authenticated.
/// </summary>
public abstract class BaseController : Controller
{
    protected readonly IHttpClientFactory HttpClientFactory;

    protected BaseController(IHttpClientFactory httpClientFactory)
        => HttpClientFactory = httpClientFactory;

    // ---------- session shortcuts ----------
    protected bool IsLoggedIn => HttpContext.Session.GetString("Token") != null;

    protected string? Role => HttpContext.Session.GetString("Role");

    protected string? FullName => HttpContext.Session.GetString("FullName");

    protected int UserId => int.TryParse(HttpContext.Session.GetString("UserId"), out var id) ? id : 0;

    protected string? Token => HttpContext.Session.GetString("Token");

    // ---------- helpers ----------

    /// <summary>An HttpClient pointing at the API, with the user's JWT attached (when logged in).</summary>
    protected HttpClient CreateClient()
    {
        var client = HttpClientFactory.CreateClient("Api");
        var token = Token;
        if (!string.IsNullOrEmpty(token))
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    /// <summary>
    /// Page guard: returns a redirect when the visitor is not allowed to see
    /// the page (anonymous -> login; wrong role -> AccessDenied), or null when
    /// the action may continue.
    /// Usage:  var guard = Guard("Participant"); if (guard != null) return guard;
    /// </summary>
    protected IActionResult? Guard(string role)
    {
        if (!IsLoggedIn) return RedirectToAction("Login", "Account");
        if (Role != role) return View("AccessDenied");   // rendered from Views/Account/AccessDenied
        return null;
    }
}
