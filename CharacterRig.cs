using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 캐릭터의 뼈(관절) 슬롯.
/// 파츠는 이 슬롯 중 하나에 붙으며, 애니메이션은 슬롯(관절)을 회전시킨다.
/// </summary>
public enum CharacterBone
{
    Hips,     // 골반
    Torso,    // 몸통
    Head,     // 머리
    ArmL,     // 왼팔
    ArmR,     // 오른팔
    LegL,     // 왼다리
    LegR,     // 오른다리
}

/// <summary>사용할 수 있는 기본 프리미티브 종류</summary>
public enum CharacterShapeType
{
    Cube,
    Capsule,
    Sphere,
    Cylinder
}

/// <summary>
/// 캐릭터 파츠 하나.
/// 프리미티브(큐브/캡슐/구/실린더)를 조합해 저폴리 캐릭터를 만든다.
/// </summary>
[Serializable]
public class CharacterShape
{
    [Tooltip("Hierarchy에서 파츠를 구분하는 이름")]
    public string name = "part";

    [Tooltip("어느 관절에 붙는지")]
    public CharacterBone bone = CharacterBone.Torso;

    [Tooltip("프리미티브 종류")]
    public CharacterShapeType type = CharacterShapeType.Capsule;

    [Tooltip("관절 기준 로컬 위치 (m)")]
    public Vector3 position = Vector3.zero;

    [Tooltip("관절 기준 로컬 회전 (도)")]
    public Vector3 euler = Vector3.zero;

    [Tooltip("최종 실치수 (m). 프리미티브 기본 크기에 맞춰 변환되므로 이 값이 그대로 치수다")]
    public Vector3 size = new Vector3(0.12f, 0.4f, 0.12f);

    [Tooltip("색상")]
    public Color color = Color.white;

    [Range(0f, 1f)]
    [Tooltip("표면 매끄러움 (0 = 무광, 1 = 반짝임)")]
    public float smoothness = 0.2f;

    [Tooltip("true면 발광처럼 보이는 언lit 재질 (눈, 발광 부위)")]
    public bool unlit = false;

    [Tooltip("true면 눈으로 처리되어 깜빡임 대상이 된다")]
    public bool isEye = false;
}

/// <summary>
/// 캐릭터의 골격 치수.
/// 관절(본) 위치는 여기서 계산된다.
/// </summary>
[Serializable]
public class CharacterSkeleton
{
    [Tooltip("발바닥 → 골반 높이")]
    public float hipHeight = 0.9f;

    [Tooltip("골반 → 어깨 높이")]
    public float torsoLength = 0.55f;

    [Tooltip("어깨 → 머리 중심 거리")]
    public float neckLength = 0.1f;

    [Tooltip("어깨 폭 (몸통 중심에서 끝까지)")]
    public float shoulderWidth = 0.21f;

    [Tooltip("어깨가 몸통 중심보다 아래로 내려간 거리 (구부린 자세 표현)")]
    public float shoulderDrop = 0f;

    [Tooltip("골반 폭 (몸통 중심에서 끝까지)")]
    public float hipWidth = 0.1f;
}

/// <summary>표정 / 시선 관련 설정</summary>
[Serializable]
public class CharacterLook
{
    [Tooltip("눈 깜빡임 사용")]
    public bool blinkEnabled = true;

    [Tooltip("깜빡임 최소 간격 (초)")]
    public float blinkIntervalMin = 2.0f;

    [Tooltip("깜빡임 최대 간격 (초)")]
    public float blinkIntervalMax = 5.5f;

    [Tooltip("눈이 감겨 있는 시간 (초)")]
    public float blinkDuration = 0.12f;

    [Tooltip("고개를 특정 대상 쪽으로 돌린다 (추격자는 플레이어를 노려본다)")]
    public bool headLookAt = false;

    [Tooltip("고개 좌우 회전 한계 (도)")]
    public float headTurnLimit = 65f;

    [Tooltip("고개 상하 회전 한계 (도)")]
    public float headPitchLimit = 25f;

    [Tooltip("호흡에 의한 몸통 변화량 (0.02 ≈ 2%)")]
    public float breathAmount = 0.02f;

    [Tooltip("호흡 속도 (Hz)")]
    public float breathSpeed = 1.5f;
}

/// <summary>보행 애니메이션 설정</summary>
[Serializable]
public class CharacterLocomotion
{
    [Tooltip("이동 속도 1m당 걸음 사이클 수")]
    public float strideFrequency = 1.1f;

    [Tooltip("걷기 시 다리/팔 진폭 (도)")]
    public float walkSwing = 22f;

    [Tooltip("달리기 시 다리/팔 진폭 (도)")]
    public float runSwing = 42f;

    [Tooltip("팔 진폭 배수 (다리 대비)")]
    public float armSwingScale = 0.75f;

    [Tooltip("걸음 박자에 따른 상하 보빙 크기 (m)")]
    public float bobAmount = 0.035f;

    [Tooltip("달릴 때 몸을 앞으로 숙이는 각도 (도)")]
    public float runLean = 9f;

    [Tooltip("이 속도에서 최대 진폭 (m/s)")]
    public float referenceSpeed = 5.5f;
}

/// <summary>
/// 캐릭터 전체 정의.
/// 관절 치수 + 파츠 목록 + 표정/보행 설정.
/// </summary>
[Serializable]
public class CharacterRig
{
    [Tooltip("표시용 이름")]
    public string displayName = "Character";

    public CharacterSkeleton skeleton = new CharacterSkeleton();
    public CharacterLook look = new CharacterLook();
    public CharacterLocomotion locomotion = new CharacterLocomotion();

    [Tooltip("캐릭터를 구성하는 파츠 목록")]
    public List<CharacterShape> shapes = new List<CharacterShape>();
}
