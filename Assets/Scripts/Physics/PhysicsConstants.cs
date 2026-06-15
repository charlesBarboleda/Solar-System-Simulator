using Unity.Mathematics;
using UnityEngine;

public static class PhysicsConstants
{
    // --- SI Real-world constants (base reference units) ---
    private const double REAL_G_SI = 6.67430e-11;    // m^3 / (kg s^2)
    public const double REAL_SPEED_OF_LIGHT_M_PER_S = 299792458.0;    // m / s
    public const double REAL_SECONDS_PER_DAY = 86400;

    // --- Real-world conversion constants (SI <-> astro) ---
    public const double REAL_METERS_PER_AU = 1.495978707e11; // m in 1 AU
    public const double REAL_KG_PER_SOLAR_MASS = 1.98847e30;  // kg in 1 solar mass

    // --- Unity world-space scaling (3D sim) ---
    public const double UNITY_UNITS_PER_AU = 20000;   // DEFAULT VALUE: 20000 Unity world-space units ≈ 1 AU
    public const double UNITY_METERS_PER_UNIT = REAL_METERS_PER_AU / UNITY_UNITS_PER_AU;  // meters/Unity unit: 1 Unity unit ≈ (1/20000) AU
    public const double UNITY_SIZE_SCALE_FACTOR = 1.0;

    // --- Unity time scaling ---
    public const double UNITY_DAYS_PER_REAL_SECOND = 1.0;

    // --- Unity converted SI real-world constants ---
    private const double UNITY_G_BASE = REAL_G_SI * (REAL_SECONDS_PER_DAY * REAL_SECONDS_PER_DAY) / (UNITY_METERS_PER_UNIT * UNITY_METERS_PER_UNIT * UNITY_METERS_PER_UNIT);

    // Converted scaled G (gravity) constant
    public static double UNITY_G => UNITY_G_BASE * SimulationSettings.Instance.GravityScale;
    // Converted speed of light constant
    public const double UNITY_SPEED_OF_LIGHT = REAL_SPEED_OF_LIGHT_M_PER_S * (REAL_SECONDS_PER_DAY / UNITY_METERS_PER_UNIT);

    // --- Real-world planetary body diameters in meters ---
    public const double REAL_SUN_DIAMETER_M = 1391400000;
    public const double REAL_EARTH_DIAMETER_M = 12742000;
    public const double REAL_NEPTUNE_DIAMETER_M = 49528000;

    // --- Real-world planetery distances from the sun in AU ---
    public const double REAL_MERCURY_SUN_DISTANCE_AU = 0.4;
    public const double REAL_VENUS_SUN_DISTANCE_AU = 0.7;
    public const double REAL_EARTH_SUN_DISTANCE_AU = 1;
    public const double REAL_MARS_SUN_DISTANCE_AU = 1.5;
    public const double REAL_ASTEROIDBELT_SUN_DISTANCE_AU = 2.8;
    public const double REAL_JUPITER_SUN_DISTANCE_AU = 5.2;
    public const double REAL_SATURN_SUN_DISTANCE_AU = 9.6;
    public const double REAL_URANUS_SUN_DISTANCE_AU = 19.2;
    public const double REAL_NEPTUNE_SUN_DISTANCE_AU = 30;

    // -- Real-world miscellaneous measurements
    public const double REAL_SATURN_MIN_RING_DISTANCE_FROM_CENTER_M = 71708500;
    public const double REAL_SATURN_MAX_RING_DISTANCE_FROM_CENTER_M = 140180000;


    // -- Real-world planetary body mass in kg
    public const double REAL_SOLAR_MASS_KG = REAL_KG_PER_SOLAR_MASS;
    public const double REAL_EARTH_MASS_KG = 5.9722e24;

    // --- GUARDS ---
    public const double UNITY_MIN_DISTANCE = 1e-4;

    // --- HELPERS ---
    // AU -> Unity units
    public static double ToUnityUnitsFromAU(double au) => au * UNITY_UNITS_PER_AU;

    // m -> Unity units
    public static double ToUnityUnitsFromM(double m) => m / UNITY_METERS_PER_UNIT;

    // km -> m -> Unity units
    public static double ToUnityUnitsFromKM(double km) => (km * 1000.0) / UNITY_METERS_PER_UNIT;

    // km/s -> Unity units/day
    public static double ToUnityUnitsFromKMPerSec(double kmps) => (kmps * 1000.0) / UNITY_METERS_PER_UNIT * REAL_SECONDS_PER_DAY;

    // Unity units -> m -> km
    public static double ToKMFromUnityUnits(double unityUnits) => (unityUnits * UNITY_METERS_PER_UNIT) / 1000.0;

    // Unity units -> m 
    public static double ToMFromUnityUnits(double unityUnits) => unityUnits * UNITY_METERS_PER_UNIT;

    // Unity units -> AU
    public static double ToAUFromUnityUnits(double unityUnits) => unityUnits / UNITY_UNITS_PER_AU;

    // Calculate distance between two coordinates in KM
    public static double GetDistanceKM(double3 worldCoordinate1, double3 worldCoordinate2)
    {
        double distKM = 0.0;

        Vector3 coordinate1 = (Vector3)(float3)worldCoordinate1;
        Vector3 coordinate2 = (Vector3)(float3)worldCoordinate2;

        float worldDist = math.length(coordinate1 - coordinate2);
        if (worldDist < 0.0001) return distKM;

        distKM = ToKMFromUnityUnits(worldDist);
        return distKM;
    }

    // Calculate distance between two SimulationObject's center in KM
    public static double GetCenterDistanceKM(SimulationObject object1, SimulationObject object2)
    {
        double distKM = 0.0;

        Vector3 obj1Pos = (Vector3)(float3)object1.GetGlobalPosition();
        Vector3 obj2Pos = (Vector3)(float3)object2.GetGlobalPosition();

        float worldDist = math.length(obj1Pos - obj2Pos);
        if (worldDist < 0.0001) return distKM;

        distKM = ToKMFromUnityUnits(worldDist);
        return distKM;
    }
}
