using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 유니티 기본 프리미티브로 파츠를 만들고, 재질 캐시까지 관리하는 헬퍼.
////
/// 주의: GameObject.CreatePrimitive는 콜라이더를 함께 만들기 때문에
/// 반드시 제거해야 한다. 남아 있으면 CharacterController / NavMeshAgent / 물리가 깨진다.
/// </summary>
public static class PrimitiveBuilder
{
    // 색상 + 표면 상태가 같으면 재질을 공유한다 (추격자 20명이 같은 재질을 쓰므로 중요)
    private static readonly Dictionary<string, Material> materialCache = new Dictionary<string, Material>();

    /// <summary>파츠 하나를 생성하고 지정한 관절 아래에 붙인다.</summary>
    public static GameObject CreateShape(CharacterShape shape, Transform parent, int layer)
    {
        GameObject go = GameObject.CreatePrimitive(ToPrimitiveType(shape.type));
        go.name = shape.name;

        // ─── 콜라이더 제거 ───────────────────────────────────────
        // 프리미티브가 붙는 순간 물리 충돌이 생기면 CharacterController/NavMesh를 방해한다.
        Collider col = go.GetComponent<Collider>();
        if (col != null)
        {
            col.enabled = false;      // 같은 프레임에 물리 계산에 참여하지 않도록 먼저 끈다
            Object.Destroy(col);
        }

        go.layer = layer;

        Transform t = go.transform;
        t.SetParent(parent, false);
        t.localPosition = shape.position;
        t.localRotation = Quaternion.Euler(shape.euler);
        t.localScale = ToLocalScale(shape.type, shape.size);

        Renderer renderer = go.GetComponent<Renderer>();
        if (renderer != null)
        {
            Material mat = GetMaterial(shape.color, shape.smoothness, shape.unlit);
            if (mat != null) renderer.sharedMaterial = mat;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            renderer.receiveShadows = true;
        }

        return go;
    }

    /// <summary>프리미티브 종류 변환</summary>
    public static PrimitiveType ToPrimitiveType(CharacterShapeType type)
    {
        switch (type)
        {
            case CharacterShapeType.Capsule:  return PrimitiveType.Capsule;
            case CharacterShapeType.Sphere:   return PrimitiveType.Sphere;
            case CharacterShapeType.Cylinder: return PrimitiveType.Cylinder;
            default:                          return PrimitiveType.Cube;
        }
    }

    /// <summary>
    /// 최종 실치수(m) → 유니티 프리미티브의 로컬 스케일로 변환.
    /// 유니티 캡슐/실린더는 높이 2, 반지름 0.5인 단위 프리미티브라는 점을 반영한다.
    /// </summary>
    public static Vector3 ToLocalScale(CharacterShapeType type, Vector3 size)
    {
        switch (type)
        {
            case CharacterShapeType.Capsule:
            case CharacterShapeType.Cylinder:
                return new Vector3(size.x, size.y * 0.5f, size.z);
            default:
                // 큐브(1단위) / 스피어(지름 1단위)
                return size;
        }
    }

    /// <summary>재질을 얻는다. 같은 조합이면 캐시된 재질을 재사용한다.</summary>
    public static Material GetMaterial(Color color, float smoothness, bool unlit)
    {
        string key = string.Format("{0:F3}_{1:F3}_{2:F3}_{3:F2}_{4}", color.r, color.g, color.b, smoothness, unlit);

        if (materialCache.TryGetValue(key, out Material cached) && cached != null)
            return cached;

        Shader shader = ResolveShader(unlit);
        if (shader == null) return null;

        Material mat = new Material(shader);
        mat.name = "CharMat_" + key;

        // URP는 _BaseColor / _Smoothness, 레거시 Standard는 _Color / _Glossiness를 쓴다.
        if (mat.HasProperty("_BaseColor"))  mat.SetColor("_BaseColor", color);
        if (mat.HasProperty("_Color"))      mat.SetColor("_Color", color);
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
        if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", smoothness);
        if (mat.HasProperty("_Metallic"))   mat.SetFloat("_Metallic", 0f);

        mat.enableInstancing = true;

        materialCache[key] = mat;
        return mat;
    }

    /// <summary>
    /// 렌더 파이프라인에 맞는 셰이더를 찾는다.
    /// 빌드에서는 사용하지 않은 셰이더가 스트립될 수 있으므로 후보를 여러 개 둔다.
    /// </summary>
    private static Shader ResolveShader(bool unlit)
    {
        string[] candidates = unlit
            ? new[] { "Universal Render Pipeline/Unlit", "Unlit/Color", "Sprites/Default", "Standard" }
            : new[] { "Universal Render Pipeline/Lit", "Standard", "Mobile/Diffuse" };

        for (int i = 0; i < candidates.Length; i++)
        {
            Shader s = Shader.Find(candidates[i]);
            if (s != null) return s;
        }
        return null;
    }

    /// <summary>캐시된 재질을 전부 정리한다 (씬 전환으로 메모리를 반환하고 싶을 때).</summary>
    public static void ClearMaterialCache()
    {
        materialCache.Clear();
    }
}
