using UnityEngine;
// using Unity.Cinemachine; // 이제 여기서 시네머신 몰라도 됨

public class ClimbTarget : MonoBehaviour
{
    [Header("필수 참조")]
    public Transform climbPoint;          // 올라갈 위치 (빈 오브젝트)

    // public CinemachineCamera closeVCam; // <-- 삭제 (더 이상 직접 안 씀)
}