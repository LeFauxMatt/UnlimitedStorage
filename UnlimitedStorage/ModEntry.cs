using System.Globalization;
using System.Reflection.Emit;
using HarmonyLib;
using LeFauxMods.Common.Integrations.GenericModConfigMenu;
using LeFauxMods.Common.Models;
using LeFauxMods.Common.Services;
using LeFauxMods.Common.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley.GameData.BigCraftables;
using StardewValley.Menus;
using StardewValley.Objects;
using StardewValley.TokenizableStrings;

namespace LeFauxMods.UnlimitedStorage;

/// <inheritdoc />
internal sealed class ModEntry : Mod
{
    private static readonly PerScreen<int> Columns = new();

    private static readonly PerScreen<ClickableTextureComponent> DownArrow = new(() =>
        new ClickableTextureComponent(Rectangle.Empty, Game1.mouseCursors, new Rectangle(421, 472, 11, 12),
            Game1.pixelZoom)
        {
            myID = 69_420,
            upNeighborID = 69_421
        });

    private static readonly PerScreen<int> Offset = new();

    private static readonly PerScreen<ClickableTextureComponent> UpArrow = new(() =>
        new ClickableTextureComponent(Rectangle.Empty, Game1.mouseCursors, new Rectangle(421, 459, 11, 12),
            Game1.pixelZoom)
        {
            myID = 69_421,
            downNeighborID = 69_420
        });

    private static ModConfig config = null!;
    private ConfigHelper<ModConfig> configHelper = null!;

    /// <inheritdoc />
    public override void Entry(IModHelper helper)
    {
        // Init
        I18n.Init(this.Helper.Translation);
        this.configHelper = new ConfigHelper<ModConfig>(helper);
        config = this.configHelper.Load();
        Log.Init(this.Monitor, config);

        // Patches
        var harmony = new Harmony(this.ModManifest.UniqueID);

        _ = harmony.Patch(
            AccessTools.DeclaredMethod(typeof(Chest), nameof(Chest.GetActualCapacity)),
            postfix: new HarmonyMethod(typeof(ModEntry), nameof(Chest_GetActualCapacity_postfix)));

        _ = harmony.Patch(
            AccessTools.DeclaredPropertyGetter(typeof(Chest), nameof(Chest.SpecialChestType)),
            postfix: new HarmonyMethod(typeof(ModEntry), nameof(Chest_SpecialChestType_postfix)));

        _ = harmony.Patch(
            AccessTools.DeclaredMethod(typeof(InventoryMenu), nameof(InventoryMenu.draw),
                [typeof(SpriteBatch), typeof(int), typeof(int), typeof(int)]),
            new HarmonyMethod(typeof(ModEntry), nameof(InventoryMenu_draw_prefix)));

        _ = harmony.Patch(
            AccessTools.DeclaredMethod(typeof(InventoryMenu), nameof(InventoryMenu.draw),
                [typeof(SpriteBatch), typeof(int), typeof(int), typeof(int)]),
            postfix: new HarmonyMethod(typeof(ModEntry), nameof(InventoryMenu_draw_postfix)));

        _ = harmony.Patch(
            AccessTools.GetDeclaredConstructors(typeof(ItemGrabMenu)).Single(info => info.GetParameters().Length > 5),
            transpiler: new HarmonyMethod(typeof(ModEntry), nameof(ItemGrabMenu_constructor_transpiler)));

        // Events
        helper.Events.Content.AssetRequested += this.OnAssetRequested;
        helper.Events.Display.MenuChanged += OnMenuChanged;
        helper.Events.Display.RenderedActiveMenu += this.OnRenderedActiveMenu;
        helper.Events.GameLoop.GameLaunched += this.OnGameLaunched;
        helper.Events.Input.ButtonPressed += this.OnButtonPressed;
        helper.Events.Input.MouseWheelScrolled += this.OnMouseWheelScrolled;

        ModEvents.Subscribe<ConfigChangedEventArgs<ModConfig>>(this.OnConfigChanged);
    }

    private static void Chest_GetActualCapacity_postfix(Chest __instance, ref int __result)
    {
        if (!Game1.bigCraftableData.TryGetValue(__instance.ItemId, out var data) ||
            data.CustomFields?.GetBool(Constants.ModEnabled) != true)
        {
            return;
        }

        __result = Math.Max(__result, __instance.GetItemsForPlayer().Count + 1);
    }

    private static void Chest_SpecialChestType_postfix(Chest __instance, ref Chest.SpecialChestTypes __result)
    {
        if (config.BigChestMenu &&
            __result is Chest.SpecialChestTypes.None &&
            Game1.bigCraftableData.TryGetValue(__instance.ItemId, out var data) &&
            data.CustomFields?.GetBool(Constants.ModEnabled) == true)
        {
            __result = Chest.SpecialChestTypes.BigChest;
        }
    }

    private static int GetActualCapacity(int capacity, object? context) =>
        (context as Chest)?.SpecialChestType switch
        {
            Chest.SpecialChestTypes.MiniShippingBin or Chest.SpecialChestTypes.JunimoChest => 9,
            Chest.SpecialChestTypes.Enricher => 1,
            Chest.SpecialChestTypes.BigChest => 70,
            not null => 36,
            _ => capacity
        };

    private static void InventoryMenu_draw_postfix(InventoryMenu __instance, ref IList<Item>? __state)
    {
        if (Columns.Value == 0 || __state is null ||
            Game1.activeClickableMenu is not ItemGrabMenu { ItemsToGrabMenu: { } inventoryMenu } ||
            !ReferenceEquals(__instance, inventoryMenu))
        {
            return;
        }

        __instance.actualInventory = __state;
    }

    private static void InventoryMenu_draw_prefix(InventoryMenu __instance, ref IList<Item>? __state)
    {
        if (Columns.Value == 0 ||
            Game1.activeClickableMenu is not ItemGrabMenu { ItemsToGrabMenu: { } inventoryMenu } ||
            !ReferenceEquals(__instance, inventoryMenu))
        {
            return;
        }

        var maxRows = (int)Math.Ceiling((float)__instance.actualInventory.Count / Columns.Value) - __instance.rows;
        if (maxRows <= 0)
        {
            return;
        }

        Offset.Value = Math.Min(Math.Max(0, Offset.Value), maxRows * Columns.Value);

        __state = __instance.actualInventory;
        __instance.actualInventory = [.. __instance.actualInventory.Skip(Offset.Value).Take(__instance.capacity)];
        var emptySlot = int.MaxValue.ToString(CultureInfo.InvariantCulture);

        // Update name
        for (var i = 0; i < __instance.inventory.Count; i++)
        {
            if (i >= __instance.actualInventory.Count || __instance.actualInventory[i] is not { } item)
            {
                __instance.inventory[i].name = emptySlot;
                continue;
            }

            __instance.inventory[i].name = __state.IndexOf(item).ToString(CultureInfo.InvariantCulture);
        }
    }

    private static IEnumerable<CodeInstruction>
        ItemGrabMenu_constructor_transpiler(IEnumerable<CodeInstruction> instructions) =>
        new CodeMatcher(instructions)
            .MatchStartForward(
                new CodeMatch(
                    instruction => instruction.Calls(
                        AccessTools.DeclaredMethod(typeof(Chest), nameof(Chest.GetActualCapacity)))))
            .Repeat(matcher =>
                matcher
                    .Advance(1)
                    .InsertAndAdvance(
                        new CodeInstruction(OpCodes.Ldarg_S, (short)16),
                        CodeInstruction.Call(typeof(ModEntry), nameof(GetActualCapacity)))
            )
            .InstructionEnumeration();

    private static void OnMenuChanged(object? sender, MenuChangedEventArgs e)
    {
        if (e.NewMenu is not ItemGrabMenu
            {
                sourceItem: Chest chest, ItemsToGrabMenu: { } inventoryMenu
            } itemGrabMenu || !Game1.bigCraftableData.TryGetValue(chest.ItemId, out var data) ||
            data.CustomFields?.GetBool(Constants.ModEnabled) != true)
        {
            Offset.Value = 0;
            Columns.Value = 0;
            return;
        }

        if ((e.OldMenu as ItemGrabMenu)?.context == itemGrabMenu.context)
        {
            return;
        }

        Offset.Value = 0;
        Columns.Value = inventoryMenu.capacity / inventoryMenu.rows;

        // Align up arrow to top-right slot
        var topSlot = inventoryMenu.inventory[Columns.Value - 1];
        UpArrow.Value.rightNeighborID = topSlot.rightNeighborID;
        UpArrow.Value.leftNeighborID = topSlot.myID;
        topSlot.rightNeighborID = UpArrow.Value.myID;

        UpArrow.Value.bounds = new Rectangle(
            inventoryMenu.xPositionOnScreen + inventoryMenu.width + 8,
            inventoryMenu.inventory[Columns.Value - 1].bounds.Center.Y - (6 * Game1.pixelZoom),
            11 * Game1.pixelZoom,
            12 * Game1.pixelZoom);

        // Align down arrow to bottom-right slot
        var bottomSlot = inventoryMenu.inventory[inventoryMenu.capacity - 1];
        DownArrow.Value.rightNeighborID = bottomSlot.rightNeighborID;
        DownArrow.Value.leftNeighborID = bottomSlot.myID;
        bottomSlot.rightNeighborID = DownArrow.Value.myID;

        DownArrow.Value.bounds = new Rectangle(
            inventoryMenu.xPositionOnScreen + inventoryMenu.width + 8,
            inventoryMenu.inventory[inventoryMenu.capacity - 1].bounds.Center.Y - (6 * Game1.pixelZoom),
            11 * Game1.pixelZoom,
            12 * Game1.pixelZoom);
    }

    private void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
    {
        if (!e.NameWithoutLocale.IsEquivalentTo(Constants.BigCraftableData))
        {
            return;
        }


        // Add config options to the data
        e.Edit(asset =>
            {
                var allData = asset.AsDictionary<string, BigCraftableData>().Data;
                foreach (var id in config.EnabledIds)
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

    private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
    {
        if (Columns.Value == 0 ||
            Game1.activeClickableMenu is not ItemGrabMenu
            {
                ItemsToGrabMenu: { } inventoryMenu
            } itemGrabMenu)
        {
            return;
        }

        var maxRows = (int)Math.Ceiling((float)inventoryMenu.actualInventory.Count / Columns.Value) -
                      inventoryMenu.rows;
        if (maxRows <= 0)
        {
            return;
        }

        if (e.Button is SButton.DPadUp or SButton.LeftThumbstickUp && Offset.Value > 0)
        {
            if (itemGrabMenu.currentlySnappedComponent is not { } slot)
            {
                return;
            }

            var index = inventoryMenu.inventory.IndexOf(slot);
            if (index == -1 || index >= Columns.Value)
            {
                return;
            }

            Offset.Value -= Columns.Value;
            this.Helper.Input.Suppress(e.Button);
            return;
        }

        if (e.Button is SButton.DPadDown or SButton.LeftThumbstickDown && Offset.Value < maxRows * Columns.Value)
        {
            if (itemGrabMenu.currentlySnappedComponent is not { } slot)
            {
                return;
            }

            var index = inventoryMenu.inventory.IndexOf(slot);
            if (index == -1 || index < inventoryMenu.capacity - Columns.Value)
            {
                return;
            }

            Offset.Value += Columns.Value;
            this.Helper.Input.Suppress(e.Button);
            return;
        }

        if (e.Button is not (SButton.MouseLeft or SButton.ControllerA))
        {
            return;
        }

        var cursor = Utility.ModifyCoordinatesForUIScale(e.Cursor.GetScaledScreenPixels()).ToPoint();
        if (Offset.Value > 0 && UpArrow.Value.containsPoint(cursor.X, cursor.Y))
        {
            UpArrow.Value.scale = 3f;
            Offset.Value -= Columns.Value;
        }
        else if (Offset.Value < maxRows * Columns.Value && DownArrow.Value.containsPoint(cursor.X, cursor.Y))
        {
            DownArrow.Value.scale = 3f;
            Offset.Value += Columns.Value;
        }
        else
        {
            return;
        }

        this.Helper.Input.Suppress(e.Button);
        Game1.playSound("drumkit6");
    }

    private void OnConfigChanged(ConfigChangedEventArgs<ModConfig> e) =>
        this.Helper.GameContent.InvalidateCache(Constants.BigCraftableData);

    private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
    {
        var gmcm = new GenericModConfigMenuIntegration(this.ModManifest, this.Helper.ModRegistry);
        if (!gmcm.IsLoaded)
        {
            return;
        }

        var defaultConfig = new ModConfig();
        var tempConfig = this.configHelper.Load();

        gmcm.Register(
            () => defaultConfig.CopyTo(tempConfig),
            () =>
            {
                tempConfig.CopyTo(config);
                this.configHelper.Save(config);
            });

        gmcm.Api.AddBoolOption(
            this.ModManifest,
            () => tempConfig.BigChestMenu,
            value => tempConfig.BigChestMenu = value,
            I18n.ConfigOption_BigChestsMenu_Name,
            I18n.ConfigOption_BigChestsMenu_Description);

        gmcm.Api.AddSectionTitle(this.ModManifest, I18n.ConfigOption_MakeUnlimited_Name);
        gmcm.Api.AddParagraph(this.ModManifest, I18n.ConfigOption_MakeUnlimited_Description);

        foreach (var id in defaultConfig.EnabledIds)
        {
            if (!Game1.bigCraftableData.TryGetValue(id, out var data))
            {
                continue;
            }

            gmcm.Api.AddBoolOption(
                this.ModManifest,
                () => tempConfig.EnabledIds.Contains(id),
                value =>
                {
                    if (value)
                    {
                        tempConfig.EnabledIds.Add(id);
                    }
                    else
                    {
                        tempConfig.EnabledIds.Remove(id);
                    }
                },
                () => TokenParser.ParseText(data.DisplayName),
                () => TokenParser.ParseText(data.Description));
        }
    }

    private void OnMouseWheelScrolled(object? sender, MouseWheelScrolledEventArgs e)
    {
        if (Columns.Value == 0 || Game1.activeClickableMenu is not ItemGrabMenu { ItemsToGrabMenu: { } inventoryMenu })
        {
            return;
        }

        var cursor = Utility.ModifyCoordinatesForUIScale(this.Helper.Input.GetCursorPosition().GetScaledScreenPixels())
            .ToPoint();
        if (!inventoryMenu.isWithinBounds(cursor.X, cursor.Y))
        {
            return;
        }

        Offset.Value += e.Delta > 0 ? -Columns.Value : Columns.Value;
    }

    private void OnRenderedActiveMenu(object? sender, RenderedActiveMenuEventArgs e)
    {
        if (Columns.Value == 0 || Game1.activeClickableMenu is not ItemGrabMenu
            {
                ItemsToGrabMenu: { } inventoryMenu
            } itemGrabMenu)
        {
            return;
        }

        var maxRows = (int)Math.Ceiling((float)inventoryMenu.actualInventory.Count / Columns.Value) -
                      inventoryMenu.rows;
        if (maxRows <= 0)
        {
            return;
        }

        var cursor = Utility.ModifyCoordinatesForUIScale(this.Helper.Input.GetCursorPosition().GetScaledScreenPixels())
            .ToPoint();

        if (Offset.Value > 0)
        {
            UpArrow.Value.tryHover(cursor.X, cursor.Y);
            UpArrow.Value.draw(e.SpriteBatch);
        }

        if (Offset.Value < maxRows * Columns.Value)
        {
            DownArrow.Value.tryHover(cursor.X, cursor.Y);
            DownArrow.Value.draw(e.SpriteBatch);
        }

        itemGrabMenu.drawMouse(e.SpriteBatch);
    }
}
