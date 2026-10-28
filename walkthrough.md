# 한정선: 추격 생존 — Unity 설치 가이드

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

## 3. 스크립트 파일 복사 위치

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
    └── UI/
        ├── HUDController.cs
        └── GameOverUI.cs
```

---

## 4. 씬 구성 — SurvivalMap.unity

### 4-1. 빈 오브젝트 및 매니저

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

---

### 4-2. 플레이어 설정

1. Capsule 오브젝트 생성 → 이름: `Player`
2. Tag: `Player`
3. 컴포넌트 추가:
   - `CharacterController` (반지름 0.35, 높이 1.8)
   - `PlayerMovement.cs`
   - `PlayerInteraction.cs`
4. `interactLayer`: `HanJeongSeon` 레이어 설정

---

### 4-3. 카메라 설정

1. Main Camera 선택
2. `ThirdPersonCamera.cs` 추가
3. `target`: Player 오브젝트 연결
4. `collisionMask`: `Default`, `Wall` 레이어 포함

---

### 4-4. 추격자 Prefab 만들기

1. Capsule 생성 → 이름: `Chaser`
2. 컴포넌트 추가:
   - `NavMeshAgent` (반지름 0.35, 높이 1.8)
   - `ChaserAI.cs`
3. `sightMask`: 벽/장애물 레이어 포함
4. `Chaser` → Prefab 저장

---

### 4-5. NavMesh Baking

1. Window → AI → Navigation 열기
2. 맵의 바닥/지형 오브젝트 선택 → **Static** 체크 → **Navigation Static** 체크
3. **Bake** 탭 → **Bake** 클릭
4. 파란 NavMesh 영역이 나타나면 성공

---

### 4-6. 한정선 아이템 Prefab

1. 큐브 또는 임포트한 모델 오브젝트 생성 → 이름: `HanJeongSeon`
2. Layer: `HanJeongSeon` 레이어 추가 및 할당
3. `HanJeongSeonItem.cs` 추가
4. 편의점 앞 적절한 위치에 배치

---

## 5. HUD 캔버스 구성

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

## 6. 게임 오버 UI

Canvas 아래에 `GameOverPanel` (비활성 상태로 시작):
- `survivalTimeText`, `finalScoreText`, `highScoreText`, `newRecordText`
- `restartButton`, `mainMenuButton`
- `GameOverUI.cs` 추가 → 슬롯 연결

---

## 7. 씬 Build Settings 등록

File → Build Settings → Add Open Scenes로 아래 씬을 등록한다.

| 인덱스 | 씬 이름 |
|-------|--------|
| 0 | MainMenu |
| 1 | SurvivalMap |

---

## 8. 테스트 체크리스트

- [ ] W/A/S/D로 이동, Shift로 달리기 작동
- [ ] 카메라가 마우스로 회전, 벽에 가까워져도 관통하지 않음
- [ ] E키로 한정선 획득 → HUD 아이콘 표시
- [ ] 획득 후 추격자 5명 즉시 스폰
- [ ] 추격자가 NavMesh 위에서만 이동 (벽 통과 없음)
- [ ] 생존 30초 → "2단계" 알림 표시
- [ ] 붙잡힘 진행도가 100%가 되면 GameOver 패널 출력
- [ ] 재시작 버튼 → 씬 재시작 정상 작동
