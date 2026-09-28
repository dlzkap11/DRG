using System.Collections.Generic;
using NUnit.Framework;

namespace DRG.Tests
{
    public class ActionValidatorTests
    {
        private GameSettings settings;
        private ActionValidator validator;
        private List<Player> players;
        private Player a;
        private Player b;
        private Player c;

        [SetUp]
        public void SetUp()
        {
            settings = new GameSettings();
            validator = new ActionValidator(settings);
            a = new Player(0, "A", settings);
            b = new Player(1, "B", settings);
            c = new Player(2, "C", settings);
            players = new List<Player> { a, b, c };
        }

        private static PlayerAction Action(Player actor, ActionType type, int target = PlayerAction.NoTarget)
        {
            return new PlayerAction { PlayerId = actor.PlayerId, ActionType = type, TargetPlayerId = target };
        }

        private ActionValidationResult Validate(PlayerAction action)
        {
            return validator.Validate(action, players);
        }

        // --- All five actions are valid when their conditions are met ---

        [Test]
        public void Gather_BelowMaxKi_IsValid()
        {
            a.Ki = 0;
            Assert.AreEqual(ActionValidationResult.Valid, Validate(Action(a, ActionType.Gather)));
        }

        [Test]
        public void EnergyWave_WithOneKi_OnLivingOpponent_IsValid()
        {
            a.Ki = 1;
            Assert.AreEqual(ActionValidationResult.Valid, Validate(Action(a, ActionType.EnergyWave, b.PlayerId)));
        }

        [Test]
        public void Block_WithZeroKi_IsValid()
        {
            a.Ki = 0;
            Assert.AreEqual(ActionValidationResult.Valid, Validate(Action(a, ActionType.Block)));
        }

        [Test]
        public void Teleport_WithOneKi_IsValid()
        {
            a.Ki = 1;
            Assert.AreEqual(ActionValidationResult.Valid, Validate(Action(a, ActionType.Teleport)));
        }

        [Test]
        public void SpiritBomb_WithThreeKi_OnLivingOpponent_IsValid()
        {
            a.Ki = 3;
            Assert.AreEqual(ActionValidationResult.Valid, Validate(Action(a, ActionType.SpiritBomb, c.PlayerId)));
        }

        // --- Ki costs and limits ---

        [Test]
        public void Gather_AtMaxKi_IsRejected()
        {
            a.Ki = a.MaxKi;
            Assert.AreEqual(ActionValidationResult.KiAtMax, Validate(Action(a, ActionType.Gather)));
        }

        [Test]
        public void EnergyWave_WithZeroKi_IsRejected()
        {
            a.Ki = 0;
            Assert.AreEqual(ActionValidationResult.InsufficientKi, Validate(Action(a, ActionType.EnergyWave, b.PlayerId)));
        }

        [Test]
        public void Teleport_WithZeroKi_IsRejected()
        {
            a.Ki = 0;
            Assert.AreEqual(ActionValidationResult.InsufficientKi, Validate(Action(a, ActionType.Teleport)));
        }

        [Test]
        public void SpiritBomb_WithTwoKi_IsRejected()
        {
            a.Ki = 2;
            Assert.AreEqual(ActionValidationResult.InsufficientKi, Validate(Action(a, ActionType.SpiritBomb, b.PlayerId)));
        }

        [Test]
        public void KiCosts_MatchSpecification()
        {
            Assert.AreEqual(0, validator.GetKiCost(ActionType.Gather));
            Assert.AreEqual(1, validator.GetKiCost(ActionType.EnergyWave));
            Assert.AreEqual(0, validator.GetKiCost(ActionType.Block));
            Assert.AreEqual(1, validator.GetKiCost(ActionType.Teleport));
            Assert.AreEqual(3, validator.GetKiCost(ActionType.SpiritBomb));
        }

        // --- Targets ---

        [TestCase(ActionType.EnergyWave, 1)]
        [TestCase(ActionType.SpiritBomb, 3)]
        public void Attack_OnSelf_IsRejected(ActionType type, int ki)
        {
            a.Ki = ki;
            Assert.AreEqual(ActionValidationResult.SelfTarget, Validate(Action(a, type, a.PlayerId)));
        }

        [TestCase(ActionType.EnergyWave, 1)]
        [TestCase(ActionType.SpiritBomb, 3)]
        public void Attack_OnEliminatedPlayer_IsRejected(ActionType type, int ki)
        {
            a.Ki = ki;
            b.State = PlayerState.Eliminated;
            Assert.AreEqual(ActionValidationResult.TargetEliminated, Validate(Action(a, type, b.PlayerId)));
        }

        [TestCase(ActionType.EnergyWave, 1)]
        [TestCase(ActionType.SpiritBomb, 3)]
        public void Attack_OnNonexistentPlayer_IsRejected(ActionType type, int ki)
        {
            a.Ki = ki;
            Assert.AreEqual(ActionValidationResult.TargetNotFound, Validate(Action(a, type, 99)));
        }

        [Test]
        public void Attack_WithoutTarget_IsRejected()
        {
            a.Ki = 1;
            Assert.AreEqual(ActionValidationResult.TargetNotFound, Validate(Action(a, ActionType.EnergyWave)));
        }

        [TestCase(ActionType.Gather)]
        [TestCase(ActionType.Block)]
        [TestCase(ActionType.Teleport)]
        public void NonAttack_WithTarget_IsRejected(ActionType type)
        {
            a.Ki = 1;
            Assert.AreEqual(ActionValidationResult.UnexpectedTarget, Validate(Action(a, type, b.PlayerId)));
        }

        // --- Actor and action type ---

        [Test]
        public void None_FromLivingPlayer_IsRejected()
        {
            Assert.AreEqual(ActionValidationResult.InvalidActionType, Validate(Action(a, ActionType.None)));
        }

        [Test]
        public void UndefinedActionType_IsRejected()
        {
            Assert.AreEqual(ActionValidationResult.InvalidActionType, Validate(Action(a, (ActionType)99)));
        }

        [Test]
        public void EliminatedActor_IsRejected()
        {
            a.State = PlayerState.Eliminated;
            Assert.AreEqual(ActionValidationResult.PlayerEliminated, Validate(Action(a, ActionType.Block)));
        }

        [Test]
        public void UnknownActor_IsRejected()
        {
            PlayerAction action = new PlayerAction { PlayerId = 99, ActionType = ActionType.Block };
            Assert.AreEqual(ActionValidationResult.PlayerNotFound, Validate(action));
        }

        [Test]
        public void NullAction_IsRejected()
        {
            Assert.AreEqual(ActionValidationResult.MissingAction, Validate(null));
        }
    }
}
