using UnityEngine;
using System;

public enum SimulationSteppingPresets
{
    Performance, // Low CPU cost at the cost of accuracy. Good for fast-forwarding or low-end hardware.
    Balanced,   // A reasonable balance of accuracy and CPU cost. Good for general use.
    Precision,  // Higher accuracy at the cost of increased CPU usage. Good for close-up views or slow-motion.
    UltraPrecision, // Maximum accuracy with no regard for CPU cost. Good for debugging or very short simulations.
    FastForward, // Aggressive time stepping for quickly advancing long simulations. Accuracy is not guaranteed and may produce non-physical results, but it will never produce unstable results that blow up to infinity. Good for fast-forwarding through long periods of time.
    Cinematic // Similar to FastForward preset, but with a special focus on maintaining visual stability and avoiding sudden jumps in object trajectories. Uses dynamic time stepping that adjusts based on the current simulation state to try to keep motion smooth and visually coherent, even if it means sacrificing some accuracy or speed. Good for cinematic fly-throughs or visualizations where smooth motion is more important than physical accuracy.
}

public class SimulationSettings : MonoBehaviour
{
    public static SimulationSettings Instance { get; private set; }

    [Header("Scales")]
    [Tooltip("Time scale multiplier. 1.0 = base speed (1 real second = 1 simulation day)")]
    [Min(0)] public double TimeScale { get; private set; }

    [Tooltip("Gravity scale multiplier. 1.0 = real gravity.")]
    [Min(0)] public double GravityScale { get; private set; }

    [Header("Integration")]
    [Tooltip("Fixed simulation-days per internal physics step.")]
    [Min(1e-9f)] public double FixedStepSimDays { get; private set; }

    [Tooltip("Hard cap to prevent runaway CPU cost if TimeScale is huge.")]
    [Min(1)] public int MaxSubstepsPerFixedUpdate { get; private set; }

    [Tooltip("Clamp backlog so it doesn't grow without bound if warp > CPU budget.")]
    [Min(0)] public double MaxBacklogSimDays { get; private set; }

    DateTime _dateTime;
    DateTime _dateTimeStart;

    public double SimDays { get; private set; }
    public double SimSeconds => SimDays * PhysicsConstants.REAL_SECONDS_PER_DAY;

    // Accumulated sim time waiting to be simulated.
    double _simDebtDays;

    bool _cpuCapWarningShown;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadDefaultSettings();
    }

    void LoadDefaultSettings()
    {
        HandleSimulationSteppingPreset(SimulationSteppingPresets.Balanced, out double fixedStep, out int maxSubstep, out double maxBacklog);

        TimeScale = 1;
        GravityScale = 1;
        FixedStepSimDays = fixedStep;
        MaxSubstepsPerFixedUpdate = maxSubstep;
        MaxBacklogSimDays = maxBacklog;

        UIMessage.Instance.NewFadingMessage(MessageType.Info, "Loaded Default Simulation Settings", 8f);
    }

    public void HandleSimulationSteppingPreset(SimulationSteppingPresets preset, out double fixedStep, out int maxSubstep, out double maxBacklog)
    {
        switch (preset)
        {
            case SimulationSteppingPresets.Performance:
                fixedStep = 1.0 / 720;
                maxSubstep = 32;
                maxBacklog = 0.1;
                break;
            case SimulationSteppingPresets.Balanced:
                fixedStep = 1.0 / 1440.0;
                maxSubstep = 64;
                maxBacklog = 0.25;
                break;
            case SimulationSteppingPresets.Precision:
                fixedStep = 1.0 / 8640.0;
                maxSubstep = 128;
                maxBacklog = 0.5;
                break;
            case SimulationSteppingPresets.UltraPrecision:
                fixedStep = 1e-5f;
                maxSubstep = 256;
                maxBacklog = 1;
                break;
            case SimulationSteppingPresets.FastForward:
                fixedStep = 0.01;
                maxSubstep = 128;
                maxBacklog = 2;
                break;
            case SimulationSteppingPresets.Cinematic:
                fixedStep = 0.05;
                maxSubstep = 16;
                maxBacklog = 0;
                break;
            default:
                fixedStep = 1.0 / 1440.0;
                maxSubstep = 64;
                maxBacklog = 0.25;
                break;
        }
    }

    public void SetMaxBacklogSimDays(double maxBacklogSimDays)
    {
        if (maxBacklogSimDays < 0)
        {
            UIMessage.Instance.NewFadingMessage(MessageType.Error, "Max Backlog Sim Days cannot be set to < 1)");
            return;
        }

        SimulationSettingsUIManager.Instance.SetPlaceholders();
        MaxBacklogSimDays = maxBacklogSimDays;
        UIMessage.Instance.NewFadingMessage(MessageType.Success, $"Max Backlog Sim Days set to: {MaxBacklogSimDays}");
    }

    public void SetMaxSubstepsPerFixedUpdate(int maxSubstepsPerFixedUpdate)
    {
        if (maxSubstepsPerFixedUpdate < 1)
        {
            UIMessage.Instance.NewFadingMessage(MessageType.Error, "Max Substeps Per Fixed Update cannot be set to < 1)");
            return;
        }

        SimulationSettingsUIManager.Instance.SetPlaceholders();
        MaxSubstepsPerFixedUpdate = maxSubstepsPerFixedUpdate;
        UIMessage.Instance.NewFadingMessage(MessageType.Success, $"Max Substeps Per Fixed Update set to: {MaxSubstepsPerFixedUpdate}");
    }

    public void SetFixedStepSimDays(double fixedStepSimDays)
    {
        if (fixedStepSimDays < 1e-9)
        {
            UIMessage.Instance.NewFadingMessage(MessageType.Error, "Fixed Step Sim Days cannot be set to < 1e-9");
            return;
        }

        SimulationSettingsUIManager.Instance.SetPlaceholders();
        FixedStepSimDays = fixedStepSimDays;
        UIMessage.Instance.NewFadingMessage(MessageType.Success, $"Fixed Step Sim Days set to: {FixedStepSimDays}");
    }

    public void SetGravityScale(double gravityScale)
    {
        if (gravityScale < 0)
        {
            UIMessage.Instance.NewFadingMessage(MessageType.Error, "Gravity Scale cannot be set to < 0");
            return;
        }

        SimulationSettingsUIManager.Instance.SetPlaceholders();
        GravityScale = gravityScale;
        UIMessage.Instance.NewFadingMessage(MessageType.Success, $"Gravity Scale set to: {GravityScale}", 10f);
    }

    public void SetTimeScale(double timeScale)
    {
        if (timeScale < 0)
        {
            UIMessage.Instance.NewFadingMessage(MessageType.Error, "Time Scale cannot be set to < 0");
            return;
        }

        SimulationSettingsUIManager.Instance.SetPlaceholders();
        TimeScale = timeScale;
        UIMessage.Instance.NewFadingMessage(MessageType.Success, $"Time Scale set to: {TimeScale}", 10f);
    }

    public void GetSubstepPlan(out int steps, out double dtStepDays, out double dtAdvancedDays, out double dtRequestedDays)
    {
        GetSubstepPlan(Time.fixedDeltaTime, out steps, out dtStepDays, out dtAdvancedDays, out dtRequestedDays);
    }

    public void GetSubstepPlan(double realDeltaSeconds, out int steps, out double dtStepDays, out double dtAdvancedDays, out double dtRequestedDays)
    {
        // Convert real time -> sim days
        // 1 real second = UNITY_DAYS_PER_REAL_SECOND simulation days at TimeScale = 1
        dtRequestedDays = realDeltaSeconds * PhysicsConstants.UNITY_DAYS_PER_REAL_SECOND * TimeScale;

        // Early out
        if (dtRequestedDays <= 0.0 || FixedStepSimDays <= 0.0)
        {
            steps = 0;
            dtStepDays = 0.0;
            dtAdvancedDays = 0.0;
            return;
        }

        // Accumulate sim debt
        _simDebtDays += dtRequestedDays;

        // Prevent infinite catch-up debt
        if (MaxBacklogSimDays > 0.0 && _simDebtDays > MaxBacklogSimDays)
            _simDebtDays = MaxBacklogSimDays;

        dtStepDays = FixedStepSimDays;

        // Since values are positive, (long)(x / y) is floor(x / y) without Math.Floor overhead
        long stepsWanted = (long)(_simDebtDays / dtStepDays);

        // Clamp to CPU budget
        if (stepsWanted <= 0)
        {
            steps = 0;
            dtAdvancedDays = 0.0;
            return;
        }

        if (stepsWanted > MaxSubstepsPerFixedUpdate) steps = MaxSubstepsPerFixedUpdate;
        else steps = (int)stepsWanted;

        dtAdvancedDays = steps * dtStepDays;

        // Reduce debt
        _simDebtDays -= dtAdvancedDays;
        if (_simDebtDays < 0.0) _simDebtDays = 0.0;

        bool cpuCapped = stepsWanted > MaxSubstepsPerFixedUpdate;

        if (cpuCapped)
        {
            steps = MaxSubstepsPerFixedUpdate;

            if (!_cpuCapWarningShown)
            {
                _cpuCapWarningShown = true;

                UIMessage.Instance.NewFadingMessage(MessageType.Warning, $"CPU cap reached. Advanced {dtAdvancedDays:F6}d of {dtRequestedDays:F6}d requested.");
                UIMessage.Instance.NewFadingMessage(MessageType.Info, $"Time Scale: {TimeScale:F2}, Max Substeps: {MaxSubstepsPerFixedUpdate}");
                UIMessage.Instance.NewFadingMessage(MessageType.Info, $"Sim Debt: {_simDebtDays:F6}d, Max Backlog: {MaxBacklogSimDays:F6}d");
                UIMessage.Instance.NewFadingMessage(MessageType.Warning, "Lower Time Scale or raise Max Substeps to reduce this warning.");
            }
        }
        else
        {
            _cpuCapWarningShown = false;
        }
    }

    public void AdvanceSimTime(double dtDays) => SimDays += dtDays;

    public void SetCurrentDateTime(DateTime dateTime)
    {
        ResetClock();
        _dateTime = dateTime;
        UIMessage.Instance.NewFadingMessage(MessageType.Info, $"Current DateTime set to: {_dateTime}");
    }

    public void SetCurrentDateTime(int year, int month, int day, int hour, int minute, int second, int millisecond)
    {
        ResetClock();
        _dateTime = new DateTime(year, month, day, hour, minute, second, millisecond);
        UIMessage.Instance.NewFadingMessage(MessageType.Info, $"Current DateTime set to: {_dateTime}");
    }

    public void SetCurrentDateTime(DateTimeOffset dateTimeOffset)
    {
        ResetClock();
        _dateTime = dateTimeOffset.DateTime;
        UIMessage.Instance.NewFadingMessage(MessageType.Info, $"Current DateTime set to: {_dateTime}");
    }

    public void SetStartDateTime(DateTime dateTime)
    {
        ResetClock();
        _dateTimeStart = dateTime;
        UIMessage.Instance.NewFadingMessage(MessageType.Info, $"Start DateTime set to: {_dateTimeStart}");
    }

    public void SetStartDateTime(int year, int month, int day, int hour, int minute, int second, int millisecond)
    {
        ResetClock();
        _dateTimeStart = new DateTime(year, month, day, hour, minute, second, millisecond);
        UIMessage.Instance.NewFadingMessage(MessageType.Info, $"Start DateTime set to: {_dateTimeStart}");
    }


    public void SetStartDateTime(DateTimeOffset dateTimeOffset)
    {
        ResetClock();
        _dateTimeStart = dateTimeOffset.DateTime;
        UIMessage.Instance.NewFadingMessage(MessageType.Info, $"Start DateTime set to: {_dateTimeStart}");
    }
    public void SetSimDays(double simDays)
    {
        ResetClock();
        SimDays = simDays;
        UIMessage.Instance.NewFadingMessage(MessageType.Info, $"Sim Days set to: {SimDays}");
    }

    public DateTime GetCurrentDateTime() => _dateTimeStart.AddDays(SimDays);

    public void ResetClock()
    {
        SimDays = 0.0;
        _simDebtDays = 0.0;
    }

    public void PlaySimulation()
    {
        if (!NBodyManager.Instance.IsPaused)
        {
            UIMessage.Instance.NewFadingMessage(MessageType.Info, "The simulation is already running!");
            return;
        }

        UIMessage.Instance.NewFadingMessage(MessageType.Info, "Resumed Simulation");
        NBodyManager.Instance.StartSimulation();
    }

    public void PauseSimulation()
    {
        if (NBodyManager.Instance.IsPaused)
        {
            UIMessage.Instance.NewFadingMessage(MessageType.Info, "The simulation is already paused!");
            return;
        }

        UIMessage.Instance.NewFadingMessage(MessageType.Info, "Paused Simulation");
        NBodyManager.Instance.PauseSimulation();
    }
}