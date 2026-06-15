using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem.LowLevel;

public class MainMenuDisplayManager : MonoBehaviour
{
    public static MainMenuDisplayManager Instance { get; private set; }
    [SerializeField] GameObject _lightPrefab;
    [SerializeField] List<GameObject> _soloPrefabs = new();
    [SerializeField] GameObject _starPrefab;

    [SerializeField] Vector3 _lightDirection = new(250, -25, 0);
    [SerializeField] Vector3 _lightDirection2 = new(180, -80, 0);
    [SerializeField] Vector3 _lightDirection3 = new(120, 0, 0);
    [SerializeField] Vector3 _lightDirection4 = new(60, -125, 0);

    [SerializeField] Vector3 _duoLightDirection = new(-25, 25, 0);



    [SerializeField] Vector3 _soloObjectPosition = new(0.3f, 0, 1.2f);
    [SerializeField] Vector3 _soloStarObjectPosition = new(0.2f, 0, 1.05f);
    [SerializeField] Vector3 _soloRotationSpeed = new(0f, 1f, 0f);
    [SerializeField] Vector3 _soloStarRotationSpeed = new(0f, 0.5f, 0f);

    [SerializeField] Vector3 _duoObjectPosition = new(0.3f, 0, 1.2f);
    [SerializeField] Vector3 _duoStarObjectPosition = new(0.3f, 0, 1.2f);

    [SerializeField] Vector3 _duoRotationSpeed = new(0f, 0.25f, 0f);


    GameObject _lightObject = null;
    GameObject _soloObject = null;

    GameObject _duoStarObject = null;
    GameObject _duoObject = null;

    bool _isPositionSet = false;

    [SerializeField] DisplaySetting _displaySetting = DisplaySetting.Solo;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        _lightObject = null;
        _soloObject = null;
        _duoStarObject = null;
        _duoStarObject = null;
        _isPositionSet = false;
    }

    void Start()
    {
        ChooseRandomDisplaySetting();
    }

    void Update()
    {
        HandleRotation(_displaySetting);

        if (!_isPositionSet) SetPosition(_displaySetting);
    }

    void HandleRotation(DisplaySetting displaySetting)
    {
        switch (displaySetting)
        {
            case DisplaySetting.Solo:
                if (_soloObject != null) _soloObject.transform.Rotate(_soloRotationSpeed * Time.deltaTime, Space.Self);
                return;
            case DisplaySetting.SoloStar:
                if (_soloObject != null) _soloObject.transform.Rotate(_soloStarRotationSpeed * Time.deltaTime, Space.Self);
                return;
            case DisplaySetting.ObjectAndStar:
                if (_duoObject != null) _duoObject.transform.Rotate(_duoRotationSpeed * Time.deltaTime, Space.Self);
                return;
        }
    }

    void ChooseRandomDisplaySetting(bool force = false)
    {
        if (!force)
        {
            Array values = Enum.GetValues(typeof(DisplaySetting));
            int randomIdx = UnityEngine.Random.Range(0, 3);

            _displaySetting = (DisplaySetting)values.GetValue(randomIdx);
        }

        InitializeDisplay(_displaySetting);
    }

    void SetPosition(DisplaySetting displaySetting)
    {
        if (_isPositionSet) return;

        Vector3[] lightDirections = new Vector3[] { _lightDirection, _lightDirection2, _lightDirection3, _lightDirection4 };
        int randomIdx = UnityEngine.Random.Range(0, lightDirections.Length);
        Vector3 chosenLightDirection = lightDirections[randomIdx];

        switch (displaySetting)
        {
            case DisplaySetting.Solo:
                _soloObject.transform.position = _soloObjectPosition;
                _lightObject.transform.rotation = Quaternion.Euler(chosenLightDirection);
                break;
            case DisplaySetting.SoloStar:
                _soloObject.transform.position = _soloStarObjectPosition;
                break;
            case DisplaySetting.ObjectAndStar:
                _duoObject.transform.position = _duoObjectPosition;
                _duoStarObject.transform.position = _duoStarObjectPosition;
                _lightObject.transform.rotation = Quaternion.Euler(_duoLightDirection);
                break;
        }

        _isPositionSet = true;
        return;
    }

    void InitializeDisplay(DisplaySetting displaySetting)
    {
        switch (displaySetting)
        {
            case DisplaySetting.Solo:
                InitializeSolo();
                return;
            case DisplaySetting.SoloStar:
                InitializeSoloStar();
                return;
            case DisplaySetting.ObjectAndStar:
                InitializeObjectAndStar();
                return;
        }
    }

    void InitializeSolo()
    {
        int randomIdx = UnityEngine.Random.Range(0, _soloPrefabs.Count);
        _soloObject = Instantiate(_soloPrefabs[randomIdx]);

        _lightObject = Instantiate(_lightPrefab);
    }

    void InitializeObjectAndStar()
    {
        int randomIdx = UnityEngine.Random.Range(0, _soloPrefabs.Count);
        _duoObject = Instantiate(_soloPrefabs[randomIdx]);

        _duoStarObject = Instantiate(_starPrefab);

        if (_duoStarObject.TryGetComponent(out SunRenderingManager _sunRenderingManager))
        {
            BodyData bodyData = new()
            {
                Temperature = 0f
            };

            _sunRenderingManager.InitializeForDisplay(bodyData, true);
        }

        _lightObject = Instantiate(_lightPrefab);
    }

    void InitializeSoloStar()
    {
        _soloObject = Instantiate(_starPrefab);

        if (_soloObject.TryGetComponent(out SunRenderingManager _sunRenderingManager))
        {
            BodyData bodyData = new()
            {
                Temperature = 0f
            };

            _sunRenderingManager.InitializeForDisplay(bodyData);
        }
    }

    public void DestroyDisplayObjects()
    {
        if (_soloObject != null) Destroy(_soloObject);

        if (_duoObject != null) Destroy(_duoObject);
        if (_duoStarObject != null) Destroy(_duoStarObject);

        if (_lightObject != null) Destroy(_lightObject);

    }

    enum DisplaySetting
    {
        Solo,
        SoloStar,
        ObjectAndStar
    }

}
