using LeFauxMods.Common.Models;
using LeFauxMods.Common.Utilities;
using LeFauxMods.UnlimitedStorage.Services;
using LeFauxMods.UnlimitedStorage.Utilities;
using Microsoft.Xna.Framework;
using StardewModdingAPI.Events;
using StardewValley.GameData.BigCraftables;
using StardewValley.Locations;
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
        ModState.Init(helper);
        Log.Init(this.Monitor, ModState.Config);
        ModPatches.Apply();

        // Events
        helper.Events.Content.AssetRequested += OnAssetRequested;
        helper.Events.Display.MenuChanged += OnMenuChanged;
        helper.Events.GameLoop.GameLaunched += this.OnGameLaunched;
        helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
        helper.Events.Input.ButtonPressed += this.OnButtonPressed;
        helper.Events.Input.ButtonsChanged += this.OnButtonsChanged;
        helper.Events.Player.Warped += OnWarped;
    }

    private static void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
    {
        if (!ModState.Config.StorageOptions.Any() || !e.NameWithoutLocale.IsEquivalentTo(ModConstants.BigCraftableData))
        {
            return;
        }

        // Add config options to the data
        e.Edit(static assetData =>
            {
                var data = assetData.AsDictionary<string, BigCraftableData>().Data;
                foreach (var (itemId, storageOptions) in ModState.Config.StorageOptions)
                {
                    if (!data.TryGetValue(itemId, out var bigCraftableData) || !storageOptions.Enabled ||
                        storageOptions.GetData() is not { } dict)
                    {
                        continue;
                    }

                    bigCraftableData.CustomFields ??= [];
                    foreach (var (key, value) in dict)
                    {
                        bigCraftableData.CustomFields[key] = value;
                    }
                }
            },
            AssetEditPriority.Late);
    }

    private static void OnMenuChanged(object? sender, MenuChangedEventArgs e)
    {
        if (!ModState.TryGetMenu(out var menu, out var inventoryMenu, out var chest))
        {
            ModState.Offset = 0;
            ModState.Columns = 0;
            return;
        }

        if ((e.OldMenu as ItemGrabMenu)?.context == menu.context)
        {
            return;
        }

        ModState.Offset = 0;
        ModState.Columns = inventoryMenu.capacity / inventoryMenu.rows;

        if (ModState.Config.ShowArrows)
        {
            var topSlot = inventoryMenu.inventory[ModState.Columns - 1];
            var bottomSlot = inventoryMenu.inventory[inventoryMenu.capacity - 1];

            // Align up arrow to top-right slot
            ModState.UpArrow.bounds.X = inventoryMenu.xPositionOnScreen + inventoryMenu.width + 8;
            ModState.UpArrow.bounds.Y =
                inventoryMenu.inventory[ModState.Columns - 1].bounds.Center.Y - (6 * Game1.pixelZoom);
            ModState.UpArrow.rightNeighborID = topSlot.rightNeighborID;
            ModState.UpArrow.leftNeighborID = topSlot.myID;
            topSlot.rightNeighborID = SharedConstants.UpArrowId;

            // Align down arrow to bottom-right slot
            ModState.DownArrow.bounds.X = inventoryMenu.xPositionOnScreen + inventoryMenu.width + 8;
            ModState.DownArrow.bounds.Y =
                inventoryMenu.inventory[inventoryMenu.capacity - 1].bounds.Center.Y - (6 * Game1.pixelZoom);
            ModState.DownArrow.rightNeighborID = bottomSlot.rightNeighborID;
            ModState.DownArrow.leftNeighborID = bottomSlot.myID;
            bottomSlot.rightNeighborID = SharedConstants.DownArrowId;

            menu.allClickableComponents.Add(ModState.UpArrow);
            menu.allClickableComponents.Add(ModState.DownArrow);
        }

        if (ModState.Config.EnableSearch)
        {
            var top = inventoryMenu.GetBorder(InventoryMenu.BorderSide.Top);
            ModState.TextBox.Width = top[^1].bounds.Right - top[0].bounds.Left;
            ModState.TextBox.X = top[0].bounds.Left;
        }
    }

    private static void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
        switch (Game1.player.currentLocation)
        {
            case FarmHouse { fridge.Value: { } fridge }:
                fridge.ItemId = ModState.IsEnabled("216") ? "216" : "130";
                break;
            case IslandFarmHouse { fridge.Value: { } fridge }:
                fridge.ItemId = ModState.IsEnabled("216") ? "216" : "130";
                break;
        }
    }

    private static void OnWarped(object? sender, WarpedEventArgs e)
    {
        switch (e.NewLocation)
        {
            case FarmHouse { fridge.Value: { } fridge }:
                fridge.ItemId = ModState.IsEnabled("216") ? "216" : "130";
                break;
            case IslandFarmHouse { fridge.Value: { } fridge }:
                fridge.ItemId = ModState.IsEnabled("216") ? "216" : "130";
                break;
        }
    }

    private void OnButtonsChanged(object? sender, ButtonsChangedEventArgs e)
    {
        if (!ModState.TryGetMenu(out _, out _, out _) || !ModState.Config.ToggleSearch.JustPressed())
        {
            return;
        }

        ModState.Config.EnableSearch = !ModState.Config.EnableSearch;
        this.Helper.Input.SuppressActiveKeybinds(ModState.Config.ToggleSearch);
    }

    private void OnRenderedActiveMenu(object? sender, RenderedActiveMenuEventArgs e)
    {
        if (!ModState.TryGetMenu(out var menu, out var inventoryMenu, out var chest))
        {
            return;
        }

        var cursor = ModState.Cursor;
        if (ModState.Config.ShowArrows)
        {
            var maxOffset = inventoryMenu.GetMaxOffset(chest);
            ModState.UpArrow.tryHover(cursor.X, cursor.Y);
            ModState.UpArrow.draw(
                e.SpriteBatch,
                ModState.Offset > 0 ? Color.White : Color.Gray * 0.8f,
                1f);

            ModState.DownArrow.tryHover(cursor.X, cursor.Y);
            ModState.DownArrow.draw(
                e.SpriteBatch,
                ModState.Offset < maxOffset * ModState.Columns ? Color.White : Color.Gray * 0.8f,
                1f);
        }

        if (ModState.Config.EnableSearch)
        {
            ModState.TextBox.Y = inventoryMenu.yPositionOnScreen - ModState.TextBox.Height - (13 * Game1.pixelZoom);

            // Adjust for Chests Anywhere
            if (this.Helper.ModRegistry.IsLoaded(ModConstants.ChestsAnywhereId))
            {
                ModState.TextBox.Y -= 52;

                // Adjust for Color Picker
                if (menu.chestColorPicker?.visible == true)
                {
                    ModState.TextBox.Y -= Game1.tileSize;
                }
            }
            else
            {
                // Adjust for Color Picker
                if (menu.chestColorPicker?.visible == true)
                {
                    ModState.TextBox.Y += 2 * Game1.pixelZoom;
                }
            }

            // Adjust for Large Chest
            if (inventoryMenu.rows >= 5)
            {
                ModState.TextBox.Y += 5 * Game1.pixelZoom;
            }

            ModState.TextBox.Hover(cursor.X, cursor.Y);
            ModState.TextBox.Draw(e.SpriteBatch, false);
        }

        menu.drawMouse(e.SpriteBatch);
    }

    private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
    {
        if (!ModState.TryGetMenu(out var menu, out var inventoryMenu, out var chest))
        {
            return;
        }

        var cursor = ModState.Cursor;
        var maxOffset = ModState.Columns == 0 ? 0 : inventoryMenu.GetMaxOffset(chest);
        switch (e.Button)
        {
            case SButton.DPadUp or SButton.LeftThumbstickUp when maxOffset > 0:
                if (ModState.TryMoveUp(menu, inventoryMenu))
                {
                    this.Helper.Input.Suppress(e.Button);
                }

                return;

            case SButton.DPadDown or SButton.LeftThumbstickDown when maxOffset > 0:
                if (ModState.TryMoveDown(menu, inventoryMenu, maxOffset))
                {
                    this.Helper.Input.Suppress(e.Button);
                }

                return;

            // Press up button
            case SButton.MouseLeft or SButton.ControllerA
                when ModState.Config.ShowArrows && ModState.UpArrow.containsPoint(cursor.X, cursor.Y):
                if (ModState.Offset <= 0)
                {
                    return;
                }

                ModState.UpArrow.scale = 3f;
                ModState.Offset -= ModState.Columns;
                this.Helper.Input.Suppress(e.Button);
                _ = Game1.playSound("drumkit6");
                return;

            // Press down button
            case SButton.MouseLeft or SButton.ControllerA
                when ModState.Config.ShowArrows && ModState.DownArrow.containsPoint(cursor.X, cursor.Y):
                if (ModState.Offset >= maxOffset * ModState.Columns)
                {
                    return;
                }

                ModState.DownArrow.scale = 3f;
                ModState.Offset += ModState.Columns;
                this.Helper.Input.Suppress(e.Button);
                _ = Game1.playSound("drumkit6");
                return;

            // Select search bar
            case SButton.MouseLeft when ModState.Config.EnableSearch:
                ModState.TextBox.Selected = cursor.X >= ModState.TextBox.X &&
                                            cursor.X <= ModState.TextBox.X + ModState.TextBox.Width &&
                                            cursor.Y >= ModState.TextBox.Y &&
                                            cursor.Y <= ModState.TextBox.Y + ModState.TextBox.Height;
                return;

            // Clear search bar
            case SButton.MouseRight when ModState.Config.EnableSearch:
                ModState.TextBox.Selected = cursor.X >= ModState.TextBox.X &&
                                            cursor.X <= ModState.TextBox.X + ModState.TextBox.Width &&
                                            cursor.Y >= ModState.TextBox.Y &&
                                            cursor.Y <= ModState.TextBox.Y + ModState.TextBox.Height;
                if (ModState.TextBox.Selected)
                {
                    ModState.TextBox.Text = string.Empty;
                }

                return;

            // Deselect search bar
            case SButton.ControllerB when ModState.Config.EnableSearch:
                ModState.TextBox.Selected = false;
                return;

            case SButton.Escape:
                if (!menu.readyToClose())
                {
                    return;
                }

                this.Helper.Input.Suppress(e.Button);
                Game1.playSound("bigDeSelect");
                menu.exitThisMenu();
                return;

            case not (SButton.LeftShift
                or SButton.RightShift
                or SButton.LeftAlt
                or SButton.RightAlt
                or SButton.LeftControl
                or SButton.RightControl) when ModState.TextBox.Selected:
                this.Helper.Input.Suppress(e.Button);
                return;
        }
    }

    private void OnConfigChanged(ConfigChangedEventArgs<ModConfig> e)
    {
        _ = this.Helper.GameContent.InvalidateCache(ModConstants.BigCraftableData);
        this.Helper.Events.Display.RenderedActiveMenu -= this.OnRenderedActiveMenu;
        this.Helper.Events.Input.MouseWheelScrolled -= this.OnMouseWheelScrolled;

        if (e.Config.EnableScrolling)
        {
            this.Helper.Events.Input.MouseWheelScrolled += this.OnMouseWheelScrolled;
        }

        if (e.Config.ShowArrows || e.Config.EnableSearch)
        {
            this.Helper.Events.Display.RenderedActiveMenu += this.OnRenderedActiveMenu;
        }
    }

    private void OnGameLaunched(object? sender, GameLaunchedEventArgs e) =>
        _ = new ConfigMenu(this.Helper, this.ModManifest);

    private void OnMouseWheelScrolled(object? sender, MouseWheelScrolledEventArgs e)
    {
        if (!ModState.TryGetMenu(out _, out var inventoryMenu, out var chest))
        {
            return;
        }

        var cursor = ModState.Cursor;
        if (!inventoryMenu.isWithinBounds(cursor.X, cursor.Y))
        {
            return;
        }

        ModState.Offset += e.Delta > 0 ? -ModState.Columns : ModState.Columns;
    }
}