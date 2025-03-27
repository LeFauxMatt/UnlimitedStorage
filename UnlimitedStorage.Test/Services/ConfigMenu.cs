using LeFauxMods.Common.Services;

namespace LeFauxMods.UnlimitedStorage.Test.Services;

/// <inheritdoc />
internal sealed class ConfigMenu(IModHelper helper, IManifest manifest)
    : BaseConfigMenu<ModConfig>(helper, manifest)
{
    /// <inheritdoc />
    protected override ModConfig Config => ModState.ConfigHelper.Temp;

    /// <inheritdoc />
    protected override ConfigHelper<ModConfig> ConfigHelper => ModState.ConfigHelper;

    /// <inheritdoc />
    protected internal override void SetupOptions() =>
        this.Api.AddTextOption(
            this.Manifest,
            () => this.Config.LastSave,
            value => this.Config.LastSave = value,
            () => "Last Save",
            () => "The last save file that was loaded");
}