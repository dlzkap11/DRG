# 02. STEP 2: 턴 관리와 행동 검증

- 날짜: 2026-09-28
- 브랜치: `feature/step2-turn-manager`
- 마일스톤: M1 전투 로직 기초
- 기준: 개발 명세서 §6, §9, §10, §24 STEP 2 / `DECISIONS.md` DEC-001

## 목표
턴 진행 순서(턴 시작 → 행동 선택 → Lock → 전원 Lock 확인 → 해결 시작 → 턴 종료)를 순수 C#으로 만든다. Lock할 때 행동이 규칙에 맞는지 검증한다.

## 작업 전에 확정한 사항 (프로젝트 소유자 결정)
| 질문 | 결정 |
|---|---|
| `ActionType.None`의 용도 | 탈락자 자동 Lock 전용. 살아 있는 플레이어가 None을 내면 거부한다. → **DEC-001**로 기록 |
| 행동 검증을 넣을 시점 | 명세서는 STEP 3이지만 이번 STEP 2에 `ActionValidator`를 포함한다. 검증 없이 Lock을 받는 상태를 만들지 않기 위해서다. |
| 테스트 시점 | 명세서는 STEP 4이지만 이번 단계부터 EditMode 테스트를 추가한다. |

## 변경한 파일
```text
Assets/_Project/Scripts/
├─ Core/GameSettings.cs                (수정: Ki 비용 필드 추가)
└─ Battle/
   ├─ ActionValidationResult.cs        (새 파일)
   ├─ ActionValidator.cs               (새 파일)
   ├─ LockResult.cs                    (새 파일)
   ├─ TurnState.cs                     (새 파일)
   └─ TurnManager.cs                   (새 파일)
Assets/_Project/Tests/EditMode/
├─ DRG.Tests.EditMode.asmdef           (새 파일)
├─ ActionValidatorTests.cs             (새 파일)
└─ TurnManagerTests.cs                 (새 파일)
Docs/DECISIONS.md                      (DEC-001 추가)
Docs/PROGRESS.md                       (진행 상태 갱신)
```

## 구현 내용과 설계 판단

### ActionValidator
`Validate(PlayerAction, List<Player>)`는 첫 번째로 걸리는 문제를 `ActionValidationResult`로 돌려준다. 검사 순서는 다음과 같다.

1. 행동이 없음 → `MissingAction`
2. 행동한 플레이어가 없음 → `PlayerNotFound`
3. 행동한 플레이어가 탈락함 → `PlayerEliminated`
4. `None`이거나 정의되지 않은 행동 → `InvalidActionType` (DEC-001)
5. Ki가 최대치인데 Gather → `KiAtMax`
6. Ki 부족 → `InsufficientKi`
7. 공격(EnergyWave, SpiritBomb)
   - 대상이 자기 자신 → `SelfTarget`
   - 대상이 없음 → `TargetNotFound`
   - 대상이 탈락함 → `TargetEliminated`
8. 공격이 아닌 행동에 대상 ID가 `-1`이 아님 → `UnexpectedTarget`

설계 판단:
- **Ki 비용은 `GameSettings`에서 관리**: `EnergyWaveKiCost` 1, `TeleportKiCost` 1, `SpiritBombKiCost` 3을 추가했다. 명세서 §8의 "숫자 하드코딩 금지"를 따른 것이다. `GetKiCost(ActionType)`는 public이라 STEP 3의 BattleResolver도 같은 값을 쓸 수 있다.
- **공격이 아닌 행동에 대상을 지정하면 거부**: 명세서 §6은 "대상이 필요 없는 행동은 -1"이라고 한다. 이건 입력 형식을 검사하는 것이지 게임 규칙을 새로 만든 것이 아니다.
- **GameState 검사는 보류**: 명세서 §9에는 "현재 GameState에서 행동 가능 여부" 검사가 있다. 하지만 `GameState`를 관리하는 클래스가 아직 없다. 지금은 TurnManager가 `ActionSelection` 단계에서만 Lock을 받는 것으로 대신한다. 이 문제는 PROGRESS의 설계 질문에 올렸다.

### TurnManager
- **상태 전환**: `StartTurn` → `StartActionSelection` → `Lock` × N → `StartResolution` → `EndTurn` → 다시 `StartTurn`
  - 순서가 틀린 전환은 `InvalidOperationException`을 던진다. 이런 호출은 프로그램 흐름 오류이기 때문이다.
  - 첫 턴 전에는 상태가 `TurnEnd`이고 턴 번호는 `0`이다. 명세서의 `TurnState`에 "시작 전" 값이 없어서 이렇게 정했다.
- **`Lock(PlayerAction)`**: 결과를 `LockResult`로 돌려준다.
  - `ActionSelection` 단계가 아니면 `WrongTurnState`
  - 이미 Lock했으면 `AlreadyLocked`
  - 검증에 실패하면 `InvalidAction`
  - 통과하면 행동을 **복사해서** `Player.CurrentAction`에 저장한다. 호출한 쪽이 원래 객체를 나중에 바꿔도 Lock된 행동은 바뀌지 않는다.
  - Lock할 때는 Ki를 소모하지 않는다. Ki 소모는 STEP 3의 전투 해결에서 한다.
- **탈락자 자동 Lock**: `StartActionSelection`에서 탈락한 플레이어에게 `None` 행동을 넣고 Lock된 것으로 처리한다(명세서 §10, DEC-001).
- **이전 턴 정리**: `StartTurn`에서 Lock 기록과 모든 플레이어의 `CurrentAction`을 비운다.
- **이번 단계에서 하지 않은 것**: `StartResolution`은 상태만 `ActionResolution`으로 바꾼다. 실제 전투 해결(BattleResolver 호출)은 STEP 3에서 한다.

### 테스트 어셈블리
- 이름: `DRG.Tests.EditMode`
- `DRG.Runtime`을 참조하고, 에디터에서만 컴파일된다.
- `UNITY_INCLUDE_TESTS` 조건이 있어서 게임 빌드에는 포함되지 않는다.

## 검증
- Unity 6000.3.22f1을 커맨드라인으로 실행했다: `-batchmode -nographics -runTests -testPlatform EditMode`
- 결과: **44개 중 44개 통과**, 실패 0, 건너뜀 0. 컴파일 오류·경고 0건.
  - `ActionValidatorTests` 25개
    - 행동 5종이 조건을 만족하면 통과
    - Ki가 최대치일 때 Gather 거부
    - Ki 부족 3종, Ki 비용 표
    - 자기 자신 대상, 탈락자 대상, 없는 대상, 대상 누락
    - 공격이 아닌 행동에 대상 지정
    - 살아 있는 플레이어의 None, 정의되지 않은 행동
    - 탈락자나 없는 플레이어의 행동, 행동 없음(null)
  - `TurnManagerTests` 19개
    - 상태 전환 순서, 잘못된 전환 시 예외
    - 새 턴에서 Lock과 이전 행동 초기화
    - Lock된 행동을 복사해서 저장, Lock할 때 Ki 미소모
    - 두 번 Lock 거부(첫 행동 유지), 원래 객체를 바꿔도 Lock된 행동 유지
    - 잘못된 행동·None·null 거부, 선택 단계가 아닐 때 Lock 거부
    - 전원 Lock 확인, 전원 Lock 전 해결 시작 시 예외
    - 탈락자 자동 None Lock, 탈락자가 전원 Lock을 막지 않음, 탈락자의 추가 Lock 거부

## 남은 문제와 설계 질문
- `GameState`를 누가 관리하는지, "현재 GameState에서 행동 가능 여부" 검사를 어디서 할지 → PROGRESS에 추가
- STEP 3 전에 정해야 할 것 (PROGRESS에 이미 있음)
  - `ActionResultType.Failed`가 언제 나오는지
  - 막히거나, 회피되거나, 상쇄된 공격도 Ki를 소모하는지
  - HP가 0 아래로 내려가면 0에서 멈출지
  - `BattleResult`에 탈락자와 승자 정보를 넣을지
  - 생존자가 0명일 때 결과

## 다음 단계
STEP 3: `BattleResolver`와 `BattleResult`
- 행동 비용 처리
- 공격, 방어, 순간이동, 원기옥 판정
- 에너지파 상호 상쇄, 다중 공격, 동시 사망, 승리 판정
