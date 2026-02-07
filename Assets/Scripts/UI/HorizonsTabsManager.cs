using UnityEngine;

public class HorizonsTabsManager : MonoBehaviour
{
    [SerializeField] GameObject _catalogDBContent;
    [SerializeField] GameObject _horizonsAPIContent;
    [SerializeField] GameObject _ephemerisDBContent;
    [SerializeField] NAIFDatabaseUIController _naifDatabaseUIController;
    [SerializeField] Canvas _mainContentCanvas;

    public void MainContentCanvasSortOrder(int sortOrder) => _mainContentCanvas.sortingOrder = sortOrder;

    public void OnClickCatalogDBTab()
    {
        _naifDatabaseUIController.OpenClosePanel();

        _horizonsAPIContent.SetActive(false);
        _ephemerisDBContent.SetActive(false);
    }

    public void OnClickHorizonsAPITab()
    {
        if (!_horizonsAPIContent.activeInHierarchy) _horizonsAPIContent.SetActive(true);
        else _horizonsAPIContent.SetActive(false);

        _naifDatabaseUIController.ClosePanel();
        _ephemerisDBContent.SetActive(false);
    }

    public void OnClickEphemerisDBTab()
    {
        if (!_ephemerisDBContent.activeInHierarchy) _ephemerisDBContent.SetActive(true);
        else _ephemerisDBContent.SetActive(false);

        _naifDatabaseUIController.ClosePanel();
        _horizonsAPIContent.SetActive(false);
    }


}
