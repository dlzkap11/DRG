using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace DRG.Tests
{
    public class SimulationTests
    {
        private GameSettings settings;

        [SetUp]
        public void SetUp()
        {
            settings = new GameSettings();
        }

        // --- RandomActionPolicy ---

        [Test]
        public void Policy_AlwaysChoosesValidAction_InVariedStates()
        {
            Random stateRandom = new Random(1);
            RandomActionPolicy policy = new RandomActionPolicy(2, settings);
            ActionValidator validator = new ActionValidator(settings);

            for (int round = 0; round < 500; round++)
            {
                List<Player> players = new List<Player>();
                int count = stateRandom.Next(2, settings.MaxPlayers + 1);
                for (int i = 0; i < count; i++)
                {
                    Player player = new Player(i, "P" + i, settings);
                    player.Ki = stateRandom.Next(0, settings.MaxKi + 1);
                    player.HP = stateRandom.Next(1, settings.MaxHP + 1);
                    players.Add(player);
                }

                // Eliminate some opponents of player 0, but keep player 0 alive.
                for (int i = 1; i < count; i++)
                {
                    if (stateRandom.Next(3) == 0)
                    {
                        players[i].State = PlayerState.Eliminated;
                        players[i].HP = 0;
                    }
                }

                PlayerAction action = policy.ChooseAction(players[0], players);
                Assert.AreEqual(ActionValidationResult.Valid, validator.Validate(action, players),
                    "Round " + round + " produced " + action.ActionType + " -> " + action.TargetPlayerId);
            }
        }

        [Test]
        public void Policy_UsesEveryLegalActionType()
        {
            RandomActionPolicy policy = new RandomActionPolicy(3, settings);
            Player self = new Player(0, "A", settings);
            Player other = new Player(1, "B", settings);
            List<Player> players = new List<Player> { self, other };
            self.Ki = 1;

            HashSet<ActionType> seen = new HashSet<ActionType>();
            for (int i = 0; i < 200; i++)
            {
                seen.Add(policy.ChooseAction(self, players).ActionType);
            }

            // At Ki 1: Gather, Energy Wave, Block, Teleport are legal; Spirit Bomb is not.
            CollectionAssert.AreEquivalent(
                new[] { ActionType.Gather, ActionType.EnergyWave, ActionType.Block, ActionType.Teleport },
                seen);
        }

        // --- BattleSimulator ---

        [Test]
        public void Simulator_SameSeed_GivesIdenticalStats()
        {
            BattleSimulator simulator = new BattleSimulator(settings);
            SimulationStats first = simulator.Run(4, 50, 42, BattleSimulator.DefaultTurnCap);
            SimulationStats second = simulator.Run(4, 50, 42, BattleSimulator.DefaultTurnCap);

            CollectionAssert.AreEqual(first.GameLengths, second.GameLengths);
            CollectionAssert.AreEqual(first.ActionCounts, second.ActionCounts);
            CollectionAssert.AreEqual(first.WinsBySeat, second.WinsBySeat);
            Assert.AreEqual(first.DrawGames, second.DrawGames);
        }

        [TestCase(2)]
        [TestCase(8)]
        public void Simulator_EveryGameIsCounted_AndNoActionFails(int playerCount)
        {
            BattleSimulator simulator = new BattleSimulator(settings);
            SimulationStats stats = simulator.Run(playerCount, 100, 7, BattleSimulator.DefaultTurnCap);

            Assert.AreEqual(100, stats.GameLengths.Count);
            Assert.AreEqual(100, stats.WinnerGames + stats.DrawGames + stats.UnfinishedGames);

            int seatWins = 0;
            for (int i = 0; i < stats.WinsBySeat.Length; i++)
            {
                seatWins += stats.WinsBySeat[i];
            }

            Assert.AreEqual(stats.WinnerGames, seatWins);
            Assert.AreEqual(0, stats.FailedActions);
        }

        [Test]
        public void Simulator_TurnCap_StopsGameAndCountsItUnfinished()
        {
            BattleSimulator simulator = new BattleSimulator(settings);
            SimulationStats stats = simulator.Run(2, 20, 11, 1);

            // After one turn from the starting state nobody can have lost 3 HP, so every game hits the cap.
            Assert.AreEqual(20, stats.UnfinishedGames);
            for (int i = 0; i < stats.GameLengths.Count; i++)
            {
                Assert.AreEqual(1, stats.GameLengths[i]);
            }
        }

        [TestCase(1)]
        [TestCase(9)]
        public void Simulator_InvalidPlayerCount_Throws(int playerCount)
        {
            BattleSimulator simulator = new BattleSimulator(settings);
            Assert.Throws<ArgumentOutOfRangeException>(() => simulator.Run(playerCount, 1, 0, BattleSimulator.DefaultTurnCap));
        }

        // --- SimulationReport ---

        [Test]
        public void Report_ContainsEveryBatchRow()
        {
            BattleSimulator simulator = new BattleSimulator(settings);
            List<SimulationStats> batches = new List<SimulationStats>
            {
                simulator.Run(2, 10, 5, BattleSimulator.DefaultTurnCap),
                simulator.Run(3, 10, 5, BattleSimulator.DefaultTurnCap)
            };

            string report = SimulationReport.ToMarkdown(batches);

            StringAssert.Contains("## 1. 게임 길이와 결과", report);
            StringAssert.Contains("| 2 | 10 |", report);
            StringAssert.Contains("| 3 | 10 |", report);
            StringAssert.Contains("`Failed`가 된 행동: 0건", report);
        }

        // --- BotAI policy through the simulator ---

        [TestCase(2)]
        [TestCase(8)]
        public void BotAISimulation_EveryGameIsCounted_AndNoActionFails(int playerCount)
        {
            BattleSimulator simulator = new BattleSimulator(settings);
            SimulationStats stats = simulator.Run(playerCount, 100, 7, BattleSimulator.DefaultTurnCap, new BotAI(7, settings));

            Assert.AreEqual(100, stats.GameLengths.Count);
            Assert.AreEqual(100, stats.WinnerGames + stats.DrawGames + stats.UnfinishedGames);
            Assert.AreEqual(0, stats.FailedActions);
            Assert.Greater(stats.ActionCounts[(int)ActionType.SpiritBomb], 0, "BotAI should reach max Ki and fire Spirit Bombs.");
        }

        [Test]
        public void BotAISimulation_SameSeed_GivesIdenticalStats()
        {
            BattleSimulator simulator = new BattleSimulator(settings);
            SimulationStats first = simulator.Run(4, 50, 42, BattleSimulator.DefaultTurnCap, new BotAI(42, settings));
            SimulationStats second = simulator.Run(4, 50, 42, BattleSimulator.DefaultTurnCap, new BotAI(42, settings));

            CollectionAssert.AreEqual(first.GameLengths, second.GameLengths);
            CollectionAssert.AreEqual(first.ActionCounts, second.ActionCounts);
            CollectionAssert.AreEqual(first.WinsBySeat, second.WinsBySeat);
        }

        [Test]
        public void RandomOverload_MatchesExplicitRandomPolicy()
        {
            BattleSimulator simulator = new BattleSimulator(settings);
            SimulationStats implicitPolicy = simulator.Run(3, 30, 13, BattleSimulator.DefaultTurnCap);
            SimulationStats explicitPolicy = simulator.Run(3, 30, 13, BattleSimulator.DefaultTurnCap, new RandomActionPolicy(13, settings));

            CollectionAssert.AreEqual(implicitPolicy.GameLengths, explicitPolicy.GameLengths);
            CollectionAssert.AreEqual(implicitPolicy.ActionCounts, explicitPolicy.ActionCounts);
        }

        [Test]
        public void PolicyReport_UsesGivenTitleAndDescription()
        {
            BattleSimulator simulator = new BattleSimulator(settings);
            List<SimulationStats> batches = new List<SimulationStats>
            {
                simulator.Run(2, 10, 5, BattleSimulator.DefaultTurnCap, new BotAI(5, settings))
            };

            string report = SimulationReport.ToMarkdown(batches,
                SimulationReport.BotAIPolicyTitle, SimulationReport.BotAIPolicyDescription, SimulationReport.BotAIPolicyCaution);

            StringAssert.StartsWith("# DRG 자동 시뮬레이션 리포트 (BotAI)", report);
            StringAssert.Contains("- 정책: " + SimulationReport.BotAIPolicyDescription, report);
            StringAssert.Contains("> **해석 주의**: " + SimulationReport.BotAIPolicyCaution, report);
        }

        [Test]
        public void ComparisonReport_HasOneRowPerPolicyPerPlayerCount()
        {
            BattleSimulator simulator = new BattleSimulator(settings);
            List<SimulationStats> baseline = new List<SimulationStats>
            {
                simulator.Run(2, 10, 5, BattleSimulator.DefaultTurnCap),
                simulator.Run(3, 10, 5, BattleSimulator.DefaultTurnCap)
            };
            List<SimulationStats> candidate = new List<SimulationStats>
            {
                simulator.Run(2, 10, 5, BattleSimulator.DefaultTurnCap, new BotAI(5, settings)),
                simulator.Run(3, 10, 5, BattleSimulator.DefaultTurnCap, new BotAI(5, settings))
            };

            string report = SimulationReport.ToComparisonMarkdown(baseline, candidate, "Random", "Bot");

            StringAssert.Contains("# DRG 시뮬레이션 정책 비교 (Random vs Bot)", report);
            StringAssert.Contains("| 2 | Random |", report);
            StringAssert.Contains("| 2 | Bot |", report);
            StringAssert.Contains("| 3 | Random |", report);
            StringAssert.Contains("| 3 | Bot |", report);
        }

        [Test]
        public void ComparisonReport_MismatchedPlayerCounts_Throws()
        {
            BattleSimulator simulator = new BattleSimulator(settings);
            List<SimulationStats> baseline = new List<SimulationStats> { simulator.Run(2, 5, 1, BattleSimulator.DefaultTurnCap) };
            List<SimulationStats> candidate = new List<SimulationStats> { simulator.Run(3, 5, 1, BattleSimulator.DefaultTurnCap) };

            Assert.Throws<ArgumentException>(() => SimulationReport.ToComparisonMarkdown(baseline, candidate, "A", "B"));
        }
    }
}
