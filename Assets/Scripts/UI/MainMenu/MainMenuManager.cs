using UnityEngine;

public class MainMenuManager : MonoBehaviour
{
    public static MainMenuManager Instance { get; private set; }

    [SerializeField] GameObject _mainMenuContainer;
    [SerializeField] GameObject _mainMenuObject;
    [SerializeField] GameObject _3DContent;
    [SerializeField] GameObject _headerTabsCanvas;

    [SerializeField] GameObject _objectDatabase;

    [Header("Simulation Assets")]
    [SerializeField] GameObject _simulationAssetsContent;

    [Header("Systems")]
    [SerializeField] GameObject _systemsContent;

    public bool IsActive { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        IsActive = true;
    }

    public void OnStartGameButtonClicked()
    {
        _mainMenuObject.SetActive(false);
        _headerTabsCanvas.SetActive(true);
        _objectDatabase.SetActive(true);
        UIInputStopper.Instance.EnablePlayerUI(true);
        NBodyManager.Instance.StartSimulation();
        SimulationAssetDatabaseManager.Instance.LoadDatabase();
        MainMenuDisplayManager.Instance.DestroyDisplayObjects();
        FPSCounter.Instance.enabled = true;
        SimulationSaveLoad.LoadLatestSave();

        IsActive = false;
    }

    public void OnExitGameButtonClicked() => Application.Quit();

    public void EnableMainMenu(bool enable) => _mainMenuContainer.SetActive(enable);

    public void Enable3DContent(bool enable) => _3DContent.SetActive(enable);

    public void OnAssetsButtonClicked()
    {
        _3DContent.SetActive(true);
        _simulationAssetsContent.SetActive(true);

        if (!SimulationAssetDatabaseManager.Instance.IsLoaded) SimulationAssetDatabaseManager.Instance.LoadDatabase();

        EnableMainMenu(false);
        _systemsContent.SetActive(false);
    }

    public void OnSystemsButtonClicked()
    {
        _3DContent.SetActive(true);
        _systemsContent.SetActive(true);

        if (!SimulationStateDatabaseUIManager.Instance.IsInitialized) SimulationStateDatabaseUIManager.Instance.Initialize();

        EnableMainMenu(false);
        _simulationAssetsContent.SetActive(false);
    }

}
