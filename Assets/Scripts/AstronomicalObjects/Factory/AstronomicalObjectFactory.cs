using UnityEngine;
using System;
using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

[Serializable]
public struct MaterialEntry
{
    public string Key;
    public Material Material;
}

public enum DefaultMaterialNames
{
    Moon,
    Neptune,
    Uranus,
    Venus,
    Earth,
    Jupiter,
    Mars,
    Mercury,
    Pluto,
    Saturn
}

public class AstronomicalObjectFactory : MonoBehaviour
{
    public static AstronomicalObjectFactory Instance { get; private set; }

    [SerializeField] GameObject _planetBasePrefab;
    [SerializeField] GameObject _ringPlanetBasePrefab;
    [SerializeField] GameObject _starBasePrefab;
    [SerializeField] GameObject _asteroidBasePrefab;
    [SerializeField] GameObject _satelliteBasePrefab;
    [SerializeField] GameObject _moonBasePrefab;

    [SerializeField] List<MaterialEntry> _materials = new();
    Dictionary<string, Material> _materialLookup;

    [SerializeField] Vector3 _rotationOffset = new(20, 0, 20);

    public float DistKM = 1f;
    public float EmissionIntensity = 1000f;

    public event Action<bool> OnIsCreatingAssetChanged;
    bool _isCreatingAsset;
    public bool IsCreatingAsset
    {
        get => _isCreatingAsset;
        private set
        {
            if (_isCreatingAsset == value) return;

            _isCreatingAsset = value;

            OnIsCreatingAssetChanged?.Invoke(_isCreatingAsset);
        }
    }

    public void StartCreating() => IsCreatingAsset = true;
    public void FinishCreating() => IsCreatingAsset = false;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        InitializeMaterialLookup();

        IsCreatingAsset = false;
    }

    public List<string> GetAllMaterialKeys()
    {
        if (_materialLookup == null) InitializeMaterialLookup();

        return _materialLookup != null ? new List<string>(_materialLookup.Keys) : new List<string>();
    }

    public bool TryGetMaterial(string key, out Material material)
    {
        material = null;

        if (string.IsNullOrWhiteSpace(key)) return false;

        if (_materialLookup == null) InitializeMaterialLookup();

        return _materialLookup != null && _materialLookup.TryGetValue(key, out material);
    }

    public bool TryRemoveMaterialEntry(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            Debug.LogWarning("Material key is null or empty.");
            return false;
        }

        int removed = _materials.RemoveAll(entry => string.Equals(entry.Key, key, StringComparison.OrdinalIgnoreCase));

        if (removed <= 0)
        {
            Debug.LogWarning($"Material key not found: {key}");
            return false;
        }

        InitializeMaterialLookup();
        UIMessage.Instance.NewFadingMessage(MessageType.Success, $"Removed material with key: {key}");
        return true;
    }

    public bool TryAddNewMaterialEntry(MaterialEntry materialEntry)
    {
        if (string.IsNullOrWhiteSpace(materialEntry.Key) || materialEntry.Material == null)
        {
            Debug.LogWarning("Invalid key or material.");
            return false;
        }

        for (int i = 0; i < _materials.Count; i++)
        {
            if (string.Equals(_materials[i].Key, materialEntry.Key, StringComparison.OrdinalIgnoreCase))
            {
                int capturedIndex = i;

                UIMessage.Instance.NewUIConfirmation(
                    $"A material with the key '{materialEntry.Key}' already exists. Do you want to overwrite it?",
                    "Confirm Material Overwrite",
                    onYes: () =>
                    {
                        _materials[capturedIndex] = materialEntry;
                        InitializeMaterialLookup();

                        UIMessage.Instance.NewFadingMessage(MessageType.Success, $"Material with key '{materialEntry.Key}' has been overwritten");
                    });

                return true;
            }
        }

        _materials.Add(materialEntry);
        InitializeMaterialLookup();
        UIMessage.Instance.NewFadingMessage(MessageType.Success, $"Added new material with key: {materialEntry.Key}");
        return true;
    }

    void InitializeMaterialLookup()
    {
        _materialLookup = new Dictionary<string, Material>(StringComparer.OrdinalIgnoreCase);

        if (_materials == null) return;

        foreach (var entry in _materials)
        {
            if (string.IsNullOrWhiteSpace(entry.Key) || entry.Material == null) continue;

            _materialLookup[entry.Key] = entry.Material;
        }
    }

    bool ValidateData(Data data)
    {
        bool isValid = true;

        if (string.IsNullOrWhiteSpace(data.Body.Name))
        {
            UIMessage.Instance.NewFadingMessage(MessageType.Error, "Invalid name input data, ensure the name field is filled out correctly.", 10f);
            isValid = false;
        }

        if (!IsValidPositiveNumber(data.Body.Diameter))
        {
            UIMessage.Instance.NewFadingMessage(MessageType.Error, "Invalid diameter input data, ensure the diameter field is greater than zero and finite.", 10f);
            isValid = false;
        }

        if (!IsValidPositiveNumber(data.Body.Mass))
        {
            UIMessage.Instance.NewFadingMessage(MessageType.Error, "Invalid mass input data, ensure the mass field is greater than zero and finite.", 10f);
            isValid = false;
        }

        return isValid;
    }

    static bool IsValidPositiveNumber(double value) => !(double.IsNaN(value) || double.IsInfinity(value) || value <= 0.0);

    public GameObject CreateEmptyAstroObject(BodyType bodyType, bool isRingPlanet = false)
    {
        if (isRingPlanet && bodyType == BodyType.Planet) return Instantiate(_ringPlanetBasePrefab);

        return bodyType switch
        {
            BodyType.Planet => Instantiate(_planetBasePrefab),
            BodyType.Asteroid => Instantiate(_asteroidBasePrefab),
            BodyType.Satellite => Instantiate(_satelliteBasePrefab),
            BodyType.Moon => Instantiate(_moonBasePrefab),
            BodyType.Star => Instantiate(_starBasePrefab),
            _ => Instantiate(_planetBasePrefab),
        };
    }

    public AstronomicalObject CreateAstronomicalObject(Data data, bool AddToRuntime = false, bool AddToAssetDatabase = false)
    {
        StartCreating();

        try
        {
            if (!ValidateData(data)) return null;

            // Handle database-only path before any prefab work
            if (AddToAssetDatabase)
            {
                SimulationAssetDatabaseManager.Instance.TryAddBody(data);
                return null;
            }

            GameObject go = CreatePrefabInstance(data);

            if (go == null)
            {
                Debug.LogError($"[{nameof(AstronomicalObjectFactory)}] Failed to create object for {data.Body.Name}.");
                return null;
            }

            if (!go.TryGetComponent<AstronomicalObject>(out var astroObject))
                astroObject = go.AddComponent<AstronomicalObject>();

            go.name = data.Body.Name;

            SetVisualAppearance(astroObject, data);
            astroObject.Initialize(data);

            if (AddToRuntime)
            {
                if (!NBodyManager.Instance.TryAddObject(astroObject))
                {
                    Destroy(go);
                    return null;
                }
            }

            return astroObject;
        }
        finally
        {
            FinishCreating();
        }
    }

    GameObject CreatePrefabInstance(Data data)
    {
        GameObject prefab = GetPrefabForData(data);

        if (prefab == null)
        {
            Debug.LogError($"[{nameof(AstronomicalObjectFactory)}] No prefab assigned for {data.Body.Type} / {data.Body.Name}.");
            return null;
        }

        return Instantiate(prefab);
    }

    GameObject GetPrefabForData(Data data)
    {
        if (data.Body.Type == BodyType.Planet && data.Ring.IsRingPlanet) return _ringPlanetBasePrefab;

        return data.Body.Type switch
        {
            BodyType.Planet => _planetBasePrefab,
            BodyType.Asteroid => _asteroidBasePrefab,
            BodyType.Star => _starBasePrefab,
            BodyType.Moon => _moonBasePrefab,
            BodyType.Satellite => _satelliteBasePrefab,
            _ => _planetBasePrefab,
        };
    }

    public void SetVisualAppearance(AstronomicalObject astroObject, Data data)
    {
        if (astroObject == null || data.Body.Type != BodyType.Planet) return;

        if (_materialLookup == null) InitializeMaterialLookup();

        string key = data.Visual.MaterialName;

        if (string.IsNullOrWhiteSpace(key)) return;

        if (!_materialLookup.TryGetValue(key, out Material mat))
        {
            Debug.LogWarning($"Material key not found: {key}");
            return;
        }

        if (astroObject.TryGetComponent<MeshRenderer>(out var renderer)) renderer.material = mat;
        else Debug.LogWarning("MeshRenderer not found on AstronomicalObject.");
    }
}