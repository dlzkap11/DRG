# 01. STEP 1: 기본 데이터 모델

- 날짜: 2026-09-28
- 마일스톤: M1 전투 로직 기초
- 기준: 개발 명세서 §3~§8, §19, §24 STEP 1

## 목표
전투 로직이 공통으로 사용할 데이터 타입을 순수 C#으로 만든다. MonoBehaviour, UI, 게임 규칙 판정은 이번 단계에 넣지 않는다.

## 참고한 문서와 파일
- `CLAUDE.md`, 기획서 v1.0, 명세서 v1.0, `DECISIONS.md`, `PROGRESS.md`
- `Packages/manifest.json`: Unity Test Framework 1.6.0이 설치돼 있는지 확인했다.
- `Assets/`: 기존 C# 스크립트나 asmdef가 없는지 확인했다.

## 변경한 파일
```text
Assets/_Project/Scripts/
├─ DRG.Runtime.asmdef
├─ Core/
│  ├─ GameState.cs
│  └─ GameSettings.cs
└─ Player/
   ├─ PlayerState.cs
   ├─ ActionType.cs
   ├─ PlayerAction.cs
   └─ Player.cs
```
Unity가 만든 `.meta` 파일도 함께 추가됐다.

## 각 타입의 역할
| 타입 | 역할 |
|---|---|
| `GameState` | 게임 화면 흐름 단계 (Lobby / Room / Game / Result) |
| `GameSettings` | 수치 기본값을 한곳에서 관리 (MaxPlayers 8, HP 3/3, Ki 0/3) |
| `PlayerState` | 플레이어 생존 여부 (Alive / Eliminated) |
| `ActionType` | 행동 종류 (None, Gather, EnergyWave, Block, Teleport, SpiritBomb) |
| `PlayerAction` | 한 턴의 행동 선택 (누가, 무엇을, 누구에게). Human과 Bot이 같은 타입을 쓴다. |
| `Player` | 플레이어 상태 (HP, Ki, 생존 여부, 이번 턴 행동) |

## 타입 간 관계
- `Player`는 생성될 때 `GameSettings`에서 초기값을 받는다.
- `Player.State`는 `PlayerState` 값을 가진다.
- `Player.CurrentAction`은 `PlayerAction`을 가리킨다.
- `PlayerAction.ActionType`은 `ActionType` 값을 가진다.

## 구현 내용과 설계 판단
- **명세서 형태를 그대로 따름**: 필드 이름과 종류, public 필드 방식을 명세서 §3~§8과 똑같이 맞췄다.
- **네임스페이스는 `DRG` 하나만 사용**: 폴더 구조대로 `DRG.Player` 같은 네임스페이스를 만들면 `Player` 클래스 이름과 겹쳐서 쓰기 불편해진다.
- **`PlayerAction.NoTarget = -1` 상수 추가**: 명세서는 대상이 없는 행동의 대상 ID를 `-1`로 정했다. 이 숫자를 곳곳에 직접 쓰지 않도록 상수로 만들고, `TargetPlayerId`의 기본값으로 지정했다.
- **`Player(int playerId, string nickname, GameSettings settings)` 생성자 추가**: 명세서 §8은 "숫자를 여러 클래스에 하드코딩하지 않는다"고 한다. 그래서 HP와 Ki 초기값을 `GameSettings`에서 받는다. 생성 직후 상태는 `Alive`이고 `CurrentAction`은 `null`이다.
- **`DRG.Runtime.asmdef` 추가**: Unity 테스트 코드는 어셈블리 정의가 없는 기본 게임 코드를 참조할 수 없다. 앞으로 만들 EditMode 테스트가 게임 로직을 참조하도록 별도 어셈블리로 분리했다. 나중에 UI 스크립트도 같은 어셈블리에 들어갈 수 있게 UnityEngine 참조는 허용했다.
- **이번 단계에서 하지 않은 것**:
  - Lock 상태, 행동 검증, Ki 소모, 전투 판정 → STEP 2~3
  - 테스트 → STEP 4. 이번 단계에는 판정 로직이 없어서 테스트할 규칙이 없다.

## 검증
- Unity 6000.3.22f1을 `-batchmode -nographics -quit`로 실행해 컴파일했다.
  - 결과: `DRG.Runtime.dll` 빌드 성공, `error CS`/`warning CS` 0건, 종료 코드 0
- `git status`로 확인한 결과, 기존 파일은 바뀌지 않았고 새 스크립트와 `.meta`만 추가됐다.
- 자동 테스트는 아직 없다.

## 남은 문제와 설계 질문
`PROGRESS.md`의 Known Design Questions에도 적어 두었다.
- `ActionType.None`의 용도: 살아 있는 플레이어가 None을 제출하는 것을 거부하는지, 탈락자 자동 Lock에 None을 쓰는지 → STEP 2~3 전에 정해야 한다.
- `ActionResultType.Failed`가 어떤 경우에 나오는지, 실패하면 Ki를 소모하는지 → STEP 3 전
- 막히거나, 회피되거나, 상쇄된 공격도 Ki를 소모하는지. 명세서 절차상 소모로 읽히지만 확인이 필요하다. → STEP 3 전
- HP가 0 아래로 내려가면 0에서 멈출지 → STEP 3 전
- `BattleResult`에 탈락자와 승자 정보를 넣을지. 이건 구조 문제라서 제안 후 승인을 받는다. → STEP 3 전

## 다음 단계
STEP 2: `TurnManager`, `TurnState`, Lock, 전원 Lock 확인, 탈락자 자동 Lock, 턴 전환
