using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace DRG.Tests
{
    // M5 plain C# parts: Room (spec §21), GameFlow / GameState (spec §3), GameResultSummary (spec §22). DEC-010.
    public class LobbyFlowTests
    {
        private GameSettings settings;

        [SetUp]
        public void SetUp()
        {
            settings = new GameSettings();
        }

        // --- Room ---

        [Test]
        public void Room_HostIsHumanAndNotReady_BotsAreReady()
        {
            Room room = new Room(settings, 2);

            List<RoomParticipant> participants = room.Participants;
            Assert.AreEqual(3, participants.Count);
            Assert.AreEqual("You", participants[0].Nickname);
            Assert.IsTrue(participants[0].IsHost);
            Assert.IsFalse(participants[0].IsBot);
            Assert.IsFalse(participants[0].IsReady);
            Assert.AreEqual("Bot 1", participants[1].Nickname);
            Assert.AreEqual("Bot 2", participants[2].Nickname);
            Assert.IsTrue(participants[1].IsBot && participants[1].IsReady);
            Assert.IsTrue(participants[2].IsBot && participants[2].IsReady);
        }

        [Test]
        public void Room_BotCountStaysBetweenZeroAndSeven()
        {
            Room room = new Room(settings, 0);
            Assert.IsFalse(room.CanRemoveBot);
            Assert.IsFalse(room.RemoveBot());

            for (int i = 0; i < 7; i++)
            {
                Assert.IsTrue(room.AddBot());
            }

            Assert.AreEqual(7, room.BotCount);
            Assert.AreEqual(settings.MaxPlayers, room.Participants.Count);
            Assert.IsFalse(room.CanAddBot);
            Assert.IsFalse(room.AddBot());
        }

        [TestCase(-3, 0)]
        [TestCase(3, 3)]
        [TestCase(20, 7)]
        public void Room_InitialBotCount_IsClamped(int requested, int expected)
        {
            Assert.AreEqual(expected, new Room(settings, requested).BotCount);
        }

        [Test]
        public void Room_CanStart_NeedsABotAndHostReady()
        {
            Room room = new Room(settings, 0);
            room.SetHostReady(true);
            Assert.IsFalse(room.CanStart, "No bots: only one participant.");

            room.AddBot();
            Assert.IsTrue(room.CanStart);

            room.SetHostReady(false);
            Assert.IsFalse(room.CanStart, "Host not Ready.");
        }

        // --- GameFlow ---

        [Test]
        public void GameFlow_FullCycle_LobbyRoomGameResultLobby()
        {
            GameFlow flow = new GameFlow(settings);
            Assert.AreEqual(GameState.Lobby, flow.State);

            Room room = flow.OpenRoom();
            Assert.AreEqual(GameState.Room, flow.State);
            room.AddBot();
            room.AddBot();
            room.SetHostReady(true);

            flow.StartGame();
            Assert.AreEqual(GameState.Game, flow.State);
            Assert.AreEqual(3, flow.BotCount);

            GameResultSummary result = GameResultSummary.Create(GameOutcome.Winner, 0, 5, new List<PlayerResult>
            {
                new PlayerResult { PlayerId = 0, Nickname = "You", IsHuman = true }
            });
            flow.FinishGame(result);
            Assert.AreEqual(GameState.Result, flow.State);
            Assert.AreSame(result, flow.LastResult);

            flow.ReturnToLobby();
            Assert.AreEqual(GameState.Lobby, flow.State);
            Assert.IsNull(flow.Room);
        }

        [Test]
        public void GameFlow_NewRoomKeepsPreviousBotCount_ButHostMustReadyAgain()
        {
            GameFlow flow = new GameFlow(settings);
            Room room = flow.OpenRoom();
            room.AddBot();
            room.AddBot();
            room.SetHostReady(true);
            flow.StartGame();
            flow.FinishGame(GameResultSummary.Create(GameOutcome.Draw, BattleResult.NoWinner, 3, new List<PlayerResult>()));
            flow.ReturnToLobby();

            Room next = flow.OpenRoom();

            Assert.AreEqual(3, next.BotCount);
            Assert.IsFalse(next.Host.IsReady);
        }

        [Test]
        public void GameFlow_StartGame_WhenRoomCannotStart_Throws()
        {
            GameFlow flow = new GameFlow(settings);
            flow.OpenRoom();

            Assert.Throws<InvalidOperationException>(() => flow.StartGame());
            Assert.AreEqual(GameState.Room, flow.State);
        }

        [Test]
        public void GameFlow_OutOfOrderTransitions_Throw()
        {
            GameFlow flow = new GameFlow(settings);

            Assert.Throws<InvalidOperationException>(() => flow.StartGame());
            Assert.Throws<InvalidOperationException>(() => flow.FinishGame(new GameResultSummary { Outcome = GameOutcome.Draw }));
            Assert.Throws<InvalidOperationException>(() => flow.ReturnToLobby());

            flow.OpenRoom();
            Assert.Throws<InvalidOperationException>(() => flow.OpenRoom());
        }

        [Test]
        public void GameFlow_FinishGame_RequiresFinishedResult()
        {
            GameFlow flow = new GameFlow(settings);
            Room room = flow.OpenRoom();
            room.SetHostReady(true);
            flow.StartGame();

            Assert.Throws<InvalidOperationException>(() => flow.FinishGame(null));
            Assert.Throws<InvalidOperationException>(() => flow.FinishGame(new GameResultSummary { Outcome = GameOutcome.Ongoing }));
            Assert.AreEqual(GameState.Game, flow.State);
        }

        // --- GameResultSummary ranking ---

        private static PlayerResult Result(int id, int eliminatedTurn)
        {
            return new PlayerResult { PlayerId = id, Nickname = "P" + id, IsHuman = id == 0, EliminatedTurn = eliminatedTurn };
        }

        [Test]
        public void Ranking_WinnerFirst_ThenLaterEliminationRanksHigher()
        {
            GameResultSummary summary = GameResultSummary.Create(GameOutcome.Winner, 2, 9, new List<PlayerResult>
            {
                Result(0, 3),
                Result(1, 7),
                Result(2, PlayerResult.NotEliminated),
                Result(3, 5)
            });

            CollectionAssert.AreEqual(new[] { 2, 1, 3, 0 }, Ids(summary));
            CollectionAssert.AreEqual(new[] { 1, 2, 3, 4 }, Ranks(summary));
        }

        [Test]
        public void Ranking_SameEliminationTurn_SharesRank_AndSkipsNext()
        {
            GameResultSummary summary = GameResultSummary.Create(GameOutcome.Winner, 0, 8, new List<PlayerResult>
            {
                Result(0, PlayerResult.NotEliminated),
                Result(1, 6),
                Result(2, 6),
                Result(3, 2)
            });

            CollectionAssert.AreEqual(new[] { 0, 1, 2, 3 }, Ids(summary));
            CollectionAssert.AreEqual(new[] { 1, 2, 2, 4 }, Ranks(summary));
        }

        [Test]
        public void Ranking_Draw_LastPlayersShareFirstPlace()
        {
            GameResultSummary summary = GameResultSummary.Create(GameOutcome.Draw, BattleResult.NoWinner, 10, new List<PlayerResult>
            {
                Result(0, 4),
                Result(1, 10),
                Result(2, 10)
            });

            CollectionAssert.AreEqual(new[] { 1, 2, 0 }, Ids(summary));
            CollectionAssert.AreEqual(new[] { 1, 1, 3 }, Ranks(summary));
        }

        [Test]
        public void FromSession_FinishedGame_RanksConsistentWithEliminationTurns()
        {
            LocalBattleSession session = new LocalBattleSession(settings, 7, 31);
            session.Human.State = PlayerState.Eliminated;
            session.Human.HP = 0;
            for (int i = 0; i < 1000 && !session.IsGameOver; i++)
            {
                session.StartNextTurn();
            }

            Assert.IsTrue(session.IsGameOver);
            GameResultSummary summary = GameResultSummary.FromSession(session);

            Assert.AreEqual(session.TurnNumber, summary.Turns);
            Assert.AreEqual(8, summary.Players.Count);
            Assert.AreEqual(1, summary.Players[0].Rank);
            if (summary.Outcome == GameOutcome.Winner)
            {
                Assert.AreEqual(summary.WinnerPlayerId, summary.Players[0].PlayerId);
                Assert.AreEqual(PlayerResult.NotEliminated, summary.Players[0].EliminatedTurn);
            }

            PlayerResult human = summary.Players.Find(p => p.IsHuman);
            Assert.AreEqual(0, human.EliminatedTurn, "The human was eliminated before turn 1 in this setup.");
            Assert.AreEqual(summary.Players[summary.Players.Count - 1], human, "Earliest elimination ranks last.");

            for (int i = 1; i < summary.Players.Count; i++)
            {
                Assert.GreaterOrEqual(summary.Players[i].Rank, summary.Players[i - 1].Rank);
            }

            // Every bot that was eliminated has its elimination turn recorded within the game.
            for (int i = 0; i < summary.Players.Count; i++)
            {
                PlayerResult player = summary.Players[i];
                if (!player.IsHuman && player.EliminatedTurn != PlayerResult.NotEliminated)
                {
                    Assert.That(player.EliminatedTurn, Is.InRange(1, summary.Turns), player.Nickname);
                }
            }
        }

        private static int[] Ids(GameResultSummary summary)
        {
            int[] ids = new int[summary.Players.Count];
            for (int i = 0; i < ids.Length; i++)
            {
                ids[i] = summary.Players[i].PlayerId;
            }

            return ids;
        }

        private static int[] Ranks(GameResultSummary summary)
        {
            int[] ranks = new int[summary.Players.Count];
            for (int i = 0; i < ranks.Length; i++)
            {
                ranks[i] = summary.Players[i].Rank;
            }

            return ranks;
        }
    }
}
