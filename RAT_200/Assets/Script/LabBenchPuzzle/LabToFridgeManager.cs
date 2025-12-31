using UnityEngine;
using DG.Tweening;
using UnityEngine.AI;
using System.Collections;

public class LabToFridgeManager : MonoBehaviour
{
    [Header("1. Rope & Card")]
    public Transform ropeObj;
    public ParticleSystem acidSmokeEffect;
    public Rigidbody cardRb;

    [Header("2. Book Bridge")]
    public Transform bookPivot;
    public Rigidbody bookRb;
    public Collider bookWalkableCol; // 밟는 용도

    [Header("Bridge Animation")]
    public Vector3 bookFallRotation = new Vector3(0, 0, 90);

    [Header("3. Navigation")]
    public NavMeshObstacle bridgeBlocker;

    [Header("4. Camera & Event")]
    public GameObject bridgeCameraTriggerObj;
    public GameObject collapseTriggerObj;

    [Header("Settings")]
    public float noiseAmount = 0.3f;
    public float collapseNoiseAmount = 1.0f;

    [Header("Fall Physics")]
    public float playerFallDrag = 5.0f;

    private bool _isCollapsing = false;

    [Header("Fuse Single")]
    public GameObject fuse;

    void Start()
    {
        if (bridgeBlocker) bridgeBlocker.enabled = true;
        if (bookWalkableCol) bookWalkableCol.enabled = false;
        if (collapseTriggerObj) collapseTriggerObj.SetActive(false);
        if (bridgeCameraTriggerObj) bridgeCameraTriggerObj.SetActive(false);

        if (bookRb) bookRb.isKinematic = true;
    }

    public void PlaySequence()
    {
        if (acidSmokeEffect) acidSmokeEffect.Play();
        if (ropeObj)
        {
            ropeObj.DOScale(Vector3.zero, 0.5f).SetEase(Ease.InBack)
                .OnComplete(() => ropeObj.gameObject.SetActive(false));
        }
        if (cardRb)
        {
            cardRb.isKinematic = false;
            cardRb.AddForce(Vector3.right * 0.5f, ForceMode.Impulse);
        }
        if (bookPivot)
        {
            DOVirtual.DelayedCall(0.5f, () => {
                bookPivot.DOLocalRotate(bookFallRotation, 0.8f)
                    .SetRelative(true).SetEase(Ease.OutBounce)
                    .OnComplete(OnBridgeLanded);
            });
        }
    }

    void OnBridgeLanded()
    {
        if (NoiseSystem.Instance) NoiseSystem.Instance.FireImpulse(noiseAmount);
        if (bridgeBlocker) bridgeBlocker.enabled = false;
        if (bookWalkableCol) bookWalkableCol.enabled = true;
        if (bridgeCameraTriggerObj) bridgeCameraTriggerObj.SetActive(true);
    }

    public void EnableCollapseTrigger()
    {
        if (collapseTriggerObj) collapseTriggerObj.SetActive(true);
    }

    public void CollapseBridge()
    {
        if (_isCollapsing) return;
        _isCollapsing = true;

        StartCoroutine(Routine_CollapseSequence());
    }

    IEnumerator Routine_CollapseSequence()
    {
        Debug.Log("[Bridge] 붕괴 시작 - 쥐 포획!");

        // 1. ★ [핵심] 딜레이 갖기 전에 쥐부터 멈춰 세웁니다!
        // 이걸 안 하면 흔들리는 동안 도망가버립니다.
        FreezePlayerOnBridge();

        // 2. 전조 증상: 덜덜 떨림 (이제 쥐는 꼼짝없이 같이 떨어야 함)
        if (bookPivot) bookPivot.DOShakeRotation(0.5f, 5f, 20);

        // 3. 공포감을 주는 짧은 대기 시간 (0.3초)
        yield return new WaitForSeconds(0.3f);

        // 4. 소음 & 길 막기
        if (NoiseSystem.Instance) NoiseSystem.Instance.FireImpulse(collapseNoiseAmount);
        if (bridgeBlocker) bridgeBlocker.enabled = true;

        // 5. 책 유령 모드 (Collider 끄기)
        if (bookPivot)
        {
            var allBookColliders = bookPivot.GetComponentsInChildren<Collider>();
            foreach (var col in allBookColliders) col.enabled = false;
        }

        // 6. 책 추락 (물리 켜기)
        if (bookRb)
        {
            bookPivot.DOKill();
            bookRb.isKinematic = false;
            bookRb.useGravity = true;
            bookRb.detectCollisions = false; // 충돌 무시 (확실히 빠지게)

            bookRb.AddTorque(Vector3.forward * 10f, ForceMode.Impulse);
            bookRb.AddForce(Vector3.down * 5f, ForceMode.Impulse);
        }

        // 7. ★ 이제 묶어뒀던 쥐를 물리 엔진으로 전환해서 떨어뜨림
        // (책이 먼저 살짝 빠지는 느낌을 위해 0.05초만 더 줌)
        yield return new WaitForSeconds(0.05f);
        DropPlayer();

        // 8. 감상 및 정리
        yield return new WaitForSeconds(2.0f);
        if (bookPivot) Destroy(bookPivot.gameObject);
        if (bridgeCameraTriggerObj) bridgeCameraTriggerObj.SetActive(false);
        if (collapseTriggerObj) collapseTriggerObj.SetActive(false);

        RecoverPlayer();
    }

    // ★ [신규 함수] 쥐를 그 자리에 얼음! 시키는 함수
    void FreezePlayerOnBridge()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player)
        {
            // 1. 이동 입력 차단 (클릭 무시)
            var inputEvents = player.GetComponent<ClickMoveOrInteract_Events>();
            if (inputEvents) inputEvents.enabled = false;

            // 2. NavMeshAgent 즉시 정지
            var agent = player.GetComponent<NavMeshAgent>();
            if (agent)
            {
                // 가던 길 취소하고 제자리에 멈춤
                if (agent.isOnNavMesh) agent.ResetPath();
                agent.velocity = Vector3.zero;
                agent.isStopped = true;
                agent.enabled = false; // 아예 꺼버림
            }

            // 3. (선택) 쥐가 "어?" 하고 놀라는 애니메이션이 있다면 여기서 실행
            // var animator = player.GetComponentInChildren<Animator>();
            // if(animator) animator.SetTrigger("Surprise");
        }
    }

    // (기존 DropPlayer 함수는 그대로 사용하되, NavMesh 끄는 부분은 중복되어도 상관없음)
    void DropPlayer()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player)
        {
            // Rigidbody 켜서 물리 낙하 시작
            var rb = player.GetComponent<Rigidbody>();
            if (rb)
            {
                rb.isKinematic = false;
                rb.useGravity = true;
                rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
                rb.linearDamping = playerFallDrag;

                // 정지 상태에서 바로 아래로 밉니다
                rb.linearVelocity = Vector3.zero;
                rb.AddForce(Vector3.down * 2f, ForceMode.Impulse);
            }

            var col = player.GetComponent<Collider>();
            if (col)
            {
                col.enabled = true;
                col.isTrigger = false;
            }

            fuse.gameObject.GetComponent<DrawerItemDispenser>().Dispense();
            fuse.gameObject.GetComponent<InteractionBlocker>().Unlock();
        }
    }
    void RecoverPlayer()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player)
        {
            

            Debug.Log("[Bridge] 플레이어 복구 시작...");

            // 1. 물리 끄기 (이제 NavMesh가 다시 조종해야 함)
            var rb = player.GetComponent<Rigidbody>();
            if (rb)
            {
                rb.isKinematic = true; // 다시 고정
                rb.useGravity = false; // 중력 끄기
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            // 2. 바닥 위치 보정 (NavMesh 위로 안착)
            var agent = player.GetComponent<NavMeshAgent>();
            if (agent)
            {
                agent.enabled = true; // 켜기
                // 현재 물리 위치(transform.position)를 NavMesh 상의 위치로 강제 동기화
                if (NavMesh.SamplePosition(player.transform.position, out NavMeshHit hit, 2.0f, NavMesh.AllAreas))
                {
                    agent.Warp(hit.position);
                }
                agent.updatePosition = true;
                agent.updateRotation = true;
                agent.isStopped = false;
            }

            // 3. 입력 스크립트 다시 켜기 (이제 클릭하면 움직임!)
            var inputEvents = player.GetComponent<ClickMoveOrInteract_Events>();
            if (inputEvents) inputEvents.enabled = true;

            // 4. Trigger 복구 (원래 Trigger였다면 복구, 아니면 그대로 둠)
            // 보통 이동 중에는 충돌체여야 하므로 isTrigger=false 유지 추천
            // 만약 상호작용을 위해 Trigger여야만 한다면 여기서 true로 바꾸세요.
        }
    }
}