using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 인게임 HUD를 제어한다.
/// - 생존 시간 텍스트
/// - 현재 점수 텍스트
/// - 스태미나 슬라이더
/// - 붙잡힘 진행도 바 (CaptureProgress)
/// - 단계 변경 알림 (코루틴으로 잠깐 표시)
/// - 한정선 획득 아이콘
/// </summary>
public class HUDController : MonoBehaviour
{
    [Header("Survival Info")]
    [SerializeField] private TextMeshProUGUI survivalTimeText;
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI stageText;

    [Header("Stamina")]
    [SerializeField] private Slider staminaSlider;
    [SerializeField] private Image  staminaFill;
    [SerializeField] private Color  staminaFullColor    = new Color(0.2f, 0.8f, 0.3f);
    [SerializeField] private Color  staminaLowColor     = new Color(0.9f, 0.2f, 0.2f);

    [Header("Capture Warning")]
    [SerializeField] private GameObject captureWarningPanel;
    [SerializeField] private Slider     captureSlider;
    [SerializeField] private Image      screenVignette;    // 붙잡힐 때 화면 주변 붉게

    [Header("Stage Notification")]
    [SerializeField] private GameObject stageNotificationPanel;
    [SerializeField] private TextMeshProUGUI stageNotificationText;
    [SerializeField] private float           notificationDuration = 2.5f;

    [Header("Han JeongSeon Indicator")]
    [SerializeField] private GameObject hanJeongSeonIcon;

    // ─── 참조 ─────────────────────────────────────────────────────
    private PlayerMovement    playerMovement;
    private SurvivalManager   survivalManager;
    private ScoreManager      scoreManager;

    private void Start()
    {
        playerMovement  = FindObjectOfType<PlayerMovement>();
        survivalManager = SurvivalManager.Instance;
        scoreManager    = ScoreManager.Instance;

        // 이벤트 구독
        if (scoreManager != null)
            scoreManager.OnScoreChanged += UpdateScoreDisplay;

        if (survivalManager != null)
            survivalManager.OnStageChanged += ShowStageNotification;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnHanJeongSeonGet += ShowHanJeongSeonIcon;
        }

        // 초기 상태 설정
        if (captureWarningPanel != null) captureWarningPanel.SetActive(false);
        if (stageNotificationPanel != null) stageNotificationPanel.SetActive(false);
        if (hanJeongSeonIcon != null) hanJeongSeonIcon.SetActive(false);
        if (screenVignette  != null)
        {
            Color c = screenVignette.color;
            c.a = 0f;
            screenVignette.color = c;
        }

        UpdateScoreDisplay(0);
    }

    private void Update()
    {
        UpdateSurvivalTime();
        UpdateStamina();
    }

    // ─── 생존 시간 ───────────────────────────────────────────────
    private void UpdateSurvivalTime()
    {
        if (survivalTimeText == null || survivalManager == null) return;
        survivalTimeText.text = survivalManager.GetFormattedTime();
    }

    // ─── 점수 ─────────────────────────────────────────────────────
    private void UpdateScoreDisplay(int score)
    {
        if (scoreText != null) scoreText.text = $"{score:N0}점";
    }

    // ─── 스태미나 ────────────────────────────────────────────────
    private void UpdateStamina()
    {
        if (staminaSlider == null || playerMovement == null) return;
        float ratio = playerMovement.GetStaminaRatio();
        staminaSlider.value = ratio;

        if (staminaFill != null)
            staminaFill.color = Color.Lerp(staminaLowColor, staminaFullColor, ratio);
    }

    // ─── 붙잡힘 경고 ─────────────────────────────────────────────
    public void UpdateCaptureWarning(float progress)
    {
        bool active = progress > 0.01f;
        if (captureWarningPanel != null && captureWarningPanel.activeSelf != active)
            captureWarningPanel.SetActive(active);

        if (captureSlider != null) captureSlider.value = progress;

        // 화면 주변 Vignette
        if (screenVignette != null)
        {
            Color c = screenVignette.color;
            c.a = Mathf.Lerp(0f, 0.6f, progress);
            screenVignette.color = c;
        }
    }

    // ─── 단계 알림 ───────────────────────────────────────────────
    private void ShowStageNotification(int stageIdx, string stageName)
    {
        if (stageText != null) stageText.text = stageName;
        StopCoroutine(nameof(NotificationCoroutine));
        StartCoroutine(NotificationCoroutine(stageName));
    }

    private IEnumerator NotificationCoroutine(string name)
    {
        if (stageNotificationPanel == null) yield break;
        if (stageNotificationText  != null) stageNotificationText.text = $"⚠ {name} 시작!";

        stageNotificationPanel.SetActive(true);
        yield return new WaitForSeconds(notificationDuration);
        stageNotificationPanel.SetActive(false);
    }

    // ─── 한정선 획득 아이콘 ──────────────────────────────────────
    private void ShowHanJeongSeonIcon()
    {
        if (hanJeongSeonIcon != null) hanJeongSeonIcon.SetActive(true);
    }

    private void OnDestroy()
    {
        if (scoreManager    != null) scoreManager.OnScoreChanged    -= UpdateScoreDisplay;
        if (survivalManager != null) survivalManager.OnStageChanged -= ShowStageNotification;
    }
}
