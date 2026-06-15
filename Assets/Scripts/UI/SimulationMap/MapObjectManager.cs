using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Unity.Mathematics;

public class MapObjectManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] RectTransform _mapObjectRectTransform;
    [SerializeField] TextMeshProUGUI _objectNameText;
    [SerializeField] Image _objectIcon;

    AstronomicalObject _astronomicalObject;

    public AstronomicalObject AstronomicalObject => _astronomicalObject;

    public void Initialize(AstronomicalObject astroObject)
    {
        _astronomicalObject = astroObject;
        if (_objectNameText != null) _objectNameText.text = astroObject.Data.Body.Name;
    }

    public double3 GetGlobalPosition()
    {
        if (_astronomicalObject == null) return double3.zero;

        return _astronomicalObject.GetGlobalPosition();
    }

    public void SetCounterRotation() => transform.rotation = Quaternion.identity;

    public void SetMapPosition(Vector2 mapPosition)
    {
        if (_mapObjectRectTransform == null) return;

        _mapObjectRectTransform.anchoredPosition = mapPosition;
    }

    public void SetVisible(bool visible) => gameObject.SetActive(visible);
}