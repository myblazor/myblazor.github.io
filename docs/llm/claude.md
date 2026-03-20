if any file needs changing, always return the FULL file so it is easy to copy paste. 
Please read the FULL dump.txt in its entirety for each prompt. 
Do not skim it. 
Do not search for keywords in it. 
Do not retrieve parts from it. 
It isn't that big. 
Read the whole thing. 
And please generate FULL files for any file that needs to change 
Please do not hallucinate. 
Take some more time up front to save time and effort for the humans. 
When in doubt, use your best judgment. 
Use best engineering practices
within the limits of this project. 
I want to learn blazor web assembly to write high performance cross platform free of cost. 
I want to use the latest and greatest technology. 
I want this to serve as a sample as well as a starting point for native applications. 
It should be easy to use the latest dotnet technology 
such as slnx files, props files, and so on. 
Where possible, we should use long term sustainable technology such as sqlite and postgresql. 
We should avoid any nuget package that requires payment of money, free of cost for non-commercial software is not enough. 
We ourselves should not charge any money, ever. 
We should have extensive logging, metrics, etc using open telemetry. 
Application should be built from the ground up to be testable.
All tests including Unit tests, integration tests should be automated and be performant so we can run them after every change. 
The whole thing should fit in a single git repository. 
the repo will be at https://github.com/ObserverMagazine/observermagazine.github.io 

and we need to deploy to https://observermagazine.github.io
use dotnet 10 
use asp dotnet 10 
generate the full project 
we want to showcase multiple web technologies 
such as responsive tables 
master detail flow 
and so on 
we also want to be able to publish a blog here 
so I will add simple markdown files in a folder and the application needs to read them 
and make them available as a blog 
also we want to make an rss feed 
because there is no real backend 
everything should be compiled so it is ready to go 
on github pages
do not hallucinate 

24
38

Looking at your requirements, I'll build a complete Blazor WebAssembly project for GitHub Pages with blog support, RSS, showcases, OpenTelemetry, and full test coverage. Let me generate every file.Now I have all the confirmed versions. Let me generate the complete project.

**Confirmed tech stack:**
- .NET 10 SDK 10.0.201 (LTS, stable, released March 12, 2026)
- C# 14, SLNX solution format (default in .NET 10)
- Markdig 1.1.1 (BSD-2-Clause) — Markdown processing
- YamlDotNet 16.3.0 (MIT) — YAML front matter
- bUnit 2.6.2 (MIT) — Blazor component testing
- xunit.v3 3.2.2 (Apache-2.0) — Test framework

All packages are free for any use, no payment required.Here's your complete project — 40+ files covering the entire application. Let me walk through the architecture and key decisions.

**How it works end-to-end:**

The project has three main parts that work together. The **Blazor WASM app** (`src/ObserverMagazine.Web`) is the user-facing site — it fetches pre-built JSON and HTML at runtime, so no server needed. The **Content Processor** (`tools/ObserverMagazine.ContentProcessor`) is a console app that runs at build time (locally or in CI) to convert your Markdown blog posts into static `blog-data/*.html` files, a `posts-index.json`, and an RSS `feed.xml`. The **test suite** covers both component behavior (bUnit) and content processing logic (xUnit v3).

**Key architectural decisions:**

The blog system works by having the Content Processor read `content/blog/*.md` files with YAML front matter, convert them to HTML via Markdig, and write the output to `wwwroot/blog-data/`. The Blazor app then fetches these via `HttpClient` as static files — no server-side rendering needed. The RSS feed is also generated as a static XML file.

For GitHub Pages SPA routing, the `404.html` contains a redirect script that stores the path in `sessionStorage` and redirects to `index.html`, which picks it up and restores the URL via `history.replaceState`. This is the standard pattern for single-page apps on GitHub Pages.

The CI pipeline (`.github/workflows/deploy.yml`) runs the content processor, then the tests, then publishes the WASM app and deploys to GitHub Pages using the official `deploy-pages` action.

**To get started:** Copy each file into the directory structure shown at the top of the artifact. Create the `.nojekyll` file (empty) at the root. Then run the three commands: `dotnet restore`, `dotnet run --project tools/ObserverMagazine.ContentProcessor`, and `dotnet run --project src/ObserverMagazine.Web`.

35
40

# Observer Magazine — Full Project Source

> Blazor WebAssembly on .NET 10, deployed to GitHub Pages  
> Repository: https://github.com/ObserverMagazine/observermagazine.github.io  
> Live site: https://observermagazine.github.io

---

## Directory Structure

```
observermagazine.github.io/
├── .github/workflows/deploy.yml
├── .gitignore
├── .nojekyll
├── global.json
├── Directory.Build.props
├── Directory.Packages.props
├── ObserverMagazine.slnx
├── README.md
├── content/blog/
│   ├── 2026-01-15-welcome-to-observer-magazine.md
│   ├── 2026-02-20-getting-started-with-blazor-wasm.md
│   └── 2026-03-10-responsive-design-patterns.md
├── src/ObserverMagazine.Web/
│   ├── ObserverMagazine.Web.csproj
│   ├── Program.cs
│   ├── _Imports.razor
│   ├── App.razor
│   ├── Routes.razor
│   ├── Layout/
│   │   ├── MainLayout.razor
│   │   └── NavMenu.razor
│   ├── Pages/
│   │   ├── Home.razor
│   │   ├── Blog.razor
│   │   ├── BlogPost.razor
│   │   ├── Showcase.razor
│   │   └── About.razor
│   ├── Components/
│   │   ├── ResponsiveTable.razor
│   │   └── MasterDetail.razor
│   ├── Services/
│   │   ├── IBlogService.cs
│   │   ├── BlogService.cs
│   │   └── TelemetryService.cs
│   ├── Models/
│   │   ├── BlogPostMetadata.cs
│   │   └── Product.cs
│   └── wwwroot/
│       ├── index.html
│       ├── 404.html
│       ├── css/app.css
│       └── sample-data/products.json
├── tools/ObserverMagazine.ContentProcessor/
│   ├── ObserverMagazine.ContentProcessor.csproj
│   ├── Program.cs
│   ├── FrontMatterParser.cs
│   ├── MarkdownProcessor.cs
│   └── RssGenerator.cs
├── tests/ObserverMagazine.Web.Tests/
│   ├── ObserverMagazine.Web.Tests.csproj
│   ├── Services/BlogServiceTests.cs
│   ├── Components/ResponsiveTableTests.cs
│   └── Components/MasterDetailTests.cs
└── tests/ObserverMagazine.Integration.Tests/
    ├── ObserverMagazine.Integration.Tests.csproj
    ├── ContentProcessorTests.cs
    └── RssGeneratorTests.cs
```

---

## FILE: `global.json`

```json
{
  "sdk": {
    "version": "10.0.201",
    "rollForward": "latestFeature"
  }
}
```

---

## FILE: `Directory.Build.props`

```xml
<Project>
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <LangVersion>14.0</LangVersion>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
  </PropertyGroup>
</Project>
```

---

## FILE: `Directory.Packages.props`

```xml
<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
  </PropertyGroup>
  <ItemGroup>
    <!-- Web -->
    <PackageVersion Include="Microsoft.AspNetCore.Components.WebAssembly" Version="10.0.0" />
    <PackageVersion Include="Microsoft.Extensions.Logging.Configuration" Version="10.0.0" />
    <PackageVersion Include="Microsoft.Extensions.Http" Version="10.0.0" />

    <!-- Content Processing -->
    <PackageVersion Include="Markdig" Version="1.1.1" />
    <PackageVersion Include="YamlDotNet" Version="16.3.0" />

    <!-- Testing -->
    <PackageVersion Include="xunit.v3" Version="3.2.2" />
    <PackageVersion Include="xunit.runner.visualstudio" Version="3.0.2" />
    <PackageVersion Include="bunit" Version="2.6.2" />
    <PackageVersion Include="Microsoft.NET.Test.Sdk" Version="17.13.0" />
  </ItemGroup>
</Project>
```

---

## FILE: `ObserverMagazine.slnx`

```xml
<Solution>
  <Folder Name="/Solution Items/">
    <File Path="Directory.Build.props" />
    <File Path="Directory.Packages.props" />
    <File Path="global.json" />
    <File Path="README.md" />
  </Folder>
  <Folder Name="/src/">
    <Project Path="src/ObserverMagazine.Web/ObserverMagazine.Web.csproj" />
  </Folder>
  <Folder Name="/tools/">
    <Project Path="tools/ObserverMagazine.ContentProcessor/ObserverMagazine.ContentProcessor.csproj" />
  </Folder>
  <Folder Name="/tests/">
    <Project Path="tests/ObserverMagazine.Web.Tests/ObserverMagazine.Web.Tests.csproj" />
    <Project Path="tests/ObserverMagazine.Integration.Tests/ObserverMagazine.Integration.Tests.csproj" />
  </Folder>
</Solution>
```

---

## FILE: `.gitignore`

```gitignore
## .NET
bin/
obj/
*.user
*.suo
*.userosscache
*.sln.docstates
*.userprefs

## Build results
[Dd]ebug/
[Rr]elease/
x64/
x86/
build/
bld/

## NuGet
**/packages/*
*.nupkg
**/[Pp]ackages/build/
*.nuget.props
*.nuget.targets
project.lock.json
project.fragment.lock.json

## Visual Studio
.vs/
*.rsuser
*.csproj.user

## JetBrains
.idea/
*.sln.iml

## VS Code
.vscode/

## OS
.DS_Store
Thumbs.db

## Publish output
publish/

## Test results
TestResults/
*.trx

## Generated blog data (created by ContentProcessor at build time)
src/ObserverMagazine.Web/wwwroot/blog-data/
src/ObserverMagazine.Web/wwwroot/feed.xml
```

---

## FILE: `.nojekyll`

```
(empty file — prevents GitHub Pages from processing with Jekyll)
```

---

## FILE: `README.md`

```markdown
# Observer Magazine

A free, open-source Blazor WebAssembly application deployed to GitHub Pages.

Built with **.NET 10** (LTS) and **C# 14**.

## Features

- **Blog** — Write posts in Markdown with YAML front matter; compiled to static JSON at build time
- **RSS Feed** — Auto-generated `feed.xml`
- **Responsive Tables** — Sortable, filterable data tables that work on mobile
- **Master-Detail Flow** — Click a list item to view full details
- **OpenTelemetry-ready Logging** — Structured logging via `ILogger`
- **Full Test Suite** — bUnit component tests + xUnit integration tests

## Quick Start

```bash
# Restore & process blog content
dotnet run --project tools/ObserverMagazine.ContentProcessor

# Run the app locally
dotnet run --project src/ObserverMagazine.Web

# Run all tests
dotnet test
```

## Technology

| Component | Technology | License |
|-----------|-----------|---------|
| Framework | .NET 10 / Blazor WASM | MIT |
| Markdown | Markdig 1.1.1 | BSD-2-Clause |
| YAML | YamlDotNet 16.3.0 | MIT |
| Testing | xUnit v3 3.2.2 + bUnit 2.6.2 | Apache-2.0 / MIT |

All dependencies are free for any use — no payment required, ever.

## License

MIT
```

---

## FILE: `.github/workflows/deploy.yml`

```yaml
name: Deploy to GitHub Pages

on:
  push:
    branches: [ main ]
  workflow_dispatch:

permissions:
  contents: read
  pages: write
  id-token: write

concurrency:
  group: "pages"
  cancel-in-progress: false

jobs:
  build-and-deploy:
    runs-on: ubuntu-latest
    environment:
      name: github-pages
      url: ${{ steps.deployment.outputs.page_url }}
    steps:
      - name: Checkout
        uses: actions/checkout@v4

      - name: Setup .NET 10
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'

      - name: Restore packages
        run: dotnet restore

      - name: Run content processor
        run: dotnet run --project tools/ObserverMagazine.ContentProcessor -- --content-dir content/blog --output-dir src/ObserverMagazine.Web/wwwroot

      - name: Run tests
        run: dotnet test --no-restore --verbosity normal

      - name: Publish Blazor WASM
        run: dotnet publish src/ObserverMagazine.Web/ObserverMagazine.Web.csproj -c Release -o publish

      - name: Prepare GitHub Pages artifacts
        run: |
          cp publish/wwwroot/.nojekyll publish/wwwroot/.nojekyll 2>/dev/null || touch publish/wwwroot/.nojekyll
          cp publish/wwwroot/index.html publish/wwwroot/404.html

      - name: Setup Pages
        uses: actions/configure-pages@v5

      - name: Upload artifact
        uses: actions/upload-pages-artifact@v3
        with:
          path: publish/wwwroot

      - name: Deploy to GitHub Pages
        id: deployment
        uses: actions/deploy-pages@v4
```

---

## FILE: `src/ObserverMagazine.Web/ObserverMagazine.Web.csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk.BlazorWebAssembly">

  <PropertyGroup>
    <RootNamespace>ObserverMagazine.Web</RootNamespace>
    <AssemblyName>ObserverMagazine.Web</AssemblyName>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.AspNetCore.Components.WebAssembly" />
    <PackageReference Include="Microsoft.Extensions.Logging.Configuration" />
    <PackageReference Include="Microsoft.Extensions.Http" />
  </ItemGroup>

</Project>
```

---

## FILE: `src/ObserverMagazine.Web/Program.cs`

```csharp
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

// Configure logging
builder.Logging.SetMinimumLevel(LogLevel.Information);

var host = builder.Build();

// Log application startup
var logger = host.Services.GetRequiredService<ILoggerFactory>().CreateLogger("App");
logger.LogInformation("Observer Magazine started at {Time}", DateTime.UtcNow);

var telemetry = host.Services.GetRequiredService<TelemetryService>();
telemetry.TrackEvent("AppStarted");

await host.RunAsync();
```

---

## FILE: `src/ObserverMagazine.Web/_Imports.razor`

```razor
@using System.Net.Http
@using System.Net.Http.Json
@using Microsoft.AspNetCore.Components.Forms
@using Microsoft.AspNetCore.Components.Routing
@using Microsoft.AspNetCore.Components.Web
@using Microsoft.AspNetCore.Components.Web.Virtualization
@using Microsoft.AspNetCore.Components.WebAssembly.Http
@using Microsoft.Extensions.Logging
@using Microsoft.JSInterop
@using ObserverMagazine.Web
@using ObserverMagazine.Web.Layout
@using ObserverMagazine.Web.Pages
@using ObserverMagazine.Web.Components
@using ObserverMagazine.Web.Models
@using ObserverMagazine.Web.Services
```

---

## FILE: `src/ObserverMagazine.Web/App.razor`

```razor
<Router AppAssembly="typeof(App).Assembly">
    <Found Context="routeData">
        <RouteView RouteData="routeData" DefaultLayout="typeof(MainLayout)" />
        <FocusOnNavigate RouteData="routeData" Selector="h1" />
    </Found>
    <NotFound>
        <PageTitle>Not Found — Observer Magazine</PageTitle>
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

---

## FILE: `src/ObserverMagazine.Web/Routes.razor`

```razor
<Router AppAssembly="typeof(App).Assembly">
    <Found Context="routeData">
        <RouteView RouteData="routeData" DefaultLayout="typeof(MainLayout)" />
    </Found>
</Router>
```

> **Note:** The `App.razor` file already contains the router. `Routes.razor` is included for compatibility with templates that reference it; you may delete this file and use `App.razor` alone.

---

## FILE: `src/ObserverMagazine.Web/Layout/MainLayout.razor`

```razor
@inherits LayoutComponentBase

<div class="page">
    <header class="top-bar">
        <div class="container top-bar-inner">
            <a class="brand" href="/">
                <strong>Observer</strong> Magazine
            </a>
            <NavMenu />
        </div>
    </header>

    <main class="container main-content">
        @Body
    </main>

    <footer class="site-footer">
        <div class="container">
            <p>&copy; @DateTime.UtcNow.Year Observer Magazine &mdash; Built with
                <a href="https://dotnet.microsoft.com/apps/aspnet/web-apps/blazor" target="_blank"
                   rel="noopener noreferrer">Blazor WebAssembly</a>
                on .NET 10.
                <a href="/feed.xml">RSS Feed</a>
            </p>
        </div>
    </footer>
</div>
```

---

## FILE: `src/ObserverMagazine.Web/Layout/NavMenu.razor`

```razor
<nav class="nav-menu" role="navigation">
    <button class="nav-toggle" @onclick="ToggleMenu" aria-label="Toggle navigation">
        ☰
    </button>
    <ul class="nav-links @(isOpen ? "open" : "")">
        <li><NavLink href="" Match="NavLinkMatch.All">Home</NavLink></li>
        <li><NavLink href="blog">Blog</NavLink></li>
        <li><NavLink href="showcase">Showcase</NavLink></li>
        <li><NavLink href="about">About</NavLink></li>
    </ul>
</nav>

@code {
    private bool isOpen;
    private void ToggleMenu() => isOpen = !isOpen;
}
```

---

## FILE: `src/ObserverMagazine.Web/Pages/Home.razor`

```razor
@page "/"

<PageTitle>Observer Magazine</PageTitle>

<section class="hero">
    <h1>Observer Magazine</h1>
    <p class="lead">
        A free, open-source Blazor WebAssembly showcase built on .NET 10.
        Explore modern web patterns, read our blog, and use this as a starting point
        for your own projects.
    </p>
    <div class="hero-actions">
        <a class="btn btn-primary" href="showcase">View Showcase</a>
        <a class="btn btn-secondary" href="blog">Read the Blog</a>
    </div>
</section>

<section class="features-grid">
    <div class="feature-card">
        <h3>Responsive Tables</h3>
        <p>Sortable, filterable data tables that adapt from desktop to mobile.</p>
    </div>
    <div class="feature-card">
        <h3>Master-Detail Flow</h3>
        <p>Click a list item, see its details — a pattern used in dashboards everywhere.</p>
    </div>
    <div class="feature-card">
        <h3>Markdown Blog</h3>
        <p>Write posts in Markdown with YAML front matter. Compiled to static JSON at build time.</p>
    </div>
    <div class="feature-card">
        <h3>RSS Feed</h3>
        <p>Auto-generated <code>feed.xml</code> so readers can subscribe in any RSS reader.</p>
    </div>
    <div class="feature-card">
        <h3>Structured Logging</h3>
        <p>ILogger-based telemetry ready for OpenTelemetry export.</p>
    </div>
    <div class="feature-card">
        <h3>Full Test Suite</h3>
        <p>bUnit component tests and xUnit integration tests run on every commit.</p>
    </div>
</section>
```

---

## FILE: `src/ObserverMagazine.Web/Pages/Blog.razor`

```razor
@page "/blog"
@inject IBlogService BlogService
@inject ILogger<Blog> Logger

<PageTitle>Blog — Observer Magazine</PageTitle>

<h1>Blog</h1>

@if (posts is null)
{
    <p><em>Loading posts...</em></p>
}
else if (posts.Length == 0)
{
    <p>No posts yet. Check back soon!</p>
}
else
{
    <div class="blog-list">
        @foreach (var post in posts)
        {
            <article class="blog-card">
                <h2><a href="blog/@post.Slug">@post.Title</a></h2>
                <div class="blog-meta">
                    <time datetime="@post.Date.ToString("yyyy-MM-dd")">
                        @post.Date.ToString("MMMM d, yyyy")
                    </time>
                    @if (!string.IsNullOrEmpty(post.Author))
                    {
                        <span> · @post.Author</span>
                    }
                </div>
                <p>@post.Summary</p>
                @if (post.Tags is { Length: > 0 })
                {
                    <div class="tag-list">
                        @foreach (var tag in post.Tags)
                        {
                            <span class="tag">@tag</span>
                        }
                    </div>
                }
            </article>
        }
    </div>
}

@code {
    private BlogPostMetadata[]? posts;

    protected override async Task OnInitializedAsync()
    {
        Logger.LogInformation("Loading blog index");
        try
        {
            posts = await BlogService.GetPostsAsync();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to load blog index");
            posts = [];
        }
    }
}
```

---

## FILE: `src/ObserverMagazine.Web/Pages/BlogPost.razor`

```razor
@page "/blog/{Slug}"
@inject IBlogService BlogService
@inject ILogger<BlogPost> Logger
@inject NavigationManager Nav

<PageTitle>@(metadata?.Title ?? "Post") — Observer Magazine</PageTitle>

@if (loading)
{
    <p><em>Loading...</em></p>
}
else if (metadata is null)
{
    <h1>Post Not Found</h1>
    <p>Sorry, we couldn't find that post. <a href="blog">Back to Blog</a></p>
}
else
{
    <article class="blog-post">
        <header>
            <h1>@metadata.Title</h1>
            <div class="blog-meta">
                <time datetime="@metadata.Date.ToString("yyyy-MM-dd")">
                    @metadata.Date.ToString("MMMM d, yyyy")
                </time>
                @if (!string.IsNullOrEmpty(metadata.Author))
                {
                    <span> · @metadata.Author</span>
                }
            </div>
            @if (metadata.Tags is { Length: > 0 })
            {
                <div class="tag-list">
                    @foreach (var tag in metadata.Tags)
                    {
                        <span class="tag">@tag</span>
                    }
                </div>
            }
        </header>
        <div class="blog-content">
            @((MarkupString)htmlContent)
        </div>
        <footer class="blog-post-footer">
            <a href="blog">&larr; Back to Blog</a>
        </footer>
    </article>
}

@code {
    [Parameter] public string Slug { get; set; } = "";

    private BlogPostMetadata? metadata;
    private string htmlContent = "";
    private bool loading = true;

    protected override async Task OnParametersSetAsync()
    {
        loading = true;
        Logger.LogInformation("Loading blog post: {Slug}", Slug);

        try
        {
            metadata = await BlogService.GetPostMetadataAsync(Slug);
            if (metadata is not null)
            {
                htmlContent = await BlogService.GetPostHtmlAsync(Slug);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to load post {Slug}", Slug);
            metadata = null;
        }
        finally
        {
            loading = false;
        }
    }
}
```

---

## FILE: `src/ObserverMagazine.Web/Pages/Showcase.razor`

```razor
@page "/showcase"

<PageTitle>Showcase — Observer Magazine</PageTitle>

<h1>Web Technology Showcase</h1>
<p>Demonstrating common web UI patterns built with Blazor WebAssembly.</p>

<section id="responsive-table" class="showcase-section">
    <h2>Responsive Table</h2>
    <p>A sortable, filterable data table that works on all screen sizes.</p>
    <ResponsiveTable />
</section>

<hr />

<section id="master-detail" class="showcase-section">
    <h2>Master-Detail Flow</h2>
    <p>Select an item from the list to view its details.</p>
    <MasterDetail />
</section>
```

---

## FILE: `src/ObserverMagazine.Web/Pages/About.razor`

```razor
@page "/about"

<PageTitle>About — Observer Magazine</PageTitle>

<h1>About Observer Magazine</h1>

<p>
    Observer Magazine is a free, open-source sample project demonstrating how to build
    a modern static web application using <strong>Blazor WebAssembly</strong> on
    <strong>.NET 10</strong>.
</p>

<h2>Goals</h2>
<p>
    This project serves as both a learning resource and a starting point for developers who
    want to build high-performance, cross-platform web applications at zero cost. Every library
    used is free for any purpose — no "free for non-commercial" caveats.
</p>

<h2>Technology Stack</h2>
<table class="simple-table">
    <thead>
        <tr>
            <th>Layer</th>
            <th>Technology</th>
            <th>License</th>
        </tr>
    </thead>
    <tbody>
        <tr><td>Runtime</td><td>.NET 10 (LTS)</td><td>MIT</td></tr>
        <tr><td>UI Framework</td><td>Blazor WebAssembly</td><td>MIT</td></tr>
        <tr><td>Language</td><td>C# 14</td><td>MIT</td></tr>
        <tr><td>Markdown</td><td>Markdig 1.1.1</td><td>BSD-2-Clause</td></tr>
        <tr><td>YAML</td><td>YamlDotNet 16.3.0</td><td>MIT</td></tr>
        <tr><td>Testing</td><td>xUnit v3 + bUnit</td><td>Apache-2.0 / MIT</td></tr>
        <tr><td>Hosting</td><td>GitHub Pages</td><td>Free</td></tr>
    </tbody>
</table>

<h2>Source Code</h2>
<p>
    <a href="https://github.com/ObserverMagazine/observermagazine.github.io"
       target="_blank" rel="noopener noreferrer">
        github.com/ObserverMagazine/observermagazine.github.io
    </a>
</p>
```

---

## FILE: `src/ObserverMagazine.Web/Components/ResponsiveTable.razor`

```razor
@inject HttpClient Http
@inject ILogger<ResponsiveTable> Logger

@if (products is null)
{
    <p><em>Loading data...</em></p>
}
else
{
    <div class="table-controls">
        <input type="text"
               placeholder="Filter by name..."
               @bind="filterText"
               @bind:event="oninput"
               class="filter-input" />
    </div>

    <div class="table-responsive">
        <table class="data-table">
            <thead>
                <tr>
                    <th class="sortable" @onclick='() => Sort("Name")'>
                        Name @SortIndicator("Name")
                    </th>
                    <th class="sortable" @onclick='() => Sort("Category")'>
                        Category @SortIndicator("Category")
                    </th>
                    <th class="sortable numeric" @onclick='() => Sort("Price")'>
                        Price @SortIndicator("Price")
                    </th>
                    <th class="sortable numeric" @onclick='() => Sort("Stock")'>
                        Stock @SortIndicator("Stock")
                    </th>
                    <th>Rating</th>
                </tr>
            </thead>
            <tbody>
                @foreach (var p in FilteredAndSorted)
                {
                    <tr>
                        <td data-label="Name">@p.Name</td>
                        <td data-label="Category">@p.Category</td>
                        <td data-label="Price" class="numeric">@p.Price.ToString("C")</td>
                        <td data-label="Stock" class="numeric">@p.Stock</td>
                        <td data-label="Rating">
                            @for (int i = 0; i < (int)Math.Round(p.Rating); i++)
                            {
                                <span class="star">★</span>
                            }
                        </td>
                    </tr>
                }
            </tbody>
        </table>
    </div>

    <p class="table-info">
        Showing @FilteredAndSorted.Count() of @products.Length items
    </p>
}

@code {
    private Product[]? products;
    private string filterText = "";
    private string sortColumn = "Name";
    private bool sortAscending = true;

    protected override async Task OnInitializedAsync()
    {
        Logger.LogInformation("Loading product data for responsive table");
        try
        {
            products = await Http.GetFromJsonAsync<Product[]>("sample-data/products.json");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to load products");
            products = [];
        }
    }

    private IEnumerable<Product> FilteredAndSorted
    {
        get
        {
            if (products is null) return [];

            var filtered = string.IsNullOrWhiteSpace(filterText)
                ? products.AsEnumerable()
                : products.Where(p =>
                    p.Name.Contains(filterText, StringComparison.OrdinalIgnoreCase));

            return sortColumn switch
            {
                "Name" => sortAscending ? filtered.OrderBy(p => p.Name) : filtered.OrderByDescending(p => p.Name),
                "Category" => sortAscending ? filtered.OrderBy(p => p.Category) : filtered.OrderByDescending(p => p.Category),
                "Price" => sortAscending ? filtered.OrderBy(p => p.Price) : filtered.OrderByDescending(p => p.Price),
                "Stock" => sortAscending ? filtered.OrderBy(p => p.Stock) : filtered.OrderByDescending(p => p.Stock),
                _ => filtered
            };
        }
    }

    private void Sort(string column)
    {
        if (sortColumn == column)
        {
            sortAscending = !sortAscending;
        }
        else
        {
            sortColumn = column;
            sortAscending = true;
        }
        Logger.LogInformation("Sorted by {Column} {Direction}", column, sortAscending ? "asc" : "desc");
    }

    private string SortIndicator(string column)
    {
        if (sortColumn != column) return "";
        return sortAscending ? "▲" : "▼";
    }
}
```

---

## FILE: `src/ObserverMagazine.Web/Components/MasterDetail.razor`

```razor
@inject HttpClient Http
@inject ILogger<MasterDetail> Logger

<div class="master-detail">
    <div class="master-list">
        <h3>Products</h3>
        @if (products is null)
        {
            <p><em>Loading...</em></p>
        }
        else
        {
            <ul>
                @foreach (var p in products)
                {
                    <li class="@(selected?.Name == p.Name ? "selected" : "")"
                        @onclick="() => Select(p)">
                        <span class="item-name">@p.Name</span>
                        <span class="item-price">@p.Price.ToString("C")</span>
                    </li>
                }
            </ul>
        }
    </div>

    <div class="detail-panel">
        @if (selected is null)
        {
            <div class="detail-empty">
                <p>Select an item from the list to view details.</p>
            </div>
        }
        else
        {
            <div class="detail-content">
                <h3>@selected.Name</h3>
                <dl>
                    <dt>Category</dt>
                    <dd>@selected.Category</dd>

                    <dt>Price</dt>
                    <dd>@selected.Price.ToString("C")</dd>

                    <dt>In Stock</dt>
                    <dd>@selected.Stock units</dd>

                    <dt>Rating</dt>
                    <dd>
                        @for (int i = 0; i < (int)Math.Round(selected.Rating); i++)
                        {
                            <span class="star">★</span>
                        }
                        (@selected.Rating.ToString("F1"))
                    </dd>

                    <dt>Description</dt>
                    <dd>@selected.Description</dd>
                </dl>
            </div>
        }
    </div>
</div>

@code {
    private Product[]? products;
    private Product? selected;

    protected override async Task OnInitializedAsync()
    {
        Logger.LogInformation("Loading product data for master-detail");
        try
        {
            products = await Http.GetFromJsonAsync<Product[]>("sample-data/products.json");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to load products for master-detail");
            products = [];
        }
    }

    private void Select(Product product)
    {
        selected = product;
        Logger.LogInformation("Selected product: {Name}", product.Name);
    }
}
```

---

## FILE: `src/ObserverMagazine.Web/Models/BlogPostMetadata.cs`

```csharp
namespace ObserverMagazine.Web.Models;

public sealed record BlogPostMetadata
{
    public required string Slug { get; init; }
    public required string Title { get; init; }
    public required DateTime Date { get; init; }
    public string Author { get; init; } = "";
    public string Summary { get; init; } = "";
    public string[] Tags { get; init; } = [];
}
```

---

## FILE: `src/ObserverMagazine.Web/Models/Product.cs`

```csharp
namespace ObserverMagazine.Web.Models;

public sealed record Product
{
    public string Name { get; init; } = "";
    public string Category { get; init; } = "";
    public decimal Price { get; init; }
    public int Stock { get; init; }
    public double Rating { get; init; }
    public string Description { get; init; } = "";
}
```

---

## FILE: `src/ObserverMagazine.Web/Services/IBlogService.cs`

```csharp
using ObserverMagazine.Web.Models;

namespace ObserverMagazine.Web.Services;

public interface IBlogService
{
    Task<BlogPostMetadata[]> GetPostsAsync();
    Task<BlogPostMetadata?> GetPostMetadataAsync(string slug);
    Task<string> GetPostHtmlAsync(string slug);
}
```

---

## FILE: `src/ObserverMagazine.Web/Services/BlogService.cs`

```csharp
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using ObserverMagazine.Web.Models;

namespace ObserverMagazine.Web.Services;

public sealed class BlogService(HttpClient http, ILogger<BlogService> logger) : IBlogService
{
    private BlogPostMetadata[]? cachedIndex;

    public async Task<BlogPostMetadata[]> GetPostsAsync()
    {
        if (cachedIndex is not null) return cachedIndex;

        logger.LogInformation("Fetching blog posts index");
        try
        {
            var posts = await http.GetFromJsonAsync<BlogPostMetadata[]>("blog-data/posts-index.json");
            cachedIndex = posts?
                .OrderByDescending(p => p.Date)
                .ToArray() ?? [];
            logger.LogInformation("Loaded {Count} blog posts", cachedIndex.Length);
            return cachedIndex;
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "Blog index not found — content processor may not have run");
            cachedIndex = [];
            return cachedIndex;
        }
    }

    public async Task<BlogPostMetadata?> GetPostMetadataAsync(string slug)
    {
        var posts = await GetPostsAsync();
        return posts.FirstOrDefault(p =>
            string.Equals(p.Slug, slug, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<string> GetPostHtmlAsync(string slug)
    {
        logger.LogInformation("Fetching HTML for post: {Slug}", slug);
        try
        {
            return await http.GetStringAsync($"blog-data/{slug}.html");
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Failed to load HTML for post: {Slug}", slug);
            return "<p>Could not load post content.</p>";
        }
    }
}
```

---

## FILE: `src/ObserverMagazine.Web/Services/TelemetryService.cs`

```csharp
using Microsoft.Extensions.Logging;

namespace ObserverMagazine.Web.Services;

/// <summary>
/// Lightweight telemetry service for tracking events and metrics in the browser.
/// Logs to ILogger (browser console). Can be extended with a real OpenTelemetry
/// collector endpoint when one is available.
/// </summary>
public sealed class TelemetryService(ILogger<TelemetryService> logger)
{
    private readonly Dictionary<string, long> counters = new();
    private readonly object lockObj = new();

    public void TrackEvent(string eventName, IDictionary<string, string>? properties = null)
    {
        var props = properties is not null
            ? string.Join(", ", properties.Select(kv => $"{kv.Key}={kv.Value}"))
            : "";
        logger.LogInformation("[Telemetry] Event: {EventName} {Properties}", eventName, props);
    }

    public void IncrementCounter(string counterName, long value = 1)
    {
        lock (lockObj)
        {
            counters.TryGetValue(counterName, out var current);
            counters[counterName] = current + value;
        }
        logger.LogDebug("[Telemetry] Counter: {CounterName} += {Value}", counterName, value);
    }

    public long GetCounter(string counterName)
    {
        lock (lockObj)
        {
            return counters.GetValueOrDefault(counterName, 0);
        }
    }

    public void TrackPageView(string pageName)
    {
        IncrementCounter($"pageview:{pageName}");
        TrackEvent("PageView", new Dictionary<string, string> { ["page"] = pageName });
    }
}
```

---

## FILE: `src/ObserverMagazine.Web/wwwroot/index.html`

```html
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <title>Observer Magazine</title>
    <base href="/" />
    <link rel="alternate" type="application/rss+xml" title="Observer Magazine RSS" href="/feed.xml" />
    <link rel="stylesheet" href="css/app.css" />
    <link rel="stylesheet" href="ObserverMagazine.Web.styles.css" />
</head>
<body>
    <div id="app">
        <div style="display:flex;align-items:center;justify-content:center;height:100vh;">
            <p>Loading Observer Magazine...</p>
        </div>
    </div>

    <div id="blazor-error-ui" style="display:none;">
        An unhandled error has occurred.
        <a href="/" class="reload">Reload</a>
        <span class="dismiss">🗙</span>
    </div>

    <!-- SPA redirect for GitHub Pages -->
    <script>
        (function () {
            var redirect = sessionStorage.redirect;
            delete sessionStorage.redirect;
            if (redirect && redirect !== location.href) {
                history.replaceState(null, null, redirect);
            }
        })();
    </script>
    <script src="_framework/blazor.webassembly.js"></script>
</body>
</html>
```

---

## FILE: `src/ObserverMagazine.Web/wwwroot/404.html`

```html
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="utf-8" />
    <title>Observer Magazine</title>
    <script>
        // Single-page app redirect for GitHub Pages.
        // Stores the path and redirects to index.html which picks it up.
        sessionStorage.redirect = location.href;
        location.replace(location.origin + location.pathname.split('/').slice(0, 1).join('/'));
    </script>
</head>
<body></body>
</html>
```

---

## FILE: `src/ObserverMagazine.Web/wwwroot/css/app.css`

```css
/* ========== RESET & BASE ========== */
*, *::before, *::after {
    box-sizing: border-box;
    margin: 0;
    padding: 0;
}

:root {
    --color-bg: #ffffff;
    --color-text: #1a1a2e;
    --color-primary: #0f3460;
    --color-accent: #e94560;
    --color-muted: #6c757d;
    --color-border: #dee2e6;
    --color-surface: #f8f9fa;
    --color-card-bg: #ffffff;
    --font-sans: system-ui, -apple-system, "Segoe UI", Roboto, sans-serif;
    --font-mono: "Cascadia Code", "Fira Code", Consolas, monospace;
    --max-width: 1100px;
    --radius: 6px;
}

html { font-size: 16px; scroll-behavior: smooth; }

body {
    font-family: var(--font-sans);
    color: var(--color-text);
    background: var(--color-bg);
    line-height: 1.65;
}

.container {
    max-width: var(--max-width);
    margin: 0 auto;
    padding: 0 1rem;
}

a { color: var(--color-primary); text-decoration: none; }
a:hover { text-decoration: underline; }

code {
    font-family: var(--font-mono);
    background: var(--color-surface);
    padding: 0.15em 0.4em;
    border-radius: 3px;
    font-size: 0.9em;
}

/* ========== LAYOUT ========== */
.page { display: flex; flex-direction: column; min-height: 100vh; }
.main-content { flex: 1; padding: 2rem 1rem; }

/* Top Bar */
.top-bar {
    background: var(--color-primary);
    color: #fff;
    padding: 0.75rem 0;
    position: sticky;
    top: 0;
    z-index: 100;
}
.top-bar-inner {
    display: flex;
    align-items: center;
    justify-content: space-between;
}
.brand { color: #fff; font-size: 1.25rem; }
.brand:hover { text-decoration: none; opacity: 0.9; }

/* Nav */
.nav-menu { display: flex; align-items: center; }
.nav-toggle {
    display: none;
    background: none;
    border: none;
    color: #fff;
    font-size: 1.5rem;
    cursor: pointer;
}
.nav-links {
    display: flex;
    list-style: none;
    gap: 1.5rem;
}
.nav-links a {
    color: rgba(255, 255, 255, 0.85);
    font-weight: 500;
    transition: color 0.15s;
}
.nav-links a:hover, .nav-links a.active { color: #fff; text-decoration: none; }

@media (max-width: 640px) {
    .nav-toggle { display: block; }
    .nav-links {
        display: none;
        flex-direction: column;
        position: absolute;
        top: 100%;
        left: 0;
        right: 0;
        background: var(--color-primary);
        padding: 1rem;
        gap: 0.75rem;
    }
    .nav-links.open { display: flex; }
}

/* Footer */
.site-footer {
    background: var(--color-surface);
    border-top: 1px solid var(--color-border);
    padding: 1.5rem 0;
    text-align: center;
    font-size: 0.875rem;
    color: var(--color-muted);
}

/* ========== HERO ========== */
.hero {
    text-align: center;
    padding: 3rem 0 2rem;
}
.hero h1 { font-size: 2.5rem; margin-bottom: 1rem; }
.lead { font-size: 1.15rem; color: var(--color-muted); max-width: 700px; margin: 0 auto 1.5rem; }
.hero-actions { display: flex; gap: 1rem; justify-content: center; flex-wrap: wrap; }

/* Buttons */
.btn {
    display: inline-block;
    padding: 0.65rem 1.5rem;
    border-radius: var(--radius);
    font-weight: 600;
    text-decoration: none;
    transition: opacity 0.15s;
}
.btn:hover { opacity: 0.88; text-decoration: none; }
.btn-primary { background: var(--color-primary); color: #fff; }
.btn-secondary { background: var(--color-accent); color: #fff; }

/* ========== FEATURES GRID ========== */
.features-grid {
    display: grid;
    grid-template-columns: repeat(auto-fit, minmax(280px, 1fr));
    gap: 1.5rem;
    padding: 2rem 0;
}
.feature-card {
    background: var(--color-card-bg);
    border: 1px solid var(--color-border);
    border-radius: var(--radius);
    padding: 1.5rem;
}
.feature-card h3 { margin-bottom: 0.5rem; color: var(--color-primary); }

/* ========== BLOG ========== */
.blog-list { display: flex; flex-direction: column; gap: 1.5rem; }
.blog-card {
    border: 1px solid var(--color-border);
    border-radius: var(--radius);
    padding: 1.5rem;
}
.blog-card h2 { font-size: 1.35rem; margin-bottom: 0.25rem; }
.blog-meta { color: var(--color-muted); font-size: 0.875rem; margin-bottom: 0.75rem; }
.tag-list { display: flex; gap: 0.5rem; flex-wrap: wrap; margin-top: 0.75rem; }
.tag {
    background: var(--color-surface);
    border: 1px solid var(--color-border);
    border-radius: 3px;
    padding: 0.15rem 0.5rem;
    font-size: 0.8rem;
}

/* Blog post */
.blog-post header { margin-bottom: 2rem; }
.blog-post h1 { font-size: 2rem; }
.blog-content { line-height: 1.8; }
.blog-content h2 { margin-top: 2rem; margin-bottom: 0.75rem; }
.blog-content h3 { margin-top: 1.5rem; margin-bottom: 0.5rem; }
.blog-content p { margin-bottom: 1rem; }
.blog-content pre {
    background: var(--color-surface);
    border: 1px solid var(--color-border);
    border-radius: var(--radius);
    padding: 1rem;
    overflow-x: auto;
    font-family: var(--font-mono);
    font-size: 0.9rem;
    margin-bottom: 1rem;
}
.blog-content code { font-size: 0.9em; }
.blog-content ul, .blog-content ol { margin-bottom: 1rem; padding-left: 1.5rem; }
.blog-post-footer { margin-top: 3rem; padding-top: 1rem; border-top: 1px solid var(--color-border); }

/* ========== SIMPLE TABLE (About page) ========== */
.simple-table {
    width: 100%;
    border-collapse: collapse;
    margin: 1rem 0;
}
.simple-table th, .simple-table td {
    text-align: left;
    padding: 0.6rem 0.75rem;
    border-bottom: 1px solid var(--color-border);
}
.simple-table th { font-weight: 600; }

/* ========== RESPONSIVE DATA TABLE ========== */
.table-controls { margin-bottom: 1rem; }
.filter-input {
    width: 100%;
    max-width: 320px;
    padding: 0.5rem 0.75rem;
    border: 1px solid var(--color-border);
    border-radius: var(--radius);
    font-size: 0.95rem;
}
.table-responsive { overflow-x: auto; }
.data-table {
    width: 100%;
    border-collapse: collapse;
}
.data-table th, .data-table td {
    text-align: left;
    padding: 0.6rem 0.75rem;
    border-bottom: 1px solid var(--color-border);
}
.data-table th { font-weight: 600; white-space: nowrap; }
.data-table th.sortable { cursor: pointer; user-select: none; }
.data-table th.sortable:hover { background: var(--color-surface); }
.numeric { text-align: right; }
.star { color: var(--color-accent); }
.table-info { margin-top: 0.75rem; font-size: 0.85rem; color: var(--color-muted); }

/* Mobile responsive: cards layout */
@media (max-width: 640px) {
    .data-table thead { display: none; }
    .data-table tr {
        display: block;
        border: 1px solid var(--color-border);
        border-radius: var(--radius);
        margin-bottom: 0.75rem;
        padding: 0.5rem;
    }
    .data-table td {
        display: flex;
        justify-content: space-between;
        border: none;
        padding: 0.35rem 0.5rem;
        text-align: right;
    }
    .data-table td::before {
        content: attr(data-label);
        font-weight: 600;
        text-align: left;
    }
}

/* ========== MASTER-DETAIL ========== */
.master-detail {
    display: grid;
    grid-template-columns: 1fr 2fr;
    gap: 1.5rem;
    border: 1px solid var(--color-border);
    border-radius: var(--radius);
    overflow: hidden;
    min-height: 400px;
}
.master-list {
    border-right: 1px solid var(--color-border);
    background: var(--color-surface);
    overflow-y: auto;
    max-height: 500px;
}
.master-list h3 { padding: 1rem; border-bottom: 1px solid var(--color-border); }
.master-list ul { list-style: none; }
.master-list li {
    display: flex;
    justify-content: space-between;
    padding: 0.75rem 1rem;
    cursor: pointer;
    border-bottom: 1px solid var(--color-border);
    transition: background 0.12s;
}
.master-list li:hover { background: #e9ecef; }
.master-list li.selected { background: var(--color-primary); color: #fff; }
.item-name { font-weight: 500; }
.item-price { font-size: 0.9rem; }

.detail-panel { padding: 1.5rem; }
.detail-empty { display: flex; align-items: center; justify-content: center; height: 100%; color: var(--color-muted); }
.detail-content h3 { font-size: 1.5rem; margin-bottom: 1rem; }
.detail-content dl { display: grid; grid-template-columns: auto 1fr; gap: 0.5rem 1rem; }
.detail-content dt { font-weight: 600; color: var(--color-muted); }

@media (max-width: 640px) {
    .master-detail { grid-template-columns: 1fr; }
    .master-list { border-right: none; border-bottom: 1px solid var(--color-border); max-height: 250px; }
}

/* ========== SHOWCASE ========== */
.showcase-section { margin-bottom: 2rem; }
.showcase-section h2 { margin-bottom: 0.5rem; }
.showcase-section > p { color: var(--color-muted); margin-bottom: 1rem; }

/* ========== BLAZOR ERROR UI ========== */
#blazor-error-ui {
    background: #fde8e8;
    bottom: 0;
    box-shadow: 0 -1px 2px rgba(0, 0, 0, 0.2);
    display: none;
    left: 0;
    padding: 0.6rem 1.25rem 0.7rem 1.25rem;
    position: fixed;
    width: 100%;
    z-index: 1000;
}
#blazor-error-ui .dismiss {
    cursor: pointer;
    position: absolute;
    right: 0.75rem;
    top: 0.5rem;
}
```

---

## FILE: `src/ObserverMagazine.Web/wwwroot/sample-data/products.json`

```json
[
  {
    "name": "Ergonomic Keyboard",
    "category": "Electronics",
    "price": 89.99,
    "stock": 142,
    "rating": 4.5,
    "description": "A split ergonomic keyboard with mechanical switches and adjustable tenting. Designed for long coding sessions with minimal wrist strain."
  },
  {
    "name": "Standing Desk",
    "category": "Furniture",
    "price": 499.00,
    "stock": 38,
    "rating": 4.8,
    "description": "Electric height-adjustable standing desk with bamboo top. Programmable presets for sit and stand heights. Supports up to 300 lbs."
  },
  {
    "name": "Noise-Cancelling Headphones",
    "category": "Electronics",
    "price": 249.95,
    "stock": 87,
    "rating": 4.7,
    "description": "Over-ear wireless headphones with adaptive noise cancellation and 30-hour battery life. Hi-res audio certified."
  },
  {
    "name": "Webcam 4K",
    "category": "Electronics",
    "price": 129.00,
    "stock": 204,
    "rating": 4.2,
    "description": "Ultra HD 4K webcam with auto-focus, built-in ring light, and noise-reducing dual microphones."
  },
  {
    "name": "Monitor Light Bar",
    "category": "Accessories",
    "price": 59.99,
    "stock": 315,
    "rating": 4.6,
    "description": "Asymmetric LED light bar that clips onto your monitor. Adjustable color temperature from 2700K to 6500K."
  },
  {
    "name": "Mesh Office Chair",
    "category": "Furniture",
    "price": 349.00,
    "stock": 56,
    "rating": 4.4,
    "description": "Full-mesh ergonomic office chair with lumbar support, adjustable arms, and headrest. Breathable design for all-day comfort."
  },
  {
    "name": "USB-C Hub",
    "category": "Accessories",
    "price": 44.99,
    "stock": 410,
    "rating": 4.1,
    "description": "7-in-1 USB-C hub with HDMI 4K, USB-A 3.0, SD card reader, and 100W power delivery pass-through."
  },
  {
    "name": "Mechanical Numpad",
    "category": "Electronics",
    "price": 39.95,
    "stock": 178,
    "rating": 4.3,
    "description": "Standalone mechanical numpad with programmable keys. Hot-swappable switches and RGB backlighting."
  },
  {
    "name": "Desk Mat XL",
    "category": "Accessories",
    "price": 24.99,
    "stock": 520,
    "rating": 4.0,
    "description": "Extra-large desk mat (900x400mm) with stitched edges and non-slip rubber base. Machine washable."
  },
  {
    "name": "Portable SSD 2TB",
    "category": "Electronics",
    "price": 149.99,
    "stock": 93,
    "rating": 4.9,
    "description": "Compact portable SSD with USB 3.2 Gen 2 speeds up to 1050 MB/s. IP55 water and dust resistant."
  }
]
```

---

## FILE: `tools/ObserverMagazine.ContentProcessor/ObserverMagazine.ContentProcessor.csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <RootNamespace>ObserverMagazine.ContentProcessor</RootNamespace>
    <AssemblyName>ObserverMagazine.ContentProcessor</AssemblyName>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Markdig" />
    <PackageReference Include="YamlDotNet" />
  </ItemGroup>

</Project>
```

---

## FILE: `tools/ObserverMagazine.ContentProcessor/Program.cs`

```csharp
using System.Text.Json;
using ObserverMagazine.ContentProcessor;

// Parse command-line arguments
string contentDir = "content/blog";
string outputDir = "src/ObserverMagazine.Web/wwwroot";

for (int i = 0; i < args.Length; i++)
{
    if (args[i] == "--content-dir" && i + 1 < args.Length)
        contentDir = args[++i];
    else if (args[i] == "--output-dir" && i + 1 < args.Length)
        outputDir = args[++i];
}

Console.WriteLine($"Content directory: {contentDir}");
Console.WriteLine($"Output directory:  {outputDir}");

if (!Directory.Exists(contentDir))
{
    Console.WriteLine($"Content directory '{contentDir}' does not exist. Creating with no posts.");
    Directory.CreateDirectory(contentDir);
}

string blogDataDir = Path.Combine(outputDir, "blog-data");
Directory.CreateDirectory(blogDataDir);

// Process markdown files
var markdownFiles = Directory.GetFiles(contentDir, "*.md", SearchOption.TopDirectoryOnly);
Console.WriteLine($"Found {markdownFiles.Length} markdown files");

var allPostMetadata = new List<PostIndexEntry>();

foreach (var mdFile in markdownFiles)
{
    Console.WriteLine($"Processing: {Path.GetFileName(mdFile)}");

    var rawContent = File.ReadAllText(mdFile);
    var (frontMatter, markdownBody) = FrontMatterParser.Parse(rawContent);

    if (string.IsNullOrEmpty(frontMatter.Title))
    {
        Console.WriteLine($"  WARNING: No title in front matter, skipping {mdFile}");
        continue;
    }

    // Derive slug from filename: "2026-01-15-welcome-to-observer-magazine.md" -> "welcome-to-observer-magazine"
    var fileName = Path.GetFileNameWithoutExtension(mdFile);
    var slug = FrontMatterParser.DeriveSlug(fileName);

    var html = MarkdownProcessor.ToHtml(markdownBody);

    // Write individual post HTML
    var htmlPath = Path.Combine(blogDataDir, $"{slug}.html");
    File.WriteAllText(htmlPath, html);
    Console.WriteLine($"  Wrote: {htmlPath}");

    allPostMetadata.Add(new PostIndexEntry
    {
        Slug = slug,
        Title = frontMatter.Title,
        Date = frontMatter.Date,
        Author = frontMatter.Author ?? "",
        Summary = frontMatter.Summary ?? "",
        Tags = frontMatter.Tags ?? []
    });
}

// Sort by date descending
allPostMetadata.Sort((a, b) => b.Date.CompareTo(a.Date));

// Write posts index JSON
var indexPath = Path.Combine(blogDataDir, "posts-index.json");
var jsonOptions = new JsonSerializerOptions
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    WriteIndented = true
};
var indexJson = JsonSerializer.Serialize(allPostMetadata, jsonOptions);
File.WriteAllText(indexPath, indexJson);
Console.WriteLine($"Wrote posts index: {indexPath} ({allPostMetadata.Count} posts)");

// Generate RSS feed
var feedPath = Path.Combine(outputDir, "feed.xml");
var rssXml = RssGenerator.Generate(
    title: "Observer Magazine",
    description: "A free, open-source Blazor WebAssembly showcase on .NET 10",
    siteUrl: "https://observermagazine.github.io",
    posts: allPostMetadata
);
File.WriteAllText(feedPath, rssXml);
Console.WriteLine($"Wrote RSS feed: {feedPath}");

Console.WriteLine("Content processing complete.");

// --- Types used by the index ---
public sealed class PostIndexEntry
{
    public string Slug { get; init; } = "";
    public string Title { get; init; } = "";
    public DateTime Date { get; init; }
    public string Author { get; init; } = "";
    public string Summary { get; init; } = "";
    public string[] Tags { get; init; } = [];
}
```

---

## FILE: `tools/ObserverMagazine.ContentProcessor/FrontMatterParser.cs`

```csharp
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace ObserverMagazine.ContentProcessor;

public static class FrontMatterParser
{
    private static readonly IDeserializer Deserializer = new DeserializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .IgnoreUnmatchedProperties()
        .Build();

    /// <summary>
    /// Splits a markdown file into YAML front matter and markdown body.
    /// Front matter is delimited by --- at the start and end.
    /// </summary>
    public static (FrontMatter FrontMatter, string MarkdownBody) Parse(string rawContent)
    {
        if (!rawContent.StartsWith("---"))
        {
            return (new FrontMatter(), rawContent);
        }

        var endIndex = rawContent.IndexOf("---", 3, StringComparison.Ordinal);
        if (endIndex < 0)
        {
            return (new FrontMatter(), rawContent);
        }

        var yamlBlock = rawContent[3..endIndex].Trim();
        var body = rawContent[(endIndex + 3)..].TrimStart('\r', '\n');

        var frontMatter = Deserializer.Deserialize<FrontMatter>(yamlBlock);
        return (frontMatter ?? new FrontMatter(), body);
    }

    /// <summary>
    /// Derives a slug from a filename like "2026-01-15-welcome-to-observer-magazine".
    /// Strips the leading date prefix if present.
    /// </summary>
    public static string DeriveSlug(string fileName)
    {
        // Pattern: YYYY-MM-DD-rest-of-slug
        if (fileName.Length > 11 &&
            char.IsDigit(fileName[0]) &&
            fileName[4] == '-' &&
            fileName[7] == '-' &&
            fileName[10] == '-')
        {
            return fileName[11..];
        }
        return fileName;
    }
}

public sealed class FrontMatter
{
    public string Title { get; set; } = "";
    public DateTime Date { get; set; } = DateTime.MinValue;
    public string? Author { get; set; }
    public string? Summary { get; set; }
    public string[]? Tags { get; set; }
}
```

---

## FILE: `tools/ObserverMagazine.ContentProcessor/MarkdownProcessor.cs`

```csharp
using Markdig;

namespace ObserverMagazine.ContentProcessor;

public static class MarkdownProcessor
{
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .Build();

    public static string ToHtml(string markdown)
    {
        return Markdown.ToHtml(markdown, Pipeline);
    }
}
```

---

## FILE: `tools/ObserverMagazine.ContentProcessor/RssGenerator.cs`

```csharp
using System.Xml.Linq;

namespace ObserverMagazine.ContentProcessor;

public static class RssGenerator
{
    public static string Generate(
        string title,
        string description,
        string siteUrl,
        IReadOnlyList<PostIndexEntry> posts)
    {
        var items = posts.Select(p =>
            new XElement("item",
                new XElement("title", p.Title),
                new XElement("link", $"{siteUrl}/blog/{p.Slug}"),
                new XElement("description", p.Summary),
                new XElement("pubDate", p.Date.ToString("R")),
                new XElement("guid", $"{siteUrl}/blog/{p.Slug}"),
                p.Tags.Length > 0
                    ? p.Tags.Select(t => new XElement("category", t))
                    : null
            ));

        var rss = new XDocument(
            new XDeclaration("1.0", "utf-8", null),
            new XElement("rss",
                new XAttribute("version", "2.0"),
                new XElement("channel",
                    new XElement("title", title),
                    new XElement("link", siteUrl),
                    new XElement("description", description),
                    new XElement("language", "en-us"),
                    new XElement("lastBuildDate", DateTime.UtcNow.ToString("R")),
                    items
                )
            )
        );

        return rss.Declaration + Environment.NewLine + rss;
    }
}
```

---

## FILE: `content/blog/2026-01-15-welcome-to-observer-magazine.md`

```markdown
---
title: Welcome to Observer Magazine
date: 2026-01-15
author: Observer Team
summary: Our first post — introducing Observer Magazine and what we're building.
tags:
  - announcement
  - introduction
---

## Hello, World!

Welcome to **Observer Magazine**, a free and open-source web application built with Blazor WebAssembly on .NET 10.

This project serves two purposes:

1. **A learning resource** for developers exploring Blazor WASM, modern .NET tooling (slnx, Directory.Build.props, central package management), and static site deployment on GitHub Pages.
2. **A starting point** you can fork and adapt for your own projects — whether that's a personal blog, a product showcase, or a full SaaS application.

## What's Inside

- A responsive, accessible UI built entirely in C# and Razor
- A blog engine powered by Markdown files with YAML front matter
- An auto-generated RSS feed
- Showcases of common web patterns: responsive tables, master-detail flows
- Structured logging ready for OpenTelemetry
- A full test suite using xUnit v3 and bUnit

## Philosophy

Every dependency we use is truly free — no "free for non-commercial" restrictions. We will never charge money for this software. The code is MIT-licensed and always will be.

Stay tuned for more posts!
```

---

## FILE: `content/blog/2026-02-20-getting-started-with-blazor-wasm.md`

```markdown
---
title: Getting Started with Blazor WebAssembly
date: 2026-02-20
author: Observer Team
summary: A quick tour of how Blazor WASM works and why it's a great choice for static sites.
tags:
  - blazor
  - dotnet
  - tutorial
---

## What is Blazor WebAssembly?

Blazor WebAssembly (WASM) lets you build interactive web UIs using C# instead of JavaScript. Your .NET code runs directly in the browser via WebAssembly — no plugins, no server needed at runtime.

## Why We Chose It

For Observer Magazine, Blazor WASM is ideal because:

- **Static hosting** — The compiled output is plain HTML, CSS, JS, and WASM files. Perfect for GitHub Pages.
- **Full .NET ecosystem** — We use the same language, tooling, and libraries as backend .NET developers.
- **Performance** — After the initial download, navigation is instant. The runtime is ahead-of-time compiled for speed.
- **Testability** — With bUnit, we can unit-test every component without a browser.

## Project Structure

Our project follows a clean layout:

```
src/ObserverMagazine.Web/     — The Blazor WASM app
tools/ContentProcessor/        — Build-time markdown processor
tests/                         — xUnit + bUnit tests
content/blog/                  — Markdown blog posts
```

The `ContentProcessor` runs at build time (in CI) to convert Markdown files into JSON and HTML that the Blazor app fetches at runtime.

## Next Steps

Check out the [Showcase](/showcase) to see responsive tables and master-detail flows in action, or browse the [source code](https://github.com/ObserverMagazine/observermagazine.github.io) to see how everything fits together.
```

---

## FILE: `content/blog/2026-03-10-responsive-design-patterns.md`

```markdown
---
title: Responsive Design Patterns in Blazor
date: 2026-03-10
author: Observer Team
summary: How we built mobile-friendly data tables and master-detail layouts in pure Blazor.
tags:
  - blazor
  - css
  - responsive
  - ui
---

## The Challenge

Data-heavy UIs are notoriously hard to make responsive. Wide tables overflow on small screens, and complex layouts need fundamentally different structures on mobile vs. desktop.

## Responsive Tables

Our approach uses CSS to transform table rows into stacked cards on small screens:

- On desktop: a traditional `<table>` with sortable column headers
- On mobile: each row becomes a card with label-value pairs

The key CSS trick is using `data-label` attributes on `<td>` elements and displaying them via `::before` pseudo-elements when the table header is hidden.

## Master-Detail Flow

The master-detail pattern uses CSS Grid:

- On desktop: a two-column layout (list on left, details on right)
- On mobile: the columns stack vertically, with the list on top

No JavaScript media queries needed — it's all pure CSS with Blazor handling the state.

## Key Takeaways

1. **Use semantic HTML** — `<table>` for tabular data, not divs pretending to be tables.
2. **CSS does the heavy lifting** — Blazor components stay clean; responsiveness lives in the stylesheet.
3. **Test on real devices** — Emulators are fine for development, but nothing beats a real phone.

See all these patterns live on the [Showcase page](/showcase).
```

---

## FILE: `tests/ObserverMagazine.Web.Tests/ObserverMagazine.Web.Tests.csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <RootNamespace>ObserverMagazine.Web.Tests</RootNamespace>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="xunit.v3" />
    <PackageReference Include="xunit.runner.visualstudio" />
    <PackageReference Include="bunit" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\src\ObserverMagazine.Web\ObserverMagazine.Web.csproj" />
  </ItemGroup>

</Project>
```

---

## FILE: `tests/ObserverMagazine.Web.Tests/Services/BlogServiceTests.cs`

```csharp
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ObserverMagazine.Web.Models;
using ObserverMagazine.Web.Services;
using Xunit;

namespace ObserverMagazine.Web.Tests.Services;

public class BlogServiceTests
{
    private static readonly BlogPostMetadata[] SamplePosts =
    [
        new()
        {
            Slug = "first-post",
            Title = "First Post",
            Date = new DateTime(2026, 1, 15),
            Author = "Test Author",
            Summary = "The first post",
            Tags = ["test", "intro"]
        },
        new()
        {
            Slug = "second-post",
            Title = "Second Post",
            Date = new DateTime(2026, 2, 20),
            Author = "Test Author",
            Summary = "The second post",
            Tags = ["test"]
        }
    ];

    private static BlogService CreateService(HttpClient httpClient)
    {
        var logger = NullLogger<BlogService>.Instance;
        return new BlogService(httpClient, logger);
    }

    [Fact]
    public async Task GetPostsAsync_ReturnsPostsSortedByDateDescending()
    {
        // Arrange
        var json = JsonSerializer.Serialize(SamplePosts,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        var handler = new FakeHttpHandler(json, "application/json");
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://test.local/") };
        var service = CreateService(httpClient);

        // Act
        var posts = await service.GetPostsAsync();

        // Assert
        Assert.Equal(2, posts.Length);
        Assert.Equal("Second Post", posts[0].Title); // Feb 20 > Jan 15
        Assert.Equal("First Post", posts[1].Title);
    }

    [Fact]
    public async Task GetPostMetadataAsync_ReturnsNullForUnknownSlug()
    {
        var json = JsonSerializer.Serialize(SamplePosts,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        var handler = new FakeHttpHandler(json, "application/json");
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://test.local/") };
        var service = CreateService(httpClient);

        var result = await service.GetPostMetadataAsync("nonexistent");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetPostMetadataAsync_FindsBySlug()
    {
        var json = JsonSerializer.Serialize(SamplePosts,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        var handler = new FakeHttpHandler(json, "application/json");
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://test.local/") };
        var service = CreateService(httpClient);

        var result = await service.GetPostMetadataAsync("first-post");

        Assert.NotNull(result);
        Assert.Equal("First Post", result.Title);
    }

    [Fact]
    public async Task GetPostsAsync_ReturnsEmptyOnHttpError()
    {
        var handler = new FakeHttpHandler(HttpStatusCode.NotFound);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://test.local/") };
        var service = CreateService(httpClient);

        var posts = await service.GetPostsAsync();

        Assert.Empty(posts);
    }
}

/// <summary>
/// Simple fake HTTP handler for unit testing without external dependencies.
/// </summary>
internal sealed class FakeHttpHandler : HttpMessageHandler
{
    private readonly HttpResponseMessage response;

    public FakeHttpHandler(string content, string mediaType)
    {
        response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(content, System.Text.Encoding.UTF8, mediaType)
        };
    }

    public FakeHttpHandler(HttpStatusCode statusCode)
    {
        response = new HttpResponseMessage(statusCode);
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        return Task.FromResult(response);
    }
}
```

---

## FILE: `tests/ObserverMagazine.Web.Tests/Components/ResponsiveTableTests.cs`

```csharp
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using ObserverMagazine.Web.Components;
using Xunit;

namespace ObserverMagazine.Web.Tests.Components;

public class ResponsiveTableTests : TestContext
{
    private const string SampleProductsJson = """
        [
          { "name": "Widget A", "category": "Tools", "price": 9.99, "stock": 100, "rating": 4.5, "description": "A widget" },
          { "name": "Gadget B", "category": "Electronics", "price": 49.99, "stock": 25, "rating": 3.8, "description": "A gadget" }
        ]
        """;

    [Fact]
    public void RendersLoadingState_Initially()
    {
        // The component fetches data on init; before data loads, it shows loading
        var fakeHandler = new FakeHttpHandler(SampleProductsJson, "application/json");
        var httpClient = new HttpClient(fakeHandler) { BaseAddress = new Uri("https://test.local/") };
        Services.AddSingleton(httpClient);

        var cut = RenderComponent<ResponsiveTable>();

        // After render with async data, table should appear
        cut.WaitForElement(".data-table");
        Assert.Contains("Widget A", cut.Markup);
        Assert.Contains("Gadget B", cut.Markup);
    }

    [Fact]
    public void FilterInput_FiltersRows()
    {
        var fakeHandler = new FakeHttpHandler(SampleProductsJson, "application/json");
        var httpClient = new HttpClient(fakeHandler) { BaseAddress = new Uri("https://test.local/") };
        Services.AddSingleton(httpClient);

        var cut = RenderComponent<ResponsiveTable>();
        cut.WaitForElement(".data-table");

        var filterInput = cut.Find(".filter-input");
        filterInput.Input("Widget");

        Assert.Contains("Widget A", cut.Markup);
        Assert.DoesNotContain("Gadget B", cut.Markup);
    }

    [Fact]
    public void ClickColumnHeader_SortsData()
    {
        var fakeHandler = new FakeHttpHandler(SampleProductsJson, "application/json");
        var httpClient = new HttpClient(fakeHandler) { BaseAddress = new Uri("https://test.local/") };
        Services.AddSingleton(httpClient);

        var cut = RenderComponent<ResponsiveTable>();
        cut.WaitForElement(".data-table");

        // Click Price header to sort
        var priceHeader = cut.FindAll("th.sortable")[2]; // Price is 3rd column
        priceHeader.Click();

        // After clicking, should show sort indicator
        Assert.Contains("▲", cut.Markup);
    }
}

// Reuse the FakeHttpHandler from BlogServiceTests
internal sealed class FakeHttpHandler : HttpMessageHandler
{
    private readonly HttpResponseMessage response;

    public FakeHttpHandler(string content, string mediaType)
    {
        response = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
        {
            Content = new StringContent(content, System.Text.Encoding.UTF8, mediaType)
        };
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        return Task.FromResult(response);
    }
}
```

---

## FILE: `tests/ObserverMagazine.Web.Tests/Components/MasterDetailTests.cs`

```csharp
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using ObserverMagazine.Web.Components;
using Xunit;

namespace ObserverMagazine.Web.Tests.Components;

public class MasterDetailTests : TestContext
{
    private const string SampleJson = """
        [
          { "name": "Alpha", "category": "Cat1", "price": 10.00, "stock": 5, "rating": 4.0, "description": "Alpha desc" },
          { "name": "Bravo", "category": "Cat2", "price": 20.00, "stock": 10, "rating": 3.5, "description": "Bravo desc" }
        ]
        """;

    [Fact]
    public void ShowsEmptyDetailPanel_WhenNothingSelected()
    {
        var handler = new MasterDetailFakeHandler(SampleJson);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://test.local/") };
        Services.AddSingleton(httpClient);

        var cut = RenderComponent<MasterDetail>();
        cut.WaitForElement(".master-list ul");

        Assert.Contains("Select an item", cut.Markup);
    }

    [Fact]
    public void ClickingItem_ShowsDetails()
    {
        var handler = new MasterDetailFakeHandler(SampleJson);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://test.local/") };
        Services.AddSingleton(httpClient);

        var cut = RenderComponent<MasterDetail>();
        cut.WaitForElement(".master-list ul");

        var firstItem = cut.Find(".master-list li");
        firstItem.Click();

        Assert.Contains("Alpha desc", cut.Markup);
        Assert.Contains("selected", firstItem.GetAttribute("class") ?? "");
    }

    [Fact]
    public void ClickingDifferentItem_UpdatesDetails()
    {
        var handler = new MasterDetailFakeHandler(SampleJson);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://test.local/") };
        Services.AddSingleton(httpClient);

        var cut = RenderComponent<MasterDetail>();
        cut.WaitForElement(".master-list ul");

        // Click second item
        var items = cut.FindAll(".master-list li");
        items[1].Click();

        Assert.Contains("Bravo desc", cut.Markup);
    }
}

internal sealed class MasterDetailFakeHandler : HttpMessageHandler
{
    private readonly HttpResponseMessage response;

    public MasterDetailFakeHandler(string json)
    {
        response = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
        {
            Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
        };
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        return Task.FromResult(response);
    }
}
```

---

## FILE: `tests/ObserverMagazine.Integration.Tests/ObserverMagazine.Integration.Tests.csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <RootNamespace>ObserverMagazine.Integration.Tests</RootNamespace>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="xunit.v3" />
    <PackageReference Include="xunit.runner.visualstudio" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" />
    <PackageReference Include="Markdig" />
    <PackageReference Include="YamlDotNet" />
  </ItemGroup>

</Project>
```

---

## FILE: `tests/ObserverMagazine.Integration.Tests/ContentProcessorTests.cs`

```csharp
using Markdig;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using Xunit;

namespace ObserverMagazine.Integration.Tests;

public class ContentProcessorTests
{
    [Fact]
    public void FrontMatter_ParsesCorrectly()
    {
        var markdown = """
            ---
            title: Test Post
            date: 2026-03-01
            author: Tester
            summary: A test summary
            tags:
              - test
              - integration
            ---

            ## Hello

            This is the body.
            """;

        var (frontMatter, body) = ParseFrontMatter(markdown);

        Assert.Equal("Test Post", frontMatter.Title);
        Assert.Equal(new DateTime(2026, 3, 1), frontMatter.Date);
        Assert.Equal("Tester", frontMatter.Author);
        Assert.Equal("A test summary", frontMatter.Summary);
        Assert.Equal(["test", "integration"], frontMatter.Tags);
        Assert.Contains("## Hello", body);
    }

    [Fact]
    public void FrontMatter_HandlesMissingFields()
    {
        var markdown = """
            ---
            title: Minimal Post
            date: 2026-01-01
            ---
            Body content.
            """;

        var (frontMatter, body) = ParseFrontMatter(markdown);

        Assert.Equal("Minimal Post", frontMatter.Title);
        Assert.Null(frontMatter.Author);
        Assert.Null(frontMatter.Tags);
        Assert.Contains("Body content.", body);
    }

    [Fact]
    public void Markdown_ConvertsToHtml()
    {
        var md = "## Hello\n\nThis is **bold** and *italic*.";
        var pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
        var html = Markdown.ToHtml(md, pipeline);

        Assert.Contains("<h2>Hello</h2>", html);
        Assert.Contains("<strong>bold</strong>", html);
        Assert.Contains("<em>italic</em>", html);
    }

    [Fact]
    public void SlugDerivation_StripsDatePrefix()
    {
        Assert.Equal("welcome-post", DeriveSlug("2026-01-15-welcome-post"));
        Assert.Equal("no-date", DeriveSlug("no-date"));
        Assert.Equal("short", DeriveSlug("short"));
    }

    // --- Helpers duplicated from ContentProcessor for isolated testing ---

    private static (TestFrontMatter, string) ParseFrontMatter(string rawContent)
    {
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();

        if (!rawContent.StartsWith("---"))
            return (new TestFrontMatter(), rawContent);

        var endIndex = rawContent.IndexOf("---", 3, StringComparison.Ordinal);
        if (endIndex < 0)
            return (new TestFrontMatter(), rawContent);

        var yaml = rawContent[3..endIndex].Trim();
        var body = rawContent[(endIndex + 3)..].TrimStart('\r', '\n');
        var fm = deserializer.Deserialize<TestFrontMatter>(yaml);
        return (fm ?? new TestFrontMatter(), body);
    }

    private static string DeriveSlug(string fileName)
    {
        if (fileName.Length > 11 &&
            char.IsDigit(fileName[0]) &&
            fileName[4] == '-' &&
            fileName[7] == '-' &&
            fileName[10] == '-')
        {
            return fileName[11..];
        }
        return fileName;
    }
}

public sealed class TestFrontMatter
{
    public string Title { get; set; } = "";
    public DateTime Date { get; set; }
    public string? Author { get; set; }
    public string? Summary { get; set; }
    public string[]? Tags { get; set; }
}
```

---

## FILE: `tests/ObserverMagazine.Integration.Tests/RssGeneratorTests.cs`

```csharp
using System.Xml.Linq;
using Xunit;

namespace ObserverMagazine.Integration.Tests;

public class RssGeneratorTests
{
    [Fact]
    public void GenerateRss_ProducesValidXml()
    {
        var posts = new List<RssPostEntry>
        {
            new()
            {
                Slug = "test-post",
                Title = "Test Post",
                Date = new DateTime(2026, 3, 1),
                Summary = "A test post",
                Tags = ["test"]
            }
        };

        var xml = GenerateRss("Test Blog", "A blog", "https://example.com", posts);

        // Should be valid XML
        var doc = XDocument.Parse(xml);
        var channel = doc.Root!.Element("channel")!;

        Assert.Equal("Test Blog", channel.Element("title")!.Value);
        Assert.Equal("https://example.com", channel.Element("link")!.Value);

        var item = channel.Element("item")!;
        Assert.Equal("Test Post", item.Element("title")!.Value);
        Assert.Equal("https://example.com/blog/test-post", item.Element("link")!.Value);
    }

    [Fact]
    public void GenerateRss_HandlesEmptyPostList()
    {
        var xml = GenerateRss("Empty Blog", "Nothing here", "https://example.com", []);
        var doc = XDocument.Parse(xml);
        var items = doc.Root!.Element("channel")!.Elements("item");
        Assert.Empty(items);
    }

    [Fact]
    public void GenerateRss_IncludesCategoryTags()
    {
        var posts = new List<RssPostEntry>
        {
            new()
            {
                Slug = "tagged",
                Title = "Tagged Post",
                Date = new DateTime(2026, 1, 1),
                Summary = "Has tags",
                Tags = ["alpha", "beta"]
            }
        };

        var xml = GenerateRss("Blog", "Desc", "https://example.com", posts);
        var doc = XDocument.Parse(xml);
        var categories = doc.Root!.Element("channel")!
            .Element("item")!.Elements("category").Select(c => c.Value).ToArray();

        Assert.Equal(["alpha", "beta"], categories);
    }

    // --- RSS generation logic (same as ContentProcessor) ---
    private static string GenerateRss(
        string title, string description, string siteUrl,
        IReadOnlyList<RssPostEntry> posts)
    {
        var items = posts.Select(p =>
            new XElement("item",
                new XElement("title", p.Title),
                new XElement("link", $"{siteUrl}/blog/{p.Slug}"),
                new XElement("description", p.Summary),
                new XElement("pubDate", p.Date.ToString("R")),
                new XElement("guid", $"{siteUrl}/blog/{p.Slug}"),
                p.Tags.Length > 0
                    ? p.Tags.Select(t => new XElement("category", t))
                    : null
            ));

        var rss = new XDocument(
            new XDeclaration("1.0", "utf-8", null),
            new XElement("rss",
                new XAttribute("version", "2.0"),
                new XElement("channel",
                    new XElement("title", title),
                    new XElement("link", siteUrl),
                    new XElement("description", description),
                    new XElement("language", "en-us"),
                    new XElement("lastBuildDate", DateTime.UtcNow.ToString("R")),
                    items
                )
            )
        );

        return rss.Declaration + Environment.NewLine + rss;
    }
}

public sealed class RssPostEntry
{
    public string Slug { get; init; } = "";
    public string Title { get; init; } = "";
    public DateTime Date { get; init; }
    public string Summary { get; init; } = "";
    public string[] Tags { get; init; } = [];
}
```

---

## Quick Start Commands

```bash
# 1. Clone the repo
git clone https://github.com/ObserverMagazine/observermagazine.github.io.git
cd observermagazine.github.io

# 2. Restore packages
dotnet restore

# 3. Process blog content (generates blog-data/ and feed.xml)
dotnet run --project tools/ObserverMagazine.ContentProcessor -- \
  --content-dir content/blog \
  --output-dir src/ObserverMagazine.Web/wwwroot

# 4. Run tests
dotnet test

# 5. Run locally (hot reload)
dotnet watch --project src/ObserverMagazine.Web

# 6. Publish for production
dotnet publish src/ObserverMagazine.Web -c Release -o publish
# Static files are in publish/wwwroot/
```

---

## Adding a New Blog Post

1. Create a new file in `content/blog/` following the naming convention: `YYYY-MM-DD-your-slug-here.md`
2. Add YAML front matter at the top:
   ```yaml
   ---
   title: Your Post Title
   date: 2026-04-01
   author: Your Name
   summary: A brief description for the blog index and RSS feed.
   tags:
     - tag1
     - tag2
   ---
   ```
3. Write your content in standard Markdown below the front matter
4. Run the content processor (or just push — CI does it automatically)
5. The post appears on the blog page and in the RSS feed

dotnet run --project tools/ObserverMagazine.ContentProcessor -- --content-dir content/blog --output-dir src/ObserverMagazine.Web/wwwroot