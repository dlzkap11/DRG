using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace DRG
{
    // Binds the BattleScreen UXML to a LocalBattleSession.
    // Holds only the human's pending (not yet locked) selection; all game state comes from the session.
    public class BattleView
    {
        private static readonly ActionType[] ActionOrder =
        {
            ActionType.Gather,
            ActionType.EnergyWave,
            ActionType.Block,
            ActionType.Teleport,
            ActionType.SpiritBomb
        };

        private readonly Label turnLabel;
        private readonly Label phaseLabel;
        private readonly VisualElement playerList;
        private readonly VisualElement actionPanel;
        private readonly VisualElement targetPanel;
        private readonly VisualElement targetButtons;
        private readonly VisualElement lockRow;
        private readonly Label selectionLabel;
        private readonly Button lockButton;
        private readonly VisualElement resultPanel;
        private readonly Label resultLabel;
        private readonly Button nextTurnButton;
        private readonly VisualElement gameOverPanel;
        private readonly Label gameOverLabel;
        private readonly ScrollView battleLog;
        private readonly Dictionary<ActionType, Button> actionButtons = new Dictionary<ActionType, Button>();

        private LocalBattleSession session;
        private ActionType pendingAction = ActionType.None;
        private int pendingTarget = PlayerAction.NoTarget;
        private string selectionMessage;
        private int lastLoggedTurn;

        public event Action RestartRequested;

        public BattleView(VisualElement root)
        {
            turnLabel = Require<Label>(root, "turn-label");
            phaseLabel = Require<Label>(root, "phase-label");
            playerList = Require<VisualElement>(root, "player-list");
            actionPanel = Require<VisualElement>(root, "action-panel");
            targetPanel = Require<VisualElement>(root, "target-panel");
            targetButtons = Require<VisualElement>(root, "target-buttons");
            lockRow = Require<VisualElement>(root, "lock-row");
            selectionLabel = Require<Label>(root, "selection-label");
            lockButton = Require<Button>(root, "lock-button");
            resultPanel = Require<VisualElement>(root, "result-panel");
            resultLabel = Require<Label>(root, "result-label");
            nextTurnButton = Require<Button>(root, "next-turn-button");
            gameOverPanel = Require<VisualElement>(root, "game-over-panel");
            gameOverLabel = Require<Label>(root, "game-over-label");
            battleLog = Require<ScrollView>(root, "battle-log");
            Button restartButton = Require<Button>(root, "restart-button");

            actionButtons[ActionType.Gather] = Require<Button>(root, "gather-button");
            actionButtons[ActionType.EnergyWave] = Require<Button>(root, "energy-wave-button");
            actionButtons[ActionType.Block] = Require<Button>(root, "block-button");
            actionButtons[ActionType.Teleport] = Require<Button>(root, "teleport-button");
            actionButtons[ActionType.SpiritBomb] = Require<Button>(root, "spirit-bomb-button");

            for (int i = 0; i < ActionOrder.Length; i++)
            {
                ActionType type = ActionOrder[i];
                actionButtons[type].clicked += () => SelectAction(type);
            }

            lockButton.clicked += LockSelection;
            nextTurnButton.clicked += NextTurn;
            restartButton.clicked += () =>
            {
                if (RestartRequested != null)
                {
                    RestartRequested();
                }
            };
        }

        public void Bind(LocalBattleSession newSession)
        {
            session = newSession;
            ClearSelection();
            selectionMessage = null;
            lastLoggedTurn = 0;
            battleLog.Clear();
            Refresh();
        }

        public void SelectAction(ActionType actionType)
        {
            if (session == null || !session.IsAwaitingHumanAction || !IsChoosable(actionType))
            {
                return;
            }

            pendingAction = actionType;
            pendingTarget = PlayerAction.NoTarget;
            selectionMessage = null;
            Refresh();
        }

        public void SelectTarget(int targetPlayerId)
        {
            if (session == null || !session.IsAwaitingHumanAction || !IsAttack(pendingAction))
            {
                return;
            }

            pendingTarget = targetPlayerId;
            selectionMessage = null;
            Refresh();
        }

        public void LockSelection()
        {
            if (session == null || !IsSelectionComplete())
            {
                return;
            }

            LockResult result = session.SubmitHumanAction(pendingAction, pendingTarget);
            if (result == LockResult.Locked)
            {
                ClearSelection();
                selectionMessage = null;
            }
            else
            {
                selectionMessage = "Cannot lock: " + result;
            }

            Refresh();
        }

        public void NextTurn()
        {
            if (session == null || !session.IsRevealed || session.IsGameOver)
            {
                return;
            }

            session.StartNextTurn();
            Refresh();
        }

        public void Refresh()
        {
            if (session == null)
            {
                return;
            }

            AppendLogIfResolved();

            turnLabel.text = "Turn " + session.TurnNumber;
            phaseLabel.text = PhaseText();

            RefreshPlayers();
            RefreshActions();
            RefreshTargets();

            selectionLabel.text = SelectionText();
            lockButton.SetEnabled(session.IsAwaitingHumanAction && IsSelectionComplete());

            bool revealed = session.IsRevealed && session.LastResult != null;
            SetVisible(resultPanel, revealed);
            resultLabel.text = revealed
                ? string.Join("\n", BattleLogFormatter.FormatTurn(session.TurnNumber, session.Players, session.LastResult))
                : string.Empty;
            SetVisible(nextTurnButton, revealed && !session.IsGameOver);

            // After the game ends there is nothing to choose; hide the input rows so the result fits on screen.
            SetVisible(actionPanel, !session.IsGameOver);
            SetVisible(lockRow, !session.IsGameOver);
            SetVisible(gameOverPanel, session.IsGameOver);
            gameOverLabel.text = session.IsGameOver ? GameOverText() : string.Empty;
        }

        private void RefreshPlayers()
        {
            playerList.Clear();
            IReadOnlyList<Player> players = session.Players;
            for (int i = 0; i < players.Count; i++)
            {
                Player player = players[i];
                VisualElement card = new VisualElement();
                card.name = "player-card-" + player.PlayerId;
                card.AddToClassList("player-card");
                if (player.PlayerId == LocalBattleSession.HumanPlayerId)
                {
                    card.AddToClassList("is-human");
                }

                bool eliminated = player.State == PlayerState.Eliminated;
                if (eliminated)
                {
                    card.AddToClassList("is-eliminated");
                }

                card.Add(CreateLabel(player.Nickname, "player-name"));
                card.Add(CreateLabel("HP " + player.HP + " / " + player.MaxHP, "player-stat"));
                card.Add(CreateLabel("Ki " + player.Ki + " / " + player.MaxKi, "player-stat"));
                card.Add(CreateLabel(eliminated ? "Eliminated" : (session.IsLocked(player.PlayerId) ? "Locked" : "Choosing..."), "player-status"));
                card.Add(CreateLabel("Action: " + VisibleActionText(player), "player-action"));
                playerList.Add(card);
            }
        }

        private void RefreshActions()
        {
            bool canAct = session.IsAwaitingHumanAction;
            actionPanel.SetEnabled(canAct);
            for (int i = 0; i < ActionOrder.Length; i++)
            {
                ActionType type = ActionOrder[i];
                Button button = actionButtons[type];
                button.SetEnabled(canAct && IsChoosable(type));
                button.EnableInClassList("selected", type == pendingAction);
            }
        }

        private void RefreshTargets()
        {
            targetButtons.Clear();
            bool show = session.IsAwaitingHumanAction && IsAttack(pendingAction);
            SetVisible(targetPanel, show);
            if (!show)
            {
                return;
            }

            IReadOnlyList<Player> players = session.Players;
            for (int i = 0; i < players.Count; i++)
            {
                Player candidate = players[i];
                if (session.CheckHumanAction(pendingAction, candidate.PlayerId) != ActionValidationResult.Valid)
                {
                    continue;
                }

                int targetId = candidate.PlayerId;
                Button button = new Button(() => SelectTarget(targetId));
                button.name = "target-button-" + targetId;
                button.text = candidate.Nickname;
                button.AddToClassList("target-button");
                button.EnableInClassList("selected", targetId == pendingTarget);
                targetButtons.Add(button);
            }
        }

        private void AppendLogIfResolved()
        {
            if (session.LastResult == null || session.TurnNumber == lastLoggedTurn)
            {
                return;
            }

            lastLoggedTurn = session.TurnNumber;
            List<string> lines = BattleLogFormatter.FormatTurn(session.TurnNumber, session.Players, session.LastResult);
            // Newest turn first; lines of one turn stay in reading order.
            for (int i = lines.Count - 1; i >= 0; i--)
            {
                battleLog.Insert(0, CreateLabel(lines[i], "log-line"));
            }
        }

        private string VisibleActionText(Player player)
        {
            PlayerAction action = session.GetVisibleAction(player.PlayerId);
            if (action == null)
            {
                return session.IsLocked(player.PlayerId) ? "? (hidden)" : "-";
            }

            if (action.ActionType == ActionType.None)
            {
                return "-";
            }

            string text = BattleLogFormatter.ActionName(action.ActionType);
            if (action.TargetPlayerId != PlayerAction.NoTarget)
            {
                text += " -> " + NameOf(action.TargetPlayerId);
            }

            return text;
        }

        private string PhaseText()
        {
            if (session.IsGameOver)
            {
                return "Game over";
            }

            if (session.IsRevealed)
            {
                return "Actions revealed";
            }

            if (session.IsAwaitingHumanAction)
            {
                return "Choose your action and lock it";
            }

            return "Waiting for other players";
        }

        private string SelectionText()
        {
            if (selectionMessage != null)
            {
                return selectionMessage;
            }

            if (!session.IsAwaitingHumanAction)
            {
                return string.Empty;
            }

            if (pendingAction == ActionType.None)
            {
                return "Select an action.";
            }

            string text = "Selected: " + BattleLogFormatter.ActionName(pendingAction);
            if (IsAttack(pendingAction))
            {
                text += pendingTarget == PlayerAction.NoTarget ? " -> choose a target" : " -> " + NameOf(pendingTarget);
            }

            return text;
        }

        private string GameOverText()
        {
            BattleResult result = session.LastResult;
            if (result == null || result.Outcome == GameOutcome.Draw)
            {
                return "Draw - no survivors.";
            }

            if (result.WinnerPlayerId == LocalBattleSession.HumanPlayerId)
            {
                return "You win!";
            }

            return NameOf(result.WinnerPlayerId) + " wins. You lose.";
        }

        private bool IsChoosable(ActionType actionType)
        {
            if (!IsAttack(actionType))
            {
                return session.CheckHumanAction(actionType, PlayerAction.NoTarget) == ActionValidationResult.Valid;
            }

            IReadOnlyList<Player> players = session.Players;
            for (int i = 0; i < players.Count; i++)
            {
                if (session.CheckHumanAction(actionType, players[i].PlayerId) == ActionValidationResult.Valid)
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsSelectionComplete()
        {
            if (pendingAction == ActionType.None)
            {
                return false;
            }

            return !IsAttack(pendingAction) || pendingTarget != PlayerAction.NoTarget;
        }

        private void ClearSelection()
        {
            pendingAction = ActionType.None;
            pendingTarget = PlayerAction.NoTarget;
        }

        private string NameOf(int playerId)
        {
            IReadOnlyList<Player> players = session.Players;
            for (int i = 0; i < players.Count; i++)
            {
                if (players[i].PlayerId == playerId)
                {
                    return players[i].Nickname;
                }
            }

            return "Player " + playerId;
        }

        private static bool IsAttack(ActionType actionType)
        {
            return actionType == ActionType.EnergyWave || actionType == ActionType.SpiritBomb;
        }

        private static Label CreateLabel(string text, string className)
        {
            Label label = new Label(text);
            label.AddToClassList(className);
            return label;
        }

        private static void SetVisible(VisualElement element, bool visible)
        {
            element.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private static T Require<T>(VisualElement root, string name) where T : VisualElement
        {
            T element = root.Q<T>(name);
            if (element == null)
            {
                throw new InvalidOperationException("BattleScreen UXML is missing element '" + name + "' of type " + typeof(T).Name + ".");
            }

            return element;
        }
    }
}
