---
title: "Without a Net, Part 2: Semantic HTML and the Document Outline Nobody Taught You"
date: 2026-05-25
author: myblazor-team
summary: "HTML is not markup for making things look right — it is a typed language for expressing document structure. Day 2 of our fifteen-part no-build web series covers the document outline, landmark elements, every input type, the dialog and details elements that replace JavaScript modal and accordion libraries, the popover attribute, the inert attribute, and exactly when (and when not) to reach for ARIA. If you have ever written a 'div with a role of button' instead of a button, this article is for you."
tags:
  - html
  - semantic-html
  - accessibility
  - aria
  - forms
  - dialog
  - popover
  - web-standards
  - no-build
  - first-principles
  - series
  - deep-dive
---

## Introduction: The Bug That Was Not A Bug

Picture a Wednesday morning in an open-plan office. A developer — let us call him Bilal, though the name does not matter — is staring at a ticket that has been reopened four times. The ticket reads, in its entirety: *"Sign up form broken for visually impaired users. Screen reader says nothing when tabbing to the error messages. Critical. Reproduces on JAWS, NVDA, and VoiceOver."*

Bilal has a solid web background. He knows his way around React. He can wrestle a Redux store into doing complicated things. He has built the sign-up form as a set of tidy components. The layout is custom. The errors are styled beautifully — a little red triangle, a nice fade animation, a custom font. His code looks like this, in spirit:

```html
<div class="form">
  <div class="field">
    <div class="label">Email address</div>
    <div class="input" contenteditable="true"></div>
    <div class="error" style="display: none;">Email is required</div>
  </div>
  <div class="button" onclick="submit()">Sign up</div>
</div>
```

Bilal has tried attaching ARIA attributes. He has added `role="alert"` to the error. He has added `aria-label` to the `<div class="input">`. He has added `role="button"` to the submit `<div>`. Nothing makes JAWS read the error reliably. Meanwhile the QA engineer keeps pasting screenshots that say *"Focus vanishes. Tab order broken. Form submission fails without a mouse."*

The ticket sits on Bilal's desk for three days. On day four, a newer engineer wanders past, glances at the screen for eleven seconds, and says, with all the bluntness that juniors have and seniors sometimes lose: *"Why aren't you using `<form>`?"*

That is the story, almost word for word, as one of our colleagues told it to us. It is a true story and, if we are being honest, it is our story too — not Bilal, but the senior engineer who spent three afternoons on it before asking the right question.

Day 2 of this series is about why that bug happened. It is about HTML as a language — not a decoration, not a canvas, but a typed, structured, semantically meaningful language that tells the browser, the screen reader, the search engine, and the automated test harness what each piece of your page *is*. A `<button>` is not "a div that looks round." A `<form>` is not "a container for inputs." A `<dialog>` is not "a floating panel." Each of these elements has dozens of built-in behaviours — keyboard handling, focus management, assistive-technology semantics, default styles, native events — that you get for free when you use the right element, and that you have to laboriously reimplement (and almost always get wrong) when you do not.

This is a long article. It is long because HTML is bigger than you think, and because the cost of getting it wrong is higher than you think. By the end, you will know every HTML element and attribute worth knowing in 2026, you will have built three things that most developers reach for a library to build, and you will never again write `<div class="button">`.

Let us begin at the beginning.

## Part 1: The Theory — HTML as a Typed Language

If you are coming from a C# background, the easiest way to understand HTML is as a **type system for document structure**. Every HTML element is a type. Each type has a name (`button`, `form`, `table`), a set of permitted attributes (the element's public API), a set of permitted content types (the element's "children"), a default rendering, a default behaviour, a default set of ARIA semantics, and a default set of events it fires.

When you write:

```html
<button type="submit">Sign up</button>
```

you are constructing an instance of type `HTMLButtonElement`. That object has, at minimum, the following behaviours *without any JavaScript on your part*:

- It is focusable with the Tab key.
- It can be activated with Enter or Space.
- If it is inside a `<form>`, pressing Enter in a text field submits that form through this button (assuming `type="submit"`, which is the default).
- It fires a `click` event when activated, regardless of whether the user clicked, tapped, pressed Enter, or pressed Space.
- Screen readers announce it as "Sign up, button."
- When disabled with the `disabled` attribute, it becomes unfocusable and its form is not submitted.
- The browser draws the OS-default focus ring around it when focused.
- On mobile, tapping it does not trigger the 300ms tap delay that used to plague `<a>` elements.
- It participates in the parent form's `elements` collection.
- If you do `form.requestSubmit(button)`, the submission fires as if the user clicked it.

None of that is something you wrote. All of it comes with the type. This is exactly analogous to the difference between:

```csharp
// C# — the right way
public class Button : Control { /* ... */ }
var btn = new Button { Text = "Sign up" };

// C# — the "div of button" equivalent
var obj = new { Text = "Sign up", Rendered = "rect" };
// Now reimplement focus, keyboard, events, semantics, accessibility...
```

No reasonable C# developer would build the second one. But in HTML we do this all the time:

```html
<div role="button" tabindex="0" class="btn"
     onclick="handleClick()"
     onkeydown="if (event.key === 'Enter' || event.key === ' ') handleClick()">
  Sign up
</div>
```

Even with all those attributes, this `<div>` is worse than a `<button>`. It does not submit forms. It does not handle disabled state. It does not have native focus ring logic. It will be treated differently by every screen reader, in ways that depend on the exact version of the AT, the exact browser, and often the exact day of the week. When Safari 19.2 subtly changes ARIA resolution behaviour, your `<div role="button">` changes behaviour in production and your `<button>` does not.

The rule is therefore very simple, and you should print it on a card and put it above your monitor:

> **Use the HTML element that the browser has already built for the job. ARIA is a patch of last resort, not a first choice.**

The official version of this rule is **the first rule of ARIA**, which has been in the WAI-ARIA Authoring Practices Guide for almost a decade: *"If you can use a native HTML element with the semantics and behaviour you require already built in, instead of re-purposing an element and adding an ARIA role, state, or property, do so."* The rest of this article is essentially an annotated tour of the elements that first rule is telling you to use.

## Part 2: The Document — Why the Top of the File Matters

Before we get to individual elements, a short tour of the top of your HTML file, because most of us got this wrong in 2011 and cargo-culted it forward.

```html
<!doctype html>
<html lang="en">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <title>My Blazor Magazine</title>
  <link rel="icon" href="/favicon.svg" type="image/svg+xml">
  <link rel="canonical" href="https://observermagazine.github.io/blog/no-build-web-html-semantics">
  <meta name="description" content="A plain-language guide to semantic HTML in 2026.">
  <meta name="theme-color" content="#0d6efd">
  <link rel="stylesheet" href="/css/app.css">
</head>
<body>
  <!-- content -->
</body>
</html>
```

Each line earns its place. Let us walk through them, because the defaults matter.

**`<!doctype html>`** tells the browser to use standards mode. Without it, browsers use "quirks mode", which is a museum of layout bugs from Internet Explorer 5. Always include it. It does not need to be uppercase, and it does not need a version number. The literal string `<!doctype html>` has been valid HTML since 2008 and is still the right thing to type in 2026.

**`<html lang="en">`** tells screen readers which language to pronounce the content in. Without it, JAWS will happily pronounce "chateau" as "CHAY-tow." Set the language correctly. Use [BCP 47 language tags](https://datatracker.ietf.org/doc/html/rfc5646) — `en`, `en-US`, `en-GB`, `fr`, `ja`, `ne` for Nepali, etc. You can also override it per-element on snippets in other languages: `<span lang="fr">bonjour</span>`.

**`<meta charset="utf-8">`** sets the character encoding. Without it some browsers will fall back to legacy encodings and your em-dashes will turn into question marks. It must be in the first 1024 bytes of the document, which means it should be the first or second child of `<head>`. `utf-8` is the only acceptable value in 2026.

**`<meta name="viewport" content="width=device-width, initial-scale=1">`** is the line that makes your site responsive on mobile. Without it, mobile browsers render at 980px wide and then shrink to fit the phone's screen, which makes your text microscopic. This single line of HTML is more important for mobile than any CSS you will ever write. Do not leave it out. Do not add `user-scalable=no` — that breaks pinch-zoom, which is an accessibility feature for users with low vision.

**`<title>`** becomes the window title, the tab title, the default bookmark name, and the result title in search engines. It is also the first thing screen readers announce on page load. Make it informative. "Home" is bad; "My Blazor Magazine — Home" is better; "Without a Net, Part 2: Semantic HTML — My Blazor Magazine" is best, because search engines display the first 50–60 characters.

**`<link rel="icon">`** specifies the favicon. SVG favicons are preferred because they scale to any resolution and can adapt to light and dark modes with `@media (prefers-color-scheme: dark)` embedded inside the SVG. You can have multiple `<link rel="icon">` tags with different `sizes` and `type` attributes for different fallbacks.

**`<link rel="canonical">`** tells search engines which URL is the canonical one when the same page is reachable through multiple URLs. Crucial for SEO; ignored by almost every blog until they notice their SEO is broken.

**`<meta name="description">`** is used by search engines as the snippet under the title. Also used by social media platforms when the Open Graph description is missing. Write actual prose, 150–160 characters, that describes the page in a way that would make you click on it.

**`<meta name="theme-color">`** sets the browser chrome colour on mobile Safari and Chrome. Small touch, big aesthetic win. You can provide different values for light and dark modes using `media` attributes:

```html
<meta name="theme-color" media="(prefers-color-scheme: light)" content="#ffffff">
<meta name="theme-color" media="(prefers-color-scheme: dark)"  content="#111111">
```

**`<link rel="stylesheet">`** is, well, the stylesheet. One note: do not bother with `type="text/css"` — it has been the default for 25 years and you are adding bytes for nothing. Also, do not add `rel="stylesheet" type="text/css"` with a `media` query expecting async loading; use `<link rel="preload" as="style">` or plain `<style>` for that.

There are other meta tags worth knowing — Open Graph (`og:title`, `og:image`, `og:description`) for social sharing, Twitter Card meta tags for Twitter specifically, and a small pile of robot-directed meta tags (`robots`, `googlebot`). We will come back to these in Day 14 when we cover SEO and social sharing.

## Part 3: The Document Outline — The Structure Everyone Ignores

Now inside `<body>`. Most developers learn `<h1>` through `<h6>` and immediately start using them as "six font sizes." This is wrong. Headings are the **structural skeleton of your document**, and screen readers navigate by them the way sighted users navigate by scrolling.

Open any major screen reader and you will find a "rotor" or "elements list" that shows every heading on the page. Users jump between headings. If your page has eight `<h1>` tags because you thought `<h1>` looked nicest, the user has no idea what the top-level structure of the page is. If your page has an `<h4>` inside an `<h2>` section, the user thinks they have skipped two heading levels and something is broken.

The rule: **exactly one `<h1>` per page** (the page title), and headings should nest logically. A section that is conceptually a subsection of another should use the next heading level down.

Good:

```html
<h1>Without a Net, Part 2: Semantic HTML</h1>
  <h2>Part 1: The Theory</h2>
    <h3>HTML as a Typed Language</h3>
    <h3>The First Rule of ARIA</h3>
  <h2>Part 2: The Document</h2>
  <h2>Part 3: The Outline</h2>
    <h3>Headings</h3>
    <h3>Landmarks</h3>
```

Bad:

```html
<h1>Site name</h1>      <!-- repeated in header across the site -->
<h1>Page title</h1>     <!-- and again here, because it "looks right" -->
<h3>Section</h3>        <!-- h2 skipped because the designer didn't like h2's margin -->
<h5>Subsection</h5>     <!-- h4 skipped because nobody cared -->
```

This is easier to get right than it sounds. Rely on the HTML structure, not on the visual size. Adjust the visual size with CSS. If your brand guidelines say "the h1 looks like 24 pixels on mobile and 48 on desktop" — fine, but that is a styling choice, not a structural one.

**The sectioning elements.** HTML5 introduced a set of elements that group related content into named regions:

- `<main>` — the primary content of the page. There should be exactly one per page. It is a landmark — screen readers can jump straight to it, skipping the navigation.
- `<nav>` — a block of navigation links. Use it for the primary site navigation, the table of contents of a long article, and breadcrumbs. Do not use it for the footer "legal links" section — those are not primary navigation.
- `<header>` — the header of a page or a section. The top-level `<header>` of the document is the site header. But `<header>` inside an `<article>` is the article's header.
- `<footer>` — the footer of a page or a section. Same nesting rules as `<header>`.
- `<aside>` — content tangentially related to the surrounding content. Sidebars, pull quotes, "related articles" blocks. A common mistake is using `<aside>` for things that are not actually "aside" — for example, a cookie banner is a `<dialog>`, not an `<aside>`.
- `<article>` — a self-contained composition that could be distributed on its own: a blog post, a comment, a news item, a forum post.
- `<section>` — a thematic grouping of content, typically with a heading. Use sparingly — a `<section>` without a heading is usually wrong.
- `<search>` — a block of content containing a search form. Baseline Widely Available since 2024. Use it as the wrapper around your search input on the home page.
- `<address>` — contact information for the nearest `<article>` or `<body>` ancestor. Commonly misused for postal addresses in general — it is for *the* address of the author/owner of the enclosing content.

Here is the skeleton of a blog home page using all of those:

```html
<body>
  <header>
    <a href="/" rel="home">
      <img src="/logo.svg" alt="My Blazor Magazine">
    </a>
    <nav aria-label="Primary">
      <ul>
        <li><a href="/">Home</a></li>
        <li><a href="/blog">Blog</a></li>
        <li><a href="/about">About</a></li>
      </ul>
    </nav>
    <search>
      <form role="search" action="/search">
        <label for="q">Search</label>
        <input id="q" name="q" type="search">
      </form>
    </search>
  </header>

  <main>
    <h1>My Blazor Magazine</h1>

    <article>
      <header>
        <h2><a href="/blog/2026-05-25-no-build-web-html-semantics">Without a Net, Part 2</a></h2>
        <p>
          By <a rel="author" href="/authors/myblazor-team">My Blazor Team</a>
          on <time datetime="2026-05-25">25 May 2026</time>
        </p>
      </header>
      <p>HTML is not markup for making things look right...</p>
      <footer>
        <a href="/blog/2026-05-25-no-build-web-html-semantics">Read more →</a>
      </footer>
    </article>

    <article>
      <!-- ... another post ... -->
    </article>

    <aside aria-labelledby="related-heading">
      <h2 id="related-heading">Related reading</h2>
      <ul>
        <li><a href="/blog/2026-05-24-no-build-web-overview">Part 1 of the series</a></li>
      </ul>
    </aside>
  </main>

  <footer>
    <p>© 2026 My Blazor Magazine · <a href="/rss.xml">RSS</a></p>
    <address>
      Contact: <a href="mailto:hello@myblazor.example">hello@myblazor.example</a>
    </address>
  </footer>
</body>
```

A few things to notice in that snippet.

- There is **exactly one `<h1>`** ("My Blazor Magazine"). Each article's title is an `<h2>` — because the articles are subordinate to the page's main heading.
- Every `<nav>`, `<aside>`, and non-landmark `<header>`/`<footer>` that could be ambiguous has an `aria-label` or `aria-labelledby`. Without it, a screen reader user who encounters three `<nav>` elements on the same page has no way to distinguish between them.
- `<time datetime="2026-05-25">` is a microformat for machine-readable dates. Search engines use it. Assistive technologies can speak it correctly.
- `<a rel="author">` is a microformat for attribution. Also `<a rel="home">` on the logo link.
- The `<search>` element wraps a `<form>` that has `role="search"` on it. The `<search>` element itself has implicit `role="search"` — strictly, the `role="search"` on the `<form>` is redundant, but because `<search>` is only Baseline since 2024, we double it up for belt-and-braces safety on older devices. You can drop it in another year.

Type that into a file, load it, and press Tab. Then open the screen reader rotor (VoiceOver: Ctrl+Option+U; NVDA: NVDA+F7). Every landmark, every heading, every link is enumerated. You did not write any JavaScript. You did not add any ARIA. The semantics came free with the elements.

## Part 4: Text-Level Semantics

Inside your blocks, you have prose. Prose has semantic elements too, and most of us reach for `<span>` when we should not.

- **`<em>`** — stress emphasis (the word being emphasised changes the meaning of the sentence). *"I never said she stole my money"* vs *"I never said **she** stole my money."* Italicises by default.
- **`<strong>`** — strong importance. Use it for words that must not be missed: "Do **not** turn off your computer during the update." Bolds by default.
- **`<b>`** — keyword or product name, not importance. "The **iPhone 15** has an A17 chip." Bolds by default, but does not imply urgency to a screen reader.
- **`<i>`** — an alternate voice or mood, or a term in another language. "She whispered *je ne regrette rien* and left the room." Italicises by default.
- **`<u>`** — an unarticulated but explicitly rendered non-textual annotation. The canonical use is for misspellings. Rarely the right tool in 2026; underlines also suggest hyperlinks, which confuses users.
- **`<mark>`** — a highlight for relevance in the current context. Use it for search result highlighting: "`...the <mark>dialog</mark> element lets you...`".
- **`<cite>`** — the title of a creative work: "I'm reading *<cite>Middlemarch</cite>*." Do not use it for the author's name.
- **`<q>`** — a short inline quotation. The browser inserts the locale-appropriate quote marks. Do not type your own quote marks when using `<q>`.
- **`<blockquote>`** — a longer, block-level quotation. Can contain a `<cite>` element for attribution.
- **`<small>`** — small print: side comments, legal notices, copyright lines.
- **`<del>`** and **`<ins>`** — deleted and inserted content, for edit tracking. Paired with `datetime` attributes for machine-readable history.
- **`<kbd>`** — keyboard input: "Press <kbd>Ctrl</kbd>+<kbd>C</kbd> to copy."
- **`<samp>`** — sample program output.
- **`<code>`** — inline code. Not just styling — screen readers can announce it.
- **`<var>`** — a variable in a mathematical or programming context.
- **`<abbr>`** — an abbreviation or acronym. Add a `title` attribute for the expansion: `<abbr title="HyperText Markup Language">HTML</abbr>`.
- **`<dfn>`** — the defining instance of a term. "The <dfn>cascade</dfn> is the algorithm that determines which CSS rule wins."
- **`<ruby>`, `<rt>`, `<rp>`** — ruby annotations, primarily for East Asian typography.
- **`<bdi>`** and **`<bdo>`** — bidirectional isolation and override, for handling right-to-left languages inside left-to-right contexts or vice versa.
- **`<wbr>`** — a word-break opportunity. A hint to the browser that if it has to break a long word, this is a good spot.
- **`<br>`** — a line break. Use sparingly — it is a typographic break within a paragraph, not a layout tool.
- **`<span>`** — the "I need to style this with no other semantic" escape hatch. Use only after confirming none of the above fits.

When you look at that list, you start to notice: "huh, there really is an element for most of this." The bug we open every blog post with — Bilal's form — partly came from the developer habit of using `<div>` and `<span>` for everything. If you train yourself to reach for the semantic element first, you get meaning for free.

A quick case study. Consider this snippet in an article describing a keyboard shortcut:

```html
<p>To open DevTools press F12 on Windows or Cmd-Option-I on macOS.</p>
```

Now compare:

```html
<p>
  To open DevTools press
  <kbd>F12</kbd> on Windows or
  <kbd><kbd>Cmd</kbd>+<kbd>Option</kbd>+<kbd>I</kbd></kbd> on macOS.
</p>
```

The second version is visibly richer — your CSS can give `<kbd>` a subtle border and a monospace font, and nested `<kbd>` elements signal a keyboard combination. Screen readers can announce it differently from prose. Copy-paste preserves the semantics. Search engines index it correctly. None of that was possible with `<span class="kbd">`.

## Part 5: Links, Anchors, and the Thing That Is Not A Button

Two elements are so ubiquitous and so often misused that they deserve their own section: `<a>` and `<button>`.

**The rule:** if the thing you are building **navigates** (changes the URL, opens a new tab, downloads a file, jumps to an anchor), it is an `<a>`. If it **acts** (triggers logic on the current page), it is a `<button>`.

Mistakes:

1. A `<button>` that is really a navigation link, because somebody wanted to style it. Fix: use an `<a>` and style it to look like a button. `<a class="button">` is perfectly fine; CSS-wise, a link and a button can look identical.

2. An `<a>` with `href="#"` and an `onclick` handler. This is a "link that is really a button." If the user middle-clicks it hoping to open it in a new tab, the page jumps to the top. Fix: use a `<button type="button">`.

3. An `<a>` with no `href`. It is not focusable. It is not a link. It is a `<span>` that you typed four characters to make. Fix: use a `<button>`, or give the `<a>` an `href`.

4. A `<button>` that submits the form when you did not want it to. The default `type` of `<button>` inside a `<form>` is `submit`. If you want a button that does something else, write `type="button"` explicitly. This is the most common HTML bug on the modern web and every junior dev trips over it.

5. An `<a target="_blank">` without `rel="noopener"`. Before Chrome 88 (January 2021), opening a new tab gave the new page a reference to your page via `window.opener`, and phishing sites would use it to rewrite your URL. All modern browsers default to `rel="noopener"` for `target="_blank"` now, but old code may still include `rel="noopener noreferrer"` explicitly, which is still fine.

The `<a>` element has a handful of useful but less-known attributes:

- **`href="mailto:"`** opens the user's email client.
- **`href="tel:"`** opens the phone dialler on mobile.
- **`href="sms:"`** opens the SMS composer on mobile.
- **`download`** offers the linked resource as a download rather than navigating to it. Optionally provide a filename: `<a href="/report.pdf" download="Q4-2025-report.pdf">`.
- **`rel`** — a space-separated list of link types. Common values: `noopener`, `noreferrer`, `nofollow`, `author`, `home`, `next`, `prev`, `canonical`.
- **`hreflang`** — the language of the linked resource.
- **`ping`** — a list of URLs the browser will POST to when the link is clicked. Used for analytics. Blocked by most privacy-focused browsers.
- **`referrerpolicy`** — controls what referrer information is sent. Values: `no-referrer`, `origin`, `same-origin`, `strict-origin-when-cross-origin` (the default in 2026), etc.

`<button>` has its own set:

- **`type`** — `submit` (default inside a form), `reset`, or `button`. Always write this explicitly.
- **`form`** — the ID of a form this button submits, if it is outside the form in the DOM.
- **`formaction`**, **`formenctype`**, **`formmethod`**, **`formnovalidate`**, **`formtarget`** — per-button overrides of the parent form's corresponding attributes. Useful for "Save draft" vs "Publish" buttons that post to different URLs.
- **`popovertarget`** — the ID of a popover element this button toggles. We will get to popovers in Part 9.
- **`popovertargetaction`** — `toggle` (default), `show`, or `hide`.
- **`command`** and **`commandfor`** — an emerging declarative way to invoke actions on other elements without JavaScript. Not yet in all browsers; use with a JS fallback.

## Part 6: Lists — Not Just Bullet Points

Lists seem trivial. They are not. The list elements carry semantics that screen readers use to announce "list, seven items" before iterating. Using `<div>` with bullet images gets you visually similar results and is categorically worse for accessibility.

- **`<ul>`** — unordered list. The items are a collection where order does not matter. Think tags, navigation items, checkboxes.
- **`<ol>`** — ordered list. The items have a meaningful sequence. Think recipe steps, search result rankings, "top 10" lists. Attributes: `start` (starting number), `reversed` (count down), `type` (`1`, `A`, `a`, `I`, `i`).
- **`<li>`** — a list item.
- **`<dl>`** — a description list (formerly "definition list"). A set of name-value pairs.
- **`<dt>`** — a term.
- **`<dd>`** — a description.

The description list is the most underused of the three. Perfect for metadata:

```html
<dl>
  <dt>Author</dt>
  <dd><a href="/authors/myblazor-team">My Blazor Team</a></dd>
  <dt>Published</dt>
  <dd><time datetime="2026-05-25">25 May 2026</time></dd>
  <dt>Reading time</dt>
  <dd>42 minutes</dd>
  <dt>Tags</dt>
  <dd>html, semantic-html, accessibility, series</dd>
</dl>
```

A screen reader announces this as "Description list, four terms. Author, My Blazor Team. Published, 25 May 2026..." which is exactly what the content means. Styled CSS-wise, it can look like anything — two columns, inline definitions, tags, cards — whatever you want. The semantics are invariant.

A lesser-known detail: `<li>` inside `<ol>` has a `value` attribute that explicitly numbers that item:

```html
<ol>
  <li>First</li>
  <li>Second</li>
  <li value="10">Tenth</li>
  <li>Eleventh</li>
</ol>
```

This is useful for resumed lists (e.g. a pause for prose in the middle of a numbered list of steps).

## Part 7: Forms — The Single Most Underrated Element

Let us return to Bilal's bug. He had a `<div class="form">`. What would a `<form>` have given him?

Everything:

1. **Submission**. Pressing Enter in any text input submits the form. The browser collects every `<input>`, `<select>`, `<textarea>` that has a `name` attribute, bundles them into a form data payload, and either fires a `submit` event or navigates to the `action` URL.
2. **Validation**. `required` on an input makes it required. `type="email"` validates email format. `pattern="..."` applies a regex. The browser will refuse to submit and display an error bubble.
3. **Focus management**. The invalid field gets focused automatically on submit attempt.
4. **Assistive technology**. Screen readers announce "form" on entry, "out of form" on exit, and associate labels with inputs when the markup is correct.
5. **Autofill**. Browsers recognise `<form>` + `name="email"` + `autocomplete="email"` and autofill from the user's saved identities.
6. **Reset**. A `<button type="reset">` clears the form to its defaults.
7. **Backend compatibility**. A real `<form>` submits via `POST` with the correct content type. It works without JavaScript. Progressive enhancement is free.

We will devote all of Day 12 to forms. For now, here is the minimum correct markup for a sign-up form:

```html
<form action="/api/signup" method="post" novalidate>
  <p>
    <label for="signup-email">Email</label>
    <input
      id="signup-email" name="email" type="email"
      autocomplete="email" required
      aria-describedby="signup-email-help">
    <span id="signup-email-help">We'll send you a confirmation link.</span>
  </p>
  <p>
    <label for="signup-password">Password</label>
    <input
      id="signup-password" name="password" type="password"
      autocomplete="new-password"
      minlength="12" required>
  </p>
  <p>
    <button type="submit">Create account</button>
  </p>
</form>
```

A few things to notice:

- Every `<input>` has an associated `<label>` via `for`/`id`. This is non-negotiable. Screen readers read the label when the input is focused. Clicking the label focuses the input. Without a label, your input is anonymous.
- `autocomplete` values come from [a fixed vocabulary](https://developer.mozilla.org/en-US/docs/Web/HTML/Attributes/autocomplete) — `email`, `username`, `current-password`, `new-password`, `given-name`, `family-name`, `street-address`, `postal-code`, etc. The browser uses them to offer the right saved value. `autocomplete="new-password"` tells the browser this is a "new" password, which triggers password-manager password generation instead of offering a saved password.
- `required` makes the field required. The browser handles the error UI.
- `type="email"` changes the validation and, on mobile, brings up the email keyboard (with the `@` key visible).
- `aria-describedby` links the help text to the input. The screen reader reads both when the input is focused.
- `novalidate` on the form disables the browser's default error bubbles. Use it when you want to handle validation messages yourself. Without it, the browser shows its own errors, which is usually fine.

### Every input type

There are twenty-two input types in HTML as of 2026, and most developers use four. Here is the full list with notes:

- **`text`** — a single-line text input. The default.
- **`password`** — like text, but obscured.
- **`email`** — like text, but validates email format and uses the email keyboard on mobile.
- **`tel`** — like text, uses the phone keypad on mobile. Does not validate format (phone formats are too varied).
- **`url`** — validates as a URL, uses a URL-optimised keyboard on mobile.
- **`search`** — like text, but browsers may render a clear button, and mobile keyboards show a "Search" key.
- **`number`** — numeric input with spinner controls. Attributes: `min`, `max`, `step`. Use `type="text" inputmode="numeric"` for credit-card-like fields where you do not want spinners.
- **`range`** — a slider. Use for imprecise selections (brightness, volume). Not for precise numeric input.
- **`date`** — a date picker. Native on every platform. Attributes: `min`, `max`, `step` (in days).
- **`time`** — a time picker.
- **`datetime-local`** — date and time combined. Note the `-local` suffix. There is no `datetime` type anymore.
- **`month`** — year-month picker.
- **`week`** — ISO week picker.
- **`color`** — colour picker. Returns `#rrggbb`.
- **`checkbox`** — checkbox. Submits only when checked.
- **`radio`** — radio button. Group them by sharing a `name` attribute.
- **`file`** — file upload. Attributes: `accept` (MIME types or extensions), `multiple`, `capture` (mobile camera).
- **`hidden`** — invisible input, submitted with the form. Useful for CSRF tokens.
- **`submit`** — submit button. Prefer `<button type="submit">` for more flexible content.
- **`reset`** — reset button. Prefer `<button type="reset">`.
- **`button`** — generic button. Prefer `<button type="button">`.
- **`image`** — an image that acts as a submit button. Rarely useful; prefer `<button>` with an `<img>` inside.

Using the right type gets you:

- The right on-screen keyboard on mobile.
- Free browser-level validation.
- The right native picker UI (date, colour, file).
- Autofill from saved addresses, payment cards, passwords.
- Screen-reader announcements that match the type.

### `<label>` placement options

There are three ways to associate a label with an input:

```html
<!-- 1. Explicit association via for/id. Works everywhere. Preferred. -->
<label for="email">Email</label>
<input id="email" name="email" type="email">

<!-- 2. Wrap the input. Association is implicit. -->
<label>
  Email
  <input name="email" type="email">
</label>

<!-- 3. aria-label (no visible label). For icon-only buttons, search inputs. -->
<input name="q" type="search" aria-label="Search the site">
```

Method 1 is the most flexible for styling. Method 2 is convenient in templates but has some edge cases with styling. Method 3 should be used sparingly — sighted users benefit from visible labels too.

### `<fieldset>` and `<legend>`

A `<fieldset>` groups related form controls. A `<legend>` labels the group. The combination is the correct way to label a set of radio buttons:

```html
<fieldset>
  <legend>Plan</legend>
  <label><input type="radio" name="plan" value="free" checked> Free</label>
  <label><input type="radio" name="plan" value="pro"> Pro</label>
  <label><input type="radio" name="plan" value="team"> Team</label>
</fieldset>
```

The screen reader reads "Plan, radio group, Free, radio button, 1 of 3, selected." Without `<fieldset>` and `<legend>`, the group is meaningless and the user has to guess.

### `<output>` — the element we used in Day 1 already

`<output>` is a live region for computed values. It has an implicit `aria-live="polite"`, which means screen readers will announce changes to its content without interrupting. Perfect for calculated totals, live character counts, dynamic previews.

```html
<form>
  <label>Price: <input name="price" type="number" value="10"></label>
  <label>Quantity: <input name="qty" type="number" value="1"></label>
  <output name="total">10</output>
</form>
```

A trivial amount of JavaScript updates the `<output>` on `input` events, and the screen reader announces the new value each time.

### `<datalist>` — autocomplete, for free

```html
<label for="country">Country</label>
<input list="countries" id="country" name="country">
<datalist id="countries">
  <option value="Australia">
  <option value="Belgium">
  <option value="Canada">
  <!-- ... -->
</datalist>
```

The input is a normal text input, but the browser offers autocomplete suggestions from the `<datalist>`. No jQuery UI, no Select2, no React-Select. You can have thousands of options — the browser handles the filtering.

### Constraint Validation — the validation API you already have

Every form control exposes a `validity` object:

```javascript
const email = document.getElementById("signup-email");
email.addEventListener("blur", () => {
  if (email.validity.typeMismatch) {
    email.setCustomValidity("Please enter a valid email address.");
  } else {
    email.setCustomValidity("");
  }
});
```

`validity` has eight boolean properties: `valueMissing`, `typeMismatch`, `patternMismatch`, `tooLong`, `tooShort`, `rangeUnderflow`, `rangeOverflow`, `stepMismatch`, `badInput`, and `customError`. You can read them without writing any validation logic — the browser has already computed the state for you. `setCustomValidity("some message")` marks the field invalid with your message; `setCustomValidity("")` clears it. We will come back to this in full detail on Day 12.

## Part 8: Tables — The Elements That Survived HTML's War Against Themselves

In 2003, Jeffrey Zeldman wrote *Designing With Web Standards*, and for about fifteen years after, the mainstream web-developer opinion on tables was "do not use them for layout." That was the right lesson. Unfortunately it morphed into "do not use tables at all", which is the wrong lesson. **Tables are for tabular data.** If you have rows of records with columns of fields, that is a table.

The full table vocabulary:

```html
<table>
  <caption>Blog posts in May 2026</caption>
  <colgroup>
    <col>
    <col>
    <col span="2" class="author-columns">
  </colgroup>
  <thead>
    <tr>
      <th scope="col">Date</th>
      <th scope="col">Title</th>
      <th scope="col">Author</th>
      <th scope="col">Reading time</th>
    </tr>
  </thead>
  <tbody>
    <tr>
      <th scope="row"><time datetime="2026-05-24">24 May</time></th>
      <td><a href="...">Without a Net, Part 1</a></td>
      <td>My Blazor Team</td>
      <td>36 min</td>
    </tr>
    <tr>
      <th scope="row"><time datetime="2026-05-25">25 May</time></th>
      <td><a href="...">Without a Net, Part 2</a></td>
      <td>My Blazor Team</td>
      <td>42 min</td>
    </tr>
  </tbody>
  <tfoot>
    <tr>
      <th scope="row" colspan="3">Total reading time</th>
      <td>78 min</td>
    </tr>
  </tfoot>
</table>
```

Things to notice:

- **`<caption>`** is the table's title. Screen readers announce it when entering the table. Looks by default like a centred line above the table; style it however you want.
- **`<colgroup>` and `<col>`** let you style entire columns without touching individual cells. `col:nth-child(2) { background: #fafafa; }` stripes a column.
- **`<thead>`, `<tbody>`, `<tfoot>`** are semantic groupings. You can have multiple `<tbody>` elements for grouped rows.
- **`<th scope="col">`** marks column headers. `<th scope="row">` marks row headers. Screen readers use `scope` to announce "Title, Without a Net, Part 2" when the user navigates to that cell.
- **`colspan`** and **`rowspan`** merge cells.

In 2026 there is *no reason* to use a `<div>`-based grid of data instead of a `<table>`. CSS on tables is excellent. You can make tables responsive with `display: block` on small screens, with container queries, or with the pattern we build in Day 5. Tables have styling options that `<div>` grids do not have: `border-collapse`, `table-layout: fixed`, column selectors.

This magazine's `ResponsiveTable` Blazor component in the existing codebase uses a real `<table>` underneath. You should too.

## Part 9: `<dialog>` — The End of Modal Libraries

This is where developers who have not kept up with HTML get a delightful surprise. For about fifteen years, every sufficiently complex JS app has shipped a modal library — Bootstrap Modal, SweetAlert, React-Modal, MUI Dialog, Reach UI, Headless UI, Radix Dialog. Each one reimplements:

- Focus trapping (tab stays inside the modal).
- Focus return (closing the modal returns focus to the opener).
- Backdrop.
- Escape-to-close.
- Click-outside-to-close.
- Scroll locking (page does not scroll behind the modal).
- Z-index layering (modal is on top of everything).
- ARIA semantics (`role="dialog"`, `aria-modal="true"`).
- Accessibility (screen readers announce the modal as a dialog).

Every one of those is a built-in browser behaviour of `<dialog>` in 2026. [`<dialog>` has been Baseline Widely Available since March 2022](https://developer.mozilla.org/en-US/docs/Web/HTML/Reference/Elements/dialog).

The basic API:

```html
<button id="open-signup">Sign up</button>

<dialog id="signup-dialog">
  <form method="dialog">
    <h2>Sign up</h2>
    <p>
      <label for="d-email">Email</label>
      <input id="d-email" name="email" type="email" required>
    </p>
    <menu>
      <button value="cancel" formnovalidate>Cancel</button>
      <button value="create" type="submit">Create account</button>
    </menu>
  </form>
</dialog>

<script type="module">
  const button = document.getElementById("open-signup");
  const dialog = document.getElementById("signup-dialog");
  button.addEventListener("click", () => dialog.showModal());
  dialog.addEventListener("close", () => {
    if (dialog.returnValue === "create") {
      // The user submitted the form.
    }
  });
</script>
```

That is a full, accessible, keyboard-navigable, focus-trapped sign-up modal in 25 lines of HTML and JavaScript combined. Things you are getting for free:

- `showModal()` puts the dialog in the browser's **top layer**, which means it renders on top of everything else in the document regardless of z-index.
- The rest of the page becomes **inert** — not focusable, not clickable, invisible to screen readers.
- The Escape key closes the dialog and fires a `cancel` event.
- `<form method="dialog">` inside the dialog submits the form *and closes the dialog*, setting `returnValue` to the `value` attribute of the button that submitted it.
- Focus is restored to the opening button when the dialog closes.
- A `::backdrop` pseudo-element is available for styling the backdrop: `dialog::backdrop { background: rgba(0, 0, 0, 0.5); }`.

There are three opening methods:

- **`dialog.showModal()`** — modal; page behind is inert; Escape closes; recommended.
- **`dialog.show()`** — non-modal; page behind is still interactive; Escape does not close; use sparingly.
- **Setting the `open` attribute directly** — also non-modal; *not recommended*, because it skips the top-layer promotion.

There are two closing methods:

- **`dialog.close(returnValue?)`** — closes immediately; fires `close` event; not cancellable.
- **`dialog.requestClose(returnValue?)`** — fires `cancel` first; if that event's default is prevented, the dialog stays open. Useful for "are you sure?" confirmations. [This is newer — not Baseline yet as of early 2026](https://developer.mozilla.org/en-US/docs/Web/API/HTMLDialogElement/requestClose) — so feature-detect before using.

There is a newer HTML attribute on `<dialog>` called `closedby` that lets you declare the dismissal behaviour:

- `closedby="none"` — only explicit close.
- `closedby="closerequest"` — Escape or OS-level close gesture.
- `closedby="any"` — Escape or a click on the backdrop.

This attribute is also newer than the base `<dialog>` element (shipping through 2025), so test for it.

### Dialog styling

By default, an open `<dialog>` centres itself on screen, draws a plain border, and has a white background. Four CSS techniques to know:

1. **Size.** `dialog { max-width: 90vw; max-inline-size: 40rem; }`. The default sizing is "fit content", which is usually wrong for longer forms.
2. **Backdrop.** `dialog::backdrop { background: oklch(0% 0 0 / 0.5); backdrop-filter: blur(4px); }`.
3. **Enter animation.** `dialog[open] { animation: fade-in 200ms ease-out; }`. Use `@starting-style` for a real enter animation that also animates the backdrop.
4. **Exit animation.** Because `<dialog>` uses the top layer, exit animations need `transition` + `overlay: auto` + `display: block` with `allow-discrete`. There is a [Chrome for Developers article](https://developer.chrome.com/blog/entry-exit-animations) on this.

We will come back to `<dialog>` in Day 6 (motion) and Day 10 (custom elements), where we build a reusable `<confirm-dialog>` on top of it.

## Part 10: `<details>` and `<summary>` — The Accordion Nobody Needed To Write

If `<dialog>` kills the modal library, `<details>` + `<summary>` kills the accordion library.

```html
<details>
  <summary>What are the browser requirements?</summary>
  <p>We support the current versions of Chrome, Edge, Firefox, and Safari.</p>
</details>

<details open>
  <summary>Is this free?</summary>
  <p>Yes.</p>
</details>
```

Behaviours you get for free:

- Click the summary to toggle.
- Summary is focusable with the Tab key.
- Enter or Space on the summary toggles.
- Screen readers announce "disclosure, collapsed" / "disclosure, expanded."
- The `open` attribute reflects state. Changing it opens/closes the element.
- A `toggle` event fires when state changes.
- The `name` attribute groups multiple `<details>` into an exclusive set — only one open at a time. Perfect for FAQ-style accordions:

```html
<details name="faq">
  <summary>First question?</summary>
  <p>Answer.</p>
</details>
<details name="faq">
  <summary>Second question?</summary>
  <p>Answer.</p>
</details>
```

That `name` grouping was the final feature that made the element a real accordion replacement; it has been Baseline since late 2024.

For styling, the `::details-content` pseudo-element targets the content area separately from the summary. `details[open]` is a CSS selector. You can animate the open/close transition using `interpolate-size: allow-keywords` on the `html` element — this lets you animate from `height: 0` to `height: auto`, which used to be impossible.

## Part 11: The Popover API — The Other Thing That Killed The Modal Library

[The Popover API became Baseline Newly Available in January 2025](https://web.dev/baseline), and it is, in many ways, more useful than `<dialog>`. A popover is any element that pops up, is associated with an invoker, and dismisses itself via Escape or click-outside.

```html
<button popovertarget="help">Help</button>

<div id="help" popover>
  <h3>Help</h3>
  <p>This is a popover. Click outside or press Escape to close.</p>
</div>
```

That is the entire example. No JavaScript. The button knows which element to toggle because of `popovertarget`. The popover is positioned by the browser in the top layer. Escape closes it. Clicking outside closes it.

Three variants of the `popover` attribute:

- **`popover` or `popover="auto"`** (default) — "light dismiss" behaviour. Clicking outside closes it. Only one auto popover can be open at a time at a given level. Perfect for menus, tooltips, context panels.
- **`popover="manual"`** — you control open/close entirely. Clicking outside does not close it. Useful for notification toasts.
- **`popover="hint"`** — newer value for tooltip-style popovers that should not steal focus.

Programmatic control:

```javascript
const popover = document.getElementById("help");
popover.showPopover();       // open
popover.hidePopover();       // close
popover.togglePopover();     // toggle
popover.togglePopover(true); // force-open
popover.togglePopover(false);// force-close
```

Events:

- `beforetoggle` — fires before the state changes. Can be prevented with `preventDefault()`.
- `toggle` — fires after the state changes. `event.newState` is `"open"` or `"closed"`.

Popover vs. dialog:

- **Popover** is for lightweight, dismissible UI: menus, tooltips, command palettes, notification toasts.
- **Dialog** is for blocking modals: confirmations, forms, critical decisions.
- You can combine them — a `<dialog popover="auto">` is a non-modal dialog in the top layer with light-dismiss.

### Anchor positioning

Popovers often need to be positioned relative to their invoker. The old way was `getBoundingClientRect()` + `position: fixed` + JavaScript. The new way is CSS anchor positioning, which is in Interop 2025 and has now shipped in all three major engines (with Firefox a few months behind Chrome and Safari).

```html
<button id="help-btn" popovertarget="help">Help</button>
<div id="help" popover anchor="help-btn">...</div>

<style>
  #help {
    position-anchor: --help-btn-anchor;
    top: anchor(bottom);
    left: anchor(left);
  }
</style>
```

We will not linger on this today; Day 10 has a full build on anchor positioning. The key fact for today is: **the browser knows how to position a popover relative to an invoker**, and you can declare that relationship in HTML and CSS rather than reimplementing it in JS.

## Part 12: `<progress>` and `<meter>` — Two Elements You Probably Ignored

- **`<progress value="30" max="100">30%</progress>`** — a progress bar. Indeterminate (no `value`) or determinate. Styleable across browsers with `::-webkit-progress-bar`, `::-webkit-progress-value`, `::-moz-progress-bar`. The fallback text inside is shown in very old browsers.
- **`<meter value="0.6" low="0.2" high="0.8" optimum="0.5">60%</meter>`** — a known, fixed measurement within a range. Disk usage, password strength, sentiment score. The colour changes based on where the value falls (low, high, optimum). Not to be confused with `<progress>` — `<progress>` is "how far through a task are we?"; `<meter>` is "where does this value sit on a scale?"

Both are accessible by default — screen readers announce percentages. You are not allowed to write a "password strength meter" out of three `<div>`s in 2026 when `<meter>` exists.

## Part 13: Media — `<img>`, `<picture>`, `<video>`, `<audio>`

The media elements deserve their own article, but the essentials:

### `<img>`

```html
<img
  src="/images/hero.avif"
  srcset="/images/hero-400.avif 400w, /images/hero-800.avif 800w, /images/hero-1600.avif 1600w"
  sizes="(min-width: 60rem) 50vw, 100vw"
  width="1600"
  height="900"
  alt="A crowd of developers at a conference, looking at a screen with code."
  loading="lazy"
  decoding="async"
  fetchpriority="low">
```

Every attribute earns its place:

- **`srcset`** and **`sizes`** — responsive images. The browser picks the smallest suitable file.
- **`width`** and **`height`** — reserve space before the image loads, preventing Cumulative Layout Shift.
- **`alt`** — required on every `<img>`. Describe the content and function. Decorative images get `alt=""` (empty, not missing), which tells the screen reader to skip.
- **`loading="lazy"`** — native lazy-loading. Images below the fold are not fetched until they approach the viewport. Works without any JavaScript.
- **`decoding="async"`** — the browser can decode the image on a worker thread rather than blocking the main thread.
- **`fetchpriority`** — `high`, `low`, or `auto`. Use `high` on your LCP image; use `low` on below-the-fold icons.

### `<picture>`

For art-direction (serving different images, not just different sizes, based on viewport or format):

```html
<picture>
  <source type="image/avif" srcset="/hero.avif">
  <source type="image/webp" srcset="/hero.webp">
  <img src="/hero.jpg" alt="..." width="1600" height="900">
</picture>
```

Browsers pick the first `<source>` they can handle. The `<img>` is the fallback.

### `<video>` and `<audio>`

```html
<video
  src="/intro.mp4"
  poster="/intro-poster.jpg"
  controls
  playsinline
  muted
  preload="metadata"
  width="1280"
  height="720">
  <track kind="captions" src="/intro.en.vtt" srclang="en" label="English" default>
  <track kind="captions" src="/intro.fr.vtt" srclang="fr" label="Français">
</video>
```

Key attributes:

- **`controls`** — show the native play/pause UI.
- **`playsinline`** — on iOS, play inline rather than fullscreen by default.
- **`muted`** — required on iOS for autoplay.
- **`preload`** — `none`, `metadata`, `auto`. Default is `metadata` in most browsers.
- **`poster`** — thumbnail image shown before play.
- **`<track>`** — captions and subtitles. A `<track kind="captions">` is required for accessibility. The `default` attribute shows that track automatically.

## Part 14: ARIA — And When Not To Use It

We have spent most of this article telling you which native elements replace which library. The logical question: "when *do* I use ARIA?"

**Rule one: never when a native element will do.** We said this already. It is the single most important rule.

**Rule two: use `aria-label` and `aria-labelledby` when you need a label that is not visible.** Icon-only buttons are the canonical case: `<button aria-label="Close"><svg>...</svg></button>`. But remember — most users benefit from visible labels. Only hide labels when space is genuinely constrained.

**Rule three: use `aria-describedby` to associate supplementary text with a control.** Form hints, error messages, help tooltips.

**Rule four: use `aria-live` on regions that update dynamically without user action.** `polite` (wait until the user is idle), `assertive` (interrupt immediately, for errors), `off` (do not announce). Use `assertive` sparingly — it is a loud interruption.

**Rule five: use `role` only to patch semantics that are missing.** `role="alert"` on an inline error message that appears asynchronously. `role="status"` for a quieter announcement. `role="log"` for an append-only log region. `role="tablist"`, `role="tab"`, `role="tabpanel"` if you are building tabs (no native HTML element for tabs — yet; it is being specced).

**Rule six: never use `role="presentation"` to "un-semantic" an element.** If the element should have no semantics, use a `<div>` or `<span>`.

**Rule seven: learn the [ARIA 1.2 spec](https://www.w3.org/TR/wai-aria-1.2/) and the [ARIA Authoring Practices Guide](https://www.w3.org/WAI/ARIA/apg/).** They are dry reading, but they are the reference when the native element does not exist.

## Part 15: `inert` — The Attribute That Makes Subtrees Disappear

The `inert` attribute makes an element and its entire subtree:

- Unfocusable (keyboard cannot tab into it).
- Unclickable (pointer events pass through).
- Invisible to assistive technologies.
- Visually unchanged (you style the visual difference yourself).

```html
<aside inert>
  <p>This sidebar is currently disabled.</p>
  <button>Click me</button>  <!-- focusable? No. -->
</aside>
```

The classic use case: when you show a non-modal UI that should temporarily disable the rest of the page (e.g. a confirmation state, a multi-step wizard where the other steps are disabled), wrap the disabled content in `inert`. It has been Baseline since late 2023 and is much cleaner than the old trick of toggling `tabindex="-1"` on every focusable child.

A very common mistake in 2026 React code: setting `inert` as a prop. In HTML, `inert` is a boolean attribute; it is *present or absent*. `inert="false"` is truthy. Use `inert` (no value) or remove it. In JavaScript: `element.inert = true` / `element.inert = false`.

## Part 16: What Bilal Should Have Written

Remember Bilal's sign-up form? Here is what the correct version looks like — all HTML, no JavaScript, fully accessible, with real validation, native focus management, and screen-reader-friendly error reporting:

```html
<form action="/api/signup" method="post">
  <fieldset>
    <legend>Create your account</legend>

    <p>
      <label for="signup-email">Email</label>
      <input
        id="signup-email" name="email" type="email"
        autocomplete="email" required
        aria-describedby="signup-email-error">
      <span id="signup-email-error" aria-live="polite"></span>
    </p>

    <p>
      <label for="signup-password">Password (12+ characters)</label>
      <input
        id="signup-password" name="password" type="password"
        autocomplete="new-password"
        minlength="12" required
        aria-describedby="signup-password-error">
      <span id="signup-password-error" aria-live="polite"></span>
    </p>

    <p>
      <button type="submit">Create account</button>
    </p>
  </fieldset>
</form>
```

This form:

- Works without JavaScript.
- Submits on Enter in any field.
- Autofills from the password manager.
- Triggers the email keyboard on mobile.
- Validates on submit; the browser focuses the first invalid field.
- Associates labels with inputs (clicking label focuses input).
- Announces errors to screen readers.
- Is keyboard-navigable.
- Is styled with plain CSS; no framework.

Zero lines of JavaScript. Zero ARIA hacks. Zero libraries.

When the back end responds with a validation error for "email is already taken", a ~30-line JavaScript enhancement populates the `<span id="signup-email-error">` element and calls `emailInput.setCustomValidity("Email already in use")`. The browser takes it from there — announces the error, focuses the field, refuses to resubmit until the field changes. We will write that enhancement on Day 12.

## Part 17: A Checklist

Bookmark this section. This is the HTML checklist you can run against any page you build.

### Document

- [ ] `<!doctype html>` at the top.
- [ ] `<html lang="...">` with an accurate language.
- [ ] `<meta charset="utf-8">` as the first child of `<head>`.
- [ ] `<meta name="viewport" content="width=device-width, initial-scale=1">`.
- [ ] Meaningful `<title>`.
- [ ] `<meta name="description">`.
- [ ] `<link rel="canonical">` on pages that might be reachable via multiple URLs.

### Structure

- [ ] Exactly one `<h1>`.
- [ ] Heading levels nest logically — no skipped levels.
- [ ] Exactly one `<main>`.
- [ ] `<header>`, `<footer>`, `<nav>`, `<aside>` used as landmarks where appropriate.
- [ ] Multiple `<nav>` elements have distinct `aria-label` or `aria-labelledby` values.
- [ ] `<article>` for self-contained items; `<section>` for thematic groupings with headings.

### Interactive elements

- [ ] `<button type="button">` for on-page actions; `<a href="...">` for navigation.
- [ ] `<button>`s inside `<form>`s have explicit `type` (`submit` or `button`).
- [ ] Every `<a>` has a meaningful link text ("Read more" alone is not meaningful).
- [ ] `<a target="_blank">` does not need `rel="noopener"` — the browser defaults to it in 2026 — but leave it in if it is already there.

### Forms

- [ ] Every input has a `<label>` (visible or via `aria-label`).
- [ ] `<input type>` matches the data: `email`, `tel`, `url`, `date`, etc.
- [ ] `autocomplete` is set on identity fields.
- [ ] `required`, `minlength`, `maxlength`, `pattern`, `min`, `max` on fields where appropriate.
- [ ] Related fields grouped with `<fieldset>` + `<legend>` (especially radio groups and checkboxes).
- [ ] Error messages associated with inputs via `aria-describedby`.
- [ ] Dynamic errors in an `aria-live="polite"` region.

### Media

- [ ] Every `<img>` has a descriptive `alt` (or `alt=""` for decoration).
- [ ] `<img>` has `width` and `height` set.
- [ ] Images below the fold have `loading="lazy"`.
- [ ] `<video>` has `<track kind="captions">`.

### Modals and popovers

- [ ] Modal dialogs use `<dialog>` + `showModal()`, not a `<div>` with `z-index: 9999`.
- [ ] Dismissible menus and tooltips use `popover`, not a JavaScript implementation.
- [ ] Accordions use `<details>`/`<summary>`.

### ARIA

- [ ] No ARIA role duplicating a native element (`<button role="button">`, `<nav role="navigation">` are both redundant).
- [ ] No `role="presentation"` or `role="none"` on elements that need semantics.
- [ ] `aria-live` regions used only where content changes dynamically.

### Language

- [ ] `lang` on any element whose content is in a language other than the document language.

If every page you write passes this checklist, you will be in the top five percent of HTML authors on the web, and many of your accessibility tickets will vanish before they are filed.

## Part 18: What We Are Building Toward

You now have the vocabulary for semantic HTML. You know the document outline. You know landmarks. You know every input type. You know that `<dialog>`, `<details>`, and the Popover API replace three entire categories of library. You know when to reach for ARIA and when not to.

In the capstone we are building over this series, every piece of HTML we produce will be semantic and accessible. The blog post list will use `<article>` elements. The detail page will use `<main>` with an `<article>` that has a proper `<header>`. The search will live inside a `<search>` element. The tags will be a `<ul>`. The metadata will be a `<dl>`. The confirm-clear-drafts dialog will be a `<dialog>` with a real `<form method="dialog">`. The "what's this?" popups will be `popover` elements. The theme toggle will be a `<button type="button">`. The RSS feed link in the footer will be a plain `<a>`.

Tomorrow, we start dressing the bones.

## Part 19: A Note On Typing

One practical tip before we close. If you are in VS Code, install the extension [WebHint](https://marketplace.visualstudio.com/items?itemName=webhint.vscode-webhint). It flags most of the semantic errors we discussed in real time. It is zero-config. It catches missing `alt`, missing `lang`, duplicate IDs, misused headings, and a long list of other things. It is a free, offline replacement for 80 percent of what a build-time linter would do, and you do not need a build to use it.

VS Code itself has built-in HTML IntelliSense from [`vscode-html-languageservice`](https://github.com/microsoft/vscode-html-languageservice). When you type `<i`, it suggests `<img>`, `<input>`, `<i>`. When you type `<img `, it lists every valid attribute. When you type an attribute value, it suggests valid values where known. This is essentially the same LSP server that TypeScript uses for JavaScript, applied to HTML. You can get the same experience in Rider and Visual Studio; the tooling is universal.

## Part 20: Tomorrow

Tomorrow — **Day 3: The Cascade, Specificity, and `@layer` — Organising CSS Without a Framework** — we start on CSS. We are going to build the mental model of the cascade from first principles, demystify specificity (no more "0,0,1,0" incantations from memory), and introduce `@layer` and `@scope`, the two features that made CSS-in-JS largely unnecessary.

Bring coffee. CSS is not as scary as you remember.

---

## Series navigation

You are reading **Part 2 of 15**.

- [Part 1: Overview — Why a Plain Browser Is Enough in 2026](/blog/2026-05-24-no-build-web-overview)
- **Part 2 (today): Semantic HTML and the Document Outline Nobody Taught You**
- Part 3 (tomorrow): The Cascade, Specificity, and `@layer`
- Part 4: Modern CSS Layout — Flexbox, Grid, Subgrid, and Container Queries
- Part 5: Responsive Design in 2026
- Part 6: Colour, Typography, and Motion
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

- [MDN: HTML Reference](https://developer.mozilla.org/en-US/docs/Web/HTML/Reference) — every element, every attribute.
- [WHATWG HTML Living Standard](https://html.spec.whatwg.org/) — the spec itself. Readable in places, forbidding in others.
- [MDN: HTMLDialogElement](https://developer.mozilla.org/en-US/docs/Web/API/HTMLDialogElement) — complete reference for `<dialog>`.
- [MDN: Popover API](https://developer.mozilla.org/en-US/docs/Web/API/Popover_API) — complete reference.
- [WAI-ARIA Authoring Practices Guide](https://www.w3.org/WAI/ARIA/apg/) — design patterns for custom widgets, with HTML-first alternatives listed where they exist.
- [WebAIM Screen Reader User Survey](https://webaim.org/projects/screenreadersurvey10/) — empirical data on which screen readers your users actually use.
- [The A11y Project checklist](https://www.a11yproject.com/checklist/) — a practical accessibility checklist to run alongside ours.
- [The HTML5 outline algorithm — and why it isn't used](https://adrianroselli.com/2016/08/there-is-no-document-outline-algorithm.html) — classic post by Adrian Roselli explaining why you should still nest headings manually.
- [web.dev Learn HTML](https://web.dev/learn/html/) — Google's free, comprehensive HTML course.
- [The Rules of ARIA](https://www.w3.org/TR/using-aria/) — the normative rules, including the five that start with "do not use ARIA if...".
