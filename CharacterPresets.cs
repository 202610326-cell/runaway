using UnityEngine;

/// <summary>
/// 미리 정의된 캐릭터 프리셋.
/// Capsule placeholder를 대체하는 저폴리 캐릭터 구성을 코드로 정의한다.
///
/// • 플레이어 "한정선 수색자"  – 청록색 재킷, 배낭, 일반적인 사람 비율
/// • 추격자 "추격자"          – 굽은 자세, 긴 팔, 등 가시, 붉게 빛나는 눈
/// </summary>
public static class CharacterPresets
{
    // ─── 색상 팔레트 ─────────────────────────────────────────────
    private static Color Hex(int rgb) => new Color(
        ((rgb >> 16) & 0xFF) / 255f,
        ((rgb >> 8) & 0xFF) / 255f,
        (rgb & 0xFF) / 255f, 1f);

    // 플레이어
    private static readonly Color P_Coat     = Hex(0x2E6E8E);
    private static readonly Color P_CoatDark = Hex(0x1F4E66);
    private static readonly Color P_Pants    = Hex(0x33405A);
    private static readonly Color P_Skin     = Hex(0xF0C9A5);
    private static readonly Color P_Hair     = Hex(0x2A2027);
    private static readonly Color P_Boot     = Hex(0x1C1A20);
    private static readonly Color P_Pack     = Hex(0x6B4A2F);
    private static readonly Color P_Accent   = Hex(0xFFD166);
    private static readonly Color P_EyeWhite = Hex(0xF5F5F5);
    private static readonly Color P_EyeDark  = Hex(0x1A1A1E);

    // 추격자
    private static readonly Color C_Body   = Hex(0x201C24);
    private static readonly Color C_Plate  = Hex(0x342C3A);
    private static readonly Color C_Spike  = Hex(0x4A3B52);
    private static readonly Color C_Bone   = Hex(0xE8E0D0);
    private static readonly Color C_EyeGlow = Hex(0xFF3B2F);

    // ─────────────────────────────────────────────────────────────
    /// <summary>플레이어용 리그를 만든다.</summary>
    public static CharacterRig CreatePlayer()
    {
        CharacterRig rig = new CharacterRig();
        rig.displayName = "한정선 수색자";

        rig.skeleton = new CharacterSkeleton
        {
            hipHeight      = 0.92f,
            torsoLength    = 0.58f,
            neckLength     = 0.10f,
            shoulderWidth  = 0.20f,
            shoulderDrop   = 0f,
            hipWidth       = 0.095f,
        };

        rig.look = new CharacterLook
        {
            blinkEnabled    = true,
            blinkIntervalMin = 2.0f,
            blinkIntervalMax = 5.5f,
            blinkDuration   = 0.12f,
            headLookAt      = false,
            breathAmount    = 0.018f,
            breathSpeed     = 1.4f,
        };

        rig.locomotion = new CharacterLocomotion
        {
            strideFrequency = 1.15f,
            walkSwing       = 20f,
            runSwing        = 38f,
            armSwingScale   = 0.8f,
            bobAmount       = 0.03f,
            runLean         = 8f,
            referenceSpeed  = 5f,
        };

        // ─── 몸통 ────────────────────────────────────────────────
        Add(rig, "pelvis", CharacterBone.Torso, CharacterShapeType.Cube,
            new Vector3(0f, 0.02f, 0f), new Vector3(0.30f, 0.18f, 0.21f), P_Pants);

        Add(rig, "torso", CharacterBone.Torso, CharacterShapeType.Cube,
            new Vector3(0f, 0.32f, 0f), new Vector3(0.34f, 0.50f, 0.22f), P_Coat);

        Add(rig, "strap", CharacterBone.Torso, CharacterShapeType.Cube,
            new Vector3(0f, 0.42f, 0f), new Vector3(0.355f, 0.085f, 0.235f), P_CoatDark);

        Add(rig, "collar", CharacterBone.Torso, CharacterShapeType.Cube,
            new Vector3(0f, 0.60f, 0f), new Vector3(0.26f, 0.075f, 0.20f), P_Accent);

        // 한정선을 담는 배낭
        Add(rig, "backpack", CharacterBone.Torso, CharacterShapeType.Cube,
            new Vector3(0f, 0.32f, -0.17f), new Vector3(0.26f, 0.34f, 0.15f), P_Pack);

        Add(rig, "packFlap", CharacterBone.Torso, CharacterShapeType.Cube,
            new Vector3(0f, 0.42f, -0.245f), new Vector3(0.22f, 0.14f, 0.03f), P_Accent);

        // ─── 머리 ────────────────────────────────────────────────
        Add(rig, "neck", CharacterBone.Head, CharacterShapeType.Cylinder,
            new Vector3(0f, -0.135f, 0f), new Vector3(0.095f, 0.09f, 0.095f), P_Skin);

        Add(rig, "head", CharacterBone.Head, CharacterShapeType.Sphere,
            new Vector3(0f, 0f, 0f), new Vector3(0.27f, 0.30f, 0.27f), P_Skin);

        Add(rig, "hair", CharacterBone.Head, CharacterShapeType.Sphere,
            new Vector3(0f, 0.075f, -0.02f), new Vector3(0.285f, 0.22f, 0.285f), P_Hair);

        Add(rig, "brow", CharacterBone.Head, CharacterShapeType.Cube,
            new Vector3(0f, 0.072f, 0.115f), new Vector3(0.16f, 0.022f, 0.03f), P_Hair);

        // 눈 (깜빡임 대상)
        Add(rig, "eyeL_sclera", CharacterBone.Head, CharacterShapeType.Sphere,
            new Vector3(0.058f, 0.010f, 0.112f), new Vector3(0.075f, 0.062f, 0.05f), P_EyeWhite, isEye: true);
        Add(rig, "eyeR_sclera", CharacterBone.Head, CharacterShapeType.Sphere,
            new Vector3(-0.058f, 0.010f, 0.112f), new Vector3(0.075f, 0.062f, 0.05f), P_EyeWhite, isEye: true);
        Add(rig, "eyeL_pupil", CharacterBone.Head, CharacterShapeType.Sphere,
            new Vector3(0.058f, 0.008f, 0.135f), new Vector3(0.032f, 0.032f, 0.02f), P_EyeDark, isEye: true);
        Add(rig, "eyeR_pupil", CharacterBone.Head, CharacterShapeType.Sphere,
            new Vector3(-0.058f, 0.008f, 0.135f), new Vector3(0.032f, 0.032f, 0.02f), P_EyeDark, isEye: true);

        // ─── 팔 ──────────────────────────────────────────────────
        Add(rig, "armL", CharacterBone.ArmL, CharacterShapeType.Capsule,
            new Vector3(0f, -0.24f, 0f), new Vector3(0.10f, 0.50f, 0.10f), P_Coat);
        Add(rig, "armR", CharacterBone.ArmR, CharacterShapeType.Capsule,
            new Vector3(0f, -0.24f, 0f), new Vector3(0.10f, 0.50f, 0.10f), P_Coat);

        Add(rig, "handL", CharacterBone.ArmL, CharacterShapeType.Sphere,
            new Vector3(0f, -0.50f, 0f), new Vector3(0.10f, 0.115f, 0.10f), P_Skin);
        Add(rig, "handR", CharacterBone.ArmR, CharacterShapeType.Sphere,
            new Vector3(0f, -0.50f, 0f), new Vector3(0.10f, 0.115f, 0.10f), P_Skin);

        // ─── 다리 ─────────────────────────────────────────────────
        Add(rig, "legL", CharacterBone.LegL, CharacterShapeType.Capsule,
            new Vector3(0f, -0.42f, 0f), new Vector3(0.125f, 0.84f, 0.125f), P_Pants);
        Add(rig, "legR", CharacterBone.LegR, CharacterShapeType.Capsule,
            new Vector3(0f, -0.42f, 0f), new Vector3(0.125f, 0.84f, 0.125f), P_Pants);

        Add(rig, "shoeL", CharacterBone.LegL, CharacterShapeType.Cube,
            new Vector3(0f, -0.87f, 0.035f), new Vector3(0.145f, 0.10f, 0.25f), P_Boot);
        Add(rig, "shoeR", CharacterBone.LegR, CharacterShapeType.Cube,
            new Vector3(0f, -0.87f, 0.035f), new Vector3(0.145f, 0.10f, 0.25f), P_Boot);

        return rig;
    }

    // ─────────────────────────────────────────────────────────────
    /// <summary>추격자용 리그를 만든다.</summary>
    public static CharacterRig CreateChaser()
    {
        CharacterRig rig = new CharacterRig();
        rig.displayName = "추격자";

        rig.skeleton = new CharacterSkeleton
        {
            hipHeight      = 1.02f,   // 플레이어보다 큼
            torsoLength    = 0.60f,
            neckLength     = 0.02f,   // 목이 없음 → 어깨에 머리가 바로 얹힌 느낌
            shoulderWidth  = 0.30f,   // 어깨가 넓음
            shoulderDrop   = 0.06f,   // 어깨가 앞으로 굽어 있음
            hipWidth       = 0.13f,
        };

        rig.look = new CharacterLook
        {
            blinkEnabled    = false,   // 무눈동자 → 더 위협적
            headLookAt      = true,    // 플레이어를 계속 노려본다
            headTurnLimit   = 70f,
            headPitchLimit  = 20f,
            breathAmount    = 0.012f,
            breathSpeed     = 0.9f,
        };

        rig.locomotion = new CharacterLocomotion
        {
            strideFrequency = 0.95f,
            walkSwing       = 26f,
            runSwing        = 46f,
            armSwingScale   = 0.5f,   // 팔을 크게 흔들지 않음 (위압감)
            bobAmount       = 0.05f,
            runLean         = 14f,
            referenceSpeed  = 5.5f,
        };

        // ─── 몸통 ────────────────────────────────────────────────
        Add(rig, "pelvis", CharacterBone.Torso, CharacterShapeType.Cube,
            new Vector3(0f, 0.02f, 0f), new Vector3(0.34f, 0.20f, 0.24f), C_Body);

        Add(rig, "torso", CharacterBone.Torso, CharacterShapeType.Cube,
            new Vector3(0f, 0.33f, 0f), new Vector3(0.46f, 0.60f, 0.30f), C_Body);

        Add(rig, "chestPlate", CharacterBone.Torso, CharacterShapeType.Cube,
            new Vector3(0f, 0.42f, 0.135f), new Vector3(0.34f, 0.26f, 0.07f), C_Plate);

        Add(rig, "ribA", CharacterBone.Torso, CharacterShapeType.Cube,
            new Vector3(0f, 0.20f, 0f), new Vector3(0.48f, 0.05f, 0.33f), C_Plate);
        Add(rig, "ribB", CharacterBone.Torso, CharacterShapeType.Cube,
            new Vector3(0f, 0.30f, 0f), new Vector3(0.48f, 0.05f, 0.33f), C_Plate);

        Add(rig, "spine", CharacterBone.Torso, CharacterShapeType.Cube,
            new Vector3(0f, 0.33f, -0.175f), new Vector3(0.11f, 0.58f, 0.07f), C_Spike);

        // 등 가시 (실루엣을 날카롭게)
        Add(rig, "spikeA", CharacterBone.Torso, CharacterShapeType.Cube,
            new Vector3(0f, 0.20f, -0.21f), new Vector3(0.075f, 0.26f, 0.075f), C_Spike,
            euler: new Vector3(-40f, 0f, 0f));
        Add(rig, "spikeB", CharacterBone.Torso, CharacterShapeType.Cube,
            new Vector3(0f, 0.36f, -0.21f), new Vector3(0.075f, 0.26f, 0.075f), C_Spike,
            euler: new Vector3(-40f, 0f, 0f));
        Add(rig, "spikeC", CharacterBone.Torso, CharacterShapeType.Cube,
            new Vector3(0f, 0.51f, -0.21f), new Vector3(0.075f, 0.26f, 0.075f), C_Spike,
            euler: new Vector3(-40f, 0f, 0f));

        // ─── 머리 ────────────────────────────────────────────────
        Add(rig, "head", CharacterBone.Head, CharacterShapeType.Sphere,
            new Vector3(0f, 0f, 0f), new Vector3(0.25f, 0.23f, 0.26f), C_Body);

        Add(rig, "brow", CharacterBone.Head, CharacterShapeType.Cube,
            new Vector3(0f, 0.055f, 0.095f), new Vector3(0.235f, 0.05f, 0.09f), C_Plate);

        // 붉게 빛나는 눈
        Add(rig, "eyeL", CharacterBone.Head, CharacterShapeType.Sphere,
            new Vector3(0.055f, 0.012f, 0.125f), new Vector3(0.065f, 0.045f, 0.04f), C_EyeGlow,
            unlit: true, isEye: true);
        Add(rig, "eyeR", CharacterBone.Head, CharacterShapeType.Sphere,
            new Vector3(-0.055f, 0.012f, 0.125f), new Vector3(0.065f, 0.045f, 0.04f), C_EyeGlow,
            unlit: true, isEye: true);

        Add(rig, "jaw", CharacterBone.Head, CharacterShapeType.Cube,
            new Vector3(0f, -0.085f, 0.07f), new Vector3(0.17f, 0.075f, 0.13f), C_Plate);

        Add(rig, "teeth", CharacterBone.Head, CharacterShapeType.Cube,
            new Vector3(0f, -0.072f, 0.135f), new Vector3(0.13f, 0.035f, 0.035f), C_Bone);

        // ─── 팔 (비대하게 길다) ──────────────────────────────────
        Add(rig, "shoulderL", CharacterBone.ArmL, CharacterShapeType.Sphere,
            new Vector3(0f, -0.04f, 0f), new Vector3(0.27f, 0.23f, 0.27f), C_Plate);
        Add(rig, "shoulderR", CharacterBone.ArmR, CharacterShapeType.Sphere,
            new Vector3(0f, -0.04f, 0f), new Vector3(0.27f, 0.23f, 0.27f), C_Plate);

        Add(rig, "armL", CharacterBone.ArmL, CharacterShapeType.Capsule,
            new Vector3(0f, -0.38f, 0f), new Vector3(0.115f, 0.76f, 0.115f), C_Body);
        Add(rig, "armR", CharacterBone.ArmR, CharacterShapeType.Capsule,
            new Vector3(0f, -0.38f, 0f), new Vector3(0.115f, 0.76f, 0.115f), C_Body);

        Add(rig, "foreL", CharacterBone.ArmL, CharacterShapeType.Capsule,
            new Vector3(0f, -0.70f, 0.02f), new Vector3(0.135f, 0.34f, 0.135f), C_Plate);
        Add(rig, "foreR", CharacterBone.ArmR, CharacterShapeType.Capsule,
            new Vector3(0f, -0.70f, 0.02f), new Vector3(0.135f, 0.34f, 0.135f), C_Plate);

        // 손톱
        Add(rig, "clawLA", CharacterBone.ArmL, CharacterShapeType.Cube,
            new Vector3(-0.03f, -0.87f, 0.07f), new Vector3(0.028f, 0.15f, 0.028f), C_Bone,
            euler: new Vector3(25f, 0f, -12f));
        Add(rig, "clawLB", CharacterBone.ArmL, CharacterShapeType.Cube,
            new Vector3(0.03f, -0.87f, 0.07f), new Vector3(0.028f, 0.15f, 0.028f), C_Bone,
            euler: new Vector3(25f, 0f, 12f));
        Add(rig, "clawRA", CharacterBone.ArmR, CharacterShapeType.Cube,
            new Vector3(-0.03f, -0.87f, 0.07f), new Vector3(0.028f, 0.15f, 0.028f), C_Bone,
            euler: new Vector3(25f, 0f, -12f));
        Add(rig, "clawRB", CharacterBone.ArmR, CharacterShapeType.Cube,
            new Vector3(0.03f, -0.87f, 0.07f), new Vector3(0.028f, 0.15f, 0.028f), C_Bone,
            euler: new Vector3(25f, 0f, 12f));

        // ─── 다리 ─────────────────────────────────────────────────
        Add(rig, "legL", CharacterBone.LegL, CharacterShapeType.Capsule,
            new Vector3(0f, -0.44f, 0f), new Vector3(0.145f, 0.88f, 0.145f), C_Body);
        Add(rig, "legR", CharacterBone.LegR, CharacterShapeType.Capsule,
            new Vector3(0f, -0.44f, 0f), new Vector3(0.145f, 0.88f, 0.145f), C_Body);

        Add(rig, "footL", CharacterBone.LegL, CharacterShapeType.Cube,
            new Vector3(0f, -0.94f, 0.04f), new Vector3(0.17f, 0.12f, 0.28f), C_Plate);
        Add(rig, "footR", CharacterBone.LegR, CharacterShapeType.Cube,
            new Vector3(0f, -0.94f, 0.04f), new Vector3(0.17f, 0.12f, 0.28f), C_Plate);

        return rig;
    }

    // ─────────────────────────────────────────────────────────────
    /// <summary>프리셋 종류에 맞는 리그를 만든다.</summary>
    public static CharacterRig Create(CharacterVisual.Preset preset)
    {
        switch (preset)
        {
            case CharacterVisual.Preset.Player: return CreatePlayer();
            case CharacterVisual.Preset.Chaser: return CreateChaser();
            default: return null;
        }
    }

    // ─── 파츠 추가 헬퍼 ─────────────────────────────────────────
    private static void Add(CharacterRig rig, string name, CharacterBone bone, CharacterShapeType type,
                            Vector3 position, Vector3 size, Color color,
                            Vector3 euler = default, float smoothness = 0.2f,
                            bool unlit = false, bool isEye = false)
    {
        CharacterShape shape = new CharacterShape
        {
            name       = name,
            bone       = bone,
            type       = type,
            position   = position,
            euler      = euler,
            size       = size,
            color      = color,
            smoothness = smoothness,
            unlit      = unlit,
            isEye      = isEye,
        };
        rig.shapes.Add(shape);
    }
}
