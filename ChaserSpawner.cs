using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 추격자를 생성하고 수를 관리한다.
/// SurvivalManager의 난이도 단계에 따라 등장 간격과 최대 인원이 조절된다.
/// </summary>
public class ChaserSpawner : MonoBehaviour
{
    public static ChaserSpawner Instance { get; private set; }

    [Header("Spawn Settings")]
    [SerializeField] private GameObject chaserPrefab;
    [SerializeField] private int        maxChasers      = 20;
    [SerializeField] private float      minSpawnDist    = 15f;  // 플레이어와 최소 거리
    [SerializeField] private float      maxSpawnDist    = 25f;  // 최대 스폰 거리
    [SerializeField] private Transform  spawnParent;            // 스폰된 추격자의 부모 오브젝트

    [Header("Initial Spawn")]
    [SerializeField] private int   initialCount        = 5;     // 시작 시 즉시 스폰 수
    [SerializeField] private float initialSpawnInterval = 3f;   // 초기 등장 간격

    private readonly List<ChaserAI> activeChasers = new();
    private Transform playerTransform;
    private float     spawnInterval;
    private float     currentSpeedMultiplier = 1f;
    private bool      isSpawning;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) playerTransform = player.transform;

        // [FIX] 이미 난이도가 적용된 경우(= 씬 로드 순서상 UpdateDifficulty가 먼저 온 경우)
        //       초기 간격으로 덮어쓰지 않는다.
        if (spawnInterval <= 0f) spawnInterval = initialSpawnInterval;
    }

    // ─── 외부 API ─────────────────────────────────────────────────

    /// <summary>추격 시작 (한정선 획득 시 GameManager에서 호출)</summary>
    public void StartSpawning()
    {
        if (isSpawning) return;
        isSpawning = true;

        // 초기 추격자 즉시 스폰
        for (int i = 0; i < initialCount; i++)
            SpawnChaser();

        StartCoroutine(SpawnLoop());
    }

    /// <summary>모든 추격자를 제거하고 스폰을 중단한다.</summary>
    public void StopAndClearAll()
    {
        StopAllCoroutines();
        isSpawning = false;

        foreach (ChaserAI chaser in activeChasers)
            if (chaser != null) Destroy(chaser.gameObject);

        activeChasers.Clear();

        // 다음 런을 위해 기본값으로 복구
        spawnInterval          = initialSpawnInterval;
        currentSpeedMultiplier = 1f;
    }

    /// <summary>난이도에 따라 등장 간격과 추격자 속도를 갱신한다.</summary>
    public void UpdateDifficulty(float newInterval, float speedMultiplier)
    {
        spawnInterval          = newInterval;
        currentSpeedMultiplier = speedMultiplier;

        foreach (ChaserAI chaser in activeChasers)
            if (chaser != null) chaser.SpeedMultiplier = speedMultiplier;
    }

    /// <summary>현재 활성 추격자 수를 반환한다.</summary>
    public int ActiveChaserCount => activeChasers.Count;

    // ─── 스폰 루프 ───────────────────────────────────────────────
    private IEnumerator SpawnLoop()
    {
        while (isSpawning)
        {
            yield return new WaitForSeconds(spawnInterval);

            // 최대 인원 미만일 때만 스폰
            CleanDeadChasers();
            if (activeChasers.Count < maxChasers)
                SpawnChaser();
        }
    }

    private void SpawnChaser()
    {
        if (chaserPrefab == null || playerTransform == null) return;

        Vector3? spawnPos = FindSpawnPosition();
        if (spawnPos == null) return;

        GameObject obj    = Instantiate(chaserPrefab, spawnPos.Value, Quaternion.identity, spawnParent);
        ChaserAI   chaser = obj.GetComponent<ChaserAI>();

        if (chaser != null)
        {
            // [FIX] 새로 스폰되는 추격자도 현재 난이도의 속도 배율을 받아야 한다.
            //       (이전에는 생성 직후 항상 1.0배로 달려 단계 상승이 무시되었다)
            chaser.SpeedMultiplier = currentSpeedMultiplier;

            activeChasers.Add(chaser);
            Debug.Log($"[ChaserSpawner] 추격자 스폰. 현재 수: {activeChasers.Count}");
        }
    }

    // ─── 스폰 위치 계산 ──────────────────────────────────────────
    private Vector3? FindSpawnPosition()
    {
        for (int attempt = 0; attempt < 10; attempt++)
        {
            Vector2 randCircle = Random.insideUnitCircle;
            // insideUnitCircle가 (0,0)이면 정규화 결과가 0벡터가 되어
            // 플레이어 위치에 스폰될 수 있으므로 방향을 새로 구한다.
            if (randCircle.sqrMagnitude < 0.0001f)
                randCircle = Random.insideUnitCircle.normalized + new Vector2(0.001f, 0f);

            Vector2 dir = randCircle.normalized;
            float   randDist   = Random.Range(minSpawnDist, maxSpawnDist);
            Vector3 candidate  = playerTransform.position
                                 + new Vector3(dir.x, 0f, dir.y) * randDist;

            if (UnityEngine.AI.NavMesh.SamplePosition(
                    candidate, out UnityEngine.AI.NavMeshHit hit, 3f,
                    UnityEngine.AI.NavMesh.AllAreas))
                return hit.position;
        }
        return null;
    }

    // ─── 소멸된 추격자 정리 ──────────────────────────────────────
    private void CleanDeadChasers()
    {
        activeChasers.RemoveAll(c => c == null);
    }
}
