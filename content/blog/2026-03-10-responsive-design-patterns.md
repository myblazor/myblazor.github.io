---
title: Responsive Design Patterns in Blazor
date: 2026-03-10
author: myblazor-team
summary: How we built mobile-friendly data tables and master-detail layouts in pure Blazor.
tags:
  - blazor
  - css
  - responsive
  - ui
---

## The Challenge

Data-heavy UIs are notoriously hard to make responsive. Wide tables overflow on small screens, and complex layouts need fundamentally different structures on mobile vs. desktop.

## Responsive Tables

Our approach uses CSS to transform table rows into stacked cards on small screens:

- On desktop: a traditional `<table>` with sortable column headers
- On mobile: each row becomes a card with label-value pairs

The key CSS trick is using `data-label` attributes on `<td>` elements and displaying them via `::before` pseudo-elements when the table header is hidden.

## Master-Detail Flow

The master-detail pattern uses CSS Grid:

- On desktop: a two-column layout (list on left, details on right)
- On mobile: the columns stack vertically, with the list on top

No JavaScript media queries needed — it's all pure CSS with Blazor handling the state.

## Key Takeaways

1. **Use semantic HTML** — `<table>` for tabular data, not divs pretending to be tables.
2. **CSS does the heavy lifting** — Blazor components stay clean; responsiveness lives in the stylesheet.
3. **Test on real devices** — Emulators are fine for development, but nothing beats a real phone.

See all these patterns live on the [Showcase page](/showcase).
