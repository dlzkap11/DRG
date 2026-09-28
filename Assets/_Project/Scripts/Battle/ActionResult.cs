namespace DRG
{
    // Outcome of one attack action (Hit/Blocked/Dodged/Cancelled), or of any action that Failed validation.
    // Successful non-attack actions have no ActionResult; their effect appears in KiChanges.
    public class ActionResult
    {
        public int PlayerId;
        public ActionType ActionType;
        public int TargetPlayerId;
        public ActionResultType ResultType;
    }
}
