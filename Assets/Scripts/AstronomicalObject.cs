using UnityEngine;
using Unity.Mathematics;
using System;

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

        double mass_Sun = PhysicsConstants.REAL_SOLAR_MASS_KG;
        double mass_Earth = PhysicsConstants.REAL_EARTH_MASS_KG;

        double unityEarthToSun = PhysicsConstants.ToUnityUnitsFromAU(PhysicsConstants.REAL_EARTH_SUN_DISTANCE_AU);

        double3 rVec = new(unityEarthToSun, 0, 0);
        double r = math.length(rVec);

        double totalGMass = PhysicsConstants.UNITY_G * (mass_Sun + mass_Earth);
        double vRel = math.sqrt(totalGMass / r);

        double3 tHat = math.normalize(new double3(0, 0, 1));

        double3 vRelVec = vRel * tHat; // Earth relative to Sun

        double invTot = 1.0 / (mass_Sun + mass_Earth);

        // Positions (relative to barycenter)
        double3 pos_Sun = -mass_Earth * invTot * rVec;
        double3 pos_Earth = mass_Sun * invTot * rVec;

        // Velocities (total momentum = 0)
        double3 vel_Sun = -mass_Earth * invTot * vRelVec;
        double3 vel_Earth = mass_Sun * invTot * vRelVec;

        if (Name == "Sun" && !Initialized && !_sunInitialized)
        {

            float sunDiameterUnity = (float)PhysicsConstants.ToUnityUnitsFromM(PhysicsConstants.REAL_SUN_DIAMETER_M);
            transform.localScale = Vector3.one * sunDiameterUnity;

            Position = pos_Sun;
            Velocity = vel_Sun;

            UpdateVisualPosition();
            _sunInitialized = true;
        }
        else if (Name == "Earth" && !Initialized && !_earthInitialized)
        {
            float earthDiameterUnity = (float)PhysicsConstants.ToUnityUnitsFromM(PhysicsConstants.REAL_EARTH_DIAMETER_M);
            transform.localScale = Vector3.one * earthDiameterUnity;

            Position = pos_Earth;
            Velocity = vel_Earth;

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

