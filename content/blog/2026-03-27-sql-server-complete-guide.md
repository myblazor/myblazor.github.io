---
title: "SQL Server: The Complete Guide for .NET Developers — From SSMS to T-SQL to Production Best Practices"
date: 2026-03-27
author: myblazor-team
summary: Everything a .NET/C#/ASP.NET developer needs to know about SQL Server — covering versions 2016 through 2025, SSMS 21 and 22, SQL Profiler, sqlcmd, T-SQL, transactions, locking, networking, sessions, debugging, and production best practices.
tags:
  - sql-server
  - dotnet
  - database
  - ssms
  - t-sql
  - best-practices
  - tutorial
---

## Introduction

SQL Server is the database engine that powers a massive share of the .NET ecosystem. Whether you are building an ASP.NET Core Web API backed by Entity Framework Core, a Blazor application hitting a data layer, or a legacy Web Forms app with hand-crafted stored procedures, SQL Server is likely somewhere in your stack. Despite its ubiquity, many .NET developers treat the database as a black box — they write LINQ queries, hope EF Core generates something reasonable, and call it a day.

This guide exists to change that. We will walk through everything a practicing .NET developer should know about SQL Server: the evolution of features across versions 2016 through 2025, how to use SQL Server Management Studio (SSMS) like a power user, how to work from the terminal with sqlcmd, the fundamentals and advanced corners of T-SQL, how transactions and locking actually work, networking and session management, debugging production issues, and the best practices that separate a smooth-running production system from a 3 AM pager alert.

This is a long article. Bookmark it and come back. Let us begin.

---

## Part 1: SQL Server Versions — What Shipped and Why It Matters

Understanding which features landed in which version is critical. Your production server might be running SQL Server 2019 while your development machine has 2022. Knowing the boundaries prevents you from writing code that works locally and fails in staging.

### SQL Server 2016 (Version 13.x)

SQL Server 2016 was a watershed release. It introduced temporal tables — system-versioned tables that automatically track the full history of data changes, letting you query data as it existed at any point in the past using the `FOR SYSTEM_TIME` clause. It brought row-level security, allowing you to define predicate functions that filter rows based on the identity of the executing user, directly within the database engine rather than in application code. Dynamic data masking arrived, enabling you to obscure sensitive columns (like email addresses or credit card numbers) so that unprivileged users see masked values while authorized users see the real data.

The Always Encrypted feature debuted in 2016, providing client-side encryption of sensitive columns such that the database engine itself never sees the plaintext values — the encryption and decryption happen entirely in the client driver, which is critical for compliance scenarios.

On the performance front, 2016 introduced the Query Store — a built-in flight recorder for query plans and runtime statistics. The Query Store captures the execution plan history for every query, along with resource consumption metrics, making it straightforward to identify plan regressions and force a known-good plan without touching application code. This single feature changed how DBAs and developers troubleshoot performance problems.

JSON support also landed in 2016 with `FOR JSON`, `OPENJSON`, `JSON_VALUE`, and `JSON_QUERY` functions, though at this stage JSON was stored as plain `NVARCHAR` with no dedicated data type. R Services (later renamed Machine Learning Services) allowed you to execute R scripts directly inside the database engine.

### SQL Server 2017 (Version 14.x)

The headline of SQL Server 2017 was Linux support. For the first time, SQL Server ran natively on Ubuntu, Red Hat Enterprise Linux, and SUSE Linux Enterprise Server, and was also available as a Docker container. This was a seismic shift — it meant you could run SQL Server in your CI pipeline on a Linux agent, deploy it on Kubernetes, or use it on a Mac for development via Docker.

Adaptive query processing appeared, where the query optimizer could adjust join strategies (for example, switching from a nested loop to a hash join) during execution based on actual row counts, and memory grant feedback allowed the engine to learn from previous executions and adjust memory allocations automatically. Graph database support was introduced with the `NODE` and `EDGE` table types, enabling you to model and query complex relationship-heavy data (think social networks, recommendation engines, or fraud detection graphs) using the `MATCH` pattern in T-SQL. Python support was added to Machine Learning Services alongside R, and automatic database tuning debuted — the engine could detect plan regressions and automatically force the last known good execution plan.

### SQL Server 2019 (Version 15.x)

SQL Server 2019 brought Intelligent Query Processing (IQP) to the forefront with a suite of features: table variable deferred compilation (so the optimizer no longer assumed one row for table variables), batch mode on rowstore (previously batch mode was only available for columnstore indexes), and scalar UDF inlining (the optimizer could inline simple scalar functions directly into the calling query's plan, eliminating the per-row function call overhead that made scalar UDFs so notoriously slow).

Big Data Clusters were introduced (and later deprecated in SQL Server 2025, so do not invest new work here). Accelerated database recovery (ADR) fundamentally changed the crash recovery model by using a persistent version store, making recovery time proportional to the longest uncommitted transaction rather than the amount of work in the log. This was a game-changer for databases with long-running transactions.

UTF-8 collation support arrived, allowing you to use `VARCHAR` columns with UTF-8 encoding instead of needing `NVARCHAR` for international text, which could significantly reduce storage for data that is mostly ASCII but needs occasional Unicode support. The `OPTIMIZE_FOR_SEQUENTIAL_KEY` index option addressed the last-page insert contention problem common in tables with identity columns under high-concurrency inserts.

### SQL Server 2022 (Version 16.x)

SQL Server 2022 was a major step toward cloud integration and performance modernization. The Intelligent Query Processing suite expanded further with Parameter Sensitivity Plan Optimization (PSP optimization) — the optimizer could now create multiple cached plans for the same parameterized query if it detected that different parameter values led to fundamentally different optimal plans. This directly attacked the classic parameter sniffing problem that has plagued SQL Server developers for decades. You no longer had to pepper your stored procedures with `OPTION (RECOMPILE)` or use the `OPTIMIZE FOR` hint as a band-aid.

Degree of Parallelism (DOP) feedback allowed the engine to learn the ideal degree of parallelism for a query over repeated executions and adjust it automatically, rather than relying on a server-wide `MAXDOP` setting. Cardinality estimation (CE) feedback let the optimizer correct persistent misestimates over time.

Ledger tables were introduced for tamper-evident data — the database maintains a cryptographic hash chain of all changes, allowing you to prove that data has not been modified outside of normal transactions. This is valuable for auditing and regulatory compliance without the complexity of a full blockchain.

Contained Availability Groups made it possible to include instance-level objects (logins, SQL Agent jobs, linked servers) inside the AG, so failover truly moved everything you needed. The `LEAST` and `GREATEST` functions finally arrived (yes, it took until 2022 to get these built-in). The `DATETRUNC` function, `GENERATE_SERIES`, `STRING_SPLIT` with an ordinal column, and `WINDOW` clause for cleaner window function syntax all simplified common T-SQL patterns.

On the connectivity side, SQL Server 2022 introduced TDS 8.0 with support for TLS 1.3 and strict encryption mode, where the connection is encrypted before the login handshake even begins.

The Query Store was enabled by default on new databases in SQL Server 2022, and Query Store hints became generally available — you could apply query hints (like `MAXDOP`, `RECOMPILE`, or `USE HINT`) to specific queries identified by their Query Store query_id, without modifying application code.

### SQL Server 2025 (Version 17.x)

SQL Server 2025 reached general availability on November 18, 2025 at Microsoft Ignite. It is the most AI-focused release in SQL Server history, while simultaneously delivering substantial improvements for traditional workloads.

The native JSON data type is the headline developer feature. After a decade of storing JSON as `NVARCHAR`, SQL Server 2025 provides a proper `JSON` column type with optimized storage and native indexing. This means JSON data is stored in an efficient binary format internally, queries against JSON properties are faster, and you get schema validation at the engine level.

The native vector data type and built-in vector search bring AI and machine learning capabilities directly into the database engine. You can store embeddings (arrays of floating-point numbers produced by ML models) in `VECTOR` columns and perform similarity searches using distance functions like cosine similarity, all in T-SQL. For .NET developers building retrieval-augmented generation (RAG) applications, this eliminates the need for a separate vector database.

T-SQL enhancements are substantial: `REGEX` functions for pattern matching (you no longer need CLR assemblies or `LIKE` with wildcards for complex patterns), fuzzy string matching functions, and the ability to call external REST endpoints directly from T-SQL using `sp_invoke_external_rest_endpoint`. You can generate text embeddings and chunks directly in T-SQL, which is remarkable for in-database AI pipelines.

Optimized locking is a major engine improvement. SQL Server 2025 reworks the locking subsystem to reduce lock memory consumption and contention, which is particularly beneficial for high-concurrency OLTP workloads. Transaction ID (TID) locking replaces row-level locking after qualification, reducing the number of locks held and the potential for deadlocks.

Optional Parameter Plan Optimization (OPPO) is the evolution of PSP optimization from 2022, allowing the query optimizer to generate multiple plans for parameterized queries with even finer granularity.

The `abort_query_execution` hint lets DBAs block known-problematic queries from executing at all, which is a powerful safety net for production systems where a single bad query can bring down the server.

SQL Server Reporting Services (SSRS) is discontinued starting with 2025 — all on-premises reporting consolidation happens under Power BI Report Server (PBIRS).

On the platform side, SQL Server 2025 on Linux adds TLS 1.3, custom password policies, and signed container images. Platform support extends to RHEL 10 and Ubuntu 24.04. The Express edition maximum database size jumps to 50 GB (up from 10 GB), and the Express Advanced edition is consolidated into the base Express edition with all features included.

Standard edition capacity limits increase to 4 sockets or 32 cores, which is meaningful for mid-tier workloads that previously required Enterprise licensing.

Change event streaming allows you to stream changes directly from the transaction log to Azure Event Hubs, providing a lower-overhead alternative to Change Data Capture (CDC) for real-time event-driven architectures.

---

## Part 2: SQL Server Management Studio (SSMS) — Mastering the Tool

SSMS is where most .NET developers spend their SQL Server time. As of March 2026, there are two current major versions: SSMS 21 and SSMS 22.

### SSMS 21 and SSMS 22 Overview

Both SSMS 21 and 22 are built on the Visual Studio 2022 shell, making them 64-bit applications. This is a significant departure from SSMS 18, 19, and 20, which used the Visual Studio 2015 shell and were 32-bit. The practical impact is that SSMS 21/22 can handle much larger result sets and more complex execution plans without running out of memory.

SSMS is completely free and standalone. It does not require a SQL Server license, and it is not tied to any specific SQL Server edition or version. You can manage SQL Server 2012 through 2025, Azure SQL Database, Azure SQL Managed Instance, and Azure Synapse Analytics from a single SSMS installation.

SSMS 22 is the latest as of March 2026, with version 22.4.1 released on March 18, 2026. It introduces initial ARM64 support, GitHub Copilot integration (preview), a rebuilt connection dialog, and native support for SQL Server 2025 features like the vector data type.

### Installation

Install SSMS using the Visual Studio Installer bootstrapper. Download the installer from the official Microsoft download page. The installer is a small bootstrapper that downloads the actual components. You do not need to install full Visual Studio — the installer handles the shell components automatically.

You can also install via the command line:

    winget install Microsoft.SQLServerManagementStudio

For SSMS 21 specifically:

    winget install Microsoft.SQLServerManagementStudio.21

SSMS 21 and 22 can coexist with SSMS 20 or earlier. You do not need to uninstall your old version first. Migrate your settings when you are comfortable.

### The Connection Dialog

When you connect to SQL Server, pay attention to the encryption settings. SSMS 22 defaults to mandatory encryption (`-Nm` behavior), which is a breaking change from earlier versions. If you are connecting to a development SQL Server that uses a self-signed certificate, you may need to check "Trust server certificate" or the connection will fail with a certificate validation error. In production, you should use a proper certificate from a trusted CA and set the encryption mode to Strict (available for SQL Server 2022 and later), which uses TDS 8.0 and encrypts before the TLS handshake.

The authentication dropdown now includes Microsoft Entra (formerly Azure Active Directory) options: MFA, Interactive, Managed Identity, Service Principal, and Default. If your organization uses Entra ID for SQL Database or Managed Instance, these are the correct authentication methods.

### SSMS Features Every Developer Should Use

**Object Explorer** is the tree view on the left. Right-clicking on any object gives you context-specific options. Right-click a table and choose "Script Table as > SELECT To > New Query Window" to generate a SELECT statement. Right-click a stored procedure and choose "Modify" to open its definition for editing. Right-click a database and go to "Reports > Standard Reports" for built-in reports on disk usage, index physical statistics, top queries by total CPU time, and more.

**Activity Monitor** (right-click the server name in Object Explorer and select "Activity Monitor") shows real-time data about processes, resource waits, data file I/O, and expensive queries. This is your first stop when something is slow. The "Recent Expensive Queries" pane shows the top queries by CPU, duration, physical reads, and logical writes. Click any query to see its execution plan.

**Execution Plans** are the single most important diagnostic tool. Before running a query, press `Ctrl+L` to display the estimated execution plan without actually executing the query. Press `Ctrl+M` to enable "Include Actual Execution Plan," then execute the query with `F5` — the actual plan appears in a new tab showing real row counts, actual vs. estimated rows, memory grants, and other runtime statistics.

When reading an execution plan, read from right to left and top to bottom. The width of the arrows between operators indicates the relative number of rows flowing through. Look for large discrepancies between estimated and actual rows — these indicate stale statistics or cardinality estimation problems. Look for Key Lookups (a nonclustered index found the rows but needed to go back to the clustered index to fetch additional columns), which often suggest adding included columns to the nonclustered index. Look for Table Scans and Clustered Index Scans on large tables, which may indicate missing indexes or non-sargable WHERE clauses.

Right-click any operator in the plan to see its properties, including the output list (columns it produces), predicates, memory fractions, estimated CPU and I/O cost, and the actual number of rows vs. estimated. Hover over the thick arrows to see the number of rows.

**Include Live Query Statistics** (`Ctrl+Alt+L` before executing) shows the execution plan with real-time progress animation — you can literally watch rows flow through the operators as the query runs. This is invaluable for long-running queries because you can see exactly where the query is spending time without waiting for it to finish.

**Query Store UI** is accessed by expanding a database in Object Explorer, then expanding "Query Store." Here you find built-in reports: Top Resource Consuming Queries, Regressed Queries, Overall Resource Consumption, and Forced Plans. The Regressed Queries view is particularly useful — it shows queries whose performance has degraded compared to historical execution, and lets you force a previous, better-performing plan with a single click. This is one of the most powerful features in SQL Server for application developers who deploy code changes and notice performance degradation.

**Template Explorer** (`Ctrl+Alt+T`) provides pre-built T-SQL templates for common tasks like creating indexes, adding constraints, or configuring replication. Each template has placeholder parameters that SSMS highlights for you to fill in.

**SQLCMD Mode** in SSMS lets you use sqlcmd-specific commands directly in the query editor. Enable it from the Query menu. In SQLCMD mode, you can use `:CONNECT` to connect to a different server mid-script, `:r` to include external script files, and scripting variables with `$(VariableName)` syntax. This is useful for deployment scripts that target multiple servers.

**Multi-Server Queries**: You can register multiple servers in the "Registered Servers" window (`Ctrl+Alt+G`), create server groups, and then execute a query simultaneously against all servers in a group. The results come back with an additional column showing which server produced each row.

**Keyboard Shortcuts**: `F5` executes the selected text (or the entire batch if nothing is selected). `Ctrl+E` also executes. `Ctrl+L` shows the estimated plan. `Ctrl+K, Ctrl+C` comments the selection, `Ctrl+K, Ctrl+U` uncomments. `Ctrl+Shift+U` uppercases the selection, `Ctrl+Shift+L` lowercases. `Alt+F1` with a table name selected runs `sp_help` on it. `Ctrl+R` toggles the results pane. `Ctrl+T` switches results to text mode (which is often more readable for narrow result sets). `Ctrl+D` switches results to grid mode.

**Snippets**: SSMS supports code snippets. Press `Ctrl+K, Ctrl+X` to insert a snippet. You can create custom snippets for your frequently-used T-SQL patterns by adding XML files to the snippets directory.

**Search**: SSMS 21 and 22 include a search bar at the top (`Ctrl+Q`) with two modes — Feature Search (find SSMS settings and commands) and Code Search (find strings in files, folders, or repositories). Feature Search is particularly handy when you cannot remember where a setting lives — just type "line numbers" and it shows you the option to toggle line numbers on or off.

**Tabs**: SSMS 21/22 supports multi-row tabs and configurable tab positions (top, left, or right). Right-click on a tab strip and choose "Set Tab Layout" to change this. With dozens of query windows open, multi-row tabs are a sanity saver.

**Git Integration**: SSMS 21/22 includes Git and GitHub integration. You can initialize a local repository, commit script changes, push to GitHub, and track historical changes to your SQL files directly within SSMS. This is accessible from the Git menu. For teams that version-control their database scripts, this eliminates the need to switch to a separate Git client.

### SQL Profiler and Extended Events

**SQL Profiler** is the legacy tracing tool included with SSMS. Launch it from Tools > SQL Server Profiler. It lets you capture a real-time stream of events happening on the server: query executions, RPC calls, logins, errors, deadlocks, and more.

To use SQL Profiler effectively: create a new trace, connect to your server, and in the "Events Selection" tab, be selective about what you capture. Capturing everything will generate massive amounts of data and impose significant overhead on the server. For a typical debugging session, include these events:

- **SQL:BatchCompleted** — captures the text of each completed batch along with duration, CPU, reads, and writes
- **RPC:Completed** — captures stored procedure calls (this is what you see from parameterized queries sent by EF Core or Dapper)
- **Showplan XML** — captures the actual execution plan for each query (high overhead, use sparingly)
- **Deadlock graph** — captures the XML deadlock graph whenever a deadlock occurs

In the "Column Filters" tab, filter by DatabaseName (to avoid capturing system database activity), Duration (set a minimum to only capture slow queries), and ApplicationName (to isolate traffic from your specific application).

**Important**: SQL Profiler is deprecated. Microsoft recommends using Extended Events instead. However, Profiler remains included in SSMS and is still the quickest way to answer "what queries is my application actually sending to the server?" during development. Just do not run Profiler against a production server under heavy load — the overhead is real and can cause performance problems.

**Extended Events** (XEvents) is the modern replacement for Profiler. It is built into the SQL Server engine and has dramatically lower overhead. In SSMS, expand your server in Object Explorer, go to Management > Extended Events > Sessions. You can create new sessions through the GUI (New Session Wizard or New Session dialog) or with T-SQL.

A common Extended Events session for development captures slow queries:

```sql
CREATE EVENT SESSION [SlowQueries] ON SERVER
ADD EVENT sqlserver.sql_batch_completed (
    SET collect_batch_text = 1
    ACTION (
        sqlserver.sql_text,
        sqlserver.database_name,
        sqlserver.client_app_name,
        sqlserver.session_id
    )
    WHERE duration > 1000000  -- 1 second in microseconds
)
ADD TARGET package0.event_file (
    SET filename = N'SlowQueries.xel',
        max_file_size = 50  -- MB
)
WITH (
    MAX_MEMORY = 4096 KB,
    EVENT_RETENTION_MODE = ALLOW_SINGLE_EVENT_LOSS,
    MAX_DISPATCH_LATENCY = 5 SECONDS,
    STARTUP_STATE = ON
);
GO

ALTER EVENT SESSION [SlowQueries] ON SERVER STATE = START;
```

You can then view the captured events by right-clicking the session in Object Explorer and choosing "Watch Live Data" for a real-time feed, or double-clicking the event file target to open captured data in the SSMS viewer with full filtering and grouping capabilities.

For deadlock analysis, SQL Server maintains a built-in Extended Events session called `system_health` that captures deadlock graphs among other diagnostic events. You can query it:

```sql
SELECT
    xdr.value('@timestamp', 'datetime2') AS deadlock_time,
    xdr.query('.') AS deadlock_graph
FROM (
    SELECT CAST(target_data AS XML) AS target_data
    FROM sys.dm_xe_session_targets st
    JOIN sys.dm_xe_sessions s ON s.address = st.event_session_address
    WHERE s.name = 'system_health'
      AND st.target_name = 'ring_buffer'
) AS data
CROSS APPLY target_data.nodes('//RingBufferTarget/event[@name="xml_deadlock_report"]') AS XEventData(xdr);
```

---

## Part 3: Working with SQL Server from the Terminal

Not every interaction with SQL Server requires opening SSMS. For scripting, automation, CI/CD pipelines, and quick checks, the command line is often faster.

### sqlcmd — The Classic and the Modern

There are two variants of sqlcmd:

**sqlcmd (ODBC)** is the traditional command-line utility that ships with SQL Server and the ODBC driver. It has been around for decades.

**sqlcmd (Go)** — also called go-sqlcmd — is the modern, cross-platform replacement built on the go-mssqldb driver. It runs on Windows, macOS, and Linux. It is open source under the MIT license. Install it with:

    winget install sqlcmd

Or on macOS:

    brew install sqlcmd

Or on Linux via the Microsoft package repository. The Go variant supports all the same commands as the ODBC version plus additional features: syntax coloring in the terminal, vertical result format (much easier to read wide rows), Docker container management (`sqlcmd create mssql` spins up a SQL Server container), and broader Microsoft Entra authentication support.

### Connecting

Connect with Windows Authentication to a local default instance:

    sqlcmd -S localhost -E

Connect with SQL Authentication:

    sqlcmd -S myserver.database.windows.net -U myuser

The Go variant no longer accepts `-P` on the command line for the password (security improvement). It prompts you, or you can set the `SQLCMDPASSWORD` environment variable.

Connect to a named instance:

    sqlcmd -S localhost\SQLEXPRESS

Connect using a specific protocol:

    sqlcmd -S tcp:myserver,1433
    sqlcmd -S np:\\myserver\pipe\sql\query

### Running Queries

Interactive mode:

    1> SELECT name, database_id FROM sys.databases;
    2> GO

The `GO` keyword is the batch terminator — it tells sqlcmd to send everything typed so far to the server. `GO` is not a T-SQL keyword; it is a client-side command recognized by sqlcmd and SSMS.

Run a single query and exit:

    sqlcmd -S localhost -d MyDatabase -Q "SELECT TOP 10 * FROM Customers"

Run a script file:

    sqlcmd -S localhost -d MyDatabase -i deploy_schema.sql -o results.txt

Run multiple script files in order:

    sqlcmd -S localhost -i schema.sql data.sql indexes.sql

Use scripting variables:

    sqlcmd -S localhost -v DatabaseName="Production" -i create_db.sql

In the script, reference the variable as `$(DatabaseName)`.

### Piping and Automation

You can pipe SQL directly:

    echo "SELECT @@VERSION" | sqlcmd -S localhost

This is useful in shell scripts and CI pipelines. When piping input, `GO` batch terminators are optional — sqlcmd automatically executes the batch when input ends.

### Checking Your Connection

Once connected, useful diagnostic queries:

```sql
-- What version am I connected to?
SELECT @@VERSION;
GO

-- What protocol am I using?
SELECT net_transport
FROM sys.dm_exec_connections
WHERE session_id = @@SPID;
GO

-- What database am I in?
SELECT DB_NAME();
GO

-- What login am I?
SELECT SUSER_SNAME();
GO
```

### PowerShell Integration

The `Invoke-Sqlcmd` cmdlet (part of the SqlServer PowerShell module) lets you run queries from PowerShell:

```powershell
Install-Module -Name SqlServer
Invoke-Sqlcmd -ServerInstance "localhost" -Database "MyDb" -Query "SELECT TOP 5 * FROM Products"
```

The SqlServer module also includes cmdlets for backup, restore, reading error logs, and managing availability groups.

### Docker for Development

The Go sqlcmd can spin up a SQL Server container in seconds:

    sqlcmd create mssql --accept-eula --tag 2025-latest

This pulls the SQL Server 2025 container image, starts it, and connects sqlcmd to it. You can also restore a sample database in the same command:

    sqlcmd create mssql --accept-eula --tag 2025-latest --using https://github.com/Microsoft/sql-server-samples/releases/download/wide-world-importers-v1.0/WideWorldImporters-Full.bak

For .NET developers, this is the fastest way to get a throwaway SQL Server instance for integration tests.

---

## Part 4: T-SQL Deep Dive

T-SQL (Transact-SQL) is Microsoft's extension of the SQL standard. As a .NET developer, even if you primarily use EF Core, you need to understand T-SQL for performance tuning, debugging, migrations, and anything that EF Core does not express cleanly.

### Data Types — Choosing Correctly

Use the narrowest appropriate data type. `INT` when you need 4 bytes, `BIGINT` when you need 8, `SMALLINT` or `TINYINT` when values fit. For monetary values, use `DECIMAL(19,4)` or `MONEY` — never `FLOAT` or `REAL`, which have floating-point precision issues. For dates, use `DATE` if you only need the date, `DATETIME2(0)` through `DATETIME2(7)` for date and time (with 0 to 7 fractional second digits), and `DATETIMEOFFSET` when you need timezone awareness. Avoid `DATETIME` for new development — it has only 3.33ms precision and wastes storage compared to `DATETIME2`.

For string columns, prefer `NVARCHAR` for user-facing text that may include international characters, and `VARCHAR` for ASCII-only data or when you use a UTF-8 collation (available since SQL Server 2019). Always specify a length — `NVARCHAR(100)` not `NVARCHAR(MAX)` — unless you truly need more than 4,000 characters. `MAX` columns cannot be part of an index key and have different storage behavior.

For SQL Server 2025, the new `JSON` data type stores JSON more efficiently than `NVARCHAR(MAX)`. The `VECTOR` data type stores embedding vectors for AI/ML workloads.

### Common Table Expressions (CTEs)

CTEs make complex queries readable:

```sql
WITH ActiveCustomers AS (
    SELECT CustomerID, Name, Email
    FROM Customers
    WHERE IsActive = 1
      AND LastOrderDate > DATEADD(MONTH, -6, GETDATE())
),
OrderTotals AS (
    SELECT CustomerID, SUM(TotalAmount) AS LifetimeValue
    FROM Orders
    GROUP BY CustomerID
)
SELECT ac.Name, ac.Email, ot.LifetimeValue
FROM ActiveCustomers ac
JOIN OrderTotals ot ON ac.CustomerID = ot.CustomerID
WHERE ot.LifetimeValue > 1000
ORDER BY ot.LifetimeValue DESC;
```

Recursive CTEs are indispensable for hierarchical data:

```sql
WITH OrgChart AS (
    -- Anchor: top-level managers
    SELECT EmployeeID, Name, ManagerID, 0 AS Level
    FROM Employees
    WHERE ManagerID IS NULL

    UNION ALL

    -- Recursive: subordinates
    SELECT e.EmployeeID, e.Name, e.ManagerID, oc.Level + 1
    FROM Employees e
    JOIN OrgChart oc ON e.ManagerID = oc.EmployeeID
)
SELECT * FROM OrgChart
ORDER BY Level, Name
OPTION (MAXRECURSION 100);
```

### Window Functions

Window functions compute values across a set of rows related to the current row without collapsing the result set:

```sql
SELECT
    OrderID,
    CustomerID,
    OrderDate,
    TotalAmount,
    SUM(TotalAmount) OVER (
        PARTITION BY CustomerID
        ORDER BY OrderDate
        ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW
    ) AS RunningTotal,
    ROW_NUMBER() OVER (
        PARTITION BY CustomerID
        ORDER BY OrderDate DESC
    ) AS RecentOrderRank,
    LAG(TotalAmount) OVER (
        PARTITION BY CustomerID
        ORDER BY OrderDate
    ) AS PreviousOrderAmount,
    LEAD(TotalAmount) OVER (
        PARTITION BY CustomerID
        ORDER BY OrderDate
    ) AS NextOrderAmount
FROM Orders;
```

The `ROWS BETWEEN` clause controls the window frame. `RANGE BETWEEN` is subtly different — it treats ties as part of the same frame. In SQL Server 2022 and later, the `WINDOW` clause lets you define named window specifications and reuse them:

```sql
SELECT
    OrderID,
    CustomerID,
    SUM(TotalAmount) OVER w AS RunningTotal,
    AVG(TotalAmount) OVER w AS RunningAvg
FROM Orders
WINDOW w AS (
    PARTITION BY CustomerID
    ORDER BY OrderDate
    ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW
);
```

### MERGE Statement

`MERGE` performs insert, update, and delete in a single atomic statement based on a source/target comparison:

```sql
MERGE INTO Products AS target
USING StagingProducts AS source
ON target.SKU = source.SKU
WHEN MATCHED AND target.Price <> source.Price THEN
    UPDATE SET target.Price = source.Price, target.UpdatedAt = GETUTCDATE()
WHEN NOT MATCHED BY TARGET THEN
    INSERT (SKU, Name, Price, CreatedAt)
    VALUES (source.SKU, source.Name, source.Price, GETUTCDATE())
WHEN NOT MATCHED BY SOURCE THEN
    DELETE;
```

Always include the semicolon after `MERGE` — it is one of the few T-SQL statements that requires a terminating semicolon.

### Error Handling

Use `TRY...CATCH` blocks:

```sql
BEGIN TRY
    BEGIN TRANSACTION;

    UPDATE Accounts SET Balance = Balance - 500 WHERE AccountID = 1;
    UPDATE Accounts SET Balance = Balance + 500 WHERE AccountID = 2;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrorSeverity INT = ERROR_SEVERITY();
    DECLARE @ErrorState INT = ERROR_STATE();
    DECLARE @ErrorLine INT = ERROR_LINE();
    DECLARE @ErrorProcedure NVARCHAR(200) = ERROR_PROCEDURE();

    -- Log the error
    INSERT INTO ErrorLog (Message, Severity, State, Line, Procedure, OccurredAt)
    VALUES (@ErrorMessage, @ErrorSeverity, @ErrorState, @ErrorLine, @ErrorProcedure, GETUTCDATE());

    -- Re-raise
    THROW;
END CATCH;
```

`THROW` (introduced in SQL Server 2012) is preferred over `RAISERROR` for re-raising errors because it preserves the original error number, severity, and state. Use `RAISERROR` when you need to raise a custom error with a specific severity level.

### String Functions — Old and New

SQL Server 2022 and 2025 added string functions that developers had been requesting for years:

```sql
-- TRIM (SQL Server 2017+)
SELECT TRIM('   hello   ');  -- 'hello'
SELECT TRIM('xy' FROM 'xyhelloyx');  -- 'hello' (SQL Server 2022+)

-- STRING_AGG (SQL Server 2017+)
SELECT DepartmentID, STRING_AGG(Name, ', ') AS Employees
FROM Employees
GROUP BY DepartmentID;

-- STRING_SPLIT with ordinal (SQL Server 2022+)
SELECT value, ordinal
FROM STRING_SPLIT('a,b,c', ',', 1);

-- GREATEST and LEAST (SQL Server 2022+)
SELECT GREATEST(10, 20, 5);   -- 20
SELECT LEAST(10, 20, 5);      -- 5

-- DATETRUNC (SQL Server 2022+)
SELECT DATETRUNC(MONTH, GETDATE());  -- First day of current month

-- GENERATE_SERIES (SQL Server 2022+)
SELECT value FROM GENERATE_SERIES(1, 10);
SELECT value FROM GENERATE_SERIES(1, 100, 5);  -- Step by 5
```

In SQL Server 2025, `REGEX` functions allow true regular expression matching without CLR:

```sql
-- SQL Server 2025
SELECT *
FROM Customers
WHERE REGEX_LIKE(Email, '^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$');
```

---

## Part 5: Transactions — Understanding the Fundamentals

Transactions are the mechanism that ensures data integrity. Every .NET developer must understand them.

### ACID Properties

**Atomicity**: All statements in a transaction succeed or all are rolled back. There is no partial commit. **Consistency**: The database moves from one valid state to another. Constraints, triggers, and cascades are enforced. **Isolation**: Concurrent transactions do not interfere with each other (the degree depends on the isolation level). **Durability**: Once committed, the data survives a crash — it is written to the transaction log on disk before the commit completes.

### Implicit vs. Explicit Transactions

By default, SQL Server operates in auto-commit mode: each individual statement is its own transaction. When you run `UPDATE Customers SET Name = 'Alice' WHERE CustomerID = 1`, SQL Server implicitly wraps it in a transaction, executes it, and commits. If the statement fails, it is automatically rolled back.

Explicit transactions use `BEGIN TRANSACTION`, `COMMIT`, and `ROLLBACK`:

```sql
BEGIN TRANSACTION;

UPDATE Inventory SET Quantity = Quantity - 1 WHERE ProductID = 42;
INSERT INTO OrderItems (OrderID, ProductID, Quantity) VALUES (100, 42, 1);

COMMIT TRANSACTION;
```

If any statement between `BEGIN` and `COMMIT` fails and you do not catch it, the transaction remains open. Always use `TRY...CATCH` with explicit transactions, and always check `@@TRANCOUNT` in the `CATCH` block.

### Save Points

Within a transaction, you can set save points to enable partial rollback:

```sql
BEGIN TRANSACTION;

INSERT INTO Orders (CustomerID, OrderDate) VALUES (1, GETDATE());
SAVE TRANSACTION AfterOrderInsert;

BEGIN TRY
    INSERT INTO OrderItems (OrderID, ProductID, Quantity) VALUES (SCOPE_IDENTITY(), 99, 1);
END TRY
BEGIN CATCH
    -- Roll back only the failed insert, not the entire transaction
    ROLLBACK TRANSACTION AfterOrderInsert;
END CATCH;

COMMIT TRANSACTION;
```

### Transaction Isolation Levels

This is where many bugs live. The isolation level controls what concurrent transactions can see.

**READ UNCOMMITTED**: The transaction can read data modified by other uncommitted transactions (dirty reads). This is the least restrictive level. Useful for rough estimates on data that is not critical.

**READ COMMITTED** (default): The transaction can only read data that has been committed. However, if you read the same row twice, it might have changed between reads (non-repeatable reads), and new rows matching your WHERE clause might appear (phantom reads).

**REPEATABLE READ**: Once a row is read, it cannot be modified by another transaction until the current transaction ends. This prevents non-repeatable reads but not phantom reads.

**SERIALIZABLE**: The most restrictive level. Range locks are placed on the data, preventing other transactions from inserting rows that would match the current transaction's WHERE clauses. This prevents dirty reads, non-repeatable reads, and phantom reads, but it causes the most blocking and the highest risk of deadlocks.

**SNAPSHOT**: Uses row versioning. When the transaction starts, it gets a consistent snapshot of the database as of that point in time. It can read without acquiring shared locks, so readers do not block writers and writers do not block readers. However, if the transaction tries to modify a row that has been modified by another transaction since the snapshot was taken, it gets an update conflict error.

**READ COMMITTED SNAPSHOT ISOLATION (RCSI)**: A database-level option that changes the behavior of READ COMMITTED to use row versioning instead of shared locks. Readers get a snapshot as of the start of each individual statement (not the start of the transaction). This is the default behavior for Azure SQL Database and is strongly recommended for most OLTP workloads.

To enable RCSI:

```sql
ALTER DATABASE MyDatabase SET READ_COMMITTED_SNAPSHOT ON;
```

This requires exclusive access to the database (no other connections). For production databases, coordinate a brief maintenance window.

### Transaction Best Practices

Keep transactions short. Every lock held by a transaction blocks other transactions. A transaction that holds locks for 30 seconds while calling an external API is a production incident waiting to happen. Do your external calls, computations, and validations outside the transaction, then enter the transaction only for the database writes.

Always set a transaction timeout in your application code:

```csharp
using var connection = new SqlConnection(connectionString);
await connection.OpenAsync();
using var transaction = await connection.BeginTransactionAsync();
// SqlCommand.CommandTimeout = 30 seconds by default
```

In EF Core:

```csharp
using var transaction = await dbContext.Database.BeginTransactionAsync();
try
{
    // ... operations
    await dbContext.SaveChangesAsync();
    await transaction.CommitAsync();
}
catch
{
    await transaction.RollbackAsync();
    throw;
}
```

---

## Part 6: Locking, Blocking, and Deadlocks

### How Locking Works

SQL Server uses a multi-granularity locking system. Locks can be acquired at the row level, page level (8 KB), extent level (64 KB, 8 pages), table level, or database level. The engine starts with the finest granularity appropriate for the operation and may escalate to a coarser level if too many fine-grained locks are held (by default, escalation occurs at approximately 5,000 locks on a single table).

The main lock modes are: Shared (S) for reads, Exclusive (X) for writes, Update (U) for update operations (a transitional lock that converts to X when the actual modification happens), Intent locks (IS, IX, IU) that signal to higher-granularity lock checks that a finer-grained lock exists, and Schema locks (Sch-S and Sch-M) for DDL operations.

### The NOLOCK Debate — Should You Use It?

`WITH (NOLOCK)` — equivalent to `SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED` for that table reference — is one of the most controversial hints in SQL Server.

**What NOLOCK does**: It tells SQL Server to read data without acquiring shared locks, and to ignore exclusive locks held by other transactions. This means the query will never be blocked by a writer and will never block a writer.

**What can go wrong**: Dirty reads (reading data from an uncommitted transaction that may later be rolled back — you would be working with data that never actually existed). Skipped rows or duplicate rows (if a page split occurs during an allocation order scan, the scan can miss rows that moved or encounter the same row twice). Errors (reading a page that is in the middle of being updated can cause incorrect column values or even errors).

**Development environment**: Using NOLOCK is generally acceptable during development for ad hoc queries where you want quick answers and do not care about perfect accuracy. Running `SELECT COUNT(*) FROM LargeTable WITH (NOLOCK)` to get a rough row count is fine.

**Production reads**: The answer depends on your workload. For a reporting query against a large table where an approximate result is acceptable and blocking readers would impact OLTP throughput, NOLOCK may be a pragmatic choice. But the better answer for most OLTP workloads is to enable Read Committed Snapshot Isolation (RCSI) at the database level. RCSI gives you non-blocking reads with transactional consistency — no dirty reads, no skipped or duplicate rows, no page-split anomalies. It costs some tempdb I/O for the version store, but this is almost always a good tradeoff.

**Production writes**: Never use NOLOCK on the target of an UPDATE or DELETE. It does not apply there anyway — write operations always acquire exclusive locks.

**Recommendation**: Enable RCSI on your databases and stop using NOLOCK. If you need historical consistency across multiple statements, use SNAPSHOT isolation.

### Diagnosing Blocking

When queries hang, check for blocking:

```sql
-- Who is blocking whom?
SELECT
    blocking.session_id AS BlockingSessionID,
    blocked.session_id AS BlockedSessionID,
    blocked.wait_type,
    blocked.wait_time / 1000 AS WaitSeconds,
    blocked_sql.text AS BlockedQuery,
    blocking_sql.text AS BlockingQuery
FROM sys.dm_exec_requests blocked
JOIN sys.dm_exec_sessions blocking
    ON blocked.blocking_session_id = blocking.session_id
CROSS APPLY sys.dm_exec_sql_text(blocked.sql_handle) blocked_sql
OUTER APPLY sys.dm_exec_sql_text(blocking.most_recent_sql_handle) blocking_sql
WHERE blocked.blocking_session_id <> 0;
```

### Deadlocks

A deadlock occurs when two or more transactions each hold a lock that the other needs. SQL Server automatically detects deadlocks (via the lock monitor thread, which runs every 5 seconds by default) and kills one of the transactions (the deadlock victim, chosen based on cost to roll back).

To minimize deadlocks: access tables in the same order in all transactions, keep transactions short, use the lowest necessary isolation level, and avoid user interaction mid-transaction. If deadlocks persist, use the deadlock graph (from Extended Events or the `system_health` session) to identify the specific resources and queries involved, then redesign the access patterns.

In your .NET code, always handle deadlocks with a retry loop:

```csharp
const int maxRetries = 3;
for (int attempt = 1; attempt <= maxRetries; attempt++)
{
    try
    {
        await ExecuteTransactionAsync();
        return;
    }
    catch (SqlException ex) when (ex.Number == 1205) // Deadlock victim
    {
        if (attempt == maxRetries) throw;
        await Task.Delay(TimeSpan.FromMilliseconds(100 * attempt));
    }
}
```

### SQL Server 2025 Optimized Locking

SQL Server 2025 introduces Transaction ID (TID) locking, which changes how row locks are handled after qualification. Instead of holding a row lock for the duration of the transaction, the engine can release it earlier and use a lighter-weight TID lock. This reduces lock memory consumption and contention, particularly for high-concurrency workloads. The behavior is automatic on SQL Server 2025 — you do not need to change queries or hints.

---

## Part 7: Indexing Best Practices

### Clustered Index

Every table should have a clustered index. The clustered index defines the physical order of data on disk. For most tables, the primary key — typically an `INT IDENTITY` or `BIGINT IDENTITY` — is the clustered index. This gives you sequential inserts (minimizing page splits), narrow keys (4 or 8 bytes — important because every nonclustered index carries a copy of the clustered index key), and unique values.

Using a `GUID` (`UNIQUEIDENTIFIER`) as a clustered index key is almost always a mistake. `NEWID()` generates random values, causing random inserts across the entire B-tree, which leads to massive page splits, fragmentation, and terrible I/O performance. `NEWSEQUENTIALID()` mitigates this somewhat but is still 16 bytes wide. Use GUIDs as nonclustered index columns if you need them for distributed identity, but keep the clustered key narrow and sequential.

### Nonclustered Indexes

Design nonclustered indexes based on your query patterns, not your table structure. The key columns should be the columns in your WHERE clause and JOIN conditions, ordered from most selective to least selective. Include columns (in the `INCLUDE` clause) for columns that are only in the SELECT list — this prevents key lookups.

```sql
-- If your common query is:
SELECT OrderID, OrderDate, TotalAmount
FROM Orders
WHERE CustomerID = @CustID AND Status = 'Shipped'
ORDER BY OrderDate DESC;

-- Then create:
CREATE NONCLUSTERED INDEX IX_Orders_CustomerID_Status
ON Orders (CustomerID, Status)
INCLUDE (OrderDate, TotalAmount);
```

### Filtered Indexes

If a column has a heavily skewed distribution (for example, 95% of rows have `Status = 'Completed'` and you only ever query for the other 5%), use a filtered index:

```sql
CREATE NONCLUSTERED INDEX IX_Orders_Pending
ON Orders (CustomerID, OrderDate)
INCLUDE (TotalAmount)
WHERE Status IN ('Pending', 'Processing', 'Shipped');
```

This index is smaller, faster to maintain, and uses less memory.

### Columnstore Indexes

For analytical queries that scan large portions of a table, columnstore indexes provide order-of-magnitude performance improvements. They store data in a columnar format and use batch mode processing. You can add a nonclustered columnstore index alongside your rowstore indexes:

```sql
CREATE NONCLUSTERED COLUMNSTORE INDEX NCCI_Orders_Analytics
ON Orders (CustomerID, OrderDate, TotalAmount, Status);
```

### Missing Index DMVs

SQL Server tracks queries that could benefit from an index:

```sql
SELECT
    mig.index_group_handle,
    mid.statement AS TableName,
    mid.equality_columns,
    mid.inequality_columns,
    mid.included_columns,
    migs.unique_compiles,
    migs.user_seeks,
    migs.avg_total_user_cost * migs.avg_user_impact * (migs.user_seeks + migs.user_scans) AS ImprovementScore
FROM sys.dm_db_missing_index_groups mig
JOIN sys.dm_db_missing_index_group_stats migs ON mig.index_group_handle = migs.group_handle
JOIN sys.dm_db_missing_index_details mid ON mig.index_handle = mid.index_handle
ORDER BY ImprovementScore DESC;
```

Do not blindly create every missing index — review them for overlap with existing indexes, consolidate where possible, and consider the write overhead of maintaining additional indexes.

---

## Part 8: Networking, Sessions, and Connection Management

### SQL Server Network Configuration

SQL Server listens on one or more network protocols: TCP/IP (the most common, default port 1433), Named Pipes (for local or intranet connections), and Shared Memory (local connections only). Configure these in SQL Server Configuration Manager.

For production, use TCP/IP exclusively. Ensure the firewall allows inbound connections on port 1433 (or your custom port). If you use a named instance, it uses a dynamic port assigned by the SQL Server Browser service (which listens on UDP 1434). For production named instances, assign a static port in Configuration Manager.

### Connection Strings from .NET

A typical ASP.NET Core connection string:

    Server=myserver.database.windows.net;Database=MyApp;User Id=myuser;Password=mypassword;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;

Key parameters to understand: `Encrypt=True` enables TLS encryption (mandatory for Azure SQL, strongly recommended for all production servers). `TrustServerCertificate=False` (the default) validates the server certificate — set this to `True` only for development with self-signed certificates. `Connection Timeout=30` is the maximum time to wait for a connection from the pool. `Max Pool Size=100` (default) is the maximum number of connections in the pool. `MultipleActiveResultSets=True` allows multiple open readers on a single connection (required by some EF Core patterns, but adds overhead).

### Connection Pooling

ADO.NET (and by extension EF Core and Dapper) uses connection pooling by default. When you close a connection in code, it is returned to the pool — not actually closed. When you open a connection, the pool gives you an existing one if available. This is why it is critical to always dispose of `SqlConnection` objects promptly (use `using` statements).

If your application hits `Max Pool Size` and all connections are in use, the next `OpenAsync()` call will block until a connection is returned or the connection timeout expires, at which point you get a `TimeoutException`. This almost always means you have a connection leak — some code path is opening a connection without closing/disposing it.

Monitor pool usage:

```sql
SELECT
    DB_NAME(dbid) AS DatabaseName,
    COUNT(*) AS ConnectionCount,
    loginame AS LoginName,
    hostname AS HostName,
    program_name AS Application
FROM sys.sysprocesses
GROUP BY dbid, loginame, hostname, program_name
ORDER BY ConnectionCount DESC;
```

Or with the modern DMV:

```sql
SELECT
    s.session_id,
    s.login_name,
    s.host_name,
    s.program_name,
    c.connect_time,
    c.net_transport,
    c.protocol_type,
    c.encrypt_option,
    s.status,
    s.last_request_start_time,
    s.last_request_end_time,
    r.command,
    r.wait_type,
    r.blocking_session_id
FROM sys.dm_exec_sessions s
LEFT JOIN sys.dm_exec_connections c ON s.session_id = c.session_id
LEFT JOIN sys.dm_exec_requests r ON s.session_id = r.session_id
WHERE s.is_user_process = 1
ORDER BY s.last_request_start_time DESC;
```

### Session Management

Every connection to SQL Server creates a session. Useful session-level settings:

```sql
SET NOCOUNT ON;              -- Suppress "N rows affected" messages (reduces network traffic)
SET XACT_ABORT ON;           -- Auto-rollback the transaction on any error
SET ARITHABORT ON;           -- Required for indexed views and computed columns
SET ANSI_NULLS ON;           -- NULL comparisons follow ANSI standard
SET QUOTED_IDENTIFIER ON;    -- Double quotes delimit identifiers, not strings
```

`SET XACT_ABORT ON` is particularly important. Without it, some errors (like constraint violations) leave the transaction open, and subsequent statements execute as if nothing happened. With `XACT_ABORT ON`, any error immediately rolls back the entire transaction. Always set this at the beginning of stored procedures.

### Killing Sessions

If a session is blocking others and needs to be terminated:

```sql
KILL 52;  -- 52 is the session_id
```

Use this judiciously — killing a session that is mid-transaction causes a rollback, which can take time proportional to the work already done.

---

## Part 9: Debugging Production Issues

### Dynamic Management Views (DMVs)

DMVs are your primary diagnostic tool for production SQL Server. They expose internal state without the overhead of profiling.

**Currently executing queries:**

```sql
SELECT
    r.session_id,
    r.status,
    r.command,
    r.wait_type,
    r.wait_time,
    r.blocking_session_id,
    r.cpu_time,
    r.logical_reads,
    r.total_elapsed_time / 1000 AS ElapsedSeconds,
    SUBSTRING(t.text, r.statement_start_offset / 2 + 1,
        (CASE WHEN r.statement_end_offset = -1
            THEN LEN(CONVERT(NVARCHAR(MAX), t.text)) * 2
            ELSE r.statement_end_offset END - r.statement_start_offset) / 2 + 1
    ) AS CurrentStatement,
    p.query_plan
FROM sys.dm_exec_requests r
CROSS APPLY sys.dm_exec_sql_text(r.sql_handle) t
CROSS APPLY sys.dm_exec_query_plan(r.plan_handle) p
WHERE r.session_id > 50  -- Exclude system sessions
ORDER BY r.total_elapsed_time DESC;
```

**Top queries by CPU (historical, from plan cache):**

```sql
SELECT TOP 20
    qs.total_worker_time / qs.execution_count AS AvgCPU,
    qs.total_worker_time AS TotalCPU,
    qs.execution_count,
    qs.total_logical_reads / qs.execution_count AS AvgReads,
    SUBSTRING(t.text, qs.statement_start_offset / 2 + 1,
        (CASE WHEN qs.statement_end_offset = -1
            THEN LEN(CONVERT(NVARCHAR(MAX), t.text)) * 2
            ELSE qs.statement_end_offset END - qs.statement_start_offset) / 2 + 1
    ) AS QueryText
FROM sys.dm_exec_query_stats qs
CROSS APPLY sys.dm_exec_sql_text(qs.sql_handle) t
ORDER BY AvgCPU DESC;
```

**Wait statistics (what is the server waiting on?):**

```sql
SELECT TOP 20
    wait_type,
    waiting_tasks_count,
    wait_time_ms / 1000 AS WaitSeconds,
    signal_wait_time_ms / 1000 AS SignalWaitSeconds,
    (wait_time_ms - signal_wait_time_ms) / 1000 AS ResourceWaitSeconds
FROM sys.dm_os_wait_stats
WHERE wait_type NOT IN (
    'SLEEP_TASK', 'BROKER_TASK_STOP', 'BROKER_EVENTHANDLER',
    'CLR_AUTO_EVENT', 'CLR_MANUAL_EVENT', 'LAZYWRITER_SLEEP',
    'SQLTRACE_BUFFER_FLUSH', 'WAITFOR', 'XE_TIMER_EVENT',
    'BROKER_TO_FLUSH', 'BROKER_RECEIVE_WAITFOR', 'CHECKPOINT_QUEUE',
    'REQUEST_FOR_DEADLOCK_SEARCH', 'FT_IFTS_SCHEDULER_IDLE_WAIT',
    'XE_DISPATCHER_WAIT', 'LOGMGR_QUEUE', 'ONDEMAND_TASK_QUEUE',
    'DIRTY_PAGE_POLL', 'HADR_FILESTREAM_IOMGR_IOCOMPLETION',
    'SP_SERVER_DIAGNOSTICS_SLEEP'
)
AND waiting_tasks_count > 0
ORDER BY wait_time_ms DESC;
```

Common wait types and what they mean: `PAGEIOLATCH_SH` (waiting for a data page to be read from disk — indicates memory pressure or slow I/O), `LCK_M_X` or `LCK_M_S` (waiting for a lock — blocking), `CXPACKET` or `CXCONSUMER` (parallelism waits — often normal, but excessive amounts may indicate skewed parallelism), `WRITELOG` (waiting for the transaction log to be written to disk — check log disk performance), `SOS_SCHEDULER_YIELD` (CPU pressure — the server needs more CPU or query tuning).

### Index Fragmentation

Check fragmentation for a specific table:

```sql
SELECT
    i.name AS IndexName,
    ps.avg_fragmentation_in_percent,
    ps.page_count,
    ps.record_count
FROM sys.dm_db_index_physical_stats(
    DB_ID(), OBJECT_ID('dbo.Orders'), NULL, NULL, 'LIMITED'
) ps
JOIN sys.indexes i ON ps.object_id = i.object_id AND ps.index_id = i.index_id
WHERE ps.page_count > 1000  -- Only look at indexes with meaningful size
ORDER BY ps.avg_fragmentation_in_percent DESC;
```

Below 10% fragmentation: do nothing. Between 10% and 30%: reorganize (`ALTER INDEX ... REORGANIZE`). Above 30%: rebuild (`ALTER INDEX ... REBUILD`). Reorganize is an online, incremental operation. Rebuild is more thorough but takes a schema lock (unless you use `ONLINE = ON`, which requires Enterprise edition or SQL Server 2025 Standard).

### tempdb Monitoring

tempdb is a shared resource used for temporary tables, table variables, sort spill, hash join spill, version store (for RCSI and snapshot isolation), and internal engine operations. If tempdb runs out of space or has contention, everything on the server slows down.

```sql
SELECT
    SUM(unallocated_extent_page_count) * 8 / 1024 AS FreeSpaceMB,
    SUM(internal_object_reserved_page_count) * 8 / 1024 AS InternalObjectsMB,
    SUM(user_object_reserved_page_count) * 8 / 1024 AS UserObjectsMB,
    SUM(version_store_reserved_page_count) * 8 / 1024 AS VersionStoreMB
FROM sys.dm_db_file_space_usage;
```

---

## Part 10: Best Practices Checklist for .NET Developers

### Database Design

Always use schemas (`dbo`, `sales`, `hr`) to organize objects. Do not put everything in `dbo`. Use meaningful, consistent naming conventions — `PascalCase` for tables and columns is the most common in .NET shops. Every table gets a clustered primary key. Use foreign keys to enforce referential integrity — do not rely on application code alone. Add appropriate check constraints.

### Stored Procedures vs. Inline SQL vs. EF Core

There is no universal answer. EF Core is excellent for CRUD operations, migrations, and applications where developer productivity matters most. Raw SQL (via Dapper or `SqlCommand`) is appropriate for complex queries, bulk operations, or performance-critical paths where you need full control over the T-SQL. Stored procedures are appropriate when you need to encapsulate complex business logic at the database layer, when security requirements mandate that the application cannot issue ad hoc SQL, or when you need to share logic across multiple applications.

If you use EF Core, always monitor the generated SQL using logging:

```csharp
optionsBuilder.LogTo(Console.WriteLine, LogLevel.Information)
              .EnableSensitiveDataLogging();
```

Look for N+1 query patterns (a query for each item in a loop instead of a single query with `Include`), unnecessary columns being fetched (use `Select` projections), and queries that pull the entire table into memory instead of filtering at the database.

### Connection Handling

Always use `using` statements or `await using` for connections, commands, and readers. Never hold a connection open across an HTTP request boundary (open late, close early). Do not increase `Max Pool Size` to mask a connection leak — find and fix the leak.

### Parameterized Queries — Always

Never concatenate user input into SQL strings. Always use parameters:

```csharp
// WRONG — SQL injection vulnerability
var sql = $"SELECT * FROM Users WHERE Name = '{userName}'";

// RIGHT
var sql = "SELECT * FROM Users WHERE Name = @Name";
cmd.Parameters.AddWithValue("@Name", userName);

// BETTER — explicit type
cmd.Parameters.Add("@Name", SqlDbType.NVarChar, 100).Value = userName;
```

EF Core handles parameterization automatically, but if you use `FromSqlRaw`, make sure to use parameter placeholders.

### Monitoring and Alerting

Set up alerts for: long-running queries (over N seconds), deadlocks, tempdb space usage, log file growth, failed logins, and database integrity check failures (`DBCC CHECKDB`). Use SQL Server Agent alerts, Azure Monitor, or your preferred monitoring stack.

Run `DBCC CHECKDB` on a schedule. It detects physical and logical corruption. For large databases, run it weekly during a maintenance window. For critical databases, run it daily.

### Backup and Recovery

Test your backups by restoring them. A backup you have never tested is not a backup — it is a hope. Understand the difference between full backups, differential backups (changes since the last full backup), and transaction log backups (changes since the last log backup). For point-in-time recovery, you need the full recovery model and a chain of log backups.

In your .NET application, handle transient failures (network blips, failovers) with retry policies. The `Microsoft.Data.SqlClient` library supports configurable retry logic.

### Statistics

SQL Server uses statistics (histograms of data distribution) to make query plan decisions. If statistics are stale, the optimizer makes bad choices. Auto-update statistics is enabled by default, but it triggers only after approximately 20% of the rows have changed (with a lower threshold for larger tables in SQL Server 2016+ with trace flag 2371, which is default behavior in SQL Server 2022+).

For tables with skewed distributions or after large data loads, manually update statistics:

```sql
UPDATE STATISTICS dbo.Orders WITH FULLSCAN;
```

Or for all tables:

```sql
EXEC sp_updatestats;
```

### Maintenance Plans

Set up regular maintenance: index reorganize/rebuild (weekly), statistics update (daily or after large data changes), `DBCC CHECKDB` (weekly), and cleanup of old backup files, job history, and maintenance plan reports. Ola Hallengren's maintenance solution (free, open source) is the gold standard for automated index and statistics maintenance.

---

## Part 11: SQL Server from C# — Practical Patterns

### Dapper for Performance-Critical Paths

```csharp
using Dapper;

await using var connection = new SqlConnection(connectionString);
var orders = await connection.QueryAsync<Order>(
    @"SELECT OrderID, CustomerID, OrderDate, TotalAmount
      FROM Orders
      WHERE CustomerID = @CustomerId AND OrderDate > @Since",
    new { CustomerId = 42, Since = DateTime.UtcNow.AddMonths(-6) }
);
```

### Bulk Operations

For inserting thousands of rows, do not use individual INSERT statements or even EF Core's `AddRange`. Use `SqlBulkCopy`:

```csharp
using var bulkCopy = new SqlBulkCopy(connection, SqlBulkCopyOptions.TableLock, null);
bulkCopy.DestinationTableName = "dbo.StagingOrders";
bulkCopy.BatchSize = 10000;
await bulkCopy.WriteToServerAsync(dataTable);
```

For EF Core 7+, the `ExecuteUpdate` and `ExecuteDelete` methods generate set-based UPDATE and DELETE statements, avoiding the per-row overhead:

```csharp
await dbContext.Orders
    .Where(o => o.Status == "Cancelled" && o.OrderDate < cutoff)
    .ExecuteDeleteAsync();
```

### Resilience with Microsoft.Data.SqlClient

```csharp
var options = new SqlRetryLogicOption
{
    NumberOfTries = 3,
    DeltaTime = TimeSpan.FromSeconds(1),
    MaxTimeInterval = TimeSpan.FromSeconds(20),
    TransientErrors = new[] { 1205, 49920, 49919 } // Deadlock, throttled, etc.
};
var retryProvider = SqlConfigurableRetryFactory.CreateExponentialRetryProvider(options);

using var connection = new SqlConnection(connectionString);
connection.RetryLogicProvider = retryProvider;
```

---

## Part 12: Security Essentials

### Principle of Least Privilege

Your application's database login should have only the permissions it needs. Create a dedicated login and database user:

```sql
CREATE LOGIN AppUser WITH PASSWORD = 'StrongPassword123!';
CREATE USER AppUser FOR LOGIN AppUser;

-- Grant specific permissions
GRANT SELECT, INSERT, UPDATE, DELETE ON SCHEMA::dbo TO AppUser;
-- Or for stored procedures:
GRANT EXECUTE ON SCHEMA::dbo TO AppUser;
```

Never use `sa` or `db_owner` for application connections.

### Always Encrypted

For columns containing sensitive data (SSN, credit card numbers), use Always Encrypted. The encryption keys are managed by the client driver (your .NET application) and the database engine never sees the plaintext. Configure this through SSMS: right-click the database, choose Tasks > Manage Always Encrypted Keys, then right-click the table and choose Encrypt Columns.

In your connection string, add `Column Encryption Setting=Enabled`.

### Row-Level Security

Create a predicate function and a security policy to filter rows based on the current user:

```sql
CREATE FUNCTION dbo.fn_TenantFilter(@TenantID INT)
RETURNS TABLE
WITH SCHEMABINDING
AS
    RETURN SELECT 1 AS Result
    WHERE @TenantID = CAST(SESSION_CONTEXT(N'TenantID') AS INT);

CREATE SECURITY POLICY dbo.TenantPolicy
ADD FILTER PREDICATE dbo.fn_TenantFilter(TenantID) ON dbo.Orders;
```

In your .NET middleware, set the session context for each request:

```csharp
await using var cmd = connection.CreateCommand();
cmd.CommandText = "EXEC sp_set_session_context @key = N'TenantID', @value = @TenantID";
cmd.Parameters.AddWithValue("@TenantID", currentTenantId);
await cmd.ExecuteNonQueryAsync();
```

### Transparent Data Encryption (TDE)

TDE encrypts the database files at rest — the data files, log files, and backups are encrypted on disk. Enable it in one command:

```sql
CREATE DATABASE ENCRYPTION KEY
WITH ALGORITHM = AES_256
ENCRYPTION BY SERVER CERTIFICATE MyServerCert;

ALTER DATABASE MyDatabase SET ENCRYPTION ON;
```

This is transparent to the application — no code changes needed.

---

## Part 13: Performance Tuning Workflow

When a query is slow, follow this systematic approach:

1. **Get the actual execution plan** (`Ctrl+M` in SSMS, then `F5`).
2. **Look at the actual vs. estimated rows** for each operator. Large discrepancies indicate statistics problems.
3. **Identify the most expensive operators** (the ones with the highest percentage cost).
4. **Check for Key Lookups** — add INCLUDE columns to the relevant nonclustered index.
5. **Check for Table Scans on large tables** — determine if an index would help.
6. **Check for implicit conversions** — look for yellow warning triangles on operators. A common cause is comparing an `NVARCHAR` parameter against a `VARCHAR` column, which forces a scan because the engine must convert every row.
7. **Check wait statistics** for the specific query — is it waiting on I/O, locks, memory, or CPU?
8. **Review the Query Store** for plan regression — did this query used to be fast with a different plan?
9. **Update statistics** with `FULLSCAN` if they appear stale.
10. **Consider rewriting the query** — sometimes a different approach (replacing a correlated subquery with a JOIN, breaking a complex query into CTEs, or using `EXISTS` instead of `IN`) changes the plan dramatically.

---

## Part 14: SQL Server 2025 AI Features for .NET Developers

SQL Server 2025 brings AI capabilities that .NET developers can use directly from their existing codebase.

### Vector Search

Store and search embeddings directly in SQL Server:

```sql
CREATE TABLE Documents (
    DocumentID INT IDENTITY PRIMARY KEY,
    Title NVARCHAR(200),
    Content NVARCHAR(MAX),
    Embedding VECTOR(1536)  -- 1536 dimensions, matching OpenAI ada-002
);

-- Find similar documents by cosine similarity
SELECT TOP 10
    DocumentID,
    Title,
    VECTOR_DISTANCE('cosine', Embedding, @QueryEmbedding) AS Distance
FROM Documents
ORDER BY VECTOR_DISTANCE('cosine', Embedding, @QueryEmbedding);
```

From C#, generate the embedding using an AI service (Azure OpenAI, for example), then pass it as a parameter.

### REST Endpoint Calls from T-SQL

Call external APIs directly from the database:

```sql
DECLARE @response NVARCHAR(MAX);
DECLARE @url NVARCHAR(4000) = 'https://api.example.com/enrich';

EXEC sp_invoke_external_rest_endpoint
    @url = @url,
    @method = 'POST',
    @payload = N'{"customerId": 42}',
    @response = @response OUTPUT;
```

This enables scenarios like data enrichment, webhook notifications, and AI model inference directly from T-SQL stored procedures.

---

## Conclusion

SQL Server is a deep, powerful, and continuously evolving database engine. As a .NET developer, your relationship with SQL Server goes far beyond writing LINQ queries. Understanding how the engine works — from locking and transactions to execution plans and indexing — makes you a dramatically more effective developer. It is the difference between guessing why something is slow and knowing.

SQL Server 2025 is the most capable release yet, with native JSON, vector search, REGEX, optimized locking, and AI integration. SSMS 22 gives you a modern, 64-bit environment with Copilot assistance and first-class support for all these new features. The go-sqlcmd tool makes command-line interactions seamless across Windows, macOS, and Linux.

Invest the time to learn these tools and concepts. Your future self — debugging a production issue at 2 AM or optimizing a critical query path — will thank you.
