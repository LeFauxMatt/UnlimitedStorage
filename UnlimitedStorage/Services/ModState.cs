using System.Runtime.CompilerServices;
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

    private readonly ConfigHelper<ModConfig> configHelper;
    private readonly IModHelper helper;
    private readonly PerScreen<int> offset = new();

    private readonly PerScreen<ConditionalWeakTable<IClickableMenu, CachedContext?>> cachedContexts =
        new(static () => new ConditionalWeakTable<IClickableMenu, CachedContext?>());

    private readonly PerScreen<ClickableTextureComponent> downArrow = new(static () =>
        new ClickableTextureComponent(
            new Rectangle(0, 0, 11 * Game1.pixelZoom, 12 * Game1.pixelZoom), Game1.mouseCursors,
            new Rectangle(421, 472, 11, 12),
            Game1.pixelZoom) { myID = SharedConstants.DownArrowId, upNeighborID = SharedConstants.UpArrowId });


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
        ModEvents.Subscribe<ConfigChangedEventArgs<ModConfig>>(this.OnConfigChanged);
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

    public static int Offset
    {
        get => Instance!.offset.Value;
        set => Instance!.offset.Value = value;
    }

    public static void Init(IModHelper helper) => Instance ??= new ModState(helper);

    public static bool TryGetContext([NotNullWhen(true)] out CachedContext? context)
    {
        switch (Game1.activeClickableMenu)
        {
            case { } menu when Instance!.cachedContexts.Value.TryGetValue(menu, out context):
                return context is not null;

            case ItemGrabMenu { ItemsToGrabMenu: { } itemsToGrabMenu, inventory: { } inventoryMenu } itemGrabMenu:
                StorageOptions? storageOptions = null;
                var inventory = itemGrabMenu.sourceItem switch
                {
                    SObject { heldObject.Value: Chest heldChest } sourceObject when Data.TryGetValue(
                            sourceObject.ItemId, out storageOptions) &&
                        storageOptions.Enabled => heldChest.GetItemsForPlayer(),
                    Chest sourceItem when Data.TryGetValue(sourceItem.ItemId, out storageOptions) &&
                                          storageOptions.Enabled => sourceItem.GetItemsForPlayer(),
                    null when itemGrabMenu.context is GameLocation location && location.IsBuildableLocation() &&
                              Data.TryGetValue(ModConstants.MiniShippingBinId, out storageOptions) &&
                              storageOptions.Enabled => (location as Farm ?? Game1.getFarm()).getShippingBin(
                        Game1.player),
                    _ => null
                };

                context = inventory is not null && storageOptions?.Enabled == true
                    ? new CachedContext(itemGrabMenu, itemsToGrabMenu, inventoryMenu, inventory, storageOptions)
                    : null;

                Instance.cachedContexts.Value.AddOrUpdate(itemGrabMenu, context);
                return context is not null;

            default:
                context = null;
                return false;
        }
    }

    private static Func<Dictionary<string, string>?> GetCustomFields(string itemId) =>
        () => Game1.bigCraftableData.TryGetValue(itemId, out var bigCraftableData)
            ? bigCraftableData.CustomFields
            : null;

    private void OnConfigChanged(ConfigChangedEventArgs<ModConfig> e)
    {
        this.data = null;
        this.helper.GameContent.InvalidateCache(ModConstants.BigCraftableData);
    }

    private Dictionary<string, StorageOptions> GetData()
    {
        this.data ??= new Dictionary<string, StorageOptions>(StringComparer.OrdinalIgnoreCase);
        foreach (var (itemId, bigCraftableData) in Game1.bigCraftableData)
        {
            if (bigCraftableData.CustomFields?.GetBool(ModConstants.ModEnabled) != true)
            {
                continue;
            }

            var customFields = new DictionaryModel(GetCustomFields(itemId));
            _ = this.data.TryAdd(itemId, new StorageOptions(customFields));
        }

        return this.data;
    }

    public record CachedContext(
        ItemGrabMenu Menu,
        InventoryMenu TopMenu,
        InventoryMenu BottomMenu,
        IInventory Inventory,
        StorageOptions StorageOptions);
}