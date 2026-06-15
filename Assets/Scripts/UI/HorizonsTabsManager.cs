using UnityEngine;

public class HorizonsTabsManager : MonoBehaviour
{
    [SerializeField] GameObject _catalogDBContent;
    [SerializeField] GameObject _horizonsAPIContent;
    [SerializeField] ObjectDatabaseUIController _objectDatabaseUIController;
    [SerializeField] NAIFDatabaseUIController _naifDatabaseUIController;
    [SerializeField] Canvas _mainContentCanvas;

    public void MainContentCanvasSortOrder(int sortOrder) => _mainContentCanvas.sortingOrder = sortOrder;

    public void OnClickCatalogDBTab()
    {
        _naifDatabaseUIController.OpenClosePanel();

        _horizonsAPIContent.SetActive(false);
        _objectDatabaseUIController.ClosePanel();
        ApplyVectorManager.Instance.OnCloseButtonClick();
    }

    public void OnClickHorizonsAPITab()
    {
        if (!_horizonsAPIContent.activeInHierarchy) _horizonsAPIContent.SetActive(true);
        else _horizonsAPIContent.SetActive(false);

        _naifDatabaseUIController.ClosePanel();
        _objectDatabaseUIController.ClosePanel();
        ApplyVectorManager.Instance.OnCloseButtonClick();
    }

    public void OnClickObjectDatabaseTab()
    {
        _objectDatabaseUIController.OpenClosePanel();

        _naifDatabaseUIController.ClosePanel();
        _horizonsAPIContent.SetActive(false);
        ApplyVectorManager.Instance.OnCloseButtonClick();
    }


}
