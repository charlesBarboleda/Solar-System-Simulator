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
    [SerializeField] GameObject _fadingMessagePanel;
    [SerializeField] CanvasGroup _fadingMessageCanvasGroup;
    [SerializeField] TextMeshProUGUI _fadingMessageText;
    [SerializeField] Button _fadingMessageCloseButton;


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
        _fadingMessagePanel.SetActive(false);
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

    bool IsValidMessage(string message, string title)
    {
        if (string.IsNullOrEmpty(message))
        {
            Debug.LogError("Message is null or empty.");
            return false;
        }
        if (string.IsNullOrEmpty(title))
        {
            Debug.LogError("Message title is null or empty.");
            return false;
        }

        if (message.Length > MaxMessageLength)
        {
            Debug.LogError("Message too long: " + message);
            return false;
        }
        if (title.Length > MaxMessageTitleLength)
        {
            Debug.LogError("Message title too long: " + title);
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

    public void NewFadingMessage(string message, float durationBeforeFade = 2f)
    {
        if (!IsValidFadeMessage(message)) return;

        if (_fadingMessagePanel.activeInHierarchy)
        {
            // If a fading message is already being shown, create a new one
            GameObject newFadingMessage = Instantiate(_fadingMessagePanel, _fadingMessagesContainer.transform);
            TextMeshProUGUI newFadingMessageText = newFadingMessage.GetComponentInChildren<TextMeshProUGUI>();
            CanvasGroup canvasGroup = newFadingMessage.GetComponent<CanvasGroup>();
            Button closeButton = newFadingMessage.GetComponentInChildren<Button>();
            closeButton.onClick.AddListener(() => Destroy(newFadingMessage));
            newFadingMessageText.text = message;
            newFadingMessage.SetActive(true);
            newFadingMessage.transform.SetAsLastSibling();
            canvasGroup.DOFade(0f, 0.5f).SetDelay(durationBeforeFade).OnComplete(() => Destroy(newFadingMessage));
        }
        else
        {
            _fadingMessageCanvasGroup.alpha = 1f;
            _fadingMessageText.text = message;
            _fadingMessageCloseButton.onClick.RemoveAllListeners();
            _fadingMessageCloseButton.onClick.AddListener(() => _fadingMessagePanel.SetActive(false));
            _fadingMessagePanel.SetActive(true);
            _fadingMessagePanel.transform.SetAsLastSibling();
            _fadingMessageCanvasGroup.DOFade(0f, 0.5f).SetDelay(durationBeforeFade).OnComplete(() => _fadingMessagePanel.SetActive(false));
        }
    }

    public void NewUIMessage(MessageType type, string message, string title)
    {
        if (!IsValidMessage(message, title)) return;

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
        _messagePanel.transform.SetAsLastSibling();
    }

    public void CloseMessage()
    {
        _messagePanel.SetActive(false);
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
}
