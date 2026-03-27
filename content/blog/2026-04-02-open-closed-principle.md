---
title: "The Open/Closed Principle: A Comprehensive Guide for .NET Developers"
date: 2026-04-02
author: observer-team
summary: A deep dive into the Open/Closed Principle — its origins with Bertrand Meyer in 1988, Robert C. Martin's reformulation in 1996, how to apply it in modern C# and ASP.NET Core with real code examples, which design patterns embody it, when to ignore it, and how it shapes testable, maintainable software architecture.
tags:
  - csharp
  - dotnet
  - solid
  - design-principles
  - software-architecture
  - best-practices
  - deep-dive
---

It is a Thursday afternoon. You are three weeks into a feature that calculates shipping costs for an e-commerce application. The original developer wrote a tidy class called `ShippingCalculator` with a `switch` statement that handles three carriers: UPS, FedEx, and USPS. The class works. It has been in production for two years. It has unit tests. Everyone is happy.

Then your product owner walks over and says, "We're adding DHL. And Amazon Logistics. And a regional carrier called OnTrac. Oh, and we need to support freight shipping for palletized orders. Can you have that done by next sprint?"

You open `ShippingCalculator.cs`. It is 400 lines long. The `switch` has grown tentacles. Every carrier's logic references shared local variables. The unit tests are brittle — each one constructs a fake order and asserts against a hardcoded dollar amount that was correct in 2024. You add the DHL case. A FedEx test breaks. You fix the FedEx test. The USPS case now returns the wrong surcharge. You spend the rest of the afternoon playing whack-a-mole with regressions.

This is the problem that the Open/Closed Principle exists to prevent.

## Part 1: What Is the Open/Closed Principle?

The Open/Closed Principle (OCP) is one of the five SOLID principles of object-oriented design. Its canonical formulation is deceptively simple:

> Software entities (classes, modules, functions, etc.) should be open for extension, but closed for modification.

That single sentence has generated more conference talks, blog posts, and heated Slack arguments than perhaps any other principle in software engineering. Let us break it down.

**Open for extension** means you can add new behavior to the entity. You can teach it new tricks. You can make it handle cases it did not handle before.

**Closed for modification** means you should not have to crack open the existing source code and change it to add that new behavior. The existing code — the code that is tested, deployed, and working in production — stays untouched.

The word "should" is doing heavy lifting here. The OCP is a principle, not a law. It describes an ideal to design toward, not an absolute rule that can never be broken. But when you manage to achieve it, the results are remarkable: new features arrive by writing new code, not by rewriting old code. Regressions drop. Deployments get smaller. Code reviews get easier. Your Thursday afternoons get less stressful.

### The "O" in SOLID

The SOLID acronym represents five principles that Robert C. Martin (widely known as Uncle Bob) consolidated in his 2000 paper *Design Principles and Design Patterns*. The acronym itself was coined around 2004 by Michael Feathers, who rearranged the initials into a memorable word:

- **S** — Single Responsibility Principle (SRP)
- **O** — Open/Closed Principle (OCP)
- **L** — Liskov Substitution Principle (LSP)
- **I** — Interface Segregation Principle (ISP)
- **D** — Dependency Inversion Principle (DIP)

The five principles are deeply interrelated. The OCP tells you what your goal is: build software that can be extended without modification. The Dependency Inversion Principle tells you how to get there: depend on abstractions, not concretions. The Liskov Substitution Principle tells you the rules your abstractions must follow. The Interface Segregation Principle tells you how to keep those abstractions lean. And the Single Responsibility Principle tells you how to scope each module so that extension points align with likely axes of change.

Think of SOLID as a constellation, not a checklist. The principles reinforce each other, and understanding the OCP in isolation is like understanding one star without seeing the pattern it belongs to.

## Part 2: A Brief History — From Meyer to Martin

### Bertrand Meyer and the Original Formulation (1988)

The Open/Closed Principle was first articulated by Bertrand Meyer in his 1988 book *Object-Oriented Software Construction*. Meyer was writing at a time when the software industry was grappling with a fundamental problem: libraries were hard to evolve. If you shipped a compiled library and a client depended on it, adding a field to a data structure or a method to a class could break every program that used that library. Recompilation cascades were real and expensive.

Meyer proposed a solution rooted in inheritance. His formulation went something like this: a class is *closed* because it can be compiled, stored in a library, baselined, and used by other classes without fear of change. But it is also *open* because any new class can inherit from it and add new fields, new methods, and new behavior — without modifying the original class or disturbing its existing clients.

In Meyer's world, the mechanism for achieving OCP was *implementation inheritance*. You extend behavior by subclassing. The parent class stays frozen. The child class adds what is new.

This was a reasonable idea in 1988. The dominant paradigm was procedural programming. Object-oriented languages like Eiffel (which Meyer himself created) and early C++ were still proving their worth. Inheritance was the exciting new tool, and Meyer wielded it well.

### Robert C. Martin and the Polymorphic Reformulation (1996)

By the mid-1990s, the software industry had learned some hard lessons about implementation inheritance. Deep inheritance hierarchies created tight coupling. The "fragile base class problem" — where changes to a parent class broke child classes in unexpected ways — became a recognized anti-pattern. Developers began to favor composition over inheritance, and interfaces over concrete base classes.

In 1996, Robert C. Martin published an article titled "The Open-Closed Principle" that reframed Meyer's idea for this new reality. Martin kept the core insight — software should be extensible without modification — but changed the mechanism. Instead of relying on implementation inheritance, Martin advocated for *abstracted interfaces*. You define a contract (an interface or an abstract base class), and then you create multiple implementations that can be polymorphically substituted for each other. The interface is closed to modification. New implementations are open for extension.

This is the version of the OCP that most developers know today. When someone says "follow the Open/Closed Principle," they almost always mean Martin's polymorphic formulation, not Meyer's inheritance-based one.

### Why the Distinction Matters

The difference between Meyer's OCP and Martin's OCP is not merely academic. It changes how you write code.

Meyer's approach says: "Here is a concrete class. Subclass it to add behavior." This leads to class hierarchies. It works well when the base class is genuinely designed for inheritance (think `Stream` in .NET, or `HttpMessageHandler`), but it falls apart when developers start subclassing everything in sight and end up with six levels of inheritance just to add a logging statement.

Martin's approach says: "Here is an interface. Implement it to add behavior." This leads to flat, composable architectures. It works well with dependency injection containers, plugin systems, and microservice boundaries. It is the approach that modern C# and ASP.NET Core are designed around.

Both formulations are valid. Both have their place. But for the rest of this article, when we say "OCP," we mean Martin's polymorphic formulation unless otherwise noted — because that is what you will use every day as a .NET developer.

## Part 3: The Problem — Code That Violates the OCP

Before we talk about how to follow the OCP, let us spend some time understanding what happens when you do not. Violations of the OCP are everywhere, and they tend to follow a few recognizable patterns.

### Pattern 1: The Giant Switch Statement

This is the most common violation. You have a method that does different things based on a type discriminator, and every time a new type appears, you add another case.

```csharp
public class InvoicePrinter
{
    public string Print(Invoice invoice)
    {
        switch (invoice.Type)
        {
            case InvoiceType.Standard:
                return FormatStandardInvoice(invoice);
            case InvoiceType.Recurring:
                return FormatRecurringInvoice(invoice);
            case InvoiceType.ProForma:
                return FormatProFormaInvoice(invoice);
            // When the business adds "Credit Note" next quarter,
            // you will be right back in this file adding another case.
            default:
                throw new ArgumentOutOfRangeException(
                    nameof(invoice.Type),
                    $"Unknown invoice type: {invoice.Type}");
        }
    }

    private string FormatStandardInvoice(Invoice invoice) { /* ... */ }
    private string FormatRecurringInvoice(Invoice invoice) { /* ... */ }
    private string FormatProFormaInvoice(Invoice invoice) { /* ... */ }
}
```

Every time a new invoice type is introduced, this class must be modified. That means recompiling, retesting, and redeploying the module that contains it — even though the existing invoice types have not changed at all.

### Pattern 2: The If-Else Chain

A close cousin of the switch statement. Instead of switching on an enum, you check conditions or types directly.

```csharp
public decimal CalculateDiscount(Customer customer, decimal orderTotal)
{
    if (customer.Tier == "Gold")
    {
        return orderTotal * 0.15m;
    }
    else if (customer.Tier == "Silver")
    {
        return orderTotal * 0.10m;
    }
    else if (customer.Tier == "Bronze")
    {
        return orderTotal * 0.05m;
    }
    else if (customer.Tier == "Employee")
    {
        return orderTotal * 0.25m;
    }
    else
    {
        return 0m;
    }
}
```

This code works perfectly — until the business invents a "Platinum" tier, or a "Loyalty Program" tier, or a "Black Friday Override" tier. Each addition requires modifying this method.

### Pattern 3: The Type-Checking Method

This one is especially insidious because it often hides behind the `is` keyword in C#.

```csharp
public void ProcessPayment(IPayment payment)
{
    if (payment is CreditCardPayment cc)
    {
        ChargeCreditCard(cc.CardNumber, cc.Amount);
    }
    else if (payment is BankTransferPayment bt)
    {
        InitiateBankTransfer(bt.Iban, bt.Amount);
    }
    else if (payment is CryptoPayment crypto)
    {
        SendCrypto(crypto.WalletAddress, crypto.Amount);
    }
    else
    {
        throw new NotSupportedException(
            $"Payment type {payment.GetType().Name} is not supported.");
    }
}
```

You have an interface (`IPayment`), which looks like you are following the OCP. But then you immediately undermine it by checking the concrete type and branching. The interface is just window dressing. This method still needs to be modified every time a new payment type is added.

### Why Do These Violations Happen?

They happen because they are the *easiest* thing to write in the moment. When you have one or two cases, a `switch` or `if-else` is perfectly readable. It is only when the third, fourth, and tenth cases arrive that the pain becomes acute. The OCP is fundamentally about anticipating change — not in a crystal-ball way, but in a "what kind of change is likely in this domain?" way.

The shipping calculator will probably need new carriers. The invoice printer will probably need new invoice types. The payment processor will probably need new payment methods. If you can see the axis of change, you can design for it.

## Part 4: Applying the OCP in C# — The Basics

Let us fix the violations from Part 3. The core technique is always the same: extract the varying behavior behind an abstraction, and let new behavior arrive as new implementations of that abstraction.

### Step 1: Define an Abstraction

Start by identifying the behavior that changes. In the invoice printer example, the thing that changes is how each invoice type is formatted. So we define an interface for that behavior:

```csharp
public interface IInvoiceFormatter
{
    InvoiceType SupportedType { get; }
    string Format(Invoice invoice);
}
```

### Step 2: Implement the Abstraction for Each Case

Each existing case in the switch statement becomes its own class:

```csharp
public class StandardInvoiceFormatter : IInvoiceFormatter
{
    public InvoiceType SupportedType => InvoiceType.Standard;

    public string Format(Invoice invoice)
    {
        // All the logic that was in FormatStandardInvoice()
        var sb = new StringBuilder();
        sb.AppendLine($"INVOICE #{invoice.Number}");
        sb.AppendLine($"Date: {invoice.Date:yyyy-MM-dd}");
        sb.AppendLine($"Customer: {invoice.CustomerName}");
        sb.AppendLine();
        foreach (var line in invoice.Lines)
        {
            sb.AppendLine($"  {line.Description,-40} {line.Amount,12:C}");
        }
        sb.AppendLine(new string('-', 54));
        sb.AppendLine($"  {"Total",-40} {invoice.Total,12:C}");
        return sb.ToString();
    }
}

public class RecurringInvoiceFormatter : IInvoiceFormatter
{
    public InvoiceType SupportedType => InvoiceType.Recurring;

    public string Format(Invoice invoice)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"RECURRING INVOICE #{invoice.Number}");
        sb.AppendLine($"Billing Period: {invoice.PeriodStart:MMM yyyy} - {invoice.PeriodEnd:MMM yyyy}");
        sb.AppendLine($"Next Charge: {invoice.NextChargeDate:yyyy-MM-dd}");
        sb.AppendLine($"Customer: {invoice.CustomerName}");
        sb.AppendLine();
        foreach (var line in invoice.Lines)
        {
            sb.AppendLine($"  {line.Description,-40} {line.Amount,12:C}");
        }
        sb.AppendLine(new string('-', 54));
        sb.AppendLine($"  {"Monthly Total",-40} {invoice.Total,12:C}");
        return sb.ToString();
    }
}

public class ProFormaInvoiceFormatter : IInvoiceFormatter
{
    public InvoiceType SupportedType => InvoiceType.ProForma;

    public string Format(Invoice invoice)
    {
        var sb = new StringBuilder();
        sb.AppendLine("*** PRO FORMA — NOT A TAX INVOICE ***");
        sb.AppendLine($"Estimate #{invoice.Number}");
        sb.AppendLine($"Valid Until: {invoice.ExpiryDate:yyyy-MM-dd}");
        sb.AppendLine($"Prepared For: {invoice.CustomerName}");
        sb.AppendLine();
        foreach (var line in invoice.Lines)
        {
            sb.AppendLine($"  {line.Description,-40} {line.Amount,12:C}");
        }
        sb.AppendLine(new string('-', 54));
        sb.AppendLine($"  {"Estimated Total",-40} {invoice.Total,12:C}");
        return sb.ToString();
    }
}
```

### Step 3: Compose via the Abstraction

Now the `InvoicePrinter` depends only on the interface, not on any specific formatter:

```csharp
public class InvoicePrinter
{
    private readonly IReadOnlyDictionary<InvoiceType, IInvoiceFormatter> _formatters;

    public InvoicePrinter(IEnumerable<IInvoiceFormatter> formatters)
    {
        _formatters = formatters.ToDictionary(f => f.SupportedType);
    }

    public string Print(Invoice invoice)
    {
        if (!_formatters.TryGetValue(invoice.Type, out var formatter))
        {
            throw new NotSupportedException(
                $"No formatter registered for invoice type '{invoice.Type}'.");
        }

        return formatter.Format(invoice);
    }
}
```

This class is now **closed for modification**. You will never need to change it again (unless the fundamental concept of "invoice printing" itself changes, which is a different kind of change — more on that later). And it is **open for extension**: when the business adds "Credit Note" as a new invoice type, you write a single new class:

```csharp
public class CreditNoteFormatter : IInvoiceFormatter
{
    public InvoiceType SupportedType => InvoiceType.CreditNote;

    public string Format(Invoice invoice)
    {
        var sb = new StringBuilder();
        sb.AppendLine("*** CREDIT NOTE ***");
        sb.AppendLine($"Credit Note #{invoice.Number}");
        sb.AppendLine($"Original Invoice: #{invoice.OriginalInvoiceNumber}");
        sb.AppendLine($"Customer: {invoice.CustomerName}");
        sb.AppendLine();
        foreach (var line in invoice.Lines)
        {
            sb.AppendLine($"  {line.Description,-40} {line.Amount,12:C}");
        }
        sb.AppendLine(new string('-', 54));
        sb.AppendLine($"  {"Credit Total",-40} {invoice.Total,12:C}");
        return sb.ToString();
    }
}
```

Register it in your dependency injection container, and you are done. The `InvoicePrinter` never knew it existed, never needed to be recompiled, and never needed to be retested. The only new code is the `CreditNoteFormatter` itself and its own unit tests.

### Step 4: Wire It Up in DI

In ASP.NET Core (or any application using `Microsoft.Extensions.DependencyInjection`), registration looks like this:

```csharp
builder.Services.AddSingleton<IInvoiceFormatter, StandardInvoiceFormatter>();
builder.Services.AddSingleton<IInvoiceFormatter, RecurringInvoiceFormatter>();
builder.Services.AddSingleton<IInvoiceFormatter, ProFormaInvoiceFormatter>();
builder.Services.AddSingleton<IInvoiceFormatter, CreditNoteFormatter>();

builder.Services.AddSingleton<InvoicePrinter>();
```

When the DI container resolves `InvoicePrinter`, it will inject an `IEnumerable<IInvoiceFormatter>` containing all registered formatters. The printer builds its dictionary and is ready to go.

This is the textbook OCP refactoring. It works for the discount calculator (extract an `IDiscountStrategy` interface), for the payment processor (let each `IPayment` implementation carry its own `Process()` method), and for the shipping calculator that started this article (extract an `IShippingRateProvider` interface with one implementation per carrier).

## Part 5: Design Patterns That Embody the OCP

The OCP is not just a principle — it is the conceptual foundation beneath many of the classic design patterns from the Gang of Four book and beyond. If you have ever used one of these patterns, you were following the OCP, even if you did not call it by name.

### Strategy Pattern

The Strategy pattern is the most direct expression of the OCP. You define a family of algorithms (strategies), encapsulate each one behind a common interface, and make them interchangeable. The context class (the one that uses the strategy) never changes when a new strategy is added.

We already saw this with the invoice formatter example. Here is another example — a file compression service:

```csharp
public interface ICompressionStrategy
{
    string FileExtension { get; }
    byte[] Compress(byte[] data);
    byte[] Decompress(byte[] data);
}

public class GzipCompression : ICompressionStrategy
{
    public string FileExtension => ".gz";

    public byte[] Compress(byte[] data)
    {
        using var output = new MemoryStream();
        using (var gzip = new GZipStream(output, CompressionLevel.Optimal))
        {
            gzip.Write(data, 0, data.Length);
        }
        return output.ToArray();
    }

    public byte[] Decompress(byte[] data)
    {
        using var input = new MemoryStream(data);
        using var gzip = new GZipStream(input, CompressionMode.Decompress);
        using var output = new MemoryStream();
        gzip.CopyTo(output);
        return output.ToArray();
    }
}

public class BrotliCompression : ICompressionStrategy
{
    public string FileExtension => ".br";

    public byte[] Compress(byte[] data)
    {
        using var output = new MemoryStream();
        using (var brotli = new BrotliStream(output, CompressionLevel.Optimal))
        {
            brotli.Write(data, 0, data.Length);
        }
        return output.ToArray();
    }

    public byte[] Decompress(byte[] data)
    {
        using var input = new MemoryStream(data);
        using var brotli = new BrotliStream(input, CompressionMode.Decompress);
        using var output = new MemoryStream();
        brotli.CopyTo(output);
        return output.ToArray();
    }
}
```

Adding Zstandard compression next year? Write a `ZstdCompression` class. Nothing else changes.

### Decorator Pattern

The Decorator pattern lets you wrap an existing object with additional behavior, without modifying the original. Each decorator implements the same interface as the object it wraps, so decorators are invisible to the consumer.

```csharp
public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(int id);
    Task SaveAsync(Order order);
}

// The base implementation — talks to the database
public class SqlOrderRepository : IOrderRepository
{
    private readonly DbContext _db;

    public SqlOrderRepository(DbContext db) => _db = db;

    public async Task<Order?> GetByIdAsync(int id)
        => await _db.Set<Order>().FindAsync(id);

    public async Task SaveAsync(Order order)
    {
        _db.Set<Order>().Update(order);
        await _db.SaveChangesAsync();
    }
}

// A decorator that adds caching — does not modify SqlOrderRepository
public class CachedOrderRepository : IOrderRepository
{
    private readonly IOrderRepository _inner;
    private readonly IMemoryCache _cache;
    private readonly ILogger<CachedOrderRepository> _logger;

    public CachedOrderRepository(
        IOrderRepository inner,
        IMemoryCache cache,
        ILogger<CachedOrderRepository> logger)
    {
        _inner = inner;
        _cache = cache;
        _logger = logger;
    }

    public async Task<Order?> GetByIdAsync(int id)
    {
        var cacheKey = $"order:{id}";
        if (_cache.TryGetValue(cacheKey, out Order? cached))
        {
            _logger.LogDebug("Cache hit for order {OrderId}", id);
            return cached;
        }

        var order = await _inner.GetByIdAsync(id);
        if (order is not null)
        {
            _cache.Set(cacheKey, order, TimeSpan.FromMinutes(5));
        }

        return order;
    }

    public async Task SaveAsync(Order order)
    {
        await _inner.SaveAsync(order);
        _cache.Remove($"order:{order.Id}");
    }
}

// A decorator that adds audit logging — does not modify either of the above
public class AuditedOrderRepository : IOrderRepository
{
    private readonly IOrderRepository _inner;
    private readonly IAuditLog _auditLog;

    public AuditedOrderRepository(IOrderRepository inner, IAuditLog auditLog)
    {
        _inner = inner;
        _auditLog = auditLog;
    }

    public Task<Order?> GetByIdAsync(int id) => _inner.GetByIdAsync(id);

    public async Task SaveAsync(Order order)
    {
        await _inner.SaveAsync(order);
        await _auditLog.RecordAsync("Order", order.Id, "Saved");
    }
}
```

You can stack decorators: `AuditedOrderRepository` wrapping `CachedOrderRepository` wrapping `SqlOrderRepository`. Each layer adds behavior without modifying the layers beneath it. The `SqlOrderRepository` class does not know it is being cached or audited.

In ASP.NET Core DI, you can wire this up using the `Scrutor` library or manually:

```csharp
builder.Services.AddScoped<SqlOrderRepository>();
builder.Services.AddScoped<IOrderRepository>(sp =>
{
    var sql = sp.GetRequiredService<SqlOrderRepository>();
    var cache = sp.GetRequiredService<IMemoryCache>();
    var cacheLogger = sp.GetRequiredService<ILogger<CachedOrderRepository>>();
    var cached = new CachedOrderRepository(sql, cache, cacheLogger);
    var auditLog = sp.GetRequiredService<IAuditLog>();
    return new AuditedOrderRepository(cached, auditLog);
});
```

### Template Method Pattern

The Template Method pattern defines the skeleton of an algorithm in a base class and lets subclasses override specific steps. This is one of the few places where Meyer's original inheritance-based OCP still shines.

```csharp
public abstract class ReportGenerator
{
    // The template method — defines the algorithm's structure
    public string Generate(ReportData data)
    {
        var sb = new StringBuilder();
        sb.AppendLine(CreateHeader(data));
        sb.AppendLine(CreateBody(data));
        sb.AppendLine(CreateFooter(data));
        return sb.ToString();
    }

    protected abstract string CreateHeader(ReportData data);
    protected abstract string CreateBody(ReportData data);

    // A default implementation that subclasses can override if needed
    protected virtual string CreateFooter(ReportData data)
        => $"Generated on {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC";
}

public class HtmlReportGenerator : ReportGenerator
{
    protected override string CreateHeader(ReportData data)
        => $"<html><head><title>{data.Title}</title></head><body><h1>{data.Title}</h1>";

    protected override string CreateBody(ReportData data)
    {
        var sb = new StringBuilder("<table>");
        foreach (var row in data.Rows)
        {
            sb.Append("<tr>");
            foreach (var cell in row)
            {
                sb.Append($"<td>{cell}</td>");
            }
            sb.Append("</tr>");
        }
        sb.Append("</table>");
        return sb.ToString();
    }

    protected override string CreateFooter(ReportData data)
        => $"<footer>Generated on {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC</footer></body></html>";
}

public class CsvReportGenerator : ReportGenerator
{
    protected override string CreateHeader(ReportData data)
        => string.Join(",", data.ColumnNames);

    protected override string CreateBody(ReportData data)
    {
        var sb = new StringBuilder();
        foreach (var row in data.Rows)
        {
            sb.AppendLine(string.Join(",", row.Select(EscapeCsv)));
        }
        return sb.ToString();
    }

    private static string EscapeCsv(string value)
        => value.Contains(',') || value.Contains('"')
            ? $"\"{value.Replace("\"", "\"\"")}\""
            : value;
}
```

The `Generate()` method in `ReportGenerator` is closed for modification. The individual steps (`CreateHeader`, `CreateBody`, `CreateFooter`) are open for extension via subclassing.

### Factory Method Pattern

The Factory Method pattern delegates object creation to subclasses or to factory methods, so you can introduce new product types without modifying the code that consumes them.

```csharp
public interface INotification
{
    Task SendAsync(string recipient, string message);
}

public class EmailNotification : INotification
{
    private readonly IEmailClient _emailClient;

    public EmailNotification(IEmailClient emailClient) => _emailClient = emailClient;

    public async Task SendAsync(string recipient, string message)
        => await _emailClient.SendAsync(recipient, "Notification", message);
}

public class SmsNotification : INotification
{
    private readonly ISmsGateway _gateway;

    public SmsNotification(ISmsGateway gateway) => _gateway = gateway;

    public async Task SendAsync(string recipient, string message)
        => await _gateway.SendTextAsync(recipient, message);
}

public class PushNotification : INotification
{
    private readonly IPushService _pushService;

    public PushNotification(IPushService pushService) => _pushService = pushService;

    public async Task SendAsync(string recipient, string message)
        => await _pushService.PushAsync(recipient, message);
}
```

When the business says "we need Slack notifications too," you write a `SlackNotification` class, register it, and nothing else needs to change.

### Observer Pattern (Events and Delegates)

C# has first-class support for the Observer pattern through events and delegates. This is OCP in action: the publisher defines an event, and any number of subscribers can attach to it without the publisher knowing or caring.

```csharp
public class OrderService
{
    // The event — an extension point
    public event Func<Order, Task>? OrderPlaced;

    public async Task PlaceOrderAsync(Order order)
    {
        // Core business logic
        order.Status = OrderStatus.Placed;
        order.PlacedAt = DateTime.UtcNow;
        await _repository.SaveAsync(order);

        // Notify all subscribers — OrderService does not know who they are
        if (OrderPlaced is not null)
        {
            foreach (var handler in OrderPlaced.GetInvocationList().Cast<Func<Order, Task>>())
            {
                await handler(order);
            }
        }
    }
}
```

Subscribers attach from outside:

```csharp
orderService.OrderPlaced += async order =>
    await emailService.SendOrderConfirmationAsync(order);

orderService.OrderPlaced += async order =>
    await inventoryService.ReserveStockAsync(order);

orderService.OrderPlaced += async order =>
    await analyticsService.TrackOrderAsync(order);
```

Adding a new side effect to order placement does not require modifying `OrderService`. That is the OCP.

## Part 6: The OCP in ASP.NET Core

ASP.NET Core is one of the best examples of OCP-friendly architecture in the .NET ecosystem. Several of its core abstractions are explicitly designed so you can extend behavior without modifying framework code.

### The Middleware Pipeline

The ASP.NET Core request pipeline is a chain of middleware components. Each middleware processes the request, optionally calls the next middleware in the chain, and then processes the response on the way back out. The pipeline itself is closed for modification — the `WebApplication` class does not need to change when you add a new middleware. But it is open for extension — you can insert new middleware at any point in the chain.

```csharp
var app = builder.Build();

// Each of these extends the pipeline without modifying any existing middleware
app.UseExceptionHandler("/Error");
app.UseHsts();
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

// Your custom middleware — open for extension
app.UseMiddleware<RequestTimingMiddleware>();
app.UseMiddleware<TenantResolutionMiddleware>();

app.MapControllers();
app.Run();
```

Writing a custom middleware is adding new behavior without modifying any existing code:

```csharp
public class RequestTimingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestTimingMiddleware> _logger;

    public RequestTimingMiddleware(RequestDelegate next, ILogger<RequestTimingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();

        await _next(context);

        stopwatch.Stop();
        _logger.LogInformation(
            "Request {Method} {Path} completed in {ElapsedMs}ms with status {StatusCode}",
            context.Request.Method,
            context.Request.Path,
            stopwatch.ElapsedMilliseconds,
            context.Response.StatusCode);
    }
}
```

### Dependency Injection and Service Registration

The DI container in ASP.NET Core is itself an OCP-friendly system. You register services against interfaces, and consumers depend on those interfaces. When you need to swap an implementation — say, replacing an in-memory cache with Redis — you change the registration, not the consumer.

```csharp
// Development: use in-memory
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddSingleton<ICacheService, InMemoryCacheService>();
}
else
{
    // Production: use Redis — no consumer code changes
    builder.Services.AddSingleton<ICacheService, RedisCacheService>();
}
```

### Configuration and Options Pattern

The Options pattern (`IOptions<T>`, `IOptionsSnapshot<T>`, `IOptionsMonitor<T>`) lets you extend application behavior through configuration without modifying code. Feature flags are a natural expression of the OCP:

```csharp
public class FeatureFlags
{
    public bool EnableNewCheckoutFlow { get; set; }
    public bool EnableRecommendationEngine { get; set; }
    public bool EnableBetaDashboard { get; set; }
}

// In Program.cs
builder.Services.Configure<FeatureFlags>(
    builder.Configuration.GetSection("Features"));

// In a controller or service
public class CheckoutController : ControllerBase
{
    private readonly IOptionsSnapshot<FeatureFlags> _features;

    public CheckoutController(IOptionsSnapshot<FeatureFlags> features)
        => _features = features;

    [HttpPost]
    public async Task<IActionResult> Checkout(CheckoutRequest request)
    {
        if (_features.Value.EnableNewCheckoutFlow)
        {
            return await NewCheckoutFlowAsync(request);
        }

        return await LegacyCheckoutFlowAsync(request);
    }
}
```

The `if` statement here might look like an OCP violation, but it is not — this is *feature toggling*, a controlled, temporary branching mechanism. The key distinction is that the toggle will be removed once the new flow is validated and the old flow is deleted. It is not a permanent, ever-growing branching mechanism like the switch statement in Part 3.

### Minimal APIs and Endpoint Filters

Minimal APIs in ASP.NET Core support endpoint filters, which are another expression of the OCP. You can attach cross-cutting behavior to endpoints without modifying the endpoint handler itself:

```csharp
app.MapPost("/api/orders", async (CreateOrderRequest request, IOrderService service) =>
{
    var order = await service.CreateAsync(request);
    return Results.Created($"/api/orders/{order.Id}", order);
})
.AddEndpointFilter<ValidationFilter<CreateOrderRequest>>()
.AddEndpointFilter<AuditLogFilter>()
.RequireAuthorization("OrderCreator");
```

Each filter extends the endpoint's behavior. The handler itself does not know about validation, audit logging, or authorization. Those concerns are composed from outside.

## Part 7: The OCP with Modern C# Features

C# has evolved significantly since the OCP was first formulated. Several modern language features make it easier to follow the principle — and a few can tempt you into violating it.

### Generics

Generics are a powerful tool for building OCP-compliant abstractions. A generic interface or class can work with types that do not exist yet when the generic is written.

```csharp
public interface IRepository<T> where T : class, IEntity
{
    Task<T?> GetByIdAsync(int id);
    Task<IReadOnlyList<T>> GetAllAsync();
    Task AddAsync(T entity);
    Task UpdateAsync(T entity);
    Task DeleteAsync(int id);
}

public class EfRepository<T> : IRepository<T> where T : class, IEntity
{
    private readonly AppDbContext _context;

    public EfRepository(AppDbContext context) => _context = context;

    public async Task<T?> GetByIdAsync(int id)
        => await _context.Set<T>().FindAsync(id);

    public async Task<IReadOnlyList<T>> GetAllAsync()
        => await _context.Set<T>().ToListAsync();

    public async Task AddAsync(T entity)
    {
        await _context.Set<T>().AddAsync(entity);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(T entity)
    {
        _context.Set<T>().Update(entity);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await GetByIdAsync(id);
        if (entity is not null)
        {
            _context.Set<T>().Remove(entity);
            await _context.SaveChangesAsync();
        }
    }
}
```

When you add a new entity type (`Invoice`, `Customer`, `Product`), you do not modify `EfRepository<T>`. You just use it with the new type. That is OCP through generics.

### Delegates and Func/Action

You do not always need a full interface to achieve OCP. Sometimes a delegate is enough. Delegates are the smallest possible abstraction — a single method signature.

```csharp
public class RetryHandler
{
    public async Task<T> ExecuteWithRetryAsync<T>(
        Func<Task<T>> operation,
        int maxRetries = 3,
        TimeSpan? delay = null)
    {
        var retryDelay = delay ?? TimeSpan.FromSeconds(1);

        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                return await operation();
            }
            catch (Exception ex) when (attempt < maxRetries)
            {
                await Task.Delay(retryDelay * attempt);
            }
        }

        return await operation(); // Final attempt — let it throw
    }
}
```

This class can retry *any* async operation without knowing what that operation does. It is closed for modification. You extend it by passing in different `Func<Task<T>>` delegates — which is open for extension.

### Extension Methods

Extension methods let you add behavior to existing types without modifying them. This is literally the OCP at the language level.

```csharp
public static class StringExtensions
{
    public static string Truncate(this string value, int maxLength)
    {
        if (string.IsNullOrEmpty(value)) return value;
        return value.Length <= maxLength
            ? value
            : value[..maxLength] + "…";
    }

    public static string ToSlug(this string value)
    {
        var slug = value.ToLowerInvariant();
        slug = Regex.Replace(slug, @"[^a-z0-9\s-]", "");
        slug = Regex.Replace(slug, @"\s+", "-");
        slug = Regex.Replace(slug, @"-+", "-");
        return slug.Trim('-');
    }
}
```

The `string` class is closed for modification (you cannot change it — it is in the BCL). But it is open for extension via extension methods.

### A Word of Caution: Pattern Matching and Switch Expressions

C# has made pattern matching and switch expressions beautifully concise. This can actually make OCP violations *more* attractive, because they look so clean:

```csharp
public decimal CalculateTax(Address address) => address.State switch
{
    "CA" => address.SubTotal * 0.0725m,
    "TX" => address.SubTotal * 0.0625m,
    "NY" => address.SubTotal * 0.08m,
    "OR" => 0m, // No sales tax
    _ => address.SubTotal * 0.05m
};
```

This is elegant, readable, and a clear OCP violation. Every time a state's tax rate changes or a new state is added, you modify this method. Whether that matters depends on context. If tax rates change frequently and the calculation is complex (considering county taxes, exemptions, thresholds), you should extract a strategy. If the rates are stable and the calculation is trivial, the switch expression might be perfectly fine. The OCP is a guide, not a religion.

## Part 8: The OCP and Testability

One of the most practical benefits of following the OCP is that it makes your code dramatically easier to test. When behavior is hidden behind abstractions, you can substitute test doubles (mocks, stubs, fakes) without any ceremony.

### Testing OCP-Compliant Code

Consider the `InvoicePrinter` from Part 4. Testing it is trivial because it depends on `IInvoiceFormatter`, not on concrete implementations:

```csharp
public class InvoicePrinterTests
{
    [Fact]
    public void Print_UsesCorrectFormatterForInvoiceType()
    {
        // Arrange
        var invoice = new Invoice
        {
            Type = InvoiceType.Standard,
            Number = "INV-001",
            CustomerName = "Acme Corp",
            Lines = [new InvoiceLine("Widget", 99.99m)],
            Total = 99.99m
        };

        var mockFormatter = new TestInvoiceFormatter(
            InvoiceType.Standard,
            "FORMATTED OUTPUT");

        var printer = new InvoicePrinter([mockFormatter]);

        // Act
        var result = printer.Print(invoice);

        // Assert
        Assert.Equal("FORMATTED OUTPUT", result);
    }

    [Fact]
    public void Print_ThrowsForUnregisteredInvoiceType()
    {
        // Arrange
        var invoice = new Invoice { Type = InvoiceType.CreditNote };
        var printer = new InvoicePrinter([]); // No formatters registered

        // Act & Assert
        Assert.Throws<NotSupportedException>(() => printer.Print(invoice));
    }

    private class TestInvoiceFormatter : IInvoiceFormatter
    {
        private readonly string _output;
        public InvoiceType SupportedType { get; }

        public TestInvoiceFormatter(InvoiceType type, string output)
        {
            SupportedType = type;
            _output = output;
        }

        public string Format(Invoice invoice) => _output;
    }
}
```

Notice how the test does not need to know anything about how standard invoices are actually formatted. It tests the *printer's* behavior (routing to the correct formatter) in isolation. The formatter's behavior is tested separately, in `StandardInvoiceFormatterTests`.

### Testing Without OCP

Compare this to testing the original switch-based `InvoicePrinter`. You would need to construct a real invoice, call `Print()`, and assert against the actual formatted output. If the formatting logic changes, the test breaks. If you want to test the routing logic separately from the formatting logic, you cannot — they are entangled in the same method.

### The OCP Makes Mocking Unnecessary (Sometimes)

When your abstractions are simple enough, you do not even need a mocking framework. The `TestInvoiceFormatter` above is a hand-written fake — it took four lines of code. This is often clearer than using Moq or NSubstitute, because the fake's behavior is explicit and visible in the test.

For more complex interactions, mocking frameworks still have their place. But the OCP ensures that the seams where you inject mocks are well-defined and stable.

## Part 9: When NOT to Follow the OCP

The OCP is a tool, not a commandment. There are legitimate situations where following it would make your code worse, not better.

### When the Axis of Change Is Unknown

The OCP requires you to predict *where* change will happen so you can place an abstraction there. If you guess wrong, you end up with an abstraction that no one ever extends, and a codebase full of interfaces with exactly one implementation. This is sometimes called "speculative generality" — one of Martin Fowler's code smells.

Do not pre-abstract everything on the off chance it might change someday. Instead, follow the "Rule of Three": the first time you encounter a new variation, handle it inline. The second time, note the pattern. The third time, refactor to an abstraction. By the third occurrence, you have enough data to know what the actual axis of change is.

### When the Cost of Abstraction Exceeds the Cost of Modification

Every abstraction has a cost. It adds a file, an interface, a registration, and a level of indirection that the next developer must understand. If your switch statement has three cases and has not changed in two years, the OCP refactoring is not "better" — it is just more code.

Ask yourself: "What is the cost of modifying this code when the next case arrives?" If the answer is "five minutes and a recompile," the switch statement is fine. If the answer is "two hours of careful surgery in a 400-line method with 15 tests to update," it is time to refactor.

### When You Are Doing a Planned Refactoring

Following the OCP slavishly can prevent healthy refactoring. If you discover that your abstraction was wrong — that the interface is too broad, or the responsibilities are divided along the wrong axis — you need to modify the existing code. That is not a violation of the OCP. That is software development.

The OCP guides the *steady-state* evolution of a system: how you add new features to a stable codebase. It does not mean "never change existing code ever again." Refactoring, fixing bugs, updating dependencies, and redesigning modules are all legitimate reasons to modify existing code.

### When Performance Matters

Virtual dispatch (calling a method through an interface) has a small cost compared to a direct call. In most applications, this cost is negligible. But in hot paths — tight loops processing millions of items, real-time game physics, high-frequency trading — the overhead of abstraction can matter. In these cases, a well-optimized switch statement or even a lookup table might be the right choice.

Modern .NET has narrowed this gap considerably. The JIT compiler can devirtualize calls in many cases, and the performance difference between a virtual call and a direct call is often just a few nanoseconds. But if you are in a domain where nanoseconds matter, measure before abstracting.

### The Pragmatic Middle Ground

The best developers do not follow the OCP blindly, and they do not ignore it either. They develop an intuition for when an abstraction will pay for itself and when it will not. That intuition comes from experience — from seeing which switch statements grew out of control and which ones stayed stable for years.

A useful mental model: think of the OCP as *insurance*. You pay a small upfront cost (the abstraction) to protect against a future cost (modifying existing code). Like real insurance, it is not worth paying for unlikely risks. But for likely risks — a payment processor that will definitely need new payment methods, a notification system that will definitely need new channels — the premium is well worth it.

## Part 10: Common Criticisms and Misconceptions

The OCP has its share of critics, and some of their points are valid. Let us address the most common ones.

### "You Cannot Predict the Future"

This is the strongest criticism. The OCP asks you to design extension points, but you can only place them where you think change will happen. If you are wrong, the extension points are useless, and the change you did not anticipate requires modifying the code anyway.

The counterargument is that you do not need to predict the future perfectly. You just need to observe the past. If your payment processor has had three new payment methods added in the last year, it is a safe bet that a fourth is coming. If your report generator has had exactly one format for five years, it probably does not need an abstraction.

### "It Leads to Too Many Classes"

A strict application of the OCP can produce a proliferation of small classes: one interface, one implementation per case, one factory, one registration. For a system with twenty payment methods, that is at least twenty-two classes (the interface, the twenty implementations, and the service that uses them) instead of one class with a twenty-case switch.

This is a real trade-off. More classes means more files to navigate, more registrations to maintain, and more cognitive load for developers new to the codebase. The mitigation is to use consistent naming conventions (so the classes are predictable) and to keep each class small and focused (so they are easy to understand in isolation).

### "Interfaces With One Implementation Are a Waste"

If you have `IShippingCalculator` and `ShippingCalculator`, and no other implementations exist or are planned, the interface is just ceremony. Some developers (and some style guides) argue that you should not introduce an interface until you need a second implementation.

This is a reasonable position. The counterarguments are: (1) the interface makes the class testable via mocking, even if there is only one production implementation, and (2) the interface documents the contract, making it explicit what the class promises to do. Whether those benefits justify the extra file is a judgment call.

### "Martin's OCP Is Not Meyer's OCP"

This is historically accurate. Robert C. Martin's reformulation of the OCP using interfaces and polymorphism is substantially different from Bertrand Meyer's original formulation using implementation inheritance. Some purists argue that Martin co-opted the term and changed its meaning.

This is an interesting debate for historians of software engineering, but it is not very useful for working developers. Both formulations share the same core insight: systems are more maintainable when new behavior can be added without modifying existing code. The mechanism differs, but the goal is identical.

## Part 11: Real-World OCP — A Complete Example

Let us build a complete, realistic example that ties together everything we have discussed. Imagine you are building a document export service for a SaaS application. Users can export their data in various formats, and you expect the list of formats to grow over time.

### The Domain

```csharp
public record ExportRequest(
    string UserId,
    string DocumentId,
    string Format,
    ExportOptions Options);

public record ExportOptions(
    bool IncludeMetadata = true,
    bool IncludeComments = false,
    string? WatermarkText = null);

public record ExportResult(
    string FileName,
    string ContentType,
    byte[] Content);
```

### The Abstraction

```csharp
public interface IDocumentExporter
{
    /// <summary>
    /// The format identifier this exporter handles (e.g., "pdf", "docx", "csv").
    /// </summary>
    string Format { get; }

    /// <summary>
    /// Exports a document in this exporter's format.
    /// </summary>
    Task<ExportResult> ExportAsync(Document document, ExportOptions options);
}
```

### The Implementations

```csharp
public class PdfExporter : IDocumentExporter
{
    private readonly ILogger<PdfExporter> _logger;

    public PdfExporter(ILogger<PdfExporter> logger) => _logger = logger;

    public string Format => "pdf";

    public async Task<ExportResult> ExportAsync(Document document, ExportOptions options)
    {
        _logger.LogInformation("Exporting document {DocumentId} as PDF", document.Id);

        // In a real app, you would use a library like QuestPDF or iText
        var pdfBytes = await GeneratePdfAsync(document, options);

        return new ExportResult(
            FileName: $"{document.Title.ToSlug()}.pdf",
            ContentType: "application/pdf",
            Content: pdfBytes);
    }

    private Task<byte[]> GeneratePdfAsync(Document document, ExportOptions options)
    {
        // PDF generation logic here
        // This is where QuestPDF, iText, or similar would be used
        throw new NotImplementedException("PDF generation not shown for brevity");
    }
}

public class CsvExporter : IDocumentExporter
{
    public string Format => "csv";

    public Task<ExportResult> ExportAsync(Document document, ExportOptions options)
    {
        var sb = new StringBuilder();

        if (options.IncludeMetadata)
        {
            sb.AppendLine($"# Title: {document.Title}");
            sb.AppendLine($"# Author: {document.Author}");
            sb.AppendLine($"# Created: {document.CreatedAt:O}");
            sb.AppendLine();
        }

        sb.AppendLine("Section,Content");
        foreach (var section in document.Sections)
        {
            var escapedContent = section.Content.Replace("\"", "\"\"");
            sb.AppendLine($"\"{section.Title}\",\"{escapedContent}\"");
        }

        var bytes = Encoding.UTF8.GetBytes(sb.ToString());

        return Task.FromResult(new ExportResult(
            FileName: $"{document.Title.ToSlug()}.csv",
            ContentType: "text/csv",
            Content: bytes));
    }
}

public class MarkdownExporter : IDocumentExporter
{
    public string Format => "md";

    public Task<ExportResult> ExportAsync(Document document, ExportOptions options)
    {
        var sb = new StringBuilder();

        sb.AppendLine($"# {document.Title}");
        sb.AppendLine();

        if (options.IncludeMetadata)
        {
            sb.AppendLine($"*Author: {document.Author}*");
            sb.AppendLine($"*Created: {document.CreatedAt:yyyy-MM-dd}*");
            sb.AppendLine();
        }

        foreach (var section in document.Sections)
        {
            sb.AppendLine($"## {section.Title}");
            sb.AppendLine();
            sb.AppendLine(section.Content);
            sb.AppendLine();

            if (options.IncludeComments && section.Comments.Count > 0)
            {
                sb.AppendLine("### Comments");
                sb.AppendLine();
                foreach (var comment in section.Comments)
                {
                    sb.AppendLine($"> **{comment.Author}** ({comment.Date:yyyy-MM-dd}): {comment.Text}");
                    sb.AppendLine();
                }
            }
        }

        if (options.WatermarkText is not null)
        {
            sb.AppendLine("---");
            sb.AppendLine($"*{options.WatermarkText}*");
        }

        var bytes = Encoding.UTF8.GetBytes(sb.ToString());

        return Task.FromResult(new ExportResult(
            FileName: $"{document.Title.ToSlug()}.md",
            ContentType: "text/markdown",
            Content: bytes));
    }
}
```

### The Service

```csharp
public class DocumentExportService
{
    private readonly IReadOnlyDictionary<string, IDocumentExporter> _exporters;
    private readonly IDocumentRepository _documents;
    private readonly ILogger<DocumentExportService> _logger;

    public DocumentExportService(
        IEnumerable<IDocumentExporter> exporters,
        IDocumentRepository documents,
        ILogger<DocumentExportService> logger)
    {
        _exporters = exporters.ToDictionary(
            e => e.Format,
            StringComparer.OrdinalIgnoreCase);
        _documents = documents;
        _logger = logger;
    }

    public IReadOnlyCollection<string> SupportedFormats => _exporters.Keys.ToList();

    public async Task<ExportResult> ExportAsync(ExportRequest request)
    {
        if (!_exporters.TryGetValue(request.Format, out var exporter))
        {
            throw new NotSupportedException(
                $"Export format '{request.Format}' is not supported. " +
                $"Supported formats: {string.Join(", ", SupportedFormats)}");
        }

        var document = await _documents.GetByIdAsync(request.DocumentId)
            ?? throw new InvalidOperationException(
                $"Document '{request.DocumentId}' not found.");

        _logger.LogInformation(
            "User {UserId} exporting document {DocumentId} as {Format}",
            request.UserId,
            request.DocumentId,
            request.Format);

        return await exporter.ExportAsync(document, request.Options);
    }
}
```

### The API Endpoint

```csharp
app.MapGet("/api/export/formats", (DocumentExportService service) =>
    Results.Ok(service.SupportedFormats));

app.MapPost("/api/export", async (ExportRequest request, DocumentExportService service) =>
{
    var result = await service.ExportAsync(request);
    return Results.File(result.Content, result.ContentType, result.FileName);
})
.RequireAuthorization();
```

### The DI Registration

```csharp
builder.Services.AddSingleton<IDocumentExporter, PdfExporter>();
builder.Services.AddSingleton<IDocumentExporter, CsvExporter>();
builder.Services.AddSingleton<IDocumentExporter, MarkdownExporter>();
builder.Services.AddScoped<DocumentExportService>();
```

### Adding a New Format

Six months from now, a customer asks for JSON export. Here is the entire change:

```csharp
public class JsonExporter : IDocumentExporter
{
    public string Format => "json";

    public Task<ExportResult> ExportAsync(Document document, ExportOptions options)
    {
        var exportData = new
        {
            document.Title,
            document.Author,
            CreatedAt = document.CreatedAt.ToString("O"),
            Sections = document.Sections.Select(s => new
            {
                s.Title,
                s.Content,
                Comments = options.IncludeComments
                    ? s.Comments.Select(c => new { c.Author, c.Date, c.Text })
                    : null
            }),
            Watermark = options.WatermarkText
        };

        var json = JsonSerializer.Serialize(exportData, new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        });

        var bytes = Encoding.UTF8.GetBytes(json);

        return Task.FromResult(new ExportResult(
            FileName: $"{document.Title.ToSlug()}.json",
            ContentType: "application/json",
            Content: bytes));
    }
}
```

And one line in DI registration:

```csharp
builder.Services.AddSingleton<IDocumentExporter, JsonExporter>();
```

That is it. The `DocumentExportService` was not modified. The API endpoints were not modified. The existing exporters were not modified. No existing tests were broken. The only new code is the `JsonExporter` class, its unit tests, and one line of registration.

This is the Open/Closed Principle at work.

## Part 12: OCP Beyond Object-Oriented Programming

The OCP is usually discussed in the context of OOP, but the underlying idea — new behavior via new code, not by modifying old code — applies to other paradigms as well.

### Functional Approaches

In functional programming, the OCP manifests through higher-order functions, pattern matching on discriminated unions, and composition.

```csharp
// A pipeline of transformations — each function extends behavior
// without modifying the others
public static class TextPipeline
{
    public static string Process(
        string input,
        params Func<string, string>[] transforms)
    {
        return transforms.Aggregate(input, (current, transform) => transform(current));
    }
}

// Usage — adding a new transform is just passing another function
var result = TextPipeline.Process(
    rawText,
    text => text.Trim(),
    text => text.ToLowerInvariant(),
    text => Regex.Replace(text, @"\s+", " "),
    text => text.Replace("colour", "color") // New transformation — nothing modified
);
```

The `Process` method is closed for modification. You extend it by passing in additional functions.

### Event-Driven and Message-Based Systems

In event-driven architectures, the OCP appears naturally. A message broker (like RabbitMQ, Azure Service Bus, or even an in-process `MediatR` pipeline) routes messages to handlers. Adding a new handler for an existing message type, or adding a handler for a new message type, does not require modifying any existing handler or the broker itself.

```csharp
// MediatR example — each handler is independent
public record OrderPlacedEvent(int OrderId, string CustomerId, decimal Total)
    : INotification;

// Handler 1 — sends confirmation email
public class SendOrderConfirmationHandler
    : INotificationHandler<OrderPlacedEvent>
{
    public async Task Handle(
        OrderPlacedEvent notification,
        CancellationToken cancellationToken)
    {
        // Send email
    }
}

// Handler 2 — reserves inventory
public class ReserveInventoryHandler
    : INotificationHandler<OrderPlacedEvent>
{
    public async Task Handle(
        OrderPlacedEvent notification,
        CancellationToken cancellationToken)
    {
        // Reserve stock
    }
}

// Handler 3 — added six months later, no existing code modified
public class UpdateAnalyticsDashboardHandler
    : INotificationHandler<OrderPlacedEvent>
{
    public async Task Handle(
        OrderPlacedEvent notification,
        CancellationToken cancellationToken)
    {
        // Push to analytics
    }
}
```

### Plugin Architectures

Plugin systems are, as Robert C. Martin himself wrote, the ultimate expression of the OCP. The host application defines extension points (interfaces, events, hooks), and plugins implement them. The host is closed for modification. Plugins provide extension.

Think of Visual Studio extensions, browser extensions, WordPress plugins, or even NuGet packages. When you install a NuGet package that adds a new middleware to your ASP.NET Core pipeline, you are experiencing the OCP. The ASP.NET Core framework did not need to be modified to support that middleware.

## Part 13: A Checklist for Applying the OCP

When you are designing a new feature or refactoring existing code, run through this checklist:

**1. Identify the axis of change.** What is likely to change in this part of the system? New payment methods? New report formats? New validation rules? New notification channels? The answer tells you where to place your abstraction.

**2. Define the abstraction.** Create an interface (or abstract class, or delegate) that captures the varying behavior. Keep it as small as possible — the Interface Segregation Principle is your friend here.

**3. Implement the abstraction for existing cases.** Extract each case from the switch/if-else chain into its own class that implements the interface.

**4. Compose via the abstraction.** The consuming class should depend only on the interface, receive implementations via dependency injection, and dispatch to the correct one.

**5. Register in DI.** Wire up the implementations in your composition root (`Program.cs` in ASP.NET Core).

**6. Write tests.** Test each implementation in isolation. Test the consuming class with fake implementations. Verify that adding a new implementation does not break existing tests.

**7. Resist premature abstraction.** If you only have one or two cases and no clear evidence of more coming, consider waiting. The Rule of Three is your friend.

**8. Delete dead abstractions.** If an interface has had one implementation for three years and there is no realistic prospect of a second, consider inlining it. Abstractions that do not earn their keep are clutter.

## Part 14: Resources and Further Reading

Here are authoritative resources for deepening your understanding of the Open/Closed Principle and SOLID design:

- **Robert C. Martin, "The Open-Closed Principle" (1996)** — The seminal article that reformulated the OCP for the age of interfaces and polymorphism. Available in Martin's book *Agile Software Development, Principles, Patterns, and Practices* (Prentice Hall, 2003).

- **Robert C. Martin, *Clean Architecture: A Craftsman's Guide to Software Structure and Design* (2017)** — Chapter 8 covers the OCP in the context of software architecture, including the concept of protecting higher-level policies from changes in lower-level details.

- **Bertrand Meyer, *Object-Oriented Software Construction*, 2nd Edition (1997)** — The original source of the OCP. The second edition (1997) is more accessible than the first (1988), though both are dense. Available from Prentice Hall.

- **Robert C. Martin's Clean Coder Blog** — Martin's post "The Open-Closed Principle" (May 2014) discusses plugin architectures as the "apotheosis" of the OCP: [blog.cleancoder.com/uncle-bob/2014/05/12/TheOpenClosedPrinciple.html](http://blog.cleancoder.com/uncle-bob/2014/05/12/TheOpenClosedPrinciple.html)

- **Martin Fowler, *Refactoring: Improving the Design of Existing Code*, 2nd Edition (2018)** — Covers "Replace Conditional with Polymorphism" and other refactorings that move code toward OCP compliance.

- **The SOLID Wikipedia article** — A concise overview of all five principles with references: [en.wikipedia.org/wiki/SOLID](https://en.wikipedia.org/wiki/SOLID)

- **Microsoft's ASP.NET Core documentation on Middleware** — A real-world example of OCP-compliant architecture: [learn.microsoft.com/en-us/aspnet/core/fundamentals/middleware](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/middleware)

- **Microsoft's ASP.NET Core documentation on Dependency Injection** — The DI container is the mechanism that makes OCP practical in .NET: [learn.microsoft.com/en-us/aspnet/core/fundamentals/dependency-injection](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/dependency-injection)

- **Design Patterns: Elements of Reusable Object-Oriented Software (1994)** — The Gang of Four book. Strategy, Decorator, Template Method, Observer, and Factory Method patterns are all expressions of the OCP.

## Conclusion

The Open/Closed Principle is not about never modifying code. It is about designing your code so that the *most common kind of change* — adding a new variation of something that already exists — can be accomplished by writing new code rather than modifying old code.

The principle was born in 1988 when Bertrand Meyer observed that libraries were hard to evolve without breaking their clients. It was refined in 1996 when Robert C. Martin replaced inheritance with interfaces as the primary mechanism. And it is alive today in every ASP.NET Core middleware you write, every `IRepository<T>` you inject, and every strategy pattern you implement.

The key insight is not the technique. The technique — interfaces, dependency injection, polymorphism — is just mechanics. The key insight is the *question*: "If I add a new case to this system, how much existing code do I have to change?" If the answer is "none," you have followed the OCP. If the answer is "one file that I own and understand," you are probably fine. If the answer is "twelve files across three projects," you have a design problem.

Build your systems like camera bodies and lenses. The body defines the mount — the interface, the extension point. Lenses (implementations) can be swapped without rewiring the body. Some photographers never buy more than two lenses, and that is fine. But when the day comes that they need a telephoto, they do not need to buy a new camera.

Write code that does not need to be rewritten when the next requirement arrives. That is the Open/Closed Principle. And on your next Thursday afternoon, when the product owner walks over with a new carrier, a new format, or a new payment method, you will be ready.
