using UnityEngine;

public class LightFactory : MonoBehaviour
{
    public static LightFactory Instance { get; private set; }

    [SerializeField] GameObject _lightObjectPrefab;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public bool TryCreateLightSource(AstronomicalObject astroObject, out GameObject go)
    {
        go = null;

        if (astroObject.Data.Body.Type != BodyType.Star)
        {
            Debug.LogError($"{astroObject.Data.Body.Name} must be of body type 'Star' to create a light source.");
            return false;
        }

        go = Instantiate(_lightObjectPrefab);
        go.name = $"[{astroObject.name}] Light Source";

        if (!go.TryGetComponent(out LightManager lightManager)) lightManager = go.AddComponent<LightManager>();

        lightManager.Initialize(astroObject);

        return true;
    }


}
