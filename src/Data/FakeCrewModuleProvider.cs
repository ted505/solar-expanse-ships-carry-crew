using System.Collections.Generic;
using Data.ScriptableObject;
using Game.ObjectInfoDataScripts;
using HarmonyLib;
using Manager;
using UnityEngine;

namespace CrewCapacityMod.Data;

internal static class FakeCrewModuleProvider
{
    public const string FakeCrewModuleId = "Crew Module";
    private const string StockCrewModuleId = "module_crew_compartment";
    private static readonly Dictionary<string, FakeCrewSpaceModule> _modules = new();
    private static readonly HashSet<SpaceModule> _fakeCrewModules = new();
    private static readonly HashSet<SpaceModuleDescriptor> _fakeCrewDescriptors = new();

    public static bool IsFakeCrewModule(SpaceModule module)
    {
        return module != null && _fakeCrewModules.Contains(module);
    }

    public static bool IsFakeCrewDescriptor(FacilityBaseDescriptor descriptor)
    {
        return descriptor is SpaceModuleDescriptor spaceModuleDescriptor
               && (_fakeCrewDescriptors.Contains(spaceModuleDescriptor) || spaceModuleDescriptor.ID == FakeCrewModuleId);
    }

    public static SpaceModule GetOrCreate(ObjectInfoData objectInfoData, int capacity)
    {
        if (objectInfoData == null || capacity <= 0)
            return null;

        string key = $"{objectInfoData.id}:{capacity}";
        if (_modules.TryGetValue(key, out var existing) && existing != null)
        {
            existing.facilityDescriptor.specialAbilityParameter = capacity;
            return existing;
        }

        var descriptor = CreateDescriptor(capacity);
        var module = new FakeCrewSpaceModule(descriptor, objectInfoData, -9001)
        {
            BuildProgress = 1f
        };

        _modules[key] = module;
        _fakeCrewModules.Add(module);
        _fakeCrewDescriptors.Add(descriptor);
        return module;
    }

    private static SpaceModuleDescriptor CreateDescriptor(int capacity)
    {
        var descriptor = ScriptableObject.CreateInstance<SpaceModuleDescriptor>();
        descriptor.ID = FakeCrewModuleId;
        descriptor.facilityType = FacilityBaseDescriptor.EFacilityType.Module;
        descriptor.specialAbilityFacilityNew = ESpecialAbilityFacilityNew.CrewTransport;
        descriptor.specialAbilityParameter = capacity;

        var trav = Traverse.Create(descriptor);
        trav.Field("canBeLoadAsCargo").SetValue(true);
        trav.Field("mass").SetValue(0f);

        var stock = SerializedMonoBehaviourSingleton<AllScriptableObjectManager>.Instance?.AllFacility?.GetByID(StockCrewModuleId) as SpaceModuleDescriptor;
        if (stock != null)
            trav.Field("sprite").SetValue(stock.Sprite);

        return descriptor;
    }
}
