#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Clashura.Utilities
{
    /// <summary>
    /// Master Procedural Art Generator for the "Better with AI" game mode.
    /// Creates high-resolution, AAA fighting-game themed sprites:
    /// - Heavy metallic obsidian modal panel with golden corner brackets & ambient cyber glow
    /// - Three tall, rich tier card plates (880x160 px, Normal & Glowing Selected states for Easy, Medium, Hard)
    /// - 110x110 circular heraldic medallions with multi-layered gold/steel rivets & radiant crests
    /// - Ornate circular hero portrait frames & mask for character bust presentation
    /// - Radiant active selection pill badges and streamlined status ribbon deck
    /// </summary>
    public static class AIArtGenerator
    {
        public const string AIArtDir = "Assets/Clashura/Art/UI/AI";

        [MenuItem("Clashura/Art/Generate AI Mode UI Assets")]
        public static void GenerateAllAIArt()
        {
            EnsureDirectory(AIArtDir);

            GeneratePlayWithAIIcon();
            GenerateArenaPanelFrame();
            GenerateTierCardSprites();
            GenerateHeraldicBadges();
            GenerateCircleMask();
            GenerateHeroPortraitFrame();
            GenerateSelectionPill();
            GenerateStreamlinedStatusDeck();
            GenerateCareerMenuAssets();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("<color=green>[AIArtGenerator] Successfully generated all premium AAA AI Mode UI assets!</color>");
        }

        private static void EnsureDirectory(string path)
        {
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
        }

        #region 1. Main Menu Play with AI Icon
        private static void GeneratePlayWithAIIcon()
        {
            int size = 64;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color[] px = new Color[size * size];

            float cx = size * 0.5f;
            float cy = size * 0.5f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - cx;
                    float dy = y - cy;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);

                    Color c = Color.clear;

                    // Hexagonal outer rim
                    float angle = Mathf.Atan2(dy, dx);
                    float hexR = 27f / (Mathf.Cos((angle % (Mathf.PI / 3f)) - (Mathf.PI / 6f)));

                    if (dist >= hexR - 3.5f && dist <= hexR + 1f)
                    {
                        c = new Color(0.20f, 0.88f, 1.0f, 1.0f); // Radiant Cyan
                    }
                    else if (dist < hexR - 3.5f && dist > 14f)
                    {
                        c = new Color(0.04f, 0.08f, 0.16f, 0.95f); // Obsidian
                        if (Mathf.Abs(dx) < 2f || Mathf.Abs(dy) < 2f || Mathf.Abs(dx - dy) < 2.5f)
                        {
                            c = new Color(0.12f, 0.58f, 0.92f, 0.85f); // Glowing circuit
                        }
                    }
                    else if (dist <= 14f)
                    {
                        if (dist <= 7f)
                        {
                            c = new Color(1.0f, 0.88f, 0.35f, 1f); // Core Gold
                        }
                        else
                        {
                            c = new Color(0.15f, 0.85f, 0.98f, 0.95f); // Iris Ring
                        }
                    }

                    px[y * size + x] = c;
                }
            }

            tex.SetPixels(px);
            tex.Apply();
            SaveSprite($"{AIArtDir}/Icon_PlayWithAI.png", tex);
        }
        #endregion

        #region 2. Modal Arena Panel Frame (960 x 740)
        private static void GenerateArenaPanelFrame()
        {
            int w = 960;
            int h = 740;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color[] px = new Color[w * h];

            Color topBg = new Color(0.05f, 0.08f, 0.15f, 0.97f);
            Color botBg = new Color(0.02f, 0.04f, 0.08f, 0.99f);
            Color borderGold = new Color(0.92f, 0.76f, 0.28f, 0.95f);
            Color borderSteel = new Color(0.22f, 0.38f, 0.60f, 0.85f);
            Color cornerCyan = new Color(0.25f, 0.85f, 1.0f, 1.0f);

            float cx = w * 0.5f;
            float cy = h * 0.5f;
            float maxR = Mathf.Sqrt(cx * cx + cy * cy);

            for (int y = 0; y < h; y++)
            {
                float ty = (float)y / h;
                Color baseBg = Color.Lerp(botBg, topBg, ty);

                for (int x = 0; x < w; x++)
                {
                    int dCorner = GetChamferDistance(x, y, w, h, 28);

                    if (dCorner > 28)
                    {
                        px[y * w + x] = Color.clear;
                        continue;
                    }

                    // Vignette depth
                    float distCenter = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy)) / maxR;
                    Color c = Color.Lerp(baseBg * 1.15f, baseBg * 0.80f, distCenter);

                    // Multi-layer border
                    if (dCorner >= 25)
                    {
                        bool isCorner = (x < 65 || x > w - 66) && (y < 65 || y > h - 66);
                        c = isCorner ? cornerCyan : borderGold;
                    }
                    else if (dCorner >= 22)
                    {
                        c = borderSteel;
                    }
                    else if (dCorner >= 19)
                    {
                        c = new Color(0.08f, 0.16f, 0.28f, 0.95f); // Inner groove
                    }
                    else
                    {
                        // Subtle header accent line at y = h - 140
                        if (y >= h - 142 && y <= h - 140 && x > 60 && x < w - 60)
                        {
                            float tGrad = 1f - Mathf.Abs(x - cx) / (cx - 60f);
                            c = Color.Lerp(c, borderGold, tGrad * 0.65f);
                        }
                    }

                    px[y * w + x] = c;
                }
            }

            tex.SetPixels(px);
            tex.Apply();
            SaveSprite($"{AIArtDir}/UI_AI_Arena_Panel_Frame.png", tex);
        }
        #endregion

        #region 3. Tier Card Sprites (880 x 160 px, Normal & Selected)
        private static void GenerateTierCardSprites()
        {
            int w = 880;
            int h = 160;
            int chamfer = 16;

            // Easy: Emerald Green
            Color easyBorder = new Color(0.14f, 0.82f, 0.42f, 1f);
            Color easyGlow = new Color(0.24f, 0.98f, 0.58f, 1f);
            Color easyGradTop = new Color(0.06f, 0.16f, 0.11f, 0.98f);
            Color easyGradBot = new Color(0.02f, 0.08f, 0.05f, 0.99f);
            CreateDifficultyCardPlate($"{AIArtDir}/UI_Card_Easy_Normal.png", w, h, chamfer, easyBorder, easyGradTop, easyGradBot, false);
            CreateDifficultyCardPlate($"{AIArtDir}/UI_Card_Easy_Selected.png", w, h, chamfer, easyGlow, easyGradTop * 1.6f, easyGradBot * 1.8f, true);

            // Medium: Celestial Gold
            Color medBorder = new Color(0.96f, 0.74f, 0.18f, 1f);
            Color medGlow = new Color(1.00f, 0.88f, 0.32f, 1f);
            Color medGradTop = new Color(0.17f, 0.12f, 0.04f, 0.98f);
            Color medGradBot = new Color(0.08f, 0.05f, 0.02f, 0.99f);
            CreateDifficultyCardPlate($"{AIArtDir}/UI_Card_Medium_Normal.png", w, h, chamfer, medBorder, medGradTop, medGradBot, false);
            CreateDifficultyCardPlate($"{AIArtDir}/UI_Card_Medium_Selected.png", w, h, chamfer, medGlow, medGradTop * 1.6f, medGradBot * 1.8f, true);

            // Hard: Crimson Fire
            Color hardBorder = new Color(0.92f, 0.24f, 0.24f, 1f);
            Color hardGlow = new Color(1.00f, 0.40f, 0.40f, 1f);
            Color hardGradTop = new Color(0.18f, 0.05f, 0.05f, 0.98f);
            Color hardGradBot = new Color(0.08f, 0.02f, 0.02f, 0.99f);
            CreateDifficultyCardPlate($"{AIArtDir}/UI_Card_Hard_Normal.png", w, h, chamfer, hardBorder, hardGradTop, hardGradBot, false);
            CreateDifficultyCardPlate($"{AIArtDir}/UI_Card_Hard_Selected.png", w, h, chamfer, hardGlow, hardGradTop * 1.6f, hardGradBot * 1.8f, true);
        }

        private static void CreateDifficultyCardPlate(string path, int w, int h, int chamfer, Color borderColor, Color topBg, Color botBg, bool isSelected)
        {
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color[] px = new Color[w * h];

            for (int y = 0; y < h; y++)
            {
                float ty = (float)y / h;
                Color bodyCol = Color.Lerp(botBg, topBg, ty);

                for (int x = 0; x < w; x++)
                {
                    int dCorner = GetChamferDistance(x, y, w, h, chamfer);

                    if (dCorner > chamfer)
                    {
                        px[y * w + x] = Color.clear;
                        continue;
                    }

                    Color c = bodyCol;

                    if (isSelected)
                    {
                        // Radiant 4.5px neon border with hot core
                        if (dCorner >= chamfer - 4)
                        {
                            c = borderColor;
                        }
                        else if (dCorner >= chamfer - 7)
                        {
                            c = Color.Lerp(borderColor, Color.white, 0.70f); // Luminous white-hot core
                        }
                        else if (dCorner >= chamfer - 13)
                        {
                            c = Color.Lerp(bodyCol, borderColor, 0.50f); // Ambient inner bloom
                        }
                        else
                        {
                            // Top horizontal energy sweep
                            if (y >= h - 22)
                            {
                                c = Color.Lerp(bodyCol, borderColor, 0.40f);
                            }
                        }
                    }
                    else
                    {
                        // Clean 2.5px metallic rim
                        if (dCorner >= chamfer - 3)
                        {
                            c = borderColor;
                        }
                        else if (dCorner >= chamfer - 5)
                        {
                            c = Color.Lerp(borderColor, Color.black, 0.40f);
                        }
                        else
                        {
                            // Top subtle sheen
                            if (y >= h - 16)
                            {
                                c = Color.Lerp(bodyCol, borderColor, 0.22f);
                            }
                        }
                    }

                    px[y * w + x] = c;
                }
            }

            tex.SetPixels(px);
            tex.Apply();
            SaveSprite(path, tex);
        }

        private static int GetChamferDistance(int x, int y, int w, int h, int chamfer)
        {
            int left = x;
            int right = (w - 1) - x;
            int bottom = y;
            int top = (h - 1) - y;

            int dist = 0;
            if (left < chamfer && top < chamfer) dist = (chamfer - left) + (chamfer - top);
            else if (right < chamfer && top < chamfer) dist = (chamfer - right) + (chamfer - top);
            else if (left < chamfer && bottom < chamfer) dist = (chamfer - left) + (chamfer - bottom);
            else if (right < chamfer && bottom < chamfer) dist = (chamfer - right) + (chamfer - bottom);

            return dist;
        }
        #endregion

        #region 4. High-Detail Heraldic Badges (110 x 110 px)
        private static void GenerateHeraldicBadges()
        {
            // Easy: Emerald Aegis Shield Medallion (110x110)
            GenerateCircularBadge($"{AIArtDir}/Icon_Badge_Easy.png", 110, new Color(0.14f, 0.88f, 0.48f, 1f), 1);

            // Medium: Celestial Crossed Blades Medallion (110x110)
            GenerateCircularBadge($"{AIArtDir}/Icon_Badge_Medium.png", 110, new Color(0.98f, 0.80f, 0.22f, 1f), 2);

            // Hard: Infernal Demon Skull Medallion (110x110)
            GenerateCircularBadge($"{AIArtDir}/Icon_Badge_Hard.png", 110, new Color(0.96f, 0.26f, 0.26f, 1f), 3);
        }

        private static void GenerateCircularBadge(string path, int size, Color accent, int emblemType)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color[] px = new Color[size * size];

            float cx = size * 0.5f;
            float cy = size * 0.5f;
            float maxR = (size - 2) * 0.5f;

            Color goldRim = new Color(0.94f, 0.78f, 0.30f, 1.0f);
            Color darkMetal = new Color(0.08f, 0.12f, 0.18f, 0.98f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - cx;
                    float dy = y - cy;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);

                    if (dist > maxR)
                    {
                        px[y * size + x] = Color.clear;
                        continue;
                    }

                    Color c = Color.clear;

                    // Layer 1: Outer Ornate Forged Bevel (dist >= maxR - 4.5)
                    if (dist >= maxR - 4.5f)
                    {
                        c = goldRim;
                    }
                    // Layer 2: Rivet / Stud Ring (dist between maxR - 4.5 and maxR - 8.5)
                    else if (dist >= maxR - 8.5f)
                    {
                        c = darkMetal;
                        // 8 Rivets/Studs around ring
                        float angle = Mathf.Atan2(dy, dx);
                        float rivetDist = Mathf.Abs(Mathf.Sin(angle * 4f));
                        if (rivetDist < 0.25f && dist >= maxR - 7f && dist <= maxR - 5.5f)
                        {
                            c = Color.white; // Shiny steel rivet
                        }
                    }
                    // Layer 3: Radiant Neon Energy Ring (dist between maxR - 8.5 and maxR - 12.5)
                    else if (dist >= maxR - 12.5f)
                    {
                        c = (dist >= maxR - 10.5f) ? accent : Color.Lerp(accent, Color.white, 0.65f);
                    }
                    // Layer 4: Inner Dark Gem Core with Radial Bloom
                    else
                    {
                        float innerT = dist / (maxR - 12.5f);
                        c = Color.Lerp(accent * 0.40f, darkMetal, innerT);

                        // High-Detail Emblems
                        if (emblemType == 1) // Aegis Shield
                        {
                            float sx = Mathf.Abs(dx);
                            float sy = dy + 3f;
                            bool inShield = (sy >= -26f && sy <= 26f && sx <= (26f - (sy > 4f ? (sy - 4f) * 0.85f : 0f)));
                            bool inBorder = inShield && (sx >= (22f - (sy > 4f ? (sy - 4f) * 0.85f : 0f)) || sy <= -22f || sy >= 23f);

                            if (inBorder) c = goldRim;
                            else if (inShield)
                            {
                                // Center Diamond Core
                                if (sx + Mathf.Abs(sy) <= 10f) c = Color.white;
                                else if (sx <= 3.5f || Mathf.Abs(sy) <= 3.5f) c = accent;
                                else c = Color.Lerp(c, accent, 0.70f);
                            }
                        }
                        else if (emblemType == 2) // Dual Crossed Blades
                        {
                            float d1 = Mathf.Abs(dx - dy);
                            float d2 = Mathf.Abs(dx + dy);
                            if (dist <= 30f && (d1 <= 3.5f || d2 <= 3.5f))
                            {
                                c = (d1 <= 1.8f || d2 <= 1.8f) ? Color.white : goldRim;
                            }
                            // Sunburst Core
                            if (dist <= 10f)
                            {
                                c = (dist <= 5f) ? Color.white : accent;
                            }
                        }
                        else if (emblemType == 3) // Infernal Skull Crest
                        {
                            float angle = Mathf.Atan2(dy, dx);
                            float rSpike = 20f + 8f * Mathf.Cos(angle * 5f);
                            if (dist <= rSpike)
                            {
                                c = (dist >= rSpike - 4f) ? goldRim : accent;
                                // Glowing Eyes
                                if (dist <= 9f && (Mathf.Abs(dx - 4.5f) <= 2.2f || Mathf.Abs(dx + 4.5f) <= 2.2f) && Mathf.Abs(dy - 1.5f) <= 2.5f)
                                {
                                    c = Color.white; // Fiery white eyes
                                }
                            }
                        }
                    }

                    px[y * size + x] = c;
                }
            }

            tex.SetPixels(px);
            tex.Apply();
            SaveSprite(path, tex);
        }
        #endregion

        #region 5. Hero Portrait Frame & Circle Mask (128 x 128 px)
        private static void GenerateCircleMask()
        {
            int size = 128;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color[] px = new Color[size * size];

            float cx = size * 0.5f;
            float cy = size * 0.5f;
            float r = 58f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
                    if (dist > r)
                    {
                        px[y * size + x] = Color.clear;
                    }
                    else if (dist >= r - 1.5f)
                    {
                        float a = (r - dist) / 1.5f;
                        px[y * size + x] = new Color(1f, 1f, 1f, a);
                    }
                    else
                    {
                        px[y * size + x] = Color.white;
                    }
                }
            }

            tex.SetPixels(px);
            tex.Apply();
            SaveSprite($"{AIArtDir}/UI_Circle_Mask.png", tex);
        }

        private static void GenerateHeroPortraitFrame()
        {
            int size = 128;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color[] px = new Color[size * size];

            float cx = size * 0.5f;
            float cy = size * 0.5f;
            float maxR = 62f;

            Color goldRim = new Color(0.96f, 0.82f, 0.32f, 1.0f);
            Color darkMetal = new Color(0.10f, 0.15f, 0.22f, 0.98f);
            Color innerBevel = new Color(0.85f, 0.70f, 0.25f, 1.0f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - cx;
                    float dy = y - cy;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);

                    if (dist > maxR || dist < 48f)
                    {
                        px[y * size + x] = Color.clear;
                        continue;
                    }

                    Color c = Color.clear;

                    // Outer gold bevel (dist between maxR and maxR - 4)
                    if (dist >= maxR - 4f)
                    {
                        c = goldRim;
                    }
                    // Middle dark steel groove with 8 golden studs
                    else if (dist >= maxR - 9.5f)
                    {
                        c = darkMetal;
                        float angle = Mathf.Atan2(dy, dx);
                        float stud = Mathf.Abs(Mathf.Sin(angle * 4f));
                        if (stud < 0.25f && dist >= maxR - 8f && dist <= maxR - 5.5f)
                        {
                            c = Color.white; // Bright metallic stud
                        }
                    }
                    // Inner gold rim (dist between maxR - 9.5 and 48)
                    else
                    {
                        c = (dist >= maxR - 12f) ? goldRim : innerBevel;
                    }

                    px[y * size + x] = c;
                }
            }

            tex.SetPixels(px);
            tex.Apply();
            SaveSprite($"{AIArtDir}/UI_Hero_Portrait_Frame.png", tex);
        }
        #endregion

        #region 6. Selection Pill Badge & Streamlined Status Deck
        private static void GenerateSelectionPill()
        {
            int w = 174;
            int h = 40;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color[] px = new Color[w * h];
            float r = (h - 1) * 0.5f;

            Color fillCol = new Color(0.05f, 0.20f, 0.12f, 0.98f);
            Color borderCol = new Color(0.18f, 0.92f, 0.48f, 1.0f);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float dEdge = 0f;
                    if (x < r) dEdge = Mathf.Sqrt((x - r) * (x - r) + (y - r) * (y - r));
                    else if (x > w - 1 - r) dEdge = Mathf.Sqrt((x - (w - 1 - r)) * (x - (w - 1 - r)) + (y - r) * (y - r));
                    else dEdge = Mathf.Abs(y - r);

                    if (dEdge > r)
                    {
                        px[y * w + x] = Color.clear;
                        continue;
                    }

                    Color c = fillCol;
                    if (dEdge >= r - 3f)
                    {
                        c = borderCol;
                    }
                    else if (dEdge >= r - 5f)
                    {
                        c = Color.Lerp(borderCol, Color.white, 0.75f);
                    }

                    px[y * w + x] = c;
                }
            }

            tex.SetPixels(px);
            tex.Apply();
            SaveSprite($"{AIArtDir}/UI_Badge_Selected_Active.png", tex);
        }

        private static void GenerateStreamlinedStatusDeck()
        {
            int w = 880;
            int h = 52;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color[] px = new Color[w * h];

            Color topBg = new Color(0.06f, 0.10f, 0.18f, 0.96f);
            Color botBg = new Color(0.03f, 0.05f, 0.10f, 0.98f);
            Color rimCyan = new Color(0.22f, 0.78f, 1.0f, 0.95f);
            Color rimGold = new Color(0.94f, 0.80f, 0.30f, 0.90f);

            for (int y = 0; y < h; y++)
            {
                float ty = (float)y / h;
                Color baseCol = Color.Lerp(botBg, topBg, ty);

                for (int x = 0; x < w; x++)
                {
                    int dCorner = GetChamferDistance(x, y, w, h, 12);

                    if (dCorner > 12)
                    {
                        px[y * w + x] = Color.clear;
                        continue;
                    }

                    Color c = baseCol;

                    if (dCorner >= 10)
                    {
                        bool isCorner = (x < 30 || x > w - 31);
                        c = isCorner ? rimGold : rimCyan;
                    }
                    else if (dCorner >= 8)
                    {
                        c = Color.Lerp(rimCyan, Color.black, 0.40f);
                    }

                    px[y * w + x] = c;
                }
            }

            tex.SetPixels(px);
            tex.Apply();
            SaveSprite($"{AIArtDir}/UI_AI_Console_Deck.png", tex);
        }
        #endregion

        #region 9. Career Menu & Confirmation Dialog Assets
        private static void GenerateCareerMenuAssets()
        {
            GenerateMenuCardPlate($"{AIArtDir}/UI_Card_NewGame_Plate.png", new Color(0.18f, 0.72f, 0.98f), new Color(1.0f, 0.85f, 0.32f));
            GenerateMenuCardPlate($"{AIArtDir}/UI_Card_Continue_Plate.png", new Color(0.22f, 0.88f, 0.52f), new Color(1.0f, 0.85f, 0.32f));
            GenerateConfirmationDialogFrame($"{AIArtDir}/UI_Confirmation_Dialog_Frame.png");
            GenerateActionButtons();
        }

        private static void GenerateMenuCardPlate(string path, Color accentRim, Color cornerGold)
        {
            int w = 880;
            int h = 140;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color[] px = new Color[w * h];

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    int dLeft = x;
                    int dRight = (w - 1) - x;
                    int dBottom = y;
                    int dTop = (h - 1) - y;

                    int cTopLeft = Mathf.Max(0, 16 - (dLeft + dTop));
                    int cBottomLeft = Mathf.Max(0, 16 - (dLeft + dBottom));
                    int cTopRight = Mathf.Max(0, 16 - (dRight + dTop));
                    int cBottomRight = Mathf.Max(0, 16 - (dRight + dBottom));
                    int dCorner = Mathf.Max(Mathf.Max(cTopLeft, cBottomLeft), Mathf.Max(cTopRight, cBottomRight));

                    if (dCorner > 14)
                    {
                        px[y * w + x] = Color.clear;
                        continue;
                    }

                    Color c = new Color(0.04f, 0.07f, 0.12f, 0.94f);

                    // Outer border
                    if (dCorner >= 11 || dLeft < 4 || dRight < 4 || dBottom < 4 || dTop < 4)
                    {
                        bool isCorner = (dLeft < 40 || dRight < 40);
                        c = isCorner ? cornerGold : accentRim;
                    }
                    else if (dCorner >= 8 || dLeft < 8 || dRight < 8 || dBottom < 8 || dTop < 8)
                    {
                        c = Color.Lerp(accentRim, Color.black, 0.55f);
                    }
                    else if ((y % 16 == 0) && (x % 32 < 16))
                    {
                        c = Color.Lerp(c, accentRim, 0.05f); // Subtle grid texture
                    }

                    px[y * w + x] = c;
                }
            }

            tex.SetPixels(px);
            tex.Apply();
            SaveSprite(path, tex);
        }

        private static void GenerateConfirmationDialogFrame(string path)
        {
            int w = 740;
            int h = 420;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color[] px = new Color[w * h];

            Color rimGold = new Color(1.0f, 0.85f, 0.35f, 1f);
            Color rimRuby = new Color(0.95f, 0.28f, 0.28f, 1f);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    int dLeft = x;
                    int dRight = (w - 1) - x;
                    int dBottom = y;
                    int dTop = (h - 1) - y;

                    int cTopLeft = Mathf.Max(0, 24 - (dLeft + dTop));
                    int cBottomLeft = Mathf.Max(0, 24 - (dLeft + dBottom));
                    int cTopRight = Mathf.Max(0, 24 - (dRight + dTop));
                    int cBottomRight = Mathf.Max(0, 24 - (dRight + dBottom));
                    int dCorner = Mathf.Max(Mathf.Max(cTopLeft, cBottomLeft), Mathf.Max(cTopRight, cBottomRight));

                    if (dCorner > 20)
                    {
                        px[y * w + x] = Color.clear;
                        continue;
                    }

                    Color c = new Color(0.03f, 0.05f, 0.09f, 0.98f); // Heavy obsidian

                    if (dCorner >= 17 || dLeft < 5 || dRight < 5 || dBottom < 5 || dTop < 5)
                    {
                        bool isCorner = (dLeft < 50 || dRight < 50);
                        c = isCorner ? rimGold : rimRuby;
                    }
                    else if (dCorner >= 13 || dLeft < 9 || dRight < 9 || dBottom < 9 || dTop < 9)
                    {
                        c = Color.Lerp(rimRuby, Color.black, 0.5f);
                    }

                    px[y * w + x] = c;
                }
            }

            tex.SetPixels(px);
            tex.Apply();
            SaveSprite(path, tex);
        }

        private static void GenerateActionButtons()
        {
            GeneratePillButton($"{AIArtDir}/UI_Button_Confirm_Yes.png", 300, 64, new Color(0.92f, 0.22f, 0.22f), new Color(0.35f, 0.05f, 0.05f));
            GeneratePillButton($"{AIArtDir}/UI_Button_Confirm_No.png", 300, 64, new Color(0.20f, 0.85f, 0.50f), new Color(0.04f, 0.25f, 0.12f));
        }

        private static void GeneratePillButton(string path, int w, int h, Color rimCol, Color baseCol)
        {
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color[] px = new Color[w * h];

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    int dLeft = x;
                    int dRight = (w - 1) - x;
                    int dBottom = y;
                    int dTop = (h - 1) - y;

                    int cTopLeft = Mathf.Max(0, 14 - (dLeft + dTop));
                    int cBottomLeft = Mathf.Max(0, 14 - (dLeft + dBottom));
                    int cTopRight = Mathf.Max(0, 14 - (dRight + dTop));
                    int cBottomRight = Mathf.Max(0, 14 - (dRight + dBottom));
                    int dCorner = Mathf.Max(Mathf.Max(cTopLeft, cBottomLeft), Mathf.Max(cTopRight, cBottomRight));

                    if (dCorner > 12)
                    {
                        px[y * w + x] = Color.clear;
                        continue;
                    }

                    Color c = baseCol;

                    if (dCorner >= 9 || dLeft < 3 || dRight < 3 || dBottom < 3 || dTop < 3)
                    {
                        c = rimCol;
                    }
                    else if (dCorner >= 6 || dLeft < 6 || dRight < 6 || dBottom < 6 || dTop < 6)
                    {
                        c = Color.Lerp(rimCol, baseCol, 0.4f);
                    }

                    px[y * w + x] = c;
                }
            }

            tex.SetPixels(px);
            tex.Apply();
            SaveSprite(path, tex);
        }
        #endregion

        private static void SaveSprite(string path, Texture2D tex)
        {
            byte[] bytes = tex.EncodeToPNG();
            Object.DestroyImmediate(tex);
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
                importer.SaveAndReimport();
            }
        }
    }
}
#endif
