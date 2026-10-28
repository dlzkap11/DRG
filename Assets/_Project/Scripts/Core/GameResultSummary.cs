using System.Collections.Generic;

namespace DRG
{
    public class PlayerResult
    {
        public const int NotEliminated = -1;

        public int PlayerId;
        public string Nickname;
        public bool IsHuman;
        public int Rank;
        // Turn in which the player was eliminated, or NotEliminated for the survivor.
        public int EliminatedTurn = NotEliminated;
    }

    // What the Result screen shows (spec §22, DEC-010): outcome, winner, turn count, and a ranking
    // by elimination order. Survivors rank first; later elimination ranks higher; players eliminated
    // in the same turn share a rank (1, 2, 2, 4 ...). In a Draw the last players standing share rank 1.
    public class GameResultSummary
    {
        public GameOutcome Outcome;
        public int WinnerPlayerId = BattleResult.NoWinner;
        public int Turns;
        // Sorted by rank, then by PlayerId for a stable order.
        public List<PlayerResult> Players = new List<PlayerResult>();

        public static GameResultSummary FromSession(LocalBattleSession session)
        {
            List<PlayerResult> players = new List<PlayerResult>();
            IReadOnlyList<Player> sessionPlayers = session.Players;
            for (int i = 0; i < sessionPlayers.Count; i++)
            {
                players.Add(new PlayerResult
                {
                    PlayerId = sessionPlayers[i].PlayerId,
                    Nickname = sessionPlayers[i].Nickname,
                    IsHuman = sessionPlayers[i].PlayerId == LocalBattleSession.HumanPlayerId,
                    EliminatedTurn = session.GetEliminationTurn(sessionPlayers[i].PlayerId)
                });
            }

            GameOutcome outcome = session.LastResult != null ? session.LastResult.Outcome : GameOutcome.Ongoing;
            int winner = session.LastResult != null ? session.LastResult.WinnerPlayerId : BattleResult.NoWinner;
            return Create(outcome, winner, session.TurnNumber, players);
        }

        // Sorts the players into ranking order and assigns ranks.
        public static GameResultSummary Create(GameOutcome outcome, int winnerPlayerId, int turns, List<PlayerResult> players)
        {
            GameResultSummary summary = new GameResultSummary();
            summary.Outcome = outcome;
            summary.WinnerPlayerId = winnerPlayerId;
            summary.Turns = turns;
            summary.Players = new List<PlayerResult>(players);
            AssignRanks(summary.Players);
            return summary;
        }

        private static void AssignRanks(List<PlayerResult> results)
        {
            results.Sort(CompareByStanding);
            for (int i = 0; i < results.Count; i++)
            {
                bool tiedWithPrevious = i > 0 && results[i].EliminatedTurn == results[i - 1].EliminatedTurn;
                results[i].Rank = tiedWithPrevious ? results[i - 1].Rank : i + 1;
            }
        }

        // Survivors first, then by elimination turn (later is better), then by PlayerId.
        private static int CompareByStanding(PlayerResult a, PlayerResult b)
        {
            int aKey = a.EliminatedTurn == PlayerResult.NotEliminated ? int.MaxValue : a.EliminatedTurn;
            int bKey = b.EliminatedTurn == PlayerResult.NotEliminated ? int.MaxValue : b.EliminatedTurn;
            if (aKey != bKey)
            {
                return bKey.CompareTo(aKey);
            }

            return a.PlayerId.CompareTo(b.PlayerId);
        }
    }
}
