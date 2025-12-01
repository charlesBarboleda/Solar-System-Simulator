using UnityEngine;
using Unity.Mathematics;

public static class SpacePhysics3D
{
    /// <summary>
    /// Custom physics functions and calculations for space simulation in 3D. 
    /// Gravitational constants adapted to Unity units (see PhysicsConstants.cs)
    /// Use double precision for all calculations to maintain accuracy over large distances
    /// Use double3 from Unity.Mathematics for 3D vector math operations
    /// </summary>

    // Uses Newton's law of universal gravitation to calculate gravitational force magnitude
    // between two astronomical objects. Returns scalar force in game-space units (kg * UnityUnits / s^2)
    // Also outputs normalized direction from 'self' to 'neighbour'.
    public static double TwoBodyGForce(AstronomicalObject self, AstronomicalObject neighbour, out double3 direction)
    {
        if (self == null || neighbour == null)
        {
            Debug.LogError("TwoBodyGForce requires valid AstronomicalObject references.");
            direction = double3.zero;
            return 0.0;
        }

        double3 displacement = neighbour.Position - self.Position;
        double dist = math.length(displacement);

        if (dist <= 0.0)
        {
            direction = double3.zero;
            Debug.LogError("Distance between objects must be greater than zero.");
            return 0.0;
        }

        if (dist < PhysicsConstants.MIN_DISTANCE_SIM)
            dist = PhysicsConstants.MIN_DISTANCE_SIM;

        double massSelf = self.TotalMassKg;
        double massOther = neighbour.TotalMassKg;

        double invDist = 1.0 / dist;
        double invDist2 = invDist * invDist;

        double force = PhysicsConstants.G_SIM * SimulationSettings.Instance.GravityScale * massSelf * massOther * invDist2;
        direction = displacement * invDist; // normalized
        return force;
    }

    // Returns an acceleration vector (UnityUnits / s^2) on 'self' due to 'neighbour' (2-body only)
    public static double3 TwoBodyAcceleration(AstronomicalObject self, AstronomicalObject neighbour)
    {
        if (self == null || self.TotalMassKg <= 0.0)
            return double3.zero;

        double force = TwoBodyGForce(self, neighbour, out double3 dir);
        if (force == 0.0)
            return double3.zero;

        double accelMag = force / self.TotalMassKg; // a = F / m_self
        return dir * accelMag;                      // UnityUnits / s^2
    }

    // Returns a force vector (kg * UnityUnits / s^2) on 'self' due to 'neighbour' (2-body only)
    public static double3 TwoBodyForceVector(AstronomicalObject self, AstronomicalObject neighbour)
    {
        double forceMagnitude = TwoBodyGForce(self, neighbour, out double3 dir);

        // Keep it all in double, do NOT cast to float here.
        return dir * forceMagnitude;
    }

}
