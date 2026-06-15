using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;


public class VerticesTest : MonoBehaviour
{
    [SerializeField] Mesh _ringMesh;
    [SerializeField] Transform _ringGameObject;
    Vector3[] _initVertices, _vertices, _outerRingVertices;
    int[] _initTriangles, _triangles;

    double _maxRingDistance => PhysicsConstants.ToUnityUnitsFromM(PhysicsConstants.REAL_SATURN_MAX_RING_DISTANCE_FROM_CENTER_M);
    double _minRingDistance => PhysicsConstants.ToUnityUnitsFromM(PhysicsConstants.REAL_SATURN_MIN_RING_DISTANCE_FROM_CENTER_M);

    [SerializeField] private Renderer saturnRenderer;
    [SerializeField] private Renderer ringRenderer;



    void Start()
    {
        if (_ringMesh != null)
        {
            _ringGameObject.position = Vector3.zero;

            _initVertices = _ringMesh.vertices;
            _vertices = _ringMesh.vertices;

            _initTriangles = _ringMesh.triangles;
            _triangles = _ringMesh.triangles;

            Debug.Log($"Target Min Distance: {_minRingDistance}");
            Debug.Log($"Target Max Distance: {_maxRingDistance}");

        }
    }
    void Update()
    {

        if (Keyboard.current.zKey.wasPressedThisFrame)
        {
            float rMinUU;
            float rMaxUU;

            var v = _ringMesh.vertices;

            // Use the mesh bounds center as the ring center in local space, then convert to world.
            Vector3 centerWorld = _ringGameObject.TransformPoint(_ringMesh.bounds.center);

            rMinUU = float.PositiveInfinity;
            rMaxUU = 0f;

            for (int i = 0; i < v.Length; i++)
            {
                Vector3 pWorld = _ringGameObject.TransformPoint(v[i]);

                float dx = pWorld.x - centerWorld.x;
                float dz = pWorld.z - centerWorld.z;

                float r = Mathf.Sqrt(dx * dx + dz * dz);

                if (r < rMinUU) rMinUU = r;
                if (r > rMaxUU) rMaxUU = r;
            }

            Debug.Log($"rMin: {rMinUU} || rMax: {rMaxUU}");
        }
    }

    public void LogRadii()
    {
        // Saturn world radius (assuming it's roughly spherical)
        float saturnWorldRadius = saturnRenderer.bounds.extents.x;

        // Ring inner/outer world radii from bounds (approx, but good first check)
        // For a flat ring, extents.x is roughly outer radius IF centered properly.
        float ringOuterApprox = ringRenderer.bounds.extents.x;

        Debug.Log($"Saturn world radius: {saturnWorldRadius}");
        Debug.Log($"Saturn target world radius: {PhysicsConstants.ToUnityUnitsFromKM(58232)}");
        Debug.Log($"Ring outer approx radius: {ringOuterApprox}");
        Debug.Log($"Outer/Planet ratio (approx): {ringOuterApprox / saturnWorldRadius}");
    }

    // void OnDrawGizmos()
    // {
    //     if (!Application.isPlaying || _vertices == null) return;
    //     Gizmos.color = Color.red;
    //     for (int i = 0; i < _vertices.Length; i++)
    //         Gizmos.DrawWireSphere(_vertices[i], 0.1f);
    // }

}
