using System;
using System.Collections.Generic;
using UnityEngine;

namespace Clashura.PlayFab
{
    /// <summary>
    /// Automated test suite validating all Section J requirements:
    /// 1. Commander with 1250 points matches with Player B with 1270 points (delta 20 <= 100).
    /// 2. Player D with 900 points does NOT match with Commander/Player B (diff 350+ > 100).
    /// 3. Player D with 900 points matches with Player E with 950 points (delta 50 <= 100).
    /// 4. Six players searching simultaneously (Commander 1250, Player B 1270, Player C 1320, Player D 900, Player E 950, Player F 850).
    /// 5. Proximity prioritization: Closer rating (diff 20) is strictly prioritized over farther rating (diff 70).
    /// 6. Players with no suitable opponent (e.g. 1250 vs 2000, diff 750 > 100) never match.
    /// 7. Two-minute (120s) search duration timeout: ticket transitions to TimedOut, removed from queue.
    /// 8. Player cancelling search: ticket removed, queue decremented.
    /// 9. Disconnect pruning: ticket pruned after heartbeat timeout (>25s).
    /// 10. Standard ELO rating formula (K=32) and PlayFab data security.
    /// </summary>
    public static class PlayFabMatchmakingTestSuite
    {
        public class TestResult
        {
            public string testName;
            public bool passed;
            public string details;
        }

        public static List<TestResult> RunAllTests()
        {
            var results = new List<TestResult>();

            results.Add(Test_CommanderAndPlayerB_Match());
            results.Add(Test_PlayerD_DoesNotMatchCommander_MatchesPlayerE());
            results.Add(Test_SixPlayersSimultaneous_ExactSpecification());
            results.Add(Test_ProximityRule_PrioritizesClosestOpponent());
            results.Add(Test_NoSuitableOpponent_NeverPairsWidelySeparated());
            results.Add(Test_TwoMinuteSearchTimeout());
            results.Add(Test_PlayerCancelsSearch());
            results.Add(Test_DisconnectPruning());
            results.Add(Test_NeverMatchAgainstOwnAccount());
            results.Add(Test_AuthoritativeEloFormula_K32());

            return results;
        }

        public static TestResult Test_CommanderAndPlayerB_Match()
        {
            GlobalMatchmakingService.ResetState();

            // Commander: 1250, Player B: 1270
            var t1 = GlobalMatchmakingService.Enqueue("PF_CMD_01", "Commander", 1250);
            var t2 = GlobalMatchmakingService.Enqueue("PF_PLB_02", "Player B", 1270);

            t1 = GlobalMatchmakingService.Poll(t1.ticketId);
            t2 = GlobalMatchmakingService.Poll(t2.ticketId);

            bool success = t1.status == "Matched" && t2.status == "Matched" &&
                           t1.matchedMatchId == t2.matchedMatchId &&
                           t1.matchedOpponentName == "Player B" &&
                           t2.matchedOpponentName == "Commander" &&
                           Math.Abs(t1.rating - t2.rating) == 20;

            return new TestResult
            {
                testName = "1. Commander (1250) & Player B (1270) Match",
                passed = success,
                details = success 
                    ? $"Matched successfully! Match ID: {t1.matchedMatchId}, Delta: 20 ELO (<= 100 limit)."
                    : $"Failed: t1.status={t1?.status}, t2.status={t2?.status}"
            };
        }

        public static TestResult Test_PlayerD_DoesNotMatchCommander_MatchesPlayerE()
        {
            GlobalMatchmakingService.ResetState();

            // Commander (1250) and Player D (900): diff = 350 > 100 -> Must NOT match
            var tCmd = GlobalMatchmakingService.Enqueue("PF_CMD_01", "Commander", 1250);
            var tD = GlobalMatchmakingService.Enqueue("PF_PLD_01", "Player D", 900);

            tCmd = GlobalMatchmakingService.Poll(tCmd.ticketId);
            tD = GlobalMatchmakingService.Poll(tD.ticketId);

            bool step1Passed = tCmd.status == "Queued" && tD.status == "Queued";

            // Now Player E (950) arrives: diff with Player D is 50 <= 100 -> Must match Player D!
            var tE = GlobalMatchmakingService.Enqueue("PF_PLE_01", "Player E", 950);

            tCmd = GlobalMatchmakingService.Poll(tCmd.ticketId);
            tD = GlobalMatchmakingService.Poll(tD.ticketId);
            tE = GlobalMatchmakingService.Poll(tE.ticketId);

            bool step2Passed = tD.status == "Matched" && tE.status == "Matched" &&
                               tD.matchedOpponentName == "Player E" &&
                               tE.matchedOpponentName == "Player D" &&
                               tCmd.status == "Queued"; // Commander remains queued, never paired with 900 player

            bool success = step1Passed && step2Passed;

            return new TestResult
            {
                testName = "2. 900 Player Never Pairs with 1250, Pairs with 950 Player",
                passed = success,
                details = success
                    ? "Verified! 900-rated player safely avoided 1250 Commander, matched with 950 Player E (delta 50)."
                    : $"Failed: step1Passed={step1Passed}, step2Passed={step2Passed}, tCmd={tCmd?.status}, tD={tD?.status}, tE={tE?.status}"
            };
        }

        public static TestResult Test_SixPlayersSimultaneous_ExactSpecification()
        {
            GlobalMatchmakingService.ResetState();

            // Specification:
            // Commander: 1250, Player B: 1270, Player C: 1320, Player D: 900, Player E: 950, Player F: 850
            var tA = GlobalMatchmakingService.Enqueue("PF_01", "Commander", 1250);
            var tB = GlobalMatchmakingService.Enqueue("PF_02", "Player B", 1270);
            var tC = GlobalMatchmakingService.Enqueue("PF_03", "Player C", 1320);
            var tD = GlobalMatchmakingService.Enqueue("PF_04", "Player D", 900);
            var tE = GlobalMatchmakingService.Enqueue("PF_05", "Player E", 950);
            var tF = GlobalMatchmakingService.Enqueue("PF_06", "Player F", 850);

            tA = GlobalMatchmakingService.Poll(tA.ticketId);
            tB = GlobalMatchmakingService.Poll(tB.ticketId);
            tC = GlobalMatchmakingService.Poll(tC.ticketId);
            tD = GlobalMatchmakingService.Poll(tD.ticketId);
            tE = GlobalMatchmakingService.Poll(tE.ticketId);
            tF = GlobalMatchmakingService.Poll(tF.ticketId);

            var tickets = new[] { tA, tB, tC, tD, tE, tF };
            var matches = new HashSet<string>();

            int matchedCount = 0;
            int queuedCount = 0;

            foreach (var t in tickets)
            {
                if (t.status == "Matched")
                {
                    matchedCount++;
                    matches.Add(t.matchedMatchId);
                }
                else if (t.status == "Queued")
                {
                    queuedCount++;
                }
            }

            // High group: Commander 1250 & Player B 1270 (delta 20) match. Player C 1320 remains queued.
            // Low group: Player D 900 & Player E 950 (delta 50) or Player F 850 (delta 50) match. Remaining remains queued.
            // Exactly 4 players match into 2 distinct matches; 2 players remain queued!
            bool success = (matchedCount == 4 && queuedCount == 2 && matches.Count == 2);

            return new TestResult
            {
                testName = "3. Six Simultaneous Players (Optimal Proximity Pairs)",
                passed = success,
                details = success
                    ? $"Exact match! 4 players paired into 2 matches based on rating proximity; 2 players safely remain queued. Distinct matches: {matches.Count}."
                    : $"Failed: matched={matchedCount}, queued={queuedCount}, distinctMatches={matches.Count}"
            };
        }

        public static TestResult Test_ProximityRule_PrioritizesClosestOpponent()
        {
            GlobalMatchmakingService.ResetState();

            // Commander (1250) has Player B (1270, diff 20) and Player C (1320, diff 70).
            // Both are within 100 limit, but Player B (1270) MUST be chosen because 20 is closer!
            var tCmd = GlobalMatchmakingService.Enqueue("PF_CMD", "Commander", 1250);
            var tB = GlobalMatchmakingService.Enqueue("PF_PLB", "Player B", 1270);
            var tC = GlobalMatchmakingService.Enqueue("PF_PLC", "Player C", 1320);

            tCmd = GlobalMatchmakingService.Poll(tCmd.ticketId);
            tB = GlobalMatchmakingService.Poll(tB.ticketId);
            tC = GlobalMatchmakingService.Poll(tC.ticketId);

            bool success = tCmd.status == "Matched" && tB.status == "Matched" &&
                           tCmd.matchedOpponentName == "Player B" &&
                           tC.status == "Queued"; // Player C left in queue because Player B was closer

            return new TestResult
            {
                testName = "4. Proximity Priority (Closest Group Chosen First)",
                passed = success,
                details = success
                    ? "Verified! Commander (1250) paired with Player B (1270, delta 20) instead of Player C (1320, delta 70)."
                    : $"Failed: tCmd opponent='{tCmd?.matchedOpponentName}', tC status='{tC?.status}'"
            };
        }

        public static TestResult Test_NoSuitableOpponent_NeverPairsWidelySeparated()
        {
            GlobalMatchmakingService.ResetState();

            // Player 1: 1250, Player 2: 2000 (diff = 750 >> 100)
            var t1 = GlobalMatchmakingService.Enqueue("PF_P1", "Commander", 1250);
            var t2 = GlobalMatchmakingService.Enqueue("PF_P2", "Grandmaster", 2000);

            t1 = GlobalMatchmakingService.Poll(t1.ticketId);
            t2 = GlobalMatchmakingService.Poll(t2.ticketId);

            bool success = t1.status == "Queued" && t2.status == "Queued";

            return new TestResult
            {
                testName = "5. No Suitable Opponent (Never Match Widely Separated Ratings)",
                passed = success,
                details = success
                    ? "Verified! 1250 and 2000 rated players never matched (delta 750 > 100). Both stay in queue."
                    : $"Failed: t1={t1?.status}, t2={t2?.status}"
            };
        }

        public static TestResult Test_TwoMinuteSearchTimeout()
        {
            GlobalMatchmakingService.ResetState();

            // Player searches alone
            var t1 = GlobalMatchmakingService.Enqueue("PF_ALONE", "LonelyWarrior", 1250);

            // Simulate 121 seconds elapsed (exceeds 120s limit)
            t1.searchStartTime -= 121;
            t1 = GlobalMatchmakingService.Poll(t1.ticketId);

            bool timedOutInPoll = t1.status == "TimedOut";
            bool removedFromQueue = GlobalMatchmakingService.GetQueuedCount() == 0;
            string msg = PlayFabConfig.MsgTimeout;

            bool success = timedOutInPoll && removedFromQueue && 
                           msg == "Sorry, no opponent is currently available. Please try again.";

            return new TestResult
            {
                testName = "6. Two-Minute Search Timeout (120s Removal & Message)",
                passed = success,
                details = success
                    ? $"Verified! Player automatically removed after 120s with message: '{msg}'"
                    : $"Failed: timedOutInPoll={timedOutInPoll}, queueCount={GlobalMatchmakingService.GetQueuedCount()}"
            };
        }

        public static TestResult Test_PlayerCancelsSearch()
        {
            GlobalMatchmakingService.ResetState();

            var t1 = GlobalMatchmakingService.Enqueue("PF_CANCEL_01", "Commander", 1250);
            int countBefore = GlobalMatchmakingService.GetQueuedCount();

            bool canceled = GlobalMatchmakingService.Cancel(t1.ticketId);
            int countAfter = GlobalMatchmakingService.GetQueuedCount();

            // Subsequent player should NOT match with canceled ticket
            var t2 = GlobalMatchmakingService.Enqueue("PF_CANCEL_02", "Player B", 1270);
            t2 = GlobalMatchmakingService.Poll(t2.ticketId);

            bool success = canceled && countBefore == 1 && countAfter == 0 && t2.status == "Queued";

            return new TestResult
            {
                testName = "7. Player Cancels Search (Immediate Queue Withdrawal)",
                passed = success,
                details = success
                    ? "Verified! Player canceled search, queue decremented to 0, subsequent player remains queued."
                    : $"Failed: canceled={canceled}, countBefore={countBefore}, countAfter={countAfter}, t2.status={t2?.status}"
            };
        }

        public static TestResult Test_DisconnectPruning()
        {
            GlobalMatchmakingService.ResetState();

            var t1 = GlobalMatchmakingService.Enqueue("PF_DISC_01", "GhostDevice", 1250);
            // Simulate lost heartbeat (>25s)
            t1.lastHeartbeatTime -= 30;

            var t2 = GlobalMatchmakingService.Enqueue("PF_DISC_02", "ActiveDevice", 1250);
            t2 = GlobalMatchmakingService.Poll(t2.ticketId);

            bool success = t2.status == "Queued" && GlobalMatchmakingService.GetQueuedCount() == 1;

            return new TestResult
            {
                testName = "8. Disconnect Pruning (Heartbeat Timeout)",
                passed = success,
                details = success
                    ? "Verified! Disconnected device was pruned safely; active device remains waiting."
                    : $"Failed: t2.status={t2?.status}, queueCount={GlobalMatchmakingService.GetQueuedCount()}"
            };
        }

        public static TestResult Test_NeverMatchAgainstOwnAccount()
        {
            GlobalMatchmakingService.ResetState();

            // Same player account submits twice
            var t1 = GlobalMatchmakingService.Enqueue("PF_SAME_ID", "Commander", 1250);
            var t2 = GlobalMatchmakingService.Enqueue("PF_SAME_ID", "Commander", 1250);

            // Must return identical ticket without matching against self
            t1 = GlobalMatchmakingService.Poll(t1.ticketId);

            bool success = t1.ticketId == t2.ticketId && t1.status == "Queued" && GlobalMatchmakingService.GetQueuedCount() == 1;

            return new TestResult
            {
                testName = "9. Never Match Against Own Account",
                passed = success,
                details = success
                    ? "Verified! Single active ticket per player; player cannot match against own account."
                    : $"Failed: t1.status={t1?.status}, queueCount={GlobalMatchmakingService.GetQueuedCount()}"
            };
        }

        public static TestResult Test_AuthoritativeEloFormula_K32()
        {
            GlobalMatchmakingService.ResetState();

            // Commander: 1250, Player B: 1270
            // If Commander wins against higher-rated Player B:
            // E_Commander = 1 / (1 + 10^((1270 - 1250)/400)) = 0.4712
            // Delta = 32 * (1 - 0.4712) = +17 ELO.
            // Commander new rating: 1267 ELO. Player B new rating: 1253 ELO.
            var tA = GlobalMatchmakingService.Enqueue("PF_ELO_CMD", "Commander", 1250);
            var tB = GlobalMatchmakingService.Enqueue("PF_ELO_PLB", "Player B", 1270);

            tA = GlobalMatchmakingService.Poll(tA.ticketId);
            tB = GlobalMatchmakingService.Poll(tB.ticketId);

            var report = new MatchResultReportDto
            {
                matchId = tA.matchedMatchId,
                winnerPlayerId = "PF_ELO_CMD",
                loserPlayerId = "PF_ELO_PLB"
            };

            var res = GlobalMatchmakingService.CompleteMatch(report);

            bool success = res.isVerified &&
                           res.winnerDelta == 17 &&
                           res.winnerNewRating == 1267 &&
                           res.loserDelta == -17 &&
                           res.loserNewRating == 1253;

            return new TestResult
            {
                testName = "10. Standard ELO Formula Verification (K=32)",
                passed = success,
                details = success
                    ? $"Exact match! Commander (1250) defeated Player B (1270): +17 -> 1267 ELO. Loser: -17 -> 1253 ELO."
                    : $"Failed: winnerDelta={res?.winnerDelta}, winnerNew={res?.winnerNewRating}, loserDelta={res?.loserDelta}, loserNew={res?.loserNewRating}"
            };
        }
    }
}
