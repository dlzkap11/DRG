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

### Important unresolved items
- Simultaneous total elimination / zero survivors rule is not finalized.
- Exact target/state snapshot semantics should be finalized before edge-case implementation if required.
- Anti-stalemate/turn-limit rules are not finalized.
- Block/Teleport balance is a playtest question, not an implementation assumption.
