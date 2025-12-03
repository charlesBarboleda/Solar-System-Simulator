using UnityEngine;
using Unity.Mathematics;
using System.Collections.Generic;

// 

public static class SpacePhysics3D
{
    /// <summary>
    /// Custom physics functions and calculations for space simulation in 3D. 
    /// Gravitational constants adapted to Unity units (see PhysicsConstants.cs)
    /// Use double precision for all calculations to maintain accuracy over large distances
    /// Use double3 from Unity.Mathematics for 3D vector math operations
    /// </summary>

    // Returns the gravitational acceleration vector on 'a' due to 'b' (2-body only) using Newton's law of universal gravitation
    public static double3 TwoBodyAccelVector(AstronomicalObject a, AstronomicalObject b)
    {
        if (a == null || b == null)
        {
            Debug.LogError("TwoBodyAcceleration requires valid AstronomicalObject references.");
            return double3.zero;
        }

        if (a.MassKg <= 0.0 || b.MassKg <= 0.0)
        {
            Debug.LogError("TwoBodyAcceleration requires AstronomicalObjects to have a valid mass greater than zero.");
            return double3.zero;
        }
        // Numerator: (GConst * GScale) * MassB * Unit Direction Vector from B to A
        double3 numerator = (PhysicsConstants.G_SIM * SimulationSettings.Instance.GravityScale) * b.MassKg * UnitVectorDirectionFrom(a, b);
        // Denominator: Distance^2 between A and B
        double denominator = DistanceBetween(a, b) * DistanceBetween(a, b);

        return numerator / denominator;
    }

    // Returns the barycentric position between two or more astronomical objects based on their mass and position
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

    // Returns the barycentric acceleration vector of body A using the Einstein-Infeld-Hoffmann equations for n-body systems
    public static double3 BarycentricAcceleration(AstronomicalObject mainBody, IReadOnlyList<AstronomicalObject> neighborBodies)
    {
        // if (mainBody == null || neighborBodies == null || neighborBodies.Count == 0)
        // {
        //     Debug.LogError("BarycentricAcceleration requires valid AstronomicalObject references and a non-empty neighbor list.");
        //     return double3.zero;
        // }

        // if (mainBody.MassKg <= 0.0)
        // {
        //     Debug.LogError("BarycentricAcceleration requires mainBody to have a valid mass greater than zero.");
        //     return double3.zero;
        // }

        // if (TotalSumMassKg(neighborBodies) <= 0.0)
        // {
        //     Debug.LogError("BarycentricAcceleration requires neighborBodies to have a total mass greater than zero.");
        //     return double3.zero;
        // }

        // // Calculate barycentric acceleration using EIH equations
        // // 1) Initialize variables needed for the calculation
        // int neighborsCount = neighborBodies.Count;
        // double neighborsMassKg = TotalSumMassKg(neighborBodies, mainBody); // exclude main body mass
        // double vASquared = math.lengthsq(mainBody.Velocity); // squared velocity of main body

        // double3 baryAccelVector = double3.zero;
        // double3 equation1 = ((neighborsMassKg) * ((PhysicsConstants.G_SIM*)/ ()));
        // baryAcceleration =
        // ((TotalSumMassKg(astroObjects) - mainBody.MassKg) * ((PhysicsConstants.G_SIM*)/ ()))


        return double3.zero;

    }

    // Returns the separation vector between two astronomical objects in Unity units
    public static double3 SeparationVectorFrom(AstronomicalObject a, AstronomicalObject b)
    {
        if (a == null || b == null)
        {
            Debug.LogError("SeparationVectorFrom requires valid AstronomicalObject references.");
            return double3.zero;
        }

        return b.Position - a.Position;
    }

    // Returns the unit vector pointing from object B to object A (direction only, magnitude = 1)
    public static double3 UnitVectorDirectionFrom(AstronomicalObject a, AstronomicalObject b)
    {
        if (a == null || b == null)
        {
            Debug.LogError("UnitVectorDirectionFrom requires valid AstronomicalObject references.");
            return double3.zero;
        }

        double3 direction = b.Position - a.Position;
        double lenSq = math.lengthsq(direction);
        const double epsilon = 1e-24; // tweak for your position scale

        if (lenSq < epsilon)
        {
            Debug.LogError("Objects are too close together to calculate a valid unit direction vector.");
            return double3.zero; // Return zero vector to avoid division by zero
        }

        return math.normalize(direction);
    }

    // Returns the distance between two astronomical objects in Unity units
    public static double DistanceBetween(AstronomicalObject a, AstronomicalObject b)
    {
        if (a == null || b == null)
        {
            Debug.LogError("DistanceBetween requires valid AstronomicalObject references.");
            return double.NaN;
        }

        double distance = math.distance(a.Position, b.Position);
        if (distance < PhysicsConstants.MIN_DISTANCE_SIM)
        {
            Debug.LogWarning($"DistanceBetween: {a.Name} and {b.Name} are too close together; using MIN_DISTANCE_SIM to avoid singularity.");
            return PhysicsConstants.MIN_DISTANCE_SIM;
        }

        return distance;
    }

    // Returns the sum of all masses in kg from the provided list of astronomical objects
    // Optionally excludes a specific body from the sum (calculating total mass of neighbors only)
    public static double TotalSumMassKg(IReadOnlyList<AstronomicalObject> bodies, AstronomicalObject excludeBody = null)
    {
        if (bodies == null || bodies.Count == 0)
        {
            Debug.LogError("TotalSumMassKg requires a non-empty list of AstronomicalObjects.");
            return 0.0;
        }

        double totalMass = 0.0;

        foreach (AstronomicalObject body in bodies)
        {
            if (body != null && body.MassKg > 0.0 && body != excludeBody)
            {
                totalMass += body.MassKg;
            }
        }

        return totalMass;
    }

    // Returns the Newtonian gravitational acceleration of an object with respect to its neighbors
    // This method calculates the first term of the Einstein-Infeld-Hoffmann equations for n-body systems
    public static double3 NBodyAccelVector(AstronomicalObject self, IReadOnlyList<AstronomicalObject> neighborBodies)
    {
        double3 accumulatedAccelVector = double3.zero;

        // Iterate through all neighboring bodies to calculate total acceleration on self
        foreach (AstronomicalObject neighbor in neighborBodies)  // The "neighbor" variable is equal to "B" in the EIH equations 
        {
            if (neighbor == null || neighbor == self) continue;

            accumulatedAccelVector += TwoBodyAccelVector(self, neighbor); // Sum of all acceleration vectors from neighbors
        }

        return accumulatedAccelVector;
    }

    // Returns the barycentric acceleration vector of object a in an n-body system using Einstein-Infeld-Hoffmann equations
    // Incomplete - placeholder for future implementation
    public static double3 NBodyBaryAccelVector(AstronomicalObject a, IReadOnlyList<AstronomicalObject> neighborBodies)
    {
        double3 accelVector = double3.zero;
        accelVector = NBodyAccelVector(a, neighborBodies);

        return accelVector;
    }
}
