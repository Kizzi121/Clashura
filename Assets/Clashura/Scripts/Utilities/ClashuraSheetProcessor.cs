#if UNITY_EDITOR
using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Clashura.Characters.Data;
using Clashura.Characters.Managers;

namespace Clashura.Utilities
{
    /// <summary>
    /// Processes the master 18-character sprite lineup sheet uploaded by the user:
    /// - Extracts all 18 characters from the 2x9 grid.
    /// - Removes the solid black background with soft edge feathering for pure alpha transparency.
    /// - Generates full-body Artwork, auto-cropped head Portrait, and 64x96 in-game 2D Gameplay Sprite.
    /// - Places all assets into their respective Assets/Clashura/Art/Characters/<Name>/ folders.
    /// - Rebinds CharacterDataSO assets and Character2D prefabs.
    /// </summary>
    public static class ClashuraSheetProcessor
    {
        private struct CharSlotDef
        {
            public int index;
            public string charName;
            public string charId;
            public string title;
            public string bio;
            public int minX;
            public int maxX;
            public int minY;
            public int maxY;
            public float headOffsetPct; // from bottom of char box (0.75 - 0.90)
        }

        private static readonly CharSlotDef[] Slots = new CharSlotDef[]
        {
            // === TOP ROW (y: 288..575) ===
            new CharSlotDef {
                index = 1, charName = "Ignis Rex", charId = "clashura_char_10",
                title = "The Crimson Brawler [Fire Striker]",
                bio = "A fiery young striker wielding blazing kinetic martial arts in an open red longcoat.",
                minX = 7, maxX = 120, minY = 296, maxY = 565, headOffsetPct = 0.86f
            },
            new CharSlotDef {
                index = 2, charName = "Selene Mist", charId = "clashura_char_11",
                title = "The Lunar Kitsune [Fox Beast Sorceress]",
                bio = "An elegant kitsune fox sorceress with animal ears and flowing purple cape, channeling lunar ether.",
                minX = 121, maxX = 237, minY = 288, maxY = 565, headOffsetPct = 0.88f
            },
            new CharSlotDef {
                index = 3, charName = "Ronin Jin", charId = "clashura_char_15",
                title = "The Ascetic Master [Bare-Fist Monk]",
                bio = "A disciplined martial arts master bearing prayer beads and orange sash, striking with lightning fists.",
                minX = 238, maxX = 348, minY = 291, maxY = 562, headOffsetPct = 0.87f
            },
            new CharSlotDef {
                index = 4, charName = "Kaelen Voss", charId = "clashura_char_07",
                title = "The Azure Spellblade [Arcane Duelist]",
                bio = "A white-haired prodigy in a dark trenchcoat weaving astral runes with glowing blue mana fire.",
                minX = 349, maxX = 457, minY = 290, maxY = 560, headOffsetPct = 0.86f
            },
            new CharSlotDef {
                index = 5, charName = "Seraphina", charId = "clashura_char_08",
                title = "The Jade Shadow [Agile Martial Rogue]",
                bio = "A swift green-clad martial rogue with ponytail and wind ribbons, weaving through enemy lines.",
                minX = 458, maxX = 554, minY = 298, maxY = 558, headOffsetPct = 0.86f
            },
            new CharSlotDef {
                index = 6, charName = "Gorgar", charId = "clashura_char_12",
                title = "The Crimson Drake [Dragonkin Titan]",
                bio = "A formidable red dragonkin warrior with horned reptilian crest, scaled tail, and desert armor.",
                minX = 555, maxX = 680, minY = 299, maxY = 565, headOffsetPct = 0.84f
            },
            new CharSlotDef {
                index = 7, charName = "Valkira", charId = "clashura_char_02",
                title = "The Corsair Lupine [Wolf Beast-Warrior]",
                bio = "A seasoned grey wolf warrior in a fur-lined naval coat with fangs, leather straps, and bushy tail.",
                minX = 684, maxX = 795, minY = 289, maxY = 566, headOffsetPct = 0.87f
            },
            new CharSlotDef {
                index = 8, charName = "Zephyrion", charId = "clashura_char_03",
                title = "The Crimson Corsair [Wind Duelist]",
                bio = "A dashing red-haired pirate captain in a bicorne hat and scarlet coat, commanding wind and rapier.",
                minX = 797, maxX = 897, minY = 299, maxY = 556, headOffsetPct = 0.85f
            },
            new CharSlotDef {
                index = 9, charName = "Brakmor", charId = "clashura_char_05",
                title = "The Ironfang Panda [Ursine Beast Vanguard]",
                bio = "A colossal martial arts panda titan in yellow and black robes, immovable as a mountain.",
                minX = 898, maxX = 1018, minY = 293, maxY = 552, headOffsetPct = 0.84f
            },

            // === BOTTOM ROW (y: 0..287) ===
            new CharSlotDef {
                index = 10, charName = "Nyxara", charId = "clashura_char_04",
                title = "The Nether Shade [Void Shadow Specialist]",
                bio = "An enigmatic masked shadow assassin in a purple cowl, conjuring dark ethereal purple flames.",
                minX = 7, maxX = 120, minY = 31, maxY = 285, headOffsetPct = 0.87f
            },
            new CharSlotDef {
                index = 11, charName = "Morvath", charId = "clashura_char_09",
                title = "The Grand Alchemist [Potion Specialist]",
                bio = "A brilliant alchemist with goggles and lab pouches, throwing volatile bubbling potion flasks.",
                minX = 121, maxX = 221, minY = 27, maxY = 277, headOffsetPct = 0.85f
            },
            new CharSlotDef {
                index = 12, charName = "Vesper", charId = "clashura_char_18",
                title = "The Abyssal Gargoyle [Shadow Beast Titan]",
                bio = "A massive horned beast brute with purple fur, heavy chains, and overwhelming primordial power.",
                minX = 222, maxX = 349, minY = 25, maxY = 278, headOffsetPct = 0.83f
            },
            new CharSlotDef {
                index = 13, charName = "Tahlia Frost", charId = "clashura_char_16",
                title = "The Glacial Sovereign [Frost Maiden]",
                bio = "A majestic ice queen in a crystalline gown with floating frost shards, freezing foes in their tracks.",
                minX = 350, maxX = 468, minY = 26, maxY = 285, headOffsetPct = 0.86f
            },
            new CharSlotDef {
                index = 14, charName = "Darius Hex", charId = "clashura_char_14",
                title = "The Molten Dreadnought [Hellfire Dark Knight]",
                bio = "A dark warrior encased in charred black armor pulsing with molten lava cracks and glowing red eyes.",
                minX = 469, maxX = 578, minY = 24, maxY = 287, headOffsetPct = 0.86f
            },
            new CharSlotDef {
                index = 15, charName = "Lyra Thorne", charId = "clashura_char_06",
                title = "The Verdant Huntress [Woodland Archer]",
                bio = "A green-haired forest huntress wielding a recurve bow and quiver with flawless marksmanship.",
                minX = 579, maxX = 685, minY = 24, maxY = 282, headOffsetPct = 0.86f
            },
            new CharSlotDef {
                index = 16, charName = "Orion Sol", charId = "clashura_char_17",
                title = "The Celestial Sage [Taoist Elder]",
                bio = "A wise celestial master with a flowing white beard and blue robes, carrying an enchanted gourd flask.",
                minX = 686, maxX = 801, minY = 29, maxY = 284, headOffsetPct = 0.86f
            },
            new CharSlotDef {
                index = 17, charName = "Aethelgard", charId = "clashura_char_01",
                title = "The Ironclad Thane [Armored Dwarf Paladin]",
                bio = "A stout red-bearded dwarf warrior in horned helm and heavy chainmail, wielding a colossal stone hammer.",
                minX = 802, maxX = 921, minY = 33, maxY = 261, headOffsetPct = 0.85f
            },
            new CharSlotDef {
                index = 18, charName = "Astrid Storm", charId = "clashura_char_13",
                title = "The Cyber Neko [Neon Android Striker]",
                bio = "A high-speed cybernetic android girl in a black and neon-pink armored bodysuit with digital cat ears.",
                minX = 925, maxX = 1014, minY = 29, maxY = 284, headOffsetPct = 0.87f
            }
        };

        [MenuItem("Clashura/Extract All 18 Sprites From Sheet")]
        public static void ExtractAllCharacters()
        {
            string sheetPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                @".gemini\antigravity\brain\76bfe5fc-2f35-42db-9203-03da4b0351a9\.user_uploaded\media_1794667079138_f404cf21.jpg"
            );

            if (!File.Exists(sheetPath))
            {
                Debug.LogError($"[ClashuraSheetProcessor] Master sprite sheet not found at: {sheetPath}");
                return;
            }

            byte[] bytes = File.ReadAllBytes(sheetPath);
            Texture2D sheetTex = new Texture2D(2, 2);
            if (!sheetTex.LoadImage(bytes))
            {
                Debug.LogError("[ClashuraSheetProcessor] Failed to decode master sprite sheet texture!");
                return;
            }

            Debug.Log($"[ClashuraSheetProcessor] Successfully loaded sprite sheet: {sheetTex.width}x{sheetTex.height}. Beginning 18-character extraction...");

            for (int i = 0; i < Slots.Length; i++)
            {
                var slot = Slots[i];
                ExtractSingleCharacter(sheetTex, slot);
            }

            // Save and refresh database
            string dbPath = "Assets/Clashura/Data/Characters/ScriptableObjects/CharacterDatabase.asset";
            var db = AssetDatabase.LoadAssetAtPath<CharacterDatabaseSO>(dbPath);
            if (db != null)
            {
                EditorUtility.SetDirty(db);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[ClashuraSheetProcessor] ALL 18 CHARACTERS EXTRACTED AND INSTALLED INTO RESPECTIVE FOLDERS SUCCESSFULLY!");
        }

        private static void ExtractSingleCharacter(Texture2D sheetTex, CharSlotDef slot)
        {
            int minX = slot.minX;
            int maxX = slot.maxX;
            int minY = slot.minY;
            int maxY = slot.maxY;

            int cropW = maxX - minX + 1;
            int cropH = maxY - minY + 1;

            Texture2D charTex = new Texture2D(cropW, cropH, TextureFormat.RGBA32, false);

            bool IsBg(Color c) => (c.r + c.g + c.b) < 0.08f;

            // 1. Flood-fill from outer crop border to detect black background
            bool[] isBg = new bool[cropW * cropH];
            var queue = new Queue<int>();

            void TryEnqueue(int x, int y)
            {
                if (x < 0 || x >= cropW || y < 0 || y >= cropH) return;
                int idx = y * cropW + x;
                if (isBg[idx]) return;
                Color c = sheetTex.GetPixel(minX + x, minY + y);
                if (IsBg(c))
                {
                    isBg[idx] = true;
                    queue.Enqueue(idx);
                }
            }

            for (int x = 0; x < cropW; x++) { TryEnqueue(x, 0); TryEnqueue(x, cropH - 1); }
            for (int y = 0; y < cropH; y++) { TryEnqueue(0, y); TryEnqueue(cropW - 1, y); }

            int[] dx = { 1, -1, 0, 0 };
            int[] dy = { 0, 0, 1, -1 };

            while (queue.Count > 0)
            {
                int curr = queue.Dequeue();
                int cx = curr % cropW;
                int cy = curr / cropW;
                for (int i = 0; i < 4; i++) TryEnqueue(cx + dx[i], cy + dy[i]);
            }

            // 2. Build transparent cutout with soft anti-aliased edge feathering
            Color[] outPixels = new Color[cropW * cropH];
            for (int y = 0; y < cropH; y++)
            {
                for (int x = 0; x < cropW; x++)
                {
                    int idx = y * cropW + x;
                    if (isBg[idx])
                    {
                        outPixels[idx] = Color.clear;
                    }
                    else
                    {
                        Color c = sheetTex.GetPixel(minX + x, minY + y);

                        // Check edge feathering
                        bool nearBg = false;
                        for (int i = 0; i < 4; i++)
                        {
                            int nx = x + dx[i];
                            int ny = y + dy[i];
                            if (nx >= 0 && nx < cropW && ny >= 0 && ny < cropH && isBg[ny * cropW + nx])
                            {
                                nearBg = true;
                                break;
                            }
                        }

                        if (nearBg && (c.r + c.g + c.b) < 0.22f)
                        {
                            float lum = Mathf.Max(c.r, Mathf.Max(c.g, c.b));
                            float alpha = Mathf.Clamp01(lum * 4.2f);
                            outPixels[idx] = new Color(c.r, c.g, c.b, alpha);
                        }
                        else
                        {
                            outPixels[idx] = new Color(c.r, c.g, c.b, 1f);
                        }
                    }
                }
            }

            charTex.SetPixels(outPixels);
            charTex.Apply();

            // 3. Save Artwork_<Name>.png
            string charFolder = $"Assets/Clashura/Art/Characters/{slot.charName}";
            EnsureDirectory(charFolder);

            string artPath = $"{charFolder}/Artwork_{slot.charName}.png";
            SaveAndImportSprite(artPath, charTex);

            // 4. Generate Portrait (128x128 head/face crop)
            int headCenterX = cropW / 2;
            int headCenterY = Mathf.Clamp((int)(cropH * slot.headOffsetPct), 0, cropH - 1);
            int portraitBoxSize = Mathf.Clamp((int)(cropH * 0.35f), 45, Mathf.Min(cropW, cropH));

            Texture2D portraitTex = CropSquare(charTex, headCenterX, headCenterY, portraitBoxSize, 128);
            string portraitPath = $"{charFolder}/Portrait_{slot.charName}.png";
            SaveAndImportSprite(portraitPath, portraitTex);

            // 5. Generate Gameplay Sprite (64x96 downscale)
            Texture2D spriteTex = DownscaleSprite(charTex, 64, 96);
            string spritePath = $"{charFolder}/Sprite_{slot.charName}.png";
            SaveAndImportSprite(spritePath, spriteTex);

            // 6. Update CharacterDataSO
            string soPath = $"Assets/Clashura/Data/Characters/ScriptableObjects/Character_{slot.charName}.asset";
            var charSO = AssetDatabase.LoadAssetAtPath<CharacterDataSO>(soPath);
            if (charSO != null)
            {
                var artSprite = AssetDatabase.LoadAssetAtPath<Sprite>(artPath);
                var portSprite = AssetDatabase.LoadAssetAtPath<Sprite>(portraitPath);
                var charSprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);

                var soObj = new SerializedObject(charSO);
                soObj.FindProperty("characterTitle").stringValue = slot.title;
                soObj.FindProperty("bio").stringValue = slot.bio;
                soObj.FindProperty("selectionArtwork").objectReferenceValue = artSprite;
                soObj.FindProperty("portrait").objectReferenceValue = portSprite;
                soObj.FindProperty("characterSprite").objectReferenceValue = charSprite;
                soObj.ApplyModifiedProperties();
                EditorUtility.SetDirty(charSO);
            }

            // 7. Update Character2D Prefab
            string prefabPath = $"Assets/Clashura/Prefabs/Characters/Character2D_{slot.charName}.prefab";
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

            Debug.Log($"[ClashuraSheetProcessor] Processed #{slot.index} {slot.charName} ({slot.charId}) successfully!");
        }

        private static Texture2D CropSquare(Texture2D src, int cx, int cy, int size, int targetSize)
        {
            int half = size / 2;
            int x0 = Mathf.Clamp(cx - half, 0, Mathf.Max(0, src.width - size));
            int y0 = Mathf.Clamp(cy - half, 0, Mathf.Max(0, src.height - size));
            int actualW = Mathf.Min(size, src.width - x0);
            int actualH = Mathf.Min(size, src.height - y0);

            Texture2D crop = new Texture2D(actualW, actualH, TextureFormat.RGBA32, false);
            Color[] cPixels = src.GetPixels(x0, y0, actualW, actualH);
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
                importer.SaveAndReimport();
            }
        }
    }
}
#endif
