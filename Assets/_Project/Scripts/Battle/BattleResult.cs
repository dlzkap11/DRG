using System.Collections.Generic;

namespace DRG
{
    // Data describing one resolved turn. Presentation/UI read this; they do not re-derive combat outcomes.
    public class BattleResult
    {
        public const int NoWinner = -1;

        public List<DamageResult> DamageResults = new List<DamageResult>();
        public List<KiChangeResult> KiChanges = new List<KiChangeResult>();
        public List<ActionResult> ActionResults = new List<ActionResult>();

        // DEC-006: elimination and game outcome are part of the turn result.
        public List<int> EliminatedPlayerIds = new List<int>();
        public GameOutcome Outcome = GameOutcome.Ongoing;
        public int WinnerPlayerId = NoWinner;
    }
}
