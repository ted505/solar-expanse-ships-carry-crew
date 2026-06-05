using System;
using System.Collections.Generic;
using CrewCapacityMod.Data;
using Game.ObjectInfoDataScripts;
using Game.UI.Windows.Elements.PlanMissionElements;
using HarmonyLib;
using UnityEngine;

namespace CrewCapacityMod.Patches;

[HarmonyPatch(typeof(PMMissionParameter), "CheckCargo")]
internal static class FakeCrewCargoValidationPatches
{
    private static void Prefix(PMMissionParameter __instance, ref List<SpaceModule> __state)
    {
        __state = null;

        try
        {
            var cargoAll = __instance?.CargoAll;
            if (cargoAll?.listCargo == null)
                return;

            foreach (var cargo in cargoAll.listCargo)
            {
                var module = cargo?.SourceModule;
                var objectInfoData = module?.ObjectInfoData;
                if (!FakeCrewModuleProvider.IsFakeCrewModule(module) || objectInfoData?.ProductionItem == null)
                    continue;

                if (objectInfoData.ProductionItem.Contains(module))
                    continue;

                objectInfoData.ProductionItem.Add(module);
                (__state ??= new List<SpaceModule>()).Add(module);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[CrewCapacityMod] Failed to prepare fake crew cargo validation: {ex}");
        }
    }

    private static void Postfix(List<SpaceModule> __state)
    {
        if (__state == null)
            return;

        foreach (var module in __state)
        {
            try
            {
                module?.ObjectInfoData?.ProductionItem?.Remove(module);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CrewCapacityMod] Failed to clean up fake crew cargo validation shim: {ex}");
            }
        }
    }
}
