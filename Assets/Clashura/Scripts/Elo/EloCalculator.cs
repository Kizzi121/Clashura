using System;
using UnityEngine;

namespace Clashura.Elo
{
    /// <summary>
    /// Implements standard competitive ELO rating calculations as required by specification:
    /// Expected Score: E = 1 / (1 + 10^((OpponentRating - PlayerRating) / 400))
    /// New Rating: NewRating = OldRating + K * (ActualScore - ExpectedScore)
    /// Default K-factor = 32, ActualScore = 1 for Win, 0 for Loss.
    /// </summary>
    public static class EloCalculator
    {
        public const float DefaultKFactor = 32f;
        public const int MinimumRating = 100;

        /// <summary>
        /// Calculates the expected score of Player against Opponent.
        /// Range: [0.0, 1.0]
        /// </summary>
        public static double CalculateExpectedScore(double playerRating, double opponentRating)
        {
            double exponent = (opponentRating - playerRating) / 400.0;
            return 1.0 / (1.0 + Math.Pow(10.0, exponent));
        }

        /// <summary>
        /// Calculates the ELO rating delta for a player based on match outcome.
        /// </summary>
        public static int CalculateRatingDelta(int playerRating, int opponentRating, bool isWinner, float kFactor = DefaultKFactor)
        {
            double expected = CalculateExpectedScore(playerRating, opponentRating);
            double actual = isWinner ? 1.0 : 0.0;
            double delta = kFactor * (actual - expected);

            // Round to nearest integer (preserving sign)
            return (int)Math.Round(delta, MidpointRounding.AwayFromZero);
        }

        /// <summary>
        /// Computes new ratings and deltas for both players simultaneously.
        /// </summary>
        public static (int p1Delta, int p2Delta, int p1NewRating, int p2NewRating) CalculateMatchOutcomes(
            int p1Rating, 
            int p2Rating, 
            bool p1Won, 
            float kFactor = DefaultKFactor)
        {
            double e1 = CalculateExpectedScore(p1Rating, p2Rating);
            double e2 = CalculateExpectedScore(p2Rating, p1Rating); // Or 1.0 - e1

            double actual1 = p1Won ? 1.0 : 0.0;
            double actual2 = p1Won ? 0.0 : 1.0;

            double deltaRaw1 = kFactor * (actual1 - e1);
            double deltaRaw2 = kFactor * (actual2 - e2);

            int p1Delta = (int)Math.Round(deltaRaw1, MidpointRounding.AwayFromZero);
            int p2Delta = (int)Math.Round(deltaRaw2, MidpointRounding.AwayFromZero);

            int p1NewRating = Math.Max(MinimumRating, p1Rating + p1Delta);
            int p2NewRating = Math.Max(MinimumRating, p2Rating + p2Delta);

            return (p1Delta, p2Delta, p1NewRating, p2NewRating);
        }
    }
}
