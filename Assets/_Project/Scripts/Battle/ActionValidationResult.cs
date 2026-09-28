namespace DRG
{
    public enum ActionValidationResult
    {
        Valid,
        MissingAction,
        PlayerNotFound,
        PlayerEliminated,
        InvalidActionType,
        KiAtMax,
        InsufficientKi,
        SelfTarget,
        TargetNotFound,
        TargetEliminated,
        UnexpectedTarget
    }
}
