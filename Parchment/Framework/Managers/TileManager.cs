using Microsoft.Xna.Framework;
using Parchment.Framework.UI.Menus;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Delegates;
using StardewValley.Triggers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using xTile.Dimensions;
using xTile.ObjectModel;
using xTile.Tiles;

namespace Parchment.Framework.Managers
{
    public class TileManager : BaseManager
    {
        private const string BookIndicatorProperty = "PeacefulEnd.Parchment_BookIndicator";

        private Dictionary<TemporaryAnimatedSprite, string> _indicatorsToQueries;

        public TileManager(IMonitor monitor, IModHelper helper) : base(monitor, helper)
        {
            _indicatorsToQueries = new Dictionary<TemporaryAnimatedSprite, string>();

            helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
            helper.Events.Player.Warped += OnWarped;
        }

        private void OnUpdateTicked(object? sender, StardewModdingAPI.Events.UpdateTickedEventArgs e)
        {
            if (e.IsMultipleOf(30))
            {
                foreach (var indicator in _indicatorsToQueries)
                {
                    if (indicator.Key is null)
                    {
                        continue;
                    }

                    if (GameStateQuery.CheckConditions(indicator.Value) is false)
                    {
                        _indicatorsToQueries.Remove(indicator.Key);
                        Game1.currentLocation.removeTemporarySpritesWithID(indicator.Key.id);
                    }
                }
            }
        }

        private void OnWarped(object? sender, StardewModdingAPI.Events.WarpedEventArgs e)
        {
            _indicatorsToQueries.Clear();

            SpawnBookIndicators(e.NewLocation);
        }

        private void SpawnBookIndicators(GameLocation location)
        {
            // Get any tiles flagged as a book indicator
            var layer = location.Map?.GetLayer("Buildings");
            if (layer is null)
            {
                return;
            }

            for (int x = 0; x < layer.LayerWidth; x++)
            {
                for (int y = 0; y < layer.LayerHeight; y++)
                {
                    var tile = layer.Tiles[x, y];
                    if (tile is null || TryGetBookIndicatorProperty(tile, out string bookIndicatorProperty) is false)
                    {
                        continue;
                    }

                    string[] args = ArgUtility.SplitBySpaceQuoteAware(bookIndicatorProperty);
                    if (ArgUtility.TryGetVector2(args, 0, out Vector2 offset, out string error, name: "offset") is false)
                    {
                        monitor.LogOnce($"The tile at ({x}, {y}) on the Buildings layer with the property \"{BookIndicatorProperty}\" has an invalid offset value. Must follow the format: X Y \"GSQ\"", LogLevel.Warn);
                        continue;
                    }
                    if (ArgUtility.TryGetOptionalRemainder(args, 2, out string gameStateQuery) is false || string.IsNullOrWhiteSpace(gameStateQuery))
                    {
                        monitor.LogOnce($"The tile at ({x}, {y}) on the Buildings layer with the property \"{BookIndicatorProperty}\" has an invalid game state query. Must follow the format: X Y \"GSQ\"", LogLevel.Warn);
                        continue;
                    }

                    if (GameStateQuery.CheckConditions(gameStateQuery))
                    {
                        var sprite = new TemporaryAnimatedSprite("LooseSprites\\Cursors", new Microsoft.Xna.Framework.Rectangle(144, 447, 15, 15), new Vector2((x * 64f) + offset.X, (y * 64f) + offset.Y - 96f - 16f), flipped: false, 0f, Color.White)
                        {
                            interval = 99999f,
                            animationLength = 1,
                            totalNumberOfLoops = 9999,
                            yPeriodic = true,
                            yPeriodicLoopTime = 4000f,
                            yPeriodicRange = 16f,
                            layerDepth = 1f,
                            scale = 4f,
                            id = (x + y) * 5000
                        };

                        _indicatorsToQueries.Add(sprite, gameStateQuery);
                        location.temporarySprites.Add(sprite);
                    }
                }
            }
        }

        private static bool TryGetBookIndicatorProperty(Tile tile, out string value)
        {
            // Tile properties take precedence over tilesheet properties, matching GameLocation.doesTileHaveProperty
            if (tile.Properties.TryGetValue(BookIndicatorProperty, out PropertyValue property) is false && tile.TileIndexProperties.TryGetValue(BookIndicatorProperty, out property) is false)
            {
                value = string.Empty;
                return false;
            }

            value = property?.ToString() ?? string.Empty;
            return string.IsNullOrEmpty(value) is false;
        }
    }
}
