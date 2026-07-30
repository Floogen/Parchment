# Title

Large heading text. Identical to [`Heading`](heading.md) and [`Paragraph`](paragraph.md) apart from its default font (and `Paragraph`'s optional `Width`). The three exist to give you a consistent vocabulary rather than to behave differently.

```json
{
  "Type": "Title",
  "Text": "Camping Guide",
  "Alignment": "Center"
}
```

## Text fields

`FontType` defaults to **`SpriteText`**, the game's bitmap title font. It's large by design. If a title is overflowing its page, `Small` or a lower `Scale` is usually the answer.

--8<-- "text-content.md"

## Common fields

`Scale` on a `Title` is the **font** scale, since a title has no sprite.

--8<-- "element-common.md"
