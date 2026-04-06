---
title: "The Liskov Substitution Principle: A Complete Guide for .NET Developers"
date: 2026-04-03
author: myblazor-team
summary: "A deep dive into the Liskov Substitution Principle — from Barbara Liskov's 1987 keynote to practical C# code, real-world violations, design-by-contract rules, and strategies for writing substitutable types in modern .NET."
tags:
  - solid
  - design-principles
  - csharp
  - dotnet
  - object-oriented-programming
  - deep-dive
---

Picture this: it is a quiet Wednesday afternoon. You are working on a payment processing system. The team lead merged a pull request last week that introduced a new `ExpressPayment` class inheriting from `Payment`. Everything compiled. The unit tests passed. The code review looked clean. And now, three days later, production is throwing `NotSupportedException` in a code path that has worked flawlessly for two years. The new subclass broke a contract that the base class had promised. The caller never expected it. The monitoring dashboard is red. Your on-call phone is buzzing.

You have just been bitten by a violation of the Liskov Substitution Principle.

The Liskov Substitution Principle — the "L" in SOLID — is arguably the most misunderstood and the most consequential of the five principles. It is the principle that separates an inheritance hierarchy that *works* from one that is a ticking time bomb. It is the principle that explains why a `Square` is not a `Rectangle`, why a `ReadOnlyCollection` should not inherit from `List<T>`, and why your carefully designed plugin architecture falls apart every time someone writes a new adapter.

This article is going to take you through the entire story — from the academic origins at OOPSLA 1987 to the practical rules you should apply in your C# code today. We will examine real violations, write real fixes, explore the relationship between LSP and Design by Contract, and end with a checklist you can pin to your wall.

Let us begin.

## Part 1: Origins — Barbara Liskov and the Birth of a Principle

To understand the Liskov Substitution Principle, you need to understand the person behind it.

Barbara Liskov was born in 1939 in Los Angeles. She earned her bachelor's degree in mathematics from UC Berkeley in 1961, then worked at the Mitre Corporation before returning to academia. In 1968, she became one of the first women in the United States to earn a PhD in computer science, from Stanford, under the supervision of John McCarthy — the father of artificial intelligence. Her thesis was on chess endgame programs, and during that work she developed the killer heuristic, a technique still used in game tree search algorithms.

After Stanford, Liskov joined MIT in 1972, where she led the design and implementation of the CLU programming language. CLU was groundbreaking. It introduced concepts that are foundational to every language you use today: data abstraction, encapsulation, iterators, parametric polymorphism, and exception handling. If you have ever written a `foreach` loop, you owe a debt to CLU. If you have ever defined an interface, you are working in an intellectual tradition that traces back to Liskov's research group at MIT in the 1970s.

In 1987, Liskov delivered a keynote address at OOPSLA (the Object-Oriented Programming, Systems, Languages, and Applications conference) titled *Data Abstraction and Hierarchy*. In that talk, she presented an informal rule about when one type can safely stand in for another:

> What is wanted here is something like the following substitution property: If for each object o1 of type S there is an object o2 of type T such that for all programs P defined in terms of T, the behavior of P is unchanged when o1 is substituted for o2, then S is a subtype of T.

This is the original formulation. It is deliberately informal — Liskov herself later called it an "informal rule." The key insight is deceptively simple: if your code works with a base type, it should continue to work when you hand it a derived type. No surprises. No exceptions. No "well, except when..."

Seven years later, in 1994, Liskov and Jeannette Wing published a rigorous formalization in their paper *A Behavioral Notion of Subtyping* in ACM Transactions on Programming Languages and Systems. This paper introduced the history constraint (sometimes called the "history rule"), which addresses what happens when a subtype adds new methods that can mutate state in ways the supertype never allowed. This was the key innovation beyond Bertrand Meyer's earlier Design by Contract work.

In 2000, Robert C. Martin published his paper *Design Principles and Design Patterns*, which collected five object-oriented design principles. Around 2004, Michael Feathers coined the SOLID acronym to make them memorable. The "L" stands for Liskov Substitution.

In 2008, Barbara Liskov received the Turing Award — the highest honor in computer science — for her contributions to programming language and system design, especially related to data abstraction, fault tolerance, and distributed computing.

### Why This History Matters

You might be wondering why we are spending time on history in a programming article. Here is why: the Liskov Substitution Principle is not a style preference. It is not a "clean code" guideline that you can take or leave. It is a mathematically grounded property of type systems. When you violate it, you break the fundamental contract that makes polymorphism work. Understanding that it comes from the same intellectual tradition as data abstraction, formal verification, and type theory helps you take it seriously — and helps you understand *why* certain designs fail.

## Part 2: The Principle in Plain Language

Let us strip away the formal notation and state the principle as simply as possible.

**If you have code that works correctly with a base type, it must also work correctly with any subtype of that base type, without the calling code needing to know or care which subtype it received.**

That is the entire principle. Everything else — preconditions, postconditions, invariants, the history rule — is a consequence of this one requirement.

Think of it like a vending machine. The machine's contract says: "Insert a coin, press a button, receive a drink." If you insert a US quarter, it works. If you insert a Canadian quarter (same size, same shape), it should also work — because the machine's contract is defined in terms of "a coin of this size and weight," not "a US quarter specifically." But if you insert a wooden token that is the same size but does not conduct electricity for the coin sensor, the machine jams. The wooden token *looks* like a valid substitution from the outside, but it violates the behavioral contract.

LSP is about behavioral compatibility, not just structural compatibility. A type can implement all the same methods, have all the same properties, and still violate LSP if its *behavior* breaks the expectations of code written against the base type.

### The Three Levels of Substitutability

It helps to think about substitutability at three increasingly strict levels:

**Level 1: Syntactic substitutability.** The subtype compiles wherever the base type is expected. In C#, this is enforced by the compiler. If `Dog` inherits from `Animal`, you can pass a `Dog` to any method that accepts an `Animal`. This is necessary but not sufficient for LSP.

**Level 2: Semantic substitutability.** The subtype behaves correctly wherever the base type is expected. Methods return meaningful results, state transitions are valid, and no unexpected exceptions are thrown. This is what LSP demands.

**Level 3: Behavioral equivalence.** The subtype behaves *identically* to the base type. This is actually too strong — LSP does not require identical behavior. A `SortedList<T>` does not behave identically to `List<T>` (it maintains sorted order), but it can still be a valid behavioral subtype if the base type's contract does not specify insertion order.

The sweet spot — and the requirement of LSP — is Level 2. Subtypes must honor the contracts of their base types while being free to extend them in compatible ways.

## Part 3: The Formal Rules — Contracts, Preconditions, and the History Constraint

The Liskov Substitution Principle can be decomposed into a set of concrete rules. These rules are drawn from Liskov and Wing's 1994 paper and from Bertrand Meyer's Design by Contract methodology. Understanding each one will let you mechanically check whether a given inheritance relationship is valid.

### Rule 1: Contravariance of Preconditions

**A subtype must not strengthen preconditions.**

A precondition is a condition that must be true before a method can be called. If the base class method accepts any positive integer, the subtype method must also accept any positive integer. It may accept *more* (like zero or negative integers), but it must not accept *less*.

Here is a violation in C#:

```csharp
public class BaseProcessor
{
    public virtual void Process(int value)
    {
        // Accepts any integer
        Console.WriteLine($"Processing {value}");
    }
}

public class StrictProcessor : BaseProcessor
{
    public override void Process(int value)
    {
        // VIOLATION: Strengthened precondition
        if (value < 0)
            throw new ArgumentOutOfRangeException(
                nameof(value), "Value must be non-negative");

        Console.WriteLine($"Strictly processing {value}");
    }
}
```

Code written against `BaseProcessor` legitimately passes `-5` and expects it to work. `StrictProcessor` blows up. That is an LSP violation.

The fix is to either relax the precondition or restructure the hierarchy so that `StrictProcessor` does not inherit from `BaseProcessor`:

```csharp
public interface IProcessor
{
    void Process(int value);
}

public class GeneralProcessor : IProcessor
{
    public void Process(int value)
    {
        Console.WriteLine($"Processing {value}");
    }
}

public class NonNegativeProcessor : IProcessor
{
    // The interface contract now explicitly documents
    // what each implementation accepts
    public void Process(int value)
    {
        if (value < 0)
            throw new ArgumentOutOfRangeException(
                nameof(value), "Value must be non-negative");

        Console.WriteLine($"Strictly processing {value}");
    }
}
```

Now neither class claims to substitute for the other. They both implement a shared interface, and the caller chooses based on their needs.

### Rule 2: Covariance of Postconditions

**A subtype must not weaken postconditions.**

A postcondition is a guarantee about what is true after a method returns. If the base class method guarantees that the return value is non-null, the subtype must also return non-null. The subtype may strengthen the postcondition (e.g., guarantee the return value is also non-empty), but it must not weaken it.

```csharp
public class DataFetcher
{
    public virtual IReadOnlyList<string> FetchRecords()
    {
        // Postcondition: always returns a non-null list
        return new List<string> { "default" };
    }
}

public class LazyDataFetcher : DataFetcher
{
    public override IReadOnlyList<string>? FetchRecords()
    {
        // VIOLATION: Can return null, weakening the postcondition
        // (In practice, C# nullable reference types would catch this,
        // but the principle applies regardless of language features)
        return null;
    }
}
```

Any caller that trusts the base class contract and writes `var count = fetcher.FetchRecords().Count;` will get a `NullReferenceException`. The postcondition was weakened.

### Rule 3: Invariant Preservation

**A subtype must preserve all invariants of the base type.**

An invariant is a condition that is always true for an object throughout its lifetime. If the base class guarantees that `Balance >= 0` at all times, every subtype must also maintain `Balance >= 0` at all times.

```csharp
public class BankAccount
{
    public decimal Balance { get; protected set; }

    public BankAccount(decimal initialBalance)
    {
        if (initialBalance < 0)
            throw new ArgumentException("Initial balance must be non-negative");
        Balance = initialBalance;
    }

    // Invariant: Balance >= 0
    public virtual void Withdraw(decimal amount)
    {
        if (amount > Balance)
            throw new InvalidOperationException("Insufficient funds");
        Balance -= amount;
    }
}

public class OverdraftAccount : BankAccount
{
    public decimal OverdraftLimit { get; }

    public OverdraftAccount(decimal initialBalance, decimal overdraftLimit)
        : base(initialBalance)
    {
        OverdraftLimit = overdraftLimit;
    }

    public override void Withdraw(decimal amount)
    {
        // VIOLATION: Allows Balance to go negative,
        // breaking the base class invariant
        if (amount > Balance + OverdraftLimit)
            throw new InvalidOperationException("Exceeds overdraft limit");
        Balance -= amount;
    }
}
```

Code that depends on the `BankAccount` invariant (`Balance >= 0`) will produce incorrect results when handed an `OverdraftAccount`. For example, a report that calculates "accounts with zero balance" by checking `account.Balance == 0` will miss overdrafted accounts entirely.

The fix depends on your domain. One approach: do not make `OverdraftAccount` inherit from `BankAccount`. Instead, define a more general `IAccount` interface whose contract does not promise non-negative balances, and let each implementation document its own invariants.

```csharp
public interface IAccount
{
    decimal Balance { get; }
    void Withdraw(decimal amount);
    // Contract: Withdraw throws if amount exceeds
    // the account's available funds (definition varies by type)
}

public class StandardAccount : IAccount
{
    public decimal Balance { get; private set; }

    public StandardAccount(decimal initialBalance)
    {
        if (initialBalance < 0)
            throw new ArgumentException("Must be non-negative");
        Balance = initialBalance;
    }

    public void Withdraw(decimal amount)
    {
        if (amount > Balance)
            throw new InvalidOperationException("Insufficient funds");
        Balance -= amount;
    }
}

public class OverdraftAccount : IAccount
{
    public decimal Balance { get; private set; }
    public decimal OverdraftLimit { get; }

    public OverdraftAccount(decimal initialBalance, decimal overdraftLimit)
    {
        Balance = initialBalance;
        OverdraftLimit = overdraftLimit;
    }

    public void Withdraw(decimal amount)
    {
        if (amount > Balance + OverdraftLimit)
            throw new InvalidOperationException("Exceeds overdraft limit");
        Balance -= amount;
    }
}
```

### Rule 4: The History Constraint

**A subtype must not allow state changes that the base type's contract forbids.**

This is the rule that Liskov and Wing added in their 1994 paper, and it is the one most developers have never heard of. It says: if the base type is immutable, a subtype must also be immutable (at least from the perspective of the base type's interface). If the base type's specification says a property can only increase, the subtype must not allow it to decrease.

The classic example: an immutable point and a mutable point.

```csharp
public class ImmutablePoint
{
    public int X { get; }
    public int Y { get; }

    public ImmutablePoint(int x, int y)
    {
        X = x;
        Y = y;
    }
}

public class MutablePoint : ImmutablePoint
{
    // VIOLATION: Adds mutation capability that contradicts
    // the base class's immutability contract
    public new int X { get; set; }
    public new int Y { get; set; }

    public MutablePoint(int x, int y) : base(x, y)
    {
        X = x;
        Y = y;
    }

    public void MoveTo(int newX, int newY)
    {
        X = newX;
        Y = newY;
    }
}
```

Code that stores an `ImmutablePoint` in a dictionary as a key (relying on the fact that `X` and `Y` will never change, and therefore the hash code is stable) will corrupt the dictionary if a `MutablePoint` sneaks in and then gets mutated. The history constraint says this inheritance relationship is invalid because the subtype introduces state transitions that the base type's history forbids.

### Rule 5: Exception Compatibility

**A subtype must not throw new exceptions that the base type's contract does not permit.**

If the base class method is documented to throw `ArgumentException` on invalid input and `IOException` on I/O failure, a subtype should not introduce `SecurityException` or `NotImplementedException`. The calling code is prepared to handle certain exceptions; introducing new ones breaks the contract.

```csharp
public abstract class FileStore
{
    /// <summary>
    /// Saves data to the store.
    /// Throws IOException if the write fails.
    /// Throws ArgumentNullException if data is null.
    /// </summary>
    public abstract void Save(byte[] data);
}

public class EncryptedFileStore : FileStore
{
    public override void Save(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);

        // VIOLATION: Throws an exception type the base
        // class contract never mentioned
        throw new CryptographicException(
            "Encryption key not configured");
    }
}
```

The fix: either make `CryptographicException` inherit from `IOException` (not ideal), or document the base class contract to allow for more general exceptions, or handle the encryption setup in the constructor so `Save` never encounters this state.

### Signature Rules

In addition to the behavioral rules above, LSP also implies structural rules at the type level. C# enforces most of these automatically:

**Contravariance of method parameter types in the subtype.** If the base method accepts `Animal`, the override should accept `Animal` or a more general type. C# method overriding requires exact parameter type matches, so this is enforced by the compiler.

**Covariance of method return types in the subtype.** If the base method returns `Animal`, the override may return `Dog` (a more specific type). C# supports covariant return types starting with C# 9 and .NET 5.

```csharp
public class AnimalShelter
{
    public virtual Animal GetAnimal() => new Animal();
}

public class DogShelter : AnimalShelter
{
    // Covariant return type — valid in C# 9+
    public override Dog GetAnimal() => new Dog();
}
```

## Part 4: The Classic Violations — And Why They Are Wrong

Every article about LSP mentions the rectangle-square problem. We will cover it here because it is genuinely instructive, but we will also go beyond it into violations you are more likely to encounter in production .NET code.

### Violation 1: The Rectangle and the Square

This is the textbook example, and it illustrates the principle perfectly.

In geometry, a square is a rectangle. Every square has four right angles and four sides, and opposite sides are equal. So it seems natural to model this with inheritance:

```csharp
public class Rectangle
{
    public virtual int Width { get; set; }
    public virtual int Height { get; set; }

    public int Area => Width * Height;
}

public class Square : Rectangle
{
    private int _side;

    public override int Width
    {
        get => _side;
        set
        {
            _side = value;
            // Must keep Width == Height for a square
        }
    }

    public override int Height
    {
        get => _side;
        set
        {
            _side = value;
        }
    }
}
```

Now consider this code, written against `Rectangle`:

```csharp
public void ResizeAndCheck(Rectangle rect)
{
    rect.Width = 5;
    rect.Height = 10;

    // For a rectangle, Area should be 50
    Debug.Assert(rect.Area == 50);
}
```

Pass in a `Rectangle` — the assertion passes. Pass in a `Square` — the assertion fails, because setting `Height = 10` also set `Width = 10`, so the area is 100.

The problem is not with geometry. The problem is that the `Rectangle` class has an implicit contract: setting `Width` does not change `Height`, and vice versa. The `Square` subclass violates this postcondition.

The fix: do not make `Square` inherit from `Rectangle`. Instead, model them as siblings under a common `IShape` interface:

```csharp
public interface IShape
{
    int Area { get; }
}

public class Rectangle : IShape
{
    public int Width { get; set; }
    public int Height { get; set; }
    public int Area => Width * Height;
}

public class Square : IShape
{
    public int Side { get; set; }
    public int Area => Side * Side;
}
```

Or, if immutability is acceptable, use immutable value types where the issue disappears entirely:

```csharp
public readonly record struct Rectangle(int Width, int Height)
{
    public int Area => Width * Height;
}

public readonly record struct Square(int Side)
{
    public int Area => Side * Side;
}
```

### Violation 2: The Read-Only Collection That Is Not

This one shows up constantly in .NET code:

```csharp
public class ReadOnlyRepository<T> : List<T>
{
    public ReadOnlyRepository(IEnumerable<T> items) : base(items) { }

    // "Disable" mutation by throwing
    public new void Add(T item) =>
        throw new NotSupportedException("Collection is read-only");

    public new void Remove(T item) =>
        throw new NotSupportedException("Collection is read-only");

    public new void Clear() =>
        throw new NotSupportedException("Collection is read-only");
}
```

This class inherits from `List<T>`, which has a contract that says "you can add, remove, and clear items." The `new` keyword hides the base methods but does not override them. If you cast to `List<T>` or `IList<T>`, the original `Add`, `Remove`, and `Clear` methods are still callable. Even if you used `override` (which you cannot, since `List<T>` methods are not virtual), throwing `NotSupportedException` weakens the postcondition — callers of `List<T>.Add` expect the item to be added, not an exception.

The fix: do not inherit from `List<T>`. Instead, expose `IReadOnlyList<T>` or `IReadOnlyCollection<T>`:

```csharp
public class ReadOnlyRepository<T>
{
    private readonly List<T> _items;

    public ReadOnlyRepository(IEnumerable<T> items)
    {
        _items = new List<T>(items);
    }

    public IReadOnlyList<T> Items => _items.AsReadOnly();
}
```

Or simply use the built-in `ReadOnlyCollection<T>`, which wraps a list and throws `NotSupportedException` from its `IList<T>` implementation. Wait — does that violate LSP? Yes, technically it does. This is why `IReadOnlyList<T>` was introduced in .NET 4.5 — to provide a *separate* interface hierarchy that does not promise mutability. The lesson: prefer `IReadOnlyList<T>` over `IList<T>` when your type does not support mutation.

### Violation 3: The NotImplementedException Anti-Pattern

This is perhaps the single most common LSP violation in real codebases:

```csharp
public interface IPaymentGateway
{
    void Charge(decimal amount);
    void Refund(decimal amount);
    PaymentStatus CheckStatus(string transactionId);
}

public class BasicPaymentGateway : IPaymentGateway
{
    public void Charge(decimal amount)
    {
        // Implementation...
    }

    public void Refund(decimal amount)
    {
        // This gateway does not support refunds
        throw new NotImplementedException(
            "Refunds are not supported by this gateway");
    }

    public PaymentStatus CheckStatus(string transactionId)
    {
        // Implementation...
    }
}
```

Any code that processes refunds through `IPaymentGateway` will explode when it encounters `BasicPaymentGateway`. The interface says "I can refund." The implementation says "actually, I can't."

The fix is interface segregation (the "I" in SOLID works hand-in-hand with the "L"):

```csharp
public interface IPaymentGateway
{
    void Charge(decimal amount);
    PaymentStatus CheckStatus(string transactionId);
}

public interface IRefundableGateway : IPaymentGateway
{
    void Refund(decimal amount);
}

public class BasicPaymentGateway : IPaymentGateway
{
    public void Charge(decimal amount) { /* ... */ }
    public PaymentStatus CheckStatus(string transactionId) { /* ... */ }
    // No Refund method — no lie
}

public class FullPaymentGateway : IRefundableGateway
{
    public void Charge(decimal amount) { /* ... */ }
    public void Refund(decimal amount) { /* ... */ }
    public PaymentStatus CheckStatus(string transactionId) { /* ... */ }
}
```

Now the type system tells the truth. If you need refund capability, accept `IRefundableGateway`. If you only need charging, accept `IPaymentGateway`. No runtime surprises.

### Violation 4: The Derived Class That Ignores Parameters

```csharp
public abstract class Logger
{
    public abstract void Log(string message, LogLevel level);
}

public class ConsoleLogger : Logger
{
    public override void Log(string message, LogLevel level)
    {
        // VIOLATION: Ignores log level entirely,
        // always writes to console
        Console.WriteLine(message);
    }
}
```

If the base class contract says "messages at `LogLevel.None` are suppressed," and `ConsoleLogger` writes everything regardless, it violates the postcondition. Callers who set `LogLevel.None` expecting silence will be surprised.

### Violation 5: Temporal Coupling in Derived Classes

```csharp
public abstract class DataPipeline
{
    public abstract void Configure(PipelineOptions options);
    public abstract void Execute();
}

public class BatchPipeline : DataPipeline
{
    private PipelineOptions? _options;

    public override void Configure(PipelineOptions options)
    {
        _options = options;
    }

    public override void Execute()
    {
        // VIOLATION: Throws if Configure was not called first,
        // introducing a precondition the base class didn't require
        if (_options is null)
            throw new InvalidOperationException(
                "Must call Configure before Execute");

        // Process...
    }
}
```

If the base class contract does not require calling `Configure` before `Execute`, then `BatchPipeline` has strengthened the precondition. The fix: either document the requirement on the base class (making it a universal precondition) or eliminate the temporal coupling by requiring configuration in the constructor.

## Part 5: LSP in the .NET Framework and Runtime

The .NET ecosystem itself contains both good examples of LSP adherence and some well-known violations. Understanding where the framework gets it right — and where it does not — will sharpen your instincts.

### Stream: A Mostly-Good Hierarchy

`System.IO.Stream` is one of the most widely used abstract classes in .NET. Its subclasses include `FileStream`, `MemoryStream`, `NetworkStream`, `GZipStream`, `CryptoStream`, `SslStream`, and many more. The design handles LSP through capability queries:

```csharp
public abstract class Stream
{
    public abstract bool CanRead { get; }
    public abstract bool CanWrite { get; }
    public abstract bool CanSeek { get; }

    public abstract int Read(byte[] buffer, int offset, int count);
    public abstract void Write(byte[] buffer, int offset, int count);
    public abstract long Seek(long offset, SeekOrigin origin);
    // ...
}
```

A `NetworkStream` sets `CanSeek` to `false` and throws `NotSupportedException` from `Seek`. Is that an LSP violation? It depends on how you define the contract. If the contract of `Stream.Seek` is "seeks to a position in the stream," then yes, `NetworkStream` violates it. But the *actual* contract, as documented, is "seeks to a position in the stream if `CanSeek` is `true`; otherwise throws `NotSupportedException`." The capability flags are part of the contract.

This is a pragmatic compromise. Ideally, you would have separate `IReadableStream`, `IWritableStream`, and `ISeekableStream` interfaces (and indeed, newer designs sometimes take this approach). But `Stream` was designed in .NET 1.0 and must maintain backward compatibility. The capability-flag pattern is the next best thing.

### ICollection<T> and IReadOnlyCollection<T>: A Course Correction

The original `ICollection<T>` interface (introduced in .NET 2.0) includes `Add`, `Remove`, and `Clear` methods. `ReadOnlyCollection<T>` implements `ICollection<T>` and throws `NotSupportedException` from the mutation methods. This is a well-known LSP weakness in the framework.

.NET 4.5 introduced `IReadOnlyCollection<T>` and `IReadOnlyList<T>` as separate interface hierarchies that do not promise mutation. This was an explicit recognition that the original design forced types into LSP violations. Today, the recommendation is:

- Accept `IReadOnlyList<T>` or `IReadOnlyCollection<T>` when you only need to read.
- Accept `IList<T>` or `ICollection<T>` when you need to mutate.
- Return `IReadOnlyList<T>` from methods that return collections you do not want callers to modify.

### Array Covariance: A Famous Type Hole

C# arrays are covariant, which means you can assign a `string[]` to an `object[]` variable:

```csharp
object[] objects = new string[3];
objects[0] = "hello";    // Fine
objects[1] = 42;         // Compiles! But throws ArrayTypeMismatchException at runtime
```

This is a genuine LSP violation baked into the language for backward compatibility (inherited from Java's design). An `object[]` promises "you can put any object in here." A `string[]` does not honor that promise. The type system says it is valid; the runtime says otherwise.

This is why generic collections (`List<T>`) are preferred over arrays for APIs. Generic variance in C# is safe: `IEnumerable<out T>` is covariant, `IComparer<in T>` is contravariant, and these are enforced at compile time.

## Part 6: Design Patterns That Promote (and Violate) LSP

### Patterns That Help

**Strategy Pattern.** The Strategy pattern is a natural fit for LSP. You define an interface, create multiple implementations, and swap them at runtime. As long as each implementation honors the interface contract, LSP is satisfied.

```csharp
public interface ISortingStrategy<T>
{
    void Sort(List<T> items, IComparer<T> comparer);
}

public class QuickSortStrategy<T> : ISortingStrategy<T>
{
    public void Sort(List<T> items, IComparer<T> comparer)
    {
        // Quick sort implementation
        items.Sort(comparer); // Delegates to built-in
    }
}

public class BubbleSortStrategy<T> : ISortingStrategy<T>
{
    public void Sort(List<T> items, IComparer<T> comparer)
    {
        // Bubble sort implementation
        for (int i = 0; i < items.Count - 1; i++)
        {
            for (int j = 0; j < items.Count - 1 - i; j++)
            {
                if (comparer.Compare(items[j], items[j + 1]) > 0)
                {
                    (items[j], items[j + 1]) = (items[j + 1], items[j]);
                }
            }
        }
    }
}
```

Both strategies sort the list. The result is the same (a sorted list). The performance differs, but the postcondition is identical. LSP is preserved.

**Template Method Pattern.** When you define an algorithm's skeleton in a base class and let subclasses override specific steps, LSP is maintained as long as the overridden steps honor their contracts. The base class controls the overall flow; subclasses customize the details.

```csharp
public abstract class ReportGenerator
{
    // Template method — not virtual
    public string Generate(ReportData data)
    {
        var header = BuildHeader(data);
        var body = BuildBody(data);
        var footer = BuildFooter(data);
        return $"{header}\n{body}\n{footer}";
    }

    protected abstract string BuildHeader(ReportData data);
    protected abstract string BuildBody(ReportData data);
    protected virtual string BuildFooter(ReportData data)
        => $"Generated at {DateTime.UtcNow:u}";
}
```

**Decorator Pattern.** Decorators wrap an existing object to add behavior. Because the decorator implements the same interface and delegates to the wrapped object, LSP is naturally preserved:

```csharp
public interface IMessageSender
{
    Task SendAsync(string recipient, string body);
}

public class EmailSender : IMessageSender
{
    public async Task SendAsync(string recipient, string body)
    {
        // Send email...
        await Task.CompletedTask;
    }
}

public class LoggingMessageSender : IMessageSender
{
    private readonly IMessageSender _inner;
    private readonly ILogger _logger;

    public LoggingMessageSender(IMessageSender inner, ILogger logger)
    {
        _inner = inner;
        _logger = logger;
    }

    public async Task SendAsync(string recipient, string body)
    {
        _logger.LogInformation("Sending message to {Recipient}", recipient);
        await _inner.SendAsync(recipient, body);
        _logger.LogInformation("Message sent to {Recipient}", recipient);
    }
}
```

### Patterns That Risk Violations

**Adapter Pattern (when misused).** Adapters translate one interface to another. If the adapted interface does not fully support the target interface's contract, the adapter will violate LSP. For example, adapting a key-value store (which supports only `Get` and `Put`) to a full `IDatabase` interface (which includes `Transaction`, `Rollback`, and `Query`) will likely produce `NotImplementedException` stubs.

**Null Object Pattern (when lazy).** The Null Object pattern provides a do-nothing implementation to avoid null checks. This is fine when the contract permits no-ops (e.g., a `NullLogger` that silently discards messages). It is an LSP violation when the contract requires meaningful action (e.g., a `NullRepository` that claims to save data but does not).

## Part 7: LSP and Dependency Injection in ASP.NET Core

Dependency injection (DI) is the standard approach in modern ASP.NET Core applications, and LSP is the principle that makes DI work safely. When you register a service in the DI container:

```csharp
builder.Services.AddScoped<IOrderService, OrderService>();
```

You are telling the framework: "Wherever someone asks for `IOrderService`, give them an `OrderService`." This is only safe if `OrderService` is a valid behavioral subtype of `IOrderService` — i.e., it honors every contract the interface promises.

### A Real-World DI Scenario

Imagine a notification service with multiple implementations:

```csharp
public interface INotificationService
{
    /// <summary>
    /// Sends a notification to the specified user.
    /// Returns true if the notification was delivered, false otherwise.
    /// Never throws on delivery failure — returns false instead.
    /// </summary>
    Task<bool> NotifyAsync(string userId, string message);
}

public class EmailNotificationService : INotificationService
{
    private readonly IEmailClient _emailClient;

    public EmailNotificationService(IEmailClient emailClient)
    {
        _emailClient = emailClient;
    }

    public async Task<bool> NotifyAsync(string userId, string message)
    {
        try
        {
            await _emailClient.SendAsync(userId, "Notification", message);
            return true;
        }
        catch (Exception)
        {
            return false; // Honors the "never throws" contract
        }
    }
}

public class SmsNotificationService : INotificationService
{
    private readonly ISmsGateway _gateway;

    public SmsNotificationService(ISmsGateway gateway)
    {
        _gateway = gateway;
    }

    public async Task<bool> NotifyAsync(string userId, string message)
    {
        try
        {
            var phone = await LookupPhoneNumber(userId);
            await _gateway.SendSmsAsync(phone, message);
            return true;
        }
        catch (Exception)
        {
            return false; // Honors the "never throws" contract
        }
    }

    private Task<string> LookupPhoneNumber(string userId)
    {
        // Lookup implementation...
        return Task.FromResult("+1234567890");
    }
}
```

Both implementations honor the contract: they return `bool`, they never throw on delivery failure. You can swap between them in `Program.cs` and the rest of the application works unchanged. That is LSP in action.

Now consider a broken implementation:

```csharp
public class PushNotificationService : INotificationService
{
    public async Task<bool> NotifyAsync(string userId, string message)
    {
        // VIOLATION: Throws instead of returning false
        var token = await GetPushToken(userId)
            ?? throw new InvalidOperationException(
                $"No push token for user {userId}");

        await SendPush(token, message);
        return true;
    }

    // ...
}
```

This violates the "never throws on delivery failure" postcondition. Any calling code that does not expect an exception from `NotifyAsync` will fail. Register this in DI, and you have a production bug waiting to happen.

### Testing for LSP in DI Scenarios

A useful testing pattern: write contract tests against the interface and run them for every registered implementation.

```csharp
public abstract class NotificationServiceContractTests
{
    protected abstract INotificationService CreateService();

    [Fact]
    public async Task NotifyAsync_WithValidInput_ReturnsBoolean()
    {
        var service = CreateService();
        var result = await service.NotifyAsync("user-1", "Hello");
        Assert.IsType<bool>(result);
    }

    [Fact]
    public async Task NotifyAsync_NeverThrowsOnDeliveryFailure()
    {
        var service = CreateService();

        // This should not throw, even if delivery fails
        var exception = await Record.ExceptionAsync(
            () => service.NotifyAsync("nonexistent-user", "Hello"));

        Assert.Null(exception);
    }

    [Fact]
    public async Task NotifyAsync_WithNullUserId_ThrowsArgumentNullException()
    {
        var service = CreateService();

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => service.NotifyAsync(null!, "Hello"));
    }
}

public class EmailNotificationServiceTests : NotificationServiceContractTests
{
    protected override INotificationService CreateService()
    {
        var mockClient = new MockEmailClient();
        return new EmailNotificationService(mockClient);
    }
}

public class SmsNotificationServiceTests : NotificationServiceContractTests
{
    protected override INotificationService CreateService()
    {
        var mockGateway = new MockSmsGateway();
        return new SmsNotificationService(mockGateway);
    }
}
```

If `PushNotificationService` fails `NotifyAsync_NeverThrowsOnDeliveryFailure`, you have caught the LSP violation before it reaches production.

## Part 8: LSP and Generics in C#

C# generics interact with LSP in subtle ways, especially around variance.

### Covariance (out)

`IEnumerable<out T>` is covariant. This means `IEnumerable<Dog>` is substitutable for `IEnumerable<Animal>` — which is safe because `IEnumerable<T>` only *produces* values of type `T`, it never *consumes* them. The consumer receives objects that are at least as specific as `Animal`, so all `Animal` operations work.

```csharp
IEnumerable<Dog> dogs = new List<Dog> { new Dog("Rex"), new Dog("Buddy") };
IEnumerable<Animal> animals = dogs; // Safe — covariance

foreach (Animal animal in animals)
{
    Console.WriteLine(animal.Name); // Works — Dog IS-A Animal
}
```

### Contravariance (in)

`IComparer<in T>` is contravariant. This means `IComparer<Animal>` is substitutable for `IComparer<Dog>` — which is safe because a comparer that can compare any two animals can certainly compare two dogs.

```csharp
IComparer<Animal> animalComparer = new AnimalByNameComparer();
IComparer<Dog> dogComparer = animalComparer; // Safe — contravariance

var dogs = new List<Dog> { new Dog("Rex"), new Dog("Buddy") };
dogs.Sort(dogComparer); // Works — the comparer can handle Dogs
```

### Invariance and the Trouble with Mutable Collections

`IList<T>` is invariant — `IList<Dog>` is not assignable to `IList<Animal>`. This is correct! If it were covariant:

```csharp
// Hypothetical (does not compile, and for good reason):
IList<Animal> animals = new List<Dog>();
animals.Add(new Cat()); // A Cat in a List<Dog> — disaster!
```

Invariance protects LSP. The type system prevents you from creating a situation where a collection promises to accept any `Animal` but can actually only hold `Dog` instances.

### Generic Constraints and LSP

When you write generic constraints, you are defining contracts:

```csharp
public class Repository<T> where T : IEntity, new()
{
    public T Create()
    {
        var entity = new T();
        entity.Id = Guid.NewGuid();
        return entity;
    }
}
```

The constraint `where T : IEntity, new()` ensures that any type used with `Repository<T>` satisfies LSP relative to `IEntity`: it has an `Id` property and a parameterless constructor. The generic constraint is a compile-time LSP check.

## Part 9: LSP Beyond Inheritance — Interfaces, Records, and Composition

A common misconception: LSP only applies to class inheritance. In fact, LSP applies to any subtyping relationship, including interface implementation, and even to any situation where one component can be substituted for another.

### Interfaces and LSP

When a class implements an interface, it enters into an LSP contract. Every implementation of `IDisposable.Dispose()` must be safe to call multiple times (the documented contract). Every implementation of `IEquatable<T>.Equals` must be reflexive, symmetric, and transitive. These are behavioral contracts, and violating them is an LSP violation.

### Records and LSP

C# records support inheritance:

```csharp
public abstract record Shape(string Color);
public record Circle(string Color, double Radius) : Shape(Color);
public record Rectangle(string Color, double Width, double Height) : Shape(Color);
```

Records automatically generate `Equals`, `GetHashCode`, `ToString`, and copy constructors. The generated `Equals` considers all properties, including those introduced in derived records. This is generally LSP-safe because the generated behavior is consistent with the declared properties.

However, be careful with `with` expressions and polymorphism:

```csharp
Shape shape = new Circle("Red", 5.0);
Shape modified = shape with { Color = "Blue" };
// modified is a Circle with Color="Blue" and Radius=5.0
// The runtime type is preserved — LSP is maintained
```

### Composition Over Inheritance: The LSP Escape Hatch

When you find yourself struggling to make an inheritance hierarchy LSP-compliant, it is often a sign that inheritance is the wrong tool. Composition — building complex objects by combining simpler ones — sidesteps LSP issues entirely because there is no subtyping relationship to violate.

```csharp
// Instead of:
public class LoggedRepository : Repository  // Fragile, LSP-risky
{
    // Override every method to add logging...
}

// Prefer:
public class LoggedRepository : IRepository  // No inheritance, no LSP risk
{
    private readonly IRepository _inner;
    private readonly ILogger _logger;

    public LoggedRepository(IRepository inner, ILogger logger)
    {
        _inner = inner;
        _logger = logger;
    }

    public async Task<Entity> GetByIdAsync(Guid id)
    {
        _logger.LogInformation("Fetching entity {Id}", id);
        return await _inner.GetByIdAsync(id);
    }

    // Delegate all methods to _inner, adding logging as needed
}
```

This is not an argument against inheritance — it is an argument for being deliberate about when to use it. Use inheritance when the "is-a" relationship is genuine and the base class contract is stable. Use composition when you want to add behavior without taking on the obligations of a subtyping contract.

## Part 10: Detecting LSP Violations

How do you find LSP violations in an existing codebase? Here are concrete techniques.

### Technique 1: Search for NotImplementedException and NotSupportedException

Run this in your project:

```bash
grep -rn "NotImplementedException\|NotSupportedException" --include="*.cs" .
```

Every hit is a potential LSP violation. Not every one will be — `Stream` subclasses that throw from `Seek` when `CanSeek` is `false` are contractually valid — but each one deserves scrutiny.

### Technique 2: Search for Type Checks in Consumer Code

```bash
grep -rn "is \|as \|GetType()\|typeof(" --include="*.cs" .
```

Code that checks the runtime type of an object before deciding what to do is often working around an LSP violation:

```csharp
// This is a code smell — the caller should not need to know the subtype
public decimal CalculateFee(IAccount account)
{
    if (account is PremiumAccount)
        return 0m;
    if (account is OverdraftAccount overdraft)
        return overdraft.OverdraftFee;
    return 5.00m;
}
```

The fix: push the fee calculation into the type hierarchy:

```csharp
public interface IAccount
{
    decimal CalculateFee();
}

public class StandardAccount : IAccount
{
    public decimal CalculateFee() => 5.00m;
}

public class PremiumAccount : IAccount
{
    public decimal CalculateFee() => 0m;
}

public class OverdraftAccount : IAccount
{
    public decimal OverdraftFee { get; init; }
    public decimal CalculateFee() => OverdraftFee;
}
```

### Technique 3: Contract Tests

As shown in Part 7, write abstract test classes that define the expected behavior of an interface, then inherit from them for each implementation. If a new implementation fails a contract test, you have found an LSP violation before it ships.

### Technique 4: Code Analysis and Roslyn Analyzers

While there is no built-in Roslyn analyzer specifically for LSP, you can write custom analyzers that flag common patterns:

- Methods that throw `NotImplementedException`
- Override methods that throw exceptions the base class does not declare
- Override methods with `if (someCondition) throw` at the top (strengthened preconditions)
- Classes that implement an interface but `new`-hide methods instead of implementing them

### Technique 5: Review Virtual Method Overrides

During code review, pay special attention to every `override` keyword. Ask:

1. Does this override accept all inputs the base method accepts?
2. Does this override produce all outputs the base method promises?
3. Does this override maintain all invariants the base class establishes?
4. Does this override throw only exceptions the base class allows?

If the answer to any question is "no," you have found a violation.

## Part 11: LSP and the Other SOLID Principles

LSP does not exist in isolation. It interacts with every other SOLID principle.

### Single Responsibility Principle (SRP) and LSP

A class with too many responsibilities is harder to subtype correctly, because the subclass must honor contracts across all those responsibilities. Keeping classes focused (SRP) makes LSP compliance easier.

### Open/Closed Principle (OCP) and LSP

OCP says: "open for extension, closed for modification." LSP says: "extensions must honor the base contract." Together they mean: you can add new behavior through subtyping, but only if the new type is a valid substitute for the base type. OCP tells you *to* extend; LSP tells you *how* to extend safely.

### Interface Segregation Principle (ISP) and LSP

ISP says: "don't force implementations to depend on methods they don't use." When interfaces are bloated, implementors are tempted to throw `NotImplementedException` from methods they cannot meaningfully implement — which violates LSP. Segregating interfaces into smaller, focused ones makes it possible for every implementor to honor the full contract.

As we saw with the payment gateway example: splitting `IPaymentGateway` into `IPaymentGateway` and `IRefundableGateway` simultaneously satisfies ISP and LSP.

### Dependency Inversion Principle (DIP) and LSP

DIP says: "depend on abstractions, not concretions." LSP says: "those abstractions are only useful if all implementations honor their contracts." DIP without LSP is just indirection for indirection's sake — you depend on an interface, but the implementations behind it behave unpredictably. LSP makes DIP trustworthy.

## Part 12: LSP in Functional and Hybrid Styles

Modern C# is increasingly functional, with pattern matching, records, expression-bodied members, and LINQ everywhere. Does LSP still matter when you are writing functional-style code?

Yes, but the vocabulary changes.

In functional programming, the equivalent of LSP is that functions with the same type signature should be interchangeable if they are used in the same context. A `Func<int, int>` that represents "double the input" and a `Func<int, int>` that represents "square the input" are both valid substitutions in any context that accepts `Func<int, int>` — as long as the calling code does not depend on specific behavior beyond "takes an int, returns an int."

Higher-order functions rely on LSP implicitly:

```csharp
public IEnumerable<T> Filter<T>(
    IEnumerable<T> source,
    Func<T, bool> predicate)
{
    foreach (var item in source)
    {
        if (predicate(item))
            yield return item;
    }
}
```

This works with *any* predicate because the contract of `Func<T, bool>` is simply "takes a `T`, returns a `bool`." A predicate that throws half the time, or that has side effects like deleting files, technically satisfies the type signature but violates the implicit behavioral contract of "a pure test function."

### Discriminated Unions and Exhaustive Matching

When you model variants with a closed hierarchy and pattern matching, LSP is satisfied by construction — every variant is known and every case is handled:

```csharp
public abstract record PaymentResult;
public record PaymentSucceeded(string TransactionId) : PaymentResult;
public record PaymentFailed(string Reason) : PaymentResult;
public record PaymentPending(string CheckUrl) : PaymentResult;

public string Describe(PaymentResult result) => result switch
{
    PaymentSucceeded s => $"Paid! Transaction: {s.TransactionId}",
    PaymentFailed f => $"Failed: {f.Reason}",
    PaymentPending p => $"Pending. Check at: {p.CheckUrl}",
    _ => throw new UnreachableException()
};
```

Each variant is a valid substitution for `PaymentResult`. The exhaustive `switch` ensures every variant is handled. This is LSP-by-design.

## Part 13: Common Pitfalls and How to Avoid Them

### Pitfall 1: Confusing "Is-A" in the Real World with "Is-A" in Code

A square *is* a rectangle in geometry. An ostrich *is* a bird in biology. But that does not mean `Square` should inherit from `Rectangle`, or `Ostrich` should inherit from `Bird` if `Bird` has a `Fly()` method.

The "is-a" relationship in code means "can be substituted for." Ask the substitution question, not the taxonomy question: "Can I use a `Square` everywhere I use a `Rectangle` without changing behavior?" If the answer is no, do not use inheritance.

### Pitfall 2: Inheriting for Code Reuse, Not Substitutability

Inheritance is often used as a code reuse mechanism: "I need these five methods from `BaseService`, so I will inherit from it." But inheritance creates a subtyping relationship, and now your class must honor the entire contract of `BaseService`. If you only want code reuse, use composition:

```csharp
// Don't do this:
public class SpecialOrderService : OrderService { }

// Do this instead:
public class SpecialOrderService
{
    private readonly OrderService _orderService;

    public SpecialOrderService(OrderService orderService)
    {
        _orderService = orderService;
    }
}
```

### Pitfall 3: Sealing Too Late

If a class is not designed for inheritance, seal it. C# classes are unsealed by default, which invites subtyping. If your class has implicit contracts that are not documented (like "setting `Width` does not change `Height`"), a subclass will eventually violate them.

```csharp
public sealed class Configuration
{
    public string ConnectionString { get; init; } = "";
    public int MaxRetries { get; init; } = 3;
}
```

Starting with .NET 7, the runtime can optimize sealed classes more aggressively (devirtualization), so sealing is a performance win as well.

### Pitfall 4: Not Documenting Contracts

LSP violations often stem from undocumented contracts. If the only way to know that `Dispose()` must be idempotent is to read the implementation, some future implementor will get it wrong.

Use XML documentation comments to document preconditions, postconditions, and invariants:

```csharp
public interface ICache<TKey, TValue> where TKey : notnull
{
    /// <summary>
    /// Retrieves a value from the cache.
    /// </summary>
    /// <param name="key">The cache key. Must not be null.</param>
    /// <returns>
    /// The cached value, or default(TValue) if the key is not found.
    /// Never throws on a missing key.
    /// </returns>
    TValue? Get(TKey key);

    /// <summary>
    /// Adds or updates a value in the cache.
    /// </summary>
    /// <param name="key">The cache key. Must not be null.</param>
    /// <param name="value">The value to cache. May be null.</param>
    /// <remarks>
    /// Postcondition: After Set returns, Get(key) returns value
    /// (or an equivalent, if the cache performs serialization).
    /// </remarks>
    void Set(TKey key, TValue value);
}
```

### Pitfall 5: Ignoring LSP in Test Doubles

Mocks and stubs are subtype implementations used in tests. If your mock violates the contract of the interface it implements, your tests may pass even when the production implementation has bugs — or your tests may fail for reasons unrelated to the code under test.

```csharp
// BAD mock: violates the contract that Get never throws on missing key
public class BadMockCache : ICache<string, string>
{
    public string? Get(string key) =>
        throw new KeyNotFoundException(); // Contract says: return default, don't throw

    public void Set(string key, string value) { }
}

// GOOD mock: honors the contract
public class GoodMockCache : ICache<string, string>
{
    private readonly Dictionary<string, string> _store = new();

    public string? Get(string key) =>
        _store.TryGetValue(key, out var value) ? value : default;

    public void Set(string key, string value) =>
        _store[key] = value;
}
```

## Part 14: A Practical Checklist

When designing a new class hierarchy or implementing an interface, run through this checklist:

**Before writing the subtype:**

1. Have I documented the preconditions, postconditions, and invariants of the base type or interface?
2. Is the "is-a" relationship genuine in the behavioral sense, not just the taxonomic sense?
3. Could I achieve my goal with composition instead of inheritance?
4. If I am inheriting from a concrete class, is it designed for inheritance (not sealed, virtual methods documented)?

**While writing the subtype:**

5. Do all overridden methods accept *at least* the same range of inputs as the base?
6. Do all overridden methods produce *at least* the same guarantees on output as the base?
7. Do I maintain all invariants from the base class?
8. Do I throw only exception types that the base class contract allows?
9. Am I introducing any new state that contradicts the base class's immutability or state-transition rules?

**After writing the subtype:**

10. Can I pass my subtype to every method that accepts the base type and have all existing tests pass?
11. Have I written contract tests that verify my implementation against the interface's behavioral contract?
12. Have I tested with `null` inputs, empty collections, boundary values, and failure scenarios?

## Part 15: LSP in the Age of Source Generators, Interceptors, and AI

Modern .NET development is evolving rapidly. Source generators can create implementations of interfaces at compile time. Interceptors can replace method implementations transparently. AI coding assistants generate implementations from interface definitions. In each case, LSP remains the quality gate.

A source-generated implementation of `IRepository<T>` must honor the same contracts as a hand-written one. An interceptor that replaces a caching layer must maintain the same preconditions and postconditions. An AI-generated implementation of `INotificationService` must satisfy the same contract tests.

The tooling changes. The principle does not.

If anything, LSP becomes *more* important as code generation increases. When humans write every line, they bring context and judgment. When code is generated — whether by a T4 template, a Roslyn source generator, or an LLM — the behavioral contract is the only thing ensuring correctness. Write clear contracts. Write contract tests. Let the principle do its work.

## Part 16: Resources and Further Reading

Here are authoritative references for deeper study:

- **Barbara Liskov and Jeannette Wing, "A Behavioral Notion of Subtyping" (1994)** — The foundational paper. Published in ACM Transactions on Programming Languages and Systems, Vol. 16, No. 6.
- **Robert C. Martin, "Design Principles and Design Patterns" (2000)** — The paper that collected the five principles that became SOLID.
- **Robert C. Martin, *Agile Software Development: Principles, Patterns, and Practices* (2002)** — Chapter 10 covers LSP with detailed C++ and Java examples.
- **Barbara Liskov, "Data Abstraction and Hierarchy" (1987)** — The original OOPSLA keynote, published in SIGPLAN Notices.
- **Bertrand Meyer, *Object-Oriented Software Construction* (1988, 2nd ed. 1997)** — Introduces Design by Contract, which provides the vocabulary (preconditions, postconditions, invariants) used to formalize LSP.
- **Microsoft C# Documentation — Covariance and Contravariance in Generics**: [https://learn.microsoft.com/en-us/dotnet/standard/generics/covariance-and-contravariance](https://learn.microsoft.com/en-us/dotnet/standard/generics/covariance-and-contravariance)
- **Microsoft .NET Design Guidelines — Choosing Between Class and Struct**: [https://learn.microsoft.com/en-us/dotnet/standard/design-guidelines/](https://learn.microsoft.com/en-us/dotnet/standard/design-guidelines/)
- **Barbara Liskov — ACM Turing Award Laureate Profile**: [https://amturing.acm.org/award_winners/liskov_1108679.cfm](https://amturing.acm.org/award_winners/liskov_1108679.cfm)
- **SOLID Principles — Wikipedia**: [https://en.wikipedia.org/wiki/SOLID](https://en.wikipedia.org/wiki/SOLID)

## Conclusion

The Liskov Substitution Principle is not about rectangles and squares. It is not an academic curiosity. It is the invisible contract that makes polymorphism — the most powerful feature of object-oriented programming — actually work.

Every time you write `ILogger logger` in a method signature, you are trusting that whatever implementation arrives at runtime will behave like a logger. Every time you register a service in the DI container, you are trusting that the concrete type honors the interface's contract. Every time you swap an adapter, a strategy, or a decorator, you are trusting that the new component is a valid substitute for the old one.

When that trust is justified — when every subtype honors every contract — your system is modular, testable, and resilient to change. When it is not — when subtypes throw unexpected exceptions, ignore parameters, break invariants, or strengthen preconditions — you get the kind of bugs that are hardest to diagnose: the ones that only appear when a specific subtype is used in a specific context that nobody anticipated.

Barbara Liskov's insight, first articulated at a conference in 1987, formalized in 1994, and adopted as a pillar of software design by 2000, remains as relevant today as it was then. The languages have changed. The frameworks have changed. The deployment targets have changed. But the need for behavioral substitutability — for types that keep their promises — has not changed, and never will.

Write clear contracts. Honor them in every implementation. Test them with contract tests. Seal what is not designed for extension. Prefer composition when inheritance does not fit. And the next time you see a `NotImplementedException`, treat it as a design smell, not a TODO — because somewhere downstream, someone is trusting your type to do what it says.

That trust is the Liskov Substitution Principle. Do not break it.