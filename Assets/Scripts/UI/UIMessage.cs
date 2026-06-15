using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public enum MessageType
{
    Error,
    Success,
    Warning,
    Info
}

// make sure this class runs before any other UI scripts that might call it
[DefaultExecutionOrder(-100)]
public class UIMessage : MonoBehaviour
{
    // Singleton
    public static UIMessage Instance;

    // Message Panel UI element references
    [SerializeField] GameObject _messagePanel;
    [SerializeField] Button _messagePanelCloseButton;
    [SerializeField] TextMeshProUGUI _messageText;
    [SerializeField] TextMeshProUGUI _messageTitleText;
    [SerializeField] Image _messageIcon;
    [SerializeField] Sprite _errorIcon;
    readonly float _errorIconScale = 1f;
    [SerializeField] Sprite _checkmarkIcon;
    readonly float _checkmarkIconScale = 1.1f;
    [SerializeField] Sprite _warningIcon;
    readonly float _warningIconScale = 1.3f;
    [SerializeField] Sprite _infoIcon;
    readonly float _infoIconScale = 1.3f;

    // Confirmation Panel UI element references
    [SerializeField] GameObject _confirmationPanel;
    [SerializeField] TextMeshProUGUI _confirmationTitleText;
    [SerializeField] TextMeshProUGUI _confirmMessageText;
    Action _onYesAction;
    Action _onNoAction;
    bool _awaitingConfirmation;

    // Fading Message UI element references
    [SerializeField] GameObject _fadingMessagesContainer;
    [SerializeField] GameObject _firstFadingMessagePanel;
    [SerializeField] GameObject _fadingMessagePrefab;

    [SerializeField] CanvasGroup _fadingMessageCanvasGroup;
    [SerializeField] TextMeshProUGUI _fadingMessageText;
    [SerializeField] Button _fadingMessageCloseButton;
    [SerializeField] Image _fadingMessageIcon;

    // Limits
    const int MaxMessageLength = 300;
    const int MaxMessageTitleLength = 25;
    const int MaxFadeMessageLength = 87;

    void Awake()
    {
        if (_messagePanel == null || _messageText == null || _messageTitleText == null)
        {
            Debug.LogError("Could not initialize message controller; missing references.");
            return;
        }

        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;

        _fadingMessagesContainer.SetActive(true);
        _firstFadingMessagePanel.SetActive(false);
        _messagePanel.SetActive(false);
        _confirmationPanel.SetActive(false);
    }

    bool IsValidFadeMessage(string message)
    {
        if (string.IsNullOrEmpty(message))
        {
            Debug.LogError("Fade message is null or empty.");
            return false;
        }

        if (message.Length > MaxFadeMessageLength)
        {
            Debug.LogError("Fade message too long: " + message);
            return false;
        }
        return true;
    }


    public void SelectedYesConfirmation()
    {
        var cb = _onYesAction;
        CloseConfirmationPanel();
        cb?.Invoke();
    }

    public void SelectedNoConfirmation()
    {
        var cb = _onNoAction;
        CloseConfirmationPanel();
        cb?.Invoke();
    }

    void CloseConfirmationPanel()
    {
        _awaitingConfirmation = false;
        _onYesAction = null;
        _onNoAction = null;
        _confirmationPanel.SetActive(false);
    }

    public void NewUIConfirmation(string message, string title = "Confirm Action", Action onYes = null, Action onNo = null)
    {
        if (!IsValidMessage(message, title)) return;

        if (_awaitingConfirmation) return;
        _awaitingConfirmation = true;

        _onYesAction = onYes;
        _onNoAction = onNo;
        _confirmationTitleText.text = title;
        _confirmMessageText.text = message;

        _confirmationPanel.SetActive(true);
        _confirmationPanel.transform.SetAsLastSibling();
    }


    void HandleFadingMessageTypeIcon(MessageType messageType, ref Image imageComponent)
    {
        if (imageComponent == null) return;

        imageComponent.rectTransform.anchorMin = new Vector2(0, 0.5f);
        imageComponent.rectTransform.anchorMax = new Vector2(0, 0.5f);
        imageComponent.rectTransform.pivot = new Vector2(0, 0.5f);

        switch (messageType)
        {
            case MessageType.Error:
                imageComponent.sprite = _errorIcon;
                imageComponent.rectTransform.localScale = new Vector3(1.75f, 1.75f, 1.75f);
                imageComponent.rectTransform.anchoredPosition = new Vector3(15, 0, 0);
                return;
            case MessageType.Success:
                imageComponent.sprite = _checkmarkIcon;
                imageComponent.rectTransform.localScale = new Vector3(1.85f, 1.85f, 1.85f);
                imageComponent.rectTransform.anchoredPosition = new Vector3(15, 0, 0);
                return;
            case MessageType.Info:
                imageComponent.sprite = _infoIcon;
                imageComponent.rectTransform.localScale = new Vector3(2.3f, 2.3f, 2.3f);
                imageComponent.rectTransform.anchoredPosition = new Vector3(10, 0, 0);
                return;
            case MessageType.Warning:
                imageComponent.sprite = _warningIcon;
                imageComponent.rectTransform.localScale = new Vector3(2.3f, 2.3f, 2.3f);
                imageComponent.rectTransform.anchoredPosition = new Vector3(10, 0, 0);
                return;
        }
    }

    GameObject CreateFadingMessageObject(GameObject fadingMessagePrefab, GameObject parentObject, MessageType messageType, string message, float durationBeforeFade = 5f)
    {
        GameObject newFadingMessage = Instantiate(fadingMessagePrefab, parentObject.transform);

        // Content references
        TextMeshProUGUI newFadingMessageText = newFadingMessage.GetComponentInChildren<TextMeshProUGUI>();
        Button closeButton = newFadingMessage.GetComponentInChildren<Button>();
        newFadingMessage.transform.GetChild(2).TryGetComponent(out Image imageIcon);
        CanvasGroup canvasGroup = newFadingMessage.GetComponent<CanvasGroup>();

        // Apply references settings
        closeButton.onClick.AddListener(() => Destroy(newFadingMessage));
        newFadingMessageText.text = message;
        newFadingMessage.SetActive(true);
        HandleFadingMessageTypeIcon(messageType, ref imageIcon);
        newFadingMessage.transform.SetAsLastSibling();
        canvasGroup.DOFade(0f, 0.5f).SetDelay(durationBeforeFade).OnComplete(() => Destroy(newFadingMessage));

        return newFadingMessage;
    }
    public void NewFadingMessage(MessageType messageType, string message, float durationBeforeFade = 5f)
    {
        if (!IsValidFadeMessage(message)) return;

        if (_firstFadingMessagePanel.activeInHierarchy)
        {
            // If a fading message is already being shown, create a new one
            GameObject newFadingMessage = CreateFadingMessageObject(
                       fadingMessagePrefab: _fadingMessagePrefab,
                       parentObject: _fadingMessagesContainer,
                       messageType: messageType,
                       message: message,
                       durationBeforeFade: durationBeforeFade);
        }
        else
        {
            _fadingMessageCanvasGroup.alpha = 1f;
            _fadingMessageText.text = message;
            _fadingMessageCloseButton.onClick.RemoveAllListeners();
            _fadingMessageCloseButton.onClick.AddListener(() => _firstFadingMessagePanel.SetActive(false));
            _firstFadingMessagePanel.SetActive(true);
            HandleFadingMessageTypeIcon(messageType, ref _fadingMessageIcon);
            _firstFadingMessagePanel.transform.SetAsLastSibling();
            _fadingMessageCanvasGroup.DOFade(0f, 0.5f).SetDelay(durationBeforeFade).OnComplete(() => _firstFadingMessagePanel.SetActive(false));
        }
    }

    bool IsValidMessage(string message, string title = null)
    {
        if (string.IsNullOrEmpty(message))
        {
            Debug.LogError("Message is null or empty.");
            return false;
        }
        if (title != null && string.IsNullOrEmpty(title))
        {
            Debug.LogError("Message title is null or empty.");
            return false;
        }

        if (message.Length > MaxMessageLength)
        {
            Debug.LogError("Message too long: " + message);
            return false;
        }
        if (title != null && title.Length > MaxMessageTitleLength)
        {
            Debug.LogError("Message title too long: " + title);
            return false;
        }

        return true;
    }
    public void NewUIMessage(MessageType type, string message, string title = null)
    {
        if (!IsValidMessage(message)) return;

        switch (type)
        {
            case MessageType.Error:
                if (string.IsNullOrEmpty(title)) title = "Error";
                _messageIcon.sprite = _errorIcon;
                _messageIcon.rectTransform.localScale = new Vector3(_errorIconScale, _errorIconScale, 1f);
                break;
            case MessageType.Success:
                if (string.IsNullOrEmpty(title)) title = "Success";
                _messageIcon.sprite = _checkmarkIcon;
                _messageIcon.rectTransform.localScale = new Vector3(_checkmarkIconScale, _checkmarkIconScale, 1f);
                break;
            case MessageType.Warning:
                if (string.IsNullOrEmpty(title)) title = "Warning";
                _messageIcon.sprite = _warningIcon;
                _messageIcon.rectTransform.localScale = new Vector3(_warningIconScale, _warningIconScale, 1f);
                break;
            case MessageType.Info:
                if (string.IsNullOrEmpty(title)) title = "Info";
                _messageIcon.sprite = _infoIcon;
                _messageIcon.rectTransform.localScale = new Vector3(_infoIconScale, _infoIconScale, 1f);
                break;
            default:
                Debug.LogError("Invalid message type: " + type);
                break;
        }

        _messageTitleText.text = title;
        _messageText.text = message;
        _messagePanel.SetActive(true);
        _messagePanelCloseButton.onClick.RemoveAllListeners();
        _messagePanelCloseButton.onClick.AddListener(() => _messagePanel.SetActive(false));
        _messagePanel.transform.SetAsLastSibling();
    }

    [ContextMenu("Test Error Message")]
    public void TestErrorMessage()
    {
        NewUIMessage(MessageType.Error, "This is a test error messagezzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzz", "Test Error");
    }

    [ContextMenu("Test Success Message")]
    public void TestSuccessMessage()
    {
        NewUIMessage(MessageType.Success, "This is a test success message.", "Test Success");
    }

    [ContextMenu("Test Warning Message")]
    public void TestWarningMessage()
    {
        NewUIMessage(MessageType.Warning, "This is a test warning message.", "Test Warning");
    }

    [ContextMenu("Test Info Message")]
    public void TestInfoMessage()
    {
        NewUIMessage(MessageType.Info, "This is a test info message.", "Test Info");
    }

    [ContextMenu("Test Fading Messages")]
    public void TestFadingMessages()
    {
        NewFadingMessage(MessageType.Error, $"This is a test Error message.", 10f);
        NewFadingMessage(MessageType.Success, $"This is a test Success message.", 10f);
        NewFadingMessage(MessageType.Info, $"This is a test Info message.", 10f);
        NewFadingMessage(MessageType.Warning, $"This is a test Warning message.", 10f);
    }
}
