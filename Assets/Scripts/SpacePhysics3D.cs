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

        public double[] PotentialPhi = Array.Empty<double>();          // phi[i] = Σ_{j≠i} G m_j / r_ij
        public double3[] NewtonianAccel = Array.Empty<double3>();      // aNewton[i]

        public double3[] SecondTermSum = Array.Empty<double3>();       // Σ second contributions
        public double3[] ThirdTermSum = Array.Empty<double3>();        // Σ third contributions
        public double3[] FourthTermSum = Array.Empty<double3>();       // Σ fourth contributions

        public void EnsureCapacity(int count)
        {
            if (BarycentricPositions.Length != count) BarycentricPositions = new double3[count];
            if (BarycentricVelocities.Length != count) BarycentricVelocities = new double3[count];

            if (PotentialPhi.Length != count) PotentialPhi = new double[count];
            if (NewtonianAccel.Length != count) NewtonianAccel = new double3[count];

            if (SecondTermSum.Length != count) SecondTermSum = new double3[count];
            if (ThirdTermSum.Length != count) ThirdTermSum = new double3[count];
            if (FourthTermSum.Length != count) FourthTermSum = new double3[count];
        }
    }

    /// <summary>
    /// Computes Newtonian acceleration of "i"
    /// Output accel must be length N.
    /// </summary>
    public static double3 NBodyAccelVectorOf(
        int i,
        ReadOnlySpan<double> masses,
        ReadOnlySpan<double3> positions)
    {
        int n = positions.Length;

        if (masses.Length != n) return double3.zero;
        if ((uint)i >= (uint)n) return double3.zero;

        double G = PhysicsConstants.UNITY_G;

        double minDist = PhysicsConstants.UNITY_MIN_DISTANCE;
        double minDistSq = minDist * minDist;

        double3 xi = positions[i];
        double3 acc = double3.zero;

        for (int j = 0; j < n; j++)
        {
            if (j == i) continue;

            double mj = masses[j];
            if (mj <= 0.0) continue;

            double3 dx = positions[j] - xi;          // x_j - x_i
            double r2 = math.lengthsq(dx);
            if (r2 < minDistSq) r2 = minDistSq;

            double invR = 1.0 / math.sqrt(r2);
            double invR3 = invR / r2;

            acc += (G * mj) * dx * invR3;
        }

        return acc;
    }

    /// <summary>
    /// Computes EIH 1PN barycentric accelerations for ALL bodies in O(N^2).
    /// Output accel must be length "bodyCount".
    /// </summary>
    public static void Einstein_Infeld_Hoffmann_1PN(
        ReadOnlySpan<double3> positions,
        ReadOnlySpan<double3> velocities,
        ReadOnlySpan<double> masses,
        Span<double3> outBarycentricAccelerations,
        Workspace_EIH workspace)
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
        double minDist = PhysicsConstants.UNITY_MIN_DISTANCE;
        double minDistSq = minDist * minDist;

        // 1) Barycenter + barycentric vectors
        GetBarycenterVectorsOf(velocities, positions, masses, out double3 barycenterPosition, out double3 barycenterVelocity);

        for (int i = 0; i < bodyCount; i++)
        {
            workspace.BarycentricPositions[i] = positions[i] - barycenterPosition;
            workspace.BarycentricVelocities[i] = velocities[i] - barycenterVelocity;

            workspace.PotentialPhi[i] = 0.0;
            workspace.NewtonianAccel[i] = double3.zero;

            workspace.SecondTermSum[i] = double3.zero;
            workspace.ThirdTermSum[i] = double3.zero;
            workspace.FourthTermSum[i] = double3.zero;

            outBarycentricAccelerations[i] = double3.zero;
        }

        double3[] baryPositions = workspace.BarycentricPositions;
        double3[] baryVelocities = workspace.BarycentricVelocities;

        // 2) Compute Newtonian acceleration and potentials in one O(N^2) sweep
        // phi[i] = Σ G m_j / r_ij
        // aNewton[i] = Σ G m_j * (r_j - r_i) / r_ij^3
        for (int i = 0; i < bodyCount - 1; i++)
        {
            double mass_i = masses[i];
            if (mass_i <= 0.0) continue;

            for (int j = i + 1; j < bodyCount; j++)
            {
                double mass_j = masses[j];
                if (mass_j <= 0.0) continue;

                double3 displacement_ij = baryPositions[j] - baryPositions[i];
                double r2 = math.lengthsq(displacement_ij);
                if (r2 < minDistSq) r2 = minDistSq;

                double invR = 1.0 / math.sqrt(r2);
                double invR3 = invR / r2;

                // Potential contributions
                workspace.PotentialPhi[i] += (G * mass_j) * invR;
                workspace.PotentialPhi[j] += (G * mass_i) * invR;

                // Newtonian accel contributions (equal and opposite)
                double3 directionOverR3 = displacement_ij * invR3;

                workspace.NewtonianAccel[i] += (G * mass_j) * directionOverR3;
                workspace.NewtonianAccel[j] -= (G * mass_i) * directionOverR3;
            }
        }

        // 3) Compute Second/Third/Fourth term corrections in one O(N^2) sweep
        for (int i = 0; i < bodyCount - 1; i++)
        {
            double mass_i = masses[i];
            if (mass_i <= 0.0) continue;

            double3 baryVel_i = baryVelocities[i];
            double baryVelSq_i = math.lengthsq(baryVel_i);

            for (int j = i + 1; j < bodyCount; j++)
            {
                double mass_j = masses[j];
                if (mass_j <= 0.0) continue;

                double3 displacement_ij = baryPositions[j] - baryPositions[i];
                double r2 = math.lengthsq(displacement_ij);
                if (r2 < minDistSq) r2 = minDistSq;

                double invR = 1.0 / math.sqrt(r2);
                double invR2 = 1.0 / r2;
                double invR3 = invR / r2;

                double3 n_ij = displacement_ij * invR; // unit from i->j

                double3 baryVel_j = baryVelocities[j];
                double baryVelSq_j = math.lengthsq(baryVel_j);

                double vi_Dot_vj = math.dot(baryVel_i, baryVel_j);

                // Pair Newtonian accelerations (needed for "accel_A" style factor)
                double3 accel_i_due_j = (G * mass_j) * (displacement_ij * invR3);
                double3 accel_j_due_i = -(G * mass_i) * (displacement_ij * invR3);

                // ----------------------------
                // SECOND TERM 
                // scalar bracket uses:
                // v_A^2 + 2 v_B^2 -4(v_A·v_B) - 3/2 (n_AB·v_B)^2 -4 phi[A] - phi[B] + 1/2 ((x_B-x_A)·aNewton[B])
                // ----------------------------

                double n_dot_baryVel_j = math.dot(n_ij, baryVel_j);
                double n_dot_baryVel_i = math.dot(n_ij, baryVel_i);

                double phi_i = workspace.PotentialPhi[i];
                double phi_j = workspace.PotentialPhi[j];

                // 1/2 * ((x_B - x_A) · a_B)
                // For i with B=j: (x_j - x_i) = displacement_ij
                double half_dx_dot_aB_for_i = 0.5 * math.dot(displacement_ij, workspace.NewtonianAccel[j]);
                // For j with B=i: (x_i - x_j) = -displacement_ij
                double half_dx_dot_aB_for_j = 0.5 * math.dot(-displacement_ij, workspace.NewtonianAccel[i]);

                double scalarBracket_for_i =
                      baryVelSq_i
                    + 2.0 * baryVelSq_j
                    - 4.0 * vi_Dot_vj
                    - (3.0 / 2.0) * (n_dot_baryVel_j * n_dot_baryVel_j)
                    - 4.0 * phi_i
                    - phi_j
                    + half_dx_dot_aB_for_i;

                double scalarBracket_for_j =
                      baryVelSq_j
                    + 2.0 * baryVelSq_i
                    - 4.0 * vi_Dot_vj
                    - (3.0 / 2.0) * (n_dot_baryVel_i * n_dot_baryVel_i)
                    - 4.0 * phi_j
                    - phi_i
                    + half_dx_dot_aB_for_j;

                workspace.SecondTermSum[i] += invC2 * accel_i_due_j * scalarBracket_for_i;
                workspace.SecondTermSum[j] += invC2 * accel_j_due_i * scalarBracket_for_j;

                // ----------------------------
                // THIRD TERM
                // (G m_B / r^2) * ( n_AB · (4 v_A - 3 v_B) ) * (v_A - v_B)
                // ----------------------------

                // A = i, B = j  => n_AB = (x_i - x_j)/r = -n_ij
                double3 n_AB_for_i = -n_ij;
                double scalarBracket3_for_i = math.dot(n_AB_for_i, (4.0 * baryVel_i) - (3.0 * baryVel_j));
                workspace.ThirdTermSum[i] += invC2 * (G * mass_j * invR2) * scalarBracket3_for_i * (baryVel_i - baryVel_j);

                // A = j, B = i  => n_AB = (x_j - x_i)/r = +n_ij
                double3 n_AB_for_j = n_ij;
                double scalarBracket3_for_j = math.dot(n_AB_for_j, (4.0 * baryVel_j) - (3.0 * baryVel_i));
                workspace.ThirdTermSum[j] += invC2 * (G * mass_i * invR2) * scalarBracket3_for_j * (baryVel_j - baryVel_i);

                // ----------------------------
                // FOURTH TERM 
                // (7 / (2 c^2)) * Σ (G m_B / r) * aNewton[B]
                // ----------------------------

                double fourthFactor_for_i = (7.0 / 2.0) * invC2 * (G * mass_j * invR);
                double fourthFactor_for_j = (7.0 / 2.0) * invC2 * (G * mass_i * invR);

                workspace.FourthTermSum[i] += fourthFactor_for_i * workspace.NewtonianAccel[j];
                workspace.FourthTermSum[j] += fourthFactor_for_j * workspace.NewtonianAccel[i];
            }
        }

        // 4) Final sum: aNewton + 1PN corrections
        for (int i = 0; i < bodyCount; i++)
        {
            if (masses[i] <= 0.0)
            {
                outBarycentricAccelerations[i] = double3.zero;
                continue;
            }

            outBarycentricAccelerations[i] = workspace.NewtonianAccel[i]
                                           + workspace.SecondTermSum[i]
                                           + workspace.ThirdTermSum[i]
                                           + workspace.FourthTermSum[i];
        }

        // 5) Enforce barycentric output: subtract COM acceleration (prevents drift)
        double totalMass = 0.0;
        double3 aCM = double3.zero;

        for (int i = 0; i < bodyCount; i++)
        {
            double mi = masses[i];
            if (mi <= 0.0) continue;

            totalMass += mi;
            aCM += mi * outBarycentricAccelerations[i];
        }

        if (totalMass > 0.0)
        {
            aCM /= totalMass;
            for (int i = 0; i < bodyCount; i++)
            {
                if (masses[i] <= 0.0) continue;
                outBarycentricAccelerations[i] -= aCM;
            }
        }

#if UNITY_EDITOR
        double3 check = double3.zero;
        double msum = 0.0;
        for (int i = 0; i < bodyCount; i++)
        {
            double mi = masses[i];
            if (mi <= 0.0) continue;
            msum += mi;
            check += mi * outBarycentricAccelerations[i];
        }
        if (msum > 0.0 && math.length(check) > 1e-10)
            Debug.LogWarning($"[SpacePhysics3D] COM accel residual: {check}");
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

        for (int i = 0; i < positions.Length; i++)
        {
            double mass = masses[i];
            if (mass <= 0.0) continue;

            weightedVelocities += velocities[i] * mass;
            weightedPositions += positions[i] * mass;
            totalMassKg += mass;
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
    static bool EnsureSameCount(int a, int b)
    {
        if (a != b)
        {
#if UNITY_EDITOR
            Debug.LogError($"[SpacePhysics3D] EnsureSameCount(): {a} must equal {b}");
#endif
            return false;
        }
        return true;
    }
}
