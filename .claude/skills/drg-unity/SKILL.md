---
name: drg-unity
description: Use when implementing DRG features that depend on Unity scenes, GameObjects, MonoBehaviours, prefabs, serialization, input, animation, or Unity lifecycle.
---

# DRG Unity Skill

## Principles
- Keep deterministic gameplay logic in plain C# where practical.
- Use MonoBehaviour for Unity lifecycle/component/scene concerns.
- Keep UI and presentation separate from authoritative gameplay state.
- Preserve serialized references and prefab dependencies.

## Before editing
Inspect:
- relevant MonoBehaviour
- referenced components
- prefab/scene references
- Animator parameters/clips/events when animation is involved
- existing input setup

Do not assume a component or serialized field exists without inspecting it.

## Battle presentation
Preferred flow:
`BattleResolver → BattleResult → BattlePresentation → animation/VFX/UI`

Animations should present results that have already been logically decided. Do not put combat rules inside animation callbacks.

## Unity verification
When possible:
- compile scripts;
- check Unity Console;
- inspect prefab/scene references;
- verify play-mode behavior for presentation changes.

If Unity cannot be launched in the current environment, say so instead of claiming a Unity test was performed.
