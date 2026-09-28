# DRG Claude Code Harness

DRG Unity 프로젝트를 Claude Code로 개발하기 위한 프로젝트 레벨 개발 하네스입니다.

## 구성

```text
DRG/
├─ CLAUDE.md
├─ Docs/
│  ├─ DRG_게임_기획서_v1.0.md
│  ├─ DRG_게임_개발_명세서_v1.0.md
│  ├─ DECISIONS.md
│  └─ PROGRESS.md
└─ .claude/
   ├─ settings.json
   ├─ settings.local.json.example
   ├─ hooks/
   │  ├─ check-protected-docs.js
   │  └─ validate-diff.js
   └─ skills/
      ├─ drg-battle/SKILL.md
      ├─ drg-test/SKILL.md
      ├─ drg-review/SKILL.md
      ├─ drg-unity/SKILL.md
      └─ drg-ui/SKILL.md
```

## 설치

이 폴더의 내용을 실제 DRG Unity 프로젝트 루트에 복사합니다.

```text
YourUnityProject/
├─ Assets/
├─ Packages/
├─ ProjectSettings/
├─ CLAUDE.md
├─ Docs/
└─ .claude/
```

이미 `Docs`가 있다면 파일을 덮어쓰기 전에 기존 문서를 백업하세요.

## Claude Code 실행

프로젝트 루트에서:

```bash
claude
```

첫 세션에서는 다음과 같이 요청하는 것을 권장합니다.

```text
Read CLAUDE.md, Docs/DRG_게임_기획서_v1.0.md,
Docs/DRG_게임_개발_명세서_v1.0.md, Docs/DECISIONS.md,
and Docs/PROGRESS.md.

Do not modify files yet.
Summarize the current architecture, unresolved design questions,
and the smallest next implementation step.
```

## 권장 개발 흐름

```text
요청
 ↓
명세/코드 조사
 ↓
영향 파일 확인
 ↓
최소 변경
 ↓
테스트/컴파일
 ↓
git diff 확인
 ↓
문서/진행상황 갱신
 ↓
작은 단위 커밋
```

## Skills

Claude가 작업 내용에 따라 다음 Skill을 활용할 수 있도록 구성되어 있습니다.

- `drg-battle`: 전투/턴/행동/판정
- `drg-test`: 테스트/회귀/시뮬레이션
- `drg-review`: 변경사항 리뷰
- `drg-unity`: Unity/Scene/Prefab/MonoBehaviour/Animation
- `drg-ui`: UI/행동선택/타겟선택/정보표시

## Hooks

현재 Hook은 두 가지입니다.

1. 보호 문서 수정 경고
   - 기획서/개발 명세서를 수정할 때 경고를 출력합니다.
   - 수정 자체를 차단하지는 않습니다.

2. 변경 후 `git diff --check`
   - whitespace 오류 등 기본 diff 문제를 검사합니다.
   - 문제가 발견되면 해당 도구 작업을 실패 처리합니다.

Unity 프로젝트의 실제 컴파일/테스트 명령은 프로젝트의 Unity 버전과 테스트 환경이 정해진 후 추가하는 것이 좋습니다.

## 중요

`DECISIONS.md`는 실제로 합의된 내용만 기록합니다.

특히 현재 다음 사항은 자동으로 결정하지 않습니다.
- 동시 전멸 시 결과
- 정확한 target snapshot/state snapshot 정책
- 장기 교착 방지 규칙
- Block/Teleport 밸런스

## Git 안전장치

기본 설정에서는 다음 위험 명령을 허용 목록에 넣지 않았습니다.

- force push
- `git reset --hard`
- `git clean -fd`

실제 프로젝트에서 필요할 경우 명시적으로 승인하고 실행하세요.
