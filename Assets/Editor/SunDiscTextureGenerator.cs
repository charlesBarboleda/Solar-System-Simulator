using UnityEngine;
using UnityEditor;
using System.IO;

public class SunDiscTextureGenerator : EditorWindow
{
    [MenuItem("Tools/Sun Textures/Generate Far Disc Gradient")]
    public static void GenerateTexture()
    {
        const int size = 1024; // disc doesn't need high res — smooth gradient
        const string savePath = "Assets/Textures/T_SunFar_Disc.png";

        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float uvX = (x / (float)size) - 0.5f;
                float uvY = (y / (float)size) - 0.5f;
                float radius = Mathf.Sqrt(uvX * uvX + uvY * uvY) * 2f; // 0 at center, 1 at edge

                // Hard disc core — full brightness up to coreRadius
                const float coreRadius = 0.09f;
                // Falloff zone — brightness fades from coreRadius to fadeRadius
                const float fadeRadius = 1.0f;

                const float exponent = 2.5f; // controls falloff curve — higher = sharper dropoff

                float brightness;
                if (radius <= coreRadius)
                {
                    brightness = 1.0f;
                }
                else if (radius <= fadeRadius)
                {
                    // Smooth power falloff — stays bright near core, drops off toward edge
                    float t = (radius - coreRadius) / (fadeRadius - coreRadius);
                    brightness = Mathf.Pow(1f - t, exponent);
                }
                else
                {
                    brightness = 0f;
                }

                if (brightness < 0.002f)
                {
                    tex.SetPixel(x, y, Color.clear);
                    continue;
                }

                // Pure white — emission color is controlled by the material's emissive color
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, brightness));
            }
        }

        tex.Apply();

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

        Debug.Log("[SunFarDiscGenerator] Saved to " + savePath);
    }
}