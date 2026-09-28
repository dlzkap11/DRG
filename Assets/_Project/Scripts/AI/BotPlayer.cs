using System.Collections.Generic;

namespace DRG
{
    // A bot seat. It produces the same PlayerAction a human would and locks through the same TurnManager path.
    // Decisions come from BotAI (DEC-008).
    public class BotPlayer
    {
        private readonly BotAI ai;

        public int PlayerId { get; private set; }

        public BotPlayer(int playerId, BotAI ai)
        {
            PlayerId = playerId;
            this.ai = ai;
        }

        public PlayerAction ChooseAction(List<Player> players)
        {
            for (int i = 0; i < players.Count; i++)
            {
                if (players[i].PlayerId == PlayerId)
                {
                    return ai.ChooseAction(players[i], players);
                }
            }

            return null;
        }
    }
}
