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
using xTile.Tiles;

namespace Parchment.Framework.Managers
{
    public class TileManager : BaseManager
    {
        private Dictionary<TemporaryAnimatedSprite, string> _markersToQueries;

        public TileManager(IMonitor monitor, IModHelper helper) : base(monitor, helper)
        {
            _markersToQueries = new Dictionary<TemporaryAnimatedSprite, string>();

            helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
            helper.Events.Player.Warped += OnWarped;
        }

        private void OnUpdateTicked(object? sender, StardewModdingAPI.Events.UpdateTickedEventArgs e)
        {
            if (e.IsMultipleOf(30))
            {
                foreach (var marker in _markersToQueries)
                {
                    if (marker.Key is null)
                    {
                        continue;
                    }

                    if (GameStateQuery.CheckConditions(marker.Value) is false)
                    {
                        _markersToQueries.Remove(marker.Key);
                        Game1.currentLocation.removeTemporarySpritesWithID(marker.Key.id);
                    }
                }
            }
        }

        private void OnWarped(object? sender, StardewModdingAPI.Events.WarpedEventArgs e)
        {
            _markersToQueries.Clear();

            SpawnBookMarkers(e.NewLocation);
        }

        private void SpawnBookMarkers(GameLocation location)
        {
            // Get tent tiles
            var layer = location.Map.GetLayer("Buildings");

            for (int x = 0; x < layer.LayerWidth; x++)
            {
                for (int y = 0; y < layer.LayerHeight; y++)
                {
                    var bookMakerProperty = location.doesTileHaveProperty(x, y, "ParchmentBookMarker", "Buildings");
                    if (string.IsNullOrEmpty(bookMakerProperty) is false)
                    {
                        string[] args = ArgUtility.SplitBySpaceQuoteAware(bookMakerProperty);
                        if (ArgUtility.TryGetVector2(args, 0, out Vector2 offset, out string error, name: "offset") is false)
                        {
                            monitor.LogOnce($"The tile at ({x}, {y}) on the Buildings layer with the property \"ParchmentBookMarker\" has an invalid offset value. Must follow the format: X Y \"GSQ\"", LogLevel.Warn);
                            continue;
                        }
                        if (ArgUtility.TryGetOptionalRemainder(args, 2, out string gameStateQuery) is false || string.IsNullOrWhiteSpace(gameStateQuery))
                        {
                            monitor.LogOnce($"The tile at ({x}, {y}) on the Buildings layer with the property \"ParchmentBookMarker\" has an invalid game state query. Must follow the format: X Y \"GSQ\"", LogLevel.Warn);
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

                            _markersToQueries.Add(sprite, gameStateQuery);
                            location.temporarySprites.Add(sprite);
                        }
                    }
                }
            }
        }
    }
}
