using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace DRG.Tests
{
    // Binds the real LobbyScreen.uxml / ResultScreen.uxml and checks the M5 scenes (DEC-010).
    public class LobbyResultViewTests
    {
        private const string LobbyUxmlPath = "Assets/_Project/UI/LobbyScreen.uxml";
        private const string ResultUxmlPath = "Assets/_Project/UI/ResultScreen.uxml";
        private const string LobbyScenePath = "Assets/_Project/Scenes/LobbyScene.unity";
        private const string GameScenePath = "Assets/_Project/Scenes/GameScene.unity";
        private const string ResultScenePath = "Assets/_Project/Scenes/ResultScene.unity";

        private static VisualElement Load(string path)
        {
            VisualTreeAsset uxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(path);
            Assert.IsNotNull(uxml, "Missing " + path);
            return uxml.CloneTree();
        }

        private static string RowText(VisualElement row, string className)
        {
            return row.Q<Label>(className: className).text;
        }

        // --- Lobby ---

        [Test]
        public void Lobby_ShowsParticipants_AndStartNeedsReady()
        {
            VisualElement root = Load(LobbyUxmlPath);
            LobbyView view = new LobbyView(root);
            Room room = new Room(new GameSettings(), 1);
            view.Bind(room);

            VisualElement list = root.Q("participant-list");
            Assert.AreEqual(2, list.childCount);
            Assert.AreEqual("You (Host)", RowText(list[0], "participant-name"));
            Assert.AreEqual("Not Ready", RowText(list[0], "participant-ready"));
            Assert.AreEqual("Bot 1 (AI)", RowText(list[1], "participant-name"));
            Assert.AreEqual("Ready", RowText(list[1], "participant-ready"));
            Assert.IsFalse(root.Q<Button>("start-button").enabledSelf);
            Assert.AreEqual("Ready", root.Q<Button>("ready-button").text);

            view.ToggleReady();

            Assert.IsTrue(room.Host.IsReady);
            Assert.IsTrue(root.Q<Button>("start-button").enabledSelf);
            Assert.AreEqual("Unready", root.Q<Button>("ready-button").text);
            Assert.AreEqual("Ready", RowText(root.Q("participant-list")[0], "participant-ready"));
        }

        [Test]
        public void Lobby_AddAndRemoveBots_RespectLimits()
        {
            VisualElement root = Load(LobbyUxmlPath);
            LobbyView view = new LobbyView(root);
            Room room = new Room(new GameSettings(), 0);
            view.Bind(room);

            Assert.IsFalse(root.Q<Button>("remove-bot-button").enabledSelf);
            Assert.AreEqual("Add at least one AI to start.", root.Q<Label>("lobby-status-label").text);

            for (int i = 0; i < 10; i++)
            {
                view.AddBot();
            }

            Assert.AreEqual(7, room.BotCount);
            Assert.AreEqual(8, root.Q("participant-list").childCount);
            Assert.IsFalse(root.Q<Button>("add-bot-button").enabledSelf);

            view.RemoveBot();
            Assert.AreEqual(6, room.BotCount);
            Assert.IsTrue(root.Q<Button>("add-bot-button").enabledSelf);
        }

        [Test]
        public void Lobby_Start_RaisesEventOnlyWhenRoomCanStart()
        {
            VisualElement root = Load(LobbyUxmlPath);
            LobbyView view = new LobbyView(root);
            Room room = new Room(new GameSettings(), 1);
            view.Bind(room);
            int requests = 0;
            view.StartRequested += () => requests++;

            view.Start();
            Assert.AreEqual(0, requests, "Host is not Ready yet.");

            view.ToggleReady();
            view.Start();
            Assert.AreEqual(1, requests);
        }

        // --- Result ---

        [Test]
        public void Result_ShowsHeadlineTurnsAndRanking()
        {
            VisualElement root = Load(ResultUxmlPath);
            ResultView view = new ResultView(root);
            GameResultSummary summary = GameResultSummary.Create(GameOutcome.Winner, 2, 12, new List<PlayerResult>
            {
                new PlayerResult { PlayerId = 0, Nickname = "You", IsHuman = true, EliminatedTurn = 4 },
                new PlayerResult { PlayerId = 1, Nickname = "Bot 1", EliminatedTurn = 12 },
                new PlayerResult { PlayerId = 2, Nickname = "Bot 2", EliminatedTurn = PlayerResult.NotEliminated }
            });

            view.Bind(summary);

            Assert.AreEqual("Bot 2 wins.", root.Q<Label>("result-headline").text);
            Assert.AreEqual("Turns played: 12", root.Q<Label>("result-turns").text);
            VisualElement list = root.Q("ranking-list");
            Assert.AreEqual(3, list.childCount);
            Assert.AreEqual("#1", RowText(list[0], "ranking-rank"));
            Assert.AreEqual("Bot 2", RowText(list[0], "ranking-name"));
            Assert.AreEqual("Winner", RowText(list[0], "ranking-standing"));
            Assert.AreEqual("Eliminated on turn 12", RowText(list[1], "ranking-standing"));
            Assert.AreEqual("#3", RowText(list[2], "ranking-rank"));
            Assert.IsTrue(list[2].ClassListContains("is-human"));
        }

        [TestCase(GameOutcome.Draw, BattleResult.NoWinner, "Draw - no survivors.")]
        [TestCase(GameOutcome.Winner, 0, "You win!")]
        public void Result_Headline(GameOutcome outcome, int winner, string expected)
        {
            VisualElement root = Load(ResultUxmlPath);
            ResultView view = new ResultView(root);

            view.Bind(GameResultSummary.Create(outcome, winner, 5, new List<PlayerResult>
            {
                new PlayerResult { PlayerId = 0, Nickname = "You", IsHuman = true, EliminatedTurn = outcome == GameOutcome.Draw ? 5 : PlayerResult.NotEliminated },
                new PlayerResult { PlayerId = 1, Nickname = "Bot 1", EliminatedTurn = 5 }
            }));

            Assert.AreEqual(expected, root.Q<Label>("result-headline").text);
        }

        [Test]
        public void Result_BackToLobby_RaisesEvent()
        {
            ResultView view = new ResultView(Load(ResultUxmlPath));
            bool requested = false;
            view.LobbyRequested += () => requested = true;

            view.RequestLobby();

            Assert.IsTrue(requested);
        }

        // --- Scenes ---

        [Test]
        public void BuildSettings_LobbyGameResultInOrder()
        {
            EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
            Assert.AreEqual(LobbyScenePath, scenes[0].path);
            Assert.AreEqual(GameScenePath, scenes[1].path);
            Assert.AreEqual(ResultScenePath, scenes[2].path);
            Assert.IsTrue(scenes[0].enabled && scenes[1].enabled && scenes[2].enabled);
        }

        [TestCase(LobbyScenePath, LobbyUxmlPath, typeof(LobbyScreen))]
        [TestCase(ResultScenePath, ResultUxmlPath, typeof(ResultScreen))]
        public void Scene_HasScreenWithUiReferences(string scenePath, string uxmlPath, System.Type screenType)
        {
            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
            try
            {
                Component screen = null;
                GameObject[] roots = scene.GetRootGameObjects();
                for (int i = 0; i < roots.Length && screen == null; i++)
                {
                    screen = roots[i].GetComponentInChildren(screenType);
                }

                Assert.IsNotNull(screen, scenePath + " has no " + screenType.Name);
                UIDocument document = screen.GetComponent<UIDocument>();
                Assert.IsNotNull(document.panelSettings);
                Assert.IsNotNull(document.panelSettings.themeStyleSheet);
                Assert.AreEqual(uxmlPath, AssetDatabase.GetAssetPath(document.visualTreeAsset));
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }
    }
}
