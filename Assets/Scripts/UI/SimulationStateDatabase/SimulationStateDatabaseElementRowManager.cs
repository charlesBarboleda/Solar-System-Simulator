using TMPro;
using UnityEngine;

public class SimulationStateDatabaseElementRowManager : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI _mainText;

    SaveSlotMeta _metaData;
    public void Initialize(SaveSlotMeta metaData)
    {
        _metaData = metaData;
        SetMainText(metaData.DisplayName, metaData.BodyCount, metaData.SavedAt);
    }

    public void OnLoadClick()
    {
        UIMessage.Instance.NewUIConfirmation($"Are you sure you want to load \"{_metaData.DisplayName}\"? This will overwrite your current simulation.",
             onYes: () => SimulationSaveLoad.Load(_metaData.Id),
             onNo: () => { }
             );
    }

    public void OnDeleteClick()
    {
        UIMessage.Instance.NewUIConfirmation($"Are you sure you want to delete \"{_metaData.DisplayName}\"? This action cannot be undone.",
             onYes: () =>
             {
                 SimulationSaveLoad.Delete(_metaData.Id);
                 SimulationStateDatabaseUIManager.Instance.SetInitialize(false);
                 SimulationStateDatabaseUIManager.Instance.Initialize();
             },
             onNo: () => { }
             );
    }

    public void SetMainText(string displayName, int objectCount, string savedAt)
    {
        if (!string.IsNullOrEmpty(displayName)) _mainText.text = $"{Truncate(displayName, 25)}\n({objectCount} simulation objects)";
        else _mainText.text = $"Unnamed Save ({objectCount} simulation objects)";
    }

    string Truncate(string text, int maxLength)
    {
        const string suffix = "...";

        if (string.IsNullOrEmpty(text) || text.Length <= maxLength) return text;

        if (maxLength <= suffix.Length) return suffix[..maxLength];

        return text[..(maxLength - suffix.Length)] + suffix;
    }

}
