using System;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;


public class VerticesTest : MonoBehaviour
{
    [SerializeField] Mesh _ringMesh;
    [SerializeField] Transform _ringGameObject;
    Vector3[] _initVertices, _vertices, _outerRingVertices;
    float _maxSqrMagnitude;
    int[] _initTriangles, _triangles;

    double _fRingDistance => PhysicsConstants.ToUnityUnitsFromM(PhysicsConstants.REAL_SATURN_MAX_RING_DISTANCE_FROM_CENTER_M);


    void Start()
    {
        if (_ringMesh != null)
        {
            _ringGameObject.position = Vector3.zero;

            _initVertices = _ringMesh.vertices;
            _vertices = _ringMesh.vertices;

            _initTriangles = _ringMesh.triangles;
            _triangles = _ringMesh.triangles;

            _maxSqrMagnitude = 0f;

            Debug.Log($"Target SqrMag (F-RING): {_fRingDistance}");

            // for (int i = 0; i < _triangles.Length; i++)
            // {
            //     _triangles[i].
            // }
        }
    }
    void Update()
    {
        if (Keyboard.current.dKey.wasPressedThisFrame)
        {
            for (int i = 0; i < _vertices.Length; i++)
            {
                _maxSqrMagnitude = math.max(_vertices[i].sqrMagnitude, _maxSqrMagnitude);
                if (_maxSqrMagnitude - _vertices[i].sqrMagnitude <= 1e-4)
                {
                    // Debug.Log($"Found Max Vertex At: {_vertices[i]} || SqrMag: {_vertices[i].sqrMagnitude}");
                    // Debug.DrawLine(_ringGameObject.position, _vertices[i], Color.red, Mathf.Infinity);
                    float multiplier = Mathf.Sqrt((float)_fRingDistance) / _vertices[i].x;

                    _vertices[i].x *= multiplier;
                    _vertices[i].z *= multiplier;
                }
            }

            Debug.Log($"Max Measured SqrMag: {_maxSqrMagnitude}");

            _ringMesh.vertices = _vertices;
            _ringMesh.RecalculateBounds();

            _vertices = _ringMesh.vertices;
        }

        if (Keyboard.current.aKey.wasPressedThisFrame)
        {
            Debug.Log($"Reset vertices to original");

            _ringMesh.vertices = _initVertices;
            _ringMesh.RecalculateBounds();
            _vertices = _ringMesh.vertices;
        }

    }

}
