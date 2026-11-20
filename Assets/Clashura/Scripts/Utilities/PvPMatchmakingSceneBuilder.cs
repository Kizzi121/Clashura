#if UNITY_EDITOR
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using Clashura.Multiplayer;
using Clashura.Characters.Managers;
using Clashura.UI.Responsive;
using Clashura.UI.Animation;

namespace Clashura.Utilities
{
    /// <summary>
    /// Builder utility for the PvP_Matchmaking scene.
    /// Constructs a clean, professional, dark fantasy 1v1 matchmaking screen matching CLASHURA's UI standards.
    /// Supports a 2-step flow:
    /// Step 1: Mode Select (CREATE ROOM vs SEARCH ROOM)
    /// Step 2: Dedicated panels for hosting or searching with input fields and back navigation.
    /// </summary>
    public static class PvPMatchmakingSceneBuilder
    {
        public const string MatchmakingScenePath = "Assets/Clashura/Scenes/Multiplayer/PvP_Matchmaking.unity";
        public const string MainMenuScenePath = "Assets/Clashura/Scenes/MainMenu/MainMenu.unity";
        private const string ArtDir = "Assets/Clashura/Art/UI/MainMenu";
        private const string MultiplayerArtDir = "Assets/Clashura/Art/UI/Multiplayer";
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

        [MenuItem("Clashura/Multiplayer/Build PvP Matchmaking Scene")]
        public static void BuildMatchmakingScene()
        {
            Debug.Log("[PvPMatchmakingSceneBuilder] Building PvP_Matchmaking Scene...");

            MultiplayerArtGenerator.GenerateAllMultiplayerArt();

            string sceneDir = Path.GetDirectoryName(MatchmakingScenePath);
            if (!Directory.Exists(sceneDir))
            {
                Directory.CreateDirectory(sceneDir);
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            Setup2DCamera();
            SetupEventSystem();
            SetupManagers();
            SetupMatchmakingCanvas();

            EditorSceneManager.SaveScene(scene, MatchmakingScenePath);
            Debug.Log($"<color=green>[PvPMatchmakingSceneBuilder] Successfully saved scene: {MatchmakingScenePath}</color>");

            EnsureBuildSettings();
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

        private static void SetupEventSystem()
        {
            GameObject esObj = new GameObject("EventSystem");
            esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
            esObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        private static void SetupManagers()
        {
            GameObject mgrObj = new GameObject("[MatchmakingManagers]");
            mgrObj.AddComponent<TeamSelectionManager>();
            mgrObj.AddComponent<PhotonFusionManager>();
            mgrObj.AddComponent<MatchmakingManager>();
        }

        private static void SetupMatchmakingCanvas()
        {
            // Canvas Root
            GameObject canvasObj = new GameObject("MatchmakingCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasObj.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasObj.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            var uiCtrl = canvasObj.AddComponent<MatchmakingUI>();

            // 1. Background Image
            GameObject bgObj = new GameObject("Background", typeof(RectTransform), typeof(Image));
            bgObj.transform.SetParent(canvasObj.transform, false);
            var bgRect = bgObj.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;
            var bgImg = bgObj.GetComponent<Image>();
            bgImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtDir}/CharSelect_Background.png");
            bgImg.color = new Color(0.35f, 0.40f, 0.55f, 1f);

            // 2. Safe Area Container
            GameObject safeObj = new GameObject("SafeArea", typeof(RectTransform), typeof(SafeAreaHandler));
            safeObj.transform.SetParent(canvasObj.transform, false);
            var safeRect = safeObj.GetComponent<RectTransform>();
            safeRect.anchorMin = Vector2.zero;
            safeRect.anchorMax = Vector2.one;
            safeRect.sizeDelta = Vector2.zero;

            // 3. Back to Main Menu Button (Top-Left)
            GameObject backObj = new GameObject("BackButton", typeof(RectTransform), typeof(Image), typeof(Button), typeof(UIButtonAnimator));
            backObj.transform.SetParent(safeRect, false);
            var bkRect = backObj.GetComponent<RectTransform>();
            bkRect.anchorMin = new Vector2(0f, 1f);
            bkRect.anchorMax = new Vector2(0f, 1f);
            bkRect.pivot = new Vector2(0f, 1f);
            bkRect.anchoredPosition = new Vector2(40, -35);
            bkRect.sizeDelta = new Vector2(240, 68);

            var bkImg = backObj.GetComponent<Image>();
            bkImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtDir}/UI_Button_Normal.png");
            bkImg.type = Image.Type.Sliced;
            var bkBtn = backObj.GetComponent<Button>();

            var bkTxt = CreateTMPText(backObj.transform, "BtnText", "< BACK TO MENU", 22, FontStyles.Bold, new Color(1.0f, 0.85f, 0.30f), 0, 0, 1, 1);
            bkTxt.alignment = TextAlignmentOptions.Center;

            // 4. Title Section (Top-Center)
            GameObject titleObj = new GameObject("TitleSection", typeof(RectTransform));
            titleObj.transform.SetParent(safeRect, false);
            var tRect = titleObj.GetComponent<RectTransform>();
            tRect.anchorMin = new Vector2(0.5f, 0.93f);
            tRect.anchorMax = new Vector2(0.5f, 0.93f);
            tRect.pivot = new Vector2(0.5f, 0.5f);
            tRect.sizeDelta = new Vector2(900, 100);
            tRect.anchoredPosition = Vector2.zero;

            var titleTmp = CreateTMPText(titleObj.transform, "Title", "PLAYER VS PLAYER", 48, FontStyles.Bold, new Color(1.0f, 0.85f, 0.30f), 0, 0.40f, 1, 1);
            titleTmp.alignment = TextAlignmentOptions.Center;
            titleTmp.characterSpacing = 3f;

            var subTmp = CreateTMPText(titleObj.transform, "Subtitle", "1V1 ONLINE MATCHMAKING · CHOOSE MODE", 20, FontStyles.Normal, new Color(0.72f, 0.85f, 1.0f, 0.95f), 0, 0, 1, 0.40f);
            subTmp.alignment = TextAlignmentOptions.Center;
            subTmp.characterSpacing = 2f;

            // 5. Main Card Container (Center)
            GameObject cardObj = new GameObject("MatchmakingCard", typeof(RectTransform), typeof(Image));
            cardObj.transform.SetParent(safeRect, false);
            var cRect = cardObj.GetComponent<RectTransform>();
            cRect.anchorMin = new Vector2(0.5f, 0.44f);
            cRect.anchorMax = new Vector2(0.5f, 0.44f);
            cRect.pivot = new Vector2(0.5f, 0.5f);
            cRect.sizeDelta = new Vector2(1080, 620);
            cRect.anchoredPosition = Vector2.zero;

            var cImg = cardObj.GetComponent<Image>();
            cImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{MultiplayerArtDir}/UI_Multiplayer_Lobby_Frame.png");
            if (cImg.sprite == null) cImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtDir}/UI_Popup_Frame.png");
            cImg.type = Image.Type.Sliced;
            cImg.color = Color.white;

            // =========================================================================
            // 5.1 PANEL A: Mode Selection Panel (Initial screen with 2 choice buttons)
            // =========================================================================
            GameObject modeSelectObj = new GameObject("Panel_ModeSelect", typeof(RectTransform));
            modeSelectObj.transform.SetParent(cardObj.transform, false);
            var msRect = modeSelectObj.GetComponent<RectTransform>();
            msRect.anchorMin = Vector2.zero;
            msRect.anchorMax = Vector2.one;
            msRect.sizeDelta = Vector2.zero;

            // Choice 1: CREATE ROOM CARD
            var (cardChooseCreate, btnChooseCreate) = CreateModeChoiceCard(
                modeSelectObj.transform,
                "Card_ChooseCreate",
                -265,
                "CREATE ROOM",
                "Host a custom 1v1 battle arena and wait for your rival to join.",
                $"{MultiplayerArtDir}/Icon_Create_Room.png",
                "CREATE ROOM",
                new Color(1.0f, 0.85f, 0.30f),
                $"{MultiplayerArtDir}/UI_Button_Gold_Action.png");

            // Choice 2: SEARCH ROOM CARD
            var (cardChooseSearch, btnChooseSearch) = CreateModeChoiceCard(
                modeSelectObj.transform,
                "Card_ChooseSearch",
                265,
                "JOIN ROOM",
                "Search for a custom room created by your rival and enter the duel.",
                $"{MultiplayerArtDir}/Icon_Join_Search.png",
                "SEARCH & JOIN",
                new Color(0.40f, 0.85f, 1.0f),
                $"{MultiplayerArtDir}/UI_Button_Cyan_Action.png");

            // =========================================================================
            // 5.2 PANEL B: Create Room Panel (Dedicated panel when player chooses Create)
            // =========================================================================
            GameObject createRoomObj = new GameObject("Panel_CreateRoom", typeof(RectTransform));
            createRoomObj.transform.SetParent(cardObj.transform, false);
            var crRect = createRoomObj.GetComponent<RectTransform>();
            crRect.anchorMin = Vector2.zero;
            crRect.anchorMax = Vector2.one;
            crRect.sizeDelta = Vector2.zero;

            var (btnHostRoom, btnBackFromCreate, inputCreate) = CreateActionSubPanel(
                createRoomObj.transform,
                "SubCard_Create",
                "HOST CUSTOM ROOM",
                "ENTER A ROOM NAME FOR YOUR BATTLE ARENA:",
                "e.g. ShadowDuel...",
                "CLASH_ARENA",
                $"{MultiplayerArtDir}/Icon_Create_Room.png",
                "HOST ROOM",
                new Color(1.0f, 0.85f, 0.30f),
                $"{MultiplayerArtDir}/UI_Button_Gold_Action.png");

            createRoomObj.SetActive(false);

            // =========================================================================
            // 5.3 PANEL C: Search Room Panel (Dedicated panel when player chooses Search)
            // =========================================================================
            GameObject searchRoomObj = new GameObject("Panel_SearchRoom", typeof(RectTransform));
            searchRoomObj.transform.SetParent(cardObj.transform, false);
            var srRect = searchRoomObj.GetComponent<RectTransform>();
            srRect.anchorMin = Vector2.zero;
            srRect.anchorMax = Vector2.one;
            srRect.sizeDelta = Vector2.zero;

            var (btnJoinRoom, btnBackFromSearch, inputSearch) = CreateActionSubPanel(
                searchRoomObj.transform,
                "SubCard_Search",
                "SEARCH & JOIN ROOM",
                "ENTER TARGET ROOM NAME TO DUEL:",
                "e.g. ShadowDuel...",
                "",
                $"{MultiplayerArtDir}/Icon_Join_Search.png",
                "JOIN ROOM",
                new Color(0.40f, 0.85f, 1.0f),
                $"{MultiplayerArtDir}/UI_Button_Cyan_Action.png");

            searchRoomObj.SetActive(false);

            // =========================================================================
            // 5.4 SHARED BOTTOM CONTROLS (Room Code Text, Status Box, Spinner, Cancel)
            // =========================================================================

            // Room Code Display (Shown once room is created)
            var roomCodeTmp = CreateTMPText(cardObj.transform, "RoomCodeText", "ROOM NAME: CLASH_ARENA", 22, FontStyles.Bold, new Color(1.0f, 0.85f, 0.30f), 0.1f, 0.22f, 0.9f, 0.28f);
            roomCodeTmp.alignment = TextAlignmentOptions.Center;
            roomCodeTmp.gameObject.SetActive(false);

            // Status Banner Box (Bottom section of the card)
            GameObject statusBox = new GameObject("StatusBox", typeof(RectTransform), typeof(Image));
            statusBox.transform.SetParent(cardObj.transform, false);
            var sRect = statusBox.GetComponent<RectTransform>();
            sRect.anchorMin = new Vector2(0.5f, 0.10f);
            sRect.anchorMax = new Vector2(0.5f, 0.10f);
            sRect.pivot = new Vector2(0.5f, 0.5f);
            sRect.sizeDelta = new Vector2(780, 52);

            var sImg = statusBox.GetComponent<Image>();
            sImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{MultiplayerArtDir}/UI_Room_Input_Box.png");
            if (sImg.sprite == null) sImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtDir}/UI_Button_Normal.png");
            sImg.type = Image.Type.Sliced;
            sImg.color = new Color(0.08f, 0.12f, 0.20f, 0.95f);

            var statusTmp = CreateTMPText(statusBox.transform, "StatusText", "Choose an option to enter 1v1 battle matchmaking.", 22, FontStyles.Bold, new Color(0.85f, 0.90f, 0.98f), 0.05f, 0, 0.95f, 1);
            statusTmp.alignment = TextAlignmentOptions.Center;

            // Loading Spinner Indicator
            GameObject loadingObj = new GameObject("LoadingIndicator", typeof(RectTransform));
            loadingObj.transform.SetParent(cardObj.transform, false);
            var lRect = loadingObj.GetComponent<RectTransform>();
            lRect.anchorMin = new Vector2(0.5f, 0.165f);
            lRect.anchorMax = new Vector2(0.5f, 0.165f);
            lRect.pivot = new Vector2(0.5f, 0.5f);
            lRect.sizeDelta = new Vector2(40, 40);

            GameObject spinIcon = new GameObject("SpinnerIcon", typeof(RectTransform), typeof(Image));
            spinIcon.transform.SetParent(loadingObj.transform, false);
            var spRect = spinIcon.GetComponent<RectTransform>();
            spRect.anchorMin = Vector2.zero;
            spRect.anchorMax = Vector2.one;
            spRect.sizeDelta = Vector2.zero;
            var spImg = spinIcon.GetComponent<Image>();
            spImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{MultiplayerArtDir}/UI_Versus_Badge.png");
            if (spImg.sprite == null) spImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtDir}/CharSelect_Dais_Base.png");
            spImg.color = new Color(1.0f, 0.85f, 0.30f, 0.95f);
            loadingObj.SetActive(false);

            // Cancel Matchmaking Button
            GameObject cancelObj = new GameObject("CancelButton", typeof(RectTransform), typeof(Image), typeof(Button), typeof(UIButtonAnimator));
            cancelObj.transform.SetParent(cardObj.transform, false);
            var cnRect = cancelObj.GetComponent<RectTransform>();
            cnRect.anchorMin = new Vector2(0.5f, 0.02f);
            cnRect.anchorMax = new Vector2(0.5f, 0.02f);
            cnRect.pivot = new Vector2(0.5f, 0.5f);
            cnRect.sizeDelta = new Vector2(260, 44);

            var cnImg = cancelObj.GetComponent<Image>();
            cnImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtDir}/UI_Button_Normal.png");
            cnImg.type = Image.Type.Sliced;
            cnImg.color = new Color(0.85f, 0.25f, 0.20f, 0.90f);
            var cnBtn = cancelObj.GetComponent<Button>();

            var cnTxt = CreateTMPText(cancelObj.transform, "BtnText", "CANCEL MATCHMAKING", 18, FontStyles.Bold, Color.white, 0, 0, 1, 1);
            cnTxt.alignment = TextAlignmentOptions.Center;
            cancelObj.SetActive(false);

            // =========================================================================
            // 5.5 VERSUS (VS) MATCHUP SCREEN (Step 3: Shown when match ready / starting)
            // =========================================================================
            var vsUI = SetupVersusMatchupPanel(safeRect.transform);

            // =========================================================================
            // 6. Connect Serialized Properties on MatchmakingUI
            // =========================================================================
            var uiSO = new SerializedObject(uiCtrl);
            uiSO.FindProperty("titleText").objectReferenceValue = titleTmp;
            uiSO.FindProperty("subtitleText").objectReferenceValue = subTmp;
            uiSO.FindProperty("roomNameText").objectReferenceValue = roomCodeTmp;

            uiSO.FindProperty("modeSelectPanel").objectReferenceValue = modeSelectObj;
            uiSO.FindProperty("createRoomPanel").objectReferenceValue = createRoomObj;
            uiSO.FindProperty("searchRoomPanel").objectReferenceValue = searchRoomObj;

            uiSO.FindProperty("chooseCreateButton").objectReferenceValue = btnChooseCreate;
            uiSO.FindProperty("chooseSearchButton").objectReferenceValue = btnChooseSearch;

            uiSO.FindProperty("createRoomInputField").objectReferenceValue = inputCreate;
            uiSO.FindProperty("hostRoomActionButton").objectReferenceValue = btnHostRoom;
            uiSO.FindProperty("backFromCreateButton").objectReferenceValue = btnBackFromCreate;

            uiSO.FindProperty("searchRoomInputField").objectReferenceValue = inputSearch;
            uiSO.FindProperty("joinRoomActionButton").objectReferenceValue = btnJoinRoom;
            uiSO.FindProperty("backFromSearchButton").objectReferenceValue = btnBackFromSearch;

            uiSO.FindProperty("cancelButton").objectReferenceValue = cnBtn;
            uiSO.FindProperty("backToMenuButton").objectReferenceValue = bkBtn;

            uiSO.FindProperty("statusText").objectReferenceValue = statusTmp;
            uiSO.FindProperty("loadingIndicator").objectReferenceValue = loadingObj;
            uiSO.FindProperty("loadingSpinnerIcon").objectReferenceValue = spImg;

            uiSO.FindProperty("versusMatchupUI").objectReferenceValue = vsUI;
            uiSO.FindProperty("matchmakingCard").objectReferenceValue = cardObj;

            uiSO.FindProperty("mainMenuSceneName").stringValue = "MainMenu";
            uiSO.ApplyModifiedProperties();
            EditorUtility.SetDirty(uiCtrl);
        }

        #region Component Helper Builders

        private static (GameObject card, Button choiceBtn) CreateModeChoiceCard(
            Transform parent,
            string name,
            float xPos,
            string title,
            string description,
            string iconPath,
            string buttonLabel,
            Color accentColor,
            string buttonSpritePath)
        {
            GameObject cardObj = new GameObject(name, typeof(RectTransform), typeof(Image));
            cardObj.transform.SetParent(parent, false);
            var rect = cardObj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.61f);
            rect.anchorMax = new Vector2(0.5f, 0.61f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(490, 400);
            rect.anchoredPosition = new Vector2(xPos, 0);

            var img = cardObj.GetComponent<Image>();
            img.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{MultiplayerArtDir}/UI_Room_Action_Card.png");
            if (img.sprite == null) img.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtDir}/UI_Popup_Frame.png");
            img.type = Image.Type.Sliced;

            // Large Icon
            GameObject iconObj = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconObj.transform.SetParent(cardObj.transform, false);
            var icRect = iconObj.GetComponent<RectTransform>();
            icRect.anchorMin = new Vector2(0.5f, 0.76f);
            icRect.anchorMax = new Vector2(0.5f, 0.76f);
            icRect.pivot = new Vector2(0.5f, 0.5f);
            icRect.sizeDelta = new Vector2(88, 88);
            var icImg = iconObj.GetComponent<Image>();
            icImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(iconPath);
            if (icImg.sprite == null) icImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{MultiplayerArtDir}/UI_Versus_Badge.png");
            icImg.preserveAspect = true;

            // Title
            var titleTmp = CreateTMPText(cardObj.transform, "Title", title, 30, FontStyles.Bold, accentColor, 0.05f, 0.52f, 0.95f, 0.63f);
            titleTmp.alignment = TextAlignmentOptions.Center;
            titleTmp.characterSpacing = 2f;

            // Description
            var descTmp = CreateTMPText(cardObj.transform, "Description", description, 17, FontStyles.Normal, new Color(0.80f, 0.88f, 0.98f, 0.90f), 0.08f, 0.35f, 0.92f, 0.50f);
            descTmp.alignment = TextAlignmentOptions.Center;
            descTmp.textWrappingMode = TextWrappingModes.Normal;

            // Action Choice Button
            GameObject btnObj = new GameObject("ChoiceButton", typeof(RectTransform), typeof(Image), typeof(Button), typeof(UIButtonAnimator));
            btnObj.transform.SetParent(cardObj.transform, false);
            var bRect = btnObj.GetComponent<RectTransform>();
            bRect.anchorMin = new Vector2(0.5f, 0.18f);
            bRect.anchorMax = new Vector2(0.5f, 0.18f);
            bRect.pivot = new Vector2(0.5f, 0.5f);
            bRect.sizeDelta = new Vector2(360, 64);

            var bImg = btnObj.GetComponent<Image>();
            bImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(buttonSpritePath);
            if (bImg.sprite == null) bImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtDir}/UI_Button_Normal.png");
            bImg.type = Image.Type.Sliced;
            var btn = btnObj.GetComponent<Button>();

            var btnTxt = CreateTMPText(btnObj.transform, "BtnText", buttonLabel, 22, FontStyles.Bold, Color.white, 0, 0, 1, 1);
            btnTxt.alignment = TextAlignmentOptions.Center;
            btnTxt.characterSpacing = 1.5f;

            return (cardObj, btn);
        }

        private static (Button actionBtn, Button backBtn, TMP_InputField inputField) CreateActionSubPanel(
            Transform parent,
            string cardName,
            string title,
            string inputLabel,
            string placeholder,
            string defaultText,
            string iconPath,
            string actionButtonLabel,
            Color accentColor,
            string buttonSpritePath)
        {
            GameObject cardObj = new GameObject(cardName, typeof(RectTransform), typeof(Image));
            cardObj.transform.SetParent(parent, false);
            var rect = cardObj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.61f);
            rect.anchorMax = new Vector2(0.5f, 0.61f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(620, 420);
            rect.anchoredPosition = Vector2.zero;

            var img = cardObj.GetComponent<Image>();
            img.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{MultiplayerArtDir}/UI_Room_Action_Card.png");
            if (img.sprite == null) img.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtDir}/UI_Popup_Frame.png");
            img.type = Image.Type.Sliced;

            // Icon
            GameObject iconObj = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconObj.transform.SetParent(cardObj.transform, false);
            var icRect = iconObj.GetComponent<RectTransform>();
            icRect.anchorMin = new Vector2(0.5f, 0.85f);
            icRect.anchorMax = new Vector2(0.5f, 0.85f);
            icRect.pivot = new Vector2(0.5f, 0.5f);
            icRect.sizeDelta = new Vector2(64, 64);
            var icImg = iconObj.GetComponent<Image>();
            icImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(iconPath);
            if (icImg.sprite == null) icImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{MultiplayerArtDir}/UI_Versus_Badge.png");
            icImg.preserveAspect = true;

            // Title
            var titleTmp = CreateTMPText(cardObj.transform, "Title", title, 28, FontStyles.Bold, accentColor, 0.05f, 0.70f, 0.95f, 0.80f);
            titleTmp.alignment = TextAlignmentOptions.Center;
            titleTmp.characterSpacing = 2f;

            // Label
            var lblTmp = CreateTMPText(cardObj.transform, "InputLabel", inputLabel, 16, FontStyles.Bold, new Color(0.80f, 0.88f, 0.98f), 0.05f, 0.57f, 0.95f, 0.66f);
            lblTmp.alignment = TextAlignmentOptions.Center;

            // Input Field Container
            GameObject inputObj = new GameObject("RoomInputField", typeof(RectTransform), typeof(Image), typeof(TMP_InputField));
            inputObj.transform.SetParent(cardObj.transform, false);
            var inRect = inputObj.GetComponent<RectTransform>();
            inRect.anchorMin = new Vector2(0.5f, 0.44f);
            inRect.anchorMax = new Vector2(0.5f, 0.44f);
            inRect.pivot = new Vector2(0.5f, 0.5f);
            inRect.sizeDelta = new Vector2(460, 52);

            var inImg = inputObj.GetComponent<Image>();
            inImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{MultiplayerArtDir}/UI_Room_Input_Box.png");
            if (inImg.sprite == null) inImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtDir}/UI_Button_Normal.png");
            inImg.type = Image.Type.Sliced;
            inImg.color = new Color(0.08f, 0.12f, 0.20f, 0.95f);

            // Text Viewport
            GameObject textArea = new GameObject("TextArea", typeof(RectTransform), typeof(RectMask2D));
            textArea.transform.SetParent(inputObj.transform, false);
            var taRect = textArea.GetComponent<RectTransform>();
            taRect.anchorMin = Vector2.zero;
            taRect.anchorMax = Vector2.one;
            taRect.offsetMin = new Vector2(16, 4);
            taRect.offsetMax = new Vector2(-16, -4);

            // Placeholder
            GameObject phObj = new GameObject("Placeholder", typeof(RectTransform), typeof(TextMeshProUGUI));
            phObj.transform.SetParent(textArea.transform, false);
            var phRect = phObj.GetComponent<RectTransform>();
            phRect.anchorMin = Vector2.zero;
            phRect.anchorMax = Vector2.one;
            phRect.offsetMin = Vector2.zero;
            phRect.offsetMax = Vector2.zero;
            var phTmp = phObj.GetComponent<TextMeshProUGUI>();
            phTmp.font = GetFont();
            phTmp.fontSize = 19;
            phTmp.fontStyle = FontStyles.Italic;
            phTmp.color = new Color(0.55f, 0.65f, 0.78f, 0.70f);
            phTmp.alignment = TextAlignmentOptions.Left;
            phTmp.text = placeholder;

            // Input Text
            GameObject txtObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            txtObj.transform.SetParent(textArea.transform, false);
            var tRect = txtObj.GetComponent<RectTransform>();
            tRect.anchorMin = Vector2.zero;
            tRect.anchorMax = Vector2.one;
            tRect.offsetMin = Vector2.zero;
            tRect.offsetMax = Vector2.zero;
            var tTmp = txtObj.GetComponent<TextMeshProUGUI>();
            tTmp.font = GetFont();
            tTmp.fontSize = 20;
            tTmp.fontStyle = FontStyles.Bold;
            tTmp.color = Color.white;
            tTmp.alignment = TextAlignmentOptions.Left;
            tTmp.text = defaultText;

            var inputField = inputObj.GetComponent<TMP_InputField>();
            inputField.textViewport = taRect;
            inputField.textComponent = tTmp;
            inputField.placeholder = phTmp;
            inputField.fontAsset = GetFont();
            inputField.pointSize = 20;
            inputField.characterLimit = 24;
            inputField.text = defaultText;

            // Primary Action Button
            GameObject actObj = new GameObject("ActionButton", typeof(RectTransform), typeof(Image), typeof(Button), typeof(UIButtonAnimator));
            actObj.transform.SetParent(cardObj.transform, false);
            var actRect = actObj.GetComponent<RectTransform>();
            actRect.anchorMin = new Vector2(0.5f, 0.24f);
            actRect.anchorMax = new Vector2(0.5f, 0.24f);
            actRect.pivot = new Vector2(0.5f, 0.5f);
            actRect.sizeDelta = new Vector2(380, 58);

            var actImg = actObj.GetComponent<Image>();
            actImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(buttonSpritePath);
            if (actImg.sprite == null) actImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtDir}/UI_Button_Normal.png");
            actImg.type = Image.Type.Sliced;
            var actionBtn = actObj.GetComponent<Button>();

            var actTxt = CreateTMPText(actObj.transform, "BtnText", actionButtonLabel, 22, FontStyles.Bold, Color.white, 0, 0, 1, 1);
            actTxt.alignment = TextAlignmentOptions.Center;
            actTxt.characterSpacing = 1.5f;

            // Secondary / Back Button to Mode Selection
            GameObject backObj = new GameObject("BackButton", typeof(RectTransform), typeof(Image), typeof(Button), typeof(UIButtonAnimator));
            backObj.transform.SetParent(cardObj.transform, false);
            var bkRect = backObj.GetComponent<RectTransform>();
            bkRect.anchorMin = new Vector2(0.5f, 0.08f);
            bkRect.anchorMax = new Vector2(0.5f, 0.08f);
            bkRect.pivot = new Vector2(0.5f, 0.5f);
            bkRect.sizeDelta = new Vector2(260, 42);

            var bkImg = backObj.GetComponent<Image>();
            bkImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtDir}/UI_Button_Normal.png");
            bkImg.type = Image.Type.Sliced;
            bkImg.color = new Color(0.20f, 0.25f, 0.35f, 0.90f);
            var backBtn = backObj.GetComponent<Button>();

            var bkTxt = CreateTMPText(backObj.transform, "BtnText", "< BACK TO MODES", 16, FontStyles.Bold, new Color(0.85f, 0.90f, 0.98f), 0, 0, 1, 1);
            bkTxt.alignment = TextAlignmentOptions.Center;

            return (actionBtn, backBtn, inputField);
        }

        private static TextMeshProUGUI CreateTMPText(Transform parent, string name, string text, float fontSize, FontStyles style, Color color, float minX, float minY, float maxX, float maxY)
        {
            GameObject txtObj = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            txtObj.transform.SetParent(parent, false);
            var rect = txtObj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(minX, minY);
            rect.anchorMax = new Vector2(maxX, maxY);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var tmp = txtObj.GetComponent<TextMeshProUGUI>();
            tmp.font = GetFont();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.fontStyle = style;
            tmp.color = color;
            tmp.raycastTarget = false;
            return tmp;
        }

        private static VersusMatchupUI SetupVersusMatchupPanel(Transform parent)
        {
            GameObject vsRoot = new GameObject("VersusMatchupPanel", typeof(RectTransform), typeof(CanvasGroup));
            vsRoot.transform.SetParent(parent, false);
            var vsRect = vsRoot.GetComponent<RectTransform>();
            vsRect.anchorMin = Vector2.zero;
            vsRect.anchorMax = Vector2.one;
            vsRect.sizeDelta = Vector2.zero;

            var vsCG = vsRoot.GetComponent<CanvasGroup>();
            var vsUI = vsRoot.AddComponent<VersusMatchupUI>();

            // Dark semi-transparent background overlay
            GameObject overlayObj = new GameObject("Overlay", typeof(RectTransform), typeof(Image));
            overlayObj.transform.SetParent(vsRoot.transform, false);
            var ovRect = overlayObj.GetComponent<RectTransform>();
            ovRect.anchorMin = Vector2.zero;
            ovRect.anchorMax = Vector2.one;
            ovRect.sizeDelta = Vector2.zero;
            var ovImg = overlayObj.GetComponent<Image>();
            ovImg.color = new Color(0.02f, 0.03f, 0.06f, 0.98f);

            // Left Background Aura (Cyan/Sapphire)
            GameObject leftAura = new GameObject("Aura_Left", typeof(RectTransform), typeof(Image));
            leftAura.transform.SetParent(vsRoot.transform, false);
            var laRect = leftAura.GetComponent<RectTransform>();
            laRect.anchorMin = new Vector2(0f, 0f);
            laRect.anchorMax = new Vector2(0.5f, 1f);
            laRect.sizeDelta = Vector2.zero;
            var laImg = leftAura.GetComponent<Image>();
            laImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{MultiplayerArtDir}/UI_Versus_Glow_Burst.png");
            laImg.color = new Color(0.08f, 0.25f, 0.45f, 0.22f);
            laImg.raycastTarget = false;

            // Right Background Aura (Crimson/Ruby)
            GameObject rightAura = new GameObject("Aura_Right", typeof(RectTransform), typeof(Image));
            rightAura.transform.SetParent(vsRoot.transform, false);
            var raRect = rightAura.GetComponent<RectTransform>();
            raRect.anchorMin = new Vector2(0.5f, 0f);
            raRect.anchorMax = new Vector2(1f, 1f);
            raRect.sizeDelta = Vector2.zero;
            var raImg = rightAura.GetComponent<Image>();
            raImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{MultiplayerArtDir}/UI_Versus_Glow_Burst.png");
            raImg.color = new Color(0.50f, 0.08f, 0.15f, 0.22f);
            raImg.raycastTarget = false;

            // 1. Center VS Container
            GameObject centerObj = new GameObject("VS_Center_Container", typeof(RectTransform));
            centerObj.transform.SetParent(vsRoot.transform, false);
            var cntRect = centerObj.GetComponent<RectTransform>();
            cntRect.anchorMin = new Vector2(0.5f, 0.5f);
            cntRect.anchorMax = new Vector2(0.5f, 0.5f);
            cntRect.pivot = new Vector2(0.5f, 0.5f);
            cntRect.sizeDelta = new Vector2(280, 1080);
            cntRect.anchoredPosition = Vector2.zero;

            // Vertical Center Divider Beam
            GameObject beamObj = new GameObject("DividerBeam", typeof(RectTransform), typeof(Image));
            beamObj.transform.SetParent(centerObj.transform, false);
            var beamRect = beamObj.GetComponent<RectTransform>();
            beamRect.anchorMin = new Vector2(0.5f, 0f);
            beamRect.anchorMax = new Vector2(0.5f, 1f);
            beamRect.pivot = new Vector2(0.5f, 0.5f);
            beamRect.sizeDelta = new Vector2(28, 0);
            var beamImg = beamObj.GetComponent<Image>();
            beamImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{MultiplayerArtDir}/UI_Versus_Divider_Beam.png");
            beamImg.type = Image.Type.Sliced;
            beamImg.color = new Color(1f, 1f, 1f, 0.75f);
            beamImg.raycastTarget = false;

            // Match Title (Top of Center Column)
            var matchTitle = CreateTMPText(centerObj.transform, "MatchTitle", "1V1 RANKED CLASH", 26, FontStyles.Bold, new Color(1.0f, 0.88f, 0.40f), 0, 0.91f, 1, 0.98f);
            matchTitle.alignment = TextAlignmentOptions.Center;
            matchTitle.characterSpacing = 2.5f;

            // Rotating Sunburst Energy Rays
            GameObject raysObj = new GameObject("EnergyRays", typeof(RectTransform), typeof(Image));
            raysObj.transform.SetParent(centerObj.transform, false);
            var rayRect = raysObj.GetComponent<RectTransform>();
            rayRect.anchorMin = new Vector2(0.5f, 0.53f);
            rayRect.anchorMax = new Vector2(0.5f, 0.53f);
            rayRect.pivot = new Vector2(0.5f, 0.5f);
            rayRect.sizeDelta = new Vector2(440, 440);
            var rayImg = raysObj.GetComponent<Image>();
            rayImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{MultiplayerArtDir}/UI_Versus_Energy_Rays.png");
            rayImg.color = new Color(1f, 1f, 1f, 0.70f);
            rayImg.raycastTarget = false;

            // Radial Glow Burst
            GameObject glowObj = new GameObject("GlowBurst", typeof(RectTransform), typeof(Image));
            glowObj.transform.SetParent(centerObj.transform, false);
            var glRect = glowObj.GetComponent<RectTransform>();
            glRect.anchorMin = new Vector2(0.5f, 0.53f);
            glRect.anchorMax = new Vector2(0.5f, 0.53f);
            glRect.pivot = new Vector2(0.5f, 0.5f);
            glRect.sizeDelta = new Vector2(340, 340);
            var glImg = glowObj.GetComponent<Image>();
            glImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{MultiplayerArtDir}/UI_Versus_Glow_Burst.png");
            glImg.color = new Color(1f, 1f, 1f, 0.85f);
            glImg.raycastTarget = false;

            // VS Badge Supreme
            GameObject vsBadgeObj = new GameObject("VS_Badge", typeof(RectTransform), typeof(Image));
            vsBadgeObj.transform.SetParent(centerObj.transform, false);
            var vbRect = vsBadgeObj.GetComponent<RectTransform>();
            vbRect.anchorMin = new Vector2(0.5f, 0.53f);
            vbRect.anchorMax = new Vector2(0.5f, 0.53f);
            vbRect.pivot = new Vector2(0.5f, 0.5f);
            vbRect.sizeDelta = new Vector2(220, 220);
            var vbImg = vsBadgeObj.GetComponent<Image>();
            vbImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{MultiplayerArtDir}/UI_Versus_Badge_Supreme.png");
            if (vbImg.sprite == null) vbImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{MultiplayerArtDir}/UI_Versus_Badge_Large.png");
            vbImg.color = Color.white;

            // Center "VS" bold text on the badge
            var vsLetters = CreateTMPText(vsBadgeObj.transform, "VS_Text", "VS", 68, FontStyles.Bold, new Color(1.0f, 0.94f, 0.55f), 0, 0, 1, 1);
            vsLetters.alignment = TextAlignmentOptions.Center;
            vsLetters.characterSpacing = 2.5f;

            // Arena Name
            var arenaTmp = CreateTMPText(centerObj.transform, "ArenaName", "ARENA: TITANDUEL", 20, FontStyles.Bold, new Color(0.75f, 0.90f, 1.0f), 0, 0.35f, 1, 0.42f);
            arenaTmp.alignment = TextAlignmentOptions.Center;
            arenaTmp.characterSpacing = 2f;

            // Countdown / Fight text
            var cdTmp = CreateTMPText(centerObj.transform, "CountdownText", "BATTLE STARTS IN 3...", 32, FontStyles.Bold, new Color(1.0f, 0.85f, 0.30f), 0, 0.16f, 1, 0.28f);
            cdTmp.alignment = TextAlignmentOptions.Center;
            cdTmp.characterSpacing = 2.5f;

            // 2. Player 1 Card (Left - Cyan/Gold, Full Screen Height)
            var (p1CardRect, p1CG, p1Tag, p1Name, p1Lvl, p1Pts, p1Tier, p1Portraits, p1Names, p1Roles, p1SlotCGs) = 
                CreatePlayerSideCard(vsRoot.transform, "Card_Player1", -485f, true);

            // 3. Player 2 Card (Right - Ruby/Crimson, Full Screen Height)
            var (p2CardRect, p2CG, p2Tag, p2Name, p2Lvl, p2Pts, p2Tier, p2Portraits, p2Names, p2Roles, p2SlotCGs) = 
                CreatePlayerSideCard(vsRoot.transform, "Card_Player2", 485f, false);

            // 4. Full-Screen Flash Overlay
            GameObject flashObj = new GameObject("ScreenFlash", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
            flashObj.transform.SetParent(vsRoot.transform, false);
            var flRect = flashObj.GetComponent<RectTransform>();
            flRect.anchorMin = Vector2.zero;
            flRect.anchorMax = Vector2.one;
            flRect.sizeDelta = Vector2.zero;
            var flImg = flashObj.GetComponent<Image>();
            flImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{MultiplayerArtDir}/UI_Versus_Screen_Flash.png");
            flImg.color = Color.white;
            flImg.raycastTarget = false;
            var flCG = flashObj.GetComponent<CanvasGroup>();
            flCG.alpha = 0f;
            flashObj.SetActive(false);

            // Wire Serialized Properties to VersusMatchupUI
            var vsSO = new SerializedObject(vsUI);
            vsSO.FindProperty("versusPanelRoot").objectReferenceValue = vsRoot;
            vsSO.FindProperty("rootCanvasGroup").objectReferenceValue = vsCG;

            vsSO.FindProperty("characterDatabase").objectReferenceValue = 
                AssetDatabase.LoadAssetAtPath<CharacterDatabaseSO>("Assets/Clashura/Data/Characters/ScriptableObjects/CharacterDatabase.asset");

            vsSO.FindProperty("vsCenterContainer").objectReferenceValue = cntRect;
            vsSO.FindProperty("vsBadgeRect").objectReferenceValue = vbRect;
            vsSO.FindProperty("vsGlowBurstImage").objectReferenceValue = glImg;
            vsSO.FindProperty("energyRaysRect").objectReferenceValue = rayRect;
            vsSO.FindProperty("dividerBeamRect").objectReferenceValue = beamRect;
            vsSO.FindProperty("vsMatchTitleText").objectReferenceValue = matchTitle;
            vsSO.FindProperty("vsArenaNameText").objectReferenceValue = arenaTmp;
            vsSO.FindProperty("vsCountdownText").objectReferenceValue = cdTmp;
            vsSO.FindProperty("screenFlashGroup").objectReferenceValue = flCG;

            // Player 1
            vsSO.FindProperty("p1CardRect").objectReferenceValue = p1CardRect;
            vsSO.FindProperty("p1CanvasGroup").objectReferenceValue = p1CG;
            vsSO.FindProperty("p1TagText").objectReferenceValue = p1Tag;
            vsSO.FindProperty("p1NameText").objectReferenceValue = p1Name;
            vsSO.FindProperty("p1LevelText").objectReferenceValue = p1Lvl;
            vsSO.FindProperty("p1PointsText").objectReferenceValue = p1Pts;
            vsSO.FindProperty("p1TierText").objectReferenceValue = p1Tier;

            for (int i = 0; i < 3; i++)
            {
                vsSO.FindProperty("p1HeroPortraits").GetArrayElementAtIndex(i).objectReferenceValue = p1Portraits[i];
                vsSO.FindProperty("p1HeroNames").GetArrayElementAtIndex(i).objectReferenceValue = p1Names[i];
                vsSO.FindProperty("p1HeroRoles").GetArrayElementAtIndex(i).objectReferenceValue = p1Roles[i];
                vsSO.FindProperty("p1HeroSlotGroups").GetArrayElementAtIndex(i).objectReferenceValue = p1SlotCGs[i];
            }

            // Player 2
            vsSO.FindProperty("p2CardRect").objectReferenceValue = p2CardRect;
            vsSO.FindProperty("p2CanvasGroup").objectReferenceValue = p2CG;
            vsSO.FindProperty("p2TagText").objectReferenceValue = p2Tag;
            vsSO.FindProperty("p2NameText").objectReferenceValue = p2Name;
            vsSO.FindProperty("p2LevelText").objectReferenceValue = p2Lvl;
            vsSO.FindProperty("p2PointsText").objectReferenceValue = p2Pts;
            vsSO.FindProperty("p2TierText").objectReferenceValue = p2Tier;

            for (int i = 0; i < 3; i++)
            {
                vsSO.FindProperty("p2HeroPortraits").GetArrayElementAtIndex(i).objectReferenceValue = p2Portraits[i];
                vsSO.FindProperty("p2HeroNames").GetArrayElementAtIndex(i).objectReferenceValue = p2Names[i];
                vsSO.FindProperty("p2HeroRoles").GetArrayElementAtIndex(i).objectReferenceValue = p2Roles[i];
                vsSO.FindProperty("p2HeroSlotGroups").GetArrayElementAtIndex(i).objectReferenceValue = p2SlotCGs[i];
            }

            vsSO.ApplyModifiedProperties();
            EditorUtility.SetDirty(vsUI);

            vsRoot.SetActive(false); // Initially hidden, shown during MatchStarting or Preview
            return vsUI;
        }

        private static (
            RectTransform cardRect,
            CanvasGroup cg,
            TMP_Text tagText,
            TMP_Text nameText,
            TMP_Text levelText,
            TMP_Text pointsText,
            TMP_Text tierText,
            Image[] portraits,
            TMP_Text[] names,
            TMP_Text[] roles,
            CanvasGroup[] slotCGs
        ) CreatePlayerSideCard(Transform parent, string name, float xPos, bool isP1)
        {
            GameObject cardObj = new GameObject(name, typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
            cardObj.transform.SetParent(parent, false);
            var cardRect = cardObj.GetComponent<RectTransform>();
            cardRect.anchorMin = new Vector2(0.5f, 0.47f);
            cardRect.anchorMax = new Vector2(0.5f, 0.47f);
            cardRect.pivot = new Vector2(0.5f, 0.5f);
            cardRect.sizeDelta = new Vector2(720, 850);
            cardRect.anchoredPosition = new Vector2(xPos, -20f);

            var cg = cardObj.GetComponent<CanvasGroup>();
            var cardImg = cardObj.GetComponent<Image>();
            string cardSprite = isP1 ? $"{MultiplayerArtDir}/UI_Versus_Card_P1.png" : $"{MultiplayerArtDir}/UI_Versus_Card_P2.png";
            cardImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(cardSprite);
            if (cardImg.sprite == null) cardImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{MultiplayerArtDir}/UI_Room_Action_Card.png");
            cardImg.type = Image.Type.Sliced;
            cardImg.color = Color.white;

            Color accentCol = isP1 ? new Color(0.35f, 0.85f, 1.0f) : new Color(1.0f, 0.35f, 0.45f);
            Color tagCol = isP1 ? new Color(1.0f, 0.85f, 0.30f) : new Color(1.0f, 0.45f, 0.55f);

            // 1. Tag Banner
            string defaultTag = isP1 ? "HOST · ARENA CHAMPION" : "CHALLENGER · RIVAL";
            var tagTmp = CreateTMPText(cardObj.transform, "PlayerTag", defaultTag, 18, FontStyles.Bold, tagCol, 0.05f, 0.92f, 0.95f, 0.97f);
            tagTmp.alignment = isP1 ? TextAlignmentOptions.Left : TextAlignmentOptions.Right;
            tagTmp.characterSpacing = 2.5f;

            // 2. Player Name (Big, Prominent)
            string defaultName = isP1 ? "PLAYER 1" : "SHADOW RIVAL";
            var nameTmp = CreateTMPText(cardObj.transform, "PlayerName", defaultName, 42, FontStyles.Bold, Color.white, 0.05f, 0.83f, 0.95f, 0.92f);
            nameTmp.alignment = isP1 ? TextAlignmentOptions.Left : TextAlignmentOptions.Right;
            nameTmp.characterSpacing = 2.0f;

            // 3. Stats Row (Level, Points & Tier Badges)
            GameObject statsRow = new GameObject("StatsRow", typeof(RectTransform));
            statsRow.transform.SetParent(cardObj.transform, false);
            var srRect = statsRow.GetComponent<RectTransform>();
            srRect.anchorMin = new Vector2(0.05f, 0.74f);
            srRect.anchorMax = new Vector2(0.95f, 0.82f);
            srRect.sizeDelta = Vector2.zero;

            // Level Pill
            GameObject lvlPill = new GameObject("Pill_Level", typeof(RectTransform), typeof(Image));
            lvlPill.transform.SetParent(statsRow.transform, false);
            var lpRect = lvlPill.GetComponent<RectTransform>();
            lpRect.anchorMin = new Vector2(0f, 0.5f);
            lpRect.anchorMax = new Vector2(0f, 0.5f);
            lpRect.pivot = new Vector2(0f, 0.5f);
            lpRect.sizeDelta = new Vector2(160, 48);
            lpRect.anchoredPosition = new Vector2(0, 0);

            var lpImg = lvlPill.GetComponent<Image>();
            lpImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{MultiplayerArtDir}/UI_Versus_Pill.png");
            if (lpImg.sprite == null) lpImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{MultiplayerArtDir}/UI_Room_Input_Box.png");
            lpImg.type = Image.Type.Sliced;
            lpImg.color = isP1 ? new Color(0.22f, 0.18f, 0.08f, 0.95f) : new Color(0.28f, 0.08f, 0.12f, 0.95f);

            var lvlTmp = CreateTMPText(lvlPill.transform, "LevelText", isP1 ? "LVL 25" : "LVL 30", 20, FontStyles.Bold, new Color(1.0f, 0.88f, 0.35f), 0, 0, 1, 1);
            lvlTmp.alignment = TextAlignmentOptions.Center;

            // Points Pill
            GameObject ptsPill = new GameObject("Pill_Points", typeof(RectTransform), typeof(Image));
            ptsPill.transform.SetParent(statsRow.transform, false);
            var ppRect = ptsPill.GetComponent<RectTransform>();
            ppRect.anchorMin = new Vector2(0f, 0.5f);
            ppRect.anchorMax = new Vector2(0f, 0.5f);
            ppRect.pivot = new Vector2(0f, 0.5f);
            ppRect.sizeDelta = new Vector2(210, 48);
            ppRect.anchoredPosition = new Vector2(175, 0);

            var ppImg = ptsPill.GetComponent<Image>();
            ppImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{MultiplayerArtDir}/UI_Versus_Pill.png");
            if (ppImg.sprite == null) ppImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{MultiplayerArtDir}/UI_Room_Input_Box.png");
            ppImg.type = Image.Type.Sliced;
            ppImg.color = isP1 ? new Color(0.08f, 0.22f, 0.35f, 0.95f) : new Color(0.28f, 0.08f, 0.12f, 0.95f);

            var ptsTmp = CreateTMPText(ptsPill.transform, "PointsText", isP1 ? "1,450 PTS" : "1,560 PTS", 20, FontStyles.Bold, accentCol, 0, 0, 1, 1);
            ptsTmp.alignment = TextAlignmentOptions.Center;

            // Tier Pill
            GameObject tierPill = new GameObject("Pill_Tier", typeof(RectTransform), typeof(Image));
            tierPill.transform.SetParent(statsRow.transform, false);
            var tpRect = tierPill.GetComponent<RectTransform>();
            tpRect.anchorMin = new Vector2(0f, 0.5f);
            tpRect.anchorMax = new Vector2(0f, 0.5f);
            tpRect.pivot = new Vector2(0f, 0.5f);
            tpRect.sizeDelta = new Vector2(200, 48);
            tpRect.anchoredPosition = new Vector2(400, 0);

            var tpImg = tierPill.GetComponent<Image>();
            tpImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{MultiplayerArtDir}/UI_Versus_Pill.png");
            tpImg.type = Image.Type.Sliced;
            tpImg.color = new Color(0.12f, 0.14f, 0.22f, 0.90f);

            var tierTmp = CreateTMPText(tierPill.transform, "TierText", isP1 ? "GOLD TIER III" : "PLATINUM TIER I", 17, FontStyles.Bold, new Color(0.90f, 0.92f, 1.0f), 0, 0, 1, 1);
            tierTmp.alignment = TextAlignmentOptions.Center;

            // 4. Team Section Header
            var teamHdr = CreateTMPText(cardObj.transform, "TeamHeader", "CHOSEN CHAMPIONS (TEAM OF 3)", 17, FontStyles.Bold, accentCol, 0.05f, 0.67f, 0.95f, 0.73f);
            teamHdr.alignment = isP1 ? TextAlignmentOptions.Left : TextAlignmentOptions.Right;
            teamHdr.characterSpacing = 2.5f;

            // 5. Team Hero Slots Container (3 large hero slots)
            GameObject teamCont = new GameObject("TeamContainer", typeof(RectTransform));
            teamCont.transform.SetParent(cardObj.transform, false);
            var tcRect = teamCont.GetComponent<RectTransform>();
            tcRect.anchorMin = new Vector2(0.5f, 0.33f);
            tcRect.anchorMax = new Vector2(0.5f, 0.33f);
            tcRect.pivot = new Vector2(0.5f, 0.5f);
            tcRect.sizeDelta = new Vector2(670, 520);
            tcRect.anchoredPosition = Vector2.zero;

            Image[] portraits = new Image[3];
            TMP_Text[] names = new TMP_Text[3];
            TMP_Text[] roles = new TMP_Text[3];
            CanvasGroup[] slotCGs = new CanvasGroup[3];

            float[] slotXOffsets = new float[] { -224f, 0f, 224f };

            for (int i = 0; i < 3; i++)
            {
                GameObject slotObj = new GameObject($"Slot_{i}", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
                slotObj.transform.SetParent(teamCont.transform, false);
                var sRect = slotObj.GetComponent<RectTransform>();
                sRect.anchorMin = new Vector2(0.5f, 0.5f);
                sRect.anchorMax = new Vector2(0.5f, 0.5f);
                sRect.pivot = new Vector2(0.5f, 0.5f);
                sRect.sizeDelta = new Vector2(212, 500);
                sRect.anchoredPosition = new Vector2(slotXOffsets[i], 0);

                slotCGs[i] = slotObj.GetComponent<CanvasGroup>();

                var sImg = slotObj.GetComponent<Image>();
                sImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{MultiplayerArtDir}/UI_Versus_Hero_Slot_Large.png");
                if (sImg.sprite == null) sImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{MultiplayerArtDir}/UI_Versus_Hero_Slot.png");
                sImg.type = Image.Type.Sliced;
                sImg.color = Color.white;

                // Inner Portrait Container (Large Character Portrait)
                GameObject portObj = new GameObject("Portrait", typeof(RectTransform), typeof(Image));
                portObj.transform.SetParent(slotObj.transform, false);
                var pRect = portObj.GetComponent<RectTransform>();
                pRect.anchorMin = new Vector2(0.5f, 0.58f);
                pRect.anchorMax = new Vector2(0.5f, 0.58f);
                pRect.pivot = new Vector2(0.5f, 0.5f);
                pRect.sizeDelta = new Vector2(196, 300);
                pRect.anchoredPosition = Vector2.zero;

                portraits[i] = portObj.GetComponent<Image>();
                portraits[i].preserveAspect = true;

                // Hero Name label
                names[i] = CreateTMPText(slotObj.transform, "HeroName", $"HERO {i + 1}", 18, FontStyles.Bold, new Color(1.0f, 0.92f, 0.60f), 0.05f, 0.16f, 0.95f, 0.24f);
                names[i].alignment = TextAlignmentOptions.Center;

                // Role badge backing pill
                GameObject rolePillObj = new GameObject("RolePill", typeof(RectTransform), typeof(Image));
                rolePillObj.transform.SetParent(slotObj.transform, false);
                var rpRect = rolePillObj.GetComponent<RectTransform>();
                rpRect.anchorMin = new Vector2(0.5f, 0.09f);
                rpRect.anchorMax = new Vector2(0.5f, 0.09f);
                rpRect.pivot = new Vector2(0.5f, 0.5f);
                rpRect.sizeDelta = new Vector2(170, 32);

                var rpImg = rolePillObj.GetComponent<Image>();
                rpImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{MultiplayerArtDir}/UI_Versus_Pill.png");
                rpImg.type = Image.Type.Sliced;
                rpImg.color = new Color(0.06f, 0.08f, 0.14f, 0.88f);

                // Role badge label
                roles[i] = CreateTMPText(rolePillObj.transform, "HeroRole", "VANGUARD", 14, FontStyles.Bold, accentCol, 0, 0, 1, 1);
                roles[i].alignment = TextAlignmentOptions.Center;
            }

            return (cardRect, cg, tagTmp, nameTmp, lvlTmp, ptsTmp, tierTmp, portraits, names, roles, slotCGs);
        }

        private static void EnsureBuildSettings()
        {
            var scenes = new List<EditorBuildSettingsScene>
            {
                new EditorBuildSettingsScene("Assets/Clashura/Scenes/MainMenu/MainMenu.unity", true),
                new EditorBuildSettingsScene("Assets/Clashura/Scenes/CharacterSelection/CharacterSelection.unity", true),
                new EditorBuildSettingsScene(MatchmakingScenePath, true),
                new EditorBuildSettingsScene("Assets/Scenes/SampleScene.unity", true)
            };
            EditorBuildSettings.scenes = scenes.ToArray();
            AssetDatabase.SaveAssets();
        }

        #endregion
    }
}
#endif
