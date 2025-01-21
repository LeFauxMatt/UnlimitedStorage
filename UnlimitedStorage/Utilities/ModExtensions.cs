using LeFauxMods.UnlimitedStorage.Services;
using StardewValley.Menus;
using StardewValley.Objects;

namespace LeFauxMods.UnlimitedStorage.Utilities;

/// <summary>Encapsulates mod extensions.</summary>
internal static class ModExtensions
{
    public static int GetMaxOffset(this InventoryMenu inventoryMenu, Chest chest) =>
        (int)Math.Ceiling((float)chest.GetItemsForPlayer().Count / ModState.Columns) - inventoryMenu.rows;

    public static IEnumerable<Item?> OrderBySearch(this IEnumerable<Item?> items) =>
        items.OrderByDescending(static
                item => item is not null &&
                        (item.DisplayName.Contains(ModState.TextBox.Text, StringComparison.OrdinalIgnoreCase)
                         || item.getDescription()
                             .Contains(ModState.TextBox.Text, StringComparison.OrdinalIgnoreCase)))
            .ThenByDescending(static item => item?.GetContextTags().Any(static tag =>
                tag.Contains(ModState.TextBox.Text, StringComparison.OrdinalIgnoreCase)) == true);
}