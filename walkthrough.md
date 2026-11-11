# 한정선: 추격 생존 — Unity 설치 가이드

> 본 문서는 스크립트 수정본(v2)에 맞춰 갱신되었습니다.
> 이전 버전에는 **MainMenu 씬 구성**과 **레이어 분리** 절차가 빠져 있어
> 게임이 시작되지 않거나 카메라/추격 AI가 동작하지 않는 문제가 있었습니다.

---

## 1. Unity 프로젝트 생성

1. **Unity Hub** 열기 → **New project** 클릭
2. 템플릿: `3D (URP)` 선택 (Universal Render Pipeline)
3. Unity 버전: `6000.x LTS` 선택
4. 프로젝트 이름: `HanJeongSeon`
5. **Create project** 클릭

> [!IMPORTANT]
> URP 템플릿을 사용해야 로우폴리 렌더링에 적합하고 NavMesh와의 충돌이 없다.

---

## 2. 패키지 설치 (Package Manager)

Window → Package Manager에서 아래 패키지를 설치한다.

| 패키지 | 용도 |
|--------|------|
| **AI Navigation** (com.unity.ai.navigation) | NavMeshAgent, NavMesh Baking |
| **TextMeshPro** (com.unity.textmeshpro) | HUD 텍스트 렌더링 |

---

## 3. 레이어 설정 (중요)

Project Settings → Tags and Layers에서 아래 레이어를 만든다.

| 레이어 | 용도 | 포함 대상 | 비고 |
|--------|------|-----------|------|
| `Default` | 일반 | 지면, 소품 | 그대로 사용 |
| `Wall` | 벽/장애물 | 맵을 막는 벽 | **sightMask, collisionMask에 포함** |
| `Player` | 플레이어 | Player 오브젝트 | **sightMask, collisionMask에서 제외** |
| `HanJeongSeon` | 한정선 아이템 | 한정선 오브젝트 | interactLayer에 사용 |

> [!WARNING]
> 플레이어와 벽을 같은 레이어(Default)에 두면 `sightMask`/`collisionMask` 설정이
> 꼬여 카메라가 플레이어에게 붙거나 추격 AI가 시야를 못 보는 문제가 생긴다.
>
> 스크립트는 플레이어 콜라이더를 코드에서 자동 제외하도록 보강되었지만,
> 그래도 레이어를 분리해 두는 편이 안전하다.

---

## 4. 스크립트 파일 복사 위치

아티팩트에서 생성된 `.cs` 파일들을 아래 경로에 복사한다.

```
Assets/
└── Scripts/
    ├── Player/
    │   ├── PlayerMovement.cs
    │   ├── PlayerInteraction.cs
    │   └── HanJeongSeonItem.cs
    ├── Camera/
    │   └── ThirdPersonCamera.cs
    ├── AI/
    │   ├── ChaserAI.cs
    │   └── ChaserSpawner.cs
    ├── Game/
    │   ├── GameManager.cs
    │   ├── SurvivalManager.cs
    │   ├── ScoreManager.cs
    │   └── CaptureProgressManager.cs
    ├── UI/
    │   ├── HUDController.cs
    │   ├── GameOverUI.cs
    │   └── MainMenuUI.cs     ← [NEW] 게임 시작 진입점
    └── Character/             ← [NEW] 절차적 캐릭터 비주얼
        ├── CharacterRig.cs        (캐릭터 정의 데이터)
        ├── CharacterPresets.cs    (플레이어/추격자 프리셋)
        ├── CharacterVisual.cs     (생성 + 애니메이션 컴포넌트)
        └── PrimitiveBuilder.cs    (프리미티브/재질 헬퍼)
```

---

## 5. MainMenu 씬 구성 — `MainMenu.unity`

> [!IMPORTANT]
> 이전 버전에는 이 씬에 해당하는 스크립트가 없어 **"게임 시작" 버튼이 아무 동작도 하지 않았다.**
> `MainMenuUI.cs`가 그 진입점이다.

1. 새 씬 생성 → `MainMenu`으로 저장
2. 빈 오브젝트 `MainMenuUI` 생성 → `MainMenuUI.cs` 추가
   - `startButton` → 시작 버튼
   - `quitButton` → 종료 버튼 (선택)
   - `highScoreText` → 최고 기록 텍스트 (선택)
   - `panel` → 메뉴 패널 (선택)
   - `gameplaySceneIndex` → `1` (Build Settings의 SurvivalMap 인덱스)
3. Canvas (Screen Space - Overlay) 아래 버튼/텍스트 배치
   - 시작 버튼의 OnClick → 별도 연결 불필요 (코드로 연결됨)
4. `GameManager` 오브젝트를 추가해도 되고, 생략해도 된다.
   - 생략하면 `MainMenuUI`가 런타임에 자동으로 생성한다.
   - 추가할 경우 `DontDestroyOnLoad` 체크

> `GameManager`가 DontDestroyOnLoad이므로 MainMenu → SurvivalMap 전환 시에도 유지된다.

---

## 6. 씬 구성 — SurvivalMap.unity

### 6-1. 빈 오브젝트 및 매니저

1. **GameManager** (빈 오브젝트)
   - `GameManager.cs` 추가
   - `DontDestroyOnLoad` 체크

2. **SurvivalManager** (빈 오브젝트)
   - `SurvivalManager.cs` 추가

3. **ScoreManager** (빈 오브젝트)
   - `ScoreManager.cs` 추가

4. **CaptureProgressManager** (빈 오브젝트)
   - `CaptureProgressManager.cs` 추가

5. **ChaserSpawner** (빈 오브젝트)
   - `ChaserSpawner.cs` 추가
   - `chaserPrefab` 슬롯에 추격자 Prefab 연결
   - `spawnParent` 슬롯에 추격자를 묶을 빈 오브젝트 연결 (선택)

### 6-2. 플레이어 설정

1. **빈 오브젝트** 생성 → 이름: `Player` (Transform의 위치 = 발 위치)
   - Capsule 프리미티브를 쓰면 `CapsuleCollider`가 남아 `CharacterController`와 **이중 충돌**이 된다.
     → Capsule을 쓰거나, 씬에서 CapsuleCollider를 제거한다.
2. Tag: `Player`
3. Layer: `Player`
4. 컴포넌트 추가:
   - `CharacterController` (중심 y=0.9, 반지름 0.35, 높이 1.8)
   - `PlayerMovement.cs`
   - `PlayerInteraction.cs`
   - `CharacterVisual.cs` → `Preset` = **Player**
5. `interactLayer`: `HanJeongSeon` 레이어 설정

### 6-3. 카메라 설정

1. Main Camera 선택
2. `ThirdPersonCamera.cs` 추가
3. `target`: Player 오브젝트 연결
4. `collisionMask`: `Wall` 레이어 포함 (Player는 제외)

### 6-4. 추격자 Prefab 만들기

1. Capsule 생성 → 이름: `Chaser`
2. 컴포넌트 추가:
   - `NavMeshAgent` (반지름 0.35, 높이 1.8)
   - `ChaserAI.cs`
   - `CharacterVisual.cs` → `Preset` = **Chaser**
3. `sightMask`: `Wall` 레이어 포함 (Player는 제외)
4. `Chaser` → Prefab 저장

> [!WARNING]
> Capsule 프리팹에 남은 `CapsuleCollider`는 NavMeshAgent와 함께 움직이며 **플레이어를 밀어낸다**.
> 추격자가 붙잡는 연출은 `ChaserAI`의 `captureRange`로 처리하므로
> 물리 충돌이 필요 없다면 CapsuleCollider를 제거하는 편이 조작감이 좋다.

### 6-5. 캐릭터 비주얼 (절차적 생성)

`CharacterVisual`은 Capsule 같은 placeholder를 **프리미티브 조립 저폴리 캐릭터**로 바꾸고,
관절을 회전시켜 걷기/달리기 애니메이션을 런타임에 계산한다. Animator/애니메이션 클립이 필요 없다.

| 항목 | 플레이어 프리셋 | 추격자 프리셋 |
|------|----------------|---------------|
| 파츠 수 | 22개 | 29개 |
| 키 | 1.79 m | 1.76 m |
| 실루엣 | 청록 재킷 + 배낭, 일반 사람 비율 | 굽은 자세, 넓은 어깨, 긴 팔, 등 가시 |
| 색상 | 재킷 `#2E6E8E` / 배낭 `#6B4A2F` / 골드 포인트 `#FFD166` | 몸체 `#201C24` / 가시 `#4A3B52` / 뼈 `#E8E0D0` |
| 눈 | 흰자 + 검은 동공, **깜빡임 있음** | 붉게 **발광**(unlit), **무눈동자** |
| 특색 | 배낭에 한정선, 호흡, 걷기/달리기 스윙 | **플레이어를 고개로 계속 노려봄**, 덜덜 떨리는 긴 보행 |

동작 원리
- 관절(본) 계층: `Hips → Torso → Head/ArmL/ArmR`, `Hips → LegL/LegR`
- 파츠는 관절 아래에 붙고, 애니메이션은 **관절 회전**만 담당 → 발이 미끄러지지 않도록 보폭은 거리 기준으로 진행
- 캐릭터 전체 회전은 기존 로직(CharacterController / NavMeshAgent)을 그대로 따르고, 비주얼은 자식이라 회전하지 않는다

프리셋을 custom으로 바꾸면 인스펙터에서 파츠를 직접 편집할 수 있다.
`Preset = Custom` → `customRig`의 `shapes` 리스트에서 파츠 추가/삭제/색상 변경.

> [!NOTE]
> 추격자 20명 × 29파츠 = 580개 오브젝트다. URP **SRP Batcher**를 쓰면 같은 재질끼리 배칭되어
> 드로우콜이 크게 늘지 않지만, 저사양이면 `maxChasers`를 줄이거나 `CharacterVisual`의 파츠를 줄이는 것이 좋다.
> (재질은 색상 조합별로 캐시되어 공유된다)

### 6-6. NavMesh Baking

1. Window → AI → Navigation 열기
2. 맵의 바닥/지형 오브젝트 선택 → **Static** 체크 → **Navigation Static** 체크
3. **Bake** 탭 → **Bake** 클릭
4. 파란 NavMesh 영역이 나타나면 성공

### 6-7. 한정선 아이템 Prefab

1. 큐브 또는 임포트한 모델 오브젝트 생성 → 이름: `HanJeongSeon`
2. Layer: `HanJeongSeon` 레이어 추가 및 할당
3. `HanJeongSeonItem.cs` 추가
4. 편의점 앞 적절한 위치에 배치

---

## 7. HUD 캔버스 구성

Canvas (Screen Space - Overlay) 아래에 다음 UI 요소를 생성한다.

| UI 요소 | 이름 | 컴포넌트 |
|---------|------|---------|
| 생존 시간 텍스트 | `SurvivalTimeText` | TextMeshProUGUI |
| 점수 텍스트 | `ScoreText` | TextMeshProUGUI |
| 단계 텍스트 | `StageText` | TextMeshProUGUI |
| 스태미나 슬라이더 | `StaminaSlider` | Slider |
| 붙잡힘 패널 | `CaptureWarningPanel` | Panel + Slider |
| 화면 Vignette | `ScreenVignette` | Image (전체 화면, 알파 0) |
| 한정선 아이콘 | `HanJeongSeonIcon` | Image |
| 단계 알림 패널 | `StageNotificationPanel` | Panel + TextMeshProUGUI |

HUD 오브젝트에 `HUDController.cs`를 추가하고 각 슬롯을 연결한다.

---

## 8. 게임 오버 UI

Canvas 아래에 `GameOverPanel` (비활성 상태로 시작):
- `survivalTimeText`, `finalScoreText`, `highScoreText`, `newRecordText`
- `restartButton`, `mainMenuButton`
- `GameOverUI.cs` 추가 → 슬롯 연결

---

## 9. 씬 Build Settings 등록

File → Build Settings → Add Open Scenes로 아래 씬을 등록한다.

| 인덱스 | 씬 이름 |
|--------|---------|
| 0 | MainMenu |
| 1 | SurvivalMap |

> `MainMenuUI.gameplaySceneIndex` 값이 이 인덱스와 일치해야 한다.

---

## 10. 테스트 체크리스트

- [ ] MainMenu에서 "게임 시작" 클릭 → SurvivalMap으로 이동
- [ ] W/A/S/D로 이동, Shift로 달리기 작동
- [ ] **플레이어 비주얼**: 몸통/머리/배낭/눈이 정상 표시, 발이 바닥에 닿음, 걸을 때 다리·팔 스윙
- [ ] **플레이어 눈이 자연스럽게 깜빡임**
- [ ] **추격자 비주얼**: 등 가시·긴 팔·붉은 눈이 보이며, 추격하면서 **고개가 나를 향함**
- [ ] 추격자를 여러 마리 소환해도 다리가 바닥에 닿고 캐릭터끼리 겹치지 않음
- [ ] 카메라가 마우스로 회전, **플레이어 몸에 붙지 않고** 벽에 가까워져도 관통하지 않음
- [ ] E키로 한정선 획득 → HUD 아이콘 표시
- [ ] 획득 후 추격자 5명 즉시 스폰
- [ ] **추격자가 플레이어를 시야 범위 안에서 발견하고 추격하는지** (sightMask 설정 확인)
- [ ] 추격자가 NavMesh 위에서만 이동 (벽 통과 없음)
- [ ] 생존 30초 → "2단계" 알림 표시
- [ ] 붙잡힘 진행도가 100%가 되면 GameOver 패널 출력
- [ ] 재시작 버튼 → **다시 플레이 가능** (이전에는 상태가 GameOver로 남아 정지됨)
- [ ] 메인 메뉴 버튼 → MainMenu 복귀, 커서 표시됨

---

## 11. 남은 알려진 이슈

아래 항목은 이번 수정 범위 밖이다. 필요하면 별도로 정리한다.

| 항목 | 위치 | 내용 |
|------|------|------|
| 힌트 UI 미연결 | `HanJeongSeonItem.SetHintVisible()` | "E - 획득" 안내 UI를 표시하는 호출자가 없다 |
| 파티클 잔류 | `HanJeongSeonItem.Collect()` | 파티클을 월드에 남긴 뒤 정리하지 않아 오브젝트가 누적된다 |
| 카메라 거리 스냅 | `ThirdPersonCamera` | 벽 모서리에서 카메라 거리가 튄다 (보간은 적용했으나 근본 개선 필요) |
| 발 IK 없음 | `CharacterVisual` | 절차적 보행이라 발이 지형 높이를 정확히 밟지 않는다 (평지 기준) |
| 인풋 시스템 | 전체 | Unity 6 신규 Input System 프로젝트에서는 `Input.*` 코드가 동작하지 않는다 (레거시 사용 프로젝트 기준) |
