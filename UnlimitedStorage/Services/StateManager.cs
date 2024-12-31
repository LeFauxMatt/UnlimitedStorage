using LeFauxMods.Common.Services;
using Microsoft.Xna.Framework;
using StardewModdingAPI.Utilities;
using StardewValley.Menus;
using StardewValley.Objects;

namespace LeFauxMods.UnlimitedStorage.Services;

/// <summary>Responsible for managing state.</summary>
internal sealed class StateManager
{
    private static StateManager? Instance;

    private readonly PerScreen<int> columns = new();

    private readonly ConfigHelper<ModConfig> configHelper;

    private readonly PerScreen<ClickableTextureComponent> downArrow = new(() => new ClickableTextureComponent(
        Rectangle.Empty, Game1.mouseCursors, new Rectangle(421, 472, 11, 12),
        Game1.pixelZoom)
    {
        myID = 69_420,
        upNeighborID = 69_421
    });

    private readonly IModHelper helper;

    private readonly PerScreen<int> offset = new();

    private readonly PerScreen<ClickableTextureComponent> upArrow = new(() => new ClickableTextureComponent(
        Rectangle.Empty, Game1.mouseCursors, new Rectangle(421, 459, 11, 12),
        Game1.pixelZoom)
    {
        myID = 69_421,
        downNeighborID = 69_420
    });

    private StateManager(IModHelper helper)
    {
        this.helper = helper;
        this.configHelper = new ConfigHelper<ModConfig>(helper);
    }

    public static ModConfig Config => Instance!.configHelper.Config;

    public static ConfigHelper<ModConfig> ConfigHelper => Instance!.configHelper;

    public static Point Cursor =>
        Utility.ModifyCoordinatesForUIScale(Instance!.helper.Input.GetCursorPosition().GetScaledScreenPixels())
            .ToPoint();

    public static ClickableTextureComponent DownArrow => Instance!.downArrow.Value;

    public static ClickableTextureComponent UpArrow => Instance!.upArrow.Value;

    public static int Columns
    {
        get => Instance!.columns.Value;
        set => Instance!.columns.Value = value;
    }

    public static int Offset
    {
        get => Instance!.offset.Value;
        set => Instance!.offset.Value = value;
    }

    public static void Init(IModHelper helper) => Instance ??= new StateManager(helper);

    public static bool TryGetMenu(
        [NotNullWhen(true)] out ItemGrabMenu? menu,
        [NotNullWhen(true)] out InventoryMenu? inventoryMenu,
        [NotNullWhen(true)] out Chest? chest)
    {
        if (Game1.activeClickableMenu is ItemGrabMenu
            {
                ItemsToGrabMenu: { } itemsToGrabMenu, sourceItem: Chest sourceItem
            } itemGrabMenu)
        {
            menu = itemGrabMenu;
            inventoryMenu = itemsToGrabMenu;
            chest = sourceItem;
            return true;
        }

        menu = null;
        inventoryMenu = null;
        chest = null;
        return false;
    }

    public static bool TryMoveDown(ItemGrabMenu itemGrabMenu, InventoryMenu inventoryMenu, int maxOffset)
    {
        if (Offset >= maxOffset * Columns)
        {
            return false;
        }

        if (itemGrabMenu.currentlySnappedComponent is not { } slot)
        {
            return false;
        }

        var index = inventoryMenu.inventory.IndexOf(slot);
        if (index == -1 || index < inventoryMenu.capacity - Columns)
        {
            return false;
        }

        Offset += Columns;
        return true;
    }

    public static bool TryMoveUp(ItemGrabMenu itemGrabMenu, InventoryMenu inventoryMenu)
    {
        if (Offset <= 0)
        {
            return false;
        }

        if (itemGrabMenu.currentlySnappedComponent is not { } slot)
        {
            return false;
        }

        var index = inventoryMenu.inventory.IndexOf(slot);
        if (index == -1 || index >= Columns)
        {
            return false;
        }

        Offset -= Columns;
        return true;
    }
}
