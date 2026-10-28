using UnityEngine;

/// <summary>
/// 3인칭 추적 카메라.
/// - 마우스 입력으로 플레이어 주위를 Orbit 회전
/// - Raycast로 카메라와 플레이어 사이의 벽 감지 → 카메라를 앞으로 당김
/// - 달리기 시 약한 카메라 흔들림(Bob) 적용
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

    // ─── 벽 충돌 설정 ─────────────────────────────────────────────
    [Header("Collision")]
    [SerializeField] private LayerMask collisionMask;
    [SerializeField] private float     collisionRadius = 0.3f;

    // ─── 카메라 흔들림(Bob) ───────────────────────────────────────
    [Header("Camera Bob")]
    [SerializeField] private float bobAmplitude = 0.05f;
    [SerializeField] private float bobFrequency = 8f;

    private float yaw;
    private float pitch;
    private float bobTime;
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
        yaw   = transform.eulerAngles.y;
        pitch = transform.eulerAngles.x;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible   = false;
    }

    private void LateUpdate()
    {
        if (target == null) return;
        if (GameManager.Instance != null && GameManager.Instance.IsPaused) return;

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
        if (Physics.SphereCast(pivotPos, collisionRadius,
                               desiredDir, out RaycastHit hit,
                               defaultDistance, collisionMask))
        {
            targetDist = Mathf.Max(minDistance, hit.distance - collisionRadius);
        }

        transform.position = pivotPos + desiredDir * targetDist;
        transform.rotation = rotation;
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
