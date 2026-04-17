---
title: "Without a Net, Part 14: Accessibility, Performance, and Security"
date: 2026-06-06
author: myblazor-team
summary: "Day 14 of our fifteen-part no-build web series covers the three concerns that separate a site that technically works from one that is actually good. We walk through WCAG 2.2 AA conformance — focus management, keyboard navigation, screen-reader testing, the rules nobody enforces. Core Web Vitals — INP, LCP, CLS — explained and budgeted. Performance techniques — content-visibility, lazy-loading, modulepreload, the right image formats. CSP, the brand-new (February 2026) cross-browser Trusted Types, Subresource Integrity, the security headers you should set on every site. We finish by running our magazine through Lighthouse and a screen reader and see how it scores. The honest answer: very well, because we have been building for these concerns from day one."
tags:
  - accessibility
  - a11y
  - wcag
  - performance
  - core-web-vitals
  - security
  - csp
  - trusted-types
  - lighthouse
  - no-build
  - first-principles
  - series
  - deep-dive
---

## Introduction: The Audit That Took Six Months

A few years ago we were brought in to help a healthcare company prepare for a regulatory accessibility audit. The product was a patient portal — appointment booking, prescription refills, lab results. The kind of site where accessibility is not a nice-to-have but a legal requirement under the Americans with Disabilities Act, equivalent legislation in Europe, and the company's own moral commitment to its patients.

The team had spent the previous year doing what they called "the accessibility work." They had hired a contractor who specialised in ARIA. They had added `role` attributes to many components. They had added `aria-label` to most buttons. They had a colour contrast checker built into their CI. Their Lighthouse accessibility score was 92 out of 100. They felt prepared.

The audit took six weeks. The audit report was 240 pages long. It identified 1,847 distinct accessibility issues. The summary called the site "essentially unusable for blind users, severely limited for keyboard-only users, and likely to fail Web Content Accessibility Guidelines conformance at any level." The remediation took six months and cost the company a substantial amount of money. Several launched-but-not-released features were pulled from the roadmap because retrofitting accessibility was more expensive than building a new replacement.

The interesting thing was *what* the audit found. The actual issues were almost never about ARIA. They were about:

- **Keyboard traps** — components the user could `Tab` into but never `Tab` out of.
- **Missing focus indicators** — the team's design system had styled `:focus { outline: none }` years ago and nobody had ever added it back.
- **Headings used for visual styling instead of structure** — the page had eighteen `<h2>` elements and one `<h1>` because the design called for many big bold lines and the developer reached for `<h2>` to get them.
- **Form errors that were invisible to screen readers** — error messages appeared next to fields visually but with no programmatic association.
- **Modals that stole focus and never returned it** — opening a modal moved focus into it; closing it left focus floating in the void.
- **Buttons made out of `<div>` and `<span>`** — visually buttons, semantically not, with custom JavaScript click handlers and no keyboard support.
- **Carousels with auto-advance that could not be paused** — animation that kept moving regardless of `prefers-reduced-motion` or any user control.

The team's "accessibility work" had been adding cosmetic ARIA on top of a foundation that was inaccessible. Every issue in the audit was a thing that would have been *automatic* if the team had used the right HTML element from the start. `<button>` instead of `<div onclick>`. `<dialog>` instead of a custom focus-trap framework. `<h1>` once and `<h2>` for sections. `<input type="email" required>` instead of regex validation in JavaScript.

This is the central observation about modern web accessibility, performance, and security: **the platform handles most of it correctly if you let it.** The work is mostly negative — not adding things, not styling away the focus indicators, not bypassing the platform with custom widgets. Every single one of the 1,847 issues that audit found would have been impossible (or trivially solved) if the team had used semantic HTML, native focus management, the Constraint Validation API, and the `<dialog>` element. Day 14 is our tour of those three concerns — accessibility, performance, security — and how to satisfy each by leaning on the platform rather than fighting it.

This is Part 14 of 15 in our no-build web series. Tomorrow we put it all together in the capstone. Today we cover the cross-cutting concerns that make the difference between a site that passes superficial checks and one that is genuinely good.

## Part 1: Accessibility From The Ground Up

The Web Content Accessibility Guidelines (WCAG) are the international standard. The current version is [WCAG 2.2](https://www.w3.org/TR/WCAG22/), published in October 2023 and adopted into most major regulations (the EU's Web Accessibility Directive, the US Section 508 update, the UK Public Sector Bodies Accessibility Regulations). WCAG 2.2 has three conformance levels: A, AA, AAA. Most legal regimes require AA.

The guidelines are organised under four principles, often abbreviated POUR:

- **Perceivable** — content must be presented in ways users can perceive (text alternatives for images, captions for video, sufficient contrast, resizable text).
- **Operable** — the interface must be usable (keyboard accessible, enough time to read, no seizure-inducing content, predictable navigation).
- **Understandable** — the interface and content must be understandable (readable text, predictable behaviour, error help).
- **Robust** — content must work with current and future assistive technologies (valid markup, name/role/value exposed for custom components).

We will not cover all 50+ success criteria. The high-impact subset, in our experience, is what follows.

### Use the right HTML element

This is 80% of accessibility. A non-exhaustive list of "use this, not that":

| Bad | Good |
|---|---|
| `<div onclick>` | `<button type="button">` |
| `<a href="#" onclick>` | `<button type="button">` |
| `<span class="link">` | `<a href="...">` |
| `<div>` of inputs | `<form>` of inputs |
| `<div>Heading</div>` | `<h1>` / `<h2>` / `<h3>` |
| `<div role="navigation">` | `<nav>` |
| `<div role="main">` | `<main>` |
| `<div>` table | `<table>` with `<th>` |
| Custom dropdown | `<select>` |
| Custom checkbox | `<input type="checkbox">` |
| Custom modal | `<dialog>` |
| Custom tooltip | `popover` attribute (Day 2) |

Each native element brings, for free:

- Correct keyboard handling (`Enter` and `Space` activate buttons; arrow keys navigate radio groups; `Esc` closes dialogs).
- Correct focus management (focusable in the right order, with the right visual indicator).
- Correct screen-reader announcement (the right role and accessible name).
- Correct interaction with assistive tech (voice control, switch control).

When you reimplement these in `<div>`s with JavaScript, you start at zero on every one of those axes. You then have to add `role`, `tabindex`, `aria-*`, and keyboard handlers to claw back the behaviour the platform already had. You will get some of it right. You will not get all of it right. Patrick H. Lauke, who spends his life auditing custom widgets, has a [legendary collection](https://github.com/patrickhlauke/recent-talks) of carousels, modals, and toggles that "look fine" but fail in some screen reader, voice-control mode, switch-control mode, or browser zoom level.

The remediation: **just use the right element.** The cost is one moment of "but I want my buttons to look custom" — entirely solvable with CSS, which works perfectly fine on `<button>`. The benefit is correctness across every assistive technology, in every browser, today and forever.

### Headings as document structure

Headings are not for visual styling. They are how screen-reader users navigate. A blind user pressing `H` in NVDA or VoiceOver jumps through the headings on the page like a sighted user skimming with their eyes. If your headings are arbitrary or out of order, navigation is broken.

The rules:

- **Exactly one `<h1>` per page**, describing the page itself.
- **`<h2>` for top-level sections.**
- **`<h3>` for subsections within `<h2>`s.** Continue the hierarchy.
- **Do not skip levels.** No jumping from `<h2>` to `<h4>`.
- **Headings communicate structure, not size.** If you want a smaller-looking heading, use CSS — keep the semantic level correct.

Use Chrome DevTools' Accessibility tab → "Show document outline" to see your heading structure. If it does not look like an outline of the page, fix it.

### Focus management

The keyboard focus is what `Tab` moves and what activated controls return to. Three rules:

**1. Every interactive element must be focusable.** If the user can interact with it via mouse, they must be able to interact with it via keyboard. Native `<button>`/`<a>`/`<input>` are focusable automatically. Custom widgets need `tabindex="0"`.

**2. Focus must be visible.** The browser draws a focus indicator (usually a blue ring) on the focused element. Do not remove it:

```css
/* Bad — never do this */
*:focus { outline: none; }

/* Good — keep visible focus, optionally style it */
:focus-visible {
  outline: 2px solid var(--brand);
  outline-offset: 2px;
  border-radius: 0.25rem;
}
```

`:focus-visible` is the modern selector that only matches keyboard focus, not mouse-click focus. (Mouse users do not need a visible focus ring while clicking, and keyboard users do.) Use `:focus-visible` for the visual ring; do not remove `:focus`.

**3. After an action, focus must be sensible.** If the user activates a button that opens a modal, focus should move into the modal. When the modal closes, focus should return to the button. If the user submits a form, focus should move to the success/error message. Routes (Day 11) should move focus to the new page's `<h1>` or main content.

### Skip links

A "skip to content" link as the first focusable element on the page lets keyboard users skip past the navigation:

```html
<body>
  <a href="#main" class="skip-link">Skip to main content</a>
  <header><nav>...</nav></header>
  <main id="main">...</main>
</body>
```

```css
.skip-link {
  position: absolute;
  top: -3em;
  left: 0;
  background: var(--surface);
  padding: 0.5em 1em;
  border: 2px solid var(--brand);
  text-decoration: none;
  transition: top 200ms;
}
.skip-link:focus {
  top: 0;
}
```

The link is visually hidden until focused, then slides into view. First `Tab` reveals it; `Enter` jumps to main content. WCAG 2.4.1.

### Accessible names

Every interactive element needs an "accessible name" — what a screen reader announces when the user lands on it. Most elements get this from their content (`<button>Submit</button>`) or from associated labels (`<label for="email">Email</label>`).

When the visible content is not enough — icon-only buttons, ambiguous links — use `aria-label`:

```html
<button aria-label="Close dialog">×</button>
<a href="..." aria-label="Read more about Bilal's article">Read more</a>
```

Or `aria-labelledby` to point at another element's text:

```html
<h2 id="card-1-title">My article</h2>
<a href="..." aria-labelledby="card-1-title">Read</a>
```

A common bug: multiple "Read more" links on a page, all with the same accessible name. Screen-reader users hear "Read more, Read more, Read more, Read more" with no idea which is which. Fix by `aria-labelledby` to the article title, or by adding visually-hidden text:

```html
<a href="...">Read more <span class="sr-only">about Bilal's article</span></a>
```

```css
.sr-only {
  position: absolute;
  width: 1px;
  height: 1px;
  padding: 0;
  margin: -1px;
  overflow: hidden;
  clip: rect(0,0,0,0);
  white-space: nowrap;
  border: 0;
}
```

The `.sr-only` class is the standard "visually hide but expose to screen readers" pattern.

### Live regions

When content updates dynamically — a notification appears, an error message changes, a status updates — screen readers do not automatically announce it. Use `aria-live`:

```html
<div role="status" aria-live="polite" id="status"></div>
<div role="alert" aria-live="assertive" id="errors"></div>
```

`aria-live="polite"` announces when the user pauses. `aria-live="assertive"` interrupts whatever the user is doing — reserve for genuine emergencies.

Setting `textContent` on these elements triggers the announcement. We used this pattern in [Day 11](/blog/2026-06-03-no-build-web-routing-and-navigation) for route changes and in [Day 12](/blog/2026-06-04-no-build-web-forms-and-validation) for async-validation status.

### Reduced motion

The `prefers-reduced-motion` media query (Day 5) lets users say "I am sensitive to motion." Honour it:

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

For users with vestibular disorders, large parallax effects can cause nausea and dizziness. WCAG 2.3.3 (level AAA, but increasingly enforced) requires offering a way to disable non-essential motion. The media query is the lightest-touch way.

### Colour contrast

WCAG 2.2 requires:

- **4.5:1** for body text (level AA).
- **3:1** for large text (18pt regular or 14pt bold).
- **3:1** for non-text contrast — focus indicators, icons, important borders.

Tools to check:

- **Chrome DevTools** colour picker shows the contrast ratio against the resolved background.
- **WebAIM Contrast Checker** for arbitrary colour pairs.
- **Lighthouse** flags failing contrast in its audits.

For a design system with `oklch()` tokens (Day 6), check contrast at design time — every `--text` against every `--background` should pass. Use [`contrast-color()`](https://developer.mozilla.org/en-US/docs/Web/CSS/Reference/Values/color_value/contrast-color) (Day 6) where possible.

### Testing — do not rely on automated tools

Lighthouse, axe, WAVE — all good. But [automated tools detect about 30% of WCAG issues](https://www.deque.com/blog/automated-testing-obtains-30-of-wcag/). The other 70% require human review.

The minimum-viable accessibility test loop:

1. **Tab through every page.** Ensure focus is visible, order is logical, every interactive element is reachable, no keyboard traps.
2. **Test with a screen reader on a key flow.** macOS VoiceOver (`Cmd-F5`) is built in and free. NVDA on Windows is free. iOS VoiceOver and Android TalkBack take learning but are essential for testing mobile.
3. **Test at 200% zoom.** Many users zoom; layouts often break.
4. **Test in dark mode.** A surprising number of contrast bugs only appear in one mode.
5. **Run Lighthouse / axe.** Catches the automatable subset.

A team that does these five things on every PR will have a site that passes a real audit.

## Part 2: Core Web Vitals — The Performance Bar

Google's Core Web Vitals are the metrics search ranking actually cares about, and which approximate user-perceived performance well. The three primary metrics in 2026:

- **LCP (Largest Contentful Paint)** — when the largest visible element is rendered. Goal: **under 2.5 seconds**.
- **INP (Interaction to Next Paint)** — the worst delay between user input and the next paint. Goal: **under 200 ms**. Replaced FID in March 2024.
- **CLS (Cumulative Layout Shift)** — how much things move around after initial render. Goal: **under 0.1**.

These are 75th-percentile measurements across real users, not lab numbers. A site that "passes" Web Vitals must satisfy all three goals for at least 75% of page loads.

### LCP — render fast

The Largest Contentful Paint element is usually a hero image or a large heading. Optimisations:

**1. Send the right HTML fast.** Server response time matters. For static sites on a CDN (which is us), this is usually fine. For server-rendered pages, time to first byte (TTFB) is what to watch.

**2. Inline critical CSS.** Above-the-fold styles should be in a `<style>` block in `<head>`. The browser does not have to wait for an external stylesheet to render the LCP element.

**3. Preload the LCP image.** If the LCP is a hero image, hint the browser:

```html
<link rel="preload" as="image" href="/hero.webp" fetchpriority="high">
```

`fetchpriority="high"` tells the browser this is the most important resource on the page. Equivalent to "prioritise this above other downloads."

**4. Use modern image formats.** AVIF is typically 30–50% smaller than JPEG. WebP is typically 25–35% smaller. Use the `<picture>` element to serve the best the browser supports:

```html
<picture>
  <source type="image/avif" srcset="/hero.avif">
  <source type="image/webp" srcset="/hero.webp">
  <img src="/hero.jpg" alt="..." width="1200" height="630">
</picture>
```

Always include `width` and `height` attributes on images. They prevent layout shift (CLS) and let the browser reserve space before the image loads.

**5. Use `srcset` for responsive images.**

```html
<img src="/hero-800.jpg"
     srcset="/hero-400.jpg 400w, /hero-800.jpg 800w, /hero-1600.jpg 1600w"
     sizes="(max-width: 600px) 400px, (max-width: 1200px) 800px, 1600px"
     alt="..." width="1600" height="900">
```

The browser picks the right-sized image for the user's viewport. Mobile users do not download the 1600px hero.

**6. Self-host fonts.** External font CDNs add a DNS lookup and cross-origin fetch. Self-hosted variable fonts (Day 6) under your own domain are faster.

### INP — keep the main thread responsive

INP measures the time from the user's input (click, tap, keypress) to the next paint that reflects the interaction. The blocker is usually JavaScript holding the main thread.

**1. Break long tasks.** A task longer than 50ms blocks input. If you have a 200ms task, split it into four 50ms tasks with `await` or `setTimeout(fn, 0)` in between to let the browser process input.

**2. Use `requestIdleCallback` for background work** (Day 8). Things that do not need to happen immediately should happen when the browser is idle.

**3. Move heavy work to a Web Worker.** CPU-heavy operations (image processing, large data parsing, regex on big strings) should not run on the main thread. Web Workers are the platform's answer.

**4. Use `content-visibility: auto` for off-screen content.** Tells the browser "this content is offscreen; do not waste time rendering it":

```css
.article {
  content-visibility: auto;
  contain-intrinsic-size: 0 500px;
}
```

The browser skips layout, paint, and style for offscreen articles, dramatically improving initial render and scroll performance for long pages. `contain-intrinsic-size` reserves space so scrollbars do not jump as content renders. [Baseline since 2024](https://caniuse.com/css-content-visibility).

**5. Defer non-essential JavaScript.** Module scripts are deferred by default (Day 7). For external scripts (analytics, etc.), use `defer` or `async`.

### CLS — don't shift content after render

Layout shift happens when content appears or resizes after the initial render, pushing other content around. Common causes:

**1. Images without dimensions.** Always set `width` and `height` on `<img>`. The browser uses them to compute aspect ratio and reserve space.

**2. Web fonts swapping in.** When the fallback font is a different size from the web font, the layout shifts when the font loads. Mitigate with `font-display: optional` (skip the swap on slow connections) or with `size-adjust` / `ascent-override` in `@font-face` to match the fallback's metrics.

**3. Ads or embeds with no reserved space.** Reserve space with CSS:

```css
.ad-slot {
  min-height: 250px;
  background: oklch(95% 0 0);
}
```

**4. Inserting content above existing content.** Banners, cookie notices, "back online" messages — anything inserted at the top of the page shifts everything down. Position them as overlays (`position: fixed`) or reserve space.

**5. Forms that grow on validation.** A form that adds error messages shifts subsequent content. Reserve the message space:

```css
.error-message {
  min-height: 1.5em;   /* always takes one line of space */
  display: block;
}
```

### Measuring Web Vitals in production

Lab measurements (Lighthouse) tell you the *capacity* of your page. Field measurements (real users) tell you what is *actually happening*. The [`web-vitals` library](https://github.com/GoogleChrome/web-vitals) is the canonical way to measure in the field:

```html
<script type="module">
  import { onCLS, onFID, onLCP, onINP } from "https://esm.sh/web-vitals@4";

  function send(metric) {
    navigator.sendBeacon("/analytics", JSON.stringify(metric));
  }

  onCLS(send);
  onLCP(send);
  onINP(send);
</script>
```

`navigator.sendBeacon` is a small platform API designed for analytics — it sends data even if the page is unloading. Send the measurements to your analytics endpoint; aggregate; watch the 75th percentile.

For hosted Web Vitals reporting without rolling your own, [Google's Chrome User Experience Report (CrUX)](https://developer.chrome.com/docs/crux) aggregates real-user data for sites with sufficient traffic. Free.

## Part 3: Performance Patterns — A Working Checklist

A pragmatic checklist for performance, in approximate priority order:

1. **Compress responses.** Brotli or zstd compression on every text response. CDNs do this; static hosts usually do too.
2. **Cache aggressively.** Versioned URLs with `Cache-Control: public, max-age=31536000, immutable`. Service worker for offline (Day 13).
3. **Preconnect to important origins.** `<link rel="preconnect" href="https://api.example.com">` for cross-origin requests you know you'll make.
4. **Preload the LCP element.** Explicit `<link rel="preload">` for the hero image.
5. **Use `modulepreload` for critical JS.** From Day 7.
6. **Defer non-critical CSS.** Critical inlined; rest with `<link rel="stylesheet" media="print" onload="this.media='all'">`.
7. **Lazy-load images below the fold.** `<img loading="lazy">` (Day 2).
8. **Use the right image format.** AVIF, WebP, with JPEG fallback.
9. **Self-host fonts; use variable fonts.** Day 6.
10. **`content-visibility: auto` for long lists.**
11. **Code-split routes.** Dynamic `import()` (Day 7) so each page only loads its own code.
12. **No blocking third-party scripts.** Analytics, tag managers, chat widgets — all `defer` or `async`, ideally loaded after first interaction.
13. **Minify and tree-shake** if you have a bundler. Optional minification step from Day 7 if you do not.
14. **Use HTTP/2 or HTTP/3.** Standard on every major host today.
15. **Measure in the field.** Use real-user monitoring; do not just trust Lighthouse.

This is a checklist, not a religion. A small site does not need to do all of these. A big site needs to do most of them.

## Part 4: Security — The Quiet Concerns

Web security has a long tail of attacks, but two patterns cover the majority:

- **Cross-Site Scripting (XSS)** — attacker injects code that runs in another user's browser, stealing data or impersonating them.
- **Cross-Site Request Forgery (CSRF)** — attacker tricks a logged-in user's browser into making a request the user did not intend.

The platform has good mitigations for both, plus more for the rest. We will cover the headers and APIs that matter.

### Content Security Policy (CSP)

A CSP header tells the browser what resources are allowed to load and execute. A strong CSP is the single most effective XSS mitigation available.

A starter CSP for our magazine:

```
Content-Security-Policy:
  default-src 'self';
  script-src 'self' https://esm.sh;
  style-src 'self' 'unsafe-inline';
  img-src 'self' data: https:;
  font-src 'self';
  connect-src 'self';
  frame-ancestors 'none';
  base-uri 'self';
  form-action 'self';
```

Going through it:

- **`default-src 'self'`** — by default, everything must come from our own origin.
- **`script-src 'self' https://esm.sh`** — scripts from us, or from esm.sh (our import-map CDN).
- **`style-src 'self' 'unsafe-inline'`** — stylesheets from us; allow inline `<style>` (often necessary for critical CSS; remove if you can).
- **`img-src 'self' data: https:`** — images from us, plus data URIs (small inline images), plus any HTTPS source (loose for content that includes external images).
- **`font-src 'self'`** — fonts only from us.
- **`connect-src 'self'`** — fetch/XHR/WebSocket only to our origin.
- **`frame-ancestors 'none'`** — nobody can put us in an `<iframe>` (clickjacking defence).
- **`base-uri 'self'`** — `<base>` tag locked to our origin.
- **`form-action 'self'`** — forms can only submit to us.

For GitHub Pages, you cannot set headers via configuration — but you can use `<meta http-equiv="Content-Security-Policy" content="...">` in your HTML. The meta tag has slightly less power (cannot set `frame-ancestors`, `report-uri`, `sandbox`) but covers the most important directives.

### Avoid `'unsafe-inline'` for scripts

The biggest CSP win is **never allowing `'unsafe-inline'` in `script-src`.** That single restriction blocks 90% of XSS. The flip side: every script you load must be in an external file or use a nonce / hash.

For a no-build site, this means structuring your HTML with `<script type="module" src="..."></script>` rather than inline scripts. The bootstrap script we showed in Day 6 (the inline script that sets `data-theme` to avoid flash-of-wrong-theme) needs a nonce or hash to comply with strict CSP:

```html
<script nonce="abc123">
  document.documentElement.dataset.theme = localStorage.getItem("theme") ?? "auto";
</script>
```

```
Content-Security-Policy: script-src 'nonce-abc123' 'self';
```

The nonce is server-generated per request. For static GitHub Pages, you cannot generate per-request nonces. Use the SHA-256 hash of the script body instead:

```
Content-Security-Policy: script-src 'sha256-abc...=' 'self';
```

The browser computes the hash of every inline script and only runs it if the hash matches an allowed one.

### Trusted Types — the new standard for XSS prevention

The Trusted Types API ([Baseline Newly Available since February 2026](https://developer.mozilla.org/en-US/docs/Web/API/Trusted_Types_API)) makes XSS-prone sinks (`element.innerHTML = ...`, `eval`, `script.src = ...`) require *typed* values rather than raw strings. With Trusted Types enforced via CSP, the browser refuses to set `innerHTML` to a string — only to a `TrustedHTML` object that has explicitly passed through a sanitiser.

Enabling it:

```
Content-Security-Policy:
  require-trusted-types-for 'script';
  trusted-types default;
```

Then in your code:

```javascript
trustedTypes.createPolicy("default", {
  createHTML(input) {
    // Sanitise. Use the HTML Sanitizer API (Day 8) or DOMPurify.
    return new Sanitizer().sanitizeFor("template", input).innerHTML;
  },
});
```

Now `element.innerHTML = "<script>alert(1)</script>"` works as before because the default policy intercepts and sanitises. But `element.innerHTML = userInput` where `userInput` was malicious is now safely sanitised before reaching the DOM.

The major news: **Trusted Types just became Baseline Newly Available in February 2026** when Firefox 148 shipped support and Safari 26.1 followed. Previously Chromium-only since 2020. For new projects in 2026, Trusted Types is the right way to harden against DOM-based XSS.

### Subresource Integrity (SRI)

When you load scripts from a third party (like esm.sh), an attacker who compromises the third party can serve malicious code to your users. SRI prevents this by asserting the file's hash:

```html
<script type="module"
        src="https://esm.sh/preact@10.26.0"
        integrity="sha384-...">
</script>
```

If the file at the URL has a different hash, the browser refuses to execute it.

For import maps, SRI is in the map:

```html
<script type="importmap">
{
  "imports": {
    "preact": "https://esm.sh/preact@10.26.0"
  },
  "integrity": {
    "https://esm.sh/preact@10.26.0": "sha384-..."
  }
}
</script>
```

For a serious site, **pin versions and add SRI** to every external dependency.

### Other security headers

Set these on every site:

```
X-Content-Type-Options: nosniff
Strict-Transport-Security: max-age=31536000; includeSubDomains
Referrer-Policy: strict-origin-when-cross-origin
Permissions-Policy: camera=(), microphone=(), geolocation=(), interest-cohort=()
Cross-Origin-Opener-Policy: same-origin
Cross-Origin-Embedder-Policy: require-corp
```

What each does:

- **`X-Content-Type-Options: nosniff`** — disable MIME-type sniffing. Stops files from being interpreted as a different type than declared.
- **`Strict-Transport-Security`** — instruct the browser to always use HTTPS for this domain. Once set, the user cannot accidentally use HTTP.
- **`Referrer-Policy`** — control what `Referer` header is sent. `strict-origin-when-cross-origin` is the modern default.
- **`Permissions-Policy`** — disable powerful APIs on your site. Disabling `interest-cohort=()` opts out of FLoC / Topics tracking.
- **`Cross-Origin-Opener-Policy: same-origin`** — windows you open are isolated. Required for `SharedArrayBuffer`.
- **`Cross-Origin-Embedder-Policy: require-corp`** — same.

GitHub Pages serves these headers but you cannot fully customise. For full header control, use Cloudflare Pages (free), Netlify, or any host with a `_headers` file.

### CSRF — `SameSite` cookies

CSRF is mostly solved by `SameSite=Lax` cookies, which is the default in modern browsers since 2020. As long as you set authentication cookies without `SameSite=None`, you are mostly safe. For state-changing endpoints, also accept only `POST` with a JSON body that requires JavaScript to construct — browsers will not let cross-origin pages do this without CORS approval.

## Part 5: Running Our Magazine Through Lighthouse

Let us actually run the magazine through an audit. The expected scores, given everything we have built across the series:

- **Performance: 95-100.** No render-blocking JavaScript (modules deferred by default), no oversized images (AVIF/WebP with `<picture>`), no layout shift (dimensions on every image), no bloated bundle (no bundler at all).
- **Accessibility: 95-100.** Semantic HTML throughout. `<dialog>`, `<details>`, `<form>` with proper labels. Headings in order. Skip link. Focus management on route change. Live regions for announcements.
- **Best Practices: 95-100.** HTTPS only. CSP set. SRI on third-party scripts. No console errors. Image aspect ratios match.
- **SEO: 100.** Static HTML for every URL (SSG). Title and description on every page. Semantic markup throughout. No render-blocking JavaScript means search engine crawlers see the full content.
- **PWA: 100.** Manifest, service worker, offline fallback (Day 13).

This is not because we are clever. It is because we used the platform from the start. Each chapter of this series — semantic HTML in Day 2, performance-conscious CSS in Day 4, native modules in Day 7, reactive state in Day 9, Web Components in Day 10, the Navigation API in Day 11, native form validation in Day 12, the service worker in Day 13 — was solving a small piece of the audit before the audit was even considered.

The team in our opening story spent six months remediating their site after the audit. We are arguing that you can spend the same effort *during* development and have a site that passes the audit when first submitted. Building for accessibility, performance, and security from day one is not slower than building without them; it is faster, because you do not have to retrofit.

## Part 6: The Honest Caveats

We have made some strong claims. A few honest qualifications.

**1. Automated audits do not equal accessibility.** A 100 Lighthouse score does not mean a blind user can use your site. Manual testing with a screen reader is non-negotiable.

**2. Real-user performance is what matters.** A site that looks fast in Lighthouse but loads in 8 seconds for users on a 2018 Android phone with 3G is not actually fast. Use field measurements.

**3. Security is a moving target.** A CSP that blocks every known XSS today might miss tomorrow's attack vector. Subscribe to security advisories. Keep dependencies pinned and patch them promptly.

**4. The platform is not perfect.** Some accessibility, performance, or security needs require external tooling. We have called these out throughout the series — DOMPurify, Workbox, fuzzy search libraries — when they are right. Use them deliberately, not by default.

**5. Browser bugs exist.** Even native widgets sometimes have subtle accessibility bugs. We file them; we work around them; the platform is, in our long experience, much more reliable than custom widgets.

## Part 7: Tomorrow

Tomorrow — **Day 15: The Conclusion — A Complete Application End to End** — is the capstone. We assemble everything from the previous fourteen days into a complete, working blog reader application. Markdown rendering, search, tag filtering, dark mode, View Transitions, keyboard navigation, WCAG 2.2 AA conformance, offline PWA, RSS feed, comment composer. The whole thing is about 1,500 lines of plain HTML, CSS, and JavaScript, deployable to GitHub Pages with a `git push`. We will look at the file structure, walk through the deployment pipeline, run the audits, and reflect on what fifteen days of "use the platform" has actually given us.

See you tomorrow for the conclusion.

---

## Series navigation

You are reading **Part 14 of 15**.

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
- [Part 13: Storage, Service Workers, and Offline](/blog/2026-06-05-no-build-web-storage-and-offline)
- **Part 14 (today): Accessibility, Performance, and Security**
- Part 15 (tomorrow): The Conclusion — A Complete Application End to End

## Resources

- [Web Content Accessibility Guidelines 2.2](https://www.w3.org/TR/WCAG22/) — the standard.
- [W3C ARIA Authoring Practices Guide](https://www.w3.org/WAI/ARIA/apg/) — patterns for common widgets.
- [WebAIM: Screen Reader User Survey](https://webaim.org/projects/screenreadersurvey10/) — what real users use.
- [The A11y Project Checklist](https://www.a11yproject.com/checklist/) — practical accessibility checklist.
- [web.dev: Learn Accessibility course](https://web.dev/learn/accessibility) — the full free course.
- [web.dev: Core Web Vitals](https://web.dev/articles/vitals) — the metrics.
- [Web Vitals library](https://github.com/GoogleChrome/web-vitals) — measurement.
- [Chrome User Experience Report](https://developer.chrome.com/docs/crux) — field data.
- [MDN: Content Security Policy](https://developer.mozilla.org/en-US/docs/Web/HTTP/Reference/CSP) — CSP reference.
- [MDN: Trusted Types API](https://developer.mozilla.org/en-US/docs/Web/API/Trusted_Types_API) — the new XSS prevention.
- [OWASP Top 10](https://owasp.org/www-project-top-ten/) — the canonical web security threat list.
- [Mozilla Observatory](https://observatory.mozilla.org/) — security headers audit tool.
- [Securityheaders.com](https://securityheaders.com/) — alternative security headers audit.
- [Patrick H. Lauke, *Custom Widgets: A Survey*](https://github.com/patrickhlauke/recent-talks) — the canonical "you didn't get it right" reference.
