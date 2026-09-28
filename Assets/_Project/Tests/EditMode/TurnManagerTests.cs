using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace DRG.Tests
{
    public class TurnManagerTests
    {
        private GameSettings settings;
        private List<Player> players;
        private Player a;
        private Player b;
        private Player c;
        private TurnManager turnManager;

        [SetUp]
        public void SetUp()
        {
            settings = new GameSettings();
            a = new Player(0, "A", settings);
            b = new Player(1, "B", settings);
            c = new Player(2, "C", settings);
            players = new List<Player> { a, b, c };
            turnManager = new TurnManager(players, settings);
        }

        private static PlayerAction Action(Player actor, ActionType type, int target = PlayerAction.NoTarget)
        {
            return new PlayerAction { PlayerId = actor.PlayerId, ActionType = type, TargetPlayerId = target };
        }

        private void BeginSelection()
        {
            turnManager.StartTurn();
            turnManager.StartActionSelection();
        }

        // --- Turn flow ---

        [Test]
        public void NewTurnManager_HasNotStartedAnyTurn()
        {
            Assert.AreEqual(TurnState.TurnEnd, turnManager.State);
            Assert.AreEqual(0, turnManager.TurnNumber);
        }

        [Test]
        public void TurnFlow_FollowsStateOrder()
        {
            turnManager.StartTurn();
            Assert.AreEqual(TurnState.TurnStart, turnManager.State);
            Assert.AreEqual(1, turnManager.TurnNumber);

            turnManager.StartActionSelection();
            Assert.AreEqual(TurnState.ActionSelection, turnManager.State);

            turnManager.Lock(Action(a, ActionType.Gather));
            turnManager.Lock(Action(b, ActionType.Block));
            turnManager.Lock(Action(c, ActionType.Gather));
            turnManager.StartResolution();
            Assert.AreEqual(TurnState.ActionResolution, turnManager.State);

            turnManager.EndTurn();
            Assert.AreEqual(TurnState.TurnEnd, turnManager.State);

            turnManager.StartTurn();
            Assert.AreEqual(2, turnManager.TurnNumber);
        }

        [Test]
        public void StartTurn_DuringActionSelection_Throws()
        {
            BeginSelection();
            Assert.Throws<InvalidOperationException>(() => turnManager.StartTurn());
        }

        [Test]
        public void EndTurn_BeforeResolution_Throws()
        {
            BeginSelection();
            Assert.Throws<InvalidOperationException>(() => turnManager.EndTurn());
        }

        [Test]
        public void NewTurn_ClearsLocksAndPreviousActions()
        {
            BeginSelection();
            turnManager.Lock(Action(a, ActionType.Gather));
            turnManager.Lock(Action(b, ActionType.Gather));
            turnManager.Lock(Action(c, ActionType.Gather));
            turnManager.StartResolution();
            turnManager.EndTurn();

            turnManager.StartTurn();

            Assert.IsFalse(turnManager.IsLocked(a.PlayerId));
            Assert.IsNull(a.CurrentAction);
        }

        // --- Lock ---

        [Test]
        public void Lock_ValidAction_CommitsCopyOfAction()
        {
            BeginSelection();
            PlayerAction action = Action(a, ActionType.Gather);

            Assert.AreEqual(LockResult.Locked, turnManager.Lock(action));
            Assert.IsTrue(turnManager.IsLocked(a.PlayerId));
            Assert.AreEqual(ActionType.Gather, a.CurrentAction.ActionType);
            Assert.AreNotSame(action, a.CurrentAction);
        }

        [Test]
        public void Lock_DoesNotSpendKi()
        {
            a.Ki = 1;
            BeginSelection();

            turnManager.Lock(Action(a, ActionType.EnergyWave, b.PlayerId));

            Assert.AreEqual(1, a.Ki);
        }

        [Test]
        public void Lock_Twice_IsRejectedAndKeepsFirstAction()
        {
            a.Ki = 1;
            BeginSelection();
            turnManager.Lock(Action(a, ActionType.Block));

            LockResult result = turnManager.Lock(Action(a, ActionType.EnergyWave, b.PlayerId));

            Assert.AreEqual(LockResult.AlreadyLocked, result);
            Assert.AreEqual(ActionType.Block, a.CurrentAction.ActionType);
        }

        [Test]
        public void Lock_ChangingCallerObjectAfterLock_DoesNotChangeCommittedAction()
        {
            a.Ki = 1;
            BeginSelection();
            PlayerAction action = Action(a, ActionType.EnergyWave, b.PlayerId);
            turnManager.Lock(action);

            action.TargetPlayerId = c.PlayerId;
            action.ActionType = ActionType.Block;

            Assert.AreEqual(ActionType.EnergyWave, a.CurrentAction.ActionType);
            Assert.AreEqual(b.PlayerId, a.CurrentAction.TargetPlayerId);
        }

        [Test]
        public void Lock_InvalidAction_IsRejectedAndNotLocked()
        {
            a.Ki = 0;
            BeginSelection();

            LockResult result = turnManager.Lock(Action(a, ActionType.EnergyWave, b.PlayerId));

            Assert.AreEqual(LockResult.InvalidAction, result);
            Assert.IsFalse(turnManager.IsLocked(a.PlayerId));
            Assert.IsNull(a.CurrentAction);
        }

        [Test]
        public void Lock_NoneFromLivingPlayer_IsRejected()
        {
            BeginSelection();
            Assert.AreEqual(LockResult.InvalidAction, turnManager.Lock(Action(a, ActionType.None)));
        }

        [Test]
        public void Lock_NullAction_IsRejected()
        {
            BeginSelection();
            Assert.AreEqual(LockResult.InvalidAction, turnManager.Lock(null));
        }

        [Test]
        public void Lock_OutsideActionSelection_IsRejected()
        {
            Assert.AreEqual(LockResult.WrongTurnState, turnManager.Lock(Action(a, ActionType.Gather)));

            turnManager.StartTurn();
            Assert.AreEqual(LockResult.WrongTurnState, turnManager.Lock(Action(a, ActionType.Gather)));
        }

        [Test]
        public void Lock_AfterResolutionStarted_IsRejected()
        {
            BeginSelection();
            turnManager.Lock(Action(a, ActionType.Gather));
            turnManager.Lock(Action(b, ActionType.Gather));
            turnManager.Lock(Action(c, ActionType.Gather));
            turnManager.StartResolution();

            Assert.AreEqual(LockResult.WrongTurnState, turnManager.Lock(Action(a, ActionType.Block)));
        }

        // --- All-locked check and eliminated players ---

        [Test]
        public void AreAllPlayersLocked_FalseUntilEveryLivingPlayerLocks()
        {
            BeginSelection();
            turnManager.Lock(Action(a, ActionType.Gather));
            turnManager.Lock(Action(b, ActionType.Gather));
            Assert.IsFalse(turnManager.AreAllPlayersLocked());

            turnManager.Lock(Action(c, ActionType.Gather));
            Assert.IsTrue(turnManager.AreAllPlayersLocked());
        }

        [Test]
        public void StartResolution_BeforeAllLocked_Throws()
        {
            BeginSelection();
            turnManager.Lock(Action(a, ActionType.Gather));
            Assert.Throws<InvalidOperationException>(() => turnManager.StartResolution());
        }

        [Test]
        public void EliminatedPlayer_IsAutoLockedWithNone()
        {
            c.State = PlayerState.Eliminated;
            BeginSelection();

            Assert.IsTrue(turnManager.IsLocked(c.PlayerId));
            Assert.AreEqual(ActionType.None, c.CurrentAction.ActionType);
            Assert.AreEqual(PlayerAction.NoTarget, c.CurrentAction.TargetPlayerId);
        }

        [Test]
        public void EliminatedPlayer_DoesNotBlockAllLocked()
        {
            c.State = PlayerState.Eliminated;
            BeginSelection();
            turnManager.Lock(Action(a, ActionType.Gather));
            turnManager.Lock(Action(b, ActionType.Gather));

            Assert.IsTrue(turnManager.AreAllPlayersLocked());
        }

        [Test]
        public void EliminatedPlayer_CannotLockAnotherAction()
        {
            c.State = PlayerState.Eliminated;
            BeginSelection();

            Assert.AreEqual(LockResult.AlreadyLocked, turnManager.Lock(Action(c, ActionType.Block)));
            Assert.AreEqual(ActionType.None, c.CurrentAction.ActionType);
        }

        // --- Resolution through TurnManager ---

        private BattleResult PlayTurn(PlayerAction[] actions)
        {
            BeginSelection();
            for (int i = 0; i < actions.Length; i++)
            {
                Assert.AreEqual(LockResult.Locked, turnManager.Lock(actions[i]));
            }

            return turnManager.StartResolution();
        }

        [Test]
        public void StartResolution_ResolvesLockedActions()
        {
            a.Ki = 1;

            BattleResult result = PlayTurn(new[]
            {
                Action(a, ActionType.EnergyWave, b.PlayerId),
                Action(b, ActionType.Gather),
                Action(c, ActionType.Block)
            });

            Assert.AreEqual(2, b.HP);
            Assert.AreEqual(0, a.Ki);
            Assert.AreEqual(1, b.Ki);
            Assert.AreEqual(1, result.DamageResults.Count);
            Assert.AreEqual(GameOutcome.Ongoing, result.Outcome);
            Assert.IsFalse(turnManager.IsGameOver);
        }

        [Test]
        public void OngoingGame_AllowsNextTurn()
        {
            PlayTurn(new[]
            {
                Action(a, ActionType.Gather),
                Action(b, ActionType.Gather),
                Action(c, ActionType.Gather)
            });
            turnManager.EndTurn();

            turnManager.StartTurn();

            Assert.AreEqual(2, turnManager.TurnNumber);
        }

        [Test]
        public void Winner_EndsGame_AndBlocksNextTurn()
        {
            a.Ki = 1;
            b.HP = 1;
            c.State = PlayerState.Eliminated;
            c.HP = 0;

            BattleResult result = PlayTurn(new[]
            {
                Action(a, ActionType.EnergyWave, b.PlayerId),
                Action(b, ActionType.Gather)
            });
            turnManager.EndTurn();

            Assert.AreEqual(GameOutcome.Winner, result.Outcome);
            Assert.AreEqual(a.PlayerId, result.WinnerPlayerId);
            Assert.IsTrue(turnManager.IsGameOver);
            Assert.Throws<InvalidOperationException>(() => turnManager.StartTurn());
        }

        [Test]
        public void Draw_EndsGame_AndBlocksNextTurn()
        {
            a.Ki = 3;
            b.Ki = 3;
            a.HP = 1;
            b.HP = 1;
            c.State = PlayerState.Eliminated;
            c.HP = 0;

            BattleResult result = PlayTurn(new[]
            {
                Action(a, ActionType.SpiritBomb, b.PlayerId),
                Action(b, ActionType.SpiritBomb, a.PlayerId)
            });
            turnManager.EndTurn();

            Assert.AreEqual(GameOutcome.Draw, result.Outcome);
            Assert.IsTrue(turnManager.IsGameOver);
            Assert.Throws<InvalidOperationException>(() => turnManager.StartTurn());
        }

        [Test]
        public void PlayerEliminatedThisTurn_IsAutoLockedNextTurn_AndCannotBeTargeted()
        {
            a.Ki = 1;
            b.HP = 1;
            PlayTurn(new[]
            {
                Action(a, ActionType.EnergyWave, b.PlayerId),
                Action(b, ActionType.Gather),
                Action(c, ActionType.Gather)
            });
            turnManager.EndTurn();

            BeginSelection();

            Assert.AreEqual(PlayerState.Eliminated, b.State);
            Assert.IsTrue(turnManager.IsLocked(b.PlayerId));
            Assert.AreEqual(ActionType.None, b.CurrentAction.ActionType);
            Assert.AreEqual(LockResult.InvalidAction, turnManager.Lock(Action(c, ActionType.EnergyWave, b.PlayerId)));
        }

        [Test]
        public void FullGame_PlaysMultipleTurnsUntilWinner()
        {
            // Turn 1: everyone gathers.
            PlayTurn(new[]
            {
                Action(a, ActionType.Gather),
                Action(b, ActionType.Gather),
                Action(c, ActionType.Gather)
            });
            turnManager.EndTurn();

            // Turn 2: A and C both hit B; damage stacks.
            PlayTurn(new[]
            {
                Action(a, ActionType.EnergyWave, b.PlayerId),
                Action(b, ActionType.Gather),
                Action(c, ActionType.EnergyWave, b.PlayerId)
            });
            turnManager.EndTurn();
            Assert.AreEqual(1, b.HP);

            // Turn 3: B hits C while A and C gather.
            PlayTurn(new[]
            {
                Action(a, ActionType.Gather),
                Action(b, ActionType.EnergyWave, c.PlayerId),
                Action(c, ActionType.Gather)
            });
            turnManager.EndTurn();
            Assert.AreEqual(2, c.HP);

            // Turn 4: A kills B, and B's committed wave still hits C in the same turn.
            PlayTurn(new[]
            {
                Action(a, ActionType.EnergyWave, b.PlayerId),
                Action(b, ActionType.EnergyWave, c.PlayerId),
                Action(c, ActionType.Gather)
            });
            turnManager.EndTurn();
            Assert.AreEqual(PlayerState.Eliminated, b.State);
            Assert.AreEqual(1, c.HP);
            Assert.IsFalse(turnManager.IsGameOver);

            // Turn 5: B is auto-locked and only A and C act.
            PlayTurn(new[]
            {
                Action(a, ActionType.Gather),
                Action(c, ActionType.Block)
            });
            turnManager.EndTurn();

            // Turn 6: A's wave hits C while C gathers.
            BattleResult result = PlayTurn(new[]
            {
                Action(a, ActionType.EnergyWave, c.PlayerId),
                Action(c, ActionType.Gather)
            });
            turnManager.EndTurn();

            Assert.AreEqual(GameOutcome.Winner, result.Outcome);
            Assert.AreEqual(a.PlayerId, result.WinnerPlayerId);
            Assert.AreEqual(6, turnManager.TurnNumber);
            Assert.IsTrue(turnManager.IsGameOver);
        }
    }
}
