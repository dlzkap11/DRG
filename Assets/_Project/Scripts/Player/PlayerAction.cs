namespace DRG
{
    // The single action representation produced by both Human and Bot players.
    public class PlayerAction
    {
        // TargetPlayerId value for actions that do not need a target.
        public const int NoTarget = -1;

        public int PlayerId;
        public ActionType ActionType;
        public int TargetPlayerId = NoTarget;
    }
}
