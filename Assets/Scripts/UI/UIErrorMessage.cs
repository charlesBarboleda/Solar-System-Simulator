using TMPro;
using UnityEngine;

public class UIErrorMessage : MonoBehaviour
{
    // Singleton
    public static UIErrorMessage Instance;

    // References to UI elements
    [SerializeField] GameObject _errorMessagePanel;
    [SerializeField] TextMeshProUGUI _errorMessageText;
    [SerializeField] TextMeshProUGUI _errorTitleText;

    // Limits
    const int MaxErrorMessageLength = 320;
    const int MaxErrorTitleLength = 17;

    void Awake()
    {
        if (_errorMessagePanel == null || _errorMessageText == null || _errorTitleText == null)
        {
            Debug.LogError("Could not initialize error message controller; missing references.");
            return;
        }

        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
    }

    public void NewError(string message, string title = "Error")
    {
        if (message.Length > MaxErrorMessageLength)
        {
            Debug.LogError("Error message too long: " + message);
            return;
        }
        if (title.Length > MaxErrorTitleLength)
        {
            Debug.LogError("Error title too long: " + title);
            return;
        }

        _errorTitleText.text = title;
        _errorMessageText.text = message;
        _errorMessagePanel.SetActive(true);
        _errorMessagePanel.transform.SetAsLastSibling();
    }

    public void CloseErrorMessage()
    {
        _errorMessageText = null;
        _errorTitleText = null;
        _errorMessagePanel.SetActive(false);
    }

    [ContextMenu("Test Error Message")]
    public void TestErrorMessage()
    {
        NewError("This is a test error messagezzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzz", "Test Error123213");
    }
}
