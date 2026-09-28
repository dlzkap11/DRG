using System;
using System.Collections.Generic;

namespace DRG
{
    // One local game: a single human (PlayerId 0) plus bots. Human and bots act through the same
    // PlayerAction -> TurnManager.Lock path. UI reads from and requests actions through this class only;
    // it must never change Player state directly.
    public class LocalBattleSession
    {
        public const int HumanPlayerId = 0;

        private readonly List<Player> players = new List<Player>();
        private readonly List<BotPlayer> bots = new List<BotPlayer>();
        private readonly TurnManager turnManager;
        private readonly ActionValidator validator;

        public BattleResult LastResult { get; private set; }

        public LocalBattleSession(GameSettings settings, int botCount, int seed)
        {
            if (botCount < 1 || botCount > settings.MaxPlayers - 1)
            {
                throw new ArgumentOutOfRangeException("botCount", "Bot count must be between 1 and " + (settings.MaxPlayers - 1) + ".");
            }

            players.Add(new Player(HumanPlayerId, "You", settings));
            for (int i = 1; i <= botCount; i++)
            {
                players.Add(new Player(i, "Bot " + i, settings));
                bots.Add(new BotPlayer(i, new RandomActionPolicy(seed + i, settings)));
            }

            turnManager = new TurnManager(players, settings);
            validator = new ActionValidator(settings);
        }

        public IReadOnlyList<Player> Players
        {
            get { return players; }
        }

        public Player Human
        {
            get { return players[0]; }
        }

        public int TurnNumber
        {
            get { return turnManager.TurnNumber; }
        }

        public bool IsGameOver
        {
            get { return turnManager.IsGameOver; }
        }

        // True from resolution until the next turn starts: every locked action is public.
        public bool IsRevealed
        {
            get { return turnManager.State == TurnState.ActionResolution; }
        }

        public bool IsAwaitingHumanAction
        {
            get { return turnManager.State == TurnState.ActionSelection && !turnManager.IsLocked(HumanPlayerId); }
        }

        public bool IsLocked(int playerId)
        {
            return turnManager.IsLocked(playerId);
        }

        // Ends the revealed turn (if any), starts the next one, and locks every living bot's action.
        public void StartNextTurn()
        {
            if (turnManager.State == TurnState.ActionResolution)
            {
                turnManager.EndTurn();
            }

            turnManager.StartTurn();
            turnManager.StartActionSelection();
            LastResult = null;

            for (int i = 0; i < bots.Count; i++)
            {
                Player bot = FindPlayer(bots[i].PlayerId);
                if (bot.State != PlayerState.Alive)
                {
                    continue;
                }

                if (turnManager.Lock(bots[i].ChooseAction(players)) != LockResult.Locked)
                {
                    throw new InvalidOperationException("Bot " + bots[i].PlayerId + " produced an action that TurnManager rejected.");
                }
            }

            // If the human is already eliminated, everyone is locked now and the turn resolves at once.
            ResolveIfAllLocked();
        }

        // Lets the UI enable only choices that game logic would accept. Lock still validates again.
        public ActionValidationResult CheckHumanAction(ActionType actionType, int targetPlayerId)
        {
            return validator.Validate(CreateHumanAction(actionType, targetPlayerId), players);
        }

        public LockResult SubmitHumanAction(ActionType actionType, int targetPlayerId)
        {
            LockResult result = turnManager.Lock(CreateHumanAction(actionType, targetPlayerId));
            if (result == LockResult.Locked)
            {
                ResolveIfAllLocked();
            }

            return result;
        }

        // Opponent actions stay hidden until the reveal. The human always sees their own locked action.
        // Returns a copy so callers cannot alter the committed action.
        public PlayerAction GetVisibleAction(int playerId)
        {
            Player player = FindPlayer(playerId);
            if (player == null || player.CurrentAction == null)
            {
                return null;
            }

            if (playerId != HumanPlayerId && !IsRevealed)
            {
                return null;
            }

            return new PlayerAction
            {
                PlayerId = player.CurrentAction.PlayerId,
                ActionType = player.CurrentAction.ActionType,
                TargetPlayerId = player.CurrentAction.TargetPlayerId
            };
        }

        private void ResolveIfAllLocked()
        {
            if (turnManager.AreAllPlayersLocked())
            {
                LastResult = turnManager.StartResolution();
            }
        }

        private static PlayerAction CreateHumanAction(ActionType actionType, int targetPlayerId)
        {
            return new PlayerAction { PlayerId = HumanPlayerId, ActionType = actionType, TargetPlayerId = targetPlayerId };
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
