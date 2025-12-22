using UnityEngine;
using System.Collections;
using DinoFracture;

public sealed class FractureImpulseOnComplete : MonoBehaviour
{
    [Header("Explosion Settings")]
    [SerializeField] private float explosionForce = 15.0f;   
    [SerializeField] private float upwardsModifier = 1.0f;   
    [SerializeField] private float randomOffset = 0.3f;     

    [Header("Stability")]
    [SerializeField] private float airResistance = 1.0f;    
    [SerializeField] private float maxSpeed = 15.0f;        

    public void OnFractureCompleted(OnFractureEventArgs args)
    {
        if (args == null || args.FracturePiecesRootObject == null) return;

        // 원래 물체의 '월드' 중심점을 정확하게 잡습니다.
        Vector3 blastPoint = args.OriginalObject.transform.position;

        var rbs = args.FracturePiecesRootObject.GetComponentsInChildren<Rigidbody>(includeInactive: false);

        foreach (var rb in rbs)
        {
            // [업데이트] 더 강력한 음수 스케일 보정
            EnsurePositivePhysics(rb.gameObject);
            
            rb.isKinematic = false; 
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true; 

            rb.linearDamping = airResistance;
            rb.angularDamping = airResistance;
        }

        StartCoroutine(BlastAway(rbs, blastPoint));
    }

    private void EnsurePositivePhysics(GameObject go)
    {
        // 1. 트랜스포트 로컬 스케일 보정
        Vector3 localScale = go.transform.localScale;
        go.transform.localScale = new Vector3(Mathf.Abs(localScale.x), Mathf.Abs(localScale.y), Mathf.Abs(localScale.z));

        // 2. 부모로부터 상속받은 음수 스케일이 물리 엔진을 방해하지 않도록 처리
        // 만약 부모가 -1이면 자식이 1이라도 월드 스케일은 -1이 됩니다.
        // 이 경우 물리 연산이 깨지므로, 파편을 잠시 부모에게서 떼어내어 스케일을 세팅할 수도 있으나
        // 가장 효과적인건 콜라이더 자체의 사이즈를 양수로 강제하는 것입니다.

        if (go.TryGetComponent(out BoxCollider box))
        {
            // 박스 콜라이더 사이즈 절대값 처리
            Vector3 bSize = box.size;
            box.size = new Vector3(Mathf.Abs(bSize.x), Mathf.Abs(bSize.y), Mathf.Abs(bSize.z));
            
            // 만약 사이즈가 0에 가깝다면 물리 엔진이 터질 수 있으므로 최소값 부여
            if (box.size.sqrMagnitude < 0.0001f) box.size = Vector3.one * 0.1f;
        }
        else if (go.TryGetComponent(out MeshCollider meshCol))
        {
            // 메시 콜라이더의 경우 Convex가 꺼져있으면 음수 스케일에서 터집니다.
            meshCol.convex = true;
        }
    }

    private IEnumerator BlastAway(Rigidbody[] rbs, Vector3 blastPoint)
    {
        // 물리 엔진이 안정화될 때까지 대기
        yield return new WaitForFixedUpdate();

        foreach (var rb in rbs)
        {
            if (rb == null) continue;

            rb.isKinematic = false;

            // [사방으로 튀게 하는 핵심 로직]
            // 파편의 현재 위치에서 폭발 지점을 뺀 벡터가 '바깥쪽' 방향입니다.
            Vector3 pushDir = (rb.worldCenterOfMass - blastPoint);
            
            // 만약 너무 겹쳐있어서 방향이 0이라면 무작위 방향 부여
            if (pushDir.sqrMagnitude < 0.001f) {
                pushDir = Random.onUnitSphere;
            }

            pushDir = pushDir.normalized;
            pushDir.y += upwardsModifier; // 위로 튀게 함
            
            // 무작위성 추가
            pushDir += Random.insideUnitSphere * randomOffset;

            // 최종 힘 적용
            rb.AddForce(pushDir.normalized * explosionForce, ForceMode.Impulse);
            rb.AddTorque(Random.insideUnitSphere * explosionForce, ForceMode.Impulse);

            // 속도 제한 (유니티 6 방식)
            if (rb.linearVelocity.magnitude > maxSpeed)
            {
                rb.linearVelocity = rb.linearVelocity.normalized * maxSpeed;
            }
        }
    }

    private void FixNegativeScale(GameObject go)
    {
        Vector3 s = go.transform.localScale;
        go.transform.localScale = new Vector3(Mathf.Abs(s.x), Mathf.Abs(s.y), Mathf.Abs(s.z));
        
        if (go.TryGetComponent(out BoxCollider box))
        {
            Vector3 b = box.size;
            box.size = new Vector3(Mathf.Abs(b.x), Mathf.Abs(b.y), Mathf.Abs(b.z));
        }
    }
}
