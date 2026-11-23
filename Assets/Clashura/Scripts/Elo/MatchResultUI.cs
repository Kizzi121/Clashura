using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

namespace Clashura.Elo
{
    /// <summary>
    /// Displays authoritative post-match ELO calculations, rating gains/losses, and updated database values.
    /// Requirements fulfilled:
    /// - Current Player Rating & Opponent Rating
    /// - Rating Gained (+) / Rating Lost (-)
    /// - Updated Rating After Match
    /// - Wins, Losses, and Matches Played career updates
    /// - Verified backend database values and Match ID anti-duplicate tracking.
    /// </summary>
    public class MatchResultUI : MonoBehaviour
    {
        private static MatchResultUI _instance;
        public static MatchResultUI Instance => _instance;

        [Header("Root Panel")]
        [SerializeField] private GameObject resultPanelRoot;
        [SerializeField] private CanvasGroup rootCanvasGroup;

        [Header("Banner & Match Info")]
        [SerializeField] private TextMeshProUGUI outcomeBannerText;
        [SerializeField] private TextMeshProUGUI matchIdText;
        [SerializeField] private TextMeshProUGUI statusSubtitleText;

        [Header("Player Result Card (Left)")]
        [SerializeField] private TextMeshProUGUI playerNameText;
        [SerializeField] private TextMeshProUGUI playerOldRatingText;
        [SerializeField] private TextMeshProUGUI playerRatingDeltaText;
        [SerializeField] private TextMeshProUGUI playerNewRatingText;
        [SerializeField] private TextMeshProUGUI playerCareerStatsText;

        [Header("Opponent Result Card (Right)")]
        [SerializeField] private TextMeshProUGUI opponentNameText;
        [SerializeField] private TextMeshProUGUI opponentOldRatingText;
        [SerializeField] private TextMeshProUGUI opponentRatingDeltaText;
        [SerializeField] private TextMeshProUGUI opponentNewRatingText;

        [Header("Action Buttons")]
        [SerializeField] private Button playAgainButton;
        [SerializeField] private Button mainMenuButton;

        [Header("Development Simulation Controls")]
        [SerializeField] private GameObject simulationToolsRoot;
        [SerializeField] private Button simulateWinButton;
        [SerializeField] private Button simulateLossButton;

        private MatchResultResponse _cachedResponse;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;

            HookButtons();
            if (resultPanelRoot != null)
            {
                resultPanelRoot.SetActive(false);
            }
        }

        private void HookButtons()
        {
            if (playAgainButton != null) playAgainButton.onClick.AddListener(OnPlayAgainClicked);
            if (mainMenuButton != null) mainMenuButton.onClick.AddListener(OnMainMenuClicked);
            if (simulateWinButton != null) simulateWinButton.onClick.AddListener(() => SimulateTestResult(true));
            if (simulateLossButton != null) simulateLossButton.onClick.AddListener(() => SimulateTestResult(false));
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }

        /// <summary>
        /// Displays the verified match result screen with exact database rating changes.
        /// </summary>
        public void ShowResult(
            MatchResultResponse response,
            string p1Name,
            string p2Name,
            int p1OldRating,
            int p2OldRating)
        {
            if (response == null) return;
            _cachedResponse = response;

            if (resultPanelRoot != null)
            {
                resultPanelRoot.SetActive(true);
                resultPanelRoot.transform.SetAsLastSibling();
            }

            // 1. Victory vs Defeat Banner
            bool isWinner = response.isLocalWinner;
            if (outcomeBannerText != null)
            {
                outcomeBannerText.text = isWinner ? "VICTORY!" : "DEFEAT";
                outcomeBannerText.color = isWinner 
                    ? new Color(0.20f, 0.95f, 0.45f) // Emerald Victory
                    : new Color(1.0f, 0.30f, 0.30f);  // Crimson Defeat
            }

            if (statusSubtitleText != null)
            {
                statusSubtitleText.text = "OFFICIAL ELO RATING UPDATED IN DATABASE";
            }

            if (matchIdText != null)
            {
                matchIdText.text = $"MATCH ID: {response.matchId} · STATUS: VERIFIED";
            }

            // 2. Local Player Card
            if (playerNameText != null) playerNameText.text = p1Name.ToUpper();
            if (playerOldRatingText != null) playerOldRatingText.text = $"OLD RATING: {p1OldRating} ELO";

            int myDelta = response.localPlayerDelta;
            if (playerRatingDeltaText != null)
            {
                string sign = myDelta >= 0 ? "+" : "";
                playerRatingDeltaText.text = $"{(myDelta >= 0 ? "RATING GAINED" : "RATING LOST")}: {sign}{myDelta} ELO";
                playerRatingDeltaText.color = myDelta >= 0 
                    ? new Color(0.25f, 1.0f, 0.50f) 
                    : new Color(1.0f, 0.40f, 0.40f);
            }

            if (playerNewRatingText != null)
            {
                playerNewRatingText.text = $"NEW RATING: {response.localPlayerFinalRating} ELO";
            }

            if (playerCareerStatsText != null)
            {
                playerCareerStatsText.text = $"RECORD: {response.localPlayerWins}W - {response.localPlayerLosses}L ({response.localPlayerMatches} Matches)";
            }

            // 3. Opponent Card
            if (opponentNameText != null) opponentNameText.text = p2Name.ToUpper();
            if (opponentOldRatingText != null) opponentOldRatingText.text = $"OLD RATING: {p2OldRating} ELO";

            // Determine opponent delta
            int oppDelta = response.winnerId == response.loserId ? 0 : (isWinner ? (response.player1Delta == myDelta ? response.player2Delta : response.player1Delta) : (response.player1Delta == myDelta ? response.player2Delta : response.player1Delta));
            int oppFinalRating = isWinner ? (response.player1FinalRating == response.localPlayerFinalRating ? response.player2FinalRating : response.player1FinalRating) : (response.player1FinalRating == response.localPlayerFinalRating ? response.player2FinalRating : response.player1FinalRating);

            if (opponentRatingDeltaText != null)
            {
                string oppSign = oppDelta >= 0 ? "+" : "";
                opponentRatingDeltaText.text = $"{(oppDelta >= 0 ? "RATING GAINED" : "RATING LOST")}: {oppSign}{oppDelta} ELO";
                opponentRatingDeltaText.color = oppDelta >= 0 
                    ? new Color(0.25f, 1.0f, 0.50f) 
                    : new Color(1.0f, 0.40f, 0.40f);
            }

            if (opponentNewRatingText != null)
            {
                opponentNewRatingText.text = $"NEW RATING: {oppFinalRating} ELO";
            }

            // Animate card entrance
            StartCoroutine(AnimateEntrance());
        }

        private IEnumerator AnimateEntrance()
        {
            if (rootCanvasGroup != null)
            {
                rootCanvasGroup.alpha = 0f;
                float duration = 0.35f;
                float elapsed = 0f;
                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    rootCanvasGroup.alpha = Mathf.Clamp01(elapsed / duration);
                    yield return null;
                }
                rootCanvasGroup.alpha = 1f;
            }
        }

        public void HideResult()
        {
            if (resultPanelRoot != null)
            {
                resultPanelRoot.SetActive(false);
            }
        }

        private void OnPlayAgainClicked()
        {
            HideResult();
            SceneManager.LoadScene("PvP_Matchmaking");
        }

        private void OnMainMenuClicked()
        {
            HideResult();
            SceneManager.LoadScene("MainMenu");
        }

        /// <summary>
        /// Development utility to test match completion and database ELO calculations.
        /// </summary>
        public void SimulateTestResult(bool localWon)
        {
            var eloMgr = EloManager.Instance;
            if (eloMgr == null) return;

            string myId = eloMgr.GetLocalPlayerId();
            string oppId = "Player_Rival_03";
            string matchId = "SIM_MATCH_" + Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper();

            // Register match in DB
            var p1 = eloMgr.CurrentProfile;
            var p2 = EloDatabase.GetOrCreatePlayer(oppId, "Shadow Rival", 1050);

            var matchRecord = new EloMatchRecord
            {
                matchId = matchId,
                sessionName = "CLASH_SIM",
                player1Id = myId,
                player2Id = oppId,
                player1Name = p1.playerName,
                player2Name = p2.playerName,
                p1InitialRating = p1.rating,
                p2InitialRating = p2.rating,
                status = "InProgress"
            };
            EloDatabase.SaveMatch(matchRecord);

            string winnerId = localWon ? myId : oppId;

            eloMgr.SubmitMatchResult(matchId, winnerId, (response) =>
            {
                if (response != null && response.success)
                {
                    ShowResult(response, p1.playerName, p2.playerName, matchRecord.p1InitialRating, matchRecord.p2InitialRating);
                }
                else
                {
                    Debug.LogError($"[MatchResultUI] Simulation failed: {response?.errorMessage}");
                }
            });
        }
    }
}
