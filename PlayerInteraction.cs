using UnityEngine;

/// <summary>
/// E키로 한정선을 획득한다.
/// HanJeongSeon 태그를 가진 오브젝트를 감지하고 상호작용한다.
/// </summary>
public class PlayerInteraction : MonoBehaviour
{
    [Header("Interaction Settings")]
    [SerializeField] private float interactRange = 2f;
    [SerializeField] private LayerMask interactLayer;

    // 한정선 보유 여부
    public bool HasHanJeongSeon { get; private set; }

    private void Update()
    {
        if (GameManager.Instance != null && !GameManager.Instance.IsPlaying)
            return;

        if (Input.GetKeyDown(KeyCode.E))
            TryInteract();
    }

    private void TryInteract()
    {
        // 이미 획득한 경우 중복 획득 방지
        if (HasHanJeongSeon) return;

        // 반경 내 HanJeongSeon 오브젝트 탐색
        Collider[] hits = Physics.OverlapSphere(transform.position, interactRange, interactLayer);

        float   closestDist = float.MaxValue;
        HanJeongSeonItem closestItem = null;

        foreach (Collider hit in hits)
        {
            HanJeongSeonItem item = hit.GetComponent<HanJeongSeonItem>();
            if (item == null || item.IsCollected) continue;

            float dist = Vector3.Distance(transform.position, hit.transform.position);
            if (dist < closestDist)
            {
                closestDist  = dist;
                closestItem  = item;
            }
        }

        if (closestItem != null)
            CollectItem(closestItem);
    }

    private void CollectItem(HanJeongSeonItem item)
    {
        item.Collect();
        HasHanJeongSeon = true;

        // 점수 매니저에 보고
        ScoreManager.Instance?.AddHanJeongSeonBonus();

        // 게임 매니저에 추격 시작 이벤트
        GameManager.Instance?.OnHanJeongSeonCollected();

        Debug.Log("[PlayerInteraction] 한정선 획득!");
    }

    /// <summary>HUD에서 범위 표시용 Gizmos</summary>
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactRange);
    }
}
