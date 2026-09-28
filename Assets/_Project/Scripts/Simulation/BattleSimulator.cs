using System;
using System.Collections.Generic;

namespace DRG
{
    // Plays full games without UI through the real TurnManager -> BattleResolver path (spec §27).
    // Deterministic for a given seed.
    public class BattleSimulator
    {
        // Simulation-only safety limit so a batch always terminates. Not a game rule.
        public const int DefaultTurnCap = 200;

        private readonly GameSettings settings;

        public BattleSimulator(GameSettings settings)
        {
            this.settings = settings;
        }

        public SimulationStats Run(int playerCount, int games, int seed, int turnCap)
        {
            if (playerCount < 2 || playerCount > settings.MaxPlayers)
            {
                throw new ArgumentOutOfRangeException("playerCount", "Player count must be between 2 and " + settings.MaxPlayers + ".");
            }

            SimulationStats stats = new SimulationStats(playerCount, games, seed, turnCap);
            RandomActionPolicy policy = new RandomActionPolicy(seed, settings);

            for (int game = 0; game < games; game++)
            {
                PlayGame(playerCount, turnCap, policy, stats);
            }

            return stats;
        }

        private void PlayGame(int playerCount, int turnCap, RandomActionPolicy policy, SimulationStats stats)
        {
            List<Player> players = new List<Player>();
            for (int seat = 0; seat < playerCount; seat++)
            {
                players.Add(new Player(seat, "P" + seat, settings));
            }

            TurnManager turnManager = new TurnManager(players, settings);
            BattleResult lastResult = null;

            while (!turnManager.IsGameOver && turnManager.TurnNumber < turnCap)
            {
                turnManager.StartTurn();
                turnManager.StartActionSelection();

                for (int i = 0; i < players.Count; i++)
                {
                    Player player = players[i];
                    if (player.State != PlayerState.Alive)
                    {
                        continue;
                    }

                    PlayerAction action = policy.ChooseAction(player, players);
                    if (turnManager.Lock(action) != LockResult.Locked)
                    {
                        throw new InvalidOperationException("Simulation produced an action that TurnManager rejected.");
                    }

                    stats.ActionCounts[(int)action.ActionType]++;
                }

                lastResult = turnManager.StartResolution();
                RecordTurn(lastResult, stats);
                turnManager.EndTurn();
            }

            stats.GameLengths.Add(turnManager.TurnNumber);
            if (!turnManager.IsGameOver)
            {
                stats.UnfinishedGames++;
            }
            else if (lastResult.Outcome == GameOutcome.Winner)
            {
                stats.WinnerGames++;
                stats.WinsBySeat[lastResult.WinnerPlayerId]++;
            }
            else
            {
                stats.DrawGames++;
            }
        }

        private static void RecordTurn(BattleResult result, SimulationStats stats)
        {
            HashSet<int> blockers = new HashSet<int>();
            HashSet<int> teleporters = new HashSet<int>();

            for (int i = 0; i < result.ActionResults.Count; i++)
            {
                ActionResult actionResult = result.ActionResults[i];
                if (actionResult.ResultType == ActionResultType.Failed)
                {
                    stats.FailedActions++;
                    continue;
                }

                stats.AttackResults[(int)actionResult.ActionType, (int)actionResult.ResultType]++;
                if (actionResult.ResultType == ActionResultType.Blocked)
                {
                    blockers.Add(actionResult.TargetPlayerId);
                }
                else if (actionResult.ResultType == ActionResultType.Dodged)
                {
                    teleporters.Add(actionResult.TargetPlayerId);
                }
            }

            stats.UsefulBlocks += blockers.Count;
            stats.UsefulTeleports += teleporters.Count;

            if (result.EliminatedPlayerIds.Count >= 2)
            {
                stats.MultiEliminationTurns++;
            }
        }
    }
}
