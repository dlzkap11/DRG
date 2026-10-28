using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace DRG
{
    // GameScene entry point. Starts a battle with the bot count chosen in the Lobby (GameFlow),
    // and on "See Results" hands the summary to GameFlow and loads the Result scene.
    [RequireComponent(typeof(UIDocument))]
    public class BattleScreen : MonoBehaviour
    {
        // Used only when GameScene is played directly in the editor without coming from the Lobby.
        [SerializeField] private int botCount = 1;

        private BattleView view;
        private LocalBattleSession session;

        private void OnEnable()
        {
            GameFlow flow = GameFlow.Current;
            if (flow.State != GameState.Game)
            {
                StartDirectlyForEditorTesting(flow);
            }

            view = new BattleView(GetComponent<UIDocument>().rootVisualElement);
            view.ResultsRequested += ShowResults;

            session = new LocalBattleSession(flow.Settings, flow.BotCount, Environment.TickCount);
            session.StartNextTurn();
            view.Bind(session);
        }

        private void OnDisable()
        {
            if (view != null)
            {
                view.ResultsRequested -= ShowResults;
                view = null;
            }
        }

        private void ShowResults()
        {
            GameFlow.Current.FinishGame(GameResultSummary.FromSession(session));
            SceneManager.LoadScene(SceneNames.Result);
        }

        // Lets GameScene run on its own (Play in the editor) by walking the flow Lobby -> Room -> Game.
        private void StartDirectlyForEditorTesting(GameFlow flow)
        {
            if (flow.State == GameState.Result)
            {
                flow.ReturnToLobby();
            }

            Room room = flow.State == GameState.Room ? flow.Room : flow.OpenRoom();
            while (room.BotCount < botCount && room.AddBot())
            {
            }

            while (room.BotCount > Math.Max(1, botCount) && room.RemoveBot())
            {
            }

            room.SetHostReady(true);
            flow.StartGame();
        }
    }
}
