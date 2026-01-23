using UnityEngine;

public class HorizonsTabsManager : MonoBehaviour
{
    [SerializeField] GameObject _catalogDBContent;
    [SerializeField] GameObject _horizonsAPIContent;
    [SerializeField] GameObject _ephemerisDBContent;

    public void OnClickCatalogDBTab()
    {
        if (!_catalogDBContent.activeInHierarchy) _catalogDBContent.SetActive(true);
        else _catalogDBContent.SetActive(false);

        _horizonsAPIContent.SetActive(false);
        _ephemerisDBContent.SetActive(false);
    }
    public void OnClickHorizonsAPITab()
    {
        if (!_horizonsAPIContent.activeInHierarchy) _horizonsAPIContent.SetActive(true);
        else _horizonsAPIContent.SetActive(false);

        _catalogDBContent.SetActive(false);
        _ephemerisDBContent.SetActive(false);
    }
    public void OnClickEphemerisDBTab()
    {
        if (!_ephemerisDBContent.activeInHierarchy) _ephemerisDBContent.SetActive(true);
        else _ephemerisDBContent.SetActive(false);

        _catalogDBContent.SetActive(false);
        _horizonsAPIContent.SetActive(false);
    }


}
