using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Parchment.Framework.Models.Data.Elements;
using Parchment.Framework.Models.Data.Results;
using Parchment.Framework.Models.Enums;
using Parchment.Framework.Models.Interfaces;
using Parchment.Framework.Utilities.Helpers;
using System.Collections.Generic;

namespace Parchment.Framework.Models.Data
{
    /// <summary>A container that arranges its children into cells of a fixed size, left to right and then top to bottom.
    /// Parchment's other containers stack vertically or place absolutely, so this is what lays anything out across the page.
    /// </summary>
    public class GridElementData : ElementData, ISprite, ILayeredContainer
    {
        public override ElementType Type => ElementType.Grid;

        /// <summary>Optional. When omitted the grid has no frame behind its cells. Note: the texture must support 9-slice scaling.</summary>
        public string? TexturePath { get; set; }
        public Rectangle? TextureSourceRectangle { get; set; }
        public Rectangle? HoverTextureSourceRectangle { get; set; }
        public string? TintColor { get; set; }
        public SpriteEffects SpriteEffects { get; set; }

        /// <summary>The elements filling the cells, in order. A child whose condition fails takes no cell at all, so the ones after it move up to close the gap.</summary>
        public List<ElementData>? Children { get; set; }

        /// <summary>Elements drawn behind <see cref="Children"/>, positioned via <see cref="ElementData.Position"/> rather than filling cells. These do not affect the grid's size.</summary>
        public List<ElementData>? Background { get; set; }

        /// <summary>Elements drawn over <see cref="Children"/>, positioned via <see cref="ElementData.Position"/> rather than filling cells. These do not affect the grid's size.</summary>
        public List<ElementData>? Foreground { get; set; }

        /// <summary>How many cells sit side by side before the next row starts. Required.</summary>
        public int Columns { get; set; } = 1;

        /// <summary>The most rows the grid draws. When null the grid is as tall as its children need, and when set the cells past the last row are dropped the way a page drops content past its bottom.</summary>
        public int? Rows { get; set; }

        /// <summary>A cell's width in unscaled sprite pixels, multiplied by <see cref="ElementData.Scale"/>. Required, and the same for every cell: a grid's geometry is declared rather than measured, so one child can't resize the others.</summary>
        public int CellWidth { get; set; }

        /// <summary>A cell's height in unscaled sprite pixels, multiplied by <see cref="ElementData.Scale"/>. Required.</summary>
        public int CellHeight { get; set; }

        /// <summary>Space between columns in unscaled sprite pixels, multiplied by <see cref="ElementData.Scale"/>. Not applied outside the outermost columns, which is what <see cref="Padding"/> is for.</summary>
        public int ColumnSpacing { get; set; } = 0;

        /// <summary>Space between rows in unscaled sprite pixels, multiplied by <see cref="ElementData.Scale"/>.</summary>
        public int RowSpacing { get; set; } = 0;

        /// <summary>Increases the space between the cells and the grid's border.</summary>
        public int Padding { get; set; } = 0;

        /// <summary>Fills the cells from an item query instead of from <see cref="Children"/>, narrowed by what the reader has typed. See the Grid reference for what this does to Children.</summary>
        public ResultsData? Results { get; set; }

        /// <summary>How many cells a Results block fills, from its own Count or from the grid's shape. Zero when the grid has no Results.</summary>
        public int GetSlotCount()
        {
            if (Results is null)
            {
                return 0;
            }

            return Results.Count ?? (Rows is int rows ? rows * Columns : 0);
        }

        public override (bool Result, string Error) IsValid()
        {
            if (Columns <= 0)
            {
                return (false, $"\"Columns\" must be positive.");
            }

            if (Rows is int rows && rows <= 0)
            {
                return (false, $"\"Rows\" must be positive.");
            }

            if (CellWidth <= 0)
            {
                return (false, $"\"CellWidth\" is required and must be positive.");
            }

            if (CellHeight <= 0)
            {
                return (false, $"\"CellHeight\" is required and must be positive.");
            }

            if (ColumnSpacing < 0 || RowSpacing < 0)
            {
                return (false, $"\"ColumnSpacing\" and \"RowSpacing\" cannot be negative.");
            }

            if (Padding < 0)
            {
                return (false, $"\"Padding\" cannot be negative.");
            }

            if (Results is not null)
            {
                var resultsIsValidData = Results.IsValid();
                if (resultsIsValidData.Result is false)
                {
                    return (false, $"[Results] {resultsIsValidData.Error}");
                }

                if (GetSlotCount() <= 0)
                {
                    return (false, $"\"Results\" needs a \"Count\", or a \"Rows\" on the grid to work one out from.");
                }
            }

            var childrenIsValidData = ElementValidationHelper.ValidateElements(Children);
            if (childrenIsValidData.Result is false)
            {
                return (false, $"[Children] {childrenIsValidData.Error}");
            }

            var backgroundIsValidData = ElementValidationHelper.ValidateElements(Background);
            if (backgroundIsValidData.Result is false)
            {
                return (false, $"[Background] {backgroundIsValidData.Error}");
            }

            var foregroundIsValidData = ElementValidationHelper.ValidateElements(Foreground);
            if (foregroundIsValidData.Result is false)
            {
                return (false, $"[Foreground] {foregroundIsValidData.Error}");
            }

            return base.IsValid();
        }
    }
}
