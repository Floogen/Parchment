namespace Parchment.Framework.Models.Enums
{
    /// <summary>How long a variable's value lasts.</summary>
    public enum VariableScope
    {
        /// <summary>Kept on the player, so it saves with the game and each save file has its own value. Multiplayer players each keep their own.</summary>
        Save,
        /// <summary>Kept once per installation, shared by every save file. For a setting the reader shouldn't have to set again on a new farm.</summary>
        Global
    }
}
