using System;
using System.Collections.Generic;
using System.IO;
using Unity.Mathematics;
using UnityEngine;

public readonly struct ObjectData
{
    public readonly string ObjectName;
    public readonly List<EphemerisEntry> EphemerisData;
    public readonly List<PhysicalTraitsEntry> PhysicalTraitsData;

    public ObjectData(string objectName,
                      List<EphemerisEntry> ephemerisData,
                      List<PhysicalTraitsEntry> physicalTraitsData)
    {
        ObjectName = objectName;
        EphemerisData = ephemerisData ?? new();
        PhysicalTraitsData = physicalTraitsData ?? new();
    }
}

[Serializable]
public class EphemerisEntryJSON
{
    public string ObjectName;
    public long DateTimeTicks;
    public double PosX, PosY, PosZ;
    public double VelX, VelY, VelZ;
    public bool HasPosition;
    public bool HasVelocity;

    public string DedupKey => $"{DateTimeTicks}|{HasPosition}|{HasVelocity}";

    public static EphemerisEntryJSON FromEntry(EphemerisEntry e) => new()
    {
        ObjectName = e.ObjectName,
        DateTimeTicks = e.DateTime.UtcTicks,
        PosX = e.Position.x,
        PosY = e.Position.y,
        PosZ = e.Position.z,
        VelX = e.Velocity.x,
        VelY = e.Velocity.y,
        VelZ = e.Velocity.z,
        HasPosition = e.HasPosition,
        HasVelocity = e.HasVelocity,
    };

    public EphemerisEntry ToEntry() => new(
        ObjectName,
        new DateTimeOffset(DateTimeTicks, TimeSpan.Zero),
        new double3(PosX, PosY, PosZ),
        new double3(VelX, VelY, VelZ),
        HasPosition,
        HasVelocity);
}

[Serializable]
public class PhysicalTraitsEntryJSON
{
    public string ObjectName;
    public string TraitName;
    public string StringValue;
    public double NumericValue;
    public bool HasNumericValue;
    public string Unit;

    public static PhysicalTraitsEntryJSON FromEntry(PhysicalTraitsEntry e) => new()
    {
        ObjectName = e.ObjectName,
        TraitName = e.TraitName,
        StringValue = e.Value.StringValue,
        NumericValue = e.Value.IsNumeric ? e.Value.NumericValue : 0d,
        HasNumericValue = e.Value.IsNumeric,
        Unit = e.Value.NumericValueUnit.ToString(),
    };

    public PhysicalTraitsEntry ToEntry()
    {
        Enum.TryParse(Unit, out UnitMeasurements u);

        ParsableDataValue val = HasNumericValue ? new ParsableDataValue(StringValue ?? NumericValue.ToString(), NumericValue, u) : new ParsableDataValue(StringValue, StringValue);

        return new PhysicalTraitsEntry(ObjectName, TraitName, val);
    }
}

[Serializable]
public class ObjectDataJSON
{
    public string ObjectName;
    public List<EphemerisEntryJSON> EphemerisData = new();
    public List<PhysicalTraitsEntryJSON> PhysicalTraitsData = new();
}

[Serializable]
public class HorizonsDatabaseJSONWrapper
{
    public List<ObjectDataJSON> Objects = new();
}

public static class HorizonsResponseSaver
{
    public static readonly string DatabaseFileName = "horizons_object_database.json";
    public static readonly string DatabaseFolderName = "HorizonsObjectDataCache";

    // In-memory store: ObjectName → ObjectDataJSON
    static readonly Dictionary<string, ObjectDataJSON> s_db = new(StringComparer.OrdinalIgnoreCase);
    static bool s_loaded;

    // Public helpers
    public static void SaveEphemeris(List<EphemerisEntry> entries)
    {
        if (entries == null || entries.Count == 0) return;

        EnsureLoaded();

        // Group by object name in case entries span multiple bodies
        foreach (EphemerisEntry entry in entries)
        {
            ObjectDataJSON obj = GetOrCreate(entry.ObjectName);
            EphemerisEntryJSON json = EphemerisEntryJSON.FromEntry(entry);

            if (!HasEphemerisEntry(obj, json.DedupKey)) obj.EphemerisData.Add(json);
        }

        Debug.Log($"[HorizonsResponseSaver] Ephemeris merged ({entries.Count} candidates).");
    }

    static void SavePhysicalTraits(List<PhysicalTraitsEntry> entries)
    {
        if (entries == null || entries.Count == 0) return;

        EnsureLoaded();

        foreach (PhysicalTraitsEntry entry in entries)
        {
            ObjectDataJSON obj = GetOrCreate(entry.ObjectName);
            PhysicalTraitsEntryJSON json = PhysicalTraitsEntryJSON.FromEntry(entry);

            if (!HasPhysicalTraitsEntry(obj, json.TraitName)) obj.PhysicalTraitsData.Add(json);
        }

        Debug.Log($"[HorizonsResponseSaver] Physical traits merged ({entries.Count} candidates).");
    }

    public static void SavePhysicalTraits(string objectName, Dictionary<string, ParsedPhysicalProperty> bodyData)
    {
        if (string.IsNullOrWhiteSpace(objectName) || bodyData == null) return;

        var entries = new List<PhysicalTraitsEntry>(bodyData.Count);
        foreach (var kvp in bodyData)
        {
            var prop = kvp.Value;

            ParsableDataValue value = prop.NumericValue.HasValue ? new ParsableDataValue(prop.RawValue, prop.NumericValue.Value, prop.Unit) : new ParsableDataValue(prop.RawValue, prop.RawValue);

            entries.Add(new PhysicalTraitsEntry(
                objectName,
                kvp.Key,
                value));
        }

        SavePhysicalTraits(entries);
    }

    public static bool TryRemoveObject(string objectName)
    {
        EnsureLoaded();
        bool removed = s_db.Remove(objectName);
        if (removed) Debug.Log($"[HorizonsResponseSaver] Removed object '{objectName}' from database.");
        return removed;
    }

    public static bool TryGetObjectData(string objectName, out ObjectData data)
    {
        data = default;
        EnsureLoaded();

        if (!s_db.TryGetValue(objectName, out ObjectDataJSON obj)) return false;

        var ephemeris = new List<EphemerisEntry>(obj.EphemerisData.Count);
        foreach (var e in obj.EphemerisData)
        {
            ephemeris.Add(e.ToEntry());
        }

        var traits = new List<PhysicalTraitsEntry>(obj.PhysicalTraitsData.Count);
        foreach (var t in obj.PhysicalTraitsData)
        {
            traits.Add(t.ToEntry());
        }

        data = new ObjectData(obj.ObjectName, ephemeris, traits);
        return true;
    }

    public static IReadOnlyCollection<string> GetAllSavedObjectDataNames()
    {
        EnsureLoaded();
        return s_db.Keys;
    }

    public static IReadOnlyDictionary<string, ObjectDataJSON> GetAllSavedObjectData()
    {
        EnsureLoaded();
        return s_db;
    }

    public static bool TrySaveToFile()
    {
        var wrapper = new HorizonsDatabaseJSONWrapper
        {
            Objects = new List<ObjectDataJSON>(s_db.Values)
        };

        string json = JsonUtility.ToJson(wrapper, prettyPrint: true);
        string folder = GetFolder();
        string path = GetFilePath();

        Directory.CreateDirectory(folder);
        string tempPath = path + ".tmp";

        try
        {
            File.WriteAllText(tempPath, json);
            File.Copy(tempPath, path, overwrite: true);
            File.Delete(tempPath);
        }
        catch (Exception e)
        {
            Debug.LogError($"[HorizonsResponseSaver] Save failed: {e}");
            return false;
        }
        finally
        {
            if (File.Exists(tempPath)) File.Delete(tempPath);
        }

        Debug.Log($"[HorizonsResponseSaver] Database saved → {path}");
        UIMessage.Instance.NewFadingMessage(MessageType.Success, "Horizons database saved", 3f);
        return true;
    }

    public static bool TryLoadFromFile()
    {
        s_db.Clear();
        s_loaded = true;   // mark even on failure so EnsureLoaded doesn't retry in the same session

        string path = GetFilePath();
        if (!File.Exists(path))
        {
            Debug.Log($"[HorizonsResponseSaver] No database file found at {path}. Starting fresh.");
            return false;
        }

        string json;
        try
        {
            json = File.ReadAllText(path);
        }
        catch (Exception e)
        {
            Debug.LogError($"[HorizonsResponseSaver] Load failed: {e}"); return false;
        }

        if (string.IsNullOrWhiteSpace(json)) return false;

        var wrapper = JsonUtility.FromJson<HorizonsDatabaseJSONWrapper>(json);
        if (wrapper?.Objects == null) return false;

        foreach (var obj in wrapper.Objects)
        {
            if (!string.IsNullOrWhiteSpace(obj.ObjectName)) s_db[obj.ObjectName] = obj;
        }

        Debug.Log($"[HorizonsResponseSaver] Loaded {s_db.Count} object(s) from {path}.");
        return true;
    }

    public static bool TryRemovePhysicalTraitsEntry(string objectName, string traitName)
    {
        EnsureLoaded();

        if (!s_db.TryGetValue(objectName, out ObjectDataJSON obj)) return false;

        int removed = obj.PhysicalTraitsData.RemoveAll(e => string.Equals(e.TraitName, traitName, StringComparison.OrdinalIgnoreCase));
        if (removed > 0) Debug.Log($"[HorizonsResponseSaver] Removed {removed} physical traits entry of type '{traitName}' from {objectName}.");

        return removed > 0;
    }

    public static bool TryRemoveEphemerisEntry(string objectName, string dedupKey)
    {
        EnsureLoaded();

        if (!s_db.TryGetValue(objectName, out ObjectDataJSON obj)) return false;

        int removed = obj.EphemerisData.RemoveAll(e => e.DedupKey == dedupKey);
        if (removed > 0) Debug.Log($"[HorizonsResponseSaver] Removed {removed} ephemeris entry(ies) from {objectName}.");

        return removed > 0;
    }

    // Private helpers 
    static void EnsureLoaded()
    {
        if (!s_loaded) TryLoadFromFile();
    }
    static ObjectDataJSON GetOrCreate(string objectName)
    {
        if (!s_db.TryGetValue(objectName, out ObjectDataJSON obj))
        {
            obj = new ObjectDataJSON { ObjectName = objectName };
            s_db[objectName] = obj;
        }
        return obj;
    }
    static bool HasEphemerisEntry(ObjectDataJSON obj, string dedupKey)
    {
        foreach (var e in obj.EphemerisData)
        {
            if (e.DedupKey == dedupKey) return true;
        }
        return false;
    }

    static bool HasPhysicalTraitsEntry(ObjectDataJSON obj, string traitName)
    {
        foreach (var t in obj.PhysicalTraitsData)
        {
            if (string.Equals(t.TraitName, traitName, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    static string GetFolder() => Path.Combine(Application.persistentDataPath, DatabaseFolderName);
    static string GetFilePath() => Path.Combine(GetFolder(), DatabaseFileName);
}