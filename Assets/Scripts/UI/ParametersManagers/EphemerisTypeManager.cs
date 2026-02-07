using TMPro;
using UnityEngine;

public class EphemerisTypeManager : MonoBehaviour, IAPIParameterManager, IDefaultable
{
    [SerializeField] TMP_Dropdown _ephemTypeDropdown;

    public bool TryGetURL(out string URL)
    {
        URL = "EPHEM_TYPE=";

        switch (_ephemTypeDropdown.value)
        {
            // Vectors
            case 0:
                URL += "'VECTORS'";
                return true;
            // Observer
            case 1:
                URL += "'OBSERVER'";
                return true;
            // Elements
            case 2:
                URL += "'ELEMENTS'";
                return true;
            // SPK
            case 3:
                URL += "'SPK'";
                return true;
            // Approach
            case 4:
                URL += "'APPROACH'";
                return true;
        }

        return false;
    }

    public void ApplyDefault()
    {
        _ephemTypeDropdown.value = 0;
        _ephemTypeDropdown.RefreshShownValue();
    }
}
