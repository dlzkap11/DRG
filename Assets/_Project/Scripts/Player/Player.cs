namespace DRG
{
    public class Player
    {
        public int PlayerId;
        public string Nickname;
        public int HP;
        public int MaxHP;
        public int Ki;
        public int MaxKi;
        public PlayerState State;
        public PlayerAction CurrentAction;

        public Player(int playerId, string nickname, GameSettings settings)
        {
            PlayerId = playerId;
            Nickname = nickname;
            HP = settings.StartingHP;
            MaxHP = settings.MaxHP;
            Ki = settings.StartingKi;
            MaxKi = settings.MaxKi;
            State = PlayerState.Alive;
            CurrentAction = null;
        }
    }
}
