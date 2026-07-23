# Page

`PageData`

A page is a stack of elements. Two consecutive pages make a spread. Page 0 and 1 are the first spread's left and right leaves, 2 and 3 the second, and so on.

```json
{
  "Id": "cover",
  "ChapterId": "chapter-1",
  "Elements": [ ... ]
}
```

---

## Fields

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| `Id` <span class="req">required</span> | `string` | — | An identifier for the page, unique within the book. Actions and conditions can refer to a page by ID, which survives inserting pages in a way that a page number doesn't. |
| `ChapterId` <span class="opt">optional</span> | `string` | — | The chapter this page belongs to. Pages sharing a value belong to the same chapter and **must be listed consecutively**. See [Chapters](#chapters). |
| `Elements` <span class="opt">optional</span> | list of [`elements`](elements/index.md) | empty list | The page's content, stacked top to bottom in order. |
| `Background` <span class="opt">optional</span> | list of [`elements`](elements/index.md) | empty list | Elements drawn **behind** `Elements`, placed by their `Position` rather than stacked. They don't affect the layout, so they can't push anything around. Use them for flourishes, watermarks or page texture. |
| `Foreground` <span class="opt">optional</span> | list of [`elements`](elements/index.md) | empty list | Elements drawn **over** `Elements`, placed by their `Position` rather than stacked. They don't affect the layout, so they can't push anything around. Use them for flourishes, watermarks or page texture. |

---

## Chapters

A chapter is a run of consecutive pages sharing a `ChapterId`. Chapters are **navigation-isolated**: turning a page never crosses a chapter boundary, and the corner curls disappear at a chapter's first and last spread the same way they do at the book's ends.

The only way in or out of a chapter is an [action](../concepts/actions.md), usually a `Button`. That's the point: it lets you build a book where a section is only reachable from a table of contents, or where the reader can't wander out of an appendix by turning pages.

Pages with no `ChapterId` form a chapter of their own, so a book that never mentions chapters is one chapter and behaves exactly as you'd expect.

Each chapter's spreads start fresh, so a chapter with an odd number of pages ends with a blank right leaf and the next chapter starts on a new spread. That's how a printed book behaves too.

!!! warning "Chapters must be contiguous"
    A chapter is derived from where its pages sit in the list, not declared separately. If pages with the same `ChapterId` appear in two separate runs, they become two chapters and only the first is reachable by ID. Parchment logs a warning when this happens.
