using UnityEngine;

/// <summary>
/// 점수 계산과 최고 기록을 관리한다.
/// - 생존 1초당 10점
/// - 한정선 획득 보너스
/// - 단계 달성 보너스
/// - PlayerPrefs로 최고 기록 저장
/// </summary>
public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    [Header("Score Settings")]
    [SerializeField] private float scorePerSecond     = 10f;
    [SerializeField] private int   hanJeongSeonBonus  = 300;
    [SerializeField] private int   noHitBonusInterval = 30;    // 초 단위, 무피격 보너스 지급 간격
    [SerializeField] private int   noHitBonusAmount   = 200;

    // ─── 공개 속성 ───────────────────────────────────────────────
    public int CurrentScore { get; private set; }
    public int HighScore    { get; private set; }

    // ─── 이벤트 ──────────────────────────────────────────────────
    public event System.Action<int> OnScoreChanged;
    public event System.Action<int> OnBonusEarned;   // 보너스 발생 시 UI 팝업용

    // ─── Private ─────────────────────────────────────────────────
    private float  scoreAccumulator;   // 소수점 점수 누적용
    private float  noHitTimer;
    private bool   wasHit;

    private const string HighScoreKey = "HanJeongSeon_HighScore";

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance  = this;
        HighScore = PlayerPrefs.GetInt(HighScoreKey, 0);
    }

    // ─── 점수 추가 API ───────────────────────────────────────────

    /// <summary>SurvivalManager가 매 프레임 호출 — deltaTime을 넘겨 1초당 scorePerSecond 누적</summary>
    public void AddSurvivalScore(float deltaTime)
    {
        scoreAccumulator += scorePerSecond * deltaTime;
        noHitTimer       += deltaTime;

        // 정수 점수 추가
        if (scoreAccumulator >= 1f)
        {
            int add          = Mathf.FloorToInt(scoreAccumulator);
            CurrentScore    += add;
            scoreAccumulator -= add;
            OnScoreChanged?.Invoke(CurrentScore);
        }

        // 무피격 보너스 확인
        if (!wasHit && noHitTimer >= noHitBonusInterval)
        {
            noHitTimer = 0f;
            AddBonus(noHitBonusAmount, "무피격 보너스");
        }
    }

    /// <summary>한정선 획득 시 보너스</summary>
    public void AddHanJeongSeonBonus()
    {
        AddBonus(hanJeongSeonBonus, "한정선 획득!");
    }

    /// <summary>단계 달성 보너스</summary>
    public void AddStageBonus(int amount)
    {
        if (amount <= 0) return;
        AddBonus(amount, "단계 달성!");
    }

    /// <summary>피격 상태 기록 (무피격 보너스 취소용)</summary>
    public void RegisterHit()
    {
        wasHit = true;
    }

    // ─── 최고 기록 저장 ──────────────────────────────────────────
    public void SaveHighScore(int score)
    {
        if (score > HighScore)
        {
            HighScore = score;
            PlayerPrefs.SetInt(HighScoreKey, HighScore);
            PlayerPrefs.Save();
            Debug.Log($"[ScoreManager] 새 최고 기록! {HighScore}점");
        }
    }

    public void ResetScore()
    {
        CurrentScore     = 0;
        scoreAccumulator = 0f;
        noHitTimer       = 0f;
        wasHit           = false;
        OnScoreChanged?.Invoke(CurrentScore);
    }

    // ─── 내부 ────────────────────────────────────────────────────
    private void AddBonus(int amount, string label)
    {
        CurrentScore += amount;
        OnScoreChanged?.Invoke(CurrentScore);
        OnBonusEarned?.Invoke(amount);
        Debug.Log($"[ScoreManager] {label} +{amount}점 (총 {CurrentScore}점)");
    }
}
