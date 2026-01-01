using UnityEngine;

public class BridgeCollapseTrigger : MonoBehaviour
{
    public LabToFridgeManager bridgeManager;

    // 트리거에 닿는 순간 무너짐
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (bridgeManager)
            {
                bridgeManager.CollapseBridge();
            }
        }
    }
}