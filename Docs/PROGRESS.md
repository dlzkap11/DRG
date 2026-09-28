# DRG Development Progress

## Current Milestone
M1 - Battle Logic Foundation

## Completed
- [x] Game concept and core loop documented
- [x] Five core actions documented
- [x] Basic Player/GameSettings data model defined
- [x] BattleResolver architecture defined
- [x] Human/Bot shared PlayerAction architecture defined
- [x] Claude Code development harness prepared
- [x] Git repository initialized and connected to GitHub (DevLog 00)
- [x] STEP 1 core data model implemented in code: GameState, PlayerState, ActionType, PlayerAction, Player, GameSettings (DevLog 01)

## In Progress
- [ ] Implement core BattleResolver
- [ ] Implement ActionValidator
- [ ] Implement TurnManager
- [ ] Add automated combat tests

## Next
- [ ] Run deterministic combat simulations
- [ ] Build basic two-player UI
- [ ] Add first BotAI
- [ ] Expand to Human + 7 Bots

## Known Design Questions
- [ ] Decide simultaneous total elimination / zero-survivor result
- [ ] Finalize target/state snapshot semantics where needed
- [ ] Evaluate anti-stalemate/turn-limit rule through playtesting
- [ ] Evaluate Block/Teleport balance through playtesting
- [ ] Define use of `ActionType.None`: rejected for living players? used for eliminated-player auto-lock?
- [ ] Define when `ActionResultType.Failed` occurs at resolution time, and whether its Ki cost is consumed
- [ ] Confirm Ki is consumed even when an attack is Blocked / Dodged / Cancelled (spec §11 order implies yes)
- [ ] Decide whether HP is clamped at 0 or may go negative from stacked damage
- [ ] Decide whether `BattleResult` carries eliminated players / winner (architecture, not a rule)

## Work Log
Per-step work records are kept in `Docs/DevLog/`.

## Last Updated
2026-09-28
