using System;
using UnityEngine.UIElements;

namespace DRG
{
    // Binds ResultScreen.uxml to a finished game's summary (spec §22, DEC-010).
    public class ResultView
    {
        private readonly Label headlineLabel;
        private readonly Label turnsLabel;
        private readonly VisualElement rankingList;

        // Raised by "Back to Lobby".
        public event Action LobbyRequested;

        public ResultView(VisualElement root)
        {
            headlineLabel = Require<Label>(root, "result-headline");
            turnsLabel = Require<Label>(root, "result-turns");
            rankingList = Require<VisualElement>(root, "ranking-list");
            Button lobbyButton = Require<Button>(root, "lobby-button");
            lobbyButton.clicked += RequestLobby;
        }

        public void Bind(GameResultSummary summary)
        {
            headlineLabel.text = Headline(summary);
            turnsLabel.text = "Turns played: " + summary.Turns;

            rankingList.Clear();
            for (int i = 0; i < summary.Players.Count; i++)
            {
                PlayerResult player = summary.Players[i];
                VisualElement row = new VisualElement();
                row.name = "ranking-" + player.PlayerId;
                row.AddToClassList("ranking-row");
                row.EnableInClassList("is-human", player.IsHuman);
                row.Add(CreateLabel("#" + player.Rank, "ranking-rank"));
                row.Add(CreateLabel(player.Nickname, "ranking-name"));
                row.Add(CreateLabel(StandingText(summary, player), "ranking-standing"));
                rankingList.Add(row);
            }
        }

        public void RequestLobby()
        {
            if (LobbyRequested != null)
            {
                LobbyRequested();
            }
        }

        private static string Headline(GameResultSummary summary)
        {
            if (summary.Outcome == GameOutcome.Draw)
            {
                return "Draw - no survivors.";
            }

            if (summary.WinnerPlayerId == LocalBattleSession.HumanPlayerId)
            {
                return "You win!";
            }

            for (int i = 0; i < summary.Players.Count; i++)
            {
                if (summary.Players[i].PlayerId == summary.WinnerPlayerId)
                {
                    return summary.Players[i].Nickname + " wins.";
                }
            }

            return "Game over.";
        }

        private static string StandingText(GameResultSummary summary, PlayerResult player)
        {
            if (player.EliminatedTurn == PlayerResult.NotEliminated)
            {
                return player.PlayerId == summary.WinnerPlayerId ? "Winner" : "Survived";
            }

            return "Eliminated on turn " + player.EliminatedTurn;
        }

        private static Label CreateLabel(string text, string className)
        {
            Label label = new Label(text);
            label.AddToClassList(className);
            return label;
        }

        private static T Require<T>(VisualElement root, string name) where T : VisualElement
        {
            T element = root.Q<T>(name);
            if (element == null)
            {
                throw new InvalidOperationException("ResultScreen UXML is missing element '" + name + "' of type " + typeof(T).Name + ".");
            }

            return element;
        }
    }
}
