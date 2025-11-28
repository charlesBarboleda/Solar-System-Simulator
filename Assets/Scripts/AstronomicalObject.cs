using System.Collections.Generic;
using UnityEngine;

public class AstronomicalObject : MonoBehaviour
{
    public string Name;
    public double TotalMassKg;
    [SerializeField] List<AstronomicalObject> _astronomicalNeighbors = new();

    void FixedUpdate()
    {
        if (_astronomicalNeighbors.Count == 0) return;
        if (_astronomicalNeighbors.Count == 1)
        {
            double force = SpacePhysics3D.TwoBodyGForce(this, _astronomicalNeighbors[0]);
        }
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

