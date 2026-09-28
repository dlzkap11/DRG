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
- [x] STEP 2 TurnManager (turn flow, Lock, all-locked check, eliminated auto-lock) and ActionValidator, with EditMode tests (DevLog 02)

## In Progress
- [ ] Implement core BattleResolver
- [ ] Add automated combat tests (validator/turn tests exist; resolver tests pending)

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
- [x] Define use of `ActionType.None` → DEC-001
- [ ] Decide who owns `GameState` and where the "can act in current GameState" check lives (currently only TurnState.ActionSelection gates Lock)
- [ ] Define when `ActionResultType.Failed` occurs at resolution time, and whether its Ki cost is consumed
- [ ] Confirm Ki is consumed even when an attack is Blocked / Dodged / Cancelled (spec §11 order implies yes)
- [ ] Decide whether HP is clamped at 0 or may go negative from stacked damage
- [ ] Decide whether `BattleResult` carries eliminated players / winner (architecture, not a rule)

## Work Log
Per-step work records are kept in `Docs/DevLog/`.

## Last Updated
2026-09-28
