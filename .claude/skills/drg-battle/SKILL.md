---
name: drg-battle
description: Use when implementing or reviewing DRG battle rules, TurnManager, BattleResolver, PlayerAction, ActionValidator, HP, Ki, or simultaneous action resolution.
---

# DRG Battle Skill

## Scope
Use this skill for:
- BattleResolver
- TurnManager
- PlayerAction
- ActionValidator
- BattleResult
- combat rules
- turn resolution
- HP/Ki processing
- action interaction tests

## Core principle
DRG is simultaneous-action combat. Players commit actions before the reveal/resolution phase. Do not accidentally implement a sequential combat system.

## Action rules
- Gather: +1 Ki, invalid at MaxKi.
- Energy Wave: cost 1 Ki, 1 damage, target one living opponent.
- Block: cost 0, blocks Energy Wave, does not block Spirit Bomb.
- Teleport: cost 1, avoids all attacks targeting self.
- Spirit Bomb: cost 3, 1 damage, ignores Block, avoided only by Teleport.

## Cancellation
Only exact mutual Energy Wave attacks cancel:
- A targets B AND B targets A → both cancelled.
- A targets C AND B targets C → neither cancels.

Do not generalize cancellation to all shared targets.

## Committed actions
An action that was locked is committed for the turn. If the owner reaches HP 0 during resolution, do not automatically erase the committed action.

## Separation
BattleResolver must remain independent of UI, Animator, GameObject, VFX, and Audio.

## Required verification
For battle changes, consider tests for:
- normal hit
- Block
- Teleport
- Spirit Bomb through Block
- mutual Energy Wave cancellation
- non-mutual multi-attacker case
- insufficient Ki
- max-Ki Gather
- invalid targets
- multiple attackers
- simultaneous death
- committed action after owner death

## Implementation discipline
Read the existing implementation before editing. Make the smallest change necessary. If a required rule is unresolved, do not invent it; flag it as a design question.
