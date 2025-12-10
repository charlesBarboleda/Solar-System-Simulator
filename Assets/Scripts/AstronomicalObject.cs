using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using NUnit.Framework;
using System.Linq;
using Unity.VisualScripting;

public class AstronomicalObject : MonoBehaviour
{
    public string Name;
    public double MassKg;

    [Tooltip("double3 Velocity instead of Vector3 to maintain precision over large distances.")]
    public double3 Velocity;

    [Tooltip("double3 Position instead of Vector3 to maintain precision over large distances.")]
    public double3 Position;

    AstronomicalObject[] StarSystem => NBodyManager.Instance.SystemBodies;


    void Start()
    {
        Position = (double3)(float3)transform.position; // initial sync from scene
        Velocity = double3.zero;

        if (!StarSystem.Contains(this)) StarSystem.Append(this);
    }

    public void ApplyPosition()
    {
        if (!math.all(math.isfinite(Position)))
        {
            Debug.LogError($"{name}: Invalid Position in SyncPosition: {Position}");
            return; // prevents writing NaN into Transform 
        }

        transform.position = (Vector3)(float3)Position;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(tag))
        {
            if (other.TryGetComponent(out AstronomicalObject otherAstronomicalObject) && !StarSystem.Contains(otherAstronomicalObject))
            {
                StarSystem.Append(otherAstronomicalObject);
            }
        }
    }


}

