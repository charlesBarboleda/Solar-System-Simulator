using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;
using TMPro;
using System.Collections;
using UnityEngine.UI;

public class HorizonsRequestUIManager : MonoBehaviour
{
    [SerializeField] RequestTypeManager _requestTypeManager;
    [SerializeField] TimeFormatManager _timeFormatManager;
    [SerializeField] List<GameObject> _requestParametersManagerObjects;
    [SerializeField] TMP_InputField _responseText;
    [SerializeField] RectTransform _refreshLayoutRect;

    bool _isSimplified;

    public void OnTryRequestButtonClick()
    {
        List<IAPIParameterManager> parameterManagers = GetParameterManagers(out _, out RequestTypeSimple requestTypeSimple);
        HorizonsRequestURLBuilder urlBuilder = new(requestTypeSimple, _requestTypeManager, _timeFormatManager, parameterManagers);
        urlBuilder.BuildSimpleURL(out string URL);
        StartCoroutine(HandleCoroutine(URL));
    }

    public void OnSaveButtonClick()
    {
        if (!HorizonsResponseManager.Instance.IsSuccessful)
        {
            UIMessage.Instance.NewUIMessage(MessageType.Warning, "No successful response to save!", "SAVE FAILED");
            return;
        }

        if (!TryHandleSave()) return;

        HorizonsResponseSaver.TrySaveToFile();

    }

    bool TryHandleSave()
    {
        RequestTypeSimple requestTypeSimple = (RequestTypeSimple)_requestTypeManager.GetDropdown().value;

        switch (requestTypeSimple)
        {
            case RequestTypeSimple.Position:
            case RequestTypeSimple.Velocity:
            case RequestTypeSimple.PosAndVel:
                {
                    HorizonsResponse response = HorizonsResponseManager.Instance.Response;

                    if (!HorizonsParser.TryParseEphemeris(response, requestTypeSimple, out List<EphemerisEntry> entries))
                    {
                        UIMessage.Instance.NewUIMessage(MessageType.Error, "Failed to parse ephemeris data!", "SAVE FAILED");
                        return false;
                    }

                    HorizonsResponseSaver.SaveEphemeris(entries);
                    return true;
                }

            case RequestTypeSimple.PhysicalTraits:
                {
                    HorizonsResponse response = HorizonsResponseManager.Instance.Response;
                    List<string> formattedResponse = HorizonsResponseManager.Instance.FormattedResponse;

                    if (formattedResponse == null)
                    {
                        UIMessage.Instance.NewUIMessage(MessageType.Error, "No formatted response available!", "SAVE FAILED");
                        return false;
                    }

                    if (!HorizonsParser.TryParseTargetBodyName(response.result, out string objectName)
                        || string.IsNullOrWhiteSpace(objectName))
                    {
                        UIMessage.Instance.NewUIMessage(MessageType.Error, "Could not determine object name from response!", "SAVE FAILED");
                        return false;
                    }

                    Dictionary<string, ParsedPhysicalProperty> bodyData = HorizonsParser.ParseBodyData(formattedResponse);

                    if (bodyData == null || bodyData.Count == 0)
                    {
                        UIMessage.Instance.NewUIMessage(MessageType.Error, "Failed to parse physical traits data!", "SAVE FAILED");
                        return false;
                    }

                    HorizonsResponseSaver.SavePhysicalTraits(objectName, bodyData);
                    return true;
                }

            default:
                return false;
        }
    }

    IEnumerator HandleCoroutine(string URL)
    {
        yield return StartCoroutine(HorizonsResponseManager.Instance.GetHorizonsResponse(URL));
        if (HorizonsResponseManager.Instance.IsSuccessful)
        {
            UIMessage.Instance.NewUIMessage(MessageType.Success, "Request successful!", "REQUEST SUCCESS");
            _responseText.text = HorizonsResponseManager.Instance.FormattedResponse != null ? string.Join("\n", HorizonsResponseManager.Instance.FormattedResponse) : "No formatted response available.";

            LayoutRebuilder.ForceRebuildLayoutImmediate(_refreshLayoutRect);
        }
        else
        {
            UIMessage.Instance.NewUIMessage(MessageType.Error, $"Error: {HorizonsResponseManager.Instance.ErrorResponse}\nCode: {HorizonsResponseManager.Instance.ResponseCode}", "REQUEST FAILED");
            _responseText.text = $"Error: {HorizonsResponseManager.Instance.ErrorResponse}\nCode: {HorizonsResponseManager.Instance.ResponseCode}";
            LayoutRebuilder.ForceRebuildLayoutImmediate(_refreshLayoutRect);
        }
    }

    List<IAPIParameterManager> GetParameterManagers(out TimeFormat timeFormat, out RequestTypeSimple requestTypeSimple)
    {
        List<IAPIParameterManager> managers = new();
        requestTypeSimple = (RequestTypeSimple)_requestTypeManager.GetDropdown().value;
        timeFormat = _timeFormatManager.GetTimeFormat();

        switch (requestTypeSimple)
        {
            // Position, Velocity, and PosAndVel all use the same parameter set
            case RequestTypeSimple.Position:
            case RequestTypeSimple.Velocity:
            case RequestTypeSimple.PosAndVel:
                {
                    switch (timeFormat)
                    {
                        case TimeFormat.Range:
                            AddManagersOfType<MainBodyNAIFManager, StartTimeManager, StopTimeManager, StepSizeManager>(managers);
                            break;

                        case TimeFormat.Specific:
                            AddManagersOfType<MainBodyNAIFManager, TListManager>(managers);
                            break;
                    }
                    break;
                }

            case RequestTypeSimple.PhysicalTraits:
                AddManagersOfType<MainBodyNAIFManager>(managers);
                break;
        }

        return managers;
    }

    void AddManagersOfType<T1>(List<IAPIParameterManager> target) where T1 : IAPIParameterManager
    {
        foreach (var go in _requestParametersManagerObjects)
            if (go.TryGetComponent(out T1 m)) target.Add(m);
    }

    // Overloads for additional type combinations 
    void AddManagersOfType<T1, T2>(List<IAPIParameterManager> target)
        where T1 : IAPIParameterManager where T2 : IAPIParameterManager
    {
        foreach (var go in _requestParametersManagerObjects)
        {
            if (go.TryGetComponent(out T1 m1)) target.Add(m1);
            else if (go.TryGetComponent(out T2 m2)) target.Add(m2);
        }
    }

    void AddManagersOfType<T1, T2, T3, T4>(List<IAPIParameterManager> target)
        where T1 : IAPIParameterManager where T2 : IAPIParameterManager
        where T3 : IAPIParameterManager where T4 : IAPIParameterManager
    {
        foreach (var go in _requestParametersManagerObjects)
        {
            if (go.TryGetComponent(out T1 m1)) target.Add(m1);
            else if (go.TryGetComponent(out T2 m2)) target.Add(m2);
            else if (go.TryGetComponent(out T3 m3)) target.Add(m3);
            else if (go.TryGetComponent(out T4 m4)) target.Add(m4);
        }
    }

    [Button]
    public void TestPositionURL()
    {
        List<IAPIParameterManager> parameterManagers = new();
        foreach (var parameterManagerObject in _requestParametersManagerObjects)
        {
            parameterManagerObject.TryGetComponent(out IAPIParameterManager parameterManager);
            if (parameterManager != null)
            {
                parameterManagers.Add(parameterManager);
            }
        }

        HorizonsRequestURLBuilder urlBuilder = new((RequestTypeSimple)_requestTypeManager.GetDropdown().value, _requestTypeManager, _timeFormatManager, parameterManagers);
        urlBuilder.BuildSimpleURL(out string URL);
        Debug.Log($"Built URL: {URL}");
    }


    public void OnSimplifyButtonClick()
    {
        if (!_isSimplified)
        {
            _requestTypeManager.SimplifiedMode();
            SimpleMode();
            _isSimplified = true;
        }
        else if (_isSimplified)
        {
            _requestTypeManager.FullMode();
            FullMode();
            _isSimplified = false;
        }
    }

    void SimpleMode()
    {
        RequestTypeSimple requestType = (RequestTypeSimple)_requestTypeManager.GetDropdown().value;
        switch (requestType)
        {
            case RequestTypeSimple.PosAndVel:
                {
                    for (int i = 0; i < _requestParametersManagerObjects.Count; i++)
                    {
                        GameObject parameterObject = _requestParametersManagerObjects[i];
                        parameterObject.TryGetComponent(out IAPIParameterManager parameterManager);
                        if (parameterManager is MainBodyNAIFManager)
                        {
                            parameterManager.GetParameterContainer().SetActive(true);
                        }
                        if (parameterManager is CenterBodyManager)
                        {
                            parameterManager.GetParameterContainer().SetActive(true);
                        }
                    }
                    _timeFormatManager.ChangeParameters();
                }
                return;
            case RequestTypeSimple.PhysicalTraits:
                {

                }
                return;
        }
    }

    void FullMode()
    {
        // Future implementation
    }
}
