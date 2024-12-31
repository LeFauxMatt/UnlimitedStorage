using LeFauxMods.Common.Models;
using LeFauxMods.Common.Utilities;
using LeFauxMods.UnlimitedStorage.Services;
using LeFauxMods.UnlimitedStorage.Utilities;
using Microsoft.Xna.Framework;
using StardewModdingAPI.Events;
using StardewValley.GameData.BigCraftables;
using StardewValley.Menus;

namespace LeFauxMods.UnlimitedStorage;

/// <inheritdoc />
internal sealed class ModEntry : Mod
{
    /// <inheritdoc />
    public override void Entry(IModHelper helper)
    {
        // Init
        ModEvents.Subscribe<ConfigChangedEventArgs<ModConfig>>(this.OnConfigChanged);
        I18n.Init(helper.Translation);
        StateManager.Init(helper);
        Log.Init(this.Monitor, StateManager.Config);
        ModPatches.Apply();

        // Events
        helper.Events.Content.AssetRequested += OnAssetRequested;
        helper.Events.Display.MenuChanged += OnMenuChanged;
        helper.Events.GameLoop.GameLaunched += this.OnGameLaunched;
        helper.Events.Input.ButtonPressed += this.OnButtonPressed;
    }

    private static void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
    {
        if (!e.NameWithoutLocale.IsEquivalentTo(Constants.BigCraftableData))
        {
            return;
        }


        // Add config options to the data
        e.Edit(asset =>
            {
                var allData = asset.AsDictionary<string, BigCraftableData>().Data;
                foreach (var id in StateManager.Config.EnabledIds)
                {
                    if (!allData.TryGetValue(id, out var data))
                    {
                        continue;
                    }

                    data.CustomFields ??= new Dictionary<string, string>();
                    data.CustomFields.Add(Constants.ModEnabled, "true");
                }
            },
            AssetEditPriority.Late);
    }

    private static void OnMenuChanged(object? sender, MenuChangedEventArgs e)
    {
        if (!StateManager.TryGetMenu(out var menu, out var inventoryMenu, out var chest) ||
            !chest.IsEnabled())
        {
            StateManager.Offset = 0;
            StateManager.Columns = 0;
            return;
        }

        if ((e.OldMenu as ItemGrabMenu)?.context == menu.context)
        {
            return;
        }

        StateManager.Offset = 0;
        StateManager.Columns = inventoryMenu.capacity / inventoryMenu.rows;

        if (!StateManager.Config.ShowArrows)
        {
            return;
        }

        // Align up arrow to top-right slot
        var topSlot = inventoryMenu.inventory[StateManager.Columns - 1];
        StateManager.UpArrow.rightNeighborID = topSlot.rightNeighborID;
        StateManager.UpArrow.leftNeighborID = topSlot.myID;
        topSlot.rightNeighborID = StateManager.UpArrow.myID;

        StateManager.UpArrow.bounds = new Rectangle(
            inventoryMenu.xPositionOnScreen + inventoryMenu.width + 8,
            inventoryMenu.inventory[StateManager.Columns - 1].bounds.Center.Y - (6 * Game1.pixelZoom),
            11 * Game1.pixelZoom,
            12 * Game1.pixelZoom);

        // Align down arrow to bottom-right slot
        var bottomSlot = inventoryMenu.inventory[inventoryMenu.capacity - 1];
        StateManager.DownArrow.rightNeighborID = bottomSlot.rightNeighborID;
        StateManager.DownArrow.leftNeighborID = bottomSlot.myID;
        bottomSlot.rightNeighborID = StateManager.DownArrow.myID;

        StateManager.DownArrow.bounds = new Rectangle(
            inventoryMenu.xPositionOnScreen + inventoryMenu.width + 8,
            inventoryMenu.inventory[inventoryMenu.capacity - 1].bounds.Center.Y - (6 * Game1.pixelZoom),
            11 * Game1.pixelZoom,
            12 * Game1.pixelZoom);
    }

    private static void OnRenderedActiveMenu(object? sender, RenderedActiveMenuEventArgs e)
    {
        if (StateManager.Columns == 0 || !StateManager.TryGetMenu(out var menu, out var inventoryMenu, out var chest))
        {
            return;
        }

        var maxOffset = inventoryMenu.GetMaxOffset(chest);
        if (maxOffset <= 0)
        {
            return;
        }

        var cursor = StateManager.Cursor;
        if (StateManager.Offset > 0)
        {
            StateManager.UpArrow.tryHover(cursor.X, cursor.Y);
            StateManager.UpArrow.draw(e.SpriteBatch);
        }

        if (StateManager.Offset < maxOffset * StateManager.Columns)
        {
            StateManager.DownArrow.tryHover(cursor.X, cursor.Y);
            StateManager.DownArrow.draw(e.SpriteBatch);
        }

        menu.drawMouse(e.SpriteBatch);
    }

    private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
    {
        if (StateManager.Columns == 0 || !StateManager.TryGetMenu(out var menu, out var inventoryMenu, out var chest))
        {
            return;
        }

        var maxoffset = inventoryMenu.GetMaxOffset(chest);
        if (maxoffset <= 0)
        {
            return;
        }

        switch (e.Button)
        {
            case SButton.DPadUp or SButton.LeftThumbstickUp:
                if (StateManager.TryMoveUp(menu, inventoryMenu))
                {
                    break;
                }

                return;

            case SButton.DPadDown or SButton.LeftThumbstickDown:
                if (StateManager.TryMoveDown(menu, inventoryMenu, maxoffset))
                {
                    break;
                }

                return;

            case SButton.MouseLeft or SButton.ControllerA when StateManager.Config.ShowArrows:
                var cursor = StateManager.Cursor;
                if (StateManager.Offset > 0 && StateManager.UpArrow.containsPoint(cursor.X, cursor.Y))
                {
                    StateManager.UpArrow.scale = 3f;
                    StateManager.Offset -= StateManager.Columns;
                    Game1.playSound("drumkit6");
                    break;
                }

                if (StateManager.Offset < maxoffset * StateManager.Columns &&
                    StateManager.DownArrow.containsPoint(cursor.X, cursor.Y))
                {
                    StateManager.DownArrow.scale = 3f;
                    StateManager.Offset += StateManager.Columns;
                    Game1.playSound("drumkit6");
                    break;
                }

                return;

            default:
                return;
        }

        this.Helper.Input.Suppress(e.Button);
        Game1.playSound("drumkit6");
    }

    private void OnConfigChanged(ConfigChangedEventArgs<ModConfig> e)
    {
        this.Helper.GameContent.InvalidateCache(Constants.BigCraftableData);
        this.Helper.Events.Display.RenderedActiveMenu -= OnRenderedActiveMenu;
        this.Helper.Events.Input.MouseWheelScrolled -= this.OnMouseWheelScrolled;

        if (e.Config.EnableScrolling)
        {
            this.Helper.Events.Input.MouseWheelScrolled += this.OnMouseWheelScrolled;
        }

        if (e.Config.ShowArrows)
        {
            this.Helper.Events.Display.RenderedActiveMenu += OnRenderedActiveMenu;
        }

        Log.Info("""
                 Config Summary:
                 - BigChestMenu: {0}
                 - EnableScrolling: {1}
                 - ShowArrows: {2}
                 - EnabledIds: {3}
                 """,
            e.Config.BigChestMenu,
            e.Config.EnableScrolling,
            e.Config.ShowArrows,
            string.Join(',', e.Config.EnabledIds));
    }

    private void OnGameLaunched(object? sender, GameLaunchedEventArgs e) =>
        _ = new ConfigMenu(this.Helper, this.ModManifest);

    private void OnMouseWheelScrolled(object? sender, MouseWheelScrolledEventArgs e)
    {
        if (StateManager.Columns == 0 ||
            !StateManager.TryGetMenu(out _, out var inventoryMenu, out _))
        {
            return;
        }

        var cursor = StateManager.Cursor;
        if (!inventoryMenu.isWithinBounds(cursor.X, cursor.Y))
        {
            return;
        }

        StateManager.Offset += e.Delta > 0 ? -StateManager.Columns : StateManager.Columns;
    }
}
