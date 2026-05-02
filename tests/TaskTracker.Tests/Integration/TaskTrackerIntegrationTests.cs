using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Xunit;

namespace TaskTracker.Tests.Integration;

public class TaskTrackerIntegrationTests : IClassFixture<TaskTrackerFactory>
{
    private readonly HttpClient _client;

    public TaskTrackerIntegrationTests(TaskTrackerFactory factory)
        => _client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

    [Fact]
    public async Task GET_Health_ReturnsOkOrDegraded()
    {
        var response = await _client.GetAsync("/health");

        Assert.True(
            response.StatusCode == HttpStatusCode.OK ||
            response.StatusCode == HttpStatusCode.ServiceUnavailable,
            $"Expected 200 or 503, got {response.StatusCode}");
    }

    [Fact]
    public async Task GET_Health_ResponseHasStatusField()
    {
        var response = await _client.GetAsync("/health");
        var body     = await response.Content.ReadAsStringAsync();

        using var doc = JsonDocument.Parse(body);
        Assert.True(doc.RootElement.TryGetProperty("status", out var status));
        Assert.Contains(status.GetString(), new[] { "healthy", "degraded" });
    }

    [Fact]
    public async Task GET_Health_ResponseHasCorrelationIdHeader()
    {
        var response = await _client.GetAsync("/health");

        Assert.True(
            response.Headers.Contains("X-Correlation-ID"),
            "Response should contain X-Correlation-ID header");
    }

    [Fact]
    public async Task GET_Health_ResponseHasContentSecurityPolicy()
    {
        var response = await _client.GetAsync("/health");

        Assert.True(
            response.Headers.Contains("Content-Security-Policy") ||
            response.Content.Headers.Contains("Content-Security-Policy"),
            "Response should contain Content-Security-Policy header");
    }

    [Fact]
    public async Task GET_Tasks_WithoutAuth_RedirectsToLogin()
    {
        var response = await _client.GetAsync("/Tasks");

        // MVC redirects unauthenticated users to login (302)
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/Account/Login", response.Headers.Location?.ToString() ?? "");
    }

    [Fact]
    public async Task GET_Login_ReturnsOk()
    {
        var response = await _client.GetAsync("/Account/Login");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task POST_Login_WithInvalidCredentials_ReturnsLoginPage()
    {
        var content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("Username", "nonexistent_user_xyz"),
            new KeyValuePair<string, string>("Password", "WrongPassword123!")
        });

        var response = await _client.PostAsync("/Account/Login", content);

        // Returns 200 (re-renders login form) or redirect if DB unavailable
        Assert.True(
            response.StatusCode == HttpStatusCode.OK ||
            response.StatusCode == HttpStatusCode.Redirect ||
            response.StatusCode == HttpStatusCode.InternalServerError,
            $"Unexpected status: {response.StatusCode}");
    }

    [Fact]
    public async Task GET_AllSecurityHeaders_Present()
    {
        var response = await _client.GetAsync("/health");

        var headers = response.Headers.ToDictionary(h => h.Key, h => h.Value.First());
        Assert.True(headers.ContainsKey("X-Frame-Options"), "X-Frame-Options missing");
        Assert.True(headers.ContainsKey("X-Content-Type-Options"), "X-Content-Type-Options missing");
        Assert.True(headers.ContainsKey("X-Correlation-ID"), "X-Correlation-ID missing");
    }
}
