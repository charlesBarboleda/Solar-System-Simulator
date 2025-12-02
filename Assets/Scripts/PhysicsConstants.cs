public static class PhysicsConstants
{
    // --- SI Real-world constants ---
    public const double G_SI = 6.67430e-11;                 // m^3 / (kg s^2)
    public const double METERS_PER_AU = 1.495978707e11;
    public const double KG_PER_SOLAR_MASS = 1.98847e30;     // kg
    public const double SECONDS_PER_YEAR = 365.25 * 86400.0;
    public const double SPEED_OF_LIGHT_M_PER_S = 299792458.0;

    // --- Real-world measurements ---
    // Sun constants
    public const double SUN_MASS_KG = 1.9885e30; // wikipedia number            
    public const double SUN_RADIUS_METERS = 6.9634e8; // wikipedia number
    public static readonly double SUN_RADIUS_SIM = SUN_RADIUS_METERS / LENGTH_UNIT_METERS;

    // Earth constants
    public const double EARTH_MASS_KG = 5.9722e24; // wikipedia number
    public const double EARTH_RADIUS_METERS = 6.371e6; // wikipedia number
    public static readonly double EARTH_RADIUS_SIM = EARTH_RADIUS_METERS / LENGTH_UNIT_METERS;

    // --- Unity simulation units ---
    // 1 Unity world-space unit = 1 AU ≈ 150 million km
    // 1 Unity mass unit   = 1 solar mass = mass of our Sun ≈ 1.9885e30 kg
    // 1 Unity time unit   = 1 day (1/365.25 year) ≈ 8.64e4 seconds
    public const double LENGTH_UNIT_METERS = METERS_PER_AU;
    public const double MASS_UNIT_KG = KG_PER_SOLAR_MASS;
    public const double TIME_UNIT_SECONDS = SECONDS_PER_YEAR / 365.25;

    // Derived gravitational constant in simulation units:
    // G_sim = G_SI * (T0^2 * M0 / L0^3)
    public static readonly double G_SIM =
        G_SI *
        (TIME_UNIT_SECONDS * TIME_UNIT_SECONDS * MASS_UNIT_KG) /
        (LENGTH_UNIT_METERS * LENGTH_UNIT_METERS * LENGTH_UNIT_METERS);

    // For T = 1 day, SIM_GRAV_CONSTANT ≈ 2.96e-4,
    // giving an orbital period of ~365 sim days at r=1, M=1.

    public const double MIN_DISTANCE_SIM = 1e-4; // in sim length units (AU)

}
