using ObserverMagazine.Web.Services;

namespace ObserverMagazine.Web.Tests.Services;

/// <summary>
/// A no-op implementation of IAnalyticsService for use in unit tests.
/// Does nothing — never calls the network.
/// </summary>
public sealed class NoOpAnalyticsService : IAnalyticsService
{
    public bool IsBackendAvailable => false;

    public Task CheckHealthAsync() => Task.CompletedTask;
    public Task TrackPageViewAsync(string pageName, string? detail = null) => Task.CompletedTask;
    public Task TrackInteractionAsync(string action, string? detail = null) => Task.CompletedTask;
}
