---
title: "JavaScript and CSS from the Ground Up: Complete Web Applications from First Principles in 2026"
date: 2026-05-02
author: myblazor-team
summary: A comprehensive guide to building complete web applications using only what evergreen browsers provide in 2026 — no npm, no build step, no frameworks, no libraries. Covers modern CSS (nesting, container queries, anchor positioning, popover, @scope, @layer, custom properties, oklch, view transitions), modern JavaScript (ES2026, Temporal, Web Components, Shadow DOM, Fetch, AbortController, structuredClone, modules), and practical architecture for real applications from first principles.
tags:
  - javascript
  - css
  - web-standards
  - deep-dive
  - best-practices
  - web-components
  - no-build
  - first-principles
---

## Part 1 — The Case for Going Zero-Dependency

### The Thursday Afternoon That Changed Everything

Picture a Thursday afternoon. You are an ASP.NET developer. Your Visual Studio solution loads in about four seconds. Your C# compiles in maybe ten. You push to GitHub, your CI pipeline builds, tests, and deploys in a few minutes. Life is good.

Then somebody says: "We need a little JavaScript on the front end."

You nod. Reasonable enough. You open a terminal and type `npm init`. Forty-five seconds later you have a `package.json`. You install a "simple" UI library. One thousand two hundred and thirty-seven packages install. Your `node_modules` folder is 287 megabytes. Your build pipeline now needs Node.js 20, and a bundler, and a transpiler, and a linter configuration file that is itself 400 lines long. The bundler has a plugin system with its own plugin ecosystem. The linter has a configuration format that changed between major versions in a way that is not backward-compatible. You install a CSS preprocessor because apparently writing CSS by hand is something people stopped doing in 2015.

You go home that evening. You come back Friday morning. `npm audit` reports 14 vulnerabilities, 3 of which are critical. You run `npm audit fix`. It fixes 9 of them. The remaining 5 require breaking changes in dependencies you do not control. A transitive dependency four levels deep has been abandoned by its maintainer. You spend Friday afternoon reading GitHub issues written by people who are angry at an open-source volunteer for not fixing a bug in a package that has 12 million weekly downloads and zero funding.

This is the world we made. And in 2026, we do not have to live in it.

### What If We Just... Didn't?

Here is the premise of this article: the modern web browser — the one sitting on your desk or in your pocket right now — is extraordinarily capable. It has a layout engine that can do things that required entire CSS frameworks five years ago. It has a JavaScript runtime that is faster than most compiled languages from the previous decade. It has built-in support for components, scoped styles, reactive color manipulation, scroll-driven animations, native popovers and dialogs, and an entire date-and-time API that finally works correctly.

What if we used all of that? What if we wrote zero `npm install` commands? What if we had zero build steps? What if our entire front-end toolchain was a text editor and a browser?

This is not a thought experiment. This is a practical engineering decision, and in 2026 it is a genuinely good one for a large category of applications.

### Who This Article Is For

You are a .NET developer. Maybe you write ASP.NET Core MVC applications. Maybe you work with Blazor. Maybe you are one of those brave souls still maintaining a Web Forms application and you have a haunted look in your eyes that tells a story words cannot.

You know C#. You know how to make HTTP requests. You have a vague understanding that JavaScript exists and that it has something to do with the `<script>` tag. You may have written a jQuery `$(document).ready()` block in 2014 and sworn an oath never to return.

This article starts from that foundation. We will build upward from first principles. Every concept will be explained. Every line of code will be justified. And by the end you will be able to build complete, production-quality web applications using nothing but what the browser gives you.

### The Rules

Throughout this article, we will follow these rules without exception:

1. **No npm.** No `package.json`. No `node_modules`. Not even once.
2. **No build step.** No Webpack, no Vite, no esbuild, no Rollup, no Parcel. If the browser cannot understand the file as-is, we do not use it.
3. **No libraries.** No React, no Vue, no Angular, no Svelte, no jQuery, no Lodash, no Tailwind, no Bootstrap. The browser is the framework.
4. **No SCSS, no LESS, no PostCSS.** Pure CSS as the browser interprets it. No compilation.
5. **No vendor prefixes.** If a feature requires `-webkit-` or `-moz-` to work, that feature does not exist for our purposes. If we encounter such a feature, we will show it as a cautionary example of what to avoid.
6. **Evergreen browsers only.** Chrome, Safari, Firefox — current stable versions. No Internet Explorer. No legacy mobile browsers. No polyfills.
7. **Baseline or better.** We prefer features that are Baseline Widely Available (supported in all major browsers for at least 30 months). For newer features, we will clearly mark them as Baseline Newly Available and note which browsers support them.

### Why This Is a Good Idea

Let me address the obvious objection: "This sounds like Not Invented Here syndrome taken to a pathological extreme."

Fair. Let me counter with five practical arguments.

**Supply chain security.** Every npm package you install is code written by a stranger that runs on your users' machines. The 2024 xz backdoor, the 2021 ua-parser-js compromise, the 2018 event-stream attack — these are not theoretical risks. They happened. When your dependency tree is zero packages deep, your supply chain attack surface is zero.

**Performance.** A fresh Create React App sends 142 KB of gzipped JavaScript to the browser before your application code does anything. A Vite + Vue 3 starter is about 50 KB. A Next.js application with server-side rendering is more, because the hydration code must re-execute the component tree on the client. When you write plain HTML, CSS, and JavaScript, the browser receives exactly what you wrote. A well-structured page with no framework overhead can achieve a Largest Contentful Paint under 1 second on a mid-range phone.

**Longevity.** jQuery was released in 2006 and is largely obsolete. AngularJS (the original, not Angular) was released in 2010 and is abandoned. React was released in 2013 and has undergone multiple paradigm shifts (classes to hooks to server components). The web platform itself, however, has maintained backward compatibility since the 1990s. A `<marquee>` tag from 1997 still works in every browser. More importantly, a well-written HTML page from 2010 using semantic elements and progressive enhancement still renders perfectly today. The platform is the only dependency that does not break.

**Simplicity.** Your production deployment is a folder of files. Your CI pipeline is "copy files to a server" or "push to GitHub Pages." There is no `npm ci` step. There is no "build failed because a transitive dependency yanked a version." There is no "works on my machine but the bundler configuration differs in CI." You edit a file, save it, refresh the browser. Done.

**Education.** When you learn the platform, you learn something permanent. React hooks are React knowledge. Angular signals are Angular knowledge. CSS Grid, `<dialog>`, `fetch()`, Custom Elements — these are web knowledge. They work in React. They work in Angular. They work in Blazor's JavaScript interop. They work in a plain HTML file. Learning the platform makes you better at every framework because every framework is built on the platform.

### What We Will Build

This is not a tutorial that ends with a styled button and a paragraph of congratulatory text. Over the course of this article we will build:

- A complete component system using Web Components and Shadow DOM
- A responsive layout system using CSS Grid, Container Queries, and modern units
- A theming engine using CSS Custom Properties and `oklch()` color manipulation
- A full data table with sorting, filtering, and pagination — zero JavaScript frameworks
- A modal dialog system using the native `<dialog>` element and the Popover API
- A tooltip and menu system using CSS Anchor Positioning
- A form validation system using the Constraint Validation API
- A client-side router using the History API
- A state management system using custom events and `structuredClone()`
- A date picker using the Temporal API (ES2026)

All of this will be delivered as plain `.html`, `.css`, and `.js` files that you can open in a browser directly from your file system. No server required for development. No build step required for production.

Let us begin.

---

## Part 2 — HTML: The Foundation You Have Been Neglecting

### The Document Is Not a Div Soup

Here is the HTML that many developers write:

```html
<!-- BAD: The div soup approach -->
<div class="page">
  <div class="header">
    <div class="logo">My App</div>
    <div class="nav">
      <div class="nav-item"><a href="/">Home</a></div>
      <div class="nav-item"><a href="/about">About</a></div>
    </div>
  </div>
  <div class="content">
    <div class="sidebar">...</div>
    <div class="main">
      <div class="article">
        <div class="title">Hello World</div>
        <div class="text">Some content here.</div>
      </div>
    </div>
  </div>
  <div class="footer">Copyright 2026</div>
</div>
```

This is bad. Not "stylistically questionable" bad. Not "I prefer a different approach" bad. **Functionally bad.** This HTML communicates nothing to the browser about what any of these elements are. A screen reader sees a flat list of generic containers. Search engines see no structure. The browser's built-in reader mode cannot extract the article. The `Tab` key has no landmarks to navigate between.

Here is the same page written correctly:

```html
<!-- GOOD: Semantic HTML -->
<body>
  <header>
    <a href="/" aria-label="Home">My App</a>
    <nav aria-label="Main">
      <ul>
        <li><a href="/">Home</a></li>
        <li><a href="/about">About</a></li>
      </ul>
    </nav>
  </header>

  <div class="layout">
    <aside aria-label="Sidebar">...</aside>
    <main>
      <article>
        <h1>Hello World</h1>
        <p>Some content here.</p>
      </article>
    </main>
  </div>

  <footer>
    <p><small>Copyright 2026</small></p>
  </footer>
</body>
```

What changed? Almost everything.

- `<header>` tells the browser "this is the page header." Screen readers announce it. Browser reader modes can skip it.
- `<nav>` tells the browser "this is a navigation region." Screen readers offer a shortcut to jump directly to it.
- `<ul>` and `<li>` tell the browser "this is a list of items." A screen reader announces "navigation, list, 2 items" — immediately communicating the structure.
- `<main>` tells the browser "this is the primary content." There should be exactly one `<main>` per page.
- `<article>` tells the browser "this is a self-contained composition." It could be syndicated, shared, or displayed in a reader view.
- `<h1>` tells the browser "this is the most important heading." The heading hierarchy (`<h1>` through `<h6>`) creates an implicit table of contents.
- `<aside>` tells the browser "this is tangentially related content."
- `<footer>` tells the browser "this is the footer." Screen readers announce it.

The `aria-label` attributes give names to regions. Without them, if a page has two `<nav>` elements (common — a main nav and a footer nav), a screen reader user has no way to distinguish them.

### The Elements You Should Know in 2026

Let me walk through the HTML elements that matter for modern web applications, many of which you may never have used.

**`<dialog>`** — A native modal or non-modal dialog. When opened with `.showModal()`, it automatically traps focus, adds a backdrop, closes on `Escape`, and returns focus to the triggering element when closed. Every accessibility behavior that you previously had to implement by hand — focus trap, scroll lock, `aria-modal`, backdrop click handling — is built in.

```html
<dialog id="confirm-dialog">
  <h2>Are you sure?</h2>
  <p>This action cannot be undone.</p>
  <form method="dialog">
    <button value="cancel">Cancel</button>
    <button value="confirm">Confirm</button>
  </form>
</dialog>

<button onclick="document.getElementById('confirm-dialog').showModal()">
  Delete Account
</button>

<script>
  const dialog = document.getElementById('confirm-dialog');
  dialog.addEventListener('close', () => {
    console.log('User chose:', dialog.returnValue);
    // "cancel" or "confirm"
  });
</script>
```

Notice the `<form method="dialog">`. This is a special form method that, when a button inside it is clicked, closes the dialog and sets the dialog's `returnValue` to the clicked button's `value`. No JavaScript event handler needed for the close behavior.

**`<details>` and `<summary>`** — A native disclosure widget. The browser handles the open/close toggle, the animation (on supported browsers), and keyboard accessibility.

```html
<details>
  <summary>Show advanced options</summary>
  <div class="options-panel">
    <label>
      <input type="checkbox" name="verbose"> Verbose logging
    </label>
    <label>
      <input type="checkbox" name="debug"> Debug mode
    </label>
  </div>
</details>
```

The `<details>` element fires a `toggle` event when opened or closed. You can have exclusive accordions by listening for this event and closing other `<details>` elements. Even better, in 2026 the `name` attribute allows you to group `<details>` elements into an exclusive accordion natively:

```html
<!-- Exclusive accordion: only one can be open at a time -->
<details name="faq">
  <summary>What is your return policy?</summary>
  <p>30 days, no questions asked.</p>
</details>
<details name="faq">
  <summary>Do you ship internationally?</summary>
  <p>Yes, to over 50 countries.</p>
</details>
<details name="faq">
  <summary>How do I contact support?</summary>
  <p>Email us at support@example.com.</p>
</details>
```

All `<details>` elements with the same `name` attribute form a group. Opening one automatically closes the others. Zero JavaScript.

**`<output>`** — Represents the result of a calculation or user action. Screen readers announce changes to its content.

```html
<form oninput="result.value = parseInt(a.value) + parseInt(b.value)">
  <input type="range" id="a" value="50"> +
  <input type="number" id="b" value="25"> =
  <output name="result" for="a b">75</output>
</form>
```

**`<meter>`** — A scalar measurement within a known range. Not a progress bar — that is `<progress>`. A `<meter>` represents a gauge, like disk usage or password strength.

```html
<label for="disk">Disk usage:</label>
<meter id="disk" value="0.7" min="0" max="1" low="0.3" high="0.7" optimum="0.2">
  70%
</meter>
```

The `low`, `high`, and `optimum` attributes cause the browser to color the meter differently depending on whether the value is in a good, acceptable, or bad range.

**`<time>`** — Machine-readable date/time. Search engines, screen readers, and browser extensions can parse this.

```html
<p>Published on <time datetime="2026-05-02">May 2, 2026</time></p>
<p>The event starts at <time datetime="2026-06-15T09:00-04:00">9 AM Eastern</time></p>
<p>Bake for <time datetime="PT45M">45 minutes</time></p>
```

**`<template>`** — A document fragment that is parsed but not rendered. It does not execute scripts, load images, or apply styles until you clone it into the DOM. This is the foundation of Web Components.

```html
<template id="card-template">
  <article class="card">
    <h3 class="card-title"></h3>
    <p class="card-body"></p>
  </article>
</template>

<script>
  const template = document.getElementById('card-template');
  const clone = template.content.cloneNode(true);
  clone.querySelector('.card-title').textContent = 'Hello';
  clone.querySelector('.card-body').textContent = 'World';
  document.body.appendChild(clone);
</script>
```

### The `popover` Attribute

This is new enough that many developers have not encountered it, but it is Baseline Newly Available across all major browsers as of early 2025. The `popover` attribute turns any element into a popover — an element that floats above the page in the top layer, with built-in light-dismiss (clicking outside closes it), focus management, and keyboard handling.

```html
<button popovertarget="my-menu">Open Menu</button>

<div id="my-menu" popover>
  <ul>
    <li><a href="/profile">Profile</a></li>
    <li><a href="/settings">Settings</a></li>
    <li><a href="/logout">Logout</a></li>
  </ul>
</div>
```

That is it. Click the button, the popover opens. Click outside, it closes. Press `Escape`, it closes. The popover renders in the top layer, above all other content, regardless of `z-index` stacking contexts. No JavaScript required for the toggle behavior.

There are three types of popovers:

- `popover="auto"` (the default when you write just `popover`) — Light-dismissable. Only one auto popover can be open at a time (unless they are nested). Clicking outside closes it.
- `popover="manual"` — Must be explicitly opened and closed. Clicking outside does not close it. Multiple manual popovers can be open simultaneously.
- `popover="hint"` — For tooltips and similar transient content. Opened by hover/focus using the `interestfor` attribute (very new, Chrome and Safari).

### Invoker Commands: `commandfor` and `command`

As of 2025-2026, browsers are shipping declarative invoker commands. These let buttons perform actions on other elements without any JavaScript:

```html
<!-- Open a dialog without JavaScript -->
<button commandfor="my-dialog" command="show-modal">Open Dialog</button>
<dialog id="my-dialog">
  <h2>Hello</h2>
  <button commandfor="my-dialog" command="close">Close</button>
</dialog>

<!-- Toggle a popover without JavaScript -->
<button commandfor="my-popover" command="toggle-popover">Menu</button>
<div id="my-popover" popover>Menu content</div>
```

The `commandfor` attribute takes the `id` of the target element. The `command` attribute specifies the action: `show-modal`, `close`, `toggle-popover`, `show-popover`, `hide-popover`, and more. This is still rolling out across browsers — Chrome shipped it in version 135, and Safari and Firefox are following. But the direction is clear: HTML is getting more declarative, and JavaScript is becoming optional for common UI patterns.

### The Input Types You Are Not Using

HTML has input types for almost every common data entry pattern. Stop using `<input type="text">` for everything and then writing JavaScript validation:

```html
<!-- Email with built-in validation -->
<input type="email" required>

<!-- URL with built-in validation -->
<input type="url" placeholder="https://example.com">

<!-- Phone number with on-screen keyboard hint -->
<input type="tel" pattern="[0-9]{3}-[0-9]{3}-[0-9]{4}" 
       placeholder="555-123-4567">

<!-- Date picker (native) -->
<input type="date" min="2026-01-01" max="2026-12-31">

<!-- Time picker (native) -->
<input type="time" step="900"> <!-- 15-minute increments -->

<!-- Date and time combined -->
<input type="datetime-local">

<!-- Number with constraints -->
<input type="number" min="0" max="100" step="5">

<!-- Range slider -->
<input type="range" min="0" max="100" value="50">

<!-- Color picker -->
<input type="color" value="#3b82f6">

<!-- Search with clear button -->
<input type="search" placeholder="Search...">
```

Each of these provides appropriate on-screen keyboards on mobile devices, built-in validation, and accessible semantics. The `date`, `time`, and `datetime-local` types render native date/time pickers on every platform — and in 2026, these native pickers are actually good. They respect the user's locale, support keyboard navigation, and are fully accessible.

### The Constraint Validation API

HTML has a built-in validation system. You do not need a JavaScript validation library:

```html
<form id="registration" novalidate>
  <div>
    <label for="username">Username</label>
    <input id="username" name="username" 
           type="text" required 
           minlength="3" maxlength="20"
           pattern="[a-zA-Z0-9_]+"
           title="Letters, numbers, and underscores only">
    <span class="error" aria-live="polite"></span>
  </div>

  <div>
    <label for="email">Email</label>
    <input id="email" name="email" 
           type="email" required>
    <span class="error" aria-live="polite"></span>
  </div>

  <div>
    <label for="password">Password</label>
    <input id="password" name="password" 
           type="password" required 
           minlength="8">
    <span class="error" aria-live="polite"></span>
  </div>

  <button type="submit">Register</button>
</form>

<script>
  const form = document.getElementById('registration');

  // Custom validation messages
  form.addEventListener('submit', (event) => {
    // Clear previous errors
    form.querySelectorAll('.error').forEach(el => el.textContent = '');

    if (!form.checkValidity()) {
      event.preventDefault();

      for (const input of form.elements) {
        if (input.validity && !input.validity.valid) {
          const errorSpan = input.nextElementSibling;
          if (errorSpan?.classList.contains('error')) {
            errorSpan.textContent = getCustomMessage(input);
          }
        }
      }
    }
  });

  function getCustomMessage(input) {
    if (input.validity.valueMissing) return `${input.labels[0].textContent} is required.`;
    if (input.validity.tooShort) return `Must be at least ${input.minLength} characters.`;
    if (input.validity.tooLong) return `Must be at most ${input.maxLength} characters.`;
    if (input.validity.patternMismatch) return input.title || 'Invalid format.';
    if (input.validity.typeMismatch) return `Please enter a valid ${input.type}.`;
    return input.validationMessage; // Browser's default message
  }
</script>
```

The `ValidityState` object (`input.validity`) has boolean properties for every possible validation failure: `valueMissing`, `typeMismatch`, `patternMismatch`, `tooLong`, `tooShort`, `rangeUnderflow`, `rangeOverflow`, `stepMismatch`, `badInput`, and `customError`. You can also set custom validity with `input.setCustomValidity('Your custom message')`.

The `novalidate` attribute on the `<form>` suppresses the browser's built-in validation UI (those tooltip bubbles) so you can show your own styled error messages. But the validation API still works — you just call `form.checkValidity()` yourself.

---

## Part 3 — CSS: The Language You Thought You Knew

### Stop Writing CSS Like It Is 2015

If your CSS files look like this:

```css
/* BAD: 2015-era CSS */
.container {
  width: 1200px;
  margin: 0 auto;
  padding: 0 15px;
}

.row {
  display: flex;
  flex-wrap: wrap;
  margin: 0 -15px;
}

.col-6 {
  width: 50%;
  padding: 0 15px;
  box-sizing: border-box;
}

.col-4 {
  width: 33.333%;
  padding: 0 15px;
  box-sizing: border-box;
}

@media (max-width: 768px) {
  .col-6, .col-4 {
    width: 100%;
  }
}
```

Then you are writing CSS that belongs in a museum. Every single technique in that snippet has been superseded by something better. Fixed pixel widths. Magic number breakpoints. Negative margin hacks. Percentage-based column widths. Manual box-sizing declarations. All of it, gone.

Here is the modern equivalent:

```css
/* GOOD: 2026-era CSS */
.grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(min(100%, 20rem), 1fr));
  gap: 1.5rem;
  padding-inline: clamp(1rem, 5vw, 3rem);
  max-inline-size: 75rem;
  margin-inline: auto;
}
```

That single rule creates a responsive grid that automatically adjusts its column count based on available space, has consistent spacing, centers itself with a maximum width, and uses logical properties that work correctly in both left-to-right and right-to-left languages. No media queries. No column classes. No magic numbers.

Let me break down every modern CSS concept you need to understand.

### Custom Properties (CSS Variables)

CSS custom properties are not just "variables." They are live, inherited, computed values that cascade through the DOM tree. They are the foundation of theming, component APIs, and responsive design in modern CSS.

```css
:root {
  /* Design tokens */
  --color-primary: oklch(55% 0.25 260);
  --color-surface: oklch(98% 0.005 260);
  --color-text: oklch(20% 0.02 260);

  --space-xs: 0.25rem;
  --space-sm: 0.5rem;
  --space-md: 1rem;
  --space-lg: 2rem;
  --space-xl: 4rem;

  --font-body: system-ui, -apple-system, sans-serif;
  --font-mono: ui-monospace, 'Cascadia Code', 'Fira Code', monospace;
  --font-size-base: 1rem;
  --font-size-lg: 1.25rem;
  --font-size-xl: clamp(1.5rem, 1rem + 2vw, 2.5rem);

  --radius-sm: 0.25rem;
  --radius-md: 0.5rem;
  --radius-lg: 1rem;

  --shadow-sm: 0 0.0625rem 0.125rem oklch(0% 0 0 / 0.05);
  --shadow-md: 0 0.25rem 0.5rem oklch(0% 0 0 / 0.1);
}
```

Notice several things:

1. **No pixels.** Every spacing value uses `rem`, which scales with the user's font size preference. A user who sets their browser to 20px base font gets proportionally larger spacing. A user who sets it to 14px gets proportionally smaller spacing. This is accessibility.

2. **`oklch()` colors.** We will cover this in depth later, but `oklch()` is the modern color function that provides perceptually uniform color manipulation. More on this shortly.

3. **`clamp()` for fluid sizing.** The `clamp(minimum, preferred, maximum)` function creates values that smoothly scale with the viewport without breakpoints. `clamp(1.5rem, 1rem + 2vw, 2.5rem)` means "start at 1.5rem, grow with the viewport at a rate of 2vw, but never exceed 2.5rem."

4. **`system-ui` font stack.** This tells the browser to use the operating system's native UI font. On macOS it is San Francisco. On Windows it is Segoe UI. On Android it is Roboto. On Linux it is whatever the user has configured. This is the fastest-loading font option because it requires zero network requests.

Custom properties can be overridden at any level of the DOM tree, which is what makes theming possible:

```css
/* Dark theme */
[data-theme="dark"] {
  --color-surface: oklch(15% 0.01 260);
  --color-text: oklch(90% 0.01 260);
}

/* A specific component can override tokens */
.danger-zone {
  --color-primary: oklch(55% 0.25 25);
}
```

And they can be used in `calc()`:

```css
.stack > * + * {
  margin-block-start: var(--space-md, 1rem);
}

.card {
  padding: var(--space-md);
  border-radius: var(--radius-md);
  background: var(--color-surface);
  box-shadow: var(--shadow-md);
}
```

The `var(--name, fallback)` syntax provides a fallback value if the property is not defined. Always provide fallbacks for component-level custom properties.

### Modern Units: Why Pixels Are Wrong

Pixels (`px`) are a fixed unit. They do not respond to the user's font size setting. They do not respond to the viewport. They do not respond to the container. They are a relic from a time when all monitors were 96 DPI and all users had 20/20 vision.

Here is a complete reference of the units you should use instead:

**`rem`** — Relative to the root element's font size. If the user has set their browser font to 18px, `1rem` is 18px. Use `rem` for spacing, padding, margins, border-radius — anything that should scale with the user's font size preference.

**`em`** — Relative to the current element's font size. Use `em` for things that should scale relative to the text size of the element they are in. For example, the padding inside a button should probably be proportional to the button's text size:

```css
/* GOOD: Button padding scales with button text size */
.btn {
  padding: 0.5em 1em;
  font-size: var(--font-size-base);
  border-radius: 0.25em;
}

.btn-large {
  font-size: var(--font-size-lg);
  /* padding and border-radius automatically scale up */
}
```

**`vw`, `vh`** — 1% of the viewport width/height. Use sparingly, and usually inside `clamp()`:

```css
/* Fluid heading that grows with the viewport */
h1 {
  font-size: clamp(1.75rem, 1rem + 3vw, 3.5rem);
}
```

**`dvh`** — Dynamic viewport height. On mobile browsers, the address bar appears and disappears as the user scrolls. `100vh` is the height when the address bar is hidden, which means content can be clipped when the address bar is visible. `100dvh` is the height of the currently visible viewport, whatever state the address bar is in. Always use `dvh` instead of `vh` when you want something to fill the screen:

```css
/* GOOD: Full-height layout that respects mobile address bars */
.app-shell {
  min-block-size: 100dvh;
  display: grid;
  grid-template-rows: auto 1fr auto;
}
```

**`svh`, `lvh`** — Small viewport height and large viewport height. `svh` is the viewport height when the browser chrome (address bar, toolbar) is fully visible (smallest possible viewport). `lvh` is the viewport height when the browser chrome is fully hidden (largest possible viewport). `dvh` dynamically updates between these two values.

**`ch`** — The width of the "0" character in the current font. Useful for setting content widths:

```css
/* Content that is a comfortable reading width */
.prose {
  max-inline-size: 65ch;
}
```

**`lh`** — The line height of the current element. Useful for spacing that relates to the text rhythm.

**`cqw`, `cqh`** — Container query units. 1% of the container's width/height. We will cover these when we get to container queries.

### `oklch()` — Color That Actually Works

RGB hex codes (`#3b82f6`) are a terrible way to specify color. They are not perceptually uniform — changing the lightness component of an RGB color by the same amount produces wildly different perceived brightness depending on the hue. Try it: `#ff0000` and `#00ff00` are both "maximum saturation" in RGB, but the green looks dramatically brighter to the human eye.

`oklch()` solves this. It is a perceptually uniform color space with three intuitive components:

- **L** (Lightness): 0% is black, 100% is white. A 10% lightness difference looks like the same amount of change regardless of hue.
- **C** (Chroma): 0 is gray, higher values are more vivid. The maximum depends on the gamut (sRGB, P3, Rec.2020).
- **H** (Hue): 0-360 degrees around the color wheel. 0/360 is red, 90 is yellow, 150 is green, 260 is blue, 330 is magenta.

```css
:root {
  --brand: oklch(55% 0.25 260);          /* A vivid blue */
  --brand-light: oklch(75% 0.15 260);    /* Same hue, lighter */
  --brand-dark: oklch(35% 0.20 260);     /* Same hue, darker */
  --brand-muted: oklch(55% 0.08 260);    /* Same hue, less vivid */
  --brand-complement: oklch(55% 0.25 80); /* Opposite hue */
}
```

The killer feature is **relative color syntax**, which lets you derive new colors from existing ones:

```css
:root {
  --brand: oklch(55% 0.25 260);
}

.button {
  background: var(--brand);
  color: oklch(from var(--brand) 98% 0.01 h);

  &:hover {
    background: oklch(from var(--brand) calc(l + 0.1) c h);
  }

  &:active {
    background: oklch(from var(--brand) calc(l - 0.1) c h);
  }
}

/* Generate a complete palette from one color */
.card {
  --card-bg: oklch(from var(--brand) 95% 0.02 h);
  --card-border: oklch(from var(--brand) 85% 0.05 h);
  --card-heading: oklch(from var(--brand) 30% 0.15 h);
}
```

The `oklch(from <color> l c h)` syntax takes the components of the source color and lets you manipulate them with `calc()`. This is how you build an entire design system from a single brand color. No Sass functions. No JavaScript. No build step. The browser does it at paint time.

There is also `color-mix()`, which is Baseline Widely Available and lets you blend two colors:

```css
.overlay {
  /* 50% transparent version of the brand color */
  background: color-mix(in oklch, var(--brand), transparent 50%);
}

.faded {
  /* Mix brand color with gray */
  color: color-mix(in oklch, var(--brand) 30%, gray);
}
```

And `light-dark()`, which returns one of two values depending on the user's color scheme:

```css
:root {
  color-scheme: light dark;
}

body {
  background: light-dark(oklch(98% 0.005 260), oklch(15% 0.01 260));
  color: light-dark(oklch(20% 0.02 260), oklch(90% 0.01 260));
}
```

The `color-scheme: light dark` declaration tells the browser that the page supports both light and dark modes. The `light-dark()` function then returns the first value in light mode and the second in dark mode. This uses the operating system's color scheme preference — no JavaScript media query polling needed.

### CSS Nesting — The End of SCSS

CSS nesting is Baseline Widely Available in 2026. It works in Chrome 120+, Firefox 117+, and Safari 17.2+. It covers over 90% of global browser usage. You do not need Sass, LESS, or any preprocessor for nesting.

```css
/* Native CSS nesting — no preprocessor needed */
.card {
  padding: var(--space-md);
  border-radius: var(--radius-md);
  background: var(--color-surface);
  box-shadow: var(--shadow-sm);

  h2 {
    margin-block: 0 var(--space-sm);
    font-size: var(--font-size-lg);
    color: var(--color-text);
  }

  p {
    margin: 0;
    color: oklch(from var(--color-text) l c h / 0.8);
  }

  &:hover {
    box-shadow: var(--shadow-md);
  }

  &.featured {
    border-inline-start: 0.25rem solid var(--color-primary);
  }

  /* Nest media queries! */
  @media (min-width: 48rem) {
    padding: var(--space-lg);
  }

  /* Nest container queries! */
  @container card (min-width: 30rem) {
    display: grid;
    grid-template-columns: 1fr 2fr;
    gap: var(--space-md);
  }
}
```

The `&` character represents the parent selector. It is optional for descendant selectors (you can write `h2 { ... }` inside `.card { ... }` without `&`), but required when you need to append to the parent: `&:hover`, `&.featured`, `&::before`.

At-rules like `@media`, `@container`, `@supports`, and `@layer` can be nested directly inside a selector block. This keeps responsive styles co-located with the component they affect instead of scattered across separate `@media` blocks at the bottom of the file.

**Why this matters more than it seems:** The number one reason people reached for Sass was nesting. The number two reason was variables. CSS now has both natively. The number three reason was mixins, and native CSS mixins are in active development by the CSS Working Group — the `@mixin` and `@apply` syntax is being prototyped in browsers right now. The preprocessor era is ending.

### `@layer` — Taming the Cascade

The cascade is CSS's most misunderstood feature. Developers fight it with `!important`, deeply specific selectors, and inline styles. The `@layer` at-rule gives you explicit control over cascade priority.

```css
/* Declare layers in priority order (first = lowest priority) */
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
    font-family: var(--font-body);
    font-size: var(--font-size-base);
    line-height: 1.75;
    color: var(--color-text);
    background: var(--color-surface);
  }

  h1, h2, h3, h4, h5, h6 {
    line-height: 1.2;
    text-wrap: balance;
  }
}

@layer components {
  .card { /* ... */ }
  .btn { /* ... */ }
  .nav { /* ... */ }
}

@layer utilities {
  .visually-hidden {
    clip: rect(0 0 0 0);
    clip-path: inset(50%);
    block-size: 1px;
    inline-size: 1px;
    overflow: hidden;
    position: absolute;
    white-space: nowrap;
  }

  .text-center { text-align: center; }
  .flow > * + * { margin-block-start: var(--space-md); }
}
```

The `@layer` declaration at the top (`@layer reset, base, components, utilities;`) establishes the cascade order. Styles in later layers always beat styles in earlier layers, regardless of specificity. This means a simple `.text-center` utility in the `utilities` layer will always override a more specific `.card .content p` rule in the `components` layer.

This is exactly how utility-class systems like Tailwind work — but without the build step, without the generated CSS, and without the thousands of utility classes you never use.

### `@scope` — Real Component Scoping

CSS has always been global. A `.title` class applies everywhere. BEM naming conventions (`.card__title`) and CSS Modules (build-time generated unique class names) were workarounds for this fundamental problem.

`@scope` is the native solution. It is Baseline Newly Available — supported in Chrome, Safari, and Firefox (Firefox 146+).

```css
@scope (.card) {
  :scope {
    padding: var(--space-md);
    border-radius: var(--radius-md);
    background: var(--color-surface);
  }

  .title {
    font-size: var(--font-size-lg);
    font-weight: 700;
  }

  .body {
    color: oklch(from var(--color-text) l c h / 0.8);
  }
}

@scope (.sidebar) {
  .title {
    font-size: var(--font-size-base);
    font-weight: 600;
    text-transform: uppercase;
    letter-spacing: 0.05em;
  }
}
```

Both use `.title`, but they do not conflict. The card's `.title` and the sidebar's `.title` are scoped to their respective containers.

Even more powerful is the "donut scope" — scoping with a lower boundary:

```css
/* Style the card chrome, but NOT its nested content */
@scope (.card) to (.card-content) {
  :scope {
    padding: var(--space-md);
    background: var(--color-surface);
    box-shadow: var(--shadow-sm);
  }

  .title {
    font-weight: 700;
  }

  /* This rule applies to the card's wrapper but NOT to anything
     inside .card-content, even if it also has a .title class */
}
```

The `to` keyword creates a boundary. Styles apply between the scope root (`.card`) and the scope limit (`.card-content`), but not inside the limit. This solves the age-old problem of a parent component accidentally styling the internals of a nested child component.

### CSS Grid — The Layout System

CSS Grid is Baseline Widely Available and has been for years. If you are still using float hacks or twelve-column flexbox grids, stop immediately.

```css
/* A responsive layout with sidebar */
.page {
  display: grid;
  grid-template-columns: minmax(min-content, 16rem) 1fr;
  grid-template-rows: auto 1fr auto;
  grid-template-areas:
    "header  header"
    "sidebar main"
    "footer  footer";
  min-block-size: 100dvh;
}

.page > header  { grid-area: header; }
.page > aside   { grid-area: sidebar; }
.page > main    { grid-area: main; }
.page > footer  { grid-area: footer; }

/* Collapse sidebar on narrow viewports */
@media (max-width: 48rem) {
  .page {
    grid-template-columns: 1fr;
    grid-template-areas:
      "header"
      "main"
      "footer";
  }
  
  .page > aside {
    display: none; /* or move to a popover/drawer */
  }
}
```

The `grid-template-areas` property is one of CSS's most readable features. You literally draw your layout as ASCII art.

For auto-flowing content grids (card layouts, product grids, blog post lists), use `auto-fit` or `auto-fill`:

```css
/* Cards that automatically reflow based on available space */
.card-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(min(100%, 18rem), 1fr));
  gap: var(--space-lg);
}
```

This creates as many columns as will fit, with each column being at least 18rem wide, and all columns being equal width. When the container gets narrow enough that only one column fits, each card takes 100% width. No media queries.

The `min(100%, 18rem)` inside `minmax()` prevents the cards from overflowing on very narrow screens (where `18rem` might be wider than the viewport). `min()` returns the smaller of its two arguments, so on a viewport that is 300px wide, each column is `min(300px, 288px)` = 288px.

**Subgrid** is also available (Baseline Widely Available since late 2023). It lets a child grid align its tracks to its parent grid:

```css
.card-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(min(100%, 18rem), 1fr));
  gap: var(--space-lg);
}

.card {
  display: grid;
  grid-template-rows: auto 1fr auto;
  /* If this card is a grid child, align its rows with siblings */
  grid-row: span 3;
}

/* With subgrid (parent must define row tracks) */
.card-grid-aligned {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  grid-template-rows: repeat(3, auto); /* heading, body, footer */
  gap: var(--space-lg) var(--space-lg);
}

.card-aligned {
  display: grid;
  grid-row: span 3;
  grid-template-rows: subgrid; /* inherit parent's row tracks */
}
```

With `subgrid`, all cards in a row have their headings aligned, their body content aligned, and their footers aligned — even when the content heights differ. This was previously impossible without fixed heights or JavaScript.

### Container Queries — Components That Respond to Their Own Size

Media queries respond to the viewport. Container queries respond to the container. This is the difference between "is the screen narrow?" and "is this card narrow?" — and it changes how you design components.

Container queries are Baseline Widely Available.

```css
/* Define a containment context */
.card-wrapper {
  container-type: inline-size;
  container-name: card;
}

/* Style based on the container's width, not the viewport */
@container card (min-width: 30rem) {
  .card {
    display: grid;
    grid-template-columns: 10rem 1fr;
    gap: var(--space-md);
  }
}

@container card (max-width: 30rem) {
  .card {
    display: flex;
    flex-direction: column;
  }
  
  .card img {
    aspect-ratio: 16 / 9;
    object-fit: cover;
  }
}
```

The card renders as a horizontal layout when its container is wide enough, and a vertical layout when the container is narrow. The same card component adapts whether it is placed in a full-width main content area, a narrow sidebar, or a modal dialog. The card does not need to know where it is — it responds to its own space.

You can also use container query units:

```css
.card-wrapper {
  container-type: inline-size;
}

.card {
  /* Fluid typography based on the container, not the viewport */
  font-size: clamp(0.875rem, 2cqw + 0.5rem, 1.25rem);
  padding: max(1rem, 3cqw);
}
```

`cqw` is 1% of the container's inline size. This makes the card's text and spacing scale with its container width, creating truly self-contained responsive components.

### Anchor Positioning — Tooltips and Menus Without JavaScript

CSS Anchor Positioning lets you tether one element to another. A tooltip tethered to a button. A dropdown menu positioned below a trigger. A floating label anchored to a form field. All of this previously required JavaScript libraries like Popper.js or Floating UI.

Anchor positioning is supported in Chrome and Safari, and is behind a flag in Firefox (expected to ship soon).

```css
/* Define an anchor */
.trigger {
  anchor-name: --menu-trigger;
}

/* Position the menu relative to its anchor */
.menu {
  position: fixed;
  position-anchor: --menu-trigger;
  
  /* Place below the trigger, centered horizontally */
  inset: auto;
  margin: 0;
  position-area: block-end;
  justify-self: anchor-center;
}
```

With the Popover API, you get an implicit anchor for free:

```html
<button popovertarget="tooltip" style="anchor-name: --tip-anchor">
  Hover me
</button>
<div id="tooltip" popover="hint" style="position-anchor: --tip-anchor">
  This is a tooltip
</div>
```

```css
[popover="hint"] {
  margin: 0;
  inset: auto;
  position-area: block-start;
  justify-self: anchor-center;
}
```

The tooltip appears above the button, horizontally centered, in the top layer (so it is never clipped by overflow). When the viewport does not have room above, you can use fallback positions:

```css
.menu {
  position-try-fallbacks: flip-block, flip-inline;
}
```

This tells the browser: if the menu does not fit in its primary position, try flipping it to the other side of the anchor.

### View Transitions

Same-document view transitions are Baseline Newly Available. They let you animate between states of your page with a single API call:

```javascript
function navigateTo(url) {
  if (!document.startViewTransition) {
    // Fallback for browsers without support
    updatePageContent(url);
    return;
  }

  document.startViewTransition(() => {
    updatePageContent(url);
  });
}
```

The browser takes a screenshot of the "old" state, you update the DOM, and the browser crossfades to the "new" state. You can customize the animation with CSS:

```css
::view-transition-old(root) {
  animation: fade-out 0.3s ease-out;
}

::view-transition-new(root) {
  animation: fade-in 0.3s ease-in;
}

@keyframes fade-out {
  to { opacity: 0; }
}

@keyframes fade-in {
  from { opacity: 0; }
}
```

You can assign `view-transition-name` to specific elements to create independent transition groups:

```css
.hero-image {
  view-transition-name: hero;
}

.page-title {
  view-transition-name: title;
}
```

Now the hero image and page title animate independently from the rest of the page — you could morph the hero image to a different size and position while the title slides to a new location, all in a smooth, browser-optimized animation.

### `@starting-style` — Entry Animations

Animating elements from `display: none` to visible used to require JavaScript timing hacks. The `@starting-style` at-rule solves this:

```css
dialog[open] {
  opacity: 1;
  transform: scale(1);
  transition: opacity 0.3s, transform 0.3s, display 0.3s allow-discrete, overlay 0.3s allow-discrete;
}

@starting-style {
  dialog[open] {
    opacity: 0;
    transform: scale(0.95);
  }
}

/* Exit animation */
dialog:not([open]) {
  opacity: 0;
  transform: scale(0.95);
}
```

The `@starting-style` block defines the starting state for entry animations. When the dialog opens, it starts at `opacity: 0; transform: scale(0.95)` and transitions to `opacity: 1; transform: scale(1)`. The `allow-discrete` keyword lets `display` participate in the transition (it changes from `none` to `block` at the start of the transition rather than at the end).

### Scroll-Driven Animations

Scroll-driven animations are reaching cross-browser baseline. They let you tie CSS animations to scroll position instead of time:

```css
/* Fade in elements as they enter the viewport */
.reveal {
  animation: fade-in-up linear;
  animation-timeline: view();
  animation-range: entry 0% entry 100%;
}

@keyframes fade-in-up {
  from {
    opacity: 0;
    transform: translateY(1.25rem);
  }
  to {
    opacity: 1;
    transform: translateY(0);
  }
}

/* Progress bar that fills as the user scrolls the page */
.progress-bar {
  position: fixed;
  inset-block-start: 0;
  inset-inline: 0;
  block-size: 0.25rem;
  background: var(--color-primary);
  transform-origin: left;
  animation: grow-x linear;
  animation-timeline: scroll();
}

@keyframes grow-x {
  from { transform: scaleX(0); }
  to { transform: scaleX(1); }
}
```

The `animation-timeline: view()` ties the animation to the element's intersection with the viewport. The `animation-timeline: scroll()` ties it to the scroll position of the nearest scrolling ancestor (or the document). No JavaScript IntersectionObserver needed. No scroll event listeners. No requestAnimationFrame polling. The browser runs these animations on the compositor thread, meaning they are guaranteed 60fps even on underpowered devices.

**Always respect motion preferences:**

```css
@media (prefers-reduced-motion: reduce) {
  *, *::before, *::after {
    animation-duration: 0.01ms !important;
    animation-iteration-count: 1 !important;
    transition-duration: 0.01ms !important;
    scroll-behavior: auto !important;
  }
}
```

### `text-wrap: balance` and `text-wrap: pretty`

These two properties improve text rendering. `balance` distributes text evenly across lines so that headings do not have a short orphan line at the end:

```css
h1, h2, h3 {
  text-wrap: balance;
}

p {
  text-wrap: pretty;
}
```

`text-wrap: pretty` improves paragraph layout by preventing orphaned words (a single word on the last line) and making more globally optimal line-breaking decisions at the cost of some rendering time. Both are Baseline Widely Available.

---

## Part 4 — JavaScript: The Language, Modernized

### ES Modules — The Import System That Actually Works

Before we write any JavaScript, we need to understand how modern JavaScript modules work in the browser. ES Modules (`import`/`export`) are Baseline Widely Available and have been since 2018. They work natively in every evergreen browser.

```html
<!-- Load a module -->
<script type="module" src="/js/app.js"></script>
```

The `type="module"` attribute is critical. Without it, the browser treats the file as a classic script (global scope, no import/export, synchronous execution that blocks rendering). With it:

- The script is deferred by default (it does not block HTML parsing).
- The script runs in strict mode.
- Each module has its own scope (no global variable pollution).
- `import` and `export` statements work.
- The module is only executed once, even if included in multiple `<script>` tags.
- Top-level `await` is supported.

```javascript
// /js/utils.js
export function formatDate(date) {
  return new Intl.DateTimeFormat('en-US', {
    year: 'numeric',
    month: 'long',
    day: 'numeric',
  }).format(date);
}

export function debounce(fn, ms) {
  let timer;
  return (...args) => {
    clearTimeout(timer);
    timer = setTimeout(() => fn(...args), ms);
  };
}
```

```javascript
// /js/app.js
import { formatDate, debounce } from './utils.js';

console.log(formatDate(new Date())); // "May 2, 2026"
```

Note the `.js` extension in the import path. Browsers do not resolve bare specifiers like `import { foo } from 'some-library'` — that is a Node.js/bundler convention. In the browser, imports must be relative paths (`./`, `../`) or absolute URLs.

**Import Maps** solve this by letting you map bare specifiers to URLs:

```html
<script type="importmap">
{
  "imports": {
    "utils": "/js/utils.js",
    "components/": "/js/components/"
  }
}
</script>

<script type="module">
  // Now bare specifiers work!
  import { formatDate } from 'utils';
  import { Card } from 'components/card.js';
</script>
```

Import maps are Baseline Widely Available. They let you create clean import paths without a bundler.

### ES2026: The Good Parts

ECMAScript 2026 is a significant release. Here are the features that matter most for building applications:

#### The Temporal API — Dates That Finally Work

JavaScript's `Date` object has been broken since 1995. It mutates in place. It has no timezone support. `getMonth()` is zero-indexed (January is 0) but `getDate()` is one-indexed (the first of the month is 1). Parsing date strings is implementation-dependent. It was originally copied from Java 1.0's `java.util.Date`, which Java itself deprecated in 1997.

The Temporal API replaces all of this. It is immutable, timezone-aware, and designed for correctness.

```javascript
// Get the current date and time in a specific timezone
const now = Temporal.Now.zonedDateTimeISO('America/New_York');
console.log(now.toString());
// "2026-05-02T14:30:00-04:00[America/New_York]"

// Create a specific date
const launch = Temporal.PlainDate.from('2026-05-02');
console.log(launch.month); // 5 (not 4!)
console.log(launch.dayOfWeek); // 6 (Saturday; 1 = Monday per ISO 8601)

// Date arithmetic
const nextWeek = launch.add({ days: 7 });
const threeMonthsLater = launch.add({ months: 3 });

// Duration between two dates
const start = Temporal.PlainDate.from('2026-01-01');
const end = Temporal.PlainDate.from('2026-05-02');
const duration = start.until(end);
console.log(duration.toString()); // "P121D" (121 days)
console.log(duration.total('days')); // 121

// Compare dates
Temporal.PlainDate.compare(start, end); // -1 (start is before end)

// Format with Intl (no external library needed)
const formatted = now.toLocaleString('en-US', {
  weekday: 'long',
  year: 'numeric',
  month: 'long',
  day: 'numeric',
  hour: 'numeric',
  minute: '2-digit',
  timeZoneName: 'short',
});
// "Saturday, May 2, 2026, 2:30 PM EDT"
```

Key types in the Temporal API:

- **`Temporal.Instant`** — An exact moment in time (like a Unix timestamp).
- **`Temporal.ZonedDateTime`** — A date and time with a timezone.
- **`Temporal.PlainDate`** — A date without a time (a calendar date).
- **`Temporal.PlainTime`** — A time without a date.
- **`Temporal.PlainDateTime`** — A date and time without a timezone.
- **`Temporal.Duration`** — A length of time.

All Temporal objects are immutable. Methods like `.add()` and `.subtract()` return new objects.

#### Explicit Resource Management — `using` and `await using`

If you have ever forgotten to close a database connection or clean up an event listener, this is for you:

```javascript
class DatabaseConnection {
  #connection;

  constructor(connectionString) {
    this.#connection = connectToDb(connectionString);
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

function getUsers() {
  using db = new DatabaseConnection('postgres://...');
  // db is available here
  const users = db.query('SELECT * FROM users');
  return users;
  // db[Symbol.dispose]() is called automatically when this block exits
  // even if an exception is thrown
}
```

The `using` keyword declares a block-scoped variable that is automatically disposed when it goes out of scope. The object must implement `[Symbol.dispose]()`. There is also `await using` for asynchronous cleanup with `[Symbol.asyncDispose]()`.

This is exactly like C#'s `using` statement. If you are coming from .NET, you already understand this pattern.

#### `Math.sumPrecise()`

Summing floating-point numbers in a loop accumulates rounding errors. `Math.sumPrecise()` avoids this:

```javascript
// BAD: Floating-point accumulation error
const numbers = [0.1, 0.2, 0.3, 0.4, 0.5, 0.6, 0.7, 0.8, 0.9, 1.0];
const naiveSum = numbers.reduce((a, b) => a + b, 0);
console.log(naiveSum); // 4.999999999999999

// GOOD: Precise sum
const preciseSum = Math.sumPrecise(numbers);
console.log(preciseSum); // 5
```

#### `Map.prototype.getOrInsert()`

A common pattern with Maps is "get the value, or insert a default if the key does not exist":

```javascript
// OLD WAY
const cache = new Map();
function getOrCompute(key) {
  if (!cache.has(key)) {
    cache.set(key, expensiveComputation(key));
  }
  return cache.get(key);
}

// NEW WAY (ES2026)
function getOrCompute(key) {
  return cache.getOrInsert(key, expensiveComputation(key));
}

// With a lazy factory (only computes if key is missing)
function getOrCompute(key) {
  return cache.getOrInsertLazy(key, () => expensiveComputation(key));
}
```

#### `Error.isError()`

Reliably checking if something is an Error has been surprisingly hard because `instanceof` breaks across realms (iframes, Web Workers):

```javascript
const real = new TypeError('bad input');
const fake = { name: 'TypeError', message: 'bad input' };

Error.isError(real); // true
Error.isError(fake); // false
```

#### `RegExp.escape()`

Safely escaping user input for use in a regular expression:

```javascript
// OLD WAY
function escapeRegex(str) {
  return str.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
}

// NEW WAY (ES2026)
const userInput = 'Hello. How are you?';
const pattern = new RegExp(RegExp.escape(userInput));
```

### Fetch and AbortController — Network Requests Done Right

The `fetch()` API replaced XMLHttpRequest years ago. Combined with `AbortController`, it gives you complete control over HTTP requests:

```javascript
async function fetchWithTimeout(url, options = {}) {
  const { timeout = 8000, ...fetchOptions } = options;

  const controller = new AbortController();
  const timeoutId = setTimeout(() => controller.abort(), timeout);

  try {
    const response = await fetch(url, {
      ...fetchOptions,
      signal: controller.signal,
    });

    if (!response.ok) {
      throw new Error(`HTTP ${response.status}: ${response.statusText}`);
    }

    return response;
  } finally {
    clearTimeout(timeoutId);
  }
}
```

Even simpler, use `AbortSignal.timeout()`:

```javascript
async function fetchData(url) {
  try {
    const response = await fetch(url, {
      signal: AbortSignal.timeout(5000), // 5 second timeout
    });
    return await response.json();
  } catch (error) {
    if (error.name === 'TimeoutError') {
      console.error('Request timed out');
    } else if (error.name === 'AbortError') {
      console.error('Request was cancelled');
    } else {
      throw error;
    }
  }
}
```

`AbortSignal.any()` composes multiple cancellation reasons:

```javascript
function fetchWithCancel(url, userController) {
  const signal = AbortSignal.any([
    userController.signal,        // User clicked "Cancel"
    AbortSignal.timeout(10000),   // 10 second timeout
  ]);

  return fetch(url, { signal });
}
```

For cleaning up event listeners, `AbortController` is transformative:

```javascript
function setupListeners() {
  const controller = new AbortController();
  const { signal } = controller;

  window.addEventListener('resize', handleResize, { signal });
  window.addEventListener('scroll', handleScroll, { signal });
  document.addEventListener('keydown', handleKeydown, { signal });

  // To remove ALL listeners at once:
  return () => controller.abort();
}

const cleanup = setupListeners();
// ... later ...
cleanup(); // all three listeners removed in one call
```

### `structuredClone()` — Deep Cloning That Works

Before `structuredClone()`, deep cloning an object required `JSON.parse(JSON.stringify(obj))` — which loses `Date` objects, `Map`, `Set`, `RegExp`, and more. Or you used a library like Lodash.

```javascript
const original = {
  date: new Date(),
  map: new Map([['key', 'value']]),
  set: new Set([1, 2, 3]),
  regex: /hello/gi,
  nested: { deep: { data: [1, 2, 3] } },
};

const clone = structuredClone(original);

// The clone is a completely independent deep copy
clone.nested.deep.data.push(4);
console.log(original.nested.deep.data); // [1, 2, 3] — unchanged
console.log(clone.date instanceof Date); // true
console.log(clone.map.get('key')); // "value"
```

`structuredClone()` is Baseline Widely Available. It cannot clone functions or DOM elements, but it handles every serializable data type correctly.

### Proxy and Reflect — Reactive State Without a Framework

The `Proxy` object lets you intercept and customize fundamental operations (property lookup, assignment, enumeration, function invocation). This is how reactive frameworks implement their magic — and you can too:

```javascript
function reactive(target, onChange) {
  return new Proxy(target, {
    set(obj, prop, value) {
      const oldValue = obj[prop];
      if (oldValue !== value) {
        obj[prop] = value;
        onChange(prop, value, oldValue);
      }
      return true;
    },
    get(obj, prop) {
      const value = obj[prop];
      if (value && typeof value === 'object' && !Array.isArray(value)) {
        return reactive(value, onChange);
      }
      return value;
    },
  });
}

// Usage
const state = reactive({ count: 0, user: { name: 'Alice' } }, (prop, newVal, oldVal) => {
  console.log(`${prop} changed from ${oldVal} to ${newVal}`);
  // Re-render the relevant part of the UI
});

state.count = 1;        // "count changed from 0 to 1"
state.user.name = 'Bob'; // "name changed from Alice to Bob"
```

This is a minimal example. A production implementation would handle arrays, avoid unnecessary re-renders, batch updates, and support computed values. But the point stands: the building blocks for reactive state management are in the language.

---

## Part 5 — Web Components: Building a Component System

### Why Web Components?

React has components. Angular has components. Vue has components. Svelte has components. They are all different, incompatible, framework-specific implementations of the same idea: a reusable, encapsulated UI element.

Web Components are the browser's native implementation. A Web Component works in React. It works in Angular. It works in a plain HTML file. It works in your ASP.NET Razor page. It will work twenty years from now.

Web Components are built on three technologies:

1. **Custom Elements** — Define new HTML elements with custom behavior.
2. **Shadow DOM** — Encapsulate styles and markup inside a component.
3. **HTML Templates** — Reusable HTML fragments that are parsed but not rendered.

### A Complete Custom Element

Let me build a real component from scratch — a data table with sorting:

```javascript
// /js/components/sortable-table.js

class SortableTable extends HTMLElement {
  #data = [];
  #columns = [];
  #sortColumn = null;
  #sortDirection = 'asc';
  #shadow;

  constructor() {
    super();
    this.#shadow = this.attachShadow({ mode: 'open' });
  }

  static get observedAttributes() {
    return ['src'];
  }

  async attributeChangedCallback(name, oldValue, newValue) {
    if (name === 'src' && newValue && newValue !== oldValue) {
      try {
        const response = await fetch(newValue, {
          signal: AbortSignal.timeout(5000),
        });
        const json = await response.json();
        this.#data = json.data ?? json;
        this.#columns = json.columns ?? Object.keys(this.#data[0] ?? {});
        this.#render();
      } catch (error) {
        this.#shadow.innerHTML = `<p class="error">Failed to load data: ${error.message}</p>`;
      }
    }
  }

  connectedCallback() {
    this.#render();
  }

  #sort(column) {
    if (this.#sortColumn === column) {
      this.#sortDirection = this.#sortDirection === 'asc' ? 'desc' : 'asc';
    } else {
      this.#sortColumn = column;
      this.#sortDirection = 'asc';
    }

    this.#data.sort((a, b) => {
      const valA = a[column];
      const valB = b[column];

      if (valA == null && valB == null) return 0;
      if (valA == null) return 1;
      if (valB == null) return -1;

      const comparison = typeof valA === 'number'
        ? valA - valB
        : String(valA).localeCompare(String(valB));

      return this.#sortDirection === 'asc' ? comparison : -comparison;
    });

    this.#render();
  }

  #render() {
    const styles = `
      <style>
        :host {
          display: block;
          container-type: inline-size;
        }

        table {
          inline-size: 100%;
          border-collapse: collapse;
          font-family: system-ui, sans-serif;
        }

        th, td {
          padding: 0.75rem 1rem;
          text-align: start;
          border-block-end: 0.0625rem solid oklch(80% 0 0);
        }

        th {
          background: oklch(95% 0.005 260);
          font-weight: 600;
          cursor: pointer;
          user-select: none;
        }

        th:hover {
          background: oklch(90% 0.01 260);
        }

        th .sort-indicator::after {
          content: '';
          margin-inline-start: 0.5em;
        }

        th[aria-sort="ascending"] .sort-indicator::after {
          content: '▲';
        }

        th[aria-sort="descending"] .sort-indicator::after {
          content: '▼';
        }

        tr:hover td {
          background: oklch(97% 0.005 260);
        }

        /* Responsive: stack on narrow containers */
        @container (max-width: 30rem) {
          thead {
            display: none;
          }

          tr {
            display: block;
            padding: 0.75rem;
            border-block-end: 0.125rem solid oklch(80% 0 0);
          }

          td {
            display: flex;
            justify-content: space-between;
            padding: 0.25rem 0;
            border: none;
          }

          td::before {
            content: attr(data-label);
            font-weight: 600;
            margin-inline-end: 1rem;
          }
        }
      </style>
    `;

    const headerCells = this.#columns.map(col => {
      const sortAttr = this.#sortColumn === col
        ? `aria-sort="${this.#sortDirection === 'asc' ? 'ascending' : 'descending'}"`
        : '';
      return `<th ${sortAttr} data-column="${col}">
        ${this.#formatHeader(col)}
        <span class="sort-indicator"></span>
      </th>`;
    }).join('');

    const rows = this.#data.map(row => {
      const cells = this.#columns.map(col =>
        `<td data-label="${this.#formatHeader(col)}">${row[col] ?? ''}</td>`
      ).join('');
      return `<tr>${cells}</tr>`;
    }).join('');

    this.#shadow.innerHTML = `
      ${styles}
      <table role="grid">
        <thead><tr>${headerCells}</tr></thead>
        <tbody>${rows}</tbody>
      </table>
    `;

    // Attach click handlers for sorting
    this.#shadow.querySelectorAll('th[data-column]').forEach(th => {
      th.addEventListener('click', () => {
        this.#sort(th.dataset.column);
      });
    });
  }

  #formatHeader(key) {
    return key
      .replace(/([A-Z])/g, ' $1')
      .replace(/[_-]/g, ' ')
      .replace(/^\w/, c => c.toUpperCase())
      .trim();
  }
}

customElements.define('sortable-table', SortableTable);
```

Usage:

```html
<script type="module" src="/js/components/sortable-table.js"></script>

<sortable-table src="/data/products.json"></sortable-table>
```

That is it. One HTML tag. The component fetches its data, renders a table, handles sorting, responds to its container width for responsive layout, and encapsulates all of its styles. No framework. No build step. No npm packages.

Let me explain the key concepts:

**`class SortableTable extends HTMLElement`** — Every custom element is a class that extends `HTMLElement`. The class name is how you refer to it in JavaScript. The tag name is registered with `customElements.define()` and must contain a hyphen (e.g., `sortable-table`, not `sortabletable`).

**`this.attachShadow({ mode: 'open' })`** — Creates a Shadow DOM for the component. Everything inside the shadow root is encapsulated: styles do not leak out, and external styles do not leak in. The `mode: 'open'` means other JavaScript can access the shadow root via `element.shadowRoot`. Use `mode: 'closed'` for security-sensitive components where you do not want external code inspecting the internals.

**`static get observedAttributes()`** — Returns an array of attribute names that the component watches. When any of these attributes change, the browser calls `attributeChangedCallback()`.

**`connectedCallback()`** — Called when the element is inserted into the DOM. This is where you set up initial rendering, event listeners, and side effects.

**`disconnectedCallback()`** — Called when the element is removed from the DOM. This is where you clean up event listeners and cancel pending requests.

**Private fields (`#data`, `#shadow`)** — The `#` prefix creates truly private fields in JavaScript. They cannot be accessed from outside the class, not even with reflection. This is a language feature, not a convention.

**Container queries inside Shadow DOM** — The `:host` selector targets the custom element itself. By setting `container-type: inline-size` on `:host`, the component's internal styles can respond to the component's own width using `@container` queries.

### Communicating Between Components

Web Components communicate through a combination of attributes, properties, custom events, and slots.

**Attributes** — For configuration passed from HTML:

```html
<sortable-table src="/data/products.json" page-size="25"></sortable-table>
```

**Properties** — For programmatic data passing:

```javascript
const table = document.querySelector('sortable-table');
table.data = myDataArray; // set a JavaScript property directly
```

**Custom Events** — For notifying parent code of things that happened:

```javascript
// Inside the component
this.dispatchEvent(new CustomEvent('row-selected', {
  detail: { row: selectedRow },
  bubbles: true,
  composed: true, // allows the event to cross the shadow DOM boundary
}));

// Outside the component
table.addEventListener('row-selected', (event) => {
  console.log('Selected:', event.detail.row);
});
```

**Slots** — For projecting content into a component:

```javascript
class Card extends HTMLElement {
  constructor() {
    super();
    const shadow = this.attachShadow({ mode: 'open' });
    shadow.innerHTML = `
      <style>
        :host {
          display: block;
          padding: var(--card-padding, 1.5rem);
          border-radius: 0.5rem;
          background: var(--card-bg, white);
          box-shadow: 0 0.0625rem 0.25rem oklch(0% 0 0 / 0.1);
        }
        ::slotted(h2) {
          margin: 0 0 0.75rem;
        }
      </style>
      <slot name="header"></slot>
      <slot></slot>
      <slot name="footer"></slot>
    `;
  }
}
customElements.define('my-card', Card);
```

```html
<my-card>
  <h2 slot="header">Card Title</h2>
  <p>This content goes in the default slot.</p>
  <div slot="footer">
    <button>Action</button>
  </div>
</my-card>
```

Named slots (`<slot name="header">`) receive elements with a matching `slot` attribute. The default slot (`<slot>` without a name) receives all other children.

Notice the CSS custom properties on `:host` — `var(--card-padding, 1.5rem)` and `var(--card-bg, white)`. These create a public styling API for the component. External CSS can customize the card by setting these custom properties, without the external styles leaking into the component's internal structure:

```css
/* External CSS — does NOT leak into the shadow DOM,
   but custom properties DO penetrate the shadow boundary */
my-card {
  --card-padding: 2rem;
  --card-bg: oklch(97% 0.01 260);
}
```

This is the correct way to theme Web Components. CSS custom properties are the only CSS values that cross the shadow DOM boundary. Everything else is encapsulated.

---

## Part 6 — A Client-Side Router Without a Framework

Single-page applications need a router. Let us build one:

```javascript
// /js/router.js

class Router {
  #routes = new Map();
  #outlet = null;
  #notFound = null;

  constructor(outlet) {
    this.#outlet = typeof outlet === 'string'
      ? document.querySelector(outlet)
      : outlet;

    window.addEventListener('popstate', () => this.#resolve());

    // Intercept link clicks
    document.addEventListener('click', (event) => {
      const link = event.target.closest('a[href]');
      if (!link) return;

      const url = new URL(link.href);
      if (url.origin !== location.origin) return; // external link
      if (link.hasAttribute('download')) return;
      if (link.target === '_blank') return;

      event.preventDefault();
      this.navigate(url.pathname);
    });
  }

  route(path, handler) {
    this.#routes.set(path, handler);
    return this;
  }

  notFound(handler) {
    this.#notFound = handler;
    return this;
  }

  navigate(path) {
    if (path !== location.pathname) {
      history.pushState(null, '', path);
    }
    this.#resolve();
  }

  async #resolve() {
    const path = location.pathname;

    // Try exact match first
    let handler = this.#routes.get(path);

    // Try pattern matching
    if (!handler) {
      for (const [pattern, h] of this.#routes) {
        const params = this.#matchPattern(pattern, path);
        if (params) {
          handler = () => h(params);
          break;
        }
      }
    }

    if (!handler && this.#notFound) {
      handler = this.#notFound;
    }

    if (handler) {
      const content = await handler();
      if (typeof content === 'string') {
        // Use view transitions if available
        if (document.startViewTransition) {
          document.startViewTransition(() => {
            this.#outlet.innerHTML = content;
          });
        } else {
          this.#outlet.innerHTML = content;
        }
      } else if (content instanceof Node) {
        if (document.startViewTransition) {
          document.startViewTransition(() => {
            this.#outlet.replaceChildren(content);
          });
        } else {
          this.#outlet.replaceChildren(content);
        }
      }
    }

    // Update active nav links
    document.querySelectorAll('a[href]').forEach(link => {
      const url = new URL(link.href);
      link.classList.toggle('active', url.pathname === path);
      if (url.pathname === path) {
        link.setAttribute('aria-current', 'page');
      } else {
        link.removeAttribute('aria-current');
      }
    });
  }

  #matchPattern(pattern, path) {
    const patternParts = pattern.split('/').filter(Boolean);
    const pathParts = path.split('/').filter(Boolean);

    if (patternParts.length !== pathParts.length) return null;

    const params = {};
    for (let i = 0; i < patternParts.length; i++) {
      if (patternParts[i].startsWith(':')) {
        params[patternParts[i].slice(1)] = decodeURIComponent(pathParts[i]);
      } else if (patternParts[i] !== pathParts[i]) {
        return null;
      }
    }
    return params;
  }

  start() {
    this.#resolve();
    return this;
  }
}

export { Router };
```

Usage:

```javascript
// /js/app.js
import { Router } from './router.js';

const router = new Router('#content');

router
  .route('/', () => `
    <h1>Home</h1>
    <p>Welcome to the application.</p>
  `)
  .route('/products', async () => {
    const response = await fetch('/data/products.json');
    const products = await response.json();
    return `
      <h1>Products</h1>
      <sortable-table></sortable-table>
    `;
  })
  .route('/products/:id', async (params) => {
    const response = await fetch(`/data/products/${params.id}.json`);
    const product = await response.json();
    return `
      <h1>${product.name}</h1>
      <p>${product.description}</p>
      <p>Price: $${product.price}</p>
    `;
  })
  .notFound(() => `
    <h1>404</h1>
    <p>Page not found.</p>
    <a href="/">Go home</a>
  `)
  .start();
```

This router:
- Intercepts link clicks and uses `history.pushState()` instead of full page loads.
- Handles the browser's back/forward buttons via the `popstate` event.
- Supports URL parameters (`:id`).
- Uses view transitions when available.
- Updates `aria-current="page"` on navigation links for accessibility.
- Supports both string HTML and DOM node return values from route handlers.

---

## Part 7 — State Management with Custom Events

For non-trivial applications, you need a way to share state between components. Here is a minimal but functional store:

```javascript
// /js/store.js

class Store extends EventTarget {
  #state;

  constructor(initialState = {}) {
    super();
    this.#state = structuredClone(initialState);
  }

  get state() {
    return structuredClone(this.#state);
  }

  setState(updater) {
    const prevState = this.#state;
    const nextState = typeof updater === 'function'
      ? updater(structuredClone(prevState))
      : { ...prevState, ...updater };

    this.#state = nextState;

    this.dispatchEvent(new CustomEvent('change', {
      detail: { state: structuredClone(nextState), prevState },
    }));
  }

  subscribe(listener) {
    this.addEventListener('change', listener);
    return () => this.removeEventListener('change', listener);
  }

  select(selector) {
    return selector(this.state);
  }
}

export { Store };
```

Usage:

```javascript
import { Store } from './store.js';

const store = new Store({
  products: [],
  cart: [],
  filter: '',
  loading: false,
});

// Subscribe to changes
const unsubscribe = store.subscribe((event) => {
  const { state } = event.detail;
  renderProductList(state.products, state.filter);
  renderCart(state.cart);
});

// Update state
async function loadProducts() {
  store.setState({ loading: true });
  const response = await fetch('/data/products.json');
  const products = await response.json();
  store.setState({ products, loading: false });
}

function addToCart(product) {
  store.setState((state) => ({
    ...state,
    cart: [...state.cart, product],
  }));
}
```

The key design decisions:

1. **`structuredClone()` everywhere** — State is always deeply cloned when read or when passed to the updater function. This prevents external code from mutating the store's internal state. It is the same principle as Redux's immutable state, enforced mechanically rather than by convention.

2. **`EventTarget` as the base class** — The store *is* an event target. Components can subscribe and unsubscribe using the standard DOM event API. No custom subscription system needed.

3. **Functional updater** — `setState` accepts either an object (merged with the current state) or a function that receives the current state and returns the new state. The function form is safer when multiple updates might be in flight.

---

## Part 8 — Putting It All Together: A Complete Application

Let me show you the structure of a complete, zero-dependency application:

```
my-app/
├── index.html
├── css/
│   ├── tokens.css          ← Design tokens (custom properties)
│   ├── reset.css            ← Modern CSS reset
│   ├── base.css             ← Typography, body styles
│   ├── layouts.css          ← Grid systems, page layouts
│   └── utilities.css        ← Utility classes
├── js/
│   ├── app.js               ← Entry point, router setup
│   ├── store.js             ← State management
│   ├── router.js            ← Client-side router
│   └── components/
│       ├── sortable-table.js
│       ├── modal-dialog.js
│       ├── toast-notification.js
│       └── theme-switcher.js
└── data/
    └── products.json
```

The `index.html`:

```html
<!DOCTYPE html>
<html lang="en">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <title>My App</title>

  <link rel="stylesheet" href="/css/tokens.css">
  <link rel="stylesheet" href="/css/reset.css">
  <link rel="stylesheet" href="/css/base.css">
  <link rel="stylesheet" href="/css/layouts.css">
  <link rel="stylesheet" href="/css/utilities.css">

  <script type="module" src="/js/app.js"></script>
  <script type="module" src="/js/components/sortable-table.js"></script>
  <script type="module" src="/js/components/theme-switcher.js"></script>
</head>
<body>
  <header>
    <a href="/" class="logo">My App</a>
    <nav aria-label="Main">
      <ul>
        <li><a href="/">Home</a></li>
        <li><a href="/products">Products</a></li>
        <li><a href="/about">About</a></li>
      </ul>
    </nav>
    <theme-switcher></theme-switcher>
  </header>

  <main id="content">
    <!-- Router renders content here -->
  </main>

  <footer>
    <p><small>Built with zero dependencies.</small></p>
  </footer>
</body>
</html>
```

Every file is a plain text file. You can serve this from any static file server — GitHub Pages, Nginx, Apache, Caddy, or even `python -m http.server`. No build step. No compilation. No bundling.

For development, open the HTML file in your browser. Edit a CSS file, save it, refresh. Edit a JavaScript file, save it, refresh. That is the development workflow. It is faster than any hot-module-replacement setup because there is no compilation step between saving and seeing the result.

---

## Part 9 — What About Angular?

The prompt for this article asked for Angular examples. Let me show you something interesting: modern Angular (version 21, as of early 2026) has been moving toward the exact same patterns we have been building by hand.

Angular's evolution over the past few years tells a story. In Angular 14, they introduced standalone components — components that do not need `NgModule`. In Angular 16, they introduced signals — reactive primitives that track state changes. In Angular 19, standalone became the default, and signals matured. In Angular 20, signals became stable and Zone.js became optional. In Angular 21, they introduced Signal Forms and Angular Aria for headless, accessible components.

Here is what a modern Angular component looks like:

```typescript
// Angular 21 — standalone, signal-based, zoneless
import { Component, signal, computed } from '@angular/core';

@Component({
  selector: 'app-counter',
  template: `
    <div>
      <p>Count: {{ count() }}</p>
      <p>Double: {{ double() }}</p>
      <button (click)="increment()">+1</button>
    </div>
  `,
})
export class CounterComponent {
  count = signal(0);
  double = computed(() => this.count() * 2);

  increment() {
    this.count.update(c => c + 1);
  }
}
```

Notice what happened: Angular adopted the same reactive signal pattern that the vanilla JavaScript `Proxy` example from Part 4 demonstrates. Angular adopted standalone components that are conceptually similar to Web Components. Angular adopted `effect()` which is conceptually similar to event subscriptions.

The framework is converging toward the platform. The patterns you learn in this article — reactive state, component encapsulation, declarative rendering — are the same patterns Angular (and React, and Vue, and Svelte) use internally. The difference is that when you learn them at the platform level, you understand why frameworks make the choices they do, and you can work effectively with any framework — or none at all.

But here is the key insight: **Angular requires npm, a build step, TypeScript compilation, and a bundler.** None of those things are bad. For large team projects with hundreds of components and complex routing, that tooling is genuinely helpful. But for many applications — blogs, documentation sites, small business websites, internal tools, prototypes, personal projects — the overhead of that toolchain is not justified by the benefits.

This is not "Angular bad, vanilla good." This is "choose the right tool for the problem." A hammer is an excellent tool. It is not the right tool for every problem.

---

## Part 10 — The Vendor Prefix Hall of Shame

The rules of this article explicitly forbid vendor prefixes. Let me show you what bad code looks like, so you can recognize it and excise it from your codebases.

```css
/* BAD: Vendor prefixes — do not write this in 2026 */
.box {
  -webkit-transform: rotate(45deg);
  -moz-transform: rotate(45deg);
  -ms-transform: rotate(45deg);
  transform: rotate(45deg);

  -webkit-transition: all 0.3s ease;
  -moz-transition: all 0.3s ease;
  -o-transition: all 0.3s ease;
  transition: all 0.3s ease;

  display: -webkit-flex;
  display: -ms-flexbox;
  display: flex;

  -webkit-user-select: none;
  -moz-user-select: none;
  -ms-user-select: none;
  user-select: none;

  background: -webkit-linear-gradient(red, blue);
  background: -moz-linear-gradient(red, blue);
  background: linear-gradient(red, blue);
}
```

```css
/* GOOD: Just write the standard property. It works in every browser. */
.box {
  transform: rotate(45deg);
  transition: all 0.3s ease;
  display: flex;
  user-select: none;
  background: linear-gradient(red, blue);
}
```

Every prefixed property in that "bad" example has been unprefixed and Baseline Widely Available for years. `transform` since 2015. `transition` since 2015. `flex` since 2015. `user-select` since 2022. `linear-gradient` since 2013.

If you see vendor prefixes in a codebase in 2026, it means one of two things: the code was written a decade ago and never cleaned up, or it was generated by an outdated Autoprefixer configuration. Either way, remove them. They add bytes to your CSS, they confuse junior developers, and they serve zero purpose in evergreen browsers.

**The one exception:** If you are a browser vendor implementing an experimental feature that is not yet standardized, the prefix tells developers "this might change." But you are not a browser vendor. You are writing application CSS. You should never type a prefix character.

---

## Part 11 — Performance Without a Bundler

"But without a bundler, won't my application be slow? Won't the browser have to make dozens of HTTP requests for all those individual files?"

In HTTP/1.1, yes, that was a real problem. Each request required a separate TCP connection (or waited in a queue for a reused connection), and the overhead per request was significant.

In HTTP/2 (which every modern web server supports), multiple requests are multiplexed over a single connection. The overhead per additional request is negligible. HTTP/3 (QUIC) improves this further by eliminating head-of-line blocking at the transport layer.

That said, here are the performance practices that matter:

**Use `<link rel="modulepreload">` for critical JavaScript modules:**

```html
<link rel="modulepreload" href="/js/app.js">
<link rel="modulepreload" href="/js/router.js">
<link rel="modulepreload" href="/js/store.js">
```

This tells the browser to fetch and parse these modules before they are needed. Without this, the browser discovers modules one import at a time (it loads `app.js`, parses it, discovers it imports `router.js`, loads that, parses it, discovers it imports something else, and so on). With `modulepreload`, all critical modules are fetched in parallel.

**Use `loading="lazy"` on images below the fold:**

```html
<img src="hero.jpg" alt="Hero" width="1200" height="600">
<!-- ^ above the fold: load immediately -->

<img src="feature.jpg" alt="Feature" loading="lazy" width="600" height="400">
<!-- ^ below the fold: load when near the viewport -->
```

**Use `fetchpriority` to hint at resource priority:**

```html
<img src="hero.jpg" alt="Hero" fetchpriority="high">
<link rel="stylesheet" href="/css/tokens.css" fetchpriority="high">
<script type="module" src="/js/analytics.js" fetchpriority="low"></script>
```

**Use `content-visibility: auto` for long lists:**

```css
.card {
  content-visibility: auto;
  contain-intrinsic-size: auto 12rem;
}
```

This tells the browser that it can skip rendering cards that are far from the viewport. The `contain-intrinsic-size` provides an estimated size so the scrollbar position remains accurate. This is the browser's native virtual scrolling.

**Compress your responses.** If you are serving from a web server, enable gzip or Brotli compression. Plain text files (HTML, CSS, JavaScript) compress extremely well — typically 70-80% size reduction.

---

## Part 12 — What Is Coming Next

The web platform is not done. Here are the features in active development that you should watch for:

### CSS Mixins (`@mixin` and `@apply`)

Native CSS mixins are being prototyped. They will let you define reusable blocks of declarations:

```css
/* Proposed syntax — not yet in browsers */
@mixin --center {
  display: flex;
  align-items: center;
  justify-content: center;
}

.hero {
  @apply --center;
  min-block-size: 50dvh;
}
```

This eliminates the last major reason people use Sass.

### CSS `if()`

Conditional logic in CSS property values:

```css
/* Proposed syntax */
.box {
  background: if(style(--variant: primary), var(--color-primary), var(--color-surface));
}
```

### Cross-Document View Transitions

View transitions that work across full page navigations (not just SPA state changes):

```css
@view-transition {
  navigation: auto;
}
```

Navigate from one HTML page to another and the browser animates the transition. This works for multi-page applications served by the same origin.

### CSS Masonry Layout

Pinterest-style layouts without JavaScript:

```css
.gallery {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(15rem, 1fr));
  grid-template-rows: masonry;
  gap: 1rem;
}
```

Items of different heights pack into columns without gaps. The implementation approach is still being debated between `grid-template-rows: masonry` and `display: masonry`, but the feature is coming.

### Customizable `<select>` Elements

Style the inside of a `<select>` dropdown — something that has been impossible for the entire history of the web:

```css
select, ::picker(select) {
  appearance: base-select;
}

select option {
  padding: 0.5rem 1rem;
  display: flex;
  align-items: center;
  gap: 0.5rem;
}
```

This is shipping in Chrome now and expected to be widely available by late 2026.

---

## Part 13 — Resources

Here are the authoritative references for everything covered in this article:

- **MDN Web Docs** — The comprehensive reference for every web API, CSS property, and HTML element: [developer.mozilla.org](https://developer.mozilla.org)
- **web.dev** — Google's web development resource with guides, courses, and Baseline tracking: [web.dev](https://web.dev)
- **web.dev/baseline** — Track which features are Baseline Newly Available and Baseline Widely Available: [web.dev/baseline](https://web.dev/baseline)
- **Can I Use** — Browser support tables for every web platform feature: [caniuse.com](https://caniuse.com)
- **CSS Wrapped 2025** — Chrome DevRel's annual roundup of CSS features that shipped: [chrome.dev/css-wrapped-2025](https://chrome.dev/css-wrapped-2025/)
- **TC39 Proposals** — The official repository of ECMAScript proposals from Stage 0 to Stage 4: [github.com/tc39/proposals](https://github.com/tc39/proposals)
- **Temporal API Documentation** — The comprehensive Temporal API reference: [tc39.es/proposal-temporal/docs](https://tc39.es/proposal-temporal/docs/)
- **Web Components examples** — MDN's repository of Web Component examples: [github.com/mdn/web-components-examples](https://github.com/mdn/web-components-examples)
- **CSS Snapshot 2026** — The W3C's official snapshot of all current CSS specifications: [w3.org/TR/css-2026](https://www.w3.org/TR/css-2026/)
- **Open Web Docs** — Community-maintained documentation for the web platform: [openwebdocs.org](https://openwebdocs.org)
- **State of CSS 2025** — Annual survey of CSS feature usage and awareness: [2025.stateofcss.com](https://2025.stateofcss.com)
- **Frontend Masters Blog — What to Know in JavaScript (2026 Edition)** — A comprehensive overview of the JavaScript ecosystem: [frontendmasters.com/blog/what-to-know-in-javascript-2026-edition](https://frontendmasters.com/blog/what-to-know-in-javascript-2026-edition/)

The web platform in 2026 is not the web platform you remember from 2016. It is faster, more capable, more accessible, and more beautiful. It does not need your npm packages. It does not need your build step. It does not need your framework.

It just needs you to learn what it can do.
