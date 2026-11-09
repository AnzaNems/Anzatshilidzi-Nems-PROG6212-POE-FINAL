using System.Net;
using Xunit;
using System.Net.Http.Json;
using RaceDayApi.Models.DTOs;

namespace RaceDayApi.Tests;

/// <summary>
/// User registration and login - the first required unit-testing area.
/// Covers success and failure for both roles.
/// </summary>
public class AuthTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public AuthTests(CustomWebApplicationFactory factory) => _factory = factory;

    private static string UniqueEmail() => $"test{Guid.NewGuid():N}@raceday.co.za";

    [Fact]
    public async Task Register_NewParticipant_Returns201AndUserWithoutPassword()
    {
        var client = _factory.CreateClient();
        var email = UniqueEmail();

        var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            fullName = "Test Participant",
            email,
            password = "Pass@123",
            role = "Participant"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var user = await response.Content.ReadFromJsonAsync<UserDto>();
        Assert.NotNull(user);
        Assert.Equal("Test Participant", user!.FullName);
        Assert.Equal(email, user.Email);
        Assert.Equal("Participant", user.Role);
    }

    [Fact]
    public async Task Register_NewOrganiser_Returns201WithOrganiserRole()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            fullName = "Test Organiser",
            email = UniqueEmail(),
            password = "Pass@123",
            role = "organiser"          // case-insensitive on purpose
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var user = await response.Content.ReadFromJsonAsync<UserDto>();
        Assert.Equal("Organiser", user!.Role);
    }

    [Fact]
    public async Task Register_DuplicateEmail_Returns400()
    {
        var client = _factory.CreateClient();
        var email = UniqueEmail();
        var body = new { fullName = "Dup User", email, password = "Pass@123", role = "Participant" };

        await client.PostAsJsonAsync("/api/auth/register", body);
        var second = await client.PostAsJsonAsync("/api/auth/register", body);

        Assert.Equal(HttpStatusCode.BadRequest, second.StatusCode);
    }

    [Fact]
    public async Task Register_InvalidRole_Returns400()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            fullName = "Sneaky Admin",
            email = UniqueEmail(),
            password = "Pass@123",
            role = "Admin"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_ValidCredentials_ReturnsTokenUserIdAndRole()
    {
        var client = _factory.CreateClient();
        var email = UniqueEmail();
        await client.PostAsJsonAsync("/api/auth/register", new
        {
            fullName = "Login User",
            email,
            password = "Pass@123",
            role = "Participant"
        });

        var response = await client.PostAsJsonAsync("/api/auth/login", new { email, password = "Pass@123" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var login = await response.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(login);
        Assert.False(string.IsNullOrEmpty(login!.Token));
        Assert.StartsWith("eyJ", login.Token);            // a real JWT starts with the header
        Assert.Equal("Participant", login.Role);
        Assert.True(login.UserId > 0);
    }

    [Fact]
    public async Task Login_WrongPassword_Returns401()
    {
        var client = _factory.CreateClient();
        var email = UniqueEmail();
        await client.PostAsJsonAsync("/api/auth/register", new
        {
            fullName = "Wrong Pass User",
            email,
            password = "Pass@123",
            role = "Participant"
        });

        var response = await client.PostAsJsonAsync("/api/auth/login", new { email, password = "Wrong@999" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_UnknownEmail_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login",
            new { email = "ghost@nowhere.co.za", password = "Pass@123" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
