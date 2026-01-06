using System.Security.Cryptography.X509Certificates;
using TMPro;
using Unity.Mathematics;
using Unity.VisualScripting;

public readonly struct EphemerisSample
{
    public readonly string Date;
    public readonly double3 Position;
    public readonly double3 Velocity;

    public EphemerisSample(string date, double3 position, double3 velocity)
    {
        Date = date;
        Position = position;
        Velocity = velocity;
    }


}
