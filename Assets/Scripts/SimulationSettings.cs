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
    [Tooltip("Maximum simulation-days allowed per internal physics substep. Smaller = more stable at high TimeScale.")]
    [Min(1e-9f)] public double MaxSubstepSimDays = 0.25;

    [Tooltip("Hard cap to prevent runaway CPU cost if TimeScale is huge.")]
    [Min(1)] public int MaxSubstepsPerFixedUpdate = 256;

    // Requested dt for current FixedUpdate (already includes TimeScale)
    public double DeltaSimDays => Time.fixedDeltaTime * PhysicsConstants.UNITY_DAYS_PER_REAL_SECOND * TimeScale;

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

    public void GetSubstepPlan(out int steps, out double dtStepDays, out double dtTotalDays)
    {
        dtTotalDays = DeltaSimDays;

        if (dtTotalDays <= 0.0)
        {
            steps = 0;
            dtStepDays = 0.0;
            return;
        }

        // Compute how many substeps are needed to keep each step <= MaxSubstepSimDays
        steps = (int)Math.Ceiling(dtTotalDays / MaxSubstepSimDays);
        steps = Math.Clamp(steps, 1, MaxSubstepsPerFixedUpdate);

        dtStepDays = dtTotalDays / steps;

#if UNITY_EDITOR
        // if dtStepDays is still too large; log a warning.
        if (steps == MaxSubstepsPerFixedUpdate && dtStepDays > MaxSubstepSimDays)
            Debug.LogWarning($"[SimulationSettings] Substep cap hit. dtTotal={dtTotalDays:F6} days, steps={steps}, dtStep={dtStepDays:F6} days (>{MaxSubstepSimDays}).");
#endif
    }

}
