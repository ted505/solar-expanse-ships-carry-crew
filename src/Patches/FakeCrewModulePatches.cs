using System;
using System.Collections.Generic;
using System.Linq;
using CrewCapacityMod.Data;
using Data.ScriptableObject;
using Extensions;
using Game;
using Game.Info;
using Game.ObjectInfoDataScripts;
using Game.UI;
using Game.UI.Windows.Elements.PlanMissionElements;
using Game.UI.Windows.Windows;
using HarmonyLib;
using Language;
using Manager;
using ScriptableObjectScripts;
using TMPro;
using UIPlanMissionElements;
using UnityEngine;

namespace CrewCapacityMod.Patches;

[HarmonyPatch]
internal static class FakeCrewModulePatches
{
    private static readonly Dictionary<int, PlannerContext> _contextsByObjectId = new();

    private readonly struct PlannerContext
    {
        public PlannerContext(ISpacecraftInfo spacecraft, Company company, int spacecraftCount)
        {
            Spacecraft = spacecraft;
            Company = company;
            SpacecraftCount = spacecraftCount;
        }

        public ISpacecraftInfo Spacecraft { get; }
        public Company Company { get; }
        public int SpacecraftCount { get; }
    }

    [HarmonyPatch(typeof(ResourcesList), "SetObjectInfo")]
    [HarmonyPostfix]
    private static void SetObjectInfoPostfix(ResourcesList __instance)
    {
        UpdateContext(__instance);
    }

    [HarmonyPatch(typeof(ResourcesList), "SetSpacecraft")]
    [HarmonyPostfix]
    private static void SetSpacecraftPostfix(ResourcesList __instance)
    {
        UpdateContext(__instance);
    }

    [HarmonyPatch(typeof(ResourcesList), "SetData")]
    [HarmonyPrefix]
    private static void SetDataPrefix(CargoAll _cargos, PMTabCargo _tabCargo)
    {
        try
        {
            EnsureCyclicalReturnCrewModule(_cargos, _tabCargo);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[CrewCapacityMod] Failed to seed fake crew module for cyclical return leg: {ex}");
        }
    }

    [HarmonyPatch(typeof(ResourcesList), "SetData")]
    [HarmonyPostfix]
    private static void SetDataPostfix(ResourcesList __instance)
    {
        UpdateContext(__instance);
    }

    [HarmonyPatch(typeof(ResourcesList), "ClearVariable")]
    [HarmonyPostfix]
    private static void ClearVariablePostfix(ResourcesList __instance)
    {
        try
        {
            var objectInfo = __instance?.ObjectInfo?.GetObjectInfo();
            if (objectInfo != null)
                _contextsByObjectId.Remove(objectInfo.id);
        }
        catch
        {
        }
    }

    [HarmonyPatch(typeof(ResourcesList), "CheckAddModuleDisable")]
    [HarmonyPostfix]
    private static void CheckAddModuleDisablePostfix(ResourcesList __instance)
    {
        try
        {
            if (__instance?.addSpecial == null)
                return;

            if (SerializedMonoBehaviourSingleton<UIManager>.Instance?.Current is not PlanCyclicalMissionWindow { CurrentStageWindow: PlanMissionWindow.EStageWindow.CargoB } planMissionWindow)
                return;

            var pmp = planMissionWindow.PMMissionParameter;
            var objectInfo = __instance.ObjectInfo?.GetObjectInfo();
            if (pmp?.SC == null || objectInfo == null || pmp.FlyCompany?.IsPlayer != true)
                return;

            if (!IsMissionCargoObject(objectInfo, pmp))
                return;

            int capacity = CrewCapacity.GetCapacity(pmp.SC, pmp.FlyCompany, Math.Max(1, pmp.SCCount));
            if (capacity > 0)
            {
                SeedMissionContexts(pmp);
                __instance.addSpecial.interactable = true;
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[CrewCapacityMod] Failed to enable fake crew module button for cyclical return leg: {ex}");
        }
    }

    [HarmonyPatch(typeof(ObjectInfo), nameof(ObjectInfo.GetAvailableModulesForCargo))]
    [HarmonyPostfix]
    private static void GetAvailableModulesForCargoPostfix(
        ObjectInfo __instance,
        Company instancePlayer,
        DropDownEnum dropDown,
        ref List<SpaceModule> __result)
    {
        if (__result == null)
            return;

        if (dropDown == null)
            return;

        try
        {
            var fake = GetAvailableFakeModule(__instance, instancePlayer, dropDown);
            if (fake != null && !__result.Contains(fake))
                __result.Insert(0, fake);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[CrewCapacityMod] GetAvailableModulesForCargoPostfix error: {ex}");
        }
    }

    [HarmonyPatch(typeof(ObjectInfo), nameof(ObjectInfo.GetCountAvailableModulesForCargo))]
    [HarmonyPostfix]
    private static void GetCountAvailableModulesForCargoPostfix(
        ObjectInfo __instance,
        Company instancePlayer,
        ref int __result)
    {
        try
        {
            var fake = GetAvailableFakeModule(__instance, instancePlayer, null);
            if (fake != null)
                __result += Math.Max(0, fake.CountSelectFromDropDown());
        }
        catch (Exception ex)
        {
            Debug.LogError($"[CrewCapacityMod] GetCountAvailableModulesForCargoPostfix error: {ex}");
        }
    }

    [HarmonyPatch(typeof(DropDownEnum), nameof(DropDownEnum.SetOptions))]
    [HarmonyPostfix]
    private static void DropDownSetOptionsPostfix(
        DropDownEnum __instance,
        IObjectInfo _objectInfo,
        bool onlyCrewCompartment)
    {
        if (__instance?.DropDownType != EDropDownType.moduleDataCargoSpaceCraft || __instance.dropDown == null)
            return;

        try
        {
            var objectInfo = _objectInfo?.GetObjectInfo();
            if (objectInfo == null)
                return;

            var fake = GetAvailableFakeModule(objectInfo, MonoBehaviourSingleton<GameManager>.Instance?.Player, __instance);
            if (fake == null)
                return;

            if (onlyCrewCompartment && fake.facilityDescriptor.specialAbilityFacilityNew != ESpecialAbilityFacilityNew.CrewTransport)
                return;

            var traverse = Traverse.Create(__instance);
            var availableModules = traverse.Field("availableModulesOnStartObject").GetValue<List<SpaceModule>>();
            if (availableModules == null)
            {
                availableModules = new List<SpaceModule>();
                traverse.Field("availableModulesOnStartObject").SetValue(availableModules);
            }

            if (availableModules.Contains(fake))
                return;

            availableModules.Insert(0, fake);

            var option = new TMP_Dropdown.OptionData
            {
                text = MonoBehaviourSingleton<ObjectInfoManager>.Instance.spriteTextStart.MyFormat(fake.facilityDescriptor.SpriteId) + LEManager.Get(fake.facilityDescriptor.ID)
            };
            __instance.dropDown.options.Insert(0, option);
            __instance.dropDown.value = 0;
            fake.SelectFromDropDown(__instance);
            __instance.dropDown.RefreshShownValue();
        }
        catch (Exception ex)
        {
            Debug.LogError($"[CrewCapacityMod] Failed to inject fake crew module into dropdown options: {ex}");
        }
    }

    private static void UpdateContext(ResourcesList resourcesList)
    {
        if (resourcesList == null)
            return;

        try
        {
            var objectInfo = resourcesList.ObjectInfo?.GetObjectInfo();
            var pmp = resourcesList.PMMissionParameter;
            var spacecraft = Traverse.Create(resourcesList).Field("spacecraft").GetValue<ISpacecraftInfo>();
            var company = pmp?.FlyCompany ?? spacecraft?.GetCompany() ?? MonoBehaviourSingleton<GameManager>.Instance?.Player;
            int spacecraftCount = Math.Max(1, pmp?.SCCount ?? 1);

            if (objectInfo == null || spacecraft == null || company?.IsPlayer != true)
                return;

            var context = new PlannerContext(spacecraft, company, spacecraftCount);
            SeedContext(objectInfo, context);
            SeedContext(objectInfo.parentObjectInfo, context);
            SeedContext(objectInfo.LowOrbitCustom?.GetObjectInfo(), context);

            if (pmp != null)
                SeedMissionContexts(pmp, context);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[CrewCapacityMod] Update fake crew module context error: {ex}");
        }
    }

    private static SpaceModule GetAvailableFakeModule(ObjectInfo objectInfo, Company company, DropDownEnum dropDown)
    {
        if (objectInfo == null)
            return null;

        if (!_contextsByObjectId.TryGetValue(objectInfo.id, out var context)
            && !TryGetContextFromCurrentMission(objectInfo, company, out context))
            return null;

        company ??= context.Company;
        if (company?.IsPlayer != true || company != context.Company)
            return null;

        int capacity = CrewCapacity.GetCapacity(context.Spacecraft, context.Company, context.SpacecraftCount);
        if (capacity <= 0)
            return null;

        var oid = objectInfo.GetObjectInfoData(context.Company);
        var fake = FakeCrewModuleProvider.GetOrCreate(oid, capacity);
        if (fake == null || fake.IsAllSelectedFromOtherDropDown(dropDown))
            return null;

        return fake;
    }

    private static bool TryGetContextFromCurrentMission(ObjectInfo objectInfo, Company company, out PlannerContext context)
    {
        context = default;

        try
        {
            if (SerializedMonoBehaviourSingleton<UIManager>.Instance?.Current is not PlanMissionWindow planMissionWindow)
                return false;

            var pmp = planMissionWindow.PMMissionParameter;
            if (pmp?.Start == null || pmp.SC == null)
                return false;

            if (!IsMissionCargoObject(objectInfo, pmp))
                return false;

            var missionCompany = pmp.FlyCompany ?? pmp.SC.GetCompany() ?? MonoBehaviourSingleton<GameManager>.Instance?.Player;
            if (missionCompany?.IsPlayer != true)
                return false;

            company ??= missionCompany;
            if (company != missionCompany)
                return false;

            context = new PlannerContext(pmp.SC, missionCompany, Math.Max(1, pmp.SCCount));
            SeedMissionContexts(pmp, context);
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[CrewCapacityMod] Failed to derive fake crew module context from mission window: {ex}");
            return false;
        }
    }

    private static bool IsMissionCargoObject(ObjectInfo objectInfo, PMMissionParameter pmp)
    {
        if (objectInfo == null || pmp == null)
            return false;

        return objectInfo == pmp.Start
               || objectInfo == pmp.StartHermesCase
               || objectInfo == pmp.Start?.parentObjectInfo
               || objectInfo == pmp.Target
               || objectInfo == pmp.Target?.parentObjectInfo
               || objectInfo == pmp.Target?.LowOrbitCustom?.GetObjectInfo();
    }

    private static void SeedMissionContexts(PMMissionParameter pmp)
    {
        if (pmp?.SC == null || pmp.FlyCompany == null)
            return;

        SeedMissionContexts(pmp, new PlannerContext(pmp.SC, pmp.FlyCompany, Math.Max(1, pmp.SCCount)));
    }

    private static void SeedMissionContexts(PMMissionParameter pmp, PlannerContext context)
    {
        SeedContext(pmp.Start, context);
        SeedContext(pmp.StartHermesCase, context);
        SeedContext(pmp.Start?.parentObjectInfo, context);
        SeedContext(pmp.Start?.LowOrbitCustom?.GetObjectInfo(), context);
        SeedContext(pmp.Target, context);
        SeedContext(pmp.Target?.parentObjectInfo, context);
        SeedContext(pmp.Target?.LowOrbitCustom?.GetObjectInfo(), context);
    }

    private static void SeedContext(ObjectInfo objectInfo, PlannerContext context)
    {
        if (objectInfo != null)
            _contextsByObjectId[objectInfo.id] = context;
    }

    private static void EnsureCyclicalReturnCrewModule(CargoAll returnCargo, PMTabCargo tabCargo)
    {
        if (tabCargo?.Stage != PlanMissionWindow.EStageWindow.CargoB)
            return;

        if (tabCargo.PlanMissionWindow is not PlanCyclicalMissionWindow { CycleMissionsDataData: not null } planCyclicalMissionWindow)
            return;

        var pmp = planCyclicalMissionWindow.PMMissionParameter;
        if (pmp?.SC == null || pmp.FlyCompany?.IsPlayer != true)
            return;

        int capacity = CrewCapacity.GetCapacity(pmp.SC, pmp.FlyCompany, Math.Max(1, pmp.SCCount));
        if (capacity <= 0)
            return;

        var cycleData = planCyclicalMissionWindow.CycleMissionsDataData;
        var startCargo = cycleData.CargoAllStart;
        if (startCargo?.listCargo == null)
            return;

        RemoveBlankFakeReturnRows(returnCargo);
        SeedMissionContexts(pmp);

        var existing = startCargo.listCargo.FirstOrDefault(IsFakeCrewCargo);
        var source = GetFakeCrewModuleSource(pmp, cycleData);
        var fake = FakeCrewModuleProvider.GetOrCreate(source, capacity);
        if (fake == null)
            return;

        if (existing != null)
        {
            existing.SourceModule = fake;
            existing.moduleData = fake.facilityDescriptor as SpaceModuleDescriptor;
            existing.objectInfo ??= cycleData.A ?? pmp.StartHermesIObjectInfo ?? pmp.Start;
            existing.crewValue = Math.Min(existing.crewValue > 0 ? existing.crewValue : capacity, capacity);
            existing.crew = existing.crewValue > 0;
            return;
        }

        startCargo.listCargo.Insert(0, new Cargo(startCargo)
        {
            resourceTypeType = EResourceTypeType.modules,
            SourceModule = fake,
            moduleData = fake.facilityDescriptor as SpaceModuleDescriptor,
            objectInfo = cycleData.A ?? pmp.StartHermesIObjectInfo ?? pmp.Start,
            crew = true,
            crewValue = capacity
        });
    }

    private static ObjectInfoData GetFakeCrewModuleSource(PMMissionParameter pmp, CycleMissionsDataData cycleData)
    {
        var sourceObject = cycleData?.A ?? pmp?.StartHermesIObjectInfo?.GetObjectInfo() ?? pmp?.Start;
        return sourceObject?.GetObjectInfoData(pmp.FlyCompany);
    }

    private static void RemoveBlankFakeReturnRows(CargoAll returnCargo)
    {
        if (returnCargo?.listCargo == null)
            return;

        returnCargo.listCargo.RemoveAll(cargo =>
            cargo != null
            && cargo.resourceTypeType == EResourceTypeType.modules
            && cargo.SourceModule == null
            && (cargo.moduleData == null || FakeCrewModuleProvider.IsFakeCrewDescriptor(cargo.moduleData)));
    }

    private static bool IsFakeCrewCargo(Cargo cargo)
    {
        return cargo != null
               && cargo.resourceTypeType == EResourceTypeType.modules
               && (FakeCrewModuleProvider.IsFakeCrewModule(cargo.SourceModule)
                   || FakeCrewModuleProvider.IsFakeCrewDescriptor(cargo.moduleData));
    }
}
