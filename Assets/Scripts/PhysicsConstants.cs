public static class PhysicsConstants
{
    // --- SI Real-world constants (base reference units) ---
    private const double REAL_G_SI = 6.67430e-11;    // m^3 / (kg s^2)
    private const double REAL_SPEED_OF_LIGHT_M_PER_S = 299792458.0;    // m / s
    private const double REAL_SECONDS_PER_DAY = 86400;

    // --- Real-world conversion constants (SI <-> astro) ---
    public const double REAL_METERS_PER_AU = 1.495978707e11; // m in 1 AU
    public const double REAL_KG_PER_SOLAR_MASS = 1.98847e30;  // kg in 1 solar mass



    // --- Unity world-space scaling (3D sim) ---
    public const double UNITY_UNITS_PER_AU = 50.0;   // 1 AU ≈ 3000 Unity world-space units 
    public const double UNITY_METERS_PER_UNIT = REAL_METERS_PER_AU / UNITY_UNITS_PER_AU;  // meters/Unity unit: 1 Unity unit ≈ (1/3000) AU

    // --- Unity time scaling ---
    public const double UNITY_DAYS_PER_REAL_SECOND = 1.0;

    // --- Unity converted SI real-world constants ---
    private const double UNITY_G_BASE = REAL_G_SI
                                    * (REAL_SECONDS_PER_DAY * REAL_SECONDS_PER_DAY)
                                    / (UNITY_METERS_PER_UNIT * UNITY_METERS_PER_UNIT * UNITY_METERS_PER_UNIT);

    // Converted scaled G (gravity) constant
    public static double UNITY_G => UNITY_G_BASE * SimulationSettings.Instance.GravityScale;
    // Converted speed of light constant
    public const double UNITY_SPEED_OF_LIGHT = REAL_SPEED_OF_LIGHT_M_PER_S
                                             * (REAL_SECONDS_PER_DAY / UNITY_METERS_PER_UNIT);

    // --- Real-world planetary body diameters in meters ---
    public const double REAL_SUN_DIAMETER_M = 1391400000;
    public const double REAL_EARTH_DIAMETER_M = 12756000;
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

}
