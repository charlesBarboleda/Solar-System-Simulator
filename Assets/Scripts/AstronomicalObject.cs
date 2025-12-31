using UnityEngine;
using Unity.Mathematics;

public class AstronomicalObject : SimulationObject
{
    public BodyData Data;
    public bool Initialized = false;
    [SerializeField] MeshRenderer _meshRenderer;

    void Awake()
    {
        if (_meshRenderer == null || !TryGetComponent(out _meshRenderer))
        {
            Debug.LogError($"No MeshRenderer component found on {name}. Cannot Initialize.");
            return;
        }
        Initialize();
    }

    void Initialize()
    {
        if (Data.Mass <= 0)
        {
            Debug.LogError($"[AstronomicalObject.cs] Initialize(): Data.Mass cannot be equal to or less than 0. Cannot Initialize.");
            return;
        }

        if (!Initialized)
        {
            // Init pos/vel
            Position = Data.StartPosition;
            Velocity = Data.StartVelocity;

            // Init size
            if (Data.Type == BodyType.Star || Data.Type == BodyType.Planet || Data.Type == BodyType.Moon)
            {
                float UnityDiameter = (float)PhysicsConstants.ToUnityUnitsFromM(Data.Diameter);
                if (UnityDiameter > 0) transform.localScale = Vector3.one * UnityDiameter;
                else Debug.LogWarning($"Diameter is too small for {Data.Name}");

                float baseDiameterLocal = _meshRenderer.localBounds.size.x;
                float UniformScale = UnityDiameter / baseDiameterLocal;
                _meshRenderer.transform.localScale = (float)PhysicsConstants.UNITY_SIZE_SCALE_FACTOR * UniformScale * Vector3.one;
            }
        }

        // Init Material/Appearance
        if (Data.VisualAppearance != null && _meshRenderer != null) _meshRenderer.material = Data.VisualAppearance;
        else Debug.LogWarning($"No material assigned for {Data.Name}");

        // Init Particles
        switch (Data.Type)
        {
            case BodyType.Planet:
                break;
        }

        // Temp Init: Horizon's ephemeris data override (Epoch: 2025-01-01 00:00:00 Barycentric Dynamical Time (TDB)) 
        // Override Position, Velocity, and Mass based on known values at a specific epoch
        // Source: JPL Horizons System
        switch (Data.Name)
        {
            case "Sun":
                double3 sunPos = new(
                    x: PhysicsConstants.ToUnityUnitsFromKM(-8.572865039469250E+05),
                    y: PhysicsConstants.ToUnityUnitsFromKM(-7.346088835335051E+05),
                    z: PhysicsConstants.ToUnityUnitsFromKM(2.685423265526889E+04)
                    );
                double3 sunVel = new(
                    x: PhysicsConstants.ToUnityUnitsFromKMPerSec(1.239859639798033E-02),
                    y: PhysicsConstants.ToUnityUnitsFromKMPerSec(-6.348466611140617E-03),
                    z: PhysicsConstants.ToUnityUnitsFromKMPerSec(-2.037876555553517E-04)
                    );

                Position = sunPos;
                Velocity = sunVel;
                Data.Mass = 1988410e24;
                break;

            case "Earth":
                double3 earthPos = new(
                    x: PhysicsConstants.ToUnityUnitsFromKM(-2.758794880287251E+07),
                    y: PhysicsConstants.ToUnityUnitsFromKM(1.439239583084676E+08),
                    z: PhysicsConstants.ToUnityUnitsFromKM(1.921064327326417E+04)
                    );
                double3 earthVel = new(
                    x: PhysicsConstants.ToUnityUnitsFromKMPerSec(-2.977686364628585E+01),
                    y: PhysicsConstants.ToUnityUnitsFromKMPerSec(-5.535813340802556E+00),
                    z: PhysicsConstants.ToUnityUnitsFromKMPerSec(-1.943387942073826E-04)
                    );

                Position = earthPos;
                Velocity = earthVel;
                Data.Mass = 5.97219e24;
                break;

            case "EarthMoon":
                double3 moonPos = new(
                    x: PhysicsConstants.ToUnityUnitsFromKM(-2.743589644230116E+07),
                    y: PhysicsConstants.ToUnityUnitsFromKM(1.435751546419631E+08),
                    z: PhysicsConstants.ToUnityUnitsFromKM(-1.145344989768416E+04)
                    );
                double3 moonVel = new(
                    x: PhysicsConstants.ToUnityUnitsFromKMPerSec(-2.884424012475249E+01),
                    y: PhysicsConstants.ToUnityUnitsFromKMPerSec(-5.089320873036412E+00),
                    z: PhysicsConstants.ToUnityUnitsFromKMPerSec(3.814177365090332E-02)
                    );

                Position = moonPos;
                Velocity = moonVel;
                Data.Mass = 7.349e22;
                break;

            case "Venus":
                double3 venusPos = new(
                    x: PhysicsConstants.ToUnityUnitsFromKM(6.697319534635594E+07),
                    y: PhysicsConstants.ToUnityUnitsFromKM(8.337171945245868E+07),
                    z: PhysicsConstants.ToUnityUnitsFromKM(-2.731933993919346E+06)
                    );
                double3 venusVel = new(
                    x: PhysicsConstants.ToUnityUnitsFromKMPerSec(-2.735548307021769E+01),
                    y: PhysicsConstants.ToUnityUnitsFromKMPerSec(2.182743070706988E+01),
                    z: PhysicsConstants.ToUnityUnitsFromKMPerSec(1.878804135388283E+00)
                    );

                Position = venusPos;
                Velocity = venusVel;
                Data.Mass = 48.685e23;
                break;

            case "Uranus":
                double3 uranusPos = new(
                    x: PhysicsConstants.ToUnityUnitsFromKM(1.660221941282016E+09),
                    y: PhysicsConstants.ToUnityUnitsFromKM(2.406965914394302E+09),
                    z: PhysicsConstants.ToUnityUnitsFromKM(-1.256903703858864E+07)
                    );
                double3 uranusVel = new(
                    x: PhysicsConstants.ToUnityUnitsFromKMPerSec(-5.655920544550284E+00),
                    y: PhysicsConstants.ToUnityUnitsFromKMPerSec(3.549247198028906E+00),
                    z: PhysicsConstants.ToUnityUnitsFromKMPerSec(8.651776992957205E-02)
                    );

                Position = uranusPos;
                Velocity = uranusVel;
                Data.Mass = 86.813e24;
                break;

            case "Jupiter":
                double3 jupiterPos = new(
                    x: PhysicsConstants.ToUnityUnitsFromKM(1.571230833020991E+08),
                    y: PhysicsConstants.ToUnityUnitsFromKM(7.429840488421507E+08),
                    z: PhysicsConstants.ToUnityUnitsFromKM(-6.597049828231782E+06)
                    );
                double3 jupiterVel = new(
                    x: PhysicsConstants.ToUnityUnitsFromKMPerSec(-1.293244436609816E+01),
                    y: PhysicsConstants.ToUnityUnitsFromKMPerSec(3.325781476287804E+00),
                    z: PhysicsConstants.ToUnityUnitsFromKMPerSec(2.755437569190042E-01)
                    );

                Position = jupiterPos;
                Velocity = jupiterVel;
                Data.Mass = 18.9819e26;
                break;
            case "Neptune":
                double3 neptunePos = new(
                    x: PhysicsConstants.ToUnityUnitsFromKM(4.469116222588663E+09),
                    y: PhysicsConstants.ToUnityUnitsFromKM(-9.560778256566879E+07),
                    z: PhysicsConstants.ToUnityUnitsFromKM(-1.010264767638457E+08)
                    );
                double3 neptuneVel = new(
                    x: PhysicsConstants.ToUnityUnitsFromKMPerSec(8.064561683471368E-02),
                    y: PhysicsConstants.ToUnityUnitsFromKMPerSec(5.465730017544922E+00),
                    z: PhysicsConstants.ToUnityUnitsFromKMPerSec(-1.151205185674022E-01)
                    );

                Position = neptunePos;
                Velocity = neptuneVel;
                Data.Mass = 102.409e24;
                break;

            case "Mars":
                double3 marsPos = new(
                    x: PhysicsConstants.ToUnityUnitsFromKM(-7.890038131682469E+07),
                    y: PhysicsConstants.ToUnityUnitsFromKM(2.274372361241295E+08),
                    z: PhysicsConstants.ToUnityUnitsFromKM(6.722196400986686E+06)
                    );
                double3 marsVel = new(
                    x: PhysicsConstants.ToUnityUnitsFromKMPerSec(-2.199759485544177E+01),
                    y: PhysicsConstants.ToUnityUnitsFromKMPerSec(-5.787405095472254E+00),
                    z: PhysicsConstants.ToUnityUnitsFromKMPerSec(4.184257990340883E-01)
                    );

                Position = marsPos;
                Velocity = marsVel;
                Data.Mass = 6.4171e23;
                break;

            case "Saturn":
                double3 saturnPos = new(
                    x: PhysicsConstants.ToUnityUnitsFromKM(1.414498231862034E+09),
                    y: PhysicsConstants.ToUnityUnitsFromKM(-2.647172137275474E+08),
                    z: PhysicsConstants.ToUnityUnitsFromKM(-5.171551879510410E+07)
                    );
                double3 saturnVel = new(
                    x: PhysicsConstants.ToUnityUnitsFromKMPerSec(1.240660798615463E+00),
                    y: PhysicsConstants.ToUnityUnitsFromKMPerSec(9.473546595187154E+00),
                    z: PhysicsConstants.ToUnityUnitsFromKMPerSec(-2.135791731559418E-01)
                    );

                Position = saturnPos;
                Velocity = saturnVel;
                Data.Mass = 5.6834e26;
                break;

            case "Pluto":
                double3 plutoPos = new(
                    x: PhysicsConstants.ToUnityUnitsFromKM(2.726134821346495E+09),
                    y: PhysicsConstants.ToUnityUnitsFromKM(-4.489869448498899E+09),
                    z: PhysicsConstants.ToUnityUnitsFromKM(-3.081172013616135E+08)
                    );
                double3 plutoVel = new(
                    x: PhysicsConstants.ToUnityUnitsFromKMPerSec(4.789449666460403E+00),
                    y: PhysicsConstants.ToUnityUnitsFromKMPerSec(1.631136121618671E+00),
                    z: PhysicsConstants.ToUnityUnitsFromKMPerSec(-1.537878641005477E+00)
                    );

                Position = plutoPos;
                Velocity = plutoVel;
                Data.Mass = 1.307e22;
                break;

            case "Mercury":
                double3 mercuryPos = new(
                    x: PhysicsConstants.ToUnityUnitsFromKM(-5.879699189509091E+07),
                    y: PhysicsConstants.ToUnityUnitsFromKM(-2.492820404148239E+07),
                    z: PhysicsConstants.ToUnityUnitsFromKM(3.364042452841429E+06)
                    );
                double3 mercuryVel = new(
                    x: PhysicsConstants.ToUnityUnitsFromKMPerSec(8.711982611106873E+00),
                    y: PhysicsConstants.ToUnityUnitsFromKMPerSec(-4.284856986770977E+01),
                    z: PhysicsConstants.ToUnityUnitsFromKMPerSec(-4.299279282370732E+00)
                    );

                Position = mercuryPos;
                Velocity = mercuryVel;
                Data.Mass = 3.302e23;
                break;
        }

        UpdateTransform();

        Initialized = true;
    }
}



