using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Unity.Mathematics;

public class ApplyVectorToRowManager : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI _objectNameText;
    [SerializeField] TextMeshProUGUI _rowNumberText;
    [SerializeField] RawImage _displayImage;

    public string ObjectName { get; private set; }
    public ApplyVectorManager.ApplyVectorMode VectorMode { get; private set; }
    public double3 VectorToApply { get; private set; }
    public AstronomicalObject AstronomicalObject { get; private set; }


    public void Initialize(string objectName, int rowNumber, Texture displayImage)
    {
        ObjectName = objectName;
        _objectNameText.text = objectName;
        SetRowNumber(rowNumber);
        _displayImage.texture = displayImage;

        if (NBodyManager.Instance.TryGetAstroObjectByName(objectName, out AstronomicalObject astroObject))
        {
            AstronomicalObject = astroObject;
        }

    }

    public void SetVectorToApply(AstronomicalObject astronomicalObject, double3 vector, ApplyVectorManager.ApplyVectorMode mode)
    {
        AstronomicalObject = astronomicalObject;
        VectorToApply = vector;
        VectorMode = mode;
    }


    public void SetRowNumber(int rowNumber)
    {
        _rowNumberText.text = $"{rowNumber}.";
    }

    public void OnClickApplyButton()
    {
        string vectorType = VectorMode == ApplyVectorManager.ApplyVectorMode.Position ? "Position" : "Velocity";


        UIMessage.Instance.NewUIConfirmation(
            $"Are you sure you want to apply the {vectorType.ToLower()} ({VectorToApply.x}, {VectorToApply.y}, {VectorToApply.z}) to {ObjectName}?",
            $"Apply {vectorType}?",
            onYes: () =>
            {
                switch (VectorMode)
                {
                    case ApplyVectorManager.ApplyVectorMode.Position:
                        SimulationObject relativeToObject = ApplyVectorManager.Instance.GetRelativeToObject();
                        if (NBodyManager.Instance.TrySetObjectPosition(AstronomicalObject, VectorToApply, relativeToObject))
                        {
                            UIMessage.Instance.NewFadingMessage(MessageType.Success, $"Applied position to {ObjectName}", 2f);
                        }
                        break;
                    case ApplyVectorManager.ApplyVectorMode.Velocity:
                        if (NBodyManager.Instance.TrySetObjectVelocity(AstronomicalObject, VectorToApply))
                        {
                            UIMessage.Instance.NewFadingMessage(MessageType.Success, $"Applied velocity to {ObjectName}", 2f);
                        }
                        break;
                }
            },
            onNo: () => { }
        );
    }


}
