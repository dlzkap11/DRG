---
name: drg-ui
description: Use when implementing or reviewing DRG UI, action selection, target selection, turn status, battle logs, result screens, or information visibility.
---

# DRG UI Skill

## Core rule
UI requests gameplay actions; UI does not become the authority for gameplay state.

## Action flow
For attack actions:
1. Player selects action.
2. UI enters target selection.
3. Player selects valid living opponent.
4. UI requests/creates PlayerAction.
5. Game logic validates it.
6. UI displays the selected/locked state.

## Information visibility
Visible:
- nickname
- alive/dead state
- HP
- Ki
- own selected action
- lock state

Opponent selected actions remain hidden until reveal.

## UI responsibilities
UI may:
- display HP/Ki;
- enable/disable controls based on current information;
- request actions;
- display validation errors;
- display battle results;
- display turn/reveal state.

UI must not directly perform authoritative HP/Ki damage or combat resolution.

## Verification
Check:
- invalid targets cannot be selected where practical;
- UI cannot bypass logic validation;
- opponent actions remain hidden before reveal;
- lock state prevents changing an action;
- eliminated players are represented consistently.
