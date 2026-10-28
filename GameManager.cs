using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 게임 전체 상태(MainMenu / Playing / Paused / GameOver)를 관리한다.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    // ─── 게임 상태 ───────────────────────────────────────────────
    public enum GameState { MainMenu, Playing, Paused, GameOver }
    public GameState CurrentState { get; private set; } = GameState.MainMenu;

    // 간편 접근 속성
    public bool IsPlaying  => CurrentState == GameState.Playing;
    public bool IsPaused   => CurrentState == GameState.Paused;
    public bool IsGameOver => CurrentState == GameState.GameOver;

    // ─── 이벤트 ──────────────────────────────────────────────────
    public event System.Action           OnGameStart;
    public event System.Action           OnGamePaused;
    public event System.Action           OnGameResumed;
    public event System.Action<float,int> OnGameOver;   // (survivalTime, score)
    public event System.Action           OnHanJeongSeonGet;

    // ─────────────────────────────────────────────────────────────
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        // Esc 일시정지 / 재개
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (CurrentState == GameState.Playing)  PauseGame();
            else if (CurrentState == GameState.Paused) ResumeGame();
        }
    }

    // ─── 게임 시작 ───────────────────────────────────────────────
    public void StartGame()
    {
        SetState(GameState.Playing);
        Time.timeScale = 1f;
        OnGameStart?.Invoke();
        Debug.Log("[GameManager] 게임 시작");
    }

    // ─── 일시정지 ─────────────────────────────────────────────────
    public void PauseGame()
    {
        if (CurrentState != GameState.Playing) return;
        SetState(GameState.Paused);
        Time.timeScale = 0f;

        // 커서 표시
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;

        OnGamePaused?.Invoke();
        Debug.Log("[GameManager] 일시정지");
    }

    // ─── 재개 ─────────────────────────────────────────────────────
    public void ResumeGame()
    {
        if (CurrentState != GameState.Paused) return;
        SetState(GameState.Playing);
        Time.timeScale = 1f;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible   = false;

        OnGameResumed?.Invoke();
        Debug.Log("[GameManager] 재개");
    }

    // ─── 게임 오버 ───────────────────────────────────────────────
    public void TriggerGameOver()
    {
        if (CurrentState == GameState.GameOver) return;
        SetState(GameState.GameOver);
        Time.timeScale = 0.3f;   // 약간 슬로모션 후 정지 (연출)

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;

        float survivalTime = SurvivalManager.Instance != null ? SurvivalManager.Instance.SurvivalTime : 0f;
        int   score        = ScoreManager.Instance    != null ? ScoreManager.Instance.CurrentScore    : 0;

        ScoreManager.Instance?.SaveHighScore(score);
        OnGameOver?.Invoke(survivalTime, score);

        Debug.Log($"[GameManager] 게임 오버 — 생존 {survivalTime:F1}초, {score}점");

        // 슬로모션 후 완전 정지
        Invoke(nameof(FreezeAfterGameOver), 0.8f);
    }

    private void FreezeAfterGameOver()
    {
        Time.timeScale = 0f;
    }

    // ─── 재시작 ──────────────────────────────────────────────────
    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // ─── 메인 메뉴 ───────────────────────────────────────────────
    public void GoToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(0);   // 0번 씬 = MainMenu
    }

    // ─── 한정선 획득 이벤트 ──────────────────────────────────────
    public void OnHanJeongSeonCollected()
    {
        OnHanJeongSeonGet?.Invoke();

        // 추격 시작
        ChaserSpawner.Instance?.StartSpawning();
        SurvivalManager.Instance?.StartSurvival();

        Debug.Log("[GameManager] 한정선 획득 → 추격 시작!");
    }

    // ─── 상태 변경 ───────────────────────────────────────────────
    private void SetState(GameState state) => CurrentState = state;
}
