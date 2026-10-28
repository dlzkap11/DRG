using System;

namespace DRG
{
    // Owns the GameState (spec §3) and carries data between the Lobby, Game, and Result scenes (DEC-010).
    //   Lobby --OpenRoom--> Room --StartGame--> Game --FinishGame--> Result --ReturnToLobby--> Lobby
    // Plain C#: scenes ask it for the next step; it never touches Unity objects.
    // Current is a process-wide instance so it survives scene loads.
    public class GameFlow
    {
        private static GameFlow current;

        private readonly GameSettings settings;
        private int lastBotCount = 1;

        public GameFlow(GameSettings settings)
        {
            this.settings = settings;
            State = GameState.Lobby;
        }

        public static GameFlow Current
        {
            get
            {
                if (current == null)
                {
                    current = new GameFlow(new GameSettings());
                }

                return current;
            }
        }

        public GameState State { get; private set; }
        public Room Room { get; private set; }
        // Bot count of the game in progress (valid in the Game state).
        public int BotCount { get; private set; }
        public GameResultSummary LastResult { get; private set; }

        public GameSettings Settings
        {
            get { return settings; }
        }

        // Opens the room shown on the Lobby screen, keeping the previous bot count.
        public Room OpenRoom()
        {
            Require(GameState.Lobby);
            Room = new Room(settings, lastBotCount);
            State = GameState.Room;
            return Room;
        }

        public void StartGame()
        {
            Require(GameState.Room);
            if (!Room.CanStart)
            {
                throw new InvalidOperationException("The room cannot start: it needs at least one bot and every participant Ready.");
            }

            BotCount = Room.BotCount;
            lastBotCount = BotCount;
            LastResult = null;
            State = GameState.Game;
        }

        public void FinishGame(GameResultSummary result)
        {
            Require(GameState.Game);
            if (result == null || result.Outcome == GameOutcome.Ongoing)
            {
                throw new InvalidOperationException("FinishGame needs the result of a finished game.");
            }

            LastResult = result;
            State = GameState.Result;
        }

        public void ReturnToLobby()
        {
            Require(GameState.Result);
            Room = null;
            State = GameState.Lobby;
        }

        private void Require(GameState expected)
        {
            if (State != expected)
            {
                throw new InvalidOperationException("Expected game state " + expected + " but was " + State + ".");
            }
        }
    }
}
