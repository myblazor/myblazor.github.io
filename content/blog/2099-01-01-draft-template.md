---
title: "Your Post Title Here"
date: 2099-01-01
draft: true
author: observer-team
summary: A short one- or two-sentence summary that appears on the blog index page.
featured: false
tags:
  - tag-one
  - tag-two
---

## Section Heading

Write your content here in standard Markdown.

### Code Example

Use fenced code blocks with a language identifier for syntax highlighting:
```csharp
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRazorPages();
var app = builder.Build();
app.MapRazorPages();
app.Run();
```

### Lists

- First item
- Second item
- Third item

### Links and Images

[Link text](https://example.com)

![Alt text](/images/example.png)

### Blockquotes

> This is a blockquote. Use it to highlight important passages.

## Checklist Before Publishing

1. Change `draft: true` to `draft: false` (or remove the line entirely)
2. Set the `date` to the desired publish date (future dates are scheduled)
3. Update the `title`, `summary`, and `tags`
4. Set `featured: true` if this should appear on the home page
5. Rename the file to match the pattern: `YYYY-MM-DD-your-slug-here.md`
