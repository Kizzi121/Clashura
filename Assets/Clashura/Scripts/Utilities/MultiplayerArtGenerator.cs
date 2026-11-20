#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Clashura.Utilities
{
    /// <summary>
    /// Procedural UI Art Generator for CLASHURA Multiplayer & Matchmaking.
    /// Generates high-DPI sprites matching the dark fantasy obsidian & gold visual identity.
    /// </summary>
    public static class MultiplayerArtGenerator
    {
        public const string ArtDir = "Assets/Clashura/Art/UI/Multiplayer";

        [MenuItem("Clashura/Art/Generate Multiplayer UI Assets")]
        public static void GenerateAllMultiplayerArt()
        {
            if (!Directory.Exists(ArtDir))
            {
                Directory.CreateDirectory(ArtDir);
            }

            GenerateCreateRoomIcon();
            GenerateSearchMatchIcon();
            GenerateLobbyFrame();
            GenerateRoomCardFrame();
            GenerateGoldActionButton();
            GenerateCyanActionButton();
            GenerateInputBox();
            GenerateVersusBadge();
            GenerateVersusCardP1();
            GenerateVersusCardP2();
            GenerateVersusBadgeSupreme();
            GenerateVersusGlowBurst();
            GenerateVersusEnergyRays();
            GenerateVersusHeroSlotLarge();
            GenerateVersusDividerBeam();
            GenerateVersusScreenFlash();
            GenerateVersusPill();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("<color=green>[MultiplayerArtGenerator] Successfully generated all Multiplayer UI assets!</color>");
        }

        private static void SaveSprite(string path, Texture2D tex, Vector4? border = null)
        {
            if (tex == null) return;
            byte[] bytes = tex.EncodeToPNG();
            File.WriteAllBytes(path, bytes);

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                if (border.HasValue)
                {
                    importer.spriteBorder = border.Value;
                }
                importer.SaveAndReimport();
            }
        }

        #region Icons

        private static void GenerateCreateRoomIcon()
        {
            int size = 256;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color[] colors = new Color[size * size];

            Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
            float radius = size * 0.46f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int idx = y * size + x;
                    Vector2 p = new Vector2(x, y);
                    float dist = Vector2.Distance(p, center);

                    if (dist > radius)
                    {
                        colors[idx] = Color.clear;
                        continue;
                    }

                    // Circular disc with dark obsidian gradient and gold trim
                    float normDist = dist / radius;
                    Color baseCol;
                    if (normDist > 0.90f)
                    {
                        // Outer gold rim
                        baseCol = new Color(1.0f, 0.85f, 0.35f, 1f);
                    }
                    else if (normDist > 0.84f)
                    {
                        // Inner metallic shadow
                        baseCol = new Color(0.18f, 0.14f, 0.08f, 1f);
                    }
                    else
                    {
                        // Center deep radial fill
                        float grad = 1f - normDist;
                        baseCol = Color.Lerp(new Color(0.06f, 0.08f, 0.14f, 1f), new Color(0.15f, 0.18f, 0.28f, 1f), grad);
                    }

                    // Draw Crossed Swords & Shield motif
                    float dx = Mathf.Abs(x - center.x);
                    float dy = Mathf.Abs(y - center.y);

                    // Shield silhouette
                    bool insideShield = (dx < 42f && y >= center.y - 48f && y <= center.y + 40f) &&
                                        (y > center.y - 10f || dx < 42f - (center.y - 10f - y) * 0.9f);

                    // Sword 1 (Diagonal /)
                    float d1 = Mathf.Abs((x - center.x) - (y - center.y));
                    bool sword1 = d1 < 5.5f && dist < radius * 0.72f;

                    // Sword 2 (Diagonal \)
                    float d2 = Mathf.Abs((x - center.x) + (y - center.y));
                    bool sword2 = d2 < 5.5f && dist < radius * 0.72f;

                    if (sword1 || sword2)
                    {
                        baseCol = Color.Lerp(baseCol, new Color(1.0f, 0.92f, 0.65f, 1f), 0.9f);
                    }
                    else if (insideShield)
                    {
                        baseCol = Color.Lerp(baseCol, new Color(0.85f, 0.68f, 0.22f, 1f), 0.85f);
                    }

                    colors[idx] = baseCol;
                }
            }

            tex.SetPixels(colors);
            tex.Apply();
            SaveSprite($"{ArtDir}/Icon_Create_Room.png", tex);
        }

        private static void GenerateSearchMatchIcon()
        {
            int size = 256;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color[] colors = new Color[size * size];

            Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
            float radius = size * 0.46f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int idx = y * size + x;
                    Vector2 p = new Vector2(x, y);
                    float dist = Vector2.Distance(p, center);

                    if (dist > radius)
                    {
                        colors[idx] = Color.clear;
                        continue;
                    }

                    // Disc with cyan radar aesthetics
                    float normDist = dist / radius;
                    Color baseCol;
                    if (normDist > 0.90f)
                    {
                        baseCol = new Color(0.35f, 0.85f, 1.0f, 1f); // Cyan rim
                    }
                    else if (normDist > 0.84f)
                    {
                        baseCol = new Color(0.04f, 0.16f, 0.24f, 1f);
                    }
                    else
                    {
                        float grad = 1f - normDist;
                        baseCol = Color.Lerp(new Color(0.04f, 0.08f, 0.15f, 1f), new Color(0.08f, 0.22f, 0.35f, 1f), grad);
                    }

                    // Concentric scanning rings (3 rings)
                    bool ring1 = Mathf.Abs(dist - radius * 0.30f) < 2.5f;
                    bool ring2 = Mathf.Abs(dist - radius * 0.55f) < 2.5f;
                    bool ring3 = Mathf.Abs(dist - radius * 0.78f) < 2.5f;

                    // Crosshairs
                    bool crossH = Mathf.Abs(y - center.y) < 2.0f && dist < radius * 0.82f;
                    bool crossV = Mathf.Abs(x - center.x) < 2.0f && dist < radius * 0.82f;

                    // Radar sweep quadrant glow
                    float angle = Mathf.Atan2(y - center.y, x - center.x) * Mathf.Rad2Deg;
                    if (angle < 0) angle += 360f;
                    bool sweep = angle >= 30f && angle <= 90f && dist < radius * 0.78f;

                    if (ring1 || ring2 || ring3 || crossH || crossV)
                    {
                        baseCol = Color.Lerp(baseCol, new Color(0.45f, 0.92f, 1.0f, 1f), 0.85f);
                    }
                    else if (sweep)
                    {
                        float sweepFactor = (angle - 30f) / 60f;
                        baseCol = Color.Lerp(baseCol, new Color(0.20f, 0.75f, 0.95f, 1f), sweepFactor * 0.5f);
                    }

                    // Glowing center blip
                    if (dist < 8.0f)
                    {
                        baseCol = new Color(0.9f, 1.0f, 1.0f, 1f);
                    }

                    colors[idx] = baseCol;
                }
            }

            tex.SetPixels(colors);
            tex.Apply();
            SaveSprite($"{ArtDir}/Icon_Join_Search.png", tex);
        }

        private static void GenerateVersusBadge()
        {
            int size = 128;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color[] colors = new Color[size * size];
            Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
            float radius = size * 0.46f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int idx = y * size + x;
                    Vector2 p = new Vector2(x, y);
                    float dist = Vector2.Distance(p, center);

                    if (dist > radius)
                    {
                        colors[idx] = Color.clear;
                        continue;
                    }

                    float norm = dist / radius;
                    Color c;
                    if (norm > 0.88f)
                    {
                        c = new Color(1.0f, 0.85f, 0.35f, 1f);
                    }
                    else if (norm > 0.80f)
                    {
                        c = new Color(0.20f, 0.16f, 0.08f, 1f);
                    }
                    else
                    {
                        c = Color.Lerp(new Color(0.08f, 0.10f, 0.18f, 1f), new Color(0.18f, 0.22f, 0.32f, 1f), 1f - norm);
                    }

                    // VS letters / center spark
                    if (dist < 18f)
                    {
                        c = Color.Lerp(c, new Color(1.0f, 0.90f, 0.45f, 1f), 0.9f);
                    }

                    colors[idx] = c;
                }
            }

            tex.SetPixels(colors);
            tex.Apply();
            SaveSprite($"{ArtDir}/UI_Versus_Badge.png", tex);
        }

        #endregion

        #region Frames and Buttons

        private static void GenerateLobbyFrame()
        {
            int w = 128;
            int h = 128;
            int border = 24;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color[] colors = new Color[w * h];

            Color borderGold = new Color(1.0f, 0.85f, 0.35f, 1f);
            Color innerShadow = new Color(0.18f, 0.14f, 0.08f, 0.9f);
            Color bodyFill = new Color(0.06f, 0.08f, 0.13f, 0.95f);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    int minEdge = Mathf.Min(Mathf.Min(x, w - 1 - x), Mathf.Min(y, h - 1 - y));
                    Color c;
                    if (minEdge <= 2)
                    {
                        c = borderGold;
                    }
                    else if (minEdge <= 5)
                    {
                        c = innerShadow;
                    }
                    else if (minEdge <= 8)
                    {
                        c = new Color(0.35f, 0.45f, 0.65f, 0.8f);
                    }
                    else
                    {
                        c = bodyFill;
                    }
                    colors[y * w + x] = c;
                }
            }

            tex.SetPixels(colors);
            tex.Apply();
            SaveSprite($"{ArtDir}/UI_Multiplayer_Lobby_Frame.png", tex, new Vector4(border, border, border, border));
        }

        private static void GenerateRoomCardFrame()
        {
            int w = 128;
            int h = 128;
            int border = 18;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color[] colors = new Color[w * h];

            Color borderGlow = new Color(0.35f, 0.50f, 0.75f, 0.9f);
            Color bodyFill = new Color(0.08f, 0.11f, 0.18f, 0.95f);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    int minEdge = Mathf.Min(Mathf.Min(x, w - 1 - x), Mathf.Min(y, h - 1 - y));
                    Color c;
                    if (minEdge <= 2)
                    {
                        c = borderGlow;
                    }
                    else if (minEdge <= 4)
                    {
                        c = new Color(0.05f, 0.07f, 0.12f, 1f);
                    }
                    else
                    {
                        c = bodyFill;
                    }
                    colors[y * w + x] = c;
                }
            }

            tex.SetPixels(colors);
            tex.Apply();
            SaveSprite($"{ArtDir}/UI_Room_Action_Card.png", tex, new Vector4(border, border, border, border));
        }

        private static void GenerateGoldActionButton()
        {
            int w = 128;
            int h = 64;
            int border = 14;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color[] colors = new Color[w * h];

            for (int y = 0; y < h; y++)
            {
                float t = (float)y / h;
                Color grad = Color.Lerp(new Color(0.85f, 0.60f, 0.10f, 1f), new Color(1.0f, 0.88f, 0.40f, 1f), t);

                for (int x = 0; x < w; x++)
                {
                    int minEdge = Mathf.Min(Mathf.Min(x, w - 1 - x), Mathf.Min(y, h - 1 - y));
                    Color c;
                    if (minEdge <= 1)
                    {
                        c = new Color(1.0f, 0.95f, 0.70f, 1f); // Highlight edge
                    }
                    else if (minEdge <= 3)
                    {
                        c = new Color(0.70f, 0.45f, 0.05f, 1f); // Shadow edge
                    }
                    else
                    {
                        c = grad;
                    }
                    colors[y * w + x] = c;
                }
            }

            tex.SetPixels(colors);
            tex.Apply();
            SaveSprite($"{ArtDir}/UI_Button_Gold_Action.png", tex, new Vector4(border, border, border, border));
        }

        private static void GenerateCyanActionButton()
        {
            int w = 128;
            int h = 64;
            int border = 14;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color[] colors = new Color[w * h];

            for (int y = 0; y < h; y++)
            {
                float t = (float)y / h;
                Color grad = Color.Lerp(new Color(0.12f, 0.55f, 0.80f, 1f), new Color(0.40f, 0.85f, 1.0f, 1f), t);

                for (int x = 0; x < w; x++)
                {
                    int minEdge = Mathf.Min(Mathf.Min(x, w - 1 - x), Mathf.Min(y, h - 1 - y));
                    Color c;
                    if (minEdge <= 1)
                    {
                        c = new Color(0.85f, 0.98f, 1.0f, 1f);
                    }
                    else if (minEdge <= 3)
                    {
                        c = new Color(0.06f, 0.30f, 0.48f, 1f);
                    }
                    else
                    {
                        c = grad;
                    }
                    colors[y * w + x] = c;
                }
            }

            tex.SetPixels(colors);
            tex.Apply();
            SaveSprite($"{ArtDir}/UI_Button_Cyan_Action.png", tex, new Vector4(border, border, border, border));
        }

        private static void GenerateInputBox()
        {
            int w = 128;
            int h = 64;
            int border = 12;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color[] colors = new Color[w * h];

            Color borderLine = new Color(0.45f, 0.55f, 0.75f, 0.8f);
            Color fill = new Color(0.05f, 0.08f, 0.14f, 0.96f);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    int minEdge = Mathf.Min(Mathf.Min(x, w - 1 - x), Mathf.Min(y, h - 1 - y));
                    Color c;
                    if (minEdge <= 1)
                    {
                        c = borderLine;
                    }
                    else
                    {
                        c = fill;
                    }
                    colors[y * w + x] = c;
                }
            }

            tex.SetPixels(colors);
            tex.Apply();
            SaveSprite($"{ArtDir}/UI_Room_Input_Box.png", tex, new Vector4(border, border, border, border));
        }

        private static void GenerateVersusCardP1()
        {
            int w = 512;
            int h = 512;
            int border = 36;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color[] colors = new Color[w * h];

            Color borderGlow = new Color(0.15f, 0.85f, 1.0f, 1.0f); // Radiant Cyan/Aqua
            Color borderInner = new Color(0.08f, 0.38f, 0.65f, 1.0f); // Deep Cobalt
            Color cornerGold = new Color(1.0f, 0.88f, 0.40f, 1.0f); // Corner Bracket
            Color bodyTop = new Color(0.06f, 0.12f, 0.22f, 0.96f);
            Color bodyBottom = new Color(0.02f, 0.04f, 0.08f, 0.98f);

            for (int y = 0; y < h; y++)
            {
                float t = (float)y / h;
                Color fill = Color.Lerp(bodyBottom, bodyTop, t);

                for (int x = 0; x < w; x++)
                {
                    int minEdge = Mathf.Min(Mathf.Min(x, w - 1 - x), Mathf.Min(y, h - 1 - y));
                    Color c;

                    // Corner brackets
                    bool isCornerBracket = (x < 42 || x > w - 43) && (y < 42 || y > h - 43);

                    if (minEdge <= 3)
                    {
                        c = isCornerBracket ? cornerGold : borderGlow;
                    }
                    else if (minEdge <= 6)
                    {
                        c = borderInner;
                    }
                    else if (minEdge <= 8)
                    {
                        c = Color.Lerp(borderInner, fill, 0.5f);
                    }
                    else
                    {
                        // Hex-like subtle tech scanline
                        float scan = Mathf.Sin(y * 0.4f) * 0.02f;
                        c = fill + new Color(scan, scan, scan * 1.5f, 0f);

                        // Subtle inner left edge neon highlight
                        if (x >= 12 && x <= 16)
                        {
                            c = Color.Lerp(c, borderGlow, 0.35f);
                        }
                    }
                    colors[y * w + x] = c;
                }
            }

            tex.SetPixels(colors);
            tex.Apply();
            SaveSprite($"{ArtDir}/UI_Versus_Card_P1.png", tex, new Vector4(border, border, border, border));
        }

        private static void GenerateVersusCardP2()
        {
            int w = 512;
            int h = 512;
            int border = 36;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color[] colors = new Color[w * h];

            Color borderGlow = new Color(1.0f, 0.22f, 0.38f, 1.0f); // Radiant Crimson/Ruby
            Color borderInner = new Color(0.55f, 0.08f, 0.16f, 1.0f); // Deep Wine
            Color cornerGold = new Color(1.0f, 0.75f, 0.35f, 1.0f); // Corner Bracket
            Color bodyTop = new Color(0.20f, 0.05f, 0.09f, 0.96f);
            Color bodyBottom = new Color(0.08f, 0.02f, 0.04f, 0.98f);

            for (int y = 0; y < h; y++)
            {
                float t = (float)y / h;
                Color fill = Color.Lerp(bodyBottom, bodyTop, t);

                for (int x = 0; x < w; x++)
                {
                    int minEdge = Mathf.Min(Mathf.Min(x, w - 1 - x), Mathf.Min(y, h - 1 - y));
                    Color c;

                    // Corner brackets
                    bool isCornerBracket = (x < 42 || x > w - 43) && (y < 42 || y > h - 43);

                    if (minEdge <= 3)
                    {
                        c = isCornerBracket ? cornerGold : borderGlow;
                    }
                    else if (minEdge <= 6)
                    {
                        c = borderInner;
                    }
                    else if (minEdge <= 8)
                    {
                        c = Color.Lerp(borderInner, fill, 0.5f);
                    }
                    else
                    {
                        // Tech scanline
                        float scan = Mathf.Sin(y * 0.4f) * 0.02f;
                        c = fill + new Color(scan * 1.5f, scan * 0.5f, scan * 0.5f, 0f);

                        // Subtle inner right edge neon highlight
                        if (x >= w - 17 && x <= w - 13)
                        {
                            c = Color.Lerp(c, borderGlow, 0.35f);
                        }
                    }
                    colors[y * w + x] = c;
                }
            }

            tex.SetPixels(colors);
            tex.Apply();
            SaveSprite($"{ArtDir}/UI_Versus_Card_P2.png", tex, new Vector4(border, border, border, border));
        }

        private static void GenerateVersusBadgeSupreme()
        {
            int size = 512;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color[] colors = new Color[size * size];
            Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
            float radius = size * 0.47f;

            Color goldHighlight = new Color(1.0f, 0.95f, 0.60f, 1f);
            Color goldOuter = new Color(1.0f, 0.85f, 0.35f, 1f);
            Color goldMid = new Color(0.80f, 0.60f, 0.12f, 1f);
            Color goldShadow = new Color(0.35f, 0.22f, 0.05f, 1f);
            Color shieldDark = new Color(0.06f, 0.08f, 0.14f, 0.98f);
            Color flameGlow = new Color(1.0f, 0.35f, 0.10f, 1f);
            Color cyanSpark = new Color(0.20f, 0.85f, 1.0f, 1f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int idx = y * size + x;
                    Vector2 p = new Vector2(x, y);
                    float dist = Vector2.Distance(p, center);

                    if (dist > radius)
                    {
                        colors[idx] = Color.clear;
                        continue;
                    }

                    float norm = dist / radius;
                    float angle = Mathf.Atan2(y - center.y, x - center.x);
                    // 12 scalloped golden teeth
                    float ripple = Mathf.Sin(angle * 12f) * 0.05f;

                    Color c;
                    if (norm + ripple > 0.92f)
                    {
                        c = goldHighlight;
                    }
                    else if (norm + ripple > 0.84f)
                    {
                        c = goldOuter;
                    }
                    else if (norm + ripple > 0.77f)
                    {
                        c = goldMid;
                    }
                    else if (norm + ripple > 0.72f)
                    {
                        c = goldShadow;
                    }
                    else if (norm > 0.66f)
                    {
                        // Inner metallic bevel ring
                        c = Color.Lerp(goldHighlight, goldMid, Mathf.Abs(Mathf.Sin(angle * 6f)));
                    }
                    else if (norm > 0.62f)
                    {
                        c = new Color(0.12f, 0.08f, 0.04f, 1f);
                    }
                    else
                    {
                        // Inner dark shield core with radial glow
                        c = Color.Lerp(new Color(0.14f, 0.18f, 0.28f, 1f), shieldDark, norm / 0.62f);

                        // Fiery diagonal energy slash across badge
                        float slashDist = Mathf.Abs((x - center.x) - (y - center.y));
                        if (slashDist < 16f)
                        {
                            float st = 1f - (slashDist / 16f);
                            c = Color.Lerp(c, flameGlow, st * 0.75f);
                        }
                        else if (Mathf.Abs((x - center.x) + (y - center.y)) < 12f)
                        {
                            float st = 1f - (Mathf.Abs((x - center.x) + (y - center.y)) / 12f);
                            c = Color.Lerp(c, cyanSpark, st * 0.6f);
                        }
                    }

                    colors[idx] = c;
                }
            }

            tex.SetPixels(colors);
            tex.Apply();
            SaveSprite($"{ArtDir}/UI_Versus_Badge_Large.png", tex);
            SaveSprite($"{ArtDir}/UI_Versus_Badge_Supreme.png", tex);
        }

        private static void GenerateVersusGlowBurst()
        {
            int size = 512;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color[] colors = new Color[size * size];
            Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
            float maxR = size * 0.48f;

            Color coreColor = new Color(1.0f, 0.95f, 0.50f, 0.95f);
            Color midColor = new Color(1.0f, 0.45f, 0.10f, 0.60f);
            Color outerColor = new Color(0.80f, 0.10f, 0.20f, 0f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int idx = y * size + x;
                    float dist = Vector2.Distance(new Vector2(x, y), center);

                    if (dist >= maxR)
                    {
                        colors[idx] = Color.clear;
                        continue;
                    }

                    float norm = dist / maxR;
                    float alpha = Mathf.Pow(1f - norm, 1.8f);

                    float angle = Mathf.Atan2(y - center.y, x - center.x);
                    float rays = 0.80f + 0.20f * Mathf.Cos(angle * 16f);

                    Color c = norm < 0.30f 
                        ? Color.Lerp(coreColor, midColor, norm / 0.30f) 
                        : Color.Lerp(midColor, outerColor, (norm - 0.30f) / 0.70f);

                    c.a *= alpha * rays;
                    colors[idx] = c;
                }
            }

            tex.SetPixels(colors);
            tex.Apply();
            SaveSprite($"{ArtDir}/UI_Versus_Glow_Burst.png", tex);
        }

        private static void GenerateVersusEnergyRays()
        {
            int size = 512;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color[] colors = new Color[size * size];
            Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
            float maxR = size * 0.48f;

            Color goldBeam = new Color(1.0f, 0.85f, 0.30f, 0.85f);
            Color cyanBeam = new Color(0.20f, 0.85f, 1.0f, 0.85f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int idx = y * size + x;
                    float dist = Vector2.Distance(new Vector2(x, y), center);

                    if (dist >= maxR)
                    {
                        colors[idx] = Color.clear;
                        continue;
                    }

                    float norm = dist / maxR;
                    float angle = Mathf.Atan2(y - center.y, x - center.x);
                    float spoke = Mathf.Cos(angle * 12f);

                    if (spoke < 0.2f)
                    {
                        colors[idx] = Color.clear;
                        continue;
                    }

                    float beamIntensity = Mathf.Pow((spoke - 0.2f) / 0.8f, 1.5f);
                    float alpha = (1f - norm) * beamIntensity;

                    Color beamCol = Mathf.Sin(angle * 2f) > 0 ? goldBeam : cyanBeam;
                    beamCol.a *= alpha;
                    colors[idx] = beamCol;
                }
            }

            tex.SetPixels(colors);
            tex.Apply();
            SaveSprite($"{ArtDir}/UI_Versus_Energy_Rays.png", tex);
        }

        private static void GenerateVersusHeroSlotLarge()
        {
            int w = 256;
            int h = 384;
            int border = 28;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color[] colors = new Color[w * h];

            Color borderGoldBright = new Color(1.0f, 0.88f, 0.40f, 1f); // Bright Gold
            Color borderGoldMid = new Color(0.78f, 0.60f, 0.16f, 1f); // Metallic Gold
            Color borderDark = new Color(0.22f, 0.16f, 0.06f, 1f);
            Color innerRecess = new Color(0.04f, 0.05f, 0.09f, 0.96f);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    int minEdge = Mathf.Min(Mathf.Min(x, w - 1 - x), Mathf.Min(y, h - 1 - y));
                    bool isCorner = (x < 32 || x > w - 33) && (y < 32 || y > h - 33);

                    Color c;
                    if (minEdge <= 2)
                    {
                        c = borderGoldBright;
                    }
                    else if (minEdge <= 5)
                    {
                        c = isCorner ? borderGoldBright : borderGoldMid;
                    }
                    else if (minEdge <= 8)
                    {
                        c = borderDark;
                    }
                    else
                    {
                        // Subtle gradient inside the card recess
                        float ty = (float)y / h;
                        c = Color.Lerp(innerRecess, new Color(0.08f, 0.10f, 0.16f, 0.96f), ty);

                        // Horizontal accent line at bottom 25% for name tag separation
                        if (y >= 88 && y <= 90)
                        {
                            c = Color.Lerp(c, borderGoldMid, 0.5f);
                        }
                    }
                    colors[y * w + x] = c;
                }
            }

            tex.SetPixels(colors);
            tex.Apply();
            SaveSprite($"{ArtDir}/UI_Versus_Hero_Slot.png", tex, new Vector4(border, border, border, border));
            SaveSprite($"{ArtDir}/UI_Versus_Hero_Slot_Large.png", tex, new Vector4(border, border, border, border));
        }

        private static void GenerateVersusDividerBeam()
        {
            int w = 64;
            int h = 512;
            int border = 16;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color[] colors = new Color[w * h];

            Color core = Color.white;
            Color auraCyan = new Color(0.20f, 0.85f, 1.0f, 0.85f);
            Color auraGold = new Color(1.0f, 0.85f, 0.30f, 0.85f);

            for (int y = 0; y < h; y++)
            {
                float ty = (float)y / h;
                Color aura = Color.Lerp(auraCyan, auraGold, ty);

                for (int x = 0; x < w; x++)
                {
                    float distFromCenter = Mathf.Abs(x - (w * 0.5f));
                    float normX = distFromCenter / (w * 0.5f);

                    Color c;
                    if (distFromCenter <= 2.5f)
                    {
                        c = core;
                    }
                    else
                    {
                        float alpha = Mathf.Pow(1f - normX, 2f);
                        c = aura;
                        c.a *= alpha;
                    }
                    colors[y * w + x] = c;
                }
            }

            tex.SetPixels(colors);
            tex.Apply();
            SaveSprite($"{ArtDir}/UI_Versus_Divider_Beam.png", tex, new Vector4(border, border, border, border));
        }

        private static void GenerateVersusScreenFlash()
        {
            int size = 64;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color[] colors = new Color[size * size];

            for (int i = 0; i < size * size; i++)
            {
                colors[i] = Color.white;
            }

            tex.SetPixels(colors);
            tex.Apply();
            SaveSprite($"{ArtDir}/UI_Versus_Screen_Flash.png", tex);
        }

        private static void GenerateVersusPill()
        {
            int w = 256;
            int h = 64;
            int border = 20;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color[] colors = new Color[w * h];

            Color borderCol = new Color(0.70f, 0.82f, 1.0f, 0.8f);
            Color fill = new Color(0.06f, 0.09f, 0.16f, 0.94f);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    int minEdge = Mathf.Min(Mathf.Min(x, w - 1 - x), Mathf.Min(y, h - 1 - y));
                    Color c;
                    if (minEdge <= 2)
                    {
                        c = borderCol;
                    }
                    else
                    {
                        c = fill;
                    }
                    colors[y * w + x] = c;
                }
            }

            tex.SetPixels(colors);
            tex.Apply();
            SaveSprite($"{ArtDir}/UI_Versus_Pill.png", tex, new Vector4(border, border, border, border));
        }

        #endregion
    }
}
#endif
