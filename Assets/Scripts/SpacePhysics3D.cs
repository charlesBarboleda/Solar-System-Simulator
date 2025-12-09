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

    // Returns the unit vector pointing from object A to object B (direction only, magnitude = 1)
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

    // Incomplete EIH equation (missing ThirdTerm() & FourthTerm())
    private static double3 Einstein_Infeld_Hoffmann(AstronomicalObject self, IReadOnlyList<AstronomicalObject> bodies)
    {
        double3 baryAccelVector = FirstTerm(self, bodies) + SecondTerm(self, bodies);
        return baryAccelVector;
    }

    // Returns the gravitational acceleration vector on 'a' due to 'b' (2-body only) using Newton's law of universal gravitation
    public static double3 TwoBodyAccelVectorOf(AstronomicalObject a, AstronomicalObject b)
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
        // Numerator: (GConst * GScale) * MassB * Unit Direction Vector from A to B
        double3 numerator = PhysicsConstants.G * b.MassKg * UnitVectorDirectionFrom(a, b);
        // Denominator: Distance^2 between A and B
        double denominator = DistanceBetween(a, b) * DistanceBetween(a, b);

        return numerator / denominator;
    }

    // Returns the Newtonian gravitational acceleration of an object due to n-neighbors
    // This method calculates the first term of the Einstein-Infeld-Hoffmann equations for n-body systems
    public static double3 NBodyAccelVectorOf(AstronomicalObject self, IReadOnlyList<AstronomicalObject> bodies)
    {
        double3 accumulatedAccelVector = Sigma(
         bodies,
         B => TwoBodyAccelVectorOf(self, B),
         B => B != null && !ReferenceEquals(B, self)
     );

        return accumulatedAccelVector;
    }



    // Solves the first term of the EIH equations
    private static double3 FirstTerm(AstronomicalObject self, IReadOnlyList<AstronomicalObject> bodies)
    {
        return NBodyAccelVectorOf(self, bodies);
    }

    // Solves the second term of the EIH equations
    private static double3 SecondTerm(AstronomicalObject self, IReadOnlyList<AstronomicalObject> bodies)
    {
        double3 secondTerm = double3.zero; // Store the result of the second term here

        // Constants 
        const double c = PhysicsConstants.SPEED_OF_LIGHT_M_PER_S;
        double G = PhysicsConstants.G;

        // Inverse law of light speed used as a correction
        double inverseSq_c = 1.0 / (c * c);

        // Variables here are relative to the barycenter (barycentric vectors) and are needed to solve the second term of the EIH equation
        GetBarycenterVectorsOf(bodies, out double3 barycenterPosition, out double3 barycenterVelocity);

        // Positions relative to the barycenter
        double3 baryPosition_A = GetBarycentricPositionOf(self, barycenterPosition);

        // Object "A"'s velocity and squared speed relative to the barycenter
        double3 baryVel_A = GetBarycentricVelocityOf(self, barycenterVelocity);
        double baryVelSq_A = math.lengthsq(baryVel_A);


        // innerSum1 solves for the first inner sigma notation inside the bracket of the second term
        double innerSum1 = Sigma(
            bodies,
            C => (G * C.MassKg) / DistanceBetween(C, self),
            C => C != null && !ReferenceEquals(C, self)
        );

        // Solves for the outer sigma notation of the second term
        secondTerm = Sigma(
            bodies,
            B =>
            {
                double3 accel_A = TwoBodyAccelVectorOf(self, B);
                double3 baryVel_B = GetBarycentricVelocityOf(B, barycenterVelocity);
                double baryVelSq_B = math.lengthsq(baryVel_B);
                double3 baryPosition_B = GetBarycentricPositionOf(B, barycenterPosition);
                double3 n_BA = UnitVectorDirectionFrom(self, B);

                // innerSum2 solves for the second inner sigma notation inside the bracket of the second term
                double innerSum2 = Sigma(
                    bodies,
                    C => (G * C.MassKg) / DistanceBetween(B, C),
                    C => C != null && !ReferenceEquals(C, B)
                );

                double nAB_dot_vB = math.dot(n_BA, baryVel_B);

                double scalarBracket = baryVelSq_A // v2_A
                       + 2.0 * baryVelSq_B // 2 * (v2_B)
                       - 4.0 * math.dot(baryVel_A, baryVel_B) // 4 * (v_A · v_B)
                       - (3.0 / 2.0) * (nAB_dot_vB * nAB_dot_vB) // 3/2 * (n_AB · v_B)^2
                       - 4.0 * (innerSum1) // 4 * (Σ_C≠A)
                       - innerSum2 // Σ_C≠B
                       + (1.0 / 2.0) * (math.dot((baryPosition_B - baryPosition_A), NBodyAccelVectorOf(B, bodies))); // 1/2 * ((x_B - x_A) · a_B)


                return inverseSq_c * accel_A * scalarBracket;
            },
            B => B != null && !ReferenceEquals(B, self)
        );

        return secondTerm;
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

}
