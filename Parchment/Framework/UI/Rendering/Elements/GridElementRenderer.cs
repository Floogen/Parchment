using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Parchment.Framework.Models;
using Parchment.Framework.Models.Data;
using Parchment.Framework.UI.Layouts;
using Parchment.Framework.Utilities.Helpers;
using StardewModdingAPI;
using StardewValley.Menus;
using System;
using System.Collections.Generic;

namespace Parchment.Framework.UI.Rendering.Elements
{
    public class GridElementRenderer : ElementRenderer<GridElementData>
    {
        public override Rectangle GetContentBounds(Element element, Rectangle bounds)
        {
            if (element.LayoutState is not GridLayout gridLayout)
            {
                return bounds;
            }

            return new Rectangle(bounds.X + gridLayout.Inset, bounds.Y + gridLayout.Inset, bounds.Width - gridLayout.Inset * 2, bounds.Height - gridLayout.Inset * 2);
        }

        protected override Vector2 Measure(GridElementData data, Element element, ElementRenderContext context)
        {
            int borderThickness = GetBorderThickness(data, element);
            int inset = borderThickness + (int)(data.Padding * data.Scale);

            float cellWidth = data.CellWidth * data.Scale;
            float cellHeight = data.CellHeight * data.Scale;
            float columnSpacing = data.ColumnSpacing * data.Scale;
            float rowSpacing = data.RowSpacing * data.Scale;

            ElementRenderContext cellContext = context.WithSize(cellWidth, cellHeight);
            element.LayoutState = new GridLayout(cellContext, inset);

            int rowCount = PlaceCells(data, element, cellContext, cellWidth, cellHeight, columnSpacing, rowSpacing);

            float contentWidth = data.Columns * cellWidth + Math.Max(0, data.Columns - 1) * columnSpacing;
            float contentHeight = rowCount * cellHeight + Math.Max(0, rowCount - 1) * rowSpacing;

            // The placed layers are measured against the content area the grid settled on, so they can't feed back into the size that produced it
            PlaceLayers(element, context.WithSize(contentWidth, contentHeight));

            return new Vector2(Math.Max(contentWidth + inset * 2f, borderThickness * 2f), Math.Max(contentHeight + inset * 2f, borderThickness * 2f));
        }

        /// <summary>Fills the cells in order and returns how many rows came of it. A hidden child takes no cell, so the children after it move up rather than leaving a hole.</summary>
        private static int PlaceCells(GridElementData data, Element element, ElementRenderContext cellContext, float cellWidth, float cellHeight, float columnSpacing, float rowSpacing)
        {
            int cellIndex = 0;
            int maximumCells = data.Rows is int rows ? rows * data.Columns : int.MaxValue;

            foreach (Element child in element.Children)
            {
                if (child.IsVisible is false)
                {
                    child.Bounds = Rectangle.Empty;
                    continue;
                }

                if (cellIndex >= maximumCells)
                {
                    child.Bounds = Rectangle.Empty;

                    Parchment.monitor.LogOnce($"A Grid has more visible children than its {data.Columns}x{data.Rows} cells hold, so the ones past the last cell are not drawn.", LogLevel.Trace);
                    continue;
                }

                Vector2 childSize = child.Renderer.Measure(child, cellContext);

                float cellX = (cellIndex % data.Columns) * (cellWidth + columnSpacing);
                float cellY = (cellIndex / data.Columns) * (cellHeight + rowSpacing);

                // A cell is a box the child sits in rather than a box the child fills, so the usual alignment fields decide where in it the child lands
                int alignedX = (int)AlignmentHelper.GetAlignedX(availableWidth: cellWidth, contentWidth: childSize.X, alignment: child.Data.Alignment);
                int alignedY = (int)AlignmentHelper.GetAlignedY(availableHeight: cellHeight, contentHeight: childSize.Y, alignment: child.Data.VerticalAlignment);

                child.Bounds = new Rectangle((int)cellX + alignedX + child.Data.Position.X, (int)cellY + alignedY + child.Data.Position.Y, (int)childSize.X, (int)childSize.Y);

                cellIndex++;
            }

            if (data.Rows is int fixedRows)
            {
                return fixedRows;
            }

            return cellIndex is 0 ? 0 : (cellIndex + data.Columns - 1) / data.Columns;
        }

        private static void PlaceLayers(Element element, ElementRenderContext placedContext)
        {
            if (element.LayoutState is not GridLayout gridLayout)
            {
                return;
            }

            gridLayout.PlacedContext = placedContext;

            Page.PositionElements(element.Background, placedContext);
            Page.PositionElements(element.Foreground, placedContext);
        }

        private static int GetBorderThickness(GridElementData data, Element element)
        {
            if (element.Texture is null || element.Texture.IsDisposed || SpriteHelper.GetDrawSourceRectangle(data, element) is not Rectangle sourceRectangle)
            {
                return 0;
            }

            return NineSliceHelper.GetBorderThickness(sourceRectangle, data.Scale);
        }

        private static void DrawLayer(SpriteBatch spriteBatch, IReadOnlyList<Element> layer, Rectangle contentBounds, ElementRenderContext placedContext)
        {
            foreach (Element placedElement in layer)
            {
                if (placedElement.Bounds == Rectangle.Empty)
                {
                    continue;
                }

                Rectangle placedBounds = new Rectangle(contentBounds.X + placedElement.Bounds.X, contentBounds.Y + placedElement.Bounds.Y, placedElement.Bounds.Width, placedElement.Bounds.Height);
                placedElement.Renderer.Draw(spriteBatch, placedElement, placedBounds, placedContext);
            }
        }

        protected override void Draw(SpriteBatch spriteBatch, GridElementData data, Element element, Rectangle bounds, ElementRenderContext context)
        {
            if (element.LayoutState is not GridLayout gridLayout)
            {
                return;
            }

            if (element.Texture is not null && element.Texture.IsDisposed is false)
            {
                IClickableMenu.drawTextureBox(spriteBatch, element.Texture, data.TextureSourceRectangle ?? element.Texture.Bounds, bounds.X, bounds.Y, bounds.Width, bounds.Height, element.TintColor, data.Scale, false);
            }

            Rectangle contentBounds = GetContentBounds(element, bounds);

            DrawLayer(spriteBatch, element.Background, contentBounds, gridLayout.PlacedContext);

            foreach (Element child in element.Children)
            {
                if (child.Bounds == Rectangle.Empty)
                {
                    continue;
                }

                Rectangle childBounds = new Rectangle(contentBounds.X + child.Bounds.X, contentBounds.Y + child.Bounds.Y, child.Bounds.Width, child.Bounds.Height);
                child.Renderer.Draw(spriteBatch, child, childBounds, gridLayout.CellContext);
            }

            DrawLayer(spriteBatch, element.Foreground, contentBounds, gridLayout.PlacedContext);
        }
    }
}
