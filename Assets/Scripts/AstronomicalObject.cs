using UnityEngine;
using Unity.Mathematics;
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


    // Orbit Diagnostics Variables
    double3 _initPos;
    double _timeElapsed = 0.0;
    double _orbitRadiusMin = 0.0;
    double _orbitRadiusMax = 0.0;
    int _totalOrbitsCompleted = 0;
    bool _waitingToExitStartRegion;
    [SerializeField] AstronomicalObject _sun;


    void Start()
    {
        Position = (double3)(float3)transform.position; // initial sync from scene
        Velocity = double3.zero;

        if (Name == "Sun")
        {
            Position = new double3(0, 0, 0);

            Velocity = new double3(0, 0, 0);

            float sunDiameterUnity = (float)PhysicsConstants.ToUnityUnitsFromM(PhysicsConstants.REAL_SUN_DIAMETER_M);
            transform.localScale = Vector3.one * sunDiameterUnity;

            MassKg = PhysicsConstants.REAL_SOLAR_MASS_KG;

            ApplyPosition();
        }
        else if (Name == "Earth")
        {
            double unityEarthToSun = PhysicsConstants.ToUnityUnitsFromAU(PhysicsConstants.REAL_EARTH_SUN_DISTANCE_AU);
            Position = new double3(unityEarthToSun, 0, 0);

            double GM = PhysicsConstants.UNITY_G * PhysicsConstants.REAL_SOLAR_MASS_KG;
            double v = math.sqrt(GM / unityEarthToSun); // UnityUnits / day
            Velocity = new double3(0.0, 0.0, v);

            float earthDiameterUnity = (float)PhysicsConstants.ToUnityUnitsFromM(PhysicsConstants.REAL_EARTH_DIAMETER_M);
            transform.localScale = Vector3.one * earthDiameterUnity;

            MassKg = PhysicsConstants.REAL_EARTH_MASS_KG;

            ApplyPosition();

            _initPos = Position;

            double r0 = math.distance(Position, _initPos);
            _orbitRadiusMin = r0;
            _orbitRadiusMax = r0;
        }
        else if (Name == "Neptune")
        {
            Debug.Log("Entered Neptune statement");
            Debug.Log($"Current {Name} position: {transform.localPosition}");
            Position = new double3(PhysicsConstants.ToUnityUnitsFromAU(PhysicsConstants.REAL_NEPTUNE_SUN_DISTANCE_AU), 0, 0);
            ApplyPosition();
            Debug.Log("Attempting to move 'Neptune' to converted position");
            Debug.Log($"Moved {Name} position: {transform.localPosition}");
        }
    }

    void FixedUpdate()
    {
        EarthOrbitalValidations();
    }

    void EarthOrbitalValidations()
    {
        if (Name == "Earth")
        {
            _timeElapsed += Time.fixedDeltaTime;

            // current radius from Sun
            double currentRadius = math.length(Position);

            if (_totalOrbitsCompleted == 0 && _orbitRadiusMax == 0.0 && _orbitRadiusMin == 0.0)
            {
                _orbitRadiusMax = currentRadius;
                _orbitRadiusMin = currentRadius;
            }

            if (currentRadius < _orbitRadiusMin) _orbitRadiusMin = currentRadius;
            if (currentRadius > _orbitRadiusMax) _orbitRadiusMax = currentRadius;

            // distance from starting point (for orbit completion detection)
            double startDistance = math.distance(Position, _initPos);

            if (!_waitingToExitStartRegion && startDistance <= 0.1)
            {
                double semiMajorAxisAU = ((_orbitRadiusMax + _orbitRadiusMin) / 2) / PhysicsConstants.UNITY_UNITS_PER_AU;
                double eccentricity = (_orbitRadiusMax - _orbitRadiusMin) / (_orbitRadiusMax + _orbitRadiusMin);

                double simDays = _timeElapsed
                                 * SimulationSettings.Instance.TimeScale
                                 * PhysicsConstants.UNITY_DAYS_PER_REAL_SECOND;

                double rMinAU = _orbitRadiusMin / PhysicsConstants.UNITY_UNITS_PER_AU;
                double rMaxAU = _orbitRadiusMax / PhysicsConstants.UNITY_UNITS_PER_AU;

                _totalOrbitsCompleted++;

                Debug.Log(
                    $"[{_totalOrbitsCompleted}] TimeScale={SimulationSettings.Instance.TimeScale}, " +
                    $"Period={simDays:F2} sim days, " +
                    $"rMin={rMinAU:F6} AU, rMax={rMaxAU:F6} AU, " +
                    $"Semi-Major Axis={semiMajorAxisAU:F6} AU, " +
                    $"Eccentricity={eccentricity:F6}"
                    );


                _timeElapsed = 0.0;

                // reset min/max for next orbit starting from current radius
                _orbitRadiusMin = currentRadius;
                _orbitRadiusMax = currentRadius;

                _waitingToExitStartRegion = true;
            }
            else if (_waitingToExitStartRegion && startDistance > 0.5)
            {
                // left the start region; ready to detect the next orbit
                _waitingToExitStartRegion = false;
            }
        }
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
}

