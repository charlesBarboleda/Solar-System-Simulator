using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

public class SimulationAssetContainerPrefabManager : MonoBehaviour
{
    // UI containers
    [SerializeField] GameObject _contextMenuContainer;
    [SerializeField] GameObject _editContextMenuContainer;
    [SerializeField] GameObject _editNameContainer;
    [SerializeField] GameObject _editMassContainer;
    [SerializeField] GameObject _editDiameterContainer;



    // Display UI texts
    [SerializeField] TextMeshProUGUI _nameText;
    [SerializeField] RawImage _displayImage;
    [SerializeField] TextMeshProUGUI _massText;
    [SerializeField] TextMeshProUGUI _diameterText;
    [SerializeField] TextMeshProUGUI _bodyTypeText;

    // Edit name input
    [SerializeField] TMP_InputField _editNameInput;

    // Edit mass input
    [SerializeField] TMP_InputField _editMassInput;

    // Edit diameter input
    [SerializeField] TMP_InputField _editDiameterInput;


    Data _data;

    public void OnEditDiameterApplyClick()
    {
        double newDiameter = _data.Body.Diameter;
        if (!double.TryParse(_editDiameterInput.text, out newDiameter))
        {
            UIMessage.Instance.NewUIMessage(MessageType.Error, $"Diameter input '{_editDiameterInput.text}' is invalid!", "Invalid Diameter");
            return;
        }

        UIMessage.Instance.NewUIConfirmation($"Change diameter of {_data.Body.Name} to {newDiameter:N0} M?",
         title: "Confirm Action",
         onYes: () =>
         {
             SimulationAssetDatabaseManager.Instance.EditBodyDiameter(_data.Body.Name, newDiameter);
         },
         onNo: null);
    }

    public void OnEditDiameterCloseButton()
    {
        _editDiameterContainer.SetActive(false);

        _editContextMenuContainer.SetActive(true);
    }

    public void OnEditDiameterClick()
    {
        _editDiameterContainer.SetActive(true);

        _editContextMenuContainer.SetActive(false);
    }

    public void OnEditMassApplyClick()
    {
        double newMass = _data.Body.Mass;
        if (!double.TryParse(_editMassInput.text, out newMass))
        {
            UIMessage.Instance.NewUIMessage(MessageType.Error, $"Mass input '{_editMassInput.text}' is invalid!", "Invalid Mass");
            return;
        }

        UIMessage.Instance.NewUIConfirmation($"Change mass of {_data.Body.Name} to {newMass:E2} KG?",
         title: "Confirm Action",
         onYes: () =>
         {
             SimulationAssetDatabaseManager.Instance.EditBodyMass(_data.Body.Name, newMass);
         },
         onNo: null);
    }

    public void OnEditMassCloseButton()
    {
        _editMassContainer.SetActive(false);
        _editContextMenuContainer.SetActive(true);
    }

    public void OnEditMassClick()
    {
        _editMassContainer.SetActive(true);
        _editContextMenuContainer.SetActive(false);
    }


    public void OnEditNameCloseButton()
    {
        _editNameContainer.SetActive(false);

        _editContextMenuContainer.SetActive(true);
    }

    public void OnEditNameClick()
    {
        _editNameContainer.SetActive(true);

        _editContextMenuContainer.SetActive(false);
    }

    public void OnEditNameApplyClick()
    {
        UIMessage.Instance.NewUIConfirmation($"Change name of {_data.Body.Name} to {_editNameInput.text}?",
         title: "Confirm Action",
         onYes: () =>
         {
             SimulationAssetDatabaseManager.Instance.EditBodyName(_data.Body.Name, _editNameInput.text);
         },
         onNo: null);
    }

    public void OnEditAssetClick()
    {
        _editContextMenuContainer.SetActive(true);

        _contextMenuContainer.SetActive(false);
    }

    public void OnEditAssetCloseButton()
    {
        _contextMenuContainer.SetActive(true);

        _editContextMenuContainer.SetActive(false);
    }

    public void OnAssetClick()
    {
        if (!_contextMenuContainer.activeInHierarchy) _contextMenuContainer.SetActive(true);
        else _contextMenuContainer.SetActive(false);

        _editContextMenuContainer.SetActive(false);

        _editNameContainer.SetActive(false);
        _editMassContainer.SetActive(false);
        _editDiameterContainer.SetActive(false);

        SimulationAssetsUIManager.Instance.DisableAllContainersExclusive(this);
        AddAssetUIManager.Instance.CloseContainer();
    }

    public void OnContextMenuCloseButton()
    {
        _contextMenuContainer.SetActive(false);
    }


    public void OnAddToSimulationClick()
    {
        AddAssetUIManager.Instance.OpenContainer(_data);
        _contextMenuContainer.SetActive(false);
    }

    public void DisableAllContainers()
    {
        _contextMenuContainer.SetActive(false);
        _editContextMenuContainer.SetActive(false);
        _editNameContainer.SetActive(false);
        _editMassContainer.SetActive(false);
        _editDiameterContainer.SetActive(false);

        AddAssetUIManager.Instance.CloseContainer();
    }



    public void OnDeleteFromAssetsClick()
    {
        UIMessage.Instance.NewUIConfirmation($"Delete {_data.Body.Name} from asset database? This will remove the asset from the world",
        title: "Confirm Action",
        onYes: () =>
        {
            string name = _data.Body.Name;
            SimulationAssetDatabaseManager.Instance.TryDeleteBody(name);
        },
        onNo: null);
    }

    public void InitializeData(Data data)
    {
        SetNameText(data.Body.Name);
        SetDisplayImage(data.Display.DisplayImage);
        SetMassText(data.Body.Mass);
        SetDiameterText(data.Body.Diameter);
        SetBodyType(data.Body.Type);

        _data = data;
    }


    public void SetBodyType(BodyType bodyType)
    {
        string bodyTypeString = bodyType.ToString();

        _bodyTypeText.SetText($"Type: {bodyTypeString}");
    }

    public void SetDiameterText(double diameter)
    {
        if (diameter <= 0)
        {
            Debug.LogError($"Could not set diameter for {_nameText} in Simulation Assets. Invalid diameter value.");
            return;
        }

        _diameterText.SetText($"Diameter (M): {diameter:E2}");
    }

    public void SetMassText(double mass)
    {
        if (mass <= 0)
        {
            Debug.LogError($"Could not set mass for {_nameText} in Simulation Assets. Invalid mass value.");
            return;
        }

        _massText.SetText($"Mass (KG): {mass:E2}");
    }

    public void SetDisplayImage(Texture2D texture2D = null)
    {
        if (texture2D != null)
        {
            _displayImage.texture = texture2D;
        }
        else
        {
            Debug.LogWarning($"No texture assigned for {_nameText.text} display image");
        }
    }

    public void SetNameText(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            Debug.LogError($"Could not set name for {name} in Simulation Assets. Empty string.");
            return;
        }

        _nameText.SetText(name);
    }

}
