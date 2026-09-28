using System;
using System.Collections.Generic;

namespace DRG
{
    // Controls turn flow and action locking:
    // TurnStart -> ActionSelection -> ActionResolution -> TurnEnd -> next TurnStart.
    // Before the first turn, State is TurnEnd and TurnNumber is 0.
    public class TurnManager
    {
        private readonly List<Player> players;
        private readonly ActionValidator validator;
        private readonly HashSet<int> lockedPlayerIds = new HashSet<int>();

        public TurnState State { get; private set; }
        public int TurnNumber { get; private set; }

        public TurnManager(List<Player> players, GameSettings settings)
        {
            this.players = players;
            validator = new ActionValidator(settings);
            State = TurnState.TurnEnd;
            TurnNumber = 0;
        }

        public void StartTurn()
        {
            RequireState(TurnState.TurnEnd);

            TurnNumber++;
            lockedPlayerIds.Clear();
            for (int i = 0; i < players.Count; i++)
            {
                players[i].CurrentAction = null;
            }

            State = TurnState.TurnStart;
        }

        public void StartActionSelection()
        {
            RequireState(TurnState.TurnStart);

            // Eliminated players do not act; they are treated as locked with ActionType.None (DEC-001).
            for (int i = 0; i < players.Count; i++)
            {
                Player player = players[i];
                if (player.State == PlayerState.Eliminated)
                {
                    player.CurrentAction = new PlayerAction
                    {
                        PlayerId = player.PlayerId,
                        ActionType = ActionType.None,
                        TargetPlayerId = PlayerAction.NoTarget
                    };
                    lockedPlayerIds.Add(player.PlayerId);
                }
            }

            State = TurnState.ActionSelection;
        }

        // Commits an action for this turn. A locked action cannot be changed until the next turn.
        public LockResult Lock(PlayerAction action)
        {
            if (State != TurnState.ActionSelection)
            {
                return LockResult.WrongTurnState;
            }

            if (action != null && lockedPlayerIds.Contains(action.PlayerId))
            {
                return LockResult.AlreadyLocked;
            }

            if (validator.Validate(action, players) != ActionValidationResult.Valid)
            {
                return LockResult.InvalidAction;
            }

            // Store a copy so later changes to the caller's object cannot alter the committed action.
            Player player = FindPlayer(action.PlayerId);
            player.CurrentAction = new PlayerAction
            {
                PlayerId = action.PlayerId,
                ActionType = action.ActionType,
                TargetPlayerId = action.TargetPlayerId
            };
            lockedPlayerIds.Add(action.PlayerId);
            return LockResult.Locked;
        }

        public bool IsLocked(int playerId)
        {
            return lockedPlayerIds.Contains(playerId);
        }

        public bool AreAllPlayersLocked()
        {
            for (int i = 0; i < players.Count; i++)
            {
                if (!lockedPlayerIds.Contains(players[i].PlayerId))
                {
                    return false;
                }
            }

            return true;
        }

        public void StartResolution()
        {
            RequireState(TurnState.ActionSelection);
            if (!AreAllPlayersLocked())
            {
                throw new InvalidOperationException("Cannot start resolution before every player is locked.");
            }

            State = TurnState.ActionResolution;
        }

        public void EndTurn()
        {
            RequireState(TurnState.ActionResolution);
            State = TurnState.TurnEnd;
        }

        private void RequireState(TurnState expected)
        {
            if (State != expected)
            {
                throw new InvalidOperationException("Expected turn state " + expected + " but was " + State + ".");
            }
        }

        private Player FindPlayer(int playerId)
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
