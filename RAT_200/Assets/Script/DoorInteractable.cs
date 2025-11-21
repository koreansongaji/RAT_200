using UnityEngine;
using DG.Tweening;

/// <summary>
/// 피벗이 힌지에 있는 문/서랍 전용 인터랙트.
/// 클릭하면 열기/닫기 토글.
/// </summary>
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
    [Tooltip("씬 시작 시 이미 열린 상태로 둘지 여부.")]
    [SerializeField] private bool startOpened = false;

    [Header("Tween")]
    [SerializeField] private float duration = 0.4f;
    [SerializeField] private Ease ease = Ease.OutCubic;

    bool _isOpen;
    bool _isAnimating;
    Vector3 _closedEuler;
    Vector3 _openedEuler;
    Tween _tween;

    void Awake()
    {
        if (!door)
            door = transform;

        _closedEuler = door.localEulerAngles;

        float dir = invertDirection ? -1f : 1f;
        _openedEuler = _closedEuler + new Vector3(0f, openAngle * dir, 0f);

        _isOpen = startOpened;
        door.localEulerAngles = _isOpen ? _openedEuler : _closedEuler;
    }

    public override bool CanInteract(PlayerInteractor i)
    {
        // 재생 중에는 다시 못 누르게
        if (_isAnimating) return false;
        return base.CanInteract(i);
    }

    public override void Interact(PlayerInteractor i)
    {
        if (!CanInteract(i)) return;
        if (!door) return;

        _isOpen = !_isOpen;
        _isAnimating = true;

        _tween?.Kill();

        Vector3 targetEuler = _isOpen ? _openedEuler : _closedEuler;

        _tween = door
            .DOLocalRotate(targetEuler, duration)
            .SetEase(ease)
            .OnComplete(() =>
            {
                _isAnimating = false;
            });
    }

    void OnDisable()
    {
        _tween?.Kill();
        _isAnimating = false;
    }
}
