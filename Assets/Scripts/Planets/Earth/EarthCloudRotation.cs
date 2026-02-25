using UnityEngine;

public class EarthCloudRotation : MonoBehaviour
{
    [SerializeField] Material _cloudMaterial;
    [SerializeField] float _rotationSpeed = 10f;

    void Awake()
    {
        if (_cloudMaterial == null)
        {
            Debug.LogWarning("[EarthCloudRotation] No Material assigned as '_cloudMaterial'");
            enabled = false;
            return;
        }
    }

    void Update()
    {
        // For testing: rotate clouds based on real time
        SimulateRotation(Time.deltaTime * PhysicsConstants.UNITY_DAYS_PER_REAL_SECOND);
    }

    public void SetRotationSpeed(float speed) => _rotationSpeed = speed;

    public void SimulateRotation(double dtDays)
    {
        _cloudMaterial.SetTextureOffset("_BaseColorMap", new Vector2((float)(dtDays * _rotationSpeed * 0.01), 0));
    }
}
