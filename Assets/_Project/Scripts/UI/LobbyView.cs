using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace DRG
{
    // Binds LobbyScreen.uxml to a Room (lobby and room on one screen, DEC-010).
    // The UI only requests room changes; Room decides what is allowed.
    public class LobbyView
    {
        private readonly VisualElement participantList;
        private readonly Button addBotButton;
        private readonly Button removeBotButton;
        private readonly Button readyButton;
        private readonly Button startButton;
        private readonly Label statusLabel;

        private Room room;

        // Raised by "Start" when the room can start.
        public event Action StartRequested;

        public LobbyView(VisualElement root)
        {
            participantList = Require<VisualElement>(root, "participant-list");
            addBotButton = Require<Button>(root, "add-bot-button");
            removeBotButton = Require<Button>(root, "remove-bot-button");
            readyButton = Require<Button>(root, "ready-button");
            startButton = Require<Button>(root, "start-button");
            statusLabel = Require<Label>(root, "lobby-status-label");

            addBotButton.clicked += AddBot;
            removeBotButton.clicked += RemoveBot;
            readyButton.clicked += ToggleReady;
            startButton.clicked += Start;
        }

        public void Bind(Room newRoom)
        {
            room = newRoom;
            Refresh();
        }

        public void AddBot()
        {
            if (room != null && room.AddBot())
            {
                Refresh();
            }
        }

        public void RemoveBot()
        {
            if (room != null && room.RemoveBot())
            {
                Refresh();
            }
        }

        public void ToggleReady()
        {
            if (room == null)
            {
                return;
            }

            room.SetHostReady(!room.Host.IsReady);
            Refresh();
        }

        public void Start()
        {
            if (room == null || !room.CanStart || StartRequested == null)
            {
                return;
            }

            StartRequested();
        }

        public void Refresh()
        {
            if (room == null)
            {
                return;
            }

            participantList.Clear();
            List<RoomParticipant> participants = room.Participants;
            for (int i = 0; i < participants.Count; i++)
            {
                RoomParticipant participant = participants[i];
                VisualElement row = new VisualElement();
                row.name = "participant-" + i;
                row.AddToClassList("participant-row");
                row.Add(CreateLabel(participant.Nickname + (participant.IsHost ? " (Host)" : participant.IsBot ? " (AI)" : string.Empty), "participant-name"));
                Label ready = CreateLabel(participant.IsReady ? "Ready" : "Not Ready", "participant-ready");
                ready.EnableInClassList("is-ready", participant.IsReady);
                row.Add(ready);
                participantList.Add(row);
            }

            addBotButton.SetEnabled(room.CanAddBot);
            removeBotButton.SetEnabled(room.CanRemoveBot);
            readyButton.text = room.Host.IsReady ? "Unready" : "Ready";
            startButton.SetEnabled(room.CanStart);
            statusLabel.text = StatusText();
        }

        private string StatusText()
        {
            if (room.BotCount == 0)
            {
                return "Add at least one AI to start.";
            }

            if (!room.Host.IsReady)
            {
                return "Press Ready when you are set.";
            }

            return "Everyone is ready. Press Start.";
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
                throw new InvalidOperationException("LobbyScreen UXML is missing element '" + name + "' of type " + typeof(T).Name + ".");
            }

            return element;
        }
    }
}
