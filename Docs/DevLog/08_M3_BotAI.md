# 08. M3: BotAI (규칙 기반 봇)

- 날짜: 2026-09-28
- 브랜치: `feature/m3-bot-ai`
- 마일스톤: M3 Human + Bot 전체 게임
- 기준: 기획서 §12 AI / 명세서 §20 BotPlayer·BotAI, §24 STEP 7 / DEC-008

## 목표
M2에서 임시로 쓰던 무작위 봇을 기획서 규칙을 따르는 BotAI로 교체한다. 사람이 "생각하는 것처럼 보이는" 봇과 한 판을 끝까지 할 수 있게 한다.

## 작업 전에 확정한 사항 (프로젝트 소유자 결정 → DEC-008)
| 질문 | 결정 |
|---|---|
| Ki 1~2에서 에너지파와 기 모으기 비율, Ki 3에서 원기옥과 에너지파 비율 | 반반 |
| "HP가 낮으면 방어/순간이동 확률 증가"의 구체 값 | HP 1이면 40% 확률로 방어 행동. Ki가 있으면 방어와 순간이동 반반, Ki 0이면 방어만 |
| 공격 대상 | 살아 있는 상대 중 무작위 |
| 이번 범위 | 게임 봇 교체만. BotAI 시뮬레이션 리포트는 다음에 하고, 봇 수 선택은 M4에서 한다. |

## 변경한 파일
```text
Assets/_Project/Scripts/AI/BotAI.cs                 (새 파일) 규칙 기반 봇
Assets/_Project/Scripts/AI/BotPlayer.cs             (수정) RandomActionPolicy 대신 BotAI 사용
Assets/_Project/Scripts/Core/LocalBattleSession.cs  (수정) 봇 생성 시 BotAI 사용 (한 줄)
Assets/_Project/Tests/EditMode/BotAITests.cs        (새 파일, 테스트 12개)
Docs/DECISIONS.md (DEC-008, DEC-007에 교체 완료 표시), Docs/PROGRESS.md
```

## 구현 내용과 설계 판단
- **`BotAI.ChooseAction(self, players)`**: 입력과 출력 형식이 `RandomActionPolicy`와 같다. 규칙은 다음과 같다.
  1. `HP <= 1`이면 `NextDouble() < 0.4`일 때 방어한다. Ki가 순간이동 비용 이상이면 방어와 순간이동을 반반으로, 아니면 방어를 고른다.
  2. 그 외에는 Ki에 따라 고른다.
     - Ki 0: 기 모으기
     - 최대치 미만: 에너지파와 기 모으기 반반
     - 최대치: 원기옥과 에너지파 반반
  3. 공격이면 살아 있는 상대 중 무작위로 대상을 고른다.
- **안전장치**: 규칙상 항상 규칙에 맞는 행동이 나온다. 그래도 `ActionValidator`로 한 번 더 확인하고, 실패하면 방어(항상 가능)로 바꾼다. Lock할 때도 `TurnManager`가 다시 검증하므로, 사람과 봇의 검증 경로는 같다.
- **Human과 Bot 통합 유지**: BotAI는 평범한 `PlayerAction`만 만든다. 전투 코드에는 봇을 위한 분기가 없다.
- **조정값 위치**: `LowHpThreshold = 1`과 `DefenseChanceAtLowHp = 0.4`는 `BotAI` 안에 상수로 두었다. 게임 규칙 수치(`GameSettings`)가 아니라 봇 성향 값이기 때문이다.
  - 비율 50/50은 코드의 `random.Next(2)`로 표현했다.
  - 난이도가 생기면 이 값들을 설정으로 빼는 것을 검토한다.
- **결정성**: seed를 받는 `System.Random`을 쓴다. 게임에서는 `BattleScreen`이 시작 시각(`Environment.TickCount`)을 seed로 넘기므로 판마다 다르게 행동한다.
- **`RandomActionPolicy`는 유지**: 시뮬레이션 기준선(DevLog 05)으로 계속 쓴다.

## 검증
- EditMode 테스트: **124개 중 124개 통과** (기존 112 + BotAI 12), 컴파일 오류·경고 0건
  - `BotAITests` (12)
    - Ki 0이면 항상 기 모으기
    - Ki 1과 2에서 에너지파·기 모으기 각 약 50%
    - Ki 3에서 원기옥·에너지파 각 약 50%
    - HP 2에서는 방어하지 않음
    - HP 1·Ki 0에서 방어 약 40%, 기 모으기 약 60%
    - HP 1·Ki 1에서 방어 20%, 순간이동 20%, 에너지파 30%, 기 모으기 30%
    - HP 1·Ki 3에서 방어 20%, 순간이동 20%, 원기옥 30%, 에너지파 30%
    - 대상은 살아 있는 상대만, 그리고 고르게 선택됨
    - 모든 Ki·HP 조합에서 규칙에 맞는 행동만 고름
    - 같은 seed → 같은 선택
    - `BotPlayer`가 자기 자리의 행동을 만듦
    - 비율은 seed를 고정한 2,000회 추출로 확인했고, 허용 오차는 ±5%p다.
  - 기존 `LocalBattleSessionTests`와 `BattleViewTests`(끝까지 플레이, 게임 종료 화면 포함)도 BotAI 봇으로 모두 통과했다.
- Windows 플레이어 빌드: `build3/DRG.exe`, `Build Finished, Result: Success`
  - 빌드할 때 Unity가 다시 저장한 관계없는 설정 파일 5개는 되돌렸다.
- **확인하지 못한 것**: 새 빌드로 실제 플레이는 하지 않았다. 화면은 바뀌지 않았고 봇의 행동 방식만 바뀌었다.

## 남은 문제와 설계 질문
- BotAI 시뮬레이션 리포트: 봇끼리 대전해서 턴 수, 행동 비율, 무승부를 무작위 기준선과 비교한다. 소유자 결정에 따라 다음 작업으로 미뤘다. 턴 제한과 밸런스 판단의 근거가 된다.
- 난이도(Easy/Normal/Hard), 집중 공격 같은 전략적 대상 선택 → 이후 작업
- M4: 봇 수 선택(최대 7명)과 다인전 화면

## 다음 단계
- 소유자 플레이 확인 → M3 완료 처리와 `M3_요약.md`
- 이후 BotAI 시뮬레이션 리포트, 또는 M4(Human + 7 Bots)
