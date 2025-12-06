using UnityEngine;
using Unity.Mathematics;
using System.Collections.Generic;
using System;

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

    // Returns the Newtonian gravitational acceleration of an object due to n-neighbors
    // This method calculates the first term of the Einstein-Infeld-Hoffmann equations for n-body systems
    public static double3 NBodyAccelVector(AstronomicalObject self, IReadOnlyList<AstronomicalObject> bodies)
    {
        double3 accumulatedAccelVector = Sigma
                                        (
                                        bodies,
                                        B =>
                                        {
                                            return (PhysicsConstants.G_SIM * B.MassKg * UnitVectorDirectionFrom(self, B))
                                                   / (DistanceBetween(B, self) * DistanceBetween(B, self));
                                        },
                                        B => B != null && !ReferenceEquals(B, self)
                                        );

        return accumulatedAccelVector;

        // // Iterate through all neighboring bodies to calculate total acceleration on self
        // foreach (AstronomicalObject neighbor in bodies)  // The "neighbor" variable is equal to "B" in the EIH equations 
        // {
        //     if (neighbor == null || neighbor == self) continue;

        //     accumulatedAccelVector += TwoBodyAccelVector(self, neighbor); // Sum of all acceleration vectors from neighbors
        // }

    }

    // These methods solve for the second term of the EIH equations
    // PLACEHOLDER METHOD
    public static double3 SecondTerm(AstronomicalObject self, IReadOnlyList<AstronomicalObject> bodies)
    {
        double3 secondTerm = double3.zero;

        double3 inverseSqLaw = 1 / (PhysicsConstants.SPEED_OF_LIGHT_M_PER_S * PhysicsConstants.SPEED_OF_LIGHT_M_PER_S);
        double3 accelVector_A = NBodyAccelVector(self, bodies);
        double3 bracket = double3.zero;

        // Variables here are relative to the barycenter (barycentric vectors) and are needed to solve the second term of the EIH equation
        GetBarycenterVectorsOf(bodies, out double3 barycenterPosition, out double3 barycenterVelocity);

        // Positions relative to the barycenter
        double3 position_A = GetBarycentricPositionOf(self, barycenterPosition);
        double3 position_B = double3.zero;

        // Object "A"'s velocity and squared speed relative to the barycenter
        double3 v_A = GetBarycentricVelocityOf(self, barycenterVelocity);
        double v2_A = math.lengthsq(v_A);

        // Object "B"'s velocity and squared speed relative to the barycenter
        double3 v_B = double3.zero;
        double v2_B = math.lengthsq(v_B);

        // Unit pointing vectors
        double3 n_AB = double3.zero;
        double3 n_BC = double3.zero;

        // NON barycentric-related variables
        double3 accel_B = double3.zero;

        double innerSum1 = 0.0;
        double innerSum2 = 0.0;


        foreach (AstronomicalObject B in bodies)
        {
            if (B == self) continue;

            v_B = GetBarycentricVelocityOf(B, barycenterPosition);
            n_AB = UnitVectorDirectionFrom(self, B);
            position_B = GetBarycentricPositionOf(B, barycenterPosition);
            accel_B = NBodyAccelVector(B, bodies);

            foreach (AstronomicalObject C in bodies) // My incomplete attempt at solving C!=A 
            {
                if (C == self) continue;


            }

            // Building up the "bracket" expression
            bracket += (v2_A + (2 * v2_B)
                        - (4 * (v_A * v_B))
                        - ((3 / 2) * (math.square(n_AB * v_B)))
                        - (4 * innerSum1)
                        - (innerSum2)
                        + ((1 / 2) * ((position_B - position_A) * accel_B))
                    );
        }

        // Final formula to calculate for the second term
        return secondTerm = inverseSqLaw
                            * accelVector_A
                            * bracket;
    }

    // Returns the result of the first inner sum inside the second term of the EIH equation
    public static double InnerSumOne(AstronomicalObject self, IReadOnlyList<AstronomicalObject> bodies)
    {
        double innerSum = Sigma(
                        bodies,
                        C =>
                        {
                            double numerator = PhysicsConstants.G_SIM * C.MassKg;
                            double denominator = DistanceBetween(self, C);
                            return numerator / denominator;
                        },
                        C => C != null && !ReferenceEquals(C, self)
                        );

        return innerSum;
    }

    // ******MATH HELPERS****** (I should probably move these methods to a different class)

    // Scalar sigma: returns a "double" value type
    private static double Sigma<T>(IEnumerable<T> source, Func<T, double> term, Func<T, bool> condition = null)
    {
        double sum = 0.0;

        foreach (var x in source)
        {
            if (condition != null && !condition(x))
                continue;

            sum += term(x);
        }

        return sum;
    }

    // Vector sigma: returns a "double3" value type
    private static double3 Sigma<T>(IEnumerable<T> source, Func<T, double3> term, Func<T, bool> condition = null)
    {
        double3 sum = double3.zero;

        foreach (var x in source)
        {
            if (condition != null && !condition(x))
                continue;

            sum += term(x);   // term(x) is a double3
        }

        return sum;
    }

    // ******BARYCENTER METHODS****** //
    // BARYCENTER: The center of mass given an n-body system

    // Outputs the barycenter vectors (position & velocity) between two or more astronomical objects based on their masses, positions, and velocities
    public static void GetBarycenterVectorsOf(IReadOnlyList<AstronomicalObject> bodies, out double3 barycenterPosition, out double3 barycenterVelocity)
    {
        if (bodies == null)
        {
            Debug.LogError($"[SpacePhysics3D] GetBarycenter: Invalid or Null AstronomicalObject list reference");
            barycenterPosition = double3.zero;
            barycenterVelocity = double3.zero;
            return;
        }

        if (bodies.Count <= 0)
        {
            Debug.LogError($"[SpacePhysics3D] GetBarycenter: Must have a minimum of 1 AstronomicalObject body in bodies");
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
            weightedPositions += body.Position * body.MassKg; // same calculations as velocity but with the position vector instead
            totalMassKg += body.MassKg; // total mass of the astronomical bodies
        }


        if (totalMassKg <= 0.0)
        {
            Debug.LogError($"[SpacePhysics3D] GetBarycenter: Cannot calculate with a totalMassKg of 0 or less");
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
            Debug.LogError($"[SpacePhysics3D] GetBarycentricPositionOf: Invalid or Null {body.GetType()} reference.");
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
            Debug.LogError($"[SpacePhysics3D] GetBarycentricVelocityOf: Invalid or Null {body.GetType()} reference.");
            return double3.zero;
        }

        double3 barycentricVelocity = body.Velocity - barycenterVelocity;
        return barycentricVelocity;
    }

    // Returns the barycentric acceleration vector of object a in an n-body system using the full-form Einstein-Infeld-Hoffmann equations
    // Incomplete - placeholder for future implementation
    public static double3 NBodyBaryAccelerationOf(AstronomicalObject self, IReadOnlyList<AstronomicalObject> neighborBodies)
    {
        double3 accelVector = double3.zero;
        accelVector = NBodyAccelVector(self, neighborBodies);

        return accelVector;
    }
}
