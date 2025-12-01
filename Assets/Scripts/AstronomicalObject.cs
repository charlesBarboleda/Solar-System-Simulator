using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

public class AstronomicalObject : MonoBehaviour
{
    public string Name;
    public double TotalMassKg;
    public double3 Velocity;
    public double3 Position;
    [SerializeField] double _acceleration;
    [SerializeField] List<AstronomicalObject> _astronomicalNeighbors = new();

    void Awake()
    {
        Position = (double3)(float3)transform.position; // initial sync from scene
        Velocity = double3.zero;
    }

    void FixedUpdate()
    {
        double deltaTime = Time.fixedDeltaTime * (SimulationSettings.Instance != null ? SimulationSettings.Instance.TimeScale : 1.0);

        if (_astronomicalNeighbors.Count == 0) return;

        var neighbour = _astronomicalNeighbors[0];

        double3 acceleration = SpacePhysics3D.TwoBodyAcceleration(this, neighbour);
        Velocity += acceleration * deltaTime;
        Position += Velocity * deltaTime;
        SyncPosition();
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
            if (other.TryGetComponent(out AstronomicalObject otherAstronomicalObject) && !_astronomicalNeighbors.Contains(otherAstronomicalObject))
            {
                _astronomicalNeighbors.Add(otherAstronomicalObject);
            }
        }
    }


}

