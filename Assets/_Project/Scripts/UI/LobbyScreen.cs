using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace DRG
{
    // LobbyScene entry point: opens the room through GameFlow and starts the game scene when the room is ready.
    [RequireComponent(typeof(UIDocument))]
    public class LobbyScreen : MonoBehaviour
    {
        private LobbyView view;

        private void OnEnable()
        {
            GameFlow flow = GameFlow.Current;
            if (flow.State == GameState.Result)
            {
                flow.ReturnToLobby();
            }

            Room room = flow.State == GameState.Room ? flow.Room : flow.OpenRoom();

            view = new LobbyView(GetComponent<UIDocument>().rootVisualElement);
            view.StartRequested += StartGame;
            view.Bind(room);
        }

        private void OnDisable()
        {
            if (view != null)
            {
                view.StartRequested -= StartGame;
                view = null;
            }
        }

        private void StartGame()
        {
            GameFlow.Current.StartGame();
            SceneManager.LoadScene(SceneNames.Game);
        }
    }
}
