using UnityEngine;

public class DrawerItemDispenser : MonoBehaviour
{
    [Header("Target Item")]
    public Rigidbody itemToDispense; // 굴러나올 아이템 (가로대)
    public Transform spawnPoint;     // 아이템이 나올 위치 (서랍 안쪽)
    public Vector3 ejectForce = new Vector3(0, 0, -2f); // 굴러나올 힘

    bool _dispensed = false;

    // DoorInteractable이나 SlidingDoorInteractable의 이벤트에 연결할 함수
    public void Dispense()
    {
        if (_dispensed) return;
        if (!itemToDispense) return;

        _dispensed = true;

        // 아이템 활성화 및 물리 힘 가하기
        itemToDispense.gameObject.SetActive(true);
        itemToDispense.transform.position = spawnPoint.position;
        itemToDispense.isKinematic = false; // 물리 켜기
        itemToDispense.AddForce(ejectForce, ForceMode.Impulse);

        Debug.Log("[Drawer] 아이템 배출!");
    }
}