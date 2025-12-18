using UnityEngine;
using Unity.Mathematics;

public class AstronomicalObject : MonoBehaviour
{
    public string Name;
    public double MassKg;

    public double3 Velocity;

    public double3 Position;

    public bool Initialized = false;
    bool _sunInitialized = false;
    bool _earthInitialized = false;


    void Awake()
    {
        Position = (double3)(float3)transform.position; // initial sync from scene
        Velocity = double3.zero;

        if (Name == "Sun" && !Initialized && !_sunInitialized)
        {
            Position = new double3(0, 0, 0);

            Velocity = new double3(0, 0, 0);

            float sunDiameterUnity = (float)PhysicsConstants.ToUnityUnitsFromM(PhysicsConstants.REAL_SUN_DIAMETER_M);
            transform.localScale = Vector3.one * sunDiameterUnity;

            MassKg = PhysicsConstants.REAL_SOLAR_MASS_KG;

            UpdateVisualPosition();
            _sunInitialized = true;
        }
        else if (Name == "Earth" && !Initialized && !_earthInitialized)
        {
            double unityEarthToSun = PhysicsConstants.ToUnityUnitsFromAU(PhysicsConstants.REAL_EARTH_SUN_DISTANCE_AU);
            Position = new double3(unityEarthToSun, 0, 0);

            double GM = PhysicsConstants.UNITY_G * PhysicsConstants.REAL_SOLAR_MASS_KG;
            double v = math.sqrt(GM / unityEarthToSun); // UnityUnits / day
            Velocity = new double3(0.0, 0.0, v);

            float earthDiameterUnity = (float)PhysicsConstants.ToUnityUnitsFromM(PhysicsConstants.REAL_EARTH_DIAMETER_M);
            transform.localScale = Vector3.one * earthDiameterUnity;

            MassKg = PhysicsConstants.REAL_EARTH_MASS_KG;

            UpdateVisualPosition();
            _earthInitialized = true;
        }
        else if (Name == "Neptune")
        {
            Debug.Log("Entered Neptune statement");
            Debug.Log($"Current {Name} position: {transform.localPosition}");
            Position = new double3(PhysicsConstants.ToUnityUnitsFromAU(PhysicsConstants.REAL_NEPTUNE_SUN_DISTANCE_AU), 0, 0);
            UpdateVisualPosition();
            Debug.Log("Attempting to move 'Neptune' to converted position");
            Debug.Log($"Moved {Name} position: {transform.localPosition}");
        }

        Initialized = true;
    }

    public void UpdateVisualPosition()
    {
        if (!math.all(math.isfinite(Position)))
        {
            Debug.LogError($"{name}: Invalid Position in SyncPosition: {Position}");
            return; // prevents writing NaN into Transform 
        }

        transform.position = (Vector3)(float3)Position;
    }
}

