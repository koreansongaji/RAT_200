using System;
using UnityEngine;

[RequireComponent(typeof(LadderPlacementController))]
public class Ladder : MonoBehaviour
{

    [SerializeField] private LadderSoundController _ladderSoundController;

    private void Awake()
    {
        if(_ladderSoundController == null) _ladderSoundController = GetComponent<LadderSoundController>();
        if(_ladderSoundController == null) Debug.LogError("No LadderSoundController found on " + gameObject.name);
    }
    [Header("Data")]
    [Range(0, 4)] public int lengthLevel = 0; // �� 0���� ����!
    public LadderPlaceSpot currentSpot;


    [Header("Visuals")]
    [Tooltip("���� 1, 2, 3, 4�� �� �� ������� ���� ���δ� ������Ʈ�� (�� 4�� ����)")]
    public GameObject[] extraRungs;

    void Start()
    {
        // ���� �� ���� 0 ����(�� ����)�� �ʱ�ȭ
        UpdateVisuals();
    }

    // ���δ� ������(RungPickup)�� ȣ���ϴ� �Լ�
    public void AddRung()
    {
        if (lengthLevel < 4) // �ִ� ���� 4 ����
        {
            _ladderSoundController.PlayFixLadder();
            lengthLevel++;
            UpdateVisuals();
            Debug.Log($"[Ladder] Level Up! Current Level: {lengthLevel}");
        }
    }

    void UpdateVisuals()
    {
        // extraRungs[0] : ���� 1 �̻��� �� ����
        // extraRungs[1] : ���� 2 �̻��� �� ���� ...
        for (int i = 0; i < extraRungs.Length; i++)
        {
            if (extraRungs[i] != null)
            {
                // �� ���� ����: (i + 1) �������� ����
                bool shouldActive = lengthLevel >= (i + 1);
                extraRungs[i].SetActive(shouldActive);
            }
        }
    }

    // ... (AttachTo, Detach �� ���� �ڵ�� �״�� ����) ...
    public void AttachTo(LadderPlaceSpot spot, bool alignRotation = true)
    {
        if (!spot || !spot.ladderAnchor) return;

        if (currentSpot) currentSpot.occupied = false;

        transform.position = spot.ladderAnchor.position;
        if (alignRotation) transform.rotation = spot.ladderAnchor.rotation;
        currentSpot = spot;
        spot.occupied = true;

        // ������ Ÿ�� ����
        var climb = GetComponent<LadderClimbInteractable>();
        if (climb && spot.climbTarget) climb.target = spot.climbTarget;
        _ladderSoundController.PlayPlaceLadder();
    }

    public void Detach()
    {
        if (currentSpot) currentSpot.occupied = false;
        currentSpot = null;

        // ������ ��Ȱ��ȭ(����)
        var climb = GetComponent<LadderClimbInteractable>();
        if (climb) climb.target = null;
        //_ladderSoundController.PlayPlaceLadder();
    }
}