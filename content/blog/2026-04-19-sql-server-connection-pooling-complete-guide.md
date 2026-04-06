---
title: "The Complete Guide to SQL Server Connection Pooling in ASP.NET: From Framework 4.8 to .NET 10"
date: 2026-04-19
author: myblazor-team
summary: An exhaustive, deeply practical guide to SQL Server connection pooling in ASP.NET applications — covering ADO.NET, Dapper, Entity Framework Core, every configuration knob, monitoring strategies, common failure modes, and when to raise or lower the default pool size of 100.
tags:
  - aspnet
  - dotnet
  - performance
  - deep-dive
  - best-practices
  - architecture
  - csharp
---

# The Complete Guide to SQL Server Connection Pooling in ASP.NET: From Framework 4.8 to .NET 10

---

## Prologue: The Thursday Afternoon That Cost $40,000

It's a Thursday afternoon. Your e-commerce platform is running the biggest flash sale of the year. Traffic is 4x the normal peak. Then, one by one, your application servers start throwing errors. Not HTTP 500 errors from bad code — something more specific, more sinister:

```
System.InvalidOperationException: Timeout expired. The timeout period elapsed prior to 
obtaining a connection from the pool. This may have occurred because all pooled connections 
were in use and max pool size was reached.
```

Requests begin to queue. The queue backs up. Pages stop loading. Support tickets flood in. Revenue evaporates in real-time on the sales dashboard. Your entire on-call team scrambles in a conference bridge. Someone suggests restarting the web servers. That buys you three minutes before it happens again. 

The root cause, once the dust settles, is not a missing index, not a slow query, not a DDOS attack. It is a pool of one hundred database connections that was never sized for the load you just put on it — and a handful of code paths that hold those connections open far longer than they should. Forty thousand dollars of lost revenue and a four-hour outage, caused by a single number: `100`.

This guide exists so that number never surprises you again.

---

## Part 1: What a Database Connection Actually Is (And Why It Is So Expensive)

### 1.1 The Physical Reality of a Database Connection

Before we talk about pooling, we need to understand what we are pooling. When your C# code calls `connection.Open()` against SQL Server, a remarkable amount of work happens underneath that single method call. Most developers never think about it because it is fast enough on a local developer machine — maybe 2–5 milliseconds. But in a production environment, that same call, unoptimized and against a server that might be in a different data center rack or even a different geographic region in a cloud deployment, can take 50 to 200 milliseconds or more. Every. Single. Time.

Here is the physical sequence of events when you open a fresh, non-pooled connection to SQL Server:

**Step 1 — TCP socket establishment.** The client operating system's TCP stack initiates a three-way handshake (SYN, SYN-ACK, ACK) with the SQL Server's port (default 1433). This involves a full round-trip across the network, meaning even for a server on the same LAN, you are burning at minimum one network round-trip — usually 0.1–1ms locally, but 20–100ms across the internet or even across data center zones.

**Step 2 — TLS handshake.** Since SQL Server 2022 and the modern `Microsoft.Data.SqlClient` driver, encryption is enabled by default. This means a TLS handshake occurs: key exchange, certificate validation, cipher negotiation. This is cryptographic work that involves multiple additional round-trips and CPU-intensive operations on both ends. In the era of `System.Data.SqlClient` and older drivers without encryption-by-default, this step was often skipped, which is why developers on older systems may not have felt the sting of connection establishment as acutely.

**Step 3 — SQL Server pre-login and login packets.** Once the transport layer is established, SQL Server and the client exchange pre-login packets (version negotiation, encryption requirements). Then the actual TDS (Tabular Data Stream) login packet is sent. This contains your credentials, your requested database, language settings, application name, workstation name, and other metadata.

**Step 4 — Authentication.** SQL Server validates your credentials. If you are using SQL Authentication (username and password), this involves hashing and comparing passwords. If you are using Windows Authentication (Integrated Security), this involves Kerberos or NTLM authentication against Active Directory, which can itself involve additional network round-trips to domain controllers.

**Step 5 — Session initialization.** SQL Server creates a new Server Process ID (SPID) for your connection. It allocates memory structures for the session: a buffer pool, a log buffer, lock manager structures, and session-level settings. It runs any server-side login triggers you may have configured. It sets your session-level settings: `SET ANSI_NULLS ON`, `SET ANSI_WARNINGS ON`, `SET QUOTED_IDENTIFIER ON`, and so on.

**Step 6 — Database selection.** SQL Server connects your session to the requested database. If you specified `Initial Catalog=MyDatabase` in your connection string, it performs the equivalent of a `USE MyDatabase` statement, which has its own security checks and metadata lookups.

**Step 7 — Confirmation.** SQL Server sends the login acknowledgement back to the client. The client receives it, processes it, and your `Open()` call finally returns.

All of that for one connection. Now imagine doing it once for every incoming HTTP request on a web application handling 1,000 requests per second. You would spend more time opening connections than doing actual database work. This is the problem that connection pooling exists to solve.

### 1.2 Memory and Resources on the SQL Server Side

It is equally important to understand what each connection costs on the SQL Server side, because this is often what gets ignored when people blindly set `Max Pool Size=500`.

Each SQL Server connection consumes memory. Microsoft's documentation and community measurements suggest that each connection requires roughly 24 KB of memory for the memory protection ring buffer, plus additional memory for the worker thread associated with that connection. On SQL Server 2019 and later, the default value for `max worker threads` is 0, which means SQL Server auto-configures it based on processor count. On a typical 8-core 64-bit machine, this auto-configuration produces around 576 worker threads.

Here is the critical insight: **each active SQL Server connection ties up one worker thread**. A worker thread is not a free resource — it is a full OS thread with its own stack (typically 512KB to 4MB). If you have 576 worker threads and 576 simultaneous connections all executing queries, your SQL Server is working at absolute maximum capacity. Add one more connection, and the 577th request has to wait in a queue until a worker becomes free. This is called "thread starvation" on the SQL Server side, and it can make your database appear to be slow when the real problem is resource exhaustion.

This is why the answer to "my connections are timing out" is almost never "set Max Pool Size to 1000." A pool size of 1000 with multiple application servers could mean 3,000 or 4,000 connections hammering a SQL Server instance with 576 worker threads. You would be creating the very problem you are trying to solve.

### 1.3 The Network as a Shared Resource

One more dimension that most connection pooling articles skip: the network itself. Each open SQL Server connection maintains a persistent TCP socket. Sockets are file descriptors in the operating system. On Linux and Windows, there are practical limits to the number of open file descriptors per process and per system. On Windows, the default maximum number of sockets is effectively the ephemeral port range: about 16,384 ports by default, configurable up to 65,535. In practice, with a busy web server making connections to multiple backend services, you can run into "port exhaustion" as a separate problem from pool exhaustion — you run out of OS-level sockets before you even hit your pool size limits.

This is another reason why modest pool sizes and aggressive connection return (closing connections quickly) are virtues, not just good habits.

---

## Part 2: Connection Pooling — The Architecture from First Principles

### 2.1 The Pool as a Cache

Connection pooling is, at its most fundamental level, a caching strategy applied to database connections. Instead of discarding a valuable resource (an open, authenticated, network-connected session to SQL Server) when you are done with it, you hold onto it in an in-memory store and loan it out to the next person who needs it.

The pool is maintained entirely on the **client side** — inside your ASP.NET application process. SQL Server knows nothing about the pool as a concept. From SQL Server's perspective, the connection that was serving your 10:00:01 AM request and the connection serving the 10:00:02 AM request are the exact same session (the same SPID), just executing different batches one after another. The pool is transparent to the database.

This is a crucial point with several important implications:

1. **The pool is per process, per AppDomain, per connection string.** Each unique connection string creates a separate pool. If your application runs on four web servers, you have four completely independent pools. If your application has two slightly different connection strings (perhaps with different timeout values, or slightly different whitespace), you have two pools even on the same server.

2. **The pool does not survive application restarts.** When your IIS application pool recycles or your Kestrel process restarts, all pooled connections are discarded and new physical connections must be established with SQL Server. This is one reason why you see a performance "cold start" effect after a deployment.

3. **The pool is thread-safe but not connection-safe.** Multiple threads share the pool, but each individual connection (each `SqlConnection` object) is not thread-safe. You must never share a single `SqlConnection` instance between threads.

### 2.2 The Lifecycle of a Pooled Connection — Step by Step

Let's trace what happens during a typical request in an ASP.NET Core application that uses connection pooling correctly.

**Request arrives. Controller method executes.**

```csharp
// Inside your controller or repository
using var connection = new SqlConnection(connectionString);
await connection.OpenAsync(cancellationToken);
// ... execute query ...
// using block exits — connection.Dispose() is called
```

**Step 1 — `new SqlConnection(connectionString)`:** No physical connection is created yet. `SqlConnection` is a thin wrapper. It just stores the connection string and a reference to the pool manager. This is essentially free — a few object allocations on the managed heap.

**Step 2 — `OpenAsync()` is called:** The pool manager (the `SqlConnectionPool` class inside `Microsoft.Data.SqlClient` or `System.Data.SqlClient`) is consulted. It looks up the pool for this specific connection string. If an idle connection exists in the pool, it is immediately returned. The "blocking period" check runs (more on this later). `sp_reset_connection` is called by the server to clear session state. `OpenAsync()` returns. Total time: typically under 1 millisecond.

**Step 3 — If no idle connection is available:** A new physical connection is created from scratch (the expensive process described in Part 1). This takes 10–200ms depending on network and authentication. This new connection is added to the pool's tracking structures. `OpenAsync()` returns once the new connection is ready. 

**Step 4 — If no idle connection is available AND we are at max pool size:** `OpenAsync()` blocks (asynchronously waits). The `Connection Timeout` setting (default 15 seconds) starts counting down. If a connection is returned to the pool before the timeout expires, this request gets it. If 15 seconds pass without a connection becoming available, `InvalidOperationException` is thrown with the message you saw at the beginning of this article.

**Step 5 — Query execution:** Your code uses the connection to execute queries. The connection is considered "checked out" from the pool and is not available to other callers.

**Step 6 — `Dispose()` is called** (via the `using` block): The connection is returned to the pool. No actual closing of the TCP socket. No authentication teardown. The connection's session state is reset (via `sp_reset_connection` internally), and it is marked as available for the next caller. This is essentially instant.

**Step 7 — Idle connection cleanup:** The pool periodically prunes idle connections. Connections that have been idle for approximately 4–8 minutes are physically closed. Connections to servers that have become unreachable are eventually detected and removed.

### 2.3 The Pool Manager in Detail — What sp_reset_connection Does

When a connection is returned to the pool and then checked out again, it must not carry any state from its previous user. A connection that just committed a transaction should not appear to be inside a transaction to the next user. A connection that just executed `SET TRANSACTION ISOLATION LEVEL SERIALIZABLE` should not impose that isolation level on the next query.

The mechanism that handles this is `sp_reset_connection`. This is an internal, undocumented stored procedure that SQL Server executes automatically when a connection is reused from the pool (you can see it in SQL Profiler traces). It resets:

- All `SET` options to their defaults
- All open cursors
- Any temp tables created with `#` prefix (local temp tables are scoped to the session, but they are dropped automatically on disconnect; `sp_reset_connection` does not drop them because the session isn't ending)
- Transaction context — any uncommitted transaction is rolled back
- Lock state — all session-level locks are released
- Row count settings
- Any `CONTEXT_INFO` set via `SET CONTEXT_INFO`

What `sp_reset_connection` does **not** reset (and this is critical):

- Global temporary tables (`##GlobalTemp`)
- Any changes to `tempdb` objects that survive connection lifecycle
- Login trigger side effects (login triggers run on the initial physical login, not on pool reuse)
- Any server-side state changed via `sp_set_session_context` in some configurations

Understanding what resets and what does not is essential for any application that relies on session-level state in SQL Server.

### 2.4 Pool Fragmentation — The Silent Performance Killer

One of the most common and least understood causes of poor connection pool performance in real applications is pool fragmentation. Remember: a separate pool is maintained for each unique connection string. "Unique" here means byte-for-byte identical, including whitespace, keyword order, and case of keywords (although the pool manager does normalize some differences, subtle variations still cause fragmentation).

Consider this scenario. Your application retrieves connection strings from configuration. In one code path, you have:

```csharp
var cs = "Server=sql01;Database=AppDb;Integrated Security=True;";
```

In another code path, perhaps written by a different developer or a different era of the codebase:

```csharp
var cs = "Data Source=sql01;Initial Catalog=AppDb;Integrated Security=True;";
```

These two strings connect to exactly the same server and database with the same authentication. But they are different strings, so they create two separate pools. Your effective max pool size has now been halved. Each pool grows independently, and neither may reach its maximum before the application starts throwing timeout errors.

Other common fragmentation scenarios:

- **Dynamically constructed connection strings** with user-specific parameters embedded in them (a terrible practice for connection pooling, but it happens)
- **Multiple environments** sharing code that appends debug parameters to connection strings in development but not production
- **Application Name** — if different parts of your application set different `Application Name` values in the connection string, they get different pools
- **Enlist** and other transaction-related parameters that differ between callers
- **Different Timeout values** — even `Connect Timeout=30` vs `Connect Timeout=15` creates two pools

The fix is simple: maintain a single, canonical connection string stored in one place (your `appsettings.json` or secrets store), loaded once at startup, and used everywhere. Never construct connection strings dynamically in hot paths.

---

## Part 3: Connection Pooling in ASP.NET Framework 4.8

### 3.1 The Classic Era — System.Data.SqlClient

For developers still maintaining ASP.NET Framework 4.8 applications (and there are millions of you — the framework is still supported and widely deployed), the connection pooling story centers around `System.Data.SqlClient`, which is built into the .NET Framework and shipped as part of Windows.

In Framework 4.8, `System.Data.SqlClient` is part of `System.Data.dll`, which lives in the Global Assembly Cache (GAC). You don't add a NuGet package for it — it's simply available. The connection pooling behavior is identical in concept to what we've described, but with some Framework-specific nuances.

### 3.2 Configuration in web.config

The canonical location for connection strings in a Framework 4.8 application is `web.config`:

```xml
<configuration>
  <connectionStrings>
    <add name="DefaultConnection" 
         connectionString="Data Source=sql01.corp.local;
                          Initial Catalog=MyAppDb;
                          Integrated Security=True;
                          Min Pool Size=5;
                          Max Pool Size=100;
                          Connection Timeout=15;
                          Connect Timeout=15;"
         providerName="System.Data.SqlClient" />
  </connectionStrings>
</configuration>
```

Note the distinction between `Connection Timeout` and `Connect Timeout`. They are aliases for each other in the `System.Data.SqlClient` parser, but `Connection Timeout` is the canonical name as documented by Microsoft. Both refer to the number of seconds to wait for a connection to be established (or checked out of the pool), defaulting to 15 seconds.

### 3.3 The Classic Repository Pattern in Framework 4.8

The standard pattern for using `SqlConnection` in a Framework 4.8 application looks like this:

```csharp
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;

public class CustomerRepository
{
    private readonly string _connectionString;

    public CustomerRepository()
    {
        _connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"]
            .ConnectionString;
    }

    public Customer GetById(int customerId)
    {
        // This using block is ESSENTIAL
        // Without it, the connection is never returned to the pool
        using (var connection = new SqlConnection(_connectionString))
        {
            connection.Open(); // Checks out from pool
            
            using (var command = new SqlCommand(
                "SELECT Id, Name, Email FROM Customers WHERE Id = @Id", 
                connection))
            {
                command.Parameters.Add("@Id", SqlDbType.Int).Value = customerId;
                
                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return new Customer
                        {
                            Id = reader.GetInt32(reader.GetOrdinal("Id")),
                            Name = reader.GetString(reader.GetOrdinal("Name")),
                            Email = reader.GetString(reader.GetOrdinal("Email"))
                        };
                    }
                    return null;
                }
            }
        } // connection.Dispose() called here — connection returned to pool
    }
}
```

The `using` statement is not optional. It is not a stylistic preference. It is the mechanism by which connections are returned to the pool. A connection not returned to the pool is a leaked connection. Leaked connections accumulate. When they reach the pool maximum, all new requests time out. This is the most common cause of pool exhaustion in production.

### 3.4 Common Anti-Patterns in Framework 4.8 Applications

The Framework 4.8 era introduced several anti-patterns that we still see in legacy codebases today. Here are the most damaging:

**Anti-Pattern 1: Storing a SqlConnection as a class field or static variable.**

```csharp
// ❌ NEVER DO THIS
public class OrderService
{
    private static SqlConnection _connection = 
        new SqlConnection(ConfigurationManager.ConnectionStrings["Default"].ConnectionString);

    public Order GetOrder(int id)
    {
        _connection.Open(); // Might throw if already open
        // ...
    }
}
```

A static `SqlConnection` is never returned to the pool. It lives for the lifetime of the AppDomain. It blocks one pool slot permanently. It causes race conditions when multiple threads attempt to use it simultaneously (SqlConnection is not thread-safe). This pattern is unfortunately common in Web Forms applications written in the 2003–2008 era.

**Anti-Pattern 2: Opening a connection at the top of a method and holding it through network calls or heavy computation.**

```csharp
// ❌ Connection held while doing expensive work
public void ProcessOrder(int orderId)
{
    using (var conn = new SqlConnection(connectionString))
    {
        conn.Open(); // Connection checked out here
        
        var order = GetOrderFromDb(conn, orderId);
        
        // This HTTP call might take 5-10 seconds
        var shippingQuote = externalShippingApi.GetQuote(order);
        
        // This PDF generation might take 2-3 seconds
        var invoice = pdfGenerator.CreateInvoice(order);
        
        // Connection has been checked out for 7-13+ seconds
        // by the time we actually use it again here
        SaveOrderResults(conn, orderId, shippingQuote, invoice);
    }
}
```

The fix: do the database work first, close the connection, do the slow work, then open a new connection for the final save. With connection pooling, that second `Open()` call is nearly free.

**Anti-Pattern 3: Forgetting to close the SqlDataReader.**

```csharp
// ❌ Reader not closed, connection held until GC
public List<Customer> GetAllCustomers()
{
    var connection = new SqlConnection(connectionString);
    connection.Open();
    
    var command = new SqlCommand("SELECT * FROM Customers", connection);
    var reader = command.ExecuteReader(); // No using block!
    
    var customers = new List<Customer>();
    while (reader.Read())
    {
        customers.Add(MapCustomer(reader));
    }
    
    return customers;
    // Neither reader, command, nor connection are disposed!
    // All three will sit in memory until GC collects them
    // which may be minutes later
}
```

The correct pattern:

```csharp
// ✅ Correct — everything properly disposed
public List<Customer> GetAllCustomers()
{
    var customers = new List<Customer>();
    
    using (var connection = new SqlConnection(connectionString))
    using (var command = new SqlCommand("SELECT * FROM Customers", connection))
    {
        connection.Open();
        using (var reader = command.ExecuteReader(CommandBehavior.CloseConnection))
        {
            while (reader.Read())
            {
                customers.Add(MapCustomer(reader));
            }
        }
    }
    
    return customers;
}
```

**Anti-Pattern 4: Using DataAdapter.Fill() without closing the connection.**

```csharp
// This one is sneaky — DataAdapter will open the connection if it is closed,
// and leave it open if it was already open when Fill() was called.
var connection = new SqlConnection(connectionString);
connection.Open(); // You open it manually
var adapter = new SqlDataAdapter("SELECT * FROM Products", connection);
var table = new DataTable();
adapter.Fill(table); // DataAdapter sees connection is already open, leaves it open

// You intend to close it but forget...
return table;
// Connection stays open until GC!
```

The DataAdapter pattern is safer when you let the DataAdapter manage the connection:

```csharp
// ✅ DataAdapter manages connection lifecycle
using (var connection = new SqlConnection(connectionString))
using (var adapter = new SqlDataAdapter("SELECT * FROM Products", connection))
{
    var table = new DataTable();
    adapter.Fill(table); // Opens, fills, closes connection automatically
    return table;
}
```

### 3.5 Connection Pooling and IIS Application Pools

In an IIS-hosted ASP.NET Framework 4.8 application, connection pools live inside the IIS worker process (`w3wp.exe`). When IIS recycles the application pool (which it does periodically by default — every 29 hours, or upon exceeding memory thresholds, or on a schedule), the worker process is recycled and all connection pools are destroyed.

This has an important implication: if you have `Min Pool Size=10` configured, those 10 connections are established lazily (on the first request after a recycle) unless you have a warm-up mechanism. The first few requests after an application pool recycle experience the full cold-start overhead of establishing new physical connections.

The solution in Framework 4.8 applications is the Application Startup event in `Global.asax.cs`:

```csharp
protected void Application_Start(object sender, EventArgs e)
{
    // Warm up the connection pool at startup
    WarmUpConnectionPool();
}

private static void WarmUpConnectionPool()
{
    var connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"]
        .ConnectionString;
    
    var warmupCount = 5; // Matches Min Pool Size
    var connections = new List<SqlConnection>();
    
    try
    {
        for (int i = 0; i < warmupCount; i++)
        {
            var conn = new SqlConnection(connectionString);
            conn.Open();
            connections.Add(conn);
        }
    }
    catch (Exception ex)
    {
        // Log but don't crash startup — the pool will warm up organically
        System.Diagnostics.EventLog.WriteEntry("Application", 
            $"Connection pool warm-up failed: {ex.Message}", 
            System.Diagnostics.EventLogEntryType.Warning);
    }
    finally
    {
        foreach (var conn in connections)
        {
            conn.Dispose(); // Return to pool
        }
    }
}
```

### 3.6 Windows Authentication and Pool Segregation in Framework 4.8

When using `Integrated Security=True` in Framework 4.8 Web Forms or MVC applications, the pool manager creates a separate connection pool for each Windows identity. If your application impersonates different users (for example, if you are building an intranet application that impersonates the logged-in Windows user for data access), each user gets their own pool.

This is catastrophically bad for scalability. If you have 500 concurrent users and `Integrated Security=True` with impersonation, you could theoretically have 500 separate pools, each allowed to grow to `Max Pool Size=100`. That's 50,000 potential connections to SQL Server, which obviously cannot be satisfied.

The standard solution is to use a dedicated service account for database access rather than impersonating end users. The application authenticates to the database as a single identity (`CORP\AppServiceAccount`), and application-level security (who can see what data) is enforced in the application layer rather than at the SQL Server level. This results in a single pool that is shared across all users, allowing efficient resource utilization.

### 3.7 Clearing the Pool — ClearPool and ClearAllPools

`System.Data.SqlClient` (and its successor `Microsoft.Data.SqlClient`) expose two static methods for manually clearing pools:

```csharp
// Clear all pools for a specific connection string
SqlConnection.ClearPool(connection);

// Clear all pools managed by this process
SqlConnection.ClearAllPools();
```

These methods are rarely needed in normal operation but are invaluable in specific scenarios:

**Database failover.** When a SQL Server instance fails over to a secondary (in an Always On Availability Group), the connections in the pool that were connected to the primary are now pointing to a dead server. They will eventually be cleaned up by the pool's dead connection detection, but "eventually" can mean minutes. Calling `ClearAllPools()` after detecting a failover forces immediate reconnection to the new primary.

```csharp
// In your retry/resilience policy, after detecting a failover
catch (SqlException ex) when (ex.Number == -2 || ex.Number == 10054 || ex.Number == 10060)
{
    SqlConnection.ClearAllPools(); // Force reconnection
    // Then retry...
}
```

**Password rotation.** If you rotate the SQL login password used in your connection string (for security compliance), existing pooled connections that authenticated with the old password will continue to work until they are naturally pruned. New connections will fail until you update the connection string. Calling `ClearAllPools()` after updating the connection string ensures all connections re-authenticate with the new credentials.

**Schema migration.** If your deployment process modifies database schema in ways that might affect cached execution plans or connection-level state, clearing the pool after migration ensures a clean slate.

---

## Part 4: Connection Pooling in ASP.NET on .NET 10 — The Modern Stack

### 4.1 Microsoft.Data.SqlClient — The New Standard

When you move to ASP.NET Core on .NET 10, the connection pooling story changes in several important ways. The most important change: you should be using `Microsoft.Data.SqlClient`, not `System.Data.SqlClient`.

`Microsoft.Data.SqlClient` was introduced in August 2019 as a NuGet-distributed, cross-platform replacement for `System.Data.SqlClient`. The key differences:

- It is the **only** actively developed SQL Server driver from Microsoft. `System.Data.SqlClient` (the NuGet package) has been deprecated and will not support .NET 10.
- It enables **encryption by default** on all connections (`Encrypt=true` unless overridden), which is more secure but requires careful attention to certificate trust.
- It supports **Microsoft Entra ID authentication** (Azure AD), Always Encrypted, JSON data types (v6+), and other SQL Server 2022+ features.
- It provides **performance counters and EventSource-based metrics** that work in .NET Core, whereas the old `System.Data.SqlClient` only supported performance counters on .NET Framework.
- It is cross-platform: it runs identically on Windows, Linux, and macOS.
- As of version 7.0, it extracts Azure dependencies into a separate optional package, so you no longer pull in Azure SDK assemblies if you don't need Entra ID auth.

To use it in your .NET 10 project:

```xml
<PackageReference Include="Microsoft.Data.SqlClient" Version="7.0.0" />
```

And update your using statements:

```csharp
// Old
using System.Data.SqlClient;

// New
using Microsoft.Data.SqlClient;
```

The connection pooling API is identical. `SqlConnection`, `SqlCommand`, `SqlDataReader` — all the same classes, same methods, same behavior, different namespace.

### 4.2 Connection Strings in .NET 10 — appsettings.json

In ASP.NET Core on .NET 10, connection strings live in `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=sql01.corp.local;Database=MyAppDb;Integrated Security=True;Min Pool Size=5;Max Pool Size=100;Connect Timeout=30;TrustServerCertificate=False;Encrypt=True;"
  }
}
```

And accessed via:

```csharp
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
```

### 4.3 The New Encryption Default and TrustServerCertificate

A change that has caught many developers migrating from `System.Data.SqlClient` to `Microsoft.Data.SqlClient` by surprise: **encryption is enabled by default**. In `System.Data.SqlClient`, the default was `Encrypt=False`. In `Microsoft.Data.SqlClient`, it is `Encrypt=True`.

This means if your SQL Server is using a self-signed certificate (common in development environments, less common but still seen in production), your connections will fail with a certificate validation error unless you add `TrustServerCertificate=True` to the connection string.

**In development:**

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=DevDb;Integrated Security=True;TrustServerCertificate=True;"
  }
}
```

**In production:**

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=sql01.corp.local;Database=ProdDb;Integrated Security=True;Encrypt=True;TrustServerCertificate=False;"
  }
}
```

Never set `TrustServerCertificate=True` in production. It disables certificate validation, making your application vulnerable to man-in-the-middle attacks on the database connection. If you are getting certificate errors in production, the correct fix is to install a valid SSL certificate on your SQL Server instance, not to disable validation.

### 4.4 Dependency Injection and the Repository Pattern in .NET 10

The modern ASP.NET Core way to manage database connections is through the built-in dependency injection container. Here is a complete example showing proper connection management in a .NET 10 application:

**Define a connection factory:**

```csharp
// IDbConnectionFactory.cs
public interface IDbConnectionFactory
{
    Task<SqlConnection> CreateOpenConnectionAsync(CancellationToken cancellationToken = default);
}

// SqlServerConnectionFactory.cs
public sealed class SqlServerConnectionFactory : IDbConnectionFactory
{
    private readonly string _connectionString;

    public SqlServerConnectionFactory(string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        _connectionString = connectionString;
    }

    public async Task<SqlConnection> CreateOpenConnectionAsync(CancellationToken cancellationToken = default)
    {
        var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }
}
```

**Register in Program.cs:**

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

// Register as singleton — the factory only holds a connection string
// (no state), so singleton lifetime is correct and efficient
builder.Services.AddSingleton<IDbConnectionFactory>(sp =>
    new SqlServerConnectionFactory(
        builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("DefaultConnection not found")));

// Register repositories as scoped (per-request lifetime)
builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();

var app = builder.Build();
```

**Use in a repository:**

```csharp
// CustomerRepository.cs
public sealed class CustomerRepository : ICustomerRepository
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ILogger<CustomerRepository> _logger;

    public CustomerRepository(
        IDbConnectionFactory connectionFactory,
        ILogger<CustomerRepository> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public async Task<Customer?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT Id, Name, Email, CreatedAt FROM Customers WHERE Id = @Id";
        command.Parameters.Add(new SqlParameter("@Id", SqlDbType.Int) { Value = id });
        
        await using var reader = await command.ExecuteReaderAsync(
            CommandBehavior.SingleRow, 
            cancellationToken);
        
        if (await reader.ReadAsync(cancellationToken))
        {
            return new Customer
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1),
                Email = reader.GetString(2),
                CreatedAt = reader.GetDateTime(3)
            };
        }
        
        return null;
        
        // await using ensures Dispose() is called, returning connection to pool
    }

    public async Task<IReadOnlyList<Customer>> GetByEmailDomainAsync(
        string domain, 
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        
        await using var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT Id, Name, Email, CreatedAt 
            FROM Customers 
            WHERE Email LIKE @Domain
            ORDER BY Name";
        command.Parameters.Add(new SqlParameter("@Domain", SqlDbType.NVarChar, 500)
        {
            Value = $"%@{domain}"
        });

        var customers = new List<Customer>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        
        while (await reader.ReadAsync(cancellationToken))
        {
            customers.Add(new Customer
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1),
                Email = reader.GetString(2),
                CreatedAt = reader.GetDateTime(3)
            });
        }

        return customers.AsReadOnly();
    }
}
```

Note the use of `await using` (C# 8+) instead of just `using`. This is the async disposal pattern, essential for async code. `SqlConnection.DisposeAsync()` is called, which closes the connection and returns it to the pool asynchronously.

### 4.5 The Blocking Period — A Rarely Discussed Feature

Both `System.Data.SqlClient` and `Microsoft.Data.SqlClient` implement a feature called the "blocking period" (also called the "error blocking period"). When connection pooling is enabled and a connection attempt fails (wrong password, server unreachable, etc.), subsequent connection attempts will fail immediately for the next 5 seconds without even trying to connect. This prevents rapid retry storms when a database is down.

You can see this behavior if you make a typo in your connection string during development — after the first failure, the next several requests fail instantly with the same error, then there's a brief pause, then they try again and fail again, in a 5-second cycle.

In `Microsoft.Data.SqlClient`, you can disable the blocking period for specific scenarios (like highly latency-sensitive services that need to retry instantly):

```csharp
var builder = new SqlConnectionStringBuilder(connectionString);
builder.PoolBlockingPeriod = PoolBlockingPeriod.NeverBlock;
var cs = builder.ConnectionString;
```

The options are:
- `Auto` (default) — Blocking period is enabled when connecting to SQL Server, disabled when connecting to Azure SQL Database (which is more resilient to transient failures and expects immediate retry)
- `AlwaysBlock` — Blocking period always enabled
- `NeverBlock` — Blocking period always disabled

For Azure SQL Database workloads, the `Auto` setting is already optimal — it disables the blocking period and allows your retry policies (Polly, etc.) to kick in immediately.

---

## Part 5: Connection Pooling with Dapper

### 5.1 What Dapper Is and What It Is Not

Dapper, created by Sam Saffron and Nick Craver at Stack Overflow and open-sourced at github.com/DapperLib/Dapper, is a "micro-ORM" — more accurately, it is a set of extension methods on `IDbConnection`. It knows how to map SQL query results to C# objects with zero friction and near-zero overhead.

What Dapper is not: a connection manager. Dapper has absolutely no connection pooling logic of its own. It delegates entirely to the underlying ADO.NET provider (`SqlConnection` for SQL Server) for all connection management. When you open a `SqlConnection` and hand it to Dapper, Dapper uses it. When Dapper is done, it does nothing special to the connection — you are responsible for disposing it.

This is one of Dapper's great strengths (zero magic, full transparency) and its greatest pitfall for inexperienced developers (full responsibility means full exposure to mistakes).

### 5.2 The Fundamental Dapper Pattern

Here is the correct way to use Dapper with `SqlConnection` in an ASP.NET Core application:

```csharp
using Dapper;
using Microsoft.Data.SqlClient;

public sealed class ProductRepository : IProductRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public ProductRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<Product?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        
        // Dapper extension method on IDbConnection
        return await connection.QueryFirstOrDefaultAsync<Product>(
            "SELECT Id, Name, Price, StockQuantity FROM Products WHERE Id = @Id",
            new { Id = id });
    }

    public async Task<IEnumerable<Product>> GetByCategoryAsync(
        int categoryId, 
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        
        const string sql = @"
            SELECT p.Id, p.Name, p.Price, p.StockQuantity, c.Name AS CategoryName
            FROM Products p
            INNER JOIN Categories c ON p.CategoryId = c.Id
            WHERE p.CategoryId = @CategoryId
            AND p.IsActive = 1
            ORDER BY p.Name";
        
        return await connection.QueryAsync<Product>(sql, new { CategoryId = categoryId });
    }
    
    public async Task<int> InsertAsync(CreateProductRequest request, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        
        const string sql = @"
            INSERT INTO Products (Name, Price, StockQuantity, CategoryId, CreatedAt)
            VALUES (@Name, @Price, @StockQuantity, @CategoryId, @CreatedAt);
            SELECT CAST(SCOPE_IDENTITY() AS INT);";
        
        return await connection.ExecuteScalarAsync<int>(sql, new
        {
            request.Name,
            request.Price,
            request.StockQuantity,
            request.CategoryId,
            CreatedAt = DateTime.UtcNow
        });
    }
}
```

### 5.3 Dapper and Transactions

Transactions with Dapper require that you explicitly manage the transaction and pass it to each Dapper call:

```csharp
public async Task TransferStockAsync(
    int sourceProductId, 
    int destinationProductId, 
    int quantity, 
    CancellationToken cancellationToken = default)
{
    await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
    
    // Begin a transaction on the checked-out connection
    await using var transaction = await connection.BeginTransactionAsync(
        IsolationLevel.ReadCommitted, 
        cancellationToken);
    
    try
    {
        // Deduct from source
        await connection.ExecuteAsync(
            "UPDATE Products SET StockQuantity = StockQuantity - @Quantity WHERE Id = @Id",
            new { Id = sourceProductId, Quantity = quantity },
            transaction: transaction);  // Pass transaction to Dapper
        
        // Add to destination
        await connection.ExecuteAsync(
            "UPDATE Products SET StockQuantity = StockQuantity + @Quantity WHERE Id = @Id",
            new { Id = destinationProductId, Quantity = quantity },
            transaction: transaction);  // Same transaction
        
        // Commit
        await transaction.CommitAsync(cancellationToken);
    }
    catch
    {
        await transaction.RollbackAsync(cancellationToken);
        throw;
    }
    // await using ensures both transaction and connection are disposed
    // Connection is returned to pool; any uncommitted transaction is rolled back by sp_reset_connection
}
```

### 5.4 Dapper Multi-Mapping and Multiple Result Sets

Dapper's multi-mapping feature allows you to map a single query to multiple objects, which is useful for join queries. Here's how it works and why it's important for connection pooling:

```csharp
// Multi-mapping: one query, two object types
// Crucially: one connection, one round-trip
public async Task<IEnumerable<OrderWithCustomer>> GetRecentOrdersAsync(
    int days,
    CancellationToken cancellationToken = default)
{
    await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
    
    const string sql = @"
        SELECT o.Id, o.TotalAmount, o.OrderDate,
               c.Id, c.Name, c.Email
        FROM Orders o
        INNER JOIN Customers c ON o.CustomerId = c.Id
        WHERE o.OrderDate >= @Since
        ORDER BY o.OrderDate DESC";
    
    var orders = await connection.QueryAsync<Order, Customer, OrderWithCustomer>(
        sql,
        (order, customer) => new OrderWithCustomer { Order = order, Customer = customer },
        new { Since = DateTime.UtcNow.AddDays(-days) },
        splitOn: "Id"  // The column where the second type starts
    );
    
    return orders;
}

// Multiple result sets: one connection, multiple queries in one round-trip
public async Task<DashboardData> GetDashboardDataAsync(CancellationToken cancellationToken = default)
{
    await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
    
    const string sql = @"
        SELECT COUNT(*) FROM Orders WHERE OrderDate >= DATEADD(day, -30, GETUTCDATE());
        SELECT COUNT(*) FROM Customers WHERE CreatedAt >= DATEADD(day, -30, GETUTCDATE());
        SELECT SUM(TotalAmount) FROM Orders WHERE OrderDate >= DATEADD(day, -30, GETUTCDATE());";
    
    using var multi = await connection.QueryMultipleAsync(sql);
    
    return new DashboardData
    {
        RecentOrderCount = await multi.ReadSingleAsync<int>(),
        NewCustomerCount = await multi.ReadSingleAsync<int>(),
        RecentRevenue = await multi.ReadSingleAsync<decimal>()
    };
    // Three queries, one connection, one round-trip
    // This is far better than three separate queries each checking out their own connection
}
```

The multiple result sets pattern is critically important for connection pooling efficiency. Three separate calls to `GetByIdAsync` would check out three connections (serially, sequentially) from the pool. One call with `QueryMultiple` uses one connection for all three queries. As your application scales, reducing the number of connections needed per request directly translates to lower pool pressure.

### 5.5 The Dapper Connection Exhaustion War Story

Here is a real scenario that plays out on production systems regularly. An ASP.NET Core API endpoint looks like this:

```csharp
// ❌ BAD: This looks innocent but is lethal under load
[HttpGet("dashboard")]
public async Task<IActionResult> GetDashboard()
{
    // Each of these hits the database separately
    var orderStats = await _orderService.GetStatsAsync();      // Uses 1 connection
    var customerStats = await _customerService.GetStatsAsync(); // Uses 1 connection
    var inventoryStats = await _productService.GetStatsAsync(); // Uses 1 connection
    var revenueStats = await _financeService.GetStatsAsync();   // Uses 1 connection
    
    // Each service internally creates a connection, queries, and disposes it
    // But they do so SERIALLY, so at peak you need 4 connections per request
    // With default Max Pool Size=100 and 25 concurrent dashboard requests...
    // ...you've consumed all 100 connections just for this one endpoint
    
    return Ok(new { orderStats, customerStats, inventoryStats, revenueStats });
}
```

Under light load this works fine. Under production load with 50 concurrent dashboard requests, each needing 4 connections serially, you need 200 connections — but your pool only has 100. Requests start timing out. The oncall engineer wakes up at 3 AM.

There are two solutions. The first is to batch the queries:

```csharp
// ✅ BETTER: Batch multiple queries into one connection/round-trip
[HttpGet("dashboard")]
public async Task<IActionResult> GetDashboard(CancellationToken cancellationToken)
{
    var dashboard = await _dashboardService.GetAllStatsAsync(cancellationToken);
    return Ok(dashboard);
}

// In DashboardService:
public async Task<DashboardStats> GetAllStatsAsync(CancellationToken cancellationToken)
{
    await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
    
    const string sql = @"
        SELECT COUNT(*) FROM Orders WHERE OrderDate >= DATEADD(day, -30, GETUTCDATE());
        SELECT COUNT(*) FROM Customers WHERE CreatedAt >= DATEADD(day, -30, GETUTCDATE());
        SELECT COUNT(*) FROM Products WHERE StockQuantity < 10;
        SELECT ISNULL(SUM(TotalAmount), 0) FROM Orders WHERE YEAR(OrderDate) = YEAR(GETUTCDATE());";
    
    using var multi = await connection.QueryMultipleAsync(sql);
    return new DashboardStats
    {
        RecentOrders = await multi.ReadSingleAsync<int>(),
        NewCustomers = await multi.ReadSingleAsync<int>(),
        LowStockProducts = await multi.ReadSingleAsync<int>(),
        YearToDateRevenue = await multi.ReadSingleAsync<decimal>()
    };
    // One connection, one round-trip, four results
}
```

The second is parallel execution with bounded concurrency (for independent queries that benefit from parallelism):

```csharp
// ✅ ALSO GOOD: Parallel with each query on its own connection
// But now each concurrent request uses 4 connections simultaneously
// Make sure your Max Pool Size can support this
[HttpGet("dashboard")]
public async Task<IActionResult> GetDashboard(CancellationToken cancellationToken)
{
    var orderTask = _orderService.GetStatsAsync(cancellationToken);
    var customerTask = _customerService.GetStatsAsync(cancellationToken);
    var inventoryTask = _productService.GetStatsAsync(cancellationToken);
    var revenueTask = _financeService.GetStatsAsync(cancellationToken);
    
    await Task.WhenAll(orderTask, customerTask, inventoryTask, revenueTask);
    
    // 4 connections used simultaneously, but request completes faster
    return Ok(new {
        orders = orderTask.Result,
        customers = customerTask.Result,
        inventory = inventoryTask.Result,
        revenue = revenueTask.Result
    });
}
```

The parallel approach is faster (wall-clock time) but uses 4 connections simultaneously per request instead of 4 serially. With 25 concurrent requests, you'd need 100 connections simultaneously — right at the default limit. The batch approach uses 1 connection per request, so 25 concurrent requests need only 25 connections. For a dashboard that isn't performance-critical, batching wins. For a latency-sensitive endpoint where the extra 50ms of serial execution matters, parallelism wins — but you must size your pool accordingly.

---

## Part 6: Connection Pooling with Entity Framework Core

### 6.1 Two Levels of Pooling — Understanding the Stack

Entity Framework Core introduces an important complexity to the connection pooling story: there are now **two separate and independent pooling mechanisms** that can be active simultaneously:

1. **ADO.NET connection pooling** (managed by `Microsoft.Data.SqlClient`) — pools physical database connections (TCP sockets, sessions). This is the same pool described throughout this article.

2. **EF Core DbContext pooling** (managed by EF Core via `AddDbContextPool`) — pools `DbContext` instances (CLR objects in your application process). This avoids the overhead of allocating, initializing, and garbage-collecting `DbContext` objects.

These two systems are completely orthogonal. They pool different things. They are configured independently. A `DbContext` instance can be recycled from EF's pool regardless of whether the connection it uses comes from ADO.NET's pool.

### 6.2 ADO.NET Connection Pooling with EF Core

EF Core manages connection opening and closing for you. By default, EF opens a connection just before executing a query and closes it immediately after, returning it to the ADO.NET pool. This is optimal behavior — connections are checked out for the minimum time necessary.

If you are using `AddDbContext` (the standard registration), each HTTP request gets a new `DbContext` instance. The connection is opened and closed (returned to pool) for each database operation. This is efficient, though there is overhead in creating and garbage-collecting the `DbContext` objects themselves.

The connection string passed to EF Core's `UseSqlServer()` is passed through to `SqlConnection`, so all the connection string parameters we've discussed apply:

```csharp
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions =>
        {
            sqlOptions.CommandTimeout(30); // 30 second command timeout
            sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 3,
                maxRetryDelay: TimeSpan.FromSeconds(5),
                errorNumbersToAdd: null);
        }));
```

And in your connection string:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=sql01;Database=AppDb;Integrated Security=True;Min Pool Size=10;Max Pool Size=100;Encrypt=True;"
  }
}
```

### 6.3 DbContext Pooling — What It Is and When to Use It

`AddDbContextPool` keeps a pool of `DbContext` instances in memory. When a request comes in, instead of newing up a `DbContext`, EF retrieves one from the pool. When the request ends, EF resets the `DbContext` state and returns it to the pool.

The default pool size in EF Core is 1,024 instances (as of EF Core 6 and later — earlier versions defaulted to 128). This is much larger than the ADO.NET pool's default of 100, because `DbContext` instances are cheap to hold in memory (they contain no active resources), while physical database connections are expensive.

To enable:

```csharp
builder.Services.AddDbContextPool<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")),
    poolSize: 256); // Tune based on your concurrent request volume
```

The `poolSize` parameter sets the maximum number of `DbContext` instances retained in the pool. Once this limit is exceeded, new `DbContext` instances are created on-demand and not returned to the pool after use (they are just garbage-collected normally). So `poolSize` is a soft ceiling on the pool's memory footprint, not a hard limit on concurrency.

#### What DbContext Pooling Resets

When a pooled `DbContext` is returned to the pool and then checked out again, EF Core resets:

- All tracked entities (the change tracker is cleared)
- Query filters
- DbContext-level interceptors reset to defaults
- Any `IDbContextTransaction` is disposed

What it does **not** reset:

- Services injected into the `DbContext` constructor — this means any service with Scoped lifetime that is injected into a pooled `DbContext` creates a problem, because Scoped services are per-request while the pooled `DbContext` lives across requests.

This is the most important constraint of `DbContext` pooling: **your DbContext must be stateless beyond what EF manages automatically**. You cannot store custom data in fields of a pooled `DbContext`.

```csharp
// ❌ This DbContext cannot be safely pooled
public class AppDbContext : DbContext
{
    private readonly ICurrentUserService _currentUser; // Scoped service!
    
    public AppDbContext(DbContextOptions<AppDbContext> options, ICurrentUserService currentUser)
        : base(options)
    {
        _currentUser = currentUser; // Problem: this would be from a different request's scope
    }
    
    // Query filter that uses _currentUser is now using stale data
    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Entity<Document>()
            .HasQueryFilter(d => d.OwnerId == _currentUser.UserId);
    }
}
```

The solution for multi-tenancy and user-context filtering with DbContext pooling is to use `IResettableService` (EF Core's interface for DbContext services that need to reset between uses) or to use `PooledDbContextFactory` directly with explicit scoping.

#### Measuring the Impact of DbContext Pooling

Microsoft's own benchmarks, referenced in the EF Core documentation, show that DbContext pooling can reduce request latency by up to 50% in high-throughput scenarios compared to `AddDbContext`. The savings come entirely from eliminating CLR allocations and GC pressure — `DbContext` objects are not tiny. They maintain a change tracker, a model cache reference, a list of interceptors, and various internal state objects.

For a rough estimate: on a web server handling 1,000 requests per second with `AddDbContext`, you are creating and garbage-collecting 1,000 `DbContext` objects per second. Each object might be 5–20KB of managed memory, plus the GC overhead of tracking and collecting it. With `AddDbContextPool`, that's zero allocations after the pool is warm.

### 6.4 Full Example — AddDbContextPool with Repository Pattern

```csharp
// Program.cs
builder.Services.AddDbContextPool<AppDbContext>(options =>
{
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions =>
        {
            sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 3,
                maxRetryDelay: TimeSpan.FromSeconds(5),
                errorNumbersToAdd: null);
            sqlOptions.CommandTimeout(30);
        });

    // Enable detailed errors in development only
    if (builder.Environment.IsDevelopment())
    {
        options.EnableDetailedErrors();
        options.EnableSensitiveDataLogging();
    }
    
    // Disable thread safety checks for performance (safe in ASP.NET Core DI)
    options.EnableThreadSafetyChecks(false);
}, poolSize: 256);

builder.Services.AddScoped<IProductRepository, EfProductRepository>();
```

```csharp
// AppDbContext.cs
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Product> Products => Set<Product>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<Customer> Customers => Set<Customer>();

    // IMPORTANT for pooling: do NOT inject scoped services into a pooled DbContext
    // If you need user context, use a separate service resolved per-query

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
```

```csharp
// EfProductRepository.cs
public sealed class EfProductRepository : IProductRepository
{
    private readonly AppDbContext _context;
    private readonly ILogger<EfProductRepository> _logger;

    public EfProductRepository(AppDbContext context, ILogger<EfProductRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Product?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Products
            .AsNoTracking() // Important: no tracking for read-only operations
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Product>> GetByCategoryAsync(
        int categoryId, 
        CancellationToken cancellationToken = default)
    {
        return await _context.Products
            .AsNoTracking()
            .Where(p => p.CategoryId == categoryId && p.IsActive)
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<Product> CreateAsync(Product product, CancellationToken cancellationToken = default)
    {
        _context.Products.Add(product);
        await _context.SaveChangesAsync(cancellationToken);
        return product;
    }
    
    public async Task UpdateAsync(Product product, CancellationToken cancellationToken = default)
    {
        _context.Products.Update(product);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
```

### 6.5 AsNoTracking — The Most Important EF Core Performance Setting for Pooling

By default, EF Core tracks every entity it queries. This means it maintains an internal copy of the entity's original values so it can detect changes when `SaveChanges()` is called. This tracking is implemented via the `ChangeTracker` — a dictionary-like structure maintained by the `DbContext`.

For read-only queries (the majority of queries in most web applications), tracking is pure overhead. It:
- Allocates memory for the original-values snapshot
- Performs equality comparisons during change detection
- Adds entries to the change tracker's internal dictionary
- Increases GC pressure

`AsNoTracking()` disables tracking for a specific query. `AsNoTrackingWithIdentityResolution()` disables tracking but still resolves duplicate entities (useful for join queries where the same entity might appear multiple times). `UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking)` sets the default for the entire context.

For a web API where 90% of operations are reads, consider setting the global default to no-tracking and opting in to tracking for write operations:

```csharp
// In AppDbContext.cs
public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
{
    ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
    ChangeTracker.LazyLoadingEnabled = false; // Explicit loading only
}
```

This keeps the change tracker empty for the vast majority of requests, which is particularly important with `DbContext` pooling since a large change tracker would need to be cleared when the context is returned to the pool.

### 6.6 Compiled Queries — Precompiling LINQ for Hot Paths

For queries that run hundreds or thousands of times per second, EF Core's LINQ compilation overhead (converting a LINQ expression tree to SQL) can be noticeable. EF Core caches compiled queries after the first execution, but the cache lookup itself has a cost proportional to query complexity. For truly hot paths, precompiling the query eliminates even this overhead:

```csharp
// Define compiled queries as static fields — compiled once, reused forever
public static class CompiledQueries
{
    public static readonly Func<AppDbContext, int, Task<Product?>> GetProductById =
        EF.CompileAsyncQuery((AppDbContext context, int id) =>
            context.Products.FirstOrDefault(p => p.Id == id));

    public static readonly Func<AppDbContext, int, IAsyncEnumerable<Product>> GetProductsByCategory =
        EF.CompileAsyncQuery((AppDbContext context, int categoryId) =>
            context.Products
                .Where(p => p.CategoryId == categoryId && p.IsActive)
                .OrderBy(p => p.Name));
}

// Usage
var product = await CompiledQueries.GetProductById(_context, id);

await foreach (var product in CompiledQueries.GetProductsByCategory(_context, categoryId))
{
    // Process product
}
```

---

## Part 7: The Connection String Parameters — Every Knob Explained

### 7.1 All ADO.NET Pool Configuration Parameters

This section provides an exhaustive reference for every connection pool-related parameter you can set in a SQL Server connection string with `Microsoft.Data.SqlClient`.

---

**`Pooling` (bool, default: `true`)**

Enables or disables connection pooling for connections using this string. Set to `false` only if you have a specific reason: integration tests that need deterministic connection behavior, diagnostic scenarios, or connections to embedded databases where pooling adds no value.

```
Pooling=False;
```

When pooling is disabled, every `Open()` creates a new physical connection and every `Close()` physically tears it down. The performance overhead is significant but the behavior is perfectly predictable — useful in test environments.

---

**`Min Pool Size` (int, default: `0`)**

The minimum number of connections that the pool will maintain. These connections are established when the pool is first created and kept alive even when idle.

Setting this to 0 (the default) means the pool starts empty and grows on demand. The first N requests after application startup pay the full connection establishment cost.

Setting this to a small positive number (5–20) means these connections are established at startup and kept warm, eliminating cold-start latency for the first requests. The tradeoff is that these connections consume resources on SQL Server even during idle periods (nights, weekends).

Recommendation: Set `Min Pool Size=5` to `Min Pool Size=20` for production applications that need consistent response times, especially when `LoadBalanceTimeout` (see below) could prune connections to zero during off-peak hours.

```
Min Pool Size=10;
```

---

**`Max Pool Size` (int, default: `100`)**

The maximum number of connections the pool will maintain. This is the most important and most frequently misunderstood parameter. When all connections are checked out (in use), further `Open()` calls wait for a connection to be returned, up to the `Connection Timeout` period.

The default of 100 is a conservative, broadly safe value. It was chosen to prevent accidental exhaustion of SQL Server resources by a single misconfigured application. For many applications, 100 is appropriate. For some, it needs to be adjusted.

See Part 8 for a detailed analysis of when and how to adjust this value.

```
Max Pool Size=100;
```

---

**`Connection Timeout` / `Connect Timeout` (int, default: `15`)**

The number of seconds to wait when attempting to obtain a connection — either from the pool (if the pool is full) or from the server (if establishing a new connection). After this time, `InvalidOperationException` is thrown.

The default of 15 seconds is generally appropriate. In high-load scenarios, a 15-second wait before throwing an exception means your thread is blocked for 15 seconds, which can cascade — if 100 requests are all waiting 15 seconds for a connection, you have 100 threads (or async continuations) piled up, increasing memory pressure.

For highly concurrent APIs, consider setting this lower (5–10 seconds) and implementing retry logic, so failed requests fail fast and make room for successful ones rather than piling up in a queue.

```
Connect Timeout=15;
```

---

**`Connection Lifetime` / `Load Balance Timeout` (int, default: `0`)**

The number of seconds a connection can remain in the pool before being destroyed. Default of 0 means connections can live indefinitely in the pool (they are only pruned after approximately 4–8 minutes of idle time by the background cleanup mechanism).

Setting this is most relevant in load-balanced environments where multiple SQL Server nodes exist behind a load balancer. With `Connection Lifetime=30`, connections are recycled every 30 seconds, ensuring that the load is redistributed across nodes as the pool refreshes. Without this setting, a pool that connected to node A during startup would continue to favor node A indefinitely.

For single-server SQL Server deployments, this setting is less important.

```
Connection Lifetime=60;
```

---

**`Connection Reset` (bool, default: `true`)**

When true, the connection state is reset (via `sp_reset_connection`) when a connection is drawn from the pool. Setting this to false is dangerous — it means the connection's previous session state (active transactions, SET options, etc.) could bleed through to the next user of the connection. This should essentially never be set to false in production.

```
Connection Reset=True;
```

---

**`Enlist` (bool, default: `true`)**

When true (the default), connections are automatically enlisted in the current `System.Transactions.Transaction` (ambient transaction) when checked out of the pool. This is the mechanism that allows `TransactionScope` to automatically enlist multiple ADO.NET connections in the same distributed transaction.

Setting `Enlist=False` means the connection ignores the ambient transaction context. This is sometimes necessary for operations that should bypass the current transaction (for example, logging operations that should be committed even if the main transaction rolls back).

```
Enlist=False;
```

Note: Connections enlisted in transactions are partitioned from connections not enlisted in transactions. The pool manages two logical groups. When you check out a connection while inside a `TransactionScope`, you get a connection that is enlisted. When you check out a connection outside a `TransactionScope`, you get an unenrolled connection.

---

**`Persist Security Info` (bool, default: `false`)**

When false (the default), the password is removed from the connection string after the connection is opened. This prevents security-sensitive information from being retrieved from an open connection object via the `ConnectionString` property.

Always keep this false unless you have a very specific diagnostic reason to enable it.

```
Persist Security Info=False;
```

---

**`Application Name` (string, default: `.Net SqlClient Data Provider`)**

Specifies the name of the application for diagnostic purposes. This appears in SQL Server traces, the `program_name` column of `sys.dm_exec_sessions`, and the `program_name` column of the SQL Profiler output.

Setting a meaningful application name makes SQL Server monitoring dramatically easier — you can see exactly which application is creating which connections.

```
Application Name=MyWebApp-API;
```

Caution: Because the application name is part of the connection string, different application names create different connection pools. If different parts of your application use different `Application Name` values, they will not share a pool. This is an intentional feature but can also be an unintentional source of pool fragmentation.

---

**`Workstation ID` (string, default: computer name)**

Identifies the client workstation in SQL Server traces. Useful for debugging in environments where multiple application servers connect to the same database — you can see which server is generating which load.

```
Workstation ID=WebServer01;
```

---

**`MultipleActiveResultSets` / `MARS` (bool, default: `false`)**

Enables Multiple Active Result Sets — the ability to execute multiple batches on a single connection simultaneously (e.g., reading from a `SqlDataReader` while also executing another command on the same connection). 

MARS is useful in specific scenarios but adds overhead to every connection when enabled. For most applications, the better design is to avoid needing MARS by using separate connections for separate operations (the pool makes this cheap). Only enable MARS if you have specific need for it.

```
MultipleActiveResultSets=True;
```

---

**`PoolBlockingPeriod` (enum: `Auto`, `AlwaysBlock`, `NeverBlock`, default: `Auto`)**

Controls the blocking period behavior described earlier. `Auto` is almost always correct: blocking is enabled for non-Azure SQL Server connections (protecting against retry storms during outages) and disabled for Azure SQL Database (which expects transient-aware clients).

```
; No need to set this in most cases — Auto is correct
PoolBlockingPeriod=Auto;
```

---

### 7.2 SQL Server-Side Settings That Interact with the Pool

There are SQL Server settings that interact with the client-side pool:

**`sp_configure 'user connections'`**

This configures the maximum number of concurrent connections to the SQL Server instance. The default of 0 means "use the system maximum" which is 32,767 on standard editions. You can reduce this to protect the server from being overwhelmed:

```sql
EXEC sp_configure 'show advanced options', 1;
RECONFIGURE;
EXEC sp_configure 'user connections', 500;
RECONFIGURE;
```

Setting this too low will cause connection errors in your application when the limit is reached, appearing as SQL Server login failures rather than pool exhaustion errors (because the rejection happens on the SQL Server side, before the client gets a connection to add to its pool).

**SQL Server's `max worker threads`**

As discussed in Part 1, this limits how many simultaneous query-executing connections SQL Server can support. Auto-configuration is generally correct for most servers, but very high pool sizes across multiple application servers can exhaust this resource.

---

## Part 8: Default Pool Size — When 100 Is Wrong

### 8.1 Why 100 Is the Default

The default `Max Pool Size=100` was chosen by Microsoft as a conservative value that is appropriate for most applications while providing some protection against accidental resource exhaustion. One hundred physical connections to SQL Server represent a significant but manageable load — approximately 2–3MB of memory on the SQL Server side (estimating 24KB per connection in ring buffers plus overhead), plus 100 worker threads.

For applications that handle dozens to hundreds of concurrent requests, with queries that complete in milliseconds and connections held for less than 5ms each, 100 connections are more than sufficient. The math: if each connection is held for an average of 5ms, in one second that connection can serve 200 requests. With 100 connections, you can serve 20,000 requests per second without any connection waiting. Most applications never come close to this.

### 8.2 When 100 Is Too Low — Signs and Symptoms

You need more than 100 connections when:

**Symptom 1: You see this exception in your logs:**
```
System.InvalidOperationException: Timeout expired. The timeout period elapsed prior to 
obtaining a connection from the pool. This may have occurred because all pooled connections 
were in use and max pool size was reached.
```

**Symptom 2: Your connection pool performance counters show:**
- `NumberOfPooledConnections` consistently at or near `Max Pool Size`
- `NumberOfStaleConnections` (connections that became invalid) is elevated

**Symptom 3: Queries that should complete in <10ms are taking 15+ seconds — this is often actually connection wait time, not query execution time.**

**Symptom 4: During load testing, response times remain good until a specific concurrency level and then degrade dramatically — a classic "cliff" pattern caused by pool exhaustion.**

Before raising `Max Pool Size`, investigate the root cause:

1. **Are connections being held too long?** Long-running queries, connections not disposed properly, or connections held during non-DB work will exhaust the pool regardless of size. Raising the pool size just delays the failure.

2. **Is there a connection leak?** Monitor `sys.dm_exec_sessions` over time. If the connection count grows monotonically without shrinking, you have a leak. Raising the pool size just takes longer to exhaust.

3. **Is there pool fragmentation?** Multiple different connection strings creating multiple pools, none of which individually hits the limit, but collectively overwhelming the server.

4. **Are queries slow?** A query that takes 1 second occupies a connection for 1 second. In a pool of 100, you can run only 100 concurrent slow queries. Fixing the query might solve the pool exhaustion without touching the pool size.

If after investigating you determine that the pool size genuinely needs to increase:

**Raising to 200–300:** Appropriate for medium-traffic APIs that have confirmed they are pool-exhausting during legitimate peak load, with quick queries and proper connection disposal. This range provides headroom without risking SQL Server overload on typical hardware.

**Raising to 300–500:** Appropriate for high-traffic applications running on robust SQL Server hardware (16+ cores, dedicated server, not Azure SQL Basic/Standard tier), with demonstrated evidence from load testing that the server can handle this connection count without worker thread starvation. Make sure `sp_configure 'max worker threads'` is appropriate for the SQL Server hardware.

**Going above 500:** Rarely appropriate. If you need this many connections from a single application instance, you likely have a design problem (long-held connections, N+1 queries, etc.). Consider horizontal scaling (multiple application instances) rather than a single giant pool.

**Going to 1000+:** Almost certainly wrong. This is either pool fragmentation (fix the connection strings), connection leaks (fix the Dispose pattern), or a fundamental application design issue requiring refactoring.

### 8.3 When 100 Is Too High — When to Lower It

Counterintuitively, there are scenarios where you want to lower `Max Pool Size` below 100:

**Scenario 1: Shared SQL Server with many application instances.**

Your organization runs 10 different applications, each with a pool of 100 connections. In aggregate, that's 1,000 connections pointing at the same SQL Server instance. With 576 worker threads available, 1,000 connections cannot all be actively executing queries simultaneously. If all 10 applications hit peak load at the same time, SQL Server is overwhelmed.

Solution: Size each application's pool proportionally to its importance and expected load. The critical billing service might get `Max Pool Size=50`, the low-priority reporting service gets `Max Pool Size=20`, the batch job gets `Max Pool Size=10`.

**Scenario 2: Azure SQL Database tier constraints.**

Azure SQL Database has per-tier connection limits. The Basic tier allows only 30 connections. The Standard S0 tier allows only 30. Standard S1 allows 100. Standard S2 allows 200. Standard S3–S12 allow various amounts. Premium P1 allows 410, P2 allows 820, and so on.

If your application is on a Basic tier Azure SQL Database and your pool is configured to 100, connections 31–100 will be rejected by Azure with "Database reached its concurrent connection limit." Your pool size must match or be less than the Azure tier limit.

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=tcp:myserver.database.windows.net,1433;Initial Catalog=mydb;User Id=myuser;Password=mypassword;Max Pool Size=25;Encrypt=True;TrustServerCertificate=False;"
  }
}
```

**Scenario 3: Microservices sharing a database (anti-pattern, but it exists).**

If 20 microservices all connect to the same SQL Server instance and each has a pool of 100, the aggregate is 2,000 connections. This is often a sign that the database should be split, but in the short term, lower pool sizes on less critical services can reduce aggregate connection pressure.

**Scenario 4: Development and testing environments.**

On a developer workstation, running SQL Server LocalDB or SQL Server Express with multiple running applications (your API + your admin tool + your test runner), large pool sizes waste resources. `Max Pool Size=20` or even `Max Pool Size=10` is entirely appropriate in development.

### 8.4 The Right Way to Determine Your Pool Size

The only reliable way to determine the correct pool size for your application is through load testing combined with connection monitoring. Here is the process:

**Step 1 — Establish a baseline.** Run your application under production-representative load (using a load testing tool like NBomber, k6, or JMeter targeting your specific endpoints). Capture: requests per second, response time percentiles (P50, P95, P99), error rate.

**Step 2 — Monitor the pool.** While the load test runs, query SQL Server:

```sql
-- See how many connections exist per application
SELECT 
    des.program_name AS ApplicationName,
    des.host_name AS ServerName,
    des.login_name AS LoginName,
    des.status AS SessionStatus,
    COUNT(dec.session_id) AS ConnectionCount
FROM sys.dm_exec_sessions des
JOIN sys.dm_exec_connections dec ON des.session_id = dec.session_id
WHERE des.is_user_process = 1
GROUP BY des.program_name, des.host_name, des.login_name, des.status
ORDER BY ConnectionCount DESC;
```

```sql
-- See currently active (executing) vs sleeping (idle in pool) connections
SELECT
    status,
    COUNT(*) AS Count
FROM sys.dm_exec_sessions
WHERE is_user_process = 1
GROUP BY status;
```

**Step 3 — Find the peak active connections.** The `status = 'running'` count during peak load tells you how many connections were genuinely being used simultaneously. Add 20–30% headroom for spikes. That is your `Max Pool Size`.

**Step 4 — Verify SQL Server health under this load.** Check `sys.dm_os_wait_stats` for high wait times on connection-related waits. Check CPU and memory. Check for THREADPOOL waits (a sign of worker thread exhaustion).

**Step 5 — Test the failure mode.** Reduce `Max Pool Size` to 80% of your measured peak and run the load test again. The application should degrade gracefully (slower responses, not crashes). Then test at 110% of peak to verify the error handling for pool exhaustion is working correctly.

### 8.5 A Formula for Initial Sizing

If you cannot run load tests initially, here is a rough heuristic for initial sizing:

```
Max Pool Size ≈ (Peak concurrent requests per server) × (Average connections per request) × 1.3
```

For a typical ASP.NET Core API:
- If you process 100 concurrent requests at peak, each using 1–2 database calls
- Each connection is held for ~5ms on average
- You need approximately 100–200 connections × 1.3 safety margin = 130–260 max

Starting at 150–200 and adjusting based on monitoring is a reasonable approach for a medium-traffic application.

For a high-traffic application handling 1,000 concurrent requests at peak, with proper connection minimization (batching, caching, etc.) reducing average connections per request to 0.5:
- 1,000 × 0.5 × 1.3 = 650 connections

But this is per application server instance. If you have four servers, that's 2,600 connections to SQL Server. You would need to verify your SQL Server can handle this, and potentially consider read replicas, caching, or connection brokering at that scale.

---

## Part 9: Monitoring, Diagnostics, and Observability

### 9.1 Performance Counters in Windows

`Microsoft.Data.SqlClient` (and the Framework version via `System.Data.SqlClient` on .NET Framework) exposes Windows Performance Monitor (PerfMon) counters that give real-time visibility into pool behavior.

The counters live under the `SqlClient: Connection Pool Groups` and `SqlClient: Connection Pools` categories. Key counters:

- **`NumberOfActiveConnectionPoolGroups`** — Number of unique connection strings that have pools
- **`NumberOfActiveConnectionPools`** — Number of actual pools (may differ from groups if integrated security creates multiple pools per group)
- **`NumberOfActiveConnections`** — Connections currently checked out (in use by application code)
- **`NumberOfFreeConnections`** — Connections available in the pool
- **`NumberOfStaleConnections`** — Connections removed from pool due to stale/dead state
- **`NumberOfPooledConnections`** — Total connections in the pool (active + free)
- **`HardConnectsPerSecond`** — Rate of new physical connections being established
- **`HardDisconnectsPerSecond`** — Rate of physical connections being closed
- **`SoftConnectsPerSecond`** — Rate of connections being checked out from the pool
- **`SoftDisconnectsPerSecond`** — Rate of connections being returned to the pool

High `HardConnectsPerSecond` relative to `SoftConnectsPerSecond` means the pool is frequently creating new physical connections rather than reusing pooled ones — this might indicate pool fragmentation, a small `Min Pool Size` with variable traffic, or a pool size that is too small and connections are being created and destroyed rapidly.

**Accessing performance counters in .NET 10:**

In .NET Core and later, performance counters are not accessible via the `PerformanceCounter` API (Windows-only). Instead, `Microsoft.Data.SqlClient` exposes metrics via `EventSource` (Microsoft.Data.SqlClient.EventSource), accessible via EventPipe and tools like dotnet-counters:

```bash
# Install dotnet-counters
dotnet tool install --global dotnet-counters

# Monitor SqlClient counters live
dotnet-counters monitor --process-id <PID> --counters Microsoft.Data.SqlClient.EventSource
```

Or in your application code, subscribe to EventSource:

```csharp
// In a background service or diagnostic utility
using System.Diagnostics.Tracing;

public class SqlClientEventListener : EventListener
{
    protected override void OnEventSourceCreated(EventSource eventSource)
    {
        if (eventSource.Name == "Microsoft.Data.SqlClient.EventSource")
        {
            EnableEvents(eventSource, EventLevel.Informational, 
                (EventKeywords)1); // 1 = Pooling keywords
        }
    }

    protected override void OnEventWritten(EventWrittenEventArgs eventData)
    {
        // Log or export eventData to your observability platform
        Console.WriteLine($"{eventData.EventName}: {string.Join(", ", eventData.Payload ?? Array.Empty<object>())}");
    }
}
```

### 9.2 Monitoring via SQL Server DMVs

The most direct view into connection pool health from the SQL Server side uses the Dynamic Management Views (DMVs):

```sql
-- Overall connection health: count by application and status
SELECT
    des.program_name          AS [Application],
    des.login_name            AS [Login],
    des.host_name             AS [Host],
    des.status                AS [Status],    -- 'running', 'sleeping', 'dormant'
    des.last_request_start_time,
    des.last_request_end_time,
    COUNT(des.session_id)     AS [SessionCount],
    SUM(des.reads)            AS [TotalReads],
    SUM(des.writes)           AS [TotalWrites],
    SUM(des.cpu_time)         AS [TotalCpuMs]
FROM sys.dm_exec_sessions des
WHERE des.is_user_process = 1
GROUP BY 
    des.program_name, 
    des.login_name, 
    des.host_name, 
    des.status,
    des.last_request_start_time,
    des.last_request_end_time
ORDER BY [SessionCount] DESC;
```

```sql
-- Find long-running, connection-holding queries
SELECT 
    des.session_id,
    des.status,
    des.login_name,
    des.host_name,
    des.program_name,
    der.command,
    der.wait_type,
    der.wait_time,
    der.blocking_session_id,
    der.cpu_time,
    der.reads,
    der.total_elapsed_time AS elapsed_ms,
    SUBSTRING(dest.text, 
              (der.statement_start_offset / 2) + 1, 
              ((CASE der.statement_end_offset 
                    WHEN -1 THEN DATALENGTH(dest.text) 
                    ELSE der.statement_end_offset 
                END - der.statement_start_offset) / 2) + 1) AS [CurrentSQL]
FROM sys.dm_exec_sessions des
JOIN sys.dm_exec_requests der ON des.session_id = der.session_id
CROSS APPLY sys.dm_exec_sql_text(der.sql_handle) dest
WHERE des.is_user_process = 1
  AND der.total_elapsed_time > 5000  -- Queries running more than 5 seconds
ORDER BY der.total_elapsed_time DESC;
```

```sql
-- Trend snapshot: store this periodically to track pool growth
SELECT
    GETUTCDATE() AS [SnapshotTime],
    des.program_name,
    COUNT(*) AS [TotalConnections],
    SUM(CASE WHEN des.status = 'running' THEN 1 ELSE 0 END) AS [ActiveConnections],
    SUM(CASE WHEN des.status = 'sleeping' THEN 1 ELSE 0 END) AS [IdleConnections]
FROM sys.dm_exec_sessions des
WHERE des.is_user_process = 1
GROUP BY des.program_name
ORDER BY TotalConnections DESC;
```

```sql
-- Check for connection accumulation (potential leaks)
-- Run this query every minute for 10 minutes during normal operation
-- and watch if the count grows monotonically
SELECT COUNT(*) AS [TotalUserConnections]
FROM sys.dm_exec_sessions
WHERE is_user_process = 1;
```

```sql
-- Performance counter for user connections (useful for baselining)
SELECT cntr_value AS [UserConnections]
FROM sys.dm_os_performance_counters
WHERE counter_name = 'User Connections';
```

### 9.3 OpenTelemetry Integration

In a modern .NET 10 application following OpenTelemetry standards, you should expose connection pool metrics to your observability stack. Here is how to integrate pool metrics with OpenTelemetry:

```csharp
// Program.cs
builder.Services.AddOpenTelemetry()
    .WithMetrics(metrics =>
    {
        metrics.AddAspNetCoreInstrumentation();
        metrics.AddRuntimeInstrumentation();
        metrics.AddMeter("Microsoft.Data.SqlClient.EventSource"); // SQL Client metrics
        
        // Custom pool metrics
        metrics.AddMeter("MyApp.Database");
        
        metrics.AddPrometheusExporter();
    })
    .WithTracing(tracing =>
    {
        tracing.AddAspNetCoreInstrumentation();
        tracing.AddSqlClientInstrumentation(options =>
        {
            options.SetDbStatementForText = true; // Include SQL in traces
            options.RecordException = true;
        });
        tracing.AddOtlpExporter();
    });
```

```csharp
// DatabaseHealthService.cs — background service that reports pool metrics
public class DatabaseHealthService : BackgroundService
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ILogger<DatabaseHealthService> _logger;
    private readonly Meter _meter;
    private readonly ObservableGauge<int> _activeConnectionsGauge;

    public DatabaseHealthService(
        IDbConnectionFactory connectionFactory, 
        ILogger<DatabaseHealthService> logger,
        IMeterFactory meterFactory)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
        _meter = meterFactory.Create("MyApp.Database");
        
        _activeConnectionsGauge = _meter.CreateObservableGauge(
            "db.connection_pool.active_connections",
            GetActiveConnectionCount,
            unit: "{connections}",
            description: "Number of active database connections in the pool");
    }

    private int GetActiveConnectionCount()
    {
        // This queries SQL Server for active session count
        // In a real implementation, you'd cache this and refresh periodically
        // to avoid creating connections just to count connections
        try
        {
            using var connection = new SqlConnection(/* your connection string */);
            connection.Open();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"
                SELECT COUNT(*) 
                FROM sys.dm_exec_sessions 
                WHERE is_user_process = 1 
                  AND program_name = @AppName";
            cmd.Parameters.AddWithValue("@AppName", "MyWebApp-API");
            return (int)cmd.ExecuteScalar()!;
        }
        catch
        {
            return -1; // Signal monitoring system that we couldn't measure
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            // The ObservableGauge callback fires on collection
        }
    }
}
```

### 9.4 Application-Level Health Checks

ASP.NET Core's health check middleware integrates well with connection pool monitoring:

```csharp
// Program.cs
builder.Services.AddHealthChecks()
    .AddSqlServer(
        connectionString: builder.Configuration.GetConnectionString("DefaultConnection")!,
        healthQuery: "SELECT 1",
        name: "sql-server",
        failureStatus: HealthStatus.Unhealthy,
        tags: new[] { "database", "sql" })
    .AddCheck<ConnectionPoolHealthCheck>("connection-pool");

// Connection pool health check
public class ConnectionPoolHealthCheck : IHealthCheck
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IConfiguration _configuration;

    public ConnectionPoolHealthCheck(
        IDbConnectionFactory connectionFactory, 
        IConfiguration configuration)
    {
        _connectionFactory = connectionFactory;
        _configuration = configuration;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var stopwatch = Stopwatch.StartNew();
            await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
            
            await using var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT 
                    COUNT(*) AS TotalSessions,
                    SUM(CASE WHEN status = 'running' THEN 1 ELSE 0 END) AS ActiveSessions,
                    SUM(CASE WHEN status = 'sleeping' THEN 1 ELSE 0 END) AS IdleSessions
                FROM sys.dm_exec_sessions
                WHERE is_user_process = 1
                  AND program_name = @AppName";
            command.Parameters.Add(new SqlParameter("@AppName", "MyWebApp-API"));
            
            stopwatch.Stop();

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            
            if (!await reader.ReadAsync(cancellationToken))
                return HealthCheckResult.Unhealthy("Could not read connection stats from SQL Server");

            var total = reader.GetInt32(0);
            var active = reader.GetInt32(1);
            var idle = reader.GetInt32(2);
            
            var maxPoolSize = 100; // Parse from config in production
            var utilizationPercent = total > 0 ? (active * 100) / maxPoolSize : 0;
            
            var data = new Dictionary<string, object>
            {
                ["total_sessions"] = total,
                ["active_sessions"] = active,
                ["idle_sessions"] = idle,
                ["pool_utilization_percent"] = utilizationPercent,
                ["check_duration_ms"] = stopwatch.ElapsedMilliseconds
            };

            if (utilizationPercent >= 90)
                return HealthCheckResult.Unhealthy(
                    $"Connection pool at {utilizationPercent}% capacity", data: data);
            
            if (utilizationPercent >= 70)
                return HealthCheckResult.Degraded(
                    $"Connection pool at {utilizationPercent}% capacity", data: data);
            
            return HealthCheckResult.Healthy(
                $"Connection pool healthy ({utilizationPercent}% utilized)", data: data);
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Failed to check connection pool", ex);
        }
    }
}
```

---

## Part 10: Resilience — Handling Pool Exhaustion and Transient Failures

### 10.1 Polly Integration for Retry Policies

Production applications must handle transient database failures gracefully. Connection pool exhaustion, transient network errors, and SQL Server failover events all produce exceptions that should trigger retries rather than immediate failures. The standard .NET library for this is Polly.

In .NET 8+, Polly's new resilience API is integrated directly into `Microsoft.Extensions.Http` and `Microsoft.Extensions.Resilience`:

```xml
<PackageReference Include="Polly.Core" Version="8.5.0" />
<PackageReference Include="Microsoft.Extensions.Resilience" Version="9.0.0" />
```

```csharp
// Program.cs
builder.Services.AddResiliencePipeline("database", pipeline =>
{
    pipeline
        .AddRetry(new RetryStrategyOptions
        {
            MaxRetryAttempts = 3,
            Delay = TimeSpan.FromSeconds(1),
            BackoffType = DelayBackoffType.Exponential,
            UseJitter = true,
            ShouldHandle = new PredicateBuilder()
                .Handle<SqlException>(ex => IsTransient(ex))
                .Handle<InvalidOperationException>(ex => 
                    ex.Message.Contains("pool") && ex.Message.Contains("timeout")),
            OnRetry = args =>
            {
                logger.LogWarning(
                    "Database retry attempt {Attempt} after {Delay}ms. Exception: {Exception}",
                    args.AttemptNumber + 1,
                    args.RetryDelay.TotalMilliseconds,
                    args.Outcome.Exception?.Message);
                
                // If we're retrying due to connection failure, clear the pool
                if (args.Outcome.Exception is SqlException sqlEx && IsConnectionFailure(sqlEx))
                {
                    SqlConnection.ClearAllPools();
                }
                
                return ValueTask.CompletedTask;
            }
        })
        .AddTimeout(TimeSpan.FromSeconds(30));
});

static bool IsTransient(SqlException ex)
{
    // Transient SQL Server error numbers
    var transientErrors = new HashSet<int>
    {
        -2,    // Timeout expired
        20,    // Instance unreachable
        64,    // Connection ended
        233,   // Client unable to establish connection
        10053, // Connection forcibly closed
        10054, // Connection reset
        10060, // Connection attempt failed
        40197, // Service error (Azure)
        40501, // Service busy (Azure)
        40613, // Database unavailable (Azure)
        49918, // Not enough resources
        49919, // Not enough resources to create or update
        49920  // Service busy
    };
    return transientErrors.Contains(ex.Number);
}

static bool IsConnectionFailure(SqlException ex)
{
    return ex.Number is 233 or 10053 or 10054 or 10060 or 64;
}
```

### 10.2 Enabling Retry in Entity Framework Core

EF Core has built-in retry support that is more convenient than custom Polly policies for EF-managed database access:

```csharp
builder.Services.AddDbContextPool<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions =>
        {
            sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 3,
                maxRetryDelay: TimeSpan.FromSeconds(10),
                errorNumbersToAdd: new[] { 4060, 40197, 40501, 40613, 49918, 49919, 49920 });
        }));
```

EF Core's retry strategy already handles the standard set of transient SQL Server errors. The `errorNumbersToAdd` parameter lets you add custom error numbers (useful for Azure SQL specific errors).

**Important limitation:** EF Core's retry on failure does not work with user-initiated transactions. If you use `context.Database.BeginTransaction()`, the retry strategy is disabled for that transaction scope. This is intentional — EF cannot safely retry operations that include user-managed transactions because it doesn't know which operations to roll back and retry. For transactional scenarios that need retry, use the execution strategy explicitly:

```csharp
var strategy = _context.Database.CreateExecutionStrategy();
await strategy.ExecuteAsync(async () =>
{
    await using var transaction = await _context.Database.BeginTransactionAsync();
    try
    {
        // Your transactional work
        await _context.SaveChangesAsync();
        await transaction.CommitAsync();
    }
    catch
    {
        await transaction.RollbackAsync();
        throw;
    }
});
```

### 10.3 Circuit Breakers for Database Connections

For highly resilient applications, a circuit breaker pattern prevents cascading failures when the database is completely unavailable. Instead of letting all requests attempt to connect (and wait for the 15-second timeout, potentially piling up thousands of waiting requests), the circuit breaker "opens" after a threshold of failures and immediately rejects requests until a probe succeeds.

```csharp
// Program.cs
builder.Services.AddResiliencePipeline("database", pipeline =>
{
    pipeline
        .AddCircuitBreaker(new CircuitBreakerStrategyOptions
        {
            FailureRatio = 0.5,           // Open when 50% of calls fail
            SamplingDuration = TimeSpan.FromSeconds(30),
            MinimumThroughput = 10,        // Need at least 10 calls to evaluate
            BreakDuration = TimeSpan.FromSeconds(15), // Stay open for 15 seconds
            ShouldHandle = new PredicateBuilder()
                .Handle<SqlException>(ex => IsTransient(ex))
                .Handle<InvalidOperationException>(),
            OnOpened = args =>
            {
                logger.LogError(
                    "Database circuit breaker OPENED. Blocking requests for {Duration}",
                    args.BreakDuration);
                return ValueTask.CompletedTask;
            },
            OnClosed = args =>
            {
                logger.LogInformation("Database circuit breaker CLOSED. Resuming requests.");
                return ValueTask.CompletedTask;
            }
        })
        .AddRetry(new RetryStrategyOptions
        {
            MaxRetryAttempts = 3,
            Delay = TimeSpan.FromMilliseconds(500),
            BackoffType = DelayBackoffType.Exponential,
            UseJitter = true,
            ShouldHandle = new PredicateBuilder().Handle<SqlException>(IsTransient)
        });
});
```

---

## Part 11: Advanced Topics — Transactions, Distributed Systems, and Special Scenarios

### 11.1 TransactionScope and the Connection Pool

`System.Transactions.TransactionScope` is the .NET API for creating ambient transactions that automatically enlist connections. It is commonly used in service layer code to ensure that multiple database operations either all succeed or all fail together.

The interaction between `TransactionScope` and the connection pool is nuanced and a source of common bugs.

When you open a connection inside a `TransactionScope`, the connection is enlisted in the ambient transaction (unless `Enlist=False` in the connection string). Connections enlisted in a transaction are quarantined in the pool — they can only be reused by code running in the same transaction context. This means:

1. If you have 10 concurrent requests each using a `TransactionScope`, each needing 3 connections, you need 30 connections just for transactions — and those connections are not available to non-transactional code during the transaction's lifetime.

2. If two operations in the same `TransactionScope` use the same connection string, they get the same pooled connection (same SPID on SQL Server), because the pool recognizes they are in the same transaction.

3. If two operations in the same `TransactionScope` use different connection strings, two connections are enlisted in the same transaction — this automatically escalates to a distributed transaction (coordinated by the Microsoft Distributed Transaction Coordinator, MSDTC), which is significantly more expensive.

```csharp
// ❌ This accidentally creates a distributed transaction
using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
{
    // Connection 1 — to MyDatabase1
    using (var conn1 = new SqlConnection("Server=sql01;Database=Db1;..."))
    {
        conn1.Open(); // Enlists in ambient transaction
        // Do work
    }
    
    // Connection 2 — to MyDatabase2 (different database!)
    using (var conn2 = new SqlConnection("Server=sql01;Database=Db2;..."))
    {
        conn2.Open(); // Second connection enlists — escalates to MSDTC!
        // Do work
    }
    
    scope.Complete();
}
```

The escalation to MSDTC requires MSDTC to be running and configured on both the application server and SQL Server, and it is dramatically slower than local transactions. Worse, MSDTC can be blocked by firewalls in many corporate network configurations.

The modern alternative: avoid `TransactionScope` for multi-database operations and use explicit transactions on a single connection, or design your system to use sagas/event sourcing for cross-database consistency.

### 11.2 Always On Availability Groups and Connection Resiliency

SQL Server Always On Availability Groups provide high availability through automatic failover. When the primary replica fails, one of the secondary replicas is promoted to primary. From the connection pooling perspective, this creates a challenge: all existing connections in the pool are now connected to a dead server.

The pool's dead connection detection (checking for broken connections after approximately 4–8 minutes of idle time) is too slow for most failover scenarios. You need active detection and pool clearing.

The `MultiSubnetFailover=True` connection string parameter is designed for this scenario:

```
Server=sql-ag-listener;Database=AppDb;Integrated Security=True;MultiSubnetFailover=True;
```

When `MultiSubnetFailover=True`:
- `Microsoft.Data.SqlClient` sends login requests to all IP addresses in the DNS response for the AG listener simultaneously (rather than sequentially)
- This dramatically reduces failover detection time from potentially minutes to seconds
- The timeout for establishing a connection is 21 seconds when using `MultiSubnetFailover=True`

Combined with pool clearing in your retry policy:

```csharp
catch (SqlException ex) when (ex.Number is 10054 or 10053 or 233 or 64 or -2)
{
    logger.LogWarning("Connection failure detected, clearing pool and retrying");
    SqlConnection.ClearAllPools(); // Force reconnection to new primary
    await Task.Delay(TimeSpan.FromSeconds(2)); // Brief wait for DNS to update
    // Retry the operation
}
```

### 11.3 Async/Await and the Thread Pool — An Often Misunderstood Interaction

In ASP.NET Core on .NET 10, database access should always use async methods (`OpenAsync`, `ExecuteReaderAsync`, `ReadAsync`, etc.). This is not just about "being modern" — it has direct implications for connection pool efficiency.

When you use synchronous database methods in an ASP.NET Core application, the calling thread is blocked while waiting for the database response. A blocked thread cannot serve other requests. With 100 threads in the thread pool and 100 slow synchronous queries running, your server is completely stalled even if the connection pool has available connections.

With async methods, the thread is released back to the thread pool while waiting for the I/O response from SQL Server. The same 100 threads can serve thousands of concurrent requests because threads are only consumed during actual CPU work, not during I/O waits.

The interaction with connection pooling: a connection can be "checked out" (not available in the pool) while an async await is in progress. The connection is not returned to the pool until `Dispose()` is called, regardless of whether there is a thread running. So if you have 100 connections each awaiting a database response, all 100 connections are occupied — even though no threads are blocked. This is why pool size and thread pool size need to be considered separately.

The most dangerous pattern combining these two issues:

```csharp
// ❌ Synchronous blocking on async — stalls both thread and holds connection
public IActionResult GetData(int id)
{
    // .Result blocks the current thread
    var result = _repository.GetByIdAsync(id).Result; 
    // This thread is now blocked for the duration of the database call
    // AND the connection is checked out
    // Under load, you exhaust both the thread pool AND the connection pool
    return Ok(result);
}
```

Always use `async` and `await` in ASP.NET Core controllers:

```csharp
// ✅ Correct
[HttpGet("{id}")]
public async Task<IActionResult> GetData(int id, CancellationToken cancellationToken)
{
    var result = await _repository.GetByIdAsync(id, cancellationToken);
    return result is null ? NotFound() : Ok(result);
}
```

### 11.4 CancellationToken Propagation

Proper `CancellationToken` propagation is essential for connection pool health. When an HTTP request is cancelled (client disconnects, request timeout), ASP.NET Core cancels the `CancellationToken`. If your database code respects this token, the in-progress database command is cancelled and the connection is promptly returned to the pool. If your code ignores the token, the database command runs to completion even though no one is waiting for the result, keeping the connection occupied.

```csharp
// Always propagate CancellationToken through the call chain
[HttpGet("{id}")]
public async Task<IActionResult> GetProduct(int id, CancellationToken cancellationToken)
{
    // cancellationToken is provided by ASP.NET Core's request cancellation
    var product = await _repository.GetByIdAsync(id, cancellationToken);
    return product is null ? NotFound() : Ok(product);
}

// Repository propagates it to the ADO.NET calls
public async Task<Product?> GetByIdAsync(int id, CancellationToken cancellationToken)
{
    await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
    
    await using var command = connection.CreateCommand();
    command.CommandText = "SELECT * FROM Products WHERE Id = @Id";
    command.Parameters.AddWithValue("@Id", id);
    
    // Pass cancellationToken to all async operations
    await using var reader = await command.ExecuteReaderAsync(
        CommandBehavior.SingleRow, 
        cancellationToken); // ← This will cancel the in-flight query if requested
    
    if (!await reader.ReadAsync(cancellationToken))
        return null;
    
    return MapProduct(reader);
}
```

When `cancellationToken` is cancelled while the `ExecuteReaderAsync` is awaiting a response from SQL Server, the driver sends a cancel request to SQL Server (TDS Cancel packet), SQL Server stops processing the query, and the connection is returned to the pool promptly. This makes connection pools much more resilient to slow queries during high-load periods.

---

## Part 12: Case Studies and War Stories

### 12.1 Case Study: The E-Commerce Flash Sale (The Story from the Prologue)

Let's revisit our Thursday afternoon flash sale. Here is the full post-mortem analysis.

**The application:** An ASP.NET MVC application running on .NET Framework 4.8, deployed on IIS, connecting to SQL Server 2019 via `System.Data.SqlClient`. The application had been in production for 3 years. Connection pool settings: all defaults (`Max Pool Size=100`, `Min Pool Size=0`, `Connection Timeout=15`).

**The load:** Normal peak traffic was approximately 200 concurrent users. The flash sale brought 800 concurrent users — 4x the normal peak.

**The failure chain:**

1. Traffic spikes to 4x normal. The application's thread pool grows to handle concurrent requests. Each request needs a database connection for product lookups, inventory checks, and cart operations.

2. With 800 concurrent users each making 2–3 database queries per page load, the application needs 1,600–2,400 connection checkouts per second. Most complete in 5–10ms (SQL queries are fast), so the pool of 100 connections is sufficient for many requests — connection checkout time averages 2ms.

3. However, three specific code paths are the problem. The order placement endpoint includes a long-running transaction that locks inventory rows for up to 2 seconds (the SQL Server is processing payment authorization synchronously inside the transaction). With 50 concurrent order placements in progress, each holding a connection for 2 seconds, that's 50 connections occupied simultaneously — half the pool — just for this one endpoint.

4. The product catalog page includes a poorly optimized stored procedure that occasionally takes 30 seconds on certain products (a missing index on a rarely-searched category). When multiple users search for these products simultaneously, 10–20 connections are occupied for 30 seconds.

5. The pool exhausts. The queue of waiting requests grows. Each waiter holds a thread for up to 15 seconds (the Connection Timeout). The ASP.NET thread pool grows, consuming memory. The system starts garbage-collecting more frequently under memory pressure. This causes additional latency. The spiral accelerates.

**The immediate fix applied during the incident:** `Max Pool Size=200` in the connection string. This bought 15 minutes of stability before the same pattern repeated.

**The real fix (implemented over the following week):**

1. The inventory locking was redesigned to use optimistic concurrency (no long-held locks). Connection hold time for order placement dropped from 2 seconds to 20ms.

2. The missing index was identified via `sys.dm_db_missing_index_details` and added. The 30-second queries dropped to 50ms.

3. `Min Pool Size=20` was set to ensure 20 connections are always warm, preventing cold-start latency for burst traffic.

4. `Max Pool Size` was set back to 100 — sufficient now that connections are held for milliseconds rather than seconds.

5. A health check endpoint was added that monitors pool utilization and pages on-call if utilization exceeds 80%.

**The lesson:** The solution to connection pool exhaustion is almost never "add more connections." Find out why connections are held so long. Fix the root cause. Add monitoring so you know immediately when the pattern recurs.

### 12.2 Case Study: The Multi-Tenant SaaS Application

**The application:** An ASP.NET Core API serving 50 enterprise customers, each with their own SQL Server database in a multi-tenant architecture. The application uses dynamic connection strings based on the current tenant's ID.

**The problem:** Each tenant's connection string is unique (different database name). Even though all databases are on the same SQL Server instance, the pool creates a separate pool for each connection string. With 50 tenants, there are 50 separate pools, each with a default max of 100 connections. The total theoretical maximum: 5,000 connections to a SQL Server that has 576 worker threads.

**The symptom:** During peak business hours, when all 50 tenants are simultaneously active, SQL Server's worker thread count reaches 400–500. CPU spikes. Queries slow down. Some connections start timing out not because the pool is exhausted, but because SQL Server is thread-starved and taking 10+ seconds to process simple queries.

**The fix:**

1. `Max Pool Size=20` per tenant connection string. With 50 tenants, maximum aggregate connections = 1,000. Much more manageable.

2. A middleware was added to standardize tenant connection strings — ensuring all non-tenant-specific options (timeout, encrypt, etc.) are identical, preventing unintentional fragmentation.

3. A connection string cache was implemented to ensure the same `SqlConnectionStringBuilder` result is returned for the same tenant, guaranteeing identical string representation and thus pool sharing within each tenant.

4. For inactive tenants (those not logged in for >30 minutes), `SqlConnection.ClearPool()` is called to free their connections rather than holding them in idle pools.

```csharp
// TenantConnectionFactory.cs
public class TenantConnectionFactory
{
    private readonly string _templateConnectionString;
    private readonly ConcurrentDictionary<int, string> _connectionStringCache = new();
    private readonly SqlConnectionStringBuilder _template;

    public TenantConnectionFactory(IConfiguration configuration)
    {
        _templateConnectionString = configuration.GetConnectionString("TenantTemplate")!;
        _template = new SqlConnectionStringBuilder(_templateConnectionString);
    }

    public string GetConnectionString(int tenantId, string databaseName)
    {
        return _connectionStringCache.GetOrAdd(tenantId, _ =>
        {
            var builder = new SqlConnectionStringBuilder(_templateConnectionString)
            {
                InitialCatalog = databaseName,
                // Tenant-specific pool sizing
                MaxPoolSize = 20,
                MinPoolSize = 2,
                ApplicationName = $"MyApp-Tenant{tenantId}"
            };
            return builder.ConnectionString;
        });
    }

    public async Task ReleaseTenantConnectionsAsync(int tenantId, string databaseName)
    {
        var connectionString = GetConnectionString(tenantId, databaseName);
        using var connection = new SqlConnection(connectionString);
        SqlConnection.ClearPool(connection);
        _connectionStringCache.TryRemove(tenantId, out _);
    }
}
```

### 12.3 Case Study: The Microservices Architecture Surprise

**The application:** A microservices architecture with 12 services, each running as a separate .NET 10 ASP.NET Core application in Kubernetes, all connecting to the same SQL Server instance. Each service has a pool of 100 connections (default).

**The math:** 12 services × 100 connections = 1,200 possible connections. With 3 replicas per service (3 Kubernetes pods), that's 12 × 3 × 100 = 3,600 possible connections. SQL Server is on a 16-core machine with approximately 900 worker threads.

During a traffic spike where all services scale to 5 replicas: 12 × 5 × 100 = 6,000 possible connections. SQL Server is catastrophically overloaded.

**The fix:** Each service's pool size was set based on its database access pattern:
- Order service (high frequency, fast queries): `Max Pool Size=50`
- Reporting service (low frequency, slow queries): `Max Pool Size=15`
- Authentication service (moderate frequency): `Max Pool Size=30`
- Notification service (bursty, light queries): `Max Pool Size=25`
- 8 other services: `Max Pool Size=15` each

Total: 50 + 15 + 30 + 25 + (8×15) = 240 connections per replica set. With 5 replicas per service, maximum = 1,200. SQL Server handles this comfortably.

Additionally, a read-only secondary replica was configured in the Always On AG, and reporting queries were redirected there using `ApplicationIntent=ReadOnly` in the connection string:

```
Server=sql-ag-listener;Database=AppDb;Integrated Security=True;MultiSubnetFailover=True;ApplicationIntent=ReadOnly;
```

This halved the connection load on the primary replica and dramatically improved reporting query performance (since secondary replicas use snapshot isolation by default).

---

## Part 13: Connection Pooling with Raw ADO.NET — The Complete Pattern Library

### 13.1 Stored Procedure Execution with Output Parameters

```csharp
public async Task<(int OrderId, DateTime EstimatedDelivery)> PlaceOrderAsync(
    PlaceOrderRequest request,
    CancellationToken cancellationToken = default)
{
    await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
    await using var command = connection.CreateCommand();
    
    command.CommandText = "usp_PlaceOrder";
    command.CommandType = CommandType.StoredProcedure;
    command.CommandTimeout = 30;
    
    command.Parameters.Add(new SqlParameter("@CustomerId", SqlDbType.Int) { Value = request.CustomerId });
    command.Parameters.Add(new SqlParameter("@ProductId", SqlDbType.Int) { Value = request.ProductId });
    command.Parameters.Add(new SqlParameter("@Quantity", SqlDbType.Int) { Value = request.Quantity });
    
    // Output parameters
    var orderIdParam = new SqlParameter("@OrderId", SqlDbType.Int)
        { Direction = ParameterDirection.Output };
    var deliveryParam = new SqlParameter("@EstimatedDelivery", SqlDbType.DateTime2)
        { Direction = ParameterDirection.Output };
    
    command.Parameters.Add(orderIdParam);
    command.Parameters.Add(deliveryParam);
    
    await command.ExecuteNonQueryAsync(cancellationToken);
    
    return (
        (int)orderIdParam.Value, 
        (DateTime)deliveryParam.Value
    );
    // Connection returned to pool here
}
```

### 13.2 Bulk Insert with SqlBulkCopy

`SqlBulkCopy` is the most efficient way to insert large numbers of rows into SQL Server. It uses the TDS BULK INSERT mechanism to bypass row-by-row processing. Importantly, it uses the same connection pool:

```csharp
public async Task BulkInsertProductsAsync(
    IEnumerable<Product> products,
    CancellationToken cancellationToken = default)
{
    await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
    
    // Begin transaction for the bulk insert
    await using var transaction = await connection.BeginTransactionAsync(
        IsolationLevel.ReadCommitted, 
        cancellationToken);
    
    try
    {
        using var bulkCopy = new SqlBulkCopy(connection, SqlBulkCopyOptions.Default, transaction)
        {
            DestinationTableName = "Products",
            BatchSize = 1000,           // Commit every 1000 rows
            BulkCopyTimeout = 600,      // 10 minute timeout for large datasets
            EnableStreaming = true       // Stream data rather than buffer everything
        };
        
        // Map source columns to destination columns
        bulkCopy.ColumnMappings.Add("Name", "Name");
        bulkCopy.ColumnMappings.Add("Price", "Price");
        bulkCopy.ColumnMappings.Add("CategoryId", "CategoryId");
        bulkCopy.ColumnMappings.Add("IsActive", "IsActive");
        
        // Convert products to DataTable
        var table = ProductsToDataTable(products);
        
        await bulkCopy.WriteToServerAsync(table, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }
    catch
    {
        await transaction.RollbackAsync(cancellationToken);
        throw;
    }
    // Connection held for the duration of the bulk insert — this is expected
    // For a 100,000 row insert, this might be 5–30 seconds
    // Size your pool accordingly if bulk inserts happen concurrently
}

private DataTable ProductsToDataTable(IEnumerable<Product> products)
{
    var table = new DataTable();
    table.Columns.Add("Name", typeof(string));
    table.Columns.Add("Price", typeof(decimal));
    table.Columns.Add("CategoryId", typeof(int));
    table.Columns.Add("IsActive", typeof(bool));
    
    foreach (var p in products)
    {
        table.Rows.Add(p.Name, p.Price, p.CategoryId, p.IsActive);
    }
    
    return table;
}
```

Note that `SqlBulkCopy` holds the connection for the entire duration of the copy. A 100,000 row bulk insert might take 10–30 seconds. During this time, that connection is checked out from the pool. If you run multiple concurrent bulk inserts, your pool will be depleted. Either increase `Max Pool Size` for the connection string used for bulk operations, or use a dedicated connection string specifically for bulk operations with a separate, smaller pool.

### 13.3 Change Data Capture and Long-Lived Connections

Some patterns inherently require long-lived connections: SQL Server's Change Data Capture (CDC) polling, Service Broker message processing, and WAITFOR-based notifications. These should never use connections from the shared application pool.

Instead, create a dedicated connection string for these long-lived connections with `Pooling=False`:

```csharp
public class CdcPollingService : BackgroundService
{
    private readonly string _cdcConnectionString;

    public CdcPollingService(IConfiguration configuration)
    {
        var baseString = configuration.GetConnectionString("DefaultConnection")!;
        var builder = new SqlConnectionStringBuilder(baseString)
        {
            Pooling = false,          // Do NOT put this connection in the shared pool
            ApplicationName = "CDC-Poller",
            CommandTimeout = 60
        };
        _cdcConnectionString = builder.ConnectionString;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // This connection lives for the lifetime of the background service
        // It should NOT be in the shared pool
        await using var connection = new SqlConnection(_cdcConnectionString);
        await connection.OpenAsync(stoppingToken);
        
        while (!stoppingToken.IsCancellationRequested)
        {
            await PollCdcChangesAsync(connection, stoppingToken);
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }
    
    private async Task PollCdcChangesAsync(SqlConnection connection, CancellationToken cancellationToken)
    {
        // Query CDC tables and process changes
        // Uses the long-lived connection without tying up the pool
    }
}
```

---

## Part 14: Security Considerations

### 14.1 Connection String Security

Connection strings frequently contain credentials. Never:

- Commit connection strings with credentials to source control
- Log connection strings (they may appear in exception messages — be careful about exception logging middleware)
- Pass connection strings as query parameters or include them in URLs
- Store them in `appsettings.json` that ships with the application binary

The correct approach in production is to use managed identity (for Azure deployments), secrets management (Azure Key Vault, HashiCorp Vault, AWS Secrets Manager), or environment variables:

```csharp
// Program.cs — Connection string from environment variable
var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING")
    ?? builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("No connection string configured");
```

For Azure SQL Database with managed identity (no password in connection string):

```csharp
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions =>
        {
            sqlOptions.UseAzureAdTokenAuthentication(); // Managed identity
        }));
```

With managed identity, the connection string contains no credentials:

```
Server=tcp:myserver.database.windows.net,1433;Database=mydb;Authentication=Active Directory Managed Identity;
```

### 14.2 Principle of Least Privilege for Pool Connections

Since connection pooling means all requests using the same connection string share the same physical connections (and the same SQL Server login), the database account used by the pool should have the minimum permissions needed:

- `SELECT`, `INSERT`, `UPDATE`, `DELETE` on the tables your application uses
- `EXECUTE` on stored procedures
- Never `db_owner` or `sysadmin`
- Never `sa` — this is the single most common security misconfiguration in production databases

```sql
-- Create a dedicated application login
CREATE LOGIN AppServiceAccount WITH PASSWORD = 'strong-random-password-here';

-- Create a database user mapped to the login  
USE MyAppDb;
CREATE USER AppServiceAccount FOR LOGIN AppServiceAccount;

-- Grant only what is needed
GRANT SELECT, INSERT, UPDATE, DELETE ON SCHEMA::dbo TO AppServiceAccount;
GRANT EXECUTE ON SCHEMA::dbo TO AppServiceAccount;

-- Deny dangerous permissions explicitly
DENY ALTER ANY DATABASE TO AppServiceAccount;
DENY CREATE TABLE TO AppServiceAccount;
DENY ALTER ANY TABLE TO AppServiceAccount;
```

### 14.3 SQL Injection and the Pool — Why Parameterization Is Non-Negotiable

SQL injection is unrelated to connection pooling, but the combination is particularly dangerous: a successful SQL injection attack through a pooled connection can compromise all data visible to the pool's SQL login. Since your pool might use `db_owner` or a similar high-privilege account (it shouldn't, but often does), a single injection vulnerability can be catastrophic.

Always use parameterized queries:

```csharp
// ❌ CATASTROPHICALLY WRONG — SQL injection vulnerability
command.CommandText = $"SELECT * FROM Users WHERE Email = '{userInput}'";

// ✅ Correct — parameterized
command.CommandText = "SELECT * FROM Users WHERE Email = @Email";
command.Parameters.Add(new SqlParameter("@Email", SqlDbType.NVarChar, 500) { Value = userInput });
```

Dapper and EF Core both handle parameterization automatically when you use their query APIs correctly. Raw string interpolation into SQL with either library is dangerous:

```csharp
// ❌ Dapper with string interpolation — SQL injection!
var users = await connection.QueryAsync<User>($"SELECT * FROM Users WHERE Email = '{email}'");

// ✅ Dapper with parameters — safe
var users = await connection.QueryAsync<User>(
    "SELECT * FROM Users WHERE Email = @Email", 
    new { Email = email });
```

---

## Part 15: Best Practices Summary and Checklists

### 15.1 The Connection Pool Configuration Checklist

Use this checklist for every ASP.NET application that connects to SQL Server:

**Connection String:**
- [ ] Connection string stored in a secrets manager, environment variable, or `appsettings.{Environment}.json` — never in source control with credentials
- [ ] Single canonical connection string used throughout the application (prevents pool fragmentation)
- [ ] `Application Name` set to a meaningful value for SQL Server diagnostic visibility
- [ ] `Min Pool Size` set to a small positive number (5–20) for warm pool on startup
- [ ] `Max Pool Size` sized based on actual load testing data, not guesswork
- [ ] `Connect Timeout` explicitly set (don't rely on the default changing between versions)
- [ ] `Encrypt=True` and `TrustServerCertificate=False` for production
- [ ] `MultiSubnetFailover=True` if connecting to an Always On Availability Group listener
- [ ] `Persist Security Info=False` (the default, but confirm it)

**Code Quality:**
- [ ] Every `SqlConnection` wrapped in `using` or `await using`
- [ ] No `SqlConnection` stored as a class field (especially not static)
- [ ] No connections opened before slow operations (API calls, file I/O, heavy computation)
- [ ] All database methods are `async` and accept `CancellationToken`
- [ ] `CancellationToken` propagated to `OpenAsync`, `ExecuteReaderAsync`, `ReadAsync`
- [ ] Transactions disposed properly with try/catch/finally
- [ ] No synchronous `.Wait()` or `.Result` blocking on async database calls
- [ ] `CommandBehavior.CloseConnection` used when returning `SqlDataReader` outside the using block

**Entity Framework Core (if used):**
- [ ] `AddDbContextPool` instead of `AddDbContext` for high-throughput scenarios
- [ ] `AsNoTracking()` on all read-only queries
- [ ] Retry on failure configured (`EnableRetryOnFailure`)
- [ ] No Scoped services injected into pooled `DbContext`

**Monitoring:**
- [ ] Pool utilization monitored via performance counters or EventSource
- [ ] SQL Server DMV queries run regularly during load testing
- [ ] Alerting configured for pool exhaustion errors
- [ ] Health check endpoint includes connection pool status

### 15.2 The Debugging Checklist When You Have Pool Problems

If you are seeing connection timeout errors in production:

1. **Check SQL Server:** `SELECT COUNT(*) FROM sys.dm_exec_sessions WHERE is_user_process = 1` — is the total connection count at or near your Max Pool Size?

2. **Check for leaks:** Run the session count query every minute for 10 minutes. Is it growing? Yes = connection leak. Fix with `using`/`await using` everywhere.

3. **Check for fragmentation:** `SELECT DISTINCT program_name, COUNT(*) FROM sys.dm_exec_sessions WHERE is_user_process = 1 GROUP BY program_name` — are there many different program names or unexpected ones? Each unique connection string creates a separate pool.

4. **Check for long-running connections:** `SELECT * FROM sys.dm_exec_sessions WHERE is_user_process = 1 AND status = 'sleeping' AND last_request_end_time < DATEADD(minute, -5, GETUTCDATE())` — connections idle for more than 5 minutes may not be returned to the pool properly.

5. **Check query duration:** `SELECT total_elapsed_time, text FROM sys.dm_exec_requests CROSS APPLY sys.dm_exec_sql_text(sql_handle) WHERE total_elapsed_time > 5000 ORDER BY total_elapsed_time DESC` — long queries hold connections. Fix the query.

6. **Check for blocking:** `SELECT blocking_session_id, session_id, wait_time, wait_type FROM sys.dm_exec_requests WHERE blocking_session_id > 0` — blocked queries hold connections while waiting. This cascades.

7. **Review error logs for the exact error message:** Pool exhaustion, connection failure, timeout during connection — each has different root causes and different fixes.

---

## Part 16: Looking Forward — Connection Pooling in .NET 10 and Beyond

### 16.1 .NET 10 Improvements

.NET 10 (released in November 2025) continues the theme of performance improvements that have characterized each .NET release since .NET 5. Relevant improvements for connection pooling:

- **Improved async I/O:** Further refinements to `ValueTask` and async state machine generation reduce the overhead of async database calls, particularly for short-lived operations.
- **Better GC:** .NET 10's garbage collector improvements reduce pauses that could temporarily stall connection return to the pool.
- **`Microsoft.Data.SqlClient` 7.0:** Released alongside .NET 10 support, includes the removal of Azure dependencies from the core package, allowing lean deployments without Azure SDK binaries. Connection pooling behavior is unchanged but the package is more maintainable.
- **Source-generated interceptors in EF Core 9+:** Compile-time code generation for EF interceptors, reducing reflection overhead for DbContext initialization (relevant to DbContext pool warm-up time).

### 16.2 Future Direction: Connection Multiplexing

A recurring topic in the ADO.NET ecosystem is connection multiplexing — the ability to share a single physical TCP connection for multiple simultaneous queries (similar to HTTP/2's stream multiplexing). This would allow a pool of 10 physical connections to serve 100 concurrent queries without the overhead of 100 separate TCP sockets.

The challenge: SQL Server's TDS protocol was not designed for connection multiplexing. MARS (Multiple Active Result Sets) allows some level of multiplexing on a single connection, but it requires careful management and doesn't eliminate the thread-per-connection model on the SQL Server side.

PostgreSQL has better support for connection multiplexing, and the `Npgsql` library for .NET supports it via PgBouncer integration or its own built-in multiplexing mode. SQL Server may gain similar capabilities in future versions.

### 16.3 Cloud-Native Considerations — Azure SQL Hyperscale and Serverless

**Azure SQL Serverless:** Scales compute up and down automatically, pausing when idle. During a pause, the database is inaccessible. When it resumes, connections in the pool may be stale. Configure `Connection Lifetime` to periodically refresh pooled connections, and implement retry logic for the "database paused and resuming" error.

**Azure SQL Hyperscale:** Supports more connections than standard tiers, with read replicas providing additional connection capacity. Use `ApplicationIntent=ReadOnly` to route read queries to replicas, reducing connection pressure on the primary.

**Azure SQL Always Serverless (new in 2025):** Uses per-request billing and scales connections dynamically. Connection pool behavior is important here — idle connections in your pool keep the Serverless database "warm" (which costs money). Balance `Min Pool Size` against cost by setting it lower for cost-sensitive environments.

---

## Resources and Further Reading

**Official Microsoft Documentation:**
- [SQL Server Connection Pooling (ADO.NET)](https://learn.microsoft.com/en-us/dotnet/framework/data/adonet/sql-server-connection-pooling) — The canonical reference
- [Introduction to Microsoft.Data.SqlClient](https://learn.microsoft.com/en-us/sql/connect/ado-net/introduction-microsoft-data-sqlclient-namespace) — Migration guide and feature overview
- [EF Core Performance — Advanced Topics](https://learn.microsoft.com/en-us/ef/core/performance/advanced-performance-topics) — DbContext pooling, compiled queries, tracking behavior
- [sys.dm_exec_sessions (Transact-SQL)](https://learn.microsoft.com/en-us/sql/relational-databases/system-dynamic-management-views/sys-dm-exec-sessions-transact-sql) — DMV reference

**Community Resources:**
- [Dapper on GitHub](https://github.com/DapperLib/Dapper) — Source, issues, and documentation
- [Microsoft.Data.SqlClient on GitHub](https://github.com/dotnet/SqlClient) — Driver source, bugs, and migration cheat sheet
- [Microsoft.Data.SqlClient Migration Cheat Sheet](https://github.com/dotnet/SqlClient/blob/main/porting-cheatsheet.md) — Essential for migrations from System.Data.SqlClient
- [Polly Resilience Library](https://github.com/App-vNext/Polly) — Retry and circuit breaker patterns for .NET
- [NBomber](https://nbomber.com/) — Modern load testing framework for .NET

**Recommended Books:**
- *Pro .NET Performance* by Sasha Goldshtein, Dima Zurbalev, and Ido Flatow — Deep performance internals
- *Entity Framework Core in Action* by Jon P Smith (3rd ed., covers EF Core 7+) — Practical EF Core guidance including pooling

---

*Published by My Blazor Magazine. All code examples are provided for educational purposes and should be reviewed and adapted to your specific security and operational requirements before use in production systems.*
