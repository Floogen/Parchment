# Parchment

Parchment is a Stardew Valley framework for making books the player can actually read: a field guide, a wizard's tome, a hand-written journal. You describe the book as data and Parchment handles the rest: the book slides in, opens, turns its pages and lays out whatever you put on them.

It's built for Content Patcher authors. A book is JSON, its art is a PNG and you never need to write C#.

---

## Start here

<div class="grid cards" markdown>

-   **[Your first book](getting-started/first-book.md)**

    Write a book, load its art, register it and hand the player something that opens it.

-   **[Concepts](concepts/units.md)**

    Units, layout, art, conditions and actions: the ideas every book is built on.

-   **[Reference](reference/book.md)**

    The full schema for books, pages and every element you can put on a page.

</div>

---

## What a book looks like

```json
{
  "Format": "1.0.0",
  "Id": "{{ModId}}_CampingGuide",
  "TintColor": "165 42 42",
  "Pages": [
    {
      "Id": "cover",
      "Elements": [
        { "Type": "Title", "Text": "Camping Guide", "Alignment": "Center" },
        { "Type": "Heading", "Text": "by Linus", "Alignment": "Center" }
      ]
    },
    {
      "Id": "tents",
      "Elements": [
        { "Type": "Heading", "Text": "Pitching a tent" },
        { "Type": "Divider", "Sizing": "Fill" },
        { "Type": "Paragraph", "Text": "Find level ground, away from the river." },
        {
          "Type": "Image",
          "ItemId": "(O)24",
          "Scale": 4,
          "Alignment": "Center"
        }
      ]
    }
  ]
}
```

That's a two-page book with a cover, a heading, a rule, some prose and a parsnip that carries its own tooltip.

---

## The ideas to know

**Elements stack.** A page is a list of [elements](reference/elements/index.md) laid out top to bottom. Text wraps to the page, pictures centre themselves if you ask and everything below closes up when something above it disappears.

**Everything is measured against your art.** Padding, widths and margins are in the pixels you drew them at, and they scale with the sprite. [Units and scale](concepts/units.md) is short. Read it before you spend an afternoon on a misplaced border.

**Anything can be clickable.** `Action` isn't special to buttons. Put one on an image and you have a bookmark. Actions are [trigger actions](concepts/actions.md), so a button can turn a page *or* give the player an item, run a quest or anything else the game can do.

**Anything can be conditional.** `Condition` takes a [game state query](concepts/conditions.md), re-checked while the book is open. A page can show one thing in winter and another in spring, or hide a section until the player has met someone.

**Chapters are one-way doors.** Pages sharing a `ChapterId` form a section you can't turn your way out of. The only way in or out is a button. Useful for a table of contents, an appendix or a book that wants to control where the reader goes.

---

## Getting started

[Your first book](getting-started/first-book.md) walks through the whole thing: the data patch, the item that opens it and the art.

To open a book without an item, from a tile action, the console, or another mod's C#, see [Opening a book](concepts/opening-books.md).

If you'd rather read the schema directly, start at [Book](reference/book.md). If something looks wrong in-game, [Troubleshooting](troubleshooting.md) is organised by what you see.

---

## Versioning

Every book declares the `Format` it was written against. Parchment reads that to keep older books working when the schema changes, so set it, and don't bump it until you've read the changelog.
