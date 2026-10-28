using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace DRG
{
    // Scene entry point for the local battle. Shows the bot-count setup panel, then creates the session
    // with the chosen number of bots and hands it to BattleView.
    [RequireComponent(typeof(UIDocument))]
    public class BattleScreen : MonoBehaviour
    {
        // Bot count preselected on the setup panel (1..MaxPlayers-1).
        [SerializeField] private int botCount = 1;

        private readonly GameSettings settings = new GameSettings();
        private BattleView view;

        private void OnEnable()
        {
            view = new BattleView(GetComponent<UIDocument>().rootVisualElement, botCount, settings.MaxPlayers - 1);
            view.StartRequested += StartNewGame;
        }

        private void OnDisable()
        {
            if (view != null)
            {
                view.StartRequested -= StartNewGame;
                view = null;
            }
        }

        private void StartNewGame(int chosenBotCount)
        {
            botCount = chosenBotCount;
            LocalBattleSession session = new LocalBattleSession(settings, chosenBotCount, Environment.TickCount);
            session.StartNextTurn();
            view.Bind(session);
        }
    }
}
