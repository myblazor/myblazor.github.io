---
title: "HttpClientFactory and Typed Clients: The Complete Guide to HTTP Connection Management in .NET"
date: 2026-04-20
author: observer-team
summary: "Socket exhaustion is to HttpClient what connection pool exhaustion is to SQL Server — a silent killer that only reveals itself under load. This exhaustive guide covers the full lifecycle of HttpMessageHandler, DNS staleness, the IHttpClientFactory handler pool, and every pattern from Basic to Typed Clients, from .NET Framework 4.8 to .NET 10."
tags:
  - aspnet
  - dotnet
  - csharp
  - deep-dive
  - best-practices
  - architecture
  - guide
---

# HttpClientFactory and Typed Clients: The Complete Guide to HTTP Connection Management in .NET

There is a bug that has quietly ruined the evenings of tens of thousands of .NET developers around the world. It doesn't announce itself at compile time. It doesn't show up in unit tests. It lives quietly in production, growing in silence, until the day your application starts throwing cryptic socket errors at three in the morning, your error rates spike on the dashboard, your on-call phone rings, and you spend four hours staring at logs before a senior engineer says, "Are you `new`-ing up `HttpClient` on every request?"

Yes. You are. And that's the problem.

This article is a complete, exhaustive guide to `HttpClient` in .NET — every mistake, every solution, every configuration option, every pattern — from the first `using var client = new HttpClient()` a fresh developer ever writes to the production-grade typed client patterns used in microservices on .NET 10. It does not matter whether you have spent twenty years in ASP.NET Framework or just installed the .NET SDK for the first time this week. This guide will meet you where you are and walk you through everything.

---

## Part 1: The Web Is Just Plumbing — And Plumbing Can Break

### 1.1 What Is HTTP and Why Does Your Application Need to Speak It?

Let's start from absolute zero.

The World Wide Web runs on a protocol called HTTP — HyperText Transfer Protocol. When your browser loads a webpage, when your mobile app fetches your profile, when your ASP.NET application calls an external payment gateway or a weather API, every single one of those interactions is an HTTP request followed by an HTTP response.

HTTP itself rides on top of a lower-level protocol called TCP — Transmission Control Protocol. TCP is the reliable, ordered, error-checked delivery layer of the internet. Think of it like shipping: HTTP is the letter you're sending, and TCP is the courier service that guarantees it arrives in the right order without being corrupted.

For a TCP connection to exist between two computers — your application server and the remote API you're calling — your operating system must open a *socket*. A socket is a combination of an IP address and a port number. Your computer has ports numbered 0 through 65,535. Ports below 1024 are reserved for well-known services (port 80 for HTTP, port 443 for HTTPS, port 22 for SSH, and so on). Ports from 1024 upward are available for applications to use.

When your application makes an outbound HTTP request, the operating system assigns it an *ephemeral port* — a temporary, randomly-chosen port in the upper range, typically 49152–65535 on Windows. That port is occupied for the duration of the connection, and for a period afterward called TIME_WAIT, even after the connection is technically closed. We will come back to TIME_WAIT shortly. It is the villain of our story.

### 1.2 The Database Connection Analogy — Your Old Friend

Before we talk about HTTP connections, let us talk about something most .NET developers know well: database connections.

Imagine you've built an ASP.NET web application backed by SQL Server. Every time a user makes a request that touches the database, your application needs a connection to SQL Server. Opening a database connection is expensive — it involves a TCP handshake with the database server, authentication, TLS negotiation, and session setup. It takes tens or hundreds of milliseconds.

If you opened a new connection for every single database query and then threw it away, your application would be unusably slow. So decades ago, .NET introduced *connection pooling*. The `SqlConnection` class does not actually close the underlying database connection when you call `connection.Close()` or dispose the `SqlConnection`. Instead, it returns the connection to a pool. The next time code asks for a connection to the same SQL Server with the same connection string, the pool hands out that same underlying connection, saving the expensive reconnection overhead.

This is why, in every ASP.NET tutorial, you will see code like this:

```csharp
// The "right" way with ADO.NET
using (var conn = new SqlConnection(connectionString))
{
    await conn.OpenAsync();
    // ... run your query ...
} // conn is disposed here, but the underlying TCP socket goes back to the pool
```

The `using` block is correct. Disposing `SqlConnection` is correct. The pool is what makes it efficient. If you never disposed your `SqlConnection` instances, you'd exhaust the connection pool and your application would hang waiting for a free slot.

Now hold that mental model. We're going to use it again in about three paragraphs.

### 1.3 The Early Days of .NET HTTP — WebClient, WebRequest, and HttpWebRequest

Before `HttpClient` was a thing, .NET developers had other options for making HTTP requests. Understanding this history is important for two reasons: first, it explains why so many legacy codebases look the way they do, and second, it helps you appreciate exactly what problem `HttpClient` was designed to solve.

**`WebClient`** was the simplest option. Introduced in .NET Framework 1.0, it was a high-level wrapper for making simple HTTP requests:

```csharp
// .NET Framework era code
using (var client = new WebClient())
{
    string result = client.DownloadString("https://api.example.com/data");
    Console.WriteLine(result);
}
```

`WebClient` is synchronous by default (though async variants were added later), does not support fine-grained control over headers or request bodies, and is essentially a convenience wrapper around what came next.

**`HttpWebRequest`** and its companion **`HttpWebResponse`** gave you much more control:

```csharp
// HttpWebRequest — verbose, but powerful for its era
var request = (HttpWebRequest)WebRequest.Create("https://api.example.com/data");
request.Method = "GET";
request.Headers.Add("Authorization", "Bearer " + token);

using (var response = (HttpWebResponse)request.GetResponse())
using (var reader = new StreamReader(response.GetResponseStream()))
{
    string result = reader.ReadToEnd();
    Console.WriteLine(result);
}
```

Both of these APIs managed their own underlying HTTP connections through a class called `ServicePointManager`, which maintained a pool of `ServicePoint` objects — each one representing a connection pool to a particular host. This was the equivalent of `SqlConnection`'s pool, but for HTTP. `ServicePointManager` is a global, static, process-wide object. If you are working in .NET Framework and you need to enable TLS 1.2, you have almost certainly seen code like this at the very top of `Application_Start` or `Main`:

```csharp
// .NET Framework — required to enable TLS 1.2
ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11;
```

The `ServicePointManager` era was not without its problems. Configuration was global and could not be varied per-request or per-client. Thread safety was tricky. The API was verbose. DNS changes were poorly handled. But for many applications, it worked.

Then came `HttpClient`.

### 1.4 The Arrival of HttpClient

`HttpClient` was introduced in .NET Framework 4.5 (released with Visual Studio 2012) as a modern, async-first HTTP client with a cleaner, composable API. It came with a pipeline model built around `HttpMessageHandler` — a chain of handlers that each request passes through, similar in concept to ASP.NET's middleware pipeline. And it was immediately embraced by .NET developers everywhere.

The basic API was refreshingly clean:

```csharp
// .NET Framework 4.5+ and all modern .NET
var client = new HttpClient();
client.BaseAddress = new Uri("https://api.example.com/");
client.DefaultRequestHeaders.Accept.Add(
    new MediaTypeWithQualityHeaderValue("application/json"));

var response = await client.GetAsync("data");
response.EnsureSuccessStatusCode();
var result = await response.Content.ReadAsStringAsync();
```

Async support was first-class. The API was composable. Handlers could be chained. Life was good.

And then developers started disposing it.

---

## Part 2: The Two Worst Mistakes You Can Make with HttpClient

### 2.1 Mistake #1: Disposing HttpClient per Request

`HttpClient` implements `IDisposable`. In .NET, the convention for disposable objects is clear: use them in a `using` block so they get disposed when you're done. Resharper warns you if you don't. Code reviewers remind you. It is one of the most drilled-in habits of C# development.

So developers wrote code like this — and it is wrong:

```csharp
// ❌ DO NOT DO THIS
[ApiController]
[Route("[controller]")]
public class WeatherController : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        // A new HttpClient — and a new TCP connection — for EVERY request
        using var client = new HttpClient();
        client.BaseAddress = new Uri("https://api.weather.example.com/");
        var result = await client.GetFromJsonAsync<WeatherData>("current");
        return Ok(result);
    }
}
```

This code looks perfectly reasonable to a developer trained in standard .NET disposal patterns. But it has a catastrophic flaw.

When `HttpClient` is disposed, its underlying `HttpMessageHandler` is also disposed. The `HttpClientHandler` (which is what does the actual TCP work) closes the TCP connection. But here's the catch: TCP does not release ports instantly when a connection is closed. The operating system puts the closed connection into a state called **TIME_WAIT**.

TIME_WAIT exists for a technically sound reason: when one side of a TCP connection closes it, delayed packets might still be in flight on the network. If the operating system immediately reused the same local port for a new connection, those delayed packets could arrive and be misinterpreted as belonging to the new connection. So instead, the OS keeps the socket in TIME_WAIT for a period — on Windows, this is **240 seconds by default** (four minutes), controlled by the registry key `HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters\TcpTimedWaitDelay`.

Now imagine your ASP.NET controller handles 100 requests per second. Each request creates a new `HttpClient`, makes one outbound call, then disposes it. That's 100 sockets entering TIME_WAIT per second. After 240 seconds, you have 24,000 sockets in TIME_WAIT. The Windows ephemeral port range is about 16,384 ports by default (though configurable). You have now exhausted your ephemeral ports.

What happens when ephemeral ports are exhausted? Your application cannot open any new TCP connections — to any destination. Your outbound HTTP calls start failing. Your database connections fail. Everything falls apart. The error messages look like:

```
System.Net.Sockets.SocketException (10055): An operation on a socket could not be performed 
because the system lacked sufficient buffer space or because a queue was full.
```

or:

```
System.Net.Http.HttpRequestException: The SSL connection could not be established, 
see inner exception.
---> System.IO.IOException: An existing connection was forcibly closed by the remote host.
---> System.Net.Sockets.SocketException (10054)
```

And the maddening part? Under light load, it works fine. The bug is invisible during development. It is invisible during QA. It shows up exactly when you need your application to perform — under production load — and it is genuinely terrifying to diagnose without prior knowledge.

You can observe it with `netstat`:

```powershell
# Windows PowerShell — count sockets in TIME_WAIT state
netstat -an | Select-String "TIME_WAIT" | Measure-Object -Line

# If this number is in the thousands, you have a problem
```

A famous post by Simon Timms titled "You're using HttpClient wrong and it is destabilizing your software" (published in 2016 on ASP.NET Monsters) brought this issue to widespread attention and sent shockwaves through the .NET community. Many teams discovered, retroactively, that this was the root cause of mysterious production instability they had never fully diagnosed.

### 2.2 Mistake #2: Making HttpClient a Static Singleton

Once developers learned about socket exhaustion, the obvious fix seemed to be: don't create a new `HttpClient` every time. Make it a singleton:

```csharp
// ❌ This fixes socket exhaustion but introduces a different problem
public class WeatherService
{
    private static readonly HttpClient _client = new HttpClient
    {
        BaseAddress = new Uri("https://api.weather.example.com/")
    };

    public async Task<WeatherData?> GetCurrentWeatherAsync()
    {
        return await _client.GetFromJsonAsync<WeatherData>("current");
    }
}
```

This eliminates socket exhaustion. The static `HttpClient` is created once, its connections are pooled and reused, and you never exhaust ports. Many applications ran like this for years without issue.

But there is a subtle, insidious problem: **DNS staleness**.

`HttpClient` only resolves DNS when it opens a new TCP connection. If it already has an open TCP connection to `api.weather.example.com`, it will continue using that connection — and the underlying IP address — indefinitely. It does not check whether the DNS entry has changed. It does not respect the DNS record's TTL (Time To Live).

In a world of static servers with static IP addresses, this is fine. But modern infrastructure is anything but static:

- **Cloud services** frequently change IP addresses. Azure, AWS, and GCP use dynamic IP pools behind their load balancers and CDNs.
- **Blue/green deployments** often involve shifting DNS from the old environment to the new one.
- **Kubernetes clusters** use short-lived pod IPs. When a pod is replaced, its IP changes. DNS is the mechanism by which clients find the new pod.
- **Microservice meshes** like Consul or Kubernetes Services use DNS for service discovery.

If your application has a long-lived `HttpClient` with a persistent connection to `https://my-service.internal/`, and that service's IP address changes due to a redeployment, your `HttpClient` will continue sending requests to the old IP until the connection drops. Depending on the server configuration, the old IP might stop responding, or worse, might redirect silently to an error page. Your application appears to be calling the service, but it's talking to a ghost.

The symptoms are intermittent. Requests work, then fail, then work again (if the connection eventually drops and is re-established with the new IP). The failure mode is particularly confusing because it depends on connection timing and network behavior that is completely opaque from within your application code.

So you are stuck with a dilemma:

- **Dispose `HttpClient` per request** → socket exhaustion
- **Use a static `HttpClient`** → DNS staleness

Both options are wrong. What is the right answer?

---

## Part 3: Understanding the Real Problem — HttpMessageHandler

### 3.1 What Actually Does the Work

To understand the solution, you need to understand the architecture of `HttpClient`. The `HttpClient` class is not what actually makes TCP connections. It is a thin wrapper and configurator. The actual HTTP connection management — the TCP socket opening, SSL/TLS handshaking, DNS resolution, HTTP protocol handling — is done by an **`HttpMessageHandler`**.

When you write:

```csharp
var client = new HttpClient();
```

.NET internally creates a default `HttpClientHandler` (which, since .NET Core 2.1, wraps a `SocketsHttpHandler`) and assigns it to the `HttpClient`. The `HttpClient` itself is almost trivially lightweight — it has base address, default headers, and a timeout. The `HttpClientHandler` is the expensive one: it owns the connection pool.

You can make the handler explicit:

```csharp
var handler = new SocketsHttpHandler
{
    PooledConnectionLifetime = TimeSpan.FromMinutes(15),
    PooledConnectionIdleTimeout = TimeSpan.FromMinutes(2),
    MaxConnectionsPerServer = 10
};

var client = new HttpClient(handler, disposeHandler: false);
```

When you dispose an `HttpClient`, if `disposeHandler` is `true` (the default when you use the parameterless constructor), the handler is disposed too. That's what kills the connection pool and forces the OS to close the TCP connection — which then enters TIME_WAIT.

The key insight is: **the `HttpClient` is cheap; the `HttpMessageHandler` is expensive**. The socket exhaustion problem occurs because creating and destroying `HttpClient` instances also creates and destroys `HttpMessageHandler` instances, and therefore creates and destroys TCP connections.

### 3.2 SocketsHttpHandler — The .NET Core Revolution

In .NET Framework, the default handler was `HttpClientHandler`, which on Windows delegated to `WinHttpHandler` (the Windows native HTTP stack). This was fine for most scenarios, but it meant behavior was tied to the Windows WinHTTP API and OS configuration.

Starting with .NET Core 2.1 (released in 2018), Microsoft introduced `SocketsHttpHandler` as the new default handler. Unlike `HttpClientHandler`, `SocketsHttpHandler` is a fully managed .NET implementation of an HTTP/1.1 and HTTP/2 client. It runs on all platforms (Windows, Linux, macOS) with identical behavior and does not depend on OS HTTP libraries. It also exposes several important properties that were not available on `HttpClientHandler`:

```csharp
var handler = new SocketsHttpHandler
{
    // How long to keep a pooled connection alive 
    // (even if idle). Triggers DNS re-resolution when exceeded.
    PooledConnectionLifetime = TimeSpan.FromMinutes(15),

    // How long an idle connection sits in the pool before being closed
    PooledConnectionIdleTimeout = TimeSpan.FromMinutes(2),

    // Maximum number of connections per server endpoint
    MaxConnectionsPerServer = 10,

    // Whether to use HTTP/2 (requires server support)
    EnableMultipleHttp2Connections = true,

    // Connection timeout
    ConnectTimeout = TimeSpan.FromSeconds(30),

    // Expect100ContinueTimeout
    Expect100ContinueTimeout = TimeSpan.FromSeconds(1)
};
```

The most important of these properties is `PooledConnectionLifetime`. When a connection's lifetime exceeds this value and the connection is not in active use, `SocketsHttpHandler` will close it and open a fresh one — including a fresh DNS resolution. This is the mechanism by which DNS staleness is solved without `IHttpClientFactory`: you set `PooledConnectionLifetime` to a reasonable value (15 minutes is commonly cited), and connections are periodically refreshed.

### 3.3 HttpClientHandler vs SocketsHttpHandler — The Timeline

This is a common source of confusion. Here is the definitive timeline:

| Version | Default Handler | Notes |
|---|---|---|
| .NET Framework 4.5+ | `HttpClientHandler` (uses WinHTTP on Windows) | OS-level HTTP stack |
| .NET Core 1.x | `HttpClientHandler` | Cross-platform managed implementation |
| .NET Core 2.0 | `HttpClientHandler` | SocketsHttpHandler preview |
| .NET Core 2.1+ | `SocketsHttpHandler` (via `HttpClientHandler`) | `HttpClientHandler` delegates to `SocketsHttpHandler` |
| .NET 5+ | `SocketsHttpHandler` (direct) | Full HTTP/2 support; HTTP/3 preview |
| .NET 6+ | `SocketsHttpHandler` | HTTP/3 stable on some platforms |
| .NET 8+ | `SocketsHttpHandler` | HTTP/3 fully stable; QUIC support |
| .NET 10 | `SocketsHttpHandler` | Continued improvements |

A note for .NET Framework developers: `SocketsHttpHandler` does **not** exist in .NET Framework. If you are using `HttpClient` in a .NET Framework 4.x application, you are using `HttpClientHandler` which ultimately calls into WinHTTP via `WinHttpHandler`. The DNS staleness problem still applies to you. The solution in .NET Framework is to use `IHttpClientFactory` (yes, it is available via NuGet even for .NET Framework) or to manually manage singleton `HttpClient` instances with `ServicePoint.ConnectionLeaseTimeout` configured.

---

## Part 4: The Solution — IHttpClientFactory

### 4.1 Introduction — What Is IHttpClientFactory?

`IHttpClientFactory` was introduced in ASP.NET Core 2.1 (which ships alongside .NET Core 2.1) in May 2018. It was designed to solve both problems we've described — socket exhaustion and DNS staleness — in a single, clean, DI-friendly abstraction.

The core idea is elegant: **separate the lifetime of `HttpClient` from the lifetime of `HttpMessageHandler`**.

- `HttpClient` instances created by the factory are short-lived. You get one, use it for a request or a short-lived operation, and let it go. Because the factory manages the underlying handler, disposing the `HttpClient` wrapper does not dispose the handler, so no sockets enter TIME_WAIT.
- `HttpMessageHandler` instances are pooled by the factory and recycled on a configurable schedule (default: two minutes). When a handler's time is up and no `HttpClient` instances are still using it, it is disposed. The next `HttpClient` gets a fresh handler, which opens fresh connections with fresh DNS resolutions.

The default handler lifetime of two minutes was chosen deliberately. TCP connections in TIME_WAIT last four minutes on Windows. Two minutes means the handler is recycled at half the TIME_WAIT interval, ensuring that any connections opened by the old handler will have fully closed by the time they might cause confusion.

This is the architectural twin of SQL Server's connection pool — and understanding it as such makes the behavior immediately intuitive.

### 4.2 Prerequisites and Registration — Getting Set Up

`IHttpClientFactory` lives in the `Microsoft.Extensions.Http` NuGet package. In ASP.NET Core projects, this package is already included transitively through `Microsoft.AspNetCore.App`. If you are using it in a non-ASP.NET project (a console app, a Worker Service, a class library), you'll need to add it explicitly:

```xml
<!-- .csproj -->
<PackageReference Include="Microsoft.Extensions.Http" Version="10.0.0" />
```

Or via the dotnet CLI:

```bash
dotnet add package Microsoft.Extensions.Http
```

Registration is done on the DI container's `IServiceCollection`. In ASP.NET Core (using the minimal hosting model introduced in .NET 6 and the recommended approach for .NET 8+/10):

```csharp
// Program.cs — .NET 6, 7, 8, 9, 10 (minimal hosting model)
var builder = WebApplication.CreateBuilder(args);

// Simplest registration — enables the basic factory
builder.Services.AddHttpClient();

var app = builder.Build();
app.Run();
```

In the older ASP.NET Core startup style (still valid, often seen in .NET Framework migration projects or older codebases):

```csharp
// Startup.cs — older ASP.NET Core style
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddControllers();
        services.AddHttpClient(); // Registers IHttpClientFactory
    }
}
```

### 4.3 Pattern 1 — Basic Factory Usage

The simplest usage: inject `IHttpClientFactory` and call `CreateClient()`:

```csharp
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("[controller]")]
public class WeatherController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;

    public WeatherController(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        // ✅ HttpClient is short-lived, but the underlying handler is pooled
        var client = _httpClientFactory.CreateClient();
        client.BaseAddress = new Uri("https://api.weather.example.com/");
        
        var result = await client.GetFromJsonAsync<WeatherData>("current");
        return Ok(result);
    }
}
```

This is already much better than `new HttpClient()`. The handler is pooled and reused. When `client` goes out of scope and is garbage collected (or if you call `client.Dispose()`), only the lightweight wrapper is disposed — the handler stays in the pool.

However, you notice the `BaseAddress` is set inside the method. This is not ideal — you're configuring the client each time you use it. That's what named clients solve.

### 4.4 Pattern 2 — Named Clients

Named clients let you pre-configure `HttpClient` instances at startup and retrieve them by name:

```csharp
// Program.cs
builder.Services.AddHttpClient("weather", client =>
{
    client.BaseAddress = new Uri("https://api.weather.example.com/");
    client.DefaultRequestHeaders.Add("Accept", "application/json");
    client.DefaultRequestHeaders.Add("X-API-Version", "2");
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddHttpClient("payments", client =>
{
    client.BaseAddress = new Uri("https://api.payments.example.com/");
    client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
    client.Timeout = TimeSpan.FromSeconds(10); // Payments must be fast or fail
});
```

Usage in a controller or service:

```csharp
public class WeatherService
{
    private readonly IHttpClientFactory _factory;

    public WeatherService(IHttpClientFactory factory)
    {
        _factory = factory;
    }

    public async Task<WeatherData?> GetWeatherAsync(string city)
    {
        // ✅ Gets a pre-configured client by name
        var client = _factory.CreateClient("weather");
        return await client.GetFromJsonAsync<WeatherData>($"forecast?city={city}");
    }
}
```

Named clients are registered with `AddHttpClient` and are good when you need to share the same configuration across multiple callers, or when you cannot use typed clients (covered next).

The downside of named clients is that the name is a magic string — "weather" — and typos at the call site will compile fine but fail at runtime. Typed clients solve this.

### 4.5 Pattern 3 — Typed Clients (The Recommended Pattern)

Typed clients are the cleanest, most expressive, and most testable pattern for using `IHttpClientFactory`. Instead of injecting the factory and calling `CreateClient("name")`, you create a class whose constructor takes an `HttpClient`, and you inject that class directly.

Here's a complete example. Suppose you are building a service that calls the GitHub API:

```csharp
// GitHubService.cs — the typed client
public class GitHubService
{
    private readonly HttpClient _client;

    // The HttpClient is injected by the DI container / factory
    public GitHubService(HttpClient client)
    {
        _client = client;
    }

    public async Task<GitHubUser?> GetUserAsync(string username)
    {
        return await _client.GetFromJsonAsync<GitHubUser>($"users/{username}");
    }

    public async Task<IEnumerable<GitHubRepo>> GetReposAsync(string username)
    {
        return await _client.GetFromJsonAsync<IEnumerable<GitHubRepo>>(
            $"users/{username}/repos") ?? Array.Empty<GitHubRepo>();
    }
}

// Models
public record GitHubUser(string Login, string Name, string AvatarUrl, int PublicRepos);
public record GitHubRepo(string Name, string Description, int StargazersCount, bool Fork);
```

Register the typed client in `Program.cs`:

```csharp
// Program.cs
builder.Services.AddHttpClient<GitHubService>(client =>
{
    client.BaseAddress = new Uri("https://api.github.com/");
    
    // GitHub API requires these headers
    client.DefaultRequestHeaders.Add("Accept", "application/vnd.github.v3+json");
    client.DefaultRequestHeaders.Add("User-Agent", "ObserverMagazineApp/1.0");
});
```

Inject and use in a controller:

```csharp
[ApiController]
[Route("[controller]")]
public class GitHubController : ControllerBase
{
    private readonly GitHubService _github;

    // ✅ GitHubService is transient — gets a fresh HttpClient from the pool each time
    public GitHubController(GitHubService github)
    {
        _github = github;
    }

    [HttpGet("{username}")]
    public async Task<IActionResult> GetUser(string username)
    {
        var user = await _github.GetUserAsync(username);
        return user is null ? NotFound() : Ok(user);
    }
}
```

What happens behind the scenes:

1. `GitHubService` is registered as a **transient** service (new instance per injection point).
2. When DI resolves `GitHubService`, it calls `IHttpClientFactory.CreateClient("GitHubService")` to get an `HttpClient` and injects it into the `GitHubService` constructor.
3. That `HttpClient` wraps a pooled `HttpMessageHandler` from the factory's handler pool.
4. When the request ends and `GitHubService` is garbage collected, the `HttpClient` wrapper is disposed, but the handler stays in the pool.
5. The next request that needs `GitHubService` gets a new `GitHubService` with a new `HttpClient` wrapper pointing to the same (or an equally valid) pooled handler.

### 4.6 Typed Clients with Interfaces — The Testable Pattern

In the example above, `GitHubService` is a concrete class. This is fine and simple, but for testability it is often better to extract an interface:

```csharp
// Interface — define the contract
public interface IGitHubService
{
    Task<GitHubUser?> GetUserAsync(string username);
    Task<IEnumerable<GitHubRepo>> GetReposAsync(string username);
}

// Implementation — wraps HttpClient
public class GitHubService : IGitHubService
{
    private readonly HttpClient _client;

    public GitHubService(HttpClient client)
    {
        _client = client;
    }

    public async Task<GitHubUser?> GetUserAsync(string username)
    {
        return await _client.GetFromJsonAsync<GitHubUser>($"users/{username}");
    }

    public async Task<IEnumerable<GitHubRepo>> GetReposAsync(string username)
    {
        return await _client.GetFromJsonAsync<IEnumerable<GitHubRepo>>(
            $"users/{username}/repos") ?? Array.Empty<GitHubRepo>();
    }
}
```

Registration with interface:

```csharp
// Program.cs
builder.Services.AddHttpClient<IGitHubService, GitHubService>(client =>
{
    client.BaseAddress = new Uri("https://api.github.com/");
    client.DefaultRequestHeaders.Add("Accept", "application/vnd.github.v3+json");
    client.DefaultRequestHeaders.Add("User-Agent", "ObserverMagazineApp/1.0");
});
```

Now controllers and services can inject `IGitHubService`, and in tests, you can mock it completely:

```csharp
// In a unit test (xUnit + Moq example)
public class GitHubControllerTests
{
    [Fact]
    public async Task GetUser_ReturnsOk_WhenUserExists()
    {
        // Arrange
        var mockGitHub = new Mock<IGitHubService>();
        mockGitHub
            .Setup(g => g.GetUserAsync("octocat"))
            .ReturnsAsync(new GitHubUser("octocat", "The Octocat", "https://...", 42));

        var controller = new GitHubController(mockGitHub.Object);

        // Act
        var result = await controller.GetUser("octocat");

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        var user = Assert.IsType<GitHubUser>(ok.Value);
        Assert.Equal("octocat", user.Login);
    }
}
```

No HTTP calls. No network. No `IHttpClientFactory` in sight. Pure, fast unit tests.

### 4.7 Pattern 4 — The SocketsHttpHandler Alternative (No DI Required)

`IHttpClientFactory` solves both problems, but it requires a DI container and the `Microsoft.Extensions.Http` package. If you're in a scenario without DI — a console application, a library, a legacy .NET Framework application — you can solve both problems using `SocketsHttpHandler` directly:

```csharp
// ✅ Alternative: singleton HttpClient with SocketsHttpHandler and PooledConnectionLifetime
// Create once at application startup
var handler = new SocketsHttpHandler
{
    PooledConnectionLifetime = TimeSpan.FromMinutes(15) // Refreshes DNS every 15 minutes
};

// Create once and share
var sharedClient = new HttpClient(handler, disposeHandler: false)
{
    BaseAddress = new Uri("https://api.example.com/")
};

// Use sharedClient throughout your application — it is thread-safe
```

The `PooledConnectionLifetime` of 15 minutes means that after 15 minutes, connections in the pool will be recycled when they are not in use, causing a fresh DNS lookup on the next connection establishment. This solves the DNS staleness problem.

This approach is appropriate when:
- You don't have a DI container.
- You have a limited number of external services (one or two).
- You want to avoid the DI overhead in a library or utility.
- You are on .NET Core 2.1 or later (where `SocketsHttpHandler` is available).

For .NET Framework applications without DI, you can configure `ServicePoint` to set `ConnectionLeaseTimeout`:

```csharp
// .NET Framework — DNS refresh approximation via ServicePoint
var endpoint = "https://api.example.com/";
ServicePoint servicePoint = ServicePointManager.FindServicePoint(new Uri(endpoint));
servicePoint.ConnectionLeaseTimeout = (int)TimeSpan.FromMinutes(15).TotalMilliseconds;
```

---

## Part 5: The Handler Lifecycle in Depth

### 5.1 What the Factory Actually Does

Let's look at what `IHttpClientFactory` (specifically `DefaultHttpClientFactory`, the internal implementation) does when you call `CreateClient("GitHubService")`.

The factory maintains an internal pool of `ActiveHandlerTrackingEntry` objects — one per named client configuration. Each entry contains:
- The `HttpMessageHandler` pipeline (the handler chain configured via `DelegatingHandler` and a primary handler).
- A creation timestamp.
- An expiry timer.
- A reference count tracking how many active `HttpClient` instances are using this handler.

When you call `CreateClient`:

1. The factory looks up the named client configuration.
2. It checks whether there is an active handler entry for that name whose lifetime has not expired.
3. If there is a valid entry, it creates a new `HttpClient` with `disposeHandler: false` pointing to the pooled handler.
4. If the existing entry has expired, or no entry exists, it creates a new `HttpMessageHandler` pipeline, creates a new `ActiveHandlerTrackingEntry`, and creates an `HttpClient` pointing to the new handler.
5. The expired entry is moved to an "expired handlers" queue but is not immediately disposed — it waits until all `HttpClient` instances that were using it have been garbage collected.

This is the key insight: **a handler is not disposed the moment its lifetime expires**. If you have an `HttpClient` that was created from a 2-minute handler, and that `HttpClient` is still alive at 3 minutes, the old handler is kept alive for that `HttpClient`. The new `HttpClient` instances get a new handler, but the old ones are not suddenly left with a dangling reference. This is managed through `ConditionalWeakTable` and a background cleanup timer that runs every 10 seconds.

When the last `HttpClient` that references an expired handler is garbage collected, the factory's cleanup timer notices that the handler's reference count has dropped to zero and disposes the handler at that point.

### 5.2 The Handler Pipeline — DelegatingHandlers

The `HttpMessageHandler` that `IHttpClientFactory` gives to your `HttpClient` is not necessarily a single handler. It can be a **pipeline** of `DelegatingHandler` instances, each one wrapping the next, with the primary handler (the `SocketsHttpHandler` or `HttpClientHandler`) at the innermost position.

Think of delegating handlers like middleware in ASP.NET Core — but for outbound HTTP requests instead of inbound ones. Each handler in the chain can inspect, modify, log, or retry the request before passing it to the next handler.

Here is a custom delegating handler that adds an API key header to every outgoing request:

```csharp
public class ApiKeyDelegatingHandler : DelegatingHandler
{
    private readonly string _apiKey;

    public ApiKeyDelegatingHandler(string apiKey)
    {
        _apiKey = apiKey;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        // Modify the request before it goes further down the pipeline
        request.Headers.Add("X-API-Key", _apiKey);

        // Pass to the next handler (or the primary handler if this is last)
        var response = await base.SendAsync(request, cancellationToken);

        // Optionally inspect or modify the response on the way back
        if (!response.IsSuccessStatusCode)
        {
            // Log the failure
            Console.WriteLine($"Request to {request.RequestUri} failed: {response.StatusCode}");
        }

        return response;
    }
}
```

Register it with a named or typed client:

```csharp
// Program.cs — register the handler and attach it to a client
builder.Services.AddTransient<ApiKeyDelegatingHandler>();

builder.Services.AddHttpClient<IExternalApiService, ExternalApiService>(client =>
{
    client.BaseAddress = new Uri("https://api.external.example.com/");
})
.AddHttpMessageHandler<ApiKeyDelegatingHandler>();
```

The critical point about DI lifetime for delegating handlers: **delegating handlers are registered as transient or scoped, not as part of the handler pool**. The factory creates a new handler pipeline for each `HttpMessageHandler` in the pool, which means each pooled handler has its own set of delegating handler instances.

Be careful: **delegating handlers that access scoped services (like `IHttpContextAccessor`) have known limitations** and can lead to context-bleed between requests. Microsoft's documentation warns explicitly about this. If you need per-request headers (like authentication tokens from the current user's context), it is safer to set them on the `HttpRequestMessage` directly or use named/typed clients with per-request configuration.

Here is a more sophisticated example — a delegating handler that adds a correlation ID to outgoing requests for distributed tracing:

```csharp
public class CorrelationIdDelegatingHandler : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        // Add a unique correlation ID to track this request across systems
        if (!request.Headers.Contains("X-Correlation-ID"))
        {
            request.Headers.Add("X-Correlation-ID", Guid.NewGuid().ToString("N"));
        }

        return base.SendAsync(request, cancellationToken);
    }
}
```

And an authorization handler that reads a token from a service:

```csharp
public class BearerTokenDelegatingHandler : DelegatingHandler
{
    private readonly ITokenService _tokenService;

    public BearerTokenDelegatingHandler(ITokenService tokenService)
    {
        _tokenService = tokenService;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var token = await _tokenService.GetAccessTokenAsync(cancellationToken);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await base.SendAsync(request, cancellationToken);
    }
}
```

### 5.3 Configuring the Primary Handler

You can replace the default primary handler via `ConfigurePrimaryHttpMessageHandler`:

```csharp
builder.Services.AddHttpClient<IMyService, MyService>(client =>
{
    client.BaseAddress = new Uri("https://api.example.com/");
})
.ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
{
    PooledConnectionLifetime = TimeSpan.FromMinutes(10),
    PooledConnectionIdleTimeout = TimeSpan.FromMinutes(2),
    MaxConnectionsPerServer = 20,
    AutomaticDecompression = System.Net.DecompressionMethods.All
});
```

Note that when using `IHttpClientFactory`, configuring `PooledConnectionLifetime` on the primary `SocketsHttpHandler` is somewhat redundant — the factory already handles handler recycling. But `MaxConnectionsPerServer` and other connection pool settings are meaningful here and are not managed by the factory.

### 5.4 Handler Lifetime Configuration

The default handler lifetime is two minutes. You can override it per named or typed client:

```csharp
// Set handler lifetime to 5 minutes for a specific client
builder.Services.AddHttpClient<IExternalApiService, ExternalApiService>()
    .SetHandlerLifetime(TimeSpan.FromMinutes(5));

// Disable handler rotation entirely (infinite lifetime)
builder.Services.AddHttpClient<IInternalService, InternalService>()
    .SetHandlerLifetime(Timeout.InfiniteTimeSpan);
```

When should you change the handler lifetime?

- **Shorter (e.g., 1 minute)**: If the service you're calling changes IPs very frequently (e.g., Kubernetes rolling deployments with fast pod replacement). More frequent DNS refreshes at the cost of more frequent connection establishment overhead (TCP handshakes, TLS negotiation).
- **Longer (e.g., 5–10 minutes)**: If connections are stable, DNS rarely changes, and TLS negotiation is expensive. Reduces connection overhead but delays DNS refresh.
- **Infinite**: For services where you absolutely control the IP (internal cluster services), where DNS changes never happen, and where you want maximum connection reuse. Combines the factory's DI benefits with singleton-like handler behavior.

The advice from the community is to start at the default 2 minutes and adjust only if you have a measured reason. A developer from one team quoted in community discussions notes: "I start at 5 minutes. Shorter rotates too aggressively (extra TLS handshakes); longer risks stale DNS."

---

## Part 6: DNS Staleness — The Full Story

### 6.1 Why DNS Changes Happen More Than You Think

If you have never been bitten by the DNS staleness problem, you might wonder how often DNS actually changes in practice. The honest answer is: more often than you'd expect, and almost always at the worst possible time.

Here are real scenarios where DNS changes and HttpClient staleness is a genuine operational risk:

**Scenario 1: Cloud Load Balancer IP Rotation**
Azure's Application Gateway, AWS's Application Load Balancer, and GCP's Cloud Load Balancing all use dynamic IP pools. The DNS records for `*.azure.microsoft.com` or AWS service endpoints may have TTLs as low as 60 seconds. A long-lived static `HttpClient` will ignore these changes entirely.

**Scenario 2: Kubernetes Pod Replacement**
In a Kubernetes deployment, each pod has an ephemeral IP. When a pod crashes and is replaced, its successor gets a different IP. Kubernetes Services expose a stable DNS name (like `my-service.my-namespace.svc.cluster.local`) that points to the current pod IPs via `kube-dns`. If your client caches the old DNS answer, it will try to connect to the dead pod's old IP, which is now either unassigned or assigned to an unrelated workload. Your requests will fail until the TCP connection timeout occurs.

**Scenario 3: Blue/Green Deployment**
During a blue/green deployment, the DNS record for `api.example.com` is updated from the blue environment's load balancer to the green environment's load balancer. Clients with long-lived connections continue to send requests to the blue environment, which may be spun down. Requests fail until connections are re-established.

**Scenario 4: Disaster Recovery Failover**
DR failover procedures almost always involve DNS changes — redirecting traffic from the primary region to the DR region. Any application with a cached DNS answer will try to connect to the primary region's (now offline) IP until the connection drops.

**Scenario 5: CDN and Edge Changes**
Content delivery networks and edge security providers (Cloudflare, Fastly, Akamai) frequently change IP addresses for traffic routing optimization, DDoS mitigation, or peering changes. If you're calling through a CDN, the IPs behind the DNS name may change without warning.

### 6.2 How DNS TTL Works and Why HttpClient Doesn't Respect It

Every DNS record has a TTL — a Time to Live measured in seconds. When a DNS resolver returns an answer, it includes the TTL, which tells the client how long to cache that answer before querying the DNS server again. A TTL of 60 means "cache this for 60 seconds and then re-query."

`HttpClient` (whether you use `new HttpClient()` or `IHttpClientFactory`) does not look at the DNS TTL. It simply holds open its TCP connections for as long as they remain alive, using whatever IP address was resolved when the connection was first established. The TTL of the DNS record is irrelevant to a connection that is already open.

This is actually correct TCP behavior — the IP address of an open connection does not change mid-stream. The problem is not that `HttpClient` ignores DNS TTL on live connections; the problem is that it never closes those connections and re-resolves. With `IHttpClientFactory`, the handler rotation every two minutes (default) forces connections to eventually close and be re-established with fresh DNS lookups. With a raw singleton `HttpClient` and `SocketsHttpHandler.PooledConnectionLifetime`, the same effect is achieved by periodically retiring connections.

### 6.3 The Stale DNS + Singleton Anti-Pattern in Detail

Here is the classic anti-pattern in full detail, so you can recognize it in existing codebases:

```csharp
// ❌ Classic anti-pattern — stale DNS waiting to happen
public class PaymentService
{
    // Created once, in the constructor, never recreated
    private readonly HttpClient _httpClient;

    public PaymentService()
    {
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri("https://api.payment-processor.com/")
        };
    }

    public async Task<PaymentResult> ChargeAsync(decimal amount, string token)
    {
        var response = await _httpClient.PostAsJsonAsync("charge", new { amount, token });
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<PaymentResult>()
               ?? throw new InvalidOperationException("Empty response");
    }
}
```

This is registered as a singleton:

```csharp
services.AddSingleton<PaymentService>(); // ❌ Singleton + static HttpClient = stale DNS
```

The `HttpClient` is created once, the TCP connection to the payment processor is established once, and it will live forever — until the connection is dropped by the server or network (e.g., TCP keepalive timeout, server restart, etc.). During that time, if the payment processor's IP changes, all payment requests go to the old IP. Your customers' payment attempts fail. This is an extremely high-stakes failure mode.

### 6.4 The Stale DNS + Typed Client in Singleton Anti-Pattern

There is a subtler version of this anti-pattern that trips up developers who *have* learned about `IHttpClientFactory`:

```csharp
// ❌ Typed client captured in a singleton — still stale DNS
public class PaymentService
{
    private readonly HttpClient _httpClient; // Injected by IHttpClientFactory

    public PaymentService(HttpClient httpClient) // Correct typed client pattern
    {
        _httpClient = httpClient; // CAPTURED HERE in the constructor
    }

    // ... methods
}

// Registration — this is the problem:
services.AddSingleton<PaymentService>(); // ❌ Singleton captures the HttpClient
services.AddHttpClient<PaymentService>(); // This registers PaymentService as Transient
// But the explicit AddSingleton OVERRIDES the AddHttpClient registration!
```

When `PaymentService` is registered as a singleton, it is created once and the `HttpClient` injected into its constructor is captured for the lifetime of the application — defeating the handler rotation mechanism of `IHttpClientFactory`. The handler will never be rotated, so DNS will never be refreshed.

The Microsoft documentation says explicitly: "❌ DO NOT cache `HttpClient` instances created by `IHttpClientFactory` for prolonged periods of time." The same applies to typed clients injected into singletons.

The correct pattern, if you truly need a singleton service that makes HTTP calls, is to inject `IHttpClientFactory` itself and call `CreateClient()` within each method:

```csharp
// ✅ Singleton service that correctly uses IHttpClientFactory
public class PaymentService
{
    private readonly IHttpClientFactory _factory;

    public PaymentService(IHttpClientFactory factory) // Inject the factory, not the client
    {
        _factory = factory;
    }

    public async Task<PaymentResult> ChargeAsync(decimal amount, string token)
    {
        // Create a fresh client for each call — the handler is still pooled
        var client = _factory.CreateClient("payments");
        var response = await client.PostAsJsonAsync("charge", new { amount, token });
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<PaymentResult>()
               ?? throw new InvalidOperationException("Empty response");
    }
}
```

---

## Part 7: Resilience with Polly — Retries, Circuit Breakers, and Beyond

### 7.1 Why You Need Resilience Patterns

In a microservices or API-driven architecture, your application depends on external services. External services fail. Networks drop packets. Load balancers return 503 temporarily. Rate limiters return 429. A downstream database has a momentary spike that causes a 500 for a few seconds.

Without resilience patterns, your application propagates these transient failures directly to your users. With resilience patterns, transient failures are absorbed automatically and the user sees a successful response (because the retry succeeded) or a graceful degradation (because a fallback was invoked).

`IHttpClientFactory` integrates seamlessly with **Polly**, the leading .NET resilience library. Starting with .NET 8, Microsoft also ships `Microsoft.Extensions.Http.Resilience`, a first-party package that provides pre-configured resilience pipelines built on Polly v8.

### 7.2 Polly v8 and Microsoft.Extensions.Http.Resilience

Polly v8 was a major rewrite of the library. It replaced the older `Policy` fluent API with a new `ResiliencePipeline` abstraction. The older `Microsoft.Extensions.Http.Polly` package (which provided `AddPolicyHandler`) is largely superseded by `Microsoft.Extensions.Http.Resilience` for new projects.

Install the package:

```bash
dotnet add package Microsoft.Extensions.Http.Resilience
```

Or in your `Directory.Packages.props` with Central Package Management:

```xml
<PackageVersion Include="Microsoft.Extensions.Http.Resilience" Version="10.4.0" />
```

### 7.3 The Standard Resilience Handler — One Call to Rule Them All

For most applications, the `AddStandardResilienceHandler()` extension method gives you a production-grade resilience pipeline with sensible defaults:

```csharp
// Program.cs
builder.Services.AddHttpClient<IGitHubService, GitHubService>(client =>
{
    client.BaseAddress = new Uri("https://api.github.com/");
    client.DefaultRequestHeaders.Add("Accept", "application/vnd.github.v3+json");
    client.DefaultRequestHeaders.Add("User-Agent", "ObserverMagazineApp/1.0");
})
.AddStandardResilienceHandler(); // ✅ Adds the full standard pipeline
```

The standard resilience pipeline includes (from outermost to innermost):
1. **Total request timeout**: Overall timeout across all retry attempts (default: 30 seconds).
2. **Retry**: Exponential backoff with jitter, up to 3 retries for transient HTTP errors (5xx, 429, 408, `HttpRequestException`).
3. **Circuit breaker**: Opens when 10% of requests fail within a 30-second sampling window (minimum 100 requests), breaks for 5 seconds.
4. **Attempt timeout**: Per-attempt timeout (default: 10 seconds), so individual retries don't block indefinitely.

You can configure the defaults:

```csharp
builder.Services.AddHttpClient<IGitHubService, GitHubService>(client =>
{
    client.BaseAddress = new Uri("https://api.github.com/");
})
.AddStandardResilienceHandler(options =>
{
    // Adjust total timeout
    options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(60);

    // Adjust retry
    options.Retry.MaxRetryAttempts = 5;
    options.Retry.Delay = TimeSpan.FromMilliseconds(500);
    options.Retry.BackoffType = DelayBackoffType.Exponential;
    options.Retry.UseJitter = true; // Prevents retry storms

    // Adjust circuit breaker
    options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(20);
    options.CircuitBreaker.FailureRatio = 0.3; // Break at 30% failure rate
    options.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(15);
});
```

### 7.4 Custom Resilience Pipelines with AddResilienceHandler

When the standard handler's defaults don't match your requirements, use `AddResilienceHandler` to build a custom pipeline:

```csharp
// Program.cs
builder.Services.AddHttpClient<IPaymentService, PaymentService>(client =>
{
    client.BaseAddress = new Uri("https://api.payment-processor.com/");
    client.Timeout = TimeSpan.FromSeconds(60); // Global timeout
})
.AddResilienceHandler("payment-pipeline", pipeline =>
{
    // Payment requests should not be retried for non-idempotent operations.
    // We retry only on specific transient network errors, not on 4xx/5xx.
    pipeline.AddRetry(new HttpRetryStrategyOptions
    {
        MaxRetryAttempts = 2,
        Delay = TimeSpan.FromMilliseconds(500),
        BackoffType = DelayBackoffType.Constant,
        UseJitter = true,
        // Only retry on network-level errors, not HTTP errors
        ShouldHandle = args => ValueTask.FromResult(
            args.Outcome.Exception is HttpRequestException
        )
    });

    // Per-attempt timeout
    pipeline.AddTimeout(new HttpTimeoutStrategyOptions
    {
        Timeout = TimeSpan.FromSeconds(15)
    });

    // Circuit breaker
    pipeline.AddCircuitBreaker(new HttpCircuitBreakerStrategyOptions
    {
        FailureRatio = 0.5,                          // Break at 50% failure
        SamplingDuration = TimeSpan.FromSeconds(10), // Over a 10-second window
        MinimumThroughput = 8,                       // Minimum 8 requests to evaluate
        BreakDuration = TimeSpan.FromSeconds(30),    // Stay open for 30 seconds
        ShouldHandle = args => ValueTask.FromResult(
            args.Outcome.Result?.StatusCode is
                HttpStatusCode.RequestTimeout or
                HttpStatusCode.TooManyRequests or
                HttpStatusCode.ServiceUnavailable
        )
    });
});
```

### 7.5 Understanding Retry Jitter — Why Randomness Saves Your Infrastructure

A common mistake when implementing retries is to use a fixed backoff delay: "retry after 2 seconds, retry after 4 seconds, retry after 8 seconds." This sounds reasonable until you consider what happens when your service has 1,000 concurrent users and an upstream service goes down briefly:

- All 1,000 requests fail at the same moment.
- All 1,000 clients retry after exactly 2 seconds.
- 1,000 simultaneous retry requests hit the upstream service, which may still be recovering.
- The upstream service goes down again under the load.
- All 1,000 clients retry after exactly 4 seconds... and the cycle continues.

This is called a **retry storm** or **thundering herd problem**, and it is a genuine cause of extended outages. The fix is jitter — adding randomness to the retry delay so that clients are spread out in time:

```csharp
pipeline.AddRetry(new HttpRetryStrategyOptions
{
    MaxRetryAttempts = 3,
    Delay = TimeSpan.FromMilliseconds(300),       // Base delay
    BackoffType = DelayBackoffType.Exponential,   // Grows exponentially
    UseJitter = true                              // Adds random variance to spread retries
    // With Exponential + Jitter, actual delays will be something like:
    // Attempt 1: ~300ms ± random
    // Attempt 2: ~600ms ± random
    // Attempt 3: ~1200ms ± random
});
```

The `UseJitter = true` flag in Polly adds proportional randomness to the backoff, implementing what is known as "decorrelated jitter" — a technique popularized by Marc Brooker's research at AWS showing it significantly reduces retry storms compared to simple exponential backoff.

### 7.6 Circuit Breakers — Failing Fast Gracefully

The circuit breaker pattern takes its name from electrical circuit breakers that protect your home's wiring. When too much current flows through a circuit, the breaker trips and cuts power — protecting the wiring from damage. When the danger is past, you reset the breaker and restore power.

In software, a circuit breaker monitors calls to a downstream service. If failures exceed a threshold within a time window, the circuit "opens" — subsequent calls immediately return an error without attempting to contact the downstream service. After a configurable break duration, the circuit enters a "half-open" state: one test request is allowed through. If it succeeds, the circuit closes. If it fails, the break duration starts again.

This serves two purposes:
1. **Protect the downstream service**: Sending it fewer requests while it is struggling gives it a chance to recover.
2. **Fail fast for the caller**: Instead of waiting for a timeout on every request, the circuit breaker immediately returns an error, keeping your application responsive.

In an ASP.NET Core application with a circuit breaker on an outbound service, you might handle the `BrokenCircuitException` to return a graceful response:

```csharp
[ApiController]
[Route("[controller]")]
public class RecommendationsController : ControllerBase
{
    private readonly IRecommendationService _recommendations;
    private readonly ILogger<RecommendationsController> _logger;

    public RecommendationsController(
        IRecommendationService recommendations,
        ILogger<RecommendationsController> logger)
    {
        _recommendations = recommendations;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        try
        {
            var recs = await _recommendations.GetAsync();
            return Ok(recs);
        }
        catch (BrokenCircuitException ex)
        {
            // Circuit is open — don't wait for the service, return a fallback
            _logger.LogWarning(ex,
                "Recommendation service circuit is open. Returning empty recommendations.");
            return Ok(Array.Empty<Recommendation>()); // Graceful degradation
        }
    }
}
```

### 7.7 The Hedging Strategy — A Different Approach to Latency

Polly v8 introduced a **hedging** strategy that is fundamentally different from retry. Retry is sequential — you wait for a failure before trying again. Hedging is parallel — if the first request takes too long to respond (even if it hasn't failed), you fire a second request concurrently and wait for whichever one responds first.

This is useful for latency-sensitive operations where you cannot afford to wait for a retry:

```csharp
builder.Services.AddHttpClient<ISearchService, SearchService>()
    .AddStandardHedgingHandler(options =>
    {
        // If no response within 2 seconds, fire a parallel request
        options.Hedging.Delay = TimeSpan.FromSeconds(2);
        options.Hedging.MaxHedgedAttempts = 3; // Up to 3 concurrent attempts
    });
```

The hedging handler uses a pool of circuit breakers per URL authority to avoid sending hedged requests to known-bad endpoints.

---

## Part 8: IHttpClientFactory in .NET Framework — It's Not Just for .NET Core

### 8.1 The Good News for Legacy Codebases

Many developers assume that `IHttpClientFactory` is a .NET Core / .NET 5+ feature that is unavailable in .NET Framework applications. This is incorrect. The `Microsoft.Extensions.Http` package targets both .NET Standard 2.0 (compatible with .NET Framework 4.6.2+) and modern .NET. You can use `IHttpClientFactory` in your ASP.NET Framework MVC 5 application today.

Of course, ASP.NET Framework does not use the Microsoft DI container (`IServiceCollection`) by default — it typically uses nothing, Unity, Autofac, Ninject, or StructureMap. Adding `Microsoft.Extensions.DependencyInjection` to a .NET Framework application is the recommended approach for getting `IHttpClientFactory` in that environment.

### 8.2 Using IHttpClientFactory in ASP.NET MVC 5 (Framework 4.8)

Here's how to add `IHttpClientFactory` to an existing ASP.NET MVC 5 application targeting .NET Framework 4.8:

Install the packages:

```bash
Install-Package Microsoft.Extensions.Http
Install-Package Microsoft.Extensions.DependencyInjection
```

Set up the DI container in `Global.asax.cs`:

```csharp
// Global.asax.cs
public class MvcApplication : System.Web.HttpApplication
{
    // Store the service provider at application level
    public static IServiceProvider Services { get; private set; } = null!;

    protected void Application_Start()
    {
        AreaRegistration.RegisterAllAreas();
        FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
        RouteConfig.RegisterRoutes(RouteTable.Routes);

        // Build the service container
        var services = new ServiceCollection();

        // Register IHttpClientFactory with named clients
        services.AddHttpClient("github", client =>
        {
            client.BaseAddress = new Uri("https://api.github.com/");
            client.DefaultRequestHeaders.Add("Accept", "application/vnd.github.v3+json");
            client.DefaultRequestHeaders.Add("User-Agent", "MyApp/1.0");
        });

        services.AddHttpClient("weather", client =>
        {
            client.BaseAddress = new Uri("https://api.openweathermap.org/data/2.5/");
        });

        // Register your services
        services.AddTransient<IGitHubService, GitHubService>();
        services.AddTransient<IWeatherService, WeatherService>();

        Services = services.BuildServiceProvider();
    }
}
```

Resolving services in controllers (since ASP.NET MVC 5 uses the obsolete `DependencyResolver` API rather than proper DI):

```csharp
// For a full solution, implement IDependencyResolver using the IServiceProvider
// Here is a simple approach for demonstration
public class GitHubController : Controller
{
    private readonly IGitHubService _github;

    public GitHubController()
    {
        // Resolve from the global container
        _github = MvcApplication.Services.GetRequiredService<IGitHubService>();
    }

    public async Task<ActionResult> Index(string username)
    {
        var user = await _github.GetUserAsync(username);
        return View(user);
    }
}
```

For a production-quality solution, implement `System.Web.Mvc.IDependencyResolver` to integrate the Microsoft DI container with ASP.NET MVC 5's built-in DI system, eliminating the need to reference `MvcApplication.Services` directly.

### 8.3 ServicePointManager — The .NET Framework Equivalent of PooledConnectionLifetime

If you cannot add the `Microsoft.Extensions.Http` package to your .NET Framework application, or if you are maintaining a very old codebase that cannot be changed significantly, you can reduce the DNS staleness window using `ServicePointManager.ConnectionLeaseTimeout`:

```csharp
// .NET Framework — set connection lease timeout to 15 minutes for a specific endpoint
// Do this in Application_Start, before any HttpClient usage
var endpoint = "https://api.example.com/";
var servicePoint = ServicePointManager.FindServicePoint(new Uri(endpoint));
servicePoint.ConnectionLeaseTimeout = (int)TimeSpan.FromMinutes(15).TotalMilliseconds;
```

This tells the `ServicePoint` to close connections older than 15 minutes, forcing a new TCP connection (and therefore a new DNS lookup) after that time. It is a cruder tool than `IHttpClientFactory` or `SocketsHttpHandler.PooledConnectionLifetime`, and it requires you to know all your endpoint URIs at startup, but it gets the job done.

You can also tune `ServicePointManager.DefaultConnectionLimit`, which sets the maximum number of concurrent connections per server (default is 2 per server in .NET Framework — yes, 2, which is absurdly low for high-throughput applications):

```csharp
// .NET Framework — increase the global connection limit
// Default is 2 per server, which throttles high-throughput apps
ServicePointManager.DefaultConnectionLimit = 50;
```

This `DefaultConnectionLimit = 2` default is a frequently encountered performance bottleneck in .NET Framework applications. If your application makes many concurrent outbound HTTP calls to the same server and performance degrades under load, check this value first.

---

## Part 9: Advanced Configuration and Production Tuning

### 9.1 MaxConnectionsPerServer — Throttling Concurrent Connections

`SocketsHttpHandler` (and by extension, `IHttpClientFactory`) maintains a pool of connections per server endpoint. The `MaxConnectionsPerServer` property controls how many concurrent connections can be open to a single server.

The default is `int.MaxValue` — effectively unlimited (constrained only by OS limits). This is a change from .NET Framework's default of 2.

When should you constrain this?

- **When calling a service that is rate-limiting connections** (some services limit incoming connections per client IP).
- **When you're running in a resource-constrained environment** and want to limit the number of open sockets.
- **When you have a shared internal service** that cannot handle many concurrent connections.

```csharp
builder.Services.AddHttpClient<IInventoryService, InventoryService>()
    .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
    {
        MaxConnectionsPerServer = 10 // Maximum 10 concurrent connections to this service
    });
```

Setting this too low under high concurrency causes requests to queue waiting for a connection — increasing latency rather than reducing load on the target server.

### 9.2 Request and Response Compression

Modern HTTP APIs commonly gzip-compress responses. `SocketsHttpHandler` can be configured to automatically decompress responses:

```csharp
builder.Services.AddHttpClient<IApiService, ApiService>()
    .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
    {
        AutomaticDecompression = System.Net.DecompressionMethods.GZip |
                                  System.Net.DecompressionMethods.Deflate |
                                  System.Net.DecompressionMethods.Brotli
    });
```

When `AutomaticDecompression` is set, `HttpClient` automatically adds the `Accept-Encoding: gzip, deflate, br` request header and decompresses the response body transparently. The response you read via `ReadAsStringAsync()` or `ReadFromJsonAsync()` is already decompressed.

### 9.3 HTTP/2 and HTTP/3 — Connection Multiplexing

HTTP/1.1 allows only one request per connection at a time (though pipelining was an attempt to address this, it was poorly supported and is disabled by default). Under high concurrency, you need many parallel connections to achieve throughput.

HTTP/2 uses a single TCP connection that can carry many concurrent request/response streams simultaneously — a technique called multiplexing. For applications making many parallel requests to the same server, HTTP/2 significantly reduces connection overhead.

```csharp
// Enable HTTP/2 with IHttpClientFactory
builder.Services.AddHttpClient<IApiService, ApiService>(client =>
{
    client.DefaultRequestVersion = HttpVersion.Version20;
    client.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrLower;
    // RequestVersionOrLower means: try HTTP/2, fall back to HTTP/1.1 if not supported
});
```

HTTP/3 (built on QUIC, a UDP-based transport) is available on .NET 6+ and is fully stable on .NET 8+:

```csharp
// Enable HTTP/3 (requires the server to support it)
builder.Services.AddHttpClient<IApiService, ApiService>(client =>
{
    client.DefaultRequestVersion = HttpVersion.Version30;
    client.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrLower;
});
```

For most internal service-to-service calls in a datacenter, HTTP/2 is the sweet spot — it significantly reduces connection overhead without the latency benefits of QUIC (since datacenter networks have very low latency and packet loss).

### 9.4 Timeout Configuration — The Four Levels

Timeouts with `IHttpClientFactory` can be configured at four distinct levels:

**Level 1: HttpClient.Timeout** — The overall request timeout, including all retries. If the entire operation (initial attempt + all retries) exceeds this, `TaskCanceledException` is thrown.

```csharp
builder.Services.AddHttpClient<IApiService, ApiService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(60); // 60 seconds total, across all retries
});
```

**Level 2: Total request timeout in resilience pipeline** — When using `AddStandardResilienceHandler` or a custom Polly pipeline, the "total request timeout" strategy wraps the entire pipeline including retries:

```csharp
.AddStandardResilienceHandler(options =>
{
    options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(30); // All attempts combined
});
```

**Level 3: Per-attempt timeout in resilience pipeline** — A per-attempt timeout ensures a single attempt doesn't block indefinitely:

```csharp
pipeline.AddTimeout(new HttpTimeoutStrategyOptions
{
    Timeout = TimeSpan.FromSeconds(10) // Each individual attempt, 10 seconds max
});
```

**Level 4: SocketsHttpHandler.ConnectTimeout** — The TCP connection establishment timeout:

```csharp
.ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
{
    ConnectTimeout = TimeSpan.FromSeconds(5) // TCP connection must establish within 5 seconds
});
```

A sensible production setup: ConnectTimeout of 5 seconds, per-attempt timeout of 10 seconds, total timeout of 30 seconds (allowing for a couple of retries). `HttpClient.Timeout` should be larger than the resilience pipeline's total timeout to avoid a race condition where `HttpClient.Timeout` fires before the pipeline can complete.

### 9.5 Logging — What You Get For Free

`IHttpClientFactory` provides automatic request logging out of the box. Every request and response for every client is logged by the built-in `HttpClientFactory` logging infrastructure, categorized by client name. By default, this produces log entries like:

```
info: System.Net.Http.HttpClient.GitHubService.ClientHandler[100]
      Sending HTTP request GET https://api.github.com/users/octocat

info: System.Net.Http.HttpClient.GitHubService.ClientHandler[101]
      Received HTTP response headers after 143.5347ms - 200
```

You can control the verbosity by setting the minimum log level for the `System.Net.Http.HttpClient` category:

```json
// appsettings.json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "System.Net.Http.HttpClient": "Warning"
        // Only log warnings and errors for all HTTP clients
    }
  }
}
```

Or control it per client:

```json
{
  "Logging": {
    "LogLevel": {
      "System.Net.Http.HttpClient.GitHubService": "Debug",
      "System.Net.Http.HttpClient.PaymentService": "Warning"
    }
  }
}
```

### 9.6 OpenTelemetry Integration

`IHttpClientFactory` integrates with .NET's built-in `System.Diagnostics.Activity` API, which means all outbound HTTP requests are automatically traced when you configure OpenTelemetry:

```csharp
// Program.cs
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing =>
    {
        tracing
            .AddAspNetCoreInstrumentation()  // Inbound ASP.NET Core requests
            .AddHttpClientInstrumentation()  // Outbound HttpClient requests ← all of them
            .AddOtlpExporter();              // Export to Jaeger, Zipkin, OTLP collector
    })
    .WithMetrics(metrics =>
    {
        metrics
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation() // HTTP client metrics (request duration, etc.)
            .AddOtlpExporter();
    });
```

With this setup, every outbound HTTP request from every typed or named client is automatically traced with spans, including the URL, method, status code, and duration. In Jaeger or Zipkin, you will see the full distributed trace — from the incoming request to your ASP.NET Core API, through the typed client, to the external service, and back.

---

## Part 10: Testing Typed Clients

### 10.1 Unit Testing — Mock the Interface

If you've defined your typed client behind an interface (`IGitHubService`, `IPaymentService`, etc.), unit testing the code that depends on it is trivial:

```csharp
// xUnit unit test
public class CheckoutServiceTests
{
    [Fact]
    public async Task Checkout_ChargesCorrectAmount()
    {
        // Arrange
        var mockPayment = new Mock<IPaymentService>();
        mockPayment
            .Setup(p => p.ChargeAsync(It.IsAny<decimal>(), It.IsAny<string>()))
            .ReturnsAsync(new PaymentResult { Success = true, TransactionId = "txn_123" });

        var service = new CheckoutService(mockPayment.Object);

        // Act
        var result = await service.CheckoutAsync(cart: new Cart { Total = 49.99m }, token: "tok_test");

        // Assert
        Assert.True(result.Success);
        mockPayment.Verify(p => p.ChargeAsync(49.99m, "tok_test"), Times.Once);
    }
}
```

No HTTP, no network, no `IHttpClientFactory` anywhere.

### 10.2 Integration Testing — Fake Message Handlers

When you want to test your typed client implementation itself (i.e., test `GitHubService` directly, not just mock it), you need to intercept the HTTP calls. The cleanest way is a custom `DelegatingHandler`:

```csharp
// A reusable fake handler for testing
public class FakeHttpMessageHandler : DelegatingHandler
{
    private readonly HttpResponseMessage _response;

    public FakeHttpMessageHandler(HttpResponseMessage response)
    {
        _response = response;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(_response);
    }
}
```

Use it in a test:

```csharp
[Fact]
public async Task GetUserAsync_ReturnsUser_OnSuccess()
{
    // Arrange — create a fake response
    var user = new GitHubUser("octocat", "The Octocat", "https://...", 42);
    var json = JsonSerializer.Serialize(user);
    
    var fakeHandler = new FakeHttpMessageHandler(
        new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        });

    // Build an HttpClient using the fake handler
    var httpClient = new HttpClient(fakeHandler)
    {
        BaseAddress = new Uri("https://api.github.com/")
    };

    var service = new GitHubService(httpClient);

    // Act
    var result = await service.GetUserAsync("octocat");

    // Assert
    Assert.NotNull(result);
    Assert.Equal("octocat", result.Login);
    Assert.Equal(42, result.PublicRepos);
}
```

This tests the real `GitHubService` implementation — JSON deserialization, URL building, error handling — without making any real network calls.

### 10.3 Integration Testing with WebApplicationFactory

For full integration tests that exercise your entire ASP.NET Core pipeline, `WebApplicationFactory<TProgram>` lets you replace `IHttpClientFactory` handlers with fakes:

```csharp
public class GitHubIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public GitHubIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetUser_ReturnsOk_WithFakeGitHubApi()
    {
        // Arrange — replace the primary handler for the "github" named client
        var fakeHandler = new FakeHttpMessageHandler(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """{"login":"octocat","name":"The Octocat","avatarUrl":"https://...","publicRepos":42}""",
                    Encoding.UTF8,
                    "application/json")
            });

        var client = _factory
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Replace the primary handler for the GitHubService typed client
                    services.AddHttpClient<IGitHubService, GitHubService>()
                        .ConfigurePrimaryHttpMessageHandler(() => fakeHandler);
                });
            })
            .CreateClient();

        // Act
        var response = await client.GetAsync("/github/octocat");

        // Assert
        response.EnsureSuccessStatusCode();
        var user = await response.Content.ReadFromJsonAsync<GitHubUser>();
        Assert.Equal("octocat", user?.Login);
    }
}
```

### 10.4 Handler Capture Verification

Sometimes you want to verify not just the response but also the exact request your typed client sent — the URL, headers, body, method:

```csharp
public class CapturingFakeHandler : DelegatingHandler
{
    public HttpRequestMessage? CapturedRequest { get; private set; }
    private readonly HttpResponseMessage _response;

    public CapturingFakeHandler(HttpResponseMessage response)
    {
        _response = response;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        CapturedRequest = request;
        return Task.FromResult(_response);
    }
}

[Fact]
public async Task ChargeAsync_SetsCorrectHeaders()
{
    var capturer = new CapturingFakeHandler(
        new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""{"success":true,"transactionId":"txn_999"}""",
                Encoding.UTF8, "application/json")
        });

    var client = new HttpClient(capturer) { BaseAddress = new Uri("https://api.payment.com/") };
    var service = new PaymentService(client);

    await service.ChargeAsync(99.99m, "tok_test");

    Assert.NotNull(capturer.CapturedRequest);
    Assert.Equal(HttpMethod.Post, capturer.CapturedRequest!.Method);
    Assert.Equal("https://api.payment.com/charge", capturer.CapturedRequest.RequestUri?.ToString());
}
```

---

## Part 11: Real-World Patterns and Case Studies

### 11.1 Case Study: The E-Commerce Platform That Nearly Melted Down

Consider a fictional but representative scenario. An e-commerce company is running an ASP.NET Core 3.1 application that calls four external services: an inventory API, a pricing API, a payment gateway, and a shipping rate calculator. The team of four developers built the application as a startup — fast, pragmatic, and functional.

The original code across multiple controllers and services:

```csharp
// Across various controllers and services...
using var client = new HttpClient();
client.BaseAddress = new Uri("https://api.inventory.example.com/");
var stock = await client.GetFromJsonAsync<StockLevel>($"products/{sku}/stock");
```

This worked perfectly in development, passed all QA tests (which ran at low load), and sailed through the staging environment. The day the platform launched a major sale and traffic jumped from 50 requests/second to 2,000 requests/second, everything fell apart.

Within eight minutes of the sale starting, socket exhaustion had consumed all available ephemeral ports. Every outbound HTTP call — inventory, pricing, payments, shipping — was failing with `SocketException`. The application could not even connect to SQL Server because the database connection attempts also needed sockets. The site went down completely.

The post-mortem identified the root cause immediately (the socket exhaustion pattern) and the fix was applied within 30 minutes — converting to `IHttpClientFactory` with named clients. The deployment went out, and the second sale two weeks later handled 3x the traffic without incident.

The fix:

```csharp
// Program.cs — after the fix
builder.Services.AddHttpClient("inventory", c =>
{
    c.BaseAddress = new Uri("https://api.inventory.example.com/");
    c.Timeout = TimeSpan.FromSeconds(5);
})
.AddStandardResilienceHandler();

builder.Services.AddHttpClient("pricing", c =>
{
    c.BaseAddress = new Uri("https://api.pricing.example.com/");
    c.Timeout = TimeSpan.FromSeconds(3);
})
.AddStandardResilienceHandler();

builder.Services.AddHttpClient("payments", c =>
{
    c.BaseAddress = new Uri("https://api.payments.example.com/");
    c.Timeout = TimeSpan.FromSeconds(30);
})
.AddResilienceHandler("payments", pipeline =>
{
    // Only retry on network errors, not HTTP errors — payments must not be double-charged
    pipeline.AddRetry(new HttpRetryStrategyOptions
    {
        MaxRetryAttempts = 1,
        ShouldHandle = args => ValueTask.FromResult(args.Outcome.Exception is HttpRequestException)
    });
    pipeline.AddTimeout(new HttpTimeoutStrategyOptions { Timeout = TimeSpan.FromSeconds(15) });
});

builder.Services.AddHttpClient("shipping", c =>
{
    c.BaseAddress = new Uri("https://api.shipping-rates.example.com/");
    c.Timeout = TimeSpan.FromSeconds(10);
})
.AddStandardResilienceHandler();
```

### 11.2 Case Study: The Kubernetes Deployment That Kept Talking to Dead Pods

A financial services team had a .NET 6 application correctly using a singleton `HttpClient` (having learned about socket exhaustion) calling an internal account-balance service:

```csharp
// Singleton registered in DI
public class AccountBalanceService
{
    private static readonly HttpClient Client = new()
    {
        BaseAddress = new Uri("http://balance-service.finance.svc.cluster.local/")
    };

    public async Task<decimal> GetBalanceAsync(string accountId) =>
        await Client.GetFromJsonAsync<decimal>($"accounts/{accountId}/balance");
}
```

The balance service was hosted in Kubernetes. When the team deployed a new version of the balance service, Kubernetes rolled out new pods. The old pods were terminated after a grace period. During the next few minutes of rolling deployment, the singleton `HttpClient` — which had cached the old pods' IP addresses — kept sending requests to the terminated pods. Requests failed intermittently for 2–3 minutes during every deployment.

The fix: replace the singleton pattern with `IHttpClientFactory` with the default 2-minute handler lifetime, or alternatively, use `SocketsHttpHandler` with a `PooledConnectionLifetime` of 1 minute:

```csharp
// Option A: IHttpClientFactory with 1-minute handler lifetime
builder.Services.AddHttpClient<IAccountBalanceService, AccountBalanceService>(c =>
{
    c.BaseAddress = new Uri("http://balance-service.finance.svc.cluster.local/");
})
.SetHandlerLifetime(TimeSpan.FromMinutes(1)); // Refresh DNS every minute

// Option B: SocketsHttpHandler with PooledConnectionLifetime (no DI required)
var handler = new SocketsHttpHandler
{
    PooledConnectionLifetime = TimeSpan.FromMinutes(1)
};
var sharedClient = new HttpClient(handler)
{
    BaseAddress = new Uri("http://balance-service.finance.svc.cluster.local/")
};
// Register sharedClient as singleton
builder.Services.AddSingleton(sharedClient);
```

With a 1-minute connection lifetime, connections are recycled roughly every minute. Kubernetes rolling deployments take 2–3 minutes total. The window of stale DNS is now much shorter, and the occasional failed request during the brief transition is handled by the resilience handler's retry policy.

### 11.3 The Cookie Sharing Gotcha

`IHttpClientFactory` has one well-documented gotcha with cookies. Because the factory pools `HttpMessageHandler` instances and reuses them across multiple `HttpClient` instances, the `CookieContainer` inside the `HttpClientHandler` is also shared. If you're calling an API that uses cookies for authentication or session tracking, you may find that cookies from one request "bleed" into another request in a different user's context.

The Microsoft documentation explicitly states: "If your app requires cookies, it's recommended to avoid using `IHttpClientFactory`."

If you genuinely need per-user cookie containers, you have two options:

**Option 1: Disable cookies in the factory-managed handler and handle cookies manually**:
```csharp
builder.Services.AddHttpClient<IMyService, MyService>()
    .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
    {
        UseCookies = false // Disable cookie container entirely
    });

// Then manually set Cookie headers in your requests
request.Headers.Add("Cookie", "sessionId=abc123");
```

**Option 2: Use a new `HttpClient` with a per-request cookie container** (sacrificing the pooling benefits for this specific use case):
```csharp
var cookieContainer = new CookieContainer();
var handler = new HttpClientHandler { CookieContainer = cookieContainer };
using var client = new HttpClient(handler);
// Use for this session, then dispose — cookie container is per-session, not per-pool
```

### 11.4 Configuring Primary Handlers for Certificates and Proxies

Two common enterprise scenarios — custom certificate validation and HTTP proxies — require configuring the primary handler:

```csharp
// Custom certificate validation (use with extreme caution in production)
builder.Services.AddHttpClient<IInternalService, InternalService>()
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
    {
        // Accept a self-signed certificate from an internal service
        // ⚠️ This bypasses TLS validation — only use for internal trusted services
        ServerCertificateCustomValidationCallback = (_, cert, _, _) =>
        {
            // Validate against a known thumbprint instead of bypassing entirely
            return cert?.GetCertHashString() == "EXPECTED_THUMBPRINT_HEX";
        }
    });

// Corporate HTTP proxy
builder.Services.AddHttpClient<IExternalService, ExternalService>()
    .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
    {
        Proxy = new WebProxy("http://proxy.corp.example.com:8080")
        {
            Credentials = new NetworkCredential("proxyuser", "proxypassword")
        },
        UseProxy = true
    });
```

---

## Part 12: The Broader Ecosystem — Refit, gRPC, and Beyond

### 12.1 Refit — Declarative HTTP Clients

Refit is an open-source library that turns REST API definitions written as C# interfaces into live, type-safe HTTP clients. Instead of writing implementation code, you define the API contract:

```csharp
// Install: dotnet add package Refit.HttpClientFactory
using Refit;

// Define the API as an interface with Refit attributes
public interface IGitHubApi
{
    [Get("/users/{username}")]
    Task<GitHubUser> GetUserAsync(string username);

    [Get("/users/{username}/repos")]
    Task<IEnumerable<GitHubRepo>> GetReposAsync(string username);

    [Post("/user/repos")]
    Task<GitHubRepo> CreateRepoAsync([Body] CreateRepoRequest request);
}
```

Register with `IHttpClientFactory`:

```csharp
builder.Services.AddRefitClient<IGitHubApi>()
    .ConfigureHttpClient(c =>
    {
        c.BaseAddress = new Uri("https://api.github.com/");
        c.DefaultRequestHeaders.Add("Accept", "application/vnd.github.v3+json");
        c.DefaultRequestHeaders.Add("User-Agent", "ObserverMagazineApp/1.0");
    })
    .AddStandardResilienceHandler();
```

Inject and use:

```csharp
public class GitHubController : ControllerBase
{
    private readonly IGitHubApi _github;

    public GitHubController(IGitHubApi github)
    {
        _github = github;
    }

    [HttpGet("{username}")]
    public async Task<IActionResult> GetUser(string username)
    {
        var user = await _github.GetUserAsync(username);
        return Ok(user);
    }
}
```

Refit generates the implementation at compile time (via source generators in modern versions). It is elegant and significantly reduces boilerplate. The handler lifecycle is managed by `IHttpClientFactory` exactly as with manually written typed clients.

### 12.2 gRPC — HttpClient Under the Hood

gRPC, the high-performance remote procedure call framework developed by Google, uses HTTP/2 as its transport in .NET. The `Grpc.Net.Client` NuGet package is the .NET gRPC client, and it uses `HttpClient` under the hood. The `Grpc.Net.ClientFactory` package integrates gRPC channels with `IHttpClientFactory` for the same lifecycle benefits:

```bash
dotnet add package Grpc.Net.ClientFactory
```

```csharp
// Program.cs — register a gRPC client via IHttpClientFactory
builder.Services.AddGrpcClient<Greeter.GreeterClient>(options =>
{
    options.Address = new Uri("https://localhost:5001");
})
.AddStandardResilienceHandler();
```

The gRPC client is managed by the factory like any other typed client, including handler pooling, lifetime management, and resilience integration.

### 12.3 HttpClient in Minimal APIs

In .NET 6+'s minimal API model, typed clients can be injected directly into route handler parameters:

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient<IGitHubService, GitHubService>(client =>
{
    client.BaseAddress = new Uri("https://api.github.com/");
    client.DefaultRequestHeaders.Add("Accept", "application/vnd.github.v3+json");
    client.DefaultRequestHeaders.Add("User-Agent", "ObserverMagazineApp/1.0");
});

var app = builder.Build();

app.MapGet("/github/{username}", async (string username, IGitHubService github) =>
{
    var user = await github.GetUserAsync(username);
    return user is null ? Results.NotFound() : Results.Ok(user);
});

app.Run();
```

`IHttpClientFactory` works identically in minimal APIs and controller-based APIs.

### 12.4 HttpClient in Background Services (IHostedService / BackgroundService)

Background services — long-running tasks that run alongside the web server — often need to make HTTP calls. The pattern here requires care because background services are registered as singletons, yet typed clients are transient.

```csharp
// ✅ Background service that correctly uses IHttpClientFactory
public class DataSyncService : BackgroundService
{
    private readonly IHttpClientFactory _factory;
    private readonly ILogger<DataSyncService> _logger;

    // Inject IHttpClientFactory (a singleton), not HttpClient or a typed client
    public DataSyncService(IHttpClientFactory factory, ILogger<DataSyncService> logger)
    {
        _factory = factory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await SyncDataAsync(stoppingToken);
            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        }
    }

    private async Task SyncDataAsync(CancellationToken ct)
    {
        try
        {
            // Create a fresh client from the factory each time — handler is pooled
            var client = _factory.CreateClient("datasync");
            var data = await client.GetFromJsonAsync<DataBatch>("sync/pending", ct);
            _logger.LogInformation("Synced {Count} records", data?.Count ?? 0);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Data sync failed");
        }
    }
}

// Registration
builder.Services.AddHttpClient("datasync", client =>
{
    client.BaseAddress = new Uri("https://api.datasource.example.com/");
});
builder.Services.AddHostedService<DataSyncService>();
```

The background service is singleton. The `IHttpClientFactory` is singleton. The `HttpClient` returned by `CreateClient` is transient — created fresh for each sync cycle, but backed by a pooled handler. This is the correct pattern.

---

## Part 13: Common Pitfalls and How to Avoid Every One of Them

### 13.1 Pitfall: Capturing Typed Clients in Singletons

Already discussed in detail in Part 6. The short version: if your typed client (`GitHubService`, `PaymentService`) is injected into a singleton (`IHostedService`, a static object, a singleton service), the `HttpClient` inside it is captured for the singleton's lifetime, defeating handler rotation and reintroducing DNS staleness.

**Solution**: Inject `IHttpClientFactory` into singletons and call `CreateClient()` per operation.

### 13.2 Pitfall: Registering the Typed Client Twice

```csharp
// ❌ This breaks the IHttpClientFactory link
builder.Services.AddHttpClient<IGitHubService, GitHubService>(client =>
{
    client.BaseAddress = new Uri("https://api.github.com/");
});

// This second registration OVERWRITES the IHttpClientFactory registration
// GitHubService will now be created by the DI container directly,
// without going through the factory — and will get an unconfigured HttpClient
builder.Services.AddTransient<IGitHubService, GitHubService>(); // ❌ DO NOT DO THIS
```

**Solution**: Register typed clients only via `AddHttpClient<>`. Do not additionally register the implementation type with `AddTransient`, `AddScoped`, or `AddSingleton`.

### 13.3 Pitfall: Registering Multiple Typed Clients on One Interface

```csharp
// ❌ Problematic — both share the same named client "string.Empty"
builder.Services.AddHttpClient<IGitHubService, GitHubService>();
builder.Services.AddHttpClient<IGitHubService, GitHubMirrorService>();

// When something injects IGitHubService, it gets the last registered implementation
// AND the HttpClient configuration from the last AddHttpClient call applies to both
```

**Solution**: Use distinct names for each typed client registration:

```csharp
// ✅ Explicit names disambiguate the registrations
builder.Services.AddHttpClient<IGitHubService, GitHubService>("primary-github", client =>
{
    client.BaseAddress = new Uri("https://api.github.com/");
});

builder.Services.AddHttpClient<IGitHubService, GitHubMirrorService>("mirror-github", client =>
{
    client.BaseAddress = new Uri("https://api-mirror.github.com/");
});
```

### 13.4 Pitfall: Not Handling Cancellation

All `HttpClient` methods accept a `CancellationToken`. If you don't pass cancellation tokens, your outbound HTTP calls will continue even when the caller has cancelled (e.g., the user cancelled their browser request):

```csharp
// ❌ No cancellation token — continues even when request is cancelled
public async Task<WeatherData?> GetWeatherAsync()
{
    return await _client.GetFromJsonAsync<WeatherData>("current");
}

// ✅ Pass the CancellationToken throughout
public async Task<WeatherData?> GetWeatherAsync(CancellationToken ct = default)
{
    return await _client.GetFromJsonAsync<WeatherData>("current", ct);
}
```

In ASP.NET Core controllers and minimal APIs, the `CancellationToken` is automatically provided:

```csharp
// Controller action
public async Task<IActionResult> Get(CancellationToken ct)
{
    var data = await _weatherService.GetWeatherAsync(ct); // ✅
    return Ok(data);
}

// Minimal API
app.MapGet("/weather", async (IWeatherService weather, CancellationToken ct) =>
{
    return await weather.GetWeatherAsync(ct); // ✅
});
```

### 13.5 Pitfall: Not Setting Timeouts

`HttpClient.Timeout` defaults to 100 seconds. Without a shorter timeout, a slow external service can hold your threads for over a minute and a half, causing cascading failures across your application as the thread pool is exhausted by requests waiting for responses that may never come.

Always set a timeout appropriate for the service:

```csharp
builder.Services.AddHttpClient<IWeatherService, WeatherService>(client =>
{
    client.BaseAddress = new Uri("https://api.weather.example.com/");
    client.Timeout = TimeSpan.FromSeconds(10); // Never wait more than 10 seconds
});
```

And remember that `HttpClient.Timeout` fires a `TaskCanceledException` (whose `CancellationToken.IsCancellationRequested` is false — this is a historical quirk that was improved in .NET 5+ where you can distinguish between user cancellation and timeout via `ex.InnerException is TimeoutException`).

### 13.6 Pitfall: Using HttpClient in Blazor WASM

If you are building a Blazor WebAssembly application (as Observer Magazine itself is), `HttpClient` works differently. In the browser environment, HTTP calls go through the browser's `fetch` API via JavaScript interop. The `SocketsHttpHandler` is not available — the browser's native fetch handles the actual networking.

`IHttpClientFactory` is still supported and recommended in Blazor WASM, but the handler is `BrowserHttpHandler` under the hood, not `SocketsHttpHandler`. Socket exhaustion is not a concern (the browser manages connections), but named/typed clients are still valuable for configuration and DI cleanliness:

```csharp
// Program.cs in Blazor WASM
var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");

// Register a typed client — works in Blazor WASM too
builder.Services.AddHttpClient<IBlogService, BlogService>(client =>
{
    client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress);
});
```

### 13.7 Pitfall: Sharing HttpClient Across Tests

`HttpClient` and `IHttpClientFactory` in test code require care:

```csharp
// ❌ A shared static HttpClient in tests can cause interference between test runs
private static readonly HttpClient _client = new();

// ✅ Create a fresh HttpClient per test, or use WebApplicationFactory
[Fact]
public async Task TestA()
{
    var handler = new FakeHttpMessageHandler(/*...*/);
    var client = new HttpClient(handler) { BaseAddress = new Uri("https://test.local/") };
    // Use client — it's local to this test
}
```

### 13.8 Pitfall: Ignoring the Response Body

If you read the response headers (e.g., to check the status code) but don't consume the response body, you may leave the HTTP connection in a bad state and prevent it from being returned to the pool:

```csharp
// ❌ Body not consumed — connection may not be returned to pool
var response = await client.GetAsync("endpoint");
if (response.IsSuccessStatusCode)
{
    return true;
}
return false;
// Body is never read — connection is not cleanly returned

// ✅ Always consume or dispose the response
var response = await client.GetAsync("endpoint");
_ = await response.Content.ReadAsStringAsync(); // Consume even if not used
return response.IsSuccessStatusCode;

// Or use using:
using var response = await client.GetAsync("endpoint");
return response.IsSuccessStatusCode;
// Disposing HttpResponseMessage also disposes content
```

### 13.9 Pitfall: Not Respecting Retry-After Headers

When a server returns `429 Too Many Requests` or `503 Service Unavailable`, it often includes a `Retry-After` header indicating how long to wait before retrying. Ignoring this header and retrying immediately is disrespectful to the server and will likely result in your client being blocked or rate-limited more aggressively.

`Microsoft.Extensions.Http.Resilience`'s `HttpRetryStrategyOptions` handles the `Retry-After` header automatically by default. If you're using raw Polly, you need to implement this yourself:

```csharp
pipeline.AddRetry(new HttpRetryStrategyOptions
{
    ShouldHandle = args => ValueTask.FromResult(
        args.Outcome.Result?.StatusCode is HttpStatusCode.TooManyRequests
    ),
    DelayGenerator = args =>
    {
        // Honor Retry-After header if present
        if (args.Outcome.Result?.Headers.RetryAfter?.Delta is { } retryAfter)
        {
            return ValueTask.FromResult<TimeSpan?>(retryAfter);
        }
        return ValueTask.FromResult<TimeSpan?>(TimeSpan.FromSeconds(2));
    }
});
```

---

## Part 14: HttpClientFactory in .NET Framework 4.8 vs. .NET 10 — Side by Side

To crystallize everything covered in this guide, here is a comprehensive side-by-side comparison across the full .NET ecosystem:

### 14.1 Registration

| Aspect | .NET Framework 4.8 | ASP.NET Core (.NET 8/10) |
|---|---|---|
| Package | `Microsoft.Extensions.Http` (NuGet) | Included in `Microsoft.AspNetCore.App` |
| Registration | `services.AddHttpClient()` in a manually built `IServiceCollection` | `builder.Services.AddHttpClient()` |
| DI container | Add `Microsoft.Extensions.DependencyInjection` to get one | Built-in, always available |
| Startup location | `Global.asax.cs` → `Application_Start` | `Program.cs` |

### 14.2 Handler Internals

| Aspect | .NET Framework 4.8 | .NET 8/10 |
|---|---|---|
| Default primary handler | `HttpClientHandler` → WinHTTP | `SocketsHttpHandler` (managed, cross-platform) |
| `PooledConnectionLifetime` | Not available on `HttpClientHandler` | Available on `SocketsHttpHandler` |
| HTTP/2 support | Limited (via WinHTTP, Windows 11/Server 2022+) | Full, cross-platform |
| HTTP/3 | Not supported | Supported (.NET 6+ preview, stable .NET 8+) |

### 14.3 Resilience

| Aspect | .NET Framework 4.8 | .NET 8/10 |
|---|---|---|
| Polly v8 | Supported (`netstandard2.0`) | Fully supported |
| `Microsoft.Extensions.Http.Resilience` | Supported (`net462+`) | Fully supported |
| `AddStandardResilienceHandler()` | Available via NuGet | Available via NuGet or `Microsoft.AspNetCore.App` |

### 14.4 Complete Configuration — The Full Modern Example

Here is a comprehensive, production-ready `Program.cs` for an ASP.NET Core .NET 10 application with multiple typed clients, resilience, observability, and correct lifetime management:

```csharp
using Microsoft.Extensions.Http.Resilience;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Polly;

var builder = WebApplication.CreateBuilder(args);

// ─────────────────────────────────────────────────
// Observability — OpenTelemetry
// ─────────────────────────────────────────────────
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing =>
    {
        tracing
            .SetResourceBuilder(ResourceBuilder.CreateDefault()
                .AddService("ObserverMagazine", serviceVersion: "1.0.0"))
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation() // ← traces all outbound HttpClient calls
            .AddOtlpExporter();
    });

// ─────────────────────────────────────────────────
// GitHub API — typed client with resilience
// ─────────────────────────────────────────────────
builder.Services.AddHttpClient<IGitHubService, GitHubService>(client =>
{
    client.BaseAddress = new Uri("https://api.github.com/");
    client.DefaultRequestHeaders.Add("Accept", "application/vnd.github.v3+json");
    client.DefaultRequestHeaders.Add("User-Agent", "ObserverMagazine/1.0");
    client.Timeout = TimeSpan.FromSeconds(30);
})
.SetHandlerLifetime(TimeSpan.FromMinutes(5))
.AddStandardResilienceHandler(options =>
{
    options.Retry.MaxRetryAttempts = 3;
    options.Retry.UseJitter = true;
    options.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(30);
});

// ─────────────────────────────────────────────────
// Payment gateway — stricter resilience (no retries on non-network errors)
// ─────────────────────────────────────────────────
builder.Services.AddHttpClient<IPaymentService, PaymentService>(client =>
{
    client.BaseAddress = new Uri(
        builder.Configuration["Services:PaymentsBaseUrl"]
        ?? throw new InvalidOperationException("PaymentsBaseUrl not configured"));
    client.Timeout = TimeSpan.FromSeconds(60);
})
.AddResilienceHandler("payments", pipeline =>
{
    pipeline.AddRetry(new HttpRetryStrategyOptions
    {
        MaxRetryAttempts = 1,
        ShouldHandle = args =>
            ValueTask.FromResult(args.Outcome.Exception is HttpRequestException)
    });
    pipeline.AddTimeout(new HttpTimeoutStrategyOptions
    {
        Timeout = TimeSpan.FromSeconds(20)
    });
    pipeline.AddCircuitBreaker(new HttpCircuitBreakerStrategyOptions
    {
        FailureRatio = 0.3,
        SamplingDuration = TimeSpan.FromSeconds(30),
        MinimumThroughput = 5,
        BreakDuration = TimeSpan.FromSeconds(60)
    });
});

// ─────────────────────────────────────────────────
// Weather API — named client (shared by multiple consumers)
// ─────────────────────────────────────────────────
builder.Services.AddHttpClient("weather", client =>
{
    client.BaseAddress = new Uri("https://api.weather.example.com/");
    client.DefaultRequestHeaders.Add("X-API-Key",
        builder.Configuration["ApiKeys:Weather"]
        ?? throw new InvalidOperationException("Weather API key not configured"));
    client.Timeout = TimeSpan.FromSeconds(5); // Weather should be fast or skipped
})
.AddStandardResilienceHandler();

// ─────────────────────────────────────────────────
// Controllers, Swagger, etc.
// ─────────────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
```

---

## Part 15: Checklist — What to Verify in Your Codebase Right Now

Walk through your codebase with this checklist. If any item is true, you have a bug or a risk:

**Socket Exhaustion Risks:**
- [ ] Are there any `new HttpClient()` calls in controllers, services, or handlers that are called per-request?
- [ ] Are there any `using var client = new HttpClient()` patterns inside method bodies that are called frequently?
- [ ] Are there `HttpClient` instances created in `foreach` loops or iterators?

**DNS Staleness Risks:**
- [ ] Is any `HttpClient` instance stored as a `static` field without `SocketsHttpHandler.PooledConnectionLifetime` configured?
- [ ] Is any typed client (class that takes `HttpClient` in constructor) injected into a singleton service?
- [ ] Is `IHttpClientFactory.CreateClient()` called once and the result stored in a long-lived field?

**Resilience Gaps:**
- [ ] Are there HTTP calls to external services with no timeout configured? (Remember: default is 100 seconds)
- [ ] Are there HTTP calls with no retry policy for transient failures?
- [ ] Are there high-throughput paths with no circuit breaker?

**Lifecycle Issues:**
- [ ] Are typed clients registered twice (once via `AddHttpClient<T>` and once via `AddTransient<T>`)?
- [ ] Are multiple typed clients registered against the same interface without explicit names?

**Testing:**
- [ ] Can all HTTP calls be replaced with fakes in unit tests?
- [ ] Do integration tests that exercise HTTP client code actually isolate the network calls?

---

## Conclusion — The Architecture Has Always Been There

The problems of socket exhaustion and DNS staleness are old problems with well-understood solutions. `IHttpClientFactory` is not a new idea — it is a formalization and industrialization of the same insight that drove database connection pooling decades earlier: expensive resources should be pooled and managed centrally, not created and destroyed per-operation.

The analogy runs deep. Just as you don't open a database connection for every SQL query and close it immediately after (even though `SqlConnection` is disposable and even though you're encouraged to use `using`), you don't create an `HttpClient` for every HTTP request. And just as the connection pool handles the cleanup, reconnection, and recycling of database connections invisibly, `IHttpClientFactory` handles the same for HTTP message handlers.

The journey from `new HttpClient()` per request → singleton `HttpClient` → `IHttpClientFactory` with typed clients mirrors the maturation of the .NET platform itself. Each step solved a real production problem. Each step is documented in the scars of real outages.

Today, in .NET 10, the tools are excellent. `IHttpClientFactory` with typed clients, `AddStandardResilienceHandler`, OpenTelemetry instrumentation, and `SocketsHttpHandler` with HTTP/2 and HTTP/3 support represent a genuinely world-class HTTP client stack. There is no excuse for socket exhaustion in a modern .NET application.

Start with typed clients. Add a resilience handler. Set your timeouts. Pass your cancellation tokens. Run `netstat` under load and confirm that your TIME_WAIT count is near zero. Sleep soundly knowing that when your next traffic spike arrives — sale day, viral moment, marketing campaign — your HTTP connections will not be the thing that brings the house down.

---

## Resources

**Official Microsoft Documentation:**
- [IHttpClientFactory with .NET](https://learn.microsoft.com/en-us/dotnet/core/extensions/httpclient-factory) — Primary reference for all patterns
- [Make HTTP requests using IHttpClientFactory in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/http-requests) — ASP.NET Core specific guide
- [HttpClient guidelines for .NET](https://learn.microsoft.com/en-us/dotnet/fundamentals/networking/http/httpclient-guidelines) — SocketsHttpHandler alternative approach
- [Troubleshoot IHttpClientFactory issues](https://learn.microsoft.com/en-us/dotnet/core/extensions/httpclient-factory-troubleshooting) — Common pitfall patterns with solutions
- [Build resilient HTTP apps](https://learn.microsoft.com/en-us/dotnet/core/resilience/http-resilience) — Microsoft.Extensions.Http.Resilience guide

**Polly:**
- [Polly GitHub repository](https://github.com/App-vNext/Polly) — Source, docs, and examples
- [Polly documentation site](https://www.pollydocs.org/) — Strategy reference
- [Building resilient cloud services with .NET 8](https://devblogs.microsoft.com/dotnet/building-resilient-cloud-services-with-dotnet-8/) — .NET Blog announcement of Microsoft.Extensions.Http.Resilience

**NuGet Packages:**
- [Microsoft.Extensions.Http](https://www.nuget.org/packages/Microsoft.Extensions.Http) — `IHttpClientFactory` core package
- [Microsoft.Extensions.Http.Resilience](https://www.nuget.org/packages/Microsoft.Extensions.Http.Resilience) — Polly-based resilience for HttpClient
- [Refit.HttpClientFactory](https://www.nuget.org/packages/Refit.HttpClientFactory) — Declarative REST clients

**Source Code and Deeper Dives:**
- [Exploring the code behind IHttpClientFactory](https://andrewlock.net/exporing-the-code-behind-ihttpclientfactory/) — Andrew Lock's deep dive into `DefaultHttpClientFactory` internals
- [dotnet/runtime on GitHub](https://github.com/dotnet/runtime/tree/main/src/libraries/Microsoft.Extensions.Http) — The actual source code of `IHttpClientFactory`
- [You're using HttpClient wrong](https://www.aspnetmonsters.com/2016/08/2016-08-27-httpclientwrong/) — The 2016 post by Simon Timms that first brought widespread attention to socket exhaustion

**TCP and Networking Reference:**
- [TCP TIME-WAIT in RFC 9293](https://www.rfc-editor.org/rfc/rfc9293) — The TCP specification that defines TIME_WAIT behavior
- [Marc Brooker: Jitter — Making Things Better With Randomness](https://brooker.co.za/blog/2015/03/21/backoff.html) — The research behind retry jitter
