using Microsoft.Xna.Framework;
using Parchment.Framework.Models;
using Parchment.Framework.UI.Menus;
using StardewValley;
using StardewValley.Extensions;
using StardewValley.GameData.Shops;
using StardewValley.Internal;
using System;
using System.Linq;
using xTile.Tiles;

namespace Parchment.Framework.Utilities
{
    public static class MapActionHelper
    {        
        public static void HandleOpenBook(GameLocation location, string[] args, Farmer player, Vector2 tile)
        {
            if (ArgUtility.TryGet(args, 1, out string bookId, out string error) is false || Parchment.bookManager.CreateBook(bookId) is not Book book || book is null)
            {
                return;
            }

            Game1.activeClickableMenu = new BookMenu(book);
        }
    }
}
