---
title: "HTML, JavaScript, and CSS from the Ground Up: Building Complete Web Applications Without Any External Packages, From First Principles, for the Modern Web in 2026"
date: 2026-05-03
author: myblazor-team
summary: "An exhaustive, from-first-principles guide to building complete web applications using nothing but what evergreen browsers provide — no npm, no build step, no TypeScript, no external dependencies of any kind. Covers semantic HTML, modern CSS (container queries, nesting, :has(), layers, anchor positioning, view transitions), vanilla JavaScript (ES modules, fetch, Web Components, Shadow DOM, Custom Elements), Content Security Policy, accessibility, performance, and the full architecture of a production application — all with working code examples you can copy into a text file and open in a browser."
tags:
  - html
  - css
  - javascript
  - web-development
  - first-principles
  - deep-dive
  - best-practices
  - security
  - accessibility
  - web-components
  - no-dependencies
featured: true
---

## Part 1 — Why We Are Here and What We Are Going to Do

You are reading this because something has gone wrong.

Not with you personally — although we will get to that. Something has gone wrong with the way most people learn web development in 2026. They learn React before they learn what the DOM is. They install `create-react-app` before they understand that a web page is a text file. They reach for npm before they can explain what a `<div>` actually does. They configure webpack before they have ever written a `<style>` tag. They pull in 847 transitive dependencies to display "Hello, World" and then wonder why their application takes fourteen seconds to build and ships 2.3 megabytes of JavaScript to the browser.

This article exists to fix that. We are going to build web applications — real, complete, functional web applications — using nothing but a text editor and a web browser. No npm. No yarn. No pnpm. No bun. No Node.js on the development machine at all. No TypeScript. No Sass. No PostCSS. No webpack, Vite, esbuild, Rollup, Parcel, Turbopack, or any other bundler. No React, Vue, Angular, Svelte, Solid, Qwik, Astro, Next.js, Nuxt, SvelteKit, or any other framework. No build step of any kind. You write files. You open them in a browser. They work.

"But that is impossible," you say. "You cannot build anything real without a framework."

You are wrong, and I say that with respect and warmth, because I used to believe the same thing. The web platform in 2026 is unrecognizably powerful compared to what it was even five years ago. CSS has container queries, nesting, `:has()`, cascade layers, anchor positioning, scroll-driven animations, and view transitions. JavaScript has native ES modules, `fetch`, the `structuredClone` function, the `Temporal` API (in some browsers), `import.meta`, top-level `await`, Web Components with Shadow DOM, the Popover API, and the `<dialog>` element. HTML has `<details>`, `<summary>`, the `loading="lazy"` attribute, `inputmode`, the `inert` attribute, and declarative Shadow DOM.

You do not need a framework. You need to understand the platform.

### Who This Article Is For

This article is for the ASP.NET developer who has been building web applications for years but has always let Visual Studio, or npm, or some template do the frontend work. You know C#. You know how a request lifecycle works. You have opinions about dependency injection. But if someone took away your `<PackageReference>` tags and your `_Layout.cshtml` and said "build me a frontend from scratch," you would stare at the screen and slowly die inside.

This article is also for the developer who has been drowning in npm for a decade and has forgotten — or never knew — that the browser itself is an extraordinarily capable application runtime. You have been so busy configuring build tools that you forgot to learn what the build tools were abstracting over.

We are going to start from the absolute beginning. We will explain what HTML is. We will explain what CSS is. We will explain what JavaScript is. Then we will build things with them. Real things. A complete single-page application with routing, state management, Web Components, a locked-down Content Security Policy, responsive layouts, dark mode, form validation, offline support with service workers, and accessible, semantic markup — all without installing a single package.

### The Rules

Let us be explicit about the constraints. These are not suggestions. They are laws.

**Rule 1: No external packages.** Nothing from npm, nothing from a CDN, nothing from anywhere. Every line of code in our application will be written by us or provided by the browser. If you cannot accomplish something without pulling in someone else's code, then you cannot accomplish it. (You will be surprised how rarely this happens.)

**Rule 2: No build step.** There is no compiler, no transpiler, no bundler, no minifier between your source files and the browser. You write a `.html` file, a `.css` file, and a `.js` file. You open the `.html` file in a browser. That is the deployment process. If you want to deploy to a server, you copy the files to the server. That is the deployment process for production.

**Rule 3: No TypeScript.** TypeScript is a fine language. It solves real problems. But it requires a build step, which violates Rule 2. We write JavaScript. Vanilla JavaScript. Modern JavaScript — but JavaScript.

**Rule 4: No CSS preprocessors.** No Sass, no Less, no Stylus, no PostCSS. Modern CSS is powerful enough that preprocessors provide little value. We write CSS. Plain CSS. Modern CSS — but CSS.

**Rule 5: Evergreen browsers only.** We target the current stable versions of Google Chrome, Mozilla Firefox, and Apple Safari. If a feature is not available in all three, we do not use it. We do not write polyfills. We do not use vendor prefixes. If something requires `-webkit-` or `-moz-`, that feature does not exist for us. We will occasionally show vendor-prefixed code as an example of what *not* to do.

**Rule 6: No external code execution on the user's machine.** We may use `fetch` to communicate with APIs. We may load images and fonts. But no external JavaScript executes in our application. No analytics scripts. No tracking pixels. No third-party widgets. No CDN-hosted libraries. Every script that runs is a script we wrote, served from our origin. This is not just a preference — it is a security posture that enables a strict Content Security Policy.

### Why This Matters

Let me tell you a story about a Thursday afternoon. It is 3:47 PM. Your deploy pipeline has been running for nine minutes. It is stuck on `npm install`. It has been stuck on `npm install` for four minutes because there is a network timeout talking to the npm registry. When it eventually finishes (twelve minutes in), it proceeds to `npm run build`, which invokes webpack, which invokes Babel, which invokes PostCSS, which invokes Terser, which invokes a source map generator. This takes another six minutes. Your total build time is eighteen minutes for a blog. A *blog*.

Now imagine this: you push a markdown file to a git repository. A GitHub Action runs for ninety seconds (most of which is the .NET content processor, not the frontend). Your blog is live. The frontend is HTML, CSS, and JavaScript files that were written by hand. There is no build step for the frontend because there is nothing to build. The files *are* the application.

This is not a hypothetical. This is how we build My Blazor Magazine. The blog engine you are reading right now compiles Markdown to HTML at build time and serves it as static files. The frontend has zero npm dependencies. The CSS is hand-written. The syntax highlighter is hand-written. The theme switcher is hand-written. The audio player is hand-written. Nothing is minified. Nothing is bundled. It loads fast because it is small, not because a build tool made it small.

There is a deeper reason this matters beyond build times, though. **You are shipping code you do not understand.** When you install React, you are shipping 46 kilobytes of minified, gzipped JavaScript that your users must download and parse before anything appears on screen. Do you know what is in that 46 kilobytes? Do you know how the virtual DOM diffing algorithm works? Do you know what `__SECRET_INTERNALS_DO_NOT_USE_OR_YOU_WILL_BE_FIRED` is? (That is a real export from the React package.) You are running code on your users' machines that you cannot explain. As a profession, we should be uncomfortable with that.

When you install a package from npm, you are trusting every maintainer of every transitive dependency in the entire tree. The `event-stream` incident of 2018, where a malicious actor gained access to a popular npm package and inserted cryptocurrency-stealing malware, was not an anomaly. It was an inevitability of a system where a "Hello World" React application has over 1,400 transitive dependencies. Every dependency is an attack surface. Zero dependencies means zero supply chain risk.

This is not Luddism. This is engineering.

### How This Article Is Structured

We will proceed in parts, building on each layer:

- **Part 2** covers HTML from first principles — the document, the DOM, semantic elements, forms, and accessibility.
- **Part 3** covers CSS from first principles — selectors, the box model, layout (flexbox, grid), custom properties, modern features (nesting, `:has()`, container queries, layers, view transitions).
- **Part 4** covers JavaScript from first principles — the language, the DOM API, events, ES modules, `fetch`, `async`/`await`, and error handling.
- **Part 5** covers Web Components — Custom Elements, Shadow DOM, templates, slots, and the lifecycle.
- **Part 6** covers building a real application — routing, state management, data flow, and architecture without a framework.
- **Part 7** covers Content Security Policy — what it is, why it matters, and how to write the strictest possible policy.
- **Part 8** covers accessibility — ARIA, keyboard navigation, screen readers, focus management, and semantic HTML.
- **Part 9** covers performance — loading strategies, caching, service workers, and the critical rendering path.
- **Part 10** covers testing — how to test vanilla JavaScript applications without Jest, without Vitest, without any framework.
- **Part 11** covers deployment — static hosting, GitHub Pages, caching headers, and production readiness.
- **Part 12** covers what is coming next — the features that are not yet in all three browsers but will be soon.

Let us begin.

---

## Part 2 — HTML: The Document

### What Is HTML?

HTML stands for HyperText Markup Language. It is a markup language, not a programming language. This distinction matters. A programming language tells a computer *what to do*. A markup language tells a computer *what something is*. When you write `<p>Hello</p>`, you are not telling the browser to "print Hello." You are telling the browser "this is a paragraph, and its content is Hello." The browser decides how to render a paragraph. You describe the structure. The browser handles the presentation.

This is the most important concept in web development and the one that beginners (and many experienced developers) get wrong most consistently. HTML is about **meaning**, not appearance. A `<strong>` tag does not mean "make this bold." It means "this content has strong importance." The fact that browsers typically render `<strong>` as bold text is a default stylesheet choice, not a semantic guarantee. You could write CSS that makes `<strong>` italic and red, and the HTML would still be semantically correct.

### The Minimum Viable Document

Here is the absolute minimum HTML document that is valid and complete:

```html
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="utf-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>My Page</title>
</head>
<body>
    <p>Hello, world.</p>
</body>
</html>
```

Let us explain every single line.

**`<!DOCTYPE html>`** — This is the document type declaration. It tells the browser "this is an HTML5 document." Without it, the browser falls into "quirks mode," where it emulates bugs from the Internet Explorer era for backward compatibility. You never want quirks mode. Always include the doctype. It must be the first thing in the file, before any whitespace.

**`<html lang="en">`** — This is the root element of the document. The `lang` attribute tells the browser (and screen readers, and search engines) that the content is in English. If your content is in Nepali, you would write `lang="ne"`. If your content is in French, `lang="fr"`. This attribute is not decorative. Screen readers use it to select the correct pronunciation rules. Translation tools use it to determine the source language. Always include it.

**`<head>`** — The head section contains metadata about the document. Nothing in the head is visible on the page. It contains information *about* the page: its character encoding, its viewport configuration, its title, links to stylesheets, and so on.

**`<meta charset="utf-8">`** — This declares the character encoding of the document. UTF-8 is the universal character encoding that supports every writing system on Earth — English, Nepali, Chinese, Arabic, emoji, everything. Always use UTF-8. Always declare it. If you omit this, the browser will guess the encoding, and it will sometimes guess wrong, and your users will see garbled text. This must appear within the first 1024 bytes of the document, so put it first in the head.

**`<meta name="viewport" content="width=device-width, initial-scale=1.0">`** — This is the viewport meta tag. Without it, mobile browsers will render your page at a desktop width (typically 980 pixels) and then zoom out so the entire page fits on the screen, making everything tiny and unreadable. This tag tells the browser "the width of the viewport should match the width of the device, and the initial zoom level should be 1." Always include it. Omitting it is one of the most common reasons a page "does not look right on mobile."

Note what is *not* in that viewport tag: `maximum-scale=1.0` or `user-scalable=no`. Those attributes prevent users from pinching to zoom. Do not use them. Some users need to zoom in — users with low vision, users with motor impairments who need to tap larger targets, users who just want to read your tiny font. Preventing zoom is an accessibility violation. Never do it.

**`<title>My Page</title>`** — The title element sets the text that appears in the browser tab, in bookmarks, in search engine results, and in screen reader announcements when the page loads. Every page must have a title. It should be descriptive and unique per page. "My Page" is a terrible title. "Home — My Blazor Magazine" is a good title. "Product Details: Widget Pro 3000 — My Store" is a good title.

**`<body>`** — The body section contains everything visible on the page. All your content goes here.

### Semantic HTML: The Elements That Matter

HTML has over 100 elements. Most developers use about eight of them: `<div>`, `<span>`, `<a>`, `<img>`, `<p>`, `<h1>`, `<ul>`, and `<li>`. This is like owning a fully equipped kitchen and only ever using the microwave.

Here are the elements that actually matter for building modern web applications. We will group them by purpose.

#### Document Structure

**`<header>`** — A header for a section or the page. Not to be confused with `<head>`. A `<header>` typically contains a logo, navigation, and perhaps a search bar. You can have multiple headers on a page — one for the site and one for each article, for example.

**`<nav>`** — A navigation section. This tells screen readers "here are the links for getting around the site." A screen reader user can jump directly to the `<nav>` to find links, just as a sighted user scans for the menu bar. If your navigation is a list of links, wrap it in `<nav>`.

**`<main>`** — The main content of the page. There should be exactly one `<main>` element per page. Screen readers use it to skip past the header and navigation and go straight to the content. This is critically important for users who navigate by landmarks.

**`<article>`** — A self-contained piece of content that could be independently distributed. A blog post is an article. A forum comment is an article. A product card in a search results page could be an article. The test is: if you pulled this content out of the page and put it somewhere else, would it still make sense?

**`<section>`** — A thematic grouping of content. Sections should typically have a heading. If you cannot think of a heading for the section, it probably should be a `<div>` instead.

**`<aside>`** — Content tangentially related to the main content. A sidebar, a pull quote, a "related articles" widget. Screen readers announce this as a separate region so users know the content is supplementary.

**`<footer>`** — A footer for a section or the page. Typically contains copyright notices, links to terms of service, and contact information.

#### The Div Problem

Now let us talk about `<div>`. The `<div>` element is a generic container with no semantic meaning. It is the most overused element in web development. Here is code that I see constantly:

```html
<!-- BAD — "div soup" -->
<div class="header">
    <div class="nav">
        <div class="nav-item"><a href="/">Home</a></div>
        <div class="nav-item"><a href="/about">About</a></div>
    </div>
</div>
<div class="main">
    <div class="article">
        <div class="title">My Post</div>
        <div class="content">
            <div class="paragraph">This is some text.</div>
        </div>
    </div>
</div>
<div class="footer">
    <div class="copyright">&copy; 2026</div>
</div>
```

This is semantically identical to writing an entire book with no chapter headings, no paragraphs, and no formatting — just one continuous stream of words with handwritten notes in the margins saying "this part is a chapter heading" and "this part is a paragraph."

Here is the same content written correctly:

```html
<!-- GOOD — semantic HTML -->
<header>
    <nav>
        <ul>
            <li><a href="/">Home</a></li>
            <li><a href="/about">About</a></li>
        </ul>
    </nav>
</header>
<main>
    <article>
        <h1>My Post</h1>
        <p>This is some text.</p>
    </article>
</main>
<footer>
    <p>&copy; 2026</p>
</footer>
```

The second version is shorter, clearer, more accessible, better for SEO, and easier to style with CSS. A screen reader user navigating the first version hears "group, group, link, Home, group, link, About" — meaningless noise. A screen reader user navigating the second version hears "banner landmark, navigation landmark, list, 2 items, link Home, link About" — useful information.

**Use `<div>` only when no semantic element fits.** If you need a container purely for styling purposes — to create a grid layout, for example — then `<div>` is appropriate. For everything else, there is a better element.

### Headings: The Outline

HTML has six heading levels: `<h1>` through `<h6>`. They create a document outline. `<h1>` is the most important heading (typically the page title), and `<h6>` is the least important.

The rules:

1. **Every page should have exactly one `<h1>`.** This is the main heading of the page.
2. **Do not skip heading levels.** Do not go from `<h2>` to `<h4>`. Screen readers and outline algorithms depend on the hierarchy being continuous.
3. **Do not use headings for visual sizing.** If you want text to be large but it is not a heading, use CSS. Headings convey structure, not size.

```html
<!-- BAD — skips h2, uses h3 for visual effect -->
<h1>My Blog</h1>
<h3>Latest Post</h3>  <!-- Wrong: should be h2 -->
<h5>Posted yesterday</h5>  <!-- Wrong: skips h4, and this is not a heading -->

<!-- GOOD — proper hierarchy -->
<h1>My Blog</h1>
<h2>Latest Post</h2>
<p><time datetime="2026-05-02">Posted yesterday</time></p>
```

### Text Content

**`<p>`** — A paragraph. The fundamental unit of text content. Use it for every block of text.

**`<strong>`** — Strong importance. Rendered bold by default. Use it when the content is *genuinely important*, not just when you want bold text.

**`<em>`** — Emphasis. Rendered italic by default. Use it when you are *stressing* a word, as in "I did *not* say that."

**`<code>`** — Inline code. Use it for variable names, function names, file names, and short code snippets.

**`<pre>`** — Preformatted text. Preserves whitespace and line breaks. Use it for code blocks, ASCII art, and anything where whitespace matters. Always put `<code>` inside `<pre>` for code blocks:

```html
<pre><code>function greet(name) {
    return `Hello, ${name}!`;
}</code></pre>
```

**`<blockquote>`** — A block quotation from another source. Use the `cite` attribute to link to the source:

```html
<blockquote cite="https://www.w3.org/Style/CSS/">
    <p>CSS is a language for writing style sheets, and is designed to
    describe the rendering of structured documents.</p>
</blockquote>
```

**`<time>`** — A machine-readable date or time. The `datetime` attribute provides the machine-readable value:

```html
<time datetime="2026-05-03">May 3, 2026</time>
<time datetime="14:30">2:30 PM</time>
<time datetime="PT2H30M">2 hours and 30 minutes</time>
```

This element is enormously useful for search engines, screen readers, and any tool that needs to understand dates in your content.

**`<mark>`** — Highlighted text. Use it for search results, for example:

```html
<p>The search term <mark>web components</mark> was found 3 times.</p>
```

**`<abbr>`** — An abbreviation or acronym. Use the `title` attribute to provide the expansion:

```html
<p>This application uses <abbr title="Content Security Policy">CSP</abbr>
to prevent cross-site scripting.</p>
```

### Lists

There are three kinds of lists in HTML.

**`<ul>`** — An unordered list. Use it when the order of items does not matter (a list of features, a navigation menu).

**`<ol>`** — An ordered list. Use it when the order matters (steps in a procedure, a ranking).

**`<dl>`** — A description list (formerly called a "definition list"). Use it for key-value pairs (a glossary, metadata, a list of terms and definitions):

```html
<dl>
    <dt>HTML</dt>
    <dd>HyperText Markup Language — the standard markup language for web pages.</dd>

    <dt>CSS</dt>
    <dd>Cascading Style Sheets — a style sheet language for describing the presentation of a document.</dd>

    <dt>JavaScript</dt>
    <dd>A programming language that enables interactive web pages.</dd>
</dl>
```

The `<dl>` element is criminally underused. It is perfect for product specifications, FAQs, contact information, and metadata displays.

### Links

The `<a>` element creates a hyperlink. It is the most important element on the web — the "hyper" in "hypertext."

```html
<a href="https://example.com">Visit Example</a>
```

Key attributes:

- **`href`** — The URL to navigate to. Can be absolute (`https://...`), relative (`/about`), or a fragment identifier (`#section-name`).
- **`target="_blank"`** — Opens the link in a new tab. **Always** pair this with `rel="noopener noreferrer"` to prevent the opened page from accessing `window.opener` (a security vulnerability).
- **`rel`** — Specifies the relationship between the current document and the linked document. Values include `noopener` (prevent `window.opener` access), `noreferrer` (do not send the Referer header), `nofollow` (tell search engines not to follow this link), and `external` (the link goes to a different site).
- **`download`** — Tells the browser to download the resource instead of navigating to it.

```html
<!-- External link — always use noopener noreferrer -->
<a href="https://example.com"
   target="_blank"
   rel="noopener noreferrer">
    Visit Example ↗
</a>

<!-- Download link -->
<a href="/files/report.pdf" download="annual-report-2026.pdf">
    Download Annual Report (PDF)
</a>

<!-- Fragment link (scrolls to an element with id="features") -->
<a href="#features">Jump to Features</a>
```

**Do not use `<a>` as a button.** If clicking something performs an action (submitting a form, opening a dialog, toggling a menu) rather than navigating to a URL, use a `<button>`. Links navigate. Buttons act. This distinction matters for accessibility — screen readers announce them differently, and keyboard users expect different behavior (Enter activates a link, Enter or Space activates a button).

### Images

The `<img>` element embeds an image.

```html
<img src="photo.jpg"
     alt="A golden retriever sitting in a field of wildflowers"
     width="800"
     height="600"
     loading="lazy">
```

Key attributes:

- **`src`** — The URL of the image.
- **`alt`** — Alternative text describing the image. This is **mandatory** (with one exception). Screen readers read the alt text aloud. If the image fails to load, the alt text is displayed. Search engines use it to understand the image. Write alt text as if you are describing the image to someone on the phone. If the image is purely decorative and conveys no information, use an empty alt attribute: `alt=""`. This tells screen readers to skip it entirely.
- **`width`** and **`height`** — The intrinsic dimensions of the image in pixels. Including these prevents layout shift — the browser can reserve the correct amount of space before the image loads, so the page does not jump around.
- **`loading="lazy"`** — Tells the browser to defer loading the image until it is near the viewport. This saves bandwidth and speeds up the initial page load. Do **not** use `loading="lazy"` on images that are visible in the initial viewport (above the fold) — those should load immediately.

For responsive images, use the `<picture>` element and `srcset`:

```html
<picture>
    <source srcset="photo-large.webp" media="(min-width: 800px)" type="image/webp">
    <source srcset="photo-small.webp" type="image/webp">
    <source srcset="photo-large.jpg" media="(min-width: 800px)" type="image/jpeg">
    <img src="photo-small.jpg"
         alt="A golden retriever sitting in a field of wildflowers"
         width="800"
         height="600"
         loading="lazy">
</picture>
```

This gives the browser choices: if the viewport is wide and the browser supports WebP, it loads the large WebP. If the viewport is narrow and the browser supports WebP, it loads the small WebP. Otherwise, it falls back to JPEG. The `<img>` at the end is the fallback for browsers that do not support `<picture>`.

### Forms

Forms are how users send data to a server (or, in a single-page application, to JavaScript). They are also one of the most accessibility-rich parts of HTML.

```html
<form action="/api/contact" method="post">
    <div>
        <label for="name">Full Name</label>
        <input type="text" id="name" name="name" required
               autocomplete="name"
               placeholder="Jane Doe">
    </div>

    <div>
        <label for="email">Email Address</label>
        <input type="email" id="email" name="email" required
               autocomplete="email"
               placeholder="jane@example.com">
    </div>

    <div>
        <label for="message">Message</label>
        <textarea id="message" name="message" required
                  rows="5"
                  placeholder="What's on your mind?"></textarea>
    </div>

    <button type="submit">Send Message</button>
</form>
```

Critical rules for forms:

1. **Every input must have a `<label>`.** The `for` attribute on the label must match the `id` attribute on the input. This creates an accessible association — a screen reader user hearing "Full Name, edit text" knows exactly what the field is for. Clicking the label also focuses the input, which improves usability for everyone.
2. **Use the correct `type` attribute.** `type="email"` gives you email validation for free and shows an email-optimized keyboard on mobile. `type="tel"` shows a phone number keyboard. `type="url"` validates URLs. `type="number"` shows a number input with increment/decrement buttons. `type="date"` shows a native date picker. Use them.
3. **Use `autocomplete` attributes.** These tell the browser what kind of data the field expects, enabling autofill. `autocomplete="name"` for full names, `autocomplete="email"` for email addresses, `autocomplete="tel"` for phone numbers, `autocomplete="street-address"` for street addresses, and so on. Autofill saves users enormous amounts of time and reduces errors.
4. **Use `required` for required fields.** The browser will prevent form submission if a required field is empty, with a built-in error message. No JavaScript needed.
5. **Use `pattern` for custom validation.** `pattern="[0-9]{5}"` requires exactly five digits (a US ZIP code, for example). The browser handles the validation.
6. **Use `placeholder` sparingly.** Placeholders disappear when the user starts typing, which means users lose the hint. Placeholders are not a replacement for labels. They are supplementary hints.

### The Dialog Element

The `<dialog>` element is a native modal dialog. Before it existed, developers had to build modal dialogs from scratch — managing focus trapping, backdrop overlays, escape key handling, and scroll locking, all in JavaScript. The `<dialog>` element does all of this natively:

```html
<dialog id="confirm-dialog">
    <h2>Confirm Delete</h2>
    <p>Are you sure you want to delete this item? This cannot be undone.</p>
    <form method="dialog">
        <button value="cancel">Cancel</button>
        <button value="confirm">Delete</button>
    </form>
</dialog>

<button onclick="document.getElementById('confirm-dialog').showModal()">
    Delete Item
</button>
```

When you call `.showModal()`, the browser:
- Centers the dialog on screen
- Adds a backdrop (the semi-transparent overlay behind the dialog)
- Traps focus inside the dialog (Tab and Shift+Tab cycle through the dialog's focusable elements)
- Closes the dialog when the user presses Escape
- Returns focus to the element that opened the dialog when it closes
- Marks the rest of the page as `inert` (not focusable, not clickable)

All of this is free. No JavaScript library required. The `<form method="dialog">` inside the dialog is a special form that, when submitted, closes the dialog and sets `dialog.returnValue` to the value of the button that was clicked.

You can style the backdrop with the `::backdrop` pseudo-element:

```css
dialog::backdrop {
    background: rgba(0, 0, 0, 0.5);
    backdrop-filter: blur(4px);
}
```

### The Details and Summary Elements

The `<details>` and `<summary>` elements create a native disclosure widget — a collapsible section that the user can expand and collapse:

```html
<details>
    <summary>What browsers do you support?</summary>
    <p>We support the current stable versions of Chrome, Firefox, and Safari.
    We do not support Internet Explorer.</p>
</details>
```

This renders as a clickable triangle with the summary text. Clicking it reveals the content. No JavaScript. No CSS. It just works.

You can make a `<details>` element open by default with the `open` attribute:

```html
<details open>
    <summary>Shipping Information</summary>
    <p>Free shipping on orders over $50.</p>
</details>
```

You can create an accordion by giving multiple `<details>` elements the same `name` attribute — only one with the same name can be open at a time:

```html
<details name="faq">
    <summary>Question 1</summary>
    <p>Answer 1</p>
</details>
<details name="faq">
    <summary>Question 2</summary>
    <p>Answer 2</p>
</details>
<details name="faq">
    <summary>Question 3</summary>
    <p>Answer 3</p>
</details>
```

This exclusive accordion behavior was added as a baseline feature in 2024 and is available in all evergreen browsers.

### The Popover API

The Popover API is a built-in mechanism for creating popovers — tooltips, menus, and other non-modal overlays:

```html
<button popovertarget="my-popover">Help</button>

<div id="my-popover" popover>
    <p>This is a help popover. Click anywhere outside to dismiss.</p>
</div>
```

When the user clicks the button, the popover appears. Clicking anywhere outside the popover dismisses it. Pressing Escape dismisses it. The popover is rendered in the top layer (above all other content, regardless of `z-index`). No JavaScript needed.

You can control whether the popover toggles (default) or only shows:

```html
<!-- Toggle popover (click to show, click again to hide) -->
<button popovertarget="menu" popovertargetaction="toggle">Menu</button>

<!-- Only show the popover (another mechanism must hide it) -->
<button popovertarget="tooltip" popovertargetaction="show">?</button>
```

The `popover` attribute has two values: `auto` (the default — light-dismiss behavior, only one auto popover can be open at a time) and `manual` (no light-dismiss, multiple can be open simultaneously).

### Tables

Tables display tabular data. Not layouts — **data**. If your content is naturally organized into rows and columns (a spreadsheet, a comparison chart, a schedule), use a table. If you are using a table to position elements on the page, stop. That was how we built layouts in 1999. We have CSS Grid now.

```html
<table>
    <caption>Quarterly Revenue (in millions USD)</caption>
    <thead>
        <tr>
            <th scope="col">Quarter</th>
            <th scope="col">Revenue</th>
            <th scope="col">Growth</th>
        </tr>
    </thead>
    <tbody>
        <tr>
            <th scope="row">Q1 2026</th>
            <td>$12.4M</td>
            <td>+8%</td>
        </tr>
        <tr>
            <th scope="row">Q2 2026</th>
            <td>$14.1M</td>
            <td>+14%</td>
        </tr>
    </tbody>
    <tfoot>
        <tr>
            <th scope="row">Total</th>
            <td>$26.5M</td>
            <td>+11%</td>
        </tr>
    </tfoot>
</table>
```

Important table elements and attributes:

- **`<caption>`** — A title for the table. Screen readers read it when the user encounters the table, giving them context.
- **`<thead>`**, **`<tbody>`**, **`<tfoot>`** — Group the header, body, and footer rows. These are not just organizational — they have semantic meaning and enable separate scrolling of the body.
- **`<th>`** — A header cell. Use `scope="col"` for column headers and `scope="row"` for row headers. This tells screen readers how to associate data cells with their headers.

### The Inert Attribute

The `inert` attribute makes an element and all its descendants non-interactive — they cannot be focused, clicked, or selected:

```html
<div id="main-content" inert>
    <!-- This content is frozen while a dialog is open -->
    <h1>Main Content</h1>
    <button>You cannot click me</button>
</div>

<dialog open>
    <p>Only this dialog is interactive.</p>
    <button>You can click me</button>
</dialog>
```

This is enormously useful for modals, loading states, and any situation where part of the page should be temporarily disabled.

---

## Part 3 — CSS: Styling the Document

### How CSS Works

CSS is a declarative language for describing how HTML elements should be rendered. It works by *selecting* elements and *declaring* properties:

```css
/* Selector: select all <p> elements */
/* Declarations: set the color and font-size */
p {
    color: #333;
    font-size: 1rem;
    line-height: 1.6;
}
```

The "Cascading" in CSS refers to the algorithm the browser uses to determine which styles apply when multiple rules target the same element. The cascade considers, in order: importance (`!important`), specificity (how specific the selector is), layer order (CSS cascade layers), and source order (which rule comes last).

### The Box Model

Every HTML element generates a rectangular box. This box has four layers:

1. **Content** — The actual content (text, images, etc.).
2. **Padding** — Space between the content and the border.
3. **Border** — A line around the padding.
4. **Margin** — Space outside the border, separating the element from its neighbors.

By default, the `width` and `height` properties set the size of the *content* area only. Padding and border are added *outside* that size. This means that if you set `width: 300px` and `padding: 20px`, the total width of the element is 340 pixels (300 + 20 + 20). This is insane. It is the single most confusing default in CSS.

Fix it with `box-sizing: border-box`:

```css
*, *::before, *::after {
    box-sizing: border-box;
}
```

With `border-box`, the `width` and `height` properties include padding and border. If you set `width: 300px` and `padding: 20px`, the content area shrinks to 260 pixels, and the total width remains 300 pixels. **Always apply this reset.** Every CSS reset and every CSS framework starts with this rule, because the default behavior is universally considered a mistake.

### Units

CSS has many units. Here are the ones that matter:

**`rem`** — Relative to the root font size (the `<html>` element's font-size). If the root font size is 16px (the default), `1rem` = 16px, `1.5rem` = 24px, `0.875rem` = 14px. Use `rem` for almost everything — font sizes, padding, margins, widths, heights. It scales proportionally when the user changes their browser font size, which is essential for accessibility.

**`em`** — Relative to the font size of the *current element*. If an element has `font-size: 20px`, then `1em` within that element is 20px. Use `em` for properties that should scale with the element's own font size — padding on a button, for example, should be proportional to the button's text size.

**`%`** — Relative to the parent element's corresponding property. `width: 50%` means "half the parent's width."

**`vw` and `vh`** — Viewport width and viewport height. `1vw` is 1% of the viewport width. `100vh` is the full viewport height. Use sparingly and carefully — on mobile, `100vh` includes the area behind the browser's address bar, which means content can be hidden. Use `100dvh` (dynamic viewport height) instead, which accounts for the address bar:

```css
.hero {
    min-height: 100dvh; /* Full screen, accounting for mobile address bar */
}
```

**`px`** — Pixels. Avoid them. Pixel values do not scale when the user changes their font size. If you use `font-size: 14px`, a user who sets their browser font size to 150% (because they have difficulty reading small text) will still see 14px text. If you use `font-size: 0.875rem`, that same user will see 21px text. **Never use pixels for font sizes.** Minimize pixel use for everything else.

### Custom Properties (CSS Variables)

Custom properties are the foundation of a maintainable CSS architecture:

```css
:root {
    /* Colors */
    --color-bg: #ffffff;
    --color-text: #1a1a2e;
    --color-primary: #2563eb;
    --color-primary-fg: #ffffff;
    --color-muted: #6b7280;
    --color-border: #e5e7eb;
    --color-surface: #f3f4f6;

    /* Typography */
    --font-sans: system-ui, -apple-system, "Segoe UI", Roboto, sans-serif;
    --font-mono: ui-monospace, "Cascadia Code", "Fira Code", monospace;

    /* Spacing */
    --space-xs: 0.25rem;
    --space-sm: 0.5rem;
    --space-md: 1rem;
    --space-lg: 2rem;
    --space-xl: 4rem;

    /* Layout */
    --max-width: 70rem;
    --radius: 0.375rem;
}
```

Custom properties are inherited. If you set `--color-primary` on `:root`, every element in the document can use it. If you set a different value on a child element, that value applies within that subtree.

This is how we implement themes. A dark theme is just a different set of custom property values:

```css
[data-theme="dark"] {
    --color-bg: #0f172a;
    --color-text: #e2e8f0;
    --color-primary: #60a5fa;
    --color-primary-fg: #0f172a;
    --color-muted: #94a3b8;
    --color-border: #334155;
    --color-surface: #1e293b;
}
```

Change the `data-theme` attribute on the `<html>` element, and every color in the application updates instantly. No JavaScript loops. No class toggling. Just CSS.

### Modern Layout: Flexbox

Flexbox is for one-dimensional layouts — arranging items in a row or a column:

```css
.navbar {
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 1rem;
    padding: 0.75rem 1rem;
}
```

The key flexbox properties:

- **`display: flex`** — Makes the element a flex container.
- **`flex-direction`** — `row` (default, horizontal) or `column` (vertical).
- **`justify-content`** — Alignment along the main axis. `flex-start`, `flex-end`, `center`, `space-between`, `space-around`, `space-evenly`.
- **`align-items`** — Alignment along the cross axis. `stretch` (default), `flex-start`, `flex-end`, `center`, `baseline`.
- **`gap`** — Space between flex items. This replaced the old hack of using margins on items.
- **`flex-wrap`** — `nowrap` (default) or `wrap` (items wrap to the next line if they overflow).

On items:

- **`flex: 1`** — Shorthand for `flex-grow: 1; flex-shrink: 1; flex-basis: 0%`. The item grows to fill available space.
- **`flex-shrink: 0`** — The item will not shrink below its natural size.
- **`order`** — Change the visual order of an item without changing the DOM order.
- **`min-width: 0`** — Critical fix. Flex items default to `min-width: auto`, which means they will not shrink smaller than their content. If you have a `<pre>` block inside a flex item, it can force the entire page wider than the viewport. Setting `min-width: 0` on the flex item allows it to shrink, and the content inside can scroll with `overflow-x: auto`.

### Modern Layout: CSS Grid

Grid is for two-dimensional layouts — rows *and* columns simultaneously:

```css
.page-layout {
    display: grid;
    grid-template-columns: 15rem 1fr;
    grid-template-rows: auto 1fr auto;
    grid-template-areas:
        "header header"
        "sidebar main"
        "footer footer";
    min-height: 100dvh;
}

.site-header  { grid-area: header; }
.site-sidebar { grid-area: sidebar; }
.site-main    { grid-area: main; }
.site-footer  { grid-area: footer; }
```

Grid-template-areas is one of the most readable layout systems ever designed. You can literally see the layout in the CSS.

For responsive grids that automatically adjust to available space:

```css
.card-grid {
    display: grid;
    grid-template-columns: repeat(auto-fit, minmax(min(100%, 20rem), 1fr));
    gap: 1.5rem;
}
```

This creates a grid where each card is at least 20rem wide. On a wide screen, you might have four columns. On a narrow screen, you might have one column. The browser calculates how many columns fit. No media queries needed.

### CSS Nesting

As of 2024, CSS supports native nesting. No Sass required:

```css
.card {
    border: 1px solid var(--color-border);
    border-radius: var(--radius);
    padding: 1.5rem;

    & h2 {
        font-size: 1.25rem;
        margin-bottom: 0.5rem;
    }

    & p {
        color: var(--color-muted);
    }

    &:hover {
        border-color: var(--color-primary);
    }

    @media (max-width: 40em) {
        padding: 1rem;
    }
}
```

The `&` represents the parent selector. Nesting is available in all evergreen browsers.

### The :has() Selector

The `:has()` selector is the "parent selector" that CSS developers waited twenty years for. It selects an element based on its descendants:

```css
/* Select a .card that contains an img */
.card:has(img) {
    grid-template-rows: 12rem auto;
}

/* Select a .card that does NOT contain an img */
.card:not(:has(img)) {
    grid-template-rows: auto;
}

/* Style a label whose associated input is required */
label:has(+ input:required)::after {
    content: " *";
    color: red;
}

/* Style a form that contains an invalid input */
form:has(:invalid) {
    border-color: red;
}
```

This is transformative. Before `:has()`, you could not style a parent based on its children without JavaScript. Now you can.

### Container Queries

Media queries respond to the *viewport* size. Container queries respond to the *container's* size. This is the difference between "is the screen narrow?" and "is *this component* narrow?"

```css
.card-container {
    container-type: inline-size;
    container-name: card;
}

.card {
    display: grid;
    grid-template-columns: 1fr;
}

@container card (min-width: 30rem) {
    .card {
        grid-template-columns: 12rem 1fr;
    }
}
```

Now the card layout responds to the container's width, not the viewport's width. If you put the same card in a narrow sidebar, it uses the single-column layout. If you put it in a wide main content area, it uses the two-column layout. The card does not care where it lives.

### Cascade Layers

Cascade layers give you explicit control over the cascade — which styles override which:

```css
/* Define the layer order — later layers have higher priority */
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
        color: var(--color-text);
        background: var(--color-bg);
        line-height: 1.6;
    }
    a { color: var(--color-primary); }
}

@layer components {
    .btn {
        display: inline-block;
        padding: 0.5rem 1rem;
        border-radius: var(--radius);
        font-weight: 600;
        text-decoration: none;
        cursor: pointer;
    }
}

@layer utilities {
    .text-center { text-align: center; }
    .mt-lg { margin-top: var(--space-lg); }
}
```

Styles in later layers always override styles in earlier layers, regardless of specificity. This eliminates the specificity warfare that plagues large CSS codebases. Your reset can use `*` selectors without worrying about overriding component styles. Your utilities can override components without needing `!important`.

### Scroll-Driven Animations

You can now animate elements based on the user's scroll position, entirely in CSS:

```css
.reveal {
    opacity: 0;
    transform: translateY(1.25rem);
    animation: fade-in linear both;
    animation-timeline: view();
    animation-range: entry 0% entry 100%;
}

@keyframes fade-in {
    to {
        opacity: 1;
        transform: translateY(0);
    }
}
```

This animates the element from invisible and shifted down to visible and in place, as it scrolls into view. No JavaScript. No Intersection Observer. Pure CSS. This feature reached baseline availability across all major browsers in 2025.

### View Transitions

View transitions animate between states of your page — for example, when navigating between pages in a single-page application:

```css
@view-transition {
    navigation: auto;
}

::view-transition-old(root) {
    animation: fade-out 0.3s ease;
}

::view-transition-new(root) {
    animation: fade-in 0.3s ease;
}
```

For same-document transitions (within a single-page application), you can trigger them from JavaScript:

```javascript
document.startViewTransition(() => {
    // Update the DOM here
    updateContent(newContent);
});
```

The browser captures a snapshot of the old state, you update the DOM, the browser captures a snapshot of the new state, and then it animates between them.

---

## Part 4 — JavaScript: Making It Interactive

### The Language

JavaScript is a dynamic, weakly typed, prototype-based programming language. It was created in ten days in 1995 by Brendan Eich at Netscape, and despite its rushed origins, it has evolved into a capable and expressive language. Modern JavaScript (ES2015 and later) is a fundamentally different language from the JavaScript of 2005.

### Variables

```javascript
// const — cannot be reassigned. Use for almost everything.
const name = "Observer Magazine";
const items = [1, 2, 3];
items.push(4); // This works — const prevents reassignment, not mutation.

// let — can be reassigned. Use when the value will change.
let count = 0;
count += 1;

// var — function-scoped, hoisted. Never use it. Pretend it does not exist.
```

### Functions

```javascript
// Function declaration — hoisted (can be called before it's defined)
function greet(name) {
    return `Hello, ${name}!`;
}

// Arrow function — concise syntax, lexical `this`
const greet = (name) => `Hello, ${name}!`;

// Arrow function with body
const processItems = (items) => {
    const results = [];
    for (const item of items) {
        results.push(item * 2);
    }
    return results;
};
```

### Template Literals

Template literals use backticks and allow embedded expressions:

```javascript
const name = "World";
const greeting = `Hello, ${name}!`; // "Hello, World!"

// Multi-line strings
const html = `
    <div class="card">
        <h2>${title}</h2>
        <p>${description}</p>
    </div>
`;
```

### Destructuring

```javascript
// Object destructuring
const { name, email, role = "user" } = user;

// Array destructuring
const [first, second, ...rest] = items;

// In function parameters
function createUser({ name, email, role = "user" }) {
    // ...
}
```

### The Spread Operator

```javascript
// Spread arrays
const combined = [...array1, ...array2];

// Spread objects (shallow copy with overrides)
const updated = { ...original, name: "New Name" };

// Function arguments
const numbers = [1, 2, 3];
Math.max(...numbers); // 3
```

### Optional Chaining and Nullish Coalescing

```javascript
// Optional chaining — returns undefined instead of throwing if any part is null/undefined
const city = user?.address?.city;
const first = items?.[0];
const result = callback?.();

// Nullish coalescing — provides a default for null or undefined (but not 0 or "")
const port = config.port ?? 3000;
const name = user.name ?? "Anonymous";
```

### ES Modules

ES modules are the native JavaScript module system. They work in all evergreen browsers:

```html
<!-- In your HTML file -->
<script type="module" src="app.js"></script>
```

```javascript
// math.js — exporting
export function add(a, b) {
    return a + b;
}

export function multiply(a, b) {
    return a * b;
}

export const PI = 3.14159265358979;

// A module can have one default export
export default class Calculator {
    // ...
}
```

```javascript
// app.js — importing
import Calculator, { add, multiply, PI } from './math.js';

const result = add(2, 3);
```

Key facts about ES modules:

1. **They are strict mode by default.** You do not need `"use strict"`.
2. **They are deferred by default.** A `<script type="module">` does not block parsing — it loads and executes after the document is parsed, like `<script defer>`.
3. **They have their own scope.** Variables defined in a module are not globals. They are scoped to the module.
4. **They support top-level `await`.** You can use `await` at the top level of a module without wrapping it in an `async` function.
5. **Import paths must be relative or absolute.** `import { add } from 'math'` does not work (that is a bare specifier, used by bundlers). You must write `import { add } from './math.js'` — with the extension and with a `./` or `/` prefix.

### The DOM API

The Document Object Model (DOM) is the browser's representation of the HTML document as a tree of objects. JavaScript interacts with the page through the DOM.

```javascript
// Finding elements
const header = document.getElementById('site-header');
const buttons = document.querySelectorAll('.btn');
const firstCard = document.querySelector('.card');

// Creating elements
const div = document.createElement('div');
div.className = 'notification';
div.textContent = 'Operation successful!';

// Inserting elements
document.body.appendChild(div);
header.insertAdjacentHTML('afterend', '<p>Welcome!</p>');

// Modifying elements
div.classList.add('visible');
div.classList.remove('hidden');
div.classList.toggle('active');
div.setAttribute('role', 'alert');
div.style.setProperty('--progress', '75%');

// Removing elements
div.remove();
```

### Events

```javascript
// Modern event handling — always use addEventListener
const button = document.querySelector('#save-btn');

button.addEventListener('click', (event) => {
    event.preventDefault();
    saveDocument();
});

// Event delegation — listen on a parent, handle clicks on children
document.querySelector('.todo-list').addEventListener('click', (event) => {
    const deleteButton = event.target.closest('.delete-btn');
    if (deleteButton) {
        const todoItem = deleteButton.closest('.todo-item');
        todoItem.remove();
    }
});
```

Event delegation is a critical pattern. Instead of attaching an event listener to every item in a list (which is wasteful and breaks when items are added dynamically), you attach one listener to the container and check which child was clicked. The `.closest()` method walks up the DOM tree to find the nearest ancestor that matches a selector.

### Fetch

`fetch` is the modern API for making HTTP requests:

```javascript
// GET request
async function loadPosts() {
    const response = await fetch('/api/posts');

    if (!response.ok) {
        throw new Error(`HTTP ${response.status}: ${response.statusText}`);
    }

    const posts = await response.json();
    return posts;
}

// POST request
async function createPost(title, body) {
    const response = await fetch('/api/posts', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
        },
        body: JSON.stringify({ title, body }),
    });

    if (!response.ok) {
        const error = await response.json();
        throw new Error(error.message);
    }

    return response.json();
}
```

**Important:** `fetch` does not reject on HTTP error statuses (404, 500, etc.). It only rejects on network errors (the server is unreachable). You must always check `response.ok` yourself. This catches many people off guard.

### Async/Await and Error Handling

```javascript
// Async/await for clean asynchronous code
async function initializeApp() {
    try {
        const [posts, authors, config] = await Promise.all([
            fetch('/api/posts').then(r => r.json()),
            fetch('/api/authors').then(r => r.json()),
            fetch('/api/config').then(r => r.json()),
        ]);

        renderApp(posts, authors, config);
    } catch (error) {
        console.error('Failed to initialize:', error);
        showErrorMessage('Unable to load the application. Please refresh.');
    }
}
```

`Promise.all` runs multiple async operations in parallel and waits for all of them to complete. This is critical for performance — loading three API calls sequentially takes three times as long as loading them in parallel.

### Local Storage

`localStorage` stores key-value string pairs that persist across browser sessions:

```javascript
// Save a value
localStorage.setItem('theme', 'dark');

// Retrieve a value
const theme = localStorage.getItem('theme');

// Save complex data (must serialize to JSON)
const prefs = { fontSize: 16, theme: 'dark', language: 'en' };
localStorage.setItem('preferences', JSON.stringify(prefs));

// Retrieve complex data
const savedPrefs = JSON.parse(localStorage.getItem('preferences'));

// Remove a value
localStorage.removeItem('theme');
```

**Security note:** Never store sensitive data (tokens, passwords, personal information) in `localStorage`. It is accessible to any JavaScript on the page, which means an XSS attack can read it. Use `httpOnly` cookies for authentication tokens.

---

## Part 5 — Web Components: Your Own HTML Elements

### What Are Web Components?

Web Components are a set of browser APIs that let you define your own HTML elements with encapsulated behavior, styles, and markup. They are built on three technologies:

1. **Custom Elements** — Define new HTML elements.
2. **Shadow DOM** — Encapsulate styles and markup so they do not leak.
3. **HTML Templates** — Define reusable chunks of HTML.

Web Components are a browser standard. They work everywhere — in a React app, in an Angular app, in a plain HTML file, anywhere. They are the web's native component model.

### Defining a Custom Element

```javascript
// Define a custom element
class AppCounter extends HTMLElement {
    #count = 0;

    constructor() {
        super();
        this.attachShadow({ mode: 'open' });
    }

    connectedCallback() {
        this.render();
    }

    get count() {
        return this.#count;
    }

    set count(value) {
        this.#count = value;
        this.render();
    }

    increment() {
        this.count += 1;
    }

    decrement() {
        this.count -= 1;
    }

    render() {
        this.shadowRoot.innerHTML = `
            <style>
                :host {
                    display: inline-flex;
                    align-items: center;
                    gap: 0.5rem;
                    font-family: system-ui, sans-serif;
                }
                button {
                    width: 2rem;
                    height: 2rem;
                    border: 1px solid #ddd;
                    border-radius: 0.25rem;
                    background: #f3f4f6;
                    cursor: pointer;
                    font-size: 1.2rem;
                    display: flex;
                    align-items: center;
                    justify-content: center;
                }
                button:hover {
                    background: #e5e7eb;
                }
                .count {
                    min-width: 2rem;
                    text-align: center;
                    font-weight: 600;
                    font-size: 1.1rem;
                }
            </style>
            <button id="dec" aria-label="Decrease count">−</button>
            <span class="count">${this.#count}</span>
            <button id="inc" aria-label="Increase count">+</button>
        `;

        this.shadowRoot.getElementById('dec')
            .addEventListener('click', () => this.decrement());
        this.shadowRoot.getElementById('inc')
            .addEventListener('click', () => this.increment());
    }
}

// Register the element
customElements.define('app-counter', AppCounter);
```

Now you can use it in HTML:

```html
<app-counter></app-counter>
```

That is it. A fully encapsulated, reusable counter component. The styles inside the Shadow DOM do not leak out — the `button` styles only apply to buttons inside this component, not to buttons elsewhere on the page. External styles do not leak in — even if you have `button { background: red; }` in your global CSS, the counter's buttons are unaffected.

### The Lifecycle

Custom elements have four lifecycle callbacks:

1. **`connectedCallback()`** — Called when the element is inserted into the document. This is where you set up the component, render content, and add event listeners.
2. **`disconnectedCallback()`** — Called when the element is removed from the document. Clean up event listeners, timers, and observers here.
3. **`attributeChangedCallback(name, oldValue, newValue)`** — Called when an observed attribute changes. You must declare which attributes to observe with a static `observedAttributes` getter.
4. **`adoptedCallback()`** — Called when the element is moved to a new document. Rarely used.

```javascript
class UserCard extends HTMLElement {
    static observedAttributes = ['name', 'email', 'avatar'];

    constructor() {
        super();
        this.attachShadow({ mode: 'open' });
    }

    connectedCallback() {
        this.render();
    }

    attributeChangedCallback(name, oldValue, newValue) {
        if (oldValue !== newValue) {
            this.render();
        }
    }

    render() {
        const name = this.getAttribute('name') || 'Unknown';
        const email = this.getAttribute('email') || '';
        const avatar = this.getAttribute('avatar') || '';

        this.shadowRoot.innerHTML = `
            <style>
                :host {
                    display: flex;
                    gap: 1rem;
                    padding: 1rem;
                    border: 1px solid #e5e7eb;
                    border-radius: 0.5rem;
                    font-family: system-ui, sans-serif;
                }
                img {
                    width: 3rem;
                    height: 3rem;
                    border-radius: 50%;
                    object-fit: cover;
                }
                .info { display: flex; flex-direction: column; }
                .name { font-weight: 600; }
                .email { color: #6b7280; font-size: 0.875rem; }
            </style>
            ${avatar ? `<img src="${avatar}" alt="${name}">` : ''}
            <div class="info">
                <span class="name">${name}</span>
                ${email ? `<span class="email">${email}</span>` : ''}
            </div>
        `;
    }
}

customElements.define('user-card', UserCard);
```

Usage:

```html
<user-card
    name="Kushal"
    email="hello@example.com"
    avatar="images/kushal.png">
</user-card>
```

### Slots

Slots let consumers of your component inject content:

```javascript
class AppModal extends HTMLElement {
    constructor() {
        super();
        this.attachShadow({ mode: 'open' });
        this.shadowRoot.innerHTML = `
            <style>
                :host { display: none; }
                :host([open]) { display: block; }
                .backdrop {
                    position: fixed;
                    inset: 0;
                    background: rgba(0, 0, 0, 0.5);
                    display: flex;
                    align-items: center;
                    justify-content: center;
                    z-index: 1000;
                }
                .content {
                    background: white;
                    border-radius: 0.5rem;
                    padding: 1.5rem;
                    max-width: 30rem;
                    width: 90%;
                    max-height: 80vh;
                    overflow-y: auto;
                }
            </style>
            <div class="backdrop" part="backdrop">
                <div class="content" part="content">
                    <slot></slot>
                </div>
            </div>
        `;

        this.shadowRoot.querySelector('.backdrop')
            .addEventListener('click', (e) => {
                if (e.target === e.currentTarget) {
                    this.close();
                }
            });
    }

    open() {
        this.setAttribute('open', '');
    }

    close() {
        this.removeAttribute('open');
        this.dispatchEvent(new CustomEvent('close'));
    }
}

customElements.define('app-modal', AppModal);
```

Usage:

```html
<app-modal id="my-modal">
    <h2>Are you sure?</h2>
    <p>This action cannot be undone.</p>
    <button onclick="document.getElementById('my-modal').close()">
        Cancel
    </button>
    <button onclick="deleteItem()">
        Delete
    </button>
</app-modal>
```

The `<slot>` element in the Shadow DOM is replaced by whatever content the consumer puts inside the element.

### CSS Custom Properties for Theming

While Shadow DOM blocks external styles, CSS custom properties *do* penetrate the shadow boundary. This is by design — it allows theming:

```javascript
// Inside the component
this.shadowRoot.innerHTML = `
    <style>
        .btn {
            background: var(--btn-bg, #2563eb);
            color: var(--btn-color, white);
            padding: var(--btn-padding, 0.5rem 1rem);
            border-radius: var(--btn-radius, 0.25rem);
        }
    </style>
    <button class="btn"><slot></slot></button>
`;
```

Consumers can theme the component:

```css
app-button {
    --btn-bg: #059669;
    --btn-color: white;
    --btn-radius: 99rem;
}
```

---

## Part 6 — Building a Real Application

### Client-Side Routing Without a Framework

A single-page application needs a router. Here is one in under 60 lines:

```javascript
// router.js
class Router {
    #routes = new Map();
    #notFound = null;

    addRoute(path, handler) {
        this.#routes.set(path, handler);
        return this;
    }

    setNotFound(handler) {
        this.#notFound = handler;
        return this;
    }

    start() {
        window.addEventListener('popstate', () => this.#resolve());
        document.addEventListener('click', (e) => {
            const link = e.target.closest('a[href]');
            if (link && link.origin === location.origin && !link.hasAttribute('target')) {
                e.preventDefault();
                this.navigate(link.pathname);
            }
        });
        this.#resolve();
    }

    navigate(path) {
        if (path !== location.pathname) {
            history.pushState(null, '', path);
            this.#resolve();
        }
    }

    #resolve() {
        const path = location.pathname;

        for (const [routePath, handler] of this.#routes) {
            const params = this.#matchRoute(routePath, path);
            if (params !== null) {
                handler(params);
                return;
            }
        }

        if (this.#notFound) {
            this.#notFound();
        }
    }

    #matchRoute(routePath, actualPath) {
        const routeParts = routePath.split('/');
        const actualParts = actualPath.split('/');

        if (routeParts.length !== actualParts.length) return null;

        const params = {};
        for (let i = 0; i < routeParts.length; i++) {
            if (routeParts[i].startsWith(':')) {
                params[routeParts[i].slice(1)] = decodeURIComponent(actualParts[i]);
            } else if (routeParts[i] !== actualParts[i]) {
                return null;
            }
        }
        return params;
    }
}

export { Router };
```

Usage:

```javascript
// app.js
import { Router } from './router.js';

const app = document.getElementById('app');
const router = new Router();

router
    .addRoute('/', () => {
        app.innerHTML = '<h1>Home</h1><p>Welcome to the app.</p>';
    })
    .addRoute('/about', () => {
        app.innerHTML = '<h1>About</h1><p>This is the about page.</p>';
    })
    .addRoute('/posts/:slug', ({ slug }) => {
        app.innerHTML = `<h1>Post: ${slug}</h1>`;
    })
    .setNotFound(() => {
        app.innerHTML = '<h1>404</h1><p>Page not found.</p>';
    })
    .start();
```

This router handles `<a>` link clicks, browser back/forward buttons, and URL parameters. It is 60 lines of code. React Router is 42,000.

### Simple State Management

```javascript
// store.js
class Store {
    #state;
    #listeners = new Set();

    constructor(initialState) {
        this.#state = structuredClone(initialState);
    }

    getState() {
        return this.#state;
    }

    setState(updater) {
        const newState = typeof updater === 'function'
            ? updater(this.#state)
            : { ...this.#state, ...updater };

        this.#state = newState;
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

export { Store };
```

Usage:

```javascript
import { Store } from './store.js';

const store = new Store({
    todos: [],
    filter: 'all',
});

// Subscribe to changes
store.subscribe((state) => {
    renderTodoList(state.todos, state.filter);
});

// Update state
function addTodo(text) {
    store.setState((state) => ({
        ...state,
        todos: [...state.todos, { id: Date.now(), text, done: false }],
    }));
}
```

---

## Part 7 — Content Security Policy

### Why CSP Matters

A Content Security Policy is an HTTP header (or `<meta>` tag) that tells the browser exactly which resources are allowed to load and execute on your page. Without a CSP, any injected script — from an XSS vulnerability, a compromised CDN, a malicious browser extension — can execute freely.

### The Strictest Possible Policy

Because we have zero external dependencies, we can write the strictest possible CSP:

```html
<meta http-equiv="Content-Security-Policy"
      content="default-src 'none';
               script-src 'self';
               style-src 'self';
               img-src 'self';
               font-src 'self';
               connect-src 'self';
               base-uri 'self';
               form-action 'self';
               frame-ancestors 'none';">
```

This policy says:

- **`default-src 'none'`** — Block everything by default. Every resource type must be explicitly allowed.
- **`script-src 'self'`** — Only allow scripts from our own origin. No inline scripts (no `onclick`, no `<script>` blocks without nonces), no `eval()`, no scripts from any other domain.
- **`style-src 'self'`** — Only allow stylesheets from our own origin.
- **`img-src 'self'`** — Only allow images from our own origin.
- **`font-src 'self'`** — Only allow fonts from our own origin.
- **`connect-src 'self'`** — Only allow `fetch`/`XMLHttpRequest` to our own origin.
- **`base-uri 'self'`** — Prevent attackers from changing the `<base>` URL.
- **`form-action 'self'`** — Forms can only submit to our own origin.
- **`frame-ancestors 'none'`** — Our page cannot be embedded in an `<iframe>` on any other site (prevents clickjacking).

This is essentially a closed fortress. Nothing gets in that you did not explicitly invite. **This policy is only possible because we have zero external dependencies.** The moment you add Google Analytics, you need `script-src 'self' https://www.googletagmanager.com https://www.google-analytics.com`. The moment you use Google Fonts, you need `font-src https://fonts.gstatic.com`. Every external dependency weakens your CSP.

### Handling Inline Scripts

Our strict CSP blocks inline scripts (`onclick`, inline `<script>` tags). If you must have inline scripts (for example, for the pre-paint theme application), use hash-based or nonce-based directives:

```html
<!-- Using a hash: the browser computes the SHA-256 hash of the script content
     and compares it to the hash in the CSP. -->
<meta http-equiv="Content-Security-Policy"
      content="script-src 'self' 'sha256-abc123...';">

<script>
    // This script's hash must match 'sha256-abc123...'
    document.documentElement.setAttribute('data-theme', 'dark');
</script>
```

Hash-based CSP is ideal for static sites where the inline script content never changes. The hash is computed once and added to the CSP.

---

## Part 8 — Accessibility

### The Core Principles

Accessibility (often abbreviated as "a11y") means making your application usable by people with disabilities. This includes:

- **Visual impairments** — blindness, low vision, color blindness.
- **Motor impairments** — inability to use a mouse, limited fine motor control.
- **Hearing impairments** — deafness, hard of hearing.
- **Cognitive impairments** — dyslexia, attention disorders, memory issues.

Accessibility is not optional. It is not a feature you add later. It is a fundamental quality of good software.

### Semantic HTML Is Accessibility

The most important accessibility technique is also the simplest: **use the right HTML element for the right purpose.** A `<button>` is focusable, responds to Enter and Space, and is announced as "button" by screen readers. A `<div onclick="...">` is none of these things. Recreating a button from a `<div>` requires `role="button"`, `tabindex="0"`, a `keydown` handler for Enter and Space, focus styles, and an accessible name. Or you could just use `<button>`.

### ARIA: When HTML Is Not Enough

ARIA (Accessible Rich Internet Applications) attributes supplement HTML semantics. The first rule of ARIA is: **if you can use a native HTML element or attribute, do not use ARIA.** ARIA is for situations where HTML does not have a semantic element for what you are building — custom widgets, dynamic content updates, complex interactions.

Key ARIA attributes:

- **`aria-label`** — Provides an accessible name when there is no visible text.
- **`aria-labelledby`** — Points to an element whose text content serves as the label.
- **`aria-describedby`** — Points to an element that provides additional description.
- **`aria-hidden="true"`** — Hides an element from screen readers (but it remains visible).
- **`aria-live="polite"`** — Announces dynamic content changes to screen readers.
- **`role`** — Overrides the element's default role.

```html
<!-- Status messages that screen readers should announce -->
<div aria-live="polite" id="status" class="sr-only"></div>

<script type="module">
    function showStatus(message) {
        document.getElementById('status').textContent = message;
    }

    // When a todo is added:
    showStatus('Todo item added successfully.');
</script>
```

### Keyboard Navigation

Every interactive element must be operable with a keyboard. Tab moves focus forward, Shift+Tab moves focus backward, Enter activates links and buttons, Space activates buttons and checkboxes, Escape closes dialogs and menus, Arrow keys navigate within composite widgets (tabs, menus, radio groups).

Test your application by unplugging your mouse. Can you reach every interactive element? Can you operate every widget? Can you see where focus is at all times?

### Focus Visibility

Always provide visible focus indicators. The browser's default focus ring is often too subtle. Enhance it:

```css
:focus-visible {
    outline: 2px solid var(--color-primary);
    outline-offset: 2px;
}
```

The `:focus-visible` pseudo-class (as opposed to `:focus`) only shows the focus indicator when the browser determines that the user is navigating with a keyboard. Mouse clicks do not trigger `:focus-visible`, so your buttons do not get a focus ring when clicked. This is the best of both worlds.

### Screen Reader Only Content

Sometimes you need to provide text that is only visible to screen readers — for example, to describe an icon button:

```css
.sr-only {
    position: absolute;
    width: 1px;
    height: 1px;
    padding: 0;
    margin: -1px;
    overflow: hidden;
    clip: rect(0, 0, 0, 0);
    white-space: nowrap;
    border: 0;
}
```

```html
<button>
    <svg aria-hidden="true"><!-- icon --></svg>
    <span class="sr-only">Close dialog</span>
</button>
```

---

## Part 9 — Performance

### The Critical Rendering Path

When a browser loads a page, it:

1. Downloads the HTML.
2. Parses the HTML, constructing the DOM.
3. When it encounters a `<link rel="stylesheet">`, it downloads and parses the CSS, constructing the CSSOM.
4. When it encounters a `<script>` (without `defer` or `async`), it stops parsing, downloads the script, and executes it.
5. Combines the DOM and CSSOM into a render tree.
6. Calculates the layout (positions and sizes of every element).
7. Paints pixels to the screen.

To make this fast:

- **Minimize blocking resources.** Use `<script defer>` or `<script type="module">` (which is deferred by default). Put CSS `<link>` tags in the `<head>` so the browser discovers them early.
- **Keep CSS small.** The browser cannot paint anything until it has parsed all the CSS. If your CSS is 500KB (looking at you, Bootstrap), that blocks rendering.
- **Keep JavaScript small.** Less code means less parsing, less compiling, and less execution time.
- **Load images lazily.** Use `loading="lazy"` on images below the fold.

### Resource Hints

```html
<!-- Preconnect to an API server (establishes connection early) -->
<link rel="preconnect" href="https://api.example.com">

<!-- Prefetch a page the user is likely to visit next -->
<link rel="prefetch" href="/about">

<!-- Preload a critical resource -->
<link rel="preload" href="/fonts/inter-var.woff2" as="font" type="font/woff2" crossorigin>
```

### Service Workers

Service workers are JavaScript files that run in a separate thread and act as a programmable network proxy. They enable offline support, background sync, and push notifications:

```javascript
// sw.js — a basic caching service worker
const CACHE_NAME = 'app-v1';
const PRECACHE_URLS = [
    '/',
    '/css/app.css',
    '/js/app.js',
    '/images/logo.svg',
];

self.addEventListener('install', (event) => {
    event.waitUntil(
        caches.open(CACHE_NAME)
            .then((cache) => cache.addAll(PRECACHE_URLS))
    );
});

self.addEventListener('fetch', (event) => {
    event.respondWith(
        caches.match(event.request)
            .then((cached) => cached || fetch(event.request))
    );
});
```

Register it:

```javascript
if ('serviceWorker' in navigator) {
    navigator.serviceWorker.register('/sw.js');
}
```

---

## Part 10 — Testing Without Frameworks

### The Browser Has a Test Runner

You do not need Jest. You do not need Vitest. You do not need Mocha. You can write tests in plain JavaScript:

```javascript
// test.js
function assert(condition, message) {
    if (!condition) {
        throw new Error(`FAIL: ${message}`);
    }
    console.log(`PASS: ${message}`);
}

function assertEqual(actual, expected, message) {
    if (actual !== expected) {
        throw new Error(`FAIL: ${message} — expected ${expected}, got ${actual}`);
    }
    console.log(`PASS: ${message}`);
}

// Test the add function
import { add, multiply } from './math.js';

assertEqual(add(2, 3), 5, 'add(2, 3) should equal 5');
assertEqual(add(-1, 1), 0, 'add(-1, 1) should equal 0');
assertEqual(add(0, 0), 0, 'add(0, 0) should equal 0');
assertEqual(multiply(3, 4), 12, 'multiply(3, 4) should equal 12');
assertEqual(multiply(0, 100), 0, 'multiply(0, 100) should equal 0');

console.log('All tests passed.');
```

Run it:

```html
<script type="module" src="test.js"></script>
```

Open the HTML file, open the browser console. Green text means passing. Red text means failing. This is all you need for unit tests.

For more comprehensive testing, you can use the built-in `console.assert`:

```javascript
console.assert(add(2, 3) === 5, 'add(2, 3) should equal 5');
```

---

## Part 11 — Deployment

### GitHub Pages

For a static site with no build step, deployment to GitHub Pages is trivial:

1. Push your files to a GitHub repository.
2. Go to Settings → Pages.
3. Select the branch and folder to deploy from.
4. Your site is live at `https://yourusername.github.io`.

If your site uses client-side routing (pushState), you need a `404.html` that redirects to `index.html`:

```html
<!-- 404.html -->
<!DOCTYPE html>
<html>
<head>
    <script>
        // Redirect all 404s to the main page with the path preserved
        sessionStorage.setItem('redirect', location.pathname);
        location.replace('/');
    </script>
</head>
</html>
```

And in your main `index.html`:

```javascript
// Check for redirect
const redirect = sessionStorage.getItem('redirect');
if (redirect) {
    sessionStorage.removeItem('redirect');
    history.replaceState(null, '', redirect);
}
```

### Caching Headers

For static files, set appropriate cache headers:

- **HTML files:** `Cache-Control: no-cache` (always revalidate).
- **CSS and JS files:** `Cache-Control: max-age=31536000, immutable` (cache forever, use file hashing for cache busting).
- **Images and fonts:** `Cache-Control: max-age=31536000, immutable`.

On GitHub Pages, you cannot set custom headers directly. But the built-in caching is reasonable for most cases.

---

## Part 12 — What Is Coming Next

### CSS Features on the Horizon

Several exciting CSS features are approaching baseline availability:

**CSS `if()` function** — Conditional logic inside property values. Not yet in all browsers, but Chrome and Safari are implementing it. This will enable truly dynamic styles without JavaScript.

**CSS Mixins (`@mixin` / `@apply`)** — Define reusable blocks of declarations and apply them across components. This is the last feature that Sass provided that native CSS did not. It is in active development by the CSS Working Group.

**CSS Masonry Layout** — Pinterest-style layouts without JavaScript. The CSS Working Group has settled on the approach, and browsers are implementing it. Currently available in some browsers behind flags.

**Anchor Positioning** — Position elements relative to other elements without JavaScript. The tooltip and popover positioning problem, solved in CSS. Available in Chrome and coming to Firefox and Safari.

**`sibling-index()` and `sibling-count()`** — Functions that let an element know its position among siblings. Perfect for staggered animations and automatic numbering. Available in Chrome and Safari, coming to Firefox.

### JavaScript Features on the Horizon

**Temporal API** — A modern replacement for the `Date` object. Handles time zones, durations, and calendar systems correctly. Available in Firefox, coming to Chrome and Safari.

**Import Maps** — Map bare specifiers to URLs, enabling `import { add } from 'math'` without a bundler. Available in all browsers.

**Decorators** — Stage 3 TC39 proposal for annotating class elements. Coming soon.

---

## Part 13 — Bringing It All Together: A Complete Application

Let us build a complete single-page application — a task manager — using everything we have learned. The application will have:

- Client-side routing
- Web Components for UI
- A strict Content Security Policy
- Local storage persistence
- Keyboard accessibility
- Dark mode with theme persistence
- No external dependencies

### The HTML Shell

```html
<!DOCTYPE html>
<html lang="en" data-theme="light">
<head>
    <meta charset="utf-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <meta http-equiv="Content-Security-Policy"
          content="default-src 'none';
                   script-src 'self';
                   style-src 'self' 'unsafe-inline';
                   img-src 'self';
                   font-src 'self';
                   connect-src 'self';
                   base-uri 'self';
                   form-action 'self';
                   frame-ancestors 'none';">
    <title>TaskFlow — A Zero-Dependency Task Manager</title>
    <link rel="stylesheet" href="css/app.css">
    <script>
        // Apply saved theme before first paint
        const t = localStorage.getItem('taskflow-theme') || 'light';
        document.documentElement.setAttribute('data-theme', t);
    </script>
</head>
<body>
    <div id="app">
        <p>Loading TaskFlow...</p>
    </div>
    <div aria-live="polite" id="announcer" class="sr-only"></div>
    <script type="module" src="js/app.js"></script>
</body>
</html>
```

This is a complete, production-ready HTML shell. The CSP locks down everything. The theme is applied before first paint to prevent flash. The screen reader announcer region is ready for dynamic messages. The application JavaScript loads as a module.

### Project Structure

```
taskflow/
    index.html
    404.html
    css/
        app.css
    js/
        app.js
        router.js
        store.js
        components/
            task-item.js
            task-list.js
            theme-toggle.js
        pages/
            home.js
            about.js
            not-found.js
```

Every file is a plain text file. There is no `package.json`. There is no `node_modules`. There is no build configuration. Open `index.html` in a browser and the application runs.

This is the power of building on the platform instead of on top of it. No abstractions to learn, no tools to configure, no dependencies to update. Just HTML, CSS, and JavaScript — the technologies that power every web page on Earth.

---

## Resources

Here are the authoritative references for everything covered in this article:

- **MDN Web Docs** — The definitive reference for HTML, CSS, and JavaScript: [developer.mozilla.org](https://developer.mozilla.org)
- **Web.dev** — Google's web development best practices: [web.dev](https://web.dev)
- **CSS Specification Snapshot 2026** — The W3C's official CSS specification: [w3.org/TR/css-2026](https://www.w3.org/TR/css-2026/)
- **HTML Living Standard** — The WHATWG's HTML specification: [html.spec.whatwg.org](https://html.spec.whatwg.org)
- **Web Components on MDN** — Complete documentation for Custom Elements, Shadow DOM, and templates: [developer.mozilla.org/en-US/docs/Web/API/Web_components](https://developer.mozilla.org/en-US/docs/Web/API/Web_components)
- **Content Security Policy on MDN** — Full CSP reference: [developer.mozilla.org/en-US/docs/Web/HTTP/Guides/CSP](https://developer.mozilla.org/en-US/docs/Web/HTTP/Guides/CSP)
- **OWASP CSP Cheat Sheet** — Security-focused CSP guidance: [cheatsheetseries.owasp.org/cheatsheets/Content_Security_Policy_Cheat_Sheet.html](https://cheatsheetseries.owasp.org/cheatsheets/Content_Security_Policy_Cheat_Sheet.html)
- **Web Accessibility Initiative (WAI)** — W3C's accessibility guidelines: [w3.org/WAI](https://www.w3.org/WAI/)
- **Baseline Dashboard** — Track which features are available across all browsers: [web.dev/baseline](https://web.dev/baseline)
- **Can I Use** — Browser compatibility tables: [caniuse.com](https://caniuse.com)
- **State of CSS 2025** — Survey results on CSS feature adoption: [2025.stateofcss.com](https://2025.stateofcss.com)
- **CSS Wrapped 2025** — Chrome DevRel's annual CSS feature roundup: [chrome.dev/css-wrapped-2025](https://chrome.dev/css-wrapped-2025)
- **My Blazor Magazine source code** — The repository that hosts this article, built with the principles described here: [github.com/MyBlazor](https://github.com/MyBlazor)
