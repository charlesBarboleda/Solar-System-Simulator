using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class SimulationSaveLoad
{
    static string SaveDirectory => Path.Combine(Application.persistentDataPath, "saves");

    static string IndexPath => Path.Combine(SaveDirectory, "save_index.json");

    static string SlotPath(string id) => Path.Combine(SaveDirectory, $"{id}.json");


    // Public API
    public static SaveSlotMeta Save(string displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
        {
            UIMessage.Instance.NewUIMessage(MessageType.Error, "Save name cannot be empty.", "Save Failed");
            return null;
        }

        EnsureSaveDirectoryExists();

        SimulationSaveData saveData = NBodyManager.Instance.CreateSaveData();

        var meta = new SaveSlotMeta
        {
            Id = Guid.NewGuid().ToString("N"), // compact, no hyphens
            DisplayName = displayName.Trim(),
            SavedAt = DateTime.UtcNow.ToString("o"), // ISO 8601 round-trip
            BodyCount = saveData.Bodies.Count,
            SimDays = saveData.CurrentSimDays
        };

        saveData.Meta = meta;

        // Write the full save file
        string slotJson = JsonUtility.ToJson(saveData, prettyPrint: true);
        File.WriteAllText(SlotPath(meta.Id), slotJson);

        // Register in the index
        SimulationSaveIndex index = ReadIndex();
        index.Slots.Add(meta);
        WriteIndex(index);

        SimulationStateDatabaseUIManager.Instance.Initialize();

        UIMessage.Instance.NewUIMessage(MessageType.Success, $"Saved \"{displayName}\" ({saveData.Bodies.Count} bodies)", "Save Successful");

        return meta;
    }

    public static bool Load(string saveId)
    {
        string path = SlotPath(saveId);

        if (!File.Exists(path))
        {
            UIMessage.Instance.NewUIMessage(MessageType.Error, $"Save file not found (id: {saveId}).", "Load Failed");
            return false;
        }

        string json = File.ReadAllText(path);
        SimulationSaveData saveData = JsonUtility.FromJson<SimulationSaveData>(json);

        if (saveData == null)
        {
            UIMessage.Instance.NewUIMessage(MessageType.Error, "Save file is corrupt or unreadable.", "Load Failed");
            return false;
        }

        NBodyManager.Instance.ApplyLoadedState(saveData);

        string displayName = saveData.Meta?.DisplayName ?? saveId;
        UIMessage.Instance.NewUIMessage(MessageType.Success, $"Loaded \"{displayName}\"", "Load Successful");

        return true;
    }

    public static void Delete(string saveId)
    {
        string path = SlotPath(saveId);

        if (File.Exists(path))
            File.Delete(path);

        // Remove from index regardless of whether the file existed
        SimulationSaveIndex index = ReadIndex();
        int removed = index.Slots.RemoveAll(s => s.Id == saveId);
        WriteIndex(index);

        if (removed > 0)
        {
            UIMessage.Instance.NewUIMessage(MessageType.Info, "Save deleted.", "Delete Successful");
            SimulationStateDatabaseUIManager.Instance.Initialize();
        }
        else
        {
            UIMessage.Instance.NewUIMessage(MessageType.Warning, $"No save slot found with id: {saveId}", "Delete Warning");
        }
    }

    public static List<SaveSlotMeta> GetAllSaves()
    {
        SimulationSaveIndex index = ReadIndex();

        // Sort newest first for the UI
        index.Slots.Sort((a, b) => b.SavedAtDateTime.CompareTo(a.SavedAtDateTime));

        return index.Slots;
    }

    public static bool IsObjectInAnySave(string objectName)
    {
        foreach (SaveSlotMeta meta in ReadIndex().Slots)
        {
            if (IsObjectInSave(objectName, meta.Id))
                return true;
        }
        return false;
    }

    public static bool IsObjectInSave(string objectName, string saveId)
    {
        string path = SlotPath(saveId);

        if (!File.Exists(path)) return false;

        string json = File.ReadAllText(path);
        SimulationSaveData saveData = JsonUtility.FromJson<SimulationSaveData>(json);

        if (saveData?.Bodies == null) return false;

        foreach (BodyStateData body in saveData.Bodies)
        {
            if (string.Equals(body.Name, objectName, StringComparison.OrdinalIgnoreCase)) return true;
        }

        return false;
    }

    static SimulationSaveIndex ReadIndex()
    {
        EnsureSaveDirectoryExists();

        if (!File.Exists(IndexPath)) return new SimulationSaveIndex();

        string json = File.ReadAllText(IndexPath);
        return JsonUtility.FromJson<SimulationSaveIndex>(json) ?? new SimulationSaveIndex();
    }

    static void WriteIndex(SimulationSaveIndex index)
    {
        EnsureSaveDirectoryExists();
        File.WriteAllText(IndexPath, JsonUtility.ToJson(index, prettyPrint: true));
    }

    static void EnsureSaveDirectoryExists()
    {
        if (!Directory.Exists(SaveDirectory)) Directory.CreateDirectory(SaveDirectory);
    }
}