---
title: "Load-Bearing Bugs, Part 2: The Complete Survival Guide to SQL Injection, web.config, IIS, Error Handling, Refactoring, Security, and Everything Else We Could Not Fit Into One Article"
date: 2026-05-16
author: myblazor-team
summary: "The sequel to our load-bearing bugs guide. This time we go deeper: a full SQL injection attack walkthrough, an exhaustive web.config reference, IIS application pool internals, Global.asax error handling, a step-by-step test-first refactoring of the broken code, source control archaeology with git blame and git bisect, the OWASP Top 10 2025 explained for ASP.NET developers, session hijacking and CSRF, how to write a good bug ticket, code review culture, deployment strategies, the strangler fig migration pattern, and blameless retrospectives."
featured: true
tags:
  - aspnet
  - dotnet
  - legacy-code
  - debugging
  - best-practices
  - deep-dive
  - software-engineering
  - sql-server
  - security
  - architecture
  - testing
  - csharp
  - iis
  - owasp
  - code-review
  - deployment
---

*This is Part 2 of our series on load-bearing bugs in legacy ASP.NET applications. If you have not read [Part 1](/blog/load-bearing-bugs-legacy-aspnet-guide), start there. It tells the story of a junior developer who finds a subtle bug during an infrastructure migration and has to decide whether to fix it. This article covers everything we could not fit into that first piece.*

---

## Part 1 — SQL Injection: The Full Attack Walkthrough

In Part 1, we mentioned that the `UserActivityLogger` has a SQL injection vulnerability. We told you it was dangerous. We told you to report it. But we did not show you exactly how dangerous it is. Let us fix that.

### The Vulnerable Code

Here is the code again:

```csharp
foreach (DictionaryEntry entry in items)
{
    sb.Append(", ");
    sb.Append(entry.Key.ToString());
    values.Append(", '");
    values.Append(SafeValue(entry.Value));
    values.Append("'");
}
```

And the `SafeValue` method:

```csharp
private string SafeValue(object value)
{
    try
    {
        if (value == null) return "";
        return value.ToString().Replace("'", "''");
    }
    catch
    {
        return "";
    }
}
```

The values are concatenated into the SQL string with single-quote escaping. The developers believed that replacing `'` with `''` would prevent SQL injection. Let us see if they were right.

### Attack 1: The Classic Single-Quote Escape

The simplest SQL injection attack involves terminating a string literal with a single quote, then appending arbitrary SQL. For example, if the `PageVisited` value is:

```
/Dashboard.aspx'; DROP TABLE UserActivity; --
```

After the `SafeValue` escaping, the single quotes are doubled:

```
/Dashboard.aspx''; DROP TABLE UserActivity; --
```

The generated SQL would be:

```sql
INSERT INTO UserActivity (UserId, ActivityDate, PageVisited) 
VALUES (@UserId, GETDATE(), '/Dashboard.aspx''; DROP TABLE UserActivity; --')
```

This is actually safe. The doubled single quote (`''`) is interpreted by SQL Server as a literal single quote character within the string. The `DROP TABLE` statement is treated as part of the string value, not as SQL code. The basic escaping works for this simple case.

### Attack 2: Unicode Smuggling

But what about Unicode? SQL Server supports Unicode strings with the `N` prefix. The code does not use `N` prefix for its string literals, which means the values are interpreted as `varchar` (non-Unicode). But the column might be `nvarchar` (Unicode).

Some SQL injection attacks exploit encoding differences. Consider the Unicode character U+02BC (MODIFIER LETTER APOSTROPHE: ʼ). This character looks very similar to a single quote but is not caught by the `Replace("'", "''")` call. In most SQL Server collations, this character is not treated as a string delimiter, so it is harmless. But in certain edge cases involving collation conversions or application-level string processing that normalizes Unicode, it could bypass the escaping.

More practically, the code is vulnerable to **second-order SQL injection**. Consider this scenario:

1. A user visits a page with a specially crafted URL: `/Page?param=harmless`
2. The `PageVisited` value is stored in the `UserActivity` table as `/Page?param=harmless`
3. Later, a reporting query reads the `PageVisited` value from the table and uses it in another dynamic SQL query without escaping

If the reporting query trusts the data in the `UserActivity` table (because "it is our own data, not user input"), the injected value can execute arbitrary SQL in the reporting context. This is second-order injection — the malicious payload is stored in the database during one operation and executed during a different operation.

### Attack 3: The Column Name Injection

Here is the attack vector that the developers probably did not think about. Look at the code again:

```csharp
sb.Append(entry.Key.ToString());
```

The **column name** is also being concatenated from the `Hashtable` key. The `SafeValue` method only escapes the **value**, not the key. If an attacker can control the keys in the `Hashtable`, they can inject arbitrary SQL into the column list.

In the current code, the `Hashtable` keys are set by the application code, not by user input. The keys are hardcoded strings like "PageVisited", "IPAddress", "LastAccessDate". An attacker cannot directly control these keys.

But what if a future developer adds a feature that includes user-controlled data in the key? Or what if the `PageVisited` value is accidentally used as a key instead of a value due to a copy-paste error? The code has no protection against key injection. A key like `UserId) VALUES ('admin', GETDATE()` would generate:

```sql
INSERT INTO UserActivity (UserId, ActivityDate, UserId) VALUES ('admin', GETDATE()) 
VALUES (@UserId, GETDATE(), 'whatever')
```

This would attempt to insert a row with `UserId = 'admin'`, potentially allowing an attacker to forge activity records for any user.

### The Only Real Fix: Parameterized Queries

The single-quote escaping is a band-aid. It works for the most common attack vectors, but it is not a complete defense. The only complete defense against SQL injection is parameterized queries:

```csharp
cmd.CommandText = @"
    INSERT INTO UserActivity 
        (UserId, ActivityDate, PageVisited, IPAddress, LastAccessDate, UserRole)
    VALUES 
        (@UserId, GETDATE(), @PageVisited, @IPAddress, @LastAccessDate, @UserRole)";

cmd.Parameters.AddWithValue("@UserId", userId);
cmd.Parameters.AddWithValue("@PageVisited", (object?)pageVisited ?? DBNull.Value);
cmd.Parameters.AddWithValue("@IPAddress", (object?)ipAddress ?? DBNull.Value);
cmd.Parameters.AddWithValue("@LastAccessDate", (object?)lastAccessDate ?? DBNull.Value);
cmd.Parameters.AddWithValue("@UserRole", (object?)userRole ?? DBNull.Value);
```

With parameterized queries, the SQL engine separates the SQL structure from the data values. The values are transmitted to the server in a separate data stream, and the server never attempts to parse them as SQL code. It does not matter what the values contain — single quotes, semicolons, `DROP TABLE` statements — they are all treated as data, never as code.

This is the fundamental principle: **SQL structure and SQL data must travel through separate channels.** When they share the same channel (string concatenation), injection is possible. When they are separated (parameterized queries), injection is impossible.

### A Note on AddWithValue

You may have heard that `AddWithValue` is bad. There is a well-known blog post titled "Can we stop using AddWithValue() already?" by Jorriss. The argument is that `AddWithValue` infers the SQL parameter type from the .NET type, and sometimes the inference is wrong:

- A C# `string` maps to `nvarchar`, but the column might be `varchar`. This can prevent index usage and cause performance problems.
- A C# `DateTime` maps to `datetime2`, but the column might be `datetime`. This can cause implicit conversions.

The recommended alternative is to specify the parameter type explicitly:

```csharp
var param = cmd.Parameters.Add("@PageVisited", SqlDbType.VarChar, 500);
param.Value = (object?)pageVisited ?? DBNull.Value;
```

This is better for performance, but it is more verbose. For the `UserActivityLogger` code, where the values are being logged (not queried), the performance difference is negligible. Using `AddWithValue` is fine here. But if you are building queries that filter or join on these columns, you should use explicit parameter types.

---

## Part 2 — The web.config File: A Complete Reference for the Terrified

The `web.config` file is the central configuration file for an ASP.NET 4.x application. It is an XML file that controls almost every aspect of the application's behavior. It is also one of the most intimidating files in the entire codebase, because it is long, it contains cryptic settings, and changing the wrong setting can break the application.

Let us demystify it.

### The Overall Structure

A web.config file has this general structure:

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <configSections>
    <!-- Custom configuration section declarations -->
  </configSections>

  <connectionStrings>
    <!-- Database connection strings -->
  </connectionStrings>

  <appSettings>
    <!-- Application-specific key-value pairs -->
  </appSettings>

  <system.web>
    <!-- ASP.NET Framework settings -->
  </system.web>

  <system.webServer>
    <!-- IIS 7+ settings (Integrated mode) -->
  </system.webServer>

  <system.diagnostics>
    <!-- Logging and tracing settings -->
  </system.diagnostics>

  <runtime>
    <!-- CLR runtime settings, assembly binding redirects -->
  </runtime>
</configuration>
```

Let us go through each section.

### connectionStrings

This section defines database connection strings. Every ASP.NET application that talks to a database has at least one connection string here:

```xml
<connectionStrings>
  <add name="MainDB" 
       connectionString="Data Source=10.0.2.45;Initial Catalog=MyAppDB;User ID=app_user;Password=S3cretP@ss;Connect Timeout=30;Encrypt=True;TrustServerCertificate=True"
       providerName="System.Data.SqlClient" />
  
  <add name="ReportingDB"
       connectionString="Data Source=10.0.2.46;Initial Catalog=MyAppReporting;Integrated Security=True"
       providerName="System.Data.SqlClient" />
</connectionStrings>
```

**Critical detail:** The connection string contains the database server's IP address. When the infrastructure team moves the database server to a new IP address, this connection string must be updated. This is one of the most common things that breaks during an infrastructure migration.

**Security concern:** Storing passwords in plaintext in the web.config is a security risk. Anyone who can read the file can see the database password. In modern applications, you would use the ASP.NET configuration encryption feature, Azure Key Vault, or environment variables. In legacy applications, the password is usually in plaintext, and everyone just tries not to think about it.

The connection string has many parameters. Here are the most important ones for SQL Server:

- `Data Source` (or `Server`): The server name or IP address. Can include a port: `10.0.2.45,1433` or a named instance: `10.0.2.45\SQLEXPRESS`.
- `Initial Catalog` (or `Database`): The database name.
- `User ID` and `Password`: SQL Server authentication credentials.
- `Integrated Security=True` (or `Trusted_Connection=True`): Use Windows authentication instead of SQL authentication. The application runs as the IIS application pool identity.
- `Connect Timeout` (default: 15 seconds): How long to wait for a connection before giving up.
- `Max Pool Size` (default: 100): The maximum number of connections in the connection pool.
- `Min Pool Size` (default: 0): The minimum number of connections kept in the pool.
- `MultipleActiveResultSets=True` (MARS): Allows multiple open result sets on the same connection. Required by Entity Framework, but can hide bugs where you forget to close a reader.
- `Encrypt=True`: Encrypt the connection to the database server.
- `TrustServerCertificate=True`: Trust the server's SSL certificate without validation. This is insecure but common in internal networks.

### appSettings

This section stores application-specific key-value pairs:

```xml
<appSettings>
  <add key="AdminEmail" value="admin@company.com" />
  <add key="MaxUploadSizeMB" value="10" />
  <add key="EnableAuditLogging" value="true" />
  <add key="LdapServer" value="10.0.1.10" />
  <add key="LdapDomain" value="CORP" />
  <add key="SmtpServer" value="smtp.internal.company.com" />
  <add key="SmtpPort" value="25" />
</appSettings>
```

You access these in code with:

```csharp
string adminEmail = ConfigurationManager.AppSettings["AdminEmail"];
int maxUpload = int.Parse(ConfigurationManager.AppSettings["MaxUploadSizeMB"]);
```

**Infrastructure migration note:** Any setting that references an IP address, hostname, or URL will need to be updated when servers move. Search the `appSettings` section for IP addresses and hostnames before the migration.

### system.web — The Core ASP.NET Configuration

This is the largest and most complex section. Here are the most important subsections:

**Authentication:**

```xml
<system.web>
  <authentication mode="Forms">
    <forms name=".MyAppAuth" loginUrl="/Login.aspx" timeout="30" 
           protection="All" requireSSL="true" slidingExpiration="true" />
  </authentication>
</system.web>
```

- `mode`: Can be `Forms` (cookie-based), `Windows` (NTLM/Kerberos), `None`, or `Passport` (obsolete).
- `loginUrl`: Where unauthenticated users are redirected.
- `timeout`: How long the authentication cookie is valid, in minutes.
- `slidingExpiration`: If true, the timeout resets with each request. If false, the cookie expires at a fixed time.

**Session State:**

```xml
<sessionState mode="StateServer" 
              stateConnectionString="tcpip=10.0.3.100:42424"
              timeout="30"
              cookieless="false" />
```

This is the setting that caused the issues in our story. The `mode` can be `InProc`, `StateServer`, `SQLServer`, `Custom`, or `Off`.

**Custom Errors:**

```xml
<customErrors mode="RemoteOnly" defaultRedirect="/Error.aspx">
  <error statusCode="404" redirect="/NotFound.aspx" />
  <error statusCode="500" redirect="/Error.aspx" />
</customErrors>
```

- `mode="On"`: Always show custom error pages (even to developers on the server).
- `mode="Off"`: Always show the detailed Yellow Screen of Death (including stack traces).
- `mode="RemoteOnly"`: Show custom error pages to remote users, but show the YSOD to users on the server itself.

**Security note:** Never set `customErrors mode="Off"` in production. The Yellow Screen of Death shows stack traces, file paths, configuration details, and sometimes even connection strings. This information is extremely valuable to an attacker. Always use `RemoteOnly` or `On`.

**Compilation:**

```xml
<compilation debug="false" targetFramework="4.7.2" />
```

- `debug="false"`: Compile in release mode. This enables optimizations and disables debug symbols.
- `debug="true"`: Compile in debug mode. Slower, uses more memory, but provides detailed error information.

**Never leave `debug="true"` in production.** Debug mode disables request timeouts, disables output caching, generates larger assemblies, and exposes more information in error messages. According to the OWASP Top 10 2025, security misconfiguration rose from position five to position two, and running with debug enabled in production is one of the most common misconfigurations.

**httpRuntime:**

```xml
<httpRuntime targetFramework="4.7.2" 
             maxRequestLength="10240"
             executionTimeout="300"
             enableVersionHeader="false"
             requestValidationMode="4.5" />
```

- `maxRequestLength`: Maximum allowed request size in kilobytes. Default is 4096 (4 MB). If users upload files, you may need to increase this. Note: IIS also has a separate limit (`maxAllowedContentLength` in `system.webServer/security/requestFiltering`) that must be increased as well. The two limits are independent, and the smaller one wins.
- `executionTimeout`: Maximum allowed execution time for a request, in seconds. Default is 110. If `debug="true"`, the timeout is ignored (infinite). This is another reason to never use debug mode in production.
- `enableVersionHeader`: If true (the default), ASP.NET adds an `X-AspNet-Version` header to every response, revealing the exact .NET Framework version. Set this to false in production — an attacker can use version information to find known vulnerabilities.
- `requestValidationMode`: Controls ASP.NET's built-in request validation, which rejects requests containing HTML or script tags. Setting it to "4.5" enables lazy validation (validation occurs only when you access the value, not when the request arrives). Setting it to "2.0" enables eager validation (validation occurs for all requests, before your code runs). Do not disable request validation unless you have a specific need and you are handling input sanitization yourself.

**Authorization:**

```xml
<authorization>
  <deny users="?" />
</authorization>
```

This is a deceptively simple but critically important setting. The `?` symbol means "anonymous users" — users who are not authenticated. This setting denies access to all unauthenticated users for the entire application. Without it, users can access any page without logging in.

You can override this for specific paths using `<location>` elements:

```xml
<!-- Allow anonymous access to the login page -->
<location path="Login.aspx">
  <system.web>
    <authorization>
      <allow users="*" />
    </authorization>
  </system.web>
</location>

<!-- Allow anonymous access to static files -->
<location path="Content">
  <system.web>
    <authorization>
      <allow users="*" />
    </authorization>
  </system.web>
</location>

<!-- Restrict admin pages to the Admin role -->
<location path="Admin">
  <system.web>
    <authorization>
      <allow roles="Administrators" />
      <deny users="*" />
    </authorization>
  </system.web>
</location>
```

Authorization rules are evaluated in order, from top to bottom. The first matching rule wins. If no rule matches, the default is to allow. This means the order of `<allow>` and `<deny>` elements matters:

```xml
<!-- CORRECT: deny first, then allow specific roles -->
<authorization>
  <allow roles="Administrators" />
  <deny users="*" />
</authorization>

<!-- WRONG: allow everyone first (this allows everyone, the deny is never reached) -->
<authorization>
  <allow users="*" />
  <deny roles="Guests" />
</authorization>
```

**Globalization:**

```xml
<globalization culture="en-US" uiCulture="en-US" 
               requestEncoding="utf-8" responseEncoding="utf-8" />
```

This setting controls the culture used for formatting numbers, dates, and currencies. It is directly relevant to our story: the `DateTime.Now.ToString()` call in the `SessionTracker` produces different output depending on the culture setting. On a server with `culture="en-US"`, it produces "3/17/2026 9:43:17 AM". On a server with `culture="de-DE"`, it produces "17.03.2026 09:43:17".

If you store formatted date strings in the database (as the `SessionTracker` code does), and then try to parse them later, the parsing will fail if the culture has changed. This is why you should always use `DateTime.ToString("o")` (ISO 8601 round-trip format: "2026-03-17T09:43:17.0000000") or store dates as `datetime` columns in the database instead of strings.

**Machine Key:**

```xml
<machineKey validationKey="AutoGenerate,IsolateApps"
            decryptionKey="AutoGenerate,IsolateApps"
            validation="HMACSHA256" 
            decryption="AES" />
```

The machine key is used to encrypt and sign ViewState, Forms Authentication tickets, and session state cookies. In a web farm (multiple servers behind a load balancer), all servers must use the **same** machine key. If the keys are different, a cookie encrypted by one server cannot be decrypted by another server, causing authentication failures and lost sessions.

`AutoGenerate,IsolateApps` generates a unique key for each application on each server. This is fine for a single-server deployment but will cause problems in a web farm. For a web farm, you must generate explicit keys and set them in the web.config on all servers:

```xml
<machineKey 
  validationKey="A1B2C3D4E5F6...long hex string..." 
  decryptionKey="F6E5D4C3B2A1...another hex string..."
  validation="HMACSHA256" 
  decryption="AES" />
```

This is another common source of problems during infrastructure migrations, especially when moving from a single server to a web farm.

### system.webServer — IIS Configuration

This section configures IIS directly. It is used when the application runs in IIS Integrated mode:

```xml
<system.webServer>
  <modules>
    <add name="SessionTracker" type="MyApp.Modules.SessionTracker, MyApp" />
  </modules>

  <handlers>
    <add name="ReportHandler" path="*.report" verb="GET"
         type="MyApp.Handlers.ReportHandler, MyApp" />
  </handlers>

  <httpErrors errorMode="Custom">
    <remove statusCode="404" />
    <error statusCode="404" path="/NotFound.aspx" responseMode="ExecuteURL" />
  </httpErrors>

  <security>
    <requestFiltering>
      <requestLimits maxAllowedContentLength="10485760" />
    </requestFiltering>
  </security>
</system.webServer>
```

**Note the difference:** `system.web/customErrors` controls ASP.NET error pages. `system.webServer/httpErrors` controls IIS error pages. They can conflict if both are configured. For IIS 7+ in Integrated mode, `httpErrors` takes precedence for static content and non-ASP.NET requests.

---

## Part 3 — IIS Application Pools: The Invisible Container

Your ASP.NET application does not run directly inside IIS. It runs inside an **application pool**, which is a worker process (`w3wp.exe`) that IIS manages. Understanding application pools is critical for understanding how your application behaves in production.

### What Is an Application Pool?

An application pool is an isolated execution environment for your web application. Each application pool runs in its own `w3wp.exe` process, with its own memory space, its own thread pool, and its own configuration. If one application pool crashes, it does not affect other application pools on the same server.

Think of it like an apartment building. Each apartment (application pool) has its own walls, its own plumbing, and its own electricity. If one apartment has a water leak, the other apartments are (mostly) unaffected.

### Application Pool Recycling

IIS periodically **recycles** application pools — it shuts down the worker process and starts a new one. This is similar to restarting the application, but it happens automatically.

Why does IIS do this? Because long-running processes can accumulate problems:

- **Memory leaks.** If your code has a memory leak (objects that are allocated but never garbage collected), the process's memory usage grows over time. Recycling starts a fresh process with no leaked memory.
- **Handle leaks.** Similar to memory leaks, but for operating system handles (file handles, socket handles, etc.).
- **Thread pool exhaustion.** If your code has threads that are stuck (waiting for a response that never comes), the thread pool gradually fills up until no threads are available to handle new requests.
- **Stale state.** Static variables, in-memory caches, and other process-level state can become stale or corrupted over time.

The default recycling settings in IIS are:

- **Regular time interval:** Recycle every 1,740 minutes (29 hours). This prevents the recycling from happening at the same time every day.
- **Specific time:** You can configure specific times for recycling (e.g., 3:00 AM).
- **Memory limit:** Recycle when the process exceeds a virtual memory or private memory threshold.
- **Request limit:** Recycle after a specific number of requests.

**What happens to in-flight requests during recycling?** IIS uses a technique called **overlapping recycling**. When a recycle is triggered, IIS starts a new worker process while the old one is still running. New requests go to the new process. The old process is given a shutdown timeout (default: 90 seconds) to finish processing any in-flight requests. After the timeout, the old process is forcibly terminated.

**What happens to InProc session state during recycling?** It is destroyed. All session data is lost. Users will be logged out (if authentication relies on session state) or will lose their in-progress work (if form data is stored in session state). This is one of the main reasons to use StateServer or SQLServer session state in production.

### Application Pool Identity

Every application pool runs as a specific Windows user account. The identity determines what the application can access on the file system, the network, and other resources.

- **ApplicationPoolIdentity (default):** A virtual account created by IIS. Each application pool gets its own unique identity. This is the most secure option because it provides process isolation.
- **NetworkService:** A built-in Windows account with limited privileges. All application pools using NetworkService share the same identity, which reduces isolation.
- **LocalSystem:** A highly privileged account. Using this is a security risk.
- **Custom account:** A specific domain or local account. Used when the application needs to access network resources (like a file share or a database using Windows authentication).

### Common Application Pool Problems

**Problem 1: Application pool stops unexpectedly.** IIS can disable an application pool if it crashes too many times within a time window. The default is 5 crashes in 5 minutes. When this happens, all requests to the application return a 503 Service Unavailable error. Check the Windows Event Log for the cause of the crashes.

**Problem 2: Application pool recycles during business hours.** If the default 29-hour recycle interval causes a recycle during peak usage, users will experience a brief delay (the first request after recycling is slower because the application must reinitialize). Configure a specific recycling time during off-peak hours.

**Problem 3: Application pool identity cannot access resources.** If the application needs to read files, write to a folder, or access a network share, the application pool identity must have the appropriate permissions. A common error after an infrastructure migration is that the new server's file permissions do not match the old server's permissions.

---

## Part 4 — Reading IIS Logs and the Windows Event Viewer

When something goes wrong in an ASP.NET application, the application's own logs are not the only source of information. IIS keeps its own logs, and Windows keeps the Event Log. You need to know how to read all three.

### IIS Logs

IIS writes a log file for every HTTP request it handles. By default, the logs are stored in `C:\inetpub\logs\LogFiles\W3SVC<site-id>\`, where `<site-id>` is the IIS site ID (usually 1 for the first site). Each log file covers one day and is named `u_exYYMMDD.log`.

The default log format is W3C Extended, and it looks like this:

```
#Software: Microsoft Internet Information Services 10.0
#Version: 1.0
#Date: 2026-03-17 00:00:01
#Fields: date time s-ip cs-method cs-uri-stem cs-uri-query s-port cs-username c-ip cs(User-Agent) cs(Referer) sc-status sc-substatus sc-win32-status time-taken
2026-03-17 08:01:15 10.0.2.10 GET /Dashboard.aspx - 80 DOMAIN\jsmith 10.0.1.50 Mozilla/5.0+(Windows+NT+10.0;+Win64;+x64) https://myapp.com/Home.aspx 200 0 0 1250
2026-03-17 08:01:16 10.0.2.10 POST /Admin/EditUser.aspx userId=42 80 DOMAIN\admin1 10.0.1.51 Mozilla/5.0+(Windows+NT+10.0;+Win64;+x64) https://myapp.com/Admin/Users.aspx 500 0 0 4523
2026-03-17 08:01:17 10.0.2.10 GET /Content/style.css - 80 - 10.0.1.50 Mozilla/5.0+(Windows+NT+10.0;+Win64;+x64) https://myapp.com/Dashboard.aspx 200 0 0 15
```

Here is what each field means:

- `date` and `time`: When the request was processed (UTC).
- `s-ip`: The server's IP address.
- `cs-method`: The HTTP method (GET, POST, PUT, DELETE).
- `cs-uri-stem`: The URL path.
- `cs-uri-query`: The query string (everything after the `?`).
- `s-port`: The port number (80 for HTTP, 443 for HTTPS).
- `cs-username`: The authenticated user (blank for anonymous requests).
- `c-ip`: The client's IP address.
- `cs(User-Agent)`: The client's browser/device information.
- `cs(Referer)`: The page that linked to this request.
- `sc-status`: The HTTP status code. **This is the most important field.**
- `sc-substatus`: The IIS substatus code (provides more detail about errors).
- `sc-win32-status`: The Windows error code (0 means success).
- `time-taken`: How long the request took, in milliseconds.

**Key status codes to watch for:**

- `200`: Success. Everything is fine.
- `301` or `302`: Redirect. Usually fine, but excessive redirects can indicate a misconfiguration.
- `304`: Not Modified. The browser's cache is valid. This is good — it means the server did not have to send the full response.
- `401`: Unauthorized. The user is not authenticated. This can be normal (the user has not logged in yet) or abnormal (authentication is broken).
- `403`: Forbidden. The user is authenticated but does not have permission. Check your authorization rules.
- `404`: Not Found. The URL does not map to a page or resource. Occasional 404s are normal (users mistype URLs). A sudden spike in 404s can indicate a broken link or a deployment problem.
- `500`: Internal Server Error. Something crashed. **This is the one you most need to watch after a migration.**
- `503`: Service Unavailable. The application pool is stopped or recycling. This is a serious problem.

**Useful command-line analysis:**

To find all 500 errors in the last day's log:

```powershell
Select-String -Path "C:\inetpub\logs\LogFiles\W3SVC1\u_ex260317.log" -Pattern " 500 "
```

To count the number of requests per status code:

```powershell
Get-Content "C:\inetpub\logs\LogFiles\W3SVC1\u_ex260317.log" | 
  Where-Object { $_ -notmatch "^#" } |
  ForEach-Object { ($_ -split ' ')[11] } |
  Group-Object | 
  Sort-Object Count -Descending |
  Format-Table Name, Count
```

To find the slowest requests (time-taken > 5000ms):

```powershell
Get-Content "C:\inetpub\logs\LogFiles\W3SVC1\u_ex260317.log" |
  Where-Object { $_ -notmatch "^#" } |
  Where-Object { [int]($_ -split ' ')[-1] -gt 5000 } |
  ForEach-Object { 
    $parts = $_ -split ' '
    "$($parts[0]) $($parts[1]) $($parts[4]) $($parts[-1])ms"
  }
```

### The Windows Event Log

The Windows Event Log is a centralized repository for events from all applications and services running on the server. You access it through Event Viewer (`eventvwr.msc`).

For ASP.NET issues, look in these locations:

- **Application log** (`Windows Logs > Application`): Contains events from ASP.NET, IIS, .NET Runtime, and your application.
- **System log** (`Windows Logs > System`): Contains events from Windows services, including the ASP.NET State Service.

Filter the Application log by source:

- **ASP.NET 4.0.xxxxx**: Unhandled exceptions in ASP.NET applications.
- **.NET Runtime**: CLR errors, including `StackOverflowException`, `OutOfMemoryException`, and assembly loading failures.
- **WAS (Windows Process Activation Service)**: Application pool events — starts, stops, crashes, recycling.
- **W3SVC (World Wide Web Publishing Service)**: IIS service events.

Each event has an **Event ID**, a **Level** (Information, Warning, Error, Critical), and a **Description** that contains the details. For ASP.NET exceptions, the description includes the exception type, message, and stack trace.

Common Event IDs to watch for:

- **Event ID 1309** (source: ASP.NET): An unhandled exception occurred during the execution of the current web request. The description contains the full exception details.
- **Event ID 1325** (source: ASP.NET): ASP.NET runtime error. Often seen when the application cannot start.
- **Event ID 5011** (source: WAS): A process serving application pool 'MyAppPool' suffered a fatal communication error with the Windows Process Activation Service.
- **Event ID 5012** (source: WAS): Application pool 'MyAppPool' is being automatically disabled due to a series of failures.
- **Event ID 5013** (source: WAS): Application pool 'MyAppPool' has been disabled because of a series of failures.

If Event ID 5012 or 5013 appears, your application pool has been disabled by IIS's Rapid Fail Protection. This means the worker process crashed too many times in a short period. Check the Application log for the underlying exception that is causing the crashes.

---

## Part 5 — Error Handling in ASP.NET: Global.asax, ELMAH, and the Yellow Screen of Death

When something goes wrong in your ASP.NET application, there are multiple layers of error handling that determine what the user sees and what gets logged. Let us walk through all of them.

### Layer 1: Try-Catch in Your Code

The most basic error handling is the `try-catch` block in your own code:

```csharp
try
{
    var result = _repository.GetUserById(userId);
    // Use result...
}
catch (SqlException ex) when (ex.Number == -2) // Timeout
{
    Logger.Warn($"Database timeout getting user {userId}", ex);
    ShowTimeoutMessage();
}
catch (SqlException ex)
{
    Logger.Error($"Database error getting user {userId}", ex);
    ShowGenericErrorMessage();
}
catch (Exception ex)
{
    Logger.Error($"Unexpected error getting user {userId}", ex);
    throw; // Re-throw to let the global handler deal with it
}
```

Best practices for try-catch:

- **Catch specific exceptions first**, then general exceptions.
- **Do not catch and swallow.** If you catch an exception and do nothing with it, you are hiding problems. At minimum, log the exception.
- **Use `throw;` to re-throw**, not `throw ex;`. Using `throw ex;` resets the stack trace, making it harder to find the original source of the error. Using `throw;` preserves the original stack trace.
- **Do not use exceptions for control flow.** Checking `if (user == null)` is much faster than catching a `NullReferenceException`.

### Layer 2: Global.asax Application_Error

The `Global.asax` file contains application-level event handlers. The `Application_Error` event fires whenever an unhandled exception occurs anywhere in the application:

```csharp
// Global.asax.cs
protected void Application_Error(object sender, EventArgs e)
{
    Exception exception = Server.GetLastError();

    // Log the exception
    Logger.Error("Unhandled exception", exception);

    // Optionally clear the error and redirect to a custom error page
    // Server.ClearError();
    // Response.Redirect("~/Error.aspx");
}
```

This is your application's last line of defense. If an exception propagates all the way up the call stack without being caught, `Application_Error` catches it. This is where you should log unhandled exceptions.

**Important:** If you call `Server.ClearError()`, the exception is considered "handled" and the custom error page mechanism kicks in. If you do not call `Server.ClearError()`, the exception continues to propagate and the ASP.NET error handling takes over (showing the YSOD or the custom error page, depending on the `customErrors` configuration).

### Layer 3: ELMAH

ELMAH (Error Logging Modules and Handlers) is a third-party library that automatically logs all unhandled exceptions without any code changes. You install it via NuGet, add a few lines to the web.config, and it starts working immediately.

```xml
<system.web>
  <httpModules>
    <add name="ErrorLog" type="Elmah.ErrorLogModule, Elmah" />
    <add name="ErrorMail" type="Elmah.ErrorMailModule, Elmah" />
    <add name="ErrorFilter" type="Elmah.ErrorFilterModule, Elmah" />
  </httpModules>
</system.web>

<system.webServer>
  <modules>
    <add name="ErrorLog" type="Elmah.ErrorLogModule, Elmah" preCondition="managedHandler" />
  </modules>
</system.webServer>

<elmah>
  <errorLog type="Elmah.XmlFileErrorLog, Elmah" logPath="~/App_Data/Elmah" />
  <security allowRemoteAccess="false" />
</elmah>
```

Once configured, you can access the ELMAH error log by navigating to `https://yourapp.com/elmah.axd`. This shows a list of all unhandled exceptions with full stack traces, request details, server variables, and form data.

**Security warning:** Set `allowRemoteAccess="false"` and additionally protect the `elmah.axd` path with authorization rules. ELMAH exposes detailed error information that could be useful to an attacker:

```xml
<location path="elmah.axd">
  <system.web>
    <authorization>
      <allow roles="Administrators" />
      <deny users="*" />
    </authorization>
  </system.web>
</location>
```

### Layer 4: The Yellow Screen of Death

The Yellow Screen of Death (YSOD) is ASP.NET's default error page. It shows:

- The exception type and message.
- The full stack trace with file paths and line numbers.
- The source code around the line that caused the error (if the source is available).
- Server variables, request details, and configuration information.

The YSOD is incredibly useful for debugging. It is also incredibly dangerous in production, because it exposes internal implementation details to anyone who can trigger an error.

The `customErrors` setting controls whether the YSOD is shown:

- `mode="Off"`: YSOD is always shown. **Never use this in production.**
- `mode="On"`: YSOD is never shown. Custom error pages are always shown.
- `mode="RemoteOnly"`: YSOD is shown only to users on the server itself (via localhost). Remote users see custom error pages.

For production, use `mode="RemoteOnly"` or `mode="On"`. For development, use `mode="Off"` (but only on your development machine, never on a shared server).

---

## Part 6 — Source Control Archaeology: Finding Out Who Wrote This and Why

In Part 1, we mentioned checking the source control history to understand why code was written. Let us get specific about how to do that.

### git blame

The `git blame` command shows who last modified each line of a file, and when. It is the single most useful command for understanding the history of a specific piece of code:

```bash
git blame src/MyApp/DataAccess/UserActivityLogger.cs
```

Output:

```
a1b2c3d4 (dthompson  2011-08-15 14:22:03 -0500  1) public class UserActivityLogger
a1b2c3d4 (dthompson  2011-08-15 14:22:03 -0500  2) {
a1b2c3d4 (dthompson  2011-08-15 14:22:03 -0500  3)     private readonly string _connectionString;
e5f6a7b8 (rpatil     2014-03-12 09:15:41 -0500  4)     
e5f6a7b8 (rpatil     2014-03-12 09:15:41 -0500  5)     public UserActivityLogger(string connectionString)
a1b2c3d4 (dthompson  2011-08-15 14:22:03 -0500  6)     {
...
c9d0e1f2 (mchen      2016-06-22 11:30:17 -0500 87)     public void LogUserActivityBatch(...)
c9d0e1f2 (mchen      2016-06-22 11:30:17 -0500 88)     {
...
```

Each line shows: the commit hash, the author, the date, and the line content. You can see that `dthompson` wrote the original code in 2011, `rpatil` modified it in 2014, and `mchen` added the batch method in 2016.

To see the full commit message for a specific commit:

```bash
git log -1 c9d0e1f2
```

Output:

```
commit c9d0e1f2a3b4c5d6e7f8a9b0c1d2e3f4a5b6c7d8
Author: mchen <mchen@company.com>
Date:   Wed Jun 22 11:30:17 2016 -0500

    Add batch activity logging for admin actions (JIRA-4521)
    
    Admin actions now log multiple activity items in a single database
    round-trip instead of individual inserts. This reduces the number
    of database calls for admin bulk operations.
```

The commit message references JIRA-4521. You can look up that ticket in your issue tracker to find more context about why the batch method was added.

### git log with Path Filtering

To see the full history of changes to a specific file:

```bash
git log --follow --oneline -- src/MyApp/DataAccess/UserActivityLogger.cs
```

Output:

```
c9d0e1f (mchen)     Add batch activity logging for admin actions (JIRA-4521)
e5f6a7b (rpatil)    Add admin action logging
a1b2c3d (dthompson) Initial implementation of user activity logging
```

The `--follow` flag tells git to follow renames. If the file was moved or renamed at some point, `--follow` will show the history from before the rename as well.

To see the actual changes (diffs) for each commit:

```bash
git log --follow -p -- src/MyApp/DataAccess/UserActivityLogger.cs
```

This shows the full diff for each commit, so you can see exactly what was added, removed, or changed.

### git bisect

`git bisect` is a powerful tool for finding exactly which commit introduced a bug. It uses binary search to narrow down the offending commit.

```bash
# Start bisecting
git bisect start

# Mark the current version as bad (the bug exists)
git bisect bad

# Mark an older version as good (the bug does not exist)
git bisect good a1b2c3d

# Git checks out a commit halfway between good and bad
# Test whether the bug exists at this commit
# If it does:
git bisect bad
# If it does not:
git bisect good

# Repeat until git finds the exact commit
# Git will output something like:
# c9d0e1f2 is the first bad commit
```

This is extremely useful when you know that something used to work and now it does not, but you do not know which of the 50 commits between then and now broke it.

For the bug in our story, you could use `git bisect` to confirm that the missing `NULL` was introduced in commit `c9d0e1f2` by `mchen` in 2016. But since `git blame` already pointed to that commit, `git bisect` is not necessary in this case.

---

## Part 7 — The OWASP Top 10 (2025 Edition) for ASP.NET Developers

The OWASP Top 10 is a standard awareness document that identifies the most critical security risks to web applications. It represents a broad consensus about the most critical security risks to web applications, and the 2025 version is the most current edition. Every ASP.NET developer should know what is on this list and how it applies to their code.

Here is the 2025 list, with specific guidance for ASP.NET 4.x applications:

### A01:2025 — Broken Access Control

This remains the number one risk. Broken Access Control maintains its position at the top, with an average of 3.73 percent of applications tested showing one or more of the 40 CWEs in this category.

In ASP.NET 4.x, broken access control often manifests as:

- Pages that check authentication but not authorization. The user is logged in, but they should not have access to this page.
- URL manipulation: changing `/Reports/MyReport?userId=42` to `/Reports/MyReport?userId=43` to access another user's data.
- Missing `[Authorize]` attributes on Web API endpoints.

**What to check in your application:** Every page that displays user-specific data should verify that the current user is authorized to see that data. Do not rely on "the user cannot get to this page without clicking through the correct menu." URLs can be typed directly into the address bar.

### A02:2025 — Security Misconfiguration

Security Misconfiguration moved up from position five in 2021 to position two in 2025.

In ASP.NET 4.x, common misconfigurations include:

- `customErrors mode="Off"` in production (exposes stack traces).
- `compilation debug="true"` in production (disables optimizations, enables verbose errors).
- Default machine keys (allows ViewState tampering across servers).
- ELMAH accessible without authentication.
- Trace.axd enabled in production (exposes detailed request traces).

### A03:2025 — Software Supply Chain Failures

This is a new category that replaced "Vulnerable and Outdated Components." It covers risks from third-party libraries, NuGet packages, and build pipeline integrity.

In ASP.NET 4.x, this means:

- Using NuGet packages with known vulnerabilities. Run `dotnet list package --vulnerable` to check.
- Using outdated packages that no longer receive security patches.
- Not verifying the integrity of packages (NuGet does have package signing, but it is not widely enforced).

### A05:2025 — Injection

Injection dropped from position three to position five, but it is still extremely relevant. The 2025 edition noted that injection fell from the third to fifth position as frameworks improve, but it still causes massive damage with over 14,000 CVEs.

The `UserActivityLogger` code in our story is a textbook injection vulnerability. It concatenates user-controllable values into SQL strings without parameterization. The fix: parameterized queries, always.

### A10:2025 — Mishandling of Exceptional Conditions

Mishandling of Exceptional Conditions is a new category for 2025, covering poorly handled errors that can create stack trace leaks, deplete resources, or leave systems in an insecure state.

This is directly relevant to our story. The empty catch block, the missing null check, the malformed SQL — all of these are examples of mishandling exceptional conditions. The OWASP guidance is that applications must "fail securely" — when something goes wrong, the application should move to a safe state, not an undefined state.

The null check in the `SessionTracker` is actually a good example of secure failure handling: when the session is in an uncertain state, the code stops and does not proceed with the logging operation. The batch method's missing `NULL` is a bad example: when a value is null, the code produces invalid SQL and crashes, which is not a controlled failure.

---

## Part 8 — Session Hijacking and CSRF: The Security Threats You Need to Understand

The load-bearing bug in our story prevents activity from being logged when the session is in an unknown state. But what could cause the session to be in an unknown state, other than the StateServer being down? Two common attack vectors are session hijacking and CSRF.

### Session Hijacking

Session hijacking is when an attacker obtains a legitimate user's session ID and uses it to impersonate that user. There are several ways an attacker can obtain a session ID:

**1. Session ID in the URL.** If the application uses "cookieless" sessions, the session ID is embedded in the URL: `https://myapp.com/(S(abc123))/Dashboard.aspx`. Anyone who sees the URL (in a log, in a referrer header, over someone's shoulder) can use the session ID.

**2. Cross-site scripting (XSS).** If the application has an XSS vulnerability, an attacker can inject JavaScript that reads the session cookie and sends it to the attacker's server: `document.cookie`.

**3. Network sniffing.** If the application does not use HTTPS (or uses HTTPS with weak ciphers), an attacker on the same network can intercept the session cookie.

**4. Session fixation.** The attacker creates a session on the application, obtains the session ID, and tricks the victim into using that session ID. If the application does not regenerate the session ID after authentication, the attacker now shares a session with the victim.

**Defenses in ASP.NET 4.x:**

```xml
<!-- Always use cookies for session IDs, never URLs -->
<sessionState cookieless="false" />

<!-- Mark cookies as HttpOnly (not accessible from JavaScript) -->
<httpCookies httpOnlyCookies="true" />

<!-- Mark cookies as Secure (only sent over HTTPS) -->
<httpCookies requireSSL="true" />

<!-- Regenerate session ID after authentication -->
```

ASP.NET does not have a built-in method to regenerate the session ID. A common workaround is to abandon the old session and create a new one after login:

```csharp
// After successful authentication
string tempData = Session["ImportantData"] as string;
Session.Abandon();
// ASP.NET will create a new session with a new ID on the next request
// Store a flag in a cookie to migrate data on the next request
Response.Cookies.Add(new HttpCookie("SessionMigrate", "true"));
```

### Cross-Site Request Forgery (CSRF)

CSRF is an attack where a malicious website tricks a user's browser into making a request to your application. Because the browser automatically includes cookies (including the session cookie), the request appears to come from the authenticated user.

For example, a malicious page could contain:

```html
<img src="https://yourapp.com/Admin/DeleteUser?userId=42" />
```

When the user visits the malicious page, their browser sends a GET request to your application, including the session cookie. If the user is logged in as an admin, the user with ID 42 is deleted.

**Defenses in ASP.NET 4.x:**

1. **Use anti-forgery tokens.** ASP.NET MVC has built-in support with `@Html.AntiForgeryToken()` and `[ValidateAntiForgeryToken]`. Web Forms can use `ViewStateUserKey`:

```csharp
// In Page_Init
protected override void OnInit(EventArgs e)
{
    base.OnInit(e);
    ViewStateUserKey = Session.SessionID;
}
```

2. **Use POST for state-changing operations.** GET requests should never change data. The `DeleteUser` endpoint above should require a POST request.

3. **Validate the Referer header.** Check that the request came from your own domain. This is not foolproof (the Referer header can be suppressed), but it adds a layer of defense.

---

## Part 9 — The Step-by-Step Refactoring: Test First, Then Fix

In Part 1, we talked about the importance of writing tests before changing legacy code. Now let us actually do it. We will take the broken `LogUserActivityBatch` method and fix it properly, step by step.

### Step 1: Make the Code Testable

The original method directly calls `SqlCommand.ExecuteNonQuery()`, which means it needs a real database connection to run. We cannot unit test it. We need to extract the SQL construction into a separate, testable method.

Create a new file: `UserActivitySqlBuilder.cs`

```csharp
namespace MyApp.DataAccess
{
    /// <summary>
    /// Builds SQL statements for user activity logging.
    /// Extracted from UserActivityLogger to enable unit testing.
    /// </summary>
    public static class UserActivitySqlBuilder
    {
        /// <summary>
        /// Builds a single INSERT statement for a user activity record.
        /// Returns the SQL string and a dictionary of parameter names to values.
        /// </summary>
        public static (string Sql, Dictionary<string, object?> Parameters) BuildInsert(
            string userId, Dictionary<string, string?> items)
        {
            var columns = new List<string> { "UserId", "ActivityDate" };
            var paramNames = new List<string> { "@UserId", "GETDATE()" };
            var parameters = new Dictionary<string, object?>
            {
                ["@UserId"] = userId
            };

            int paramIndex = 0;
            foreach (var kvp in items)
            {
                string paramName = $"@p{paramIndex}";
                columns.Add(kvp.Key);
                paramNames.Add(paramName);
                parameters[paramName] = (object?)kvp.Value ?? DBNull.Value;
                paramIndex++;
            }

            string sql = $"INSERT INTO UserActivity ({string.Join(", ", columns)}) " +
                         $"VALUES ({string.Join(", ", paramNames)})";

            return (sql, parameters);
        }

        /// <summary>
        /// Builds batch INSERT statements for multiple activity records.
        /// Returns the SQL string and a dictionary of parameter names to values.
        /// </summary>
        public static (string Sql, Dictionary<string, object?> Parameters) BuildBatchInsert(
            string userId, List<Dictionary<string, string?>> activityList)
        {
            var allSql = new StringBuilder();
            var allParameters = new Dictionary<string, object?>
            {
                ["@UserId"] = userId
            };

            for (int batchIndex = 0; batchIndex < activityList.Count; batchIndex++)
            {
                var items = activityList[batchIndex];
                var columns = new List<string> { "UserId", "ActivityDate" };
                var paramNames = new List<string> { "@UserId", "GETDATE()" };

                int paramIndex = 0;
                foreach (var kvp in items)
                {
                    string paramName = $"@b{batchIndex}p{paramIndex}";
                    columns.Add(kvp.Key);
                    paramNames.Add(paramName);
                    allParameters[paramName] = (object?)kvp.Value ?? DBNull.Value;
                    paramIndex++;
                }

                allSql.AppendLine(
                    $"INSERT INTO UserActivity ({string.Join(", ", columns)}) " +
                    $"VALUES ({string.Join(", ", paramNames)});");
            }

            return (allSql.ToString(), allParameters);
        }
    }
}
```

### Step 2: Write Tests for the New Code

```csharp
using Xunit;

namespace MyApp.Tests.DataAccess
{
    public class UserActivitySqlBuilderTests
    {
        [Fact]
        public void BuildInsert_WithAllValues_ProducesValidSql()
        {
            var items = new Dictionary<string, string?>
            {
                ["PageVisited"] = "/Dashboard.aspx",
                ["IPAddress"] = "10.0.1.50",
                ["LastAccessDate"] = "2026-03-17 09:00:00"
            };

            var (sql, parameters) = UserActivitySqlBuilder.BuildInsert("jsmith", items);

            Assert.Contains("INSERT INTO UserActivity", sql);
            Assert.Contains("UserId", sql);
            Assert.Contains("PageVisited", sql);
            Assert.Contains("@UserId", sql);
            Assert.Equal("jsmith", parameters["@UserId"]);
            Assert.Equal("/Dashboard.aspx", parameters["@p0"]);
        }

        [Fact]
        public void BuildInsert_WithNullValue_UsesDbNull()
        {
            var items = new Dictionary<string, string?>
            {
                ["PageVisited"] = "/Dashboard.aspx",
                ["LastAccessDate"] = null
            };

            var (sql, parameters) = UserActivitySqlBuilder.BuildInsert("jsmith", items);

            Assert.Contains("LastAccessDate", sql);
            Assert.Equal(DBNull.Value, parameters["@p1"]);
        }

        [Fact]
        public void BuildInsert_WithEmptyItems_ProducesMinimalSql()
        {
            var items = new Dictionary<string, string?>();

            var (sql, parameters) = UserActivitySqlBuilder.BuildInsert("jsmith", items);

            Assert.Equal(
                "INSERT INTO UserActivity (UserId, ActivityDate) VALUES (@UserId, GETDATE())",
                sql);
            Assert.Single(parameters); // Only @UserId
        }

        [Fact]
        public void BuildInsert_SqlInjectionInValue_IsParameterized()
        {
            var items = new Dictionary<string, string?>
            {
                ["PageVisited"] = "'; DROP TABLE UserActivity; --"
            };

            var (sql, parameters) = UserActivitySqlBuilder.BuildInsert("jsmith", items);

            // The malicious value should be in the parameters, not in the SQL
            Assert.DoesNotContain("DROP TABLE", sql);
            Assert.Equal("'; DROP TABLE UserActivity; --", parameters["@p0"]);
        }

        [Fact]
        public void BuildBatchInsert_WithNullValues_UsesDbNull()
        {
            var activityList = new List<Dictionary<string, string?>>
            {
                new()
                {
                    ["AdminAction"] = "EditUser",
                    ["TargetId"] = "12345",
                    ["LastAccessDate"] = null,
                    ["SessionToken"] = null
                }
            };

            var (sql, parameters) = UserActivitySqlBuilder
                .BuildBatchInsert("jsmith", activityList);

            // The SQL should use parameter placeholders, not empty commas
            Assert.DoesNotContain(", , ", sql);
            Assert.DoesNotContain(", )", sql);

            // Null values should be DBNull
            Assert.Equal(DBNull.Value, parameters["@b0p2"]);
            Assert.Equal(DBNull.Value, parameters["@b0p3"]);
        }

        [Fact]
        public void BuildBatchInsert_MultipleRecords_ProducesMultipleInserts()
        {
            var activityList = new List<Dictionary<string, string?>>
            {
                new() { ["Action"] = "View" },
                new() { ["Action"] = "Edit" }
            };

            var (sql, parameters) = UserActivitySqlBuilder
                .BuildBatchInsert("jsmith", activityList);

            // Should contain two INSERT statements
            Assert.Equal(2, sql.Split("INSERT INTO").Length - 1);
            Assert.Equal("View", parameters["@b0p0"]);
            Assert.Equal("Edit", parameters["@b1p0"]);
        }
    }
}
```

### Step 3: Run the Tests

All six tests should pass. The new code uses parameterized queries, so:

- Null values become `DBNull.Value` parameters, not empty commas.
- SQL injection payloads are stored as parameter values, not interpreted as SQL.
- The SQL structure is always valid, regardless of the data values.

### Step 4: Update the Logger to Use the New Builder

Now modify `UserActivityLogger` to use the new builder:

```csharp
public void LogUserActivity(string userId, Dictionary<string, string?> items)
{
    var (sql, parameters) = UserActivitySqlBuilder.BuildInsert(userId, items);

    using (var conn = new SqlConnection(_connectionString))
    {
        conn.Open();
        using (var cmd = new SqlCommand(sql, conn))
        {
            foreach (var kvp in parameters)
            {
                cmd.Parameters.AddWithValue(kvp.Key, kvp.Value ?? DBNull.Value);
            }
            cmd.ExecuteNonQuery();
        }
    }
}

public void LogUserActivityBatch(string userId, List<Dictionary<string, string?>> activityList)
{
    var (sql, parameters) = UserActivitySqlBuilder.BuildBatchInsert(userId, activityList);

    using (var conn = new SqlConnection(_connectionString))
    {
        conn.Open();
        using (var cmd = new SqlCommand(sql, conn))
        {
            foreach (var kvp in parameters)
            {
                cmd.Parameters.AddWithValue(kvp.Key, kvp.Value ?? DBNull.Value);
            }
            cmd.ExecuteNonQuery();
        }
    }
}
```

### Step 5: Update the Callers

Change the callers from `Hashtable` to `Dictionary<string, string?>`:

```csharp
// Before (in SessionTracker)
Hashtable items = new Hashtable();
items.Add("PageVisited", pageUrl);

// After
var items = new Dictionary<string, string?>
{
    ["PageVisited"] = pageUrl,
    ["IPAddress"] = ipAddress
};

string? strLastAccessDate = context.Session["LastAccessDate"] as string;
// NOTE: If LastAccessDate is null, the session may be expired or invalid.
// We intentionally skip logging in this case rather than supplying a default.
// See Ticket #4521 discussion and incident report 2026-03-17 for context.
if (!string.IsNullOrEmpty(strLastAccessDate))
    items["LastAccessDate"] = strLastAccessDate;
```

### Step 6: Deploy and Monitor

Deploy the changes during a maintenance window. Monitor the logs for the next 24 hours. Verify that:

- Activity records are being written to the database.
- No SQL syntax errors appear in the logs.
- The null-value case (empty session) is handled correctly (DBNull.Value inserted, or column skipped, depending on the design decision).

---

## Part 10 — How to Write a Bug Ticket That People Will Actually Read

You found two bugs and filed two tickets. But a bug ticket is only useful if someone reads it and understands it. Here is how to write a ticket that communicates clearly.

### The Template

```
Title: [Component] — [Brief description of the bug]

Example: UserActivityLogger — BatchInsert generates invalid SQL when session values are null

## Summary
One or two sentences describing the bug at a high level.

## Steps to Reproduce
1. Set the session state mode to StateServer.
2. Stop the StateServer service.
3. Log in as an admin user.
4. Perform any admin action (e.g., edit a user).
5. Observe the error in the application logs.

## Expected Behavior
The admin action should be logged successfully, with NULL values for 
missing session data.

## Actual Behavior
A SqlException is thrown: "Incorrect syntax near ','."
The admin action is NOT logged.

## Root Cause
In UserActivityLogger.cs, line 142, the LogUserActivityBatch method 
appends ", " to the values list when a value is null, instead of ", NULL".
This generates SQL like: VALUES (..., , ) which is invalid.

## Suggested Fix
Change line 142 from:
    vals.Append(", ");
To:
    vals.Append(", NULL");

Or, preferably, refactor the entire method to use parameterized queries
(see also Ticket #4522 for the SQL injection vulnerability in the same code).

## Impact
- 23 admin activity records were lost during the 2026-03-17 migration.
- No user-facing impact.
- No data corruption.

## Environment
- ASP.NET 4.7.2 on Windows Server 2016
- SQL Server 2019
- Session state mode: StateServer

## Related
- Ticket #4522 (SQL injection vulnerability in same code)
- Incident report: 2026-03-17 infrastructure migration
```

### What Makes This Ticket Good

1. **The title is specific.** Not "Bug in logging" but "UserActivityLogger — BatchInsert generates invalid SQL when session values are null." A developer can read the title and immediately know which file to open.

2. **Steps to reproduce are concrete.** Not "trigger a null session" but specific numbered steps that anyone can follow.

3. **Root cause is identified.** The ticket does not just describe the symptom ("SQL error"). It identifies the exact line of code and explains why it is wrong.

4. **Suggested fix is provided.** The developer who picks up the ticket does not have to figure out how to fix it from scratch. They have a starting point.

5. **Impact is quantified.** Not "some records were lost" but "23 records were lost." This helps prioritize.

6. **Related tickets are linked.** The SQL injection vulnerability and the incident report are cross-referenced, so anyone working on one issue can see the full context.

---

## Part 11 — Code Review: How to Review and How to Be Reviewed

The senior developer reviewed the fix for the SQL injection vulnerability in one afternoon. That review caught two issues that the fix's author had missed. Code review is the most effective quality practice in software engineering. Here is how to do it well.

### As the Reviewer

**Start with the big picture.** Before reading individual lines, understand what the change is supposed to do. Read the ticket. Read the commit message. Look at the file list. Does the scope of the change make sense?

**Look for correctness, not style.** If the code works correctly but uses a naming convention you do not prefer, that is not a review finding. Style preferences should be codified in a style guide and enforced by an automated tool, not debated in code reviews.

**Check the tests.** Does the change include tests? Do the tests cover the important cases? Are there edge cases that are not tested?

**Check for security issues.** Is user input validated? Are queries parameterized? Are sensitive values logged? Are error messages safe for external users?

**Be kind.** You are reviewing code, not judging a person. Use phrases like "What do you think about..." and "Have you considered..." instead of "This is wrong" and "You should have..."

### As the Author

**Keep changes small.** A 50-line change is easy to review. A 500-line change is painful. A 5,000-line change will not be reviewed properly, no matter how good the reviewer is.

**Write a good description.** Explain what the change does, why it is needed, and any design decisions you made. The reviewer should not have to guess.

**Respond to feedback gracefully.** If the reviewer points out a problem, thank them. They saved you from shipping a bug. If you disagree, explain your reasoning respectfully.

**Do not take it personally.** A comment on your code is not a comment on your worth as a person. The reviewer is trying to make the code better, not trying to make you feel bad.

---

## Part 12 — Deployment Strategies for Legacy Applications

The fix for the SQL injection vulnerability is ready. It has tests. It has been reviewed. Now it needs to be deployed. How you deploy matters almost as much as what you deploy.

### The Copy-and-Pray Method

Many legacy ASP.NET applications are deployed by copying files to a folder on the server. The developer builds the application in Visual Studio, copies the DLLs and ASPX files to the server (via Remote Desktop, FTP, or a file share), and then checks if the application still works.

This is the simplest deployment method, and it is terrifying. There is no rollback. There is no automation. There is no verification. If the deployment fails, you have to manually copy the old files back, assuming you saved them, which you probably did not.

### A Better Approach: Web Deploy with Backup

If you are stuck with manual deployments, at least add a backup step:

```batch
@echo off
REM deploy.bat — Deploy with backup

SET APP_PATH=\\webserver\c$\inetpub\wwwroot\MyApp
SET BACKUP_PATH=\\webserver\c$\Backups\MyApp_%DATE:~-4%%DATE:~4,2%%DATE:~7,2%_%TIME:~0,2%%TIME:~3,2%

echo Creating backup at %BACKUP_PATH%...
xcopy /E /I /H "%APP_PATH%" "%BACKUP_PATH%"

echo Deploying new files...
xcopy /E /Y ".\publish\*" "%APP_PATH%"

echo Deployment complete. Backup at %BACKUP_PATH%
echo If something is wrong, restore with:
echo xcopy /E /Y "%BACKUP_PATH%\*" "%APP_PATH%"
```

This script creates a timestamped backup before overwriting the files. If the deployment fails, you can restore the backup.

### The Right Approach: CI/CD

The right approach is a CI/CD pipeline that builds, tests, and deploys the application automatically. Even for legacy ASP.NET 4.x applications, you can set up a pipeline using GitHub Actions, Azure DevOps, Jenkins, or TeamCity:

1. **Build:** Compile the solution with `msbuild`.
2. **Test:** Run the unit tests with `vstest.console`.
3. **Package:** Create a Web Deploy package.
4. **Deploy to staging:** Deploy the package to a staging server.
5. **Smoke test:** Run a few HTTP requests against the staging server to verify it works.
6. **Deploy to production:** If the smoke test passes, deploy to production.
7. **Monitor:** Watch the logs for errors for the next hour.

This is more work to set up, but it eliminates the human error in manual deployments and provides automatic rollback (deploy the previous package if the smoke test fails).

---

## Part 13 — The Strangler Fig Pattern: Migrating Away From Legacy Code

If the ultimate goal is to move from ASP.NET 4.x to modern .NET, the strangler fig pattern is the most practical approach for large applications.

### What Is a Strangler Fig?

A strangler fig is a tropical plant that grows around a host tree. It starts small, wrapping itself around the trunk. Over time, it grows larger and larger, gradually replacing the host tree's functions (absorbing sunlight, taking up nutrients). Eventually, the host tree dies and the strangler fig stands on its own.

In software, the strangler fig pattern means building a new system around the old system, gradually replacing its functionality, until the old system can be decommissioned.

### How It Works for ASP.NET

1. **Set up a reverse proxy.** Put a reverse proxy (like NGINX, HAProxy, or IIS URL Rewrite) in front of both the old and new applications.

2. **Route by URL.** Initially, all requests go to the old application. As you build new features (or rewrite existing ones) in the new application, you route specific URLs to the new application.

3. **Share authentication.** Both applications need to recognize the same authenticated user. You can achieve this with a shared authentication cookie (using the same machine key) or with an external identity provider (OAuth2, OpenID Connect).

4. **Migrate incrementally.** Rewrite one feature at a time. Start with the simplest, least-used features. As you gain confidence, tackle more complex features.

5. **Decommission the old application.** When all routes point to the new application, the old application is no longer needed.

The advantages of this approach:

- **No big bang.** You do not have to rewrite the entire application before deploying anything.
- **Reduced risk.** Each migration step is small and can be rolled back independently.
- **Continuous delivery.** The new application can be deployed frequently, even while the old application is still running.
- **Learning.** You learn about the old application's behavior as you migrate it, which helps you avoid introducing bugs in the new version.

---

## Part 14 — Blameless Retrospectives: Learning Without Punishing

After the incident is resolved and the fix is deployed, the team should hold a retrospective. The goal is to learn from what happened and prevent similar incidents in the future. The retrospective should be **blameless**.

### What "Blameless" Means

Blameless does not mean "nobody is responsible." It means "we focus on the system, not the individual." The question is not "who screwed up?" but "what allowed this to happen, and how do we prevent it?"

In our story:

- **Blameful:** "The infrastructure team forgot to start the StateServer. They need to be more careful."
- **Blameless:** "The post-migration checklist did not include verification of Windows services. We need to add a service verification step."

The blameful version focuses on the person. It makes people defensive and discourages them from reporting problems in the future (because reporting problems means admitting mistakes, and admitting mistakes means being blamed).

The blameless version focuses on the process. It assumes that people do their best, and that errors are caused by gaps in the process, not by personal failure. It encourages people to report problems (because the focus is on fixing the process, not punishing the person).

### The Retrospective Format

A simple retrospective format:

1. **Timeline.** Walk through the incident chronologically. When did it start? When was it detected? When was it resolved? What happened at each step?

2. **What went well.** What did the team do right? In our story: the junior developer investigated the error thoroughly, documented it properly, and did not make hasty changes.

3. **What could be improved.** What could the team do better next time? In our story: the post-migration checklist was incomplete, the session state change was not tested with the application, and the StateServer was not monitored.

4. **Action items.** Specific, assignable tasks. Not "be more careful" but "add a Windows service verification step to the migration checklist" (assigned to a specific person, with a deadline).

5. **Follow-up.** Schedule a follow-up meeting to verify that the action items have been completed.

---

## Part 15 — The Psychology of Legacy Code: Fear, Shame, and Learned Helplessness

Let us close with something that is rarely discussed in technical articles: the emotional experience of working with legacy code.

### Fear

Legacy code is scary. You do not understand it. You do not know what will break if you change it. You have heard stories of people who made a "small change" that took down the production system for three hours. You are afraid to touch it.

This fear is rational. It is based on real experiences and real risks. But it can also be paralyzing. If you are too afraid to change legacy code, the code never improves. The technical debt grows. The bugs accumulate. The security vulnerabilities remain unpatched. The application becomes more and more fragile, which makes you more and more afraid, which makes you even less likely to change anything.

The antidote to fear is knowledge. The more you understand the code, the less scary it is. Reading the code, tracing the logic, writing tests, talking to colleagues — these activities reduce uncertainty, and reduced uncertainty reduces fear.

### Shame

Many developers feel ashamed of legacy code. They are embarrassed by the `Hashtable` and the string-concatenated SQL and the Hungarian notation. They apologize for the code when showing it to new team members: "Yeah, I know, it is terrible, but it works."

Here is the thing: the code works. It has been working for over a decade. It processes thousands of requests per day. It handles real users' real data. Yes, it has bugs. Yes, it has security vulnerabilities. Yes, it uses outdated patterns. But it also does its job. It delivers value. It pays the bills.

There is no shame in maintaining a working system. There is shame in neglecting one.

### Learned Helplessness

Learned helplessness is a psychological condition where a person has experienced so many failures that they stop trying. In the context of legacy code, it looks like this: "The code is too messy to fix. There is too much technical debt. The tests do not exist. The documentation is incomplete. There is nothing we can do."

This is a lie. There is always something you can do. You can write one test. You can fix one bug. You can add one comment. You can refactor one method. You can update one dependency. These small improvements compound over time, and they also build confidence. Each small victory makes the next one easier.

The junior developer in our story did something remarkable. They did not fix the code. They did not refactor the code. They did not rewrite the code. They **understood** the code. They read it, traced it, investigated it, and documented it. And in doing so, they made the code less scary for the next person who encounters it.

That is not a small thing. That is the foundation on which all future improvements will be built.

---

## Part 16 — Resources

Here are the resources specific to the topics covered in this follow-up article:

**SQL Injection:**
- Erland Sommarskog: [The Curse and Blessings of Dynamic SQL](https://www.sommarskog.se/dynamic_sql.html) — comprehensive treatment of parameterized queries in SQL Server
- OWASP: [SQL Injection Prevention Cheat Sheet](https://cheatsheetseries.owasp.org/cheatsheets/SQL_Injection_Prevention_Cheat_Sheet.html)
- Troy Hunt: [Everything you wanted to know about SQL injection](https://www.troyhunt.com/everything-you-wanted-to-know-about-sql/)

**OWASP Top 10:**
- [OWASP Top 10:2025](https://owasp.org/Top10/2025/) — the current edition

**IIS Configuration:**
- Microsoft Documentation: [Application Pools in IIS](https://learn.microsoft.com/en-us/iis/configuration/system.applicationHost/applicationPools/)
- Microsoft Documentation: [web.config Reference](https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/iis/web-config)

**Error Handling:**
- ELMAH: [Error Logging Modules and Handlers](https://elmah.github.io/)
- Microsoft Documentation: [How to: Handle Application-Level Errors](https://learn.microsoft.com/en-us/previous-versions/aspnet/24395wz3(v=vs.100))

**Source Control Archaeology:**
- Scott Chacon and Ben Straub: [Pro Git](https://git-scm.com/book/en/v2) — free online book, Chapter 7 covers `git bisect`
- Julia Evans: [git exercises](https://jvns.ca/blog/2019/08/30/git-exercises/) — practical exercises for learning git internals

**Code Review:**
- Google Engineering Practices: [How to do a code review](https://google.github.io/eng-practices/review/reviewer/)
- Michaela Greiler: [Code Review Best Practices](https://www.michaelagreiler.com/code-review-best-practices/)

**Blameless Retrospectives:**
- Etsy Engineering: [Blameless PostMortems and a Just Culture](https://www.etsy.com/codeascraft/blameless-postmortems/)
- Google SRE Book: [Chapter 15: Postmortem Culture](https://sre.google/sre-book/postmortem-culture/)

**The Strangler Fig Pattern:**
- Martin Fowler: [StranglerFigApplication](https://martinfowler.com/bliki/StranglerFigApplication.html) — the original description

---

*This concludes Part 2 of our load-bearing bugs series. If you are a developer who works with legacy code — and statistically, most developers are — remember this: understanding code is just as valuable as writing code. The developer who reads carefully, investigates thoroughly, documents honestly, and asks for help when they need it is not an idiot. They are a professional. And the industry needs more of them.*
