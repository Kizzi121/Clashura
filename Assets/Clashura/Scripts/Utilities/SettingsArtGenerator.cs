#if UNITY_EDITOR
using System.IO;
using UnityEngine;
using UnityEditor;

namespace Clashura.Utilities
{
    /// <summary>
    /// Procedural Art Generator for the CLASHURA Settings System.
    /// Produces high-DPI original sprites styled in dark fantasy obsidian, gold bevels,
    /// and glowing jewel accents. Zero external or copied reference graphics used.
    /// </summary>
    public static class SettingsArtGenerator
    {
        private const string ArtDir = "Assets/Clashura/Art/UI/Settings";

        [MenuItem("Clashura/Generate Settings Art")]
        public static void GenerateAllArt()
        {
            if (!Directory.Exists(ArtDir))
            {
                Directory.CreateDirectory(ArtDir);
            }

            GeneratePanelsAndFrames();
            GenerateTabs();
            GenerateSliders();
            GenerateToggles();
            GenerateSteppers();
            GenerateTabIcons();

            AssetDatabase.Refresh();
            Debug.Log("[SettingsArtGenerator] All Settings UI Art Assets Generated Successfully!");
        }

        #region 1. Panels & Frames
        private static void GeneratePanelsAndFrames()
        {
            // Settings Main Background Panel (1200 x 800)
            CreateChamferedFrame($"{ArtDir}/UI_Settings_Frame.png", 1200, 800, 24,
                new Color(0.04f, 0.06f, 0.11f, 0.95f),
                new Color(0.08f, 0.12f, 0.20f, 0.95f),
                new Color(0.85f, 0.70f, 0.25f, 1f),
                new Color(1.0f, 0.90f, 0.45f, 0.9f),
                new Color(0.95f, 0.80f, 0.30f, 1f),
                false);

            // Reset Button (220 x 64 px, crimson-tinged obsidian with gold border)
            CreateChamferedFrame($"{ArtDir}/UI_Button_Reset.png", 220, 64, 14,
                new Color(0.24f, 0.08f, 0.08f, 0.98f),
                new Color(0.38f, 0.12f, 0.12f, 0.98f),
                new Color(1.0f, 0.45f, 0.35f, 1f),
                new Color(1.0f, 0.85f, 0.40f, 0.9f),
                new Color(1.0f, 0.60f, 0.30f, 1f),
                false);
        }
        #endregion

        #region 2. Category Tabs
        private static void GenerateTabs()
        {
            // Normal Tab (360 x 76 px)
            CreateChamferedFrame($"{ArtDir}/UI_Tab_Normal.png", 360, 76, 12,
                new Color(0.07f, 0.10f, 0.17f, 0.92f),
                new Color(0.12f, 0.16f, 0.26f, 0.92f),
                new Color(0.40f, 0.50f, 0.65f, 0.75f),
                new Color(0.60f, 0.70f, 0.85f, 0.6f),
                new Color(0.30f, 0.40f, 0.55f, 0.8f),
                false);

            // Selected Tab (360 x 76 px, radiant gold highlight & sapphire core)
            CreateChamferedFrame($"{ArtDir}/UI_Tab_Selected.png", 360, 76, 12,
                new Color(0.12f, 0.18f, 0.32f, 0.98f),
                new Color(0.18f, 0.28f, 0.46f, 0.98f),
                new Color(1.0f, 0.85f, 0.30f, 1f),
                new Color(1.0f, 0.95f, 0.60f, 1f),
                new Color(0.95f, 0.75f, 0.20f, 1f),
                true);
        }
        #endregion

        #region 3. Sliders
        private static void GenerateSliders()
        {
            // Slider Track (500 x 24 px)
            CreateChamferedFrame($"{ArtDir}/UI_Slider_Track.png", 500, 24, 6,
                new Color(0.05f, 0.07f, 0.12f, 0.98f),
                new Color(0.08f, 0.11f, 0.18f, 0.98f),
                new Color(0.35f, 0.45f, 0.60f, 0.85f),
                new Color(0.50f, 0.60f, 0.75f, 0.7f),
                new Color(0.25f, 0.35f, 0.48f, 0.9f),
                false);

            // Slider Fill (500 x 24 px, radiant gold/amber gradient)
            int fw = 500, fh = 24;
            Texture2D fillTex = new Texture2D(fw, fh, TextureFormat.RGBA32, false);
            Color[] fillPx = new Color[fw * fh];
            for (int y = 0; y < fh; y++)
            {
                float ty = (float)y / fh;
                for (int x = 0; x < fw; x++)
                {
                    float tx = (float)x / fw;
                    Color c = Color.Lerp(new Color(1.0f, 0.65f, 0.15f, 1f), new Color(1.0f, 0.90f, 0.40f, 1f), tx);
                    if (ty > 0.65f) c = Color.Lerp(c, Color.white, (ty - 0.65f) * 0.8f);
                    fillPx[y * fw + x] = c;
                }
            }
            fillTex.SetPixels(fillPx);
            fillTex.Apply();
            SaveSprite($"{ArtDir}/UI_Slider_Fill.png", fillTex);

            // Slider Knob (48 x 48 px, circular gold crest)
            int kw = 48, kh = 48;
            Texture2D knobTex = new Texture2D(kw, kh, TextureFormat.RGBA32, false);
            Color[] knobPx = new Color[kw * kh];
            float kR = 23f;
            Vector2 kCenter = new Vector2(kw * 0.5f, kh * 0.5f);
            for (int y = 0; y < kh; y++)
            {
                for (int x = 0; x < kw; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), kCenter);
                    if (d <= kR)
                    {
                        float rim = kR - d;
                        if (rim <= 3f)
                        {
                            knobPx[y * kw + x] = new Color(1.0f, 0.95f, 0.60f, 1f); // bright outer gold rim
                        }
                        else if (rim <= 6f)
                        {
                            knobPx[y * kw + x] = new Color(0.85f, 0.65f, 0.20f, 1f); // bevel
                        }
                        else
                        {
                            // Faceted jewel center
                            float nx = (x - kCenter.x) / kR;
                            float ny = (y - kCenter.y) / kR;
                            float light = Mathf.Clamp01(0.5f - nx * 0.3f + ny * 0.4f);
                            knobPx[y * kw + x] = Color.Lerp(new Color(0.12f, 0.20f, 0.35f, 1f), new Color(0.30f, 0.55f, 0.85f, 1f), light);
                        }
                    }
                    else if (d <= kR + 1f)
                    {
                        knobPx[y * kw + x] = new Color(1.0f, 0.85f, 0.30f, 0.4f);
                    }
                    else
                    {
                        knobPx[y * kw + x] = Color.clear;
                    }
                }
            }
            knobTex.SetPixels(knobPx);
            knobTex.Apply();
            SaveSprite($"{ArtDir}/UI_Slider_Knob.png", knobTex);
        }
        #endregion

        #region 4. Toggles
        private static void GenerateToggles()
        {
            // Toggle Track (110 x 48 px, pill-shaped)
            int tw = 110, th = 48;
            Texture2D tTex = new Texture2D(tw, th, TextureFormat.RGBA32, false);
            Color[] tPx = new Color[tw * th];
            float r = th * 0.5f;
            for (int y = 0; y < th; y++)
            {
                for (int x = 0; x < tw; x++)
                {
                    float dLeft = Vector2.Distance(new Vector2(x, y), new Vector2(r, r));
                    float dRight = Vector2.Distance(new Vector2(x, y), new Vector2(tw - r, r));
                    bool inPill = (x >= r && x <= tw - r) || (dLeft <= r) || (dRight <= r);

                    if (inPill)
                    {
                        float dBorder = Mathf.Min(x, tw - 1 - x, y, th - 1 - y);
                        if (x < r) dBorder = r - dLeft;
                        else if (x > tw - r) dBorder = r - dRight;

                        if (dBorder <= 2.5f)
                        {
                            tPx[y * tw + x] = new Color(1.0f, 0.85f, 0.35f, 1f); // gold border
                        }
                        else
                        {
                            tPx[y * tw + x] = new Color(0.08f, 0.11f, 0.18f, 0.95f); // dark interior
                        }
                    }
                    else
                    {
                        tPx[y * tw + x] = Color.clear;
                    }
                }
            }
            tTex.SetPixels(tPx);
            tTex.Apply();
            SaveSprite($"{ArtDir}/UI_Toggle_Track.png", tTex);

            // Toggle Knob (38 x 38 px)
            int kw = 38, kh = 38;
            Texture2D knobTex = new Texture2D(kw, kh, TextureFormat.RGBA32, false);
            Color[] knobPx = new Color[kw * kh];
            float kR = 18f;
            Vector2 kCenter = new Vector2(kw * 0.5f, kh * 0.5f);
            for (int y = 0; y < kh; y++)
            {
                for (int x = 0; x < kw; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), kCenter);
                    if (d <= kR)
                    {
                        float rim = kR - d;
                        if (rim <= 2f)
                            knobPx[y * kw + x] = new Color(1.0f, 0.95f, 0.65f, 1f);
                        else
                            knobPx[y * kw + x] = new Color(0.95f, 0.78f, 0.25f, 1f);
                    }
                    else if (d <= kR + 1f)
                        knobPx[y * kw + x] = new Color(1f, 0.85f, 0.3f, 0.4f);
                    else
                        knobPx[y * kw + x] = Color.clear;
                }
            }
            knobTex.SetPixels(knobPx);
            knobTex.Apply();
            SaveSprite($"{ArtDir}/UI_Toggle_Knob.png", knobTex);
        }
        #endregion

        #region 5. Steppers
        private static void GenerateSteppers()
        {
            // Stepper Value Box (260 x 58 px)
            CreateChamferedFrame($"{ArtDir}/UI_Stepper_Box.png", 260, 58, 10,
                new Color(0.06f, 0.09f, 0.16f, 0.95f),
                new Color(0.10f, 0.14f, 0.24f, 0.95f),
                new Color(0.85f, 0.70f, 0.25f, 1f),
                new Color(1.0f, 0.90f, 0.45f, 0.85f),
                new Color(0.90f, 0.75f, 0.20f, 1f),
                false);
        }
        #endregion

        #region 6. Category Icons
        private static void GenerateTabIcons()
        {
            // Audio Icon (Speaker / Winged Horn)
            GenerateThematicIcon($"{ArtDir}/Icon_Tab_Audio.png", (x, y, w, h) =>
            {
                float cx = w * 0.5f, cy = h * 0.5f;
                float nx = x - cx, ny = y - cy;
                bool body = (nx >= -18f && nx <= -4f && Mathf.Abs(ny) <= 12f);
                bool horn = (nx >= -4f && nx <= 14f && Mathf.Abs(ny) <= (nx + 4f) * 1.3f + 12f);
                bool wave1 = (nx >= 18f && nx <= 22f && Mathf.Abs(ny) <= 16f);
                bool wave2 = (nx >= 24f && nx <= 28f && Mathf.Abs(ny) <= 24f);
                return body || horn || wave1 || wave2;
            });

            // Graphics Icon (Monitor / Eye)
            GenerateThematicIcon($"{ArtDir}/Icon_Tab_Graphics.png", (x, y, w, h) =>
            {
                float cx = w * 0.5f, cy = h * 0.5f;
                float nx = x - cx, ny = y - cy;
                bool screen = (Mathf.Abs(nx) <= 24f && ny >= -14f && ny <= 18f);
                bool innerScreen = (Mathf.Abs(nx) <= 20f && ny >= -10f && ny <= 14f);
                bool stand = (Mathf.Abs(nx) <= 5f && ny >= -22f && ny <= -14f);
                bool basePl = (Mathf.Abs(nx) <= 16f && ny >= -26f && ny <= -22f);
                return (screen && !innerScreen) || stand || basePl || (Mathf.Abs(nx) <= 8f && Mathf.Abs(ny - 2f) <= 6f);
            });

            // Controls Icon (Crossed Swords / D-pad)
            GenerateThematicIcon($"{ArtDir}/Icon_Tab_Controls.png", (x, y, w, h) =>
            {
                float cx = w * 0.5f, cy = h * 0.5f;
                float nx = x - cx, ny = y - cy;
                bool dpadH = (Mathf.Abs(nx) <= 24f && Mathf.Abs(ny) <= 8f);
                bool dpadV = (Mathf.Abs(nx) <= 8f && Mathf.Abs(ny) <= 24f);
                bool centerGem = (Mathf.Abs(nx) <= 4f && Mathf.Abs(ny) <= 4f);
                return (dpadH || dpadV) && !centerGem;
            });

            // Gameplay Icon (Shield & Spark)
            GenerateThematicIcon($"{ArtDir}/Icon_Tab_Gameplay.png", (x, y, w, h) =>
            {
                float cx = w * 0.5f, cy = h * 0.5f;
                float nx = x - cx, ny = y - cy;
                bool shield = (Mathf.Abs(nx) <= 20f && ny <= 18f && (ny >= -6f || Mathf.Abs(nx) <= (ny + 24f) * 1.1f));
                bool inner = (Mathf.Abs(nx) <= 15f && ny <= 13f && (ny >= -4f || Mathf.Abs(nx) <= (ny + 20f) * 1.0f));
                bool star = (Mathf.Abs(nx) + Mathf.Abs(ny) <= 9f);
                return (shield && !inner) || star;
            });

            // Accessibility Icon (Universal Eye / Gear)
            GenerateThematicIcon($"{ArtDir}/Icon_Tab_Accessibility.png", (x, y, w, h) =>
            {
                float cx = w * 0.5f, cy = h * 0.5f;
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(cx, cy));
                bool outerRing = (d >= 16f && d <= 23f);
                bool pupil = (d <= 9f);
                bool ray1 = (Mathf.Abs(x - cx) <= 3f && Mathf.Abs(y - cy) <= 27f);
                bool ray2 = (Mathf.Abs(y - cy) <= 3f && Mathf.Abs(x - cx) <= 27f);
                return outerRing || pupil || ray1 || ray2;
            });
        }

        private static void GenerateThematicIcon(string path, System.Func<int, int, int, int, bool> predicate)
        {
            int w = 64, h = 64;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color[] px = new Color[w * h];
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    if (predicate(x, y, w, h))
                    {
                        float ty = (float)y / h;
                        px[y * w + x] = Color.Lerp(new Color(1.0f, 0.85f, 0.35f, 1f), new Color(1.0f, 0.95f, 0.70f, 1f), ty);
                    }
                    else
                    {
                        px[y * w + x] = Color.clear;
                    }
                }
            }
            tex.SetPixels(px);
            tex.Apply();
            SaveSprite(path, tex);
        }
        #endregion

        #region Helpers
        private static void CreateChamferedFrame(string path, int w, int h, int chamfer,
            Color baseColor, Color topColor, Color borderBottom, Color borderTop, Color highlight, bool glow)
        {
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[w * h];

            for (int y = 0; y < h; y++)
            {
                float ty = (float)y / h;
                Color bodyCol = Color.Lerp(baseColor, topColor, ty);

                for (int x = 0; x < w; x++)
                {
                    int dTopLeft = chamfer - (x + (h - 1 - y));
                    int dTopRight = chamfer - ((w - 1 - x) + (h - 1 - y));
                    int dBottomLeft = chamfer - (x + y);
                    int dBottomRight = chamfer - ((w - 1 - x) + y);

                    if (dTopLeft > 0 || dTopRight > 0 || dBottomLeft > 0 || dBottomRight > 0)
                    {
                        pixels[y * w + x] = Color.clear;
                        continue;
                    }

                    int distBorder = Mathf.Min(x, w - 1 - x, y, h - 1 - y);
                    if (dTopLeft >= -3 || dTopRight >= -3 || dBottomLeft >= -3 || dBottomRight >= -3)
                        distBorder = 1;

                    if (distBorder <= 3)
                    {
                        Color borderCol = Color.Lerp(borderBottom, borderTop, ty);
                        pixels[y * w + x] = (distBorder == 1 && ty > 0.6f) ? highlight : borderCol;
                    }
                    else
                    {
                        if (glow && x <= 16)
                            pixels[y * w + x] = Color.Lerp(bodyCol, highlight, (16f - x) / 16f * 0.75f);
                        else
                            pixels[y * w + x] = bodyCol;
                    }
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            SaveSprite(path, tex);
        }

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
        #endregion
    }
}
#endif
