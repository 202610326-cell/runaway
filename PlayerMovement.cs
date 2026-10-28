using UnityEngine;

/// <summary>
/// 플레이어 이동, 달리기, 점프, 스태미나를 처리한다.
/// CharacterController 기반. 이동 방향은 카메라 기준으로 계산된다.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    // ─── 이동 설정 ───────────────────────────────────────────────
    [Header("Movement")]
    [SerializeField] private float walkSpeed    = 5f;
    [SerializeField] private float runSpeed     = 9f;
    [SerializeField] private float jumpHeight   = 1.5f;
    [SerializeField] private float gravity      = -20f;
    [SerializeField] private float rotationSpeed = 10f;

    // ─── 스태미나 설정 ───────────────────────────────────────────
    [Header("Stamina")]
    [SerializeField] private float maxStamina        = 100f;
    [SerializeField] private float staminaDrainRate  = 20f;
    [SerializeField] private float staminaRegenRate  = 10f;
    [SerializeField] private float staminaRegenDelay = 1.5f;

    // ─── 카메라 참조 ─────────────────────────────────────────────
    [Header("References")]
    [SerializeField] private Transform cameraTransform;

    // ─── 공개 속성 ───────────────────────────────────────────────
    public float CurrentStamina  => stamina;
    public float MaxStamina      => maxStamina;
    public bool  IsRunning       => isRunning;
    public bool  IsGrounded      => controller.isGrounded;
    public bool  IsCaptured      => isCaptured;

    // ─── Private 필드 ─────────────────────────────────────────────
    private CharacterController controller;
    private Vector3 velocity;
    private float   stamina;
    private float   regenTimer;
    private bool    isRunning;
    private bool    isCaptured;

    public event System.Action OnJump;
    public event System.Action OnLand;
    private bool wasGrounded;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        stamina    = maxStamina;
        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;
    }

    private void Update()
    {
        if (GameManager.Instance != null && !GameManager.Instance.IsPlaying)
            return;

        HandleLanding();
        HandleGravity();
        if (isCaptured) return;
        HandleMovement();
        HandleJump();
    }

    private void HandleMovement()
    {
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");
        Vector3 input = new Vector3(h, 0f, v);

        if (input.magnitude < 0.01f)
        {
            isRunning  = false;
            regenTimer = 0f;
            RegenerateStamina();
            return;
        }

        bool tryRun = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        isRunning   = tryRun && stamina > 0f;

        if (isRunning) DrainStamina();
        else           RegenerateStamina();

        float speed = isRunning ? runSpeed : walkSpeed;

        Vector3 camForward = cameraTransform.forward;
        Vector3 camRight   = cameraTransform.right;
        camForward.y = 0f; camRight.y = 0f;
        camForward.Normalize(); camRight.Normalize();

        Vector3 moveDir = (camForward * v + camRight * h).normalized;
        controller.Move(moveDir * speed * Time.deltaTime);

        if (moveDir.sqrMagnitude > 0.01f)
        {
            Quaternion targetRot = Quaternion.LookRotation(moveDir);
            transform.rotation  = Quaternion.Slerp(transform.rotation, targetRot,
                                                     rotationSpeed * Time.deltaTime);
        }
    }

    private void HandleJump()
    {
        if (Input.GetKeyDown(KeyCode.Space) && controller.isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            OnJump?.Invoke();
        }
    }

    private void HandleGravity()
    {
        if (controller.isGrounded && velocity.y < 0f)
            velocity.y = -2f;
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    private void HandleLanding()
    {
        bool grounded = controller.isGrounded;
        if (!wasGrounded && grounded) OnLand?.Invoke();
        wasGrounded = grounded;
    }

    private void DrainStamina()
    {
        stamina    = Mathf.Max(0f, stamina - staminaDrainRate * Time.deltaTime);
        regenTimer = staminaRegenDelay;
    }

    private void RegenerateStamina()
    {
        if (regenTimer > 0f) { regenTimer -= Time.deltaTime; return; }
        stamina = Mathf.Min(maxStamina, stamina + staminaRegenRate * Time.deltaTime);
    }

    public void SetCaptured(bool captured) => isCaptured = captured;
    public float GetStaminaRatio()         => stamina / maxStamina;
}
