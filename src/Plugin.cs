using BepInEx;
using CrewCapacityMod.Data;
using HarmonyLib;
using System.IO;
using UnityEngine;

namespace CrewCapacityMod;

[BepInPlugin("com.crewcapacitymod", "Crew Capacity Mod", "0.1.0")]
public class Plugin : BaseUnityPlugin
{
    private void Awake()
    {
        CrewCapacity.Initialize(Path.Combine(Paths.PluginPath, "ships-carry-crew", "crew_capacity.yaml"));
        Harmony.CreateAndPatchAll(typeof(Plugin).Assembly, "com.crewcapacitymod");
        Debug.Log("[CrewCapacityMod] Plugin loaded!");
    }
}
