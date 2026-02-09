using UnityEngine;

public class MainContentCanvasManager : MonoBehaviour
{
    public static MainContentCanvasManager Instance { get; private set; }
    [SerializeField] Canvas _mainContentCanvas;


    void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;

    }
    public void SetSortOrder(int sortOrder) => _mainContentCanvas.sortingOrder = sortOrder;

}
