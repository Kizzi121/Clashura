#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Clashura.AI;
using Clashura.AI.Career;
using Clashura.Characters.Managers;
using Clashura.UI.Animation;

namespace Clashura.Utilities
{
    /// <summary>
    /// Master Scene Builder for AI Career Mode:
    /// - Constructs the Unified Single-Scene AI Career (AICareerMenu.unity, BetterWithAI.unity, AIVersusScreen.unity)
    /// - Contains all 3 modular panels inside ONE scene:
    ///   * Panel 1: Career Menu (New Game, Continue, Confirmation Dialog)
    ///   * Panel 2: Difficulty Selection (Better With AI: Easy, Medium, Hard)
    ///   * Panel 3: 3v3 Versus Screen (Player Team vs permanent NOVA AI Team)
    /// - Adds all scenes to EditorBuildSettings automatically
    /// </summary>
    public static class AICareerSceneBuilder
    {
        public const string AIArtDir = "Assets/Clashura/Art/UI/AI";
        public const string MainMenuArtDir = "Assets/Clashura/Art/UI/MainMenu";
        public const string MultiplayerArtDir = "Assets/Clashura/Art/UI/Multiplayer";
        public const string CharArtDir = "Assets/Clashura/Art/Characters";
        public const string MasterDbPath = "Assets/Clashura/Data/Characters/ScriptableObjects/CharacterDatabase.asset";

        public const string AICareerMenuScenePath = "Assets/Clashura/Scenes/BetterWithAI/AICareerMenu.unity";

        [MenuItem("Clashura/AI/Build Unified AI Career Scene (Single Scene - 3 Panels)")]
        public static void BuildAllCareerScenes()
        {
            AIArtGenerator.GenerateAllAIArt();

            BuildUnifiedAICareerScene();

            RegisterScenesInBuildSettings();

            Debug.Log("<color=green>[AICareerSceneBuilder] Successfully built and registered Unified AI Career Scene (AICareerMenu.unity)!</color>");
        }

        #region Unified AI Career Scene Builder (1 Scene, 3 Modular Panels)
        [MenuItem("Clashura/AI/Build Unified AI Career Scene (Single Scene - 3 Panels)")]
        public static void BuildUnifiedAICareerScene()
        {
            Debug.Log("[AICareerSceneBuilder] Building Unified Single-Scene AI Career with 3 Modular Panels...");
            EnsureDirectory("Assets/Clashura/Scenes/BetterWithAI");
            AIArtGenerator.GenerateAllAIArt();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 1. Camera
            var camObj = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            var cam = camObj.GetComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.02f, 0.04f, 0.08f);
            cam.orthographic = true;
            cam.orthographicSize = 5f;
            camObj.tag = "MainCamera";

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

            // 4. Background & Scrim
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
            bgImg.color = new Color(0.80f, 0.88f, 1.0f, 1.0f);

            var vigObj = new GameObject("VignetteOverlay", typeof(RectTransform), typeof(Image));
            vigObj.transform.SetParent(canvasObj.transform, false);
            var vigRect = vigObj.GetComponent<RectTransform>();
            vigRect.anchorMin = Vector2.zero;
            vigRect.anchorMax = Vector2.one;
            vigRect.sizeDelta = Vector2.zero;
            var vigImg = vigObj.GetComponent<Image>();
            vigImg.color = new Color(0.01f, 0.02f, 0.05f, 0.50f);

            // Master Mode Badge (Top-Right)
            var masterBadgeTmp = CreateModeBadge(canvasObj.transform, "AI CAREER · CAMPAIGN");

            // -------------------------------------------------------------
            // PANEL 1: CAREER MENU PANEL
            // -------------------------------------------------------------
            var panelCareerMenu = new GameObject("Panel_CareerMenu", typeof(RectTransform));
            panelCareerMenu.transform.SetParent(canvasObj.transform, false);
            var pcmRect = panelCareerMenu.GetComponent<RectTransform>();
            pcmRect.anchorMin = Vector2.zero;
            pcmRect.anchorMax = Vector2.one;
            pcmRect.sizeDelta = Vector2.zero;

            var careerBackBtn = CreateTopLeftBackButton(panelCareerMenu.transform, "< MAIN MENU", "Btn_BackButton");

            // Center Stage Panel (960 x 740 px)
            var careerCenterPanel = new GameObject("CenterStagePanel", typeof(RectTransform), typeof(Image));
            careerCenterPanel.transform.SetParent(panelCareerMenu.transform, false);
            var cpRect = careerCenterPanel.GetComponent<RectTransform>();
            cpRect.anchorMin = new Vector2(0.5f, 0.5f);
            cpRect.anchorMax = new Vector2(0.5f, 0.5f);
            cpRect.pivot = new Vector2(0.5f, 0.5f);
            cpRect.sizeDelta = new Vector2(960, 740);
            cpRect.anchoredPosition = new Vector2(0, -10);
            var cpImg = careerCenterPanel.GetComponent<Image>();
            cpImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{AIArtDir}/UI_AI_Arena_Panel_Frame.png");
            cpImg.type = Image.Type.Simple;

            CreateHeaderSection(careerCenterPanel.transform, "AI CAREER MENU", "SELECT CAMPAIGN OPTION");

            var careerButtonsContainer = new GameObject("ButtonsContainer", typeof(RectTransform), typeof(VerticalLayoutGroup));
            careerButtonsContainer.transform.SetParent(careerCenterPanel.transform, false);
            var cbcRect = careerButtonsContainer.GetComponent<RectTransform>();
            cbcRect.anchorMin = new Vector2(0.5f, 0.48f);
            cbcRect.anchorMax = new Vector2(0.5f, 0.48f);
            cbcRect.pivot = new Vector2(0.5f, 0.5f);
            cbcRect.sizeDelta = new Vector2(880, 360);
            cbcRect.anchoredPosition = Vector2.zero;

            var cvlg = careerButtonsContainer.GetComponent<VerticalLayoutGroup>();
            cvlg.spacing = 25;
            cvlg.childAlignment = TextAnchor.MiddleCenter;
            cvlg.childControlWidth = true;
            cvlg.childControlHeight = false;

            var (newGameBtn, _) = CreateCareerOptionButton(
                careerButtonsContainer.transform,
                "Btn_NewGame",
                "NEW GAME",
                "START A FRESH AI CAREER CAMPAIGN",
                "[ FRESH CAMPAIGN ]",
                new Color(0.20f, 0.88f, 1.0f),
                $"{AIArtDir}/Icon_Badge_Easy.png",
                $"{AIArtDir}/UI_Card_NewGame_Plate.png");

            var (continueBtn, contSubTmp) = CreateCareerOptionButton(
                careerButtonsContainer.transform,
                "Btn_Continue",
                "CONTINUE",
                "RESUME PREVIOUS PROGRESS",
                "[ RESUME BATTLE ]",
                new Color(1.0f, 0.85f, 0.32f),
                $"{AIArtDir}/Icon_Badge_Medium.png",
                $"{AIArtDir}/UI_Card_Continue_Plate.png");

            var continueGroup = continueBtn.gameObject.AddComponent<CanvasGroup>();
            var careerStatusTmp = CreateStatusRibbon(careerCenterPanel.transform);

            // Confirmation Modal Panel (Modal Dialog over Panel 1)
            var (confRoot, confPromptTmp, confYesBtn, confNoBtn) = CreateConfirmationPanel(canvasObj.transform);

            // -------------------------------------------------------------
            // PANEL 2: DIFFICULTY SELECTION PANEL ("Better With AI")
            // -------------------------------------------------------------
            var panelDiffSelect = new GameObject("Panel_DifficultySelect", typeof(RectTransform));
            panelDiffSelect.transform.SetParent(canvasObj.transform, false);
            var pdsRect = panelDiffSelect.GetComponent<RectTransform>();
            pdsRect.anchorMin = Vector2.zero;
            pdsRect.anchorMax = Vector2.one;
            pdsRect.sizeDelta = Vector2.zero;

            var diffBackBtn = CreateTopLeftBackButton(panelDiffSelect.transform, "< CAREER MENU", "Btn_BackToMenu");

            var diffCenterPanel = new GameObject("CenterStagePanel", typeof(RectTransform), typeof(Image));
            diffCenterPanel.transform.SetParent(panelDiffSelect.transform, false);
            var dcpRect = diffCenterPanel.GetComponent<RectTransform>();
            dcpRect.anchorMin = new Vector2(0.5f, 0.5f);
            dcpRect.anchorMax = new Vector2(0.5f, 0.5f);
            dcpRect.pivot = new Vector2(0.5f, 0.5f);
            dcpRect.sizeDelta = new Vector2(960, 860);
            dcpRect.anchoredPosition = new Vector2(0, -10);
            var dcpImg = diffCenterPanel.GetComponent<Image>();
            dcpImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{AIArtDir}/UI_AI_Arena_Panel_Frame.png");
            dcpImg.type = Image.Type.Simple;

            var (diffTitleTmp, diffSubTmp) = CreateDifficultyHeaderSection(diffCenterPanel.transform);

            var diffCardsContainer = new GameObject("CardsContainer", typeof(RectTransform), typeof(VerticalLayoutGroup));
            diffCardsContainer.transform.SetParent(diffCenterPanel.transform, false);
            var dccRect = diffCardsContainer.GetComponent<RectTransform>();
            dccRect.anchorMin = new Vector2(0.5f, 0.48f);
            dccRect.anchorMax = new Vector2(0.5f, 0.48f);
            dccRect.pivot = new Vector2(0.5f, 0.5f);
            dccRect.sizeDelta = new Vector2(880, 520);
            dccRect.anchoredPosition = Vector2.zero;

            var dvlg = diffCardsContainer.GetComponent<VerticalLayoutGroup>();
            dvlg.spacing = 16;
            dvlg.childAlignment = TextAnchor.MiddleCenter;
            dvlg.childControlWidth = true;
            dvlg.childControlHeight = false;

            var (easyBtn, easyCardImg, easyPortrait, easyBadge, easyRect) = CreatePremierDifficultyCard(
                diffCardsContainer.transform, "Btn_Difficulty_Easy", "EASY", "[ ROOKIE · TIER I ]",
                $"{AIArtDir}/Icon_Badge_Easy.png", new Color(0.25f, 0.85f, 1.0f),
                $"{CharArtDir}/Aethelgard/Portrait_Aethelgard.png", Vector2.zero, new Vector2(108, 108),
                $"{AIArtDir}/UI_Card_Easy_Normal.png");

            var (medBtn, medCardImg, medPortrait, medBadge, medRect) = CreatePremierDifficultyCard(
                diffCardsContainer.transform, "Btn_Difficulty_Medium", "MEDIUM", "[ BALANCED · TIER II ]",
                $"{AIArtDir}/Icon_Badge_Medium.png", new Color(1.0f, 0.82f, 0.25f),
                $"{CharArtDir}/Ronin Jin/Portrait_Ronin Jin.png", Vector2.zero, new Vector2(108, 108),
                $"{AIArtDir}/UI_Card_Medium_Normal.png");

            var (hardBtn, hardCardImg, hardPortrait, hardBadge, hardRect) = CreatePremierDifficultyCard(
                diffCardsContainer.transform, "Btn_Difficulty_Hard", "HARD", "[ EXPERT · TIER III ]",
                $"{AIArtDir}/Icon_Badge_Hard.png", new Color(1.0f, 0.32f, 0.32f),
                $"{CharArtDir}/Gorgar/Portrait_Gorgar.png", Vector2.zero, new Vector2(108, 108),
                $"{AIArtDir}/UI_Card_Hard_Normal.png");

            var diffStatusTmp = CreateStatusRibbon(diffCenterPanel.transform);

            // -------------------------------------------------------------
            // PANEL 3: 3v3 VERSUS SCREEN PANEL ("AIVersusPanel")
            // -------------------------------------------------------------
            var panelAIVersus = new GameObject("Panel_AIVersus", typeof(RectTransform));
            panelAIVersus.transform.SetParent(canvasObj.transform, false);
            var pavRect = panelAIVersus.GetComponent<RectTransform>();
            pavRect.anchorMin = Vector2.zero;
            pavRect.anchorMax = Vector2.one;
            pavRect.sizeDelta = Vector2.zero;

            var versusBackBtn = CreateTopLeftBackButton(panelAIVersus.transform, "< DIFFICULTY", "Btn_VersusBackButton");

            var vsContainer = new GameObject("CenterVSContainer", typeof(RectTransform));
            vsContainer.transform.SetParent(panelAIVersus.transform, false);
            var vcRect = vsContainer.GetComponent<RectTransform>();
            vcRect.anchorMin = new Vector2(0.5f, 0.5f);
            vcRect.anchorMax = new Vector2(0.5f, 0.5f);
            vcRect.pivot = new Vector2(0.5f, 0.5f);
            vcRect.sizeDelta = new Vector2(400, 800);
            vcRect.anchoredPosition = Vector2.zero;

            var matchTitleTmp = CreateTMPText(vsContainer.transform, "MatchTitle", "AI CAREER 3v3 SHOWDOWN", 26, FontStyles.Bold, new Color(1.0f, 0.88f, 0.35f), 0f, 0.88f, 1f, 0.95f);
            matchTitleTmp.alignment = TextAlignmentOptions.Center;
            matchTitleTmp.characterSpacing = 3f;

            var arenaTmp = CreateTMPText(vsContainer.transform, "ArenaName", "ARENA: CELESTIAL STADIUM", 16, FontStyles.Normal, new Color(0.7f, 0.85f, 1.0f, 0.9f), 0f, 0.82f, 1f, 0.88f);
            arenaTmp.alignment = TextAlignmentOptions.Center;
            arenaTmp.characterSpacing = 2f;

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

            var diffTierTmp = CreateTMPText(vsContainer.transform, "DifficultyTierTag", "MEDIUM · [ BALANCED · TIER II ]", 18, FontStyles.Bold, new Color(1.0f, 0.82f, 0.25f), 0f, 0.32f, 1f, 0.38f);
            diffTierTmp.alignment = TextAlignmentOptions.Center;
            diffTierTmp.characterSpacing = 2f;

            var readyTmp = CreateTMPText(vsContainer.transform, "ReadyStatus", "COMBATANTS READY", 16, FontStyles.Bold, new Color(0.35f, 0.95f, 0.65f), 0f, 0.25f, 1f, 0.30f);
            readyTmp.alignment = TextAlignmentOptions.Center;

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

            // Left Team (Player 1)
            var (pNameTmp, pLvlTmp, pPtsTmp, pTagTmp, pPortraits, pNames, pRoles, pSlots) = CreateTeamVersusColumn(
                panelAIVersus.transform, "PlayerTeamColumn", new Vector2(0.24f, 0.48f),
                new Color(0.18f, 0.72f, 0.98f), "COMMANDER", "LVL 25", "1250 PTS", "[ PLAYER 1 · COMMANDER ]",
                $"{MultiplayerArtDir}/UI_Versus_Card_P1.png");

            // Right Team (AI NOVA)
            var (aiNameTmp, aiLvlTmp, aiPtsTmp, aiTagTmp, aiPortraits, aiNames, aiRoles, aiSlots) = CreateTeamVersusColumn(
                panelAIVersus.transform, "AITeamColumn", new Vector2(0.76f, 0.48f),
                new Color(0.96f, 0.32f, 0.32f), "NOVA", "LVL 25", "1250 PTS", "[ AI COMBAT NEXUS ]",
                $"{MultiplayerArtDir}/UI_Versus_Card_P2.png");

            // -------------------------------------------------------------
            // CONTROLLERS SETUP & WIRING ON MasterCanvas
            // -------------------------------------------------------------
            var masterDb = AssetDatabase.LoadAssetAtPath<CharacterDatabaseSO>(MasterDbPath);

            // 1. AICareerUI
            var careerUI = canvasObj.AddComponent<AICareerUI>();
            var cuiSO = new SerializedObject(careerUI);
            cuiSO.FindProperty("newGameButton").objectReferenceValue = newGameBtn;
            cuiSO.FindProperty("continueButton").objectReferenceValue = continueBtn;
            cuiSO.FindProperty("backButton").objectReferenceValue = careerBackBtn;
            cuiSO.FindProperty("continueCanvasGroup").objectReferenceValue = continueGroup;
            cuiSO.FindProperty("continueSubtitleText").objectReferenceValue = contSubTmp;
            cuiSO.FindProperty("confirmationPanelRoot").objectReferenceValue = confRoot;
            cuiSO.FindProperty("confirmationPromptText").objectReferenceValue = confPromptTmp;
            cuiSO.FindProperty("confirmYesButton").objectReferenceValue = confYesBtn;
            cuiSO.FindProperty("confirmNoButton").objectReferenceValue = confNoBtn;
            cuiSO.FindProperty("statusBannerText").objectReferenceValue = careerStatusTmp;
            cuiSO.ApplyModifiedProperties();

            // 2. BetterWithAIController
            var diffUI = canvasObj.AddComponent<BetterWithAIController>();
            var duiSO = new SerializedObject(diffUI);
            duiSO.FindProperty("titleText").objectReferenceValue = diffTitleTmp;
            duiSO.FindProperty("subtitleText").objectReferenceValue = diffSubTmp;
            duiSO.FindProperty("backButton").objectReferenceValue = diffBackBtn;
            duiSO.FindProperty("easyButton").objectReferenceValue = easyBtn;
            duiSO.FindProperty("mediumButton").objectReferenceValue = medBtn;
            duiSO.FindProperty("hardButton").objectReferenceValue = hardBtn;
            duiSO.FindProperty("easyCardImage").objectReferenceValue = easyCardImg;
            duiSO.FindProperty("mediumCardImage").objectReferenceValue = medCardImg;
            duiSO.FindProperty("hardCardImage").objectReferenceValue = hardCardImg;
            duiSO.FindProperty("easyCharPortrait").objectReferenceValue = easyPortrait;
            duiSO.FindProperty("mediumCharPortrait").objectReferenceValue = medPortrait;
            duiSO.FindProperty("hardCharPortrait").objectReferenceValue = hardPortrait;
            duiSO.FindProperty("easyNormalSprite").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>($"{AIArtDir}/UI_Card_Easy_Normal.png");
            duiSO.FindProperty("easySelectedSprite").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>($"{AIArtDir}/UI_Card_Easy_Selected.png");
            duiSO.FindProperty("mediumNormalSprite").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>($"{AIArtDir}/UI_Card_Medium_Normal.png");
            duiSO.FindProperty("mediumSelectedSprite").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>($"{AIArtDir}/UI_Card_Medium_Selected.png");
            duiSO.FindProperty("hardNormalSprite").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>($"{AIArtDir}/UI_Card_Hard_Normal.png");
            duiSO.FindProperty("hardSelectedSprite").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>($"{AIArtDir}/UI_Card_Hard_Selected.png");
            duiSO.FindProperty("easyActiveBadge").objectReferenceValue = easyBadge;
            duiSO.FindProperty("mediumActiveBadge").objectReferenceValue = medBadge;
            duiSO.FindProperty("hardActiveBadge").objectReferenceValue = hardBadge;
            duiSO.FindProperty("easyCardRect").objectReferenceValue = easyRect;
            duiSO.FindProperty("mediumCardRect").objectReferenceValue = medRect;
            duiSO.FindProperty("hardCardRect").objectReferenceValue = hardRect;
            duiSO.FindProperty("statusTitleText").objectReferenceValue = diffStatusTmp;
            duiSO.ApplyModifiedProperties();

            // 3. AIVSScreenController
            var vsUI = canvasObj.AddComponent<AIVSScreenController>();
            var vuiSO = new SerializedObject(vsUI);
            vuiSO.FindProperty("masterDatabase").objectReferenceValue = masterDb;
            vuiSO.FindProperty("matchTitleText").objectReferenceValue = matchTitleTmp;
            vuiSO.FindProperty("arenaNameText").objectReferenceValue = arenaTmp;
            vuiSO.FindProperty("difficultyTierText").objectReferenceValue = diffTierTmp;
            vuiSO.FindProperty("countdownText").objectReferenceValue = readyTmp;
            vuiSO.FindProperty("vsBadgeRect").objectReferenceValue = bdRect;
            vuiSO.FindProperty("vsGlowImage").objectReferenceValue = glImg;
            vuiSO.FindProperty("playerNameText").objectReferenceValue = pNameTmp;
            vuiSO.FindProperty("playerLevelText").objectReferenceValue = pLvlTmp;
            vuiSO.FindProperty("playerRatingText").objectReferenceValue = pPtsTmp;
            vuiSO.FindProperty("playerTagText").objectReferenceValue = pTagTmp;
            vuiSO.FindProperty("aiNameText").objectReferenceValue = aiNameTmp;
            vuiSO.FindProperty("aiLevelText").objectReferenceValue = aiLvlTmp;
            vuiSO.FindProperty("aiRatingText").objectReferenceValue = aiPtsTmp;
            vuiSO.FindProperty("aiTagText").objectReferenceValue = aiTagTmp;
            vuiSO.FindProperty("backButton").objectReferenceValue = versusBackBtn;
            vuiSO.FindProperty("startMatchButton").objectReferenceValue = startBtn;
            for (int i = 0; i < 3; i++)
            {
                vuiSO.FindProperty($"playerHeroPortraits.Array.data[{i}]").objectReferenceValue = pPortraits[i];
                vuiSO.FindProperty($"playerHeroNames.Array.data[{i}]").objectReferenceValue = pNames[i];
                vuiSO.FindProperty($"playerHeroRoles.Array.data[{i}]").objectReferenceValue = pRoles[i];
                vuiSO.FindProperty($"playerHeroSlots.Array.data[{i}]").objectReferenceValue = pSlots[i];
                vuiSO.FindProperty($"aiHeroPortraits.Array.data[{i}]").objectReferenceValue = aiPortraits[i];
                vuiSO.FindProperty($"aiHeroNames.Array.data[{i}]").objectReferenceValue = aiNames[i];
                vuiSO.FindProperty($"aiHeroRoles.Array.data[{i}]").objectReferenceValue = aiRoles[i];
                vuiSO.FindProperty($"aiHeroSlots.Array.data[{i}]").objectReferenceValue = aiSlots[i];
            }
            vuiSO.ApplyModifiedProperties();

            // 4. AICareerMasterUI
            var masterUI = canvasObj.AddComponent<AICareerMasterUI>();
            var muiSO = new SerializedObject(masterUI);
            muiSO.FindProperty("careerMenuPanel").objectReferenceValue = panelCareerMenu;
            muiSO.FindProperty("difficultySelectPanel").objectReferenceValue = panelDiffSelect;
            muiSO.FindProperty("versusScreenPanel").objectReferenceValue = panelAIVersus;
            muiSO.FindProperty("confirmationModalPanel").objectReferenceValue = confRoot;
            muiSO.FindProperty("careerUI").objectReferenceValue = careerUI;
            muiSO.FindProperty("difficultyUI").objectReferenceValue = diffUI;
            muiSO.FindProperty("versusUI").objectReferenceValue = vsUI;
            muiSO.FindProperty("masterModeBadge").objectReferenceValue = masterBadgeTmp;
            muiSO.ApplyModifiedProperties();

            // 5. Persistent Manager Object in Scene
            var mgrObj = new GameObject("AICareerManager", typeof(AICareerManager), typeof(AICharacterDatabase));
            var charDb = mgrObj.GetComponent<AICharacterDatabase>();
            var charDbSO = new SerializedObject(charDb);
            charDbSO.FindProperty("masterDatabase").objectReferenceValue = masterDb;
            charDbSO.ApplyModifiedProperties();
            charDb.EnsurePopulated();

            // Default initial panel visibility: Panel 1 active, 2 & 3 inactive, modal inactive
            panelCareerMenu.SetActive(true);
            panelDiffSelect.SetActive(false);
            panelAIVersus.SetActive(false);
            confRoot.SetActive(false);

            // Save single unified scene to AICareerMenu
            EditorSceneManager.SaveScene(scene, AICareerMenuScenePath);
            Debug.Log("<color=green>[AICareerSceneBuilder] Successfully constructed and saved Unified AI Career Scene to AICareerMenu.unity!</color>");
        }
        #endregion

        #region 1. Build AICareerMenu Scene
        [MenuItem("Clashura/AI/Build AI Career Menu Scene")]
        public static void BuildAICareerMenuScene()
        {
            BuildUnifiedAICareerScene();
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

        private static Button CreateTopLeftBackButton(Transform parent, string label, string buttonName = "Btn_BackButton")
        {
            var btnObj = new GameObject(buttonName, typeof(RectTransform), typeof(Image), typeof(Button), typeof(UIButtonAnimator));
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

        private static TextMeshProUGUI CreateModeBadge(Transform parent, string label)
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
            return txt;
        }

        private static (TextMeshProUGUI title, TextMeshProUGUI subtitle) CreateDifficultyHeaderSection(Transform parent)
        {
            var headerObj = new GameObject("TitleSection", typeof(RectTransform));
            headerObj.transform.SetParent(parent, false);
            var rect = headerObj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.88f);
            rect.anchorMax = new Vector2(0.5f, 0.88f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(880, 120);
            rect.anchoredPosition = Vector2.zero;

            // Winged Crest Emblem
            var wingsObj = new GameObject("CrestEmblem", typeof(RectTransform), typeof(Image));
            wingsObj.transform.SetParent(headerObj.transform, false);
            var wRect = wingsObj.GetComponent<RectTransform>();
            wRect.anchorMin = new Vector2(0.5f, 0.82f);
            wRect.anchorMax = new Vector2(0.5f, 0.82f);
            wRect.pivot = new Vector2(0.5f, 0.5f);
            wRect.sizeDelta = new Vector2(230, 68);
            wRect.anchoredPosition = Vector2.zero;
            var wImg = wingsObj.GetComponent<Image>();
            wImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{MainMenuArtDir}/Logo_Clashura_Emblem.png");
            wImg.preserveAspect = true;

            var titleTmp = CreateTMPText(headerObj.transform, "MainTitle", "BETTER WITH AI", 44, FontStyles.Bold, Color.white, 0f, 0.26f, 1f, 0.78f);
            titleTmp.alignment = TextAlignmentOptions.Center;
            titleTmp.characterSpacing = 4f;

            var subTmp = CreateTMPText(headerObj.transform, "Subtitle", "CHOOSE YOUR DIFFICULTY", 18, FontStyles.Normal, new Color(0.68f, 0.85f, 1.0f, 0.95f), 0f, 0f, 1f, 0.28f);
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
            var cardObj = new GameObject(gameObjectName, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement), typeof(UIButtonAnimator));
            cardObj.transform.SetParent(parent, false);

            var cRect = cardObj.GetComponent<RectTransform>();
            var le = cardObj.GetComponent<LayoutElement>();
            le.preferredWidth = 880;
            le.preferredHeight = 155;

            var bgImg = cardObj.GetComponent<Image>();
            bgImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(defaultSpritePath);
            bgImg.type = Image.Type.Simple;

            var btn = cardObj.GetComponent<Button>();
            btn.transition = Selectable.Transition.None;

            // 1. Circular Metallic Heraldic Medallion (Left, 106x106 px)
            var badgeObj = new GameObject("HeraldicMedallion", typeof(RectTransform), typeof(Image));
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
            var titleTmp = CreateTMPText(cardObj.transform, "Title", title, 44, FontStyles.Bold, tierAccentColor, 0f, 0.5f, 0f, 0.5f);
            var tRect = titleTmp.GetComponent<RectTransform>();
            tRect.pivot = new Vector2(0f, 0.5f);
            tRect.sizeDelta = new Vector2(300, 50);
            tRect.anchoredPosition = new Vector2(152, 18);
            titleTmp.alignment = TextAlignmentOptions.Left;
            titleTmp.characterSpacing = 4f;

            var pillTmp = CreateTMPText(cardObj.transform, "PillTag", pillText, 17, FontStyles.Bold, new Color(0.85f, 0.92f, 1.0f, 0.90f), 0f, 0.5f, 0f, 0.5f);
            var pRect = pillTmp.GetComponent<RectTransform>();
            pRect.pivot = new Vector2(0f, 0.5f);
            pRect.sizeDelta = new Vector2(300, 30);
            pRect.anchoredPosition = new Vector2(152, -18);
            pillTmp.alignment = TextAlignmentOptions.Left;
            pillTmp.characterSpacing = 3f;

            // 3. Active Selection Pill Badge [ SELECTED ]
            var activeObj = new GameObject("ActiveSelectionBadge", typeof(RectTransform), typeof(Image));
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

            // 4. Hero Portrait Frame
            var heroFrameObj = new GameObject("HeroPortraitFrame", typeof(RectTransform));
            heroFrameObj.transform.SetParent(cardObj.transform, false);
            var hfRect = heroFrameObj.GetComponent<RectTransform>();
            hfRect.anchorMin = new Vector2(1f, 0.5f);
            hfRect.anchorMax = new Vector2(1f, 0.5f);
            hfRect.pivot = new Vector2(0.5f, 0.5f);
            hfRect.sizeDelta = new Vector2(120, 120);
            hfRect.anchoredPosition = new Vector2(-80, 0);

            var maskObj = new GameObject("CircleMask", typeof(RectTransform), typeof(Image), typeof(Mask));
            maskObj.transform.SetParent(heroFrameObj.transform, false);
            var mRect = maskObj.GetComponent<RectTransform>();
            mRect.anchorMin = new Vector2(0.5f, 0.5f);
            mRect.anchorMax = new Vector2(0.5f, 0.5f);
            mRect.pivot = new Vector2(0.5f, 0.5f);
            mRect.sizeDelta = new Vector2(108, 108);
            mRect.anchoredPosition = Vector2.zero;
            var mImg = maskObj.GetComponent<Image>();
            mImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{AIArtDir}/UI_Circle_Mask.png");
            maskObj.GetComponent<Mask>().showMaskGraphic = false;

            var portraitObj = new GameObject("PortraitImage", typeof(RectTransform), typeof(Image));
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

            var borderObj = new GameObject("GoldenBezelOverlay", typeof(RectTransform), typeof(Image));
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
                AICareerMenuScenePath
            };

            var list = new System.Collections.Generic.List<EditorBuildSettingsScene>();

            foreach (var req in requiredScenes)
            {
                if (File.Exists(req))
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
