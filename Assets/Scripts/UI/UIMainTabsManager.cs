using TMPro;
using UnityEngine;

public class UIMainTabsManager : MonoBehaviour
{
    [SerializeField] GameObject _horizonsContent;
    [SerializeField] GameObject _3DContent;
    [SerializeField] GameObject _2DContent;
    [SerializeField] GameObject _settingsContent;
    [SerializeField] NAIFDatabaseUIController _naifDatabaseUIController;


    public void OnClickHorizonsTab()
    {
        if (!_horizonsContent.activeInHierarchy) _horizonsContent.SetActive(true);
        else _horizonsContent.SetActive(false);

        _3DContent.SetActive(false);
        _2DContent.SetActive(false);
        _settingsContent.SetActive(false);
        _naifDatabaseUIController.ClosePanel();
    }

    public void OnClick3DTab()
    {
        if (!_3DContent.activeInHierarchy) _3DContent.SetActive(true);
        else _3DContent.SetActive(false);

        _horizonsContent.SetActive(false);
        _2DContent.SetActive(false);
        _settingsContent.SetActive(false);
        _naifDatabaseUIController.ClosePanel();
    }

    public void OnClick2DTab()
    {
        if (!_2DContent.activeInHierarchy) _2DContent.SetActive(true);
        else _2DContent.SetActive(false);

        _horizonsContent.SetActive(false);
        _3DContent.SetActive(false);
        _settingsContent.SetActive(false);
        _naifDatabaseUIController.ClosePanel();
    }

    public void OnClickSettingsTab()
    {
        if (!_settingsContent.activeInHierarchy) _settingsContent.SetActive(true);
        else _settingsContent.SetActive(false);

        _horizonsContent.SetActive(false);
        _3DContent.SetActive(false);
        _2DContent.SetActive(false);
        _naifDatabaseUIController.ClosePanel();
    }
}
