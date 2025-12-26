using UnityEngine;
using DG.Tweening;
using Unity.Cinemachine;

public class LabToFridgeBridgeManager : MonoBehaviour
{
    [Header("1. Rope (밧줄)")]
    public Transform ropeObj;
    public ParticleSystem acidSmokeEffect;

    [Header("2. Card (카드)")]
    public Rigidbody cardRb;
    public Transform cardSpawnPoint;

    [Header("3. Book Bridge (책 다리)")]
    public Transform bookPivot;
    public Transform bookObj;
    public Collider bookWalkableCol; // 다리 바닥 판정용 (Ground)
    public Vector3 bookFallRotation = new Vector3(0, 0, 90);
    public float fallDuration = 0.8f;
    public Ease fallEase = Ease.OutBounce;

    [Header("4. NavMesh & Camera Trigger (중요)")]
    public GameObject bridgeNavMeshLink; // 다리 길 연결 (NavMeshLink)

    // ▼▼▼ [추가] 다리가 놓이면 켜질 카메라 트리거 영역 ▼▼▼
    [Tooltip("BridgeCameraTrigger가 붙은 오브젝트. 다리가 생기면 켜줍니다.")]
    public GameObject bridgeCameraTriggerObj;
    // ▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲

    [Header("Settings")]
    public float noiseAmount = 0.3f;

    public void PlaySequence()
    {
        // 1. 산성 연기
        if (acidSmokeEffect) acidSmokeEffect.Play();

        // 2. 밧줄 끊어짐
        if (ropeObj)
        {
            ropeObj.DOScale(Vector3.zero, 0.5f).SetEase(Ease.InBack)
                .OnComplete(() => ropeObj.gameObject.SetActive(false));
        }

        // 3. 카드 떨어짐
        if (cardRb)
        {
            cardRb.isKinematic = false;
            cardRb.AddForce(Vector3.right * 0.5f, ForceMode.Impulse);
        }

        // 4. 책 넘어짐 (다리 생성)
        if (bookPivot)
        {
            DOVirtual.DelayedCall(0.5f, () => {
                bookPivot.DOLocalRotate(bookFallRotation, fallDuration)
                    .SetEase(fallEase)
                    .OnComplete(OnBridgeLanded);
            });
        }
    }

    // 책이 바닥에 닿았을 때 후처리
    void OnBridgeLanded()
    {
        // 1. 쿵! 소음 발생
        if (NoiseSystem.Instance) NoiseSystem.Instance.FireImpulse(noiseAmount);

        // 2. 플레이어가 밟고 지나갈 수 있게 콜라이더/링크 활성화
        if (bookWalkableCol) bookWalkableCol.enabled = true;
        if (bridgeNavMeshLink) bridgeNavMeshLink.SetActive(true);

        // ▼▼▼ [추가] 카메라 트리거 활성화! ▼▼▼
        if (bridgeCameraTriggerObj) bridgeCameraTriggerObj.SetActive(true);
        // ▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲

        Debug.Log("[Bridge] 책 다리 & 카메라 트리거 연결 완료!");
    }
}