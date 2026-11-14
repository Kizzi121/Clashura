#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using Clashura.Characters.Managers;
using Clashura.Characters.Preview;
using Clashura.CharacterSelection.Managers;
using Clashura.CharacterSelection.UI;
using Clashura.UI.Responsive;

namespace Clashura.Utilities
{
    /// <summary>
    /// Builds the complete responsive 2D Character Selection Scene with prominently sized
    /// ability cards, large text, full descriptions, and high-readability layouts.
    /// Strictly 2D game architecture.
    /// </summary>
    public static class ClashuraSceneBuilder
    {
        private const string ScenePath = "Assets/Clashura/Scenes/CharacterSelection/CharacterSelection.unity";
        private const string FontPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";
        private static TMP_FontAsset _cachedFont;

        private static TMP_FontAsset GetDefaultFont()
        {
            if (_cachedFont == null)
            {
                _cachedFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            }
            return _cachedFont;
        }

        [MenuItem("Clashura/Build 2D Character Selection Scene")]
        public static void Build2DScene()
        {
            Debug.Log("[ClashuraSceneBuilder] Building High-Readability 2D Character Selection Scene with Enlarged Abilities...");

            // 1. Create new empty scene
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 2. Setup 2D Camera
            Setup2DCamera();

            // 3. Setup Managers
            var selectionManager = SetupManagers();

            // 4. Setup UI & EventSystem
            Setup2DUI(selectionManager);

            // 5. Save Scene
            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log($"[ClashuraSceneBuilder] High-Readability 2D Scene saved to {ScenePath}!");
        }

        private static void Setup2DCamera()
        {
            GameObject camObj = new GameObject("Main Camera");
            var cam = camObj.AddComponent<Camera>();
            camObj.AddComponent<AudioListener>();
            cam.orthographic = true;
            cam.orthographicSize = 5f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.06f, 0.07f, 0.10f, 1f);
            cam.transform.position = new Vector3(0f, 0f, -10f);
            camObj.tag = "MainCamera";
        }

        private static CharacterSelectionManager SetupManagers()
        {
            GameObject mgrObj = new GameObject("[SelectionManager]");
            var manager = mgrObj.AddComponent<CharacterSelectionManager>();

            string dbPath = "Assets/Clashura/Data/Characters/ScriptableObjects/CharacterDatabase.asset";
            var db = AssetDatabase.LoadAssetAtPath<CharacterDatabaseSO>(dbPath);

            var so = new SerializedObject(manager);
            so.FindProperty("characterDatabase").objectReferenceValue = db;
            so.FindProperty("localPlayerId").stringValue = "Player_Local";
            so.FindProperty("autoLoadOnStart").boolValue = true;
            so.ApplyModifiedProperties();

            return manager;
        }

        private static void Setup2DUI(CharacterSelectionManager selectionManager)
        {
            // 1. EventSystem
            GameObject esObj = new GameObject("EventSystem");
            esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
            esObj.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();

            // 2. Canvas
            GameObject canvasObj = new GameObject("SelectionCanvas");
            var canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<GraphicRaycaster>();

            var scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            canvasObj.AddComponent<ResponsiveLayoutController>();

            // 3. SafeArea Container
            GameObject safeAreaObj = CreateUIObject("SafeArea", canvasObj.transform);
            var safeAreaRect = safeAreaObj.GetComponent<RectTransform>();
            safeAreaRect.anchorMin = Vector2.zero;
            safeAreaRect.anchorMax = Vector2.one;
            safeAreaRect.offsetMin = Vector2.zero;
            safeAreaRect.offsetMax = Vector2.zero;
            safeAreaObj.AddComponent<SafeAreaHandler>();

            // 4. UI Manager component on Canvas
            var ui = canvasObj.AddComponent<CharacterSelectionUI>();
            var uiSO = new SerializedObject(ui);
            uiSO.FindProperty("selectionManager").objectReferenceValue = selectionManager;

            // 5. Build 2D UI Hierarchy inside SafeArea
            Build2DUIHierarchy(safeAreaObj.transform, uiSO);

            uiSO.ApplyModifiedProperties();
        }

        private static void Build2DUIHierarchy(Transform parent, SerializedObject uiSO)
        {
            // --- TOP HEADER PANEL ---
            GameObject header = CreateUIObject("HeaderPanel", parent);
            RectTransform headerRT = header.GetComponent<RectTransform>();
            headerRT.anchorMin = new Vector2(0.08f, 0.84f);
            headerRT.anchorMax = new Vector2(0.92f, 0.985f);
            headerRT.offsetMin = Vector2.zero;
            headerRT.offsetMax = Vector2.zero;

            // Character Name Text (Large, Prominent, Golden)
            GameObject nameObj = CreateTextObject("CharacterNameText", header.transform, 56, FontStyles.Bold, TextAlignmentOptions.Center, new Color(1f, 0.88f, 0.35f, 1f), true, 36f, 68f);
            RectTransform nameRT = nameObj.GetComponent<RectTransform>();
            nameRT.anchorMin = new Vector2(0f, 0.44f);
            nameRT.anchorMax = new Vector2(1f, 1f);
            nameRT.offsetMin = Vector2.zero;
            nameRT.offsetMax = Vector2.zero;
            var nameTmp = nameObj.GetComponent<TextMeshProUGUI>();
            nameTmp.text = "AETHELGARD";
            uiSO.FindProperty("characterNameText").objectReferenceValue = nameTmp;

            // Title Text (Clear Silver White)
            GameObject titleObj = CreateTextObject("CharacterTitleText", header.transform, 28, FontStyles.Italic, TextAlignmentOptions.Center, new Color(0.92f, 0.94f, 0.98f, 1f), true, 20f, 32f);
            RectTransform titleRT = titleObj.GetComponent<RectTransform>();
            titleRT.anchorMin = new Vector2(0f, 0.15f);
            titleRT.anchorMax = new Vector2(0.5f, 0.44f);
            titleRT.offsetMin = Vector2.zero;
            titleRT.offsetMax = Vector2.zero;
            var titleTmp = titleObj.GetComponent<TextMeshProUGUI>();
            titleTmp.text = "The Shield of Dawn";
            uiSO.FindProperty("characterTitleText").objectReferenceValue = titleTmp;

            // Role Badge Text (Vibrant Cyan)
            GameObject roleObj = CreateTextObject("CharacterRoleText", header.transform, 28, FontStyles.Bold, TextAlignmentOptions.Center, new Color(0.0f, 0.9f, 1f, 1f), true, 20f, 32f);
            RectTransform roleRT = roleObj.GetComponent<RectTransform>();
            roleRT.anchorMin = new Vector2(0.5f, 0.15f);
            roleRT.anchorMax = new Vector2(1f, 0.44f);
            roleRT.offsetMin = Vector2.zero;
            roleRT.offsetMax = Vector2.zero;
            var roleTmp = roleObj.GetComponent<TextMeshProUGUI>();
            roleTmp.text = "VANGUARD";
            uiSO.FindProperty("characterRoleText").objectReferenceValue = roleTmp;

            // Counter Text (1 / 18)
            GameObject countObj = CreateTextObject("CounterText", header.transform, 24, FontStyles.Bold, TextAlignmentOptions.Center, new Color(0.75f, 0.8f, 0.9f, 1f), true, 18f, 28f);
            RectTransform countRT = countObj.GetComponent<RectTransform>();
            countRT.anchorMin = new Vector2(0.38f, 0f);
            countRT.anchorMax = new Vector2(0.62f, 0.20f);
            countRT.offsetMin = Vector2.zero;
            countRT.offsetMax = Vector2.zero;
            var countTmp = countObj.GetComponent<TextMeshProUGUI>();
            countTmp.text = "1 / 18";
            uiSO.FindProperty("characterCounterText").objectReferenceValue = countTmp;

            // --- CENTER 2D SELECTION ARTWORK CONTAINER ---
            GameObject artworkContainer = CreateUIObject("ArtworkViewport", parent);
            RectTransform artRT = artworkContainer.GetComponent<RectTransform>();
            artRT.anchorMin = new Vector2(0.31f, 0.33f);
            artRT.anchorMax = new Vector2(0.69f, 0.83f);
            artRT.offsetMin = Vector2.zero;
            artRT.offsetMax = Vector2.zero;

            var artBg = artworkContainer.AddComponent<Image>();
            artBg.color = new Color(0.04f, 0.05f, 0.08f, 0.4f);

            // Primary Image
            GameObject primaryObj = CreateUIObject("PrimaryArtwork", artworkContainer.transform);
            RectTransform pRT = primaryObj.GetComponent<RectTransform>();
            pRT.anchorMin = Vector2.zero;
            pRT.anchorMax = Vector2.one;
            pRT.offsetMin = Vector2.zero;
            pRT.offsetMax = Vector2.zero;
            var pImg = primaryObj.AddComponent<Image>();
            pImg.preserveAspect = true;

            // Secondary Image (for smooth cross-fade)
            GameObject secondaryObj = CreateUIObject("SecondaryArtwork", artworkContainer.transform);
            RectTransform sRT = secondaryObj.GetComponent<RectTransform>();
            sRT.anchorMin = Vector2.zero;
            sRT.anchorMax = Vector2.one;
            sRT.offsetMin = Vector2.zero;
            sRT.offsetMax = Vector2.zero;
            var sImg = secondaryObj.AddComponent<Image>();
            sImg.preserveAspect = true;

            var visualCtrl = artworkContainer.AddComponent<CharacterVisual2DController>();
            var vcSO = new SerializedObject(visualCtrl);
            vcSO.FindProperty("primaryImage").objectReferenceValue = pImg;
            vcSO.FindProperty("secondaryImage").objectReferenceValue = sImg;
            vcSO.FindProperty("transitionDuration").floatValue = 0.25f;
            vcSO.ApplyModifiedProperties();

            uiSO.FindProperty("visual2DController").objectReferenceValue = visualCtrl;

            // --- NAVIGATION BUTTONS (Left / Right) with Large High-Contrast Arrows ---
            GameObject prevBtnObj = CreateButton("PrevButton", parent, "<", new Vector2(0.22f, 0.50f), new Vector2(0.29f, 0.64f), 48, Color.white, new Color(0.16f, 0.22f, 0.32f, 0.95f));
            uiSO.FindProperty("prevButton").objectReferenceValue = prevBtnObj.GetComponent<Button>();

            GameObject nextBtnObj = CreateButton("NextButton", parent, ">", new Vector2(0.71f, 0.50f), new Vector2(0.78f, 0.64f), 48, Color.white, new Color(0.16f, 0.22f, 0.32f, 0.95f));
            uiSO.FindProperty("nextButton").objectReferenceValue = nextBtnObj.GetComponent<Button>();

            // --- TOGGLE SELECTION BUTTON ---
            GameObject toggleBtn = CreateButton("ToggleSelectButton", parent, "SELECT FOR TEAM", new Vector2(0.35f, 0.27f), new Vector2(0.65f, 0.325f), 26, Color.white, new Color(0.15f, 0.68f, 0.28f, 1f));
            uiSO.FindProperty("toggleSelectButton").objectReferenceValue = toggleBtn.GetComponent<Button>();
            uiSO.FindProperty("toggleSelectButtonText").objectReferenceValue = toggleBtn.GetComponentInChildren<TextMeshProUGUI>();
            uiSO.FindProperty("toggleSelectButtonImage").objectReferenceValue = toggleBtn.GetComponent<Image>();

            // --- COMBAT STATS PANEL (Left side, High Contrast, Color-Coded) ---
            GameObject statsPanel = CreateUIObject("StatsPanel", parent);
            RectTransform statsRT = statsPanel.GetComponent<RectTransform>();
            statsRT.anchorMin = new Vector2(0.03f, 0.28f);
            statsRT.anchorMax = new Vector2(0.21f, 0.70f);
            statsRT.offsetMin = Vector2.zero;
            statsRT.offsetMax = Vector2.zero;
            var statsBg = statsPanel.AddComponent<Image>();
            statsBg.color = new Color(0.06f, 0.08f, 0.12f, 0.92f);

            var statsLayout = statsPanel.AddComponent<VerticalLayoutGroup>();
            statsLayout.padding = new RectOffset(18, 18, 16, 16);
            statsLayout.spacing = 10;
            statsLayout.childControlHeight = true;
            statsLayout.childControlWidth = true;

            GameObject statsHeader = CreateTextObject("StatsTitle", statsPanel.transform, 24, FontStyles.Bold, TextAlignmentOptions.Center, new Color(1f, 0.85f, 0.35f, 1f), true, 18f, 28f);
            statsHeader.GetComponent<TextMeshProUGUI>().text = "ATTRIBUTES";

            // HP (Vibrant Green)
            GameObject hpObj = CreateTextObject("HPText", statsPanel.transform, 24, FontStyles.Bold, TextAlignmentOptions.Left, new Color(0.2f, 1f, 0.5f, 1f), true, 18f, 28f);
            var hpTmp = hpObj.GetComponent<TextMeshProUGUI>();
            hpTmp.text = "HP: 1600";
            uiSO.FindProperty("healthText").objectReferenceValue = hpTmp;

            // ATK (Vibrant Orange)
            GameObject atkObj = CreateTextObject("ATKText", statsPanel.transform, 24, FontStyles.Bold, TextAlignmentOptions.Left, new Color(1f, 0.45f, 0.25f, 1f), true, 18f, 28f);
            var atkTmp = atkObj.GetComponent<TextMeshProUGUI>();
            atkTmp.text = "ATK: 95";
            uiSO.FindProperty("attackText").objectReferenceValue = atkTmp;

            // DEF (Vibrant Blue)
            GameObject defObj = CreateTextObject("DEFText", statsPanel.transform, 24, FontStyles.Bold, TextAlignmentOptions.Left, new Color(0.35f, 0.75f, 1f, 1f), true, 18f, 28f);
            var defTmp = defObj.GetComponent<TextMeshProUGUI>();
            defTmp.text = "DEF: 190";
            uiSO.FindProperty("defenseText").objectReferenceValue = defTmp;

            // SPD (Vibrant Yellow)
            GameObject spdObj = CreateTextObject("SPDText", statsPanel.transform, 24, FontStyles.Bold, TextAlignmentOptions.Left, new Color(1f, 0.85f, 0.2f, 1f), true, 18f, 28f);
            var spdTmp = spdObj.GetComponent<TextMeshProUGUI>();
            spdTmp.text = "SPD: 35";
            uiSO.FindProperty("speedText").objectReferenceValue = spdTmp;

            // --- ENLARGED ABILITIES PANEL (Prominent size, 125px tall across width) ---
            GameObject abPanel = CreateUIObject("AbilitiesPanel", parent);
            RectTransform abRT = abPanel.GetComponent<RectTransform>();
            abRT.anchorMin = new Vector2(0.04f, 0.125f);
            abRT.anchorMax = new Vector2(0.96f, 0.255f);
            abRT.offsetMin = Vector2.zero;
            abRT.offsetMax = Vector2.zero;
            var abLayout = abPanel.AddComponent<HorizontalLayoutGroup>();
            abLayout.spacing = 16;
            abLayout.childControlWidth = true;
            abLayout.childControlHeight = true;
            abLayout.childForceExpandWidth = true;
            abLayout.childForceExpandHeight = true;
            uiSO.FindProperty("abilitiesContainer").objectReferenceValue = abPanel.transform;

            // Ability Slot Prefab
            GameObject abSlotTemplate = CreateAbilitySlotTemplate();
            var abSlotComp = abSlotTemplate.GetComponent<AbilitySlotUI>();
            uiSO.FindProperty("abilitySlotPrefab").objectReferenceValue = abSlotComp;

            // --- SELECTED TEAM BAR (3 Slots + Confirm Button) ---
            GameObject teamBar = CreateUIObject("TeamBar", parent);
            RectTransform tbRT = teamBar.GetComponent<RectTransform>();
            tbRT.anchorMin = new Vector2(0.03f, 0.015f);
            tbRT.anchorMax = new Vector2(0.97f, 0.11f);
            tbRT.offsetMin = Vector2.zero;
            tbRT.offsetMax = Vector2.zero;
            var tbBg = teamBar.AddComponent<Image>();
            tbBg.color = new Color(0.05f, 0.06f, 0.09f, 0.95f);

            var tbLayout = teamBar.AddComponent<HorizontalLayoutGroup>();
            tbLayout.padding = new RectOffset(20, 20, 8, 8);
            tbLayout.spacing = 20;
            tbLayout.childControlWidth = true;
            tbLayout.childControlHeight = true;

            // Team Status Text
            GameObject teamStatusObj = CreateTextObject("TeamStatusText", teamBar.transform, 24, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, Color.white, true, 18f, 28f);
            var statusTmp = teamStatusObj.GetComponent<TextMeshProUGUI>();
            statusTmp.text = "Selected: 0 / 3";
            uiSO.FindProperty("teamStatusText").objectReferenceValue = statusTmp;

            // 3 Selected Slots
            var slotProps = uiSO.FindProperty("selectedSlots");
            slotProps.ClearArray();

            for (int i = 0; i < 3; i++)
            {
                GameObject slotObj = CreateSelectedSlot(teamBar.transform, i);
                var slotComp = slotObj.GetComponent<SelectedSlotUI>();
                slotProps.InsertArrayElementAtIndex(i);
                slotProps.GetArrayElementAtIndex(i).objectReferenceValue = slotComp;
            }

            // Confirm Button (Dark Text on Bright Gold Button for Maximum Contrast)
            GameObject confirmBtn = CreateButton("ConfirmTeamButton", teamBar.transform, "CONFIRM TEAM", Vector2.zero, Vector2.one, 24, new Color(0.06f, 0.08f, 0.12f, 1f), new Color(1f, 0.8f, 0.1f, 1f));
            uiSO.FindProperty("confirmTeamButton").objectReferenceValue = confirmBtn.GetComponent<Button>();

            // --- TOAST / FEEDBACK BANNER ---
            GameObject toastObj = CreateUIObject("FeedbackBanner", parent);
            RectTransform toastRT = toastObj.GetComponent<RectTransform>();
            toastRT.anchorMin = new Vector2(0.20f, 0.68f);
            toastRT.anchorMax = new Vector2(0.80f, 0.75f);
            toastRT.offsetMin = Vector2.zero;
            toastRT.offsetMax = Vector2.zero;
            var toastBg = toastObj.AddComponent<Image>();
            toastBg.color = new Color(0.08f, 0.10f, 0.15f, 0.96f);

            GameObject toastTextObj = CreateTextObject("FeedbackText", toastObj.transform, 26, FontStyles.Bold, TextAlignmentOptions.Center, Color.white, true, 18f, 32f);
            RectTransform ttRT = toastTextObj.GetComponent<RectTransform>();
            ttRT.anchorMin = Vector2.zero;
            ttRT.anchorMax = Vector2.one;
            ttRT.offsetMin = Vector2.zero;
            ttRT.offsetMax = Vector2.zero;
            var ttTmp = toastTextObj.GetComponent<TextMeshProUGUI>();
            ttTmp.text = "";
            uiSO.FindProperty("feedbackBanner").objectReferenceValue = toastObj;
            uiSO.FindProperty("feedbackText").objectReferenceValue = ttTmp;
            toastObj.SetActive(false);
        }

        private static GameObject CreateSelectedSlot(Transform parent, int slotIndex)
        {
            GameObject slotObj = CreateUIObject($"SelectedSlot_{slotIndex}", parent);
            var img = slotObj.AddComponent<Image>();
            img.color = new Color(0.11f, 0.13f, 0.18f, 1f);

            var slotUI = slotObj.AddComponent<SelectedSlotUI>();
            var slotSO = new SerializedObject(slotUI);
            slotSO.FindProperty("slotIndex").intValue = slotIndex;

            // Empty State (Prominent readable placeholder)
            GameObject emptyRoot = CreateTextObject("EmptyLabel", slotObj.transform, 20, FontStyles.Bold, TextAlignmentOptions.Center, new Color(0.6f, 0.65f, 0.75f, 1f), true, 14f, 22f);
            var emptyTmp = emptyRoot.GetComponent<TextMeshProUGUI>();
            emptyTmp.text = $"[ Slot {slotIndex + 1} Empty ]";
            RectTransform emptyRT = emptyRoot.GetComponent<RectTransform>();
            emptyRT.anchorMin = Vector2.zero;
            emptyRT.anchorMax = Vector2.one;
            emptyRT.offsetMin = Vector2.zero;
            emptyRT.offsetMax = Vector2.zero;
            slotSO.FindProperty("emptyStateRoot").objectReferenceValue = emptyRoot;

            // Filled State
            GameObject filledRoot = CreateUIObject("FilledRoot", slotObj.transform);
            RectTransform fRT = filledRoot.GetComponent<RectTransform>();
            fRT.anchorMin = Vector2.zero;
            fRT.anchorMax = Vector2.one;
            fRT.offsetMin = Vector2.zero;
            fRT.offsetMax = Vector2.zero;
            slotSO.FindProperty("filledStateRoot").objectReferenceValue = filledRoot;

            // Portrait
            GameObject portObj = CreateUIObject("Portrait", filledRoot.transform);
            RectTransform portRT = portObj.GetComponent<RectTransform>();
            portRT.anchorMin = new Vector2(0.04f, 0.08f);
            portRT.anchorMax = new Vector2(0.34f, 0.92f);
            portRT.offsetMin = Vector2.zero;
            portRT.offsetMax = Vector2.zero;
            var portImg = portObj.AddComponent<Image>();
            portImg.preserveAspect = true;
            slotSO.FindProperty("portraitImage").objectReferenceValue = portImg;

            // Name (Large Bold White)
            GameObject nameObj = CreateTextObject("HeroName", filledRoot.transform, 22, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, Color.white, true, 16f, 26f);
            RectTransform nameRT = nameObj.GetComponent<RectTransform>();
            nameRT.anchorMin = new Vector2(0.36f, 0.50f);
            nameRT.anchorMax = new Vector2(0.84f, 0.94f);
            nameRT.offsetMin = Vector2.zero;
            nameRT.offsetMax = Vector2.zero;
            var nameTmp = nameObj.GetComponent<TextMeshProUGUI>();
            slotSO.FindProperty("nameText").objectReferenceValue = nameTmp;

            // Role (Bright Cyan)
            GameObject roleObj = CreateTextObject("HeroRole", filledRoot.transform, 17, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, new Color(0.0f, 0.88f, 1f, 1f), true, 13f, 20f);
            RectTransform roleRT = roleObj.GetComponent<RectTransform>();
            roleRT.anchorMin = new Vector2(0.36f, 0.08f);
            roleRT.anchorMax = new Vector2(0.84f, 0.50f);
            roleRT.offsetMin = Vector2.zero;
            roleRT.offsetMax = Vector2.zero;
            var roleTmp = roleObj.GetComponent<TextMeshProUGUI>();
            slotSO.FindProperty("roleText").objectReferenceValue = roleTmp;

            // Remove Button ("X" Red Bold)
            GameObject removeBtnObj = CreateButton("RemoveBtn", filledRoot.transform, "X", new Vector2(0.85f, 0.25f), new Vector2(0.98f, 0.75f), 24, Color.white, new Color(0.85f, 0.2f, 0.2f, 1f));
            slotSO.FindProperty("removeButton").objectReferenceValue = removeBtnObj.GetComponent<Button>();

            // Main slot click button
            var slotBtn = slotObj.AddComponent<Button>();
            slotSO.FindProperty("slotButton").objectReferenceValue = slotBtn;

            filledRoot.SetActive(false);
            slotSO.ApplyModifiedProperties();

            return slotObj;
        }

        private static GameObject CreateAbilitySlotTemplate()
        {
            string prefabPath = "Assets/Clashura/Prefabs/UI/AbilitySlotUI.prefab";
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (existing != null) return existing;

            // Large, spacious ability card (380x130)
            GameObject slot = new GameObject("AbilitySlotUI");
            var slotRT = slot.AddComponent<RectTransform>();
            slotRT.sizeDelta = new Vector2(380, 130);
            var bg = slot.AddComponent<Image>();
            bg.color = new Color(0.08f, 0.11f, 0.17f, 0.95f);

            var comp = slot.AddComponent<AbilitySlotUI>();
            var so = new SerializedObject(comp);

            // Left: Large Framed Icon (64x64)
            GameObject iconObj = CreateUIObject("Icon", slot.transform);
            RectTransform iRT = iconObj.GetComponent<RectTransform>();
            iRT.anchorMin = new Vector2(0.03f, 0.28f);
            iRT.anchorMax = new Vector2(0.22f, 0.88f);
            iRT.offsetMin = Vector2.zero;
            iRT.offsetMax = Vector2.zero;
            var iconImg = iconObj.AddComponent<Image>();
            iconImg.preserveAspect = true;
            so.FindProperty("iconImage").objectReferenceValue = iconImg;

            // Top: Ability Name (22pt Bold White)
            GameObject nameObj = CreateTextObject("Name", slot.transform, 22, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, Color.white, true, 15f, 26f);
            RectTransform nRT = nameObj.GetComponent<RectTransform>();
            nRT.anchorMin = new Vector2(0.25f, 0.62f);
            nRT.anchorMax = new Vector2(0.70f, 0.94f);
            nRT.offsetMin = Vector2.zero;
            nRT.offsetMax = Vector2.zero;
            so.FindProperty("nameText").objectReferenceValue = nameObj.GetComponent<TextMeshProUGUI>();

            // Top Right: Cooldown (17pt Bold Gold)
            GameObject cdObj = CreateTextObject("CD", slot.transform, 17, FontStyles.Bold, TextAlignmentOptions.MidlineRight, new Color(1f, 0.85f, 0.3f, 1f), true, 12f, 20f);
            RectTransform cdRT = cdObj.GetComponent<RectTransform>();
            cdRT.anchorMin = new Vector2(0.70f, 0.62f);
            cdRT.anchorMax = new Vector2(0.97f, 0.94f);
            cdRT.offsetMin = Vector2.zero;
            cdRT.offsetMax = Vector2.zero;
            so.FindProperty("cooldownText").objectReferenceValue = cdObj.GetComponent<TextMeshProUGUI>();

            // Middle Left: Type Badge (16pt Bold Cyan)
            GameObject typeObj = CreateTextObject("Type", slot.transform, 16, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, new Color(0.0f, 0.88f, 1f, 1f), true, 12f, 18f);
            RectTransform tRT = typeObj.GetComponent<RectTransform>();
            tRT.anchorMin = new Vector2(0.25f, 0.36f);
            tRT.anchorMax = new Vector2(0.65f, 0.62f);
            tRT.offsetMin = Vector2.zero;
            tRT.offsetMax = Vector2.zero;
            so.FindProperty("typeText").objectReferenceValue = typeObj.GetComponent<TextMeshProUGUI>();

            // Bottom Area: Full Description Text (15pt Crisp Silver, Multi-line)
            GameObject descObj = CreateTextObject("Description", slot.transform, 15, FontStyles.Normal, TextAlignmentOptions.TopLeft, new Color(0.82f, 0.86f, 0.94f, 1f), true, 11f, 17f);
            RectTransform dRT = descObj.GetComponent<RectTransform>();
            dRT.anchorMin = new Vector2(0.03f, 0.04f);
            dRT.anchorMax = new Vector2(0.97f, 0.36f);
            dRT.offsetMin = Vector2.zero;
            dRT.offsetMax = Vector2.zero;
            var descTmp = descObj.GetComponent<TextMeshProUGUI>();
            descTmp.enableWordWrapping = true;
            so.FindProperty("descriptionText").objectReferenceValue = descTmp;

            so.ApplyModifiedProperties();

            GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(slot, prefabPath);
            Object.DestroyImmediate(slot);
            return savedPrefab;
        }

        private static GameObject CreateUIObject(string name, Transform parent)
        {
            GameObject obj = new GameObject(name);
            obj.AddComponent<RectTransform>();
            obj.transform.SetParent(parent, false);
            return obj;
        }

        private static GameObject CreateTextObject(
            string name,
            Transform parent,
            float fontSize,
            FontStyles style,
            TextAlignmentOptions align,
            Color color,
            bool autoSize = true,
            float minSize = 12f,
            float maxSize = 72f)
        {
            GameObject obj = CreateUIObject(name, parent);
            var tmp = obj.AddComponent<TextMeshProUGUI>();
            var font = GetDefaultFont();
            if (font != null) tmp.font = font;
            tmp.fontSize = fontSize;
            tmp.fontStyle = style;
            tmp.alignment = align;
            tmp.color = color;
            tmp.enableAutoSizing = autoSize;
            tmp.fontSizeMin = minSize;
            tmp.fontSizeMax = maxSize;
            tmp.overflowMode = TextOverflowModes.Truncate;
            return obj;
        }

        private static GameObject CreateButton(
            string name,
            Transform parent,
            string label,
            Vector2 anchorMin,
            Vector2 anchorMax,
            float fontSize = 24,
            Color? textColor = null,
            Color? btnColor = null)
        {
            GameObject btnObj = CreateUIObject(name, parent);
            RectTransform rt = btnObj.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var img = btnObj.AddComponent<Image>();
            img.color = btnColor ?? new Color(0.18f, 0.24f, 0.35f, 1f);

            var btn = btnObj.AddComponent<Button>();

            GameObject textObj = CreateTextObject("Text", btnObj.transform, fontSize, FontStyles.Bold, TextAlignmentOptions.Center, textColor ?? Color.white, true, 14f, fontSize + 4f);
            RectTransform textRT = textObj.GetComponent<RectTransform>();
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.offsetMin = Vector2.zero;
            textRT.offsetMax = Vector2.zero;
            var tmp = textObj.GetComponent<TextMeshProUGUI>();
            tmp.text = label;

            return btnObj;
        }
    }
}
#endif
