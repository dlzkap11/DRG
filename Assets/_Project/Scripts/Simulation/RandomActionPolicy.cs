using System;
using System.Collections.Generic;

namespace DRG
{
    // Simulation-only baseline player: picks uniformly among the action types that are legal right now,
    // then uniformly among valid targets for attacks. It makes no strategic decisions (BotAI is separate, M3).
    public class RandomActionPolicy
    {
        private static readonly ActionType[] ChoosableActions =
        {
            ActionType.Gather,
            ActionType.EnergyWave,
            ActionType.Block,
            ActionType.Teleport,
            ActionType.SpiritBomb
        };

        private readonly Random random;
        private readonly ActionValidator validator;

        public RandomActionPolicy(int seed, GameSettings settings)
        {
            random = new Random(seed);
            validator = new ActionValidator(settings);
        }

        public PlayerAction ChooseAction(Player self, List<Player> players)
        {
            List<ActionType> legalTypes = new List<ActionType>();
            for (int i = 0; i < ChoosableActions.Length; i++)
            {
                ActionType type = ChoosableActions[i];
                if (IsAttack(type))
                {
                    if (GetValidTargets(self, type, players).Count > 0)
                    {
                        legalTypes.Add(type);
                    }
                }
                else if (validator.Validate(CreateAction(self, type, PlayerAction.NoTarget), players) == ActionValidationResult.Valid)
                {
                    legalTypes.Add(type);
                }
            }

            // Block is always legal for a living player, so legalTypes is never empty.
            ActionType chosen = legalTypes[random.Next(legalTypes.Count)];
            if (!IsAttack(chosen))
            {
                return CreateAction(self, chosen, PlayerAction.NoTarget);
            }

            List<int> targets = GetValidTargets(self, chosen, players);
            return CreateAction(self, chosen, targets[random.Next(targets.Count)]);
        }

        private List<int> GetValidTargets(Player self, ActionType type, List<Player> players)
        {
            List<int> targets = new List<int>();
            for (int i = 0; i < players.Count; i++)
            {
                PlayerAction candidate = CreateAction(self, type, players[i].PlayerId);
                if (validator.Validate(candidate, players) == ActionValidationResult.Valid)
                {
                    targets.Add(players[i].PlayerId);
                }
            }

            return targets;
        }

        private static PlayerAction CreateAction(Player self, ActionType type, int targetPlayerId)
        {
            return new PlayerAction { PlayerId = self.PlayerId, ActionType = type, TargetPlayerId = targetPlayerId };
        }

        private static bool IsAttack(ActionType type)
        {
            return type == ActionType.EnergyWave || type == ActionType.SpiritBomb;
        }
    }
}
