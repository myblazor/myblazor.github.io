---
title: "Without a Net, Part 13: Storage, Service Workers, and Offline"
date: 2026-06-05
author: myblazor-team
summary: "Day 13 of our fifteen-part no-build web series covers persistence and offline. We walk through every storage option the browser ships — localStorage, sessionStorage, IndexedDB, the Cache API, the Origin Private File System, and the smaller utilities like Cookies — explaining when each is right. Then we build a complete service worker with stale-while-revalidate caching, a precache for the app shell, an offline fallback page, navigation preload for fast first responses, and a Background Sync queue for offline form submissions. We finish with the manifest file that turns the site into an installable PWA. By the end, our magazine works fully offline, installs to the home screen, and survives a flaky train Wi-Fi connection — built from scratch in about 250 lines."
tags:
  - javascript
  - service-workers
  - pwa
  - localstorage
  - indexeddb
  - cache-api
  - background-sync
  - offline
  - web-standards
  - no-build
  - first-principles
  - series
  - deep-dive
---

## Introduction: The Train From Hyderabad To Bengaluru

We were riding the Garib Rath express from Hyderabad to Bengaluru a few summers ago — overnight train, twelve hours, cheap-and-cheerful sleeper compartments. The colleague we were travelling with had her laptop open, working on a draft article. We were on a phone, trying to pull up the article she had asked us to read first.

The Wi-Fi was the railway's, which meant: not really. There were occasional moments of two-bar 4G as the train passed near a town, then long stretches of nothing through farmland, then a brief window of 3G at a station where the train sat for ten minutes, then nothing again. This is the actual experience of most internet users in most of the world. Not "the Wi-Fi is slow." Not "I have one bar." More like: the connection comes and goes, in unpredictable bursts, and any application that assumes "I have the internet" or "I do not have the internet" as a stable binary state is going to feel broken for hours.

We pulled up our colleague's draft. It was hosted on a popular collaborative-editing platform that we will not name. The page loaded — we had a good moment of signal — and immediately replaced its content with a spinner saying "Connecting…". The spinner stayed on the screen for the next eleven hours. The article we had loaded successfully was somewhere in the JavaScript heap, but the application had decided that without a live WebSocket connection it could not show us anything. We watched it spin past Kurnool, past Anantapur, past Penukonda, all the way to Yelahanka. We were unable to read text that we had successfully downloaded onto our phone, because the app refused to render it without a server handshake.

This is the experience an enormous amount of "modern" web tooling produces by default. The defaults assume a stable, fast connection. They handle "online" gracefully and "offline" with a fatal-looking error. The middle ground — flaky, intermittent, slow — is the actual experience of billions of people, and most production sites treat it the way that platform treated us: sit and spin, indefinitely, on a screen with no useful content. There is no excuse for this. The platform has had the primitives to build offline-capable, intermittently-connected web applications since Service Workers shipped in Chrome in 2015 and reached every browser by 2018. The work to use those primitives is modest — a few hundred lines of JavaScript per site. We have just collectively chosen not to do it.

Day 13 is our complete tour of the persistence and offline story. We will cover the seven storage mechanisms the browser exposes (yes, seven; you may not know all of them), explain when each is right, then build a complete service worker that turns our magazine into an offline-first application that survives a train ride from Hyderabad to Bengaluru. We will set up the manifest file that makes it installable to the home screen. We will discuss Background Sync and where it shines and where it does not. We will be honest about browser-support gaps — Background Sync is Chrome-only in 2026, and we will say so plainly.

By the end you will have a working, installable PWA built from scratch in about 250 lines of mostly-declarative configuration. No Workbox. No `sw-precache`. No npm. The result is faster than every PWA generator's output, because every line is written for your specific case.

This is Part 13 of 15 in our no-build web series. After [Day 11's routing](/blog/2026-06-03-no-build-web-routing-and-navigation) and [Day 12's forms](/blog/2026-06-04-no-build-web-forms-and-validation), we have a complete interactive application. Today we make it survive the network.

## Part 1: The Seven Storage Mechanisms

The browser exposes seven distinct storage APIs. Each has a use case. Most developers use one (`localStorage`) and ignore the others, which is fine for tiny sites and limiting for everything else. The full list:

| API | Capacity | Sync/async | Persists | Use for |
|---|---|---|---|---|
| **`localStorage`** | ~5–10MB | Sync | Until cleared | User preferences, small JSON state |
| **`sessionStorage`** | ~5–10MB | Sync | Until tab closes | Per-tab transient state |
| **Cookies** | ~4KB total | Sync | Configurable | Server-readable session info |
| **IndexedDB** | Large (gigabytes) | Async | Until cleared | Large structured data, blobs |
| **Cache API** | Large (gigabytes) | Async | Until cleared | HTTP response caching for SW |
| **OPFS** (Origin Private File System) | Large (gigabytes) | Sync (in worker) / async | Until cleared | Fast file-like binary storage |
| **`window.name`** | ~2MB | Sync | Until tab closes | Niche cross-page messaging (rarely useful) |

We will cover all of them in this article, with the most depth on `localStorage`, `IndexedDB`, and the Cache API — the three you will actually reach for.

### Quotas and eviction

The browser does not give you unlimited storage. Quotas vary:

- **Per-origin quota**: typically a percentage of disk space. On a 256GB phone, an origin might get 8–32GB.
- **Per-API quota**: `localStorage` and `sessionStorage` are limited to ~5–10MB each. Cookies are ~4KB total per domain. IndexedDB, Cache API, and OPFS share the per-origin quota.
- **Eviction**: under storage pressure, the browser may evict data from less-recently-visited origins. You can request "persistent" storage that resists eviction:

```javascript
const persisted = await navigator.storage.persist();
// `persisted` is true if the request was granted.
```

The browser grants persistence based on heuristics — bookmarked sites, installed PWAs, frequently visited sites are more likely to get it. For an installed PWA, you almost always get persistence. For a casual browsing site, you almost never do.

To check current usage:

```javascript
const estimate = await navigator.storage.estimate();
console.log(`Used: ${estimate.usage} of ${estimate.quota}`);
```

Useful for pre-flighting large operations and warning the user before they hit the quota.

## Part 2: `localStorage` And `sessionStorage`

The simplest storage. Synchronous. String-only. Works in every browser since around 2010.

```javascript
localStorage.setItem("theme", "dark");
const theme = localStorage.getItem("theme");
localStorage.removeItem("theme");
localStorage.clear();   // wipes everything for this origin

// Iterate
for (let i = 0; i < localStorage.length; i++) {
  const key = localStorage.key(i);
  console.log(key, localStorage.getItem(key));
}
```

Values are always strings. To store objects, serialise:

```javascript
localStorage.setItem("user", JSON.stringify(user));
const user = JSON.parse(localStorage.getItem("user") ?? "null");
```

`sessionStorage` has the same API but the data is scoped to the current browser tab and disappears when the tab closes. Useful for "wizard state that should not survive accidental tab close."

### When to use, when not to

**Use `localStorage` for:**

- User preferences (theme, sidebar collapsed, language).
- Small piece of UI state to persist (last-viewed page, filter settings).
- Small caches (rarely-changing config that you fetch once and reuse).

**Do not use `localStorage` for:**

- **Authentication tokens.** It is accessible from any JavaScript on the page, including injected XSS attacks. Use `httpOnly` cookies for tokens; the server sets them, JavaScript cannot read them.
- **Large amounts of data.** It is synchronous, which means writing or reading blocks the main thread. Hundreds of KB of `localStorage` activity in one go will cause visible jank.
- **Anything you need to query.** It is a flat key-value store with no indexing, no search, no transactions.
- **Data that changes from multiple tabs simultaneously.** Use `BroadcastChannel` (Day 9) for cross-tab notifications.

### The storage event

When `localStorage` changes in *another* tab of the same origin, your tab gets a `storage` event:

```javascript
window.addEventListener("storage", (event) => {
  console.log(event.key, "changed from", event.oldValue, "to", event.newValue);
});
```

This is the cheapest cross-tab sync mechanism. Fires only on changes from *other* tabs (not the one making the change). Combine with `BroadcastChannel` for richer messaging.

### Common bugs

- **Quota exceeded.** Wrap writes in `try/catch`; `setItem` throws `QuotaExceededError` when full.
- **Disabled in private browsing.** Some browsers expose `localStorage` in private mode but throw on write. `try/catch` your writes.
- **JSON parse failures.** A previous build set the value in a different format; the new code's `JSON.parse` throws. Wrap parses too: `try { JSON.parse(...) } catch { return defaultValue }`.

## Part 3: Cookies — Still Useful, Often Misunderstood

Cookies are old (1994) and famously unloved. They have a few specific uses where they remain the right tool.

```javascript
document.cookie = "theme=dark; max-age=31536000; path=/; SameSite=Lax";
```

The shape:

- `name=value` — the cookie itself.
- `max-age=N` (seconds) or `expires=...` — lifetime.
- `path=...` — URL path scope (default: current page's directory).
- `domain=...` — domain scope (default: current domain).
- `Secure` — HTTPS only.
- `HttpOnly` — JavaScript cannot read (server-set only).
- `SameSite=Strict | Lax | None` — cross-site behaviour.

Use cookies for:

- **Authentication tokens.** Set `HttpOnly` so XSS cannot steal them. Set `Secure` so they are only sent over HTTPS. Set `SameSite=Lax` (the default in modern browsers) to prevent most CSRF attacks.
- **Server-side state that needs to survive without JavaScript.** A user's language preference might be a cookie so the server can render in the right language on first request.

Do not use cookies for:

- Large data — they are limited to ~4KB and sent with every HTTP request.
- Client-only state — `localStorage` is faster and not transmitted to the server.

### The Cookie Store API — the modern interface

`document.cookie` is a famously bad string-concatenation API. The [Cookie Store API](https://developer.mozilla.org/en-US/docs/Web/API/Cookie_Store_API) is its replacement: async, structured, and clean.

```javascript
await cookieStore.set({
  name: "theme",
  value: "dark",
  expires: Date.now() + 31536000_000,
  path: "/",
  sameSite: "lax",
});

const cookie = await cookieStore.get("theme");

cookieStore.addEventListener("change", (event) => {
  console.log("Changed cookies:", event.changed, event.deleted);
});
```

Currently Chrome-only as of 2026 — Firefox and Safari are evaluating. For broadly-compatible code, the old `document.cookie` still works.

## Part 4: IndexedDB — The Real Database

When you outgrow `localStorage`, IndexedDB is what you want. It is a transactional, indexed, async key-value-and-object store with megabyte-to-gigabyte capacity. Every browser has supported it since around 2013.

It is also famously verbose. The raw API is event-based (older than `Promise`), uses request objects, and requires version-bumping for schema changes. A simple "store an object, retrieve it later" interaction is about 30 lines of code.

For most uses, wrap it. Either with the excellent [`idb`](https://github.com/jakearchibald/idb) library (~2KB from esm.sh, written by Jake Archibald who also wrote service workers), or with a small in-house wrapper.

### A small in-house wrapper

```javascript
// @ts-check
/** Open an IndexedDB database with a simple object-store schema. */
export function openDB(name, version, stores) {
  return new Promise((resolve, reject) => {
    const request = indexedDB.open(name, version);
    request.onerror = () => reject(request.error);
    request.onsuccess = () => resolve(request.result);
    request.onupgradeneeded = () => {
      const db = request.result;
      for (const [storeName, options] of Object.entries(stores)) {
        if (!db.objectStoreNames.contains(storeName)) {
          db.createObjectStore(storeName, options);
        }
      }
    };
  });
}

/** Run a transaction returning a promise. */
function tx(db, store, mode, fn) {
  return new Promise((resolve, reject) => {
    const transaction = db.transaction(store, mode);
    const objectStore = transaction.objectStore(store);
    const result = fn(objectStore);
    transaction.oncomplete = () => resolve(result);
    transaction.onerror = () => reject(transaction.error);
  });
}

export async function dbGet(db, store, key) {
  return tx(db, store, "readonly", (s) => {
    return new Promise((resolve, reject) => {
      const r = s.get(key);
      r.onsuccess = () => resolve(r.result);
      r.onerror = () => reject(r.error);
    });
  });
}

export async function dbSet(db, store, value, key) {
  return tx(db, store, "readwrite", (s) => key === undefined ? s.put(value) : s.put(value, key));
}

export async function dbDelete(db, store, key) {
  return tx(db, store, "readwrite", (s) => s.delete(key));
}

export async function dbList(db, store) {
  return tx(db, store, "readonly", (s) => {
    return new Promise((resolve, reject) => {
      const r = s.getAll();
      r.onsuccess = () => resolve(r.result);
      r.onerror = () => reject(r.error);
    });
  });
}
```

Usage:

```javascript
const db = await openDB("magazine", 1, {
  posts: { keyPath: "id" },
  drafts: { keyPath: "id", autoIncrement: true },
});

await dbSet(db, "posts", { id: "hello-world", title: "Hello World", body: "..." });
const post = await dbGet(db, "posts", "hello-world");
const allPosts = await dbList(db, "posts");
await dbDelete(db, "posts", "hello-world");
```

For most application use, those four operations cover almost everything. For more advanced needs (indexes, range queries, cursors), the raw API is there underneath.

### When to use IndexedDB

- **Caching API responses for offline use.** Instead of refetching a list of posts every load, cache them in IDB and read on next launch.
- **Large structured data.** Anything bigger than a few hundred KB. IDB handles megabytes happily.
- **Binary data.** `Blob` objects (images, PDFs, audio) can be stored directly.
- **Drafts and offline-only data.** Save state that should survive the user closing the tab and even the browser quitting.
- **Anything you want to query by index.** Search by author, by tag, by date range.

### When NOT to use IndexedDB

- **Tiny preferences.** `localStorage` is simpler.
- **Authentication tokens.** Cookies (`HttpOnly`) are still right.
- **Data that should reach a server quickly.** IDB is local-only; you need to sync separately.

## Part 5: The Cache API — The Companion To Service Workers

The Cache API stores `Request`/`Response` pairs. It is designed for service worker use, but is available from regular pages too:

```javascript
const cache = await caches.open("v1");
await cache.put("/posts.json", new Response(JSON.stringify(posts)));
const cached = await cache.match("/posts.json");
if (cached) {
  const data = await cached.json();
}
```

The interface is built around the same `Request`/`Response` objects you use with `fetch`, which makes it natural to use as a pass-through cache.

### Why a separate API for HTTP caching?

The browser's built-in HTTP cache is opaque — you cannot inspect or control it from JavaScript. The Cache API gives you a programmatic, namespaced cache that you control completely. You can:

- Pre-populate caches at install time (precaching the app shell).
- Implement custom strategies — stale-while-revalidate, network-first, cache-first.
- Use multiple named caches for different purposes (`shell-v1`, `images-v1`, `posts-v1`) and clean them up independently.
- Inspect contents (`cache.keys()`).
- Delete selectively.

We will use the Cache API extensively in our service worker below.

## Part 6: OPFS — The File System You Did Not Know You Had

The [Origin Private File System](https://developer.mozilla.org/en-US/docs/Web/API/File_System_API/Origin_private_file_system) is a real, fast file system per origin. [Baseline since 2024](https://web-platform-dx.github.io/web-features-explorer/features/file-system-access/). Use it when you need:

- **Genuinely fast binary storage** — orders of magnitude faster than IndexedDB for blob-shaped data. Used by SQLite-in-the-browser implementations for performance.
- **File-like operations** — `read`, `write`, `seek`, `truncate`. Useful for log files, video edit caches, etc.
- **Synchronous access from a worker** — the only place sync filesystem operations are allowed.

```javascript
const root = await navigator.storage.getDirectory();
const fileHandle = await root.getFileHandle("notes.txt", { create: true });
const writable = await fileHandle.createWritable();
await writable.write("Hello, OPFS");
await writable.close();

const file = await fileHandle.getFile();
const text = await file.text();
console.log(text);
```

For 95% of web apps, you will not need OPFS. For the 5% that do — text editors with autosave, image editors with undo histories, databases — it is invaluable.

## Part 7: Service Workers — The Big Idea

A service worker is a script that runs in a separate thread, intercepts network requests from your pages, and can serve responses from cache. It is the foundation of every offline-capable web app.

The key facts:

- **Per-origin singleton.** One service worker per origin (with one exception: scope, see below). Controls all pages in its scope.
- **Lifecycle independent of pages.** Installs once, persists across page loads, can wake up to handle background events.
- **Runs in a worker context.** No DOM, no `window`. Access to `fetch`, `Cache`, `IndexedDB`, `BroadcastChannel`, and a few other APIs.
- **HTTPS only.** Service workers require a secure context. `localhost` is exempt for development.
- **Same-origin scope.** A service worker registered at `/sw.js` controls all pages under `/`. Registered at `/app/sw.js`, controls `/app/...`.

### Registration

In your page (or an entry-point module):

```javascript
if ("serviceWorker" in navigator) {
  window.addEventListener("load", async () => {
    try {
      const registration = await navigator.serviceWorker.register("/sw.js", {
        type: "module",
        scope: "/",
      });
      console.log("Service worker registered:", registration.scope);
    } catch (error) {
      console.error("Service worker registration failed:", error);
    }
  });
}
```

`type: "module"` lets the SW use `import` statements — much nicer for organising larger SWs. `scope: "/"` ensures the SW controls every page on the site.

### The lifecycle

A service worker has three lifecycle states:

1. **`install`** — the SW is being installed (first time, or after the script changed).
2. **`activate`** — the SW is now controlling pages.
3. **`fetch`** / **`message`** / **`sync`** / etc. — operating events.

```javascript
self.addEventListener("install", (event) => {
  console.log("SW installing");
  event.waitUntil(precache());   // hold the install open until precache completes
});

self.addEventListener("activate", (event) => {
  console.log("SW activated");
  event.waitUntil(cleanupOldCaches());
});

self.addEventListener("fetch", (event) => {
  event.respondWith(handleFetch(event.request));
});
```

The `event.waitUntil(promise)` pattern keeps the SW alive until the promise settles. Without it, the browser may terminate the SW before your async work completes.

### Update and `skipWaiting`

When you change `sw.js`, the browser detects the byte-for-byte difference (compared to the cached SW), installs the new version, but **does not activate** it until the old version's clients have all closed. This is the source of the "I deployed but my changes don't show up" frustration with SWs.

To take over immediately:

```javascript
self.addEventListener("install", (event) => {
  self.skipWaiting();   // skip the "waiting" phase
  event.waitUntil(precache());
});

self.addEventListener("activate", (event) => {
  event.waitUntil(self.clients.claim());   // take control of existing clients
});
```

This is usually what you want during development. In production, the conservative default exists for good reason — version skew can cause subtle bugs if the new SW serves new code to a page running old code. Decide deliberately.

## Part 8: A Complete Service Worker

Here is a production-shape service worker for our magazine. Annotated heavily; the unannotated version is about 100 lines.

```javascript
// @ts-check
// sw.js — service worker for My Blazor Magazine

const VERSION = "v1.0.5";
const SHELL_CACHE = `shell-${VERSION}`;
const RUNTIME_CACHE = `runtime-${VERSION}`;
const IMAGES_CACHE = `images-${VERSION}`;

// Precached files: the "app shell" that should always work offline.
const PRECACHE_URLS = [
  "/",
  "/index.html",
  "/offline.html",
  "/css/app.css",
  "/js/app.js",
  "/js/router.js",
  "/manifest.webmanifest",
];

// =====================================================================
// INSTALL: pre-cache the app shell
// =====================================================================
self.addEventListener("install", (event) => {
  event.waitUntil((async () => {
    const cache = await caches.open(SHELL_CACHE);
    await cache.addAll(PRECACHE_URLS);
    self.skipWaiting();
  })());
});

// =====================================================================
// ACTIVATE: clean up old caches, claim clients, enable navigation preload
// =====================================================================
self.addEventListener("activate", (event) => {
  event.waitUntil((async () => {
    const cacheNames = await caches.keys();
    await Promise.all(
      cacheNames
        .filter((name) => !name.endsWith(VERSION))
        .map((name) => caches.delete(name))
    );

    if (self.registration.navigationPreload) {
      await self.registration.navigationPreload.enable();
    }

    await self.clients.claim();
  })());
});

// =====================================================================
// FETCH: route by request type
// =====================================================================
self.addEventListener("fetch", (event) => {
  const request = event.request;
  const url = new URL(request.url);

  // Only handle same-origin GET requests.
  if (request.method !== "GET" || url.origin !== location.origin) return;

  // Navigation requests (HTML pages): network-first, fall back to shell, then offline page
  if (request.mode === "navigate") {
    event.respondWith(navigationStrategy(event));
    return;
  }

  // Static shell assets: cache-first
  if (PRECACHE_URLS.includes(url.pathname)) {
    event.respondWith(cacheFirst(request, SHELL_CACHE));
    return;
  }

  // Images: cache-first, with runtime caching
  if (request.destination === "image") {
    event.respondWith(cacheFirst(request, IMAGES_CACHE));
    return;
  }

  // Posts (JSON): stale-while-revalidate
  if (url.pathname.startsWith("/content/posts/")) {
    event.respondWith(staleWhileRevalidate(request, RUNTIME_CACHE));
    return;
  }

  // Everything else: network-first
  event.respondWith(networkFirst(request, RUNTIME_CACHE));
});

// =====================================================================
// STRATEGIES
// =====================================================================

async function navigationStrategy(event) {
  try {
    // Use the navigation preload response if available
    const preload = await event.preloadResponse;
    if (preload) return preload;

    const response = await fetch(event.request);
    return response;
  } catch {
    // Network failed; serve the cached app shell
    const cache = await caches.open(SHELL_CACHE);
    const cached = await cache.match("/index.html");
    if (cached) return cached;
    return cache.match("/offline.html");
  }
}

async function cacheFirst(request, cacheName) {
  const cache = await caches.open(cacheName);
  const cached = await cache.match(request);
  if (cached) return cached;

  try {
    const response = await fetch(request);
    if (response.ok) cache.put(request, response.clone());
    return response;
  } catch {
    return new Response("", { status: 503, statusText: "Offline" });
  }
}

async function networkFirst(request, cacheName) {
  const cache = await caches.open(cacheName);
  try {
    const response = await fetch(request);
    if (response.ok) cache.put(request, response.clone());
    return response;
  } catch {
    const cached = await cache.match(request);
    return cached ?? new Response("", { status: 503, statusText: "Offline" });
  }
}

async function staleWhileRevalidate(request, cacheName) {
  const cache = await caches.open(cacheName);
  const cached = await cache.match(request);

  const fetchPromise = fetch(request).then((response) => {
    if (response.ok) cache.put(request, response.clone());
    return response;
  }).catch(() => null);

  // Return cached immediately if we have it, but kick off the refetch
  return cached ?? await fetchPromise ?? new Response("", { status: 503 });
}
```

That is a complete, production-shape service worker. It handles:

- **Pre-caching** of the app shell on install — the user can launch the app fully offline after one online visit.
- **Cache versioning** — each deploy bumps `VERSION`, old caches are cleaned up.
- **Navigation preload** — for non-cached navigations, the browser starts the network request in parallel with the SW boot, reducing time-to-first-byte.
- **Different strategies for different content types** — cache-first for static assets and images, stale-while-revalidate for content that might update, network-first for everything else.
- **Offline fallback** — if all else fails, serve `/offline.html`.

A simple `offline.html`:

```html
<!doctype html>
<html lang="en">
<head>
  <meta charset="utf-8">
  <title>Offline</title>
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <style>
    body { font-family: system-ui; max-width: 30rem; margin: 4rem auto; padding: 1rem; }
    h1 { font-size: 1.5rem; }
  </style>
</head>
<body>
  <h1>You are offline</h1>
  <p>This page is not in the cache. Please check your connection and try again.</p>
  <p><a href="/">Return home</a></p>
</body>
</html>
```

Pre-cached in the SW's install step, served as a fallback when the user requests an uncached page while offline.

## Part 9: Caching Strategies — The Decision Framework

Each strategy has trade-offs. A summary:

| Strategy | Behaviour | Best for |
|---|---|---|
| **Cache-first** | Serve cache; fetch only if not cached. | Static assets that never change (versioned URLs), images. |
| **Network-first** | Try network; fall back to cache. | Live data; the user wants the freshest version. |
| **Stale-while-revalidate** | Serve cache immediately; update in background. | Content that updates but is not critical to be live. |
| **Network-only** | Always fetch. Never cache. | Login, payments, anything sensitive or live. |
| **Cache-only** | Always serve from cache. Never fetch. | Pre-built static assets in a known cache. |

A common pattern combines them: precached shell uses cache-only, content uses stale-while-revalidate, images use cache-first, API calls use network-first.

### Cache hygiene

Caches grow. Without pruning, they will eventually fill the quota and the browser will start evicting (potentially your important precached files). Prune by age or size:

```javascript
async function trimCache(cacheName, maxItems) {
  const cache = await caches.open(cacheName);
  const keys = await cache.keys();
  if (keys.length > maxItems) {
    const toDelete = keys.slice(0, keys.length - maxItems);
    await Promise.all(toDelete.map((key) => cache.delete(key)));
  }
}
```

Call `trimCache(IMAGES_CACHE, 100)` periodically (or after each `cache.put`) to keep the images cache to 100 items.

### Don't cache opaque responses

Cross-origin responses without CORS are "opaque" — you cannot read their status, and they take up the *full size of the response* in the cache (because the browser cannot tell what content-length they have). A handful of opaque CDN scripts can blow your quota in a hurry. Filter them out:

```javascript
if (response.ok && response.type !== "opaque") {
  cache.put(request, response.clone());
}
```

## Part 10: Background Sync — Honest About Browser Support

The Background Sync API lets a service worker re-send failed requests when the network comes back. The classic use case: a user composes a comment offline, taps Submit, the request is queued, and when their connection returns, the service worker sends it without the user doing anything.

The shape:

```javascript
// In the page:
async function postComment(text) {
  try {
    await fetch("/api/comments", { method: "POST", body: text });
  } catch {
    // Save to IDB and register a sync
    await saveCommentDraft(text);
    const registration = await navigator.serviceWorker.ready;
    await registration.sync.register("send-comments");
  }
}

// In the service worker:
self.addEventListener("sync", (event) => {
  if (event.tag === "send-comments") {
    event.waitUntil(sendQueuedComments());
  }
});

async function sendQueuedComments() {
  const drafts = await loadCommentDrafts();
  for (const draft of drafts) {
    try {
      await fetch("/api/comments", { method: "POST", body: draft.text });
      await deleteCommentDraft(draft.id);
    } catch {
      // Will retry next time the sync fires
      throw new Error("Sync failed; will retry");
    }
  }
}
```

This is the right shape for offline-tolerant UX. The user does not see an error; the request just gets sent later.

**The honest caveat: as of 2026, Background Sync is Chromium-only.** [Firefox does not support it](https://caniuse.com/background-sync) and has no announced plans. Safari does not support it. If you build offline submission with Background Sync, your fallback for Firefox/Safari users is a queue that gets drained when the user returns to the page (via a `visibilitychange` handler):

```javascript
document.addEventListener("visibilitychange", async () => {
  if (document.visibilityState === "visible" && navigator.onLine) {
    await sendQueuedComments();
  }
});
```

Less elegant — only fires when the user returns to the tab — but works everywhere. Combine the two: register a Background Sync if available *and* have the visibility-based fallback. Chrome users get the proper experience; everyone else still gets their comments sent eventually.

### Periodic Background Sync — even more limited

Periodic Background Sync (`registration.periodicSync`) lets the SW wake up periodically to refresh content. *Also Chromium-only*. Useful in narrow cases (news apps that want fresh content on next launch); for cross-browser code, do the refresh on page load or on `visibilitychange`.

## Part 11: The Web App Manifest — Making It Installable

A `manifest.webmanifest` file declares your site as a "web application" that browsers can install to the home screen / desktop. It is JSON, sits alongside your HTML, linked from `<head>`:

```html
<link rel="manifest" href="/manifest.webmanifest">
```

A complete manifest:

```json
{
  "name": "My Blazor Magazine",
  "short_name": "Magazine",
  "description": "Long-form articles on web development.",
  "start_url": "/",
  "scope": "/",
  "display": "standalone",
  "orientation": "portrait",
  "background_color": "#ffffff",
  "theme_color": "#3366cc",
  "lang": "en",
  "dir": "ltr",
  "categories": ["news", "magazines", "education"],
  "icons": [
    { "src": "/icons/icon-192.png", "sizes": "192x192", "type": "image/png" },
    { "src": "/icons/icon-512.png", "sizes": "512x512", "type": "image/png" },
    {
      "src": "/icons/maskable-512.png",
      "sizes": "512x512",
      "type": "image/png",
      "purpose": "maskable"
    }
  ],
  "screenshots": [
    {
      "src": "/screenshots/desktop.png",
      "sizes": "1280x720",
      "type": "image/png",
      "form_factor": "wide"
    },
    {
      "src": "/screenshots/mobile.png",
      "sizes": "390x844",
      "type": "image/png",
      "form_factor": "narrow"
    }
  ],
  "shortcuts": [
    {
      "name": "Latest articles",
      "short_name": "Latest",
      "url": "/blog",
      "icons": [{ "src": "/icons/blog.png", "sizes": "96x96" }]
    }
  ]
}
```

Field-by-field:

- **`name`** / **`short_name`** — full and abbreviated names. The short name is used on the home screen icon.
- **`description`** — for app stores and install prompts.
- **`start_url`** — the URL the app opens at when launched. Often `/` or `/?source=pwa` for analytics.
- **`scope`** — which URLs are "in" the app. Usually `/`.
- **`display`** — `"browser"` (regular tab), `"minimal-ui"` (browser chrome simplified), `"standalone"` (looks like a native app), `"fullscreen"` (no chrome at all). `"standalone"` is the usual choice.
- **`background_color`** / **`theme_color`** — used during the splash screen and for the OS title bar.
- **`icons`** — required for installability. Need at least 192×192 and 512×512 PNG. Add a `purpose: "maskable"` icon for proper Android adaptive-icon rendering.
- **`screenshots`** — required by some browsers (and recommended) to show in the install prompt. Provide both `wide` and `narrow` form factors.
- **`shortcuts`** — appear in the right-click menu of the home-screen icon (Android) or in the dock menu (macOS).

### Triggering install

Browsers decide when to show the "Install this app" UI. The criteria, roughly:

1. The site has a valid manifest with required fields.
2. The site has a service worker with a `fetch` handler.
3. The site is served over HTTPS.
4. The user has interacted with the site (varies by browser).

For some control, listen for `beforeinstallprompt`:

```javascript
let deferredPrompt;

window.addEventListener("beforeinstallprompt", (event) => {
  event.preventDefault();
  deferredPrompt = event;
  document.getElementById("install-button").hidden = false;
});

document.getElementById("install-button").addEventListener("click", async () => {
  if (!deferredPrompt) return;
  deferredPrompt.prompt();
  const { outcome } = await deferredPrompt.userChoice;
  console.log("Install:", outcome);
  deferredPrompt = null;
});
```

This is Chromium's API — Safari triggers the install via "Add to Home Screen" in the share menu (no programmatic API). Firefox supports it on Android. Show your install button only when `beforeinstallprompt` fires; for Safari users, document the manual steps elsewhere on the site.

### `display_override`

The newer `display_override` array gives finer control:

```json
"display_override": ["window-controls-overlay", "standalone", "minimal-ui"]
```

The browser tries each in order. `window-controls-overlay` lets your app draw into the title bar area on desktop. `standalone` is the standard PWA display.

## Part 12: Testing The Whole Thing

For our magazine, the install flow is:

1. User visits the site with Chrome, browses a few articles.
2. After a few sessions, Chrome shows "Install" in the address bar.
3. User clicks Install. The site adds itself to the home screen / Start menu / Dock.
4. Launching from the home screen opens in standalone mode (no browser chrome).
5. The service worker pre-cached the shell on first visit, so the app launches even with no network.

To verify each step works:

- **Lighthouse PWA audit.** In Chrome DevTools → Lighthouse, run a PWA audit. It checks the manifest, service worker, HTTPS, and other requirements.
- **Application panel in DevTools.** See the service worker state, registered caches, IndexedDB contents, and manifest.
- **Network throttling.** DevTools → Network → "Offline" mode. Refresh the page; your offline fallback should load.
- **Service worker update flow.** DevTools → Application → Service Workers → "Update on reload" while you iterate.
- **Real device install.** Test on an actual phone. The install experience is meaningfully different from desktop.

For automated testing, [Playwright](https://playwright.dev/) supports service worker debugging and offline-mode testing. We use it for the magazine's CI.

## Part 13: Honest Limits And Caveats

The offline story is genuinely good in 2026, but it has rough edges.

**1. Service workers are subtle.** The lifecycle (install / waiting / activate) trips up new developers. A change to the SW that the cache deduplicates differently than expected can leave users on stale code. The "Update on reload" devtools setting is essential during development.

**2. Background Sync is Chromium-only.** Already discussed. For cross-browser offline-submit, also implement a `visibilitychange`-driven retry.

**3. iOS PWA installation is awkward.** Safari does not show "Install" in the address bar; the user has to use the share menu and tap "Add to Home Screen." Document this on your site.

**4. Storage quotas are unpredictable.** A user with a near-full disk gets less; a user with persistent storage gets more. Always handle `QuotaExceededError`.

**5. Service workers can be "lost."** If the user clears site data (deliberately or via a browser data-clearing tool), the SW is gone. The next page load reinstalls it. Build for this.

**6. Origin restrictions are real.** A SW at `/sw.js` cannot control pages on a subdomain or different path. Plan your scope from the start.

**7. Updates can break in-flight pages.** A user opens a page, then your service worker updates with a breaking change to the API, then the user does something on the old page. They get errors. Use the `Service-Worker-Allowed` header and version your APIs to mitigate.

## Part 14: When You Want A Library

Two libraries earn their keep in this space:

1. **[Workbox](https://developer.chrome.com/docs/workbox)** — Google's service worker library. Pre-built strategies, route definitions, helpers for precaching, sophisticated handling of edge cases. Worth using if your service worker grows past ~300 lines or you have multiple developers contributing to it. About 15–30KB.

2. **[idb](https://github.com/jakearchibald/idb)** — Jake Archibald's tiny (2KB) IndexedDB wrapper that gives you a Promise-based API over the verbose native one. Worth it for any non-trivial IDB use. Available on esm.sh.

For our magazine, we chose to write the SW from scratch (the version above) and used `idb` for IndexedDB. That gives us full understanding of the SW behaviour and convenience for the IDB part. About 250 lines of our own SW + 2KB of `idb` covers everything.

## Part 15: Tomorrow

Tomorrow — **Day 14: Accessibility, Performance, and Security** — we cover the three concerns that determine whether your site is *good* or just *technically functional*. WCAG 2.2 AA conformance, focus management, screen-reader testing. Core Web Vitals, `content-visibility`, lazy-loading patterns, the performance budget framework. CSP, Trusted Types, Subresource Integrity, the security headers that prevent the common attacks. We will run our magazine through Lighthouse and see how it scores. Spoiler: very well, because we have been building for these concerns from Day 1.

See you tomorrow.

---

## Series navigation

You are reading **Part 13 of 15**.

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
- [Part 11: Client-Side Routing with the Navigation API and View Transitions](/blog/2026-06-03-no-build-web-routing-and-navigation)
- [Part 12: Forms, Validation, and the Constraint Validation API](/blog/2026-06-04-no-build-web-forms-and-validation)
- **Part 13 (today): Storage, Service Workers, and Offline**
- Part 14 (tomorrow): Accessibility, Performance, and Security
- Part 15: The Conclusion — A Complete Application End to End

## Resources

- [MDN: Service Worker API](https://developer.mozilla.org/en-US/docs/Web/API/Service_Worker_API) — the canonical reference.
- [MDN: Cache API](https://developer.mozilla.org/en-US/docs/Web/API/Cache) — for the cache-handling parts.
- [MDN: IndexedDB](https://developer.mozilla.org/en-US/docs/Web/API/IndexedDB_API) — the database reference.
- [MDN: Origin Private File System](https://developer.mozilla.org/en-US/docs/Web/API/File_System_API/Origin_private_file_system) — OPFS guide.
- [MDN: Web App Manifest](https://developer.mozilla.org/en-US/docs/Web/Progressive_web_apps/Manifest) — every manifest field.
- [MDN: Background Sync API](https://developer.mozilla.org/en-US/docs/Web/API/Background_Synchronization_API) — reference.
- [web.dev: Service workers — an introduction](https://web.dev/articles/service-workers) — Jake Archibald's introduction (still the best).
- [web.dev: Offline cookbook](https://web.dev/articles/offline-cookbook) — the canonical strategy guide.
- [Workbox](https://developer.chrome.com/docs/workbox) — Google's SW library, when you outgrow your handwritten one.
- [idb](https://github.com/jakearchibald/idb) — the Promise-based IndexedDB wrapper.
- [PWA Builder](https://www.pwabuilder.com/) — a tool that audits and packages your PWA.
- [Maximiliano Firtman, *Progressive Web Apps* book](https://firt.dev/books/) — the depth resource on PWAs.
- [Caniuse: Background Sync](https://caniuse.com/background-sync) — current support.
