---
title: "The ASP.NET Request Lifecycle: Why Cold Starts Are Slow and How .NET 10 Changes Everything"
date: 2026-03-21
author: myblazor-team
summary: A deep dive into the ASP.NET request lifecycle across both .NET Framework and modern .NET 10, explaining why cold starts have historically been slow, what you can do about it, and how Native AOT and other advances have fundamentally changed the equation.
tags:
  - dotnet
  - aspnet
  - performance
  - lifecycle
  - aot
  - tutorial
---

## Introduction

If you have ever deployed an ASP.NET application and noticed that the very first request takes seconds — sometimes tens of seconds — while subsequent requests are blazing fast, you have experienced the infamous "cold start" problem. This post breaks down the entire ASP.NET request lifecycle, explains where that cold start time goes, and shows how modern .NET (up through .NET 10) has systematically attacked this problem from every angle.

## Part 1: The Classic ASP.NET Framework Request Lifecycle

To understand why cold starts are slow, you first need to understand what happens when a request arrives at an ASP.NET Framework application running on IIS.

### The IIS Pipeline

When IIS receives an HTTP request, it goes through a series of stages before your code ever runs. In Integrated Pipeline Mode (the default since IIS 7), the request flows through a unified pipeline of native IIS modules and managed ASP.NET modules. The key stages are:

**BeginRequest** is where the pipeline starts. IIS determines which application pool should handle the request and routes it accordingly. If the application pool's worker process (w3wp.exe) is not running — because the pool was recycled or the app was idle — IIS must spin up an entirely new process. This is the first major source of cold start latency.

**AuthenticateRequest and AuthorizeRequest** handle identity and permissions. These stages load authentication modules (Windows Auth, Forms Auth, etc.) and can involve talking to Active Directory or a database.

**ResolveRequestCache** checks whether a cached response exists. On a cold start, the cache is empty, so this is a no-op that adds no benefit.

**MapRequestHandler** determines which handler processes the request. For MVC, this involves the routing engine matching a URL pattern to a controller and action. For Web Forms, this maps to a .aspx page handler.

**ExecuteRequestHandler** is where your actual application code runs — your controller action, your page lifecycle, your business logic. On a cold start, this is where the bulk of the delay happens because of JIT compilation and dependency initialization (more on this below).

**UpdateRequestCache** stores the response for future cache hits.

**EndRequest** performs cleanup and sends the response.

### The ASP.NET Page Lifecycle (Web Forms)

If your application uses Web Forms, the ExecuteRequestHandler stage triggers a complex page lifecycle of its own: Init, LoadViewState, Load, PostBack event handling, PreRender, SaveViewState, Render, and Unload. Each of these stages can involve control tree construction, viewstate deserialization, and dynamic compilation of .aspx and .ascx files. On the first request, every page and user control must be compiled from markup into a .NET class, compiled to IL, and then JIT-compiled to native code. This is why a complex Web Forms application can take minutes on its very first request.

### The ASP.NET MVC Lifecycle

MVC applications are leaner but still go through significant work on cold start. Routing tables must be built from your RouteConfig (or attribute routes). Controller factories and dependency injection containers must be constructed. The Razor view engine must locate, parse, compile, and JIT-compile every .cshtml file the first time it is accessed. Area registrations, filter providers, model binders, and value providers all need initialization.

## Part 2: Why Is the Cold Start So Slow in .NET Framework?

The cold start slowness in classic .NET Framework comes from several compounding factors.

### 1. JIT Compilation

.NET Framework applications ship as Intermediate Language (IL) bytecode. When a method is called for the first time, the CLR's Just-In-Time compiler translates it to native machine code. This happens method-by-method, on demand. On a cold start, virtually every method in your application's startup path must be JIT-compiled: your Global.asax, your DI container setup, your routing configuration, your first controller, your first Razor view, and every framework method those call into. For a large application with hundreds of types, this can take seconds of raw CPU time.

### 2. Assembly Loading

The CLR must locate and load assemblies from disk. .NET Framework applications often have dozens of DLLs in their bin folder — your code, NuGet packages, framework libraries. Each DLL must be found on disk, read into memory, and have its metadata parsed. On a traditional spinning hard drive (still common in older server environments), this I/O alone can add hundreds of milliseconds. Even on SSDs, loading 50-100 assemblies sequentially adds up.

### 3. IIS Application Pool Recycling

By default, IIS recycles application pools every 1740 minutes (29 hours) and shuts them down after 20 minutes of inactivity. When a pool recycles, the next request must go through the entire cold start sequence again: process creation, CLR initialization, assembly loading, JIT compilation, and application initialization. This means users regularly experience cold starts, not just after deployments.

### 4. Dynamic Compilation of Views

In ASP.NET MVC on .NET Framework, Razor views (.cshtml files) are compiled at runtime by default. The Razor engine reads the file from disk, parses it into C# code, compiles the generated C# to IL, and then the CLR JIT-compiles it to native code. For an application with hundreds of views, this cascade of disk reads, parsing, and compilation is brutally slow on first access.

### 5. Heavy Initialization in Global.asax

Classic ASP.NET applications perform massive amounts of work in Application_Start: registering routes, configuring dependency injection, setting up Entity Framework models, loading configuration, initializing logging frameworks, building AutoMapper profiles, and more. All of this runs synchronously before the first request can be served. A complex enterprise application might spend 5-30 seconds in Application_Start alone.

### 6. Entity Framework Model Compilation

Entity Framework (especially versions 4 through 6) must build an in-memory model of your entire database schema the first time a DbContext is used. For large schemas with hundreds of tables and complex relationships, this model compilation can take several seconds. Combined with JIT compilation of EF's own code, the first database query often takes 10-50x longer than subsequent queries.

## Part 3: Mitigations for .NET Framework Cold Starts

Developers have historically used several strategies to reduce cold start pain on .NET Framework.

### Pre-compilation

The `aspnet_compiler.exe` tool can pre-compile all views and pages at build time rather than at runtime. Combined with `aspnet_merge.exe` (which merges the resulting assemblies into a smaller number of DLLs), this eliminates runtime view compilation entirely. You can enable this in MSBuild with `/p:PrecompileBeforePublish=true /p:UseMerge=true`.

### NGen (Native Image Generator)

Running `ngen install` on your assemblies produces native images that bypass JIT compilation. The CLR loads the pre-compiled native code directly instead of JIT-compiling IL. However, NGen images are machine-specific, fragile (they're invalidated when dependencies change), and don't benefit from runtime profile-guided optimization. Still, for cold starts, NGen can reduce startup time by 30-60%.

### IIS Application Initialization Module

The IIS Application Initialization module (available since IIS 8) sends a synthetic request to your application immediately when the app pool starts, rather than waiting for the first real user request. Combined with the "AlwaysRunning" start mode for the application pool, this ensures the cold start happens in the background before any user is affected.

### Reducing Idle Timeout and Recycling Frequency

Setting the IIS idle timeout to 0 (never timeout) and extending or disabling periodic recycling prevents the application from shutting down between requests. This trades memory for availability.

### Warm-up Scripts

Many teams write HTTP health-check scripts that hit key endpoints after deployment, forcing JIT compilation and cache population before real traffic arrives. This is a brute-force approach but effective.

### Pre-building Singletons

Instead of lazily constructing singletons during the first request, you can eagerly resolve all registered singleton services during startup. This front-loads the DI container work so the first real request does not pay the price.

## Part 4: The Modern .NET Lifecycle (.NET 6 through .NET 10)

Modern .NET (the cross-platform runtime, not .NET Framework) has fundamentally restructured the application lifecycle. Understanding the differences helps explain why cold starts are dramatically better.

### The Minimal Hosting Model

Starting with .NET 6 and refined through .NET 10, the application entry point is a simple `Program.cs` with a `WebApplicationBuilder`. There is no more Global.asax, no Startup class split into ConfigureServices and Configure, no complex lifecycle of OWIN middleware registration. The pipeline is built declaratively:

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRazorPages();

var app = builder.Build();
app.UseRouting();
app.MapRazorPages();
app.Run();
```

This minimal model does less work at startup because the framework itself is more modular. You only pay for what you use.

### Kestrel Instead of IIS

Modern ASP.NET Core applications run on Kestrel, a lightweight, cross-platform HTTP server written from scratch for performance. Kestrel does not have IIS's application pool recycling behavior, idle timeouts, or heavy process management overhead. When deployed behind a reverse proxy (NGINX, YARP, or even IIS as a reverse proxy via ANCM), the application process stays alive continuously.

### Razor View Compilation at Build Time

Since .NET Core 3.0, Razor views and pages are compiled at build time by default. The `Microsoft.NET.Sdk.Razor` SDK compiles .cshtml files into C# classes and then into IL during `dotnet build`, not at runtime. This completely eliminates the runtime view compilation that plagued .NET Framework.

### Tiered Compilation

Introduced in .NET Core 3.0 and enabled by default since, Tiered Compilation replaces the single-pass JIT with a two-tier approach. Tier 0 ("Quick JIT") compiles methods very fast but produces lower-quality code. After a method has been called enough times, the runtime recompiles it at Tier 1 with full optimizations. The result: methods are available almost instantly on first call (much faster than the old full-optimization JIT), and hot methods eventually reach peak performance. For cold starts, Tiered Compilation dramatically reduces the time spent in JIT.

### ReadyToRun (R2R)

ReadyToRun is a form of ahead-of-time compilation available since .NET Core 3.0. When you publish with `<PublishReadyToRun>true</PublishReadyToRun>`, the compiler pre-compiles IL to native code for the target platform. Unlike NGen, R2R images are portable across machines with the same OS and architecture. The CLR can load R2R code directly, bypassing Tier 0 JIT entirely. In serverless and containerized environments, R2R typically reduces cold start time by 30-80%.

### Trimming

IL trimming (enabled with `<PublishTrimmed>true</PublishTrimmed>`) removes unused code from your application and its dependencies at publish time. A smaller application means fewer assemblies to load and less code to JIT-compile (if any). This is particularly impactful in Blazor WebAssembly, where the trimmed application must be downloaded to the browser.

## Part 5: .NET 10 and Native AOT — The Cold Start Killer

.NET 10, released as an LTS release in late 2025, represents the most significant advancement in cold start performance since .NET's creation.

### Native AOT Compilation

Native Ahead-of-Time compilation (`<PublishAot>true</PublishAot>`) compiles your entire application to a native binary at publish time. There is no IL, no JIT compiler, no CLR runtime to initialize. The resulting binary is a self-contained native executable that starts like a C program.

The performance difference is staggering. Benchmarks show startup times dropping from hundreds of milliseconds to single-digit milliseconds for minimal APIs. One production report documented startup dropping from 70ms to 14ms — an 80% reduction — with memory usage cut by more than 50%. In serverless environments like AWS Lambda, cold start improvements of up to 86% have been measured.

Native AOT achieves this by eliminating several entire categories of cold start work: there is no JIT compilation (code is already native), no IL metadata loading, no tiered compilation infrastructure, and the binary includes only the code your application actually uses (aggressive tree shaking). The resulting binary for a minimal API console app is around 1 MB in .NET 10, down from several MB in .NET 7.

### The Trade-offs

Native AOT is not free. It imposes constraints that you must design around:

**No runtime reflection** — You cannot use `Type.GetType()`, `Activator.CreateInstance()`, or other reflection APIs that depend on metadata that has been stripped away. This means libraries like traditional Entity Framework (which relies heavily on reflection), many DI containers, and AutoMapper in its default configuration do not work with Native AOT.

**Source generators required** — Instead of reflection, .NET 10 uses compile-time source generators. `System.Text.Json` requires `[JsonSerializable]` attributes to generate serialization code at compile time. DI containers must use compile-time registration.

**Platform-specific binaries** — A Native AOT binary compiled on Linux x64 runs only on Linux x64. You need separate publish steps for each target platform.

**Longer publish times** — The native compiler takes significantly longer than `dotnet publish` without AOT, because it must compile and optimize the entire application.

**Potentially lower peak throughput** — The JIT compiler can use runtime profiling data to optimize hot paths in ways the AOT compiler cannot. For long-running server applications, JIT-compiled code may achieve higher steady-state requests per second than AOT-compiled code. You trade peak throughput for instant startup.

### Selective AOT in .NET 10

.NET 10 introduces the ability to AOT-compile specific performance-critical assemblies while keeping the rest JIT-compiled. This hybrid approach lets you optimize startup-critical paths with AOT while retaining the flexibility and peak performance of JIT for the rest of your application.

### CreateSlimBuilder

For Native AOT scenarios, .NET 10 provides `WebApplication.CreateSlimBuilder()`, a minimal builder that excludes services not compatible with AOT (like the full MVC framework). This produces even smaller, faster binaries for API-only workloads.

### Blazor WebAssembly and AOT

Blazor WebAssembly benefits from AOT as well. The `<WasmStripILAfterAOT>true</WasmStripILAfterAOT>` property in .NET 10 removes IL from the WASM bundle after AOT compilation, producing significantly smaller downloads. Combined with Blazor's 76% smaller JavaScript bundles in .NET 10, the initial load time for Blazor WASM applications has improved dramatically.

### MAUI and Mobile Native AOT

.NET 10 extends Native AOT support to Android (with measured startup improvements from 1+ seconds with Mono AOT down to 271-331ms) and continues existing iOS/Mac Catalyst AOT support. Windows App SDK is expected to gain Native AOT support shortly after the .NET 10 release.

## Part 6: The Modern ASP.NET Core Request Pipeline in .NET 10

With all these compilation advances in mind, here is what the modern .NET 10 request lifecycle looks like:

### Application Startup

1. **Process start** — The native binary (if using AOT) or dotnet runtime loads the application. With Native AOT, this is nearly instant. With JIT + R2R, Tiered Compilation ensures Quick JIT handles initial methods in microseconds.

2. **Host configuration** — `WebApplicationBuilder` reads configuration from appsettings.json, environment variables, and other providers. The DI container is built with all registered services.

3. **Middleware pipeline construction** — The middleware pipeline is built in the order you specified. Each `Use*` call adds a delegate to a chain. The pipeline is constructed once and reused for all requests.

4. **Server start** — Kestrel begins listening on configured ports.

### Per-Request Flow

Once the application is running, each request flows through the middleware pipeline:

1. **Kestrel receives the connection** — HTTP parsing happens in optimized, allocation-free code using `System.IO.Pipelines` and `Span<T>`.

2. **Middleware pipeline executes** — Each middleware gets a chance to handle the request or pass it to the next middleware. Common middleware includes exception handling, HTTPS redirection, static files, routing, authentication, authorization, and CORS.

3. **Routing** — The routing middleware matches the request URL to an endpoint. In .NET 10, the routing system uses a highly optimized trie-based data structure that matches endpoints in near-constant time regardless of how many routes are registered.

4. **Endpoint execution** — The matched endpoint runs. For minimal APIs, this is a simple delegate. For MVC controllers, this involves model binding, action filters, action execution, result filters, and result execution. For Razor Pages, the page handler executes.

5. **Response writing** — The response flows back through the middleware pipeline in reverse order, allowing each middleware to modify headers or the response body.

## Part 7: Practical Recommendations

Based on everything above, here is what you should do depending on your situation.

### If you are still on .NET Framework

Migrate. Seriously. The performance, security, and ecosystem benefits of modern .NET are enormous, and .NET Framework 4.8 is in maintenance mode with no new features. If migration is not immediately possible, use pre-compilation, NGen, IIS Application Initialization, and disable idle timeouts.

### If you are on .NET 6/8 and cold starts matter

Publish with ReadyToRun (`<PublishReadyToRun>true</PublishReadyToRun>`). Enable trimming if your dependency graph supports it. Consider Native AOT if your application uses minimal APIs and avoids heavy reflection. Evaluate your startup code for unnecessary synchronous work that can be deferred or made asynchronous.

### If you are starting a new project on .NET 10

Design for Native AOT from day one. Use `[JsonSerializable]` for all JSON types. Avoid reflection-based libraries. Use source generators wherever possible. Test AOT compatibility early with `<IsAotCompatible>true</IsAotCompatible>`. Use `dotnet publish` with AOT regularly during development to catch compatibility issues before they accumulate. Take advantage of the new SLNX solution format, Directory.Build.props for shared configuration, and central package management for clean project organization.

### For Blazor WebAssembly specifically

Enable AOT compilation and IL stripping. Use lazy loading for assemblies not needed on the initial page. Keep your dependency graph lean — every NuGet package adds to the download size. Pre-render on the server if possible (Blazor Server or Blazor United) to give users an instant first paint while the WASM runtime downloads in the background.

## Conclusion

The ASP.NET cold start problem was real and painful for over a decade. It was caused by a perfect storm of just-in-time compilation, dynamic view compilation, heavy framework initialization, and IIS process management. Modern .NET has attacked each of these causes systematically: Tiered Compilation and ReadyToRun reduce JIT overhead, build-time view compilation eliminates runtime Razor compilation, the minimal hosting model reduces initialization work, and Kestrel eliminates IIS recycling. Native AOT in .NET 10 goes even further by eliminating JIT entirely, producing native binaries with startup times measured in milliseconds rather than seconds.

The result is that a well-optimized .NET 10 application can cold-start faster than most Node.js or Python applications — a dramatic reversal from the .NET Framework era. The ecosystem has matured, the tooling is excellent, and the migration path from .NET 8 LTS to .NET 10 LTS is smooth. If cold starts have been holding you back from .NET, it is time to take another look.
