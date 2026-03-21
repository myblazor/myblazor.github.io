namespace ObserverMagazine.Web.Services;

/// <summary>
/// Sends analytics events to the Cloudflare Workers backend.
/// Gracefully degrades if the backend is unavailable.
/// </summary>
public interface IAnalyticsService
{
    /// <summary>
    /// Tracks a page view. Called from each page's OnInitializedAsync.
    /// </summary>
    Task TrackPageViewAsync(string pageName, string? detail = null);

    /// <summary>
    /// Tracks a user interaction (click, filter, sort, selection, etc.)
    /// </summary>
    Task TrackInteractionAsync(string action, string? detail = null);

    /// <summary>
    /// Returns true if the backend is reachable (cached from the last health check).
    /// </summary>
    bool IsBackendAvailable { get; }

    /// <summary>
    /// Checks the backend health and caches the result.
    /// </summary>
    Task CheckHealthAsync();
}
