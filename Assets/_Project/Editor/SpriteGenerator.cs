using System.IO;
using UnityEditor;
using UnityEngine;

namespace BrainBattle.Editor
{
    public static class SpriteGenerator
    {
        private const string OutputDir     = "Assets/_Project/Resources/Sprites";
        private const int    TextureSize   = 32;
        private const float  PixelsPerUnit = 32f;

        [MenuItem("BrainBattle/Generate Placeholder Sprites")]
        static void Generate()
        {
            string absDir = Path.Combine(
                Application.dataPath, "_Project", "Resources", "Sprites");
            Directory.CreateDirectory(absDir);

            SaveSprite(CreateDotTexture(),   "DotSprite");
            SaveSprite(CreateCrownTexture(), "CrownSprite");

            AssetDatabase.Refresh();
            Debug.Log($"[SpriteGenerator] DotSprite and CrownSprite saved to {OutputDir}");
        }

        // ── Dot ───────────────────────────────────────────────────────────────────

        static Texture2D CreateDotTexture()
        {
            var tex = NewTexture();
            var px  = new Color[TextureSize * TextureSize];

            float cx = (TextureSize - 1) * 0.5f;
            float cy = (TextureSize - 1) * 0.5f;
            float r  = TextureSize * 0.5f - 1.5f; // slight inset so the edge doesn't clip

            for (int y = 0; y < TextureSize; y++)
                for (int x = 0; x < TextureSize; x++)
                {
                    float dx    = x - cx;
                    float dy    = y - cy;
                    float dist  = Mathf.Sqrt(dx * dx + dy * dy);
                    // 1-pixel soft anti-aliased edge
                    float alpha = Mathf.Clamp01(r + 0.5f - dist);
                    px[y * TextureSize + x] = new Color(1f, 1f, 1f, alpha);
                }

            tex.SetPixels(px);
            tex.Apply();
            return tex;
        }

        // ── Crown ─────────────────────────────────────────────────────────────────

        static Texture2D CreateCrownTexture()
        {
            var tex = NewTexture();
            var px  = new Color[TextureSize * TextureSize];

            for (int y = 0; y < TextureSize; y++)
                for (int x = 0; x < TextureSize; x++)
                    px[y * TextureSize + x] = IsInsideCrown(x, y) ? Color.white : Color.clear;

            tex.SetPixels(px);
            tex.Apply();
            return tex;
        }

        // Crown geometry — all coords in Texture2D space (y=0 = bottom of image).
        //
        //         *           *
        //        ***    *    ***
        //       *****  ***  *****
        //      ******* *** *******
        //      ***********************   ← base band
        //      ***********************
        //
        // Base band:    y  2–10, x  2–29
        // Left  peak:   tip (7, 26)  — base edge y=10, x=[2,12]
        // Centre peak:  tip (16, 30) — base edge y=10, x=[11,21]
        // Right  peak:  tip (25, 26) — base edge y=10, x=[20,29]
        static bool IsInsideCrown(int x, int y)
        {
            // Base rectangle
            if (y >= 2 && y <= 10 && x >= 2 && x <= 29)
                return true;

            // Left peak triangle
            if (y > 10 && y <= 26)
            {
                float t     = (y - 10f) / 16f;
                int   left  = Mathf.RoundToInt(Mathf.Lerp( 2f,  7f, t));
                int   right = Mathf.RoundToInt(Mathf.Lerp(12f,  7f, t));
                if (x >= left && x <= right) return true;
            }

            // Centre peak triangle (tallest)
            if (y > 10 && y <= 30)
            {
                float t     = (y - 10f) / 20f;
                int   left  = Mathf.RoundToInt(Mathf.Lerp(11f, 16f, t));
                int   right = Mathf.RoundToInt(Mathf.Lerp(21f, 16f, t));
                if (x >= left && x <= right) return true;
            }

            // Right peak triangle
            if (y > 10 && y <= 26)
            {
                float t     = (y - 10f) / 16f;
                int   left  = Mathf.RoundToInt(Mathf.Lerp(20f, 25f, t));
                int   right = Mathf.RoundToInt(Mathf.Lerp(29f, 25f, t));
                if (x >= left && x <= right) return true;
            }

            return false;
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        static Texture2D NewTexture()
        {
            var tex        = new Texture2D(TextureSize, TextureSize, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode   = WrapMode.Clamp;
            return tex;
        }

        static void SaveSprite(Texture2D tex, string assetName)
        {
            string assetPath = $"{OutputDir}/{assetName}.png";
            string absPath   = Path.Combine(
                Application.dataPath, "_Project", "Resources", "Sprites",
                assetName + ".png");

            File.WriteAllBytes(absPath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);

            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);

            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
            {
                Debug.LogError($"[SpriteGenerator] Failed to get TextureImporter for {assetPath}");
                return;
            }

            importer.textureType         = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = PixelsPerUnit;
            importer.spritePivot         = new Vector2(0.5f, 0.5f);
            importer.mipmapEnabled       = false;
            importer.alphaIsTransparency = true;
            importer.filterMode          = FilterMode.Bilinear;
            importer.textureCompression  = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();

            Debug.Log($"[SpriteGenerator] Saved {assetPath}");
        }
    }
}
