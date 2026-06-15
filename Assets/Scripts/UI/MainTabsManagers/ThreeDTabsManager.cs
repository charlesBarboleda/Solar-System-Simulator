using UnityEngine;

public class ThreeDTabsManager : MonoBehaviour
{
    [SerializeField] GameObject _simulationAssetDatabaseContent;
    [SerializeField] GameObject _simulationSettingsContent;
    [SerializeField] GameObject _simulationStateDatabaseContent;
    [SerializeField] GameObject _liveSimulationContent;

    public void OnClickLiveSimulationTab()
    {
        if (!_liveSimulationContent.activeInHierarchy)
        {
            _liveSimulationContent.SetActive(true);
        }
        else _liveSimulationContent.SetActive(false);

        _simulationSettingsContent.SetActive(false);
        _simulationAssetDatabaseContent.SetActive(false);
        _simulationStateDatabaseContent.SetActive(false);
        ApplyVectorManager.Instance.OnCloseButtonClick();
    }

    public void OnClickStateDatabaseTab()
    {
        if (!_simulationStateDatabaseContent.activeInHierarchy)
        {
            _simulationStateDatabaseContent.SetActive(true);
        }
        else _simulationStateDatabaseContent.SetActive(false);

        _liveSimulationContent.SetActive(false);
        _simulationSettingsContent.SetActive(false);
        _simulationAssetDatabaseContent.SetActive(false);
        ApplyVectorManager.Instance.OnCloseButtonClick();
    }

    public void OnClickSettingsTab()
    {
        if (!_simulationSettingsContent.activeInHierarchy)
        {
            _simulationSettingsContent.SetActive(true);
            SimulationSettingsUIManager.Instance.SetPlaceholders();
        }
        else _simulationSettingsContent.SetActive(false);

        _liveSimulationContent.SetActive(false);
        _simulationAssetDatabaseContent.SetActive(false);
        _simulationStateDatabaseContent.SetActive(false);
        ApplyVectorManager.Instance.OnCloseButtonClick();
    }

    public void OnClickAssetsTab()
    {
        if (!_simulationAssetDatabaseContent.activeInHierarchy)
        {
            _simulationAssetDatabaseContent.SetActive(true);
            if (!SimulationAssetDatabaseManager.Instance.IsLoaded) SimulationAssetDatabaseManager.Instance.LoadDatabase();
        }
        else _simulationAssetDatabaseContent.SetActive(false);

        _liveSimulationContent.SetActive(false);
        _simulationSettingsContent.SetActive(false);
        _simulationStateDatabaseContent.SetActive(false);
        ApplyVectorManager.Instance.OnCloseButtonClick();
    }
}