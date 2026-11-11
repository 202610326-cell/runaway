using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 추격자 AI.
/// 상태 머신: Patrol → Chase → Search → Capture
/// NavMeshAgent로 경로를 탐색하고, 시야각+거리로 플레이어를 감지한다.
///
/// [FIX] 시야 판정 버그 수정
///   이전 코드는 Physics.Raycast 결과가 true이기만 하면 "시야 차단"으로 처리했다.
///   sightMask에 플레이어 레이어가 포함되어 있으면 플레이어 몸통에 맞아서
///   항상 시야가 막힌 것으로 판정 → 추격자가 플레이어를 전혀 발견하지 못했다.
///   이제는 플레이어 콜라이더를 명시적으로 제외하고, 가장 가까운 '실제 장애물'과의
///   거리로 시야 차단 여부를 판단한다.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class ChaserAI : MonoBehaviour
{
    // ─── 감지 설정 ───────────────────────────────────────────────
    [Header("Detection")]
    [SerializeField] private float detectionRange  = 12f;
    [SerializeField] private float fieldOfView     = 110f;   // 시야각 (도)
    [SerializeField] private LayerMask sightMask;

    [Header("Eye Height")]
    [Tooltip("추격자의 눈 높이 (피드 기준, m)")]
    [SerializeField] private float eyeHeight       = 1.2f;
    [Tooltip("플레이어의 눈 높이 (피드 기준, m)")]
    [SerializeField] private float playerEyeHeight = 0.9f;

    // ─── 붙잡기 설정 ─────────────────────────────────────────────
    [Header("Capture")]
    [SerializeField] private float captureRange    = 1.3m;
    [SerializeField] private float captureSpeed    = 1f;     // 초당 CaptureProgress 증가량
    [SerializeField] private float captureDecay    = 0.5f;   // 거리 벌어질 때 초당 감소량

    // ─── 이동 설정 ───────────────────────────────────────────────
    [Header("Movement")]
    [SerializeField] private float patrolSpeed = 2.5f;
    [SerializeField] private float chaseSpeed  = 5.5f;
    [SerializeField] private float searchTime  = 5f;         // 마지막 위치 탐색 시간
    [SerializeField] private float patrolRadius = 8f;

    // ─── 목표 분산 ───────────────────────────────────────────────
    [Header("Spread")]
    [SerializeField] private float spreadRadius = 0.8f;      // 추격자끼리 목표 분산 반경
    [SerializeField] private float spreadRefreshInterval = 0.35f; // 분산 목표 갱신 주기(초)

    // ─────────────────────────────────────────────────────────────
    public enum State { Patrol, Chase, Search, Capture }
    public State CurrentState { get; private set; }

    private NavMeshAgent  agent;
    private Transform     playerTransform;
    private Vector3       lastKnownPosition;
    private float         searchTimer;
    private float         captureProgress;
    private Vector3       patrolTarget;

    // 시야 판정용 버퍼 (프레임마다 할당하지 않기 위해 재사용)
    private static readonly RaycastHit[] sightHits = new RaycastHit[16];

    private Vector3 chaseDestination;
    private float   spreadTimer;

    // 속도 배율 (SurvivalManager에서 조정)
    public float SpeedMultiplier { get; set; } = 1f;

    // ─────────────────────────────────────────────────────────────
    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    private void Start()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) playerTransform = player.transform;

        SetState(State.Patrol);
        SetPatrolTarget();
    }

    private void Update()
    {
        if (GameManager.Instance != null && !GameManager.Instance.IsPlaying)
            return;
        if (playerTransform == null) return;

        // 속도 배율 적용
        agent.speed = (CurrentState == State.Chase || CurrentState == State.Capture)
                      ? chaseSpeed  * SpeedMultiplier
                      : patrolSpeed * SpeedMultiplier;

        switch (CurrentState)
        {
            case State.Patrol:  UpdatePatrol();  break;
            case State.Chase:   UpdateChase();   break;
            case State.Search:  UpdateSearch();  break;
            case State.Capture: UpdateCapture(); break;
        }
    }

    // ─── 순찰 ─────────────────────────────────────────────────────
    private void UpdatePatrol()
    {
        if (CanSeePlayer())
        {
            SetState(State.Chase);
            return;
        }

        // 목표에 도달하면 다음 순찰 지점 설정
        if (!agent.pathPending && agent.remainingDistance < 0.5f)
            SetPatrolTarget();
    }

    private void SetPatrolTarget()
    {
        Vector3 randomDir  = Random.insideUnitSphere * patrolRadius;
        randomDir         += transform.position;
        randomDir.y        = transform.position.y;

        if (NavMesh.SamplePosition(randomDir, out NavMeshHit hit, patrolRadius, NavMesh.AllAreas))
        {
            patrolTarget = hit.position;
            agent.SetDestination(patrolTarget);
        }
    }

    // ─── 추격 ─────────────────────────────────────────────────────
    private void UpdateChase()
    {
        if (!CanSeePlayer())
        {
            lastKnownPosition = playerTransform.position;
            SetState(State.Search);
            return;
        }

        // 붙잡기 거리 판정
        float dist = Vector3.Distance(transform.position, playerTransform.position);
        if (dist <= captureRange)
        {
            SetState(State.Capture);
            return;
        }

        // 목표 위치에 약간의 분산을 줘서 여러 추격자가 겹치지 않게 한다.
        // [FIX] 매 프레임 새로 뽑으면 목적지가 계속 흔들려(지터) 추격이 어색해진다.
        //       갱신 주기를 두고 재사용한다.
        spreadTimer -= Time.deltaTime;
        if (spreadTimer <= 0f || chaseDestination == Vector3.zero)
        {
            spreadTimer = spreadRefreshInterval;

            Vector3 spread      = Random.insideUnitSphere * spreadRadius;
            spread.y            = 0f;
            Vector3 destination = playerTransform.position + spread;

            if (NavMesh.SamplePosition(destination, out NavMeshHit hit, spreadRadius + 1f, NavMesh.AllAreas))
                chaseDestination = hit.position;
            else
                chaseDestination = playerTransform.position;
        }

        agent.SetDestination(chaseDestination);
        lastKnownPosition = playerTransform.position;
    }

    // ─── 마지막 위치 탐색 ────────────────────────────────────────
    private void UpdateSearch()
    {
        if (CanSeePlayer())
        {
            SetState(State.Chase);
            return;
        }

        searchTimer -= Time.deltaTime;
        if (searchTimer <= 0f)
        {
            SetState(State.Patrol);
            return;
        }

        // 목표에 도달하면 제자리 대기
        if (!agent.pathPending && agent.remainingDistance < 0.5f)
            agent.isStopped = true;
    }

    // ─── 붙잡기 ─────────────────────────────────────────────────
    private void UpdateCapture()
    {
        float dist = Vector3.Distance(transform.position, playerTransform.position);

        if (dist <= captureRange)
        {
            // 붙잡힘 진행도 증가
            captureProgress += captureSpeed * Time.deltaTime;
            captureProgress  = Mathf.Clamp01(captureProgress);

            // 플레이어 이동 제한 & 진행도 전달
            CaptureProgressManager.Instance?.UpdateProgress(captureProgress);

            if (captureProgress >= 1f)
                GameManager.Instance?.TriggerGameOver();

            agent.isStopped = true;
        }
        else
        {
            // 거리가 벌어지면 진행도 감소 후 추격으로 복귀
            captureProgress -= captureDecay * Time.deltaTime;
            captureProgress  = Mathf.Max(0f, captureProgress);
            CaptureProgressManager.Instance?.UpdateProgress(captureProgress);

            agent.isStopped = false;
            SetState(State.Chase);
        }
    }

    // ─── 시야 확인 ───────────────────────────────────────────────
    private bool CanSeePlayer()
    {
        if (playerTransform == null) return false;

        // [FIX] Raycast 시작점을 1m 올리면서 길이는 수평거리로 두어 1m 부족했던 문제를 해결.
        //        눈/가슴 높이를 양쪽 다 명시해 3D 거리 기준으로 판정한다.
        Vector3 eyePos    = transform.position       + Vector3.up * eyeHeight;
        Vector3 targetPos = playerTransform.position + Vector3.up * playerEyeHeight;

        Vector3 toPlayer = targetPos - eyePos;
        float   dist     = toPlayer.magnitude;

        if (dist > detectionRange) return false;
        if (dist < 0.001f) return true;

        // 시야각 확인
        float angle = Vector3.Angle(transform.forward, toPlayer);
        if (angle > fieldOfView * 0.5f) return false;

        Vector3 dir = toPlayer / dist;

        // 장애물 확인.
        // Raycast는 '어디에' 맞았는지와 무관하게 true를 반환하므로,
        // 플레이어 콜라이더는 시야 차단 대상에서 제외해야 한다.
        int hitCount = Physics.RaycastNonAlloc(eyePos, dir, sightHits, dist,
                                               sightMask, QueryTriggerInteraction.Ignore);

        float closestBlocker = float.MaxValue;
        for (int i = 0; i < hitCount; i++)
        {
            if (IsPlayerTransform(sightHits[i].transform)) continue;  // 플레이어 자신은 제외
            if (sightHits[i].distance < closestBlocker)
                closestBlocker = sightHits[i].distance;
        }

        // 플레이어까지 시야가 확보되면 보인 것
        return closestBlocker >= dist - 0.05f;
    }

    /// <summary>해당 트랜스폼이 플레이어(또는 플레이어의 일부)인지 판정</summary>
    private bool IsPlayerTransform(Transform t)
    {
        if (t == null || playerTransform == null) return false;
        if (t == playerTransform) return true;              // 플레이어 자신
        if (t.IsChildOf(playerTransform)) return true;      // 플레이어의 자식 (장비 등)
        if (playerTransform.IsChildOf(t)) return true;      // 플레이어를 포함하는 부모
        return false;
    }

    // ─── 상태 전환 ───────────────────────────────────────────────
    private void SetState(State newState)
    {
        CurrentState = newState;

        switch (newState)
        {
            case State.Patrol:
                agent.isStopped = false;
                agent.speed     = patrolSpeed * SpeedMultiplier;
                SetPatrolTarget();
                break;

            case State.Chase:
                agent.isStopped = false;
                agent.speed     = chaseSpeed * SpeedMultiplier;
                spreadTimer     = 0f;                       // 분산 목표 즉시 재계산
                break;

            case State.Search:
                agent.isStopped = false;
                agent.speed     = patrolSpeed * SpeedMultiplier;
                searchTimer     = searchTime;
                agent.SetDestination(lastKnownPosition);
                break;

            case State.Capture:
                agent.isStopped = true;
                // [FIX] 무피격 보너스 취소를 위해 피격 사실 기록 (이전엔 호출처가 없었음)
                ScoreManager.Instance?.RegisterHit();
                break;
        }
    }

    // ─── Gizmos ─────────────────────────────────────────────────
    private void OnDrawGizmosSelected()
    {
        // 감지 범위
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position + Vector3.up * eyeHeight, detectionRange);

        // 붙잡기 범위
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, captureRange);
    }
}
