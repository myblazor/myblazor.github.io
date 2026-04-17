---
title: "Without a Net, Part 1: Overview — Why a Plain Browser Is Enough in 2026"
date: 2026-05-24
author: myblazor-team
summary: "We just spent seven articles dissecting React. Now the honest next question — do you actually need any of that? This is the opening post of a fifteen-part series on building real, responsive, accessible, offline-capable web applications in 2026 using only what evergreen browsers ship: plain HTML, plain CSS, plain JavaScript. No npm. No Node. No bundler. No framework. We frame the series, define what 'evergreen' means in 2026, tour the capabilities browsers now expose natively, and set expectations for the fourteen posts that follow."
featured: true
tags:
  - javascript
  - css
  - html
  - web-standards
  - no-build
  - vanilla
  - first-principles
  - series
  - deep-dive
---

## Introduction: The Question We Kept Not Asking

For seven long articles we wrote about React. We compared it to Blazor WebAssembly. We built a tiny Virtual DOM clone. We traced `useEffect` through its stale-closure hell and mapped it to `OnAfterRenderAsync`. We talked about Vite. We talked about Fiber. We talked about prop drilling, Redux, Zustand, Jotai, signals, and the inability of the React Context API to behave the way `IServiceCollection` does.

And at no point in those seven articles did we stop and ask the question that the reader kept almost asking, silently, in the margins, every time we described another layer of tooling:

> **Do I actually need any of this?**

That is the question we are going to spend the next fifteen days answering. Honestly. From first principles. With working code. With no hand-waving.

This is Part 1 of a fifteen-part series titled **Without a Net**. The premise is simple, and it is the premise that the modern web has been quietly making true for about five years now: **an evergreen browser in 2026 ships enough capability natively that, for a very wide class of applications, you do not need a build step at all.** No npm. No Node.js. No bundler. No TypeScript compiler. No Tailwind. No Vite. No framework. No polyfills. You open a text file, you type HTML and CSS and JavaScript into it, you double-click, and a real application happens.

Before anyone writes an angry comment: we are not saying React is bad. We are not saying frameworks are bad. We spent seven articles taking React seriously precisely because it is a serious, well-designed tool that solves real problems. But a tool is a tool because it matches a job. The goal of this series is to teach you what the browser gives you for free in 2026 so that, when you are sitting in a meeting where somebody says "we need to add React because we want to build a settings page with a master-detail view and a dark mode toggle," you are equipped to say — politely, patiently, without smugness — *we might not need to.*

And in the cases where we do need a framework, you will be able to say *why* with specificity, instead of defaulting to the framework because you do not know what the alternative even looks like.

This opening article has one job: **frame the series and tell you what we are going to build.** We will tour the platform. We will define our target. We will define our non-goals. We will tell you what you will know by the end, and we will be honest about what we are not going to cover. If at the end of this post you decide this series is not for you, that is a completely legitimate outcome and we will not be offended.

Let us begin.

## Part 1: What "Evergreen Browser" Means in 2026

The word *evergreen* has been used loosely for about a decade. Officially it means a browser that auto-updates itself, quietly, in the background, without the user having to do anything. In practice, for the purposes of this series, when we say "evergreen browser" we mean exactly three browsers running on their latest production versions:

1. **Google Chrome**, on desktop (Windows, macOS, Linux, ChromeOS) and mobile (Android).
2. **Apple Safari**, on desktop (macOS) and mobile (iOS, iPadOS).
3. **Mozilla Firefox**, on desktop (Windows, macOS, Linux) and mobile (Android).

We also implicitly cover:

- **Microsoft Edge**, because since January 2020 Edge has been a Chromium-based browser, meaning it shares a rendering engine with Chrome. If Chrome supports it, Edge supports it.
- **Brave, Opera, Arc, Vivaldi, and friends.** All Chromium.
- **Samsung Internet.** Chromium.
- **Any browser on iOS.** Apple requires all browsers on iOS to use WebKit, which is the same engine Safari uses. Chrome on iPhone is Safari with a different UI.

What we are **not** covering:

- Internet Explorer. It does not exist anymore. Microsoft retired it in June 2022.
- Legacy Edge (the pre-2020, non-Chromium version). Also gone.
- Any version of any browser older than about 18 months.
- Headless or unusual runtime environments like WebViews in old Android phones unless explicitly called out.

If you are thinking *wait, we support IE 11 at my job* — we hear you, we have been there, our condolences. This series is not for that job. This series is for the blog you are starting next weekend, the internal tool that will be used by the eight people on your team, the side project you want to deploy to GitHub Pages, or the modernisation effort where you have been given the freedom to drop legacy support and you want to know what the actual minimum viable stack looks like.

There is a formal definition of "evergreen" that we will lean on throughout this series: **Baseline**. Baseline is a cross-browser compatibility initiative run by the W3C's WebDX Community Group. It tracks features and classifies them into three buckets:

- **Limited availability** — not yet in all four core browser sets (Chrome, Edge, Firefox, Safari).
- **Newly available** — supported in the latest version of all four core browsers, but only recently. Interoperability is good today, but 18-month-old devices might not have it.
- **Widely available** — has been supported in all four core browsers for at least 30 months.

[Baseline is maintained by web.dev and the WebDX Community Group](https://web.dev/baseline). When this series refers to "safe to use today," we mean Baseline Widely Available or Baseline Newly Available with a graceful fallback. When we discuss something that is only partially supported, we will say so loudly and tell you what to do about it.

One concrete example of how far the web has come: **container queries**, the long-awaited ability for CSS to ask "how wide is my *parent*, not my window?" — the feature Ethan Marcotte was pleading for in 2011 — became Baseline Newly Available in February 2023, and crossed the Baseline Widely Available threshold in August 2025. It is available in every current browser. We will use it heavily.

Another example: the **Popover API**, which lets any HTML element become a popover with a single attribute, no JavaScript needed for open/close behaviour, no focus management to write, no click-outside-to-close to implement — [became Baseline Newly Available in January 2025](https://web.dev/baseline). We will use it in Day 10.

Another: **same-document View Transitions** — the ability to animate between DOM states with a single API call instead of reaching for Framer Motion — reached Baseline Newly Available status in October 2025. Every current browser has it. We will build a master-detail view with it in Day 11.

The web did not sit still while we were all importing `classnames` from npm. It got dramatically better. This series is your chance to catch up.

## Part 2: Who This Series Is For

Our imagined reader is the same developer we have been writing to for the last thirty articles: an ASP.NET developer, probably writing C# by day, probably inherited some legacy Web Forms code, probably knows HTML and CSS well enough to get by but has never really *studied* them. You have written JavaScript — mostly jQuery, probably, and more recently some fragments of vanilla JS inside Blazor's `JSInterop` calls. You have used Bootstrap. You have maybe tried Tailwind once and felt vaguely guilty about the class list. You have heard of React but you have not shipped anything in it. You have heard of Web Components but you think they are "not production ready" because somebody on Hacker News said so in 2018.

Every one of those reference points is outdated. We are going to update them.

What we are going to assume:

- **You can read C# and are comfortable with strongly-typed, object-oriented code.** If a concept is hard in JavaScript, we will usually explain it by contrasting with how it works in C#.
- **You know what a browser is and you have opened Chrome DevTools at least once.** If you have not, open it right now: F12 on Windows, Option-Cmd-I on macOS. Keep it open for the next two weeks.
- **You have a text editor.** VS Code, JetBrains Rider, Visual Studio, Notepad++ — any of them is fine. We will not be using any editor-specific features.
- **You have seen HTML and CSS before.** We will cover both from first principles, but we will move briskly.

What we are **not** assuming:

- That you know JavaScript. We will teach the language from scratch where we need to, using a dialect that ASP.NET developers will recognise.
- That you know modern CSS. If your mental model of CSS stops at `float: left`, you are in the right place.
- That you have ever run `npm install`. Many of our readers never have. We are going to teach you a web that never requires you to.

We are also not assuming that you believe us when we say this is all possible. Part of the job of this series is to *show*, not tell. Every article will have a complete, runnable example you can paste into a single `.html` file, double-click, and watch happen in your browser. No installation steps. No `package.json`. No `dotnet restore`. Double-click, browser opens, thing works.

## Part 3: What We Will Build

Over the course of the next fifteen days, we will build — piece by piece — a complete, production-quality web application. Not a toy. Not a TodoMVC. A real thing. Specifically, we are going to build a **blog reader** with the following features:

1. A home page with a list of posts, each with title, summary, author, and reading time.
2. A detail view for each post with Markdown rendered to HTML.
3. Full-text search.
4. Tag filtering with container-query-driven responsive layouts.
5. Light and dark mode with automatic detection and a manual toggle.
6. Smooth view transitions between pages (no jank, no blank flash).
7. Keyboard navigation throughout.
8. Full WCAG 2.2 AA accessibility.
9. An offline mode that works entirely without a network connection.
10. Installable as a Progressive Web App.
11. A full RSS feed.
12. A "compose post" page with a real form, real validation, and real draft-saving.
13. Zero external dependencies, zero npm, zero build step, zero `package.json`, zero config files of any kind.

If you are thinking *that sounds suspiciously like the Blazor WASM app this magazine runs on* — yes, exactly. The point of the exercise is to demonstrate that the same application can be built without .NET, without Blazor, without WebAssembly, and in a smaller total download. That does not make Blazor wrong; we will have a long, honest discussion about the trade-offs in Day 15. But it means a prospective user of our Blazor code has an alternative, and we have a responsibility to show them what it looks like.

We will release the final application's source code at the end of the series. One folder. No hidden build outputs. Every file human-readable. You will be able to open any of them in a text editor and understand what it does.

## Part 4: The Fifteen Days, at a Glance

Here is the road map. Bookmark this page. We will link back to it from every subsequent article.

**Day 1 (today): Overview.** Why we are doing this, what evergreen means, who this is for, what we will build.

**Day 2: Semantic HTML and the Document Outline.** HTML is not markup for making things look right. It is a typed language for expressing document structure. We will cover landmarks, headings, lists, the `<dialog>` element, the `<details>/<summary>` disclosure widget, form controls, and ARIA (and when NOT to use it). If you have ever written `<div class="button">` instead of `<button>`, this day is for you.

**Day 3: The Cascade, Specificity, and `@layer`.** Modern CSS has solved the organisation problem that caused a generation of frameworks. We will cover the cascade from first principles, explain specificity without reciting the "0,0,1,0" incantation from memory, and show how `@layer` and `@scope` let you build maintainable CSS at scale without BEM, CSS Modules, or CSS-in-JS.

**Day 4: Modern CSS Layout.** Flexbox, Grid, Subgrid. The end of Bootstrap's twelve-column grid. We will reimplement the entire Bootstrap responsive grid in nine lines of CSS Grid and then argue that you probably do not want a twelve-column grid anyway.

**Day 5: Responsive Design in 2026.** Container queries (not media queries) are the primary responsive tool now. We will cover `clamp()` for fluid typography, the `:has()` selector for parent-aware styles, and intrinsic design principles that make responsive work without per-breakpoint tweaking.

**Day 6: Colour, Typography, and Motion.** `oklch()` for perceptually uniform colour. `light-dark()` for automatic theme support. Variable fonts for loading one font file that covers every weight. View Transitions for free animated page changes. `prefers-reduced-motion` for users who need it.

**Day 7: Native ES Modules.** The quiet change that made everything else possible. Since 2018 every evergreen browser can do `import` and `export` natively. We will cover import maps, dynamic `import()`, and explain why a `<script type="module">` tag is the only bundler you need for most projects.

**Day 8: The DOM, Events, and Platform Primitives.** `querySelector`, event delegation, the event object, `addEventListener` options you never read, `AbortController` for cancelling fetches, `MutationObserver`, `IntersectionObserver`, `ResizeObserver`. The platform primitives that replaced half of jQuery.

**Day 9: State Without a Library.** Proxies, signals built from scratch, the Observer pattern in 40 lines. Not a reimplementation of Redux — a genuinely simpler model that works for the vast majority of applications.

**Day 10: Web Components.** Custom Elements, Shadow DOM, templates, slots, and the Declarative Shadow DOM. We will build a reusable card component, a tabs component, and a modal component, all of which work without any framework and can be dropped into any HTML page anywhere.

**Day 11: Client-Side Routing and View Transitions.** The Navigation API replaces the old History API with something sane. View Transitions make routing animations free. We will build an SPA router in about 100 lines of JavaScript.

**Day 12: Forms and the Constraint Validation API.** The `<form>` element is the most under-appreciated component on the web. It handles submission, validation, error reporting, and autocomplete. We will build a complex form with client-side validation that works without JavaScript and gets enhanced with it.

**Day 13: Storage, Service Workers, and Offline.** `localStorage`, `IndexedDB`, the Cache API, service workers, Background Sync, and installable PWAs. By the end of this article our blog reader works fully offline and installs on a user's home screen on iOS and Android.

**Day 14: Accessibility, Performance, and Security.** Lighthouse scores. Core Web Vitals. Focus management. ARIA when and only when you need it. Content Security Policy. Trusted Types. The things no framework can hide from you.

**Day 15: Conclusion and Capstone.** A complete, working blog reader in one folder. A decision framework for when to reach for a framework anyway. A final reckoning with the trade-offs. And, because we are fair to the alternative, a direct side-by-side comparison with the Blazor WASM equivalent.

## Part 5: What a "No-Build" Development Loop Actually Looks Like

A reader who has been writing Blazor or React or Angular for a while is going to have an immediate, almost physical reaction to the claim that we do not need a build step. Hot Module Replacement is nice. TypeScript catches bugs. Minification reduces bundle size. Tree shaking removes dead code. Linting prevents errors. A bundle size report makes you look responsible in performance reviews. All of these are real. We are going to acknowledge each of them honestly as we go.

But a lot of build tooling exists to compensate for the gaps browsers used to have — gaps that are now filled. Let us walk through a concrete no-build loop so you can see what we mean.

Create a folder called `hello`. Inside it, create one file called `index.html`:

```html
<!doctype html>
<html lang="en">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <title>Hello, Platform</title>
  <style>
    body {
      font-family: system-ui, sans-serif;
      max-width: 40rem;
      margin: 2rem auto;
      padding: 0 1rem;
      line-height: 1.6;
      color: light-dark(#111, #eee);
      background: light-dark(#fff, #111);
      color-scheme: light dark;
    }
    button {
      font: inherit;
      padding: 0.5rem 1rem;
      border-radius: 0.25rem;
      border: 1px solid currentColor;
      background: transparent;
      color: inherit;
      cursor: pointer;
    }
  </style>
</head>
<body>
  <h1>Hello, Platform</h1>
  <p>The current time is <output id="clock">—</output>.</p>
  <button id="stop">Stop</button>

  <script type="module">
    const output = document.getElementById("clock");
    const button = document.getElementById("stop");
    const formatter = new Intl.DateTimeFormat(undefined, {
      hour: "2-digit",
      minute: "2-digit",
      second: "2-digit"
    });

    const interval = setInterval(() => {
      output.textContent = formatter.format(new Date());
    }, 1000);

    button.addEventListener("click", () => {
      clearInterval(interval);
      button.disabled = true;
      button.textContent = "Stopped";
    });
  </script>
</body>
</html>
```

Save that file. Double-click it. Your browser opens. There is a heading, a clock that updates every second, a button that stops the clock. The page respects your operating system's dark mode preference automatically, because of `color-scheme: light dark` and `light-dark()`. It uses your operating system's default font, because of `font-family: system-ui`. It formats the time according to your locale, because of `new Intl.DateTimeFormat(undefined, ...)`. It works on every evergreen browser, on every platform, with zero installation and zero configuration.

That is an application. It is not a large application, but it is a real one. It makes use of at least six features — `<meta name="viewport">`, `system-ui` fonts, `color-scheme`, `light-dark()`, `Intl.DateTimeFormat`, `<script type="module">` — that would have required a polyfill, a build step, or a library when we were writing our first jQuery plugin ten years ago.

The edit loop is equally simple. Change something. Save. Refresh the browser. Done. No `dotnet watch`. No `npm run dev`. No Vite. No HMR. Refresh is free — it takes less than 50 milliseconds on a warm cache, because the browser is not recompiling anything. Your reload is the entire build.

For more complex projects we will want at least a simple static server, because some browser features (ES modules from the filesystem in some browsers, service workers everywhere) require an HTTP origin. The smallest possible local server is a single command:

```bash
# Python 3 — already installed on macOS and most Linux
python3 -m http.server 8080

# Node.js — if you happen to have it
npx serve

# .NET 10 — the one you already have as an ASP.NET dev
dotnet tool install --global dotnet-serve
dotnet serve -p 8080
```

Any of those will give you a local server at `http://localhost:8080`. No config files. Nothing to install beyond the runtime you already have.

This is the development loop for the entire series. Edit. Save. Refresh. We will not install a single npm package. We will not write a single `package.json`. We will not use TypeScript, or Sass, or PostCSS, or Webpack, or Rollup, or Vite, or esbuild, or Parcel, or Turbopack, or Rolldown. If you are wondering "but how will I get IntelliSense?" — VS Code gives you HTML, CSS, and JavaScript IntelliSense out of the box, for free, based entirely on the [TypeScript language server reading your plain JavaScript files](https://code.visualstudio.com/docs/languages/javascript). You do not need to use TypeScript the language to get the TypeScript language server's help in JavaScript.

## Part 6: The Honest Trade-offs

We are not going to spend fifteen days pretending that a plain-browser workflow is strictly superior to a framework. It is not. Frameworks solve real problems and they solve them well. We want to be specific about what you are giving up so that when you make the decision, you make it with your eyes open.

**What you give up by going no-build:**

1. **Compile-time type checking.** Plain JavaScript does not have it. You can get most of the way there with JSDoc comments (we will cover this in Day 7), but it is weaker than TypeScript.

2. **Dead code elimination for your own code.** When you `import` a module, the whole module loads even if you only use one function. Tree-shaking is a bundler feature. For a blog reader this does not matter. For a 300-module application it might.

3. **Automatic minification.** Browsers do not minify your code for you. Your JavaScript is shipped as you wrote it. This adds bytes but gains readability — "view source" on your site actually shows your source.

4. **Compile-time errors for typos in import paths.** A missing module is a runtime 404, not a build failure. With good DevTools and a habit of testing in an actual browser, this is a papercut, not a wound.

5. **Single-command build artefacts.** You cannot run `npm run build` and get an optimised `dist/`. What you ship is what you wrote. For static sites this is fine. For enterprise apps with hundreds of routes it may not be.

6. **The ecosystem.** If you need a charting library, a date picker library, or a PDF generator, you either find one that works as an ES module served from a CDN (many do — `unpkg`, `esm.sh`, `skypack`, `jsdelivr`), write your own, or give up. The default answer in the framework world is `npm install`. The default answer in our world is "is there a way the browser can do this natively?" followed by "if not, is there a vanilla library that ships as ES modules?"

**What you gain:**

1. **Radical simplicity.** New developer arrives on Monday. Git clone. Double-click. Application runs. Total onboarding time: under 5 minutes. There is no lock file to be stale, no `node_modules` to be wrong, no Node version mismatch, no `engines` field, no `.nvmrc`, no npm registry outage.

2. **Longevity.** An HTML file you wrote in 2015 using nothing but the platform still works, exactly as written, in 2026. A React 15 application from 2015 does not. A Webpack 3 build from 2015 does not. Platform code has an asymptotically longer shelf life than framework code. If you look at this site's Blazor posts, they will still render from Markdown in ten years; so will the capstone we build in this series.

3. **Smaller downloads.** The capstone application at the end of this series will be around 40 kilobytes gzipped — HTML, CSS, and JavaScript combined — before any of our content loads. The Blazor WASM version of the same app loads several megabytes of runtime. That difference matters on a flaky train connection.

4. **Smaller cognitive surface.** You learn the platform, once. That knowledge transfers between every project, every team, every year. Framework knowledge expires. Three years of React knowledge in 2020 is partially obsolete in 2026 because hooks, Server Components, the app router, and concurrent rendering have all fundamentally shifted the mental model. MDN in 2020 looks almost identical to MDN in 2026 because `addEventListener` still works the same way it did.

5. **No supply-chain risk.** No npm package can be compromised if you do not install any npm packages. No `left-pad` incident can break your build if you do not have a build. No dependabot alerts. No `resolutions` field. No `overrides`. No yarn-audit-tangerine.

6. **Every skill you learn transfers to every other web project.** You cannot be a React developer who has never heard of the DOM. But you can be a React developer whose DOM knowledge is weak and whose CSS is filtered through styled-components. That is a career risk. Learning the platform makes you bulletproof across every framework.

None of this means "do not use frameworks." It means: **know what the platform does natively, and reach for a framework when it solves a problem the platform does not.** Today, in 2026, the platform solves many more problems than it did even two years ago.

## Part 7: A Brief Tour of Everything the Browser Now Ships

To calibrate expectations, here is a partial list of capabilities that are Baseline Newly Available or Widely Available in 2026. We are not going to explain all of these — we will get to most of them in the coming fourteen posts — but the size of the list is the point.

**HTML elements and attributes:**

- `<dialog>` with modal and non-modal behaviour. Replaces 90 percent of uses of JavaScript modal libraries.
- `<details>` and `<summary>` for disclosure widgets. Replaces every "show more" accordion you ever wrote.
- `<search>` landmark for accessible search regions.
- `popover` attribute. Any element can become a popover with no JavaScript. Baseline Newly Available January 2025.
- `inert` attribute for hiding subtrees from assistive technology without CSS tricks.
- `loading="lazy"` on images and iframes. Native lazy-loading.
- `<input type="search">`, `<input type="date">`, `<input type="color">`, `<input type="range">`. Dozens of input types that work natively on mobile and desktop.
- `<picture>` and `<source>` for responsive images and modern image formats (AVIF, WebP) with fallbacks.
- `importmap` in `<script type="importmap">` — lets you alias import specifiers without a bundler.

**CSS features:**

- Flexbox (Baseline Widely Available since 2020).
- Grid, including Subgrid (Subgrid Widely Available since 2024).
- Container queries (Widely Available August 2025).
- `:has()` selector (Widely Available since late 2024).
- Native CSS nesting — no Sass required.
- `@layer` for explicit cascade layers.
- `@scope` for scoped styles.
- Custom properties (CSS variables).
- `color-mix()` and the full `<color>` system including `oklch`, `oklab`, `color()`, `lab()`, and `lch()`.
- `light-dark()` and the `color-scheme` property.
- `clamp()`, `min()`, `max()` for fluid values.
- Anchor positioning (in Interop 2025, shipping across browsers).
- View Transitions (same-document Baseline since October 2025).
- `@property` for typed custom properties.
- `@starting-style` for enter animations.
- Scroll-driven animations.
- `text-wrap: balance` and `text-wrap: pretty`.
- Logical properties (`margin-inline`, `padding-block`, etc.) for international support.

**JavaScript — ECMAScript itself:**

- ES Modules with `import` and `export`.
- Top-level `await` in modules.
- Classes, private fields (`#foo`), decorators.
- Async/await, generators, async generators.
- Promises, including `Promise.any`, `Promise.allSettled`, `Promise.withResolvers`.
- `Array.prototype.toSorted`, `toReversed`, `toSpliced`, `with`.
- `Object.groupBy`, `Map.groupBy`.
- `structuredClone()` for deep cloning.
- `Error.cause` for error chaining.
- `Error.isError()` for robust error detection.
- `WeakRef` and `FinalizationRegistry`.
- Private methods.
- Logical assignment operators (`||=`, `??=`, `&&=`).
- Nullish coalescing (`??`) and optional chaining (`?.`).
- Numeric separators (`1_000_000`).
- Regex `/d` flag for match indices, named capture groups, lookbehind.

**Web APIs:**

- `fetch()` with streaming bodies and `AbortController`.
- Service Workers.
- Cache API.
- IndexedDB.
- WebSockets, WebRTC, WebTransport (Interop 2026).
- Web Components: Custom Elements, Shadow DOM, `<template>`, `<slot>`.
- `IntersectionObserver`, `MutationObserver`, `ResizeObserver`, `PerformanceObserver`.
- The Clipboard API.
- The Navigation API (Chromium; Firefox is catching up).
- The Web Share API.
- `navigator.locks` for cross-tab coordination.
- `BroadcastChannel` for cross-tab messaging.
- The File System Access API (Chromium).
- WebAuthn for passwordless authentication.
- Web Crypto for cryptographic primitives.

**Progressive Web App features:**

- Manifest files for installable apps.
- Service workers for offline.
- Push API.
- Background Sync.
- Periodic Background Sync.
- Badging API.
- Protocol handlers.

**Internationalisation:**

- The full `Intl` namespace: `DateTimeFormat`, `NumberFormat`, `RelativeTimeFormat`, `ListFormat`, `DisplayNames`, `PluralRules`, `Segmenter`, `Locale`, and more.
- Full support for non-Gregorian calendars.
- Locale-aware collation and case conversion.

**Things still rolling out in 2026 that we will mention with caveats:**

- **Temporal.** The replacement for the `Date` object. Shipped in Chrome 144 (January 2026) and Firefox 139 (May 2025). As of April 2026 it is **not yet in Safari's production releases** — it is in Safari Technical Preview but not mainline. We will teach it in Day 8 but note the compatibility caveat and offer the [official polyfill](https://github.com/js-temporal/temporal-polyfill) as a drop-in. [Temporal reached TC39 Stage 4 in March 2026](https://github.com/tc39/proposal-temporal) and is now part of ES2026.
- **Cross-document View Transitions.** Chrome 126+ and Safari 18.2+. Firefox added partial support in 146 (early 2026). We will teach both the same-document version (fully Baseline) and the cross-document version (nearly Baseline) in Day 11.
- **Anchor positioning.** In Interop 2025; Chrome shipped first, Safari and Firefox followed. We will use it sparingly.

That is — to be clear — a partial list. Every one of those items used to require a library. Now they are a tag, an attribute, or a function call.

## Part 8: The Four Files We Are Not Writing

To underscore how different this series is from a typical modern-web tutorial, let us enumerate the files that a comparable React project would require that we will never produce.

**`package.json`.** We will not have one. There is nothing to install.

**`package-lock.json` or `yarn.lock` or `pnpm-lock.yaml`.** We will not have one. There is nothing to lock.

**`node_modules/`.** We will not have one. Not ever. Not anywhere.

**`tsconfig.json`.** We will not have one. We are not using TypeScript. Plain JavaScript with JSDoc comments gets us 90% of the IntelliSense benefit without the compiler.

**`vite.config.js`, `webpack.config.js`, `rollup.config.js`, `esbuild.config.js`.** None. There is no bundler to configure.

**`.babelrc` or `babel.config.js`.** None. We are targeting browsers that already understand our JavaScript.

**`postcss.config.js`, `tailwind.config.js`.** None. We are using plain CSS.

**`jest.config.js`, `vitest.config.ts`.** We will cover testing in Day 14. We will use something native to the browser.

**`.eslintrc.js`, `.prettierrc`.** Optional. You can use them if you like. Nothing in our code depends on them.

**`next.config.js`, `astro.config.js`, `svelte.config.js`.** Obviously none.

The root of our project, at the end of the series, will contain approximately eight files: `index.html`, a stylesheet, a few JavaScript modules, a service worker, a web app manifest, a few content files, and a README. That is the whole application. You will be able to fit the entire project tree in your head.

Compare that to a typical modern React project, which can easily ship with a hundred files of configuration and boilerplate before a single line of application code is written.

## Part 9: How We Will Teach

A quick note on methodology, because fifteen days is a long time and we do not want anybody to get lost.

**Each article is self-contained.** You can skip around if you want. Day 7 does not strictly require Day 6. The capstone in Day 15 will show how everything fits, but the individual days are useful on their own.

**Each article has three sections:**

1. **A story.** We open every article with a scenario — a bug we hit in production, a question a colleague asked, an argument we had in a code review. The story motivates the material. If you do not care about the material, the story at least tells you why we thought you should.

2. **The full technical content.** Rigorous. Exhaustive. Every relevant option, every relevant API, every relevant caveat. We will not summarise. We will not say "there are several options here and you can look them up." We will list every option and explain every one.

3. **Practical recommendations.** We will not just describe features. We will tell you which ones to actually use, which to avoid, and why. We will give opinions. We will defend them.

**Every code example is complete.** No pseudocode. No "assume the rest." Every example either fits in a single HTML file you can double-click or a set of files you can place in a folder. We will give you the exact file names, the exact file contents, the exact commands, and the exact expected outputs.

**We will cite sources.** Version numbers, feature availability dates, specification references, and performance claims will all be backed up with links to MDN, web.dev, the WHATWG specs, or primary-source blog posts from browser teams. We will never make up a fact. When we are uncertain about something — and we will be uncertain about some things — we will say so.

**We will be kind.** This is a patient series. If you find yourself behind, re-read. Email us. Skip ahead and come back. Nobody is keeping score.

## Part 10: The React Comparison Won't Go Away

Because we just spent seven articles on React, some of you will be expecting direct comparisons on every page. We will do this, but in moderation. Reaching for a React analogy every time we introduce a new concept would be both tedious and slightly unfair — the idea of this series is to teach the platform as its own coherent thing, not as "React but without React."

That said, here is a quick lookup table for the React developers in the audience. We will flesh this out through the series.

| React concept | Native equivalent | Article |
|---|---|---|
| Component | Custom Element / module-level function | Day 10 |
| JSX | `<template>` + `innerHTML` or Custom Elements | Day 10 |
| `useState` | A plain variable + `set` function over a store; or `Proxy`; or signals | Day 9 |
| `useEffect` | `connectedCallback` / `disconnectedCallback` on a Custom Element | Day 10 |
| `useMemo` | A plain memoised function | Day 9 |
| `useCallback` | A function reference held in a closure | Day 9 |
| `useRef` | A plain variable; or `element.querySelector` | Day 8 |
| `useContext` | Custom Events bubbling up; or a module-level store | Day 9 |
| Redux / Zustand | A module-level store class with `addEventListener` | Day 9 |
| React Router | The Navigation API + a ~100-line router | Day 11 |
| `<Suspense>` | `<template>`, `<slot>`, and `loading="lazy"` | Day 10 |
| Server Components | Plain HTML served from the CDN; hydrated islands | Day 15 |
| CSS-in-JS | Plain CSS with `@layer` and custom properties | Day 3 |
| Tailwind | Plain CSS with custom properties and `@scope` | Day 3, Day 6 |
| `react-query` | `fetch` + a tiny cache in a `Map` | Day 8 |
| `framer-motion` | View Transitions | Day 11 |
| `react-hook-form` | `<form>` + Constraint Validation API | Day 12 |

Every row in that table is a claim the rest of the series will defend.

## Part 11: Deployment — Because It Matters

One thing that sold us on this approach is deployment. If you have ever tried to deploy a .NET or Node application to a serverless host, you know the drill: you need a runtime, you need environment variables, you need a Dockerfile or a specific host, you need a CI pipeline, and you need to pay attention to cold starts, memory limits, and egress costs.

A no-build web application deploys by copying files. That's it. Every static host on the internet supports this.

- **GitHub Pages.** Free. Commit to a `gh-pages` branch or use the `actions/deploy-pages` workflow. This is how this very magazine is deployed. Five minutes of setup.
- **Cloudflare Pages.** Free tier. Unlimited bandwidth. Best-in-class CDN.
- **Netlify.** Free tier. Drag-and-drop deployment from the UI.
- **Vercel.** Free tier.
- **S3 + CloudFront.** If you are on AWS. Pennies per month for small sites.
- **Azure Static Web Apps.** If you are on Azure. Free tier.
- **Your own nginx server.** Copy files to `/var/www/html`. Done.
- **A USB stick.** This is slightly a joke but not entirely — a fully static site can run from any file server, including a literal local filesystem mount, as long as you serve it over HTTP.

At the end of this series we will commit the capstone application to this magazine's repository, push it, and GitHub Actions will deploy it. The deployment workflow for a no-build static site is about 15 lines of YAML.

## Part 12: What You Should Do Right Now

If you are committed to following along, here are three preparation steps to take before Day 2:

**Step one: pick an editor.** If you do not have a favourite, install VS Code. It is free, works on every platform, and has great support for HTML, CSS, and JavaScript out of the box.

**Step two: open three browsers.** Install Firefox and Chrome if you do not have them. On macOS, you also have Safari. On Linux, Safari is unavailable, but Firefox and Chrome cover the vast majority of what we will do. Keep all three open as you work; cross-check your work in each.

**Step three: learn your DevTools shortcut.** F12 (or Option-Cmd-I on macOS) opens DevTools. The **Elements** panel shows the live DOM and applied CSS. The **Console** shows your `console.log` output and lets you run JavaScript against the live page. The **Network** panel shows every HTTP request. The **Sources** panel lets you debug JavaScript with breakpoints. The **Application** panel shows storage, service workers, and the web app manifest. You will use all of these constantly. They are more than sufficient for everything we will do.

**Bonus step: find a quiet Saturday morning.** This series is long. Each post takes 60 to 120 minutes to read carefully, and another hour or two if you type along. If you cram it, you will retain nothing. Space it out.

## Part 13: A Note on Honesty

Some of you have noticed — perhaps while reading the React series — that this magazine has a bias. We are called *My Blazor Magazine*. We run on Blazor WebAssembly. We like .NET. Our name is on the masthead.

We are going to spend fifteen days telling you that you do not need our stack for a large class of applications.

We are doing this on purpose. A magazine that exists only to advocate for its own stack is not a magazine, it is a marketing brochure. We want our readers to make informed choices, and the informed choice sometimes is "do not use Blazor." Day 15 of this series will include an honest comparison, and we will not pull our punches. Blazor has genuine strengths — C#, strong typing, shared models with your backend, Razor templating, dependency injection — and genuine weaknesses — larger downloads, longer initial compile times, more tooling complexity, a smaller native ecosystem. The right choice depends on your team, your audience, your constraints, and your time budget. We want you to have the information to decide.

If at the end of this series you decide "my side project is just a blog, I am going to build it without Blazor," we consider that a win. We would rather have a well-informed reader who sometimes uses a different stack than a loyal one who never leaves.

## Part 14: A Small Acknowledgement

Before we close, we want to acknowledge the people and projects whose work made this series possible.

The **WHATWG** and the **W3C**, the standards bodies that have spent decades negotiating between browser vendors to make the modern web interoperable. **MDN Web Docs**, which is the single most valuable resource for a working web developer, and which you should bookmark right now. **web.dev** and the **Chrome DevRel** team, whose Baseline initiative has done more for web developer productivity than any tool in the last five years. **The WebKit**, **Gecko**, and **Blink** engineering teams, who are often in competition but whose collaboration through Interop has made the web actually work across browsers. **Alex Russell** and **Jen Simmons**, whose public criticism of framework-dominated web development pushed the platform to improve. **The TC39 committee** and especially **Philipp Dunkel**, **Maggie Johnson-Pint**, **Matt Johnson-Pint**, **Brian Terlson**, **Shane Carr**, **Ujjwal Sharma**, **Philip Chimento**, **Jason Williams**, and **Justin Grant**, who spent the better part of a decade designing the [Temporal API](https://github.com/tc39/proposal-temporal) so that `Date` can finally retire.

Without any of them this series would have to be ten thousand words shorter and start with "first, install Node."

## Part 15: Tomorrow

Tomorrow's article — **Day 2: Semantic HTML and the Document Outline Nobody Taught You** — will do something that will make seasoned web developers roll their eyes before we begin: it will spend 10,000 words on HTML.

Here is the thing. In the ASP.NET Web Forms era, the "HTML" we wrote was mostly server controls. `<asp:Button>` turned into `<input type="submit">`. `<asp:GridView>` turned into a `<table>`. We never had to think about semantics because the framework did it for us. Then we moved to MVC, then Razor, and we wrote our own HTML, and we got comfortable with `<div>` and `<span>` because that was what the CSS frameworks assumed. And then we wrote apps for a decade that were technically accessible but structurally indistinguishable from a giant bag of divs.

Modern HTML has 115 elements. We use maybe 15 of them. The other 100 exist for reasons. Some of them replace features we have been reinventing in JavaScript for years. `<dialog>` is a modal. `<details>` is an accordion. `<search>` is an accessible search region. `<output>` is a live region for computed values. Every one of these elements is a component that already exists, that screen readers understand, that keyboards navigate correctly, that does not require a single line of JavaScript, and that works the same way in every browser from the last five years.

We are going to cover all of them. We are going to cover the heading outline. We are going to cover landmarks. We are going to cover forms and every input type. We are going to cover ARIA, and, crucially, we are going to cover *when not to use ARIA*, because using ARIA to recreate something HTML already does is the single most common accessibility bug in the wild.

If you have ever written a JavaScript modal with a z-index of 9999, Day 2 is going to give you back an afternoon.

## Closing

Here is the spirit we want to bring to this series, in one paragraph:

The web platform is, in 2026, one of the largest and most carefully-designed application platforms ever built. It is free, open, and cross-platform. It is maintained by a rotating cast of some of the best engineers in the world, paid by some of the largest companies in the world, negotiated in the open, specified in excruciating detail, and tested against tens of thousands of compatibility tests every week. It is possible to write beautiful, useful, accessible, performant applications on this platform using nothing but what the browser ships. Frameworks are fine. We use them. We write about them. But the platform is the foundation under every framework, and a working developer who understands the platform is unfairly more powerful than one who only understands a framework that sits on top of it.

Over the next fourteen days we are going to make you that developer.

See you tomorrow.

---

## Series navigation

You are reading **Part 1 of 15**.

- **Part 1 (today): Overview — Why a Plain Browser Is Enough in 2026**
- Part 2 (tomorrow): Semantic HTML and the Document Outline Nobody Taught You
- Part 3: The Cascade, Specificity, and `@layer`
- Part 4: Modern CSS Layout — Flexbox, Grid, Subgrid, and Container Queries
- Part 5: Responsive Design in 2026 — Container Queries, `clamp()`, Fluid Type, and `:has()`
- Part 6: Colour, Typography, and Motion
- Part 7: Native ES Modules
- Part 8: The DOM, Events, and Platform Primitives
- Part 9: State Management Without a Library
- Part 10: Web Components — Custom Elements, Shadow DOM, Templates, and Slots
- Part 11: Client-Side Routing with the Navigation API and View Transitions
- Part 12: Forms, Validation, and the Constraint Validation API
- Part 13: Storage, Service Workers, and Making a Plain HTML App Work Offline
- Part 14: Accessibility, Performance, and Security
- Part 15: The Conclusion — A Complete Application, End to End, in a Single `<script type="module">`

See also our companion seven-part series on React for ASP.NET developers:

- [The Elephant in the Room, Part 1: Overview](/blog/2026-05-17-react)
- [The Elephant in the Room, Part 2: The Virtual DOM vs the Render Tree](/blog/2026-05-18-react)
- [The Elephant in the Room, Part 3: Escaping the useEffect Trap](/blog/2026-05-19-react)
- [The Elephant in the Room, Part 4: The Polyglot Front-End](/blog/2026-05-20-react)
- [The Elephant in the Room, Part 5: State of the Union](/blog/2026-05-21-react)
- [The Elephant in the Room, Part 6: Build Pipelines and Bundlers](/blog/2026-05-22-react)
- [The Elephant in the Room, Part 7: The Conclusion](/blog/2026-05-23-react)

## Resources

- [MDN Web Docs](https://developer.mozilla.org/) — the single most important bookmark for a web developer.
- [web.dev Baseline](https://web.dev/baseline) — the definitive tracker for cross-browser feature availability.
- [Can I Use](https://caniuse.com/) — granular per-version browser support tables.
- [The WHATWG HTML Living Standard](https://html.spec.whatwg.org/) — the HTML specification itself.
- [The W3C CSS specifications](https://www.w3.org/Style/CSS/) — the CSS specifications.
- [The TC39 proposals](https://github.com/tc39/proposals) — upcoming JavaScript language features.
- [Interop 2026](https://webkit.org/blog/17818/announcing-interop-2026/) — the browser-vendor agreement on which features to prioritise this year.
- [WebKit Feature Status](https://webkit.org/status/) — what Safari is shipping and when.
- [Firefox Platform Status](https://firefox-source-docs.mozilla.org/) — what Firefox is shipping.
- [Chrome Status](https://chromestatus.com/) — what Chrome is shipping.
- [HTTPArchive's 2025 Web Almanac](https://almanac.httparchive.org/) — annual state-of-the-web report.
