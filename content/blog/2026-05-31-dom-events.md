---
title: "Without a Net, Part 8: The DOM, Events, and Platform Primitives Everyone Reimplemented"
date: 2026-05-31
author: myblazor-team
summary: "Day 8 of our fifteen-part no-build web series tours the DOM, events, and the quiet-but-vast set of browser primitives that replaced about half of jQuery's reason for existing. We cover querySelector in depth, event delegation and AbortController, the full fetch API with streaming and timeouts, the three Observer APIs (Intersection, Mutation, Resize), structuredClone, requestIdleCallback, and the handful of utilities that make 'vanilla JavaScript' feel like a mature framework. Zero dependencies, zero frameworks, just the platform."
tags:
  - javascript
  - dom
  - events
  - abortcontroller
  - intersection-observer
  - mutation-observer
  - resize-observer
  - fetch
  - web-standards
  - no-build
  - first-principles
  - series
  - deep-dive
---

## Introduction: The Twelve-Line Debounce

A few years ago a colleague asked us to help review a pull request. The change description was brief: *"Add debounce to search field."* The diff was eight lines. One of them was an import:

```javascript
import { debounce } from "lodash";
```

We pointed out, gently, that the entire point of the pull request — a single use of a single utility function — had just added 70 kilobytes of JavaScript to the production bundle. Lodash's tree-shaking helps, but the team's bundler config did not shake effectively, and every `import from "lodash"` was pulling in the whole library. The cost of this one debounce call, across all users of the site, was measurable in gigabytes of bandwidth per month.

"Could we just write it ourselves?" we asked.

The response, paraphrased slightly: *"I'd rather use a tested library than rewrite something."* Fair, in principle. But a debounce function is twelve lines:

```javascript
export function debounce(fn, ms) {
  let id;
  return (...args) => {
    clearTimeout(id);
    id = setTimeout(() => fn(...args), ms);
  };
}
```

That is the complete, correct implementation. It has been the complete implementation since JavaScript had `setTimeout`, which is to say since 1995. You can test it in three lines. It has no edge cases that differ from Lodash's version for the 99% use case. There is no library maintenance to worry about because the thing does not need maintenance — it is five lines of platform primitives.

This is a miniature of a pattern that runs through the entire web-development ecosystem: *we reach for libraries to do things the platform has done natively for years.* The pattern was defensible in 2008, when the DOM was genuinely inconsistent across browsers and jQuery was a cross-browser compatibility layer. It was defensible in 2013, when `fetch` and `Promise` were not yet standard. It made some sense in 2018, when the rise of React made framework-specific wrappers the default choice. In 2026, it is mostly habit.

Day 8 is our tour of the platform primitives. The things the browser ships that you have been importing as libraries. We will cover, in order: `querySelector` and DOM traversal; event handling from first principles; event delegation and `AbortController`; `fetch`, timeouts, and `AbortSignal`; `IntersectionObserver`, `MutationObserver`, `ResizeObserver`; `structuredClone` and JSON; `requestIdleCallback`, `requestAnimationFrame`, and scheduling; the Clipboard and URL APIs; a handful of smaller primitives; and a practical utility file we will reuse in every subsequent article. No frameworks. No dependencies. Just what the platform ships.

This is Part 8 of 15 in our no-build web series. After [Day 7's](/blog/2026-05-30-no-build-web-es-modules) module system, we have a way to organise code. Today we cover what you actually put in that code.

## Part 1: `querySelector` And DOM Traversal

The DOM is a tree of nodes. The JavaScript interface to that tree is — in 2026 — pleasantly uniform. The only selector methods you need are:

- **`document.querySelector(selector)`** — the first matching element, or `null`.
- **`document.querySelectorAll(selector)`** — a `NodeList` of all matching elements.
- **`element.querySelector(selector)`** — first matching descendant of `element`.
- **`element.querySelectorAll(selector)`** — all matching descendants.

The selector is a standard CSS selector — anything that works in a stylesheet works here. Attribute selectors, pseudo-classes, combinators, everything:

```javascript
document.querySelector("#main")
document.querySelector(".card")
document.querySelector("article:has(img) > h2")
document.querySelector('input[type="email"][required]')
document.querySelectorAll("nav a:not(:last-child)")
```

`:has()` (Day 5) works in `querySelector` too, which is genuinely powerful — you can match "the form that contains an invalid field" in one call.

### `NodeList` vs `Array`

`querySelectorAll` returns a `NodeList`, not an `Array`. The difference is mostly historical; a `NodeList` is iterable, so `for...of` works, and you can call `.forEach` on it. But `NodeList` does not have `.map`, `.filter`, or `.reduce`. The idiom is:

```javascript
const links = document.querySelectorAll("a");
for (const link of links) {
  link.classList.add("external");
}

// or, if you need array methods:
const hrefs = [...document.querySelectorAll("a")].map(a => a.href);
```

`[...nodeList]` converts to an array. `Array.from(nodeList)` also works.

### `closest()` — the upward query

`element.closest(selector)` walks *up* from the element, returning the nearest ancestor (including the element itself) that matches the selector. Perfect for event delegation:

```javascript
document.addEventListener("click", (event) => {
  const link = event.target.closest("a[data-action]");
  if (link) {
    event.preventDefault();
    handleAction(link.dataset.action);
  }
});
```

The user might click the `<img>` inside an `<a>`, the `<span>` inside a `<button>`, the `<strong>` inside a `<p>` that handles clicks. `closest()` finds the right ancestor regardless.

### `matches()` — the predicate

`element.matches(selector)` returns `true` if the element matches. Useful for filtering inside event handlers or during traversal:

```javascript
if (event.target.matches("button[type=submit]")) {
  event.preventDefault();
  // ...
}
```

### Traversal properties

- **`parentElement`** — the parent, or `null`. (`parentNode` also includes document and document fragments; prefer `parentElement`.)
- **`children`** — element children (excludes text nodes).
- **`childNodes`** — all child nodes including text and comments. You rarely want this.
- **`firstElementChild`**, **`lastElementChild`** — first/last element child.
- **`nextElementSibling`**, **`previousElementSibling`** — siblings.
- **`childElementCount`** — count of element children.

Forget `firstChild` and `nextSibling` unless you need to work with text nodes — they behave surprisingly when whitespace is a text node, which it usually is.

### Manipulation

- **`element.append(...nodes)`** — append children, text, or mixed.
- **`element.prepend(...nodes)`** — prepend.
- **`element.before(...nodes)`**, **`element.after(...nodes)`** — insert as siblings.
- **`element.replaceWith(...nodes)`** — replace in place.
- **`element.remove()`** — remove from the tree.
- **`element.insertAdjacentElement(position, element)`** and variants (`insertAdjacentText`, `insertAdjacentHTML`) — fine-grained placement.

All of the `append`/`prepend`/`before`/`after` methods accept both nodes and strings — strings become text nodes automatically. This replaces the old `appendChild(document.createTextNode("..."))` ceremony.

### `innerHTML`, `outerHTML`, `textContent`

- **`textContent`** — the concatenated text content of the element and all descendants. Setting it replaces the contents with a single text node. Safe — no HTML parsing.
- **`innerHTML`** — the HTML markup of the element's contents. Setting it parses HTML and replaces. **Unsafe with untrusted input** — it will execute any script-injecting markup. Sanitize first.
- **`outerHTML`** — same as `innerHTML` but includes the element itself. Setting it replaces the element entirely.

The security rule: **never set `innerHTML` with string-concatenated user input.** Use `textContent` instead, or sanitize the HTML with a library (like [DOMPurify](https://github.com/cure53/DOMPurify)) before assignment.

### `setHTMLUnsafe()` and the Sanitizer API

A newer approach: the [HTML Sanitizer API](https://developer.mozilla.org/en-US/docs/Web/API/HTML_Sanitizer_API) provides `element.setHTML(string)` which parses HTML and removes dangerous elements and attributes. [Baseline Newly Available in late 2025](https://web.dev/blog/web-platform-09-2025). The counterpart, `setHTMLUnsafe()`, explicitly allows everything — a name designed to scream "you are opting out of safety" every time you read the code:

```javascript
element.setHTML(untrustedMarkdown);  // sanitized
element.setHTMLUnsafe(trustedHtml);  // unsafe, explicit
```

If the rendered HTML comes from anywhere other than your own code, use `setHTML()`. For the small case where `innerHTML` is fine — static template strings, fully trusted sources — the performance overhead of `setHTML` is modest.

### DocumentFragment

For batch insertions, use `DocumentFragment`:

```javascript
const fragment = document.createDocumentFragment();
for (const post of posts) {
  const li = document.createElement("li");
  li.textContent = post.title;
  fragment.append(li);
}
list.append(fragment);
```

The fragment is a lightweight container. Building it up and appending once is faster than appending one by one, because the browser only does layout after the final append. For a list of 100+ items the difference is noticeable.

## Part 2: Creating Elements

Two ways to build DOM trees:

### `createElement` — the verbose but safe way

```javascript
const card = document.createElement("article");
card.classList.add("card");

const title = document.createElement("h2");
title.textContent = post.title;
card.append(title);

const meta = document.createElement("p");
meta.classList.add("meta");
meta.append(`By ${post.author} on `);
const time = document.createElement("time");
time.dateTime = post.date.toISOString();
time.textContent = post.date.toLocaleDateString();
meta.append(time);
card.append(meta);

list.append(card);
```

Verbose, yes. But every value is inserted as text or attribute — no HTML injection is possible. For user-controlled data, this is the safe default.

### A small template helper

To make the above less tedious, a tiny helper:

```javascript
// @ts-check
/**
 * Create an element with attributes and children.
 * @param {string} tag
 * @param {Record<string, any>} [attrs]
 * @param {...(Node | string)} children
 * @returns {HTMLElement}
 */
export function h(tag, attrs = {}, ...children) {
  const el = document.createElement(tag);
  for (const [key, value] of Object.entries(attrs)) {
    if (key === "class") el.className = value;
    else if (key === "style" && typeof value === "object") {
      Object.assign(el.style, value);
    } else if (key.startsWith("on") && typeof value === "function") {
      el.addEventListener(key.slice(2).toLowerCase(), value);
    } else if (key in el) {
      el[key] = value;
    } else {
      el.setAttribute(key, value);
    }
  }
  el.append(...children.filter(c => c != null));
  return el;
}
```

And usage:

```javascript
const card = h("article", { class: "card" },
  h("h2", {}, post.title),
  h("p", { class: "meta" },
    `By ${post.author} on `,
    h("time", { dateTime: post.date.toISOString() },
      post.date.toLocaleDateString()
    )
  )
);
```

Fifteen lines of `h()` helper, thirty-character reduction per element. This is conceptually the same as [htm](https://esm.sh/htm) or [hyperscript](https://github.com/hyperhype/hyperscript) — a thin wrapper around `createElement` that makes tree-building ergonomic. Every byte of it is yours. No framework.

### Templates from strings — safely

If you really want string-based templates, use `<template>`:

```html
<template id="card-template">
  <article class="card">
    <h2></h2>
    <p class="meta"></p>
  </article>
</template>
```

```javascript
const tpl = document.getElementById("card-template");
const clone = tpl.content.cloneNode(true);
clone.querySelector("h2").textContent = post.title;
clone.querySelector(".meta").textContent = `By ${post.author}`;
list.append(clone);
```

`<template>` is a first-class element designed for exactly this. The content inside never renders, never fetches resources, never runs scripts. `cloneNode(true)` produces a new copy. You fill in the blanks with `textContent` (never `innerHTML`), and append. No sanitization issues because you never concatenate strings into markup.

This pattern is the heart of how Web Components work, which we cover in Day 10.

## Part 3: Events From First Principles

Event handling in 2026 has three forms, in order of preference.

### 1. `addEventListener`

```javascript
button.addEventListener("click", (event) => {
  console.log("clicked");
});
```

The canonical way. Multiple listeners can coexist on the same element and event. You pass a function; the browser calls it when the event fires.

The third argument is an options object (older code uses a boolean for `useCapture`, which is still valid but less clear):

```javascript
button.addEventListener("click", handler, {
  capture: false,     // true = listen during capture phase
  once: true,         // auto-remove after firing once
  passive: true,      // cannot call preventDefault()
  signal: signal,     // AbortController signal for removal
});
```

Each option earns its place:

- **`once: true`** — the listener is removed after its first invocation. Great for one-shot initialisation, setup dialogs, or user-confirmation hooks.
- **`passive: true`** — promises you will not call `preventDefault()`, which lets the browser optimise scrolling. Use for `scroll`, `touchstart`, `touchmove`, `wheel` — anywhere you listen but do not cancel.
- **`signal: signal`** — covered in detail in Part 4 below. Ties the listener's lifetime to an `AbortController`.

### 2. Inline event properties

```javascript
button.onclick = (event) => { /* ... */ };
```

Legacy, but still valid. The problem: only one listener at a time. Setting `onclick` twice replaces the first. For complex apps, prefer `addEventListener`.

The one place `onclick` remains useful is for *removing* a listener you know was set via this pattern: `button.onclick = null`.

### 3. HTML attributes

```html
<button onclick="doThing()">Do thing</button>
```

Avoid. It requires `doThing` to be a global, which is back to the 1995 problem we solved with modules in Day 7. Also violates strict Content Security Policy (Day 14). Use `addEventListener` in a module instead.

### The event object

Every event handler gets an `Event` (or a subclass like `MouseEvent`, `KeyboardEvent`, `InputEvent`). Useful properties:

- **`event.target`** — the element that triggered the event. If the user clicks on an `<img>` inside an `<a>`, `event.target` is the `<img>`.
- **`event.currentTarget`** — the element the listener is attached to. In the above case, the `<a>`. Use this when your handler is attached to a container.
- **`event.type`** — the event name (`"click"`, `"keydown"`, etc.).
- **`event.preventDefault()`** — cancel the default action. Link navigation, form submission, checkbox toggling.
- **`event.stopPropagation()`** — stop the event from bubbling up.
- **`event.stopImmediatePropagation()`** — stop, *and* prevent further listeners on the current element from running.

The common mistake: using `event.stopPropagation()` defensively, "just in case." This breaks event delegation (see Part 4) in ways that are nearly impossible to debug. Use `stopPropagation` only when you have a specific reason — usually *never* in application code.

### Keyboard events

- **`keydown`** — fires when a key is pressed down. Repeats with key-repeat.
- **`keyup`** — fires when released.
- **`keypress`** — deprecated. Do not use.

`event.key` is a string: `"a"`, `"Enter"`, `"Escape"`, `"ArrowDown"`. `event.code` is a physical key code: `"KeyA"`, `"Enter"`, `"ArrowDown"`. Use `event.key` for "what did the user type" and `event.code` for "which physical key" (game controls, keyboard shortcuts independent of layout).

Modifier keys: `event.ctrlKey`, `event.shiftKey`, `event.altKey`, `event.metaKey` (Cmd on macOS, Windows key elsewhere).

A keyboard-shortcut handler:

```javascript
document.addEventListener("keydown", (event) => {
  if ((event.ctrlKey || event.metaKey) && event.key === "k") {
    event.preventDefault();
    openCommandPalette();
  }
  if (event.key === "Escape") {
    closeAnyOpenModals();
  }
});
```

### Custom events

Dispatch your own events. This is the bridge between arbitrary DOM nodes without a library.

```javascript
// Dispatch
element.dispatchEvent(new CustomEvent("post-deleted", {
  detail: { id: postId },
  bubbles: true,
  composed: true,   // crosses shadow DOM boundaries
}));

// Listen
document.addEventListener("post-deleted", (event) => {
  console.log("Post deleted:", event.detail.id);
});
```

Custom events bubble just like native events. Attach listeners at any ancestor. Components can emit events, other components can listen. This is exactly how every framework's "event bus" works — and it is already built into the DOM.

## Part 4: Event Delegation And `AbortController`

A common pattern: instead of attaching a listener to every `<li>`, attach one to the list and use `event.target.closest()` to identify which item was interacted with.

```javascript
document.getElementById("posts").addEventListener("click", (event) => {
  const deleteBtn = event.target.closest("button[data-delete]");
  if (deleteBtn) {
    const id = deleteBtn.dataset.delete;
    deletePost(id);
    return;
  }

  const link = event.target.closest("a[data-post-link]");
  if (link) {
    event.preventDefault();
    openPost(link.dataset.postLink);
    return;
  }
});
```

One listener. Handles any number of items. Items added later are automatically handled — no re-binding. This is **event delegation**, and it is the right pattern for any list-like UI.

### `AbortController` for listener lifecycle

Every modern listener pattern needs a way to remove the listener when it is no longer needed. The old way:

```javascript
const handler = () => { /* ... */ };
element.addEventListener("click", handler);
// later:
element.removeEventListener("click", handler);
```

The function reference must match exactly. If you defined it inline, you cannot remove it. If you reassign it, you cannot remove it.

The new way uses `AbortController` (which also controls `fetch`, timeouts, and any other abortable operation):

```javascript
const controller = new AbortController();

element.addEventListener("click", () => { /* ... */ }, {
  signal: controller.signal,
});
button.addEventListener("click", () => { /* ... */ }, {
  signal: controller.signal,
});
document.addEventListener("keydown", () => { /* ... */ }, {
  signal: controller.signal,
});

// Later — remove all of the above at once:
controller.abort();
```

One `abort()` call removes *every* listener registered with that signal. This is enormously useful for:

- **Custom elements** cleaning up in `disconnectedCallback`.
- **Page transitions** removing all listeners from the old page when navigating.
- **Dialog open/close** registering escape-to-close, click-outside, etc., all tied to the dialog's lifetime.

The `AbortController` primitive unifies fetch cancellation, timeout, and event cleanup into a single mental model. Your "cleanup" is just `controller.abort()`. No reference tracking, no `useEffect` return values, no per-listener boilerplate.

### A complete dialog handler

A modal dialog that handles Escape, click-outside, and cleanup:

```javascript
function openDialog(dialog) {
  const controller = new AbortController();
  const { signal } = controller;

  dialog.showModal();

  document.addEventListener("keydown", (event) => {
    if (event.key === "Escape") {
      dialog.close();
    }
  }, { signal });

  dialog.addEventListener("click", (event) => {
    // Click outside the dialog content closes it
    if (event.target === dialog) {
      dialog.close();
    }
  }, { signal });

  dialog.addEventListener("close", () => {
    controller.abort();   // removes all our listeners
  }, { signal });
}
```

Three event handlers, one cleanup, thirteen lines. No framework needed. Not even the `<dialog>` element's built-in escape handling (though in practice, `<dialog>` via `showModal()` already handles Escape — this is illustrative).

## Part 5: `fetch` — The Modern Network API

`fetch` replaced `XMLHttpRequest` a decade ago and is now the canonical way to talk to servers from the browser. A minimal GET:

```javascript
const response = await fetch("/api/posts");
if (!response.ok) throw new Error(`HTTP ${response.status}`);
const posts = await response.json();
```

Two lines. The `Response` object exposes the body via:

- `await response.json()` — parse as JSON.
- `await response.text()` — read as string.
- `await response.blob()` — read as binary Blob.
- `await response.arrayBuffer()` — read as raw bytes.
- `await response.formData()` — read as FormData (for form submissions).
- `response.body` — a `ReadableStream` for streaming responses.

### POST, PUT, DELETE

```javascript
const response = await fetch("/api/posts", {
  method: "POST",
  headers: { "Content-Type": "application/json" },
  body: JSON.stringify({ title: "Hello", body: "World" }),
});
```

### FormData submissions

```javascript
const form = document.querySelector("form");
const response = await fetch(form.action, {
  method: form.method,
  body: new FormData(form),
});
```

`new FormData(formElement)` captures every named field in the form automatically. Exactly how a traditional HTML form submission works. The server sees `multipart/form-data` just like a native submission. This pattern gives you JavaScript-enhanced form handling with zero serialisation code.

### Request headers

Pass an object or a `Headers` instance:

```javascript
const response = await fetch("/api/posts", {
  headers: {
    "Accept": "application/json",
    "Authorization": `Bearer ${token}`,
  },
});
```

### Credentials

By default, `fetch` does NOT send cookies to cross-origin requests. To include them:

```javascript
fetch(url, { credentials: "include" });
```

Same-origin requests always include cookies unless you set `credentials: "omit"`.

### Timeouts via `AbortSignal.timeout()`

`fetch` does not have a built-in timeout. The idiomatic way:

```javascript
try {
  const response = await fetch(url, {
    signal: AbortSignal.timeout(5000),   // abort after 5 seconds
  });
  const data = await response.json();
} catch (error) {
  if (error.name === "TimeoutError") {
    console.log("Timed out");
  } else if (error.name === "AbortError") {
    console.log("Aborted for another reason");
  } else {
    throw error;
  }
}
```

`AbortSignal.timeout(ms)` returns a signal that auto-aborts after the given time. It throws a `DOMException` with name `"TimeoutError"`. [Baseline Widely Available since 2023](https://developer.mozilla.org/en-US/docs/Web/API/AbortSignal/timeout_static).

### Combining signals

`AbortSignal.any()` combines multiple signals into one that fires when any of them does:

```javascript
const userCancel = new AbortController();
const timeout = AbortSignal.timeout(5000);

fetch(url, {
  signal: AbortSignal.any([userCancel.signal, timeout]),
});

// Now either:
userCancel.abort();           // user clicked cancel
// or the timeout fires after 5s — either aborts the fetch
```

This is the right pattern for any long-running operation: combine a user-cancel signal with a timeout signal. The operation cancels on whichever fires first.

### Streaming responses

Large responses can be consumed as a stream rather than buffered:

```javascript
const response = await fetch("/api/huge-json-stream");
const reader = response.body
  .pipeThrough(new TextDecoderStream())
  .getReader();

while (true) {
  const { done, value } = await reader.read();
  if (done) break;
  processChunk(value);
}
```

Useful for streaming JSON newline-delimited responses, server-sent events, long downloads where you want to show progress. `pipeThrough(new TextDecoderStream())` converts the byte stream to a text stream. `getReader()` gives you a reader you can pull from.

### A robust `fetch` wrapper

Put this in your `utils.js`:

```javascript
// @ts-check
/**
 * @param {string} url
 * @param {RequestInit & { timeout?: number }} [init]
 * @returns {Promise<any>}
 */
export async function fetchJson(url, init = {}) {
  const { timeout = 10_000, signal, ...rest } = init;
  const signals = [AbortSignal.timeout(timeout)];
  if (signal) signals.push(signal);

  const response = await fetch(url, {
    ...rest,
    headers: { "Accept": "application/json", ...rest.headers },
    signal: AbortSignal.any(signals),
  });

  if (!response.ok) {
    const text = await response.text();
    throw new Error(`HTTP ${response.status}: ${text}`);
  }
  return response.json();
}
```

Timeout, combined signals, consistent error shape, JSON by default. Thirty lines of JavaScript that replace `axios`, `ky`, or `undici` for 90% of uses.

## Part 6: `IntersectionObserver` — Knowing When Something Is Visible

`IntersectionObserver` watches elements and tells you when they enter or leave the viewport (or any container). Before it existed, the common pattern was a scroll event listener that called `getBoundingClientRect()` on every element, every frame — expensive and janky.

[Baseline Widely Available since 2019](https://developer.mozilla.org/en-US/docs/Web/API/IntersectionObserver). The shape:

```javascript
const observer = new IntersectionObserver((entries) => {
  for (const entry of entries) {
    if (entry.isIntersecting) {
      entry.target.classList.add("visible");
    } else {
      entry.target.classList.remove("visible");
    }
  }
}, {
  root: null,               // null = viewport; or an element
  rootMargin: "0px",        // expand/shrink the "visible" area
  threshold: 0,             // 0 = fire when any part is visible; 1 = fully visible
});

document.querySelectorAll(".card").forEach(card => observer.observe(card));
```

`threshold` can be a single number (fire when that fraction is visible) or an array (fire at multiple points). `threshold: [0, 0.25, 0.5, 0.75, 1]` fires five times as the element scrolls through.

### Lazy-loading images

Native `<img loading="lazy">` handles this now (Day 2), but for custom cases:

```javascript
const lazyImages = new IntersectionObserver((entries) => {
  for (const entry of entries) {
    if (entry.isIntersecting) {
      const img = entry.target;
      img.src = img.dataset.src;
      lazyImages.unobserve(img);
    }
  }
});

document.querySelectorAll("img[data-src]").forEach(img => lazyImages.observe(img));
```

### Infinite scroll

```javascript
const sentinel = document.querySelector(".load-more-sentinel");
const observer = new IntersectionObserver(async (entries) => {
  if (entries[0].isIntersecting) {
    const moreItems = await fetchNextPage();
    appendItems(moreItems);
    if (!hasMorePages()) observer.disconnect();
  }
}, { rootMargin: "200px" });

observer.observe(sentinel);
```

The sentinel is an empty `<div>` at the bottom of the list. When it approaches the viewport (`rootMargin: "200px"` expands the viewport 200px in all directions), we fetch the next page. No scroll listener. No throttling needed. The browser does the work efficiently.

### Analytics — tracking actual visibility

A common use: "track which articles the user actually sees, not just clicks."

```javascript
const tracked = new IntersectionObserver((entries) => {
  for (const entry of entries) {
    if (entry.isIntersecting && entry.intersectionRatio >= 0.5) {
      analytics.track("article-viewed", { id: entry.target.dataset.id });
      tracked.unobserve(entry.target);
    }
  }
}, { threshold: 0.5 });

document.querySelectorAll("article[data-id]").forEach(el => tracked.observe(el));
```

Only fires when at least half the article is on screen. `unobserve` after firing prevents duplicate events.

## Part 7: `MutationObserver` — Reacting To DOM Changes

`MutationObserver` tells you when the DOM changes. Attributes added, children added or removed, text changed. Replaces the ugly `setInterval` polling that used to be common.

```javascript
const observer = new MutationObserver((mutations) => {
  for (const mutation of mutations) {
    console.log(
      `Type: ${mutation.type}, target:`,
      mutation.target,
      "added:", mutation.addedNodes,
      "removed:", mutation.removedNodes
    );
  }
});

observer.observe(document.getElementById("chat"), {
  childList: true,       // observe child additions/removals
  subtree: true,         // including descendants
  attributes: true,      // observe attribute changes
  attributeOldValue: true,
  characterData: true,   // observe text changes
  characterDataOldValue: true,
});
```

Uses:

- **Integrating with third-party code that inserts content.** You get a callback when they do.
- **Implementing custom elements with dynamic content projection.**
- **Auto-focusing the first input in any modal that appears.**
- **Syntax highlighting for code blocks that might be inserted lazily.**

Batch calls — `MutationObserver` fires its callback once per microtask, not per mutation, so it is inexpensive to observe.

## Part 8: `ResizeObserver` — Knowing When Something Changes Size

`ResizeObserver` tells you when an element's size changes — for any reason, not just window resize.

```javascript
const observer = new ResizeObserver((entries) => {
  for (const entry of entries) {
    const { width, height } = entry.contentRect;
    console.log(`${entry.target.id}: ${width} x ${height}`);
  }
});

observer.observe(document.getElementById("chart"));
```

The callback fires when the element's size changes. Uses:

- **Charts that need to re-render when their container resizes.** A canvas at 800x400 rendered onto a now-1600x400 container looks awful; re-render on resize.
- **Responsive images inside dynamic containers** where the viewport width is not the right signal (use container queries if possible; fall back to `ResizeObserver` if you need JavaScript-level logic).
- **Detecting text wrapping changes** for tooltips that need to reposition.

Combined with `requestAnimationFrame` for smooth re-rendering:

```javascript
let scheduled = null;
const observer = new ResizeObserver((entries) => {
  if (scheduled) cancelAnimationFrame(scheduled);
  scheduled = requestAnimationFrame(() => {
    for (const entry of entries) {
      redraw(entry.target, entry.contentRect);
    }
    scheduled = null;
  });
});
```

## Part 9: `structuredClone`, JSON, And Deep Copying

For years, deep-cloning a JavaScript object meant `JSON.parse(JSON.stringify(obj))`. This fails on `Date`, `Map`, `Set`, `RegExp`, typed arrays, `undefined`, and cyclic references. It was always a hack.

`structuredClone(value)` does deep-clone the right way. [Baseline Widely Available since 2022](https://developer.mozilla.org/en-US/docs/Web/API/Window/structuredClone):

```javascript
const original = {
  name: "Alice",
  created: new Date(),
  tags: new Set(["admin", "user"]),
  metadata: new Map([["role", "admin"]]),
  children: [{ name: "Bob" }],
};

const copy = structuredClone(original);
copy.children[0].name = "Charlie";
console.log(original.children[0].name);   // still "Bob"
```

Handles every structured-cloneable type. Handles cycles. Is much faster than `JSON.parse(JSON.stringify(...))` for large objects. Use it whenever you need a deep copy. Gone are the days of `lodash.cloneDeep`.

### JSON — the things people miss

Two `JSON` features worth knowing:

**Reviver function:**

```javascript
const data = JSON.parse(text, (key, value) => {
  if (typeof value === "string" && /^\d{4}-\d{2}-\d{2}/.test(value)) {
    return new Date(value);
  }
  return value;
});
```

Converts ISO date strings to `Date` instances during parsing. Works for any pre-processing you want.

**Replacer function:**

```javascript
const text = JSON.stringify(data, (key, value) => {
  if (value instanceof Map) return Object.fromEntries(value);
  if (value instanceof Set) return [...value];
  return value;
});
```

Customises serialisation. Useful for non-JSON-native types.

**`toJSON` method:**

If an object has a `toJSON()` method, `JSON.stringify` calls it:

```javascript
class Money {
  constructor(amount, currency) {
    this.amount = amount;
    this.currency = currency;
  }
  toJSON() {
    return { amount: this.amount, currency: this.currency };
  }
}
```

This is how `Date` objects serialise to ISO strings automatically.

## Part 10: Scheduling — `requestAnimationFrame`, `requestIdleCallback`, `queueMicrotask`

JavaScript is single-threaded, but the browser gives you several ways to schedule work.

### `setTimeout` and `setInterval`

The classics. `setTimeout(fn, 0)` runs `fn` "as soon as possible after this turn of the event loop." `setInterval` repeats. Both return an ID you pass to `clearTimeout` / `clearInterval` to cancel.

Gotcha: the minimum timeout is actually clamped to about 4ms (sometimes more on background tabs). So `setTimeout(fn, 1)` is *at least* 4ms. For "run after current stack unwinds but before any I/O," use `queueMicrotask`:

```javascript
queueMicrotask(() => {
  // Runs after current JavaScript finishes, before any I/O.
});
```

### `requestAnimationFrame`

Schedules a callback for the next frame, just before the browser repaints. Returns an ID you can pass to `cancelAnimationFrame`. Use for:

- **Animations driven from JavaScript.** Paint on every frame at the browser's native refresh rate.
- **DOM reads-then-writes.** Measure layout, then mutate — rAF ensures no extra layout thrash.
- **Throttling expensive work.** "Do this at most once per frame" is a rAF-based idiom:

```javascript
let pending = false;
window.addEventListener("scroll", () => {
  if (pending) return;
  pending = true;
  requestAnimationFrame(() => {
    updateScrollProgress();
    pending = false;
  });
});
```

### `requestIdleCallback`

Schedules work for when the browser is idle — not actively rendering, handling input, or running other high-priority tasks. [Not yet Baseline in Safari as of 2026](https://caniuse.com/requestidlecallback), though widely implemented. Useful for non-critical background tasks:

```javascript
requestIdleCallback((deadline) => {
  while (deadline.timeRemaining() > 0 && workQueue.length > 0) {
    doWork(workQueue.shift());
  }
  if (workQueue.length > 0) {
    requestIdleCallback(processQueue);
  }
});
```

`deadline.timeRemaining()` tells you how many milliseconds you have before the browser needs the main thread back. You do as much work as fits, then reschedule.

Because Safari doesn't support it natively, a fallback:

```javascript
const ric = window.requestIdleCallback ??
            ((cb) => setTimeout(() => cb({ timeRemaining: () => 16 }), 1));
```

### `scheduler.postTask` — the newer, better API

[The Prioritized Task Scheduling API](https://developer.mozilla.org/en-US/docs/Web/API/Prioritized_Task_Scheduling_API) is a more powerful version of `setTimeout` with explicit priority:

```javascript
scheduler.postTask(() => doImportantWork(), { priority: "user-blocking" });
scheduler.postTask(() => doBackgroundWork(), { priority: "background" });
```

`"user-blocking"` runs ASAP; `"user-visible"` (the default) is normal; `"background"` is deferred. Not yet in Safari; shipping in Chrome and Firefox. Use with a fallback for now.

## Part 11: The Clipboard, URL, And Smaller APIs

### Clipboard

```javascript
// Write
await navigator.clipboard.writeText("copied!");

// Read (requires user gesture, prompts for permission)
const text = await navigator.clipboard.readText();
```

[Baseline Widely Available](https://developer.mozilla.org/en-US/docs/Web/API/Clipboard). Replaces the execCommand-based hacks of the past entirely.

For non-text (images, etc.), `navigator.clipboard.write([new ClipboardItem({ "image/png": blob })])`.

### URL and URLSearchParams

The `URL` class is a full-featured URL parser:

```javascript
const url = new URL("/api/posts?page=2", "https://example.com");
console.log(url.pathname);                    // "/api/posts"
console.log(url.searchParams.get("page"));    // "2"

url.searchParams.set("page", "3");
url.searchParams.set("sort", "date");
console.log(url.toString());
// "https://example.com/api/posts?page=3&sort=date"
```

No string concatenation. No manual URL encoding. The browser handles it.

### `crypto.randomUUID()`

```javascript
const id = crypto.randomUUID();   // "550e8400-e29b-41d4-a716-446655440000"
```

Cryptographically random UUIDs. No library. Replaces every `nanoid` or `uuid` npm package for ordinary uses.

### `crypto.subtle` — hashing, signing, encrypting

```javascript
const encoder = new TextEncoder();
const data = encoder.encode("hello world");
const hash = await crypto.subtle.digest("SHA-256", data);
const hex = [...new Uint8Array(hash)]
  .map(b => b.toString(16).padStart(2, "0"))
  .join("");
console.log(hex);
```

Full cryptographic primitives — hash (SHA-256, SHA-384, SHA-512), HMAC, AES, RSA. For most JavaScript code, `digest` is the only one you need. It is there. Use it.

### `performance.now()` and performance marks

```javascript
const start = performance.now();
doWork();
console.log(`Took ${performance.now() - start}ms`);
```

Sub-millisecond accuracy. Prefer to `Date.now()` for timing.

For more elaborate instrumentation:

```javascript
performance.mark("render-start");
renderPosts();
performance.mark("render-end");
performance.measure("render", "render-start", "render-end");

// Later
for (const entry of performance.getEntriesByType("measure")) {
  console.log(entry.name, entry.duration);
}
```

Marks and measures show up in Chrome DevTools' Performance panel, annotated with your labels.

### `navigator.share` — the Web Share API

```javascript
if (navigator.share) {
  await navigator.share({
    title: "My Blazor Magazine",
    text: "Check out this article",
    url: "https://observermagazine.github.io/blog/...",
  });
}
```

On mobile, opens the OS share sheet. On desktop, limited support (Chromium has it; Safari on macOS; Firefox no). Feature-detect and fall back to a copy-to-clipboard.

### Page Visibility

```javascript
document.addEventListener("visibilitychange", () => {
  if (document.hidden) {
    pauseVideo();
  } else {
    resumeVideo();
  }
});
```

Replaces ugly `focus` / `blur` tracking. Fires when the tab goes to a background tab, when the window is minimised, or when the screen locks.

### `beforeunload` vs `pagehide`

Use `pagehide` to save state when the user navigates away:

```javascript
window.addEventListener("pagehide", () => {
  saveFormDraft();
});
```

`beforeunload` is for prompting the user ("Are you sure you want to leave?") before a reload/close. Overuse is a dark pattern; most users have browsers that ignore the dialog anyway.

### `visualViewport`

The viewport is *not* the window when mobile keyboards are open, pinch-zoom is active, etc. `visualViewport` gives you the actual visible area:

```javascript
visualViewport.addEventListener("resize", () => {
  const bottomOffset = window.innerHeight - visualViewport.height;
  toolbar.style.bottom = `${bottomOffset}px`;
});
```

Useful for mobile apps that have floating UI near the bottom of the screen.

## Part 12: Data Attributes And The `dataset` API

HTML `data-*` attributes are a built-in way to attach structured data to elements. JavaScript reads them via `element.dataset`:

```html
<button data-action="delete" data-post-id="42" data-confirm="true">Delete</button>
```

```javascript
button.dataset.action       // "delete"
button.dataset.postId       // "42" — note camelCase conversion
button.dataset.confirm      // "true" — always a string
```

`data-post-id` becomes `dataset.postId` (kebab-case to camelCase). Values are always strings; parse as needed.

Setting works too:

```javascript
button.dataset.status = "pending";
// HTML now has data-status="pending"
```

Data attributes are excellent for:

- **Event delegation discrimination** (which action was clicked).
- **Styling state** — CSS can target `[data-status="pending"]`.
- **Passing data to components** without a framework.

## Part 13: `FormData` And `URLSearchParams`

Two siblings that cover most body/query handling.

### `FormData`

```javascript
const data = new FormData(formElement);
for (const [key, value] of data) console.log(key, value);
data.set("extra", "value");
data.delete("sensitive");

// Submit
await fetch("/api/submit", { method: "POST", body: data });
```

`new FormData(form)` captures every named field, including files. Iterable with `for...of`. Supports `get`, `getAll`, `has`, `set`, `append`, `delete`. Sends as `multipart/form-data` when passed as `body` to `fetch`.

### `URLSearchParams`

```javascript
const params = new URLSearchParams({ q: "dogs", page: 2 });
console.log(params.toString());   // "q=dogs&page=2"

// Iterate
for (const [key, value] of params) console.log(key, value);

// From a URL
const url = new URL(location.href);
url.searchParams.set("filter", "all");
history.replaceState(null, "", url);
```

`URLSearchParams` serialises as `application/x-www-form-urlencoded`, the old-school form encoding. Pair with `fetch` for API calls that expect form-encoded bodies:

```javascript
await fetch("/api/login", {
  method: "POST",
  headers: { "Content-Type": "application/x-www-form-urlencoded" },
  body: new URLSearchParams({ username, password }),
});
```

## Part 14: The Half Of jQuery You No Longer Need

Pulling the threads together, here is a small table mapping jQuery idioms to native equivalents. This should feel familiar — every row is something your codebase has probably done.

| jQuery | Native |
|---|---|
| `$("selector")` | `document.querySelectorAll("selector")` |
| `$(el).find("child")` | `el.querySelectorAll("child")` |
| `$(el).closest("x")` | `el.closest("x")` |
| `$(el).parent()` | `el.parentElement` |
| `$(el).addClass("x")` | `el.classList.add("x")` |
| `$(el).removeClass("x")` | `el.classList.remove("x")` |
| `$(el).toggleClass("x")` | `el.classList.toggle("x")` |
| `$(el).hasClass("x")` | `el.classList.contains("x")` |
| `$(el).attr("href")` | `el.getAttribute("href")` (or `el.href`) |
| `$(el).val()` | `el.value` |
| `$(el).text()` | `el.textContent` |
| `$(el).html()` | `el.innerHTML` |
| `$(el).on("click", fn)` | `el.addEventListener("click", fn)` |
| `$(el).off("click", fn)` | `el.removeEventListener("click", fn)` or `controller.abort()` |
| `$(document).ready(fn)` | `<script type="module">` (already deferred) |
| `$.ajax({ url })` | `fetch(url)` |
| `$.getJSON(url)` | `fetch(url).then(r => r.json())` |
| `$(el).each(fn)` | `el.forEach(fn)` or `for (const item of el)` |
| `$.extend({}, a, b)` | `{...a, ...b}` |
| `$.trim(s)` | `s.trim()` |
| `$.isArray(x)` | `Array.isArray(x)` |
| `$.parseJSON(s)` | `JSON.parse(s)` |
| `$.each(obj, fn)` | `for (const [k, v] of Object.entries(obj))` |
| `$(el).show()` | `el.hidden = false` |
| `$(el).hide()` | `el.hidden = true` |
| `$(el).css("color", "red")` | `el.style.color = "red"` |
| `$(el).data("key", val)` | `el.dataset.key = val` |
| `$(el).animate({...})` | `el.animate({...}, duration)` (Web Animations API) |
| `$(el).fadeIn()` | CSS `transition` + toggle class, or View Transitions |
| `$(window).on("resize", fn)` | `window.addEventListener("resize", fn)` |
| `$.debounce` | 12-line `debounce` helper (Part 1 story above) |

Every native form is shorter or equivalent. Every one avoids a wrapper function call. Every one has TypeScript types built in. jQuery solved real problems in 2006. In 2026, the platform solves them better.

## Part 15: A Practical `dom.js` Utility Module

Here is a complete utility file we use in every subsequent article of this series. Paste it into `js/utils/dom.js`:

```javascript
// @ts-check
/**
 * Shorthand for document.querySelector.
 * @param {string} selector
 * @param {ParentNode} [root]
 */
export function $(selector, root = document) {
  return root.querySelector(selector);
}

/**
 * Shorthand for querySelectorAll, returns an Array.
 * @param {string} selector
 * @param {ParentNode} [root]
 */
export function $$(selector, root = document) {
  return [...root.querySelectorAll(selector)];
}

/**
 * Create an element with attributes and children.
 * @param {string} tag
 * @param {Record<string, any>} [attrs]
 * @param {...(Node | string | null | undefined)} children
 */
export function h(tag, attrs = {}, ...children) {
  const el = document.createElement(tag);
  for (const [key, value] of Object.entries(attrs)) {
    if (value == null) continue;
    if (key === "class") el.className = value;
    else if (key === "style" && typeof value === "object") Object.assign(el.style, value);
    else if (key.startsWith("on") && typeof value === "function") {
      el.addEventListener(key.slice(2).toLowerCase(), value);
    } else if (key in el) el[key] = value;
    else el.setAttribute(key, String(value));
  }
  for (const child of children) {
    if (child != null) el.append(child);
  }
  return el;
}

/**
 * Debounce: call fn only after `ms` ms of silence.
 * @template {(...args: any[]) => any} F
 * @param {F} fn
 * @param {number} ms
 */
export function debounce(fn, ms) {
  let id;
  return /** @type {F} */ ((...args) => {
    clearTimeout(id);
    id = setTimeout(() => fn(...args), ms);
  });
}

/**
 * Throttle: call fn at most once per `ms` ms.
 * @template {(...args: any[]) => any} F
 * @param {F} fn
 * @param {number} ms
 */
export function throttle(fn, ms) {
  let last = 0;
  let id;
  return /** @type {F} */ ((...args) => {
    const now = Date.now();
    const wait = last + ms - now;
    clearTimeout(id);
    if (wait <= 0) {
      last = now;
      fn(...args);
    } else {
      id = setTimeout(() => { last = Date.now(); fn(...args); }, wait);
    }
  });
}

/**
 * Fetch JSON with a timeout.
 * @param {string} url
 * @param {RequestInit & { timeout?: number }} [init]
 */
export async function fetchJson(url, init = {}) {
  const { timeout = 10_000, signal, ...rest } = init;
  const signals = [AbortSignal.timeout(timeout)];
  if (signal) signals.push(signal);

  const response = await fetch(url, {
    ...rest,
    headers: { "Accept": "application/json", ...rest.headers },
    signal: AbortSignal.any(signals),
  });
  if (!response.ok) throw new Error(`HTTP ${response.status}`);
  return response.json();
}

/**
 * Observe intersection; call handler with (entry, observer).
 * @param {Element} target
 * @param {(entry: IntersectionObserverEntry, obs: IntersectionObserver) => void} onIntersect
 * @param {IntersectionObserverInit} [options]
 */
export function observeIntersection(target, onIntersect, options) {
  const obs = new IntersectionObserver((entries, o) => {
    for (const entry of entries) onIntersect(entry, o);
  }, options);
  obs.observe(target);
  return obs;
}

/**
 * Wait for an element to exist in the DOM.
 * @param {string} selector
 * @param {AbortSignal} [signal]
 * @returns {Promise<Element>}
 */
export function waitFor(selector, signal) {
  return new Promise((resolve, reject) => {
    const existing = document.querySelector(selector);
    if (existing) return resolve(existing);

    const observer = new MutationObserver(() => {
      const el = document.querySelector(selector);
      if (el) {
        observer.disconnect();
        resolve(el);
      }
    });
    observer.observe(document.body, { childList: true, subtree: true });

    signal?.addEventListener("abort", () => {
      observer.disconnect();
      reject(new DOMException("Aborted", "AbortError"));
    });
  });
}
```

Eighty lines of utilities, type-checked, no dependencies. We will use this in every remaining article in the series. You can too — copy it, rename it, modify it. It is yours.

## Part 16: When The Platform Isn't Enough

A brief honest note. The platform does a lot. But there are cases where a small, focused library still makes sense:

1. **Rich text editing.** Writing a full contenteditable editor is a career. Use [Lexical](https://lexical.dev/) or similar, loaded from esm.sh.

2. **Markdown rendering.** `marked` (~20KB) or `markdown-it`. Worth the dependency.

3. **HTML sanitisation.** DOMPurify. Much more battle-tested than the built-in Sanitizer API for the moment.

4. **Fuzzy search.** [Fuse.js](https://fusejs.io/) for a few thousand items. Small, well-scoped.

5. **Complex forms.** The Constraint Validation API (Day 12) handles most cases, but a few complex forms benefit from a specialised library.

6. **Virtual lists for very long data.** Hundreds of thousands of items. Even native rendering can choke; a virtualiser library pays for itself.

For everything else — the 95% of UI work most apps do — the platform is enough.

## Part 17: Tomorrow

Tomorrow — **Day 9: State Management Without a Library — Proxies, Signals from Scratch, and the Observer Pattern** — we build state management from first principles. No Redux, no Zustand, no Jotai, no MobX. A reactive store in 50 lines. Signals from scratch. A pattern that scales from a single counter to a full application. If you have been terrified of "global state" because of framework baggage, tomorrow is the article that restores your confidence.

See you tomorrow.

---

## Series navigation

You are reading **Part 8 of 15**.

- [Part 1: Overview — Why a Plain Browser Is Enough in 2026](/blog/2026-05-24-no-build-web-overview)
- [Part 2: Semantic HTML and the Document Outline Nobody Taught You](/blog/2026-05-25-no-build-web-html-semantics)
- [Part 3: The Cascade, Specificity, and `@layer`](/blog/2026-05-26-no-build-web-css-cascade-layers)
- [Part 4: Modern CSS Layout — Flexbox, Grid, Subgrid, and Container Queries](/blog/2026-05-27-no-build-web-modern-css-layout)
- [Part 5: Responsive Design in 2026](/blog/2026-05-28-no-build-web-responsive-design)
- [Part 6: Colour, Typography, and Motion](/blog/2026-05-29-no-build-web-color-typography-motion)
- [Part 7: Native ES Modules](/blog/2026-05-30-no-build-web-es-modules)
- **Part 8 (today): The DOM, Events, and Platform Primitives Everyone Reimplemented**
- Part 9 (tomorrow): State Management Without a Library
- Part 10: Web Components
- Part 11: Client-Side Routing with the Navigation API and View Transitions
- Part 12: Forms, Validation, and the Constraint Validation API
- Part 13: Storage, Service Workers, and Offline
- Part 14: Accessibility, Performance, and Security
- Part 15: The Conclusion — A Complete Application End to End

## Resources

- [MDN: Document](https://developer.mozilla.org/en-US/docs/Web/API/Document) — the top-level reference.
- [MDN: Element](https://developer.mozilla.org/en-US/docs/Web/API/Element) — every DOM element's interface.
- [MDN: EventTarget](https://developer.mozilla.org/en-US/docs/Web/API/EventTarget) — `addEventListener` in detail.
- [MDN: AbortController](https://developer.mozilla.org/en-US/docs/Web/API/AbortController) — cancellation for fetches, listeners, and more.
- [MDN: fetch](https://developer.mozilla.org/en-US/docs/Web/API/fetch) — the network request API.
- [MDN: IntersectionObserver](https://developer.mozilla.org/en-US/docs/Web/API/IntersectionObserver) — viewport intersection tracking.
- [MDN: MutationObserver](https://developer.mozilla.org/en-US/docs/Web/API/MutationObserver) — DOM change notifications.
- [MDN: ResizeObserver](https://developer.mozilla.org/en-US/docs/Web/API/ResizeObserver) — size change notifications.
- [MDN: structuredClone](https://developer.mozilla.org/en-US/docs/Web/API/Window/structuredClone) — deep cloning.
- [MDN: URL](https://developer.mozilla.org/en-US/docs/Web/API/URL) — URL parsing and construction.
- [MDN: FormData](https://developer.mozilla.org/en-US/docs/Web/API/FormData) — form-encoded data.
- [MDN: Web Crypto API](https://developer.mozilla.org/en-US/docs/Web/API/Web_Crypto_API) — cryptographic primitives.
- [YouMightNotNeedJQuery.com](https://youmightnotneedjquery.com/) — the canonical jQuery-to-native reference.
- [web.dev: Request Idle Callback](https://web.dev/requestidlecallback/) — scheduling background work.
