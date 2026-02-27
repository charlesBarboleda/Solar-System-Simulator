using UnityEngine;
using Unity.Mathematics;

public class SunRenderingManager : MonoBehaviour
{
    [Header("Simulation References")]
    [SerializeField] SimulationObject _playerObject;
    [SerializeField] SimulationObject _sunObject;

    [Header("Render Objects")]
    [SerializeField] GameObject _sunNear;
    [SerializeField] GameObject _sunFar;

    [Header("Renderers")]
    [SerializeField] MeshRenderer _sunNearRenderer;
    [SerializeField] MeshRenderer _sunFarRenderer;

    [Header("Material Slot Index (0 if single material)")]
    [SerializeField, Min(0)] int _nearMaterialIndex = 0;
    [SerializeField, Min(0)] int _farMaterialIndex = 0;

    [Header("Distances")]
    [SerializeField] float _switchDistanceKM = 100000f;

    [Header("Near Emission Range")]
    [SerializeField] float _nearMinEmissiveIntensity = 6000f;     // close
    [SerializeField] float _nearMaxEmissiveIntensity = 150000f;   // far (within near range)

    [Header("Near Emission Curve (far gets brighter aggressively)")]
    [Tooltip("> 1 = ramps up aggressively toward the far end. Try 3..10.")]
    [SerializeField, Min(0.01f)] float _emissionExponent = 6f;

    [Header("Near Color Range")]
    [SerializeField] Color _nearMinColor = Color.white;   // close
    [SerializeField] Color _nearMaxColor = Color.yellow;  // far (within near range)

    [Header("Near Color Curve (how fast it reaches _nearMaxColor)")]
    [Tooltip("< 1 = reaches _nearMaxColor sooner. Try 0.1..0.5.")]
    [SerializeField, Min(0.01f)] float _colorExponent = 0.2f;

    [Header("Debug")]
    [SerializeField] bool _debugLogs = false;

    // Shader Graph reference names
    static readonly int EmissionIntensityId = Shader.PropertyToID("_P_EmissionIntensity");
    static readonly int CoolColorId = Shader.PropertyToID("_P_CoolColor");

    MaterialPropertyBlock _nearMPB;

    float _switchDistanceUnity;
    bool _lastIsNear;

    void Awake()
    {
        if (!ValidateRefs())
        {
            enabled = false;
            return;
        }

        _switchDistanceUnity = (float)PhysicsConstants.ToUnityUnitsFromKM(_switchDistanceKM);
        if (_switchDistanceUnity <= 0f) _switchDistanceUnity = 0.0001f;

        _nearMPB = new MaterialPropertyBlock();
        _lastIsNear = _sunNear != null && _sunNear.activeSelf;
    }

    void LateUpdate()
    {
        // Compute distance once
        double3 playerPos = _playerObject.GetGlobalPosition();
        double3 sunPos = _sunObject.GetGlobalPosition();
        double distD = math.distance(playerPos, sunPos);

        bool isNear = distD < _switchDistanceUnity;

        // Only toggle when the state changes
        if (isNear != _lastIsNear)
        {
            _lastIsNear = isNear;
            _sunNear.SetActive(isNear);
            _sunFar.SetActive(!isNear);
        }

        if (!isNear) return;

        float dist = (float)distD; // safe within near range

        // Base normalized distance (0 close -> 1 far)
        float tBase = math.saturate(dist / _switchDistanceUnity);

        // Color: close = _nearMinColor, far = _nearMaxColor
        float tColor = math.pow(tBase, _colorExponent);
        Color coolColor = Color.Lerp(_nearMinColor, _nearMaxColor, tColor);

        // Emission: close = min, far = max, aggressively ramps near far end (exponent > 1)
        float tEmission = math.pow(tBase, _emissionExponent);
        float intensity = math.lerp(_nearMinEmissiveIntensity, _nearMaxEmissiveIntensity, tEmission);

        ApplyNearOverrides(_sunNearRenderer, _nearMaterialIndex, _nearMPB, intensity, coolColor);

        if (_debugLogs)
        {
            Debug.Log($"dist={distD} switch={_switchDistanceUnity} tBase={tBase}");
            Debug.Log($"tColor={tColor} color={coolColor}");
            Debug.Log($"tEmission={tEmission} intensity={intensity}");
        }
    }

    void ApplyNearOverrides(MeshRenderer renderer, int materialIndex, MaterialPropertyBlock mpb, float intensity, Color coolColor)
    {
        renderer.GetPropertyBlock(mpb, materialIndex);
        mpb.SetFloat(EmissionIntensityId, intensity);
        mpb.SetColor(CoolColorId, coolColor);
        renderer.SetPropertyBlock(mpb, materialIndex);
    }

    bool ValidateRefs()
    {
        if (_playerObject == null) { Debug.LogError("No reference assigned to '_playerObject'"); return false; }
        if (_sunObject == null) { Debug.LogError("No reference assigned to '_sunObject'"); return false; }
        if (_sunNear == null) { Debug.LogError("No reference assigned to '_sunNear'"); return false; }
        if (_sunFar == null) { Debug.LogError("No reference assigned to '_sunFar'"); return false; }
        if (_sunNearRenderer == null) { Debug.LogError("No reference assigned to '_sunNearRenderer'"); return false; }
        if (_sunFarRenderer == null) { Debug.LogError("No reference assigned to '_sunFarRenderer'"); return false; }
        return true;
    }
}