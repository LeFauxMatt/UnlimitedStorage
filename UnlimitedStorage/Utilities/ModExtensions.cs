using LeFauxMods.Common.Utilities;
using LeFauxMods.UnlimitedStorage.Services;
using StardewValley.Menus;
using StardewValley.Objects;

namespace LeFauxMods.UnlimitedStorage.Utilities;

/// <summary>Encapsulates mod extensions.</summary>
internal static class ModExtensions
{
    public static bool IsEnabled(this Chest chest) =>
        Game1.bigCraftableData.TryGetValue(chest.ItemId, out var data) &&
        data.CustomFields?.GetBool(Constants.ModEnabled) == true;

    public static int GetMaxOffset(this InventoryMenu inventoryMenu, Chest chest) =>
        (int)Math.Ceiling((float)chest.GetItemsForPlayer().Count / StateManager.Columns) - inventoryMenu.rows;
}
