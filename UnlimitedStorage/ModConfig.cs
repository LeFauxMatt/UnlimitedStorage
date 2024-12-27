using LeFauxMods.Common.Interface;
using LeFauxMods.Common.Models;

namespace LeFauxMods.UnlimitedStorage;

/// <summary>Represents the mod configuration.</summary>
internal sealed class ModConfig : IConfigWithLogAmount
{
    /// <inheritdoc />
    public LogAmount LogAmount { get; set; }

    /// <summary>Gets or sets a value indicating whether to make all chests big.</summary>
    public bool MakeChestsBig { get; set; }

    /// <summary>
    ///     Copies the values from this instance to another instance.
    /// </summary>
    /// <param name="other">The other config instance.</param>
    public void CopyTo(ModConfig other)
    {
        other.LogAmount = this.LogAmount;
        other.MakeChestsBig = this.MakeChestsBig;
    }
}
