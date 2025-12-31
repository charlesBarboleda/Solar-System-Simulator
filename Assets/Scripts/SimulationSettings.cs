using UnityEngine;
using System;

public class SimulationSettings : MonoBehaviour
{
    public static SimulationSettings Instance { get; private set; }

    [Header("Scales")]
    [Tooltip("Time scale multiplier. 1.0 = base speed (1 real second = 1 simulation day).")]
    [Min(0)] public double TimeScale = 1.0;

    [Tooltip("Gravity scale multiplier. 1.0 = real gravity.")]
    [Min(0)] public double GravityScale = 1.0;

    [Header("Integration")]
    [Tooltip("Fixed simulation-days per internal physics step. This is your accuracy knob.")]
    [Min(1e-9f)] public double FixedStepSimDays = 0.01; // ~14.4 minutes

    [Tooltip("Hard cap to prevent runaway CPU cost if TimeScale is huge.")]
    [Min(1)] public int MaxSubstepsPerFixedUpdate = 256;

    [Tooltip("Clamp backlog so it doesn't grow without bound if warp > CPU budget.")]
    [Min(0)] public double MaxBacklogSimDays = 10.0; // allow up to 10 sim days of "debt"

    [Header("Simulation Time State")]
    public double SimDays { get; private set; }
    public double SimSeconds => SimDays * PhysicsConstants.REAL_SECONDS_PER_DAY;

    double _simDebtDays; // accumulated sim time waiting to be simulated

    double RequestedSimDaysThisFixedUpdate =>
        Time.fixedDeltaTime * PhysicsConstants.UNITY_DAYS_PER_REAL_SECOND * TimeScale;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        ResetClock();
    }

    public void GetSubstepPlan(out int steps, out double dtStepDays, out double dtAdvancedDays, out double dtRequestedDays)
    {
        dtRequestedDays = RequestedSimDaysThisFixedUpdate;

        if (dtRequestedDays <= 0.0 || FixedStepSimDays <= 0.0)
        {
            steps = 0;
            dtStepDays = 0.0;
            dtAdvancedDays = 0.0;
            return;
        }

        // Accumulate "time we owe" the simulation.
        _simDebtDays += dtRequestedDays;

        // Prevent infinite catch-up debt.
        if (MaxBacklogSimDays > 0.0)
            _simDebtDays = Math.Min(_simDebtDays, MaxBacklogSimDays);

        dtStepDays = FixedStepSimDays;

        long stepsWanted = (long)Math.Floor(_simDebtDays / dtStepDays);
        steps = (int)Math.Clamp(stepsWanted, 0, (long)MaxSubstepsPerFixedUpdate);

        dtAdvancedDays = steps * dtStepDays;
        _simDebtDays -= dtAdvancedDays;
        if (_simDebtDays < 0.0) _simDebtDays = 0.0;

#if UNITY_EDITOR
        if (steps == MaxSubstepsPerFixedUpdate && stepsWanted > steps)
            Debug.LogWarning($"[SimulationSettings] CPU cap hit. Requested={dtRequestedDays:F6}d, Advanced={dtAdvancedDays:F6}d, Debt={_simDebtDays:F6}d");
#endif
    }

    public void AdvanceSimTime(double dtDays)
    {
        SimDays += dtDays;
    }

    void ResetClock()
    {
        SimDays = 0.0;
        _simDebtDays = 0.0;
    }
}
