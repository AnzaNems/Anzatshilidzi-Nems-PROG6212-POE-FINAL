using System.Net;
using Xunit;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using RaceDayApi.Models.DTOs;

namespace RaceDayApi.Tests;

/// <summary>
/// Event management - the second required unit-testing area: Organisers can
/// manage events, other roles are rejected, and unauthenticated calls bounce.
/// </summary>
public class EventsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public EventsTests(CustomWebApplicationFactory factory) => _factory = factory;

    /// <summary>Logs in with a seeded account and returns a client with the Bearer token attached.</summary>
    private async Task<HttpClient> LoginAsync(string email, string password)
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/login", new { email, password });
        response.EnsureSuccessStatusCode();
        var login = await response.Content.ReadFromJsonAsync<LoginResponse>();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", login!.Token);
        return client;
    }

    private static object NewEvent(string name) => new
    {
        eventName = name,
        description = "A test event created by the unit test suite.",
        eventDate = "2027-06-01",
        location = "Testville, Gauteng",
        distanceKm = 21.1,
        eventTypeId = 1,
        bannerImageUrl = (string?)null
    };

    [Fact]
    public async Task GetEvents_Anonymous_Returns200WithSeededEvents()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/events");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var events = await response.Content.ReadFromJsonAsync<List<EventDto>>();
        Assert.NotNull(events);
        Assert.NotEmpty(events!);                          // seeder ran at startup
    }

    [Fact]
    public async Task CreateEvent_AsOrganiser_Returns201()
    {
        var client = await LoginAsync("thabo@raceday.co.za", "Organ@123");

        var response = await client.PostAsJsonAsync("/api/events", NewEvent("Unit Test Marathon"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<EventDto>();
        Assert.Equal("Unit Test Marathon", created!.EventName);
        Assert.Equal("Thabo Mokoena", created.OrganiserName);   // organiser taken from the token
    }

    [Fact]
    public async Task CreateEvent_AsParticipant_Returns403()
    {
        var client = await LoginAsync("sibusiso@raceday.co.za", "Part@123");

        var response = await client.PostAsJsonAsync("/api/events", NewEvent("Forbidden Marathon"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateEvent_Anonymous_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/events", NewEvent("Anonymous Marathon"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task UpdateEvent_ByOwningOrganiser_Returns200()
    {
        var client = await LoginAsync("thabo@raceday.co.za", "Organ@123");

        var create = await client.PostAsJsonAsync("/api/events", NewEvent("Editable Marathon"));
        var created = await create.Content.ReadFromJsonAsync<EventDto>();

        var response = await client.PutAsJsonAsync($"/api/events/{created!.EventId}", new
        {
            eventName = "Edited Marathon",
            description = "Updated description.",
            eventDate = "2027-07-01",
            location = "Pretoria",
            distanceKm = 10.0,
            eventTypeId = 2,
            bannerImageUrl = (string?)null
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<EventDto>();
        Assert.Equal("Edited Marathon", updated!.EventName);
    }

    [Fact]
    public async Task UpdateEvent_ByOtherOrganiser_Returns403()
    {
        // Thabo creates an event; Lerato (a different Organiser) must not be able to edit it.
        var thabo = await LoginAsync("thabo@raceday.co.za", "Organ@123");
        var create = await thabo.PostAsJsonAsync("/api/events", NewEvent("Thabo's Private Event"));
        var created = await create.Content.ReadFromJsonAsync<EventDto>();

        var lerato = await LoginAsync("lerato@raceday.co.za", "Organ@123");
        var response = await lerato.PutAsJsonAsync($"/api/events/{created!.EventId}", new
        {
            eventName = "Hijacked Event",
            description = "Should not be allowed.",
            eventDate = "2027-07-01",
            location = "Durban",
            distanceKm = 5.0,
            eventTypeId = 2,
            bannerImageUrl = (string?)null
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task DeleteEvent_SoftDeletes_EventDisappearsFromListing()
    {
        var client = await LoginAsync("thabo@raceday.co.za", "Organ@123");
        var create = await client.PostAsJsonAsync("/api/events", NewEvent("Doomed Marathon"));
        var created = await create.Content.ReadFromJsonAsync<EventDto>();

        var delete = await client.DeleteAsync($"/api/events/{created!.EventId}");
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);

        var get = await client.GetAsync($"/api/events/{created.EventId}");
        Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
    }
}
