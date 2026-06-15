using UnityEngine;
using Unity.Mathematics;
using System;
using UnityEngine.VFX;
using NaughtyAttributes;

public class SunRenderingManager : MonoBehaviour
{
    // // Gran / Layer
    // [SerializeField] float _debugGranSpeed = 1f;
    // [SerializeField] float _debugGranStrength = 10f;
    // [SerializeField] float _debugGranWorldScale = 100f;
    // [SerializeField] float _debugTriSharpness = 2f;
    // [SerializeField] float _debugL1Scale = 25f;
    // [SerializeField] float _debugL1Speed = 0.5f;
    // [SerializeField] float _debugL2Scale = 20f;
    // [SerializeField] float _debugL2Speed = 0.6f;
    // [SerializeField] float _debugLayerBlend = 0f;

    // [Header("Debug - Scroll Dirs")]
    // [SerializeField] Vector2 _debugScrollDirXY = new Vector2(0.8f, 0.1f);
    // [SerializeField] Vector2 _debugScrollDirXZ = new Vector2(-0.2f, 0.7f);
    // [SerializeField] Vector2 _debugScrollDirYZ = new Vector2(0.3f, -0.6f);

    // [Header("Debug - Scroll 2 Dirs")]
    // [SerializeField] Vector2 _debugScrollDir2XY = new Vector2(-0.4f, 0.9f);
    // [SerializeField] Vector2 _debugScrollDir2XZ = new Vector2(0.7f, 0.2f);
    // [SerializeField] Vector2 _debugScrollDir2YZ = new Vector2(-0.8f, 0.3f);

    // [Header("Debug - Scroll 2")]
    // [SerializeField] float _debugScroll2SpeedMul = 0.25f;
    // [SerializeField] float _debugScroll2Blend = 0.5f;

    // [Header("Debug - Phase")]
    // [SerializeField] float _debugEpsilon = -0.2f;
    // [SerializeField] float _debugPhaseXY = 10f;
    // [SerializeField] float _debugPhaseXZ = -21.44f;
    // [SerializeField] float _debugPhaseYZ = 29.1f;
    // [SerializeField] float _debugLimbMin = 0.6f;
    // [SerializeField] float _debugLimbPower = 0.2f;

    // [Header("Debug - Bright Blob")]
    // [SerializeField] float _debugBrightBlobPower = 9f;
    // [SerializeField] float _debugBrightThresholdLow = 0f;
    // [SerializeField] float _debugBrightThresholdHigh = 0.05f;
    // [SerializeField] float _debugBrightNoiseScale = 2.5f;
    // [SerializeField] float _debugBrightIntensity = 3f;

    // [Header("Debug - Bright Streak")]
    // [SerializeField] float _debugBrightStreakNoiseScale = 1f;
    // [SerializeField] float _debugBrightStreakThresholdLow = 1f;
    // [SerializeField] float _debugBrightStreakThresholdHigh = 1f;
    // [SerializeField] float _debugBrightStreakPower = 1f;

    // [Header("Debug - Bright Warp")]
    // [SerializeField] float _debugBrightWarpNoiseScale = 10f;
    // [SerializeField] float _debugBrightWarpStrength = 6f;
    // [SerializeField] float _debugBrightWarpSpeed = 3f;
    // [SerializeField] Vector2 _debugBrightWarpDirXY = new Vector2(0.01f, 0.035f);
    // [SerializeField] Vector2 _debugBrightWarpDirXZ = new Vector2(-0.05f, 0.025f);
    // [SerializeField] Vector2 _debugBrightWarpDirYZ = new Vector2(0.063f, -1.02f);

    // [Header("Debug - Bright Life")]
    // [SerializeField] float _debugBrightLifeLocalStrength = 0.7f;
    // [SerializeField] float _debugBrightLifeSpeed = 0.01f;
    // [SerializeField] float _debugBrightLifeNoiseScale = 6f;
    // [SerializeField] float _debugBrightLifeThresholdLow = 0.35f;
    // [SerializeField] float _debugBrightLifeThresholdHigh = 1f;
    // [SerializeField] float _debugBrightLifeMaskPower = 6f;

    // [Header("Debug - Bright Scroll")]
    // [SerializeField] float _debugBrightSoftLow = 0.2f;
    // [SerializeField] float _debugBrightSoftHigh = 0.5f;
    // [SerializeField] float _debugBrightCoreLow = 0.7f;
    // [SerializeField] float _debugBrightCoreHigh = 0.9f;
    // [SerializeField] float _debugBrightColorIntensity = 4f;
    // [SerializeField] float _debugBrightCoreIntensity = 4f;
    // [SerializeField] float _debugBrightBlendIntensity = 4.5f;
    // [SerializeField] float _debugBrightScrollBlend = 0.15f;

    [Header("Simulation References")]
    [SerializeField] SimulationObject _playerObject;
    [SerializeField] SimulationObject _sunObject;
    [SerializeField] AstronomicalObject _sunAstronomicalObject;

    [Header("Camera")]
    [SerializeField] Camera _playerCamera;

    [Header("Render Objects")]
    [SerializeField] GameObject _sunNear;
    [SerializeField] GameObject _sunFar;

    [Header("Renderers")]
    [SerializeField] MeshRenderer _sunNearRenderer;
    [SerializeField] MeshRenderer _sunFarRenderer;
    [SerializeField] MeshRenderer _sunStreakRenderer;
    [SerializeField] MeshRenderer _sunSpikesRenderer;
    [SerializeField] MeshRenderer _sunChromAberrationOuterRenderer;
    [SerializeField] MeshRenderer _sunChromAberrationInnerRenderer;

    [Header("Solar Flares References")]
    [SerializeField] VisualEffect _solarFlaresContinuousSmall1;
    [SerializeField] VisualEffect _solarFlaresContinuousSmall2;
    [SerializeField] VisualEffect _solarFlaresBurstMedium1;
    [SerializeField] VisualEffect _solarFlaresBurstMedium2;
    [SerializeField] VisualEffect _solarFlaresBurstMedium3;
    [SerializeField] VisualEffect _solarFlaresBurstBig1;
    [SerializeField] VisualEffect _solarFlaresBurstBig2;
    [SerializeField] VisualEffect _solarFlaresBurstBig3;
    [SerializeField] VisualEffect _solarFlaresBurstBig4;


    [Header("Temperature Kelvin")]
    [SerializeField] double _temperatureKelvin = 5000f;

    [Header("Material Slot Indices")]
    [SerializeField, Min(0)] int _nearMaterialIndex = 0;

    [Header("Distances (KM) — Tuned At Reference Speed")]
    [Tooltip("Distance at which the Far object fully takes over")]
    [SerializeField] float _switchDistanceKM = 5e8f;

    [Tooltip("Width of the cross-fade blend zone before the switch distance")]
    [SerializeField] float _blendZoneKM = 1.5e8f;

    [Tooltip("Extra buffer past switch distance before re-enabling Near")]
    [SerializeField] float _hysteresisKM = 1000000f;



    [Header("Near - Emission Range")]
    [SerializeField] float _nearMinEmissiveIntensity = 6000f;
    [SerializeField] float _nearMaxEmissiveIntensity = 150000f;

    [Header("Near - Emission Curve")]
    [Tooltip("> 1 ramps up aggressively toward the far end")]
    [SerializeField, Min(0.01f)] float _emissionExponent = 6f;

    [Header("Near - Color Range")]
    [SerializeField] Color _nearCoolColor = new(1f, 0.45f, 0.1f);
    [SerializeField] Color _nearHotColor = Color.white;
    [SerializeField] Color _nearMaxColor = Color.white;
    [Tooltip("Distance in KM at which the color finishes transitioning to white")]
    [SerializeField] float _colorWhiteDistanceKM = 3e8f;

    [Header("Near - Color Curve")]
    [Tooltip("< 1 reaches max color sooner")]
    [SerializeField, Min(0.01f)] float _colorExponent = 0.2f;

    [Header("Far - All Effects")]
    [SerializeField] float _completeFadeStartDistanceKM = 1e12f;
    [SerializeField] float _completeFadeEndDistanceKM = 1e12f;

    [Header("Far - Billboard")]
    [SerializeField] float _farPlacementDistance = 500000f;

    [Tooltip("Scale multiplier at switch point")]
    [SerializeField, Min(0.01f)] float _farScaleMin = 1f;

    [Tooltip("Scale multiplier at max distance")]
    [SerializeField, Min(0.01f)] float _farScaleMax = 1.8f;

    [Tooltip("Distance in KM at which Far Scale Max is reached")]
    [SerializeField] float _farScaleMaxDistanceKM = 1.4e9f;

    [Tooltip("Controls how fast scale grows. > 1 = slow start, fast finish")]
    [SerializeField, Min(0.01f)] float _farScaleExponent = 1f;

    [Header("Far - Streak")]
    [SerializeField, Range(0f, 1f)] float _streakMaxAlpha = 0.1f;
    [SerializeField] float _streakMaxAlphaDistanceKM = 7e8f;
    [SerializeField] GameObject _streaksObj;


    [Header("Far - Spikes")]
    [SerializeField, Range(0f, 1f)] float _spikesMaxAlpha = 0.5f;
    [SerializeField] float _spikesMaxAlphaDistanceKM = 6e8f;
    [SerializeField] GameObject _spikesObj;


    [Header("Far - Flare Exposure")]
    [Tooltip("Distance in KM past switch point at which streak/spikes Exposure Weight reaches 0")]
    [SerializeField] float _flareExposureFullDistanceKM = 7e8f;

    [Header("Far - Chromatic Aberration Ring")]
    [Tooltip("Chromatic Aberration ring alpha at full strength (0-1)")]
    [SerializeField, Range(0f, 1f)] float _chromaticAberrationMaxAlpha = 0.2f;
    [SerializeField] float _chromAberrationMaxAlphaDistanceKM = 1e9f;
    [SerializeField] float _chromAberrationInitialScale = 2.5f;
    [SerializeField] float _chromAberrationInitialDistance = 23299.89f;
    [SerializeField] float _chromAberrationScaleFactorMultiplier = 1f;
    [SerializeField] float _chromAberrationDistanceThreshold = 115000f;
    [SerializeField] float _chromAberrationNearScaleDampening = 0.85f;
    [SerializeField] GameObject _chromAberrationObj;

    [Header("Far - Lens Orbs")]
    [SerializeField] float _lensOrbsMaxAlpha = 0.00117f;
    [SerializeField] float _lensOrbsMaxAlphaDistanceKM = 1.4e9f;
    [SerializeField] float _lensOrbsInitialScale = 2.5f;
    [SerializeField] float _lensOrbsInitialDistance = 23299.89f;
    [SerializeField] float _lensOrbsScaleFactorMultiplier = 1f;
    [SerializeField] float _lensOrbsDistanceThreshold = 115000f;
    [SerializeField] float _lensOrbsNearScaleDampening = 0.85f;
    [SerializeField] GameObject _sunLensOrbNear;
    [SerializeField] GameObject _sunLensOrbFar;
    [SerializeField] MeshRenderer _sunLensOrbNearRenderer;
    [SerializeField] MeshRenderer _sunLensOrbFarRenderer;

    [Header("Speed Scaling")]
    [Tooltip("The reference speed in KM/s at which all distance variables were tuned")]
    [SerializeField] float _referenceSpeedKM = 1e13f;

    [Header("Debug")]
    [SerializeField] bool _debugLogs = false;

    // Shader property IDs
    static readonly int EmissionIntensityId = Shader.PropertyToID("_P_EmissionIntensity");
    static readonly int CoolColorId = Shader.PropertyToID("_P_CoolColor");
    static readonly int HotColorId = Shader.PropertyToID("_P_HotColor");
    static readonly int UnlitColorId = Shader.PropertyToID("_UnlitColor");

    MaterialPropertyBlock _nearMPB;

    // Cached scaled distances
    float _switchDist;
    float _blendStart;
    float _hysteresisDist;
    float _colorWhiteDist;
    float _completeStartFadeDist;
    float _completeEndFadeDist;
    float _farScaleMaxDist;
    float _flareExposureFullDist;
    float _chromAberrationMaxAlphaDist;
    float _farSpikesMaxAlphaDist;
    float _farStreakMaxAlphaDist;
    float _lensOrbsMaxAlphaDist;


    // Cached alpha values for dirty-checking
    float _lastSpikesAlpha = -1f;
    float _lastStreakAlpha = -1f;
    float _lastChromAberrationAlpha = -1f;
    float _lastOrbAlpha = -1f;

    // Cached distance range
    float _spikesDistRange;
    float _streakDistRange;
    float _chromAberrationDistRange;
    float _lensOrbsDistRange;

    // Static cached values
    float _sunRadiusUnity;
    float _currentSpeedKM;

    // Cached Materials 
    Material _spikesMat;
    Material _chromAberrationInnerMat;
    Material _chromAberrationOuterMat;
    Material _streakMat;
    Material _orbNearMat;
    Material _orbFarMat;

    // Size scaling
    const float ReferenceDiameterM = 999_999_999f;
    float _sizeRatio = 1f;

    // Cached size-scaled lens distances (size-only, not speed-dependent)
    float _chromAberrationInitialDistScaled;
    float _chromAberrationDistThresholdScaled;
    float _lensOrbsInitialDistScaled;
    float _lensOrbsDistThresholdScaled;

    enum SunState { Near, Blend, Far }
    SunState _currentState = SunState.Near;
    StarType _starType = StarType.MType;

    float _minMaxBoundOffset = 0.1f; // Allow some leeway outside the viewport for flare visibility

    bool _isDisplay = false;

    // void Update()
    // {
    //     ApplyDebugProperties();
    // }

    // void ApplyDebugProperties()
    // {
    //     _sunNearRenderer.GetPropertyBlock(_nearMPB, _nearMaterialIndex);

    //     // Gran / Layer
    //     _nearMPB.SetFloat("_GranSpeed", _debugGranSpeed);
    //     _nearMPB.SetFloat("_P_GranStrength", _debugGranStrength);
    //     _nearMPB.SetFloat("_P_GranWorldScale", _debugGranWorldScale);
    //     _nearMPB.SetFloat("_P_TriSharpness", _debugTriSharpness);
    //     _nearMPB.SetFloat("_P_L1_Scale", _debugL1Scale);
    //     _nearMPB.SetFloat("_P_L1_Speed", _debugL1Speed);
    //     _nearMPB.SetFloat("_P_L2_Scale", _debugL2Scale);
    //     _nearMPB.SetFloat("_P_L2_Speed", _debugL2Speed);
    //     _nearMPB.SetFloat("_LayerBlend", _debugLayerBlend);

    //     // Scroll Dirs (Layer 1)
    //     _nearMPB.SetVector("_P_ScrollDir_XY", _debugScrollDirXY);
    //     _nearMPB.SetVector("_P_ScrollDir_XZ", _debugScrollDirXZ);
    //     _nearMPB.SetVector("_P_ScrollDir_YZ", _debugScrollDirYZ);

    //     // Scroll Dirs (Layer 2)
    //     _nearMPB.SetVector("_P_ScrollDir2_XY", _debugScrollDir2XY);
    //     _nearMPB.SetVector("_P_ScrollDir2_XZ", _debugScrollDir2XZ);
    //     _nearMPB.SetVector("_P_ScrollDir2_YZ", _debugScrollDir2YZ);

    //     // Scroll 2
    //     _nearMPB.SetFloat("_P_Scroll2_SpeedMul", _debugScroll2SpeedMul);
    //     _nearMPB.SetFloat("_P_Scroll2_Blend", _debugScroll2Blend);

    //     // Phase / Limb
    //     _nearMPB.SetFloat("_epsilon", _debugEpsilon);
    //     _nearMPB.SetFloat("_P_Phase_XY", _debugPhaseXY);
    //     _nearMPB.SetFloat("_P_Phase_XZ", _debugPhaseXZ);
    //     _nearMPB.SetFloat("_P_Phase_YZ", _debugPhaseYZ);
    //     _nearMPB.SetFloat("_P_LimbMin", _debugLimbMin);
    //     _nearMPB.SetFloat("_P_LimbPower", _debugLimbPower);

    //     // Bright Blob
    //     _nearMPB.SetFloat("_BrightBlobPower", _debugBrightBlobPower);
    //     _nearMPB.SetFloat("_BrightThresholdLow", _debugBrightThresholdLow);
    //     _nearMPB.SetFloat("_BrightThresholdHigh", _debugBrightThresholdHigh);
    //     _nearMPB.SetFloat("_BrightNoiseScale", _debugBrightNoiseScale);
    //     _nearMPB.SetFloat("_BrightIntensity", _debugBrightIntensity);

    //     // Bright Streak
    //     _nearMPB.SetFloat("_BrightStreakNoiseScale", _debugBrightStreakNoiseScale);
    //     _nearMPB.SetFloat("_BrightStreakThresholdLow", _debugBrightStreakThresholdLow);
    //     _nearMPB.SetFloat("_BrightStreakThresholdHigh", _debugBrightStreakThresholdHigh);
    //     _nearMPB.SetFloat("_BrightStreakPower", _debugBrightStreakPower);

    //     // Bright Warp
    //     _nearMPB.SetFloat("_BrightWarpNoiseScale", _debugBrightWarpNoiseScale);
    //     _nearMPB.SetFloat("_BrightWarpStrength", _debugBrightWarpStrength);
    //     _nearMPB.SetFloat("_BrightWarpSpeed", _debugBrightWarpSpeed);
    //     _nearMPB.SetVector("_BrightWarpDir_XY", _debugBrightWarpDirXY);
    //     _nearMPB.SetVector("_BrightWarpDir_XZ", _debugBrightWarpDirXZ);
    //     _nearMPB.SetVector("_BrightWarpDir_YZ", _debugBrightWarpDirYZ);

    //     // Bright Life
    //     _nearMPB.SetFloat("_BrightLifeLocalStrength", _debugBrightLifeLocalStrength);
    //     _nearMPB.SetFloat("_BrightLifeSpeed", _debugBrightLifeSpeed);
    //     _nearMPB.SetFloat("_BrightLifeNoiseScale", _debugBrightLifeNoiseScale);
    //     _nearMPB.SetFloat("_BrightLifeThresholdLow", _debugBrightLifeThresholdLow);
    //     _nearMPB.SetFloat("_BrightLifeThresholdHigh", _debugBrightLifeThresholdHigh);
    //     _nearMPB.SetFloat("_BrightLifeMaskPower", _debugBrightLifeMaskPower);

    //     // Bright Soft / Core
    //     _nearMPB.SetFloat("_BrightSoftLow", _debugBrightSoftLow);
    //     _nearMPB.SetFloat("_BrightSoftHigh", _debugBrightSoftHigh);
    //     _nearMPB.SetFloat("_BrightCoreLow", _debugBrightCoreLow);
    //     _nearMPB.SetFloat("_BrightCoreHigh", _debugBrightCoreHigh);
    //     _nearMPB.SetFloat("_BrightColorIntensity", _debugBrightColorIntensity);
    //     _nearMPB.SetFloat("_BrightCoreIntensity", _debugBrightCoreIntensity);
    //     _nearMPB.SetFloat("_BrightBlendIntensity", _debugBrightBlendIntensity);
    //     _nearMPB.SetFloat("_BrightScrollBlend", _debugBrightScrollBlend);

    //     _sunNearRenderer.SetPropertyBlock(_nearMPB, _nearMaterialIndex);
    // }

    public void Initialize(Data data)
    {
        if (!ValidateReferences()) { enabled = false; return; }

        _temperatureKelvin = data.Body.Temperature;
        InitializeStarType();

        _sunFar.transform.SetParent(null, worldPositionStays: true);

        _sunRadiusUnity = (float)PhysicsConstants.ToUnityUnitsFromM(_sunAstronomicalObject.Data.Body.Diameter / 2.0f);
        _sizeRatio = (float)(_sunAstronomicalObject.Data.Body.Diameter / ReferenceDiameterM);

        _nearMPB = new MaterialPropertyBlock();
        _chromAberrationInnerMat = _sunChromAberrationInnerRenderer.material;
        _chromAberrationOuterMat = _sunChromAberrationOuterRenderer.material;
        _spikesMat = _sunSpikesRenderer.material;
        _streakMat = _sunStreakRenderer.material;
        _orbNearMat = _sunLensOrbNearRenderer.material;
        _orbFarMat = _sunLensOrbFarRenderer.material;

        _currentSpeedKM = _referenceSpeedKM;
        RecomputeScaledDistances();
        // InitializeColors();
        // InitializeSolarFlares();

        _isDisplay = false;

        ForceState(SunState.Far);
    }

    public void InitializeForDisplay(BodyData bodyData, bool farDisplay = false)
    {
        _temperatureKelvin = bodyData.Temperature;
        _nearMPB = new MaterialPropertyBlock();

        InitializeStarType();

        // if (_starType != StarType.GType)
        // {
        //     InitializeSolarFlares();
        //     InitializeColors();
        // }

        _currentSpeedKM = _referenceSpeedKM;
        RecomputeScaledDistances();

        if (!farDisplay)
        {
            float distKM = AstronomicalObjectFactory.Instance.DistKM;
            UpdateNear(distKM: distKM, nearAlpha: 1f);
            float emissionIntensity = AstronomicalObjectFactory.Instance.EmissionIntensity;
            _sunNearRenderer.GetPropertyBlock(_nearMPB, _nearMaterialIndex); // re-fetch current state
            _nearMPB.SetFloat(EmissionIntensityId, emissionIntensity);
            _sunNearRenderer.SetPropertyBlock(_nearMPB, _nearMaterialIndex); // actually apply it
        }
        else
        {
            _chromAberrationInnerMat = _sunChromAberrationInnerRenderer.material;
            _chromAberrationOuterMat = _sunChromAberrationOuterRenderer.material;
            _spikesMat = _sunSpikesRenderer.material;
            _streakMat = _sunStreakRenderer.material;
            _orbNearMat = _sunLensOrbNearRenderer.material;
            _orbFarMat = _sunLensOrbFarRenderer.material;

            // Activate only the far object
            _currentState = SunState.Far;
            _sunNear.SetActive(false);
            _sunFar.SetActive(true);
        }

        _isDisplay = true;

        if (!farDisplay) Destroy(_sunFar);
        else Destroy(_sunNear);
    }

    public void DestroyFarVisuals() => Destroy(_sunFar);

    void InitializeStarType()
    {
        // (2500 - 3800 K)
        if (_temperatureKelvin > 2500 && _temperatureKelvin < 3800) _starType = StarType.MType;
        // (3800 - 5200 K)
        else if (_temperatureKelvin >= 3800 && _temperatureKelvin < 5200) _starType = StarType.KType;
        // (5200 - 6000 K) Sun-like appearance 
        else if (_temperatureKelvin >= 5200 && _temperatureKelvin < 6000) _starType = StarType.GType;
        // (6000 - 7500 K)
        else if (_temperatureKelvin >= 6000 && _temperatureKelvin < 7500) _starType = StarType.FType;
        // (7500 - 10000 K)
        else if (_temperatureKelvin >= 7500 && _temperatureKelvin < 10000) _starType = StarType.AType;
        // (10000 - 30000 K)
        else if (_temperatureKelvin >= 10000 && _temperatureKelvin < 30000) _starType = StarType.BType;
        // (30000+ K)
        else if (_temperatureKelvin >= 30000) _starType = StarType.OType;
    }

    void InitializeSolarFlares()
    {
        InitTempSolarFlaresColor(_starType);

        switch (_starType)
        {
            case StarType.MType:
            case StarType.KType:
                break;
            case StarType.GType:
                InitializeSolarFlaresContinuous(_continuousEffect: _solarFlaresContinuousSmall1, sunRadius: 0.505f, ejectionSpeed: 0.001f, lifetime: 2f, drag: 500f, particleSize1: 0.01f, particleSize2: 0.003f, particleSize3: 0.001f, tangentStrength: 0.1f, curlFrequency: 5f, curlStrength: 0.01f, curlSpeed: 0.001f, stretchAmount: 0.1f, stretchBase: 1f, spawnRate1: 500f, spawnRate2: 1000f, spawnRate3: 250f, loopDuration: 0.001f, particleSize1Weight: 0.1f, particleSize2Weight: 0.4f, particleSize3Weight: 0.5f, flareColor1Weight: 0.45f, flareColor2Weight: 0.1f, flareColor3Weight: 0.45f, spawnRate1Weight: 0.33f, spawnRate2Weight: 0.33f, spawnRate3Weight: 0.33f,
                                                flareColor1: new Color(95.608f, 17.647f, 2.004f, 1f), flareColor2: new Color(191.75f, 64.25f, 0f, 1f), flareColor3: new Color(95.69f, 6.02f, 2.01f, 1f));
                InitializeSolarFlaresContinuous(_continuousEffect: _solarFlaresContinuousSmall2, sunRadius: 0.4975f, ejectionSpeed: 0.0001f, lifetime: 6f, drag: 1000f, particleSize1: 0.01f, particleSize2: 0.007f, particleSize3: 0.003f, tangentStrength: 1f, curlFrequency: 0.05f, curlStrength: 0.01f, curlSpeed: 0.1f, stretchAmount: 0.5f, stretchBase: 1f, spawnRate1: 3000f, spawnRate2: 3000f, spawnRate3: 3000f, loopDuration: 1e-05f, particleSize1Weight: 0.2f, particleSize2Weight: 0.4f, particleSize3Weight: 0.4f, flareColor1Weight: 0.33f, flareColor2Weight: 0.33f, flareColor3Weight: 0.33f, spawnRate1Weight: 0.33f, spawnRate2Weight: 0.33f, spawnRate3Weight: 0.33f,
                                                flareColor1: new Color(383.50f, 12.04f, 0f, 1f), flareColor2: new Color(767.04f, 24.09f, 0f, 1f), flareColor3: new Color(767.04f, 24.09f, 0f, 1f));
                InitializeSolarFlaresBurst(_burstEffect: _solarFlaresBurstMedium1, sunRadius: 0.5f, burstCount: 3, burstInterval: 1f, ejectionSpeed: 0.05f, lifetime: 3f, drag: 250f, particleSize: 0.09f, tangentStrength: 0.1f, curlFrequency: 0.01f, curlStrength: 0.01f, curlSpeed: 0.01f, stretchAmount: 1f, stretchBase: 1f, flipbookFPS: 6f,
                                            flareColor: new Color(1.0f, 0.40f, 0f, 0f));
                InitializeSolarFlaresBurst(_burstEffect: _solarFlaresBurstMedium2, sunRadius: 0.5f, burstCount: 3, burstInterval: 6f, ejectionSpeed: 0.001f, lifetime: 6f, drag: 1000f, particleSize: 0.085f, tangentStrength: 0.1f, curlFrequency: 0.01f, curlStrength: 0.1f, curlSpeed: 0.01f, stretchAmount: 1f, stretchBase: 1f, flipbookFPS: 4f,
                                            flareColor: new Color(5.99f, 2.42f, 0f, 0f));
                InitializeSolarFlaresBurst(_burstEffect: _solarFlaresBurstMedium3, sunRadius: 0.5f, burstCount: 4, burstInterval: 10f, ejectionSpeed: 0.001f, lifetime: 10f, drag: 2000f, particleSize: 0.085f, tangentStrength: 0.1f, curlFrequency: 0.01f, curlStrength: 0.1f, curlSpeed: 0.01f, stretchAmount: 1f, stretchBase: 1f, flipbookFPS: 4f,
                                            flareColor: new Color(47.94f, 19.33f, 0f, 0f));
                InitializeSolarFlaresBurst(_burstEffect: _solarFlaresBurstBig1, sunRadius: 0.52f, burstCount: 1, burstInterval: 8f, ejectionSpeed: 0.0001f, lifetime: 8f, drag: 5000f, particleSize: 0.15f, tangentStrength: 0.1f, curlFrequency: 0.2f, curlStrength: 0.1f, curlSpeed: 0.1f, stretchAmount: 1f, stretchBase: 1f, flipbookFPS: 3f,
                                            flareColor: new Color(3.00f, 2.98f, 2.98f, 0f));
                InitializeSolarFlaresBurst(_burstEffect: _solarFlaresBurstBig2, sunRadius: 0.498f, burstCount: 1, burstInterval: 8f, ejectionSpeed: 1e-06f, lifetime: 10f, drag: 6000f, particleSize: 0.25f, tangentStrength: 0.001f, curlFrequency: 0.1f, curlStrength: 0.01f, curlSpeed: 0.1f, stretchAmount: 1f, stretchBase: 1f, flipbookFPS: 3f,
                                            flareColor: new Color(0.749f, 0.745f, 0.745f, 0f));
                InitializeSolarFlaresBurst(_burstEffect: _solarFlaresBurstBig3, sunRadius: 0.48f, burstCount: 3, burstInterval: 20f, ejectionSpeed: 0.0001f, lifetime: 20f, drag: 7000f, particleSize: 0.15f, tangentStrength: 0f, curlFrequency: 0f, curlStrength: 0.0001f, curlSpeed: 0f, stretchAmount: 1f, stretchBase: 1f, flipbookFPS: 3f,
                                            flareColor: new Color(5.99f, 5.96f, 5.96f, 0f));
                InitializeSolarFlaresBurst(_burstEffect: _solarFlaresBurstBig4, sunRadius: 0.48f, burstCount: 2, burstInterval: 20f, ejectionSpeed: 1f, lifetime: 30f, drag: 10000f, particleSize: 0.25f, tangentStrength: 1e-05f, curlFrequency: 1e-05f, curlStrength: 1e-05f, curlSpeed: 1e-05f, stretchAmount: 1f, stretchBase: 1.25f, flipbookFPS: 2f,
                                            flareColor: new Color(0.376f, 0.373f, 0.373f, 0f));
                break;
            case StarType.FType:
            case StarType.AType:
            case StarType.BType:
            case StarType.OType:
                break;
        }
    }

    void InitTempSolarFlaresColor(StarType starType)
    {
        switch (starType)
        {
            case StarType.MType:
                {
                    _solarFlaresContinuousSmall1.SetVector4("FlareColor1", new(13.056f, 0f, 0f, 1f));
                    _solarFlaresContinuousSmall1.SetVector4("FlareColor2", new(26.112f, 0f, 0f, 1f));
                    _solarFlaresContinuousSmall1.SetVector4("FlareColor3", new(13.056f, 0f, 0f, 1f));

                    _solarFlaresContinuousSmall2.SetVector4("FlareColor1", new(52.224f, 0f, 0f, 1f));
                    _solarFlaresContinuousSmall2.SetVector4("FlareColor2", new(104.448f, 0f, 0f, 1f));
                    _solarFlaresContinuousSmall2.SetVector4("FlareColor3", new(104.448f, 0f, 0f, 1f));

                    _solarFlaresBurstMedium1.SetVector4("FlareColor", new(0.102f, 0f, 0f, 1f));
                    _solarFlaresBurstMedium2.SetVector4("FlareColor", new(0.816f, 0f, 0f, 1f));
                    _solarFlaresBurstMedium3.SetVector4("FlareColor", new(6.528f, 0f, 0f, 1f));

                    _solarFlaresBurstBig1.SetVector4("FlareColor", new(0.102f, 0f, 0f, 1f));
                    _solarFlaresBurstBig2.SetVector4("FlareColor", new(0.102f, 0f, 0f, 1f));
                    _solarFlaresBurstBig3.SetVector4("FlareColor", new(0.816f, 0f, 0f, 1f));
                    _solarFlaresBurstBig4.SetVector4("FlareColor", new(0.102f, 0f, 0f, 1f));

                    break;
                }

            case StarType.KType:
                {
                    _solarFlaresContinuousSmall1.SetVector4("FlareColor1", new(21.76f, 0f, 0f, 1f));
                    _solarFlaresContinuousSmall1.SetVector4("FlareColor2", new(43.52f, 0f, 0f, 1f));
                    _solarFlaresContinuousSmall1.SetVector4("FlareColor3", new(21.76f, 0f, 0f, 1f));

                    _solarFlaresContinuousSmall2.SetVector4("FlareColor1", new(87.04f, 0f, 0f, 1f));
                    _solarFlaresContinuousSmall2.SetVector4("FlareColor2", new(174.08f, 0f, 0f, 1f));
                    _solarFlaresContinuousSmall2.SetVector4("FlareColor3", new(174.08f, 0f, 0f, 1f));

                    _solarFlaresBurstMedium1.SetVector4("FlareColor", new(0.17f, 0f, 0f, 1f));
                    _solarFlaresBurstMedium2.SetVector4("FlareColor", new(1.36f, 0f, 0f, 1f));
                    _solarFlaresBurstMedium3.SetVector4("FlareColor", new(10.88f, 0f, 0f, 1f));

                    _solarFlaresBurstBig1.SetVector4("FlareColor", new(0.17f, 0f, 0f, 1f));
                    _solarFlaresBurstBig2.SetVector4("FlareColor", new(0.17f, 0f, 0f, 1f));
                    _solarFlaresBurstBig3.SetVector4("FlareColor", new(1.36f, 0f, 0f, 1f));
                    _solarFlaresBurstBig4.SetVector4("FlareColor", new(0.17f, 0f, 0f, 1f));

                    break;
                }

            case StarType.GType:
                {
                    break;
                }

            case StarType.FType:
                {
                    _solarFlaresContinuousSmall1.SetVector4("FlareColor1", new(102.4f, 16.64f, 0f, 1f));
                    _solarFlaresContinuousSmall1.SetVector4("FlareColor2", new(204.8f, 33.28f, 0f, 1f));
                    _solarFlaresContinuousSmall1.SetVector4("FlareColor3", new(102.4f, 16.64f, 0f, 1f));

                    _solarFlaresContinuousSmall2.SetVector4("FlareColor1", new(409.6f, 66.56f, 0f, 1f));
                    _solarFlaresContinuousSmall2.SetVector4("FlareColor2", new(819.2f, 133.12f, 0f, 1f));
                    _solarFlaresContinuousSmall2.SetVector4("FlareColor3", new(819.2f, 133.12f, 0f, 1f));

                    _solarFlaresBurstMedium1.SetVector4("FlareColor", new(0.8f, 0.13f, 0f, 1f));
                    _solarFlaresBurstMedium2.SetVector4("FlareColor", new(6.4f, 1.04f, 0f, 1f));
                    _solarFlaresBurstMedium3.SetVector4("FlareColor", new(51.2f, 8.32f, 0f, 1f));

                    _solarFlaresBurstBig1.SetVector4("FlareColor", new(0.8f, 0.13f, 0f, 1f));
                    _solarFlaresBurstBig2.SetVector4("FlareColor", new(0.8f, 0.13f, 0f, 1f));
                    _solarFlaresBurstBig3.SetVector4("FlareColor", new(6.4f, 1.04f, 0f, 1f));
                    _solarFlaresBurstBig4.SetVector4("FlareColor", new(0.8f, 0.13f, 0f, 1f));

                    break;
                }

            case StarType.AType:
                {
                    _solarFlaresContinuousSmall1.SetVector4("FlareColor1", new(128f, 42.24f, 0f, 1f));
                    _solarFlaresContinuousSmall1.SetVector4("FlareColor2", new(256f, 84.48f, 0f, 1f));
                    _solarFlaresContinuousSmall1.SetVector4("FlareColor3", new(128f, 42.24f, 0f, 1f));

                    _solarFlaresContinuousSmall2.SetVector4("FlareColor1", new(512f, 168.96f, 0f, 1f));
                    _solarFlaresContinuousSmall2.SetVector4("FlareColor2", new(1024f, 337.92f, 0f, 1f));
                    _solarFlaresContinuousSmall2.SetVector4("FlareColor3", new(1024f, 337.92f, 0f, 1f));

                    _solarFlaresBurstMedium1.SetVector4("FlareColor", new(1f, 0.33f, 0f, 1f));
                    _solarFlaresBurstMedium2.SetVector4("FlareColor", new(8f, 2.64f, 0f, 1f));
                    _solarFlaresBurstMedium3.SetVector4("FlareColor", new(64f, 21.12f, 0f, 1f));

                    _solarFlaresBurstBig1.SetVector4("FlareColor", new(1f, 0.33f, 0f, 1f));
                    _solarFlaresBurstBig2.SetVector4("FlareColor", new(1f, 0.33f, 0f, 1f));
                    _solarFlaresBurstBig3.SetVector4("FlareColor", new(8f, 2.64f, 0f, 1f));
                    _solarFlaresBurstBig4.SetVector4("FlareColor", new(1f, 0.33f, 0f, 1f));

                    break;
                }

            case StarType.BType:
                {
                    _solarFlaresContinuousSmall1.SetVector4("FlareColor1", new(128f, 102.4f, 34.56f, 1f));
                    _solarFlaresContinuousSmall1.SetVector4("FlareColor2", new(256f, 204.8f, 69.12f, 1f));
                    _solarFlaresContinuousSmall1.SetVector4("FlareColor3", new(128f, 102.4f, 34.56f, 1f));

                    _solarFlaresContinuousSmall2.SetVector4("FlareColor1", new(512f, 409.6f, 138.24f, 1f));
                    _solarFlaresContinuousSmall2.SetVector4("FlareColor2", new(1024f, 819.2f, 276.48f, 1f));
                    _solarFlaresContinuousSmall2.SetVector4("FlareColor3", new(1024f, 819.2f, 276.48f, 1f));

                    _solarFlaresBurstMedium1.SetVector4("FlareColor", new(1f, 0.8f, 0.27f, 1f));
                    _solarFlaresBurstMedium2.SetVector4("FlareColor", new(8f, 6.4f, 2.16f, 1f));
                    _solarFlaresBurstMedium3.SetVector4("FlareColor", new(64f, 51.2f, 17.28f, 1f));

                    _solarFlaresBurstBig1.SetVector4("FlareColor", new(1f, 0.8f, 0.27f, 1f));
                    _solarFlaresBurstBig2.SetVector4("FlareColor", new(1f, 0.8f, 0.27f, 1f));
                    _solarFlaresBurstBig3.SetVector4("FlareColor", new(8f, 6.4f, 2.16f, 1f));
                    _solarFlaresBurstBig4.SetVector4("FlareColor", new(1f, 0.8f, 0.27f, 1f));

                    break;
                }

            case StarType.OType:
                {
                    _solarFlaresContinuousSmall1.SetVector4("FlareColor1", new(119.04f, 119.04f, 128f, 1f));
                    _solarFlaresContinuousSmall1.SetVector4("FlareColor2", new(238.08f, 238.08f, 256f, 1f));
                    _solarFlaresContinuousSmall1.SetVector4("FlareColor3", new(119.04f, 119.04f, 128f, 1f));

                    _solarFlaresContinuousSmall2.SetVector4("FlareColor1", new(476.16f, 476.16f, 512f, 1f));
                    _solarFlaresContinuousSmall2.SetVector4("FlareColor2", new(952.32f, 952.32f, 1024f, 1f));
                    _solarFlaresContinuousSmall2.SetVector4("FlareColor3", new(952.32f, 952.32f, 1024f, 1f));

                    _solarFlaresBurstMedium1.SetVector4("FlareColor", new(0.93f, 0.93f, 1f, 1f));
                    _solarFlaresBurstMedium2.SetVector4("FlareColor", new(7.44f, 7.44f, 8f, 1f));
                    _solarFlaresBurstMedium3.SetVector4("FlareColor", new(59.52f, 59.52f, 64f, 1f));

                    _solarFlaresBurstBig1.SetVector4("FlareColor", new(0.93f, 0.93f, 1f, 1f));
                    _solarFlaresBurstBig2.SetVector4("FlareColor", new(0.93f, 0.93f, 1f, 1f));
                    _solarFlaresBurstBig3.SetVector4("FlareColor", new(7.44f, 7.44f, 8f, 1f));
                    _solarFlaresBurstBig4.SetVector4("FlareColor", new(0.93f, 0.93f, 1f, 1f));

                    break;
                }
        }
    }

    void DisableSolarFlare(VisualEffect _solarFlareEffect)
    {
        if (_solarFlareEffect == null) return;

        _solarFlareEffect.gameObject.SetActive(false);
    }

    void InitializeSolarFlaresBurst(VisualEffect _burstEffect, float sunRadius, int burstCount, float burstInterval, float ejectionSpeed, float lifetime, float drag, float particleSize, Color flareColor, float tangentStrength, float curlFrequency, float curlStrength, float curlSpeed, float stretchAmount, float stretchBase, float flipbookFPS)
    {
        _burstEffect.SetFloat("SunRadius", sunRadius);
        _burstEffect.SetInt("BurstCount", burstCount);
        _burstEffect.SetFloat("BurstInterval", burstInterval);
        _burstEffect.SetFloat("EjectionSpeed", ejectionSpeed);
        _burstEffect.SetFloat("Lifetime", lifetime);
        _burstEffect.SetFloat("Drag", drag);

        _burstEffect.SetFloat("ParticleSize", particleSize);

        _burstEffect.SetVector4("FlareColor", flareColor);

        _burstEffect.SetFloat("TangentStrength", tangentStrength);
        _burstEffect.SetFloat("CurlFrequency", curlFrequency);
        _burstEffect.SetFloat("CurlStrength", curlStrength);
        _burstEffect.SetFloat("CurlSpeed", curlSpeed);

        _burstEffect.SetFloat("StretchAmount", stretchAmount);
        _burstEffect.SetFloat("StretchBase", stretchBase);

        _burstEffect.SetFloat("FlipbookFPS", flipbookFPS);
    }

    void InitializeSolarFlaresContinuous(VisualEffect _continuousEffect, float sunRadius, float ejectionSpeed, float lifetime, float drag, float particleSize1, float particleSize2, float particleSize3, Color flareColor1, Color flareColor2, Color flareColor3, float tangentStrength, float curlFrequency, float curlStrength, float curlSpeed, float stretchAmount, float stretchBase, float spawnRate1, float spawnRate2, float spawnRate3, float loopDuration, float particleSize1Weight, float particleSize2Weight, float particleSize3Weight, float flareColor1Weight, float flareColor2Weight, float flareColor3Weight, float spawnRate1Weight, float spawnRate2Weight, float spawnRate3Weight)
    {
        _continuousEffect.SetFloat("SunRadius", sunRadius);
        _continuousEffect.SetFloat("EjectionSpeed", ejectionSpeed);
        _continuousEffect.SetFloat("Lifetime", lifetime);
        _continuousEffect.SetFloat("Drag", drag);

        _continuousEffect.SetFloat("ParticleSize1", particleSize1);
        _continuousEffect.SetFloat("ParticleSize2", particleSize2);
        _continuousEffect.SetFloat("ParticleSize3", particleSize3);

        _continuousEffect.SetVector4("FlareColor1", flareColor1);
        _continuousEffect.SetVector4("FlareColor2", flareColor2);
        _continuousEffect.SetVector4("FlareColor3", flareColor3);

        _continuousEffect.SetFloat("TangentStrength", tangentStrength);
        _continuousEffect.SetFloat("CurlFrequency", curlFrequency);
        _continuousEffect.SetFloat("CurlStrength", curlStrength);
        _continuousEffect.SetFloat("CurlSpeed", curlSpeed);

        _continuousEffect.SetFloat("StretchAmount", stretchAmount);
        _continuousEffect.SetFloat("StretchBase", stretchBase);

        _continuousEffect.SetFloat("SpawnRate1", spawnRate1);
        _continuousEffect.SetFloat("SpawnRate2", spawnRate2);
        _continuousEffect.SetFloat("SpawnRate3", spawnRate3);

        _continuousEffect.SetFloat("LoopDuration", loopDuration);

        _continuousEffect.SetFloat("ParticleSize1 Weight", particleSize1Weight);
        _continuousEffect.SetFloat("ParticleSize2 Weight", particleSize2Weight);
        _continuousEffect.SetFloat("ParticleSize3 Weight", particleSize3Weight);

        _continuousEffect.SetFloat("FlareColor1 Weight", flareColor1Weight);
        _continuousEffect.SetFloat("FlareColor2 Weight", flareColor2Weight);
        _continuousEffect.SetFloat("FlareColor3 Weight", flareColor3Weight);

        _continuousEffect.SetFloat("SpawnRate1 Weight", spawnRate1Weight);
        _continuousEffect.SetFloat("SpawnRate2 Weight", spawnRate2Weight);
        _continuousEffect.SetFloat("SpawnRate3 Weight", spawnRate3Weight);
    }

    void InitializeColors()
    {
        // stopped here, will need to adjust these based on the star type and temperature for better accuracy
        _nearMaxColor = new(0.8352941176470589f, 0.8745098039215686f, 1);

        // (2500 - 3800 K) M-Type
        if (_temperatureKelvin > 2500 && _temperatureKelvin < 3800)
        {
            _nearCoolColor = new(0.102f, 0, 0);
            _nearHotColor = new(1, 0.102f, 0);
        }
        // (3800 - 5200 K) K-Type
        else if (_temperatureKelvin >= 3800 && _temperatureKelvin < 5200)
        {
            _nearCoolColor = new(0.17f, 0, 0);
            _nearHotColor = new(1, 0.15f, 0);
        }
        // (5200 - 6000 K) Sun-like appearance G-Type
        else if (_temperatureKelvin >= 5200 && _temperatureKelvin < 6000)
        {
            _nearCoolColor = new(1, 0.063f, 0);
            _nearHotColor = new(1, 1, 0.59f);
        }
        // (6000 - 7500 K) F-Type
        else if (_temperatureKelvin >= 6000 && _temperatureKelvin < 7500)
        {
            _nearCoolColor = new(0.8f, 0.13f, 0);
            _nearHotColor = new(1, 0.4f, 0);
        }
        // (7500 - 10000 K) A-Type
        else if (_temperatureKelvin >= 7500 && _temperatureKelvin < 10000)
        {
            _nearCoolColor = new(1, 0.33f, 0);
            _nearHotColor = new(1, 0.67f, 0);
        }
        // (10000 - 30000 K) B-Type
        else if (_temperatureKelvin >= 10000 && _temperatureKelvin < 30000)
        {
            _nearCoolColor = new(1, 0.8f, 0.27f);
            _nearHotColor = new(1, 0.93f, 0.67f);
        }
        // (30000+ K) O-Type
        // Hot O‐ and B‐type stars (T_eff ≳30,000 K) emit extremely strong UV continua
        //. In a 304 Å image, an O‐star’s disk would likely appear nearly uniformly bright orange-white, with few discernible dark or colored features. Massive stars have very weak solar‐like magnetic activity (few surface spots or plages)
        //, so the patchy bright regions of the Sun would be largely absent. Any cool sunspot analogs would vanish in 304 Å – the star’s entire visible surface would glow strongly. B‐type and A‐type stars (T_eff ≈10,000–20,000 K) are cooler than O‐stars but still emit substantial UV; their 304 Å images would be similarly bright and smooth. In short, dark spots or filaments would disappear or become negligible, and the “orange background” would be uniformly intense. Flares on O/B/A stars (driven by wind shocks or rare magnetic events) would be comparatively insignificant in contrast to the bright disk.
        else if (_temperatureKelvin >= 30000)
        {
            _nearCoolColor = new(0.93f, 0.93f, 1f);
            _nearHotColor = Color.white;
        }
    }

    void OnEnable()
    {
        if (_isDisplay) return;
        MovementController.Instance.OnSpeedChanged += OnSpeedChanged;
    }

    void OnDisable()
    {
        if (_isDisplay) return;
        MovementController.Instance.OnSpeedChanged -= OnSpeedChanged;
    }

    void LateUpdate()
    {
        if (_isDisplay) return;

        double3 playerPos = _playerObject.GetGlobalPosition();
        double3 sunPos = _sunObject.GetGlobalPosition();
        double distD = math.distance(playerPos, sunPos);
        float dist = (float)distD;
        float distKM = (float)PhysicsConstants.ToKMFromUnityUnits(dist);

        _minMaxBoundOffset = CalculateScreenBoundsOffset(distKM);

        SunState nextState = ResolveState(distKM);
        if (nextState != _currentState) ForceState(nextState);

        bool inView = IsObjectInView(_sunObject.transform, _minMaxBoundOffset, out Vector3 sunViewPos);

        switch (_currentState)
        {
            case SunState.Near:
                UpdateNear(distKM, nearAlpha: 1f);
                break;

            case SunState.Blend:
                float blendDenominator = Mathf.Max(1f, _switchDist - _blendStart);
                float blendT = Mathf.Clamp01((distKM - _blendStart) / blendDenominator);
                UpdateNear(distKM, nearAlpha: 1f - blendT);
                RepositionAndScaleFar(playerPos, sunPos, distKM);
                InViewHandler(inView, sunViewPos, distKM);
                OutOfViewHandler(inView);
                break;

            case SunState.Far:
                RepositionAndScaleFar(playerPos, sunPos, distKM);
                InViewHandler(inView, sunViewPos, distKM);
                OutOfViewHandler(inView);
                break;
        }

        // Debug.Log($"DistKM: {distKM}");

        // float epsilonUnity = (float)PhysicsConstants.ToUnityUnitsFromKM(10000f);
        // float distanceToSaturnUnity = (float)PhysicsConstants.ToUnityUnitsFromKM(1.4e9f);
        // Debug.Log($"Distance To Saturn: {distanceToSaturnUnity - dist:E2} KM");
        // Debug.Log($"Is at Saturn: {distanceToSaturnUnity - dist < epsilonUnity}");
    }

    void OnSpeedChanged(float beforeSpeed, float newSpeed)
    {
        _currentSpeedKM = newSpeed > 0f ? newSpeed : _referenceSpeedKM;
        RecomputeScaledDistances();

        if (_debugLogs)
            Debug.Log($"[SunRendering] Speed changed: {beforeSpeed:E2} → {newSpeed:E2} KM/s  ratio={_currentSpeedKM / _referenceSpeedKM:F4}");
    }

    void RecomputeScaledDistances()
    {
        float speedRatio = _currentSpeedKM / _referenceSpeedKM;

        // Core near/far transition distances — scale with size, NOT speed
        _switchDist = _switchDistanceKM * _sizeRatio;
        _blendStart = Mathf.Max(0f, _switchDist - _blendZoneKM * _sizeRatio);
        _hysteresisDist = _switchDist - _hysteresisKM * _sizeRatio;

        // Speed-scaled AND size-scaled visual effect distances
        _colorWhiteDist = _colorWhiteDistanceKM * speedRatio * _sizeRatio;
        _farScaleMaxDist = _farScaleMaxDistanceKM * speedRatio * _sizeRatio;
        _flareExposureFullDist = _flareExposureFullDistanceKM * speedRatio * _sizeRatio;
        _chromAberrationMaxAlphaDist = _chromAberrationMaxAlphaDistanceKM * speedRatio * _sizeRatio;
        _farSpikesMaxAlphaDist = _spikesMaxAlphaDistanceKM * speedRatio * _sizeRatio;
        _farStreakMaxAlphaDist = _streakMaxAlphaDistanceKM * speedRatio * _sizeRatio;
        _lensOrbsMaxAlphaDist = _lensOrbsMaxAlphaDistanceKM * speedRatio * _sizeRatio;
        _completeStartFadeDist = _completeFadeStartDistanceKM * speedRatio * _sizeRatio;
        _completeEndFadeDist = _completeFadeEndDistanceKM * speedRatio * _sizeRatio;

        // Size-only lens effect distances (non speed-dependent)
        _chromAberrationInitialDistScaled = _chromAberrationInitialDistance * _sizeRatio;
        _chromAberrationDistThresholdScaled = _chromAberrationDistanceThreshold * _sizeRatio;
        _lensOrbsInitialDistScaled = _lensOrbsInitialDistance * _sizeRatio;
        _lensOrbsDistThresholdScaled = _lensOrbsDistanceThreshold * _sizeRatio;

        // Safety clamps
        _spikesDistRange = Mathf.Max(1f, _farSpikesMaxAlphaDist - _blendStart);
        _streakDistRange = Mathf.Max(1f, _farStreakMaxAlphaDist - _blendStart);
        _chromAberrationDistRange = Mathf.Max(1f, _chromAberrationMaxAlphaDist - _blendStart);
        _lensOrbsDistRange = Mathf.Max(1f, _lensOrbsMaxAlphaDist - _blendStart);
    }


    void UpdateSpikesColor(float distKM, float chebyshev, float distAlphaFactor)
    {
        float spikesDistT = Mathf.Clamp01((distKM - _blendStart) / _spikesDistRange);
        float spikesAlpha = 0f;
        if (_currentState != SunState.Near)
        {
            float viewportAlphaFactor = Mathf.Lerp(1f, 0f, Mathf.InverseLerp(0f, 0.5f, chebyshev));
            spikesAlpha = ((_spikesMaxAlpha * spikesDistT) * viewportAlphaFactor) * distAlphaFactor;
        }
        if (Mathf.Approximately(_lastSpikesAlpha, spikesAlpha)) return;
        _lastSpikesAlpha = spikesAlpha;
        _spikesMat.SetColor(UnlitColorId, new Color(1f, 1f, 1f, spikesAlpha));
    }

    void UpdateStreakColor(float distKM, float chebyshev, float distAlphaFactor)
    {
        float streakDistT = Mathf.Clamp01((distKM - _blendStart) / _streakDistRange);
        float streakAlpha = 0f;
        if (_currentState != SunState.Near)
        {
            float viewportAlphaFactor = Mathf.Lerp(1f, 0f, Mathf.InverseLerp(0f, 0.5f, chebyshev));
            streakAlpha = ((_streakMaxAlpha * streakDistT) * viewportAlphaFactor) * distAlphaFactor;
        }
        if (Mathf.Approximately(_lastStreakAlpha, streakAlpha)) return;
        _lastStreakAlpha = streakAlpha;
        _streakMat.SetColor(UnlitColorId, new Color(1f, 1f, 1f, streakAlpha));
    }

    void UpdateChromaticAberrationRingColor(float distKM, float chebyshev, float distAlphaFactor)
    {
        float chromaticAberrationDistT = Mathf.Clamp01((distKM - _blendStart) / _chromAberrationDistRange);
        float chromaticAberrationAlpha = 0f;
        if (_currentState != SunState.Near)
        {
            float viewportAlphaFactor = Mathf.Lerp(1f, 0f, Mathf.InverseLerp(0f, 0.5f, chebyshev));
            chromaticAberrationAlpha = ((_chromaticAberrationMaxAlpha * chromaticAberrationDistT) * viewportAlphaFactor) * distAlphaFactor;
        }
        if (Mathf.Approximately(_lastChromAberrationAlpha, chromaticAberrationAlpha)) return;
        _lastChromAberrationAlpha = chromaticAberrationAlpha;
        _chromAberrationOuterMat.SetColor(UnlitColorId, new Color(1f, 1f, 1f, chromaticAberrationAlpha));
        _chromAberrationInnerMat.SetColor(UnlitColorId, new Color(1f, 1f, 1f, chromaticAberrationAlpha));
    }

    void UpdateLensOrbsColor(float distKM, float chebyshev, float distAlphaFactor)
    {
        float orbDistT = Mathf.Clamp01((distKM - _blendStart) / _lensOrbsDistRange);
        float orbAlpha = 0f;
        if (_currentState != SunState.Near)
        {
            float viewportAlphaFactor = Mathf.Exp(-Mathf.Pow((chebyshev - 0.25f) / 0.2f, 2f));
            orbAlpha = ((_lensOrbsMaxAlpha * orbDistT) * viewportAlphaFactor) * distAlphaFactor;
        }
        if (Mathf.Approximately(_lastOrbAlpha, orbAlpha)) return;
        _lastOrbAlpha = orbAlpha;
        _orbNearMat.SetColor(UnlitColorId, new Color(0.4588235f, 0.6117647f, 1f, orbAlpha));
        _orbFarMat.SetColor(UnlitColorId, new Color(0.4588235f, 0.6117647f, 1f, orbAlpha * 0.9f));
    }

    void UpdateNear(float distKM, float nearAlpha)
    {
        float tBase = Mathf.Clamp01(distKM / _switchDist);
        float tEmission = Mathf.Pow(tBase, _emissionExponent);

        float tColorBase = _colorWhiteDist > 0f ? Mathf.Clamp01(distKM / _colorWhiteDist) : 1f;
        float tColor = Mathf.Pow(tColorBase, _colorExponent);

        Color coolColor = Color.Lerp(_nearMaxColor, Color.Lerp(_nearCoolColor, _nearMaxColor, tColor), nearAlpha);
        Color hotColor = Color.Lerp(_nearMaxColor, Color.Lerp(_nearHotColor, _nearMaxColor, tColor), nearAlpha);
        float emissionIntensity = Mathf.Lerp(
            _nearMinEmissiveIntensity, _nearMaxEmissiveIntensity, tEmission) * nearAlpha;

        _sunNearRenderer.GetPropertyBlock(_nearMPB, _nearMaterialIndex);
        _nearMPB.SetFloat(EmissionIntensityId, emissionIntensity);
        _nearMPB.SetColor(CoolColorId, coolColor);
        _nearMPB.SetColor(HotColorId, hotColor);
        _sunNearRenderer.SetPropertyBlock(_nearMPB, _nearMaterialIndex);
    }

    void RepositionAndScaleFar(double3 playerPos, double3 sunPos, float distKM)
    {
        double3 dirToSun = math.normalize(sunPos - playerPos);
        Vector3 dir = new((float)dirToSun.x, (float)dirToSun.y, (float)dirToSun.z);

        Vector3 farPosition = _playerCamera.transform.position + dir * _farPlacementDistance;
        _sunFar.transform.position = farPosition;
        _sunFar.transform.LookAt(farPosition + _playerCamera.transform.forward, _playerCamera.transform.up);

        double scaleMultiplier;
        if (_currentState == SunState.Blend) scaleMultiplier = _farScaleMin;
        else
        {
            double distBeyondSwitch = math.max(0.0, distKM - (double)_switchDist);
            double scaleDistance = math.max(1.0, _farScaleMaxDist - _switchDist);
            double scaleT = math.pow(math.clamp(distBeyondSwitch / scaleDistance, 0.0, 1.0), _farScaleExponent);
            scaleMultiplier = math.lerp(_farScaleMin, _farScaleMax, scaleT);
        }

        double sunRadiusKM = PhysicsConstants.ToKMFromUnityUnits(_sunRadiusUnity);
        double apparentScale = (sunRadiusKM * 2.0 / distKM) * _farPlacementDistance * scaleMultiplier;

        _sunFar.transform.localScale = Vector3.one * (float)apparentScale;
    }

    void UpdateChromAberrationPosition(Vector2 sunViewPos)
    {
        float ringX = -0.3f * sunViewPos.x + 0.15f;
        float ringY = -0.4f * sunViewPos.y + 0.2f;

        Vector2 newOuterRingPosition = new(ringX, ringY);

        _sunChromAberrationOuterRenderer.transform.localPosition = newOuterRingPosition;
        _sunChromAberrationInnerRenderer.transform.localPosition = newOuterRingPosition * 0.95f;
    }

    void UpdateLensOrbsPosition(Vector2 sunViewportPos, float distKM)
    {
        float ky = 0.000451f * 1.75f / 14959.787f;
        float kx = ky * _playerCamera.aspect; // ~1.584x

        float localX = distKM * (0.5f - sunViewportPos.x) * kx;
        float localY = distKM * (0.5f - sunViewportPos.y) * ky;

        Vector2 orbPos = new(localX, localY);
        Vector2 orbPosFar = (orbPos * 4.5f) * 1.05f;
        Vector2 orbPosNear = (orbPos * 4.5f) * 0.85f; // near orb positioned closer to Sun

        _sunLensOrbFar.transform.localPosition = orbPosFar;
        _sunLensOrbNear.transform.localPosition = orbPosNear;
    }

    void UpdateLensEffectsSize(float distKM)
    {
        ResizeLensEffect(
            objectToScale: _chromAberrationObj.transform,
            distKM: distKM,
            initScale: _chromAberrationInitialScale,
            initDistance: _chromAberrationInitialDistScaled,      // was _chromAberrationInitialDistance
            distThreshold: _chromAberrationDistThresholdScaled,    // was _chromAberrationDistanceThreshold
            nearScaleDampening: _chromAberrationNearScaleDampening,
            scaleMultiplier: _chromAberrationScaleFactorMultiplier
        );

        ResizeLensEffect(
            objectToScale: _sunLensOrbNear.transform,
            objectToScale2: _sunLensOrbFar.transform,
            distKM: distKM,
            initScale: _lensOrbsInitialScale,
            initDistance: _lensOrbsInitialDistScaled,             // was _lensOrbsInitialDistance
            distThreshold: _lensOrbsDistThresholdScaled,           // was _lensOrbsDistanceThreshold
            nearScaleDampening: _lensOrbsNearScaleDampening,
            scaleMultiplier: _lensOrbsScaleFactorMultiplier
        );
    }

    void ResizeLensEffect(Transform objectToScale, float distKM, float initScale, float initDistance, float distThreshold, float nearScaleDampening, float scaleMultiplier, Transform objectToScale2 = null)
    {
        float scaleFactor = distKM / initDistance;

        if (distKM < distThreshold)
        {
            float t = distKM / distThreshold;

            scaleFactor = Mathf.Pow(t, nearScaleDampening) * (distThreshold / initDistance);
        }

        float finalScale = initScale * scaleFactor * scaleMultiplier;
        objectToScale.localScale = Vector3.one * finalScale;

        if (objectToScale2 != null) objectToScale2.localScale = Vector3.one * finalScale;
    }

    void UpdateLensEffectsColor(float distKM, Vector2 viewportPos)
    {
        Vector2 offset = viewportPos - new Vector2(0.5f, 0.5f);
        float chebyshev = Mathf.Max(Mathf.Abs(offset.x), Mathf.Abs(offset.y));

        float range = Mathf.Max(1f, _completeEndFadeDist - _completeStartFadeDist);

        float distAlphaFactor = (_completeEndFadeDist - distKM) / range;
        distAlphaFactor = Mathf.Clamp01(distAlphaFactor);

        UpdateSpikesColor(distKM, chebyshev, distAlphaFactor);
        UpdateStreakColor(distKM, chebyshev, distAlphaFactor);
        UpdateLensOrbsColor(distKM, chebyshev, distAlphaFactor);
        UpdateChromaticAberrationRingColor(distKM, chebyshev, distAlphaFactor);
    }


    void UpdateLensEffectsPosition(Vector2 viewportPos, float distKM)
    {
        UpdateLensOrbsPosition(viewportPos, distKM);
        UpdateChromAberrationPosition(viewportPos);
    }

    void InViewHandler(bool isObjectInView, Vector2 viewportPos, float distKM)
    {
        if (isObjectInView)
        {
            UpdateLensEffectsColor(distKM, viewportPos);
            UpdateLensEffectsPosition(viewportPos, distKM);
            UpdateLensEffectsSize(distKM);
        }
    }

    void OutOfViewHandler(bool isObjectInView)
    {
        if (!isObjectInView)
        {
            _lastSpikesAlpha = -1f;
            _lastStreakAlpha = -1f;
            _lastChromAberrationAlpha = -1f;
            _lastOrbAlpha = -1f;

            if (_chromAberrationObj.activeSelf) _chromAberrationObj.SetActive(false);
            if (_streaksObj.activeSelf) _streaksObj.SetActive(false);
            if (_spikesObj.activeSelf) _spikesObj.SetActive(false);
            if (_sunLensOrbNear.activeSelf) _sunLensOrbNear.SetActive(false);
            if (_sunLensOrbFar.activeSelf) _sunLensOrbFar.SetActive(false);
        }
        else
        {
            if (!_chromAberrationObj.activeSelf) _chromAberrationObj.SetActive(true);
            if (!_streaksObj.activeSelf) _streaksObj.SetActive(true);
            if (!_spikesObj.activeSelf) _spikesObj.SetActive(true);
            if (!_sunLensOrbNear.activeSelf) _sunLensOrbNear.SetActive(true);
            if (!_sunLensOrbFar.activeSelf) _sunLensOrbFar.SetActive(true);
        }
    }

    bool IsObjectInView(Transform objectWorldPosition, float _minMaxBoundOffset, out Vector3 viewportPos)
    {
        viewportPos = _playerCamera.WorldToViewportPoint(objectWorldPosition.position);
        if (viewportPos.x > (0f - _minMaxBoundOffset) && viewportPos.x < (1f + _minMaxBoundOffset) && viewportPos.y > (0f - _minMaxBoundOffset) && viewportPos.y < (1f + _minMaxBoundOffset) && viewportPos.z > 0f)
        {
            return true;
        }
        return false;
    }

    float CalculateScreenBoundsOffset(float distKM)
    {
        if (distKM < 1.0) return 0f;

        return 18000000.0f / distKM;
    }

    SunState ResolveState(float distKM)
    {
        switch (_currentState)
        {
            case SunState.Near:
                if (distKM >= _blendStart) return SunState.Blend;
                break;
            case SunState.Blend:
                if (distKM < _blendStart) return SunState.Near;
                if (distKM >= _switchDist) return SunState.Far;
                break;
            case SunState.Far:
                if (distKM < _hysteresisDist) return SunState.Blend;
                break;
        }
        return _currentState;
    }

    void ForceState(SunState state)
    {
        _currentState = state;
        _sunNear.SetActive(state == SunState.Near || state == SunState.Blend);
        _sunFar.SetActive(state == SunState.Far || state == SunState.Blend);

        if (state == SunState.Far)
        {
            _sunNearRenderer.GetPropertyBlock(_nearMPB, _nearMaterialIndex);
            _nearMPB.SetFloat(EmissionIntensityId, 0f);
            _nearMPB.SetColor(CoolColorId, Color.white);
            _nearMPB.SetColor(HotColorId, Color.white);
            _sunNearRenderer.SetPropertyBlock(_nearMPB, _nearMaterialIndex);
        }
    }

    bool ValidateReferences()
    {
        bool ok = true;
        if (!_playerObject)
        {
            _playerObject = MovementController.Instance;
            if (!_playerObject) { Debug.LogError("[SunRendering] _playerObject is not assigned."); ok = false; }
        }
        if (!_sunObject) { Debug.LogError("[SunRendering] _sunObject missing"); ok = false; }
        if (!_sunAstronomicalObject) { Debug.LogError("[SunRendering] _sunAstronomicalObject missing"); ok = false; }
        if (!_playerCamera)
        {
            _playerCamera = MovementController.Instance.PlayerCamera;
            if (!_playerCamera) { Debug.LogError("[SunRendering] _playerCamera missing and not found on MovementController"); ok = false; }
        }
        if (!_sunNear) { Debug.LogError("[SunRendering] _sunNear missing"); ok = false; }
        if (!_sunFar) { Debug.LogError("[SunRendering] _sunFar missing"); ok = false; }
        if (!_sunFarRenderer) { Debug.LogError("[SunRendering] _sunFarRenderer missing"); ok = false; }
        if (!_sunNearRenderer) { Debug.LogError("[SunRendering] _sunNearRenderer missing"); ok = false; }
        if (!_sunStreakRenderer) { Debug.LogError("[SunRendering] _sunStreakRenderer missing"); ok = false; }
        if (!_sunSpikesRenderer) { Debug.LogError("[SunRendering] _sunSpikesRenderer missing"); ok = false; }
        if (!_sunChromAberrationInnerRenderer) { Debug.LogError("[SunRendering] _sunChromAberrationInnerRenderer missing"); ok = false; }
        if (!_sunChromAberrationOuterRenderer) { Debug.LogError("[SunRendering] _sunChromAberrationOuterRenderer missing"); ok = false; }
        if (_completeFadeStartDistanceKM >= _completeFadeEndDistanceKM)
        {
            Debug.LogError("[SunRendering] _completeFadeStartDistanceKM must be less than _completeFadeEndDistanceKM");
            ok = false;
        }
        return ok;
    }

    enum StarType
    {
        MType,
        KType,
        GType,
        FType,
        AType,
        BType,
        OType
    }
}