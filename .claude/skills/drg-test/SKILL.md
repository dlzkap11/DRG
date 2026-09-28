---
name: drg-test
description: Use when adding, updating, or reviewing tests for DRG gameplay, combat, turn flow, AI decisions, or regressions.
---

# DRG Test Skill

## Goal
Protect the game rules, especially simultaneous resolution and action interactions.

## Test priorities
1. Deterministic unit tests for pure gameplay logic.
2. Regression tests for every fixed bug.
3. Integration tests for turn locking/resolution.
4. Simulation tests for balance and long-run behavior.

## Combat matrix
Cover all relevant pairs involving:
- Gather
- Energy Wave
- Block
- Teleport
- Spirit Bomb

Explicitly test:
- A→B / B→A Energy Wave cancellation.
- A→C / B→C does not cancel.
- Block stops Energy Wave.
- Block does not stop Spirit Bomb.
- Teleport stops Energy Wave and Spirit Bomb.
- Multiple attacks can stack.
- Locked action can resolve after the owner dies during resolution.

## Validation tests
Test:
- self target rejected;
- dead target rejected;
- nonexistent target rejected;
- insufficient Ki rejected;
- Gather rejected at MaxKi;
- locked action cannot change;
- eliminated players do not need to submit a new action.

## Test integrity
Never delete or weaken a test simply because the current implementation fails it.
If the test conflicts with an accepted design document, identify the specification conflict explicitly.

## Simulation
When the core resolver is stable, support deterministic seeded simulations where practical. Useful measurements include average turns, action frequency, damage frequency, simultaneous death rate, and survival distribution.
