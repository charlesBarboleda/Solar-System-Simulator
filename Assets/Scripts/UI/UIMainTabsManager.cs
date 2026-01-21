using UnityEngine;

public class UIMainTabsManager : MonoBehaviour
{
    [SerializeField] GameObject _horizonsContent;
    [SerializeField] GameObject _3DContent;
    [SerializeField] GameObject _2DContent;
    [SerializeField] GameObject _settingsContent;


    public void OnClickHorizonsTab()
    {
        _horizonsContent.SetActive(true);

        _3DContent.SetActive(false);
        _2DContent.SetActive(false);
        _settingsContent.SetActive(false);
    }

    public void OnClick3DTab()
    {
        _3DContent.SetActive(true);

        _horizonsContent.SetActive(false);
        _2DContent.SetActive(false);
        _settingsContent.SetActive(false);
    }

    public void OnClick2DTab()
    {
        _2DContent.SetActive(true);

        _horizonsContent.SetActive(false);
        _3DContent.SetActive(false);
        _settingsContent.SetActive(false);
    }

    public void OnClickSettingsTab()
    {
        _settingsContent.SetActive(true);

        _horizonsContent.SetActive(false);
        _3DContent.SetActive(false);
        _2DContent.SetActive(false);
    }
}
