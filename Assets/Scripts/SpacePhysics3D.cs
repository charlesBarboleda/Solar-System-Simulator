using UnityEngine;
using Unity.Mathematics;
using System.Collections.Generic;

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

        double massSelf = self.MassKg;
        double massOther = neighbour.MassKg;

        double invDist = 1.0 / dist;
        double invDist2 = invDist * invDist;

        double force = PhysicsConstants.G_SIM * SimulationSettings.Instance.GravityScale * massSelf * massOther * invDist2;
        direction = displacement * invDist; // normalized
        return force;
    }

    // Returns an acceleration vector (UnityUnits / s^2) on 'self' due to 'neighbour' (2-body only)
    public static double3 TwoBodyAcceleration(AstronomicalObject self, AstronomicalObject neighbour)
    {
        if (self == null || self.MassKg <= 0.0)
            return double3.zero;

        double force = TwoBodyGForce(self, neighbour, out double3 dir);
        if (force == 0.0)
            return double3.zero;

        double accelMag = force / self.MassKg; // a = F / m_self
        return dir * accelMag;                      // UnityUnits / s^2
    }

    // Returns a force vector (kg * UnityUnits / s^2) on 'self' due to 'neighbour' (2-body only)
    public static double3 TwoBodyForceVector(AstronomicalObject self, AstronomicalObject neighbour)
    {
        double forceMagnitude = TwoBodyGForce(self, neighbour, out double3 dir);

        // Keep it all in double, do NOT cast to float here.
        return dir * forceMagnitude;
    }

    // Returns the barycentric position between two or more astronomical objects based on their masses and positions
    // The Barycentric Position is the center of mass in an n-body star system
    public static double3 GetBarycentricPosition(List<AstronomicalObject> bodies)
    {
        double3 barycenterPos = double3.zero;
        double MassKg = 0.0;

        // 1) Calculate total mass and weighted position sum by mass
        foreach (AstronomicalObject body in bodies)
        {
            if (body == null || body.MassKg <= 0.0) continue;

            barycenterPos += body.Position * body.MassKg; // weighted position sum by mass
            MassKg += body.MassKg; // total mass of the astronomical bodies
        }

        // 2) Divide weighted position sum by total mass to get barycenter position
        if (MassKg > 0.0) barycenterPos /= MassKg;
        return barycenterPos;
    }

    // Returns the distance between two astronomical objects in simulation units
    public static double DistanceBetween(AstronomicalObject obj1, AstronomicalObject obj2)
    {
        if (obj1 == null || obj2 == null)
            return double.PositiveInfinity;

        double3 displacement = obj2.Position - obj1.Position;
        return math.length(displacement);
    }

}
