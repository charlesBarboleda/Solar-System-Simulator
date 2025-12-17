using UnityEngine;
using Unity.Mathematics;
using System;

public class NBodyManager : MonoBehaviour
{
    public static NBodyManager Instance { get; private set; }

    public AstronomicalObject[] SystemBodies;

    [Header("Authorative Object States")]
    string[] _names;
    double[] _masses;
    double3[] _accelerations, _velocities, _positions; // current (snapshot) vector properties

    [Header("Predicted Object States")]
    double3[] _positionsNext, _accelerationsNext, _velocityHalf; // next (predicted) vector properties

    private readonly SpacePhysics3D.Workspace_EIH _workspaceEIH = new();

    double DtSimDays => SimulationSettings.Instance.DeltaSimDays;

    [SerializeField] bool _useNewtonian = false;

    [Header("Debugging & Diagnostics")]
    bool _debug = false;
    int _earthIndex = 1;
    int _sunIndex = 0;
    double _orbitTimeSimDays;
    double _orbitRMin = double.PositiveInfinity;
    double _orbitRMax = 0.0;
    bool _waitingToExit;
    double3 _initEarthRel;
    int _orbits;



    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        if (SystemBodies == null) return;

        int numOfBodies = SystemBodies.Length;
        if (numOfBodies <= 0)
        {
            Debug.LogError("[NBodyManager] Start(): Invalid numOfBodies <= 0");
        }
        else
        {
            _earthIndex = FindIndexByName(SystemBodies, "Earth");
            _sunIndex = FindIndexByName(SystemBodies, "Sun");
            if (_earthIndex < 0 || _sunIndex < 0) _debug = false;

            _names = new string[numOfBodies];
            _masses = new double[numOfBodies];

            _accelerations = new double3[numOfBodies];
            _accelerationsNext = new double3[numOfBodies];

            _positions = new double3[numOfBodies];
            _positionsNext = new double3[numOfBodies];

            _velocities = new double3[numOfBodies];
            _velocityHalf = new double3[numOfBodies];

            SnapshotSystemState();
            InitEarthDiagnostics(_earthIndex, _sunIndex, _positions);
        }
    }

    void FixedUpdate()
    {
        int n = SystemBodies?.Length ?? 0;
        if (n <= 0) return;

        double dt = DtSimDays;

        // 0) Snapshot bodies -> arrays (authoritative state for this step)
        SnapshotSystemState();

        // 1) a(t) for all bodies (batched)
        if (_useNewtonian)
        {
            for (int i = 0; i < n; i++)
                _accelerations[i] = (_masses[i] <= 0.0) ? double3.zero : SpacePhysics3D.NBodyAccelVectorOf(i, _masses, _positions);
        }
        else
        {
            SpacePhysics3D.Einstein_Infeld_Hoffmann_1PN(
                _positions,
                _velocities,
                _masses,
                _accelerations,
                _workspaceEIH
            );
        }

        // 2) Kick: v(t+dt/2) = v(t) + 0.5*a(t)*dt
        for (int i = 0; i < n; i++)
        {
            if (_masses[i] <= 0.0)
            {
                _velocityHalf[i] = double3.zero;
                continue;
            }

            _velocityHalf[i] = _velocities[i] + 0.5 * _accelerations[i] * dt;
        }

        // 3) Drift: x(t+dt) = x(t) + v(t+dt/2)*dt
        for (int i = 0; i < n; i++)
        {
            if (_masses[i] <= 0.0)
            {
                _positionsNext[i] = double3.zero;
                continue;
            }

            _positionsNext[i] = _positions[i] + _velocityHalf[i] * dt;
        }

        // 4) a(t+dt) using predicted state (x(t+dt), v(t+dt/2))
        if (_useNewtonian) // use Newtonian 
        {
            for (int i = 0; i < n; i++)
                _accelerationsNext[i] = (_masses[i] <= 0.0) ? double3.zero : SpacePhysics3D.NBodyAccelVectorOf(i, _masses, _positionsNext);
        }
        else // use EIH
        {
            SpacePhysics3D.Einstein_Infeld_Hoffmann_1PN(
                _positionsNext,
                _velocityHalf,          // important: predicted velocity state
                _masses,
                _accelerationsNext,
                _workspaceEIH
            );
        }

        // 5) Kick: v(t+dt) = v(t+dt/2) + 0.5*a(t+dt)*dt
        for (int i = 0; i < n; i++)
        {
            if (_masses[i] <= 0.0)
            {
                _velocities[i] = double3.zero;
                continue;
            }

            _velocities[i] = _velocityHalf[i] + 0.5 * _accelerationsNext[i] * dt;
        }

        // 6) Commit authoritative next state to arrays (positions become positionsNext; velocities already updated)
        (_positions, _positionsNext) = (_positionsNext, _positions);


        // 7) Apply arrays -> bodies + visuals
        ApplySimulationStateFromArrays(_positions, _velocities);


        if (_debug)
        {
            int earth = _earthIndex;
            int sun = _sunIndex;

            Diagnostics_SampledOrbit(
                earth,
                sun,
                dt: DtSimDays,          // use sim dt directly
                positions: _positions,
                velocities: _velocities
            );
        }
    }


    // --- Private Helpers ---
    void ApplySimulationStateFromArrays(double3[] positions, double3[] velocities)
    {
        int n = SystemBodies.Length;

        for (int i = 0; i < n; i++)
        {
            var body = SystemBodies[i];
            if (!IsValidAstronomicalBody(body)) continue;

            body.Position = positions[i];
            body.Velocity = velocities[i];
            body.UpdateVisualPosition();
        }
    }

    bool IsValidAstronomicalBody(AstronomicalObject body)
    {
        if (body == null || body.MassKg <= 0.0)
        {
            Debug.LogError($"[NBodyManager] IsValidAstronomicalBody(): Invalid or Null AstronomicalObject.");
            return false;
        }

        return true;
    }

    void SnapshotSystemState()
    {
        int n = SystemBodies.Length;

        for (int i = 0; i < n; i++)
        {
            var body = SystemBodies[i];

            if (!IsValidAstronomicalBody(body))
            {
                _masses[i] = 0.0;
                _positions[i] = double3.zero;
                _velocities[i] = double3.zero;
                continue;
            }

            _names[i] = body.name;
            _masses[i] = body.MassKg;
            _positions[i] = body.Position;
            _velocities[i] = body.Velocity;
        }
    }

    void InitEarthDiagnostics(int earth, int sun, ReadOnlySpan<double3> positions)
    {
        _initEarthRel = positions[earth] - positions[sun];
        _orbitTimeSimDays = 0.0;
        _orbitRMin = double.PositiveInfinity;
        _orbitRMax = 0.0;
        _waitingToExit = true; // prevent immediate trigger at t=0
        _orbits = 0;
    }

    void Diagnostics_SampledOrbit(
        int earth, int sun,
        double dt,
        ReadOnlySpan<double3> positions,
        ReadOnlySpan<double3> velocities)
    {
        _orbitTimeSimDays += dt;

        double3 r = positions[earth] - positions[sun];
        double radius = math.length(r);

        if (radius < _orbitRMin) _orbitRMin = radius;
        if (radius > _orbitRMax) _orbitRMax = radius;

        double3 r0 = _initEarthRel;
        double startDistance = math.length(r - r0);

        // epsilon should be expressed in Unity units.
        // If 1 AU == UNITY_UNITS_PER_AU, this is a small fraction of AU.
        double epsilon = 1e-4 * PhysicsConstants.UNITY_UNITS_PER_AU;

        if (_waitingToExit)
        {
            if (startDistance > epsilon) _waitingToExit = false;
            return;
        }

        if (startDistance <= epsilon)
        {
            _orbits++;

            double periAU = _orbitRMin / PhysicsConstants.UNITY_UNITS_PER_AU;
            double apheAU = _orbitRMax / PhysicsConstants.UNITY_UNITS_PER_AU;
            double aAU = ((_orbitRMin + _orbitRMax) * 0.5) / PhysicsConstants.UNITY_UNITS_PER_AU;
            double eSam = (_orbitRMax - _orbitRMin) / (_orbitRMax + _orbitRMin);

            Debug.Log($"[{_orbits}] Period={_orbitTimeSimDays:F2} sim days, Perihelion=Sam {periAU:F12} AU, Aphelion=Sam {apheAU:F12} AU, Semi-Major Axis=Sam {aAU:F12} AU, Eccentricity=Sam {eSam:F12}");

            // reset window
            _orbitTimeSimDays = 0.0;
            _orbitRMin = double.PositiveInfinity;
            _orbitRMax = 0.0;

            _waitingToExit = true;
        }

    }

    int FindIndexByName(AstronomicalObject[] bodies, string targetName)
    {
        for (int i = 0; i < bodies.Length; i++)
        {
            // Ordinal is typically fastest/correct for identifiers (case-sensitive)
            if (string.Equals(bodies[i].Name, targetName, StringComparison.Ordinal))
                return i;
        }
        return -1; // not found
    }

}

