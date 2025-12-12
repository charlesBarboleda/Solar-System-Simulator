using UnityEngine;
using Unity.Mathematics;
using System.Linq;

public class NBodyManager : MonoBehaviour
{
    public static NBodyManager Instance { get; private set; }

    public AstronomicalObject[] SystemBodies;

    private double3[] _accelerations; // UnityUnits/day^2
    private double DtSimDays => SimulationSettings.Instance.DeltaSimDays;

    [SerializeField] bool _debug = false;

    [SerializeField] bool _useNewtonian = false;



    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        if (SystemBodies == null)
        {
            Debug.LogError("[NBodyManager] Start(): Invalid or Null SystemBodies.");
        }
        if (SystemBodies.Count() <= 0)
        {
            Debug.LogError("[NBodyManager] Start(): SystemBodies must have at least one AstronomicalObject element.");
        }

        _accelerations = new double3[SystemBodies.Count()];
    }
    void FixedUpdate()
    {
        if (SystemBodies == null || SystemBodies.Count() <= 0)
        {
            Debug.LogError("[NBodyManager] FixedUpdate(): Invalid or Null SystemBodies array.");
            return;
        }

        // 1) Compute accelerations for every body inside SystemBodies
        for (int currentBody = 0; currentBody < SystemBodies.Count(); currentBody++)
        {
            AstronomicalObject self = SystemBodies[currentBody];

            if (self == null || self.MassKg <= 0.0)
            {
                _accelerations[currentBody] = double3.zero;
                continue;
            }

            _accelerations[currentBody] = _useNewtonian
                                        ? SpacePhysics3D.NBodyAccelVectorOf(self, SystemBodies)
                                        : SpacePhysics3D.Einstein_Infeld_Hoffmann(self, SystemBodies);
        }

        // 2) Integrate velocity and position with a semi-implicit Euler
        for (int currentBody = 0; currentBody < SystemBodies.Count(); currentBody++)
        {
            AstronomicalObject self = SystemBodies[currentBody];
            if (self == null || self.MassKg <= 0.0)
                continue;

            self.Velocity += _accelerations[currentBody] * DtSimDays;
            self.Position += self.Velocity * DtSimDays;

            self.ApplyPosition();
        }

        // Debugging
        if (_debug)
        {
            for (int currentBody = 0; currentBody < SystemBodies.Count(); currentBody++)
            {
                AstronomicalObject body = SystemBodies[currentBody];
                if (body == null || body.MassKg <= 0.0) return;

                SpacePhysics3D.GetBarycenterVectorsOf(SystemBodies, out double3 barycenterPosition, out double3 barycenterVelocity);
                Debug.DrawLine(body.transform.position, new Vector3((float)barycenterPosition.x, (float)barycenterPosition.y, (float)barycenterPosition.z), Color.red);
            }

        }

    }
}

