using System;
using Unity.Mathematics;


public readonly struct EphemerisSample
{
    public readonly string BodyName;
    public readonly string CenterBodyName;
    public readonly DateTimeOffset Date;
    public readonly double3 Position;
    public readonly double3 Velocity;

    public EphemerisSample(string bodyName, string centerbodyname, DateTimeOffset date, double3 position, double3 velocity)
    {
        BodyName = bodyName;
        CenterBodyName = centerbodyname;
        Date = date;
        Position = position;
        Velocity = velocity;
    }


}
