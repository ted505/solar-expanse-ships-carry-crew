using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using CustomUpdate;
using Data.ScriptableObject;
using Game;
using Game.Info;
using UnityEngine;

namespace CrewCapacityMod.Data;

internal static class CrewCapacity
{
    private static readonly Dictionary<string, int> _configuredCapacities = new(StringComparer.OrdinalIgnoreCase);
    private static string _yamlPath;
    private static DateTime _lastWriteTimeUtc = DateTime.MinValue;
    private static bool _loaded;

    public static void Initialize(string yamlPath)
    {
        _yamlPath = yamlPath;
        EnsureYamlExists();
        ReloadIfNeeded(force: true);
    }

    public static int GetCapacityPerSpacecraft(SpacecraftType type)
    {
        if (type == null)
            return 0;

        ReloadIfNeeded();
        return _configuredCapacities.TryGetValue(type.ID, out int capacity) ? Math.Max(0, capacity) : 0;
    }

    public static int GetCapacity(ISpacecraftInfo spacecraft, Company company, int spacecraftCount)
    {
        var type = spacecraft?.GetTypeSpaceCraft();
        if (type == null || company == null)
            return 0;

        if (type.GetMAXLifeSupport(company) <= 0f)
            return 0;

        int perSpacecraft = GetCapacityPerSpacecraft(type);
        return Math.Max(0, perSpacecraft * Math.Max(1, spacecraftCount));
    }

    private static void ReloadIfNeeded(bool force = false)
    {
        EnsureYamlExists();

        if (string.IsNullOrEmpty(_yamlPath) || !File.Exists(_yamlPath))
            return;

        DateTime writeTime = File.GetLastWriteTimeUtc(_yamlPath);
        if (!force && _loaded && writeTime == _lastWriteTimeUtc)
            return;

        _lastWriteTimeUtc = writeTime;
        _loaded = true;
        _configuredCapacities.Clear();

        foreach (string rawLine in File.ReadAllLines(_yamlPath))
        {
            string line = StripComment(rawLine).Trim();
            if (line.Length == 0 || line == "crewCapacity:" || line == "spacecraft:")
                continue;

            string[] parts = line.Split(new[] { ':' }, 2);
            if (parts.Length != 2)
                continue;

            string id = parts[0].Trim().Trim('"', '\'');
            string value = parts[1].Trim().Trim('"', '\'');
            if (id.Length == 0 || value.Length == 0)
                continue;

            if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int capacity))
                _configuredCapacities[id] = Math.Max(0, capacity);
            else
                Debug.LogWarning($"[CrewCapacityMod] Ignoring invalid crew capacity entry: {rawLine}");
        }

        Debug.Log($"[CrewCapacityMod] Loaded {_configuredCapacities.Count} crew capacity entries from {_yamlPath}");
    }

    private static void EnsureYamlExists()
    {
        if (string.IsNullOrEmpty(_yamlPath) || File.Exists(_yamlPath))
            return;

        string directory = Path.GetDirectoryName(_yamlPath);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        File.WriteAllText(_yamlPath,
            "# Crew capacity per spacecraft. Capacity is multiplied by the selected SC count.\n" +
            "# Spacecraft IDs come from Teddit's spacecraft.yaml dump.\n" +
            "crewCapacity:\n" +
            "  spacecraft_capsule: 5\n" +
            "  spacecraft_chem_large: 30\n");
    }

    private static string StripComment(string line)
    {
        int index = line.IndexOf('#');
        return index >= 0 ? line.Substring(0, index) : line;
    }
}
