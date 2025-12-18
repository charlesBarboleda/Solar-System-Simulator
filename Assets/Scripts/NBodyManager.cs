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

    [SerializeField] AccelBMode _accelBMode = AccelBMode.FixedPointIterated;
    [SerializeField] bool _useNewtonian = false;

    [Header("Debugging & Diagnostics")]
    [SerializeField] bool _debug = false;
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

            _names = new string[numOfBodies];
            _masses = new double[numOfBodies];

            _accelerations = new double3[numOfBodies];
            _accelerationsNext = new double3[numOfBodies];

            _positions = new double3[numOfBodies];
            _positionsNext = new double3[numOfBodies];

            _velocities = new double3[numOfBodies];
            _velocityHalf = new double3[numOfBodies];

            SnapshotSystemState();

            if (_earthIndex >= 0 && _sunIndex >= 0) InitEarthDiagnostics(_earthIndex, _sunIndex, _positions);
            else _debug = false;
        }
    }

    void FixedUpdate()
    {
        int numOfBodies = SystemBodies?.Length ?? 0;
        if (numOfBodies <= 0) return;
        if (_masses == null || _masses.Length != numOfBodies)
        {
            Debug.LogError("State arrays not initialized / length mismatch.");
            return;
        }

        // SnapshotSystemState(); 

        SimulationSettings.Instance.GetSubstepPlan(out int steps, out double dtStep, out double dtTotal);
        if (steps <= 0) return;

        for (int i = 0; i < steps; i++) IntegrateOneStep(dtStep, numOfBodies);

        ApplySimulationStateFromArrays(_positions, _velocities);

        if (_debug && _earthIndex >= 0 && _sunIndex >= 0 && _earthIndex < numOfBodies && _sunIndex < numOfBodies)
        {
            Diagnostics_SampledOrbit(
                _earthIndex, _sunIndex,
                dt: dtTotal,              // total sim time advanced this FixedUpdate
                positions: _positions,
                velocities: _velocities
            );
            // int logPositionEvery = 1; // Logs the position every x seconds
            // _elapsedTime += Time.fixedDeltaTime;
            // if (_elapsedTime >= logPositionEvery)
            // {
            //     Debug.Log($"Earth Authorative Position: {_positions[_earthIndex]}");
            //     _elapsedTime = 0;
            // }
        }
    }

    // --- Private Helpers ---

    // Main Integrator (velocity-Verlet leapfrog with half-step)
    void IntegrateOneStep(double dt, int numOfBodies)
    {
        // 1) Compute acceleration of each body "a" using base Newtonian (one at a time)
        if (_useNewtonian)
        {
            for (int a = 0; a < numOfBodies; a++)
                _accelerations[a] = (_masses[a] <= 0.0) ? double3.zero : SpacePhysics3D.NBodyAccelVectorOf(a, _masses, _positions);
        }
        else // OR compute all body's accelerations at the same time using EIH
        {
            SpacePhysics3D.Einstein_Infeld_Hoffmann_1PN(_positions, _velocities, _masses, _accelerations, _workspaceEIH, _accelBMode);
        }

        // 2) Compute half-step velocity of each body "a" using its velocity & acceleration
        for (int a = 0; a < numOfBodies; a++)
            _velocityHalf[a] = (_masses[a] <= 0.0) ? double3.zero : _velocities[a] + 0.5 * _accelerations[a] * dt;

        // 3) Compute predicted position of each body "a" using its half-step velocity & position
        for (int a = 0; a < numOfBodies; a++)
            _positionsNext[a] = (_masses[a] <= 0.0) ? double3.zero : _positions[a] + _velocityHalf[a] * dt;

        // 4) Compute predicted acceleration of each body "a" using its predicted position
        if (_useNewtonian)
        {
            for (int a = 0; a < numOfBodies; a++)
                _accelerationsNext[a] = (_masses[a] <= 0.0) ? double3.zero : SpacePhysics3D.NBodyAccelVectorOf(a, _masses, _positionsNext);
        }
        else // OR compute all body's predicted accelerations at the same time
        {
            SpacePhysics3D.Einstein_Infeld_Hoffmann_1PN(_positionsNext, _velocityHalf, _masses, _accelerationsNext, _workspaceEIH, _accelBMode);
        }

        // 5) Compute predicted velocities using half-step velocities & predicted accelerations
        for (int a = 0; a < numOfBodies; a++)
            _velocities[a] = (_masses[a] <= 0.0) ? double3.zero : _velocityHalf[a] + 0.5 * _accelerationsNext[a] * dt;

        // 6) Commit and update positions 
        (_positions, _positionsNext) = (_positionsNext, _positions);
    }

    void ApplySimulationStateFromArrays(double3[] positions, double3[] velocities)
    {
        int numOfBodies = _masses.Length;

        for (int a = 0; a < numOfBodies; a++)
        {
            if (_masses[a] <= 0.0) continue;

            var body = SystemBodies[a];
            if (body == null) continue; // optional: log once at init instead

            body.Position = positions[a];
            body.Velocity = velocities[a];
            body.UpdateVisualPosition();
        }
    }

    bool IsValidAstronomicalBody(AstronomicalObject body)
    {
        if (body == null)
        {
            Debug.LogWarning($"[NBodyManager] IsValidAstronomicalBody(): Invalid or Null AstronomicalObject.");
            return false;
        }
        if (body.MassKg <= 0.0)
        {
            Debug.LogWarning($"[NBodyManager] IsValidAstronomicalBody(): {body.Name} must have MassKg > 0.0");
            return false;
        }

        return true;
    }

    void SnapshotSystemState()
    {
        int numOfBodies = SystemBodies.Length;

        for (int a = 0; a < numOfBodies; a++)
        {
            AstronomicalObject body = SystemBodies[a];

            if (!IsValidAstronomicalBody(body))
            {
                _masses[a] = 0.0;
                _positions[a] = double3.zero;
                _velocities[a] = double3.zero;
                continue;
            }

            _names[a] = body.Name;
            _masses[a] = body.MassKg;
            _positions[a] = body.Position;
            _velocities[a] = body.Velocity;
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

        double epsilon = PhysicsConstants.UNITY_MIN_DISTANCE;

        if (_waitingToExit)
        {
            if (startDistance > epsilon) _waitingToExit = false;
            return;
        }

        if (startDistance <= epsilon)
        {
            _orbits++;

            double perihelionAU = _orbitRMin / PhysicsConstants.UNITY_UNITS_PER_AU;
            double aphelionAU = _orbitRMax / PhysicsConstants.UNITY_UNITS_PER_AU;
            double semiMajorAxisAU = ((_orbitRMin + _orbitRMax) * 0.5) / PhysicsConstants.UNITY_UNITS_PER_AU;
            double eccentricitySam = (_orbitRMax - _orbitRMin) / (_orbitRMax + _orbitRMin);

            Debug.Log($"[{_orbits}] Period={_orbitTimeSimDays:F2} sim days, " +
            $"Perihelion = Sam: {perihelionAU:F12} AU, | " +
            $"Aphelion = Sam: {aphelionAU:F12} AU, | " +
            $"Semi-Major Axis = Sam: {semiMajorAxisAU:F12} AU, | " +
            $"Eccentricity = Sam: {eccentricitySam:F12} | ");


            // reset window
            _orbitTimeSimDays = 0.0;
            _orbitRMin = double.PositiveInfinity;
            _orbitRMax = 0.0;

            _waitingToExit = true;
        }

    }

    int FindIndexByName(AstronomicalObject[] bodies, string targetName)
    {
        for (int a = 0; a < bodies.Length; a++)
        {
            AstronomicalObject body = bodies[a];
            if (body == null) continue;
            if (string.Equals(body.Name, targetName, StringComparison.Ordinal)) return a;

        }
        return -1; // not found
    }

}

