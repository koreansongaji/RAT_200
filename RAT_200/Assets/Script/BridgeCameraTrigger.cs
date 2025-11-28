using Unity.Cinemachine;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class BridgeCameraTrigger : MonoBehaviour
{
    public CinemachineCamera sideViewCam; // 사이드 뷰용 가상 카메라

    void Start()
    {
        if (sideViewCam) CloseupCamManager.Deactivate(sideViewCam);
        GetComponent<BoxCollider>().isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            CloseupCamManager.Activate(sideViewCam);
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            CloseupCamManager.Deactivate(sideViewCam);
        }
    }
}