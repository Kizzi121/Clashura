using System;
using System.Collections.Generic;
using UnityEngine;
using Clashura.Multiplayer;

namespace Clashura.AI.Career
{
    /// <summary>
    /// Controls the AI Opponent identity and profile presentation.
    /// Strictly enforces the permanent opponent display name: "NOVA".
    /// </summary>
    public static class AIOpponentController
    {
        public const string FixedAIDisplayName = "NOVA";
        public const string OpponentSubtitle = "AI COMBAT NEXUS";

        /// <summary>
        /// Generates an authoritative PlayerMatchProfile for the AI opponent.
        /// Guaranteed to always have display name "NOVA".
        /// </summary>
        public static PlayerMatchProfile CreateNOVAProfile(AIDifficulty difficulty, List<string> selectedAICharacterIds)
        {
            var profile = new PlayerMatchProfile
            {
                playerId = "AI_NOVA_001",
                playerName = FixedAIDisplayName, // Permanent AI name
                playerLevel = difficulty switch
                {
                    AIDifficulty.Easy => 10,
                    AIDifficulty.Medium => 25,
                    AIDifficulty.Hard => 40,
                    _ => 25
                },
                playerPoints = AIDifficultyManager.GetEstimatedAIRating(difficulty),
                wins = 12,
                losses = 4,
                teamCharacterIds = selectedAICharacterIds != null ? new List<string>(selectedAICharacterIds) : new List<string>()
            };

            return profile;
        }
    }
}
