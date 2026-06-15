using UnityEngine;
using UnityEditor;
using System.IO;

public class SunLensOrbTextureGenerator : EditorWindow
{
    [MenuItem("Tools/Sun Textures/Generate Lens Orb (Plain)")]
    public static void GeneratePlain()
    {
        Generate("Assets/Textures/T_SunFar_LensOrb.png", brightenEdge: false);
    }

    [MenuItem("Tools/Sun Textures/Generate Lens Orb (Edge Accent)")]
    public static void GenerateEdgeAccent()
    {
        Generate("Assets/Textures/T_SunFar_LensOrb_Edge.png", brightenEdge: true);
    }

    static void Generate(string savePath, bool brightenEdge)
    {
        const int size = 512;
        const float hexRadius = 0.42f; // hex fills most of the texture

        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float uvX = (x / (float)size) - 0.5f;
                float uvY = (y / (float)size) - 0.5f;

                float ax = Mathf.Abs(uvX);
                float ay = Mathf.Abs(uvY);
                float hexDist = Mathf.Max(ay, ax * 0.866025f + ay * 0.5f) / hexRadius;

                if (hexDist >= 1.0f) { tex.SetPixel(x, y, Color.clear); continue; }

                // Circular distance for gradient — perfectly smooth, no seams
                float circDist = Mathf.Sqrt(uvX * uvX + uvY * uvY) / hexRadius;
                float brightness = Mathf.Clamp01(1f - circDist) * 0.5f;

                tex.SetPixel(x, y, new Color(1f, 1f, 1f, brightness));
            }
        }

        tex.Apply();
        SaveTexture(tex, savePath);
        Debug.Log("[SunLensOrbGenerator] Saved to " + savePath);
    }

    static void SaveTexture(Texture2D tex, string savePath)
    {
        string dir = Path.GetDirectoryName(savePath);
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

        File.WriteAllBytes(savePath, tex.EncodeToPNG());
        AssetDatabase.Refresh();

        TextureImporter importer = AssetImporter.GetAtPath(savePath) as TextureImporter;
        if (importer != null)
        {
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.sRGBTexture = false;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
        }
    }
}