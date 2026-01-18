using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;


public static class JSONCatalog
{
    [Serializable]
    public class BodyCatalogListJSONWrapper
    {
        public List<BodyCatalog> Database;
    }

    public static readonly string CatalogDatabaseFileName = "naif_major_bodies_latest.json";
    public static readonly string UserCatalogDatabaseFileName = "user_naif_catalog_database.json";
    public static readonly string CatalogDatabaseFolderName = "HorizonsNAIFDatabaseCache";

    public static bool TryStoreCatalogDBAsJSON(List<BodyCatalog> catalogDatabase)
    {
        if (catalogDatabase == null || catalogDatabase.Count <= 0)
        {
            Debug.LogError($"Could not store {catalogDatabase} as JSON");
            return false;
        }

        var jsonWrapper = new BodyCatalogListJSONWrapper { Database = catalogDatabase };
        string json = JsonUtility.ToJson(jsonWrapper, prettyPrint: true);
        string folder = GetCachedFolderFor(CatalogDatabaseFolderName);
        string filePath = GetCachedFilePathFor(CatalogDatabaseFileName, CatalogDatabaseFolderName);
        Directory.CreateDirectory(folder);
        string tempPath = filePath + ".tmp";

        try
        {
            File.WriteAllText(tempPath, json);
            File.Copy(tempPath, filePath, overwrite: true);
            File.Delete(tempPath);
        }
        catch (Exception e)
        {
            Debug.LogError(e);
            return false;
        }

        if (File.Exists(tempPath)) File.Delete(tempPath);

        Debug.Log($"Saved catalog JSON to: {filePath}");
        return true;
    }

    static string GetCachedFolderFor(string folderName) => Path.Combine(Application.persistentDataPath, folderName);
    static string GetCachedFilePathFor(string fileName, string folderName) => Path.Combine(GetCachedFolderFor(folderName), fileName);

    public static bool HasLocalJSONDatabase(out string json)
    {
        json = null;
        string path = GetCachedFilePathFor(CatalogDatabaseFileName, CatalogDatabaseFolderName);

        if (!File.Exists(path))
        {
            Debug.LogWarning($"Could not find a valid file that exists in '{path}'");
            return false;
        }

        try
        {
            json = File.ReadAllText(path);
        }
        catch (Exception e)
        {
            Debug.LogError(e);
            return false;
        }

        return !string.IsNullOrWhiteSpace(json);
    }

    public static bool TryLoadCatalogDBFromJSON(string json, out List<BodyCatalog> database)
    {
        database = null;
        if (string.IsNullOrEmpty(json))
        {
            Debug.LogError($"Could not load a NAIF catalog database from JSON (empty/null)");
            return false;
        }

        var loaded = JsonUtility.FromJson<BodyCatalogListJSONWrapper>(json);
        if (loaded == null || loaded.Database == null || loaded.Database.Count < 1)
        {
            Debug.LogError($"Could not deserialize a NAIF catalog database from JSON (empty/null)");
            return false;
        }

        database = new(loaded.Database);
        return true;
    }

    public static bool TryLoadLocalCatalogDatabase(out List<BodyCatalog> database)
    {
        database = null;
        if (!HasLocalJSONDatabase(out string json)) return false;

        return TryLoadCatalogDBFromJSON(json, out database);
    }
}
