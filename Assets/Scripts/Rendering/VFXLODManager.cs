using Unity.Mathematics;
using UnityEngine;

[RequireComponent(typeof(ISimulationObject))]
public class VFXLODManager : MonoBehaviour
{
    [SerializeField] float _maxVFXRenderDistance = 10000f;
    [SerializeField] GameObject[] _vfxObjects;
    [SerializeField] SimulationObject _simObject;
    [SerializeField] SimulationObject _playerObject;
    double3 _simObjectPosition;
    double3 _playerPosition;



    void Start()
    {
        if (_simObject == null)
        {
            if (!TryGetComponent(out _simObject))
            {
                Debug.LogError("VFXLOD requires an ISimulationObject component.");
                return;
            }
        }
        if (_playerObject == null)
        {
            SimulationObject playerSimObj = FindFirstObjectByType<MovementController>();
            if (playerSimObj == null)
            {
                Debug.LogError("VFXLOD could not find the player SimulationObject.");
                return;
            }
        }
    }

    void Update()
    {
        _simObjectPosition = _simObject.Position;
        _playerPosition = _playerObject.Position;

        if (math.distance(_simObjectPosition, _playerPosition) > _maxVFXRenderDistance) SetVFXActive(false);
        else SetVFXActive(true);
    }

    void SetVFXActive(bool isActive)
    {
        for (int i = 0; i < _vfxObjects.Length; i++)
        {
            if (_vfxObjects[i].activeSelf != isActive) _vfxObjects[i].SetActive(isActive);
        }
    }
}
