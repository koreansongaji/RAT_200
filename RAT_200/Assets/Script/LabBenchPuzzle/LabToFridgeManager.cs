using System;
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
    public Collider bookWalkableCol; // ��� �뵵

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

    [Header("Sounds Clips")]
    [SerializeField] private AudioClip _fallSound;

    private void Awake()
    {
        if(_fallSound == null) _fallSound = Resources.Load<AudioClip>("Sounds/Effect/Trap/book_fall");
    }

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
        Debug.Log("[Bridge] �ر� ���� - �� ��ȹ!");

        // 1. �� [�ٽ�] ����� ���� ���� ����� ���� ����ϴ�!
        // �̰� �� �ϸ� ��鸮�� ���� �����������ϴ�.
        FreezePlayerOnBridge();

        // 2. ���� ����: ���� ���� (���� ��� ��¦���� ���� ����� ��)
        if (bookPivot) bookPivot.DOShakeRotation(0.5f, 5f, 20);

        // 3. �������� �ִ� ª�� ��� �ð� (0.3��)
        yield return new WaitForSeconds(0.3f);

        // 4. ���� & �� ����
        if (NoiseSystem.Instance) NoiseSystem.Instance.FireImpulse(collapseNoiseAmount);
        if (bridgeBlocker) bridgeBlocker.enabled = true;

        // 5. å ���� ��� (Collider ���)
        if (bookPivot)
        {
            var allBookColliders = bookPivot.GetComponentsInChildren<Collider>();
            foreach (var col in allBookColliders) col.enabled = false;
        }

        // 6. å �߶� (���� �ѱ�)
        if (bookRb)
        {
            bookPivot.DOKill();
            bookRb.isKinematic = false;
            bookRb.useGravity = true;
            bookRb.detectCollisions = false; // �浹 ���� (Ȯ���� ������)

            bookRb.AddTorque(Vector3.forward * 10f, ForceMode.Impulse);
            bookRb.AddForce(Vector3.down * 5f, ForceMode.Impulse);
        }

        // 7. �� ���� ����״� �㸦 ���� �������� ��ȯ�ؼ� ����߸�
        // (å�� ���� ��¦ ������ ������ ���� 0.05�ʸ� �� ��)
        yield return new WaitForSeconds(0.05f);
        DropPlayer();

        // 8. ���� �� ����
        yield return new WaitForSeconds(2.0f);
        if (bookPivot) Destroy(bookPivot.gameObject);
        if (bridgeCameraTriggerObj) bridgeCameraTriggerObj.SetActive(false);
        if (collapseTriggerObj) collapseTriggerObj.SetActive(false);

        RecoverPlayer();
    }

    // �� [�ű� �Լ�] �㸦 �� �ڸ��� ����! ��Ű�� �Լ�
    void FreezePlayerOnBridge()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player)
        {
            // 1. �̵� �Է� ���� (Ŭ�� ����)
            var inputEvents = player.GetComponent<ClickMoveOrInteract_Events>();
            if (inputEvents) inputEvents.enabled = false;

            // 2. NavMeshAgent ��� ����
            var agent = player.GetComponent<NavMeshAgent>();
            if (agent)
            {
                // ���� �� ����ϰ� ���ڸ��� ����
                if (agent.isOnNavMesh) agent.ResetPath();
                agent.velocity = Vector3.zero;
                agent.isStopped = true;
                agent.enabled = false; // �ƿ� ������
            }

            // 3. (����) �㰡 "��?" �ϰ� ���� �ִϸ��̼��� �ִٸ� ���⼭ ����
            // var animator = player.GetComponentInChildren<Animator>();
            // if(animator) animator.SetTrigger("Surprise");
        }
    }

    // (���� DropPlayer �Լ��� �״�� ����ϵ�, NavMesh ��� �κ��� �ߺ��Ǿ �������)
    void DropPlayer()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player)
        {
            // Rigidbody �Ѽ� ���� ���� ����
            var rb = player.GetComponent<Rigidbody>();
            if (rb)
            {
                rb.isKinematic = false;
                rb.useGravity = true;
                rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
                rb.linearDamping = playerFallDrag;

                // ���� ���¿��� �ٷ� �Ʒ��� �Ӵϴ�
                rb.linearVelocity = Vector3.zero;
                rb.AddForce(Vector3.down * 2f, ForceMode.Impulse);
            }

            var col = player.GetComponent<Collider>();
            if (col)
            {
                col.enabled = true;
                col.isTrigger = false;
            }

            AudioManager.Instance.Play(_fallSound);
            
            fuse.gameObject.GetComponent<DrawerItemDispenser>().Dispense();
            fuse.gameObject.GetComponent<InteractionBlocker>().Unlock();
        }
    }
    void RecoverPlayer()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player)
        {
            

            Debug.Log("[Bridge] �÷��̾� ���� ����...");

            // 1. ���� ��� (���� NavMesh�� �ٽ� �����ؾ� ��)
            var rb = player.GetComponent<Rigidbody>();
            if (rb)
            {
                rb.isKinematic = true; // �ٽ� ����
                rb.useGravity = false; // �߷� ���
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            // 2. �ٴ� ��ġ ���� (NavMesh ���� ����)
            var agent = player.GetComponent<NavMeshAgent>();
            if (agent)
            {
                agent.enabled = true; // �ѱ�
                // ���� ���� ��ġ(transform.position)�� NavMesh ���� ��ġ�� ���� ����ȭ
                if (NavMesh.SamplePosition(player.transform.position, out NavMeshHit hit, 2.0f, NavMesh.AllAreas))
                {
                    agent.Warp(hit.position);
                }
                agent.updatePosition = true;
                agent.updateRotation = true;
                agent.isStopped = false;
            }

            // 3. �Է� ��ũ��Ʈ �ٽ� �ѱ� (���� Ŭ���ϸ� ������!)
            var inputEvents = player.GetComponent<ClickMoveOrInteract_Events>();
            if (inputEvents) inputEvents.enabled = true;

            // 4. Trigger ���� (���� Trigger���ٸ� ����, �ƴϸ� �״�� ��)
            // ���� �̵� �߿��� �浹ü���� �ϹǷ� isTrigger=false ���� ��õ
            // ���� ��ȣ�ۿ��� ���� Trigger���߸� �Ѵٸ� ���⼭ true�� �ٲټ���.
        }
    }
}