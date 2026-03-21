using System.Net.Http.Json;
using Microsoft.Extensions.Logging;

namespace ObserverMagazine.Web.Services;

public sealed class AnalyticsService : IAnalyticsService
{
    private const string BackendBaseUrl = "https://my-api.2w7sp317.workers.dev";
    private readonly HttpClient http;
    private readonly ILogger<AnalyticsService> logger;
    private bool backendAvailable;
    private DateTime lastHealthCheck = DateTime.MinValue;
    private static readonly TimeSpan HealthCheckInterval = TimeSpan.FromMinutes(5);

    public AnalyticsService(HttpClient http, ILogger<AnalyticsService> logger)
    {
        this.http = http;
        this.logger = logger;
    }

    public bool IsBackendAvailable => backendAvailable;

    public async Task CheckHealthAsync()
    {
        if (DateTime.UtcNow - lastHealthCheck < HealthCheckInterval)
            return;

        try
        {
            var response = await http.GetAsync($"{BackendBaseUrl}/api/health");
            backendAvailable = response.IsSuccessStatusCode;
            lastHealthCheck = DateTime.UtcNow;
            logger.LogInformation("Backend health check: {Status}", backendAvailable ? "available" : "unavailable");
        }
        catch (Exception ex)
        {
            backendAvailable = false;
            lastHealthCheck = DateTime.UtcNow;
            logger.LogDebug(ex, "Backend health check failed — running in offline mode");
        }
    }

    public async Task TrackPageViewAsync(string pageName, string? detail = null)
    {
        await EnsureHealthChecked();
        if (!backendAvailable) return;

        var content = detail is not null
            ? $"[PageView] {pageName} — {detail}"
            : $"[PageView] {pageName}";

        await SendEventAsync($"PageView: {pageName}", content);
    }

    public async Task TrackInteractionAsync(string action, string? detail = null)
    {
        await EnsureHealthChecked();
        if (!backendAvailable) return;

        var content = detail is not null
            ? $"[Interaction] {action} — {detail}"
            : $"[Interaction] {action}";

        await SendEventAsync($"Interaction: {action}", content);
    }

    private async Task EnsureHealthChecked()
    {
        if (lastHealthCheck == DateTime.MinValue)
            await CheckHealthAsync();
    }

    private async Task SendEventAsync(string title, string content)
    {
        try
        {
            var payload = new { title, content };
            var response = await http.PostAsJsonAsync($"{BackendBaseUrl}/api/notes", payload);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogDebug("Analytics POST returned {StatusCode}", response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            // Never crash the app due to analytics failure
            logger.LogDebug(ex, "Failed to send analytics event: {Title}", title);
        }
    }
}
