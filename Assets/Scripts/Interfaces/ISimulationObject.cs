using Unity.Mathematics;
using UnityEngine;

public interface ISimulationObject
{
    double3 Position { get; set; }
    double3 Velocity { get; set; }
}