# 10. BotAI 시뮬레이션 리포트

- 날짜: 2026-10-28
- 브랜치: `feature/botai-simulation` (`main`에서 분기)
- 기준: 명세서 §27 자동 시뮬레이션 / DEC-008 / DevLog 05(무작위 기준선), 08(BotAI)
- 번호 메모: 09번은 `docs/qa-drafts` 브랜치의 QA 문서 초안이다. 아직 `main`에 합치지 않았다.

## 목표
M3에서 미뤄 둔 BotAI 시뮬레이션을 돌린다. 모든 자리가 BotAI(DEC-008)인 2~8인 게임을 각 1,000판 진행하고, DevLog 05의 무작위 기준선과 같은 조건(seed·판 수·턴 상한)으로 비교한다. 턴 제한과 Block/Teleport 밸런스 논의에 쓸 자료를 만든다.

게임 코드와 규칙은 바꾸지 않았다.

## 변경한 파일
```text
Assets/_Project/Scripts/Simulation/IActionPolicy.cs      (새 파일) ChooseAction(self, players) 인터페이스
Assets/_Project/Scripts/Simulation/RandomActionPolicy.cs (수정) IActionPolicy 구현 선언만 추가
Assets/_Project/Scripts/AI/BotAI.cs                      (수정) IActionPolicy 구현 선언만 추가
Assets/_Project/Scripts/Simulation/BattleSimulator.cs    (수정) 정책을 받는 Run 오버로드 추가
Assets/_Project/Scripts/Simulation/SimulationReport.cs   (수정) 정책 이름·설명·주의 문구를 인자로 받음, 비교 리포트 추가
Assets/_Project/Editor/SimulationRunner.cs               (수정) 두 정책 실행, 리포트 3개 작성
Assets/_Project/Tests/EditMode/SimulationTests.cs        (테스트 7개 추가)
Docs/Simulation/bot_ai_report.md                         (생성됨) BotAI 전체 리포트
Docs/Simulation/policy_comparison.md                     (생성됨) 무작위 vs BotAI 비교
Docs/Simulation/random_policy_report.md                  (다시 생성됨, 내용 변화 없음)
```

## 구현 내용과 설계 판단
- **`IActionPolicy` 인터페이스**: 무작위 정책과 BotAI가 이미 같은 형식의 `ChooseAction(self, players)`를 갖고 있었다. 시뮬레이터가 둘 다 쓸 수 있도록 인터페이스로 묶었다. 구현체가 실제로 두 개라서 미리 만든 추상화가 아니다.
- **`BattleSimulator.Run(…, IActionPolicy)`**: 모든 자리가 같은 정책 인스턴스를 쓴다. 기존 `Run(playerCount, games, seed, turnCap)`은 무작위 정책으로 그대로 동작한다. 두 경로가 같은 결과를 내는지 테스트로 확인했다.
- **리포트 일반화**: 제목, 정책 설명, 해석 주의 문구를 인자로 받는다. 기존 `ToMarkdown(batches)`는 무작위 정책 문구를 그대로 넘긴다.
  - 그래서 `random_policy_report.md`를 다시 생성해도 **바뀐 글자가 하나도 없다** (`git diff` 비어 있음).
  - 이것으로 리포트 코드를 정리한 뒤에도 출력이 같고, 같은 seed면 결과가 재현된다는 것을 확인했다.
- **비교 리포트**: 인원마다 두 정책을 위아래 두 줄로 놓는다. 항목은 다음과 같다.
  - 게임 길이: 평균, 중앙값, 최대, 무승부, 미종료
  - 행동 사용률
  - 공격 결과와 방어 효과
  - 동시 탈락
  - 인원 구성이 다르면 예외를 던진다.
- **같은 조건**: seed 20260928, 인원별 1,000판, 턴 상한 200. 턴 상한은 시뮬레이션 안전장치이며 게임 규칙이 아니다.

## 실행 방법
- 에디터 메뉴 `DRG > Run Simulation`, 또는 에디터를 닫은 상태에서 아래 명령을 실행한다. 리포트 3개가 생성된다.
  ```bash
  "/c/Program Files/Unity/Hub/Editor/6000.3.22f1/Editor/Unity.exe" -batchmode -nographics -quit -projectPath "D:/Project/DRG" -executeMethod DRG.EditorTools.SimulationRunner.Run -logFile unity.log
  ```

## 검증
- EditMode 테스트: **131개 중 131개 통과** (기존 124 + 시뮬레이션 7), 컴파일 오류·경고 0건
  - BotAI 시뮬레이션 2인과 8인 각 100판
    - 모든 판이 승리, 무승부, 미종료 중 하나로 집계됨
    - Failed 0건
    - 원기옥이 실제로 사용됨
  - BotAI 시뮬레이션은 같은 seed면 같은 통계가 나옴
  - 기존 무작위 오버로드와 무작위 정책을 직접 넘긴 경우의 결과가 같음
  - 정책 리포트에 넘긴 제목, 설명, 주의 문구가 들어감
  - 비교 리포트에 인원마다 정책별 행이 들어감
  - 인원 구성이 다르면 예외
- 실제 실행: 커맨드라인으로 무작위 7,000판과 BotAI 7,000판을 돌렸다. 종료 코드는 0이고 리포트 3개가 생성됐다. BotAI의 Failed 행동도 0건이었다.

## 주요 결과 (BotAI vs 무작위 기준선)
전체 표는 [비교 리포트](../Simulation/policy_comparison.md)와 [BotAI 리포트](../Simulation/bot_ai_report.md)에 있다.

| 인원 | 평균 턴 (무작위 → BotAI) | 최대 턴 | 무승부 | 2명 이상 함께 탈락한 턴 |
|---:|---|---:|---:|---:|
| 2 | 36.3 → **12.8** | 106 → 52 | 1.1% → 3.4% | 0.0% → 0.3% |
| 4 | 48.2 → **15.4** | 151 → 45 | 1.4% → 2.3% | 0.2% → 1.2% |
| 8 | 60.0 → **20.3** | 134 → 59 | 2.3% → 3.6% | 0.8% → 4.9% |

관찰한 내용 (수치 그대로이며 해석은 아님):
- **게임 길이**:
  - BotAI 게임은 무작위보다 약 3배 짧다. 2인은 평균 13턴, 8인은 평균 20턴이다.
  - 가장 긴 판도 59턴이고, 턴 상한(200)에 걸린 판은 없다.
  - 두 정책 모두 게임이 무한히 길어지는 현상은 보이지 않는다.
- **행동 사용률 (BotAI)**: 기 모으기 약 53%, 에너지파 약 28%, 방어 약 10%, 순간이동 약 5%, 원기옥 약 3.5%.
  - DEC-008 규칙대로 방어와 순간이동은 HP 1일 때만 나온다. 그래서 무작위(방어 36%, 순간이동 14%)보다 훨씬 적다.
- **공격 결과 (BotAI)**:
  - 에너지파 적중률은 51%(2인)에서 76%(8인)로, 무작위(35~46%)보다 높다. 상대가 거의 방어하지 않기 때문이다.
  - 원기옥 적중률은 약 95%다. 순간이동은 HP 1일 때 20% 확률로만 쓰이기 때문이다.
  - 2인 에너지파 상쇄는 36.6%로 높다. 두 봇 모두 Ki 1~2에서 50% 확률로 서로에게 에너지파를 쏘기 때문이다. 인원이 늘면 대상이 흩어져 상쇄가 줄어든다(8인 8.5%).
- **방어 효과**: 방어 중 실제로 공격을 막은 경우 약 22~25%, 순간이동 중 실제로 피한 경우 약 28~31%. 무작위의 약 14~15%보다 높은데, HP 1일 때 공격이 몰리는 상황에서 방어를 쓰기 때문이다.
- **동시 탈락**: 인원이 많을수록 늘어나고(8인 턴의 4.9%), 무승부는 2~4% 수준이다.
- **좌석별 승률**:
  - BotAI에서도 대체로 고르다. 8인에서 기대값 12.5%보다 높은 칸은 P6 15.2%, 낮은 칸은 P3 11.0% 정도다.
  - 무작위 기준선에서는 P6이 9.2%로 낮았는데 BotAI에서는 반대로 높게 나왔다.
  - 특정 좌석이 꾸준히 유리하거나 불리한 패턴이 없으므로 **표본 오차일 가능성을 뒷받침한다**. 단정하려면 더 큰 표본이 필요하다.

## 남은 문제와 설계 질문
- **턴 제한**: BotAI 게임은 평균 13~20턴에 끝나고 최대 59턴이다. 봇끼리는 교착 상태가 생기지 않았다. 다만 사람이 방어와 순간이동을 적극적으로 쓰면 결과가 다를 수 있어 결론은 미룬다. → PROGRESS 갱신
- **Block/Teleport 밸런스**: BotAI는 HP 1일 때만 방어해서, 방어 행동의 가치를 판단하기에는 여전히 부족하다. 방어를 더 자주 쓰는 봇이나 실제 플레이 데이터가 있어야 한다. → PROGRESS 갱신
- 좌석별 승률 편차: 두 정책에서 서로 다른 좌석이 튀었다. 표본 오차일 가능성이 높아졌지만 미해결로 둔다.
- 이번 결과를 근거로 BotAI 값(DEC-008)을 조정할지는 소유자가 정한다. 이번 작업에서는 값을 바꾸지 않았다.

## 다음 단계
- M3 플레이 확인과 M3 완료 처리 (확인용 빌드를 다시 만들어야 함)
- M4 Human + 7 Bots
