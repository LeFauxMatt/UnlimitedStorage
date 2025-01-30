using System.Globalization;
using System.Reflection.Emit;
using HarmonyLib;
using LeFauxMods.Common.Utilities;
using LeFauxMods.UnlimitedStorage.Utilities;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI.Utilities;
using StardewValley.Inventories;
using StardewValley.Menus;
using StardewValley.Objects;

namespace LeFauxMods.UnlimitedStorage.Services;

/// <summary>Encapsulates mod patches.</summary>
internal static class ModPatches
{
    private static readonly string EmptySlot = int.MaxValue.ToString(CultureInfo.InvariantCulture);
    private static readonly Harmony Harmony = new(Constants.ModId);

    private static readonly PerScreen<InventoryMenu.highlightThisItem?> HighlightMethod = new();

    public static void Apply()
    {
        Log.Info("Applying Patches");

        try
        {
            _ = Harmony.Patch(
                AccessTools.DeclaredMethod(typeof(Chest), nameof(Chest.GetActualCapacity)),
                postfix: new HarmonyMethod(typeof(ModPatches), nameof(Chest_GetActualCapacity_postfix)));

            _ = Harmony.Patch(
                AccessTools.DeclaredMethod(typeof(InventoryMenu), nameof(InventoryMenu.draw),
                    [typeof(SpriteBatch), typeof(int), typeof(int), typeof(int)]),
                new HarmonyMethod(typeof(ModPatches), nameof(TryAdjustInventory)),
                new HarmonyMethod(typeof(ModPatches), nameof(TryRevertInventory)),
                new HarmonyMethod(typeof(ModPatches), nameof(InventoryMenu_draw_transpiler)));

            _ = Harmony.Patch(
                AccessTools.DeclaredMethod(typeof(InventoryMenu), nameof(InventoryMenu.GetBorder)),
                new HarmonyMethod(typeof(ModPatches), nameof(TryAdjustInventory)),
                new HarmonyMethod(typeof(ModPatches), nameof(TryRevertInventory)));
        }
        catch
        {
            Log.Warn("Failed to apply patches");
        }

        _ = Harmony.Patch(
            AccessTools.GetDeclaredConstructors(typeof(ItemGrabMenu))
                .Single(static info => info.GetParameters().Length > 5),
            new HarmonyMethod(typeof(ModPatches), nameof(ItemGrabMenu_constructor_prefix)),
            transpiler: new HarmonyMethod(typeof(ModPatches), nameof(ItemGrabMenu_constructor_transpiler)));
    }


    [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Harmony")]
    [SuppressMessage("ReSharper", "SuggestBaseTypeForParameter", Justification = "Harmony")]
    private static void Chest_GetActualCapacity_postfix(Chest __instance, ref int __result)
    {
        if (Game1.bigCraftableData.TryGetValue(__instance.ItemId, out var data) &&
            data.CustomFields?.GetBool(Constants.ModEnabled) == true)
        {
            __result = int.MaxValue;
        }
    }

    private static int GetActualCapacity(int capacity, Item? sourceItem) =>
        sourceItem switch
        {
            not null when ModState.Config.BigChestMenu => 70,
            Chest
            {
                SpecialChestType: Chest.SpecialChestTypes.MiniShippingBin or Chest.SpecialChestTypes.JunimoChest
            } => 9,
            Chest { SpecialChestType: Chest.SpecialChestTypes.Enricher } => 1,
            Chest { SpecialChestType: Chest.SpecialChestTypes.BigChest } => 70,
            _ => capacity
        };

    private static IEnumerable<CodeInstruction>
        InventoryMenu_draw_transpiler(IEnumerable<CodeInstruction> instructions) =>
        new CodeMatcher(instructions)
            .MatchEndForward(new CodeMatch(CodeInstruction.LoadField(typeof(InventoryMenu),
                nameof(InventoryMenu.highlightMethod))))
            .Repeat(static matcher =>
                matcher
                    .Advance(1)
                    .InsertAndAdvance(
                        new CodeInstruction(OpCodes.Ldarg_0),
                        CodeInstruction.Call(typeof(ModPatches), nameof(GetHighlightMethod))))
            .InstructionEnumeration();

    private static InventoryMenu.highlightThisItem GetHighlightMethod(InventoryMenu.highlightThisItem highlightMethod,
        InventoryMenu instance)
    {
        if (ModState.Columns == 0 ||
            !ModState.Config.EnableSearch ||
            string.IsNullOrWhiteSpace(ModState.TextBox.Text) ||
            !ModState.TryGetMenu(out _, out var inventoryMenu, out _) ||
            !ReferenceEquals(instance, inventoryMenu))
        {
            return highlightMethod;
        }

        HighlightMethod.Value = highlightMethod;
        return HighlightItem;
    }

    private static bool HighlightItem(Item item) =>
        HighlightMethod.Value?.Invoke(item) != false && (
            item.DisplayName.Contains(ModState.TextBox.Text, StringComparison.OrdinalIgnoreCase) ||
            item.getDescription()
                .Contains(ModState.TextBox.Text, StringComparison.OrdinalIgnoreCase) ||
            item.GetContextTags().Any(static tag =>
                tag.Contains(ModState.TextBox.Text, StringComparison.OrdinalIgnoreCase)));

    [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Harmony")]
    [SuppressMessage("ReSharper", "RedundantAssignment", Justification = "Harmony")]
    private static void TryAdjustInventory(InventoryMenu __instance, ref IInventory? __state)
    {
        if (ModState.Columns == 0 ||
            !ModState.TryGetMenu(out _, out var inventoryMenu, out var chest) ||
            !ReferenceEquals(__instance, inventoryMenu))
        {
            return;
        }

        var maxOffset = __instance.GetMaxOffset(chest);
        __state = chest.GetItemsForPlayer();

        var adjustedInventory = __state.AsEnumerable();
        if (ModState.Config.EnableSearch && !string.IsNullOrWhiteSpace(ModState.TextBox.Text))
        {
            adjustedInventory = adjustedInventory.OrderBySearch();
        }

        ModState.Offset = Math.Min(Math.Max(0, ModState.Offset), maxOffset * ModState.Columns);
        if (maxOffset > 0)
        {
            adjustedInventory = adjustedInventory.Skip(ModState.Offset).Take(__instance.capacity);
        }

        __instance.actualInventory = [..adjustedInventory];

        // Update name
        for (var i = 0; i < __instance.inventory.Count; i++)
        {
            if (i >= __instance.actualInventory.Count || __instance.actualInventory[i] is not { } item)
            {
                __instance.inventory[i].name = EmptySlot;
                continue;
            }

            __instance.inventory[i].name = __state.IndexOf(item).ToString(CultureInfo.InvariantCulture);
        }
    }


    [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Harmony")]
    private static void TryRevertInventory(InventoryMenu __instance, ref IInventory? __state)
    {
        if (__state is not null)
        {
            __instance.actualInventory = __state;
        }
    }

    private static void ItemGrabMenu_constructor_prefix(ref Item? sourceItem, object context)
    {
        if (context is SObject { QualifiedItemId: "(BC)165" } item)
        {
            sourceItem = item;
        }
    }

    private static IEnumerable<CodeInstruction>
        ItemGrabMenu_constructor_transpiler(IEnumerable<CodeInstruction> instructions) =>
        new CodeMatcher(instructions)
            .MatchEndForward(
                new CodeMatch(OpCodes.Isinst, typeof(Chest)),
                new CodeMatch(OpCodes.Stloc_1))
            .InsertAndAdvance(
                new CodeInstruction(OpCodes.Ldarg_S, (short)14),
                CodeInstruction.Call(typeof(ModPatches), nameof(GetAutoGrabberChest)))
            .MatchStartForward(
                new CodeMatch(
                    static instruction => instruction.Calls(
                        AccessTools.DeclaredMethod(typeof(Chest), nameof(Chest.GetActualCapacity)))))
            .Repeat(static matcher =>
                matcher
                    .Advance(1)
                    .InsertAndAdvance(
                        new CodeInstruction(OpCodes.Ldarg_S, (short)14),
                        CodeInstruction.Call(typeof(ModPatches), nameof(GetActualCapacity)))
            )
            .InstructionEnumeration();

    private static Chest? GetAutoGrabberChest(Chest? result, Item? sourceItem) =>
        result ?? (sourceItem is SObject { QualifiedItemId: "(BC)165", heldObject.Value: Chest heldChest }
            ? heldChest
            : null);
}