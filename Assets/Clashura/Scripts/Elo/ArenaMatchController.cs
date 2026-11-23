using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using Clashura.Multiplayer;

namespace Clashura.Elo
{
    /// <summary>
    /// Controls the battle arena match lifecycle in SampleScene:
    /// Tracks active players, match ID, win/loss triggers, and invokes authoritative ELO result submission.
    /// </summary>
    public class ArenaMatchController : MonoBehaviour
    {
        [Header("Arena Match Information")]
        [SerializeField] private TextMeshProUGUI matchInfoText;
        [SerializeField] private TextMeshProUGUI p1InfoText;
        [SerializeField] private TextMeshProUGUI p2InfoText;

        [Header("Match Outcome Triggers")]
        [SerializeField] private Button declareVictoryButton;
        [SerializeField] private Button declareDefeatButton;
        [SerializeField] private Button forfeitButton;

        [Header("Result UI")]
        [SerializeField] private MatchResultUI matchResultUI;

        private PlayerMatchProfile _p1Profile;
        private PlayerMatchProfile _p2Profile;
        private string _activeMatchId = string.Empty;
        private bool _isMatchEnded = false;

        private void Start()
        {
            InitializeArenaMatch();
            HookButtons();
        }

        private void HookButtons()
        {
            if (declareVictoryButton != null)
                declareVictoryButton.onClick.AddListener(OnDeclareVictoryClicked);

            if (declareDefeatButton != null)
                declareDefeatButton.onClick.AddListener(OnDeclareDefeatClicked);

            if (forfeitButton != null)
                forfeitButton.onClick.AddListener(OnForfeitClicked);
        }

        private void InitializeArenaMatch()
        {
            // Retrieve profiles from PhotonFusionManager
            if (PhotonFusionManager.Instance != null)
            {
                _p1Profile = PhotonFusionManager.Instance.GetHostProfile();
                _p2Profile = PhotonFusionManager.Instance.GetClientProfile();
            }
            else
            {
                _p1Profile = PlayerMatchProfile.CreateLocalProfile(true);
                _p2Profile = PlayerMatchProfile.CreateRivalPreviewProfile();
            }

            _activeMatchId = !string.IsNullOrEmpty(_p1Profile.currentMatchId) 
                ? _p1Profile.currentMatchId 
                : "MATCH_" + Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper();

            if (EloManager.Instance != null)
            {
                EloManager.Instance.SetCurrentMatchId(_activeMatchId);
            }

            // Ensure match record exists in DB
            var existingMatch = EloDatabase.GetMatch(_activeMatchId);
            if (existingMatch == null)
            {
                var newMatch = new EloMatchRecord
                {
                    matchId = _activeMatchId,
                    sessionName = "CLASH_ARENA",
                    player1Id = _p1Profile.playerId,
                    player2Id = _p2Profile.playerId,
                    player1Name = _p1Profile.playerName,
                    player2Name = _p2Profile.playerName,
                    p1InitialRating = _p1Profile.playerPoints,
                    p2InitialRating = _p2Profile.playerPoints,
                    status = "InProgress"
                };
                EloDatabase.SaveMatch(newMatch);
            }

            UpdateHudDisplay();
        }

        private void UpdateHudDisplay()
        {
            if (matchInfoText != null)
            {
                matchInfoText.text = $"ARENA BATTLE · MATCH ID: {_activeMatchId}";
            }

            if (p1InfoText != null && _p1Profile != null)
            {
                p1InfoText.text = $"PLAYER 1 (HOST)\n{_p1Profile.playerName.ToUpper()}\n{_p1Profile.playerPoints} ELO";
            }

            if (p2InfoText != null && _p2Profile != null)
            {
                p2InfoText.text = $"PLAYER 2 (CHALLENGER)\n{_p2Profile.playerName.ToUpper()}\n{_p2Profile.playerPoints} ELO";
            }
        }

        public void OnDeclareVictoryClicked()
        {
            if (_isMatchEnded) return;
            string localId = EloManager.Instance != null ? EloManager.Instance.GetLocalPlayerId() : _p1Profile.playerId;
            EndMatchWithWinner(localId);
        }

        public void OnDeclareDefeatClicked()
        {
            if (_isMatchEnded) return;
            string oppId = _p2Profile != null ? _p2Profile.playerId : "Player_Rival_03";
            EndMatchWithWinner(oppId);
        }

        public void OnForfeitClicked()
        {
            if (_isMatchEnded) return;
            string localId = EloManager.Instance != null ? EloManager.Instance.GetLocalPlayerId() : _p1Profile.playerId;
            string oppId = _p2Profile != null ? _p2Profile.playerId : "Player_Rival_03";

            _isMatchEnded = true;
            var response = EloServerService.HandleDisconnect(_activeMatchId, localId);
            if (response != null && response.success && matchResultUI != null)
            {
                matchResultUI.ShowResult(response, _p1Profile.playerName, _p2Profile.playerName, _p1Profile.playerPoints, _p2Profile.playerPoints);
            }
        }

        private void EndMatchWithWinner(string winnerId)
        {
            _isMatchEnded = true;

            string loserId = string.Equals(winnerId, _p1Profile.playerId, StringComparison.OrdinalIgnoreCase) 
                ? _p2Profile.playerId 
                : _p1Profile.playerId;

            if (PlayFab.PlayFabMatchmakingClient.HasInstance)
            {
                PlayFab.PlayFabMatchmakingClient.Instance.SubmitMatchResult(_activeMatchId, winnerId, loserId, (pfRes) =>
                {
                    Debug.Log($"[ArenaMatchController] PlayFab match result processed: {pfRes?.status}");
                });
            }

            var eloMgr = EloManager.Instance;
            if (eloMgr != null)
            {
                eloMgr.SubmitMatchResult(_activeMatchId, winnerId, (response) =>
                {
                    if (response != null && response.success && matchResultUI != null)
                    {
                        matchResultUI.ShowResult(response, _p1Profile.playerName, _p2Profile.playerName, _p1Profile.playerPoints, _p2Profile.playerPoints);
                    }
                    else
                    {
                        Debug.LogError($"[ArenaMatchController] Result submission failed: {response?.errorMessage}");
                    }
                });
            }
            else
            {
                var report = new MatchResultReport
                {
                    matchId = _activeMatchId,
                    reportingPlayerId = _p1Profile.playerId,
                    winnerId = winnerId
                };
                var response = EloServerService.ProcessMatchResult(report);
                if (response != null && response.success && matchResultUI != null)
                {
                    matchResultUI.ShowResult(response, _p1Profile.playerName, _p2Profile.playerName, _p1Profile.playerPoints, _p2Profile.playerPoints);
                }
            }
        }
    }
}
