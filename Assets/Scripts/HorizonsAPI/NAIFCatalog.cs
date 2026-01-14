using System.Collections.Generic;
using UnityEngine;

public struct BodyCatalog
{
    public int NAIFID;
    public string Name;
    public string Designation;
    public string Aliases;
}

[CreateAssetMenu(menuName = "Horizons/NAIF Bodies Catalog")]
public class MajorBodies : ScriptableObject
{
    public static List<BodyCatalog> Catalog;

    public static bool TryAddMajorBody(BodyCatalog bodyCatalogData)
    {
        Catalog.Add(bodyCatalogData);
        return true;
    }

}

public static class MinorBodies
{
    public static List<BodyCatalog> Catalog;
}