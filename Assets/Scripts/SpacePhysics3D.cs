using Unity.Mathematics;
using UnityEngine;
using System;



public static class SpacePhysics3D
{
    // Workspace to avoid per-frame allocations (reuse arrays).
    public sealed class Workspace_EIH
    {
        public double3[] BarycentricPositions = Array.Empty<double3>();
        public double3[] BarycentricVelocities = Array.Empty<double3>();

        public double[] PotentialPhi = Array.Empty<double>();          // phi[a] = Σ_{b≠a} G m_b / r_ab
        public double3[] NewtonianAccel = Array.Empty<double3>();      // aNewton[a]
        public double3[] AccelApprox = Array.Empty<double3>();         // aApprox[a] ~ a_B used inside the RHS terms

        public double3[] SecondTermSum = Array.Empty<double3>();       // Σ second contributions
        public double3[] ThirdTermSum = Array.Empty<double3>();        // Σ third contributions
        public double3[] FourthTermSum = Array.Empty<double3>();       // Σ fourth contributions


        public void EnsureCapacity(int count)
        {
            if (BarycentricPositions.Length != count) BarycentricPositions = new double3[count];
            if (BarycentricVelocities.Length != count) BarycentricVelocities = new double3[count];

            if (PotentialPhi.Length != count) PotentialPhi = new double[count];
            if (NewtonianAccel.Length != count) NewtonianAccel = new double3[count];
            if (AccelApprox.Length != count) AccelApprox = new double3[count];

            if (SecondTermSum.Length != count) SecondTermSum = new double3[count];
            if (ThirdTermSum.Length != count) ThirdTermSum = new double3[count];
            if (FourthTermSum.Length != count) FourthTermSum = new double3[count];
        }
    }

    /// <summary>
    /// Returns the Newtonian acceleration of "a"
    /// Masses and positions must have the same length numOfBodies.
    /// </summary>
    public static double3 NBodyAccelVectorOf(
        int a,
        ReadOnlySpan<double> masses,
        ReadOnlySpan<double3> positions)
    {
        int numOfBodies = positions.Length;

        if (masses.Length != numOfBodies) return double3.zero;
        if ((uint)a >= (uint)numOfBodies) return double3.zero;

        double G = PhysicsConstants.UNITY_G;

        double minDist = PhysicsConstants.UNITY_MIN_DISTANCE;
        double minDistSq = minDist * minDist;

        double3 pos_a = positions[a];
        double3 totalAccel = double3.zero;

        for (int b = 0; b < numOfBodies; b++)
        {
            if (b == a) continue;

            double mass_b = masses[b];
            if (mass_b <= 0.0) continue;

            double3 displacement_ab = positions[b] - pos_a;   // x_b - x_a
            double r2_ab = math.lengthsq(displacement_ab);
            if (r2_ab < minDistSq) r2_ab = minDistSq;

            double invR = 1.0 / math.sqrt(r2_ab);
            double invR3 = invR / r2_ab;

            totalAccel += (G * mass_b) * displacement_ab * invR3;
        }

        return totalAccel;
    }

    /// <summary>
    /// Computes EIH 1PN barycentric accelerations for ALL bodies in O(accelIterations * N^2).
    /// Output accel must be length "bodyCount".
    /// </summary>
    public static void Einstein_Infeld_Hoffmann_1PN(
        ReadOnlySpan<double3> positions,
        ReadOnlySpan<double3> velocities,
        ReadOnlySpan<double> masses,
        Span<double3> outBarycentricAccelerations,
        Workspace_EIH workspace,
        AccelBMode accelBMode = AccelBMode.NewtonianApprox,
        int accelIterations = 2)
    {
        int bodyCount = positions.Length;

        if (!EnsureSameCount(velocities.Length, bodyCount) || !EnsureSameCount(masses.Length, bodyCount))
            return;

        if (outBarycentricAccelerations.Length != bodyCount)
        {
            Debug.LogError("[SpacePhysics3D] ComputeEinsteinInfeldHoffmann1PN(): output span size mismatch.");
            return;
        }

        workspace.EnsureCapacity(bodyCount);

        // Constants
        const double c = PhysicsConstants.UNITY_SPEED_OF_LIGHT;
        double invC2 = 1.0 / (c * c);
        double G = PhysicsConstants.UNITY_G;

        // Minimum-distance handling (compare squared-to-squared)
        double minDistSq = PhysicsConstants.UNITY_MIN_DISTANCE * PhysicsConstants.UNITY_MIN_DISTANCE;

        // 1) Barycenter + barycentric vectors
        GetBarycenterVectorsOf(velocities, positions, masses, out double3 barycenterPosition, out double3 barycenterVelocity);

        for (int a = 0; a < bodyCount; a++)
        {
            workspace.BarycentricPositions[a] = positions[a] - barycenterPosition;
            workspace.BarycentricVelocities[a] = velocities[a] - barycenterVelocity;

            workspace.PotentialPhi[a] = 0.0;
            workspace.NewtonianAccel[a] = double3.zero;
            workspace.AccelApprox[a] = double3.zero;

            workspace.SecondTermSum[a] = double3.zero;
            workspace.ThirdTermSum[a] = double3.zero;
            workspace.FourthTermSum[a] = double3.zero;

            outBarycentricAccelerations[a] = double3.zero;
        }

        double3[] baryPositions = workspace.BarycentricPositions;
        double3[] baryVelocities = workspace.BarycentricVelocities;

        // 2) Compute Newtonian acceleration and potentials in one O(N^2) sweep
        // phi[a] = Σ G m_b / r_ab
        // aNewton[a] = Σ G m_b * (r_b - r_a) / r_ab^3
        for (int a = 0; a < bodyCount - 1; a++)
        {
            double mass_a = masses[a];
            if (mass_a <= 0.0) continue;

            for (int b = a + 1; b < bodyCount; b++)
            {
                double mass_b = masses[b];
                if (mass_b <= 0.0) continue;

                double3 displacement_ab = baryPositions[b] - baryPositions[a];
                double r2_ab = math.lengthsq(displacement_ab);
                if (r2_ab < minDistSq) r2_ab = minDistSq;

                double invR = 1.0 / math.sqrt(r2_ab);
                double invR3 = invR / r2_ab;

                // Potential contributions
                workspace.PotentialPhi[a] += (G * mass_b) * invR;
                workspace.PotentialPhi[b] += (G * mass_a) * invR;

                // Newtonian accel contributions (equal and opposite)
                double3 directionOverR3 = displacement_ab * invR3;

                workspace.NewtonianAccel[a] += (G * mass_b) * directionOverR3;
                workspace.NewtonianAccel[b] -= (G * mass_a) * directionOverR3;
            }
        }
        double3[] accelApprox = workspace.AccelApprox;
        for (int a = 0; a < bodyCount; a++) accelApprox[a] = workspace.NewtonianAccel[a];

        int iterations = (accelBMode == AccelBMode.FixedPointIterated) ? math.max(1, accelIterations) : 1;

        // 3) Fixed-point iterations: RHS of EIH contains a_B, so iterate using accelApprox as a_B estimate
        for (int iter = 0; iter < iterations; iter++)
        {
            // Clear correction sums each iteration
            for (int a = 0; a < bodyCount; a++)
            {
                workspace.SecondTermSum[a] = double3.zero;
                workspace.ThirdTermSum[a] = double3.zero;
                workspace.FourthTermSum[a] = double3.zero;
            }


            for (int a = 0; a < bodyCount - 1; a++)
            {
                double mass_a = masses[a];
                if (mass_a <= 0.0) continue;

                double3 baryVel_a = baryVelocities[a];
                double baryVelSq_a = math.lengthsq(baryVel_a);

                for (int b = a + 1; b < bodyCount; b++)
                {
                    double mass_b = masses[b];
                    if (mass_b <= 0.0) continue;

                    double3 displacement_ab = baryPositions[b] - baryPositions[a];
                    double r2_ab = math.lengthsq(displacement_ab);
                    if (r2_ab < minDistSq) r2_ab = minDistSq;

                    double invR = 1.0 / math.sqrt(r2_ab);
                    double invR2 = 1.0 / r2_ab;
                    double invR3 = invR / r2_ab;

                    double3 n_ab = -displacement_ab * invR; // unit vector direction from 'b' to 'a'
                    double3 n_ba = -n_ab; // unit vector direction from 'a' to 'b'

                    double3 baryVel_b = baryVelocities[b];
                    double baryVelSq_b = math.lengthsq(baryVel_b);

                    double va_Dot_vb = math.dot(baryVel_a, baryVel_b);

                    double3 accel_a_due_b = (G * mass_b) * (displacement_ab * invR3);
                    double3 accel_b_due_a = -(G * mass_a) * (displacement_ab * invR3);

                    double n_ab_dot_baryVel_b = math.dot(n_ab, baryVel_b);
                    double n_ba_dot_baryVel_a = math.dot(n_ba, baryVel_a);

                    double phi_a = workspace.PotentialPhi[a];
                    double phi_b = workspace.PotentialPhi[b];

                    double half_dx_dot_aB_for_a = 0.5 * math.dot(displacement_ab, accelApprox[b]);
                    double half_dx_dot_aB_for_b = 0.5 * math.dot(-displacement_ab, accelApprox[a]);

                    double scalarBracket_for_a =
                          baryVelSq_a
                        + 2.0 * baryVelSq_b
                        - 4.0 * va_Dot_vb
                        - (3.0 / 2.0) * (n_ab_dot_baryVel_b * n_ab_dot_baryVel_b)
                        - 4.0 * phi_a
                        - phi_b
                        + half_dx_dot_aB_for_a;

                    double scalarBracket_for_b =
                          baryVelSq_b
                        + 2.0 * baryVelSq_a
                        - 4.0 * va_Dot_vb
                        - (3.0 / 2.0) * (n_ba_dot_baryVel_a * n_ba_dot_baryVel_a)
                        - 4.0 * phi_b
                        - phi_a
                        + half_dx_dot_aB_for_b;

                    workspace.SecondTermSum[a] += invC2 * accel_a_due_b * scalarBracket_for_a;
                    workspace.SecondTermSum[b] += invC2 * accel_b_due_a * scalarBracket_for_b;

                    // Third term
                    double3 n_ab_for_a = n_ab;
                    double scalarBracket3_for_a = math.dot(n_ab_for_a, (4.0 * baryVel_a) - (3.0 * baryVel_b));
                    workspace.ThirdTermSum[a] += invC2 * (G * mass_b * invR2) * scalarBracket3_for_a * (baryVel_a - baryVel_b);

                    double3 n_ab_for_b = n_ba;
                    double scalarBracket3_for_b = math.dot(n_ab_for_b, (4.0 * baryVel_b) - (3.0 * baryVel_a));
                    workspace.ThirdTermSum[b] += invC2 * (G * mass_a * invR2) * scalarBracket3_for_b * (baryVel_b - baryVel_a);

                    // a_B estimate (Newtonian or iterated) used where RHS depends on acceleration
                    double fourthFactor_for_a = (7.0 / 2.0) * invC2 * (G * mass_b * invR);
                    double fourthFactor_for_b = (7.0 / 2.0) * invC2 * (G * mass_a * invR);

                    workspace.FourthTermSum[a] += fourthFactor_for_a * accelApprox[b];
                    workspace.FourthTermSum[b] += fourthFactor_for_b * accelApprox[a];
                }
            }

            // 4) Final sum: aNewton + 1PN corrections
            for (int a = 0; a < bodyCount; a++)
            {
                if (masses[a] <= 0.0)
                {
                    outBarycentricAccelerations[a] = double3.zero;
                    continue;
                }

                outBarycentricAccelerations[a] = workspace.NewtonianAccel[a]
                                               + workspace.SecondTermSum[a]
                                               + workspace.ThirdTermSum[a]
                                               + workspace.FourthTermSum[a];
            }

            EnforceZeroCOMAcceleration(outBarycentricAccelerations, masses);

            // 5) Feed back for next iteration if using iterated mode
            if (accelBMode == AccelBMode.FixedPointIterated && iter < iterations - 1)
            {
                for (int a = 0; a < bodyCount; a++)
                    accelApprox[a] = outBarycentricAccelerations[a];
            }
        }

#if UNITY_EDITOR
        {
            double totalMass = 0.0;
            double3 residual = double3.zero;

            for (int a = 0; a < bodyCount; a++)
            {
                double m = masses[a];
                if (m <= 0.0) continue;

                totalMass += m;
                residual += m * outBarycentricAccelerations[a];
            }

            // if (totalMass > 0.0 && math.length(residual) > 1e-10)
            //     Debug.LogWarning($"[SpacePhysics3D] COM accel residual: {residual}");
        }
#endif

    }

    // --- Barycenter methods (unchanged behavior, but keep them correct and tight) ---
    public static void GetBarycenterVectorsOf(
        ReadOnlySpan<double3> velocities,
        ReadOnlySpan<double3> positions,
        ReadOnlySpan<double> masses,
        out double3 barycenterPosition,
        out double3 barycenterVelocity)
    {
        if (!EnsureSameCount(velocities.Length, positions.Length) || !EnsureSameCount(positions.Length, masses.Length) || positions.Length == 0)
        {
            barycenterPosition = double3.zero;
            barycenterVelocity = double3.zero;
            return;
        }

        double3 weightedVelocities = double3.zero;
        double3 weightedPositions = double3.zero;
        double totalMassKg = 0.0;

        for (int a = 0; a < positions.Length; a++)
        {
            double mass_a = masses[a];
            if (mass_a <= 0.0) continue;

            weightedVelocities += velocities[a] * mass_a;
            weightedPositions += positions[a] * mass_a;
            totalMassKg += mass_a;
        }

        if (totalMassKg <= 0.0)
        {
#if UNITY_EDITOR
            Debug.LogError("[SpacePhysics3D] GetBarycenterVectorsOf(): totalMassKg must be > 0");
#endif
            barycenterPosition = double3.zero;
            barycenterVelocity = double3.zero;
            return;
        }

        barycenterPosition = weightedPositions / totalMassKg;
        barycenterVelocity = weightedVelocities / totalMassKg;
    }




    // --- Private helpers ---
    static void EnforceZeroCOMAcceleration(Span<double3> accelerations, ReadOnlySpan<double> masses)
    {
        double totalMass = 0.0;
        double3 aCM = double3.zero;

        int n = accelerations.Length;
        for (int a = 0; a < n; a++)
        {
            double m = masses[a];
            if (m <= 0.0) continue;

            totalMass += m;
            aCM += m * accelerations[a];
        }

        if (totalMass <= 0.0) return;

        aCM /= totalMass;

        for (int a = 0; a < n; a++)
        {
            if (masses[a] <= 0.0) continue;
            accelerations[a] -= aCM;
        }
    }

    static bool EnsureSameCount(int countA, int countB)
    {
        if (countA != countB)
        {
#if UNITY_EDITOR
            Debug.LogError($"[SpacePhysics3D] EnsureSameCount(): {countA} must equal {countB}");
#endif
            return false;
        }
        return true;
    }
}
