using UnityEngine;
using DG.Tweening;

public class SlidingDoorInteractable : BaseInteractable
{
    [Header("Target")]
    [Tooltip("������ ��� (����θ� �ڱ� �ڽ�)")]
    [SerializeField] Transform drawer;

    [Header("Sliding Settings")]
    [SerializeField] Vector3 openLocalOffset = new Vector3(0f, 0f, -0.3f);
    [SerializeField] float duration = 0.5f;
    [SerializeField] Ease ease = Ease.InOutSine;

    [Header("Initial State & Restrictions")]
    [SerializeField] bool startOpened = false;   // ���� �� ���� ���� ����
    [SerializeField] bool openOnlyOnce = false;  // �� �߰�: �� �� ���� ���� �� ����

    bool _opened;
    bool _isAnimating;
    Vector3 _startLocalPos;
    Vector3 _openLocalPos;
    Tween _tween;

    void Awake()
    {
        if (!drawer) drawer = transform;

        _startLocalPos = drawer.localPosition;
        _openLocalPos = _startLocalPos + openLocalOffset;

        _opened = startOpened;
        drawer.localPosition = _opened ? _openLocalPos : _startLocalPos;
    }

    public override bool CanInteract(PlayerInteractor i)
    {
        if (_isAnimating) return false;

        // �� �߰��� ����: �ѹ��� ���� �ɼ��� �����ְ�, �̹� �����ִٸ� ��ȣ�ۿ� �Ұ�
        if (openOnlyOnce && _opened) return false;

        return true;
    }

    public override void Interact(PlayerInteractor i)
    {
        if (!CanInteract(i)) return;

        ToggleMove();
    }

    void ToggleMove()
    {
        _opened = !_opened;
        _isAnimating = true;
        _tween?.Kill();

        Vector3 target = _opened ? _openLocalPos : _startLocalPos;

        var t = drawer.DOLocalMove(target, duration)
                      .SetEase(ease)
                      .SetLink(gameObject, LinkBehaviour.KillOnDestroy)
                      .OnComplete(() => _isAnimating = false);

        // 공용 사운드 재생 (슬라이드형 문)
        CommonSoundController.Instance?.PlayDoorSlide();

        var noise = GetComponent<TweenNoiseAdapter>();
        _tween = TweenNoiseAdapter.WithNoise(t, noise);
    }

    void OnDisable()
    {
        _tween?.Kill();
        _isAnimating = false;
    }
}