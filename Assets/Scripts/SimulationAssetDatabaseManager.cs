using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using NaughtyAttributes;
using UnityEditor.Overlays;

public class SimulationAssetDatabaseManager : MonoBehaviour
{
    public static SimulationAssetDatabaseManager Instance { get; private set; }

    public List<Data> SimulationAssetDatabase { get; private set; } = new();

    string SavePath => Path.Combine(Application.persistentDataPath, "simulation_asset_database.json");
    string TextureDir => Path.Combine(Application.persistentDataPath, "asset_textures");

    public static event Action OnDatabaseChanged;
    public bool IsLoaded { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        IsLoaded = false;
    }

    public bool TryAddBody(Data data)
    {
        foreach (Data existingData in SimulationAssetDatabase)
        {
            if (string.Equals(existingData.Body.Name, data.Body.Name, StringComparison.OrdinalIgnoreCase))
            {
                UIMessage.Instance.NewUIMessage(MessageType.Error, $"Failed to add {data.Body.Name} to asset database, an object with that name already exists", "Failed Task");
                return false;
            }
        }

        data.Display = WriteDisplayTexture(data.Body.Name, data.Display);

        SimulationAssetDatabase.Add(data);
        SaveDatabase();
        return true;
    }

    public void TryDeleteBody(string name)
    {
        void PerformDelete()
        {
            int index = SimulationAssetDatabase.FindIndex(x =>
                string.Equals(x.Body.Name, name, StringComparison.OrdinalIgnoreCase));

            if (index < 0)
            {
                UIMessage.Instance.NewUIMessage(MessageType.Error, $"Body {name} not found in database.", "Failed Task");
                return;
            }

            DeleteTexture(SimulationAssetDatabase[index].Display.DisplayImageFileName);

            SimulationAssetDatabase.RemoveAt(index);
            if (NBodyManager.Instance.TryGetAstroObjectByName(name, out _))
                NBodyManager.Instance.TryRemoveObjectByName(name);

            SaveDatabase();
        }

        if (SimulationSaveLoad.IsObjectInAnySave(name))
        {
            UIMessage.Instance.NewUIConfirmation(
                $"Are you sure you want to delete {name}? It exists in one or more saves " +
                $"and deleting it may cause those saves to load with missing bodies.",
                "Confirm Delete",
                onYes: PerformDelete,
                onNo: () => { });
        }
        else PerformDelete();
    }

    public bool TryGetBodyByName(string name, out Data data)
    {
        foreach (Data dataEntry in SimulationAssetDatabase)
        {
            if (string.Equals(dataEntry.Body.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                data = dataEntry;
                return true;
            }
        }

        data = default;
        return false;
    }

    bool TryGetIndex(string name, out int index)
    {
        index = SimulationAssetDatabase.FindIndex(x => string.Equals(x.Body.Name, name, StringComparison.OrdinalIgnoreCase));

        return index >= 0;
    }

    string SaveTexture(Texture2D texture, string bodyName)
    {
        if (texture == null) return string.Empty;

        Directory.CreateDirectory(TextureDir);

        string fileName = $"{SanitizeFileName(bodyName)}.png";
        string fullPath = Path.Combine(TextureDir, fileName);

        byte[] pngBytes = texture.EncodeToPNG();
        File.WriteAllBytes(fullPath, pngBytes);

        return fileName;
    }

    Texture2D LoadTexture(string fileName)
    {
        if (string.IsNullOrEmpty(fileName)) return null;

        string fullPath = Path.Combine(TextureDir, fileName);
        if (!File.Exists(fullPath)) return null;

        byte[] pngBytes = File.ReadAllBytes(fullPath);

        // Use a 1×1 placeholder; LoadImage resizes it automatically
        Texture2D texture = new(1, 1, TextureFormat.RGBA32, false);
        if (texture.LoadImage(pngBytes)) return texture;

        Destroy(texture);
        return null;
    }

    void DeleteTexture(string fileName)
    {
        if (string.IsNullOrEmpty(fileName)) return;

        string fullPath = Path.Combine(TextureDir, fileName);
        if (File.Exists(fullPath)) File.Delete(fullPath);
    }

    static string SanitizeFileName(string name)
    {
        foreach (char c in Path.GetInvalidFileNameChars())
            name = name.Replace(c, '_');

        return name;
    }

    public void EditBodyMass(string name, double newMass)
    {
        if (newMass <= 0)
        {
            UIMessage.Instance.NewUIMessage(MessageType.Error, $"You cannot set an object's mass to less than or equal to 0", "Failed Task");
            return;
        }

        if (!TryGetIndex(name, out int index))
        {
            Debug.LogError($"[Database] Body {name} not found");
            return;
        }

        Data data = SimulationAssetDatabase[index];
        data.Body.Mass = newMass;
        SimulationAssetDatabase[index] = data;

        SaveDatabase();
    }

    public void EditBodyDiameter(string name, double newDiameter)
    {
        if (newDiameter <= 0)
        {
            UIMessage.Instance.NewUIMessage(MessageType.Error, $"You cannot set an object's diameter to less than or equal to 0", "Failed Task");
            return;
        }

        if (!TryGetIndex(name, out int index))
        {
            Debug.LogError($"[Database] Body {name} not found");
            return;
        }

        Data data = SimulationAssetDatabase[index];
        data.Body.Diameter = newDiameter;
        SimulationAssetDatabase[index] = data;

        SaveDatabase();
    }

    public void EditBodyType(string name, BodyType newBodyType)
    {
        if (!TryGetIndex(name, out int index))
        {
            Debug.LogError($"[Database] Body {name} not found");
            return;
        }

        Data data = SimulationAssetDatabase[index];
        data.Body.Type = newBodyType;
        SimulationAssetDatabase[index] = data;

        SaveDatabase();
    }

    public void EditBodyMaterial(string name, string materialKey)
    {
        if (string.IsNullOrEmpty(materialKey))
        {
            UIMessage.Instance.NewUIMessage(MessageType.Error, $"Invalid material key for {name}", "Failed Task");
            return;
        }

        if (!TryGetIndex(name, out int index))
        {
            UIMessage.Instance.NewUIMessage(MessageType.Error, $"Body {name} not found", "Failed Task");
            return;
        }

        Data data = SimulationAssetDatabase[index];

        data.Visual.MaterialName = materialKey;

        SimulationAssetDatabase[index] = data;

        SaveDatabase();
    }

    public void EditBodyRotation(string name, RotationData newRotationData)
    {
        int index = SimulationAssetDatabase.FindIndex(x => string.Equals(x.Body.Name, name, StringComparison.OrdinalIgnoreCase));

        if (index < 0)
        {
            Debug.LogError($"[Database] Body not found: {name}");
            return;
        }

        Data data = SimulationAssetDatabase[index];
        data.Rotation = newRotationData;
        SimulationAssetDatabase[index] = data;

        UIMessage.Instance.NewUIMessage(MessageType.Success, $"Updated rotation data for {name}", "Success");

        SaveDatabase();
    }

    public void EditBodyName(string currentName, string newName)
    {
        if (string.IsNullOrEmpty(newName))
        {
            UIMessage.Instance.NewUIMessage(MessageType.Error, $"Failed to rename {currentName}, new name cannot be empty!", "Failed Task");
            return;
        }
        if (TryGetBodyByName(newName, out _))
        {
            UIMessage.Instance.NewUIMessage(MessageType.Error, $"Failed to rename {currentName} to {newName}, name already exists!", "Failed Task");
            return;
        }
        if (!TryGetIndex(currentName, out int index))
        {
            UIMessage.Instance.NewUIMessage(MessageType.Error, $"Failed to rename {currentName}, body not found!", "Failed Task");
            return;
        }

        Data data = SimulationAssetDatabase[index];

        // Rename the sidecar PNG to match the new body name
        string oldFileName = data.Display.DisplayImageFileName;
        if (!string.IsNullOrEmpty(oldFileName))
        {
            string oldPath = Path.Combine(TextureDir, oldFileName);
            string newFileName = $"{SanitizeFileName(newName)}.png";
            string newPath = Path.Combine(TextureDir, newFileName);

            if (File.Exists(oldPath)) File.Move(oldPath, newPath);

            DisplayData display = data.Display;
            display.DisplayImageFileName = newFileName;
            data.Display = display;
        }

        data.Body.Name = newName;
        SimulationAssetDatabase[index] = data;
        SaveDatabase();
    }

    void SaveDatabase()
    {
        var wrapper = new SimulationAssetDatabaseWrapper
        {
            SimulationAssetDatabase = SimulationAssetDatabase
        };

        string json = JsonUtility.ToJson(wrapper, true);
        File.WriteAllText(SavePath, json);

        OnDatabaseChanged?.Invoke();
        LoadDatabase();
    }

    public void LoadDatabase()
    {
        if (!File.Exists(SavePath))
        {
            SimulationAssetDatabase = new List<Data>();
            return;
        }

        string json = File.ReadAllText(SavePath);
        var wrapper = JsonUtility.FromJson<SimulationAssetDatabaseWrapper>(json);
        SimulationAssetDatabase = wrapper?.SimulationAssetDatabase ?? new List<Data>();

        for (int i = 0; i < SimulationAssetDatabase.Count; i++)
        {
            Data data = SimulationAssetDatabase[i];
            data.Display.DisplayImage = LoadTexture(data.Display.DisplayImageFileName);
            SimulationAssetDatabase[i] = data;
        }

        SimulationAssetsUIManager.Instance.BuildUI();
        IsLoaded = true;
    }

    DisplayData WriteDisplayTexture(string bodyName, DisplayData display)
    {
        if (display.DisplayImage != null)
            display.DisplayImageFileName = SaveTexture(display.DisplayImage, bodyName);

        return display;
    }
}

[Serializable]
public class SimulationAssetDatabaseWrapper
{
    public List<Data> SimulationAssetDatabase;
}
