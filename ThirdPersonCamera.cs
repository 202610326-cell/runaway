using UnityEngine;

/// <summary>
/// 3인칭 추적 카메라.
/// - 마우스 입력으로 플레이어 주위를 Orbit 회전
/// - SphereCast로 카메라와 플레이어 사이의 벽 감지 → 카메라를 앞으로 당김
/// - 달리기 시 약한 카메라 흔들림(Bob) 적용
///
/// [FIX] 카메라가 플레이어 몸에 붙어버리던 버그 수정
///   이전 코드는 SphereCast 결과가 true이면 곧바로 카메라를 당겼다.
///   collisionMask에 플레이어 레이어(Default)가 포함되어 있으면 플레이어 캡슐에
///   항상 걸려 카메라가 계속 minDistance로 붕괴했다.
///   이제는 플레이어 콜라이더를 제외한 '실제 장애물' 중 가장 가까운 것만 적용한다.
/// </summary>
public class ThirdPersonCamera : MonoBehaviour
{
    // ─── 추적 대상 ────────────────────────────────────────────────
    [Header("Target")]
    [SerializeField] private Transform target;
    [SerializeField] private Vector3   offset = new Vector3(0f, 1.8f, 0f);

    // ─── 회전 설정 ────────────────────────────────────────────────
    [Header("Rotation")]
    [SerializeField] private float mouseSensitivity = 3f;
    [SerializeField] private float minPitch         = -20f;
    [SerializeField] private float maxPitch         =  60f;

    // ─── 거리 설정 ────────────────────────────────────────────────
    [Header("Distance")]
    [SerializeField] private float defaultDistance = 5f;
    [SerializeField] private float minDistance     = 1.5f;
    [SerializeField] private float distanceSmoothTime = 0.08f;   // 거리 복귀 시 보간 속도

    // ─── 벽 충돌 설정 ─────────────────────────────────────────────
    [Header("Collision")]
    [SerializeField] private LayerMask collisionMask;
    [SerializeField] private float     collisionRadius = 0.3f;

    // ─── 카메라 흔들림(Bob) ───────────────────────────────────────
    [Header("Camera Bob")]
    [SerializeField] private float bobAmplitude = 0.05f;
    [SerializeField] private float bobFrequency = 8f;

    // 충돌 판정용 버퍼 (프레임마다 할당하지 않기 위해 재사용)
    private static readonly RaycastHit[] collisionHits = new RaycastHit[16];

    private float yaw;
    private float pitch;
    private float bobTime;
    private float currentDistance;
    private PlayerMovement playerMovement;

    private void Awake()
    {
        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                target         = player.transform;
                playerMovement = player.GetComponent<PlayerMovement>();
            }
        }
        else
        {
            playerMovement = target.GetComponent<PlayerMovement>();
        }

        // 초기 회전 값을 현재 카메라 회전에서 가져옴
        // [FIX] eulerAngles.x는 0~360 범위라 그대로 쓰면 pitch가 잘못 클램프된다.
        //        DeltaAngle로 -180~180 범위의 부호 있는 각도로 변환한다.
        yaw   = transform.eulerAngles.y;
        pitch = Mathf.Clamp(Mathf.DeltaAngle(0f, transform.eulerAngles.x), minPitch, maxPitch);

        currentDistance = defaultDistance;

        // [FIX] 커서 잠금은 게임 진행 중에만 한다.
        // MainMenu 씬에서 잠기면 시작 버튼을 클릭할 수 없다.
        if (GameManager.Instance != null && GameManager.Instance.IsPlaying)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible   = false;
        }
    }

    private void LateUpdate()
    {
        if (target == null) return;
        if (GameManager.Instance != null && !GameManager.Instance.IsPlaying) return;

        HandleRotation();
        HandlePositioning();
        HandleBob();
    }

    // ─── 회전 ─────────────────────────────────────────────────────
    private void HandleRotation()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        yaw   += mouseX;
        pitch -= mouseY;
        pitch  = Mathf.Clamp(pitch, minPitch, maxPitch);
    }

    // ─── 위치 (벽 충돌 처리 포함) ────────────────────────────────
    private void HandlePositioning()
    {
        Quaternion rotation    = Quaternion.Euler(pitch, yaw, 0f);
        Vector3    pivotPos    = target.position + offset;
        Vector3    desiredDir  = rotation * Vector3.back;
        float      targetDist  = defaultDistance;

        // 벽 충돌 감지: Pivot → 원하는 카메라 위치로 Sphere 캐스트
        // [FIX] 플레이어 콜라이더를 제외하고, 실제로 카메라를 막는 장애물만 적용한다.
        int hitCount = Physics.SphereCastNonAlloc(pivotPos, collisionRadius, desiredDir,
                                                  collisionHits, defaultDistance, collisionMask,
                                                  QueryTriggerInteraction.Ignore);

        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit hit = collisionHits[i];
            if (IsTargetCollider(hit.transform)) continue;   // 플레이어는 충돌 대상이 아님

            float d = hit.distance - collisionRadius;
            if (d < targetDist) targetDist = d;
        }

        targetDist = Mathf.Max(minDistance, targetDist);

        // 벽 쪽으로는 즉시 당겨 관통을 막고, 벽에서 멀어질 때만 부드럽게 복귀시킨다.
        if (targetDist < currentDistance)
            currentDistance = targetDist;
        else
        {
            float t = distanceSmoothTime <= 0f
                        ? 1f
                        : 1f - Mathf.Exp(-Time.deltaTime / distanceSmoothTime);
            currentDistance = Mathf.Lerp(currentDistance, targetDist, t);
        }

        transform.position = pivotPos + desiredDir * currentDistance;
        transform.rotation = rotation;
    }

    /// <summary>해당 트랜스폼이 플레이어(또는 플레이어의 일부)인지 판정</summary>
    private bool IsTargetCollider(Transform t)
    {
        if (t == null || target == null) return false;
        if (t == target) return true;              // 플레이어 자신
        if (t.IsChildOf(target)) return true;      // 플레이어의 자식 (장비 등)
        if (target.IsChildOf(t)) return true;      // 플레이어를 포함하는 부모
        return false;
    }

    // ─── 달리기 흔들림 ───────────────────────────────────────────
    private void HandleBob()
    {
        if (playerMovement == null || !playerMovement.IsRunning || !playerMovement.IsGrounded)
        {
            bobTime = 0f;
            return;
        }

        bobTime += Time.deltaTime * bobFrequency;
        float bobOffset = Mathf.Sin(bobTime) * bobAmplitude;
        transform.position += transform.up * bobOffset;
    }

    // ─── 외부 API ─────────────────────────────────────────────────

    /// <summary>일시정지/메뉴 전환 시 커서 상태를 설정한다.</summary>
    public void SetCursorLocked(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible   = !locked;
    }
}
