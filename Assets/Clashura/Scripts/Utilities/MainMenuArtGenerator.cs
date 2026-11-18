#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Clashura.Utilities
{
    /// <summary>
    /// Master Procedural UI Art Generator for CLASHURA.
    /// Generates 100% original, crisp, professional 2D game interface assets:
    /// - 9-sliceable chamfered obsidian & gold button frames
    /// - Vector fighting-game menu icons (PvP, Matchmaking, Character Select, Settings, Exit)
    /// - Data-driven ability card plates & 8 ability archetype icons
    /// - Combat attributes panel frame, stat icons & gradient health/stat bars
    /// - Team selection dashboard frame, portrait slots, and badges
    /// - Original CLASHURA winged crest emblem and commander profile frame
    /// </summary>
    public static class MainMenuArtGenerator
    {
        private const string ArtDir = "Assets/Clashura/Art/UI/MainMenu";

        [MenuItem("Clashura/Art/Generate Original CLASHURA UI Assets")]
        public static void GenerateAllArt()
        {
            EnsureDirectory(ArtDir);

            GenerateButtonFrames();
            GenerateMenuIcons();
            GenerateAbilityCardAssets();
            GenerateCombatAttributeAssets();
            GenerateTeamBarAssets();
            GenerateLogoAndBadges();
            GeneratePopupAssets();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("<color=green>[MainMenuArtGenerator] Successfully generated all 100% original CLASHURA UI assets!</color>");
        }

        private static void EnsureDirectory(string path)
        {
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
        }

        #region 1. Button Frames
        private static void GenerateButtonFrames()
        {
            int w = 650;
            int h = 90;

            // A. Normal Button Frame
            CreateChamferedFrame($"{ArtDir}/UI_Button_Normal.png", w, h, 16,
                new Color(0.08f, 0.11f, 0.17f, 0.96f),   // Dark obsidian core
                new Color(0.14f, 0.18f, 0.28f, 0.96f),   // Subtle top sheen
                new Color(0.35f, 0.45f, 0.62f, 1f),      // Steel-blue outer border
                new Color(0.60f, 0.75f, 0.95f, 0.9f),    // Specular top bevel
                new Color(0.85f, 0.70f, 0.25f, 0.7f),    // Warm corner accent
                false);

            // B. Highlight / Hover Button Frame (Radiant Gold & Amber Energy)
            CreateChamferedFrame($"{ArtDir}/UI_Button_Highlight.png", w, h, 16,
                new Color(0.12f, 0.16f, 0.26f, 0.98f),
                new Color(0.20f, 0.26f, 0.40f, 0.98f),
                new Color(1.0f, 0.82f, 0.28f, 1f),       // Bright gold border
                new Color(1.0f, 0.95f, 0.65f, 1f),       // Top gold highlight
                new Color(1.0f, 0.75f, 0.20f, 1f),       // Gold energy corners
                true);

            // C. Pressed Button Frame
            CreateChamferedFrame($"{ArtDir}/UI_Button_Pressed.png", w, h, 16,
                new Color(0.06f, 0.08f, 0.13f, 0.98f),
                new Color(0.10f, 0.13f, 0.20f, 0.98f),
                new Color(0.75f, 0.58f, 0.18f, 1f),
                new Color(0.85f, 0.70f, 0.30f, 0.8f),
                new Color(0.70f, 0.50f, 0.15f, 0.8f),
                false);

            // D. Hexagonal Back Button (230x78)
            CreateHexButton($"{ArtDir}/UI_Button_Back.png", 230, 78, 14,
                new Color(0.08f, 0.11f, 0.17f, 0.94f),
                new Color(0.95f, 0.78f, 0.28f, 1f));

            // E. Select For Team Button (380x80)
            CreateHexButton($"{ArtDir}/UI_Button_Select.png", 380, 80, 14,
                new Color(0.09f, 0.13f, 0.22f, 0.95f),
                new Color(1.0f, 0.82f, 0.28f, 1f));

            // F. Confirm Button (185x85)
            CreateHexButton($"{ArtDir}/UI_Button_Confirm.png", 185, 85, 12,
                new Color(0.18f, 0.14f, 0.05f, 0.95f),
                new Color(1.0f, 0.85f, 0.25f, 1f));

            // G. Square Nav Button Frame for < and > (90x90)
            CreateChamferedFrame($"{ArtDir}/UI_Square_Nav_Button.png", 90, 90, 12,
                new Color(0.09f, 0.12f, 0.18f, 0.96f),
                new Color(0.15f, 0.20f, 0.30f, 0.96f),
                new Color(1.0f, 0.82f, 0.28f, 1f),
                new Color(1.0f, 0.95f, 0.65f, 1f),
                new Color(1.0f, 0.75f, 0.20f, 1f),
                false);
        }

        private static void CreateChamferedFrame(string path, int w, int h, int chamfer, Color bgBot, Color bgTop, Color border, Color topShine, Color cornerAcc, bool glow)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var px = new Color[w * h];

            for (int y = 0; y < h; y++)
            {
                float ny = (float)y / h;
                for (int x = 0; x < w; x++)
                {
                    int idx = y * w + x;

                    // Chamfer corner check
                    bool cut = false;
                    if (x < chamfer && y < chamfer && (chamfer - x + chamfer - y > chamfer + 1)) cut = true;
                    else if (x >= w - chamfer && y < chamfer && (x - (w - chamfer - 1) + chamfer - y > chamfer + 1)) cut = true;
                    else if (x < chamfer && y >= h - chamfer && (chamfer - x + y - (h - chamfer - 1) > chamfer + 1)) cut = true;
                    else if (x >= w - chamfer && y >= h - chamfer && (x - (w - chamfer - 1) + y - (h - chamfer - 1) > chamfer + 1)) cut = true;

                    if (cut)
                    {
                        px[idx] = Color.clear;
                        continue;
                    }

                    bool isBorder = (x <= 1 || x >= w - 2 || y <= 1 || y >= h - 2);
                    bool isTopBevel = (y >= h - 4 && y < h - 2 && x > chamfer && x < w - chamfer);
                    bool isCorner = ((x <= chamfer + 2 || x >= w - chamfer - 3) && (y <= chamfer + 2 || y >= h - chamfer - 3));

                    if (isCorner && isBorder)
                    {
                        px[idx] = cornerAcc;
                    }
                    else if (isTopBevel)
                    {
                        px[idx] = topShine;
                    }
                    else if (isBorder)
                    {
                        px[idx] = border;
                    }
                    else
                    {
                        Color fill = Color.Lerp(bgBot, bgTop, ny);
                        if (glow)
                        {
                            float midFlare = Mathf.Exp(-Mathf.Pow((ny - 0.5f) * 5f, 2f)) * 0.12f;
                            fill += new Color(midFlare * 1.4f, midFlare * 1.1f, midFlare * 0.3f, 0f);
                        }
                        px[idx] = fill;
                    }
                }
            }

            tex.SetPixels(px);
            tex.Apply();
            SaveSprite(path, tex);
        }

        private static void CreateHexButton(string path, int w, int h, int chamfer, Color bg, Color border)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var px = new Color[w * h];

            for (int y = 0; y < h; y++)
            {
                float ny = (float)y / h;
                for (int x = 0; x < w; x++)
                {
                    int idx = y * w + x;

                    bool cut = false;
                    if (x < chamfer && y < chamfer && (chamfer - x + chamfer - y > chamfer + 1)) cut = true;
                    else if (x >= w - chamfer && y < chamfer && (x - (w - chamfer - 1) + chamfer - y > chamfer + 1)) cut = true;
                    else if (x < chamfer && y >= h - chamfer && (chamfer - x + y - (h - chamfer - 1) > chamfer + 1)) cut = true;
                    else if (x >= w - chamfer && y >= h - chamfer && (x - (w - chamfer - 1) + y - (h - chamfer - 1) > chamfer + 1)) cut = true;

                    if (cut)
                    {
                        px[idx] = Color.clear;
                        continue;
                    }

                    bool isBorder = (x <= 1 || x >= w - 2 || y <= 1 || y >= h - 2);
                    bool isInnerBorder = (x == 3 || x == w - 4 || y == 3 || y == h - 4);

                    if (isBorder)
                    {
                        px[idx] = border;
                    }
                    else if (isInnerBorder)
                    {
                        px[idx] = new Color(border.r, border.g, border.b, 0.35f);
                    }
                    else
                    {
                        Color fill = Color.Lerp(bg * 0.85f, bg * 1.15f, ny);
                        px[idx] = fill;
                    }
                }
            }

            tex.SetPixels(px);
            tex.Apply();
            SaveSprite(path, tex);
        }
        #endregion

        #region 2. Original Vector Menu Icons
        private static void GenerateMenuIcons()
        {
            int sz = 64;

            // A. PvP Swords Icon (Twin crossed blades with golden glow)
            GenerateIcon($"{ArtDir}/Icon_PvP.png", sz, (x, y) =>
            {
                // Blade 1: bottom-left to top-right
                float d1 = Mathf.Abs((x - y));
                // Blade 2: top-left to bottom-right
                float d2 = Mathf.Abs((x - (sz - 1 - y)));

                bool inBlade1 = (d1 <= 2.2f && x >= 12 && x <= sz - 13);
                bool inBlade2 = (d2 <= 2.2f && x >= 12 && x <= sz - 13);

                // Crossguards
                bool guard1 = (Mathf.Abs(x - 18) <= 4 && Mathf.Abs(y - 18) <= 4 && Mathf.Abs((x - 18) + (y - 18)) <= 2.5f);
                bool guard2 = (Mathf.Abs(x - (sz - 19)) <= 4 && Mathf.Abs(y - 18) <= 4 && Mathf.Abs((x - (sz - 19)) - (y - 18)) <= 2.5f);

                // Central clash spark
                float cDist = Mathf.Sqrt((x - 31.5f) * (x - 31.5f) + (y - 31.5f) * (y - 31.5f));
                if (cDist <= 4f) return Color.white;

                if (guard1 || guard2) return new Color(0.98f, 0.40f, 0.20f, 1f); // Ruby crossguard
                if (inBlade1 || inBlade2) return new Color(1.0f, 0.85f, 0.32f, 1f); // Radiant gold blade

                return Color.clear;
            });

            // B. Random Matchmaking Icon (Mystical Runic Polyhedron)
            GenerateIcon($"{ArtDir}/Icon_RandomMatch.png", sz, (x, y) =>
            {
                float cx = 31.5f, cy = 31.5f;
                float r = 22f;
                float dx = x - cx, dy = y - cy;
                float d = Mathf.Sqrt(dx * dx + dy * dy);

                // Hexagonal boundary
                float angle = Mathf.Atan2(dy, dx);
                float hexR = r / Mathf.Cos(angle - Mathf.Round(angle / (Mathf.PI / 3f)) * (Mathf.PI / 3f));

                if (d <= hexR && d >= hexR - 2.5f) return new Color(0.30f, 0.75f, 1.0f, 1f); // Cyan rim

                // Inner radial ribs
                for (int i = 0; i < 6; i++)
                {
                    float a = i * Mathf.PI / 3f;
                    float rx = Mathf.Cos(a) * (dx) + Mathf.Sin(a) * (dy);
                    float ry = -Mathf.Sin(a) * (dx) + Mathf.Cos(a) * (dy);
                    if (Mathf.Abs(ry) <= 1.2f && rx >= 0 && rx <= hexR) return new Color(0.50f, 0.88f, 1.0f, 0.9f);
                }

                // Core lightning diamond
                if (Mathf.Abs(dx) + Mathf.Abs(dy) <= 7f) return Color.white;
                if (d <= hexR) return new Color(0.08f, 0.22f, 0.42f, 0.75f);

                return Color.clear;
            });

            // C. Character Selection Icon (Champion Helmet Crest)
            GenerateIcon($"{ArtDir}/Icon_CharSelect.png", sz, (x, y) =>
            {
                float dx = Mathf.Abs(x - 31.5f);
                float dy = y - 28f;

                // Crown plume
                if (y >= 44 && y <= 54 && dx <= (54 - y) * 0.7f) return new Color(1.0f, 0.90f, 0.35f, 1f);

                // Helmet visor
                if (y >= 26 && y <= 42 && dx <= 18f && dx >= 14f) return new Color(0.98f, 0.80f, 0.28f, 1f);
                // Visor slit
                if (y >= 32 && y <= 35 && dx <= 14f) return new Color(0.15f, 0.85f, 1.0f, 1f);

                // Faceplate
                if (y >= 16 && y <= 26 && dx <= (y - 12) * 1.1f && (dx >= (y - 14) * 0.8f || Mathf.Abs(dx - 5f) <= 1.2f))
                    return new Color(0.98f, 0.78f, 0.25f, 1f);

                return Color.clear;
            });

            // D. Settings Icon (Precision 8-Toothed Cogwheel)
            GenerateIcon($"{ArtDir}/Icon_Settings.png", sz, (x, y) =>
            {
                float cx = 31.5f, cy = 31.5f;
                float dx = x - cx, dy = y - cy;
                float d = Mathf.Sqrt(dx * dx + dy * dy);

                // Teeth
                float angle = Mathf.Atan2(dy, dx);
                float tooth = Mathf.Cos(angle * 8f);
                float outerR = 22f + tooth * 3.5f;

                if (d <= outerR && d >= 14f) return new Color(0.55f, 0.70f, 0.90f, 1f); // Steel gear
                if (d < 14f && d >= 9f) return new Color(0.12f, 0.16f, 0.25f, 1f);      // Recessed groove
                if (d < 9f && d >= 5f) return new Color(0.20f, 0.80f, 1.0f, 1f);       // Cyan core gem
                if (d < 5f) return Color.white;

                return Color.clear;
            });

            // E. Exit Game Icon (Dimensional Portal Gate)
            GenerateIcon($"{ArtDir}/Icon_Exit.png", sz, (x, y) =>
            {
                // Portal Arch
                float dx = Mathf.Abs(x - 31.5f);
                bool inArch = (dx <= 18f && y >= 14 && y <= 48 && (dx >= 14f || y >= 44));
                // Base
                bool inBase = (dx <= 22f && y >= 10 && y <= 14);

                // Exit arrow
                bool arrowStem = (y >= 26 && y <= 32 && x >= 22 && x <= 36);
                bool arrowHead = (x >= 32 && x <= 42 && Mathf.Abs(y - 29f) <= (42 - x));

                if (arrowStem || arrowHead) return new Color(1.0f, 0.35f, 0.30f, 1f); // Crimson exit arrow
                if (inArch || inBase) return new Color(0.98f, 0.80f, 0.28f, 1f);      // Gold portal arch

                return Color.clear;
            });

            // F. Navigation Chevrons (< and >)
            GenerateIcon($"{ArtDir}/Icon_Chevron_Left.png", 48, (x, y) =>
            {
                float cy = 23.5f;
                float dy = Mathf.Abs(y - cy);
                float targetX = 18f + dy * 0.9f; // Points left (<)
                if (Mathf.Abs(x - targetX) <= 3f && dy <= 16f) return new Color(1.0f, 0.85f, 0.30f, 1f);
                return Color.clear;
            });

            GenerateIcon($"{ArtDir}/Icon_Chevron_Right.png", 48, (x, y) =>
            {
                float cy = 23.5f;
                float dy = Mathf.Abs(y - cy);
                float targetX = 30f - dy * 0.9f; // Points right (>)
                if (Mathf.Abs(x - targetX) <= 3f && dy <= 16f) return new Color(0.35f, 0.85f, 1.0f, 1f); // Radiant cyan
                return Color.clear;
            });

            // G. Remove Badge (Small circular X)
            GenerateIcon($"{ArtDir}/Icon_Remove_Badge.png", 24, (x, y) =>
            {
                float d = Mathf.Sqrt((x - 11.5f) * (x - 11.5f) + (y - 11.5f) * (y - 11.5f));
                if (d <= 11f && d >= 9.5f) return Color.white;
                if (d < 9.5f)
                {
                    // X Mark
                    float dx = Mathf.Abs(x - 11.5f);
                    float dy = Mathf.Abs(y - 11.5f);
                    if (Mathf.Abs(dx - dy) <= 1.2f && dx <= 6f) return Color.white;
                    return new Color(0.85f, 0.20f, 0.20f, 1f);
                }
                return Color.clear;
            });
        }

        private static void GenerateIcon(string path, int sz, System.Func<int, int, Color> pixelFunc)
        {
            var tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
            var px = new Color[sz * sz];
            for (int y = 0; y < sz; y++)
            {
                for (int x = 0; x < sz; x++)
                {
                    px[y * sz + x] = pixelFunc(x, y);
                }
            }
            tex.SetPixels(px);
            tex.Apply();
            SaveSprite(path, tex);
        }
        #endregion

        #region 3. Ability Card Components & 8 Archetype Icons
        private static void GenerateAbilityCardAssets()
        {
            // A. Ability Card Frame (320x420 High-Resolution for Mobile)
            int w = 320;
            int h = 420;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var px = new Color[w * h];

            Color bgTop = new Color(0.10f, 0.13f, 0.22f, 0.96f);
            Color bgBot = new Color(0.06f, 0.08f, 0.14f, 0.96f);
            Color border = new Color(0.92f, 0.78f, 0.32f, 1f);
            Color innerBorder = new Color(0.35f, 0.50f, 0.75f, 0.5f);

            int chamfer = 16;
            for (int y = 0; y < h; y++)
            {
                float ny = (float)y / h;
                for (int x = 0; x < w; x++)
                {
                    int idx = y * w + x;
                    bool cut = false;
                    if (x < chamfer && y < chamfer && (chamfer - x + chamfer - y > chamfer + 1)) cut = true;
                    else if (x >= w - chamfer && y < chamfer && (x - (w - chamfer - 1) + chamfer - y > chamfer + 1)) cut = true;
                    else if (x < chamfer && y >= h - chamfer && (chamfer - x + y - (h - chamfer - 1) > chamfer + 1)) cut = true;
                    else if (x >= w - chamfer && y >= h - chamfer && (x - (w - chamfer - 1) + y - (h - chamfer - 1) > chamfer + 1)) cut = true;

                    if (cut)
                    {
                        px[idx] = Color.clear;
                        continue;
                    }

                    bool isBorder = (x <= 1 || x >= w - 2 || y <= 1 || y >= h - 2);
                    bool isInnerLine = (x == 4 || x == w - 5 || y == 4 || y == h - 5);

                    if (isBorder) px[idx] = border;
                    else if (isInnerLine) px[idx] = innerBorder;
                    else px[idx] = Color.Lerp(bgBot, bgTop, ny);
                }
            }
            tex.SetPixels(px);
            tex.Apply();
            SaveSprite($"{ArtDir}/UI_Card_Ability_Frame.png", tex);

            // B. 8 Original High-Resolution Ability Archetype Icons (96x96)
            int sz = 96;
            float c = (sz - 1) * 0.5f;

            // 1. Shield (Aegis Bastion / Guard)
            GenerateIcon($"{ArtDir}/Icon_Ability_Shield.png", sz, (x, y) =>
            {
                float dx = Mathf.Abs(x - c);
                float dy = y - c;
                float shieldW = y >= c ? 34f : 34f - (c - y) * 1.1f;
                if (dx <= shieldW && y >= 14 && y <= sz - 14)
                {
                    // Golden rim
                    if (dx >= shieldW - 4.5f || y <= 19 || y >= sz - 19) return new Color(1.0f, 0.85f, 0.30f, 1f);
                    // Central golden cross
                    if (dx <= 4.5f || Mathf.Abs(y - c) <= 4.5f) return new Color(1.0f, 0.95f, 0.65f, 1f);
                    // Azure core
                    return new Color(0.15f, 0.38f, 0.70f, 0.95f);
                }
                return Color.clear;
            });

            // 2. Strike (Blade Slash / Primary Attack)
            GenerateIcon($"{ArtDir}/Icon_Ability_Strike.png", sz, (x, y) =>
            {
                float dist = Mathf.Abs((x - y));
                if (dist <= 5.5f && x >= 16 && x <= sz - 16)
                {
                    if (dist <= 2.5f) return Color.white;
                    return new Color(1.0f, 0.80f, 0.25f, 1f); // Golden blade edge
                }
                // Radiant cyan energy arcs
                float arc = Mathf.Abs(Mathf.Sqrt((x - 24) * (x - 24) + (y - (sz - 24)) * (y - (sz - 24))) - 46f);
                if (arc <= 3.5f && x >= 32 && y <= sz - 32) return new Color(0.25f, 0.85f, 1.0f, 0.85f);
                return Color.clear;
            });

            // 3. Sun (Solar Radiance / Holy Aura)
            GenerateIcon($"{ArtDir}/Icon_Ability_Sun.png", sz, (x, y) =>
            {
                float dx = x - c, dy = y - c;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                if (d <= 15f) return Color.white; // Brilliant white core
                if (d <= 23f) return new Color(1.0f, 0.88f, 0.30f, 1f); // Sun disk
                // 8 radiating sunburst rays
                float a = Mathf.Atan2(dy, dx);
                float ray = Mathf.Cos(a * 8f);
                if (ray > 0.80f && d <= 42f) return new Color(1.0f, 0.70f, 0.20f, Mathf.Clamp01((42f - d) / 16f));
                return Color.clear;
            });

            // 4. Shockwave (Earth Shatter / Ultimate Impact)
            GenerateIcon($"{ArtDir}/Icon_Ability_Shockwave.png", sz, (x, y) =>
            {
                float dy = y - 26f;
                float d = Mathf.Sqrt((x - c) * (x - c) + dy * dy);
                // 3 concentric wave arcs
                for (int i = 1; i <= 3; i++)
                {
                    float targetR = i * 18f;
                    if (Mathf.Abs(d - targetR) <= 3.5f && y >= 24) return new Color(1.0f, 0.82f, 0.30f, 1f);
                }
                // Fissure base
                if (y <= 28 && y >= 18 && Mathf.Abs(x - c) <= 32f) return new Color(0.85f, 0.50f, 0.20f, 1f);
                return Color.clear;
            });

            // 5. Magic (Arcane Hexagram / Astral Blast)
            GenerateIcon($"{ArtDir}/Icon_Ability_Magic.png", sz, (x, y) =>
            {
                float dx = x - c, dy = y - c;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                if (d <= 35f && d >= 31f) return new Color(0.75f, 0.45f, 1.0f, 1f); // Violet circle
                if (Mathf.Abs(dx) + Mathf.Abs(dy) <= 22f && Mathf.Abs(dx) + Mathf.Abs(dy) >= 17f) return Color.white;
                if (d <= 11f) return new Color(0.35f, 0.90f, 1.0f, 1f);
                return Color.clear;
            });

            // 6. Flame (Inferno Blaze / Fireburst)
            GenerateIcon($"{ArtDir}/Icon_Ability_Flame.png", sz, (x, y) =>
            {
                float dx = Mathf.Abs(x - c);
                float dy = y - 16f;
                if (dy >= 0 && dy <= 68 && dx <= Mathf.Sqrt(dy) * 4.6f * (1f - dy / 72f))
                {
                    if (dx <= 6f && dy <= 34f) return Color.white;
                    if (dx <= 12f && dy <= 48f) return new Color(1.0f, 0.85f, 0.25f, 1f);
                    return new Color(0.95f, 0.25f, 0.15f, 1f);
                }
                return Color.clear;
            });

            // 7. Frost (Glacial Crystal / Ice Lance)
            GenerateIcon($"{ArtDir}/Icon_Ability_Frost.png", sz, (x, y) =>
            {
                float dx = x - c, dy = y - c;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                if (d <= 35f)
                {
                    for (int i = 0; i < 6; i++)
                    {
                        float a = i * Mathf.PI / 3f;
                        float rx = Mathf.Cos(a) * dx + Mathf.Sin(a) * dy;
                        float ry = -Mathf.Sin(a) * dx + Mathf.Cos(a) * dy;
                        if (Mathf.Abs(ry) <= 2.2f && rx >= -32 && rx <= 32) return Color.white;
                        if (Mathf.Abs(rx - 22) <= 5.5f && Mathf.Abs(ry) <= 5.5f) return new Color(0.35f, 0.85f, 1.0f, 1f);
                    }
                }
                return Color.clear;
            });

            // 8. Shadow (Dual Nightblades / Shadowstep)
            GenerateIcon($"{ArtDir}/Icon_Ability_Shadow.png", sz, (x, y) =>
            {
                float d1 = Mathf.Abs((x - 34) - (y - c) * 0.45f);
                float d2 = Mathf.Abs((x - 62) + (y - c) * 0.45f);
                if ((d1 <= 3.2f || d2 <= 3.2f) && y >= 22 && y <= sz - 22) return new Color(0.85f, 0.35f, 1.0f, 1f);
                if (Mathf.Abs(x - c) <= 9f && y >= 40 && y <= 56) return new Color(0.25f, 0.08f, 0.40f, 0.95f);
                return Color.clear;
            });
        }
        #endregion

        #region 4. Combat Attributes Panel & Health Bars
        private static void GenerateCombatAttributeAssets()
        {
            // A. Attributes Panel Frame (490x650 High-Resolution for Mobile)
            int w = 490;
            int h = 650;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var px = new Color[w * h];

            Color bgTop = new Color(0.09f, 0.12f, 0.18f, 0.96f);
            Color bgBot = new Color(0.05f, 0.07f, 0.12f, 0.96f);
            Color border = new Color(0.92f, 0.78f, 0.30f, 1f);
            Color innerGlow = new Color(0.30f, 0.45f, 0.70f, 0.45f);

            int chamfer = 16;
            for (int y = 0; y < h; y++)
            {
                float ny = (float)y / h;
                for (int x = 0; x < w; x++)
                {
                    int idx = y * w + x;
                    bool cut = false;
                    if (x < chamfer && y < chamfer && (chamfer - x + chamfer - y > chamfer + 1)) cut = true;
                    else if (x >= w - chamfer && y < chamfer && (x - (w - chamfer - 1) + chamfer - y > chamfer + 1)) cut = true;
                    else if (x < chamfer && y >= h - chamfer && (chamfer - x + y - (h - chamfer - 1) > chamfer + 1)) cut = true;
                    else if (x >= w - chamfer && y >= h - chamfer && (x - (w - chamfer - 1) + y - (h - chamfer - 1) > chamfer + 1)) cut = true;

                    if (cut)
                    {
                        px[idx] = Color.clear;
                        continue;
                    }

                    bool isBorder = (x <= 1 || x >= w - 2 || y <= 1 || y >= h - 2);
                    bool isInnerLine = (x == 4 || x == w - 5 || y == 4 || y == h - 5);
                    bool isHeaderDivider = (y == 565 && x > 18 && x < w - 18);

                    if (isBorder) px[idx] = border;
                    else if (isHeaderDivider) px[idx] = new Color(border.r, border.g, border.b, 0.7f);
                    else if (isInnerLine) px[idx] = innerGlow;
                    else px[idx] = Color.Lerp(bgBot, bgTop, ny);
                }
            }
            tex.SetPixels(px);
            tex.Apply();
            SaveSprite($"{ArtDir}/UI_Panel_Attributes_Frame.png", tex);

            // B. 4 High-Resolution Stat Icons (64x64 for Mobile)
            int sz = 64;

            // Heart (HP)
            GenerateIcon($"{ArtDir}/Icon_Stat_Heart.png", sz, (x, y) =>
            {
                float dx = (x - 31.5f) / 20f;
                float dy = (y - 34f) / 20f;
                float f = dx * dx + Mathf.Pow(dy - Mathf.Sqrt(Mathf.Abs(dx)), 2f);
                if (f <= 0.85f)
                {
                    if (f >= 0.70f) return new Color(1.0f, 0.85f, 0.30f, 1f); // Gold outline
                    return new Color(0.95f, 0.20f, 0.22f, 1f);                 // Ruby red
                }
                return Color.clear;
            });

            // Swords (ATK)
            GenerateIcon($"{ArtDir}/Icon_Stat_Swords.png", sz, (x, y) =>
            {
                float d1 = Mathf.Abs(x - y);
                float d2 = Mathf.Abs(x - (sz - 1 - y));
                if ((d1 <= 2.5f || d2 <= 2.5f) && x >= 10 && x <= sz - 11) return new Color(1.0f, 0.82f, 0.25f, 1f);
                return Color.clear;
            });

            // Shield (DEF)
            GenerateIcon($"{ArtDir}/Icon_Stat_Shield.png", sz, (x, y) =>
            {
                float dx = Mathf.Abs(x - 31.5f);
                float wCurv = y >= 28 ? 20f : 20f - (28 - y) * 0.9f;
                if (dx <= wCurv && y >= 10 && y <= 54)
                {
                    if (dx >= wCurv - 3f || y <= 12 || y >= 52) return new Color(1.0f, 0.85f, 0.30f, 1f);
                    return new Color(0.20f, 0.60f, 0.95f, 1f); // Azure steel
                }
                return Color.clear;
            });

            // Boot (SPD)
            GenerateIcon($"{ArtDir}/Icon_Stat_Boot.png", sz, (x, y) =>
            {
                bool inLeg = (x >= 24 && x <= 36 && y >= 24 && y <= 54);
                bool inFoot = (x >= 14 && x <= 50 && y >= 12 && y <= 24);
                bool inWing = (x >= 36 && x <= 54 && y >= 36 && y <= 50 && (x - 36) >= (y - 36));
                if (inWing) return new Color(0.30f, 0.90f, 1.0f, 1f);
                if (inLeg || inFoot) return new Color(1.0f, 0.85f, 0.30f, 1f);
                return Color.clear;
            });

            // C. Stat Bar Track & Gradient Fills (530x50 High-Definition)
            int bw = 530, bh = 50;

            // Track Background
            CreateStatBarTrack($"{ArtDir}/UI_StatBar_Track.png", bw, bh);

            // HP Fill (Crimson to Orange)
            CreateStatBarFill($"{ArtDir}/UI_StatBar_Fill_Red.png", bw, bh, new Color(0.85f, 0.15f, 0.18f), new Color(1.0f, 0.35f, 0.20f));

            // ATK Fill (Amber to Gold)
            CreateStatBarFill($"{ArtDir}/UI_StatBar_Fill_Orange.png", bw, bh, new Color(0.95f, 0.45f, 0.10f), new Color(1.0f, 0.78f, 0.22f));

            // DEF Fill (Cobalt to Azure)
            CreateStatBarFill($"{ArtDir}/UI_StatBar_Fill_Blue.png", bw, bh, new Color(0.12f, 0.45f, 0.95f), new Color(0.25f, 0.75f, 1.0f));

            // SPD Fill (Teal to Mint)
            CreateStatBarFill($"{ArtDir}/UI_StatBar_Fill_Teal.png", bw, bh, new Color(0.10f, 0.75f, 0.75f), new Color(0.35f, 0.95f, 0.80f));
        }

        private static void CreateStatBarTrack(string path, int w, int h)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var px = new Color[w * h];
            Color bg = new Color(0.05f, 0.07f, 0.11f, 0.95f);
            Color border = new Color(0.25f, 0.32f, 0.45f, 1f);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    bool isBorder = (x == 0 || x == w - 1 || y == 0 || y == h - 1);
                    px[y * w + x] = isBorder ? border : bg;
                }
            }
            tex.SetPixels(px);
            tex.Apply();
            SaveSprite(path, tex);
        }

        private static void CreateStatBarFill(string path, int w, int h, Color colLeft, Color colRight)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var px = new Color[w * h];

            for (int y = 0; y < h; y++)
            {
                float ny = (float)y / h;
                for (int x = 0; x < w; x++)
                {
                    float nx = (float)x / w;
                    Color c = Color.Lerp(colLeft, colRight, nx);
                    // Top highlight specular line
                    if (y >= h - 3) c = Color.Lerp(c, Color.white, 0.55f);
                    // Bottom shadow bevel
                    else if (y <= 2) c *= 0.75f;
                    c.a = 1f;
                    px[y * w + x] = c;
                }
            }
            tex.SetPixels(px);
            tex.Apply();
            SaveSprite(path, tex);
        }
        #endregion

        #region 5. Team Selection Dashboard & Slots
        private static void GenerateTeamBarAssets()
        {
            // A. Team Bar Frame (920x380) - High-DPI Mobile & Desktop Dashboard
            int w = 920;
            int h = 380;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var px = new Color[w * h];

            Color bgTop = new Color(0.09f, 0.12f, 0.20f, 0.96f);
            Color bgBot = new Color(0.05f, 0.07f, 0.13f, 0.96f);
            Color border = new Color(0.92f, 0.78f, 0.30f, 1f);
            Color innerBevel = new Color(0.35f, 0.50f, 0.75f, 0.55f);

            int chamfer = 16;
            for (int y = 0; y < h; y++)
            {
                float ny = (float)y / h;
                for (int x = 0; x < w; x++)
                {
                    int idx = y * w + x;
                    bool cut = false;
                    if (x < chamfer && y < chamfer && (chamfer - x + chamfer - y > chamfer + 1)) cut = true;
                    else if (x >= w - chamfer && y < chamfer && (x - (w - chamfer - 1) + chamfer - y > chamfer + 1)) cut = true;
                    else if (x < chamfer && y >= h - chamfer && (chamfer - x + y - (h - chamfer - 1) > chamfer + 1)) cut = true;
                    else if (x >= w - chamfer && y >= h - chamfer && (x - (w - chamfer - 1) + y - (h - chamfer - 1) > chamfer + 1)) cut = true;

                    if (cut)
                    {
                        px[idx] = Color.clear;
                        continue;
                    }

                    bool isBorder = (x <= 2 || x >= w - 3 || y <= 2 || y >= h - 3);
                    bool isInnerLine = (x == 5 || x == w - 6 || y == 5 || y == h - 6);

                    // Luxury golden corner brackets
                    bool isCornerBracket = ((x <= 26 || x >= w - 27) && (y <= 26 || y >= h - 27) && (x <= 5 || x >= w - 6 || y <= 5 || y >= h - 6));

                    if (isBorder || isCornerBracket) px[idx] = border;
                    else if (isInnerLine) px[idx] = innerBevel;
                    else px[idx] = Color.Lerp(bgBot, bgTop, ny);
                }
            }
            tex.SetPixels(px);
            tex.Apply();
            SaveSprite($"{ArtDir}/UI_Panel_TeamBar_Frame.png", tex);

            // B. Smart Status Message Banner Frame (880x94)
            int bw = 880, bh = 94;
            var bTex = new Texture2D(bw, bh, TextureFormat.RGBA32, false);
            var bPx = new Color[bw * bh];
            int bChamfer = 12;

            for (int y = 0; y < bh; y++)
            {
                float bny = (float)y / bh;
                for (int x = 0; x < bw; x++)
                {
                    int idx = y * bw + x;
                    bool cut = false;
                    if (x < bChamfer && y < bChamfer && (bChamfer - x + bChamfer - y > bChamfer + 1)) cut = true;
                    else if (x >= bw - bChamfer && y < bChamfer && (x - (bw - bChamfer - 1) + bChamfer - y > bChamfer + 1)) cut = true;
                    else if (x < bChamfer && y >= bh - bChamfer && (bChamfer - x + y - (bh - bChamfer - 1) > bChamfer + 1)) cut = true;
                    else if (x >= bw - bChamfer && y >= bh - bChamfer && (x - (bw - bChamfer - 1) + y - (bh - bChamfer - 1) > bChamfer + 1)) cut = true;

                    if (cut)
                    {
                        bPx[idx] = Color.clear;
                        continue;
                    }

                    bool isBorder = (x <= 2 || x >= bw - 3 || y <= 2 || y >= bh - 3);
                    bool isTopShine = (y >= bh - 4 && x > 15 && x < bw - 16);
                    bool isCornerAcc = ((x <= 16 || x >= bw - 17) && (y <= 16 || y >= bh - 17));

                    if (isBorder || isCornerAcc) bPx[idx] = new Color(0.95f, 0.82f, 0.32f, 1f);
                    else if (isTopShine) bPx[idx] = new Color(0.60f, 0.80f, 1.0f, 0.7f);
                    else bPx[idx] = Color.Lerp(new Color(0.06f, 0.08f, 0.14f, 0.95f), new Color(0.11f, 0.15f, 0.24f, 0.95f), bny);
                }
            }
            bTex.SetPixels(bPx);
            bTex.Apply();
            SaveSprite($"{ArtDir}/UI_Panel_Status_Banner.png", bTex);

            // C. Status Icon (64x64) - Winged Shield Star Emblem
            int sz = 64;
            GenerateIcon($"{ArtDir}/Icon_Team_Status.png", sz, (x, y) =>
            {
                float dx = Mathf.Abs(x - 31.5f);
                float dy = y - 31.5f;
                // Circular diamond medal
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                if (dist <= 24f)
                {
                    if (dist >= 20f) return new Color(1.0f, 0.85f, 0.30f, 1f); // Gold rim
                    if (dist <= 8f) return Color.white;                        // Radiant core star
                    return new Color(0.20f, 0.75f, 1.0f, 1f);                  // Azure gem
                }
                // Small wing points
                if (y >= 26 && y <= 38 && dx >= 22f && dx <= 30f && (30f - dx) >= Mathf.Abs(y - 32f))
                {
                    return new Color(1.0f, 0.85f, 0.30f, 1f);
                }
                return Color.clear;
            });

            // D. Champion Slot Frame (130x170) - Chunky Touch Targets
            int sw = 130, sh = 170;
            var sTex = new Texture2D(sw, sh, TextureFormat.RGBA32, false);
            var sPx = new Color[sw * sh];
            int sChamfer = 10;

            for (int y = 0; y < sh; y++)
            {
                float sny = (float)y / sh;
                for (int x = 0; x < sw; x++)
                {
                    int idx = y * sw + x;
                    bool cut = false;
                    if (x < sChamfer && y < sChamfer && (sChamfer - x + sChamfer - y > sChamfer + 1)) cut = true;
                    else if (x >= sw - sChamfer && y < sChamfer && (x - (sw - sChamfer - 1) + sChamfer - y > sChamfer + 1)) cut = true;
                    else if (x < sChamfer && y >= sh - sChamfer && (sChamfer - x + y - (sh - sChamfer - 1) > sChamfer + 1)) cut = true;
                    else if (x >= sw - sChamfer && y >= sh - sChamfer && (x - (sw - sChamfer - 1) + y - (sh - sChamfer - 1) > sChamfer + 1)) cut = true;

                    if (cut)
                    {
                        sPx[idx] = Color.clear;
                        continue;
                    }

                    bool isBorder = (x <= 2 || x >= sw - 3 || y <= 2 || y >= sh - 3);
                    bool isInnerBevel = (x == 4 || x == sw - 5 || y == 4 || y == sh - 5);
                    if (isBorder) sPx[idx] = new Color(0.95f, 0.80f, 0.30f, 1f); // Gold
                    else if (isInnerBevel) sPx[idx] = new Color(0.35f, 0.55f, 0.75f, 0.6f);
                    else sPx[idx] = Color.Lerp(new Color(0.06f, 0.08f, 0.14f, 0.95f), new Color(0.10f, 0.14f, 0.22f, 0.95f), sny);
                }
            }
            sTex.SetPixels(sPx);
            sTex.Apply();
            SaveSprite($"{ArtDir}/UI_Slot_Frame.png", sTex);
        }
        #endregion

        #region 6. Original CLASHURA Crest & Commander Frame
        private static void GenerateLogoAndBadges()
        {
            // A. Original Winged Crest Emblem (320x100)
            int w = 320, h = 100;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var px = new Color[w * h];
            for (int i = 0; i < px.Length; i++) px[i] = Color.clear;

            float cx = w * 0.5f, cy = 35f;

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float dx = Mathf.Abs(x - cx);
                    float dy = y - cy;

                    // Central diamond gem
                    float cDiam = dx * 1.2f + Mathf.Abs(dy) * 0.8f;
                    if (cDiam <= 24f)
                    {
                        if (cDiam >= 20f) px[y * w + x] = new Color(1.0f, 0.85f, 0.30f, 1f); // Gold rim
                        else px[y * w + x] = new Color(0.20f, 0.80f, 1.0f, 1f);             // Cyan core star
                        continue;
                    }

                    // Flanking golden warrior wings (3 tiers)
                    for (int wing = 0; wing < 3; wing++)
                    {
                        float wingStartX = 20f + wing * 15f;
                        float wingEndX = 140f - wing * 20f;
                        float wingY = 15f + wing * 20f + (dx - wingStartX) * 0.35f;

                        if (dx >= wingStartX && dx <= wingEndX && Mathf.Abs(y - wingY) <= (4.5f - wing * 0.8f))
                        {
                            px[y * w + x] = Color.Lerp(new Color(1.0f, 0.88f, 0.35f, 1f), new Color(0.85f, 0.55f, 0.15f, 1f), dx / 150f);
                        }
                    }

                    // Central upward spear tip
                    if (y >= 40 && y <= 85 && dx <= (85 - y) * 0.3f)
                    {
                        px[y * w + x] = (dx <= 1.5f) ? Color.white : new Color(1.0f, 0.85f, 0.30f, 1f);
                    }
                }
            }
            tex.SetPixels(px);
            tex.Apply();
            SaveSprite($"{ArtDir}/Logo_Clashura_Emblem.png", tex);

            // B. Commander Badge Frame (360x86) - Extra Chunky User Profile Bar
            int bw = 360, bh = 86;
            var bTex = new Texture2D(bw, bh, TextureFormat.RGBA32, false);
            var bPx = new Color[bw * bh];

            for (int y = 0; y < bh; y++)
            {
                float ny = (float)y / bh;
                for (int x = 0; x < bw; x++)
                {
                    int idx = y * bw + x;
                    int cut = 10;
                    bool inTL = (x < cut && y > (bh - 1 - cut + x));
                    bool inBR = (x > (bw - 1 - cut) && y < (cut - (bw - 1 - x)));

                    if (inTL || inBR)
                    {
                        bPx[idx] = Color.clear;
                        continue;
                    }

                    bool isBorder = (x <= 2 || x >= bw - 3 || y <= 2 || y >= bh - 3);
                    if (isBorder) bPx[idx] = new Color(0.95f, 0.80f, 0.28f, 1f);
                    else bPx[idx] = Color.Lerp(new Color(0.07f, 0.09f, 0.15f, 0.94f), new Color(0.12f, 0.16f, 0.25f, 0.94f), ny);
                }
            }
            bTex.SetPixels(bPx);
            bTex.Apply();
            SaveSprite($"{ArtDir}/Badge_Commander_Frame.png", bTex);

            // C. Bottom Center Winged Axe Emblem (200x60)
            int ew = 200, eh = 60;
            var eTex = new Texture2D(ew, eh, TextureFormat.RGBA32, false);
            var ePx = new Color[ew * eh];
            for (int i = 0; i < ePx.Length; i++) ePx[i] = Color.clear;
            float ecx = ew * 0.5f, ecy = eh * 0.5f;

            for (int y = 0; y < eh; y++)
            {
                for (int x = 0; x < ew; x++)
                {
                    float dx = Mathf.Abs(x - ecx);
                    float dy = y - ecy;

                    // Central glowing gold gem
                    float dGem = dx * 1.2f + Mathf.Abs(dy);
                    if (dGem <= 18f)
                    {
                        if (dGem >= 14f) ePx[y * ew + x] = new Color(1.0f, 0.88f, 0.35f, 1f);
                        else ePx[y * ew + x] = new Color(0.20f, 0.80f, 1.0f, 1f);
                        continue;
                    }

                    // Dual winged axes
                    bool inShaft = (dx <= 3f && Mathf.Abs(dy) <= 28f);
                    bool inBlade = (dx >= 12f && dx <= 38f && Mathf.Abs(dy) <= (38f - dx) * 0.9f);
                    // Wing filigree arcs
                    float wingDist = Mathf.Abs(dy - Mathf.Sin(dx * 0.08f) * 14f);
                    bool inWingArc = (dx >= 20f && dx <= 95f && wingDist <= 3f);

                    if (inShaft || inBlade) ePx[y * ew + x] = new Color(1.0f, 0.82f, 0.28f, 1f);
                    else if (inWingArc) ePx[y * ew + x] = Color.Lerp(new Color(1.0f, 0.85f, 0.35f, 1f), new Color(0.25f, 0.75f, 1.0f, 0.8f), dx / 95f);
                }
            }
            eTex.SetPixels(ePx);
            eTex.Apply();
            SaveSprite($"{ArtDir}/UI_Bottom_Center_Emblem.png", eTex);
        }
        #endregion

        #region 7. Confirmation Popup Assets
        private static void GeneratePopupAssets()
        {
            // A. Large Ornate Popup Frame (860 x 560 px)
            CreateChamferedFrame($"{ArtDir}/UI_Popup_Frame.png", 860, 560, 24,
                new Color(0.06f, 0.08f, 0.13f, 0.98f),   // Dark obsidian core
                new Color(0.12f, 0.16f, 0.25f, 0.98f),   // Subtle top sheen
                new Color(1.0f, 0.85f, 0.35f, 1f),       // Radiant Gold outer border
                new Color(1.0f, 0.92f, 0.55f, 0.9f),     // Specular top bevel
                new Color(1.0f, 0.80f, 0.20f, 1f),       // Warm corner accent
                true);                                   // Dual inner pinstripe

            // B. Warning Badge (64 x 64 px) - Radiant Amber Shield with Exclamation Mark
            int bw = 64, bh = 64;
            var wTex = new Texture2D(bw, bh, TextureFormat.RGBA32, false);
            var wPx = new Color[bw * bh];
            float cx = (bw - 1) * 0.5f, cy = (bh - 1) * 0.5f;

            for (int y = 0; y < bh; y++)
            {
                for (int x = 0; x < bw; x++)
                {
                    float dx = Mathf.Abs(x - cx);
                    float dy = y - cy;
                    // Shield shape
                    bool inShield = (dx <= 26f && dy <= 24f && (dy >= -26f + dx * 0.7f));
                    if (inShield)
                    {
                        float edgeDist = Mathf.Min(26f - dx, 24f - dy, dy - (-26f + dx * 0.7f));
                        if (edgeDist <= 3f)
                        {
                            wPx[y * bw + x] = new Color(1.0f, 0.82f, 0.30f, 1f); // Gold rim
                        }
                        else
                        {
                            wPx[y * bw + x] = new Color(0.85f, 0.40f, 0.12f, 0.95f); // Amber glow core
                        }

                        // Exclamation mark
                        if (dx <= 3f && dy >= -4f && dy <= 16f)
                        {
                            wPx[y * bw + x] = Color.white;
                        }
                        if (dx <= 3.5f && dy >= -16f && dy <= -10f)
                        {
                            wPx[y * bw + x] = Color.white;
                        }
                    }
                }
            }
            wTex.SetPixels(wPx);
            wTex.Apply();
            SaveSprite($"{ArtDir}/Icon_Popup_Warning.png", wTex);

            // C. Success Badge (64 x 64 px) - Radiant Emerald Winged Star with Checkmark
            var sTex = new Texture2D(bw, bh, TextureFormat.RGBA32, false);
            var sPx = new Color[bw * bh];
            for (int y = 0; y < bh; y++)
            {
                for (int x = 0; x < bw; x++)
                {
                    float dx = Mathf.Abs(x - cx);
                    float dy = Mathf.Abs(y - cy);
                    float dStar = dx + dy;
                    float dCircle = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));

                    if (dCircle <= 28f)
                    {
                        if (dCircle >= 24f) sPx[y * bw + x] = new Color(1.0f, 0.88f, 0.35f, 1f); // Gold rim
                        else sPx[y * bw + x] = new Color(0.12f, 0.65f, 0.32f, 0.95f); // Emerald core

                        // Checkmark
                        float nx = x - cx;
                        float ny = y - cy;
                        bool inCheckStem1 = (nx >= -14f && nx <= -2f && Mathf.Abs(ny - (-nx - 8f)) <= 3f);
                        bool inCheckStem2 = (nx >= -2f && nx <= 16f && Mathf.Abs(ny - (nx * 1.3f - 4f)) <= 3f);
                        if (inCheckStem1 || inCheckStem2)
                        {
                            sPx[y * bw + x] = Color.white;
                        }
                    }
                    else if (dStar <= 31f)
                    {
                        sPx[y * bw + x] = new Color(1.0f, 0.85f, 0.35f, 0.85f);
                    }
                }
            }
            sTex.SetPixels(sPx);
            sTex.Apply();
            SaveSprite($"{ArtDir}/Icon_Popup_Success.png", sTex);

            // D. OK Button (220 x 70 px)
            CreateChamferedFrame($"{ArtDir}/UI_Button_Popup_OK.png", 220, 70, 14,
                new Color(0.10f, 0.15f, 0.25f, 0.98f),
                new Color(0.16f, 0.24f, 0.38f, 0.98f),
                new Color(1.0f, 0.85f, 0.35f, 1f),
                new Color(1.0f, 0.92f, 0.55f, 0.9f),
                new Color(1.0f, 0.80f, 0.20f, 1f),
                false);
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
