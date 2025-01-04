using LeFauxMods.Common.Integrations.GenericModConfigMenu;
using LeFauxMods.Common.Services;
using StardewValley.TokenizableStrings;

namespace LeFauxMods.UnlimitedStorage.Services;

/// <summary>Responsible for handling the mod configuration menu.</summary>
internal sealed class ConfigMenu
{
    private readonly IGenericModConfigMenuApi api = null!;
    private readonly GenericModConfigMenuIntegration gmcm;
    private readonly IManifest manifest;
    private readonly IModHelper helper = null!;

    public ConfigMenu(IModHelper helper, IManifest manifest)
    {
        this.manifest = manifest;
        this.gmcm = new GenericModConfigMenuIntegration(manifest, helper.ModRegistry);
        if (!this.gmcm.IsLoaded)
        {
            return;
        }

        this.api = this.gmcm.Api;
        this.helper = helper;
        this.SetupMenu();
    }

    private static ModConfig Config => ModState.ConfigHelper.Temp;

    private static ConfigHelper<ModConfig> ConfigHelper => ModState.ConfigHelper;

    private void SetupMenu()
    {
        this.gmcm.Register(ConfigHelper.Reset, ConfigHelper.Save);

        this.api.AddKeybindList(
            this.manifest,
            static () => Config.ToggleSearch,
            static value => Config.ToggleSearch = value,
            I18n.ConfigOption_ToggleSearch_Name,
            I18n.ConfigOption_ToggleSearch_Description);

        this.api.AddBoolOption(
            this.manifest,
            static () => Config.BigChestMenu,
            static value => Config.BigChestMenu = value,
            I18n.ConfigOption_BigChestsMenu_Name,
            I18n.ConfigOption_BigChestsMenu_Description);

        this.api.AddBoolOption(
            this.manifest,
            static () => Config.EnableScrolling,
            static value => Config.EnableScrolling = value,
            I18n.ConfigOption_EnableScrolling_Name,
            I18n.ConfigOption_EnableScrolling_Description);

        this.api.AddBoolOption(
            this.manifest,
            static () => Config.EnableSearch,
            static value => Config.EnableSearch = value,
            I18n.ConfigOption_EnableSearch_Name,
            I18n.ConfigOption_EnableSearch_Description);

        this.api.AddBoolOption(
            this.manifest,
            static () => Config.ShowArrows,
            static value => Config.ShowArrows = value,
            I18n.ConfigOption_ShowArrows_Name,
            I18n.ConfigOption_ShowArrows_Description);

        this.api.AddSectionTitle(this.manifest, I18n.ConfigOption_MakeUnlimited_Name);
        this.api.AddParagraph(this.manifest, I18n.ConfigOption_MakeUnlimited_Description);

        
        var EndabledIds = ConfigHelper.Default.EnabledIds;

        // If the FilteredChestHopper mod is present, add hoppers as an option; otherwise remove them
        if (this.helper.ModRegistry.IsLoaded(Constants.FilteredChestHopper))
        {
            EndabledIds.Add("275");
        }
        else
        {
            EndabledIds.Remove("275");
        }

        foreach (var id in EndabledIds)
        {
            if (!Game1.bigCraftableData.TryGetValue(id, out var data))
            {
                continue;
            }

            this.api.AddBoolOption(
                this.manifest,
                () => Config.EnabledIds.Contains(id),
                value =>
                {
                    if (value)
                    {
                        Config.EnabledIds.Add(id);
                    }
                    else
                    {
                        Config.EnabledIds.Remove(id);
                    }
                },
                () => TokenParser.ParseText(data.DisplayName),
                () => TokenParser.ParseText(data.Description));
        }
    }
}