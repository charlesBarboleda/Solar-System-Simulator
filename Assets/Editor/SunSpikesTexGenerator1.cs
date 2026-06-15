using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

public class SunSpikesTextureGenerator : EditorWindow
{
    [MenuItem("Tools/Sun Textures/Generate Diffraction Spikes")]
    public static void GenerateSpikesTexture()
    {
        const int size = 2048;
        const string savePath = "Assets/Textures/T_SunFar_Spikes.png";

        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);

        Color coreColor = new Color(0.95f, 0.97f, 1.00f);
        Color spikeColor = new Color(0.75f, 0.85f, 1.00f);
        Color tipColor = new Color(0.50f, 0.60f, 0.90f);

        // --- Spike dimensions ---
        const float majorMaxRadius = 0.62f;
        const float majorHalfWidth = 8.25f;
        const float majorBrightness = 1.00f;

        const float minorMaxRadius = majorMaxRadius * 0.75f;
        const float minorHalfWidth = majorHalfWidth * 0.85f;
        const float minorBrightness = 0.55f;

        const float smallMaxRadius = minorMaxRadius * 0.50f;
        const float smallHalfWidth = minorHalfWidth * 0.25f; // +25% from previous 0.20
        const float smallBrightness = 0.30f;

        // Width taper: center = full width, tip = 10% of center width
        const float widthTaperMin = 0.10f;

        float[] majorAngles = { 90f, 30f, 330f, 270f, 210f, 150f };

        var spikes = new List<(float angle, float halfWidth, float maxRadius, float brightness)>();

        for (int i = 0; i < majorAngles.Length; i++)
        {
            float a = majorAngles[i];
            float b = majorAngles[(i + 1) % majorAngles.Length];
            float bUnwrapped = b;
            while (bUnwrapped > a) bUnwrapped -= 360f;
            float midAB = (a + bUnwrapped) * 0.5f;

            spikes.Add((a, majorHalfWidth, majorMaxRadius, majorBrightness));
            spikes.Add((midAB, minorHalfWidth, minorMaxRadius, minorBrightness));

            float midAMid = (a + midAB) * 0.5f;
            float midMidB = (midAB + bUnwrapped) * 0.5f;
            spikes.Add((midAMid, smallHalfWidth, smallMaxRadius, smallBrightness));
            spikes.Add((midMidB, smallHalfWidth, smallMaxRadius, smallBrightness));
        }

        System.Random rng = new System.Random(42);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float uvX = (x / (float)size) - 0.5f;
                float uvY = (y / (float)size) - 0.5f;

                float radius = Mathf.Sqrt(uvX * uvX + uvY * uvY) * 2f;
                float angleDeg = Mathf.Atan2(uvY, uvX) * Mathf.Rad2Deg;
                if (angleDeg < 0f) angleDeg += 360f;

                float totalBrightness = 0f;
                Color totalColor = Color.black;

                foreach (var spike in spikes)
                {
                    if (radius > spike.maxRadius) continue;

                    float angDiff1 = Mathf.Abs(Mathf.DeltaAngle(angleDeg, spike.angle));
                    float angDiff2 = Mathf.Abs(Mathf.DeltaAngle(angleDeg, spike.angle + 180f));
                    float angDiff = Mathf.Min(angDiff1, angDiff2);

                    // Taper: widest at center, narrowest at tip
                    float taperT = Mathf.Clamp01(radius / spike.maxRadius);
                    float taperedHalfWidth = spike.halfWidth * Mathf.Lerp(1.0f, widthTaperMin, taperT);

                    if (angDiff >= taperedHalfWidth) continue;

                    float angFalloff = Mathf.Pow(Mathf.Clamp01(1f - angDiff / taperedHalfWidth), 2.5f);

                    // Gentler radial falloff (exponent 1.2 instead of 2.5) so spikes stay
                    // bright enough along their length for the taper silhouette to be visible
                    float radFalloff = Mathf.Pow(Mathf.Clamp01(1f - radius / spike.maxRadius), 2f);

                    float noise = (float)(rng.NextDouble() * 0.06 - 0.03);
                    float brightness = Mathf.Clamp01(angFalloff * (radFalloff + noise) * spike.brightness);

                    if (brightness > 0.005f)
                    {
                        float colorT = Mathf.Clamp01(radius / spike.maxRadius);
                        Color sColor = Color.Lerp(
                            Color.Lerp(coreColor, spikeColor, colorT),
                            tipColor,
                            Mathf.Pow(colorT, 2f)
                        );
                        totalColor += sColor * brightness;
                        totalBrightness += brightness;
                    }
                }

                totalBrightness = Mathf.Clamp01(totalBrightness);

                Color finalColor = totalBrightness > 0f
                    ? totalColor / (totalColor.r + totalColor.g + totalColor.b + 0.001f)
                    : Color.black;

                // Straight alpha — store full color in RGB, opacity in alpha only
                // Prevents black-bleed at transparent edges
                tex.SetPixel(x, y, new Color(
                    finalColor.r,
                    finalColor.g,
                    finalColor.b,
                    totalBrightness
                ));
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

        Debug.Log("[SunSpikesGenerator] Saved to " + savePath);
    }
}