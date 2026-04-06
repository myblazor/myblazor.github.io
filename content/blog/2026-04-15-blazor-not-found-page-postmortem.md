---
title: "Post-Mortem: How We Broke My Blazor Magazine With a Missing @page Directive and What We Learned About Blazor's NotFoundPage in .NET 10"
date: 2026-04-15
author: myblazor-team
summary: "A detailed post-mortem of a production-breaking bug in My Blazor Magazine caused by migrating from the deprecated Router <NotFound> render fragment to the new .NET 10 NotFoundPage parameter — without adding the required @page directive to the target component. Covers the full history of Blazor's 404 handling, the exact error, the root cause, the fix, and every lesson learned."
tags:
  - blazor
  - dotnet
  - postmortem
  - routing
  - aspnet
  - deep-dive
  - best-practices
---

## Part 1 — What Happened

On April 2, 2026, My Blazor Magazine went down. Not "partially degraded." Not "slow." Down. Every single page — the home page, the blog, the showcase, the about page — rendered a white screen with a cryptic error in the browser console:

```
Unhandled exception rendering component: The type ObserverMagazine.Web.Pages.NotFoundView does not have a Microsoft.AspNetCore.Components.RouteAttribute applied to it.
System.InvalidOperationException: The type ObserverMagazine.Web.Pages.NotFoundView does not have a Microsoft.AspNetCore.Components.RouteAttribute applied to it.
   at Microsoft.AspNetCore.Components.Routing.Router.SetParametersAsync(ParameterView parameters)
```

The application was completely non-functional. The Blazor WebAssembly runtime loaded, the .NET runtime initialized, the `App` component attempted to render, and then the `Router` component threw a `System.InvalidOperationException` during `SetParametersAsync` — before any page component ever had a chance to render. The error was not in a leaf component, not in a service, not in a page. It was in the Router itself, the very first thing Blazor renders. The entire component tree was dead on arrival.

This is the story of what went wrong, why it went wrong, exactly how we fixed it, and what we learned about Blazor's routing system in the process.

## Part 2 — The Change That Broke Everything

The breaking change was a migration from the old, deprecated `<NotFound>` render fragment pattern to the new `NotFoundPage` parameter on the `Router` component. Here is what the `App.razor` file looked like before the change:

```razor
<Router AppAssembly="typeof(App).Assembly">
    <Found Context="routeData">
        <RouteView RouteData="routeData" DefaultLayout="typeof(MainLayout)" />
        <FocusOnNavigate RouteData="routeData" Selector="h1" />
    </Found>
    <NotFound>
        <PageTitle>Not Found — My Blazor Magazine</PageTitle>
        <LayoutView Layout="typeof(MainLayout)">
            <div class="container text-center" style="padding: 4rem 1rem;">
                <h1>404 — Page Not Found</h1>
                <p>The page you're looking for doesn't exist.</p>
                <a href="/">Go Home</a>
            </div>
        </LayoutView>
    </NotFound>
</Router>
```

This was the "old way." The `<NotFound>` block is a `RenderFragment` — a chunk of inline Razor markup that the Router renders whenever the current URL does not match any `@page` route in the application. It worked. It was stable. It had been shipping with Blazor since the very beginning.

The migration changed `App.razor` to this:

```razor
<Router AppAssembly="typeof(App).Assembly" NotFoundPage="typeof(NotFoundView)">
    <Found Context="routeData">
        <RouteView RouteData="routeData" DefaultLayout="typeof(MainLayout)" />
        <FocusOnNavigate RouteData="routeData" Selector="h1" />
    </Found>
</Router>
```

And a new file, `Pages/NotFoundView.razor`, was created:

```razor
<PageTitle>Not Found — My Blazor Magazine</PageTitle>
<LayoutView Layout="typeof(MainLayout)">
    <div class="container text-center" style="padding: 4rem 1rem;">
        <h1>404 — Page Not Found</h1>
        <p>The page you're looking for doesn't exist.</p>
        <a href="/">Go Home</a>
    </div>
</LayoutView>
```

Do you see the problem? The `NotFoundView` component has no `@page` directive. It is a plain component, not a routable page. The `Router.NotFoundPage` parameter requires a routable page — a component with a `RouteAttribute`, which is what the `@page` directive compiles into. Without that attribute, the Router throws an `InvalidOperationException` during its own initialization, before it ever gets a chance to match any route.

The result: every page, including the home page, is broken. Not just the 404 page. Everything.

## Part 3 — Understanding the Blazor Router's Initialization Sequence

To understand why this error is so catastrophic, you need to understand how the Blazor Router initializes. The Router is not just another component. It is the root of the entire component tree for routable content. Here is the sequence of events when a Blazor WebAssembly application starts:

1. The browser downloads and executes `blazor.webassembly.js`.
2. The script downloads the .NET WebAssembly runtime, the application's DLLs, and any satellite assemblies.
3. The runtime initializes and calls `Program.cs`, which configures services and root components.
4. The `App` component is added as a root component mounted to the `#app` DOM element.
5. `App.razor` renders, which means the `Router` component renders.
6. The `Router.SetParametersAsync` method is called with whatever parameters are declared on the `<Router>` tag — `AppAssembly`, `NotFoundPage`, the `Found` render fragment, and so on.
7. Inside `SetParametersAsync`, the Router validates its parameters. If `NotFoundPage` is set, it checks that the provided `Type` has a `RouteAttribute`. If it does not, the Router throws `InvalidOperationException` immediately.
8. If validation passes, the Router scans the specified assembly for all types with `RouteAttribute` and builds a route table.
9. The Router matches the current URL against the route table.
10. If a match is found, the `<Found>` render fragment is rendered with the `RouteData`.
11. If no match is found, the `NotFoundPage` component is rendered (or the `<NotFound>` fragment, if `NotFoundPage` is not set).

The critical thing to understand is that step 7 — the validation of the `NotFoundPage` type — happens before step 8 through 11. It happens before any route matching occurs. It happens unconditionally, on every single page load. If the validation fails, no route is ever matched, no page is ever rendered, and the entire application is dead.

This is not a "the 404 page is broken" situation. This is a "the entire application is broken" situation. The Router validates `NotFoundPage` eagerly, not lazily. It does not wait until a 404 actually occurs to check whether the type is valid. It checks immediately, on startup, for every request.

## Part 4 — What Is a RouteAttribute and Why Does the Router Require It?

In Blazor, the `@page` directive is syntactic sugar for applying the `Microsoft.AspNetCore.Components.RouteAttribute` to the compiled component class. When you write this:

```razor
@page "/about"
```

The Razor compiler generates a C# class with this attribute:

```csharp
[RouteAttribute("/about")]
public partial class About : ComponentBase
{
    // ...
}
```

The `RouteAttribute` serves two purposes:

1. **Route registration.** During Router initialization (step 8 above), the Router scans the assembly for all types decorated with `RouteAttribute` and builds a route table mapping URL patterns to component types.
2. **Type validation.** When the Router receives a `Type` via the `NotFoundPage` parameter, it checks for the presence of at least one `RouteAttribute` on that type. This is a design decision by the ASP.NET Core team, documented in the API proposal for `Router.NotFoundPage` (GitHub issue dotnet/aspnetcore#62409): "If the specified NotFoundPage type is not a valid Blazor component or is a component without RouteAttribute, a runtime error will occur."

Why does the Router require a `RouteAttribute` on the `NotFoundPage` type? The reason is that the `NotFoundPage` feature was designed to work in concert with server-side middleware, specifically the Status Code Pages Re-execution Middleware. In a Blazor Server or Blazor Web App (the new unified hosting model in .NET 8 and later), the `NotFoundPage` is not only rendered by the client-side interactive router when no route matches — it is also rendered by the server-side middleware when a 404 status code is returned during static server-side rendering or streaming rendering.

For the server-side middleware to work, the `NotFoundPage` component must be a routable page with a URL that the server can redirect to. If the component has `@page "/not-found"`, the server can re-execute the request pipeline with the URL `/not-found`, which will then match the `NotFoundPage` component and render it with the full layout and styling. Without a route, the server-side middleware has no URL to redirect to.

In a pure Blazor WebAssembly application like My Blazor Magazine — which runs entirely in the browser with no server-side rendering — the server-side middleware integration is irrelevant. The Router could, in theory, render a component without a `RouteAttribute` for client-side 404 handling. But the ASP.NET Core team made a deliberate design choice to enforce the `RouteAttribute` requirement unconditionally, regardless of hosting model. This simplifies the Router's implementation and ensures that the `NotFoundPage` feature works consistently across all hosting models.

The Microsoft Learn documentation for .NET 10 shows the canonical pattern explicitly:

```razor
@page "/not-found"
@layout MainLayout

<h3>Not Found</h3>
<p>Sorry, the content you are looking for does not exist.</p>
```

The `@page "/not-found"` directive is not optional. It is a hard requirement.

## Part 5 — The History of 404 Handling in Blazor

To appreciate why this migration was attempted in the first place, and why the old pattern was deprecated, it helps to understand the full history of 404 handling in Blazor.

### Blazor 3.0 Through 7.0 — The NotFound Render Fragment

From the very first release of Blazor (as part of ASP.NET Core 3.0 in September 2019), the Router component supported a `<NotFound>` child content parameter. This was a `RenderFragment` — a chunk of inline Razor markup that the Router rendered whenever no route matched the current URL.

The pattern looked like this:

```razor
<Router AppAssembly="typeof(App).Assembly">
    <Found Context="routeData">
        <RouteView RouteData="routeData" DefaultLayout="typeof(MainLayout)" />
    </Found>
    <NotFound>
        <LayoutView Layout="typeof(MainLayout)">
            <p>Sorry, there's nothing at this address.</p>
        </LayoutView>
    </NotFound>
</Router>
```

This pattern was simple, self-contained, and worked for all hosting models (Blazor Server and Blazor WebAssembly). It was the recommended approach in all official documentation and tutorials for five major releases of .NET.

### .NET 8 — The Unified Hosting Model and the Problem With NotFound

.NET 8 introduced the "Blazor Web App" project template, which unified Blazor Server and Blazor WebAssembly into a single hosting model with static server-side rendering (static SSR), streaming rendering, and interactive rendering modes. This was a fundamental architectural shift.

With the new hosting model, the `<NotFound>` render fragment had a problem. Steve Sanderson (one of the original creators of Blazor) filed GitHub issue dotnet/aspnetcore#48983 in June 2023, explaining the issue:

In the new .NET 8 project style, the `<NotFound>` render fragment was never actually used. Here is why:

- If the Router is not interactive (static SSR), a navigation to a nonexistent URL returns a 404 from the server before the Router ever runs. The Router never gets a chance to render the `<NotFound>` fragment.
- If the Router is interactive, a navigation to a nonexistent URL does not match any `@page` route, and the existing client-side routing logic causes a full-page load (which again hits the server, which returns a 404).

In both cases, the `<NotFound>` fragment is unreachable. The default project template in .NET 8 still included it, which was confusing because it gave developers the impression that it was functional when it was not.

### .NET 9 — NavigationManager.NotFound and the Birth of NotFoundPage

ASP.NET Core 9 (released November 2024) introduced a new approach: the `NavigationManager.NotFound()` method and the `Router.NotFoundPage` parameter. The idea was to replace the inline `<NotFound>` render fragment with a dedicated, reusable page component that could be:

1. Rendered by the client-side interactive Router when `NavigationManager.NotFound()` is called.
2. Rendered by the server-side Status Code Pages Re-execution Middleware when a 404 status code is returned during static SSR or streaming rendering.

This unified approach meant that both the client and the server could render the same 404 page, with the same layout and styling, regardless of how the 404 was triggered.

The `<NotFound>` render fragment was deprecated (marked with `[Obsolete]` in the Router source code, generating compiler warning CS0618) in favor of the new `NotFoundPage` parameter.

### .NET 10 — The Deprecation Becomes a Practical Concern

In .NET 10 (the version My Blazor Magazine targets), the `TreatWarningsAsErrors` compiler option is enabled in our `Directory.Build.props`:

```xml
<TreatWarningsAsErrors>true</TreatWarningsAsErrors>
```

This means that the CS0618 deprecation warning for the `<NotFound>` render fragment becomes a build error. The old pattern no longer compiles. We were forced to migrate to `NotFoundPage`.

And that is how we ended up here: we migrated to `NotFoundPage` but forgot the `@page` directive.

## Part 6 — The Exact Error, Dissected

Let us look at the error message one more time:

```
System.InvalidOperationException: The type ObserverMagazine.Web.Pages.NotFoundView does not have a Microsoft.AspNetCore.Components.RouteAttribute applied to it.
   at Microsoft.AspNetCore.Components.Routing.Router.SetParametersAsync(ParameterView parameters)
```

This error message tells us several things:

1. **`System.InvalidOperationException`** — This is not an `ArgumentException` or a `NullReferenceException`. It is an `InvalidOperationException`, which in .NET conventions means "the operation is not valid given the current state of the object." The Router is telling us that it cannot initialize because the provided `NotFoundPage` type is in an invalid state (missing the required attribute).

2. **`The type ObserverMagazine.Web.Pages.NotFoundView`** — The error identifies the exact type that caused the problem. This is the type we passed to `NotFoundPage="typeof(NotFoundView)"`.

3. **`does not have a Microsoft.AspNetCore.Components.RouteAttribute applied to it`** — The error is unambiguous about what is missing. The `RouteAttribute` is what the `@page` directive compiles into. Without it, the type is a plain component, not a routable page.

4. **`at Microsoft.AspNetCore.Components.Routing.Router.SetParametersAsync(ParameterView parameters)`** — The error occurs during parameter initialization of the Router itself. This is the very first thing that happens when the Router renders. No route matching, no page rendering, no component tree — just parameter validation. If this fails, the entire application fails.

The exception is thrown from the `Router.SetParametersAsync` method in the ASP.NET Core source code. The relevant validation logic checks whether the `NotFoundPage` type has at least one `RouteAttribute`. If it does not, the exception is thrown unconditionally.

## Part 7 — The Fix

The fix is a single line. Add the `@page "/not-found"` directive to `NotFoundView.razor`.

Here is the complete, corrected `NotFoundView.razor`:

```razor
@page "/not-found"
@layout MainLayout

<PageTitle>Not Found — My Blazor Magazine</PageTitle>

<div class="container text-center" style="padding: 4rem 1rem;">
    <h1>404 — Page Not Found</h1>
    <p>The page you're looking for doesn't exist.</p>
    <a href="/">Go Home</a>
</div>
```

Three changes from the broken version:

1. **Added `@page "/not-found"`** — This is the critical fix. It causes the Razor compiler to generate a `[RouteAttribute("/not-found")]` on the compiled class, satisfying the Router's validation check.

2. **Added `@layout MainLayout`** — This tells the component to use `MainLayout` as its layout, which provides the header, footer, and navigation. Without this, the 404 page would render without the site's chrome. Previously, the component used `<LayoutView Layout="typeof(MainLayout)">` inline, which achieved the same effect but is unnecessary when `@layout` is available.

3. **Removed the `<LayoutView>` wrapper** — Since `@layout MainLayout` handles the layout assignment, the explicit `<LayoutView>` component is no longer needed. The content is rendered directly inside the layout's `@Body` slot.

`App.razor` does not change. It was already correct:

```razor
<Router AppAssembly="typeof(App).Assembly" NotFoundPage="typeof(NotFoundView)">
    <Found Context="routeData">
        <RouteView RouteData="routeData" DefaultLayout="typeof(MainLayout)" />
        <FocusOnNavigate RouteData="routeData" Selector="h1" />
    </Found>
</Router>
```

The `NotFoundPage="typeof(NotFoundView)"` parameter is the correct, non-deprecated way to specify a 404 page in .NET 10. The only thing that was missing was the `@page` directive on the target component.

## Part 8 — Why Not Just Go Back to the Old Code?

A reasonable question: why not just revert to the `<NotFound>` render fragment? It worked for five years. It is simpler. It does not require a separate component file.

The answer is that we cannot. Our project has `TreatWarningsAsErrors` enabled:

```xml
<PropertyGroup>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
</PropertyGroup>
```

The `<NotFound>` render fragment on the `Router` component is decorated with the `[Obsolete]` attribute in .NET 10's ASP.NET Core source code. Using it generates compiler warning CS0618:

```
warning CS0618: 'Router.NotFound' is obsolete: 'Use NotFoundPage instead.'
```

With `TreatWarningsAsErrors` enabled, this warning becomes a build error:

```
error CS0618: 'Router.NotFound' is obsolete: 'Use NotFoundPage instead.'
```

We could disable `TreatWarningsAsErrors`, but that would be a terrible trade-off. `TreatWarningsAsErrors` is one of the most important compiler settings for maintaining code quality. It catches nullability violations, unused variables, platform compatibility issues, and dozens of other problems that would otherwise silently accumulate. Disabling it to avoid a deprecation migration is technical debt of the worst kind — you are not fixing the problem, you are hiding it while simultaneously hiding all future problems.

We could also add a `#pragma warning disable CS0618` around the `<NotFound>` usage, but this is just a more targeted version of the same bad idea. You are still using deprecated API that will eventually be removed in a future version of .NET.

The correct approach is to use the new `NotFoundPage` parameter and ensure the target component has the required `@page` directive. That is what we did.

## Part 9 — The Difference Between a Component and a Page in Blazor

This incident highlights a fundamental distinction in Blazor that is easy to overlook: the difference between a component and a page.

### Components

A component is any class that inherits from `ComponentBase` (directly or indirectly) and is defined in a `.razor` file. Components can accept parameters, render markup, and be nested inside other components. Examples:

```razor
@* AuthorCard.razor — a component *@
@if (Author is not null)
{
    <div class="author-card">
        <strong>@Author.Name</strong>
    </div>
}

@code {
    [Parameter] public AuthorProfile? Author { get; set; }
}
```

Components do not have a `@page` directive. They are not routable. You cannot navigate to them via a URL. They exist to be composed inside other components or pages.

### Pages

A page is a component that has a `@page` directive. The `@page` directive compiles into a `RouteAttribute` on the generated class. Pages are routable — the Router can match a URL pattern to them and render them directly.

```razor
@page "/about"

<PageTitle>About — My Blazor Magazine</PageTitle>

<h1>About My Blazor Magazine</h1>
<p>We build things.</p>
```

The distinction matters because the Router treats these two categories differently:

- **Route scanning:** During initialization, the Router scans the specified assembly for all types with `RouteAttribute`. Only pages are included in the route table. Components without `@page` are invisible to the Router.
- **`NotFoundPage` validation:** The Router requires the `NotFoundPage` type to have a `RouteAttribute`. This means `NotFoundPage` must point to a page, not a plain component.
- **`@layout` directive:** The `@layout` directive only works on pages (components with `@page`). On a plain component, `@layout` is ignored. If you want to apply a layout to a non-page component, you must use `<LayoutView>` explicitly.

Our `NotFoundView` was a component pretending to be a page. It had no `@page` directive, so it was not a page. But `NotFoundPage` expected a page. The mismatch caused the crash.

## Part 10 — The @page Directive Route Does Not Matter for NotFoundPage

An important subtlety: the `@page "/not-found"` route on the `NotFoundView` component does not determine when the component is rendered by the Router's 404 handling. The Router renders `NotFoundPage` whenever no other route matches, regardless of what URL pattern is on the `@page` directive.

You could write `@page "/this-url-will-never-be-typed-by-anyone"` and the Router would still render it as the 404 page when no route matches. The `@page` directive is required only to satisfy the `RouteAttribute` validation check.

However, there is a practical reason to choose a sensible route like `/not-found`: if you ever set up server-side status code page re-execution (via `app.UseStatusCodePagesWithReExecute("/not-found")` in a Blazor Server or Blazor Web App), the server will redirect 404 responses to that URL. The route needs to actually match the component for this to work.

For a pure Blazor WebAssembly application hosted on GitHub Pages (like My Blazor Magazine), server-side middleware is not applicable. But choosing `/not-found` as the route is still good practice — it is descriptive, it follows the convention used in the official Microsoft templates, and it future-proofs the application in case we ever add a server-side component.

## Part 11 — How GitHub Pages Handles 404s for SPAs

My Blazor Magazine is a Blazor WebAssembly application deployed to GitHub Pages. Understanding how GitHub Pages handles 404s is essential context for this post-mortem.

GitHub Pages is a static file server. It serves files from a directory. When a request comes in for a URL that does not correspond to a file on disk, GitHub Pages returns a 404 status code and serves the contents of a `404.html` file if one exists in the root of the site.

For single-page applications (SPAs) like Blazor WebAssembly, this creates a problem. When a user navigates to `https://observermagazine.github.io/blog/some-post`, there is no file at `/blog/some-post` on disk. GitHub Pages returns a 404.

Our `404.html` file handles this with a JavaScript redirect trick:

```html
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="utf-8" />
    <title>My Blazor Magazine</title>
    <script>
        var pathSegmentsToKeep = 0;
        var l = window.location;
        l.replace(
            l.protocol + '//' + l.hostname + (l.port ? ':' + l.port : '') +
            l.pathname.split('/').slice(0, 1 + pathSegmentsToKeep).join('/') + '/?/' +
            l.pathname.slice(1).split('/').slice(pathSegmentsToKeep).join('/').replace(/&/g, '~and~') +
            (l.search ? '&' + l.search.slice(1).replace(/&/g, '~and~') : '') +
            l.hash
        );
    </script>
</head>
<body></body>
</html>
```

This script converts the path into a query string and redirects to the root URL. For example, `/blog/some-post` becomes `/?/blog/some-post`. Then, in `index.html`, a complementary script reads the query string, reconstructs the original path, and uses `history.replaceState` to update the browser's address bar. Blazor's Router then reads the URL from the address bar and matches it to the correct page component.

This means that in a Blazor WebAssembly application on GitHub Pages, the Router's `NotFoundPage` is rendered in a very specific scenario: when the user navigates to a URL that does not match any `@page` route, but the navigation happens client-side (without a full page reload). For example, if the user clicks a link to `/blog/nonexistent-slug`, and that link is handled by Blazor's enhanced navigation (no full page reload), the Router will fail to find a matching route and render the `NotFoundPage` component.

For full-page navigations to nonexistent URLs (e.g., typing a URL directly in the address bar), the `404.html` redirect kicks in, Blazor loads, and the Router attempts to match the URL. If no match is found, `NotFoundPage` renders. So even in the GitHub Pages scenario, the `NotFoundPage` is functional — it just takes a roundtrip through the `404.html` redirect first.

## Part 12 — The Cascade of Failures

One thing that made this incident particularly painful is that the error was not gradual or partial. It was total and immediate. Here is why:

1. **The Router is the root of the component tree.** Every page in the application is rendered through the Router. If the Router cannot initialize, nothing renders.

2. **The validation is eager.** The Router checks `NotFoundPage` during `SetParametersAsync`, which runs on the very first render. There is no lazy initialization, no deferred validation, no graceful fallback.

3. **The error is an unhandled exception.** Blazor's `WebAssemblyRenderer` catches unhandled exceptions and logs them, but does not recover. The `#blazor-error-ui` element is shown (if configured), but the application is non-functional.

4. **The error occurs on every page.** Because the Router is in `App.razor`, which is the root component for every page, the error occurs regardless of which URL the user navigates to. The home page fails. The blog page fails. The about page fails. Everything fails.

5. **There is no server-side fallback.** In a Blazor Server application, the server could potentially render a fallback page. But in Blazor WebAssembly on GitHub Pages, there is no server-side rendering. If the client-side Router fails, there is nothing else.

This cascade of failures meant that the bug was a total site outage, not a degraded experience. The lesson: the Router is the single most critical component in a Blazor application. Any bug in Router initialization is, by definition, a total outage.

## Part 13 — Why the Compiler Did Not Catch This

A natural question: why did the C# compiler not catch this? The answer is that the `NotFoundPage` parameter is typed as `Type?`, not as a more specific type:

```csharp
[Parameter]
public Type? NotFoundPage { get; set; }
```

The `Type` class in .NET represents any type. There is no compile-time constraint that says "this Type must have a RouteAttribute." The compiler sees `typeof(NotFoundView)` and says "that is a valid Type" and moves on. The `RouteAttribute` check happens at runtime, in `Router.SetParametersAsync`.

Could the ASP.NET Core team have designed this differently? Possibly. They could have introduced a marker interface (e.g., `IRoutablePage`) that pages must implement, and typed the parameter as `Type` with a generic constraint or a custom analyzer. But the Blazor component model does not currently have such a marker interface, and adding one would be a breaking change to the component model.

In practice, this means that the `NotFoundPage` parameter is a runtime-checked contract. The compiler cannot help you here. You must know the requirement (the component must have `@page`) and satisfy it manually. If you do not, you get a runtime exception.

This is one of the rare cases in modern .NET development where the type system cannot express the constraint, and the error surfaces only at runtime. It is a paper cut in an otherwise excellent type system.

## Part 14 — How We Should Have Caught This Before Deployment

This bug should never have reached production. Here are the checkpoints that failed:

### 1. We Did Not Read the Documentation Carefully Enough

The Microsoft Learn documentation for `Router.NotFoundPage` clearly states that the target component must have a `@page` directive. The documentation includes a complete example:

```razor
@page "/not-found"
@layout MainLayout

<h3>Not Found</h3>
<p>Sorry, the content you are looking for does not exist.</p>
```

We skipped the documentation and assumed that `NotFoundPage` worked like the old `<NotFound>` render fragment — that any component would do. Assumption is the enemy of correctness.

### 2. We Did Not Run the Application Locally Before Deploying

If we had run `dotnet run --project src/ObserverMagazine.Web` and opened the application in a browser, we would have seen the error immediately. The error occurs on the very first page load. There is no scenario in which the application works with this bug. A single local test would have caught it.

### 3. We Did Not Have a Smoke Test in CI

Our CI pipeline (`deploy.yml`) runs `dotnet test`, which executes our bUnit component tests and integration tests. But none of our tests exercise the `App.razor` component directly. Our bUnit tests render individual components (`ResponsiveTable`, `MasterDetail`) in isolation, without the Router. We had no test that verified the Router initialization.

A smoke test that renders the `App` component and asserts that no exception is thrown would have caught this:

```csharp
[Fact]
public void App_RendersWithoutException()
{
    using var ctx = new BunitContext();
    // Register required services...
    var cut = ctx.Render<App>();
    Assert.NotNull(cut);
}
```

This is a test we should add.

### 4. The PR Preview Did Not Include Manual Testing

Our PR check workflow builds the full site and uploads it as a downloadable artifact. But we did not download and open the artifact to verify that the site actually works. An automated Playwright or similar browser-based test in CI would have caught this.

## Part 15 — Lessons Learned

### Lesson 1: The Router Is Critical Infrastructure

The Router is not just another component. It is the single point of failure for the entire application. Any change to `App.razor` or to the components referenced by `App.razor` (like `NotFoundView`) must be tested with extreme care. A bug in the Router means a total outage, not a degraded experience.

### Lesson 2: Read the Docs When Migrating Away From Deprecated APIs

When a compiler warning tells you that an API is deprecated and suggests an alternative, do not assume that the alternative is a drop-in replacement. Read the documentation for the new API. Understand its requirements. The `NotFoundPage` parameter has different requirements from the `<NotFound>` render fragment. The former requires a routable page; the latter accepts any markup.

### Lesson 3: Always Test Locally Before Deploying

This is the most basic software engineering practice, and we violated it. A single page load in a browser would have caught this bug. Never deploy a change without verifying it locally, especially a change to core infrastructure like the Router.

### Lesson 4: The Compiler Cannot Catch Everything

The .NET type system is excellent, but it cannot express every constraint. The `NotFoundPage` parameter is typed as `Type?`, which accepts any type. The `RouteAttribute` requirement is enforced at runtime, not compile-time. Be aware of runtime-checked contracts and test accordingly.

### Lesson 5: Add Smoke Tests for Core Components

We had unit tests for individual components and services, but no smoke test for the application as a whole. A smoke test that renders the root `App` component and verifies that it does not throw an exception is cheap to write and would have caught this bug in CI.

### Lesson 6: Understand the Difference Between Components and Pages

In Blazor, a component without `@page` is not a page. The `@page` directive is not just documentation or convention — it compiles to a `RouteAttribute` that the Router uses for route matching and type validation. When an API expects a page, you must provide a page. A component will not do.

## Part 16 — The Complete Diff

Here is the complete set of changes to fix this bug. Only one file changed:

### `src/ObserverMagazine.Web/Pages/NotFoundView.razor`

**Before (broken):**

```razor
<PageTitle>Not Found — My Blazor Magazine</PageTitle>
<LayoutView Layout="typeof(MainLayout)">
    <div class="container text-center" style="padding: 4rem 1rem;">
        <h1>404 — Page Not Found</h1>
        <p>The page you're looking for doesn't exist.</p>
        <a href="/">Go Home</a>
    </div>
</LayoutView>
```

**After (fixed):**

```razor
@page "/not-found"
@layout MainLayout

<PageTitle>Not Found — My Blazor Magazine</PageTitle>

<div class="container text-center" style="padding: 4rem 1rem;">
    <h1>404 — Page Not Found</h1>
    <p>The page you're looking for doesn't exist.</p>
    <a href="/">Go Home</a>
</div>
```

Three changes:
1. Added `@page "/not-found"` — the critical fix.
2. Added `@layout MainLayout` — replaces the inline `<LayoutView>` wrapper.
3. Removed the `<LayoutView>` wrapper — `@layout` handles it.

`App.razor` is unchanged. It was correct.

## Part 17 — The Broader Pattern — Deprecation Migrations in .NET

This incident is part of a broader pattern in .NET: the framework team deprecates an API in version N, introduces a replacement, and the replacement has subtly different requirements from the original. This is not a criticism — the new APIs are usually better designed and more capable. But the migration path is not always obvious, and the differences are not always well-communicated in the deprecation warning itself.

The CS0618 warning for `Router.NotFound` says:

```
'Router.NotFound' is obsolete: 'Use NotFoundPage instead.'
```

This tells you what to use, but it does not tell you how to use it. It does not mention the `@page` directive requirement. It does not link to documentation. It is a single sentence.

Compare this with some other deprecation messages in .NET that do include more context:

```
'WebClient' is obsolete: 'WebClient has been deprecated. Use HttpClient instead.'
```

Neither of these messages is detailed enough to guide a migration. The developer is expected to read the documentation for the replacement API.

The lesson for library authors (and for us, as consumers): when you see a deprecation warning, always read the full documentation for the replacement. Do not treat the warning message as a migration guide. It is a pointer, not a manual.

## Part 18 — What the Official .NET 10 Template Looks Like

For reference, here is what the official .NET 10 Blazor project template generates for 404 handling.

In a Blazor Web App (server-side rendering):

**`Components/Pages/NotFound.razor`:**
```razor
@page "/not-found"
@layout MainLayout

<h3>Not Found</h3>
<p>Sorry, the content you are looking for does not exist.</p>
```

**`Components/Routes.razor`:**
```razor
<Router AppAssembly="typeof(Program).Assembly"
        NotFoundPage="typeof(Pages.NotFound)">
    <Found Context="routeData">
        <RouteView RouteData="routeData"
                   DefaultLayout="typeof(Layout.MainLayout)" />
        <FocusOnNavigate RouteData="routeData" Selector="h1" />
    </Found>
</Router>
```

**`Program.cs` (server-side only):**
```csharp
app.UseStatusCodePagesWithReExecute("/not-found",
    createScopeForStatusCodePages: true);
```

The Blazor WebAssembly standalone template does not include `UseStatusCodePagesWithReExecute` (there is no server), but the `NotFound.razor` page still has the `@page` directive because the Router requires it.

Our fixed code matches this pattern exactly.

## Part 19 — Timeline of the Incident

- **April 1, 2026, evening** — A batch of code cleanup and modernization changes was prepared, including the migration from `<NotFound>` to `NotFoundPage`. The `NotFoundView.razor` component was extracted from the inline markup in `App.razor` but the `@page` directive was not added.
- **April 2, 2026, 00:58 UTC** — The project dump was exported, showing the broken code in the repository.
- **April 2, 2026, ~01:04 UTC** — The deployed site was accessed. The Blazor runtime loaded, `App` started, the `TelemetryService` logged "AppStarted," and then the `Router` threw `InvalidOperationException`. The site was completely non-functional.
- **April 2, 2026** — The error was reported via the browser console.
- **April 15, 2026** — This post-mortem was written, the fix was applied (a single `@page` directive added to `NotFoundView.razor`), and the site was restored.

## Part 20 — Recommendations for Other Teams

If you are maintaining a Blazor application and migrating from the `<NotFound>` render fragment to `NotFoundPage`, here is a checklist:

1. **Create a dedicated page component** for your 404 content. Put it in your `Pages` folder (e.g., `Pages/NotFound.razor` or `Pages/NotFoundView.razor`).

2. **Add a `@page` directive.** Use `@page "/not-found"` or a similar descriptive route. This is required.

3. **Add a `@layout` directive** if you want the 404 page to use your application's layout (header, footer, navigation). Without it, the page renders without any layout chrome.

4. **Set the `NotFoundPage` parameter** on the Router in `App.razor` (or `Routes.razor`): `NotFoundPage="typeof(NotFoundView)"`.

5. **Remove the `<NotFound>` render fragment** from the Router. If both `<NotFound>` and `NotFoundPage` are present, `NotFoundPage` takes priority, but having both is confusing and generates the deprecation warning.

6. **Test locally.** Open the application in a browser. Navigate to a nonexistent URL (e.g., `/this-does-not-exist`). Verify that your 404 page renders correctly with the layout.

7. **If you use server-side rendering**, add `app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true)` in `Program.cs`. This ensures that server-side 404 responses also render your 404 page.

8. **Add a smoke test** that renders your root component (App or Routes) in a bUnit test and asserts that no exception is thrown.

## Part 21 — Resources

- [ASP.NET Core Blazor routing — Microsoft Learn (.NET 10)](https://learn.microsoft.com/en-us/aspnet/core/blazor/fundamentals/routing?view=aspnetcore-10.0) — The official documentation for Blazor routing, including the `NotFoundPage` parameter and the `@page` directive requirement.
- [API proposal for Router.NotFoundPage — GitHub issue dotnet/aspnetcore#62409](https://github.com/dotnet/aspnetcore/issues/62409) — The original API proposal that introduced `NotFoundPage`, including the design rationale and the statement that "If the specified NotFoundPage type is not a valid Blazor component or is a component without RouteAttribute, a runtime error will occur."
- [Router's NotFound content is never used in new Web project style — GitHub issue dotnet/aspnetcore#48983](https://github.com/dotnet/aspnetcore/issues/48983) — Steve Sanderson's issue explaining why the `<NotFound>` render fragment is unreachable in the .NET 8 unified hosting model.
- [.NET 10 Has Arrived — Here's What's Changed for Blazor — Telerik Blog](https://www.telerik.com/blogs/net-10-has-arrived-heres-whats-changed-blazor) — A summary of Blazor changes in .NET 10, including the `NotFoundPage` feature.
- [My Blazor Magazine source code — GitHub](https://github.com/ObserverMagazine/observermagazine.github.io) — The full source code of the application discussed in this post-mortem.
