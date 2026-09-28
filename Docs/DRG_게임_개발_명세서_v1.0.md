# DRG 게임 개발 명세서 v1.0

## 1. 개발 기본 정보
- Engine: Unity
- Language: C#
- 초기 목표: Human 1 + AI Bot 프로토타입
- 최대 플레이어: 8
- 온라인 멀티플레이: 초기 범위 제외

## 2. 개발 원칙
1. 전체 프로젝트를 한 번에 구현하지 않는다.
2. 요청된 STEP만 구현한다.
3. 기존 구조를 존중하고 불필요한 리팩토링을 하지 않는다.
4. UI와 게임 로직을 분리한다.
5. 행동 선택과 행동 실행을 분리한다.
6. Human과 Bot은 동일한 PlayerAction을 사용한다.
7. BattleResolver는 UI/Animation에 의존하지 않는다.
8. 가능하면 게임 로직은 순수 C# 클래스로 작성한다.
9. 구현 전에 변경 파일과 책임을 설명한다.
10. 구현 후 테스트 방법을 제시한다.

## 3. GameState
```csharp
public enum GameState
{
    Lobby,
    Room,
    Game,
    Result
}
```

## 4. PlayerState
```csharp
public enum PlayerState
{
    Alive,
    Eliminated
}
```

## 5. ActionType
```csharp
public enum ActionType
{
    None,
    Gather,
    EnergyWave,
    Block,
    Teleport,
    SpiritBomb
}
```

## 6. PlayerAction
```csharp
public class PlayerAction
{
    public int PlayerId;
    public ActionType ActionType;
    public int TargetPlayerId;
}
```
Target이 필요 없는 행동은 -1. 공격은 살아있는 상대를 대상으로 하며 자기 자신은 대상이 될 수 없다. Lock 후 해당 턴 동안 변경할 수 없다.

## 7. Player
```csharp
public class Player
{
    public int PlayerId;
    public string Nickname;
    public int HP;
    public int MaxHP;
    public int Ki;
    public int MaxKi;
    public PlayerState State;
    public PlayerAction CurrentAction;
}
```
기본값: HP 3/MaxHP 3, Ki 0/MaxKi 3, State Alive.

## 8. GameSettings
```csharp
public class GameSettings
{
    public int MaxPlayers = 8;
    public int StartingHP = 3;
    public int MaxHP = 3;
    public int StartingKi = 0;
    public int MaxKi = 3;
}
```
숫자를 여러 클래스에 하드코딩하지 않는다.

## 9. ActionValidator
검사:
- 플레이어 생존 여부
- ActionType 유효성
- Ki 비용 지불 가능 여부
- Ki 3에서 Gather 금지
- 공격 대상 존재
- 공격 대상 생존
- 자기 자신 대상 금지
- 현재 GameState에서 행동 가능 여부

UI에서 버튼을 막아도 게임 로직에서 다시 검증한다.

## 10. TurnManager
```csharp
public enum TurnState
{
    TurnStart,
    ActionSelection,
    ActionResolution,
    TurnEnd
}
```
책임:
- 턴 시작
- 행동 선택 시작
- Lock
- 전원 Lock 확인
- 해결 시작
- 턴 종료
- 다음 턴 시작

탈락 플레이어는 자동으로 Lock된 것으로 취급한다.

## 11. BattleResolver
```csharp
public class BattleResolver
{
    public BattleResult ResolveTurn(List<Player> players)
    {
        // ...
    }
}
```
권장 절차:
1. Lock된 행동 수집
2. 최종 유효성 검증
3. Ki 비용 적용
4. Spirit Bomb 판정
5. Teleport 판정
6. Energy Wave 상호 취소 판정
7. Block 판정
8. 데미지 계산
9. HP 적용
10. HP 0 이하 Eliminate
11. 승리 조건 검사

처리 순서는 구현상의 순서일 뿐, 게임 규칙상 행동은 동시적으로 확정된 것으로 취급한다.

## 12. 동시성 규칙
- Lock된 행동은 해결 도중 사망해도 실행한다.
- 진정한 동시성을 위해 대상은 행동 선택 완료 시점의 상태를 기준으로 결정하도록 한다.
- 여러 공격은 누적될 수 있다.
- Teleport는 자신을 대상으로 한 모든 공격을 회피한다.
- Block은 자신을 대상으로 한 모든 Energy Wave를 방어한다.
- Spirit Bomb은 Block을 무시한다.
- Mutual Cancel은 정확히 A→B와 B→A일 때만 발생한다.
- A→C와 B→C는 상쇄되지 않는다.

## 13. BattleResult
```csharp
public class BattleResult
{
    public List<DamageResult> DamageResults;
    public List<KiChangeResult> KiChanges;
    public List<ActionResult> ActionResults;
}

public class DamageResult
{
    public int AttackerId;
    public int TargetId;
    public int Damage;
}

public enum ActionResultType
{
    Hit,
    Blocked,
    Dodged,
    Cancelled,
    Failed
}
```

## 14. Ki 처리
| 행동 | Ki 변화 |
|---|---:|
| Gather | +1 |
| EnergyWave | -1 |
| Block | 0 |
| Teleport | -1 |
| SpiritBomb | -3 |

## 15. UI 구조
```text
GameUI
├─ TurnUI
├─ PlayerInfoUI
├─ ActionUI
├─ TargetSelectUI
├─ ActionStatusUI
├─ BattleLogUI
└─ ResultUI
```

ActionUI 버튼:
- Gather
- Energy Wave
- Block
- Teleport
- Spirit Bomb

Energy Wave/Spirit Bomb:
```text
Button → Target Select → Target 선택 → PlayerAction 생성 → 선택 행동 표시 → Turn End
```

## 16. UI 표시
자신: Nickname, HP, Ki, 선택 행동, Lock 상태.
상대: Nickname, HP, Ki, 생존 상태.
상대 행동은 Reveal 전까지 숨긴다.

## 17. Battle Presentation
```text
BattleResolver
→ BattleResult
→ BattlePresentation
→ Animation / VFX / Sound
→ UI Update
```
BattleResolver는 Animation을 직접 실행하지 않는다.

## 18. Scene
```text
Scenes
├─ LobbyScene.unity
├─ GameScene.unity
└─ ResultScene.unity
```

## 19. 폴더 구조
```text
Assets
└─ _Project
   ├─ Scripts
   │  ├─ Core
   │  ├─ Player
   │  ├─ Battle
   │  ├─ AI
   │  ├─ Lobby
   │  └─ UI
   ├─ Scenes
   ├─ Prefabs
   ├─ ScriptableObjects
   ├─ Sprites
   ├─ Animations
   ├─ Audio
   └─ Resources
```

## 20. BotPlayer / BotAI
```text
BotPlayer → BotAI → PlayerAction → TurnManager → BattleResolver
```
초기 규칙:
- Ki 0 → Gather
- Ki 1~2 → Energy Wave/Gather
- Ki 3 → Spirit Bomb/Energy Wave
- HP 낮음 → Block/Teleport 확률 증가

향후 난이도별 의사결정으로 확장하며 테스트를 위해 랜덤 요소는 Seed 지정이 가능하도록 한다.

## 21. Lobby / Room
LobbyManager: 게임 시작, 방 흐름, 프로토타입 AI 추가.
RoomManager: 참가자, 최대 8명, Ready/Unready, 캐릭터 선택, 방장, 시작 조건.
프로토타입은 로컬 상태로 구현한다.

## 22. Result
ResultScene에서 승자, 결과, 생존 결과 등을 표시하고 로비로 이동한다. 무승부 규칙 확정 후 Draw도 구현한다.

## 23. Save
```csharp
public class PlayerData
{
    public int PlayerId;
    public string Nickname;
    public int TotalGames;
    public int Wins;
    public int Losses;
}
```
프로토타입은 로컬 JSON 저장.

## 24. 개발 순서
### STEP 1
GameState, PlayerState, ActionType, PlayerAction, Player, GameSettings

### STEP 2
TurnManager, TurnState, Lock, 전원 Lock 확인, 턴 전환

### STEP 3
BattleResolver, 행동 비용, 공격/방어/순간이동/원기옥, 상호 취소, 다중 공격, 동시 사망, 승리 판정

### STEP 4
UI 이전 콘솔 또는 Unit Test로 전투 로직 검증

### STEP 5
Human Player / Bot Player 연결

### STEP 6
HP/Ki/행동 버튼/대상 선택/Lock/Reveal/Battle Log UI

### STEP 7
BotAI 연결

### STEP 8
Lobby

### STEP 9
Result

### STEP 10
Human 1 + AI 최대 7명 전체 게임

### STEP 11 이후
캐릭터, 이모트, 기록 저장, 추가 콘텐츠, 온라인 멀티플레이

## 25. 마일스톤
- M1: BattleResolver 단독 검증
- M2: 2인 UI
- M3: Human + Bot 전체 게임
- M4: Human + 7 Bot 전체 게임
- M5: Lobby → Game → Result → Lobby

## 26. 테스트 체크리스트
- [ ] 모든 5개 행동
- [ ] 모든 행동 상호작용
- [ ] 다중 공격
- [ ] 동시 사망
- [ ] 사망해도 Lock 행동 실행
- [ ] 탈락 플레이어 자동 Lock
- [ ] 탈락 플레이어 대상 지정 방지
- [ ] 자기 자신 대상 방지
- [ ] Ki 부족 방지
- [ ] Max Ki에서 Gather 방지
- [ ] Lock 후 변경 방지
- [ ] Reveal 전 상대 행동 숨김
- [ ] Teleport의 전체 공격 회피
- [ ] Block의 전체 Energy Wave 방어
- [ ] Spirit Bomb의 Block 무시
- [ ] 정확한 Mutual Cancel
- [ ] 제3자 대상 공격은 상쇄되지 않음

## 27. 자동 시뮬레이션
BattleResolver를 UI와 독립적으로 실행해 1,000~10,000게임을 자동 시뮬레이션할 수 있도록 한다.

측정 후보:
- 평균 턴 수
- 행동별 사용 횟수/성공률
- Block/Teleport/Spirit Bomb 사용률
- 생존률
- 동시 사망 비율

## 28. Claude 개발 지침
- 전체 프로젝트를 한 번에 구현하지 않는다.
- 요청된 STEP만 구현한다.
- 기존 구조를 존중한다.
- 불필요한 리팩토링을 하지 않는다.
- UI와 게임 로직을 분리한다.
- Human/Bot은 동일한 PlayerAction을 사용한다.
- BattleResolver는 UI/Animation에 의존하지 않는다.
- 이 명세서를 게임 규칙의 기준으로 삼는다.
- 구현 전 변경 파일/책임/관계를 설명한다.
- 코드 후 테스트 방법을 제시한다.

### 최초 Claude 요청
```text
위 DRG 개발 명세서를 기준으로 STEP 1만 구현해줘.

아직 Unity MonoBehaviour나 UI는 만들지 마.

다음 요소만 먼저 구현해줘.
- GameState
- PlayerState
- ActionType
- PlayerAction
- Player
- GameSettings

코드를 작성하기 전에
1. 생성할 파일 구조
2. 각 클래스의 책임
3. 클래스 간 관계
4. 구현 방법
을 먼저 설명해줘.

불필요한 기능이나 리팩토링은 추가하지 마.
```
