using UnityEngine;
using Unity.Mathematics;
using System;

public class NBodyManager : MonoBehaviour
{
    public static NBodyManager Instance { get; private set; }

    public AstronomicalObject[] SystemBodies;
    double G;

    [Header("Authorative Object States")]
    string[] _names;
    double[] _masses;
    double3[] _accelerations, _velocities, _positions; // current (snapshot) vector properties

    [Header("Predicted Object States")]
    double3[] _positionsNext, _accelerationsNext, _velocityHalf; // next (predicted) vector properties

    private readonly SpacePhysics3D.Workspace_EIH _workspaceEIH = new();

    [SerializeField] AccelBMode _accelBMode = AccelBMode.FixedPointIterated;
    [SerializeField] bool _useNewtonian = false;

    [Header("Debugging")]
    [SerializeField] bool _debug = false;

    int _earthIndex = 1;
    int _sunIndex = 0;


    [Header("System Invariants Diagnostics")]
    [SerializeField] bool _diagnostics = false;
    [SerializeField] int _diagEveryNSteps = 50;




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
        G = PhysicsConstants.UNITY_G;
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

            if (_earthIndex >= 0 && _sunIndex >= 0) NBodyDiagnostics.InitEarthDiagnostics(_earthIndex, _sunIndex, _positions);
            else _diagnostics = false;
            if (_diagnostics) NBodyDiagnostics.InitSystemInvariantBaseline(_masses, _positions, _velocities, G);
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

        SimulationSettings.Instance.GetSubstepPlan(out int steps, out double dtStep, out double dtAdvanced, out double dtRequested);
#if UNITY_EDITOR
        if (_debug)
        {
            double baseDaysThisFixed = Time.fixedDeltaTime * PhysicsConstants.UNITY_DAYS_PER_REAL_SECOND;
            double effectiveTimeScale = (baseDaysThisFixed > 0.0) ? (dtAdvanced / baseDaysThisFixed) : 0.0;
            Debug.Log($"RequestedScale={SimulationSettings.Instance.TimeScale:F2}, EffectiveScale={effectiveTimeScale:F2}, steps={steps}, dtStep={dtStep:F6}d");
        }
#endif

        for (int i = 0; i < steps; i++)
        {
            IntegrateOneStep(dtStep, numOfBodies);
            if (_diagnostics)
            {
                NBodyDiagnostics.Diagnostics_OrbitByPeriapsis(
                    _earthIndex,
                    _sunIndex,
                    dtStep,
                    _positions,
                    _velocities,
                    PhysicsConstants.UNITY_G * (_masses[_sunIndex] + _masses[_earthIndex]));

                NBodyDiagnostics.StepSystemDiagnostics(dtStep, _diagEveryNSteps, _masses, _positions, _velocities, G);
            }
        }

        ApplySimulationStateFromArrays(_positions, _velocities);

    }

    // --- Private Helpers ---

    // Main Integrator (velocity-Verlet for Newtonian, Heun/RK2 for EIH)
    void IntegrateOneStep(double dt, int numOfBodies)
    {
        if (_useNewtonian)
        {
            // 1) Acceleration of each body using base Newtonian (symmetric sweep)
            SpacePhysics3D.NBodyAccelVectorFrom(_masses, _positions, _accelerations);

            // 2) Half-step velocity of each body "a" using its velocity & acceleration
            for (int a = 0; a < numOfBodies; a++)
                _velocityHalf[a] = (_masses[a] <= 0.0) ? double3.zero : _velocities[a] + 0.5 * _accelerations[a] * dt;

            // 3) Predicted position of each body "a" using its half-step velocity & position
            for (int a = 0; a < numOfBodies; a++)
                _positionsNext[a] = (_masses[a] <= 0.0) ? double3.zero : _positions[a] + _velocityHalf[a] * dt;

            // 4) Predicted acceleration of each body "a" using its predicted position
            SpacePhysics3D.NBodyAccelVectorFrom(_masses, _positionsNext, _accelerationsNext);

            // 5) Predicted velocities using half-step velocities & predicted accelerations
            for (int a = 0; a < numOfBodies; a++)
                _velocities[a] = (_masses[a] <= 0.0) ? double3.zero : _velocityHalf[a] + 0.5 * _accelerationsNext[a] * dt;

            // 6) Commit and update positions 
            (_positions, _positionsNext) = (_positionsNext, _positions);
        }
        else
        {
            // Heun/RK2 for velocity-dependent acceleration (EIH)
            SpacePhysics3D.Einstein_Infeld_Hoffmann_1PN(_positions, _velocities, _masses, _accelerations, _workspaceEIH, _accelBMode);

            for (int a = 0; a < numOfBodies; a++)
            {
                if (_masses[a] <= 0.0)
                {
                    _velocityHalf[a] = double3.zero;
                    _positionsNext[a] = double3.zero;
                    continue;
                }

                double3 v0 = _velocities[a];
                _velocityHalf[a] = v0 + _accelerations[a] * dt; // predictor velocity
                _positionsNext[a] = _positions[a] + v0 * dt;     // predictor position
            }

            SpacePhysics3D.Einstein_Infeld_Hoffmann_1PN(_positionsNext, _velocityHalf, _masses, _accelerationsNext, _workspaceEIH, _accelBMode);

            for (int a = 0; a < numOfBodies; a++)
            {
                if (_masses[a] <= 0.0)
                {
                    _velocities[a] = double3.zero;
                    _positions[a] = double3.zero;
                    continue;
                }

                double3 v0 = _velocities[a];
                double3 v1 = _velocityHalf[a];

                _velocities[a] = v0 + 0.5 * (_accelerations[a] + _accelerationsNext[a]) * dt;
                _positions[a] = _positions[a] + 0.5 * (v0 + v1) * dt;
            }
        }
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

