using UnityEngine;
using System.Collections;

public class FracturePieceHandler : MonoBehaviour
{
    private Rigidbody _rb;
    private static Vector3 _lastBlastPoint;

    [Header("Force Settings")]
    [SerializeField] private float explosionForce = 15.0f;
    [SerializeField] private float upwardModifier = 1.0f;

    public static void SetBlastPoint(Vector3 point) => _lastBlastPoint = point;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _rb.isKinematic = true; // 생성 시점(미리 생성)엔 무조건 고정
        
        Vector3 s = transform.localScale;
        transform.localScale = new Vector3(Mathf.Abs(s.x), Mathf.Abs(s.y), Mathf.Abs(s.z));
    }

    // 이 함수는 원본이 깨질 때 수동으로 호출될 것입니다.
    public void Launch()
    {
        _rb.isKinematic = false;
        _rb.linearDamping = 1.0f;
        _rb.angularDamping = 1.0f;

        Vector3 pushDir = (transform.position - _lastBlastPoint);
        if (pushDir.sqrMagnitude < 0.001f) pushDir = Random.onUnitSphere;
        
        pushDir = pushDir.normalized;
        pushDir.y += upwardModifier;

        _rb.AddForce(pushDir.normalized * explosionForce, ForceMode.VelocityChange);
        _rb.AddTorque(Random.insideUnitSphere * explosionForce, ForceMode.VelocityChange);
    }
}
