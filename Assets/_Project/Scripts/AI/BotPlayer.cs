using System.Collections.Generic;

namespace DRG
{
    // A bot seat. It produces the same PlayerAction a human would and locks through the same TurnManager path.
    // Until BotAI is built (M3), decisions come from the uniform RandomActionPolicy.
    public class BotPlayer
    {
        private readonly RandomActionPolicy policy;

        public int PlayerId { get; private set; }

        public BotPlayer(int playerId, RandomActionPolicy policy)
        {
            PlayerId = playerId;
            this.policy = policy;
        }

        public PlayerAction ChooseAction(List<Player> players)
        {
            for (int i = 0; i < players.Count; i++)
            {
                if (players[i].PlayerId == PlayerId)
                {
                    return policy.ChooseAction(players[i], players);
                }
            }

            return null;
        }
    }
}
