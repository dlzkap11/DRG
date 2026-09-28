# DRG Development Guide

## 1. Project Identity
DRG is a Unity/C# strategy game based on simultaneous-action turn-based psychological gameplay.

Current development target:
- Human 1 player + AI bots
- Maximum 8 players
- PC / Unity / C#
- Online multiplayer is NOT part of the current prototype implementation scope.

The core gameplay value is prediction and psychological play, not merely action-counter mechanics.

## 2. Source of Truth
Before implementing gameplay features, inspect these documents:
1. `Docs/DRG_게임_기획서_v1.0.md`
2. `Docs/DRG_게임_개발_명세서_v1.0.md`
3. `Docs/DECISIONS.md`
4. `Docs/PROGRESS.md`

Priority:
- Accepted decisions in `DECISIONS.md`
- Development specification
- Game design document
- Existing implementation

If a requested feature conflicts with the specification or an accepted decision, STOP and report the conflict before changing gameplay rules.

Never silently invent or change game rules.

## 3. Development Philosophy
Use this workflow:
1. Understand the requested task.
2. Read the relevant specification and existing code.
3. Identify affected files and responsibilities.
4. Make the smallest necessary change.
5. Compile/test when possible.
6. Inspect the resulting diff.
7. Update documentation when the behavior or architecture changed.
8. Report what changed, what was tested, and any remaining issue.

Avoid unnecessary refactoring, abstractions, configuration, or new files.

If temporary scripts/files are created for investigation, remove them before finishing unless they are intentionally part of the project.

## 4. Scope Control
Only implement what the user requested or what is clearly necessary for correctness.

Do NOT:
- add unrelated features;
- redesign working systems without a reason;
- perform broad refactors during a small task;
- add speculative online-multiplayer infrastructure;
- add premature extensibility;
- change game balance without an explicit design decision;
- weaken or delete tests merely to make implementation pass.

## 5. Architecture
Preferred flow:

Human / Bot
    ↓
PlayerAction
    ↓
TurnManager
    ↓
BattleResolver
    ↓
BattleResult
    ↓
BattlePresentation
    ↓
UI / Animation / VFX

Responsibilities:
- Player/Human/Bot: produce actions.
- TurnManager: collect locks and control turn state.
- ActionValidator: validate legal actions.
- BattleResolver: deterministic gameplay resolution.
- BattleResult: data describing the resolved outcome.
- BattlePresentation: present already-decided results.
- UI: request actions and display state.

`BattleResolver` must not depend on:
- UI
- Animator
- Animation clips/events
- VFX
- Audio
- GameObject hierarchy
- Scene objects

## 6. Human and Bot Unification
Human and Bot must produce the same `PlayerAction` representation.

The battle system must not contain separate combat rules such as:
- `if human...`
- `if bot...`

AI decision-making belongs to Bot/AI code. Resolution belongs to the same BattleResolver for every player.

## 7. Simultaneous Action Rules
Actions are committed when the player locks.

A committed action is not retroactively cancelled merely because its owner reaches HP 0 earlier in the resolution process.

Do not convert simultaneous combat into sequential combat through implementation details.

Important: target/state semantics that are not explicitly finalized in the design document must be treated as unresolved design questions, not silently decided by code.

## 8. Current Action Rules
Default stats:
- HP: 3
- Max HP: 3
- Ki: 0
- Max Ki: 3

Actions:
- Gather: +1 Ki; unavailable at Ki 3.
- Energy Wave: -1 Ki; target one living opponent; 1 damage.
- Block: no cost; blocks all Energy Waves targeting self; does not block Spirit Bomb.
- Teleport: -1 Ki; avoids all attacks targeting self.
- Spirit Bomb: -3 Ki; target one living opponent; 1 damage; ignores Block; only Teleport avoids it.

Energy Wave mutual cancellation:
- A → B and B → A cancel each other.
- A → C and B → C do NOT cancel each other.
- Multiple attackers can damage the same target.

## 9. UI Rules
UI must not directly mutate gameplay state such as HP, Ki, PlayerState, or authoritative CurrentAction.

UI should request an action. Game logic validates and applies/commits it.

Opponent selected actions remain hidden until the reveal phase.

## 10. Validation
Gameplay changes should have tests covering relevant combinations, including:
- all five actions;
- Ki costs and limits;
- invalid/self/dead targets;
- insufficient Ki;
- Energy Wave mutual cancellation;
- non-mutual multi-attacker cases;
- Block vs Energy Wave;
- Spirit Bomb vs Block;
- Teleport vs all attacks;
- committed actions after owner death;
- multiple simultaneous attackers;
- eliminated players auto-locking;
- lock immutability.

Do not assume tests passing proves the design is correct. Verify tests against the specification.

## 11. Unity Rules
Prefer plain C# for deterministic gameplay logic.

Use `MonoBehaviour` when Unity lifecycle, scene objects, components, serialization, or presentation genuinely require it.

Do not place core combat rules in UI scripts, Animator callbacks, or visual effects code.

Respect existing Unity serialized references and prefab/scene dependencies before changing components.

## 12. C# Style
Follow the existing project's style.

Prefer readable explicit code over clever code.

Avoid LINQ when a simple loop is clearer or when the surrounding project deliberately avoids LINQ.

Do not introduce abstractions merely for theoretical future use.

## 13. Investigate Before Editing
Never speculate about code that has not been opened.

Before editing a referenced file:
- read it;
- inspect nearby dependencies/usages;
- determine whether the requested feature already partially exists;
- identify the smallest safe change.

If the user references a specific file, read that file before making claims about it.

## 14. After Editing
When possible:
1. compile the affected code;
2. run relevant tests;
3. inspect Unity Console errors if available;
4. inspect `git diff` and `git status`;
5. remove temporary files;
6. update `Docs/PROGRESS.md` when milestone status changed;
7. update `Docs/DECISIONS.md` when an architectural/design decision is explicitly accepted.

Do not claim a test or compilation was run unless it actually was.

## 15. Documentation Rules
Documentation describes intent and accepted behavior.

If implementation exposes an unresolved design question, record it under `Docs/PROGRESS.md` rather than silently resolving it.

Only add an entry to `DECISIONS.md` after the decision is explicitly accepted by the project owner.

## 16. Current Development Phase
Phase: Prototype foundation.

Priority:
1. Core gameplay correctness
2. Battle resolution
3. Automated tests/simulation
4. Turn flow
5. Basic UI
6. Bot AI
7. Lobby/result flow
8. Polish

Online multiplayer, advanced character systems, monetization, live service, and premature optimization are outside the current scope.

## 17. Task Completion Standard
A task is complete when:
- the requested behavior is implemented;
- the implementation is consistent with the accepted specification;
- relevant tests/verification have been performed when possible;
- no unnecessary unrelated changes were made;
- documentation was updated when required;
- remaining limitations are clearly reported.

## 18. Communication Format
For non-trivial tasks, report:
1. Understanding of the task
2. Files inspected
3. Files changed
4. What changed
5. Tests/verification performed
6. Remaining issues or design questions

Do not hide uncertainty. If a rule is unspecified, ask or record it as unresolved instead of inventing a rule.
