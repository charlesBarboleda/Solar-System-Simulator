using TMPro;
using UnityEngine;

public class UIMainTabsManager : MonoBehaviour
{
    [Header("Horizons")]
    [SerializeField] GameObject _horizonsContent;
    [SerializeField] GameObject _horizonsTabs;
    [SerializeField] TextMeshProUGUI _horizonsTabText;

    [Header("3D")]
    [SerializeField] GameObject _3DContent;
    [SerializeField] GameObject _3DTabs;
    [SerializeField] TextMeshProUGUI _3DTabText;

    [Header("2D")]
    [SerializeField] GameObject _2DContent;
    [SerializeField] GameObject _2DTabs;
    [SerializeField] TextMeshProUGUI _2DTabText;

    [Header("Settings")]
    [SerializeField] GameObject _settingsContent;
    [SerializeField] GameObject _settingsTabs;
    [SerializeField] TextMeshProUGUI _settingsTabText;

    [Header("Database Controllers")]
    [SerializeField] NAIFDatabaseUIController _naifDatabaseUIController;
    [SerializeField] ObjectDatabaseUIController _objectDatabaseUIController;


    public void OnClickHorizonsTab()
    {
        if (!_horizonsContent.activeInHierarchy)
        {
            _horizonsContent.SetActive(true);
            _horizonsTabText.fontStyle = FontStyles.Underline;
        }
        else
        {
            _horizonsContent.SetActive(false);
            _horizonsTabText.fontStyle = FontStyles.Normal;
        }

        if (!_horizonsTabs.activeInHierarchy)
        {
            _horizonsTabs.SetActive(true);
            _horizonsTabText.fontStyle = FontStyles.Underline;
        }
        else
        {
            _horizonsTabs.SetActive(false);
            _horizonsTabText.fontStyle = FontStyles.Normal;
        }

        ApplyVectorManager.Instance.OnCloseButtonClick();
        _3DContent.SetActive(false);
        _3DTabs.SetActive(false);
        _2DContent.SetActive(false);
        _2DTabs.SetActive(false);
        _settingsContent.SetActive(false);
        _settingsTabs.SetActive(false);
        _objectDatabaseUIController.ClosePanel();
        _naifDatabaseUIController.ClosePanel();
    }

    public void OnClick3DTab()
    {
        if (!_3DContent.activeInHierarchy)
        {
            _3DContent.SetActive(true);
            _3DTabText.fontStyle = FontStyles.Underline;
        }
        else
        {
            _3DContent.SetActive(false);
            _3DTabText.fontStyle = FontStyles.Normal;
        }

        if (!_3DTabs.activeInHierarchy)
        {
            _3DTabs.SetActive(true);
            _3DTabText.fontStyle = FontStyles.Underline;
        }
        else
        {
            _3DTabs.SetActive(false);
            _3DTabText.fontStyle = FontStyles.Normal;
        }

        ApplyVectorManager.Instance.OnCloseButtonClick();
        _horizonsContent.SetActive(false);
        _horizonsTabs.SetActive(false);
        _2DContent.SetActive(false);
        _2DTabs.SetActive(false);
        _settingsContent.SetActive(false);
        _settingsTabs.SetActive(false);
        _objectDatabaseUIController.ClosePanel();
        _naifDatabaseUIController.ClosePanel();
    }

    public void OnClick2DTab()
    {
        if (!_2DContent.activeInHierarchy)
        {
            _2DContent.SetActive(true);
            _2DTabText.fontStyle = FontStyles.Underline;
        }
        else
        {
            _2DContent.SetActive(false);
            _2DTabText.fontStyle = FontStyles.Normal;
        }

        if (!_2DTabs.activeInHierarchy)
        {
            _2DTabs.SetActive(true);
            _2DTabText.fontStyle = FontStyles.Underline;
        }
        else
        {
            _2DTabs.SetActive(false);
            _2DTabText.fontStyle = FontStyles.Normal;
        }

        ApplyVectorManager.Instance.OnCloseButtonClick();
        _horizonsContent.SetActive(false);
        _horizonsTabs.SetActive(false);
        _3DContent.SetActive(false);
        _3DTabs.SetActive(false);
        _settingsContent.SetActive(false);
        _settingsTabs.SetActive(false);
        _objectDatabaseUIController.ClosePanel();
        _naifDatabaseUIController.ClosePanel();
    }

    public void OnClickSettingsTab()
    {
        if (!_settingsContent.activeInHierarchy)
        {
            _settingsContent.SetActive(true);
            _settingsTabText.fontStyle = FontStyles.Underline;
        }
        else
        {
            _settingsContent.SetActive(false);
            _settingsTabText.fontStyle = FontStyles.Normal;
        }

        if (!_settingsTabs.activeInHierarchy)
        {
            _settingsTabs.SetActive(true);
            _settingsTabText.fontStyle = FontStyles.Underline;
        }
        else
        {
            _settingsTabs.SetActive(false);
            _settingsTabText.fontStyle = FontStyles.Normal;
        }

        ApplyVectorManager.Instance.OnCloseButtonClick();
        _horizonsContent.SetActive(false);
        _horizonsTabs.SetActive(false);
        _3DContent.SetActive(false);
        _3DTabs.SetActive(false);
        _2DContent.SetActive(false);
        _2DTabs.SetActive(false);
        _objectDatabaseUIController.ClosePanel();
        _naifDatabaseUIController.ClosePanel();
    }
}
