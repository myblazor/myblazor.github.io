---
title: "The Complete .NET 10 and C# 14 Guide: Everything You Need to Know from Framework to Modern .NET"
date: 2026-04-11
author: myblazor-team
summary: "A comprehensive, from-the-ground-up guide to .NET 10 and C# 14 for developers coming from any .NET background — covering the full history of .NET Framework through modern .NET, every major C# language feature from version 1.0 to 14, the .NET 10 runtime and SDK improvements, ASP.NET Core 10, Blazor, EF Core 10, NativeAOT, the SLNX solution format, file-based apps, and practical migration strategies."
tags:
  - dotnet
  - csharp
  - aspnet
  - blazor
  - deep-dive
  - best-practices
  - performance
  - migration
  - ef-core
---

Picture this. You have been writing C# since the days of Windows Forms and `DataSet`. Your production applications run on .NET Framework 4.8. Your `web.config` files are hundreds of lines long. Your team deploys by copying DLLs to a Windows Server. You have heard the words ".NET Core" and ".NET 5" and ".NET 8" and now ".NET 10" thrown around for years, but the migration always seemed like too much work, too much risk, and frankly too much to learn all at once.

Or maybe you jumped to .NET Core 3.1 a few years ago and have been humming along, but now you look at a C# 14 code sample and see syntax you do not recognize. Extension blocks? The `field` keyword? Implicit span conversions? What happened?

This article is for you. Both of you.

We are going to start from the very beginning — what .NET Framework was, how .NET Core happened, where .NET 5 through .NET 10 fit in the timeline — and then walk through every significant C# language feature from version 1.0 through 14. Not just the new stuff. All of it. Because if you are coming from .NET Framework 4.x, you may have missed C# 8, 9, 10, 11, 12, 13, and 14 in one go, and each of those versions added features that modern .NET code depends on.

Then we will cover the .NET 10 runtime, SDK, ASP.NET Core 10, Blazor, Entity Framework Core 10, and everything else that shipped in November 2025.

Let us get started.

## Part 1: The History of .NET — From Framework to Modern .NET

### The .NET Framework Era (2002–2019)

Microsoft released .NET Framework 1.0 in February 2002. It shipped with C# 1.0 and Visual Studio .NET. The idea was revolutionary at the time: a managed runtime (the Common Language Runtime, or CLR) that handled memory management, type safety, and exception handling, paired with a massive class library (the Base Class Library, or BCL) and a language designed from scratch to be safe, modern, and object-oriented.

Here is the condensed timeline of .NET Framework releases:

- **.NET Framework 1.0** (February 2002): The beginning. C# 1.0, ASP.NET Web Forms, ADO.NET, Windows Forms.
- **.NET Framework 1.1** (April 2003): Minor improvements. C# 1.2. ASP.NET mobile controls.
- **.NET Framework 2.0** (November 2005): A huge leap. C# 2.0 brought generics, nullable types, anonymous methods, iterators, and partial classes. ASP.NET 2.0 added master pages, membership providers, and the `GridView`.
- **.NET Framework 3.0** (November 2006): No new C# version, but three massive frameworks arrived: Windows Presentation Foundation (WPF), Windows Communication Foundation (WCF), and Windows Workflow Foundation (WF). This was also when XAML entered the .NET world.
- **.NET Framework 3.5** (November 2007): C# 3.0 brought LINQ, lambda expressions, extension methods, anonymous types, and automatic properties. This was the release that changed how C# developers think about data access forever.
- **.NET Framework 4.0** (April 2010): C# 4.0 added the `dynamic` keyword, named and optional parameters, and COM interop improvements. The Task Parallel Library (TPL) and `Parallel.ForEach` appeared. The Managed Extensibility Framework (MEF) shipped.
- **.NET Framework 4.5** (August 2012): C# 5.0 brought `async` and `await`. This was another paradigm shift — asynchronous programming went from callback hell to readable, sequential-looking code.
- **.NET Framework 4.6** (July 2015): C# 6.0 brought quality-of-life improvements like string interpolation (`$"Hello {name}"`), null-conditional operators (`?.`), expression-bodied members, `nameof`, and auto-property initializers. The new RyuJIT compiler replaced the older 64-bit JIT.
- **.NET Framework 4.7** (April 2017): Minor runtime improvements. Better support for high-DPI in Windows Forms and WPF.
- **.NET Framework 4.7.1 / 4.7.2** (2017–2018): Continued incremental improvements.
- **.NET Framework 4.8** (April 2019): The final version. Microsoft announced that 4.8 would be the last major release of .NET Framework. It continues to receive security updates as a component of Windows, but no new features will be added. Ever.

Every one of these releases was Windows-only. The runtime was not open source (though reference source was available under a restrictive license). Deployment meant the Global Assembly Cache (GAC), `machine.config`, IIS, and all the ceremony that came with it.

### The .NET Core Revolution (2014–2020)

On November 12, 2014, Microsoft made a stunning announcement: they were building an open-source, cross-platform reimplementation of .NET from scratch. They called it .NET Core. The source code went up on GitHub under the MIT license. Mono creator Miguel de Icaza described it as "a redesigned version of .NET based on the simplified version of the class libraries."

- **.NET Core 1.0** (June 2016): The first release. Lean, cross-platform, but missing many APIs that .NET Framework developers expected. No Windows Forms. No WPF. No `AppDomain`. No `System.Drawing`. Many NuGet packages did not work. It was a brave new world that many teams could not yet migrate to.
- **.NET Core 2.0** (August 2017): A turning point. The `.NET Standard 2.0` specification meant that a huge number of existing NuGet packages worked on .NET Core without changes. The API surface expanded dramatically.
- **.NET Core 2.1** (May 2018): The first Long-Term Support (LTS) release of .NET Core. `Span<T>` appeared, signaling the beginning of the performance revolution. `HttpClientFactory` was introduced.
- **.NET Core 3.0** (September 2019): Windows Forms and WPF came to .NET Core (Windows-only, naturally). C# 8.0 shipped with nullable reference types, async streams, switch expressions, and default interface methods. gRPC support arrived. `System.Text.Json` appeared as an alternative to Newtonsoft.Json.
- **.NET Core 3.1** (December 2019): LTS. The last release to carry the "Core" name.

### The Unified .NET Era (2020–Present)

Starting with .NET 5 in November 2020, Microsoft dropped the "Core" branding and skipped version 4 to avoid confusion with .NET Framework 4.x. The message was clear: there is one .NET going forward.

- **.NET 5** (November 2020): The unification release. C# 9 brought records, top-level statements, init-only setters, and pattern matching improvements. `System.Text.Json` became the default serializer for ASP.NET. Source generators appeared. STS (Standard Term Support — 18 months at the time).
- **.NET 6** (November 2021): LTS. C# 10 brought global usings, file-scoped namespaces, record structs, and constant interpolated strings. Minimal APIs in ASP.NET Core. Hot Reload. .NET MAUI previews. The `DateOnly` and `TimeOnly` types appeared.
- **.NET 7** (November 2022): STS. C# 11 introduced raw string literals, required members, generic math, list patterns, and `file`-scoped types. Native AOT compilation for console apps. Rate limiting middleware in ASP.NET Core.
- **.NET 8** (November 2023): LTS. C# 12 brought primary constructors for classes and structs, collection expressions (`[1, 2, 3]`), default lambda parameters, and `InlineArray`. Blazor United (server + WASM rendering in one project). Native AOT for ASP.NET Core. Aspire for cloud-native orchestration.
- **.NET 9** (November 2024): STS. C# 13 added `params` for any collection type, the `\e` escape sequence, the new `Lock` type, implicit indexer access in object initializers, and `ref struct` support for interfaces. LINQ got `CountBy` and `AggregateBy`. Tensor primitives for AI workloads.
- **.NET 10** (November 11, 2025): LTS. C# 14. The release we are here to talk about in depth. Supported until November 10, 2028.

### What "LTS" and "STS" Mean in Practice

.NET follows a predictable annual release cycle. Every November, a new major version ships. Even-numbered versions are Long-Term Support (LTS) with three years of patches and security updates. Odd-numbered versions are Standard Term Support (STS) — now with two years of support (extended from the original 18 months starting with .NET 9). 

For production applications, the safe bet is to target LTS releases: .NET 6, .NET 8, .NET 10. If you want cutting-edge features and do not mind upgrading annually, STS releases are fine.

As of today, .NET 10.0.5 is the latest patch (released March 12, 2026). .NET 8 and .NET 9 both reach end of support on November 10, 2026. .NET 10 will be supported until November 10, 2028.

## Part 2: The C# Language — Every Major Feature from 1.0 to 13

Before we cover C# 14, let us make sure you are caught up on every significant feature that has been added to the language since its inception. If you have been on .NET Framework 4.8, you are stuck at C# 7.3. That means you have missed seven major language versions. Let us walk through them all.

### C# 1.0 Through 6.0 (The Framework Years)

These are the features most .NET Framework developers know. A quick refresher:

**C# 1.0 (2002):** Classes, structs, interfaces, enums, delegates, events, properties, indexers, `foreach`, garbage collection. The foundation.

**C# 2.0 (2005):** Generics (`List<T>`), nullable value types (`int?`), anonymous methods (`delegate(int x) { return x > 5; }`), iterators (`yield return`), partial classes, static classes, covariance and contravariance for delegates.

**C# 3.0 (2007):** LINQ, lambda expressions (`x => x > 5`), extension methods, anonymous types (`new { Name = "Bob", Age = 42 }`), automatic properties (`public string Name { get; set; }`), object and collection initializers, implicitly typed local variables (`var`), expression trees.

**C# 4.0 (2010):** `dynamic` keyword, named and optional parameters (`void Foo(int x, string y = "default")`), generic covariance and contravariance on interfaces (`IEnumerable<out T>`), improved COM interop.

**C# 5.0 (2012):** `async` and `await`. Caller info attributes (`[CallerMemberName]`, `[CallerFilePath]`, `[CallerLineNumber]`).

**C# 6.0 (2015):** String interpolation (`$"Hello {name}"`), null-conditional operator (`obj?.Property`), expression-bodied members (`public int Area => Width * Height;`), `nameof` operator, auto-property initializers (`public int Count { get; set; } = 0;`), index initializers, `using static`, exception filters (`when`).

### C# 7.x (The Last of .NET Framework)

C# 7.0 shipped with Visual Studio 2017 and was the last major version usable on .NET Framework 4.x (through C# 7.3).

```csharp
// Out variables — declare inline
if (int.TryParse(input, out var number))
{
    Console.WriteLine(number);
}

// Tuples and deconstruction
(string Name, int Age) GetPerson() => ("Alice", 30);
var (name, age) = GetPerson();

// Pattern matching in is/switch
if (shape is Circle c)
{
    Console.WriteLine(c.Radius);
}

switch (shape)
{
    case Circle ci when ci.Radius > 10:
        Console.WriteLine("Big circle");
        break;
    case Rectangle r:
        Console.WriteLine($"{r.Width}x{r.Height}");
        break;
}

// Local functions
int Factorial(int n)
{
    return n <= 1 ? 1 : n * Inner(n - 1);
    
    int Inner(int x) => x <= 1 ? 1 : x * Inner(x - 1);
}

// Ref locals and returns
ref int Find(int[] arr, int target)
{
    for (int i = 0; i < arr.Length; i++)
    {
        if (arr[i] == target)
            return ref arr[i];
    }
    throw new InvalidOperationException("Not found");
}

// Discards
_ = SomeMethodWithReturnValueWeDoNotNeed();

// Digit separators and binary literals
int million = 1_000_000;
int flags = 0b1010_1100;
```

C# 7.1 added `async Main`, `default` literal expressions, and tuple name inference. C# 7.2 added `in` parameters, `ref readonly`, `Span<T>` support, and `private protected`. C# 7.3 added tuple equality, improved pattern matching, and `stackalloc` in more contexts.

**If you are on .NET Framework 4.8, this is where you stopped.** Everything below is new to you.

### C# 8.0 (2019) — Requires .NET Core 3.0+

C# 8 was the first version that could not run on .NET Framework (some features could, but nullable reference types and default interface members required .NET Core 3.0). This was the breaking point.

```csharp
// Nullable reference types
#nullable enable
string? maybeNull = null;
string definitelyNotNull = "hello";

// Switch expressions
string GetQuadrant(Point p) => p switch
{
    { X: > 0, Y: > 0 } => "Q1",
    { X: < 0, Y: > 0 } => "Q2",
    { X: < 0, Y: < 0 } => "Q3",
    { X: > 0, Y: < 0 } => "Q4",
    _ => "Origin or axis"
};

// Using declarations (no braces needed)
using var stream = File.OpenRead("data.bin");
// stream is disposed at the end of the enclosing scope

// Async streams
await foreach (var item in GetItemsAsync())
{
    Console.WriteLine(item);
}

async IAsyncEnumerable<int> GetItemsAsync()
{
    for (int i = 0; i < 10; i++)
    {
        await Task.Delay(100);
        yield return i;
    }
}

// Indices and ranges
int[] arr = [1, 2, 3, 4, 5];
int last = arr[^1];         // 5
int[] slice = arr[1..3];    // [2, 3]

// Null-coalescing assignment
List<int>? list = null;
list ??= new List<int>();

// Default interface methods
public interface ILogger
{
    void Log(string message);
    void LogError(string message) => Log($"ERROR: {message}");
}
```

### C# 9.0 (2020) — .NET 5

```csharp
// Records — immutable reference types with value equality
public record Person(string Name, int Age);

var alice = new Person("Alice", 30);
var alice2 = alice with { Age = 31 }; // Non-destructive mutation

// Top-level statements
// An entire Program.cs can be just:
Console.WriteLine("Hello, World!");

// Init-only setters
public class Config
{
    public string ConnectionString { get; init; } = "";
    public int Timeout { get; init; } = 30;
}

var config = new Config { ConnectionString = "Server=..." };
// config.ConnectionString = "other"; // Compile error!

// Target-typed new
List<Person> people = new();

// Relational and logical patterns
string Classify(int n) => n switch
{
    < 0 => "negative",
    0 => "zero",
    > 0 and <= 100 => "small positive",
    _ => "large positive"
};

// Covariant return types
public class Animal
{
    public virtual Animal Create() => new Animal();
}
public class Dog : Animal
{
    public override Dog Create() => new Dog(); // Returns Dog, not Animal
}

// Static anonymous functions
var square = static (int x) => x * x;
```

### C# 10 (2021) — .NET 6

```csharp
// Global usings (typically in a GlobalUsings.cs file)
global using System;
global using System.Collections.Generic;
global using System.Linq;

// File-scoped namespaces
namespace MyApp.Models;

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
}

// Record structs
public record struct Point(double X, double Y);

// Constant interpolated strings
const string Name = "World";
const string Greeting = $"Hello, {Name}!";

// Extended property patterns
if (person is { Address.City: "Seattle" })
{
    // ...
}

// Lambda improvements: natural type, attributes, return types
var parse = (string s) => int.Parse(s);
var choose = [Obsolete] (bool b) => b ? 1 : 0;
var explicitReturn = object (bool b) => b ? "yes" : "no";
```

### C# 11 (2022) — .NET 7

```csharp
// Raw string literals
string json = """
    {
        "name": "Alice",
        "age": 30
    }
    """;

// Required members
public class User
{
    public required string Email { get; init; }
    public required string Name { get; init; }
}

// var user = new User(); // Compile error: Email and Name are required

// List patterns
int[] numbers = [1, 2, 3, 4, 5];
if (numbers is [1, 2, .. var rest])
{
    Console.WriteLine(rest.Length); // 3
}

// Generic math (static abstract interface members)
T Sum<T>(T[] values) where T : INumber<T>
{
    T result = T.Zero;
    foreach (T value in values)
    {
        result += value;
    }
    return result;
}

// File-scoped types
file class InternalHelper
{
    // Only visible within this file
}

// String interpolation improvements — now works with Span<char>
// UTF-8 string literals
ReadOnlySpan<byte> utf8 = "Hello"u8;

// Newlines in string interpolation expressions
string s = $"Value is {
    SomeMethod()
}";
```

### C# 12 (2023) — .NET 8

```csharp
// Primary constructors for classes and structs
public class UserService(IUserRepository repo, ILogger<UserService> logger)
{
    public User? GetUser(int id)
    {
        logger.LogInformation("Fetching user {Id}", id);
        return repo.FindById(id);
    }
}

// Collection expressions
int[] nums = [1, 2, 3];
List<string> names = ["Alice", "Bob", "Charlie"];
Span<int> span = [10, 20, 30];

// Spread operator in collection expressions
int[] first = [1, 2, 3];
int[] second = [4, 5, 6];
int[] combined = [..first, ..second]; // [1, 2, 3, 4, 5, 6]

// Default lambda parameters
var greet = (string name = "World") => $"Hello, {name}!";
greet();       // "Hello, World!"
greet("Alice"); // "Hello, Alice!"

// Alias any type with using
using Point = (double X, double Y);
using Measurements = double[];

// InlineArray (for runtime/library authors)
[System.Runtime.CompilerServices.InlineArray(10)]
public struct Buffer10
{
    private int _element0;
}

// Experimental attribute
[System.Diagnostics.CodeAnalysis.Experimental("MYLIB001")]
public void BetaFeature() { }
```

### C# 13 (2024) — .NET 9

```csharp
// params for any collection type
public void Log(params ReadOnlySpan<string> messages)
{
    foreach (var msg in messages)
        Console.WriteLine(msg);
}
Log("Error", "Something went wrong", "User: 42");
// Zero allocation — no hidden array created!

// New escape sequence
char escape = '\e'; // U+001B ESCAPE character

// New Lock type
System.Threading.Lock myLock = new();
lock (myLock)
{
    // Uses Lock.EnterScope() — more efficient than Monitor
}

// Implicit indexer access in object initializers
var timer = new System.Timers.Timer
{
    [^1] = 100 // Hypothetical — illustrates the syntax
};

// ref struct interfaces
// ref structs can now implement interfaces (with restrictions)

// Overload resolution priority
[OverloadResolutionPriority(1)]
public void Process(ReadOnlySpan<char> text) { }
public void Process(string text) { }
// The Span overload is now preferred when applicable
```

## Part 3: C# 14 — The Full Feature Tour

C# 14 shipped with .NET 10 on November 11, 2025. It is supported on .NET 10 and later. If your project targets `net10.0`, you get C# 14 automatically. Let us go through every feature.

### Extension Members — The Headline Feature

Since C# 3.0 in 2007, developers have been able to write extension methods — static methods that appear to be instance methods on a type. But you could only write extension methods. Not extension properties. Not extension operators. Not static extension members.

That limitation has finally been removed after over fifteen years of requests. C# 14 introduces **extension members** with a new `extension` block syntax.

Here is the old way (which still works):

```csharp
public static class StringExtensions
{
    public static bool IsNullOrEmpty(this string? value) 
        => string.IsNullOrEmpty(value);
}
```

And here is the new way:

```csharp
public static class StringExtensions
{
    extension(string? value)
    {
        // Instance extension property
        public bool IsNullOrEmpty => string.IsNullOrEmpty(value);
        
        // Instance extension method (new syntax)
        public string Truncate(int maxLength)
            => string.IsNullOrEmpty(value) || value.Length <= maxLength 
                ? value ?? "" 
                : value[..maxLength];
    }
    
    extension(string)
    {
        // Static extension method — appears as string.IsAscii(c)
        public static bool IsAscii(char c) => c <= 0x7F;
    }
}
```

Now you can call these naturally:

```csharp
string? name = GetName();

// Extension property
if (name.IsNullOrEmpty)
    Console.WriteLine("No name provided");

// Extension method (new syntax, same call site)
string shortened = name.Truncate(50);

// Static extension — appears on the type itself
bool ascii = string.IsAscii('A');
```

You can also define extension operators. Imagine you have a `Money` type from a library you do not own:

```csharp
public static class MoneyExtensions
{
    extension(Money m)
    {
        // Extension operator
        public static Money operator +(Money left, Money right)
            => new Money(left.Amount + right.Amount, left.Currency);
    }
}
```

The `extension` block groups all extension members for the same receiver type. You can have multiple blocks in the same class when the receiver types or generic parameters differ. The receiver name (like `value` or `m`) is optional if you only have static extensions.

A few important rules:

1. The old `this` parameter syntax for extension methods continues to work. You do not need to migrate existing code.
2. Extension blocks live inside `static` classes, just like before.
3. If your extension member has the same signature as an actual member on the type, the type's own member wins.
4. You still need the right `using` directive to bring extensions into scope.

### The `field` Keyword

Before C# 14, auto-implemented properties were all-or-nothing. If you wanted to add validation to a setter, you had to create an explicit backing field:

```csharp
// Before C# 14 — verbose
private string _name = "";
public string Name
{
    get => _name;
    set => _name = value ?? throw new ArgumentNullException(nameof(value));
}
```

With C# 14, the `field` keyword lets you access the compiler-generated backing field directly:

```csharp
// C# 14 — concise
public string Name
{
    get;
    set => field = value ?? throw new ArgumentNullException(nameof(value));
}
```

You can provide a body for one or both accessors. The compiler creates the backing field for you, and it is only accessible through the `field` keyword inside the property — not elsewhere in the class. This prevents the common bug of accidentally bypassing property validation by accessing the backing field directly.

This is extremely useful for `INotifyPropertyChanged` implementations:

```csharp
public class ViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    public string Title
    {
        get;
        set
        {
            if (field != value)
            {
                field = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Title)));
            }
        }
    } = "";
}
```

If you already have a member named `field` in your class, you can disambiguate using `@field` to reference the keyword or `this.field` to reference the class member.

### Null-Conditional Assignment

The null-conditional operators `?.` and `?[]` have been read-only since C# 6. You could read through a null chain, but you could not assign through one. C# 14 fixes this:

```csharp
// Before C# 14
if (customer is not null)
{
    customer.Order = GetCurrentOrder();
}

// C# 14
customer?.Order = GetCurrentOrder();

// Works with compound assignment too
customer?.LoyaltyPoints += 100;

// And with indexers
orders?[0] = updatedOrder;
```

This is a small but significant quality-of-life improvement that eliminates a common pattern of null-checking before assignment.

### Implicit Span Conversions

`Span<T>` and `ReadOnlySpan<T>` are central to high-performance .NET code. C# 14 adds implicit conversions between arrays, spans, and read-only spans, making it more natural to work with these types:

```csharp
void ProcessData(ReadOnlySpan<byte> data) 
{
    // ...
}

byte[] buffer = new byte[1024];

// Before C# 14 — explicit conversion needed
ProcessData(buffer.AsSpan());

// C# 14 — implicit conversion
ProcessData(buffer);

// Slicing with ranges also converts implicitly
ProcessData(buffer[..512]);

// Span<T> to ReadOnlySpan<T> is also implicit
Span<byte> mutable = buffer;
ReadOnlySpan<byte> readOnly = mutable; // Implicit in C# 14
```

This matters enormously for library authors and for the runtime itself. The .NET 10 base class libraries use these conversions extensively, which is one reason your code gets faster simply by upgrading — the BCL can use more efficient span-based code paths.

### Lambda Parameter Modifiers

You can now use `ref`, `in`, `out`, and `scoped` modifiers on lambda parameters without specifying the parameter type:

```csharp
// Before C# 14 — had to specify the type
delegate bool TryParse<T>(string input, out T result);
TryParse<int> parser = (string input, out int result) => int.TryParse(input, out result);

// C# 14 — type inferred, modifier still specified
TryParse<int> parser = (input, out result) => int.TryParse(input, out result);
```

### Partial Constructors and Partial Events

C# 13 added partial properties. C# 14 extends this to constructors and events, which is particularly useful for source generators:

```csharp
public partial class ViewModel
{
    // Defining declaration (typically in your code)
    public partial ViewModel(string name);
    
    // Defining declaration for event
    public partial event EventHandler? NameChanged;
}

public partial class ViewModel
{
    // Implementing declaration (typically source-generated)
    public partial ViewModel(string name)
    {
        Name = name;
    }
    
    public partial event EventHandler? NameChanged
    {
        add { /* custom add logic */ }
        remove { /* custom remove logic */ }
    }
}
```

Only the implementing declaration of a partial constructor can include a constructor initializer (`: this()` or `: base()`).

### `nameof` with Unbound Generic Types

Before C# 14, `nameof` required a closed generic type:

```csharp
// Before C# 14
string name = nameof(List<int>); // "List" — but you had to pick a type argument

// C# 14
string name = nameof(List<>);   // "List" — no type argument needed
string name2 = nameof(Dictionary<,>); // "Dictionary"
```

This is useful for logging, diagnostics, and attribute arguments where you want the type name without committing to a specific type argument.

### User-Defined Compound Assignment Operators

Before C# 14, if you defined `operator +` on a type, the compiler would automatically generate `+=` by calling `+` and reassigning. But this creates a temporary object. C# 14 lets you define `+=`, `-=`, `*=`, and other compound assignment operators directly:

```csharp
public struct Vector3
{
    public float X, Y, Z;
    
    // Existing addition operator
    public static Vector3 operator +(Vector3 a, Vector3 b)
        => new(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
    
    // C# 14: User-defined compound assignment — can modify in place
    public static void operator +=(ref Vector3 a, Vector3 b)
    {
        a.X += b.X;
        a.Y += b.Y;
        a.Z += b.Z;
    }
}
```

For value types, this avoids creating a temporary copy. For numerical and vector code, this can be a meaningful performance win.

## Part 4: The .NET 10 Runtime — Performance Without Changing Your Code

One of the most compelling reasons to upgrade to .NET 10 is that your existing code runs faster without any changes. The JIT compiler and runtime received significant improvements.

### JIT Compiler Enhancements

**Struct argument promotion.** When you pass a struct to a method and the calling convention requires members to be in registers, the JIT used to store values to memory first and then load them. In .NET 10, the JIT places struct members directly into registers, eliminating unnecessary memory operations.

```csharp
// This code benefits automatically in .NET 10
public readonly struct Point(double X, double Y);

double Distance(Point a, Point b)
{
    double dx = a.X - b.X;
    double dy = a.Y - b.Y;
    return Math.Sqrt(dx * dx + dy * dy);
}
```

**Array interface devirtualization.** This is a big one. Arrays in .NET implement interfaces like `IList<T>` and `IEnumerable<T>`, but the JIT historically could not devirtualize these interface calls on arrays. In .NET 10, it can. This means that code using `foreach` on arrays via interfaces, or LINQ methods on arrays, gets significantly faster.

```csharp
// This was slower than a manual for loop in .NET 9
// In .NET 10, the JIT devirtualizes the interface calls
int Sum(IEnumerable<int> values)
{
    int total = 0;
    foreach (var v in values)
        total += v;
    return total;
}

int[] numbers = [1, 2, 3, 4, 5];
int result = Sum(numbers); // Much faster in .NET 10
```

**Enhanced loop inversion.** The JIT now uses a graph-based loop recognition algorithm instead of a lexical one. This means more loops are recognized as candidates for optimization (unrolling, cloning, induction variable analysis), and fewer false positives waste compilation time.

**Improved code layout.** The JIT now uses a model based on the asymmetric Travelling Salesman Problem to arrange basic blocks. This increases hot-path density and reduces branch distances, improving instruction cache utilization.

**Conditional escape analysis.** The JIT can now determine that certain objects do not escape from a method even when there are conditional code paths. This enables stack allocation of objects that previously had to be heap-allocated:

```csharp
// In .NET 10, the enumerator can be stack-allocated
// when the JIT determines it doesn't escape
foreach (var item in myReadOnlyCollection)
{
    Process(item);
}
```

### Hardware Acceleration

.NET 10 adds support for AVX10.2 (the latest Intel vector extensions) and ARM64 SVE (Scalable Vector Extensions). This means that SIMD-accelerated code — whether you wrote it explicitly using `Vector<T>` or the runtime does it automatically for things like string operations and array copying — uses the most efficient instructions available on modern hardware.

ARM64 write barrier improvements reduce garbage collection pause times by 8–20% on ARM processors, which matters for cloud workloads running on ARM-based instances (like AWS Graviton or Azure Cobalt).

### NativeAOT Improvements

Native Ahead-of-Time compilation (NativeAOT) produces standalone native executables without requiring the .NET runtime to be installed. In .NET 10:

- The type preinitializer now supports all `conv.*` and `neg` opcodes, allowing more methods to be preinitialized.
- Console apps can natively create container images.
- File-based apps (see the SDK section below) publish in NativeAOT mode by default.
- Binary sizes continue to shrink.
- Android NativeAOT support is nearly production-ready, with developers reporting startup times of 271–331ms compared to 1.3–1.4 seconds with Mono AOT.

### Garbage Collector Improvements

The GC in .NET 10 features improved write barriers that the runtime can dynamically switch between, optimized background collection for reduced fragmentation, and better memory compaction. On x64, the runtime picks the optimal write-barrier implementation based on workload characteristics.

## Part 5: The .NET 10 SDK — A Better Developer Experience

### The SLNX Solution Format

For decades, `.sln` files have been a source of merge conflicts, confusion, and frustration. They use a proprietary text format packed with GUIDs and configuration sections that no human wants to edit by hand.

.NET 10 changes the default. When you run `dotnet new sln`, you now get a `.slnx` file — an XML-based format that is compact, readable, and merge-friendly.

A typical `.sln` file for a three-project solution might be 70+ lines of GUID-laden text. The equivalent `.slnx` is about 10 lines:

```xml
<Solution>
  <Folder Name="/src/">
    <Project Path="src/MyApp.Web/MyApp.Web.csproj" />
    <Project Path="src/MyApp.Core/MyApp.Core.csproj" />
  </Folder>
  <Folder Name="/tests/">
    <Project Path="tests/MyApp.Tests/MyApp.Tests.csproj" />
  </Folder>
</Solution>
```

To migrate an existing solution:

```bash
dotnet sln MyApp.sln migrate
```

This creates a `.slnx` file alongside your existing `.sln`. Validate it, then delete the old file:

```bash
git rm MyApp.sln
git add MyApp.slnx
git commit -m "Migrate to SLNX format"
```

Tooling support is solid: Visual Studio 2022 (17.13+), Visual Studio 2026, JetBrains Rider (2024.3+), VS Code with C# Dev Kit, and the .NET CLI all support `.slnx`. If you need the old format, pass `--format sln` to `dotnet new sln`.

### File-Based Apps

This is one of the most delightful features in .NET 10. You can now run a single `.cs` file directly — no `.csproj`, no `.sln`, no solution structure:

```bash
mkdir hello
cd hello
echo 'Console.WriteLine("Hello from .NET 10!");' > Program.cs
dotnet run
```

That is it. No project file needed. The SDK infers everything. This is perfect for scripts, prototypes, and quick experiments. File-based apps even support `dotnet publish` and default to NativeAOT compilation.

You can add NuGet package references using a special directive syntax at the top of the file:

```csharp
#:package Newtonsoft.Json@13.0.3

var json = Newtonsoft.Json.JsonConvert.SerializeObject(new { Name = "Alice" });
Console.WriteLine(json);
```

### CLI Improvements

The `dotnet` CLI in .NET 10 brings several improvements:

- **Standardized command order**: Arguments and options now follow consistent ordering across all commands.
- **Native tab-completion scripts**: The CLI generates shell-specific completion scripts for bash, zsh, fish, and PowerShell.
- **`dotnet test` with Microsoft.Testing.Platform**: The new testing platform integration is now the default.
- **`dotnet tool exec`**: One-shot tool execution without global or local installation.
- **`--cli-schema`**: Introspection support for tooling and IDE integration.
- **`dotnet package update --vulnerable`**: Updates only packages with known security vulnerabilities to their first secure version.

### Directory.Build.props and Central Package Management

These are not new to .NET 10 but are essential modern .NET practices that many Framework-era developers have not adopted.

`Directory.Build.props` sits at the root of your repository and applies MSBuild properties to every project:

```xml
<Project>
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <AnalysisLevel>latest-recommended</AnalysisLevel>
  </PropertyGroup>
</Project>
```

Central Package Management (`Directory.Packages.props`) pins all NuGet package versions in one place:

```xml
<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
  </PropertyGroup>
  <ItemGroup>
    <PackageVersion Include="Microsoft.Extensions.Logging" Version="10.0.0" />
    <PackageVersion Include="xunit" Version="2.9.3" />
  </ItemGroup>
</Project>
```

Then in each `.csproj`, you reference packages without specifying versions:

```xml
<PackageReference Include="Microsoft.Extensions.Logging" />
```

This eliminates version drift across projects and makes upgrades a single-file change.

## Part 6: ASP.NET Core 10 — Web Development in .NET 10

### Minimal APIs

Minimal APIs, introduced in .NET 6, have matured significantly. In .NET 10, they gain built-in validation support:

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddValidation();

var app = builder.Build();

app.MapPost("/products", (Product product) =>
{
    return Results.Created($"/products/{product.Id}", product);
});

app.Run();

public class Product
{
    public int Id { get; set; }
    
    [System.ComponentModel.DataAnnotations.Required]
    [System.ComponentModel.DataAnnotations.StringLength(100)]
    public string Name { get; set; } = "";
    
    [System.ComponentModel.DataAnnotations.Range(0.01, 99999.99)]
    public decimal Price { get; set; }
}
```

If validation fails, ASP.NET Core automatically returns a `400 Bad Request` with problem details — no additional code needed. The validation framework has moved to a new `Microsoft.Extensions.Validation` package, making it usable outside of ASP.NET Core.

Other minimal API improvements include `PipeReader`-based JSON parsing for better throughput, support for `record` types in `[FromForm]`, Server-Sent Events support for streaming data, and `RedirectHttpResult.IsLocalUrl` for safe redirect validation.

### OpenAPI 3.1

ASP.NET Core 10 now generates OpenAPI 3.1 documents (up from 3.0). The internal OpenAPI.NET library has been updated to version 2.0, bringing YAML output support, improved XML documentation processing, and endpoint-specific transformers.

```csharp
app.MapGet("/weather/{city}", (string city) => 
    new WeatherForecast(city, Random.Shared.Next(-10, 35)))
    .WithOpenApi(); // Generates OpenAPI documentation automatically
```

### Authentication and Security

.NET 10 introduces passkey support for ASP.NET Core Identity. Passkeys use the WebAuthn/FIDO2 standards, enabling fingerprint login, Face ID, and hardware security key authentication without third-party libraries:

```csharp
builder.Services.AddAuthentication()
    .AddIdentityPasskeys();
```

The Blazor Web App template scaffolds the passkey endpoints and UI automatically.

Other security improvements include enhanced OIDC and Microsoft Entra ID integration, encrypted distributed token caching, and Azure Key Vault integration with Azure Managed Identities for data protection.

## Part 7: Blazor in .NET 10

Blazor has received some of the broadest improvements in .NET 10.

### Persistent Component State

The `[PersistentState]` attribute is arguably the most impactful Blazor change. It reduces 25+ lines of manual state serialization code to a single attribute:

```csharp
@page "/weather"

<h1>Weather</h1>

@if (forecasts is null)
{
    <p>Loading...</p>
}
else
{
    @foreach (var f in forecasts)
    {
        <p>@f.Date: @f.TemperatureC°C</p>
    }
}

@code {
    [PersistentState]
    private WeatherForecast[]? forecasts;
    
    protected override async Task OnInitializedAsync()
    {
        // This only runs once — the state is restored after prerendering
        forecasts ??= await Http.GetFromJsonAsync<WeatherForecast[]>("api/weather");
    }
}
```

Before this, you had to manually subscribe to `PersistentComponentState`, serialize state during `OnPersisting`, and restore it during initialization. The `[PersistentState]` attribute handles all of that.

### Reconnection UI

The Blazor Web App template now includes a `ReconnectModal` component with collocated CSS and JavaScript for handling WebSocket disconnections. This replaces the default reconnection UI (which could cause Content Security Policy violations) with a developer-customizable component.

### WebAssembly Improvements

- **Preloading**: Framework static assets are preloaded, reducing initial load times significantly for data-heavy applications.
- **Fingerprinted Blazor script**: The `blazor.web.js` script is now served as a static web asset with automatic compression and fingerprinting, improving caching.
- **Better diagnostics**: Runtime performance profiling is now available for Blazor WebAssembly.
- **Hot Reload improvements**: More reliable hot reload during development.

### QuickGrid Enhancements

The `QuickGrid` component (Blazor's built-in data grid) gains `RowClass` for conditional row styling:

```csharp
<QuickGrid Items="items" RowClass="GetRowCssClass">
    <PropertyColumn Property="@(p => p.Name)" Title="Name" />
    <PropertyColumn Property="@(p => p.Status)" Title="Status" />
</QuickGrid>

@code {
    private string? GetRowCssClass(OrderItem item)
    {
        return item.Status == "Cancelled" ? "cancelled-row" : null;
    }
}
```

### JavaScript Interop

New APIs for invoking JavaScript constructors and accessing properties directly from .NET. Support for referencing JavaScript functions via `IJSObjectReference` has been expanded.

## Part 8: Entity Framework Core 10

EF Core 10 ships as an LTS release alongside .NET 10.

### Vector Search Support

For AI workloads, EF Core 10 supports the new `vector` data type and `VECTOR_DISTANCE()` function in SQL Server 2025 and Azure SQL Database:

```csharp
var similar = await context.Products
    .OrderBy(p => EF.Functions.VectorDistance(p.Embedding, queryVector))
    .Take(10)
    .ToListAsync();
```

### JSON Data Type

When targeting SQL Server 2025 with compatibility level 170+, EF Core automatically uses the native `json` type instead of storing JSON in `nvarchar` columns. This provides better performance and data validation.

### Named Query Filters

You can now define multiple named filters per entity type and selectively disable them:

```csharp
modelBuilder.Entity<Blog>()
    .HasQueryFilter("SoftDelete", b => !b.IsDeleted)
    .HasQueryFilter("Tenant", b => b.TenantId == currentTenantId);

// Query that ignores soft delete but keeps tenant filter
var all = await context.Blogs
    .IgnoreQueryFilters(["SoftDelete"])
    .ToListAsync();
```

### LINQ Improvements

**Left and Right Joins:**

```csharp
var results = context.Students
    .LeftJoin(
        context.Departments,
        s => s.DepartmentId,
        d => d.Id,
        (student, department) => new 
        { 
            student.Name, 
            Department = department.Name ?? "[None]" 
        });
```

**Conditional ExecuteUpdateAsync:**

```csharp
await context.Blogs.ExecuteUpdateAsync(s =>
{
    s.SetProperty(b => b.Views, 0);
    if (resetNames)
        s.SetProperty(b => b.Name, "Default");
});
```

### Full-Text and Hybrid Search

```csharp
var results = context.Articles
    .Where(a => EF.Functions.FullTextContains(a.Content, "performance"))
    .OrderByDescending(a => EF.Functions.FullTextScore(a.Content, "performance"))
    .ToListAsync();
```

Hybrid search combines vector similarity with full-text search using the RRF (Reciprocal Rank Fusion) function.

## Part 9: .NET Libraries — What Is New in the BCL

### Post-Quantum Cryptography

With quantum computing advancing, .NET 10 expands post-quantum cryptography (PQC) support:

- **ML-DSA** (Module-Lattice Digital Signature Algorithm): For quantum-resistant digital signatures.
- **ML-KEM** (Module-Lattice Key Encapsulation Mechanism): For quantum-resistant key exchange.
- **Composite ML-DSA**: Hybrid approaches combining traditional and quantum-resistant algorithms.
- Windows CNG support for these algorithms.

```csharp
using System.Security.Cryptography;

// ML-DSA signing
using var mldsa = MLDsa.GenerateKey(MLDsaAlgorithm.MLDsa65);
byte[] signature = mldsa.SignData(data);
bool valid = mldsa.VerifyData(data, signature);
```

### JSON Serialization

`System.Text.Json` gains several options:

```csharp
var options = new JsonSerializerOptions
{
    // Disallow duplicate property names in deserialization
    AllowDuplicateProperties = false,
    
    // Strict mode — all required properties must be present
    UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
};

// PipeReader support for streaming deserialization
var result = await JsonSerializer.DeserializeAsync<MyType>(pipeReader, options);
```

### Collections

`OrderedDictionary<TKey, TValue>` now has additional APIs. `ISOWeek` date utilities have been added. `CompareOptions.NumericOrdering` enables natural sort order (so "file2" sorts before "file10").

### ZIP Archives

`ZipArchive` now uses lazy entry loading for better performance with large archives.

### Networking

`WebSocketStream` provides a `Stream`-based API over WebSockets, simplifying integration with stream-based APIs. TLS 1.3 support is now available on macOS.

## Part 10: Migrating from .NET Framework — A Practical Roadmap

If you are on .NET Framework 4.8, the migration to .NET 10 is a significant but well-understood process. Here is a practical sequence.

### Step 1: Assess Your Dependencies

Use the .NET Portability Analyzer or `try-convert` tool to scan your projects. Common blockers:

- **WCF**: Use CoreWCF (community-supported) or switch to gRPC.
- **`System.Web`**: There is no equivalent. ASP.NET Core is a rewrite, not a port.
- **`AppDomain`**: Not supported. Use `AssemblyLoadContext` instead.
- **`System.Drawing`**: Use `System.Drawing.Common` (Windows-only) or `SkiaSharp` (cross-platform).
- **Windows Registry**: Use `Microsoft.Win32.Registry` NuGet package.
- **COM Interop**: Still works but may need adjustments.

### Step 2: Modernize Your Project Files

Convert from the old verbose `.csproj` format to SDK-style:

```xml
<!-- Old format (hundreds of lines) -->
<?xml version="1.0" encoding="utf-8"?>
<Project ToolsVersion="15.0" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
  <Import Project="$(MSBuildExtensionsPath)\$(MSBuildToolsVersion)\Microsoft.Common.props" />
  <PropertyGroup>
    <Configuration Condition=" '$(Configuration)' == '' ">Debug</Configuration>
    <!-- ... 50 more lines ... -->
  </PropertyGroup>
  <ItemGroup>
    <Reference Include="System" />
    <Reference Include="System.Core" />
    <!-- ... every file listed individually ... -->
  </ItemGroup>
  <!-- ... -->
</Project>
```

```xml
<!-- New SDK-style format -->
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
  </PropertyGroup>
</Project>
```

The SDK-style format uses file globbing (all `.cs` files are included automatically), eliminates boilerplate, and supports multi-targeting.

### Step 3: Multi-Target During Transition

If you have shared libraries, you can target both frameworks simultaneously:

```xml
<PropertyGroup>
  <TargetFrameworks>net48;net10.0</TargetFrameworks>
</PropertyGroup>
```

Use `#if` directives for framework-specific code:

```csharp
#if NET10_0_OR_GREATER
    await using var connection = new SqlConnection(connectionString);
#else
    using var connection = new SqlConnection(connectionString);
#endif
```

### Step 4: Adopt Modern Patterns Incrementally

You do not have to rewrite everything at once. Start with:

1. Enable nullable reference types (`<Nullable>enable</Nullable>`).
2. Add `global using` statements to reduce `using` noise.
3. Convert classes to file-scoped namespaces.
4. Replace `Newtonsoft.Json` with `System.Text.Json` where practical.
5. Use `ILogger<T>` and the built-in dependency injection container.
6. Replace `HttpWebRequest` with `HttpClient` and `IHttpClientFactory`.

### Step 5: The ASP.NET Migration

This is the hardest part. ASP.NET (Framework) and ASP.NET Core are fundamentally different frameworks:

- `Global.asax` → `Program.cs` with the host builder pattern
- `web.config` → `appsettings.json` + environment variables
- `System.Web.HttpContext` → `Microsoft.AspNetCore.Http.HttpContext`
- `HttpModule` / `HttpHandler` → Middleware
- `MVC filters` → Still exist but the pipeline is different
- `System.Web.Routing` → Endpoint routing

Microsoft provides the [.NET Upgrade Assistant](https://dotnet.microsoft.com/en-us/platform/upgrade-assistant) tool that automates many of these transformations.

## Part 11: Modern .NET Project Structure — Best Practices

Here is a recommended project structure for a .NET 10 application:

```
MyApp/
├── MyApp.slnx                          # SLNX solution file
├── Directory.Build.props               # Shared MSBuild properties
├── Directory.Packages.props            # Central Package Management
├── global.json                         # Pin SDK version
├── .editorconfig                       # Code style rules
├── src/
│   ├── MyApp.Web/
│   │   ├── MyApp.Web.csproj
│   │   ├── Program.cs
│   │   ├── Pages/
│   │   └── Components/
│   ├── MyApp.Core/
│   │   ├── MyApp.Core.csproj
│   │   ├── Models/
│   │   ├── Services/
│   │   └── Interfaces/
│   └── MyApp.Infrastructure/
│       ├── MyApp.Infrastructure.csproj
│       ├── Data/
│       └── Repositories/
├── tests/
│   ├── MyApp.Unit.Tests/
│   │   └── MyApp.Unit.Tests.csproj
│   └── MyApp.Integration.Tests/
│       └── MyApp.Integration.Tests.csproj
└── tools/
    └── MyApp.ContentProcessor/
        └── MyApp.ContentProcessor.csproj
```

A good `global.json`:

```json
{
  "sdk": {
    "version": "10.0.100",
    "rollForward": "latestMinor"
  }
}
```

## Part 12: Testing in .NET 10

### xUnit v3

xUnit v3, the latest version, integrates with the `Microsoft.Testing.Platform` which is now the default in .NET 10's `dotnet test` command. Key improvements include parallel test execution by default, better test discovery, and first-class support for async tests.

```csharp
using Xunit;

namespace MyApp.Tests;

public class CalculatorTests
{
    [Fact]
    public void Add_TwoPositiveNumbers_ReturnsSum()
    {
        var calc = new Calculator();
        Assert.Equal(5, calc.Add(2, 3));
    }
    
    [Theory]
    [InlineData(0, 0, 0)]
    [InlineData(-1, 1, 0)]
    [InlineData(int.MaxValue, 1, unchecked(int.MaxValue + 1))]
    public void Add_VariousInputs_ReturnsExpected(int a, int b, int expected)
    {
        var calc = new Calculator();
        Assert.Equal(expected, calc.Add(a, b));
    }
}
```

### bUnit for Blazor

For Blazor component testing, bUnit remains the standard. It works with .NET 10 and xUnit v3:

```csharp
using Bunit;
using Xunit;

public class CounterTests : TestContext
{
    [Fact]
    public void Counter_IncrementsOnClick()
    {
        var cut = RenderComponent<Counter>();
        
        cut.Find("button").Click();
        
        cut.Find("p").MarkupMatches("<p>Current count: 1</p>");
    }
}
```

### Architecture Testing with NetArchTest

For enforcing architectural rules:

```csharp
[Fact]
public void Domain_ShouldNotDependOn_Infrastructure()
{
    var result = Types.InAssembly(typeof(Order).Assembly)
        .ShouldNot()
        .HaveDependencyOn("MyApp.Infrastructure")
        .GetResult();
    
    Assert.True(result.IsSuccessful);
}
```

## Part 13: OpenTelemetry and Observability

.NET 10 continues to invest in OpenTelemetry as the standard for observability. ASP.NET Core 10 includes new Identity-specific metrics for user management and login tracking.

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Logging.AddOpenTelemetry(logging =>
{
    logging.IncludeFormattedMessage = true;
    logging.IncludeScopes = true;
});

builder.Services.AddOpenTelemetry()
    .WithTracing(tracing =>
    {
        tracing
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddEntityFrameworkCoreInstrumentation()
            .AddOtlpExporter();
    })
    .WithMetrics(metrics =>
    {
        metrics
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddRuntimeInstrumentation()
            .AddOtlpExporter();
    });
```

## Part 14: Common Pitfalls When Upgrading to .NET 10

### Breaking Changes to Watch For

1. **SLNX default**: `dotnet new sln` now creates `.slnx` files. If your CI/CD pipeline hardcodes `.sln`, update it. Pass `--format sln` if you need the old format.

2. **EF Core 10 runs exclusively on .NET 10.** Unlike EF Core 9 (which worked on .NET 8 and 9), EF Core 10 requires .NET 10.

3. **Cookie login redirects disabled for APIs.** ASP.NET Core 10 no longer redirects to a login page for API endpoints — they return `401`/`403` directly. This is the correct behavior but may surprise applications that relied on the redirect.

4. **`IActionContextAccessor` is obsolete.** If you are using this in MVC controllers, migrate to alternatives.

5. **Default container images now use Ubuntu.** If you had Debian-specific scripts in your Dockerfiles, they may need updating.

6. **`WithOpenApi()` extension method deprecated.** Use the updated OpenAPI generator features instead.

7. **SQLite date/time parsing changes.** Applications using SQLite with date/time parsing or `REAL` timestamp storage should test thoroughly.

8. **ICU environment variable renamed.** The environment variable for controlling ICU globalization behavior has been renamed.

9. **Single-file apps no longer probe the executable directory for native libs.** The `DllImport` search path has been tightened.

### The `field` Keyword Naming Conflict

If you have a variable, field, or property named `field` in a class, the new contextual keyword may shadow it inside property accessors. The compiler warns about this. Use `@field` to reference the keyword or `this.field` to reference the class member:

```csharp
public class Example
{
    private int field = 42; // Existing member named 'field'
    
    public string Data
    {
        get;
        set
        {
            @field = value;         // Backing field (the keyword)
            Console.WriteLine(this.field); // The class member named 'field'
        }
    }
}
```

## Part 15: What Is Ahead — .NET 11 and Beyond

.NET 11 is scheduled for November 2026. It will be an STS release with two years of support. C# 15 is already being discussed, with potential features including:

- Discriminated unions / algebraic data types
- Null-conditional `await` (`await?`)
- Further extension member capabilities
- More pattern matching enhancements

The .NET roadmap is publicly available on GitHub. The team publishes design proposals for C# features at the [dotnet/csharplang](https://github.com/dotnet/csharplang) repository, and you can follow (or participate in) the design process.

## Part 16: Practical Recommendations

### If You Are on .NET Framework 4.8

1. Start today. .NET Framework 4.8 receives only security patches. Every month you wait makes the gap wider.
2. Use the .NET Upgrade Assistant for automated conversion.
3. Target .NET 10 directly — do not stop at .NET 6 or .NET 8.
4. Migrate one project at a time, starting with class libraries.
5. Invest time in learning nullable reference types, `async`/`await` best practices, and dependency injection.

### If You Are on .NET 6 or .NET 8

1. Upgrade to .NET 10 — it is the current LTS and you get three years of support.
2. Both .NET 6 and .NET 8 reach end of support on November 10, 2026.
3. The upgrade is straightforward: update your TFM to `net10.0`, update NuGet packages, and fix any breaking changes (there are very few between .NET 8 and .NET 10).
4. Start adopting C# 14 features incrementally — the `field` keyword and extension properties are the highest-value additions for most codebases.

### If You Are Starting a New Project

1. Use .NET 10 with C# 14.
2. Use the SLNX solution format.
3. Set up `Directory.Build.props` and `Directory.Packages.props` from day one.
4. Enable nullable reference types, implicit usings, and `TreatWarningsAsErrors`.
5. Use Minimal APIs for web projects unless you specifically need MVC's controller pattern.
6. Set up OpenTelemetry for logging, tracing, and metrics from the start.
7. Write tests alongside your code using xUnit v3 and bUnit.

## Resources

Here are the official sources to go deeper on everything covered in this article:

- [What's new in .NET 10](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-10/overview) — The official overview from Microsoft.
- [What's new in C# 14](https://learn.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-14) — The official C# 14 feature documentation.
- [Introducing C# 14](https://devblogs.microsoft.com/dotnet/introducing-csharp-14/) — The .NET blog announcement with detailed examples.
- [Performance Improvements in .NET 10](https://devblogs.microsoft.com/dotnet/performance-improvements-in-net-10/) — Stephen Toub's legendary deep-dive into every performance improvement.
- [Announcing .NET 10](https://devblogs.microsoft.com/dotnet/announcing-dotnet-10/) — The official release announcement.
- [What's new in ASP.NET Core 10](https://learn.microsoft.com/en-us/aspnet/core/release-notes/aspnetcore-10.0) — Complete ASP.NET Core 10 feature list.
- [What's new in EF Core 10](https://learn.microsoft.com/en-us/ef/core/what-is-new/ef-core-10.0/whatsnew) — EF Core 10 features and improvements.
- [C# Language Version History](https://learn.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-version-history) — Complete history of every C# version and its features.
- [.NET Support Policy](https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core) — Official LTS/STS support timelines.
- [.NET Upgrade Assistant](https://dotnet.microsoft.com/en-us/platform/upgrade-assistant) — Automated migration tool for .NET Framework to modern .NET.
- [SLNX Support in the .NET CLI](https://devblogs.microsoft.com/dotnet/introducing-slnx-support-dotnet-cli/) — Official blog post on the new solution format.
- [.NET 10 Download Page](https://dotnet.microsoft.com/en-us/download/dotnet/10.0) — Download the SDK and runtime.
