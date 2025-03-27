using LeFauxMods.Common.Integrations.FauxCore;
using LeFauxMods.Common.Utilities;
using LeFauxMods.UnlimitedStorage.Test.Services;
using StardewModdingAPI.Events;

namespace LeFauxMods.UnlimitedStorage.Test;

/// <inheritdoc />
internal sealed class ModEntry : Mod
{
    private FauxCoreIntegration fauxCore = null!;

    /// <inheritdoc />
    public override void Entry(IModHelper helper)
    {
        // Init
        ModState.Init(helper);
        Log.Init(this.Monitor);
        this.fauxCore = new FauxCoreIntegration(helper.ModRegistry);

        // Events
        helper.Events.GameLoop.GameLaunched += this.OnGameLaunched;
    }

    private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
    {
        _ = new ConfigMenu(this.Helper, this.ModManifest);

        if (this.fauxCore.IsLoaded)
        {
            this.fauxCore.Api.LoadLastSave(ModState.ConfigHelper.Temp);
        }
    }
}