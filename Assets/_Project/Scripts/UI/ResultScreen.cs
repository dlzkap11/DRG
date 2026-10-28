using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace DRG
{
    // ResultScene entry point: shows GameFlow's last result and returns to the Lobby.
    [RequireComponent(typeof(UIDocument))]
    public class ResultScreen : MonoBehaviour
    {
        private ResultView view;

        private void OnEnable()
        {
            view = new ResultView(GetComponent<UIDocument>().rootVisualElement);
            view.LobbyRequested += ReturnToLobby;

            GameFlow flow = GameFlow.Current;
            if (flow.State == GameState.Result && flow.LastResult != null)
            {
                view.Bind(flow.LastResult);
            }
        }

        private void OnDisable()
        {
            if (view != null)
            {
                view.LobbyRequested -= ReturnToLobby;
                view = null;
            }
        }

        private void ReturnToLobby()
        {
            GameFlow flow = GameFlow.Current;
            if (flow.State == GameState.Result)
            {
                flow.ReturnToLobby();
            }

            SceneManager.LoadScene(SceneNames.Lobby);
        }
    }
}
