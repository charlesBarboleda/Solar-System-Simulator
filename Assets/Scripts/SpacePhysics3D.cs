using UnityEngine;

public static class SpacePhysics3D
{
    // Uses Newton's law of universal gravitation to calculate gravitational force between two (ONLY two) astronomical objects
    public static double TwoBodyGForce(AstronomicalObject astroObject, AstronomicalObject neighbour)
    {
        if (neighbour == null)
        {
            Debug.LogError("TwoBodyGForce requires a valid neighbour astronomical object.");
            return 0.0;
        }

        // double distance = Vector3.Distance(astroObject.transform.position, otherAstroObject.transform.position); // in meters (or units)
        var distance = Vector3.Distance(astroObject.transform.position, neighbour.transform.position);
        if (distance <= 0.0)
        {
            Debug.LogError("Distance between astronomical objects must be greater than zero.");
            return 0.0;
        }

        double mass1 = astroObject.TotalMassKg;
        double mass2 = neighbour.TotalMassKg;

        // If `distance` is in kilometers, convert: distance *= 1000.0;
        double force = PhysicsConstants.GRAVITY * mass1 * mass2 / (distance * distance); // Newton's law of universal gravitation
        return force;
    }
}