---
title: "Kotlin, Compose, and Android Development: A Comprehensive Guide from First Principles for the ASP.NET Developer Who Wants to Build Real Things"
date: 2026-05-05
author: myblazor-team
summary: "An exhaustive, from-the-ground-up guide to Kotlin, Jetpack Compose, Kotlin Multiplatform, and Android development — covering the JVM, the language itself, data structures, algorithms, Compose UI, app signing, Containerfiles for CI, and a complete GitHub Actions pipeline for building signed release APKs on every push. Written for C# and ASP.NET developers who want to understand everything, not just copy-paste boilerplate."
tags:
  - kotlin
  - android
  - jetpack-compose
  - kotlin-multiplatform
  - jvm
  - deep-dive
  - mobile
  - ci-cd
  - github-actions
  - data-structures
  - algorithms
  - best-practices
  - software-engineering
  - beginner
---

You are a C# developer. You write ASP.NET web applications. You use Visual Studio, or maybe Rider if you are feeling adventurous. You know what a `Controller` is, and what a `DbContext` is, and you have probably written more LINQ queries than you care to admit. You have a comfortable life.

And now someone — maybe a manager, maybe your own ambition, maybe a client who wants a mobile app — has asked you to build an Android application.

You open a browser. You type "how to build Android app." The first result tells you to install Android Studio. The second result mentions something called Kotlin. The third result mentions Jetpack Compose. The fourth mentions Gradle. The fifth mentions the JVM. The sixth mentions KMP — Kotlin Multiplatform. The seventh mentions APK signing, keystore files, and something called a "release build." You close your browser.

This article is for you. We are going to start from the very beginning. Not "download Android Studio and follow the wizard" beginning — the actual beginning. The JVM. What it is. Why it exists. What assumptions it makes about your code, your memory, your threads, and your entire worldview as a programmer. Then we will learn Kotlin, the language itself, from syntax through coroutines. Then we will build user interfaces with Jetpack Compose. Then we will learn about Android as a platform — its architecture, its lifecycle, its permissions model. Then we will learn how to sign an application and why it matters. Then we will set up a complete CI/CD pipeline with GitHub Actions that builds a signed release APK on every single push. Then we will learn about Kotlin Multiplatform and how to share code between Android, iOS, desktop, and the web.

And we will do all of this from first principles. We will not skip steps. We will not hand-wave. We will not say "this is left as an exercise for the reader." We will build understanding from the ground up, brick by brick.

This article is going to be very, very long. Get comfortable.

## Part 1: The Java Virtual Machine — Why It Exists and What It Does

### The Problem That Created the JVM

In the early 1990s, Sun Microsystems had a problem. The company was building consumer electronics — set-top boxes, interactive televisions, handheld devices — and each device had a different processor. The C and C++ code that powered these devices had to be recompiled for each architecture. If you wrote your code for a SPARC processor, it would not run on a MIPS processor. If you wrote it for MIPS, it would not run on ARM. Every new piece of hardware required porting, retesting, and re-releasing software.

James Gosling, Mike Sheridan, and Patrick Naughton started a project called "Green" in 1991 to address this. The key insight was simple: instead of compiling source code into machine code for a specific processor, compile it into an intermediate form — bytecode — that would be executed by a virtual machine. You write the code once. You compile it once. The virtual machine — which does need to be ported to each platform — interprets the bytecode on any device.

This is the origin of "Write Once, Run Anywhere," the marketing slogan that Sun Microsystems attached to Java when it was publicly released in 1995.

If you come from C#, this will sound extraordinarily familiar. The Common Language Runtime (CLR) in .NET does exactly the same thing. C# compiles to Common Intermediate Language (CIL, formerly MSIL). CIL runs on the CLR. The CLR is available for Windows, Linux, and macOS. The architecture is nearly identical because the CLR was directly inspired by the JVM. Anders Hejlsberg, who designed C#, studied Java closely. The parallels are not coincidental.

Here is a comparison to orient you:

| Concept | Java/JVM | C#/.NET |
|---|---|---|
| Source language | Java, Kotlin, Scala, Clojure | C#, F#, VB.NET |
| Intermediate form | Bytecode (.class files) | CIL (.dll files) |
| Runtime | JVM (HotSpot, OpenJ9, GraalVM) | CLR (CoreCLR, Mono) |
| Garbage collection | Yes (multiple algorithms) | Yes (generational GC) |
| JIT compilation | Yes (C1, C2 compilers in HotSpot) | Yes (RyuJIT) |
| AOT compilation | Yes (GraalVM Native Image) | Yes (NativeAOT, .NET 10) |
| Package manager | Maven, Gradle | NuGet |
| Build tool | Gradle, Maven | MSBuild, dotnet CLI |

### How the JVM Actually Works

When you compile a Java or Kotlin source file, the compiler produces `.class` files containing bytecode. Bytecode is a set of instructions for an abstract stack-based machine. It is not the native instruction set of any real processor. It is the instruction set of the JVM.

Here is a trivial Java class:

```java
public class Hello {
    public static void main(String[] args) {
        int a = 10;
        int b = 20;
        int c = a + b;
        System.out.println(c);
    }
}
```

If you compile this with `javac Hello.java` and then examine the bytecode with `javap -c Hello.class`, you will see something like:

```
public static void main(java.lang.String[]);
  Code:
     0: bipush        10
     2: istore_1
     3: bipush        20
     5: istore_2
     6: iload_1
     7: iload_2
     8: iadd
     9: istore_3
    10: getstatic     #2  // Field java/lang/System.out:Ljava/io/PrintStream;
    13: iload_3
    14: invokevirtual #3  // Method java/io/PrintStream.println:(I)V
    17: return
```

Look at what happens: `bipush 10` pushes the constant 10 onto the operand stack. `istore_1` pops it off the stack and stores it in local variable slot 1. `bipush 20` pushes 20. `istore_2` stores it in slot 2. `iload_1` loads variable 1 back onto the stack. `iload_2` loads variable 2 onto the stack. `iadd` pops both, adds them, pushes the result. `istore_3` stores the result in slot 3. Then it loads the result again and calls `println`.

This is a stack machine. Every operation pushes to and pops from a stack. There are no registers in the JVM specification (although the JIT compiler will use real CPU registers when it compiles this to native code at runtime).

In .NET, the CIL is also stack-based. The bytecode looks remarkably similar:

```
.method public static void Main(string[] args) cil managed
{
    .entrypoint
    ldc.i4.s   10
    stloc.0
    ldc.i4.s   20
    stloc.1
    ldloc.0
    ldloc.1
    add
    stloc.2
    ldloc.2
    call       void [System.Console]System.Console::WriteLine(int32)
    ret
}
```

The patterns are nearly identical: load constant, store local, load local, add, store, print. If you understand one, you understand both. The JVM is not a foreign concept for a .NET developer. It is a cousin.

### JVM Memory Model

The JVM divides memory into several regions. Understanding these is critical for writing correct concurrent code and for understanding garbage collection behavior.

**The Heap** is where all objects live. When you write `new ArrayList<>()` or `new User("Alice")`, the object is allocated on the heap. The garbage collector manages this memory. The heap is divided into generations (Young Generation, Old Generation) in most GC implementations, similar to .NET's Gen 0, Gen 1, Gen 2 generational garbage collector.

**The Stack** is per-thread. Each thread gets its own stack. Each method invocation creates a new frame on the stack containing local variables, the operand stack, and a reference to the constant pool of the current class. When the method returns, the frame is popped. This is exactly how the .NET thread stack works.

**The Method Area (Metaspace)** stores class metadata — the structure of classes, method bytecode, the constant pool, field information. In older JVM versions this was called "PermGen" (Permanent Generation) and had a fixed size, leading to the infamous `java.lang.OutOfMemoryError: PermGen space` error. Since Java 8, it was replaced with Metaspace, which uses native memory and can grow dynamically. If you have ever hit a `TypeLoadException` or `OutOfMemoryException` in .NET related to assembly loading, the root cause is in the same conceptual area.

**The PC (Program Counter) Register** holds the address of the current bytecode instruction for each thread. Not something you will ever interact with directly.

### Garbage Collection — The Same Problem, Different Algorithms

If you come from .NET, you already understand garbage collection conceptually. Objects are allocated. A graph of reachable objects is maintained. Unreachable objects are collected. Generations are used to optimize collection frequency.

The JVM has multiple garbage collector implementations:

**Serial GC** — Single-threaded, stop-the-world. Used on very small heaps or in testing. Think of it as the simplest possible GC. Not suitable for production server workloads.

**Parallel GC (Throughput Collector)** — Multi-threaded for young generation collection. Optimized for throughput — it wants to maximize the amount of work done between GC pauses. This was the default for Java 8 and earlier.

**G1 GC (Garbage-First)** — The default since Java 9. Divides the heap into regions (typically 2,048 of them). Collects the regions with the most garbage first (hence the name). Designed to keep pause times under a configurable target (default 200ms). This is closest to .NET's Server GC mode in philosophy — it tries to balance throughput and latency.

**ZGC** — A low-latency collector that keeps pause times under 10 milliseconds regardless of heap size. It can handle terabyte-sized heaps. Concurrent, meaning most of its work happens while application threads are running. Available since Java 11 (experimental) and production-ready since Java 15.

**Shenandoah** — Another low-latency collector, developed by Red Hat. Similar goals to ZGC but with a different implementation. Available in some OpenJDK builds.

For Android development, none of these matter directly because Android does not use the standard JVM. Android uses the **Android Runtime (ART)**, which has its own GC implementation optimized for mobile devices with limited memory. But understanding the JVM's GC helps you write code that is GC-friendly on any runtime — avoid excessive allocations in tight loops, reuse objects where appropriate, be mindful of object lifetimes.

### The JVM Type System and Primitives

The JVM has a distinction that does not exist in modern .NET (though it existed historically): **primitive types** versus **reference types**.

JVM primitive types: `boolean`, `byte`, `short`, `int`, `long`, `float`, `double`, `char`. These are value types stored on the stack (when used as local variables) or inlined into objects. They are not objects. They do not inherit from `java.lang.Object`. You cannot call methods on them. You cannot store them in generic collections.

To put an `int` into an `ArrayList<Integer>`, the JVM must "box" it — wrap it in an `Integer` object on the heap. This is identical to boxing in .NET before generics (and still happens in certain scenarios with value types in .NET).

Kotlin hides this complexity. When you write `val x: Int = 42` in Kotlin, the compiler decides at compile time whether to use the JVM primitive `int` or the boxed `Integer` based on context. If the value is nullable (`Int?`) or used in a generic context, it gets boxed. Otherwise, it stays as a primitive. You do not need to think about this most of the time, but you need to know it exists because it affects performance and memory usage.

This is similar to how C#'s `int` is a struct (`System.Int32`) that gets boxed when cast to `object` or used through a non-generic interface. The pattern is the same; the syntax is different.

## Part 2: Kotlin — The Language Itself

### Why Kotlin, Not Java

Java is to Kotlin what C was to C#. Java works. It has worked for decades. It runs billions of devices. But it has accumulated decades of baggage: checked exceptions, verbose syntax, null as a valid value for every reference type, no extension functions, no coroutines, no data classes, and a painfully slow evolution cadence (though this has improved since Java 9's six-month release cycle starting in 2017).

JetBrains created Kotlin in 2010 and publicly released it in 2011. The goal was a modern language that runs on the JVM, is fully interoperable with Java, and fixes Java's paper cuts. In 2017, Google announced Kotlin as a first-class language for Android development. In 2019, Google recommended Kotlin as the preferred language for Android. As of 2026, the latest stable version is **Kotlin 2.3.20** (released March 16, 2026), and the vast majority of new Android development is done in Kotlin.

The current stable release of Kotlin is 2.3.20. Kotlin 2.3.0 was a major release that brought context-sensitive resolution improvements, explicit backing fields, and the unused return value checker to stable status. The K2 compiler, which rewrites the entire frontend of the Kotlin compiler for dramatically faster compilation, has been the default since Kotlin 2.0. Kotlin 2.4.0 is planned for June–July 2026.

### Your First Kotlin Program

Let us start with the absolute simplest program:

```kotlin
fun main() {
    println("Hello, world!")
}
```

That is the entire file. No class wrapping. No `public static void`. No `String[] args` (unless you need them, in which case you write `fun main(args: Array<String>)`). No semicolons.

Compare this with C#:

```csharp
// C# with top-level statements (.NET 6+)
Console.WriteLine("Hello, world!");
```

C# has evolved to be similarly concise with top-level statements, but Kotlin had this from the beginning. The function `main` is the entry point. `fun` declares a function. `println` is a function that prints to standard output with a newline.

### Variables: val and var

Kotlin has two keywords for declaring variables:

```kotlin
val name = "Alice"   // Immutable — cannot be reassigned
var age = 30         // Mutable — can be reassigned

// name = "Bob"      // Compilation error: Val cannot be reassigned
age = 31             // Fine
```

`val` is roughly equivalent to `readonly` in C# or `final` in Java. The reference cannot be reassigned, but if it points to a mutable object, the object's contents can still change:

```kotlin
val list = mutableListOf(1, 2, 3)
list.add(4)  // Fine — the list is mutable; we're not reassigning 'list'
// list = mutableListOf(5, 6, 7)  // Compilation error
```

**Best practice: Use `val` by default. Only use `var` when you genuinely need to reassign the variable.** This is not a stylistic suggestion — it is a correctness strategy. Immutable bindings eliminate an entire class of bugs where a variable is accidentally reassigned in a branch you did not consider.

In C#, you would achieve this with `readonly` for fields, or simply by not using a variable after initial assignment. But C# does not enforce this at the local variable level the way Kotlin does. C# requires explicit `readonly` for struct fields and uses `const` for compile-time constants, but local `readonly` variables are not a language feature (as of C# 14). Kotlin's approach is more concise and more pervasive.

### Type Inference

Kotlin has strong type inference. You usually do not need to declare types explicitly:

```kotlin
val name = "Alice"          // Inferred as String
val age = 30                // Inferred as Int
val pi = 3.14159            // Inferred as Double
val items = listOf(1, 2, 3) // Inferred as List<Int>
```

But you can always declare types explicitly:

```kotlin
val name: String = "Alice"
val age: Int = 30
```

This is exactly like C#'s `var`:

```csharp
var name = "Alice";  // Inferred as string
var age = 30;        // Inferred as int
```

The difference is that in Kotlin, `val` with type inference is the dominant style. In C#, there is an ongoing debate about when to use `var` versus explicit types. In Kotlin, the debate is settled: use inference unless the type is not obvious from the right-hand side.

### Null Safety

This is the single most important difference between Kotlin and Java, and it is one of the things that makes Kotlin a genuinely better language for writing correct code.

In Java, any reference type can be null. Always. There is nothing in the type system that tells you "this String will never be null." You learn to write defensive `if (x != null)` checks everywhere, and you inevitably miss one, and you get a `NullPointerException` at runtime.

In C#, nullable reference types (NRTs) were added in C# 8.0 (2019) as an opt-in feature. When enabled, the compiler warns you about potential null dereferences. But it is still just warnings — the runtime does not enforce anything, and legacy code does not use NRTs.

In Kotlin, null safety is built into the type system from day one, and it is enforced at compile time:

```kotlin
var name: String = "Alice"
// name = null  // Compilation error: Null can not be a value of a non-null type String

var nullableName: String? = "Alice"
nullableName = null  // Fine — the type is String?, which allows null
```

The `?` suffix means "this value might be null." Without it, the value is guaranteed to be non-null by the compiler. You cannot assign null to a non-null type. Period.

To work with nullable types, you use safe calls and the Elvis operator:

```kotlin
val length = nullableName?.length  // Returns null if nullableName is null
                                    // Returns the length otherwise
                                    // Type of 'length' is Int?

val lengthOrDefault = nullableName?.length ?: 0  // Returns length, or 0 if null
```

The `?.` is the safe call operator. The `?:` is the Elvis operator (tilt your head left — it looks like Elvis's pompadour). Together, they replace entire chains of null checks:

```kotlin
// Instead of this nightmare:
val city: String? = if (user != null) {
    if (user.address != null) {
        user.address.city
    } else null
} else null

// Write this:
val city = user?.address?.city

// Or with a default:
val city = user?.address?.city ?: "Unknown"
```

In C#, you have the null-conditional operator `?.` and the null-coalescing operator `??`, which are syntactically similar:

```csharp
var city = user?.Address?.City ?? "Unknown";
```

The syntax is almost identical. The difference is that Kotlin enforces nullability at the type level, so you cannot accidentally forget to handle the null case. The compiler will not let you call `.length` on a `String?` without using `?.` or checking for null first.

There is also the not-null assertion operator `!!`:

```kotlin
val length = nullableName!!.length  // Throws NullPointerException if null
```

**Never use `!!` unless you have an ironclad reason.** It defeats the entire purpose of null safety. Every `!!` in your codebase is a ticking time bomb. If you find yourself writing `!!`, ask yourself why the type is nullable in the first place and whether you can restructure the code to eliminate the nullability.

This is equivalent to the `!` (null-forgiving) operator in C# — it tells the compiler "trust me, this is not null." And just like in C#, it is almost always a sign that something is wrong with your design.

### Functions

Functions in Kotlin are declared with the `fun` keyword:

```kotlin
fun add(a: Int, b: Int): Int {
    return a + b
}
```

If the function body is a single expression, you can use the expression body syntax:

```kotlin
fun add(a: Int, b: Int): Int = a + b
```

With type inference on the return type (when the type is obvious):

```kotlin
fun add(a: Int, b: Int) = a + b
```

Kotlin supports default parameter values:

```kotlin
fun greet(name: String, greeting: String = "Hello") {
    println("$greeting, $name!")
}

greet("Alice")              // "Hello, Alice!"
greet("Bob", "Good morning") // "Good morning, Bob!"
```

And named arguments:

```kotlin
greet(greeting = "Hey", name = "Charlie")  // "Hey, Charlie!"
```

Named arguments are transformative for readability. Compare:

```kotlin
// What does 'true, false, 10' mean?
createUser("Alice", true, false, 10)

// Ah, now I understand:
createUser(
    name = "Alice",
    isAdmin = true,
    isVerified = false,
    loginCount = 10
)
```

C# has both default parameters and named arguments, so this should feel familiar. The syntax is nearly identical.

### Extension Functions

This is one of Kotlin's most powerful features, and it will be immediately familiar to C# developers because C# has had extension methods since C# 3.0 (2007).

In Kotlin:

```kotlin
fun String.addExclamation(): String = "$this!"

println("Hello".addExclamation())  // "Hello!"
```

In C#:

```csharp
public static string AddExclamation(this string s) => $"{s}!";

Console.WriteLine("Hello".AddExclamation());  // "Hello!"
```

The syntax differs — Kotlin uses `fun ReceiverType.functionName()` while C# uses the `this` keyword in the parameter list — but the concept is identical. Extension functions do not modify the original class. They are syntactic sugar for static functions that take the receiver as the first argument.

### Data Classes

In C#, you have records (introduced in C# 9.0 / .NET 5):

```csharp
public record User(string Name, int Age);
```

In Kotlin, you have data classes:

```kotlin
data class User(val name: String, val age: Int)
```

A data class automatically generates:

- `equals()` and `hashCode()` based on all properties in the primary constructor
- `toString()` that includes all properties — e.g., `User(name=Alice, age=30)`
- `copy()` function for creating modified copies
- `componentN()` functions for destructuring

```kotlin
val alice = User("Alice", 30)
val olderAlice = alice.copy(age = 31)
println(olderAlice)  // User(name=Alice, age=31)

val (name, age) = alice  // Destructuring
println("$name is $age")  // "Alice is 30"
```

The `copy()` function is similar to C#'s `with` expression for records:

```csharp
var alice = new User("Alice", 30);
var olderAlice = alice with { Age = 31 };
```

### When Expressions (Pattern Matching)

Kotlin's `when` is the equivalent of C#'s `switch` expression, but more powerful:

```kotlin
fun describe(x: Any): String = when (x) {
    1 -> "One"
    "Hello" -> "Greeting"
    is Long -> "Long: $x"
    !is String -> "Not a string"
    else -> "Unknown"
}
```

`when` can also be used without an argument:

```kotlin
fun classify(temperature: Int): String = when {
    temperature < 0 -> "Freezing"
    temperature < 10 -> "Cold"
    temperature < 20 -> "Cool"
    temperature < 30 -> "Warm"
    else -> "Hot"
}
```

This is equivalent to C#'s switch expression with patterns:

```csharp
string Classify(int temperature) => temperature switch
{
    < 0 => "Freezing",
    < 10 => "Cold",
    < 20 => "Cool",
    < 30 => "Warm",
    _ => "Hot"
};
```

### Sealed Classes and Interfaces

Sealed classes restrict which classes can inherit from them. They must be defined in the same file (or, since Kotlin 1.5, the same package):

```kotlin
sealed class Result {
    data class Success(val data: String) : Result()
    data class Error(val message: String) : Result()
    data object Loading : Result()
}

fun handleResult(result: Result): String = when (result) {
    is Result.Success -> "Got data: ${result.data}"
    is Result.Error -> "Error: ${result.message}"
    Result.Loading -> "Loading..."
    // No 'else' needed — the compiler knows all cases are covered
}
```

This is equivalent to C#'s approach, though C# does not have a direct `sealed class` with the same algebraic data type semantics. You would use abstract records with positional constructors:

```csharp
public abstract record Result;
public record Success(string Data) : Result;
public record Error(string Message) : Result;
public record Loading : Result;
```

The key advantage of sealed classes is exhaustive `when` expressions. The compiler knows every possible subtype and will error if you miss one. This eliminates an entire class of bugs where you forget to handle a case.

### Collections

Kotlin distinguishes between read-only and mutable collections at the type level:

```kotlin
val readOnlyList: List<Int> = listOf(1, 2, 3)
// readOnlyList.add(4)  // Compilation error: no 'add' method on List

val mutableList: MutableList<Int> = mutableListOf(1, 2, 3)
mutableList.add(4)  // Fine
```

This is a significant improvement over Java, where `java.util.List` has `add()` and `remove()` methods even if you intend the list to be immutable (you have to use `Collections.unmodifiableList()` to get an immutable wrapper, and it only throws at runtime if you try to modify it).

C# has `IReadOnlyList<T>` and `List<T>`, which is somewhat similar but less pervasive — most C# code just uses `List<T>` for everything.

Kotlin's standard library provides rich collection operations through extension functions:

```kotlin
val numbers = listOf(1, 2, 3, 4, 5, 6, 7, 8, 9, 10)

val evenSquares = numbers
    .filter { it % 2 == 0 }
    .map { it * it }
    .sortedDescending()
// [100, 64, 36, 16, 4]

val sum = numbers.fold(0) { acc, n -> acc + n }
// 55

val grouped = listOf("apple", "banana", "avocado", "blueberry")
    .groupBy { it.first() }
// {a=[apple, avocado], b=[banana, blueberry]}
```

If you know LINQ in C#, this is the same thing with different method names:

| Kotlin | C# LINQ |
|---|---|
| `filter { }` | `.Where(x => )` |
| `map { }` | `.Select(x => )` |
| `flatMap { }` | `.SelectMany(x => )` |
| `sortedBy { }` | `.OrderBy(x => )` |
| `groupBy { }` | `.GroupBy(x => )` |
| `fold(initial) { }` | `.Aggregate(initial, (acc, x) => )` |
| `any { }` | `.Any(x => )` |
| `all { }` | `.All(x => )` |
| `first { }` | `.First(x => )` |
| `firstOrNull { }` | `.FirstOrDefault(x => )` |

The lambda syntax is different — Kotlin uses `{ parameter -> body }` while C# uses `(parameter) => body` — but the operations are equivalent.

### Coroutines — Kotlin's async/await

This is where Kotlin gets interesting for the C# developer, because you already understand async programming from C#'s `async`/`await`.

In C#:

```csharp
public async Task<string> FetchDataAsync(string url)
{
    using var client = new HttpClient();
    var response = await client.GetStringAsync(url);
    return response;
}
```

In Kotlin:

```kotlin
suspend fun fetchData(url: String): String {
    return httpClient.get(url).bodyAsText()
}
```

The `suspend` keyword in Kotlin is equivalent to `async` in C#. A `suspend` function can be paused and resumed without blocking a thread. The compiler transforms `suspend` functions into state machines, exactly as the C# compiler transforms `async` methods into state machines.

But there are important differences:

1. **No `Task<T>` wrapper by default.** In C#, an `async` method returns `Task<T>` (or `ValueTask<T>`). In Kotlin, a `suspend` function returns the value directly — the suspension is transparent. The equivalent of `Task<T>` is `Deferred<T>`, which you get from `async { }`.

2. **Structured concurrency.** Kotlin coroutines are always launched within a `CoroutineScope`. When the scope is cancelled, all coroutines within it are cancelled. This is a fundamental design choice that prevents the "fire and forget" pattern that causes so many problems in .NET.

```kotlin
import kotlinx.coroutines.*

fun main() = runBlocking {
    val result = async { fetchData("https://example.com") }
    println(result.await())
}
```

`runBlocking` creates a coroutine scope that blocks the current thread until all coroutines within it complete. `async` launches a coroutine that returns a `Deferred<T>`. `await()` suspends until the result is available.

In Android development, you will use `viewModelScope` (provided by Android's lifecycle libraries) instead of `runBlocking`:

```kotlin
class MyViewModel : ViewModel() {
    fun loadData() {
        viewModelScope.launch {
            val data = fetchData("https://api.example.com/items")
            _uiState.value = UiState.Success(data)
        }
    }
}
```

When the ViewModel is cleared (because the user navigated away), `viewModelScope` is cancelled, which cancels the coroutine, which cancels the network request. No leaked resources. No callbacks firing after the screen is gone. This is structured concurrency in practice.

## Part 3: Data Structures and Algorithms in Kotlin

You asked to learn, not to assemble Legos. This section covers the fundamental data structures and algorithms using only Kotlin's standard library — no external dependencies. We are going to implement things ourselves so you understand what is happening underneath.

### Arrays

The most fundamental data structure. A contiguous block of memory holding elements of the same type, accessed by integer index in O(1) time.

```kotlin
// Kotlin arrays
val numbers = intArrayOf(1, 2, 3, 4, 5)
val names = arrayOf("Alice", "Bob", "Charlie")

// Access by index: O(1)
println(numbers[0])  // 1
println(names[2])    // Charlie

// Iteration
for (n in numbers) {
    print("$n ")  // 1 2 3 4 5
}

// Size
println(numbers.size)  // 5
```

`IntArray` is a wrapper around `int[]` on the JVM — no boxing. `Array<String>` is `String[]` on the JVM. Use `IntArray`, `LongArray`, `DoubleArray`, and so on for primitive types to avoid boxing overhead.

### Linked List — Implementing Our Own

The Kotlin standard library does not have a dedicated `LinkedList` class (though Java's `java.util.LinkedList` is available). Let us build one from scratch to understand the data structure:

```kotlin
class SinglyLinkedList<T> {
    private class Node<T>(val value: T, var next: Node<T>? = null)

    private var head: Node<T>? = null
    private var _size: Int = 0

    val size: Int get() = _size

    fun addFirst(value: T) {
        head = Node(value, head)
        _size++
    }

    fun addLast(value: T) {
        val newNode = Node(value)
        if (head == null) {
            head = newNode
        } else {
            var current = head
            while (current?.next != null) {
                current = current.next
            }
            current?.next = newNode
        }
        _size++
    }

    fun removeFirst(): T? {
        val value = head?.value
        head = head?.next
        if (value != null) _size--
        return value
    }

    fun contains(value: T): Boolean {
        var current = head
        while (current != null) {
            if (current.value == value) return true
            current = current.next
        }
        return false
    }

    fun toList(): List<T> {
        val result = mutableListOf<T>()
        var current = head
        while (current != null) {
            result.add(current.value)
            current = current.next
        }
        return result
    }

    override fun toString(): String = toList().joinToString(" -> ")
}

fun main() {
    val list = SinglyLinkedList<Int>()
    list.addFirst(3)
    list.addFirst(2)
    list.addFirst(1)
    list.addLast(4)
    list.addLast(5)
    println(list)  // 1 -> 2 -> 3 -> 4 -> 5
    println(list.contains(3))  // true
    println(list.removeFirst())  // 1
    println(list)  // 2 -> 3 -> 4 -> 5
}
```

Time complexities:
- `addFirst`: O(1)
- `addLast`: O(n) — we have to traverse to the end
- `removeFirst`: O(1)
- `contains`: O(n) — linear search
- Random access: O(n) — no index access

Compare with an `ArrayList` (or `MutableList<T>` in Kotlin, backed by `ArrayList` on the JVM):
- Random access: O(1)
- `add` at end: Amortized O(1) — occasionally O(n) when the backing array is resized
- `add` at beginning: O(n) — all elements must be shifted
- `contains`: O(n) — linear search

The choice between linked list and array list depends on your access patterns. If you need frequent insertions and deletions at the beginning, a linked list wins. If you need random access by index, an array list wins. In practice, array-backed lists are almost always faster due to CPU cache locality — sequential memory access is dramatically faster than pointer chasing through scattered heap nodes. This is why `ArrayList` is the default collection in virtually every modern language.

### Stack — Last In, First Out

A stack is trivially implemented using a list:

```kotlin
class Stack<T> {
    private val elements = mutableListOf<T>()

    val size: Int get() = elements.size
    val isEmpty: Boolean get() = elements.isEmpty()

    fun push(value: T) {
        elements.add(value)
    }

    fun pop(): T {
        if (isEmpty) throw NoSuchElementException("Stack is empty")
        return elements.removeAt(elements.lastIndex)
    }

    fun peek(): T {
        if (isEmpty) throw NoSuchElementException("Stack is empty")
        return elements.last()
    }

    override fun toString(): String = elements.reversed().joinToString("\n")
}
```

All operations are O(1) amortized. A stack is used for expression parsing, undo systems, DFS (depth-first search), and the call stack of every programming language.

### Queue — First In, First Out

```kotlin
class Queue<T> {
    private val elements = ArrayDeque<T>()

    val size: Int get() = elements.size
    val isEmpty: Boolean get() = elements.isEmpty()

    fun enqueue(value: T) {
        elements.addLast(value)
    }

    fun dequeue(): T {
        if (isEmpty) throw NoSuchElementException("Queue is empty")
        return elements.removeFirst()
    }

    fun peek(): T {
        if (isEmpty) throw NoSuchElementException("Queue is empty")
        return elements.first()
    }
}
```

Kotlin's `ArrayDeque` is a double-ended queue backed by a circular buffer — both `addFirst`/`removeFirst` and `addLast`/`removeLast` are O(1) amortized. This is the correct backing structure for a queue. Do not use an `ArrayList` for a queue — `removeAt(0)` is O(n) because all remaining elements must be shifted.

### HashMap — Understanding Hash Tables

Kotlin's `HashMap` is the JVM's `java.util.HashMap`. It uses an array of "buckets." To find which bucket a key belongs to, the hash code of the key is computed and mapped to an index using modular arithmetic.

Here is a simplified hash map implementation to illustrate the concept:

```kotlin
class SimpleHashMap<K, V>(private val capacity: Int = 16) {
    private data class Entry<K, V>(val key: K, var value: V)

    private val buckets: Array<MutableList<Entry<K, V>>> =
        Array(capacity) { mutableListOf() }
    private var _size: Int = 0

    val size: Int get() = _size

    private fun bucketIndex(key: K): Int {
        return (key.hashCode() and 0x7FFFFFFF) % capacity
    }

    fun put(key: K, value: V) {
        val index = bucketIndex(key)
        val bucket = buckets[index]
        for (entry in bucket) {
            if (entry.key == key) {
                entry.value = value  // Update existing
                return
            }
        }
        bucket.add(Entry(key, value))
        _size++
    }

    fun get(key: K): V? {
        val index = bucketIndex(key)
        val bucket = buckets[index]
        for (entry in bucket) {
            if (entry.key == key) return entry.value
        }
        return null
    }

    fun containsKey(key: K): Boolean {
        val index = bucketIndex(key)
        return buckets[index].any { it.key == key }
    }

    fun remove(key: K): V? {
        val index = bucketIndex(key)
        val bucket = buckets[index]
        val iterator = bucket.iterator()
        while (iterator.hasNext()) {
            val entry = iterator.next()
            if (entry.key == key) {
                iterator.remove()
                _size--
                return entry.value
            }
        }
        return null
    }
}
```

Average-case time complexity: O(1) for `get`, `put`, `containsKey`, and `remove`. Worst case: O(n) if all keys hash to the same bucket (hash collision). The real `HashMap` in Java mitigates this by converting long chains to balanced trees (red-black trees) when a bucket exceeds 8 entries (since Java 8).

**The critical requirement**: for a hash map to work correctly, `equals()` and `hashCode()` must be consistent. If two objects are equal according to `equals()`, they must have the same `hashCode()`. Kotlin's data classes generate correct `equals()` and `hashCode()` automatically. If you write your own class and use it as a hash map key, you must override both.

This is identical to the requirement in .NET: if you override `Equals()`, you must also override `GetHashCode()`.

### Binary Search

Binary search finds an element in a sorted array in O(log n) time:

```kotlin
fun <T : Comparable<T>> binarySearch(sorted: List<T>, target: T): Int {
    var low = 0
    var high = sorted.size - 1

    while (low <= high) {
        val mid = low + (high - low) / 2  // Avoid overflow
        val comparison = sorted[mid].compareTo(target)
        when {
            comparison == 0 -> return mid
            comparison < 0 -> low = mid + 1
            else -> high = mid - 1
        }
    }
    return -1  // Not found
}

fun main() {
    val numbers = listOf(1, 3, 5, 7, 9, 11, 13, 15)
    println(binarySearch(numbers, 7))   // 3 (index)
    println(binarySearch(numbers, 6))   // -1 (not found)
}
```

Note the use of `low + (high - low) / 2` instead of `(low + high) / 2`. The latter can overflow if `low + high` exceeds `Int.MAX_VALUE`. This is a classic bug that existed in the JDK's own `Arrays.binarySearch()` for nine years before being found and fixed.

### Sorting — Merge Sort Implementation

Kotlin's `sorted()` uses TimSort (a hybrid merge sort / insertion sort), which is excellent for real-world data. But let us implement merge sort ourselves:

```kotlin
fun <T : Comparable<T>> mergeSort(list: List<T>): List<T> {
    if (list.size <= 1) return list

    val mid = list.size / 2
    val left = mergeSort(list.subList(0, mid))
    val right = mergeSort(list.subList(mid, list.size))

    return merge(left, right)
}

private fun <T : Comparable<T>> merge(left: List<T>, right: List<T>): List<T> {
    val result = mutableListOf<T>()
    var i = 0
    var j = 0

    while (i < left.size && j < right.size) {
        if (left[i] <= right[j]) {
            result.add(left[i])
            i++
        } else {
            result.add(right[j])
            j++
        }
    }

    while (i < left.size) {
        result.add(left[i])
        i++
    }

    while (j < right.size) {
        result.add(right[j])
        j++
    }

    return result
}

fun main() {
    val unsorted = listOf(38, 27, 43, 3, 9, 82, 10)
    println(mergeSort(unsorted))  // [3, 9, 10, 27, 38, 43, 82]
}
```

Time complexity: O(n log n) in all cases. Space complexity: O(n) for the temporary lists.

Merge sort is a stable sort — equal elements maintain their relative order. It is the foundation of TimSort, which is used by both Java/Kotlin's `sort()` and Python's `sorted()`.

### Graph Representation and BFS/DFS

Graphs are represented as adjacency lists — a map from each node to its neighbors:

```kotlin
class Graph<T> {
    private val adjacencyList = mutableMapOf<T, MutableList<T>>()

    fun addEdge(from: T, to: T) {
        adjacencyList.getOrPut(from) { mutableListOf() }.add(to)
        adjacencyList.getOrPut(to) { mutableListOf() }.add(from)  // Undirected
    }

    fun bfs(start: T): List<T> {
        val visited = mutableSetOf<T>()
        val queue = ArrayDeque<T>()
        val result = mutableListOf<T>()

        visited.add(start)
        queue.addLast(start)

        while (queue.isNotEmpty()) {
            val current = queue.removeFirst()
            result.add(current)

            for (neighbor in adjacencyList[current] ?: emptyList()) {
                if (neighbor !in visited) {
                    visited.add(neighbor)
                    queue.addLast(neighbor)
                }
            }
        }

        return result
    }

    fun dfs(start: T): List<T> {
        val visited = mutableSetOf<T>()
        val result = mutableListOf<T>()

        fun dfsRecursive(node: T) {
            visited.add(node)
            result.add(node)
            for (neighbor in adjacencyList[node] ?: emptyList()) {
                if (neighbor !in visited) {
                    dfsRecursive(neighbor)
                }
            }
        }

        dfsRecursive(start)
        return result
    }
}

fun main() {
    val graph = Graph<String>()
    graph.addEdge("A", "B")
    graph.addEdge("A", "C")
    graph.addEdge("B", "D")
    graph.addEdge("C", "D")
    graph.addEdge("D", "E")

    println("BFS: ${graph.bfs("A")}")  // BFS: [A, B, C, D, E]
    println("DFS: ${graph.dfs("A")}")  // DFS: [A, B, D, C, E] (order may vary)
}
```

BFS uses a queue and explores neighbors level by level. DFS uses a stack (implicit in recursion) and explores as deep as possible before backtracking. Both are O(V + E) where V is vertices and E is edges.

## Part 4: Jetpack Compose — Modern Android UI

### What Compose Is (and What It Replaces)

For years, Android UI was built with XML layout files and Java/Kotlin code that manipulated `View` objects imperatively. You would write an XML file defining buttons, text fields, and lists, then find those views by ID in your Activity or Fragment and set listeners, update text, show/hide elements, and so on.

This is similar to how ASP.NET Web Forms worked — declare UI in markup, manipulate it imperatively in code-behind. And just as ASP.NET evolved from Web Forms to MVC to Razor Pages to Blazor, Android evolved from XML Views to Jetpack Compose.

Jetpack Compose is a declarative UI framework. Instead of imperatively manipulating UI elements, you describe what the UI should look like given the current state, and the framework handles the rendering. When the state changes, the UI is recomposed — the relevant composable functions are called again with the new state, and only the parts of the UI that changed are updated.

The latest stable Jetpack Compose release (December 2025) includes Compose 1.10 for core modules and Material 3 version 1.4. The Compose BOM (Bill of Materials) version is `2025.12.00`.

If you have used Blazor, you will find Compose familiar. In Blazor:

```razor
@code {
    private int count = 0;
}

<p>Current count: @count</p>
<button @onclick="() => count++">Click me</button>
```

In Compose:

```kotlin
@Composable
fun Counter() {
    var count by remember { mutableStateOf(0) }

    Column {
        Text("Current count: $count")
        Button(onClick = { count++ }) {
            Text("Click me")
        }
    }
}
```

Both are declarative. Both re-render when state changes. Both compose UI from nested function calls (Blazor: Razor components; Compose: Composable functions).

### Your First Composable

A composable function is any function annotated with `@Composable`:

```kotlin
@Composable
fun Greeting(name: String) {
    Text(text = "Hello, $name!")
}
```

You call it from another composable:

```kotlin
@Composable
fun MyApp() {
    Greeting("World")
}
```

Composables are not classes. They are functions. They do not return anything (the return type is `Unit`). Instead, they emit UI elements by calling other composable functions. `Text()`, `Button()`, `Column()`, `Row()`, `Box()` — these are all composable functions provided by the Compose library.

### State Management

State is the core of Compose. When state changes, the composables that read that state are recomposed.

```kotlin
@Composable
fun TodoList() {
    var text by remember { mutableStateOf("") }
    val items = remember { mutableStateListOf<String>() }

    Column(modifier = Modifier.padding(16.dp)) {
        Row(verticalAlignment = Alignment.CenterVertically) {
            TextField(
                value = text,
                onValueChange = { text = it },
                modifier = Modifier.weight(1f),
                placeholder = { Text("Add a task...") }
            )
            Spacer(modifier = Modifier.width(8.dp))
            Button(
                onClick = {
                    if (text.isNotBlank()) {
                        items.add(text)
                        text = ""
                    }
                }
            ) {
                Text("Add")
            }
        }

        Spacer(modifier = Modifier.height(16.dp))

        LazyColumn {
            items(items.size) { index ->
                Text(
                    text = items[index],
                    modifier = Modifier
                        .fillMaxWidth()
                        .padding(vertical = 8.dp)
                )
            }
        }
    }
}
```

`remember` tells Compose to remember this value across recompositions. Without `remember`, the state would be reset every time the composable is called. `mutableStateOf` creates an observable value — when it changes, any composable that reads it is scheduled for recomposition.

`LazyColumn` is the Compose equivalent of `RecyclerView` in the old View system — it only renders the items that are visible on screen. This is critical for lists with hundreds or thousands of items.

### Modifiers

Modifiers are how you style and position composables. They are applied using a builder pattern:

```kotlin
Text(
    text = "Hello",
    modifier = Modifier
        .fillMaxWidth()
        .padding(16.dp)
        .background(Color.LightGray)
        .clickable { /* handle click */ }
)
```

Modifier order matters. `Modifier.padding(16.dp).background(Color.Red)` applies padding first, then background (so the background includes the padding). `Modifier.background(Color.Red).padding(16.dp)` applies background first, then padding (so there is a 16dp gap between the background and the content).

This is similar to CSS box model concepts, but the order is explicit rather than implicit.

### Navigation

For multi-screen apps, you use the Navigation Compose library:

```kotlin
@Composable
fun AppNavigation() {
    val navController = rememberNavController()

    NavHost(navController = navController, startDestination = "home") {
        composable("home") {
            HomeScreen(
                onNavigateToDetail = { id ->
                    navController.navigate("detail/$id")
                }
            )
        }
        composable(
            route = "detail/{itemId}",
            arguments = listOf(navArgument("itemId") { type = NavType.StringType })
        ) { backStackEntry ->
            val itemId = backStackEntry.arguments?.getString("itemId") ?: ""
            DetailScreen(itemId = itemId)
        }
    }
}
```

This is analogous to routing in ASP.NET or Blazor — you define routes with parameters, and navigation triggers a transition to the appropriate screen.

### ViewModel and State Hoisting

In a well-structured Compose app, state lives in a `ViewModel`, not in the composable:

```kotlin
class ItemListViewModel : ViewModel() {
    private val _uiState = MutableStateFlow(ItemListUiState())
    val uiState: StateFlow<ItemListUiState> = _uiState.asStateFlow()

    fun loadItems() {
        viewModelScope.launch {
            _uiState.value = _uiState.value.copy(isLoading = true)
            try {
                val items = repository.getItems()
                _uiState.value = ItemListUiState(items = items)
            } catch (e: Exception) {
                _uiState.value = ItemListUiState(error = e.message)
            }
        }
    }

    fun deleteItem(id: String) {
        viewModelScope.launch {
            repository.deleteItem(id)
            loadItems()
        }
    }
}

data class ItemListUiState(
    val items: List<Item> = emptyList(),
    val isLoading: Boolean = false,
    val error: String? = null
)
```

The composable observes the state:

```kotlin
@Composable
fun ItemListScreen(viewModel: ItemListViewModel = viewModel()) {
    val uiState by viewModel.uiState.collectAsState()

    LaunchedEffect(Unit) {
        viewModel.loadItems()
    }

    when {
        uiState.isLoading -> CircularProgressIndicator()
        uiState.error != null -> Text("Error: ${uiState.error}")
        else -> {
            LazyColumn {
                items(uiState.items) { item ->
                    ItemCard(
                        item = item,
                        onDelete = { viewModel.deleteItem(item.id) }
                    )
                }
            }
        }
    }
}
```

This pattern — ViewModel holds state, composable observes and renders — is the Compose equivalent of the MVVM (Model-View-ViewModel) pattern. If you have used Blazor with services injected into components, the concept is identical: state management lives outside the UI layer.

## Part 5: Android Application Architecture

### The Activity Lifecycle

An Android `Activity` is roughly equivalent to a page or window. It has a lifecycle — it is created, started, resumed, paused, stopped, and destroyed as the user navigates and the system manages resources.

```kotlin
class MainActivity : ComponentActivity() {
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        setContent {
            MyAppTheme {
                AppNavigation()
            }
        }
    }
}
```

With Compose, the `Activity` is minimal. It sets the content to your root composable, and Compose handles everything else. In the old View-based world, Activities were heavyweight — they managed XML inflation, view binding, fragment transactions, saved instance state, and more. With Compose, the Activity is just a host.

### The AndroidManifest.xml

Every Android app has a manifest file that declares the app's components, permissions, and metadata:

```xml
<?xml version="1.0" encoding="utf-8"?>
<manifest xmlns:android="http://schemas.android.com/apk/res/android">

    <uses-permission android:name="android.permission.INTERNET" />

    <application
        android:allowBackup="true"
        android:icon="@mipmap/ic_launcher"
        android:label="@string/app_name"
        android:theme="@style/Theme.MyApp">

        <activity
            android:name=".MainActivity"
            android:exported="true">
            <intent-filter>
                <action android:name="android.intent.action.MAIN" />
                <category android:name="android.intent.category.LAUNCHER" />
            </intent-filter>
        </activity>

    </application>
</manifest>
```

This is analogous to `Program.cs` in ASP.NET — it is the entry point configuration that tells the platform what your app is, what permissions it needs, and which component to launch first.

### The build.gradle.kts File

Android projects use Gradle as the build tool. The build file is written in Kotlin (`.kts` extension) or Groovy. As of 2026, Kotlin DSL is the recommended default.

```kotlin
// app/build.gradle.kts
plugins {
    alias(libs.plugins.android.application)
    alias(libs.plugins.kotlin.android)
    alias(libs.plugins.compose.compiler)
}

android {
    namespace = "com.example.myapp"
    compileSdk = 35

    defaultConfig {
        applicationId = "com.example.myapp"
        minSdk = 24
        targetSdk = 35
        versionCode = 1
        versionName = "1.0.0"
    }

    buildFeatures {
        compose = true
    }

    buildTypes {
        release {
            isMinifyEnabled = true
            isShrinkResources = true
            proguardFiles(
                getDefaultProguardFile("proguard-android-optimize.txt"),
                "proguard-rules.pro"
            )
        }
    }

    compileOptions {
        sourceCompatibility = JavaVersion.VERSION_17
        targetCompatibility = JavaVersion.VERSION_17
    }

    kotlinOptions {
        jvmTarget = "17"
    }
}

dependencies {
    implementation(platform(libs.compose.bom))
    implementation(libs.compose.ui)
    implementation(libs.compose.material3)
    implementation(libs.compose.ui.tooling.preview)
    implementation(libs.lifecycle.viewmodel.compose)
    implementation(libs.navigation.compose)
    debugImplementation(libs.compose.ui.tooling)
}
```

The version catalog (`libs.versions.toml`) centralizes dependency versions:

```toml
# gradle/libs.versions.toml
[versions]
kotlin = "2.3.20"
agp = "9.1.0"
compose-bom = "2025.12.00"
lifecycle = "2.9.0"
navigation = "2.9.0"

[plugins]
android-application = { id = "com.android.application", version.ref = "agp" }
kotlin-android = { id = "org.jetbrains.kotlin.android", version.ref = "kotlin" }
compose-compiler = { id = "org.jetbrains.kotlin.plugin.compose", version.ref = "kotlin" }

[libraries]
compose-bom = { group = "androidx.compose", name = "compose-bom", version.ref = "compose-bom" }
compose-ui = { group = "androidx.compose.ui", name = "ui" }
compose-material3 = { group = "androidx.compose.material3", name = "material3" }
compose-ui-tooling = { group = "androidx.compose.ui", name = "ui-tooling" }
compose-ui-tooling-preview = { group = "androidx.compose.ui", name = "ui-tooling-preview" }
lifecycle-viewmodel-compose = { group = "androidx.lifecycle", name = "lifecycle-viewmodel-compose", version.ref = "lifecycle" }
navigation-compose = { group = "androidx.navigation", name = "navigation-compose", version.ref = "navigation" }
```

If you come from .NET, the version catalog is like `Directory.Packages.props` with central package management — it ensures all projects in the repository use the same dependency versions.

## Part 6: App Signing — What, How, and Why

### Why Apps Are Signed

Every Android APK (and AAB — Android App Bundle) must be digitally signed before it can be installed on a device. This is not optional. An unsigned APK will not install.

Signing serves three purposes:

1. **Identity.** The signature proves who published the app. If you sign version 1.0 and later publish version 2.0 with the same signature, Android knows both came from the same publisher. If an attacker tries to publish a modified version of your app, they cannot sign it with your key (unless they have stolen your key), so the signature will not match, and Android will refuse to install the update.

2. **Integrity.** The signature covers the entire contents of the APK. If any file inside the APK is modified after signing, the signature becomes invalid, and the APK will not install. This prevents tampering.

3. **Update trust.** Android only allows updates to an app if the update is signed with the same key as the installed version. This prevents an attacker from replacing a legitimate app with a malicious one through an "update."

This is conceptually similar to code signing in the .NET world — Authenticode signing for Windows executables, or NuGet package signing. The mechanism is different (Android uses JAR signing / APK Signature Scheme v2/v3/v4), but the purpose is identical.

### Keystore Files

A keystore is a file that contains one or more cryptographic key pairs (private key + certificate). Android uses the Java KeyStore (JKS) format or the newer PKCS12 format.

To create a keystore:

```bash
keytool -genkeypair \
  -v \
  -keystore my-release-key.jks \
  -keyalg RSA \
  -keysize 2048 \
  -validity 10000 \
  -alias my-key-alias \
  -storepass your_store_password \
  -keypass your_key_password \
  -dname "CN=Your Name, OU=Your Org, O=Your Company, L=Your City, ST=Your State, C=US"
```

This creates a JKS keystore with a 2048-bit RSA key pair that is valid for 10,000 days (about 27 years). You choose a store password (protects the keystore file) and a key password (protects the individual key within the keystore).

**Critical: Do not lose this keystore file. Do not lose the passwords.** If you lose them, you cannot update your app on Google Play. You will have to publish a new app with a new package name. Google Play App Signing (where Google holds a copy of your signing key) mitigates this somewhat, but the upload key is still your responsibility.

### Debug vs. Release Signing

When you build an app in debug mode (`./gradlew assembleDebug`), Android Studio automatically signs it with a debug keystore located at `~/.android/debug.keystore`. This key is self-signed, uses a known password (`android`), and is only valid for 30 years. It is fine for development but must never be used for production.

For release builds, you must sign with your own keystore. You can configure this in `build.gradle.kts`:

```kotlin
android {
    signingConfigs {
        create("release") {
            storeFile = file(System.getenv("KEYSTORE_PATH") ?: "release.jks")
            storePassword = System.getenv("KEYSTORE_PASSWORD") ?: ""
            keyAlias = System.getenv("KEY_ALIAS") ?: ""
            keyPassword = System.getenv("KEY_PASSWORD") ?: ""
        }
    }

    buildTypes {
        release {
            signingConfig = signingConfigs.getByName("release")
            isMinifyEnabled = true
            isShrinkResources = true
            proguardFiles(
                getDefaultProguardFile("proguard-android-optimize.txt"),
                "proguard-rules.pro"
            )
        }
    }
}
```

The signing credentials are read from environment variables, never hard-coded into the build file. This is critical for CI/CD — you never commit passwords to source control.

## Part 7: Containerfiles for Android Builds

Building Android apps in CI requires the JDK, the Android SDK, and the Android build tools. Instead of relying on a pre-configured CI environment (which can drift), you can use a Containerfile (Dockerfile) that contains everything needed to build your app.

Here is a comprehensive Containerfile:

```dockerfile
# Containerfile for building Android apps with Kotlin 2.3.20
# Uses Eclipse Temurin JDK 17 as the base

FROM eclipse-temurin:17-jdk-jammy AS builder

# Avoid interactive prompts during package installation
ENV DEBIAN_FRONTEND=noninteractive

# Install essential tools
RUN apt-get update && \
    apt-get install -y --no-install-recommends \
        curl \
        unzip \
        git \
        && rm -rf /var/lib/apt/lists/*

# Set up Android SDK
ENV ANDROID_HOME=/opt/android-sdk
ENV ANDROID_SDK_ROOT=${ANDROID_HOME}
ENV PATH="${ANDROID_HOME}/cmdline-tools/latest/bin:${ANDROID_HOME}/platform-tools:${PATH}"

# Download and install Android command-line tools
# Check https://developer.android.com/studio#command-line-tools-only for latest
ARG CMDLINE_TOOLS_URL="https://dl.google.com/android/repository/commandlinetools-linux-11076708_latest.zip"
RUN mkdir -p ${ANDROID_HOME}/cmdline-tools && \
    curl -fsSL "${CMDLINE_TOOLS_URL}" -o /tmp/cmdline-tools.zip && \
    unzip -q /tmp/cmdline-tools.zip -d /tmp/cmdline-tools && \
    mv /tmp/cmdline-tools/cmdline-tools ${ANDROID_HOME}/cmdline-tools/latest && \
    rm -rf /tmp/cmdline-tools.zip /tmp/cmdline-tools

# Accept licenses (required before installing SDK components)
RUN yes | sdkmanager --licenses > /dev/null 2>&1

# Install SDK components
# - platform-tools: adb, fastboot
# - platforms;android-35: Android 15 platform (API level 35, required for targetSdk 35)
# - build-tools;35.0.0: Build tools for compiling and packaging
RUN sdkmanager --install \
    "platform-tools" \
    "platforms;android-35" \
    "build-tools;35.0.0"

# Create a non-root user for building
RUN useradd -m -s /bin/bash builder
USER builder
WORKDIR /home/builder/project

# Copy Gradle wrapper first (for layer caching)
COPY --chown=builder:builder gradle/ gradle/
COPY --chown=builder:builder gradlew .
COPY --chown=builder:builder gradle.properties .

# Download Gradle distribution (cached in this layer)
RUN ./gradlew --version

# Copy build configuration files
COPY --chown=builder:builder build.gradle.kts .
COPY --chown=builder:builder settings.gradle.kts .
COPY --chown=builder:builder gradle/libs.versions.toml gradle/

# Copy app build files and download dependencies (cached)
COPY --chown=builder:builder app/build.gradle.kts app/
RUN ./gradlew dependencies --no-daemon || true

# Copy source code (this layer changes most frequently)
COPY --chown=builder:builder app/src/ app/src/

# Build the release APK
# The signing config will be provided via environment variables at build time
CMD ["./gradlew", "assembleRelease", "--no-daemon"]
```

This Containerfile uses multi-layer caching to speed up builds. The Gradle wrapper, build configuration, and dependencies are cached in separate layers. Only when source code changes does the build layer need to be re-executed.

To build using this Containerfile:

```bash
# Build the container image
podman build -t android-builder .

# Run the build with signing credentials
podman run --rm \
  -e KEYSTORE_PASSWORD="your_store_password" \
  -e KEY_ALIAS="my-key-alias" \
  -e KEY_PASSWORD="your_key_password" \
  -v ./release.jks:/home/builder/project/release.jks:ro \
  -v ./output:/home/builder/project/app/build/outputs:rw \
  android-builder
```

## Part 8: GitHub Actions — Building Signed Release APKs on Every Push

This is where everything comes together. We are going to set up a GitHub Actions workflow that:

1. Checks out the code
2. Sets up JDK 17
3. Caches Gradle dependencies
4. Decodes the signing keystore from a GitHub Secret
5. Builds a signed release APK
6. Uploads the APK as a build artifact
7. Runs on every push to every branch

### Setting Up Secrets

First, you need to store your signing credentials as GitHub Secrets:

1. **Encode your keystore as base64:**

```bash
base64 -i my-release-key.jks | tr -d '\n' > keystore_base64.txt
```

2. **Add these secrets in your GitHub repository** (Settings → Secrets and variables → Actions):

   - `KEYSTORE_BASE64` — the contents of `keystore_base64.txt`
   - `KEYSTORE_PASSWORD` — your store password
   - `KEY_ALIAS` — your key alias (e.g., `my-key-alias`)
   - `KEY_PASSWORD` — your key password

### The Workflow File

```yaml
# .github/workflows/build.yml
name: Build Signed Release APK

on:
  push:
    branches: ['*']
  pull_request:
    branches: [main]

# Cancel in-progress runs for the same branch
concurrency:
  group: build-${{ github.ref }}
  cancel-in-progress: true

jobs:
  build:
    runs-on: ubuntu-latest
    timeout-minutes: 30

    steps:
      - name: Checkout code
        uses: actions/checkout@v4

      - name: Set up JDK 17
        uses: actions/setup-java@v4
        with:
          java-version: '17'
          distribution: 'temurin'

      - name: Setup Gradle
        uses: gradle/actions/setup-gradle@v4
        with:
          cache-read-only: ${{ github.ref != 'refs/heads/main' }}

      - name: Decode keystore
        env:
          KEYSTORE_BASE64: ${{ secrets.KEYSTORE_BASE64 }}
        run: |
          echo "${KEYSTORE_BASE64}" | base64 --decode > ${{ github.workspace }}/release.jks

      - name: Build release APK
        env:
          KEYSTORE_PATH: ${{ github.workspace }}/release.jks
          KEYSTORE_PASSWORD: ${{ secrets.KEYSTORE_PASSWORD }}
          KEY_ALIAS: ${{ secrets.KEY_ALIAS }}
          KEY_PASSWORD: ${{ secrets.KEY_PASSWORD }}
        run: ./gradlew assembleRelease --no-daemon

      - name: Upload APK artifact
        uses: actions/upload-artifact@v4
        with:
          name: release-apk-${{ github.sha }}
          path: app/build/outputs/apk/release/*.apk
          retention-days: 30

      - name: Upload build reports (on failure)
        if: failure()
        uses: actions/upload-artifact@v4
        with:
          name: build-reports
          path: app/build/reports/
          retention-days: 7

      - name: Clean up keystore
        if: always()
        run: rm -f ${{ github.workspace }}/release.jks
```

Let us walk through each step:

**Checkout** clones the repository. Standard.

**Set up JDK 17** installs Eclipse Temurin JDK 17. Android development currently requires JDK 17 as the minimum. JDK 21 is also supported by recent AGP versions.

**Setup Gradle** configures Gradle with caching. The `cache-read-only` flag ensures that only builds on `main` write to the cache, while branch builds only read from it. This prevents cache pollution from experimental branches.

**Decode keystore** converts the base64-encoded keystore back into a binary `.jks` file. This is necessary because GitHub Secrets are text-only — you cannot store binary files directly.

**Build release APK** runs the Gradle build with signing credentials passed as environment variables. The `build.gradle.kts` reads these via `System.getenv()` as shown earlier.

**Upload APK artifact** makes the signed APK available for download from the Actions run page. Anyone with repository access can download it. The retention is set to 30 days.

**Upload build reports on failure** captures Gradle build reports if the build fails, making debugging easier.

**Clean up keystore** removes the decoded keystore file. The `if: always()` ensures this runs even if previous steps fail. This is defense-in-depth — the keystore file only exists for the duration of the build.

### Running Tests Before Building

In a real project, you should run tests before building the release APK. Add a test step before the build step:

```yaml
      - name: Run tests
        run: ./gradlew test --no-daemon

      - name: Run Android instrumented tests (optional)
        run: ./gradlew connectedAndroidTest --no-daemon
```

Or split into separate jobs where the build job depends on the test job:

```yaml
jobs:
  test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-java@v4
        with:
          java-version: '17'
          distribution: 'temurin'
      - uses: gradle/actions/setup-gradle@v4
      - run: ./gradlew test --no-daemon

  build:
    needs: test
    runs-on: ubuntu-latest
    steps:
      # ... build steps from above
```

## Part 9: Kotlin Multiplatform — Sharing Code Across Platforms

### What KMP Is

Kotlin Multiplatform (KMP) is a technology that allows you to write Kotlin code once and compile it for multiple platforms: JVM (Android, server), Native (iOS, macOS, Linux, Windows), JavaScript, and WebAssembly.

KMP is officially supported by Google for sharing business logic between Android and iOS. It is stable and production-ready. Companies including Netflix, VMware, Philips, and Cash App ship production KMP code to millions of users.

KMP is not the same as Compose Multiplatform. KMP handles shared business logic — networking, data persistence, domain models, algorithms. Compose Multiplatform (developed by JetBrains, built on top of KMP) handles shared UI. You can use KMP without Compose Multiplatform (sharing only logic, with native UI per platform), or you can use both together (sharing logic and UI).

As of April 2026, Compose Multiplatform 1.10.3 is the latest stable release. The Android, iOS, and desktop targets are Stable. The Web target (based on WebAssembly) is in Beta.

### Project Structure

A KMP project has a shared module with source sets for each platform:

```
shared/
├── src/
│   ├── commonMain/        ← Shared code (all platforms)
│   │   └── kotlin/
│   │       └── com/example/
│   │           ├── data/
│   │           │   └── ItemRepository.kt
│   │           └── model/
│   │               └── Item.kt
│   ├── commonTest/        ← Shared tests
│   │   └── kotlin/
│   ├── androidMain/       ← Android-specific code
│   │   └── kotlin/
│   ├── iosMain/           ← iOS-specific code
│   │   └── kotlin/
│   └── desktopMain/       ← Desktop-specific code
│       └── kotlin/
└── build.gradle.kts
```

Code in `commonMain` is available on all platforms. Code in `androidMain` is only available when compiling for Android. The `expect`/`actual` mechanism lets you declare an API in common code and provide platform-specific implementations:

```kotlin
// commonMain — declares the expectation
expect fun platformName(): String

// androidMain — provides the Android implementation
actual fun platformName(): String = "Android ${android.os.Build.VERSION.SDK_INT}"

// iosMain — provides the iOS implementation
actual fun platformName(): String = UIDevice.currentDevice.systemName() +
    " " + UIDevice.currentDevice.systemVersion
```

This is analogous to partial classes or conditional compilation in C#, but with stronger type-checking — the compiler ensures every `expect` declaration has a corresponding `actual` for each configured target.

### Sharing a ViewModel Across Platforms

Here is a practical example of sharing a ViewModel:

```kotlin
// commonMain
class ItemListViewModel(private val repository: ItemRepository) {
    private val _state = MutableStateFlow(ItemListState())
    val state: StateFlow<ItemListState> = _state.asStateFlow()

    fun loadItems() {
        _state.value = _state.value.copy(isLoading = true)
        // In real code, this would be in a coroutine scope
        // provided by the platform (viewModelScope on Android)
    }
}

data class ItemListState(
    val items: List<Item> = emptyList(),
    val isLoading: Boolean = false,
    val error: String? = null
)

data class Item(
    val id: String,
    val title: String,
    val completed: Boolean
)
```

On Android, you wrap this in a Jetpack ViewModel:

```kotlin
// androidMain
class AndroidItemListViewModel(
    repository: ItemRepository
) : ViewModel() {
    private val shared = ItemListViewModel(repository)
    val state = shared.state

    fun loadItems() {
        viewModelScope.launch {
            shared.loadItems()
        }
    }
}
```

On iOS, the SwiftUI view observes the shared state:

```swift
// In Swift
struct ItemListView: View {
    @StateObject private var viewModel = ItemListViewModelWrapper()

    var body: some View {
        List(viewModel.items) { item in
            Text(item.title)
        }
        .onAppear {
            viewModel.loadItems()
        }
    }
}
```

The business logic — what items to load, how to filter them, when to show loading states — is written once in Kotlin and shared across platforms. Only the UI layer is platform-specific.

## Part 10: Practical Project Setup — From Zero to Running App

Let us walk through creating a complete Android project from scratch, using the command line and a text editor. No wizard. No magic. Just files.

### Step 1: Project Structure

Create the following directory structure:

```
my-app/
├── app/
│   ├── src/
│   │   └── main/
│   │       ├── kotlin/
│   │       │   └── com/
│   │       │       └── example/
│   │       │           └── myapp/
│   │       │               ├── MainActivity.kt
│   │       │               ├── ui/
│   │       │               │   ├── theme/
│   │       │               │   │   └── Theme.kt
│   │       │               │   └── screens/
│   │       │               │       └── HomeScreen.kt
│   │       │               └── viewmodel/
│   │       │                   └── HomeViewModel.kt
│   │       ├── res/
│   │       │   ├── values/
│   │       │   │   ├── strings.xml
│   │       │   │   └── themes.xml
│   │       │   └── mipmap-xxxhdpi/
│   │       │       └── ic_launcher.webp
│   │       └── AndroidManifest.xml
│   ├── build.gradle.kts
│   └── proguard-rules.pro
├── gradle/
│   ├── wrapper/
│   │   ├── gradle-wrapper.jar
│   │   └── gradle-wrapper.properties
│   └── libs.versions.toml
├── build.gradle.kts
├── settings.gradle.kts
├── gradle.properties
└── gradlew
```

### Step 2: The Settings File

```kotlin
// settings.gradle.kts
pluginManagement {
    repositories {
        google()
        mavenCentral()
        gradlePluginPortal()
    }
}

dependencyResolution {
    repositories {
        google()
        mavenCentral()
    }
}

rootProject.name = "MyApp"
include(":app")
```

### Step 3: The Root Build File

```kotlin
// build.gradle.kts (root)
plugins {
    alias(libs.plugins.android.application) apply false
    alias(libs.plugins.kotlin.android) apply false
    alias(libs.plugins.compose.compiler) apply false
}
```

### Step 4: Gradle Properties

```properties
# gradle.properties
org.gradle.jvmargs=-Xmx2048m -Dfile.encoding=UTF-8
android.useAndroidX=true
kotlin.code.style=official
android.nonTransitiveRClass=true
```

### Step 5: Build and Run

```bash
# Generate the Gradle wrapper (if not already present)
gradle wrapper --gradle-version 9.4.1

# Build debug APK
./gradlew assembleDebug

# Build release APK (requires signing config)
./gradlew assembleRelease

# Run unit tests
./gradlew test

# Install on connected device
./gradlew installDebug
```

## Part 11: Bad Code vs. Good Code — Unlearning Bad Habits

This section is for you specifically — the ASP.NET developer with bad instincts. We are going to look at common patterns that C# developers bring to Kotlin that are wrong, and what the correct Kotlin approach is.

### Bad: Using Mutable State Everywhere

```kotlin
// BAD — mutable everything, imperative style
class UserManager {
    var users = ArrayList<User>()

    fun addUser(name: String, age: Int) {
        val user = User(name, age)
        users.add(user)
    }

    fun getAdults(): ArrayList<User> {
        val result = ArrayList<User>()
        for (user in users) {
            if (user.age >= 18) {
                result.add(user)
            }
        }
        return result
    }
}
```

```kotlin
// GOOD — immutable by default, functional operations
class UserManager {
    private val _users = mutableListOf<User>()
    val users: List<User> get() = _users.toList()

    fun addUser(name: String, age: Int) {
        _users.add(User(name, age))
    }

    fun getAdults(): List<User> = _users.filter { it.age >= 18 }
}
```

The good version uses `List<User>` (read-only) for the public interface, keeps the mutable list private, uses `.filter {}` instead of manual iteration, and uses expression body syntax for concise functions.

### Bad: Using `!!` to Silence the Compiler

```kotlin
// BAD — defeats null safety
fun getUserCity(user: User?): String {
    return user!!.address!!.city!!
}
```

```kotlin
// GOOD — handle nullability properly
fun getUserCity(user: User?): String {
    return user?.address?.city ?: "Unknown"
}
```

Every `!!` is a potential `NullPointerException`. The compiler is trying to help you. Listen to it.

### Bad: God Classes

```kotlin
// BAD — one class does everything
class AppManager(private val context: Context) {
    fun fetchUsers(): List<User> { /* network call */ }
    fun saveUser(user: User) { /* database call */ }
    fun showNotification(message: String) { /* UI logic */ }
    fun logEvent(event: String) { /* analytics */ }
    fun validateEmail(email: String): Boolean { /* validation */ }
    fun formatDate(date: LocalDate): String { /* formatting */ }
    // ... 2,000 more lines
}
```

```kotlin
// GOOD — single responsibility
class UserRepository(private val api: UserApi, private val db: UserDao) {
    suspend fun getUsers(): List<User> = api.fetchUsers()
    suspend fun saveUser(user: User) = db.insert(user)
}

class NotificationService(private val context: Context) {
    fun show(message: String) { /* notification logic */ }
}

class AnalyticsService {
    fun logEvent(event: String) { /* analytics logic */ }
}

class EmailValidator {
    fun isValid(email: String): Boolean =
        email.matches(Regex("^[A-Za-z0-9+_.-]+@[A-Za-z0-9.-]+$"))
}
```

Each class has one reason to change. The repository handles data access. The notification service handles notifications. The analytics service handles analytics. The validator handles validation. This is the Single Responsibility Principle, and it matters just as much in Kotlin as it does in C#.

### Bad: Ignoring Coroutine Structured Concurrency

```kotlin
// BAD — fire and forget, no cancellation, no error handling
fun loadData() {
    GlobalScope.launch {
        val data = api.fetchData()
        updateUi(data)
    }
}
```

```kotlin
// GOOD — structured concurrency with proper scope
class MyViewModel : ViewModel() {
    fun loadData() {
        viewModelScope.launch {
            try {
                val data = api.fetchData()
                _uiState.value = UiState.Success(data)
            } catch (e: CancellationException) {
                throw e  // Always rethrow CancellationException
            } catch (e: Exception) {
                _uiState.value = UiState.Error(e.message ?: "Unknown error")
            }
        }
    }
}
```

`GlobalScope.launch` is the coroutine equivalent of `Task.Run` without tracking the task — if the user navigates away, the coroutine keeps running, potentially updating UI that no longer exists. `viewModelScope.launch` is automatically cancelled when the ViewModel is cleared.

Note the `CancellationException` handling: in Kotlin coroutines, cancellation is cooperative and uses exceptions. You must always rethrow `CancellationException` — catching it and swallowing it breaks the cancellation mechanism.

## Part 12: Testing

### Unit Testing with JUnit and Kotlin

```kotlin
// src/test/kotlin/com/example/myapp/EmailValidatorTest.kt
import kotlin.test.Test
import kotlin.test.assertTrue
import kotlin.test.assertFalse

class EmailValidatorTest {
    private val validator = EmailValidator()

    @Test
    fun `valid email returns true`() {
        assertTrue(validator.isValid("alice@example.com"))
    }

    @Test
    fun `email without at sign returns false`() {
        assertFalse(validator.isValid("alice.example.com"))
    }

    @Test
    fun `empty string returns false`() {
        assertFalse(validator.isValid(""))
    }

    @Test
    fun `email with spaces returns false`() {
        assertFalse(validator.isValid("alice @example.com"))
    }
}
```

Kotlin allows backtick-quoted test names, which makes test names much more readable than `testValidEmailReturnsTrue`. The `kotlin.test` library provides platform-agnostic assertions that work on JVM, JS, and Native.

### Testing Composables

```kotlin
@OptIn(ExperimentalTestApi::class)
class CounterScreenTest {
    @get:Rule
    val composeTestRule = createComposeRule()

    @Test
    fun `counter starts at zero`() {
        composeTestRule.setContent {
            Counter()
        }

        composeTestRule
            .onNodeWithText("Current count: 0")
            .assertIsDisplayed()
    }

    @Test
    fun `clicking button increments counter`() {
        composeTestRule.setContent {
            Counter()
        }

        composeTestRule
            .onNodeWithText("Click me")
            .performClick()

        composeTestRule
            .onNodeWithText("Current count: 1")
            .assertIsDisplayed()
    }
}
```

This is similar to bUnit testing in Blazor — you render a component, interact with it, and assert on the resulting state.

### Testing Coroutines

```kotlin
class UserRepositoryTest {
    @Test
    fun `getUsers returns list from API`() = runTest {
        val fakeApi = object : UserApi {
            override suspend fun fetchUsers() = listOf(
                User("Alice", 30),
                User("Bob", 25)
            )
        }
        val repository = UserRepository(fakeApi)

        val users = repository.getUsers()

        assertEquals(2, users.size)
        assertEquals("Alice", users[0].name)
    }
}
```

`runTest` from `kotlinx-coroutines-test` provides a test coroutine scope with a virtual time dispatcher — `delay()` calls skip instantly instead of waiting real time.

## Part 13: Resources

Here are the authoritative resources you should bookmark:

- **Kotlin Language Documentation**: https://kotlinlang.org/docs/home.html
- **Kotlin Releases**: https://kotlinlang.org/docs/releases.html — Kotlin 2.3.20 is the current stable release (March 2026)
- **Jetpack Compose Documentation**: https://developer.android.com/develop/ui/compose
- **Android Developer Guides**: https://developer.android.com/guide
- **Kotlin Multiplatform**: https://kotlinlang.org/docs/multiplatform.html
- **Compose Multiplatform**: https://www.jetbrains.com/compose-multiplatform/
- **Kotlin Coroutines Guide**: https://kotlinlang.org/docs/coroutines-guide.html
- **Android Studio Downloads**: https://developer.android.com/studio — Latest stable is Android Studio Panda 3 (2025.3.3)
- **Gradle**: https://gradle.org — Latest stable is 9.4.1 (March 2026)
- **Android Gradle Plugin**: https://developer.android.com/build/releases/gradle-plugin — AGP 9.1.0 (March 2026)
- **Google Play Target SDK Requirements**: https://developer.android.com/google/play/requirements/target-sdk — New apps must target API 35 (Android 15)
- **Kotlin Playground**: https://play.kotlinlang.org — Try Kotlin in your browser
- **Compose Multiplatform Samples**: https://github.com/JetBrains/compose-multiplatform/tree/master/examples
- **Android Architecture Components**: https://developer.android.com/topic/libraries/architecture
- **KotlinConf**: https://kotlinconf.com — The official Kotlin conference, happening May 2026

Every version number in this article was verified via web search at the time of writing (April 2026). Technologies move fast. When in doubt, check the official documentation.

---

You started this article as a C# developer who knew nothing about Android. If you have read this far — and I mean actually read it, not skimmed the headings — you now understand the JVM and how it compares to the CLR. You understand Kotlin's type system, null safety, coroutines, and collection operations. You understand Jetpack Compose and how it compares to Blazor. You understand Android application architecture, the manifest, the build system, and signing. You understand how to set up a complete CI/CD pipeline that builds signed release APKs on every push. You understand Kotlin Multiplatform and how to share code across platforms.

You do not know everything. Nobody does. But you have a foundation. You know what to search for when you get stuck. You know the right questions to ask. You know where the bodies are buried — the null safety traps, the coroutine cancellation gotchas, the Gradle cache invalidation nightmares, the keystore management responsibilities.

Go build something. Make it small. Make it work. Make it tested. Then make it bigger. That is how you learn.
