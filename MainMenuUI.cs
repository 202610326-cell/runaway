using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 메인 메뉴 UI.
///
/// [NEW] 치명적 이슈 해결용으로 추가된 스크립트.
/// 이전에는 MainMenu 씬을 담당하는 스크립트가 저장소에 하나도 없어서
/// "게임 시작" 진입 경로(StartGame 호출 + SurvivalMap 씬 로드)가 존재하지 않았다.
///
/// MainMenu 씬의 "게임 시작" 버튼에 연결하면
///   GameManager.StartGame()  → 상태 Playing + 런타임 초기화
///   SceneManager.LoadScene(1) → SurvivalMap 진입
/// 흐름으로 시작된다.
/// </summary>
public class MainMenuUI : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private Button startButton;
    [SerializeField] private Button quitButton;

    [Header("Info")]
    [SerializeField] private TextMeshProUGUI highScoreText;
    [SerializeField] private GameObject   panel;

    [Header("Scene")]
    [Tooltip("Build Settings에서 SurvivalMap 씬의 인덱스 (기본 1)")]
    [SerializeField] private int gameplaySceneIndex = 1;

    private void Start()
    {
        if (startButton != null) startButton.onClick.AddListener(OnStart);
        if (quitButton  != null) quitButton.onClick.AddListener(OnQuit);

        // 메인 메뉴에서는 커서를 반드시 표시한다.
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;

        if (panel != null) panel.SetActive(true);

        UpdateHighScore();
    }

    private void UpdateHighScore()
    {
        if (highScoreText == null) return;
        int high = PlayerPrefs.GetInt(ScoreManager.HighScoreKey, 0);
        highScoreText.text = $"최고 기록: {high:N0}점";
    }

    private void OnStart()
    {
        // MainMenu 씬에 GameManager를 배치하지 않았어도 동작하도록 보장
        GameManager.EnsureInstance();

        GameManager.Instance.StartGame();
        SceneManager.LoadScene(gameplaySceneIndex);
    }

    private void OnQuit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
