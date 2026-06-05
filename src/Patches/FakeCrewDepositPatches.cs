using System;
using System.Linq;
using CrewCapacityMod.Data;
using Data.ScriptableObject;
using Game.ObjectInfoDataScripts;
using HarmonyLib;
using UnityEngine;

namespace CrewCapacityMod.Patches;

[HarmonyPatch]
internal static class FakeCrewDepositPatches
{
    [HarmonyPatch(typeof(ObjectInfoData), nameof(ObjectInfoData.AddResourcesAndModules))]
    [HarmonyPostfix]
    private static void AddResourcesAndModulesPostfix(ObjectInfoData __instance)
    {
        PurgeFakeCrewModules(__instance);
    }

    private static void PurgeFakeCrewModules(ObjectInfoData objectInfoData)
    {
        if (objectInfoData?.ListFacility == null)
            return;

        try
        {
            foreach (var facility in objectInfoData.ListFacility.ToList())
            {
                if (facility != null && FakeCrewModuleProvider.IsFakeCrewDescriptor(facility.facilityDescriptor))
                    objectInfoData.RemoveProductionItem(facility);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[CrewCapacityMod] Failed to purge fake crew module from destination: {ex}");
        }
    }
}
