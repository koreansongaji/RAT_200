using Unity.Cinemachine;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class ZoneCameraTrigger : MonoBehaviour
{
    [Header("Camera Settings")]
    [Tooltip("이 구역에 들어오면 켜질 카메라")]
    public CinemachineCamera targetCamera;

    [Tooltip("활성화 시 적용할 우선순위 (기본값 100)")]
    public int activePriority = 100;

    void Start()
    {
        // 시작할 때 카메라는 꺼둡니다 (우선순위 0)
        if (targetCamera) targetCamera.Priority = 0;

        // 트리거 설정 강제
        GetComponent<BoxCollider>().isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && targetCamera)
        {
            // 구역 진입 -> 카메라 우선순위 높임 (덮어쓰기)
            targetCamera.Priority = activePriority;
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") && targetCamera)
        {
            // 구역 퇴장 -> 카메라 우선순위 낮춤 (원상복구)
            targetCamera.Priority = 0;
        }
    }
}