using UnityEngine;
using DG.Tweening;
using UnityEngine.Events; // 이벤트 사용을 위해 추가

public class DoorInteractable : BaseInteractable
{
    [Header("Door Target")]
    [Tooltip("실제로 회전할 Transform. 비우면 이 스크립트가 붙은 오브젝트를 사용.")]
    [SerializeField] private Transform door;

    [Header("Open Settings")]
    [Tooltip("닫힌 상태에서 Y축으로 몇 도 열릴지(+는 한쪽, -는 반대 방향).")]
    [SerializeField] private float openAngle = 90f;
    [Tooltip("왼쪽/오른쪽 문 방향이 반대일 때 체크하면 각도가 반대로 적용됨.")]
    [SerializeField] private bool invertDirection = false;
    [SerializeField] private bool startOpened = false;

    [Header("Restrictions")]
    [SerializeField] private bool openOnlyOnce = false;

    // ▼▼▼ [추가] 잠금 기능 ▼▼▼
    [Header("Lock Settings")]
    [Tooltip("체크되면 클릭해도 안 열림. 외부(키패드 등)에서 Unlock 해줘야 함.")]
    public bool isLocked = false;
    [Tooltip("잠겨있을 때 클릭하면 실행될 이벤트 (예: '잠겨있다' 메시지, 덜컹거리는 소리)")]
    public UnityEvent OnTryLockedInteract;
    // ▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲

    [Header("Tween")]
    [SerializeField] private float duration = 0.4f;
    [SerializeField] private Ease ease = Ease.OutCubic;

    [Header("Audio (Optional)")]
    public AudioSource sfxSource;
    public AudioClip openSound;
    public AudioClip closeSound;

    bool _isOpen;
    bool _isAnimating;
    Vector3 _closedEuler;
    Vector3 _openedEuler;
    Tween _tween;

    void Awake()
    {
        if (!door) door = transform;

        _closedEuler = door.localEulerAngles;
        float dir = invertDirection ? -1f : 1f;
        _openedEuler = _closedEuler + new Vector3(0f, openAngle * dir, 0f);

        _isOpen = startOpened;
        door.localEulerAngles = _isOpen ? _openedEuler : _closedEuler;
    }

    public override bool CanInteract(PlayerInteractor i)
    {
        if (_isAnimating) return false;

        // 한번만 열기 모드이고 이미 열려있으면 불가
        if (openOnlyOnce && _isOpen) return false;

        // ★ 잠겨있으면 상호작용 불가 (하지만 클릭 시도 이벤트는 발생시키기 위해 true를 반환하고 Interact에서 처리할 수도 있음.
        // 여기서는 '열 수 없음'을 명확히 하기 위해 false를 반환하되, 
        // 클릭 피드백이 필요하면 로직을 Interact로 옮겨야 함. 
        // 일단은 잠겨있으면 '상호작용 마크'가 안 뜨게 false 처리)
        if (isLocked) return false;

        return base.CanInteract(i);
    }

    // 플레이어가 직접 클릭했을 때
    public override void Interact(PlayerInteractor i)
    {
        // CanInteract에서 이미 걸러지지만, 혹시 모르니 체크
        if (isLocked)
        {
            OnTryLockedInteract?.Invoke();
            return;
        }

        if (!CanInteract(i)) return;
        if (!door) return;

        ToggleMove();
    }

    // 내부 동작 로직
    void ToggleMove()
    {
        _isOpen = !_isOpen;
        _isAnimating = true;
        _tween?.Kill();

        Vector3 targetEuler = _isOpen ? _openedEuler : _closedEuler;

        // 소리 재생
        if (sfxSource)
        {
            var clip = _isOpen ? openSound : closeSound;
            if (clip) sfxSource.PlayOneShot(clip);
        }

        // Tween 실행
        var t = door.DOLocalRotate(targetEuler, duration)
            .SetEase(ease)
            .OnComplete(() => _isAnimating = false);

        // 소음 시스템 연동
        var noise = GetComponent<TweenNoiseAdapter>();
        _tween = TweenNoiseAdapter.WithNoise(t, noise);
    }

    // ▼▼▼ [추가] 외부에서 호출할 함수들 (UnityEvent 연결용) ▼▼▼

    /// <summary>
    /// 잠금을 해제하고 문을 엽니다. (비밀번호 성공 시 연결)
    /// </summary>
    public void UnlockAndOpen()
    {
        isLocked = false;
        if (!_isOpen) ToggleMove();
    }

    /// <summary>
    /// 강제로 엽니다 (잠금 상태 무시).
    /// </summary>
    public void ForceOpen()
    {
        if (!_isOpen) ToggleMove();
    }

    /// <summary>
    /// 강제로 닫습니다.
    /// </summary>
    public void ForceClose()
    {
        if (_isOpen) ToggleMove();
    }

    /// <summary>
    /// 잠금만 해제합니다 (문은 그대로).
    /// </summary>
    public void UnlockOnly()
    {
        isLocked = true;
    }
    // ▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲

    void OnDisable()
    {
        _tween?.Kill();
        _isAnimating = false;
    }
}