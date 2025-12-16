using UnityEngine;
using Unity.Mathematics;
using System.Linq;
using Unity.VisualScripting;
using System.Xml.Serialization;
using UnityEditor;

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

            UpdateVisualPosition();
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

            UpdateVisualPosition();

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
            UpdateVisualPosition();
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
            double currentRadius = math.length(Position - _sun.Position);

            if (_totalOrbitsCompleted == 0 && _orbitRadiusMax == 0.0 && _orbitRadiusMin == 0.0)
            {
                _orbitRadiusMax = currentRadius;
                _orbitRadiusMin = currentRadius;
            }

            if (currentRadius < _orbitRadiusMin) _orbitRadiusMin = currentRadius;
            if (currentRadius > _orbitRadiusMax) _orbitRadiusMax = currentRadius;

            // distance from starting point (for orbit completion detection)
            double startDistance = math.distance(Position - _sun.Position, _initPos - _sun.Position);
            double distanceEpsilon = 0.1;

            if (!_waitingToExitStartRegion && startDistance <= distanceEpsilon)
            {
                double simDays = _timeElapsed
                                 * SimulationSettings.Instance.TimeScale
                                 * PhysicsConstants.UNITY_DAYS_PER_REAL_SECOND;

                // Sampled Diagnostics
                double semiMajorAxis_SamAU = ((_orbitRadiusMax + _orbitRadiusMin) / 2) / PhysicsConstants.UNITY_UNITS_PER_AU;
                double eccentricity_Sam = (_orbitRadiusMax - _orbitRadiusMin) / (_orbitRadiusMax + _orbitRadiusMin);

                double perihelion_SamAU = _orbitRadiusMin / PhysicsConstants.UNITY_UNITS_PER_AU;
                double aphelion_SamAU = _orbitRadiusMax / PhysicsConstants.UNITY_UNITS_PER_AU;

                double3 displacementVec = Position - _sun.Position;
                double3 velocityVec = Velocity - _sun.Velocity;
                double3 h = math.cross(displacementVec, velocityVec);
                double angularMomentum_Sam = math.length(h);

                double rMag = math.length(displacementVec);
                double v2 = math.lengthsq(velocityVec);
                double mu = PhysicsConstants.UNITY_G * _sun.MassKg;
                double orbitalEnergy_Sam = 0.5 * v2 - (mu / rMag);

                // Elements Diagnostics
                double semiMajorAxis_ElemAU = (-mu / (2.0 * orbitalEnergy_Sam)) / PhysicsConstants.UNITY_UNITS_PER_AU;
                double3 eccentricityVec = math.cross(velocityVec, h) / mu - (displacementVec / rMag);
                double eccentricity_Elem = math.length(eccentricityVec);

                double perihelion_ElemAU = (semiMajorAxis_ElemAU * (1.0 - eccentricity_Elem));
                double aphelion_ElemAU = (semiMajorAxis_ElemAU * (1.0 + eccentricity_Elem));

                _totalOrbitsCompleted++;

                Debug.Log(
                    $"[{_totalOrbitsCompleted}] TimeScale={SimulationSettings.Instance.TimeScale}, " +
                    $"Period={simDays:F2} sim days, " +
                    $"Perihelion=Sam: {perihelion_SamAU:F6} AU | Elem: {perihelion_ElemAU:F12} AU, " +
                    $"Aphelion=Sam: {aphelion_SamAU:F6} AU | Elem: {aphelion_ElemAU:F12} AU, " +
                    $"Semi-Major Axis=Sam: {semiMajorAxis_SamAU:F6} AU | Elem: {semiMajorAxis_ElemAU:F12} AU, " +
                    $"Eccentricity=Sam: {eccentricity_Sam:F6} | Elem: {eccentricity_Elem:F12}, " +
                    $"Angular Momentum={angularMomentum_Sam:F6},  "
                    );


                _timeElapsed = 0.0;

                // reset min/max for next orbit starting from current radius
                _orbitRadiusMin = currentRadius;
                _orbitRadiusMax = currentRadius;

                _waitingToExitStartRegion = true;
            }
            else if (_waitingToExitStartRegion && startDistance > distanceEpsilon)
            {
                // left the start region; ready to detect the next orbit
                _waitingToExitStartRegion = false;
            }
        }
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

