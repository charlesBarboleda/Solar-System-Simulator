using Unity.Mathematics;
using System;
using UnityEngine;


public static class NBodyDiagnostics
{
    static double _orbitTimeSimDays;
    static bool _waitingToExit;
    static double3 _initEarthRel;
    static int _orbits;
    static bool _hasPrevRdot;
    static double _prevRdot;
    static double _timeSincePeri;
    static double _rMin, _rMax;
    static double _simTimeDays;
    static int _diagStepCounter;
    static bool _diagBaselineSet;
    static double _E0;
    static double3 _P0, _L0, _Rcm0, _Vcm0;

    // Worst drifts measured
    static double _ErelMax, _PrelMax, _LrelMax, _RcmErrMax;

    public static void InitEarthDiagnostics(int earth, int sun, ReadOnlySpan<double3> positions)
    {
        _initEarthRel = positions[earth] - positions[sun];
        _orbitTimeSimDays = 0.0;
        _rMin = double.PositiveInfinity;
        _rMax = 0.0;
        _waitingToExit = true;
        _orbits = 0;
    }

    public static void InitSystemInvariantBaseline(ReadOnlySpan<double> _masses, ReadOnlySpan<double3> _positions, ReadOnlySpan<double3> _velocities, double G)
    {
        _simTimeDays = 0.0;
        _diagStepCounter = 0;

        ComputeSystemInvariants(_masses, _positions, _velocities, G,
            out _E0, out _P0, out _L0, out _Rcm0, out _Vcm0);

        _diagBaselineSet = true;

        _ErelMax = _PrelMax = _LrelMax = _RcmErrMax = 0.0;

#if UNITY_EDITOR
        Debug.Log($"[DiagBaseline] E0={_E0:E6}, |P0|={math.length(_P0):E6}, |L0|={math.length(_L0):E6}, " +
                  $"Rcm0={_Rcm0}, Vcm0={_Vcm0}");
#endif
    }

    public static void StepSystemDiagnostics(double dtDays, int diagEveryNSteps, ReadOnlySpan<double> _masses, ReadOnlySpan<double3> _positions, ReadOnlySpan<double3> _velocities, double G)
    {
        if (!_diagBaselineSet) return;

        _simTimeDays += dtDays;
        _diagStepCounter++;

        if (diagEveryNSteps > 1 && (_diagStepCounter % diagEveryNSteps) != 0)
            return;

        ComputeSystemInvariants(_masses, _positions, _velocities, G,
            out double E, out double3 P, out double3 L, out double3 Rcm, out double3 Vcm);

        const double eps = 1e-30;

        double Erel = math.abs(E - _E0) / math.max(math.abs(_E0), eps);
        double Prel = math.length(P - _P0) / math.max(math.length(_P0), 1.0); // if P0≈0, use absolute scale 1
        double Lrel = math.length(L - _L0) / math.max(math.length(_L0), eps);

        // Barycenter position should follow Rcm0 + Vcm0*t if momentum is conserved.
        double3 RcmExpected = _Rcm0 + _Vcm0 * _simTimeDays;
        double RcmErr = math.length(Rcm - RcmExpected);

        _ErelMax = math.max(_ErelMax, Erel);
        _PrelMax = math.max(_PrelMax, Prel);
        _LrelMax = math.max(_LrelMax, Lrel);
        _RcmErrMax = math.max(_RcmErrMax, RcmErr);

#if UNITY_EDITOR
        Debug.Log($"[SysDiag t={_simTimeDays:F3}d] dE/E={Erel:E3} | dP={math.length(P - _P0):E3} dL/L={Lrel:E3} | RcmErr={RcmErr:E6}");
#endif
    }

    public static void ComputeSystemInvariants(
       ReadOnlySpan<double> masses,        // mass[i] 
       ReadOnlySpan<double3> positions,     // pos[i] 
       ReadOnlySpan<double3> velocities,    // vel[i] 
       double G,
       out double totalEnergy,              // E = kinetic + potential (Newtonian)
       out double3 totalMomentum,           // P = Σ m v
       out double3 totalAngularMomentum,    // L = Σ m (r × v)
       out double3 barycenterPosition,    // Rcm = (Σ m r) / Σ m
       out double3 barycenterVelocity     // Vcm = (Σ m v) / Σ m
   )
    {
        int bodyCount = masses.Length;

        double totalMass = 0.0;

        totalEnergy = 0.0;
        totalMomentum = double3.zero; // totalMomentum == sum of all weighted velocity in the system
        totalAngularMomentum = double3.zero;

        // Only need Σmass*pos (barycenter position). Barycenter velocity comes from totalMomentum / totalMass.
        double3 sumWeightedPos = double3.zero;

        // 1) Single-body sums
        // Computes: total mass, kinetic energy, linear momentum, angular momentum, barycenter position and velocity
        for (int a = 0; a < bodyCount; a++)
        {
            double mass_a = masses[a];
            if (mass_a <= 0.0) continue; // skip invalid bodies

            double3 pos_a = positions[a];
            double3 vel_a = velocities[a];

            // Accumulate total system mass
            totalMass += mass_a;

            // Kinetic energy = 1/2 * mass * (vel_a)^2
            double speedSquared = math.lengthsq(vel_a);
            totalEnergy += 0.5 * mass_a * speedSquared;

            // Linear momentum/Weighted velocity = mass * velocity
            double3 weightedVel_a = mass_a * vel_a;
            totalMomentum += weightedVel_a;

            // Angular momentum = mass * (position * velocity)
            totalAngularMomentum += mass_a * math.cross(pos_a, vel_a);

            // Sum of all weighted mass
            sumWeightedPos += mass_a * pos_a;
        }

        // Solve barycenterPosition and barycenterVector from mass & momentum
        if (totalMass > 0.0)
            SpacePhysics3D.GetBarycenterVectorsFrom(totalMass, sumWeightedPos, totalMomentum, out barycenterPosition, out barycenterVelocity);
        else
        {
            barycenterPosition = double3.zero;
            barycenterVelocity = double3.zero;
        }

        // 2) Pairwise potential energy
        // Newtonian gravitational potential energy for each pair: U_ab = -G * mass_a * mass_b / displacement_ab
        // Do a<b to count each pair once
        // Complexity is O(n^2)
        for (int a = 0; a < bodyCount; a++)
        {
            double mass_a = masses[a];
            if (mass_a <= 0.0) continue;

            for (int b = a + 1; b < bodyCount; b++)
            {
                double mass_b = masses[b];
                if (mass_b <= 0.0) continue;

                double3 displacement_ab = positions[a] - positions[b]; // r_ij vector
                double distanceSq = math.lengthsq(displacement_ab); // |r_ij|
                if (distanceSq <= 0.0) continue;

                double invDistance = 1.0 / Math.Sqrt(distanceSq);
                totalEnergy -= G * mass_a * mass_b * invDistance;
            }
        }
    }

    public static void Diagnostics_OrbitByPeriapsis(
        int indexBodyA,                 // e.g. Earth
        int indexBodyB,                 // e.g. Sun
        double dtDays,                  // simulation time advanced this step (days)
        ReadOnlySpan<double3> positions,
        ReadOnlySpan<double3> velocities,
        double mu                        // gravitational parameter (mu) = G*(mass_A + mass_B)
    )
    {
        // Track time since last detected periapsis (closest approach)
        _timeSincePeri += dtDays;

        // A as seen from B (USED ONLY FOR TWO-BODY SYSTEMS)
        double3 relativePosition_ab = positions[indexBodyA] - positions[indexBodyB];   // relative position vector
        double3 relativeVelocity_ab = velocities[indexBodyA] - velocities[indexBodyB]; // relative velocity vector

        double distance = math.length(relativePosition_ab); // scalar separation 
        if (distance <= 0.0) return;

        // Track min/max distance within the current orbit "window"
        _rMin = math.min(_rMin, distance); // periapsis estimate for this orbit
        _rMax = math.max(_rMax, distance); // apoapsis estimate for this orbit

        // Radial velocity: how fast the distance is changing (dr/dt)
        // Positive => moving apart, Negative => moving closer
        double radialSpeed = math.dot(relativePosition_ab, relativeVelocity_ab) / distance;

        // First sample needs a previous radialSpeed to detect a sign change
        if (!_hasPrevRdot)
        {
            _hasPrevRdot = true;
            _prevRdot = radialSpeed;

            // Initialize orbit window tracking
            _rMin = distance;
            _rMax = distance;
            _timeSincePeri = 0.0;
            return;
        }

        // Periapsis detection:
        // During approach: radialSpeed < 0 (distance decreasing)
        // After closest point: radialSpeed > 0 (distance increasing)
        // So periapsis happens when radialSpeed crosses from negative to positive
        if (_prevRdot < 0.0 && radialSpeed >= 0.0)
        {
            _orbits++;

            // --- Osculating orbital elements (2-body) ---
            // These describe "the orbit you would have right now if it became a perfect 2-body system"
            // In a pure 2-body sim they should be nearly constant

            // Speed squared
            double speed2 = math.lengthsq(relativeVelocity_ab);

            // Specific orbital energy (energy per unit mass of the reduced system):
            // epsilon = v^2/2 - mu/r
            double specificEnergy = 0.5 * speed2 - mu / distance;

            // Semi-major axis:
            // a = -mu / (2 * epsilon)
            // For a bound ellipse: epsilon < 0 => a > 0
            double semiMajorAxis = -mu / (2.0 * specificEnergy);

            // Specific angular momentum vector:
            // h = r x v
            // Direction = orbit plane normal, magnitude relates to how "wide" the orbit is
            double3 specificAngularMomentum = math.cross(relativePosition_ab, relativeVelocity_ab);

            // Eccentricity vector:
            // eVec points toward periapsis and its length is the eccentricity "e"
            // eVec = (v x h)/mu - rHat
            double3 rHat = relativePosition_ab / distance;
            double3 eccentricityVector = (math.cross(relativeVelocity_ab, specificAngularMomentum) / mu) - rHat;
            double eccentricity = math.length(eccentricityVector);

            // Keplerian period from semi-major axis:
            // T = 2π * sqrt(a^3 / mu)
            // This is a "theoretical" period implied by your current a and mu
            double keplerPeriod = 2.0 * math.PI * math.sqrt((semiMajorAxis * semiMajorAxis * semiMajorAxis) / mu);

            Debug.Log(
                $"[{_orbits}] Period={_timeSincePeri:F4} days | " +
                $"Semi-Major Axis={semiMajorAxis / PhysicsConstants.UNITY_UNITS_PER_AU:F12} AU | " +
                $"Eccentricity={eccentricity:F12} | " +
                $"Periapsis={_rMin / PhysicsConstants.UNITY_UNITS_PER_AU:F12} AU | " +
                $"Apoapsis={_rMax / PhysicsConstants.UNITY_UNITS_PER_AU:F12} AU | " +
                $"TKepler={keplerPeriod:F4} days"
            );

            // Reset orbit window after periapsis so next periapsis-to-periapsis window is measured cleanly
            _timeSincePeri = 0.0;
            _rMin = distance;
            _rMax = distance;
        }

        // Store current radialSpeed for next step’s crossing test
        _prevRdot = radialSpeed;
    }


}