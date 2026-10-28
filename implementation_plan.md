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

---

## 검증 계획

| 항목 | 방법 |
|------|------|
| 이동·카메라 | 장애물 사이 이동, 벽 접촉 시 카메라 확인 |
| 한정선 획득 | E키 입력 → UI 변화 확인 |
| AI 추격 | 시야 범위 안팎 전환 시 상태 변화 확인 |
| 생존 시스템 | 180초 이상 생존 시 5단계 진입 확인 |
| 게임 오버 | 붙잡힘 → GameOver UI 표시 → 재시작 |
