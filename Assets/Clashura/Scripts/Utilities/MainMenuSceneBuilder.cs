#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using Clashura.UI.Navigation;
using Clashura.UI.MainMenu;
using Clashura.UI.Responsive;
using Clashura.UI.Animation;
using Clashura.Characters.Data;
using Clashura.Characters.Managers;
using Clashura.Characters.Preview;
using Clashura.CharacterSelection.Managers;
using Clashura.CharacterSelection.UI;
using Clashura.Settings;
using Clashura.Settings.UI;
using Clashura.Core;
using Clashura.Multiplayer;

namespace Clashura.Utilities
{
    /// <summary>
    /// Master Scene Builder for CLASHURA — 100% Original Game UI.
    /// Completely free of cropped screenshots or copied reference UI elements.
    /// Builds modular, dynamic, data-driven interfaces with TextMeshPro typography,
    /// dynamic ability cards, dynamic combat attributes/health bars, and responsive scaling.
    /// </summary>
    public static class MainMenuSceneBuilder
    {
        private const string MainMenuScenePath = "Assets/Clashura/Scenes/MainMenu/MainMenu.unity";
        private const string CharSelectScenePath = "Assets/Clashura/Scenes/CharacterSelection/CharacterSelection.unity";
        private const string PrefabDir = "Assets/Clashura/Prefabs/UI/MainMenu";
        private const string ArtDir = "Assets/Clashura/Art/UI/MainMenu";
        private const string SettingsArtDir = "Assets/Clashura/Art/UI/Settings";
        private const string AIArtDir = "Assets/Clashura/Art/UI/AI";
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

        [MenuItem("Clashura/Build Main Menu Scene & Navigation")]
        public static void BuildMainMenuScene()
        {
            Debug.Log("[MainMenuSceneBuilder] Building 100% Original Animated CLASHURA Main Menu & Selection Scene...");

            // 1. Ensure original art assets exist
            MainMenuArtGenerator.GenerateAllArt();
            SettingsArtGenerator.GenerateAllArt();
            AIArtGenerator.GenerateAllAIArt();

            // 2. Assign high-resolution themed archetype icons to ability data assets
            AssignThemedAbilityIcons();

            // 3. Create or load the dynamic AbilityCardPrefab
            var abilityPrefab = CreateOrLoadAbilityCardPrefab();

            // 3. Create new empty scene
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 4. Setup 2D Camera
            Setup2DCamera();

            // 5. Setup Managers (Navigation, CharacterSelection, and Settings)
            var (navController, selectionManager, settingsManager) = SetupManagers();

            // 6. Setup EventSystem
            SetupEventSystem();

            // 7. Setup Master Canvas
            var (canvasObj, mainMenuPanel, charSelectPanel, settingsPanel, charSelectUI) = SetupMasterCanvas(selectionManager, navController, abilityPrefab);

            // 8. Connect navigation controller serialized references
            var navSO = new SerializedObject(navController);
            navSO.FindProperty("mainMenuPanel").objectReferenceValue = mainMenuPanel;
            navSO.FindProperty("characterSelectionPanel").objectReferenceValue = charSelectPanel;
            navSO.FindProperty("settingsPanel").objectReferenceValue = settingsPanel;
            navSO.FindProperty("selectionManager").objectReferenceValue = selectionManager;
            navSO.FindProperty("selectionUI").objectReferenceValue = charSelectUI;
            navSO.FindProperty("startingScreen").enumValueIndex = 0;
            navSO.ApplyModifiedProperties();
            EditorUtility.SetDirty(navController);

            // 9. Save Main Menu Prefab
            SaveMainMenuPrefab(mainMenuPanel);

            // 10. Save & Open Scene
            EnsureDirectory("Assets/Clashura/Scenes/MainMenu");
            EditorSceneManager.SaveScene(scene, MainMenuScenePath);
            EditorSceneManager.OpenScene(MainMenuScenePath, OpenSceneMode.Single);

            // 11. Add scene to Build Settings if not already present
            AddSceneToBuildSettings(MainMenuScenePath);
            AddSceneToBuildSettings(CharSelectScenePath);

            Debug.Log($"<color=green>[MainMenuSceneBuilder] Successfully built and saved 100% original Main Menu scene: {MainMenuScenePath}!</color>");
        }

        private static void Setup2DCamera()
        {
            GameObject camObj = new GameObject("Main Camera");
            var cam = camObj.AddComponent<Camera>();
            camObj.AddComponent<AudioListener>();
            cam.orthographic = true;
            cam.orthographicSize = 5f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.04f, 0.05f, 0.08f, 1f);
            cam.transform.position = new Vector3(0f, 0f, -10f);
            camObj.tag = "MainCamera";
        }

        private static (UINavigationController, CharacterSelectionManager, SettingsManager) SetupManagers()
        {
            GameObject mgrObj = new GameObject("[GameManagers]");
            var navController = mgrObj.AddComponent<UINavigationController>();
            var selectionManager = mgrObj.AddComponent<CharacterSelectionManager>();
            var settingsManager = mgrObj.AddComponent<SettingsManager>();

            string dbPath = "Assets/Clashura/Data/Characters/ScriptableObjects/CharacterDatabase.asset";
            var db = AssetDatabase.LoadAssetAtPath<CharacterDatabaseSO>(dbPath);

            var selSO = new SerializedObject(selectionManager);
            selSO.FindProperty("characterDatabase").objectReferenceValue = db;
            selSO.FindProperty("localPlayerId").stringValue = "Player_Local";
            selSO.FindProperty("autoLoadOnStart").boolValue = false;
            selSO.ApplyModifiedProperties();
            EditorUtility.SetDirty(selectionManager);

            return (navController, selectionManager, settingsManager);
        }

        private static void SetupEventSystem()
        {
            GameObject esObj = new GameObject("EventSystem");
            esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
            esObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        private static (GameObject canvasObj, GameObject mainMenuPanel, GameObject charSelectPanel, GameObject settingsPanel, CharacterSelectionUI charSelectUI) SetupMasterCanvas(
            CharacterSelectionManager selectionManager, UINavigationController navController, AbilitySlotUI abilityPrefab)
        {
            // 1. Master Canvas
            GameObject canvasObj = new GameObject("MasterCanvas", typeof(RectTransform));
            var canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<GraphicRaycaster>();

            var scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            canvasObj.AddComponent<ResponsiveLayoutController>();

            // 2. Build Main Menu Panel
            GameObject mainMenuPanel = BuildMainMenuPanel(canvasObj.transform, navController);

            // 3. Build Character Selection Panel
            GameObject charSelectPanel = BuildCharacterSelectionPanel(canvasObj.transform, selectionManager, navController, abilityPrefab, out CharacterSelectionUI charSelectUI);

            // 4. Build Settings Panel
            GameObject settingsPanel = BuildSettingsPanel(canvasObj.transform, navController);

            // Initially: Main Menu is active, Character Selection and Settings are inactive
            mainMenuPanel.SetActive(true);
            charSelectPanel.SetActive(false);
            settingsPanel.SetActive(false);

            return (canvasObj, mainMenuPanel, charSelectPanel, settingsPanel, charSelectUI);
        }

        #region Main Menu Panel Building
        private static GameObject BuildMainMenuPanel(Transform parent, UINavigationController navController)
        {
            GameObject panel = new GameObject("MainMenuPanel", typeof(RectTransform));
            panel.transform.SetParent(parent, false);
            var rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;

            var controller = panel.AddComponent<MainMenuController>();

            // 1. Panoramic Scenic Backdrop
            GameObject bgObj = new GameObject("Background", typeof(RectTransform), typeof(Image));
            bgObj.transform.SetParent(panel.transform, false);
            var bgRect = bgObj.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;
            var bgImg = bgObj.GetComponent<Image>();
            bgImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtDir}/MainMenu_Background.png");
            bgImg.type = Image.Type.Simple;

            // Safe Area
            GameObject safeObj = new GameObject("SafeArea", typeof(RectTransform), typeof(SafeAreaHandler));
            safeObj.transform.SetParent(panel.transform, false);
            var safeRect = safeObj.GetComponent<RectTransform>();
            safeRect.anchorMin = Vector2.zero;
            safeRect.anchorMax = Vector2.one;
            safeRect.sizeDelta = Vector2.zero;

            // 2. Commander Profile Badge (Top-Right)
            BuildCommanderProfileBadge(safeRect.transform);

            // 3. Original CLASHURA Game Title Section (Top-Center)
            GameObject titleObj = new GameObject("TitleSection", typeof(RectTransform));
            titleObj.transform.SetParent(safeRect, false);
            var titleRect = titleObj.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.5f, 0.77f);
            titleRect.anchorMax = new Vector2(0.5f, 0.77f);
            titleRect.pivot = new Vector2(0.5f, 0.5f);
            titleRect.sizeDelta = new Vector2(660, 200);
            titleRect.anchoredPosition = Vector2.zero;

            // Winged Crest Emblem
            GameObject emblemObj = new GameObject("CrestEmblem", typeof(RectTransform), typeof(Image));
            emblemObj.transform.SetParent(titleObj.transform, false);
            var embRect = emblemObj.GetComponent<RectTransform>();
            embRect.anchorMin = new Vector2(0.5f, 0.74f);
            embRect.anchorMax = new Vector2(0.5f, 0.74f);
            embRect.pivot = new Vector2(0.5f, 0.5f);
            embRect.sizeDelta = new Vector2(290, 88);
            embRect.anchoredPosition = Vector2.zero;
            var embImg = emblemObj.GetComponent<Image>();
            embImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtDir}/Logo_Clashura_Emblem.png");
            embImg.preserveAspect = true;

            // Title Text: CLASHURA
            GameObject mainTextObj = new GameObject("MainTitleText", typeof(RectTransform), typeof(TextMeshProUGUI));
            mainTextObj.transform.SetParent(titleObj.transform, false);
            var mtRect = mainTextObj.GetComponent<RectTransform>();
            mtRect.anchorMin = new Vector2(0.5f, 0.35f);
            mtRect.anchorMax = new Vector2(0.5f, 0.35f);
            mtRect.pivot = new Vector2(0.5f, 0.5f);
            mtRect.sizeDelta = new Vector2(660, 80);
            mtRect.anchoredPosition = Vector2.zero;
            var mtTmp = mainTextObj.GetComponent<TextMeshProUGUI>();
            mtTmp.font = GetFont();
            mtTmp.text = "CLASHURA";
            mtTmp.fontSize = 70;
            mtTmp.fontStyle = FontStyles.Bold;
            mtTmp.color = new Color(1.0f, 0.85f, 0.30f, 1f);
            mtTmp.alignment = TextAlignmentOptions.Center;
            mtTmp.characterSpacing = 4f;

            // Subtitle Text: CHAMPIONS OF ETERNITY
            GameObject subTextObj = new GameObject("SubtitleText", typeof(RectTransform), typeof(TextMeshProUGUI));
            subTextObj.transform.SetParent(titleObj.transform, false);
            var stRect = subTextObj.GetComponent<RectTransform>();
            stRect.anchorMin = new Vector2(0.5f, 0.05f);
            stRect.anchorMax = new Vector2(0.5f, 0.05f);
            stRect.pivot = new Vector2(0.5f, 0.5f);
            stRect.sizeDelta = new Vector2(660, 28);
            stRect.anchoredPosition = Vector2.zero;
            var stTmp = subTextObj.GetComponent<TextMeshProUGUI>();
            stTmp.font = GetFont();
            stTmp.text = "CHAMPIONS OF ETERNITY";
            stTmp.fontSize = 19;
            stTmp.fontStyle = FontStyles.Bold;
            stTmp.color = new Color(0.70f, 0.82f, 0.98f, 0.9f);
            stTmp.alignment = TextAlignmentOptions.Center;
            stTmp.characterSpacing = 8f;

            // 4. Menu Buttons Container
            GameObject btnContainer = new GameObject("MenuButtonsContainer", typeof(RectTransform), typeof(VerticalLayoutGroup));
            btnContainer.transform.SetParent(safeRect, false);
            var bcRect = btnContainer.GetComponent<RectTransform>();
            bcRect.anchorMin = new Vector2(0.5f, 0.365f);
            bcRect.anchorMax = new Vector2(0.5f, 0.365f);
            bcRect.pivot = new Vector2(0.5f, 0.5f);
            bcRect.sizeDelta = new Vector2(660, 520);
            bcRect.anchoredPosition = Vector2.zero;

            var vlg = btnContainer.GetComponent<VerticalLayoutGroup>();
            vlg.spacing = 8;
            vlg.childAlignment = TextAnchor.MiddleCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;

            // Modular Animated Buttons (1: PvP, 2: Random Match, 3: Play with AI, 4: Character Selection, 5: Settings, 6: Exit)
            var btnPvp = CreateModularMenuButton(btnContainer.transform, "Btn_PvP", "PLAYER VS PLAYER", "1v1, 2v2 & More", $"{ArtDir}/Icon_PvP.png", false);
            var btnRandom = CreateModularMenuButton(btnContainer.transform, "Btn_RandomMatch", "RANDOM MATCH", "Quick Match (Auto-Balanced)", $"{ArtDir}/Icon_RandomMatch.png", true);
            var btnAi = CreateModularMenuButton(btnContainer.transform, "Btn_PlayWithAI", "PLAY WITH AI", "Novice, Medium & Hard", $"{AIArtDir}/Icon_PlayWithAI.png", true);
            var btnCharSelect = CreateModularMenuButton(btnContainer.transform, "Btn_CharacterSelection", "CHARACTER SELECTION", "Choose Your Hero", $"{ArtDir}/Icon_CharSelect.png", true);
            var btnSettings = CreateModularMenuButton(btnContainer.transform, "Btn_Settings", "SETTINGS", "Audio & Controls", $"{ArtDir}/Icon_Settings.png", false);
            var btnExit = CreateModularMenuButton(btnContainer.transform, "Btn_Exit", "EXIT GAME", "Return to Desktop", $"{ArtDir}/Icon_Exit.png", false);

            // 5. Footer Panel
            GameObject footerObj = new GameObject("FooterPanel", typeof(RectTransform));
            footerObj.transform.SetParent(safeRect, false);
            var fRect = footerObj.GetComponent<RectTransform>();
            fRect.anchorMin = new Vector2(0, 0);
            fRect.anchorMax = new Vector2(1, 0.08f);
            fRect.sizeDelta = Vector2.zero;

            var verTmp = CreateTMPText(footerObj.transform, "VersionText", "v0.2.0 - 2D Early Alpha  |  © 2025 Clashura Studio", 19, FontStyles.Normal, new Color(0.65f, 0.75f, 0.88f, 0.85f), 0, 0, 0.5f, 1);
            verTmp.alignment = TextAlignmentOptions.Left;
            verTmp.GetComponent<RectTransform>().offsetMin = new Vector2(40, 10);

            var copyTmp = CreateTMPText(footerObj.transform, "CopyrightText", "CLASHURA © 2025 — All Rights Reserved", 19, FontStyles.Normal, new Color(0.65f, 0.75f, 0.88f, 0.85f), 0.5f, 0, 1, 1);
            copyTmp.alignment = TextAlignmentOptions.Right;
            copyTmp.GetComponent<RectTransform>().offsetMax = new Vector2(-40, -10);

            // Feedback Toast
            GameObject toastObj = new GameObject("FeedbackToast", typeof(RectTransform), typeof(Image));
            toastObj.transform.SetParent(safeRect, false);
            var tRect = toastObj.GetComponent<RectTransform>();
            tRect.anchorMin = new Vector2(0.5f, 0.08f);
            tRect.anchorMax = new Vector2(0.5f, 0.08f);
            tRect.sizeDelta = new Vector2(600, 52);
            tRect.anchoredPosition = new Vector2(0, 25);
            var tImg = toastObj.GetComponent<Image>();
            tImg.color = new Color(0.10f, 0.14f, 0.22f, 0.95f);
            var ttTmp = CreateTMPText(toastObj.transform, "Text", "", 20, FontStyles.Bold, new Color(1.0f, 0.85f, 0.30f), 0, 0, 1, 1);
            ttTmp.alignment = TextAlignmentOptions.Center;
            toastObj.SetActive(false);

            // Wire MainMenuController Serialized Properties
            var ctrlSO = new SerializedObject(controller);
            ctrlSO.FindProperty("pvpButton").objectReferenceValue = btnPvp;
            ctrlSO.FindProperty("playWithAIButton").objectReferenceValue = btnAi;
            ctrlSO.FindProperty("randomMatchButton").objectReferenceValue = btnRandom;
            ctrlSO.FindProperty("characterSelectionButton").objectReferenceValue = btnCharSelect;
            ctrlSO.FindProperty("settingsButton").objectReferenceValue = btnSettings;
            ctrlSO.FindProperty("exitButton").objectReferenceValue = btnExit;
            ctrlSO.FindProperty("feedbackBanner").objectReferenceValue = toastObj;
            ctrlSO.FindProperty("feedbackText").objectReferenceValue = ttTmp;
            ctrlSO.ApplyModifiedProperties();

            // Build Team Warning Panel for 1v1 PvP and Random Match ELO Queue
            BuildTeamWarningPanel(panel.transform, btnPvp.GetComponent<Button>(), btnRandom.GetComponent<Button>());

            return panel;
        }

        private static void BuildTeamWarningPanel(Transform parent, Button pvpButton, Button randomButton = null)
        {
            // 1. Modal Scrim (fullscreen blocker)
            GameObject warnObj = new GameObject("TeamWarningPanel", typeof(RectTransform), typeof(Image));
            warnObj.transform.SetParent(parent, false);
            var rect = warnObj.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;

            var bgImg = warnObj.GetComponent<Image>();
            bgImg.color = new Color(0.02f, 0.04f, 0.08f, 0.90f);

            // 2. Dialog Frame Container
            GameObject dialogObj = new GameObject("DialogFrame", typeof(RectTransform), typeof(Image));
            dialogObj.transform.SetParent(warnObj.transform, false);
            var dRect = dialogObj.GetComponent<RectTransform>();
            dRect.anchorMin = new Vector2(0.5f, 0.5f);
            dRect.anchorMax = new Vector2(0.5f, 0.5f);
            dRect.pivot = new Vector2(0.5f, 0.5f);
            dRect.sizeDelta = new Vector2(720, 460);
            dRect.anchoredPosition = Vector2.zero;

            var dImg = dialogObj.GetComponent<Image>();
            dImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtDir}/UI_Popup_Frame.png");
            dImg.type = Image.Type.Sliced;
            dImg.color = Color.white;

            // 3. Warning Icon (Emblem at top)
            GameObject iconObj = new GameObject("WarningIcon", typeof(RectTransform), typeof(Image));
            iconObj.transform.SetParent(dialogObj.transform, false);
            var icRect = iconObj.GetComponent<RectTransform>();
            icRect.anchorMin = new Vector2(0.5f, 1f);
            icRect.anchorMax = new Vector2(0.5f, 1f);
            icRect.pivot = new Vector2(0.5f, 1f);
            icRect.anchoredPosition = new Vector2(0, -40);
            icRect.sizeDelta = new Vector2(76, 76);

            var icImg = iconObj.GetComponent<Image>();
            icImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtDir}/Icon_Popup_Warning.png");
            icImg.preserveAspect = true;

            // 4. Header Tag
            var tagTmp = CreateTMPText(dialogObj.transform, "TagText", "ONLINE DUEL REQUIRES A TEAM", 20, FontStyles.Bold, new Color(0.65f, 0.78f, 0.95f), 0.05f, 0.65f, 0.95f, 0.75f);
            tagTmp.alignment = TextAlignmentOptions.Center;
            tagTmp.characterSpacing = 2f;

            // 5. Primary Warning Message Text (EXACT specification: "Please Select Your Team First!")
            var warnTmp = CreateTMPText(dialogObj.transform, "WarningMessageText", "Please Select Your Team First!", 34, FontStyles.Bold, new Color(1.0f, 0.85f, 0.30f), 0.04f, 0.48f, 0.96f, 0.64f);
            warnTmp.alignment = TextAlignmentOptions.Center;

            // 6. Subtitle / Guidance
            var subTmp = CreateTMPText(dialogObj.transform, "SubtitleText", "You must assemble a team of 3 champions before entering Player vs Player matchmaking.", 19, FontStyles.Normal, new Color(0.80f, 0.88f, 0.98f, 0.92f), 0.08f, 0.32f, 0.92f, 0.46f);
            subTmp.alignment = TextAlignmentOptions.Center;

            // 7. SINGLE BUTTON: "Go to Character Selection" (strictly NO other buttons!)
            GameObject btnObj = new GameObject("Btn_GoToCharacterSelection", typeof(RectTransform), typeof(Image), typeof(Button), typeof(UIButtonAnimator));
            btnObj.transform.SetParent(dialogObj.transform, false);
            var bRect = btnObj.GetComponent<RectTransform>();
            bRect.anchorMin = new Vector2(0.5f, 0.12f);
            bRect.anchorMax = new Vector2(0.5f, 0.12f);
            bRect.pivot = new Vector2(0.5f, 0.5f);
            bRect.sizeDelta = new Vector2(420, 68);

            var bImg = btnObj.GetComponent<Image>();
            bImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtDir}/UI_Button_Normal.png");
            bImg.type = Image.Type.Sliced;
            bImg.color = Color.white;
            var goBtn = btnObj.GetComponent<Button>();

            var btnTxt = CreateTMPText(btnObj.transform, "BtnText", "Go to Character Selection", 22, FontStyles.Bold, new Color(1.0f, 0.88f, 0.35f), 0, 0, 1, 1);
            btnTxt.alignment = TextAlignmentOptions.Center;

            // Initially hidden
            warnObj.SetActive(false);

            // Attach and configure PvPMenuController on parent panel
            var pvpCtrl = parent.gameObject.AddComponent<PvPMenuController>();
            var pvpSO = new SerializedObject(pvpCtrl);
            pvpSO.FindProperty("pvpButton").objectReferenceValue = pvpButton;
            if (randomButton != null && pvpSO.FindProperty("randomMatchButton") != null)
            {
                pvpSO.FindProperty("randomMatchButton").objectReferenceValue = randomButton;
            }
            pvpSO.FindProperty("teamWarningPanel").objectReferenceValue = warnObj;
            pvpSO.FindProperty("warningMessageText").objectReferenceValue = warnTmp;
            pvpSO.FindProperty("goToCharacterSelectionButton").objectReferenceValue = goBtn;
            pvpSO.FindProperty("matchmakingSceneName").stringValue = "PvP_Matchmaking";
            pvpSO.FindProperty("characterSelectionSceneName").stringValue = "CharacterSelection";
            pvpSO.ApplyModifiedProperties();
        }

        private static void BuildCommanderProfileBadge(Transform parent)
        {
            GameObject profileObj = new GameObject("PlayerProfileBadge", typeof(RectTransform), typeof(Image));
            profileObj.transform.SetParent(parent, false);
            var rect = profileObj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.sizeDelta = new Vector2(350, 82);
            rect.anchoredPosition = new Vector2(-40, -25);

            var img = profileObj.GetComponent<Image>();
            img.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtDir}/Badge_Commander_Frame.png");
            img.type = Image.Type.Simple;

            // Crown Icon
            GameObject crownObj = new GameObject("CrownIcon", typeof(RectTransform), typeof(Image));
            crownObj.transform.SetParent(profileObj.transform, false);
            var cRect = crownObj.GetComponent<RectTransform>();
            cRect.anchorMin = new Vector2(0, 0.5f);
            cRect.anchorMax = new Vector2(0, 0.5f);
            cRect.pivot = new Vector2(0, 0.5f);
            cRect.sizeDelta = new Vector2(46, 46);
            cRect.anchoredPosition = new Vector2(20, 0);
            var cImg = crownObj.GetComponent<Image>();
            cImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtDir}/Icon_CharSelect.png");
            cImg.preserveAspect = true;

            // Title & Level text
            var titleTmp = CreateTMPText(profileObj.transform, "Title", "COMMANDER", 17, FontStyles.Bold, new Color(1.0f, 0.85f, 0.30f), 0.22f, 0.52f, 0.95f, 0.92f);
            titleTmp.alignment = TextAlignmentOptions.Left;

            var valTmp = CreateTMPText(profileObj.transform, "Value", "Lv. 12  ·  1,250", 22, FontStyles.Bold, Color.white, 0.22f, 0.10f, 0.95f, 0.54f);
            valTmp.alignment = TextAlignmentOptions.Left;
        }

        private static Button CreateModularMenuButton(Transform parent, string name, string primaryText, string subText, string iconPath, bool pulse)
        {
            GameObject btnObj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement), typeof(UIButtonAnimator));
            btnObj.transform.SetParent(parent, false);

            var le = btnObj.GetComponent<LayoutElement>();
            le.preferredWidth = 650;
            le.preferredHeight = 76;

            var img = btnObj.GetComponent<Image>();
            string normPath = pulse ? $"{ArtDir}/UI_Button_Highlight.png" : $"{ArtDir}/UI_Button_Normal.png";
            img.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(normPath);
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

            // 1. Icon (Left)
            GameObject iconObj = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconObj.transform.SetParent(btnObj.transform, false);
            var iRect = iconObj.GetComponent<RectTransform>();
            iRect.anchorMin = new Vector2(0, 0.5f);
            iRect.anchorMax = new Vector2(0, 0.5f);
            iRect.pivot = new Vector2(0, 0.5f);
            iRect.sizeDelta = new Vector2(46, 46);
            iRect.anchoredPosition = new Vector2(28, 0);
            var iImg = iconObj.GetComponent<Image>();
            iImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(iconPath);
            iImg.preserveAspect = true;

            // 2. Primary Text
            var primTmp = CreateTMPText(btnObj.transform, "PrimaryText", primaryText, 24, FontStyles.Bold, Color.white, 0.15f, 0.48f, 0.85f, 0.94f);
            primTmp.alignment = TextAlignmentOptions.Left;

            // 3. Subtitle Text
            var subTmp = CreateTMPText(btnObj.transform, "SubText", subText, 15, FontStyles.Normal, new Color(0.72f, 0.84f, 0.98f, 0.95f), 0.15f, 0.10f, 0.85f, 0.48f);
            subTmp.alignment = TextAlignmentOptions.Left;

            // 4. Chevron (Right)
            GameObject chevObj = new GameObject("Chevron", typeof(RectTransform), typeof(Image));
            chevObj.transform.SetParent(btnObj.transform, false);
            var cRect = chevObj.GetComponent<RectTransform>();
            cRect.anchorMin = new Vector2(1, 0.5f);
            cRect.anchorMax = new Vector2(1, 0.5f);
            cRect.pivot = new Vector2(1, 0.5f);
            cRect.sizeDelta = new Vector2(32, 32);
            cRect.anchoredPosition = new Vector2(-26, 0);
            var cImg = chevObj.GetComponent<Image>();
            cImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtDir}/Icon_Chevron_Right.png");
            cImg.preserveAspect = true;

            // Micro-Interaction Animator
            var anim = btnObj.GetComponent<UIButtonAnimator>();
            var animSO = new SerializedObject(anim);
            animSO.FindProperty("enablePulse").boolValue = pulse;
            animSO.ApplyModifiedProperties();
            EditorUtility.SetDirty(anim);

            return btn;
        }
        #endregion

        #region Character Selection Panel Building
        private static GameObject BuildCharacterSelectionPanel(Transform parent, CharacterSelectionManager selectionManager, 
            UINavigationController navController, AbilitySlotUI abilityPrefab, out CharacterSelectionUI charSelectUI)
        {
            GameObject panel = new GameObject("CharacterSelectionPanel", typeof(RectTransform));
            panel.transform.SetParent(parent, false);
            var rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;

            charSelectUI = panel.AddComponent<CharacterSelectionUI>();

            // 1. Scenic Cathedral Hall Backdrop
            GameObject bgObj = new GameObject("Background", typeof(RectTransform), typeof(Image));
            bgObj.transform.SetParent(panel.transform, false);
            var bgRect = bgObj.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;
            var bgImg = bgObj.GetComponent<Image>();
            bgImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtDir}/CharSelect_Background.png");
            bgImg.type = Image.Type.Simple;

            // Safe Area
            GameObject safeObj = new GameObject("SafeArea", typeof(RectTransform), typeof(SafeAreaHandler));
            safeObj.transform.SetParent(panel.transform, false);
            var safeRect = safeObj.GetComponent<RectTransform>();
            safeRect.anchorMin = Vector2.zero;
            safeRect.anchorMax = Vector2.one;
            safeRect.sizeDelta = Vector2.zero;

            // 2. Original Back Button (Top-Left)
            Button backBtn = CreateBackButton(safeRect.transform);

            // 3. Header Panel (Hero Name, Title, VANGUARD Role Badge, Counter)
            GameObject headerObj = new GameObject("HeaderPanel", typeof(RectTransform));
            headerObj.transform.SetParent(safeRect, false);
            var hRect = headerObj.GetComponent<RectTransform>();
            hRect.anchorMin = new Vector2(0.15f, 0.83f);
            hRect.anchorMax = new Vector2(0.72f, 0.985f);
            hRect.sizeDelta = Vector2.zero;

            var nameTmp = CreateTMPText(headerObj.transform, "CharacterNameText", "Aethelgard", 72, FontStyles.Bold, new Color(1.0f, 0.88f, 0.35f), 0, 0.48f, 1, 1);
            var titleTmp = CreateTMPText(headerObj.transform, "CharacterTitleText", "The Ironclad Thane [Armored Dwarf Paladin]", 28, FontStyles.Italic, new Color(0.78f, 0.88f, 0.98f), 0, 0.20f, 1, 0.50f);
            var roleTmp = CreateTMPText(headerObj.transform, "CharacterRoleText", "🛡 VANGUARD", 26, FontStyles.Bold, new Color(1.0f, 0.42f, 0.32f), 0, 0, 1, 0.22f);
            var counterTmp = CreateTMPText(headerObj.transform, "CounterText", "1 / 18", 30, FontStyles.Bold, new Color(0.70f, 0.82f, 0.98f), 0.82f, 0.40f, 1, 0.95f);
            counterTmp.alignment = TextAlignmentOptions.Right;

            // 4. Hero Artwork Viewport (Towering hero presence across screen width and height)
            GameObject viewObj = new GameObject("ArtworkViewport", typeof(RectTransform), typeof(Image), typeof(CharacterVisual2DController));
            viewObj.transform.SetParent(safeRect, false);
            var vRect = viewObj.GetComponent<RectTransform>();
            vRect.anchorMin = new Vector2(0.24f, 0.16f);
            vRect.anchorMax = new Vector2(0.76f, 0.95f);
            vRect.sizeDelta = Vector2.zero;
            var vImg = viewObj.GetComponent<Image>();
            vImg.color = Color.clear;
            var visualCtrl = viewObj.GetComponent<CharacterVisual2DController>();

            GameObject primObj = new GameObject("PrimaryArtwork", typeof(RectTransform), typeof(Image));
            primObj.transform.SetParent(viewObj.transform, false);
            var pRect = primObj.GetComponent<RectTransform>();
            pRect.anchorMin = Vector2.zero;
            pRect.anchorMax = Vector2.one;
            pRect.sizeDelta = Vector2.zero;
            var pImg = primObj.GetComponent<Image>();
            pImg.preserveAspect = true;

            GameObject ghostObj = new GameObject("GhostArtwork", typeof(RectTransform), typeof(Image));
            ghostObj.transform.SetParent(viewObj.transform, false);
            var gRect = ghostObj.GetComponent<RectTransform>();
            gRect.anchorMin = Vector2.zero;
            gRect.anchorMax = Vector2.one;
            gRect.sizeDelta = Vector2.zero;
            var gImg = ghostObj.GetComponent<Image>();
            gImg.preserveAspect = true;
            gImg.color = Color.clear;

            // 5. Select For Team Button (Placed in front of dais, above abilities dock with comfortable space)
            Button toggleBtn = CreateSelectButton(safeRect.transform, out TextMeshProUGUI toggleTxt, out Image toggleImg);

            // 6. Navigation Chevrons (< and >) with generous mobile touch targets (44px spacing from panels)
            Button prevBtn = CreateNavChevron(safeRect.transform, "PrevButton", new Vector2(0.285f, 0.55f), $"{ArtDir}/Icon_Chevron_Left.png");
            Button nextBtn = CreateNavChevron(safeRect.transform, "NextButton", new Vector2(0.715f, 0.55f), $"{ArtDir}/Icon_Chevron_Right.png");

            // 7. Dynamic Combat Attributes Panel (Right Side - Expanded proportions)
            var (statsPanel, hpFill, atkFill, defFill, spdFill, hpTxt, atkTxt, defTxt, spdTxt) = BuildCombatAttributesPanel(safeRect.transform);

            // 8. Dynamic Abilities Dashboard (Bottom-Left: Lowered to footer for maximum arena visibility)
            GameObject abObj = new GameObject("AbilitiesPanel", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            abObj.transform.SetParent(safeRect, false);
            var abRect = abObj.GetComponent<RectTransform>();
            abRect.anchorMin = new Vector2(0.015f, 0.008f);
            abRect.anchorMax = new Vector2(0.505f, 0.280f);
            abRect.sizeDelta = Vector2.zero;

            var abHlg = abObj.GetComponent<HorizontalLayoutGroup>();
            abHlg.spacing = 10;
            abHlg.childAlignment = TextAnchor.MiddleLeft;
            abHlg.childControlWidth = true;
            abHlg.childControlHeight = true;
            abHlg.childForceExpandWidth = true;
            abHlg.childForceExpandHeight = true;

            // 9. Team Selection Bar (Bottom-Right: Symmetrical footer dock)
            var (teamObj, slotsList, confBtn, bannerObj, headTmp, subTmp, icImg) = BuildTeamSelectionBar(safeRect.transform);

            // 10. Bottom Center Winged Axe Emblem Ornament
            GameObject emblemObj = new GameObject("BottomCenterEmblem", typeof(RectTransform), typeof(Image));
            emblemObj.transform.SetParent(safeRect, false);
            var ebRect = emblemObj.GetComponent<RectTransform>();
            ebRect.anchorMin = new Vector2(0.510f, 0.006f);
            ebRect.anchorMax = new Vector2(0.510f, 0.006f);
            ebRect.pivot = new Vector2(0.5f, 0f);
            ebRect.sizeDelta = new Vector2(118, 36);
            var ebImg = emblemObj.GetComponent<Image>();
            ebImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtDir}/UI_Bottom_Center_Emblem.png");
            ebImg.preserveAspect = true;
            ebImg.raycastTarget = false;

            // 11. Confirmation Validation Popup System (4-State Reusable Popup)
            ConfirmationPopupUI popupUI = BuildConfirmationPopup(safeRect.transform);

            // Feedback Toast Banner
            GameObject feedObj = new GameObject("FeedbackBanner", typeof(RectTransform), typeof(Image));
            feedObj.transform.SetParent(safeRect, false);
            var fRect = feedObj.GetComponent<RectTransform>();
            fRect.anchorMin = new Vector2(0.5f, 0.35f);
            fRect.anchorMax = new Vector2(0.5f, 0.35f);
            fRect.sizeDelta = new Vector2(480, 44);
            feedObj.GetComponent<Image>().color = new Color(0.10f, 0.14f, 0.22f, 0.95f);
            var feedTmp = CreateTMPText(feedObj.transform, "Text", "", 16, FontStyles.Bold, new Color(1.0f, 0.85f, 0.30f), 0, 0, 1, 1);
            feedTmp.alignment = TextAlignmentOptions.Center;
            feedObj.SetActive(false);

            // Wire Serialized Properties on CharacterSelectionUI
            var selSO = new SerializedObject(charSelectUI);
            selSO.FindProperty("selectionManager").objectReferenceValue = selectionManager;
            selSO.FindProperty("visual2DController").objectReferenceValue = visualCtrl;
            selSO.FindProperty("characterNameText").objectReferenceValue = nameTmp;
            selSO.FindProperty("characterTitleText").objectReferenceValue = titleTmp;
            selSO.FindProperty("characterRoleText").objectReferenceValue = roleTmp;
            selSO.FindProperty("characterCounterText").objectReferenceValue = counterTmp;
            selSO.FindProperty("prevButton").objectReferenceValue = prevBtn;
            selSO.FindProperty("nextButton").objectReferenceValue = nextBtn;
            selSO.FindProperty("toggleSelectButton").objectReferenceValue = toggleBtn;
            selSO.FindProperty("toggleSelectButtonText").objectReferenceValue = toggleTxt;
            selSO.FindProperty("toggleSelectButtonImage").objectReferenceValue = toggleImg;

            // Dynamic Stats wiring
            selSO.FindProperty("healthText").objectReferenceValue = hpTxt;
            selSO.FindProperty("attackText").objectReferenceValue = atkTxt;
            selSO.FindProperty("defenseText").objectReferenceValue = defTxt;
            selSO.FindProperty("speedText").objectReferenceValue = spdTxt;
            selSO.FindProperty("healthBarFill").objectReferenceValue = hpFill;
            selSO.FindProperty("attackBarFill").objectReferenceValue = atkFill;
            selSO.FindProperty("defenseBarFill").objectReferenceValue = defFill;
            selSO.FindProperty("speedBarFill").objectReferenceValue = spdFill;

            // Dynamic Abilities wiring
            var loadedAbilityPrefab = AssetDatabase.LoadAssetAtPath<AbilitySlotUI>($"{PrefabDir}/AbilityCardPrefab.prefab");
            selSO.FindProperty("abilitiesContainer").objectReferenceValue = abObj.transform;
            selSO.FindProperty("abilitySlotPrefab").objectReferenceValue = loadedAbilityPrefab != null ? loadedAbilityPrefab : abilityPrefab;

            // Team Bar wiring
            selSO.FindProperty("confirmTeamButton").objectReferenceValue = confBtn;
            selSO.FindProperty("teamStatusText").objectReferenceValue = null;
            selSO.FindProperty("statusPanelRoot").objectReferenceValue = bannerObj;
            selSO.FindProperty("statusHeadingText").objectReferenceValue = headTmp;
            selSO.FindProperty("statusSubtitleText").objectReferenceValue = subTmp;
            selSO.FindProperty("statusIconImage").objectReferenceValue = icImg;
            selSO.FindProperty("feedbackBanner").objectReferenceValue = feedObj;
            selSO.FindProperty("feedbackText").objectReferenceValue = feedTmp;
            selSO.FindProperty("backButton").objectReferenceValue = backBtn;
            selSO.FindProperty("confirmationPopup").objectReferenceValue = popupUI;

            var slotsProp = selSO.FindProperty("selectedSlots");
            slotsProp.arraySize = slotsList.Count;
            for (int i = 0; i < slotsList.Count; i++)
            {
                slotsProp.GetArrayElementAtIndex(i).objectReferenceValue = slotsList[i];
            }

            selSO.ApplyModifiedProperties();
            EditorUtility.SetDirty(charSelectUI);

            // Setup Visual Controller
            var vcSO = new SerializedObject(visualCtrl);
            vcSO.FindProperty("primaryImage").objectReferenceValue = pImg;
            vcSO.FindProperty("secondaryImage").objectReferenceValue = gImg;
            vcSO.ApplyModifiedProperties();

            return panel;
        }

        private static (GameObject, Image, Image, Image, Image, TextMeshProUGUI, TextMeshProUGUI, TextMeshProUGUI, TextMeshProUGUI) 
            BuildCombatAttributesPanel(Transform parent)
        {
            GameObject statsObj = new GameObject("StatsPanel", typeof(RectTransform), typeof(Image));
            statsObj.transform.SetParent(parent, false);
            var statsRect = statsObj.GetComponent<RectTransform>();
            statsRect.anchorMin = new Vector2(0.760f, 0.365f);
            statsRect.anchorMax = new Vector2(0.988f, 0.955f);
            statsRect.sizeDelta = Vector2.zero;
            var statsImg = statsObj.GetComponent<Image>();
            statsImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtDir}/UI_Panel_Attributes_Frame.png");
            statsImg.type = Image.Type.Simple;

            // Header: ATTRIBUTES
            var headTmp = CreateTMPText(statsObj.transform, "Header", "ATTRIBUTES", 36, FontStyles.Bold, new Color(1.0f, 0.88f, 0.35f), 0.05f, 0.88f, 0.95f, 0.98f);
            headTmp.alignment = TextAlignmentOptions.Center;
            headTmp.characterSpacing = 5f;

            // 4 Dynamic Stat Rows (Evenly distributed with thick gauges)
            var (hpFill, hpTxt) = CreateStatRow(statsObj.transform, "Row_HP", "HP: 1600", $"{ArtDir}/Icon_Stat_Heart.png", $"{ArtDir}/UI_StatBar_Fill_Red.png", 0.67f);
            var (atkFill, atkTxt) = CreateStatRow(statsObj.transform, "Row_ATK", "ATK: 95", $"{ArtDir}/Icon_Stat_Swords.png", $"{ArtDir}/UI_StatBar_Fill_Orange.png", 0.47f);
            var (defFill, defTxt) = CreateStatRow(statsObj.transform, "Row_DEF", "DEF: 100", $"{ArtDir}/Icon_Stat_Shield.png", $"{ArtDir}/UI_StatBar_Fill_Blue.png", 0.27f);
            var (spdFill, spdTxt) = CreateStatRow(statsObj.transform, "Row_SPD", "SPD: 35", $"{ArtDir}/Icon_Stat_Boot.png", $"{ArtDir}/UI_StatBar_Fill_Teal.png", 0.07f);

            return (statsObj, hpFill, atkFill, defFill, spdFill, hpTxt, atkTxt, defTxt, spdTxt);
        }

        private static (Image, TextMeshProUGUI) CreateStatRow(Transform parent, string name, string defaultText, string iconPath, string fillSpritePath, float yAnchor)
        {
            GameObject rowObj = new GameObject(name, typeof(RectTransform));
            rowObj.transform.SetParent(parent, false);
            var rRect = rowObj.GetComponent<RectTransform>();
            rRect.anchorMin = new Vector2(0.06f, yAnchor);
            rRect.anchorMax = new Vector2(0.94f, yAnchor + 0.19f);
            rRect.sizeDelta = Vector2.zero;

            // Icon (Top-Left of row)
            GameObject iconObj = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconObj.transform.SetParent(rowObj.transform, false);
            var iRect = iconObj.GetComponent<RectTransform>();
            iRect.anchorMin = new Vector2(0, 0.74f);
            iRect.anchorMax = new Vector2(0, 0.74f);
            iRect.pivot = new Vector2(0, 0.5f);
            iRect.sizeDelta = new Vector2(36, 36);
            iRect.anchoredPosition = Vector2.zero;
            var iImg = iconObj.GetComponent<Image>();
            iImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(iconPath);
            iImg.preserveAspect = true;

            // Text Label (Top-Left next to Icon, Large bold stat readout)
            var valTmp = CreateTMPText(rowObj.transform, "ValueText", defaultText, 32, FontStyles.Bold, Color.white, 0f, 0.52f, 1f, 1f);
            valTmp.GetComponent<RectTransform>().offsetMin = new Vector2(46, 0);
            valTmp.alignment = TextAlignmentOptions.MidlineLeft;

            // Bar Track (Bottom of row, full width across panel)
            GameObject trackObj = new GameObject("BarTrack", typeof(RectTransform), typeof(Image));
            trackObj.transform.SetParent(rowObj.transform, false);
            var tRect = trackObj.GetComponent<RectTransform>();
            tRect.anchorMin = new Vector2(0f, 0.04f);
            tRect.anchorMax = new Vector2(1f, 0.44f);
            tRect.offsetMin = Vector2.zero;
            tRect.offsetMax = Vector2.zero;
            var tImg = trackObj.GetComponent<Image>();
            tImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtDir}/UI_StatBar_Track.png");
            tImg.type = Image.Type.Simple;

            // Bar Fill
            GameObject fillObj = new GameObject("BarFill", typeof(RectTransform), typeof(Image));
            fillObj.transform.SetParent(trackObj.transform, false);
            var fRect = fillObj.GetComponent<RectTransform>();
            fRect.anchorMin = Vector2.zero;
            fRect.anchorMax = Vector2.one;
            fRect.sizeDelta = Vector2.zero;
            var fImg = fillObj.GetComponent<Image>();
            fImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(fillSpritePath);
            fImg.type = Image.Type.Filled;
            fImg.fillMethod = Image.FillMethod.Horizontal;
            fImg.fillAmount = 0.8f;

            return (fImg, valTmp);
        }

        private static (GameObject teamObj, List<SelectedSlotUI> slotsList, Button confBtn, GameObject bannerObj, TextMeshProUGUI headTmp, TextMeshProUGUI subTmp, Image icImg) BuildTeamSelectionBar(Transform parent)
        {
            GameObject teamObj = new GameObject("TeamBar", typeof(RectTransform), typeof(Image));
            teamObj.transform.SetParent(parent, false);
            var tRect = teamObj.GetComponent<RectTransform>();
            tRect.anchorMin = new Vector2(0.515f, 0.008f);
            tRect.anchorMax = new Vector2(0.985f, 0.280f);
            tRect.sizeDelta = Vector2.zero;
            tRect.anchoredPosition = Vector2.zero;

            var tImg = teamObj.GetComponent<Image>();
            tImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtDir}/UI_Panel_TeamBar_Frame.png");
            tImg.type = Image.Type.Simple;

            // Tier 1 (Top): Smart Selection Status Banner
            GameObject bannerObj = new GameObject("StatusBanner", typeof(RectTransform), typeof(Image));
            bannerObj.transform.SetParent(teamObj.transform, false);
            var bRect = bannerObj.GetComponent<RectTransform>();
            bRect.anchorMin = new Vector2(0.02f, 0.68f);
            bRect.anchorMax = new Vector2(0.98f, 0.96f);
            bRect.sizeDelta = Vector2.zero;
            bRect.anchoredPosition = Vector2.zero;

            var bImg = bannerObj.GetComponent<Image>();
            bImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtDir}/UI_Panel_Status_Banner.png");
            bImg.type = Image.Type.Simple;

            // Status Icon (Left inside Banner)
            GameObject iconObj = new GameObject("StatusIcon", typeof(RectTransform), typeof(Image));
            iconObj.transform.SetParent(bannerObj.transform, false);
            var iRect = iconObj.GetComponent<RectTransform>();
            iRect.anchorMin = new Vector2(0.02f, 0.5f);
            iRect.anchorMax = new Vector2(0.02f, 0.5f);
            iRect.pivot = new Vector2(0f, 0.5f);
            iRect.sizeDelta = new Vector2(40, 40);
            iRect.anchoredPosition = Vector2.zero;
            var icImg = iconObj.GetComponent<Image>();
            icImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtDir}/Icon_Team_Status.png");
            icImg.preserveAspect = true;

            // Heading Text (Top of Banner text area)
            var headTmp = CreateTMPText(bannerObj.transform, "HeadingText", "PLEASE SELECT YOUR CHARACTERS", 22, FontStyles.Bold, new Color(1.0f, 0.85f, 0.35f), 0.09f, 0.48f, 0.98f, 0.95f);
            headTmp.alignment = TextAlignmentOptions.MidlineLeft;
            headTmp.characterSpacing = 2f;

            // Subtitle Text (Bottom of Banner text area)
            var subTmp = CreateTMPText(bannerObj.transform, "SubtitleText", "Choose 3 characters to build your team.", 15, FontStyles.Normal, new Color(0.78f, 0.88f, 0.98f, 0.9f), 0.09f, 0.06f, 0.98f, 0.50f);
            subTmp.alignment = TextAlignmentOptions.MidlineLeft;

            // Tier 2 (Bottom): Slots and Confirm Row
            GameObject slotsRowObj = new GameObject("SlotsAndConfirmRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            slotsRowObj.transform.SetParent(teamObj.transform, false);
            var srRect = slotsRowObj.GetComponent<RectTransform>();
            srRect.anchorMin = new Vector2(0.02f, 0.04f);
            srRect.anchorMax = new Vector2(0.98f, 0.65f);
            srRect.sizeDelta = Vector2.zero;
            srRect.anchoredPosition = Vector2.zero;

            var srHlg = slotsRowObj.GetComponent<HorizontalLayoutGroup>();
            srHlg.padding = new RectOffset(8, 8, 4, 4);
            srHlg.spacing = 20;
            srHlg.childAlignment = TextAnchor.MiddleCenter;
            srHlg.childControlWidth = false;
            srHlg.childControlHeight = false;

            var slotsList = new List<SelectedSlotUI>();
            for (int i = 0; i < 3; i++)
            {
                var slotUI = CreateTeamSlot(slotsRowObj.transform, i);
                slotsList.Add(slotUI);
            }

            // Confirm Button - Large High-DPI mobile touch target
            GameObject confObj = new GameObject("ConfirmButton", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement), typeof(UIButtonAnimator));
            confObj.transform.SetParent(slotsRowObj.transform, false);
            var cfRect = confObj.GetComponent<RectTransform>();
            cfRect.sizeDelta = new Vector2(165, 74);
            var cfLe = confObj.GetComponent<LayoutElement>();
            cfLe.preferredWidth = 165;
            cfLe.preferredHeight = 74;

            var cfImg = confObj.GetComponent<Image>();
            cfImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtDir}/UI_Button_Confirm.png");
            cfImg.type = Image.Type.Simple;

            var confBtn = confObj.GetComponent<Button>();
            var confTmp = CreateTMPText(confObj.transform, "Text", "CONFIRM", 28, FontStyles.Bold, Color.white, 0, 0, 1, 1);
            confTmp.alignment = TextAlignmentOptions.Center;

            var confAnim = confObj.GetComponent<UIButtonAnimator>();
            confAnim.EnablePulse = false;

            return (teamObj, slotsList, confBtn, bannerObj, headTmp, subTmp, icImg);
        }

        private static SelectedSlotUI CreateTeamSlot(Transform parent, int slotIndex)
        {
            GameObject slotObj = new GameObject($"TeamSlot_{slotIndex + 1}", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement), typeof(SelectedSlotUI));
            slotObj.transform.SetParent(parent, false);

            var le = slotObj.GetComponent<LayoutElement>();
            le.preferredWidth = 115;
            le.preferredHeight = 145;

            var rect = slotObj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(115, 145);

            var img = slotObj.GetComponent<Image>();
            img.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtDir}/UI_Slot_Frame.png");
            img.type = Image.Type.Simple;

            // Portrait
            GameObject portObj = new GameObject("Portrait", typeof(RectTransform), typeof(Image));
            portObj.transform.SetParent(slotObj.transform, false);
            var pRect = portObj.GetComponent<RectTransform>();
            pRect.anchorMin = Vector2.zero;
            pRect.anchorMax = Vector2.one;
            pRect.sizeDelta = new Vector2(-10, -10);
            var portImg = portObj.GetComponent<Image>();
            portImg.preserveAspect = true;
            portImg.enabled = false;

            // Empty Number Label (1, 2, 3)
            GameObject numObj = new GameObject("EmptyLabel", typeof(RectTransform), typeof(TextMeshProUGUI));
            numObj.transform.SetParent(slotObj.transform, false);
            var nRect = numObj.GetComponent<RectTransform>();
            nRect.anchorMin = Vector2.zero;
            nRect.anchorMax = Vector2.one;
            nRect.sizeDelta = Vector2.zero;
            var numTmp = numObj.GetComponent<TextMeshProUGUI>();
            numTmp.font = GetFont();
            numTmp.text = (slotIndex + 1).ToString();
            numTmp.fontSize = 48;
            numTmp.fontStyle = FontStyles.Bold;
            numTmp.color = new Color(0.95f, 0.80f, 0.30f, 0.9f);
            numTmp.alignment = TextAlignmentOptions.Center;

            // Character Name Label (When filled)
            GameObject nameObj = new GameObject("NameText", typeof(RectTransform), typeof(TextMeshProUGUI));
            nameObj.transform.SetParent(slotObj.transform, false);
            var nmRect = nameObj.GetComponent<RectTransform>();
            nmRect.anchorMin = new Vector2(0, 0);
            nmRect.anchorMax = new Vector2(1, 0.35f);
            nmRect.sizeDelta = Vector2.zero;
            var nmTmp = nameObj.GetComponent<TextMeshProUGUI>();
            nmTmp.font = GetFont();
            nmTmp.text = "";
            nmTmp.fontSize = 17;
            nmTmp.fontStyle = FontStyles.Bold;
            nmTmp.color = new Color(0.98f, 0.82f, 0.35f);
            nmTmp.alignment = TextAlignmentOptions.Center;
            nmTmp.enableAutoSizing = true;
            nmTmp.fontSizeMin = 12;
            nmTmp.fontSizeMax = 19;

            // Remove Button (Small X Badge)
            GameObject remObj = new GameObject("RemoveButton", typeof(RectTransform), typeof(Image), typeof(Button));
            remObj.transform.SetParent(slotObj.transform, false);
            var rmRect = remObj.GetComponent<RectTransform>();
            rmRect.anchorMin = new Vector2(1, 1);
            rmRect.anchorMax = new Vector2(1, 1);
            rmRect.pivot = new Vector2(1, 1);
            rmRect.sizeDelta = new Vector2(32, 32);
            rmRect.anchoredPosition = new Vector2(4, 4);
            var rmImg = remObj.GetComponent<Image>();
            rmImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtDir}/Icon_Remove_Badge.png");
            rmImg.preserveAspect = true;
            var remBtn = remObj.GetComponent<Button>();
            remObj.SetActive(false);

            var slotUI = slotObj.GetComponent<SelectedSlotUI>();
            var sSO = new SerializedObject(slotUI);
            sSO.FindProperty("portraitImage").objectReferenceValue = portImg;
            sSO.FindProperty("nameText").objectReferenceValue = nmTmp;
            sSO.FindProperty("emptyStateRoot").objectReferenceValue = numObj;
            sSO.FindProperty("filledStateRoot").objectReferenceValue = portObj;
            sSO.FindProperty("removeButton").objectReferenceValue = remBtn;
            sSO.FindProperty("slotButton").objectReferenceValue = slotObj.GetComponent<Button>();
            sSO.ApplyModifiedProperties();

            return slotUI;
        }

        private static Button CreateBackButton(Transform parent)
        {
            GameObject backObj = new GameObject("BackButton", typeof(RectTransform), typeof(Image), typeof(Button), typeof(UIButtonAnimator));
            backObj.transform.SetParent(parent, false);

            var bRect = backObj.GetComponent<RectTransform>();
            bRect.anchorMin = new Vector2(0f, 1f);
            bRect.anchorMax = new Vector2(0f, 1f);
            bRect.pivot = new Vector2(0f, 1f);
            bRect.sizeDelta = new Vector2(220, 76);
            bRect.anchoredPosition = new Vector2(25, -25);

            var img = backObj.GetComponent<Image>();
            img.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtDir}/UI_Button_Back.png");
            img.type = Image.Type.Simple;

            // Chevron Icon
            GameObject chevObj = new GameObject("Chevron", typeof(RectTransform), typeof(Image));
            chevObj.transform.SetParent(backObj.transform, false);
            var cRect = chevObj.GetComponent<RectTransform>();
            cRect.anchorMin = new Vector2(0, 0.5f);
            cRect.anchorMax = new Vector2(0, 0.5f);
            cRect.pivot = new Vector2(0, 0.5f);
            cRect.sizeDelta = new Vector2(42, 42);
            cRect.anchoredPosition = new Vector2(20, 0);
            var cImg = chevObj.GetComponent<Image>();
            cImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtDir}/Icon_Chevron_Left.png");
            cImg.preserveAspect = true;

            // Text
            var backTmp = CreateTMPText(backObj.transform, "Text", "BACK", 28, FontStyles.Bold, new Color(1.0f, 0.88f, 0.35f), 0.34f, 0, 0.95f, 1);
            backTmp.alignment = TextAlignmentOptions.Left;

            return backObj.GetComponent<Button>();
        }

        private static Button CreateNavChevron(Transform parent, string name, Vector2 anchor, string spritePath)
        {
            GameObject navObj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(UIButtonAnimator));
            navObj.transform.SetParent(parent, false);

            var rect = navObj.GetComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(84, 84);
            rect.anchoredPosition = Vector2.zero;

            var img = navObj.GetComponent<Image>();
            img.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtDir}/UI_Square_Nav_Button.png");
            img.type = Image.Type.Simple;

            // Arrow Icon inside
            GameObject arrowObj = new GameObject("Arrow", typeof(RectTransform), typeof(Image));
            arrowObj.transform.SetParent(navObj.transform, false);
            var aRect = arrowObj.GetComponent<RectTransform>();
            aRect.anchorMin = new Vector2(0.5f, 0.5f);
            aRect.anchorMax = new Vector2(0.5f, 0.5f);
            aRect.pivot = new Vector2(0.5f, 0.5f);
            aRect.sizeDelta = new Vector2(48, 48);
            aRect.anchoredPosition = Vector2.zero;
            var aImg = arrowObj.GetComponent<Image>();
            aImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            aImg.preserveAspect = true;
            aImg.raycastTarget = false;

            return navObj.GetComponent<Button>();
        }

        private static Button CreateSelectButton(Transform parent, out TextMeshProUGUI btnTxt, out Image btnImg)
        {
            GameObject selObj = new GameObject("ToggleSelectButton", typeof(RectTransform), typeof(Image), typeof(Button), typeof(UIButtonAnimator));
            selObj.transform.SetParent(parent, false);

            var sRect = selObj.GetComponent<RectTransform>();
            sRect.anchorMin = new Vector2(0.5f, 0.340f);
            sRect.anchorMax = new Vector2(0.5f, 0.340f);
            sRect.pivot = new Vector2(0.5f, 0.5f);
            sRect.sizeDelta = new Vector2(360, 68);
            sRect.anchoredPosition = Vector2.zero;

            btnImg = selObj.GetComponent<Image>();
            btnImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtDir}/UI_Button_Select.png");
            btnImg.type = Image.Type.Simple;

            var btn = selObj.GetComponent<Button>();

            // Button Text
            GameObject txtObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            txtObj.transform.SetParent(selObj.transform, false);
            var tRect = txtObj.GetComponent<RectTransform>();
            tRect.anchorMin = Vector2.zero;
            tRect.anchorMax = Vector2.one;
            tRect.sizeDelta = Vector2.zero;
            btnTxt = txtObj.GetComponent<TextMeshProUGUI>();
            btnTxt.font = GetFont();
            btnTxt.text = "SELECT FOR TEAM";
            btnTxt.fontSize = 28;
            btnTxt.fontStyle = FontStyles.Bold;
            btnTxt.color = new Color(1.0f, 0.88f, 0.35f);
            btnTxt.alignment = TextAlignmentOptions.Center;

            return btn;
        }

        private static AbilitySlotUI CreateOrLoadAbilityCardPrefab()
        {
            string prefabPath = $"{PrefabDir}/AbilityCardPrefab.prefab";
            EnsureDirectory(PrefabDir);

            // Create Programmatic Card Object with generous dimensions for mobile and desktop readability
            GameObject cardObj = new GameObject("AbilityCardPrefab", typeof(RectTransform), typeof(Image), typeof(AbilitySlotUI), typeof(LayoutElement));
            var rect = cardObj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(210, 285);

            var le = cardObj.GetComponent<LayoutElement>();
            le.minWidth = 160;
            le.preferredWidth = 210;
            le.flexibleWidth = 1;
            le.minHeight = 240;
            le.preferredHeight = 285;
            le.flexibleHeight = 1;

            var bgImg = cardObj.GetComponent<Image>();
            bgImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtDir}/UI_Card_Ability_Frame.png");
            bgImg.type = Image.Type.Simple;

            // 1. Ability Name (Full top header tier, large bold and commanding)
            var nameTmp = CreateTMPText(cardObj.transform, "NameText", "Ability Name", 23, FontStyles.Bold, Color.white, 0.06f, 0.80f, 0.94f, 0.97f);
            nameTmp.alignment = TextAlignmentOptions.MidlineLeft;
            nameTmp.enableAutoSizing = true;
            nameTmp.fontSizeMin = 17;
            nameTmp.fontSizeMax = 25;

            // 2. Icon (Left-aligned mid tier, 50x50)
            GameObject iconObj = new GameObject("IconImage", typeof(RectTransform), typeof(Image));
            iconObj.transform.SetParent(cardObj.transform, false);
            var iRect = iconObj.GetComponent<RectTransform>();
            iRect.anchorMin = new Vector2(0.07f, 0.58f);
            iRect.anchorMax = new Vector2(0.07f, 0.58f);
            iRect.pivot = new Vector2(0f, 0.5f);
            iRect.sizeDelta = new Vector2(50, 50);
            var iImg = iconObj.GetComponent<Image>();
            iImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtDir}/Icon_Ability_Strike.png");
            iImg.preserveAspect = true;

            // 3. Type Text (Stacked top-half next to icon, 20pt bold radiant cyan/amber/crimson)
            var typeTmp = CreateTMPText(cardObj.transform, "TypeText", "SKILL", 20f, FontStyles.Bold, new Color(0.20f, 0.85f, 1.0f), 0.35f, 0.58f, 0.94f, 0.77f);
            typeTmp.alignment = TextAlignmentOptions.MidlineLeft;

            // 4. Cooldown Text (Stacked bottom-half next to icon, 18pt bold gold)
            var cdTmp = CreateTMPText(cardObj.transform, "CooldownText", "5s CD", 18f, FontStyles.Bold, new Color(1.0f, 0.85f, 0.30f), 0.35f, 0.43f, 0.94f, 0.58f);
            cdTmp.alignment = TextAlignmentOptions.MidlineLeft;

            // 5. Description Text (Clear, readable multi-line body, full 20pt for effortless reading)
            var descTmp = CreateTMPText(cardObj.transform, "DescriptionText", "Description of ability effect in combat.", 20f, FontStyles.Normal, new Color(0.95f, 0.97f, 1.0f), 0.06f, 0.04f, 0.94f, 0.40f);
            descTmp.alignment = TextAlignmentOptions.TopLeft;
            descTmp.enableAutoSizing = true;
            descTmp.fontSizeMin = 15f;
            descTmp.fontSizeMax = 22f;
            descTmp.lineSpacing = 1.2f;
            descTmp.textWrappingMode = TextWrappingModes.Normal;

            // Wire Serialized Properties on AbilitySlotUI
            var slotUI = cardObj.GetComponent<AbilitySlotUI>();
            var sSO = new SerializedObject(slotUI);
            sSO.FindProperty("nameText").objectReferenceValue = nameTmp;
            sSO.FindProperty("typeText").objectReferenceValue = typeTmp;
            sSO.FindProperty("cooldownText").objectReferenceValue = cdTmp;
            sSO.FindProperty("descriptionText").objectReferenceValue = descTmp;
            sSO.FindProperty("iconImage").objectReferenceValue = iImg;
            sSO.ApplyModifiedProperties();

            // Save Prefab
            GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(cardObj, prefabPath);
            Object.DestroyImmediate(cardObj);

            return savedPrefab.GetComponent<AbilitySlotUI>();
        }

        private static void AssignThemedAbilityIcons()
        {
            string abilityDir = "Assets/Clashura/Data/Characters/AbilityData";
            string[] guids = AssetDatabase.FindAssets("t:AbilityDataSO", new[] { abilityDir });

            var shieldSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtDir}/Icon_Ability_Shield.png");
            var strikeSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtDir}/Icon_Ability_Strike.png");
            var sunSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtDir}/Icon_Ability_Sun.png");
            var shockwaveSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtDir}/Icon_Ability_Shockwave.png");
            var magicSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtDir}/Icon_Ability_Magic.png");
            var flameSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtDir}/Icon_Ability_Flame.png");
            var frostSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtDir}/Icon_Ability_Frost.png");
            var shadowSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtDir}/Icon_Ability_Shadow.png");

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var ability = AssetDatabase.LoadAssetAtPath<AbilityDataSO>(path);
                if (ability == null) continue;

                string name = ability.AbilityName.ToLowerInvariant();
                string desc = ability.Description.ToLowerInvariant();
                Sprite target = null;

                // 1. High precision match by Ability Name
                if (name.Contains("bastion") || name.Contains("shield") || name.Contains("guard") || name.Contains("barrier") || name.Contains("defense"))
                    target = shieldSprite;
                else if (name.Contains("strike") || name.Contains("slash") || name.Contains("bash") || name.Contains("pummel") || name.Contains("flurry") || name.Contains("claw") || name.Contains("paw") || name.Contains("fist") || name.Contains("blade"))
                    target = strikeSprite;
                else if (name.Contains("wrath") || name.Contains("shockwave") || name.Contains("earth") || name.Contains("shatter") || name.Contains("skald") || name.Contains("quake") || name.Contains("rupture") || name.Contains("smash") || name.Contains("stomp"))
                    target = shockwaveSprite;
                else if (name.Contains("sun") || name.Contains("radiance") || name.Contains("solar") || name.Contains("dawn") || name.Contains("light") || name.Contains("holy") || name.Contains("heal"))
                    target = sunSprite;
                else if (name.Contains("flame") || name.Contains("fire") || name.Contains("inferno") || name.Contains("blaze") || name.Contains("burn") || name.Contains("magma") || name.Contains("ignite"))
                    target = flameSprite;
                else if (name.Contains("frost") || name.Contains("ice") || name.Contains("glacial") || name.Contains("freeze") || name.Contains("crystal") || name.Contains("chill"))
                    target = frostSprite;
                else if (name.Contains("magic") || name.Contains("arcane") || name.Contains("astral") || name.Contains("hex") || name.Contains("rune") || name.Contains("spell") || name.Contains("cosmic"))
                    target = magicSprite;
                else if (name.Contains("shadow") || name.Contains("night") || name.Contains("void") || name.Contains("dark") || name.Contains("stealth") || name.Contains("scythe") || name.Contains("poison"))
                    target = shadowSprite;
                // 2. Secondary match by Description
                else if (desc.Contains("shield") || desc.Contains("armor") || desc.Contains("absorb"))
                    target = shieldSprite;
                else if (desc.Contains("slash") || desc.Contains("strike") || desc.Contains("stun"))
                    target = strikeSprite;
                else if (desc.Contains("heal") || desc.Contains("restore") || desc.Contains("passive"))
                    target = sunSprite;
                else if (desc.Contains("shockwave") || desc.Contains("quake") || desc.Contains("knockup"))
                    target = shockwaveSprite;
                // 3. Fallback by AbilityType
                else
                {
                    switch (ability.AbilityType)
                    {
                        case AbilityType.Primary: target = strikeSprite; break;
                        case AbilityType.Skill: target = shieldSprite; break;
                        case AbilityType.Passive: target = sunSprite; break;
                        case AbilityType.Ultimate: target = shockwaveSprite; break;
                    }
                }

                if (target != null)
                {
                    var so = new SerializedObject(ability);
                    so.FindProperty("icon").objectReferenceValue = target;
                    so.ApplyModifiedProperties();
                    EditorUtility.SetDirty(ability);
                }
            }
            AssetDatabase.SaveAssets();
        }
        #endregion

        #region Settings Panel Building
        private static GameObject BuildSettingsPanel(Transform parent, UINavigationController navController)
        {
            GameObject panel = new GameObject("SettingsPanel", typeof(RectTransform));
            panel.transform.SetParent(parent, false);
            var rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;

            var settingsCtrl = panel.AddComponent<SettingsUIController>();

            // 1. Scenic Backdrop (Tinted deep midnight for settings focus)
            GameObject bgObj = new GameObject("Background", typeof(RectTransform), typeof(Image));
            bgObj.transform.SetParent(panel.transform, false);
            var bgRect = bgObj.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;
            var bgImg = bgObj.GetComponent<Image>();
            bgImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtDir}/CharSelect_Background.png");
            bgImg.color = new Color(0.50f, 0.55f, 0.70f, 1f);
            bgImg.type = Image.Type.Simple;

            // Safe Area
            GameObject safeObj = new GameObject("SafeArea", typeof(RectTransform), typeof(SafeAreaHandler));
            safeObj.transform.SetParent(panel.transform, false);
            var safeRect = safeObj.GetComponent<RectTransform>();
            safeRect.anchorMin = Vector2.zero;
            safeRect.anchorMax = Vector2.one;
            safeRect.sizeDelta = Vector2.zero;

            // 2. Header Bar
            // Back Button (Top-Left)
            Button backBtn = CreateBackButton(safeRect.transform);

            // Title & Subtitle (Top-Center)
            GameObject titleObj = new GameObject("TitleSection", typeof(RectTransform));
            titleObj.transform.SetParent(safeRect, false);
            var tRect = titleObj.GetComponent<RectTransform>();
            tRect.anchorMin = new Vector2(0.5f, 0.94f);
            tRect.anchorMax = new Vector2(0.5f, 0.94f);
            tRect.pivot = new Vector2(0.5f, 0.5f);
            tRect.sizeDelta = new Vector2(600, 90);
            tRect.anchoredPosition = Vector2.zero;

            var titleTmp = CreateTMPText(titleObj.transform, "Title", "SETTINGS", 62, FontStyles.Bold, new Color(1.0f, 0.85f, 0.30f), 0, 0.35f, 1, 1);
            titleTmp.alignment = TextAlignmentOptions.Center;
            titleTmp.characterSpacing = 4f;

            var subTmp = CreateTMPText(titleObj.transform, "Subtitle", "GAMEPLAY & SYSTEM CONFIGURATION", 18, FontStyles.Bold, new Color(0.65f, 0.80f, 0.98f, 0.9f), 0, 0, 1, 0.35f);
            subTmp.alignment = TextAlignmentOptions.Center;
            subTmp.characterSpacing = 5f;

            // Reset Defaults Button (Top-Right)
            GameObject rstObj = new GameObject("ResetDefaultsButton", typeof(RectTransform), typeof(Image), typeof(Button), typeof(UIButtonAnimator));
            rstObj.transform.SetParent(safeRect, false);
            var rstRect = rstObj.GetComponent<RectTransform>();
            rstRect.anchorMin = new Vector2(1f, 1f);
            rstRect.anchorMax = new Vector2(1f, 1f);
            rstRect.pivot = new Vector2(1f, 1f);
            rstRect.sizeDelta = new Vector2(230, 72);
            rstRect.anchoredPosition = new Vector2(-25, -27);

            var rstImg = rstObj.GetComponent<Image>();
            rstImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SettingsArtDir}/UI_Button_Reset.png");
            rstImg.type = Image.Type.Simple;

            var rstBtn = rstObj.GetComponent<Button>();
            var rstTmp = CreateTMPText(rstObj.transform, "Text", "RESET DEFAULTS", 22, FontStyles.Bold, new Color(1.0f, 0.88f, 0.70f), 0, 0, 1, 1);
            rstTmp.alignment = TextAlignmentOptions.Center;

            // 3. Category Navigation Dock (Left Column, Width ~370px)
            GameObject navDock = new GameObject("CategoryNavDock", typeof(RectTransform), typeof(VerticalLayoutGroup));
            navDock.transform.SetParent(safeRect, false);
            var ndRect = navDock.GetComponent<RectTransform>();
            ndRect.anchorMin = new Vector2(0.018f, 0.04f);
            ndRect.anchorMax = new Vector2(0.245f, 0.87f);
            ndRect.sizeDelta = Vector2.zero;

            var vlg = navDock.GetComponent<VerticalLayoutGroup>();
            vlg.spacing = 14;
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;

            Button audioTab = CreateCategoryTab(navDock.transform, "Tab_Audio", "AUDIO", $"{SettingsArtDir}/Icon_Tab_Audio.png");
            Button graphicsTab = CreateCategoryTab(navDock.transform, "Tab_Graphics", "GRAPHICS", $"{SettingsArtDir}/Icon_Tab_Graphics.png");
            Button controlsTab = CreateCategoryTab(navDock.transform, "Tab_Controls", "CONTROLS", $"{SettingsArtDir}/Icon_Tab_Controls.png");
            Button gameplayTab = CreateCategoryTab(navDock.transform, "Tab_Gameplay", "GAMEPLAY", $"{SettingsArtDir}/Icon_Tab_Gameplay.png");
            Button accessTab = CreateCategoryTab(navDock.transform, "Tab_Accessibility", "ACCESSIBILITY", $"{SettingsArtDir}/Icon_Tab_Accessibility.png");

            // 4. Content Viewport Frame (Right Column)
            GameObject contentFrame = new GameObject("ContentViewportFrame", typeof(RectTransform), typeof(Image));
            contentFrame.transform.SetParent(safeRect, false);
            var cfRect = contentFrame.GetComponent<RectTransform>();
            cfRect.anchorMin = new Vector2(0.258f, 0.04f);
            cfRect.anchorMax = new Vector2(0.982f, 0.87f);
            cfRect.sizeDelta = Vector2.zero;

            var cfImg = contentFrame.GetComponent<Image>();
            cfImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SettingsArtDir}/UI_Settings_Frame.png");
            cfImg.type = Image.Type.Simple;

            // Content Panels
            // A. Audio Panel
            GameObject audioPanel = CreateContentContainer(contentFrame.transform, "AudioContentPanel");
            var (masterSlider, masterTxt) = CreateSliderRow(audioPanel.transform, "Row_MasterVolume", "MASTER VOLUME", 0f, 100f);
            var (musicSlider, musicTxt) = CreateSliderRow(audioPanel.transform, "Row_MusicVolume", "MUSIC VOLUME", 0f, 100f);
            var (sfxSlider, sfxTxt) = CreateSliderRow(audioPanel.transform, "Row_SFXVolume", "SOUND EFFECTS", 0f, 100f);
            var (muteBtn, muteTxt, muteFill) = CreateToggleRow(audioPanel.transform, "Row_MuteAll", "MUTE ALL AUDIO");

            // B. Graphics Panel
            GameObject graphicsPanel = CreateContentContainer(contentFrame.transform, "GraphicsContentPanel");
            var (qPrev, qNext, qTxt) = CreateStepperRow(graphicsPanel.transform, "Row_Quality", "GRAPHICS QUALITY");
            var (fpsPrev, fpsNext, fpsTxt) = CreateStepperRow(graphicsPanel.transform, "Row_FPSLimit", "TARGET FRAME RATE");
            var (vsyncBtn, vsyncTxt, vsyncFill) = CreateToggleRow(graphicsPanel.transform, "Row_VSync", "VERTICAL SYNC (VSYNC)");
            var (fullBtn, fullTxt, fullFill) = CreateToggleRow(graphicsPanel.transform, "Row_Fullscreen", "FULLSCREEN MODE");
            var (resPrev, resNext, resTxt) = CreateStepperRow(graphicsPanel.transform, "Row_Resolution", "SCREEN RESOLUTION");

            // C. Controls Panel
            GameObject controlsPanel = CreateContentContainer(contentFrame.transform, "ControlsContentPanel");
            var (sensSlider, sensTxt) = CreateSliderRow(controlsPanel.transform, "Row_Sensitivity", "TOUCH SENSITIVITY", 0f, 100f);
            var (btnSizePrev, btnSizeNext, btnSizeTxt) = CreateStepperRow(controlsPanel.transform, "Row_ButtonSize", "CONTROL BUTTON SIZE");
            var (vibBtn, vibTxt, vibFill) = CreateToggleRow(controlsPanel.transform, "Row_Vibration", "HAPTIC VIBRATION");
            var (layoutPrev, layoutNext, layoutTxt) = CreateStepperRow(controlsPanel.transform, "Row_Layout", "CONTROL SCHEME LAYOUT");

            // D. Gameplay Panel
            GameObject gameplayPanel = CreateContentContainer(contentFrame.transform, "GameplayContentPanel");
            var (dmgBtn, dmgTxt, dmgFill) = CreateToggleRow(gameplayPanel.transform, "Row_DamageNumbers", "FLOATING DAMAGE NUMBERS");
            var (shakeBtn, shakeTxt, shakeFill) = CreateToggleRow(gameplayPanel.transform, "Row_ScreenShake", "SCREEN IMPACT SHAKE");
            var (notifBtn, notifTxt, notifFill) = CreateToggleRow(gameplayPanel.transform, "Row_BattleNotifs", "BATTLE NOTIFICATIONS");
            var (vfxBtn, vfxTxt, vfxFill) = CreateToggleRow(gameplayPanel.transform, "Row_CombatVFX", "COMBAT PARTICLE EFFECTS");

            // E. Accessibility Panel
            GameObject accessPanel = CreateContentContainer(contentFrame.transform, "AccessibilityContentPanel");
            var (scaleSlider, scaleTxt) = CreateSliderRow(accessPanel.transform, "Row_UIScale", "INTERFACE SCALE (UI)", 80f, 130f);
            var (txtSizePrev, txtSizeNext, txtSizeTxt) = CreateStepperRow(accessPanel.transform, "Row_TextSize", "TYPOGRAPHY TEXT SIZE");
            var (redEffBtn, redEffTxt, redEffFill) = CreateToggleRow(accessPanel.transform, "Row_ReduceEffects", "REDUCE FLASHING EFFECTS");
            var (highConBtn, highConTxt, highConFill) = CreateToggleRow(accessPanel.transform, "Row_HighContrast", "HIGH CONTRAST MODE");

            // 5. Reset Confirmation Popup Modal
            ResetConfirmationPopupUI resetPopup = BuildResetConfirmationPopup(safeRect.transform);

            // Wire Serialized Properties on SettingsUIController
            var sSO = new SerializedObject(settingsCtrl);
            sSO.FindProperty("audioTabButton").objectReferenceValue = audioTab;
            sSO.FindProperty("graphicsTabButton").objectReferenceValue = graphicsTab;
            sSO.FindProperty("controlsTabButton").objectReferenceValue = controlsTab;
            sSO.FindProperty("gameplayTabButton").objectReferenceValue = gameplayTab;
            sSO.FindProperty("accessibilityTabButton").objectReferenceValue = accessTab;

            sSO.FindProperty("audioContentPanel").objectReferenceValue = audioPanel;
            sSO.FindProperty("graphicsContentPanel").objectReferenceValue = graphicsPanel;
            sSO.FindProperty("controlsContentPanel").objectReferenceValue = controlsPanel;
            sSO.FindProperty("gameplayContentPanel").objectReferenceValue = gameplayPanel;
            sSO.FindProperty("accessibilityContentPanel").objectReferenceValue = accessPanel;

            sSO.FindProperty("backButton").objectReferenceValue = backBtn;
            sSO.FindProperty("resetDefaultsButton").objectReferenceValue = rstBtn;
            sSO.FindProperty("resetPopup").objectReferenceValue = resetPopup;

            sSO.FindProperty("masterVolumeSlider").objectReferenceValue = masterSlider;
            sSO.FindProperty("masterVolumeText").objectReferenceValue = masterTxt;
            sSO.FindProperty("musicVolumeSlider").objectReferenceValue = musicSlider;
            sSO.FindProperty("musicVolumeText").objectReferenceValue = musicTxt;
            sSO.FindProperty("sfxVolumeSlider").objectReferenceValue = sfxSlider;
            sSO.FindProperty("sfxVolumeText").objectReferenceValue = sfxTxt;
            sSO.FindProperty("muteAllToggleButton").objectReferenceValue = muteBtn;
            sSO.FindProperty("muteAllToggleText").objectReferenceValue = muteTxt;
            sSO.FindProperty("muteAllToggleFill").objectReferenceValue = muteFill;

            sSO.FindProperty("qualityPrevButton").objectReferenceValue = qPrev;
            sSO.FindProperty("qualityNextButton").objectReferenceValue = qNext;
            sSO.FindProperty("qualityText").objectReferenceValue = qTxt;
            sSO.FindProperty("fpsPrevButton").objectReferenceValue = fpsPrev;
            sSO.FindProperty("fpsNextButton").objectReferenceValue = fpsNext;
            sSO.FindProperty("fpsText").objectReferenceValue = fpsTxt;
            sSO.FindProperty("vsyncToggleButton").objectReferenceValue = vsyncBtn;
            sSO.FindProperty("vsyncToggleText").objectReferenceValue = vsyncTxt;
            sSO.FindProperty("vsyncToggleFill").objectReferenceValue = vsyncFill;
            sSO.FindProperty("fullscreenToggleButton").objectReferenceValue = fullBtn;
            sSO.FindProperty("fullscreenToggleText").objectReferenceValue = fullTxt;
            sSO.FindProperty("fullscreenToggleFill").objectReferenceValue = fullFill;
            sSO.FindProperty("resPrevButton").objectReferenceValue = resPrev;
            sSO.FindProperty("resNextButton").objectReferenceValue = resNext;
            sSO.FindProperty("resText").objectReferenceValue = resTxt;

            sSO.FindProperty("sensitivitySlider").objectReferenceValue = sensSlider;
            sSO.FindProperty("sensitivityText").objectReferenceValue = sensTxt;
            sSO.FindProperty("btnSizePrevButton").objectReferenceValue = btnSizePrev;
            sSO.FindProperty("btnSizeNextButton").objectReferenceValue = btnSizeNext;
            sSO.FindProperty("btnSizeText").objectReferenceValue = btnSizeTxt;
            sSO.FindProperty("vibrationToggleButton").objectReferenceValue = vibBtn;
            sSO.FindProperty("vibrationToggleText").objectReferenceValue = vibTxt;
            sSO.FindProperty("vibrationToggleFill").objectReferenceValue = vibFill;
            sSO.FindProperty("layoutPrevButton").objectReferenceValue = layoutPrev;
            sSO.FindProperty("layoutNextButton").objectReferenceValue = layoutNext;
            sSO.FindProperty("layoutText").objectReferenceValue = layoutTxt;

            sSO.FindProperty("damageNumbersToggleButton").objectReferenceValue = dmgBtn;
            sSO.FindProperty("damageNumbersToggleText").objectReferenceValue = dmgTxt;
            sSO.FindProperty("damageNumbersToggleFill").objectReferenceValue = dmgFill;
            sSO.FindProperty("screenShakeToggleButton").objectReferenceValue = shakeBtn;
            sSO.FindProperty("screenShakeToggleText").objectReferenceValue = shakeTxt;
            sSO.FindProperty("screenShakeToggleFill").objectReferenceValue = shakeFill;
            sSO.FindProperty("battleNotifsToggleButton").objectReferenceValue = notifBtn;
            sSO.FindProperty("battleNotifsToggleText").objectReferenceValue = notifTxt;
            sSO.FindProperty("battleNotifsToggleFill").objectReferenceValue = notifFill;
            sSO.FindProperty("combatVFXToggleButton").objectReferenceValue = vfxBtn;
            sSO.FindProperty("combatVFXToggleText").objectReferenceValue = vfxTxt;
            sSO.FindProperty("combatVFXToggleFill").objectReferenceValue = vfxFill;

            sSO.FindProperty("uiScaleSlider").objectReferenceValue = scaleSlider;
            sSO.FindProperty("uiScaleText").objectReferenceValue = scaleTxt;
            sSO.FindProperty("textSizePrevButton").objectReferenceValue = txtSizePrev;
            sSO.FindProperty("textSizeNextButton").objectReferenceValue = txtSizeNext;
            sSO.FindProperty("textSizeText").objectReferenceValue = txtSizeTxt;
            sSO.FindProperty("reduceEffectsToggleButton").objectReferenceValue = redEffBtn;
            sSO.FindProperty("reduceEffectsToggleText").objectReferenceValue = redEffTxt;
            sSO.FindProperty("reduceEffectsToggleFill").objectReferenceValue = redEffFill;
            sSO.FindProperty("highContrastToggleButton").objectReferenceValue = highConBtn;
            sSO.FindProperty("highContrastToggleText").objectReferenceValue = highConTxt;
            sSO.FindProperty("highContrastToggleFill").objectReferenceValue = highConFill;

            sSO.ApplyModifiedProperties();
            EditorUtility.SetDirty(settingsCtrl);

            return panel;
        }

        private static Button CreateCategoryTab(Transform parent, string name, string label, string iconPath)
        {
            GameObject tabObj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement), typeof(UIButtonAnimator));
            tabObj.transform.SetParent(parent, false);

            var le = tabObj.GetComponent<LayoutElement>();
            le.preferredWidth = 350;
            le.preferredHeight = 76;

            var rect = tabObj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(350, 76);

            var img = tabObj.GetComponent<Image>();
            img.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SettingsArtDir}/UI_Tab_Normal.png");
            img.type = Image.Type.Simple;

            var btn = tabObj.GetComponent<Button>();

            // Icon
            GameObject iconObj = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconObj.transform.SetParent(tabObj.transform, false);
            var iRect = iconObj.GetComponent<RectTransform>();
            iRect.anchorMin = new Vector2(0, 0.5f);
            iRect.anchorMax = new Vector2(0, 0.5f);
            iRect.pivot = new Vector2(0, 0.5f);
            iRect.sizeDelta = new Vector2(44, 44);
            iRect.anchoredPosition = new Vector2(20, 0);
            var iImg = iconObj.GetComponent<Image>();
            iImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(iconPath);
            iImg.preserveAspect = true;
            iImg.raycastTarget = false;

            // Label
            var lblTmp = CreateTMPText(tabObj.transform, "Label", label, 24, FontStyles.Bold, Color.white, 0.22f, 0, 0.98f, 1);
            lblTmp.alignment = TextAlignmentOptions.MidlineLeft;

            return btn;
        }

        private static GameObject CreateContentContainer(Transform parent, string name)
        {
            GameObject contObj = new GameObject(name, typeof(RectTransform), typeof(VerticalLayoutGroup));
            contObj.transform.SetParent(parent, false);
            var rect = contObj.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;

            var vlg = contObj.GetComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(45, 45, 35, 35);
            vlg.spacing = 18;
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;

            return contObj;
        }

        private static (Slider, TextMeshProUGUI) CreateSliderRow(Transform parent, string name, string label, float minVal, float maxVal)
        {
            GameObject rowObj = new GameObject(name, typeof(RectTransform), typeof(LayoutElement));
            rowObj.transform.SetParent(parent, false);
            var le = rowObj.GetComponent<LayoutElement>();
            le.preferredHeight = 74;

            var rect = rowObj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(0, 74);

            // Label (Left)
            var lblTmp = CreateTMPText(rowObj.transform, "Label", label, 24, FontStyles.Bold, Color.white, 0.02f, 0, 0.38f, 1);
            lblTmp.alignment = TextAlignmentOptions.MidlineLeft;

            // Slider (Center)
            GameObject sliderObj = new GameObject("Slider", typeof(RectTransform), typeof(Slider));
            sliderObj.transform.SetParent(rowObj.transform, false);
            var slRect = sliderObj.GetComponent<RectTransform>();
            slRect.anchorMin = new Vector2(0.40f, 0.5f);
            slRect.anchorMax = new Vector2(0.85f, 0.5f);
            slRect.pivot = new Vector2(0.5f, 0.5f);
            slRect.sizeDelta = new Vector2(0, 24);

            var slider = sliderObj.GetComponent<Slider>();
            slider.minValue = minVal;
            slider.maxValue = maxVal;

            // Background Track
            GameObject bgObj = new GameObject("Background", typeof(RectTransform), typeof(Image));
            bgObj.transform.SetParent(sliderObj.transform, false);
            var bgRect = bgObj.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;
            var bgImg = bgObj.GetComponent<Image>();
            bgImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SettingsArtDir}/UI_Slider_Track.png");
            bgImg.type = Image.Type.Simple;

            // Fill Area & Fill
            GameObject faObj = new GameObject("Fill Area", typeof(RectTransform));
            faObj.transform.SetParent(sliderObj.transform, false);
            var faRect = faObj.GetComponent<RectTransform>();
            faRect.anchorMin = new Vector2(0, 0.25f);
            faRect.anchorMax = new Vector2(1, 0.75f);
            faRect.sizeDelta = new Vector2(-20, 0);

            GameObject fillObj = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fillObj.transform.SetParent(faObj.transform, false);
            var fRect = fillObj.GetComponent<RectTransform>();
            fRect.sizeDelta = Vector2.zero;
            var fImg = fillObj.GetComponent<Image>();
            fImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SettingsArtDir}/UI_Slider_Fill.png");
            fImg.type = Image.Type.Simple;

            // Handle Slide Area & Handle
            GameObject haObj = new GameObject("Handle Slide Area", typeof(RectTransform));
            haObj.transform.SetParent(sliderObj.transform, false);
            var haRect = haObj.GetComponent<RectTransform>();
            haRect.anchorMin = Vector2.zero;
            haRect.anchorMax = Vector2.one;
            haRect.sizeDelta = new Vector2(-20, 0);

            GameObject handleObj = new GameObject("Handle", typeof(RectTransform), typeof(Image));
            handleObj.transform.SetParent(haObj.transform, false);
            var hRect = handleObj.GetComponent<RectTransform>();
            hRect.sizeDelta = new Vector2(46, 46);
            var hImg = handleObj.GetComponent<Image>();
            hImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SettingsArtDir}/UI_Slider_Knob.png");
            hImg.preserveAspect = true;

            slider.fillRect = fRect;
            slider.handleRect = hRect;
            slider.targetGraphic = hImg;
            slider.direction = Slider.Direction.LeftToRight;

            // Value Text (Right)
            var valTmp = CreateTMPText(rowObj.transform, "ValueText", $"{Mathf.RoundToInt(maxVal)}%", 22, FontStyles.Bold, new Color(1.0f, 0.85f, 0.35f), 0.87f, 0, 0.98f, 1);
            valTmp.alignment = TextAlignmentOptions.MidlineRight;

            return (slider, valTmp);
        }

        private static (Button, TextMeshProUGUI, Image) CreateToggleRow(Transform parent, string name, string label)
        {
            GameObject rowObj = new GameObject(name, typeof(RectTransform), typeof(LayoutElement));
            rowObj.transform.SetParent(parent, false);
            var le = rowObj.GetComponent<LayoutElement>();
            le.preferredHeight = 74;

            var rect = rowObj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(0, 74);

            // Label (Left)
            var lblTmp = CreateTMPText(rowObj.transform, "Label", label, 24, FontStyles.Bold, Color.white, 0.02f, 0, 0.48f, 1);
            lblTmp.alignment = TextAlignmentOptions.MidlineLeft;

            // Toggle Button (Center)
            GameObject toggleObj = new GameObject("ToggleButton", typeof(RectTransform), typeof(Image), typeof(Button), typeof(UIButtonAnimator));
            toggleObj.transform.SetParent(rowObj.transform, false);
            var tgRect = toggleObj.GetComponent<RectTransform>();
            tgRect.anchorMin = new Vector2(0.60f, 0.5f);
            tgRect.anchorMax = new Vector2(0.60f, 0.5f);
            tgRect.pivot = new Vector2(0.5f, 0.5f);
            tgRect.sizeDelta = new Vector2(110, 46);

            var tgImg = toggleObj.GetComponent<Image>();
            tgImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SettingsArtDir}/UI_Toggle_Track.png");
            tgImg.type = Image.Type.Simple;

            var btn = toggleObj.GetComponent<Button>();

            // Fill / Knob indicator inside toggle
            GameObject fillObj = new GameObject("ToggleFill", typeof(RectTransform), typeof(Image));
            fillObj.transform.SetParent(toggleObj.transform, false);
            var flRect = fillObj.GetComponent<RectTransform>();
            flRect.anchorMin = Vector2.zero;
            flRect.anchorMax = Vector2.one;
            flRect.sizeDelta = new Vector2(-6, -6);
            var flImg = fillObj.GetComponent<Image>();
            flImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SettingsArtDir}/UI_Toggle_Knob.png");
            flImg.color = new Color(0.20f, 0.85f, 0.45f, 1f);
            flImg.preserveAspect = true;

            // State Text (Right)
            var stateTmp = CreateTMPText(rowObj.transform, "StateText", "ON", 22, FontStyles.Bold, new Color(0.30f, 1.0f, 0.55f), 0.74f, 0, 0.95f, 1);
            stateTmp.alignment = TextAlignmentOptions.MidlineLeft;

            return (btn, stateTmp, flImg);
        }

        private static (Button, Button, TextMeshProUGUI) CreateStepperRow(Transform parent, string name, string label)
        {
            GameObject rowObj = new GameObject(name, typeof(RectTransform), typeof(LayoutElement));
            rowObj.transform.SetParent(parent, false);
            var le = rowObj.GetComponent<LayoutElement>();
            le.preferredHeight = 74;

            var rect = rowObj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(0, 74);

            // Label (Left)
            var lblTmp = CreateTMPText(rowObj.transform, "Label", label, 24, FontStyles.Bold, Color.white, 0.02f, 0, 0.40f, 1);
            lblTmp.alignment = TextAlignmentOptions.MidlineLeft;

            // Stepper Container (Right side)
            GameObject stepObj = new GameObject("StepperContainer", typeof(RectTransform));
            stepObj.transform.SetParent(rowObj.transform, false);
            var stRect = stepObj.GetComponent<RectTransform>();
            stRect.anchorMin = new Vector2(0.50f, 0.5f);
            stRect.anchorMax = new Vector2(0.95f, 0.5f);
            stRect.pivot = new Vector2(0.5f, 0.5f);
            stRect.sizeDelta = new Vector2(0, 52);

            // Left Chevron (<)
            GameObject prevObj = new GameObject("PrevBtn", typeof(RectTransform), typeof(Image), typeof(Button), typeof(UIButtonAnimator));
            prevObj.transform.SetParent(stepObj.transform, false);
            var pRect = prevObj.GetComponent<RectTransform>();
            pRect.anchorMin = new Vector2(0, 0.5f);
            pRect.anchorMax = new Vector2(0, 0.5f);
            pRect.pivot = new Vector2(0, 0.5f);
            pRect.sizeDelta = new Vector2(48, 48);
            var pImg = prevObj.GetComponent<Image>();
            pImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtDir}/Icon_Chevron_Left.png");
            pImg.preserveAspect = true;
            var prevBtn = prevObj.GetComponent<Button>();

            // Value Box
            GameObject boxObj = new GameObject("ValueBox", typeof(RectTransform), typeof(Image));
            boxObj.transform.SetParent(stepObj.transform, false);
            var bRect = boxObj.GetComponent<RectTransform>();
            bRect.anchorMin = new Vector2(0.15f, 0f);
            bRect.anchorMax = new Vector2(0.85f, 1f);
            bRect.sizeDelta = Vector2.zero;
            var bImg = boxObj.GetComponent<Image>();
            bImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SettingsArtDir}/UI_Stepper_Box.png");
            bImg.type = Image.Type.Simple;

            var valTmp = CreateTMPText(boxObj.transform, "Text", "OPTION", 22, FontStyles.Bold, new Color(1.0f, 0.85f, 0.35f), 0, 0, 1, 1);
            valTmp.alignment = TextAlignmentOptions.Center;

            // Right Chevron (>)
            GameObject nextObj = new GameObject("NextBtn", typeof(RectTransform), typeof(Image), typeof(Button), typeof(UIButtonAnimator));
            nextObj.transform.SetParent(stepObj.transform, false);
            var nRect = nextObj.GetComponent<RectTransform>();
            nRect.anchorMin = new Vector2(1, 0.5f);
            nRect.anchorMax = new Vector2(1, 0.5f);
            nRect.pivot = new Vector2(1, 0.5f);
            nRect.sizeDelta = new Vector2(48, 48);
            var nImg = nextObj.GetComponent<Image>();
            nImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtDir}/Icon_Chevron_Right.png");
            nImg.preserveAspect = true;
            var nextBtn = nextObj.GetComponent<Button>();

            return (prevBtn, nextBtn, valTmp);
        }

        private static ResetConfirmationPopupUI BuildResetConfirmationPopup(Transform parent)
        {
            GameObject root = new GameObject("ResetConfirmationPopup", typeof(RectTransform), typeof(ResetConfirmationPopupUI));
            root.transform.SetParent(parent, false);
            var rootRect = root.GetComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.sizeDelta = Vector2.zero;

            // Overlay
            GameObject overlay = new GameObject("DarkOverlay", typeof(RectTransform), typeof(Image));
            overlay.transform.SetParent(root.transform, false);
            var oRect = overlay.GetComponent<RectTransform>();
            oRect.anchorMin = Vector2.zero;
            oRect.anchorMax = Vector2.one;
            oRect.sizeDelta = Vector2.zero;
            var oImg = overlay.GetComponent<Image>();
            oImg.color = new Color(0.02f, 0.04f, 0.08f, 0.82f);
            oImg.raycastTarget = true;

            // Dialog Panel
            GameObject dialog = new GameObject("PopupPanel", typeof(RectTransform), typeof(Image));
            dialog.transform.SetParent(root.transform, false);
            var dRect = dialog.GetComponent<RectTransform>();
            dRect.anchorMin = new Vector2(0.5f, 0.5f);
            dRect.anchorMax = new Vector2(0.5f, 0.5f);
            dRect.pivot = new Vector2(0.5f, 0.5f);
            dRect.sizeDelta = new Vector2(760, 420);
            var dImg = dialog.GetComponent<Image>();
            dImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtDir}/UI_Popup_Frame.png");
            dImg.type = Image.Type.Simple;

            // Heading
            var headTmp = CreateTMPText(dialog.transform, "HeadingText", "RESET ALL SETTINGS?", 32, FontStyles.Bold, new Color(1.0f, 0.85f, 0.30f), 0.05f, 0.68f, 0.95f, 0.88f);
            headTmp.alignment = TextAlignmentOptions.Center;
            headTmp.characterSpacing = 3f;

            // Message
            var msgTmp = CreateTMPText(dialog.transform, "MessageText", "Are you sure you want to restore default settings?", 23, FontStyles.Bold, Color.white, 0.05f, 0.48f, 0.95f, 0.64f);
            msgTmp.alignment = TextAlignmentOptions.Center;

            // Subtitle
            var subTmp = CreateTMPText(dialog.transform, "SubtitleText", "All custom audio, graphics, controls and gameplay adjustments will be reverted.", 18, FontStyles.Normal, new Color(0.75f, 0.85f, 0.98f, 0.95f), 0.05f, 0.32f, 0.95f, 0.46f);
            subTmp.alignment = TextAlignmentOptions.Center;

            // Cancel Button
            GameObject cancelObj = new GameObject("CancelButton", typeof(RectTransform), typeof(Image), typeof(Button), typeof(UIButtonAnimator));
            cancelObj.transform.SetParent(dialog.transform, false);
            var cRect = cancelObj.GetComponent<RectTransform>();
            cRect.anchorMin = new Vector2(0.28f, 0f);
            cRect.anchorMax = new Vector2(0.28f, 0f);
            cRect.pivot = new Vector2(0.5f, 0f);
            cRect.sizeDelta = new Vector2(200, 64);
            cRect.anchoredPosition = new Vector2(0, 32);

            var cImg = cancelObj.GetComponent<Image>();
            cImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtDir}/UI_Button_Popup_OK.png");
            cImg.type = Image.Type.Simple;

            var cancelBtn = cancelObj.GetComponent<Button>();
            var cTmp = CreateTMPText(cancelObj.transform, "Text", "CANCEL", 24, FontStyles.Bold, Color.white, 0, 0, 1, 1);
            cTmp.alignment = TextAlignmentOptions.Center;

            // Confirm Button
            GameObject confObj = new GameObject("ConfirmButton", typeof(RectTransform), typeof(Image), typeof(Button), typeof(UIButtonAnimator));
            confObj.transform.SetParent(dialog.transform, false);
            var cfRect = confObj.GetComponent<RectTransform>();
            cfRect.anchorMin = new Vector2(0.72f, 0f);
            cfRect.anchorMax = new Vector2(0.72f, 0f);
            cfRect.pivot = new Vector2(0.5f, 0f);
            cfRect.sizeDelta = new Vector2(200, 64);
            cfRect.anchoredPosition = new Vector2(0, 32);

            var cfImg = confObj.GetComponent<Image>();
            cfImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SettingsArtDir}/UI_Button_Reset.png");
            cfImg.type = Image.Type.Simple;

            var confBtn = confObj.GetComponent<Button>();
            var cfTmp = CreateTMPText(confObj.transform, "Text", "CONFIRM", 24, FontStyles.Bold, Color.white, 0, 0, 1, 1);
            cfTmp.alignment = TextAlignmentOptions.Center;

            // Wire properties
            var popupUI = root.GetComponent<ResetConfirmationPopupUI>();
            var pSO = new SerializedObject(popupUI);
            pSO.FindProperty("darkOverlay").objectReferenceValue = overlay;
            pSO.FindProperty("popupPanel").objectReferenceValue = dialog;
            pSO.FindProperty("headingText").objectReferenceValue = headTmp;
            pSO.FindProperty("messageText").objectReferenceValue = msgTmp;
            pSO.FindProperty("subtitleText").objectReferenceValue = subTmp;
            pSO.FindProperty("confirmButton").objectReferenceValue = confBtn;
            pSO.FindProperty("cancelButton").objectReferenceValue = cancelBtn;
            pSO.ApplyModifiedProperties();

            root.SetActive(false);
            return popupUI;
        }
        #endregion

        #region Confirmation Popup Building
        private static ConfirmationPopupUI BuildConfirmationPopup(Transform parent)
        {
            // Root container
            GameObject root = new GameObject("ConfirmationPopup", typeof(RectTransform), typeof(ConfirmationPopupUI));
            root.transform.SetParent(parent, false);
            var rootRect = root.GetComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.sizeDelta = Vector2.zero;

            // 1. Dark semi-transparent overlay
            GameObject overlay = new GameObject("DarkOverlay", typeof(RectTransform), typeof(Image));
            overlay.transform.SetParent(root.transform, false);
            var oRect = overlay.GetComponent<RectTransform>();
            oRect.anchorMin = Vector2.zero;
            oRect.anchorMax = Vector2.one;
            oRect.sizeDelta = Vector2.zero;
            var oImg = overlay.GetComponent<Image>();
            oImg.color = new Color(0.02f, 0.04f, 0.08f, 0.82f);
            oImg.raycastTarget = true;

            // 2. Centered Ornate Dialog Frame (860 x 560 px)
            GameObject dialog = new GameObject("PopupPanel", typeof(RectTransform), typeof(Image));
            dialog.transform.SetParent(root.transform, false);
            var dRect = dialog.GetComponent<RectTransform>();
            dRect.anchorMin = new Vector2(0.5f, 0.5f);
            dRect.anchorMax = new Vector2(0.5f, 0.5f);
            dRect.pivot = new Vector2(0.5f, 0.5f);
            dRect.sizeDelta = new Vector2(860, 560);
            var dImg = dialog.GetComponent<Image>();
            dImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtDir}/UI_Popup_Frame.png");
            dImg.type = Image.Type.Simple;

            // 3. Status Emblem / Icon (Warning Shield or Success Star)
            GameObject iconObj = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconObj.transform.SetParent(dialog.transform, false);
            var iRect = iconObj.GetComponent<RectTransform>();
            iRect.anchorMin = new Vector2(0.5f, 1f);
            iRect.anchorMax = new Vector2(0.5f, 1f);
            iRect.pivot = new Vector2(0.5f, 1f);
            iRect.sizeDelta = new Vector2(72, 72);
            iRect.anchoredPosition = new Vector2(0, -32);
            var icImg = iconObj.GetComponent<Image>();
            var warnSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtDir}/Icon_Popup_Warning.png");
            var succSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtDir}/Icon_Popup_Success.png");
            icImg.sprite = warnSprite;
            icImg.preserveAspect = true;

            // 4. Heading Text (36pt Bold)
            var headTmp = CreateTMPText(dialog.transform, "HeadingText", "TEAM NOT SELECTED!", 36, FontStyles.Bold, new Color(1.0f, 0.85f, 0.30f), 0.05f, 0.73f, 0.95f, 0.84f);
            headTmp.alignment = TextAlignmentOptions.Center;
            headTmp.characterSpacing = 3f;

            // 5. Message Text (25pt Bold White)
            var msgTmp = CreateTMPText(dialog.transform, "MessageText", "Please select your team first.", 25, FontStyles.Bold, Color.white, 0.05f, 0.62f, 0.95f, 0.72f);
            msgTmp.alignment = TextAlignmentOptions.Center;

            // 6. Subtitle Text (20pt Soft Blue)
            var subTmp = CreateTMPText(dialog.transform, "SubtitleText", "Choose 3 characters before continuing.", 20, FontStyles.Normal, new Color(0.75f, 0.85f, 0.98f, 0.95f), 0.05f, 0.52f, 0.95f, 0.61f);
            subTmp.alignment = TextAlignmentOptions.Center;

            // 7. Success Heroes Display Container (State 3)
            GameObject heroesCont = new GameObject("HeroesContainer", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            heroesCont.transform.SetParent(dialog.transform, false);
            var hRect = heroesCont.GetComponent<RectTransform>();
            hRect.anchorMin = new Vector2(0.5f, 0.5f);
            hRect.anchorMax = new Vector2(0.5f, 0.5f);
            hRect.pivot = new Vector2(0.5f, 0.5f);
            hRect.sizeDelta = new Vector2(640, 200);
            hRect.anchoredPosition = new Vector2(0, -25);

            var hHlg = heroesCont.GetComponent<HorizontalLayoutGroup>();
            hHlg.padding = new RectOffset(10, 10, 5, 5);
            hHlg.spacing = 30;
            hHlg.childAlignment = TextAnchor.MiddleCenter;
            hHlg.childControlWidth = false;
            hHlg.childControlHeight = false;

            Image[] portraitImgs = new Image[3];
            TextMeshProUGUI[] nameTexts = new TextMeshProUGUI[3];
            for (int i = 0; i < 3; i++)
            {
                CreatePopupHeroSlot(heroesCont.transform, i, out portraitImgs[i], out nameTexts[i]);
            }
            heroesCont.SetActive(false);

            // 8. Countdown Timer Text (State 3 only)
            var countTmp = CreateTMPText(dialog.transform, "CountdownText", "Returning to Main Menu in 5s...", 22, FontStyles.Bold, new Color(1.0f, 0.85f, 0.30f), 0.05f, 0.08f, 0.95f, 0.17f);
            countTmp.alignment = TextAlignmentOptions.Center;
            countTmp.gameObject.SetActive(false);

            // 9. OK Button (State 0, 1, 2)
            GameObject okObj = new GameObject("OKButton", typeof(RectTransform), typeof(Image), typeof(Button), typeof(UIButtonAnimator));
            okObj.transform.SetParent(dialog.transform, false);
            var okRect = okObj.GetComponent<RectTransform>();
            okRect.anchorMin = new Vector2(0.5f, 0f);
            okRect.anchorMax = new Vector2(0.5f, 0f);
            okRect.pivot = new Vector2(0.5f, 0f);
            okRect.sizeDelta = new Vector2(220, 70);
            okRect.anchoredPosition = new Vector2(0, 36);

            var okImg = okObj.GetComponent<Image>();
            okImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtDir}/UI_Button_Popup_OK.png");
            okImg.type = Image.Type.Simple;

            var okBtn = okObj.GetComponent<Button>();
            var okTmp = CreateTMPText(okObj.transform, "Text", "OK", 26, FontStyles.Bold, Color.white, 0, 0, 1, 1);
            okTmp.alignment = TextAlignmentOptions.Center;

            // Wire ConfirmationPopupUI Serialized Properties
            var popupUI = root.GetComponent<ConfirmationPopupUI>();
            var pSO = new SerializedObject(popupUI);
            pSO.FindProperty("popupPanel").objectReferenceValue = dialog;
            pSO.FindProperty("darkOverlay").objectReferenceValue = overlay;
            pSO.FindProperty("iconImage").objectReferenceValue = icImg;
            pSO.FindProperty("headingText").objectReferenceValue = headTmp;
            pSO.FindProperty("messageText").objectReferenceValue = msgTmp;
            pSO.FindProperty("subtitleText").objectReferenceValue = subTmp;
            pSO.FindProperty("okButton").objectReferenceValue = okBtn;
            pSO.FindProperty("countdownText").objectReferenceValue = countTmp;
            pSO.FindProperty("heroesContainer").objectReferenceValue = heroesCont;
            pSO.FindProperty("warningIcon").objectReferenceValue = warnSprite;
            pSO.FindProperty("successIcon").objectReferenceValue = succSprite;

            var pPortsProp = pSO.FindProperty("heroPortraitImages");
            pPortsProp.arraySize = 3;
            for (int i = 0; i < 3; i++) pPortsProp.GetArrayElementAtIndex(i).objectReferenceValue = portraitImgs[i];

            var pNamesProp = pSO.FindProperty("heroNameTexts");
            pNamesProp.arraySize = 3;
            for (int i = 0; i < 3; i++) pNamesProp.GetArrayElementAtIndex(i).objectReferenceValue = nameTexts[i];

            pSO.ApplyModifiedProperties();

            root.SetActive(false);
            return popupUI;
        }

        private static void CreatePopupHeroSlot(Transform parent, int index, out Image portraitImg, out TextMeshProUGUI nameTmp)
        {
            GameObject slotObj = new GameObject($"PopupSlot_{index + 1}", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            slotObj.transform.SetParent(parent, false);

            var le = slotObj.GetComponent<LayoutElement>();
            le.preferredWidth = 155;
            le.preferredHeight = 185;

            var rect = slotObj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(155, 185);

            var img = slotObj.GetComponent<Image>();
            img.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtDir}/UI_Slot_Frame.png");
            img.type = Image.Type.Simple;

            // Portrait
            GameObject pObj = new GameObject("Portrait", typeof(RectTransform), typeof(Image));
            pObj.transform.SetParent(slotObj.transform, false);
            var pRect = pObj.GetComponent<RectTransform>();
            pRect.anchorMin = new Vector2(0.08f, 0.22f);
            pRect.anchorMax = new Vector2(0.92f, 0.94f);
            pRect.sizeDelta = Vector2.zero;
            portraitImg = pObj.GetComponent<Image>();
            portraitImg.preserveAspect = true;

            // Name
            nameTmp = CreateTMPText(slotObj.transform, "NameText", "Hero", 18, FontStyles.Bold, new Color(1.0f, 0.88f, 0.35f), 0.05f, 0.02f, 0.95f, 0.22f);
            nameTmp.alignment = TextAlignmentOptions.Center;
            nameTmp.enableAutoSizing = true;
            nameTmp.fontSizeMin = 13;
            nameTmp.fontSizeMax = 20;
        }
        #endregion

        #region Helpers
        private static TextMeshProUGUI CreateTMPText(Transform parent, string name, string text, float size, FontStyles style, Color color, float aMinX, float aMinY, float aMaxX, float aMaxY)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            obj.transform.SetParent(parent, false);

            var rect = obj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(aMinX, aMinY);
            rect.anchorMax = new Vector2(aMaxX, aMaxY);
            rect.sizeDelta = Vector2.zero;

            var tmp = obj.GetComponent<TextMeshProUGUI>();
            tmp.font = GetFont();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.fontStyle = style;
            tmp.color = color;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.raycastTarget = false;

            return tmp;
        }

        private static void SaveMainMenuPrefab(GameObject panel)
        {
            EnsureDirectory(PrefabDir);
            string prefabPath = $"{PrefabDir}/MainMenuPanel.prefab";
            PrefabUtility.SaveAsPrefabAssetAndConnect(panel, prefabPath, InteractionMode.AutomatedAction);
        }

        private static void AddSceneToBuildSettings(string scenePath)
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            for (int i = 0; i < scenes.Count; i++)
            {
                if (scenes[i].path == scenePath) return;
            }
            scenes.Add(new EditorBuildSettingsScene(scenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private static void EnsureDirectory(string path)
        {
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
        }
        #endregion
    }
}
#endif
