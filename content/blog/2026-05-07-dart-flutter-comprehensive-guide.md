---
title: "Dart and Flutter: A Comprehensive Guide from First Principles for the ASP.NET Developer Who Wants to Build Everywhere from a Single Codebase"
date: 2026-05-07
author: myblazor-team
summary: "An exhaustive, from-the-ground-up guide to Dart and Flutter — covering the Dart language from variables through isolates, Flutter's widget system from hello world to production architecture, state management, the rendering pipeline and Impeller engine, data structures and algorithms implemented from scratch, platform channels, building for Android, iOS, web, Windows, macOS, and Linux, app signing for every platform, a complete GitHub Actions CI/CD pipeline, and the practical engineering knowledge you need to ship real applications. Written for C# and ASP.NET developers who need to understand the entire stack, not just follow a wizard."
tags:
  - dart
  - flutter
  - cross-platform
  - mobile
  - web
  - desktop
  - deep-dive
  - ci-cd
  - github-actions
  - data-structures
  - algorithms
  - best-practices
  - software-engineering
  - beginner
  - android
  - ios
---

You build web applications with ASP.NET. You know C#. You know HTTP. You know how to deploy to a server and watch logs scroll by in a terminal. Your world is request-response, controllers, services, and databases.

And now someone wants you to build an application that runs on a phone. And a tablet. And a desktop. And the web. And they want it to look native on each platform. And they want it built by one team, from one codebase, sharing one set of business logic. And they do not want to hire separate iOS, Android, web, Windows, macOS, and Linux developers.

This is the exact problem Flutter was built to solve.

Flutter is Google's open-source UI toolkit for building natively compiled applications for mobile, web, and desktop from a single codebase. It uses Dart as its programming language. The current stable version is **Flutter 3.41.5** with **Dart 3.11**, released in February 2026. Flutter's rendering engine, Impeller, draws every pixel directly using the GPU — it does not use the platform's native UI widgets. This means a Flutter app looks and behaves identically on every platform (unless you specifically tell it not to).

If you come from Blazor, this will sound familiar. Blazor WebAssembly also runs the same C# code everywhere. But Blazor targets the web; Flutter targets everything. Blazor renders HTML; Flutter renders pixels. Blazor uses the browser's layout engine; Flutter uses its own layout engine. They solve related but different problems.

This article will teach you Dart from scratch, compare every concept to C#, implement data structures and algorithms using only the standard library, then teach you Flutter's widget system, state management, navigation, platform channels, and deployment to every supported platform. We will set up CI/CD with GitHub Actions. We will cover app signing for both Android and iOS. We will leave no stone unturned.

## Part 1: Dart — The Language

### Why Dart Exists

Dart was created at Google by Lars Bak and Kasper Lund, both veterans of the V8 JavaScript engine (which powers Chrome and Node.js). It was announced in 2011 and initially positioned as a replacement for JavaScript in web browsers. That vision did not materialize — browsers did not adopt the Dart VM, and the language languished for years.

Then Flutter happened. The Flutter team needed a language that could compile to native ARM code for mobile, compile to JavaScript for the web, support hot reload for a fast development cycle, and had a garbage collector optimized for UI workloads (where objects are created and destroyed rapidly during frame rendering). Dart checked every box. Flutter's success revived Dart, and today Dart exists primarily because of Flutter.

Dart 3.0 (May 2023) introduced sound null safety as a mandatory feature, sealed classes, class modifiers, patterns, and records — many features directly inspired by C#, Kotlin, and Swift. Dart 3.10 (November 2025) added dot shorthands. Dart 3.11 (February 2026) focused on tooling and analysis server improvements.

### Dart vs. C# — Your Mental Map

| Concept | Dart | C# |
|---|---|---|
| Entry point | `void main() { }` | `static void Main() { }` / top-level |
| Variables | `var x = 10;` / `final` / `const` | `var x = 10;` / `readonly` / `const` |
| String interpolation | `'Hello, $name'` or `'${expr}'` | `$"Hello, {name}"` |
| Null safety | `String?` | `string?` (NRTs) |
| Null coalescing | `x ?? 'default'` | `x ?? "default"` |
| Null-aware access | `x?.length` | `x?.Length` |
| Collections | `List<int>`, `Map<String, int>`, `Set<int>` | `List<int>`, `Dictionary<string, int>`, `HashSet<int>` |
| Async | `Future<T>`, `async`, `await` | `Task<T>`, `async`, `await` |
| Streams | `Stream<T>`, `async*`, `yield` | `IAsyncEnumerable<T>`, `async`, `yield return` |
| Generics | `class Box<T> { }` | `class Box<T> { }` |
| Mixins | `mixin Serializable { }` | (no direct equivalent; use interfaces + extension methods) |
| Extension methods | `extension on String { }` | `static class StringExtensions { }` |
| Records | `(String, int)` | `(string, int)` |
| Pattern matching | `switch` with patterns | `switch` with patterns |
| Sealed classes | `sealed class Shape { }` | (similar via abstract records) |
| Package manager | `pub` (pubspec.yaml) | NuGet |
| Build tool | `dart` CLI / `flutter` CLI | `dotnet` CLI |

The two languages are strikingly similar. Dart was influenced by Java, JavaScript, C#, and Erlang. If you know C#, you can read Dart code with minimal effort.

### Variables and Types

```dart
// Type inference
var name = 'Alice';      // Inferred as String
var age = 30;            // Inferred as int
var pi = 3.14159;        // Inferred as double
var items = [1, 2, 3];   // Inferred as List<int>

// Explicit types
String name = 'Alice';
int age = 30;

// Immutable references
final greeting = 'Hello';  // Cannot be reassigned (runtime constant)
const pi = 3.14159;        // Compile-time constant

// greeting = 'Bye';  // Error: a final variable can only be set once
```

`final` is like C#'s `readonly` — the reference cannot be reassigned, but the object it points to can be mutated if it is mutable. `const` is a compile-time constant — the value must be known at compile time, and the object is deeply immutable.

```dart
final list = [1, 2, 3];
list.add(4);  // Fine — the list is mutable; we're not reassigning 'list'

const constantList = [1, 2, 3];
// constantList.add(4);  // Runtime error: Cannot add to an unmodifiable list
```

### Null Safety

Dart has sound null safety since Dart 3.0. Every type is non-nullable by default:

```dart
String name = 'Alice';
// name = null;  // Error: A value of type 'Null' can't be assigned to a variable of type 'String'

String? maybeName = 'Alice';
maybeName = null;  // Fine — String? allows null
```

Working with nullable types:

```dart
// Null-aware operators (identical to C#)
String? maybeName = getName();
int? length = maybeName?.length;        // null if maybeName is null
String displayName = maybeName ?? 'Anonymous';  // default if null

// Null assertion (like C#'s null-forgiving operator)
String definitelyName = maybeName!;  // Throws if null

// If-null assignment
maybeName ??= 'Default';  // Assign only if currently null

// Flow analysis (like C#'s null state analysis)
if (maybeName != null) {
  print(maybeName.length);  // Compiler knows it's non-null here
}
```

Dart's null safety is sound — the compiler guarantees that a non-nullable variable will never be null at runtime (barring use of `!` or interop with older code). This is stronger than C#'s nullable reference types, which are advisory warnings.

### Functions

```dart
// Named function
int add(int a, int b) {
  return a + b;
}

// Arrow syntax for single-expression bodies
int add(int a, int b) => a + b;

// Optional positional parameters
String greet(String name, [String greeting = 'Hello']) {
  return '$greeting, $name!';
}

greet('Alice');              // "Hello, Alice!"
greet('Bob', 'Good morning'); // "Good morning, Bob!"

// Named parameters (like C#'s named arguments, but more idiomatic)
String greet({required String name, String greeting = 'Hello'}) {
  return '$greeting, $name!';
}

greet(name: 'Alice');                        // "Hello, Alice!"
greet(name: 'Bob', greeting: 'Good morning'); // "Good morning, Bob!"
```

Named parameters are used extensively in Flutter. Every widget constructor uses them:

```dart
Container(
  width: 200,
  height: 100,
  color: Colors.blue,
  child: Text('Hello'),
)
```

The `required` keyword marks a named parameter as mandatory. Without it, named parameters are optional and must have a default value or be nullable.

### Classes

```dart
class User {
  final String name;
  final int age;
  final String? email;

  // Constructor with named parameters
  const User({
    required this.name,
    required this.age,
    this.email,
  });

  // Named constructor
  User.anonymous() : name = 'Anonymous', age = 0, email = null;

  // Method
  String greet() => 'Hi, I\'m $name';

  // Getter
  bool get isAdult => age >= 18;

  @override
  String toString() => 'User(name: $name, age: $age, email: $email)';

  @override
  bool operator ==(Object other) =>
      identical(this, other) ||
      other is User && name == other.name && age == other.age && email == other.email;

  @override
  int get hashCode => Object.hash(name, age, email);
}

// Usage
final alice = User(name: 'Alice', age: 30, email: 'alice@example.com');
final anon = User.anonymous();
print(alice.greet());  // "Hi, I'm Alice"
print(alice.isAdult);  // true
```

Key differences from C#:

1. **No `new` keyword needed.** `User(name: 'Alice')` creates an instance. The `new` keyword exists but is unnecessary since Dart 2.

2. **`const` constructors.** If all fields are `final` and the constructor is marked `const`, you can create compile-time constant instances: `const User(name: 'Alice', age: 30)`. Two `const` instances with the same values are identical in memory.

3. **No overloaded constructors.** Dart uses named constructors instead: `User.anonymous()`, `User.fromJson(json)`, etc.

4. **Initializer lists.** The `: name = 'Anonymous', age = 0` syntax initializes fields before the constructor body runs. Similar to C#'s member initializer syntax.

### Records and Patterns (Dart 3.0+)

Records are anonymous, immutable, aggregate types — exactly like C# tuples:

```dart
// Positional record
(String, int) getUserInfo() {
  return ('Alice', 30);
}

var (name, age) = getUserInfo();  // Destructuring

// Named record
({String name, int age}) getUserInfo() {
  return (name: 'Alice', age: 30);
}

var (:name, :age) = getUserInfo();  // Destructuring with names
```

Pattern matching in switch expressions:

```dart
sealed class Shape {}
class Circle extends Shape { final double radius; Circle(this.radius); }
class Rectangle extends Shape { final double width, height; Rectangle(this.width, this.height); }
class Triangle extends Shape { final double base, height; Triangle(this.base, this.height); }

double area(Shape shape) => switch (shape) {
  Circle(radius: var r) => 3.14159 * r * r,
  Rectangle(width: var w, height: var h) => w * h,
  Triangle(base: var b, height: var h) => 0.5 * b * h,
};
```

This is the same pattern matching available in C# 11+ with switch expressions and positional patterns.

### Async Programming — Future, Stream, and Isolates

Dart's async model maps directly to C#:

```dart
// Future<T> is equivalent to Task<T>
Future<String> fetchData(String url) async {
  final response = await http.get(Uri.parse(url));
  return response.body;
}

// Stream<T> is equivalent to IAsyncEnumerable<T>
Stream<int> countUp(int max) async* {
  for (var i = 0; i <= max; i++) {
    await Future.delayed(Duration(seconds: 1));
    yield i;
  }
}

// Consuming a stream
await for (final count in countUp(5)) {
  print(count);  // Prints 0, 1, 2, 3, 4, 5 with 1-second delays
}
```

**Isolates** are Dart's concurrency primitive. Dart is single-threaded within an isolate — there is no shared mutable state between isolates. Each isolate has its own memory heap and event loop. Communication between isolates happens via message passing.

```dart
// Simple compute function — runs in a separate isolate
import 'dart:isolate';

Future<int> computeExpensiveSum(int n) async {
  return await Isolate.run(() {
    var sum = 0;
    for (var i = 0; i <= n; i++) {
      sum += i;
    }
    return sum;
  });
}

void main() async {
  final result = await computeExpensiveSum(1000000000);
  print('Sum: $result');
}
```

`Isolate.run()` is equivalent to `Task.Run()` in C# — it runs compute-intensive work on a background thread (isolate) without blocking the UI. In Flutter, you will use `compute()` or `Isolate.run()` for expensive operations to keep the UI at 60fps.

### Collections and Functional Operations

```dart
final numbers = [5, 3, 8, 1, 9, 2, 7, 4, 6];

// Filter (Where in C#)
final evens = numbers.where((n) => n % 2 == 0).toList();

// Map (Select in C#)
final doubled = numbers.map((n) => n * 2).toList();

// Reduce (Aggregate in C#)
final sum = numbers.reduce((a, b) => a + b);

// Fold (Aggregate with seed in C#)
final product = numbers.fold(1, (acc, n) => acc * n);

// Sort (in-place)
final sorted = List.of(numbers)..sort();

// Any / Every (Any / All in C#)
final hasEven = numbers.any((n) => n % 2 == 0);
final allPositive = numbers.every((n) => n > 0);

// First / FirstWhere (First / FirstOrDefault in C#)
final first = numbers.first;
final firstEven = numbers.firstWhere((n) => n % 2 == 0);

// Spread operator
final combined = [...numbers, 10, 11, 12];

// Collection if / for
final items = [
  'always here',
  if (showExtra) 'conditional item',
  for (var i = 0; i < 3; i++) 'item $i',
];
```

The spread operator (`...`) and collection `if`/`for` are used extensively in Flutter to build widget trees dynamically.

### Extension Methods

```dart
extension StringExtensions on String {
  String capitalize() {
    if (isEmpty) return this;
    return '${this[0].toUpperCase()}${substring(1)}';
  }

  bool get isBlank => trim().isEmpty;
}

extension DateTimeExtensions on DateTime {
  String toFriendlyString() {
    final now = DateTime.now();
    final diff = now.difference(this);
    if (diff.inDays > 0) return '${diff.inDays}d ago';
    if (diff.inHours > 0) return '${diff.inHours}h ago';
    if (diff.inMinutes > 0) return '${diff.inMinutes}m ago';
    return 'just now';
  }
}

// Usage
print('hello'.capitalize());  // "Hello"
print('  '.isBlank);          // true
```

Identical concept to C# extension methods, different syntax.

## Part 2: Data Structures and Algorithms in Dart

Everything implemented with Dart's standard library. No packages.

### Linked List

```dart
class LinkedList<T> {
  _Node<T>? _head;
  int _length = 0;

  int get length => _length;
  bool get isEmpty => _head == null;

  void addFirst(T value) {
    _head = _Node(value, _head);
    _length++;
  }

  void addLast(T value) {
    if (_head == null) {
      _head = _Node(value);
    } else {
      var current = _head!;
      while (current.next != null) {
        current = current.next!;
      }
      current.next = _Node(value);
    }
    _length++;
  }

  T? removeFirst() {
    if (_head == null) return null;
    final value = _head!.value;
    _head = _head!.next;
    _length--;
    return value;
  }

  bool contains(T value) {
    var current = _head;
    while (current != null) {
      if (current.value == value) return true;
      current = current.next;
    }
    return false;
  }

  List<T> toList() {
    final result = <T>[];
    var current = _head;
    while (current != null) {
      result.add(current.value);
      current = current.next;
    }
    return result;
  }

  @override
  String toString() => toList().join(' -> ');
}

class _Node<T> {
  T value;
  _Node<T>? next;
  _Node(this.value, [this.next]);
}
```

### Stack and Queue

```dart
class Stack<T> {
  final _elements = <T>[];

  int get length => _elements.length;
  bool get isEmpty => _elements.isEmpty;

  void push(T value) => _elements.add(value);

  T pop() {
    if (isEmpty) throw StateError('Stack is empty');
    return _elements.removeLast();
  }

  T get peek {
    if (isEmpty) throw StateError('Stack is empty');
    return _elements.last;
  }
}

class Queue<T> {
  final _elements = <T>[];
  int _head = 0;

  int get length => _elements.length - _head;
  bool get isEmpty => length == 0;

  void enqueue(T value) => _elements.add(value);

  T dequeue() {
    if (isEmpty) throw StateError('Queue is empty');
    final value = _elements[_head];
    _head++;
    if (_head > _elements.length ~/ 2) {
      _elements.removeRange(0, _head);
      _head = 0;
    }
    return value;
  }

  T get peek {
    if (isEmpty) throw StateError('Queue is empty');
    return _elements[_head];
  }
}
```

### Binary Search

```dart
int? binarySearch<T extends Comparable<T>>(List<T> sorted, T target) {
  var low = 0;
  var high = sorted.length - 1;

  while (low <= high) {
    final mid = low + (high - low) ~/ 2;
    final cmp = sorted[mid].compareTo(target);
    if (cmp == 0) return mid;
    if (cmp < 0) {
      low = mid + 1;
    } else {
      high = mid - 1;
    }
  }
  return null;
}
```

Note `~/` — Dart's integer division operator. Regular `/` returns a `double` even for integers.

### Merge Sort

```dart
List<T> mergeSort<T extends Comparable<T>>(List<T> list) {
  if (list.length <= 1) return list;

  final mid = list.length ~/ 2;
  final left = mergeSort(list.sublist(0, mid));
  final right = mergeSort(list.sublist(mid));

  return _merge(left, right);
}

List<T> _merge<T extends Comparable<T>>(List<T> left, List<T> right) {
  final result = <T>[];
  var i = 0, j = 0;

  while (i < left.length && j < right.length) {
    if (left[i].compareTo(right[j]) <= 0) {
      result.add(left[i++]);
    } else {
      result.add(right[j++]);
    }
  }

  result.addAll(left.sublist(i));
  result.addAll(right.sublist(j));
  return result;
}
```

### Graph with BFS and DFS

```dart
class Graph<T> {
  final _adjacency = <T, List<T>>{};

  void addEdge(T from, T to) {
    _adjacency.putIfAbsent(from, () => []).add(to);
    _adjacency.putIfAbsent(to, () => []).add(from);
  }

  List<T> bfs(T start) {
    final visited = <T>{start};
    final queue = <T>[start];
    final result = <T>[];
    var index = 0;

    while (index < queue.length) {
      final current = queue[index++];
      result.add(current);
      for (final neighbor in _adjacency[current] ?? []) {
        if (visited.add(neighbor)) {
          queue.add(neighbor);
        }
      }
    }
    return result;
  }

  List<T> dfs(T start) {
    final visited = <T>{};
    final result = <T>[];

    void visit(T node) {
      if (!visited.add(node)) return;
      result.add(node);
      for (final neighbor in _adjacency[node] ?? []) {
        visit(neighbor);
      }
    }

    visit(start);
    return result;
  }
}
```

## Part 3: Flutter — The Widget System

### How Flutter Renders

Flutter does not use native UI components. It does not use UIKit on iOS, Android Views on Android, or HTML elements on the web. Instead, Flutter draws every pixel itself using a graphics engine called **Impeller** (which replaced the older Skia-based engine). Impeller compiles shaders ahead of time to eliminate the "shader compilation jank" that plagued earlier Flutter versions.

When you write a Flutter app, you build a tree of widgets. Flutter converts this widget tree into a render tree, which is then painted to a canvas, which is rasterized by Impeller and displayed on screen. The entire pipeline — layout, painting, compositing — is under Flutter's control.

This architecture means:

1. **Pixel-perfect consistency.** Your app looks identical on every platform because Flutter controls every pixel.
2. **No platform UI limitations.** You can create any visual design — you are not constrained by what the platform's native widgets support.
3. **Performance.** Impeller targets 60fps (or 120fps on capable devices) with pre-compiled shaders and a rendering pipeline optimized for UI workloads.

The tradeoff: Flutter apps do not automatically adopt platform-specific look and feel. A Flutter app on iOS does not look like a native UIKit app unless you explicitly use Cupertino widgets. Most Flutter apps use Material Design, which looks the same everywhere.

### Your First Widget

```dart
import 'package:flutter/material.dart';

void main() {
  runApp(const MyApp());
}

class MyApp extends StatelessWidget {
  const MyApp({super.key});

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      title: 'My First App',
      home: Scaffold(
        appBar: AppBar(title: const Text('Hello Flutter')),
        body: const Center(
          child: Text(
            'Hello, World!',
            style: TextStyle(fontSize: 24),
          ),
        ),
      ),
    );
  }
}
```

Everything in Flutter is a widget. `MaterialApp` is a widget. `Scaffold` is a widget. `AppBar` is a widget. `Center` is a widget. `Text` is a widget. Widgets compose by nesting — each widget's `child` (or `children`) parameter accepts other widgets.

### StatelessWidget vs. StatefulWidget

A `StatelessWidget` has no mutable state. Its `build` method always returns the same result for the same inputs:

```dart
class Greeting extends StatelessWidget {
  const Greeting({super.key, required this.name});
  final String name;

  @override
  Widget build(BuildContext context) {
    return Text('Hello, $name!');
  }
}
```

A `StatefulWidget` has mutable state managed by a companion `State` object:

```dart
class Counter extends StatefulWidget {
  const Counter({super.key});

  @override
  State<Counter> createState() => _CounterState();
}

class _CounterState extends State<Counter> {
  int _count = 0;

  @override
  Widget build(BuildContext context) {
    return Column(
      mainAxisAlignment: MainAxisAlignment.center,
      children: [
        Text('Count: $_count', style: const TextStyle(fontSize: 32)),
        const SizedBox(height: 16),
        ElevatedButton(
          onPressed: () => setState(() => _count++),
          child: const Text('Increment'),
        ),
      ],
    );
  }
}
```

`setState()` tells Flutter that the state has changed and the widget needs to be rebuilt. This is equivalent to Blazor's `StateHasChanged()`.

### Layout

Flutter uses a constraint-based layout system. Parent widgets pass constraints (minimum and maximum width/height) to their children. Children choose a size within those constraints. Parents then position children.

Key layout widgets:

```dart
// Column — vertical stack (like Blazor's vertical layout)
Column(
  children: [
    Text('First'),
    Text('Second'),
    Text('Third'),
  ],
)

// Row — horizontal stack
Row(
  children: [
    Icon(Icons.star),
    Text('Rated'),
  ],
)

// Stack — overlay children (like CSS position: absolute)
Stack(
  children: [
    Image.network('https://example.com/bg.jpg'),
    Positioned(
      bottom: 16,
      left: 16,
      child: Text('Overlay text'),
    ),
  ],
)

// Expanded — fills remaining space (like flex: 1 in CSS)
Row(
  children: [
    Expanded(child: TextField()),  // Takes all remaining space
    SizedBox(width: 8),
    ElevatedButton(onPressed: () {}, child: Text('Send')),
  ],
)

// ListView — scrollable list
ListView.builder(
  itemCount: items.length,
  itemBuilder: (context, index) => ListTile(
    title: Text(items[index].name),
    subtitle: Text(items[index].description),
  ),
)
```

### Navigation

```dart
// Using Navigator 2.0 with GoRouter (the recommended approach)
// But first, basic Navigator.push:

// Push a new screen
Navigator.push(
  context,
  MaterialPageRoute(builder: (context) => DetailScreen(item: item)),
);

// Pop back
Navigator.pop(context);

// Named routes (simple approach)
MaterialApp(
  routes: {
    '/': (context) => HomeScreen(),
    '/detail': (context) => DetailScreen(),
    '/settings': (context) => SettingsScreen(),
  },
)

// Navigate by name
Navigator.pushNamed(context, '/detail');
```

For production apps, use the `go_router` package or Flutter's built-in `Router` widget with declarative routing.

### State Management with Provider / Riverpod

While `setState` works for simple cases, production apps need a more structured approach. The most common patterns:

**Using ChangeNotifier (built into Flutter):**

```dart
class TodoListModel extends ChangeNotifier {
  final List<Todo> _items = [];
  bool _isLoading = false;
  String? _error;

  List<Todo> get items => List.unmodifiable(_items);
  bool get isLoading => _isLoading;
  String? get error => _error;

  Future<void> loadItems() async {
    _isLoading = true;
    _error = null;
    notifyListeners();

    try {
      final response = await http.get(Uri.parse('https://api.example.com/todos'));
      final data = jsonDecode(response.body) as List;
      _items
        ..clear()
        ..addAll(data.map((json) => Todo.fromJson(json)));
    } catch (e) {
      _error = e.toString();
    } finally {
      _isLoading = false;
      notifyListeners();
    }
  }

  void addItem(String title) {
    _items.add(Todo(id: DateTime.now().millisecondsSinceEpoch.toString(), title: title));
    notifyListeners();
  }

  void toggleItem(String id) {
    final index = _items.indexWhere((item) => item.id == id);
    if (index >= 0) {
      _items[index] = _items[index].copyWith(
        isCompleted: !_items[index].isCompleted,
      );
      notifyListeners();
    }
  }
}
```

**Providing it to the widget tree:**

```dart
void main() {
  runApp(
    ChangeNotifierProvider(
      create: (_) => TodoListModel(),
      child: const MyApp(),
    ),
  );
}
```

**Consuming it in a widget:**

```dart
class TodoListScreen extends StatelessWidget {
  const TodoListScreen({super.key});

  @override
  Widget build(BuildContext context) {
    final model = context.watch<TodoListModel>();

    return Scaffold(
      appBar: AppBar(title: const Text('Todos')),
      body: switch (model) {
        TodoListModel(isLoading: true) => const Center(child: CircularProgressIndicator()),
        TodoListModel(error: final e?) => Center(child: Text('Error: $e')),
        _ => ListView.builder(
          itemCount: model.items.length,
          itemBuilder: (context, index) {
            final item = model.items[index];
            return CheckboxListTile(
              title: Text(item.title),
              value: item.isCompleted,
              onChanged: (_) => model.toggleItem(item.id),
            );
          },
        ),
      },
      floatingActionButton: FloatingActionButton(
        onPressed: () => _showAddDialog(context, model),
        child: const Icon(Icons.add),
      ),
    );
  }
}
```

This `ChangeNotifier` + `Provider` pattern is equivalent to MVVM in Blazor or WPF — the model holds state, notifies listeners when it changes, and the UI rebuilds automatically.

## Part 4: Platform Targets

### Android

Flutter compiles to native ARM/ARM64/x86_64 code on Android via the Dart AOT compiler. The Flutter engine runs as a native library loaded by a thin Java/Kotlin host Activity.

```bash
flutter build apk --release            # Build APK
flutter build appbundle --release      # Build AAB (for Play Store)
```

### iOS

Flutter compiles to native ARM64 code on iOS. The Flutter engine runs within a UIKit host.

```bash
flutter build ios --release
flutter build ipa --release            # Build IPA for App Store
```

### Web

Flutter compiles to JavaScript (using `dart2js`) or WebAssembly (using `dart2wasm`). The UI is rendered either to an HTML canvas (CanvasKit renderer) or to HTML elements (HTML renderer).

```bash
flutter build web --release                    # Default (auto-selects renderer)
flutter build web --release --wasm             # WebAssembly (better performance)
```

### Windows

Flutter uses the Win32 API and ANGLE (for OpenGL ES rendering) on Windows.

```bash
flutter build windows --release
```

### macOS

Flutter uses Metal for rendering on macOS.

```bash
flutter build macos --release
```

### Linux

Flutter uses GTK and OpenGL/Vulkan on Linux.

```bash
flutter build linux --release
```

## Part 5: App Signing

### Android Signing

Create a keystore and configure signing in `android/app/build.gradle.kts`:

```bash
keytool -genkeypair -v -keystore release.jks -keyalg RSA -keysize 2048 \
  -validity 10000 -alias release -storepass YOUR_PASSWORD -keypass YOUR_PASSWORD
```

```kotlin
// android/app/build.gradle.kts
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
        }
    }
}
```

### iOS Signing

iOS signing uses the same certificate + provisioning profile system described in our Swift/iOS article. For CI/CD, you base64-encode the certificate and profile, store them as secrets, and install them on the build machine.

## Part 6: GitHub Actions CI/CD

```yaml
# .github/workflows/build.yml
name: Build Flutter App

on:
  push:
    branches: ['*']
  pull_request:
    branches: [main]

concurrency:
  group: flutter-${{ github.ref }}
  cancel-in-progress: true

jobs:
  test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: subosito/flutter-action@v2
        with:
          flutter-version: '3.41.5'
          channel: stable
          cache: true
      - run: flutter pub get
      - run: dart analyze
      - run: flutter test

  build-android:
    needs: test
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-java@v4
        with:
          distribution: temurin
          java-version: '17'
      - uses: subosito/flutter-action@v2
        with:
          flutter-version: '3.41.5'
          channel: stable
          cache: true

      - name: Decode keystore
        env:
          KEYSTORE_BASE64: ${{ secrets.KEYSTORE_BASE64 }}
        run: echo "$KEYSTORE_BASE64" | base64 --decode > android/app/release.jks

      - name: Build APK
        env:
          KEYSTORE_PATH: release.jks
          KEYSTORE_PASSWORD: ${{ secrets.KEYSTORE_PASSWORD }}
          KEY_ALIAS: ${{ secrets.KEY_ALIAS }}
          KEY_PASSWORD: ${{ secrets.KEY_PASSWORD }}
        run: flutter build apk --release

      - uses: actions/upload-artifact@v4
        with:
          name: android-apk-${{ github.sha }}
          path: build/app/outputs/flutter-apk/app-release.apk

      - name: Clean up
        if: always()
        run: rm -f android/app/release.jks

  build-web:
    needs: test
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: subosito/flutter-action@v2
        with:
          flutter-version: '3.41.5'
          channel: stable
          cache: true
      - run: flutter build web --release --wasm
      - uses: actions/upload-artifact@v4
        with:
          name: web-build-${{ github.sha }}
          path: build/web/

  build-ios:
    needs: test
    runs-on: macos-15
    steps:
      - uses: actions/checkout@v4
      - uses: subosito/flutter-action@v2
        with:
          flutter-version: '3.41.5'
          channel: stable
          cache: true

      - name: Install certificates
        env:
          BUILD_CERTIFICATE_BASE64: ${{ secrets.BUILD_CERTIFICATE_BASE64 }}
          P12_PASSWORD: ${{ secrets.P12_PASSWORD }}
          BUILD_PROVISION_PROFILE_BASE64: ${{ secrets.BUILD_PROVISION_PROFILE_BASE64 }}
          KEYCHAIN_PASSWORD: ${{ secrets.KEYCHAIN_PASSWORD }}
        run: |
          CERTIFICATE_PATH=$RUNNER_TEMP/build_certificate.p12
          PP_PATH=$RUNNER_TEMP/build_pp.mobileprovision
          KEYCHAIN_PATH=$RUNNER_TEMP/app-signing.keychain-db
          echo -n "$BUILD_CERTIFICATE_BASE64" | base64 --decode -o $CERTIFICATE_PATH
          echo -n "$BUILD_PROVISION_PROFILE_BASE64" | base64 --decode -o $PP_PATH
          security create-keychain -p "$KEYCHAIN_PASSWORD" $KEYCHAIN_PATH
          security set-keychain-settings -lut 21600 $KEYCHAIN_PATH
          security unlock-keychain -p "$KEYCHAIN_PASSWORD" $KEYCHAIN_PATH
          security import $CERTIFICATE_PATH -P "$P12_PASSWORD" -A -t cert -f pkcs12 -k $KEYCHAIN_PATH
          security set-key-partition-list -S apple-tool:,apple: -k "$KEYCHAIN_PASSWORD" $KEYCHAIN_PATH
          security list-keychain -d user -s $KEYCHAIN_PATH
          mkdir -p ~/Library/MobileDevice/Provisioning\ Profiles
          cp $PP_PATH ~/Library/MobileDevice/Provisioning\ Profiles

      - run: flutter build ipa --release --export-options-plist=ios/ExportOptions.plist

      - uses: actions/upload-artifact@v4
        with:
          name: ios-ipa-${{ github.sha }}
          path: build/ios/ipa/*.ipa

      - name: Clean up
        if: always()
        run: security delete-keychain $RUNNER_TEMP/app-signing.keychain-db || true
```

This workflow runs tests on Linux (fast, cheap), then builds Android, web, and iOS in parallel. Each platform build uploads its artifact for download.

## Part 7: Testing

### Unit Tests

```dart
// test/models/todo_test.dart
import 'package:test/test.dart';
import 'package:my_app/models/todo.dart';

void main() {
  group('Todo', () {
    test('creates with default values', () {
      final todo = Todo(id: '1', title: 'Test');
      expect(todo.isCompleted, isFalse);
    });

    test('copyWith creates modified copy', () {
      final todo = Todo(id: '1', title: 'Test');
      final completed = todo.copyWith(isCompleted: true);
      expect(completed.isCompleted, isTrue);
      expect(completed.title, equals('Test'));
      expect(todo.isCompleted, isFalse);  // Original unchanged
    });
  });
}
```

### Widget Tests

```dart
// test/widgets/counter_test.dart
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:my_app/widgets/counter.dart';

void main() {
  testWidgets('Counter increments on button press', (tester) async {
    await tester.pumpWidget(const MaterialApp(home: Counter()));

    expect(find.text('Count: 0'), findsOneWidget);

    await tester.tap(find.text('Increment'));
    await tester.pump();

    expect(find.text('Count: 1'), findsOneWidget);
  });

  testWidgets('Counter displays correct initial value', (tester) async {
    await tester.pumpWidget(const MaterialApp(home: Counter()));
    expect(find.text('Count: 0'), findsOneWidget);
    expect(find.text('Count: 1'), findsNothing);
  });
}
```

`pumpWidget` renders the widget. `pump` processes a frame (equivalent to waiting for re-render). `tap` simulates a user tap. `find.text` locates widgets by their text content. This is equivalent to bUnit testing in Blazor.

## Part 8: Bad Code vs. Good Code

### Bad: setState Everything

```dart
// BAD — business logic mixed into UI
class _OrderScreenState extends State<OrderScreen> {
  List<Order> orders = [];
  bool loading = false;

  @override
  void initState() {
    super.initState();
    loading = true;
    fetchOrders().then((data) {
      setState(() {
        orders = data;
        loading = false;
      });
    }).catchError((e) {
      setState(() { loading = false; });
    });
  }

  // ... 200 more lines of setState calls
}
```

```dart
// GOOD — separate business logic from UI
class OrderModel extends ChangeNotifier {
  List<Order> _orders = [];
  bool _isLoading = false;

  List<Order> get orders => List.unmodifiable(_orders);
  bool get isLoading => _isLoading;

  Future<void> loadOrders() async {
    _isLoading = true;
    notifyListeners();
    try {
      _orders = await _repository.fetchOrders();
    } finally {
      _isLoading = false;
      notifyListeners();
    }
  }
}
```

### Bad: Deeply Nested Widget Trees

```dart
// BAD — pyramid of doom
Widget build(BuildContext context) {
  return Scaffold(
    body: Padding(
      padding: EdgeInsets.all(16),
      child: Column(
        children: [
          Container(
            decoration: BoxDecoration(/* ... */),
            child: Row(
              children: [
                Expanded(
                  child: Column(
                    children: [
                      // 15 levels deep...
```

```dart
// GOOD — extract methods and widgets
Widget build(BuildContext context) {
  return Scaffold(body: Padding(
    padding: const EdgeInsets.all(16),
    child: Column(children: [_header(), _body(), _footer()]),
  ));
}

Widget _header() => /* ... */;
Widget _body() => /* ... */;
Widget _footer() => /* ... */;
```

### Bad: Not Using const

```dart
// BAD — rebuilds child widgets unnecessarily
ListView.builder(
  itemBuilder: (context, index) => Padding(
    padding: EdgeInsets.all(8),  // New EdgeInsets every build
    child: Icon(Icons.star),     // New Icon every build
  ),
)
```

```dart
// GOOD — const prevents unnecessary rebuilds
ListView.builder(
  itemBuilder: (context, index) => const Padding(
    padding: EdgeInsets.all(8),
    child: Icon(Icons.star),
  ),
)
```

`const` widgets are created once and reused. Flutter's framework skips rebuilding const subtrees entirely.

## Part 9: Resources

- **Dart Language Tour**: https://dart.dev/language — Dart 3.11 is the current stable (February 2026)
- **Flutter Documentation**: https://docs.flutter.dev — Flutter 3.41.5 is the current stable (February 2026)
- **Flutter Cookbook**: https://docs.flutter.dev/cookbook
- **Dart Packages**: https://pub.dev
- **Flutter Widget Catalog**: https://docs.flutter.dev/ui/widgets
- **Flutter Samples**: https://flutter.github.io/samples/
- **Effective Dart**: https://dart.dev/effective-dart — Style, documentation, usage, and design guidelines
- **Flutter YouTube Channel**: https://www.youtube.com/flutterdev
- **Flutter Community Medium**: https://medium.com/flutter
- **Dart Blog**: https://blog.dart.dev — Dart 3.11 announcement
- **Flutter GitHub**: https://github.com/flutter/flutter
- **Dart GitHub**: https://github.com/dart-lang/sdk

Every version number was verified via web search at the time of writing (April 2026).

---

You started this article knowing C# and ASP.NET. You now understand Dart — its type system, null safety, async model, isolates, and collections. You understand Flutter — widgets, state management, layout, navigation, and the Impeller rendering engine. You understand how to build for Android, iOS, web, Windows, macOS, and Linux from a single codebase. You understand app signing for both mobile platforms. You have a complete CI/CD pipeline that tests, builds, and packages your app for every target.

Flutter's promise is real: one codebase, every platform, native performance. The cost is learning a new language (Dart, though its similarity to C# makes this modest) and accepting that your UI will not use native platform widgets (which is a feature, not a bug, if you want pixel-perfect consistency). For teams that need to ship on multiple platforms without multiplying their engineering headcount, Flutter is the most practical choice available in 2026.

Go build something. Ship it everywhere.
