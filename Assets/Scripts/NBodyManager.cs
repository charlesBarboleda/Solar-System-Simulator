using System;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public class NBodyManager : MonoBehaviour
{
    public static NBodyManager Instance { get; private set; }

    [Header("System Bodies")]
    public List<AstronomicalObject> SystemBodies { get; private set; }

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

    [SerializeField] GameObject _simObjects;

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

    // Pause / Resume
    [field: SerializeField] public bool IsPaused { get; private set; }

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

        SystemBodies = new List<AstronomicalObject>();
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

    void OnDisable()
    {
        DisposeNative();
    }

    void Initialize()
    {
        ReinitializeNativeState();

        if (SystemBodies.Count <= 0)
        {
            SimulationMapManager.Instance.Initialize();
            RenderSpaceManager.Instance.Initialize();
            UIObjectMarker.Instance.InitializeSystem(SystemBodies);
            ApplyVectorManager.Instance.PopulateDropdown();
            AddAssetUIManager.Instance.PopulateDropdown();
            LiveSimulationManager.Instance.Initialize(SystemBodies);
            return;
        }

        SimulationMapManager.Instance.Initialize();
        RenderSpaceManager.Instance.Initialize();
        UIObjectMarker.Instance.InitializeSystem(SystemBodies);
        ApplyVectorManager.Instance.InitializeList();
        AddAssetUIManager.Instance.PopulateDropdown();
        ApplyVectorManager.Instance.PopulateDropdown();
        LiveSimulationManager.Instance.Initialize(SystemBodies);

        Debug.Log($"Initialized NBodyManager with {SystemBodies.Count} bodies");
    }

    void ReinitializeNativeState()
    {
        ValidateSystemBodies();

        int n = SystemBodies.Count;

        DisposeNative();

        if (n <= 0)
        {
            _initialized = false;
            return;
        }

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

        _initialized = n > 0;
    }

    bool TryGetRuntimeObject(string name, out Transform runTimeObjectTransform)
    {
        runTimeObjectTransform = default;

        if (string.IsNullOrEmpty(name)) return false;

        runTimeObjectTransform = _simObjects.transform.Find(name);

        if (runTimeObjectTransform == null) return false;

        return true;
    }

    public bool TryRemoveObjectByName(string name)
    {
        if (!TryGetRuntimeObject(name, out Transform runTimeObjectTransform))
        {
            Debug.LogWarning($"[NBodyManager] TryRemoveObjectByName(): No runtime object found with name {name}");
            return false;
        }


        if (!runTimeObjectTransform.TryGetComponent<AstronomicalObject>(out var astroObject))
        {
            Debug.LogWarning($"[NBodyManager] TryRemoveObjectByName(): Runtime object with name {name} does not have an AstronomicalObject component");
            return false;
        }

        return TryRemoveObject(astroObject);
    }

    public bool TryRemoveObject(AstronomicalObject astroObject)
    {
        if (astroObject == null) return false;

        PauseSimulation();

        string bodyName = astroObject.Data.Body.Name;

        bool removed = SystemBodies.Remove(astroObject);
        if (!removed)
        {
            Debug.LogError($"[NBodyManager] Failed to remove {bodyName} from SystemBodies");
            return false;
        }

        if (!TryGetRuntimeObject(bodyName, out Transform runTimeObjectTransform))
        {
            // Body was in SystemBodies but has no runtime object — still resync
            ReinitializeNativeState();
            RenderSpaceManager.Instance.RemoveSimulationObject(astroObject);
            UIObjectMarker.Instance.RemoveMarker(bodyName);
            ApplyVectorManager.Instance.PopulateDropdown();
            AddAssetUIManager.Instance.PopulateDropdown();
            return false;
        }

        if (astroObject.Data.Body.Type == BodyType.Star)
        {
            astroObject.DestroyFarVisuals();
            astroObject.DestroyLightSource();
        }

        // Only reallocate native state (no full system rebuild)
        ReinitializeNativeState();

        // Dependent system updates
        ApplyVectorManager.Instance.DeleteRowEntry(bodyName);
        SimulationMapManager.Instance.DestroyMapObject(bodyName);
        RenderSpaceManager.Instance.RemoveSimulationObject(astroObject);
        UIObjectMarker.Instance.RemoveMarker(bodyName);
        ApplyVectorManager.Instance.PopulateDropdown();
        AddAssetUIManager.Instance.PopulateDropdown();
        LiveSimulationManager.Instance.RemoveEntry(bodyName);

        Destroy(runTimeObjectTransform.gameObject);

        UIMessage.Instance.NewFadingMessage(MessageType.Success, $"Removed {bodyName} from simulation", 7.5f);
        return true;
    }

    public bool TryAddObject(AstronomicalObject astroObject)
    {
        if (astroObject == null) return false;

        string name = astroObject.Data.Body.Name;

        if (!SimulationAssetDatabaseManager.Instance.TryGetBodyByName(name, out _))
        {
            UIMessage.Instance.NewUIMessage(MessageType.Error, $"Failed to add {name} to simulation, could not find body in asset database", "Failed Task");
            return false;
        }

        for (int i = 0; i < SystemBodies.Count; i++)
        {
            if (SystemBodies[i].Data.Body.Name == name)
            {
                UIMessage.Instance.NewUIMessage(MessageType.Error, $"Failed to add {name}, already exists in simulation!", "Failed Task");
                return false;
            }
        }

        if (IsPositionOccupied(astroObject, astroObject.Position))
        {
            UIMessage.Instance.NewUIMessage(MessageType.Error, $"Failed to add {name}, spawn position is occupied.", "Failed Task");
            return false;
        }

        PauseSimulation();

        SystemBodies.Add(astroObject);
        astroObject.gameObject.transform.SetParent(_simObjects.transform);
        astroObject.gameObject.SetActive(true);

        // Only reallocate native state (no full system rebuild)
        ReinitializeNativeState();

        // Dependent system updates
        ApplyVectorManager.Instance.CreateNewRowEntry(astroObject, SystemBodies.Count);
        SimulationMapManager.Instance.CreateMapObject(astroObject);
        RenderSpaceManager.Instance.AddSimulationObject(astroObject);
        UIObjectMarker.Instance.AddMarker(astroObject);
        ApplyVectorManager.Instance.PopulateDropdown();
        AddAssetUIManager.Instance.PopulateDropdown();
        LiveSimulationManager.Instance.CreateNewEntry(astroObject, SystemBodies.Count);

        if (astroObject.Data.Body.Type == BodyType.Star)
            LightFactory.Instance.TryCreateLightSource(astroObject, out _);

        UIMessage.Instance.NewFadingMessage(MessageType.Success, $"Added {name} to simulation", 7.5f);
        return true;
    }


    public void PauseSimulation() => IsPaused = true;

    public void StartSimulation() => IsPaused = false;

    public bool TrySetObjectPosition(AstronomicalObject astroObject, double3 position, SimulationObject relativeTo = null)
    {
        if (!_initialized || astroObject == null) return false;

        int index = FindBodyIndex(astroObject);
        if (index < 0)
        {
            Debug.LogWarning($"[NBodyManager] TrySetObjectPosition(): {astroObject.name} not found in SystemBodies.");
            return false;
        }

        double3 globalPosition = relativeTo != null ? relativeTo.Position + position : position;

        if (IsPositionOccupied(astroObject, globalPosition))
        {
            UIMessage.Instance.NewUIMessage(MessageType.Error, $"Failed to move {astroObject.Data.Body.Name}, target position is occupied.", "Teleport Blocked");
            return false;
        }

        PauseSimulation();
        _positions[index] = globalPosition;
        astroObject.Position = globalPosition;
        return true;
    }

    public bool TrySetObjectVelocity(AstronomicalObject astroObject, double3 velocity)
    {
        if (!_initialized || astroObject == null) return false;

        int index = FindBodyIndex(astroObject);
        if (index < 0)
        {
            Debug.LogWarning($"[NBodyManager] TrySetObjectVelocity(): {astroObject.name} not found in SystemBodies.");
            return false;
        }

        PauseSimulation();
        _velocities[index] = velocity;
        astroObject.Velocity = velocity;
        return true;
    }

    public SimulationSaveData CreateSaveData()
    {
        var save = new SimulationSaveData
        {
            IsPaused = IsPaused,
            GravityModel = _gravityModel,
            AccelBMode = _accelBMode,
            AccelIterations = _accelIterations,
            CurrentSimDays = SimulationSettings.Instance.SimDays,
            TimeScale = SimulationSettings.Instance.TimeScale,
            GravityScale = SimulationSettings.Instance.GravityScale,
            FixedStepSimDays = SimulationSettings.Instance.FixedStepSimDays,
            MaxSubstepsPerFixedUpdate = SimulationSettings.Instance.MaxSubstepsPerFixedUpdate,
            MaxBacklogSimDays = SimulationSettings.Instance.MaxBacklogSimDays
        };

        int n = SystemBodies.Count;
        for (int i = 0; i < n; i++)
        {
            if (_masses[i] <= 0.0) continue;

            var body = SystemBodies[i];
            if (body == null) continue;

            save.Bodies.Add(new BodyStateData
            {
                Name = body.Data.Body.Name,
                Position = new Double3DTO(_positions[i]),
                Velocity = new Double3DTO(_velocities[i]),
                Rotation = body.transform.rotation
            });
        }

        return save;
    }

    public void ApplyLoadedState(SimulationSaveData save)
    {
        if (!_initialized) Initialize();
        SystemBodies.Clear();
        SimulationSettings.Instance.ResetClock();

        _gravityModel = save.GravityModel;
        _accelBMode = save.AccelBMode;
        _accelIterations = save.AccelIterations;
        IsPaused = save.IsPaused;
        SimulationSettings.Instance.SetTimeScale(save.TimeScale);
        SimulationSettings.Instance.SetGravityScale(save.GravityScale);
        SimulationSettings.Instance.SetFixedStepSimDays(save.FixedStepSimDays);
        SimulationSettings.Instance.SetMaxSubstepsPerFixedUpdate(save.MaxSubstepsPerFixedUpdate);
        SimulationSettings.Instance.SetMaxBacklogSimDays(save.MaxBacklogSimDays);
        SimulationSettings.Instance.SetSimDays(save.CurrentSimDays);

        foreach (var bodyState in save.Bodies)
        {
            if (!SimulationAssetDatabaseManager.Instance.TryGetBodyByName(bodyState.Name, out Data objectData))
            {
                UIMessage.Instance.NewFadingMessage(MessageType.Error, $"Failed to load body {bodyState.Name} from save data, no matching body found in asset database", 8f);
                continue;
            }

            AstronomicalObject astroObject = AstronomicalObjectFactory.Instance.CreateAstronomicalObject(objectData);

            astroObject.Position = bodyState.Position.ToDouble3();
            astroObject.Velocity = bodyState.Velocity.ToDouble3();
            astroObject.transform.rotation = bodyState.Rotation;

            TryAddObject(astroObject);

        }

        Initialize();
    }

    public bool TryGetAstroObjectByName(string name, out AstronomicalObject astroObject)
    {
        astroObject = null;

        foreach (AstronomicalObject astroObj in SystemBodies)
        {
            if (string.Equals(astroObj.Data.Body.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                astroObject = astroObj;
                return true;
            }
        }

        return false;
    }

    public double GetSafeDistanceBetweenObjects(SimulationObject a, SimulationObject b)
    {
        if (a == null || b == null) return 0.0;

        double padding = PhysicsConstants.ToUnityUnitsFromKM(50000.0);

        return a.GetCollisionRadius() + b.GetCollisionRadius() + padding;
    }

    public bool IsPositionOccupied(SimulationObject requester, double3 targetPosition, SimulationObject exclude = null)
    {
        if (SystemBodies == null || SystemBodies.Count <= 0) return false;

        for (int i = 0; i < SystemBodies.Count; i++)
        {
            AstronomicalObject body = SystemBodies[i];

            if (body == null) continue;
            if (ReferenceEquals(body, requester)) continue;
            if (exclude != null && ReferenceEquals(body, exclude)) continue; // skip target

            double safeDistance = GetSafeDistanceBetweenObjects(requester, body);
            double distanceSq = math.distancesq(targetPosition, body.Position);

            if (distanceSq < safeDistance * safeDistance)
            {
                UIMessage.Instance.NewUIMessage(MessageType.Error, $"Target position intersects with {body.Data.Body.Name}.", "Teleport Blocked");
                return true;
            }
        }

        if (MovementController.Instance != null && !ReferenceEquals(MovementController.Instance, requester))
        {
            double playerSafeDistance = GetSafeDistanceBetweenObjects(requester, MovementController.Instance);
            double playerDistanceSq = math.distancesq(targetPosition, MovementController.Instance.Position);

            if (playerDistanceSq < playerSafeDistance * playerSafeDistance)
            {
                UIMessage.Instance.NewUIMessage(MessageType.Error, $"Target position intersects with your position.", "Teleport Blocked");
                return true;
            }
        }

        return false;
    }

    void ValidateSystemBodies()
    {
        for (int i = SystemBodies.Count - 1; i >= 0; i--)
        {
            var body = SystemBodies[i];

            if (body == null)
            {
                SystemBodies.RemoveAt(i);
                continue;
            }

            string name = body.Data.Body.Name;

            if (!SimulationAssetDatabaseManager.Instance.TryGetBodyByName(name, out _))
            {
                Debug.LogWarning($"[NBodyManager] Removing invalid body not in database: {name}");
                SystemBodies.RemoveAt(i);
            }
        }
    }

    int FindBodyIndex(SimulationObject simObject)
    {
        int n = SystemBodies?.Count ?? 0;
        for (int i = 0; i < n; i++)
        {
            if (ReferenceEquals(SystemBodies[i], simObject)) return i;
        }

        return -1;
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

        // Dispose workspace safely (avoids potential double-dispose issues if workspace Dispose has bugs).
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
        if (IsPaused) return;

        int n = SystemBodies?.Count ?? 0;
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

        // Diagnostics require per-step observability -> complete per step
        if (_diagnostics)
        {
            for (int i = 0; i < steps; i++)
            {
                CopyNativeToManaged();
                NBodyDiagnostics.SnapshotBeforeStep(ref _diagWorkspace, _positionsManaged, _velocitiesManaged);

                JobHandle h = ScheduleIntegrateOneStep(dtStepDays, n, default);
                h.Complete();

                SimulationSettings.Instance.AdvanceSimTime(dtStepDays);
                AdvanceBodyRotations(dtStepDays);

                CopyNativeToManaged();
                NBodyDiagnostics.LogAtDailyMidnightBoundaries(
                    ref _diagWorkspace,
                    epochJdTdb: 2460676.5,
                    dtStepDays: dtStepDays,
                    positionsAfter: _positionsManaged,
                    velocitiesAfter: _velocitiesManaged,
                    bodyNameOf: idx => SystemBodies[idx].Data.Body.Name,
                    logBodyIndex: -1
                );
            }

            ApplySimulationStateFromNative();
        }
        else
        {
            // Best performance path: chain all substeps and complete once
            JobHandle chain = default;

            for (int i = 0; i < steps; i++)
                chain = ScheduleIntegrateOneStep(dtStepDays, n, chain);

            chain.Complete();

            // Advance sim time once, matching what was actually executed
            SimulationSettings.Instance.AdvanceSimTime(dtAdvancedDays);
            AdvanceBodyRotations(dtAdvancedDays);

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

    // State sync helpers
    void SnapshotSystemStateToNative()
    {
        int n = SystemBodies.Count;

        for (int a = 0; a < n; a++)
        {
            AstronomicalObject bodyObject = SystemBodies[a];

            if (!IsValidAstronomicalBody(bodyObject))
            {
                _masses[a] = 0.0;
                _positions[a] = double3.zero;
                _velocities[a] = double3.zero;
                continue;
            }

            _masses[a] = bodyObject.Data.Body.Mass;
            _positions[a] = bodyObject.Position;
            _velocities[a] = bodyObject.Velocity;
        }
    }

    void ApplySimulationStateFromNative()
    {
        for (int i = 0; i < SystemBodies.Count; i++)
        {
            if (_masses[i] <= 0.0) continue;

            AstronomicalObject body = SystemBodies[i];
            if (body == null) continue;

            body.Position = _positions[i];
            body.Velocity = _velocities[i];
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

    static bool IsValidAstronomicalBody(AstronomicalObject bodyObject)
    {
        if (bodyObject == null) return false;
        if (bodyObject.Data.Body.Mass <= 0.0) return false;
        return true;
    }

    void AdvanceBodyRotations(double dtDays)
    {
        int n = SystemBodies?.Count ?? 0;
        for (int i = 0; i < n; i++)
        {
            AstronomicalObject bodyObject = SystemBodies[i];
            if (bodyObject == null) continue;

            bodyObject.AdvanceRotationBySimDays(dtDays);
        }
    }
}