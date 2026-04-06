---
title: "Data Structures in .NET: A Comprehensive Guide from Primitives to Advanced Collections"
date: 2026-04-09
author: myblazor-team
summary: "An exhaustive guide to every data structure available in .NET 10 and C# 14 — from primitive types and value semantics through arrays, lists, dictionaries, trees, graphs, queues, stacks, spans, and frozen collections — with working code examples, internal implementation details, Big-O analysis, and practical advice for ASP.NET developers."
tags:
  - dotnet
  - csharp
  - data-structures
  - deep-dive
  - best-practices
  - aspnet
  - software-engineering
  - performance
---

You are staring at a pull request. The author has used a `List<string>` to store unique user roles, a `Dictionary<int, Order>` as a priority queue, and a raw `object[]` where a `Span<byte>` would have eliminated three allocations per request. The code works. It passes tests. And it will fall over in production the moment traffic spikes, because every data structure choice is wrong.

This is not a contrived example. This is Tuesday.

Data structures are the most consequential architectural decisions you make, and you make them dozens of times a day — often on autopilot. Every time you write `new List<T>()` you are choosing a contiguous array with O(n) insertion at arbitrary positions. Every time you write `new Dictionary<TKey, TValue>()` you are choosing a hash table with O(1) amortized lookups but no ordering guarantees. Every time you ignore `Span<T>` in a hot path you are choosing heap allocations that the garbage collector will eventually have to clean up.

This article is a comprehensive tour of every data structure that matters in modern .NET 10 development. We start at the very bottom — the primitive types that sit directly on the managed stack — and work our way up through arrays, lists, sets, dictionaries, queues, stacks, trees, concurrent collections, immutable collections, frozen collections, and the memory-oriented types like `Span<T>` and `Memory<T>`. For every data structure, we cover three things: what it is, how .NET implements it internally, and when you should (and should not) use it. Every section includes working C# code you can paste into a .NET 10 console app and run.

Let us begin.

## Part 1: Primitive Types — The Foundation of Everything

### Why Primitives Matter

Before you can understand a `List<int>`, you need to understand `int`. Before you can reason about a `Dictionary<string, decimal>`, you need to understand `string` and `decimal`. Primitives are not just "simple types." They are the atoms from which every other data structure is built, and their memory layout, size, and value-versus-reference semantics determine the performance characteristics of every collection that holds them.

### The Numeric Types

C# provides a rich set of numeric primitives. Each one maps directly to a Common Language Runtime (CLR) type, and ultimately to a specific number of bytes in memory.

```csharp
// Signed integers — stored in two's complement
sbyte  temperature = -40;     // System.SByte   — 1 byte  (-128 to 127)
short  elevation   = -413;    // System.Int16   — 2 bytes (-32,768 to 32,767)
int    population  = 331_000_000; // System.Int32 — 4 bytes (~±2.1 billion)
long   nationalDebt = 34_000_000_000_000L; // System.Int64 — 8 bytes

// Unsigned integers — no negative values, double the positive range
byte   age         = 255;     // System.Byte    — 1 byte  (0 to 255)
ushort port        = 443;     // System.UInt16  — 2 bytes (0 to 65,535)
uint   ipAddress   = 3_232_235_777; // System.UInt32 — 4 bytes (0 to ~4.2 billion)
ulong  fileSize    = 18_446_744_073_709_551_615; // System.UInt64 — 8 bytes

// Native-sized integers — match pointer size (8 bytes on 64-bit)
nint   managedPtr  = nint.MaxValue;   // System.IntPtr
nuint  unmanagedSz = nuint.MinValue;  // System.UIntPtr

// Floating point
float  latitude    = 37.7749f;   // System.Single — 4 bytes, ~6-9 digits precision
double longitude   = -122.4194;  // System.Double — 8 bytes, ~15-17 digits precision

// Decimal — 128-bit, base-10 arithmetic
decimal price      = 19.99m;     // System.Decimal — 16 bytes, 28-29 digits precision
```

A common question from developers who have only worked in C# is: "Why do we have both `float` and `double` and `decimal`?" The answer comes down to how they represent numbers internally.

`float` and `double` use IEEE 754 binary floating-point representation. They are fast because modern CPUs have dedicated floating-point units (FPUs) that operate on these formats natively. But they cannot represent all base-10 fractions exactly. The classic example:

```csharp
double a = 0.1;
double b = 0.2;
Console.WriteLine(a + b == 0.3); // False!
Console.WriteLine(a + b);        // 0.30000000000000004
```

`decimal` uses base-10 arithmetic internally. It is slower — roughly 10 to 20 times slower than `double` for arithmetic — but it represents decimal fractions exactly. This is why financial calculations must use `decimal`. When someone tells you "use `decimal` for money," this is the reason. It is not a style preference. It is a correctness requirement.

### Boolean and Character Types

```csharp
bool isActive = true;   // System.Boolean — 1 byte (not 1 bit!)
char letter   = 'A';    // System.Char    — 2 bytes (UTF-16 code unit)
```

A `bool` occupies one full byte in memory, even though it only needs one bit. This is because the CLR's smallest addressable unit is a byte. If you need to store millions of booleans efficiently, you should use `BitArray` or `BitVector32`, which we cover later.

A `char` is 2 bytes because .NET strings use UTF-16 encoding. This was a design decision made in the early 2000s when the Unicode Basic Multilingual Plane covered most characters. Today, with emoji and extended scripts, a single "character" as perceived by a human can require two `char` values (a surrogate pair). This is important when you work with string slicing.

### The String: A Reference Type That Behaves Like a Value

```csharp
string greeting = "Hello, World!";
```

`string` is technically a reference type — it lives on the heap, and the variable holds a pointer. But `string` is immutable. Once created, a `string` instance can never be modified. Every operation that appears to modify a string actually creates a new string. This has profound implications:

```csharp
// This creates 10,001 string objects on the heap
string result = "";
for (int i = 0; i <= 10_000; i++)
{
    result += i.ToString(); // Each += allocates a new string
}

// This creates exactly 1 string at the end
var sb = new StringBuilder();
for (int i = 0; i <= 10_000; i++)
{
    sb.Append(i);
}
string result2 = sb.ToString();
```

Internally, a `string` is stored as a contiguous array of `char` values with a length prefix. The CLR stores the character data inline with the object header, which means accessing characters by index is O(1). The `string` class also overrides `==` and `GetHashCode()` to provide value semantics — two different string instances with the same characters compare as equal. This is why `string` is the most common dictionary key type.

### Value Types vs. Reference Types: The Fundamental Divide

Every type in .NET is either a value type or a reference type. This distinction affects how data structures store and retrieve elements, how much memory they consume, and how the garbage collector interacts with them.

```csharp
// Value types: stored directly where they are declared
int x = 42;         // 4 bytes on the stack (or inline in a struct/array)
DateTime now = DateTime.UtcNow; // 8 bytes on the stack

// Reference types: stored on the heap, variable holds a pointer
string name = "Alice";  // 8-byte pointer on stack, object on heap
int[] numbers = [1, 2, 3]; // 8-byte pointer on stack, array on heap
```

When you put a value type into a collection like `List<int>`, the values are stored inline in the list's internal array — no heap allocation per element. When you put a reference type into `List<string>`, the list stores pointers, and the actual objects live elsewhere on the heap. This is why a `List<int>` with a million elements uses roughly 4 MB (1,000,000 × 4 bytes), while a `List<string>` with a million elements uses 8 MB for the pointers alone, plus whatever the strings themselves consume.

Boxing is what happens when you put a value type into a container that expects `object`:

```csharp
object boxed = 42;  // Allocates a new object on the heap containing the int
int unboxed = (int)boxed; // Copies the value back out

// This is why the old non-generic ArrayList was so slow for value types:
// every Add boxed, every retrieval unboxed
var oldList = new System.Collections.ArrayList();
oldList.Add(42);   // Boxing!
oldList.Add(43);   // Boxing!
int val = (int)oldList[0]; // Unboxing!
```

Generic collections like `List<int>` eliminated boxing. This single change, introduced in .NET Framework 2.0 back in 2005, was one of the most significant performance improvements in .NET history.

### Structs: User-Defined Value Types

```csharp
public readonly struct Point(double x, double y)
{
    public double X { get; } = x;
    public double Y { get; } = y;

    public double DistanceTo(Point other)
    {
        double dx = X - other.X;
        double dy = Y - other.Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }
}

// No heap allocation — stored inline
Point p1 = new(0, 0);
Point p2 = new(3, 4);
Console.WriteLine(p1.DistanceTo(p2)); // 5
```

Structs are stored inline in arrays and other value-type containers. A `Point[]` of 1,000 elements is a single contiguous block of 16,000 bytes (1,000 × 2 × 8 bytes for two doubles). This gives excellent cache locality — the CPU prefetcher can load the next elements into L1 cache before you need them.

The rules for when to use a struct versus a class are well-established:

- Use a struct when the type logically represents a single value (like a coordinate, a color, or a monetary amount).
- Use a struct when instances are small (Microsoft recommends under 16 bytes, though up to 64 bytes can be reasonable with modern hardware).
- Use a struct when instances are short-lived or embedded in other objects.
- Use `readonly struct` whenever possible to enable compiler optimizations and avoid defensive copies.

### Enums

```csharp
public enum OrderStatus : byte
{
    Pending = 0,
    Processing = 1,
    Shipped = 2,
    Delivered = 3,
    Cancelled = 4
}

[Flags]
public enum Permissions : ushort
{
    None    = 0,
    Read    = 1,
    Write   = 2,
    Execute = 4,
    Delete  = 8,
    Admin   = Read | Write | Execute | Delete
}

// Flags enums support bitwise operations
Permissions userPerms = Permissions.Read | Permissions.Write;
bool canWrite = userPerms.HasFlag(Permissions.Write); // true
bool canDelete = userPerms.HasFlag(Permissions.Delete); // false
```

Enums are value types backed by an integer type (default is `int`, but you can specify `byte`, `short`, `long`, and others). A `[Flags]` enum represents a bit field — each named value should be a power of two, and you combine them with bitwise OR. The `HasFlag` method checks whether a specific bit is set.

Under the hood, an enum is just an integer with compile-time type safety. The runtime does not enforce that an enum variable holds a named value — you can cast any integer to an enum type. This is a common source of bugs when deserializing external data.

## Part 2: Arrays — The Bedrock Data Structure

### What an Array Really Is

An array is a contiguous block of memory with a fixed number of elements, all of the same type, accessible by integer index in O(1) time.

```csharp
// Single-dimensional arrays
int[] scores = new int[5];           // 5 elements, all default (0)
int[] primes = [2, 3, 5, 7, 11];    // Collection expression (C# 12+)
string[] names = ["Alice", "Bob", "Charlie"];

// Accessing by index — O(1)
int first = primes[0];   // 2
int last = primes[^1];   // 11 (index-from-end operator)

// Slicing — creates a new array
int[] middle = primes[1..4]; // [3, 5, 7]
```

### How Arrays Are Implemented in .NET

When you write `new int[5]`, the CLR allocates a contiguous block on the managed heap. The layout looks roughly like this:

1. **Object header** (8 bytes on 64-bit) — contains the sync block index and method table pointer.
2. **Length field** (4 or 8 bytes) — stores the number of elements.
3. **Element data** — the actual values, packed contiguously.

For `new int[5]`, the element data is 5 × 4 = 20 bytes. Total allocation is approximately 36 bytes (header + length + padding + data). The key insight is that elements are stored contiguously. When the CPU reads `primes[0]`, the hardware prefetcher anticipates that you will read `primes[1]` next and loads it into cache. This is called spatial locality, and it is why array iteration is so fast.

### Multi-Dimensional and Jagged Arrays

```csharp
// Multi-dimensional (rectangular) array — single heap object
int[,] matrix = new int[3, 4];
matrix[0, 0] = 1;
matrix[2, 3] = 42;

// Jagged array — array of arrays (each row can be different length)
int[][] jagged = new int[3][];
jagged[0] = [1, 2, 3];
jagged[1] = [4, 5];
jagged[2] = [6, 7, 8, 9];
```

Here is a crucial performance detail: the CLR JIT compiler optimizes jagged arrays (`int[][]`) better than multi-dimensional arrays (`int[,]`). Multi-dimensional array access involves a method call to compute the element offset, while jagged array access compiles to a simple bounds check and pointer offset. If performance matters, prefer jagged arrays.

### Array Methods and Common Operations

```csharp
int[] data = [5, 3, 8, 1, 9, 2, 7, 4, 6];

// Sorting — O(n log n) using IntroSort (hybrid of quicksort, heapsort, insertion sort)
Array.Sort(data);
// data is now [1, 2, 3, 4, 5, 6, 7, 8, 9]

// Binary search — O(log n), array must be sorted
int index = Array.BinarySearch(data, 7); // 6

// Reversing — O(n)
Array.Reverse(data);

// Finding
int firstEven = Array.Find(data, x => x % 2 == 0); // first even number
int[] allEvens = Array.FindAll(data, x => x % 2 == 0);
bool anyNegative = Array.Exists(data, x => x < 0);

// Copying
int[] copy = new int[data.Length];
Array.Copy(data, copy, data.Length);

// Or more idiomatically in modern C#:
int[] copy2 = [.. data]; // Spread operator (C# 12+)

// Resizing (creates a new array and copies)
Array.Resize(ref data, 20); // Now has 20 elements, new ones are 0
```

### When to Use Arrays

Use arrays when:
- You know the exact number of elements at creation time.
- You need the fastest possible iteration and random access.
- You are working with interop (P/Invoke), as arrays have a predictable memory layout.
- You are building a performance-critical system and every allocation matters.

Do not use arrays when:
- You need to add or remove elements frequently. Arrays are fixed-size; every "resize" creates a new array and copies everything.
- You need to search for elements by value frequently. Linear search on an unsorted array is O(n).

## Part 3: List\<T\> — The Workhorse Collection

### What List\<T\> Really Is

`List<T>` is a dynamically-sized array. It wraps an internal `T[]` array and manages resizing automatically as you add elements.

```csharp
var orders = new List<Order>();
orders.Add(new Order("ORD-001", 29.99m));
orders.Add(new Order("ORD-002", 149.50m));
orders.Add(new Order("ORD-003", 9.99m));

// Access by index — O(1)
Order first = orders[0];

// Search — O(n)
Order? found = orders.Find(o => o.Total > 100);

// Insert at position — O(n) because elements must shift
orders.Insert(1, new Order("ORD-001a", 5.00m));

// Remove — O(n) because elements must shift
orders.RemoveAt(1);

// Count vs Capacity
Console.WriteLine($"Count: {orders.Count}");       // 3
Console.WriteLine($"Capacity: {orders.Capacity}");  // 4 (or more)
```

### How List\<T\> Is Implemented

Inside `List<T>`, the source code (which you can read on GitHub since .NET is open source) reveals:

```csharp
// Simplified view of List<T> internals
public class List<T> : IList<T>, IReadOnlyList<T>
{
    internal T[] _items;   // The backing array
    internal int _size;    // Number of elements actually in use
    private int _version;  // Incremented on every mutation (for enumerator safety)

    public void Add(T item)
    {
        if (_size == _items.Length)
        {
            Grow(_size + 1); // Double the capacity
        }
        _items[_size] = item;
        _size++;
        _version++;
    }

    private void Grow(int capacity)
    {
        int newCapacity = _items.Length == 0 ? 4 : 2 * _items.Length;
        if (newCapacity < capacity) newCapacity = capacity;
        T[] newItems = new T[newCapacity];
        Array.Copy(_items, newItems, _size);
        _items = newItems;
    }
}
```

The growth strategy is to double the array size each time it fills up. This means that `Add` is O(1) amortized — most calls are O(1) (just write to the next slot), but occasionally one call is O(n) (allocate a new array and copy everything). Over a sequence of n additions, the total work is proportional to n, so the average cost per operation is O(1).

The downside of this doubling strategy is that you can waste up to 50% of allocated memory. If you add 1,025 elements, the capacity jumps to 2,048, leaving 1,023 slots empty. If you know the final size in advance, set the capacity:

```csharp
// Pre-allocate to avoid unnecessary resizing
var customers = new List<Customer>(10_000);

// Or after populating, trim excess
customers.TrimExcess(); // Reallocates to exactly Count
```

### CollectionsMarshal: The Performance Escape Hatch

.NET provides `CollectionsMarshal` for advanced scenarios where you need direct access to a `List<T>`'s internal array:

```csharp
using System.Runtime.InteropServices;

var numbers = new List<int> { 1, 2, 3, 4, 5 };

// Get a Span<T> over the list's internal array
Span<int> span = CollectionsMarshal.AsSpan(numbers);

// Modify elements in place — no bounds checking overhead
for (int i = 0; i < span.Length; i++)
{
    span[i] *= 2;
}
// numbers is now [2, 4, 6, 8, 10]
```

This is an advanced technique. The span becomes invalid if you add or remove elements from the list (which may reallocate the backing array). Use it when you have a hot inner loop and profiling shows that bounds checking is a measurable cost.

### List\<T\> Performance Characteristics

| Operation | Time Complexity | Notes |
|---|---|---|
| `Add` (end) | O(1) amortized | Occasional O(n) for resize |
| `Insert` (middle) | O(n) | Must shift elements right |
| `RemoveAt` (middle) | O(n) | Must shift elements left |
| `this[index]` | O(1) | Direct array access |
| `Contains` / `Find` | O(n) | Linear scan |
| `Sort` | O(n log n) | IntroSort |
| `BinarySearch` | O(log n) | Requires sorted list |

## Part 4: LinkedList\<T\> — When You Need O(1) Insertion

### What It Is

`LinkedList<T>` is a doubly-linked list. Each element is wrapped in a `LinkedListNode<T>` that contains a reference to the previous node and the next node.

```csharp
var playlist = new LinkedList<string>();

// Adding elements — O(1) at either end
playlist.AddLast("Song A");
playlist.AddLast("Song B");
playlist.AddLast("Song C");
playlist.AddFirst("Intro");

// Insert relative to a known node — O(1)
LinkedListNode<string> nodeB = playlist.Find("Song B")!;
playlist.AddAfter(nodeB, "Song B (Remix)");

// Traversal
foreach (string song in playlist)
{
    Console.WriteLine(song);
}
// Intro, Song A, Song B, Song B (Remix), Song C

// Remove a known node — O(1)
playlist.Remove(nodeB);
```

### How It Is Implemented

Each `LinkedListNode<T>` is a separate heap object containing:
- `T Value` — the actual element
- `LinkedListNode<T>? Next` — pointer to next node
- `LinkedListNode<T>? Prev` — pointer to previous node
- `LinkedList<T>? List` — reference back to the owning list

This means every element carries the overhead of an object header (16 bytes on 64-bit) plus three references (24 bytes) plus the value. For a `LinkedList<int>`, each element uses roughly 48+ bytes instead of the 4 bytes it would take in a `List<int>`. The nodes are also scattered across the heap, destroying cache locality.

### When to Use LinkedList\<T\>

In practice, `LinkedList<T>` is rarely the right choice in .NET. The cache-unfriendly nature of scattered heap objects means that even O(n) operations on `List<T>` (shifting elements for insert/remove) are often faster than O(1) operations on `LinkedList<T>` for collections under a few thousand elements — because the CPU cache is that fast when data is contiguous.

Use `LinkedList<T>` only when:
- You have frequent insertions and removals in the middle of a large collection, and you already hold a reference to the node at the insertion point.
- You are implementing an LRU cache or similar structure where you need to move items between positions efficiently.

For almost everything else, `List<T>` wins.

## Part 5: Dictionary\<TKey, TValue\> — The Hash Table

### What It Is and Why It Matters

`Dictionary<TKey, TValue>` is the single most important collection in .NET application development. It provides O(1) average-case lookups, insertions, and deletions by key.

```csharp
var userCache = new Dictionary<string, UserProfile>();

// Add entries
userCache["alice"] = new UserProfile("Alice", "alice@example.com");
userCache["bob"] = new UserProfile("Bob", "bob@example.com");

// Lookup — O(1) average
if (userCache.TryGetValue("alice", out UserProfile? profile))
{
    Console.WriteLine(profile.Email);
}

// Check existence — O(1) average
bool hasBob = userCache.ContainsKey("bob"); // true

// Iterate all entries (no guaranteed order)
foreach (var (key, value) in userCache)
{
    Console.WriteLine($"{key}: {value.Email}");
}
```

### How Dictionary\<TKey, TValue\> Is Implemented

The .NET `Dictionary<TKey, TValue>` uses separate chaining with an array of buckets. Internally, it maintains two arrays:

```csharp
// Simplified internal structure
private int[] _buckets;       // Maps hash codes to entry indices
private Entry[] _entries;      // The actual key-value pairs

private struct Entry
{
    public uint hashCode;     // Hash code of the key
    public int next;          // Index of next entry in the chain (-1 if last)
    public TKey key;
    public TValue value;
}
```

When you call `dict["alice"]`:

1. The dictionary calls `"alice".GetHashCode()`, which returns an integer.
2. It computes `hashCode % _buckets.Length` to find the bucket index.
3. It follows the chain of `Entry` structs linked through their `next` fields.
4. For each entry in the chain, it calls `EqualityComparer<string>.Default.Equals(entry.key, "alice")`.
5. When it finds a match, it returns `entry.value`.

The performance depends on the hash function distributing keys evenly across buckets. A good hash function means most chains have 0 or 1 entries — O(1) lookup. A bad hash function means many keys land in the same bucket — O(n) lookup in the worst case.

### Hash Code Contracts

For any type you use as a dictionary key, you must ensure:

1. If `a.Equals(b)` returns `true`, then `a.GetHashCode()` must return the same value as `b.GetHashCode()`.
2. `GetHashCode()` must return the same value for the lifetime of the object while it is in the dictionary.
3. `GetHashCode()` should distribute values broadly across the `int` range.

```csharp
public sealed class CustomerId : IEquatable<CustomerId>
{
    public string Region { get; }
    public int Number { get; }

    public CustomerId(string region, int number)
    {
        Region = region;
        Number = number;
    }

    public bool Equals(CustomerId? other)
    {
        if (other is null) return false;
        return Region == other.Region && Number == other.Number;
    }

    public override bool Equals(object? obj) => Equals(obj as CustomerId);

    public override int GetHashCode() => HashCode.Combine(Region, Number);
}
```

The `HashCode.Combine` method (introduced in .NET Core 2.1) uses the xxHash algorithm internally and produces well-distributed hash codes. Always use it instead of writing your own hash combination logic.

### Dictionary Gotchas

**Enumeration order is not guaranteed.** Prior to .NET Core 3.0, `Dictionary<TKey, TValue>` happened to enumerate in insertion order if no deletions occurred. Many developers relied on this undocumented behavior. It is not a contract. If you need ordered enumeration, use `OrderedDictionary<TKey, TValue>` (covered in Part 9) or `SortedDictionary<TKey, TValue>`.

**Resizing is expensive.** Like `List<T>`, a dictionary doubles its internal arrays when it runs out of space. If you know the approximate number of entries, set the initial capacity:

```csharp
// Avoid unnecessary resizes
var cache = new Dictionary<string, byte[]>(estimatedCount);
```

**String keys and case sensitivity.** By default, string comparison is ordinal and case-sensitive. If you want case-insensitive keys:

```csharp
var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
headers["Content-Type"] = "application/json";
bool found = headers.ContainsKey("content-type"); // true
```

Always use `StringComparer.Ordinal` or `StringComparer.OrdinalIgnoreCase` for dictionary keys unless you have a specific reason for culture-sensitive comparison. Culture-sensitive comparisons are slower and can produce surprising results with certain Unicode characters.

## Part 6: HashSet\<T\> and SortedSet\<T\> — Collections of Unique Elements

### HashSet\<T\>

A `HashSet<T>` is a collection that stores unique elements with O(1) average-time lookups, additions, and removals. It is implemented the same way as `Dictionary<TKey, TValue>` — with a hash table — but it stores only keys, not key-value pairs.

```csharp
var activeSessions = new HashSet<string>();

activeSessions.Add("session-abc-123");
activeSessions.Add("session-def-456");
activeSessions.Add("session-abc-123"); // Ignored — already present

Console.WriteLine(activeSessions.Count); // 2
Console.WriteLine(activeSessions.Contains("session-abc-123")); // true — O(1)

// Set operations
var todaySessions = new HashSet<string> { "session-abc-123", "session-ghi-789" };
var yesterdaySessions = new HashSet<string> { "session-abc-123", "session-def-456" };

// Who was active both days?
todaySessions.IntersectWith(yesterdaySessions);
// todaySessions now contains only "session-abc-123"

// All unique sessions across both days
var allSessions = new HashSet<string>(todaySessions);
allSessions.UnionWith(yesterdaySessions);

// Who was active today but not yesterday?
var newToday = new HashSet<string> { "session-abc-123", "session-ghi-789" };
newToday.ExceptWith(yesterdaySessions);
// newToday now contains only "session-ghi-789"
```

### SortedSet\<T\>

`SortedSet<T>` stores unique elements in sorted order. It is implemented as a red-black tree, which provides O(log n) lookups, additions, and removals with guaranteed sorted enumeration.

```csharp
var leaderboard = new SortedSet<int> { 100, 85, 92, 78, 95, 88 };

// Elements are always in sorted order
foreach (int score in leaderboard)
{
    Console.Write($"{score} "); // 78 85 88 92 95 100
}

// Range queries
SortedSet<int> topScores = leaderboard.GetViewBetween(90, 100);
// Contains: 92, 95, 100

// Min and Max — O(log n)
Console.WriteLine(leaderboard.Min); // 78
Console.WriteLine(leaderboard.Max); // 100
```

### When to Use Each

Use `HashSet<T>` when you need fast membership testing and do not care about order. Use `SortedSet<T>` when you need elements to be maintained in sorted order or you need range queries. If you just need to check "is this value in the set?" — `HashSet<T>` is faster by a constant factor because hash table lookups are O(1) versus O(log n) for tree lookups.

## Part 7: Stack\<T\> and Queue\<T\> — LIFO and FIFO

### Stack\<T\> — Last In, First Out

A stack is a collection where the last element added is the first one removed. Think of a stack of plates — you add plates to the top and take them from the top.

```csharp
var undoHistory = new Stack<string>();

undoHistory.Push("Typed 'Hello'");
undoHistory.Push("Changed font to bold");
undoHistory.Push("Deleted paragraph");

// Undo the most recent action
string lastAction = undoHistory.Pop(); // "Deleted paragraph"

// Peek without removing
string nextUndo = undoHistory.Peek(); // "Changed font to bold"

Console.WriteLine(undoHistory.Count); // 2
```

Internally, `Stack<T>` is backed by an array with a pointer to the top. `Push` and `Pop` are O(1) amortized (with occasional O(n) resizes, just like `List<T>`).

### Queue\<T\> — First In, First Out

A queue is a collection where the first element added is the first one removed. Think of a line at a coffee shop — first in line, first served.

```csharp
var printQueue = new Queue<PrintJob>();

printQueue.Enqueue(new PrintJob("Report.pdf", 10));
printQueue.Enqueue(new PrintJob("Invoice.pdf", 2));
printQueue.Enqueue(new PrintJob("Manual.pdf", 100));

// Process in order
while (printQueue.Count > 0)
{
    PrintJob job = printQueue.Dequeue();
    Console.WriteLine($"Printing {job.FileName} ({job.Pages} pages)");
}
// Report.pdf, Invoice.pdf, Manual.pdf

record PrintJob(string FileName, int Pages);
```

Internally, `Queue<T>` uses a circular buffer — an array with a head and tail index. When the tail wraps around the end of the array, it continues from the beginning. This avoids the O(n) shifting that would be needed with a simple array-based queue. Both `Enqueue` and `Dequeue` are O(1) amortized.

### PriorityQueue\<TElement, TPriority\> — The Heap

.NET 6 introduced `PriorityQueue<TElement, TPriority>`, which dequeues elements in priority order rather than insertion order. It is implemented as a min-heap (a binary heap stored in an array).

```csharp
var taskQueue = new PriorityQueue<string, int>();

// Lower number = higher priority
taskQueue.Enqueue("Fix critical bug", 1);
taskQueue.Enqueue("Write documentation", 5);
taskQueue.Enqueue("Code review", 3);
taskQueue.Enqueue("Deploy hotfix", 1);
taskQueue.Enqueue("Refactor module", 4);

while (taskQueue.Count > 0)
{
    string task = taskQueue.Dequeue();
    Console.WriteLine(task);
}
// Fix critical bug
// Deploy hotfix
// Code review
// Refactor module
// Write documentation
```

The time complexity: `Enqueue` is O(log n) (heap bubble-up), `Dequeue` is O(log n) (heap bubble-down), and `Peek` is O(1) (just return the root). This is vastly better than using a sorted list, which would be O(n) for insertion.

Note that `PriorityQueue` does not guarantee any particular order among elements with the same priority. If you enqueue "Fix critical bug" and "Deploy hotfix" both with priority 1, either one could come out first. This is by design — maintaining stable ordering would require additional overhead.

## Part 8: SortedList\<TKey, TValue\> and SortedDictionary\<TKey, TValue\> — Sorted Key-Value Pairs

### SortedDictionary\<TKey, TValue\>

`SortedDictionary<TKey, TValue>` stores key-value pairs sorted by key. It is implemented as a red-black tree (a self-balancing binary search tree).

```csharp
var eventLog = new SortedDictionary<DateTime, string>();

eventLog[new DateTime(2026, 4, 9, 14, 30, 0)] = "Deployment started";
eventLog[new DateTime(2026, 4, 9, 14, 25, 0)] = "Build completed";
eventLog[new DateTime(2026, 4, 9, 14, 35, 0)] = "Health check passed";

// Iteration is always in key order
foreach (var (timestamp, message) in eventLog)
{
    Console.WriteLine($"[{timestamp:HH:mm:ss}] {message}");
}
// [14:25:00] Build completed
// [14:30:00] Deployment started
// [14:35:00] Health check passed
```

Time complexity: O(log n) for `Add`, `Remove`, `ContainsKey`, and `TryGetValue`. Enumeration is O(n) in sorted order.

### SortedList\<TKey, TValue\>

`SortedList<TKey, TValue>` also stores sorted key-value pairs, but it uses two parallel sorted arrays internally (one for keys, one for values) with binary search for lookups.

```csharp
var config = new SortedList<string, string>
{
    ["database.host"] = "localhost",
    ["database.port"] = "5432",
    ["app.name"] = "My Blazor Magazine",
    ["app.version"] = "1.0.0"
};

// Access by index (not available on SortedDictionary!)
string firstKey = config.Keys[0];     // "app.name"
string firstValue = config.Values[0]; // "My Blazor Magazine"

// Binary search lookup — O(log n)
if (config.TryGetValue("database.host", out string? host))
{
    Console.WriteLine(host); // localhost
}
```

### SortedList vs SortedDictionary: When to Use Which

| Feature | SortedList | SortedDictionary |
|---|---|---|
| **Implementation** | Sorted arrays + binary search | Red-black tree |
| **Lookup** | O(log n) | O(log n) |
| **Insert** | O(n) (must shift array elements) | O(log n) |
| **Remove** | O(n) | O(log n) |
| **Memory** | Less (two arrays, no node overhead) | More (tree nodes are heap objects) |
| **Access by index** | Yes (`Keys[i]`, `Values[i]`) | No |
| **Enumeration** | Faster (arrays have cache locality) | Slower (tree nodes scattered in heap) |

Use `SortedList` when you populate the collection once (or rarely modify it) and then read from it frequently. Use `SortedDictionary` when you need frequent insertions and deletions.

## Part 9: OrderedDictionary\<TKey, TValue\> — Insertion-Order Preservation

.NET 9 introduced the generic `OrderedDictionary<TKey, TValue>` — a long-awaited addition that preserves insertion order while providing O(1) hash-based lookups. Before .NET 9, the only option was the non-generic `System.Collections.Specialized.OrderedDictionary`, which stored keys and values as `object` and required boxing for value types.

```csharp
using System.Collections.Generic;

var pipeline = new OrderedDictionary<string, Func<HttpContext, Task>>
{
    ["authentication"] = ctx => AuthenticateAsync(ctx),
    ["authorization"]  = ctx => AuthorizeAsync(ctx),
    ["routing"]        = ctx => RouteAsync(ctx),
    ["endpoint"]       = ctx => ExecuteEndpointAsync(ctx),
    ["response"]       = ctx => WriteResponseAsync(ctx)
};

// Iteration preserves insertion order
foreach (var (name, middleware) in pipeline)
{
    Console.WriteLine(name);
}
// authentication, authorization, routing, endpoint, response

// Access by key — O(1)
var routingStep = pipeline["routing"];

// Access by index — O(1)
var firstStep = pipeline.GetAt(0);

// Insert at specific position — O(n)
pipeline.Insert(2, "logging", ctx => LogAsync(ctx));
```

Internally, `OrderedDictionary<TKey, TValue>` maintains both a hash table (for O(1) key lookups) and a list structure (for O(1) index access and ordered enumeration). This uses more memory than a plain `Dictionary<TKey, TValue>`, but it gives you the combination of fast lookups and predictable iteration order that many application scenarios require.

## Part 10: Span\<T\> and Memory\<T\> — Zero-Allocation Slicing

### The Problem They Solve

Every time you call `array.Skip(10).Take(5).ToArray()` in a hot path, you allocate a new array. Every time you call `string.Substring(10, 5)`, you allocate a new string on the heap. In a web server handling thousands of requests per second, these allocations add up and put pressure on the garbage collector.

`Span<T>` and `ReadOnlySpan<T>` solve this by providing a view into existing memory without copying or allocating.

```csharp
int[] data = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9];

// Create a span over a slice — no allocation, no copy
Span<int> slice = data.AsSpan(3, 4); // View of elements [3, 4, 5, 6]

// Modify through the span — modifies the original array
slice[0] = 99;
Console.WriteLine(data[3]); // 99

// ReadOnlySpan for strings — no allocation substring
ReadOnlySpan<char> greeting = "Hello, World!".AsSpan();
ReadOnlySpan<char> world = greeting[7..12]; // "World" — no allocation

// Parsing without allocation
ReadOnlySpan<char> csvLine = "42,3.14,true".AsSpan();
int commaIndex = csvLine.IndexOf(',');
int firstValue = int.Parse(csvLine[..commaIndex]); // 42

// Span works with stackalloc
Span<byte> buffer = stackalloc byte[256];
buffer[0] = 0xFF;
```

### The Key Constraint

`Span<T>` is a `ref struct`, which means it can only live on the stack. You cannot store it in a field of a class, you cannot box it, you cannot use it in async methods, and you cannot put it in a collection. These constraints exist because `Span<T>` can point to stack-allocated memory (`stackalloc`) or interior pointers into objects, and the GC cannot track these references if they escape the stack frame.

When you need to store a reference to a region of memory in a field or pass it across `await` boundaries, use `Memory<T>`:

```csharp
public class BufferPool
{
    private Memory<byte> _buffer;

    public BufferPool(int size)
    {
        _buffer = new byte[size]; // Implicit conversion from T[] to Memory<T>
    }

    public Memory<byte> Rent(int offset, int length)
    {
        return _buffer.Slice(offset, length);
    }

    public async Task ProcessAsync(Memory<byte> chunk)
    {
        // Memory<T> can cross await boundaries — Span<T> cannot
        await Task.Delay(100);
        chunk.Span[0] = 42; // Access the underlying Span
    }
}
```

### C# 14 Implicit Span Conversions

C# 14, which shipped with .NET 10 in November 2025, added implicit conversions between arrays, `Span<T>`, and `ReadOnlySpan<T>`. This makes span-based APIs significantly more ergonomic:

```csharp
// Before C# 14 — explicit conversion needed
void ProcessData(ReadOnlySpan<int> data) { /* ... */ }
int[] numbers = [1, 2, 3];
ProcessData(numbers.AsSpan()); // Had to call .AsSpan()

// C# 14 — implicit conversion
ProcessData(numbers); // Just works — implicit T[] → ReadOnlySpan<T>

// Slicing is also implicit
ProcessData(numbers[1..3]); // T[] slice → ReadOnlySpan<T>
```

This is one of the most practically significant changes in C# 14 for performance-sensitive code. Library authors can now write `Span<T>`-based APIs and callers can pass arrays directly.

## Part 11: Immutable Collections — Thread Safety by Design

### The System.Collections.Immutable Namespace

Immutable collections create a new instance whenever you modify them, leaving the original unchanged. This makes them inherently thread-safe — multiple threads can read the same collection without locks because nobody can modify it.

```csharp
using System.Collections.Immutable;

// Create an immutable list
ImmutableList<string> original = ["Alice", "Bob", "Charlie"];

// "Add" returns a new list — original is unchanged
ImmutableList<string> withDave = original.Add("Dave");

Console.WriteLine(original.Count);  // 3
Console.WriteLine(withDave.Count);  // 4

// Same pattern for all operations
ImmutableList<string> withoutBob = original.Remove("Bob");
ImmutableList<string> sorted = original.Sort();
```

### The Immutable Collection Types

| Type | Mutable Equivalent | Notes |
|---|---|---|
| `ImmutableArray<T>` | `T[]` | Thin wrapper around array; fastest iteration |
| `ImmutableList<T>` | `List<T>` | Balanced tree; O(log n) operations |
| `ImmutableDictionary<TKey, TValue>` | `Dictionary<TKey, TValue>` | Hash-based; O(log n) operations |
| `ImmutableHashSet<T>` | `HashSet<T>` | Hash-based; O(log n) operations |
| `ImmutableSortedDictionary<TKey, TValue>` | `SortedDictionary<TKey, TValue>` | Sorted by key; O(log n) |
| `ImmutableSortedSet<T>` | `SortedSet<T>` | Sorted; O(log n) |
| `ImmutableStack<T>` | `Stack<T>` | O(1) push/pop |
| `ImmutableQueue<T>` | `Queue<T>` | O(1) amortized enqueue/dequeue |

### How Structural Sharing Works

Immutable collections avoid the O(n) cost of copying everything on every modification by using structural sharing. `ImmutableList<T>` is implemented as a balanced binary tree (AVL tree). When you add an element, only the nodes along the path from the root to the insertion point are replaced — the rest of the tree is shared between the old and new versions.

```
Original tree:           After adding "Dave":
     B                        B (new)
    / \                      / \
   A   C                   A   C (new)
                                 \
                                  D (new)
```

Only three nodes are created. The "A" node is shared between both versions. This is why `ImmutableList<T>` operations are O(log n) — the tree height is logarithmic.

### ImmutableArray\<T\> — The Lightweight Option

`ImmutableArray<T>` is a special case. Unlike `ImmutableList<T>`, it is backed by a plain array with no tree structure. It is a `readonly struct` wrapper around `T[]`, making it the most memory-efficient immutable collection for read-heavy scenarios.

```csharp
ImmutableArray<int> primes = [2, 3, 5, 7, 11];

// Iteration is as fast as a regular array
foreach (int p in primes)
{
    Console.Write($"{p} ");
}

// But modification creates a full copy — O(n)
ImmutableArray<int> withThirteen = primes.Add(13);
```

Use `ImmutableArray<T>` when you build the collection once and then only read from it. Use `ImmutableList<T>` when you need to make frequent modifications and want the O(log n) structural sharing.

### Building Immutable Collections Efficiently

Never build an immutable collection by calling `.Add()` in a loop — that creates a new instance on every iteration. Use a builder instead:

```csharp
// Bad — O(n²) for ImmutableList, O(n) allocations
ImmutableList<int> bad = ImmutableList<int>.Empty;
for (int i = 0; i < 10_000; i++)
{
    bad = bad.Add(i); // New tree on every iteration
}

// Good — O(n) with a single final build
ImmutableList<int>.Builder builder = ImmutableList.CreateBuilder<int>();
for (int i = 0; i < 10_000; i++)
{
    builder.Add(i); // Mutable operations internally
}
ImmutableList<int> good = builder.ToImmutable(); // Single conversion
```

## Part 12: Frozen Collections — Read-Optimized Immutability

### What They Are

.NET 8 introduced `FrozenDictionary<TKey, TValue>` and `FrozenSet<T>` in the `System.Collections.Frozen` namespace. These collections are designed for a specific scenario: you create the collection once at application startup, and then read from it for the lifetime of the application.

```csharp
using System.Collections.Frozen;

// Build from an existing dictionary
var mutableConfig = new Dictionary<string, string>
{
    ["Database:Host"] = "db.example.com",
    ["Database:Port"] = "5432",
    ["App:Name"] = "My Blazor Magazine",
    ["App:Version"] = "2.0.0",
    ["Feature:DarkMode"] = "true"
};

// Freeze it — expensive creation, extremely fast reads
FrozenDictionary<string, string> config = mutableConfig.ToFrozenDictionary();

// Lookups are faster than Dictionary<TKey, TValue>
if (config.TryGetValue("Database:Host", out string? host))
{
    Console.WriteLine(host); // db.example.com
}
```

### How They Achieve Faster Reads

When you call `.ToFrozenDictionary()`, the runtime analyzes the actual keys you provide and generates an optimized lookup strategy tailored to those specific keys. For string keys, it might:

- Compute a perfect hash function that maps each key to a unique bucket with no collisions.
- Choose hash function parameters based on the actual character distribution in the keys.
- Use specialized comparison logic based on key lengths.

This analysis is expensive at creation time, but the resulting lookup function is faster than `Dictionary<TKey, TValue>` because it avoids collision handling entirely. Benchmarks show that `FrozenDictionary` lookups can be 40-50% faster than `Dictionary` lookups for typical workloads.

### When to Use Frozen Collections

Use `FrozenDictionary<TKey, TValue>` and `FrozenSet<T>` when:
- The data is created once (at startup, from configuration, from a database load) and never modified.
- The collection is read frequently — ideally thousands or millions of times per second.
- You are willing to spend extra time at initialization for faster reads at runtime.

Examples: configuration dictionaries, route tables, permission lookups, feature flag registries, static lookup tables.

Do not use frozen collections when:
- The data changes frequently. You cannot modify a frozen collection — you must create a new one from scratch.
- The collection is small (under ~10 entries). The optimization overhead may not be worthwhile for tiny collections.
- Creation time matters. `ToFrozenDictionary()` can be 5-10x slower than creating a regular `Dictionary`.

## Part 13: Concurrent Collections — Thread-Safe Without Locks

### The Problem

When multiple threads read and write the same `Dictionary<TKey, TValue>` concurrently, the internal state can become corrupted. This leads to infinite loops, lost data, or crashes — and the bugs are intermittent and nearly impossible to reproduce. The `System.Collections.Concurrent` namespace provides collections designed for multi-threaded access.

### ConcurrentDictionary\<TKey, TValue\>

```csharp
using System.Collections.Concurrent;

var pageViews = new ConcurrentDictionary<string, int>();

// Thread-safe add-or-update
Parallel.For(0, 1_000_000, i =>
{
    string page = $"/page/{i % 100}";
    pageViews.AddOrUpdate(
        page,
        addValue: 1,
        updateValueFactory: (key, oldValue) => oldValue + 1
    );
});

foreach (var (page, count) in pageViews.OrderByDescending(kv => kv.Value).Take(5))
{
    Console.WriteLine($"{page}: {count} views");
}
```

`ConcurrentDictionary` uses fine-grained locking — it locks individual hash buckets rather than the entire collection. This means multiple threads can read and write simultaneously as long as they are accessing different buckets. It also provides atomic operations like `AddOrUpdate`, `GetOrAdd`, and `TryRemove` that would require external locking with a regular `Dictionary`.

### ConcurrentQueue\<T\> and ConcurrentStack\<T\>

```csharp
var workItems = new ConcurrentQueue<WorkItem>();

// Producer threads enqueue work
Task.Run(() =>
{
    for (int i = 0; i < 1000; i++)
    {
        workItems.Enqueue(new WorkItem($"Task-{i}"));
    }
});

// Consumer threads dequeue work
Task.Run(() =>
{
    while (workItems.TryDequeue(out WorkItem? item))
    {
        ProcessItem(item);
    }
});
```

Both `ConcurrentQueue<T>` and `ConcurrentStack<T>` are lock-free — they use atomic compare-and-swap (CAS) operations instead of locks. This makes them extremely fast under contention because threads never block waiting for a lock.

### ConcurrentBag\<T\>

`ConcurrentBag<T>` is a thread-safe, unordered collection optimized for scenarios where the same thread that produces items also consumes them. It uses thread-local storage internally, so each thread has its own private list. This minimizes contention because most operations only touch the thread's local list.

```csharp
var results = new ConcurrentBag<AnalysisResult>();

Parallel.ForEach(dataSets, dataSet =>
{
    var result = Analyze(dataSet);
    results.Add(result);
});

// Process all results after parallel work completes
foreach (var result in results)
{
    SaveToDatabase(result);
}
```

Use `ConcurrentBag<T>` for producer-consumer scenarios where order does not matter and the same thread tends to produce and consume. Use `ConcurrentQueue<T>` when you need FIFO order. Use `ConcurrentStack<T>` when you need LIFO order.

### Channel\<T\> — The Modern Producer-Consumer Primitive

While `ConcurrentQueue<T>` works, modern .NET code typically uses `System.Threading.Channels.Channel<T>` for producer-consumer patterns, especially with async code:

```csharp
using System.Threading.Channels;

var channel = Channel.CreateBounded<LogEntry>(new BoundedChannelOptions(1000)
{
    FullMode = BoundedChannelFullMode.Wait,
    SingleReader = true,
    SingleWriter = false
});

// Multiple producers (e.g., request handlers)
async Task ProduceAsync(string message)
{
    await channel.Writer.WriteAsync(new LogEntry(DateTime.UtcNow, message));
}

// Single consumer (e.g., background log writer)
async Task ConsumeAsync(CancellationToken ct)
{
    await foreach (LogEntry entry in channel.Reader.ReadAllAsync(ct))
    {
        await WriteToFileAsync(entry);
    }
}

record LogEntry(DateTime Timestamp, string Message);
```

`Channel<T>` provides backpressure (the producer waits when the buffer is full), supports async/await natively, and can be configured as bounded or unbounded, single-reader or multi-reader. It is the recommended approach for producer-consumer patterns in modern .NET.

## Part 14: BitArray and BitVector32 — Efficient Boolean Storage

### BitArray

Remember how we said `bool` takes 1 byte, not 1 bit? `BitArray` stores booleans using 1 bit per value, giving you 8x memory efficiency.

```csharp
// Store 1 million boolean flags using only ~125 KB instead of ~1 MB
var flags = new BitArray(1_000_000, defaultValue: false);

flags[0] = true;
flags[999_999] = true;

// Bitwise operations on entire arrays
var mask = new BitArray(1_000_000, defaultValue: true);
flags.And(mask);  // Bitwise AND
flags.Or(mask);   // Bitwise OR
flags.Xor(mask);  // Bitwise XOR
flags.Not();      // Bitwise NOT
```

Use `BitArray` when you have a large number of boolean flags and memory is a concern — for example, a sieve of Eratosthenes, a Bloom filter, or tracking which records in a batch have been processed.

### BitVector32

`BitVector32` is a 32-bit structure that provides efficient access to individual bits or small groups of bits within a single `int`. It is useful for packing multiple small fields into a single value.

```csharp
using System.Collections.Specialized;

// Create sections (groups of bits)
BitVector32.Section daySection = BitVector32.CreateSection(31);      // 5 bits (0-31)
BitVector32.Section monthSection = BitVector32.CreateSection(12, daySection); // 4 bits (0-12)
BitVector32.Section yearSection = BitVector32.CreateSection(127, monthSection); // 7 bits (0-127, for year offset)

var date = new BitVector32(0);
date[daySection] = 9;
date[monthSection] = 4;
date[yearSection] = 26; // 2000 + 26 = 2026

Console.WriteLine($"Day: {date[daySection]}");     // 9
Console.WriteLine($"Month: {date[monthSection]}"); // 4
Console.WriteLine($"Year: 20{date[yearSection]:D2}"); // 2026
```

## Part 15: Specialized String Collections

### StringBuilder — Mutable String Construction

We mentioned `StringBuilder` briefly in Part 1. Here is a more thorough look:

```csharp
var sb = new StringBuilder(capacity: 1024);

sb.Append("SELECT ");
sb.AppendJoin(", ", new[] { "Id", "Name", "Email", "CreatedAt" });
sb.Append(" FROM Users");
sb.Append(" WHERE IsActive = 1");

if (hasFilter)
{
    sb.Append(" AND Name LIKE @Filter");
}

sb.Append(" ORDER BY CreatedAt DESC");
sb.Append(" OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY");

string sql = sb.ToString();
```

Internally, `StringBuilder` uses a linked list of character buffers. When one buffer fills up, it allocates a new one and chains it to the previous. This avoids the O(n) copy that `string` concatenation requires on every operation.

In .NET 6+, you can also use `string.Create` with `ISpanFormattable` for allocation-free string building, and in .NET 8+ the `DefaultInterpolatedStringHandler` makes interpolated strings faster than ever.

### StringValues — ASP.NET Core's Multi-Value String

ASP.NET Core uses `Microsoft.Extensions.Primitives.StringValues` extensively for headers and query parameters because a single header can have multiple values:

```csharp
using Microsoft.Extensions.Primitives;

// Single value — no array allocation
StringValues single = "text/html";

// Multiple values — backed by a string[]
StringValues multiple = new string[] { "text/html", "application/json" };

// Implicit conversion from string
StringValues fromString = "gzip";

// Used in ASP.NET Core request handling
StringValues acceptHeaders = context.Request.Headers.Accept;
foreach (string? value in acceptHeaders)
{
    Console.WriteLine(value);
}
```

`StringValues` is a `readonly struct` that holds either a single `string` or a `string[]`. It avoids unnecessary array allocations for the common case of a single value.

## Part 16: Tuples — Lightweight Grouping

### Value Tuples

C# tuples are value types that let you group multiple values without defining a dedicated class or struct:

```csharp
// Named tuple elements
(string Name, int Age, decimal Salary) employee = ("Alice", 30, 85_000m);
Console.WriteLine($"{employee.Name} is {employee.Age} years old");

// Tuple deconstruction
var (name, age, salary) = employee;

// Method returning multiple values
(int Min, int Max, double Average) AnalyzeScores(int[] scores)
{
    return (scores.Min(), scores.Max(), scores.Average());
}

var stats = AnalyzeScores([85, 92, 78, 95, 88]);
Console.WriteLine($"Min: {stats.Min}, Max: {stats.Max}, Avg: {stats.Average:F1}");
```

Under the hood, value tuples are `System.ValueTuple<T1, T2, ...>` structs. They are value types, so they are stored inline with no heap allocation. The named element syntax (`Name`, `Age`) is purely a compiler feature — the names exist only in source code and metadata; at runtime, the fields are just `Item1`, `Item2`, and so on.

### When to Use Tuples vs. Records

Use tuples for temporary, local groupings — return values from private methods, intermediate results in a computation. Use records when the grouping has domain meaning and you want named types, pattern matching, and persistence:

```csharp
// Tuple: fine for local use
var (lat, lng) = ParseCoordinates(input);

// Record: better for domain types
public record Coordinate(double Latitude, double Longitude);
```

## Part 17: Records — Immutable Data Carriers

### Record Classes and Record Structs

C# records are types designed to carry data with value-based equality semantics:

```csharp
// Record class — reference type with value semantics
public record UserDto(string Name, string Email, DateTime CreatedAt);

// Record struct — value type with value semantics
public readonly record struct Point(double X, double Y);

// Records provide value-based equality
var a = new UserDto("Alice", "alice@example.com", DateTime.UtcNow);
var b = new UserDto("Alice", "alice@example.com", a.CreatedAt);
Console.WriteLine(a == b); // true (compares values, not references)

// Non-destructive mutation with 'with' expression
var updated = a with { Email = "newalice@example.com" };
Console.WriteLine(a.Email);       // alice@example.com (unchanged)
Console.WriteLine(updated.Email); // newalice@example.com
```

Records automatically generate `Equals`, `GetHashCode`, `ToString`, and a `Deconstruct` method. This makes them excellent dictionary keys and set elements — the hash code is computed from all properties, and equality compares all property values.

```csharp
// Records work beautifully as dictionary keys
var cache = new Dictionary<UserDto, CachedResponse>();
cache[new UserDto("Alice", "a@b.com", date)] = cachedResponse;

// Later, a different instance with the same values finds the entry
bool found = cache.TryGetValue(new UserDto("Alice", "a@b.com", date), out var response);
// found == true
```

## Part 18: Arrays of Complex Types — Understanding Memory Layout

### How the CLR Lays Out Arrays of Value Types vs. Reference Types

This is a topic many developers overlook, but it has enormous performance implications.

```csharp
// Array of value types — all data is inline, contiguous
readonly record struct Pixel(byte R, byte G, byte B, byte A);
Pixel[] image = new Pixel[1920 * 1080];
// Memory layout: [RGBA|RGBA|RGBA|RGBA|...]
// Total: ~8 MB (2,073,600 × 4 bytes) in one contiguous block
// CPU cache prefetcher loves this

// Array of reference types — only pointers are contiguous
record PixelClass(byte R, byte G, byte B, byte A);
PixelClass[] imageRef = new PixelClass[1920 * 1080];
// Memory layout: [ptr|ptr|ptr|ptr|...]
// Each pointer leads to a separate heap object: [header|R|G|B|A]
// Total: ~16 MB for pointers + ~50 MB for objects = ~66 MB
// CPU cache misses on every access because objects are scattered
```

This is why game developers, image processing libraries, and high-performance computing code in .NET use structs extensively. The memory layout difference between a value-type array and a reference-type array can mean a 10x performance difference for iteration-heavy code.

### InlineArray — Fixed-Size Buffers in Structs

.NET 8 introduced the `[InlineArray]` attribute for creating fixed-size buffers within structs:

```csharp
[System.Runtime.CompilerServices.InlineArray(16)]
public struct FixedBuffer16
{
    private byte _element;
}

// Usage — 16 bytes stored inline in the struct, no heap allocation
FixedBuffer16 buffer = new();
Span<byte> span = buffer; // Implicit conversion to Span
span[0] = 42;
span[15] = 255;
```

This is useful for embedding small fixed-size buffers in structs without resorting to `unsafe` code or `stackalloc`.

## Part 19: Read-Only Wrappers and Interfaces

### The Read-Only Collection Hierarchy

.NET provides a hierarchy of interfaces for read-only access to collections:

```csharp
// IEnumerable<T> — the most basic: forward-only iteration
IEnumerable<Order> LazyOrders()
{
    yield return new Order("ORD-001", 29.99m);
    yield return new Order("ORD-002", 149.50m);
}

// IReadOnlyCollection<T> — adds Count
IReadOnlyCollection<Order> orders = new List<Order>
{
    new("ORD-001", 29.99m),
    new("ORD-002", 149.50m)
};
int count = orders.Count; // O(1)

// IReadOnlyList<T> — adds indexer
IReadOnlyList<Order> orderList = orders.ToList();
Order first = orderList[0]; // O(1)

// IReadOnlyDictionary<TKey, TValue> — read-only dictionary access
IReadOnlyDictionary<string, Order> orderLookup =
    new Dictionary<string, Order> { ["ORD-001"] = new("ORD-001", 29.99m) };
```

### ReadOnlyCollection\<T\> and ReadOnlyDictionary\<TKey, TValue\>

These are concrete wrappers that prevent modification through the wrapper while allowing the original collection to be modified:

```csharp
var internalList = new List<string> { "Alice", "Bob" };
var readOnly = internalList.AsReadOnly(); // ReadOnlyCollection<string>

// readOnly.Add("Charlie"); // Compile error — no Add method
// But modifying the underlying list is reflected:
internalList.Add("Charlie");
Console.WriteLine(readOnly.Count); // 3
```

### ReadOnlySet\<T\> — New in .NET 9

.NET 9 added `ReadOnlySet<T>` to provide a read-only wrapper for `ISet<T>`, completing the read-only wrapper trio:

```csharp
var mutableSet = new HashSet<string> { "admin", "editor", "viewer" };
var readOnlySet = new ReadOnlySet<string>(mutableSet);

bool isAdmin = readOnlySet.Contains("admin"); // true
// readOnlySet.Add("superadmin"); // Compile error
```

### Choosing the Right Return Type for APIs

When designing public APIs, return the most restrictive interface that the caller needs:

```csharp
public class OrderService
{
    private readonly List<Order> _orders = new();

    // Return IReadOnlyList<T> — callers can index and count, but not modify
    public IReadOnlyList<Order> GetRecentOrders(int count)
    {
        return _orders.OrderByDescending(o => o.Date).Take(count).ToList();
    }

    // Return IEnumerable<T> for lazy/streaming results
    public IEnumerable<Order> GetAllOrders()
    {
        foreach (var order in _orders)
        {
            yield return order;
        }
    }
}
```

This is a practical application of the Interface Segregation Principle. By returning `IReadOnlyList<Order>` instead of `List<Order>`, you communicate that the caller should not (and cannot) modify the returned collection.

## Part 20: LINQ — Querying Any Data Structure

### How LINQ Works Under the Hood

LINQ (Language-Integrated Query) is not a data structure, but it is the universal way to query data structures in .NET. Understanding how it works helps you avoid performance traps.

```csharp
var orders = new List<Order>
{
    new("ORD-001", "Alice", 29.99m, OrderStatus.Shipped),
    new("ORD-002", "Bob", 149.50m, OrderStatus.Processing),
    new("ORD-003", "Alice", 9.99m, OrderStatus.Delivered),
    new("ORD-004", "Charlie", 299.00m, OrderStatus.Pending),
    new("ORD-005", "Alice", 49.99m, OrderStatus.Shipped),
};

// Method syntax (preferred by most .NET developers)
var aliceShipped = orders
    .Where(o => o.Customer == "Alice" && o.Status == OrderStatus.Shipped)
    .OrderByDescending(o => o.Total)
    .Select(o => new { o.Id, o.Total })
    .ToList();

// Query syntax (SQL-like, less common)
var query = from o in orders
            where o.Customer == "Alice" && o.Status == OrderStatus.Shipped
            orderby o.Total descending
            select new { o.Id, o.Total };
```

LINQ uses deferred execution — calling `.Where()` and `.Select()` does not execute anything. It builds a chain of iterator objects. The actual iteration happens only when you enumerate the result (with `foreach`, `.ToList()`, `.First()`, and similar).

### LINQ Performance Considerations

```csharp
// Dangerous — evaluates the entire query for every call to Count and indexer
IEnumerable<Order> filtered = orders.Where(o => o.Total > 100);
int count = filtered.Count();    // Iterates all elements
var first = filtered.First();    // Iterates from the beginning again

// Better — materialize once, reuse
List<Order> materialized = orders.Where(o => o.Total > 100).ToList();
int count2 = materialized.Count;  // O(1) — stored in list
var first2 = materialized[0];     // O(1) — direct index access
```

### LINQ with Different Collections

LINQ works with any `IEnumerable<T>`, but the performance characteristics depend on the underlying collection:

- `.Contains()` on a `List<T>` is O(n). On a `HashSet<T>`, the LINQ `.Contains()` extension method is smart enough to call the native `Contains`, which is O(1).
- `.Count()` on an `ICollection<T>` (like `List<T>` or `HashSet<T>`) is O(1) because it reads the `Count` property directly. On a plain `IEnumerable<T>` from a `yield return` method, it is O(n) because it must enumerate everything.
- `.OrderBy()` is always O(n log n) regardless of the source collection.

## Part 21: ArrayPool\<T\> and MemoryPool\<T\> — Renting Instead of Allocating

### The Allocation Problem in Hot Paths

In a web server processing 10,000 requests per second, each request might need a temporary buffer of 4 KB. That is 40 MB/second of allocations that the garbage collector must eventually clean up. Array pooling eliminates these allocations by reusing buffers.

```csharp
using System.Buffers;

// Rent a buffer from the shared pool
byte[] buffer = ArrayPool<byte>.Shared.Rent(4096);
try
{
    // Use the buffer
    int bytesRead = await stream.ReadAsync(buffer.AsMemory(0, 4096));
    ProcessData(buffer.AsSpan(0, bytesRead));
}
finally
{
    // Return the buffer to the pool
    ArrayPool<byte>.Shared.Return(buffer, clearArray: true);
}
```

Important caveats:
- The returned array may be larger than requested. `Rent(4096)` might return an array of length 4,096, 8,192, or even larger. Always track the actual length you need separately.
- Always return rented arrays in a `finally` block. Failing to return them causes the pool to grow unboundedly.
- Pass `clearArray: true` when returning buffers that contained sensitive data.

### MemoryPool\<T\>

`MemoryPool<T>` is the `Memory<T>` equivalent of `ArrayPool<T>`. It returns `IMemoryOwner<T>` instances that implement `IDisposable`:

```csharp
using System.Buffers;

using IMemoryOwner<byte> owner = MemoryPool<byte>.Shared.Rent(4096);
Memory<byte> memory = owner.Memory[..4096]; // Slice to the size we need

// Use memory in async code
await ProcessAsync(memory);
// Disposal returns the memory to the pool
```

## Part 22: Choosing the Right Data Structure — A Decision Guide

Here is a practical decision tree for choosing the right collection:

**Do you need key-value pairs?**

- Yes, with O(1) lookups → `Dictionary<TKey, TValue>`
- Yes, with sorted keys → `SortedDictionary<TKey, TValue>` (frequent modifications) or `SortedList<TKey, TValue>` (infrequent modifications)
- Yes, with insertion-order preservation → `OrderedDictionary<TKey, TValue>` (.NET 9+)
- Yes, read-only after creation → `FrozenDictionary<TKey, TValue>` (.NET 8+)
- Yes, thread-safe → `ConcurrentDictionary<TKey, TValue>`

**Do you need a sequence of elements?**

- Yes, with fast random access → `List<T>` or `T[]`
- Yes, with fast insertion/removal at arbitrary positions → `LinkedList<T>` (only with node references)
- Yes, immutable → `ImmutableArray<T>` (read-heavy) or `ImmutableList<T>` (modification-heavy)
- Yes, thread-safe producer-consumer → `Channel<T>` or `ConcurrentQueue<T>`

**Do you need unique elements?**

- Yes, with O(1) lookups → `HashSet<T>`
- Yes, sorted → `SortedSet<T>`
- Yes, read-only after creation → `FrozenSet<T>` (.NET 8+)

**Do you need FIFO processing?**

- Yes, single-threaded → `Queue<T>`
- Yes, multi-threaded → `ConcurrentQueue<T>` or `Channel<T>`
- Yes, with priorities → `PriorityQueue<TElement, TPriority>`

**Do you need LIFO processing?**

- Yes, single-threaded → `Stack<T>`
- Yes, multi-threaded → `ConcurrentStack<T>`

**Do you need efficient boolean storage?**

- Yes, large collections → `BitArray`
- Yes, 32 or fewer flags → `BitVector32`

**Do you need zero-allocation slicing?**

- Yes, stack-only → `Span<T>` / `ReadOnlySpan<T>`
- Yes, across async boundaries → `Memory<T>` / `ReadOnlyMemory<T>`

## Part 23: Performance Benchmarking — Measuring What Matters

### BenchmarkDotNet

Never guess about performance. Measure. BenchmarkDotNet is the standard tool for micro-benchmarking in .NET:

```csharp
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using System.Collections.Frozen;

BenchmarkRunner.Run<LookupBenchmarks>();

[MemoryDiagnoser]
public class LookupBenchmarks
{
    private Dictionary<string, int> _dictionary = null!;
    private FrozenDictionary<string, int> _frozen = null!;
    private SortedDictionary<string, int> _sorted = null!;
    private string[] _keys = null!;

    [Params(100, 1000, 10000)]
    public int N;

    [GlobalSetup]
    public void Setup()
    {
        var data = Enumerable.Range(0, N)
            .ToDictionary(i => $"key-{i:D6}", i => i);
        _dictionary = data;
        _frozen = data.ToFrozenDictionary();
        _sorted = new SortedDictionary<string, int>(data);
        _keys = data.Keys.ToArray();
    }

    [Benchmark(Baseline = true)]
    public int Dictionary_TryGetValue()
    {
        int sum = 0;
        foreach (string key in _keys)
        {
            if (_dictionary.TryGetValue(key, out int value))
                sum += value;
        }
        return sum;
    }

    [Benchmark]
    public int FrozenDictionary_TryGetValue()
    {
        int sum = 0;
        foreach (string key in _keys)
        {
            if (_frozen.TryGetValue(key, out int value))
                sum += value;
        }
        return sum;
    }

    [Benchmark]
    public int SortedDictionary_TryGetValue()
    {
        int sum = 0;
        foreach (string key in _keys)
        {
            if (_sorted.TryGetValue(key, out int value))
                sum += value;
        }
        return sum;
    }
}
```

Run it with `dotnet run -c Release` and you will get precise, statistically significant timings with memory allocation measurements.

## Part 24: Data Structures in ASP.NET Core — Practical Patterns

### Dependency Injection and Collection Registration

ASP.NET Core's DI container uses dictionaries and lists internally to manage service registrations. Understanding this helps you make better registration decisions:

```csharp
var builder = WebApplication.CreateBuilder(args);

// Singleton — created once, stored in a ConcurrentDictionary-like structure
builder.Services.AddSingleton<IConfigService, ConfigService>();

// Scoped — one per request, stored in a per-scope dictionary
builder.Services.AddScoped<IUserContext, UserContext>();

// Transient — new instance every time, no caching
builder.Services.AddTransient<IValidator, OrderValidator>();

// Keyed services (.NET 8+) — dictionary lookup by key
builder.Services.AddKeyedSingleton<INotifier, EmailNotifier>("email");
builder.Services.AddKeyedSingleton<INotifier, SmsNotifier>("sms");

// Resolve by key
app.MapGet("/notify", ([FromKeyedServices("email")] INotifier notifier) =>
{
    return notifier.Send("Hello!");
});
```

### Caching Patterns

```csharp
// In-memory cache with FrozenDictionary for static data
public class ProductCatalogCache
{
    private FrozenDictionary<string, Product> _products =
        FrozenDictionary<string, Product>.Empty;

    public async Task RefreshAsync(IProductRepository repo)
    {
        var allProducts = await repo.GetAllAsync();
        // Atomic swap — readers never see a partially-built dictionary
        _products = allProducts.ToFrozenDictionary(p => p.Sku);
    }

    public Product? GetBySku(string sku)
    {
        _products.TryGetValue(sku, out Product? product);
        return product;
    }
}

// Register as singleton
builder.Services.AddSingleton<ProductCatalogCache>();
```

### Request Processing with Channels

```csharp
// Background service that processes events from a Channel
public class EventProcessor : BackgroundService
{
    private readonly Channel<DomainEvent> _channel;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<EventProcessor> _logger;

    public EventProcessor(
        Channel<DomainEvent> channel,
        IServiceScopeFactory scopeFactory,
        ILogger<EventProcessor> logger)
    {
        _channel = channel;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        await foreach (DomainEvent evt in _channel.Reader.ReadAllAsync(ct))
        {
            using var scope = _scopeFactory.CreateScope();
            var handler = scope.ServiceProvider
                .GetRequiredService<IEventHandler>();
            try
            {
                await handler.HandleAsync(evt, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process event {EventId}", evt.Id);
            }
        }
    }
}

// Registration
builder.Services.AddSingleton(Channel.CreateBounded<DomainEvent>(
    new BoundedChannelOptions(10_000)
    {
        FullMode = BoundedChannelFullMode.Wait,
        SingleReader = true
    }));
builder.Services.AddHostedService<EventProcessor>();
```

## Part 25: Custom Data Structures — When the Standard Library Is Not Enough

### Ring Buffer (Circular Buffer)

Sometimes you need a fixed-size buffer where old entries are overwritten by new ones. This is common for metrics collection, sliding windows, and recent-history tracking.

```csharp
public sealed class RingBuffer<T>
{
    private readonly T[] _buffer;
    private int _head;
    private int _count;

    public RingBuffer(int capacity)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(capacity, 1);
        _buffer = new T[capacity];
    }

    public int Count => _count;
    public int Capacity => _buffer.Length;

    public void Add(T item)
    {
        _buffer[_head] = item;
        _head = (_head + 1) % _buffer.Length;
        if (_count < _buffer.Length)
            _count++;
    }

    public IEnumerable<T> GetAll()
    {
        int start = _count < _buffer.Length ? 0 : _head;
        for (int i = 0; i < _count; i++)
        {
            yield return _buffer[(start + i) % _buffer.Length];
        }
    }
}

// Usage: keep the last 100 request durations
var recentDurations = new RingBuffer<TimeSpan>(100);
recentDurations.Add(TimeSpan.FromMilliseconds(42));
recentDurations.Add(TimeSpan.FromMilliseconds(38));

double avgMs = recentDurations.GetAll().Average(d => d.TotalMilliseconds);
```

### Trie (Prefix Tree)

A trie is useful for autocomplete, prefix matching, and IP routing tables:

```csharp
public sealed class Trie
{
    private sealed class Node
    {
        public Dictionary<char, Node> Children { get; } = new();
        public bool IsEndOfWord { get; set; }
    }

    private readonly Node _root = new();

    public void Insert(string word)
    {
        Node current = _root;
        foreach (char c in word)
        {
            if (!current.Children.TryGetValue(c, out Node? child))
            {
                child = new Node();
                current.Children[c] = child;
            }
            current = child;
        }
        current.IsEndOfWord = true;
    }

    public bool Search(string word)
    {
        Node? node = FindNode(word);
        return node is { IsEndOfWord: true };
    }

    public bool StartsWith(string prefix)
    {
        return FindNode(prefix) is not null;
    }

    public IEnumerable<string> GetWordsWithPrefix(string prefix)
    {
        Node? node = FindNode(prefix);
        if (node is null) yield break;

        var stack = new Stack<(Node Node, string Word)>();
        stack.Push((node, prefix));

        while (stack.Count > 0)
        {
            var (current, word) = stack.Pop();
            if (current.IsEndOfWord)
                yield return word;

            foreach (var (c, child) in current.Children)
            {
                stack.Push((child, word + c));
            }
        }
    }

    private Node? FindNode(string prefix)
    {
        Node current = _root;
        foreach (char c in prefix)
        {
            if (!current.Children.TryGetValue(c, out Node? child))
                return null;
            current = child;
        }
        return current;
    }
}

// Usage: autocomplete
var trie = new Trie();
trie.Insert("application");
trie.Insert("apple");
trie.Insert("apply");
trie.Insert("banana");

var suggestions = trie.GetWordsWithPrefix("app").ToList();
// ["application", "apple", "apply"]
```

### Graph Representation

Graphs appear in routing, dependency resolution, social networks, and workflow engines. Here is an adjacency list representation:

```csharp
public sealed class Graph<T> where T : notnull
{
    private readonly Dictionary<T, HashSet<T>> _adjacency = new();

    public void AddVertex(T vertex)
    {
        _adjacency.TryAdd(vertex, []);
    }

    public void AddEdge(T from, T to)
    {
        AddVertex(from);
        AddVertex(to);
        _adjacency[from].Add(to);
    }

    public void AddUndirectedEdge(T a, T b)
    {
        AddEdge(a, b);
        AddEdge(b, a);
    }

    public IEnumerable<T> GetNeighbors(T vertex)
    {
        return _adjacency.TryGetValue(vertex, out var neighbors)
            ? neighbors
            : [];
    }

    // Breadth-first search
    public IEnumerable<T> BreadthFirstTraversal(T start)
    {
        var visited = new HashSet<T>();
        var queue = new Queue<T>();
        queue.Enqueue(start);
        visited.Add(start);

        while (queue.Count > 0)
        {
            T current = queue.Dequeue();
            yield return current;

            foreach (T neighbor in GetNeighbors(current))
            {
                if (visited.Add(neighbor))
                {
                    queue.Enqueue(neighbor);
                }
            }
        }
    }

    // Depth-first search
    public IEnumerable<T> DepthFirstTraversal(T start)
    {
        var visited = new HashSet<T>();
        var stack = new Stack<T>();
        stack.Push(start);

        while (stack.Count > 0)
        {
            T current = stack.Pop();
            if (!visited.Add(current)) continue;
            yield return current;

            foreach (T neighbor in GetNeighbors(current))
            {
                if (!visited.Contains(neighbor))
                {
                    stack.Push(neighbor);
                }
            }
        }
    }

    // Topological sort (for directed acyclic graphs)
    public List<T> TopologicalSort()
    {
        var inDegree = new Dictionary<T, int>();
        foreach (var vertex in _adjacency.Keys)
            inDegree[vertex] = 0;

        foreach (var (_, neighbors) in _adjacency)
            foreach (T neighbor in neighbors)
                inDegree[neighbor]++;

        var queue = new Queue<T>(inDegree.Where(kv => kv.Value == 0).Select(kv => kv.Key));
        var result = new List<T>();

        while (queue.Count > 0)
        {
            T current = queue.Dequeue();
            result.Add(current);

            foreach (T neighbor in GetNeighbors(current))
            {
                inDegree[neighbor]--;
                if (inDegree[neighbor] == 0)
                    queue.Enqueue(neighbor);
            }
        }

        if (result.Count != _adjacency.Count)
            throw new InvalidOperationException("Graph contains a cycle");

        return result;
    }
}

// Usage: build dependency graph
var deps = new Graph<string>();
deps.AddEdge("App", "Database");
deps.AddEdge("App", "Cache");
deps.AddEdge("Database", "Config");
deps.AddEdge("Cache", "Config");

var buildOrder = deps.TopologicalSort();
// [Config, Database, Cache, App] or [Config, Cache, Database, App]
```

## Part 26: Big-O Summary — Every .NET Collection at a Glance

Here is the complete time-complexity reference for every major collection in .NET:

| Collection | Add/Insert | Remove | Lookup/Access | Contains | Iteration | Memory Overhead |
|---|---|---|---|---|---|---|
| `T[]` | N/A (fixed) | N/A | O(1) by index | O(n) | O(n) | Minimal |
| `List<T>` | O(1) amortized end, O(n) middle | O(n) | O(1) by index | O(n) | O(n) | Up to 2x |
| `LinkedList<T>` | O(1) at known node | O(1) at known node | O(n) | O(n) | O(n) | ~48+ bytes/element |
| `Dictionary<TKey, TValue>` | O(1) amortized | O(1) amortized | O(1) by key | O(1) | O(n) | Moderate |
| `SortedDictionary<TKey, TValue>` | O(log n) | O(log n) | O(log n) | O(log n) | O(n) sorted | Tree node overhead |
| `SortedList<TKey, TValue>` | O(n) | O(n) | O(log n) | O(log n) | O(n) sorted | Minimal (arrays) |
| `OrderedDictionary<TKey, TValue>` | O(1) amortized end | O(n) | O(1) by key, O(1) by index | O(1) | O(n) ordered | Hash + list overhead |
| `HashSet<T>` | O(1) amortized | O(1) amortized | N/A | O(1) | O(n) | Moderate |
| `SortedSet<T>` | O(log n) | O(log n) | N/A | O(log n) | O(n) sorted | Tree node overhead |
| `Stack<T>` | O(1) push | O(1) pop | O(1) peek | O(n) | O(n) | Up to 2x |
| `Queue<T>` | O(1) enqueue | O(1) dequeue | O(1) peek | O(n) | O(n) | Circular buffer |
| `PriorityQueue<T, P>` | O(log n) | O(log n) dequeue | O(1) peek | O(n) | O(n) | Array-based heap |
| `FrozenDictionary<TKey, TValue>` | N/A (immutable) | N/A | O(1) (faster than Dict) | O(1) | O(n) | Optimized |
| `FrozenSet<T>` | N/A (immutable) | N/A | N/A | O(1) (faster than HashSet) | O(n) | Optimized |
| `ConcurrentDictionary<TKey, TValue>` | O(1) amortized | O(1) amortized | O(1) | O(1) | O(n) | Lock striping overhead |
| `ImmutableList<T>` | O(log n) | O(log n) | O(log n) by index | O(n) | O(n) | AVL tree overhead |
| `ImmutableDictionary<TKey, TValue>` | O(log n) | O(log n) | O(log n) | O(log n) | O(n) | Tree overhead |
| `ImmutableArray<T>` | O(n) (copies) | O(n) | O(1) by index | O(n) | O(n) | Minimal |

## Part 27: Common Mistakes and How to Avoid Them

### Mistake 1: Using List\<T\>.Contains() for Frequent Lookups

```csharp
// Bad — O(n) per check, O(n²) total for n checks
var blacklist = new List<string>(LoadBlockedIps());
foreach (string clientIp in incomingRequests)
{
    if (blacklist.Contains(clientIp)) // O(n) each time!
    {
        Reject(clientIp);
    }
}

// Good — O(1) per check, O(n) total
var blacklist = new HashSet<string>(LoadBlockedIps());
foreach (string clientIp in incomingRequests)
{
    if (blacklist.Contains(clientIp)) // O(1)
    {
        Reject(clientIp);
    }
}

// Best — if the set never changes
var blacklist = LoadBlockedIps().ToFrozenSet();
```

### Mistake 2: Not Pre-Sizing Collections

```csharp
// Bad — causes multiple resizes (4 → 8 → 16 → 32 → 64 → ... → 1024+)
var results = new List<Result>();
foreach (var item in thousandItems)
{
    results.Add(Transform(item));
}

// Good — no resizes needed
var results = new List<Result>(thousandItems.Count);
foreach (var item in thousandItems)
{
    results.Add(Transform(item));
}

// Best — use LINQ which knows the source count
var results = thousandItems.Select(Transform).ToList();
```

### Mistake 3: Modifying a Collection During Enumeration

```csharp
// Throws InvalidOperationException
var users = new Dictionary<int, User> { /* ... */ };
foreach (var (id, user) in users)
{
    if (user.IsExpired)
    {
        users.Remove(id); // Boom! Collection was modified
    }
}

// Correct — collect first, then remove
var expiredIds = users
    .Where(kv => kv.Value.IsExpired)
    .Select(kv => kv.Key)
    .ToList(); // Materialize before modifying

foreach (int id in expiredIds)
{
    users.Remove(id);
}
```

### Mistake 4: Ignoring GetHashCode() for Dictionary Keys

```csharp
// Broken — objects with same data land in different buckets
public class BadKey
{
    public string Name { get; set; } = "";
    // Uses default GetHashCode() which is based on object identity!
}

var dict = new Dictionary<BadKey, int>();
dict[new BadKey { Name = "test" }] = 42;
bool found = dict.ContainsKey(new BadKey { Name = "test" }); // false!

// Fixed — use a record or override Equals + GetHashCode
public record GoodKey(string Name);

var dict2 = new Dictionary<GoodKey, int>();
dict2[new GoodKey("test")] = 42;
bool found2 = dict2.ContainsKey(new GoodKey("test")); // true
```

### Mistake 5: Using ConcurrentDictionary When You Do Not Need Concurrency

`ConcurrentDictionary` is slower than `Dictionary` for single-threaded access because of the locking overhead. If you do not have concurrent access, use a regular `Dictionary`. If you need thread-safety but write once and read many times, use `FrozenDictionary` or protect a regular `Dictionary` with a `ReaderWriterLockSlim`.

### Mistake 6: Selecting the Wrong String Comparison for Dictionary Keys

```csharp
// Silent bug — default comparison is ordinal, case-sensitive
var settings = new Dictionary<string, string>();
settings["ContentType"] = "text/html";
bool found = settings.ContainsKey("contenttype"); // false!

// Fixed — specify the comparer at creation
var settings2 = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
settings2["ContentType"] = "text/html";
bool found2 = settings2.ContainsKey("contenttype"); // true
```

### Mistake 7: Returning a Mutable Collection from a Public API

```csharp
public class UserRepository
{
    private readonly List<User> _users = new();

    // Bad — callers can add/remove/clear your internal list
    public List<User> GetUsers() => _users;

    // Good — read-only view, callers cannot modify
    public IReadOnlyList<User> GetUsers() => _users.AsReadOnly();

    // Also good — defensive copy if you need complete isolation
    public IReadOnlyList<User> GetUsersCopy() => _users.ToList().AsReadOnly();
}
```

## Part 28: The .NET Collections Namespace Map

Here is a map of every collections namespace in .NET 10 and what lives in each:

**System** — `Array`, `ArraySegment<T>`, `Tuple`, `ValueTuple`

**System.Collections** — Legacy non-generic collections: `ArrayList`, `Hashtable`, `Queue`, `Stack`, `SortedList`, `BitArray`. Do not use these in new code except `BitArray`.

**System.Collections.Generic** — The main generic collections: `List<T>`, `Dictionary<TKey, TValue>`, `HashSet<T>`, `SortedDictionary<TKey, TValue>`, `SortedList<TKey, TValue>`, `SortedSet<T>`, `LinkedList<T>`, `Queue<T>`, `Stack<T>`, `PriorityQueue<TElement, TPriority>`, `OrderedDictionary<TKey, TValue>` (.NET 9+).

**System.Collections.ObjectModel** — `Collection<T>`, `ReadOnlyCollection<T>`, `ReadOnlyDictionary<TKey, TValue>`, `ReadOnlySet<T>` (.NET 9+), `ObservableCollection<T>`, `KeyedCollection<TKey, TItem>`.

**System.Collections.Concurrent** — Thread-safe collections: `ConcurrentDictionary<TKey, TValue>`, `ConcurrentQueue<T>`, `ConcurrentStack<T>`, `ConcurrentBag<T>`, `BlockingCollection<T>`.

**System.Collections.Immutable** — Persistent immutable collections: `ImmutableArray<T>`, `ImmutableList<T>`, `ImmutableDictionary<TKey, TValue>`, `ImmutableHashSet<T>`, `ImmutableSortedDictionary<TKey, TValue>`, `ImmutableSortedSet<T>`, `ImmutableStack<T>`, `ImmutableQueue<T>`.

**System.Collections.Frozen** — Read-optimized immutable collections (.NET 8+): `FrozenDictionary<TKey, TValue>`, `FrozenSet<T>`.

**System.Collections.Specialized** — Legacy specialized collections: `NameValueCollection`, `StringCollection`, `StringDictionary`, `BitVector32`, non-generic `OrderedDictionary`.

**System.Buffers** — `ArrayPool<T>`, `MemoryPool<T>`, `SearchValues<T>`.

**System.Threading.Channels** — `Channel<T>`, `ChannelReader<T>`, `ChannelWriter<T>`.

## Part 29: What Is New in .NET 10 for Collections

.NET 10, released on November 11, 2025 as a Long-Term Support release supported until November 2028, builds on the collection improvements introduced in .NET 8 and .NET 9.

Key collection-related improvements in the .NET 10 era:

C# 14 introduced implicit span conversions, making it seamless to pass arrays to methods that accept `Span<T>` or `ReadOnlySpan<T>`. This is a game-changer for writing high-performance, allocation-free APIs because callers no longer need to call `.AsSpan()` explicitly.

The `params ReadOnlySpan<T>` feature (introduced in C# 13, refined in C# 14) eliminates the hidden `params` array allocation. Methods like `string.Concat`, `Path.Combine`, and your own APIs can now accept variable arguments without allocating an array:

```csharp
// Old — allocates a string[] for the params
public void Log(params string[] messages) { }

// New — zero allocation
public void Log(params ReadOnlySpan<string> messages)
{
    foreach (string msg in messages)
    {
        Console.WriteLine(msg);
    }
}

// Caller syntax is identical
Log("Error", "Something went wrong", userId);
// But now it's stack-allocated — no GC pressure
```

The JIT compiler in .NET 10 also improved de-virtualization for array-based enumerations, which means `foreach` over arrays and list-backed collections is faster. The JIT can now inline and optimize array enumeration patterns more aggressively, and small arrays used temporarily can be stack-allocated in some cases.

`System.Linq.AsyncEnumerable` is now included in the core libraries, providing a full set of LINQ operators for `IAsyncEnumerable<T>` without needing the `System.Linq.Async` NuGet package.

## Conclusion

Data structures are not an academic exercise. They are the difference between an API that responds in 2 milliseconds and one that responds in 200 milliseconds. They are the difference between a service that handles 10,000 concurrent users and one that falls over at 500. They are the difference between code that is readable and maintainable, and code that is a maze of workarounds for the wrong abstraction.

The .NET ecosystem provides one of the richest standard-library collection frameworks of any language runtime. From the simple `int` sitting directly on the stack, through the workhorse `List<T>` and `Dictionary<TKey, TValue>`, to the specialized `FrozenDictionary<TKey, TValue>` and `Channel<T>`, there is a tool for every job. The challenge is not finding a tool — it is choosing the right one.

The rules are simple, even if applying them takes practice:

Use the most specific type that fits your requirements. Do not default to `List<T>` for everything. If you need unique elements, use `HashSet<T>`. If you need key-value lookups, use `Dictionary<TKey, TValue>`. If the data is immutable after creation, use a frozen collection. If you need thread-safety, use a concurrent collection or a `Channel<T>`.

Measure before optimizing. The Big-O tables in this article tell you the theoretical performance characteristics. Real-world performance depends on cache effects, allocation patterns, and the specific sizes and access patterns of your data. Use BenchmarkDotNet to measure the actual impact before switching collection types in a hot path.

Understand the memory model. The difference between value types and reference types is not trivia — it determines whether your collection stores data inline or chases pointers across the heap. For performance-critical code, prefer structs and spans. For domain modeling, prefer records and classes.

And above all, prefer clarity. A well-chosen data structure communicates intent. When a future developer reads your code and sees a `HashSet<string>`, they immediately know: this is a collection of unique strings with O(1) lookup. That is worth more than any micro-optimization.

## Resources

- Microsoft. "Collections and Data Structures." [learn.microsoft.com/en-us/dotnet/standard/collections](https://learn.microsoft.com/en-us/dotnet/standard/collections). The official overview of .NET collection types with selection guidance.
- Microsoft. "System.Collections.Generic Namespace." [learn.microsoft.com/en-us/dotnet/api/system.collections.generic](https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic?view=net-10.0). API reference for all generic collections.
- Microsoft. "System.Collections.Frozen Namespace." [learn.microsoft.com/en-us/dotnet/api/system.collections.frozen](https://learn.microsoft.com/en-us/dotnet/api/system.collections.frozen?view=net-10.0). API reference for FrozenDictionary and FrozenSet.
- Microsoft. "System.Collections.Immutable Namespace." [learn.microsoft.com/en-us/dotnet/api/system.collections.immutable](https://learn.microsoft.com/en-us/dotnet/api/system.collections.immutable?view=net-10.0). API reference for all immutable collections.
- Microsoft. "Memory\<T\> and Span\<T\> usage guidelines." [learn.microsoft.com/en-us/dotnet/standard/memory-and-spans/memory-t-usage-guidelines](https://learn.microsoft.com/en-us/dotnet/standard/memory-and-spans/memory-t-usage-guidelines). Official guidance on when to use Span vs Memory.
- Microsoft. "What's new in .NET 10." [learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-10/overview](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-10/overview). Overview of all .NET 10 features including collection and runtime improvements.
- Microsoft. "What's new in C# 14." [learn.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-14](https://learn.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-14). C# 14 features including implicit span conversions and extension members.
- Toub, Stephen. "Performance Improvements in .NET 10." [devblogs.microsoft.com/dotnet](https://devblogs.microsoft.com/dotnet/). Annual deep dive into runtime and library performance improvements.
- dotnet/runtime GitHub repository. [github.com/dotnet/runtime](https://github.com/dotnet/runtime). The open-source codebase for the .NET runtime and base class libraries — read the actual collection implementations.
- BenchmarkDotNet. [benchmarkdotnet.org](https://benchmarkdotnet.org/). The standard micro-benchmarking library for .NET.
- Cormen, Thomas H., Charles E. Leiserson, Ronald L. Rivest, and Clifford Stein. *Introduction to Algorithms* (4th edition, MIT Press, 2022). The definitive computer science reference for data structure theory and analysis.
