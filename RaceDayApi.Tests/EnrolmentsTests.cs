using System.Net;
using Xunit;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using RaceDayApi.Models.DTOs;

namespace RaceDayApi.Tests;

/// <summary>
/// Event enrolments - the fourth required unit-testing area: Participants can
/// enrol, the enrolment is recorded correctly, duplicates are refused and
/// other roles are rejected.
/// </summary>
public class EnrolmentsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public EnrolmentsTests(CustomWebApplicationFactory factory) => _factory = factory;

    /// <summary>Registers a brand-new participant and returns a logged-in client.</summary>
    private async Task<(HttpClient client, string email)> NewParticipantAsync()
    {
        var email = $"enrol{Guid.NewGuid():N}@raceday.co.za";
        var client = _factory.CreateClient();
        await client.PostAsJsonAsync("/api/auth/register", new
        {
            fullName = "Enrolment Tester",
            email,
            password = "Pass@123",
            role = "Participant"
        });
        var login = await client.PostAsJsonAsync("/api/auth/login", new { email, password = "Pass@123" });
        var result = await login.Content.ReadFromJsonAsync<LoginResponse>();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", result!.Token);
        return (client, email);
    }

    /// <summary>Picks the first upcoming seeded event and one of its categories.</summary>
    private static async Task<(EventDto evt, CategoryDto cat)> FirstUpcomingEventWithCategoryAsync(HttpClient client)
    {
        var events = await client.GetFromJsonAsync<List<EventDto>>("/api/events");
        var evt = events!.First(e => e.CategoryCount > 0);
        var detail = await client.GetFromJsonAsync<EventDetailDto>($"/api/events/{evt.EventId}");
        return (evt, detail!.Categories.First());
    }

    [Fact]
    public async Task Enrol_AsParticipant_Returns201AndIsRecorded()
    {
        var (client, _) = await NewParticipantAsync();
        var (evt, cat) = await FirstUpcomingEventWithCategoryAsync(client);

        var response = await client.PostAsJsonAsync($"/api/events/{evt.EventId}/enrolments",
            new { categoryId = cat.CategoryId });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var enrolment = await response.Content.ReadFromJsonAsync<EnrolmentDto>();
        Assert.Equal("Pending", enrolment!.Status);              // enrolments start amber
        Assert.Equal(cat.CategoryName, enrolment.CategoryName);

        // The enrolment must show up in the participant's own list.
        var mine = await client.GetFromJsonAsync<List<EnrolmentDto>>("/api/enrolments/me");
        Assert.Contains(mine!, x => x.EventId == evt.EventId && x.CategoryId == cat.CategoryId);
    }

    [Fact]
    public async Task Enrol_SecondTimeForSameEvent_Returns409()
    {
        var (client, _) = await NewParticipantAsync();
        var (evt, cat) = await FirstUpcomingEventWithCategoryAsync(client);

        await client.PostAsJsonAsync($"/api/events/{evt.EventId}/enrolments",
            new { categoryId = cat.CategoryId });
        var second = await client.PostAsJsonAsync($"/api/events/{evt.EventId}/enrolments",
            new { categoryId = cat.CategoryId });

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Enrol_AsOrganiser_Returns403()
    {
        var login = await _factory.CreateClient().PostAsJsonAsync("/api/auth/login",
            new { email = "thabo@raceday.co.za", password = "Organ@123" });
        var result = await login.Content.ReadFromJsonAsync<LoginResponse>();
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", result!.Token);

        var (evt, cat) = await FirstUpcomingEventWithCategoryAsync(client);

        var response = await client.PostAsJsonAsync($"/api/events/{evt.EventId}/enrolments",
            new { categoryId = cat.CategoryId });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Enrol_Anonymous_Returns401()
    {
        var client = _factory.CreateClient();
        var (evt, cat) = await FirstUpcomingEventWithCategoryAsync(client);

        var response = await client.PostAsJsonAsync($"/api/events/{evt.EventId}/enrolments",
            new { categoryId = cat.CategoryId });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task MyEnrolments_ShowsOnlyTheLoggedInParticipant()
    {
        var (clientA, _) = await NewParticipantAsync();
        var (clientB, _) = await NewParticipantAsync();

        var (evt, cat) = await FirstUpcomingEventWithCategoryAsync(clientA);
        await clientA.PostAsJsonAsync($"/api/events/{evt.EventId}/enrolments",
            new { categoryId = cat.CategoryId });

        var mineA = await clientA.GetFromJsonAsync<List<EnrolmentDto>>("/api/enrolments/me");
        var mineB = await clientB.GetFromJsonAsync<List<EnrolmentDto>>("/api/enrolments/me");

        Assert.NotEmpty(mineA!);
        Assert.Empty(mineB!);                       // B never enrolled - the /me pattern works
    }

    [Fact]
    public async Task EventEnrolments_AsOwningOrganiser_ReturnsList()
    {
        var (participant, _) = await NewParticipantAsync();
        var (evt, cat) = await FirstUpcomingEventWithCategoryAsync(participant);
        await participant.PostAsJsonAsync($"/api/events/{evt.EventId}/enrolments",
            new { categoryId = cat.CategoryId });

        // The seeded organiser who owns the first upcoming event (Soweto Marathon) is Thabo.
        var login = await _factory.CreateClient().PostAsJsonAsync("/api/auth/login",
            new { email = "thabo@raceday.co.za", password = "Organ@123" });
        var result = await login.Content.ReadFromJsonAsync<LoginResponse>();
        var organiser = _factory.CreateClient();
        organiser.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", result!.Token);

        var response = await organiser.GetAsync($"/api/events/{evt.EventId}/enrolments");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var list = await response.Content.ReadFromJsonAsync<List<EnrolmentDto>>();
        Assert.Contains(list!, x => x.CategoryId == cat.CategoryId);
    }
}
