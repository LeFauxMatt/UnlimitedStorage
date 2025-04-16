using LeFauxMods.Common.Services;

namespace LeFauxMods.UnlimitedStorage.Services;

/// <inheritdoc />
internal sealed class ConfigMenu(IModHelper helper, IManifest manifest)
    : BaseConfigMenu<ModConfig>(helper, manifest)
{
    /// <inheritdoc />
    protected override ModConfig Config => ModState.ConfigHelper.Temp;

    /// <inheritdoc />
    protected override ConfigHelper<ModConfig> ConfigHelper => ModState.ConfigHelper;

    /// <inheritdoc />
    protected internal override void SetupOptions()
    {
        this.Api.AddKeybindList(
            this.Manifest,
            () => this.Config.ToggleSearch,
            value => this.Config.ToggleSearch = value,
            I18n.ConfigOption_ToggleSearch_Name,
            I18n.ConfigOption_ToggleSearch_Description);

        this.Api.AddBoolOption(
            this.Manifest,
            () => this.Config.EnableScrolling,
            value => this.Config.EnableScrolling = value,
            I18n.ConfigOption_EnableScrolling_Name,
            I18n.ConfigOption_EnableScrolling_Description);

        this.Api.AddBoolOption(
            this.Manifest,
            () => this.Config.EnableSearch,
            value => this.Config.EnableSearch = value,
            I18n.ConfigOption_EnableSearch_Name,
            I18n.ConfigOption_EnableSearch_Description);

        this.Api.AddBoolOption(
            this.Manifest,
            () => this.Config.ShowArrows,
            value => this.Config.ShowArrows = value,
            I18n.ConfigOption_ShowArrows_Name,
            I18n.ConfigOption_ShowArrows_Description);

        this.GMCM.AddComplexOption(new UnlimitedOption(this.Helper));
    }
}