using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 게임 전체 상태(MainMenu / Playing / Paused / GameOver)를 관리한다.
///
/// [FIX] 상태 복구 버그 수정
///   - GameManager는 DontDestroyOnLoad이므로 씬이 바뀌어도 살아남는다.
///     예전 코드처럼 CurrentState를 복구하지 않으면 재시작 후에도 GameOver 상태로
///     남아 IsPlaying == false가 되어 플레이/AI/생존 타이머가 전부 정지한다.
///   - 씬 로드 완료 시점에 런타임 상태를 초기화하고, 게임플레이 씬 진입 시 Playing으로 복구한다.
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

    /// <summary>마지막 게임에서 최고 기록을 경신했는지. (GameOverUI에서 사용)</summary>
    public bool LastGameWasNewRecord { get; private set; }

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

    private void OnEnable()  { SceneManager.sceneLoaded += HandleSceneLoaded; }
    private void OnDisable() { SceneManager.sceneLoaded -= HandleSceneLoaded; }

    /// <summary>
    /// MainMenu 씬에 GameManager 오브젝트를 배치하지 않았을 때를 위한 런타임 생성 헬퍼.
    /// </summary>
    public static GameManager EnsureInstance()
    {
        if (Instance != null) return Instance;

        GameObject go = new GameObject("GameManager");
        return go.AddComponent<GameManager>();
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

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible   = false;

        ResetRunState();
        OnGameStart?.Invoke();
        Debug.Log("[GameManager] 게임 시작");
    }

    /// <summary>
    /// 런타임 상태(생존시간/점수/캡처 진행도/추격자)를 초기화한다.
    /// 씬 재로드 후에는 매니저 인스턴스 자체가 새로 만들어지지만,
    /// 씬 전환 없이 재시작하는 경우를 위해 명시적으로 호출한다.
    /// </summary>
    private void ResetRunState()
    {
        Time.timeScale = 1f;

        SurvivalManager.Instance?.ResetRun();
        ScoreManager.Instance?.ResetScore();
        CaptureProgressManager.Instance?.Reset();
        ChaserSpawner.Instance?.StopAndClearAll();
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

        // [FIX] 최고 기록 갱신 전에 이전 기록과 비교해야 "신기록" 판정이 정확하다.
        // 이전 코드처럼 저장 후에 비교하면 최고점 == 현재점이 되어 항상 신기록이 된다.
        int previousHighScore = ScoreManager.Instance != null ? ScoreManager.Instance.HighScore : 0;
        LastGameWasNewRecord = score > previousHighScore && score > 0;

        ScoreManager.Instance?.SaveHighScore(score);
        OnGameOver?.Invoke(survivalTime, score);

        Debug.Log($"[GameManager] 게임 오버 — 생존 {survivalTime:F1}초, {score}점");

        // 슬로모션 후 완전 정지
        StartCoroutine(FreezeAfterGameOver());
    }

    /// <summary>
    /// [FIX] Invoke는 scaled time이라 timeScale = 0.3 상태에서 0.8초가 2.67초로 늘어난다.
    /// 슬로모션 연출은 실제 시간 기준으로 제어해야 하므로 WaitForSecondsRealtime을 사용한다.
    /// </summary>
    private IEnumerator FreezeAfterGameOver()
    {
        yield return new WaitForSecondsRealtime(0.8f);
        Time.timeScale = 0f;
    }

    // ─── 재시작 ──────────────────────────────────────────────────
    public void RestartGame()
    {
        Time.timeScale = 1f;
        SetState(GameState.Playing);   // [FIX] 상태를 Playing으로 복구 (GameOver 잔존 방지)

        int index = SceneManager.GetActiveScene().buildIndex;
        if (index < 0)
        {
            Debug.LogError("[GameManager] 현재 씬이 Build Settings에 등록되어 있지 않아 재시작할 수 없습니다.");
            return;
        }

        SceneManager.LoadScene(index);
    }

    // ─── 메인 메뉴 ───────────────────────────────────────────────
    public void GoToMainMenu()
    {
        Time.timeScale = 1f;
        SetState(GameState.MainMenu);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;
        SceneManager.LoadScene(0);   // 0번 씬 = MainMenu
    }

    // ─── 한정선 획득 이벤트 ──────────────────────────────────────
    public void OnHanJeongSeonCollected()
    {
        OnHanJeongSeonGet?.Invoke();

        // [FIX] 순서 변경: 단계 적용(스폰 간격/속도 배율)이 먼저 끝나야
        // 이후 스폰되는 추격자가 올바른 배율로 생성된다.
        SurvivalManager.Instance?.StartSurvival();
        ChaserSpawner.Instance?.StartSpawning();

        Debug.Log("[GameManager] 한정선 획득 → 추격 시작!");
    }

    // ─── 씬 로드 완료 처리 ───────────────────────────────────────
    private void HandleSceneLoaded(Scene scene, LoadMode mode)
    {
        // 씬이 바뀌면 런타임 상태를 초기화한다.
        ResetRunState();

        // 게임플레이 씬 판별: ChaserSpawner 또는 Player가 존재하는 씬
        bool isGameplayScene = ChaserSpawner.Instance != null
                            || GameObject.FindGameObjectWithTag("Player") != null;

        if (isGameplayScene)
        {
            SetState(GameState.Playing);
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible   = false;
        }
        else
        {
            SetState(GameState.MainMenu);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible   = true;
        }
    }

    // ─── 상태 변경 ───────────────────────────────────────────────
    private void SetState(GameState state) => CurrentState = state;
}
