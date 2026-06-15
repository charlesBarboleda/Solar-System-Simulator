using UnityEngine;
using UnityEditor;
using System.IO;

public class SunStreakTextureGenerator : EditorWindow
{
    [MenuItem("Tools/Sun Textures/Generate Anamorphic Streak")]
    public static void GenerateStreakTexture()
    {
        const int width = 2048;
        const int height = 1024;
        const string savePath = "Assets/Textures/T_SunFar_Streak.png";

        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);

        // Slightly blue anamorphic tint
        Color coreColor = new Color(0.95f, 0.97f, 1.00f);  // near white, hint of blue
        Color midColor = new Color(0.60f, 0.75f, 1.00f);  // soft blue mid
        Color edgeColor = new Color(0.20f, 0.35f, 0.80f);  // deeper blue at edges

        System.Random rng = new System.Random(42);

        float centerY = 0.5f;
        float centerX = 0.5f;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float uvX = x / (float)width;
                float uvY = y / (float)height;

                // Horizontal distance from center (drives streak length)
                float dx = Mathf.Abs(uvX - centerX) * 2f; // 0 at center, 1 at edges

                // Vertical distance from center (drives streak thinness)
                float dy = Mathf.Abs(uvY - centerY) * 2f;

                // Core bright spot — tight radial falloff at center
                float radial = Mathf.Sqrt((uvX - centerX) * (uvX - centerX) * 0.25f +
                                            (uvY - centerY) * (uvY - centerY));
                float core = Mathf.Pow(Mathf.Clamp01(1f - radial * 6f), 2f);

                // Streak — very tight vertical falloff, long horizontal
                // Multiple layers for organic thickness variation
                float streakSharp = Mathf.Pow(Mathf.Clamp01(1f - dy * 18f), 3f);
                float streakSoft = Mathf.Pow(Mathf.Clamp01(1f - dy * 6f), 6f) * 0.3f;
                float streakGlow = Mathf.Pow(Mathf.Clamp01(1f - dy * 2f), 8f) * 0.1f;

                // Horizontal falloff — streak fades toward edges with slight noise
                float noise = (float)(rng.NextDouble() * 0.08 - 0.04); // ±4% noise
                float hFalloff = Mathf.Pow(Mathf.Clamp01(1f - dx + noise), 1.5f);

                // Subtle horizontal bands for organic imperfection
                float bands = 1f + Mathf.Sin(uvY * 180f) * 0.04f
                                       + Mathf.Sin(uvY * 430f) * 0.02f;

                float streak = (streakSharp + streakSoft + streakGlow) * hFalloff * bands;
                float total = Mathf.Clamp01(core + streak);

                // Color: core is near-white, streak transitions to blue toward edges
                float colorT = Mathf.Clamp01(dx * 1.2f + dy * 0.5f);
                Color col = Color.Lerp(Color.Lerp(coreColor, midColor, colorT),
                                          edgeColor,
                                          Mathf.Pow(colorT, 2f));

                tex.SetPixel(x, y, new Color(col.r, col.g, col.b, total));
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

        Debug.Log("[SunStreakGenerator] Saved to " + savePath);
    }
}