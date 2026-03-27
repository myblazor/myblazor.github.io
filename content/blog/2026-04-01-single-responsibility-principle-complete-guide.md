---
title: "The Single Responsibility Principle: A Complete Guide for .NET Developers"
date: 2026-04-01
author: observer-team
summary: A comprehensive deep dive into the Single Responsibility Principle — from its intellectual origins in structured analysis through Robert C. Martin's evolving definitions, with extensive C# examples showing how to recognize, refactor, and sustain SRP in real-world .NET applications.
tags:
  - solid
  - design-principles
  - csharp
  - dotnet
  - architecture
  - best-practices
  - deep-dive
---

The Single Responsibility Principle is the most frequently cited, most frequently misunderstood, and most frequently violated of the five SOLID principles. Ask ten developers what SRP means, and you will get at least three different answers: "a class should do one thing," "a class should have one reason to change," and "a class should be responsible to one actor." All three of these formulations have been used at various points in the principle's history. Only the last one captures what the principle's author, Robert C. Martin, actually intended.

This article traces SRP from its intellectual roots in the 1970s through its final formulation in 2017. Along the way, we will look at dozens of C# code examples — from obvious violations to subtle ones — and build practical intuition for applying SRP in everyday .NET development. We will also examine the tension between SRP and pragmatism, because blindly splitting every class into the smallest possible pieces creates its own problems.

## Part 1: Where the Single Responsibility Principle Came From

### Cohesion: The Idea Before the Name

Long before Robert C. Martin coined the term "Single Responsibility Principle," software engineers were grappling with the same underlying concept under a different name: **cohesion**.

In 1978, Tom DeMarco published *Structured Analysis and System Specification*, a book about decomposing systems into modules using data flow diagrams. DeMarco argued that a well-designed module should have a clear, focused purpose. When a module's internal elements were all related to the same concern, DeMarco called it "cohesive." When a module mixed unrelated concerns, it was said to have low cohesion — and low cohesion led to fragile, hard-to-change systems.

Around the same time, Meilir Page-Jones wrote *The Practical Guide to Structured Systems Design* (1980), which formalized a spectrum of cohesion types ranging from "coincidental cohesion" (the worst — elements thrown together for no reason) through "functional cohesion" (the best — every element contributes to a single, well-defined task).

Larry Constantine and Edward Yourdon had introduced these ideas even earlier in *Structured Design* (1975), identifying seven levels of cohesion. The insight was always the same: modules that group related things together are easier to understand, easier to test, and easier to change.

### Robert C. Martin and the Birth of SRP

Robert C. Martin — widely known as "Uncle Bob" — synthesized these ideas into a single, memorable principle in the late 1990s. He introduced the term "Single Responsibility Principle" in his article *The Principles of OOD* and later included it as the first of the five SOLID principles in his 2003 book *Agile Software Development, Principles, Patterns, and Practices*.

Martin's original formulation was:

> A class should have only one reason to change.

This was elegant and quotable, but it turned out to be ambiguous. What counts as a "reason to change"? Is a bug fix a reason to change? Is a refactoring a reason to change? Is a new business requirement a reason to change? Developers argued endlessly about where to draw the line.

### The 2014 Clarification

In May 2014, Martin published a blog post titled "The Single Responsibility Principle" on his Clean Coder blog. In it, he acknowledged the confusion around "reason to change" and tried to clarify. The key insight was that "reasons to change" map to **people** — specifically, to the different stakeholders or user groups whose needs drive changes to the software.

Martin used the example of an `Employee` class with three methods: `calculatePay()`, `reportHours()`, and `save()`. Each method serves a different stakeholder: the CFO's organization cares about pay calculation, the COO's organization cares about hour reporting, and the CTO's organization cares about database persistence. Three stakeholders, three reasons to change — and therefore three responsibilities that should live in separate classes or modules.

He also offered an alternative phrasing: "Gather together the things that change for the same reasons. Separate those things that change for different reasons." This is really just another way of describing cohesion and coupling — maximize cohesion within a module, minimize coupling between modules.

### The Final Definition in Clean Architecture

In his 2017 book *Clean Architecture: A Craftsman's Guide to Software Structure and Design*, Martin gave what he considers the definitive formulation of SRP:

> A module should be responsible to one, and only one, actor.

Here, "module" means a source file (or, in object-oriented languages, a class). And "actor" means a group of stakeholders or users who want the system to change in the same way. This is the most precise version of the principle because it eliminates the ambiguity of "reason to change" — it is not about the number of methods, or the number of lines of code, or even the number of conceptual "things" a class does. It is about the number of different groups of people who might ask you to change that class.

This matters because when two different actors drive changes to the same module, those changes can collide. A change requested by the accounting department might accidentally break something the operations department depends on. SRP exists to prevent that collision.

## Part 2: What SRP Is Not

Before we go further, let us clear up the most common misconceptions. These misunderstandings cause real harm — they lead developers to either ignore the principle entirely or apply it so aggressively that their codebase becomes an unnavigable sea of tiny classes.

### Misconception 1: "A Class Should Do Only One Thing"

This is the most widespread misunderstanding. It reduces SRP to a vague platitude: what counts as "one thing"? A `UserService` that creates users, validates them, and sends welcome emails — is that one thing ("user management") or three things? A `StringBuilder` that appends characters, inserts strings, and converts to output — is that one thing or many?

The "do one thing" interpretation leads to two failure modes. Developers who interpret "one thing" broadly end up with God classes that do everything related to a concept. Developers who interpret "one thing" narrowly end up with anemic classes that each contain a single method and accomplish nothing on their own.

SRP is not about the number of things a class does. It is about the number of actors it serves. A `StringBuilder` does many things, but they all serve the same actor — the developer who needs to build strings. There is no scenario where the accounting department wants `StringBuilder.Append()` to work differently than the operations department does. One actor, one responsibility, no violation.

### Misconception 2: "A Class Should Have Only One Method"

This is the extreme version of misconception one. Some developers, upon learning SRP, immediately start breaking every class into single-method classes. This is not what the principle asks for. A class can have dozens of methods and still follow SRP, as long as all those methods serve the same actor's needs.

Consider the .NET `List<T>` class. It has methods for adding, removing, sorting, searching, enumerating, copying, reversing, and converting. That is a lot of methods. But they all serve the same purpose — managing an in-memory collection — and they all change for the same reasons. Nobody from the sales department is going to ask you to change how `List<T>.Sort()` works while someone from the warehouse team asks you to change how `List<T>.Add()` works. One actor, one responsibility.

### Misconception 3: "SRP Means Small Classes"

Class size is a consequence of good design, not a goal in itself. Sometimes following SRP produces small classes. Sometimes it produces large ones. A well-designed repository class might have twenty methods — one for each query the application needs — and still follow SRP if all those queries serve the same actor.

The danger of fetishizing small classes is that it leads to **class explosion** — a codebase with hundreds of tiny classes, each containing a single method, connected by a web of interfaces and dependency injection registrations. This kind of codebase is hard to navigate, hard to understand, and hard to change — the exact problems SRP was supposed to solve.

### Misconception 4: "SRP Only Applies to Classes"

Martin's final formulation uses the word "module," which he clarifies to mean a source file. But the principle applies at every level of abstraction: methods, classes, namespaces, assemblies, services, and even entire systems. A microservice that handles both user authentication and order processing is violating SRP at the service level, just as surely as a class that mixes business logic and database access violates it at the class level.

In fact, some of the most impactful SRP violations occur at the architectural level. We will explore this in Part 10.

## Part 3: Recognizing SRP Violations in C# Code

Now let us get practical. How do you spot SRP violations in a real codebase? Here are the most reliable indicators.

### Indicator 1: The Class Has Multiple Reasons to Change

This is the classic test. Look at a class and ask: "What might cause me to change this class?" If you can identify multiple independent axes of change, you have a likely SRP violation.

```csharp
public class InvoiceService
{
    private readonly IDbConnection _db;
    private readonly IEmailSender _email;

    public InvoiceService(IDbConnection db, IEmailSender email)
    {
        _db = db;
        _email = email;
    }

    public Invoice CreateInvoice(Order order)
    {
        // Business logic: calculate line items, apply tax rules, compute totals
        var invoice = new Invoice
        {
            OrderId = order.Id,
            LineItems = order.Items.Select(i => new InvoiceLineItem
            {
                Description = i.ProductName,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                Total = i.Quantity * i.UnitPrice
            }).ToList()
        };

        invoice.Subtotal = invoice.LineItems.Sum(li => li.Total);
        invoice.Tax = invoice.Subtotal * 0.08m; // Tax rate
        invoice.Total = invoice.Subtotal + invoice.Tax;

        return invoice;
    }

    public void SaveInvoice(Invoice invoice)
    {
        // Persistence logic: insert into database
        _db.Execute(
            "INSERT INTO Invoices (OrderId, Subtotal, Tax, Total) VALUES (@OrderId, @Subtotal, @Tax, @Total)",
            invoice);

        foreach (var lineItem in invoice.LineItems)
        {
            _db.Execute(
                "INSERT INTO InvoiceLineItems (InvoiceId, Description, Quantity, UnitPrice, Total) VALUES (@InvoiceId, @Description, @Quantity, @UnitPrice, @Total)",
                new { InvoiceId = invoice.Id, lineItem.Description, lineItem.Quantity, lineItem.UnitPrice, lineItem.Total });
        }
    }

    public void SendInvoiceEmail(Invoice invoice, string recipientEmail)
    {
        // Presentation logic: format the invoice as HTML for email
        var html = $"""
            <h1>Invoice #{invoice.Id}</h1>
            <table>
                <tr><th>Item</th><th>Qty</th><th>Price</th><th>Total</th></tr>
                {string.Join("", invoice.LineItems.Select(li =>
                    $"<tr><td>{li.Description}</td><td>{li.Quantity}</td><td>{li.UnitPrice:C}</td><td>{li.Total:C}</td></tr>"))}
            </table>
            <p><strong>Subtotal:</strong> {invoice.Subtotal:C}</p>
            <p><strong>Tax:</strong> {invoice.Tax:C}</p>
            <p><strong>Total:</strong> {invoice.Total:C}</p>
            """;

        _email.Send(recipientEmail, $"Invoice #{invoice.Id}", html);
    }
}
```

This class has three independent axes of change. The accounting team might ask you to change how tax is calculated. The DBA might ask you to change the database schema. The marketing team might ask you to change how the invoice email looks. Three actors, three responsibilities, one class — a clear SRP violation.

### Indicator 2: Unrelated Dependencies in the Constructor

When a class's constructor requires a grab-bag of unrelated dependencies, that is a strong signal. The `InvoiceService` above depends on both `IDbConnection` (persistence infrastructure) and `IEmailSender` (communication infrastructure). These have nothing to do with each other.

A useful heuristic: if you can draw a line through your constructor parameters that divides them into two groups with no relationship, you probably have two responsibilities.

### Indicator 3: Methods That Do Not Use the Same Fields

In a well-designed class, most methods operate on the same internal state. When you see methods that use completely disjoint sets of fields or dependencies, those methods probably belong in separate classes.

```csharp
public class ReportGenerator
{
    private readonly IDbConnection _db;       // Used by data methods
    private readonly IPdfRenderer _renderer;   // Used by rendering methods
    private readonly IFileStorage _storage;    // Used by storage methods

    public DataTable FetchReportData(DateTime from, DateTime to)
    {
        // Uses _db only
        return _db.QueryDataTable("SELECT * FROM Sales WHERE Date BETWEEN @from AND @to",
            new { from, to });
    }

    public byte[] RenderToPdf(DataTable data, string title)
    {
        // Uses _renderer only
        return _renderer.Render(data, title);
    }

    public void SaveReport(byte[] pdf, string fileName)
    {
        // Uses _storage only
        _storage.Upload(pdf, fileName);
    }
}
```

Each method uses exactly one dependency and ignores the others. This is a sign that `ReportGenerator` is really three classes wearing a trench coat.

### Indicator 4: The God Class

Sometimes the violation is not subtle at all. You open a file and it is 3,000 lines long, with fifty methods, twenty fields, and a name like `ApplicationManager` or `Utilities` or `Helper`. This is the God Class — a class that has accumulated every responsibility nobody knew where else to put.

God classes are the ultimate SRP violation, but they are also the easiest to recognize. The harder violations are the ones that look reasonable at first glance.

### Indicator 5: Merge Conflicts in the Same File

This is a process-level indicator. If two developers working on unrelated features keep getting merge conflicts in the same file, that file probably has multiple responsibilities. Developer A is changing the tax calculation logic while Developer B is changing the email template, and they are both editing `InvoiceService.cs`. This is exactly the collision that SRP is designed to prevent.

## Part 4: Refactoring Toward SRP — A Step-by-Step Example

Let us take the `InvoiceService` from Part 3 and refactor it properly. The goal is not to create the maximum number of classes — it is to separate the responsibilities along actor boundaries.

### Step 1: Identify the Actors

Who are the stakeholders for this code?

1. **The finance team** cares about how invoices are calculated — tax rules, discounts, rounding behavior.
2. **The infrastructure team** (or DBA) cares about how invoices are stored — database schema, query performance, transactions.
3. **The communications team** (or marketing) cares about how invoices are presented — email templates, formatting, branding.

Three actors, three classes.

### Step 2: Extract the Business Logic

```csharp
public class InvoiceCalculator
{
    private readonly TaxRateProvider _taxRateProvider;

    public InvoiceCalculator(TaxRateProvider taxRateProvider)
    {
        _taxRateProvider = taxRateProvider;
    }

    public Invoice CreateInvoice(Order order)
    {
        var invoice = new Invoice
        {
            OrderId = order.Id,
            LineItems = order.Items.Select(i => new InvoiceLineItem
            {
                Description = i.ProductName,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                Total = i.Quantity * i.UnitPrice
            }).ToList()
        };

        invoice.Subtotal = invoice.LineItems.Sum(li => li.Total);
        invoice.Tax = invoice.Subtotal * _taxRateProvider.GetRate(order.ShippingAddress);
        invoice.Total = invoice.Subtotal + invoice.Tax;

        return invoice;
    }
}
```

This class has one actor: the finance team. The only reason to change it is if the business rules for calculating invoices change.

Notice that we also extracted the hard-coded tax rate into a `TaxRateProvider`. The magic number `0.08m` was a code smell — it mixed configuration with logic. Now the tax rate can vary by jurisdiction without touching the calculator.

### Step 3: Extract the Persistence Logic

```csharp
public class InvoiceRepository
{
    private readonly IDbConnection _db;

    public InvoiceRepository(IDbConnection db)
    {
        _db = db;
    }

    public void Save(Invoice invoice)
    {
        using var transaction = _db.BeginTransaction();
        try
        {
            _db.Execute(
                """
                INSERT INTO Invoices (OrderId, Subtotal, Tax, Total, CreatedAt)
                VALUES (@OrderId, @Subtotal, @Tax, @Total, @CreatedAt)
                """,
                new { invoice.OrderId, invoice.Subtotal, invoice.Tax, invoice.Total, CreatedAt = DateTime.UtcNow },
                transaction);

            var invoiceId = _db.QuerySingle<int>("SELECT SCOPE_IDENTITY()", transaction: transaction);

            foreach (var lineItem in invoice.LineItems)
            {
                _db.Execute(
                    """
                    INSERT INTO InvoiceLineItems (InvoiceId, Description, Quantity, UnitPrice, Total)
                    VALUES (@InvoiceId, @Description, @Quantity, @UnitPrice, @Total)
                    """,
                    new { InvoiceId = invoiceId, lineItem.Description, lineItem.Quantity, lineItem.UnitPrice, lineItem.Total },
                    transaction);
            }

            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public Invoice? GetById(int id)
    {
        return _db.QuerySingleOrDefault<Invoice>(
            "SELECT * FROM Invoices WHERE Id = @Id", new { Id = id });
    }
}
```

This class has one actor: the infrastructure team. The only reason to change it is if the database schema changes or if you need to optimize queries.

Notice we also added a transaction — something the original `InvoiceService` was missing. When responsibilities are separated, it becomes easier to get the details right for each one.

### Step 4: Extract the Presentation Logic

```csharp
public class InvoiceEmailSender
{
    private readonly IEmailSender _email;

    public InvoiceEmailSender(IEmailSender email)
    {
        _email = email;
    }

    public async Task SendAsync(Invoice invoice, string recipientEmail)
    {
        var html = BuildEmailHtml(invoice);
        await _email.SendAsync(recipientEmail, $"Invoice #{invoice.Id}", html);
    }

    private static string BuildEmailHtml(Invoice invoice)
    {
        var rows = string.Join("", invoice.LineItems.Select(li =>
            $"<tr><td>{li.Description}</td><td>{li.Quantity}</td><td>{li.UnitPrice:C}</td><td>{li.Total:C}</td></tr>"));

        return $"""
            <!DOCTYPE html>
            <html>
            <body style="font-family: Arial, sans-serif;">
                <h1>Invoice #{invoice.Id}</h1>
                <table border="1" cellpadding="8" cellspacing="0">
                    <thead>
                        <tr><th>Item</th><th>Qty</th><th>Price</th><th>Total</th></tr>
                    </thead>
                    <tbody>{rows}</tbody>
                </table>
                <p><strong>Subtotal:</strong> {invoice.Subtotal:C}</p>
                <p><strong>Tax:</strong> {invoice.Tax:C}</p>
                <p><strong>Total:</strong> {invoice.Total:C}</p>
            </body>
            </html>
            """;
    }
}
```

One actor: the communications/marketing team. The only reason to change this class is if the email format or branding changes.

### Step 5: Compose Them Together

Now we need something to orchestrate these three classes. This is a legitimate responsibility of its own — coordinating the workflow of creating, saving, and sending an invoice.

```csharp
public class InvoiceWorkflow
{
    private readonly InvoiceCalculator _calculator;
    private readonly InvoiceRepository _repository;
    private readonly InvoiceEmailSender _emailSender;
    private readonly ILogger<InvoiceWorkflow> _logger;

    public InvoiceWorkflow(
        InvoiceCalculator calculator,
        InvoiceRepository repository,
        InvoiceEmailSender emailSender,
        ILogger<InvoiceWorkflow> logger)
    {
        _calculator = calculator;
        _repository = repository;
        _emailSender = emailSender;
        _logger = logger;
    }

    public async Task ProcessOrderAsync(Order order, string customerEmail)
    {
        _logger.LogInformation("Creating invoice for order {OrderId}", order.Id);

        var invoice = _calculator.CreateInvoice(order);
        _repository.Save(invoice);

        _logger.LogInformation("Invoice {InvoiceId} saved for order {OrderId}", invoice.Id, order.Id);

        await _emailSender.SendAsync(invoice, customerEmail);

        _logger.LogInformation("Invoice email sent to {Email}", customerEmail);
    }
}
```

Is this class violating SRP? It depends on three other classes, after all. But look at what it *does* — it simply calls the three collaborators in sequence. It contains no business logic, no persistence logic, and no presentation logic. Its single responsibility is *orchestration*, and it serves a single actor: whoever owns the business process of invoicing. If the sequence of steps changes (maybe invoices need approval before sending), this is the only class that changes.

### The Result

We went from one class with three responsibilities to four classes, each with one:

| Class | Responsibility | Actor |
|---|---|---|
| `InvoiceCalculator` | Business rules for invoice calculation | Finance team |
| `InvoiceRepository` | Database persistence | Infrastructure/DBA |
| `InvoiceEmailSender` | Email formatting and delivery | Marketing/Communications |
| `InvoiceWorkflow` | Process orchestration | Business process owner |

Each class can change independently. The finance team can add discount logic to `InvoiceCalculator` without touching the email template. The DBA can migrate from SQL Server to PostgreSQL by changing only `InvoiceRepository`. The marketing team can redesign the email in `InvoiceEmailSender` without risking a broken tax calculation.

## Part 5: SRP at the Method Level

SRP does not only apply to classes. It applies to methods too, and this is often where the most impactful improvements can be made.

### The "And" Test

Read the name of a method. If you have to use the word "and" to describe what it does, it probably has multiple responsibilities.

```csharp
// Bad: this method validates AND saves AND notifies
public async Task ValidateAndSaveAndNotifyAsync(User user)
{
    // Validation
    if (string.IsNullOrWhiteSpace(user.Email))
        throw new ValidationException("Email is required");
    if (user.Email.Length > 255)
        throw new ValidationException("Email too long");
    if (!user.Email.Contains('@'))
        throw new ValidationException("Invalid email format");

    // Persistence
    await _db.ExecuteAsync("INSERT INTO Users (Email, Name) VALUES (@Email, @Name)", user);

    // Notification
    await _emailSender.SendAsync(user.Email, "Welcome!", "Thanks for signing up!");
}
```

Better:

```csharp
public async Task RegisterUserAsync(User user)
{
    ValidateUser(user);
    await SaveUserAsync(user);
    await SendWelcomeEmailAsync(user);
}

private static void ValidateUser(User user)
{
    if (string.IsNullOrWhiteSpace(user.Email))
        throw new ValidationException("Email is required");
    if (user.Email.Length > 255)
        throw new ValidationException("Email too long");
    if (!user.Email.Contains('@'))
        throw new ValidationException("Invalid email format");
}

private async Task SaveUserAsync(User user)
{
    await _db.ExecuteAsync("INSERT INTO Users (Email, Name) VALUES (@Email, @Name)", user);
}

private async Task SendWelcomeEmailAsync(User user)
{
    await _emailSender.SendAsync(user.Email, "Welcome!", "Thanks for signing up!");
}
```

Each private method does one thing. The public method composes them. The code reads like a story.

### The Abstraction Level Test

A method should operate at a single level of abstraction. When a method mixes high-level orchestration with low-level details, it becomes harder to understand and harder to change.

```csharp
// Bad: mixes high-level workflow with low-level string manipulation
public async Task<string> GenerateReportAsync(int year, int quarter)
{
    var data = await _repository.GetSalesDataAsync(year, quarter);

    // Suddenly we're doing low-level CSV formatting
    var sb = new StringBuilder();
    sb.AppendLine("Product,Revenue,Units,AvgPrice");
    foreach (var row in data)
    {
        sb.Append(row.Product.Replace(",", "\\,"));
        sb.Append(',');
        sb.Append(row.Revenue.ToString("F2", CultureInfo.InvariantCulture));
        sb.Append(',');
        sb.Append(row.Units);
        sb.Append(',');
        sb.AppendLine((row.Revenue / row.Units).ToString("F2", CultureInfo.InvariantCulture));
    }

    var fileName = $"sales-{year}-Q{quarter}.csv";
    await _storage.UploadAsync(fileName, Encoding.UTF8.GetBytes(sb.ToString()));

    return fileName;
}
```

Better:

```csharp
public async Task<string> GenerateReportAsync(int year, int quarter)
{
    var data = await _repository.GetSalesDataAsync(year, quarter);
    var csv = FormatAsCsv(data);
    var fileName = $"sales-{year}-Q{quarter}.csv";
    await _storage.UploadAsync(fileName, Encoding.UTF8.GetBytes(csv));
    return fileName;
}

private static string FormatAsCsv(IReadOnlyList<SalesRow> data)
{
    var sb = new StringBuilder();
    sb.AppendLine("Product,Revenue,Units,AvgPrice");
    foreach (var row in data)
    {
        sb.Append(EscapeCsvField(row.Product));
        sb.Append(',');
        sb.Append(row.Revenue.ToString("F2", CultureInfo.InvariantCulture));
        sb.Append(',');
        sb.Append(row.Units);
        sb.Append(',');
        sb.AppendLine((row.Revenue / row.Units).ToString("F2", CultureInfo.InvariantCulture));
    }
    return sb.ToString();
}

private static string EscapeCsvField(string field)
{
    if (field.Contains(',') || field.Contains('"') || field.Contains('\n'))
        return $"\"{field.Replace("\"", "\"\"")}\"";
    return field;
}
```

Now the public method reads at one level of abstraction — fetch, format, upload, return — and the details are pushed into focused helper methods.

## Part 6: SRP in ASP.NET Core — Controllers, Services, and Middleware

ASP.NET Core gives you a layered architecture out of the box: controllers (or minimal API endpoints) handle HTTP, services handle business logic, and middleware handles cross-cutting concerns. This layering naturally supports SRP — if you use it correctly.

### Fat Controllers: The Most Common ASP.NET SRP Violation

A "fat controller" is a controller that contains business logic, validation, database access, and HTTP response formatting all in one action method. This is extremely common, especially in tutorials and quick prototypes that never get cleaned up.

```csharp
// Bad: fat controller action
[HttpPost("orders")]
public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request)
{
    // Validation
    if (request.Items == null || request.Items.Count == 0)
        return BadRequest("Order must have at least one item");

    foreach (var item in request.Items)
    {
        if (item.Quantity <= 0)
            return BadRequest($"Invalid quantity for {item.ProductId}");
    }

    // Business logic: check inventory
    foreach (var item in request.Items)
    {
        var product = await _db.Products.FindAsync(item.ProductId);
        if (product == null)
            return NotFound($"Product {item.ProductId} not found");
        if (product.Stock < item.Quantity)
            return Conflict($"Insufficient stock for {product.Name}");
    }

    // More business logic: calculate total
    decimal total = 0;
    var orderItems = new List<OrderItem>();
    foreach (var item in request.Items)
    {
        var product = await _db.Products.FindAsync(item.ProductId);
        var orderItem = new OrderItem
        {
            ProductId = item.ProductId,
            Quantity = item.Quantity,
            UnitPrice = product!.Price,
            Total = item.Quantity * product.Price
        };
        orderItems.Add(orderItem);
        total += orderItem.Total;

        // Side effect: decrement stock
        product.Stock -= item.Quantity;
    }

    // Persistence
    var order = new Order
    {
        CustomerId = request.CustomerId,
        Items = orderItems,
        Total = total,
        CreatedAt = DateTime.UtcNow
    };
    _db.Orders.Add(order);
    await _db.SaveChangesAsync();

    // Notification
    await _emailSender.SendAsync(request.CustomerEmail,
        "Order Confirmation",
        $"Your order #{order.Id} for {total:C} has been placed.");

    return CreatedAtAction(nameof(GetOrder), new { id = order.Id }, order);
}
```

This single action method handles: HTTP request validation, business rule validation (inventory check), price calculation, stock management, database persistence, email notification, and HTTP response formatting. That is at least five responsibilities.

### The Refactored Version

```csharp
// Controller: only HTTP concerns
[HttpPost("orders")]
public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request)
{
    var result = await _orderService.PlaceOrderAsync(request);

    return result.Match<IActionResult>(
        success: order => CreatedAtAction(nameof(GetOrder), new { id = order.Id }, order),
        validationError: errors => BadRequest(errors),
        notFound: message => NotFound(message),
        conflict: message => Conflict(message));
}
```

```csharp
// Service: business logic orchestration
public class OrderService
{
    private readonly IOrderValidator _validator;
    private readonly IInventoryService _inventory;
    private readonly IPricingService _pricing;
    private readonly IOrderRepository _repository;
    private readonly IOrderNotifier _notifier;
    private readonly ILogger<OrderService> _logger;

    public OrderService(
        IOrderValidator validator,
        IInventoryService inventory,
        IPricingService pricing,
        IOrderRepository repository,
        IOrderNotifier notifier,
        ILogger<OrderService> logger)
    {
        _validator = validator;
        _inventory = inventory;
        _pricing = pricing;
        _repository = repository;
        _notifier = notifier;
        _logger = logger;
    }

    public async Task<OrderResult> PlaceOrderAsync(CreateOrderRequest request)
    {
        var validationResult = _validator.Validate(request);
        if (!validationResult.IsValid)
            return OrderResult.ValidationError(validationResult.Errors);

        var availabilityResult = await _inventory.CheckAvailabilityAsync(request.Items);
        if (!availabilityResult.IsAvailable)
            return OrderResult.Conflict(availabilityResult.Message);

        var pricedItems = await _pricing.CalculateAsync(request.Items);
        var order = await _repository.CreateAsync(request.CustomerId, pricedItems);
        await _inventory.ReserveStockAsync(order.Items);

        _logger.LogInformation("Order {OrderId} placed for customer {CustomerId}",
            order.Id, request.CustomerId);

        // Fire-and-forget notification (or use a message queue)
        _ = _notifier.SendConfirmationAsync(order, request.CustomerEmail);

        return OrderResult.Success(order);
    }
}
```

Now the controller knows nothing about business rules. The `OrderService` orchestrates the workflow but delegates each responsibility to a focused collaborator. The validator, inventory service, pricing service, repository, and notifier each have a single responsibility.

### Minimal APIs and SRP

With .NET minimal APIs, the temptation to put everything in a lambda is even stronger:

```csharp
// Bad: everything in a lambda
app.MapPost("/orders", async (CreateOrderRequest request, AppDbContext db, IEmailSender email) =>
{
    // 50 lines of mixed concerns...
});
```

The fix is the same — extract a service:

```csharp
app.MapPost("/orders", async (CreateOrderRequest request, OrderService service) =>
{
    var result = await service.PlaceOrderAsync(request);
    return result.Match(
        success: order => Results.Created($"/orders/{order.Id}", order),
        validationError: errors => Results.BadRequest(errors),
        notFound: message => Results.NotFound(message),
        conflict: message => Results.Conflict(message));
});
```

### Middleware and Cross-Cutting Concerns

ASP.NET Core middleware is a natural home for cross-cutting concerns that should not leak into controllers or services. Each middleware should handle exactly one concern:

```csharp
// Good: each middleware has a single responsibility
app.UseExceptionHandler("/error");   // Error handling
app.UseHttpsRedirection();           // Transport security
app.UseAuthentication();             // Identity verification
app.UseAuthorization();              // Access control
app.UseRateLimiting();               // Traffic management
app.UseResponseCaching();            // Performance optimization
```

If you find yourself writing a single middleware that handles both logging and authentication, split it in two. The middleware pipeline is designed for composition.

## Part 7: SRP and Dependency Injection

Dependency injection and SRP are natural allies. When each class has a single responsibility, its dependencies are few, focused, and easy to mock. When SRP is violated, dependencies multiply and testing becomes painful.

### The Constructor Over-Injection Smell

If a class requires more than four or five constructor dependencies, that is a strong signal of an SRP violation. The cure is not to use a service locator or property injection — it is to split the class.

```csharp
// Smells like an SRP violation
public class OrderProcessor
{
    public OrderProcessor(
        IOrderValidator validator,
        IInventoryChecker inventory,
        IPricingEngine pricing,
        IDiscountCalculator discounts,
        ITaxCalculator tax,
        IShippingCalculator shipping,
        IPaymentGateway payment,
        IOrderRepository repository,
        IEmailSender email,
        ISmsNotifier sms,
        IAuditLogger audit,
        IAnalyticsTracker analytics)
    {
        // 12 dependencies = multiple responsibilities
    }
}
```

Twelve dependencies means this class is doing too much. Some natural groupings emerge: pricing (pricing + discounts + tax + shipping), payment processing, persistence, and notification (email + SMS). Each group should be its own class.

### DI Registration as Documentation

Your `Program.cs` (or wherever you register services) is a map of your application's responsibilities. When it is well-organized, you can read it and understand the architecture:

```csharp
// Each section registers classes for one responsibility area
// --- Business Logic ---
builder.Services.AddScoped<InvoiceCalculator>();
builder.Services.AddScoped<TaxRateProvider>();
builder.Services.AddScoped<DiscountEngine>();

// --- Persistence ---
builder.Services.AddScoped<IInvoiceRepository, SqlInvoiceRepository>();
builder.Services.AddScoped<IOrderRepository, SqlOrderRepository>();

// --- Notifications ---
builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();
builder.Services.AddScoped<InvoiceEmailSender>();

// --- Orchestration ---
builder.Services.AddScoped<InvoiceWorkflow>();
builder.Services.AddScoped<OrderService>();
```

If you cannot organize your registrations into coherent groups, your classes probably do not have coherent responsibilities.

## Part 8: SRP and Testing

Perhaps the most practical argument for SRP is that it makes testing dramatically easier. When a class has one responsibility, it has one reason to test. Its test setup is simple, its assertions are focused, and its test suite is easy to maintain.

### Testing a Class with Multiple Responsibilities

Consider testing the original `InvoiceService` from Part 3. To test the `CreateInvoice` method (business logic), you need to set up an `IDbConnection` and an `IEmailSender` — even though the method does not use them. This is a sign that the class has dependencies it should not have.

```csharp
// Painful: unnecessary mocking
[Fact]
public void CreateInvoice_CalculatesCorrectTotal()
{
    // We have to create these even though CreateInvoice doesn't use them
    var mockDb = new Mock<IDbConnection>();
    var mockEmail = new Mock<IEmailSender>();

    var service = new InvoiceService(mockDb.Object, mockEmail.Object);

    var order = new Order
    {
        Id = 1,
        Items =
        [
            new OrderItem { ProductName = "Widget", Quantity = 3, UnitPrice = 10.00m }
        ]
    };

    var invoice = service.CreateInvoice(order);

    Assert.Equal(30.00m, invoice.Subtotal);
    Assert.Equal(2.40m, invoice.Tax);
    Assert.Equal(32.40m, invoice.Total);
}
```

### Testing After Refactoring

After splitting into `InvoiceCalculator`, the test is clean:

```csharp
[Fact]
public void CreateInvoice_CalculatesCorrectTotal()
{
    var taxProvider = new FakeTaxRateProvider(rate: 0.08m);
    var calculator = new InvoiceCalculator(taxProvider);

    var order = new Order
    {
        Id = 1,
        Items =
        [
            new OrderItem { ProductName = "Widget", Quantity = 3, UnitPrice = 10.00m }
        ]
    };

    var invoice = calculator.CreateInvoice(order);

    Assert.Equal(30.00m, invoice.Subtotal);
    Assert.Equal(2.40m, invoice.Tax);
    Assert.Equal(32.40m, invoice.Total);
}
```

No mock database. No mock email sender. Just the class under test and its actual dependency. The test is shorter, more readable, and more resilient to changes in unrelated parts of the system.

### Testing the Repository in Isolation

```csharp
[Fact]
public async Task Save_InsertsInvoiceAndLineItems()
{
    using var connection = new SqliteConnection("Data Source=:memory:");
    await connection.OpenAsync();
    await CreateTablesAsync(connection);

    var repository = new InvoiceRepository(connection);
    var invoice = new Invoice
    {
        OrderId = 42,
        Subtotal = 100m,
        Tax = 8m,
        Total = 108m,
        LineItems =
        [
            new InvoiceLineItem
            {
                Description = "Widget",
                Quantity = 10,
                UnitPrice = 10m,
                Total = 100m
            }
        ]
    };

    repository.Save(invoice);

    var saved = await connection.QuerySingleAsync<int>("SELECT COUNT(*) FROM Invoices");
    Assert.Equal(1, saved);

    var lineItems = await connection.QuerySingleAsync<int>("SELECT COUNT(*) FROM InvoiceLineItems");
    Assert.Equal(1, lineItems);
}
```

This test exercises only persistence logic. It does not need to worry about tax rates or email templates. If the test fails, you know the problem is in the persistence code.

### Testing the Email Sender in Isolation

```csharp
[Fact]
public async Task SendAsync_FormatsInvoiceAsHtml()
{
    var mockEmail = new Mock<IEmailSender>();
    string? capturedBody = null;
    mockEmail
        .Setup(e => e.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
        .Callback<string, string, string>((to, subject, body) => capturedBody = body)
        .Returns(Task.CompletedTask);

    var sender = new InvoiceEmailSender(mockEmail.Object);
    var invoice = new Invoice
    {
        Id = 99,
        Subtotal = 50m,
        Tax = 4m,
        Total = 54m,
        LineItems = [new InvoiceLineItem { Description = "Gadget", Quantity = 5, UnitPrice = 10m, Total = 50m }]
    };

    await sender.SendAsync(invoice, "customer@example.com");

    Assert.NotNull(capturedBody);
    Assert.Contains("Invoice #99", capturedBody);
    Assert.Contains("Gadget", capturedBody);
}
```

Clean, focused, fast.

### The Testing Pyramid and SRP

SRP aligns naturally with the testing pyramid. When responsibilities are separated:

- **Unit tests** cover individual classes (business logic, formatting, validation) with zero infrastructure dependencies. These are fast and numerous.
- **Integration tests** cover collaborations between classes (repository + real database, email sender + SMTP stub). These are slower but fewer.
- **End-to-end tests** cover complete workflows (place an order, verify the email). These are slowest and fewest.

Without SRP, every test becomes an integration test because you cannot isolate any single concern. The testing pyramid collapses into a testing rectangle — slow, expensive, and brittle.

## Part 9: SRP in Real-World .NET Patterns

Let us examine how SRP manifests in several patterns you encounter daily in .NET development.

### The Repository Pattern

The repository pattern is a direct application of SRP: separate data access from business logic. A repository is responsible to one actor — whoever manages the data store.

```csharp
public interface IProductRepository
{
    Task<Product?> GetByIdAsync(int id);
    Task<IReadOnlyList<Product>> GetByCategoryAsync(string category);
    Task<IReadOnlyList<Product>> SearchAsync(string query, int skip, int take);
    Task AddAsync(Product product);
    Task UpdateAsync(Product product);
    Task DeleteAsync(int id);
}
```

All methods in this interface relate to the same concern: storing and retrieving products. The interface does not include methods for calculating prices, generating reports, or sending notifications. Those belong elsewhere.

A common SRP violation in repositories is adding query methods that serve different actors:

```csharp
// Bad: the repository is serving too many actors
public interface IProductRepository
{
    // Used by the catalog service (customer-facing)
    Task<IReadOnlyList<Product>> GetActiveByCategoryAsync(string category);

    // Used by the admin dashboard (internal)
    Task<IReadOnlyList<Product>> GetAllIncludingDeletedAsync();

    // Used by the analytics service (reporting)
    Task<ProductSalesReport> GetSalesReportAsync(DateTime from, DateTime to);

    // Used by the inventory service (operations)
    Task<IReadOnlyList<Product>> GetLowStockAsync(int threshold);
}
```

The `GetSalesReportAsync` method does not belong here — it serves the analytics/reporting actor, not the data access actor. It should live in a separate `IProductReportingRepository` or a dedicated reporting service.

### The MediatR / CQRS Pattern

The MediatR library and the Command Query Responsibility Segregation (CQRS) pattern are built on SRP. Each command handler has exactly one responsibility: handling one specific command.

```csharp
public record CreateOrderCommand(int CustomerId, List<OrderItemDto> Items) : IRequest<OrderResult>;

public class CreateOrderHandler : IRequestHandler<CreateOrderCommand, OrderResult>
{
    private readonly IOrderRepository _repository;
    private readonly IPricingService _pricing;
    private readonly ILogger<CreateOrderHandler> _logger;

    public CreateOrderHandler(
        IOrderRepository repository,
        IPricingService pricing,
        ILogger<CreateOrderHandler> logger)
    {
        _repository = repository;
        _pricing = pricing;
        _logger = logger;
    }

    public async Task<OrderResult> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        var pricedItems = await _pricing.CalculateAsync(request.Items);
        var order = await _repository.CreateAsync(request.CustomerId, pricedItems);

        _logger.LogInformation("Order {OrderId} created", order.Id);

        return OrderResult.Success(order);
    }
}
```

Each handler is a small, focused class with a single responsibility. You can test it in isolation, reason about it in isolation, and change it without affecting other handlers.

CQRS takes this further by separating the read side (queries) from the write side (commands). The read model can be optimized for fast queries while the write model is optimized for business rule enforcement — two different actors with two different needs.

### The Options Pattern

ASP.NET Core's Options pattern (`IOptions<T>`) is an SRP-friendly way to manage configuration. Instead of one giant configuration object, you create focused configuration classes:

```csharp
public class SmtpSettings
{
    public string Host { get; set; } = "";
    public int Port { get; set; } = 587;
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
    public bool UseSsl { get; set; } = true;
}

public class InvoiceSettings
{
    public decimal DefaultTaxRate { get; set; } = 0.08m;
    public int PaymentTermDays { get; set; } = 30;
    public string CompanyName { get; set; } = "";
}
```

Each settings class is responsible to one actor. The IT team manages SMTP settings. The finance team manages invoice settings. Changes to email configuration never accidentally affect invoice configuration.

### The Specification Pattern

The Specification pattern separates query criteria from query execution:

```csharp
public class ActiveProductsInCategorySpec : Specification<Product>
{
    public ActiveProductsInCategorySpec(string category)
    {
        Where(p => p.IsActive && p.Category == category);
        OrderBy(p => p.Name);
        Take(50);
    }
}

public class LowStockProductsSpec : Specification<Product>
{
    public LowStockProductsSpec(int threshold)
    {
        Where(p => p.Stock < threshold);
        OrderByDescending(p => p.Stock);
    }
}
```

Each specification has a single responsibility: defining one set of query criteria. The repository handles execution. This keeps the repository from becoming a dumping ground for query methods.

## Part 10: SRP at the Architectural Level

SRP applies beyond individual classes. At the architectural level, it guides how you structure assemblies, projects, and services.

### Project Structure

A common .NET project structure reflects SRP at the assembly level:

```
src/
  MyApp.Domain/           # Business entities, value objects, domain events
  MyApp.Application/      # Use cases, commands, queries, interfaces
  MyApp.Infrastructure/   # Database access, file system, external APIs
  MyApp.Web/              # HTTP endpoints, view models, middleware
```

Each project has one responsibility. `Domain` knows nothing about databases. `Infrastructure` knows nothing about HTTP. `Web` knows nothing about SQL. Changes to the database schema affect only `Infrastructure`. Changes to the API contract affect only `Web`.

### Microservices and SRP

Each microservice should have a single responsibility — serving one bounded context. A `UserService` that handles authentication, profile management, and recommendation engines is violating SRP at the service level.

The cost of splitting too aggressively at the microservice level is high — distributed systems are complex. But the cost of a monolithic service that multiple teams need to deploy independently is higher. SRP helps you find the right boundaries.

### The Vertical Slice Architecture

Vertical slice architecture, popularized by Jimmy Bogard, organizes code by feature rather than by layer. Each "slice" contains everything needed for one use case: the endpoint, the handler, the validator, and even the data access.

```
Features/
  CreateOrder/
    CreateOrderEndpoint.cs
    CreateOrderHandler.cs
    CreateOrderValidator.cs
    CreateOrderRequest.cs
  GetOrderById/
    GetOrderByIdEndpoint.cs
    GetOrderByIdHandler.cs
    GetOrderByIdResponse.cs
```

This is SRP applied at the feature level. Each folder is responsible to one use case — one actor's need. Changes to order creation never touch order retrieval. It is a different organizational principle than the traditional layered architecture, but it serves the same SRP goal: isolating the things that change for different reasons.

## Part 11: When SRP Goes Wrong — Over-Engineering and Class Explosion

Every principle, taken to its extreme, becomes a vice. SRP is no exception.

### The One-Method-Per-Class Trap

Some developers, upon learning SRP, start creating classes like:

```csharp
public class UserEmailValidator
{
    public bool Validate(string email) => email.Contains('@');
}

public class UserNameValidator
{
    public bool Validate(string name) => !string.IsNullOrWhiteSpace(name);
}

public class UserAgeValidator
{
    public bool Validate(int age) => age >= 18;
}

public class UserPasswordValidator
{
    public bool Validate(string password) => password.Length >= 8;
}
```

Four classes for what should be one `UserValidator` class. All four serve the same actor (whoever defines the user validation rules), and all four change for the same reason (when validation rules change). Splitting them is not SRP — it is fragmentation.

The correct application of SRP groups them together:

```csharp
public class UserValidator
{
    public ValidationResult Validate(User user)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(user.Name))
            errors.Add("Name is required");

        if (!user.Email.Contains('@'))
            errors.Add("Invalid email format");

        if (user.Age < 18)
            errors.Add("Must be at least 18 years old");

        if (user.Password.Length < 8)
            errors.Add("Password must be at least 8 characters");

        return new ValidationResult(errors);
    }
}
```

One class, one responsibility: validating users. The fact that it checks multiple fields does not make it multi-responsible.

### The Interface Explosion Problem

Over-zealous SRP can also lead to an explosion of interfaces:

```csharp
public interface IUserCreator { Task CreateAsync(User user); }
public interface IUserUpdater { Task UpdateAsync(User user); }
public interface IUserDeleter { Task DeleteAsync(int id); }
public interface IUserFinder { Task<User?> FindAsync(int id); }
public interface IUserSearcher { Task<List<User>> SearchAsync(string query); }
```

Five interfaces for what should be one `IUserRepository`. Again, all five serve the same actor and change for the same reason. The Interface Segregation Principle (ISP) says clients should not depend on methods they do not use — but that does not mean every method gets its own interface. It means you split along client boundaries, not along method boundaries.

### Finding the Right Granularity

The right level of granularity depends on your actual actors. Ask these questions:

1. **Who will ask me to change this class?** If the answer is one person or one team, it is probably fine.
2. **When I change one method, do I risk breaking the others?** If the methods are independent and non-interacting, they might belong in separate classes. If they share state and logic, they probably belong together.
3. **Can I test this class without complex setup?** If you need ten mocks in your test constructor, the class is doing too much. If you need zero dependencies, you might have split too aggressively and lost the ability to verify meaningful behavior.
4. **Would a new team member understand this class in five minutes?** If the class is 30 lines and does one obvious thing, great. If it is 30 lines spread across five files in three folders, you have traded one kind of complexity for another.

## Part 12: SRP and Related Principles

SRP does not exist in isolation. It interacts with the other SOLID principles and with broader design principles.

### SRP and the Open/Closed Principle (OCP)

OCP says that software entities should be open for extension but closed for modification. SRP makes OCP easier to achieve. When a class has a single responsibility, you can extend its behavior by creating a new class rather than modifying the existing one.

For example, if `InvoiceCalculator` only handles standard tax calculation, you can create a `DiscountedInvoiceCalculator` that extends it (via inheritance or composition) rather than adding discount logic to the existing class. SRP keeps each class focused enough that extension points are clear.

### SRP and the Liskov Substitution Principle (LSP)

LSP says that subtypes must be substitutable for their base types. SRP violations often lead to LSP violations. When a base class has multiple responsibilities, subtypes may need to override some behavior while leaving others unchanged — and the overrides can break expectations.

Consider a base class `Notification` with methods `Send()` and `Log()`. An `SmsNotification` subclass might override `Send()` but need a completely different `Log()` implementation because SMS logging has different requirements. The two responsibilities (sending and logging) should have been separate from the start.

### SRP and the Interface Segregation Principle (ISP)

ISP is SRP applied to interfaces. A "fat" interface that serves multiple actors should be split into smaller, focused interfaces — each serving one actor.

```csharp
// Fat interface serving multiple actors
public interface IUserService
{
    Task<User> GetByIdAsync(int id);        // Read by many
    Task CreateAsync(User user);             // Write by admin
    Task DeactivateAsync(int id);            // Write by compliance
    Task<UserReport> GenerateReportAsync();   // Read by analytics
}

// Split by actor
public interface IUserReader
{
    Task<User> GetByIdAsync(int id);
}

public interface IUserAdmin
{
    Task CreateAsync(User user);
    Task DeactivateAsync(int id);
}

public interface IUserReporting
{
    Task<UserReport> GenerateReportAsync();
}
```

### SRP and the Dependency Inversion Principle (DIP)

DIP says that high-level modules should not depend on low-level modules — both should depend on abstractions. SRP makes this practical. When each class has a single responsibility, the abstractions (interfaces) it exposes are small and focused. A `IInvoiceCalculator` interface with two methods is easy to mock and easy to implement. A `IInvoiceService` interface with fifteen methods spanning three responsibilities is a pain point.

### SRP and Separation of Concerns

Separation of Concerns is the broader principle from which SRP derives. While SRP focuses on the class level and defines "concern" as "an actor's needs," Separation of Concerns applies at every level — from the lines within a method to the services in a distributed system.

The MVC pattern is Separation of Concerns at the UI level: Model (data), View (presentation), Controller (user input). The layered architecture is Separation of Concerns at the application level: presentation, business logic, data access. SRP provides a specific, testable criterion for evaluating whether concerns are adequately separated.

## Part 13: Applying SRP in Blazor WebAssembly

Since Observer Magazine is built on Blazor WebAssembly, let us look at how SRP applies specifically to Blazor components and services.

### Components Should Not Contain Business Logic

A Blazor component's responsibility is rendering UI and handling user interactions. Business logic — calculations, validations, data transformations — belongs in services.

```csharp
// Bad: business logic in the component
@code {
    private List<CartItem> _items = new();

    private decimal CalculateTotal()
    {
        var subtotal = _items.Sum(i => i.Price * i.Quantity);
        var discount = subtotal > 100 ? subtotal * 0.10m : 0;
        var tax = (subtotal - discount) * 0.08m;
        return subtotal - discount + tax;
    }

    private bool CanCheckout()
    {
        return _items.Count > 0
            && _items.All(i => i.Quantity > 0)
            && _items.Sum(i => i.Price * i.Quantity) >= 5.00m;
    }
}
```

```csharp
// Good: component delegates to a service
@inject ICartService CartService

@code {
    private List<CartItem> _items = new();
    private decimal _total;
    private bool _canCheckout;

    private async Task RefreshAsync()
    {
        _total = CartService.CalculateTotal(_items);
        _canCheckout = CartService.CanCheckout(_items);
    }
}
```

The component renders and delegates. The service calculates and validates. Each can be tested independently.

### Separate Data Fetching from Data Presentation

A common pattern in Blazor is to fetch data in `OnInitializedAsync` and render it in the markup. When the fetch logic becomes complex (caching, error handling, retry logic), extract it into a service.

```csharp
// The component focuses on UI state management
@inject IBlogService BlogService

@if (_loading)
{
    <p>Loading...</p>
}
else if (_error is not null)
{
    <p class="error">@_error</p>
}
else
{
    @foreach (var post in _posts)
    {
        <BlogCard Post="post" />
    }
}

@code {
    private BlogPostMetadata[] _posts = [];
    private bool _loading = true;
    private string? _error;

    protected override async Task OnInitializedAsync()
    {
        try
        {
            _posts = await BlogService.GetPostsAsync();
        }
        catch (Exception ex)
        {
            _error = "Failed to load blog posts. Please try again later.";
        }
        finally
        {
            _loading = false;
        }
    }
}
```

The component handles UI states (loading, error, success). The `BlogService` handles HTTP calls, caching, and deserialization. The component does not know or care where the data comes from.

### CSS Isolation and SRP

Blazor's component-scoped CSS (`.razor.css` files) is an application of SRP to styles. Each component owns its own styles. Changes to the `BlogCard` component's appearance do not affect `ProductCard`. This eliminates the "CSS blast radius" problem where a global style change breaks unrelated pages.

```css
/* BlogCard.razor.css — only affects BlogCard */
.blog-card {
    border: 1px solid var(--border-color);
    padding: 1rem;
    border-radius: 8px;
    margin-bottom: 1rem;
}

.blog-card h3 {
    margin-top: 0;
}
```

This is exactly the same principle as SRP for classes — scope the concern so that changes in one area do not ripple into others.

## Part 14: A Checklist for Evaluating SRP

Here is a practical checklist you can apply to any class, module, or service in your codebase. Not every "yes" answer means you have a violation — these are signals, not rules. But if you answer "yes" to three or more, it is worth investigating.

**Actor Analysis:**
- Can you identify more than one stakeholder or team who might request changes to this class?
- Have you received change requests from different sources that both touched this class?
- Does this class appear in merge conflicts between developers working on unrelated features?

**Dependency Analysis:**
- Does the constructor take more than four or five dependencies?
- Are any dependencies completely unused by some methods?
- Can you group the dependencies into two or more unrelated clusters?

**Method Analysis:**
- Do some methods operate on a completely different subset of fields than others?
- Would you need the word "and" to describe what this class does?
- Does the class mix different levels of abstraction (e.g., business logic and SQL strings)?

**Testing Analysis:**
- Do you need complex test setup that includes mock objects the test never actually exercises?
- Is it hard to name your test class because the class under test does not have a clear, single purpose?
- Do tests for one concern break when you change code related to a different concern?

**Naming Analysis:**
- Does the class name include words like "Manager," "Processor," "Handler," "Service," or "Utility" without further qualification? (These are often catch-all names for multi-responsibility classes.)
- Would adding a more specific suffix improve clarity? For example, `OrderProcessor` could be split into `OrderValidator`, `OrderPricer`, and `OrderPersister`.

## Part 15: SRP in Practice — A Decision Framework

Theory is important, but daily development requires practical decisions. Here is a framework for deciding when and how to apply SRP.

### When to Split

Split a class when:

1. **Different actors need different changes.** This is the textbook case. If the finance team wants to change how discounts work and the marketing team wants to change how promotions display, and both changes touch the same class, split it.

2. **Testing is painful.** If you need ten mocks to test one method, the class is doing too much. Split it so each piece can be tested with minimal setup.

3. **The class is growing without bound.** If a class keeps accumulating methods every sprint, it is probably a dumping ground. New methods should make you ask: "Does this belong here, or does it need a new home?"

4. **Merge conflicts are frequent.** If two developers keep stepping on each other in the same file, the file has too many responsibilities.

### When NOT to Split

Do not split when:

1. **All methods serve the same actor.** A class with ten methods that all serve the same actor's needs is not violating SRP, even if it feels large.

2. **Splitting would scatter related logic.** If understanding one concern requires jumping between five files in three folders, you have gone too far. Cohesion matters.

3. **The "violation" is purely theoretical.** If a class technically serves two actors but one of them has not changed in three years and is unlikely to ever change, the violation is harmless. Refactor when the pain is real, not when the principle is theoretically violated.

4. **You are writing a prototype or spike.** SRP matters most in code that will be maintained. If you are writing a throwaway prototype to test an idea, do not spend hours on perfect separation. Just make it work. If the prototype succeeds and becomes production code, then refactor.

### The Refactoring Trigger

The best time to apply SRP is not during initial development — it is when you feel the pain of a violation. The second time you need to change a class for an unrelated reason, that is your signal. The first time might be coincidence. The second time is a pattern. Refactor on the second occurrence.

This aligns with the "Rule of Three" from Martin Fowler: the first time you do something, just do it. The second time, wince. The third time, refactor.

## Part 16: Common SRP Violations in the Wild

Let us catalog the SRP violations you are most likely to encounter in real .NET codebases.

### The God Controller

We covered this in Part 6, but it bears repeating because it is everywhere. A controller that validates input, applies business rules, accesses the database, and formats the response is the most common SRP violation in ASP.NET applications.

### The Entity with Behavior

Domain-driven design (DDD) encourages putting behavior on entities. But there is a line between "behavior that belongs to this concept" and "behavior that belongs to a different actor."

```csharp
// The entity has crossed the line
public class Order
{
    public int Id { get; set; }
    public List<OrderItem> Items { get; set; } = new();
    public decimal Total => Items.Sum(i => i.Total);

    // Fine: domain behavior
    public void AddItem(Product product, int quantity)
    {
        Items.Add(new OrderItem(product, quantity));
    }

    // Questionable: persistence concern
    public void SaveToDatabase(IDbConnection db)
    {
        db.Execute("INSERT INTO Orders ...", this);
    }

    // Violation: presentation concern
    public string ToEmailHtml()
    {
        return $"<h1>Order #{Id}</h1>...";
    }

    // Violation: external API concern
    public async Task SyncToErpAsync(IErpClient client)
    {
        await client.PostOrderAsync(this);
    }
}
```

The `AddItem` method is legitimate domain behavior — it enforces business rules about what can be added to an order. But `SaveToDatabase`, `ToEmailHtml`, and `SyncToErpAsync` serve completely different actors and belong in separate classes.

### The Utility Class

```csharp
public static class Helpers
{
    public static string FormatCurrency(decimal amount) { ... }
    public static bool IsValidEmail(string email) { ... }
    public static byte[] CompressGzip(byte[] data) { ... }
    public static DateTime ParseFlexibleDate(string input) { ... }
    public static string Slugify(string title) { ... }
    public static int LevenshteinDistance(string a, string b) { ... }
}
```

This class is a textbook example of **coincidental cohesion** — the lowest form. These methods have nothing in common except that someone did not know where else to put them. They should be in separate, well-named static classes: `CurrencyFormatter`, `EmailValidator`, `CompressionHelper`, `DateParser`, `SlugGenerator`, `StringDistance`.

### The Configuration Dumping Ground

```csharp
public class AppSettings
{
    public string DatabaseConnectionString { get; set; } = "";
    public string SmtpHost { get; set; } = "";
    public int SmtpPort { get; set; } = 587;
    public string JwtSecret { get; set; } = "";
    public int JwtExpirationMinutes { get; set; } = 60;
    public string StorageBucket { get; set; } = "";
    public decimal DefaultTaxRate { get; set; } = 0.08m;
    public int MaxLoginAttempts { get; set; } = 5;
    public string SupportEmail { get; set; } = "";
}
```

Every class in the system depends on `AppSettings`, but each class only uses one or two properties. Use the Options pattern to split this into focused configuration classes. We covered this in Part 9.

## Part 17: SRP Across the Software Development Lifecycle

SRP is not just a coding principle. It applies to processes, teams, and tooling.

### SRP in Source Control

Each commit should have a single responsibility — one logical change. A commit that "adds discount feature, fixes email bug, and updates NuGet packages" is the source control equivalent of a God class. It is harder to review, harder to revert, and harder to bisect.

```bash
# Bad: one commit doing three things
git commit -m "Add discount feature, fix email bug, update packages"

# Good: three focused commits
git commit -m "feat: add percentage-based discount calculation"
git commit -m "fix: correct email template encoding for special characters"
git commit -m "chore: update NuGet packages to latest stable versions"
```

### SRP in CI/CD Pipelines

Each stage in your pipeline should have a single responsibility:

```yaml
jobs:
  build:        # Compile the code
  test:         # Run the tests
  analyze:      # Run static analysis
  package:      # Create deployment artifacts
  deploy-staging: # Deploy to staging
  deploy-prod:  # Deploy to production
```

Mixing build and test in a single stage makes failures harder to diagnose. Mixing deploy with test makes rollbacks harder to orchestrate.

### SRP in Documentation

Each documentation file should cover one topic. A single README that explains installation, architecture, API reference, deployment, and troubleshooting is a God document. Split it:

```
docs/
  getting-started.md
  architecture.md
  api-reference.md
  deployment.md
  troubleshooting.md
```

### SRP in Team Organization

Conway's Law says that organizations design systems that mirror their communication structures. If one team owns both the billing system and the notification system, those systems will tend to be coupled. SRP at the team level means giving each team ownership of one area of the business — and the code boundaries should follow.

## Part 18: Summary and Key Takeaways

The Single Responsibility Principle, correctly understood, is not about class size, method count, or even the number of "things" a class does. It is about the number of actors — the groups of stakeholders whose needs drive changes to your code.

Here are the key takeaways:

**The definition:** A module should be responsible to one, and only one, actor.

**The purpose:** To prevent changes requested by one actor from accidentally breaking functionality used by another actor.

**The mechanism:** Group together the things that change for the same reasons. Separate the things that change for different reasons.

**The balance:** SRP is a guideline, not a law. Applying it dogmatically leads to class explosion and unnecessary complexity. Ignoring it leads to fragile, untestable, conflict-prone code. The sweet spot is somewhere in between, guided by real pain points rather than theoretical purity.

**The practice:** You do not need to get SRP right on the first pass. Write the code, feel the pain, then refactor. The second time you change a class for an unrelated reason is your signal to split.

**The test:** If you can test a class with simple setup and focused assertions, SRP is probably in good shape. If testing requires a Christmas tree of mock objects, something needs splitting.

## Resources

- Martin, Robert C. *Agile Software Development, Principles, Patterns, and Practices.* Pearson, 2003. The book where SRP was first formalized as part of the SOLID principles.
- Martin, Robert C. "The Single Responsibility Principle." [blog.cleancoder.com/uncle-bob/2014/05/08/SingleReponsibilityPrinciple.html](https://blog.cleancoder.com/uncle-bob/2014/05/08/SingleReponsibilityPrinciple.html). The 2014 blog post clarifying the "reason to change" definition.
- Martin, Robert C. *Clean Architecture: A Craftsman's Guide to Software Structure and Design.* Pearson, 2017. Contains the final formulation of SRP with the "actor" definition.
- DeMarco, Tom. *Structured Analysis and System Specification.* Yourdon Press, 1978. The origin of the cohesion concept that SRP builds upon.
- Page-Jones, Meilir. *The Practical Guide to Structured Systems Design.* Yourdon Press, 1980. Formalizes the spectrum of cohesion types.
- Fowler, Martin. *Refactoring: Improving the Design of Existing Code.* 2nd ed. Addison-Wesley, 2018. Practical techniques for refactoring toward better responsibility separation. [refactoring.com](https://refactoring.com/)
- Microsoft. "Dependency injection in ASP.NET Core." [learn.microsoft.com/aspnet/core/fundamentals/dependency-injection](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/dependency-injection). Official documentation on DI, which works hand-in-hand with SRP.
- Microsoft. "ASP.NET Core Blazor component-scoped CSS." [learn.microsoft.com/aspnet/core/blazor/components/css-isolation](https://learn.microsoft.com/en-us/aspnet/core/blazor/components/css-isolation). CSS isolation as SRP applied to component styles.
- Bogard, Jimmy. "Vertical Slice Architecture." [jimmybogard.com/vertical-slice-architecture](https://www.jimmybogard.com/vertical-slice-architecture/). An alternative to layered architecture that applies SRP at the feature level.
- DigitalOcean. "SOLID: The First Five Principles of Object-Oriented Design." [digitalocean.com/community/conceptual-articles/s-o-l-i-d-the-first-five-principles-of-object-oriented-design](https://www.digitalocean.com/community/conceptual-articles/s-o-l-i-d-the-first-five-principles-of-object-oriented-design). A thorough walkthrough of all five SOLID principles with code examples.

Now let me verify the key historical facts about the Liskov Substitution Principle before writing.I have all the historical facts verified. Now let me write the comprehensive article.Here's your complete blog article. Save it as:

**`content/blog/2026-03-27-liskov-substitution-principle.md`**

Then commit and push — GitHub Actions will handle the rest.

The article covers 16 parts spanning roughly 7,000+ words, including:

- Barbara Liskov's full history (Stanford PhD 1968, CLU, OOPSLA 1987 keynote, 1994 paper with Wing, 2008 Turing Award)
- The principle in plain language with a vending machine analogy
- All five formal rules: precondition contravariance, postcondition covariance, invariant preservation, the history constraint, and exception compatibility — each with C# code examples
- Five classic violations: Rectangle/Square, read-only collections inheriting List\<T\>, NotImplementedException, ignored parameters, temporal coupling
- LSP in the .NET framework itself (Stream, ICollection\<T\> vs IReadOnlyCollection\<T\>, array covariance)
- Design patterns that help (Strategy, Template Method, Decorator) and patterns that risk violations (Adapter, Null Object)
- LSP + dependency injection in ASP.NET Core with contract test patterns
- Generics variance (covariance, contravariance, invariance)
- Detection techniques (grep for NotImplementedException, type checks, contract tests, Roslyn analyzers)
- Interaction with the other SOLID principles
- A practical checklist
- Resources and further reading

