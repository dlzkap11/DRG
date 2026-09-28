using System.Collections.Generic;
using NUnit.Framework;

namespace DRG.Tests
{
    public class BattleLogFormatterTests
    {
        private GameSettings settings;
        private BattleResolver resolver;
        private Player a;
        private Player b;
        private List<Player> players;

        [SetUp]
        public void SetUp()
        {
            settings = new GameSettings();
            resolver = new BattleResolver(settings);
            a = new Player(0, "A", settings);
            b = new Player(1, "B", settings);
            players = new List<Player> { a, b };
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

        [Test]
        public void FormatTurn_ListsActionsAndAttackResults()
        {
            a.Ki = 1;
            Act(a, ActionType.EnergyWave, b);
            Act(b, ActionType.Gather);
            BattleResult result = resolver.ResolveTurn(players);

            List<string> lines = BattleLogFormatter.FormatTurn(3, players, result);

            CollectionAssert.AreEqual(new[]
            {
                "-- Turn 3 --",
                "A: Energy Wave -> B",
                "B: Gather",
                "Energy Wave A -> B: Hit"
            }, lines);
        }

        [Test]
        public void FormatTurn_ReportsEliminationAndWinner()
        {
            a.Ki = 3;
            b.HP = 1;
            Act(a, ActionType.SpiritBomb, b);
            Act(b, ActionType.Block);
            BattleResult result = resolver.ResolveTurn(players);

            List<string> lines = BattleLogFormatter.FormatTurn(1, players, result);

            CollectionAssert.Contains(lines, "Spirit Bomb A -> B: Hit");
            CollectionAssert.Contains(lines, "Eliminated: B");
            Assert.AreEqual("Winner: A", lines[lines.Count - 1]);
        }

        [Test]
        public void FormatTurn_ReportsDraw()
        {
            a.Ki = 1;
            b.Ki = 3;
            a.HP = 1;
            b.HP = 1;
            Act(a, ActionType.EnergyWave, b);
            Act(b, ActionType.SpiritBomb, a);
            BattleResult result = resolver.ResolveTurn(players);

            List<string> lines = BattleLogFormatter.FormatTurn(1, players, result);

            Assert.AreEqual("Draw: no survivors.", lines[lines.Count - 1]);
        }

        [Test]
        public void FormatTurn_SkipsEliminatedPlayersNoneAction()
        {
            Player c = new Player(2, "C", settings);
            c.State = PlayerState.Eliminated;
            c.HP = 0;
            players.Add(c);
            Act(a, ActionType.Block);
            Act(b, ActionType.Block);
            Act(c, ActionType.None);
            BattleResult result = resolver.ResolveTurn(players);

            List<string> lines = BattleLogFormatter.FormatTurn(1, players, result);

            CollectionAssert.AreEqual(new[] { "-- Turn 1 --", "A: Block", "B: Block" }, lines);
        }

        [Test]
        public void ActionName_CoversAllActions()
        {
            Assert.AreEqual("Gather", BattleLogFormatter.ActionName(ActionType.Gather));
            Assert.AreEqual("Energy Wave", BattleLogFormatter.ActionName(ActionType.EnergyWave));
            Assert.AreEqual("Block", BattleLogFormatter.ActionName(ActionType.Block));
            Assert.AreEqual("Teleport", BattleLogFormatter.ActionName(ActionType.Teleport));
            Assert.AreEqual("Spirit Bomb", BattleLogFormatter.ActionName(ActionType.SpiritBomb));
            Assert.AreEqual("-", BattleLogFormatter.ActionName(ActionType.None));
        }
    }
}
