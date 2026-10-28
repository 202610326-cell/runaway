using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 게임 오버 화면 UI.
/// - 최종 생존 시간 / 점수 / 최고 기록 표시
/// - 재시작 / 메인 메뉴 버튼
/// </summary>
public class GameOverUI : MonoBehaviour
{
    [Header("Result Texts")]
    [SerializeField] private TextMeshProUGUI survivalTimeText;
    [SerializeField] private TextMeshProUGUI finalScoreText;
    [SerializeField] private TextMeshProUGUI highScoreText;
    [SerializeField] private TextMeshProUGUI newRecordText;   // "신기록!" 텍스트

    [Header("Buttons")]
    [SerializeField] private Button restartButton;
    [SerializeField] private Button mainMenuButton;

    [Header("Panel")]
    [SerializeField] private GameObject panel;
    [SerializeField] private Animator   panelAnimator;        // 슬라이드인 애니메이션

    private void Start()
    {
        // 패널 비활성
        if (panel != null) panel.SetActive(false);

        // 버튼 이벤트 연결
        if (restartButton  != null) restartButton.onClick.AddListener(OnRestart);
        if (mainMenuButton != null) mainMenuButton.onClick.AddListener(OnMainMenu);

        // GameManager 게임오버 이벤트 구독
        if (GameManager.Instance != null)
            GameManager.Instance.OnGameOver += Show;
    }

    // ─── 표시 ─────────────────────────────────────────────────────
    private void Show(float survivalTime, int score)
    {
        if (panel != null) panel.SetActive(true);
        if (panelAnimator != null) panelAnimator.SetTrigger("Show");

        // 생존 시간
        int minutes = Mathf.FloorToInt(survivalTime / 60f);
        int seconds = Mathf.FloorToInt(survivalTime % 60f);
        if (survivalTimeText != null)
            survivalTimeText.text = $"{minutes:00}:{seconds:00}";

        // 현재 점수
        if (finalScoreText != null)
            finalScoreText.text = $"{score:N0}점";

        // 최고 기록
        int highScore = ScoreManager.Instance != null ? ScoreManager.Instance.HighScore : 0;
        if (highScoreText != null)
            highScoreText.text = $"최고 기록: {highScore:N0}점";

        // 신기록 여부
        bool isNewRecord = score >= highScore && score > 0;
        if (newRecordText != null) newRecordText.gameObject.SetActive(isNewRecord);
    }

    // ─── 버튼 핸들러 ─────────────────────────────────────────────
    private void OnRestart()
    {
        GameManager.Instance?.RestartGame();
    }

    private void OnMainMenu()
    {
        GameManager.Instance?.GoToMainMenu();
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnGameOver -= Show;
    }
}
