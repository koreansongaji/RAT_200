using System;
using UnityEngine;

[RequireComponent(typeof(LadderPlacementController))]
public class Ladder : MonoBehaviour
{
    [Range(1, 4)] public int lengthLevel = 1; // ���׷��̵�� ����
    public LadderPlaceSpot currentSpot;       // ���� �پ��ִ� ���� (������ ��� �ٴϴ� ����)
    
    [SerializeField] private LadderSoundController _ladderSoundController;

    private void Awake()
    {
        if(_ladderSoundController == null) _ladderSoundController = GetComponent<LadderSoundController>();
        if(_ladderSoundController == null) Debug.LogError("No LadderSoundController found on " + gameObject.name);
    }

    public void AttachTo(LadderPlaceSpot spot, bool alignRotation = true)
    {
        if (!spot || !spot.ladderAnchor) return;
        
        // ���� ���� ����
        if (currentSpot) currentSpot.occupied = false;

        // ���� & ����
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
        _ladderSoundController.PlayPlaceLadder();
    }
}
