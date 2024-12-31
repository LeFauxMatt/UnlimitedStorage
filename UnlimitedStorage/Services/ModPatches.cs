using System.Globalization;
using System.Reflection.Emit;
using HarmonyLib;
using LeFauxMods.Common.Utilities;
using LeFauxMods.UnlimitedStorage.Utilities;
using Microsoft.Xna.Framework.Graphics;
using StardewValley.Menus;
using StardewValley.Objects;

namespace LeFauxMods.UnlimitedStorage.Services;

/// <summary>Encapsulates mod patches.</summary>
internal static class ModPatches
{
    private static readonly string EmptySlot = int.MaxValue.ToString(CultureInfo.InvariantCulture);
    private static readonly Harmony Harmony = new(Constants.ModId);

    public static void Apply()
    {
        try
        {
            Log.Info("Applying Patches");

            _ = Harmony.Patch(
                AccessTools.DeclaredMethod(typeof(Chest), nameof(Chest.GetActualCapacity)),
                postfix: new HarmonyMethod(typeof(ModPatches), nameof(Chest_GetActualCapacity_postfix)));

            _ = Harmony.Patch(
                AccessTools.DeclaredPropertyGetter(typeof(Chest), nameof(Chest.SpecialChestType)),
                postfix: new HarmonyMethod(typeof(ModPatches), nameof(Chest_SpecialChestType_postfix)));

            _ = Harmony.Patch(
                AccessTools.DeclaredMethod(typeof(InventoryMenu), nameof(InventoryMenu.draw),
                    [typeof(SpriteBatch), typeof(int), typeof(int), typeof(int)]),
                new HarmonyMethod(typeof(ModPatches), nameof(InventoryMenu_draw_prefix)));

            _ = Harmony.Patch(
                AccessTools.DeclaredMethod(typeof(InventoryMenu), nameof(InventoryMenu.draw),
                    [typeof(SpriteBatch), typeof(int), typeof(int), typeof(int)]),
                postfix: new HarmonyMethod(typeof(ModPatches), nameof(InventoryMenu_draw_postfix)));

            _ = Harmony.Patch(
                AccessTools.GetDeclaredConstructors(typeof(ItemGrabMenu))
                    .Single(info => info.GetParameters().Length > 5),
                transpiler: new HarmonyMethod(typeof(ModPatches), nameof(ItemGrabMenu_constructor_transpiler)));
        }
        catch
        {
            Log.Warn("Failed to apply patches");
        }
    }


    [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Harmony")]
    private static void Chest_GetActualCapacity_postfix(Chest __instance, ref int __result)
    {
        if (__instance.IsEnabled())
        {
            __result = Math.Max(__result, __instance.GetItemsForPlayer().Count + 1);
        }
    }

    [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Harmony")]
    private static void Chest_SpecialChestType_postfix(Chest __instance, ref Chest.SpecialChestTypes __result)
    {
        if (StateManager.Config.BigChestMenu &&
            __result is Chest.SpecialChestTypes.None &&
            __instance.IsEnabled())
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

    [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Harmony")]
    private static void InventoryMenu_draw_postfix(InventoryMenu __instance, ref bool __state)
    {
        if (__state && StateManager.TryGetMenu(out _, out var inventoryMenu, out var chest) &&
            ReferenceEquals(__instance, inventoryMenu))
        {
            __instance.actualInventory = chest.GetItemsForPlayer();
        }
    }

    [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Harmony")]
    private static void InventoryMenu_draw_prefix(InventoryMenu __instance, ref bool __state)
    {
        if (StateManager.Columns == 0 || !StateManager.TryGetMenu(out _, out var inventoryMenu, out var chest) ||
            !ReferenceEquals(__instance, inventoryMenu))
        {
            return;
        }

        var maxOffset = __instance.GetMaxOffset(chest);
        if (maxOffset <= 0)
        {
            return;
        }

        StateManager.Offset = Math.Min(Math.Max(0, StateManager.Offset), maxOffset * StateManager.Columns);
        __state = true;
        var actualInventory = chest.GetItemsForPlayer();
        __instance.actualInventory = [.. actualInventory.Skip(StateManager.Offset).Take(__instance.capacity)];

        // Update name
        for (var i = 0; i < __instance.inventory.Count; i++)
        {
            if (i >= __instance.actualInventory.Count || __instance.actualInventory[i] is not { } item)
            {
                __instance.inventory[i].name = EmptySlot;
                continue;
            }

            __instance.inventory[i].name = actualInventory.IndexOf(item).ToString(CultureInfo.InvariantCulture);
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
                        CodeInstruction.Call(typeof(ModPatches), nameof(GetActualCapacity)))
            )
            .InstructionEnumeration();
}
