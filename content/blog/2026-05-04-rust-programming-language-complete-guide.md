---
title: "The Rust Programming Language: High-Performance Computing from First Principles — Data Structures, Algorithms, and Everything You Were Afraid to Ask"
date: 2026-05-04
author: myblazor-team
summary: "A comprehensive, from-the-ground-up guide to the Rust programming language for developers who want to understand systems programming, memory management, ownership, data structures, algorithms, and high-performance computing — all without importing a single external crate. Every concept explained from first principles with bad code, good code, and the reasoning behind both."
featured: true
tags:
  - rust
  - systems-programming
  - performance
  - data-structures
  - algorithms
  - deep-dive
  - memory-safety
  - best-practices
  - software-engineering
---

## Part 1 — Why Rust Exists and Why You Should Care

Picture yourself on a Thursday afternoon. You have just deployed a C# ASP.NET web application that works — barely. The pages load, the database queries return results, and nobody has filed a bug report yet. You lean back in your chair, satisfied, and then someone says: "We need that component rewritten in something faster. The latency budget is two milliseconds."

Two milliseconds. Your garbage collector alone sometimes pauses for longer than that.

This is the world Rust was built for. Not the world of web forms and JSON serialization (though Rust can do those things), but the world where every microsecond counts, where a misplaced pointer dereference crashes an airplane's avionics system, where a buffer overflow in a network driver gives an attacker root access to every server in your data center.

You might be thinking: "I am a C# developer. I write web applications. Why should I care about systems programming?" The answer is that the infrastructure underneath your web application — the operating system kernel, the database engine, the network stack, the container runtime, the WebAssembly virtual machine that might one day run your Blazor code — all of it is written in languages like C, C++, and increasingly, Rust. Understanding how that layer works makes you a better developer at every layer above it. And understanding Rust specifically teaches you concepts — ownership, borrowing, lifetimes, zero-cost abstractions — that will permanently change how you think about software, even when you go back to writing C#.

### The Broken Elevator

Rust began in 2006 as a personal project by Graydon Hoare, a software developer at Mozilla. The origin story, as reported by MIT Technology Review, is almost too good to be true: Hoare was frustrated by a broken elevator in his apartment building — an elevator whose software had crashed. He started sketching out a language that would make it harder to write the kinds of bugs that crash elevators, corrupt databases, and bring down websites.

The name "Rust" comes from the rust fungi, a family of parasitic organisms that Hoare described as "over-engineered for survival." It is a fitting name for a language that prioritizes reliability and resilience above all else.

Between 2006 and 2009, Hoare worked on Rust in his spare time without publicizing it to anyone at Mozilla. When a small group at Mozilla became interested, the company officially sponsored the project in 2009. By 2010, the ownership system — the core innovation that makes Rust unique — was already in place.

Early Rust was quite different from the language we know today. It had an `obj` keyword for explicit object-oriented programming, a typestate system for tracking variable state changes, and even a garbage collector. Over the years, feature after feature was removed in favor of simplicity. The garbage collector was dropped entirely by 2013. The language was influenced by decades-old research from languages like CLU (1975), ML (1973), Erlang (1986), and Haskell (1990). As early Rust developer Manish Goregaokar put it, Rust is based on "mostly decades-old research."

On May 15, 2015, Rust 1.0 was released — the first stable version, with a promise that code compiling on Rust 1.0 would continue to compile on every future version. That promise has been kept for over eleven years.

### The Ownership Revolution

What makes Rust different from every other mainstream programming language is its ownership system. Let us be blunt about this: if you are coming from C#, Java, Python, JavaScript, or any garbage-collected language, the ownership system will feel strange and restrictive at first. You will fight the compiler. You will curse at error messages. You will wonder why a language would make simple things so complicated.

And then, one day, you will understand. And you will never look at memory the same way again.

In C#, when you write this:

```csharp
string name = "Alice";
string greeting = name;
Console.WriteLine(name); // This works fine
```

Both `name` and `greeting` point to the same string object on the managed heap. The garbage collector tracks how many references exist and frees the memory when all references go out of scope. You never think about it. The runtime handles everything.

In C, when you write this:

```c
char* name = malloc(6);
strcpy(name, "Alice");
char* greeting = name;
free(name);
printf("%s\n", greeting); // Use-after-free: UNDEFINED BEHAVIOR
```

You have just created a use-after-free bug. The memory pointed to by `greeting` has been freed. Reading from it might print garbage, might print "Alice" (if the memory has not been reused yet), might crash, or might execute arbitrary code that an attacker has injected. This is the kind of bug that causes approximately two-thirds of security vulnerabilities in systems software, according to research presented at the 2019 Linux Security Summit.

In Rust, when you write this:

```rust
fn main() {
    let name = String::from("Alice");
    let greeting = name; // Ownership MOVES from name to greeting
    println!("{}", name); // COMPILE ERROR: value used after move
}
```

The compiler refuses to compile this code. It tells you, at compile time, before your program ever runs, that you have tried to use a value after its ownership has been transferred. This is not a runtime check. There is no garbage collector. There is no performance penalty. The compiler simply will not let you write this bug.

This is the fundamental bargain of Rust: you give up some flexibility in how you write code, and in return, the compiler guarantees that an entire category of bugs — use-after-free, double-free, null pointer dereference, data races — cannot exist in your program.

### Where Rust Is Used Today

As of early 2026, Rust is the latest stable version 1.94.1. It has been voted the "most admired language" in the Stack Overflow Developer Survey for nine consecutive years. The Rust Foundation, established on February 8, 2021, counts Amazon Web Services, Google, Huawei, Microsoft, and Mozilla among its founding platinum members.

But the most significant development in Rust's recent history happened in December 2025, at the Linux Kernel Maintainers Summit in Tokyo. After three years of experimentation that began with Rust support landing in Linux 6.1 (October 2022), the assembled kernel developers — including Linus Torvalds himself — declared that Rust in the Linux kernel is no longer experimental. Rust is now a core language of the Linux kernel, alongside C and assembly. As Miguel Ojeda, the lead of the Rust for Linux project, wrote: "The experiment is done, i.e. Rust is here to stay."

Millions of Android devices already ship with Rust-written kernel components. The Nova GPU driver for NVIDIA graphics cards is being written in Rust. Microsoft has rewritten parts of Windows in Rust. Amazon Web Services uses Rust for Firecracker (the micro-VM that powers AWS Lambda and Fargate), for components of S3, and for the Bottlerocket operating system. Cloudflare uses Rust for its edge computing platform. Discord rewrote performance-critical services from Go to Rust, reducing tail latencies dramatically. The list goes on.

For you, the web developer reading this article, the takeaway is this: Rust is not a niche language for hobbyists. It is the language that the most demanding production systems on Earth are increasingly being built with. Learning it will teach you how computers actually work, and that knowledge transfers to everything you do.

---

## Part 2 — Installing Rust and Writing Your First Program

### Getting Started

Installing Rust is straightforward. Open a terminal and run:

```bash
curl --proto '=https' --tlsv1.2 -sSf https://sh.rustup.rs | sh
```

This installs `rustup`, the Rust toolchain manager. It manages the Rust compiler (`rustc`), the package manager and build tool (`cargo`), and the standard library. After installation, you can verify everything is working:

```bash
rustc --version
cargo --version
```

As of this writing, the current stable version is Rust 1.94.1.

`rustup` also allows you to install multiple toolchains (stable, beta, nightly) and switch between them. For this article, we will use the stable toolchain exclusively.

### Your First Rust Program

Create a new project:

```bash
cargo new hello_rust
cd hello_rust
```

This creates the following structure:

```
hello_rust/
├── Cargo.toml
└── src/
    └── main.rs
```

`Cargo.toml` is the project manifest (similar to a `.csproj` file in .NET). `src/main.rs` is the entry point. Open `src/main.rs` and you will see:

```rust
fn main() {
    println!("Hello, world!");
}
```

Run it:

```bash
cargo run
```

You should see `Hello, world!` printed to your terminal.

Let us break down every single element of this program, because if you are coming from C#, several things here are different from what you expect.

`fn main()` — This declares a function named `main`. In Rust, `fn` is the keyword for function declarations (not `public static void`). The `main` function is the entry point of every Rust binary, just like in C#, but there is no class wrapping it. Rust is not an object-oriented language in the Java/C# sense. There are no classes. There is no inheritance. (There are structs, enums, traits, and implementations, which we will cover extensively.)

`println!("Hello, world!");` — This is a macro invocation, not a function call. The exclamation mark `!` is the tell. Macros in Rust are a form of metaprogramming — `println!` is expanded at compile time into code that formats and prints text to standard output. We use a macro here instead of a function because `println!` needs to accept a variable number of arguments with different types, something that regular Rust functions cannot do (Rust does not have variadic functions like C's `printf`).

The semicolons at the end of statements are mandatory. The curly braces delimit the function body. There is no `using` statement, no `namespace`, no `class Program`. Just the function.

### Cargo.toml — The Project Manifest

Open `Cargo.toml`:

```toml
[package]
name = "hello_rust"
version = "0.1.0"
edition = "2024"

[dependencies]
```

The `edition` field is important. Rust uses editions to introduce backward-incompatible changes. There are four editions: 2015, 2018, 2021, and 2024 (the latest, released alongside Rust 1.85 in February 2025). Crates targeting different editions can interoperate seamlessly — a 2024-edition crate can depend on a 2015-edition crate and vice versa. New projects default to the latest edition.

The `[dependencies]` section is empty. We are going to keep it that way for this entire article. We will not import a single external crate. Everything we build will use only the Rust standard library. This is not because external crates are bad — the Rust ecosystem on crates.io is excellent — but because the goal of this article is to learn. You cannot learn how a house is built by ordering a pre-fabricated one.

### Variables and Mutability

Here is your first surprise if you are coming from C#: variables in Rust are immutable by default.

```rust
fn main() {
    let x = 5;
    x = 6; // COMPILE ERROR: cannot assign twice to immutable variable `x`
}
```

This is the opposite of C#, where `int x = 5; x = 6;` works without complaint. In Rust, you must explicitly opt into mutability:

```rust
fn main() {
    let mut x = 5;
    x = 6; // This works
    println!("x = {}", x);
}
```

Why does Rust default to immutability? Because immutable data is inherently thread-safe. You never need a lock to read data that cannot be written. You never have a race condition on a value that never changes. By making mutability explicit, Rust forces you to think about which data actually needs to change, and the compiler can verify that mutable data is only accessed from one place at a time.

This is one of those things that feels annoying at first and becomes second nature after a few weeks. You will find that most variables genuinely do not need to be mutable, and making them `let` instead of `let mut` communicates intent to the reader.

### Type Inference and Explicit Types

Rust has type inference, but it works differently from C#'s `var`. In Rust, the compiler infers types from context:

```rust
fn main() {
    let x = 5;           // inferred as i32 (32-bit signed integer)
    let y = 3.14;        // inferred as f64 (64-bit float)
    let name = "Alice";  // inferred as &str (string slice)
    let flag = true;     // inferred as bool
}
```

You can always be explicit:

```rust
fn main() {
    let x: i32 = 5;
    let y: f64 = 3.14;
    let name: &str = "Alice";
    let flag: bool = true;
}
```

Rust's primitive numeric types are:

| Type | Size | Range |
|------|------|-------|
| `i8` | 1 byte | -128 to 127 |
| `i16` | 2 bytes | -32,768 to 32,767 |
| `i32` | 4 bytes | -2,147,483,648 to 2,147,483,647 |
| `i64` | 8 bytes | -9.2 × 10^18 to 9.2 × 10^18 |
| `i128` | 16 bytes | -1.7 × 10^38 to 1.7 × 10^38 |
| `isize` | pointer-sized | architecture-dependent |
| `u8` | 1 byte | 0 to 255 |
| `u16` | 2 bytes | 0 to 65,535 |
| `u32` | 4 bytes | 0 to 4,294,967,295 |
| `u64` | 8 bytes | 0 to 1.8 × 10^19 |
| `u128` | 16 bytes | 0 to 3.4 × 10^38 |
| `usize` | pointer-sized | architecture-dependent |
| `f32` | 4 bytes | IEEE 754 single precision |
| `f64` | 8 bytes | IEEE 754 double precision |

Notice that Rust has 128-bit integers built into the language. No `BigInteger` class needed. Also notice `isize` and `usize` — these are pointer-sized integers, meaning they are 4 bytes on 32-bit systems and 8 bytes on 64-bit systems. Array indices in Rust are always `usize`.

This is different from C#, where `int` is always 32 bits regardless of platform. Rust makes the distinction explicit because when you are writing systems code, the difference between a 32-bit and 64-bit pointer matters enormously.

---

## Part 3 — Ownership, Borrowing, and Lifetimes — The Heart of Rust

This is the section that separates people who dabble in Rust from people who understand Rust. If you read nothing else in this article, read this section. Read it twice. Read it three times. Every concept in the rest of the article builds on what you learn here.

### The Three Rules of Ownership

Rust's ownership system is governed by three rules. These rules are checked at compile time by a component of the compiler called the borrow checker.

**Rule 1:** Each value in Rust has exactly one owner (a variable).

**Rule 2:** When the owner goes out of scope, the value is dropped (its memory is freed).

**Rule 3:** There can only be one owner at a time.

Let us see each rule in action.

### Rule 1 and Rule 2: One Owner, Automatic Cleanup

```rust
fn main() {
    {
        let s = String::from("hello"); // s is the owner of this String
        println!("{}", s);             // s is valid here
    } // s goes out of scope here. The String's memory is freed.
    
    // println!("{}", s); // ERROR: s is not in scope
}
```

When `s` goes out of scope at the closing brace, Rust calls a special function called `drop` on the `String`. This function frees the heap memory that the string was using. This happens deterministically — at exactly the point where the variable goes out of scope — not at some unpredictable future time when a garbage collector gets around to it.

This is similar to C#'s `IDisposable` pattern and `using` blocks, except in Rust it happens automatically for every type, every time, without you having to remember to write a `using` block or call `Dispose()`.

### Rule 3: Move Semantics

```rust
fn main() {
    let s1 = String::from("hello");
    let s2 = s1; // Ownership MOVES from s1 to s2
    
    println!("{}", s2); // This works: s2 is the owner
    // println!("{}", s1); // ERROR: s1 has been moved
}
```

When you assign `s1` to `s2`, Rust does not copy the string data. It does not increment a reference count. It *moves* the ownership from `s1` to `s2`. After the move, `s1` is no longer valid. The compiler enforces this.

Why? Think about what would happen without this rule. If both `s1` and `s2` pointed to the same heap memory, and both went out of scope, Rust would try to free the same memory twice — a double-free bug. In C, this is undefined behavior that can corrupt the heap, crash the program, or create a security vulnerability. In Rust, it simply cannot happen, because only one variable ever owns the data at a time.

**The bad way (what C lets you do):**

```c
// C code — DO NOT do this
char* s1 = malloc(6);
strcpy(s1, "hello");
char* s2 = s1;  // Both point to the same memory
free(s1);        // Free through s1
free(s2);        // Double-free! UNDEFINED BEHAVIOR
```

**The Rust way (the compiler stops you):**

```rust
fn main() {
    let s1 = String::from("hello");
    let s2 = s1; // Moved. s1 is no longer valid.
    // No double-free is possible.
}
```

### The Copy Trait: Small Types Are Different

Not everything in Rust uses move semantics. Types that are small and cheap to copy — like integers, floats, booleans, and characters — implement the `Copy` trait. For these types, assignment copies the value instead of moving it:

```rust
fn main() {
    let x = 5;
    let y = x; // x is COPIED, not moved
    println!("x = {}, y = {}", x, y); // Both are valid
}
```

This works because an integer is just 4 bytes on the stack. Copying 4 bytes is so cheap that there is no reason to move ownership. The compiler knows this because `i32` implements the `Copy` trait.

The general rule: if a type implements `Copy`, assignment copies. If it does not, assignment moves. Types that manage heap memory (like `String`, `Vec`, `HashMap`) do not implement `Copy`, because copying them would mean duplicating potentially large amounts of heap data.

If you actually want to duplicate a heap-allocated value, you use the `clone` method:

```rust
fn main() {
    let s1 = String::from("hello");
    let s2 = s1.clone(); // Explicitly copies all heap data
    println!("s1 = {}, s2 = {}", s1, s2); // Both are valid
}
```

`clone()` is explicit and visible. You can see it in the code and know that a potentially expensive heap allocation is happening. In C#, this kind of copy happens silently all the time through reference semantics and garbage collection. In Rust, you always know when memory is being allocated.

### Borrowing: References Without Ownership

Moving ownership everywhere would be impractical. Often you want a function to look at data without taking ownership of it. This is what borrowing is for.

```rust
fn calculate_length(s: &String) -> usize {
    s.len()
}

fn main() {
    let s = String::from("hello");
    let len = calculate_length(&s); // Borrow s (immutable reference)
    println!("The length of '{}' is {}.", s, len); // s is still valid
}
```

The `&` symbol creates a reference — a pointer to the data that does not own it. The function `calculate_length` borrows `s` for the duration of the call and then gives it back. After the call, `s` is still valid and owned by `main`.

References in Rust are like pointers in C, but with two critical safety guarantees enforced by the compiler:

**Guarantee 1:** A reference can never be null. If you have a `&String`, it always points to a valid `String`. There is no null reference exception in Rust.

**Guarantee 2:** A reference can never outlive the data it points to. The compiler verifies this at compile time through a system called lifetimes (which we will cover shortly).

### Mutable References

Immutable references (`&T`) let you read data. Mutable references (`&mut T`) let you modify data:

```rust
fn add_exclamation(s: &mut String) {
    s.push_str("!");
}

fn main() {
    let mut s = String::from("hello");
    add_exclamation(&mut s);
    println!("{}", s); // Prints "hello!"
}
```

But here is the critical rule: **you can have either one mutable reference or any number of immutable references to a value at the same time, but not both.**

```rust
fn main() {
    let mut s = String::from("hello");
    
    let r1 = &s;     // OK: immutable reference
    let r2 = &s;     // OK: another immutable reference
    println!("{} and {}", r1, r2);
    // r1 and r2 are no longer used after this point
    
    let r3 = &mut s;  // OK: mutable reference (no immutable refs active)
    println!("{}", r3);
}
```

But this fails:

```rust
fn main() {
    let mut s = String::from("hello");
    
    let r1 = &s;      // immutable reference
    let r2 = &mut s;   // ERROR: cannot borrow `s` as mutable because
                        // it is also borrowed as immutable
    println!("{}, {}", r1, r2);
}
```

Why this rule? It prevents data races at compile time. A data race occurs when two threads access the same memory simultaneously and at least one of them is writing. By ensuring that mutable access is exclusive, Rust makes it impossible to have two pieces of code modifying the same data at the same time — even in single-threaded programs. This eliminates not just data races, but also an enormous class of bugs involving iterator invalidation, aliased mutation, and more.

### Lifetimes: How Long References Live

Lifetimes are the most conceptually difficult feature in Rust. They are also the feature that provides some of the strongest safety guarantees. Let us build up to them carefully.

Consider this broken code:

```rust
fn main() {
    let r;
    {
        let x = 5;
        r = &x; // ERROR: `x` does not live long enough
    }
    println!("{}", r); // r would be a dangling reference
}
```

The variable `x` is created in the inner block and destroyed when that block ends. If we were allowed to store a reference to `x` in `r`, then `r` would be a dangling pointer — pointing to memory that has been freed. In C, this compiles without warning and causes undefined behavior. In Rust, the compiler rejects it.

The compiler tracks the lifetime of every reference and verifies that no reference outlives the data it points to. Most of the time, the compiler can figure out lifetimes automatically (this is called lifetime elision). But sometimes, when a function returns a reference, you need to annotate lifetimes explicitly:

```rust
fn longest<'a>(x: &'a str, y: &'a str) -> &'a str {
    if x.len() > y.len() {
        x
    } else {
        y
    }
}
```

The `'a` (pronounced "lifetime a" or "tick a") is a lifetime parameter. It tells the compiler: "The returned reference will live at least as long as both input references." This is necessary because the compiler cannot know at compile time whether `x` or `y` will be returned — that depends on runtime values. The lifetime annotation tells the compiler what it needs to know to verify safety.

Lifetime syntax looks strange at first. Here is how to read it:

- `&'a str` means "a reference to a `str` that lives for at least lifetime `'a`"
- `fn longest<'a>(x: &'a str, y: &'a str) -> &'a str` means "this function takes two string references that share a lifetime, and returns a reference with that same lifetime"

In practice, you will find that you rarely need to write explicit lifetime annotations. The compiler's elision rules handle the vast majority of cases. But understanding lifetimes is essential for understanding what the compiler is doing for you.

---

## Part 4 — The Type System: Structs, Enums, Traits, and Generics

If you are a C# developer, you are used to classes with inheritance, interfaces, abstract methods, and generics. Rust has some of these concepts but implements them very differently. There are no classes. There is no inheritance. Instead, Rust uses structs (data), enums (variants), traits (behavior), and generics (polymorphism).

### Structs

A struct is like a C# class without methods (initially) or inheritance (ever):

```rust
struct Point {
    x: f64,
    y: f64,
}

fn main() {
    let p = Point { x: 1.0, y: 2.0 };
    println!("Point: ({}, {})", p.x, p.y);
}
```

You add methods to a struct using an `impl` block:

```rust
struct Point {
    x: f64,
    y: f64,
}

impl Point {
    // Constructor (by convention, called `new`)
    fn new(x: f64, y: f64) -> Self {
        Point { x, y }
    }
    
    // Method that borrows self immutably
    fn distance_from_origin(&self) -> f64 {
        (self.x * self.x + self.y * self.y).sqrt()
    }
    
    // Method that borrows self mutably
    fn translate(&mut self, dx: f64, dy: f64) {
        self.x += dx;
        self.y += dy;
    }
    
    // Method that takes ownership of self (consumes the point)
    fn into_tuple(self) -> (f64, f64) {
        (self.x, self.y)
    }
}

fn main() {
    let mut p = Point::new(3.0, 4.0);
    println!("Distance: {}", p.distance_from_origin()); // 5.0
    
    p.translate(1.0, 1.0);
    println!("After translation: ({}, {})", p.x, p.y);
    
    let (x, y) = p.into_tuple(); // p is consumed (moved)
    // println!("{}", p.x); // ERROR: p has been moved
}
```

Notice the three kinds of `self`:
- `&self` — borrows immutably (like C#'s `this` in a regular method)
- `&mut self` — borrows mutably (like C#'s `this` in a method that modifies fields)
- `self` — takes ownership (the struct is consumed and cannot be used afterward)

The third form is unusual for C# developers. It is used for methods that transform a value into something else, and it is a powerful pattern because it makes it impossible to accidentally use the original value after the transformation.

### Enums: The Most Powerful Feature You Are Not Used To

Rust's enums are not the weak `enum` you know from C#. In C#, an enum is just a named integer:

```csharp
enum Direction { North, South, East, West }
```

In Rust, each variant of an enum can hold data:

```rust
enum Shape {
    Circle(f64),                    // radius
    Rectangle(f64, f64),            // width, height
    Triangle(f64, f64, f64),        // three sides
}

fn area(shape: &Shape) -> f64 {
    match shape {
        Shape::Circle(radius) => std::f64::consts::PI * radius * radius,
        Shape::Rectangle(w, h) => w * h,
        Shape::Triangle(a, b, c) => {
            let s = (a + b + c) / 2.0;
            (s * (s - a) * (s - b) * (s - c)).sqrt()
        }
    }
}

fn main() {
    let shapes = vec![
        Shape::Circle(5.0),
        Shape::Rectangle(4.0, 6.0),
        Shape::Triangle(3.0, 4.0, 5.0),
    ];
    
    for shape in &shapes {
        println!("Area: {:.2}", area(shape));
    }
}
```

The `match` expression is like C#'s `switch`, but it is exhaustive — the compiler verifies that you have handled every possible variant. If you add a new variant to `Shape` and forget to handle it in a `match`, the code will not compile.

### Option and Result: No More Null

The two most important enums in Rust are `Option<T>` and `Result<T, E>`. They are defined in the standard library:

```rust
enum Option<T> {
    Some(T),
    None,
}

enum Result<T, E> {
    Ok(T),
    Err(E),
}
```

`Option<T>` replaces null. In C#, any reference type can be null, and you might get a `NullReferenceException` at runtime. In Rust, there is no null. If a value might be absent, you use `Option<T>`:

```rust
fn find_user(id: u64) -> Option<String> {
    if id == 1 {
        Some(String::from("Alice"))
    } else {
        None
    }
}

fn main() {
    match find_user(1) {
        Some(name) => println!("Found user: {}", name),
        None => println!("User not found"),
    }
    
    // Or more concisely:
    if let Some(name) = find_user(1) {
        println!("Found: {}", name);
    }
}
```

`Result<T, E>` replaces exceptions. In C#, functions throw exceptions, and you catch them (or forget to catch them, and your application crashes). In Rust, functions that can fail return `Result`:

```rust
use std::num::ParseIntError;

fn parse_number(s: &str) -> Result<i32, ParseIntError> {
    s.parse::<i32>()
}

fn main() {
    match parse_number("42") {
        Ok(n) => println!("Parsed: {}", n),
        Err(e) => println!("Error: {}", e),
    }
    
    match parse_number("not_a_number") {
        Ok(n) => println!("Parsed: {}", n),
        Err(e) => println!("Error: {}", e),
    }
}
```

The `?` operator is syntactic sugar for propagating errors:

```rust
use std::fs;
use std::io;

fn read_username_from_file() -> Result<String, io::Error> {
    let contents = fs::read_to_string("username.txt")?;
    // The ? means: if this returns Err, return the Err immediately.
    // If it returns Ok, unwrap the value and continue.
    Ok(contents.trim().to_string())
}
```

This is vastly superior to exception-based error handling for several reasons:

1. **Errors are in the type signature.** You can see from the function signature that `read_username_from_file` might fail with an `io::Error`. In C#, you have to read the documentation (or the source code) to know what exceptions a function might throw.

2. **You cannot ignore errors.** If a function returns `Result`, you must handle the error case. In C#, you can silently ignore exceptions by not writing a try-catch block.

3. **There is no performance cost for the error path.** In C#, throwing an exception involves capturing a stack trace, which is expensive. In Rust, returning an `Err` is as cheap as returning any other value.

### Traits: Behavior Without Inheritance

Traits in Rust are similar to interfaces in C#, but more powerful. A trait defines a set of methods that a type must implement:

```rust
trait Describable {
    fn describe(&self) -> String;
}

struct Dog {
    name: String,
    breed: String,
}

struct Car {
    make: String,
    model: String,
    year: u16,
}

impl Describable for Dog {
    fn describe(&self) -> String {
        format!("{} is a {}", self.name, self.breed)
    }
}

impl Describable for Car {
    fn describe(&self) -> String {
        format!("{} {} {}", self.year, self.make, self.model)
    }
}

fn print_description(item: &dyn Describable) {
    println!("{}", item.describe());
}

fn main() {
    let dog = Dog {
        name: String::from("Rex"),
        breed: String::from("German Shepherd"),
    };
    let car = Car {
        make: String::from("Toyota"),
        model: String::from("Camry"),
        year: 2007,
    };
    
    print_description(&dog);
    print_description(&car);
}
```

Traits can have default implementations:

```rust
trait Summary {
    fn summarize_author(&self) -> String;
    
    fn summarize(&self) -> String {
        format!("(Read more from {}...)", self.summarize_author())
    }
}
```

Traits can be used as generic bounds:

```rust
fn largest<T: PartialOrd>(list: &[T]) -> &T {
    let mut largest = &list[0];
    for item in &list[1..] {
        if item > largest {
            largest = item;
        }
    }
    largest
}
```

This is Rust's version of C#'s generic constraints (like `where T : IComparable<T>`). The difference is that in Rust, the compiler monomorphizes generic functions — it generates a specialized version of the function for each concrete type it is called with. This means generic code in Rust has zero runtime overhead. There is no boxing. There is no virtual dispatch. The compiler generates code that is as fast as if you had written separate functions for each type by hand.

---

## Part 5 — Data Structures from the Standard Library

Now we get to the core of this article: data structures. We will explore every major data structure in the Rust standard library, understand how each one works internally, analyze the time complexity of each operation, and see practical examples. We will not import a single crate. Everything here is built into Rust.

### Vec — The Dynamic Array

`Vec<T>` is Rust's growable array, equivalent to `List<T>` in C# or `std::vector<T>` in C++. It stores elements contiguously in heap-allocated memory.

```rust
fn main() {
    // Creating vectors
    let mut v: Vec<i32> = Vec::new();       // Empty vector
    let v2 = vec![1, 2, 3, 4, 5];          // Using the vec! macro
    let v3 = vec![0; 10];                   // 10 zeros
    
    // Adding elements
    v.push(1);
    v.push(2);
    v.push(3);
    
    // Accessing elements
    let third = v[2];                        // Panics if out of bounds
    let third_safe = v.get(2);               // Returns Option<&T>
    
    // Iterating
    for val in &v {
        println!("{}", val);
    }
    
    // Removing elements
    let last = v.pop();                      // Returns Option<T>
    let removed = v.remove(0);               // Removes and returns element at index
    
    println!("Length: {}, Capacity: {}", v.len(), v.capacity());
}
```

**Internal implementation:** A `Vec<T>` consists of three fields: a pointer to heap-allocated memory, a length (how many elements are stored), and a capacity (how many elements the allocated memory can hold). When you push an element and the length equals the capacity, the vector allocates a new buffer (typically double the current capacity), copies all elements to the new buffer, and frees the old one.

**Time complexities:**

| Operation | Time Complexity |
|-----------|----------------|
| `push` (amortized) | O(1) |
| `pop` | O(1) |
| `get(index)` / `[index]` | O(1) |
| `insert(index, value)` | O(n) — shifts elements |
| `remove(index)` | O(n) — shifts elements |
| `contains` | O(n) |
| `sort` | O(n log n) |

**Bad code — reallocating in a tight loop:**

```rust
// BAD: Creates a new vector every iteration
fn bad_collect_even(data: &[i32]) -> Vec<i32> {
    let mut result = Vec::new(); // Starts with capacity 0
    for &val in data {
        if val % 2 == 0 {
            result.push(val); // May reallocate multiple times
        }
    }
    result
}
```

**Good code — pre-allocating capacity:**

```rust
// GOOD: Pre-allocates estimated capacity
fn good_collect_even(data: &[i32]) -> Vec<i32> {
    let mut result = Vec::with_capacity(data.len() / 2);
    for &val in data {
        if val % 2 == 0 {
            result.push(val);
        }
    }
    result
}
```

**Even better — using iterators:**

```rust
// BEST: Functional style, often optimized better by the compiler
fn best_collect_even(data: &[i32]) -> Vec<i32> {
    data.iter()
        .copied()
        .filter(|&val| val % 2 == 0)
        .collect()
}
```

The iterator version is not just more readable — it can often be more efficient because the compiler can optimize the entire chain into a single pass with no intermediate allocations.

### String and &str — Text in Rust

Strings in Rust are one of the first pain points for newcomers, because Rust has two main string types:

- `String` — an owned, heap-allocated, growable UTF-8 string (like `Vec<u8>` that guarantees valid UTF-8)
- `&str` — a borrowed, immutable view into a string (a "string slice")

```rust
fn main() {
    // String: owned, heap-allocated
    let mut owned = String::from("Hello");
    owned.push_str(", world!");
    owned.push('!');
    
    // &str: borrowed slice
    let slice: &str = "Hello, world!"; // String literal (stored in binary)
    let slice2: &str = &owned[0..5];   // Slice of an owned String
    
    // Converting between them
    let s: String = slice.to_string();
    let s2: String = String::from(slice);
    let borrowed: &str = &owned;
    
    // String operations (all from standard library, no crates)
    println!("Length: {}", owned.len());          // Bytes, not characters!
    println!("Is empty: {}", owned.is_empty());
    println!("Contains 'world': {}", owned.contains("world"));
    println!("Uppercase: {}", owned.to_uppercase());
    println!("Trimmed: '{}'", "  hello  ".trim());
    
    // Splitting
    for word in "one two three".split_whitespace() {
        println!("Word: {}", word);
    }
    
    // Replacing
    let replaced = owned.replace("world", "Rust");
    println!("{}", replaced);
}
```

**Critical understanding:** Rust strings are UTF-8 encoded. This means that `len()` returns the number of bytes, not the number of characters. A character like 'é' takes 2 bytes. A Chinese character might take 3 bytes. An emoji might take 4 bytes. You cannot index a `String` by character position with `s[0]` because that would require scanning from the beginning to count characters — an O(n) operation that looks like O(1) and would violate Rust's principle of making performance costs visible.

```rust
fn main() {
    let hello = String::from("Здравствуйте"); // Russian "Hello"
    println!("Bytes: {}", hello.len());       // 24 bytes
    println!("Chars: {}", hello.chars().count()); // 12 characters
    
    // Iterating over characters
    for c in hello.chars() {
        print!("{} ", c);
    }
    println!();
    
    // Iterating over bytes
    for b in hello.bytes() {
        print!("{} ", b);
    }
    println!();
}
```

### HashMap — The Hash Table

`HashMap<K, V>` is Rust's hash map, equivalent to `Dictionary<TKey, TValue>` in C#.

```rust
use std::collections::HashMap;

fn main() {
    let mut scores: HashMap<String, i32> = HashMap::new();
    
    // Inserting
    scores.insert(String::from("Alice"), 100);
    scores.insert(String::from("Bob"), 85);
    scores.insert(String::from("Charlie"), 92);
    
    // Accessing
    if let Some(score) = scores.get("Alice") {
        println!("Alice's score: {}", score);
    }
    
    // Iterating
    for (name, score) in &scores {
        println!("{}: {}", name, score);
    }
    
    // Entry API — insert only if key does not exist
    scores.entry(String::from("David")).or_insert(0);
    
    // Entry API — modify existing value
    let counter = scores.entry(String::from("Alice")).or_insert(0);
    *counter += 10; // Alice now has 110
    
    // Removing
    scores.remove("Bob");
    
    println!("Number of entries: {}", scores.len());
}
```

**The Entry API** is one of Rust's most elegant features for hash maps. It lets you look up a key and decide what to do based on whether it exists, all in one operation. This is particularly useful for counting:

```rust
use std::collections::HashMap;

fn word_count(text: &str) -> HashMap<&str, usize> {
    let mut counts = HashMap::new();
    for word in text.split_whitespace() {
        let count = counts.entry(word).or_insert(0);
        *count += 1;
    }
    counts
}

fn main() {
    let text = "the cat sat on the mat the cat";
    let counts = word_count(text);
    for (word, count) in &counts {
        println!("{}: {}", word, count);
    }
}
```

**Time complexities:**

| Operation | Average | Worst Case |
|-----------|---------|------------|
| `insert` | O(1) | O(n) — rehashing |
| `get` | O(1) | O(n) — hash collision |
| `remove` | O(1) | O(n) |
| `contains_key` | O(1) | O(n) |

### BTreeMap — The Ordered Map

If you need your keys to be sorted, use `BTreeMap<K, V>`:

```rust
use std::collections::BTreeMap;

fn main() {
    let mut map = BTreeMap::new();
    map.insert("Charlie", 92);
    map.insert("Alice", 100);
    map.insert("Bob", 85);
    
    // Iteration is in sorted key order
    for (name, score) in &map {
        println!("{}: {}", name, score);
    }
    // Output:
    // Alice: 100
    // Bob: 85
    // Charlie: 92
    
    // Range queries
    for (name, score) in map.range("A".."C") {
        println!("{}: {}", name, score);
    }
}
```

**Time complexities:** All operations are O(log n) — insert, get, remove. The internal structure is a B-tree, which is cache-friendly because each node contains multiple keys (unlike a binary search tree where each node has one key).

### HashSet and BTreeSet

Sets are maps without values:

```rust
use std::collections::HashSet;

fn main() {
    let mut set_a: HashSet<i32> = [1, 2, 3, 4, 5].iter().copied().collect();
    let set_b: HashSet<i32> = [3, 4, 5, 6, 7].iter().copied().collect();
    
    // Set operations
    let union: HashSet<_> = set_a.union(&set_b).copied().collect();
    let intersection: HashSet<_> = set_a.intersection(&set_b).copied().collect();
    let difference: HashSet<_> = set_a.difference(&set_b).copied().collect();
    let symmetric_diff: HashSet<_> = set_a.symmetric_difference(&set_b).copied().collect();
    
    println!("Union: {:?}", union);
    println!("Intersection: {:?}", intersection);
    println!("Difference (A-B): {:?}", difference);
    println!("Symmetric Difference: {:?}", symmetric_diff);
    
    // Membership
    println!("Contains 3: {}", set_a.contains(&3));
    
    // Insert and remove
    set_a.insert(10);
    set_a.remove(&1);
}
```

### VecDeque — The Double-Ended Queue

`VecDeque<T>` is a ring buffer that supports efficient push and pop from both ends:

```rust
use std::collections::VecDeque;

fn main() {
    let mut deque = VecDeque::new();
    
    deque.push_back(1);
    deque.push_back(2);
    deque.push_back(3);
    deque.push_front(0);
    
    println!("{:?}", deque); // [0, 1, 2, 3]
    
    deque.pop_front(); // Removes 0
    deque.pop_back();  // Removes 3
    
    println!("{:?}", deque); // [1, 2]
}
```

This is useful for implementing queues (FIFO), sliding windows, and breadth-first search.

### LinkedList — When You Actually Need One (You Probably Do Not)

Rust has `LinkedList<T>`, but you almost never want to use it:

```rust
use std::collections::LinkedList;

fn main() {
    let mut list = LinkedList::new();
    list.push_back(1);
    list.push_back(2);
    list.push_back(3);
    list.push_front(0);
    
    for val in &list {
        print!("{} ", val);
    }
    println!();
}
```

**Why you probably do not want LinkedList:** Linked lists have terrible cache locality. Each node is a separate heap allocation, and nodes are scattered across memory. Modern CPUs are extremely fast at accessing contiguous memory (like a `Vec`) and extremely slow at chasing pointers to random locations (like a linked list). In practice, a `Vec` or `VecDeque` is almost always faster than a `LinkedList`, even for operations where the linked list has theoretically better time complexity.

The official Rust documentation says: "It is almost always better to use `Vec` or `VecDeque` instead of `LinkedList`."

### BinaryHeap — The Priority Queue

`BinaryHeap<T>` is a max-heap (the largest element is always at the top):

```rust
use std::collections::BinaryHeap;

fn main() {
    let mut heap = BinaryHeap::new();
    heap.push(3);
    heap.push(1);
    heap.push(4);
    heap.push(1);
    heap.push(5);
    heap.push(9);
    
    // Peek at the maximum
    println!("Max: {:?}", heap.peek()); // Some(9)
    
    // Pop elements in descending order
    while let Some(val) = heap.pop() {
        print!("{} ", val);
    }
    println!(); // 9 5 4 3 1 1
}
```

For a min-heap, wrap your values in `std::cmp::Reverse`:

```rust
use std::collections::BinaryHeap;
use std::cmp::Reverse;

fn main() {
    let mut min_heap = BinaryHeap::new();
    min_heap.push(Reverse(3));
    min_heap.push(Reverse(1));
    min_heap.push(Reverse(4));
    
    while let Some(Reverse(val)) = min_heap.pop() {
        print!("{} ", val);
    }
    println!(); // 1 3 4
}
```

---

## Part 6 — Building Data Structures From Scratch

Now we build data structures ourselves, using no crates, to understand how they work internally.

### A Stack

A stack is the simplest collection: last in, first out (LIFO). We will implement it using a `Vec` as the backing store.

```rust
struct Stack<T> {
    elements: Vec<T>,
}

impl<T> Stack<T> {
    fn new() -> Self {
        Stack { elements: Vec::new() }
    }
    
    fn with_capacity(capacity: usize) -> Self {
        Stack { elements: Vec::with_capacity(capacity) }
    }
    
    fn push(&mut self, item: T) {
        self.elements.push(item);
    }
    
    fn pop(&mut self) -> Option<T> {
        self.elements.pop()
    }
    
    fn peek(&self) -> Option<&T> {
        self.elements.last()
    }
    
    fn is_empty(&self) -> bool {
        self.elements.is_empty()
    }
    
    fn len(&self) -> usize {
        self.elements.len()
    }
}

fn main() {
    let mut stack = Stack::new();
    stack.push(1);
    stack.push(2);
    stack.push(3);
    
    while let Some(val) = stack.pop() {
        println!("{}", val); // 3, 2, 1
    }
}
```

### A Queue Using Two Stacks

This is a classic data structure interview question. A queue (FIFO) can be implemented using two stacks so that both enqueue and dequeue are amortized O(1):

```rust
struct Queue<T> {
    inbox: Vec<T>,
    outbox: Vec<T>,
}

impl<T> Queue<T> {
    fn new() -> Self {
        Queue {
            inbox: Vec::new(),
            outbox: Vec::new(),
        }
    }
    
    fn enqueue(&mut self, item: T) {
        self.inbox.push(item);
    }
    
    fn dequeue(&mut self) -> Option<T> {
        if self.outbox.is_empty() {
            while let Some(item) = self.inbox.pop() {
                self.outbox.push(item);
            }
        }
        self.outbox.pop()
    }
    
    fn peek(&self) -> Option<&T> {
        if self.outbox.is_empty() {
            self.inbox.first()
        } else {
            self.outbox.last()
        }
    }
    
    fn is_empty(&self) -> bool {
        self.inbox.is_empty() && self.outbox.is_empty()
    }
    
    fn len(&self) -> usize {
        self.inbox.len() + self.outbox.len()
    }
}

fn main() {
    let mut q = Queue::new();
    q.enqueue("first");
    q.enqueue("second");
    q.enqueue("third");
    
    while let Some(val) = q.dequeue() {
        println!("{}", val); // first, second, third
    }
}
```

**Why this works:** When you dequeue and the outbox is empty, you transfer everything from the inbox to the outbox, reversing the order. Each element is moved at most twice (once into the outbox, once out of the outbox), so the amortized cost per operation is O(1).

### A Simple Hash Map From Scratch

Let us build a basic hash map to understand how hashing works. We will use linear probing for collision resolution.

```rust
use std::hash::{Hash, Hasher};
use std::collections::hash_map::DefaultHasher;

const INITIAL_CAPACITY: usize = 16;
const LOAD_FACTOR_THRESHOLD: f64 = 0.75;

struct Entry<K, V> {
    key: K,
    value: V,
}

struct SimpleHashMap<K, V> {
    buckets: Vec<Option<Entry<K, V>>>,
    len: usize,
}

impl<K: Hash + Eq, V> SimpleHashMap<K, V> {
    fn new() -> Self {
        let mut buckets = Vec::with_capacity(INITIAL_CAPACITY);
        for _ in 0..INITIAL_CAPACITY {
            buckets.push(None);
        }
        SimpleHashMap { buckets, len: 0 }
    }
    
    fn hash_key(&self, key: &K) -> usize {
        let mut hasher = DefaultHasher::new();
        key.hash(&mut hasher);
        (hasher.finish() as usize) % self.buckets.len()
    }
    
    fn insert(&mut self, key: K, value: V) -> Option<V> {
        // Check load factor and resize if needed
        let load_factor = self.len as f64 / self.buckets.len() as f64;
        if load_factor > LOAD_FACTOR_THRESHOLD {
            self.resize();
        }
        
        let mut index = self.hash_key(&key);
        
        // Linear probing
        loop {
            match &self.buckets[index] {
                None => {
                    self.buckets[index] = Some(Entry { key, value });
                    self.len += 1;
                    return None;
                }
                Some(entry) if entry.key == key => {
                    // Key exists — replace value
                    let old_entry = self.buckets[index].take().unwrap();
                    self.buckets[index] = Some(Entry { key, value });
                    return Some(old_entry.value);
                }
                Some(_) => {
                    index = (index + 1) % self.buckets.len();
                }
            }
        }
    }
    
    fn get(&self, key: &K) -> Option<&V> {
        let mut index = self.hash_key(key);
        let start = index;
        
        loop {
            match &self.buckets[index] {
                None => return None,
                Some(entry) if entry.key == *key => return Some(&entry.value),
                Some(_) => {
                    index = (index + 1) % self.buckets.len();
                    if index == start {
                        return None; // Wrapped around
                    }
                }
            }
        }
    }
    
    fn resize(&mut self) {
        let new_capacity = self.buckets.len() * 2;
        let old_buckets = std::mem::replace(
            &mut self.buckets,
            {
                let mut v = Vec::with_capacity(new_capacity);
                for _ in 0..new_capacity {
                    v.push(None);
                }
                v
            },
        );
        
        self.len = 0;
        for bucket in old_buckets {
            if let Some(entry) = bucket {
                self.insert(entry.key, entry.value);
            }
        }
    }
    
    fn len(&self) -> usize {
        self.len
    }
}

fn main() {
    let mut map = SimpleHashMap::new();
    map.insert("name", "Alice");
    map.insert("age", "30");
    map.insert("city", "Portland");
    
    println!("name: {:?}", map.get(&"name"));   // Some("Alice")
    println!("age: {:?}", map.get(&"age"));     // Some("30")
    println!("missing: {:?}", map.get(&"foo")); // None
    println!("Size: {}", map.len());             // 3
}
```

This implementation teaches several important concepts:

1. **Hashing:** We use Rust's `Hash` trait and `DefaultHasher` to convert keys into numeric indices. The modulo operation maps the hash to a valid index.

2. **Collision resolution:** When two keys hash to the same index (a collision), we probe forward to find an empty slot. This is called linear probing. The standard library's `HashMap` uses Robin Hood hashing (now SwissTable-based), which is more sophisticated but uses the same principle.

3. **Load factor:** When the table gets too full (more than 75% capacity), we resize to maintain O(1) average performance. Without resizing, the number of collisions grows and performance degrades to O(n).

4. **Generic constraints:** The function signature `impl<K: Hash + Eq, V>` says that the key type must implement both `Hash` (so we can compute hash values) and `Eq` (so we can compare keys for equality).

### A Binary Search Tree

```rust
use std::cmp::Ordering;
use std::fmt;

struct BstNode<T> {
    value: T,
    left: Option<Box<BstNode<T>>>,
    right: Option<Box<BstNode<T>>>,
}

struct Bst<T> {
    root: Option<Box<BstNode<T>>>,
    len: usize,
}

impl<T: Ord> Bst<T> {
    fn new() -> Self {
        Bst { root: None, len: 0 }
    }
    
    fn insert(&mut self, value: T) {
        fn insert_node<T: Ord>(node: &mut Option<Box<BstNode<T>>>, value: T) -> bool {
            match node {
                None => {
                    *node = Some(Box::new(BstNode {
                        value,
                        left: None,
                        right: None,
                    }));
                    true
                }
                Some(n) => match value.cmp(&n.value) {
                    Ordering::Less => insert_node(&mut n.left, value),
                    Ordering::Greater => insert_node(&mut n.right, value),
                    Ordering::Equal => false, // No duplicates
                },
            }
        }
        
        if insert_node(&mut self.root, value) {
            self.len += 1;
        }
    }
    
    fn contains(&self, value: &T) -> bool {
        fn search<T: Ord>(node: &Option<Box<BstNode<T>>>, value: &T) -> bool {
            match node {
                None => false,
                Some(n) => match value.cmp(&n.value) {
                    Ordering::Less => search(&n.left, value),
                    Ordering::Greater => search(&n.right, value),
                    Ordering::Equal => true,
                },
            }
        }
        
        search(&self.root, value)
    }
    
    fn in_order(&self) -> Vec<&T> {
        fn traverse<'a, T>(node: &'a Option<Box<BstNode<T>>>, result: &mut Vec<&'a T>) {
            if let Some(n) = node {
                traverse(&n.left, result);
                result.push(&n.value);
                traverse(&n.right, result);
            }
        }
        
        let mut result = Vec::with_capacity(self.len);
        traverse(&self.root, &mut result);
        result
    }
    
    fn len(&self) -> usize {
        self.len
    }
    
    fn height(&self) -> usize {
        fn calc_height<T>(node: &Option<Box<BstNode<T>>>) -> usize {
            match node {
                None => 0,
                Some(n) => {
                    let left_h = calc_height(&n.left);
                    let right_h = calc_height(&n.right);
                    1 + left_h.max(right_h)
                }
            }
        }
        
        calc_height(&self.root)
    }
}

impl<T: Ord + fmt::Display> fmt::Display for Bst<T> {
    fn fmt(&self, f: &mut fmt::Formatter<'_>) -> fmt::Result {
        let values = self.in_order();
        write!(f, "[")?;
        for (i, val) in values.iter().enumerate() {
            if i > 0 {
                write!(f, ", ")?;
            }
            write!(f, "{}", val)?;
        }
        write!(f, "]")
    }
}

fn main() {
    let mut tree = Bst::new();
    let values = [5, 3, 7, 1, 4, 6, 8, 2];
    
    for &v in &values {
        tree.insert(v);
    }
    
    println!("Tree (in-order): {}", tree);
    println!("Contains 4: {}", tree.contains(&4));
    println!("Contains 9: {}", tree.contains(&9));
    println!("Height: {}", tree.height());
    println!("Size: {}", tree.len());
}
```

Notice the use of `Box<BstNode<T>>`. `Box` is Rust's heap-allocation pointer. Without it, each `BstNode` would need to contain two more `BstNode`s by value, which would require infinite memory. `Box` puts the child nodes on the heap and stores a pointer to them. `Option<Box<BstNode<T>>>` means "either a pointer to a child node, or nothing" — this is how we represent the absence of a child.

---

## Part 7 — Algorithms: Sorting, Searching, and Graph Traversal

### Sorting Algorithms

Let us implement several sorting algorithms from scratch to understand their performance characteristics.

#### Bubble Sort (The Bad One)

```rust
fn bubble_sort<T: Ord>(arr: &mut [T]) {
    let n = arr.len();
    for i in 0..n {
        let mut swapped = false;
        for j in 0..n - 1 - i {
            if arr[j] > arr[j + 1] {
                arr.swap(j, j + 1);
                swapped = true;
            }
        }
        if !swapped {
            break; // Already sorted
        }
    }
}
```

**Time complexity:** O(n^2) average and worst case, O(n) best case (already sorted). **Space:** O(1).

**Why it is bad:** Bubble sort makes O(n^2) comparisons and swaps in the average case. For a million-element array, that is a trillion operations. This is unusable for anything beyond toy datasets.

#### Insertion Sort (Good for Small Arrays)

```rust
fn insertion_sort<T: Ord>(arr: &mut [T]) {
    for i in 1..arr.len() {
        let mut j = i;
        while j > 0 && arr[j - 1] > arr[j] {
            arr.swap(j - 1, j);
            j -= 1;
        }
    }
}
```

**Time complexity:** O(n^2) average, O(n) best case (nearly sorted). **Space:** O(1).

Insertion sort is actually useful for small arrays (say, fewer than 20 elements) because it has very low overhead — no recursion, no extra memory. The standard library's sort implementation often switches to insertion sort for small sub-arrays.

#### Merge Sort (The Reliable One)

```rust
fn merge_sort<T: Ord + Clone>(arr: &mut [T]) {
    let len = arr.len();
    if len <= 1 {
        return;
    }
    
    let mid = len / 2;
    let mut left = arr[..mid].to_vec();
    let mut right = arr[mid..].to_vec();
    
    merge_sort(&mut left);
    merge_sort(&mut right);
    
    merge(&left, &right, arr);
}

fn merge<T: Ord + Clone>(left: &[T], right: &[T], result: &mut [T]) {
    let mut i = 0;
    let mut j = 0;
    let mut k = 0;
    
    while i < left.len() && j < right.len() {
        if left[i] <= right[j] {
            result[k] = left[i].clone();
            i += 1;
        } else {
            result[k] = right[j].clone();
            j += 1;
        }
        k += 1;
    }
    
    while i < left.len() {
        result[k] = left[i].clone();
        i += 1;
        k += 1;
    }
    
    while j < right.len() {
        result[k] = right[j].clone();
        j += 1;
        k += 1;
    }
}
```

**Time complexity:** O(n log n) in all cases. **Space:** O(n).

Merge sort is guaranteed O(n log n) regardless of input. The downside is that it requires O(n) extra memory for the temporary arrays.

#### Quick Sort (The Fast One)

```rust
fn quick_sort<T: Ord>(arr: &mut [T]) {
    let len = arr.len();
    if len <= 1 {
        return;
    }
    
    let pivot_index = partition(arr);
    
    quick_sort(&mut arr[..pivot_index]);
    if pivot_index + 1 < len {
        quick_sort(&mut arr[pivot_index + 1..]);
    }
}

fn partition<T: Ord>(arr: &mut [T]) -> usize {
    let len = arr.len();
    let pivot_index = len - 1;
    let mut store_index = 0;
    
    for i in 0..pivot_index {
        if arr[i] <= arr[pivot_index] {
            arr.swap(i, store_index);
            store_index += 1;
        }
    }
    
    arr.swap(store_index, pivot_index);
    store_index
}

fn main() {
    let mut data = vec![38, 27, 43, 3, 9, 82, 10];
    
    println!("Before: {:?}", data);
    quick_sort(&mut data);
    println!("After:  {:?}", data);
}
```

**Time complexity:** O(n log n) average, O(n^2) worst case (already sorted with bad pivot). **Space:** O(log n) for the recursion stack.

Quick sort is usually the fastest sorting algorithm in practice because it has excellent cache locality — it works on contiguous memory and rarely causes cache misses.

#### Using the Standard Library's Sort

In practice, you should almost always use the standard library:

```rust
fn main() {
    let mut data = vec![38, 27, 43, 3, 9, 82, 10];
    
    // Stable sort (preserves order of equal elements)
    data.sort();
    println!("{:?}", data);
    
    // Unstable sort (may reorder equal elements, but faster)
    data.sort_unstable();
    
    // Sort by a key
    let mut words = vec!["banana", "apple", "cherry"];
    words.sort_by_key(|w| w.len());
    println!("{:?}", words); // ["apple", "banana", "cherry"]
    
    // Custom comparator
    data.sort_by(|a, b| b.cmp(a)); // Descending order
    println!("{:?}", data);
}
```

The standard library's `sort()` uses a pattern-defeating quicksort (pdqsort) variant that is O(n log n) and adaptive — it detects already-sorted runs and exploits them. You should use this unless you have a very specific reason not to.

### Binary Search

```rust
fn binary_search<T: Ord>(arr: &[T], target: &T) -> Option<usize> {
    let mut low = 0;
    let mut high = arr.len();
    
    while low < high {
        let mid = low + (high - low) / 2; // Avoids overflow
        match arr[mid].cmp(target) {
            std::cmp::Ordering::Equal => return Some(mid),
            std::cmp::Ordering::Less => low = mid + 1,
            std::cmp::Ordering::Greater => high = mid,
        }
    }
    
    None
}

fn main() {
    let data = vec![1, 3, 5, 7, 9, 11, 13, 15];
    
    println!("Found 7 at: {:?}", binary_search(&data, &7));   // Some(3)
    println!("Found 8 at: {:?}", binary_search(&data, &8));   // None
    
    // Standard library version:
    println!("Std: {:?}", data.binary_search(&7)); // Ok(3)
    println!("Std: {:?}", data.binary_search(&8)); // Err(4) — insertion point
}
```

Note the subtle but important difference: `low + (high - low) / 2` instead of `(low + high) / 2`. The latter can overflow if `low + high` exceeds the maximum value of `usize`. This is a classic bug in binary search implementations.

### Graph Traversal: BFS and DFS

Let us implement a graph using an adjacency list and perform both breadth-first search and depth-first search:

```rust
use std::collections::{HashMap, HashSet, VecDeque};

struct Graph {
    adjacency: HashMap<usize, Vec<usize>>,
}

impl Graph {
    fn new() -> Self {
        Graph {
            adjacency: HashMap::new(),
        }
    }
    
    fn add_edge(&mut self, from: usize, to: usize) {
        self.adjacency.entry(from).or_insert_with(Vec::new).push(to);
        self.adjacency.entry(to).or_insert_with(Vec::new).push(from);
    }
    
    fn bfs(&self, start: usize) -> Vec<usize> {
        let mut visited = HashSet::new();
        let mut queue = VecDeque::new();
        let mut order = Vec::new();
        
        visited.insert(start);
        queue.push_back(start);
        
        while let Some(node) = queue.pop_front() {
            order.push(node);
            
            if let Some(neighbors) = self.adjacency.get(&node) {
                for &neighbor in neighbors {
                    if visited.insert(neighbor) {
                        queue.push_back(neighbor);
                    }
                }
            }
        }
        
        order
    }
    
    fn dfs(&self, start: usize) -> Vec<usize> {
        let mut visited = HashSet::new();
        let mut stack = vec![start];
        let mut order = Vec::new();
        
        while let Some(node) = stack.pop() {
            if !visited.insert(node) {
                continue;
            }
            
            order.push(node);
            
            if let Some(neighbors) = self.adjacency.get(&node) {
                for &neighbor in neighbors.iter().rev() {
                    if !visited.contains(&neighbor) {
                        stack.push(neighbor);
                    }
                }
            }
        }
        
        order
    }
    
    fn shortest_path(&self, start: usize, end: usize) -> Option<Vec<usize>> {
        let mut visited = HashSet::new();
        let mut queue = VecDeque::new();
        let mut parent: HashMap<usize, usize> = HashMap::new();
        
        visited.insert(start);
        queue.push_back(start);
        
        while let Some(node) = queue.pop_front() {
            if node == end {
                // Reconstruct path
                let mut path = vec![end];
                let mut current = end;
                while current != start {
                    current = *parent.get(&current).unwrap();
                    path.push(current);
                }
                path.reverse();
                return Some(path);
            }
            
            if let Some(neighbors) = self.adjacency.get(&node) {
                for &neighbor in neighbors {
                    if visited.insert(neighbor) {
                        parent.insert(neighbor, node);
                        queue.push_back(neighbor);
                    }
                }
            }
        }
        
        None
    }
}

fn main() {
    let mut graph = Graph::new();
    graph.add_edge(0, 1);
    graph.add_edge(0, 2);
    graph.add_edge(1, 3);
    graph.add_edge(2, 4);
    graph.add_edge(3, 4);
    graph.add_edge(3, 5);
    graph.add_edge(4, 5);
    
    println!("BFS from 0: {:?}", graph.bfs(0));
    println!("DFS from 0: {:?}", graph.dfs(0));
    println!("Shortest path 0->5: {:?}", graph.shortest_path(0, 5));
}
```

---

## Part 8 — Error Handling Done Right

We covered `Result` and `Option` briefly earlier. Now let us go deep on error handling, because this is where Rust's approach is vastly different from C#'s exception-based model.

### The Problem with Exceptions

In C#, when something goes wrong, you throw an exception:

```csharp
// C# — exception-based error handling
public int ParseConfig(string path)
{
    string content = File.ReadAllText(path); // Can throw IOException
    return int.Parse(content);                // Can throw FormatException
}
```

There are three problems with this:

1. **The function signature lies.** It says it returns `int`, but it might actually throw two different exceptions. You have to read the documentation (or the source code) to know this.

2. **You can silently ignore errors.** If you do not write a try-catch, the exception propagates up the call stack. You might not even know a function can fail until it fails in production.

3. **Throwing exceptions is expensive.** Capturing a stack trace involves walking the entire call stack and allocating strings. This is orders of magnitude slower than returning an error value.

### Rust's Approach

In Rust, the equivalent code makes error handling explicit:

```rust
use std::fs;
use std::io;
use std::num::ParseIntError;

#[derive(Debug)]
enum ConfigError {
    IoError(io::Error),
    ParseError(ParseIntError),
}

impl From<io::Error> for ConfigError {
    fn from(err: io::Error) -> Self {
        ConfigError::IoError(err)
    }
}

impl From<ParseIntError> for ConfigError {
    fn from(err: ParseIntError) -> Self {
        ConfigError::ParseError(err)
    }
}

fn parse_config(path: &str) -> Result<i32, ConfigError> {
    let content = fs::read_to_string(path)?;
    let value = content.trim().parse::<i32>()?;
    Ok(value)
}

fn main() {
    match parse_config("config.txt") {
        Ok(value) => println!("Config value: {}", value),
        Err(ConfigError::IoError(e)) => eprintln!("Could not read file: {}", e),
        Err(ConfigError::ParseError(e)) => eprintln!("Could not parse value: {}", e),
    }
}
```

Every possible failure is visible in the function signature. The `?` operator provides the same ergonomics as exceptions (errors propagate automatically) but without the hidden control flow. You always know exactly where errors can occur and what kinds of errors they are.

### Custom Error Types With Display

For production code, you want your error types to implement `Display` so they can be printed nicely:

```rust
use std::fmt;
use std::io;
use std::num::ParseIntError;

#[derive(Debug)]
enum AppError {
    Io(io::Error),
    Parse(ParseIntError),
    Validation(String),
}

impl fmt::Display for AppError {
    fn fmt(&self, f: &mut fmt::Formatter<'_>) -> fmt::Result {
        match self {
            AppError::Io(err) => write!(f, "I/O error: {}", err),
            AppError::Parse(err) => write!(f, "Parse error: {}", err),
            AppError::Validation(msg) => write!(f, "Validation error: {}", msg),
        }
    }
}

impl std::error::Error for AppError {
    fn source(&self) -> Option<&(dyn std::error::Error + 'static)> {
        match self {
            AppError::Io(err) => Some(err),
            AppError::Parse(err) => Some(err),
            AppError::Validation(_) => None,
        }
    }
}

impl From<io::Error> for AppError {
    fn from(err: io::Error) -> Self {
        AppError::Io(err)
    }
}

impl From<ParseIntError> for AppError {
    fn from(err: ParseIntError) -> Self {
        AppError::Parse(err)
    }
}
```

This is more boilerplate than C# exceptions, but it is also more explicit, more type-safe, and more performant. The trade-off is intentional.

---

## Part 9 — Concurrency: Fearless Parallelism

Concurrency is where Rust's ownership system truly shines. The same rules that prevent use-after-free and double-free bugs also prevent data races — at compile time.

### Threads

```rust
use std::thread;

fn main() {
    let handles: Vec<_> = (0..5)
        .map(|i| {
            thread::spawn(move || {
                println!("Thread {} says hello!", i);
                i * i
            })
        })
        .collect();
    
    let results: Vec<_> = handles
        .into_iter()
        .map(|h| h.join().unwrap())
        .collect();
    
    println!("Results: {:?}", results);
}
```

The `move` keyword transfers ownership of `i` into the thread's closure. Without it, the compiler would reject the code because `i` is borrowed by a thread that might outlive the current function.

### The Data Race Prevention Guarantee

This code will not compile:

```rust
use std::thread;

fn main() {
    let mut data = vec![1, 2, 3];
    
    thread::spawn(|| {
        data.push(4); // ERROR: closure may outlive the current function
    });
    
    data.push(5); // Two threads writing to the same data!
}
```

The compiler prevents this because `data` would be simultaneously accessed from two threads (the main thread and the spawned thread), with at least one of them writing. This is a data race, and Rust refuses to compile it.

### Shared State with Mutex

To share mutable state between threads, you need a `Mutex` (mutual exclusion lock):

```rust
use std::sync::{Arc, Mutex};
use std::thread;

fn main() {
    let counter = Arc::new(Mutex::new(0));
    let mut handles = vec![];
    
    for _ in 0..10 {
        let counter = Arc::clone(&counter);
        let handle = thread::spawn(move || {
            let mut num = counter.lock().unwrap();
            *num += 1;
        });
        handles.push(handle);
    }
    
    for handle in handles {
        handle.join().unwrap();
    }
    
    println!("Result: {}", *counter.lock().unwrap()); // 10
}
```

`Arc` (Atomic Reference Counted) is a thread-safe reference-counted pointer. It allows multiple threads to own the same data. `Mutex` ensures that only one thread can access the data at a time.

The beauty of Rust's system is that you cannot accidentally forget to lock the mutex. The data inside the mutex is only accessible through the lock guard returned by `.lock()`. When the guard goes out of scope, the lock is automatically released. There is no way to access the data without holding the lock.

### Channels: Message Passing

Channels provide thread-safe communication:

```rust
use std::sync::mpsc;
use std::thread;

fn main() {
    let (tx, rx) = mpsc::channel();
    
    // Spawn multiple sender threads
    for i in 0..5 {
        let tx = tx.clone();
        thread::spawn(move || {
            let message = format!("Message from thread {}", i);
            tx.send(message).unwrap();
        });
    }
    
    drop(tx); // Drop the original sender
    
    // Receive all messages
    for received in rx {
        println!("Got: {}", received);
    }
}
```

`mpsc` stands for Multiple Producer, Single Consumer. Multiple threads can send messages through cloned transmitters, and a single receiver reads them.

---

## Part 10 — Iterators and Functional Programming

Rust's iterator system is one of its most powerful features. Iterators are lazy — they do not compute results until you consume them — and they are optimized away by the compiler into code that is as fast as hand-written loops.

### Iterator Basics

```rust
fn main() {
    let numbers = vec![1, 2, 3, 4, 5, 6, 7, 8, 9, 10];
    
    // Map: transform each element
    let doubled: Vec<i32> = numbers.iter().map(|&x| x * 2).collect();
    println!("Doubled: {:?}", doubled);
    
    // Filter: keep elements matching a condition
    let evens: Vec<&i32> = numbers.iter().filter(|&&x| x % 2 == 0).collect();
    println!("Evens: {:?}", evens);
    
    // Fold (reduce): accumulate into a single value
    let sum: i32 = numbers.iter().sum();
    println!("Sum: {}", sum);
    
    let product: i32 = numbers.iter().fold(1, |acc, &x| acc * x);
    println!("Product: {}", product);
    
    // Chaining operations
    let result: i32 = numbers
        .iter()
        .filter(|&&x| x % 2 == 0)
        .map(|&x| x * x)
        .sum();
    println!("Sum of squares of evens: {}", result); // 4+16+36+64+100 = 220
    
    // Enumerate: get index with element
    for (i, val) in numbers.iter().enumerate() {
        println!("[{}] = {}", i, val);
    }
    
    // Zip: combine two iterators
    let names = vec!["Alice", "Bob", "Charlie"];
    let ages = vec![30, 25, 35];
    let people: Vec<_> = names.iter().zip(ages.iter()).collect();
    println!("People: {:?}", people);
    
    // Take and skip
    let first_three: Vec<&i32> = numbers.iter().take(3).collect();
    let after_three: Vec<&i32> = numbers.iter().skip(3).collect();
    println!("First 3: {:?}", first_three);
    println!("After 3: {:?}", after_three);
    
    // Any and all
    let has_even = numbers.iter().any(|&x| x % 2 == 0);
    let all_positive = numbers.iter().all(|&x| x > 0);
    println!("Has even: {}, All positive: {}", has_even, all_positive);
    
    // Find
    let first_even = numbers.iter().find(|&&x| x % 2 == 0);
    println!("First even: {:?}", first_even);
    
    // Position
    let pos = numbers.iter().position(|&x| x == 5);
    println!("Position of 5: {:?}", pos);
    
    // Max, min
    println!("Max: {:?}", numbers.iter().max());
    println!("Min: {:?}", numbers.iter().min());
    
    // Flat map
    let nested = vec![vec![1, 2], vec![3, 4], vec![5]];
    let flat: Vec<&i32> = nested.iter().flat_map(|v| v.iter()).collect();
    println!("Flat: {:?}", flat);
}
```

### Writing Your Own Iterator

```rust
struct Fibonacci {
    a: u64,
    b: u64,
}

impl Fibonacci {
    fn new() -> Self {
        Fibonacci { a: 0, b: 1 }
    }
}

impl Iterator for Fibonacci {
    type Item = u64;
    
    fn next(&mut self) -> Option<Self::Item> {
        let result = self.a;
        let next = self.a.checked_add(self.b)?; // Returns None on overflow
        self.a = self.b;
        self.b = next;
        Some(result)
    }
}

fn main() {
    // First 20 Fibonacci numbers
    let fibs: Vec<u64> = Fibonacci::new().take(20).collect();
    println!("{:?}", fibs);
    
    // Sum of Fibonacci numbers below 1000
    let sum: u64 = Fibonacci::new().take_while(|&x| x < 1000).sum();
    println!("Sum of fibs below 1000: {}", sum);
    
    // First Fibonacci number above a million
    let big = Fibonacci::new().find(|&x| x > 1_000_000);
    println!("First fib > 1M: {:?}", big);
}
```

### Zero-Cost Abstraction

The term "zero-cost abstraction" means that using iterators compiles down to the same machine code as hand-written loops. The compiler inlines all the iterator adapter methods and optimizes away all the intermediate structures.

This iterator chain:

```rust
let sum: i32 = (0..1000)
    .filter(|x| x % 2 == 0)
    .map(|x| x * x)
    .sum();
```

Compiles to essentially the same code as:

```rust
let mut sum: i32 = 0;
let mut i = 0;
while i < 1000 {
    if i % 2 == 0 {
        sum += i * i;
    }
    i += 1;
}
```

There is no allocation of intermediate vectors, no virtual dispatch, no garbage collection. The compiler's optimizer (LLVM) sees through the abstraction and generates tight, efficient machine code.

---

## Part 11 — Pattern Matching in Depth

Pattern matching in Rust is far more powerful than `switch` statements in C# or most other languages. You have already seen `match` with enums. Let us explore its full power.

```rust
fn main() {
    // Matching on numbers
    let x = 42;
    match x {
        0 => println!("zero"),
        1..=10 => println!("one to ten"),
        11 | 13 | 17 | 19 => println!("teen prime"),
        20..=99 => println!("twenty to ninety-nine"),
        _ => println!("something else"),
    }
    
    // Destructuring tuples
    let point = (3, -5);
    match point {
        (0, 0) => println!("origin"),
        (x, 0) => println!("on x-axis at {}", x),
        (0, y) => println!("on y-axis at {}", y),
        (x, y) if x > 0 && y > 0 => println!("first quadrant ({}, {})", x, y),
        (x, y) => println!("elsewhere ({}, {})", x, y),
    }
    
    // Destructuring structs
    struct Color { r: u8, g: u8, b: u8 }
    
    let c = Color { r: 255, g: 0, b: 128 };
    match c {
        Color { r: 255, g: 0, b: 0 } => println!("pure red"),
        Color { r: 0, g: 255, b: 0 } => println!("pure green"),
        Color { r: 0, g: 0, b: 255 } => println!("pure blue"),
        Color { r, g: 0, b: 0 } => println!("red shade: {}", r),
        Color { r, g, b } => println!("rgb({}, {}, {})", r, g, b),
    }
    
    // Matching on references
    let values = vec![1, 2, 3];
    match values.as_slice() {
        [] => println!("empty"),
        [single] => println!("one element: {}", single),
        [first, second] => println!("two elements: {}, {}", first, second),
        [first, .., last] => println!("first: {}, last: {}", first, last),
    }
    
    // if let — when you only care about one pattern
    let some_value: Option<i32> = Some(42);
    if let Some(v) = some_value {
        println!("Got value: {}", v);
    }
    
    // while let — loop while pattern matches
    let mut stack = vec![1, 2, 3];
    while let Some(top) = stack.pop() {
        println!("Popped: {}", top);
    }
    
    // let-else — require a pattern or diverge
    let value: Option<i32> = Some(42);
    let Some(v) = value else {
        println!("No value!");
        return;
    };
    println!("Value is {}", v);
}
```

### Exhaustive Matching

The compiler requires that `match` expressions cover all possible cases:

```rust
enum Command {
    Quit,
    Echo(String),
    Move { x: i32, y: i32 },
    ChangeColor(u8, u8, u8),
}

fn execute(cmd: Command) {
    match cmd {
        Command::Quit => println!("Quitting"),
        Command::Echo(msg) => println!("Echo: {}", msg),
        Command::Move { x, y } => println!("Moving to ({}, {})", x, y),
        Command::ChangeColor(r, g, b) => println!("Color: ({}, {}, {})", r, g, b),
    }
    // If you forget any variant, the compiler will tell you!
}
```

If someone adds a new variant to `Command`, every `match` expression in the codebase that does not handle it will produce a compile error. This is enormously valuable for maintenance.

---

## Part 12 — Closures and Higher-Order Functions

Closures are anonymous functions that can capture variables from their surrounding scope. They are how Rust does functional programming.

```rust
fn main() {
    // Basic closure
    let add_one = |x: i32| -> i32 { x + 1 };
    println!("{}", add_one(5)); // 6
    
    // Type inference works for closures
    let double = |x| x * 2;
    println!("{}", double(5)); // 10
    
    // Closures can capture variables
    let name = String::from("Alice");
    let greet = || println!("Hello, {}!", name);
    greet();
    
    // Mutable capture
    let mut count = 0;
    let mut increment = || {
        count += 1;
        count
    };
    println!("{}", increment()); // 1
    println!("{}", increment()); // 2
    println!("{}", increment()); // 3
    
    // Move capture (takes ownership)
    let data = vec![1, 2, 3];
    let owns_data = move || {
        println!("Data: {:?}", data);
    };
    owns_data();
    // println!("{:?}", data); // ERROR: data has been moved into the closure
}
```

### Higher-Order Functions

Functions that take other functions as arguments or return functions:

```rust
fn apply_twice<F: Fn(i32) -> i32>(f: F, x: i32) -> i32 {
    f(f(x))
}

fn make_adder(n: i32) -> impl Fn(i32) -> i32 {
    move |x| x + n
}

fn main() {
    let result = apply_twice(|x| x + 3, 7);
    println!("apply_twice: {}", result); // 13
    
    let add_five = make_adder(5);
    println!("add_five(10) = {}", add_five(10)); // 15
}
```

The three `Fn` traits:
- `Fn` — borrows captured variables immutably (can be called multiple times)
- `FnMut` — borrows captured variables mutably (can be called multiple times)
- `FnOnce` — takes ownership of captured variables (can only be called once)

---

## Part 13 — Traits in Depth: Implementing Standard Library Protocols

Rust's standard library defines traits that act as protocols. When your type implements these traits, it integrates smoothly with the language and the standard library.

### Display — Human-Readable Formatting

```rust
use std::fmt;

struct Matrix {
    data: Vec<Vec<f64>>,
    rows: usize,
    cols: usize,
}

impl Matrix {
    fn new(rows: usize, cols: usize) -> Self {
        Matrix {
            data: vec![vec![0.0; cols]; rows],
            rows,
            cols,
        }
    }
    
    fn set(&mut self, row: usize, col: usize, val: f64) {
        self.data[row][col] = val;
    }
}

impl fmt::Display for Matrix {
    fn fmt(&self, f: &mut fmt::Formatter<'_>) -> fmt::Result {
        for (i, row) in self.data.iter().enumerate() {
            if i > 0 {
                writeln!(f)?;
            }
            write!(f, "[")?;
            for (j, val) in row.iter().enumerate() {
                if j > 0 {
                    write!(f, ", ")?;
                }
                write!(f, "{:.2}", val)?;
            }
            write!(f, "]")?;
        }
        Ok(())
    }
}

fn main() {
    let mut m = Matrix::new(3, 3);
    m.set(0, 0, 1.0);
    m.set(1, 1, 1.0);
    m.set(2, 2, 1.0);
    println!("{}", m);
}
```

### Iterator — Making Types Iterable

```rust
struct Range {
    current: i64,
    end: i64,
    step: i64,
}

impl Range {
    fn new(start: i64, end: i64, step: i64) -> Self {
        Range { current: start, end, step }
    }
}

impl Iterator for Range {
    type Item = i64;
    
    fn next(&mut self) -> Option<Self::Item> {
        if (self.step > 0 && self.current >= self.end) ||
           (self.step < 0 && self.current <= self.end) {
            return None;
        }
        let val = self.current;
        self.current += self.step;
        Some(val)
    }
}

fn main() {
    // Count by 3s from 0 to 20
    let by_threes: Vec<i64> = Range::new(0, 20, 3).collect();
    println!("{:?}", by_threes); // [0, 3, 6, 9, 12, 15, 18]
    
    // Count down
    let countdown: Vec<i64> = Range::new(10, 0, -1).collect();
    println!("{:?}", countdown); // [10, 9, 8, 7, 6, 5, 4, 3, 2, 1]
}
```

### FromStr — Parsing from Strings

```rust
use std::num::ParseFloatError;
use std::str::FromStr;

struct Coordinate {
    lat: f64,
    lon: f64,
}

impl FromStr for Coordinate {
    type Err = String;
    
    fn from_str(s: &str) -> Result<Self, Self::Err> {
        let parts: Vec<&str> = s.split(',').collect();
        if parts.len() != 2 {
            return Err(format!("Expected 'lat,lon', got '{}'", s));
        }
        
        let lat = parts[0].trim().parse::<f64>()
            .map_err(|e: ParseFloatError| format!("Invalid latitude: {}", e))?;
        let lon = parts[1].trim().parse::<f64>()
            .map_err(|e: ParseFloatError| format!("Invalid longitude: {}", e))?;
        
        if lat < -90.0 || lat > 90.0 {
            return Err(format!("Latitude {} out of range [-90, 90]", lat));
        }
        if lon < -180.0 || lon > 180.0 {
            return Err(format!("Longitude {} out of range [-180, 180]", lon));
        }
        
        Ok(Coordinate { lat, lon })
    }
}

fn main() {
    let coord: Coordinate = "40.7128, -74.0060".parse().unwrap();
    println!("NYC: ({}, {})", coord.lat, coord.lon);
    
    let result: Result<Coordinate, _> = "not a coordinate".parse();
    println!("Error: {:?}", result.err());
}
```

---

## Part 14 — File I/O and Working With the Standard Library

Everything in this section uses only the standard library. No crates.

### Reading and Writing Files

```rust
use std::fs;
use std::io::{self, BufRead, Write, BufWriter};
use std::path::Path;

fn main() -> Result<(), Box<dyn std::error::Error>> {
    // Write a file
    fs::write("output.txt", "Hello, Rust!\nLine 2\nLine 3")?;
    
    // Read entire file into a string
    let contents = fs::read_to_string("output.txt")?;
    println!("Contents:\n{}", contents);
    
    // Read file line by line (memory efficient for large files)
    let file = fs::File::open("output.txt")?;
    let reader = io::BufReader::new(file);
    
    for (i, line) in reader.lines().enumerate() {
        let line = line?;
        println!("Line {}: {}", i + 1, line);
    }
    
    // Write with buffering (much faster for many small writes)
    let file = fs::File::create("buffered_output.txt")?;
    let mut writer = BufWriter::new(file);
    
    for i in 0..1000 {
        writeln!(writer, "Line {}: some data here", i)?;
    }
    writer.flush()?;
    
    // File metadata
    let metadata = fs::metadata("output.txt")?;
    println!("Size: {} bytes", metadata.len());
    println!("Is file: {}", metadata.is_file());
    
    // Directory operations
    fs::create_dir_all("temp/nested/dir")?;
    
    // List directory contents
    for entry in fs::read_dir(".")? {
        let entry = entry?;
        let name = entry.file_name();
        let file_type = entry.file_type()?;
        let kind = if file_type.is_dir() { "DIR" } else { "FILE" };
        println!("{} {:?}", kind, name);
    }
    
    // Cleanup
    fs::remove_file("output.txt")?;
    fs::remove_file("buffered_output.txt")?;
    fs::remove_dir_all("temp")?;
    
    Ok(())
}
```

### Working With Paths

```rust
use std::path::{Path, PathBuf};

fn main() {
    let path = Path::new("/home/user/documents/report.txt");
    
    println!("File name: {:?}", path.file_name());
    println!("Extension: {:?}", path.extension());
    println!("Parent: {:?}", path.parent());
    println!("Stem: {:?}", path.file_stem());
    println!("Is absolute: {}", path.is_absolute());
    
    // Building paths
    let mut full_path = PathBuf::from("/home/user");
    full_path.push("projects");
    full_path.push("my_app");
    full_path.push("src");
    full_path.set_extension("rs");
    println!("Built path: {:?}", full_path);
    
    // Path operations work cross-platform
    let joined = Path::new("/home/user").join("documents").join("file.txt");
    println!("Joined: {:?}", joined);
}
```

---

## Part 15 — Testing

Rust has a built-in testing framework. No external test runner required.

```rust
fn add(a: i32, b: i32) -> i32 {
    a + b
}

fn divide(a: f64, b: f64) -> Result<f64, String> {
    if b == 0.0 {
        Err(String::from("Division by zero"))
    } else {
        Ok(a / b)
    }
}

#[cfg(test)]
mod tests {
    use super::*;
    
    #[test]
    fn test_add() {
        assert_eq!(add(2, 3), 5);
    }
    
    #[test]
    fn test_add_negative() {
        assert_eq!(add(-1, -1), -2);
    }
    
    #[test]
    fn test_add_zero() {
        assert_eq!(add(0, 0), 0);
    }
    
    #[test]
    fn test_divide_normal() {
        let result = divide(10.0, 3.0).unwrap();
        assert!((result - 3.333).abs() < 0.01);
    }
    
    #[test]
    fn test_divide_by_zero() {
        assert!(divide(10.0, 0.0).is_err());
    }
    
    #[test]
    fn test_divide_error_message() {
        let err = divide(10.0, 0.0).unwrap_err();
        assert_eq!(err, "Division by zero");
    }
    
    #[test]
    #[should_panic(expected = "overflow")]
    fn test_overflow_panics() {
        let _ = (i32::MAX).checked_add(1).expect("overflow");
    }
}
```

Run tests with:

```bash
cargo test
```

### Documentation Tests

Rust can run code examples in documentation comments as tests:

```rust
/// Adds two numbers together.
///
/// # Examples
///
/// ```
/// let result = add(2, 3);
/// assert_eq!(result, 5);
/// ```
pub fn add(a: i32, b: i32) -> i32 {
    a + b
}
```

When you run `cargo test`, the code inside the `///` comment is compiled and executed as a test. This ensures your documentation examples are always correct.

---

## Part 16 — A Complete Project: Building a Key-Value Store

Let us put everything together and build a complete, working, in-memory key-value store with command-line interaction. No external crates. Standard library only.

```rust
use std::collections::HashMap;
use std::fs;
use std::io::{self, Write, BufRead, BufWriter};
use std::path::Path;
use std::time::{SystemTime, UNIX_EPOCH};

/// A simple in-memory key-value store with persistence.
struct KvStore {
    data: HashMap<String, String>,
    path: String,
    operation_count: u64,
}

impl KvStore {
    fn new(path: &str) -> Self {
        let mut store = KvStore {
            data: HashMap::new(),
            path: path.to_string(),
            operation_count: 0,
        };
        
        if Path::new(path).exists() {
            if let Err(e) = store.load() {
                eprintln!("Warning: Could not load data from {}: {}", path, e);
            }
        }
        
        store
    }
    
    fn get(&self, key: &str) -> Option<&String> {
        self.data.get(key)
    }
    
    fn set(&mut self, key: String, value: String) -> Option<String> {
        self.operation_count += 1;
        let old = self.data.insert(key, value);
        
        // Auto-save every 10 operations
        if self.operation_count % 10 == 0 {
            if let Err(e) = self.save() {
                eprintln!("Warning: Auto-save failed: {}", e);
            }
        }
        
        old
    }
    
    fn delete(&mut self, key: &str) -> Option<String> {
        self.operation_count += 1;
        self.data.remove(key)
    }
    
    fn keys(&self) -> Vec<&String> {
        let mut keys: Vec<&String> = self.data.keys().collect();
        keys.sort();
        keys
    }
    
    fn len(&self) -> usize {
        self.data.len()
    }
    
    fn search(&self, pattern: &str) -> Vec<(&String, &String)> {
        let pattern_lower = pattern.to_lowercase();
        let mut results: Vec<_> = self.data.iter()
            .filter(|(k, v)| {
                k.to_lowercase().contains(&pattern_lower) ||
                v.to_lowercase().contains(&pattern_lower)
            })
            .collect();
        results.sort_by_key(|(k, _)| k.to_string());
        results
    }
    
    fn save(&self) -> io::Result<()> {
        let file = fs::File::create(&self.path)?;
        let mut writer = BufWriter::new(file);
        
        for (key, value) in &self.data {
            // Simple format: key=value, one per line
            // Escape newlines in values
            let escaped_value = value.replace('\\', "\\\\").replace('\n', "\\n");
            writeln!(writer, "{}={}", key, escaped_value)?;
        }
        
        writer.flush()?;
        Ok(())
    }
    
    fn load(&mut self) -> io::Result<()> {
        let file = fs::File::open(&self.path)?;
        let reader = io::BufReader::new(file);
        
        for line in reader.lines() {
            let line = line?;
            if let Some((key, value)) = line.split_once('=') {
                let unescaped_value = value.replace("\\n", "\n").replace("\\\\", "\\");
                self.data.insert(key.to_string(), unescaped_value);
            }
        }
        
        Ok(())
    }
    
    fn stats(&self) -> String {
        let total_key_bytes: usize = self.data.keys().map(|k| k.len()).sum();
        let total_value_bytes: usize = self.data.values().map(|v| v.len()).sum();
        
        format!(
            "Entries: {}, Key bytes: {}, Value bytes: {}, Total bytes: {}, Operations: {}",
            self.len(),
            total_key_bytes,
            total_value_bytes,
            total_key_bytes + total_value_bytes,
            self.operation_count,
        )
    }
}

fn timestamp() -> u64 {
    SystemTime::now()
        .duration_since(UNIX_EPOCH)
        .unwrap_or_default()
        .as_secs()
}

fn main() {
    let mut store = KvStore::new("kvstore.dat");
    let stdin = io::stdin();
    
    println!("Simple Key-Value Store (type 'help' for commands)");
    println!("Loaded {} entries from disk.", store.len());
    
    loop {
        print!("> ");
        io::stdout().flush().unwrap();
        
        let mut input = String::new();
        if stdin.lock().read_line(&mut input).is_err() || input.is_empty() {
            break;
        }
        
        let input = input.trim();
        if input.is_empty() {
            continue;
        }
        
        let parts: Vec<&str> = input.splitn(3, ' ').collect();
        let command = parts[0].to_lowercase();
        
        match command.as_str() {
            "get" => {
                if parts.len() < 2 {
                    println!("Usage: get <key>");
                    continue;
                }
                match store.get(parts[1]) {
                    Some(value) => println!("{}", value),
                    None => println!("(not found)"),
                }
            }
            "set" => {
                if parts.len() < 3 {
                    println!("Usage: set <key> <value>");
                    continue;
                }
                let old = store.set(parts[1].to_string(), parts[2].to_string());
                match old {
                    Some(old_val) => println!("Updated (was: {})", old_val),
                    None => println!("OK"),
                }
            }
            "del" | "delete" => {
                if parts.len() < 2 {
                    println!("Usage: del <key>");
                    continue;
                }
                match store.delete(parts[1]) {
                    Some(val) => println!("Deleted: {}", val),
                    None => println!("(not found)"),
                }
            }
            "keys" => {
                let keys = store.keys();
                if keys.is_empty() {
                    println!("(empty)");
                } else {
                    for key in keys {
                        println!("  {}", key);
                    }
                }
            }
            "search" => {
                if parts.len() < 2 {
                    println!("Usage: search <pattern>");
                    continue;
                }
                let results = store.search(parts[1]);
                if results.is_empty() {
                    println!("No matches found.");
                } else {
                    for (key, value) in results {
                        println!("  {} = {}", key, value);
                    }
                }
            }
            "save" => {
                match store.save() {
                    Ok(()) => println!("Saved {} entries.", store.len()),
                    Err(e) => println!("Error saving: {}", e),
                }
            }
            "stats" => println!("{}", store.stats()),
            "help" => {
                println!("Commands:");
                println!("  get <key>          - Get a value");
                println!("  set <key> <value>  - Set a value");
                println!("  del <key>          - Delete a key");
                println!("  keys               - List all keys");
                println!("  search <pattern>   - Search keys and values");
                println!("  save               - Save to disk");
                println!("  stats              - Show statistics");
                println!("  quit               - Exit (auto-saves)");
            }
            "quit" | "exit" | "q" => {
                if let Err(e) = store.save() {
                    eprintln!("Warning: Could not save on exit: {}", e);
                }
                println!("Goodbye! Saved {} entries.", store.len());
                break;
            }
            _ => println!("Unknown command: '{}'. Type 'help' for commands.", command),
        }
    }
}

#[cfg(test)]
mod tests {
    use super::*;
    
    #[test]
    fn test_set_and_get() {
        let mut store = KvStore::new("/tmp/test_kvstore.dat");
        store.set("name".to_string(), "Alice".to_string());
        assert_eq!(store.get("name"), Some(&"Alice".to_string()));
    }
    
    #[test]
    fn test_overwrite() {
        let mut store = KvStore::new("/tmp/test_kvstore2.dat");
        store.set("key".to_string(), "old".to_string());
        let old = store.set("key".to_string(), "new".to_string());
        assert_eq!(old, Some("old".to_string()));
        assert_eq!(store.get("key"), Some(&"new".to_string()));
    }
    
    #[test]
    fn test_delete() {
        let mut store = KvStore::new("/tmp/test_kvstore3.dat");
        store.set("key".to_string(), "value".to_string());
        let deleted = store.delete("key");
        assert_eq!(deleted, Some("value".to_string()));
        assert_eq!(store.get("key"), None);
    }
    
    #[test]
    fn test_search() {
        let mut store = KvStore::new("/tmp/test_kvstore4.dat");
        store.set("color_red".to_string(), "FF0000".to_string());
        store.set("color_blue".to_string(), "0000FF".to_string());
        store.set("size".to_string(), "large".to_string());
        
        let results = store.search("color");
        assert_eq!(results.len(), 2);
    }
    
    #[test]
    fn test_get_nonexistent() {
        let store = KvStore::new("/tmp/test_kvstore5.dat");
        assert_eq!(store.get("nope"), None);
    }
}
```

This project demonstrates nearly every concept we have covered: structs, methods, ownership, borrowing, error handling with `Result`, HashMap, file I/O, iterators, pattern matching, closures, testing, and more.

---

## Part 17 — Performance: Why Rust Is Fast

Let us be precise about what makes Rust fast, because "fast" is a vague word.

### Zero-Cost Abstractions

We have mentioned this phrase several times. Here is what it means concretely: in Rust, high-level abstractions (iterators, generics, traits) compile down to the same machine code as low-level, hand-written code. There is no runtime overhead for using abstractions.

In C#, when you use LINQ:

```csharp
var result = numbers.Where(x => x % 2 == 0).Select(x => x * x).Sum();
```

Each `.Where()` and `.Select()` creates an iterator object on the heap. The lambda expressions are compiled into classes with a single method. There is virtual dispatch at each step. The garbage collector must eventually clean up the iterator objects.

In Rust, the equivalent:

```rust
let result: i32 = numbers.iter()
    .filter(|&&x| x % 2 == 0)
    .map(|&x| x * x)
    .sum();
```

Compiles to a single tight loop with no heap allocations, no virtual dispatch, and no garbage collection. The compiler monomorphizes the generic iterator adaptors and inlines everything.

### No Garbage Collector

Rust has no garbage collector. Memory is freed deterministically when variables go out of scope. This means:

1. **No GC pauses.** In C# or Java, the garbage collector periodically stops your program to identify and free unreachable objects. These pauses can range from microseconds to hundreds of milliseconds. In Rust, there are no pauses.

2. **Predictable latency.** Because there are no GC pauses, Rust programs have more predictable response times. This is critical for real-time systems, game engines, audio processing, and high-frequency trading.

3. **Lower memory usage.** A garbage collector needs to track every object and maintain metadata about reachability. Rust does not need any of this.

### Stack vs. Heap

In Rust, you have precise control over whether data lives on the stack or the heap:

```rust
fn main() {
    // Stack allocated: fixed size, very fast
    let x: i32 = 42;
    let arr: [i32; 5] = [1, 2, 3, 4, 5];
    let point: (f64, f64) = (1.0, 2.0);
    
    // Heap allocated: dynamic size, requires allocation
    let s: String = String::from("hello");
    let v: Vec<i32> = vec![1, 2, 3, 4, 5];
    let b: Box<i32> = Box::new(42);
}
```

Stack allocation is essentially free — it just moves the stack pointer. Heap allocation requires calling the system allocator, which involves acquiring a lock, searching for a free block, and updating metadata. In hot loops, avoiding unnecessary heap allocations can make a dramatic difference in performance.

### Inline Everything

Rust and LLVM are aggressive about inlining functions. Small functions are almost always inlined, eliminating function call overhead. The `#[inline]` and `#[inline(always)]` attributes give you further control:

```rust
#[inline]
fn add(a: i32, b: i32) -> i32 {
    a + b
}
```

### SIMD and Auto-Vectorization

The LLVM backend can automatically vectorize loops to use SIMD (Single Instruction, Multiple Data) instructions, processing multiple data elements in a single CPU instruction:

```rust
fn sum_array(data: &[f32]) -> f32 {
    data.iter().sum()
}
```

With optimization enabled (`cargo build --release`), LLVM may compile this to use AVX2 instructions that process 8 floats at a time.

---

## Part 18 — Unsafe Rust: When You Need to Break the Rules

Everything we have discussed so far is "safe Rust" — code where the compiler guarantees memory safety. But sometimes you need to do things the compiler cannot verify:

```rust
fn main() {
    // Raw pointer arithmetic
    let mut data = [1, 2, 3, 4, 5];
    let ptr = data.as_mut_ptr();
    
    unsafe {
        // Dereference a raw pointer
        *ptr.add(2) = 99;
    }
    
    println!("{:?}", data); // [1, 2, 99, 4, 5]
}
```

The `unsafe` keyword does not turn off the borrow checker. It enables five specific capabilities:

1. Dereference raw pointers
2. Call unsafe functions
3. Access or modify mutable static variables
4. Implement unsafe traits
5. Access fields of unions

The key principle is: `unsafe` blocks should be small, well-documented, and surrounded by a safe API. You use `unsafe` to build safe abstractions, not to bypass the type system willy-nilly.

The standard library itself uses `unsafe` extensively. `Vec`, `HashMap`, `String` — all of them use unsafe code internally, but they present a safe API to users. The unsafe code has been reviewed, tested, and formally verified in many cases.

**When to use `unsafe`:**
- Calling C functions via FFI (Foreign Function Interface)
- Implementing lock-free data structures
- Writing very low-level code that manages raw memory
- Performance-critical inner loops where the bounds checks are provably safe

**When NOT to use `unsafe`:**
- Because the borrow checker is annoying
- Because you think you are smarter than the compiler
- Because you want to use patterns from C or C++

---

## Part 19 — The Future of Rust: What Comes Next

Rust has a defined roadmap and governance process. The language uses editions (the latest being 2024) to introduce backward-incompatible changes every two to three years, while maintaining the promise that code compiled on Rust 1.0 continues to compile on every future release.

### Active Development Areas

**Async Rust improvements:** Async closures were stabilized in the 2024 edition. Future work includes improving async traits, async generators, and the overall ergonomics of async Rust.

**Compile time improvements:** This is the community's most-requested improvement. The Rust compiler is notoriously slow compared to compilers for C, Go, or even C#. The team is working on parallel compilation, incremental compilation improvements, and linker optimizations. Using `lld` (LLVM's linker) by default on x86_64 Linux (starting with Rust 1.90) was one such improvement.

**The Linux kernel:** With Rust now a core language in Linux, expect rapid expansion of Rust-written kernel drivers. The Nova GPU driver for NVIDIA, the Asahi GPU driver for Apple silicon, and Tyr for ARM Mali GPUs are all in active development.

**Formal specification:** One criticism of Rust is that it lacks a formal language specification (unlike C, which has the ISO standard). Work on a Rust specification is underway. The Ferrocene compiler by Ferrous Systems has already been qualified for safety-critical use under IEC 62278 and ISO 26262.

**WebAssembly:** Rust is one of the best languages for compiling to WebAssembly. The Rust-to-Wasm pipeline is mature, and Rust powers many high-performance web applications running in the browser.

### What This Means for You

If you are a web developer learning Rust, you are investing in a language that is going to be increasingly important for decades. The underlying infrastructure of the internet — operating systems, databases, web servers, runtimes — is being rewritten in Rust. Understanding Rust gives you a window into that world, and the concepts you learn (ownership, lifetimes, zero-cost abstractions) will make you a better programmer in every language you use.

---

## Part 20 — Resources

Here are the most important resources for continuing your Rust journey:

- **The Rust Programming Language (the book):** [https://doc.rust-lang.org/book/](https://doc.rust-lang.org/book/) — The official, comprehensive guide. Read it cover to cover.

- **Rust By Example:** [https://doc.rust-lang.org/rust-by-example/](https://doc.rust-lang.org/rust-by-example/) — Learn through annotated examples.

- **The Rust Standard Library documentation:** [https://doc.rust-lang.org/std/](https://doc.rust-lang.org/std/) — Reference for every type, trait, and function in the standard library.

- **Rustlings:** [https://github.com/rust-lang/rustlings](https://github.com/rust-lang/rustlings) — Small exercises to get you used to reading and writing Rust code.

- **The Rust Reference:** [https://doc.rust-lang.org/reference/](https://doc.rust-lang.org/reference/) — The definitive language reference.

- **Rust Playground:** [https://play.rust-lang.org/](https://play.rust-lang.org/) — Write and run Rust code in your browser.

- **This Week in Rust:** [https://this-week-in-rust.org/](https://this-week-in-rust.org/) — Weekly newsletter covering Rust news, blog posts, and crate releases.

- **The Rust Foundation:** [https://foundation.rust-lang.org/](https://foundation.rust-lang.org/) — The organization that stewards the Rust project.

- **Rust Edition Guide:** [https://doc.rust-lang.org/edition-guide/](https://doc.rust-lang.org/edition-guide/) — Explains the edition system and what changed in each edition.

- **Crates.io:** [https://crates.io/](https://crates.io/) — The Rust package registry. When you are ready to go beyond the standard library, this is where you find libraries.

This article has given you a foundation: the ownership model, the type system, the standard library's data structures, sorting and searching algorithms, concurrency primitives, error handling, file I/O, testing, and a complete working project. The next step is to write code. Lots of code. The borrow checker will fight you at first. That is normal. It gets easier with practice, and what it teaches you is worth every minute of struggle.

Welcome to Rust. The elevator software will be better for it.
