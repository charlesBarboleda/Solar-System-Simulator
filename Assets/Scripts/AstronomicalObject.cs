using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using UnityEditor.EditorTools;

public class AstronomicalObject : MonoBehaviour
{
    public string Name;
    public double MassKg;

    [Tooltip("Object velocity in double3 instead of Vector3 to maintain precision over large distances.")]
    public double3 Velocity;

    [Tooltip("Object position in double3 instead of Vector3 to maintain precision over large distances.")]
    public double3 Position;

    [Tooltip("The barycentric position is the position relative to the system's center of mass.")]
    [SerializeField] double3 _barycentricPosition;

    [Tooltip("The barycentric velocity is the velocity relative to the system's center of mass.")]
    [SerializeField] double3 _barycentricVelocity;

    [Tooltip("The barycentric acceleration is the acceleration relative to the system's center of mass.")]
    [SerializeField] double3 _barycentricAcceleration;

    [SerializeField] double3 _acceleration;

    [Tooltip("Neighbors for N-body gravitational calculations. Neighbors are added automatically via trigger colliders.")]
    [SerializeField] List<AstronomicalObject> AstronomicalNeighbors = new();


    void Awake()
    {
        Position = (double3)(float3)transform.position; // initial sync from scene
        Velocity = double3.zero;

    }

    void FixedUpdate()
    {

    }

    void SyncPosition()
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
            if (other.TryGetComponent(out AstronomicalObject otherAstronomicalObject) && !AstronomicalNeighbors.Contains(otherAstronomicalObject))
            {
                AstronomicalNeighbors.Add(otherAstronomicalObject);
            }
        }
    }


}

