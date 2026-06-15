using UnityEngine;
using Unity.Mathematics;

public class EarthCloudRotation : MonoBehaviour
{
    [Header("References")]
    [SerializeField] Transform _localCloudTransform;

    [Header("Cloud Rotation")]
    [SerializeField, Min(0.0001f)] double _cloudRotationPeriodHours = 24.0;

    [Tooltip("Initial local Y rotation in DEGREES at simulation day 0. Use this to choose the starting cloud orientation.")]
    [SerializeField] float _initialLocalYDeg = 0f;

    [Tooltip("If enabled, the cloud layer rotates in the opposite direction.")]
    [SerializeField] bool _reverseDirection = false;

    void Awake()
    {
        if (_localCloudTransform == null)
        {
            Debug.LogWarning("[EarthCloudRotation] No '_localCloudTransform' assigned.");
            enabled = false;
            return;
        }

        if (_cloudRotationPeriodHours <= 0.0)
        {
            Debug.LogWarning("[EarthCloudRotation] Cloud rotation period must be greater than 0.");
            enabled = false;
            return;
        }
    }

    void Update()
    {
        if (SimulationSettings.Instance == null)
            return;

        ApplyCloudRotationFromSimDays(SimulationSettings.Instance.SimDays);
    }

    public void SetCloudRotationPeriodHours(double hours)
    {
        if (hours <= 0.0)
        {
            Debug.LogWarning("[EarthCloudRotation] Rotation period must be greater than 0.");
            return;
        }

        _cloudRotationPeriodHours = hours;
    }

    public void SetInitialLocalYDeg(float angleDeg) => _initialLocalYDeg = angleDeg;

    public void SetReverseDirection(bool reverse) => _reverseDirection = reverse;

    void ApplyCloudRotationFromSimDays(double simDays)
    {
        double periodDays = _cloudRotationPeriodHours / 24.0;
        if (periodDays <= 0.0)
            return;

        double normalizedCycles = simDays / periodDays;
        if (_reverseDirection)
            normalizedCycles *= -1.0;

        double angleDeg = _initialLocalYDeg + (normalizedCycles * 360.0);
        angleDeg = math.fmod(angleDeg, 360.0);

        if (angleDeg < 0.0)
            angleDeg += 360.0;

        Vector3 localEuler = _localCloudTransform.localEulerAngles;
        localEuler.y = (float)angleDeg;
        _localCloudTransform.localEulerAngles = localEuler;
    }
}