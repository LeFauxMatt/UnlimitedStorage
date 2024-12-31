using LeFauxMods.Common.Interface;
using LeFauxMods.Common.Models;

namespace LeFauxMods.UnlimitedStorage;

/// <summary>Represents the mod configuration.</summary>
internal sealed class ModConfig : IConfigWithCopyTo<ModConfig>, IConfigWithLogAmount
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

    /// <summary>Gets or sets a value indicating whether to enable scroll wheel.</summary>
    public bool EnableScrolling { get; set; } = true;

    /// <summary>Gets or sets a value indicating whether to show arrows.</summary>
    public bool ShowArrows { get; set; } = true;

    /// <inheritdoc />
    public void CopyTo(ModConfig other)
    {
        other.LogAmount = this.LogAmount;
        other.BigChestMenu = this.BigChestMenu;
        other.EnableScrolling = this.EnableScrolling;
        other.ShowArrows = this.ShowArrows;
        other.EnabledIds.Clear();
        other.EnabledIds.UnionWith(this.EnabledIds);
    }

    /// <inheritdoc />
    public LogAmount LogAmount { get; set; }
}
