using UnityEngine;
using UnityEngine.Events;
using DG.Tweening;

public class ResearcherController : MonoBehaviour
{
    public enum State
    {
        Idle,
        SummonIntro,
        Searching,
        Focusing,
        Capture,
        BusyWithEvent // �� [�߰�] �̺�Ʈ ���� �� �ٸ� �ൿ �� ��
    }

    [Header("Refs")]
    public Light roomMainLight;
    public Light spotLight;
    public Transform eyePivot;
    public Transform doorHinge;
    public Transform player;

    [Header("Optional Target")]
    [Tooltip("�÷��̾� ������ ������ NPC�� �ִٸ� ����.")]
    public Transform npcTarget;

    [Header("Events")]
    public UnityEvent OnSummonStarted;
    public UnityEvent OnIntroFinished;
    public UnityEvent OnSearchEnded;
    public UnityEvent OnPlayerCaught; // �÷��̾� �߰� ��
    public UnityEvent OnNpcCaught;    // �� [�߰�] NPC �߰� ��

    [Header("Settings")]
    public Vector3 doorOpenEuler = new Vector3(0, 90, 0);
    public Vector3 doorClosedEuler = Vector3.zero;
    public float introDelay = 5f;
    public float searchDuration = 30f;
    public float focusDuration = 5f;
    public LayerMask obstacleMask;
    public float catchDistance = 1.0f;
    public Transform roomCenter;
    public Vector2 scanAreaSize = new Vector2(8f, 8f);
    public float scanMoveSpeed = 3f;

    [Header("Game Over")]
    public Transform handModel;
    public UnityEvent OnGameOver;

    State _state = State.Idle;
    float _stateTimer;
    Vector3 _scanTargetPos;
    Tween _scanTween;
    bool _subscribed;
    Transform _currentFocusTarget; // ���� �Ĵٺ��� �ִ� ��� (�÷��̾� or NPC)

    [Header("Sfx Clips")]
    [SerializeField] private AudioClip _summonAlertClip;
    [SerializeField] private AudioClip _doorCreakClip;
    [SerializeField] private AudioClip _spotlightToggleClip;
    [SerializeField] private AudioClip _lightBuzzClip;
    [SerializeField] private AudioClip _caughtScareClip;
    Vector3? _pendingFocusPos = null;

    void Start()
    {
        SubscribeNoiseSystem();
        if (spotLight) spotLight.enabled = false;
        if (handModel) handModel.gameObject.SetActive(false);
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
        if (ns != null && _subscribed) { ns.OnValueChanged -= HandleNoiseChanged; _subscribed = false; }
        _scanTween?.Kill();
    }

    void SubscribeNoiseSystem()
    {
        var ns = NoiseSystem.Instance;
        if (ns != null && !_subscribed) { ns.OnValueChanged += HandleNoiseChanged; _subscribed = true; }
    }

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
        // �ü� ó�� (Ÿ���� ������ Ÿ����, ������ �ٴ� ��ĵ ������)
        Vector3 lookPos = _currentFocusTarget ? _currentFocusTarget.position : _scanTargetPos;

        if (eyePivot)
        {
            Vector3 dir = lookPos - eyePivot.position;
            if (dir != Vector3.zero)
            {
                Quaternion targetRot = Quaternion.LookRotation(dir);
                eyePivot.rotation = Quaternion.Slerp(eyePivot.rotation, targetRot, Time.deltaTime * 10f);
            }
        }

        switch (_state)
        {
            case State.SummonIntro: UpdateSummonIntro(); break;
            case State.Searching: UpdateSearching(); break;
            case State.Focusing: UpdateFocusing(); break;
            case State.BusyWithEvent: /* �̺�Ʈ ������ �˾Ƽ� �� */ break;
        }
    }

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
        DetectTargets(); // �� �÷��̾� & NPC ����
        if (_stateTimer >= searchDuration) EndSearchAndResetNoise();
    }

    void UpdateFocusing()
    {
        _stateTimer += Time.deltaTime;
        DetectTargets(); // �� ���� �߿��� ����
        if (_stateTimer >= focusDuration)
        {
            _state = State.Searching;
            _stateTimer = 0f;
            PickNextRandomScanPoint();
        }
    }

    // ���� [�ٽ� ����] Ÿ�� ���� ���� ����
    void DetectTargets()
    {
        if (!spotLight) return;

        // 1����: �÷��̾� üũ (�ɸ��� ���ӿ���)
        if (CheckVisibility(player))
        {
            StartCapturePlayer();
            return;
        }

        // 2����: NPC üũ (�ɸ��� �̺�Ʈ)
        if (npcTarget && CheckVisibility(npcTarget))
        {
            StartCaptureNPC();
            return;
        }
    }

    // �þ� üũ ���� �Լ�
    bool CheckVisibility(Transform target)
    {
        if (!target) return false;
        Vector3 toTarget = target.position - eyePivot.position;
        float dist = toTarget.magnitude;

        // ���� üũ
        float angle = Vector3.Angle(eyePivot.forward, toTarget);
        if (angle > spotLight.spotAngle * 0.5f) return false;

        // ��ֹ� üũ
        if (Physics.Raycast(eyePivot.position, toTarget.normalized, dist, obstacleMask))
        {
            return false; // ���� (���� ����)
        }
        return true; // ����
    }

    void StartCapturePlayer()
    {
        if (_state == State.Capture || _state == State.BusyWithEvent) return;
        _state = State.Capture;
        _scanTween?.Kill();

        _currentFocusTarget = player; // �÷��̾� ����
        OnPlayerCaught?.Invoke();

        //if (handModel)
        //{
        //    handModel.gameObject.SetActive(true);
        //    handModel.DOMove(player.position, 0.4f).SetEase(Ease.InExpo).OnComplete(() => OnGameOver?.Invoke());
        //}
        //else OnGameOver?.Invoke();

        if (GameLoopManager.Instance)
            GameLoopManager.Instance.TriggerGameOver();
        else
            OnGameOver?.Invoke();
    }

    void StartCaptureNPC()
    {
        if (_state == State.Capture || _state == State.BusyWithEvent) return;

        // �� NPC �߰�! -> �̺�Ʈ ���� ��ȯ
        _state = State.BusyWithEvent;
        _scanTween?.Kill();

        _currentFocusTarget = npcTarget; // �ü��� NPC�� ���� (����ٴ�)
        OnNpcCaught?.Invoke(); // �������� ��ȣ ����
    }

    // ���� [�߰�] �̺�Ʈ ������ �� �Լ��� ����

    // ������ ���� ��� (�̺�Ʈ ���� �� ȣ��)
    public void ForceLeave()
    {
        _state = State.Idle;
        _currentFocusTarget = null;
        if (spotLight) spotLight.enabled = false;
        if (doorHinge) doorHinge.DOLocalRotate(doorClosedEuler, 0.5f);
        if (roomMainLight) StartCoroutine(Routine_FlickerLightOn());
    }

    void PickNextRandomScanPoint()
    {
        if (_state != State.Searching) return;
        if (!roomCenter) return;
        float rx = Random.Range(-scanAreaSize.x * 0.5f, scanAreaSize.x * 0.5f);
        float rz = Random.Range(-scanAreaSize.y * 0.5f, scanAreaSize.y * 0.5f);
        Vector3 nextPos = roomCenter.position + new Vector3(rx, 0, rz);
        float dist = Vector3.Distance(_scanTargetPos, nextPos);

        _scanTween = DOTween.To(() => _scanTargetPos, x => _scanTargetPos = x, nextPos, dist / scanMoveSpeed)
                            .SetEase(Ease.InOutSine).OnComplete(PickNextRandomScanPoint);
    }

    public void NotifyNoiseEvent(Vector3 worldPos)
    {
        if (_state == State.Searching || _state == State.Focusing)
        {
            _state = State.Focusing;
            _stateTimer = 0f;
            _scanTween?.Kill();
            DOTween.To(() => _scanTargetPos, x => _scanTargetPos = x, worldPos, 0.5f).SetEase(Ease.OutCubic);
        }
        else if (_state == State.Idle || _state == State.SummonIntro)
        {
            _pendingFocusPos = worldPos;
            Debug.Log($"[Researcher] Noise detected at {worldPos}. Will investigate after door opens.");
        }
    }

    void EndSearchAndResetNoise()
    {
        OnSearchEnded?.Invoke();
        _state = State.Idle;
        _scanTween?.Kill();
        if (spotLight) spotLight.enabled = false;
        if (doorHinge) doorHinge.DOLocalRotate(doorClosedEuler, 0.5f);
        if (roomMainLight) StartCoroutine(Routine_FlickerLightOn());
        if (NoiseSystem.Instance) NoiseSystem.Instance.SetLevel01(0.2f);
    }

    System.Collections.IEnumerator Routine_FlickerLightOn()
    {
        yield return new WaitForSeconds(0.5f);
        roomMainLight.enabled = true;
        yield return new WaitForSeconds(0.05f);
        roomMainLight.enabled = false;
        yield return new WaitForSeconds(0.1f);
        roomMainLight.enabled = true;
    }

    void OnDrawGizmos()
    {
        if (Application.isPlaying && spotLight && _currentFocusTarget == null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(eyePivot.position, _scanTargetPos);
            Gizmos.DrawSphere(_scanTargetPos, 0.2f);
        }
    }
}