using System.Collections.Generic;

namespace DRG
{
    // Turns an already-resolved BattleResult into readable log lines. It does not decide any outcome.
    public static class BattleLogFormatter
    {
        public static string ActionName(ActionType actionType)
        {
            switch (actionType)
            {
                case ActionType.Gather:
                    return "Gather";
                case ActionType.EnergyWave:
                    return "Energy Wave";
                case ActionType.Block:
                    return "Block";
                case ActionType.Teleport:
                    return "Teleport";
                case ActionType.SpiritBomb:
                    return "Spirit Bomb";
                default:
                    return "-";
            }
        }

        // Call after resolution, while each player's CurrentAction still holds this turn's locked action.
        public static List<string> FormatTurn(int turnNumber, IReadOnlyList<Player> players, BattleResult result)
        {
            List<string> lines = new List<string>();
            lines.Add("-- Turn " + turnNumber + " --");

            for (int i = 0; i < players.Count; i++)
            {
                PlayerAction action = players[i].CurrentAction;
                if (action == null || action.ActionType == ActionType.None)
                {
                    continue;
                }

                string line = players[i].Nickname + ": " + ActionName(action.ActionType);
                if (action.TargetPlayerId != PlayerAction.NoTarget)
                {
                    line += " -> " + NameOf(players, action.TargetPlayerId);
                }

                lines.Add(line);
            }

            AppendOutcomes(players, result, lines);
            return lines;
        }

        // Only what the resolution decided: attack outcomes, failed actions, eliminations, and the game result.
        // Used by the compact reveal panel; each player's chosen action is shown on their card instead.
        public static List<string> FormatOutcomes(IReadOnlyList<Player> players, BattleResult result)
        {
            List<string> lines = new List<string>();
            AppendOutcomes(players, result, lines);
            if (lines.Count == 0)
            {
                lines.Add("No attacks this turn.");
            }

            return lines;
        }

        private static void AppendOutcomes(IReadOnlyList<Player> players, BattleResult result, List<string> lines)
        {
            for (int i = 0; i < result.ActionResults.Count; i++)
            {
                ActionResult actionResult = result.ActionResults[i];
                string owner = NameOf(players, actionResult.PlayerId);
                if (actionResult.ResultType == ActionResultType.Failed)
                {
                    lines.Add(owner + ": action failed");
                    continue;
                }

                lines.Add(ActionName(actionResult.ActionType) + " " + owner + " -> "
                    + NameOf(players, actionResult.TargetPlayerId) + ": " + actionResult.ResultType);
            }

            for (int i = 0; i < result.EliminatedPlayerIds.Count; i++)
            {
                lines.Add("Eliminated: " + NameOf(players, result.EliminatedPlayerIds[i]));
            }

            // "Label: name" keeps the wording correct for both "You" and bot names.
            if (result.Outcome == GameOutcome.Winner)
            {
                lines.Add("Winner: " + NameOf(players, result.WinnerPlayerId));
            }
            else if (result.Outcome == GameOutcome.Draw)
            {
                lines.Add("Draw: no survivors.");
            }
        }

        private static string NameOf(IReadOnlyList<Player> players, int playerId)
        {
            for (int i = 0; i < players.Count; i++)
            {
                if (players[i].PlayerId == playerId)
                {
                    return players[i].Nickname;
                }
            }

            return "Player " + playerId;
        }
    }
}
