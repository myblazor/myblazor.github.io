using System.Net.Http.Json;
using Microsoft.Extensions.Logging;

namespace ObserverMagazine.Web.Services;

public sealed class AnalyticsService(HttpClient http, ILogger<AnalyticsService> logger) : IAnalyticsService
{
    private const string BackendBaseUrl = "https://my-api.2w7sp317.workers.dev";
    private static readonly TimeSpan HealthCacheDuration = TimeSpan.FromMinutes(5);

    private bool _backendAvailable;
    private DateTime _lastHealthCheck = DateTime.MinValue;

    public bool IsBackendAvailable => _backendAvailable;

    public async Task CheckHealthAsync()
    {
        // Cache health check result for 5 minutes
        if (DateTime.UtcNow - _lastHealthCheck < HealthCacheDuration)
            return;

        try
        {
            var response = await http.GetAsync($"{BackendBaseUrl}/api/health");
            _backendAvailable = response.IsSuccessStatusCode;
            logger.LogInformation("Backend health check: {Status}", _backendAvailable ? "available" : "unavailable");
        }
        catch (Exception ex)
        {
            _backendAvailable = false;
            logger.LogDebug(ex, "Backend health check failed — running without backend");
        }
        finally
        {
            _lastHealthCheck = DateTime.UtcNow;
        }
    }

    public async Task TrackPageViewAsync(string pageName, string? detail = null)
    {
        await EnsureHealthChecked();
        if (!_backendAvailable) return;

        var content = detail is not null
            ? $"[PageView] {pageName} — {detail}"
            : $"[PageView] {pageName}";

        await SendEventAsync($"PageView: {pageName}", content);
    }

    public async Task TrackInteractionAsync(string action, string? detail = null)
    {
        await EnsureHealthChecked();
        if (!_backendAvailable) return;

        var content = detail is not null
            ? $"[Interaction] {action} — {detail}"
            : $"[Interaction] {action}";

        await SendEventAsync($"Interaction: {action}", content);
    }

    public async Task IncrementViewAsync(string slug)
    {
        await EnsureHealthChecked();
        if (!_backendAvailable) return;

        try
        {
            await http.PostAsync($"{BackendBaseUrl}/api/views/{slug}", null);
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Failed to increment view for {Slug}", slug);
        }
    }

    public async Task<int?> GetViewCountAsync(string slug)
    {
        await EnsureHealthChecked();
        if (!_backendAvailable) return null;

        try
        {
            var result = await http.GetFromJsonAsync<ViewCountResponse>($"{BackendBaseUrl}/api/views/{slug}");
            return result?.Count;
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Failed to get view count for {Slug}", slug);
            return null;
        }
    }

    public async Task AddReactionAsync(string slug, string reactionType)
    {
        await EnsureHealthChecked();
        if (!_backendAvailable) return;

        try
        {
            var payload = new { type = reactionType };
            await http.PostAsJsonAsync($"{BackendBaseUrl}/api/reactions/{slug}", payload);
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Failed to add reaction for {Slug}", slug);
        }
    }

    public async Task<Dictionary<string, int>?> GetReactionsAsync(string slug)
    {
        await EnsureHealthChecked();
        if (!_backendAvailable) return null;

        try
        {
            return await http.GetFromJsonAsync<Dictionary<string, int>>($"{BackendBaseUrl}/api/reactions/{slug}");
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Failed to get reactions for {Slug}", slug);
            return null;
        }
    }

    private async Task EnsureHealthChecked()
    {
        if (_lastHealthCheck == DateTime.MinValue)
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
            logger.LogDebug(ex, "Failed to send analytics event: {Title}", title);
        }
    }

    private sealed record ViewCountResponse(int Count);
}
