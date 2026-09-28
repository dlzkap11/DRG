using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace DRG
{
    // Scene entry point for the local battle. Creates the session and hands the UIDocument root to BattleView.
    [RequireComponent(typeof(UIDocument))]
    public class BattleScreen : MonoBehaviour
    {
        [SerializeField] private int botCount = 1;

        private BattleView view;

        private void OnEnable()
        {
            view = new BattleView(GetComponent<UIDocument>().rootVisualElement);
            view.RestartRequested += StartNewGame;
            StartNewGame();
        }

        private void OnDisable()
        {
            if (view != null)
            {
                view.RestartRequested -= StartNewGame;
                view = null;
            }
        }

        private void StartNewGame()
        {
            LocalBattleSession session = new LocalBattleSession(new GameSettings(), botCount, Environment.TickCount);
            session.StartNextTurn();
            view.Bind(session);
        }
    }
}
