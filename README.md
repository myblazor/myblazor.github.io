# My Blazor Magazine

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

AGPLv3
