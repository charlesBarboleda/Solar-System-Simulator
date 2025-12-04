using UnityEngine;
using Unity.Mathematics;
using System.Collections.Generic;
using UnityEditor;

public static class SpacePhysics3D
{
    /// <summary>
    /// Custom physics functions and calculations for space simulation in 3D. 
    /// Gravitational constants adapted to Unity units (see PhysicsConstants.cs)
    /// Use double precision for all calculations to maintain accuracy over large distances
    /// Use double3 from Unity.Mathematics for 3D vector math operations
    /// </summary>



    // ******GENERAL HELPER METHODS****** // 

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
        const double epsilon = 1e-24; // tweak to accomodate for position scaling

        if (lenSq < epsilon)
        {
            Debug.LogError("Objects are too close together to calculate a valid unit direction vector.");
            return double3.zero; // Return zero vector to avoid division by zero
        }

        return math.normalize(direction);
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


    // ******EINSTEIN-INFELD-HOFFMANN EQUATION METHODS****** //


    // These methods solve the first term of the EIH equations 
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

    // These methods solve the second term of the EIH equations
    // PLACEHOLDER METHOD
    public static double3 SecondTerm(AstronomicalObject self, IReadOnlyList<AstronomicalObject> neighborBodies)
    {
        double3 secondTerm = double3.zero;

        double3 inverseSqLaw = 1 / (PhysicsConstants.SPEED_OF_LIGHT_M_PER_S * PhysicsConstants.SPEED_OF_LIGHT_M_PER_S);
        double3 accelVectorAB = NBodyAccelVector(self, neighborBodies);
        double3 bracket = double3.zero;

        // Variables needed to solve the second term of the EIH equation

        double3 v_A = self.Velocity; // Velocity of self or object "A"
        double speed_A = math.length(v_A); // Speed of self from velocity
        double3 v2_A = math.square(speed_A); // Speed of "A" squared

        double3 v_B = double3.zero; // Velocity of neighbor object "B"
        double speed_B = math.length(v_B); // Speed magnitude of neighbor object "B" from velocity
        double3 v2_B = math.square(speed_B); // Speed of neighbor object "B" squared

        double3 nUnitVect_AB = double3.zero; // Vector direction from B to A
        double3 nUnitVect_BC = double3.zero; // Vector direction from C to B



        foreach (AstronomicalObject B in neighborBodies)
        {
            if (B == self) continue;

            v_B = B.Velocity;
            nUnitVect_AB = UnitVectorDirectionFrom(self, B);

            // bracket += (v2_A + (2 * v2_B)
            //             - (4 * (v_A * v_B))
            //             - ((3 / 2) * (nUnitVect_AB * v_B))
            //             - (4 * ( / rAC))
            //         );
        }

        return secondTerm = inverseSqLaw
                            * accelVectorAB
                            * bracket;
    }

    // ******BARYCENTER METHODS****** //
    // BARYCENTER: The center of mass given an n-body system

    // Outputs the barycenter vectors (position & velocity) between two or more astronomical objects based on their masses, positions, and velocities
    public static void GetBarycenterVectorsOf(List<AstronomicalObject> bodies, out double3 barycenterPosition, out double3 barycenterVelocity)
    {
        if (bodies == null)
        {
            Debug.LogError("[SpacePhysics3D] GetBarycenter: Invalid or Null AstronomicalObject references");
            barycenterPosition = double3.zero;
            barycenterVelocity = double3.zero;
            return;
        }

        if (bodies.Count <= 0)
        {
            Debug.LogError("[SpacePhysics3D] GetBarycenter: Must have a minimum of 1 AstronomicalObject body in bodies");
            barycenterPosition = double3.zero;
            barycenterVelocity = double3.zero;
            return;
        }

        double3 weightedVelocities = double3.zero;
        double3 weightedPositions = double3.zero;
        double totalMassKg = 0.0;

        // 1) Calculate total mass and weighted sum by mass of the position/velocity of each object
        foreach (AstronomicalObject body in bodies)
        {
            if (body == null || body.MassKg <= 0.0) continue;

            weightedVelocities += body.Velocity * body.MassKg; // velocity of object weighted/multiplied/scaled by the object's mass
            weightedPositions += body.Position * body.MassKg; // same calculations as velocity but with the position value instead
            totalMassKg += body.MassKg; // total mass of the astronomical bodies
        }


        if (totalMassKg <= 0.0)
        {
            Debug.LogError("[SpacePhysics3D] GetBarycenter: Cannot calculate with a totalMassKg of 0 or less");
            barycenterPosition = double3.zero;
            barycenterVelocity = double3.zero;
            return;
        }

        // 2) Divide weighted sum of position/velocity by total mass to get barycenter vectors
        barycenterPosition = weightedPositions / totalMassKg;
        barycenterVelocity = weightedVelocities / totalMassKg;
        return;

    }

    // Returns the position of "body" expressed in barycentric coordinates (relative to the given barycenterPosition)
    public static double3 GetBarycentricPositionOf(AstronomicalObject body, double3 barycenterPosition)
    {
        if (body == null)
        {
            Debug.LogError("Invalid or null reference of an astronomical object.");
            return double3.zero;
        }

        double3 barycentricPosition = body.Position - barycenterPosition;
        return barycentricPosition;
    }

    // Returns the velocity of "body" relative to the given barycenterVelocity
    public static double3 GetBarycentricVelocityOf(AstronomicalObject body, double3 barycenterVelocity)
    {
        if (body == null)
        {
            Debug.LogError("Invalid or Null AstronomicalObject reference.");
            return double3.zero;
        }

        double3 barycentricVelocity = body.Velocity - barycenterVelocity;
        return barycentricVelocity;
    }

    // Returns the barycentric acceleration vector of object a in an n-body system using the full-form Einstein-Infeld-Hoffmann equations
    // Incomplete - placeholder for future implementation
    public static double3 NBodyBaryAccelVector(AstronomicalObject a, IReadOnlyList<AstronomicalObject> neighborBodies)
    {
        double3 accelVector = double3.zero;
        accelVector = NBodyAccelVector(a, neighborBodies);

        return accelVector;
    }
}
