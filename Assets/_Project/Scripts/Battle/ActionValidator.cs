using System;
using System.Collections.Generic;

namespace DRG
{
    // Checks whether a living player's action is legal against the current player states.
    // UI may pre-filter choices, but game logic must always validate here again.
    public class ActionValidator
    {
        private readonly GameSettings settings;

        public ActionValidator(GameSettings settings)
        {
            this.settings = settings;
        }

        public ActionValidationResult Validate(PlayerAction action, List<Player> players)
        {
            if (action == null)
            {
                return ActionValidationResult.MissingAction;
            }

            Player actor = FindPlayer(players, action.PlayerId);
            if (actor == null)
            {
                return ActionValidationResult.PlayerNotFound;
            }

            if (actor.State != PlayerState.Alive)
            {
                return ActionValidationResult.PlayerEliminated;
            }

            // DEC-001: None is reserved for eliminated-player auto-lock; living players must choose an action.
            if (action.ActionType == ActionType.None || !Enum.IsDefined(typeof(ActionType), action.ActionType))
            {
                return ActionValidationResult.InvalidActionType;
            }

            if (action.ActionType == ActionType.Gather && actor.Ki >= actor.MaxKi)
            {
                return ActionValidationResult.KiAtMax;
            }

            if (actor.Ki < GetKiCost(action.ActionType))
            {
                return ActionValidationResult.InsufficientKi;
            }

            if (IsAttack(action.ActionType))
            {
                if (action.TargetPlayerId == actor.PlayerId)
                {
                    return ActionValidationResult.SelfTarget;
                }

                Player target = FindPlayer(players, action.TargetPlayerId);
                if (target == null)
                {
                    return ActionValidationResult.TargetNotFound;
                }

                if (target.State != PlayerState.Alive)
                {
                    return ActionValidationResult.TargetEliminated;
                }
            }
            else if (action.TargetPlayerId != PlayerAction.NoTarget)
            {
                return ActionValidationResult.UnexpectedTarget;
            }

            return ActionValidationResult.Valid;
        }

        // Ki spent by an action. Gather gains Ki and Block is free, so both cost 0.
        public int GetKiCost(ActionType actionType)
        {
            switch (actionType)
            {
                case ActionType.EnergyWave:
                    return settings.EnergyWaveKiCost;
                case ActionType.Teleport:
                    return settings.TeleportKiCost;
                case ActionType.SpiritBomb:
                    return settings.SpiritBombKiCost;
                default:
                    return 0;
            }
        }

        private static bool IsAttack(ActionType actionType)
        {
            return actionType == ActionType.EnergyWave || actionType == ActionType.SpiritBomb;
        }

        private static Player FindPlayer(List<Player> players, int playerId)
        {
            for (int i = 0; i < players.Count; i++)
            {
                if (players[i].PlayerId == playerId)
                {
                    return players[i];
                }
            }

            return null;
        }
    }
}
