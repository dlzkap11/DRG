# 03. STEP 3: 전투 해결 (BattleResolver)

- 날짜: 2026-09-28
- 브랜치: `feature/step3-battle-resolver`
- 마일스톤: M1 전투 로직 기초
- 기준: 기획서 §4~§6, §9 / 명세서 §11~§14, §24 STEP 3, §26 / `DECISIONS.md` DEC-001~006

## 목표
모든 플레이어가 Lock한 행동으로 한 턴을 처리하는 `BattleResolver`와, 그 결과를 담는 `BattleResult`를 만든다. 처리 범위는 다음과 같다.
- Ki 처리
- 행동 5종의 상호작용, 에너지파 상호 상쇄, 다중 공격
- 동시 사망, 탈락 처리, 승리·무승부 판정

## 작업 전에 확정한 사항 (프로젝트 소유자 결정)
| # | 질문 | 결정 |
|---|---|---|
| DEC-002 | 막히거나, 회피되거나, 상쇄된 공격의 Ki | 항상 소모 |
| DEC-003 | HP가 0 아래로 내려가는지 | 0에서 멈춤. DamageResult에는 실제 데미지를 그대로 기록 |
| DEC-004 | 생존자가 0명일 때 | 무승부 (승자 없음) |
| DEC-005 | 전투 처리 중 잘못된 행동 | `Failed`로 기록하고 효과 없음, Ki도 변하지 않음 |
| DEC-006 | BattleResult에 탈락과 승리 정보를 넣을지 | 넣음: `EliminatedPlayerIds`, `Outcome`, `WinnerPlayerId` |

## 변경한 파일
```text
Assets/_Project/Scripts/
├─ Core/GameSettings.cs              (수정: GatherKiGain, EnergyWaveDamage, SpiritBombDamage 추가)
└─ Battle/
   ├─ BattleResolver.cs              (새 파일)
   ├─ BattleResult.cs                (새 파일)
   ├─ ActionResult.cs                (새 파일)
   ├─ ActionResultType.cs            (새 파일)
   ├─ DamageResult.cs                (새 파일)
   ├─ KiChangeResult.cs              (새 파일)
   └─ GameOutcome.cs                 (새 파일)
Assets/_Project/Tests/EditMode/
└─ BattleResolverTests.cs            (새 파일)
Docs/DECISIONS.md                    (DEC-002 ~ DEC-006 추가)
Docs/PROGRESS.md                     (진행 상태 갱신)
```

## 처리 절차
`ResolveTurn(List<Player>)`는 명세서 §11의 권장 순서를 따른다.

1. **행동 수집과 검증**
   - 살아 있는 플레이어의 `CurrentAction`을 전투 처리 시작 시점의 상태로 다시 검증한다.
   - 이미 탈락한 플레이어는 건너뛴다(DEC-001).
   - 행동이 없거나, 다른 플레이어의 행동이거나, 검증에 실패하면 `Failed`로 기록하고 이후 처리에서 뺀다(DEC-005).
2. **Ki 적용**: Gather는 +1(최대치를 넘지 않음), 나머지는 비용만큼 뺀다. 공격 결과와 관계없이 적용한다(DEC-002). 변화량이 0이 아닌 경우만 `KiChanges`에 기록한다.
3. **공격 판정**: 에너지파와 원기옥마다 대상의 행동을 보고 판정한다.
   - 대상이 순간이동 → `Dodged` (에너지파와 원기옥 모두)
   - 원기옥 → `Hit` (방어 무시, 상쇄되지 않음)
   - 에너지파이고, 대상이 **바로 이 공격자에게** 에너지파를 쏨 → `Cancelled` (정확히 A↔B일 때만)
   - 에너지파이고 대상이 방어 → `Blocked`
   - 그 외 → `Hit`
4. **데미지 합산**: `Hit`마다 `DamageResult`를 만들고, 대상별로 데미지를 더한다.
5. **HP 적용**: `HP = max(0, HP - 합산 데미지)` (DEC-003)
6. **탈락 처리**: HP가 0 이하인 생존자를 `Eliminated`로 바꾸고 `EliminatedPlayerIds`에 넣는다.
7. **승리 판정**: 생존자 1명이면 `Winner`, 0명이면 `Draw`(DEC-004), 2명 이상이면 `Ongoing`

## 설계 판단
- **동시성 보장**:
  - 공격 판정은 Lock된 행동만 보고, HP는 보지 않는다. 그래서 처리 순서가 결과에 영향을 주지 않는다.
  - 이번 턴에 HP가 0이 되는 플레이어의 공격도 그대로 적용된다. 예를 들어 A→B, B→C, C→A이고 모두 HP 1이면 셋 다 탈락한다.
- **실패한 행동은 방어도 되지 않음**: 대상의 행동이 `Failed`이면 방어나 순간이동 효과가 없다. DEC-005의 "효과 없음"을 그대로 적용한 것이다.
- **ActionResult를 기록하는 행동**:
  - 공격 행동(에너지파, 원기옥)과 `Failed` 행동만 기록한다.
  - 성공한 Gather, Block, Teleport는 `ActionResultType`에 맞는 값이 없어서 기록하지 않는다. 효과는 `KiChanges`로 알 수 있다.
  - 공개 단계에서 모든 행동을 보여줄 때는 각 플레이어의 `CurrentAction`을 읽으면 된다.
- **수치는 `GameSettings`에서 관리**: `KiChangeResult`와 `ActionResult`의 필드는 명세서에 이름만 있어서 최소한으로 정했다. Gather 증가량과 에너지파·원기옥 데미지를 `GameSettings`로 옮겨 코드에 숫자를 직접 쓰지 않았다.
- **Ki 비용 재사용**: `ActionValidator.GetKiCost`를 그대로 써서 Lock 검증과 전투 처리의 비용이 항상 같다.
- **의존성**: UI, Animator, VFX, GameObject 등 Unity 객체에 의존하지 않는다. `System`과 `System.Collections.Generic`만 사용한다.

## 검증
- Unity 6000.3.22f1을 커맨드라인으로 실행했다: `-batchmode -nographics -runTests -testPlatform EditMode`
- 결과: **73개 중 73개 통과**, 실패 0. 컴파일 오류·경고 0건.
  - ActionValidatorTests 25개, TurnManagerTests 19개: STEP 2 테스트, 이번에도 모두 통과
  - BattleResolverTests 29개: 이번에 추가
    - 행동 5종의 Ki 변화: Gather +1, Block 0, Teleport -1, 에너지파 -1, 원기옥 -3
    - 에너지파 vs 기 모으기, 방어, 순간이동, 에너지파(상호 상쇄), 원기옥
    - 원기옥 vs 기 모으기, 방어, 순간이동, 원기옥 (서로 쏴도 상쇄되지 않고 둘 다 적중)
    - 막히거나, 회피되거나, 상쇄돼도 Ki 소모 (DEC-002)
    - 같은 대상을 공격하면 상쇄되지 않고 데미지 누적(A→C, B→C)
    - 연쇄 공격은 상쇄되지 않음(A→B, B→C)
    - 상쇄된 쌍이 있어도 제3자의 공격은 적중
    - 순간이동은 여러 공격을 한꺼번에 회피
    - 방어는 여러 에너지파를 막지만 원기옥은 못 막음
    - 여러 공격을 맞아도 HP는 0에서 멈추고 DamageResult에는 전체 데미지가 남음 (DEC-003)
    - 이번 턴에 탈락하는 플레이어의 행동도 적용됨: 순환 공격, 탈락하는 플레이어의 Gather Ki
    - 이미 탈락한 플레이어는 건너뜀
    - 결과 판정: 승리, 무승부(DEC-004), 진행 중
    - Failed 처리(DEC-005): 잘못된 행동, 행동 없음, 다른 플레이어의 행동, 실패한 방어는 보호 효과 없음

## 남은 문제와 설계 질문
- **TurnManager와 연결되지 않음**: `TurnManager.StartResolution`은 아직 상태만 바꾸고 `BattleResolver`를 호출하지 않는다. 승리나 무승부 뒤에 다음 턴을 막는 처리도 없다. → PROGRESS In Progress
- 무승부를 플레이어 전적(승/패)에 어떻게 기록할지 → PROGRESS 설계 질문 (명세서 §14/§23)
- GameState를 누가 관리하는지 (STEP 2에서 넘어온 질문)
- 교착 상태 방지와 턴 제한 규칙, Block/Teleport 밸런스 → 플레이테스트와 자동 시뮬레이션으로 판단

## 다음 단계
다음 중 하나를 고른다.
1. `TurnManager.StartResolution`이 `BattleResolver`를 호출해 `BattleResult`를 돌려주게 하고, 게임이 끝나면 다음 턴을 막는다. M1 마무리에 해당한다.
2. 명세서 §27 자동 시뮬레이션: 랜덤 행동을 seed로 고정하고 1,000판 이상 돌려 평균 턴 수, 행동별 사용률, 무승부 비율 등을 측정한다.
