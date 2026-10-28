using System;
using System.Collections.Generic;

namespace DRG
{
    public class RoomParticipant
    {
        public string Nickname;
        public bool IsBot;
        public bool IsHost;
        public bool IsReady;
    }

    // Local prototype room (spec §21): the human is the host, AI bots can be added or removed,
    // and the game can start only when there are at least two participants and everyone is Ready.
    // Bots are always Ready. Holds no gameplay state; the battle starts from BotCount.
    public class Room
    {
        private readonly int maxPlayers;
        private readonly RoomParticipant host;
        private readonly List<RoomParticipant> bots = new List<RoomParticipant>();

        public Room(GameSettings settings, int initialBotCount)
        {
            maxPlayers = settings.MaxPlayers;
            host = new RoomParticipant { Nickname = "You", IsBot = false, IsHost = true, IsReady = false };
            int count = Math.Max(0, Math.Min(initialBotCount, MaxBots));
            for (int i = 0; i < count; i++)
            {
                AddBot();
            }
        }

        public int MaxBots
        {
            get { return maxPlayers - 1; }
        }

        public int BotCount
        {
            get { return bots.Count; }
        }

        public RoomParticipant Host
        {
            get { return host; }
        }

        // Host first, then bots in join order. Names match LocalBattleSession ("You", "Bot 1", ...).
        public List<RoomParticipant> Participants
        {
            get
            {
                List<RoomParticipant> all = new List<RoomParticipant>();
                all.Add(host);
                all.AddRange(bots);
                return all;
            }
        }

        public bool CanAddBot
        {
            get { return bots.Count < MaxBots; }
        }

        public bool CanRemoveBot
        {
            get { return bots.Count > 0; }
        }

        public bool CanStart
        {
            get { return bots.Count >= 1 && host.IsReady; }
        }

        public bool AddBot()
        {
            if (!CanAddBot)
            {
                return false;
            }

            bots.Add(new RoomParticipant { Nickname = "Bot " + (bots.Count + 1), IsBot = true, IsHost = false, IsReady = true });
            return true;
        }

        public bool RemoveBot()
        {
            if (!CanRemoveBot)
            {
                return false;
            }

            bots.RemoveAt(bots.Count - 1);
            return true;
        }

        public void SetHostReady(bool ready)
        {
            host.IsReady = ready;
        }
    }
}
