using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using RaceDayMvc.Models;

namespace RaceDayMvc.Controllers;

/// <summary>
/// Register, login and logout. The MVC app never validates passwords itself -
/// it forwards everything to the API and keeps only the returned JWT token,
/// user id, role and name in session.
/// </summary>
public class AccountController : BaseController
{
    public AccountController(IHttpClientFactory f) : base(f) { }

    // ---------------- LOGIN ----------------

    [HttpGet]
    public IActionResult Login()
    {
        if (IsLoggedIn) return RedirectToAction("Index", "Dashboard");
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        HttpClient client;
        try { client = CreateClient(); }
        catch { ModelState.AddModelError("", "Cannot reach the RaceDay API. Is RaceDayApi running?"); return View(model); }

        HttpResponseMessage response;
        try
        {
            response = await client.PostAsJsonAsync("/api/auth/login",
                new { email = model.Email, password = model.Password });
        }
        catch (HttpRequestException)
        {
            ModelState.AddModelError("", "Cannot reach the RaceDay API. Is RaceDayApi running?");
            return View(model);
        }

        if (response.IsSuccessStatusCode)
        {
            var login = await response.Content.ReadFromJsonAsync<LoginResponse>();
            if (login != null)
            {
                HttpContext.Session.SetString("Token", login.Token);
                HttpContext.Session.SetString("UserId", login.UserId.ToString());
                HttpContext.Session.SetString("Role", login.Role);
                HttpContext.Session.SetString("FullName", login.FullName);
                return RedirectToAction("Index", "Dashboard");
            }
        }

        ModelState.AddModelError("", "Invalid email or password.");
        return View(model);
    }

    // ---------------- REGISTER ----------------

    [HttpGet]
    public IActionResult Register()
    {
        if (IsLoggedIn) return RedirectToAction("Index", "Dashboard");
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        HttpResponseMessage response;
        try
        {
            var client = CreateClient();
            response = await client.PostAsJsonAsync("/api/auth/register", new
            {
                fullName = model.FullName,
                email = model.Email,
                password = model.Password,
                role = model.Role
            });
        }
        catch (HttpRequestException)
        {
            ModelState.AddModelError("", "Cannot reach the RaceDay API. Is RaceDayApi running?");
            return View(model);
        }

        if (response.IsSuccessStatusCode)
        {
            // Auto-login straight after a successful registration.
            var loginClient = CreateClient();
            var login = await loginClient.PostAsJsonAsync("/api/auth/login",
                new { email = model.Email, password = model.Password });

            if (login.IsSuccessStatusCode)
            {
                var result = await login.Content.ReadFromJsonAsync<LoginResponse>();
                HttpContext.Session.SetString("Token", result!.Token);
                HttpContext.Session.SetString("UserId", result.UserId.ToString());
                HttpContext.Session.SetString("Role", result.Role);
                HttpContext.Session.SetString("FullName", result.FullName);
                return RedirectToAction("Index", "Dashboard");
            }
        }

        ModelState.AddModelError("", "Registration failed - the email may already be in use, or the role was invalid.");
        return View(model);
    }

    // ---------------- LOGOUT ----------------

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return RedirectToAction("Index", "Home");
    }

    // ---------------- ACCESS DENIED ----------------

    [HttpGet]
    public IActionResult AccessDenied() => View();
}
