---
title: "Without a Net, Part 11: Client-Side Routing with the Navigation API and View Transitions"
date: 2026-06-03
author: myblazor-team
summary: "Day 11 of our fifteen-part no-build web series builds a complete client-side router from scratch, using the brand-new Navigation API that just became Baseline Newly Available in January 2026. We cover the full mental model — what client-side routing is, why MPAs deserve a fair hearing, the Navigation API replacing fifteen years of pushState wrangling, and how to combine it with View Transitions for truly app-like navigation. Two complete worked routers — a ~120-line SPA router with shared-element transitions and a zero-JavaScript MPA approach — and an honest decision framework for choosing between them."
tags:
  - javascript
  - navigation-api
  - routing
  - spa
  - mpa
  - view-transitions
  - history
  - no-build
  - first-principles
  - series
  - deep-dive
---

## Introduction: The Router That Was 12,000 Lines

A few years ago we joined a team midway through a project. The codebase was a mid-sized React application — about 80,000 lines, a couple of dozen pages, some good engineering but also some accumulated cruft. One of the first things we did, as we always do, was open `package.json` and read every dependency. Roughly halfway down was something called `react-router`, version 6.4. We knew about React Router by reputation but had not used the version that introduced the "data router" model.

We pulled up the documentation. It was extensive. Loaders. Actions. Defer. Await. Form components. ScrollRestoration. ClientLoader vs Loader. createBrowserRouter. createHashRouter. createMemoryRouter. RouterProvider. Outlet. Link. NavLink. useNavigate. useLocation. useNavigation. useFetcher. useFetchers. useFormAction. useResolvedPath. useMatches. useMatch. useNavigationType. useSubmit. useRevalidator. useRouteError. errorElement. ErrorBoundary. shouldRevalidate. handle. lazy. defer. await. Form. fetcher.Form. fetcher.submit. fetcher.load. unstable_HistoryRouter. We counted the exported symbols. There were 84 of them.

The team's actual use of the router was: navigate to one of about 25 pages when the user clicks a link, and call back to a function when the URL changes. Two operations. Eighty-four exports. The library itself, with its dependencies, was about 60 kilobytes minified. The team had a senior engineer assigned half-time to "router upgrades" because every minor version was breaking. The router was the most consistently expensive single dependency in the project.

About a year after we joined, we had a conversation about replacing the router with about 100 lines of plain JavaScript. The proposal was rejected — for entirely defensible reasons, we should add. The team had React-shaped thinking about routing, the existing tests were tied to React Router idioms, and switching would be a multi-month project for marginal gain. Sometimes the right answer is "live with what you have." Code costs money to change.

But we still wonder, occasionally, what the world would look like if the project had started with no router at all. If the team had simply written `addEventListener("click", handleLink)` and `addEventListener("popstate", render)` from day one, and grown the routing logic as needs emerged. Our guess: it would never have grown beyond about 200 lines. There would have been no library to upgrade. The senior engineer's half-time slot would have gone toward features.

This is the story of every framework router. It is also the story of why people genuinely fear writing a router themselves. Routing has a reputation for being "subtle" — for having edge cases that bite you when you least expect them. The defensible position has been: "use the library; it has handled the edge cases." The defensible position is now wrong, because in January 2026, [the Navigation API became Baseline Newly Available across all major browsers](https://web.dev/blog/baseline-navigation-api). The Navigation API is the browser team admitting, finally, that the History API was the wrong tool for SPA routing, and shipping the right one. Combined with View Transitions (Day 6), it makes a complete client-side router achievable in about 120 lines.

Day 11 is our walk through both kinds of routing — single-page (SPA) and multi-page (MPA) — and an honest case for when each is appropriate. We will build a complete SPA router from scratch using the new Navigation API, then build the MPA equivalent in three lines of CSS, then compare the trade-offs honestly. By the end you will have a strong mental model for routing, a working router file you can adapt, and the confidence to choose the right architecture for the next project.

This is Part 11 of 15 in our no-build web series. We have HTML, CSS, modules, DOM primitives, state, and components. Today we cover the navigation that ties them into a real application.

## Part 1: What "Routing" Actually Means

A web application has multiple "screens" — a list view, a detail view, a settings page, a profile page. The user moves between them. Routing is the answer to two coupled questions:

1. **How does the URL change to reflect the current screen?**
2. **How does the screen change when the URL changes?**

In a traditional **multi-page application** (MPA), the browser handles both. Click a link, the browser navigates to a new URL, fetches the HTML for that URL, replaces the document. The address bar updates. The back button works. Each "screen" is its own HTML file or its own server-rendered response. This is how the web worked from 1991 to about 2010, and how a great many sites still work, perfectly well.

In a **single-page application** (SPA), JavaScript intercepts navigation. Click a link, JavaScript changes the URL via `history.pushState()` (or, in 2026, `navigation.navigate()`), updates the DOM in place, and prevents the browser from reloading. The address bar updates as if the navigation had happened. The back button works because the history API integrates with browser history. This is the model that React, Vue, Svelte, Angular, and every framework router targets.

A **hybrid** sits in the middle: the first page load is a full document fetch (so search engines and slow connections get something fast), but subsequent navigations within the app are handled by JavaScript. This is the common production pattern in 2026, and it is what we will build today.

Both models are legitimate. Both have trade-offs. The bias for SPA in modern web development is partly genuine merit (smoother transitions, less white-flash between pages), partly framework momentum (the framework has a router; you use it), and partly a habit nobody has questioned in a decade. We are going to question it.

## Part 2: A Brief, Honest History Of SPA Routing

The story of how we got to the Navigation API is short but illustrative.

**1995–2010.** The History API does not exist. SPA-like behaviour is achieved via URL fragments (`/page#detail/42`) — the only part of the URL JavaScript could change without a server round-trip. Hash-based routing libraries proliferate.

**2010.** HTML5 ships `history.pushState()` and `history.replaceState()`. JavaScript can change the path portion of the URL without reloading. The `popstate` event fires when the user uses back/forward.

**2011–2024.** The History API is *almost* enough. It has well-known gaps:

- No way to *intercept* a navigation. You can react to `popstate` after the fact, but you cannot say "I am about to navigate; let me prepare first."
- No way to know the URL the user is *trying* to navigate to before it happens.
- No way to abort a navigation in progress.
- No way to know if a navigation succeeded or failed.
- No clean way to associate state with a navigation.
- Scroll position management is broken — particularly for back navigations.
- No good way to block navigation pending unsaved changes (the only mechanism, `beforeunload`, is a deliberately-ugly browser-level prompt).

Every SPA router — Backbone Router, Angular Router, Vue Router, React Router, Reach Router, the dozen others — exists primarily to paper over these gaps. They all reimplement the same logic: intercept link clicks, manually call `pushState`, manage their own state, restore scroll on back navigation, expose lifecycle hooks for "before navigation," etc.

**2022.** Chrome ships the Navigation API. Same shape as a router, designed natively. Firefox and Safari are skeptical, debate the design, request changes.

**January 2026.** [Both Firefox and Safari ship Navigation API support](https://web.dev/blog/baseline-navigation-api). [The API becomes Baseline Newly Available](https://developer.mozilla.org/en-US/docs/Web/API/Navigation_API). The era of "routing libraries" — at least the part that exists to compensate for History API gaps — is, in principle, over.

For the first time in fifteen years, the browser ships an API designed for the use case. That is what we will use.

## Part 3: The Navigation API — The Mental Model

The Navigation API is exposed via `window.navigation`. Its core ideas:

- **`navigation.navigate(url)`** — programmatic navigation. Returns a promise.
- **The `navigate` event** — fires before *any* same-origin navigation, including link clicks, back/forward, programmatic navigation, and form submissions. You can intercept and handle.
- **`event.intercept({ handler })`** — within the navigate handler, supply a callback that will execute the navigation. The browser commits the URL change immediately and shows your handler's progress.
- **`navigation.entries()`** — the actual same-origin history list, with rich metadata.
- **`navigation.currentEntry`** — the current entry, with `.url`, `.key`, `.id`, `.getState()`.
- **`navigation.addEventListener("currententrychange", ...)`** — fires after a navigation completes.
- **`navigation.transition`** — the in-progress navigation, with a promise that resolves when committed.

The differences from the History API are large enough that the migration is a rewrite, not an incremental change. But for a new application, the new API is so much cleaner that it almost feels like a different kind of programming.

### The simplest possible Navigation API usage

```javascript
// Intercept all same-origin navigations
navigation.addEventListener("navigate", (event) => {
  // Skip if we cannot intercept (cross-origin, downloads, etc.)
  if (!event.canIntercept) return;
  if (event.hashChange || event.downloadRequest !== null) return;

  // Tell the browser we will handle this navigation
  event.intercept({
    handler() {
      // Update the DOM based on the new URL
      const url = new URL(event.destination.url);
      return renderRoute(url.pathname);
    },
  });
});
```

That is a working SPA router. Twelve lines, including comments. One event handler. Click any same-origin link, the handler runs, the URL updates, the DOM updates. Back button works. Forward button works. Browser refresh works (hits the server, which serves the right HTML for the URL — see Part 9 for the `404 → index.html` GitHub Pages dance).

Compare this to what `history.pushState` requires:

```javascript
// The old way — about 40 lines for the equivalent
document.addEventListener("click", (event) => {
  const link = event.target.closest("a[href]");
  if (!link) return;
  if (link.target === "_blank") return;
  if (event.ctrlKey || event.metaKey || event.shiftKey) return;
  if (event.button !== 0) return;
  const href = link.getAttribute("href");
  const url = new URL(href, location.href);
  if (url.origin !== location.origin) return;
  event.preventDefault();
  history.pushState({}, "", url);
  renderRoute(url.pathname);
});

window.addEventListener("popstate", () => {
  renderRoute(location.pathname);
});

// Plus: scroll restoration, abort handling, focus management,
// modifier-key handling for new tabs, form submission interception...
```

Forty lines and you have only the basics. The Navigation API handles all of those edge cases — modifier keys, target=_blank, downloads, hash changes, cross-origin — in the `event.canIntercept` flag. You do not have to think about them.

## Part 4: The `navigate` Event In Detail

The `navigate` event is the core of the Navigation API. Its properties:

- **`event.canIntercept`** — `true` if you can call `event.intercept()`. Cross-origin navigations, downloads, and certain edge cases set this to `false`.
- **`event.destination`** — the navigation target. Has `.url`, `.key`, `.id`, `.index`, `.getState()`.
- **`event.navigationType`** — `"push"` (new entry), `"replace"` (replaced current), `"reload"` (refresh), or `"traverse"` (back/forward).
- **`event.hashChange`** — `true` if only the URL fragment changed.
- **`event.downloadRequest`** — non-null if the link has `download="..."`.
- **`event.userInitiated`** — `true` if the user actually clicked a link or pressed back, `false` for `navigation.navigate(...)` from script.
- **`event.signal`** — an `AbortSignal` that aborts if the user cancels (for example, by clicking another link before this one finishes).
- **`event.formData`** — for form-submission navigations, the submitted form data.
- **`event.info`** — arbitrary data passed via `navigation.navigate(url, { info })`.

A more complete handler:

```javascript
navigation.addEventListener("navigate", (event) => {
  // Bail-out conditions
  if (!event.canIntercept) return;
  if (event.hashChange) return;
  if (event.downloadRequest !== null) return;
  if (event.formData) return;     // let forms submit normally

  event.intercept({
    handler: async () => {
      const url = new URL(event.destination.url);
      const route = matchRoute(url.pathname);
      if (!route) return render404();
      const Page = await route.load();
      // Use the abort signal for fetches
      const data = await fetchData(url.pathname, { signal: event.signal });
      renderPage(Page, data);
    },
    // Tells the browser: I will handle scroll position myself
    scroll: "manual",
  });
});
```

The `event.signal` is the killer feature — pass it to `fetch`, and if the user clicks another link before this navigation completes, the in-flight fetch aborts. No race conditions where slow data overwrites fresh data.

### Programmatic navigation

```javascript
navigation.navigate("/dashboard", { state: { from: "menu" } });
navigation.navigate("/post/42", { history: "replace" });
navigation.back();
navigation.forward();
navigation.traverseTo(entry.key);   // jump to any entry by key
navigation.reload({ state: { refreshed: true } });
```

`navigation.navigate` accepts `state`, `info`, and `history: "auto" | "push" | "replace"`. The `state` is structured-cloneable (so it can hold `Date`, `Map`, `Set`, etc., not just JSON-safe values).

Each method returns a `{ committed, finished }` object — both are promises. `committed` resolves when the URL is committed; `finished` resolves when your `intercept` handler completes.

```javascript
const { committed, finished } = navigation.navigate("/dashboard");
await committed;    // URL is now /dashboard
await finished;     // Page render is complete
```

### History inspection

```javascript
console.log(navigation.entries());      // all same-origin history entries
console.log(navigation.currentEntry);   // the current one
console.log(navigation.canGoBack);      // bool
console.log(navigation.canGoForward);   // bool
```

`navigation.entries()` returns an array of `NavigationHistoryEntry` objects. Each has `.url`, `.key` (stable across navigations to the same entry), `.id` (unique per visit), `.index`, `.getState()`. Far richer than the History API's opaque "stack with no introspection."

A `dispose` event fires when an entry leaves history (for example, if the user navigates back twice and then forward to a new URL — the two skipped-over entries are disposed). Useful for cleaning up per-entry state.

### Listening for completed navigation

```javascript
navigation.addEventListener("currententrychange", (event) => {
  console.log("Now at:", navigation.currentEntry.url);
  console.log("Came from:", event.from?.url);
  console.log("Type:", event.navigationType);
});
```

Fires after the navigation has committed and your handler has finished. Useful for analytics, focus management, scroll restoration, etc.

## Part 5: Building A Complete SPA Router

Let us build a real router. The complete file, ~120 lines, that we use in the capstone:

```javascript
// @ts-check
// router.js — a complete SPA router using the Navigation API

/** @typedef {{ pattern: URLPattern, load: () => Promise<{ default: PageRender }> }} Route */
/** @typedef {(el: HTMLElement, params: Record<string, string>, signal: AbortSignal) => void | Promise<void>} PageRender */

/** @type {Route[]} */
const routes = [];

/** @type {AbortController | null} */
let currentController = null;

/**
 * Define a route.
 * @param {string} pattern - URL pattern, e.g. "/posts/:id" or "/blog/*"
 * @param {() => Promise<{ default: PageRender }>} load - dynamic import of page module
 */
export function route(pattern, load) {
  routes.push({
    pattern: new URLPattern({ pathname: pattern }),
    load,
  });
}

/**
 * Find the matching route for a URL.
 * @param {URL} url
 * @returns {{ route: Route, params: Record<string, string> } | null}
 */
function matchRoute(url) {
  for (const r of routes) {
    const match = r.pattern.exec({ pathname: url.pathname });
    if (match) {
      const params = match.pathname.groups;
      return { route: r, params: /** @type {Record<string, string>} */ (params) };
    }
  }
  return null;
}

/**
 * Render the matched route into the given container.
 * @param {URL} url
 * @param {HTMLElement} container
 * @param {AbortSignal} signal
 */
async function renderRoute(url, container, signal) {
  const matched = matchRoute(url);
  if (!matched) {
    container.innerHTML = `<h1>404 — Not Found</h1>`;
    document.title = "Not found";
    return;
  }

  // Load the page module (cached after first load)
  const module = await matched.route.load();
  if (signal.aborted) return;

  // Render
  container.innerHTML = "";
  await module.default(container, matched.params, signal);
}

/**
 * Initialise the router. Call once, after defining routes.
 * @param {string} containerSelector - CSS selector for the route outlet, e.g. "#app"
 */
export function startRouter(containerSelector) {
  const container = /** @type {HTMLElement} */ (document.querySelector(containerSelector));
  if (!container) throw new Error(`Router container not found: ${containerSelector}`);

  // Initial render for the URL the user landed on
  const initialController = new AbortController();
  currentController = initialController;
  renderRoute(new URL(location.href), container, initialController.signal);

  // Intercept future navigations
  navigation.addEventListener("navigate", (event) => {
    if (!event.canIntercept) return;
    if (event.hashChange) return;
    if (event.downloadRequest !== null) return;
    if (event.formData) return;

    const url = new URL(event.destination.url);
    if (url.origin !== location.origin) return;

    event.intercept({
      scroll: "after-transition",
      handler: async () => {
        // Cancel any previous render in flight
        currentController?.abort();
        const controller = new AbortController();
        currentController = controller;

        // Wrap render in a View Transition for smooth UX
        const transition = document.startViewTransition(async () => {
          await renderRoute(url, container, controller.signal);
        });

        try {
          await transition.finished;
        } catch {
          // User cancelled (e.g. navigated again); ignore
        }
      },
    });
  });

  // Update document title and announce changes after navigation
  navigation.addEventListener("currententrychange", () => {
    const url = new URL(navigation.currentEntry.url);
    const matched = matchRoute(url);
    if (matched && document.title) {
      // Defer title-setting to the page module if it wants
    }
    announceNavigation(url);
  });
}

/**
 * Announce navigation to assistive technologies.
 * @param {URL} url
 */
function announceNavigation(url) {
  let announcer = document.getElementById("route-announcer");
  if (!announcer) {
    announcer = document.createElement("div");
    announcer.id = "route-announcer";
    announcer.setAttribute("role", "status");
    announcer.setAttribute("aria-live", "polite");
    announcer.style.cssText =
      "position:absolute;width:1px;height:1px;clip:rect(0 0 0 0);overflow:hidden;";
    document.body.append(announcer);
  }
  // The page module is responsible for setting document.title;
  // wait a tick for it to do so.
  setTimeout(() => {
    announcer.textContent = `Navigated to ${document.title}`;
  }, 100);
}
```

That is the entire router. Usage:

```javascript
// app.js
import { route, startRouter } from "./router.js";

route("/", () => import("./pages/home.js"));
route("/blog", () => import("./pages/blog-list.js"));
route("/blog/:slug", () => import("./pages/blog-post.js"));
route("/about", () => import("./pages/about.js"));

startRouter("#app");
```

And a page module:

```javascript
// pages/blog-post.js
// @ts-check
import { fetchJson } from "../utils/dom.js";

/** @type {import("../router.js").PageRender} */
export default async function blogPost(container, params, signal) {
  const post = await fetchJson(`/content/posts/${params.slug}.json`, { signal });
  if (signal.aborted) return;

  document.title = `${post.title} — My Blog`;
  container.innerHTML = `
    <article>
      <h1>${post.title}</h1>
      <p class="meta">${post.author} · ${post.date}</p>
      <div>${post.bodyHtml}</div>
    </article>
  `;
}
```

That is the complete model. Define a page as a function that takes `(container, params, signal)`. Register it with a URL pattern. The router takes care of the rest:

- Route matching using the standard `URLPattern` API.
- Dynamic import for code splitting (pages load only when navigated to).
- View Transitions for animated page changes.
- Abort signals for cancelling in-flight loads.
- Scroll restoration via `scroll: "after-transition"`.
- Screen-reader announcement via `aria-live`.

Total: 120 lines of router, ~30 lines per page module. No npm install. No 84-export library. No "router upgrade weeks." The whole thing is yours.

### `URLPattern` — the standard pattern matcher

`URLPattern` is the standard for URL matching in modern browsers. [Baseline since 2025](https://developer.mozilla.org/en-US/docs/Web/API/URLPattern). It supports the patterns you would expect:

```javascript
new URLPattern({ pathname: "/blog/:slug" });           // /blog/anything
new URLPattern({ pathname: "/posts/:year/:month" });    // /posts/2026/05
new URLPattern({ pathname: "/files/*" });               // wildcard
new URLPattern({ pathname: "/api/(items|tags)/:id" });  // alternation
new URLPattern({ pathname: "/optional{/:lang}?" });     // optional segments
```

Match results expose captured groups under `.pathname.groups`. A `:slug` segment shows up as `match.pathname.groups.slug`.

Safari shipped `URLPattern` later than the others. If you need to support older Safari, ship a tiny ~3KB polyfill from esm.sh. For evergreen 2026, native is fine.

## Part 6: View Transitions In The Router

The most beautiful thing about the Navigation API is how cleanly it composes with View Transitions. Three lines:

```javascript
event.intercept({
  handler: async () => {
    const transition = document.startViewTransition(async () => {
      await renderRoute(url, container, controller.signal);
    });
    await transition.finished;
  },
});
```

The `document.startViewTransition` callback updates the DOM. The browser captures snapshots before and after, and animates between them. By default this is a 250ms cross-fade — tasteful, almost free.

### Named transitions for shared elements

The real win comes from named transitions. Mark an element with `view-transition-name` in CSS, and if both the old page and the new page have an element with that name, the browser morphs between them.

A canonical example: a blog list where each post card has a hero image. On the detail page, the hero image is the full-width header. With named transitions:

```css
.post-card .hero {
  view-transition-name: hero;
}

/* On the detail page: */
.post-detail .hero {
  view-transition-name: hero;
}
```

Now navigating from `/blog` to `/blog/some-post`, the browser sees `view-transition-name: hero` on both pages and animates the hero image from its card position to its detail position. It grows, repositions, transforms — all on the GPU, with no JavaScript involved. This is the "magic move" effect every native app has.

### Per-route customisation

You can also customise the transition CSS per route. Use `data-route` on the `<html>` element:

```javascript
event.intercept({
  handler: async () => {
    document.documentElement.dataset.route = matchedRoute.name;
    const transition = document.startViewTransition(/* ... */);
    await transition.finished;
  },
});
```

```css
html[data-route="blog-list"] ::view-transition-old(root) {
  animation: slide-out-left 300ms ease forwards;
}
html[data-route="blog-detail"] ::view-transition-new(root) {
  animation: slide-in-right 300ms ease forwards;
}
```

Different transitions per page. List → detail slides left; back navigation slides right. All declarative CSS.

### Direction-aware transitions

For "back" navigation feeling different from "forward," check `event.navigationType`:

```javascript
event.intercept({
  handler: async () => {
    document.documentElement.dataset.transition =
      event.navigationType === "traverse" ? "back" : "forward";
    const transition = document.startViewTransition(/* ... */);
    await transition.finished;
  },
});
```

```css
html[data-transition="back"] ::view-transition-new(root) {
  animation: slide-in-from-left 300ms ease forwards;
}
html[data-transition="forward"] ::view-transition-new(root) {
  animation: slide-in-from-right 300ms ease forwards;
}
```

Native iOS-style directional transitions. Twenty lines of CSS. No animation library.

## Part 7: Cross-Document View Transitions — The MPA Path

We have been talking about SPA routing. But for many sites — blogs, marketing sites, documentation, anything where each "page" is genuinely a different document — the right architecture is **multi-page**. Each URL is its own HTML file. The browser handles navigation. No JavaScript router.

The historical objection: MPAs feel slow because the browser does a full white flash between pages. View Transitions fixed this in 2024 with **cross-document transitions**. Both pages opt in with one CSS rule:

```css
@view-transition {
  navigation: auto;
}
```

Add this rule to the stylesheet of every page on your site. Now any same-origin navigation between pages on your site is animated with a default cross-fade. The browser captures the old document, fetches and parses the new document, and animates between them. The white flash is gone.

### Named transitions across documents

Named transitions also work cross-document. A `<img class="thumbnail" style="view-transition-name: hero-1">` on the list page, and a `<img class="hero" style="view-transition-name: hero-1">` on the detail page, get the same magic-move effect — across two completely separate HTML documents. No JavaScript. Nothing.

There is one constraint: each `view-transition-name` must be unique on the source page. You cannot have ten cards all named `hero` simultaneously, because the browser would not know which to morph. The standard pattern is to make the name dynamic per item, set as an inline style on the source page only:

```html
<a href="/posts/42" style="view-transition-name: hero-42">
  <img src="/images/42.jpg" alt="">
</a>
```

```html
<!-- On /posts/42 -->
<img src="/images/42.jpg" style="view-transition-name: hero-42" alt="" class="hero">
```

The browser sees `hero-42` on both, morphs the thumbnail into the hero. Repeat for each post — ten thumbnails on the list page each have their own unique name (`hero-1`, `hero-2`, ...), and only the active link's name matches a name on the destination page.

### When MPA is the right choice

- **Content-heavy sites.** Blogs, documentation, marketing sites. Each page is largely independent content.
- **SEO-critical sites.** Each URL serves complete HTML, nothing requires JavaScript to render.
- **Long-tail content.** Static sites generated at build time, deployed to a CDN. No JavaScript runtime overhead per page.
- **Simplicity priority.** Smaller cognitive surface. The "router" is the file system.

For our magazine, **we use MPA** for the marketing pages (home, about, archive) and the SPA router only for the interactive parts (the search interface, the live tag filter, the comment composer). This is the hybrid we recommended in Part 1: MPA where it makes sense, SPA where interactivity demands it. The two compose cleanly because both use View Transitions.

### Cross-document support note

[Cross-document View Transitions are in Chrome 126+, Safari 18.2+, and Firefox 146+ as of early 2026](https://caniuse.com/css-view-transitions). Firefox was the last holdout; it landed support in late 2025. Not yet Baseline (we are months away), but degrading gracefully — non-supporting browsers just navigate without the transition, which is the previous behaviour. Safe to use today.

## Part 8: Pre-Loading And Speculation Rules

For an SPA, `import()` only fetches the module when the user navigates. For an MPA, the browser only fetches the next page when the user clicks. Both mean a small delay on first navigation.

You can hint the browser to prefetch ahead of time.

### `<link rel="modulepreload">` — for SPAs

For pages you know the user is likely to visit next:

```html
<link rel="modulepreload" href="/js/pages/blog-list.js">
```

The browser fetches the module (and its dependency graph) in parallel with the rest of the page. When `import()` later runs, the response is already cached.

### `<link rel="prefetch">` — for MPAs

For documents:

```html
<link rel="prefetch" href="/blog/" as="document">
```

The browser fetches the page in idle time. When the user navigates, the response is cached.

### Speculation Rules — the modern, smarter version

[The Speculation Rules API](https://developer.chrome.com/docs/web-platform/prerender-pages) is a newer, more powerful version that goes beyond fetching: it can fully **prerender** the next page in a hidden process. When the user clicks, the page is already rendered and the navigation is essentially instant.

```html
<script type="speculationrules">
{
  "prerender": [
    {
      "where": { "href_matches": "/blog/*" },
      "eagerness": "moderate"
    }
  ]
}
</script>
```

`eagerness` ranges from `"conservative"` (only when explicitly hinted, e.g., `pointerdown`) to `"eager"` (prerender liberally). `"moderate"` triggers on link hover, which is a good default — by the time the user clicks, the page is ready.

[Currently Chrome-only](https://caniuse.com/mdn-html_elements_script_type_speculationrules), with Firefox and Safari evaluating. Degrades gracefully: browsers that do not understand it ignore the script tag entirely. Adding it costs you nothing and benefits Chrome users.

For a static site like ours, speculation rules with `eagerness: moderate` produce navigation that is usually faster than an SPA, because the next page is fully prerendered (HTML, CSS, fonts, images, scripts) by the time the user clicks. The SPA still has to call `import()`, parse, evaluate, render. Prerendered MPA navigation is, surprisingly often, faster than its SPA equivalent.

## Part 9: GitHub Pages, 404, And The Static Site Problem

A practical concern for this magazine: GitHub Pages serves static files. If your SPA URL is `/blog/some-post` and the user types that URL into the address bar (or shares it, or refreshes), the server tries to serve `/blog/some-post.html`, which does not exist, and returns a 404.

Two mitigations:

### Option 1: 404.html as fallback

GitHub Pages serves `/404.html` for any unmatched path. Make `/404.html` your SPA shell — the same HTML as `/index.html`, with the router that reads `location.pathname` and renders the right route. Now any path returns the SPA, and the router handles it.

The downside: the response status is 404, which search engines might honour. For a content-heavy site, you do not want this.

### Option 2: Pre-generate every route

For a finite set of routes (blog posts you know about), generate one HTML file per route at build time. `/blog/some-post/index.html` is a real file, served as 200, indexed by search engines, complete content for users who land on it. JavaScript still runs, the SPA "upgrades" the page after first load.

This is the static-site-generator approach. It is what every modern SSG (Astro, Eleventy, Hugo, Jekyll, Next.js export) does. It composes perfectly with our hybrid: each "real" URL has a static HTML file, and JavaScript adds interactivity on top.

For our magazine, we go with option 2. Every blog post has a static HTML file, generated by a Node-free Bash script that reads the markdown and produces the HTML. We will cover the full pipeline in Day 15, the capstone post.

## Part 10: Scroll Restoration

Done badly, scroll restoration is one of the most-irritating bugs in SPAs: the user scrolls down a long list, clicks a link, comes back, and is at the top of the page. The Navigation API handles this much better than the History API did.

By default, the Navigation API restores scroll position when the navigation is `event.navigationType === "traverse"` (back/forward) and you do not interfere. The browser's behaviour:

1. User scrolls to position 1500.
2. User clicks a link, navigates to a new page.
3. Navigation API saves scroll position 1500 against the entry being left.
4. New page renders.
5. User clicks back.
6. Navigation API restores scroll to 1500.

The trick: scroll restoration must happen *after* the page has rendered to its full height. If the new content is shorter than the saved scroll position, the browser cannot scroll there.

The Navigation API exposes scroll control via `event.intercept({ scroll: ... })`:

- **`"after-transition"`** (default) — browser scrolls after your handler completes.
- **`"manual"`** — you call `event.scroll()` yourself when ready.

For routes whose content arrives async (the page renders, then data loads, then more content appears), use `"manual"`:

```javascript
event.intercept({
  scroll: "manual",
  handler: async () => {
    await renderInitialShell(url);
    event.scroll();   // restore now (skeleton is at full height)
    await renderData(url);   // content fills in
  },
});
```

The browser scrolls to the saved position when you call `event.scroll()`. If the page is shorter than the saved position, the browser scrolls as close as it can.

For "go-to-top" navigation (clicking a new link should always start at the top, regardless of saved position):

```javascript
event.intercept({
  handler: async () => {
    await renderRoute(url, container, signal);
    if (event.navigationType !== "traverse") {
      window.scrollTo({ top: 0, behavior: "instant" });
    }
  },
});
```

Push/replace navigations go to top; back/forward restore. Standard expected behaviour, in three lines.

## Part 11: Form Submissions

Forms are navigations. When the user submits a form, the browser navigates to the form's `action` URL with the form data as the body. By default, this is a full page reload — exactly what an MPA wants.

For an SPA, intercept the form submission:

```javascript
navigation.addEventListener("navigate", (event) => {
  if (!event.canIntercept) return;
  if (!event.formData) return;   // not a form

  event.intercept({
    handler: async () => {
      const url = new URL(event.destination.url);
      const response = await fetch(url, {
        method: "POST",
        body: event.formData,
        signal: event.signal,
      });
      const data = await response.json();
      handleResponse(data);
    },
  });
});
```

The `event.formData` is automatically populated for form-submission navigations. You get the user's data, you handle it, you decide whether to navigate or not. The form's `action` and `method` attributes still drive the URL — you are just intercepting before the round-trip.

For our static-site case, forms typically post to a third-party service (Formspree, Netlify Forms, etc.) or use only client-side state. Either way, the pattern is the same: intercept the navigation, handle the form, optionally navigate elsewhere with `navigation.navigate(...)` to show a "thanks" page.

## Part 12: A Side-By-Side Comparison

Putting both architectures next to each other.

| Concern | SPA (Navigation API) | MPA (cross-doc View Transitions) |
|---|---|---|
| Initial load time | Slower — full bundle | Faster — single page only |
| Subsequent nav speed | Fast (no full reload) | Fast (with View Transitions + speculation rules) |
| Animation flexibility | Maximum (any DOM change is animatable) | Good (named transitions across documents work) |
| Code complexity | ~120-line router + page modules | Zero — file system is the router |
| SEO | Requires SSG or careful setup | Trivial — every URL is a real document |
| Long-running state | Lives in JS memory across navigations | Must persist (URL, localStorage, IndexedDB) |
| Error recovery | App-level error boundaries | Standard browser error pages |
| Service worker integration | Uniform — service worker sees one shell | Per-page caching strategy |
| Deep linking | Trivial | Trivial |
| Browser memory | Grows during session | Reset between pages |
| Rendering control | Total control inside JavaScript | Trust the browser |

Neither is universally right. Our advice for new projects in 2026:

- **Default to MPA with cross-document View Transitions.** It is simpler, faster on initial load, and the navigation experience is now genuinely smooth.
- **Use SPA where the app has long-lived in-memory state that should survive navigation** — a multi-step wizard, an editor, an in-progress conversation, a collaborative document.
- **Hybrid is fine.** The home page, blog list, and blog posts are MPA. The interactive editor and dashboard are SPA. They live in the same site.

This is precisely the opposite of what most teams have done for the last decade, where SPA was the default and MPA was the exception. The browser has caught up. SPA's compelling reasons are now narrower than they have ever been.

## Part 13: When You Still Want A Routing Library

We have made the case that you do not need a library. There are still cases where one earns its keep:

1. **Massive route tables** with deep nesting. A library that has nested layouts, parallel routes, and route groups (Next.js App Router) is genuinely useful for large applications with many concurrent navigation contexts (sidebars + main content + modals).
2. **Server-driven routing** where loaders run on the server before the page renders. React Router 7 Data API and Next.js loaders are designed for this. Without server rendering, much of their complexity disappears.
3. **Strong opinions about loader/action patterns.** Some teams want the "data is always fetched alongside the route" pattern enforced. Libraries provide the structure.
4. **Existing codebases.** If you have a working React Router app, do not migrate. Code costs money.

For a new content-driven site or a small-to-medium app, the Navigation API plus dynamic imports is enough. The `120-line router` we built today scales to dozens of routes without any modification. Past a hundred routes, you might want a library; below that, you almost certainly do not.

## Part 14: Tomorrow

Tomorrow — **Day 12: Forms, Validation, and the Constraint Validation API** — we go deeper on forms. The Constraint Validation API (which has been there since 2010 and most developers have never read about), `setCustomValidity` for arbitrary rules, the `:user-valid` and `:user-invalid` pseudo-classes, the Constraint Validation API's relationship with Form-Associated Custom Elements (Day 10), the new `requestSubmit()` method, and a complete worked form with native validation, async server checks, and accessible error messages. If you have ever reached for Formik, React Hook Form, or VeeValidate for "it had validation," tomorrow is the article that shows you what the platform already gives you.

See you tomorrow.

---

## Series navigation

You are reading **Part 11 of 15**.

- [Part 1: Overview — Why a Plain Browser Is Enough in 2026](/blog/2026-05-24-no-build-web-overview)
- [Part 2: Semantic HTML and the Document Outline Nobody Taught You](/blog/2026-05-25-no-build-web-html-semantics)
- [Part 3: The Cascade, Specificity, and `@layer`](/blog/2026-05-26-no-build-web-css-cascade-layers)
- [Part 4: Modern CSS Layout — Flexbox, Grid, Subgrid, and Container Queries](/blog/2026-05-27-no-build-web-modern-css-layout)
- [Part 5: Responsive Design in 2026](/blog/2026-05-28-no-build-web-responsive-design)
- [Part 6: Colour, Typography, and Motion](/blog/2026-05-29-no-build-web-color-typography-motion)
- [Part 7: Native ES Modules](/blog/2026-05-30-no-build-web-es-modules)
- [Part 8: The DOM, Events, and Platform Primitives](/blog/2026-05-31-no-build-web-dom-and-events)
- [Part 9: State Management Without a Library](/blog/2026-06-01-no-build-web-state-and-signals)
- [Part 10: Web Components](/blog/2026-06-02-no-build-web-custom-elements)
- **Part 11 (today): Client-Side Routing with the Navigation API and View Transitions**
- Part 12 (tomorrow): Forms, Validation, and the Constraint Validation API
- Part 13: Storage, Service Workers, and Offline
- Part 14: Accessibility, Performance, and Security
- Part 15: The Conclusion — A Complete Application End to End

## Resources

- [MDN: Navigation API](https://developer.mozilla.org/en-US/docs/Web/API/Navigation_API) — the canonical reference.
- [MDN: NavigateEvent](https://developer.mozilla.org/en-US/docs/Web/API/NavigateEvent) — every property, every method.
- [MDN: URLPattern](https://developer.mozilla.org/en-US/docs/Web/API/URLPattern) — pattern syntax reference.
- [web.dev: Navigation API is Baseline Newly Available](https://web.dev/blog/baseline-navigation-api) — the January 2026 announcement.
- [web.dev: Modern client-side routing: the Navigation API](https://developer.chrome.com/docs/web-platform/navigation-api) — Chrome's deep dive.
- [web.dev: View Transitions API](https://developer.chrome.com/docs/web-platform/view-transitions/) — same and cross-document.
- [web.dev: Cross-document view transitions](https://developer.chrome.com/docs/web-platform/view-transitions/cross-document) — the MPA technique.
- [Speculation Rules API documentation](https://developer.chrome.com/docs/web-platform/prerender-pages) — prerender for instant navigation.
- [Caniuse: Navigation API](https://caniuse.com/mdn-api_navigation) — current browser support.
- [Caniuse: cross-document View Transitions](https://caniuse.com/css-view-transitions) — current support.
- [Jake Archibald, *Demystifying View Transitions*](https://jakearchibald.com/2024/view-transitions-handling-aspect-ratio-changes/) — practical View Transitions techniques.
- [Astro](https://astro.build/) — a static site generator that uses View Transitions natively.
