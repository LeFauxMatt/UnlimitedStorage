using LeFauxMods.Common.Interface;
using LeFauxMods.Common.Models;

namespace LeFauxMods.UnlimitedStorage;

/// <summary>Represents the mod configuration.</summary>
internal sealed class ModConfig : IConfigWithLogAmount
{
    /// <summary>Gets or sets a value indicating whether to make all chests big.</summary>
    public bool BigChestMenu { get; set; }

    /// <summary>Gets or sets the list of ids that this mod is enabled for.</summary>
    public HashSet<string> EnabledIds { get; set; } = new(StringComparer.OrdinalIgnoreCase)
    {
        "130", // Chest
        "216", // Mini-Fridge
        "232", // Stone Chest
        "248", // Mini-Shipping Bin
        "256", // Junimo Chest
        "BigChest",
        "BigStoneChest"
    };

    /// <inheritdoc />
    public LogAmount LogAmount { get; set; }

    /// <summary>
    ///     Copies the values from this instance to another instance.
    /// </summary>
    /// <param name="other">The other config instance.</param>
    public void CopyTo(ModConfig other)
    {
        other.BigChestMenu = this.BigChestMenu;
        other.EnabledIds.Clear();
        other.EnabledIds.UnionWith(this.EnabledIds);
        other.LogAmount = this.LogAmount;
    }
}
