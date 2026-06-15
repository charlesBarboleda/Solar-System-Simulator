using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class LiveSimulationRowContainerManager : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI _numberText;
    [SerializeField] TextMeshProUGUI _nameText;
    [SerializeField] RawImage _displayImage;

    AstronomicalObject _astronomicalObject;

    public void Initialize(AstronomicalObject astronomicalObject, int rowNumber)
    {
        _astronomicalObject = astronomicalObject;
        _numberText.text = $"{rowNumber}.";
        _nameText.text = astronomicalObject.Data.Body.Name;
        _displayImage.texture = astronomicalObject.Data.Display.DisplayImage;
    }

    public void SetRowNumber(int rowNumber) => _numberText.text = $"{rowNumber}.";

    public void OnVectorsButtonClick() => LiveSimulationVectorsPanelManager.Instance.OpenPanel(_astronomicalObject);

    public void OnInformationButtonClick() => LiveSimulationInformationPanelManager.Instance.Initialize(_astronomicalObject);

    public void OnRemoveButtonClick()
    {
        UIMessage.Instance.NewUIConfirmation(
            $"Are you sure you want to remove '{_astronomicalObject.Data.Body.Name}' from the simulation?",
            $"Confirm Action",
            onYes: () =>
            {
                NBodyManager.Instance.TryRemoveObjectByName(_astronomicalObject.Data.Body.Name);
            },
            onNo: () => { }
        );
    }

}
