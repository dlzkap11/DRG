# DRG Development Progress

## Current Milestone
M2 - Two-player UI: implemented and visually play-tested via screen capture (DevLog 07); UI fixes from that test await a visual re-check before marking complete
- M1 - Battle Logic Foundation: complete (DevLog 04, summary: `Docs/DevLog/M1_요약.md`)

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
- [x] STEP 3 BattleResolver + BattleResult (Ki, all action interactions, mutual cancel, stacking, elimination, Winner/Draw) (DevLog 03)
- [x] STEP 4 automated combat tests (validator 25, turn 25, resolver 29 = 79 EditMode tests passing)
- [x] M1 wrap-up: `TurnManager.StartResolution` calls `BattleResolver` and returns `BattleResult`; Winner/Draw sets `IsGameOver` and blocks further turns (DevLog 04)
- [x] Deterministic combat simulation (spec §27): random-policy baseline, 2–8 players × 1,000 games, report at `Docs/Simulation/random_policy_report.md` (DevLog 05; 88 EditMode tests passing)
- [x] STEP 5–6 two-player UI: UI Toolkit GameScene, LocalBattleSession (Human + BotPlayer through the same Lock path), hidden opponent actions until reveal, battle log, game over / play again (DevLog 06; 112 EditMode tests passing)

## In Progress
- [x] M2 visual playtest of the built player: clicks, hidden/revealed actions, all four attack outcomes, game over, play again all work (DevLog 07)
- [ ] Visually re-check the UI fixes (button contrast, card width, game-over layout, log wording); then mark M2 complete and write `Docs/DevLog/M2_요약.md`

## Next
- [ ] Add first BotAI (replaces the temporary random bot, DEC-007)
- [ ] Expand to Human + 7 Bots

## Known Design Questions
- [x] Decide simultaneous total elimination / zero-survivor result → DEC-004 (Draw)
- [ ] Finalize target/state snapshot semantics where needed
- [ ] Evaluate anti-stalemate/turn-limit rule through playtesting (random baseline: avg 36–60 turns, max 151, none reached the 200-turn simulation cap; re-measure with BotAI)
- [ ] Evaluate Block/Teleport balance through playtesting (random baseline cannot judge balance; re-measure with BotAI)
- [ ] Check whether seat win-rate deviations in the random baseline (e.g. 8p P6 9.2%) are sampling noise (larger run or different seed)
- [x] Define use of `ActionType.None` → DEC-001
- [ ] Decide who owns `GameState` and where the "can act in current GameState" check lives (currently only TurnState.ActionSelection gates Lock)
- [x] Define when `ActionResultType.Failed` occurs at resolution time, and whether its Ki cost is consumed → DEC-005
- [x] Confirm Ki is consumed even when an attack is Blocked / Dodged / Cancelled → DEC-002
- [x] Decide whether HP is clamped at 0 or may go negative from stacked damage → DEC-003
- [x] Decide whether `BattleResult` carries eliminated players / winner → DEC-006
- [ ] Decide how a Draw is recorded in player statistics (wins/losses, spec §14/§23)
- [ ] Add a Korean font asset for the UI (license to be chosen; UI is English for now, DEC-007)
- [ ] Decide whether to delete the template `Assets/Scenes/SampleScene.unity` (still second in build settings)

## Work Log
Per-step work records are kept in `Docs/DevLog/`.

## Last Updated
2026-09-28
