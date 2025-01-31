using LeFauxMods.UnlimitedStorage.Services;
using StardewValley.Inventories;
using StardewValley.Menus;

namespace LeFauxMods.UnlimitedStorage.Utilities;

/// <summary>Encapsulates mod extensions.</summary>
internal static class ModExtensions
{
    public static int GetMaxOffset(this InventoryMenu inventoryMenu, IInventory inventory) =>
        (int)Math.Ceiling((float)inventory.Count / ModState.Columns) - inventoryMenu.rows;

    public static IEnumerable<Item?> OrderBySearch(this IEnumerable<Item?> items) =>
        items.OrderByDescending(static
                item => item is not null &&
                        (item.DisplayName.Contains(ModState.TextBox.Text, StringComparison.OrdinalIgnoreCase)
                         || item.getDescription()
                             .Contains(ModState.TextBox.Text, StringComparison.OrdinalIgnoreCase)))
            .ThenByDescending(static item => item?.GetContextTags().Any(static tag =>
                tag.Contains(ModState.TextBox.Text, StringComparison.OrdinalIgnoreCase)) == true);
}