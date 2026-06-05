using System.Collections.Generic;
using CrewCapacityMod.Data;
using CustomUpdate;
using Data.ScriptableObject;
using Game;
using HarmonyLib;

namespace CrewCapacityMod.Patches;

[HarmonyPatch]
internal static class SpacecraftTooltipPatches
{
    [HarmonyPatch(typeof(SpacecraftType), nameof(SpacecraftType.GetTooltipStats))]
    [HarmonyPostfix]
    private static void GetTooltipStatsPostfix(
        SpacecraftType __instance,
        Company company,
        ref List<(string, string)> __result)
    {
        if (__result == null)
            return;

        int capacity = CrewCapacity.GetCapacityPerSpacecraft(__instance);
        if (capacity <= 0)
            return;

        __result.Add(("Crew Capacity", capacity.ToString("N0")));
    }
}
