#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Clashura.Utilities
{
    public static class ClashuraMatchmakingRebuilder
    {
        public static void RebuildAllScenesAndBuild()
        {
            Debug.Log("[ClashuraMatchmakingRebuilder] Rebuilding Main Menu & PvP Matchmaking scenes...");
            MainMenuSceneBuilder.BuildMainMenuScene();
            PvPMatchmakingSceneBuilder.BuildMatchmakingScene();
            AICareerSceneBuilder.BuildAllCareerScenes();
            Debug.Log("[ClashuraMatchmakingRebuilder] All scenes rebuilt and saved!");
        }

        public static void RunTestSuiteAndReport()
        {
            var results = PlayFab.PlayFabMatchmakingTestSuite.RunAllTests();
            int passed = 0;
            foreach (var r in results)
            {
                if (r.passed) passed++;
                Debug.Log($"[PlayFabTestSuite] {r.testName}: {(r.passed ? "PASSED" : "FAILED")} - {r.details}");
            }
            Debug.Log($"[PlayFabTestSuite] COMPLETED: {passed}/{results.Count} tests passed.");
        }

        public static void BuildStandaloneWindows64()
        {
            RebuildAllScenesAndBuild();
            Debug.Log("[ClashuraMatchmakingRebuilder] Starting StandaloneWindows64 build to D:/CLASH/CLASH.exe...");

            string[] scenes = new string[]
            {
                "Assets/Clashura/Scenes/MainMenu/MainMenu.unity",
                "Assets/Clashura/Scenes/CharacterSelection/CharacterSelection.unity",
                "Assets/Clashura/Scenes/Multiplayer/PvP_Matchmaking.unity",
                "Assets/Scenes/SampleScene.unity",
                "Assets/Clashura/Scenes/BetterWithAI/AICareerMenu.unity"
            };

            BuildPlayerOptions opts = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = "D:/CLASH/CLASH.exe",
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            };

            var report = BuildPipeline.BuildPlayer(opts);
            Debug.Log($"[ClashuraMatchmakingRebuilder] Build Result: {report.summary.result}, Errors: {report.summary.totalErrors}, Size: {report.summary.totalSize} bytes");
        }
    }
}
#endif
