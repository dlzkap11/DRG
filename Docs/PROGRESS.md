# DRG Development Progress

## Current Milestone
M5 - Lobby → Game → Result → Lobby: implemented (DevLog 13), awaiting the owner's visual check before marking complete
- M4 - Human + up to 7 Bots: implemented (DevLog 11), awaiting the owner's visual check before marking complete
- M3 - Human + Bot full game (BotAI, spec STEP 7): implemented (DevLog 08), awaiting the owner's play check before marking complete
- M2 - Two-player UI: complete (DevLog 06–07, summary: `Docs/DevLog/M2_요약.md`)
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
- [x] M2 visual playtest of the built player via screen capture: clicks, hidden/revealed actions, all four attack outcomes, game over, play again work; 4 UI issues fixed (DevLog 07)
- [x] UI fixes re-checked by the owner on the fixed build: no issues → M2 complete (summary: `Docs/DevLog/M2_요약.md`)

- [x] STEP 7 first BotAI per design doc §12 / DEC-008 (low-HP defense 40%, Ki-based 50/50 choices, random living target); replaces the temporary random bot in the game (DevLog 08; 124 EditMode tests passing)

- [x] STEP 10 multi-player: bot count setup panel (1–7), compact cards for up to 8 players, outcome-only reveal panel, spectate + "Skip to End" after elimination (DEC-009; DevLog 11; 137 EditMode tests passing)

- [x] STEP 8–9 Lobby and Result: LobbyScene / GameScene / ResultScene, `GameFlow` owns GameState, Room (host Ready, add/remove AI), ranking by elimination order; the M4 setup panel moved to the Lobby (DEC-010; DevLog 13; 161 EditMode tests passing)

## In Progress
- [ ] Owner visual check on `build5/DRG.exe` (includes M3, M4, M5): play Lobby → Game → Result → Lobby with 1 bot (M3 checklist) and with 7 bots (8-card row, targets, spectate/Skip to End); then mark M3/M4/M5 complete and write `M3_요약.md`, `M4_요약.md`, `M5_요약.md`
- Note: `docs/qa-drafts` (DevLog 09, 12), `feature/botai-simulation` (DevLog 10), `feature/m4-multiplayer` (DevLog 11) and `feature/m5-lobby-result` (DevLog 13, built on M4) are not merged into `main` yet

## Next
- [ ] Add Lobby/Result cases to the QA TC document (`docs/qa-drafts`)
- [ ] Player records (spec §14/§23, local JSON) — needs the Draw-counting decision first
- [ ] UI visual overhaul toward a casual style with art assets/packages (owner request 2026-09-28; the current screen is the temporary minimal presentation of DEC-007; assets/packages/style not chosen yet)

## Known Design Questions
- [x] Decide simultaneous total elimination / zero-survivor result → DEC-004 (Draw)
- [ ] Finalize target/state snapshot semantics where needed
- [ ] Evaluate anti-stalemate/turn-limit rule through playtesting (random baseline: avg 36–60 turns, max 151, none reached the 200-turn simulation cap; re-measure with BotAI)
- [ ] Evaluate Block/Teleport balance through playtesting (random baseline cannot judge balance; re-measure with BotAI)
- [ ] Check whether seat win-rate deviations in the random baseline (e.g. 8p P6 9.2%) are sampling noise (larger run or different seed)
- [x] Define use of `ActionType.None` → DEC-001
- [x] Decide who owns `GameState` and where the "can act in current GameState" check lives → DEC-010 (`GameFlow`; a battle session only exists in the Game state, Lock still gated by TurnState)
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
2026-10-28
