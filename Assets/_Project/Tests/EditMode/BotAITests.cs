using System.Collections.Generic;
using NUnit.Framework;

namespace DRG.Tests
{
    // Checks the DEC-008 bot rules. Seeds are fixed, so the ratio checks are deterministic.
    public class BotAITests
    {
        private const int Draws = 2000;

        private GameSettings settings;
        private Player bot;
        private Player opponent;
        private List<Player> players;

        [SetUp]
        public void SetUp()
        {
            settings = new GameSettings();
            bot = new Player(0, "Bot", settings);
            opponent = new Player(1, "Opponent", settings);
            players = new List<Player> { bot, opponent };
        }

        private Dictionary<ActionType, int> CountChoices(int ki, int hp, int seed)
        {
            bot.Ki = ki;
            bot.HP = hp;
            BotAI ai = new BotAI(seed, settings);
            Dictionary<ActionType, int> counts = new Dictionary<ActionType, int>();
            for (int i = 0; i < Draws; i++)
            {
                ActionType type = ai.ChooseAction(bot, players).ActionType;
                int count;
                counts.TryGetValue(type, out count);
                counts[type] = count + 1;
            }

            return counts;
        }

        private static int CountOf(Dictionary<ActionType, int> counts, ActionType type)
        {
            int count;
            counts.TryGetValue(type, out count);
            return count;
        }

        private static void AssertShare(Dictionary<ActionType, int> counts, ActionType type, double expected)
        {
            double share = (double)CountOf(counts, type) / Draws;
            Assert.AreEqual(expected, share, 0.05, type + " share");
        }

        private static void AssertOnly(Dictionary<ActionType, int> counts, params ActionType[] allowed)
        {
            List<ActionType> allowedList = new List<ActionType>(allowed);
            foreach (KeyValuePair<ActionType, int> pair in counts)
            {
                Assert.IsTrue(allowedList.Contains(pair.Key), "Unexpected action " + pair.Key);
            }
        }

        // --- Base rules (HP above the low threshold) ---

        [Test]
        public void KiZero_AlwaysGathers()
        {
            Dictionary<ActionType, int> counts = CountChoices(0, 3, 1);

            Assert.AreEqual(Draws, CountOf(counts, ActionType.Gather));
        }

        [TestCase(1)]
        [TestCase(2)]
        public void KiOneOrTwo_EnergyWaveOrGather_HalfEach(int ki)
        {
            Dictionary<ActionType, int> counts = CountChoices(ki, 3, 2);

            AssertOnly(counts, ActionType.EnergyWave, ActionType.Gather);
            AssertShare(counts, ActionType.EnergyWave, 0.5);
            AssertShare(counts, ActionType.Gather, 0.5);
        }

        [Test]
        public void MaxKi_SpiritBombOrEnergyWave_HalfEach()
        {
            Dictionary<ActionType, int> counts = CountChoices(3, 3, 3);

            AssertOnly(counts, ActionType.SpiritBomb, ActionType.EnergyWave);
            AssertShare(counts, ActionType.SpiritBomb, 0.5);
            AssertShare(counts, ActionType.EnergyWave, 0.5);
        }

        [Test]
        public void HpTwo_NeverDefends()
        {
            Dictionary<ActionType, int> counts = CountChoices(1, 2, 4);

            Assert.AreEqual(0, CountOf(counts, ActionType.Block));
            Assert.AreEqual(0, CountOf(counts, ActionType.Teleport));
        }

        // --- Low HP defense ---

        [Test]
        public void LowHp_KiZero_Blocks40Percent_OtherwiseGathers()
        {
            Dictionary<ActionType, int> counts = CountChoices(0, 1, 5);

            AssertOnly(counts, ActionType.Block, ActionType.Gather);
            AssertShare(counts, ActionType.Block, 0.4);
            AssertShare(counts, ActionType.Gather, 0.6);
        }

        [Test]
        public void LowHp_WithKi_DefendsWithBlockOrTeleport_40PercentTotal()
        {
            Dictionary<ActionType, int> counts = CountChoices(1, 1, 6);

            AssertOnly(counts, ActionType.Block, ActionType.Teleport, ActionType.EnergyWave, ActionType.Gather);
            AssertShare(counts, ActionType.Block, 0.2);
            AssertShare(counts, ActionType.Teleport, 0.2);
            AssertShare(counts, ActionType.EnergyWave, 0.3);
            AssertShare(counts, ActionType.Gather, 0.3);
        }

        [Test]
        public void LowHp_MaxKi_DefendsOrAttacks()
        {
            Dictionary<ActionType, int> counts = CountChoices(3, 1, 7);

            AssertOnly(counts, ActionType.Block, ActionType.Teleport, ActionType.SpiritBomb, ActionType.EnergyWave);
            AssertShare(counts, ActionType.Block, 0.2);
            AssertShare(counts, ActionType.Teleport, 0.2);
            AssertShare(counts, ActionType.SpiritBomb, 0.3);
            AssertShare(counts, ActionType.EnergyWave, 0.3);
        }

        // --- Targets, validity, determinism ---

        [Test]
        public void Attacks_TargetOnlyLivingOpponents_Uniformly()
        {
            Player second = new Player(2, "Second", settings);
            Player eliminated = new Player(3, "Eliminated", settings);
            eliminated.State = PlayerState.Eliminated;
            eliminated.HP = 0;
            players.Add(second);
            players.Add(eliminated);
            bot.Ki = 1;
            BotAI ai = new BotAI(8, settings);

            Dictionary<int, int> targets = new Dictionary<int, int>();
            int attacks = 0;
            for (int i = 0; i < Draws; i++)
            {
                PlayerAction action = ai.ChooseAction(bot, players);
                if (action.ActionType != ActionType.EnergyWave)
                {
                    continue;
                }

                attacks++;
                int count;
                targets.TryGetValue(action.TargetPlayerId, out count);
                targets[action.TargetPlayerId] = count + 1;
            }

            CollectionAssert.AreEquivalent(new[] { opponent.PlayerId, second.PlayerId }, targets.Keys);
            Assert.AreEqual(0.5, (double)targets[opponent.PlayerId] / attacks, 0.05);
        }

        [Test]
        public void EveryChoice_IsValid_AcrossStates()
        {
            ActionValidator validator = new ActionValidator(settings);
            BotAI ai = new BotAI(9, settings);
            for (int ki = 0; ki <= settings.MaxKi; ki++)
            {
                for (int hp = 1; hp <= settings.MaxHP; hp++)
                {
                    bot.Ki = ki;
                    bot.HP = hp;
                    for (int i = 0; i < 200; i++)
                    {
                        PlayerAction action = ai.ChooseAction(bot, players);
                        Assert.AreEqual(ActionValidationResult.Valid, validator.Validate(action, players),
                            "Ki " + ki + ", HP " + hp + ": " + action.ActionType);
                    }
                }
            }
        }

        [Test]
        public void SameSeed_GivesSameChoices()
        {
            bot.Ki = 1;
            bot.HP = 1;
            BotAI first = new BotAI(10, settings);
            BotAI second = new BotAI(10, settings);

            for (int i = 0; i < 100; i++)
            {
                PlayerAction a = first.ChooseAction(bot, players);
                PlayerAction b = second.ChooseAction(bot, players);
                Assert.AreEqual(a.ActionType, b.ActionType);
                Assert.AreEqual(a.TargetPlayerId, b.TargetPlayerId);
            }
        }

        [Test]
        public void BotPlayer_UsesBotAIForItsOwnSeat()
        {
            bot.Ki = 0;
            BotPlayer botPlayer = new BotPlayer(bot.PlayerId, new BotAI(11, settings));

            PlayerAction action = botPlayer.ChooseAction(players);

            Assert.AreEqual(bot.PlayerId, action.PlayerId);
            Assert.AreEqual(ActionType.Gather, action.ActionType);
        }
    }
}
