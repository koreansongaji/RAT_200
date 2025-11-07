using Unity.Cinemachine;
using UnityEngine;

public class ClimbTarget : MonoBehaviour
{
    [Header("필수 참조")]
    public Transform climbPoint;          // 올라갈 빈 오브젝트 (자식)
    public CinemachineCamera closeVCam;   // 클로즈업 카메라
}
