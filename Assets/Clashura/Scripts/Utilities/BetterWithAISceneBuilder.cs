#if UNITY_EDITOR
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using Clashura.AI;
using Clashura.UI.Responsive;
using Clashura.UI.Animation;

namespace Clashura.Utilities
{
    /// <summary>
    /// Master Scene Builder for the "Better with AI" game mode screen.
    /// Constructs a premier fighting-game difficulty selection arena:
    /// - Heavy metallic obsidian modal frame with cyan/gold cyber-heraldic brackets
    /// - Three tall, rich vertical difficulty cards (880x160 px, Easy, Medium, Hard)
    /// - 106x106 circular heraldic crest medallions (Shield, Dueling Blades, Flaming Skull)
    /// - Ornate circular hero portrait frames (Aethelgard, Ronin Jin, Gorgar)
    /// - Bold fighting-game typography without cluttered telemetry or wall-of-text paragraphs
    /// - Luminous active selection states and streamlined status ribbon deck
    /// </summary>
    public static class BetterWithAISceneBuilder
    {
        public const string ScenePath = "Assets/Clashura/Scenes/BetterWithAI/BetterWithAI.unity";
        public const string MainMenuScenePath = "Assets/Clashura/Scenes/MainMenu/MainMenu.unity";
        private const string ArtDir = "Assets/Clashura/Art/UI/MainMenu";
        private const string AIArtDir = "Assets/Clashura/Art/UI/AI";
        private const string CharArtDir = "Assets/Clashura/Art/Characters";
        private const string FontPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";

        private static TMP_FontAsset _cachedFont;
        private static TMP_FontAsset GetFont()
        {
            if (_cachedFont == null)
            {
                _cachedFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            }
            return _cachedFont;
        }

        [MenuItem("Clashura/AI/Build BetterWithAI Scene")]
        public static void BuildBetterWithAIScene()
        {
            Debug.Log("[BetterWithAISceneBuilder] Building Premier AAA BetterWithAI Scene...");

            // 1. Ensure all AI art assets exist
            AIArtGenerator.GenerateAllAIArt();

            string sceneDir = Path.GetDirectoryName(ScenePath);
            if (!Directory.Exists(sceneDir))
            {
                Directory.CreateDirectory(sceneDir);
            }

            // 2. Create new empty scene
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 3. Setup 2D Camera
            Setup2DCamera();

            // 4. Setup EventSystem
            SetupEventSystem();

            // 5. Setup Master Canvas & Hierarchy
            SetupBetterWithAICanvas();

            // 6. Save Scene
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            // 7. Add to Build Settings
            AddSceneToBuildSettings(ScenePath);

            Debug.Log($"<color=green>[BetterWithAISceneBuilder] Successfully built and registered AAA BetterWithAI scene: {ScenePath}!</color>");
        }

        private static void Setup2DCamera()
        {
            GameObject camObj = new GameObject("Main Camera");
            var cam = camObj.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.02f, 0.04f, 0.08f, 1f);
            cam.orthographic = true;
            cam.orthographicSize = 5f;
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 1000f;
            camObj.transform.position = new Vector3(0, 0, -10);
            camObj.tag = "MainCamera";
        }

        private static void SetupEventSystem()
        {
            GameObject esObj = new GameObject("EventSystem");
            esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
            esObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        private static void SetupBetterWithAICanvas()
        {
            // 1. Master Canvas
            GameObject canvasObj = new GameObject("BetterWithAICanvas");
            var canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.pixelPerfect = false;

            var scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            canvasObj.AddComponent<GraphicRaycaster>();

            // Controller
            var controller = canvasObj.AddComponent<BetterWithAIController>();

            // 2. Fullscreen Background Image
            GameObject bgObj = new GameObject("Background", typeof(RectTransform), typeof(Image));
            bgObj.transform.SetParent(canvasObj.transform, false);
            var bgRect = bgObj.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;

            var bgImg = bgObj.GetComponent<Image>();
            var bgSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtDir}/MainMenu_Background.png")
                           ?? AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtDir}/MainMenu_Background.jpg");
            if (bgSprite != null)
            {
                bgImg.sprite = bgSprite;
                bgImg.type = Image.Type.Simple;
                bgImg.color = Color.white;
            }

            // Dark ambient atmospheric scrim for high contrast
            GameObject scrimObj = new GameObject("AtmosphericScrim", typeof(RectTransform), typeof(Image));
            scrimObj.transform.SetParent(canvasObj.transform, false);
            var sRect = scrimObj.GetComponent<RectTransform>();
            sRect.anchorMin = Vector2.zero;
            sRect.anchorMax = Vector2.one;
            sRect.sizeDelta = Vector2.zero;
            var sImg = scrimObj.GetComponent<Image>();
            sImg.color = new Color(0.01f, 0.02f, 0.05f, 0.50f);

            // 3. SafeArea Container
            GameObject safeObj = new GameObject("SafeArea", typeof(RectTransform));
            safeObj.transform.SetParent(canvasObj.transform, false);
            var safeRect = safeObj.GetComponent<RectTransform>();
            safeRect.anchorMin = Vector2.zero;
            safeRect.anchorMax = Vector2.one;
            safeRect.sizeDelta = Vector2.zero;

            // 4. Top Bar: Back Button & Mode Badge
            var backBtn = CreateBackButton(safeRect.transform);
            CreateModeBadge(safeRect.transform);

            // 5. Central Metallic Modal Panel Frame (960 x 740 px)
            GameObject panelObj = new GameObject("CenterStagePanel", typeof(RectTransform), typeof(Image));
            panelObj.transform.SetParent(safeRect.transform, false);
            var pRect = panelObj.GetComponent<RectTransform>();
            pRect.anchorMin = new Vector2(0.5f, 0.49f);
            pRect.anchorMax = new Vector2(0.5f, 0.49f);
            pRect.pivot = new Vector2(0.5f, 0.5f);
            pRect.sizeDelta = new Vector2(960, 740);
            pRect.anchoredPosition = Vector2.zero;

            var pImg = panelObj.GetComponent<Image>();
            pImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{AIArtDir}/UI_AI_Arena_Panel_Frame.png");
            pImg.type = Image.Type.Simple;

            // 6. Title Section (Inside Top of Center Panel)
            var (titleTmp, subTmp) = CreateTitleSection(panelObj.transform);

            // 7. Difficulty Cards Container (Vertical layout)
            GameObject cardsContainer = new GameObject("DifficultyCardsContainer", typeof(RectTransform), typeof(VerticalLayoutGroup));
            cardsContainer.transform.SetParent(panelObj.transform, false);
            var ccRect = cardsContainer.GetComponent<RectTransform>();
            ccRect.anchorMin = new Vector2(0.5f, 0.47f);
            ccRect.anchorMax = new Vector2(0.5f, 0.47f);
            ccRect.pivot = new Vector2(0.5f, 0.5f);
            ccRect.sizeDelta = new Vector2(880, 520);
            ccRect.anchoredPosition = Vector2.zero;

            var vlg = cardsContainer.GetComponent<VerticalLayoutGroup>();
            vlg.spacing = 15;
            vlg.childAlignment = TextAnchor.MiddleCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;

            // 8. Create the Three Premier Difficulty Cards (Taller, character-driven, no text wall!)
            // EASY CARD (Aethelgard - Stalwart Guardian)
            var (easyBtn, easyCardImg, easyCharPortrait, easyActiveBadge, easyCardRect) = CreatePremierDifficultyCard(
                cardsContainer.transform,
                "Btn_Difficulty_Easy",
                "EASY",
                "[ NOVICE · TIER I ]",
                $"{AIArtDir}/Icon_Badge_Easy.png",
                new Color(0.14f, 0.88f, 0.48f, 1f), // Bright Emerald
                $"{CharArtDir}/Aethelgard/Portrait_Aethelgard.png",
                Vector2.zero,
                new Vector2(108, 108),
                $"{AIArtDir}/UI_Card_Easy_Normal.png");

            // MEDIUM CARD (Ronin Jin - Balanced Martial Master)
            var (medBtn, medCardImg, medCharPortrait, medActiveBadge, medCardRect) = CreatePremierDifficultyCard(
                cardsContainer.transform,
                "Btn_Difficulty_Medium",
                "MEDIUM",
                "[ BALANCED · TIER II ]",
                $"{AIArtDir}/Icon_Badge_Medium.png",
                new Color(0.98f, 0.80f, 0.22f, 1f), // Bright Celestial Gold
                $"{CharArtDir}/Ronin Jin/Portrait_Ronin Jin.png",
                Vector2.zero,
                new Vector2(108, 108),
                $"{AIArtDir}/UI_Card_Medium_Normal.png");

            // HARD CARD (Gorgar - Ruthless Dragon Overlord)
            var (hardBtn, hardCardImg, hardCharPortrait, hardActiveBadge, hardCardRect) = CreatePremierDifficultyCard(
                cardsContainer.transform,
                "Btn_Difficulty_Hard",
                "HARD",
                "[ EXPERT · TIER III ]",
                $"{AIArtDir}/Icon_Badge_Hard.png",
                new Color(0.96f, 0.32f, 0.32f, 1f), // Bright Crimson Fire
                $"{CharArtDir}/Gorgar/Portrait_Gorgar.png",
                Vector2.zero,
                new Vector2(108, 108),
                $"{AIArtDir}/UI_Card_Hard_Normal.png");

            // 9. Streamlined Status Ribbon Deck (Inside Bottom of Center Panel)
            var statusTitleTmp = CreateStatusRibbonDeck(panelObj.transform);

            // 10. Load Sprites for Controller
            var easyNorm = AssetDatabase.LoadAssetAtPath<Sprite>($"{AIArtDir}/UI_Card_Easy_Normal.png");
            var easySel = AssetDatabase.LoadAssetAtPath<Sprite>($"{AIArtDir}/UI_Card_Easy_Selected.png");
            var medNorm = AssetDatabase.LoadAssetAtPath<Sprite>($"{AIArtDir}/UI_Card_Medium_Normal.png");
            var medSel = AssetDatabase.LoadAssetAtPath<Sprite>($"{AIArtDir}/UI_Card_Medium_Selected.png");
            var hardNorm = AssetDatabase.LoadAssetAtPath<Sprite>($"{AIArtDir}/UI_Card_Hard_Normal.png");
            var hardSel = AssetDatabase.LoadAssetAtPath<Sprite>($"{AIArtDir}/UI_Card_Hard_Selected.png");

            // 11. Wire Serialized Properties on BetterWithAIController
            var ctrlSO = new SerializedObject(controller);
            ctrlSO.FindProperty("titleText").objectReferenceValue = titleTmp;
            ctrlSO.FindProperty("subtitleText").objectReferenceValue = subTmp;
            ctrlSO.FindProperty("backButton").objectReferenceValue = backBtn;

            ctrlSO.FindProperty("easyButton").objectReferenceValue = easyBtn;
            ctrlSO.FindProperty("mediumButton").objectReferenceValue = medBtn;
            ctrlSO.FindProperty("hardButton").objectReferenceValue = hardBtn;

            ctrlSO.FindProperty("easyCardImage").objectReferenceValue = easyCardImg;
            ctrlSO.FindProperty("mediumCardImage").objectReferenceValue = medCardImg;
            ctrlSO.FindProperty("hardCardImage").objectReferenceValue = hardCardImg;

            ctrlSO.FindProperty("easyCharPortrait").objectReferenceValue = easyCharPortrait;
            ctrlSO.FindProperty("mediumCharPortrait").objectReferenceValue = medCharPortrait;
            ctrlSO.FindProperty("hardCharPortrait").objectReferenceValue = hardCharPortrait;

            ctrlSO.FindProperty("easyNormalSprite").objectReferenceValue = easyNorm;
            ctrlSO.FindProperty("easySelectedSprite").objectReferenceValue = easySel;
            ctrlSO.FindProperty("mediumNormalSprite").objectReferenceValue = medNorm;
            ctrlSO.FindProperty("mediumSelectedSprite").objectReferenceValue = medSel;
            ctrlSO.FindProperty("hardNormalSprite").objectReferenceValue = hardNorm;
            ctrlSO.FindProperty("hardSelectedSprite").objectReferenceValue = hardSel;

            ctrlSO.FindProperty("easyActiveBadge").objectReferenceValue = easyActiveBadge;
            ctrlSO.FindProperty("mediumActiveBadge").objectReferenceValue = medActiveBadge;
            ctrlSO.FindProperty("hardActiveBadge").objectReferenceValue = hardActiveBadge;

            ctrlSO.FindProperty("easyCardRect").objectReferenceValue = easyCardRect;
            ctrlSO.FindProperty("mediumCardRect").objectReferenceValue = medCardRect;
            ctrlSO.FindProperty("hardCardRect").objectReferenceValue = hardCardRect;

            ctrlSO.FindProperty("statusTitleText").objectReferenceValue = statusTitleTmp;
            ctrlSO.FindProperty("mainMenuSceneName").stringValue = "MainMenu";
            ctrlSO.ApplyModifiedProperties();
        }

        private static Button CreateBackButton(Transform parent)
        {
            GameObject btnObj = new GameObject("Btn_BackToMenu", typeof(RectTransform), typeof(Image), typeof(Button), typeof(UIButtonAnimator));
            btnObj.transform.SetParent(parent, false);

            var rect = btnObj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.sizeDelta = new Vector2(210, 52);
            rect.anchoredPosition = new Vector2(40, -32);

            var img = btnObj.GetComponent<Image>();
            img.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtDir}/UI_Button_Normal.png");
            img.type = Image.Type.Simple;

            var btn = btnObj.GetComponent<Button>();
            btn.transition = Selectable.Transition.SpriteSwap;
            var ss = new SpriteState
            {
                highlightedSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtDir}/UI_Button_Highlight.png"),
                pressedSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtDir}/UI_Button_Pressed.png"),
                selectedSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtDir}/UI_Button_Highlight.png")
            };
            btn.spriteState = ss;

            // Back Arrow Icon
            GameObject iconObj = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconObj.transform.SetParent(btnObj.transform, false);
            var iRect = iconObj.GetComponent<RectTransform>();
            iRect.anchorMin = new Vector2(0f, 0.5f);
            iRect.anchorMax = new Vector2(0f, 0.5f);
            iRect.pivot = new Vector2(0f, 0.5f);
            iRect.sizeDelta = new Vector2(24, 24);
            iRect.anchoredPosition = new Vector2(18, 0);
            var iImg = iconObj.GetComponent<Image>();
            iImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtDir}/Icon_BackArrow.png");
            iImg.preserveAspect = true;

            var txt = CreateTMPText(btnObj.transform, "Label", "MAIN MENU", 18, FontStyles.Bold, new Color(1.0f, 0.88f, 0.35f), 0.22f, 0f, 0.95f, 1f);
            txt.alignment = TextAlignmentOptions.Left;

            return btn;
        }

        private static void CreateModeBadge(Transform parent)
        {
            GameObject badgeObj = new GameObject("ModeBadge", typeof(RectTransform), typeof(Image));
            badgeObj.transform.SetParent(parent, false);

            var rect = badgeObj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.sizeDelta = new Vector2(280, 52);
            rect.anchoredPosition = new Vector2(-40, -32);

            var img = badgeObj.GetComponent<Image>();
            img.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtDir}/Badge_Commander_Frame.png");
            img.type = Image.Type.Simple;

            // AI Core Icon
            GameObject iconObj = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconObj.transform.SetParent(badgeObj.transform, false);
            var iRect = iconObj.GetComponent<RectTransform>();
            iRect.anchorMin = new Vector2(0f, 0.5f);
            iRect.anchorMax = new Vector2(0f, 0.5f);
            iRect.pivot = new Vector2(0f, 0.5f);
            iRect.sizeDelta = new Vector2(32, 32);
            iRect.anchoredPosition = new Vector2(16, 0);
            var iImg = iconObj.GetComponent<Image>();
            iImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{AIArtDir}/Icon_PlayWithAI.png");
            iImg.preserveAspect = true;

            var txt = CreateTMPText(badgeObj.transform, "Label", "AI ARENA · TRAINING", 17, FontStyles.Bold, new Color(0.35f, 0.85f, 1.0f), 0.22f, 0f, 0.95f, 1f);
            txt.alignment = TextAlignmentOptions.Left;
        }

        private static (TextMeshProUGUI title, TextMeshProUGUI subtitle) CreateTitleSection(Transform parent)
        {
            GameObject sectionObj = new GameObject("TitleSection", typeof(RectTransform));
            sectionObj.transform.SetParent(parent, false);
            var rect = sectionObj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.88f);
            rect.anchorMax = new Vector2(0.5f, 0.88f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(880, 120);
            rect.anchoredPosition = Vector2.zero;

            // Winged Crest Emblem
            GameObject emblemObj = new GameObject("CrestEmblem", typeof(RectTransform), typeof(Image));
            emblemObj.transform.SetParent(sectionObj.transform, false);
            var embRect = emblemObj.GetComponent<RectTransform>();
            embRect.anchorMin = new Vector2(0.5f, 0.82f);
            embRect.anchorMax = new Vector2(0.5f, 0.82f);
            embRect.pivot = new Vector2(0.5f, 0.5f);
            embRect.sizeDelta = new Vector2(230, 68);
            embRect.anchoredPosition = Vector2.zero;
            var embImg = emblemObj.GetComponent<Image>();
            embImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtDir}/Logo_Clashura_Emblem.png");
            embImg.preserveAspect = true;

            // Primary Title: "BETTER WITH AI"
            var titleTmp = CreateTMPText(sectionObj.transform, "MainTitle", "BETTER WITH AI", 46, FontStyles.Bold, Color.white, 0f, 0.26f, 1f, 0.78f);
            titleTmp.alignment = TextAlignmentOptions.Center;
            titleTmp.characterSpacing = 6f;

            // Subtitle: "CHOOSE YOUR DIFFICULTY"
            var subTmp = CreateTMPText(sectionObj.transform, "Subtitle", "CHOOSE YOUR DIFFICULTY", 18, FontStyles.Normal, new Color(0.68f, 0.85f, 1.0f, 0.95f), 0f, 0f, 1f, 0.28f);
            subTmp.alignment = TextAlignmentOptions.Center;
            subTmp.characterSpacing = 4f;

            return (titleTmp, subTmp);
        }

        private static (Button btn, Image cardImg, Image charPortrait, GameObject activeBadge, RectTransform cardRect) CreatePremierDifficultyCard(
            Transform parent,
            string gameObjectName,
            string title,
            string pillText,
            string badgePath,
            Color tierAccentColor,
            string characterArtPath,
            Vector2 portraitOffset,
            Vector2 portraitSize,
            string defaultSpritePath)
        {
            GameObject cardObj = new GameObject(gameObjectName, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement), typeof(UIButtonAnimator));
            cardObj.transform.SetParent(parent, false);

            var cRect = cardObj.GetComponent<RectTransform>();
            var le = cardObj.GetComponent<LayoutElement>();
            le.preferredWidth = 880;
            le.preferredHeight = 160;

            var bgImg = cardObj.GetComponent<Image>();
            bgImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(defaultSpritePath);
            bgImg.type = Image.Type.Simple;

            var btn = cardObj.GetComponent<Button>();
            btn.transition = Selectable.Transition.None; // Visuals driven explicitly by controller

            // 1. Circular Metallic Heraldic Medallion (Left, 106x106 px)
            GameObject badgeObj = new GameObject("HeraldicMedallion", typeof(RectTransform), typeof(Image));
            badgeObj.transform.SetParent(cardObj.transform, false);
            var bRect = badgeObj.GetComponent<RectTransform>();
            bRect.anchorMin = new Vector2(0f, 0.5f);
            bRect.anchorMax = new Vector2(0f, 0.5f);
            bRect.pivot = new Vector2(0f, 0.5f);
            bRect.sizeDelta = new Vector2(106, 106);
            bRect.anchoredPosition = new Vector2(26, 0);
            var bImg = badgeObj.GetComponent<Image>();
            bImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(badgePath);
            bImg.preserveAspect = true;
            bImg.raycastTarget = false;

            // 2. Bold Clean Typography (Center-Left: Title + Tier Tag)
            var titleTmp = CreateTMPText(cardObj.transform, "Title", title, 48, FontStyles.Bold, tierAccentColor, 0f, 0.5f, 0f, 0.5f);
            var tRect = titleTmp.GetComponent<RectTransform>();
            tRect.pivot = new Vector2(0f, 0.5f);
            tRect.sizeDelta = new Vector2(300, 52);
            tRect.anchoredPosition = new Vector2(152, 20);
            titleTmp.alignment = TextAlignmentOptions.Left;
            titleTmp.characterSpacing = 4f;

            var pillTmp = CreateTMPText(cardObj.transform, "PillTag", pillText, 18, FontStyles.Bold, new Color(0.85f, 0.92f, 1.0f, 0.90f), 0f, 0.5f, 0f, 0.5f);
            var pRect = pillTmp.GetComponent<RectTransform>();
            pRect.pivot = new Vector2(0f, 0.5f);
            pRect.sizeDelta = new Vector2(300, 32);
            pRect.anchoredPosition = new Vector2(152, -20);
            pillTmp.alignment = TextAlignmentOptions.Left;
            pillTmp.characterSpacing = 3f;

            // 3. Radiant Active Selection Pill Badge [ SELECTED ]
            GameObject activeObj = new GameObject("ActiveSelectionBadge", typeof(RectTransform), typeof(Image));
            activeObj.transform.SetParent(cardObj.transform, false);
            var aRect = activeObj.GetComponent<RectTransform>();
            aRect.anchorMin = new Vector2(1f, 0.5f);
            aRect.anchorMax = new Vector2(1f, 0.5f);
            aRect.pivot = new Vector2(0.5f, 0.5f);
            aRect.sizeDelta = new Vector2(160, 38);
            aRect.anchoredPosition = new Vector2(-235, 0);
            var aImg = activeObj.GetComponent<Image>();
            aImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{AIArtDir}/UI_Badge_Selected_Active.png");
            aImg.type = Image.Type.Simple;

            var aTxt = CreateTMPText(activeObj.transform, "BadgeText", "[ SELECTED ]", 15, FontStyles.Bold, Color.white, 0f, 0f, 1f, 1f);
            aTxt.alignment = TextAlignmentOptions.Center;
            activeObj.SetActive(false);

            // 4. Ornate Circular Hero Portrait Frame (Right Side)
            GameObject heroFrameObj = new GameObject("HeroPortraitFrame", typeof(RectTransform));
            heroFrameObj.transform.SetParent(cardObj.transform, false);
            var hfRect = heroFrameObj.GetComponent<RectTransform>();
            hfRect.anchorMin = new Vector2(1f, 0.5f);
            hfRect.anchorMax = new Vector2(1f, 0.5f);
            hfRect.pivot = new Vector2(0.5f, 0.5f);
            hfRect.sizeDelta = new Vector2(120, 120);
            hfRect.anchoredPosition = new Vector2(-80, 0);

            // Circular Mask
            GameObject maskObj = new GameObject("CircleMask", typeof(RectTransform), typeof(Image), typeof(Mask));
            maskObj.transform.SetParent(heroFrameObj.transform, false);
            var mRect = maskObj.GetComponent<RectTransform>();
            mRect.anchorMin = new Vector2(0.5f, 0.5f);
            mRect.anchorMax = new Vector2(0.5f, 0.5f);
            mRect.pivot = new Vector2(0.5f, 0.5f);
            mRect.sizeDelta = new Vector2(108, 108);
            mRect.anchoredPosition = Vector2.zero;

            var mImg = maskObj.GetComponent<Image>();
            mImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{AIArtDir}/UI_Circle_Mask.png");
            var maskComp = maskObj.GetComponent<Mask>();
            maskComp.showMaskGraphic = false;

            // Character Image inside Mask
            GameObject portraitObj = new GameObject("PortraitImage", typeof(RectTransform), typeof(Image));
            portraitObj.transform.SetParent(maskObj.transform, false);
            var portRect = portraitObj.GetComponent<RectTransform>();
            portRect.anchorMin = new Vector2(0.5f, 0.5f);
            portRect.anchorMax = new Vector2(0.5f, 0.5f);
            portRect.pivot = new Vector2(0.5f, 0.5f);
            portRect.sizeDelta = portraitSize;
            portRect.anchoredPosition = portraitOffset;

            var portImg = portraitObj.GetComponent<Image>();
            portImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(characterArtPath);
            portImg.preserveAspect = true;
            portImg.raycastTarget = false;

            // Golden Frame Overlay on top of Mask
            GameObject borderObj = new GameObject("GoldenBezelOverlay", typeof(RectTransform), typeof(Image));
            borderObj.transform.SetParent(heroFrameObj.transform, false);
            var boRect = borderObj.GetComponent<RectTransform>();
            boRect.anchorMin = new Vector2(0.5f, 0.5f);
            boRect.anchorMax = new Vector2(0.5f, 0.5f);
            boRect.pivot = new Vector2(0.5f, 0.5f);
            boRect.sizeDelta = new Vector2(120, 120);
            boRect.anchoredPosition = Vector2.zero;

            var boImg = borderObj.GetComponent<Image>();
            boImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{AIArtDir}/UI_Hero_Portrait_Frame.png");
            boImg.preserveAspect = true;
            boImg.raycastTarget = false;

            return (btn, bgImg, portImg, activeObj, cRect);
        }

        private static TextMeshProUGUI CreateStatusRibbonDeck(Transform parent)
        {
            GameObject deckObj = new GameObject("StatusRibbonDeck", typeof(RectTransform), typeof(Image));
            deckObj.transform.SetParent(parent, false);

            var rect = deckObj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.08f);
            rect.anchorMax = new Vector2(0.5f, 0.08f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(880, 52);
            rect.anchoredPosition = Vector2.zero;

            var img = deckObj.GetComponent<Image>();
            img.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{AIArtDir}/UI_AI_Console_Deck.png");
            img.type = Image.Type.Simple;

            var titleTmp = CreateTMPText(deckObj.transform, "StatusText", "• CURRENT SELECTION: MEDIUM (BALANCED) •", 18, FontStyles.Bold, new Color(1.0f, 0.88f, 0.35f), 0.03f, 0f, 0.97f, 1f);
            titleTmp.alignment = TextAlignmentOptions.Center;

            return titleTmp;
        }

        private static TextMeshProUGUI CreateTMPText(Transform parent, string name, string text, float size, FontStyles style, Color color, float aMinX, float aMinY, float aMaxX, float aMaxY)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            obj.transform.SetParent(parent, false);

            var rect = obj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(aMinX, aMinY);
            rect.anchorMax = new Vector2(aMaxX, aMaxY);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var tmp = obj.GetComponent<TextMeshProUGUI>();
            tmp.font = GetFont();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.fontStyle = style;
            tmp.color = color;
            tmp.raycastTarget = false;

            return tmp;
        }

        private static void AddSceneToBuildSettings(string scenePath)
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            foreach (var s in scenes)
            {
                if (s.path == scenePath) return;
            }
            scenes.Add(new EditorBuildSettingsScene(scenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
#endif
