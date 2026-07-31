using StardewModdingAPI;
using System;
using System.Collections.Generic;

namespace Parchment.Framework.Integrations
{
    public interface IContentPatcherApi
    {
        void RegisterToken(IManifest mod, string name, Func<IEnumerable<string>> getValue);
    }
}
