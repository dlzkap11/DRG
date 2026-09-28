# DRG Development Decisions

This file contains only decisions explicitly accepted by the project owner.

## Decision Template

```markdown
## DEC-XXX: Title
Status: Accepted
Date: YYYY-MM-DD

### Decision
What was decided.

### Reason
Why this decision was made.

### Consequences
What this changes for implementation/design/testing.
```

## Current Decisions

## DEC-001: ActionType.None is reserved for eliminated players
Status: Accepted
Date: 2026-09-28

### Decision
- Eliminated players are auto-locked each turn with `ActionType.None` and `TargetPlayerId = -1`.
- A living player cannot lock `ActionType.None`; validation rejects it. Every living player must choose one of the five actions each turn.

### Reason
The specification defines `ActionType.None` but not when it is used. Allowing living players to "pass" would add a new gameplay option that is not in the design document.

### Consequences
- `ActionValidator` returns `InvalidActionType` for `None` from a living player.
- `TurnManager.StartActionSelection` assigns a `None` action to eliminated players and marks them locked.
- `BattleResolver` (STEP 3) must treat `None` as "no action": no Ki change, no attack, and no defense.

## DEC-002: Ki cost is always paid for a resolved action
Status: Accepted
Date: 2026-09-28

### Decision
A valid locked action pays its Ki cost even when its attack is Blocked, Dodged, or Cancelled.

### Reason
This follows the spec §11 resolution order, where the Ki cost is applied before attacks are judged. Risking Ki on an attack that may be blocked is part of the prediction game.

### Consequences
`BattleResolver` applies all Ki changes before judging attacks. Tests assert that Ki is spent on Blocked, Dodged, and Cancelled attacks.

## DEC-003: HP is clamped at 0
Status: Accepted
Date: 2026-09-28

### Decision
Stacked damage never reduces HP below 0. `DamageResult` still records the full damage from every hit.

### Reason
A negative HP value has no gameplay meaning, because elimination happens at HP 0 or below either way. Clamping keeps the displayed state simple.

### Consequences
`BattleResolver` applies `HP = max(0, HP - totalDamage)`.

## DEC-004: Zero survivors is a draw
Status: Accepted
Date: 2026-09-28

### Decision
If every remaining player is eliminated in the same turn, the game ends as a Draw with no winner.

### Reason
This resolves the open "simultaneous total elimination" question. Spec §22 already anticipates a Draw result.

### Consequences
`BattleResult.Outcome = Draw` and `WinnerPlayerId = -1`. The players eliminated in that turn are listed in `EliminatedPlayerIds`. How a Draw is recorded in player statistics (§14/§23) is still undecided.

## DEC-005: Invalid actions at resolution are Failed and have no effect
Status: Accepted
Date: 2026-09-28

### Decision
During resolution, `BattleResolver` re-validates each living player's locked action against the state at the start of resolution. An action is recorded as `ActionResultType.Failed` when any of these holds:
- it is invalid;
- it is missing;
- it belongs to a different player.

A Failed action has no effect: no Ki change, no attack, and no defense. All other actions resolve normally.

### Reason
In normal flow, `TurnManager` validates actions at Lock, so failures cannot happen. They are possible only when the resolver is called directly, for example from tests or simulations. The specification defines a `Failed` result type for this case.

### Consequences
A player whose defensive action Failed has no protection that turn.

## DEC-006: BattleResult carries elimination and game outcome
Status: Accepted
Date: 2026-09-28

### Decision
In addition to the spec §13 fields, `BattleResult` includes:
- `EliminatedPlayerIds`: players eliminated this turn
- `Outcome`: `Ongoing`, `Winner`, or `Draw`
- `WinnerPlayerId`: `-1` when there is no winner

### Reason
Spec §11 makes the resolver responsible for elimination and the victory check, but spec §13 had no fields to report them.

### Consequences
Presentation and UI read a single `BattleResult` per turn.

## DEC-007: M2 prototype UI approach
Status: Accepted
Date: 2026-09-28

### Decision
- The UI uses **UI Toolkit**: the layout is in UXML and the styles are in USS. An editor script (`DRG > Build Game Scene`) generates the scene and the PanelSettings asset.
- Until BotAI (M3) exists, the opponent in the prototype is a `BotPlayer` driven by the uniform `RandomActionPolicy`. The bot locks through the same `PlayerAction` → `TurnManager.Lock` path as the human.
- UI text is in **English** until a Korean font asset is added, because Unity's default font has no Korean glyphs.
- The presentation is **functional and minimal**: text and colored boxes with no animation or VFX.

### Reason
- Text-based UXML and USS files, together with a generated scene, are easy to review in Git and to test.
- BotAI probability values are not yet specified, so the prototype uses the random policy instead.

### Consequences
- UI code reads and requests everything through `LocalBattleSession`.
- Opponent actions are hidden until the reveal (`GetVisibleAction`).
- The random bot must be replaced by BotAI in M3.
- Adding a Korean font requires a separately licensed font asset.

### Important unresolved items
- Simultaneous total elimination / zero survivors rule → resolved by DEC-004.
- Exact target/state snapshot semantics should be finalized before edge-case implementation if required.
- Anti-stalemate/turn-limit rules are not finalized.
- Block/Teleport balance is a playtest question, not an implementation assumption.
