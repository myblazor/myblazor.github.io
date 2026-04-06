---
title: "SOLID Principles: A Complete Guide to Writing Clean, Maintainable Object-Oriented Code"
date: 2026-03-31
author: myblazor-team
summary: An exhaustive deep dive into all five SOLID principles — Single Responsibility, Open/Closed, Liskov Substitution, Interface Segregation, and Dependency Inversion — with C# examples, historical context, real-world scenarios, common violations, and practical guidance for .NET developers.
tags:
  - csharp
  - dotnet
  - solid
  - design-principles
  - object-oriented-programming
  - clean-code
  - software-architecture
  - best-practices
  - deep-dive
---

If you have been writing software for any meaningful length of time, you have almost certainly felt the slow creep of rot. A codebase that was once small and elegant becomes tangled and fragile. A class that started with thirty lines now has three hundred. A change in one corner of the system triggers failures in another. You deploy on a Friday afternoon and your phone buzzes all weekend.

The SOLID principles are a set of five design guidelines that exist precisely to fight that decay. They are not a silver bullet, nor are they a rigid checklist that you must follow dogmatically in every file you write. They are, however, among the most battle-tested heuristics in object-oriented programming for keeping code maintainable, testable, and extensible over the life of a project.

In this article, we will work through all five principles in full detail: where they came from, what they mean in precise terms, how to apply them in C# and .NET, how to spot violations, and what tradeoffs to keep in mind. Every principle gets real, compilable code examples — not toy pseudocode, but scenarios you might encounter in a production system.

## Part 1: History and Context — Where SOLID Came From

### The Origins

The five principles that compose the SOLID acronym were not all invented by the same person at the same time. They emerged over roughly a decade of thought by several computer scientists and were unified under a single banner by Robert C. Martin — universally known as "Uncle Bob."

Robert C. Martin first collected and articulated these principles in his 2000 paper *Design Principles and Design Patterns*, where he described the symptoms of rotting software (rigidity, fragility, immobility, viscosity) and proposed a set of principles to combat them. The actual acronym "SOLID" was coined around 2004 by Michael Feathers, who rearranged the initial letters of the five principles into a memorable word.

But the individual principles have deeper roots:

- **Single Responsibility Principle (SRP)**: Articulated by Robert C. Martin, drawing on ideas about cohesion that go back to Tom DeMarco and Meilir Page-Jones in the 1970s and 1980s.
- **Open/Closed Principle (OCP)**: First defined by Bertrand Meyer in his 1988 book *Object-Oriented Software Construction*. Meyer's original formulation relied on implementation inheritance; Martin later reinterpreted it using polymorphism and abstraction.
- **Liskov Substitution Principle (LSP)**: Introduced by Barbara Liskov in her 1987 keynote *Data Abstraction and Hierarchy*, and formalized in a 1994 paper with Jeannette Wing. It draws on Bertrand Meyer's Design by Contract concepts.
- **Interface Segregation Principle (ISP)**: Articulated by Robert C. Martin while consulting for Xerox in the 1990s. The principle arose from a real problem with a large, monolithic interface in a printer system.
- **Dependency Inversion Principle (DIP)**: Formulated by Robert C. Martin, building on the broader idea that high-level policy should not depend on low-level detail.

Martin later expanded on all five in his 2003 book *Agile Software Development: Principles, Patterns, and Practices* and its 2006 C# edition with Micah Martin.

### Why SOLID Still Matters in 2026

You might wonder whether principles conceived in the late 1980s through the early 2000s are still relevant in an era of microservices, serverless functions, functional programming, and AI-assisted code generation. The answer is a firm yes — though with some nuance.

The underlying problems that SOLID addresses — managing dependencies, isolating change, reducing coupling, enabling testability — are universal to software engineering regardless of paradigm or architecture. A microservice with tangled internal dependencies is just as painful to maintain as a monolithic class with too many responsibilities. A serverless function that depends on concrete implementations is just as hard to test as a desktop application with the same problem.

What has changed is the scale at which these principles apply. In 2000, SOLID was primarily discussed in the context of classes within a single application. Today, the same ideas apply at the level of modules, packages, services, and even entire systems. The Single Responsibility Principle can be applied to a function, a class, a NuGet package, or a microservice. Dependency Inversion shows up in hexagonal architecture, clean architecture, and any system that uses ports and adapters.

Let us now work through each principle in detail.

## Part 2: The Single Responsibility Principle (SRP)

### The Definition

Robert C. Martin's original formulation of the Single Responsibility Principle is:

> A class should have one, and only one, reason to change.

The key phrase is "reason to change." A "reason to change" corresponds to a stakeholder or an actor — a person or group of people who might request a change to the software. If a class serves multiple actors, changes requested by one actor might break the code that serves another.

Martin later refined this definition in his 2018 book *Clean Architecture*:

> A module should be responsible to one, and only one, actor.

This is a subtle but important shift. It is not about the class doing "only one thing" in the most literal sense — a class can have multiple methods and still have a single responsibility. The question is whether those methods all serve the same actor or the same axis of change.

### A Violation in the Wild

Imagine you are building an employee management system. You write a class like this:

```csharp
public class Employee
{
    public string Name { get; set; } = "";
    public decimal Salary { get; set; }
    public string Department { get; set; } = "";

    // Used by the HR department to calculate pay
    public decimal CalculatePay()
    {
        // Complex payroll logic: overtime, benefits, deductions
        return Salary * 1.0m; // simplified
    }

    // Used by the reporting team to generate reports
    public string GeneratePerformanceReport()
    {
        return $"Performance report for {Name} in {Department}";
    }

    // Used by the DBA team to persist data
    public void SaveToDatabase(string connectionString)
    {
        // ADO.NET or EF Core logic to save the employee
        Console.WriteLine($"Saving {Name} to database...");
    }
}
```

This class has three reasons to change:

1. The HR department changes the payroll calculation rules.
2. The reporting team changes the report format.
3. The DBA team changes the database schema or persistence strategy.

Each of these changes serves a different actor. If the reporting team asks for a new column in the performance report, you modify the `Employee` class — and now the payroll calculation code and the persistence code must be recompiled, retested, and redeployed, even though they did not change.

### Applying SRP

The fix is to separate these responsibilities into distinct classes:

```csharp
// The Employee class is now a pure data model
public class Employee
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public decimal Salary { get; set; }
    public string Department { get; set; } = "";
}

// Responsibility: payroll calculations (serves the HR actor)
public class PayrollCalculator
{
    public decimal CalculatePay(Employee employee)
    {
        // All the complex payroll logic lives here
        var basePay = employee.Salary;
        var deductions = basePay * 0.08m; // example: 8% deductions
        return basePay - deductions;
    }
}

// Responsibility: generating reports (serves the reporting actor)
public class PerformanceReportGenerator
{
    public string Generate(Employee employee)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Performance Report: {employee.Name}");
        sb.AppendLine($"Department: {employee.Department}");
        sb.AppendLine($"Generated: {DateTime.UtcNow:yyyy-MM-dd}");
        return sb.ToString();
    }
}

// Responsibility: persistence (serves the DBA/infrastructure actor)
public class EmployeeRepository
{
    private readonly string _connectionString;

    public EmployeeRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public void Save(Employee employee)
    {
        // EF Core, Dapper, ADO.NET — whatever the persistence strategy is
        Console.WriteLine($"Saving employee {employee.Id} to database...");
    }

    public Employee? GetById(int id)
    {
        // Retrieve from database
        Console.WriteLine($"Loading employee {id} from database...");
        return null; // simplified
    }
}
```

Now each class has one reason to change. The `PayrollCalculator` changes only when payroll rules change. The `PerformanceReportGenerator` changes only when the report format changes. The `EmployeeRepository` changes only when the persistence strategy changes. The `Employee` class itself changes only when the data model changes.

### SRP in ASP.NET and Blazor

In the ASP.NET world, SRP shows up frequently in controller and service design. A common violation is the "god controller" that handles authentication, business logic, validation, and response formatting all in one class:

```csharp
// Violation: this controller does too much
[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly DbContext _db;

    public OrdersController(DbContext db) => _db = db;

    [HttpPost]
    public async Task<IActionResult> CreateOrder(CreateOrderRequest request)
    {
        // Validation logic (should be in a validator)
        if (string.IsNullOrEmpty(request.CustomerEmail))
            return BadRequest("Email is required");

        // Business rules (should be in a service)
        var discount = request.Total > 100 ? 0.1m : 0m;
        var finalTotal = request.Total * (1 - discount);

        // Persistence (should be in a repository)
        var order = new Order { Total = finalTotal, Email = request.CustomerEmail };
        _db.Orders.Add(order);
        await _db.SaveChangesAsync();

        // Notification (should be in a notification service)
        await SendEmailAsync(request.CustomerEmail, "Order Confirmed", $"Total: {finalTotal}");

        return Ok(order);
    }

    private Task SendEmailAsync(string to, string subject, string body)
    {
        Console.WriteLine($"Sending email to {to}: {subject}");
        return Task.CompletedTask;
    }
}
```

A cleaner approach separates each concern:

```csharp
// The controller only orchestrates — it delegates to specialized services
[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;

    public OrdersController(IOrderService orderService) => _orderService = orderService;

    [HttpPost]
    public async Task<IActionResult> CreateOrder(CreateOrderRequest request)
    {
        var result = await _orderService.PlaceOrderAsync(request);
        return result.IsSuccess ? Ok(result.Order) : BadRequest(result.Error);
    }
}

// The service handles orchestration of business rules
public class OrderService : IOrderService
{
    private readonly IOrderRepository _repository;
    private readonly IDiscountCalculator _discountCalculator;
    private readonly INotificationService _notificationService;

    public OrderService(
        IOrderRepository repository,
        IDiscountCalculator discountCalculator,
        INotificationService notificationService)
    {
        _repository = repository;
        _discountCalculator = discountCalculator;
        _notificationService = notificationService;
    }

    public async Task<OrderResult> PlaceOrderAsync(CreateOrderRequest request)
    {
        var discount = _discountCalculator.Calculate(request.Total);
        var finalTotal = request.Total * (1 - discount);

        var order = new Order { Total = finalTotal, Email = request.CustomerEmail };
        await _repository.SaveAsync(order);

        await _notificationService.SendOrderConfirmationAsync(order);

        return new OrderResult { IsSuccess = true, Order = order };
    }
}
```

### Common SRP Mistakes

**Mistake 1: Taking it too far.** Creating a class for every single method leads to an explosion of tiny classes that are individually simple but collectively hard to navigate. The principle is about cohesion — grouping things that change together — not about minimizing the number of methods per class.

**Mistake 2: Confusing "one thing" with "one responsibility."** A `UserValidator` class might have methods for validating email format, password strength, and username length. These are all part of one responsibility: validation of user input. They change for the same reason (validation rules change) and serve the same actor. This is a single responsibility, even though it involves multiple methods.

**Mistake 3: Ignoring SRP in Blazor components.** A Blazor component that fetches data, transforms it, renders it, and handles multiple types of user interaction is doing too much. Extract data fetching into services, transformation into utility classes, and complex interaction logic into separate components.

## Part 3: The Open/Closed Principle (OCP)

### The Definition

Bertrand Meyer first articulated this principle in his 1988 book *Object-Oriented Software Construction*:

> Software entities (classes, modules, functions, etc.) should be open for extension, but closed for modification.

"Open for extension" means you can add new behavior. "Closed for modification" means you do not need to change existing, working code to add that new behavior.

Meyer's original interpretation relied on implementation inheritance: you extend a class by inheriting from it and overriding methods, without modifying the base class. Robert C. Martin later reinterpreted the principle to emphasize polymorphism through abstractions (interfaces and abstract classes) rather than concrete inheritance.

### Why It Matters

Every time you modify existing code, you risk introducing bugs into functionality that was previously working. If you can add new features by writing new code rather than changing old code, you dramatically reduce the surface area for regressions.

Consider a payment processing system:

```csharp
// Violation: adding a new payment method requires modifying this class
public class PaymentProcessor
{
    public void ProcessPayment(string paymentType, decimal amount)
    {
        if (paymentType == "CreditCard")
        {
            Console.WriteLine($"Processing credit card payment of {amount:C}");
            // Credit card specific logic
        }
        else if (paymentType == "PayPal")
        {
            Console.WriteLine($"Processing PayPal payment of {amount:C}");
            // PayPal specific logic
        }
        else if (paymentType == "BankTransfer")
        {
            Console.WriteLine($"Processing bank transfer of {amount:C}");
            // Bank transfer specific logic
        }
        else
        {
            throw new ArgumentException($"Unknown payment type: {paymentType}");
        }
    }
}
```

This class violates OCP because every time the business adds a new payment method — cryptocurrency, Apple Pay, buy-now-pay-later — you must open this class and add another `else if` branch. Each modification risks breaking the existing branches.

### Applying OCP with Polymorphism

The standard solution is to define an abstraction and let each payment method implement it:

```csharp
public interface IPaymentMethod
{
    string Name { get; }
    Task<PaymentResult> ProcessAsync(decimal amount);
}

public class CreditCardPayment : IPaymentMethod
{
    public string Name => "CreditCard";

    public Task<PaymentResult> ProcessAsync(decimal amount)
    {
        Console.WriteLine($"Charging credit card: {amount:C}");
        // Real implementation: call Stripe, Square, etc.
        return Task.FromResult(new PaymentResult { Success = true, TransactionId = Guid.NewGuid().ToString() });
    }
}

public class PayPalPayment : IPaymentMethod
{
    public string Name => "PayPal";

    public Task<PaymentResult> ProcessAsync(decimal amount)
    {
        Console.WriteLine($"Processing PayPal payment: {amount:C}");
        return Task.FromResult(new PaymentResult { Success = true, TransactionId = Guid.NewGuid().ToString() });
    }
}

public class BankTransferPayment : IPaymentMethod
{
    public string Name => "BankTransfer";

    public Task<PaymentResult> ProcessAsync(decimal amount)
    {
        Console.WriteLine($"Initiating bank transfer: {amount:C}");
        return Task.FromResult(new PaymentResult { Success = true, TransactionId = Guid.NewGuid().ToString() });
    }
}

public record PaymentResult
{
    public bool Success { get; init; }
    public string TransactionId { get; init; } = "";
    public string? ErrorMessage { get; init; }
}
```

Now the processor is closed for modification:

```csharp
public class PaymentProcessor
{
    private readonly IEnumerable<IPaymentMethod> _paymentMethods;

    public PaymentProcessor(IEnumerable<IPaymentMethod> paymentMethods)
    {
        _paymentMethods = paymentMethods;
    }

    public async Task<PaymentResult> ProcessPaymentAsync(string paymentType, decimal amount)
    {
        var method = _paymentMethods.FirstOrDefault(m =>
            m.Name.Equals(paymentType, StringComparison.OrdinalIgnoreCase));

        if (method is null)
            return new PaymentResult { Success = false, ErrorMessage = $"Unknown payment type: {paymentType}" };

        return await method.ProcessAsync(amount);
    }
}
```

When a new payment method is needed — say, cryptocurrency — you simply write a new class:

```csharp
public class CryptoPayment : IPaymentMethod
{
    public string Name => "Crypto";

    public Task<PaymentResult> ProcessAsync(decimal amount)
    {
        Console.WriteLine($"Processing crypto payment: {amount:C}");
        return Task.FromResult(new PaymentResult { Success = true, TransactionId = Guid.NewGuid().ToString() });
    }
}
```

And register it in your DI container:

```csharp
builder.Services.AddTransient<IPaymentMethod, CreditCardPayment>();
builder.Services.AddTransient<IPaymentMethod, PayPalPayment>();
builder.Services.AddTransient<IPaymentMethod, BankTransferPayment>();
builder.Services.AddTransient<IPaymentMethod, CryptoPayment>(); // new — no existing code changed
```

The `PaymentProcessor` class was never modified. The existing payment method classes were never modified. You added new behavior solely by writing new code.

### OCP with the Strategy Pattern

The Strategy pattern is one of the most natural ways to apply OCP. Here is a sorting example that allows pluggable comparison strategies:

```csharp
public interface ISortStrategy<T>
{
    IEnumerable<T> Sort(IEnumerable<T> items);
}

public class AlphabeticalSortStrategy : ISortStrategy<string>
{
    public IEnumerable<string> Sort(IEnumerable<string> items) =>
        items.OrderBy(x => x, StringComparer.OrdinalIgnoreCase);
}

public class LengthSortStrategy : ISortStrategy<string>
{
    public IEnumerable<string> Sort(IEnumerable<string> items) =>
        items.OrderBy(x => x.Length);
}

public class ReverseSortStrategy : ISortStrategy<string>
{
    public IEnumerable<string> Sort(IEnumerable<string> items) =>
        items.OrderByDescending(x => x, StringComparer.OrdinalIgnoreCase);
}

// The sorter is closed for modification — new strategies can be added without changing this class
public class ItemSorter<T>
{
    private readonly ISortStrategy<T> _strategy;

    public ItemSorter(ISortStrategy<T> strategy)
    {
        _strategy = strategy;
    }

    public IEnumerable<T> Sort(IEnumerable<T> items) => _strategy.Sort(items);
}
```

### OCP in ASP.NET Middleware

ASP.NET Core's middleware pipeline is a beautiful example of OCP in action. The pipeline itself is closed for modification — you do not change the framework source code. But it is open for extension — you add new middleware components:

```csharp
var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// Each of these extends the pipeline without modifying existing middleware
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

// Your custom middleware — extends the pipeline, modifies nothing
app.Use(async (context, next) =>
{
    var stopwatch = Stopwatch.StartNew();
    await next(context);
    stopwatch.Stop();
    context.Response.Headers["X-Response-Time"] = $"{stopwatch.ElapsedMilliseconds}ms";
});

app.MapControllers();
app.Run();
```

### Common OCP Mistakes

**Mistake 1: Premature abstraction.** Do not create interfaces and abstract classes for everything "just in case" you might need to extend it later. Apply OCP when you have evidence that a particular axis of change is real or likely. The first time you need a second implementation is usually the right time to extract an interface.

**Mistake 2: Thinking OCP means you can never edit a file.** The principle is about design, not a literal prohibition on modifying source files. Bug fixes, refactoring for clarity, and performance improvements are all valid reasons to modify existing code. OCP is about designing your system so that adding new features does not require modifying code that already works.

**Mistake 3: Switch statements are not always violations.** A switch statement over a small, stable set of values (like days of the week, or a finite set of known enum values) is not necessarily an OCP violation. The principle applies when the set of cases is expected to grow over time.

## Part 4: The Liskov Substitution Principle (LSP)

### The Definition

Barbara Liskov introduced this principle in her 1987 keynote *Data Abstraction and Hierarchy*. In a 1994 paper with Jeannette Wing, she formalized it as:

> Let φ(x) be a property provable about objects x of type T. Then φ(y) should be true for objects y of type S where S is a subtype of T.

Robert C. Martin restated it more accessibly:

> Subtypes must be substitutable for their base types.

In practical terms: if your code works with a reference to a base class or interface, it should continue to work correctly when you substitute any derived class or implementation — without the calling code needing to know or care about the specific subtype.

### The Classic Violation: Rectangle and Square

This is the most famous example of an LSP violation. In geometry, a square "is a" rectangle — it is a rectangle with equal sides. So you might model this with inheritance:

```csharp
public class Rectangle
{
    public virtual int Width { get; set; }
    public virtual int Height { get; set; }

    public int CalculateArea() => Width * Height;
}

public class Square : Rectangle
{
    public override int Width
    {
        get => base.Width;
        set
        {
            base.Width = value;
            base.Height = value; // Keep sides equal
        }
    }

    public override int Height
    {
        get => base.Height;
        set
        {
            base.Height = value;
            base.Width = value; // Keep sides equal
        }
    }
}
```

This compiles and even seems to work. But consider a function that operates on rectangles:

```csharp
public void ResizeRectangle(Rectangle rect)
{
    rect.Width = 10;
    rect.Height = 5;

    // For any Rectangle, we expect the area to be 10 * 5 = 50
    Debug.Assert(rect.CalculateArea() == 50);
}
```

Pass a `Rectangle` and the assertion holds. Pass a `Square` and it fails — because setting `Height = 5` also sets `Width = 5`, so the area is 25, not 50.

The `Square` class cannot be substituted for `Rectangle` without breaking the program's correctness. This is an LSP violation.

### The Fix

The solution is to rethink the inheritance hierarchy. In terms of behavior, a square is not a rectangle because it does not honor the rectangle's contract that width and height can be set independently. A better design uses composition or separate types:

```csharp
public interface IShape
{
    int CalculateArea();
}

public class Rectangle : IShape
{
    public int Width { get; }
    public int Height { get; }

    public Rectangle(int width, int height)
    {
        Width = width;
        Height = height;
    }

    public int CalculateArea() => Width * Height;
}

public class Square : IShape
{
    public int Side { get; }

    public Square(int side)
    {
        Side = side;
    }

    public int CalculateArea() => Side * Side;
}
```

Now `Rectangle` and `Square` are siblings under `IShape`, not parent and child. No code that works with `IShape` will be surprised by either implementation because neither makes promises it cannot keep.

### LSP and Design by Contract

The Liskov Substitution Principle is closely related to Bertrand Meyer's Design by Contract, which he introduced in his 1988 book *Object-Oriented Software Construction* and implemented in the Eiffel language. The rules are:

1. **Preconditions cannot be strengthened in a subtype.** If the base class accepts any positive integer, the subtype cannot demand only even numbers.
2. **Postconditions cannot be weakened in a subtype.** If the base class guarantees the result is non-null, the subtype cannot return null.
3. **Invariants must be preserved.** If the base class guarantees that a balance is never negative, the subtype must maintain that guarantee.

Here is a practical C# example:

```csharp
public abstract class Account
{
    public decimal Balance { get; protected set; }

    // Precondition: amount > 0
    // Postcondition: Balance decreases by amount
    // Invariant: Balance >= 0
    public virtual void Withdraw(decimal amount)
    {
        if (amount <= 0)
            throw new ArgumentException("Amount must be positive");

        if (Balance - amount < 0)
            throw new InvalidOperationException("Insufficient funds");

        Balance -= amount;
    }
}

public class SavingsAccount : Account
{
    // CORRECT: Does not strengthen the precondition.
    // Adds a postcondition (minimum balance check) that is stricter,
    // which is allowed because it does not weaken the base class guarantee.
    public override void Withdraw(decimal amount)
    {
        if (amount <= 0)
            throw new ArgumentException("Amount must be positive");

        if (Balance - amount < 100) // Minimum balance of 100
            throw new InvalidOperationException("Must maintain minimum balance of 100");

        Balance -= amount;
    }
}

public class FixedDepositAccount : Account
{
    // VIOLATION: This strengthens the precondition by adding a maturity date check.
    // Code that works with Account.Withdraw() will be surprised when this throws
    // for a reason it did not expect.
    public DateTime MaturityDate { get; set; }

    public override void Withdraw(decimal amount)
    {
        if (DateTime.UtcNow < MaturityDate)
            throw new InvalidOperationException("Cannot withdraw before maturity");

        base.Withdraw(amount);
    }
}
```

The `FixedDepositAccount` violates LSP because it introduces a new precondition — the current date must be past the maturity date — that callers working with the base `Account` type do not expect. A better design would either not inherit from `Account` or use a separate interface that explicitly models the maturity constraint.

### Real-World LSP Violations in .NET

**Violating LSP with collections:** A common trap is returning a `ReadOnlyCollection<T>` from a property typed as `IList<T>`. The `IList<T>` interface includes `Add`, `Remove`, and `Insert` methods, but `ReadOnlyCollection<T>` throws `NotSupportedException` when you call them. Code that expects an `IList<T>` to support mutation will break.

```csharp
// Violation: IList<T> promises mutation, but this implementation does not deliver
public class UserService
{
    private readonly List<string> _roles = ["admin", "editor", "viewer"];

    // This return type promises mutability but delivers read-only
    public IList<string> GetRoles() => _roles.AsReadOnly();
}

// Better: use a type that accurately describes the contract
public class UserServiceFixed
{
    private readonly List<string> _roles = ["admin", "editor", "viewer"];

    public IReadOnlyList<string> GetRoles() => _roles.AsReadOnly();
}
```

**Violating LSP with exceptions:** If a base class method does not document that it throws a specific exception, a derived class should not introduce that exception. Callers who are not prepared to catch it will be surprised.

```csharp
public interface IFileReader
{
    string ReadAll(string path);
}

// Good: throws IOException, which is expected for file operations
public class LocalFileReader : IFileReader
{
    public string ReadAll(string path) => File.ReadAllText(path);
}

// Problematic: throws HttpRequestException, which callers of IFileReader do not expect
public class RemoteFileReader : IFileReader
{
    private readonly HttpClient _http;

    public RemoteFileReader(HttpClient http) => _http = http;

    public string ReadAll(string path)
    {
        // This can throw HttpRequestException — a surprise for callers expecting file I/O errors
        return _http.GetStringAsync(path).GetAwaiter().GetResult();
    }
}
```

The fix is to catch the transport-specific exceptions and wrap them in something the caller expects:

```csharp
public class RemoteFileReaderFixed : IFileReader
{
    private readonly HttpClient _http;

    public RemoteFileReaderFixed(HttpClient http) => _http = http;

    public string ReadAll(string path)
    {
        try
        {
            return _http.GetStringAsync(path).GetAwaiter().GetResult();
        }
        catch (HttpRequestException ex)
        {
            throw new IOException($"Failed to read remote file: {path}", ex);
        }
    }
}
```

### How to Test for LSP Compliance

Write tests that exercise the base type contract, then run those same tests against every subtype:

```csharp
public abstract class ShapeTests<T> where T : IShape
{
    protected abstract T CreateShape();

    [Fact]
    public void Area_ShouldBeNonNegative()
    {
        var shape = CreateShape();
        Assert.True(shape.CalculateArea() >= 0);
    }
}

public class RectangleTests : ShapeTests<Rectangle>
{
    protected override Rectangle CreateShape() => new(5, 3);

    [Fact]
    public void Area_ShouldBeWidthTimesHeight()
    {
        var rect = new Rectangle(5, 3);
        Assert.Equal(15, rect.CalculateArea());
    }
}

public class SquareTests : ShapeTests<Square>
{
    protected override Square CreateShape() => new(4);

    [Fact]
    public void Area_ShouldBeSideSquared()
    {
        var square = new Square(4);
        Assert.Equal(16, square.CalculateArea());
    }
}
```

If any derived class fails a test written for the base type, you have an LSP violation.

## Part 5: The Interface Segregation Principle (ISP)

### The Definition

> Clients should not be forced to depend upon interfaces that they do not use.

Robert C. Martin developed this principle while consulting for Xerox. The Xerox printer system had a single "Job" interface with methods for printing, stapling, collating, faxing, and scanning. Every client — even one that only needed to print — was forced to depend on the entire interface. Changes to the faxing methods forced recompilation of printing clients, even though they had nothing to do with faxing.

### A Violation

Consider a worker interface in a factory management system:

```csharp
public interface IWorker
{
    void Work();
    void Eat();
    void Sleep();
    void AttendMeeting();
    void WriteReport();
}

public class HumanWorker : IWorker
{
    public void Work() => Console.WriteLine("Working...");
    public void Eat() => Console.WriteLine("Eating lunch...");
    public void Sleep() => Console.WriteLine("Sleeping...");
    public void AttendMeeting() => Console.WriteLine("In a meeting...");
    public void WriteReport() => Console.WriteLine("Writing report...");
}

public class RobotWorker : IWorker
{
    public void Work() => Console.WriteLine("Robot working...");

    // Robots do not eat
    public void Eat() => throw new NotSupportedException("Robots don't eat");

    // Robots do not sleep
    public void Sleep() => throw new NotSupportedException("Robots don't sleep");

    // Robots do not attend meetings
    public void AttendMeeting() => throw new NotSupportedException("Robots don't attend meetings");

    // Robots do not write reports
    public void WriteReport() => throw new NotSupportedException("Robots don't write reports");
}
```

The `RobotWorker` class is forced to implement five methods, four of which it does not support. This is an ISP violation — and it is also an LSP violation, since substituting a `RobotWorker` for a `HumanWorker` will throw exceptions that callers do not expect.

### Applying ISP

Split the interface into smaller, focused interfaces that each describe a single capability:

```csharp
public interface IWorkable
{
    void Work();
}

public interface IFeedable
{
    void Eat();
}

public interface ISleepable
{
    void Sleep();
}

public interface IMeetingAttendee
{
    void AttendMeeting();
}

public interface IReportWriter
{
    void WriteReport();
}

public class HumanWorker : IWorkable, IFeedable, ISleepable, IMeetingAttendee, IReportWriter
{
    public void Work() => Console.WriteLine("Working...");
    public void Eat() => Console.WriteLine("Eating lunch...");
    public void Sleep() => Console.WriteLine("Sleeping...");
    public void AttendMeeting() => Console.WriteLine("In a meeting...");
    public void WriteReport() => Console.WriteLine("Writing report...");
}

public class RobotWorker : IWorkable
{
    public void Work() => Console.WriteLine("Robot working efficiently...");
}
```

Now `RobotWorker` only implements what it actually supports. Code that only needs a worker can accept `IWorkable`. Code that needs meeting attendance can accept `IMeetingAttendee`. No client is forced to depend on capabilities it does not use.

### A Realistic .NET Example: Repository Interfaces

A common ISP violation in .NET projects is the "god repository" interface:

```csharp
// Violation: every consumer depends on all methods, even if they only need one
public interface IRepository<T>
{
    Task<T?> GetByIdAsync(int id);
    Task<IReadOnlyList<T>> GetAllAsync();
    Task<IReadOnlyList<T>> FindAsync(Expression<Func<T, bool>> predicate);
    Task AddAsync(T entity);
    Task UpdateAsync(T entity);
    Task DeleteAsync(int id);
    Task<int> CountAsync();
    Task<bool> ExistsAsync(int id);
    Task BulkInsertAsync(IEnumerable<T> entities);
    Task ExecuteRawSqlAsync(string sql);
}
```

A read-only reporting service should not need to depend on `AddAsync`, `DeleteAsync`, or `ExecuteRawSqlAsync`. Split it:

```csharp
public interface IReadRepository<T>
{
    Task<T?> GetByIdAsync(int id);
    Task<IReadOnlyList<T>> GetAllAsync();
    Task<IReadOnlyList<T>> FindAsync(Expression<Func<T, bool>> predicate);
    Task<int> CountAsync();
    Task<bool> ExistsAsync(int id);
}

public interface IWriteRepository<T>
{
    Task AddAsync(T entity);
    Task UpdateAsync(T entity);
    Task DeleteAsync(int id);
}

public interface IBulkRepository<T>
{
    Task BulkInsertAsync(IEnumerable<T> entities);
}

public interface IRawSqlRepository
{
    Task ExecuteRawSqlAsync(string sql);
}

// The full repository composes all the interfaces
public class ProductRepository : IReadRepository<Product>, IWriteRepository<Product>, IBulkRepository<Product>
{
    // Implementation using EF Core, Dapper, or raw ADO.NET
    public Task<Product?> GetByIdAsync(int id) => throw new NotImplementedException();
    public Task<IReadOnlyList<Product>> GetAllAsync() => throw new NotImplementedException();
    public Task<IReadOnlyList<Product>> FindAsync(Expression<Func<Product, bool>> predicate) => throw new NotImplementedException();
    public Task<int> CountAsync() => throw new NotImplementedException();
    public Task<bool> ExistsAsync(int id) => throw new NotImplementedException();
    public Task AddAsync(Product entity) => throw new NotImplementedException();
    public Task UpdateAsync(Product entity) => throw new NotImplementedException();
    public Task DeleteAsync(int id) => throw new NotImplementedException();
    public Task BulkInsertAsync(IEnumerable<Product> entities) => throw new NotImplementedException();
}

// A reporting service only depends on what it needs
public class ProductReportService
{
    private readonly IReadRepository<Product> _repository;

    public ProductReportService(IReadRepository<Product> repository)
    {
        _repository = repository;
    }

    public async Task<int> GetProductCountAsync()
    {
        return await _repository.CountAsync();
    }
}
```

### ISP in Blazor Components

ISP also applies to the parameters and services that Blazor components depend on. A component that accepts a massive parameter object when it only needs a few fields is violating ISP at the component level:

```csharp
// Violation: the component depends on the entire Order object
// but only displays the customer name and total
@code {
    [Parameter] public Order FullOrder { get; set; } = default!;
}

<p>Customer: @FullOrder.Customer.FullName</p>
<p>Total: @FullOrder.Total.ToString("C")</p>
```

Better: pass only what the component needs, or define a focused view model:

```csharp
@code {
    [Parameter] public string CustomerName { get; set; } = "";
    [Parameter] public decimal Total { get; set; }
}

<p>Customer: @CustomerName</p>
<p>Total: @Total.ToString("C")</p>
```

### Common ISP Mistakes

**Mistake 1: Going too granular.** An interface with a single method is sometimes appropriate (think `IDisposable`, `IComparable<T>`), but splitting every interface down to one method per interface can make the system harder to understand. Group methods that are almost always used together.

**Mistake 2: Marker interfaces with no methods.** An empty interface used only for type identification (`public interface IEntity { }`) is not necessarily an ISP violation — it is a different pattern entirely — but be cautious about using them for anything beyond tagging.

**Mistake 3: Ignoring ISP in DI registration.** Even if you split your interfaces correctly, registering them all as the same concrete type in DI means that any consumer can resolve the full implementation. Use specific interface registrations.

## Part 6: The Dependency Inversion Principle (DIP)

### The Definition

Robert C. Martin stated the Dependency Inversion Principle as two rules:

> 1. High-level modules should not depend on low-level modules. Both should depend on abstractions.
> 2. Abstractions should not depend on details. Details should depend on abstractions.

"High-level modules" are the parts of your system that embody business rules and policy. "Low-level modules" are the implementation details — file I/O, database access, HTTP clients, third-party APIs. The principle says that the direction of dependency should be inverted: instead of high-level code depending on low-level code, both should depend on an abstraction that lives alongside the high-level code.

### Why "Inversion"?

In traditional procedural programming, the dependency structure follows the call graph: high-level code calls low-level code, and therefore depends on it. If the database layer changes, the business logic layer must change too.

Dependency Inversion flips this. The high-level module defines an interface that describes what it needs. The low-level module implements that interface. The dependency arrow now points from the low-level module toward the high-level module's abstraction, not the other way around.

### A Violation

```csharp
// High-level module directly depends on low-level module
public class OrderProcessor
{
    private readonly SqlServerDatabase _database;
    private readonly SmtpEmailSender _emailSender;
    private readonly FileSystemLogger _logger;

    public OrderProcessor()
    {
        _database = new SqlServerDatabase("Server=localhost;Database=Orders;...");
        _emailSender = new SmtpEmailSender("smtp.company.com", 587);
        _logger = new FileSystemLogger("/var/log/orders.log");
    }

    public void Process(Order order)
    {
        _logger.Log($"Processing order {order.Id}");
        _database.Save(order);
        _emailSender.Send(order.CustomerEmail, "Order Confirmed", $"Order {order.Id} is confirmed");
        _logger.Log($"Order {order.Id} processed");
    }
}
```

This code has several problems:

- `OrderProcessor` directly instantiates its dependencies, making it impossible to unit test without a real SQL Server, SMTP server, and file system.
- Switching from SQL Server to PostgreSQL requires modifying `OrderProcessor`.
- Switching from SMTP to a queue-based email service requires modifying `OrderProcessor`.
- The high-level business logic is tightly coupled to low-level infrastructure.

### Applying DIP

Define abstractions for each dependency:

```csharp
// Abstractions — these live alongside the high-level module
public interface IOrderRepository
{
    Task SaveAsync(Order order);
    Task<Order?> GetByIdAsync(int id);
}

public interface INotificationService
{
    Task SendAsync(string to, string subject, string body);
}

public interface IAppLogger
{
    void LogInformation(string message);
    void LogError(string message, Exception? ex = null);
}
```

The high-level module depends only on abstractions:

```csharp
public class OrderProcessor
{
    private readonly IOrderRepository _repository;
    private readonly INotificationService _notifications;
    private readonly IAppLogger _logger;

    public OrderProcessor(
        IOrderRepository repository,
        INotificationService notifications,
        IAppLogger logger)
    {
        _repository = repository;
        _notifications = notifications;
        _logger = logger;
    }

    public async Task ProcessAsync(Order order)
    {
        _logger.LogInformation($"Processing order {order.Id}");

        await _repository.SaveAsync(order);
        await _notifications.SendAsync(
            order.CustomerEmail,
            "Order Confirmed",
            $"Your order {order.Id} has been confirmed.");

        _logger.LogInformation($"Order {order.Id} processed successfully");
    }
}
```

Low-level modules implement the abstractions:

```csharp
// Low-level module: SQL Server implementation
public class SqlServerOrderRepository : IOrderRepository
{
    private readonly string _connectionString;

    public SqlServerOrderRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task SaveAsync(Order order)
    {
        // Use EF Core, Dapper, or ADO.NET to save
        await Task.CompletedTask;
    }

    public async Task<Order?> GetByIdAsync(int id)
    {
        await Task.CompletedTask;
        return null; // simplified
    }
}

// Low-level module: PostgreSQL implementation
public class PostgresOrderRepository : IOrderRepository
{
    private readonly string _connectionString;

    public PostgresOrderRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task SaveAsync(Order order)
    {
        // Npgsql-based implementation
        await Task.CompletedTask;
    }

    public async Task<Order?> GetByIdAsync(int id)
    {
        await Task.CompletedTask;
        return null;
    }
}

// Low-level module: SMTP email
public class SmtpNotificationService : INotificationService
{
    private readonly string _smtpHost;
    private readonly int _port;

    public SmtpNotificationService(string smtpHost, int port)
    {
        _smtpHost = smtpHost;
        _port = port;
    }

    public async Task SendAsync(string to, string subject, string body)
    {
        Console.WriteLine($"Sending email via SMTP to {to}: {subject}");
        await Task.CompletedTask;
    }
}

// Low-level module: Queue-based notifications
public class QueueNotificationService : INotificationService
{
    public async Task SendAsync(string to, string subject, string body)
    {
        Console.WriteLine($"Queuing notification for {to}: {subject}");
        await Task.CompletedTask;
    }
}
```

Wire it up in the DI container:

```csharp
// In Program.cs or Startup.cs
builder.Services.AddScoped<IOrderRepository, PostgresOrderRepository>(
    sp => new PostgresOrderRepository(builder.Configuration.GetConnectionString("Orders")!));
builder.Services.AddScoped<INotificationService, QueueNotificationService>();
builder.Services.AddScoped<IAppLogger, SerilogAppLogger>();
builder.Services.AddScoped<OrderProcessor>();
```

Switching from SQL Server to PostgreSQL is now a one-line change in DI registration. No business logic code is modified.

### DIP and Testability

The single greatest practical benefit of DIP is testability. With abstractions injected, you can substitute test doubles:

```csharp
public class OrderProcessorTests
{
    [Fact]
    public async Task ProcessAsync_SavesOrderAndSendsNotification()
    {
        // Arrange
        var savedOrders = new List<Order>();
        var sentNotifications = new List<(string To, string Subject, string Body)>();

        var mockRepo = new InMemoryOrderRepository(savedOrders);
        var mockNotifier = new FakeNotificationService(sentNotifications);
        var mockLogger = new NullAppLogger();

        var processor = new OrderProcessor(mockRepo, mockNotifier, mockLogger);
        var order = new Order { Id = 1, CustomerEmail = "test@example.com" };

        // Act
        await processor.ProcessAsync(order);

        // Assert
        Assert.Single(savedOrders);
        Assert.Equal(1, savedOrders[0].Id);
        Assert.Single(sentNotifications);
        Assert.Equal("test@example.com", sentNotifications[0].To);
    }
}

// Simple test doubles — no mocking framework needed
public class InMemoryOrderRepository : IOrderRepository
{
    private readonly List<Order> _orders;

    public InMemoryOrderRepository(List<Order> orders) => _orders = orders;

    public Task SaveAsync(Order order)
    {
        _orders.Add(order);
        return Task.CompletedTask;
    }

    public Task<Order?> GetByIdAsync(int id) =>
        Task.FromResult(_orders.FirstOrDefault(o => o.Id == id));
}

public class FakeNotificationService : INotificationService
{
    private readonly List<(string To, string Subject, string Body)> _sent;

    public FakeNotificationService(List<(string To, string Subject, string Body)> sent) => _sent = sent;

    public Task SendAsync(string to, string subject, string body)
    {
        _sent.Add((to, subject, body));
        return Task.CompletedTask;
    }
}

public class NullAppLogger : IAppLogger
{
    public void LogInformation(string message) { }
    public void LogError(string message, Exception? ex = null) { }
}
```

These tests run in milliseconds, require no infrastructure, and will never fail because a database is down or an SMTP server is unreachable.

### DIP in Blazor WebAssembly

In Blazor WebAssembly, DIP is essential for components that consume services:

```csharp
// The Blazor component depends on an abstraction
@inject IBlogService BlogService
@inject ILogger<Blog> Logger

@code {
    private BlogPostMetadata[]? posts;

    protected override async Task OnInitializedAsync()
    {
        posts = await BlogService.GetPostsAsync();
    }
}
```

The concrete `BlogService` (which uses `HttpClient` to fetch JSON) is registered in DI. During testing, you register a different implementation that returns canned data. The component never knows the difference.

### DIP vs. Dependency Injection

A common confusion: Dependency Inversion is a design principle about the direction of dependencies. Dependency Injection is a technique for providing dependencies to a class (typically through constructor parameters). DI frameworks (like ASP.NET Core's built-in container) are tools that automate dependency injection.

You can apply Dependency Inversion without a DI container — just pass interfaces through constructors manually. And you can use a DI container without actually inverting dependencies (by injecting concrete classes instead of abstractions). They are related but distinct concepts:

- **Dependency Inversion**: A principle about which direction dependencies should point.
- **Dependency Injection**: A pattern for supplying dependencies from outside a class.
- **IoC Container**: A framework that automates dependency injection.

### Common DIP Mistakes

**Mistake 1: Abstracting everything.** Not every class needs an interface. If a class is a simple data container (`record Product(string Name, decimal Price)`), wrapping it in an interface adds complexity with no benefit. Apply DIP to the boundaries — the seams where high-level policy meets low-level infrastructure.

**Mistake 2: Leaky abstractions.** An interface that mirrors the API of a specific implementation (like `ISqlServerDatabase` with methods named `ExecuteStoredProcedure` and `UseTempTable`) is not a real abstraction. It is just an indirection. True abstractions describe what the high-level module needs, not how the low-level module works.

**Mistake 3: Putting abstractions in the wrong project.** The interface should live in the same project or layer as the high-level module that depends on it, not alongside the low-level implementation. If `IOrderRepository` lives in your data access project, the dependency arrow still points from business logic down to data access — even though you are coding against an interface.

## Part 7: How SOLID Principles Interact

The five principles are not independent — they reinforce each other. Understanding their interactions helps you apply them holistically rather than as isolated rules.

### SRP + OCP

If a class has a single responsibility, it is easier to keep it closed for modification. A class that does one thing has fewer reasons to change. When new behavior is needed, you add a new class rather than modifying the existing one.

### OCP + DIP

Dependency Inversion is often the mechanism by which you achieve OCP. By depending on abstractions (DIP), you can substitute different concrete implementations (OCP) without modifying the code that depends on the abstraction. The `PaymentProcessor` example from Part 3 works precisely because it depends on `IPaymentMethod` (DIP) rather than concrete payment classes.

### LSP + ISP

Interface Segregation helps prevent LSP violations. When interfaces are small and focused, implementations are less likely to throw `NotSupportedException` or exhibit degenerate behavior. The `RobotWorker` that threw exceptions was both an ISP violation (fat interface) and an LSP violation (could not be substituted for `IWorker` without breaking things).

### All Five Together: A Complete Example

Let us design a notification system that demonstrates all five principles working in concert:

```csharp
// ISP: Small, focused interfaces for different capabilities
public interface INotificationSender
{
    string Channel { get; } // "email", "sms", "push"
    Task SendAsync(NotificationMessage message);
}

public interface INotificationTemplateEngine
{
    string Render(string templateName, Dictionary<string, string> variables);
}

public interface INotificationLogger
{
    Task LogAsync(NotificationMessage message, bool success, string? errorMessage = null);
}

// SRP: Each class has one reason to change
public record NotificationMessage(
    string Recipient,
    string Subject,
    string Body,
    string Channel);

public class EmailSender : INotificationSender
{
    public string Channel => "email";

    public async Task SendAsync(NotificationMessage message)
    {
        Console.WriteLine($"Sending email to {message.Recipient}: {message.Subject}");
        await Task.CompletedTask;
    }
}

public class SmsSender : INotificationSender
{
    public string Channel => "sms";

    public async Task SendAsync(NotificationMessage message)
    {
        Console.WriteLine($"Sending SMS to {message.Recipient}: {message.Body}");
        await Task.CompletedTask;
    }
}

public class PushNotificationSender : INotificationSender
{
    public string Channel => "push";

    public async Task SendAsync(NotificationMessage message)
    {
        Console.WriteLine($"Sending push notification to {message.Recipient}: {message.Subject}");
        await Task.CompletedTask;
    }
}

// OCP: Adding a new channel requires writing a new class, not modifying existing ones
// LSP: Every INotificationSender implementation is fully substitutable
// DIP: NotificationService depends on abstractions, not concrete senders

public class NotificationService
{
    private readonly IEnumerable<INotificationSender> _senders;
    private readonly INotificationTemplateEngine _templateEngine;
    private readonly INotificationLogger _logger;

    public NotificationService(
        IEnumerable<INotificationSender> senders,
        INotificationTemplateEngine templateEngine,
        INotificationLogger logger)
    {
        _senders = senders;
        _templateEngine = templateEngine;
        _logger = logger;
    }

    public async Task NotifyAsync(
        string recipient,
        string channel,
        string templateName,
        Dictionary<string, string> variables)
    {
        var body = _templateEngine.Render(templateName, variables);
        var message = new NotificationMessage(recipient, templateName, body, channel);

        var sender = _senders.FirstOrDefault(s =>
            s.Channel.Equals(channel, StringComparison.OrdinalIgnoreCase));

        if (sender is null)
        {
            await _logger.LogAsync(message, false, $"No sender found for channel: {channel}");
            return;
        }

        try
        {
            await sender.SendAsync(message);
            await _logger.LogAsync(message, true);
        }
        catch (Exception ex)
        {
            await _logger.LogAsync(message, false, ex.Message);
            throw;
        }
    }
}
```

Registration in DI:

```csharp
builder.Services.AddTransient<INotificationSender, EmailSender>();
builder.Services.AddTransient<INotificationSender, SmsSender>();
builder.Services.AddTransient<INotificationSender, PushNotificationSender>();
builder.Services.AddTransient<INotificationTemplateEngine, HandlebarsTemplateEngine>();
builder.Services.AddTransient<INotificationLogger, DatabaseNotificationLogger>();
builder.Services.AddTransient<NotificationService>();
```

Adding a new channel (say, Slack):

```csharp
public class SlackSender : INotificationSender
{
    public string Channel => "slack";

    public async Task SendAsync(NotificationMessage message)
    {
        Console.WriteLine($"Posting to Slack for {message.Recipient}: {message.Body}");
        await Task.CompletedTask;
    }
}

// One line added to DI — nothing else changes
builder.Services.AddTransient<INotificationSender, SlackSender>();
```

## Part 8: Common Pitfalls and Anti-Patterns

### Over-Engineering: SOLID as a Hammer

The most common pitfall is applying SOLID reflexively to every class, regardless of whether the complexity is warranted. If you have a utility class that formats dates and it will never need to be extended or substituted, wrapping it in an interface and injecting it through DI is unnecessary ceremony.

**Guideline**: Apply SOLID at the boundaries — where your application logic meets external systems (databases, APIs, file systems, message queues). For internal utility code that is unlikely to change, prefer simplicity.

### The "Interface Per Class" Anti-Pattern

Creating an interface for every class, even when only one implementation will ever exist, leads to what some developers call "interface pollution." You end up with pairs of files — `IFooService.cs` and `FooService.cs` — where the interface is an exact copy of the class's public surface.

**Guideline**: Create an interface when you need polymorphism — when you will have multiple implementations, or when you need to substitute a test double. If neither applies, a concrete class is fine.

### Anemic Domain Models

Overly zealous application of SRP can lead to anemic domain models — classes that are pure data containers with no behavior, while all the behavior lives in service classes. This is not inherently wrong, but it can result in procedural code dressed up in object-oriented clothing.

**Guideline**: Some behavior naturally belongs on the domain entity itself. A `Money` class that knows how to add and subtract currencies is not violating SRP — arithmetic on money is that class's single responsibility.

### Circular Dependencies

Applying DIP incorrectly can create circular dependencies. If module A defines an interface that module B implements, but module B also defines an interface that module A implements, you have a cycle.

**Guideline**: Identify which module is the higher-level one (the one with the policy) and let that module own the abstractions. The lower-level module depends on the higher-level module's abstractions, never the reverse.

### Analysis Paralysis

SOLID can lead to analysis paralysis — spending more time designing abstractions than writing code that solves the actual problem. Remember that these are principles, not laws. They exist to serve your codebase, not the other way around.

**Guideline**: Start simple. Write the straightforward solution. When you feel the pain of a SOLID violation — a class that keeps growing, a change that breaks unrelated tests, a type that cannot be substituted — refactor then. This approach is sometimes called "refactoring toward SOLID."

## Part 9: SOLID in the Context of Modern .NET

### Records and Value Objects

C# `record` types naturally support SRP by encouraging small, focused data structures:

```csharp
// Each record has one responsibility: representing a specific concept
public record Money(decimal Amount, string Currency);
public record Address(string Street, string City, string PostalCode, string Country);
public record CustomerName(string First, string Last)
{
    public string FullName => $"{First} {Last}";
}
```

### Pattern Matching and OCP

C# pattern matching can sometimes replace polymorphism for simple cases, but be cautious — a `switch` expression over a discriminated union is fine for a closed set of types, but if the set of types grows over time, polymorphism is more maintainable:

```csharp
// This is fine for a small, stable set of shapes
public decimal CalculateArea(Shape shape) => shape switch
{
    Circle c => Math.PI * c.Radius * c.Radius,
    Rectangle r => r.Width * r.Height,
    Triangle t => 0.5m * t.Base * t.Height,
    _ => throw new ArgumentException($"Unknown shape: {shape.GetType().Name}")
};

// But if new shapes are added frequently, prefer an interface with a method:
public interface IShape
{
    decimal CalculateArea();
}
```

### Minimal APIs and DIP

.NET minimal APIs work naturally with DIP:

```csharp
var builder = WebApplication.CreateBuilder(args);

// Register abstractions
builder.Services.AddScoped<IOrderRepository, PostgresOrderRepository>();
builder.Services.AddScoped<IOrderService, OrderService>();

var app = builder.Build();

// Endpoints depend on abstractions injected by the framework
app.MapPost("/orders", async (CreateOrderRequest request, IOrderService orderService) =>
{
    var result = await orderService.CreateAsync(request);
    return result.IsSuccess ? Results.Created($"/orders/{result.Order!.Id}", result.Order) : Results.BadRequest(result.Error);
});

app.Run();
```

### Source Generators and ISP

Source generators in modern .NET can auto-implement interfaces, reducing the boilerplate of ISP. Libraries like Refit generate HTTP client implementations from interfaces, and EF Core generates much of the repository plumbing. These tools make ISP cheaper to apply in practice.

### Primary Constructors

C# 12 primary constructors reduce the boilerplate of DIP by eliminating explicit field declarations:

```csharp
// Before C# 12
public class OrderService
{
    private readonly IOrderRepository _repository;
    private readonly INotificationService _notifications;
    private readonly ILogger<OrderService> _logger;

    public OrderService(
        IOrderRepository repository,
        INotificationService notifications,
        ILogger<OrderService> logger)
    {
        _repository = repository;
        _notifications = notifications;
        _logger = logger;
    }

    public async Task ProcessAsync(Order order)
    {
        _logger.LogInformation("Processing order {OrderId}", order.Id);
        await _repository.SaveAsync(order);
        await _notifications.SendAsync(order.CustomerEmail, "Confirmed", "...");
    }
}

// C# 12+ with primary constructors
public class OrderService(
    IOrderRepository repository,
    INotificationService notifications,
    ILogger<OrderService> logger) : IOrderService
{
    public async Task ProcessAsync(Order order)
    {
        logger.LogInformation("Processing order {OrderId}", order.Id);
        await repository.SaveAsync(order);
        await notifications.SendAsync(order.CustomerEmail, "Confirmed", "...");
    }
}
```

Primary constructors make DIP feel almost effortless. The dependency injection boilerplate shrinks dramatically while preserving all the benefits of abstraction and testability.

## Part 10: Practical Recommendations

Here is a distilled set of actionable advice for applying SOLID in your day-to-day .NET development:

### When to Apply Each Principle

**SRP**: Apply always. Every class, module, and function should have a clear, singular purpose. This is the easiest principle to apply and the one with the most immediate benefit.

**OCP**: Apply when you see a pattern of repeated modification to a class to support new variants. If a class has been opened and modified three times in the last three months to add a new case to a switch statement, it is time to apply OCP.

**LSP**: Apply whenever you use inheritance. Before creating a subclass, ask: "Can every function that works with the base type work correctly with this subclass?" If the answer is "not without special handling," reconsider the hierarchy.

**ISP**: Apply when you see classes implementing interfaces where some methods throw `NotSupportedException`, return dummy values, or are simply empty. Also apply when changing one method on an interface forces recompilation of clients that do not use that method.

**DIP**: Apply at architectural boundaries — where business logic meets infrastructure. Your domain logic should never directly reference `SqlConnection`, `HttpClient`, `SmtpClient`, or any other infrastructure class.

### The Refactoring Approach

Rather than trying to design a perfectly SOLID system from scratch, follow this iterative approach:

1. **Write the simple, obvious solution.** Do not pre-abstract.
2. **Watch for pain points.** Classes growing too large (SRP). Frequent modifications to add new cases (OCP). Unexpected behavior from subclasses (LSP). Interfaces with methods nobody uses (ISP). Untestable code (DIP).
3. **Refactor to address the specific pain.** Extract a class. Extract an interface. Replace inheritance with composition.
4. **Repeat.** Good design is a living process, not a one-time activity.

### Testing as a SOLID Litmus Test

If your code is hard to test, it almost certainly violates at least one SOLID principle:

- **Hard to instantiate a class?** It probably creates its own dependencies (DIP violation).
- **Need to set up too much state?** The class probably has too many responsibilities (SRP violation).
- **Tests break when unrelated code changes?** Coupling is too high, likely from fat interfaces (ISP violation) or missing abstractions (OCP violation).
- **Mock behaves differently from real implementation?** The inheritance hierarchy might have LSP issues.

Unit testing is both a beneficiary of SOLID design and a diagnostic tool for finding violations.

## Part 11: SOLID Beyond Object-Oriented Programming

While SOLID was articulated for OOP, the underlying ideas transcend paradigm boundaries.

### SRP in Functional Programming

Functions should do one thing. A function that both validates input and transforms data is harder to compose and test than two separate functions. Functional programmers achieve SRP through small, composable functions rather than small classes.

### OCP via Higher-Order Functions

In functional programming, you achieve OCP by passing behavior as arguments (higher-order functions) rather than by subclassing:

```csharp
// OCP via function parameters — the processing logic is open for extension
public static IEnumerable<T> Filter<T>(IEnumerable<T> items, Func<T, bool> predicate)
    => items.Where(predicate);

// Add new filtering behavior without modifying Filter
var expensiveItems = Filter(products, p => p.Price > 100);
var inStockItems = Filter(products, p => p.Stock > 0);
var featuredItems = Filter(products, p => p.IsFeatured);
```

### DIP in Microservices

At the service level, DIP manifests as services depending on contracts (API schemas, message formats, event definitions) rather than on each other's implementations. If Service A publishes an event and Service B consumes it, both depend on the event schema (the abstraction), not on each other's internal code.

## Part 12: Resources and Further Reading

If you want to go deeper into SOLID and related design topics, here are the most authoritative resources:

- **Robert C. Martin, *Agile Software Development: Principles, Patterns, and Practices* (2003)** — The definitive book on SOLID with C++ and Java examples. The 2006 C# edition (with Micah Martin) covers the same material with .NET examples.
- **Robert C. Martin, *Clean Architecture: A Craftsman's Guide to Software Structure and Design* (2018)** — Extends SOLID principles to architectural concerns, with updated thinking on SRP.
- **Bertrand Meyer, *Object-Oriented Software Construction, 2nd Edition* (1997)** — The source of the Open/Closed Principle and Design by Contract. Dense but foundational.
- **Barbara Liskov and Jeannette Wing, *A Behavioral Notion of Subtyping* (1994)** — The formal paper on the Liskov Substitution Principle. Available from Carnegie Mellon's technical reports.
- **Robert C. Martin's original papers** — Available at [butunclebob.com](http://butunclebob.com/ArticleS.UncleBob.PrinciplesOfOod). The original articles on OCP, LSP, DIP, and ISP are short, readable, and illuminating.
- **Microsoft's .NET Architecture Guides** — [docs.microsoft.com/en-us/dotnet/architecture](https://docs.microsoft.com/en-us/dotnet/architecture/) covers clean architecture patterns using SOLID principles with ASP.NET Core.
- **Mark Seemann, *Dependency Injection in .NET* (2019, 2nd Edition)** — Deep dive into DIP and DI patterns specifically in the .NET ecosystem.

## Conclusion

The SOLID principles are not a checklist to be applied mechanically to every class in every project. They are a set of heuristics — mental tools — for recognizing and addressing design problems before they metastasize into unmaintainable code.

Single Responsibility keeps your classes small and focused. Open/Closed lets you add behavior without risking what already works. Liskov Substitution ensures that your inheritance hierarchies are sound and your polymorphism is trustworthy. Interface Segregation prevents your clients from depending on capabilities they do not need. Dependency Inversion decouples your business logic from infrastructure, making your code testable and adaptable.

None of these principles are free. Abstraction has a cost — in indirection, in the number of files to navigate, in the time spent designing interfaces. The art is in knowing when the cost is worth paying. For a throwaway script, it usually is not. For a production system that will be maintained for years, by multiple developers, through changing requirements, it almost always is.

Start simple. Write code that works. Feel the pain when it resists change. Then apply the principle that addresses that specific pain. Over time, this builds an instinct for design that no checklist can replace.
