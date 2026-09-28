using System;
using System.Collections.Generic;

namespace DRG
{
    // First rule-based bot (design doc §12, spec §20; tuning values from DEC-008).
    //   1. Low HP (HP <= LowHpThreshold): with DefenseChanceAtLowHp, defend.
    //      Block or Teleport 50/50 when Ki allows Teleport, otherwise Block.
    //   2. Otherwise by Ki: 0 -> Gather, 1..2 -> Energy Wave / Gather 50/50,
    //      MaxKi -> Spirit Bomb / Energy Wave 50/50.
    //   3. Attack target: uniform among living opponents.
    // Produces an ordinary PlayerAction; the same Lock validation applies as for a human.
    // Randomness is seeded so games and tests are reproducible.
    public class BotAI
    {
        public const int LowHpThreshold = 1;
        public const double DefenseChanceAtLowHp = 0.4;

        private readonly Random random;
        private readonly ActionValidator validator;

        public BotAI(int seed, GameSettings settings)
        {
            random = new Random(seed);
            validator = new ActionValidator(settings);
        }

        public PlayerAction ChooseAction(Player self, List<Player> players)
        {
            ActionType choice = ChooseActionType(self);
            PlayerAction action = CreateAction(self, choice, players);

            // Safety net: the rules above only pick legal actions, but never hand an illegal one to Lock.
            if (action == null || validator.Validate(action, players) != ActionValidationResult.Valid)
            {
                action = new PlayerAction { PlayerId = self.PlayerId, ActionType = ActionType.Block, TargetPlayerId = PlayerAction.NoTarget };
            }

            return action;
        }

        private ActionType ChooseActionType(Player self)
        {
            if (self.HP <= LowHpThreshold && random.NextDouble() < DefenseChanceAtLowHp)
            {
                bool canTeleport = self.Ki >= validator.GetKiCost(ActionType.Teleport);
                if (canTeleport && random.Next(2) == 0)
                {
                    return ActionType.Teleport;
                }

                return ActionType.Block;
            }

            if (self.Ki <= 0)
            {
                return ActionType.Gather;
            }

            if (self.Ki >= self.MaxKi)
            {
                return random.Next(2) == 0 ? ActionType.SpiritBomb : ActionType.EnergyWave;
            }

            return random.Next(2) == 0 ? ActionType.EnergyWave : ActionType.Gather;
        }

        private PlayerAction CreateAction(Player self, ActionType type, List<Player> players)
        {
            int target = PlayerAction.NoTarget;
            if (type == ActionType.EnergyWave || type == ActionType.SpiritBomb)
            {
                List<int> opponents = new List<int>();
                for (int i = 0; i < players.Count; i++)
                {
                    if (players[i].PlayerId != self.PlayerId && players[i].State == PlayerState.Alive)
                    {
                        opponents.Add(players[i].PlayerId);
                    }
                }

                if (opponents.Count == 0)
                {
                    return null;
                }

                target = opponents[random.Next(opponents.Count)];
            }

            return new PlayerAction { PlayerId = self.PlayerId, ActionType = type, TargetPlayerId = target };
        }
    }
}
