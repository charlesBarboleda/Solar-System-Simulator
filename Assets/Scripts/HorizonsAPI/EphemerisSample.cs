using System;
using Unity.Mathematics;


public readonly struct EphemerisSample
{
    public readonly DateTimeOffset Date;
    public readonly double3 Position;
    public readonly double3 Velocity;

    public EphemerisSample(DateTimeOffset date, double3 position, double3 velocity)
    {
        Date = date;
        Position = position;
        Velocity = velocity;
    }


}
