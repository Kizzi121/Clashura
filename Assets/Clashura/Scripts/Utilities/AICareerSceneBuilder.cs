#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Clashura.AI.Career;
using Clashura.Characters.Managers;
using Clashura.UI.Animation;

namespace Clashura.Utilities
{
    /// <summary>
    /// Master Scene Builder for AI Career Mode:
    /// - Constructs the AICareerMenu scene (New Game, Continue, Confirmation Dialog)
    /// - Constructs the AIVersusScreen scene (3v3 Player vs NOVA Matchup)
    /// - Adds both scenes to EditorBuildSettings automatically
    /// </summary>
    public static class AICareerSceneBuilder
    {
        public const string AIArtDir = "Assets/Clashura/Art/UI/AI";
        public const string MainMenuArtDir = "Assets/Clashura/Art/UI/MainMenu";
        public const string MultiplayerArtDir = "Assets/Clashura/Art/UI/Multiplayer";
        public const string CharArtDir = "Assets/Clashura/Art/Characters";
        public const string MasterDbPath = "Assets/Clashura/Data/Characters/ScriptableObjects/CharacterDatabase.asset";

        public const string AICareerMenuScenePath = "Assets/Clashura/Scenes/BetterWithAI/AICareerMenu.unity";
        public const string AIVersusScenePath = "Assets/Clashura/Scenes/BetterWithAI/AIVersusScreen.unity";

        [MenuItem("Clashura/AI/Build All AI Career Scenes")]
        public static void BuildAllCareerScenes()
        {
            AIArtGenerator.GenerateAllAIArt();

            BuildAICareerMenuScene();
            BuildAIVersusScene();

            RegisterScenesInBuildSettings();

            Debug.Log("<color=green>[AICareerSceneBuilder] Successfully built and registered all AI Career scenes!</color>");
        }

        #region 1. Build AICareerMenu Scene
        [MenuItem("Clashura/AI/Build AI Career Menu Scene")]
        public static void BuildAICareerMenuScene()
        {
            Debug.Log("[AICareerSceneBuilder] Building AI Career Menu Scene...");
            EnsureDirectory("Assets/Clashura/Scenes/BetterWithAI");

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 1. Camera
            var camObj = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            var cam = camObj.GetComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.02f, 0.04f, 0.08f);
            cam.orthographic = true;
            cam.orthographicSize = 5f;

            // 2. EventSystem
            new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem), typeof(UnityEngine.EventSystems.StandaloneInputModule));

            // 3. Master Canvas
            var canvasObj = new GameObject("MasterCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasObj.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasObj.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            // 4. Background
            var bgObj = new GameObject("Background", typeof(RectTransform), typeof(Image));
            bgObj.transform.SetParent(canvasObj.transform, false);
            var bgRect = bgObj.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;
            var bgImg = bgObj.GetComponent<Image>();
            bgImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{MainMenuArtDir}/CharSelect_Background.png") 
                        ?? AssetDatabase.LoadAssetAtPath<Sprite>($"{MainMenuArtDir}/MainMenu_Background.png");
            bgImg.type = Image.Type.Simple;
            bgImg.color = new Color(0.85f, 0.90f, 1.0f, 1.0f);

            // Vignette Overlay
            var vigObj = new GameObject("VignetteOverlay", typeof(RectTransform), typeof(Image));
            vigObj.transform.SetParent(canvasObj.transform, false);
            var vigRect = vigObj.GetComponent<RectTransform>();
            vigRect.anchorMin = Vector2.zero;
            vigRect.anchorMax = Vector2.one;
            vigRect.sizeDelta = Vector2.zero;
            var vigImg = vigObj.GetComponent<Image>();
            vigImg.color = new Color(0.01f, 0.02f, 0.05f, 0.45f);

            // 5. Top Navigation
            var backBtn = CreateTopLeftBackButton(canvasObj.transform, "< MAIN MENU");
            CreateModeBadge(canvasObj.transform, "AI CAREER · CAMPAIGN");

            // 6. Center Stage Panel (960 x 740 px)
            var panelObj = new GameObject("CenterStagePanel", typeof(RectTransform), typeof(Image));
            panelObj.transform.SetParent(canvasObj.transform, false);
            var pRect = panelObj.GetComponent<RectTransform>();
            pRect.anchorMin = new Vector2(0.5f, 0.5f);
            pRect.anchorMax = new Vector2(0.5f, 0.5f);
            pRect.pivot = new Vector2(0.5f, 0.5f);
            pRect.sizeDelta = new Vector2(960, 740);
            pRect.anchoredPosition = new Vector2(0, -10);

            var pImg = panelObj.GetComponent<Image>();
            pImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{AIArtDir}/UI_AI_Arena_Panel_Frame.png");
            pImg.type = Image.Type.Simple;

            // Title Section
            CreateHeaderSection(panelObj.transform, "AI CAREER MENU", "SELECT CAMPAIGN OPTION");

            // Container for the two main buttons
            var buttonsContainer = new GameObject("ButtonsContainer", typeof(RectTransform), typeof(VerticalLayoutGroup));
            buttonsContainer.transform.SetParent(panelObj.transform, false);
            var bcRect = buttonsContainer.GetComponent<RectTransform>();
            bcRect.anchorMin = new Vector2(0.5f, 0.48f);
            bcRect.anchorMax = new Vector2(0.5f, 0.48f);
            bcRect.pivot = new Vector2(0.5f, 0.5f);
            bcRect.sizeDelta = new Vector2(880, 360);
            bcRect.anchoredPosition = Vector2.zero;

            var vlg = buttonsContainer.GetComponent<VerticalLayoutGroup>();
            vlg.spacing = 25;
            vlg.childAlignment = TextAnchor.MiddleCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;

            // 1. NEW GAME BUTTON
            var (newGameBtn, _) = CreateCareerOptionButton(
                buttonsContainer.transform,
                "Btn_NewGame",
                "NEW GAME",
                "START A FRESH AI CAREER CAMPAIGN",
                "[ FRESH CAMPAIGN ]",
                new Color(0.20f, 0.88f, 1.0f),
                $"{AIArtDir}/Icon_Badge_Easy.png",
                $"{AIArtDir}/UI_Card_NewGame_Plate.png");

            // 2. CONTINUE BUTTON
            var (continueBtn, contSubTmp) = CreateCareerOptionButton(
                buttonsContainer.transform,
                "Btn_Continue",
                "CONTINUE",
                "RESUME PREVIOUS PROGRESS",
                "[ RESUME BATTLE ]",
                new Color(1.0f, 0.85f, 0.32f),
                $"{AIArtDir}/Icon_Badge_Medium.png",
                $"{AIArtDir}/UI_Card_Continue_Plate.png");

            var continueGroup = continueBtn.gameObject.AddComponent<CanvasGroup>();

            // Status Ribbon Deck
            var statusTmp = CreateStatusRibbon(panelObj.transform);

            // 7. Large Confirmation Panel (Modal Dialog)
            var (confRoot, confPromptTmp, confYesBtn, confNoBtn) = CreateConfirmationPanel(canvasObj.transform);

            // 8. Controller Wiring
            var controller = canvasObj.AddComponent<AICareerUI>();
            var ctrlSO = new SerializedObject(controller);
            ctrlSO.FindProperty("newGameButton").objectReferenceValue = newGameBtn;
            ctrlSO.FindProperty("continueButton").objectReferenceValue = continueBtn;
            ctrlSO.FindProperty("backButton").objectReferenceValue = backBtn;
            ctrlSO.FindProperty("continueCanvasGroup").objectReferenceValue = continueGroup;
            ctrlSO.FindProperty("continueSubtitleText").objectReferenceValue = contSubTmp;
            ctrlSO.FindProperty("confirmationPanelRoot").objectReferenceValue = confRoot;
            ctrlSO.FindProperty("confirmationPromptText").objectReferenceValue = confPromptTmp;
            ctrlSO.FindProperty("confirmYesButton").objectReferenceValue = confYesBtn;
            ctrlSO.FindProperty("confirmNoButton").objectReferenceValue = confNoBtn;
            ctrlSO.FindProperty("statusBannerText").objectReferenceValue = statusTmp;
            ctrlSO.ApplyModifiedProperties();

            // Also attach persistent managers if not present
            var mgrObj = new GameObject("AICareerManager", typeof(AICareerManager), typeof(AICharacterDatabase));
            var charDb = mgrObj.GetComponent<AICharacterDatabase>();
            var masterDb = AssetDatabase.LoadAssetAtPath<CharacterDatabaseSO>(MasterDbPath);
            var charDbSO = new SerializedObject(charDb);
            charDbSO.FindProperty("masterDatabase").objectReferenceValue = masterDb;
            charDbSO.ApplyModifiedProperties();
            charDb.EnsurePopulated();

            EditorSceneManager.SaveScene(scene, AICareerMenuScenePath);
            Debug.Log($"<color=green>[AICareerSceneBuilder] Successfully built: {AICareerMenuScenePath}</color>");
        }
        #endregion

        #region 2. Build AIVersusScene Scene
        [MenuItem("Clashura/AI/Build AI Versus Screen Scene")]
        public static void BuildAIVersusScene()
        {
            Debug.Log("[AICareerSceneBuilder] Building AI Versus Screen Scene...");
            EnsureDirectory("Assets/Clashura/Scenes/BetterWithAI");

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 1. Camera
            var camObj = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            var cam = camObj.GetComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.02f, 0.03f, 0.06f);
            cam.orthographic = true;
            cam.orthographicSize = 5f;

            // 2. EventSystem
            new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem), typeof(UnityEngine.EventSystems.StandaloneInputModule));

            // 3. Master Canvas
            var canvasObj = new GameObject("MasterCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasObj.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasObj.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            // 4. Background
            var bgObj = new GameObject("Background", typeof(RectTransform), typeof(Image));
            bgObj.transform.SetParent(canvasObj.transform, false);
            var bgRect = bgObj.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;
            var bgImg = bgObj.GetComponent<Image>();
            bgImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{MainMenuArtDir}/CharSelect_Background.png") 
                        ?? AssetDatabase.LoadAssetAtPath<Sprite>($"{MainMenuArtDir}/MainMenu_Background.png");
            bgImg.type = Image.Type.Simple;
            bgImg.color = new Color(0.55f, 0.65f, 0.85f, 1.0f);

            // Dark vignette
            var vigObj = new GameObject("VignetteOverlay", typeof(RectTransform), typeof(Image));
            vigObj.transform.SetParent(canvasObj.transform, false);
            var vigRect = vigObj.GetComponent<RectTransform>();
            vigRect.anchorMin = Vector2.zero;
            vigRect.anchorMax = Vector2.one;
            vigRect.sizeDelta = Vector2.zero;
            var vigImg = vigObj.GetComponent<Image>();
            vigImg.color = new Color(0.01f, 0.02f, 0.05f, 0.60f);

            // Top Navigation
            var backBtn = CreateTopLeftBackButton(canvasObj.transform, "< RETURN TO MENU");
            CreateModeBadge(canvasObj.transform, "3v3 AI ARENA · SHOWDOWN");

            // 5. Center VS Container
            var vsContainer = new GameObject("CenterVSContainer", typeof(RectTransform));
            vsContainer.transform.SetParent(canvasObj.transform, false);
            var vcRect = vsContainer.GetComponent<RectTransform>();
            vcRect.anchorMin = new Vector2(0.5f, 0.5f);
            vcRect.anchorMax = new Vector2(0.5f, 0.5f);
            vcRect.pivot = new Vector2(0.5f, 0.5f);
            vcRect.sizeDelta = new Vector2(400, 800);
            vcRect.anchoredPosition = Vector2.zero;

            // Match Title (Top Center)
            var matchTitleTmp = CreateTMPText(vsContainer.transform, "MatchTitle", "AI CAREER 3v3 SHOWDOWN", 26, FontStyles.Bold, new Color(1.0f, 0.88f, 0.35f), 0f, 0.88f, 1f, 0.95f);
            matchTitleTmp.alignment = TextAlignmentOptions.Center;
            matchTitleTmp.characterSpacing = 3f;

            var arenaTmp = CreateTMPText(vsContainer.transform, "ArenaName", "ARENA: CELESTIAL STADIUM", 16, FontStyles.Normal, new Color(0.7f, 0.85f, 1.0f, 0.9f), 0f, 0.82f, 1f, 0.88f);
            arenaTmp.alignment = TextAlignmentOptions.Center;
            arenaTmp.characterSpacing = 2f;

            // VS Glow Burst
            var glowObj = new GameObject("VSGlowBurst", typeof(RectTransform), typeof(Image));
            glowObj.transform.SetParent(vsContainer.transform, false);
            var glRect = glowObj.GetComponent<RectTransform>();
            glRect.anchorMin = new Vector2(0.5f, 0.52f);
            glRect.anchorMax = new Vector2(0.5f, 0.52f);
            glRect.pivot = new Vector2(0.5f, 0.5f);
            glRect.sizeDelta = new Vector2(340, 340);
            glRect.anchoredPosition = Vector2.zero;
            var glImg = glowObj.GetComponent<Image>();
            glImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{MultiplayerArtDir}/UI_Versus_Glow_Burst.png");
            glImg.color = new Color(1f, 0.85f, 0.3f, 0.45f);

            // Supreme VS Badge
            var badgeObj = new GameObject("VSBadge", typeof(RectTransform), typeof(Image));
            badgeObj.transform.SetParent(vsContainer.transform, false);
            var bdRect = badgeObj.GetComponent<RectTransform>();
            bdRect.anchorMin = new Vector2(0.5f, 0.52f);
            bdRect.anchorMax = new Vector2(0.5f, 0.52f);
            bdRect.pivot = new Vector2(0.5f, 0.5f);
            bdRect.sizeDelta = new Vector2(210, 210);
            bdRect.anchoredPosition = Vector2.zero;
            var bdImg = badgeObj.GetComponent<Image>();
            bdImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{MultiplayerArtDir}/UI_Versus_Badge_Supreme.png")
                        ?? AssetDatabase.LoadAssetAtPath<Sprite>($"{MultiplayerArtDir}/UI_Versus_Badge_Large.png");
            bdImg.preserveAspect = true;

            // Difficulty Tier Pill below VS Badge
            var diffTierTmp = CreateTMPText(vsContainer.transform, "DifficultyTierTag", "MEDIUM · [ BALANCED · TIER II ]", 18, FontStyles.Bold, new Color(1.0f, 0.82f, 0.25f), 0f, 0.32f, 1f, 0.38f);
            diffTierTmp.alignment = TextAlignmentOptions.Center;
            diffTierTmp.characterSpacing = 2f;

            // Countdown / Ready status
            var readyTmp = CreateTMPText(vsContainer.transform, "ReadyStatus", "COMBATANTS READY", 16, FontStyles.Bold, new Color(0.35f, 0.95f, 0.65f), 0f, 0.25f, 1f, 0.30f);
            readyTmp.alignment = TextAlignmentOptions.Center;

            // Start Match Button (Bottom Center)
            var startBtnObj = new GameObject("Btn_StartMatch", typeof(RectTransform), typeof(Image), typeof(Button), typeof(UIButtonAnimator));
            startBtnObj.transform.SetParent(vsContainer.transform, false);
            var sbRect = startBtnObj.GetComponent<RectTransform>();
            sbRect.anchorMin = new Vector2(0.5f, 0.12f);
            sbRect.anchorMax = new Vector2(0.5f, 0.12f);
            sbRect.pivot = new Vector2(0.5f, 0.5f);
            sbRect.sizeDelta = new Vector2(280, 56);
            sbRect.anchoredPosition = Vector2.zero;
            var sbImg = startBtnObj.GetComponent<Image>();
            sbImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{AIArtDir}/UI_Button_Confirm_No.png")
                        ?? AssetDatabase.LoadAssetAtPath<Sprite>($"{MultiplayerArtDir}/UI_Button_Gold_Action.png");
            sbImg.type = Image.Type.Simple;
            var sbTxt = CreateTMPText(startBtnObj.transform, "Label", "COMMENCE BATTLE", 18, FontStyles.Bold, Color.white, 0f, 0f, 1f, 1f);
            sbTxt.alignment = TextAlignmentOptions.Center;
            var startBtn = startBtnObj.GetComponent<Button>();

            // 6. Left Side: Player Team Card (520 x 780 px)
            var (pNameTmp, pLvlTmp, pPtsTmp, pTagTmp, pPortraits, pNames, pRoles, pSlots) = CreateTeamVersusColumn(
                canvasObj.transform,
                "PlayerTeamColumn",
                new Vector2(0.24f, 0.48f),
                new Color(0.18f, 0.72f, 0.98f),
                "COMMANDER",
                "LVL 25",
                "1250 PTS",
                "[ PLAYER 1 · COMMANDER ]",
                $"{MultiplayerArtDir}/UI_Versus_Card_P1.png");

            // 7. Right Side: AI Opponent (NOVA) Card (520 x 780 px)
            var (aiNameTmp, aiLvlTmp, aiPtsTmp, aiTagTmp, aiPortraits, aiNames, aiRoles, aiSlots) = CreateTeamVersusColumn(
                canvasObj.transform,
                "AITeamColumn",
                new Vector2(0.76f, 0.48f),
                new Color(0.96f, 0.32f, 0.32f),
                "NOVA",
                "LVL 25",
                "1250 PTS",
                "[ AI COMBAT NEXUS ]",
                $"{MultiplayerArtDir}/UI_Versus_Card_P2.png");

            // 8. Wire Controller
            var controller = canvasObj.AddComponent<AIVSScreenController>();
            var masterDb = AssetDatabase.LoadAssetAtPath<CharacterDatabaseSO>(MasterDbPath);
            var ctrlSO = new SerializedObject(controller);
            ctrlSO.FindProperty("masterDatabase").objectReferenceValue = masterDb;

            ctrlSO.FindProperty("matchTitleText").objectReferenceValue = matchTitleTmp;
            ctrlSO.FindProperty("arenaNameText").objectReferenceValue = arenaTmp;
            ctrlSO.FindProperty("difficultyTierText").objectReferenceValue = diffTierTmp;
            ctrlSO.FindProperty("countdownText").objectReferenceValue = readyTmp;
            ctrlSO.FindProperty("vsBadgeRect").objectReferenceValue = bdRect;
            ctrlSO.FindProperty("vsGlowImage").objectReferenceValue = glImg;

            ctrlSO.FindProperty("playerNameText").objectReferenceValue = pNameTmp;
            ctrlSO.FindProperty("playerLevelText").objectReferenceValue = pLvlTmp;
            ctrlSO.FindProperty("playerRatingText").objectReferenceValue = pPtsTmp;
            ctrlSO.FindProperty("playerTagText").objectReferenceValue = pTagTmp;

            ctrlSO.FindProperty("aiNameText").objectReferenceValue = aiNameTmp;
            ctrlSO.FindProperty("aiLevelText").objectReferenceValue = aiLvlTmp;
            ctrlSO.FindProperty("aiRatingText").objectReferenceValue = aiPtsTmp;
            ctrlSO.FindProperty("aiTagText").objectReferenceValue = aiTagTmp;

            ctrlSO.FindProperty("backButton").objectReferenceValue = backBtn;
            ctrlSO.FindProperty("startMatchButton").objectReferenceValue = startBtn;

            for (int i = 0; i < 3; i++)
            {
                ctrlSO.FindProperty($"playerHeroPortraits.Array.data[{i}]").objectReferenceValue = pPortraits[i];
                ctrlSO.FindProperty($"playerHeroNames.Array.data[{i}]").objectReferenceValue = pNames[i];
                ctrlSO.FindProperty($"playerHeroRoles.Array.data[{i}]").objectReferenceValue = pRoles[i];
                ctrlSO.FindProperty($"playerHeroSlots.Array.data[{i}]").objectReferenceValue = pSlots[i];

                ctrlSO.FindProperty($"aiHeroPortraits.Array.data[{i}]").objectReferenceValue = aiPortraits[i];
                ctrlSO.FindProperty($"aiHeroNames.Array.data[{i}]").objectReferenceValue = aiNames[i];
                ctrlSO.FindProperty($"aiHeroRoles.Array.data[{i}]").objectReferenceValue = aiRoles[i];
                ctrlSO.FindProperty($"aiHeroSlots.Array.data[{i}]").objectReferenceValue = aiSlots[i];
            }

            ctrlSO.ApplyModifiedProperties();

            // Attach persistent managers
            var mgrObj = new GameObject("AICareerManager", typeof(AICareerManager), typeof(AICharacterDatabase));
            var charDb = mgrObj.GetComponent<AICharacterDatabase>();
            var charDbSO = new SerializedObject(charDb);
            charDbSO.FindProperty("masterDatabase").objectReferenceValue = masterDb;
            charDbSO.ApplyModifiedProperties();
            charDb.EnsurePopulated();

            EditorSceneManager.SaveScene(scene, AIVersusScenePath);
            Debug.Log($"<color=green>[AICareerSceneBuilder] Successfully built: {AIVersusScenePath}</color>");
        }
        #endregion

        #region Helper UI Builders
        private static (Button btn, TextMeshProUGUI subtitleTmp) CreateCareerOptionButton(
            Transform parent,
            string gameObjectName,
            string title,
            string subtitle,
            string pillText,
            Color accentColor,
            string badgePath,
            string cardSpritePath)
        {
            var cardObj = new GameObject(gameObjectName, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement), typeof(UIButtonAnimator));
            cardObj.transform.SetParent(parent, false);

            var le = cardObj.GetComponent<LayoutElement>();
            le.preferredWidth = 880;
            le.preferredHeight = 140;

            var bgImg = cardObj.GetComponent<Image>();
            bgImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(cardSpritePath);
            bgImg.type = Image.Type.Simple;

            var btn = cardObj.GetComponent<Button>();
            btn.targetGraphic = bgImg;

            // Heraldic Medallion (Left, 96x96 px)
            var badgeObj = new GameObject("Medallion", typeof(RectTransform), typeof(Image));
            badgeObj.transform.SetParent(cardObj.transform, false);
            var bRect = badgeObj.GetComponent<RectTransform>();
            bRect.anchorMin = new Vector2(0f, 0.5f);
            bRect.anchorMax = new Vector2(0f, 0.5f);
            bRect.pivot = new Vector2(0f, 0.5f);
            bRect.sizeDelta = new Vector2(96, 96);
            bRect.anchoredPosition = new Vector2(28, 0);
            var bImg = badgeObj.GetComponent<Image>();
            bImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(badgePath);
            bImg.preserveAspect = true;
            bImg.raycastTarget = false;

            // Typography: Title + Subtitle
            var titleTmp = CreateTMPText(cardObj.transform, "Title", title, 40, FontStyles.Bold, accentColor, 0f, 0.5f, 0f, 0.5f);
            var tRect = titleTmp.GetComponent<RectTransform>();
            tRect.pivot = new Vector2(0f, 0.5f);
            tRect.sizeDelta = new Vector2(500, 48);
            tRect.anchoredPosition = new Vector2(150, 18);
            titleTmp.characterSpacing = 4f;

            var subTmp = CreateTMPText(cardObj.transform, "Subtitle", subtitle, 16, FontStyles.Normal, new Color(0.75f, 0.88f, 1.0f, 0.90f), 0f, 0.5f, 0f, 0.5f);
            var sRect = subTmp.GetComponent<RectTransform>();
            sRect.pivot = new Vector2(0f, 0.5f);
            sRect.sizeDelta = new Vector2(500, 32);
            sRect.anchoredPosition = new Vector2(150, -18);
            subTmp.characterSpacing = 2f;

            // Pill Badge (Right, 170x38 px)
            var pillObj = new GameObject("PillTag", typeof(RectTransform), typeof(Image));
            pillObj.transform.SetParent(cardObj.transform, false);
            var pRect = pillObj.GetComponent<RectTransform>();
            pRect.anchorMin = new Vector2(1f, 0.5f);
            pRect.anchorMax = new Vector2(1f, 0.5f);
            pRect.pivot = new Vector2(1f, 0.5f);
            pRect.sizeDelta = new Vector2(180, 40);
            pRect.anchoredPosition = new Vector2(-30, 0);
            var pImg = pillObj.GetComponent<Image>();
            pImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{AIArtDir}/UI_Badge_Selected_Active.png");
            pImg.type = Image.Type.Simple;

            var pTxt = CreateTMPText(pillObj.transform, "Text", pillText, 14, FontStyles.Bold, Color.white, 0f, 0f, 1f, 1f);
            pTxt.alignment = TextAlignmentOptions.Center;

            return (btn, subTmp);
        }

        private static (GameObject root, TextMeshProUGUI promptTmp, Button yesBtn, Button noBtn) CreateConfirmationPanel(Transform canvasTransform)
        {
            var modalRoot = new GameObject("ConfirmationPanelRoot", typeof(RectTransform), typeof(Image));
            modalRoot.transform.SetParent(canvasTransform, false);
            var mrRect = modalRoot.GetComponent<RectTransform>();
            mrRect.anchorMin = Vector2.zero;
            mrRect.anchorMax = Vector2.one;
            mrRect.sizeDelta = Vector2.zero;
            var mrImg = modalRoot.GetComponent<Image>();
            mrImg.color = new Color(0f, 0f, 0f, 0.82f); // Dark translucent barrier

            // Dialog Window (740 x 420 px)
            var dialogObj = new GameObject("DialogWindow", typeof(RectTransform), typeof(Image));
            dialogObj.transform.SetParent(modalRoot.transform, false);
            var dRect = dialogObj.GetComponent<RectTransform>();
            dRect.anchorMin = new Vector2(0.5f, 0.5f);
            dRect.anchorMax = new Vector2(0.5f, 0.5f);
            dRect.pivot = new Vector2(0.5f, 0.5f);
            dRect.sizeDelta = new Vector2(740, 420);
            dRect.anchoredPosition = Vector2.zero;
            var dImg = dialogObj.GetComponent<Image>();
            dImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{AIArtDir}/UI_Confirmation_Dialog_Frame.png")
                       ?? AssetDatabase.LoadAssetAtPath<Sprite>($"{AIArtDir}/UI_AI_Arena_Panel_Frame.png");
            dImg.type = Image.Type.Simple;

            // Warning Crest Icon (Top Center)
            var warnObj = new GameObject("WarnIcon", typeof(RectTransform), typeof(Image));
            warnObj.transform.SetParent(dialogObj.transform, false);
            var wRect = warnObj.GetComponent<RectTransform>();
            wRect.anchorMin = new Vector2(0.5f, 0.85f);
            wRect.anchorMax = new Vector2(0.5f, 0.85f);
            wRect.pivot = new Vector2(0.5f, 0.5f);
            wRect.sizeDelta = new Vector2(72, 72);
            wRect.anchoredPosition = Vector2.zero;
            var wImg = warnObj.GetComponent<Image>();
            wImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{AIArtDir}/Icon_Badge_Hard.png");
            wImg.preserveAspect = true;

            // Header Title
            var hTmp = CreateTMPText(dialogObj.transform, "Header", "NEW GAME CONFIRMATION", 26, FontStyles.Bold, new Color(1.0f, 0.85f, 0.35f), 0.05f, 0.65f, 0.95f, 0.75f);
            hTmp.alignment = TextAlignmentOptions.Center;
            hTmp.characterSpacing = 3f;

            // EXACT PROMPT: "Are you sure you want to clear your previous progress?"
            var promptTmp = CreateTMPText(dialogObj.transform, "PromptText", "Are you sure you want to clear your previous progress?", 22, FontStyles.Bold, Color.white, 0.08f, 0.44f, 0.92f, 0.62f);
            promptTmp.alignment = TextAlignmentOptions.Center;

            // Clarification Note
            var noteTmp = CreateTMPText(dialogObj.transform, "NoteText", "Starting a new career will reset your previous AI Career save.\nYour PlayFab account, name, rating points and character unlocks will NOT be affected.", 15, FontStyles.Normal, new Color(0.70f, 0.82f, 0.95f, 0.85f), 0.08f, 0.30f, 0.92f, 0.42f);
            noteTmp.alignment = TextAlignmentOptions.Center;

            // Buttons Container (Horizontal)
            var btnContainer = new GameObject("ButtonRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            btnContainer.transform.SetParent(dialogObj.transform, false);
            var bcRect = btnContainer.GetComponent<RectTransform>();
            bcRect.anchorMin = new Vector2(0.5f, 0.16f);
            bcRect.anchorMax = new Vector2(0.5f, 0.16f);
            bcRect.pivot = new Vector2(0.5f, 0.5f);
            bcRect.sizeDelta = new Vector2(620, 64);
            bcRect.anchoredPosition = Vector2.zero;

            var hlg = btnContainer.GetComponent<HorizontalLayoutGroup>();
            hlg.spacing = 30;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;

            // YES Button (Reset)
            var yesBtnObj = new GameObject("Btn_Confirm_Yes", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement), typeof(UIButtonAnimator));
            yesBtnObj.transform.SetParent(btnContainer.transform, false);
            var yle = yesBtnObj.GetComponent<LayoutElement>();
            yle.preferredWidth = 280;
            yle.preferredHeight = 56;
            var yImg = yesBtnObj.GetComponent<Image>();
            yImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{AIArtDir}/UI_Button_Confirm_Yes.png");
            yImg.type = Image.Type.Simple;
            var yTxt = CreateTMPText(yesBtnObj.transform, "Label", "YES, CLEAR PROGRESS", 16, FontStyles.Bold, Color.white, 0f, 0f, 1f, 1f);
            yTxt.alignment = TextAlignmentOptions.Center;
            var yesBtn = yesBtnObj.GetComponent<Button>();

            // NO Button (Keep)
            var noBtnObj = new GameObject("Btn_Confirm_No", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement), typeof(UIButtonAnimator));
            noBtnObj.transform.SetParent(btnContainer.transform, false);
            var nle = noBtnObj.GetComponent<LayoutElement>();
            nle.preferredWidth = 280;
            nle.preferredHeight = 56;
            var nImg = noBtnObj.GetComponent<Image>();
            nImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{AIArtDir}/UI_Button_Confirm_No.png");
            nImg.type = Image.Type.Simple;
            var nTxt = CreateTMPText(noBtnObj.transform, "Label", "NO, KEEP PROGRESS", 16, FontStyles.Bold, Color.white, 0f, 0f, 1f, 1f);
            nTxt.alignment = TextAlignmentOptions.Center;
            var noBtn = noBtnObj.GetComponent<Button>();

            modalRoot.SetActive(false); // Inactive by default
            return (modalRoot, promptTmp, yesBtn, noBtn);
        }

        private static (TextMeshProUGUI nameTmp, TextMeshProUGUI lvlTmp, TextMeshProUGUI ptsTmp, TextMeshProUGUI tagTmp, Image[] portraits, TextMeshProUGUI[] names, TextMeshProUGUI[] roles, GameObject[] slots)
            CreateTeamVersusColumn(Transform parent, string objectName, Vector2 anchorPos, Color themeColor, string defName, string defLvl, string defPts, string defTag, string cardSpritePath)
        {
            var colObj = new GameObject(objectName, typeof(RectTransform), typeof(Image));
            colObj.transform.SetParent(parent, false);
            var cRect = colObj.GetComponent<RectTransform>();
            cRect.anchorMin = anchorPos;
            cRect.anchorMax = anchorPos;
            cRect.pivot = new Vector2(0.5f, 0.5f);
            cRect.sizeDelta = new Vector2(520, 780);
            cRect.anchoredPosition = Vector2.zero;

            var cImg = colObj.GetComponent<Image>();
            cImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(cardSpritePath);
            cImg.type = Image.Type.Simple;

            // Player/AI Header Profile Section (Inside top of card)
            var tagTmp = CreateTMPText(colObj.transform, "TagText", defTag, 15, FontStyles.Bold, themeColor, 0.05f, 0.88f, 0.95f, 0.94f);
            tagTmp.alignment = TextAlignmentOptions.Left;
            tagTmp.characterSpacing = 2f;

            var nameTmp = CreateTMPText(colObj.transform, "DisplayName", defName, 36, FontStyles.Bold, Color.white, 0.05f, 0.80f, 0.70f, 0.88f);
            nameTmp.alignment = TextAlignmentOptions.Left;
            nameTmp.characterSpacing = 3f;

            var lvlTmp = CreateTMPText(colObj.transform, "LevelText", defLvl, 18, FontStyles.Bold, new Color(1.0f, 0.85f, 0.35f), 0.72f, 0.82f, 0.95f, 0.88f);
            lvlTmp.alignment = TextAlignmentOptions.Right;

            var ptsTmp = CreateTMPText(colObj.transform, "PointsText", defPts, 16, FontStyles.Normal, new Color(0.70f, 0.85f, 1.0f), 0.72f, 0.76f, 0.95f, 0.82f);
            ptsTmp.alignment = TextAlignmentOptions.Right;

            // 3 Hero Slots Container (Vertical Stack)
            var slotsContainer = new GameObject("HeroSlotsContainer", typeof(RectTransform), typeof(VerticalLayoutGroup));
            slotsContainer.transform.SetParent(colObj.transform, false);
            var scRect = slotsContainer.GetComponent<RectTransform>();
            scRect.anchorMin = new Vector2(0.5f, 0.38f);
            scRect.anchorMax = new Vector2(0.5f, 0.38f);
            scRect.pivot = new Vector2(0.5f, 0.5f);
            scRect.sizeDelta = new Vector2(470, 500);
            scRect.anchoredPosition = Vector2.zero;

            var vlg = slotsContainer.GetComponent<VerticalLayoutGroup>();
            vlg.spacing = 14;
            vlg.childAlignment = TextAnchor.MiddleCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;

            Image[] portraits = new Image[3];
            TextMeshProUGUI[] heroNames = new TextMeshProUGUI[3];
            TextMeshProUGUI[] heroRoles = new TextMeshProUGUI[3];
            GameObject[] heroSlots = new GameObject[3];

            for (int i = 0; i < 3; i++)
            {
                var (slotObj, pImg, nTmp, rTmp) = CreateVersusHeroSlot(slotsContainer.transform, $"HeroSlot_0{i + 1}", themeColor);
                heroSlots[i] = slotObj;
                portraits[i] = pImg;
                heroNames[i] = nTmp;
                heroRoles[i] = rTmp;
            }

            return (nameTmp, lvlTmp, ptsTmp, tagTmp, portraits, heroNames, heroRoles, heroSlots);
        }

        private static (GameObject slotObj, Image portrait, TextMeshProUGUI nameTmp, TextMeshProUGUI roleTmp)
            CreateVersusHeroSlot(Transform parent, string objectName, Color roleColor)
        {
            var slotObj = new GameObject(objectName, typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            slotObj.transform.SetParent(parent, false);

            var le = slotObj.GetComponent<LayoutElement>();
            le.preferredWidth = 470;
            le.preferredHeight = 145;

            var sImg = slotObj.GetComponent<Image>();
            sImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{MultiplayerArtDir}/UI_Versus_Hero_Slot_Large.png")
                       ?? AssetDatabase.LoadAssetAtPath<Sprite>($"{MultiplayerArtDir}/UI_Versus_Hero_Slot.png");
            sImg.type = Image.Type.Simple;

            // Circular Portrait Frame (Left side of slot, 110x110 px)
            var frameObj = new GameObject("HeroPortraitFrame", typeof(RectTransform));
            frameObj.transform.SetParent(slotObj.transform, false);
            var fRect = frameObj.GetComponent<RectTransform>();
            fRect.anchorMin = new Vector2(0f, 0.5f);
            fRect.anchorMax = new Vector2(0f, 0.5f);
            fRect.pivot = new Vector2(0f, 0.5f);
            fRect.sizeDelta = new Vector2(110, 110);
            fRect.anchoredPosition = new Vector2(16, 0);

            // Circle Mask
            var maskObj = new GameObject("CircleMask", typeof(RectTransform), typeof(Image), typeof(Mask));
            maskObj.transform.SetParent(frameObj.transform, false);
            var mRect = maskObj.GetComponent<RectTransform>();
            mRect.anchorMin = new Vector2(0.5f, 0.5f);
            mRect.anchorMax = new Vector2(0.5f, 0.5f);
            mRect.pivot = new Vector2(0.5f, 0.5f);
            mRect.sizeDelta = new Vector2(98, 98);
            mRect.anchoredPosition = Vector2.zero;
            var mImg = maskObj.GetComponent<Image>();
            mImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{AIArtDir}/UI_Circle_Mask.png");
            maskObj.GetComponent<Mask>().showMaskGraphic = false;

            // Character Portrait Image
            var pObj = new GameObject("PortraitImage", typeof(RectTransform), typeof(Image));
            pObj.transform.SetParent(maskObj.transform, false);
            var pRect = pObj.GetComponent<RectTransform>();
            pRect.anchorMin = new Vector2(0.5f, 0.5f);
            pRect.anchorMax = new Vector2(0.5f, 0.5f);
            pRect.pivot = new Vector2(0.5f, 0.5f);
            pRect.sizeDelta = new Vector2(100, 100);
            pRect.anchoredPosition = Vector2.zero;
            var pImg = pObj.GetComponent<Image>();
            pImg.preserveAspect = true;

            // Golden Ring Bezel
            var bObj = new GameObject("GoldenBezel", typeof(RectTransform), typeof(Image));
            bObj.transform.SetParent(frameObj.transform, false);
            var bRect = bObj.GetComponent<RectTransform>();
            bRect.anchorMin = new Vector2(0.5f, 0.5f);
            bRect.anchorMax = new Vector2(0.5f, 0.5f);
            bRect.pivot = new Vector2(0.5f, 0.5f);
            bRect.sizeDelta = new Vector2(110, 110);
            bRect.anchoredPosition = Vector2.zero;
            var bImg = bObj.GetComponent<Image>();
            bImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{AIArtDir}/UI_Hero_Portrait_Frame.png");
            bImg.preserveAspect = true;

            // Hero Name & Role (Center/Right of slot)
            var nTmp = CreateTMPText(slotObj.transform, "HeroName", "HERO NAME", 24, FontStyles.Bold, Color.white, 0f, 0.5f, 0f, 0.5f);
            var nRect = nTmp.GetComponent<RectTransform>();
            nRect.pivot = new Vector2(0f, 0.5f);
            nRect.sizeDelta = new Vector2(300, 36);
            nRect.anchoredPosition = new Vector2(142, 14);
            nTmp.characterSpacing = 2f;

            var rTmp = CreateTMPText(slotObj.transform, "HeroRole", "VANGUARD", 16, FontStyles.Bold, roleColor, 0f, 0.5f, 0f, 0.5f);
            var rRect = rTmp.GetComponent<RectTransform>();
            rRect.pivot = new Vector2(0f, 0.5f);
            rRect.sizeDelta = new Vector2(300, 26);
            rRect.anchoredPosition = new Vector2(142, -18);
            rTmp.characterSpacing = 2f;

            return (slotObj, pImg, nTmp, rTmp);
        }

        private static Button CreateTopLeftBackButton(Transform parent, string label)
        {
            var btnObj = new GameObject("Btn_BackButton", typeof(RectTransform), typeof(Image), typeof(Button), typeof(UIButtonAnimator));
            btnObj.transform.SetParent(parent, false);

            var rect = btnObj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.sizeDelta = new Vector2(230, 52);
            rect.anchoredPosition = new Vector2(40, -32);

            var img = btnObj.GetComponent<Image>();
            img.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{MainMenuArtDir}/UI_Button_Normal.png");
            img.type = Image.Type.Simple;

            var txt = CreateTMPText(btnObj.transform, "Label", label, 17, FontStyles.Bold, new Color(1.0f, 0.88f, 0.35f), 0f, 0f, 1f, 1f);
            txt.alignment = TextAlignmentOptions.Center;

            return btnObj.GetComponent<Button>();
        }

        private static void CreateModeBadge(Transform parent, string label)
        {
            var badgeObj = new GameObject("ModeBadge", typeof(RectTransform), typeof(Image));
            badgeObj.transform.SetParent(parent, false);

            var rect = badgeObj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.sizeDelta = new Vector2(300, 52);
            rect.anchoredPosition = new Vector2(-40, -32);

            var img = badgeObj.GetComponent<Image>();
            img.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{MainMenuArtDir}/Badge_Commander_Frame.png")
                      ?? AssetDatabase.LoadAssetAtPath<Sprite>($"{MainMenuArtDir}/UI_Button_Normal.png");
            img.type = Image.Type.Simple;

            var txt = CreateTMPText(badgeObj.transform, "Label", label, 16, FontStyles.Bold, new Color(0.35f, 0.85f, 1.0f), 0f, 0f, 1f, 1f);
            txt.alignment = TextAlignmentOptions.Center;
        }

        private static void CreateHeaderSection(Transform parent, string title, string subtitle)
        {
            var headerObj = new GameObject("HeaderSection", typeof(RectTransform));
            headerObj.transform.SetParent(parent, false);
            var rect = headerObj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.90f);
            rect.anchorMax = new Vector2(0.5f, 0.90f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(880, 110);
            rect.anchoredPosition = Vector2.zero;

            // Wings Crest
            var wingsObj = new GameObject("WingsCrest", typeof(RectTransform), typeof(Image));
            wingsObj.transform.SetParent(headerObj.transform, false);
            var wRect = wingsObj.GetComponent<RectTransform>();
            wRect.anchorMin = new Vector2(0.5f, 0.80f);
            wRect.anchorMax = new Vector2(0.5f, 0.80f);
            wRect.pivot = new Vector2(0.5f, 0.5f);
            wRect.sizeDelta = new Vector2(230, 68);
            wRect.anchoredPosition = Vector2.zero;
            var wImg = wingsObj.GetComponent<Image>();
            wImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{MainMenuArtDir}/Logo_Clashura_Emblem.png");
            wImg.preserveAspect = true;

            var titleTmp = CreateTMPText(headerObj.transform, "Title", title, 44, FontStyles.Bold, Color.white, 0f, 0.28f, 1f, 0.72f);
            titleTmp.alignment = TextAlignmentOptions.Center;
            titleTmp.characterSpacing = 4f;

            var subTmp = CreateTMPText(headerObj.transform, "Subtitle", subtitle, 17, FontStyles.Normal, new Color(0.68f, 0.85f, 1.0f, 0.95f), 0f, 0f, 1f, 0.28f);
            subTmp.alignment = TextAlignmentOptions.Center;
            subTmp.characterSpacing = 4f;
        }

        private static TextMeshProUGUI CreateStatusRibbon(Transform parent)
        {
            var deckObj = new GameObject("StatusRibbonDeck", typeof(RectTransform), typeof(Image));
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

            var titleTmp = CreateTMPText(deckObj.transform, "StatusText", "• SELECT 'NEW GAME' TO START YOUR AI CAREER •", 18, FontStyles.Bold, new Color(1.0f, 0.88f, 0.35f), 0.03f, 0f, 0.97f, 1f);
            titleTmp.alignment = TextAlignmentOptions.Center;

            return titleTmp;
        }

        private static TextMeshProUGUI CreateTMPText(Transform parent, string name, string text, float fontSize, FontStyles style, Color color, float aMinX, float aMinY, float aMaxX, float aMaxY)
        {
            var obj = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            obj.transform.SetParent(parent, false);

            var rect = obj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(aMinX, aMinY);
            rect.anchorMax = new Vector2(aMaxX, aMaxY);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var tmp = obj.GetComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.fontStyle = style;
            tmp.color = color;
            tmp.raycastTarget = false;

            return tmp;
        }

        public static void RegisterScenesInBuildSettings()
        {
            string[] requiredScenes = {
                "Assets/Clashura/Scenes/MainMenu/MainMenu.unity",
                "Assets/Clashura/Scenes/CharacterSelection/CharacterSelection.unity",
                "Assets/Clashura/Scenes/Multiplayer/PvP_Matchmaking.unity",
                "Assets/Scenes/SampleScene.unity",
                "Assets/Clashura/Scenes/BetterWithAI/BetterWithAI.unity",
                AICareerMenuScenePath,
                AIVersusScenePath
            };

            var currentScenes = EditorBuildSettings.scenes;
            var list = new System.Collections.Generic.List<EditorBuildSettingsScene>(currentScenes);

            foreach (var req in requiredScenes)
            {
                bool exists = false;
                foreach (var s in list)
                {
                    if (s.path == req)
                    {
                        exists = true;
                        s.enabled = true;
                        break;
                    }
                }

                if (!exists && File.Exists(req))
                {
                    list.Add(new EditorBuildSettingsScene(req, true));
                }
            }

            EditorBuildSettings.scenes = list.ToArray();
            Debug.Log($"[AICareerSceneBuilder] Registered {list.Count} scenes in EditorBuildSettings.");
        }

        private static void EnsureDirectory(string path)
        {
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
        }
        #endregion
    }
}
#endif
