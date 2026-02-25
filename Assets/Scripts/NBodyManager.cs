using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public class NBodyManager : MonoBehaviour
{
    public static NBodyManager Instance { get; private set; }

    [Header("System Bodies")]
    public AstronomicalObject[] SystemBodies;

    [Header("Integrator / Gravity")]
    [SerializeField] GravityModel _gravityModel = GravityModel.EIH;
    [SerializeField] AccelBMode _accelBMode = AccelBMode.FixedPointIterated;
    [SerializeField, Min(1)] int _accelIterations = 2;

    [Header("Simulation Loop")]
    [Tooltip("If true, drive simulation from Update() using SimulationSettings.GetSubstepPlan(deltaSeconds,...).")]
    [SerializeField] bool _runInUpdate = true;

    [Tooltip("If true and running in Update(), uses Time.unscaledDeltaTime. Otherwise uses Time.deltaTime.")]
    [SerializeField] bool _useUnscaledDeltaTime = true;

    [Header("Debugging")]
    [SerializeField] bool _debug = false;

    [Header("System Invariants Diagnostics (expensive)")]
    [SerializeField] bool _diagnostics = false;
    NBodyDiagnostics.Workspace_Diagnosis _diagWorkspace = new();

    // Native (authoritative) state
    NativeArray<double> _masses;
    NativeArray<double3> _positions;
    NativeArray<double3> _velocities;

    // Native scratch (per-step)
    NativeArray<double3> _accelerations;
    NativeArray<double3> _positionsNext;
    NativeArray<double3> _velocityHalf;
    NativeArray<double3> _accelerationsNext;

    // Burst EIH workspace (NativeArrays)
    SpacePhysics3D.Workspace_EIH _workspaceEIH;

    // Managed mirrors (only for diagnostics)
    double3[] _positionsManaged;
    double3[] _velocitiesManaged;

    bool _initialized;

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

    void Start() => Initialize();

    void Update()
    {
        if (!_runInUpdate) return;
        if (!_initialized) return;

        double dtSeconds = _useUnscaledDeltaTime ? Time.unscaledDeltaTime : Time.deltaTime;
        StepSimulation(dtSeconds);
    }

    void FixedUpdate()
    {
        if (_runInUpdate) return;
        if (!_initialized) return;

        StepSimulation(Time.fixedDeltaTime);
    }

    void OnDestroy() => DisposeNative();

    void Initialize()
    {
        if (SystemBodies == null || SystemBodies.Length <= 0)
        {
            _initialized = false;
            return;
        }

        int n = SystemBodies.Length;

        DisposeNative();

        _masses = new NativeArray<double>(n, Allocator.Persistent);
        _positions = new NativeArray<double3>(n, Allocator.Persistent);
        _velocities = new NativeArray<double3>(n, Allocator.Persistent);

        _accelerations = new NativeArray<double3>(n, Allocator.Persistent);
        _positionsNext = new NativeArray<double3>(n, Allocator.Persistent);
        _velocityHalf = new NativeArray<double3>(n, Allocator.Persistent);
        _accelerationsNext = new NativeArray<double3>(n, Allocator.Persistent);

        _workspaceEIH = new SpacePhysics3D.Workspace_EIH();
        _workspaceEIH.EnsureCapacity(n);

        _positionsManaged = new double3[n];
        _velocitiesManaged = new double3[n];

        SnapshotSystemStateToNative();

        _initialized = true;
    }

    void DisposeNative()
    {
        if (_masses.IsCreated) _masses.Dispose();
        if (_positions.IsCreated) _positions.Dispose();
        if (_velocities.IsCreated) _velocities.Dispose();

        if (_accelerations.IsCreated) _accelerations.Dispose();
        if (_positionsNext.IsCreated) _positionsNext.Dispose();
        if (_velocityHalf.IsCreated) _velocityHalf.Dispose();
        if (_accelerationsNext.IsCreated) _accelerationsNext.Dispose();

        // Dispose workspace safely (avoids potential double-dispose issues if your workspace Dispose has bugs).
        DisposeWorkspaceSafe();
    }

    void DisposeWorkspaceSafe()
    {
        if (_workspaceEIH == null) return;

        if (_workspaceEIH.BarycentricPositions.IsCreated) _workspaceEIH.BarycentricPositions.Dispose();
        if (_workspaceEIH.BarycentricVelocities.IsCreated) _workspaceEIH.BarycentricVelocities.Dispose();
        if (_workspaceEIH.ActiveMask.IsCreated) _workspaceEIH.ActiveMask.Dispose();

        if (_workspaceEIH.PotentialPhi.IsCreated) _workspaceEIH.PotentialPhi.Dispose();
        if (_workspaceEIH.NewtonianAccel.IsCreated) _workspaceEIH.NewtonianAccel.Dispose();
        if (_workspaceEIH.AccelApprox.IsCreated) _workspaceEIH.AccelApprox.Dispose();

        if (_workspaceEIH.SecondTermSum.IsCreated) _workspaceEIH.SecondTermSum.Dispose();
        if (_workspaceEIH.ThirdTermSum.IsCreated) _workspaceEIH.ThirdTermSum.Dispose();
        if (_workspaceEIH.FourthTermSum.IsCreated) _workspaceEIH.FourthTermSum.Dispose();

        _workspaceEIH = null;
    }


    void StepSimulation(double realDeltaSeconds)
    {
        int n = SystemBodies?.Length ?? 0;
        if (n <= 0) return;

        if (!_masses.IsCreated || _masses.Length != n)
        {
            Debug.LogError("Native state arrays not initialized / length mismatch.");
            return;
        }

        // Uses your new overload (works from Update or FixedUpdate).
        SimulationSettings.Instance.GetSubstepPlan(
            realDeltaSeconds,
            out int steps,
            out double dtStepDays,
            out double dtAdvancedDays,
            out double dtRequestedDays);

#if UNITY_EDITOR
        if (_debug)
        {
            double baseDaysThisTick = realDeltaSeconds * PhysicsConstants.UNITY_DAYS_PER_REAL_SECOND;
            double effectiveTimeScale = (baseDaysThisTick > 0.0) ? (dtAdvancedDays / baseDaysThisTick) : 0.0;
            Debug.Log($"RequestedScale={SimulationSettings.Instance.TimeScale:F2}, EffectiveScale={effectiveTimeScale:F2}, steps={steps}, dtStep={dtStepDays:F6}d");
        }
#endif

        if (steps <= 0) return;

        NBodyDiagnostics.EnsureInitialized(ref _diagWorkspace, n);

        // Diagnostics require per-step observability => complete per step.
        if (_diagnostics)
        {
            for (int i = 0; i < steps; i++)
            {
                CopyNativeToManaged();
                NBodyDiagnostics.SnapshotBeforeStep(ref _diagWorkspace, _positionsManaged, _velocitiesManaged);

                JobHandle h = ScheduleIntegrateOneStep(dtStepDays, n, default);
                h.Complete();

                SimulationSettings.Instance.AdvanceSimTime(dtStepDays);

                CopyNativeToManaged();
                NBodyDiagnostics.LogAtDailyMidnightBoundaries(
                    ref _diagWorkspace,
                    epochJdTdb: 2460676.5,
                    dtStepDays: dtStepDays,
                    positionsAfter: _positionsManaged,
                    velocitiesAfter: _velocitiesManaged,
                    bodyNameOf: idx => SystemBodies[idx].Data.Name,
                    logBodyIndex: -1
                );
            }

            ApplySimulationStateFromNative();
        }
        else
        {
            // Best performance path: chain all substeps and complete once.
            JobHandle chain = default;

            for (int i = 0; i < steps; i++)
                chain = ScheduleIntegrateOneStep(dtStepDays, n, chain);

            chain.Complete();

            // Advance sim time once, matching what was actually executed.
            SimulationSettings.Instance.AdvanceSimTime(dtAdvancedDays);

            ApplySimulationStateFromNative();
        }
    }

    JobHandle ScheduleIntegrateOneStep(double dtDays, int n, JobHandle dependsOn)
    {
        switch (_gravityModel)
        {
            case GravityModel.Newtonian:
                {
                    // a(t)
                    JobHandle h0 = SpacePhysics3D.NBodyAccelVectorFrom_Schedule(
                        _masses, _positions, _accelerations, dependsOn);

                    // predictor
                    var pred = new PredictorVerletJob
                    {
                        Masses = _masses,
                        Positions = _positions,
                        Velocities = _velocities,
                        Accel = _accelerations,
                        Dt = dtDays,
                        VelocityHalf = _velocityHalf,
                        PositionsNext = _positionsNext
                    };
                    JobHandle h1 = pred.Schedule(h0);

                    // a(t+dt)
                    JobHandle h2 = SpacePhysics3D.NBodyAccelVectorFrom_Schedule(
                        _masses, _positionsNext, _accelerationsNext, h1);

                    // correct
                    var corr = new CorrectorVerletJob
                    {
                        Masses = _masses,
                        VelocityHalf = _velocityHalf,
                        AccelNext = _accelerationsNext,
                        Dt = dtDays,
                        PositionsNext = _positionsNext,
                        Positions = _positions,
                        Velocities = _velocities
                    };
                    return corr.Schedule(h2);
                }

            case GravityModel.EIH:
            default:
                {
                    // a0 = EIH(x0, v0)
                    JobHandle h0 = SpacePhysics3D.Einstein_Infeld_Hoffmann_1PN_Schedule(
                        _positions, _velocities, _masses, _accelerations,
                        _workspaceEIH, _accelBMode, _accelIterations, dependsOn);

                    // predictor (Heun/RK2)
                    var pred = new PredictorHeunJob
                    {
                        Masses = _masses,
                        Positions = _positions,
                        Velocities = _velocities,
                        Accel = _accelerations,
                        Dt = dtDays,
                        VelocityHalf = _velocityHalf,
                        PositionsNext = _positionsNext
                    };
                    JobHandle h1 = pred.Schedule(h0);

                    // a1 = EIH(x1, v1)
                    JobHandle h2 = SpacePhysics3D.Einstein_Infeld_Hoffmann_1PN_Schedule(
                        _positionsNext, _velocityHalf, _masses, _accelerationsNext,
                        _workspaceEIH, _accelBMode, _accelIterations, h1);

                    // correct (Heun/RK2)
                    var corr = new CorrectorHeunJob
                    {
                        Masses = _masses,
                        Positions = _positions,
                        Velocities = _velocities,
                        VelocityHalf = _velocityHalf,
                        Accel0 = _accelerations,
                        Accel1 = _accelerationsNext,
                        Dt = dtDays
                    };
                    return corr.Schedule(h2);
                }
        }
    }

    [BurstCompile(FloatMode = FloatMode.Strict, FloatPrecision = FloatPrecision.High)]
    struct PredictorHeunJob : IJob
    {
        [ReadOnly] public NativeArray<double> Masses;
        [ReadOnly] public NativeArray<double3> Positions;
        [ReadOnly] public NativeArray<double3> Velocities;
        [ReadOnly] public NativeArray<double3> Accel;

        public double Dt;

        public NativeArray<double3> VelocityHalf;
        public NativeArray<double3> PositionsNext;

        public void Execute()
        {
            int n = Masses.Length;
            for (int a = 0; a < n; a++)
            {
                if (Masses[a] <= 0.0)
                {
                    VelocityHalf[a] = double3.zero;
                    PositionsNext[a] = double3.zero;
                    continue;
                }

                double3 v0 = Velocities[a];
                VelocityHalf[a] = v0 + Accel[a] * Dt;
                PositionsNext[a] = Positions[a] + v0 * Dt;
            }
        }
    }

    [BurstCompile(FloatMode = FloatMode.Strict, FloatPrecision = FloatPrecision.High)]
    struct CorrectorHeunJob : IJob
    {
        [ReadOnly] public NativeArray<double> Masses;

        public NativeArray<double3> Positions;
        public NativeArray<double3> Velocities;

        [ReadOnly] public NativeArray<double3> VelocityHalf;
        [ReadOnly] public NativeArray<double3> Accel0;
        [ReadOnly] public NativeArray<double3> Accel1;

        public double Dt;

        public void Execute()
        {
            int n = Masses.Length;
            for (int a = 0; a < n; a++)
            {
                if (Masses[a] <= 0.0)
                {
                    Positions[a] = double3.zero;
                    Velocities[a] = double3.zero;
                    continue;
                }

                double3 v0 = Velocities[a];
                double3 v1 = VelocityHalf[a];

                Velocities[a] = v0 + 0.5 * (Accel0[a] + Accel1[a]) * Dt;
                Positions[a] = Positions[a] + 0.5 * (v0 + v1) * Dt;
            }
        }
    }

    [BurstCompile(FloatMode = FloatMode.Strict, FloatPrecision = FloatPrecision.High)]
    struct PredictorVerletJob : IJob
    {
        [ReadOnly] public NativeArray<double> Masses;
        [ReadOnly] public NativeArray<double3> Positions;
        [ReadOnly] public NativeArray<double3> Velocities;
        [ReadOnly] public NativeArray<double3> Accel;

        public double Dt;

        public NativeArray<double3> VelocityHalf;
        public NativeArray<double3> PositionsNext;

        public void Execute()
        {
            int n = Masses.Length;
            for (int a = 0; a < n; a++)
            {
                if (Masses[a] <= 0.0)
                {
                    VelocityHalf[a] = double3.zero;
                    PositionsNext[a] = double3.zero;
                    continue;
                }

                VelocityHalf[a] = Velocities[a] + 0.5 * Accel[a] * Dt;
                PositionsNext[a] = Positions[a] + VelocityHalf[a] * Dt;
            }
        }
    }

    [BurstCompile(FloatMode = FloatMode.Strict, FloatPrecision = FloatPrecision.High)]
    struct CorrectorVerletJob : IJob
    {
        [ReadOnly] public NativeArray<double> Masses;

        [ReadOnly] public NativeArray<double3> VelocityHalf;
        [ReadOnly] public NativeArray<double3> AccelNext;
        [ReadOnly] public NativeArray<double3> PositionsNext;

        public double Dt;

        public NativeArray<double3> Positions;
        public NativeArray<double3> Velocities;

        public void Execute()
        {
            int n = Masses.Length;
            for (int a = 0; a < n; a++)
            {
                if (Masses[a] <= 0.0)
                {
                    Positions[a] = double3.zero;
                    Velocities[a] = double3.zero;
                    continue;
                }

                Velocities[a] = VelocityHalf[a] + 0.5 * AccelNext[a] * Dt;
                Positions[a] = PositionsNext[a];
            }
        }
    }

    // ---------------------------
    // State sync helpers
    // ---------------------------

    void SnapshotSystemStateToNative()
    {
        int n = SystemBodies.Length;

        for (int a = 0; a < n; a++)
        {
            AstronomicalObject body = SystemBodies[a];

            if (!IsValidAstronomicalBody(body))
            {
                _masses[a] = 0.0;
                _positions[a] = double3.zero;
                _velocities[a] = double3.zero;
                continue;
            }

            _masses[a] = body.Data.Mass;
            _positions[a] = body.Position;
            _velocities[a] = body.Velocity;
        }
    }

    void ApplySimulationStateFromNative()
    {
        int n = SystemBodies.Length;

        for (int a = 0; a < n; a++)
        {
            if (_masses[a] <= 0.0) continue;

            var body = SystemBodies[a];
            if (body == null) continue;

            body.Position = _positions[a];
            body.Velocity = _velocities[a];
        }
    }

    void CopyNativeToManaged()
    {
        int n = _masses.Length;
        for (int a = 0; a < n; a++)
        {
            _positionsManaged[a] = _positions[a];
            _velocitiesManaged[a] = _velocities[a];
        }
    }

    static bool IsValidAstronomicalBody(AstronomicalObject body)
    {
        if (body == null) return false;
        if (body.Data.Mass <= 0.0) return false;
        return true;
    }
}