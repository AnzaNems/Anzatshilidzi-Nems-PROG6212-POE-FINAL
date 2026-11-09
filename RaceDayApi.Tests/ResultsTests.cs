using System.Net;
using Xunit;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using RaceDayApi.Models.DTOs;

namespace RaceDayApi.Tests;

/// <summary>
/// Results - capture as Organiser, duplicate refusal, role rejection, and the
/// personal "My Results" view that must only ever show the participant's own races.
/// </summary>
public class ResultsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public ResultsTests(CustomWebApplicationFactory factory) => _factory = factory;

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

    /// <summary>Registers a participant, enrols them in Thabo's first upcoming event and returns the enrolment.</summary>
    private async Task<(HttpClient participant, EnrolmentDto enrolment)> EnrolledParticipantAsync()
    {
        var email = $"result{Guid.NewGuid():N}@raceday.co.za";
        var participant = _factory.CreateClient();
        await participant.PostAsJsonAsync("/api/auth/register", new
        {
            fullName = "Result Tester",
            email,
            password = "Pass@123",
            role = "Participant"
        });
        var login = await participant.PostAsJsonAsync("/api/auth/login", new { email, password = "Pass@123" });
        var auth = await login.Content.ReadFromJsonAsync<LoginResponse>();
        participant.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", auth!.Token);

        var events = await participant.GetFromJsonAsync<List<EventDto>>("/api/events");
        var evt = events!.First(e => e.CategoryCount > 0);
        var detail = await participant.GetFromJsonAsync<EventDetailDto>($"/api/events/{evt.EventId}");
        var cat = detail!.Categories.First();

        var enrol = await participant.PostAsJsonAsync($"/api/events/{evt.EventId}/enrolments",
            new { categoryId = cat.CategoryId });
        var enrolment = await enrol.Content.ReadFromJsonAsync<EnrolmentDto>();
        return (participant, enrolment!);
    }

    [Fact]
    public async Task CaptureResult_AsOwningOrganiser_Returns201()
    {
        var (_, enrolment) = await EnrolledParticipantAsync();
        var organiser = await LoginAsync("thabo@raceday.co.za", "Organ@123");

        var response = await organiser.PostAsJsonAsync($"/api/enrolments/{enrolment.EnrolmentId}/result", new
        {
            finishTime = "01:23:45",
            position = 5,
            isDisqualified = false
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<ResultDto>();
        Assert.Equal("01:23:45", result!.FinishTime);
        Assert.Equal(5, result.Position);
    }

    [Fact]
    public async Task CaptureResult_SecondTimeForSameEnrolment_Returns409()
    {
        var (_, enrolment) = await EnrolledParticipantAsync();
        var organiser = await LoginAsync("thabo@raceday.co.za", "Organ@123");

        await organiser.PostAsJsonAsync($"/api/enrolments/{enrolment.EnrolmentId}/result", new
        {
            finishTime = "01:00:00", position = 1, isDisqualified = false
        });
        var second = await organiser.PostAsJsonAsync($"/api/enrolments/{enrolment.EnrolmentId}/result", new
        {
            finishTime = "00:59:59", position = 1, isDisqualified = false
        });

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task CaptureResult_AsParticipant_Returns403()
    {
        var (participant, enrolment) = await EnrolledParticipantAsync();

        var response = await participant.PostAsJsonAsync($"/api/enrolments/{enrolment.EnrolmentId}/result", new
        {
            finishTime = "00:30:00", position = 1, isDisqualified = false
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CaptureResult_InvalidTimeFormat_Returns400()
    {
        var (_, enrolment) = await EnrolledParticipantAsync();
        var organiser = await LoginAsync("thabo@raceday.co.za", "Organ@123");

        var response = await organiser.PostAsJsonAsync($"/api/enrolments/{enrolment.EnrolmentId}/result", new
        {
            finishTime = "not-a-time",
            position = 1,
            isDisqualified = false
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task MyResults_ShowsOwnResultsWithEventDetails()
    {
        var (participant, enrolment) = await EnrolledParticipantAsync();
        var organiser = await LoginAsync("thabo@raceday.co.za", "Organ@123");
        await organiser.PostAsJsonAsync($"/api/enrolments/{enrolment.EnrolmentId}/result", new
        {
            finishTime = "02:15:30",
            position = 12,
            isDisqualified = false
        });

        var response = await participant.GetAsync("/api/results/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var results = await response.Content.ReadFromJsonAsync<List<ResultDto>>();

        // Seeded participant? No - this user only has the one result we just captured.
        var own = results!.Single(r => r.EnrolmentId == enrolment.EnrolmentId);
        Assert.Equal("02:15:30", own.FinishTime);
        Assert.False(string.IsNullOrEmpty(own.EventName));     // event details included
        Assert.True(own.TotalFinishers >= 1);
    }

    [Fact]
    public async Task MyResults_AsOrganiser_Returns403()
    {
        var organiser = await LoginAsync("thabo@raceday.co.za", "Organ@123");

        var response = await organiser.GetAsync("/api/results/me");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
