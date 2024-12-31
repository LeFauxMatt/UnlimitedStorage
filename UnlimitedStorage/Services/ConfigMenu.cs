using LeFauxMods.Common.Integrations.GenericModConfigMenu;
using StardewValley.TokenizableStrings;

namespace LeFauxMods.UnlimitedStorage.Services;

internal sealed class ConfigMenu
{
    private readonly IGenericModConfigMenuApi api = null!;
    private readonly GenericModConfigMenuIntegration gmcm;
    private readonly IManifest manifest;

    public ConfigMenu(IModHelper helper, IManifest manifest)
    {
        this.manifest = manifest;
        this.gmcm = new GenericModConfigMenuIntegration(manifest, helper.ModRegistry);
        if (!this.gmcm.IsLoaded)
        {
            return;
        }

        this.api = this.gmcm.Api;
        this.SetupMenu();
    }

    private void SetupMenu()
    {
        this.gmcm.Register(StateManager.ConfigHelper.Reset, StateManager.ConfigHelper.Save);

        this.api.AddBoolOption(
            this.manifest,
            () => StateManager.ConfigHelper.Temp.BigChestMenu,
            value => StateManager.ConfigHelper.Temp.BigChestMenu = value,
            I18n.ConfigOption_BigChestsMenu_Name,
            I18n.ConfigOption_BigChestsMenu_Description);

        this.api.AddBoolOption(
            this.manifest,
            () => StateManager.ConfigHelper.Temp.EnableScrolling,
            value => StateManager.ConfigHelper.Temp.EnableScrolling = value,
            I18n.ConfigOption_EnableScrolling_Name,
            I18n.ConfigOption_EnableScrolling_Description);

        this.api.AddBoolOption(
            this.manifest,
            () => StateManager.ConfigHelper.Temp.ShowArrows,
            value => StateManager.ConfigHelper.Temp.ShowArrows = value,
            I18n.ConfigOption_ShowArrows_Name,
            I18n.ConfigOption_ShowArrows_Description);

        this.api.AddSectionTitle(this.manifest, I18n.ConfigOption_MakeUnlimited_Name);
        this.api.AddParagraph(this.manifest, I18n.ConfigOption_MakeUnlimited_Description);

        foreach (var id in StateManager.ConfigHelper.Default.EnabledIds)
        {
            if (!Game1.bigCraftableData.TryGetValue(id, out var data))
            {
                continue;
            }

            this.api.AddBoolOption(
                this.manifest,
                () => StateManager.ConfigHelper.Temp.EnabledIds.Contains(id),
                value =>
                {
                    if (value)
                    {
                        StateManager.ConfigHelper.Temp.EnabledIds.Add(id);
                    }
                    else
                    {
                        StateManager.ConfigHelper.Temp.EnabledIds.Remove(id);
                    }
                },
                () => TokenParser.ParseText(data.DisplayName),
                () => TokenParser.ParseText(data.Description));
        }
    }
}
