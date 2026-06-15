using UnityEngine;
using UnityEditor;
using System.IO;

public class SunChromAberrationTextureGenerator : EditorWindow
{
    [MenuItem("Tools/Sun Textures/Generate Chromatic Aberration Outer Ring")]
    public static void GenerateOuterRing()
    {
        const int size = 2048;
        const string savePath = "Assets/Textures/T_SunFar_ChromAberration_Outer.png";

        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);

        const float outerRingHalfWidth = 0.024f;
        const float outerRingRadius = 0.400f;
        const float outerOpacity = 0.15f;
        const float plateauFraction = 0.25f;

        Color outerRed = new Color(0.90f, 0.08f, 0.03f);
        Color outerYellow = new Color(0.95f, 0.88f, 0.05f);
        Color outerGreen = new Color(0.08f, 0.85f, 0.18f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float uvX = (x / (float)size) - 0.5f;
                float uvY = (y / (float)size) - 0.5f;
                float radius = Mathf.Sqrt(uvX * uvX + uvY * uvY) * 2f;

                float outerDist = Mathf.Abs(radius - outerRingRadius);
                if (outerDist >= outerRingHalfWidth)
                {
                    tex.SetPixel(x, y, Color.clear);
                    continue;
                }

                float plateauEdge = outerRingHalfWidth * plateauFraction;
                float edgeZone = outerRingHalfWidth - plateauEdge;
                float edgeDist = Mathf.Max(0f, outerDist - plateauEdge);
                float outerShape = 1f - Mathf.SmoothStep(0f, 1f, edgeDist / edgeZone);

                float signed = radius - outerRingRadius;
                float t = Mathf.Clamp01((signed + outerRingHalfWidth) / (outerRingHalfWidth * 2f));

                float tRemapped = t < 0.5f
                    ? 0.5f * Mathf.Pow(t * 2f, 3f)
                    : 1f - 0.5f * Mathf.Pow((1f - t) * 2f, 3f);

                Color baseColor = Color.Lerp(outerRed, outerGreen, tRemapped);
                float yellowHint = Mathf.Sin(t * Mathf.PI) * 0.45f;
                Color outerColor = Color.Lerp(baseColor, outerYellow, yellowHint);

                float edgeCorrection = 1f - Mathf.Pow(Mathf.Abs(t - 0.5f) * 2f, 2f) * 0.25f;
                float brightness = outerShape * outerOpacity * edgeCorrection;

                if (brightness < 0.005f) { tex.SetPixel(x, y, Color.clear); continue; }

                tex.SetPixel(x, y, new Color(outerColor.r, outerColor.g, outerColor.b, brightness));
            }
        }

        tex.Apply();
        SaveTexture(tex, savePath);
        Debug.Log("[ChromAberrationGenerator] Outer ring saved to " + savePath);
    }

    [MenuItem("Tools/Sun Textures/Generate Chromatic Aberration Inner Ring")]
    public static void GenerateInnerRing()
    {
        const int size = 2048;
        const string savePath = "Assets/Textures/T_SunFar_ChromAberration_Inner.png";

        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);

        // Identical constants to the combined generator
        const float outerRingHalfWidth = 0.024f;
        const float outerRingRadius = 0.400f;
        const float plateauFraction = 0.25f;
        const float innerRingHalfWidth = outerRingHalfWidth * 0.35f * 1.75f;
        const float ringGap = 0.025f;
        const float innerRingRadius = outerRingRadius - outerRingHalfWidth - ringGap - innerRingHalfWidth;
        const float innerOpacity = 0.10f;

        Color innerBlue = new Color(0.12f, 0.30f, 1.00f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float uvX = (x / (float)size) - 0.5f;
                float uvY = (y / (float)size) - 0.5f;
                float radius = Mathf.Sqrt(uvX * uvX + uvY * uvY) * 2f;

                float innerDist = Mathf.Abs(radius - innerRingRadius);
                if (innerDist >= innerRingHalfWidth)
                {
                    tex.SetPixel(x, y, Color.clear);
                    continue;
                }

                float innerPlateauEdge = innerRingHalfWidth * plateauFraction;
                float innerEdgeZone = innerRingHalfWidth - innerPlateauEdge;
                float innerEdgeDist = Mathf.Max(0f, innerDist - innerPlateauEdge);
                float innerShape = 1f - Mathf.SmoothStep(0f, 1f, innerEdgeDist / innerEdgeZone);

                float signed = radius - innerRingRadius;
                float tInner = Mathf.Clamp01((signed + innerRingHalfWidth) / (innerRingHalfWidth * 2f));
                float radFade = Mathf.Lerp(1.0f, 0.45f, tInner);

                float brightness = innerShape * innerOpacity * radFade;

                if (brightness < 0.005f) { tex.SetPixel(x, y, Color.clear); continue; }

                tex.SetPixel(x, y, new Color(innerBlue.r, innerBlue.g, innerBlue.b, brightness));
            }
        }

        tex.Apply();
        SaveTexture(tex, savePath);
        Debug.Log("[ChromAberrationGenerator] Inner ring saved to " + savePath);
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