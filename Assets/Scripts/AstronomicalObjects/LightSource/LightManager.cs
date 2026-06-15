using UnityEngine;
using Unity.Mathematics;

public class LightManager : MonoBehaviour
{
    [SerializeField] Light _directionalLight;
    [SerializeField] Transform _playerPos; // player/camera
    [SerializeField] AstronomicalObject _star;

    [Header("Intensity Tuning")]
    [SerializeField] float _baseIntensity = 1000f;
    [SerializeField] float _nearDistance = 1f;
    [SerializeField] float _farDistance = 1000000f;
    [SerializeField] AnimationCurve _distanceToIntensity = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);


    void LateUpdate()
    {
        if (_playerPos == null || _star == null || _directionalLight == null) return;

        HandleRotation();
        HandleIntensity();
    }

    void HandleRotation()
    {
        Vector3 direction = (_playerPos.position - _star.transform.position).normalized;
        transform.rotation = Quaternion.LookRotation(direction);
    }

    void HandleIntensity()
    {
        if (RenderSpaceManager.Instance == null) return;

        if (!RenderSpaceManager.Instance.TryGetClosestVisiblePlanet(out AstronomicalObject planet))
        {
            _directionalLight.intensity = _baseIntensity;
            return;
        }

        double distance = math.distance(_star.Position, planet.Position);

        float normalized = Mathf.InverseLerp(_nearDistance, _farDistance, (float)distance);
        float falloff = _distanceToIntensity.Evaluate(normalized);

        float finalIntensity = Mathf.Max(0f, _baseIntensity * falloff);
        _directionalLight.intensity = finalIntensity;
    }

    public void SetTemperatureKelvin(float value)
    {
        if (value < 1500)
        {
            Debug.LogError("Temperature (Kelvin) must be > 1500");
            return;
        }

        if (value > 20000)
        {
            Debug.LogError("Temperature (Kelvin) must be < 20000");
            return;
        }

        if (_directionalLight != null)
        {
            _directionalLight.useColorTemperature = true;
            _directionalLight.colorTemperature = value;
        }
    }

    public void Initialize(AstronomicalObject starObject)
    {
        if (starObject == null) return;

        if (MovementController.Instance != null) _playerPos = MovementController.Instance.transform;

        starObject.SetLightSource(this);
        _star = starObject;

        if (_directionalLight != null) _directionalLight.useColorTemperature = true;
    }
}