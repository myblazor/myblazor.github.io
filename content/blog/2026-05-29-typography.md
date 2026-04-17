---
title: "Without a Net, Part 6: Colour, Typography, and Motion — oklch, light-dark(), Variable Fonts, and View Transitions"
date: 2026-05-29
author: myblazor-team
summary: "Day 6 of our fifteen-part no-build web series covers the design half of modern CSS — perceptually uniform colour with oklch(), automatic theme switching with light-dark(), variable fonts that ship one file to cover every weight, typographic refinements you did not know existed, and the View Transitions API that animates page changes with a single function call. You will leave this article with a complete design system, in plain CSS, that beats every 'utility framework' on file size and flexibility."
tags:
  - css
  - color
  - oklch
  - light-dark
  - typography
  - variable-fonts
  - view-transitions
  - motion
  - animation
  - accessibility
  - web-standards
  - no-build
  - first-principles
  - series
  - deep-dive
---

## Introduction: The Red That Was Not Quite Red

A colleague showed us this once, slightly embarrassed, in a design review. She had been asked to build a "status badge" component with three states — success, warning, error. She had used the standard hex codes most of us reach for when we are not paying attention: `#00ff00` for success, `#ffff00` for warning, `#ff0000` for error. Pure RGB primaries. Standard web reds and yellows. The kind of values you dash off in five seconds and never think about again.

The design review was not going well. The CEO looked at the screen, squinted, and said: "Why is the warning yellow hurting my eyes? And why does the error red look kind of… orange? Are we doing accessibility on this?"

Our colleague had not, in fact, been doing accessibility on this. She had been using the first hex codes that came to mind. But the CEO's complaint was real. `#ffff00` on a white background is genuinely eye-searing. `#ff0000` on a white background has a peculiar tendency to look slightly off. `#00ff00` is the colour of an early-2000s neon t-shirt, not a success indicator. And the contrast between the three — in terms of perceived brightness, the thing that actually matters for differentiation — was uneven and unintuitive. The yellow was far brighter than the red, which was far brighter than the green. All three had the "same" value in RGB terms, but none of them had the same *perceptual* value. The designers' eyes were not lying.

That was the moment this team discovered `oklch()`.

Day 6 is about the design side of modern CSS — the layer on top of layout that makes a page actually look and feel like something. Colour, typography, motion, theming, and the delightful set of features that have landed in the last three years to make every one of those concerns dramatically easier. By the end, you will have a complete design system in plain CSS that is smaller, more flexible, and more accessible than anything you could assemble by importing a design-token library from npm.

A running theme of this article: **modern CSS has moved from RGB to perceptual colour, from static fonts to variable fonts, from keyframe animations to declarative transitions, from manual theme switching to automatic adaptation.** Each of these moves is a case of the platform catching up with what we have been doing with libraries and preprocessors, and each of them removes a dependency you no longer need.

This is Part 6 of 15 in our no-build web series. If you have followed [Part 1](/blog/2026-05-24-no-build-web-overview) through [Part 5](/blog/2026-05-28-no-build-web-responsive-design), you will recognise the tone. If you are dropping in fresh, welcome — most of today stands on its own.

## Part 1: Colour — From RGB To OKLCH

Before we talk about `oklch()`, we need to talk about why RGB is not enough.

RGB — and its hex sibling — describes colours in terms of three light sources: red, green, and blue. Each channel gets a value from 0 to 255 (or 00 to FF in hex). The colour is whatever you get when you mix those three lights at those intensities. This is the native colour space of computer screens, which is why the web adopted it in the 1990s, and it is fine for a lot of things.

But RGB has three problems that bite you the moment you try to build a real design system.

**Problem 1: unequal perceptual brightness.** `#ff0000` (red) and `#00ff00` (green) have, in a naive sense, the same "intensity" — both are the maximum value of a single channel. But green is *far* brighter to the human eye than red. If you put `#ff0000` and `#00ff00` side by side on a white background, the green almost disappears and the red is barely visible. Now imagine you are using both as status colours: the eye-wateringly bright yellow and the barely-visible red are going to confuse users. RGB does not know anything about how humans perceive light; it is just three numbers.

**Problem 2: unpredictable interpolation.** If you try to compute a 50/50 blend between `#ff0000` and `#0000ff` — red and blue — you get `#800080`, which is a muddy dark purple. Not the vibrant purple you would expect. This happens because RGB averaging throws away the perceptual meaning. Every designer who has ever tried to generate a colour palette by interpolating between two brand colours in RGB has hit this wall.

**Problem 3: no ability to adjust brightness without shifting hue.** If you want to take your brand colour and produce a "lighter" version for hover states, you are stuck in RGB — there is no direct way to say "the same colour but 10% brighter." You can add to each channel (`+0x20 to each`), but that shifts the hue too. You can blend with white, but that also shifts the hue. Designers work around this with opinions and elbow grease, but it is fundamentally the wrong tool.

### Enter `oklch()`

`oklch()` is a colour function based on the OKLab colour space, which was developed in 2020 as an improvement on the earlier LAB colour space. It describes colours in terms of three values:

- **L (Lightness)** — 0% (black) to 100% (white). Perceptually uniform: 50% is visually half as bright as 100%.
- **C (Chroma)** — 0 (grey) to about 0.4 (max saturation). Not bounded at 1 because different hues hit their saturation limits at different values.
- **h (Hue)** — 0° to 360°. The colour wheel. 0° is red, 120° green, 240° blue.

```css
:root {
  --brand: oklch(60% 0.15 250);       /* a lightly-saturated blue */
  --success: oklch(60% 0.15 145);     /* same lightness, green hue */
  --warning: oklch(75% 0.15 85);      /* slightly lighter, yellow-orange */
  --error:   oklch(55% 0.2  25);      /* slightly darker, red-orange */
}
```

Those four colours have consistent perceptual weight. The success green, warning yellow, and error red all have comparable lightness and chroma — the eye perceives them as the "same size" of colour. Put them on a page and they differentiate beautifully, without any one overpowering the others.

More importantly, `oklch()` lets you construct a palette from rules instead of guesses.

### Building a palette with `oklch()`

Say your brand hue is 250 (a blue). Build the palette by varying lightness and chroma:

```css
:root {
  --brand-h: 250;
  --brand-c: 0.15;

  --brand-50:  oklch(97% 0.02 var(--brand-h));
  --brand-100: oklch(92% 0.04 var(--brand-h));
  --brand-200: oklch(85% 0.08 var(--brand-h));
  --brand-300: oklch(75% 0.12 var(--brand-h));
  --brand-400: oklch(65% var(--brand-c) var(--brand-h));
  --brand-500: oklch(55% var(--brand-c) var(--brand-h));  /* the canonical brand */
  --brand-600: oklch(45% var(--brand-c) var(--brand-h));
  --brand-700: oklch(35% 0.12 var(--brand-h));
  --brand-800: oklch(25% 0.08 var(--brand-h));
  --brand-900: oklch(15% 0.04 var(--brand-h));
  --brand-950: oklch(10% 0.02 var(--brand-h));
}
```

Change `--brand-h` from 250 to 145 (green), and your entire palette shifts to a green theme. Change the chroma, and the whole palette becomes more or less vibrant. You are now designing *by principle*, not by guessing hex codes.

This is [the exact technique Tailwind's v4 uses to generate its palette](https://tailwindcss.com/) in a way that supports arbitrary brand hues. You are doing what Tailwind does, with one declaration at the top of your tokens file.

### Relative colour syntax — `oklch(from ...)`

Baseline in 2025, relative colour syntax lets you derive one colour from another:

```css
.button {
  --base: oklch(60% 0.15 250);
  background: var(--base);
}
.button:hover {
  background: oklch(from var(--base) calc(l - 10%) c h);
}
```

`oklch(from <color> L C H)` takes a color and lets you modify individual channels. Here `calc(l - 10%)` is "10% darker than the base." `c` and `h` keep the original chroma and hue. Perfect hover variant, derived from the base — if you change the base, the hover updates automatically.

### `color-mix()` — the blending function

`color-mix()` has been Baseline Widely Available since 2024. It blends two colours in any colour space:

```css
--brand-subtle: color-mix(in oklch, var(--brand) 20%, transparent);
--brand-bg:     color-mix(in oklch, var(--brand) 10%, var(--surface));
```

The first produces a 20%-opaque version of the brand (a "subtle" background for hover or selection). The second produces a 10% mix with the surface colour (a "tinted" background). Both adapt automatically when the brand or surface colour changes.

Always specify the colour space for `color-mix()` — `in oklch` or `in srgb`. `in oklch` produces perceptually-weighted blends that look right to the human eye. `in srgb` produces the old RGB-averaged blends, which are often wrong. Use `oklch` unless you have a specific reason not to.

### `contrast-color()` — guaranteed legible text

[`contrast-color()` became Baseline Newly Available in late 2025](https://web.dev/blog/web-platform-12-2025) with Firefox 146's support. It takes a colour and returns black or white, whichever has more contrast:

```css
.button {
  --base: oklch(60% 0.15 250);
  background: var(--base);
  color: contrast-color(var(--base));
}
```

Change `--base` to any colour — the button's text colour updates to black or white, whichever passes WCAG AA contrast. No designer decisions needed. No palette-specific text colour overrides. Works for every variant of every colour.

This is the kind of thing that used to take JavaScript (`chroma-js.contrast()`) or a preprocessor (`sass-light-dark()` functions). Now it is native CSS.

### Wide-gamut colour

Modern monitors and phones support colour ranges beyond sRGB — P3, Rec. 2020, and larger colour spaces that include colours physically impossible to reproduce on a 2005 LCD. `oklch()` naturally addresses these wider gamuts, but you can also use the explicit `color()` function:

```css
--vivid-red: color(display-p3 1 0 0);
--vivid-red-fallback: #ff0000;
```

Safari has had P3 support since 2017; Chrome and Firefox have it in 2026. On a wide-gamut display the colour is vivid; on an sRGB display the browser approximates. For subtle aesthetic gains — especially in photography-heavy sites — wide-gamut colour adds depth without hurting anyone on older displays.

Use `@media (color-gamut: p3)` to conditionally opt into wider colours:

```css
@media (color-gamut: p3) {
  :root {
    --brand: color(display-p3 0.2 0.4 0.9);
  }
}
```

For most sites, `oklch()` is sufficient — the browser handles the gamut mapping automatically.

### Colour summary — what to write in 2026

**Write `oklch()` everywhere.** Do not write hex for colour tokens. Do not use `rgb()` or `hsl()` unless you have a specific reason. Every design token for colour in your `tokens.css` should be `oklch()`. Relative colour syntax, `color-mix()`, and `contrast-color()` cover every derived-colour case.

Hex is fine for one-offs (`background: #fafafa`), but treat it like you treat literal pixels — a direct value, not a semantic token.

## Part 2: Light Mode, Dark Mode, Auto Mode

Three ways to do dark mode in 2026, in order of increasing sophistication.

### The old way (still works)

```css
:root {
  --bg: oklch(99% 0 0);
  --text: oklch(15% 0 0);
}

@media (prefers-color-scheme: dark) {
  :root {
    --bg: oklch(12% 0 0);
    --text: oklch(95% 0 0);
  }
}
```

Two blocks. Works in every browser going back years. Still correct. The downside is you have to duplicate every colour declaration.

### The slightly nicer way

```css
:root {
  color-scheme: light dark;
  --bg: light-dark(oklch(99% 0 0), oklch(12% 0 0));
  --text: light-dark(oklch(15% 0 0), oklch(95% 0 0));
}
```

One block. Each token has two values — the first for light, the second for dark. The `color-scheme: light dark` line tells the browser that this page supports both modes, which activates default dark-mode styles for native controls (scrollbars, form fields, date pickers). `light-dark()` picks the right value based on the active mode.

[`light-dark()` is Baseline Newly Available since May 2024](https://web.dev/baseline), with Widely Available expected November 2026. Safe to use now if your audience is evergreen. The one caveat: `light-dark()` only accepts colours (and since 2025, gradients). It does not work on other values — you still use `prefers-color-scheme` for those.

### The flexible way — user override

Many users want to override the OS preference. You want a theme toggle. The solution combines `color-scheme` with a data attribute:

```html
<html data-theme="auto">
```

```css
:root {
  color-scheme: light dark;
}

:root[data-theme="light"] {
  color-scheme: light;
}

:root[data-theme="dark"] {
  color-scheme: dark;
}

:root {
  --bg: light-dark(oklch(99% 0 0), oklch(12% 0 0));
  --text: light-dark(oklch(15% 0 0), oklch(95% 0 0));
}
```

Now `data-theme="auto"` follows the OS. `data-theme="light"` and `data-theme="dark"` force the theme. `light-dark()` returns the right value in each case, because it reads the resolved `color-scheme`.

A minimal JavaScript enhancement to make the toggle work:

```html
<button id="theme-toggle" aria-label="Toggle theme">Toggle</button>

<script type="module">
  const root = document.documentElement;
  const saved = localStorage.getItem("theme") ?? "auto";
  root.dataset.theme = saved;

  document.getElementById("theme-toggle").addEventListener("click", () => {
    const next =
      root.dataset.theme === "auto" ? "dark" :
      root.dataset.theme === "dark" ? "light" : "auto";
    root.dataset.theme = next;
    localStorage.setItem("theme", next);
  });
</script>
```

Three-state toggle (auto → dark → light → auto). Persisted in `localStorage`. No flash of wrong theme on reload, because we set `dataset.theme` before any paint. This is the complete modern dark-mode implementation. Your production stylesheet should add maybe 15–20 more colour tokens; the structure stays the same.

### Prevent the flash of unstyled theme

If you set the theme only in JavaScript after the DOM loads, users see a light flash before the dark theme applies. The fix: inline a blocking script in `<head>` that reads `localStorage` and sets `data-theme` before the page paints:

```html
<head>
  <script>
    (function() {
      const saved = localStorage.getItem("theme") ?? "auto";
      document.documentElement.dataset.theme = saved;
    })();
  </script>
  <!-- stylesheet follows -->
</head>
```

Small, sync, inline. The theme attribute is set before the first paint. No flash.

## Part 3: Typography — Variable Fonts

Static fonts are individual files: one per weight, one per italic, one per width. A complete font family with regular, italic, bold, bold italic, and four weights is eight files, and can total 400–800KB.

A **variable font** is one file containing the entire family as a continuum along design axes. [Variable fonts have been Baseline since 2018](https://caniuse.com/variable-fonts), and most of Google Fonts now offers variable versions. A complete variable font is typically smaller than three static weights combined, and gives you access to *every* weight, width, and slant in between the extremes.

### The five registered axes

OpenType defines five "registered" axes that map to standard CSS properties:

| Axis tag | CSS property | What it does |
|---|---|---|
| `wght` | `font-weight` | Boldness (often 100 to 900, but some fonts extend further) |
| `wdth` | `font-stretch` | Width — condensed to expanded |
| `slnt` | `font-style: oblique Xdeg` | Slant angle (0 to about 15 degrees) |
| `ital` | `font-style: italic` | Italic (0 or 1 — usually binary) |
| `opsz` | `font-optical-sizing` | Optical size — small text gets sturdier strokes |

### Loading a variable font

```css
@font-face {
  font-family: "Inter";
  src: url("/fonts/Inter-Variable.woff2") format("woff2-variations");
  font-weight: 100 900;        /* range supported */
  font-style: normal;
  font-display: swap;
}

@font-face {
  font-family: "Inter";
  src: url("/fonts/Inter-Italic-Variable.woff2") format("woff2-variations");
  font-weight: 100 900;
  font-style: italic;
  font-display: swap;
}
```

Two declarations: one for the upright variable font, one for the italic. `font-weight: 100 900` declares the range of weights available, which lets CSS use `font-weight: 542` (or any number) at the author's discretion.

Using it is no different from static fonts:

```css
body {
  font-family: "Inter", system-ui, sans-serif;
  font-weight: 400;
}

h1 {
  font-weight: 700;
}

.lede {
  font-weight: 450;   /* a specific intermediate weight */
}

strong {
  font-weight: 600;
}
```

For custom axes (anything beyond the five registered), use `font-variation-settings`:

```css
h1 {
  font-variation-settings: "GRAD" 150;  /* Grade axis — like weight but without width change */
}
```

`GRAD` and other custom axes are all-caps to distinguish from registered ones.

### Fluid variable fonts

Combine variable fonts with `clamp()` and you get typography that responds continuously to the viewport:

```css
h1 {
  font-size: clamp(1.5rem, 1rem + 2vw, 3rem);
  font-weight: clamp(600, 550 + 1vw, 800);    /* does not quite work — see below */
}
```

Unfortunately, `clamp()` does not interpolate `font-weight` smoothly because `font-weight` is an integer in some resolution paths. The workaround: set `font-weight` once and animate or transition it with CSS if you need motion. For static responsive weights, use a small set of viewport-based overrides or accept that the weight is stepwise.

A better trick: use the `GRAD` custom axis of fonts that support it (Inter, Roboto Flex, Noto) and animate the grade — which changes the visual weight *without changing letter widths*, which means no reflow.

```css
.heading {
  transition: font-variation-settings 200ms;
}
.heading:hover {
  font-variation-settings: "GRAD" 150;
}
```

Hover produces a subtle weight increase without text reflow. This is impossible with static fonts.

### Optical sizing

`font-optical-sizing: auto` (the default in some browsers) tells the browser to adjust the `opsz` axis based on the rendered font size. Small text gets sturdier, large text gets more elegant. This used to require manual font selection ("use the Display version above 36px"); now it is one line. [`font-optical-sizing` is Baseline](https://developer.mozilla.org/en-US/docs/Web/CSS/font-optical-sizing) and basically free to enable:

```css
body {
  font-optical-sizing: auto;
}
```

Forget it is there; enjoy the improved typography. Especially noticeable on serif fonts.

### System font stack

The cheapest, fastest, most consistent font is the user's OS default. For UI-centric sites, the modern system font stack is:

```css
:root {
  --font-ui: system-ui, -apple-system, "Segoe UI Variable", "Segoe UI", Roboto,
             "Helvetica Neue", Arial, sans-serif;
}
```

On macOS you get San Francisco. On Windows you get Segoe UI Variable (yes — Segoe is a variable font now). On Android you get Roboto Flex (also variable). On iOS you get San Francisco. Every one of them is high-quality, pre-loaded, and zero bytes to download.

For headings or editorial sites where you want a specific typeface, load one variable font. One file. Two font-face declarations (roman + italic). Total download: maybe 80KB. Much cheaper than the Google Fonts embed you have been pasting into the `<head>`.

### `font-display`

Controls how the browser handles FOIT (flash of invisible text) vs FOUT (flash of unstyled text):

- **`auto`** — browser default.
- **`block`** — invisible for up to 3 seconds, then fallback. Do not use.
- **`swap`** — fallback immediately, swap when custom font loads. Default choice for body text.
- **`fallback`** — brief invisible, then fallback; swap only if font arrives within 3 seconds. Good for non-critical text.
- **`optional`** — the best choice for visually-nice-but-not-critical fonts. The browser may not load the font at all on slow connections.

Use `swap` for body text if the custom font is important, `optional` if the fallback is acceptable. We will revisit this in Day 14 on performance.

### Typographic refinements you did not know existed

Modern CSS has a handful of typography improvements that fix problems most of us have lived with for years.

**`text-wrap: balance`**: balances line lengths so headings do not end with a lonely word on the last line.

```css
h1, h2, h3 {
  text-wrap: balance;
}
```

**`text-wrap: pretty`**: avoids orphans in paragraphs (the last line being a single word).

```css
p {
  text-wrap: pretty;
}
```

Both [are Baseline since 2024](https://web.dev/baseline). The visual improvement is immediately obvious — your headings and paragraphs suddenly look professionally typeset.

**`hanging-punctuation: first`**: makes opening quotes hang outside the text block for cleaner optical alignment.

**`text-underline-offset`** and **`text-decoration-thickness`**: control the position and thickness of underlines. Default underlines are often too close to the text and too thick. A small tweak:

```css
a {
  text-underline-offset: 0.15em;
  text-decoration-thickness: 0.08em;
}
```

Subtle; transformative.

**`font-synthesis: none`**: prevents the browser from faking bold or italic if those weights are not loaded. Setting to `none` means you will see regular text instead of a synthesised bold — useful if fake-bold is visually jarring.

**`font-feature-settings`**: access OpenType features like small caps, stylistic sets, and ligatures:

```css
.numbers {
  font-variant-numeric: tabular-nums;    /* monospaced digits for tables */
}
.headlines {
  font-variant-ligatures: discretionary-ligatures;
  font-feature-settings: "ss02" 1;       /* stylistic set 2 */
}
```

These features are in most modern fonts and trivially improve rendering.

**`line-height: 1.5`** (or close to it) for body text; `1.1` to `1.2` for headings. Smaller line-heights for big text, larger for small text. `calc(1em + 0.725rem)` produces a line-height that scales sensibly with font size.

## Part 4: Motion — Transitions, Animations, Keyframes

CSS has had transitions and animations for over a decade. The refinements since 2023 are what make them feel natively-integrated.

### Transitions — the workhorse

A transition smoothly interpolates a property change:

```css
.button {
  background: var(--brand);
  transition: background 200ms ease;
}
.button:hover {
  background: var(--brand-hover);
}
```

Four-part shorthand: `property | duration | easing | delay`. Multiple properties:

```css
transition:
  background 200ms ease,
  transform 150ms ease-out,
  box-shadow 200ms ease;
```

Easing functions worth knowing:

- **`ease`** — default. Slow start, fast middle, slow end.
- **`linear`** — constant speed. Sometimes the right choice for long animations or scroll-driven effects.
- **`ease-in`** — slow start.
- **`ease-out`** — slow end. Usually the right choice for things entering the screen.
- **`ease-in-out`** — slow both ends.
- **`cubic-bezier(x1, y1, x2, y2)`** — custom curve. Use a visual tool like [easings.net](https://easings.net/) to design them.
- **`steps(n)`** — discrete steps. Good for "loading dots" or typewriter effects.

A production-quality default:

```css
:root {
  --easing: cubic-bezier(0.2, 0, 0, 1);
  --duration-fast: 150ms;
  --duration-base: 300ms;
  --duration-slow: 500ms;
}
```

The `cubic-bezier(0.2, 0, 0, 1)` curve is the "Material Design standard" easing — fast out, slow in. Looks good on most interactions.

### Transitioning `display: none`

For a long time, transitions could not involve `display: none`. You could not fade an element in and out with `display` toggling. The workaround was `opacity` + `visibility`, which did not fully remove from the tab order.

[In 2024 this was fixed with `display: allow-discrete` and `@starting-style`](https://developer.chrome.com/blog/entry-exit-animations):

```css
.toast {
  opacity: 1;
  transition:
    opacity 300ms,
    display 300ms allow-discrete;
}

.toast[hidden] {
  opacity: 0;
  display: none;
}

@starting-style {
  .toast {
    opacity: 0;
  }
}
```

Now toggling `hidden` on the `.toast` element fades it out, waits for the transition to finish, then sets `display: none`. Adding `hidden` produces the fade-in. The `@starting-style` rule gives the "before" state for enter animations.

This is one of the most-asked-for features in CSS for a decade. It has arrived. Use it.

### Keyframe animations

For things that loop or have multiple steps, keyframes:

```css
@keyframes pulse {
  0%, 100% { transform: scale(1); }
  50% { transform: scale(1.05); }
}

.notification-bell {
  animation: pulse 2s ease-in-out infinite;
}
```

Animation shorthand: `name | duration | easing | delay | iteration-count | direction | fill-mode | play-state`.

Useful properties:

- **`animation-fill-mode: forwards`** — keep the final state after the animation ends.
- **`animation-play-state: paused`** — freeze the animation. Useful for hover-to-pause.
- **`animation-delay`** — wait before starting.
- **`animation-timing-function: steps(n, start)`** — for typewriter or flicker effects.
- **`animation-composition: add`** — layer multiple animations on the same property.

### Motion design tokens

Put your motion values in your tokens file, not inline:

```css
:root {
  --duration-micro: 100ms;
  --duration-fast: 150ms;
  --duration-base: 250ms;
  --duration-slow: 400ms;
  --duration-slower: 600ms;

  --ease-linear: linear;
  --ease-in: cubic-bezier(0.4, 0, 1, 1);
  --ease-out: cubic-bezier(0, 0, 0.2, 1);
  --ease-in-out: cubic-bezier(0.4, 0, 0.2, 1);
  --ease-bounce: cubic-bezier(0.68, -0.55, 0.27, 1.55);
}
```

Every transition and animation references these. The whole site's motion personality changes by editing one file.

### Respecting reduced motion

We covered `prefers-reduced-motion` in Day 5. The universal rule:

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

This shuts down all motion. For essential transitions (focus rings, loading indicators) you may want to keep them — either scope the override to specific classes or flip the logic:

```css
@media (prefers-reduced-motion: no-preference) {
  .card:hover {
    transform: translateY(-2px);
    transition: transform 200ms var(--ease-out);
  }
}
```

Decorative motion is *opted into* by users without the preference set. Users with the preference get a static site. That is correct.

## Part 5: View Transitions — The Feature That Made SPAs Smooth

The [View Transitions API](https://developer.mozilla.org/en-US/docs/Web/API/View_Transitions_API) is the single most exciting front-end feature of the last three years, in our opinion. It animates DOM changes — and cross-document navigations — with a single API call, using a browser-native compositor. It is essentially free motion.

### Same-document view transitions

You have a button that replaces content. Normally this is a jarring jump. With View Transitions:

```javascript
document.startViewTransition(() => {
  // any DOM mutation — updating text, replacing a node, rerendering a list
  document.getElementById("content").innerHTML = newContent;
});
```

The browser:

1. Takes a snapshot of the current state.
2. Runs your callback, which updates the DOM.
3. Takes a snapshot of the new state.
4. Cross-fades between them with a default fade animation.

No keyframe definitions. No JavaScript animation library. No Framer Motion. No GSAP. Thirty bytes of code.

[Same-document view transitions reached Baseline Newly Available in October 2025](https://web.dev/blog/web-platform-12-2025). Safe to use in evergreen browsers.

### Named transitions — the "magic move"

Where it gets interesting: if two elements across the transition share a `view-transition-name`, the browser morphs between them. This is the "shared element" animation that every design system has been building since iOS 7:

```css
.card-image {
  view-transition-name: hero-image;
}
```

```javascript
document.startViewTransition(() => {
  router.navigate("/article/42");
});
```

If both the list view and the detail view have an element with `view-transition-name: hero-image`, the browser animates the image from its list position to its detail position. The thumbnail grows into the hero image. Smoothly. On the GPU. No JavaScript position calculation.

We will use this pattern heavily in Day 11 for client-side routing.

### Customising the transition

By default the transition is a crossfade. To customise, target the pseudo-elements the browser generates:

```css
::view-transition-old(root) {
  animation: fadeOut 300ms both;
}
::view-transition-new(root) {
  animation: slideIn 300ms both;
}

@keyframes fadeOut {
  to { opacity: 0; }
}
@keyframes slideIn {
  from { transform: translateX(100%); }
}
```

The `(root)` represents the whole page; `(hero-image)` would target the named shared element; `(*)` matches everything. You have full CSS animation control.

### Cross-document view transitions

The big one. Multi-page applications — plain HTML navigating to plain HTML — can now get SPA-like transitions with zero JavaScript:

```css
/* On both pages: */
@view-transition {
  navigation: auto;
}
```

Two lines of CSS. Every navigation within the same origin animates with a default crossfade. Add named transitions for shared elements (the article thumbnail morphs into the article hero), and you have a Pinterest-quality experience with a plain-HTML architecture.

[Cross-document view transitions are in Chrome 126+, Safari 18.2+, and Firefox 146+ as of early 2026](https://caniuse.com/css-view-transitions). Not yet Baseline, but close, and degrading gracefully (non-supporting browsers just navigate normally).

We will build a full cross-document view transition in Day 11. For now, know that this feature exists and that for a blog or content site it can eliminate 90% of the reason you reach for an SPA framework.

### `prefers-reduced-motion` and view transitions

View Transitions respect `prefers-reduced-motion` automatically. Users with the preference set see an instant snap instead of the animation. You do not have to code this behaviour.

## Part 6: Scroll-Driven Animations

A newer feature that deserves mention: **scroll-driven animations** let CSS animations be tied to scroll position instead of time. [Baseline Newly Available since mid-2024](https://developer.chrome.com/blog/scroll-driven-animations) in Chrome and Safari; Firefox shipped in 2025 and 2026 catches up.

A "reading progress" bar at the top of an article, in pure CSS:

```css
.progress {
  position: fixed;
  top: 0;
  left: 0;
  width: 0;
  height: 4px;
  background: var(--brand);
  animation: progress linear;
  animation-timeline: scroll();
}

@keyframes progress {
  to { width: 100%; }
}
```

That is it. The bar grows as the user scrolls. No `window.scroll` event listeners. No JavaScript. No libraries. Pure CSS, composited on the GPU, buttery smooth.

Or a "fade in on scroll" pattern:

```css
.fade-in {
  animation: fadeIn linear;
  animation-timeline: view();
  animation-range: entry 0% entry 100%;
  opacity: 0;
}

@keyframes fadeIn {
  to { opacity: 1; }
}
```

The animation plays as the element scrolls into view. No IntersectionObserver. No JavaScript scroll handlers.

Support is not yet everywhere — Firefox was the last holdout and is still catching up to Chrome and Safari's implementations. Use `@supports (animation-timeline: scroll())` to guard:

```css
@supports (animation-timeline: scroll()) {
  .progress { /* ... */ }
}
```

Non-supporting browsers just skip the animation, which is usually fine.

## Part 7: Putting It All Together — A Complete Design System

We now have enough to build a complete design system in plain CSS. Here is the annotated capstone version.

**`tokens.css`** (the complete token layer, in a single file):

```css
:root {
  color-scheme: light dark;

  /* --- Colour system --- */

  /* Brand hue — change this one number to retheme the site */
  --brand-h: 250;
  --brand-c: 0.15;

  /* Primary brand colour */
  --brand: oklch(55% var(--brand-c) var(--brand-h));
  --brand-hover: oklch(from var(--brand) calc(l - 7%) c h);
  --brand-muted: oklch(from var(--brand) 94% 0.03 h);
  --brand-subtle: color-mix(in oklch, var(--brand) 10%, transparent);

  /* Semantic colours */
  --success: oklch(60% 0.15 145);
  --warning: oklch(75% 0.15 85);
  --danger:  oklch(55% 0.2  25);
  --info:    oklch(65% 0.12 230);

  /* Surfaces — light-dark() auto-switches */
  --bg:         light-dark(oklch(99% 0 0), oklch(12% 0 0));
  --bg-subtle:  light-dark(oklch(96% 0 0), oklch(15% 0 0));
  --surface:    light-dark(oklch(100% 0 0), oklch(16% 0 0));
  --surface-raised: light-dark(oklch(100% 0 0), oklch(20% 0 0));

  --text:         light-dark(oklch(15% 0 0), oklch(95% 0 0));
  --text-muted:   light-dark(oklch(40% 0 0), oklch(70% 0 0));
  --text-subtle:  light-dark(oklch(55% 0 0), oklch(55% 0 0));

  --border:        light-dark(oklch(90% 0 0), oklch(25% 0 0));
  --border-strong: light-dark(oklch(75% 0 0), oklch(40% 0 0));

  /* --- Type — fluid scale from Utopia.fyi --- */
  --font-ui: "Inter", system-ui, -apple-system, "Segoe UI Variable", sans-serif;
  --font-serif: "Source Serif Variable", Georgia, serif;
  --font-mono: "JetBrains Mono", ui-monospace, "SF Mono", Menlo, monospace;

  --step--2: clamp(0.6944rem, 0.6693rem + 0.1256vw, 0.7656rem);
  --step--1: clamp(0.8333rem, 0.7876rem + 0.2285vw, 0.9688rem);
  --step-0:  clamp(1rem,      0.9286rem + 0.3571vw, 1.25rem);
  --step-1:  clamp(1.2rem,    1.0857rem + 0.5714vw, 1.5625rem);
  --step-2:  clamp(1.44rem,   1.2629rem + 0.8857vw, 1.9531rem);
  --step-3:  clamp(1.728rem,  1.4571rem + 1.3543vw, 2.4414rem);
  --step-4:  clamp(2.0736rem, 1.6714rem + 2.0114vw, 3.0518rem);
  --step-5:  clamp(2.4883rem, 1.9086rem + 2.8986vw, 3.8147rem);

  /* --- Space — fluid scale --- */
  --space-3xs: clamp(0.25rem, 0.2143rem + 0.1786vw, 0.375rem);
  --space-2xs: clamp(0.5rem,  0.4286rem + 0.3571vw, 0.75rem);
  --space-xs:  clamp(0.75rem, 0.6429rem + 0.5357vw, 1.125rem);
  --space-s:   clamp(1rem,    0.8571rem + 0.7143vw, 1.5rem);
  --space-m:   clamp(1.5rem,  1.2857rem + 1.0714vw, 2.25rem);
  --space-l:   clamp(2rem,    1.7143rem + 1.4286vw, 3rem);
  --space-xl:  clamp(3rem,    2.5714rem + 2.1429vw, 4.5rem);
  --space-2xl: clamp(4rem,    3.4286rem + 2.8571vw, 6rem);

  /* --- Shape --- */
  --radius-xs:   0.125rem;
  --radius-sm:   0.25rem;
  --radius-md:   0.375rem;
  --radius-lg:   0.5rem;
  --radius-xl:   0.75rem;
  --radius-full: 9999px;

  /* --- Elevation --- */
  --shadow-sm: 0 1px 2px oklch(0% 0 0 / 0.05);
  --shadow-md: 0 4px 8px oklch(0% 0 0 / 0.08), 0 1px 2px oklch(0% 0 0 / 0.06);
  --shadow-lg: 0 16px 32px oklch(0% 0 0 / 0.12), 0 4px 8px oklch(0% 0 0 / 0.08);

  /* --- Motion --- */
  --duration-fast: 150ms;
  --duration-base: 250ms;
  --duration-slow: 400ms;

  --ease-in:     cubic-bezier(0.4, 0, 1, 1);
  --ease-out:    cubic-bezier(0, 0, 0.2, 1);
  --ease-in-out: cubic-bezier(0.4, 0, 0.2, 1);

  /* --- Focus --- */
  --focus-ring: var(--brand);
  --focus-ring-width: 2px;
  --focus-ring-offset: 2px;
}

/* User overrides */
:root[data-theme="light"] { color-scheme: light; }
:root[data-theme="dark"]  { color-scheme: dark; }
```

That is approximately 80 lines. It is the complete design vocabulary for a mid-sized application. Change `--brand-h` to re-theme. Change the type scale inputs and regenerate. Add a new semantic colour token and use it everywhere.

## Part 8: Anti-Patterns To Retire

Five things we saw a lot of before 2024 that are not needed now:

**1. JavaScript theme libraries.** If you are still importing `next-themes` or similar, replace with 15 lines of vanilla JS (the toggle we showed in Part 2) plus `light-dark()` tokens. You will ship less code and have fewer bugs.

**2. Separate CSS for each theme.** Two stylesheets — `light.css` and `dark.css` — with different token values, selectively loaded. Now one stylesheet with `light-dark()` does both.

**3. Static font files for every weight.** If you are loading `font-400.woff2`, `font-500.woff2`, `font-600.woff2`, `font-700.woff2` — stop. Load one variable font. Smaller total download, more flexibility.

**4. JavaScript for "shared element transitions."** If you are still using Framer Motion's `layoutId` or GSAP's FLIP for morphing animations, learn View Transitions. Orders of magnitude less code.

**5. Hex colour as the primary token.** If your `tokens.css` starts with `--blue-500: #3b82f6`, migrate to `oklch()`. The first time you need to derive a colour from a brand colour, you will thank us.

## Part 9: Testing Your Colour System

Three things to check:

1. **Contrast.** For any text colour on any background, the ratio should be 4.5:1 minimum for body text (WCAG AA), 3:1 minimum for large text. Use DevTools' contrast checker — in the colour picker, the contrast ratio against the current background is shown automatically. `contrast-color()` handles many cases, but for content with rich colour you still need to check.

2. **Colour blindness.** DevTools has a colour-blindness simulation mode under the "Rendering" tab. Test your UI for deuteranopia (red-green), protanopia (red-green), and tritanopia (blue-yellow). Do not rely on colour alone to communicate state.

3. **Dark mode.** Actually test both themes, ideally on real hardware. It is stunningly easy to ship a "dark mode" that was never tested on a real dark-mode device, and the results are often bad — subtle contrast issues, image fringing, shadow invisibility.

## Part 10: Tomorrow

Tomorrow — **Day 7: Native ES Modules — Import Maps, Dynamic Import, and the Death of the Bundler** — we leave CSS entirely and switch to JavaScript. The first four parts of our series were design-and-layout; Day 7 starts the software architecture. We will cover why `<script type="module">` is the only bundler you need, import maps for dependency management without npm, dynamic imports for code splitting without Webpack, and the no-build development loop that has made this whole series possible.

See you tomorrow.

---

## Series navigation

You are reading **Part 6 of 15**.

- [Part 1: Overview — Why a Plain Browser Is Enough in 2026](/blog/2026-05-24-no-build-web-overview)
- [Part 2: Semantic HTML and the Document Outline Nobody Taught You](/blog/2026-05-25-no-build-web-html-semantics)
- [Part 3: The Cascade, Specificity, and `@layer`](/blog/2026-05-26-no-build-web-css-cascade-layers)
- [Part 4: Modern CSS Layout — Flexbox, Grid, Subgrid, and Container Queries](/blog/2026-05-27-no-build-web-modern-css-layout)
- [Part 5: Responsive Design in 2026](/blog/2026-05-28-no-build-web-responsive-design)
- **Part 6 (today): Colour, Typography, and Motion — `oklch`, `light-dark()`, Variable Fonts, and View Transitions**
- Part 7 (tomorrow): Native ES Modules
- Part 8: The DOM, Events, and Platform Primitives
- Part 9: State Management Without a Library
- Part 10: Web Components
- Part 11: Client-Side Routing with the Navigation API and View Transitions
- Part 12: Forms, Validation, and the Constraint Validation API
- Part 13: Storage, Service Workers, and Offline
- Part 14: Accessibility, Performance, and Security
- Part 15: The Conclusion — A Complete Application End to End

## Resources

- [MDN: `oklch()`](https://developer.mozilla.org/en-US/docs/Web/CSS/Reference/Values/color_value/oklch) — reference.
- [MDN: `light-dark()`](https://developer.mozilla.org/en-US/docs/Web/CSS/Reference/Values/color_value/light-dark) — reference.
- [MDN: `color-mix()`](https://developer.mozilla.org/en-US/docs/Web/CSS/Reference/Values/color_value/color-mix) — reference.
- [MDN: Variable fonts guide](https://developer.mozilla.org/en-US/docs/Web/CSS/Guides/Fonts/Variable_fonts) — comprehensive reference.
- [MDN: View Transitions API](https://developer.mozilla.org/en-US/docs/Web/API/View_Transitions_API) — reference and examples.
- [Vadim Makeev, *Native HTML light and dark color scheme switching*](https://pepelsbey.dev/articles/native-light-dark/) — excellent deep dive on `light-dark()` and related HTML features.
- [web.dev: Cross-document view transitions](https://developer.chrome.com/docs/web-platform/view-transitions/cross-document) — Chrome's definitive guide.
- [Utopia.fyi](https://utopia.fyi/) — fluid type and space scale calculator.
- [v-fonts.com](https://v-fonts.com/) — catalogue of free variable fonts.
- [Wakamai Fondue](https://wakamaifondue.com/) — inspect what a variable font can do.
- [OKLCH Color Picker](https://oklch.com/) — visual colour picker for OKLCH.
- [Adam Argyle, *Conic gradient with relative color syntax*](https://nerdy.dev/) — real-world relative colour examples.
- [Josh W. Comeau, *An Interactive Guide to CSS Transitions*](https://www.joshwcomeau.com/animation/css-transitions/) — great primer on easing.
- [Can I Use: variable fonts](https://caniuse.com/variable-fonts) — current browser support.
- [Chrome DevTools contrast checker docs](https://developer.chrome.com/docs/devtools/accessibility/contrast) — how to use the built-in accessibility audit.
