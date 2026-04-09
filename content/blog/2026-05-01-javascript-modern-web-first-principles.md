---
title: "JavaScript for the Modern Web in 2026: Complete Web Applications from First Principles Without npm, Without Build Steps, Without Nonsense"
date: 2026-05-01
author: observer-team
summary: "A comprehensive, from-first-principles guide to building complete web applications using only what ships in evergreen browsers — vanilla JavaScript (ES2026), pure CSS, Web Components, and the Fetch API. No npm. No Node.js. No build step. No Sass. No vendor prefixes. Covers the Temporal API, Explicit Resource Management, container queries, cascade layers, native CSS nesting, shadow DOM, client-side routing, state management, and full application architecture — with extensive code examples, case studies, and honest comparisons to framework-heavy alternatives."
tags:
  - javascript
  - css
  - web-components
  - deep-dive
  - best-practices
  - performance
  - architecture
  - tutorial
  - opinion
---

There is a mass delusion in the web development industry, and it has persisted for the better part of a decade. The delusion goes like this: you cannot build a real web application without npm, without a bundler, without a transpiler, without a CSS preprocessor, without a framework, without a state management library, without a routing library, without a form validation library, without a date-time library, and without at least forty-seven other packages whose names you will forget by next Tuesday.

This is not true. It was not true in 2020. It was even less true in 2023. And in 2026, with ECMAScript 2026 finalized and shipping in browsers, with CSS that would have seemed like science fiction five years ago, and with Web Components that actually work — it is so spectacularly, demonstrably untrue that continuing to believe it constitutes a kind of professional negligence.

This article is for the ASP.NET developer who opens a frontend project and finds a `package.json` with 1,847 dependencies, a `webpack.config.js` that would make a tax attorney weep, and a build step that takes longer than compiling the entire .NET runtime. This article is for the developer who has been told, repeatedly, that this is normal. That this is how things are done. That you need all of this.

You do not.

We are going to build complete web applications — with routing, state management, components, data fetching, forms, validation, date-time handling, and responsive layouts — using nothing except what ships in Chrome, Firefox, and Safari today. No npm. No Node.js. No build step. No Sass. No SCSS. No PostCSS. No Tailwind. No vendor prefixes. If it requires `-webkit-` or `-moz-`, it does not exist for our purposes. If it only works in Chrome, we will use it as an example of bad code, not good code.

We go full Not Invented Here — except everything we use was, in fact, invented here, inside the browser, by the standards bodies and engine teams who have spent the last decade making this possible.

Let us begin.

## Part 1: The Case for Going Vanilla

### The Cost of the Status Quo

Picture this scenario. It is a Wednesday morning. You are an ASP.NET developer. Your team has decided to add a small interactive feature to a page — a date picker that respects time zones. A reasonable request.

Your frontend colleague tells you that you need to install Moment.js. Or date-fns. Or Luxon. You need npm, which means you need Node.js. You need a bundler — Webpack, or Vite, or esbuild, or Turbopack, or Rspack, or whichever one the JavaScript community has decided is correct this month. You need a `package.json`. You need a `package-lock.json` (or a `yarn.lock`, or a `pnpm-lock.yaml`, depending on which package manager is in vogue). You need a configuration file for the bundler. You may need Babel for transpilation. You may need PostCSS for your CSS. You may need TypeScript, which needs `tsconfig.json`. By the time you have a working date picker, your project has acquired 200 megabytes of `node_modules/`, a build step that takes eighteen seconds, and a supply chain that includes code written by people you will never meet, reviewed by nobody, and downloaded from a registry that has been compromised multiple times.

All because you wanted to pick a date.

This is not an exaggeration. This is the reality of mainstream frontend development in 2026, and it is insane.

### What Changed

Three things happened between 2018 and 2026 that made the npm-industrial complex unnecessary for most web applications.

First, JavaScript itself grew up. ECMAScript 2026 includes the Temporal API — a complete, immutable, time-zone-aware date-time system that makes Moment.js, date-fns, and Luxon completely redundant for the vast majority of use cases. It includes Explicit Resource Management (the `using` keyword), which brings deterministic cleanup to JavaScript for the first time. It includes immutable array methods (`toReversed()`, `toSorted()`, `toSpliced()`, `with()`), Set operations (`union()`, `intersection()`, `difference()`), `Promise.try()`, `RegExp.escape()`, and `Float16Array`. JavaScript in 2026 is not the JavaScript of 2016.

Second, CSS became a real programming language. Container queries let components respond to their own size, not the viewport. Cascade layers (`@layer`) eliminated specificity wars. Native nesting removed the primary reason people used Sass. The `:has()` pseudo-class gave CSS the ability to style parents based on children — something that required JavaScript for two decades. `color-mix()` and `oklch()` made color systems trivial. `text-box-trim` fixed vertical alignment. Scroll-driven animations replaced entire JavaScript libraries. CSS in 2026 can do things that required a JavaScript framework in 2020.

Third, Web Components matured. Custom Elements, Shadow DOM, and Declarative Shadow DOM are all baseline-supported across every evergreen browser. You can build encapsulated, reusable components with scoped styles and custom APIs, and you can do it without React, without Vue, without Angular, without Svelte, without Solid, without Lit, without any framework whatsoever.

### The Evergreen Browser Principle

Throughout this article, we follow one iron rule: a feature must be supported in the current stable versions of Google Chrome, Mozilla Firefox, and Apple Safari. If it only works in Chrome, it is not a feature — it is a Chrome experiment, and we treat it the same way we would treat a compiler extension in C++: interesting to know about, dangerous to rely on, and absolutely not something we ship to production.

We do not use vendor prefixes. If a CSS property requires `-webkit-` or `-moz-`, that property is not ready. We mention these properties only to explain why you should not use them.

We do not support Internet Explorer. We do not support legacy Edge (EdgeHTML). We do not support any browser that has been dead for years. The audience for this article is developers building applications for the modern web, not archaeologists maintaining compatibility layers for browsers that Microsoft itself has abandoned.

### Why This Matters for .NET Developers

If you are an ASP.NET developer — and if you are reading Observer Magazine, you probably are — this matters to you for three reasons.

First, Blazor WebAssembly already proves that you can build serious web applications without npm. The Observer Magazine website you are reading right now is built in Blazor WASM, deployed to GitHub Pages, with zero JavaScript build steps. But sometimes you need to write JavaScript — for interop, for a standalone feature, for a microsite, or for a project where Blazor is overkill. When you do, you should know that you can write that JavaScript the same way you write C#: with standard libraries, without a package manager, and without a build step.

Second, understanding what the web platform can do natively makes you a better Blazor developer. When you know that CSS container queries exist, you stop writing C# code to toggle CSS classes based on viewport width. When you know that the Temporal API exists, you stop serializing `DateTimeOffset` into a format that a JavaScript date library can parse. The platform is the foundation under everything, Blazor included.

Third, supply chain security. Every npm package is a liability. The .NET ecosystem learned this lesson with the left-pad incident's cousin problems. The fewer dependencies you have, the smaller your attack surface. In an enterprise environment, "we have zero JavaScript dependencies" is not just a flex — it is a security posture.

## Part 2: JavaScript in 2026 — The Language

### ES Modules: No Bundler Required

Let us start with the most fundamental shift: you do not need a bundler. Modern browsers understand ES modules natively. You can use `import` and `export` directly in `<script type="module">` tags, and the browser handles everything.

```html
<!-- index.html -->
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="utf-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>My Application</title>
    <link rel="stylesheet" href="css/app.css">
</head>
<body>
    <div id="app"></div>
    <script type="module" src="js/main.js"></script>
</body>
</html>
```

```javascript
// js/main.js
import { Router } from './router.js';
import { AppState } from './state.js';
import { renderHeader } from './components/header.js';

const state = new AppState();
const router = new Router(document.getElementById('app'));

renderHeader(document.body, state);
router.start();
```

```javascript
// js/router.js
export class Router {
    #outlet;
    #routes = new Map();

    constructor(outlet) {
        this.#outlet = outlet;
        window.addEventListener('popstate', () => this.#resolve());
        document.addEventListener('click', (e) => {
            const anchor = e.target.closest('a[data-route]');
            if (anchor) {
                e.preventDefault();
                this.navigate(anchor.getAttribute('href'));
            }
        });
    }

    route(path, handler) {
        this.#routes.set(path, handler);
        return this;
    }

    navigate(path) {
        history.pushState(null, '', path);
        this.#resolve();
    }

    start() {
        this.#resolve();
    }

    #resolve() {
        const path = location.pathname;
        for (const [pattern, handler] of this.#routes) {
            const match = this.#match(pattern, path);
            if (match) {
                handler(this.#outlet, match.params);
                return;
            }
        }
    }

    #match(pattern, path) {
        const patternParts = pattern.split('/');
        const pathParts = path.split('/');
        if (patternParts.length !== pathParts.length) return null;

        const params = {};
        for (let i = 0; i < patternParts.length; i++) {
            if (patternParts[i].startsWith(':')) {
                params[patternParts[i].slice(1)] = pathParts[i];
            } else if (patternParts[i] !== pathParts[i]) {
                return null;
            }
        }
        return { params };
    }
}
```

That is a complete client-side router. Fifty-some lines of JavaScript. No library. No npm install. It handles path parameters, the browser back button, and declarative navigation via `data-route` attributes. It uses private class fields (`#outlet`, `#routes`) — a feature that has been baseline-supported since 2021.

Here is what the bad version looks like — the version that requires npm:

```bash
# BAD: The npm way
npm init -y
npm install react react-dom react-router-dom
# Downloads 47 packages, 14 MB of node_modules
# Now you need a bundler too
npm install --save-dev vite @vitejs/plugin-react
# Now you need configuration files
# Now you need a build step
# Now you need to understand JSX transpilation
# All to do what the browser can do natively
```

Do not do this. The browser already has `history.pushState()`. The browser already has `addEventListener('popstate')`. The browser already has `import` and `export`. You do not need React Router. You do not need Vue Router. You do not need a 14-megabyte `node_modules` folder to handle URL changes.

### `let`, `const`, and Why `var` is Dead

If you are coming from older JavaScript or from ASP.NET Web Forms where inline script blocks used `var` everywhere, here is the rule: never use `var`. Ever. It is not deprecated in the specification — JavaScript maintains backward compatibility religiously — but it is deprecated in the minds of every competent JavaScript developer.

The problem with `var` is that it is function-scoped, not block-scoped. This leads to bugs that are subtle, maddening, and entirely preventable:

```javascript
// BAD: var is function-scoped
for (var i = 0; i < 5; i++) {
    setTimeout(() => console.log(i), 100);
}
// Prints: 5, 5, 5, 5, 5
// Because there is only ONE i, and it is 5 by the time the timeouts fire

// GOOD: let is block-scoped
for (let i = 0; i < 5; i++) {
    setTimeout(() => console.log(i), 100);
}
// Prints: 0, 1, 2, 3, 4
// Because each iteration gets its own i
```

Use `const` for values that will not be reassigned. Use `let` for values that will be reassigned. Use `var` for nothing.

```javascript
// GOOD
const API_URL = 'https://api.example.com';
const fetchData = async (endpoint) => {
    const response = await fetch(`${API_URL}/${endpoint}`);
    const data = await response.json();
    return data;
};

let currentPage = 1;
let isLoading = false;
```

Note that `const` does not make the value immutable — it makes the binding immutable. A `const` object can still have its properties changed. A `const` array can still be pushed to. If you want true immutability, you use `Object.freeze()` — or, better yet, the new immutable array methods that ECMAScript 2026 provides, which we will cover shortly.

### Arrow Functions and `this`

Arrow functions are not just shorter syntax. They have a fundamentally different relationship with `this`. A traditional function creates its own `this` context. An arrow function inherits `this` from its enclosing scope. This distinction matters enormously when writing event handlers and callbacks:

```javascript
// BAD: Traditional function loses `this` context
class Counter {
    constructor() {
        this.count = 0;
        this.button = document.createElement('button');
        this.button.addEventListener('click', function() {
            this.count++; // BUG: `this` is the button element, not the Counter
            console.log(this.count); // NaN
        });
    }
}

// GOOD: Arrow function preserves `this` context
class Counter {
    count = 0;

    constructor() {
        this.button = document.createElement('button');
        this.button.addEventListener('click', () => {
            this.count++; // Correct: `this` is the Counter instance
            console.log(this.count); // 1, 2, 3, ...
        });
    }
}
```

If you are a C# developer, think of arrow functions as analogous to C# lambda expressions with captured variables. The `this` in a JavaScript arrow function behaves like the captured `this` in a C# lambda inside a method — it always refers to the enclosing instance.

### Template Literals

Template literals (backtick strings) are one of those features that seem minor until you use them for a week and realize you can never go back to string concatenation:

```javascript
// BAD: String concatenation
const greeting = 'Hello, ' + user.name + '! You have ' + user.messages + ' messages.';

// GOOD: Template literal
const greeting = `Hello, ${user.name}! You have ${user.messages} messages.`;

// GOOD: Multi-line strings
const html = `
    <article class="blog-card">
        <h2>${post.title}</h2>
        <time datetime="${post.date}">${formatDate(post.date)}</time>
        <p>${post.summary}</p>
    </article>
`;

// GOOD: Tagged templates for escaping
function html(strings, ...values) {
    return strings.reduce((result, str, i) => {
        const value = i < values.length ? escapeHtml(String(values[i])) : '';
        return result + str + value;
    }, '');
}

function escapeHtml(str) {
    return str
        .replace(/&/g, '&amp;')
        .replace(/</g, '&lt;')
        .replace(/>/g, '&gt;')
        .replace(/"/g, '&quot;');
}

// Safe from XSS
const card = html`<div class="user">${userInput}</div>`;
```

That tagged template function at the end is worth studying. It gives you XSS protection without a framework. No React. No DOMPurify. Just a function that intercepts template literal interpolation and escapes the values. This pattern is how you build safe HTML rendering from first principles.

### Destructuring and Spread

Destructuring lets you unpack values from objects and arrays into distinct variables. If you know C# deconstruction patterns, this will feel familiar:

```javascript
// Object destructuring
const { name, email, role = 'user' } = await fetchUser(userId);

// Array destructuring
const [first, second, ...rest] = items;

// Function parameter destructuring
function createUser({ name, email, role = 'user' }) {
    return { id: crypto.randomUUID(), name, email, role };
}

// Spread operator for shallow copies (immutable patterns)
const original = { a: 1, b: 2, c: 3 };
const updated = { ...original, b: 42 }; // { a: 1, b: 42, c: 3 }

const arr = [1, 2, 3];
const extended = [...arr, 4, 5]; // [1, 2, 3, 4, 5]
```

The spread operator is particularly important because it enables immutable update patterns without any library. When you are managing application state (and we will build a complete state management system later in this article), you never mutate state directly. You always create new objects with the changes applied. The spread operator makes this ergonomic.

### Async/Await and Fetch

If you have written C# with `async`/`await` and `HttpClient`, JavaScript's equivalent will feel immediately familiar. The `fetch` API is built into every browser and handles HTTP requests without any library:

```javascript
// Basic GET request
async function loadPosts() {
    const response = await fetch('/api/posts');
    if (!response.ok) {
        throw new Error(`HTTP ${response.status}: ${response.statusText}`);
    }
    return response.json();
}

// POST with JSON body
async function createPost(post) {
    const response = await fetch('/api/posts', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(post),
    });
    if (!response.ok) {
        const error = await response.json().catch(() => ({}));
        throw new Error(error.message || `HTTP ${response.status}`);
    }
    return response.json();
}

// With AbortController for cancellation
async function searchWithTimeout(query, timeoutMs = 5000) {
    const controller = new AbortController();
    const timeoutId = setTimeout(() => controller.abort(), timeoutMs);

    try {
        const response = await fetch(`/api/search?q=${encodeURIComponent(query)}`, {
            signal: controller.signal,
        });
        return response.json();
    } finally {
        clearTimeout(timeoutId);
    }
}
```

The `AbortController` pattern is the JavaScript equivalent of `CancellationToken` in C#. You create a controller, pass its signal to `fetch`, and call `abort()` when you want to cancel the request. This works for timeouts, for cancelling in-flight requests when the user navigates away, and for debouncing search inputs.

Notice what we did not do: we did not install Axios. We did not install `node-fetch`. We did not install `isomorphic-fetch`. The `fetch` API is built into the browser. It handles everything Axios handles, minus the automatic JSON transformation (which is one line: `response.json()`) and the interceptors (which you can build yourself with a wrapper function).

### ECMAScript 2026: The Features That Matter

ECMAScript 2026 was finalized in December 2025 and is now shipping in browsers. Here are the features that change how you write JavaScript.

#### Temporal API: Dates and Times That Actually Work

The Temporal API is, by many measures, the single largest addition to JavaScript ever. It replaces the `Date` object — a disaster that was hastily copied from Java's `java.util.Date` in 1995, a class that Java itself deprecated in 1997. For thirty years, JavaScript developers have suffered with `Date`. Those days are over.

As of April 2026, Temporal has reached TC39 Stage 4 and is part of the ES2026 specification. It ships natively in Firefox 139 (since May 2025), Chrome 144 (since January 2026), and Edge 144. Safari support is in development in Safari Technology Preview. For production use today, a polyfill is available, but the end of the polyfill era is in sight.

Here is why `Date` is terrible and `Temporal` is not:

```javascript
// BAD: Date is mutable, confusing, and buggy
const d = new Date('2026-01-31');
d.setMonth(d.getMonth() + 1);
console.log(d.toDateString()); // "Tue Mar 03 2026"
// You asked for February 31st. Date silently overflowed to March 3rd.
// This is not a feature. This is a bug in the specification.

// GOOD: Temporal is immutable, explicit, and correct
const date = Temporal.PlainDate.from('2026-01-31');
const nextMonth = date.add({ months: 1 });
console.log(nextMonth.toString()); // "2026-02-28"
// Temporal clamps to the last day of the month. No silent overflow.
```

Temporal provides distinct types for distinct concepts:

```javascript
// A date without a time (like a birthday)
const birthday = Temporal.PlainDate.from('1990-06-15');

// A time without a date (like a meeting time)
const meetingTime = Temporal.PlainTime.from('14:30:00');

// A date and time without a time zone (like a calendar event)
const appointment = Temporal.PlainDateTime.from('2026-05-01T14:30:00');

// A date and time WITH a time zone (like a flight departure)
const departure = Temporal.ZonedDateTime.from({
    year: 2026,
    month: 5,
    day: 1,
    hour: 14,
    minute: 30,
    timeZone: 'America/New_York',
});

// Current time
const now = Temporal.Now.zonedDateTimeISO();
console.log(now.toString());
// "2026-05-01T10:30:00-04:00[America/New_York]"

// Duration arithmetic
const duration = Temporal.Duration.from({ hours: 2, minutes: 30 });
const later = now.add(duration);

// Comparison (no more getTime() hacks)
const a = Temporal.PlainDate.from('2026-01-01');
const b = Temporal.PlainDate.from('2026-06-15');
console.log(Temporal.PlainDate.compare(a, b)); // -1 (a is before b)

// Difference between dates
const diff = a.until(b);
console.log(diff.toString()); // "P165D" (165 days)
console.log(diff.total('days')); // 165
```

For ASP.NET developers: `Temporal.PlainDate` is like `DateOnly` in .NET. `Temporal.PlainTime` is like `TimeOnly`. `Temporal.ZonedDateTime` is like `DateTimeOffset` (but better, because it carries the actual time zone name, not just an offset). `Temporal.Duration` is like `TimeSpan`.

This is the feature that kills Moment.js. Moment.js (and its descendants date-fns, Luxon, Day.js) collectively have over 100 million weekly npm downloads. Every one of those downloads is now unnecessary for new projects targeting evergreen browsers.

**Important caveat for our evergreen-only rule**: Safari does not yet ship Temporal by default. It is available in Safari Technology Preview behind a flag. This means Temporal is not yet baseline-available across all three engines. For production code today, you have two choices: use the polyfill (which adds roughly 25KB gzipped — still far smaller than Moment.js), or wait for Safari to ship it, which is expected in the first half of 2026. We include Temporal in this article because it is so close to universal availability that excluding it would be negligent, but we are honest about the gap.

#### Immutable Array Methods

Before ES2026, many array methods mutated the original array. This was a constant source of bugs in any codebase that tried to maintain immutable state:

```javascript
// BAD: sort() mutates the original array
const scores = [3, 1, 4, 1, 5, 9];
const sorted = scores.sort((a, b) => a - b);
console.log(scores); // [1, 1, 3, 4, 5, 9] — original is destroyed!
console.log(sorted === scores); // true — it's the same array!

// GOOD: toSorted() returns a new array
const scores = [3, 1, 4, 1, 5, 9];
const sorted = scores.toSorted((a, b) => a - b);
console.log(scores); // [3, 1, 4, 1, 5, 9] — original untouched
console.log(sorted); // [1, 1, 3, 4, 5, 9] — new array

// toReversed() — immutable reverse
const reversed = scores.toReversed(); // [9, 5, 1, 4, 1, 3]

// toSpliced() — immutable splice
const items = ['a', 'b', 'c', 'd'];
const modified = items.toSpliced(1, 1, 'x', 'y'); // ['a', 'x', 'y', 'c', 'd']

// with() — immutable index assignment
const updated = items.with(2, 'z'); // ['a', 'b', 'z', 'd']
```

These methods are baseline-available in all evergreen browsers since mid-2023. There is no excuse not to use them. If you are still calling `.sort()` and hoping nobody notices the mutation, stop.

#### Set Operations

Before ES2026, if you wanted the union of two sets, you wrote a loop. Now:

```javascript
const frontend = new Set(['html', 'css', 'javascript', 'typescript']);
const backend = new Set(['csharp', 'javascript', 'sql', 'typescript']);

// Union
const all = frontend.union(backend);
// Set {'html', 'css', 'javascript', 'typescript', 'csharp', 'sql'}

// Intersection
const shared = frontend.intersection(backend);
// Set {'javascript', 'typescript'}

// Difference
const frontendOnly = frontend.difference(backend);
// Set {'html', 'css'}

// Symmetric difference
const exclusive = frontend.symmetricDifference(backend);
// Set {'html', 'css', 'csharp', 'sql'}

// Subset/superset checks
console.log(shared.isSubsetOf(frontend)); // true
console.log(frontend.isSupersetOf(shared)); // true
console.log(frontend.isDisjointFrom(backend)); // false
```

For C# developers, these are exactly like the `HashSet<T>` methods you already know: `UnionWith`, `IntersectWith`, `ExceptWith`, `SymmetricExceptWith`, `IsSubsetOf`, `IsSupersetOf`. JavaScript finally has parity.

#### Explicit Resource Management

The `using` keyword brings deterministic resource disposal to JavaScript, analogous to `using` statements in C#:

```javascript
// The Symbol.dispose protocol
class DatabaseConnection {
    #connection;

    constructor(connectionString) {
        this.#connection = openConnection(connectionString);
        console.log('Connection opened');
    }

    query(sql) {
        return this.#connection.execute(sql);
    }

    [Symbol.dispose]() {
        this.#connection.close();
        console.log('Connection closed');
    }
}

// Using it — connection is automatically disposed at end of block
function getUsers() {
    using conn = new DatabaseConnection('mydb');
    return conn.query('SELECT * FROM users');
    // conn[Symbol.dispose]() is called here, even if query throws
}

// Async version with Symbol.asyncDispose
class FileHandle {
    [Symbol.asyncDispose]() {
        return this.flush().then(() => this.close());
    }
}

async function processFile() {
    await using file = await openFile('data.csv');
    const contents = await file.read();
    // file is flushed and closed here
}
```

This shipped in Chrome and Firefox, with Safari support coming. It eliminates an entire category of resource leaks — unclosed connections, unremoved event listeners, unreleased locks — by making cleanup automatic and deterministic, the same way C#'s `IDisposable` and `using` statements have done for over two decades.

#### Promise.try

`Promise.try()` wraps a function call so that both synchronous exceptions and asynchronous rejections are handled uniformly:

```javascript
// BAD: Synchronous exception escapes the promise chain
function loadData(source) {
    if (!source) throw new Error('Source is required'); // This throws synchronously!
    return fetch(source).then(r => r.json());
}

// If source is null, this throws instead of rejecting the promise
loadData(null).catch(handleError); // UNCAUGHT! The catch never fires

// GOOD: Promise.try catches everything
function loadData(source) {
    return Promise.try(() => {
        if (!source) throw new Error('Source is required');
        return fetch(source).then(r => r.json());
    });
}

loadData(null).catch(handleError); // Works! The error is caught
```

#### RegExp.escape

You no longer need a utility function to escape strings for use in regular expressions:

```javascript
// BAD: Hand-rolled regex escaping (error-prone)
function escapeRegex(str) {
    return str.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
}

// GOOD: Built-in
const userInput = 'price: $5.00 (USD)';
const escaped = RegExp.escape(userInput);
const regex = new RegExp(escaped);
// Correctly matches the literal string, special characters and all
```

## Part 3: CSS in 2026 — No Sass, No PostCSS, No Vendor Prefixes

### Why Sass is Dead

Sass (and SCSS, and Less) existed because CSS was missing features. CSS did not have variables. CSS did not have nesting. CSS did not have color functions. CSS did not have math functions. So preprocessors filled the gap.

Every one of those gaps is now filled by CSS itself:

| Feature | Sass Syntax | Native CSS (2026) |
|---------|------------|-------------------|
| Variables | `$primary: #2563eb` | `--primary: #2563eb` |
| Nesting | `.card { .title { ... } }` | `.card { .title { ... } }` |
| Color functions | `darken($color, 10%)` | `color-mix(in oklch, var(--color), black 10%)` |
| Math | `$width / 3` | `calc(var(--width) / 3)` |
| Mixins | `@mixin responsive { ... }` | `@layer`, container queries |
| Loops | `@for $i from 1 through 12 { ... }` | Not needed — use `repeat()` in grid |

The only Sass feature without a direct CSS equivalent is the `@for` loop for generating utility classes. If your architecture depends on generating 400 utility classes in a loop, your architecture is the problem, not the absence of `@for`.

### Native CSS Nesting

CSS nesting is baseline-available in all evergreen browsers since December 2023. It works almost exactly like Sass nesting:

```css
/* Native CSS nesting — no preprocessor needed */
.blog-card {
    border: 1px solid var(--color-border);
    border-radius: 0.5rem;
    padding: 1.5rem;

    & .title {
        font-size: 1.25rem;
        font-weight: 600;
        color: var(--color-text);
    }

    & .meta {
        font-size: 0.875rem;
        color: var(--color-muted);
    }

    &:hover {
        box-shadow: 0 0.25rem 0.5rem rgba(0, 0, 0, 0.1);
    }

    @media (min-width: 48rem) {
        display: grid;
        grid-template-columns: 1fr 2fr;
        gap: 1rem;
    }
}
```

The `&` is required when nesting selectors that do not start with a symbol (like a class or pseudo-class). This is the one difference from Sass nesting, and it makes the syntax unambiguous for the CSS parser.

### Custom Properties (CSS Variables)

CSS custom properties are more powerful than Sass variables because they cascade, they can be changed at runtime, and they can be scoped to any element:

```css
/* Design tokens — defined once, used everywhere */
:root {
    --color-bg: #ffffff;
    --color-text: #1a1a2e;
    --color-primary: #2563eb;
    --color-surface: #f3f4f6;
    --color-border: #e5e7eb;
    --font-sans: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, sans-serif;
    --font-mono: "SFMono-Regular", Consolas, "Liberation Mono", monospace;
    --radius: 0.375rem;
    --max-width: 60rem;
}

/* Dark theme — just override the tokens */
[data-theme="dark"] {
    --color-bg: #0f172a;
    --color-text: #e2e8f0;
    --color-primary: #60a5fa;
    --color-surface: #1e293b;
    --color-border: #334155;
}

/* Components use tokens, never hardcoded values */
body {
    background: var(--color-bg);
    color: var(--color-text);
    font-family: var(--font-sans);
}

.btn-primary {
    background: var(--color-primary);
    color: var(--color-bg);
    border: none;
    border-radius: var(--radius);
    padding: 0.5rem 1rem;
}
```

Theme switching becomes trivial JavaScript:

```javascript
function setTheme(theme) {
    document.documentElement.setAttribute('data-theme', theme);
    localStorage.setItem('theme', theme);
}
```

No framework. No CSS-in-JS library. No build step. One attribute change, and every component on the page updates instantly because CSS custom properties cascade.

### Container Queries: Components That Know Their Own Size

Container queries are, alongside CSS nesting, the most important CSS feature of the decade. They have been baseline-available since February 2023 and baseline-widely-available since 2025.

The problem with media queries is that they respond to the viewport size, not the component's container size. A card component in a narrow sidebar and a card component in a wide content area both see the same viewport width, but they need completely different layouts.

```css
/* Declare a containment context */
.card-container {
    container-type: inline-size;
}

/* Default: vertical stack */
.card {
    display: flex;
    flex-direction: column;
    gap: 0.75rem;
}

/* When the container is wide enough: horizontal layout */
@container (min-width: 30rem) {
    .card {
        flex-direction: row;
        align-items: flex-start;
    }

    .card .image {
        flex: 0 0 40%;
    }

    .card .content {
        flex: 1;
    }
}

/* Even wider: add more detail */
@container (min-width: 50rem) {
    .card .meta {
        display: flex;
        gap: 1rem;
    }
}
```

This card component adapts to its container automatically. Drop it in a sidebar? It stacks vertically. Drop it in a main content area? It goes horizontal. Drop it in a full-width hero section? It shows extra detail. All without JavaScript, without media queries based on the viewport, and without any framework.

Container query length units (`cqi`, `cqb`) let you size things relative to the container:

```css
@container (min-width: 20rem) {
    .card .title {
        font-size: clamp(1rem, 4cqi, 2rem);
    }
}
```

### Cascade Layers

Cascade layers (`@layer`) solve the specificity wars that have plagued CSS since its inception. You declare the order of your layers, and styles in later layers always beat styles in earlier layers, regardless of specificity:

```css
/* Declare layer order — last wins */
@layer reset, base, components, utilities;

@layer reset {
    *, *::before, *::after {
        box-sizing: border-box;
        margin: 0;
        padding: 0;
    }
}

@layer base {
    body {
        font-family: var(--font-sans);
        line-height: 1.75;
        color: var(--color-text);
        background: var(--color-bg);
    }

    a {
        color: var(--color-primary);
        text-decoration: none;
    }
}

@layer components {
    .btn {
        display: inline-flex;
        align-items: center;
        padding: 0.5rem 1rem;
        border-radius: var(--radius);
        font-weight: 500;
    }
}

@layer utilities {
    .hidden { display: none; }
    .sr-only {
        position: absolute;
        width: 1px;
        height: 1px;
        overflow: hidden;
        clip: rect(0, 0, 0, 0);
    }
}
```

The utility layer always wins because it was declared last, even if a component selector has higher specificity. This eliminates the need for `!important` and the entire class of bugs that come from specificity conflicts. Cascade layers have been baseline-available since 2022.

### The :has() Selector

The `:has()` pseudo-class lets you style elements based on what they contain. It has been called the "parent selector" — something CSS developers have requested for two decades:

```css
/* Style a form group differently when it contains an invalid input */
.form-group:has(:invalid) {
    border-color: var(--color-error);
}

/* Change card layout when it has no image */
.card:has(img) {
    grid-template-columns: 1fr 2fr;
}

.card:not(:has(img)) {
    grid-template-columns: 1fr;
}

/* Show a counter when a list has more than 5 items */
.list:has(:nth-child(6)) .list-header::after {
    content: ' (' attr(data-count) ')';
}

/* Highlight the table row when any cell has focus */
tr:has(:focus-visible) {
    outline: 2px solid var(--color-primary);
}
```

Every one of these examples previously required JavaScript. A `MutationObserver`, or an event listener, or a framework directive. Now they are pure CSS, evaluated natively by the rendering engine, faster than any JavaScript alternative. `:has()` has been baseline-available since December 2023.

### What Not to Use: Chrome-Only Features

These CSS features work only in Chrome (or Chrome and Firefox, but not Safari). They are examples of bad code — or more precisely, code that is not ready yet. We mention them so you know they exist and know to wait:

**Container style queries using custom properties**: Chrome supports `@container style(--theme: dark)`, but Firefox does not yet. Wait for Firefox.

**CSS `if()` function**: A conditional function inside property values. Still in early specification, not shipping anywhere. Exciting but premature.

**Masonry layout via `grid-template-rows: masonry`**: Chrome and Firefox have experimental implementations, but the specification is still being debated. Do not ship this.

**Vendor-prefixed anything**: If you see `-webkit-text-stroke` or `-moz-appearance` in your CSS, those properties are not standards. They may never be standards. Using them makes your code dependent on a single engine and breaks the contract with your users that your site works across browsers.

## Part 4: Web Components — Encapsulated, Reusable, Framework-Free

### Custom Elements

Custom Elements let you define new HTML tags with their own behavior. The API has been stable and baseline-available across all browsers for years:

```javascript
// js/components/alert-banner.js
class AlertBanner extends HTMLElement {
    static observedAttributes = ['type', 'dismissible'];

    #type = 'info';
    #dismissible = false;

    constructor() {
        super();
        this.attachShadow({ mode: 'open' });
    }

    connectedCallback() {
        this.#render();
    }

    attributeChangedCallback(name, oldValue, newValue) {
        if (name === 'type') this.#type = newValue || 'info';
        if (name === 'dismissible') this.#dismissible = newValue !== null;
        if (this.isConnected) this.#render();
    }

    #render() {
        const colors = {
            info: { bg: '#dbeafe', border: '#3b82f6', text: '#1e40af' },
            success: { bg: '#dcfce7', border: '#22c55e', text: '#166534' },
            warning: { bg: '#fef3c7', border: '#f59e0b', text: '#92400e' },
            error: { bg: '#fee2e2', border: '#ef4444', text: '#991b1b' },
        };
        const c = colors[this.#type] || colors.info;

        this.shadowRoot.innerHTML = `
            <style>
                :host {
                    display: block;
                    padding: 1rem 1.25rem;
                    border-radius: 0.5rem;
                    border-left: 4px solid ${c.border};
                    background: ${c.bg};
                    color: ${c.text};
                    font-family: inherit;
                    margin-bottom: 1rem;
                }
                .dismiss {
                    float: right;
                    background: none;
                    border: none;
                    font-size: 1.25rem;
                    cursor: pointer;
                    color: inherit;
                    opacity: 0.7;
                }
                .dismiss:hover { opacity: 1; }
            </style>
            ${this.#dismissible ? '<button class="dismiss" aria-label="Dismiss">&times;</button>' : ''}
            <slot></slot>
        `;

        if (this.#dismissible) {
            this.shadowRoot.querySelector('.dismiss')
                ?.addEventListener('click', () => this.remove());
        }
    }
}

customElements.define('alert-banner', AlertBanner);
```

Usage in HTML:

```html
<alert-banner type="success" dismissible>
    Your changes have been saved.
</alert-banner>

<alert-banner type="error">
    Unable to connect to the server. Please try again.
</alert-banner>
```

That is a fully encapsulated, reusable component. Its styles do not leak out. Page styles do not leak in. It has a declarative API via HTML attributes. It works in any framework — React, Angular, Vue, Blazor, or no framework at all.

### Shadow DOM and Style Encapsulation

Shadow DOM is the mechanism that makes Web Components truly self-contained. Styles defined inside a shadow root do not affect the rest of the page, and page styles do not affect elements inside the shadow root.

This is the same idea as Blazor's CSS isolation (`.razor.css` files), but enforced by the browser at the DOM level rather than by a build tool rewriting selectors.

```javascript
class DataCard extends HTMLElement {
    constructor() {
        super();
        const shadow = this.attachShadow({ mode: 'open' });

        // Styles are scoped to this component
        const style = new CSSStyleSheet();
        style.replaceSync(`
            :host {
                display: block;
                container-type: inline-size;
            }
            .card {
                border: 1px solid #e5e7eb;
                border-radius: 0.5rem;
                padding: 1rem;
            }
            /* Container queries work inside shadow DOM */
            @container (min-width: 30rem) {
                .card {
                    display: grid;
                    grid-template-columns: 1fr 2fr;
                }
            }
            /* Inherit design tokens from the page */
            :host {
                color: var(--color-text, #1a1a2e);
                font-family: var(--font-sans, sans-serif);
            }
        `);

        shadow.adoptedStyleSheets = [style];
    }

    connectedCallback() {
        this.shadowRoot.innerHTML += `
            <div class="card">
                <slot name="header"></slot>
                <slot></slot>
            </div>
        `;
    }
}

customElements.define('data-card', DataCard);
```

The `adoptedStyleSheets` API lets you share `CSSStyleSheet` objects between components. If you have a design system with 20 components, they can all share the same base stylesheet instance, and the browser only parses it once. This is a performance optimization that no CSS-in-JS library can match because it uses the browser's native CSS engine.

### Declarative Shadow DOM

Declarative Shadow DOM lets you define shadow roots in static HTML, without JavaScript. The browser automatically attaches the shadow root when it parses the HTML:

```html
<user-profile>
    <template shadowrootmode="open">
        <style>
            :host {
                display: block;
                padding: 1rem;
                border: 1px solid var(--color-border, #e5e7eb);
                border-radius: 0.5rem;
            }
            .name { font-weight: 600; }
            .bio { color: var(--color-muted, #6b7280); }
        </style>
        <div class="name"><slot name="name">Anonymous</slot></div>
        <div class="bio"><slot name="bio">No bio provided</slot></div>
    </template>
    <span slot="name">Kushal</span>
    <span slot="bio">Writes code, reads books, builds things</span>
</user-profile>
```

This works even with JavaScript disabled. The content renders immediately — no flash of unstyled content, no waiting for JavaScript to execute. Declarative Shadow DOM has been baseline-available across all evergreen browsers since early 2024.

## Part 5: Building a Complete Application

Let us put everything together and build a real application. We will build a contact management app — a simplified version of the kind of thing you would build with Blazor Server or ASP.NET MVC, but entirely in vanilla JavaScript and CSS with no build step.

### Project Structure

```
contacts-app/
├── index.html
├── css/
│   └── app.css
├── js/
│   ├── main.js
│   ├── router.js
│   ├── state.js
│   ├── components/
│   │   ├── contact-list.js
│   │   ├── contact-detail.js
│   │   ├── contact-form.js
│   │   └── nav-bar.js
│   └── services/
│       └── contact-service.js
└── data/
    └── contacts.json
```

No `package.json`. No `node_modules`. No `.babelrc`. No `webpack.config.js`. No `vite.config.js`. No `tsconfig.json`. Just files. You open `index.html` in a browser (via any static file server, including `dotnet serve` or Python's `python -m http.server`), and it works.

### State Management

Here is a complete reactive state management system in under 40 lines:

```javascript
// js/state.js
export class Store {
    #state;
    #listeners = new Set();

    constructor(initialState) {
        this.#state = structuredClone(initialState);
    }

    getState() {
        return this.#state;
    }

    setState(updater) {
        const next = typeof updater === 'function'
            ? updater(this.#state)
            : { ...this.#state, ...updater };
        this.#state = structuredClone(next);
        this.#notify();
    }

    subscribe(listener) {
        this.#listeners.add(listener);
        return () => this.#listeners.delete(listener);
    }

    #notify() {
        for (const listener of this.#listeners) {
            listener(this.#state);
        }
    }
}
```

That is the entire state management library. No Redux. No Zustand. No MobX. No Pinia. It supports immutable updates (via `structuredClone`), subscriptions, and functional updaters. It is 35 lines of code, and it does everything that Redux does for a typical application — minus the time-travel debugging, which you probably do not need, and the middleware ecosystem, which you definitely do not need.

Usage:

```javascript
// js/main.js
import { Store } from './state.js';

const store = new Store({
    contacts: [],
    selectedId: null,
    filter: '',
    isLoading: false,
});

// Subscribe to changes
store.subscribe((state) => {
    renderContactList(state.contacts, state.filter);
    renderContactDetail(state.selectedId, state.contacts);
});

// Dispatch an update
async function loadContacts() {
    store.setState({ isLoading: true });
    const response = await fetch('/data/contacts.json');
    const contacts = await response.json();
    store.setState({ contacts, isLoading: false });
}
```

### The Contact Service

```javascript
// js/services/contact-service.js
const STORAGE_KEY = 'contacts-app-data';

export class ContactService {
    #contacts = [];

    async load() {
        const stored = localStorage.getItem(STORAGE_KEY);
        if (stored) {
            this.#contacts = JSON.parse(stored);
        } else {
            const response = await fetch('data/contacts.json');
            this.#contacts = await response.json();
            this.#save();
        }
        return this.#contacts;
    }

    getAll() {
        return [...this.#contacts];
    }

    getById(id) {
        return this.#contacts.find(c => c.id === id) ?? null;
    }

    create(contact) {
        const newContact = {
            ...contact,
            id: crypto.randomUUID(),
            createdAt: Temporal.Now.plainDateTimeISO().toString(),
        };
        this.#contacts = [...this.#contacts, newContact];
        this.#save();
        return newContact;
    }

    update(id, changes) {
        this.#contacts = this.#contacts.map(c =>
            c.id === id ? { ...c, ...changes } : c
        );
        this.#save();
        return this.getById(id);
    }

    delete(id) {
        this.#contacts = this.#contacts.filter(c => c.id !== id);
        this.#save();
    }

    search(query) {
        const q = query.toLowerCase();
        return this.#contacts.filter(c =>
            c.name.toLowerCase().includes(q) ||
            c.email.toLowerCase().includes(q)
        );
    }

    #save() {
        localStorage.setItem(STORAGE_KEY, JSON.stringify(this.#contacts));
    }
}
```

Notice the use of `crypto.randomUUID()` — a built-in browser API for generating UUIDs. No npm package needed. And `Temporal.Now.plainDateTimeISO()` for timestamps. No Moment.js. No date-fns.

### Form Validation Without a Library

The browser's Constraint Validation API is comprehensive and underused:

```javascript
// js/components/contact-form.js
export function renderContactForm(container, { onSubmit, contact = null }) {
    container.innerHTML = `
        <form id="contact-form" novalidate>
            <div class="form-group">
                <label for="name">Name</label>
                <input id="name" name="name" required minlength="2" maxlength="100"
                       value="${contact?.name ?? ''}"
                       aria-describedby="name-error">
                <span id="name-error" class="error" role="alert"></span>
            </div>

            <div class="form-group">
                <label for="email">Email</label>
                <input id="email" name="email" type="email" required
                       value="${contact?.email ?? ''}"
                       aria-describedby="email-error">
                <span id="email-error" class="error" role="alert"></span>
            </div>

            <div class="form-group">
                <label for="phone">Phone</label>
                <input id="phone" name="phone" type="tel"
                       pattern="[+]?[0-9\\s\\-()]{7,20}"
                       value="${contact?.phone ?? ''}"
                       aria-describedby="phone-error">
                <span id="phone-error" class="error" role="alert"></span>
            </div>

            <button type="submit">${contact ? 'Update' : 'Create'} Contact</button>
        </form>
    `;

    const form = container.querySelector('#contact-form');

    // Show validation errors on blur
    form.querySelectorAll('input').forEach(input => {
        input.addEventListener('blur', () => showFieldError(input));
        input.addEventListener('input', () => clearFieldError(input));
    });

    form.addEventListener('submit', (e) => {
        e.preventDefault();

        // Validate all fields
        let isValid = true;
        form.querySelectorAll('input').forEach(input => {
            if (!input.checkValidity()) {
                showFieldError(input);
                isValid = false;
            }
        });

        if (!isValid) {
            form.querySelector(':invalid')?.focus();
            return;
        }

        const data = Object.fromEntries(new FormData(form));
        onSubmit(data);
    });
}

function showFieldError(input) {
    const errorEl = document.getElementById(`${input.name}-error`);
    if (!errorEl) return;

    if (input.validity.valueMissing) {
        errorEl.textContent = `${input.labels[0]?.textContent} is required`;
    } else if (input.validity.typeMismatch) {
        errorEl.textContent = `Please enter a valid ${input.type}`;
    } else if (input.validity.tooShort) {
        errorEl.textContent = `Must be at least ${input.minLength} characters`;
    } else if (input.validity.patternMismatch) {
        errorEl.textContent = 'Please enter a valid format';
    } else {
        errorEl.textContent = '';
    }
}

function clearFieldError(input) {
    if (input.checkValidity()) {
        const errorEl = document.getElementById(`${input.name}-error`);
        if (errorEl) errorEl.textContent = '';
    }
}
```

The `checkValidity()` method, the `validity` object with its `valueMissing`, `typeMismatch`, `tooShort`, and `patternMismatch` properties, and the `FormData` constructor are all built into the browser. This is not a polyfill. This is not a library. This is the web platform, and it handles form validation at least as well as any JavaScript validation library you have ever used.

## Part 6: Angular as a Contrast — The Framework Tax

The prompt for this article asked for Angular examples. Let us be clear about what we are comparing: Angular is a legitimate, well-engineered framework used by serious organizations. The current stable version is Angular 21 (released in 2026, with v21.2.8 as of early April 2026). It uses signals-based reactivity, standalone components by default, zoneless change detection, and TypeScript 6. It is a good framework.

But it is a framework. And using a framework always means paying a tax.

Here is a simple contact list component in Angular 21:

```typescript
// contact-list.component.ts (Angular 21)
import { Component, signal, computed, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { FormsModule } from '@angular/forms';

interface Contact {
    id: string;
    name: string;
    email: string;
}

@Component({
    selector: 'app-contact-list',
    standalone: true,
    imports: [FormsModule],
    template: `
        <input [(ngModel)]="filter" placeholder="Search contacts...">
        <ul>
            @for (contact of filteredContacts(); track contact.id) {
                <li (click)="select(contact)">
                    {{ contact.name }} — {{ contact.email }}
                </li>
            }
        </ul>
    `,
})
export class ContactListComponent {
    private http = inject(HttpClient);

    contacts = signal<Contact[]>([]);
    filter = signal('');

    filteredContacts = computed(() => {
        const q = this.filter().toLowerCase();
        return this.contacts().filter(c =>
            c.name.toLowerCase().includes(q) ||
            c.email.toLowerCase().includes(q)
        );
    });

    constructor() {
        this.http.get<Contact[]>('/api/contacts')
            .subscribe(data => this.contacts.set(data));
    }

    select(contact: Contact) {
        // navigation logic
    }
}
```

And here is the equivalent in vanilla JavaScript:

```javascript
// No TypeScript. No decorator. No framework. No build step.
export function renderContactList(container, contacts, onSelect) {
    const filter = document.createElement('input');
    filter.placeholder = 'Search contacts...';

    const list = document.createElement('ul');

    function render(query = '') {
        const q = query.toLowerCase();
        const filtered = contacts.filter(c =>
            c.name.toLowerCase().includes(q) ||
            c.email.toLowerCase().includes(q)
        );

        list.innerHTML = filtered.map(c => `
            <li data-id="${c.id}">${c.name} — ${c.email}</li>
        `).join('');
    }

    filter.addEventListener('input', () => render(filter.value));
    list.addEventListener('click', (e) => {
        const li = e.target.closest('li');
        if (li) onSelect(li.dataset.id);
    });

    container.append(filter, list);
    render();
}
```

The Angular version requires: Node.js, npm, the Angular CLI, TypeScript, the Angular compiler, a development server, a build step, and a configuration file. The vanilla version requires: a text editor and a browser.

This is not to say Angular is bad. If you are building a large application with a team of twenty developers, Angular's structure, type safety, and opinionated architecture provide real value. The dependency injection system is excellent. The signal-based reactivity in Angular 21 is genuinely well-designed. The built-in testing tools are solid.

But if you are building a contact list, a blog, a documentation site, a marketing page, a product showcase, a dashboard, or any other application where the team is small and the complexity is moderate — you do not need Angular. You do not need React. You do not need Vue. You need JavaScript, CSS, and the browser.

## Part 7: Patterns for Real Applications

### Event Delegation

Instead of attaching an event listener to every button, attach one listener to a parent and check what was clicked:

```javascript
// BAD: One listener per button (100 buttons = 100 listeners)
document.querySelectorAll('.delete-btn').forEach(btn => {
    btn.addEventListener('click', () => deleteItem(btn.dataset.id));
});

// GOOD: One listener for all buttons (event delegation)
document.querySelector('.item-list').addEventListener('click', (e) => {
    const deleteBtn = e.target.closest('.delete-btn');
    if (deleteBtn) {
        deleteItem(deleteBtn.dataset.id);
    }

    const editBtn = e.target.closest('.edit-btn');
    if (editBtn) {
        editItem(editBtn.dataset.id);
    }
});
```

Event delegation uses fewer listeners (better memory), works for dynamically added elements (no need to rebind), and is the pattern that every framework uses internally. jQuery popularized it with `.on()`. React uses it under the hood. You can use it directly.

### The Observer Pattern for Reactive Updates

```javascript
// js/observable.js
export class Observable {
    #subscribers = new Map();

    on(event, callback) {
        if (!this.#subscribers.has(event)) {
            this.#subscribers.set(event, new Set());
        }
        this.#subscribers.get(event).add(callback);
        return () => this.#subscribers.get(event)?.delete(callback);
    }

    emit(event, data) {
        this.#subscribers.get(event)?.forEach(cb => cb(data));
    }
}

// Usage
const bus = new Observable();

// Component A subscribes
const unsub = bus.on('contact:updated', (contact) => {
    renderContactDetail(contact);
});

// Component B emits
bus.emit('contact:updated', updatedContact);

// Clean up when done
unsub();
```

This is the same pattern as C# events, `IObservable<T>`, or .NET's `EventHandler`. It decouples components that produce events from components that consume them, without requiring a shared framework.

### Debouncing and Throttling

```javascript
function debounce(fn, delay) {
    let timeoutId;
    return (...args) => {
        clearTimeout(timeoutId);
        timeoutId = setTimeout(() => fn(...args), delay);
    };
}

function throttle(fn, limit) {
    let inThrottle = false;
    return (...args) => {
        if (!inThrottle) {
            fn(...args);
            inThrottle = true;
            setTimeout(() => inThrottle = false, limit);
        }
    };
}

// Usage
const searchInput = document.querySelector('#search');
searchInput.addEventListener('input', debounce((e) => {
    performSearch(e.target.value);
}, 300));

window.addEventListener('scroll', throttle(() => {
    checkScrollPosition();
}, 100));
```

Lodash has 80,000+ lines of code. You needed two functions from it. Now you have two functions, in twelve lines, with no dependency.

### Intersection Observer for Lazy Loading and Scroll Effects

```javascript
// Lazy load images
const imageObserver = new IntersectionObserver((entries) => {
    for (const entry of entries) {
        if (entry.isIntersecting) {
            const img = entry.target;
            img.src = img.dataset.src;
            img.removeAttribute('data-src');
            imageObserver.unobserve(img);
        }
    }
}, { rootMargin: '200px' });

document.querySelectorAll('img[data-src]').forEach(img => {
    imageObserver.observe(img);
});

// Scroll-reveal animation
const revealObserver = new IntersectionObserver((entries) => {
    for (const entry of entries) {
        if (entry.isIntersecting) {
            entry.target.classList.add('revealed');
        }
    }
}, { threshold: 0.1 });

document.querySelectorAll('.reveal').forEach(el => {
    revealObserver.observe(el);
});
```

Combined with CSS:

```css
.reveal {
    opacity: 0;
    transform: translateY(1rem);
    transition: opacity 0.6s ease, transform 0.6s ease;
}

.reveal.revealed {
    opacity: 1;
    transform: translateY(0);
}

@media (prefers-reduced-motion: reduce) {
    .reveal {
        opacity: 1;
        transform: none;
        transition: none;
    }
}
```

That last rule is important. The `prefers-reduced-motion` media query respects users who have indicated that they experience motion sickness or other adverse effects from animations. Every animation you write should have a reduced-motion override. This is not optional. This is accessibility.

## Part 8: Performance Without a Bundler

### Module Loading Strategy

When you serve ES modules directly, the browser makes one HTTP request per `import`. This can be slow over high-latency connections if you have many small modules. Here are the mitigation strategies:

**HTTP/2 multiplexing**: Modern servers (and GitHub Pages, and Cloudflare Pages, and every CDN) serve content over HTTP/2, which multiplexes many requests over a single connection. The overhead of individual requests is dramatically lower than it was in the HTTP/1.1 era.

**`<link rel="modulepreload">`**: Tell the browser to start fetching modules before they are needed:

```html
<link rel="modulepreload" href="js/router.js">
<link rel="modulepreload" href="js/state.js">
<link rel="modulepreload" href="js/components/contact-list.js">
```

**Code splitting by route**: Only import the code you need for the current page:

```javascript
// In your router
router.route('/contacts', async (outlet) => {
    const { renderContactList } = await import('./components/contact-list.js');
    renderContactList(outlet, store.getState().contacts);
});
```

Dynamic `import()` is lazy — the module is only fetched when the route is visited. This gives you the same code-splitting behavior that Webpack provides, without Webpack.

**Import maps**: If you have modules that are referenced by bare specifiers (like `import { html } from 'lit'`), you can use import maps to tell the browser where to find them:

```html
<script type="importmap">
{
    "imports": {
        "lit": "https://cdn.jsdelivr.net/npm/lit@3/+esm"
    }
}
</script>
```

Import maps have been baseline-available since 2023. They are the browser-native equivalent of Webpack's `resolve.alias`.

### When You Actually Need a Build Step

We have been arguing against build steps for the entire article. Let us be honest about when you actually need one:

**TypeScript**: If you want static type checking (and you should — TypeScript's type system catches real bugs), you need the TypeScript compiler. However, you can run `tsc --noEmit` as a check-only step and still serve `.js` files directly. Or you can use JSDoc type annotations, which TypeScript understands without any compilation:

```javascript
// TypeScript types via JSDoc — no build step required
/** @type {import('./types').Contact} */
const contact = { id: '123', name: 'Alice', email: 'alice@example.com' };

/**
 * @param {string} query
 * @returns {Promise<Contact[]>}
 */
async function searchContacts(query) {
    const response = await fetch(`/api/search?q=${encodeURIComponent(query)}`);
    return response.json();
}
```

**Minification for production**: For a production deployment where every kilobyte matters, you might want to minify your JavaScript and CSS. You can do this with a single `esbuild --minify` command — no configuration file, no plugins, no ecosystem.

**Very large applications**: If your application has 500 modules and cold-start loading time is critical, a bundler helps by reducing the number of HTTP requests. But "500 modules" is a very large application. Most applications do not have 500 modules.

## Part 9: Security Without Dependencies

### Content Security Policy

A strict Content Security Policy (CSP) is easier to enforce when you have no third-party scripts:

```html
<meta http-equiv="Content-Security-Policy"
      content="default-src 'self'; script-src 'self'; style-src 'self'; img-src 'self' data:;">
```

This policy says: only load scripts, styles, and images from the same origin. No CDN. No inline scripts. No `eval()`. This is the most restrictive CSP you can reasonably have, and it is trivially achievable when you have zero external dependencies.

Compare this to a typical React application that loads scripts from five CDNs, uses inline styles (React's `style` prop), and may depend on `eval()` for certain build configurations. Getting a strict CSP to work with a React app is a week-long project. Getting it to work with vanilla JavaScript is a one-line `<meta>` tag.

### Subresource Integrity

If you do load anything from a CDN (a polyfill, for example), use Subresource Integrity (SRI) to ensure the file has not been tampered with:

```html
<script src="https://cdn.example.com/polyfill.js"
        integrity="sha384-abc123..."
        crossorigin="anonymous"></script>
```

The browser will refuse to execute the script if its hash does not match. This is free protection against CDN compromises, and it is built into every browser.

## Part 10: Testing Without a Framework

You can run unit tests in the browser without Jest, without Mocha, without Vitest, without any test runner:

```javascript
// tests/test-runner.js
let passed = 0;
let failed = 0;

function assert(condition, message) {
    if (condition) {
        passed++;
        console.log(`  ✓ ${message}`);
    } else {
        failed++;
        console.error(`  ✗ ${message}`);
    }
}

function assertEqual(actual, expected, message) {
    assert(
        JSON.stringify(actual) === JSON.stringify(expected),
        `${message}: expected ${JSON.stringify(expected)}, got ${JSON.stringify(actual)}`
    );
}

function describe(name, fn) {
    console.group(name);
    fn();
    console.groupEnd();
}

// Tests
import { Store } from '../js/state.js';

describe('Store', () => {
    const store = new Store({ count: 0 });

    assert(store.getState().count === 0, 'initial state is correct');

    store.setState({ count: 5 });
    assert(store.getState().count === 5, 'setState with object works');

    store.setState(state => ({ count: state.count + 1 }));
    assert(store.getState().count === 6, 'setState with function works');

    let notified = false;
    const unsub = store.subscribe(() => { notified = true; });
    store.setState({ count: 7 });
    assert(notified === true, 'subscriber is notified');

    notified = false;
    unsub();
    store.setState({ count: 8 });
    assert(notified === false, 'unsubscribed listener is not notified');
});

console.log(`\nResults: ${passed} passed, ${failed} failed`);
```

Open `tests/test-runner.html` in a browser, check the console. Done. No npm install. No configuration file. No 200-megabyte `node_modules` folder containing a test runner that takes 8 seconds to start up.

For a real application, you would eventually want a proper test runner for CI integration. But the point is that you can get started testing immediately, with no setup cost, using only what the browser provides.

## Part 11: What Is Coming Next

### The Future of JavaScript

The TC39 committee has several proposals that are likely to reach Stage 4 in the next few years:

**Pattern matching**: A structured way to match values against patterns, similar to C#'s pattern matching with `switch` expressions. This will eliminate many `if`/`else` chains.

**Decorators**: A standardized way to annotate and modify classes and class members. TypeScript has had decorators for years, but the TC39 proposal has a different (and better) design.

**`import defer`**: Lazy module evaluation — a module is not loaded until its exports are first accessed. This gives you code splitting without dynamic `import()`.

**`Math.sumPrecise()`**: Precise floating-point summation. If you have ever seen `0.1 + 0.2 === 0.30000000000000004`, this function is the fix.

### The Future of CSS

**CSS `if()` function**: Conditional values inside property declarations. This would let you write `color: if(style(--dark) ? white : black)` directly in CSS.

**CSS masonry layout**: A native way to do Pinterest-style layouts without JavaScript. The specification is still being debated between a Grid-based approach and a standalone `display: masonry` approach.

**`@scope`**: Scoping CSS rules to a DOM subtree, similar to Shadow DOM but without the full encapsulation boundary. Chrome and Firefox have implementations; Safari is expected to follow.

**Typed `attr()` in all properties**: Using HTML attributes directly in any CSS property with type checking: `width: attr(data-width length, 100px)`. Currently available in Chrome, coming to others.

## Part 12: Resources

Here are the authoritative references for everything covered in this article:

**JavaScript / ECMAScript**
- TC39 Proposals: https://github.com/tc39/proposals
- Temporal API Proposal: https://github.com/tc39/proposal-temporal
- Temporal Polyfill: https://github.com/js-temporal/temporal-polyfill
- MDN Web Docs (JavaScript): https://developer.mozilla.org/en-US/docs/Web/JavaScript
- Exploring JavaScript (ES2025 Edition) by Dr. Axel Rauschmayer: https://exploringjs.com/js/

**CSS**
- MDN Web Docs (CSS): https://developer.mozilla.org/en-US/docs/Web/CSS
- web.dev CSS articles: https://web.dev/css
- Baseline feature availability: https://web.dev/baseline
- State of CSS Survey: https://stateofcss.com

**Web Components**
- MDN Web Docs (Web Components): https://developer.mozilla.org/en-US/docs/Web/API/Web_components
- web.dev Declarative Shadow DOM: https://web.dev/articles/declarative-shadow-dom

**Browser Compatibility**
- Can I Use: https://caniuse.com
- Baseline Dashboard: https://webstatus.dev

**The Observer Magazine Source Code**
- GitHub: https://github.com/ObserverMagazine/observermagazine.github.io — this very site is built with Blazor WASM and serves as a practical example of deploying a complete web application to GitHub Pages with zero JavaScript dependencies.

The web platform in 2026 is extraordinary. It has native modules, native components, native date-time handling, native color functions, native container queries, native nesting, native form validation, and native everything else. The only question is whether you will use it, or whether you will continue paying the npm tax for things the browser gives you for free.

Choose wisely.
