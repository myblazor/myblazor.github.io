---
title: "PostgreSQL, Npgsql, and Open-Source IDEs: The Definitive Guide for .NET Developers on Linux"
date: 2026-03-28
author: myblazor-team
summary: A comprehensive, leave-no-stone-unturned guide to PostgreSQL 17 and 18, Npgsql with Dapper and EF Core, terminal workflows, configuration, transactions, networking, sessions, debugging, Docker/Podman setup, and every free open-source IDE available — all from the perspective of a .NET C# ASP.NET web developer working on Linux.
tags:
  - postgresql
  - npgsql
  - dotnet
  - dapper
  - efcore
  - linux
  - database
  - tutorial
---

## Introduction

If you are a .NET developer who has spent most of your career working with SQL Server on Windows, PostgreSQL can feel like a different world. The terminology is different, the tooling is different, the configuration is different, and even the philosophical approach to certain problems diverges significantly from what you are used to. This guide is written to bridge that gap completely.

We are going to cover everything. Not some things. Everything. From installing PostgreSQL on bare metal Linux, a VPS, or a Docker/Podman container, to configuring it for development and production, to writing queries in the terminal, to connecting from .NET using Npgsql with both Dapper and Entity Framework Core, to understanding transactions, isolation levels, locking, connection pooling, session management, networking, debugging, and monitoring. We will also survey every free and open-source IDE and GUI tool available on Linux for working with PostgreSQL.

This article assumes you are running Linux (Fedora, Ubuntu, Debian, Arch, or a similar distribution). It assumes you know C# and have worked with ASP.NET. It does not assume any prior PostgreSQL experience.

Let us begin.

## Part 1: What Is PostgreSQL and Why Should You Care?

PostgreSQL is a free, open-source, object-relational database management system. It has been under active development since 1986, originating from the POSTGRES project at the University of California, Berkeley. The "SQL" was appended to the name in 1996 when SQL language support was added, and the project has been community-driven ever since.

PostgreSQL is not owned by any corporation. There is no "PostgreSQL Inc." that controls the project. It is developed by a global community of contributors under the PostgreSQL Global Development Group. The license is the PostgreSQL License, which is a permissive open-source license similar to BSD and MIT. You can use PostgreSQL for any purpose, including commercial, without paying anyone anything, ever. There are no "community editions" versus "enterprise editions." There is one PostgreSQL, and it is free.

As of early 2026, PostgreSQL has surpassed MySQL as the most widely used database among developers, with roughly 55% usage in developer surveys. Every major cloud provider offers managed PostgreSQL services: Amazon RDS and Aurora PostgreSQL, Azure Database for PostgreSQL, Google Cloud SQL for PostgreSQL, and many others. But you do not need to use any cloud service. PostgreSQL runs perfectly well on a single Linux machine, a Raspberry Pi, or a $5/month VPS.

For .NET developers specifically, PostgreSQL is compelling because the .NET ecosystem has first-class support for it through Npgsql, the open-source ADO.NET data provider. Npgsql consistently ranks among the top performers on the TechEmpower Web Framework Benchmarks. Entity Framework Core has an official PostgreSQL provider maintained by the Npgsql team. Dapper works flawlessly with Npgsql. There is no technical reason to avoid PostgreSQL in a .NET application.

### PostgreSQL vs. SQL Server: Key Philosophical Differences

Before we dive into specifics, you need to understand a few philosophical differences between PostgreSQL and SQL Server:

PostgreSQL uses Multi-Version Concurrency Control (MVCC) as its fundamental concurrency mechanism. Every transaction sees a snapshot of the data as it existed at the start of the transaction. Readers never block writers, and writers never block readers. This is fundamentally different from SQL Server's default behavior, where readers acquire shared locks that can block writers. SQL Server added MVCC-like behavior later through Read Committed Snapshot Isolation (RCSI) and Snapshot Isolation, but these are opt-in features. In PostgreSQL, MVCC is the default and only model.

PostgreSQL does not have a concept equivalent to SQL Server's `NOLOCK` hint, and you should not miss it. The entire `NOLOCK` pattern exists in SQL Server because its default isolation level (Read Committed with locking) causes readers to block writers. Since PostgreSQL uses MVCC by default, readers never block writers, so the problem `NOLOCK` solves simply does not exist. We will discuss this in much more detail in the transactions section.

PostgreSQL is case-sensitive for identifiers by default, but it lowercases unquoted identifiers. If you write `CREATE TABLE MyTable`, PostgreSQL stores it as `mytable`. If you want mixed-case identifiers, you must double-quote them: `CREATE TABLE "MyTable"`. The strong convention in the PostgreSQL world is to use `snake_case` for everything: table names, column names, function names. Embrace this convention.

PostgreSQL uses schemas differently than SQL Server. In SQL Server, `dbo` is the default schema and many teams barely think about schemas. In PostgreSQL, `public` is the default schema, but the schema system is powerful and you should use it to organize your database objects.

## Part 2: Installing PostgreSQL on Linux

### Bare Metal / VPS Installation

On Fedora or RHEL-based systems:

```bash
# Install PostgreSQL 18 (latest stable as of March 2026)
sudo dnf install postgresql18-server postgresql18

# Initialize the database cluster
sudo postgresql-18-setup --initdb

# Start and enable the service
sudo systemctl start postgresql-18
sudo systemctl enable postgresql-18
```

On Ubuntu or Debian-based systems:

```bash
# Add the official PostgreSQL APT repository
sudo sh -c 'echo "deb https://apt.postgresql.org/pub/repos/apt $(lsb_release -cs)-pgdg main" > /etc/apt/sources.list.d/pgdg.list'
wget --quiet -O - https://www.postgresql.org/media/keys/ACCC4CF8.asc | sudo apt-key add -
sudo apt-get update

# Install PostgreSQL 18
sudo apt-get install postgresql-18

# The service starts automatically on Debian/Ubuntu
sudo systemctl status postgresql
```

On Arch Linux:

```bash
sudo pacman -S postgresql

# Initialize the data directory
sudo -u postgres initdb -D /var/lib/postgres/data

# Start and enable
sudo systemctl start postgresql
sudo systemctl enable postgresql
```

After installation, PostgreSQL creates a system user called `postgres`. This user is the default superuser. To connect for the first time:

```bash
# Switch to the postgres user
sudo -u postgres psql

# You are now in the psql shell as the superuser
# Create a database and user for your application
CREATE USER myapp WITH PASSWORD 'my-secure-password';
CREATE DATABASE myappdb OWNER myapp;

# Grant connect privilege
GRANT CONNECT ON DATABASE myappdb TO myapp;

# Exit
\q
```

### Docker Installation

Docker is the quickest way to get PostgreSQL running for development:

```bash
# Pull the official PostgreSQL 18 image
docker pull postgres:18

# Run a container
docker run -d \
  --name pg-dev \
  -e POSTGRES_USER=myapp \
  -e POSTGRES_PASSWORD=my-secure-password \
  -e POSTGRES_DB=myappdb \
  -p 5432:5432 \
  -v pgdata:/var/lib/postgresql/data \
  postgres:18

# Connect using psql from the host
psql -h localhost -U myapp -d myappdb

# Or connect from inside the container
docker exec -it pg-dev psql -U myapp -d myappdb
```

The `-v pgdata:/var/lib/postgresql/data` flag creates a named Docker volume so your data persists across container restarts and removals. Without it, you lose all data when the container is removed.

### Podman Installation

Podman is a daemonless container engine that is often preferred on Fedora and RHEL systems. It is a drop-in replacement for Docker:

```bash
# Pull and run (identical syntax to Docker)
podman run -d \
  --name pg-dev \
  -e POSTGRES_USER=myapp \
  -e POSTGRES_PASSWORD=my-secure-password \
  -e POSTGRES_DB=myappdb \
  -p 5432:5432 \
  -v pgdata:/var/lib/postgresql/data \
  docker.io/library/postgres:18

# Connect
podman exec -it pg-dev psql -U myapp -d myappdb
```

If you want to run PostgreSQL as a rootless Podman container that starts on boot:

```bash
# Generate a systemd user service
podman generate systemd --name pg-dev --files --new
mkdir -p ~/.config/systemd/user/
mv container-pg-dev.service ~/.config/systemd/user/
systemctl --user daemon-reload
systemctl --user enable container-pg-dev.service
systemctl --user start container-pg-dev.service

# Enable lingering so it starts on boot even without login
loginctl enable-linger $USER
```

### Docker Compose for Development

For a more complete development setup, use a `docker-compose.yml`:

```yaml
services:
  db:
    image: postgres:18
    restart: unless-stopped
    environment:
      POSTGRES_USER: myapp
      POSTGRES_PASSWORD: my-secure-password
      POSTGRES_DB: myappdb
    ports:
      - "5432:5432"
    volumes:
      - pgdata:/var/lib/postgresql/data
      - ./init.sql:/docker-entrypoint-initdb.d/init.sql
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U myapp -d myappdb"]
      interval: 5s
      timeout: 5s
      retries: 5

volumes:
  pgdata:
```

Any `.sql` or `.sh` files placed in `/docker-entrypoint-initdb.d/` inside the container are executed when the database is initialized for the first time.

## Part 3: Configuring PostgreSQL

PostgreSQL's configuration lives in two primary files: `postgresql.conf` and `pg_hba.conf`. Understanding both is essential.

### Finding the Configuration Files

```sql
-- Inside psql, find the config file locations
SHOW config_file;
-- Example: /var/lib/postgresql/data/postgresql.conf

SHOW hba_file;
-- Example: /var/lib/postgresql/data/pg_hba.conf

SHOW data_directory;
-- Example: /var/lib/postgresql/data
```

On a Docker container, these are at `/var/lib/postgresql/data/`. On a bare-metal Fedora install, they are typically at `/var/lib/pgsql/18/data/`. On Ubuntu, they are at `/etc/postgresql/18/main/`.

### postgresql.conf: The Main Configuration File

This file controls everything about how PostgreSQL runs. Here are the settings you need to understand:

**Connection Settings:**

```ini
# Listen on all interfaces (default is localhost only)
listen_addresses = '*'          # For development; restrict in production

# Maximum concurrent connections
max_connections = 100           # Default is 100; tune based on workload

# Port (default 5432)
port = 5432
```

**Memory Settings:**

```ini
# Shared memory for caching data pages
# Rule of thumb: 25% of total system RAM
shared_buffers = 2GB            # Default is 128MB — far too low

# Memory for sorting, hashing, and other operations per query
work_mem = 64MB                 # Default 4MB; increase for complex queries

# Memory for maintenance operations (VACUUM, CREATE INDEX)
maintenance_work_mem = 512MB    # Default 64MB

# OS page cache hint
effective_cache_size = 6GB      # 50-75% of total RAM; helps query planner
```

**Write-Ahead Log (WAL) Settings:**

```ini
# WAL level (minimal, replica, or logical)
wal_level = replica             # Needed for replication and point-in-time recovery

# Checkpoint settings
checkpoint_completion_target = 0.9
max_wal_size = 2GB
min_wal_size = 80MB
```

**Query Planner Settings:**

```ini
# Cost estimates for planner decisions
random_page_cost = 1.1          # Lower if using SSDs (default 4.0 assumes HDDs)
effective_io_concurrency = 200  # Higher for SSDs; default 1

# PostgreSQL 18: Asynchronous I/O method
io_method = worker              # 'worker' (all platforms), 'io_uring' (Linux), 'sync' (legacy)
```

**Logging:**

```ini
# Log destination
logging_collector = on
log_directory = 'pg_log'
log_filename = 'postgresql-%Y-%m-%d_%H%M%S.log'

# What to log
log_min_duration_statement = 500    # Log queries taking > 500ms
log_statement = 'none'              # 'none', 'ddl', 'mod', or 'all'
log_line_prefix = '%t [%p] %u@%d '  # Timestamp, PID, user@database

# Log slow queries with their execution plans
auto_explain.log_min_duration = 1000  # Requires loading auto_explain extension
```

**Development vs. Production:**

For development, you might use more aggressive logging:

```ini
log_statement = 'all'
log_min_duration_statement = 0
log_connections = on
log_disconnections = on
```

For production, you want to log only what matters:

```ini
log_statement = 'ddl'
log_min_duration_statement = 1000
log_connections = off
log_disconnections = off
```

### pg_hba.conf: Client Authentication Configuration

This file controls who can connect to your database and how they authenticate. Each line specifies a connection type, database, user, address, and authentication method.

```
# TYPE  DATABASE    USER        ADDRESS         METHOD

# Local connections (Unix socket)
local   all         postgres                    peer
local   all         all                         scram-sha-256

# IPv4 local connections
host    all         all         127.0.0.1/32    scram-sha-256

# IPv4 remote connections (restrict in production)
host    all         all         0.0.0.0/0       scram-sha-256

# IPv6 local connections
host    all         all         ::1/128         scram-sha-256
```

Authentication methods you should know:

`peer` uses the operating system username. If you are logged in as the Linux user `postgres`, you can connect to the `postgres` database role without a password. This only works for local (Unix socket) connections.

`scram-sha-256` is the modern password authentication method. It is significantly more secure than the older `md5` method. PostgreSQL 18 has deprecated MD5 authentication, and it will be removed in a future release. Always use SCRAM.

`reject` denies the connection. Useful for explicitly blocking certain combinations.

`cert` requires a TLS client certificate. Used in high-security environments.

After editing `pg_hba.conf`, you must reload the configuration:

```bash
sudo systemctl reload postgresql-18
# Or from inside psql:
SELECT pg_reload_conf();
```

### Configuration for Docker Containers

When running PostgreSQL in Docker, you can pass configuration parameters at startup:

```bash
docker run -d \
  --name pg-dev \
  -e POSTGRES_PASSWORD=secret \
  -p 5432:5432 \
  postgres:18 \
  -c shared_buffers=512MB \
  -c work_mem=32MB \
  -c max_connections=200
```

Or mount a custom configuration file:

```bash
docker run -d \
  --name pg-dev \
  -e POSTGRES_PASSWORD=secret \
  -p 5432:5432 \
  -v ./my-postgresql.conf:/etc/postgresql/postgresql.conf \
  postgres:18 \
  -c config_file=/etc/postgresql/postgresql.conf
```

## Part 4: The Terminal — psql and Beyond

### psql: The Standard Client

`psql` is PostgreSQL's interactive terminal. It is the equivalent of `sqlcmd` for SQL Server, but far more capable. Every PostgreSQL developer should be fluent with psql.

**Connecting:**

```bash
# Connect to a local database
psql -U myapp -d myappdb

# Connect to a remote server
psql -h 192.168.1.100 -p 5432 -U myapp -d myappdb

# Using a connection string
psql "host=192.168.1.100 port=5432 dbname=myappdb user=myapp password=secret sslmode=require"

# Using a URI
psql postgresql://myapp:secret@192.168.1.100:5432/myappdb?sslmode=require
```

**Essential Meta-Commands:**

```
\l          List all databases
\c dbname   Connect to a different database
\dt         List tables in current schema
\dt+        List tables with sizes
\d table    Describe a table (columns, types, constraints)
\d+ table   Describe with additional detail (storage, description)
\di         List indexes
\df         List functions
\dv         List views
\dn         List schemas
\du         List roles/users
\dp         List table privileges
\x          Toggle expanded display (vertical output)
\timing     Toggle query timing display
\e          Open query in $EDITOR
\i file.sql Execute SQL from a file
\o file.txt Send output to a file
\q          Quit
```

**Running SQL from the Command Line:**

```bash
# Execute a single command
psql -U myapp -d myappdb -c "SELECT count(*) FROM users;"

# Execute a SQL file
psql -U myapp -d myappdb -f migrations/001-create-tables.sql

# Execute and get CSV output
psql -U myapp -d myappdb -c "COPY (SELECT * FROM users) TO STDOUT WITH CSV HEADER;"

# Pipe SQL from stdin
echo "SELECT now();" | psql -U myapp -d myappdb
```

**Environment Variables:**

You can avoid typing credentials repeatedly by setting environment variables:

```bash
export PGHOST=localhost
export PGPORT=5432
export PGUSER=myapp
export PGPASSWORD=my-secure-password
export PGDATABASE=myappdb

# Now just type:
psql
```

For a more secure approach, use a `.pgpass` file:

```bash
# Create ~/.pgpass with format: hostname:port:database:username:password
echo "localhost:5432:myappdb:myapp:my-secure-password" > ~/.pgpass
chmod 600 ~/.pgpass
```

### pgcli: A Better Terminal Experience

`pgcli` is a drop-in replacement for psql with intelligent autocompletion and syntax highlighting:

```bash
# Install via pip
pip install pgcli

# Or on Fedora
sudo dnf install pgcli

# Or on Ubuntu
sudo apt install pgcli

# Use exactly like psql
pgcli -U myapp -d myappdb
```

pgcli provides real-time autocomplete for table names, column names, SQL keywords, and even suggests JOINs based on foreign key relationships. If you spend any time in the terminal, install pgcli immediately.

## Part 5: PostgreSQL 17 and 18 — What Is New

### PostgreSQL 17 (Released September 26, 2024)

PostgreSQL 17 delivered major performance improvements. The vacuum subsystem received a complete memory management overhaul, reducing memory consumption by up to 20x. This means autovacuum runs more efficiently, keeping your tables healthy with less resource contention. Bulk loading and exporting via the `COPY` command saw up to 2x performance improvements for large rows.

The `JSON_TABLE` function arrived, letting you convert JSON data directly into a relational table representation within SQL:

```sql
SELECT *
FROM JSON_TABLE(
    '[{"name": "Alice", "age": 30}, {"name": "Bob", "age": 25}]'::jsonb,
    '$[*]'
    COLUMNS (
        name TEXT PATH '$.name',
        age INT PATH '$.age'
    )
) AS jt;
```

The `MERGE` statement gained a `RETURNING` clause, and views became updatable via `MERGE`. The `COPY` command added an `ON_ERROR` option that allows imports to continue even when individual rows fail. Logical replication received failover slot synchronization, enabling high-availability setups to maintain replication through primary failovers. Incremental backups landed natively via `pg_basebackup --incremental`, with `pg_combinebackup` for restoration. Direct SSL connections became possible with the `sslnegotiation=direct` client option, saving a roundtrip during connection establishment.

### PostgreSQL 18 (Released September 25, 2025)

PostgreSQL 18 is a landmark release. The headline feature is the Asynchronous I/O (AIO) subsystem, which fundamentally changes how PostgreSQL handles read operations. Instead of issuing synchronous I/O calls and waiting for each to complete, PostgreSQL 18 can issue multiple I/O requests concurrently. Benchmarks demonstrate up to 3x performance improvements for sequential scans, bitmap heap scans, and vacuum operations.

```sql
-- Configure the AIO method
SET io_method = 'worker';     -- Worker-based (all platforms)
SET io_method = 'io_uring';   -- io_uring (Linux only, fastest)
SET io_method = 'sync';       -- Traditional synchronous I/O
```

Native UUIDv7 support arrived via the `uuidv7()` function. UUIDv7 combines global uniqueness with timestamp-based ordering, making it ideal for primary keys because the sequential nature provides excellent B-tree index performance:

```sql
-- Generate a timestamp-ordered UUID
SELECT uuidv7();
-- Result: 01980de8-ad3d-715c-b739-faf2bb1a7aad

-- Extract the embedded timestamp
SELECT uuid_extract_timestamp(uuidv7());

-- Use as a primary key
CREATE TABLE orders (
    id UUID PRIMARY KEY DEFAULT uuidv7(),
    customer_id INT NOT NULL,
    total DECIMAL(10,2) NOT NULL,
    created_at TIMESTAMPTZ DEFAULT now()
);
```

Virtual generated columns became the default. Unlike stored generated columns (which write computed values to disk), virtual columns compute their values on-the-fly during reads:

```sql
CREATE TABLE invoices (
    id SERIAL PRIMARY KEY,
    subtotal DECIMAL(10,2),
    tax_rate DECIMAL(5,4) DEFAULT 0.0875,
    -- Virtual by default: computed at read time, no disk storage
    total DECIMAL(10,2) GENERATED ALWAYS AS (subtotal * (1 + tax_rate))
);
```

The `RETURNING` clause was enhanced with `OLD` and `NEW` references for `INSERT`, `UPDATE`, `DELETE`, and `MERGE`:

```sql
-- See both old and new values in a single UPDATE
UPDATE products
SET price = price * 1.10
WHERE category = 'electronics'
RETURNING name, old.price AS was, new.price AS now;
```

Temporal constraints allow defining non-overlapping constraints on range types, ideal for scheduling and reservation systems:

```sql
CREATE TABLE room_bookings (
    room_id INT,
    booked_during TSTZRANGE,
    guest TEXT,
    PRIMARY KEY (room_id, booked_during WITHOUT OVERLAPS)
);
```

OAuth 2.0 authentication support was added, enabling integration with modern identity providers. MD5 password authentication was deprecated in favor of SCRAM-SHA-256. The `pg_upgrade` utility now preserves planner statistics during major version upgrades, eliminating the performance dip that previously occurred while `ANALYZE` rebuilt statistics. Skip scan lookups on multicolumn B-tree indexes allow queries that omit leading index columns to still benefit from the index.

`EXPLAIN ANALYZE` now automatically includes buffer usage statistics (previously required `BUFFERS` option), and verbose output includes WAL writes, CPU time, and average read times.

## Part 6: Npgsql — The .NET Data Provider

Npgsql is the open-source ADO.NET data provider for PostgreSQL. It is licensed under the PostgreSQL License (permissive, like MIT). The latest major version is Npgsql 10.x, which targets .NET 10.

### Installation

```bash
dotnet add package Npgsql
```

Or in your `Directory.Packages.props` for central package management:

```xml
<PackageVersion Include="Npgsql" Version="10.0.2" />
```

### Basic Usage with NpgsqlDataSource

Modern Npgsql (version 7+) uses `NpgsqlDataSource` as the preferred entry point. It manages connection pooling, configuration, and type mapping:

```csharp
using Npgsql;

var connString = "Host=localhost;Port=5432;Database=myappdb;Username=myapp;Password=secret";
var dataSourceBuilder = new NpgsqlDataSourceBuilder(connString);
await using var dataSource = dataSourceBuilder.Build();

// Get a connection from the pool
await using var conn = await dataSource.OpenConnectionAsync();

// Execute a query
await using var cmd = new NpgsqlCommand("SELECT id, name, email FROM users WHERE active = @active", conn);
cmd.Parameters.AddWithValue("active", true);

await using var reader = await cmd.ExecuteReaderAsync();
while (await reader.ReadAsync())
{
    var id = reader.GetInt32(0);
    var name = reader.GetString(1);
    var email = reader.GetString(2);
    Console.WriteLine($"{id}: {name} ({email})");
}
```

### Connection String Parameters You Should Know

```
Host=localhost           Server hostname or IP
Port=5432                Server port
Database=myappdb         Database name
Username=myapp           Database user
Password=secret          Password
SSL Mode=Prefer          None, Prefer, Require, VerifyCA, VerifyFull
Pooling=true             Enable connection pooling (default: true)
Minimum Pool Size=0      Minimum idle connections
Maximum Pool Size=100    Maximum concurrent connections
Connection Idle Lifetime=300   Seconds before idle connection is closed
Timeout=15               Connection timeout in seconds
Command Timeout=30       Default command timeout in seconds
Include Error Detail=true  Include server error details (dev only)
```

For production, always use SSL:

```
Host=db.example.com;Database=prod;Username=app;Password=secret;SSL Mode=VerifyFull;Trust Server Certificate=false
```

### Npgsql with Dependency Injection in ASP.NET

```csharp
// In Program.cs
builder.Services.AddNpgsqlDataSource(
    builder.Configuration.GetConnectionString("DefaultConnection")!,
    dataSourceBuilder =>
    {
        dataSourceBuilder.UseNodaTime();       // Optional: NodaTime date/time types
        dataSourceBuilder.MapEnum<OrderStatus>("order_status"); // Map PostgreSQL enums
    }
);
```

This registers `NpgsqlDataSource` as a singleton in the DI container. Inject it anywhere:

```csharp
public class UserRepository(NpgsqlDataSource dataSource)
{
    public async Task<User?> GetByIdAsync(int id)
    {
        await using var conn = await dataSource.OpenConnectionAsync();
        await using var cmd = new NpgsqlCommand("SELECT id, name, email FROM users WHERE id = @id", conn);
        cmd.Parameters.AddWithValue("id", id);

        await using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return new User(reader.GetInt32(0), reader.GetString(1), reader.GetString(2));
        }
        return null;
    }
}
```

### Key Npgsql 9.0 and 10.0 Features

Npgsql 9.0 dropped .NET Standard 2.0 support (and thus .NET Framework). If you need .NET Framework, stay on Npgsql 8.x.

Npgsql 9.0 introduced UUIDv7 generation for EF Core key values by default. When EF Core generates `Guid` keys client-side, Npgsql 9.0+ uses sequential UUIDv7 instead of random UUIDv4, improving index performance significantly.

Direct SSL support was added for PostgreSQL 17+, saving a roundtrip when establishing secure connections. Enable it with `SslNegotiation=direct` in your connection string.

OpenTelemetry tracing was improved with a `ConfigureTracing` API that lets you filter which commands are traced, add custom tags to spans, and control span naming.

Npgsql 10.0 (latest as of March 2026) targets .NET 10 and is considering deprecating synchronous APIs (`NpgsqlConnection.Open`, `NpgsqlCommand.ExecuteNonQuery`, etc.) in a future release. The recommendation is to use async APIs everywhere: `OpenAsync`, `ExecuteNonQueryAsync`, `ExecuteReaderAsync`.

## Part 7: Npgsql with Dapper

Dapper is a lightweight micro-ORM that extends `IDbConnection` with extension methods for mapping query results to objects. It works beautifully with Npgsql.

### Installation

```bash
dotnet add package Dapper
```

### Basic Queries

```csharp
using Dapper;
using Npgsql;

public class ProductRepository(NpgsqlDataSource dataSource)
{
    public async Task<IEnumerable<Product>> GetAllAsync()
    {
        await using var conn = await dataSource.OpenConnectionAsync();
        return await conn.QueryAsync<Product>("SELECT id, name, price, stock FROM products ORDER BY name");
    }

    public async Task<Product?> GetByIdAsync(int id)
    {
        await using var conn = await dataSource.OpenConnectionAsync();
        return await conn.QuerySingleOrDefaultAsync<Product>(
            "SELECT id, name, price, stock FROM products WHERE id = @Id",
            new { Id = id }
        );
    }

    public async Task<int> CreateAsync(Product product)
    {
        await using var conn = await dataSource.OpenConnectionAsync();
        return await conn.ExecuteScalarAsync<int>(
            """
            INSERT INTO products (name, price, stock)
            VALUES (@Name, @Price, @Stock)
            RETURNING id
            """,
            product
        );
    }

    public async Task<bool> UpdateAsync(Product product)
    {
        await using var conn = await dataSource.OpenConnectionAsync();
        var affected = await conn.ExecuteAsync(
            """
            UPDATE products
            SET name = @Name, price = @Price, stock = @Stock
            WHERE id = @Id
            """,
            product
        );
        return affected > 0;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        await using var conn = await dataSource.OpenConnectionAsync();
        var affected = await conn.ExecuteAsync("DELETE FROM products WHERE id = @Id", new { Id = id });
        return affected > 0;
    }
}
```

### Multi-Mapping (Joins)

```csharp
public async Task<IEnumerable<Order>> GetOrdersWithCustomerAsync()
{
    await using var conn = await dataSource.OpenConnectionAsync();
    var sql = """
        SELECT o.id, o.order_date, o.total,
               c.id, c.name, c.email
        FROM orders o
        INNER JOIN customers c ON o.customer_id = c.id
        ORDER BY o.order_date DESC
        """;

    return await conn.QueryAsync<Order, Customer, Order>(
        sql,
        (order, customer) =>
        {
            order.Customer = customer;
            return order;
        },
        splitOn: "id"  // Column where the second object starts
    );
}
```

### Transactions with Dapper

```csharp
public async Task TransferFundsAsync(int fromId, int toId, decimal amount)
{
    await using var conn = await dataSource.OpenConnectionAsync();
    await using var tx = await conn.BeginTransactionAsync();

    try
    {
        await conn.ExecuteAsync(
            "UPDATE accounts SET balance = balance - @Amount WHERE id = @Id",
            new { Amount = amount, Id = fromId },
            transaction: tx
        );

        await conn.ExecuteAsync(
            "UPDATE accounts SET balance = balance + @Amount WHERE id = @Id",
            new { Amount = amount, Id = toId },
            transaction: tx
        );

        await tx.CommitAsync();
    }
    catch
    {
        await tx.RollbackAsync();
        throw;
    }
}
```

### Dapper Tips for PostgreSQL

PostgreSQL uses `snake_case` column names, but C# uses `PascalCase` properties. Configure Dapper to handle this automatically:

```csharp
// In Program.cs or startup
Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;
```

Now `order_date` in PostgreSQL maps to `OrderDate` in C#.

For PostgreSQL arrays, Npgsql handles them natively:

```csharp
var tags = new[] { "electronics", "sale" };
var products = await conn.QueryAsync<Product>(
    "SELECT * FROM products WHERE tags && @Tags",
    new { Tags = tags }
);
```

For JSONB columns:

```csharp
var metadata = JsonSerializer.Serialize(new { source = "web", campaign = "spring" });
await conn.ExecuteAsync(
    "INSERT INTO events (type, metadata) VALUES (@Type, @Metadata::jsonb)",
    new { Type = "page_view", Metadata = metadata }
);
```

## Part 8: Npgsql with Entity Framework Core

### Installation

```bash
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL
```

### DbContext Configuration

```csharp
public class AppDbContext : DbContext
{
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<Customer> Customers => Set<Customer>();

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Use snake_case naming convention for all tables and columns
        modelBuilder.HasDefaultSchema("public");

        modelBuilder.Entity<Product>(entity =>
        {
            entity.ToTable("products");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(200);
            entity.Property(e => e.Price).HasColumnName("price").HasColumnType("decimal(10,2)");
            entity.Property(e => e.Stock).HasColumnName("stock");
            entity.Property(e => e.Tags).HasColumnName("tags").HasColumnType("text[]");
            entity.Property(e => e.Metadata).HasColumnName("metadata").HasColumnType("jsonb");
            entity.HasIndex(e => e.Name);
        });
    }
}
```

### Registration in ASP.NET

```csharp
// Program.cs
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        npgsqlOptions =>
        {
            npgsqlOptions.UseNodaTime();
            npgsqlOptions.MapEnum<OrderStatus>("order_status");
            npgsqlOptions.SetPostgresVersion(18, 0);  // Enable PG18-specific SQL generation
            npgsqlOptions.EnableRetryOnFailure(
                maxRetryCount: 3,
                maxRetryDelay: TimeSpan.FromSeconds(5),
                errorCodesToAdd: null
            );
        }
    )
);
```

### Migrations

```bash
# Add a migration
dotnet ef migrations add InitialCreate

# Apply migrations
dotnet ef database update

# Generate a SQL script (for production deployments)
dotnet ef migrations script -o migrations.sql
```

### PostgreSQL-Specific EF Core Features

**JSONB Columns:**

```csharp
public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public Dictionary<string, string> Metadata { get; set; } = new();
}

// In OnModelCreating
entity.Property(e => e.Metadata).HasColumnType("jsonb");

// Query JSONB
var products = await context.Products
    .Where(p => EF.Functions.JsonContains(p.Metadata, new { color = "red" }))
    .ToListAsync();
```

**Array Columns:**

```csharp
public class Product
{
    public int Id { get; set; }
    public string[] Tags { get; set; } = [];
}

// Query arrays
var electronics = await context.Products
    .Where(p => p.Tags.Contains("electronics"))
    .ToListAsync();
```

**Full-Text Search:**

```csharp
var results = await context.Products
    .Where(p => EF.Functions.ToTsVector("english", p.Name + " " + p.Description)
        .Matches(EF.Functions.ToTsQuery("english", "wireless & keyboard")))
    .ToListAsync();
```

**PostgreSQL Enums:**

```csharp
public enum OrderStatus { Pending, Processing, Shipped, Delivered, Cancelled }

// In OnModelCreating
modelBuilder.HasPostgresEnum<OrderStatus>();
modelBuilder.Entity<Order>().Property(e => e.Status).HasColumnType("order_status");

// In UseNpgsql configuration
npgsqlOptions.MapEnum<OrderStatus>("order_status");
```

### EF Core Performance Tips for PostgreSQL

Use compiled queries for hot paths:

```csharp
private static readonly Func<AppDbContext, int, Task<Product?>> GetProductById =
    EF.CompileAsyncQuery((AppDbContext ctx, int id) =>
        ctx.Products.FirstOrDefault(p => p.Id == id));
```

Use `AsNoTracking()` for read-only queries:

```csharp
var products = await context.Products.AsNoTracking().ToListAsync();
```

Use `ExecuteUpdateAsync` and `ExecuteDeleteAsync` for bulk operations (avoids loading entities):

```csharp
await context.Products
    .Where(p => p.Stock == 0)
    .ExecuteUpdateAsync(s => s.SetProperty(p => p.Status, "Discontinued"));

await context.Products
    .Where(p => p.DeletedAt < DateTime.UtcNow.AddYears(-1))
    .ExecuteDeleteAsync();
```

## Part 9: Transactions and Isolation Levels

### Transaction Basics

PostgreSQL supports full ACID transactions. Every statement in PostgreSQL runs inside a transaction. If you do not explicitly begin one, each statement is wrapped in an implicit transaction.

```sql
-- Explicit transaction
BEGIN;
    UPDATE accounts SET balance = balance - 100 WHERE id = 1;
    UPDATE accounts SET balance = balance + 100 WHERE id = 2;
COMMIT;

-- Rollback on error
BEGIN;
    UPDATE accounts SET balance = balance - 100 WHERE id = 1;
    -- Oops, something went wrong
ROLLBACK;
```

### Savepoints

Savepoints allow partial rollback within a transaction:

```sql
BEGIN;
    INSERT INTO orders (customer_id, total) VALUES (1, 99.99);
    SAVEPOINT before_items;

    INSERT INTO order_items (order_id, product_id, qty) VALUES (1, 100, 1);
    -- This fails due to a constraint violation
    ROLLBACK TO SAVEPOINT before_items;

    -- Try a different product
    INSERT INTO order_items (order_id, product_id, qty) VALUES (1, 200, 1);
COMMIT;
```

### Isolation Levels

PostgreSQL supports four isolation levels. Here is what each one actually does:

**Read Committed (Default):** Each statement within a transaction sees a snapshot of the database as of the moment that statement began execution. If another transaction commits between two statements in your transaction, the second statement sees the committed changes. This is the default and is appropriate for most workloads.

**Repeatable Read:** The transaction sees a snapshot of the database as of the moment the transaction began (not each statement). If another transaction commits changes to rows your transaction has read, and you try to update those same rows, PostgreSQL raises a serialization error and you must retry the transaction. This prevents non-repeatable reads and phantom reads.

**Serializable:** The strictest level. PostgreSQL guarantees that the result of concurrent serializable transactions is equivalent to some serial (one-at-a-time) ordering. If PostgreSQL detects that no such ordering is possible, it raises a serialization error. This is the safest but most restrictive level.

**Read Uncommitted:** In PostgreSQL, this is identical to Read Committed. PostgreSQL does not support dirty reads, ever. Setting `READ UNCOMMITTED` is accepted for SQL standard compliance but behaves as Read Committed.

```sql
-- Set isolation level for a transaction
BEGIN ISOLATION LEVEL REPEATABLE READ;
    SELECT * FROM accounts WHERE id = 1;
    -- ... more operations ...
COMMIT;

-- Set default isolation level for a session
SET default_transaction_isolation = 'repeatable read';
```

In C# with Npgsql:

```csharp
await using var conn = await dataSource.OpenConnectionAsync();
await using var tx = await conn.BeginTransactionAsync(IsolationLevel.RepeatableRead);

try
{
    // ... operations ...
    await tx.CommitAsync();
}
catch (PostgresException ex) when (ex.SqlState == "40001") // serialization_failure
{
    await tx.RollbackAsync();
    // Retry the entire transaction
}
```

### The NOLOCK Question

This deserves its own section because it is the single most common question from SQL Server developers.

In SQL Server, `NOLOCK` (or `READ UNCOMMITTED` isolation level) tells the engine to read data without acquiring shared locks. This prevents readers from blocking writers and vice versa. It is commonly used in SQL Server because the default Read Committed isolation level uses locking, which can cause severe blocking under concurrent load.

**You do not need NOLOCK in PostgreSQL. It does not exist, and you should not miss it.**

PostgreSQL uses MVCC for all isolation levels. Readers never block writers. Writers never block readers. The problem that `NOLOCK` solves in SQL Server simply does not exist in PostgreSQL. When you execute a `SELECT` in PostgreSQL, you read from a consistent snapshot without acquiring any locks that would block concurrent `INSERT`, `UPDATE`, or `DELETE` operations.

The only time you can experience blocking in PostgreSQL is when two transactions try to modify the same row concurrently. In that case, the second transaction waits for the first to commit or rollback. This is correct behavior — you would not want two concurrent updates to silently overwrite each other.

**Should you use `READ UNCOMMITTED` in development?** It makes no difference in PostgreSQL. It behaves identically to `READ COMMITTED`.

**Should you use `READ UNCOMMITTED` in production?** It makes no difference in PostgreSQL. But do not bother setting it. Just use the default `READ COMMITTED`.

**Bottom line: forget about `NOLOCK`. PostgreSQL solved this problem at the architecture level.**

### Advisory Locks

PostgreSQL provides advisory locks for application-level locking that does not correspond to any particular table or row:

```sql
-- Session-level advisory lock (held until session ends or explicitly released)
SELECT pg_advisory_lock(12345);
-- ... do exclusive work ...
SELECT pg_advisory_unlock(12345);

-- Transaction-level advisory lock (released at end of transaction)
BEGIN;
SELECT pg_advisory_xact_lock(12345);
-- ... do exclusive work ...
COMMIT;  -- Lock is automatically released

-- Try to acquire without blocking
SELECT pg_try_advisory_lock(12345);  -- Returns true/false
```

In C# with Npgsql:

```csharp
await using var conn = await dataSource.OpenConnectionAsync();
await using var tx = await conn.BeginTransactionAsync();

await using (var cmd = new NpgsqlCommand("SELECT pg_advisory_xact_lock(@key)", conn))
{
    cmd.Parameters.AddWithValue("key", 12345L);
    cmd.Transaction = tx;
    await cmd.ExecuteNonQueryAsync();
}

// ... perform exclusive work ...

await tx.CommitAsync(); // Advisory lock released
```

## Part 10: Networking, Sessions, and Connection Pooling

### SSL/TLS Configuration

For production, always encrypt connections. In `postgresql.conf`:

```ini
ssl = on
ssl_cert_file = '/path/to/server.crt'
ssl_key_file = '/path/to/server.key'
ssl_ca_file = '/path/to/ca.crt'

# PostgreSQL 18: Control TLS 1.3 cipher suites
ssl_tls13_ciphers = 'TLS_AES_256_GCM_SHA384:TLS_CHACHA20_POLY1305_SHA256'
```

In your .NET connection string:

```
Host=db.example.com;Database=prod;Username=app;Password=secret;SSL Mode=VerifyFull;Root Certificate=/path/to/ca.crt
```

### Connection Pooling

Npgsql has built-in connection pooling enabled by default. Each unique connection string gets its own pool. Key parameters:

```
Minimum Pool Size=0       # Pre-create this many connections
Maximum Pool Size=100     # Hard limit on concurrent connections
Connection Idle Lifetime=300  # Close idle connections after 5 minutes
Connection Pruning Interval=10  # Check for idle connections every 10 seconds
```

For high-concurrency applications, consider PgBouncer as an external connection pooler:

```ini
# pgbouncer.ini
[databases]
myappdb = host=127.0.0.1 port=5432 dbname=myappdb

[pgbouncer]
listen_port = 6432
listen_addr = 0.0.0.0
auth_type = scram-sha-256
auth_file = /etc/pgbouncer/userlist.txt
pool_mode = transaction    # transaction pooling is best for web apps
default_pool_size = 20
max_client_conn = 1000
```

With transaction-mode pooling, PgBouncer assigns a server connection to a client for the duration of a transaction, then returns it to the pool. This allows hundreds of application connections to share a much smaller number of PostgreSQL connections.

### Monitoring Active Sessions

```sql
-- View all active connections
SELECT pid, usename, datname, client_addr, state, query, query_start
FROM pg_stat_activity
WHERE state != 'idle'
ORDER BY query_start;

-- Kill a specific session
SELECT pg_terminate_backend(12345);

-- Cancel the current query in a session (gentler than terminate)
SELECT pg_cancel_backend(12345);

-- View connection counts by state
SELECT state, count(*)
FROM pg_stat_activity
GROUP BY state;
```

### Lock Monitoring

```sql
-- View current locks
SELECT l.pid, l.locktype, l.mode, l.granted,
       a.usename, a.query, a.state
FROM pg_locks l
JOIN pg_stat_activity a ON l.pid = a.pid
WHERE NOT l.granted
ORDER BY l.pid;

-- Find blocking queries
SELECT blocked.pid AS blocked_pid,
       blocked.query AS blocked_query,
       blocking.pid AS blocking_pid,
       blocking.query AS blocking_query
FROM pg_stat_activity blocked
JOIN pg_locks bl ON blocked.pid = bl.pid AND NOT bl.granted
JOIN pg_locks gl ON bl.locktype = gl.locktype
    AND bl.relation = gl.relation
    AND bl.page = gl.page
    AND bl.tuple = gl.tuple
    AND gl.granted
JOIN pg_stat_activity blocking ON gl.pid = blocking.pid
WHERE blocked.pid != blocking.pid;
```

## Part 11: Debugging and Performance Tuning

### EXPLAIN and EXPLAIN ANALYZE

This is the single most important debugging tool in PostgreSQL. `EXPLAIN` shows the query plan. `EXPLAIN ANALYZE` actually executes the query and shows real timing.

```sql
-- Show the query plan (does not execute)
EXPLAIN SELECT * FROM products WHERE price > 100;

-- Execute and show actual timing
EXPLAIN ANALYZE SELECT * FROM products WHERE price > 100;

-- PostgreSQL 18: BUFFERS is included automatically in EXPLAIN ANALYZE
-- In older versions, add it explicitly:
EXPLAIN (ANALYZE, BUFFERS) SELECT * FROM products WHERE price > 100;

-- Format as JSON (useful for visualization tools)
EXPLAIN (ANALYZE, FORMAT JSON) SELECT * FROM products WHERE price > 100;
```

Key things to look for in query plans:

**Seq Scan:** A full table scan. Fine for small tables, concerning for large ones. If you see a Seq Scan on a large table with a `WHERE` clause, you probably need an index.

**Index Scan:** Uses a B-tree (or other) index. This is what you want for selective queries.

**Index Only Scan:** Even better — the query is answered entirely from the index without accessing the table heap.

**Bitmap Index Scan + Bitmap Heap Scan:** Used when the query matches many rows. The bitmap index scan builds a bitmap of matching pages, then the bitmap heap scan fetches those pages. Efficient for medium-selectivity queries.

**Nested Loop / Hash Join / Merge Join:** Join strategies. Nested Loop is best for small result sets, Hash Join for larger ones, Merge Join when both inputs are sorted.

**Rows:** Compare "estimated" vs "actual" rows. Large discrepancies mean your statistics are stale (run `ANALYZE`).

### Statistics and ANALYZE

PostgreSQL's query planner relies on statistics about your data to choose efficient plans. These statistics are updated by the autovacuum daemon, but you can trigger an update manually:

```sql
-- Update statistics for a specific table
ANALYZE products;

-- Update statistics for the entire database
ANALYZE;

-- Check when statistics were last updated
SELECT schemaname, relname, last_analyze, last_autoanalyze
FROM pg_stat_user_tables;
```

### pg_stat_statements

This extension tracks execution statistics for all SQL statements:

```sql
-- Enable the extension
CREATE EXTENSION IF NOT EXISTS pg_stat_statements;

-- View top queries by total time
SELECT query, calls, total_exec_time, mean_exec_time, rows
FROM pg_stat_statements
ORDER BY total_exec_time DESC
LIMIT 20;

-- Reset statistics
SELECT pg_stat_statements_reset();
```

Add to `postgresql.conf`:

```ini
shared_preload_libraries = 'pg_stat_statements'
pg_stat_statements.track = all
```

### auto_explain

Automatically logs execution plans for slow queries:

```ini
# postgresql.conf
shared_preload_libraries = 'pg_stat_statements,auto_explain'
auto_explain.log_min_duration = 1000    # Log plans for queries > 1 second
auto_explain.log_analyze = on           # Include actual timing
auto_explain.log_buffers = on           # Include buffer usage
auto_explain.log_format = json          # JSON format for tooling
```

### Indexing Best Practices

```sql
-- Standard B-tree index (most common)
CREATE INDEX idx_products_name ON products (name);

-- Partial index (only indexes rows matching a condition)
CREATE INDEX idx_active_products ON products (name) WHERE active = true;

-- Multi-column index (order matters for leftmost prefix matching)
CREATE INDEX idx_orders_customer_date ON orders (customer_id, order_date DESC);

-- GIN index for full-text search
CREATE INDEX idx_products_fts ON products
    USING GIN (to_tsvector('english', name || ' ' || description));

-- GIN index for JSONB containment queries
CREATE INDEX idx_products_metadata ON products USING GIN (metadata);

-- GIN index for array containment
CREATE INDEX idx_products_tags ON products USING GIN (tags);

-- BRIN index for naturally ordered data (timestamps, sequences)
-- Much smaller than B-tree, good for append-only tables
CREATE INDEX idx_events_created ON events USING BRIN (created_at);

-- Covering index (includes extra columns to enable index-only scans)
CREATE INDEX idx_products_name_covering ON products (name) INCLUDE (price, stock);

-- Concurrent index creation (does not lock the table)
CREATE INDEX CONCURRENTLY idx_products_sku ON products (sku);
```

## Part 12: Free and Open-Source IDEs and GUI Tools on Linux

### pgAdmin 4

pgAdmin is the official PostgreSQL administration tool, maintained by the PostgreSQL Global Development Group. It is the equivalent of SQL Server Management Studio, though it operates as a web application.

**Installation on Fedora:**
```bash
sudo rpm -i https://ftp.postgresql.org/pub/pgadmin/pgadmin4/yum/pgadmin4-fedora-repo-2-1.noarch.rpm
sudo dnf install pgadmin4-desktop  # Desktop mode
# Or
sudo dnf install pgadmin4-web      # Web server mode
```

**Installation on Ubuntu:**
```bash
curl -fsS https://www.pgadmin.org/static/packages_pgadmin_org.pub | sudo gpg --dearmor -o /usr/share/keyrings/packages-pgadmin-org.gpg
echo "deb [signed-by=/usr/share/keyrings/packages-pgadmin-org.gpg] https://ftp.postgresql.org/pub/pgadmin/pgadmin4/apt/$(lsb_release -cs) pgadmin4 main" | sudo tee /etc/apt/sources.list.d/pgadmin4.list
sudo apt update && sudo apt install pgadmin4-desktop
```

**Strengths:** Comprehensive server administration, backup/restore wizards, role management, server monitoring dashboard, visual explain plan viewer, query history. It is free, official, and supports every PostgreSQL feature.

**Weaknesses:** The interface is web-based (runs a local web server), which makes it noticeably slower than native applications. The UI is dense and complex. Query autocompletion is basic compared to other tools. Startup time is slow. It only supports PostgreSQL.

### DBeaver Community Edition

DBeaver is the most popular general-purpose open-source database GUI. The Community Edition is free and open-source under the Apache License 2.0. It supports over 100 database types through JDBC drivers.

**Installation:**
```bash
# Flatpak (universal)
flatpak install flathub io.dbeaver.DBeaverCommunity

# Snap
sudo snap install dbeaver-ce

# Or download the .deb/.rpm from https://dbeaver.io/download/
```

**Strengths:** Supports virtually every database you will ever encounter. SQL editor with intelligent autocompletion. ER diagram generation. Data export to CSV, JSON, XML, SQL, Excel, HTML. Visual query builder. Active community with frequent releases. It works with PostgreSQL, SQL Server, MySQL, SQLite, Oracle, MongoDB, and dozens more from a single application.

**Weaknesses:** Java-based, so it can feel sluggish compared to native applications. The interface is feature-rich but busy. Initial schema loading can be slow on very large databases.

### Beekeeper Studio

Beekeeper Studio is a modern, cross-platform SQL editor focused on usability. The Community Edition is free and open-source under GPL v3.

**Installation:**
```bash
# Flatpak
flatpak install flathub io.beekeeperstudio.Studio

# Snap
sudo snap install beekeeper-studio

# Or download from https://www.beekeeperstudio.io/
```

**Strengths:** Clean, fast, modern interface. Excellent autocomplete. Tabbed query results. Native-feeling performance. Supports PostgreSQL, MySQL, SQLite, SQL Server, CockroachDB, and more. The simplest tool to pick up and use immediately.

**Weaknesses:** Fewer advanced administration features compared to pgAdmin or DBeaver. The free Community Edition has some limitations compared to the paid Ultimate edition (though all PostgreSQL core features are free).

### DbGate

DbGate is a free, open-source database client that runs both as a desktop application and as a web application. It supports SQL and NoSQL databases.

**Installation:**
```bash
# Snap
sudo snap install dbgate

# Or download from https://dbgate.org/
```

**Strengths:** Works in the browser (no installation needed for the web version). Supports PostgreSQL, MySQL, SQL Server, MongoDB, SQLite, CockroachDB, and more. Data archiving and comparison features. Active development.

**Weaknesses:** Smaller community than DBeaver or pgAdmin. Some rough edges in the UI.

### pgcli (Terminal)

Already mentioned above, but worth emphasizing: pgcli is the best terminal-based PostgreSQL client. It provides intelligent autocompletion, syntax highlighting, and multi-line editing.

```bash
pip install pgcli
# or
sudo dnf install pgcli
```

### Visual Studio Code with PostgreSQL Extension

Microsoft released an official PostgreSQL extension for VS Code. It provides an object explorer, query editor with IntelliSense, schema visualization, and query history. Since many .NET developers already live in VS Code, this is a natural choice.

**Installation:**
Search for "PostgreSQL" in the VS Code extensions marketplace and install the one by Microsoft.

### Azure Data Studio

Azure Data Studio (formerly SQL Operations Studio) is Microsoft's cross-platform database tool. While it originated as a SQL Server tool, it supports PostgreSQL through an extension. It is free and open-source.

```bash
# Download from https://learn.microsoft.com/en-us/azure-data-studio/download
# Or install via Snap/Flatpak
```

### Adminer

Adminer is a single PHP file that provides a complete database management interface. If you have PHP installed, you can deploy it in seconds. It supports PostgreSQL, MySQL, SQLite, SQL Server, and Oracle.

```bash
# Download the single file
wget https://www.adminer.org/latest.php -O adminer.php
php -S localhost:8080 adminer.php
# Open http://localhost:8080 in your browser
```

### Comparison Summary

For pure PostgreSQL administration, use **pgAdmin**. It has every feature and is maintained by the PostgreSQL team. For a general-purpose GUI that handles multiple databases beautifully, use **DBeaver Community**. For a fast, clean, modern developer experience, use **Beekeeper Studio**. For terminal work, use **pgcli**. For integration with your editor, use the **VS Code PostgreSQL extension**.

All of these tools are completely free and open-source. None require payment for any feature relevant to PostgreSQL development work on Linux.

## Part 13: Backup and Restore

### pg_dump and pg_restore

```bash
# Dump a single database to a custom-format file (recommended)
pg_dump -h localhost -U myapp -d myappdb -Fc -f backup.dump

# Dump to plain SQL
pg_dump -h localhost -U myapp -d myappdb -f backup.sql

# Dump only the schema (no data)
pg_dump -h localhost -U myapp -d myappdb --schema-only -f schema.sql

# Dump only the data (no schema)
pg_dump -h localhost -U myapp -d myappdb --data-only -f data.sql

# Restore from custom format
pg_restore -h localhost -U myapp -d myappdb -c backup.dump

# Restore from plain SQL
psql -h localhost -U myapp -d myappdb -f backup.sql

# Dump all databases
pg_dumpall -h localhost -U postgres -f all-databases.sql
```

### PostgreSQL 17: Incremental Backups

```bash
# Enable WAL summarization
ALTER SYSTEM SET summarize_wal = on;
SELECT pg_reload_conf();

# Take a full base backup
pg_basebackup -D /backups/full -Ft -z -P

# Take an incremental backup (only changes since last backup)
pg_basebackup -D /backups/incr1 --incremental /backups/full/backup_manifest -Ft -z -P

# Combine full + incremental for restore
pg_combinebackup /backups/full /backups/incr1 -o /backups/combined
```

### Automated Backups with Cron

```bash
# Daily backup at 2 AM, keep 7 days
# Add to crontab: crontab -e
0 2 * * * pg_dump -h localhost -U myapp -d myappdb -Fc -f /backups/myappdb-$(date +\%Y\%m\%d).dump && find /backups -name "myappdb-*.dump" -mtime +7 -delete
```

## Part 14: Common SQL Patterns for .NET Developers

### Pagination

```sql
-- Offset-based (simple but slow for large offsets)
SELECT * FROM products ORDER BY id LIMIT 20 OFFSET 40;

-- Cursor-based (efficient for large datasets)
SELECT * FROM products WHERE id > @LastId ORDER BY id LIMIT 20;
```

### Upsert (INSERT ON CONFLICT)

```sql
INSERT INTO products (sku, name, price, stock)
VALUES ('WIDGET-001', 'Widget', 9.99, 100)
ON CONFLICT (sku)
DO UPDATE SET
    name = EXCLUDED.name,
    price = EXCLUDED.price,
    stock = EXCLUDED.stock;
```

### Common Table Expressions (CTEs)

```sql
-- Recursive CTE for hierarchical data (e.g., categories)
WITH RECURSIVE category_tree AS (
    -- Base case: root categories
    SELECT id, name, parent_id, 0 AS depth
    FROM categories
    WHERE parent_id IS NULL

    UNION ALL

    -- Recursive case: children
    SELECT c.id, c.name, c.parent_id, ct.depth + 1
    FROM categories c
    INNER JOIN category_tree ct ON c.parent_id = ct.id
)
SELECT * FROM category_tree ORDER BY depth, name;
```

### Window Functions

```sql
-- Rank products by price within each category
SELECT name, category, price,
       RANK() OVER (PARTITION BY category ORDER BY price DESC) AS price_rank,
       AVG(price) OVER (PARTITION BY category) AS avg_category_price
FROM products;

-- Running total
SELECT order_date, total,
       SUM(total) OVER (ORDER BY order_date) AS running_total
FROM orders;
```

### GENERATE_SERIES

```sql
-- Generate a date series (useful for reports with no gaps)
SELECT d::date AS day,
       COALESCE(SUM(o.total), 0) AS daily_total
FROM generate_series('2026-01-01'::date, '2026-01-31'::date, '1 day') AS d
LEFT JOIN orders o ON o.order_date::date = d::date
GROUP BY d::date
ORDER BY d::date;
```

### Full-Text Search

```sql
-- Add a tsvector column (or use a generated column)
ALTER TABLE products ADD COLUMN search_vector tsvector
    GENERATED ALWAYS AS (to_tsvector('english', name || ' ' || coalesce(description, ''))) STORED;

-- Create a GIN index
CREATE INDEX idx_products_search ON products USING GIN (search_vector);

-- Search
SELECT name, ts_rank(search_vector, query) AS rank
FROM products, to_tsquery('english', 'wireless & keyboard') AS query
WHERE search_vector @@ query
ORDER BY rank DESC;
```

## Part 15: OpenTelemetry and Observability

Npgsql has built-in OpenTelemetry support:

```bash
dotnet add package Npgsql.OpenTelemetry
```

```csharp
// Program.cs
builder.Services.AddNpgsqlDataSource(
    connectionString,
    dataSourceBuilder =>
    {
        dataSourceBuilder.ConfigureTracing(tracing =>
        {
            tracing.ConfigureCommandFilter(cmd =>
                !cmd.CommandText.StartsWith("SELECT 1")); // Filter out health checks
        });
    }
);

builder.Services.AddOpenTelemetry()
    .WithTracing(tracing =>
    {
        tracing.AddNpgsql();
        tracing.AddAspNetCoreInstrumentation();
        tracing.AddOtlpExporter();
    });
```

This emits OpenTelemetry spans for every database command, including the SQL text (sanitized by default), duration, and error information. You can view these in Jaeger, Zipkin, Grafana Tempo, or any OpenTelemetry-compatible backend.

For metrics, Npgsql emits connection pool statistics (active connections, idle connections, pending requests) as OpenTelemetry metrics automatically when you configure the tracing above.

## Part 16: Security Best Practices

Always use SCRAM-SHA-256 authentication, never MD5 (deprecated in PostgreSQL 18). Always use SSL in production. Never use the `postgres` superuser for application connections; create dedicated users with minimal privileges.

```sql
-- Create a read-only user
CREATE ROLE readonly_user WITH LOGIN PASSWORD 'secure-password';
GRANT CONNECT ON DATABASE myappdb TO readonly_user;
GRANT USAGE ON SCHEMA public TO readonly_user;
GRANT SELECT ON ALL TABLES IN SCHEMA public TO readonly_user;
ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT SELECT ON TABLES TO readonly_user;

-- Create an application user with read/write but no DDL
CREATE ROLE app_user WITH LOGIN PASSWORD 'secure-password';
GRANT CONNECT ON DATABASE myappdb TO app_user;
GRANT USAGE ON SCHEMA public TO app_user;
GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA public TO app_user;
GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA public TO app_user;
ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO app_user;
ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT USAGE, SELECT ON SEQUENCES TO app_user;
```

Use row-level security for multi-tenant applications:

```sql
ALTER TABLE tenant_data ENABLE ROW LEVEL SECURITY;

CREATE POLICY tenant_isolation ON tenant_data
    USING (tenant_id = current_setting('app.current_tenant')::int);

-- In your application, set the tenant context per request:
-- SET app.current_tenant = '42';
```

## Part 17: Migrating from SQL Server Mental Models

Here is a quick reference for translating SQL Server concepts to PostgreSQL:

SQL Server's `IDENTITY` becomes PostgreSQL's `SERIAL` or `GENERATED ALWAYS AS IDENTITY`. SQL Server's `NVARCHAR(MAX)` becomes PostgreSQL's `TEXT` (there is no performance difference between `VARCHAR(n)` and `TEXT` in PostgreSQL; `TEXT` is preferred). SQL Server's `DATETIME2` becomes PostgreSQL's `TIMESTAMPTZ` (always use the timezone-aware variant). SQL Server's `BIT` becomes PostgreSQL's `BOOLEAN`. SQL Server's `UNIQUEIDENTIFIER` becomes PostgreSQL's `UUID`. SQL Server's `NVARCHAR(n)` becomes PostgreSQL's `VARCHAR(n)` or `TEXT` (PostgreSQL stores all text as UTF-8 by default; there is no separate `N` prefix). SQL Server's `TOP n` becomes PostgreSQL's `LIMIT n`. SQL Server's `ISNULL()` becomes PostgreSQL's `COALESCE()`. SQL Server's `GETDATE()` becomes PostgreSQL's `now()` or `CURRENT_TIMESTAMP`. SQL Server's square-bracket quoting `[column]` becomes PostgreSQL's double-quote quoting `"column"`, but you should use `snake_case` and avoid quoting entirely. SQL Server's `@@IDENTITY` / `SCOPE_IDENTITY()` becomes PostgreSQL's `RETURNING id` clause. SQL Server's stored procedures written in T-SQL become PostgreSQL functions or procedures written in PL/pgSQL, though many .NET developers prefer to keep logic in the application layer.

## Conclusion

PostgreSQL is a world-class database that is completely free, fully featured, and exceptionally well-supported in the .NET ecosystem through Npgsql. Whether you are building a small side project or an enterprise application, PostgreSQL provides everything you need: MVCC concurrency that eliminates the locking headaches of SQL Server, a rich type system with native JSON, arrays, and full-text search support, excellent performance through the new AIO subsystem in PostgreSQL 18, and first-class .NET integration through Npgsql with both Dapper and Entity Framework Core.

The tooling on Linux is mature and diverse. pgAdmin gives you full administration capabilities, DBeaver gives you a universal GUI, Beekeeper Studio gives you a beautiful modern interface, pgcli gives you a superb terminal experience, and VS Code gives you database access without leaving your editor. All of it is free. All of it is open source.

The configuration is straightforward once you understand the two key files: `postgresql.conf` for server behavior and `pg_hba.conf` for authentication. Docker and Podman make it trivially easy to spin up PostgreSQL for development. And with the connection pooling built into Npgsql (or external via PgBouncer), your ASP.NET applications can handle massive concurrent loads efficiently.

If you are coming from SQL Server, the transition is smoother than you might expect. The SQL is standard. The concepts are familiar. The main adjustments are embracing MVCC (and forgetting about `NOLOCK`), adopting `snake_case` naming conventions, and learning the PostgreSQL-specific extensions like JSONB, arrays, and full-text search that do not have direct SQL Server equivalents.

Welcome to PostgreSQL. Your database just became free forever.
