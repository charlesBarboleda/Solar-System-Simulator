using UnityEngine;

public class SimulationSettings : MonoBehaviour
{
    public static SimulationSettings Instance { get; private set; }

    [Header("Scales")]
    [Tooltip("Time scale multiplier for the simulation. 1.0 = base speed (1 real second = 1 simulation day).")]
    [Min(0)] public double TimeScale = 1.0;
    public double DeltaSimDays => Time.fixedDeltaTime * PhysicsConstants.UNITY_DAYS_PER_REAL_SECOND * TimeScale;


    [Tooltip("Gravity scale multiplier for the simulation. 1.0 = real gravity. Value should be kept reasonable to avoid instability (<100).")]
    [Min(0)] public double GravityScale = 1.0;


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

}
