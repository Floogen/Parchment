using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.BellsAndWhistles;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Parchment.Framework.UI.Fonts
{
    /// <summary>Writes the Junimo script the game uses for its bundle notes, for text a [junimo] tag has swapped for stand-in characters.
    /// Each stand-in sits <see cref="GLYPH_OFFSET"/> past the character it stands for, so the text keeps its length and every step of the layout measures it through the font like any other text.
    /// </summary>
    public static class JunimoScript
    {
        public const char GLYPH_OFFSET = '\uE000';

        // SpriteText has a sprite for every character from here to LAST_CHARACTER. A space is left as it is, so the text still wraps between words
        private const char FIRST_CHARACTER = '!';
        private const char LAST_CHARACTER = '~';

        // SpriteText reads this as a line break the layout knows nothing about, so it stays in the element's own font
        private const char SPRITE_TEXT_LINE_BREAK = '^';

        private const string LINE_HEIGHT_MEASURE_TEXT = "A";

        // A capital with flat tops and a flat foot, so its glyph gives a font's letter height and baseline without a serif or a point throwing them off
        private const char HOST_REFERENCE_LETTER = 'H';

        // The letters whose Junimo glyphs are read for the script's usual height, taking whichever top and foot most of them share
        private const string SCRIPT_REFERENCE_LETTERS = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

        // How far each of SpriteText's glyphs' drop shadow reaches below its letter, in its unscaled pixels. It hangs below the baseline the way a font's shadow does, so the letter itself sits on it
        private const int SCRIPT_SHADOW_DEPTH = 1;

        // How far below the font's own baseline the script is set, in the font's unscaled pixels. Settled by eye against the game's Small font
        private const int HOST_BASELINE_NUDGE = 1;

        // Extra room after each glyph drawn among another font's letters, in SpriteText's unscaled pixels, as the script reads tighter at a smaller size than it does at its own
        private const float SCRIPT_LETTER_SPACING = 1f;

        // A glyph whose visible pixels take up less than this share of its cell's width (such as a period or a comma) is pulled in to its own width among another font's letters, rather than keeping the room a full letter gets
        private const float NARROW_GLYPH_SHARE = 0.5f;

        // How many rows at the top of each cell on SpriteText's sheet no glyph uses. They are left out when drawing and reading, as the sprite above a cell can reach down into them (as it does over the semicolon)
        private const int SHEET_CELL_TOP_PADDING = 1;

        // The colors SpriteText's bold sheet draws its glyphs in: the letter itself, then its drop shadow (nearly the game's own text shadow color) and the deeper shade at the shadow's edge
        private static readonly Color SHEET_LETTER_COLOR = new Color(86, 22, 12);
        private static readonly Color SHEET_SHADOW_COLOR = new Color(224, 150, 80);
        private static readonly Color SHEET_DEEP_SHADOW_COLOR = new Color(193, 124, 60);

        // The sheet's colors in the order GetNearestSheetColor numbers them, kept so splitting the sheet doesn't build the list again for every pixel
        private static readonly Color[] _sheetColors = new Color[] { SHEET_LETTER_COLOR, SHEET_SHADOW_COLOR, SHEET_DEEP_SHADOW_COLOR };

        /// <summary>Where a font's letters sit, in its own unscaled pixels from the top of its line. <see cref="Bottom"/> is the first row below them, being the baseline.</summary>
        public readonly record struct LetterBounds(float Top, float Bottom)
        {
            public float Height => Bottom - Top;
        }

        /// <summary>How the script is drawn among a line of text. A direct one is drawn a glyph at a time from SpriteText's sheet, otherwise SpriteText draws it.</summary>
        /// <param name="LetterSpacing">The extra room after each glyph, in screen pixels. Always zero when SpriteText draws it.</param>
        private readonly record struct Placement(bool IsDirect, float FontPixelZoom, float OffsetY, float LetterSpacing);

        /// <summary>Where each glyph of a stretch goes at SpriteText's own zoom.</summary>
        /// <param name="Offsets">How far into the stretch each glyph's room starts, the last entry being the stretch's whole width.</param>
        /// <param name="DrawShifts">How far each glyph is drawn to the left of where its room starts, being its empty columns for a narrow glyph that was pulled in and zero for every other.</param>
        private sealed record NativeLayout(float[] Offsets, float[] DrawShifts);


        // Kept once made, as a scramble over the script asks for the same few glyphs every frame
        private static readonly Dictionary<string, string> _glyphStrings = new Dictionary<string, string>();

        // Where each glyph of a stretch of the script goes at SpriteText's own zoom, kept for whether narrow glyphs were pulled in
        private static readonly Dictionary<(string Characters, float FontPixelZoom, bool IsTightened), NativeLayout> _nativeLayouts = new Dictionary<(string Characters, float FontPixelZoom, bool IsTightened), NativeLayout>();

        // Where each glyph's visible pixels sit within its cell, read from the sheet the first time the glyph is measured. Null for one that couldn't be read
        private static readonly Dictionary<char, Rectangle?> _glyphInks = new Dictionary<char, Rectangle?>();

        private static readonly Dictionary<char, Rectangle> _sourceRectangles = new Dictionary<char, Rectangle>();

        // The sheet the script's measurements were last read from. A different one (as a change of language can bring) is read again
        private static Texture2D? _readTexture;
        private static LetterBounds? _scriptLetters;
        private static IReflectedMethod? _getSourceRectangle;
        private static bool _hasLookedUpSourceRectangle;

        // Looked up once, as the sheet is checked every time the script is measured or drawn among another font's letters
        private static IReflectedField<Texture2D>? _spriteTextureField;
        private static bool _hasLookedUpSpriteTexture;

        // Split out of the sheet the first time the script is colored or haloed. Null when it couldn't be, which leaves the script in its own colors and out of halos
        private static ScriptMasks? _masks;
        private static bool _hasBuiltMasks;

        /// <summary>The script's glyphs split into layers that take a color, each the same size as SpriteText's sheet so a glyph's source rectangle finds it in every one.</summary>
        private sealed class ScriptMasks : IDisposable
        {
            /// <summary>The letter of each glyph in white, drawn in the text's color.</summary>
            public Texture2D Letter { get; }

            /// <summary>The drop shadow of each glyph in white, its deeper edge in gray, drawn in the text's shadow color.</summary>
            public Texture2D Shadow { get; }

            /// <summary>The whole of each glyph in flat white, drawn as an outline's or a glow's copy.</summary>
            public Texture2D Silhouette { get; }

            public ScriptMasks(Texture2D letter, Texture2D shadow, Texture2D silhouette)
            {
                Letter = letter;
                Shadow = shadow;
                Silhouette = silhouette;
            }

            public void Dispose()
            {
                Letter.Dispose();
                Shadow.Dispose();
                Silhouette.Dispose();
            }
        }

        /// <summary>The stand-in for a character, or the character itself when the script can't write it.</summary>
        public static char ToGlyph(char character)
        {
            if (character < FIRST_CHARACTER || character > LAST_CHARACTER || character == SPRITE_TEXT_LINE_BREAK)
            {
                return character;
            }

            return (char)(GLYPH_OFFSET + character);
        }

        /// <summary>Whether the character is a stand-in for one the script writes.</summary>
        public static bool IsGlyph(char character)
        {
            return character >= GLYPH_OFFSET + FIRST_CHARACTER && character <= GLYPH_OFFSET + LAST_CHARACTER;
        }

        /// <summary>The text with every character the script can write swapped for its stand-in.</summary>
        public static string ToGlyphs(string text)
        {
            if (_glyphStrings.TryGetValue(text, out string? glyphs))
            {
                return glyphs;
            }

            char[] characters = text.ToCharArray();

            for (int index = 0; index < characters.Length; index++)
            {
                characters[index] = ToGlyph(characters[index]);
            }

            glyphs = new string(characters);
            _glyphStrings[text] = glyphs;

            return glyphs;
        }

        /// <summary>Whether any of the text is written in the script.</summary>
        public static bool Contains(string? text)
        {
            return text is not null && Contains(text, 0, text.Length);
        }

        /// <summary>Whether any of the text from start up to (but not including) end is written in the script.</summary>
        public static bool Contains(string text, int start, int end)
        {
            int clampedEnd = Math.Min(end, text.Length);

            for (int index = Math.Max(start, 0); index < clampedEnd; index++)
            {
                if (IsGlyph(text[index]))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Reads where a font's letters sit from its <see cref="HOST_REFERENCE_LETTER"/>. Null when the font has no such letter, which leaves the script filling the font's line instead.
        /// A font's glyphs are cropped to their visible pixels when it is built, with how far each was moved down kept in its cropping, so the glyph's own bounds already are the letter's. Its texture is usually compressed, so it couldn't be read pixel by pixel anyway.
        /// </summary>
        public static LetterBounds? ReadLetterBounds(SpriteFont spriteFont)
        {
            try
            {
                if (spriteFont.GetGlyphs().TryGetValue(HOST_REFERENCE_LETTER, out SpriteFont.Glyph glyph) is false || glyph.BoundsInTexture.Height <= 0)
                {
                    return null;
                }

                return new LetterBounds(glyph.Cropping.Y, glyph.Cropping.Y + glyph.BoundsInTexture.Height);
            }
            catch (Exception exception)
            {
                Parchment.monitor.LogOnce($"Couldn't read the letter height of a font, so [junimo] text fills its line instead: {exception.Message}", LogLevel.Trace);
                return null;
            }
        }

        /// <summary>Measures text holding stand-ins. Each line is as tall as the font's own line and each stretch of the script is as wide as SpriteText writes it at the size it is drawn.</summary>
        /// <param name="scale">The scale the text is drawn at, which the font's letter bounds are multiplied by.</param>
        /// <param name="hostLetters">Where the font's letters sit, which the script is sized to. Null to have it fill the font's line, as it does among SpriteText's own letters.</param>
        /// <param name="measurePlain">Measures text without any stand-ins in the element's own font, already scaled.</param>
        public static Vector2 Measure(string text, float scale, LetterBounds? hostLetters, Func<string, Vector2> measurePlain)
        {
            float lineHeight = measurePlain(LINE_HEIGHT_MEASURE_TEXT).Y;
            Placement placement = GetPlacement(lineHeight, scale, hostLetters);
            float maximumLineWidth = 0f;
            float totalHeight = 0f;

            foreach (string line in text.Split('\n'))
            {
                float lineWidth = 0f;

                foreach ((string runText, bool isScript) in GetRuns(line))
                {
                    lineWidth += isScript ? MeasureScript(runText, placement) : measurePlain(runText).X;
                }

                maximumLineWidth = Math.Max(maximumLineWidth, lineWidth);
                totalHeight += lineHeight;
            }

            return new Vector2(maximumLineWidth, totalHeight);
        }

        /// <summary>Draws text holding stand-ins a stretch at a time, the script from SpriteText's sheet and everything else through the element's own font.</summary>
        /// <param name="color">The text's color, alpha included. The script keeps its own colors and only takes the alpha from it unless <paramref name="isColored"/> is set.</param>
        /// <param name="shadowColor">The text's shadow color, alpha included, which colored script draws its shadow in. Fully transparent (as under an outline) leaves the shadow out.</param>
        /// <param name="isColored">Whether the color came from markup or a link, which the script then takes in place of its own colors.</param>
        /// <param name="boldness">How far to the right a second copy of the script is drawn to thicken it, in screen pixels.</param>
        /// <param name="rotation">How far to turn each glyph about its own center, in radians. SpriteText can't turn its letters, so the script stays upright among them.</param>
        /// <param name="isHalo">Whether this is one of an outline's or a glow's copies, which the script draws as a flat silhouette in the given color.</param>
        /// <param name="scale">The scale the text is drawn at, which the font's letter bounds are multiplied by.</param>
        /// <param name="hostLetters">Where the font's letters sit, which the script is sized to. Null to have it fill the font's line, as it does among SpriteText's own letters.</param>
        /// <param name="measurePlain">Measures text without any stand-ins in the element's own font, already scaled.</param>
        /// <param name="drawPlain">Draws text without any stand-ins in the element's own font at the given position.</param>
        public static void Draw(SpriteBatch spriteBatch, string text, Vector2 position, Color color, Color shadowColor, bool isColored, int boldness, float rotation, bool isHalo, float scale, LetterBounds? hostLetters, Func<string, Vector2> measurePlain, Action<string, Vector2> drawPlain)
        {
            float lineHeight = measurePlain(LINE_HEIGHT_MEASURE_TEXT).Y;
            Placement placement = GetPlacement(lineHeight, scale, hostLetters);
            float currentY = position.Y;

            foreach (string line in text.Split('\n'))
            {
                float currentX = position.X;

                foreach ((string runText, bool isScript) in GetRuns(line))
                {
                    if (isScript is false)
                    {
                        drawPlain(runText, new Vector2(currentX, currentY));
                        currentX += measurePlain(runText).X;

                        continue;
                    }

                    DrawScript(spriteBatch, runText, new Vector2(currentX, currentY), color, shadowColor, isColored, boldness, rotation, isHalo, placement);
                    currentX += MeasureScript(runText, placement);
                }

                currentY += lineHeight;
            }
        }

        /// <summary>Settles how the script sits among the font's letters. Sized to them and set on their baseline when both they and the script's own glyphs could be read, otherwise filling the line.
        /// The baseline takes the glyph's letter rather than its drop shadow, as the font's own shadow is drawn apart from its letters.
        /// </summary>
        private static Placement GetPlacement(float lineHeight, float scale, LetterBounds? hostLetters)
        {
            if (hostLetters is LetterBounds host && host.Height > 0f && TryGetScriptLetters(out LetterBounds script) && script.Height > 0f)
            {
                float fontPixelZoom = host.Height * scale / script.Height;
                return new Placement(true, fontPixelZoom, (host.Bottom + HOST_BASELINE_NUDGE) * scale - (script.Bottom - SCRIPT_SHADOW_DEPTH) * fontPixelZoom, SCRIPT_LETTER_SPACING * fontPixelZoom);
            }

            return new Placement(false, GetFontPixelZoom(lineHeight), 0f, 0f);
        }

        /// <summary>Cuts a line into stretches that are all script or all not, handing each stretch of script back as the characters it stands for.</summary>
        private static List<(string Text, bool IsScript)> GetRuns(string line)
        {
            List<(string Text, bool IsScript)> runs = new List<(string Text, bool IsScript)>();
            int runStart = 0;

            for (int index = 1; index <= line.Length; index++)
            {
                if (index < line.Length && IsGlyph(line[index]) == IsGlyph(line[runStart]))
                {
                    continue;
                }

                string runText = line.Substring(runStart, index - runStart);
                bool isScript = IsGlyph(line[runStart]);

                runs.Add((isScript ? ToCharacters(runText) : runText, isScript));
                runStart = index;
            }

            return runs;
        }

        /// <summary>The characters a stretch of stand-ins stands for, which is what SpriteText is given to write in the script.</summary>
        private static string ToCharacters(string glyphs)
        {
            char[] characters = glyphs.ToCharArray();

            for (int index = 0; index < characters.Length; index++)
            {
                characters[index] = (char)(characters[index] - GLYPH_OFFSET);
            }

            return new string(characters);
        }

        /// <summary>How wide a stretch of the script is at the zoom it is drawn at. Measured at SpriteText's own zoom and scaled from there, as SpriteText rounds each character's width to whole pixels and a fractional zoom would lose a little on every one.</summary>
        private static float MeasureScript(string characters, Placement placement)
        {
            float[] offsets = GetNativeLayout(characters, placement.IsDirect).Offsets;

            // The extra room follows every glyph, the last included, so measuring the first few of a stretch lands exactly where the next one is drawn
            return offsets[^1] * placement.FontPixelZoom / SpriteText.fontPixelZoom + characters.Length * placement.LetterSpacing;
        }

        private static void DrawScript(SpriteBatch spriteBatch, string characters, Vector2 position, Color color, Color shadowColor, bool isColored, int boldness, float rotation, bool isHalo, Placement placement)
        {
            if (placement.IsDirect && _readTexture is Texture2D texture)
            {
                ScriptMasks? masks = isColored || isHalo ? GetMasks(texture) : null;

                // Without masks a copy of the script would only smudge its letters, so it stays out of the halo
                if (isHalo && masks is null)
                {
                    return;
                }

                DrawGlyphs(spriteBatch, texture, masks, characters, position, color, shadowColor, isColored, boldness, rotation, isHalo, placement);
                return;
            }

            // SpriteText keeps its own colors and can't be split into masks here, so it stays out of a halo
            if (isHalo)
            {
                return;
            }

            float alpha = color.A / 255f;
            float previousFontPixelZoom = SpriteText.fontPixelZoom;
            SpriteText.fontPixelZoom = placement.FontPixelZoom;

            try
            {
                SpriteText.drawString(spriteBatch, characters, (int)position.X, (int)position.Y, alpha: alpha, junimoText: true);

                // The script's shadow is part of each glyph, so the second copy simply goes over the first, the same as SpriteText's own letters
                if (boldness > 0)
                {
                    SpriteText.drawString(spriteBatch, characters, (int)position.X + boldness, (int)position.Y, alpha: alpha, junimoText: true);
                }
            }
            finally
            {
                SpriteText.fontPixelZoom = previousFontPixelZoom;
            }
        }

        /// <summary>Draws each glyph of the script from SpriteText's sheet where SpriteText would have placed it at its own zoom, scaled to the zoom it is drawn at, so the spacing grows and shrinks evenly with the glyphs.
        /// A halo's copy is the glyph's silhouette in the halo's color. Colored script draws every glyph's shadow in the shadow color before any letter goes down in the text's color, the same as the font draws a string, so no glyph's shadow covers the letter beside it. Anything else is the glyph as the sheet has it.
        /// </summary>
        /// <param name="masks">The script split into layers that take a color. Null draws every glyph as the sheet has it.</param>
        private static void DrawGlyphs(SpriteBatch spriteBatch, Texture2D texture, ScriptMasks? masks, string characters, Vector2 position, Color color, Color shadowColor, bool isColored, int boldness, float rotation, bool isHalo, Placement placement)
        {
            float alpha = color.A / 255f;

            if (masks is not null && isHalo)
            {
                DrawLayer(spriteBatch, masks.Silhouette, characters, position, color, boldness, rotation, placement);
                return;
            }

            if (masks is not null && isColored)
            {
                // A shadow left fully transparent (as under an outline) draws nothing, so it is skipped
                if (shadowColor.PackedValue is not 0)
                {
                    DrawLayer(spriteBatch, masks.Shadow, characters, position, shadowColor, boldness, rotation, placement);
                }

                DrawLayer(spriteBatch, masks.Letter, characters, position, color, boldness, rotation, placement);
                return;
            }

            DrawLayer(spriteBatch, texture, characters, position, Color.White * alpha, boldness, rotation, placement);
        }

        /// <summary>Draws one layer of every glyph in the stretch, each where it sits and again shifted right for a bold.</summary>
        private static void DrawLayer(SpriteBatch spriteBatch, Texture2D layer, string characters, Vector2 position, Color layerColor, int boldness, float rotation, Placement placement)
        {
            NativeLayout layout = GetNativeLayout(characters, placement.IsDirect);
            float widthScale = placement.FontPixelZoom / SpriteText.fontPixelZoom;
            float layerDepth = position.Y / 10000f;

            for (int index = 0; index < characters.Length; index++)
            {
                if (TryGetSourceRectangle(characters[index], out Rectangle sourceRectangle) is false)
                {
                    continue;
                }

                // Turned about its own center, so a wobbling glyph rocks in place. Measured on the whole cell before its top rows are left out, so leaving them out doesn't move it
                Vector2 origin = new Vector2(sourceRectangle.Width / 2f, sourceRectangle.Height / 2f);
                Vector2 center = new Vector2(position.X + (layout.Offsets[index] - layout.DrawShifts[index]) * widthScale + index * placement.LetterSpacing, position.Y + placement.OffsetY) + origin * placement.FontPixelZoom;

                // The cell's unused top rows are left out, with the origin moved up to match so the glyph itself stays exactly where it was
                if (SHEET_CELL_TOP_PADDING < sourceRectangle.Height)
                {
                    sourceRectangle = new Rectangle(sourceRectangle.X, sourceRectangle.Y + SHEET_CELL_TOP_PADDING, sourceRectangle.Width, sourceRectangle.Height - SHEET_CELL_TOP_PADDING);
                    origin.Y -= SHEET_CELL_TOP_PADDING;
                }

                spriteBatch.Draw(layer, center, sourceRectangle, layerColor, rotation, origin, placement.FontPixelZoom, SpriteEffects.None, layerDepth);

                if (boldness > 0)
                {
                    spriteBatch.Draw(layer, center + new Vector2(boldness, 0f), sourceRectangle, layerColor, rotation, origin, placement.FontPixelZoom, SpriteEffects.None, layerDepth);
                }
            }
        }

        /// <summary>The script split into layers that take a color, made from the sheet the first time it is asked for. Null when the sheet can't be read or holds nothing to split.</summary>
        private static ScriptMasks? GetMasks(Texture2D texture)
        {
            if (_hasBuiltMasks)
            {
                return _masks;
            }

            _hasBuiltMasks = true;

            try
            {
                _masks = BuildMasks(texture);
            }
            catch (Exception exception)
            {
                Parchment.monitor.LogOnce($"Couldn't split the Junimo script's glyphs for coloring, so colored [junimo] text keeps its own colors: {exception.Message}", LogLevel.Trace);
                _masks = null;
            }

            return _masks;
        }

        /// <summary>Splits every Junimo glyph on the sheet into its letter, its shadow and its silhouette.
        /// Each pixel goes with whichever of the sheet's colors it is nearest, so a retextured sheet whose colors drift a little still splits the same way. The deeper shadow keeps how much darker it is than the shadow.
        /// </summary>
        private static ScriptMasks? BuildMasks(Texture2D texture)
        {
            if (texture.Format is not SurfaceFormat.Color)
            {
                Parchment.monitor.LogOnce($"Couldn't split the Junimo script's glyphs for coloring from SpriteText's sheet (a {texture.Format} texture), so colored [junimo] text keeps its own colors.", LogLevel.Trace);
                return null;
            }

            Color[] pixels = new Color[texture.Width * texture.Height];
            texture.GetData(pixels);

            List<int> glyphPixels = GetGlyphPixelIndices(texture);

            if (glyphPixels.Count is 0)
            {
                return null;
            }

            float deepShadowShade = Math.Clamp(GetBrightness(SHEET_DEEP_SHADOW_COLOR) / GetBrightness(SHEET_SHADOW_COLOR), 0f, 1f);

            Color[] letterPixels = new Color[pixels.Length];
            Color[] shadowPixels = new Color[pixels.Length];
            Color[] silhouettePixels = new Color[pixels.Length];

            foreach (int index in glyphPixels)
            {
                Color pixel = pixels[index];
                Color white = new Color(pixel.A, pixel.A, pixel.A, pixel.A);

                // The sheet's colors are premultiplied, so white at a pixel's own alpha is every channel at that alpha
                silhouettePixels[index] = white;

                switch (GetNearestSheetColor(pixel))
                {
                    case 0:
                        letterPixels[index] = white;
                        break;
                    case 1:
                        shadowPixels[index] = white;
                        break;
                    default:
                        byte shade = (byte)Math.Round(pixel.A * deepShadowShade);
                        shadowPixels[index] = new Color(shade, shade, shade, pixel.A);
                        break;
                }
            }

            return new ScriptMasks(CreateTexture(texture, letterPixels), CreateTexture(texture, shadowPixels), CreateTexture(texture, silhouettePixels));
        }

        /// <summary>Which of the sheet's colors a pixel is nearest: 0 for the letter, 1 for the shadow and 2 for the deeper shadow. Read from its color before the sheet's premultiplied alpha darkened it.</summary>
        private static int GetNearestSheetColor(Color pixel)
        {
            Vector3 straightColor = new Vector3(pixel.R, pixel.G, pixel.B) * (255f / pixel.A);
            int nearestIndex = 0;
            float nearestDistance = float.MaxValue;

            for (int index = 0; index < _sheetColors.Length; index++)
            {
                float distance = Vector3.DistanceSquared(straightColor, new Vector3(_sheetColors[index].R, _sheetColors[index].G, _sheetColors[index].B));

                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestIndex = index;
                }
            }

            return nearestIndex;
        }

        /// <summary>How bright a solid color is, from 0 to 1.</summary>
        private static float GetBrightness(Color color)
        {
            return (0.299f * color.R + 0.587f * color.G + 0.114f * color.B) / 255f;
        }

        /// <summary>Where every visible pixel of every Junimo glyph sits in the sheet's pixel data, each once. A cell's unused top rows are skipped, as they are never drawn.</summary>
        private static List<int> GetGlyphPixelIndices(Texture2D texture)
        {
            HashSet<int> indices = new HashSet<int>();
            Color[] cellPixels = Array.Empty<Color>();

            for (char character = FIRST_CHARACTER; character <= LAST_CHARACTER; character++)
            {
                if (TryGetSourceRectangle(character, out Rectangle cell) is false || cell.Width <= 0 || cell.Height <= SHEET_CELL_TOP_PADDING || cell.Left < 0 || cell.Top < 0 || cell.Right > texture.Width || cell.Bottom > texture.Height)
                {
                    continue;
                }

                Rectangle area = new Rectangle(cell.X, cell.Y + SHEET_CELL_TOP_PADDING, cell.Width, cell.Height - SHEET_CELL_TOP_PADDING);

                if (cellPixels.Length != area.Width * area.Height)
                {
                    cellPixels = new Color[area.Width * area.Height];
                }

                texture.GetData(0, area, cellPixels, 0, cellPixels.Length);

                for (int row = 0; row < area.Height; row++)
                {
                    for (int column = 0; column < area.Width; column++)
                    {
                        if (cellPixels[row * area.Width + column].A is not 0)
                        {
                            indices.Add((area.Top + row) * texture.Width + area.Left + column);
                        }
                    }
                }
            }

            return indices.ToList();
        }

        private static Texture2D CreateTexture(Texture2D template, Color[] pixels)
        {
            Texture2D texture = new Texture2D(Game1.graphics.GraphicsDevice, template.Width, template.Height);
            texture.SetData(pixels);

            return texture;
        }

        /// <summary>Where each glyph of a stretch goes at SpriteText's own zoom. Every glyph keeps the room SpriteText gives it, except that a narrow one drawn among another font's letters is pulled in to its own width.
        /// Its empty columns on either side are taken out, so it sits right after the glyph before it and the next follows just as closely. The room it gives up is taken off everything after it, which is how measuring part of a stretch still lands where the next glyph is drawn.
        /// Kept once worked out, as the same stretches are drawn every frame.
        /// </summary>
        /// <param name="isTightened">Whether narrow glyphs are pulled in, which only happens when the script is drawn a glyph at a time. SpriteText places its own glyphs otherwise.</param>
        private static NativeLayout GetNativeLayout(string characters, bool isTightened)
        {
            (string Characters, float FontPixelZoom, bool IsTightened) key = (characters, SpriteText.fontPixelZoom, isTightened);

            if (_nativeLayouts.TryGetValue(key, out NativeLayout? layout))
            {
                return layout;
            }

            float[] offsets = new float[characters.Length + 1];
            float[] drawShifts = new float[characters.Length];
            float previousWidth = 0f;
            float savedRoom = 0f;

            for (int index = 0; index < characters.Length; index++)
            {
                float width = SpriteText.getWidthOfString(characters.Substring(0, index + 1));
                float advance = width - previousWidth;
                previousWidth = width;

                offsets[index] = width - advance - savedRoom;

                if (isTightened && TryGetNarrowInk(characters[index], out Rectangle ink))
                {
                    // Only pulled in when its own width is less than the room it was given, so a glyph can never be pushed into the one after it
                    float tightAdvance = ink.Width * SpriteText.fontPixelZoom;

                    if (tightAdvance < advance)
                    {
                        drawShifts[index] = ink.Left * SpriteText.fontPixelZoom;
                        savedRoom += advance - tightAdvance;
                    }
                }
            }

            offsets[characters.Length] = previousWidth - savedRoom;
            layout = new NativeLayout(offsets, drawShifts);
            _nativeLayouts[key] = layout;

            return layout;
        }

        /// <summary>Where a narrow glyph's visible pixels sit within its cell, in SpriteText's unscaled pixels. False for a glyph that fills enough of its cell to keep its room, or whose pixels can't be read.</summary>
        private static bool TryGetNarrowInk(char character, out Rectangle ink)
        {
            ink = Rectangle.Empty;

            if (_readTexture is not Texture2D texture || TryGetSourceRectangle(character, out Rectangle sourceRectangle) is false || TryGetGlyphInk(texture, character, out Rectangle glyphInk) is false)
            {
                return false;
            }

            if (glyphInk.Width >= sourceRectangle.Width * NARROW_GLYPH_SHARE)
            {
                return false;
            }

            ink = glyphInk;
            return true;
        }

        /// <summary>Where a glyph's visible pixels sit within its cell, read once and kept. False when the cell can't be read or shows nothing.</summary>
        private static bool TryGetGlyphInk(Texture2D texture, char character, out Rectangle ink)
        {
            if (_glyphInks.TryGetValue(character, out Rectangle? knownInk) is false)
            {
                knownInk = null;

                try
                {
                    if (TryGetSourceRectangle(character, out Rectangle sourceRectangle) && TryReadInk(texture, sourceRectangle, out Rectangle readInk))
                    {
                        knownInk = readInk;
                    }
                }
                catch (Exception exception)
                {
                    Parchment.monitor.LogOnce($"Couldn't read a Junimo script glyph from SpriteText's sheet, so it keeps its usual room and is left out of the script's letter height: {exception.Message}", LogLevel.Trace);
                }

                _glyphInks[character] = knownInk;
            }

            ink = knownInk ?? Rectangle.Empty;
            return knownInk is not null;
        }

        /// <summary>The zoom that makes SpriteText's letters as tall as the line they sit on. SpriteText's height follows its zoom, so it is measured at the zoom it already has and scaled from there.</summary>
        private static float GetFontPixelZoom(float lineHeight)
        {
            float naturalHeight = SpriteText.getHeightOfString(LINE_HEIGHT_MEASURE_TEXT);

            if (naturalHeight <= 0f)
            {
                return SpriteText.fontPixelZoom;
            }

            return SpriteText.fontPixelZoom * lineHeight / naturalHeight;
        }

        /// <summary>Where the script's glyphs usually sit within their cells on SpriteText's sheet, read from the pixels of its <see cref="SCRIPT_REFERENCE_LETTERS"/>. False when the sheet or the glyphs can't be read.</summary>
        private static bool TryGetScriptLetters(out LetterBounds letters)
        {
            if (_hasLookedUpSpriteTexture is false)
            {
                _hasLookedUpSpriteTexture = true;
                _spriteTextureField = Parchment.modHelper.Reflection.GetField<Texture2D>(typeof(SpriteText), "spriteTexture", required: false);
            }

            // Read every time rather than kept, as SpriteText swaps in a new sheet when the language changes
            Texture2D? texture = _spriteTextureField?.GetValue();

            if (texture is null)
            {
                letters = default;
                return false;
            }

            if (ReferenceEquals(texture, _readTexture) is false)
            {
                _readTexture = texture;
                _sourceRectangles.Clear();
                _nativeLayouts.Clear();
                _glyphInks.Clear();
                _scriptLetters = ReadScriptLetters(texture);

                // Split again from the new sheet the next time they're needed
                _masks?.Dispose();
                _masks = null;
                _hasBuiltMasks = false;
            }

            letters = _scriptLetters ?? default;
            return _scriptLetters is not null;
        }

        /// <summary>The top and foot most of the reference glyphs share, so a glyph that reaches higher or lower than the rest doesn't shrink the whole script to fit it.</summary>
        private static LetterBounds? ReadScriptLetters(Texture2D texture)
        {
            List<int> tops = new List<int>();
            List<int> bottoms = new List<int>();

            // A glyph that can't be read is left out here rather than failing the rest, as reading each one already catches its own problems
            foreach (char letter in SCRIPT_REFERENCE_LETTERS)
            {
                if (TryGetGlyphInk(texture, letter, out Rectangle ink))
                {
                    tops.Add(ink.Top);
                    bottoms.Add(ink.Bottom);
                }
            }

            if (tops.Count is 0)
            {
                Parchment.monitor.LogOnce($"Couldn't read the Junimo script's glyphs from SpriteText's sheet (a {texture.Format} texture), so [junimo] text fills its line instead.", LogLevel.Trace);
                return null;
            }

            return new LetterBounds(GetMostCommon(tops), GetMostCommon(bottoms));
        }

        private static int GetMostCommon(List<int> values)
        {
            return values.GroupBy(value => value).OrderByDescending(group => group.Count()).ThenBy(group => group.Key).First().Key;
        }

        /// <summary>Where a glyph's Junimo script sprite sits on SpriteText's sheet, asked of SpriteText itself so it always matches what the game draws.</summary>
        private static bool TryGetSourceRectangle(char character, out Rectangle sourceRectangle)
        {
            if (_sourceRectangles.TryGetValue(character, out sourceRectangle))
            {
                return true;
            }

            if (_hasLookedUpSourceRectangle is false)
            {
                _hasLookedUpSourceRectangle = true;
                _getSourceRectangle = Parchment.modHelper.Reflection.GetMethod(typeof(SpriteText), "getSourceRectForChar", required: false);
            }

            if (_getSourceRectangle is null)
            {
                return false;
            }

            sourceRectangle = _getSourceRectangle.Invoke<Rectangle>(character, true);
            _sourceRectangles[character] = sourceRectangle;

            return true;
        }

        /// <summary>The smallest rectangle holding every visible pixel of a glyph's cell, counted from the cell's top left. The cell's unused top rows are skipped, so anything the sprite above reaches into them isn't counted.
        /// False when nothing in it shows, it falls outside the texture or the texture is compressed and can't be read a pixel at a time.
        /// </summary>
        private static bool TryReadInk(Texture2D texture, Rectangle area, out Rectangle ink)
        {
            ink = Rectangle.Empty;

            if (texture.Format is not SurfaceFormat.Color || area.Width <= 0 || area.Height <= SHEET_CELL_TOP_PADDING || area.Left < 0 || area.Top < 0 || area.Right > texture.Width || area.Bottom > texture.Height)
            {
                return false;
            }

            Rectangle readArea = new Rectangle(area.X, area.Y + SHEET_CELL_TOP_PADDING, area.Width, area.Height - SHEET_CELL_TOP_PADDING);
            Color[] pixels = new Color[readArea.Width * readArea.Height];
            texture.GetData(0, readArea, pixels, 0, pixels.Length);

            int left = readArea.Width;
            int right = -1;
            int top = readArea.Height;
            int bottom = -1;

            for (int row = 0; row < readArea.Height; row++)
            {
                for (int column = 0; column < readArea.Width; column++)
                {
                    if (pixels[row * readArea.Width + column].A is 0)
                    {
                        continue;
                    }

                    left = Math.Min(left, column);
                    right = Math.Max(right, column);
                    top = Math.Min(top, row);
                    bottom = Math.Max(bottom, row);
                }
            }

            if (right < 0)
            {
                return false;
            }

            // Counted from the cell's own top rather than the rows read, so it lines up with the rest of the cell
            ink = new Rectangle(left, top + SHEET_CELL_TOP_PADDING, right - left + 1, bottom - top + 1);

            return true;
        }
    }
}
