using UnityEngine;
using Unity.Mathematics;
using System.Linq;
using UnityEngine.UIElements;
using UnityEditor.UI;
using System.Linq.Expressions;

public class NBodyManager : MonoBehaviour
{
    public static NBodyManager Instance { get; private set; }

    public AstronomicalObject[] SystemBodies;

    [Header("Authorative Object States")]
    string[] _names;
    double[] _masses;
    double3[] _accelerations, _velocities, _positions; // current (snapshot) vector properties

    [Header("Predicted Object States")]
    double3[] _positionsNext, _accelerationsNext, _velocityHalf; // next (predicted) vector properties




    double DtSimDays => SimulationSettings.Instance.DeltaSimDays;

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
        int numOfBodies = SystemBodies.Length;
        if (SystemBodies == null || numOfBodies <= 0)
        {
            Debug.LogError("[NBodyManager] Start(): Invalid or Null SystemBodies.");
        }
        else
        {
            _names = new string[numOfBodies];
            _masses = new double[numOfBodies];

            _accelerations = new double3[numOfBodies];
            _accelerationsNext = new double3[numOfBodies];

            _positions = new double3[numOfBodies];
            _positionsNext = new double3[numOfBodies];

            _velocities = new double3[numOfBodies];
            _velocityHalf = new double3[numOfBodies];
        }
    }

    void FixedUpdate()
    {
        int numOfBodies = SystemBodies?.Length ?? 0;
        if (numOfBodies <= 0)
        {
            Debug.LogError("[NBodyManager] FixedUpdate(): Invalid or Null SystemBodies array.");
            return;
        }

        // 1) Compute accelerations for all bodies using the same current simulation state (same time-frame)
        for (int currentBody = 0; currentBody < numOfBodies; currentBody++)
        {
            AstronomicalObject self = SystemBodies[currentBody];
            bool isValidBody = IsValidAstronomicalBody(self);

            _accelerations[currentBody] = !isValidBody
                                        ? double3.zero
                                        : (_useNewtonian
                                        ? SpacePhysics3D.NBodyAccelVectorOf(self, SystemBodies)
                                        : SpacePhysics3D.Einstein_Infeld_Hoffmann(self, SystemBodies));
        }

        // 2) Compute the half-step velocities for all bodies
        for (int currentBody = 0; currentBody < numOfBodies; currentBody++)
        {
            AstronomicalObject self = SystemBodies[currentBody];
            bool isValidBody = IsValidAstronomicalBody(self);

            if (!isValidBody)
            {
                _velocityHalf[currentBody] = double3.zero;
                continue;
            }

            _velocityHalf[currentBody] = self.Velocity + 0.5 * _accelerations[currentBody] * DtSimDays;
        }

        // 3) Compute the future positions for all bodies in the next update-frame using the half-step velocity
        for (int currentBody = 0; currentBody < numOfBodies; currentBody++)
        {
            AstronomicalObject self = SystemBodies[currentBody];
            bool isValidBody = IsValidAstronomicalBody(self);

            if (!isValidBody) continue;

            _positionsNext[currentBody] = self.Position
                                        + _velocityHalf[currentBody]
                                        * DtSimDays;
        }

        // 4) Apply the predicted velocity (half-step) to the object's current velocity
        for (int currentBody = 0; currentBody < numOfBodies; currentBody++)
        {
            AstronomicalObject self = SystemBodies[currentBody];
            bool isValidBody = IsValidAstronomicalBody(self);

            if (!isValidBody) continue;

            self.Velocity = _velocityHalf[currentBody];
        }

        // 5) Apply the next positions so the calculations use a consistent simulation state
        for (int currentBody = 0; currentBody < numOfBodies; currentBody++)
        {
            AstronomicalObject self = SystemBodies[currentBody];
            bool isValidBody = IsValidAstronomicalBody(self);

            if (isValidBody) continue;

            self.Position = _positionsNext[currentBody];
        }

        // 6) Compute the next accelerations for the new positions
        for (int currentBody = 0; currentBody < numOfBodies; currentBody++)
        {
            AstronomicalObject self = SystemBodies[currentBody];
            bool isValidBody = IsValidAstronomicalBody(self);

            _accelerationsNext[currentBody] = !isValidBody
                                            ? double3.zero
                                            : (_useNewtonian
                                            ? SpacePhysics3D.NBodyAccelVectorOf(self, SystemBodies)
                                            : SpacePhysics3D.Einstein_Infeld_Hoffmann(self, SystemBodies));

        }

        // 7) Finalize velocity using half-step velocity and predicted accelerations
        for (int currentBody = 0; currentBody < numOfBodies; currentBody++)
        {
            AstronomicalObject self = SystemBodies[currentBody];
            bool isValidBody = IsValidAstronomicalBody(self);

            if (!isValidBody) continue;

            self.Velocity = _velocityHalf[currentBody] + 0.5 * _accelerationsNext[currentBody] * DtSimDays;

            self.UpdateVisualPosition();
        }

        // Debugging
        if (_debug)
        {
            for (int currentBody = 0; currentBody < numOfBodies; currentBody++)
            {
                AstronomicalObject body = SystemBodies[currentBody];
                if (body == null || body.MassKg <= 0.0) continue;

                SpacePhysics3D.GetBarycenterVectorsOf(SystemBodies, out double3 barycenterPosition, out double3 barycenterVelocity);
                Debug.DrawLine(body.transform.position, new Vector3((float)barycenterPosition.x, (float)barycenterPosition.y, (float)barycenterPosition.z), Color.red);
            }

        }

    }

    bool IsValidAstronomicalBody(AstronomicalObject body)
    {
        if (body == null || body.MassKg <= 0.0)
        {
            Debug.LogError($"[NBodyManager] IsValidAstronomicalBody(): Invalid or Null AstronomicalObject.");
            return false;
        }

        return true;
    }
}

