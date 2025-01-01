using System.Globalization;
using System.Text;
using LeFauxMods.Common.Interface;
using LeFauxMods.Common.Models;
using StardewModdingAPI.Utilities;

namespace LeFauxMods.UnlimitedStorage;

/// <inheritdoc cref="IModConfig{TConfig}" />
internal sealed class ModConfig : IModConfig<ModConfig>, IConfigWithLogAmount
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

    /// <summary>Gets or sets a value indicating whether to enable search.</summary>
    public bool EnableSearch { get; set; } = true;

    /// <summary>Gets or sets a value indicating whether to show arrows.</summary>
    public bool ShowArrows { get; set; } = true;

    /// <summary>Gets or sets the keybind to show the search bar.</summary>
    public KeybindList ToggleSearch { get; set; } =
        new(new Keybind(SButton.LeftControl, SButton.F),
            new Keybind(SButton.RightControl, SButton.F));

    /// <inheritdoc />
    public LogAmount LogAmount { get; set; }

    /// <inheritdoc />
    public void CopyTo(ModConfig other)
    {
        other.LogAmount = this.LogAmount;
        other.BigChestMenu = this.BigChestMenu;
        other.EnableScrolling = this.EnableScrolling;
        other.EnableSearch = this.EnableSearch;
        other.ShowArrows = this.ShowArrows;
        other.EnabledIds.Clear();
        other.EnabledIds.UnionWith(this.EnabledIds);
    }

    public string GetSummary() =>
        new StringBuilder()
            .AppendLine(CultureInfo.InvariantCulture, $"{nameof(this.BigChestMenu),25}: {this.BigChestMenu}")
            .AppendLine(CultureInfo.InvariantCulture, $"{nameof(this.EnableScrolling),25}: {this.EnableScrolling}")
            .AppendLine(CultureInfo.InvariantCulture, $"{nameof(this.EnableSearch),25}: {this.EnableSearch}")
            .AppendLine(CultureInfo.InvariantCulture, $"{nameof(this.ShowArrows),25}: {this.ShowArrows}")
            .AppendLine(CultureInfo.InvariantCulture,
                $"{nameof(this.EnabledIds),25}: {string.Join(',', this.EnabledIds)}")
            .ToString();
}