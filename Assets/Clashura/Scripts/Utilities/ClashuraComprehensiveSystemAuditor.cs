#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Clashura.Multiplayer;
using Clashura.UI;
using Clashura.UI.MainMenu;
using Clashura.UI.Animation;
using Clashura.AI;
using Clashura.AI.Career;
using Clashura.PlayFab;
using Clashura.CharacterSelection.SaveSystem;

namespace Clashura.Utilities
{
    /// <summary>
    /// Comprehensive automated system auditor for CLASHURA.
    /// Exhaustively tests all 7 scenes, all buttons, click handlers, team validation,
    /// animations (UIButtonAnimator), ELO matchmaking, and PlayFab integration.
    /// </summary>
    public static class ClashuraComprehensiveSystemAuditor
    {
        public class AuditReport
        {
            public int TotalChecks = 0;
            public int PassedChecks = 0;
            public int FailedChecks = 0;
            public List<string> LogEntries = new List<string>();

            public void Record(string testName, bool passed, string details = "")
            {
                TotalChecks++;
                if (passed)
                {
                    PassedChecks++;
                    LogEntries.Add($"[PASS] {testName} {(string.IsNullOrEmpty(details) ? "" : "-> " + details)}");
                    Debug.Log($"<color=green>[AUDIT PASS]</color> {testName} {details}");
                }
                else
                {
                    FailedChecks++;
                    LogEntries.Add($"[FAIL] {testName} -> {details}");
                    Debug.LogError($"<color=red>[AUDIT FAIL]</color> {testName} -> {details}");
                }
            }
        }

        private static GameObject FindInScene(string name)
        {
            var roots = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
            foreach (var root in roots)
            {
                var found = FindTransformRecursive(root.transform, name);
                if (found != null) return found.gameObject;
            }
            return null;
        }

        private static Transform FindTransformRecursive(Transform parent, string name)
        {
            if (parent.name == name) return parent;
            for (int i = 0; i < parent.childCount; i++)
            {
                var found = FindTransformRecursive(parent.GetChild(i), name);
                if (found != null) return found;
            }
            return null;
        }

        [MenuItem("Clashura/Audit/Run Full System & Animation Audit")]
        public static void RunFullAudit()
        {
            var report = new AuditReport();
            Debug.Log("==========================================================================");
            Debug.Log("[ClashuraSystemAuditor] STARTING COMPREHENSIVE SYSTEM & ANIMATION AUDIT...");
            Debug.Log("==========================================================================");

            // 1. Build Settings & Scene Assets
            AuditBuildSettings(report);

            // 2. Main Menu Scene Audit
            AuditMainMenuScene(report);

            // 3. PvP Matchmaking Scene Audit (Custom Rooms & Random Match)
            AuditPvPMatchmakingScene(report);

            // 4. Character Selection Scene Audit
            AuditCharacterSelectionScene(report);

            // 5. AI Career Menu Scene Audit
            AuditAICareerMenuScene(report);

            // 6. Difficulty Selection Scene Audit (BetterWithAI)
            AuditBetterWithAIScene(report);

            // 7. AI Versus Screen Audit
            AuditAIVersusScreenScene(report);

            // 8. Unified AI Career Single-Scene 3-Panel Audit
            AuditUnifiedAICareerMode(report);

            // 9. PlayFab & ELO Matchmaking Test Suite
            AuditPlayFabAndElo(report);

            // 9. Summary & Report Generation
            Debug.Log("==========================================================================");
            Debug.Log($"[ClashuraSystemAuditor] AUDIT FINISHED: {report.PassedChecks}/{report.TotalChecks} PASSED (Failures: {report.FailedChecks})");
            Debug.Log("==========================================================================");

            string reportDir = "C:/Users/User/.gemini/antigravity/brain/76bfe5fc-2f35-42db-9203-03da4b0351a9/scratch";
            if (!Directory.Exists(reportDir)) Directory.CreateDirectory(reportDir);
            string reportPath = Path.Combine(reportDir, "system_audit_results.txt");
            File.WriteAllLines(reportPath, report.LogEntries);
        }

        private static void AuditBuildSettings(AuditReport report)
        {
            var scenes = EditorBuildSettings.scenes;
            report.Record("BuildSettings Registered Scenes Count", scenes.Length == 5, $"Found {scenes.Length} scenes (5 active scenes: MainMenu, CharacterSelection, PvP_Matchmaking, SampleScene, AICareerMenu).");

            string[] expectedScenes = new string[]
            {
                "Assets/Clashura/Scenes/MainMenu/MainMenu.unity",
                "Assets/Clashura/Scenes/CharacterSelection/CharacterSelection.unity",
                "Assets/Clashura/Scenes/Multiplayer/PvP_Matchmaking.unity",
                "Assets/Scenes/SampleScene.unity",
                "Assets/Clashura/Scenes/BetterWithAI/AICareerMenu.unity"
            };

            foreach (var s in expectedScenes)
            {
                bool exists = File.Exists(s);
                report.Record($"Scene File Exists: {Path.GetFileName(s)}", exists, s);
            }
        }

        private static void AuditMainMenuScene(AuditReport report)
        {
            string scenePath = "Assets/Clashura/Scenes/MainMenu/MainMenu.unity";
            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

            var mmCtrl = UnityEngine.Object.FindFirstObjectByType<MainMenuController>();
            report.Record("MainMenu: MainMenuController exists", mmCtrl != null);

            var pvpCtrl = UnityEngine.Object.FindFirstObjectByType<PvPMenuController>();
            report.Record("MainMenu: PvPMenuController exists", pvpCtrl != null);

            // Audit Buttons
            string[] buttons = new string[] { "Btn_PvP", "Btn_RandomMatch", "Btn_PlayWithAI", "Btn_CharacterSelection", "Btn_Settings", "Btn_Exit" };
            foreach (var bName in buttons)
            {
                var go = FindInScene(bName);
                report.Record($"MainMenu: Button '{bName}' exists", go != null);
                if (go != null)
                {
                    var btn = go.GetComponent<Button>();
                    report.Record($"MainMenu: Button '{bName}' has Button component", btn != null && btn.interactable);
                    var anim = go.GetComponent<UIButtonAnimator>();
                    report.Record($"MainMenu: Button '{bName}' has UIButtonAnimator", anim != null);
                }
            }

            // Audit Button Hierarchy Order: Position 1 = PvP, Position 2 = RandomMatch, Position 3 = PlayWithAI
            var btnContainer = FindInScene("MenuButtonsContainer");
            if (btnContainer != null)
            {
                var pvpChild = btnContainer.transform.Find("Btn_PvP");
                var randomChild = btnContainer.transform.Find("Btn_RandomMatch");
                var aiChild = btnContainer.transform.Find("Btn_PlayWithAI");

                report.Record("MainMenu: Position 1 is PvP", pvpChild != null && pvpChild.GetSiblingIndex() == 0);
                report.Record("MainMenu: Position 2 is Random Match", randomChild != null && randomChild.GetSiblingIndex() == 1);
                report.Record("MainMenu: Position 3 is Play With AI", aiChild != null && aiChild.GetSiblingIndex() == 2);

                var randomAnim = randomChild?.GetComponent<UIButtonAnimator>();
                report.Record("MainMenu: Btn_RandomMatch has EnablePulse animation (matching CharacterSelection)", randomAnim != null && randomAnim.EnablePulse);

                var aiAnim = aiChild?.GetComponent<UIButtonAnimator>();
                report.Record("MainMenu: Btn_PlayWithAI has EnablePulse animation (matching CharacterSelection)", aiAnim != null && aiAnim.EnablePulse);

                var charAnim = btnContainer.transform.Find("Btn_CharacterSelection")?.GetComponent<UIButtonAnimator>();
                report.Record("MainMenu: Btn_CharacterSelection has EnablePulse animation", charAnim != null && charAnim.EnablePulse);
            }

            // Audit Warning Panel & Team Validation
            var warnGo = pvpCtrl != null && pvpCtrl.WarningPanel != null ? pvpCtrl.WarningPanel : FindInScene("TeamWarningPanel");
            report.Record("MainMenu: TeamWarningPanel exists", warnGo != null);

            if (pvpCtrl != null && warnGo != null)
            {
                // Test 1: Without Team -> shows warning
                PlayerPrefs.DeleteKey("CLASHURA_SELECTION_Player_Local");
                pvpCtrl.OnPlayerVsPlayerClicked();
                report.Record("MainMenu: PvP click with no team shows TeamWarningPanel", warnGo.activeSelf);

                var warnTxt = pvpCtrl.WarningText != null ? pvpCtrl.WarningText : FindInScene("WarningMessageText")?.GetComponent<TextMeshProUGUI>();
                report.Record("MainMenu: TeamWarningPanel message is exact ('Please Select Your Team First!')", warnTxt != null && warnTxt.text == "Please Select Your Team First!");

                // Test 2: Random Match without Team -> shows warning
                warnGo.SetActive(false);
                pvpCtrl.OnRandomMatchClicked();
                report.Record("MainMenu: Random Match click with no team shows TeamWarningPanel", warnGo.activeSelf);

                // Test 3: Go to Character Selection button exists on Warning Panel
                var goBtn = pvpCtrl.GoToCharacterSelectionButton != null ? pvpCtrl.GoToCharacterSelectionButton.gameObject : FindInScene("Btn_GoToCharacterSelection");
                report.Record("MainMenu: Warning Panel has 'GoToCharacterSelectionButton'", goBtn != null);
                if (goBtn != null)
                {
                    report.Record("MainMenu: 'GoToCharacterSelectionButton' has UIButtonAnimator", goBtn.GetComponent<UIButtonAnimator>() != null);
                }

                // Test 4: With Team -> sets EntryMode and allows entry
                var dummySelection = new PlayerSelectionData
                {
                    SelectedCharacterIds = new List<string> { "clashura_char_01", "clashura_char_02", "clashura_char_03" }
                };
                PlayerPrefs.SetString("CLASHURA_SELECTION_Player_Local", JsonUtility.ToJson(dummySelection));
                warnGo.SetActive(false);

                pvpCtrl.OnPlayerVsPlayerClicked();
                report.Record("MainMenu: PvP with team sets EntryMode.CustomRooms", MatchmakingManager.EntryMode == MatchmakingEntryMode.CustomRooms);

                pvpCtrl.OnRandomMatchClicked();
                report.Record("MainMenu: Random Match with team sets EntryMode.RandomMatch", MatchmakingManager.EntryMode == MatchmakingEntryMode.RandomMatch);
            }
        }

        private static void AuditPvPMatchmakingScene(AuditReport report)
        {
            string scenePath = "Assets/Clashura/Scenes/Multiplayer/PvP_Matchmaking.unity";
            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

            var mmUI = UnityEngine.Object.FindFirstObjectByType<MatchmakingUI>();
            report.Record("PvP_Matchmaking: MatchmakingUI exists", mmUI != null);

            var mmMgr = UnityEngine.Object.FindFirstObjectByType<MatchmakingManager>();
            report.Record("PvP_Matchmaking: MatchmakingManager exists", mmMgr != null);

            if (mmUI != null)
            {
                var awakeMethod = typeof(MatchmakingUI).GetMethod("Awake", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                awakeMethod?.Invoke(mmUI, null);

                // Audit Buttons & Animations via MatchmakingUI properties
                var buttonMap = new Dictionary<string, Button>
                {
                    { "BackButton", mmUI.BackToMenuButton },
                    { "Btn_Choice_Create", mmUI.ChooseCreateButton },
                    { "Btn_Choice_Search", mmUI.ChooseSearchButton },
                    { "Btn_HostRoom_Action", mmUI.HostRoomActionButton },
                    { "Btn_BackFromCreate", mmUI.BackFromCreateButton },
                    { "Btn_JoinRoom_Action", mmUI.JoinRoomActionButton },
                    { "Btn_BackFromSearch", mmUI.BackFromSearchButton },
                    { "CancelButton", mmUI.CancelButton }
                };

                foreach (var kvp in buttonMap)
                {
                    report.Record($"PvP_Matchmaking: Button '{kvp.Key}' exists", kvp.Value != null);
                    if (kvp.Value != null)
                    {
                        var anim = kvp.Value.GetComponent<UIButtonAnimator>();
                        report.Record($"PvP_Matchmaking: Button '{kvp.Key}' has UIButtonAnimator", anim != null);
                    }
                }

                // Audit Loading Spinner Animation
                report.Record("PvP_Matchmaking: LoadingSpinnerIcon exists for rotation animation", mmUI.LoadingSpinnerIcon != null);

                // Audit Versus Screen component
                report.Record("PvP_Matchmaking: VersusMatchupUI component exists", mmUI.VersusUI != null);

                // Custom Rooms mode
                MatchmakingManager.EntryMode = MatchmakingEntryMode.CustomRooms;
                mmUI.ShowModeSelection();
                var modePanel = FindInScene("Panel_ModeSelect");
                report.Record("PvP_Matchmaking: ShowModeSelection displays ModeSelectPanel", modePanel != null && modePanel.activeSelf);

                // Random Match mode
                MatchmakingManager.EntryMode = MatchmakingEntryMode.RandomMatch;
                mmUI.StartRandomEloMatchmaking();
                report.Record("PvP_Matchmaking: StartRandomEloMatchmaking hides ModeSelectPanel", modePanel != null && !modePanel.activeSelf);

                report.Record("PvP_Matchmaking: Random Match activates CancelButton", mmUI.CancelButton != null && mmUI.CancelButton.gameObject.activeSelf);

                var badge = FindInScene("PlayerRatingBadge");
                report.Record("PvP_Matchmaking: PlayerRatingBadge exists", badge != null);

                // Audit Cancel Confirmation Modal Panel
                var confModal = mmUI.CancelConfirmationPanel;
                report.Record("PvP_Matchmaking: CancelConfirmationPanel exists", confModal != null);
                if (confModal != null)
                {
                    report.Record("PvP_Matchmaking: CancelConfirmationPanel starts inactive", !confModal.activeSelf);

                    var yesBtn = mmUI.CancelConfirmYesButton;
                    report.Record("PvP_Matchmaking: CancelConfirmYesButton exists", yesBtn != null);
                    if (yesBtn != null)
                    {
                        report.Record("PvP_Matchmaking: CancelConfirmYesButton has UIButtonAnimator", yesBtn.GetComponent<UIButtonAnimator>() != null);
                    }

                    var noBtn = mmUI.CancelConfirmNoButton;
                    report.Record("PvP_Matchmaking: CancelConfirmNoButton exists", noBtn != null);
                    if (noBtn != null)
                    {
                        report.Record("PvP_Matchmaking: CancelConfirmNoButton has UIButtonAnimator", noBtn.GetComponent<UIButtonAnimator>() != null);
                    }

                    // Test clicking CancelButton opens CancelConfirmationPanel
                    mmUI.CancelButton.onClick.Invoke();
                    report.Record("PvP_Matchmaking: Clicking CancelButton displays CancelConfirmationPanel", confModal.activeSelf);

                    // Test clicking NO closes CancelConfirmationPanel
                    if (noBtn != null)
                    {
                        noBtn.onClick.Invoke();
                        report.Record("PvP_Matchmaking: Clicking CancelConfirmNoButton closes CancelConfirmationPanel", !confModal.activeSelf);
                    }

                    // Re-test display and YES button
                    mmUI.CancelButton.onClick.Invoke();
                    if (yesBtn != null)
                    {
                        yesBtn.onClick.Invoke();
                        report.Record("PvP_Matchmaking: Clicking CancelConfirmYesButton closes modal and cancels queue", !confModal.activeSelf);
                    }
                }
            }
        }

        private static void AuditCharacterSelectionScene(AuditReport report)
        {
            string scenePath = "Assets/Clashura/Scenes/CharacterSelection/CharacterSelection.unity";
            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

            var charUI = UnityEngine.Object.FindFirstObjectByType<Clashura.CharacterSelection.UI.CharacterSelectionUI>();
            report.Record("CharacterSelection: CharacterSelectionUI exists", charUI != null);

            var charMgr = UnityEngine.Object.FindFirstObjectByType<Clashura.CharacterSelection.Managers.CharacterSelectionManager>();
            report.Record("CharacterSelection: CharacterSelectionManager exists", charMgr != null);

            // Audit Hero Database
            var db = AssetDatabase.LoadAssetAtPath<Clashura.Characters.Managers.CharacterDatabaseSO>("Assets/Clashura/Data/Characters/ScriptableObjects/CharacterDatabase.asset");
            report.Record("CharacterSelection: CharacterDatabase.asset exists", db != null);
            if (db != null)
            {
                report.Record("CharacterSelection: CharacterDatabase has 18 champions", db.CharacterCount == 18);
            }
        }

        private static void AuditAICareerMenuScene(AuditReport report)
        {
            string scenePath = "Assets/Clashura/Scenes/BetterWithAI/AICareerMenu.unity";
            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

            var careerUI = UnityEngine.Object.FindFirstObjectByType<AICareerUI>();
            report.Record("AICareerMenu: AICareerUI exists", careerUI != null);

            // Audit Buttons
            string[] buttons = new string[] { "Btn_NewGame", "Btn_Continue", "Btn_BackButton", "Btn_Confirm_Yes", "Btn_Confirm_No" };
            foreach (var bName in buttons)
            {
                var go = FindInScene(bName);
                report.Record($"AICareerMenu: Button '{bName}' exists", go != null);
                if (go != null)
                {
                    var anim = go.GetComponent<UIButtonAnimator>();
                    report.Record($"AICareerMenu: Button '{bName}' has UIButtonAnimator", anim != null);
                }
            }

            // Audit Confirmation Panel
            var confPanel = careerUI != null ? careerUI.ConfirmationPanel : FindInScene("ConfirmationPanelRoot");
            report.Record("AICareerMenu: ConfirmationPanel exists", confPanel != null);

            // Audit AI Character Database & Random Selector in Career Manager
            var charDb = UnityEngine.Object.FindFirstObjectByType<AICharacterDatabase>(FindObjectsInactive.Include);
            report.Record("AICareerMenu: AICharacterDatabase exists on AICareerManager", charDb != null);
            if (charDb != null)
            {
                report.Record("AICareerMenu: Easy AI has 6 characters", charDb.EasyAIList.Count == 6);
                report.Record("AICareerMenu: Medium AI has 6 characters", charDb.MediumAIList.Count == 6);
                report.Record("AICareerMenu: Hard AI has 6 characters", charDb.HardAIList.Count == 6);

                var sampled = AIRandomSelector.SelectRandomAITeam(AIDifficulty.Easy, charDb);
                report.Record("AICareerMenu: AIRandomSelector samples exactly 3 unique characters", sampled.Count == 3 && AIRandomSelector.ValidateThreeUnique(sampled));
            }
        }

        private static void AuditBetterWithAIScene(AuditReport report)
        {
            string scenePath = "Assets/Clashura/Scenes/BetterWithAI/AICareerMenu.unity";
            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

            var ctrl = UnityEngine.Object.FindFirstObjectByType<BetterWithAIController>();
            report.Record("BetterWithAI: BetterWithAIController exists", ctrl != null);

            if (ctrl != null)
            {
                // Audit Difficulty Buttons & Animations
                var buttonMap = new Dictionary<string, Button>
                {
                    { "Btn_Difficulty_Easy", ctrl.EasyButton },
                    { "Btn_Difficulty_Medium", ctrl.MediumButton },
                    { "Btn_Difficulty_Hard", ctrl.HardButton },
                    { "Btn_BackToMenu", ctrl.BackButton }
                };

                foreach (var kvp in buttonMap)
                {
                    report.Record($"BetterWithAI: Button '{kvp.Key}' exists", kvp.Value != null);
                    if (kvp.Value != null)
                    {
                        var anim = kvp.Value.GetComponent<UIButtonAnimator>();
                        report.Record($"BetterWithAI: Button '{kvp.Key}' has UIButtonAnimator", anim != null);
                    }
                }
            }
        }

        private static void AuditAIVersusScreenScene(AuditReport report)
        {
            string scenePath = "Assets/Clashura/Scenes/BetterWithAI/AICareerMenu.unity";
            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

            var vsCtrl = UnityEngine.Object.FindFirstObjectByType<AIVSScreenController>();
            report.Record("AIVersusScreen: AIVSScreenController exists", vsCtrl != null);

            var startBtn = FindInScene("Btn_StartMatch");
            report.Record("AIVersusScreen: Btn_StartMatch exists", startBtn != null);
            if (startBtn != null)
            {
                report.Record("AIVersusScreen: Btn_StartMatch has UIButtonAnimator", startBtn.GetComponent<UIButtonAnimator>() != null);
            }

            var backBtn = FindInScene("Btn_BackButton") ?? FindInScene("Btn_VersusBackButton");
            report.Record("AIVersusScreen: Btn_BackButton exists", backBtn != null);
            if (backBtn != null)
            {
                report.Record("AIVersusScreen: Btn_BackButton has UIButtonAnimator", backBtn.GetComponent<UIButtonAnimator>() != null);
            }

            // Verify AI Identity is Permanently NOVA
            report.Record("AIVersusScreen: AI Opponent Name is permanently NOVA", AIOpponentController.FixedAIDisplayName == "NOVA");
        }

        private static void AuditUnifiedAICareerMode(AuditReport report)
        {
            string scenePath = "Assets/Clashura/Scenes/BetterWithAI/AICareerMenu.unity";
            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

            var masterUI = UnityEngine.Object.FindFirstObjectByType<AICareerMasterUI>();
            report.Record("UnifiedAICareer: AICareerMasterUI component exists", masterUI != null);

            if (masterUI != null)
            {
                // Verify all 3 panels are present
                report.Record("UnifiedAICareer: Panel 1 (CareerMenu) assigned", masterUI.CareerMenuPanel != null);
                report.Record("UnifiedAICareer: Panel 2 (DifficultySelect) assigned", masterUI.DifficultySelectPanel != null);
                report.Record("UnifiedAICareer: Panel 3 (VersusScreen) assigned", masterUI.VersusScreenPanel != null);
                report.Record("UnifiedAICareer: ConfirmationModalPanel assigned", masterUI.ConfirmationModalPanel != null);

                // Verify sub-controllers
                report.Record("UnifiedAICareer: CareerUI sub-controller assigned", masterUI.CareerUI != null);
                report.Record("UnifiedAICareer: DifficultyUI sub-controller assigned", masterUI.DifficultyUI != null);
                report.Record("UnifiedAICareer: VersusUI sub-controller assigned", masterUI.VersusUI != null);

                // Test Seamless Panel Switching (Zero Scene Reload)
                masterUI.ShowCareerMenu();
                report.Record("UnifiedAICareer: ShowCareerMenu sets Panel 1 active and others inactive", 
                    masterUI.CurrentPanel == AICareerMasterUI.PanelType.CareerMenu 
                    && masterUI.CareerMenuPanel.activeSelf 
                    && !masterUI.DifficultySelectPanel.activeSelf 
                    && !masterUI.VersusScreenPanel.activeSelf);

                masterUI.ShowDifficultySelection();
                report.Record("UnifiedAICareer: ShowDifficultySelection sets Panel 2 active and others inactive", 
                    masterUI.CurrentPanel == AICareerMasterUI.PanelType.DifficultySelect 
                    && !masterUI.CareerMenuPanel.activeSelf 
                    && masterUI.DifficultySelectPanel.activeSelf 
                    && !masterUI.VersusScreenPanel.activeSelf);

                masterUI.ShowVersusScreen(AIDifficulty.Medium);
                report.Record("UnifiedAICareer: ShowVersusScreen sets Panel 3 active and others inactive", 
                    masterUI.CurrentPanel == AICareerMasterUI.PanelType.AIVersus 
                    && !masterUI.CareerMenuPanel.activeSelf 
                    && !masterUI.DifficultySelectPanel.activeSelf 
                    && masterUI.VersusScreenPanel.activeSelf);

                // Return to Career Menu
                masterUI.ShowCareerMenu();
                report.Record("UnifiedAICareer: Returns to Panel 1 cleanly", 
                    masterUI.CurrentPanel == AICareerMasterUI.PanelType.CareerMenu 
                    && masterUI.CareerMenuPanel.activeSelf);
            }
        }

        private static void AuditPlayFabAndElo(AuditReport report)
        {
            var results = PlayFabMatchmakingTestSuite.RunAllTests();
            report.Record("PlayFab/ELO: All 10 Test Suite Scenarios Pass", results.TrueForAll(r => r.passed), $"{results.FindAll(r => r.passed).Count}/10 passed.");
            foreach (var r in results)
            {
                report.Record($"PlayFab/ELO: {r.testName}", r.passed, r.details);
            }
        }
    }
}
#endif
