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

    public static bool TryLoadLocalCatalogDB(out List<BodyCatalog> database, string fileName, string folderName)
    {
        database = null;
        if (!HasLocalJSONDatabase(out string json, fileName, folderName)) return false;

        return TryLoadCatalogDBFromJSON(json, out database);
    }

    public static bool TryStoreCatalogDBAsJSON(List<BodyCatalog> catalogDatabase, string fileName, string folderName)
    {
        if (catalogDatabase == null)
        {
            Debug.LogError("Could not store catalog database as JSON: catalogDatabase is null.");
            return false;
        }

        var jsonWrapper = new BodyCatalogListJSONWrapper { Database = catalogDatabase };
        string json = JsonUtility.ToJson(jsonWrapper, prettyPrint: true);

        string folder = GetCachedFolderFor(folderName);
        string filePath = GetCachedFilePathFor(fileName, folderName);

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
        finally
        {
            if (File.Exists(tempPath)) File.Delete(tempPath);
        }

        Debug.Log($"Catalog database saved locally: {filePath}");
        UIMessage.Instance.NewFadingMessage(MessageType.Success, "Catalog database saved locally", 3f);
        return true;
    }


    static string GetCachedFolderFor(string folderName) => Path.Combine(Application.persistentDataPath, folderName);
    static string GetCachedFilePathFor(string fileName, string folderName) => Path.Combine(GetCachedFolderFor(folderName), fileName);
    static bool HasLocalJSONDatabase(out string json, string fileName, string folderName)
    {
        json = null;
        string path = GetCachedFilePathFor(fileName, folderName);

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

    static bool TryLoadCatalogDBFromJSON(string json, out List<BodyCatalog> database)
    {
        database = null;
        if (string.IsNullOrEmpty(json))
        {
            Debug.LogError($"Could not load a NAIF catalog database from JSON (empty/null)");
            return false;
        }

        var loaded = JsonUtility.FromJson<BodyCatalogListJSONWrapper>(json);
        if (loaded == null || loaded.Database == null || loaded.Database.Count < 1) return false;


        database = new(loaded.Database);
        return true;
    }

}
