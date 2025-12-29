using UnityEngine;
using UnityEngine.Events;
using DG.Tweening; // DOTween �ʼ�

public class ResearcherController : MonoBehaviour
{
    public enum State
    {
        Idle,           // ���
        SummonIntro,    // 100% ���� �� 5�ʰ� ��� (�߼Ҹ� ��)
        Searching,      // 30�� �Ϲ� ���� (����/����)
        Focusing,       // Ư�� ���� ��ġ ����
        Capture         // �÷��̾� �߰� (���ӿ���)
    }

    [Header("Refs")]
    [Tooltip("�� ��ü�� ���ߴ� ���� (������ ���� �� ����).")]
    public Light roomMainLight;
    [Tooltip("���� Spot Light ������Ʈ.")]
    public Light spotLight;
    [Tooltip("���� ȸ���� �߽��� (�� ����). Eye Pivot.")]
    public Transform eyePivot;
    [Tooltip("�� ���� (���� �ݱ��).")]
    public Transform doorHinge;
    [Tooltip("�÷��̾� Transform (������).")]
    public Transform player;

    [Header("Settings - Door")]
    public Vector3 doorOpenEuler = new Vector3(0, 90, 0);
    public Vector3 doorClosedEuler = Vector3.zero;

    [Header("Settings - Timing")]
    public float introDelay = 5f;        // 5�� ���
    public float searchDuration = 30f;   // 30�� ����
    public float focusDuration = 5f;     // 5�� ����
    [Range(0f, 1f)] public float noiseResetLevel = 0.2f; // ���� �� 20%

    [Header("Settings - Detection")]
    public LayerMask obstacleMask;       // ���� �� �ִ� ��ֹ� ���̾�
    public float catchDistance = 1.0f;   // (�ɼ�) �ʹ� ������ ��� ����

    [Header("Settings - Scanning (Floor Target)")]
    [Tooltip("�� �ٴ� �߾�.")]
    public Transform roomCenter;
    [Tooltip("���� ���� (����/����).")]
    public Vector2 scanAreaSize = new Vector2(8f, 8f);
    [Tooltip("���� �̵��ϴ� �ӵ�.")]
    public float scanMoveSpeed = 3f;

    [Header("Game Over")]
    public Transform handModel; // ������ ���� ��
    public UnityEvent OnGameOver; // ���� ���� �̺�Ʈ

    [Header("Events")]
    public UnityEvent OnSummonStarted;    // 100% ���� (��Ʈ�� ����)
    public UnityEvent OnIntroFinished;    // 5�� �� (�� ����, ���� ����)
    public UnityEvent OnSearchEnded;      // ���� ���� (�� ����)
    public UnityEvent OnPlayerCaught;     // �÷��̾� �߰�

    // Internal State
    State _state = State.Idle;
    float _stateTimer;
    Vector3 _scanTargetPos; // ���� �ٶ󺸴� �ٴ��� ���� ����
    Tween _scanTween;
    bool _subscribed;

    // ���� ���� ������ ��ġ�� ����ϴ� ����
    Vector3? _pendingFocusPos = null;

    [Header("Sfx Clips")]
    [SerializeField] private AudioClip _summonAlertClip;
    [SerializeField] private AudioClip _doorCreakClip;
    [SerializeField] private AudioClip _spotlightToggleClip;
    [SerializeField] private AudioClip _lightBuzzClip;
    [SerializeField] private AudioClip _caughtScareClip;

    void Start()
    {
        SubscribeNoiseSystem();
        if (spotLight) spotLight.enabled = false;
        if (handModel) handModel.gameObject.SetActive(false);

        // 초기 타겟은 방 중앙으로
        if (roomCenter) _scanTargetPos = roomCenter.position;

        // 리소스 로드 (할당 안 된 경우)
        if (_summonAlertClip == null) _summonAlertClip = Resources.Load<AudioClip>("Sounds/Effect/Researcher/alert");
        if (_doorCreakClip == null) _doorCreakClip = Resources.Load<AudioClip>("Sounds/Effect/Universal/creak_a");
        if (_spotlightToggleClip == null) _spotlightToggleClip = Resources.Load<AudioClip>("Sounds/Effect/Universal/button_b");
        if (_lightBuzzClip == null) _lightBuzzClip = Resources.Load<AudioClip>("Sounds/Effect/Universal/spark");
        if (_caughtScareClip == null) _caughtScareClip = Resources.Load<AudioClip>("Sounds/Effect/Rat/rat_death");
    }

    void OnEnable() => SubscribeNoiseSystem();
    void OnDisable()
    {
        var ns = NoiseSystem.Instance;
        if (ns != null && _subscribed)
        {
            ns.OnValueChanged -= HandleNoiseChanged;
            _subscribed = false;
        }
        _scanTween?.Kill();
    }

    void SubscribeNoiseSystem()
    {
        var ns = NoiseSystem.Instance;
        if (ns != null && !_subscribed)
        {
            ns.OnValueChanged += HandleNoiseChanged;
            _subscribed = true;
        }
    }

    // ===== NoiseSystem �ݹ� =====
    void HandleNoiseChanged(float value01)
    {
        if (_state != State.Idle) return;
        if (value01 >= 0.999f) StartSummon();
    }

    public void StartSummon()
    {
        if (_state != State.Idle) return;
        _state = State.SummonIntro;
        _stateTimer = 0f;
        _pendingFocusPos = null;
        
        // 소환 시작 효과음 (경고음)
        if (_summonAlertClip != null) AudioManager.Instance.Play(_summonAlertClip, AudioManager.Sound.Effect, 1.0f, 0.8f);
        
        OnSummonStarted?.Invoke(); // ���⼭ ���ڱ� �Ҹ� ���
        Debug.Log("[Researcher] Summon Started (Wait 5s)");
    }

    void Update()
    {
        // ���� �׻� Ÿ���� �ٶ󺸵��� ȸ�� (�ڿ������� 3�� ȸ��)
        if (eyePivot && (_state == State.Searching || _state == State.Focusing))
        {
            Vector3 dir = _scanTargetPos - eyePivot.position;
            Quaternion targetRot = Quaternion.LookRotation(dir);
            eyePivot.rotation = Quaternion.Slerp(eyePivot.rotation, targetRot, Time.deltaTime * 5f);
        }

        switch (_state)
        {
            case State.SummonIntro: UpdateSummonIntro(); break;
            case State.Searching: UpdateSearching(); break;
            case State.Focusing: UpdateFocusing(); break;
            case State.Capture: /* ���ӿ��� ���� �� */ break;
        }
    }

    // [����] 5�� ��� �� �� ���� ��
    void UpdateSummonIntro()
    {
        _stateTimer += Time.deltaTime;
        if (_stateTimer >= introDelay)
        {
            // �� ����
            if (doorHinge) doorHinge.DOLocalRotate(doorOpenEuler, 0.5f).SetEase(Ease.OutBack);
            if (spotLight) spotLight.enabled = true;
            if (roomMainLight) roomMainLight.enabled = false; // �� �� ����

            OnIntroFinished?.Invoke();

            // ���� [�ٽ� ����] ����ص� ���� ��ġ�� �ִ°�? ����
            if (_pendingFocusPos.HasValue)
            {
                // �ִ� -> �ٷ� '����(Focusing)' ���·� ����
                Debug.Log("[Researcher] Door Open -> Immediately Focusing on Target!");
                _state = State.Focusing;
                _stateTimer = 0f;

                // �ü� Ÿ���� ����ص� ��ġ�� ����
                _scanTargetPos = _pendingFocusPos.Value;
                _pendingFocusPos = null; // ��������� �ʱ�ȭ
            }
            else
            {
                // ���� -> �׳� '�Ϲ� ����(Searching)' ����
                Debug.Log("[Researcher] Door Open -> Random Search Start");
                _state = State.Searching;
                _stateTimer = 0f;
                PickNextRandomScanPoint();
            }
            // �����������������������������������������
        }
    }

    void UpdateSearching()
    {
        _stateTimer += Time.deltaTime;

        // ���� ����
        DetectPlayer();

        // �ð� ���� üũ
        if (_stateTimer >= searchDuration)
        {
            EndSearchAndResetNoise();
        }
    }

    void UpdateFocusing()
    {
        _stateTimer += Time.deltaTime;

        // ���� ���� (���� �߿��� ��ų �� ����)
        DetectPlayer();

        if (_stateTimer >= focusDuration)
        {
            // ���� �� -> �ٽ� ���� �������� ����
            _state = State.Searching;
            _stateTimer = 0f; // ���� Ÿ�̸� ���� ���δ� ��ȹ�� ���� ���� (���⼱ ��ü �ð� ���� �� ��)
            PickNextRandomScanPoint();
        }
    }

    // ===== ���� ���� (Raycast & Spotlight Angle) =====
    void DetectPlayer()
    {
        if (!player || !spotLight) return;

        Vector3 toPlayer = player.position - eyePivot.position;
        float dist = toPlayer.magnitude;

        // 1. ���� ���� �ȿ� ���Դ°�?
        float angle = Vector3.Angle(eyePivot.forward, toPlayer);
        if (angle > spotLight.spotAngle * 0.5f) return; // �� ����

        // 2. ��ֹ� �˻� (Shadow Casting)
        // ���� �������� �÷��̾���� ���̸� ���� �� ��ֹ��� ��������
        if (!Physics.Raycast(eyePivot.position, toPlayer.normalized, dist, obstacleMask))
        {
            // ��ֹ��� ���ٸ� -> �÷��̾� ����� -> �˰�
            StartCapture();
        }
    }

    void StartCapture()
    {
        if (_state == State.Capture) return;
        _state = State.Capture;

        _scanTween?.Kill();
        // �ü��� �÷��̾�� ����
        _scanTargetPos = player.position;

        Debug.Log("[Researcher] CAUGHT YOU!");
        OnPlayerCaught?.Invoke(); // ����ڵ� �Ҹ� ��

        // �� �� ����
        if (handModel)
        {
            handModel.gameObject.SetActive(true);
            handModel.DOMove(player.position, 0.4f).SetEase(Ease.InExpo)
                     .OnComplete(() => OnGameOver?.Invoke());
        }
        else
        {
            OnGameOver?.Invoke();
        }
    }

    // ===== Scanning Logic (�ٴ� Ÿ�� �̵�) =====
    void PickNextRandomScanPoint()
    {
        if (_state != State.Searching) return;
        if (!roomCenter) return;

        // ���� ��ġ ����
        float rx = Random.Range(-scanAreaSize.x * 0.5f, scanAreaSize.x * 0.5f);
        float rz = Random.Range(-scanAreaSize.y * 0.5f, scanAreaSize.y * 0.5f);
        Vector3 nextPos = roomCenter.position + new Vector3(rx, 0, rz);

        // ���� ��ġ���� ���� ��ġ���� �Ÿ� ��� �ð� ��� (���� �ӵ� ����)
        float dist = Vector3.Distance(_scanTargetPos, nextPos);
        float moveTime = dist / scanMoveSpeed;

        _scanTween = DOTween.To(() => _scanTargetPos, x => _scanTargetPos = x, nextPos, moveTime)
                            .SetEase(Ease.InOutSine)
                            .OnComplete(PickNextRandomScanPoint); // �����ϸ� ���� ������
    }

    // ===== �ܺ� �˸��� (NoiseTrapReportToResearcher���� ȣ��) =====
    // [����] �ܺ� �˸���
    public void NotifyNoiseEvent(Vector3 worldPos)
    {
        if (_state == State.Capture) return;

        // ��� 1: �̹� ���ͼ� Ȱ�� ���� �� -> ��� �Ĵٺ�
        if (_state == State.Searching || _state == State.Focusing)
        {
            Debug.Log($"[Researcher] Investigating Noise at {worldPos}");
            _state = State.Focusing;
            _stateTimer = 0f;
            _scanTween?.Kill();

            DOTween.To(() => _scanTargetPos, x => _scanTargetPos = x, worldPos, 0.5f)
                   .SetEase(Ease.OutCubic);
        }
        // ��� 2: ��� ���̰ų�, ���� ����(5��) ���� �� -> ��ġ�� ����ص�
        else if (_state == State.Idle || _state == State.SummonIntro)
        {
            _pendingFocusPos = worldPos;
            Debug.Log($"[Researcher] Noise detected at {worldPos}. Will investigate after door opens.");
        }
    }

    // ===== ���� ���� �� ���� =====
    void EndSearchAndResetNoise()
    {
        _state = State.Idle;
        _scanTween?.Kill();

        if (spotLight) spotLight.enabled = false;
        if (doorHinge) doorHinge.DOLocalRotate(doorClosedEuler, 0.5f);

        OnSearchEnded?.Invoke();

        // ���� ������ ����
        if (NoiseSystem.Instance)
        {
            NoiseSystem.Instance.SetLevel01(noiseResetLevel);
        }

        if (roomMainLight) StartCoroutine(Routine_FlickerLightOn());

        Debug.Log("[Researcher] Returned to Idle.");
    }

    System.Collections.IEnumerator Routine_FlickerLightOn()
    {
        // �� ������ �ð�(0.5��) ���� ��ٷȴٰ� �ѱ� ����
        yield return new WaitForSeconds(0.5f);

        // ġ..��.. (������ ������ �ݺ�)
        roomMainLight.enabled = true;
        yield return new WaitForSeconds(0.05f);
        roomMainLight.enabled = false;
        yield return new WaitForSeconds(0.1f);

        roomMainLight.enabled = true;
        yield return new WaitForSeconds(0.1f);
        roomMainLight.enabled = false;
        yield return new WaitForSeconds(0.2f); // ��� ��

        // Ź! (������ ����)
        roomMainLight.enabled = true;
    }

    // ������ ����� (���� ���� Ȯ�ο�)
    void OnDrawGizmosSelected()
    {
        if (roomCenter)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(roomCenter.position, new Vector3(scanAreaSize.x, 0.1f, scanAreaSize.y));
        }
        if (spotLight)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(spotLight.transform.position, catchDistance);
        }
    }

    // ���� [���� �߰�] ���� ���� �߿� ���� ��(Ÿ��)�� �ü�(��)�� �׸� ����
    void OnDrawGizmos()
    {
        // ���� �÷��� ���� �ƴϰų�, ����/���� ���°� �ƴϸ� �׸��� ����
        if (!Application.isPlaying) return;
        if (_state != State.Searching && _state != State.Focusing) return;

        Gizmos.color = Color.red;

        // 1. �ٴ��� ���ٴϴ� Ÿ�� (���� ��)
        Gizmos.DrawSphere(_scanTargetPos, 0.3f);

        // 2. ������ Ÿ������ ��� ������ (���� ��)
        if (eyePivot)
        {
            Gizmos.DrawLine(eyePivot.position, _scanTargetPos);
        }
    }
}