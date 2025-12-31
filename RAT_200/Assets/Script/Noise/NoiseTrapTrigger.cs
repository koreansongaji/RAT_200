using UnityEngine;

[RequireComponent(typeof(Collider))]
public class NoiseTrapTrigger : MonoBehaviour
{
    public enum FireMode
    {
        AddPercent,       // ���� ���� ���ϱ� (��: +30%)
        SetPercentExact   // ��ǥ ������ ���߱� (��: 100%�� ����)
    }

    [Header("Trigger ���")]
    [Tooltip("�� �±׸� ���� ������Ʈ�� ���� ���� �ߵ� (���� ��� ������Ʈ).")]
    public string targetTag = "Player";

    [Header("Noise ����")]
    public FireMode fireMode = FireMode.AddPercent;

    [Tooltip("0~1 ������. 0.3 = 30%, 0.9 = 90%, 1.0 = 100%.")]
    [Range(0f, 1f)] public float amount01 = 0.3f;

    [Tooltip("���� �� ������ �� �ִ� Ʈ���� ��� false, �� �����̸� true.")]
    public bool oneShot = true;

    [Tooltip("���� �� �ߵ� ������ ���, �ߵ� �� ��ٿ� �ð�(��). 0�̸� ��ٿ� ����.")]
    [Min(0f)] public float cooldown = 0f;

    [Header("�ߵ� �� ó��")]
    [Tooltip("�ߵ� �� �ݶ��̴��� ��Ȱ��ȭ���� ����.")]
    public bool disableColliderOnTrigger = false;

    [Tooltip("�ߵ� �� �� ������Ʈ�� �ı����� ����.")]
    public bool destroyOnTrigger = false;

    float _lastFireTime = -999f;
    bool _used;
    Collider _col;
    
    [Header("Sounds Clip")]
    [SerializeField] AudioClip _trapClip;

    void Reset()
    {
        // Reset���� �ڵ����� Trigger ����
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
            // ���� ��ġ: ������ Trigger�� ������ش�.
            _col.isTrigger = true;
        }
        
        if(_trapClip == null) _trapClip = Resources.Load<AudioClip>("Sounds/Effect/Trap/weak_floor");
    }

    void OnTriggerEnter(Collider other)
    {
        TryFire(other);
    }

    void TryFire(Collider other)
    {
        // �� ���� ����ϵ��� �����Ǿ��� �̹� ��ٸ� ����
        if (oneShot && _used) return;

        // ��ٿ� �ð� Ȯ��
        if (Time.time < _lastFireTime + cooldown) return;

        // �±� ����
        if (!string.IsNullOrEmpty(targetTag) && !other.CompareTag(targetTag))
            return;

        var ns = NoiseSystem.Instance;
        if (!ns) return;

        // 소리 출력
        AudioManager.Instance.Play(_trapClip);
        
        switch (fireMode)
        {
            case FireMode.AddPercent:
                // �ܼ��� amount01 ��ŭ ���� (��: +0.3 �� +30%p)
                ns.FireImpulse(amount01);
                break;

            case FireMode.SetPercentExact:
                // ���� ������ ��ǥ �������� ���̸� �� ���� Impulse�� ����
                // ��: current=0.4, amount01=1.0 �� +0.6�� ���ؼ� 100%�� ����
                float diff = amount01 - ns.current01;
                if (diff > 0f)
                {
                    ns.FireImpulse(diff);
                }
                // �̹� �� ���ٸ� ���� ���� �ʿ�� ���ٰ� ����(��ȹ�� "�ּ� X%")
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
