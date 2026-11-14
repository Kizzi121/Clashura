using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Clashura.Characters.Data;

namespace Clashura.Utilities
{
    public static class ClashuraDesktopCharacterProcessor
    {
        private class DesktopCharDef
        {
            public string charId;
            public string charName;
            public string rawPngFile;
            public bool removeEnclosedWhite;
            public float headOffsetPct;
            public string title;
            public string bio;

            public DesktopCharDef(string charId, string charName, string rawPngFile, bool removeEnclosedWhite, float headOffsetPct, string title, string bio)
            {
                this.charId = charId;
                this.charName = charName;
                this.rawPngFile = rawPngFile;
                this.removeEnclosedWhite = removeEnclosedWhite;
                this.headOffsetPct = headOffsetPct;
                this.title = title;
                this.bio = bio;
            }
        }

        private static readonly DesktopCharDef[] Roster = new DesktopCharDef[]
        {
            new DesktopCharDef("clashura_char_01", "Aethelgard", "WhatsApp Image 2026-11-14 at 9.00.06 PM.png", false, 0.84f,
                "The Ironclad Thane [Armored Dwarf Paladin]",
                "A stout red-bearded dwarf warrior in horned helm and heavy chainmail, wielding a colossal stone hammer."),

            new DesktopCharDef("clashura_char_02", "Valkira", "WhatsApp Image 2026-11-14 at 8.59.25 PM.png", false, 0.85f,
                "The Lupine Corsair [Dire Wolf Vanguard]",
                "A towering anthropomorphic grey wolf warrior clad in a fur-collared pirate coat and dark leather armor."),

            new DesktopCharDef("clashura_char_03", "Zephyrion", "WhatsApp Image 2026-11-14 at 8.59.25 PM (1).png", false, 0.84f,
                "The Crimson Buccaneer [Rogue Captain]",
                "A dashing red-haired pirate captain with a feathered tricorn hat and twin dueling rapiers."),

            new DesktopCharDef("clashura_char_04", "Nyxara", "WhatsApp Image 2026-11-14 at 8.54.15 PM.png", true, 0.85f,
                "The Shadow Sovereign [Void Demoness]",
                "A hooded assassin cloaked in ethereal purple shadows, wielding twin void daggers and dark flames."),

            new DesktopCharDef("clashura_char_05", "Brakmor", "WhatsApp Image 2026-11-14 at 8.59.26 PM.png", false, 0.85f,
                "The Iron Paw [Panda Martial Master]",
                "A massive martial artist panda in gold and black robes, channeling inner peace and devastating earth palm strikes."),

            new DesktopCharDef("clashura_char_06", "Lyra Thorne", "WhatsApp Image 2026-11-14 at 8.59.38 PM.png", true, 0.85f,
                "The Verdant Huntress [Woodland Archer]",
                "A green-haired forest huntress wielding a recurve bow and quiver with flawless marksmanship."),

            new DesktopCharDef("clashura_char_07", "Kaelen Voss", "WhatsApp Image 2026-11-14 at 8.59.23 PM.png", false, 0.86f,
                "The Arcane Spellblade [Mana Flame Duelist]",
                "A white-haired prodigy clad in a glowing azure longcoat, wielding arcane sorcery and blue soul flames."),

            new DesktopCharDef("clashura_char_08", "Seraphina", "WhatsApp Image 2026-11-14 at 8.59.24 PM.png", true, 0.85f,
                "The Emerald Dervish [Agile Martial Rogue]",
                "A ponytail-sporting green martial artist with agile footwork, razor-sharp kicks, and dual wrist blades."),

            new DesktopCharDef("clashura_char_09", "Morvath", "WhatsApp Image 2026-11-14 at 8.59.26 PM (1).png", false, 0.84f,
                "The Hex Alchemist [Steampunk Artificer]",
                "A brilliant steampunk scholar with brass goggles and potion bandolier, throwing explosive alchemical flasks."),

            new DesktopCharDef("clashura_char_10", "Ignis Rex", "WhatsApp Image 2026-11-14 at 8.53.53 PM.png", true, 0.87f,
                "The Crimson Flame [Street Brawler]",
                "A spirited young martial fighter clad in a crimson hooded trench coat and combat gloves, channeling inner fire."),

            new DesktopCharDef("clashura_char_11", "Selene Mist", "WhatsApp Image 2026-11-14 at 8.55.33 PM.png", false, 0.85f,
                "The Lunar Kitsune [Celestial Nine-Tails]",
                "A mysterious fox spirit maiden in indigo robes with pointed ears and majestic tails, wielding spirit fire."),

            new DesktopCharDef("clashura_char_12", "Gorgar", "WhatsApp Image 2026-11-14 at 8.59.24 PM (1).png", true, 0.84f,
                "The Draconic Berserker [Dragonkin Warrior]",
                "A fearsome crimson-scaled reptilian warrior with a jagged horn, muscular build, and scorched bronze mail."),

            new DesktopCharDef("clashura_char_13", "Astrid Storm", "WhatsApp Image 2026-11-14 at 9.00.20 PM.png", true, 0.87f,
                "The Cyber Neko [Neon Android Striker]",
                "A high-speed cybernetic android girl in a black and neon-pink armored bodysuit with digital cat ears."),

            new DesktopCharDef("clashura_char_14", "Darius Hex", "WhatsApp Image 2026-11-14 at 8.59.28 PM.png", true, 0.85f,
                "The Molten Dreadnought [Hellfire Dark Knight]",
                "A dark warrior encased in charred black armor pulsing with molten lava cracks and glowing red eyes."),

            new DesktopCharDef("clashura_char_15", "Ronin Jin", "WhatsApp Image 2026-11-14 at 8.55.58 PM.png", false, 0.86f,
                "The Wandering Monk [Bare-Knuckle Asura]",
                "A battle-hardened bare-chested brawler with giant prayer beads and martial wraps, fighting for honor."),

            new DesktopCharDef("clashura_char_16", "Tahlia Frost", "WhatsApp Image 2026-11-14 at 8.59.27 PM (1).png", false, 0.85f,
                "The Glacial Sovereign [Frost Maiden]",
                "A majestic ice queen in a crystalline gown with floating frost shards, freezing foes in their tracks."),

            new DesktopCharDef("clashura_char_17", "Orion Sol", "WhatsApp Image 2026-11-14 at 8.59.50 PM.png", false, 0.85f,
                "The Celestial Sage [Taoist Elder]",
                "A wise celestial master with a flowing white beard and blue robes, carrying an enchanted gourd flask."),

            new DesktopCharDef("clashura_char_18", "Vesper", "WhatsApp Image 2026-11-14 at 8.59.27 PM.png", true, 0.84f,
                "The Primordial Behemoth [Gargoyle Titan]",
                "A massive horned beast brute with purple fur, heavy chains, and overwhelming primordial power.")
        };

        [MenuItem("Clashura/Process Desktop Character Pictures (Remove White BG)")]
        public static void ProcessAllDesktopCharacters()
        {
            string rawDir = @"C:\Users\User\Desktop\characters\raw_png";
            if (!Directory.Exists(rawDir))
            {
                Debug.LogError($"[Clashura] Directory not found: {rawDir}");
                return;
            }

            Debug.Log($"[Clashura] Starting high-res processing of 18 desktop character images from: {rawDir}");

            int processedCount = 0;
            foreach (var def in Roster)
            {
                string filePath = Path.Combine(rawDir, def.rawPngFile);
                if (!File.Exists(filePath))
                {
                    Debug.LogWarning($"[Clashura] File missing for {def.charName}: {filePath}");
                    continue;
                }

                ProcessSingleCharacter(def, filePath);
                processedCount++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // Re-bind to database and prefabs
            RebindAllCharacters();

            Debug.Log($"<color=green>[Clashura] Successfully processed and rebound {processedCount} / 18 characters with transparent cutouts!</color>");
        }

        private static void ProcessSingleCharacter(DesktopCharDef def, string filePath)
        {
            byte[] fileBytes = File.ReadAllBytes(filePath);
            Texture2D srcTex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            srcTex.LoadImage(fileBytes);

            int w = srcTex.width;
            int h = srcTex.height;
            Color[] srcPixels = srcTex.GetPixels();

            // Sample background reference from corners
            Color bg0 = srcPixels[0];
            Color bg1 = srcPixels[w - 1];
            Color bg2 = srcPixels[(h - 1) * w];
            Color bg3 = srcPixels[(h - 1) * w + (w - 1)];

            float bgR = (bg0.r + bg1.r + bg2.r + bg3.r) * 0.25f;
            float bgG = (bg0.g + bg1.g + bg2.g + bg3.g) * 0.25f;
            float bgB = (bg0.b + bg1.b + bg2.b + bg3.b) * 0.25f;

            bool IsWhiteBackground(Color c)
            {
                float brightness = (c.r + c.g + c.b) / 3f;
                float diffR = Mathf.Abs(c.r - bgR);
                float diffG = Mathf.Abs(c.g - bgG);
                float diffB = Mathf.Abs(c.b - bgB);
                float maxDiff = Mathf.Max(diffR, Mathf.Max(diffG, diffB));

                return (brightness > 0.90f && maxDiff < 0.13f) || (c.r > 0.94f && c.g > 0.94f && c.b > 0.94f);
            }

            // 1. Flood-fill from outer 4 perimeter borders
            bool[,] isBg = new bool[w, h];
            Queue<int> queue = new Queue<int>();

            void TryPush(int x, int y)
            {
                if (x < 0 || x >= w || y < 0 || y >= h) return;
                if (isBg[x, y]) return;
                if (!IsWhiteBackground(srcPixels[y * w + x])) return;
                isBg[x, y] = true;
                queue.Enqueue(y * w + x);
            }

            // Seed 4 borders
            for (int x = 0; x < w; x++) { TryPush(x, 0); TryPush(x, h - 1); }
            for (int y = 0; y < h; y++) { TryPush(0, y); TryPush(w - 1, y); }

            int[] dx = { 1, -1, 0, 0 };
            int[] dy = { 0, 0, 1, -1 };

            while (queue.Count > 0)
            {
                int curr = queue.Dequeue();
                int cx = curr % w;
                int cy = curr / w;
                for (int d = 0; d < 4; d++)
                {
                    TryPush(cx + dx[d], cy + dy[d]);
                }
            }

            // 2. For characters without white clothing (Group B), remove enclosed white background holes
            if (def.removeEnclosedWhite)
            {
                for (int y = 0; y < h; y++)
                {
                    for (int x = 0; x < w; x++)
                    {
                        if (!isBg[x, y] && IsWhiteBackground(srcPixels[y * w + x]))
                        {
                            isBg[x, y] = true;
                        }
                    }
                }
            }

            // 3. Find bounding box of character
            int minX = w, maxX = 0, minY = h, maxY = 0;
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    if (!isBg[x, y])
                    {
                        if (x < minX) minX = x;
                        if (x > maxX) maxX = x;
                        if (y < minY) minY = y;
                        if (y > maxY) maxY = y;
                    }
                }
            }

            // Safety check
            if (minX > maxX || minY > maxY)
            {
                Debug.LogError($"[Clashura] Failed to find character bounds for {def.charName}!");
                return;
            }

            // Add margin
            int pad = 20;
            minX = Mathf.Max(0, minX - pad);
            maxX = Mathf.Min(w - 1, maxX + pad);
            minY = Mathf.Max(0, minY - pad);
            maxY = Mathf.Min(h - 1, maxY + pad);

            int cropW = maxX - minX + 1;
            int cropH = maxY - minY + 1;

            Texture2D cutoutTex = new Texture2D(cropW, cropH, TextureFormat.RGBA32, false);
            Color[] outPixels = new Color[cropW * cropH];

            int[] adjDx = { 1, -1, 0, 0, 1, -1, 1, -1 };
            int[] adjDy = { 0, 0, 1, -1, 1, 1, -1, -1 };

            for (int cy = 0; cy < cropH; cy++)
            {
                int sy = minY + cy;
                for (int cx = 0; cx < cropW; cx++)
                {
                    int sx = minX + cx;
                    int sIdx = sy * w + sx;
                    int oIdx = cy * cropW + cx;

                    if (isBg[sx, sy])
                    {
                        outPixels[oIdx] = Color.clear;
                    }
                    else
                    {
                        Color c = srcPixels[sIdx];

                        // Antialiasing and defringing on perimeter
                        bool adjBg = false;
                        for (int d = 0; d < 8; d++)
                        {
                            int nx = sx + adjDx[d];
                            int ny = sy + adjDy[d];
                            if (nx >= 0 && nx < w && ny >= 0 && ny < h && isBg[nx, ny])
                            {
                                adjBg = true;
                                break;
                            }
                        }

                        if (adjBg)
                        {
                            float lum = (c.r + c.g + c.b) / 3f;
                            float alpha = 1f;
                            if (lum > 0.82f)
                            {
                                alpha = Mathf.Clamp01((0.96f - lum) / (0.96f - 0.82f));
                            }
                            float invA = 1f - alpha;
                            float nr = Mathf.Clamp01((c.r - invA * bgR) / Mathf.Max(alpha, 0.01f));
                            float ng = Mathf.Clamp01((c.g - invA * bgG) / Mathf.Max(alpha, 0.01f));
                            float nb = Mathf.Clamp01((c.b - invA * bgB) / Mathf.Max(alpha, 0.01f));
                            outPixels[oIdx] = new Color(nr, ng, nb, alpha);
                        }
                        else
                        {
                            outPixels[oIdx] = new Color(c.r, c.g, c.b, 1f);
                        }
                    }
                }
            }

            cutoutTex.SetPixels(outPixels);
            cutoutTex.Apply();

            // 4. Save to character directory
            string charFolder = $"Assets/Clashura/Art/Characters/{def.charName}";
            EnsureDirectory(charFolder);

            string artPath = $"{charFolder}/Artwork_{def.charName}.png";
            SaveAndImportSprite(artPath, cutoutTex);

            // 5. Generate Portrait (128x128 head/face crop)
            int headCenterX = cropW / 2;
            int headCenterY = Mathf.Clamp((int)(cropH * def.headOffsetPct), 0, cropH - 1);
            int portraitBoxSize = Mathf.Clamp((int)(cropH * 0.32f), 45, Mathf.Min(cropW, cropH));

            Texture2D portraitTex = CropSquare(cutoutTex, headCenterX, headCenterY, portraitBoxSize, 128);
            string portraitPath = $"{charFolder}/Portrait_{def.charName}.png";
            SaveAndImportSprite(portraitPath, portraitTex);

            // 6. Generate In-Game 2D Sprite (64x96 downscale)
            Texture2D spriteTex = DownscaleSprite(cutoutTex, 64, 96);
            string spritePath = $"{charFolder}/Sprite_{def.charName}.png";
            SaveAndImportSprite(spritePath, spriteTex);

            // Clean up temporary textures from memory
            UnityEngine.Object.DestroyImmediate(srcTex);
            UnityEngine.Object.DestroyImmediate(cutoutTex);
            UnityEngine.Object.DestroyImmediate(portraitTex);
            UnityEngine.Object.DestroyImmediate(spriteTex);
        }

        private static Texture2D CropSquare(Texture2D src, int centerX, int centerY, int boxSize, int targetSize)
        {
            int half = boxSize / 2;
            int x0 = Mathf.Clamp(centerX - half, 0, src.width - boxSize);
            int y0 = Mathf.Clamp(centerY - half, 0, src.height - boxSize);
            x0 = Mathf.Max(0, x0);
            y0 = Mathf.Max(0, y0);

            int actualW = Mathf.Min(boxSize, src.width - x0);
            int actualH = Mathf.Min(boxSize, src.height - y0);

            Texture2D crop = new Texture2D(actualW, actualH, TextureFormat.RGBA32, false);
            Color[] cPixels = src.GetPixels(x0, y0, actualW, actualH);
            crop.SetPixels(cPixels);
            crop.Apply();

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
            UnityEngine.Object.DestroyImmediate(crop);
            return res;
        }

        private static Texture2D DownscaleSprite(Texture2D src, int targetW, int targetH)
        {
            Texture2D res = new Texture2D(targetW, targetH, TextureFormat.RGBA32, false);
            Color[] rPixels = new Color[targetW * targetH];
            for (int i = 0; i < rPixels.Length; i++) rPixels[i] = Color.clear;

            float scale = Mathf.Min((float)(targetW - 6) / src.width, (float)(targetH - 6) / src.height);
            int destW = (int)(src.width * scale);
            int destH = (int)(src.height * scale);
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

                    rPixels[py * targetW + px] = src.GetPixelBilinear(u, v);
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
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.spritePixelsPerUnit = 100;
                importer.mipmapEnabled = false;
                importer.SaveAndReimport();
            }
        }

        public static void RebindAllCharacters()
        {
            foreach (var def in Roster)
            {
                string charFolder = $"Assets/Clashura/Art/Characters/{def.charName}";
                string artPath = $"{charFolder}/Artwork_{def.charName}.png";
                string portraitPath = $"{charFolder}/Portrait_{def.charName}.png";
                string spritePath = $"{charFolder}/Sprite_{def.charName}.png";

                var artSprite = AssetDatabase.LoadAssetAtPath<Sprite>(artPath);
                var portSprite = AssetDatabase.LoadAssetAtPath<Sprite>(portraitPath);
                var charSprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);

                string soPath = $"Assets/Clashura/Data/Characters/ScriptableObjects/Character_{def.charName}.asset";
                var charSO = AssetDatabase.LoadAssetAtPath<CharacterDataSO>(soPath);
                if (charSO != null)
                {
                    var soObj = new SerializedObject(charSO);
                    soObj.FindProperty("characterTitle").stringValue = def.title;
                    soObj.FindProperty("bio").stringValue = def.bio;
                    soObj.FindProperty("selectionArtwork").objectReferenceValue = artSprite;
                    soObj.FindProperty("portrait").objectReferenceValue = portSprite;
                    soObj.FindProperty("characterSprite").objectReferenceValue = charSprite;
                    soObj.ApplyModifiedProperties();
                    EditorUtility.SetDirty(charSO);
                }

                // Update 2D prefab
                string prefabPath = $"Assets/Clashura/Prefabs/Characters/Character2D_{def.charName}.prefab";
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                if (prefab != null)
                {
                    var sr = prefab.GetComponent<SpriteRenderer>();
                    if (sr != null && charSprite != null)
                    {
                        var pObj = new SerializedObject(sr);
                        pObj.FindProperty("m_Sprite").objectReferenceValue = charSprite;
                        pObj.ApplyModifiedProperties();
                        EditorUtility.SetDirty(prefab);
                    }
                }
            }

            AssetDatabase.SaveAssets();
            Debug.Log("[Clashura] Rebind complete across all 18 CharacterDataSO and Character2D prefabs.");
        }
    }
}
