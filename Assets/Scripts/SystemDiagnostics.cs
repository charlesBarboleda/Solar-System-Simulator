using Unity.Mathematics;
using System;
using UnityEngine;


public static class SystemDiagnostics
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

    public static void InitSystemInvariantBaseline(ReadOnlySpan<double> _masses, ReadOnlySpan<double3> _positions, ReadOnlySpan<double3> _velocities)
    {
        _simTimeDays = 0.0;
        _diagStepCounter = 0;

        ComputeSystemInvariants(_masses, _positions, _velocities,
            out _E0, out _P0, out _L0, out _Rcm0, out _Vcm0);

        _diagBaselineSet = true;

        _ErelMax = _PrelMax = _LrelMax = _RcmErrMax = 0.0;

#if UNITY_EDITOR
        Debug.Log($"[DiagBaseline] E0={_E0:E6}, |P0|={math.length(_P0):E6}, |L0|={math.length(_L0):E6}, " +
                  $"Rcm0={_Rcm0}, Vcm0={_Vcm0}");
#endif
    }

    public static void StepSystemDiagnostics(double dtDays, int diagEveryNSteps, ReadOnlySpan<double> _masses, ReadOnlySpan<double3> _positions, ReadOnlySpan<double3> _velocities)
    {
        if (!_diagBaselineSet) return;

        _simTimeDays += dtDays;
        _diagStepCounter++;

        if (diagEveryNSteps > 1 && (_diagStepCounter % diagEveryNSteps) != 0)
            return;

        ComputeSystemInvariants(_masses, _positions, _velocities,
            out double E, out double3 P, out double3 L, out double3 Rcm, out double3 Vcm);

        const double eps = 1e-30;

        double Erel = math.abs(E - _E0) / math.max(math.abs(_E0), eps);
        double Prel = math.length(P - _P0) / math.max(math.length(_P0), 1.0); // if P0≈0, use absolute scale 1
        double Lrel = math.length(L - _L0) / math.max(math.length(_L0), eps);

        // COM position should follow Rcm0 + Vcm0*t if momentum is conserved.
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
    ReadOnlySpan<double> m,
    ReadOnlySpan<double3> x,
    ReadOnlySpan<double3> v,
    out double E,
    out double3 P,
    out double3 L,
    out double3 Rcm,
    out double3 Vcm)
    {
        int n = m.Length;

        double M = 0.0;
        E = 0.0;
        P = double3.zero;
        L = double3.zero;

        double3 sumMx = double3.zero;
        double3 sumMv = double3.zero;

        // Kinetic, momentum, angular momentum, COM sums
        for (int i = 0; i < n; i++)
        {
            double mi = m[i];
            if (mi <= 0.0) continue;

            M += mi;

            double v2 = math.lengthsq(v[i]);
            E += 0.5 * mi * v2;

            P += mi * v[i];
            L += mi * math.cross(x[i], v[i]);

            sumMx += mi * x[i];
            sumMv += mi * v[i];
        }

        Rcm = (M > 0.0) ? (sumMx / M) : double3.zero;
        Vcm = (M > 0.0) ? (sumMv / M) : double3.zero;

        // Newtonian potential energy (O(n^2))
        double G = PhysicsConstants.UNITY_G;
        for (int i = 0; i < n; i++)
        {
            double mi = m[i];
            if (mi <= 0.0) continue;

            for (int j = i + 1; j < n; j++)
            {
                double mj = m[j];
                if (mj <= 0.0) continue;

                double3 rij = x[i] - x[j];
                double r = math.length(rij);
                if (r <= 0.0) continue;

                E -= G * mi * mj / r;
            }
        }
    }


    public static void Diagnostics_OrbitByPeriapsis(
     int earth, int sun,
     double dtDays,
     ReadOnlySpan<double3> pos,
     ReadOnlySpan<double3> vel,
     double mu) // mu = G*(M_sun + M_earth) in *your* units
    {
        _timeSincePeri += dtDays;

        double3 rVec = pos[earth] - pos[sun];
        double3 vVec = vel[earth] - vel[sun];

        double r = math.length(rVec);
        if (r <= 0.0) return;

        // Track extrema within the current orbit window
        _rMin = math.min(_rMin, r);
        _rMax = math.max(_rMax, r);

        double rdot = math.dot(rVec, vVec) / r;

        if (!_hasPrevRdot)
        {
            _hasPrevRdot = true;
            _prevRdot = rdot;
            _rMin = r;
            _rMax = r;
            _timeSincePeri = 0.0;
            return;
        }

        // Periapsis crossing
        if (_prevRdot < 0.0 && rdot >= 0.0)
        {
            _orbits++;

            // Compute osculating elements (2-body)
            double v2 = math.lengthsq(vVec);
            double specificEnergy = 0.5 * v2 - mu / r;
            double a = -mu / (2.0 * specificEnergy); // semi-major axis (in your distance units)

            double3 h = math.cross(rVec, vVec);
            double3 eVec = (math.cross(vVec, h) / mu) - (rVec / r);
            double e = math.length(eVec);

            double periodKepler = 2.0 * math.PI * math.sqrt((a * a * a) / mu); // in sim-days if mu uses days

            Debug.Log(
                $"[{_orbits}] Period={_timeSincePeri:F4} days | " +
                $"Semi-Major Axis={a / PhysicsConstants.UNITY_UNITS_PER_AU:F12} AU | Eccentricity={e:F12} | " +
                $"Periapsis={_rMin / PhysicsConstants.UNITY_UNITS_PER_AU:F12} AU | " +
                $"Apoapsis={_rMax / PhysicsConstants.UNITY_UNITS_PER_AU:F12} AU | " +
                $"TKepler={periodKepler:F4} days");

            // Reset window for next orbit
            _timeSincePeri = 0.0;
            _rMin = r;
            _rMax = r;
        }

        _prevRdot = rdot;
    }

}