using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ChangeBackground : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Image Reference")]
    [SerializeField] Image _imageComponent;

    [Header("Image Settings")]
    [SerializeField] Sprite _hoverEnterBackgroundImage;
    [SerializeField] Sprite _hoverExitBackgroundImage;
    [SerializeField] Color _hoverEnterColor;
    [SerializeField] Color _hoverExitColor;
    [SerializeField] bool _applyColor;

    void Awake()
    {
        if (_imageComponent == null)
        {
            Debug.LogError("No Image Component assigned.");
            enabled = false;
            return;
        }
    }

    public void OnPointerEnter(PointerEventData eventData) => OnHoverEnter();
    public void OnPointerExit(PointerEventData eventData) => OnHoverExit();

    public void OnHoverEnter()
    {
        if (_hoverEnterBackgroundImage != null)
        {
            if (_applyColor)
            {
                _imageComponent.sprite = _hoverEnterBackgroundImage;
                _imageComponent.color = _hoverEnterColor;
            }
            else _imageComponent.sprite = _hoverEnterBackgroundImage;
        }
        else if (_applyColor) _imageComponent.color = _hoverEnterColor;

    }

    public void OnHoverExit()
    {
        if (_hoverExitBackgroundImage != null)
        {
            if (_applyColor)
            {
                _imageComponent.sprite = _hoverExitBackgroundImage;
                _imageComponent.color = _hoverExitColor;
            }
            else _imageComponent.sprite = _hoverExitBackgroundImage;
        }
        else if (_applyColor) _imageComponent.color = _hoverExitColor;
    }

    [ContextMenu("Get Image component reference")]
    public void GetImageComponent()
    {
        if (_imageComponent == null)
        {
            if (!TryGetComponent(out _imageComponent))
            {
                Debug.LogWarning($"Could not find Image component on {name}");
            }
        }
    }
}
