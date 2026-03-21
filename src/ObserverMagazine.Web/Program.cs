using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using ObserverMagazine.Web;
using ObserverMagazine.Web.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Configure HttpClient for fetching static data
builder.Services.AddScoped(sp =>
    new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// Register application services
builder.Services.AddScoped<IBlogService, BlogService>();
builder.Services.AddSingleton<TelemetryService>();
builder.Services.AddScoped<IAnalyticsService, AnalyticsService>();

// Configure logging
builder.Logging.SetMinimumLevel(LogLevel.Information);

var host = builder.Build();

// Log application startup
var logger = host.Services.GetRequiredService<ILoggerFactory>().CreateLogger("App");
logger.LogInformation("Observer Magazine started at {Time}", DateTime.UtcNow);

var telemetry = host.Services.GetRequiredService<TelemetryService>();
telemetry.TrackEvent("AppStarted");

await host.RunAsync();
