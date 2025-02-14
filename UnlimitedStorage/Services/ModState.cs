using LeFauxMods.Common.Models;
using LeFauxMods.Common.Services;
using LeFauxMods.Common.Utilities;
using LeFauxMods.UnlimitedStorage.Models;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI.Utilities;
using StardewValley.Inventories;
using StardewValley.Menus;
using StardewValley.Objects;

namespace LeFauxMods.UnlimitedStorage.Services;

/// <summary>Responsible for managing state.</summary>
internal sealed class ModState
{
    private static ModState? Instance;

    private readonly PerScreen<int> columns = new();
    private readonly ConfigHelper<ModConfig> configHelper;

    private readonly PerScreen<ClickableTextureComponent> downArrow = new(static () =>
        new ClickableTextureComponent(
            new Rectangle(0, 0, 11 * Game1.pixelZoom, 12 * Game1.pixelZoom), Game1.mouseCursors,
            new Rectangle(421, 472, 11, 12),
            Game1.pixelZoom) { myID = SharedConstants.DownArrowId, upNeighborID = SharedConstants.UpArrowId });

    private readonly IModHelper helper;
    private readonly PerScreen<int> offset = new();

    private readonly PerScreen<TextBox> textBox = new(static () =>
        new TextBox(
            Game1.content.Load<Texture2D>("LooseSprites/textBox"),
            null,
            Game1.smallFont,
            Game1.textColor) { limitWidth = false });

    private readonly PerScreen<ClickableTextureComponent> upArrow = new(static () =>
        new ClickableTextureComponent(
            new Rectangle(0, 0, 11 * Game1.pixelZoom, 12 * Game1.pixelZoom), Game1.mouseCursors,
            new Rectangle(421, 459, 11, 12),
            Game1.pixelZoom) { myID = SharedConstants.UpArrowId, downNeighborID = SharedConstants.DownArrowId });

    private Dictionary<string, StorageOptions>? data;

    private ModState(IModHelper helper)
    {
        this.helper = helper;
        this.configHelper = new ConfigHelper<ModConfig>(helper);
    }

    public static ModConfig Config => Instance!.configHelper.Config;

    public static ConfigHelper<ModConfig> ConfigHelper => Instance!.configHelper;

    public static Point Cursor =>
        Utility.ModifyCoordinatesForUIScale(Instance!.helper.Input.GetCursorPosition().GetScaledScreenPixels())
            .ToPoint();

    public static Dictionary<string, StorageOptions> Data => Instance!.data ??= Instance.GetData();

    public static ClickableTextureComponent DownArrow => Instance!.downArrow.Value;

    public static ClickableTextureComponent UpArrow => Instance!.upArrow.Value;

    public static TextBox TextBox => Instance!.textBox.Value;

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

    public static bool IsEnabled(string itemId) =>
        Data.TryGetValue(itemId, out var storageOptions) && storageOptions.Enabled;

    public static void Init(IModHelper helper) => Instance ??= new ModState(helper);

    public static bool TryGetMenu(
        [NotNullWhen(true)] out ItemGrabMenu? menu,
        [NotNullWhen(true)] out InventoryMenu? inventoryMenu,
        [NotNullWhen(true)] out IInventory? inventory)
    {
        if (Game1.activeClickableMenu is not ItemGrabMenu { ItemsToGrabMenu: { } itemsToGrabMenu } itemGrabMenu)
        {
            menu = null;
            inventoryMenu = null;
            inventory = null;
            return false;
        }

        menu = itemGrabMenu;
        inventoryMenu = itemsToGrabMenu;
        switch (itemGrabMenu.sourceItem)
        {
            case SObject { heldObject.Value: Chest heldChest } sourceObject
                when IsEnabled(sourceObject.ItemId):
                inventory = heldChest.GetItemsForPlayer();
                return true;
            case Chest sourceItem when IsEnabled(sourceItem.ItemId):
                inventory = sourceItem.GetItemsForPlayer();
                return true;
        }

        switch (itemGrabMenu.context)
        {
            // Chests Anywhere
            case GameLocation location when location.IsBuildableLocation():
                inventory = (location as Farm ?? Game1.getFarm()).getShippingBin(Game1.player);
                return true;
        }

        inventory = null;
        return false;
    }

    public static bool TryMoveDown(ItemGrabMenu itemGrabMenu, InventoryMenu inventoryMenu, int maxOffset)
    {
        if (Offset >= maxOffset * Columns)
        {
            return false;
        }

        if (itemGrabMenu.currentlySnappedComponent is not { } component)
        {
            return false;
        }

        var bottom = inventoryMenu.GetBorder(InventoryMenu.BorderSide.Bottom);
        if (!bottom.Contains(component))
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

        if (itemGrabMenu.currentlySnappedComponent is not { } component)
        {
            return false;
        }

        if (!inventoryMenu.GetBorder(InventoryMenu.BorderSide.Top).Contains(component))
        {
            return false;
        }

        Offset -= Columns;
        return true;
    }

    private static Func<Dictionary<string, string>?> GetCustomFields(string itemId) =>
        () => Game1.bigCraftableData.TryGetValue(itemId, out var bigCraftableData)
            ? bigCraftableData.CustomFields
            : null;

    private Dictionary<string, StorageOptions> GetData()
    {
        this.data ??= new Dictionary<string, StorageOptions>(StringComparer.OrdinalIgnoreCase);
        foreach (var (itemId, _) in Game1.bigCraftableData)
        {
            if (!Config.StorageOptions.TryGetValue(itemId, out var storageOptions) || !storageOptions.Enabled)
            {
                continue;
            }

            var customFields = new DictionaryModel(GetCustomFields(itemId));
            this.data[itemId] = new StorageOptions(customFields);
        }

        return this.data;
    }
}