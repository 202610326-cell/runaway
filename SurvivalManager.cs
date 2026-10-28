using UnityEngine;

/// <summary>
/// 생존 타이머와 5단계 난이도 시스템을 관리한다.
/// 단계가 오를 때마다 ChaserSpawner의 등장 간격과 속도를 조정한다.
/// </summary>
public class SurvivalManager : MonoBehaviour
{
    public static SurvivalManager Instance { get; private set; }

    // ─── 난이도 단계 정의 ────────────────────────────────────────
    [System.Serializable]
    public class DifficultyStage
    {
        public string stageName;
        public float  startTime;        // 이 단계가 시작되는 생존 시간(초)
        public float  spawnInterval;    // 추격자 등장 간격
        public float  speedMultiplier;  // 추격자 이동 속도 배율
        public int    bonusScore;       // 단계 진입 시 보너스 점수
    }

    [Header("Difficulty Stages")]
    [SerializeField] private DifficultyStage[] stages = new DifficultyStage[]
    {
        new DifficultyStage { stageName="1단계", startTime=0f,   spawnInterval=8f,  speedMultiplier=1.0f, bonusScore=0    },
        new DifficultyStage { stageName="2단계", startTime=30f,  spawnInterval=6f,  speedMultiplier=1.1f, bonusScore=500  },
        new DifficultyStage { stageName="3단계", startTime=60f,  spawnInterval=4f,  speedMultiplier=1.2f, bonusScore=1000 },
        new DifficultyStage { stageName="4단계", startTime=120f, spawnInterval=2.5f,speedMultiplier=1.35f,bonusScore=2000 },
        new DifficultyStage { stageName="5단계", startTime=180f, spawnInterval=1.5f,speedMultiplier=1.5f, bonusScore=3000 },
    };

    // ─── 공개 속성 ───────────────────────────────────────────────
    public float SurvivalTime    { get; private set; }
    public int   CurrentStageIdx { get; private set; }
    public string CurrentStageName => stages[CurrentStageIdx].stageName;

    // ─── 이벤트 ──────────────────────────────────────────────────
    public event System.Action<int, string> OnStageChanged;  // (stageIdx, stageName)

    private bool isSurviving;

    // ─────────────────────────────────────────────────────────────
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Update()
    {
        if (!isSurviving) return;
        if (GameManager.Instance != null && !GameManager.Instance.IsPlaying) return;

        SurvivalTime += Time.deltaTime;
        ScoreManager.Instance?.AddSurvivalScore(Time.deltaTime);

        CheckStageProgression();
    }

    // ─── 생존 시작 (한정선 획득 시 호출) ──────────────────────────
    public void StartSurvival()
    {
        isSurviving      = true;
        SurvivalTime     = 0f;
        CurrentStageIdx  = 0;
        ApplyStage(0);
        Debug.Log("[SurvivalManager] 생존 타이머 시작");
    }

    public void StopSurvival() => isSurviving = false;

    // ─── 단계 진행 확인 ──────────────────────────────────────────
    private void CheckStageProgression()
    {
        if (CurrentStageIdx >= stages.Length - 1) return;

        DifficultyStage next = stages[CurrentStageIdx + 1];
        if (SurvivalTime >= next.startTime)
        {
            CurrentStageIdx++;
            ApplyStage(CurrentStageIdx);

            // 보너스 점수 지급
            ScoreManager.Instance?.AddStageBonus(next.bonusScore);
            OnStageChanged?.Invoke(CurrentStageIdx, next.stageName);
            Debug.Log($"[SurvivalManager] {next.stageName} 진입! +{next.bonusScore}점");
        }
    }

    // ─── 난이도 적용 ─────────────────────────────────────────────
    private void ApplyStage(int idx)
    {
        if (idx < 0 || idx >= stages.Length) return;
        DifficultyStage stage = stages[idx];
        ChaserSpawner.Instance?.UpdateDifficulty(stage.spawnInterval, stage.speedMultiplier);
    }

    // ─── 생존 시간 포맷 ──────────────────────────────────────────
    public string GetFormattedTime()
    {
        int minutes = Mathf.FloorToInt(SurvivalTime / 60f);
        int seconds = Mathf.FloorToInt(SurvivalTime % 60f);
        return $"{minutes:00}:{seconds:00}";
    }
}
