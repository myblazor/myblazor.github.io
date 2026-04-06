---
title: "Aspire, Containers, and Self-Hosting: A Complete Guide to Deploying .NET Applications on Your Own Hardware"
date: 2026-04-08
author: myblazor-team
summary: A comprehensive guide to Aspire, OCI containers, and self-hosted deployment. Covers what Aspire is, how it generates container artifacts, how to write Containerfiles, how to deploy to bare metal or a VPS using podman-compose, and when (if ever) you actually need Kubernetes. Uses a real Blazor + SQLite application as the running example throughout.
tags:
  - aspire
  - containers
  - podman
  - self-hosting
  - deep-dive
  - blazor
  - devops
  - dotnet
featured: true
---

You have a .NET application. It works on your machine. You want to put it on a server — a real server, one you can SSH into, maybe a $5/month VPS on Hetzner or a recycled Dell OptiPlex humming in your closet. You do not want to send your code to Azure. You do not want to learn Kubernetes. You do not want to pay Docker, Inc. a licensing fee. You just want your application running, behind a reverse proxy, with HTTPS, and you want to be able to update it by pushing to a git repository.

This article is about how to get there. We will start with Aspire — what it is, what it is not, and why it matters even if you never deploy to a cloud provider. We will walk through OCI containers using vendor-neutral terminology and tooling. We will write Containerfiles (not "Dockerfiles" — more on that distinction shortly). We will use Podman and podman-compose because they are free, open-source, daemonless, and rootless by default. And we will deploy a real application — a Blazor Server address book backed by SQLite — to a Linux server you control.

The sample application for this article is [Virginia](https://github.com/collabskus/virginia), an open-source contact management application built with .NET 10, Blazor Server, Entity Framework Core, SQLite, and Aspire 13. It uses the Aspire AppHost for local development orchestration, OpenTelemetry for observability, and ASP.NET Core Identity for authentication. Everything in this article uses Virginia as its concrete example, but the patterns apply to any .NET application.

Let us begin.

## Part 1: What Is Aspire?

### The Elevator Pitch

Aspire is an open-source framework from Microsoft for building, running, and deploying distributed applications. It was first previewed in November 2023 alongside the .NET 8 launch, reached general availability with version 8.0 in May 2024, and has since evolved rapidly through versions 8.1, 8.2, 9.0 through 9.5, 13.0, 13.1, and 13.2 (released March 23, 2026). The version jump from 9.x to 13 happened when the project dropped the ".NET" prefix — it is now simply "Aspire," reflecting its expansion to support Python, JavaScript, and TypeScript as first-class citizens alongside .NET.

At its core, Aspire does three things:

**Orchestration.** During local development, Aspire starts all the pieces of your application — your .NET projects, your containers (Redis, PostgreSQL, RabbitMQ, whatever), your Python scripts, your Node.js frontends — and wires them together. It handles service discovery (so your API can find your database without you hardcoding ports), environment variable injection, health check monitoring, and a real-time dashboard that shows logs, traces, and metrics from every component.

**Integrations.** Aspire provides NuGet packages (called "integrations," formerly "components") that configure popular services with sensible defaults. Adding Redis caching, PostgreSQL, or OpenTelemetry export takes a single method call. Each integration comes pre-configured with health checks, retry policies, and telemetry — the cross-cutting concerns that every production application needs but that nobody enjoys setting up from scratch.

**Deployment.** Starting with Aspire 13.0, the framework can generate deployment artifacts from your application model. You describe your application in C# (or now TypeScript), and Aspire can output Docker Compose files, Kubernetes manifests, or Azure Bicep templates. The `aspire publish` command generates these artifacts, and `aspire deploy` can apply them to your target environment.

### What Aspire Is Not

Aspire is not a runtime. Your application does not depend on Aspire at runtime in production (unless you choose to deploy the Aspire Dashboard alongside it, which is optional). The Service Defaults library configures OpenTelemetry, health checks, and resilience — these are standard .NET libraries that work with or without Aspire.

Aspire is not a hosting platform. It does not run your application in production. It generates the artifacts (Compose files, Kubernetes manifests) that some other system uses to run your application.

Aspire is not Azure-specific. The first deployment target Microsoft built was Azure Container Apps, which gave many developers the impression that Aspire was an Azure lock-in tool. That impression was wrong then and is especially wrong now. Aspire 13.x supports Docker Compose as a first-class deployment target, which means you can deploy to any Linux server that runs an OCI-compatible container runtime. That includes your VPS. That includes your closet server. That includes a Raspberry Pi if you are feeling adventurous.

### Aspire's Architecture in Virginia

Let us look at how Aspire is structured in the Virginia application. The solution has four projects:

```
Virginia.slnx
├── Virginia.AppHost          → Aspire orchestrator (dev-time only)
├── Virginia.ServiceDefaults  → Shared infrastructure (OTel, health, resilience)
├── Virginia                  → Main Blazor Server application
└── Virginia.Tests            → Unit and integration tests
```

The **AppHost** is a tiny project. Here is its entire `AppHost.cs`:

```csharp
var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.Virginia>("virginia");

builder.Build().Run();
```

That is it. One line registers the Virginia web project. When you run `dotnet run --project Virginia.AppHost`, Aspire starts the Virginia web application, sets up environment variables for telemetry export, and launches the Aspire Dashboard. The dashboard gives you a real-time view of structured logs, distributed traces, and metrics — all the OpenTelemetry data that the Service Defaults library configures.

The **Service Defaults** project is more substantial. Its `Extensions.cs` file configures:

```csharp
public static TBuilder AddServiceDefaults<TBuilder>(this TBuilder builder)
    where TBuilder : IHostApplicationBuilder
{
    builder.ConfigureOpenTelemetry();
    builder.AddDefaultHealthChecks();
    builder.Services.AddServiceDiscovery();

    builder.Services.ConfigureHttpClientDefaults(http =>
    {
        http.AddStandardResilienceHandler();
        http.AddServiceDiscovery();
    });

    return builder;
}
```

This single method call in the main application's `Program.cs` gives you OpenTelemetry logging, metrics, and tracing with automatic ASP.NET Core and HTTP client instrumentation; health check endpoints at `/health` and `/alive`; service discovery for all HttpClient calls; and standard resilience policies (retries, circuit breakers, timeouts) for outgoing HTTP requests. These are all production-quality features that you would configure manually without Aspire. The Service Defaults library is just a convenient way to apply them consistently across every project in your solution.

The AppHost project file uses the Aspire AppHost SDK:

```xml
<Project Sdk="Aspire.AppHost.Sdk/13.1.0">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <UserSecretsId>6587bc8b-aaa4-48f4-84f2-85a615267c18</UserSecretsId>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\Virginia\Virginia.csproj" />
  </ItemGroup>
</Project>
```

Notice the SDK version in the `<Project>` tag. As of Aspire 13.0, the SDK is specified directly in the project file rather than requiring a workload installation. This simplifies CI/CD pipelines enormously — you do not need `dotnet workload install` in your GitHub Actions workflow.

### The Key Insight

Here is the mental model that matters: **Aspire is a dev-time orchestrator and a deploy-time artifact generator.** During development, it makes your life easier by starting everything and showing you telemetry. During deployment, it generates the files you need to run your application in containers. In between, it does not exist. Your application in production is just a .NET application running in a container, and the container runtime does not know or care that Aspire generated the configuration.

This separation is powerful because it means you are never locked in. If Aspire generates a Docker Compose file and you do not like something about it, you can edit the file. If you outgrow Docker Compose and want Kubernetes, you can ask Aspire to generate Kubernetes manifests instead. If you decide Aspire is not for you at all, you still have a perfectly normal .NET application — just remove the AppHost and Service Defaults projects and configure OpenTelemetry and health checks directly.

## Part 2: OCI Containers — Vendor-Neutral Thinking

### The Terminology Problem

The container ecosystem has a language problem. Most developers say "Docker" when they mean "containers," "Dockerfile" when they mean "a file that describes how to build a container image," and "Docker Compose" when they mean "a tool for running multiple containers together." This conflation is understandable — Docker, Inc. popularized containers and their tooling became the de facto standard. But it leads to confusion, especially when you want to use alternatives.

Let us establish precise terminology:

**OCI (Open Container Initiative)** is the governance body that defines open standards for container images and runtimes. The OCI Image Specification defines the format for container images. The OCI Runtime Specification defines how container runtimes execute those images. Both Docker and Podman implement these standards, which means images built by one tool can be run by the other. There is no lock-in at the image level.

**Container image** is a read-only template containing your application, its dependencies, and the configuration needed to run it. An image is built from a set of layers, each created by an instruction in a build file.

**Containerfile** (or Dockerfile) is the build file that describes how to construct a container image. The Open Container Initiative does not mandate a specific filename, but by convention, the file is named `Containerfile` (the vendor-neutral name) or `Dockerfile` (the Docker-originated name). Both Podman and Docker accept either filename. Throughout this article, we will use "Containerfile" to emphasize the vendor-neutral nature of the format, but the syntax is identical regardless of what you name the file.

**Container runtime** is the software that runs container images. Docker Engine (with its `dockerd` daemon), Podman (daemonless), containerd, and CRI-O are all container runtimes that implement the OCI Runtime Specification.

**Container registry** is a service that stores and distributes container images. Docker Hub, GitHub Container Registry (ghcr.io), Quay.io, and any self-hosted registry like Harbor are all container registries. They all speak the same OCI Distribution Specification protocol.

### Why Podman, Not Docker

Docker Desktop — the GUI application that most developers use on macOS and Windows — requires a paid subscription for companies with more than 250 employees or more than $10 million in annual revenue. Docker Engine on Linux is free, but Docker Desktop is not universally free. This licensing change in August 2021 caused a lot of organizations to look for alternatives.

Podman is that alternative. It is free, open-source (Apache 2.0 license), developed by Red Hat, and ships as the default container engine on Red Hat Enterprise Linux. Here is why it matters for self-hosting:

**Daemonless architecture.** Docker runs a persistent background daemon (`dockerd`) that manages all containers. If the daemon crashes, every container goes down. Podman launches each container as a regular child process of your user session. There is no central daemon, no single point of failure, and no background process consuming resources when you are not running containers.

**Rootless by default.** Docker traditionally required root privileges to run containers (though rootless mode is now available). Podman runs containers as your regular user by default, which is a significant security improvement. No root-level daemon socket means no vector for container escape attacks to gain root on the host.

**CLI compatibility.** Podman implements the same command-line interface as Docker. You can literally run `alias docker=podman` and most scripts will work unchanged. The commands `podman build`, `podman run`, `podman push`, `podman pull`, and `podman images` all behave identically to their Docker counterparts.

**Systemd integration.** On a Linux server, you can generate systemd service files from running Podman containers using `podman generate systemd`. This means your containers start on boot, restart on failure, and are managed by the same init system that manages everything else on the server.

**No licensing concerns.** Podman and Podman Desktop are completely free for all users, including commercial use.

### Installing Podman

On a Debian/Ubuntu VPS:

```bash
sudo apt-get update
sudo apt-get install -y podman podman-compose
```

On Fedora/RHEL:

```bash
sudo dnf install -y podman podman-compose
```

On macOS (for local development):

```bash
brew install podman
podman machine init
podman machine start
```

On Windows (for local development), download Podman Desktop from [podman-desktop.io](https://podman-desktop.io) or install via winget:

```bash
winget install RedHat.Podman
winget install RedHat.Podman-Desktop
```

Verify your installation:

```bash
podman --version
# podman version 5.x.x

podman-compose --version
# podman-compose version x.x.x
```

### Telling Aspire to Use Podman

If you want to use Aspire's local development orchestration with Podman instead of Docker, set an environment variable:

```bash
# Linux/macOS
export DOTNET_ASPIRE_CONTAINER_RUNTIME=podman

# Windows (PowerShell)
$env:DOTNET_ASPIRE_CONTAINER_RUNTIME = "podman"

# Windows (persistent)
[System.Environment]::SetEnvironmentVariable("DOTNET_ASPIRE_CONTAINER_RUNTIME", "podman", "User")
```

With this set, Aspire will use Podman to pull and run any container resources (like Redis, PostgreSQL, or SQL Server) that your AppHost defines.

## Part 3: Writing a Containerfile for Virginia

### Understanding the Application

Before we containerize Virginia, let us understand what it needs at runtime:

1. The .NET 10 ASP.NET Core runtime (not the SDK — we only need the SDK for building)
2. The published application files
3. A writable directory for the SQLite database file
4. Network ports for HTTP/HTTPS
5. Environment variables for configuration

Virginia uses SQLite, which means the database is a single file on disk. This is both a simplification (no separate database container) and a complication (we need persistent storage for the file). We will handle persistence with a volume mount.

### The Multi-Stage Containerfile

A multi-stage build uses one image to build the application and a different, smaller image to run it. The build stage includes the full .NET SDK (which is large), while the runtime stage only includes the ASP.NET Core runtime (which is much smaller).

Create a file called `Containerfile` in the root of the Virginia repository:

```dockerfile
# ──────────────────────────────────────────────────────────────────────────────
# Stage 1: Build
# ──────────────────────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy the solution-level files first for better layer caching
COPY Directory.Build.props Directory.Packages.props Virginia.slnx ./

# Copy project files (these change less often than source code)
COPY Virginia/Virginia.csproj Virginia/
COPY Virginia.ServiceDefaults/Virginia.ServiceDefaults.csproj Virginia.ServiceDefaults/

# Restore NuGet packages (this layer is cached unless .csproj files change)
RUN dotnet restore Virginia/Virginia.csproj

# Copy everything else
COPY Virginia/ Virginia/
COPY Virginia.ServiceDefaults/ Virginia.ServiceDefaults/

# Publish the application in Release configuration
RUN dotnet publish Virginia/Virginia.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish

# ──────────────────────────────────────────────────────────────────────────────
# Stage 2: Runtime
# ──────────────────────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Create a non-root user for security
RUN adduser --disabled-password --gecos "" appuser

# Create a directory for the SQLite database with correct ownership
RUN mkdir -p /data && chown appuser:appuser /data

# Copy the published application from the build stage
COPY --from=build /app/publish .

# Switch to the non-root user
USER appuser

# Expose the HTTP port (we will handle HTTPS at the reverse proxy)
EXPOSE 8080

# Configure ASP.NET Core to listen on port 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Point the SQLite database to the persistent volume
ENV ConnectionStrings__DefaultConnection="Data Source=/data/virginia.db"

# Start the application
ENTRYPOINT ["dotnet", "Virginia.dll"]
```

Let us walk through every decision in this file.

### Why Multi-Stage?

The .NET SDK image (`mcr.microsoft.com/dotnet/sdk:10.0`) is roughly 800 MB. The ASP.NET Core runtime image (`mcr.microsoft.com/dotnet/aspnet:10.0`) is roughly 220 MB. By building in one stage and running in another, our final image is significantly smaller. The build stage and all its tools (compilers, NuGet cache, SDK) are discarded once the published files are copied to the runtime stage.

### Layer Caching Strategy

Container images are built layer by layer, and each layer is cached. If the inputs to a layer have not changed since the last build, the cached layer is reused. This is why we copy `Directory.Build.props`, `Directory.Packages.props`, and the `.csproj` files before copying the source code. NuGet package restore (`dotnet restore`) depends only on the project files and the package version props. If you change a `.razor` file but do not add a new NuGet package, the restore layer is cached and the build is much faster.

### Non-Root User

Running the application as a non-root user inside the container is a security best practice. If an attacker exploits a vulnerability in the application, they gain the privileges of `appuser`, not `root`. This limits the blast radius significantly.

### The /data Volume

The SQLite database lives in `/data/virginia.db`. This directory will be mounted as a persistent volume when we run the container, so the database survives container restarts and updates. The `chown` command ensures the non-root user can write to this directory.

### Port Configuration

We expose port 8080 and configure ASP.NET Core to listen on it via the `ASPNETCORE_URLS` environment variable. We do not configure HTTPS inside the container — that is the job of the reverse proxy (Caddy, Nginx, or Traefik) that sits in front of our application. This is a standard pattern in containerized deployments: the application handles HTTP, the reverse proxy handles TLS termination.

### Environment Variable Configuration

The `ConnectionStrings__DefaultConnection` environment variable overrides the `ConnectionStrings:DefaultConnection` setting from `appsettings.json`. ASP.NET Core's configuration system uses `__` (double underscore) as a hierarchy separator in environment variables, mapping to `:` in JSON configuration. This lets us configure the application without modifying any files inside the container.

### Building the Image

```bash
# Build with Podman
podman build -t virginia:latest -f Containerfile .

# Verify the image was created
podman images | grep virginia
```

### Testing Locally

```bash
# Run the container with a local volume for the database
podman run -d \
    --name virginia \
    -p 8080:8080 \
    -v virginia-data:/data \
    virginia:latest

# Check the logs
podman logs virginia

# Open in browser: http://localhost:8080

# Stop and remove when done
podman stop virginia
podman rm virginia
```

## Part 4: Composing Multiple Containers with podman-compose

### Why Compose?

Virginia is a single-application project — one .NET application and a SQLite file. You might think Compose is overkill. But even for a single application, Compose gives you several benefits:

1. **Declarative configuration.** Your entire deployment is described in a single YAML file that lives in your repository. Anyone can read it and understand the deployment.
2. **Volume management.** Compose creates and manages named volumes for you.
3. **Network isolation.** Compose creates a dedicated network for your services, isolating them from other containers on the host.
4. **Reverse proxy.** You will almost certainly want Caddy or Nginx in front of your application for TLS termination. Compose orchestrates both containers together.
5. **Reproducibility.** `podman-compose up -d` produces the same result every time, regardless of who runs it.

### The Compose File

Create a file called `compose.yaml` (the modern standard name — `docker-compose.yml` also works but is the legacy convention) in the repository root:

```yaml
services:
  virginia:
    build:
      context: .
      dockerfile: Containerfile
    container_name: virginia
    restart: unless-stopped
    volumes:
      - virginia-data:/data
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ConnectionStrings__DefaultConnection=Data Source=/data/virginia.db
      - AdminUser__Email=admin@virginia.local
      - AdminUser__Password=YourStrongPasswordHere!
    networks:
      - web

  caddy:
    image: caddy:2-alpine
    container_name: caddy
    restart: unless-stopped
    ports:
      - "80:80"
      - "443:443"
    volumes:
      - ./Caddyfile:/etc/caddy/Caddyfile:ro
      - caddy-data:/data
      - caddy-config:/config
    networks:
      - web

volumes:
  virginia-data:
  caddy-data:
  caddy-config:

networks:
  web:
```

### The Caddyfile

Caddy is a web server that automatically obtains and renews TLS certificates from Let's Encrypt. It is the simplest way to get HTTPS in front of your application. Create a `Caddyfile` in the repository root:

```
virginia.yourdomain.com {
    reverse_proxy virginia:8080
}
```

That is the entire Caddy configuration. When Caddy starts, it will:

1. Obtain a TLS certificate from Let's Encrypt for `virginia.yourdomain.com`
2. Automatically renew the certificate before it expires
3. Redirect all HTTP traffic to HTTPS
4. Forward all HTTPS traffic to the Virginia container on port 8080

Replace `virginia.yourdomain.com` with your actual domain. You will need a DNS A record pointing to your server's IP address.

### Understanding the Compose File

The `services` section defines two containers:

**virginia** is built from the Containerfile in the current directory. It mounts the `virginia-data` volume at `/data` for SQLite persistence. It does not expose any ports to the host — it is only reachable from other containers on the `web` network. The `restart: unless-stopped` policy means the container restarts automatically after crashes or server reboots (unless you explicitly stop it).

**caddy** uses the official Caddy image from Docker Hub (which Podman pulls from by default). It exposes ports 80 and 443 on the host for HTTP and HTTPS traffic. The Caddyfile is mounted read-only (`:ro`). Two volumes persist Caddy's certificate data and configuration across restarts.

Both containers are connected to the `web` network. Within this network, containers can reach each other by name — that is why the Caddyfile uses `virginia:8080` as the reverse proxy target.

### Deploying

On your VPS, clone your repository and run:

```bash
cd virginia
podman-compose up -d
```

That is it. Podman will build the Virginia image, pull the Caddy image, create the volumes and network, and start both containers. Within a minute or two, Caddy will have obtained a TLS certificate and your application will be live at `https://virginia.yourdomain.com`.

To check the status:

```bash
podman-compose ps
podman-compose logs virginia
podman-compose logs caddy
```

To update after pushing new code:

```bash
cd virginia
git pull
podman-compose build virginia
podman-compose up -d virginia
```

This rebuilds the Virginia image with the latest code and restarts only the Virginia container. Caddy continues running undisturbed. The SQLite database persists in the `virginia-data` volume across container restarts.

### podman-compose vs docker compose

You might wonder about the differences. There are two tools in the Compose ecosystem:

**Docker Compose** (the reference implementation) is maintained by Docker, Inc. Version 2 (`docker compose` as a plugin) is written in Go and is the most feature-complete implementation of the Compose specification.

**podman-compose** is a community-maintained Python tool that translates Compose YAML into Podman commands. It supports the most commonly used Compose features — services, volumes, networks, build, environment variables, port mappings, restart policies, and depends_on. It does not support every edge case that Docker Compose handles (some advanced networking features, custom plugins, specific extension fields).

For the deployment we are describing — a web application behind a reverse proxy — podman-compose is more than sufficient. If you encounter a Compose feature that podman-compose does not support, you have two options: use `podman compose` (Podman's built-in Docker Compose compatibility layer, which delegates to Docker Compose if installed) or restructure your Compose file to avoid the unsupported feature.

In practice, for a Blazor application with SQLite and a Caddy reverse proxy, you will not hit any compatibility issues.

## Part 5: Aspire's Docker Compose Publisher

### Let Aspire Generate the Compose File

We wrote our Compose file by hand in Part 4, and that is a perfectly valid approach for simple applications. But Aspire can generate Compose files from your application model, which becomes valuable as your application grows to include more services.

To use Aspire's Docker Compose publisher, first add the Docker hosting integration to your AppHost:

```bash
cd Virginia.AppHost
dotnet add package Aspire.Hosting.Docker
```

Then update `AppHost.cs` to add a Docker Compose environment:

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var compose = builder.AddDockerComposeEnvironment("local");

builder.AddProject<Projects.Virginia>("virginia")
    .WithExternalHttpEndpoints();

builder.Build().Run();
```

Now you can use the Aspire CLI to publish:

```bash
# Install the Aspire CLI if you have not already
dotnet tool install --global aspire.cli

# Generate Docker Compose artifacts
aspire publish --output-path ./aspire-output
```

This generates a `compose.yaml`, a `.env` file with parameterized values, and potentially a Containerfile for the Virginia project. The generated Compose file includes the Aspire Dashboard as an optional service for telemetry visualization.

The generated files serve as a starting point. You can edit them, add your Caddy reverse proxy service, adjust environment variables, and commit the result to your repository. The power of this approach is that Aspire understands your application model — it knows which services depend on which, what ports they use, and what configuration they need. For a two-service application like Virginia, the manual approach is fine. For a ten-service application with Redis, PostgreSQL, RabbitMQ, and three APIs, having Aspire generate the initial Compose file saves significant time.

### The Current Podman Caveat

As of Aspire 13.2, the Docker Compose publisher uses the `docker` CLI internally. This means that when you run `aspire deploy` to apply the Compose file, Aspire expects `docker compose` to be available. If you are using Podman exclusively, the deploy step will fail.

The workaround is straightforward: use `aspire publish` to generate the artifacts, then use `podman-compose` to deploy them yourself. The generated `compose.yaml` is standard Compose specification YAML that works with any Compose-compatible tool.

There is an open issue on the Aspire GitHub repository requesting native Podman support for the deploy command, including auto-detection of the available container runtime. The Aspire team has acknowledged this as a gap. In the meantime, the publish-then-deploy-manually workflow works perfectly well.

```bash
# Generate artifacts with Aspire
aspire publish --output-path ./aspire-output

# Deploy with Podman (on your VPS)
cd aspire-output
podman-compose up -d
```

## Part 6: Putting Everything in Your GitHub Repository

### Repository Structure

Here is what your repository looks like with all the containerization files added:

```
virginia/
├── .github/
│   └── workflows/
│       ├── ci.yml              # Build + test (already exists)
│       └── deploy.yml          # Build image, push to registry, deploy
├── Containerfile               # Multi-stage build for the application
├── Caddyfile                   # Caddy reverse proxy configuration
├── compose.yaml                # podman-compose / docker compose file
├── compose.production.yaml     # Production overrides (optional)
├── .env.example                # Template for environment variables
├── Directory.Build.props
├── Directory.Packages.props
├── Virginia.slnx
├── Virginia/                   # Main application
├── Virginia.AppHost/           # Aspire orchestrator (dev-time)
├── Virginia.ServiceDefaults/   # Shared infrastructure
└── Virginia.Tests/             # Tests
```

### The .env.example File

Sensitive values like the admin password should not be committed to the repository. Create a `.env.example` template:

```bash
# Copy this file to .env and fill in the values
# DO NOT commit .env to source control

ASPNETCORE_ENVIRONMENT=Production
ConnectionStrings__DefaultConnection=Data Source=/data/virginia.db
AdminUser__Email=admin@virginia.local
AdminUser__Password=CHANGE_ME_TO_A_STRONG_PASSWORD
```

Add `.env` to your `.gitignore` so the actual values are never committed:

```
# Environment files with secrets
.env
```

On your VPS, copy `.env.example` to `.env` and fill in the real values:

```bash
cp .env.example .env
nano .env  # edit with real values
```

Update `compose.yaml` to use the `.env` file:

```yaml
services:
  virginia:
    build:
      context: .
      dockerfile: Containerfile
    container_name: virginia
    restart: unless-stopped
    env_file:
      - .env
    volumes:
      - virginia-data:/data
    networks:
      - web
```

### CI/CD with GitHub Actions

You can automate the entire build-push-deploy pipeline with GitHub Actions. Here is a workflow that builds the container image, pushes it to GitHub Container Registry, and deploys to your VPS via SSH:

```yaml
name: Build and Deploy

on:
  push:
    branches: [main, master]

permissions:
  contents: read
  packages: write

env:
  FORCE_JAVASCRIPT_ACTIONS_TO_NODE24: true
  REGISTRY: ghcr.io
  IMAGE_NAME: ${{ github.repository }}

jobs:
  test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: 10.0.x
      - run: dotnet restore Virginia.slnx
      - run: dotnet build Virginia.slnx --no-restore --configuration Release
      - run: dotnet test Virginia.slnx --no-build --configuration Release

  build-and-push:
    needs: test
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      - name: Log in to GitHub Container Registry
        run: echo "${{ secrets.GITHUB_TOKEN }}" | podman login ghcr.io -u ${{ github.actor }} --password-stdin

      - name: Build container image
        run: podman build -t ${{ env.REGISTRY }}/${{ env.IMAGE_NAME }}:latest -f Containerfile .

      - name: Push to registry
        run: podman push ${{ env.REGISTRY }}/${{ env.IMAGE_NAME }}:latest

  deploy:
    needs: build-and-push
    runs-on: ubuntu-latest
    steps:
      - name: Deploy to VPS via SSH
        uses: appleboy/ssh-action@v1
        with:
          host: ${{ secrets.VPS_HOST }}
          username: ${{ secrets.VPS_USER }}
          key: ${{ secrets.VPS_SSH_KEY }}
          script: |
            cd /opt/virginia
            podman pull ${{ env.REGISTRY }}/${{ env.IMAGE_NAME }}:latest
            podman-compose down virginia
            podman-compose up -d virginia
```

This workflow requires three GitHub repository secrets: `VPS_HOST` (your server's IP or hostname), `VPS_USER` (the SSH username), and `VPS_SSH_KEY` (the private SSH key for authentication).

On the VPS, your `/opt/virginia` directory contains the `compose.yaml`, `Caddyfile`, and `.env` files. The deploy step pulls the latest image from GitHub Container Registry and restarts the Virginia container. Caddy continues running.

An alternative approach that avoids a container registry entirely: clone the repository on the VPS and build locally:

```bash
# On the VPS
cd /opt/virginia
git pull
podman-compose build virginia
podman-compose up -d virginia
```

This is simpler but slower (the VPS has to compile the application) and requires the .NET SDK to be available during the build stage (which it is, inside the container build — the SDK is in the build stage of the Containerfile).

## Part 7: Do You Need Kubernetes?

### The Short Answer

No. Not for this. Not for most things.

### The Longer Answer

Kubernetes is a container orchestration platform designed for running applications at scale across multiple machines. It provides automated scheduling (deciding which machine runs each container), self-healing (restarting failed containers, replacing unhealthy nodes), horizontal scaling (running multiple copies of a service and load-balancing between them), service discovery, configuration management, and rolling updates with zero downtime.

These are real capabilities that solve real problems — if you have those problems. Here is when you need Kubernetes:

**You are running dozens or hundreds of services.** Kubernetes shines when you have a large number of interdependent services that need to be scheduled across a cluster of machines. The overhead of Kubernetes (etcd, the API server, the scheduler, the controller manager, kubelet on every node) is justified by the automation it provides at scale.

**You need horizontal auto-scaling.** If your traffic is unpredictable and you need to automatically scale from 2 to 20 instances of your API based on CPU usage or request rate, Kubernetes does this out of the box.

**You require high availability across multiple machines.** If your application must survive the failure of an entire server, you need multiple nodes and a system that automatically moves workloads when a node dies. Kubernetes does this.

**You are in an organization that already has a Kubernetes cluster and a platform team.** If the infrastructure is already there and someone else manages it, deploying to Kubernetes is reasonable.

Here is when you do not need Kubernetes:

**You have one application on one server.** Virginia is a Blazor Server application with a SQLite database. It runs on a single machine. There is nothing to orchestrate across multiple nodes.

**You have 2-5 services.** A Compose file handles this perfectly. You do not need a control plane, an API server, or a scheduler to run five containers on one machine.

**Your team does not have Kubernetes expertise.** Kubernetes has a steep learning curve. The operational complexity of running a Kubernetes cluster (upgrading, patching, monitoring the control plane, managing certificates, debugging networking issues) is substantial. If you are a solo developer or a small team, that operational burden is not justified for a simple deployment.

**You are trying to save money.** A $5/month VPS with Podman and Compose is significantly cheaper than any managed Kubernetes service. Even self-hosted Kubernetes (k3s, for example) adds overhead in terms of memory usage, disk usage, and your time maintaining it.

### The Architecture Spectrum

Think of deployment architectures as a spectrum:

```
Simple ────────────────────────────────────────────── Complex

Single process     Compose/Podman     k3s/MicroK8s     Full Kubernetes
(no containers)    (single machine)   (single machine)  (multi-node cluster)
```

Virginia sits squarely in the "Compose/Podman on a single machine" zone. If Virginia grew into a multi-tenant SaaS application with a separate API, a job queue, a PostgreSQL cluster, and Redis, it might move to k3s on a single beefy server. If it grew further to handle millions of users with geographic distribution, then full Kubernetes would be appropriate.

Do not adopt the complexity of the right side of the spectrum before your application's needs require it. You can always migrate later — and because everything is in OCI-compliant containers, migration is a matter of writing new deployment manifests, not rewriting your application.

## Part 8: Advanced Containerfile Techniques

### Health Checks

Container health checks let the runtime (and Compose, and Kubernetes) know whether your application is actually healthy, not just running. Add a health check to your Compose file:

```yaml
services:
  virginia:
    # ... other configuration ...
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:8080/health"]
      interval: 30s
      timeout: 10s
      retries: 3
      start_period: 15s
```

Since the ASP.NET Core runtime image does not include `curl`, you can instead use a .NET-based health check. Add `wget` as an alternative (it is available in the base image), or install `curl` in your Containerfile:

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime

# Install curl for health checks (adds ~3 MB to the image)
RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/*
```

Virginia already configures health check endpoints at `/health` and `/alive` through the Aspire Service Defaults. These are ASP.NET Core health checks that return HTTP 200 when the application is healthy. In development mode (which the Service Defaults library checks), these endpoints are mapped automatically. For production, you may want to map them unconditionally by modifying `MapDefaultEndpoints` in `Extensions.cs`:

```csharp
public static WebApplication MapDefaultEndpoints(this WebApplication app)
{
    // Always map health checks, not just in development
    app.MapHealthChecks("/health");
    app.MapHealthChecks("/alive", new HealthCheckOptions
    {
        Predicate = r => r.Tags.Contains("live")
    });

    return app;
}
```

### SQLite Backup Strategy

One thing that makes people nervous about SQLite in containers is backup. The database is inside a volume — how do you back it up?

Option 1: Copy the file. SQLite supports safe copying while the database is in use, as long as you use SQLite's backup API or copy during a WAL checkpoint. The simplest approach:

```bash
# On the VPS, run a backup
podman exec virginia sqlite3 /data/virginia.db ".backup /data/virginia-backup.db"

# Copy the backup to your local machine
scp user@vps:/opt/virginia-data/virginia-backup.db ./backups/
```

Option 2: Use a cron job on the host:

```bash
# /etc/cron.daily/backup-virginia
#!/bin/bash
BACKUP_DIR=/opt/backups/virginia
mkdir -p "$BACKUP_DIR"
podman exec virginia sqlite3 /data/virginia.db ".backup /tmp/backup.db"
podman cp virginia:/tmp/backup.db "$BACKUP_DIR/virginia-$(date +%Y%m%d).db"
# Keep only the last 30 days
find "$BACKUP_DIR" -name "*.db" -mtime +30 -delete
```

Option 3: Volume-level backup. Podman volumes are stored on the host filesystem (typically under `/var/lib/containers/storage/volumes/` or `~/.local/share/containers/storage/volumes/` for rootless). You can back up the entire volume directory with standard filesystem tools.

### OpenTelemetry in Production

Virginia already has comprehensive OpenTelemetry instrumentation through the Service Defaults library. In production, you probably want to send this telemetry somewhere persistent rather than just the Aspire Dashboard (which is a development tool).

You can add a lightweight telemetry backend to your Compose file. Here is an example using Grafana's free, open-source LGTM stack (Loki for logs, Grafana for dashboards, Tempo for traces, Mimir for metrics) via the all-in-one container:

```yaml
services:
  virginia:
    # ... existing configuration ...
    environment:
      - OTEL_EXPORTER_OTLP_ENDPOINT=http://otel-collector:4317
      - OTEL_EXPORTER_OTLP_PROTOCOL=grpc

  otel-collector:
    image: grafana/alloy:latest
    container_name: otel-collector
    restart: unless-stopped
    volumes:
      - ./alloy-config.yaml:/etc/alloy/config.alloy:ro
    networks:
      - web

  grafana:
    image: grafana/grafana:latest
    container_name: grafana
    restart: unless-stopped
    ports:
      - "3000:3000"
    volumes:
      - grafana-data:/var/lib/grafana
    networks:
      - web

volumes:
  grafana-data:
```

This is optional. Virginia works perfectly well without it. But if you want the same observability in production that the Aspire Dashboard gives you in development, an OTLP collector and Grafana is the way to get it.

## Part 9: Security Considerations

### Running on a VPS

When you deploy to a VPS, you are responsible for the security of the server. Here is a minimal security checklist:

**SSH hardening.** Disable password authentication and use SSH keys only. Disable root login over SSH. Use a non-standard SSH port if you want to reduce noise from automated scanners.

```bash
# /etc/ssh/sshd_config
PasswordAuthentication no
PermitRootLogin no
Port 2222
```

**Firewall.** Allow only ports 80, 443 (for Caddy), and your SSH port. Block everything else.

```bash
sudo ufw default deny incoming
sudo ufw default allow outgoing
sudo ufw allow 2222/tcp   # SSH
sudo ufw allow 80/tcp     # HTTP (Caddy redirect)
sudo ufw allow 443/tcp    # HTTPS (Caddy)
sudo ufw enable
```

**Automatic security updates.** On Debian/Ubuntu:

```bash
sudo apt-get install -y unattended-upgrades
sudo dpkg-reconfigure -plow unattended-upgrades
```

**Container isolation.** Podman's rootless mode means your containers run as a regular user, not root. Even if an attacker escapes the container, they have limited privileges on the host.

### Application-Level Security

Virginia uses ASP.NET Core Identity for authentication with bcrypt-hashed passwords, cookie authentication with HTTPS-only cookies, anti-forgery tokens on all forms, and an approval-based registration flow (new users must be approved by an admin before they can log in).

The admin credentials are configured via environment variables (`AdminUser__Email` and `AdminUser__Password`), which are stored in the `.env` file on the VPS. Make sure this file has restrictive permissions:

```bash
chmod 600 /opt/virginia/.env
```

### Container Image Security

Keep your base images up to date. The `mcr.microsoft.com/dotnet/aspnet:10.0` image is regularly updated with security patches. To ensure your deployment uses the latest patched base image:

```bash
# Pull the latest base image before rebuilding
podman pull mcr.microsoft.com/dotnet/aspnet:10.0
podman-compose build --no-cache virginia
podman-compose up -d virginia
```

You can automate this with a weekly cron job or a GitHub Actions scheduled workflow.

## Part 10: When Things Go Wrong — Troubleshooting

### Container Will Not Start

```bash
# Check the logs
podman-compose logs virginia

# Common issues:
# 1. Port already in use → another process is listening on 8080
# 2. Volume permission denied → the /data directory is not writable by appuser
# 3. Missing environment variable → check your .env file
```

### Database Locked Errors

SQLite allows multiple readers but only one writer at a time. If you see "database is locked" errors, it usually means two processes are trying to write simultaneously. In a single-container deployment, this should not happen because there is only one application process. If it does happen:

```bash
# Check that WAL mode is enabled (it is by default in EF Core with SQLite)
podman exec virginia sqlite3 /data/virginia.db "PRAGMA journal_mode;"
# Should output: wal
```

WAL (Write-Ahead Logging) mode allows concurrent reads and writes and is EF Core's default for SQLite. If it is not enabled, you can set it in your connection string:

```
Data Source=/data/virginia.db;Cache=Shared
```

### Caddy Certificate Issues

If Caddy cannot obtain a TLS certificate, check:

```bash
podman-compose logs caddy

# Common issues:
# 1. DNS not pointing to your server → verify with dig or nslookup
# 2. Ports 80/443 blocked by firewall → check ufw status
# 3. Rate limited by Let's Encrypt → wait an hour and try again
```

### Updating Without Downtime

For a single-instance deployment, there will be a brief period of downtime when the container restarts. To minimize it:

```bash
# Build the new image first (this takes time)
podman-compose build virginia

# The restart itself is fast (usually 2-3 seconds)
podman-compose up -d virginia
```

If you need true zero-downtime deployments, you would need a load balancer (like Caddy or Traefik) in front of two instances of the application, deploying one at a time. But for a personal application or small team tool, a 2-3 second restart during deployment is usually acceptable.

## Part 11: Putting It All Together — The Complete Deployment Walkthrough

Let us walk through the entire process from scratch. You have a VPS running Debian 12 or Ubuntu 24.04 with a fresh installation.

### Step 1: Server Setup

```bash
# SSH into your VPS
ssh root@your-server-ip

# Create a non-root user
adduser deploy
usermod -aG sudo deploy

# Switch to the new user
su - deploy

# Install Podman and podman-compose
sudo apt-get update
sudo apt-get install -y podman podman-compose git curl

# Verify
podman --version
podman-compose --version
```

### Step 2: Configure Firewall

```bash
sudo apt-get install -y ufw
sudo ufw default deny incoming
sudo ufw default allow outgoing
sudo ufw allow OpenSSH
sudo ufw allow 80/tcp
sudo ufw allow 443/tcp
sudo ufw enable
```

### Step 3: Clone and Configure

```bash
# Clone the repository
sudo mkdir -p /opt/virginia
sudo chown deploy:deploy /opt/virginia
cd /opt/virginia
git clone https://github.com/collabskus/virginia.git .

# Create the environment file
cp .env.example .env
nano .env
# Set a strong admin password and any other configuration

# Make the env file readable only by the owner
chmod 600 .env
```

### Step 4: Configure DNS

In your domain registrar's DNS settings, create an A record:

```
virginia.yourdomain.com    A    your-server-ip
```

Wait for DNS propagation (usually a few minutes, sometimes up to 48 hours).

### Step 5: Update the Caddyfile

```bash
nano Caddyfile
# Replace virginia.yourdomain.com with your actual domain
```

### Step 6: Deploy

```bash
podman-compose up -d
```

Podman will:
1. Build the Virginia container image from the Containerfile
2. Pull the Caddy image
3. Create the named volumes
4. Create the network
5. Start both containers

### Step 7: Verify

```bash
# Check both containers are running
podman-compose ps

# Check Virginia logs
podman-compose logs virginia

# Check Caddy logs (certificate acquisition)
podman-compose logs caddy

# Test with curl
curl -I https://virginia.yourdomain.com
```

You should see a 200 OK response with HTTPS headers. Open the URL in your browser, and you should see the Virginia login page. Log in with the admin credentials from your `.env` file.

### Step 8: Set Up Automatic Restarts

Podman containers with `restart: unless-stopped` will restart when Podman itself starts. To ensure Podman's systemd socket is active on boot for rootless containers:

```bash
# Enable lingering for the deploy user (keeps user services running after logout)
sudo loginctl enable-linger deploy

# Generate and enable a systemd service for the compose stack
cd /opt/virginia
podman-compose down
podman generate systemd --new --files --name virginia
podman generate systemd --new --files --name caddy

# Move the service files and enable them
mkdir -p ~/.config/systemd/user/
mv container-*.service ~/.config/systemd/user/
systemctl --user daemon-reload
systemctl --user enable container-virginia.service
systemctl --user enable container-caddy.service
```

Alternatively, and more simply, you can use a systemd service that runs `podman-compose up -d`:

```bash
# /etc/systemd/system/virginia.service
[Unit]
Description=Virginia Application Stack
After=network-online.target
Wants=network-online.target

[Service]
Type=oneshot
RemainAfterExit=yes
WorkingDirectory=/opt/virginia
ExecStart=/usr/bin/podman-compose up -d
ExecStop=/usr/bin/podman-compose down
User=deploy

[Install]
WantedBy=multi-user.target
```

```bash
sudo systemctl enable virginia.service
sudo systemctl start virginia.service
```

## Part 12: Summary and Resources

### What We Covered

We started with Aspire — what it is (a dev-time orchestrator and deploy-time artifact generator), what it is not (a runtime, a hosting platform, or an Azure lock-in). We looked at how Aspire structures a .NET application with AppHost and Service Defaults projects, and how the Virginia sample application uses Aspire 13.1 for local development with OpenTelemetry observability.

We then moved to OCI containers, establishing vendor-neutral terminology (Containerfile, not Dockerfile; container runtime, not Docker) and explaining why Podman is a compelling choice for self-hosted deployments — daemonless, rootless, free, and CLI-compatible with Docker.

We wrote a multi-stage Containerfile for Virginia with proper layer caching, non-root execution, and volume-based SQLite persistence. We composed it with Caddy for automatic HTTPS using podman-compose. We explored Aspire's Docker Compose publisher and its current Podman limitations. We discussed CI/CD with GitHub Actions, security hardening, backup strategies, and troubleshooting.

And we answered the big question: **No, you do not need Kubernetes.** For a single application on a single server, podman-compose is the right tool. Kubernetes solves problems of scale and multi-node orchestration that a personal or small-team application simply does not have.

### The Key Takeaway

The simplest deployment that works is the best deployment. A Containerfile, a Compose file, a Caddyfile, and a $5 VPS give you a production-ready deployment with automatic HTTPS, persistent storage, automatic restarts, and a reproducible, version-controlled configuration. You can always add complexity later — a container registry, a CI/CD pipeline, monitoring with Grafana, or even Kubernetes — but start simple and add layers only when you have a real need for them.

### Resources

Here are the official resources for everything covered in this article:

- **Aspire documentation**: [aspire.dev](https://aspire.dev) — the official docs site, covering all Aspire features including the Docker Compose publisher
- **Aspire GitHub repository**: [github.com/microsoft/aspire](https://github.com/microsoft/aspire) — source code, issues, discussions, and roadmap
- **Aspire 13.2 release notes**: [aspire.dev/whats-new/aspire-13-2](https://aspire.dev/whats-new/aspire-13-2/) — the latest release as of March 2026
- **Virginia sample application**: [github.com/collabskus/virginia](https://github.com/collabskus/virginia) — the Blazor + SQLite + Aspire application used throughout this article
- **Podman documentation**: [docs.podman.io](https://docs.podman.io) — comprehensive Podman documentation
- **Podman Desktop**: [podman-desktop.io](https://podman-desktop.io) — GUI for Podman on macOS, Windows, and Linux
- **podman-compose repository**: [github.com/containers/podman-compose](https://github.com/containers/podman-compose) — the Compose implementation for Podman
- **OCI specifications**: [opencontainers.org](https://opencontainers.org) — the Open Container Initiative standards
- **Caddy web server**: [caddyserver.com](https://caddyserver.com) — automatic HTTPS, reverse proxy, and more
- **Aspire SSH deploy template**: [github.com/davidfowl/aspire-docker-ssh-template](https://github.com/davidfowl/aspire-docker-ssh-template) — David Fowler's template for deploying Aspire applications over SSH
- **.NET 10 download**: [dotnet.microsoft.com/download/dotnet/10.0](https://dotnet.microsoft.com/download/dotnet/10.0) — the latest .NET 10 LTS SDK and runtime
- **Containerfile reference**: [docs.podman.io/en/latest/markdown/podman-build.1.html](https://docs.podman.io/en/latest/markdown/podman-build.1.html) — the build file specification as understood by Podman
