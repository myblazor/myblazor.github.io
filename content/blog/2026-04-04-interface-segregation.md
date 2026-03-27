---
title: "The Interface Segregation Principle: A Complete Guide for .NET Developers"
date: 2026-04-04
author: observer-team
summary: "A deep dive into the Interface Segregation Principle (ISP), the 'I' in SOLID. Covers the origin story at Xerox, what ISP really means (and what it does not mean), how it manifests in the .NET Base Class Library, practical C# refactoring walkthroughs, its relationship to the other SOLID principles, and how to apply it in modern .NET 10 applications."
tags:
  - solid
  - csharp
  - design-principles
  - dotnet
  - architecture
  - deep-dive
  - best-practices
---

Picture this. You are six months into building a document management system. The `IDocumentService` interface started with three methods — `Upload`, `Download`, and `Delete`. Reasonable enough. Then the PM asked for versioning. Then someone needed OCR text extraction. Then the compliance team wanted audit trails. Then the mobile team needed thumbnail generation. Now your interface has fourteen methods, and every class that implements it — the local file store, the Azure Blob adapter, the in-memory test double — must carry the weight of all fourteen, even though most of them use only three or four. Every time you add a method, you touch every implementation. Every time you touch every implementation, you risk breaking something that was already working. You are living inside a violation of the Interface Segregation Principle, and you might not even know it yet.

This article will take you from the origin story of the ISP, through the theory, into the .NET Base Class Library where Microsoft themselves struggled with it, through practical C# refactoring examples, and finally into the modern .NET 10 world of default interface methods, minimal APIs, and microservice boundaries. By the end, you will have a mental model for recognizing fat interfaces, a toolkit for breaking them apart, and the judgment to know when to stop splitting.

## Part 1: The Origin Story — A Printer, a Fat Class, and an Hour-Long Build

The Interface Segregation Principle was not conceived in an ivory tower. It was born out of pain at Xerox in the early 1990s.

Robert C. Martin — universally known as Uncle Bob — was consulting for Xerox on a new multifunction printer system. This printer could print, staple, fax, and collate. The software driving it had been built from scratch. At the heart of the system sat a single `Job` class. Every task — print jobs, staple jobs, fax jobs — went through this one class. The `Job` class knew about every operation the printer could perform.

As the system grew, the `Job` class grew with it. It accumulated methods for every conceivable operation. And here is where the real damage showed up: because every module in the system depended on this single class, even the tiniest change to a fax-related method triggered a recompilation of the stapling module, the printing module, and everything else. The build cycle ballooned to an hour. Development became nearly impossible. A one-line fix to fax retry logic meant every developer on the team had to wait an hour before they could test anything.

Martin's solution was to insert interfaces between the `Job` class and its clients. Instead of every module depending directly on the monolithic `Job` class, each module would depend on a narrow interface tailored to its needs. A `StapleJob` interface exposed only the methods the stapling module needed. A `PrintJob` interface exposed only the methods the printing module needed. The `Job` class still implemented all of those interfaces — it still contained the actual logic — but the modules no longer knew about each other's methods. A change to a fax method no longer triggered recompilation of the stapling code, because the stapling code did not depend on the fax interface.

This was the moment the Interface Segregation Principle crystallized. Martin later formulated it as a single sentence:

**"Clients should not be forced to depend on methods they do not use."**

He published the principle formally in his 2002 book *Agile Software Development: Principles, Patterns, and Practices*, and it became the "I" in the SOLID acronym (coined by Michael Feathers around 2004). But the underlying insight predates the book by nearly a decade. It was born on a factory floor, from a real system with real build times that had become real obstacles.

## Part 2: What the ISP Actually Says (and What It Does Not Say)

The ISP is frequently misunderstood. Let us be precise about what it claims and what it does not.

### What ISP says

An interface should be designed from the perspective of its clients. If two clients use different subsets of an interface's methods, those subsets should be expressed as separate interfaces. The goal is to prevent a change demanded by one client from rippling through to another client that does not care about that change.

Think of it like a restaurant menu. A vegetarian diner and a meat-loving diner both eat at the same restaurant. If the restaurant hands them a single menu that is 40 pages long, the vegetarian has to flip past 30 pages of steak and pork to find the three salad options. Worse, if the chef changes the steak section, the vegetarian's menu is reprinted too. A better design: give the vegetarian a focused vegetarian menu and the carnivore a focused carnivore menu. The kitchen (the implementing class) still prepares all the dishes, but each diner (client) only sees what is relevant to them.

### What ISP does not say

**ISP does not say every interface should have one method.** This is a common over-application. An interface with five methods is perfectly fine if every client that depends on it uses all five. The principle is about unused dependencies, not about counting methods. An `ILogger` with `LogDebug`, `LogInformation`, `LogWarning`, `LogError`, and `LogCritical` is not an ISP violation if every consumer of the logger calls all five methods (or at least could reasonably call any of them).

**ISP is not the same as the Single Responsibility Principle (SRP).** SRP says a class should have one reason to change. ISP says a client should not depend on methods it does not use. They are related but distinct. You can violate ISP without violating SRP, and vice versa. An interface might have a single responsibility (managing user accounts) but still be too fat for certain clients (a reporting module that only needs to read user names).

**ISP is not about `NotImplementedException`.** If a class implements an interface and throws `NotImplementedException` for some methods, that is a Liskov Substitution Principle (LSP) violation, not an ISP violation per se. ISP focuses on the client side — what the consuming class is forced to depend on — not the implementing side. Of course, in practice, the two often appear together. A fat interface leads to implementations that cannot fully honor the contract, which is both an ISP smell and an LSP violation. But they are distinct diagnoses.

**ISP is not limited to the C# `interface` keyword.** The principle applies to any abstraction boundary. A class with twenty public methods where different consumers use different subsets is an ISP problem even if no `interface` keyword is in sight. Abstract classes, base classes, and even module APIs in microservice architectures can all exhibit fat-interface problems.

### The precise formulation

Uncle Bob later refined the principle in his article on the topic: when a client depends on a class that contains methods the client does not use, but that other clients do use, then that client will be affected by the changes those other clients force upon the class. The clients become indirectly coupled to each other through the shared interface, even though they have no direct relationship.

## Part 3: ISP in the .NET Base Class Library

The .NET BCL is a fascinating study in interface segregation — both its successes and its historical failures. The designers of the framework have been wrestling with ISP since .NET 1.0, and the evolution of collection interfaces tells the story better than any textbook.

### The IList problem

Consider `IList<T>`. It defines methods for reading (`this[int index]`, `IndexOf`), adding (`Add`, `Insert`), removing (`Remove`, `RemoveAt`), and clearing (`Clear`). If your code only needs to iterate over a collection, depending on `IList<T>` forces you to carry the conceptual weight of all those mutation methods. Your class is now coupled to the idea that collections can be modified, even if your code never modifies anything.

Worse, `Array` in .NET implements `IList<T>`. But arrays have a fixed size. Calling `Add` on an array throws `NotSupportedException`. This is a textbook LSP violation that exists precisely because of an ISP problem: `IList<T>` bundles reading and writing into a single contract, forcing fixed-size collections to implement methods they cannot meaningfully support.

### The read-only interfaces arrive in .NET 4.5

For years, .NET developers asked Microsoft for read-only collection interfaces. The BCL team initially declined, arguing that the value did not justify the added complexity. Then WinRT arrived. The Windows Runtime exposed `IVectorView<T>` and `IMapView<K, V>`, and .NET needed corresponding types for interop. This external pressure finally pushed the team to introduce `IReadOnlyCollection<T>` and `IReadOnlyList<T>` in .NET 4.5.

The result is a textbook application of ISP:

```csharp
// IEnumerable<T> — forward-only iteration, nothing more
public interface IEnumerable<out T> : IEnumerable
{
    IEnumerator<T> GetEnumerator();
}

// IReadOnlyCollection<T> — iteration plus a count
public interface IReadOnlyCollection<out T> : IEnumerable<T>
{
    int Count { get; }
}

// IReadOnlyList<T> — iteration, count, and indexed access
public interface IReadOnlyList<out T> : IReadOnlyCollection<T>
{
    T this[int index] { get; }
}

// ICollection<T> — adds mutation (Add, Remove, Clear)
public interface ICollection<T> : IEnumerable<T>
{
    int Count { get; }
    bool IsReadOnly { get; }
    void Add(T item);
    void Clear();
    bool Contains(T item);
    void CopyTo(T[] array, int arrayIndex);
    bool Remove(T item);
}

// IList<T> — adds indexed mutation (Insert, RemoveAt, indexer set)
public interface IList<T> : ICollection<T>
{
    T this[int index] { get; set; }
    int IndexOf(T item);
    void Insert(int index, T item);
    void RemoveAt(int index);
}
```

Notice the hierarchy. Each interface adds a narrow slice of capability. A method that only needs to iterate takes `IEnumerable<T>`. A method that also needs a count takes `IReadOnlyCollection<T>`. A method that needs indexed access takes `IReadOnlyList<T>`. And only a method that genuinely needs to mutate the collection takes `ICollection<T>` or `IList<T>`. This is ISP in action: each client depends only on the capability it actually uses.

### The IQueryable hierarchy

LINQ provides another beautiful example. `IQueryable<T>` inherits from `IEnumerable<T>`, `IQueryable`, and `IEnumerable`. The capability of iterating over a collection is segregated from the capability of evaluating expression trees against a query provider. Code that only needs to iterate depends on `IEnumerable<T>`. Code that needs to build and translate expression trees depends on `IQueryable<T>`. The consuming code declares exactly the level of capability it requires.

### Stream and the CanRead / CanWrite pattern

The `System.IO.Stream` class takes a different approach to the same problem. Rather than segregating into multiple interfaces, `Stream` uses capability flags: `CanRead`, `CanWrite`, `CanSeek`, and `CanTimeout`. Callers check these flags before invoking read or write operations.

This is a pragmatic compromise. A strict ISP application would split `Stream` into `IReadableStream`, `IWritableStream`, `ISeekableStream`, and various combinations. The BCL team decided that the combinatorial explosion of interfaces was worse than the capability-flag approach. This is a valid engineering trade-off, and it reminds us that ISP is a principle, not a law. Sometimes the cure is worse than the disease.

### The practical guideline for .NET collection types

A widely-accepted guideline in modern .NET follows directly from ISP:

**Accept the most general type you can. Return the most specific type you can.**

For method parameters, prefer `IEnumerable<T>` (the most general). For return types, prefer `IReadOnlyList<T>` (the most specific read-only indexed collection). This way, callers of your method get the richest possible contract without mutation capability, and your method accepts the widest possible range of inputs.

```csharp
// Good: accepts IEnumerable<T>, returns IReadOnlyList<T>
public IReadOnlyList<Customer> FilterActive(IEnumerable<Customer> customers)
{
    return customers.Where(c => c.IsActive).ToList();
}

// Bad: accepts List<Customer> (too specific), returns IEnumerable<Customer> (too vague)
public IEnumerable<Customer> FilterActive(List<Customer> customers)
{
    return customers.Where(c => c.IsActive);
}
```

## Part 4: Recognizing Fat Interfaces in Your Own Code

Before you can fix an ISP violation, you need to spot one. Here are the telltale signs, ordered from obvious to subtle.

### Sign 1: NotImplementedException or NotSupportedException

This is the most glaring symptom. If a class implements an interface and some methods throw `NotImplementedException`, one of two things is happening: the implementation is incomplete (a temporary state), or the interface is too broad for this class. If it is the latter, you have an ISP problem on the implementing side and almost certainly an LSP problem on the consuming side.

```csharp
// Smells like ISP violation
public class ReadOnlyProductStore : IProductStore
{
    public Product GetById(int id) { /* works fine */ }
    public IReadOnlyList<Product> GetAll() { /* works fine */ }
    public void Add(Product product) => throw new NotSupportedException();
    public void Update(Product product) => throw new NotSupportedException();
    public void Delete(int id) => throw new NotSupportedException();
}
```

The `ReadOnlyProductStore` is telling you that it does not belong behind the `IProductStore` interface. It needs a read-only interface.

### Sign 2: Clients that only use a subset of methods

Open any class that depends on an interface. Count the methods it actually calls. If it calls three out of twelve, the interface is too fat for this client. This is the canonical ISP violation, and it is far more common than the `NotImplementedException` variant.

```csharp
public class ProductReportGenerator
{
    private readonly IProductRepository _repository;

    public ProductReportGenerator(IProductRepository repository)
    {
        _repository = repository;
    }

    public Report Generate()
    {
        // Only calls GetAll and GetById — never Add, Update, or Delete
        var products = _repository.GetAll();
        // ... build report ...
    }
}
```

The `ProductReportGenerator` depends on `IProductRepository` but only uses the read methods. It is coupled to the write methods unnecessarily. If someone adds a `BulkDelete` method to `IProductRepository`, the `ProductReportGenerator` is affected by the change even though it never deletes anything.

### Sign 3: Mock objects in tests that have many Setup calls for unused methods

When you write unit tests using a mocking framework, pay attention to how many `Setup` or `Returns` calls you need. If you are setting up eight methods on a mock but the code under test only calls two, that is a strong signal that the interface is too fat.

```csharp
// If you find yourself writing this:
var mock = new Mock<IDocumentService>();
mock.Setup(x => x.Upload(It.IsAny<Document>())).Returns(Task.CompletedTask);
mock.Setup(x => x.Download(It.IsAny<int>())).Returns(Task.FromResult(doc));
mock.Setup(x => x.Delete(It.IsAny<int>())).Returns(Task.CompletedTask);
mock.Setup(x => x.ExtractText(It.IsAny<int>())).Returns(Task.FromResult(""));
mock.Setup(x => x.GenerateThumbnail(It.IsAny<int>())).Returns(Task.FromResult(thumb));
// ... but the class under test only calls Download()
// ... you have an ISP problem.
```

### Sign 4: Frequent recompilation of unrelated code

This was the original symptom at Xerox and it remains relevant today, especially in large solutions with many projects. If modifying an interface in one assembly forces recompilation of assemblies that do not use the changed method, you are experiencing the ISP violation's original pain point. In a modern .NET solution, this manifests as unnecessarily long `dotnet build` times and spurious CI failures in projects that should not be affected by the change.

### Sign 5: Interface names that are vague or overly general

Names like `IService`, `IManager`, `IHandler`, or `IRepository` (without any qualifier) are often signs that the interface is trying to be everything to everyone. A well-segregated interface has a name that tells you exactly what it does: `IProductReader`, `IOrderWriter`, `IAuditLogger`, `IThumbnailGenerator`.

## Part 5: Refactoring Fat Interfaces — A Step-by-Step Walkthrough

Let us take a realistic example and walk through the refactoring from a fat interface to well-segregated ones. We will use a scenario familiar to .NET web developers: a user repository.

### The starting point: a fat IUserRepository

```csharp
public interface IUserRepository
{
    // Read operations
    Task<User?> GetByIdAsync(int id);
    Task<User?> GetByEmailAsync(string email);
    Task<IReadOnlyList<User>> GetAllAsync();
    Task<IReadOnlyList<User>> SearchAsync(string query);

    // Write operations
    Task AddAsync(User user);
    Task UpdateAsync(User user);
    Task DeleteAsync(int id);

    // Bulk operations
    Task BulkImportAsync(IEnumerable<User> users);
    Task BulkDeleteAsync(IEnumerable<int> ids);

    // Reporting
    Task<int> GetTotalCountAsync();
    Task<IReadOnlyList<User>> GetRecentlyActiveAsync(DateTime since);
    Task<Dictionary<string, int>> GetRegistrationsByMonthAsync(int year);
}
```

Thirteen methods. Not enormous by real-world standards, but let us look at who actually calls what.

The **web API controllers** use `GetByIdAsync`, `GetAllAsync`, `SearchAsync`, `AddAsync`, `UpdateAsync`, and `DeleteAsync`. The **admin bulk import tool** uses `BulkImportAsync` and `BulkDeleteAsync`. The **dashboard widget** uses `GetTotalCountAsync`, `GetRecentlyActiveAsync`, and `GetRegistrationsByMonthAsync`. The **authentication middleware** uses only `GetByEmailAsync`.

Four clients, four different subsets. Every client is coupled to every other client's methods.

### Step 1: Identify the client groups

Group the methods by which clients use them:

- **Read (single)**: `GetByIdAsync`, `GetByEmailAsync` — used by controllers and auth middleware
- **Read (collection)**: `GetAllAsync`, `SearchAsync` — used by controllers
- **Write**: `AddAsync`, `UpdateAsync`, `DeleteAsync` — used by controllers
- **Bulk**: `BulkImportAsync`, `BulkDeleteAsync` — used by admin tool
- **Reporting**: `GetTotalCountAsync`, `GetRecentlyActiveAsync`, `GetRegistrationsByMonthAsync` — used by dashboard

### Step 2: Define focused interfaces

```csharp
public interface IUserReader
{
    Task<User?> GetByIdAsync(int id);
    Task<User?> GetByEmailAsync(string email);
    Task<IReadOnlyList<User>> GetAllAsync();
    Task<IReadOnlyList<User>> SearchAsync(string query);
}

public interface IUserWriter
{
    Task AddAsync(User user);
    Task UpdateAsync(User user);
    Task DeleteAsync(int id);
}

public interface IUserBulkOperations
{
    Task BulkImportAsync(IEnumerable<User> users);
    Task BulkDeleteAsync(IEnumerable<int> ids);
}

public interface IUserReporting
{
    Task<int> GetTotalCountAsync();
    Task<IReadOnlyList<User>> GetRecentlyActiveAsync(DateTime since);
    Task<Dictionary<string, int>> GetRegistrationsByMonthAsync(int year);
}
```

### Step 3: Optionally compose larger interfaces

If some clients genuinely need both reading and writing, you can compose:

```csharp
public interface IUserRepository : IUserReader, IUserWriter { }
```

This is a common and idiomatic C# pattern. The web API controllers can depend on `IUserRepository` (which gives them read and write), while the dashboard depends only on `IUserReporting`, and the auth middleware depends only on `IUserReader`.

### Step 4: Update the implementing class

The implementing class does not change much. It simply declares that it implements all the interfaces:

```csharp
public class SqlUserRepository : IUserRepository, IUserBulkOperations, IUserReporting
{
    private readonly AppDbContext _db;

    public SqlUserRepository(AppDbContext db) => _db = db;

    // IUserReader
    public async Task<User?> GetByIdAsync(int id)
        => await _db.Users.FindAsync(id);

    public async Task<User?> GetByEmailAsync(string email)
        => await _db.Users.FirstOrDefaultAsync(u => u.Email == email);

    public async Task<IReadOnlyList<User>> GetAllAsync()
        => await _db.Users.OrderBy(u => u.Name).ToListAsync();

    public async Task<IReadOnlyList<User>> SearchAsync(string query)
        => await _db.Users.Where(u => u.Name.Contains(query)).ToListAsync();

    // IUserWriter
    public async Task AddAsync(User user)
    {
        _db.Users.Add(user);
        await _db.SaveChangesAsync();
    }

    public async Task UpdateAsync(User user)
    {
        _db.Users.Update(user);
        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var user = await _db.Users.FindAsync(id);
        if (user is not null)
        {
            _db.Users.Remove(user);
            await _db.SaveChangesAsync();
        }
    }

    // IUserBulkOperations
    public async Task BulkImportAsync(IEnumerable<User> users)
    {
        _db.Users.AddRange(users);
        await _db.SaveChangesAsync();
    }

    public async Task BulkDeleteAsync(IEnumerable<int> ids)
    {
        var users = await _db.Users.Where(u => ids.Contains(u.Id)).ToListAsync();
        _db.Users.RemoveRange(users);
        await _db.SaveChangesAsync();
    }

    // IUserReporting
    public async Task<int> GetTotalCountAsync()
        => await _db.Users.CountAsync();

    public async Task<IReadOnlyList<User>> GetRecentlyActiveAsync(DateTime since)
        => await _db.Users.Where(u => u.LastActiveAt >= since).ToListAsync();

    public async Task<Dictionary<string, int>> GetRegistrationsByMonthAsync(int year)
        => await _db.Users
            .Where(u => u.CreatedAt.Year == year)
            .GroupBy(u => u.CreatedAt.Month)
            .ToDictionaryAsync(
                g => g.Key.ToString("D2"),
                g => g.Count());
}
```

The class is the same size it was before. The difference is in how it is consumed. Each client now depends on exactly the interface it needs.

### Step 5: Register in DI

In your `Program.cs` or DI configuration:

```csharp
builder.Services.AddScoped<SqlUserRepository>();
builder.Services.AddScoped<IUserReader>(sp => sp.GetRequiredService<SqlUserRepository>());
builder.Services.AddScoped<IUserWriter>(sp => sp.GetRequiredService<SqlUserRepository>());
builder.Services.AddScoped<IUserRepository>(sp => sp.GetRequiredService<SqlUserRepository>());
builder.Services.AddScoped<IUserBulkOperations>(sp => sp.GetRequiredService<SqlUserRepository>());
builder.Services.AddScoped<IUserReporting>(sp => sp.GetRequiredService<SqlUserRepository>());
```

Now each class can request exactly the interface it needs through constructor injection:

```csharp
// The dashboard only sees reporting methods
public class DashboardService
{
    private readonly IUserReporting _reporting;
    public DashboardService(IUserReporting reporting) => _reporting = reporting;
}

// The auth middleware only sees read methods
public class AuthenticationHandler
{
    private readonly IUserReader _users;
    public AuthenticationHandler(IUserReader users) => _users = users;
}

// The admin tool only sees bulk operations
public class BulkImportService
{
    private readonly IUserBulkOperations _bulk;
    public BulkImportService(IUserBulkOperations bulk) => _bulk = bulk;
}
```

### The payoff

After this refactoring, consider what happens when the reporting team asks for a new method, `GetChurnRateAsync`. You add it to `IUserReporting` and implement it in `SqlUserRepository`. The auth middleware, the web controllers, and the admin tool are completely unaffected. They do not depend on `IUserReporting`. Their interfaces have not changed. Their tests do not need to be updated. Their assemblies do not need to be recompiled (in a multi-project solution). This is precisely the decoupling the ISP was designed to achieve.

## Part 6: ISP and the Other SOLID Principles

The SOLID principles are not isolated rules. They interact with and reinforce each other. Understanding how ISP relates to the other four helps you apply all of them more effectively.

### ISP and Single Responsibility Principle (SRP)

SRP says a class should have one reason to change. ISP says a client should not depend on methods it does not use. In practice, a fat interface often indicates that the implementing class has multiple responsibilities. Splitting the interface along ISP lines frequently reveals SRP violations in the implementation, too. The user repository refactoring above hints at this: the reporting queries are a conceptually different responsibility from the CRUD operations. In a mature system, you might split them into separate classes behind separate interfaces.

But they can diverge. An interface might be fat for ISP purposes while the implementing class is perfectly SRP-compliant. Consider a `JsonSerializer` interface with methods for serialization and deserialization. Both operations are the same responsibility (JSON conversion), but a client that only serializes does not need the deserialization methods. That is an ISP concern, not an SRP concern.

### ISP and Open/Closed Principle (OCP)

OCP says software entities should be open for extension but closed for modification. Fat interfaces make OCP harder to follow because adding a method to an interface is a modification that forces changes in every implementation. Well-segregated interfaces are easier to extend: you can add new interfaces for new capabilities without modifying existing ones.

### ISP and Liskov Substitution Principle (LSP)

ISP and LSP are two sides of the same coin. ISP prevents clients from depending on methods they do not use (the client perspective). LSP prevents implementations from failing to honor the contract (the implementation perspective). Fat interfaces lead to both problems: the client depends on too much, and the implementation throws `NotSupportedException` for things it cannot do. Fix the ISP violation, and the LSP violation often disappears automatically. `Array` implementing `IList<T>` is the canonical example: the ISP violation (forcing array consumers to see `Add`) directly causes the LSP violation (`Add` throwing an exception).

### ISP and Dependency Inversion Principle (DIP)

DIP says high-level modules should not depend on low-level modules; both should depend on abstractions. ISP refines this: the abstractions themselves should be well-designed. A fat abstraction is not much better than a concrete dependency. DIP tells you to use interfaces. ISP tells you to make those interfaces the right size.

## Part 7: ISP in ASP.NET Core and Modern .NET

Modern .NET and ASP.NET Core provide several features and patterns that interact directly with ISP.

### Dependency injection and interface-per-concern

ASP.NET Core's built-in DI container makes ISP natural to apply. You register services by interface, and each consumer requests only the interface it needs. The DI container resolves everything at runtime. This is exactly what we showed in the user repository example above.

A particularly powerful pattern is registering a single implementation class behind multiple interfaces:

```csharp
// Register the concrete type once
builder.Services.AddScoped<SqlUserRepository>();

// Forward each interface to the same instance
builder.Services.AddScoped<IUserReader>(sp => sp.GetRequiredService<SqlUserRepository>());
builder.Services.AddScoped<IUserWriter>(sp => sp.GetRequiredService<SqlUserRepository>());
```

This preserves ISP at the consumer level while keeping a single implementation at the runtime level. The consumer sees a narrow interface; the container provides the full implementation.

### Minimal APIs and endpoint-specific dependencies

ASP.NET Core minimal APIs encourage you to inject dependencies directly into endpoint handlers rather than into controller classes. This makes ISP violations more visible, because each handler declares exactly what it needs:

```csharp
app.MapGet("/users/{id}", async (int id, IUserReader reader) =>
{
    var user = await reader.GetByIdAsync(id);
    return user is not null ? Results.Ok(user) : Results.NotFound();
});

app.MapPost("/users", async (User user, IUserWriter writer) =>
{
    await writer.AddAsync(user);
    return Results.Created($"/users/{user.Id}", user);
});

app.MapGet("/dashboard/stats", async (IUserReporting reporting) =>
{
    var count = await reporting.GetTotalCountAsync();
    return Results.Ok(new { TotalUsers = count });
});
```

Each endpoint depends on exactly the interface it needs. There is no controller class pulling in twelve dependencies that different action methods use in different combinations. Minimal APIs make ISP almost effortless.

### Default interface methods (C# 8+)

C# 8 introduced default interface methods (DIMs), which let you add methods to an interface with a default implementation, so existing implementing classes are not forced to change.

```csharp
public interface IUserReader
{
    Task<User?> GetByIdAsync(int id);
    Task<User?> GetByEmailAsync(string email);
    Task<IReadOnlyList<User>> GetAllAsync();
    Task<IReadOnlyList<User>> SearchAsync(string query);

    // Default implementation — existing implementers are not forced to provide this
    Task<bool> ExistsAsync(int id)
        => GetByIdAsync(id).ContinueWith(t => t.Result is not null);
}
```

DIMs can mitigate ISP pressure by allowing you to grow an interface without breaking existing implementations. But they are not a substitute for proper segregation. If different clients need fundamentally different subsets of an interface, no amount of default methods will fix the coupling. DIMs are best used for adding convenience methods that build on existing methods, not for bolting unrelated capabilities onto an interface.

### The IHost and IHostBuilder interfaces

ASP.NET Core's hosting model itself demonstrates ISP. The `IHost` interface is deliberately narrow: `StartAsync`, `StopAsync`, `Dispose`, and a `Services` property. The builder (`IHostBuilder`) is separate. Configuration, logging, and DI are all configured through the builder, not through the host. The running host exposes only what running code needs. This separation allows different consumers (health check probes, graceful shutdown handlers, background services) to depend on the narrow `IHost` interface without being coupled to the builder's configuration API.

## Part 8: ISP Beyond OOP — Microservices, APIs, and Event-Driven Systems

The ISP is not limited to C# interfaces in a single codebase. The same principle applies at architectural boundaries.

### REST API design

A REST API is an interface in the broadest sense. If you expose a single `/api/users` endpoint that supports GET, POST, PUT, DELETE, PATCH, and a dozen query parameters, every consumer of that API is coupled to the full surface area. A consumer that only reads user data still needs to understand the write endpoints exist (at minimum, to ignore them). If you version the API and change a write endpoint, read-only consumers must still validate that nothing they depend on has changed.

API segregation looks like this: separate read endpoints from write endpoints, or even separate them into distinct services. A read-optimized service with caching sits behind `/api/users/query`, while a write service with validation and event publishing sits behind `/api/users/command`. This is the CQRS (Command Query Responsibility Segregation) pattern, and it is ISP applied at the service boundary.

### Message contracts in event-driven systems

In an event-driven architecture, messages are interfaces. If you define a single `UserEvent` class with fields for creation, update, deletion, and password reset, every subscriber must deserialize and ignore the fields it does not care about. Worse, if you add a field for a new event type, every subscriber's deserialization might break.

ISP-compliant event design uses separate event types: `UserCreatedEvent`, `UserUpdatedEvent`, `UserDeletedEvent`, `UserPasswordResetEvent`. Each subscriber handles only the events it cares about. This is exactly the ISP applied to message contracts.

### gRPC service definitions

gRPC uses Protocol Buffers to define service contracts. A `.proto` file with 30 RPC methods in a single service definition is a fat interface. Clients generated from this proto file will have stubs for all 30 methods, even if they only call two. The idiomatic gRPC approach is to define multiple, focused service definitions in separate `.proto` files (or at least separate `service` blocks within the same file). This keeps the generated client code lean and reduces the coupling between different consumers.

## Part 9: Common Pitfalls and How to Avoid Them

### Pitfall 1: Over-segregation

The most common mistake when learning ISP is splitting interfaces too aggressively. If you end up with one interface per method, you have not improved anything. You have just traded one problem (fat interfaces) for another (a proliferation of micro-interfaces that are individually meaningless and collectively confusing).

The rule of thumb: split when different clients use different subsets. If every client uses every method, there is nothing to split. If you find yourself creating `ICanAdd`, `ICanDelete`, `ICanUpdate`, and `ICanGetById` as four separate single-method interfaces, step back and ask whether any client actually uses `ICanAdd` without also using `ICanUpdate`. If the answer is no, merge them.

### Pitfall 2: Splitting by implementation detail instead of client need

Interfaces should be designed from the perspective of the client, not the implementation. Do not split an interface because the implementing class has two private fields. Split it because two clients need different subsets of the public contract. The implementation is free to use whatever internal structure it wants.

A bad split:

```csharp
// Split based on which database table the methods hit — an implementation detail
public interface IUserTableQueries { /* queries on User table */ }
public interface IAuditLogTableQueries { /* queries on AuditLog table */ }
```

A good split:

```csharp
// Split based on what consumers need
public interface IUserReader { /* methods for reading user data */ }
public interface IAuditTrail { /* methods for recording and querying audit events */ }
```

### Pitfall 3: Breaking changes during refactoring

When you refactor a fat interface into multiple smaller ones, you are making a breaking change. Every consumer of the original interface must be updated to depend on one of the new interfaces. In a small codebase this is trivial. In a large codebase with hundreds of consumers, it can be daunting.

The pragmatic approach: keep the original fat interface as a composition of the new smaller ones, at least temporarily.

```csharp
// Old interface — now composed of smaller ones
public interface IUserRepository : IUserReader, IUserWriter, IUserBulkOperations, IUserReporting
{
    // No new members — just aggregates the smaller interfaces
}
```

Existing code continues to compile. New code can depend on the smaller interfaces. Over time, you can migrate consumers one by one and eventually deprecate the fat composite interface.

### Pitfall 4: Ignoring ISP in test doubles

If your test doubles (mocks, stubs, fakes) implement the full fat interface, you are masking the ISP violation. The tests work, but they quietly accept the coupling. When you move to well-segregated interfaces, your test doubles become simpler and your tests become more focused. A test for the dashboard should only need a mock of `IUserReporting`, not a mock of the entire repository.

### Pitfall 5: Applying ISP to value objects and DTOs

ISP is about behavioral contracts — methods and their dependencies. It does not apply to data transfer objects, records, or value objects in the same way. A `UserDto` with fifteen properties is not an ISP violation. It is a data container. The ISP applies to the interfaces through which behavior is exposed, not to the shape of data structures. (You might have other concerns about a DTO with fifteen properties — perhaps it is doing too much — but that is SRP, not ISP.)

## Part 10: ISP in Blazor WebAssembly

For those of us building Blazor WebAssembly applications — like this very blog you are reading on Observer Magazine — ISP has practical implications for how we structure our services.

### Service interfaces for Blazor components

In a Blazor WASM app, components inject services to fetch data, manage state, and interact with APIs. A common mistake is to create a single `IApiService` that every component depends on:

```csharp
// Fat interface — every component depends on everything
public interface IApiService
{
    Task<IReadOnlyList<BlogPost>> GetBlogPostsAsync();
    Task<BlogPost?> GetBlogPostAsync(string slug);
    Task<IReadOnlyList<Product>> GetProductsAsync();
    Task<Product?> GetProductAsync(int id);
    Task SaveProductAsync(Product product);
    Task DeleteProductAsync(int id);
    Task<UserProfile> GetCurrentUserAsync();
    Task UpdateUserProfileAsync(UserProfile profile);
    Task<WeatherForecast[]> GetForecastAsync();
}
```

The blog components only need blog methods. The product showcase only needs product methods. The user profile page only needs user methods. Every component is coupled to every other component's data-fetching needs.

A well-segregated design:

```csharp
public interface IBlogService
{
    Task<IReadOnlyList<BlogPostMetadata>> GetPostsAsync();
    Task<BlogPostMetadata?> GetPostAsync(string slug);
    Task<string> GetPostHtmlAsync(string slug);
}

public interface IProductCatalog
{
    Task<IReadOnlyList<Product>> GetProductsAsync();
    Task<Product?> GetProductAsync(int id);
}

public interface IProductEditor
{
    Task SaveProductAsync(Product product);
    Task DeleteProductAsync(int id);
}

public interface IUserProfileService
{
    Task<UserProfile> GetCurrentUserAsync();
    Task UpdateUserProfileAsync(UserProfile profile);
}
```

Each Blazor component injects only the interface it needs. The blog page depends on `IBlogService`. The product detail page depends on `IProductCatalog`. The admin editor depends on `IProductEditor`. When you change the blog data format, the product components are completely unaffected.

### Testability benefits in Blazor

This segregation pays enormous dividends in bUnit tests. Consider testing a blog post component:

```csharp
[Fact]
public void BlogPost_RendersTitle()
{
    // With segregated interfaces, the mock is minimal
    var mockBlog = new Mock<IBlogService>();
    mockBlog.Setup(b => b.GetPostAsync("test-slug"))
        .ReturnsAsync(new BlogPostMetadata { Title = "Test Post", Slug = "test-slug" });
    mockBlog.Setup(b => b.GetPostHtmlAsync("test-slug"))
        .ReturnsAsync("<p>Hello</p>");

    using var ctx = new BunitContext();
    ctx.Services.AddSingleton(mockBlog.Object);

    var cut = ctx.Render<BlogPost>(parameters =>
        parameters.Add(p => p.Slug, "test-slug"));

    cut.Find("h1").TextContent.ShouldBe("Test Post");
}
```

No need to mock product methods, user methods, or weather methods. The test sets up exactly the interface the component uses. This makes tests faster to write, easier to read, and more resistant to changes in unrelated parts of the system.

## Part 11: Practical Heuristics — When to Split and When to Stop

After all this theory and examples, here are concrete heuristics you can apply in your daily work.

### Split when

1. **Two or more clients use different subsets** of the same interface. This is the canonical ISP trigger.
2. **You find yourself writing `NotImplementedException`** in an implementation. The interface is asking for something this class cannot do.
3. **Your mocks are bloated.** If setting up a mock requires configuring methods the test never exercises, the interface is too fat for this consumer.
4. **A change to one method ripples to unrelated consumers.** If adding a reporting method forces you to update an authentication handler, the coupling is wrong.
5. **You are splitting a monolith into microservices.** Each service should expose a focused API, not a mirror of the monolith's fat interface.

### Do not split when

1. **Every client uses every method.** If there is no divergence in how clients consume the interface, splitting adds complexity without benefit.
2. **The interface has fewer than five methods and they are all cohesive.** An `ILogger` with five log-level methods is fine.
3. **The split would create single-method interfaces that are always used together.** If `ICanRead` and `ICanCount` are always injected together, merge them into `IReadOnlyCollection` (which is exactly what Microsoft did).
4. **You are working on a throwaway prototype.** ISP is an investment in long-term maintainability. If the code will be deleted next sprint, the investment does not pay off.
5. **The interface is a well-known framework type.** Do not wrap `ILogger<T>` in your own `IMyLogger` just to remove methods you do not call. The framework type is well-understood, widely documented, and carries minimal ISP risk because its methods are highly cohesive.

### The "one more method" test

When someone asks to add a method to an existing interface, ask yourself: "Will every existing client of this interface benefit from or be unaffected by this addition?" If the answer is yes, add the method. If the answer is "no, this is only for the new admin panel," create a new interface for the admin panel's needs. This single question, asked consistently, prevents most ISP violations from ever forming.

## Part 12: A Real-World Example from This Project

Observer Magazine itself — the Blazor WebAssembly application you are reading right now — applies ISP throughout its service layer. Here is a concrete example.

The application has an analytics service for tracking page views and reactions. The original design might have been a single `IAnalyticsService`:

```csharp
public interface IAnalyticsService
{
    Task TrackPageViewAsync(string pageName, string details = "");
    Task IncrementViewAsync(string slug);
    Task<int?> GetViewCountAsync(string slug);
    Task AddReactionAsync(string slug, string reaction);
    Task<Dictionary<string, int>?> GetReactionsAsync(string slug);
}
```

But consider the consumers. The `Blog.razor` page only calls `TrackPageViewAsync` to record that someone visited the blog index. The `BlogPost.razor` page calls `IncrementViewAsync`, `GetViewCountAsync`, and `GetReactionsAsync`. The `Reactions.razor` component calls `AddReactionAsync` and `GetReactionsAsync`.

Different components use different subsets. In a fully ISP-compliant design, these would be separate interfaces. In practice, for a project this size, the trade-off is debatable — the interface is small, the team is small, and the cost of the coupling is low. But if the analytics service grows to include A/B testing, funnel tracking, and conversion metrics, the pressure to split will increase. Knowing where to draw the line is as important as knowing the principle.

## Part 13: ISP in the Age of Source Generators and AOT

Modern .NET 10 introduces patterns that interact with ISP in interesting ways.

### Source generators and minimal interfaces

Source generators in .NET can produce boilerplate code from interfaces. The `System.Text.Json` source generator, for example, reads your serialization attributes and generates optimized serializer code at compile time. For this to work well, the interfaces your generators consume should be focused and stable. A fat interface that changes frequently will trigger frequent regeneration and recompilation — echoing the original Xerox build-time problem.

### Native AOT and interface dispatch

Native Ahead-of-Time compilation eliminates the JIT compiler and produces native binaries. One consequence: the AOT compiler must statically analyze all possible interface implementations at compile time. Fat interfaces with many implementations can increase the size of the dispatch tables the compiler generates. Well-segregated interfaces with fewer implementations per interface produce leaner binaries. This is a marginal concern for most applications, but it becomes relevant at the edges — embedded systems, serverless functions with tight cold-start budgets, and mobile applications where binary size matters.

### Keyed services in .NET 8+

.NET 8 introduced keyed services in the DI container, allowing you to register multiple implementations of the same interface distinguished by a key:

```csharp
builder.Services.AddKeyedScoped<IUserReader, CachedUserReader>("cached");
builder.Services.AddKeyedScoped<IUserReader, SqlUserReader>("sql");
```

This interacts with ISP by making it easier to have multiple implementations of the same focused interface for different contexts (cached for the web layer, direct SQL for the admin layer). Without segregated interfaces, keyed services become harder to use because the keys would need to distinguish not just the implementation but also the subset of the interface the consumer needs.

## Part 14: Summary and Takeaways

The Interface Segregation Principle is one of the most practical of the SOLID principles. It directly addresses a problem that every growing codebase eventually faces: interfaces that started simple and grew fat as requirements accumulated. The principle is not about counting methods or enforcing a maximum interface size. It is about ensuring that each consumer of an interface depends only on the capabilities it actually uses.

The key ideas to carry with you:

**Design interfaces from the client's perspective.** Ask "what does this consumer need?" not "what can this class do?" The answers to those two questions should produce different interfaces.

**The .NET BCL is your teacher.** Study the progression from `IEnumerable<T>` to `IReadOnlyCollection<T>` to `IReadOnlyList<T>` to `ICollection<T>` to `IList<T>`. Each step adds a narrow slice of capability. This is ISP done well.

**Composition over proliferation.** When you split interfaces, compose them back together for clients that need the full surface area. `IUserRepository : IUserReader, IUserWriter` is idiomatic C#.

**The principle is fractal.** ISP applies at the class level (C# interfaces), the service level (REST APIs, gRPC services), the system level (microservice boundaries), and the event level (message contracts). The same question — "is this consumer forced to depend on things it does not use?" — applies everywhere.

**Know when to stop.** Not every interface needs splitting. Not every three-method interface hides an ISP violation. Apply the principle when you see the symptoms: bloated mocks, unrelated recompilations, `NotImplementedException`, and clients that use three out of twelve methods.

## Resources

Here are the key resources for further study:

- Robert C. Martin, *Agile Software Development: Principles, Patterns, and Practices* (Prentice Hall, 2002) — the original book-length treatment of all five SOLID principles, including the ISP chapter with the Xerox story and ATM transaction example.
- Robert C. Martin, "The Interface Segregation Principle" — the original article available at [https://web.archive.org/web/20150924054349/http://www.objectmentor.com/resources/articles/isp.pdf](https://web.archive.org/web/20150924054349/http://www.objectmentor.com/resources/articles/isp.pdf)
- Microsoft, "Guidelines for Collections" — [https://learn.microsoft.com/en-us/dotnet/standard/design-guidelines/guidelines-for-collections](https://learn.microsoft.com/en-us/dotnet/standard/design-guidelines/guidelines-for-collections)
- NDepend Blog, "SOLID Design in C#: The Interface Segregation Principle (ISP) with Examples" — [https://blog.ndepend.com/solid-design-the-interface-segregation-principle-isp/](https://blog.ndepend.com/solid-design-the-interface-segregation-principle-isp/)
- DevIQ, "Interface Segregation Principle" — [https://deviq.com/principles/interface-segregation/](https://deviq.com/principles/interface-segregation/)
- Scott Hannen, "The Interface Segregation Principle Applied in C#/.NET" — [https://scotthannen.org/blog/2019/01/01/interface-segregation-principle-applied.html](https://scotthannen.org/blog/2019/01/01/interface-segregation-principle-applied.html)
- Vladimir Khorikov (Enterprise Craftsmanship), "IEnumerable vs IReadOnlyList" — [https://enterprisecraftsmanship.com/posts/ienumerable-vs-ireadonlylist/](https://enterprisecraftsmanship.com/posts/ienumerable-vs-ireadonlylist/)
