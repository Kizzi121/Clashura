#if UNITY_EDITOR
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Clashura.Characters.Data;

namespace Clashura.Utilities
{
    /// <summary>
    /// Processes character concept art illustrations:
    /// - Removes solid white/light background using edge-seeded flood-fill and soft alpha feathering.
    /// - Generates transparent 32-bit PNG full-body Artwork.
    /// - Auto-crops head & face bust for Portrait.
    /// - Downscales full-body cutout for in-game 2D Gameplay Sprite.
    /// - Connects assets to CharacterDataSO and Character2D Prefab.
    /// </summary>
    public static class ClashuraCutoutProcessor
    {
        public static bool ProcessCharacterArt(string sourceJpgPath, string charName, string charId)
        {
            if (!File.Exists(sourceJpgPath))
            {
                Debug.LogError($"[ClashuraCutoutProcessor] Source image not found: {sourceJpgPath}");
                return false;
            }

            byte[] fileBytes = File.ReadAllBytes(sourceJpgPath);
            Texture2D srcTex = new Texture2D(2, 2);
            if (!srcTex.LoadImage(fileBytes))
            {
                Debug.LogError($"[ClashuraCutoutProcessor] Failed to load image from: {sourceJpgPath}");
                return false;
            }

            int w = srcTex.width;
            int h = srcTex.height;
            Color[] pixels = srcTex.GetPixels();

            // 1. Flood fill from border perimeter
            bool[] isBg = new bool[w * h];
            var queue = new Queue<int>();

            void TryEnqueue(int x, int y)
            {
                if (x < 0 || x >= w || y < 0 || y >= h) return;
                int idx = y * w + x;
                if (isBg[idx]) return;
                Color c = pixels[idx];
                // Background threshold (near white / light studio backdrop)
                if (c.r > 0.86f && c.g > 0.86f && c.b > 0.86f)
                {
                    isBg[idx] = true;
                    queue.Enqueue(idx);
                }
            }

            for (int x = 0; x < w; x++) { TryEnqueue(x, 0); TryEnqueue(x, h - 1); }
            for (int y = 0; y < h; y++) { TryEnqueue(0, y); TryEnqueue(w - 1, y); }

            int[] dx = { 1, -1, 0, 0 };
            int[] dy = { 0, 0, 1, -1 };

            while (queue.Count > 0)
            {
                int curr = queue.Dequeue();
                int cx = curr % w;
                int cy = curr / w;

                for (int i = 0; i < 4; i++)
                {
                    TryEnqueue(cx + dx[i], cy + dy[i]);
                }
            }

            // 2. Build transparent cutout with edge feathering
            Texture2D cutoutTex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color[] outPixels = new Color[w * h];

            int minX = w, maxX = 0, minY = h, maxY = 0;

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    int idx = y * w + x;
                    if (isBg[idx])
                    {
                        outPixels[idx] = Color.clear;
                    }
                    else
                    {
                        if (x < minX) minX = x;
                        if (x > maxX) maxX = x;
                        if (y < minY) minY = y;
                        if (y > maxY) maxY = y;

                        // Check edge feathering
                        bool nearBg = false;
                        for (int i = 0; i < 4; i++)
                        {
                            int nx = x + dx[i];
                            int ny = y + dy[i];
                            if (nx >= 0 && nx < w && ny >= 0 && ny < h && isBg[ny * w + nx])
                            {
                                nearBg = true;
                                break;
                            }
                        }

                        Color c = pixels[idx];
                        if (nearBg && c.r > 0.80f && c.g > 0.80f && c.b > 0.80f)
                        {
                            float brightness = (c.r + c.g + c.b) / 3f;
                            float alpha = Mathf.Clamp01((1f - brightness) * 3.5f);
                            outPixels[idx] = new Color(c.r, c.g, c.b, alpha);
                        }
                        else
                        {
                            outPixels[idx] = new Color(c.r, c.g, c.b, 1f);
                        }
                    }
                }
            }

            cutoutTex.SetPixels(outPixels);
            cutoutTex.Apply();

            // 3. Save Artwork PNG
            string charFolder = $"Assets/Clashura/Art/Characters/{charName}";
            EnsureDirectory(charFolder);

            string artPath = $"{charFolder}/Artwork_{charName}.png";
            SaveAndImportSprite(artPath, cutoutTex);

            // 4. Generate Portrait (Auto-crop head & face)
            int bodyH = Mathf.Max(maxY - minY, 10);
            int headCenterX = (minX + maxX) / 2;
            int headCenterY = Mathf.Clamp((int)(minY + bodyH * 0.82f), 0, h - 1);
            int portraitBoxSize = Mathf.Clamp((int)(bodyH * 0.35f), 128, Mathf.Min(w, h));

            Texture2D portraitTex = CropSquare(cutoutTex, headCenterX, headCenterY, portraitBoxSize, 128);
            string portraitPath = $"{charFolder}/Portrait_{charName}.png";
            SaveAndImportSprite(portraitPath, portraitTex);

            // 5. Generate Gameplay Sprite (64 x 96 downscale)
            Texture2D spriteTex = DownscaleSprite(cutoutTex, minX, maxX, minY, maxY, 64, 96);
            string spritePath = $"{charFolder}/Sprite_{charName}.png";
            SaveAndImportSprite(spritePath, spriteTex);

            // 6. Connect to CharacterDataSO
            string soPath = $"Assets/Clashura/Data/Characters/ScriptableObjects/Character_{charName}.asset";
            var charSO = AssetDatabase.LoadAssetAtPath<CharacterDataSO>(soPath);
            if (charSO != null)
            {
                var artSprite = AssetDatabase.LoadAssetAtPath<Sprite>(artPath);
                var portraitSprite = AssetDatabase.LoadAssetAtPath<Sprite>(portraitPath);
                var charSprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);

                var soObj = new SerializedObject(charSO);
                soObj.FindProperty("selectionArtwork").objectReferenceValue = artSprite;
                soObj.FindProperty("portrait").objectReferenceValue = portraitSprite;
                soObj.FindProperty("characterSprite").objectReferenceValue = charSprite;
                soObj.ApplyModifiedProperties();
                EditorUtility.SetDirty(charSO);
            }

            // 7. Update Character2D Prefab
            string prefabPath = $"Assets/Clashura/Prefabs/Characters/Character2D_{charName}.prefab";
            var prefabObj = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefabObj != null)
            {
                var sr = prefabObj.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
                    EditorUtility.SetDirty(prefabObj);
                }
            }

            Debug.Log($"[ClashuraCutoutProcessor] Successfully processed transparent art for {charName} ({charId})!");
            return true;
        }

        private static Texture2D CropSquare(Texture2D src, int cx, int cy, int size, int targetSize)
        {
            int half = size / 2;
            int x0 = Mathf.Clamp(cx - half, 0, src.width - size);
            int y0 = Mathf.Clamp(cy - half, 0, src.height - size);

            Texture2D crop = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color[] cPixels = src.GetPixels(x0, y0, size, size);
            crop.SetPixels(cPixels);
            crop.Apply();

            // Resample to targetSize
            Texture2D res = new Texture2D(targetSize, targetSize, TextureFormat.RGBA32, false);
            Color[] rPixels = new Color[targetSize * targetSize];

            for (int y = 0; y < targetSize; y++)
            {
                float v = (float)y / targetSize;
                for (int x = 0; x < targetSize; x++)
                {
                    float u = (float)x / targetSize;
                    rPixels[y * targetSize + x] = crop.GetPixelBilinear(u, v);
                }
            }

            res.SetPixels(rPixels);
            res.Apply();
            return res;
        }

        private static Texture2D DownscaleSprite(Texture2D src, int minX, int maxX, int minY, int maxY, int targetW, int targetH)
        {
            int charW = Mathf.Max(maxX - minX, 1);
            int charH = Mathf.Max(maxY - minY, 1);

            Texture2D res = new Texture2D(targetW, targetH, TextureFormat.RGBA32, false);
            Color[] rPixels = new Color[targetW * targetH];

            for (int i = 0; i < rPixels.Length; i++) rPixels[i] = Color.clear;

            // Fit character into targetW x targetH maintaining aspect ratio with 2px padding
            float scale = Mathf.Min((float)(targetW - 6) / charW, (float)(targetH - 6) / charH);
            int destW = (int)(charW * scale);
            int destH = (int)(charH * scale);
            int offsetX = (targetW - destW) / 2;
            int offsetY = 3;

            for (int dy = 0; dy < destH; dy++)
            {
                float v = (float)dy / destH;
                int py = offsetY + dy;
                if (py < 0 || py >= targetH) continue;

                for (int dx = 0; dx < destW; dx++)
                {
                    float u = (float)dx / destW;
                    int px = offsetX + dx;
                    if (px < 0 || px >= targetW) continue;

                    float srcU = (minX + u * charW) / src.width;
                    float srcV = (minY + v * charH) / src.height;
                    rPixels[py * targetW + px] = src.GetPixelBilinear(srcU, srcV);
                }
            }

            res.SetPixels(rPixels);
            res.Apply();
            return res;
        }

        private static void EnsureDirectory(string folder)
        {
            string fullPath = Path.Combine(Application.dataPath, folder.Substring("Assets/".Length));
            if (!Directory.Exists(fullPath)) Directory.CreateDirectory(fullPath);
        }

        private static void SaveAndImportSprite(string assetPath, Texture2D tex)
        {
            byte[] bytes = tex.EncodeToPNG();
            string fullPath = Path.Combine(Application.dataPath, assetPath.Substring("Assets/".Length));
            string dir = Path.GetDirectoryName(fullPath);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            File.WriteAllBytes(fullPath, bytes);
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);

            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.alphaIsTransparency = true;
                importer.spritePixelsPerUnit = 100;
                importer.SaveAndReimport();
            }
        }
    }
}
#endif
