using UnityEngine;

[RequireComponent(typeof(Collider))]
public class NoiseTrapTrigger : MonoBehaviour
{
    public enum FireMode
    {
        AddPercent,       // 현재 값에 더하기 (예: +30%)
        SetPercentExact   // 목표 값으로 맞추기 (예: 100%로 설정)
    }

    [Header("Trigger 대상")]
    [Tooltip("이 태그를 가진 오브젝트가 들어올 때만 발동 (비우면 모든 오브젝트).")]
    public string targetTag = "Player";

    [Header("Noise 설정")]
    public FireMode fireMode = FireMode.AddPercent;

    [Tooltip("0~1 스케일. 0.3 = 30%, 0.9 = 90%, 1.0 = 100%.")]
    [Range(0f, 1f)] public float amount01 = 0.3f;

    [Tooltip("여러 번 지나갈 수 있는 트랩일 경우 false, 한 번용이면 true.")]
    public bool oneShot = true;

    [Tooltip("여러 번 발동 가능한 경우, 발동 후 쿨다운 시간(초). 0이면 쿨다운 없음.")]
    [Min(0f)] public float cooldown = 0f;

    [Header("발동 후 처리")]
    [Tooltip("발동 후 콜라이더를 비활성화할지 여부.")]
    public bool disableColliderOnTrigger = false;

    [Tooltip("발동 후 이 오브젝트를 파괴할지 여부.")]
    public bool destroyOnTrigger = false;

    float _lastFireTime = -999f;
    bool _used;
    Collider _col;

    void Reset()
    {
        // Reset에서 자동으로 Trigger 설정
        _col = GetComponent<Collider>();
        if (_col)
        {
            _col.isTrigger = true;
        }
    }

    void Awake()
    {
        _col = GetComponent<Collider>();
        if (_col && !_col.isTrigger)
        {
            // 안전 장치: 강제로 Trigger로 만들어준다.
            _col.isTrigger = true;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        TryFire(other);
    }

    void TryFire(Collider other)
    {
        // 한 번만 사용하도록 설정되었고 이미 썼다면 무시
        if (oneShot && _used) return;

        // 쿨다운 시간 확인
        if (Time.time < _lastFireTime + cooldown) return;

        // 태그 필터
        if (!string.IsNullOrEmpty(targetTag) && !other.CompareTag(targetTag))
            return;

        var ns = NoiseSystem.Instance;
        if (!ns) return;

        switch (fireMode)
        {
            case FireMode.AddPercent:
                // 단순히 amount01 만큼 더함 (예: +0.3 → +30%p)
                ns.FireImpulse(amount01);
                break;

            case FireMode.SetPercentExact:
                // 현재 값에서 목표 값까지의 차이를 한 번에 Impulse로 보냄
                // 예: current=0.4, amount01=1.0 → +0.6을 더해서 100%로 맞춤
                float diff = amount01 - ns.current01;
                if (diff > 0f)
                {
                    ns.FireImpulse(diff);
                }
                // 이미 더 높다면 굳이 내릴 필요는 없다고 가정(기획상 "최소 X%")
                break;
        }

        _lastFireTime = Time.time;
        if (oneShot) _used = true;

        if (disableColliderOnTrigger && _col)
            _col.enabled = false;

        if (destroyOnTrigger)
            Destroy(gameObject);
    }
}
