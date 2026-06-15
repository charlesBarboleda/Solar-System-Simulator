using UnityEngine;
using NaughtyAttributes;

public class RingPlanet : MonoBehaviour
{
    [SerializeField] GameObject _ringObject;
    [SerializeField] MeshFilter _meshCylinder;

    [SerializeField] float _innerGapKM;
    [SerializeField] float _ringWidthKM;
    [SerializeField] float _planetDiameterM;

    public void SetProperties(float innerGapKM, float ringWidthKM, float planetDiameterM)
    {
        _innerGapKM = innerGapKM;
        _ringWidthKM = ringWidthKM;
        _planetDiameterM = planetDiameterM;
    }

    [Button("Set Ring Planet Test")]
    void InitializeRingTest()
    {
        SetProperties(innerGapKM: _innerGapKM, ringWidthKM: _ringWidthKM, planetDiameterM: _planetDiameterM);
        Initialize();
    }

    void Update()
    {
        _ringObject.transform.rotation = Quaternion.identity;
    }

    public void Initialize()
    {
        if (_ringWidthKM <= 0f || _innerGapKM < 0f || _planetDiameterM <= 0f)
        {
            Debug.LogError("Ring width must be greater than 0, inner gap must be non-negative, and planet diameter must be greater than 0.");
            return;
        }

        float planetRadius = (float)PhysicsConstants.ToUnityUnitsFromM(_planetDiameterM / 2f);
        float innerRadius = planetRadius + (float)PhysicsConstants.ToUnityUnitsFromKM(_innerGapKM);
        float outerRadius = innerRadius + (float)PhysicsConstants.ToUnityUnitsFromKM(_ringWidthKM);

        GameObject parent = gameObject;
        _ringObject.transform.SetParent(null, true);

        InitializeRing(innerRadius, outerRadius);

        _ringObject.transform.SetParent(parent.transform, true);
    }

    void InitializeRing(float innerRadius, float outerRadius, int segments = 128)
    {
        Mesh mesh = new();

        Vector3[] vertices = new Vector3[(segments + 1) * 2];
        Vector2[] uvs = new Vector2[(segments + 1) * 2];
        int[] triangles = new int[segments * 6];

        for (int i = 0; i <= segments; i++)
        {
            float angle = (float)i / segments * Mathf.PI * 2;
            float cos = Mathf.Cos(angle);
            float sin = Mathf.Sin(angle);

            vertices[i * 2] = new Vector3(cos * innerRadius, 0, sin * innerRadius);
            vertices[i * 2 + 1] = new Vector3(cos * outerRadius, 0, sin * outerRadius);

            float v = (float)i / segments;

            uvs[i * 2] = new Vector2(0f, v);
            uvs[i * 2 + 1] = new Vector2(1f, v);
        }

        for (int i = 0; i < segments; i++)
        {
            triangles[i * 6] = i * 2;
            triangles[i * 6 + 1] = (i + 1) * 2;
            triangles[i * 6 + 2] = i * 2 + 1;

            triangles[i * 6 + 3] = i * 2 + 1;
            triangles[i * 6 + 4] = (i + 1) * 2;
            triangles[i * 6 + 5] = (i + 1) * 2 + 1;
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        mesh.RecalculateTangents();

        _meshCylinder.mesh = mesh;
    }

    public void UnparentRing() => _ringObject.transform.SetParent(null, true);
    public void ParentRing() => _ringObject.transform.SetParent(transform, true);

}
