---
title: "Without a Net, Part 10: Web Components — Custom Elements, Shadow DOM, Templates, and Slots"
date: 2026-06-02
author: myblazor-team
summary: "Day 10 of our fifteen-part no-build web series covers Web Components — the platform's native answer to 'components without a framework.' Custom elements with full lifecycle hooks, Shadow DOM for style encapsulation, Declarative Shadow DOM for server rendering, slots for composition, ElementInternals for form-associated components, and custom states for CSS targeting. We build a reusable <confirm-dialog> wrapping the native <dialog> element, an accessible <tabs> component with keyboard navigation, a <live-clock> that ticks by itself, and a form-associated <rating-input>. Zero dependencies, full accessibility, and components that work identically in React, Vue, Blazor, or plain HTML."
tags:
  - javascript
  - web-components
  - custom-elements
  - shadow-dom
  - declarative-shadow-dom
  - slots
  - element-internals
  - accessibility
  - no-build
  - first-principles
  - series
  - deep-dive
---

## Introduction: The Component That Outlived Three Frameworks

A former colleague, a senior engineer we worked with in 2017, built a date picker. The team was on Angular 1 at the time. She wrote it as an Angular 1 directive — about 500 lines of code, with controller, template, link function, the whole ceremony — and shipped it. Everyone on the team loved it. It had keyboard navigation, ARIA support, localisation, range selection, the full story.

In 2018 the team migrated to Angular 2. The date picker had to be rewritten. Same feature set, different framework, 600 lines this time because Angular 2 components are chattier. About six weeks of effort, because the date picker had accumulated edge cases and the test suite had to be ported to Jasmine-new-spelling.

In 2020 the team migrated to React. The date picker had to be rewritten again. 700 lines this time — React's useReducer for the range-selection logic, useEffect for the click-outside handler, forwardRef for composition, the usual. Four weeks.

In 2023 the team started evaluating a migration to Qwik. The date picker was one of the items on the concerns list.

In 2024 our colleague — who had moved on from that team years before, but was occasionally asked to consult — sat down and rebuilt the date picker as a Web Component. 400 lines of plain JavaScript, one file, no framework. She gave it to the team. It dropped in without changes. It still works. The React team uses it as `<date-picker>`. When they migrate to Qwik, they will use it as `<date-picker>`. If they ever migrate back to Angular, it will still be `<date-picker>`. If they open the component in a plain HTML file with no framework at all, it is still `<date-picker>`. Each migration saves four to seven weeks.

This is the pitch for Web Components in one story. They are components that survive framework changes, because they depend on the platform, not on a framework. They are what you would build if you wanted the things you build this year to still work in 2040.

Day 10 is our tour of the Web Components specification. We will cover Custom Elements with full lifecycle hooks, Shadow DOM with its encapsulation model, Declarative Shadow DOM for server rendering, `<template>` and `<slot>` for composition, `ElementInternals` for form association and accessibility, and the newer `:state()` pseudo-class for CSS hooks. We will build four components you can paste into your project today: a `<confirm-dialog>`, a `<tabs>` container, a `<live-clock>`, and a form-associated `<rating-input>`. By the end, you will understand the entire native component model and be able to decide — meaningfully — when to reach for a framework and when to use the platform directly.

This is Part 10 of 15 in our no-build web series. We have HTML, CSS, modules, DOM primitives, and state management. Today we assemble them into the unit we all think in: a reusable component.

## Part 1: The Four Pillars

Web Components are not one technology. They are four specifications that compose:

1. **Custom Elements** — register your own HTML tag name with a JavaScript class that defines its behaviour.
2. **Shadow DOM** — a private DOM subtree that isolates markup and styles from the rest of the page.
3. **HTML Templates** — the `<template>` element, inert markup that can be cloned and inserted later.
4. **ES Modules** — how we ship and load the code that defines the component (covered in depth in [Day 7](/blog/2026-05-30-no-build-web-es-modules)).

You can use Custom Elements without Shadow DOM. You can use Shadow DOM on native elements without Custom Elements. You can use `<template>` for any reason. They are four orthogonal tools. Most production Web Components use all four.

The browser support story is excellent. [Custom Elements and Shadow DOM have been Baseline since 2020](https://caniuse.com/custom-elementsv1). [Declarative Shadow DOM became Baseline Newly Available in August 2024, and is expected to reach Widely Available in August 2026](https://web-platform-dx.github.io/web-features-explorer/features/declarative-shadow-dom/) — effectively now. [Form-Associated Custom Elements](https://webkit.org/blog/13711/elementinternals-and-form-associated-custom-elements/) are supported in Chrome, Safari, and Firefox — the last of which has partial ARIA support, which we will note where relevant.

## Part 2: The Simplest Possible Custom Element

Let us start absolutely minimal. We will define an element called `<hello-world>` that, when placed in HTML, renders "Hello, world!":

```javascript
class HelloWorld extends HTMLElement {
  connectedCallback() {
    this.textContent = "Hello, world!";
  }
}

customElements.define("hello-world", HelloWorld);
```

```html
<hello-world></hello-world>
```

That is a working Web Component. The browser recognises `<hello-world>`, upgrades it to an instance of the `HelloWorld` class, and calls `connectedCallback` when the element is added to the DOM. The class extends `HTMLElement`, which means your element is a full citizen — it has all the methods, properties, and events that any HTML element has.

Three rules about the tag name:

1. **Must contain a hyphen.** `<hello-world>` works. `<helloworld>` does not. This is how the browser distinguishes custom elements from built-in ones — and it is a permanent guarantee that HTML will never add a built-in element with a hyphen in its name.
2. **Must start with a lowercase letter.** `<hello-world>` works; `<Hello-World>` does not.
3. **Must not be a reserved name.** There is a short list (`annotation-xml`, `color-profile`, `font-face`, `font-face-src`, `font-face-uri`, `font-face-format`, `font-face-name`, `missing-glyph`) that you cannot use.

A defensive way to check if a name is already registered:

```javascript
if (!customElements.get("hello-world")) {
  customElements.define("hello-world", HelloWorld);
}
```

Useful when your component module might be loaded twice (for example, if two modules depend on it). Calling `define` twice with the same name throws.

## Part 3: The Full Lifecycle

Custom elements have five lifecycle callbacks, each fired at a specific point:

```javascript
class Example extends HTMLElement {
  static observedAttributes = ["name", "count"];

  constructor() {
    super();
    // Runs once, when the element is created.
    // DO NOT access attributes or children here — they may not exist yet.
    // Good for: initialising internal state, attaching Shadow DOM.
  }

  connectedCallback() {
    // Runs when the element is inserted into the DOM.
    // Safe to access attributes, children, and the document.
    // Can fire multiple times if the element is moved.
    // Good for: setting up event listeners, rendering, fetching data.
  }

  disconnectedCallback() {
    // Runs when the element is removed from the DOM.
    // Good for: cleanup — timers, event listeners, network requests.
  }

  attributeChangedCallback(name, oldValue, newValue) {
    // Runs when an observed attribute changes.
    // Only fires for attributes listed in `observedAttributes`.
    // Fires once during initial parse for each present observed attribute.
  }

  adoptedCallback() {
    // Runs when the element is moved between documents.
    // Rare — typically only when using <iframe> adoption.
  }
}
```

The crucial pair is **`connectedCallback`** and **`disconnectedCallback`**. They bracket the time the element is live in the DOM. Setup in the former, cleanup in the latter. The `AbortController` pattern from Day 8 makes cleanup trivial:

```javascript
class Example extends HTMLElement {
  #controller;

  connectedCallback() {
    this.#controller = new AbortController();
    const { signal } = this.#controller;

    this.addEventListener("click", this.#handleClick, { signal });
    document.addEventListener("keydown", this.#handleKey, { signal });
    window.addEventListener("resize", this.#handleResize, { signal });
  }

  disconnectedCallback() {
    this.#controller.abort();
  }

  #handleClick = (event) => { /* ... */ };
  #handleKey = (event) => { /* ... */ };
  #handleResize = () => { /* ... */ };
}
```

One `controller.abort()` call removes every listener, cancels every fetch, and cleans up any other abortable operation. This is the pattern we use in every component we write.

### Observed attributes

```javascript
static observedAttributes = ["disabled", "variant", "count"];

attributeChangedCallback(name, oldValue, newValue) {
  if (name === "disabled") this.#render();
  if (name === "variant") this.#applyVariant(newValue);
  if (name === "count") this.#count = Number(newValue);
}
```

The `observedAttributes` static property declares which attributes trigger `attributeChangedCallback`. This is a performance optimisation — the browser only notifies you of changes to attributes you care about.

For attributes that also have a JavaScript property shape, expose both:

```javascript
class MyElement extends HTMLElement {
  static observedAttributes = ["count"];

  get count() {
    return Number(this.getAttribute("count") ?? 0);
  }
  set count(value) {
    this.setAttribute("count", String(value));
  }

  attributeChangedCallback(name, oldValue, newValue) {
    if (name === "count") this.#render();
  }
}
```

Now both work:

```html
<my-element count="5"></my-element>
```

```javascript
document.querySelector("my-element").count = 10;   // updates attribute, re-renders
```

This is the idiom every HTML element uses — `input.value`, `a.href`, `img.src` are all property-attribute pairs. Your custom element behaves like a built-in.

## Part 4: Shadow DOM — True Encapsulation

The biggest problem with a regular component is that its styles and internal DOM are visible to the rest of the page. A stylesheet elsewhere can accidentally target your internals. A `document.querySelector` call from outside can reach into your component and break it. Shadow DOM fixes both.

### Attaching a shadow root

```javascript
class Badge extends HTMLElement {
  constructor() {
    super();
    this.attachShadow({ mode: "open" });
    this.shadowRoot.innerHTML = `
      <style>
        .badge {
          display: inline-block;
          padding: 0.25em 0.75em;
          border-radius: 999px;
          background: oklch(55% 0.15 250);
          color: white;
          font-size: 0.875em;
        }
      </style>
      <span class="badge"><slot></slot></span>
    `;
  }
}
customElements.define("c-badge", Badge);
```

```html
<c-badge>New</c-badge>
```

The `<slot>` is where the element's light-DOM children (`"New"`) are rendered. The `<style>` is scoped to this shadow root — `.badge` does not leak to the outside, and outside CSS cannot target `.badge` inside the shadow.

### `open` vs `closed`

`attachShadow({ mode: "open" })` makes the shadow root accessible via `element.shadowRoot`. `attachShadow({ mode: "closed" })` makes it inaccessible. In practice, **always use `open`**. The `closed` mode does not provide security — a motivated attacker can still reach the DOM through other means — and it only gets in the way of debugging and testing. Nobody uses `closed` in production.

### Style scoping rules

Styles inside a shadow root:

- Apply only to elements inside that shadow root.
- Do not leak out.
- Are not affected by page stylesheets, except for:
  - Inherited properties (font, color).
  - CSS custom properties (`--brand-color`) pierce shadow boundaries.

This is a feature, not a bug. You can expose your component's theming via custom properties:

```css
/* Inside the shadow root */
.badge {
  background: var(--badge-bg, oklch(55% 0.15 250));
  color: var(--badge-fg, white);
}
```

Outside, consumers theme it without knowing the internals:

```html
<c-badge style="--badge-bg: red; --badge-fg: white;">Error</c-badge>
```

This is the standard contract for themeable components. Expose a minimal surface of CSS variables; keep everything else private.

### The `:host` and `:host()` selectors

Inside a shadow root, `:host` refers to the element itself (the shadow host — the `<c-badge>`):

```css
:host {
  display: inline-block;    /* make the custom element a block */
  cursor: pointer;
}

:host(:hover) {
  opacity: 0.8;
}

:host([disabled]) {
  opacity: 0.5;
  pointer-events: none;
}
```

`:host(selector)` matches the host when it matches the selector. `:host([disabled])` applies when the element has a `disabled` attribute. Combined with attribute-driven rendering, this is how you expose state-based styling to consumers.

### `:host-context()` — responding to ancestor context

```css
:host-context([data-theme="dark"]) {
  background: #222;
  color: white;
}
```

`:host-context(selector)` matches if any ancestor of the host matches. Useful for components that want to adapt to an outer theme attribute. [Baseline Widely Available](https://developer.mozilla.org/en-US/docs/Web/CSS/:host-context), with the note that Firefox has had some adoption hiccups — for broadly-compatible code, also provide a CSS-custom-property override.

### `::part` — pierce-the-shadow styling

Sometimes you want to expose specific elements inside your shadow tree for outside styling. Use the `part` attribute:

```javascript
shadowRoot.innerHTML = `
  <button part="button">
    <span part="label"><slot></slot></span>
  </button>
`;
```

Outside:

```css
c-badge::part(button) {
  border-radius: 0;
}
c-badge::part(label) {
  font-weight: bold;
}
```

`::part` is a controlled-access-window into the shadow. Consumers can style marked parts without reaching for arbitrary internals. This is the right way to make a component flexibly themeable beyond what custom properties express.

### `slotted()` — styling what users put in the slot

Content that comes from outside — through `<slot>` — is *not* inside the shadow tree conceptually, even though it renders there. To style it from shadow CSS, use `::slotted`:

```css
::slotted(a) {
  color: var(--link-color, blue);
}

::slotted(*) {
  margin: 0;
}
```

`::slotted(selector)` matches top-level slotted elements that match the selector. It does *not* match descendants of slotted elements — only the direct children of the host that get distributed into slots.

## Part 5: Templates and Slots — Composition

The `<template>` element is inert HTML — it is parsed but not rendered. It is the standard way to define a chunk of markup that will be cloned later:

```html
<template id="card-template">
  <style>
    article {
      border: 1px solid var(--border, #ccc);
      padding: 1rem;
      border-radius: 0.5rem;
    }
    h2 { margin-top: 0; }
    .meta { color: var(--text-muted, #666); font-size: 0.875em; }
  </style>
  <article>
    <h2><slot name="title"></slot></h2>
    <div class="meta"><slot name="meta"></slot></div>
    <div class="body"><slot></slot></div>
  </article>
</template>

<script type="module">
  class CCard extends HTMLElement {
    constructor() {
      super();
      const template = document.getElementById("card-template");
      this.attachShadow({ mode: "open" })
          .append(template.content.cloneNode(true));
    }
  }
  customElements.define("c-card", CCard);
</script>
```

Usage:

```html
<c-card>
  <h3 slot="title">My Article</h3>
  <span slot="meta">Published May 30, 2026</span>
  <p>This is the body of the article.</p>
</c-card>
```

What happens:

- The `<h3 slot="title">` goes into the `<slot name="title">`.
- The `<span slot="meta">` goes into the `<slot name="meta">`.
- The `<p>` (no slot attribute) goes into the default slot (`<slot>` without a name).
- The styles in the template are scoped to this shadow root, so `article` and `h2` do not conflict with page styles.

This is **composition**. The component defines slots — named holes — and consumers fill them. The result is a reusable structural component where the consumer controls content and the author controls layout and styling.

### Default slot content

A `<slot>` can have default content that appears when nothing is slotted:

```html
<slot name="icon">
  <svg>...</svg>
</slot>
```

If the consumer provides `<svg slot="icon">`, that replaces the default. If they do not, the default SVG renders.

### The `slotchange` event

You can react when slotted content changes:

```javascript
this.shadowRoot.querySelector("slot").addEventListener("slotchange", (event) => {
  const nodes = event.target.assignedNodes();
  console.log("Slot content changed:", nodes);
});
```

Useful for components that need to know what was slotted — for instance, a `<tab-group>` that wants to index its `<tab-panel>` children.

## Part 6: Declarative Shadow DOM — The Server-Rendering Path

The big limitation of Shadow DOM used to be that it required JavaScript to construct. A server-rendered page could not include the shadow tree in the initial HTML, which meant:

- Components would flash unstyled until JavaScript ran.
- Search engines indexing the page saw empty `<my-card>` tags, not their shadow content.
- Pages with JavaScript disabled saw nothing.

[Declarative Shadow DOM (DSD)](https://web.dev/articles/declarative-shadow-dom), which [became Baseline Newly Available in August 2024](https://web-platform-dx.github.io/web-features-explorer/features/declarative-shadow-dom/), fixes this. You can declare a shadow root directly in HTML:

```html
<c-card>
  <template shadowrootmode="open">
    <style>
      article { border: 1px solid #ccc; padding: 1rem; }
      h2 { margin-top: 0; }
    </style>
    <article>
      <h2><slot name="title"></slot></h2>
      <div><slot></slot></div>
    </article>
  </template>
  <h3 slot="title">My Article</h3>
  <p>Content here.</p>
</c-card>
```

The `<template shadowrootmode="open">` inside `<c-card>` is automatically attached as the shadow root during parsing — no JavaScript needed. The component's content renders immediately with full styling. When the JavaScript for `<c-card>` eventually loads and the class is registered, the element is "upgraded" — its `constructor` runs, it can access the already-attached shadow root via `this.shadowRoot`, and it adds behaviour to the existing structure.

### The upgrade path

A Declarative-Shadow-DOM-aware custom element looks like:

```javascript
class CCard extends HTMLElement {
  constructor() {
    super();
    // Use existing shadow root if present (from DSD)...
    if (!this.shadowRoot) {
      // ...or create one and hydrate from a template.
      this.attachShadow({ mode: "open" });
      const template = document.getElementById("card-template");
      this.shadowRoot.append(template.content.cloneNode(true));
    }
  }

  connectedCallback() {
    // Behaviour setup — click handlers, state initialisation, etc.
    const button = this.shadowRoot.querySelector("button");
    button?.addEventListener("click", () => this.#handleClick());
  }
}
```

The element works whether JavaScript runs or not. With JavaScript, it is interactive. Without, it still renders with full styling. This is progressive enhancement at the component level.

### DSD considerations

- **`<template shadowrootmode="open">`** is the standard attribute. Older Chrome used `shadowroot` (without `mode`); that spelling is obsolete.
- **`shadowrootmode="closed"`** is also valid; same caveat as above — prefer `open`.
- **`shadowrootdelegatesfocus`** is a boolean attribute that enables focus delegation (when the host is focused, focus goes to the first focusable child in the shadow).
- **`DOMParser` and `innerHTML` do not process `shadowrootmode` by default** for security. If you need to parse DSD programmatically, use `setHTMLUnsafe()` or the `includeShadowRoots: true` option on DOMParser.

For static sites like ours on GitHub Pages, DSD is a genuine win — a page rendered at build time can include complete shadow DOM, the user sees the fully-styled page on first paint, and JavaScript upgrades it progressively. We use this pattern for the capstone.

## Part 7: A Real Component — `<confirm-dialog>`

Let us build something useful. A `<confirm-dialog>` that wraps the native `<dialog>` element (Day 2) with a cleaner API:

```javascript
// @ts-check
class ConfirmDialog extends HTMLElement {
  #controller;

  constructor() {
    super();
    this.attachShadow({ mode: "open" });
    this.shadowRoot.innerHTML = `
      <style>
        dialog {
          border: none;
          border-radius: 0.5rem;
          padding: 1.5rem;
          max-width: 90vw;
          width: 24rem;
          box-shadow: 0 10px 25px rgba(0, 0, 0, 0.15);
        }
        dialog::backdrop {
          background: oklch(0% 0 0 / 0.5);
          backdrop-filter: blur(2px);
        }
        h2 { margin: 0 0 0.5rem; font-size: 1.25rem; }
        p  { margin: 0 0 1.5rem; color: oklch(40% 0 0); }
        .actions { display: flex; gap: 0.5rem; justify-content: flex-end; }
        button {
          padding: 0.5rem 1rem;
          border-radius: 0.375rem;
          border: 1px solid oklch(80% 0 0);
          background: white;
          cursor: pointer;
          font: inherit;
        }
        button.primary {
          background: oklch(55% 0.15 250);
          color: white;
          border-color: transparent;
        }
      </style>
      <dialog part="dialog">
        <h2 part="title"><slot name="title">Confirm</slot></h2>
        <p part="message"><slot>Are you sure?</slot></p>
        <div class="actions">
          <button type="button" data-action="cancel" part="cancel">
            <slot name="cancel-label">Cancel</slot>
          </button>
          <button type="button" class="primary" data-action="confirm" part="confirm">
            <slot name="confirm-label">Confirm</slot>
          </button>
        </div>
      </dialog>
    `;
  }

  connectedCallback() {
    this.#controller = new AbortController();
    const { signal } = this.#controller;

    this.shadowRoot.addEventListener("click", (event) => {
      const action = event.target.closest("[data-action]")?.dataset.action;
      if (action === "confirm") this.#close(true);
      else if (action === "cancel") this.#close(false);
    }, { signal });

    this.shadowRoot.querySelector("dialog").addEventListener("close", (event) => {
      // Escape key or dialog.close() — if not already resolved, treat as cancel
      if (this.#resolve) this.#close(false);
    }, { signal });
  }

  disconnectedCallback() {
    this.#controller?.abort();
  }

  #resolve = null;

  /** Open the dialog; returns a Promise<boolean>. */
  open() {
    const dialog = this.shadowRoot.querySelector("dialog");
    dialog.showModal();
    return new Promise((resolve) => {
      this.#resolve = resolve;
    });
  }

  #close(result) {
    const dialog = this.shadowRoot.querySelector("dialog");
    dialog.close();
    this.#resolve?.(result);
    this.#resolve = null;
  }
}

customElements.define("confirm-dialog", ConfirmDialog);
```

Usage:

```html
<confirm-dialog id="delete-confirm">
  <span slot="title">Delete this post?</span>
  This action cannot be undone.
  <span slot="confirm-label">Delete</span>
</confirm-dialog>

<button id="delete-button">Delete post</button>

<script type="module">
  const dialog = document.getElementById("delete-confirm");
  document.getElementById("delete-button").addEventListener("click", async () => {
    const confirmed = await dialog.open();
    if (confirmed) deletePost();
  });
</script>
```

`dialog.open()` returns a promise that resolves to `true` if the user clicks Confirm, or `false` if they cancel (or press Escape, or close via any other means). The consumer `await`s the result. This is exactly the `window.confirm()` API, but fully stylable, themeable, accessible, and reusable.

Features:

- Wraps native `<dialog>` (Escape key, focus trap, `::backdrop`, inert page, all free).
- Keyboard accessible: Tab cycles within the dialog; Enter on the focused button activates it; Escape cancels.
- Slots for title, message, and both button labels — consumer controls all content.
- `::part()` exposed for deep styling customisation.
- Promise-based API that feels like the native `confirm()`.

Total: ~80 lines. Drop into any page, any framework, any year. Works today. Will still work in 2040.

## Part 8: A More Complex Component — `<tabs-container>`

Tabs are a classic UI pattern and a good test of real accessibility. Let us build one that follows [the ARIA Authoring Practices Guide](https://www.w3.org/WAI/ARIA/apg/patterns/tabs/):

```javascript
// @ts-check
class TabsContainer extends HTMLElement {
  #controller;

  constructor() {
    super();
    this.attachShadow({ mode: "open" });
    this.shadowRoot.innerHTML = `
      <style>
        :host { display: block; }
        [role="tablist"] {
          display: flex;
          gap: 0.5rem;
          border-bottom: 2px solid oklch(90% 0 0);
        }
        [role="tab"] {
          padding: 0.5rem 1rem;
          border: none;
          background: transparent;
          cursor: pointer;
          font: inherit;
          border-bottom: 2px solid transparent;
          margin-bottom: -2px;
        }
        [role="tab"][aria-selected="true"] {
          color: oklch(55% 0.15 250);
          border-bottom-color: oklch(55% 0.15 250);
          font-weight: 600;
        }
        [role="tab"]:focus-visible {
          outline: 2px solid oklch(55% 0.15 250);
          outline-offset: 2px;
        }
        [role="tabpanel"] {
          padding: 1rem 0;
        }
        [role="tabpanel"][hidden] { display: none; }
      </style>
      <div role="tablist" part="tablist"></div>
      <div part="panels"><slot></slot></div>
    `;
  }

  connectedCallback() {
    this.#controller = new AbortController();
    const { signal } = this.#controller;
    this.#buildTabs();

    this.shadowRoot.querySelector('[role="tablist"]')
      .addEventListener("click", this.#onTabClick, { signal });
    this.shadowRoot.querySelector('[role="tablist"]')
      .addEventListener("keydown", this.#onTabKeydown, { signal });

    // Re-index on slot changes (tabs added/removed dynamically)
    this.shadowRoot.querySelector("slot")
      .addEventListener("slotchange", this.#buildTabs, { signal });
  }

  disconnectedCallback() {
    this.#controller?.abort();
  }

  #buildTabs = () => {
    const tablist = this.shadowRoot.querySelector('[role="tablist"]');
    tablist.innerHTML = "";

    const panels = [...this.querySelectorAll(":scope > [data-tab]")];
    panels.forEach((panel, index) => {
      const label = panel.getAttribute("data-tab");
      const id = panel.id || `tab-${crypto.randomUUID()}`;
      panel.id = id;
      panel.setAttribute("role", "tabpanel");
      panel.setAttribute("aria-labelledby", `${id}-tab`);
      panel.hidden = index !== 0;

      const tab = document.createElement("button");
      tab.type = "button";
      tab.id = `${id}-tab`;
      tab.setAttribute("role", "tab");
      tab.setAttribute("aria-controls", id);
      tab.setAttribute("aria-selected", index === 0 ? "true" : "false");
      tab.setAttribute("tabindex", index === 0 ? "0" : "-1");
      tab.textContent = label;
      tablist.append(tab);
    });
  };

  #onTabClick = (event) => {
    const tab = event.target.closest('[role="tab"]');
    if (tab) this.#activate(tab);
  };

  #onTabKeydown = (event) => {
    const tab = event.target.closest('[role="tab"]');
    if (!tab) return;
    const tabs = [...this.shadowRoot.querySelectorAll('[role="tab"]')];
    const i = tabs.indexOf(tab);
    let next;
    if (event.key === "ArrowRight") next = tabs[(i + 1) % tabs.length];
    else if (event.key === "ArrowLeft") next = tabs[(i - 1 + tabs.length) % tabs.length];
    else if (event.key === "Home") next = tabs[0];
    else if (event.key === "End") next = tabs[tabs.length - 1];
    if (next) {
      event.preventDefault();
      this.#activate(next);
      next.focus();
    }
  };

  #activate(tab) {
    const tabs = [...this.shadowRoot.querySelectorAll('[role="tab"]')];
    const panelId = tab.getAttribute("aria-controls");
    for (const t of tabs) {
      const isActive = t === tab;
      t.setAttribute("aria-selected", String(isActive));
      t.setAttribute("tabindex", isActive ? "0" : "-1");
    }
    for (const panel of this.querySelectorAll("[data-tab]")) {
      panel.hidden = panel.id !== panelId;
    }
    this.dispatchEvent(new CustomEvent("tab-change", {
      detail: { panelId },
      bubbles: true,
    }));
  }
}

customElements.define("tabs-container", TabsContainer);
```

Usage is pleasantly clean:

```html
<tabs-container>
  <div data-tab="Overview">
    <p>Overview content.</p>
  </div>
  <div data-tab="Details">
    <p>Details content.</p>
  </div>
  <div data-tab="Reviews">
    <p>Reviews content.</p>
  </div>
</tabs-container>
```

The child elements with `data-tab="..."` become panels; the tab labels come from the attribute. The component handles everything else: ARIA roles, keyboard navigation (arrows, Home, End), `aria-selected`/`aria-controls`/`tabindex` management, a `tab-change` custom event for consumers, and re-indexing if tabs are added or removed.

A consumer gets a proper tabs component by typing one element and labeled children. No framework. No configuration. No dependencies.

## Part 9: `<live-clock>` — Self-Updating Components

A tiny component that illustrates lifecycle cleanup:

```javascript
// @ts-check
class LiveClock extends HTMLElement {
  #interval;

  constructor() {
    super();
    this.attachShadow({ mode: "open" });
    this.shadowRoot.innerHTML = `
      <style>
        :host { font-variant-numeric: tabular-nums; }
      </style>
      <time></time>
    `;
  }

  connectedCallback() {
    this.#tick();
    this.#interval = setInterval(() => this.#tick(), 1000);
  }

  disconnectedCallback() {
    clearInterval(this.#interval);
  }

  #tick() {
    const time = this.shadowRoot.querySelector("time");
    const now = new Date();
    time.dateTime = now.toISOString();
    const fmt = this.getAttribute("format") === "12h"
      ? { hour: "numeric", minute: "2-digit", hour12: true }
      : { hour: "2-digit", minute: "2-digit", second: "2-digit", hour12: false };
    time.textContent = now.toLocaleTimeString(undefined, fmt);
  }
}

customElements.define("live-clock", LiveClock);
```

```html
<live-clock></live-clock>
<live-clock format="12h"></live-clock>
```

Two attributes' worth of control; a self-updating component. The `disconnectedCallback` stops the interval when the element is removed from the DOM — no memory leak, no zombie timer. `tabular-nums` keeps the digits from jittering (Day 6). `<time datetime="...">` is semantic and machine-readable.

Twenty-five lines. Indistinguishable from a framework component in use, without needing a framework.

## Part 10: Form-Associated Custom Elements

Custom elements can act as form controls — `<c-checkbox>`, `<c-toggle>`, `<c-rating>`, `<c-color-picker>`. They submit values with the form, participate in constraint validation, and appear in `FormData`. This is done via the `ElementInternals` API, added to the spec in 2020.

[Form-Associated Custom Elements are supported in Chrome, Safari, and Firefox](https://caniuse.com/wf-form-associated-custom-elements) with one caveat: Firefox has partial ARIA property support on `ElementInternals`, so for full ARIA semantics, fall back to attribute-based accessibility on the host element itself. The form-participation features — value submission, validation, `formAssociatedCallback` — work in all three.

### A `<rating-input>` that submits with forms

```javascript
// @ts-check
class RatingInput extends HTMLElement {
  static formAssociated = true;
  static observedAttributes = ["value", "max", "name"];

  #internals;
  #value = 0;

  constructor() {
    super();
    this.#internals = this.attachInternals();
    this.attachShadow({ mode: "open" });
    this.shadowRoot.innerHTML = `
      <style>
        :host { display: inline-flex; gap: 0.125rem; }
        button {
          border: none;
          background: none;
          font-size: 1.5rem;
          cursor: pointer;
          padding: 0.125rem;
          color: oklch(80% 0 0);
          transition: color 100ms;
        }
        button[aria-pressed="true"] { color: oklch(75% 0.2 85); }
        button:focus-visible {
          outline: 2px solid oklch(55% 0.15 250);
          outline-offset: 2px;
        }
      </style>
      <div role="radiogroup" part="group"></div>
    `;
    this.#render();
  }

  connectedCallback() {
    // Default ARIA
    if (!this.hasAttribute("role")) {
      this.#internals.role = "slider";
    }
    this.#update();
  }

  attributeChangedCallback(name, oldValue, newValue) {
    if (name === "value") this.#value = Number(newValue) || 0;
    this.#render();
    this.#update();
  }

  get value() { return String(this.#value); }
  set value(v) {
    this.#value = Number(v) || 0;
    this.setAttribute("value", String(this.#value));
  }

  get max() { return Number(this.getAttribute("max") ?? 5); }

  get form() { return this.#internals.form; }
  get name() { return this.getAttribute("name"); }
  get validity() { return this.#internals.validity; }
  get validationMessage() { return this.#internals.validationMessage; }
  get willValidate() { return this.#internals.willValidate; }
  checkValidity() { return this.#internals.checkValidity(); }
  reportValidity() { return this.#internals.reportValidity(); }

  formResetCallback() {
    this.value = this.getAttribute("value") ?? "0";
  }

  formDisabledCallback(disabled) {
    for (const btn of this.shadowRoot.querySelectorAll("button")) {
      btn.disabled = disabled;
    }
  }

  #render() {
    const group = this.shadowRoot.querySelector("[role='radiogroup']");
    group.innerHTML = "";
    for (let i = 1; i <= this.max; i++) {
      const btn = document.createElement("button");
      btn.type = "button";
      btn.textContent = i <= this.#value ? "★" : "☆";
      btn.setAttribute("aria-label", `${i} of ${this.max}`);
      btn.setAttribute("aria-pressed", String(i <= this.#value));
      btn.addEventListener("click", () => {
        this.value = String(i);
        this.dispatchEvent(new Event("change", { bubbles: true }));
      });
      group.append(btn);
    }
  }

  #update() {
    this.#internals.setFormValue(String(this.#value));
    if (this.hasAttribute("required") && this.#value === 0) {
      this.#internals.setValidity(
        { valueMissing: true },
        "Please select a rating.",
      );
    } else {
      this.#internals.setValidity({});
    }
  }
}

customElements.define("rating-input", RatingInput);
```

Usage:

```html
<form id="review">
  <label>
    Rating:
    <rating-input name="rating" required max="5"></rating-input>
  </label>
  <button type="submit">Submit</button>
</form>

<script type="module">
  document.getElementById("review").addEventListener("submit", (event) => {
    event.preventDefault();
    const data = new FormData(event.target);
    console.log("rating:", data.get("rating"));   // the value!
  });
</script>
```

The `<rating-input>` participates in form submission as if it were a native input:

- `FormData` captures its value under `name="rating"`.
- Validation runs (the `required` attribute triggers our `valueMissing` validity).
- `formResetCallback` resets the value when the form resets.
- `formDisabledCallback` disables the buttons when the form is disabled.

All of this is wired through `ElementInternals`. The consumer gets a first-class form control that just happens to be custom-built.

## Part 11: Custom States — `:state()` For CSS

A newer feature worth knowing: custom elements can expose custom boolean states to CSS via the `:state()` pseudo-class. [Baseline Newly Available in 2024](https://developer.mozilla.org/en-US/docs/Web/CSS/:state):

```javascript
class CustomToggle extends HTMLElement {
  #internals;
  constructor() {
    super();
    this.#internals = this.attachInternals();
  }

  toggle() {
    if (this.#internals.states.has("on")) {
      this.#internals.states.delete("on");
    } else {
      this.#internals.states.add("on");
    }
  }
}
customElements.define("custom-toggle", CustomToggle);
```

Now in CSS:

```css
custom-toggle:state(on) {
  background: oklch(60% 0.2 145);
}
```

The `:state(on)` pseudo-class matches when the element has that state active. This is cleaner than adding attributes — it does not appear in the DOM, does not affect CSS specificity weirdly, and is designed for exactly this purpose.

Before `:state()`, the idiom was to toggle attributes (`el.toggleAttribute("data-on", ...)`) and target them with `[data-on]`. Still works, still fine. The custom-state API is a slight improvement for internal component states.

## Part 12: Components That Use Signals

Putting Day 9 and today together: a component that uses our signal primitives for reactive rendering.

```javascript
// @ts-check
import { signal, effect } from "@app/utils/signals.js";

class CounterElement extends HTMLElement {
  #count = signal(0);
  #dispose;

  constructor() {
    super();
    this.attachShadow({ mode: "open" });
    this.shadowRoot.innerHTML = `
      <style>
        :host { display: inline-flex; gap: 0.5rem; align-items: center; }
        button { padding: 0.25rem 0.75rem; font: inherit; cursor: pointer; }
      </style>
      <button data-action="decrement">-</button>
      <span part="value"></span>
      <button data-action="increment">+</button>
    `;
  }

  connectedCallback() {
    const initial = Number(this.getAttribute("value") || "0");
    this.#count.set(initial);

    this.#dispose = effect(() => {
      this.shadowRoot.querySelector("[part=value]").textContent = String(this.#count());
      this.dispatchEvent(new CustomEvent("count-change", {
        detail: { count: this.#count() },
        bubbles: true,
      }));
    });

    this.shadowRoot.addEventListener("click", (event) => {
      const action = event.target.closest("[data-action]")?.dataset.action;
      if (action === "increment") this.#count.set(this.#count() + 1);
      else if (action === "decrement") this.#count.set(this.#count() - 1);
    });
  }

  disconnectedCallback() {
    this.#dispose?.();
  }
}

customElements.define("counter-element", CounterElement);
```

The signal holds the counter's value. The `effect` subscribes to the signal and updates the DOM whenever the value changes. `disconnectedCallback` disposes the effect. No render loop, no virtual DOM, no framework — reactive rendering in a custom element, in 30 lines. This pattern scales to arbitrarily complex components.

## Part 13: Trade-offs And Honest Caveats

Web Components are excellent. They are not perfect. A short, honest list of where the model has rough edges.

**1. Inter-component communication is pure DOM events.** If you want typed APIs between components, you have to build them yourself. Frameworks make this easier with props.

**2. Server-side rendering is now solved (DSD) but only recently.** Older tooling does not understand DSD. If you use a static site generator that predates 2024, check whether it outputs `<template shadowrootmode>`.

**3. TypeScript support requires manual typing.** `customElements.get("my-el")` returns `HTMLElement` — TypeScript does not infer the custom class. You write type declarations yourself, usually in a `.d.ts` file.

**4. Framework integration has friction.** React treats all custom-element-attribute bindings as strings (it is getting better; React 19 improved this). Vue, Svelte, Angular, and Blazor generally work well. For a pure-React app, a framework component is sometimes smoother.

**5. Shadow DOM complicates styling at the edges.** Global CSS does not reach shadow trees. This is usually what you want, but if you are layering a custom element on top of an existing design system, the integration takes thought.

**6. Distributed event targets can surprise.** An event that originates inside a shadow root is re-targeted to the host when it crosses the shadow boundary. This is usually intuitive, but occasionally confusing — `event.target` is not always the deepest element.

**7. Form-associated custom elements are new-ish.** Firefox's ARIA property support lags. If you write a form control that depends heavily on ARIA properties on `ElementInternals`, test in Firefox.

**8. The "no framework" story is mostly true.** For rich lists (keyed reconciliation from Day 9's Part 9), you may still want a rendering helper like `htm` + `preact` from esm.sh. That is fine — Web Components compose with frameworks at the component boundary.

## Part 14: When To Use Web Components

Cases where Web Components are the clear best choice:

- **Design-system components** shared across multiple apps, possibly written in different frameworks.
- **Embeddable widgets** you ship to third-party sites (comments widgets, payment buttons, rating displays).
- **Long-lived codebases** where surviving the next framework migration is an explicit goal.
- **Framework-agnostic component libraries** — if you are building something like Material Web or Shoelace, Web Components are the only defensible foundation.

Cases where a framework's native components are better:

- **A single-codebase SPA** where every consumer is the same framework. The ergonomic wins of framework components outweigh the portability wins of Web Components.
- **Very high-level abstractions** with deeply interlocking state. Web Components shine for isolated units; cross-component reactive state is easier in a framework designed for it.
- **Teams that heavily lean on TypeScript generics and inference.** Framework components integrate better with sophisticated type systems.

A common pattern that works well: **use Web Components at the boundary, frameworks on the inside**. Your design-system primitives are Web Components (portable); your pages are framework-built (productive). We do this. It is the best of both.

## Part 15: Tomorrow

Tomorrow — **Day 11: Client-Side Routing with the Navigation API and View Transitions** — we cover how to build a single-page application that feels like one without the JavaScript payload of one. We revisit same-document and cross-document View Transitions (Day 6), build a ~100-line SPA router from scratch, cover the new Navigation API that replaces `history.pushState`, and conclude with a honest comparison of SPA vs MPA for different workloads. If you have been hesitant to build client-side navigation without a framework, tomorrow should change your mind.

See you tomorrow.

---

## Series navigation

You are reading **Part 10 of 15**.

- [Part 1: Overview — Why a Plain Browser Is Enough in 2026](/blog/2026-05-24-no-build-web-overview)
- [Part 2: Semantic HTML and the Document Outline Nobody Taught You](/blog/2026-05-25-no-build-web-html-semantics)
- [Part 3: The Cascade, Specificity, and `@layer`](/blog/2026-05-26-no-build-web-css-cascade-layers)
- [Part 4: Modern CSS Layout — Flexbox, Grid, Subgrid, and Container Queries](/blog/2026-05-27-no-build-web-modern-css-layout)
- [Part 5: Responsive Design in 2026](/blog/2026-05-28-no-build-web-responsive-design)
- [Part 6: Colour, Typography, and Motion](/blog/2026-05-29-no-build-web-color-typography-motion)
- [Part 7: Native ES Modules](/blog/2026-05-30-no-build-web-es-modules)
- [Part 8: The DOM, Events, and Platform Primitives](/blog/2026-05-31-no-build-web-dom-and-events)
- [Part 9: State Management Without a Library](/blog/2026-06-01-no-build-web-state-and-signals)
- **Part 10 (today): Web Components — Custom Elements, Shadow DOM, Templates, and Slots**
- Part 11 (tomorrow): Client-Side Routing with the Navigation API and View Transitions
- Part 12: Forms, Validation, and the Constraint Validation API
- Part 13: Storage, Service Workers, and Offline
- Part 14: Accessibility, Performance, and Security
- Part 15: The Conclusion — A Complete Application End to End

## Resources

- [MDN: Using custom elements](https://developer.mozilla.org/en-US/docs/Web/API/Web_components/Using_custom_elements) — the definitive guide.
- [MDN: Using shadow DOM](https://developer.mozilla.org/en-US/docs/Web/API/Web_components/Using_shadow_DOM) — reference.
- [MDN: ElementInternals](https://developer.mozilla.org/en-US/docs/Web/API/ElementInternals) — form-associated custom elements.
- [MDN: `:state()` pseudo-class](https://developer.mozilla.org/en-US/docs/Web/CSS/:state) — custom state CSS hooks.
- [web.dev: Declarative Shadow DOM](https://web.dev/articles/declarative-shadow-dom) — the announcement and guide.
- [WebKit: ElementInternals and Form-Associated Custom Elements](https://webkit.org/blog/13711/elementinternals-and-form-associated-custom-elements/) — the Safari implementation notes.
- [WebKit: Declarative Shadow DOM](https://webkit.org/blog/13851/declarative-shadow-dom/) — Safari's DSD announcement.
- [ARIA Authoring Practices Guide — Tabs](https://www.w3.org/WAI/ARIA/apg/patterns/tabs/) — how to build accessible tabs.
- [Open Web Components](https://open-wc.org/) — best-practices guides for Web Components.
- [Shoelace](https://shoelace.style/) — a production-quality Web Components library (now part of Web Awesome).
- [Material Web](https://material-web.dev/) — Google's Material Design as Web Components.
- [Lit](https://lit.dev/) — a small library (3KB) on top of Web Components for reactive templating, if the plain-JS version becomes painful.
- [The Good, the Bad, and the Web Components](https://jakelazaroff.com/words/web-components-are-not-the-future/) — a critical view worth reading for balance.
- [Can I use: Custom elements](https://caniuse.com/custom-elementsv1) — current browser support.
- [Can I use: Declarative Shadow DOM](https://caniuse.com/declarative-shadow-dom) — current browser support.
