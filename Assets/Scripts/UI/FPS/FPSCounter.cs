using UnityEngine;
using TMPro;

public class FPSCounter : MonoBehaviour
{
    public static FPSCounter Instance { get; private set; }

    [SerializeField] float updateInterval = 0.5f;

    [SerializeField] TextMeshProUGUI _textComponent;
    float _accumulatedTime = 0f;
    int _frameCount = 0;
    float _timeLeft;

    readonly string[] _fpsCachedStrings = new string[1000];

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        enabled = false;
    }

    void Start()
    {
        _timeLeft = updateInterval;

        for (int i = 0; i < _fpsCachedStrings.Length; i++)
        {
            _fpsCachedStrings[i] = $"FPS: {i}";
        }
    }

    void Update()
    {
        _timeLeft -= Time.unscaledDeltaTime;
        _accumulatedTime += Time.unscaledDeltaTime;
        _frameCount++;

        if (_timeLeft <= 0.0f)
        {
            int fps = Mathf.RoundToInt(_frameCount / _accumulatedTime);
            fps = Mathf.Clamp(fps, 0, _fpsCachedStrings.Length - 1);

            _textComponent.SetText(_fpsCachedStrings[fps]);

            _timeLeft = updateInterval;
            _accumulatedTime = 0.0f;
            _frameCount = 0;
        }
    }
}