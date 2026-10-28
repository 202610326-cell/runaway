using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 추격자 AI.
/// 상태 머신: Patrol → Chase → Search → Capture
/// NavMeshAgent로 경로를 탐색하고, 시야각+거리로 플레이어를 감지한다.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class ChaserAI : MonoBehaviour
{
    // ─── 감지 설정 ───────────────────────────────────────────────
    [Header("Detection")]
    [SerializeField] private float detectionRange  = 12f;
    [SerializeField] private float fieldOfView     = 110f;   // 시야각 (도)
    [SerializeField] private LayerMask sightMask;

    // ─── 붙잡기 설정 ─────────────────────────────────────────────
    [Header("Capture")]
    [SerializeField] private float captureRange    = 1.3f;
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

    // ─────────────────────────────────────────────────────────────
    public enum State { Patrol, Chase, Search, Capture }
    public State CurrentState { get; private set; }

    private NavMeshAgent  agent;
    private Transform     playerTransform;
    private Vector3       lastKnownPosition;
    private float         searchTimer;
    private float         captureProgress;
    private Vector3       patrolTarget;

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

        // 목표 위치에 약간의 분산을 줘서 여러 추격자가 겹치지 않게 함
        Vector3 spread      = Random.insideUnitSphere * spreadRadius;
        spread.y            = 0f;
        Vector3 destination = playerTransform.position + spread;

        if (NavMesh.SamplePosition(destination, out NavMeshHit hit, spreadRadius + 1f, NavMesh.AllAreas))
            agent.SetDestination(hit.position);
        else
            agent.SetDestination(playerTransform.position);

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

        Vector3 toPlayer = playerTransform.position - transform.position;
        float   dist     = toPlayer.magnitude;

        if (dist > detectionRange) return false;

        // 시야각 확인
        float angle = Vector3.Angle(transform.forward, toPlayer);
        if (angle > fieldOfView * 0.5f) return false;

        // 장애물 확인 (Raycast)
        if (Physics.Raycast(transform.position + Vector3.up * 1f,
                            toPlayer.normalized, dist, sightMask))
            return false;

        return true;
    }

    // ─── 상태 전환 ───────────────────────────────────────────────
    private void SetState(State newState)
    {
        CurrentState = newState;

        switch (newState)
        {
            case State.Patrol:
                agent.isStopped = false;
                agent.speed     = patrolSpeed;
                SetPatrolTarget();
                break;

            case State.Chase:
                agent.isStopped = false;
                agent.speed     = chaseSpeed * SpeedMultiplier;
                break;

            case State.Search:
                agent.isStopped = false;
                agent.speed     = patrolSpeed * SpeedMultiplier;
                searchTimer     = searchTime;
                agent.SetDestination(lastKnownPosition);
                break;

            case State.Capture:
                agent.isStopped = true;
                break;
        }
    }

    // ─── Gizmos ─────────────────────────────────────────────────
    private void OnDrawGizmosSelected()
    {
        // 감지 범위
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        // 붙잡기 범위
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, captureRange);
    }
}
