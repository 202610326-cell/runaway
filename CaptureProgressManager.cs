using UnityEngine;

/// <summary>
/// 여러 추격자의 CaptureProgress를 집계해 HUD와 PlayerMovement에 전달한다.
/// 가장 높은 진행도를 기준으로 처리한다.
/// </summary>
public class CaptureProgressManager : MonoBehaviour
{
    public static CaptureProgressManager Instance { get; private set; }

    // 현재 최대 붙잡힘 진행도 (0~1)
    public float CurrentProgress { get; private set; }

    private HUDController  hud;
    private PlayerMovement playerMovement;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        hud           = FindObjectOfType<HUDController>();
        playerMovement = FindObjectOfType<PlayerMovement>();
    }

    /// <summary>
    /// 개별 추격자가 자신의 CaptureProgress를 보고한다.
    /// 이 클래스는 최대값을 유지하고 HUD와 플레이어 상태를 갱신한다.
    /// </summary>
    public void UpdateProgress(float progress)
    {
        CurrentProgress = Mathf.Max(CurrentProgress, progress);
        ApplyProgress();
    }

    /// <summary>매 프레임 진행도를 자연스럽게 감소시킨다 (추격자가 모두 멀어진 경우)</summary>
    private void Update()
    {
        if (CurrentProgress > 0f)
        {
            // 이 프레임에 아무 추격자도 UpdateProgress를 호출하지 않으면 자연 감소
            // (Update 순서 문제 회피를 위해 LateUpdate에서 처리)
        }
    }

    private void LateUpdate()
    {
        // 추격자가 이 프레임에 보고하지 않았으면 감소
        // (ChaserAI.UpdateCapture에서 매 프레임 호출하므로 호출이 없으면 0으로 수렴)
        CurrentProgress = Mathf.MoveTowards(CurrentProgress, 0f, 0.3f * Time.deltaTime);
        ApplyProgress();
    }

    private void ApplyProgress()
    {
        // HUD 업데이트
        hud?.UpdateCaptureWarning(CurrentProgress);

        // 플레이어 이동 제한: 진행도가 최대에 달하면 게임오버는 ChaserAI에서 처리
        // 여기서는 시각적 피드백만 담당
        if (playerMovement != null)
        {
            // 0.8 이상이면 이동 속도 절반으로 (긴장감 연출)
            // PlayerMovement에서 직접 isCaptured를 건드리지 않고 속도만 조절하려면
            // PlayerMovement에 SlowFactor 프로퍼티를 추가할 수 있다.
            // MVP에서는 생략하고 게임오버 판정만 ChaserAI에 맡긴다.
        }
    }

    /// <summary>게임 재시작 시 초기화</summary>
    public void Reset()
    {
        CurrentProgress = 0f;
        hud?.UpdateCaptureWarning(0f);
    }
}
