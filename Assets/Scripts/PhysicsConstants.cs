public static class PhysicsConstants
{
    // Conversion factor between Unity units and meters
    public const double METERS_PER_UNIT = 5_000_000_000.0; // 1 Unity unit = 5e9 m

    // Gravitational constant in SI units (real-world gravitational constant)
    public const double GRAVITY_SI = 6.67430e-11;                // m^3 / (kg s^2)

    // Gravitational constant in "Unity units" 
    public const double GRAVITY = GRAVITY_SI /
        (METERS_PER_UNIT * METERS_PER_UNIT * METERS_PER_UNIT);

    // Minimum distance to avoid singularities in gravitational calculations
    public const double MIN_DISTANCE = 1e-4; // in Unity units

}
