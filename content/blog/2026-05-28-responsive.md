---
title: "Without a Net, Part 5: Responsive Design in 2026 — Container Queries, clamp(), Fluid Type, and :has()"
date: 2026-05-28
author: myblazor-team
summary: "Day 5 of our fifteen-part no-build web series takes a hard look at responsive design as a discipline. We build a mental model for intrinsic design, replace most media queries with container queries and fluid values, reach into clamp() and the typography scale, rebuild a responsive navbar without a single breakpoint, use :has() to react to DOM state without JavaScript, and discuss the accessibility settings — reduced motion, reduced transparency, data saver — that turn a responsive site into a genuinely adaptable one. If you still think 'responsive' means 'mobile breakpoint at 768px,' this article is for you."
tags:
  - css
  - responsive-design
  - container-queries
  - clamp
  - fluid-typography
  - has-selector
  - accessibility
  - media-queries
  - web-standards
  - no-build
  - first-principles
  - series
  - deep-dive
---

## Introduction: The Tablet That Did Not Exist

On the desk beside us as we write this is a notebook from 2013. It is a tattered thing, coffee-stained, and on page 47 somebody — possibly us, though we would prefer to deny it — has drawn three rectangles labelled "mobile", "tablet", and "desktop" with arrows pointing to the breakpoints `320px`, `768px`, and `1024px`. Those numbers were not pulled out of thin air. They were, at the time, the approximate screen widths of the iPhone 5, the original iPad, and a standard desktop monitor. Every responsive website built between about 2011 and 2016 used some variation of those three numbers as the structural skeleton of its design.

There is exactly one problem with that skeleton: **there is no such thing as a "tablet device."** There never was. There is a spectrum of screens, starting at watches and phones around 200 pixels wide, going through foldables and tablets in the 500-to-1200 range, through the strange no-man's-land of 13-inch MacBooks with Retina displays reporting 1440 logical pixels, up into 4K and 8K monitors above 2500 logical pixels. Inside a browser window on any of those, users resize to half-width, quarter-width, pop-out side panels, vertical splits. A "tablet breakpoint" at 768 pixels misses every configuration that is slightly above or slightly below, and when you chain three of those breakpoints together, you are asserting that every device on Earth falls into one of three buckets, each with its own layout, with cliffs between them. That is a design theory that is, to put it diplomatically, unmoored from reality.

The uncomfortable truth, which has been true for at least ten years and which most of us have still not internalised, is: **the viewport is a continuous variable.** Treating it as discrete buckets is a human convenience, not a technical necessity. The designer's job is to produce a layout that works at every width, not at three arbitrary widths with jank in between. The developer's job is to implement it the same way — continuously, not in buckets.

In 2026 we have the tools to do this. Container queries removed the dependency on viewport size. `clamp()` and fluid sizing removed the need for per-breakpoint adjustments. The `:has()` selector lets CSS respond to DOM state rather than just viewport size. `prefers-*` media features let us respond to user preferences instead of just measurements. The whole toolkit for **intrinsic responsive design** — design that adapts because the layout is described declaratively, not because we inspected a width — is finally in every browser.

Day 5 is our tour of that toolkit. We will cover, in order: a brief history of "responsive" to level-set, intrinsic design as a philosophy, fluid values with `clamp()` and `min()` / `max()`, the `:has()` parent selector, container queries as the default responsive tool, user-preference queries (`prefers-color-scheme` and friends), building a responsive navbar with zero media queries, the capsule case for CSS logical properties, and, finally, a sanity check on when media queries are still the right tool. The capstone app we are building across this series will inherit everything from this article.

## Part 1: A Short History Of "Responsive"

If you have been building websites for more than ten years you already know this history. If you have been writing ASP.NET for ten years and only occasionally touching the front end, this is the brief you will find useful.

**2004–2009: fixed-width layouts.** Websites were 960 pixels wide, centred on screen. If you were on a 1024-pixel monitor they fit. If you were on a 1920-pixel monitor they fit with large left and right margins. If you were on a phone, you pinched and zoomed.

**2010: Ethan Marcotte coins "Responsive Web Design"** in [a short article in *A List Apart*](https://alistapart.com/article/responsive-web-design/) that outlines three techniques — fluid grids, flexible images, and media queries — and argues you should use them together to make a single layout work at any size. This was a paradigm shift. The entire industry rewrote itself over the next five years around these ideas.

**2010–2015: media queries everywhere.** The mainstream approach became "mobile first" — style everything for phones, then use `@media (min-width: 768px)` to progressively enhance for bigger screens. Frameworks (Bootstrap, Foundation, later Tailwind) codified breakpoints, and a generation of developers learned responsive design as "write rules inside media queries."

**2015–2020: Flexbox and Grid arrive.** CSS finally got real layout primitives. Responsive design became much easier, but the mental model remained "media queries."

**2022: container queries ship.** The [CSS Containment Level 3](https://www.w3.org/TR/css-contain-3/) spec, which had been under development for about a decade, finally shipped in all browsers. Components could now respond to their *container's* size rather than the *viewport's* size. This was the moment the media-query-centric approach became obsolete, although most of us did not notice for another two years.

**2023–2024: container queries went Widely Available.** The tooling caught up. Browser support hit near-universal. `:has()`, the parent selector, became Baseline.

**2025: `@scope`, `@container`, and `@layer` make component-level CSS feel like a first-class citizen.** Tools like Tailwind grow container-query plugins. The industry starts to shift.

**2026: intrinsic responsive design becomes the default.** Viewport media queries are still useful — for dark mode, reduced motion, print — but for *layout*, the default tool is now container queries and fluid sizing.

That is where we are. The concept of "responsive" has not changed — it still means "adapt to context" — but the tools are dramatically better. Day 5 is about using those tools well.

## Part 2: Intrinsic Design — A Philosophy

"Intrinsic design" is Jen Simmons's term, coined around 2018, for an approach to CSS layout that emphasises letting elements size themselves based on their content and available space, rather than being assigned explicit dimensions. The core insight:

> **Most of the time, you should not be telling the browser how wide or tall something should be. You should be telling it what it is, and letting it figure out the size.**

If you have been writing `width: 33.333%` in a media query block, you are working against the browser. The browser has sophisticated layout engines specifically designed to handle "three items in a row" better than you can. Your job is to describe the intent. The browser's job is to execute it.

Intrinsic design leans heavily on:

- **`min-content`, `max-content`, `fit-content`** — content-driven sizing keywords.
- **`auto`** in grid templates — "size to the content."
- **`minmax(min, max)`** — a range, not a fixed value.
- **`clamp(min, preferred, max)`** — a fluid value that is bounded.
- **`flex: 1 1 Xpx`** — "prefer this size, grow and shrink as needed."
- **`repeat(auto-fill, minmax(Xpx, 1fr))`** — "as many columns as fit, each at least X wide."

When your CSS is full of these constructs and nearly empty of `@media` queries, you have arrived at intrinsic design. The layout is not tuned for three fake devices; it is continuous across the spectrum.

Two rules of thumb from this philosophy. They will not always be right, but they are right more often than the old rules.

**Rule 1: prefer ranges over fixed values.** Instead of `width: 300px`, write `width: clamp(250px, 30%, 400px)`. Instead of `font-size: 24px` in a media query, write `font-size: clamp(1.25rem, 2vw, 2rem)`.

**Rule 2: prefer intrinsic sizing over explicit sizing.** Instead of `grid-template-columns: 1fr 1fr 1fr`, write `grid-template-columns: repeat(auto-fill, minmax(15rem, 1fr))`. The second adapts; the first does not.

## Part 3: `clamp()`, `min()`, `max()` — The Math Functions You Must Know

`min()`, `max()`, and `clamp()` are three of the most useful additions to CSS in the last decade. All three have been Baseline Widely Available since 2020. They accept values in any combination of units — pixels, rems, percentages, viewport units, container units, `calc()` expressions — and compute a result at runtime.

### `min()`

Returns the smaller of its arguments:

```css
.page {
  max-width: min(80rem, 100%);
}
```

The page is at most 80rem wide OR 100% of its container, whichever is *smaller*. On a wide screen, this caps at 80rem (roughly 1280 pixels). On a narrow screen, it fills the container — no horizontal scroll. Three-line responsive behaviour in one declaration.

### `max()`

Returns the larger. Useful for minimum sizing that respects user preferences:

```css
.hero {
  padding: max(2rem, 5vw);
}
```

The hero gets at least 2rem of padding, or 5% of the viewport width, whichever is larger. Small viewports get compact padding; large viewports get generous padding; there is no media query and no breakpoint.

### `clamp()`

The most useful of the three. Three arguments: a minimum, a preferred value, and a maximum:

```css
h1 {
  font-size: clamp(1.5rem, 1rem + 2vw, 3rem);
}
```

The heading is never smaller than 1.5rem, never larger than 3rem, and in between it scales with the viewport at a rate of `1rem + 2vw`. This is **fluid typography**: the type size adjusts continuously with the screen, clamped to sensible bounds.

The preferred value can be any expression — `calc()`, `var()`, anything that produces a length. The common pattern for fluid type is:

```css
font-size: clamp(MIN, BASE + RATE * 1vw, MAX);
```

where `BASE` and `RATE` together define the slope: at what rate does the size grow with the viewport?

### Computing fluid values — the short version

You do not need to do the math by hand. [Utopia.fyi](https://utopia.fyi/) is a free, open-source calculator that produces a full fluid type scale from two numbers — a minimum viewport width and its corresponding base size, and a maximum viewport width and its corresponding base size. Paste the output into your tokens layer. Done. No Sass. No build step.

For those curious about the math, the formula is a linear interpolation:

```text
rate_vw = (max_size - min_size) / (max_viewport - min_viewport) * 100
base_rem = min_size - rate_vw / 100 * min_viewport
```

If you want a font that is 1rem at 20rem viewport and 1.5rem at 80rem viewport:

- `rate_vw = (1.5 - 1) / (80 - 20) * 100 = 0.833`
- `base_rem = 1 - 0.833 / 100 * 20 = 0.833`

So: `font-size: clamp(1rem, 0.833rem + 0.833vw, 1.5rem);`

Most of the time you let Utopia do this and you move on.

## Part 4: Container Queries — The Default Responsive Tool

We introduced container queries in Day 4. In Day 5 we use them properly. Here is the mental shift that takes a while:

**Stop asking "how big is the window?" Start asking "how big is this component?"**

A card in a wide main column should look different than the same card in a narrow sidebar. With media queries, both cards see the same viewport, and there is no clean way to style them differently. With container queries, each card sees its own container, and both can style themselves correctly with the same CSS.

### The shape of a container query

```css
.card-host {
  container-type: inline-size;
  container-name: card-host;
}

@container card-host (min-width: 30rem) {
  .card {
    display: grid;
    grid-template-columns: 8rem 1fr;
    gap: 1rem;
  }
}
```

- `.card-host` becomes a "query container" on its inline axis. Its width can be queried.
- Its `container-name` is `card-host` — optional, but useful when you have multiple containers and want to target a specific one.
- The `@container` rule fires when that specific container is at least 30rem wide. It matches `.card` elements *inside* the container.

A container query only sees the container, not the viewport. This is the whole point. Two cards, one in a wide main column, one in a narrow sidebar: one lays out horizontally, the other vertically, same CSS, no JavaScript, no media query.

### Container query anonymous form

If you do not name the container:

```css
.card-host { container-type: inline-size; }

@container (min-width: 30rem) {
  .card { ... }
}
```

The query matches the *nearest* container. This works fine for most cases, but when you have nested containers you need named queries to disambiguate.

### Common container query patterns

**Card that stacks below a threshold:**

```css
.card-host { container-type: inline-size; }

.card {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

@container (min-width: 30rem) {
  .card {
    flex-direction: row;
    align-items: center;
  }
}
```

Vertical below 30rem container width; horizontal above. Drop the card into a wide column: horizontal. Drop it into a narrow sidebar: vertical.

**Grid that densifies as it grows:**

```css
.grid-host { container-type: inline-size; }

.grid {
  display: grid;
  grid-template-columns: 1fr;
  gap: 1rem;
}

@container (min-width: 30rem) {
  .grid { grid-template-columns: repeat(2, 1fr); }
}

@container (min-width: 60rem) {
  .grid { grid-template-columns: repeat(3, 1fr); }
}
```

One column in narrow containers, two at medium, three at wide. Note that this is exactly the behaviour you would write with media queries; the difference is it responds to the container, not the viewport. The grid works correctly whether it is the page's main content area or embedded in a sidebar widget.

**Honest note: the above pattern can often be replaced by intrinsic sizing without any container query at all.** `grid-template-columns: repeat(auto-fill, minmax(15rem, 1fr))` adapts to any width, without the container query, and arguably better. Reach for container queries when you need to change the layout model (flex to grid, row to column, etc.), not just the column count.

### Container units

We covered these briefly in Day 4. The four most useful:

- **`cqi`** — 1% of the container's inline size.
- **`cqb`** — 1% of the container's block size.
- **`cqw`** / **`cqh`** — aliases for `cqi` / `cqb` when the writing mode is horizontal.
- **`cqmin`** / **`cqmax`** — smaller / larger of the two.

Combine with `clamp()` for fluid type that responds to the container:

```css
.card-title {
  font-size: clamp(1rem, 0.8rem + 2cqi, 1.5rem);
}
```

Title is 1rem in a narrow card, 1.5rem in a wide card, scaling smoothly in between. Viewport size is irrelevant.

### Style queries

Container queries also test custom property values:

```css
@container style(--density: compact) {
  .toolbar button {
    padding: 0.25rem 0.5rem;
    font-size: 0.875rem;
  }
}
```

Set `--density: compact` on an ancestor; the toolbar inside compacts itself. Switch it to `--density: comfortable`; the toolbar relaxes. This is a light-touch way to implement variant components without classes.

`style()` queries are [in Interop 2026 for full cross-browser consistency](https://webkit.org/blog/17818/announcing-interop-2026/). Safari shipped them in 2024, Chrome and Firefox followed. If your audience is evergreen, use them.

## Part 5: `:has()` — The Parent Selector That Rewrote The Rulebook

For twenty years, CSS could only match elements *top-down*: a descendant combinator let you say ".card > .title", but no selector let you style *the card* based on whether it had a title. CSS was fundamentally parent-to-child.

[`:has()` changed that. It became Baseline Newly Available in December 2023](https://web.dev/baseline), and in 2026 is effectively universal. The syntax:

```css
element:has(relative-selector) { ... }
```

The element matches if the relative selector — applied from the element's position — matches anything. "Card that contains an image":

```css
.card:has(img) {
  grid-template-rows: auto 1fr;
}
```

"Label that contains an invalid input":

```css
label:has(:invalid) {
  color: oklch(50% 0.2 25);
}
```

"Form where the submit button is disabled":

```css
form:has(button[type="submit"]:disabled) {
  opacity: 0.9;
}
```

### Why this matters for responsive design

`:has()` makes CSS reactive to DOM state without any JavaScript. Before `:has()`, these behaviours required event handlers and class toggling. Now they are declarative.

**A layout that changes when a panel is open:**

```css
.layout:has(.side-panel[open]) {
  grid-template-columns: 1fr 20rem;
}
.layout {
  grid-template-columns: 1fr;
}
```

The layout has two columns when the side panel is open, one when it is closed. The `<details open>` state is the driver. No JavaScript.

**A page that reacts to a form's validity:**

```css
body:has(form:valid) .submit-button {
  background: var(--brand);
}
body:has(form:invalid) .submit-button {
  background: gray;
  cursor: not-allowed;
}
```

The submit button changes colour based on the form's validity. Again, no JS.

### Combining `:has()` with sibling combinators

`:has()` accepts any relative selector, including sibling combinators. This gives us a **previous sibling selector**, something CSS lacked for two decades:

```css
/* Heading immediately followed by a paragraph */
h2:has(+ p) {
  margin-bottom: 0;
}
```

**All cards before the hovered one:**

```css
.card:has(~ .card:hover) {
  opacity: 0.8;
}
```

**The form field whose input has focus:**

```css
.field:has(:focus) {
  outline: 2px solid var(--brand);
}
```

These patterns are transformative for interactive, state-reactive CSS.

### `:has()` performance — the myth

Early discussions worried that `:has()` would be catastrophically slow because the browser has to track state in both directions. In practice, all three major engines have invested heavily in optimising `:has()` and the performance impact is negligible for ordinary use. Do not write pathological cases (`:has(*:has(*))` across thousands of elements), but ordinary patterns perform fine. The [State of CSS 2025 survey](https://2025.stateofcss.com/) named `:has()` the most-used and most-loved CSS feature of the year. It is solid. Use it.

## Part 6: `user-preference` Queries — The Other Half Of Responsive

Responsive design is not only about viewport sizes. It is about matching each user's context — their device, their eyesight, their motion tolerance, their power budget, their preferences. Modern CSS exposes all of these as media features, and they are the other half of what "responsive" means in 2026.

### `prefers-color-scheme`

The user's OS-level light or dark preference.

```css
@media (prefers-color-scheme: dark) {
  :root { --bg: #111; --text: #eee; }
}
```

In 2026 we rarely write this directly — the `light-dark()` function does the same thing inline, which is cleaner. We will cover it fully in Day 6.

### `prefers-reduced-motion`

The user has asked for reduced motion at the OS level. This is an accessibility setting for people with vestibular disorders, motion sensitivity, or who are easily distracted. Respect it.

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

This brute-force rule kills all animations and transitions across the site. It is the one place we unreservedly use `!important` because it is an accessibility override. The `0.01ms` duration (rather than `0`) is a trick: it keeps `animation-end` and `transitionend` events firing so that JavaScript handlers that listen for them continue to work.

A more nuanced version keeps essential motion but kills decorative motion:

```css
@media (prefers-reduced-motion: no-preference) {
  .carousel {
    animation: slide 5s infinite;
  }
}
```

Invert the logic: only animate if the user has *not* asked for reduced motion. Decorative motion is now opt-in.

### `prefers-contrast`

The user has asked for higher or lower contrast at the OS level.

```css
@media (prefers-contrast: more) {
  :root {
    --text: black;
    --border: black;
    --border-width: 2px;
  }
}
```

For users with low vision or in high-glare environments, this makes everything sturdier.

### `prefers-reduced-transparency`

For users who find translucent or frosted-glass effects disorienting.

```css
.site-header {
  backdrop-filter: blur(8px);
  background: oklch(100% 0 0 / 0.8);
}

@media (prefers-reduced-transparency: reduce) {
  .site-header {
    backdrop-filter: none;
    background: var(--bg);
  }
}
```

A great deal of modern macOS and iOS design uses these translucent panels. For users with the preference set, we disable them.

### `prefers-reduced-data`

The user wants to save data — typically on a metered connection or slow network. [Not yet Baseline as of early 2026](https://developer.mozilla.org/en-US/docs/Web/CSS/@media/prefers-reduced-data), but progressing. Ship low-resolution images and fewer decorative elements:

```css
@media (prefers-reduced-data: reduce) {
  .hero {
    background-image: url("/hero-small.webp");
  }
  .decorative-video { display: none; }
}
```

### `forced-colors`

For users of Windows High Contrast Mode and equivalent. The browser forcibly substitutes system colours (buttons, backgrounds, text) to guarantee legibility.

```css
@media (forced-colors: active) {
  .button {
    border: 1px solid ButtonText;
    background: ButtonFace;
    color: ButtonText;
  }
}
```

Using the system colour keywords (`ButtonText`, `ButtonFace`, `Canvas`, `CanvasText`, `Highlight`, `HighlightText`, `LinkText`, `VisitedText`, `GrayText`) produces something that works in every forced-color theme.

### `inverted-colors`

Some users run their OS with inverted colours for visual comfort. `inverted-colors: inverted` lets you detect this and, for example, un-invert images that shouldn't be inverted (logos, photos).

```css
@media (inverted-colors: inverted) {
  img, video {
    filter: invert(1) hue-rotate(180deg);
  }
}
```

### `hover` and `pointer`

Not user preferences exactly, but device characteristics:

- **`hover: hover`** — the primary pointer can hover (mouse).
- **`hover: none`** — no hover (touch).
- **`pointer: fine`** — precise pointer (mouse, stylus).
- **`pointer: coarse`** — imprecise pointer (finger).

Use them to avoid applying hover effects that will never show on touch:

```css
@media (hover: hover) and (pointer: fine) {
  .card:hover {
    transform: translateY(-2px);
    box-shadow: var(--shadow-md);
  }
}
```

On mobile, the hover rule is ignored, avoiding the "sticky hover" problem where tapping an element triggers the hover and leaves it stuck.

### Combined queries

You can combine these features:

```css
@media (prefers-color-scheme: dark) and (prefers-reduced-transparency: reduce) {
  .site-header {
    background: #000;
    backdrop-filter: none;
  }
}
```

For every combination the user could have, you can set the right defaults. This is genuine accessibility, not lip service.

## Part 7: Fluid Typography — A Complete Scale

Putting `clamp()` and fluid principles together, a complete fluid type scale for a magazine-style site looks like this:

```css
:root {
  /* Utopia-generated fluid scale, min 20rem → max 80rem */
  --step--2: clamp(0.6944rem, 0.6693rem + 0.1256vw, 0.7656rem);
  --step--1: clamp(0.8333rem, 0.7876rem + 0.2285vw, 0.9688rem);
  --step-0:  clamp(1rem,      0.9286rem + 0.3571vw, 1.25rem);
  --step-1:  clamp(1.2rem,    1.0857rem + 0.5714vw, 1.5625rem);
  --step-2:  clamp(1.44rem,   1.2629rem + 0.8857vw, 1.9531rem);
  --step-3:  clamp(1.728rem,  1.4571rem + 1.3543vw, 2.4414rem);
  --step-4:  clamp(2.0736rem, 1.6714rem + 2.0114vw, 3.0518rem);
  --step-5:  clamp(2.4883rem, 1.9086rem + 2.8986vw, 3.8147rem);
}
```

Those numbers came from [Utopia](https://utopia.fyi/) with inputs "minimum viewport 320px, minimum base size 16px, maximum viewport 1280px, maximum base size 20px, scale 1.2 minor third at minimum, 1.25 major third at maximum." Every viewport between 320px and 1280px gets a smooth interpolation; below 320px the minimum applies; above 1280px the maximum applies.

To use:

```css
h1 { font-size: var(--step-5); }
h2 { font-size: var(--step-4); }
h3 { font-size: var(--step-3); }
h4 { font-size: var(--step-2); }
p  { font-size: var(--step-0); }
small { font-size: var(--step--1); }
```

Now your typography scales smoothly at every viewport. Zero media queries. Zero per-breakpoint tuning. You will be shocked how much better this looks than a scale with hard breakpoints.

### Fluid spacing

The same approach works for spacing. A fluid spacing scale:

```css
:root {
  --space-3xs: clamp(0.25rem, 0.2143rem + 0.1786vw, 0.375rem);
  --space-2xs: clamp(0.5rem,  0.4286rem + 0.3571vw, 0.75rem);
  --space-xs:  clamp(0.75rem, 0.6429rem + 0.5357vw, 1.125rem);
  --space-s:   clamp(1rem,    0.8571rem + 0.7143vw, 1.5rem);
  --space-m:   clamp(1.5rem,  1.2857rem + 1.0714vw, 2.25rem);
  --space-l:   clamp(2rem,    1.7143rem + 1.4286vw, 3rem);
  --space-xl:  clamp(3rem,    2.5714rem + 2.1429vw, 4.5rem);
  --space-2xl: clamp(4rem,    3.4286rem + 2.8571vw, 6rem);
}
```

Section padding, card gaps, button padding — all of them scale smoothly with viewport. A hero section has modest padding on a phone and generous padding on a 4K monitor, with no jarring jumps at fake breakpoints.

## Part 8: The Responsive Navbar, Without Media Queries

Let us build something practical — a responsive navbar that collapses into a menu on small screens — without a single media query. This is the sort of thing you have written five or six times in your career using three or four different frameworks. In 2026 it is maybe forty lines of CSS and HTML together.

The HTML:

```html
<header class="site-header">
  <a class="logo" href="/">My Blazor Magazine</a>

  <button class="menu-toggle" popovertarget="main-menu" aria-label="Menu">
    <svg viewBox="0 0 24 24" width="24" height="24" aria-hidden="true">
      <path d="M3 6h18M3 12h18M3 18h18" stroke="currentColor" stroke-width="2" fill="none"/>
    </svg>
  </button>

  <nav id="main-menu" popover="auto" aria-label="Primary">
    <ul>
      <li><a href="/">Home</a></li>
      <li><a href="/blog">Blog</a></li>
      <li><a href="/about">About</a></li>
      <li><a href="/rss.xml">RSS</a></li>
    </ul>
  </nav>
</header>
```

Notice what we are using:

- A `<button>` with `popovertarget="main-menu"` opens the popover. No JavaScript.
- The `<nav>` with `popover="auto"` is a light-dismissible popover. Clicking outside closes it. Escape closes it.
- The toggle button has an `aria-label` because it has only an icon.

The CSS:

```css
.site-header {
  container-type: inline-size;
  container-name: site-header;
  display: grid;
  grid-template-columns: auto 1fr;
  align-items: center;
  padding: var(--space-s) var(--space-m);
  gap: var(--space-s);
  background: var(--bg);
  border-bottom: 1px solid var(--border);
}

.logo {
  font-weight: 700;
  text-decoration: none;
  color: inherit;
}

.menu-toggle {
  justify-self: end;
  background: none;
  border: 0;
  padding: var(--space-2xs);
  color: inherit;
  cursor: pointer;
}

#main-menu {
  position: fixed;
  inset: auto 0 0 0;
  max-height: 50dvh;
  padding: var(--space-m);
  background: var(--bg);
  border-top: 1px solid var(--border);
  border-radius: var(--radius-lg) var(--radius-lg) 0 0;
  margin: 0;
}

#main-menu ul {
  list-style: none;
  padding: 0;
  margin: 0;
  display: flex;
  flex-direction: column;
  gap: var(--space-s);
}

@container site-header (min-width: 40rem) {
  .menu-toggle { display: none; }

  #main-menu {
    position: static;
    inset: auto;
    max-height: none;
    padding: 0;
    background: none;
    border: none;
    border-radius: 0;
    display: block !important;
    /* popover="auto" defaults to display: none; we override when inline */
    opacity: 1 !important;
  }

  #main-menu ul {
    flex-direction: row;
    gap: var(--space-m);
  }
}
```

A single `@container` rule flips the behaviour. Below 40rem container width, the menu is a bottom-sheet popover triggered by the toggle button. Above 40rem, the toggle is hidden, the menu is displayed inline in the header, and the popover behaviour is suppressed.

The `display: block !important` and `opacity: 1 !important` overrides are one of the few legitimate uses of `!important` — we need to defeat the browser's default `display: none` on a `popover` element when we want to show it inline instead. In practice you will also want to add `popover="manual"` or handle this slightly differently depending on your HTML structure. There are cleaner variants using `<details>` instead of `popover`, or a plain `<nav>` with `hidden` toggled — pick what fits your project.

The important point: **no media queries**. The navbar adapts to its container. Drop it into a narrow sidebar on a wide screen and it becomes a mobile navbar. Drop it into a wide main column on a phone and it becomes a desktop navbar. That is what "responsive" actually means, and the old media-query approach could not do it.

### The `:has()` upgrade

One more refinement. We can use `:has()` to avoid one bit of JavaScript around "highlight the active nav link":

```css
.site-header nav a {
  padding: var(--space-2xs) var(--space-xs);
  border-radius: var(--radius-sm);
  text-decoration: none;
  color: inherit;
}

.site-header nav a[aria-current="page"] {
  background: var(--brand-muted);
  color: var(--brand);
}

.site-header:has(a[aria-current="page"]:hover) a[aria-current="page"] {
  outline: 2px solid var(--brand);
}
```

Small example, but illustrative: we are styling the header based on a descendant's state. Without `:has()` this would require JavaScript.

## Part 9: Responsive Images In 2026

We covered `<img>` in Day 2, but the responsive-design lens deserves one more pass. The modern responsive image pattern:

```html
<picture>
  <source
    type="image/avif"
    srcset="/hero-400.avif 400w, /hero-800.avif 800w, /hero-1600.avif 1600w"
    sizes="(min-width: 60rem) 50vw, 100vw">
  <source
    type="image/webp"
    srcset="/hero-400.webp 400w, /hero-800.webp 800w, /hero-1600.webp 1600w"
    sizes="(min-width: 60rem) 50vw, 100vw">
  <img
    src="/hero-800.jpg"
    width="1600" height="900"
    alt="Developers looking at a screen of code."
    loading="lazy"
    decoding="async"
    fetchpriority="low">
</picture>
```

Three points:

1. **`srcset` + `sizes`** lets the browser choose the smallest image that will render crisply at the element's final size. It is one of the strongest performance wins available to you.
2. **Modern formats first.** AVIF before WebP before JPEG. Each format is roughly 30–50% smaller than the previous at equal quality. The browser picks the first it supports.
3. **`width` and `height`** on the `<img>` let the browser compute aspect ratio and reserve the correct space before the image loads — eliminating layout shift.

Combine with `aspect-ratio` in CSS for even stricter control:

```css
.hero img {
  aspect-ratio: 16 / 9;
  width: 100%;
  object-fit: cover;
}
```

And with `content-visibility: auto` for below-the-fold images:

```css
.below-fold {
  content-visibility: auto;
  contain-intrinsic-size: auto 500px;
}
```

The browser skips rendering the element entirely until it scrolls into view, reserving `500px` of placeholder space. Long lists render faster. [This feature has been Baseline Widely Available since 2024](https://dev.to/marianocodes/baseline-web-features-you-can-safely-use-today-to-boost-performance-4bnb).

## Part 10: Logical Properties — Responsive For The World

We mentioned logical properties in Day 4. They are worth revisiting here because "responsive" in a global product means working correctly in right-to-left languages (Arabic, Hebrew, Persian, Urdu) and in vertical writing modes (some Japanese and Chinese typography).

The mapping:

| Physical | Logical |
|---|---|
| `margin-top` | `margin-block-start` |
| `margin-bottom` | `margin-block-end` |
| `margin-left` | `margin-inline-start` |
| `margin-right` | `margin-inline-end` |
| `margin-left` + `margin-right` | `margin-inline` |
| `margin-top` + `margin-bottom` | `margin-block` |
| `width` | `inline-size` |
| `height` | `block-size` |
| `min-width` | `min-inline-size` |
| `max-width` | `max-inline-size` |
| `border-left` | `border-inline-start` |
| `padding-right` | `padding-inline-end` |
| `top` / `bottom` | `inset-block-start` / `inset-block-end` |
| `left` / `right` | `inset-inline-start` / `inset-inline-end` |

And many more. The shorthand `inset: 0 auto auto 0` becomes `inset-block: 0 auto; inset-inline: 0 auto`, or just `inset: 0 auto` for a four-value-combined version.

For an English-only site, the difference is invisible — but the moment someone asks "can we translate this into Arabic?" you are either rewriting every margin declaration or congratulating yourself for using logical properties from the start. Choose the second future.

The one property where `physical` still makes sense is when you want a corner truly in a corner: `border-bottom-right-radius: 0` is unambiguous. The logical equivalent is `border-end-end-radius: 0`, which also works but takes getting used to.

## Part 11: `interpolate-size` — Animating To Auto

A subtle but magical feature in 2026: CSS can finally animate from `height: 0` to `height: auto`. For twenty years this was impossible — you had to either hard-code a height, use JavaScript to measure, or use `max-height` as a hacky proxy.

The fix, [landing in Chrome and Safari in 2024 and Firefox in 2025](https://developer.mozilla.org/en-US/docs/Web/CSS/interpolate-size):

```css
:root {
  interpolate-size: allow-keywords;
}

details {
  overflow: clip;
}

details::details-content {
  height: 0;
  transition: height 300ms ease, content-visibility 300ms allow-discrete;
}

details[open]::details-content {
  height: auto;
}
```

`interpolate-size: allow-keywords` flips a global switch that lets browsers interpolate to intrinsic sizes. Combined with the `::details-content` pseudo-element (Baseline 2025), this animates the opening and closing of any `<details>` element smoothly. No JavaScript. No measuring. No fighting the browser.

If this one feature does not make you happy, you have not been paying attention for the last ten years.

## Part 12: Print Styles — The Forgotten Medium

Print styles are the thing every website forgets. But a printed article is often the final archival form of a piece of writing, and doing it right costs nothing:

```css
@media print {
  /* Larger, cleaner type */
  :root { font-size: 12pt; }
  body { max-width: none; line-height: 1.4; }

  /* Hide non-essential */
  .site-header, .site-footer, .no-print, nav, aside {
    display: none;
  }

  /* Inline link URLs so they remain useful on paper */
  a::after {
    content: " (" attr(href) ")";
    font-size: 0.85em;
    color: gray;
  }

  /* Don't break mid-paragraph across pages */
  p { orphans: 3; widows: 3; }

  /* Don't break headings from their following content */
  h1, h2, h3, h4 { break-after: avoid; }

  /* Each article starts on a new page */
  article { break-before: page; }

  /* Show images */
  img { max-width: 100%; break-inside: avoid; }
}
```

Test it. Hit Ctrl+P (Cmd+P on macOS) on any of your pages. Most sites look abominable. A good print stylesheet is a gift to readers who want to archive, share, or take notes.

## Part 13: Case Study — Rebuilding This Magazine's Home Page

The homepage of *My Blazor Magazine* has three zones: a featured posts carousel, a main list of recent posts, and a sidebar with tags and author info. In the old CSS (pre-Day-5), the layout was driven by three media queries at `640px`, `960px`, and `1280px`. The code looked like this (abbreviated):

```css
.home-grid { display: grid; grid-template-columns: 1fr; gap: 1rem; }
@media (min-width: 640px) {
  .home-grid { grid-template-columns: 1fr 1fr; }
}
@media (min-width: 960px) {
  .home-grid { grid-template-columns: 2fr 1fr; gap: 2rem; }
}
@media (min-width: 1280px) {
  .home-grid { grid-template-columns: 3fr 1fr; }
  .home-grid .sidebar { padding-inline-start: 2rem; }
}
```

Four blocks, clutter, tuning for three fake devices. The 2026 rewrite is one declaration:

```css
.home-grid {
  display: grid;
  grid-template-columns: minmax(0, 3fr) minmax(min-content, 1fr);
  gap: clamp(1rem, 2vw, 2rem);
}

@container (max-width: 40rem) {
  .home-grid {
    grid-template-columns: 1fr;
  }
}
```

Two rules. The sidebar never exceeds 1fr, never shrinks below its own minimum content. The gap scales fluidly. Below 40rem container width, it stacks. At every viewport width in between, the ratio is gracefully maintained. We deleted sixteen lines and added better behaviour.

Multiply this across the 2000 lines of layout CSS the magazine had in 2023. We have, after Day 5 of this series in our own codebase, about 600 lines. Lightness-of-being achieved.

## Part 14: Common Anti-Patterns To Retire

Five patterns we see repeatedly in legacy code that are no longer necessary:

### 1. `width: 100%` on everything

```css
/* Old */
.container { width: 100%; box-sizing: border-box; }

/* New */
.container { /* don't specify width */ }
```

A block-level element is 100% wide by default. Explicitly setting it is noise. Worse, with `padding` added, it creates overflow problems. Leave it alone.

### 2. Media queries for every component

```css
/* Old */
.card { font-size: 14px; }
@media (min-width: 768px) { .card { font-size: 16px; } }
@media (min-width: 1024px) { .card { font-size: 18px; } }

/* New */
.card { font-size: clamp(0.875rem, 1rem + 0.2vw, 1.125rem); }
```

One line. Smoother scaling. No cliffs.

### 3. `min-height: 100vh`

```css
/* Old */
.full-screen { min-height: 100vh; }

/* New */
.full-screen { min-height: 100dvh; }
```

On mobile, `100vh` causes content to hide under the browser chrome when it appears. `100dvh` tracks the dynamic viewport.

### 4. Wrapper divs for centering

```html
<!-- Old -->
<div class="outer">
  <div class="middle">
    <div class="inner">
      Content
    </div>
  </div>
</div>

<!-- New -->
<div class="centered">
  Content
</div>
```

```css
.centered {
  display: grid;
  place-items: center;
}
```

### 5. Hand-built carousels

Native solutions are coming: [scroll-snap](https://developer.mozilla.org/en-US/docs/Web/CSS/CSS_scroll_snap), [carousel primitives with `::scroll-button()` and `::scroll-marker()`](https://chrome.dev/css-wrapped-2025/), and the Interest Invoker API for hover tooltips. The hand-rolled libraries should be retired for most use cases as these features land.

## Part 15: The Responsive Design Checklist For 2026

Run this against your next project before shipping:

- [ ] No `width: 100%` declarations on block elements unless there is a reason.
- [ ] All font sizes use `clamp()` or a fluid scale.
- [ ] All spacing tokens use `clamp()` or fluid.
- [ ] Grid layouts use `repeat(auto-fill, minmax(Xrem, 1fr))` for responsive card grids.
- [ ] Viewport-specific behaviour in media queries is limited to `prefers-*` features (color-scheme, reduced-motion, contrast, transparency, data).
- [ ] Component-level responsive behaviour uses container queries.
- [ ] DOM-state-reactive styling uses `:has()` instead of JavaScript class toggling.
- [ ] Logical properties (`margin-inline`, `padding-block`, etc.) used throughout.
- [ ] `100dvh`, not `100vh`, for full-screen sections on mobile.
- [ ] Images have `width`, `height`, `loading`, `decoding`, and (where appropriate) `fetchpriority`.
- [ ] Every `<picture>` offers AVIF and WebP sources.
- [ ] `prefers-reduced-motion` media query present and honoured.
- [ ] `prefers-reduced-transparency` respected for any blur or opacity effects.
- [ ] Print stylesheet exists and has been tested.
- [ ] Forced-colors mode tested.

If your project ships against this checklist, it will work — gracefully — on every device, every browser, every accessibility setting, and every future viewport size nobody has thought of yet.

## Part 16: Tomorrow

Tomorrow — **Day 6: Colour, Typography, and Motion — `oklch`, `light-dark()`, Variable Fonts, View Transitions** — we cover the design side of modern CSS. How to build a complete design system without a design system. Perceptually uniform colour. Automatic dark mode. Variable fonts that ship one file to cover every weight. Free motion via the View Transitions API. By the end you will know why `#ff0000` is a worse red than `oklch(60% 0.2 25)`, and how to animate a page transition with one line of CSS.

---

## Series navigation

You are reading **Part 5 of 15**.

- [Part 1: Overview — Why a Plain Browser Is Enough in 2026](/blog/2026-05-24-no-build-web-overview)
- [Part 2: Semantic HTML and the Document Outline Nobody Taught You](/blog/2026-05-25-no-build-web-html-semantics)
- [Part 3: The Cascade, Specificity, and `@layer`](/blog/2026-05-26-no-build-web-css-cascade-layers)
- [Part 4: Modern CSS Layout — Flexbox, Grid, Subgrid, and Container Queries](/blog/2026-05-27-no-build-web-modern-css-layout)
- **Part 5 (today): Responsive Design in 2026 — Container Queries, `clamp()`, Fluid Type, and `:has()`**
- Part 6 (tomorrow): Colour, Typography, and Motion
- Part 7: Native ES Modules
- Part 8: The DOM, Events, and Platform Primitives
- Part 9: State Management Without a Library
- Part 10: Web Components
- Part 11: Client-Side Routing with the Navigation API and View Transitions
- Part 12: Forms, Validation, and the Constraint Validation API
- Part 13: Storage, Service Workers, and Offline
- Part 14: Accessibility, Performance, and Security
- Part 15: The Conclusion — A Complete Application End to End

## Resources

- [Ethan Marcotte, *Responsive Web Design*, *A List Apart*, 2010](https://alistapart.com/article/responsive-web-design/) — the article that started it all.
- [MDN: Container queries](https://developer.mozilla.org/en-US/docs/Web/CSS/CSS_containment/Container_queries) — reference and examples.
- [MDN: `:has()`](https://developer.mozilla.org/en-US/docs/Web/CSS/:has) — the parent selector.
- [MDN: Media queries](https://developer.mozilla.org/en-US/docs/Web/CSS/CSS_media_queries) — full reference for `prefers-*` features.
- [MDN: `clamp()`](https://developer.mozilla.org/en-US/docs/Web/CSS/clamp), [`min()`](https://developer.mozilla.org/en-US/docs/Web/CSS/min), [`max()`](https://developer.mozilla.org/en-US/docs/Web/CSS/max) — the fluid math functions.
- [Utopia.fyi](https://utopia.fyi/) — fluid type and space scale calculator.
- [Every Layout](https://every-layout.dev/) — Heydon Pickering and Andy Bell's book on intrinsic CSS layouts.
- [web.dev: Container queries](https://web.dev/articles/css-container-queries) — practical intro.
- [State of CSS 2025](https://2025.stateofcss.com/) — what features are being adopted, loved, and abandoned.
- [Adrian Roselli on logical properties and accessibility](https://adrianroselli.com/) — nuance and edge cases.
- [CSS Containment Module Level 3](https://www.w3.org/TR/css-contain-3/) — the container queries spec.
