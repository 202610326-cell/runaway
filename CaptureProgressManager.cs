using UnityEngine;

/// <summary>
/// 여러 추격자의 CaptureProgress를 집계해 HUD와 PlayerMovement에 전달한다.
/// 가장 높은 진행도를 기준으로 처리한다.
///
/// [FIX] 이전에는 ApplyProgress() 안의 플레이어 처리 블록이 비어 있어서
///       구현 계획서의 "붙잡힘 시 이동 불능 처리"가 실제로는 동작하지 않았다.
///       이제 진행도에 따라 이동 속도를 낮추고, 100%에서는 완전히 고정한다.
/// </summary>
public class CaptureProgressManager : MonoBehaviour
{
    public static CaptureProgressManager Instance { get; private set; }

    // 현재 최대 붙잡힘 진행도 (0~1)
    public float CurrentProgress { get; private set; }

    // ─── 플레이어 이동 저하 설정 ─────────────────────────────────
    [Header("Player Slowdown")]
    [Tooltip("이 진행도 이상부터 이동 속도가 낮아지기 시작")]
    [SerializeField] private float slowThreshold = 0.2f;
    [Tooltip("100% 붙잡힘일 때의 이동 속도 배율 (1 = 정상)")]
    [SerializeField] private float slowFactorAtFullCapture = 0.5f;

    private HUDController  hud;
    private PlayerMovement playerMovement;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        hud            = FindFirstObjectByType<HUDController>();
        playerMovement = FindFirstObjectByType<PlayerMovement>();
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

    private void LateUpdate()
    {
        // 추격자가 보고하지 않은 동안에는 진행도가 자연스럽게 감소한다.
        CurrentProgress = Mathf.MoveTowards(CurrentProgress, 0f, 0.3f * Time.deltaTime);
        ApplyProgress();
    }

    private void ApplyProgress()
    {
        // HUD 업데이트
        hud?.UpdateCaptureWarning(CurrentProgress);

        if (playerMovement == null)
        {
            playerMovement = FindFirstObjectByType<PlayerMovement>();
            if (playerMovement == null) return;
        }

        // 붙잡힘이 진행될수록 이동 속도가 떨어진다 (도주 방해 연출)
        float t      = Mathf.InverseLerp(slowThreshold, 1f, CurrentProgress);
        float factor = Mathf.Lerp(1f, slowFactorAtFullCapture, t);
        playerMovement.SetCaptureFactor(factor);

        // 완전히 붙잡혔을 때 이동 불가
        playerMovement.SetCaptured(CurrentProgress >= 1f);
    }

    /// <summary>게임 재시작 시 초기화</summary>
    public void Reset()
    {
        CurrentProgress = 0f;

        if (playerMovement == null)
            playerMovement = FindFirstObjectByType<PlayerMovement>();

        playerMovement?.SetCaptureFactor(1f);
        playerMovement?.SetCaptured(false);

        hud?.UpdateCaptureWarning(0f);
    }
}
