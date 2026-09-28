using System.Collections.Generic;
using NUnit.Framework;

namespace DRG.Tests
{
    public class BattleResolverTests
    {
        private GameSettings settings;
        private BattleResolver resolver;
        private Player a;
        private Player b;
        private Player c;
        private List<Player> players;

        [SetUp]
        public void SetUp()
        {
            settings = new GameSettings();
            resolver = new BattleResolver(settings);
            a = new Player(0, "A", settings);
            b = new Player(1, "B", settings);
            c = new Player(2, "C", settings);
            players = new List<Player> { a, b, c };
        }

        private static void Act(Player actor, ActionType type, Player target = null)
        {
            actor.CurrentAction = new PlayerAction
            {
                PlayerId = actor.PlayerId,
                ActionType = type,
                TargetPlayerId = target != null ? target.PlayerId : PlayerAction.NoTarget
            };
        }

        private BattleResult Resolve()
        {
            return resolver.ResolveTurn(players);
        }

        private static ActionResultType ResultOf(BattleResult result, Player attacker)
        {
            for (int i = 0; i < result.ActionResults.Count; i++)
            {
                if (result.ActionResults[i].PlayerId == attacker.PlayerId)
                {
                    return result.ActionResults[i].ResultType;
                }
            }

            Assert.Fail("No ActionResult for player " + attacker.PlayerId);
            return ActionResultType.Failed;
        }

        // --- Ki for all five actions ---

        [Test]
        public void Gather_AddsOneKi()
        {
            Act(a, ActionType.Gather);
            Act(b, ActionType.Block);
            Act(c, ActionType.Block);

            BattleResult result = Resolve();

            Assert.AreEqual(1, a.Ki);
            Assert.AreEqual(1, result.KiChanges.Count);
            Assert.AreEqual(a.PlayerId, result.KiChanges[0].PlayerId);
            Assert.AreEqual(1, result.KiChanges[0].Amount);
        }

        [Test]
        public void Block_CostsNoKi()
        {
            a.Ki = 2;
            Act(a, ActionType.Block);
            Act(b, ActionType.Block);
            Act(c, ActionType.Block);

            BattleResult result = Resolve();

            Assert.AreEqual(2, a.Ki);
            Assert.AreEqual(0, result.KiChanges.Count);
        }

        [Test]
        public void Teleport_CostsOneKi()
        {
            a.Ki = 1;
            Act(a, ActionType.Teleport);
            Act(b, ActionType.Block);
            Act(c, ActionType.Block);

            Resolve();

            Assert.AreEqual(0, a.Ki);
        }

        [Test]
        public void EnergyWave_CostsOneKi_SpiritBomb_CostsThreeKi()
        {
            a.Ki = 1;
            b.Ki = 3;
            Act(a, ActionType.EnergyWave, c);
            Act(b, ActionType.SpiritBomb, c);
            Act(c, ActionType.Gather);

            Resolve();

            Assert.AreEqual(0, a.Ki);
            Assert.AreEqual(0, b.Ki);
        }

        // --- Energy Wave interactions ---

        [Test]
        public void EnergyWave_OnGather_Hits()
        {
            a.Ki = 1;
            Act(a, ActionType.EnergyWave, b);
            Act(b, ActionType.Gather);
            Act(c, ActionType.Block);

            BattleResult result = Resolve();

            Assert.AreEqual(ActionResultType.Hit, ResultOf(result, a));
            Assert.AreEqual(2, b.HP);
            Assert.AreEqual(1, result.DamageResults.Count);
            Assert.AreEqual(a.PlayerId, result.DamageResults[0].AttackerId);
            Assert.AreEqual(b.PlayerId, result.DamageResults[0].TargetId);
            Assert.AreEqual(1, result.DamageResults[0].Damage);
        }

        [Test]
        public void EnergyWave_OnBlock_IsBlocked_AndKiIsStillSpent()
        {
            a.Ki = 1;
            Act(a, ActionType.EnergyWave, b);
            Act(b, ActionType.Block);
            Act(c, ActionType.Block);

            BattleResult result = Resolve();

            Assert.AreEqual(ActionResultType.Blocked, ResultOf(result, a));
            Assert.AreEqual(3, b.HP);
            Assert.AreEqual(0, a.Ki);
            Assert.AreEqual(0, result.DamageResults.Count);
        }

        [Test]
        public void EnergyWave_OnTeleport_IsDodged_AndKiIsStillSpent()
        {
            a.Ki = 1;
            b.Ki = 1;
            Act(a, ActionType.EnergyWave, b);
            Act(b, ActionType.Teleport);
            Act(c, ActionType.Block);

            BattleResult result = Resolve();

            Assert.AreEqual(ActionResultType.Dodged, ResultOf(result, a));
            Assert.AreEqual(3, b.HP);
            Assert.AreEqual(0, a.Ki);
        }

        [Test]
        public void EnergyWave_Mutual_BothCancelled_NoDamage_KiStillSpent()
        {
            a.Ki = 1;
            b.Ki = 1;
            Act(a, ActionType.EnergyWave, b);
            Act(b, ActionType.EnergyWave, a);
            Act(c, ActionType.Block);

            BattleResult result = Resolve();

            Assert.AreEqual(ActionResultType.Cancelled, ResultOf(result, a));
            Assert.AreEqual(ActionResultType.Cancelled, ResultOf(result, b));
            Assert.AreEqual(3, a.HP);
            Assert.AreEqual(3, b.HP);
            Assert.AreEqual(0, a.Ki);
            Assert.AreEqual(0, b.Ki);
            Assert.AreEqual(0, result.DamageResults.Count);
        }

        [Test]
        public void EnergyWave_SameTarget_DoesNotCancel_AndDamageStacks()
        {
            a.Ki = 1;
            b.Ki = 1;
            Act(a, ActionType.EnergyWave, c);
            Act(b, ActionType.EnergyWave, c);
            Act(c, ActionType.Gather);

            BattleResult result = Resolve();

            Assert.AreEqual(ActionResultType.Hit, ResultOf(result, a));
            Assert.AreEqual(ActionResultType.Hit, ResultOf(result, b));
            Assert.AreEqual(1, c.HP);
            Assert.AreEqual(2, result.DamageResults.Count);
        }

        [Test]
        public void EnergyWave_Chain_IsNotMutual_BothHit()
        {
            // A -> B and B -> C: B's wave is not aimed at A, so nothing cancels.
            a.Ki = 1;
            b.Ki = 1;
            Act(a, ActionType.EnergyWave, b);
            Act(b, ActionType.EnergyWave, c);
            Act(c, ActionType.Gather);

            BattleResult result = Resolve();

            Assert.AreEqual(ActionResultType.Hit, ResultOf(result, a));
            Assert.AreEqual(ActionResultType.Hit, ResultOf(result, b));
            Assert.AreEqual(2, b.HP);
            Assert.AreEqual(2, c.HP);
        }

        [Test]
        public void EnergyWave_MutualPair_ThirdAttackerStillHits()
        {
            a.Ki = 1;
            b.Ki = 1;
            c.Ki = 1;
            Act(a, ActionType.EnergyWave, b);
            Act(b, ActionType.EnergyWave, a);
            Act(c, ActionType.EnergyWave, b);

            BattleResult result = Resolve();

            Assert.AreEqual(ActionResultType.Cancelled, ResultOf(result, a));
            Assert.AreEqual(ActionResultType.Cancelled, ResultOf(result, b));
            Assert.AreEqual(ActionResultType.Hit, ResultOf(result, c));
            Assert.AreEqual(3, a.HP);
            Assert.AreEqual(2, b.HP);
        }

        [Test]
        public void EnergyWave_And_SpiritBomb_AtEachOther_BothHit()
        {
            a.Ki = 1;
            b.Ki = 3;
            Act(a, ActionType.EnergyWave, b);
            Act(b, ActionType.SpiritBomb, a);
            Act(c, ActionType.Block);

            BattleResult result = Resolve();

            Assert.AreEqual(ActionResultType.Hit, ResultOf(result, a));
            Assert.AreEqual(ActionResultType.Hit, ResultOf(result, b));
            Assert.AreEqual(2, a.HP);
            Assert.AreEqual(2, b.HP);
        }

        // --- Spirit Bomb interactions ---

        [Test]
        public void SpiritBomb_OnGather_Hits()
        {
            a.Ki = 3;
            Act(a, ActionType.SpiritBomb, b);
            Act(b, ActionType.Gather);
            Act(c, ActionType.Block);

            BattleResult result = Resolve();

            Assert.AreEqual(ActionResultType.Hit, ResultOf(result, a));
            Assert.AreEqual(2, b.HP);
        }

        [Test]
        public void SpiritBomb_IgnoresBlock()
        {
            a.Ki = 3;
            Act(a, ActionType.SpiritBomb, b);
            Act(b, ActionType.Block);
            Act(c, ActionType.Block);

            BattleResult result = Resolve();

            Assert.AreEqual(ActionResultType.Hit, ResultOf(result, a));
            Assert.AreEqual(2, b.HP);
        }

        [Test]
        public void SpiritBomb_OnTeleport_IsDodged()
        {
            a.Ki = 3;
            b.Ki = 1;
            Act(a, ActionType.SpiritBomb, b);
            Act(b, ActionType.Teleport);
            Act(c, ActionType.Block);

            BattleResult result = Resolve();

            Assert.AreEqual(ActionResultType.Dodged, ResultOf(result, a));
            Assert.AreEqual(3, b.HP);
            Assert.AreEqual(0, a.Ki);
        }

        [Test]
        public void SpiritBomb_Mutual_DoesNotCancel_BothHit()
        {
            a.Ki = 3;
            b.Ki = 3;
            Act(a, ActionType.SpiritBomb, b);
            Act(b, ActionType.SpiritBomb, a);
            Act(c, ActionType.Block);

            BattleResult result = Resolve();

            Assert.AreEqual(ActionResultType.Hit, ResultOf(result, a));
            Assert.AreEqual(ActionResultType.Hit, ResultOf(result, b));
            Assert.AreEqual(2, a.HP);
            Assert.AreEqual(2, b.HP);
        }

        // --- Defense against multiple attackers ---

        [Test]
        public void Teleport_AvoidsAllAttacksAtOnce()
        {
            a.Ki = 1;
            b.Ki = 3;
            c.Ki = 1;
            Act(a, ActionType.EnergyWave, c);
            Act(b, ActionType.SpiritBomb, c);
            Act(c, ActionType.Teleport);

            BattleResult result = Resolve();

            Assert.AreEqual(ActionResultType.Dodged, ResultOf(result, a));
            Assert.AreEqual(ActionResultType.Dodged, ResultOf(result, b));
            Assert.AreEqual(3, c.HP);
        }

        [Test]
        public void Block_StopsAllEnergyWaves_ButNotSpiritBomb()
        {
            Player d = new Player(3, "D", settings);
            players.Add(d);
            a.Ki = 1;
            b.Ki = 1;
            c.Ki = 3;
            Act(a, ActionType.EnergyWave, d);
            Act(b, ActionType.EnergyWave, d);
            Act(c, ActionType.SpiritBomb, d);
            Act(d, ActionType.Block);

            BattleResult result = Resolve();

            Assert.AreEqual(ActionResultType.Blocked, ResultOf(result, a));
            Assert.AreEqual(ActionResultType.Blocked, ResultOf(result, b));
            Assert.AreEqual(ActionResultType.Hit, ResultOf(result, c));
            Assert.AreEqual(2, d.HP);
        }

        // --- HP, elimination, simultaneity ---

        [Test]
        public void StackedDamage_ClampsHpAtZero_ButDamageResultsKeepFullDamage()
        {
            a.Ki = 1;
            b.Ki = 1;
            c.HP = 1;
            Act(a, ActionType.EnergyWave, c);
            Act(b, ActionType.EnergyWave, c);
            Act(c, ActionType.Gather);

            BattleResult result = Resolve();

            Assert.AreEqual(0, c.HP);
            Assert.AreEqual(2, result.DamageResults.Count);
            Assert.AreEqual(PlayerState.Eliminated, c.State);
            CollectionAssert.AreEqual(new[] { c.PlayerId }, result.EliminatedPlayerIds);
        }

        [Test]
        public void CommittedAttack_ResolvesEvenWhenOwnerDiesThisTurn()
        {
            // A -> B, B -> C, C -> A with everyone at HP 1: everyone's attack lands.
            a.Ki = 1;
            b.Ki = 1;
            c.Ki = 1;
            a.HP = 1;
            b.HP = 1;
            c.HP = 1;
            Act(a, ActionType.EnergyWave, b);
            Act(b, ActionType.EnergyWave, c);
            Act(c, ActionType.EnergyWave, a);

            BattleResult result = Resolve();

            Assert.AreEqual(ActionResultType.Hit, ResultOf(result, a));
            Assert.AreEqual(ActionResultType.Hit, ResultOf(result, b));
            Assert.AreEqual(ActionResultType.Hit, ResultOf(result, c));
            Assert.AreEqual(3, result.EliminatedPlayerIds.Count);
        }

        [Test]
        public void GatherStillAppliesKi_WhenOwnerIsEliminatedThisTurn()
        {
            a.Ki = 1;
            b.HP = 1;
            Act(a, ActionType.EnergyWave, b);
            Act(b, ActionType.Gather);
            Act(c, ActionType.Block);

            Resolve();

            Assert.AreEqual(PlayerState.Eliminated, b.State);
            Assert.AreEqual(1, b.Ki);
        }

        [Test]
        public void AlreadyEliminatedPlayer_IsSkipped()
        {
            c.State = PlayerState.Eliminated;
            c.HP = 0;
            Act(c, ActionType.None);
            Act(a, ActionType.Gather);
            Act(b, ActionType.Gather);

            BattleResult result = Resolve();

            Assert.AreEqual(0, c.Ki);
            Assert.AreEqual(0, result.EliminatedPlayerIds.Count);
            Assert.AreEqual(0, result.ActionResults.Count);
        }

        // --- Game outcome ---

        [Test]
        public void OneSurvivor_IsWinner()
        {
            a.Ki = 1;
            b.HP = 1;
            c.State = PlayerState.Eliminated;
            c.HP = 0;
            Act(a, ActionType.EnergyWave, b);
            Act(b, ActionType.Gather);
            Act(c, ActionType.None);

            BattleResult result = Resolve();

            Assert.AreEqual(GameOutcome.Winner, result.Outcome);
            Assert.AreEqual(a.PlayerId, result.WinnerPlayerId);
        }

        [Test]
        public void ZeroSurvivors_IsDraw()
        {
            a.Ki = 3;
            b.Ki = 3;
            a.HP = 1;
            b.HP = 1;
            c.State = PlayerState.Eliminated;
            c.HP = 0;
            Act(a, ActionType.SpiritBomb, b);
            Act(b, ActionType.SpiritBomb, a);
            Act(c, ActionType.None);

            BattleResult result = Resolve();

            Assert.AreEqual(GameOutcome.Draw, result.Outcome);
            Assert.AreEqual(BattleResult.NoWinner, result.WinnerPlayerId);
            Assert.AreEqual(2, result.EliminatedPlayerIds.Count);
        }

        [Test]
        public void TwoOrMoreSurvivors_IsOngoing()
        {
            a.Ki = 1;
            b.HP = 1;
            Act(a, ActionType.EnergyWave, b);
            Act(b, ActionType.Gather);
            Act(c, ActionType.Gather);

            BattleResult result = Resolve();

            Assert.AreEqual(GameOutcome.Ongoing, result.Outcome);
            Assert.AreEqual(BattleResult.NoWinner, result.WinnerPlayerId);
        }

        // --- Failed actions (DEC-005) ---

        [Test]
        public void InvalidAction_IsFailed_WithNoEffectAndNoKiChange()
        {
            a.Ki = 0;
            Act(a, ActionType.EnergyWave, b);
            Act(b, ActionType.Gather);
            Act(c, ActionType.Gather);

            BattleResult result = Resolve();

            Assert.AreEqual(ActionResultType.Failed, ResultOf(result, a));
            Assert.AreEqual(0, a.Ki);
            Assert.AreEqual(3, b.HP);
            Assert.AreEqual(1, b.Ki);
            Assert.AreEqual(1, c.Ki);
        }

        [Test]
        public void LivingPlayerWithoutAction_IsFailed()
        {
            Act(b, ActionType.Gather);
            Act(c, ActionType.Gather);

            BattleResult result = Resolve();

            Assert.AreEqual(ActionResultType.Failed, ResultOf(result, a));
            Assert.AreEqual(0, a.Ki);
        }

        [Test]
        public void ActionWithMismatchedPlayerId_IsFailed()
        {
            a.CurrentAction = new PlayerAction { PlayerId = b.PlayerId, ActionType = ActionType.Gather };
            Act(b, ActionType.Block);
            Act(c, ActionType.Block);

            BattleResult result = Resolve();

            Assert.AreEqual(ActionResultType.Failed, ResultOf(result, a));
            Assert.AreEqual(0, a.Ki);
            Assert.AreEqual(0, b.Ki);
        }

        [Test]
        public void FailedDefense_GivesNoProtection()
        {
            a.Ki = 1;
            b.Ki = 0;
            Act(a, ActionType.EnergyWave, b);
            Act(b, ActionType.Teleport);
            Act(c, ActionType.Block);

            BattleResult result = Resolve();

            Assert.AreEqual(ActionResultType.Failed, ResultOf(result, b));
            Assert.AreEqual(ActionResultType.Hit, ResultOf(result, a));
            Assert.AreEqual(2, b.HP);
        }
    }
}
