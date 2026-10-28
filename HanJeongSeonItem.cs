using System.Collections;
using UnityEngine;

/// <summary>
/// 맵에 배치된 한정선 아이템 오브젝트.
/// PlayerInteraction이 Collect()를 호출하면 획득 처리된다.
/// </summary>
public class HanJeongSeonItem : MonoBehaviour
{
    [Header("Visual Feedback")]
    [SerializeField] private ParticleSystem collectParticle;
    [SerializeField] private float bobSpeed     = 1.5f;   // 상하 부유 속도
    [SerializeField] private float bobAmplitude = 0.2f;   // 부유 높이
    [SerializeField] private float rotateSpeed  = 60f;    // 초당 회전 각도

    [Header("Interaction Hint")]
    [SerializeField] private GameObject interactHint;     // "E - 획득" 안내 UI

    public bool IsCollected { get; private set; }

    private Vector3 startPos;
    private Renderer[] renderers;

    private void Awake()
    {
        startPos  = transform.position;
        renderers = GetComponentsInChildren<Renderer>();
    }

    private void Update()
    {
        if (IsCollected) return;

        // 상하 부유 애니메이션
        float newY = startPos.y + Mathf.Sin(Time.time * bobSpeed) * bobAmplitude;
        transform.position = new Vector3(startPos.x, newY, startPos.z);

        // 자전
        transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime);
    }

    /// <summary>플레이어와의 거리에 따라 안내 UI 표시/숨김</summary>
    public void SetHintVisible(bool visible)
    {
        if (interactHint != null) interactHint.SetActive(visible && !IsCollected);
    }

    /// <summary>아이템 획득 처리</summary>
    public void Collect()
    {
        if (IsCollected) return;
        IsCollected = true;

        // 파티클 재생
        if (collectParticle != null)
        {
            collectParticle.transform.parent = null;  // 월드에 남겨 재생
            collectParticle.Play();
        }

        // 안내 UI 숨김
        if (interactHint != null) interactHint.SetActive(false);

        // 오브젝트 숨기고 짧은 딜레이 후 제거
        foreach (Renderer r in renderers) r.enabled = false;
        StartCoroutine(DestroyAfterDelay(2f));
    }

    private IEnumerator DestroyAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, 0.5f);
    }
}
