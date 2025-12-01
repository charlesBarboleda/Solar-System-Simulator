using UnityEngine;
using Unity.Mathematics;

public class NBodyManager : MonoBehaviour
{
    public AstronomicalObject[] AstronomicalBodies;

    private double3[] _positions;     // sim units (AU)
    private double3[] _velocities;    // sim units (AU / day)
    private double[] _massesSim;     // sim mass units (solar masses)
    private double3[] _accelerations; // sim units (AU / day^2)
}
