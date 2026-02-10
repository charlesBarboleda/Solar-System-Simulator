using TMPro;
using UnityEngine;
using UnityEngine.UIElements.Experimental;

public class ReferencePlaneManager : MonoBehaviour, IAPIParameterManager, IDefaultable
{
    [SerializeField] TMP_Dropdown _referencePlaneDropdown;


    public void ApplyDefault()
    {
        _referencePlaneDropdown.value = 0;
        _referencePlaneDropdown.RefreshShownValue();
    }
    public bool TryGetURL(out string URL)
    {
        InputDropdown input = (InputDropdown)_referencePlaneDropdown.value;

        string token = input.ToString().ToUpperInvariant();   // "ECLIPTIC", "FRAME", "BODYEQUATOR"
        string value = HorizonsAPIParameters.EncodeQuoted(token);

        URL = $"REF_PLANE={value}";
        return true;
    }

    enum InputDropdown
    {
        Ecliptic,
        Frame,
        BodyEquator
    }
}
