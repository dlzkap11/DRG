using System.Collections.Generic;

namespace DRG
{
    // Anything that picks a living player's action for the simulator: the uniform RandomActionPolicy baseline or BotAI.
    public interface IActionPolicy
    {
        PlayerAction ChooseAction(Player self, List<Player> players);
    }
}
