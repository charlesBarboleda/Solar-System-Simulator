using UnityEngine;
using TMPro;

public class UnderlineText : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI _textElement;

    void OnEnable()
    {
        _textElement.fontStyle = FontStyles.Underline;
    }

    void OnDisable()
    {
        _textElement.fontStyle = FontStyles.Normal;
    }
}
