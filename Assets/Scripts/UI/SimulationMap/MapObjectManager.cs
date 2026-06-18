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
    SimulationMapTrail _trail;

    public AstronomicalObject AstronomicalObject => _astronomicalObject;

    public void Initialize(AstronomicalObject astroObject)
    {
        _astronomicalObject = astroObject;
        gameObject.name = $"{astroObject.Data.Body.Name} map object";
        if (_objectNameText != null) _objectNameText.text = astroObject.Data.Body.Name;
    }

    public void InitializeTrail(Transform mapParent, System.Func<double3, Vector2> projector)
    {
        GameObject trailGO = new($"Trail_{_astronomicalObject.Data.Body.Name}");
        trailGO.transform.SetParent(mapParent, false);
        trailGO.transform.SetAsFirstSibling();

        RectTransform rt = trailGO.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        trailGO.AddComponent<CanvasRenderer>();

        _trail = trailGO.AddComponent<SimulationMapTrail>();
        _trail.ProjectWorldToMap = projector;
        _trail.ClearTrail();
    }

    public void UpdateTrail(double3 playerPosition, float mapScale)
    {
        if (_trail == null) return;

        _trail.PlayerPosition = playerPosition;
        _trail.MapScale = mapScale;
        _trail.RecordPosition(GetGlobalPosition());
        _trail.SetVerticesDirty();
    }

    public void ClearTrail()
    {
        if (_trail == null) return;
        _trail.ClearTrail();
    }

    public void DestroyTrail()
    {
        if (_trail != null) Destroy(_trail.gameObject);
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