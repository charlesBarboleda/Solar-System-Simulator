using UnityEngine;
using System;

public class SimulationSettings : MonoBehaviour
{
    public static SimulationSettings Instance { get; private set; }

    [Header("Scales")]
    [Tooltip("Time scale multiplier. 1.0 = base speed (1 real second = 1 simulation day)")]
    [Min(0)] public double TimeScale = 1.0;

    [Tooltip("Gravity scale multiplier. 1.0 = real gravity.")]
    [Min(0)] public double GravityScale = 1.0;

    [Header("Integration")]
    [Tooltip("Fixed simulation-days per internal physics step")]
    [Min(1e-9f)] public double FixedStepSimDays = 0.01; // ~14.4 minutes

    [Tooltip("Hard cap to prevent runaway CPU cost if TimeScale is huge")]
    [Min(1)] public int MaxSubstepsPerFixedUpdate = 256;

    [Tooltip("Clamp backlog so it doesn't grow without bound if warp > CPU budget")]
    [Min(0)] public double MaxBacklogSimDays = 10.0; // allow up to 10 sim days of "debt"

    [Header("Simulation Time State")]
    [SerializeField] int _startYear;
    [SerializeField] int _startMonth;
    [SerializeField] int _startDay;
    [SerializeField] int _startHour;
    [SerializeField] int _startMinute;
    [SerializeField] int _startSecond;
    [SerializeField] int _startMillisecond;

    DateTime _dateTime;
    DateTime _dateTimeStart;

    public double SimDays { get; private set; }
    public double SimSeconds => SimDays * PhysicsConstants.REAL_SECONDS_PER_DAY;

    // Accumulated sim time waiting to be simulated.
    double _simDebtDays;

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
        SetStartDateTime(
            _startYear,
            _startMonth,
            _startDay,
            _startHour,
            _startMinute,
            _startSecond,
            _startMillisecond
        );

        _dateTime = _dateTimeStart;
        ResetClock();
    }

    public void GetSubstepPlan(out int steps, out double dtStepDays, out double dtAdvancedDays, out double dtRequestedDays)
    {
        GetSubstepPlan(Time.fixedDeltaTime, out steps, out dtStepDays, out dtAdvancedDays, out dtRequestedDays);
    }

    public void GetSubstepPlan(double realDeltaSeconds, out int steps, out double dtStepDays, out double dtAdvancedDays, out double dtRequestedDays)
    {
        // Convert real time -> sim days
        // 1 real second = UNITY_DAYS_PER_REAL_SECOND simulation days at TimeScale=1
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

        // Since values are positive, (long)(x / y) is floor(x/y) without Math.Floor overhead
        long stepsWanted = (long)(_simDebtDays / dtStepDays);

        // Clamp to CPU budget
        if (stepsWanted <= 0)
        {
            steps = 0;
            dtAdvancedDays = 0.0;
            return;
        }

        if (stepsWanted > MaxSubstepsPerFixedUpdate)
            steps = MaxSubstepsPerFixedUpdate;
        else
            steps = (int)stepsWanted;

        dtAdvancedDays = steps * dtStepDays;

        // Reduce debt
        _simDebtDays -= dtAdvancedDays;
        if (_simDebtDays < 0.0) _simDebtDays = 0.0;

#if UNITY_EDITOR
        if (steps == MaxSubstepsPerFixedUpdate && stepsWanted > steps)
        {
            Debug.LogWarning(
                $"[SimulationSettings] CPU cap reached. Requested={dtRequestedDays:F6}d, Advanced={dtAdvancedDays:F6}d, Debt={_simDebtDays:F6}d");
        }
#endif
    }

    public void AdvanceSimTime(double dtDays) => SimDays += dtDays;

    public void SetCurrentDateTime(DateTime dateTime) => _dateTime = dateTime;

    public void SetCurrentDateTime(int year, int month, int day, int hour, int minute, int second, int millisecond)
    {
        _dateTime = new DateTime(year, month, day, hour, minute, second, millisecond);
    }

    public void SetCurrentDateTime(DateTimeOffset dateTimeOffset) => _dateTime = dateTimeOffset.DateTime;

    public void SetStartDateTime(DateTime dateTime) => _dateTimeStart = dateTime;

    public void SetStartDateTime(int year, int month, int day, int hour, int minute, int second, int millisecond)
    {
        _dateTimeStart = new DateTime(year, month, day, hour, minute, second, millisecond);
    }

    public void SetStartDateTime(DateTimeOffset dateTimeOffset) => _dateTimeStart = dateTimeOffset.DateTime;

    public DateTime GetCurrentDateTime() => _dateTimeStart.AddDays(SimDays);

    public void ResetClock()
    {
        SimDays = 0.0;
        _simDebtDays = 0.0;
    }
}