using UnityEngine;
using UnityEngine.EventSystems;

public class EnlargeButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] RectTransform _elementToResize;
    [SerializeField] float _scaleXBy = 1f;
    [SerializeField] float _scaleYBy = 1f;

    Vector2 _initScale;

    void OnEnable() => _initScale = _elementToResize.localScale;
    void OnDisable() => _elementToResize.localScale = _initScale;


    public void OnPointerEnter(PointerEventData eventData) => EnlargeElement();
    public void OnPointerExit(PointerEventData eventData) => ReturnOriginalSize();

    public void EnlargeElement()
    {
        _initScale = _elementToResize.localScale;
        _elementToResize.localScale = new Vector3(_initScale.x * _scaleXBy, _initScale.y * _scaleYBy, 1);
    }

    public void ReturnOriginalSize()
    {
        _elementToResize.localScale = _initScale;
    }

}
