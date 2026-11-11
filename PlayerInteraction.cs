using UnityEngine;

/// <summary>
/// E키로 한정선을 획득한다.
/// interactLayer 레이어에 포함된 오브젝트를 감지하고 상호작용한다.
/// </summary>
public class PlayerInteraction : MonoBehaviour
{
    [Header("Interaction Settings")]
    [SerializeField] private float interactRange = 2f;
    [SerializeField] private LayerMask interactLayer;

    // 한정선 보유 여부
    public bool HasHanJeongSeon { get; private set; }

    private void Start()
    {
        // 씬 재로드 후 초기화 보장
        HasHanJeongSeon = false;
    }

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

        // 반경 내 한정선 오브젝트 탐색
        Collider[] hits = Physics.OverlapSphere(transform.position, interactRange, interactLayer);

        float   closestDist = float.MaxValue;
        HanJeongSeonItem closestItem = null;

        foreach (Collider hit in hits)
        {
            // 콜라이더가 자식 오브젝트에 있을 수 있으므로 부모까지 탐색한다.
            HanJeongSeonItem item = hit.GetComponentInParent<HanJeongSeonItem>();
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
