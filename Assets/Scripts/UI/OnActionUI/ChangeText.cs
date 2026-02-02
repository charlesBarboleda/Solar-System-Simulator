using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class ChangeText : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Text Reference")]
    [SerializeField] TextMeshProUGUI _textComponent;

    [Header("Settings")]
    [SerializeField] bool _applyStyle;

    [Header("Text Style Settings")]
    [SerializeField] bool _hoverEnterBold;
    [SerializeField] bool _hoverExitBold;

    [SerializeField] bool _hoverEnterItalic;
    [SerializeField] bool _hoverExitItalic;

    [SerializeField] bool _hoverEnterUnderline;
    [SerializeField] bool _hoverExitUnderline;

    [SerializeField] int _hoverEnterFontSize;
    [SerializeField] int _hoverExitFontSize;

    [SerializeField] Color _hoverEnterFontColor;
    [SerializeField] Color _hoverExitFontColor;


    void Awake()
    {
        if (_textComponent == null)
        {
            if (!TryGetComponent(out _textComponent))
            {
                enabled = false;
                return;
            }
            else Debug.LogError("No TextMeshProUGUI component found.");

        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_applyStyle)
        {
            if (_hoverEnterBold) _textComponent.fontStyle = FontStyles.Bold;
            if (_hoverEnterItalic) _textComponent.fontStyle = FontStyles.Italic;
            if (_hoverEnterUnderline) _textComponent.fontStyle = FontStyles.Underline;

            _textComponent.color = _hoverEnterFontColor;
            _textComponent.fontSize = _hoverEnterFontSize;
        }

        return;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (_applyStyle)
        {
            if (_hoverExitBold) _textComponent.fontStyle = FontStyles.Bold;
            else _textComponent.fontStyle = FontStyles.Normal;
            if (_hoverExitItalic) _textComponent.fontStyle = FontStyles.Italic;
            else _textComponent.fontStyle = FontStyles.Normal;
            if (_hoverExitUnderline) _textComponent.fontStyle = FontStyles.Underline;
            else _textComponent.fontStyle = FontStyles.Normal;

            _textComponent.color = _hoverExitFontColor;
            _textComponent.fontSize = _hoverExitFontSize;
        }

        return;
    }



}
