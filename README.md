# 한정선: 추격 생존 (runaway)

Unity 6 LTS + URP 기반 3인칭 추격 생존 게임의 MVP 스크립트 모음입니다.

> ⚠️ 이 저장소는 **스크립트만** 담고 있습니다. 씬(.unity) · 프리팹 · 레이어 설정은
> [`walkthrough.md`](walkthrough.md)를 따라 직접 구성해야 합니다.

## 게임 흐름

```
MainMenu ──[게임 시작]──▶ SurvivalMap
                            │
      이동(WASD) · 달리기(Shift) · 점프(Space)
                            │
                    E키로 한정선 획득
                            │
              ┌─────────────┴─────────────┐
        추격자 스폰(5명)              생존 타이머 시작
              └─────────────┬─────────────┘
                            │
         30s / 60s / 120s / 180s마다 단계 상승
         (스폰 간격 ↓ · 추격 속도 ↑ · 보너스 점수)
                            │
        추격자가 붙잡음 → CaptureProgress 100% → GAME OVER
                            │
                  재시작 / 메인 메뉴
```

## 조작법

| 입력 | 동작 |
|------|------|
| `W A S D` | 이동 (카메라 기준) |
| `Shift` | 달리기 (스태미나 소모) |
| `Space` | 점프 |
| `E` | 한정선 획득 |
| `Esc` | 일시정지 / 재개 |
| 마우스 | 카메라 회전 |

## 스크립트 구성

| 경로 | 파일 | 역할 |
|------|------|------|
| `Player/` | `PlayerMovement` | 이동·달리기·점프·스태미나·붙잡힘 감속 |
| | `PlayerInteraction` | E키 상호작용, 한정선 획득 |
| | `HanJeongSeonItem` | 아이템 부유/자전, 획득 처리 |
| `Camera/` | `ThirdPersonCamera` | 오빗 카메라, 벽 관통 방지, 달리기 흔들림 |
| `AI/` | `ChaserAI` | Patrol→Chase→Search→Capture 상태 머신 |
| | `ChaserSpawner` | 추격자 생성·수량 관리 |
| `Game/` | `GameManager` | 게임 상태·일시정지·게임오버·재시작 |
| | `SurvivalManager` | 생존 타이머 + 5단계 난이도 |
| | `ScoreManager` | 점수·보너스·최고 기록(PlayerPrefs) |
| | `CaptureProgressManager` | 붙잡힘 진행도 집계 + HUD/플레이어 반영 |
| `UI/` | `HUDController` | 생존시간·점수·스태미나·경고·단계 알림 |
| | `GameOverUI` | 결과 화면, 재시작/메인 메뉴 |
| | `MainMenuUI` | 메인 메뉴, 게임 시작 진입점 |
| `Character/` | `CharacterVisual` | 절차적 캐릭터 생성 + 걷기/달리기 애니메이션 |
| | `CharacterPresets` | 플레이어 / 추격자 프리셋 |
| | `CharacterRig` | 캐릭터 정의 데이터(관절 치수·파츠·표정/보행 설정) |
| | `PrimitiveBuilder` | 프리미티브 생성 + 재질 캐시 |

## 캐릭터 비주얼

Capsule placeholder를 **프리미티브로 조립한 저폴리 캐릭터**로 교체한다.
Animator나 애니메이션 클립 없이 관절 회전을 런타임에 계산한다.

| | 플레이어 "한정선 수색자" | 추격자 |
|---|---|---|
| 파츠 | 22개 / 키 1.79 m | 29개 / 키 1.76 m |
| 실루엣 | 청록 재킷 + 배낭 + 골드 포인트 | 굽은 자세, 넓은 어깨, 긴 팔, 등 가시 |
| 색상 | `#2E6E8E` / `#6B4A2F` / `#FFD166` | `#201C24` / `#4A3B52` / `#E8E0D0` |
| 눈 | 흰자 + 동공, 깜빡임 | 붉은 발광(unlit), 무눈동자 |
| 특색 | 호흡, 걷기/달리기 스윙 | **플레이어를 고개로 노려봄**, 덜덜 떨리는 보행 |

사용법: 플레이어/추격자 프리팹에 `CharacterVisual`을 붙이고 `Preset`을 지정하면 끝.
`Preset = Custom`으로 바꾸면 인스펙터에서 파츠를 직접 편집할 수 있다.

## 빠른 시작

1. Unity 6 LTS로 `3D (URP)` 템플릿 프로젝트 생성
2. `AI Navigation`, `TextMeshPro` 패키지 설치
3. 이 저장소의 `.cs`를 `Assets/Scripts/` 아래 구조대로 배치 (`walkthrough.md` 참고)
4. `Wall` / `Player` / `HanJeongSeon` 레이어 생성
5. `MainMenu` · `SurvivalMap` 씬 구성 후 Build Settings에 인덱스 0 / 1로 등록
6. 플레이 → **MainMenu에서 시작** (시작 경로는 `MainMenuUI`가 담당)

## 문서

- [`implementation_plan.md`](implementation_plan.md) — 구현 계획 + **수정 이력(치명적 이슈)**
- [`walkthrough.md`](walkthrough.md) — 레이어/씬/프리팹 구성 + 테스트 체크리스트

## 라이선스

MIT — [LICENSE](LICENSE)
