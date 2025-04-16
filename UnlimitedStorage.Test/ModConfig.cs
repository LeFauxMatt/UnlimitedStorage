using System.Globalization;
using System.Text;
using LeFauxMods.Common.Integrations.FauxCore;
using LeFauxMods.Common.Interface;

namespace LeFauxMods.UnlimitedStorage.Test;

/// <inheritdoc cref="IModConfig{TConfig}" />
internal sealed class ModConfig : IModConfig<ModConfig>, IConfigWithLastSave
{
    /// <inheritdoc />
    public string LastSave { get; set; } = string.Empty;

    /// <inheritdoc />
    public void CopyTo(ModConfig other) => other.LastSave = this.LastSave;

    /// <inheritdoc />
    public string GetSummary() =>
        new StringBuilder()
            .AppendLine(CultureInfo.InvariantCulture, $"{nameof(this.LastSave),25}: {this.LastSave}")
            .ToString();
}