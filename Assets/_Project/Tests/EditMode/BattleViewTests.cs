using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace DRG.Tests
{
    // Binds the real BattleScreen.uxml to a session and drives it through the view's public input methods.
    public class BattleViewTests
    {
        private const string UxmlPath = "Assets/_Project/UI/BattleScreen.uxml";
        private const string ScenePath = "Assets/_Project/Scenes/GameScene.unity";

        private VisualElement root;
        private BattleView view;
        private LocalBattleSession session;

        [SetUp]
        public void SetUp()
        {
            VisualTreeAsset uxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UxmlPath);
            Assert.IsNotNull(uxml, "Missing " + UxmlPath);
            root = uxml.CloneTree();
            view = new BattleView(root);
            session = new LocalBattleSession(new GameSettings(), 1, 321);
            session.StartNextTurn();
            view.Bind(session);
        }

        private void BindNewSession(int botCount, int seed)
        {
            session = new LocalBattleSession(new GameSettings(), botCount, seed);
            session.StartNextTurn();
            view.Bind(session);
        }

        private Button ButtonNamed(string name)
        {
            return root.Q<Button>(name);
        }

        private string CardText(int playerId, string className)
        {
            return root.Q<VisualElement>("player-card-" + playerId).Q<Label>(className: className).text;
        }

        private static bool IsShown(VisualElement element)
        {
            return element.style.display.value != DisplayStyle.None;
        }

        [Test]
        public void InitialScreen_ShowsTurnAndHidesOpponentAction()
        {
            Assert.AreEqual("Turn 1", root.Q<Label>("turn-label").text);
            Assert.AreEqual("Action: ? (hidden)", CardText(1, "player-action"));
            Assert.AreEqual("Locked", CardText(1, "player-status"));
            Assert.AreEqual("Choosing...", CardText(0, "player-status"));
            Assert.AreEqual("HP 3 / 3", root.Q<VisualElement>("player-card-0").Q<Label>(className: "player-stat").text);
        }

        [Test]
        public void InitialScreen_EnablesOnlyLegalActions()
        {
            Assert.IsTrue(ButtonNamed("gather-button").enabledSelf);
            Assert.IsTrue(ButtonNamed("block-button").enabledSelf);
            Assert.IsFalse(ButtonNamed("energy-wave-button").enabledSelf);
            Assert.IsFalse(ButtonNamed("teleport-button").enabledSelf);
            Assert.IsFalse(ButtonNamed("spirit-bomb-button").enabledSelf);
            Assert.IsFalse(ButtonNamed("lock-button").enabledSelf);
            Assert.IsTrue(IsShown(root.Q("action-panel")));
            Assert.IsTrue(IsShown(root.Q("lock-row")));
            Assert.IsFalse(IsShown(root.Q("target-panel")));
            Assert.IsFalse(IsShown(root.Q("result-panel")));
            Assert.IsFalse(IsShown(root.Q("game-over-panel")));
        }

        [Test]
        public void SelectingIllegalAction_IsIgnored()
        {
            view.SelectAction(ActionType.EnergyWave);

            Assert.IsFalse(ButtonNamed("energy-wave-button").ClassListContains("selected"));
            Assert.IsFalse(ButtonNamed("lock-button").enabledSelf);
        }

        [Test]
        public void SelectAndLock_RevealsTurnAndWritesLog()
        {
            view.SelectAction(ActionType.Gather);
            Assert.IsTrue(ButtonNamed("gather-button").ClassListContains("selected"));
            Assert.IsTrue(ButtonNamed("lock-button").enabledSelf);

            view.LockSelection();

            Assert.IsTrue(session.IsRevealed);
            Assert.IsTrue(IsShown(root.Q("result-panel")));
            Assert.IsTrue(IsShown(ButtonNamed("next-turn-button")));
            Assert.AreNotEqual("Action: ? (hidden)", CardText(1, "player-action"));
            Assert.AreEqual("Action: Gather", CardText(0, "player-action"));
            Assert.Greater(root.Q<ScrollView>("battle-log").contentContainer.childCount, 0);
            Assert.IsFalse(ButtonNamed("gather-button").enabledSelf);
        }

        [Test]
        public void NextTurn_StartsTurnTwo_AndHidesResult()
        {
            view.SelectAction(ActionType.Block);
            view.LockSelection();

            view.NextTurn();

            Assert.AreEqual("Turn 2", root.Q<Label>("turn-label").text);
            Assert.IsFalse(IsShown(root.Q("result-panel")));
            Assert.AreEqual("Action: ? (hidden)", CardText(1, "player-action"));
        }

        [Test]
        public void AttackFlow_RequiresTargetBeforeLock()
        {
            view.SelectAction(ActionType.Gather);
            view.LockSelection();
            view.NextTurn();
            Assert.AreEqual(1, session.Human.Ki);

            view.SelectAction(ActionType.EnergyWave);
            Assert.IsTrue(IsShown(root.Q("target-panel")));
            Assert.IsNotNull(ButtonNamed("target-button-1"));
            Assert.IsNull(ButtonNamed("target-button-0"), "The human must not be offered as their own target.");
            Assert.IsFalse(ButtonNamed("lock-button").enabledSelf);

            view.SelectTarget(1);
            Assert.IsTrue(ButtonNamed("lock-button").enabledSelf);

            view.LockSelection();
            Assert.IsTrue(session.IsRevealed);
            Assert.AreEqual("Action: Energy Wave -> Bot 1", CardText(0, "player-action"));
        }

        [Test]
        public void PlayingToTheEnd_ShowsGameOverPanel()
        {
            ActionType[] preference = { ActionType.SpiritBomb, ActionType.EnergyWave, ActionType.Gather, ActionType.Block };
            for (int turn = 0; turn < BattleSimulator.DefaultTurnCap && !session.IsGameOver; turn++)
            {
                if (turn > 0)
                {
                    view.NextTurn();
                }

                for (int i = 0; i < preference.Length; i++)
                {
                    view.SelectAction(preference[i]);
                    if (ButtonNamed(ButtonNameFor(preference[i])).ClassListContains("selected"))
                    {
                        break;
                    }
                }

                view.SelectTarget(1);
                view.LockSelection();
                Assert.IsTrue(session.IsRevealed, "Turn " + session.TurnNumber + " did not resolve.");
            }

            Assert.IsTrue(session.IsGameOver);
            Assert.IsTrue(IsShown(root.Q("game-over-panel")));
            Assert.IsFalse(IsShown(ButtonNamed("next-turn-button")));
            Assert.IsFalse(IsShown(root.Q("action-panel")));
            Assert.IsFalse(IsShown(root.Q("lock-row")));
            Assert.IsNotEmpty(root.Q<Label>("game-over-label").text);
        }

        // --- Reveal panel (M4: outcomes only) ---

        [Test]
        public void RevealPanel_ShowsOutcomesOnly()
        {
            // A fresh BotAI at Ki 0 always gathers, so this turn has no attacks.
            view.SelectAction(ActionType.Gather);
            view.LockSelection();

            string reveal = root.Q<Label>("result-label").text;
            Assert.AreEqual("No attacks this turn.", reveal);
            StringAssert.DoesNotContain("You: Gather", reveal);
        }

        // --- Results (M5: the bot-count setup moved to the Lobby) ---

        [Test]
        public void GameScreen_HasNoSetupPanel_AndShowsResultsButtonOnGameOver()
        {
            Assert.IsNull(root.Q("setup-panel"));
            Assert.AreEqual("See Results", ButtonNamed("results-button").text);
        }

        [Test]
        public void RequestResults_BeforeGameOver_DoesNothing()
        {
            bool requested = false;
            view.ResultsRequested += () => requested = true;

            view.RequestResults();

            Assert.IsFalse(requested);
        }

        [Test]
        public void RequestResults_AfterGameOver_RaisesEvent()
        {
            bool requested = false;
            view.ResultsRequested += () => requested = true;
            session = new LocalBattleSession(new GameSettings(), 1, 9);
            session.Human.State = PlayerState.Eliminated;
            session.Human.HP = 0;
            session.StartNextTurn();
            view.Bind(session);

            Assert.IsTrue(session.IsGameOver);
            view.RequestResults();

            Assert.IsTrue(requested);
        }

        // --- Multi-player (M4) ---

        [Test]
        public void EightPlayers_ShowEightCards_AndSevenTargets()
        {
            BindNewSession(7, 55);
            Assert.AreEqual(8, root.Q("player-list").childCount);
            for (int id = 1; id <= 7; id++)
            {
                Assert.AreEqual("Action: ? (hidden)", CardText(id, "player-action"));
            }

            view.SelectAction(ActionType.Gather);
            view.LockSelection();
            view.NextTurn();
            view.SelectAction(ActionType.EnergyWave);

            Assert.AreEqual(7, root.Q("target-buttons").childCount);
            Assert.IsNull(ButtonNamed("target-button-0"));
        }

        [Test]
        public void EliminatedHuman_Spectates_AndSkipToEndFinishesTheGame()
        {
            session = new LocalBattleSession(new GameSettings(), 7, 77);
            session.Human.State = PlayerState.Eliminated;
            session.Human.HP = 0;
            session.StartNextTurn();
            view.Bind(session);

            Assert.AreEqual("You are eliminated - spectating", root.Q<Label>("phase-label").text);
            Assert.IsFalse(IsShown(root.Q("action-panel")));
            Assert.IsFalse(IsShown(root.Q("lock-row")));
            Assert.IsTrue(IsShown(ButtonNamed("next-turn-button")));
            Assert.IsTrue(IsShown(ButtonNamed("skip-button")));

            view.SkipToEnd();

            Assert.IsTrue(session.IsGameOver);
            Assert.IsTrue(IsShown(root.Q("game-over-panel")));
            Assert.IsFalse(IsShown(ButtonNamed("skip-button")));

            int turnHeaders = 0;
            foreach (VisualElement line in root.Q<ScrollView>("battle-log").contentContainer.Children())
            {
                if (((Label)line).text.StartsWith("-- Turn "))
                {
                    turnHeaders++;
                }
            }

            Assert.AreEqual(session.TurnNumber, turnHeaders, "Every skipped turn must be logged.");
        }

        [Test]
        public void SkipButton_IsHidden_WhileHumanIsAlive()
        {
            view.SelectAction(ActionType.Block);
            view.LockSelection();

            Assert.IsTrue(IsShown(ButtonNamed("next-turn-button")));
            Assert.IsFalse(IsShown(ButtonNamed("skip-button")));
        }

        [Test]
        public void GameScene_HasBattleScreenWithUiReferences()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            try
            {
                BattleScreen battleScreen = null;
                GameObject[] roots = scene.GetRootGameObjects();
                for (int i = 0; i < roots.Length && battleScreen == null; i++)
                {
                    battleScreen = roots[i].GetComponentInChildren<BattleScreen>();
                }

                Assert.IsNotNull(battleScreen, "GameScene has no BattleScreen.");
                UIDocument document = battleScreen.GetComponent<UIDocument>();
                Assert.IsNotNull(document.panelSettings);
                Assert.IsNotNull(document.panelSettings.themeStyleSheet);
                Assert.AreEqual(UxmlPath, AssetDatabase.GetAssetPath(document.visualTreeAsset));
                Assert.AreEqual(ScenePath, EditorBuildSettings.scenes[1].path, "GameScene is second, after LobbyScene (DEC-010).");
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static string ButtonNameFor(ActionType type)
        {
            switch (type)
            {
                case ActionType.Gather:
                    return "gather-button";
                case ActionType.EnergyWave:
                    return "energy-wave-button";
                case ActionType.Block:
                    return "block-button";
                case ActionType.Teleport:
                    return "teleport-button";
                default:
                    return "spirit-bomb-button";
            }
        }
    }
}
