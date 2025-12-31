using Unity.Mathematics;
using UnityEngine;
using System;

public static class NBodyDiagnostics
{
    // Holds state between calls (owned by NBodyManager, passed by ref).
    public struct Workspace_Diagnosis
    {
        public bool Initialized;

        // Simulation time since the epoch in SIM-DAYS (your integrator's time unit).
        public double SimDaysSinceEpoch;

        // Next day boundary to log (0 = epoch @ 00:00, 1 = +1 day @ 00:00, etc.)
        public int NextDayToLog;

        // Snapshot of state BEFORE IntegrateOneStep(dt)
        public double3[] PosPrev;
        public double3[] VelPrev;
    }

    /// <summary>
    /// Call once (or when body count changes).
    /// epochJdTdb should match Horizons (e.g., 2460676.5 for 2025-Jan-01 00:00:00 TDB).
    /// </summary>
    public static void EnsureInitialized(ref Workspace_Diagnosis ws, int numBodies)
    {
        if (numBodies <= 0)
            return;

        if (!ws.Initialized || ws.PosPrev == null || ws.PosPrev.Length != numBodies)
        {
            ws.PosPrev = new double3[numBodies];
            ws.VelPrev = new double3[numBodies];
            ws.SimDaysSinceEpoch = 0.0;
            ws.NextDayToLog = 0;
            ws.Initialized = true;
        }
    }

    /// <summary>Copy "before step" arrays so we can interpolate to exact day-boundaries.</summary>
    public static void SnapshotBeforeStep(ref Workspace_Diagnosis ws, double3[] positions, double3[] velocities)
    {
        int n = positions.Length;
        for (int i = 0; i < n; i++)
        {
            ws.PosPrev[i] = positions[i];
            ws.VelPrev[i] = velocities[i];
        }
    }

    /// <summary>
    /// Call AFTER IntegrateOneStep(dtStepDays).
    /// Logs when the sim crosses 00:00 boundaries (whole simulated days).
    /// </summary>
    /// <param name="epochJdTdb">Horizons JD(TDB) at sim start (e.g., 2460676.5)</param>
    /// <param name="dtStepDays">Integrator step size in days</param>
    /// <param name="positionsAfter">State after the step</param>
    /// <param name="velocitiesAfter">State after the step</param>
    /// <param name="bodyNameOf">Optional function to provide names for logging</param>
    /// <param name="logBodyIndex">If >=0, logs only that body; if -1, logs all bodies.</param>
    public static void LogAtDailyMidnightBoundaries(
        ref Workspace_Diagnosis ws,
        double epochJdTdb,
        double dtStepDays,
        double3[] positionsAfter,
        double3[] velocitiesAfter,
        Func<int, string> bodyNameOf = null,
        int logBodyIndex = -1)
    {
        if (!ws.Initialized) return;
        if (dtStepDays <= 0.0) return;

        double prevDays = ws.SimDaysSinceEpoch;
        double nextDays = prevDays + dtStepDays;

        // If we crossed one or more whole-day boundaries, log each.
        while (ws.NextDayToLog <= (int)Math.Floor(nextDays))
        {
            double targetDay = ws.NextDayToLog;

            // alpha in [0,1] for interpolation between prev and after
            double alpha = (targetDay - prevDays) / dtStepDays;
            alpha = Math.Clamp(alpha, 0.0, 1.0);

            double jdNow = epochJdTdb + targetDay;

            if (logBodyIndex >= 0)
            {
                LogOneBody(ws, jdNow, ws.NextDayToLog, logBodyIndex, alpha,
                           positionsAfter, velocitiesAfter, bodyNameOf);
            }
            else
            {
                int n = positionsAfter.Length;
                for (int i = 0; i < n; i++)
                    LogOneBody(ws, jdNow, ws.NextDayToLog, i, alpha,
                               positionsAfter, velocitiesAfter, bodyNameOf);
            }

            ws.NextDayToLog++;
        }

        ws.SimDaysSinceEpoch = nextDays;
    }

    private static void LogOneBody(
        Workspace_Diagnosis ws,
        double jdTdb,
        int dayIndex,
        int bodyIndex,
        double alpha,
        double3[] posAfter,
        double3[] velAfter,
        Func<int, string> bodyNameOf)
    {
        // Interpolate to exact midnight boundary
        double3 pUnits = math.lerp(ws.PosPrev[bodyIndex], posAfter[bodyIndex], alpha);
        double3 vUnitsPerDay = math.lerp(ws.VelPrev[bodyIndex], velAfter[bodyIndex], alpha);

        string name = bodyNameOf != null ? bodyNameOf(bodyIndex) : $"Body[{bodyIndex}]";

        // Optional: convert to Horizons-like units (km and km/s) for direct comparison
        double3 pKm = UnitsToKm(pUnits);
        double3 vKmPerSec = UnitsPerDayToKmPerSec(vUnitsPerDay);

        Debug.Log(
            $"[HorizonsSample] Day={dayIndex:00} JD(TDB)={jdTdb:F6} {name} " +
            $"pos_km=({pKm.x:E6}, {pKm.y:E6}, {pKm.z:E6}) " +
            $"vel_kmps=({vKmPerSec.x:E6}, {vKmPerSec.y:E6}, {vKmPerSec.z:E6})"
        );
    }

    // --- Unit conversion helpers for comparison output ---
    // Assumes:
    // - your positions are in Unity units
    // - your velocities are in Unity units/day (matches your integrator)
    private static double3 UnitsToKm(double3 units)
    {
        double meters = PhysicsConstants.UNITY_METERS_PER_UNIT;
        return (units * meters) / 1000.0;
    }

    private static double3 UnitsPerDayToKmPerSec(double3 unitsPerDay)
    {
        double meters = PhysicsConstants.UNITY_METERS_PER_UNIT;
        double secondsPerDay = PhysicsConstants.REAL_SECONDS_PER_DAY;
        return (unitsPerDay * meters) / 1000.0 / secondsPerDay;
    }
}
