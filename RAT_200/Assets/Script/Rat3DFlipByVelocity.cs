using UnityEngine;
using UnityEngine.AI;

public class Rat3DFlipByVelocity : MonoBehaviour
{
    public NavMeshAgent agent;
    public Camera cam;
    public Transform modelRoot; // 3D 모델 루트

    void LateUpdate()
    {
        if (!agent || !cam || !modelRoot) return;

        Vector3 v = agent.velocity;

        // 움직이지 않으면 flip 유지
        if (v.sqrMagnitude < 0.0001f) return;

        // 카메라 기준으로 좌/우 판별
        float dot = Vector3.Dot(cam.transform.right, v.normalized);

        // 오른쪽으로 이동하면 scale.x = +1
        if (dot > 0f)
        {
            Vector3 s = modelRoot.localScale;
            s.x = -Mathf.Abs(s.x);
            modelRoot.localScale = s;
        }
        // 왼쪽이면 scale.x = -1
        else
        {
            Vector3 s = modelRoot.localScale;
            s.x = Mathf.Abs(s.x);
            modelRoot.localScale = s;
        }
    }
}
