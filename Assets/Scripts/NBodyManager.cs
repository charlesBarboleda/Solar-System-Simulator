using UnityEngine;
using Unity.Mathematics;
using System.Linq;
using UnityEditor.Rendering;

public class NBodyManager : MonoBehaviour
{
    public static NBodyManager Instance { get; private set; }

    public AstronomicalObject[] SystemBodies;

    private double3[] _accelerations; // sim units (AU / day^2)
    private double _effectiveTime;

    [SerializeField] bool _debug = false;

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

        _effectiveTime = Time.deltaTime * SimulationSettings.Instance.TimeScale;

        // 1) Compute accelerations for every body inside SystemBodies
        for (int currentBody = 0; currentBody < SystemBodies.Count(); currentBody++)
        {
            AstronomicalObject self = SystemBodies[currentBody];

            if (self == null || self.MassKg <= 0.0)
            {
                _accelerations[currentBody] = double3.zero;
                continue;
            }

            _accelerations[currentBody] = SpacePhysics3D.Einstein_Infeld_Hoffmann(self, SystemBodies);
        }

        // 2) Integrate velocity and position with a semi-implicit Euler
        for (int currentBody = 0; currentBody < SystemBodies.Count(); currentBody++)
        {
            AstronomicalObject body = SystemBodies[currentBody];
            if (body == null || body.MassKg <= 0.0)
                continue;

            body.Velocity += _accelerations[currentBody] * _effectiveTime;
            body.Position += body.Velocity * _effectiveTime;

            body.ApplyPosition();
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

