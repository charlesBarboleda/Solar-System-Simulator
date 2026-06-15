using UnityEngine;
using System;

public class UIInputStopper : MonoBehaviour
{
    public static UIInputStopper Instance { get; private set; }

    public static event Action<bool> OnUIActiveChanged;

    public GameObject _3DHeader;
    public GameObject _2DHeader;
    public GameObject _SettingsHeader;
    public GameObject _HorizonsHeader;
    public GameObject _ApplyVectorObject;
    public GameObject _MainMenuCanvas;

    [SerializeField] GameObject _playerUICanvas;

    bool _previousUIState;

    public bool IsUIActive =>
        _3DHeader.activeInHierarchy ||
        _2DHeader.activeInHierarchy ||
        _SettingsHeader.activeInHierarchy ||
        _HorizonsHeader.activeInHierarchy ||
        _ApplyVectorObject.activeInHierarchy ||
        _MainMenuCanvas.activeInHierarchy;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        _previousUIState = IsUIActive;
        _playerUICanvas.SetActive(!_previousUIState);
    }

    void Update()
    {
        bool current = IsUIActive;

        if (current == _previousUIState) return;

        _previousUIState = current;
        _playerUICanvas.SetActive(!current);
        OnUIActiveChanged?.Invoke(current);
    }

    public void EnablePlayerUI(bool enable) => _playerUICanvas.SetActive(enable);
}