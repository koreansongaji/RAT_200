using Unity.Cinemachine;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class CameraZoneTrigger : MonoBehaviour
{
    public CinemachineCamera targetCam;

    [Header("Priority Settings")]
    // 가구 위(Zone)는 20, 다리/밧줄(Action)은 30
    public int activePriority = 20;

    private int _defaultPriority = 0;

    void Awake()
    {
        GetComponent<BoxCollider>().isTrigger = true;
        if (targetCam)
        {
            // 시작할 때 꺼두기
            targetCam.Priority = _defaultPriority;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && targetCam)
        {
            // 구역 진입: 우선순위 높임 (덮어쓰기)
            targetCam.Priority = activePriority;
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") && targetCam)
        {
            // 구역 퇴장: 우선순위 원상복구 (밑에 깔려있던 카메라가 나옴)
            targetCam.Priority = _defaultPriority;
        }
    }
}