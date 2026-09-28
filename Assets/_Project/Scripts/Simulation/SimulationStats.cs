using System;
using System.Collections.Generic;

namespace DRG
{
    // Aggregated measurements for one simulation batch (one player count).
    public class SimulationStats
    {
        public static readonly int ActionTypeCount = Enum.GetValues(typeof(ActionType)).Length;
        public static readonly int ActionResultTypeCount = Enum.GetValues(typeof(ActionResultType)).Length;

        public int PlayerCount;
        public int Games;
        public int Seed;
        public int TurnCap;

        // Turn count of every game, in play order.
        public List<int> GameLengths = new List<int>();

        public int WinnerGames;
        public int DrawGames;
        // Games stopped by the simulation-only TurnCap. This cap is not a game rule.
        public int UnfinishedGames;
        public int[] WinsBySeat;

        // Actions locked by living players, indexed by (int)ActionType.
        public int[] ActionCounts = new int[ActionTypeCount];
        // Attack outcomes, indexed by [(int)ActionType, (int)ActionResultType].
        public int[,] AttackResults = new int[ActionTypeCount, ActionResultTypeCount];

        // Block/Teleport actions that actually stopped at least one attack that turn.
        public int UsefulBlocks;
        public int UsefulTeleports;

        public int FailedActions;
        // Turns in which two or more players were eliminated at once.
        public int MultiEliminationTurns;

        public SimulationStats(int playerCount, int games, int seed, int turnCap)
        {
            PlayerCount = playerCount;
            Games = games;
            Seed = seed;
            TurnCap = turnCap;
            WinsBySeat = new int[playerCount];
        }

        public int TotalTurns
        {
            get
            {
                int total = 0;
                for (int i = 0; i < GameLengths.Count; i++)
                {
                    total += GameLengths[i];
                }

                return total;
            }
        }

        public int TotalActions
        {
            get
            {
                int total = 0;
                for (int i = 0; i < ActionCounts.Length; i++)
                {
                    total += ActionCounts[i];
                }

                return total;
            }
        }
    }
}
