using System.Globalization;
using System.Text;
using LeFauxMods.Common.Interface;
using LeFauxMods.Common.Models;
using LeFauxMods.UnlimitedStorage.Models;
using StardewModdingAPI.Utilities;

namespace LeFauxMods.UnlimitedStorage;

/// <inheritdoc cref="IModConfig{TConfig}" />
internal sealed class ModConfig : IModConfig<ModConfig>, IConfigWithLogAmount
{
    /// <summary>Gets or sets a value indicating whether to enable scroll wheel.</summary>
    public bool EnableScrolling { get; set; } = true;

    /// <summary>Gets or sets a value indicating whether to enable search.</summary>
    public bool EnableSearch { get; set; } = true;

    /// <summary>Gets or sets a value indicating whether to show arrows.</summary>
    public bool ShowArrows { get; set; } = true;

    /// <summary>Gets or sets the storage options.</summary>
    public Dictionary<string, StorageOptions> StorageOptions { get; set; } = new(StringComparer.OrdinalIgnoreCase)
    {
        { "130", new StorageOptions() },
        { "165", new StorageOptions() },
        { "216", new StorageOptions() },
        { "232", new StorageOptions() },
        { "248", new StorageOptions() },
        { "256", new StorageOptions() },
        { "275", new StorageOptions() },
        { "BigChest", new StorageOptions() },
        { "BigStoneChest", new StorageOptions() }
    };

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
        other.EnableScrolling = this.EnableScrolling;
        other.EnableSearch = this.EnableSearch;
        other.ShowArrows = this.ShowArrows;
        other.StorageOptions.Clear();
        foreach (var (key, value) in this.StorageOptions)
        {
            other.StorageOptions.Add(key, value);
        }
    }

    /// <inheritdoc />
    public string GetSummary()
    {
        var sb = new StringBuilder()
            .AppendLine(CultureInfo.InvariantCulture, $"{nameof(this.EnableScrolling),25}: {this.EnableScrolling}")
            .AppendLine(CultureInfo.InvariantCulture, $"{nameof(this.EnableSearch),25}: {this.EnableSearch}")
            .AppendLine(CultureInfo.InvariantCulture, $"{nameof(this.ShowArrows),25}: {this.ShowArrows}");

        foreach (var (itemId, storageOptions) in this.StorageOptions)
        {
            sb.AppendLine(itemId)
                .AppendLine(CultureInfo.InvariantCulture,
                    $"{nameof(storageOptions.Capacity),25}: {storageOptions.Capacity}")
                .AppendLine(CultureInfo.InvariantCulture,
                    $"{nameof(storageOptions.Enabled),25}: {storageOptions.Enabled}")
                .AppendLine(CultureInfo.InvariantCulture,
                    $"{nameof(storageOptions.MenuHeight),25}: {storageOptions.MenuHeight}")
                .AppendLine(CultureInfo.InvariantCulture,
                    $"{nameof(storageOptions.MenuWidth),25}: {storageOptions.MenuWidth}");
        }

        return sb.ToString();
    }
}