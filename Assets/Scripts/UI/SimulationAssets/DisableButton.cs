using UnityEngine.UI;
using UnityEngine;

public class DisableButton : MonoBehaviour
{
    [SerializeField] Button _button;
    void OnEnable()
    {
        AstronomicalObjectFactory.Instance.OnIsCreatingAssetChanged += HandleAssetCreation;
        HandleAssetCreation(AstronomicalObjectFactory.Instance.IsCreatingAsset);
    }
    void OnDisable() => AstronomicalObjectFactory.Instance.OnIsCreatingAssetChanged -= HandleAssetCreation;

    void HandleAssetCreation(bool isCreating) => _button.interactable = !isCreating;

}
