using System;
using System.Linq;
using CrewCapacityMod.Data;
using CustomUpdate;
using Game.Info;
using Game.ObjectInfoDataScripts;
using Game.UI.Windows.Elements.PlanMissionElements;
using HarmonyLib;
using UnityEngine;

namespace CrewCapacityMod.Patches;

[HarmonyPatch]
internal static class FakeCrewCyclicalMissionPatches
{
    [HarmonyPatch(typeof(InfoCargoCyclicalMission), nameof(InfoCargoCyclicalMission.ReleaseSpaceModule))]
    [HarmonyPrefix]
    private static bool ReleaseSpaceModulePrefix(InfoCargoCyclicalMission __instance, ObjectInfoData oiData)
    {
        try
        {
            foreach (var descriptor in __instance.ListSM.ToList())
            {
                if (!FakeCrewModuleProvider.IsFakeCrewDescriptor(descriptor))
                    oiData.AddFacility(descriptor, prebuilt: true);
            }

            __instance.ListSM.Clear();
            return false;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[CrewCapacityMod] Failed to release cyclical mission modules safely: {ex}");
            return true;
        }
    }

    [HarmonyPatch(typeof(ObjectInfoData), nameof(ObjectInfoData.CreatedCargoToTakeNormal))]
    [HarmonyPostfix]
    private static void CreatedCargoToTakeNormalPostfix(
        ObjectInfoData __instance,
        CycleMissionsData cycleMissionsData,
        ObjectInfo startObject,
        Spacecraft sc,
        int countSC,
        CargoAll __result)
    {
        if (__result?.listCargo == null || cycleMissionsData == null || sc == null)
            return;

        try
        {
            bool outboundLeg = startObject == cycleMissionsData.A;
            double remainingCrew = outboundLeg ? __instance.CrewResource.Value : 0.0;

            foreach (var cargo in __result.listCargo.Concat(__result.listCargoToOrbit ?? Enumerable.Empty<Cargo>()))
            {
                if (cargo?.moduleData == null || !FakeCrewModuleProvider.IsFakeCrewDescriptor(cargo.moduleData))
                    continue;

                int capacity = Math.Max(0, CrewCapacity.GetCapacity(sc, sc.GetCompany(), countSC));
                cargo.moduleData.specialAbilityParameter = capacity;
                cargo.SourceModule = null;

                if (outboundLeg && capacity > 0 && remainingCrew > 0.0)
                {
                    cargo.crewValue = Math.Min(capacity, (int)Math.Floor(remainingCrew));
                    cargo.crew = cargo.crewValue > 0;
                    remainingCrew -= cargo.crewValue;
                }
                else
                {
                    cargo.crew = false;
                    cargo.crewValue = 0;
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[CrewCapacityMod] Failed to normalize fake crew cyclical cargo: {ex}");
        }
    }
}
