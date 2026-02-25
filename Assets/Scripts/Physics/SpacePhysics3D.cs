using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

public static class SpacePhysics3D
{
    // Workspace to avoid per-step allocations (NativeArrays, persistent).
    public sealed class Workspace_EIH : IDisposable
    {
        public NativeArray<double3> BarycentricPositions;   // r'_a
        public NativeArray<double3> BarycentricVelocities;  // v'_a
        public NativeArray<byte> ActiveMask;                // 1 active, 0 inactive

        public NativeArray<double> PotentialPhi;            // phi[a] = Σ_{b≠a} G m_b / r_ab
        public NativeArray<double3> NewtonianAccel;         // aNewton[a]
        public NativeArray<double3> AccelApprox;            // aApprox[a] (a_B estimate)

        public NativeArray<double3> SecondTermSum;
        public NativeArray<double3> ThirdTermSum;
        public NativeArray<double3> FourthTermSum;

        public int Length => ActiveMask.IsCreated ? ActiveMask.Length : 0;

        public void EnsureCapacity(int count)
        {
            if (count <= 0) return;

            if (ActiveMask.IsCreated && ActiveMask.Length == count) return;

            Dispose();

            BarycentricPositions = new NativeArray<double3>(count, Allocator.Persistent);
            BarycentricVelocities = new NativeArray<double3>(count, Allocator.Persistent);
            ActiveMask = new NativeArray<byte>(count, Allocator.Persistent);

            PotentialPhi = new NativeArray<double>(count, Allocator.Persistent);
            NewtonianAccel = new NativeArray<double3>(count, Allocator.Persistent);
            AccelApprox = new NativeArray<double3>(count, Allocator.Persistent);

            SecondTermSum = new NativeArray<double3>(count, Allocator.Persistent);
            ThirdTermSum = new NativeArray<double3>(count, Allocator.Persistent);
            FourthTermSum = new NativeArray<double3>(count, Allocator.Persistent);
        }

        public void Dispose()
        {
            if (BarycentricPositions.IsCreated) BarycentricPositions.Dispose();
            if (BarycentricVelocities.IsCreated) BarycentricVelocities.Dispose();
            if (ActiveMask.IsCreated) ActiveMask.Dispose();

            if (PotentialPhi.IsCreated) PotentialPhi.Dispose();
            if (NewtonianAccel.IsCreated) NewtonianAccel.Dispose();
            if (AccelApprox.IsCreated) AccelApprox.Dispose();

            if (SecondTermSum.IsCreated) SecondTermSum.Dispose();
            if (ThirdTermSum.IsCreated) ThirdTermSum.Dispose();
            if (FourthTermSum.IsCreated) ThirdTermSum.Dispose();
            if (FourthTermSum.IsCreated) FourthTermSum.Dispose();
        }
    }


    /// <summary>
    /// Computes barycentric positions/velocities and ActiveMask in a single Burst job (deterministic loop order).
    /// </summary>
    [BurstCompile(FloatMode = FloatMode.Strict, FloatPrecision = FloatPrecision.High)]
    struct BuildBarycentricVectorsJob : IJob
    {
        [ReadOnly] public NativeArray<double3> Positions;
        [ReadOnly] public NativeArray<double3> Velocities;
        [ReadOnly] public NativeArray<double> Masses;

        public NativeArray<byte> ActiveMask;
        public NativeArray<double3> BaryPos;
        public NativeArray<double3> BaryVel;

        public void Execute()
        {
            int n = Positions.Length;

            double3 weightedPos = double3.zero;
            double3 weightedVel = double3.zero;
            double totalMass = 0.0;

            // Active + weighted sums (deterministic order)
            for (int a = 0; a < n; a++)
            {
                double m = Masses[a];
                byte active = (m > 0.0) ? (byte)1 : (byte)0;
                ActiveMask[a] = active;

                if (active == 0)
                    continue;

                weightedPos += Positions[a] * m;
                weightedVel += Velocities[a] * m;
                totalMass += m;
            }

            if (totalMass <= 0.0)
            {
                // No valid bodies: zero everything deterministically.
                for (int a = 0; a < n; a++)
                {
                    ActiveMask[a] = 0;
                    BaryPos[a] = double3.zero;
                    BaryVel[a] = double3.zero;
                }
                return;
            }

            double3 barycenterPos = weightedPos / totalMass;
            double3 barycenterVel = weightedVel / totalMass;

            for (int a = 0; a < n; a++)
            {
                if (ActiveMask[a] == 0)
                {
                    BaryPos[a] = double3.zero;
                    BaryVel[a] = double3.zero;
                    continue;
                }

                BaryPos[a] = Positions[a] - barycenterPos;
                BaryVel[a] = Velocities[a] - barycenterVel;
            }
        }
    }

    /// <summary>
    /// Computes phi[a] and NewtonianAccel[a] using the SAME pair-sweep architecture as your original CPU version.
    /// Deterministic (single job, fixed loop order) and preserves equal-and-opposite contributions.
    /// </summary>
    [BurstCompile(FloatMode = FloatMode.Strict, FloatPrecision = FloatPrecision.High)]
    struct PhiAndNewtonPairSweepJob : IJob
    {
        [ReadOnly] public NativeArray<double> Masses;
        [ReadOnly] public NativeArray<double3> BaryPos;
        [ReadOnly] public NativeArray<byte> ActiveMask;

        public NativeArray<double> Phi;
        public NativeArray<double3> Newton;

        public double G;
        public double MinDistSq;

        public void Execute()
        {
            int n = BaryPos.Length;

            // Clear outputs (deterministic)
            for (int a = 0; a < n; a++)
            {
                Phi[a] = 0.0;
                Newton[a] = double3.zero;
            }

            // Pair sweep (same structure as your original)
            for (int a = 0; a < n - 1; a++)
            {
                if (ActiveMask[a] == 0) continue;
                double mass_a = Masses[a];

                for (int b = a + 1; b < n; b++)
                {
                    if (ActiveMask[b] == 0) continue;
                    double mass_b = Masses[b];

                    double3 displacement_ab = BaryPos[b] - BaryPos[a];
                    double r2_ab = math.lengthsq(displacement_ab);
                    if (r2_ab < MinDistSq) r2_ab = MinDistSq;

                    double invR = 1.0 / math.sqrt(r2_ab);
                    double invR3 = invR / r2_ab;

                    // Potential contributions
                    Phi[a] += (G * mass_b) * invR;
                    Phi[b] += (G * mass_a) * invR;

                    // Newtonian accel contributions (equal and opposite)
                    double3 directionOverR3 = displacement_ab * invR3;
                    Newton[a] += (G * mass_b) * directionOverR3;
                    Newton[b] -= (G * mass_a) * directionOverR3;
                }
            }
        }
    }

    /// <summary>
    /// accelApprox[a] = Newton[a]
    /// </summary>
    [BurstCompile(FloatMode = FloatMode.Strict, FloatPrecision = FloatPrecision.High)]
    struct InitAccelApproxFromNewtonJob : IJob
    {
        [ReadOnly] public NativeArray<double3> Newton;
        public NativeArray<double3> AccelApprox;

        public void Execute()
        {
            int n = Newton.Length;
            for (int a = 0; a < n; a++)
                AccelApprox[a] = Newton[a];
        }
    }

    /// <summary>
    /// Full EIH 1PN fixed-point iteration implemented as ONE Burst job (no main-thread ping-pong).
    /// Uses the same pair-sweep structure and term accumulation arrays as your original CPU implementation.
    /// </summary>
    [BurstCompile(FloatMode = FloatMode.Strict, FloatPrecision = FloatPrecision.High)]
    struct Eih1PnIteratedPairJob : IJob
    {
        [ReadOnly] public NativeArray<double> Masses;
        [ReadOnly] public NativeArray<double3> BaryPos;
        [ReadOnly] public NativeArray<double3> BaryVel;
        [ReadOnly] public NativeArray<byte> ActiveMask;

        [ReadOnly] public NativeArray<double> Phi;
        [ReadOnly] public NativeArray<double3> Newton;

        public NativeArray<double3> AccelApprox;   // updated between iterations if FixedPointIterated
        public NativeArray<double3> OutAccel;      // final output

        public NativeArray<double3> SecondSum;
        public NativeArray<double3> ThirdSum;
        public NativeArray<double3> FourthSum;

        public double G;
        public double InvC2;
        public double MinDistSq;

        public AccelBMode AccelBMode;
        public int AccelIterations;

        public void Execute()
        {
            int n = BaryPos.Length;

            int iterations = (AccelBMode == AccelBMode.FixedPointIterated) ? math.max(1, AccelIterations) : 1;

            for (int iter = 0; iter < iterations; iter++)
            {
                // Clear correction sums (deterministic)
                for (int a = 0; a < n; a++)
                {
                    SecondSum[a] = double3.zero;
                    ThirdSum[a] = double3.zero;
                    FourthSum[a] = double3.zero;
                }

                // Pair sweep (matches your original structure)
                for (int a = 0; a < n - 1; a++)
                {
                    if (ActiveMask[a] == 0) continue;
                    double mass_a = Masses[a];

                    double3 baryVel_a = BaryVel[a];
                    double baryVelSq_a = math.lengthsq(baryVel_a);

                    for (int b = a + 1; b < n; b++)
                    {
                        if (ActiveMask[b] == 0) continue;
                        double mass_b = Masses[b];

                        double3 displacement_ab = BaryPos[b] - BaryPos[a];
                        double r2_ab = math.lengthsq(displacement_ab);
                        if (r2_ab < MinDistSq) r2_ab = MinDistSq;

                        double invR = 1.0 / math.sqrt(r2_ab);
                        double invR2 = 1.0 / r2_ab;
                        double invR3 = invR / r2_ab;

                        double3 n_ab = -displacement_ab * invR; // unit vector direction from 'b' to 'a'
                        double3 n_ba = -n_ab;                   // unit vector direction from 'a' to 'b'

                        double3 baryVel_b = BaryVel[b];
                        double baryVelSq_b = math.lengthsq(baryVel_b);

                        double va_Dot_vb = math.dot(baryVel_a, baryVel_b);

                        double3 accel_a_due_b = (G * mass_b) * (displacement_ab * invR3);
                        double3 accel_b_due_a = -(G * mass_a) * (displacement_ab * invR3);

                        double n_ab_dot_baryVel_b = math.dot(n_ab, baryVel_b);
                        double n_ba_dot_baryVel_a = math.dot(n_ba, baryVel_a);

                        double phi_a = Phi[a];
                        double phi_b = Phi[b];

                        double half_dx_dot_aB_for_a = 0.5 * math.dot(displacement_ab, AccelApprox[b]);
                        double half_dx_dot_aB_for_b = 0.5 * math.dot(-displacement_ab, AccelApprox[a]);

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

                        SecondSum[a] += InvC2 * accel_a_due_b * scalarBracket_for_a;
                        SecondSum[b] += InvC2 * accel_b_due_a * scalarBracket_for_b;

                        // Third term
                        double3 n_ab_for_a = n_ab;
                        double scalarBracket3_for_a = math.dot(n_ab_for_a, (4.0 * baryVel_a) - (3.0 * baryVel_b));
                        ThirdSum[a] += InvC2 * (G * mass_b * invR2) * scalarBracket3_for_a * (baryVel_a - baryVel_b);

                        double3 n_ab_for_b = n_ba;
                        double scalarBracket3_for_b = math.dot(n_ab_for_b, (4.0 * baryVel_b) - (3.0 * baryVel_a));
                        ThirdSum[b] += InvC2 * (G * mass_a * invR2) * scalarBracket3_for_b * (baryVel_b - baryVel_a);

                        // Fourth term
                        double fourthFactor_for_a = (7.0 / 2.0) * InvC2 * (G * mass_b * invR);
                        double fourthFactor_for_b = (7.0 / 2.0) * InvC2 * (G * mass_a * invR);

                        FourthSum[a] += fourthFactor_for_a * AccelApprox[b];
                        FourthSum[b] += fourthFactor_for_b * AccelApprox[a];
                    }
                }

                // Final sum: aNewton + 1PN corrections
                for (int a = 0; a < n; a++)
                {
                    if (ActiveMask[a] == 0)
                    {
                        OutAccel[a] = double3.zero;
                        continue;
                    }

                    OutAccel[a] = Newton[a] + SecondSum[a] + ThirdSum[a] + FourthSum[a];
                }

                // Enforce Σ m a = 0 in barycentric frame (deterministic order)
                double totalMass = 0.0;
                double3 aCM = double3.zero;
                for (int a = 0; a < n; a++)
                {
                    if (ActiveMask[a] == 0) continue;
                    double m = Masses[a];
                    totalMass += m;
                    aCM += m * OutAccel[a];
                }

                if (totalMass > 0.0)
                {
                    aCM /= totalMass;
                    for (int a = 0; a < n; a++)
                    {
                        if (ActiveMask[a] == 0) continue;
                        OutAccel[a] -= aCM;
                    }
                }

                // Feed back for next iteration if iterated mode
                if (AccelBMode == AccelBMode.FixedPointIterated && iter < iterations - 1)
                {
                    for (int a = 0; a < n; a++)
                        AccelApprox[a] = OutAccel[a];
                }
            }
        }
    }

    /// <summary>
    /// Newtonian acceleration pair sweep (matches your original structure, equal-and-opposite).
    /// </summary>
    [BurstCompile(FloatMode = FloatMode.Strict, FloatPrecision = FloatPrecision.High)]
    struct NewtonianPairSweepJob : IJob
    {
        [ReadOnly] public NativeArray<double> Masses;
        [ReadOnly] public NativeArray<double3> Positions;

        public NativeArray<double3> OutAccel;

        public double G;
        public double MinDistSq;

        public void Execute()
        {
            int n = Positions.Length;

            // Clear output
            for (int a = 0; a < n; a++)
                OutAccel[a] = double3.zero;

            for (int a = 0; a < n - 1; a++)
            {
                double mass_a = Masses[a];
                if (mass_a <= 0.0) continue;

                double3 pos_a = Positions[a];

                for (int b = a + 1; b < n; b++)
                {
                    double mass_b = Masses[b];
                    if (mass_b <= 0.0) continue;

                    double3 displacement_ab = Positions[b] - pos_a;
                    double r2_ab = math.lengthsq(displacement_ab);
                    if (r2_ab < MinDistSq) r2_ab = MinDistSq;

                    double invR = 1.0 / math.sqrt(r2_ab);
                    double invR3 = invR / r2_ab;

                    OutAccel[a] += (G * mass_b) * displacement_ab * invR3;
                    OutAccel[b] -= (G * mass_a) * displacement_ab * invR3;
                }
            }
        }
    }

    /// <summary>
    /// Schedule Newtonian acceleration. Caller decides when to Complete().
    /// Preserves original method name via wrapper below.
    /// </summary>
    public static JobHandle NBodyAccelVectorFrom_Schedule(
        NativeArray<double> masses,
        NativeArray<double3> positions,
        NativeArray<double3> outNewtonianAccelerations,
        JobHandle dependsOn = default)
    {
        int n = positions.Length;
        if (masses.Length != n || outNewtonianAccelerations.Length != n) return dependsOn;

        double G = PhysicsConstants.UNITY_G;
        double minDist = PhysicsConstants.UNITY_MIN_DISTANCE;
        double minDistSq = minDist * minDist;

        var job = new NewtonianPairSweepJob
        {
            Masses = masses,
            Positions = positions,
            OutAccel = outNewtonianAccelerations,
            G = G,
            MinDistSq = minDistSq
        };

        return job.Schedule(dependsOn);
    }

    /// <summary>
    /// Compatibility wrapper using the same method name idea: schedules and completes ONCE.
    /// </summary>
    public static void NBodyAccelVectorFrom(
        NativeArray<double> masses,
        NativeArray<double3> positions,
        NativeArray<double3> outNewtonianAccelerations)
    {
        var h = NBodyAccelVectorFrom_Schedule(masses, positions, outNewtonianAccelerations);
        h.Complete();
    }

    /// <summary>
    /// Schedule EIH 1PN. Caller decides when to Complete().
    /// </summary>
    public static JobHandle Einstein_Infeld_Hoffmann_1PN_Schedule(
        NativeArray<double3> positions,
        NativeArray<double3> velocities,
        NativeArray<double> masses,
        NativeArray<double3> outBarycentricAccelerations,
        Workspace_EIH workspace,
        AccelBMode accelBMode = AccelBMode.NewtonianApprox,
        int accelIterations = 2,
        JobHandle dependsOn = default)
    {
        int n = positions.Length;
        if (velocities.Length != n || masses.Length != n || outBarycentricAccelerations.Length != n)
            return dependsOn;

        if (workspace == null)
            return dependsOn;

        workspace.EnsureCapacity(n);

        double c = PhysicsConstants.UNITY_SPEED_OF_LIGHT;
        double invC2 = 1.0 / (c * c);
        double G = PhysicsConstants.UNITY_G;
        double minDist = PhysicsConstants.UNITY_MIN_DISTANCE;
        double minDistSq = minDist * minDist;

        // 1) Build barycentric vectors + active mask
        var baryJob = new BuildBarycentricVectorsJob
        {
            Positions = positions,
            Velocities = velocities,
            Masses = masses,
            ActiveMask = workspace.ActiveMask,
            BaryPos = workspace.BarycentricPositions,
            BaryVel = workspace.BarycentricVelocities
        };
        JobHandle h0 = baryJob.Schedule(dependsOn);

        // 2) Phi + Newton pair sweep
        var phiNewtJob = new PhiAndNewtonPairSweepJob
        {
            Masses = masses,
            BaryPos = workspace.BarycentricPositions,
            ActiveMask = workspace.ActiveMask,
            Phi = workspace.PotentialPhi,
            Newton = workspace.NewtonianAccel,
            G = G,
            MinDistSq = minDistSq
        };
        JobHandle h1 = phiNewtJob.Schedule(h0);

        // 3) accelApprox = Newton
        var initApproxJob = new InitAccelApproxFromNewtonJob
        {
            Newton = workspace.NewtonianAccel,
            AccelApprox = workspace.AccelApprox
        };
        JobHandle h2 = initApproxJob.Schedule(h1);

        // 4) EIH fixed-point iterations + enforce bary constraint (all inside one job)
        var eihJob = new Eih1PnIteratedPairJob
        {
            Masses = masses,
            BaryPos = workspace.BarycentricPositions,
            BaryVel = workspace.BarycentricVelocities,
            ActiveMask = workspace.ActiveMask,

            Phi = workspace.PotentialPhi,
            Newton = workspace.NewtonianAccel,

            AccelApprox = workspace.AccelApprox,
            OutAccel = outBarycentricAccelerations,

            SecondSum = workspace.SecondTermSum,
            ThirdSum = workspace.ThirdTermSum,
            FourthSum = workspace.FourthTermSum,

            G = G,
            InvC2 = invC2,
            MinDistSq = minDistSq,

            AccelBMode = accelBMode,
            AccelIterations = accelIterations
        };

        JobHandle h3 = eihJob.Schedule(h2);
        return h3;
    }

    /// <summary>
    /// Compatibility wrapper: schedules and completes ONCE.
    /// </summary>
    public static void Einstein_Infeld_Hoffmann_1PN(
        NativeArray<double3> positions,
        NativeArray<double3> velocities,
        NativeArray<double> masses,
        NativeArray<double3> outBarycentricAccelerations,
        Workspace_EIH workspace,
        AccelBMode accelBMode = AccelBMode.NewtonianApprox,
        int accelIterations = 2)
    {
        var h = Einstein_Infeld_Hoffmann_1PN_Schedule(
            positions, velocities, masses, outBarycentricAccelerations,
            workspace, accelBMode, accelIterations, default);

        h.Complete();
    }
}