using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Clashura.Elo
{
    /// <summary>
    /// Server-authoritative Matchmaking & ELO Service.
    /// Executes dynamic rating range matchmaking (±50 -> ±100 -> ±150),
    /// authoritative match result validation, duplicate protection, and ELO rating updates.
    /// Never allows clients to directly modify their own rating.
    /// </summary>
    public static class EloServerService
    {
        public const int InitialRadius = 50;
        public const int RadiusStep = 50;
        public const int MaxRadius = 500;
        public const int StepIntervalSeconds = 10;

        /// <summary>
        /// Registers or updates a player's initial rating in the database.
        /// </summary>
        public static PlayerEloProfile GetOrRegisterPlayer(string playerId, string playerName, int defaultRating = 1000)
        {
            return EloDatabase.GetOrCreatePlayer(playerId, playerName, defaultRating);
        }

        /// <summary>
        /// Creates and enqueues a new matchmaking ticket with the player's authoritative database rating.
        /// </summary>
        public static MatchmakingTicket EnqueueMatchmaking(string playerId, string playerName)
        {
            // 1. Always fetch authoritative rating from database (do not trust client)
            var profile = EloDatabase.GetOrCreatePlayer(playerId, playerName);

            var ticket = new MatchmakingTicket
            {
                ticketId = "TKT_" + Guid.NewGuid().ToString("N").Substring(0, 12),
                playerId = profile.playerId,
                playerName = profile.playerName,
                playerRating = profile.rating,
                currentSearchRadius = InitialRadius,
                searchStartTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                status = "Queued"
            };

            EloDatabase.EnqueueTicket(ticket);
            Debug.Log($"[EloServerService] Enqueued ticket {ticket.ticketId} for player '{ticket.playerName}' (Rating: {ticket.playerRating}, Initial Radius: ±{InitialRadius}).");
            return ticket;
        }

        /// <summary>
        /// Evaluates the matchmaking pool for a given ticket, applying dynamic range expansion:
        /// 0-10s: ±50 points
        /// 10-20s: ±100 points
        /// 20-30s: ±150 points
        /// Continues expanding up to MaxRadius (±500 points).
        /// </summary>
        public static MatchmakingTicket PollMatchmaking(string ticketId)
        {
            var ticket = EloDatabase.GetTicket(ticketId);
            if (ticket == null) return null;

            if (ticket.status == "Matched" || ticket.status == "Canceled")
            {
                return ticket;
            }

            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            long elapsed = now - ticket.searchStartTime;

            // 1. Calculate dynamic search radius
            int steps = (int)(elapsed / StepIntervalSeconds);
            int targetRadius = Math.Min(MaxRadius, InitialRadius + (steps * RadiusStep));

            if (targetRadius != ticket.currentSearchRadius)
            {
                ticket.currentSearchRadius = targetRadius;
                EloDatabase.UpdateTicket(ticket);
                Debug.Log($"[EloServerService] Dynamic Range Expansion for '{ticket.playerName}': Search radius expanded to ±{ticket.currentSearchRadius} (Elapsed: {elapsed}s).");
            }

            // 2. Scan active tickets for eligible opponent
            EloDatabase.CleanupExpiredTickets(120);
            var db = EloDatabase.Load();

            var candidates = new List<MatchmakingTicket>();
            foreach (var candidate in db.activeTickets)
            {
                // Must not be self
                if (string.Equals(candidate.playerId, ticket.playerId, StringComparison.OrdinalIgnoreCase))
                    continue;

                // Candidate must be waiting
                if (candidate.status != "Queued")
                    continue;

                int ratingDiff = Math.Abs(candidate.playerRating - ticket.playerRating);

                // Both players must accept each other's search radius
                if (ratingDiff <= ticket.currentSearchRadius && ratingDiff <= candidate.currentSearchRadius)
                {
                    candidates.Add(candidate);
                }
            }

            // 3. Match pairing logic
            if (candidates.Count > 0)
            {
                // "Never match the player with an unsuitable opponent immediately if a closer rating opponent is available."
                int minDiff = candidates.Min(c => Math.Abs(c.playerRating - ticket.playerRating));
                var bestCandidates = candidates.Where(c => Math.Abs(c.playerRating - ticket.playerRating) == minDiff).ToList();

                // "Randomly select one suitable opponent" among the closest tier
                var opponent = bestCandidates[UnityEngine.Random.Range(0, bestCandidates.Count)];

                // 4. Create authoritative match record
                string matchId = "MATCH_" + Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper();
                string sessionName = "CLASH_ELO_" + matchId.Substring(6);

                var matchRecord = new EloMatchRecord
                {
                    matchId = matchId,
                    sessionName = sessionName,
                    player1Id = ticket.playerId,
                    player2Id = opponent.playerId,
                    player1Name = ticket.playerName,
                    player2Name = opponent.playerName,
                    p1InitialRating = ticket.playerRating,
                    p2InitialRating = opponent.playerRating,
                    status = "InProgress",
                    createdTimestamp = now
                };

                EloDatabase.SaveMatch(matchRecord);

                // 5. Update Ticket 1 (Host)
                ticket.status = "Matched";
                ticket.isHost = true;
                ticket.matchId = matchId;
                ticket.matchedSessionName = sessionName;
                ticket.matchedOpponentId = opponent.playerId;
                ticket.matchedOpponentName = opponent.playerName;
                ticket.matchedOpponentRating = opponent.playerRating;
                EloDatabase.UpdateTicket(ticket);

                // 6. Update Ticket 2 (Client)
                opponent.status = "Matched";
                opponent.isHost = false;
                opponent.matchId = matchId;
                opponent.matchedSessionName = sessionName;
                opponent.matchedOpponentId = ticket.playerId;
                opponent.matchedOpponentName = ticket.playerName;
                opponent.matchedOpponentRating = ticket.playerRating;
                EloDatabase.UpdateTicket(opponent);

                Debug.Log($"[EloServerService] MATCH CREATED! {ticket.playerName} (Rating: {ticket.playerRating}) vs {opponent.playerName} (Rating: {opponent.playerRating}) | Diff: {minDiff} | Match ID: {matchId} | Session: {sessionName}");
                return ticket;
            }

            return ticket;
        }

        /// <summary>
        /// Cancels an enqueued matchmaking ticket.
        /// </summary>
        public static bool CancelMatchmaking(string ticketId)
        {
            var ticket = EloDatabase.GetTicket(ticketId);
            if (ticket != null)
            {
                ticket.status = "Canceled";
                EloDatabase.UpdateTicket(ticket);
                EloDatabase.RemoveTicket(ticketId);
                Debug.Log($"[EloServerService] Canceled matchmaking ticket {ticketId}.");
                return true;
            }
            return false;
        }

        /// <summary>
        /// Server-authoritative match resolution:
        /// 1. Validates Match ID and prevents duplicate submissions (idempotency guard).
        /// 2. Verifies winner / loser from server records.
        /// 3. Computes standard ELO calculation:
        ///    E = 1 / (1 + 10^((OpponentRating - PlayerRating) / 400))
        ///    NewRating = OldRating + K * (ActualScore - ExpectedScore)
        /// 4. Updates both players' persistent database records (ratings, wins, losses, matchesPlayed).
        /// 5. Marks match as permanently processed.
        /// </summary>
        public static MatchResultResponse ProcessMatchResult(MatchResultReport report)
        {
            var response = new MatchResultResponse();

            if (report == null || string.IsNullOrWhiteSpace(report.matchId))
            {
                response.success = false;
                response.errorMessage = "Invalid match report payload.";
                return response;
            }

            // 1. Prevent duplicate rating updates for the same Match ID
            if (EloDatabase.IsMatchProcessed(report.matchId))
            {
                Debug.LogWarning($"[EloServerService] Duplicate result submission blocked for Match ID '{report.matchId}'.");
                var existingMatch = EloDatabase.GetMatch(report.matchId);
                if (existingMatch != null)
                {
                    PopulateResponseFromExisting(response, existingMatch, report.reportingPlayerId);
                    response.success = true;
                    return response;
                }
                response.success = false;
                response.errorMessage = "MATCH_ALREADY_PROCESSED";
                return response;
            }

            // 2. Retrieve authoritative match record
            var match = EloDatabase.GetMatch(report.matchId);
            if (match == null)
            {
                response.success = false;
                response.errorMessage = $"Match ID '{report.matchId}' not found in database.";
                return response;
            }

            // 3. Validate winner ID
            bool isP1Winner = string.Equals(report.winnerId, match.player1Id, StringComparison.OrdinalIgnoreCase);
            bool isP2Winner = string.Equals(report.winnerId, match.player2Id, StringComparison.OrdinalIgnoreCase);

            if (!isP1Winner && !isP2Winner)
            {
                response.success = false;
                response.errorMessage = $"Winner ID '{report.winnerId}' is neither Player 1 nor Player 2 in match '{report.matchId}'.";
                return response;
            }

            // 4. Fetch authoritative current ratings from database
            var p1Profile = EloDatabase.GetOrCreatePlayer(match.player1Id, match.player1Name, match.p1InitialRating);
            var p2Profile = EloDatabase.GetOrCreatePlayer(match.player2Id, match.player2Name, match.p2InitialRating);

            // 5. Execute standard ELO calculation
            var outcomes = EloCalculator.CalculateMatchOutcomes(p1Profile.rating, p2Profile.rating, isP1Winner);

            int p1Delta = outcomes.p1Delta;
            int p2Delta = outcomes.p2Delta;
            int p1NewRating = outcomes.p1NewRating;
            int p2NewRating = outcomes.p2NewRating;

            // 6. Update database profiles
            p1Profile.rating = p1NewRating;
            p1Profile.matchesPlayed++;
            if (isP1Winner) p1Profile.wins++; else p1Profile.losses++;
            EloDatabase.UpdatePlayer(p1Profile);

            p2Profile.rating = p2NewRating;
            p2Profile.matchesPlayed++;
            if (isP2Winner) p2Profile.wins++; else p2Profile.losses++;
            EloDatabase.UpdatePlayer(p2Profile);

            // 7. Update and finalize Match Record
            match.status = "Completed";
            match.winnerId = report.winnerId;
            match.loserId = isP1Winner ? match.player2Id : match.player1Id;
            match.p1RatingDelta = p1Delta;
            match.p2RatingDelta = p2Delta;
            match.p1FinalRating = p1NewRating;
            match.p2FinalRating = p2NewRating;
            match.isProcessed = true;
            match.completedTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            EloDatabase.SaveMatch(match);
            EloDatabase.MarkMatchProcessed(match.matchId);

            // 8. Build and return verified response
            response.success = true;
            response.matchId = match.matchId;
            response.winnerId = match.winnerId;
            response.loserId = match.loserId;
            response.player1Delta = p1Delta;
            response.player2Delta = p2Delta;
            response.player1FinalRating = p1NewRating;
            response.player2FinalRating = p2NewRating;

            bool isReportingP1 = string.Equals(report.reportingPlayerId, match.player1Id, StringComparison.OrdinalIgnoreCase);
            response.localPlayerDelta = isReportingP1 ? p1Delta : p2Delta;
            response.localPlayerFinalRating = isReportingP1 ? p1NewRating : p2NewRating;
            response.localPlayerWins = isReportingP1 ? p1Profile.wins : p2Profile.wins;
            response.localPlayerLosses = isReportingP1 ? p1Profile.losses : p2Profile.losses;
            response.localPlayerMatches = isReportingP1 ? p1Profile.matchesPlayed : p2Profile.matchesPlayed;
            response.isLocalWinner = string.Equals(report.reportingPlayerId, match.winnerId, StringComparison.OrdinalIgnoreCase);

            Debug.Log($"[EloServerService] MATCH RESULT PROCESSED Authoritatively!" +
                      $"\nMatch ID: {match.matchId}" +
                      $"\nWinner: {match.winnerId}" +
                      $"\nPlayer 1 ({match.player1Name}): {match.p1InitialRating} -> {p1NewRating} (Delta: {(p1Delta >= 0 ? "+" : "")}{p1Delta})" +
                      $"\nPlayer 2 ({match.player2Name}): {match.p2InitialRating} -> {p2NewRating} (Delta: {(p2Delta >= 0 ? "+" : "")}{p2Delta})");

            return response;
        }

        /// <summary>
        /// Handles player forfeit or disconnection by awarding match victory to the remaining player.
        /// </summary>
        public static MatchResultResponse HandleDisconnect(string matchId, string disconnectedPlayerId)
        {
            var match = EloDatabase.GetMatch(matchId);
            if (match == null || match.isProcessed) return null;

            string winnerId = string.Equals(disconnectedPlayerId, match.player1Id, StringComparison.OrdinalIgnoreCase)
                ? match.player2Id
                : match.player1Id;

            var report = new MatchResultReport
            {
                matchId = matchId,
                reportingPlayerId = winnerId,
                winnerId = winnerId,
                isForfeit = true
            };

            return ProcessMatchResult(report);
        }

        private static void PopulateResponseFromExisting(MatchResultResponse res, EloMatchRecord match, string localPlayerId)
        {
            res.matchId = match.matchId;
            res.winnerId = match.winnerId;
            res.loserId = match.loserId;
            res.player1Delta = match.p1RatingDelta;
            res.player2Delta = match.p2RatingDelta;
            res.player1FinalRating = match.p1FinalRating;
            res.player2FinalRating = match.p2FinalRating;

            bool isP1 = string.Equals(localPlayerId, match.player1Id, StringComparison.OrdinalIgnoreCase);
            res.localPlayerDelta = isP1 ? match.p1RatingDelta : match.p2RatingDelta;
            res.localPlayerFinalRating = isP1 ? match.p1FinalRating : match.p2FinalRating;
            res.isLocalWinner = string.Equals(localPlayerId, match.winnerId, StringComparison.OrdinalIgnoreCase);

            var profile = EloDatabase.GetPlayer(localPlayerId);
            if (profile != null)
            {
                res.localPlayerWins = profile.wins;
                res.localPlayerLosses = profile.losses;
                res.localPlayerMatches = profile.matchesPlayed;
            }
        }
    }
}
