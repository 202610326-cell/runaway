# 한정선: 추격 생존 — MVP 구현 계획

## 개요

기획서 v1.0에 따라 Unity 6 LTS + C# + URP 기반의 MVP를 구현한다.  
플레이어 이동 → 한정선 획득 → 추격 AI → 생존 타이머 → 게임 오버의 핵심 루프를 완성한다.

---

## 구현할 스크립트 목록

### Player/
#### [NEW] PlayerMovement.cs
- CharacterController 기반 이동, 달리기(Shift), 점프(Space)
- 스태미나 시스템 (달리기 시 감소, 정지 시 회복)
- 붙잡힘 상태(CaptureProgress) 연동으로 이동 불능 처리

#### [NEW] PlayerInteraction.cs
- E키로 한정선 획득
- 반경 내 HanJeongSeon 오브젝트 감지
- 획득 시 GameManager에 이벤트 발송

### Camera/
#### [NEW] ThirdPersonCamera.cs
- 마우스 입력으로 카메라 Orbit 회전
- 카메라-플레이어 사이 장애물 감지 → 카메라 위치 조정 (벽 통과 방지)
- 달릴 때 약한 카메라 흔들림(Bob)

### AI/
#### [NEW] ChaserAI.cs
- 상태 머신: Patrol → Chase → Search → Capture
- NavMeshAgent 경로 탐색
- 시야각(110°) + 감지 거리(12m) 기반 플레이어 발견
- 붙잡기 거리(1.3m) 판정 → CaptureProgress 증가
- 여러 추격자 목표 위치 분산 처리

#### [NEW] ChaserSpawner.cs
- SurvivalManager 난이도 단계에 따라 추격자 생성
- SpawnInterval, 최대 인원(20명) 관리
- 스폰 위치: 플레이어와 최소 거리 유지

### Game/
#### [NEW] GameManager.cs
- 게임 상태 관리 (MainMenu / Playing / Paused / GameOver)
- Esc 일시정지
- 게임 시작/재시작/종료

#### [NEW] SurvivalManager.cs
- 생존 타이머
- 5단계 난이도 시스템 (0~30s / 30~60s / 60~120s / 120~180s / 180s+)
- ChaserSpeedMultiplier, SpawnInterval 조절

#### [NEW] ScoreManager.cs
- 생존 1초당 10점
- 한정선 획득 보너스
- 단계 달성 보너스
- PlayerPrefs 기반 최고 기록 저장

### UI/
#### [NEW] HUDController.cs
- 생존 시간 표시
- 현재 점수 표시
- 스태미나 바
- 붙잡힘 경고 UI (CaptureProgress)
- 단계 변경 알림

#### [NEW] GameOverUI.cs
- 최종 점수 / 최고 기록 표시
- 재시작 / 메인 메뉴 버튼

#### [NEW] MainMenuUI.cs
- 메인 메뉴 화면 (버튼 → 게임 시작 / 종료)
- 최고 기록 표시
- `GameManager.StartGame()` 호출로 플레이 진입

---

## 검증 계획

| 항목 | 방법 |
|------|------|
| 진입 | MainMenu → 게임 시작 → SurvivalMap 진입 확인 |
| 이동·카메라 | 장애물 사이 이동, 벽 접촉 시 카메라 확인, 카메라가 플레이어에 붙지 않는지 확인 |
| 한정선 획득 | E키 입력 → UI 변화 확인 |
| AI 추격 | 시야 범위 안팎 전환 시 상태 변화 확인, 추격자가 실제로 플레이어를 따라오는지 확인 |
| 생존 시스템 | 180초 이상 생존 시 5단계 진입 확인 |
| 게임 오버 | 붙잡힘 → GameOver UI 표시 → 재시작 후 다시 플레이 가능 확인 |

---

## 수정 이력 (v2) — 치명적 이슈 반영

리뷰에서 확인된 치명적 이슈 4건과, 같은 코드 경로에 포함된 주요 이슈를 함께 수정했다.

### 치명적 이슈

| # | 증상 | 원인 | 수정 |
|---|------|------|------|
| C1 | **게임을 시작할 수 없음** (시작 경로 부재) | MainMenu 담당 스크립트가 없고 `LoadScene(1)` 호출이 어디에도 없음 | `MainMenuUI.cs` 추가. `GameManager.StartGame()` → `SceneManager.LoadScene(gameplaySceneIndex)` 흐름 확립. `GameManager.EnsureInstance()`로 GameManager 미배치 시 자동 생성 |
| C2 | **재시작하면 완전 정지** | GameManager가 DontDestroyOnLoad라 `CurrentState = GameOver`가 재시작 후에도 잔존. 모든 `IsPlaying` 검사에 걸림 | `RestartGame()`에서 `SetState(Playing)`. `SceneManager.sceneLoaded` 핸들러 추가 → 씬 로드 시 런타임 상태 초기화 + 게임플레이 씬 진입 시 Playing 복구 |
| C3 | **추격 AI가 플레이어를 전혀 발견 못함** | `Physics.Raycast`가 어디에 맞든 true를 반환 → `sightMask`에 플레이어 레이어가 있으면 자기 대상에 막혀 항상 시야 차단 | `RaycastNonAlloc` + 플레이어 콜라이더 제외 + 가장 가까운 실제 장애물 거리로 판정. 눈 높이(eyeHeight/playerEyeHeight) 도입으로 Raycast 길이 1m 부족 문제도 해결 |
| C4 | **카메라가 플레이어 몸에 붙음** | `SphereCast` 결과가 true이면 즉시 당김 → `collisionMask`에 플레이어 레이어가 있으면 매 프레임 걸림 | `SphereCastNonAlloc` + 플레이어 콜라이더 제외. 벽 방향 즉시 당김 / 복귀 시 보간 |

### 주요 이슈 (같은 흐름에 포함되어 함께 수정)

| # | 증상 | 수정 |
|---|------|------|
| M1 | 단계 알림 코루틴이 중단되지 않음 | `StopCoroutine(nameof(...))` → `Coroutine` 참조 직접 보관 |
| M2 | 단계 상승 후 스폰된 추격자가 항상 1.0배 속도 | `ChaserSpawner`가 `currentSpeedMultiplier`를 보관하고 스폰 시 적용 |
| M3 | "무피격 보너스"가 취소되지 않음 | `ChaserAI`가 `Capture` 진입 시 `ScoreManager.RegisterHit()` 호출 |
| M4 | 붙잡힘 시 이동 저하/정지가 미구현 | `CaptureProgressManager.ApplyProgress()`가 `PlayerMovement.SetCaptureFactor()` / `SetCaptured()` 호출 |
| M5 | 게임 오버 슬로모션이 0.8초 → 2.67초로 늘어남 | `Invoke`(scaled time) → `WaitForSecondsRealtime` 코루틴 |
| M6 | "신기록!"이 항상 표시됨 | 최고 기록 저장 **전** 값으로 비교하도록 `GameManager.LastGameWasNewRecord` 추가 |
| M7 | 추격 목표가 매 프레임 재계산되어 지터 | 분산 갱신 주기(`spreadRefreshInterval`) 도입 |
| M8 | 카메라 초기 pitch가 잘못 클램프 | `Mathf.DeltaAngle` 사용 |
| M9 | HUD가 `GameManager.OnHanJeongSeonGet` 구독을 해제하지 않음 | `OnDestroy`에서 해제 추가 |
| M10 | 정지 상태에서 스태미나 회복 지연이 무시됨 | 입력 0 분기에서 `regenTimer`를 초기화하지 않도록 수정 |
| M11 | `FindObjectOfType` deprecated 경고 | `FindFirstObjectByType` 사용 |
| M12 | 아이템 획득 시 `GetComponent` 실패 가능 | `GetComponentInParent` 사용 |

### 여전히 미해결 (범위 외)

- `HanJeongSeonItem.SetHintVisible()` 호출자 부재 → "E - 획득" 힌트 UI가 표시되지 않음
- `HanJeongSeonItem.Collect()`가 파티클을 월드에 남기고 정리하지 않음
- Unity 6 신규 Input System 프로젝트에서의 `Input.*` 호환성
- `.cs.metadata.json` 생성 산출물 및 `browser/scratchpad_*.md` 잔여 파일 정리
