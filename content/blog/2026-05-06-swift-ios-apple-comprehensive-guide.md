---
title: "Swift, SwiftUI, and the Apple Ecosystem: A Comprehensive Guide from First Principles for the ASP.NET Developer Who Wants to Ship on iPhone"
date: 2026-05-06
author: myblazor-team
summary: "An exhaustive, from-the-ground-up guide to Swift, SwiftUI, Xcode, and the entire Apple development ecosystem — covering the language from variables to concurrency, SwiftUI from hello world to production architecture, app signing, provisioning profiles, TestFlight, App Store submission, data structures and algorithms in Swift, and a complete GitHub Actions pipeline for automated builds. Written for C# and ASP.NET developers who need to understand everything, not just follow a tutorial."
tags:
  - swift
  - ios
  - swiftui
  - apple
  - xcode
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

You write C# for a living. You understand classes, interfaces, generics, async/await, dependency injection, and LINQ. You deploy to IIS or Azure or a Linux container. You know how HTTP works. You know what a 500 error means. You have opinions about Entity Framework.

And now you need to build an iPhone app.

Maybe your company needs one. Maybe a client is demanding one. Maybe you just want to put something on a phone that you built yourself, with your own hands, from scratch. Whatever the reason, you have opened Apple's developer documentation, encountered the words "Swift," "SwiftUI," "Xcode," "provisioning profile," "code signing identity," "entitlements," and "App Store Connect," and you have concluded that Apple has constructed an elaborate hazing ritual disguised as a development platform.

You are not entirely wrong. But the hazing ritual produces apps that run on over a billion active devices, and once you understand the underlying logic — and there is logic, even if it is buried under layers of historical accident — the platform is powerful, coherent, and in many ways more pleasant to work with than anything in the .NET ecosystem.

This article will teach you everything. We will start with Swift the language, compare every concept to C# so you have a mental anchor, build real data structures and algorithms from scratch, learn SwiftUI's declarative UI model, understand Apple's application architecture, navigate the code signing and provisioning system, set up automated builds with GitHub Actions, and prepare an app for the App Store.

We will not skip steps. We will not summarize. We will not say "consult the documentation." We will explain everything.

The current stable version of Swift is **6.3**, released on **March 24, 2026**. The current stable version of Xcode is **26.4**, also released on **March 24, 2026**. Apple recently unified its version numbering across platforms, which is why Xcode jumped from 16.x to 26.x. iOS 18 is the current shipping OS, with iOS 26 announced for later this year.

Let us begin.

## Part 1: Swift — The Language That Replaced Objective-C

### Where Swift Came From

Chris Lattner started working on Swift in July 2010 while at Apple. Lattner had previously created LLVM (the compiler infrastructure that powers Clang, Rust's compiler, and dozens of other tools) and Clang (the C/C++/Objective-C compiler). He knew compiler design better than almost anyone alive, and he looked at Objective-C — Apple's primary language since the NeXT acquisition in 1997 — and saw a language with extraordinary capability but also extraordinary age.

Objective-C was created in the early 1980s by Brad Cox and Tom Love. It grafted Smalltalk-style message passing onto C. The result was a language that was both extremely powerful (dynamic dispatch, categories, protocols, a rich runtime) and extremely dangerous (C's manual memory management, null pointer dereferences, buffer overflows, no type safety for message passing). Objective-C syntax was also deeply alien to anyone coming from the C/C++/Java/C# tradition:

```objectivec
// Objective-C
NSString *greeting = @"Hello, World!";
NSArray *items = @[@"one", @"two", @"three"];
NSDictionary *person = @{@"name": @"Alice", @"age": @30};

[person objectForKey:@"name"];  // Message passing syntax
```

Swift replaced this with syntax that is immediately readable to any C#, Java, or TypeScript developer:

```swift
// Swift
let greeting = "Hello, World!"
let items = ["one", "two", "three"]
let person = ["name": "Alice", "age": 30]

person["name"]  // Subscript syntax
```

Swift was announced at WWDC 2014 and open-sourced in December 2015 under the Apache 2.0 license. It is now developed by a community governance process (Swift Evolution) with proposals reviewed by a core team. Apple remains the primary contributor, but Swift runs on Linux, Windows, and — as of Swift 6.3 — has an official Android SDK.

### Swift vs. C# — A Mental Map

Before we dive into syntax, here is the Rosetta Stone between Swift and C# concepts:

| Concept | Swift | C# |
|---|---|---|
| Immutable variable | `let x = 10` | `readonly` / `const` |
| Mutable variable | `var x = 10` | `var x = 10;` |
| String interpolation | `"Hello, \(name)"` | `$"Hello, {name}"` |
| Optionals / Nullables | `String?` | `string?` |
| Null coalescing | `x ?? "default"` | `x ?? "default"` |
| Optional chaining | `x?.count` | `x?.Length` |
| Force unwrap | `x!` | `x!` (null-forgiving) |
| Array | `[Int]` or `Array<Int>` | `int[]` or `List<int>` |
| Dictionary | `[String: Int]` | `Dictionary<string, int>` |
| Tuple | `(String, Int)` | `(string, int)` |
| Closure / Lambda | `{ (x: Int) -> Int in x * 2 }` | `(int x) => x * 2` |
| Protocol | `protocol Drawable` | `interface IDrawable` |
| Extension | `extension String { }` | `static class StringExtensions` |
| Struct (value type) | `struct Point { }` | `struct Point { }` |
| Class (reference type) | `class Vehicle { }` | `class Vehicle { }` |
| Enum with data | `enum Result { case success(Data) }` | `abstract record Result` |
| Generics | `func swap<T>(_ a: inout T, _ b: inout T)` | `void Swap<T>(ref T a, ref T b)` |
| Async/Await | `async` / `await` | `async` / `await` |
| Error handling | `throws` / `try` / `catch` | `throw` / `try` / `catch` |
| Package manager | Swift Package Manager | NuGet |
| Build tool | Xcode / `swift build` | MSBuild / `dotnet build` |

The two languages are remarkably similar in philosophy. Both are strongly typed with type inference. Both support value types and reference types. Both have generics, closures, protocol/interface-oriented design, and structured concurrency. The differences are in the details, and those details matter.

### Variables: let and var

```swift
let name = "Alice"   // Immutable — cannot be reassigned
var age = 30         // Mutable — can be reassigned

// name = "Bob"      // Compilation error: Cannot assign to value: 'name' is a 'let' constant
age = 31             // Fine
```

This is identical to Kotlin's `val` and `var`. The convention in Swift, as in Kotlin, is to use `let` by default and `var` only when mutation is necessary.

In C#, local immutability requires discipline — there is no `let` keyword for local variables. You use `var` for everything and rely on convention. Swift and Kotlin enforce it at the language level, which is objectively better for catching accidental mutations.

### Type Inference

Swift has excellent type inference:

```swift
let name = "Alice"          // Inferred as String
let age = 30                // Inferred as Int
let pi = 3.14159            // Inferred as Double
let items = [1, 2, 3]       // Inferred as [Int]
let scores = ["Alice": 95]  // Inferred as [String: Int]
```

You can always specify types explicitly:

```swift
let name: String = "Alice"
let age: Int = 30
```

This is the same as C#'s `var` inference, but Swift uses it more pervasively — including for function return types in many cases.

### Optionals — Swift's Billion Dollar Solution

Tony Hoare, the inventor of null references, famously called them his "billion dollar mistake." Every language since has grappled with how to handle the absence of a value. C# added nullable reference types in C# 8.0 as an opt-in compiler warning. Kotlin made null safety part of the type system.

Swift takes the most rigorous approach of all: there is no null. The concept does not exist. Instead, Swift has **optionals** — a generic enum that either contains a value or contains nothing:

```swift
var name: String = "Alice"
// name = nil  // Error: 'nil' cannot be assigned to type 'String'

var maybeName: String? = "Alice"
maybeName = nil  // Fine — String? means "String or nil"
```

Under the hood, `String?` is syntactic sugar for `Optional<String>`, which is defined as:

```swift
enum Optional<Wrapped> {
    case none
    case some(Wrapped)
}
```

This is an algebraic data type. A `String?` is either `.none` (nil) or `.some("Alice")`. The compiler enforces that you handle both cases before you can use the value.

To extract the value from an optional, you have several options:

**Optional binding with `if let`:**

```swift
if let name = maybeName {
    print("Hello, \(name)")  // name is String here, not String?
} else {
    print("No name")
}
```

**Optional binding with `guard let`:**

```swift
func greet(_ name: String?) {
    guard let name = name else {
        print("No name provided")
        return
    }
    // name is non-optional String from here to the end of the scope
    print("Hello, \(name)")
}
```

`guard let` is the idiomatic way to handle optionals in functions. It extracts the value and returns early if it is nil. This keeps the "happy path" un-indented.

**Nil coalescing with `??`:**

```swift
let displayName = maybeName ?? "Anonymous"
```

Identical to C#'s `??`.

**Optional chaining with `?.`:**

```swift
let length = maybeName?.count  // Int? — nil if maybeName is nil
```

Identical to C#'s `?.`.

**Force unwrapping with `!`:**

```swift
let length = maybeName!.count  // Crashes if maybeName is nil
```

Just like the `!!` operator in Kotlin, force unwrapping should be used sparingly and only when you have absolute certainty the value is not nil. Every `!` is a potential crash.

### Functions

```swift
func add(_ a: Int, _ b: Int) -> Int {
    return a + b
}

// Or with implicit return for single-expression bodies:
func add(_ a: Int, _ b: Int) -> Int { a + b }
```

Swift function parameters have both an **argument label** (used at the call site) and a **parameter name** (used inside the function). The underscore `_` suppresses the argument label:

```swift
func greet(person name: String, with greeting: String = "Hello") {
    print("\(greeting), \(name)!")
}

greet(person: "Alice", with: "Good morning")  // "Good morning, Alice!"
greet(person: "Bob")                           // "Hello, Bob!"
```

This is one of Swift's most distinctive features. Argument labels make call sites read like English:

```swift
array.insert(42, at: 0)
string.replacingOccurrences(of: "foo", with: "bar")
view.padding(.horizontal, 16)
```

C# does not have argument labels as a separate concept — parameter names serve double duty. Swift's approach produces more readable API calls, which is why Apple's frameworks read so naturally.

### Closures

Swift closures are equivalent to C# lambdas:

```swift
let numbers = [5, 3, 8, 1, 9, 2]

// Full closure syntax
let sorted = numbers.sorted(by: { (a: Int, b: Int) -> Bool in
    return a < b
})

// Type inference
let sorted = numbers.sorted(by: { a, b in a < b })

// Trailing closure syntax
let sorted = numbers.sorted { a, b in a < b }

// Shorthand argument names
let sorted = numbers.sorted { $0 < $1 }

// Operator as closure
let sorted = numbers.sorted(by: <)
```

All five versions produce the same result. The trailing closure syntax — where you can write the closure outside the parentheses if it is the last parameter — is used extensively in SwiftUI:

```swift
VStack {
    Text("Hello")
    Text("World")
}
```

Here, the `{ }` block is a trailing closure passed to `VStack`'s initializer.

### Structs vs. Classes

This is a critical distinction in Swift, and it differs meaningfully from C#.

In C#, most types are classes (reference types). Structs are used occasionally for small value types (like `DateTime`, `Point`, custom value objects), but the default is `class`.

In Swift, the convention is reversed: **structs are the default**. You use a class only when you need reference semantics (shared mutable state, inheritance, or identity).

```swift
// Struct — value type, copied on assignment
struct Point {
    var x: Double
    var y: Double
}

var p1 = Point(x: 1, y: 2)
var p2 = p1       // p2 is a COPY
p2.x = 10
print(p1.x)       // 1 — p1 is unchanged
print(p2.x)       // 10

// Class — reference type, shared on assignment
class Person {
    var name: String
    init(name: String) { self.name = name }
}

let alice = Person(name: "Alice")
let alsoAlice = alice    // alsoAlice points to the SAME object
alsoAlice.name = "Bob"
print(alice.name)        // "Bob" — both references see the change
```

Why does Swift prefer structs? Because value types are:
- **Thread-safe by default** — no shared mutable state means no data races
- **Predictable** — copying eliminates spooky action at a distance
- **Efficient** — the compiler can often optimize away the copy (copy-on-write for collections)

SwiftUI builds its entire architecture on structs. Every view is a struct. Every modifier returns a new struct. This is why SwiftUI code looks the way it does — it is a pipeline of value transformations, not a graph of mutable objects.

### Enums — More Powerful Than You Think

Swift enums are not C# enums. C# enums are named integers. Swift enums are full algebraic data types:

```swift
enum NetworkError: Error {
    case noConnection
    case timeout(seconds: Int)
    case serverError(statusCode: Int, message: String)
    case unknown(underlying: Error)
}

func handleError(_ error: NetworkError) {
    switch error {
    case .noConnection:
        print("Check your internet connection")
    case .timeout(let seconds):
        print("Request timed out after \(seconds) seconds")
    case .serverError(let code, let message):
        print("Server returned \(code): \(message)")
    case .unknown(let underlying):
        print("Unknown error: \(underlying)")
    }
}
```

Each case can carry associated values of different types. The `switch` statement is exhaustive — the compiler requires you to handle every case (or provide a `default`). This is equivalent to pattern matching on sealed classes in Kotlin or C# records, but more concise.

Swift enums can also have methods, computed properties, and conform to protocols:

```swift
enum Compass: String, CaseIterable {
    case north, south, east, west

    var opposite: Compass {
        switch self {
        case .north: return .south
        case .south: return .north
        case .east: return .west
        case .west: return .east
        }
    }
}

for direction in Compass.allCases {
    print("\(direction) -> \(direction.opposite)")
}
```

### Protocols — Swift's Interfaces (and More)

A protocol in Swift is like an interface in C#, but with some key differences:

```swift
protocol Drawable {
    func draw()
    var boundingBox: CGRect { get }
}

protocol Resizable {
    mutating func resize(to size: CGSize)
}

struct Circle: Drawable, Resizable {
    var center: CGPoint
    var radius: Double

    func draw() {
        print("Drawing circle at \(center) with radius \(radius)")
    }

    var boundingBox: CGRect {
        CGRect(
            x: center.x - radius,
            y: center.y - radius,
            width: radius * 2,
            height: radius * 2
        )
    }

    mutating func resize(to size: CGSize) {
        radius = min(size.width, size.height) / 2
    }
}
```

Key differences from C# interfaces:

1. **Structs can conform to protocols.** In C#, structs can implement interfaces, but boxing occurs in many scenarios. In Swift, protocol conformance for structs is a first-class, zero-cost abstraction.

2. **Protocol extensions provide default implementations.** This is similar to C# default interface methods (introduced in C# 8.0), but Swift's version is more powerful and more widely used:

```swift
protocol Describable {
    var description: String { get }
}

extension Describable {
    var description: String { "\(type(of: self))" }
}

// Any type conforming to Describable gets description for free
struct Dog: Describable {
    let name: String
    // Uses default description: "Dog"
}
```

3. **Protocol-oriented programming.** Swift encourages designing systems around protocols rather than class hierarchies. Instead of thinking "what is this thing?" (class hierarchy), think "what can this thing do?" (protocol conformance). This is similar to interface-based design in C#, but Swift's standard library is designed around it from the ground up.

### Error Handling

Swift uses a `throws`/`try`/`catch` pattern that is similar to but distinct from C#:

```swift
enum ValidationError: Error {
    case tooShort(minimum: Int)
    case invalidCharacters
    case alreadyTaken
}

func validateUsername(_ username: String) throws -> String {
    guard username.count >= 3 else {
        throw ValidationError.tooShort(minimum: 3)
    }
    guard username.allSatisfy({ $0.isLetter || $0.isNumber }) else {
        throw ValidationError.invalidCharacters
    }
    return username.lowercased()
}

// Calling a throwing function
do {
    let username = try validateUsername("ab")
    print("Valid: \(username)")
} catch ValidationError.tooShort(let minimum) {
    print("Username must be at least \(minimum) characters")
} catch ValidationError.invalidCharacters {
    print("Username contains invalid characters")
} catch {
    print("Unexpected error: \(error)")
}
```

Key differences from C#:

1. **`throws` is part of the function signature.** In C#, any method can throw any exception — there is no way to know from the signature. In Swift, `throws` explicitly marks functions that can fail, and the compiler forces you to handle the error with `try`.

2. **Errors are values, not classes.** Any type conforming to the `Error` protocol can be thrown. Most errors are enums with associated values, not class hierarchies.

3. **`try?` converts to optional.** If you do not care about the specific error:

```swift
let username = try? validateUsername("ab")  // nil if it throws
```

4. **`try!` force-unwraps the result.** Crashes if the function throws:

```swift
let username = try! validateUsername("alice")  // Crashes if it throws
```

### Concurrency — async/await and Actors

Swift's concurrency model was introduced in Swift 5.5 (2021) and has been refined through Swift 6.x. It is remarkably similar to C#'s async/await, with one major addition: **actors** for safe shared mutable state.

```swift
// Async function
func fetchUser(id: String) async throws -> User {
    let url = URL(string: "https://api.example.com/users/\(id)")!
    let (data, response) = try await URLSession.shared.data(from: url)

    guard let httpResponse = response as? HTTPURLResponse,
          httpResponse.statusCode == 200 else {
        throw NetworkError.serverError(statusCode: 0, message: "Bad response")
    }

    return try JSONDecoder().decode(User.self, from: data)
}

// Calling from another async context
func loadUserProfile() async {
    do {
        let user = try await fetchUser(id: "123")
        print("Loaded: \(user.name)")
    } catch {
        print("Failed: \(error)")
    }
}
```

The `async` keyword marks a function that can suspend. The `await` keyword marks suspension points. This is identical to C#'s `async`/`await` in semantics.

**Structured concurrency with task groups:**

```swift
func fetchAllUsers(ids: [String]) async throws -> [User] {
    try await withThrowingTaskGroup(of: User.self) { group in
        for id in ids {
            group.addTask {
                try await fetchUser(id: id)
            }
        }

        var users: [User] = []
        for try await user in group {
            users.append(user)
        }
        return users
    }
}
```

This is equivalent to `Task.WhenAll` in C#, but with structured lifetime management — when the task group scope exits, all child tasks are cancelled.

**Actors for shared mutable state:**

```swift
actor UserCache {
    private var cache: [String: User] = [:]

    func get(_ id: String) -> User? {
        cache[id]
    }

    func set(_ user: User, for id: String) {
        cache[id] = user
    }

    var count: Int { cache.count }
}

let cache = UserCache()
await cache.set(user, for: "123")    // Must use await — actor isolation
let user = await cache.get("123")
```

An actor is like a class, but all access to its mutable state is serialized. Only one task can execute actor methods at a time. The compiler enforces that you use `await` when calling actor methods from outside the actor. This eliminates data races at compile time.

C# does not have actors built into the language (though frameworks like Orleans, Akka.NET, and Dapr provide actor models). Swift's built-in actor support is a significant advantage for concurrent programming.

**Swift 6 strict concurrency.** Starting with Swift 6.0 (September 2024), the compiler can enforce complete data-race safety. The `Sendable` protocol marks types that can safely be shared across concurrency domains. The compiler checks that you do not share non-sendable mutable state between tasks. Swift 6.1 and 6.2 made these checks more practical with relaxed rules and the `@concurrent` attribute.

## Part 2: Data Structures and Algorithms in Swift

We are implementing everything using only Swift's standard library. No packages. No frameworks. Just the language itself.

### Arrays

```swift
var numbers = [1, 2, 3, 4, 5]  // Array<Int>

// Access: O(1)
print(numbers[0])  // 1

// Append: O(1) amortized
numbers.append(6)

// Insert at index: O(n)
numbers.insert(0, at: 0)

// Remove: O(n) for arbitrary index, O(1) for last
numbers.removeLast()

// Iteration
for n in numbers {
    print(n)
}

// Functional operations (like LINQ)
let evens = numbers.filter { $0 % 2 == 0 }
let doubled = numbers.map { $0 * 2 }
let sum = numbers.reduce(0, +)
```

Swift arrays use copy-on-write (COW): when you assign an array to a new variable, no actual copying occurs until one of them is mutated. This gives you value semantics with reference-type performance for the common case.

### Linked List

```swift
class LinkedList<T> {
    class Node {
        var value: T
        var next: Node?

        init(value: T, next: Node? = nil) {
            self.value = value
            self.next = next
        }
    }

    private var head: Node?
    private(set) var count = 0

    var isEmpty: Bool { head == nil }

    func prepend(_ value: T) {
        head = Node(value: value, next: head)
        count += 1
    }

    func append(_ value: T) {
        guard let head = head else {
            self.head = Node(value: value)
            count += 1
            return
        }
        var current = head
        while let next = current.next {
            current = next
        }
        current.next = Node(value: value)
        count += 1
    }

    @discardableResult
    func removeFirst() -> T? {
        let value = head?.value
        head = head?.next
        if value != nil { count -= 1 }
        return value
    }

    func contains(where predicate: (T) -> Bool) -> Bool {
        var current = head
        while let node = current {
            if predicate(node.value) { return true }
            current = node.next
        }
        return false
    }

    func toArray() -> [T] {
        var result: [T] = []
        var current = head
        while let node = current {
            result.append(node.value)
            current = node.next
        }
        return result
    }
}

extension LinkedList: CustomStringConvertible where T: CustomStringConvertible {
    var description: String {
        toArray().map(\.description).joined(separator: " -> ")
    }
}

// Usage
let list = LinkedList<Int>()
list.prepend(3)
list.prepend(2)
list.prepend(1)
list.append(4)
print(list)  // 1 -> 2 -> 3 -> 4
```

Note the `where T: CustomStringConvertible` constraint on the extension — this is a conditional conformance, similar to C#'s constrained extension methods but more powerful because you can add entire protocol conformances conditionally.

### Stack

```swift
struct Stack<T> {
    private var elements: [T] = []

    var count: Int { elements.count }
    var isEmpty: Bool { elements.isEmpty }

    mutating func push(_ value: T) {
        elements.append(value)
    }

    @discardableResult
    mutating func pop() -> T? {
        elements.popLast()
    }

    func peek() -> T? {
        elements.last
    }
}

// Usage
var stack = Stack<Int>()
stack.push(1)
stack.push(2)
stack.push(3)
print(stack.pop()!)  // 3
print(stack.peek()!) // 2
```

Notice `mutating` on methods that modify the struct. Since `Stack` is a value type (struct), methods that change its state must be marked `mutating`. This is enforced by the compiler — you cannot call a mutating method on a `let` constant.

### Queue

```swift
struct Queue<T> {
    private var elements: [T] = []
    private var headIndex = 0

    var count: Int { elements.count - headIndex }
    var isEmpty: Bool { count == 0 }

    mutating func enqueue(_ value: T) {
        elements.append(value)
    }

    @discardableResult
    mutating func dequeue() -> T? {
        guard headIndex < elements.count else { return nil }
        let value = elements[headIndex]
        headIndex += 1

        // Reclaim memory when half the array is dequeued
        if headIndex > elements.count / 2 {
            elements.removeFirst(headIndex)
            headIndex = 0
        }

        return value
    }

    func peek() -> T? {
        guard headIndex < elements.count else { return nil }
        return elements[headIndex]
    }
}
```

This implementation avoids the O(n) cost of `removeFirst()` on arrays by tracking a head index and periodically compacting.

### Hash Map — Building Our Own

```swift
struct SimpleHashMap<Key: Hashable, Value> {
    private struct Entry {
        let key: Key
        var value: Value
    }

    private var buckets: [[Entry]]
    private let capacity: Int
    private(set) var count = 0

    init(capacity: Int = 16) {
        self.capacity = capacity
        self.buckets = Array(repeating: [], count: capacity)
    }

    private func bucketIndex(for key: Key) -> Int {
        abs(key.hashValue) % capacity
    }

    mutating func set(_ value: Value, for key: Key) {
        let index = bucketIndex(for: key)
        for i in buckets[index].indices {
            if buckets[index][i].key == key {
                buckets[index][i].value = value
                return
            }
        }
        buckets[index].append(Entry(key: key, value: value))
        count += 1
    }

    func get(_ key: Key) -> Value? {
        let index = bucketIndex(for: key)
        return buckets[index].first { $0.key == key }?.value
    }

    mutating func remove(_ key: Key) -> Value? {
        let index = bucketIndex(for: key)
        guard let entryIndex = buckets[index].firstIndex(where: { $0.key == key }) else {
            return nil
        }
        let value = buckets[index][entryIndex].value
        buckets[index].remove(at: entryIndex)
        count -= 1
        return value
    }
}
```

Swift's `Hashable` protocol is equivalent to implementing `GetHashCode()` and `Equals()` in C#. Any type that conforms to `Hashable` can be used as a dictionary key.

### Binary Search

```swift
func binarySearch<T: Comparable>(_ array: [T], target: T) -> Int? {
    var low = 0
    var high = array.count - 1

    while low <= high {
        let mid = low + (high - low) / 2
        if array[mid] == target {
            return mid
        } else if array[mid] < target {
            low = mid + 1
        } else {
            high = mid - 1
        }
    }

    return nil  // Not found — returns optional
}

let numbers = [1, 3, 5, 7, 9, 11, 13]
if let index = binarySearch(numbers, target: 7) {
    print("Found at index \(index)")  // Found at index 3
} else {
    print("Not found")
}
```

Note the return type `Int?` — the function returns an optional because the target might not exist. This is idiomatic Swift: use optionals to represent the possibility of absence.

### Merge Sort

```swift
func mergeSort<T: Comparable>(_ array: [T]) -> [T] {
    guard array.count > 1 else { return array }

    let mid = array.count / 2
    let left = mergeSort(Array(array[..<mid]))
    let right = mergeSort(Array(array[mid...]))

    return merge(left, right)
}

private func merge<T: Comparable>(_ left: [T], _ right: [T]) -> [T] {
    var result: [T] = []
    result.reserveCapacity(left.count + right.count)

    var i = 0, j = 0
    while i < left.count && j < right.count {
        if left[i] <= right[j] {
            result.append(left[i])
            i += 1
        } else {
            result.append(right[j])
            j += 1
        }
    }

    result.append(contentsOf: left[i...])
    result.append(contentsOf: right[j...])

    return result
}

let unsorted = [38, 27, 43, 3, 9, 82, 10]
print(mergeSort(unsorted))  // [3, 9, 10, 27, 38, 43, 82]
```

Note `reserveCapacity` — a performance optimization that pre-allocates memory for the result array, avoiding reallocation during appends.

### Graph with BFS and DFS

```swift
struct Graph<T: Hashable> {
    private var adjacencyList: [T: [T]] = [:]

    mutating func addEdge(from: T, to: T) {
        adjacencyList[from, default: []].append(to)
        adjacencyList[to, default: []].append(from)  // Undirected
    }

    func bfs(from start: T) -> [T] {
        var visited: Set<T> = [start]
        var queue: [T] = [start]
        var result: [T] = []
        var index = 0

        while index < queue.count {
            let current = queue[index]
            index += 1
            result.append(current)

            for neighbor in adjacencyList[current] ?? [] {
                if !visited.contains(neighbor) {
                    visited.insert(neighbor)
                    queue.append(neighbor)
                }
            }
        }

        return result
    }

    func dfs(from start: T) -> [T] {
        var visited: Set<T> = []
        var result: [T] = []

        func visit(_ node: T) {
            visited.insert(node)
            result.append(node)
            for neighbor in adjacencyList[node] ?? [] {
                if !visited.contains(neighbor) {
                    visit(neighbor)
                }
            }
        }

        visit(start)
        return result
    }
}

var graph = Graph<String>()
graph.addEdge(from: "A", to: "B")
graph.addEdge(from: "A", to: "C")
graph.addEdge(from: "B", to: "D")
graph.addEdge(from: "C", to: "D")
graph.addEdge(from: "D", to: "E")

print("BFS:", graph.bfs(from: "A"))  // [A, B, C, D, E]
print("DFS:", graph.dfs(from: "A"))  // [A, B, D, C, E]
```

## Part 3: SwiftUI — Declarative UI for Apple Platforms

### What SwiftUI Is

SwiftUI is Apple's declarative UI framework, introduced at WWDC 2019. It replaces UIKit (imperative, Objective-C heritage, view controller-based) with a declarative, Swift-native approach. If you know Blazor, the mental model transfers directly:

| Concept | SwiftUI | Blazor |
|---|---|---|
| UI declaration | `@Composable`-like `body` property | Razor markup |
| State management | `@State`, `@Observable` | `@code { }` block |
| Data binding | Automatic via `@State` | `@bind-value` |
| Event handling | Closures on views | `@onclick` etc. |
| Navigation | `NavigationStack` | Blazor Router |
| List rendering | `ForEach` / `List` | `@foreach` |

### Your First SwiftUI View

```swift
import SwiftUI

struct ContentView: View {
    var body: some View {
        VStack {
            Text("Hello, World!")
                .font(.largeTitle)
                .foregroundStyle(.blue)

            Text("Welcome to SwiftUI")
                .font(.subheadline)
                .foregroundStyle(.secondary)
        }
        .padding()
    }
}
```

Every SwiftUI view is a struct that conforms to the `View` protocol. The `body` property returns the view's content. Views are composed by nesting — `VStack` stacks children vertically, `HStack` horizontally, `ZStack` overlays them.

Modifiers like `.font()`, `.foregroundStyle()`, and `.padding()` return new view values. They do not mutate the original view. This is functional composition:

```swift
// This:
Text("Hello").font(.title).padding()

// Is equivalent to:
let t1 = Text("Hello")
let t2 = t1.font(.title)     // Returns ModifiedContent<Text, _FontModifier>
let t3 = t2.padding()         // Returns ModifiedContent<ModifiedContent<...>, _PaddingModifier>
```

Each modifier wraps the previous view in a new type. The compiler knows the exact type at compile time, enabling zero-cost abstraction — no heap allocation, no virtual dispatch.

### State Management

```swift
struct CounterView: View {
    @State private var count = 0

    var body: some View {
        VStack(spacing: 20) {
            Text("Count: \(count)")
                .font(.title)

            HStack {
                Button("Decrement") { count -= 1 }
                Button("Increment") { count += 1 }
            }
        }
    }
}
```

`@State` is a property wrapper that tells SwiftUI to manage this value's storage. When the state changes, SwiftUI re-evaluates the `body` property and updates only the parts of the UI that depend on the changed state.

This is equivalent to Blazor's `StateHasChanged()` being called automatically when you modify a bound property.

For more complex state, use `@Observable` (introduced in iOS 17 / Swift 5.9):

```swift
@Observable
class TodoViewModel {
    var items: [TodoItem] = []
    var newItemText = ""
    var isLoading = false
    var errorMessage: String?

    func addItem() {
        guard !newItemText.isEmpty else { return }
        items.append(TodoItem(title: newItemText))
        newItemText = ""
    }

    func deleteItem(at offsets: IndexSet) {
        items.remove(atOffsets: offsets)
    }

    func loadItems() async {
        isLoading = true
        defer { isLoading = false }

        do {
            items = try await api.fetchItems()
        } catch {
            errorMessage = error.localizedDescription
        }
    }
}

struct TodoItem: Identifiable {
    let id = UUID()
    var title: String
    var isCompleted = false
}
```

And the view:

```swift
struct TodoListView: View {
    @State private var viewModel = TodoViewModel()

    var body: some View {
        NavigationStack {
            List {
                ForEach(viewModel.items) { item in
                    HStack {
                        Image(systemName: item.isCompleted ? "checkmark.circle.fill" : "circle")
                            .foregroundStyle(item.isCompleted ? .green : .secondary)
                        Text(item.title)
                    }
                }
                .onDelete(perform: viewModel.deleteItem)
            }
            .navigationTitle("Todos")
            .toolbar {
                ToolbarItem(placement: .primaryAction) {
                    Button("Add") { viewModel.addItem() }
                }
            }
            .overlay {
                if viewModel.isLoading {
                    ProgressView()
                }
            }
            .task {
                await viewModel.loadItems()
            }
        }
    }
}
```

The `.task` modifier launches an async task when the view appears and cancels it when the view disappears. This is structured concurrency in practice.

### Navigation

```swift
struct AppView: View {
    var body: some View {
        NavigationStack {
            List {
                NavigationLink("Profile", value: Route.profile)
                NavigationLink("Settings", value: Route.settings)
                NavigationLink("About", value: Route.about)
            }
            .navigationTitle("My App")
            .navigationDestination(for: Route.self) { route in
                switch route {
                case .profile:
                    ProfileView()
                case .settings:
                    SettingsView()
                case .about:
                    AboutView()
                }
            }
        }
    }
}

enum Route: Hashable {
    case profile
    case settings
    case about
}
```

`NavigationStack` manages a stack of views with push/pop navigation. `NavigationLink` pushes a new view when tapped. `navigationDestination` defines what view to show for each route value. This is type-safe, value-based navigation — no string route matching.

### Lists and Forms

```swift
struct SettingsView: View {
    @AppStorage("username") private var username = ""
    @AppStorage("notificationsEnabled") private var notifications = true
    @AppStorage("fontSize") private var fontSize = 14.0

    var body: some View {
        Form {
            Section("Account") {
                TextField("Username", text: $username)
                Toggle("Enable Notifications", isOn: $notifications)
            }

            Section("Appearance") {
                Slider(value: $fontSize, in: 10...24, step: 1) {
                    Text("Font Size: \(Int(fontSize))")
                }
            }

            Section("Info") {
                LabeledContent("Version", value: "1.0.0")
                LabeledContent("Build", value: "42")
            }
        }
        .navigationTitle("Settings")
    }
}
```

`@AppStorage` is a property wrapper that automatically persists values to `UserDefaults`. The `$` prefix creates a binding — a two-way connection between the state and the UI control. When the user types in the text field, `username` is updated. When `username` changes programmatically, the text field updates.

This is equivalent to Blazor's `@bind-Value` syntax.

## Part 4: The Apple Development Ecosystem

### Xcode

Xcode is Apple's IDE. It is the only officially supported way to build and submit apps for Apple platforms. There is no alternative. Unlike Android development (where you can use Android Studio, IntelliJ, VS Code, or even Vim), Apple app development requires Xcode for code signing, provisioning, asset compilation, and App Store submission.

The current stable version is **Xcode 26.4** (March 24, 2026). Apple changed the version numbering from sequential (16.x) to year-aligned (26.x) to match their OS versioning.

Xcode includes:
- The Swift and Objective-C compilers
- Interface Builder and SwiftUI Previews
- The iOS/macOS/watchOS/tvOS/visionOS simulators
- Instruments (performance profiling)
- The code signing and provisioning system
- TestFlight integration
- App Store Connect submission tools

### The Project Structure

An Xcode project has this structure:

```
MyApp/
├── MyApp.xcodeproj/       or MyApp.xcworkspace/
├── MyApp/
│   ├── MyAppApp.swift     ← App entry point
│   ├── ContentView.swift  ← Root view
│   ├── Models/
│   ├── Views/
│   ├── ViewModels/
│   ├── Services/
│   ├── Assets.xcassets/   ← Images, colors, app icon
│   ├── Info.plist          ← App configuration (optional since iOS 15)
│   └── Preview Content/
├── MyAppTests/
├── MyAppUITests/
└── Package.swift           ← If using Swift Package Manager
```

The app entry point:

```swift
import SwiftUI

@main
struct MyApp: App {
    var body: some Scene {
        WindowGroup {
            ContentView()
        }
    }
}
```

`@main` marks the entry point. `App` is a protocol that defines the app's structure. `WindowGroup` creates a window containing `ContentView`. For a simple app, this is all you need.

### Swift Package Manager

Swift Package Manager (SPM) is the official dependency manager, built into both Xcode and the `swift` command-line tool. It uses a `Package.swift` manifest:

```swift
// swift-tools-version: 6.0
import PackageDescription

let package = Package(
    name: "MyApp",
    platforms: [
        .iOS(.v17),
        .macOS(.v14)
    ],
    dependencies: [
        // No external dependencies — we build everything ourselves
    ],
    targets: [
        .executableTarget(
            name: "MyApp",
            dependencies: []
        ),
        .testTarget(
            name: "MyAppTests",
            dependencies: ["MyApp"]
        )
    ]
)
```

SPM is equivalent to NuGet in .NET. Dependencies are declared in the manifest, resolved automatically, and compiled from source (not pre-compiled binaries, unlike NuGet). This means build times are longer for first-time resolution but you get full source transparency.

## Part 5: Code Signing and Provisioning — The Hazing Ritual

This is the part that confuses everyone. Let us demystify it.

### Why Code Signing Exists

Apple requires every app to be cryptographically signed before it can run on a device or be distributed through the App Store. The purposes are:

1. **Identity**: The signature proves the app came from a specific developer.
2. **Integrity**: The signature covers the entire app bundle. Any modification invalidates it.
3. **Authorization**: The provisioning profile embedded in the app specifies which devices can run it and which capabilities (push notifications, iCloud, etc.) it can use.

### The Components

**Apple Developer Account**: A membership in the Apple Developer Program ($99/year). Required for distributing apps through the App Store or TestFlight. Not required for development on your own devices.

**Signing Certificate**: A cryptographic certificate issued by Apple that identifies you or your organization. There are two types:
- **Development certificate**: For building and running on your devices during development.
- **Distribution certificate**: For submitting to the App Store or distributing via TestFlight/Ad Hoc.

**Provisioning Profile**: A file that ties together three things: a signing certificate, an App ID, and a list of authorized devices (for development/ad hoc). It answers the question: "Is this developer allowed to run this app on these devices with these capabilities?"

**App ID**: A unique identifier for your app, consisting of a Team ID (assigned by Apple) and a Bundle ID (chosen by you). Example: `ABCDE12345.com.example.myapp`.

**Entitlements**: A property list declaring which system capabilities your app uses — push notifications, iCloud, HealthKit, Apple Pay, etc. Each entitlement must be registered with Apple and included in the provisioning profile.

### How It All Fits Together

```
Developer writes code
        |
        v
Xcode compiles the code
        |
        v
Xcode signs the app bundle with the signing certificate
        |
        v
Xcode embeds the provisioning profile
        |
        v
The signed, provisioned app is installed on a device
        |
        v
iOS verifies:
  1. Is the signature valid? (certificate check)
  2. Is the provisioning profile valid? (not expired, correct App ID)
  3. Is this device authorized? (UUID in profile, for dev/ad hoc)
  4. Are the entitlements authorized? (capabilities in profile)
        |
        v
If all checks pass: app runs
If any check fails: app refuses to install/launch
```

### Automatic Signing

For development, Xcode can manage all of this automatically. In your project settings, enable "Automatically manage signing" and select your team. Xcode will:

1. Create a development signing certificate if you do not have one
2. Register your device with Apple
3. Create a development provisioning profile
4. Sign the app

For App Store distribution, you still need to configure some things manually, but Xcode handles most of the complexity.

### Manual Signing for CI/CD

In a CI/CD environment (like GitHub Actions), you cannot use Xcode's automatic signing because there is no interactive login. Instead, you:

1. Export your signing certificate and private key as a `.p12` file
2. Export or download your provisioning profile
3. Store both as base64-encoded secrets in your CI system
4. In the build script, decode them, install the certificate into a temporary keychain, and copy the provisioning profile to the correct location

We will set this up in Part 7.

## Part 6: App Architecture — MVVM with SwiftUI

### The Pattern

SwiftUI apps use MVVM (Model-View-ViewModel), similar to what you would use in Blazor or WPF:

```
Model (data + business logic)
    ↕
ViewModel (transforms model data for display, handles user actions)
    ↕
View (declares UI based on ViewModel state)
```

### A Complete Example

**Model:**

```swift
struct Article: Identifiable, Codable {
    let id: UUID
    let title: String
    let body: String
    let publishedAt: Date
    let author: String
    var isFavorite: Bool

    static let example = Article(
        id: UUID(),
        title: "Getting Started with Swift",
        body: "Swift is a powerful language...",
        publishedAt: .now,
        author: "Observer Team",
        isFavorite: false
    )
}
```

**Service:**

```swift
actor ArticleService {
    func fetchArticles() async throws -> [Article] {
        let url = URL(string: "https://api.example.com/articles")!
        let (data, _) = try await URLSession.shared.data(from: url)
        let decoder = JSONDecoder()
        decoder.dateDecodingStrategy = .iso8601
        return try decoder.decode([Article].self, from: data)
    }
}
```

**ViewModel:**

```swift
@Observable
class ArticleListViewModel {
    private let service = ArticleService()

    var articles: [Article] = []
    var isLoading = false
    var errorMessage: String?
    var searchText = ""

    var filteredArticles: [Article] {
        if searchText.isEmpty {
            return articles
        }
        return articles.filter {
            $0.title.localizedCaseInsensitiveContains(searchText)
        }
    }

    func loadArticles() async {
        isLoading = true
        errorMessage = nil
        defer { isLoading = false }

        do {
            articles = try await service.fetchArticles()
        } catch {
            errorMessage = error.localizedDescription
        }
    }

    func toggleFavorite(_ article: Article) {
        guard let index = articles.firstIndex(where: { $0.id == article.id }) else { return }
        articles[index].isFavorite.toggle()
    }
}
```

**View:**

```swift
struct ArticleListView: View {
    @State private var viewModel = ArticleListViewModel()

    var body: some View {
        NavigationStack {
            Group {
                if viewModel.isLoading {
                    ProgressView("Loading articles...")
                } else if let error = viewModel.errorMessage {
                    ContentUnavailableView(
                        "Could not load articles",
                        systemImage: "exclamationmark.triangle",
                        description: Text(error)
                    )
                } else {
                    articleList
                }
            }
            .navigationTitle("Articles")
            .searchable(text: $viewModel.searchText)
            .task {
                await viewModel.loadArticles()
            }
            .refreshable {
                await viewModel.loadArticles()
            }
        }
    }

    private var articleList: some View {
        List(viewModel.filteredArticles) { article in
            NavigationLink(value: article.id) {
                ArticleRow(article: article) {
                    viewModel.toggleFavorite(article)
                }
            }
        }
        .navigationDestination(for: UUID.self) { id in
            if let article = viewModel.articles.first(where: { $0.id == id }) {
                ArticleDetailView(article: article)
            }
        }
    }
}

struct ArticleRow: View {
    let article: Article
    let onToggleFavorite: () -> Void

    var body: some View {
        VStack(alignment: .leading, spacing: 4) {
            Text(article.title)
                .font(.headline)
            Text(article.author)
                .font(.caption)
                .foregroundStyle(.secondary)
            Text(article.publishedAt, style: .date)
                .font(.caption2)
                .foregroundStyle(.tertiary)
        }
        .swipeActions(edge: .trailing) {
            Button {
                onToggleFavorite()
            } label: {
                Label(
                    article.isFavorite ? "Unfavorite" : "Favorite",
                    systemImage: article.isFavorite ? "star.slash" : "star"
                )
            }
            .tint(article.isFavorite ? .gray : .yellow)
        }
    }
}
```

### Testing

```swift
import Testing

struct ArticleListViewModelTests {
    @Test func filteredArticlesReturnsAll_whenSearchIsEmpty() {
        let vm = ArticleListViewModel()
        vm.articles = [
            Article(id: UUID(), title: "Swift", body: "", publishedAt: .now, author: "", isFavorite: false),
            Article(id: UUID(), title: "Kotlin", body: "", publishedAt: .now, author: "", isFavorite: false),
        ]
        vm.searchText = ""

        #expect(vm.filteredArticles.count == 2)
    }

    @Test func filteredArticlesFilters_whenSearchHasText() {
        let vm = ArticleListViewModel()
        vm.articles = [
            Article(id: UUID(), title: "Swift Guide", body: "", publishedAt: .now, author: "", isFavorite: false),
            Article(id: UUID(), title: "Kotlin Guide", body: "", publishedAt: .now, author: "", isFavorite: false),
        ]
        vm.searchText = "Swift"

        #expect(vm.filteredArticles.count == 1)
        #expect(vm.filteredArticles[0].title == "Swift Guide")
    }

    @Test func toggleFavorite_togglesValue() {
        let vm = ArticleListViewModel()
        let id = UUID()
        vm.articles = [
            Article(id: id, title: "Test", body: "", publishedAt: .now, author: "", isFavorite: false),
        ]

        vm.toggleFavorite(vm.articles[0])

        #expect(vm.articles[0].isFavorite == true)
    }
}
```

This uses Swift Testing (introduced with Xcode 16), which is the modern testing framework replacing XCTest. The `@Test` attribute marks test functions. The `#expect` macro replaces `XCTAssert`.

## Part 7: GitHub Actions for iOS Builds

### The Challenge

Building iOS apps on CI is harder than building Android apps because:

1. **macOS required.** iOS apps can only be built on macOS. GitHub Actions provides macOS runners, but they are more expensive and slower than Linux runners.
2. **Code signing.** You need to install certificates and provisioning profiles on the CI machine.
3. **Xcode versions.** Different projects require different Xcode versions. GitHub's macOS runners come with a specific Xcode version pre-installed, and you may need to select a different one.

### Setting Up Secrets

Store these as GitHub repository secrets:

- `BUILD_CERTIFICATE_BASE64` — Your distribution certificate exported as `.p12`, base64-encoded
- `P12_PASSWORD` — The password for the `.p12` file
- `BUILD_PROVISION_PROFILE_BASE64` — Your provisioning profile, base64-encoded
- `KEYCHAIN_PASSWORD` — An arbitrary password for a temporary keychain

### The Workflow

```yaml
# .github/workflows/build-ios.yml
name: Build iOS App

on:
  push:
    branches: ['*']
  pull_request:
    branches: [main]

concurrency:
  group: ios-${{ github.ref }}
  cancel-in-progress: true

jobs:
  build:
    runs-on: macos-15
    timeout-minutes: 45

    steps:
      - name: Checkout
        uses: actions/checkout@v4

      - name: Select Xcode version
        run: sudo xcode-select -s /Applications/Xcode_16.3.app/Contents/Developer

      - name: Show environment
        run: |
          xcodebuild -version
          swift --version

      - name: Install the Apple certificate and provisioning profile
        env:
          BUILD_CERTIFICATE_BASE64: ${{ secrets.BUILD_CERTIFICATE_BASE64 }}
          P12_PASSWORD: ${{ secrets.P12_PASSWORD }}
          BUILD_PROVISION_PROFILE_BASE64: ${{ secrets.BUILD_PROVISION_PROFILE_BASE64 }}
          KEYCHAIN_PASSWORD: ${{ secrets.KEYCHAIN_PASSWORD }}
        run: |
          # Create variables
          CERTIFICATE_PATH=$RUNNER_TEMP/build_certificate.p12
          PP_PATH=$RUNNER_TEMP/build_pp.mobileprovision
          KEYCHAIN_PATH=$RUNNER_TEMP/app-signing.keychain-db

          # Decode from base64
          echo -n "$BUILD_CERTIFICATE_BASE64" | base64 --decode -o $CERTIFICATE_PATH
          echo -n "$BUILD_PROVISION_PROFILE_BASE64" | base64 --decode -o $PP_PATH

          # Create temporary keychain
          security create-keychain -p "$KEYCHAIN_PASSWORD" $KEYCHAIN_PATH
          security set-keychain-settings -lut 21600 $KEYCHAIN_PATH
          security unlock-keychain -p "$KEYCHAIN_PASSWORD" $KEYCHAIN_PATH

          # Import certificate
          security import $CERTIFICATE_PATH -P "$P12_PASSWORD" \
            -A -t cert -f pkcs12 -k $KEYCHAIN_PATH
          security set-key-partition-list -S apple-tool:,apple: \
            -k "$KEYCHAIN_PASSWORD" $KEYCHAIN_PATH
          security list-keychain -d user -s $KEYCHAIN_PATH

          # Copy provisioning profile
          mkdir -p ~/Library/MobileDevice/Provisioning\ Profiles
          cp $PP_PATH ~/Library/MobileDevice/Provisioning\ Profiles

      - name: Resolve Swift packages
        run: |
          xcodebuild -resolvePackageDependencies \
            -project MyApp.xcodeproj \
            -scheme MyApp

      - name: Run tests
        run: |
          xcodebuild test \
            -project MyApp.xcodeproj \
            -scheme MyApp \
            -destination 'platform=iOS Simulator,name=iPhone 16,OS=18.0' \
            -resultBundlePath TestResults.xcresult \
            | xcpretty

      - name: Build archive
        run: |
          xcodebuild archive \
            -project MyApp.xcodeproj \
            -scheme MyApp \
            -configuration Release \
            -archivePath $RUNNER_TEMP/MyApp.xcarchive \
            -destination 'generic/platform=iOS' \
            CODE_SIGN_STYLE=Manual \
            | xcpretty

      - name: Export IPA
        run: |
          xcodebuild -exportArchive \
            -archivePath $RUNNER_TEMP/MyApp.xcarchive \
            -exportPath $RUNNER_TEMP/export \
            -exportOptionsPlist ExportOptions.plist

      - name: Upload IPA artifact
        uses: actions/upload-artifact@v4
        with:
          name: ipa-${{ github.sha }}
          path: ${{ runner.temp }}/export/*.ipa
          retention-days: 30

      - name: Upload test results
        if: always()
        uses: actions/upload-artifact@v4
        with:
          name: test-results
          path: TestResults.xcresult
          retention-days: 7

      - name: Clean up keychain
        if: always()
        run: security delete-keychain $RUNNER_TEMP/app-signing.keychain-db
```

The `ExportOptions.plist` file specifies how to export the archive:

```xml
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN"
  "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>method</key>
    <string>app-store</string>
    <key>teamID</key>
    <string>YOUR_TEAM_ID</string>
    <key>uploadSymbols</key>
    <true/>
    <key>compileBitcode</key>
    <false/>
</dict>
</plist>
```

### Uploading to TestFlight

Add a step after the export to upload to App Store Connect:

```yaml
      - name: Upload to TestFlight
        if: github.ref == 'refs/heads/main'
        env:
          APP_STORE_CONNECT_API_KEY_ID: ${{ secrets.ASC_KEY_ID }}
          APP_STORE_CONNECT_ISSUER_ID: ${{ secrets.ASC_ISSUER_ID }}
          APP_STORE_CONNECT_API_KEY: ${{ secrets.ASC_API_KEY }}
        run: |
          xcrun altool --upload-app \
            --type ios \
            --file $RUNNER_TEMP/export/MyApp.ipa \
            --apiKey "$APP_STORE_CONNECT_API_KEY_ID" \
            --apiIssuer "$APP_STORE_CONNECT_ISSUER_ID"
```

This uploads the IPA to TestFlight on every push to `main`. Testers will automatically receive the new build.

## Part 8: Bad Code vs. Good Code

### Bad: Force Unwrapping Everything

```swift
// BAD — crash waiting to happen
func getUserName(from dict: [String: Any]) -> String {
    return dict["name"] as! String
}
```

```swift
// GOOD — safe and communicative
func getUserName(from dict: [String: Any]) -> String? {
    dict["name"] as? String
}

// Or with a default:
func getUserName(from dict: [String: Any]) -> String {
    dict["name"] as? String ?? "Unknown"
}
```

### Bad: Massive View Bodies

```swift
// BAD — 200-line body property
struct ProfileView: View {
    var body: some View {
        // 200 lines of nested VStack, HStack, ForEach...
    }
}
```

```swift
// GOOD — extract subviews
struct ProfileView: View {
    var body: some View {
        ScrollView {
            VStack(spacing: 20) {
                profileHeader
                statsSection
                recentActivity
            }
            .padding()
        }
    }

    private var profileHeader: some View {
        // Header layout
    }

    private var statsSection: some View {
        // Stats layout
    }

    private var recentActivity: some View {
        // Activity list
    }
}
```

SwiftUI views should be small, focused, and composable. Extract subviews as computed properties or separate structs.

### Bad: Using Class When Struct Will Do

```swift
// BAD — unnecessary reference semantics
class UserSettings {
    var theme: Theme = .light
    var fontSize: Double = 14
}
```

```swift
// GOOD — value type with clear mutation boundaries
struct UserSettings {
    var theme: Theme = .light
    var fontSize: Double = 14
}
```

Use classes only when you need reference semantics (shared identity, inheritance, or the object must be an actor).

### Bad: Callback Hell

```swift
// BAD — nested callbacks
func loadData(completion: @escaping (Result<Data, Error>) -> Void) {
    fetchUser { userResult in
        switch userResult {
        case .success(let user):
            fetchPosts(for: user) { postsResult in
                switch postsResult {
                case .success(let posts):
                    fetchComments(for: posts) { commentsResult in
                        // Three levels deep...
                    }
                case .failure(let error):
                    completion(.failure(error))
                }
            }
        case .failure(let error):
            completion(.failure(error))
        }
    }
}
```

```swift
// GOOD — async/await
func loadData() async throws -> (User, [Post], [Comment]) {
    let user = try await fetchUser()
    let posts = try await fetchPosts(for: user)
    let comments = try await fetchComments(for: posts)
    return (user, posts, comments)
}
```

Async/await flattens callback hell into sequential, readable code. There is no reason to use completion handlers in new Swift code.

## Part 9: App Store Submission

### Preparing Your App

Before submitting, ensure:

1. **App icon**: Provide a 1024x1024 app icon in your asset catalog
2. **Launch screen**: Use a storyboard or SwiftUI launch screen
3. **Privacy manifest**: Declare all data collection and tracking
4. **Target SDK**: As of April 28, 2026, apps must be built with the iOS 26 SDK
5. **Minimum deployment target**: iOS 17 covers over 95% of active devices

### App Store Connect

App Store Connect is the web portal for managing your App Store presence. You create an app record, provide metadata (description, screenshots, keywords, category, pricing), and submit builds for review.

### The Review Process

Apple reviews every app submission. The review checks for:
- Crashes and bugs
- Compliance with the App Store Review Guidelines
- Privacy and data collection practices
- Content appropriateness
- Performance (no excessive battery drain, network usage, etc.)

Reviews typically take 24-48 hours. If your app is rejected, Apple provides specific feedback explaining what to fix.

## Part 10: Resources

- **Swift Language Guide**: https://docs.swift.org/swift-book/documentation/the-swift-programming-language/
- **Swift Evolution**: https://github.com/swiftlang/swift-evolution — Swift 6.3 is the current stable release (March 24, 2026), with Swift 6.4 announced
- **SwiftUI Documentation**: https://developer.apple.com/documentation/swiftui
- **Xcode Downloads**: https://developer.apple.com/xcode/ — Xcode 26.4 is the latest stable (March 24, 2026)
- **Apple Developer**: https://developer.apple.com
- **App Store Review Guidelines**: https://developer.apple.com/app-store/review/guidelines/
- **Swift Package Manager**: https://www.swift.org/documentation/package-manager/
- **Hacking with Swift**: https://www.hackingwithswift.com — Paul Hudson's comprehensive Swift/SwiftUI tutorials
- **Swift by Sundell**: https://www.swiftbysundell.com — John Sundell's articles and podcast
- **Swift.org**: https://www.swift.org — The open-source Swift project
- **App Store Connect**: https://appstoreconnect.apple.com
- **WWDC Videos**: https://developer.apple.com/videos/ — Apple's annual developer conference sessions

Every version number in this article was verified via web search at the time of writing (April 2026). Technologies move fast. When in doubt, check the official documentation.

---

You came into this article as a C# developer who did not know Swift from Objective-C. If you have read carefully, you now understand Swift's type system and how it compares to C#'s — optionals versus nullable reference types, structs versus classes, protocols versus interfaces, enums as algebraic data types. You understand SwiftUI's declarative model and how it parallels Blazor. You understand the code signing and provisioning system that gates access to Apple's billion devices. You understand how to build, test, and ship an iOS app from a GitHub Actions pipeline.

More importantly, you understand the why behind each piece. Why Swift prefers structs. Why optionals are better than null. Why actors exist. Why code signing is structured the way it is. Why SwiftUI uses value types for views.

The Apple ecosystem rewards developers who understand these fundamentals. The APIs are well-designed. The hardware is consistent. The user base expects quality. Build something that meets that expectation.
