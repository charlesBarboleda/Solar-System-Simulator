using UnityEngine;

public interface IAPIParameterManager
{
    public bool TryGetURL(out string URL);

    public GameObject GetParameterContainer();


}
