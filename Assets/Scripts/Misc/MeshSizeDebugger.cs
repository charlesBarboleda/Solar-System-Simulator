using UnityEngine;

public class MeshSizeDebugger : MonoBehaviour
{
    void Start()
    {
        Renderer renderer = GetComponentInChildren<Renderer>();

        if (renderer == null)
        {
            Debug.LogError("No renderer found.");
            return;
        }

        Bounds bounds = renderer.bounds;

        Debug.Log($"[{gameObject.name}]");
        Debug.Log($"World Size: {bounds.size}");
        Debug.Log($"Diameter X: {bounds.size.x}");
        Debug.Log($"Diameter Y: {bounds.size.y}");
        Debug.Log($"Diameter Z: {bounds.size.z}");
    }
}