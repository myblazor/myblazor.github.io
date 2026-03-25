---
title: "The Complete Guide to PostgreSQL, Npgsql, and Free IDEs for .NET Developers on Linux"
date: 2026-03-28
author: observer-team
summary: Everything a .NET C# ASP.NET web developer needs to know about PostgreSQL 18, Npgsql 10, Dapper, EF Core, transactions, configuration, networking, sessions, debugging, and the best free open-source database IDEs available on Linux.
tags:
  - postgresql
  - npgsql
  - dotnet
  - efcore
  - dapper
  - linux
  - database
  - tutorial
  - tools
---

## Introduction

PostgreSQL is the world's most advanced open-source relational database. As of March 2026, the latest major version is PostgreSQL 18 (with 18.3 being the most recent patch release), and it brings transformative improvements including asynchronous I/O, virtual generated columns, UUIDv7, B-tree skip scans, temporal constraints, and OAuth authentication. For .NET developers, Npgsql 10.0.2 provides a mature, high-performance ADO.NET data provider that integrates seamlessly with both Dapper and Entity Framework Core.

This article is written for a .NET C# ASP.NET web developer who is working on Linux. We will cover everything: installing and configuring PostgreSQL on bare metal, on a VPS, and in containers; connecting from C# using Npgsql, Dapper, and EF Core; transactions, isolation levels, and concurrency; networking and session management; debugging and profiling; and a thorough review of every free and open-source database IDE available on Linux. This is meant to be the single reference you bookmark and return to.

## Part 1: PostgreSQL Fundamentals

### What Is PostgreSQL?

PostgreSQL (often called Postgres) is a free, open-source object-relational database management system released under the PostgreSQL License, which is a permissive BSD-style license. It originated at the University of California, Berkeley in the 1980s as the POSTGRES project led by Michael Stonebraker, who later won the Turing Award for this work. The project was renamed PostgreSQL in 1996 to reflect its SQL support, and the first numbered release (6.0) came in January 1997.

PostgreSQL is notable for several things that distinguish it from other databases. It uses multiversion concurrency control (MVCC) for transaction isolation, which means readers never block writers and writers never block readers. It has extraordinary standards compliance — as of the PostgreSQL 17 release in September 2024, it conforms to at least 170 of the 177 mandatory features for SQL:2023 Core conformance, which is better than any other database. It supports advanced data types including arrays, JSONB, ranges, multiranges, geometric types, full-text search vectors, and network address types. And it has a powerful extension system that allows third parties to add entirely new data types, index methods, procedural languages, and foreign data wrappers.

### What Is New in PostgreSQL 18?

PostgreSQL 18 was released on September 25, 2025, and it is one of the most significant releases in the project's history. Here are the headline features.

**Asynchronous I/O (AIO).** This is the marquee feature. PostgreSQL 18 introduces an entirely new I/O subsystem that allows backends to queue multiple read requests concurrently instead of waiting for each one to finish before starting the next. This dramatically improves throughput for sequential scans, bitmap heap scans, and VACUUM operations. The new `io_method` configuration parameter controls how AIO works, with three possible values: `sync` (the traditional blocking behavior from PostgreSQL 17), `worker` (the default in PostgreSQL 18, which delegates reads to background I/O worker processes), and `io_uring` (which uses Linux's high-performance io_uring interface, available on kernel 5.1 and later, and provides the best performance by establishing a shared ring buffer between PostgreSQL and the kernel). Benchmarks have shown up to 2–3x performance improvements in disk-read throughput for typical workloads when using the `worker` or `io_uring` methods. Note that AIO currently applies only to read operations; writes and WAL operations remain synchronous.

To check your AIO configuration:

```sql
SHOW io_method;
SHOW io_workers;
SHOW effective_io_concurrency;
SHOW maintenance_io_concurrency;
```

**B-tree Skip Scan.** PostgreSQL 18 adds skip scan capability to B-tree indexes. If you have a composite index on `(region, category, date)`, queries that filter on `category` and `date` but omit `region` can now efficiently use the index by "skipping" over distinct values of the leading column. Previously, such queries would fall back to a sequential scan or require a separate index.

**UUIDv7.** The new `uuidv7()` function generates timestamp-ordered UUIDs that are monotonically increasing over time. This is a big deal for indexed primary keys because, unlike UUIDv4 (which is random and causes B-tree page splits), UUIDv7 values are inserted in order, which gives you the uniqueness of UUIDs with the insertion performance of auto-incrementing integers.

**Virtual Generated Columns.** PostgreSQL 18 makes virtual generated columns the default for generated columns. These compute their values at query time rather than storing them on disk, which saves storage space and eliminates the need to update them when the source columns change.

**Temporal Constraints.** You can now define PRIMARY KEY, UNIQUE, and FOREIGN KEY constraints that operate over ranges, enabling bitemporal and temporal data modeling directly in the database.

**OAuth 2.0 Authentication.** PostgreSQL 18 supports OAuth 2.0 for client authentication, making it easier to integrate with single-sign-on systems and cloud identity providers.

**Wire Protocol 3.2.** This is the first new version of the PostgreSQL wire protocol since PostgreSQL 7.4 in 2003. It lays the groundwork for future improvements to client-server communication.

**Other improvements** include parallel GIN index builds, improved hash join performance, smarter OR/IN query optimization, a new `pg_aios` system view for monitoring AIO operations, `log_lock_failures` for logging lock acquisition failures, and new columns in `pg_stat_all_tables` for tracking vacuum and analyze durations.

### Supported Versions and End of Life

The PostgreSQL Global Development Group supports each major version for five years. As of March 2026, the supported versions are PostgreSQL 18 (through 2030), 17 (through 2029), 16 (through 2028), 15 (through 2027), and 14 (through 2026). Version 13 reached end-of-life in November 2025. Always run the latest minor release for whichever major version you are on.

## Part 2: Installing PostgreSQL on Linux

### Installing on Bare Metal (Fedora, Ubuntu, Debian)

On Fedora:

```bash
sudo dnf install postgresql-server postgresql-contrib
sudo postgresql-setup --initdb
sudo systemctl start postgresql
sudo systemctl enable postgresql
```

On Ubuntu or Debian, you should add the official PostgreSQL APT repository for the latest version:

```bash
sudo apt install -y postgresql-common
sudo /usr/share/postgresql-common/pgdg/apt.postgresql.org.sh
sudo apt install -y postgresql-18
```

After installation, PostgreSQL runs as the `postgres` system user. Switch to it and create your first database:

```bash
sudo -u postgres psql
```

Inside psql:

```sql
CREATE ROLE myappuser WITH LOGIN PASSWORD 'securepassword';
CREATE DATABASE myappdb OWNER myappuser;
GRANT ALL PRIVILEGES ON DATABASE myappdb TO myappuser;
\q
```

### Installing with Docker or Podman

For development, running PostgreSQL in a container is the most convenient approach. It avoids polluting your system, makes version management trivial, and mirrors production environments.

With Docker:

```bash
docker run -d \
  --name pg-dev \
  -e POSTGRES_USER=myappuser \
  -e POSTGRES_PASSWORD=securepassword \
  -e POSTGRES_DB=myappdb \
  -p 5432:5432 \
  -v pgdata:/var/lib/postgresql/data \
  postgres:18
```

With Podman (rootless):

```bash
podman run -d \
  --name pg-dev \
  -e POSTGRES_USER=myappuser \
  -e POSTGRES_PASSWORD=securepassword \
  -e POSTGRES_DB=myappdb \
  -p 5432:5432 \
  -v pgdata:/var/lib/postgresql/data \
  docker.io/library/postgres:18
```

Using a `docker-compose.yml` (or `podman-compose`):

```yaml
services:
  postgres:
    image: postgres:18
    container_name: pg-dev
    environment:
      POSTGRES_USER: myappuser
      POSTGRES_PASSWORD: securepassword
      POSTGRES_DB: myappdb
    ports:
      - "5432:5432"
    volumes:
      - pgdata:/var/lib/postgresql/data
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U myappuser -d myappdb"]
      interval: 10s
      timeout: 5s
      retries: 5

volumes:
  pgdata:
```

### Installing on a VPS

On a VPS (DigitalOcean, Linode, Hetzner, etc.), you install the same way as bare metal, but with additional security considerations:

1. Install PostgreSQL using the official repository as shown above.
2. Change the default `postgres` user password immediately.
3. Configure `pg_hba.conf` to allow remote connections from your application server's IP only (see the configuration section below).
4. Configure `postgresql.conf` to listen on the VPS's private network interface rather than `0.0.0.0`.
5. Set up a firewall (ufw, firewalld, or iptables) to only allow port 5432 from trusted IPs.
6. Enable SSL for all connections.
7. Consider using a connection pooler like PgBouncer in front of PostgreSQL.

## Part 3: PostgreSQL Configuration Files

PostgreSQL has three primary configuration files. Understanding them is essential.

### postgresql.conf

This is the main configuration file that controls server behavior. On a typical Linux installation, it lives at `/etc/postgresql/18/main/postgresql.conf` (Debian/Ubuntu) or `/var/lib/pgsql/data/postgresql.conf` (Fedora/RHEL). Key settings:

**Connection settings:**

```
listen_addresses = 'localhost'          # For local-only dev
listen_addresses = '0.0.0.0'           # For remote access (use with pg_hba.conf)
port = 5432
max_connections = 100                   # Default; increase for high-concurrency apps
```

**Memory settings:**

```
shared_buffers = '256MB'               # 25% of RAM for dedicated DB server
effective_cache_size = '768MB'         # 50-75% of RAM
work_mem = '4MB'                       # Per-sort/hash operation; be careful
maintenance_work_mem = '64MB'          # For VACUUM, CREATE INDEX
```

**WAL (Write-Ahead Log) settings:**

```
wal_level = 'replica'                  # Required for replication
max_wal_size = '1GB'
min_wal_size = '80MB'
checkpoint_completion_target = 0.9
```

**Asynchronous I/O settings (PostgreSQL 18):**

```
io_method = 'worker'                   # 'sync', 'worker', or 'io_uring'
io_workers = 3                         # Number of background I/O worker processes
effective_io_concurrency = 16          # Raised default in PG18
maintenance_io_concurrency = 16
```

**Logging settings:**

```
logging_collector = on
log_directory = 'log'
log_filename = 'postgresql-%Y-%m-%d_%H%M%S.log'
log_min_duration_statement = 500       # Log queries taking > 500ms
log_statement = 'none'                 # 'none', 'ddl', 'mod', or 'all'
log_connections = on
log_disconnections = on
log_lock_failures = on                 # New in PG18
```

**Performance monitoring:**

```
shared_preload_libraries = 'pg_stat_statements'
pg_stat_statements.track = 'all'
```

### pg_hba.conf

This file controls client authentication — who can connect, from where, and how. Each line has the format:

```
# TYPE  DATABASE  USER  ADDRESS        METHOD
local   all       all                  peer
host    all       all   127.0.0.1/32   scram-sha-256
host    all       all   ::1/128        scram-sha-256
```

For development, you might add:

```
host    myappdb   myappuser  192.168.1.0/24  scram-sha-256
```

For a VPS allowing your application server:

```
hostssl myappdb   myappuser  10.0.0.5/32     scram-sha-256
```

The `hostssl` type requires SSL connections. Always use `scram-sha-256` rather than the older `md5` method. Never use `trust` in production.

After editing `pg_hba.conf`, reload the configuration:

```bash
sudo systemctl reload postgresql
```

### pg_ident.conf

This file maps operating system user names to PostgreSQL user names when using `peer` or `ident` authentication. Most applications use password-based authentication and do not need to modify this file.

### Configuration for Docker/Podman

When running in a container, you can mount a custom `postgresql.conf`:

```bash
docker run -d \
  --name pg-dev \
  -e POSTGRES_USER=myappuser \
  -e POSTGRES_PASSWORD=securepassword \
  -e POSTGRES_DB=myappdb \
  -p 5432:5432 \
  -v pgdata:/var/lib/postgresql/data \
  -v ./custom-postgresql.conf:/etc/postgresql/postgresql.conf \
  postgres:18 -c 'config_file=/etc/postgresql/postgresql.conf'
```

Alternatively, you can pass individual settings as command-line arguments:

```bash
docker run -d postgres:18 \
  -c 'shared_buffers=256MB' \
  -c 'max_connections=200' \
  -c 'log_min_duration_statement=500'
```

## Part 4: The psql Terminal Client

`psql` is the interactive terminal client for PostgreSQL and is the single most important tool you will use. Every .NET developer working with PostgreSQL should be comfortable with it.

### Connecting

```bash
# Connect to local database
psql -U myappuser -d myappdb

# Connect to remote database
psql -h 192.168.1.100 -p 5432 -U myappuser -d myappdb

# Using a connection URI
psql "postgresql://myappuser:securepassword@localhost:5432/myappdb?sslmode=require"
```

### Essential Meta-Commands

```
\l              -- List all databases
\dt             -- List tables in current schema
\dt+            -- List tables with sizes
\d tablename    -- Describe a table (columns, types, constraints)
\di             -- List indexes
\df             -- List functions
\du             -- List roles/users
\dn             -- List schemas
\dx             -- List installed extensions
\conninfo       -- Show current connection info
\timing on      -- Show query execution time
\x              -- Toggle expanded display (useful for wide rows)
\i filename.sql -- Execute SQL from a file
\e              -- Open the last query in your $EDITOR
\q              -- Quit
```

### Useful Queries for Diagnostics

Check active connections and running queries:

```sql
SELECT pid, usename, datname, client_addr, state, query, query_start
FROM pg_stat_activity
WHERE state != 'idle'
ORDER BY query_start;
```

Check table sizes:

```sql
SELECT schemaname, tablename,
       pg_size_pretty(pg_total_relation_size(schemaname || '.' || tablename)) AS total_size
FROM pg_tables
WHERE schemaname = 'public'
ORDER BY pg_total_relation_size(schemaname || '.' || tablename) DESC;
```

Check index usage:

```sql
SELECT schemaname, tablename, indexname, idx_scan, idx_tup_read, idx_tup_fetch
FROM pg_stat_user_indexes
ORDER BY idx_scan DESC;
```

Find unused indexes (candidates for removal):

```sql
SELECT schemaname, tablename, indexname, idx_scan
FROM pg_stat_user_indexes
WHERE idx_scan = 0
  AND indexrelname NOT LIKE '%pkey%'
ORDER BY pg_relation_size(indexrelid) DESC;
```

Find slow queries (requires pg_stat_statements):

```sql
SELECT query, calls, mean_exec_time, total_exec_time, rows
FROM pg_stat_statements
ORDER BY mean_exec_time DESC
LIMIT 20;
```

Check lock contention:

```sql
SELECT blocked_locks.pid AS blocked_pid,
       blocking_locks.pid AS blocking_pid,
       blocked_activity.usename AS blocked_user,
       blocking_activity.usename AS blocking_user,
       blocked_activity.query AS blocked_query,
       blocking_activity.query AS blocking_query
FROM pg_catalog.pg_locks blocked_locks
JOIN pg_catalog.pg_stat_activity blocked_activity ON blocked_activity.pid = blocked_locks.pid
JOIN pg_catalog.pg_locks blocking_locks ON blocking_locks.locktype = blocked_locks.locktype
  AND blocking_locks.database IS NOT DISTINCT FROM blocked_locks.database
  AND blocking_locks.relation IS NOT DISTINCT FROM blocked_locks.relation
  AND blocking_locks.page IS NOT DISTINCT FROM blocked_locks.page
  AND blocking_locks.tuple IS NOT DISTINCT FROM blocked_locks.tuple
  AND blocking_locks.virtualxid IS NOT DISTINCT FROM blocked_locks.virtualxid
  AND blocking_locks.transactionid IS NOT DISTINCT FROM blocked_locks.transactionid
  AND blocking_locks.classid IS NOT DISTINCT FROM blocked_locks.classid
  AND blocking_locks.objid IS NOT DISTINCT FROM blocked_locks.objid
  AND blocking_locks.objsubid IS NOT DISTINCT FROM blocked_locks.objsubid
  AND blocking_locks.pid != blocked_locks.pid
JOIN pg_catalog.pg_stat_activity blocking_activity ON blocking_activity.pid = blocking_locks.pid
WHERE NOT blocked_locks.granted;
```

Monitor AIO operations (PostgreSQL 18):

```sql
SELECT * FROM pg_aios;
```

### EXPLAIN and EXPLAIN ANALYZE

Always use `EXPLAIN ANALYZE` to understand query plans:

```sql
EXPLAIN ANALYZE
SELECT o.id, o.total, c.name
FROM orders o
JOIN customers c ON c.id = o.customer_id
WHERE o.created_at > '2026-01-01'
ORDER BY o.total DESC
LIMIT 100;
```

The output will show the execution plan including actual execution time, row estimates vs actual rows, which indexes were used, and where sequential scans occurred. Pay attention to large discrepancies between estimated and actual rows, which indicate stale statistics (run `ANALYZE tablename`).

## Part 5: Npgsql — The .NET PostgreSQL Driver

### What Is Npgsql?

Npgsql is the open-source ADO.NET data provider for PostgreSQL, implemented entirely in C#. As of March 2026, the latest version is Npgsql 10.0.2. It is released under the PostgreSQL License (permissive, BSD-like) and is completely free for any use, commercial or otherwise.

Npgsql provides high-performance access to PostgreSQL, regularly appearing near the top of the TechEmpower Web Framework Benchmarks. It supports the full range of PostgreSQL data types including arrays, enums, ranges, multiranges, composites, JSONB, PostGIS geometry, and NodaTime temporal types. Starting with version 8.0, Npgsql is fully compatible with NativeAOT and trimming.

### Installation

Add Npgsql to your project:

```bash
dotnet add package Npgsql
```

Or in your `Directory.Packages.props` if using central package management:

```xml
<PackageVersion Include="Npgsql" Version="10.0.2" />
```

### The NpgsqlDataSource Pattern (Recommended)

Starting with Npgsql 7.0, the recommended way to use Npgsql is through `NpgsqlDataSource`, which manages connection pooling and configuration in one place:

```csharp
using Npgsql;

var connString = "Host=localhost;Port=5432;Username=myappuser;Password=securepassword;Database=myappdb";
var dataSourceBuilder = new NpgsqlDataSourceBuilder(connString);
// Configure the data source (type mappings, logging, etc.)
await using var dataSource = dataSourceBuilder.Build();

// Get a connection from the pool
await using var conn = await dataSource.OpenConnectionAsync();

// Insert data
await using (var cmd = new NpgsqlCommand("INSERT INTO products (name, price) VALUES (@name, @price)", conn))
{
    cmd.Parameters.AddWithValue("name", "Widget");
    cmd.Parameters.AddWithValue("price", 29.99m);
    await cmd.ExecuteNonQueryAsync();
}

// Query data
await using (var cmd = new NpgsqlCommand("SELECT id, name, price FROM products WHERE price > @min", conn))
{
    cmd.Parameters.AddWithValue("min", 10.00m);
    await using var reader = await cmd.ExecuteReaderAsync();
    while (await reader.ReadAsync())
    {
        Console.WriteLine($"{reader.GetInt32(0)}: {reader.GetString(1)} - ${reader.GetDecimal(2)}");
    }
}
```

In ASP.NET Core, register the data source in the DI container:

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddNpgsqlDataSource(
    builder.Configuration.GetConnectionString("Postgres")!);
```

Then inject `NpgsqlDataSource` or `NpgsqlConnection` anywhere you need it.

### Connection String Parameters

The most important connection string parameters:

```
Host=localhost;
Port=5432;
Username=myappuser;
Password=securepassword;
Database=myappdb;
Pooling=true;                    # Default: true
Minimum Pool Size=0;             # Default: 0
Maximum Pool Size=100;           # Default: 100
Connection Idle Lifetime=300;    # Seconds before idle connection is closed
Connection Pruning Interval=10;  # Seconds between pruning checks
Timeout=30;                      # Connection timeout in seconds
Command Timeout=30;              # Default command timeout in seconds
SSL Mode=Prefer;                 # Disable, Allow, Prefer, Require, VerifyCA, VerifyFull
Trust Server Certificate=false;  # Set true for self-signed certs in dev
Include Error Detail=true;       # Include server error details (dev only!)
Log Parameters=true;             # Log parameter values (dev only!)
```

**Connection pooling best practice:** Always let Npgsql manage the connection pool. Do not set `Pooling=false` unless you are using an external pooler like PgBouncer. The default pool size of 100 is appropriate for most web applications. If you need more than 100 concurrent database connections per application instance, you likely have a design problem.

### OpenTelemetry Integration

Npgsql has built-in OpenTelemetry support via the `Npgsql.OpenTelemetry` package:

```bash
dotnet add package Npgsql.OpenTelemetry
```

```csharp
var dataSourceBuilder = new NpgsqlDataSourceBuilder(connString);
// The data source will emit OpenTelemetry traces and metrics
await using var dataSource = dataSourceBuilder.Build();

// In your OpenTelemetry setup:
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .AddNpgsql()    // Adds Npgsql instrumentation
        .AddAspNetCoreInstrumentation()
        .AddOtlpExporter());
```

### Bulk Operations with COPY

For inserting large amounts of data, PostgreSQL's COPY protocol is vastly faster than individual INSERT statements. Npgsql exposes this directly:

```csharp
await using var conn = await dataSource.OpenConnectionAsync();

await using var writer = conn.BeginBinaryImport(
    "COPY products (name, price, category) FROM STDIN (FORMAT BINARY)");

foreach (var product in products)
{
    await writer.StartRowAsync();
    await writer.WriteAsync(product.Name, NpgsqlDbType.Text);
    await writer.WriteAsync(product.Price, NpgsqlDbType.Numeric);
    await writer.WriteAsync(product.Category, NpgsqlDbType.Text);
}

await writer.CompleteAsync();
```

This can insert hundreds of thousands of rows per second, compared to hundreds per second with individual INSERT statements.

## Part 6: Using Dapper with PostgreSQL

### What Is Dapper?

Dapper is a micro-ORM that extends `IDbConnection` with methods like `Query<T>`, `Execute`, `QueryFirst<T>`, and others. It maps SQL results directly to C# objects with minimal overhead. In performance benchmarks, Dapper typically runs within a few percent of raw ADO.NET while providing much nicer syntax.

### Setup

```bash
dotnet add package Dapper
dotnet add package Npgsql
```

### Basic CRUD Operations

```csharp
using Dapper;
using Npgsql;

// Create a connection factory
public class DbConnectionFactory(string connectionString)
{
    public NpgsqlConnection CreateConnection() => new(connectionString);
}

// Register in DI
builder.Services.AddSingleton(new DbConnectionFactory(
    builder.Configuration.GetConnectionString("Postgres")!));

// Repository using Dapper
public class ProductRepository(DbConnectionFactory db)
{
    public async Task<IEnumerable<Product>> GetAllAsync()
    {
        await using var conn = db.CreateConnection();
        return await conn.QueryAsync<Product>(
            "SELECT id, name, price, category, created_at FROM products ORDER BY name");
    }

    public async Task<Product?> GetByIdAsync(int id)
    {
        await using var conn = db.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<Product>(
            "SELECT id, name, price, category, created_at FROM products WHERE id = @Id",
            new { Id = id });
    }

    public async Task<int> CreateAsync(Product product)
    {
        await using var conn = db.CreateConnection();
        return await conn.ExecuteScalarAsync<int>(
            """
            INSERT INTO products (name, price, category)
            VALUES (@Name, @Price, @Category)
            RETURNING id
            """,
            product);
    }

    public async Task<bool> UpdateAsync(Product product)
    {
        await using var conn = db.CreateConnection();
        var affected = await conn.ExecuteAsync(
            """
            UPDATE products
            SET name = @Name, price = @Price, category = @Category
            WHERE id = @Id
            """,
            product);
        return affected > 0;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        await using var conn = db.CreateConnection();
        var affected = await conn.ExecuteAsync(
            "DELETE FROM products WHERE id = @Id",
            new { Id = id });
        return affected > 0;
    }
}
```

### Important Dapper Best Practices with PostgreSQL

**Always use parameterized queries.** Never interpolate values into SQL strings. Dapper's anonymous object syntax makes parameterization easy and natural.

**Mind case sensitivity.** PostgreSQL folds unquoted identifiers to lowercase. If your C# property is `CreatedAt` but your column is `created_at`, you have two options: use the `AS` clause to alias (`SELECT created_at AS CreatedAt`), or use Dapper's `DefaultTypeMap.MatchNamesWithUnderscores = true` setting:

```csharp
Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;
```

This global setting makes Dapper automatically match `CreatedAt` to `created_at`. Set it once at startup.

**Use PostgreSQL arrays.** Dapper with Npgsql supports sending arrays directly:

```csharp
var products = await conn.QueryAsync<Product>(
    "SELECT * FROM products WHERE id = ANY(@Ids)",
    new { Ids = new[] { 1, 2, 3 } });
```

Note: use `ANY(@param)` rather than `IN @param`. The `IN` syntax with Dapper expands to individual parameters (which has a limit), while `ANY` sends a single PostgreSQL array parameter and has no such limit.

**Use JSONB.** Dapper can work with PostgreSQL's JSONB type:

```csharp
var result = await conn.QueryAsync<dynamic>(
    "SELECT id, metadata->>'name' AS name FROM products WHERE metadata @> @Filter::jsonb",
    new { Filter = "{\"category\": \"electronics\"}" });
```

**Dispose connections promptly.** Always use `await using` to ensure connections are returned to the pool. Do not hold connections across HTTP request boundaries.

### Dapper with Transactions

```csharp
await using var conn = db.CreateConnection();
await conn.OpenAsync();
await using var tx = await conn.BeginTransactionAsync();

try
{
    var orderId = await conn.ExecuteScalarAsync<int>(
        "INSERT INTO orders (customer_id, total) VALUES (@CustomerId, @Total) RETURNING id",
        new { CustomerId = 42, Total = 99.99m },
        transaction: tx);

    await conn.ExecuteAsync(
        "INSERT INTO order_items (order_id, product_id, quantity) VALUES (@OrderId, @ProductId, @Qty)",
        new { OrderId = orderId, ProductId = 7, Qty = 2 },
        transaction: tx);

    await tx.CommitAsync();
}
catch
{
    await tx.RollbackAsync();
    throw;
}
```

## Part 7: Using Entity Framework Core with PostgreSQL

### Setup

```bash
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL
```

As of March 2026, the latest version is `Npgsql.EntityFrameworkCore.PostgreSQL 10.0.1`.

### DbContext Configuration

```csharp
public class AppDbContext : DbContext
{
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>(entity =>
        {
            entity.ToTable("products");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(200);
            entity.Property(e => e.Price).HasColumnName("price").HasColumnType("numeric(10,2)");
            entity.Property(e => e.Category).HasColumnName("category").HasMaxLength(100);
            entity.Property(e => e.CreatedAt).HasColumnName("created_at")
                .HasDefaultValueSql("NOW()");
            entity.Property(e => e.Metadata).HasColumnName("metadata")
                .HasColumnType("jsonb");

            entity.HasIndex(e => e.Category);
            entity.HasIndex(e => e.CreatedAt);
        });
    }
}
```

Register in DI:

```csharp
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Postgres")));
```

### Snake Case Naming Convention

PostgreSQL conventionally uses snake_case for identifiers. Rather than manually mapping every column, use the Npgsql naming convention:

```bash
dotnet add package EFCore.NamingConventions
```

```csharp
options.UseNpgsql(connectionString)
       .UseSnakeCaseNamingConvention();
```

Now your `Product.CreatedAt` property automatically maps to the `created_at` column, and your `OrderItem` entity maps to the `order_item` table.

### EF Core Migrations

Generate a migration:

```bash
dotnet ef migrations add InitialCreate --project src/MyApp --startup-project src/MyApp
```

Apply to your development database:

```bash
dotnet ef database update --project src/MyApp --startup-project src/MyApp
```

**Production best practice:** Never run `dotnet ef database update` against a production database. Instead, generate SQL scripts and review them before execution:

```bash
dotnet ef migrations script --idempotent --output migration.sql
```

Then apply the reviewed script:

```bash
psql -U myappuser -d myappdb -f migration.sql
```

### PostgreSQL-Specific EF Core Features

**JSONB columns:**

```csharp
entity.Property(e => e.Metadata)
    .HasColumnType("jsonb");

// Query into JSONB
var results = await context.Products
    .Where(p => EF.Functions.JsonContains(p.Metadata, new { category = "electronics" }))
    .ToListAsync();
```

**Array columns:**

```csharp
entity.Property(e => e.Tags)
    .HasColumnType("text[]");

// Query array columns
var results = await context.Products
    .Where(p => p.Tags.Contains("sale"))
    .ToListAsync();
```

**Full-text search:**

```csharp
var results = await context.Products
    .Where(p => EF.Functions.ToTsVector("english", p.Name + " " + p.Description)
        .Matches(EF.Functions.ToTsQuery("english", "wireless & headphones")))
    .ToListAsync();
```

**Index foreign keys manually.** Unlike SQL Server, PostgreSQL does not automatically create indexes on foreign key columns. Always add them:

```csharp
entity.HasIndex(e => e.CustomerId);
```

### Mixing EF Core and Dapper

A common and effective pattern is to use EF Core for writes (migrations, change tracking, validation) and Dapper for reads (complex reports, dashboards, performance-critical queries). You can get the underlying `NpgsqlConnection` from your DbContext:

```csharp
public class ReportService(AppDbContext context)
{
    public async Task<IEnumerable<SalesSummary>> GetSalesSummaryAsync()
    {
        var conn = context.Database.GetDbConnection();
        return await conn.QueryAsync<SalesSummary>(
            """
            SELECT category,
                   COUNT(*) AS order_count,
                   SUM(total) AS revenue,
                   AVG(total) AS avg_order_value
            FROM orders o
            JOIN products p ON p.id = o.product_id
            WHERE o.created_at >= @Since
            GROUP BY category
            ORDER BY revenue DESC
            """,
            new { Since = DateTime.UtcNow.AddMonths(-3) });
    }
}
```

## Part 8: Transactions and Concurrency

### PostgreSQL MVCC

PostgreSQL uses Multiversion Concurrency Control. Every transaction sees a consistent snapshot of the database as of the transaction start (or statement start, depending on isolation level). Readers never block writers and writers never block readers. This is fundamentally different from SQL Server's default locking behavior.

### There Is No NOLOCK in PostgreSQL

If you are coming from SQL Server, you may be accustomed to using `WITH (NOLOCK)` or `SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED` to avoid blocking on reads. **PostgreSQL does not have this concept and does not need it.** Because of MVCC, a SELECT in PostgreSQL never blocks on a concurrent INSERT, UPDATE, or DELETE, and vice versa. If you set `READ UNCOMMITTED` in PostgreSQL, it silently upgrades to `READ COMMITTED`.

**Should you use NOLOCK equivalents in development?** No. PostgreSQL's `READ COMMITTED` default is already non-blocking for reads. There is nothing to gain and nothing to simulate.

**Should you use NOLOCK equivalents in production?** The question is moot. PostgreSQL does not support dirty reads, and its architecture does not need them. Your queries will not block on writes.

### Isolation Levels

PostgreSQL supports four isolation levels, though two of them are effectively identical:

**READ UNCOMMITTED:** Treated as READ COMMITTED. Dirty reads are never possible.

**READ COMMITTED (default):** Each statement within a transaction sees its own snapshot. If another transaction commits between your two statements, the second statement will see the committed changes.

**REPEATABLE READ:** The transaction sees a consistent snapshot as of the start of the transaction. Any changes committed by other transactions after the start are invisible. If a conflicting update occurs, PostgreSQL will abort your transaction with a serialization error, and you must retry.

**SERIALIZABLE:** The strictest level. PostgreSQL guarantees that the result is the same as if transactions had executed one at a time. Uses Serializable Snapshot Isolation (SSI). Serialization failures are possible and must be handled with retry logic.

### Transactions in Npgsql

```csharp
await using var conn = await dataSource.OpenConnectionAsync();
await using var tx = await conn.BeginTransactionAsync(IsolationLevel.RepeatableRead);

try
{
    // All commands within this transaction see the same snapshot
    await using var cmd1 = new NpgsqlCommand("UPDATE accounts SET balance = balance - 100 WHERE id = 1", conn, tx);
    await cmd1.ExecuteNonQueryAsync();

    await using var cmd2 = new NpgsqlCommand("UPDATE accounts SET balance = balance + 100 WHERE id = 2", conn, tx);
    await cmd2.ExecuteNonQueryAsync();

    await tx.CommitAsync();
}
catch (NpgsqlException ex) when (ex.SqlState == "40001") // Serialization failure
{
    await tx.RollbackAsync();
    // Retry the transaction
}
catch
{
    await tx.RollbackAsync();
    throw;
}
```

### Transactions in EF Core

```csharp
await using var tx = await context.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead);

try
{
    context.Orders.Add(new Order { CustomerId = 42, Total = 99.99m });
    await context.SaveChangesAsync();

    context.Inventory.Where(i => i.ProductId == 7)
        .ExecuteUpdate(s => s.SetProperty(i => i.Quantity, i => i.Quantity - 1));
    await context.SaveChangesAsync();

    await tx.CommitAsync();
}
catch (DbUpdateException ex) when (ex.InnerException is NpgsqlException { SqlState: "40001" })
{
    await tx.RollbackAsync();
    // Retry
}
```

### Advisory Locks

PostgreSQL provides advisory locks — application-level locks that are managed by the database but not tied to any particular table or row. These are useful for coordinating distributed work:

```csharp
// Acquire an advisory lock (blocks until available)
await conn.ExecuteAsync("SELECT pg_advisory_lock(@LockId)", new { LockId = 12345L });

try
{
    // Do exclusive work
}
finally
{
    await conn.ExecuteAsync("SELECT pg_advisory_unlock(@LockId)", new { LockId = 12345L });
}

// Non-blocking alternative
var acquired = await conn.ExecuteScalarAsync<bool>(
    "SELECT pg_try_advisory_lock(@LockId)", new { LockId = 12345L });
if (acquired)
{
    try { /* work */ }
    finally { await conn.ExecuteAsync("SELECT pg_advisory_unlock(@LockId)", new { LockId = 12345L }); }
}
```

### Optimistic Concurrency

For web applications, optimistic concurrency is usually preferable to pessimistic locking. Add a `version` column (or use `xmin`):

```csharp
// Using a version column
var affected = await conn.ExecuteAsync(
    """
    UPDATE products
    SET name = @Name, price = @Price, version = version + 1
    WHERE id = @Id AND version = @ExpectedVersion
    """,
    new { Name = "Updated", Price = 39.99m, Id = 1, ExpectedVersion = 3 });

if (affected == 0)
    throw new DbUpdateConcurrencyException("The record was modified by another user.");
```

EF Core supports this natively:

```csharp
entity.Property(e => e.Version).IsRowVersion();
// Or use PostgreSQL's xmin system column:
entity.UseXminAsConcurrencyToken();
```

## Part 9: Networking and Session Management

### Connection Pooling

Npgsql has built-in connection pooling enabled by default. Each unique connection string gets its own pool. Key parameters:

- `Maximum Pool Size=100` — The maximum number of connections in the pool.
- `Minimum Pool Size=0` — Connections below this threshold are maintained even when idle.
- `Connection Idle Lifetime=300` — Seconds before an idle connection above the minimum is closed.
- `Connection Pruning Interval=10` — How often the pool checks for idle connections to close.

**Best practice:** Create one `NpgsqlDataSource` per connection string and register it as a singleton. Do not create data sources per-request.

### PgBouncer

For high-concurrency applications (thousands of concurrent connections), consider putting PgBouncer in front of PostgreSQL. PgBouncer is a lightweight connection pooler that multiplexes many client connections onto a smaller number of PostgreSQL server connections.

```ini
# pgbouncer.ini
[databases]
myappdb = host=127.0.0.1 port=5432 dbname=myappdb

[pgbouncer]
listen_addr = 0.0.0.0
listen_port = 6432
auth_type = scram-sha-256
auth_file = /etc/pgbouncer/userlist.txt
pool_mode = transaction
max_client_conn = 10000
default_pool_size = 25
```

When using PgBouncer in transaction mode, add these to your Npgsql connection string:

```
No Reset On Close=true;
```

This disables Npgsql's connection reset logic (`DISCARD ALL`), which does not work correctly in PgBouncer's transaction mode.

### SSL/TLS Configuration

Always use SSL in production:

```
Host=myserver;SSL Mode=VerifyFull;Trust Server Certificate=false;Root Certificate=/path/to/ca.crt
```

To generate self-signed certificates for development:

```bash
# Generate CA
openssl req -new -x509 -days 365 -nodes -out ca.crt -keyout ca.key -subj "/CN=PostgreSQL-CA"

# Generate server certificate
openssl req -new -nodes -out server.csr -keyout server.key -subj "/CN=localhost"
openssl x509 -req -in server.csr -CA ca.crt -CAkey ca.key -CAcreateserial -out server.crt -days 365

# Copy to PostgreSQL data directory
cp server.crt server.key /var/lib/postgresql/data/
chmod 600 /var/lib/postgresql/data/server.key
chown postgres:postgres /var/lib/postgresql/data/server.*
```

In `postgresql.conf`:

```
ssl = on
ssl_cert_file = 'server.crt'
ssl_key_file = 'server.key'
ssl_ca_file = 'ca.crt'
```

### Session Variables and Settings

PostgreSQL supports per-session settings that you can use for row-level security, audit logging, or passing context:

```csharp
// Set a session variable
await conn.ExecuteAsync("SET app.current_user_id = @UserId", new { UserId = "user-42" });

// Use it in a policy or trigger
// CREATE POLICY user_isolation ON orders
//   USING (user_id = current_setting('app.current_user_id')::int);
```

## Part 10: Debugging and Profiling

### Logging All Queries

For development, enable comprehensive query logging in `postgresql.conf`:

```
log_statement = 'all'
log_min_duration_statement = 0   # Log all queries with their durations
```

For production, log only slow queries:

```
log_min_duration_statement = 500   # Only queries taking > 500ms
```

### pg_stat_statements

This is the single most useful extension for production performance analysis. Enable it:

```sql
CREATE EXTENSION IF NOT EXISTS pg_stat_statements;
```

In `postgresql.conf`:

```
shared_preload_libraries = 'pg_stat_statements'
pg_stat_statements.track = 'all'
```

Then query it to find your slowest queries:

```sql
SELECT query,
       calls,
       round(mean_exec_time::numeric, 2) AS avg_ms,
       round(total_exec_time::numeric, 2) AS total_ms,
       rows,
       round((100 * total_exec_time / sum(total_exec_time) OVER ())::numeric, 2) AS pct
FROM pg_stat_statements
ORDER BY total_exec_time DESC
LIMIT 20;
```

### EXPLAIN with Buffers

For detailed I/O analysis:

```sql
EXPLAIN (ANALYZE, BUFFERS, FORMAT TEXT)
SELECT * FROM orders WHERE customer_id = 42;
```

The `BUFFERS` option shows how many pages were read from shared buffers (cache hits) versus disk. A low hit rate suggests you need more `shared_buffers` or better indexes.

### Npgsql Logging

Configure Npgsql logging in your ASP.NET Core application:

```csharp
builder.Services.AddNpgsqlDataSource(
    connectionString,
    dataSourceBuilder =>
    {
        dataSourceBuilder.EnableParameterLogging();  // Dev only!
    });

// In appsettings.Development.json:
{
  "Logging": {
    "LogLevel": {
      "Npgsql": "Debug",
      "Npgsql.Command": "Debug"
    }
  }
}
```

This will log every SQL command with parameters and execution times.

### EF Core Query Logging

```csharp
options.UseNpgsql(connectionString)
       .EnableSensitiveDataLogging()    // Logs parameter values (dev only!)
       .EnableDetailedErrors()          // More detailed error messages
       .LogTo(Console.WriteLine, LogLevel.Information);
```

## Part 11: Essential SQL for .NET Developers

### UPSERT (INSERT ... ON CONFLICT)

PostgreSQL's UPSERT is one of its most useful features:

```sql
INSERT INTO products (sku, name, price)
VALUES ('SKU-001', 'Widget', 29.99)
ON CONFLICT (sku) DO UPDATE
SET name = EXCLUDED.name,
    price = EXCLUDED.price,
    updated_at = NOW();
```

With Dapper:

```csharp
await conn.ExecuteAsync(
    """
    INSERT INTO products (sku, name, price)
    VALUES (@Sku, @Name, @Price)
    ON CONFLICT (sku) DO UPDATE
    SET name = EXCLUDED.name, price = EXCLUDED.price, updated_at = NOW()
    """,
    new { Sku = "SKU-001", Name = "Widget", Price = 29.99m });
```

### RETURNING Clause

PostgreSQL can return data from INSERT, UPDATE, and DELETE statements. PostgreSQL 18 adds OLD and NEW support for RETURNING:

```sql
-- Get the generated ID after insert
INSERT INTO products (name, price) VALUES ('Widget', 29.99) RETURNING id;

-- Get the old and new values after update (PostgreSQL 18)
UPDATE products SET price = 39.99 WHERE id = 1
RETURNING OLD.price AS old_price, NEW.price AS new_price;

-- Get deleted rows
DELETE FROM products WHERE discontinued = true RETURNING *;
```

### Common Table Expressions (CTEs)

CTEs make complex queries readable and composable:

```sql
WITH monthly_sales AS (
    SELECT date_trunc('month', created_at) AS month,
           category,
           SUM(total) AS revenue
    FROM orders
    WHERE created_at >= '2026-01-01'
    GROUP BY 1, 2
),
ranked AS (
    SELECT *,
           RANK() OVER (PARTITION BY month ORDER BY revenue DESC) AS rank
    FROM monthly_sales
)
SELECT * FROM ranked WHERE rank <= 3;
```

### Window Functions

PostgreSQL has excellent window function support:

```sql
SELECT id, customer_id, total,
       SUM(total) OVER (PARTITION BY customer_id ORDER BY created_at) AS running_total,
       LAG(total) OVER (PARTITION BY customer_id ORDER BY created_at) AS prev_order_total,
       ROW_NUMBER() OVER (PARTITION BY customer_id ORDER BY total DESC) AS rank_by_total
FROM orders;
```

### Full-Text Search

PostgreSQL has built-in full-text search that works well for many applications without needing Elasticsearch or Solr:

```sql
-- Create a tsvector column and index
ALTER TABLE products ADD COLUMN search_vector tsvector
    GENERATED ALWAYS AS (
        setweight(to_tsvector('english', coalesce(name, '')), 'A') ||
        setweight(to_tsvector('english', coalesce(description, '')), 'B')
    ) STORED;

CREATE INDEX idx_products_search ON products USING GIN (search_vector);

-- Search
SELECT id, name, ts_rank(search_vector, query) AS rank
FROM products, to_tsquery('english', 'wireless & headphones') AS query
WHERE search_vector @@ query
ORDER BY rank DESC;
```

### JSONB Operations

```sql
-- Store JSON
INSERT INTO events (data) VALUES ('{"type": "click", "page": "/home", "user_id": 42}');

-- Query JSON fields
SELECT data->>'type' AS event_type, data->>'page' AS page
FROM events
WHERE data->>'user_id' = '42';

-- Containment query (uses GIN index)
SELECT * FROM events WHERE data @> '{"type": "click"}';

-- Update a JSON field
UPDATE events
SET data = jsonb_set(data, '{processed}', 'true')
WHERE id = 1;

-- Create a GIN index for JSONB
CREATE INDEX idx_events_data ON events USING GIN (data);
```

## Part 12: Free and Open-Source PostgreSQL IDEs on Linux

Every tool listed here is genuinely free (no payment required for any use) and runs on Linux.

### pgAdmin 4

pgAdmin is the official PostgreSQL GUI, maintained by the PostgreSQL Global Development Group and backed by EnterpriseDB. It is released under the PostgreSQL License. pgAdmin 4 is a complete rewrite built with Python and JavaScript that can run as a web application in your browser or as a desktop application.

**Strengths:** Comprehensive PostgreSQL administration tools including server monitoring, query tool with explain plan visualization, backup and restore wizards, user and role management, tablespace management, and full schema browsing. It understands PostgreSQL-specific features better than any other tool because it is purpose-built for PostgreSQL.

**Weaknesses:** The interface can feel cluttered and slow compared to more modern tools. The web-based architecture introduces noticeable latency. It only supports PostgreSQL, so if you also work with MySQL, SQLite, or SQL Server, you need a separate tool.

**Install on Linux:**

```bash
# Fedora
sudo dnf install pgadmin4

# Ubuntu/Debian (from official repo)
curl -fsS https://www.pgadmin.org/static/packages_pgadmin_org.pub | sudo gpg --dearmor -o /usr/share/keyrings/packages-pgadmin-org.gpg
echo "deb [signed-by=/usr/share/keyrings/packages-pgadmin-org.gpg] https://ftp.postgresql.org/pub/pgadmin/pgadmin4/apt/$(lsb_release -cs) pgadmin4 main" | sudo tee /etc/apt/sources.list.d/pgadmin4.list
sudo apt update
sudo apt install pgadmin4-desktop
```

Or run it in Docker:

```bash
docker run -d -p 5050:80 \
  -e PGADMIN_DEFAULT_EMAIL=admin@example.com \
  -e PGADMIN_DEFAULT_PASSWORD=admin \
  dpage/pgadmin4
```

### DBeaver Community Edition

DBeaver Community is a free, open-source universal database tool released under the Apache 2.0 License. It supports dozens of databases including PostgreSQL, MySQL, SQLite, SQL Server, Oracle, and many more through JDBC drivers. It is built on the Eclipse platform using Java.

**Strengths:** Excellent multi-database support. Full-featured SQL editor with autocomplete, syntax highlighting, and query execution plans. Data browser with inline editing. ER diagrams and schema visualization. Active development with frequent releases. Plugin architecture for extensibility.

**Weaknesses:** Being Java-based, it can feel sluggish compared to native applications, especially on lower-powered machines. The interface is feature-dense and can be overwhelming for new users. Startup time is noticeable.

**Install on Linux:**

```bash
# Flatpak (universal)
flatpak install flathub io.dbeaver.DBeaverCommunity

# Snap
sudo snap install dbeaver-ce

# Fedora
sudo dnf install dbeaver-ce

# Or download the .deb/.rpm from dbeaver.io
```

### Beekeeper Studio (Community Edition)

Beekeeper Studio is a modern, cross-platform SQL editor released under the GPLv3 License. It supports PostgreSQL, MySQL, SQLite, SQL Server, CockroachDB, and others. It is built with Electron but is notably faster and more responsive than many Electron applications.

**Strengths:** Clean, intuitive interface that feels modern and native. Fast startup. SQL editor with autocomplete, query formatting, and tabbed results. Privacy-focused with no telemetry or tracking. All core features are free.

**Weaknesses:** The free Community Edition lacks some features available in the paid Ultimate edition, such as full import/export, backup and restore, and support for additional databases like Oracle. Being Electron-based, it uses more memory than a truly native application.

**Install on Linux:**

```bash
# Flatpak
flatpak install flathub io.beekeeperstudio.Studio

# Snap
sudo snap install beekeeper-studio

# AppImage available from beekeeperstudio.io
```

### DbGate

DbGate is a free, open-source database manager released under the MIT License that works as both a desktop application and a web application. It supports PostgreSQL, MySQL, SQL Server, SQLite, MongoDB, CockroachDB, and others.

**Strengths:** Lightweight and fast. Works in the browser (self-hosted) as well as on the desktop. Supports both SQL and NoSQL databases. Clean interface. Good cross-platform support. Active development.

**Weaknesses:** Less mature than DBeaver or pgAdmin. Fewer advanced features for PostgreSQL-specific administration.

**Install on Linux:**

```bash
# Snap
sudo snap install dbgate

# Or download AppImage from dbgate.org
```

### Adminer

Adminer is a single-PHP-file database management tool that supports PostgreSQL, MySQL, SQLite, MS SQL, and Oracle. It is released under the Apache 2.0 or GPLv2 License. You deploy it by dropping a single PHP file onto any web server.

**Strengths:** Incredibly simple deployment. Tiny footprint. Supports multiple databases. Clean, functional interface. Community plugins for theming and additional database support.

**Weaknesses:** Requires a PHP-capable web server. The interface is functional but not modern. Fewer features than desktop applications.

**Install on Linux:**

```bash
# Just download the PHP file
wget https://www.adminer.org/latest.php -O adminer.php
# Serve with PHP's built-in server
php -S localhost:8080 adminer.php
```

### pgconsole

pgconsole is a newer open-source PostgreSQL-specific editor built as a single Go binary. It requires no separate database for configuration — everything lives in a `pgconsole.toml` file, making it GitOps-friendly. It features a SQL editor with real-time autocomplete powered by a full PostgreSQL parser, fine-grained access control, audit logging, and a built-in AI assistant that supports OpenAI, Anthropic Claude, and Google Gemini.

**Strengths:** Single binary deployment. PostgreSQL-specific with deep parser integration. Team-oriented with access controls. Modern web UI. No dependencies.

**Weaknesses:** PostgreSQL only. Relatively new project with a smaller community.

### Azure Data Studio

Azure Data Studio is a free, open-source (source-available under MIT license) data management tool from Microsoft. While its name suggests Azure-only, it works with any PostgreSQL server through the PostgreSQL extension. It is built on VS Code's core.

**Strengths:** Familiar VS Code interface. Notebook support for mixing SQL and documentation. Extension ecosystem. Good integrated terminal.

**Weaknesses:** The PostgreSQL extension is less polished than the SQL Server support. Can be resource-heavy.

### VS Code with PostgreSQL Extension

Microsoft has released a dedicated PostgreSQL extension for VS Code that provides IntelliSense, schema visualization, a database explorer, and query history. Combined with VS Code's existing ecosystem, this turns your code editor into a capable database tool without switching applications.

**Install:**

```bash
code --install-extension ms-ossdata.vscode-postgresql
```

### Comparison Summary

For a .NET developer on Linux, here is how to think about the choices. If you work exclusively with PostgreSQL and need comprehensive admin tools, use **pgAdmin 4**. If you work with multiple databases and want a powerful all-in-one tool, use **DBeaver Community**. If you value a clean, modern interface and primarily write queries, use **Beekeeper Studio**. If you want a lightweight web-based option, use **DbGate** or **Adminer**. If you live in VS Code, install the **PostgreSQL extension** and stay in your editor.

Many experienced developers use `psql` for quick queries and admin tasks, a GUI tool like DBeaver or Beekeeper for data exploration and complex query development, and VS Code with the PostgreSQL extension for queries that live alongside their application code. There is no reason to limit yourself to one tool.

## Part 13: Best Practices Summary

### Schema Design

- Use snake_case for all identifiers (tables, columns, indexes). PostgreSQL folds unquoted identifiers to lowercase, so this avoids quoting headaches.
- Use `bigint` or `uuid` (preferably `uuidv7()` on PostgreSQL 18) for primary keys. Avoid `serial` in new projects; use `GENERATED ALWAYS AS IDENTITY` inst