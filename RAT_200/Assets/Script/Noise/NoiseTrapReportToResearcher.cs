using UnityEngine;

public class NoiseTrapReportToResearcher : MonoBehaviour
{
    public ResearcherController researcher;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && researcher)
        {
            //researcher.NotifyNoiseEvent(transform.position);
        }
    }
}
