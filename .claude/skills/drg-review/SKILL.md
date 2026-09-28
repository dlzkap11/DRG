---
name: drg-review
description: Use when reviewing DRG changes for specification consistency, architecture, scope, tests, and unnecessary modifications.
---

# DRG Review Skill

Review changes against:
- `CLAUDE.md`
- `Docs/DRG_게임_기획서_v1.0.md`
- `Docs/DRG_게임_개발_명세서_v1.0.md`
- `Docs/DECISIONS.md`

## Review checklist

### Specification
- Does the implementation match accepted rules?
- Did it silently introduce a new gameplay rule?
- Did it change action costs/effects?
- Did it accidentally make combat sequential?

### Architecture
- Is BattleResolver independent of UI/presentation?
- Do Human and Bot use the same PlayerAction path?
- Is UI avoiding direct authoritative state mutation?

### Scope
- Are there unrelated refactors?
- Are there unnecessary files or abstractions?
- Was online multiplayer added prematurely?

### Tests
- Are relevant edge cases covered?
- Were existing tests preserved?
- Does the test actually assert the intended rule?

## Output format

1. Correct
2. Potential Problems
3. Specification Conflicts
4. Unnecessary Changes
5. Missing Tests
6. Recommended Next Step

Do not assign an overall score or ranking. Report concrete findings.
