using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace DRG.Tests
{
    public class LocalBattleSessionTests
    {
        private GameSettings settings;

        [SetUp]
        public void SetUp()
        {
            settings = new GameSettings();
        }

        private LocalBattleSession StartedSession(int botCount)
        {
            LocalBattleSession session = new LocalBattleSession(settings, botCount, 123);
            session.StartNextTurn();
            return session;
        }

        [Test]
        public void NewSession_HasHumanAtIdZero_AndBots()
        {
            LocalBattleSession session = new LocalBattleSession(settings, 1, 0);

            Assert.AreEqual(2, session.Players.Count);
            Assert.AreEqual(LocalBattleSession.HumanPlayerId, session.Human.PlayerId);
            Assert.AreEqual("You", session.Human.Nickname);
            Assert.AreEqual("Bot 1", session.Players[1].Nickname);
        }

        [TestCase(0)]
        [TestCase(8)]
        public void InvalidBotCount_Throws(int botCount)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new LocalBattleSession(settings, botCount, 0));
        }

        [Test]
        public void StartNextTurn_LocksBots_AndWaitsForHuman()
        {
            LocalBattleSession session = StartedSession(1);

            Assert.AreEqual(1, session.TurnNumber);
            Assert.IsTrue(session.IsLocked(1));
            Assert.IsFalse(session.IsLocked(LocalBattleSession.HumanPlayerId));
            Assert.IsTrue(session.IsAwaitingHumanAction);
            Assert.IsFalse(session.IsRevealed);
        }

        [Test]
        public void OpponentAction_IsHiddenBeforeReveal()
        {
            LocalBattleSession session = StartedSession(1);

            Assert.IsTrue(session.IsLocked(1));
            Assert.IsNull(session.GetVisibleAction(1));
        }

        [Test]
        public void HumanLock_ResolvesTurn_AndRevealsOpponentAction()
        {
            LocalBattleSession session = StartedSession(1);

            LockResult result = session.SubmitHumanAction(ActionType.Gather, PlayerAction.NoTarget);

            Assert.AreEqual(LockResult.Locked, result);
            Assert.IsTrue(session.IsRevealed);
            Assert.IsNotNull(session.LastResult);
            Assert.IsNotNull(session.GetVisibleAction(1));
            Assert.AreEqual(ActionType.Gather, session.GetVisibleAction(LocalBattleSession.HumanPlayerId).ActionType);
            Assert.AreEqual(1, session.Human.Ki);
        }

        [Test]
        public void InvalidHumanAction_IsRejected_AndTurnDoesNotResolve()
        {
            LocalBattleSession session = StartedSession(1);

            Assert.AreEqual(ActionValidationResult.InsufficientKi, session.CheckHumanAction(ActionType.EnergyWave, 1));
            Assert.AreEqual(LockResult.InvalidAction, session.SubmitHumanAction(ActionType.EnergyWave, 1));
            Assert.IsFalse(session.IsLocked(LocalBattleSession.HumanPlayerId));
            Assert.IsFalse(session.IsRevealed);
            Assert.IsNull(session.LastResult);
        }

        [Test]
        public void VisibleAction_IsACopy()
        {
            LocalBattleSession session = StartedSession(1);
            session.SubmitHumanAction(ActionType.Block, PlayerAction.NoTarget);

            PlayerAction visible = session.GetVisibleAction(1);
            ActionType original = session.Players[1].CurrentAction.ActionType;
            visible.ActionType = original == ActionType.Block ? ActionType.Gather : ActionType.Block;

            Assert.AreEqual(original, session.Players[1].CurrentAction.ActionType);
        }

        [Test]
        public void StartNextTurn_AfterReveal_BeginsNewTurn()
        {
            LocalBattleSession session = StartedSession(1);
            session.SubmitHumanAction(ActionType.Block, PlayerAction.NoTarget);

            session.StartNextTurn();

            Assert.AreEqual(2, session.TurnNumber);
            Assert.IsFalse(session.IsRevealed);
            Assert.IsNull(session.LastResult);
            Assert.IsTrue(session.IsAwaitingHumanAction);
        }

        [Test]
        public void EliminatedHuman_BotsStillResolveTheTurn()
        {
            LocalBattleSession session = new LocalBattleSession(settings, 2, 5);
            session.Human.State = PlayerState.Eliminated;
            session.Human.HP = 0;

            session.StartNextTurn();

            Assert.IsFalse(session.IsAwaitingHumanAction);
            Assert.IsTrue(session.IsRevealed);
            Assert.IsNotNull(session.LastResult);
        }

        [Test]
        public void FullGame_EndsAndBlocksFurtherTurns()
        {
            LocalBattleSession session = StartedSession(1);
            RandomActionPolicy humanPolicy = new RandomActionPolicy(99, settings);

            for (int turn = 0; turn < BattleSimulator.DefaultTurnCap && !session.IsGameOver; turn++)
            {
                if (turn > 0)
                {
                    session.StartNextTurn();
                }

                PlayerAction action = humanPolicy.ChooseAction(session.Human, new List<Player>(session.Players));
                Assert.AreEqual(LockResult.Locked, session.SubmitHumanAction(action.ActionType, action.TargetPlayerId));
            }

            Assert.IsTrue(session.IsGameOver);
            Assert.AreNotEqual(GameOutcome.Ongoing, session.LastResult.Outcome);
            Assert.Throws<InvalidOperationException>(() => session.StartNextTurn());
        }
    }
}
