# UI 수동 테스트 케이스

- 대상: M4 화면 (사람 1 + 봇 1~7), `build4/DRG.exe` (`feature/m4-multiplayer` 커밋 `9641c7c`, DevLog 11)
  - 2인전 케이스(A~H)는 설정 패널에서 **봇 1명**을 골라 진행한다.
  - 다인전 케이스(I~K)는 봇 수를 케이스에 적힌 대로 고른다.
- 기준: 명세서 §26, CLAUDE.md §9 UI 규칙, 기획서 §6·§8·§16, DevLog 06·07·11, DEC-007·DEC-009
- 화면 요소 이름: `Assets/_Project/UI/BattleScreen.uxml`, 동작: `Assets/_Project/Scripts/UI/BattleView.cs`
- 목적: 자동 테스트(`BattleViewTests`)가 보지 못하는 실제 마우스·키보드 조작, 화면 배치, 문구를 확인한다.
- 변경 이력:
  - 2026-09-28: 2인 화면(M3) 기준 UI-01~24 작성
  - 2026-10-28: M4 반영. UI-01·UI-10·UI-18 수정, UI-25~40 추가

"관련 자동 테스트"가 있는 케이스는 로직이 이미 자동으로 확인된다. 이런 케이스는 **실제 빌드에서 눈으로 같은지**만 보면 된다. 이 칸이 비어 있는 케이스가 수동 확인의 핵심이다.

## 확인 정보
| 항목 | 내용 |
|---|---|
| 확인한 사람 | |
| 날짜 | |
| 빌드 / 커밋 | |
| 화면 크기 / 전체 화면 여부 | |

## 화면 요소 (UXML 이름)
| 요소 | 이름 | 화면에 보이는 글자 |
|---|---|---|
| 설정 패널 | `setup-panel`, `bot-count-1` ~ `bot-count-7`, `start-button` | `New Game`, `Number of bots`, `1`~`7`, `Start` |
| 게임 화면 전체 | `game-root` | 설정 패널이 보일 때는 숨겨짐 |
| 턴 / 단계 | `turn-label`, `phase-label` | `Turn N`, `Choose your action and lock it` / `Actions revealed` / `You are eliminated - spectating` / `Game over` |
| 플레이어 카드 | `player-card-0`(나), `player-card-1`~`player-card-7`(Bot 1~7) | 이름, `HP x / 3`, `Ki x / 3`, 상태(`Choosing...` / `Locked` / `Eliminated`), `Action: …` |
| 행동 버튼 | `gather-button`, `energy-wave-button`, `block-button`, `teleport-button`, `spirit-bomb-button` | `Gather (+1 Ki)` 등 |
| 대상 패널 | `target-panel`, `target-button-<id>` | `Choose a target`, 상대 이름 |
| 선택 / Lock | `selection-label`, `lock-button` | `Select an action.` / `Selected: …`, `Lock` |
| 공개 패널 | `result-panel`, `result-label`, `next-turn-button`, `skip-button` | `Reveal`, 이번 턴 **판정 결과만** (없으면 `No attacks this turn.`), `Next Turn`, `Skip to End`(관전 중에만) |
| 종료 패널 | `game-over-panel`, `game-over-label`, `restart-button` | 결과 문구, `Play Again` |
| 전투 로그 | `battle-log` | `-- Turn N --` 등 |

## 케이스
결과 칸: `통과` / `실패` / `확인 못 함`

### A. 시작 화면
| ID | 전제 | 조작 | 기대 결과 | 관련 자동 테스트 | 결과 | 비고 |
|---|---|---|---|---|---|---|
| UI-01 | 설정 패널에서 봇 1명 → `Start` | 없음 | `Turn 1`, 단계 `Choose your action and lock it`. 두 카드 모두 `HP 3 / 3`, `Ki 0 / 3`. 내 상태 `Choosing...`, Bot 1 상태 `Locked`, Bot 1 행동 `? (hidden)`. 대상·공개·종료 패널은 보이지 않음 | `InitialScreen_ShowsTurnAndHidesOpponentAction`, `InitialScreen_EnablesOnlyLegalActions` | | |

### B. 행동 버튼
| ID | 전제 | 조작 | 기대 결과 | 관련 자동 테스트 | 결과 | 비고 |
|---|---|---|---|---|---|---|
| UI-02 | 내 Ki 0 | 없음 | Gather, Block만 켜짐. Energy Wave, Teleport, Spirit Bomb은 흐리게 꺼짐. Lock 꺼짐 | `InitialScreen_EnablesOnlyLegalActions` | | |
| UI-03 | 내 Ki 3 (Gather 세 번) | 없음 | **Gather가 꺼짐**. Energy Wave, Block, Teleport, Spirit Bomb은 켜짐 | 없음 | | DevLog 07에서 한 번 확인됨 |
| UI-04 | 내 Ki 1 또는 2 | 없음 | Gather, Energy Wave, Block, Teleport 켜짐. Spirit Bomb 꺼짐 | 없음 | | |
| UI-05 | 행동 선택 가능 | Block 클릭 → Gather 클릭 | 마지막에 누른 버튼만 강조됨. `selection-label`이 `Selected: Block` → `Selected: Gather`로 바뀜. Lock 전에는 자유롭게 바꿀 수 있음 | `SelectAndLock_RevealsTurnAndWritesLog` (강조만) | | |
| UI-06 | 내 Ki 0 | 꺼진 Energy Wave 버튼 클릭 | 아무 변화 없음. 선택 문구와 Lock 상태 그대로 | `SelectingIllegalAction_IsIgnored` | | |

### C. 대상 선택
| ID | 전제 | 조작 | 기대 결과 | 관련 자동 테스트 | 결과 | 비고 |
|---|---|---|---|---|---|---|
| UI-07 | 내 Ki 1 이상 | Energy Wave 클릭 | 대상 패널이 나타나고 `Bot 1`만 있음 (나는 없음). 문구 `Selected: Energy Wave -> choose a target`. Lock 꺼짐 | `AttackFlow_RequiresTargetBeforeLock` | | |
| UI-08 | UI-07 다음 | `Bot 1` 클릭 | 대상 버튼 강조, 문구 `Selected: Energy Wave -> Bot 1`, Lock 켜짐 | `AttackFlow_RequiresTargetBeforeLock` | | |
| UI-09 | UI-08 다음 | Block 클릭 → 다시 Energy Wave 클릭 | Block을 누르면 대상 패널이 사라짐. 다시 Energy Wave를 누르면 대상이 초기화되어 Lock이 꺼져 있음 | 없음 | | 대상이 남아 있으면 실패 |

### D. Lock과 공개
| ID | 전제 | 조작 | 기대 결과 | 관련 자동 테스트 | 결과 | 비고 |
|---|---|---|---|---|---|---|
| UI-10 | 선택 완료 | Lock 클릭 | 바로 공개됨: 단계 `Actions revealed`, Bot 1 행동이 카드에 실제 행동으로 바뀜, 공개 패널에 이번 턴 **판정 결과만**(공격이 없으면 `No attacks this turn.`), `Next Turn` 보임, `Skip to End`는 안 보임, 행동 버튼 모두 꺼짐 | `SelectAndLock_RevealsTurnAndWritesLog`, `RevealPanel_ShowsOutcomesOnly`, `SkipButton_IsHidden_WhileHumanIsAlive` | | M4에서 공개 패널이 판정 결과만 보여주도록 바뀜 (DEC-009) |
| UI-11 | 선택 완료 | Lock을 빠르게 여러 번 클릭 | 턴이 한 번만 해결됨. 로그에 같은 턴이 한 번만 있음. Ki·HP가 두 번 바뀌지 않음 | 없음 | | |
| UI-12 | 공개 상태 | 꺼진 행동 버튼, 대상 버튼 자리 클릭 | 아무 변화 없음. 공개된 내 행동이 바뀌지 않음 | 없음 | | Lock 후 변경 방지 (§26) |
| UI-13 | 공개 상태 | 카드와 공개 패널 비교 | 카드의 HP·Ki가 판정과 맞음 (예: Hit면 대상 HP -1, 공격자 Ki -비용, Blocked·Dodged·Cancelled여도 Ki는 소비) | 없음 | | DEC-002 |

### E. 다음 턴
| ID | 전제 | 조작 | 기대 결과 | 관련 자동 테스트 | 결과 | 비고 |
|---|---|---|---|---|---|---|
| UI-14 | 공개 상태 | Next Turn 클릭 (빠르게 여러 번도 시도) | 턴 번호가 1만 늘어남. 공개 패널 사라짐, Bot 1 행동 다시 `? (hidden)`, 내 선택 초기화, 문구 `Select an action.` | `NextTurn_StartsTurnTwo_AndHidesResult` | | 연타는 자동 테스트 없음 |

### F. 게임 종료
| ID | 전제 | 조작 | 기대 결과 | 관련 자동 테스트 | 결과 | 비고 |
|---|---|---|---|---|---|---|
| UI-15 | 둘 다 같은 턴에 탈락 | 없음 | `Draw - no survivors.`, 로그 마지막 줄 `Draw: no survivors.`, 두 카드 모두 `Eliminated` | `BattleLogFormatter::FormatTurn_ReportsDraw` (로그만) | | 둘 다 HP 1에서 서로 공격하면 나올 수 있음. 안 나오면 `확인 못 함` |
| UI-16 | 내가 탈락 | 없음 | `Bot 1 wins. You lose.`, 내 카드 `Eliminated`로 흐리게 표시, 로그 `Winner: Bot 1` | 없음 | | Gather·Block만 반복하면 쉽게 나옴 |
| UI-17 | Bot 1이 탈락 | 없음 | `You win!`, 단계 `Game over`, 행동 패널·Lock 줄·Next Turn 숨김, Play Again 보임, 모든 글자와 버튼이 화면 안에 들어옴 | `PlayingToTheEnd_ShowsGameOverPanel` | | 화면 넘침은 DevLog 07 문제 #3 |
| UI-18 | 종료 화면 | Play Again 클릭 (여러 번도 시도) → 설정 패널에서 `Start` | Play Again을 누르면 **설정 패널로 돌아감**. Start 후 새 게임 `Turn 1`, HP 3·Ki 0, 로그 비워짐, 선택 초기화. 이전 판 정보가 남지 않음 | `ShowSetup_AfterGame_ReturnsToSetupPanel` (설정 패널 복귀만) | | M4에서 동작 바뀜 (DEC-009) |

### G. 로그와 표시
| ID | 전제 | 조작 | 기대 결과 | 관련 자동 테스트 | 결과 | 비고 |
|---|---|---|---|---|---|---|
| UI-19 | 아무 턴 공개 | 로그 읽기 | `-- Turn N --`, `You: Energy Wave -> Bot 1`, `Energy Wave You -> Bot 1: Hit` 형식. 탈락은 `Eliminated: …`, 승자는 `Winner: …`. 어색한 문장 없음 | `FormatTurn_ListsActionsAndAttackResults`, `FormatTurn_ReportsEliminationAndWinner` | | |
| UI-20 | 15턴 이상 진행 | 로그 스크롤 | 최신 턴이 맨 위. 길어지면 스크롤바가 생기고 스크롤이 잘 됨 | 없음 | | |
| UI-21 | 아무 때나 | 카드 글자 확인 | 긴 행동 문구(`Action: Spirit Bomb -> Bot 1`)가 카드 밖으로 넘치지 않음 | 없음 | | DevLog 07 문제 #2 |

### H. 비정상 조작과 창
| ID | 전제 | 조작 | 기대 결과 | 관련 자동 테스트 | 결과 | 비고 |
|---|---|---|---|---|---|---|
| UI-22 | 선택 중 / 공개 중 / 종료 화면 | 키보드 Tab, Enter, Space | 꺼진 버튼이나 숨겨진 버튼이 키보드로 눌리지 않음. 눌려도 규칙에 없는 동작이 일어나지 않음 | 없음 | | |
| UI-23 | 게임 중 | 창 모드 전환, 창 크기 줄이기(예: 1280×720), 최소화 후 복귀, Alt+Tab | 배치가 깨지지 않고 게임 상태(턴, HP, Ki, 선택)가 그대로 | 없음 | | |
| UI-24 | 게임 중 | 행동 → 대상 → 다른 행동 → Lock을 아주 빠르게 | Lock된 행동이 화면에 마지막으로 보인 선택과 같음 | 없음 | | |

### I. 설정 패널 (M4, DEC-009)
| ID | 전제 | 조작 | 기대 결과 | 관련 자동 테스트 | 결과 | 비고 |
|---|---|---|---|---|---|---|
| UI-25 | 게임 실행 직후 | 없음 | 설정 패널만 보임: `New Game`, `Number of bots`, 버튼 `1`~`7`(7개), `Start`. 처음엔 `1`이 강조됨. 턴·카드·행동 버튼 등 게임 화면은 보이지 않음. 패널이 화면 가운데에 있고 글자가 잘리지 않음 | `NewView_ShowsSetupPanel_WithBotCountButtons` | | 처음 선택값은 씬의 `BattleScreen.botCount`(1) |
| UI-26 | 설정 패널 | `3` → `7` → `2` 순서로 클릭 | 마지막에 누른 버튼만 강조됨. 다른 버튼 강조는 사라짐 | `StartGame_RaisesStartRequestedWithSelectedBotCount` | | |
| UI-27 | 설정 패널 | 봇 수 `N`(1~7 중 아무거나) 선택 → `Start` | 설정 패널이 사라지고 게임 화면이 나옴. 카드가 `N+1`장(`You`, `Bot 1`~`Bot N`). 모든 봇 `Locked`, `? (hidden)` | `StartGame_RaisesStartRequestedWithSelectedBotCount`, `Bind_HidesSetupPanel`, `EightPlayers_ShowEightCards_AndSevenTargets` | | 1, 4, 7은 꼭 확인 |
| UI-28 | 게임 한 판 종료 | Play Again → (봇 수를 바꾸지 않고) `Start` | 직전에 고른 봇 수가 강조된 채로 설정 패널이 나오고, 같은 인원으로 새 게임이 시작됨 | 없음 | | |
| UI-29 | 설정 패널 | `Start`를 빠르게 여러 번 클릭 | 게임이 한 번만 시작됨. 카드 수가 정상이고 로그가 비어 있음 | 없음 | | |

### J. 다인전 화면 (M4, DEC-009)
| ID | 전제 | 조작 | 기대 결과 | 관련 자동 테스트 | 결과 | 비고 |
|---|---|---|---|---|---|---|
| UI-30 | 봇 7명 | 없음 | 카드 8장이 **한 줄**에 들어감. 각 카드의 이름·HP·Ki·상태·행동 글자가 잘리거나 겹치지 않음. 내 카드만 파란 테두리 | `EightPlayers_ShowEightCards_AndSevenTargets` (카드 수만) | | 배치는 자동 테스트로 볼 수 없음 |
| UI-31 | 봇 7명, 내 Ki 1 이상 | Energy Wave 클릭 | 대상 버튼 7개(`Bot 1`~`Bot 7`), 내 이름 없음. 버튼이 2줄 이내로 화면 안에 들어오고 Lock 줄이 가려지지 않음 | `EightPlayers_ShowEightCards_AndSevenTargets` (버튼 수만) | | |
| UI-32 | 봇 2명 이상, 봇 하나가 탈락한 뒤 (나는 생존, Ki 1 이상) | Energy Wave 클릭 | 탈락한 봇은 **대상 목록에 없음**. 그 봇의 카드는 `Eliminated`로 흐리게 표시되고 행동은 `-` | 없음 (로직은 `ActionValidator::Attack_OnEliminatedPlayer_IsRejected`) | | §26 #7 UI 확인. 2인전에서는 나올 수 없던 상황 |
| UI-33 | 봇 2명 이상, 공개 상태 | 카드·공개 패널·로그 비교 | 카드: 각자 이번 턴 행동. 공개 패널: 판정 결과만(적중·막힘·회피·상쇄·탈락·승자). 로그: 행동 줄과 판정 줄 모두. 셋이 서로 맞음 | `RevealPanel_ShowsOutcomesOnly`, `BattleLogFormatter::FormatOutcomes_ListsOnlyResolutionResults` | | |
| UI-34 | 봇 7명, 공격이 많은 턴 (판정 줄이 많을 때) | 공개 패널 확인 | 판정 줄이 많아도 공개 패널, Next Turn 버튼, 로그가 화면 안에 있음. 줄이 패널 밖으로 넘치지 않음 | 없음 | | 8인전에서 한 턴에 판정이 7줄 넘게 나올 수 있음 |
| UI-35 | 봇 7명, 여러 판 | 봇 행동 관찰 (공개 후 카드·로그) | 봇의 공격 대상이 나에게만 몰리지 않고 다른 봇들에게도 나뉨. 탈락한 플레이어를 노리는 행동이 없음. 로그에 `action failed`가 없음 | `BotAI::Attacks_TargetOnlyLivingOpponents_Uniformly`, `LocalBattleSession::SevenBots_EightPlayers_AllBotsLockAndHideActions` | | DEC-008: 대상은 살아 있는 상대 중 무작위 |

### K. 탈락 후 관전 (M4, DEC-009)
| ID | 전제 | 조작 | 기대 결과 | 관련 자동 테스트 | 결과 | 비고 |
|---|---|---|---|---|---|---|
| UI-36 | 봇 2명 이상, **내가 먼저 탈락**하고 봇 2명 이상 생존 | 없음 | 단계 `You are eliminated - spectating`. 내 카드 `Eliminated`(흐림). 행동 패널과 Lock 줄이 사라짐. 공개 패널에 `Next Turn`과 `Skip to End`가 함께 보임 | `EliminatedHuman_Spectates_AndSkipToEndFinishesTheGame` | | Gather·Block만 반복하면 탈락하기 쉬움 |
| UI-37 | UI-36 상태 | `Next Turn` 클릭 | 내 입력 없이 다음 턴이 바로 공개됨(봇끼리 진행). 턴 번호 +1, 로그에 새 턴 추가 | `LocalBattleSession::EliminatedHuman_BotsStillResolveTheTurn` | | |
| UI-38 | UI-36 상태 | `Skip to End` 클릭 | 곧바로 종료 화면(`Bot N wins. You lose.` 또는 `Draw - no survivors.`). 로그에 건너뛴 **모든 턴**이 `-- Turn N --`부터 빠짐없이 있음. 화면이 멈추지 않음 | `EliminatedHuman_Spectates_AndSkipToEndFinishesTheGame` | | 로그 턴 수 = 종료 턴 번호인지 확인 |
| UI-39 | 내가 살아 있는 동안 | 공개 상태 확인 | `Skip to End`가 보이지 않음 | `SkipButton_IsHidden_WhileHumanIsAlive` | | |
| UI-40 | UI-36 상태 | `Skip to End` 연타, 또는 `Next Turn`과 섞어서 빠르게 클릭 | 오류 없이 한 번만 종료 화면으로 감. 같은 턴이 로그에 두 번 나오지 않음 | 없음 | | |

## 결과 요약
| 통과 | 실패 | 확인 못 함 |
|---|---|---|
| | | |

실패한 케이스는 [README의 버그 리포트 양식](README.md#버그-리포트-양식)으로 기록한다.
