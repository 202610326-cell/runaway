using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 절차적 캐릭터 비주얼.
///
/// Capsule 프리팹(placeholder) 위에 컴포넌트를 붙이면, 기본 프리미티브를 조합해
/// 저폴리 캐릭터를 만들고 관절을 회전시켜 걷기/달리기 애니메이션까지 구동한다.
///
/// • 루트 오브젝트는 그대로 유지한다 (CharacterController / NavMeshAgent / 캡슐 콜라이더)
///   → 비주얼은 항상 자식 오브젝트로 만들어야 물리/내비게이션이 깨지지 않는다.
/// • 파츠의 콜라이더는 전부 제거한다.
/// • 별도 에니메이터/애니메이션 클립이 필요 없다 (런타임 계산)
/// </summary>
[AddComponentMenu("Character/Character Visual")]
public class CharacterVisual : MonoBehaviour
{
    /// <summary>사용할 캐릭터 프리셋</summary>
    public enum Preset
    {
        Player,   // 한정선 수색자
        Chaser,   // 추격자
        Custom,   // 인스펙터에서 직접 정의
    }

    // ─── 설정 ────────────────────────────────────────────────────
    [Header("Preset")]
    [SerializeField] private Preset preset = Preset.Player;
    [SerializeField] private CharacterRig customRig;

    [Header("Options")]
    [Tooltip("루트의 캡슐 메시를 숨긴다 (비주얼이 대신 보이도록)")]
    [SerializeField] private bool hideRootMesh = true;

    [Tooltip("고개 시선 대상으로 플레이어를 자동 탐색한다 (추격자용)")]
    [SerializeField] private bool usePlayerAsLookTarget = true;

    [SerializeField] private Transform lookAtTarget;

    // ─── 런타임 ──────────────────────────────────────────────────
    private CharacterRig rig;

    private Transform visualRoot;
    private Transform boneHips;
    private Transform boneTorso;
    private Transform boneHead;
    private Transform boneArmL;
    private Transform boneArmR;
    private Transform boneLegL;
    private Transform boneLegR;

    private readonly Dictionary<string, Transform> shapeObjects = new Dictionary<string, Transform>();
    private readonly List<Transform> eyeObjects = new List<Transform>();
    private readonly Dictionary<Transform, Vector3> eyeBaseScales = new Dictionary<Transform, Vector3>();

    private Vector3 baseHipPosition;
    private Vector3 torsoBaseScale;

    private CharacterController controller;
    private NavMeshAgent agent;

    private float phase;
    private float blinkTimer;
    private float blinkRemaining;
    private bool  blinking;

    // ─────────────────────────────────────────────────────────────
    private void Awake()
    {
        Build();
    }

    /// <summary>비주얼을 다시 만든다 (프리셋을 바꾸고 싶을 때 런타임에서 호출).</summary>
    [ContextMenu("Rebuild Visual")]
    public void Build()
    {
        CharacterRig built = preset == Preset.Custom ? customRig : CharacterPresets.Create(preset);
        if (built == null)
        {
            Debug.LogError($"[CharacterVisual] '{name}': 사용할 리그를 찾을 수 없습니다. 프리셋을 지정하세요.", this);
            return;
        }
        rig = built;

        // 리빌드 대비 기존 비주얼 제거
        if (visualRoot != null)
        {
            if (Application.isPlaying) Destroy(visualRoot.gameObject);
            else                   DestroyImmediate(visualRoot.gameObject);
        }
        shapeObjects.Clear();
        eyeObjects.Clear();
        eyeBaseScales.Clear();

        GameObject visualGo = new GameObject("Visual");
        // 에디터에서 미리보기한 비주얼이 씬 파일에 저장되지 않도록 한다
        if (!Application.isPlaying) visualGo.hideFlags = HideFlags.DontSave;

        visualRoot = visualGo.transform;
        visualRoot.SetParent(transform, false);
        visualRoot.gameObject.layer = gameObject.layer;

        BuildBones();
        BuildShapes();

        if (hideRootMesh) HideRootRenderer();

        // 이동 속도 공급원
        controller = GetComponent<CharacterController>();
        agent     = GetComponent<NavMeshAgent>();

        // 시선 대상
        if (usePlayerAsLookTarget && lookAtTarget == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) lookAtTarget = player.transform;
        }

        phase      = 0f;
        blinking   = false;
        blinkTimer = UnityEngine.Random.Range(rig.look.blinkIntervalMin, rig.look.blinkIntervalMax);
    }

    // ─── 골격(관절) 생성 ─────────────────────────────────────────
    private void BuildBones()
    {
        CharacterSkeleton s = rig.skeleton;

        boneHips  = CreateBone("Hips",  visualRoot, new Vector3(0f, s.hipHeight, 0f));
        boneTorso = CreateBone("Torso", boneHips,  Vector3.zero);
        boneHead  = CreateBone("Head",  boneTorso,
                               new Vector3(0f, s.torsoLength + s.neckLength, 0f));

        float shoulderY = s.torsoLength - s.shoulderDrop;
        boneArmL = CreateBone("ArmL", boneTorso, new Vector3( s.shoulderWidth, shoulderY, 0f));
        boneArmR = CreateBone("ArmR", boneTorso, new Vector3(-s.shoulderWidth, shoulderY, 0f));

        boneLegL = CreateBone("LegL", boneHips, new Vector3( s.hipWidth, 0f, 0f));
        boneLegR = CreateBone("LegR", boneHips, new Vector3(-s.hipWidth, 0f, 0f));

        baseHipPosition = boneHips.localPosition;
    }

    private Transform CreateBone(string boneName, Transform parent, Vector3 localPosition)
    {
        GameObject go = new GameObject(boneName);
        go.transform.SetParent(parent, false);
        go.transform.localPosition    = localPosition;
        go.transform.localRotation    = Quaternion.identity;
        go.transform.localScale       = Vector3.one;
        return go.transform;
    }

    // ─── 파츠 생성 ───────────────────────────────────────────────
    private void BuildShapes()
    {
        for (int i = 0; i < rig.shapes.Count; i++)
        {
            CharacterShape shape = rig.shapes[i];
            Transform bone = GetBone(shape.bone);
            if (bone == null) continue;

            GameObject go = PrimitiveBuilder.CreateShape(shape, bone, gameObject.layer);
            shapeObjects[shape.name] = go.transform;

            if (shape.isEye)
            {
                eyeObjects.Add(go.transform);
                eyeBaseScales[go.transform] = go.transform.localScale;
            }
        }

        // 호흡 적용 대상(몸통)
        if (shapeObjects.TryGetValue("torso", out Transform torso))
            torsoBaseScale = torso.localScale;
    }

    private Transform GetBone(CharacterBone bone)
    {
        switch (bone)
        {
            case CharacterBone.Hips:  return boneHips;
            case CharacterBone.Torso: return boneTorso;
            case CharacterBone.Head:  return boneHead;
            case CharacterBone.ArmL:  return boneArmL;
            case CharacterBone.ArmR:  return boneArmR;
            case CharacterBone.LegL:  return boneLegL;
            case CharacterBone.LegR:  return boneLegR;
            default:                  return null;
        }
    }

    private void HideRootRenderer()
    {
        Renderer r = GetComponent<Renderer>();
        if (r != null) r.enabled = false;
    }

    // ─── 갱신 ─────────────────────────────────────────────────────
    private void Update()
    {
        if (rig == null || visualRoot == null) return;

        float dt    = Time.deltaTime;
        float speed = GetCurrentSpeed();

        UpdateLocomotion(speed, dt);
        UpdateBreathing(speed, dt);
        UpdateBlink(dt);
        UpdateHeadLook();
    }

    private float GetCurrentSpeed()
    {
        if (controller != null) return controller.velocity.magnitude;
        if (agent     != null) return agent.velocity.magnitude;
        return 0f;
    }

    /// <summary>걷기/달리기: 다리·팔 스윙, 상하 보빙, 달리기 숙임</summary>
    private void UpdateLocomotion(float speed, float dt)
    {
        CharacterLocomotion loc = rig.locomotion;

        // 흔들림이 사라져야 정지 상태에서 다리가 기울어 보이지 않는다
        float moveAmount = Mathf.Clamp01(speed / 0.6f);
        float norm       = Mathf.Clamp01(speed / Mathf.Max(0.1f, loc.referenceSpeed));

        // 보폭은 거리 기준으로 진행 → 발이 미끄러지지 않는다
        phase += speed * loc.strideFrequency * dt;
        if (phase > 10000f) phase -= 10000f;

        float amplitude = Mathf.Lerp(loc.walkSwing, loc.runSwing, norm) * moveAmount;
        float swing     = Mathf.Sin(phase * Mathf.PI * 2f) * amplitude;

        boneLegL.localRotation = Quaternion.Euler( swing, 0f, 0f);
        boneLegR.localRotation = Quaternion.Euler(-swing, 0f, 0f);
        boneArmL.localRotation = Quaternion.Euler(-swing * loc.armSwingScale, 0f, 0f);
        boneArmR.localRotation = Quaternion.Euler( swing * loc.armSwingScale, 0f, 0f);

        // 걸음 박자 2배로 보빙
        float bob = Mathf.Abs(Mathf.Sin(phase * Mathf.PI * 2f)) * loc.bobAmount * moveAmount;
        boneHips.localPosition = baseHipPosition + new Vector3(0f, bob, 0f);

        // 달리면 앞으로 숙인다
        boneTorso.localRotation = Quaternion.Euler(-loc.runLean * norm, 0f, 0f);
    }

    /// <summary>호흡: 멈춰 있을 때 몸통이 살짝 부풀었다 꺼진다</summary>
    private void UpdateBreathing(float speed, float dt)
    {
        if (torsoBaseScale == Vector3.zero) return;

        float idleAmount = Mathf.Clamp01(1f - speed / 0.8f);
        float breath     = 1f + Mathf.Sin(Time.time * rig.look.breathSpeed) * rig.look.breathAmount * idleAmount;

        if (shapeObjects.TryGetValue("torso", out Transform torso))
        {
            Vector3 s = torso.localScale;
            torso.localScale = new Vector3(s.x, torsoBaseScale.y * breath, s.z);
        }
    }

    /// <summary>눈 깜빡임</summary>
    private void UpdateBlink(float dt)
    {
        if (!rig.look.blinkEnabled || eyeObjects.Count == 0) return;

        if (blinking)
        {
            blinkRemaining -= dt;
            if (blinkRemaining <= 0f)
            {
                blinking = false;
                foreach (Transform eye in eyeObjects)
                {
                    if (eyeBaseScales.TryGetValue(eye, out Vector3 baseScale))
                        eye.localScale = baseScale;
                }
            }
            return;
        }

        blinkTimer -= dt;
        if (blinkTimer > 0f) return;

        // 눈 감기
        blinking      = true;
        blinkRemaining = rig.look.blinkDuration;
        foreach (Transform eye in eyeObjects)
        {
            Vector3 s = eye.localScale;
            eye.localScale = new Vector3(s.x, s.y * 0.15f, s.z);
        }

        blinkTimer = UnityEngine.Random.Range(rig.look.blinkIntervalMin, rig.look.blinkIntervalMax);
    }

    /// <summary>고개 시선: 추격자는 플레이어를 계속 노려본다</summary>
    private void UpdateHeadLook()
    {
        if (!rig.look.headLookAt || lookAtTarget == null || boneHead == null) return;

        Vector3 worldDir = lookAtTarget.position - boneHead.position;
        if (worldDir.sqrMagnitude < 0.0001f) return;

        Vector3 localDir = transform.InverseTransformDirection(worldDir.normalized);

        float yaw   = Mathf.Atan2(localDir.x, localDir.z) * Mathf.Rad2Deg;
        float pitch = -Mathf.Asin(Mathf.Clamp(localDir.y, -1f, 1f)) * Mathf.Rad2Deg;

        yaw   = Mathf.Clamp(yaw,   -rig.look.headTurnLimit,  rig.look.headTurnLimit);
        pitch = Mathf.Clamp(pitch, -rig.look.headPitchLimit, rig.look.headPitchLimit);

        boneHead.localRotation = Quaternion.Euler(pitch, yaw, 0f);
    }
}
