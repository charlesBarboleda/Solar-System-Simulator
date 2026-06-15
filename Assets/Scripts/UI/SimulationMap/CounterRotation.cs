using UnityEngine;

public class CounterRotation : MonoBehaviour
{
    void Update()
    {
        // Counter-rotate to keep the object upright relative to the map
        transform.rotation = Quaternion.identity;
    }
}
