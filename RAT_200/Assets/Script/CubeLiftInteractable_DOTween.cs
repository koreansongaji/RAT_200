using UnityEngine;
using UnityEngine.AI;
using DG.Tweening;
using Unity.Cinemachine;

public class CubeLiftInteractable_DOTween : BaseInteractable
{
    [Header("Lift")]
    [SerializeField] float liftHeight = 1.5f;
    [SerializeField] float duration = 0.6f;
    [SerializeField] Ease ease = Ease.InOutSine;
    [SerializeField] bool openOnlyOnce = true;
    [SerializeField] bool disableColliderOnLift = false;

    [Header("NavMesh")]
    [SerializeField] NavMeshObstacle obstacle;
    [SerializeField] bool disableObstacleOnLiftStart = true; // 시작 즉시 길 오픈

    Vector3 _startPos;
    bool _opened, _busy;
    Collider _col;
    Tween _tMove, _tFov;

    void Awake()
    {
        _startPos = transform.position;
        if (!obstacle) obstacle = GetComponent<NavMeshObstacle>();
        _col = GetComponent<Collider>();
        // 네 프로젝트에선 오브젝트별 RequiredDistance는 무시되고, PlayerReach만 사용됨
    }

    public override bool CanInteract(PlayerInteractor i)
        => !_busy && (!openOnlyOnce || !_opened);

    public override void Interact(PlayerInteractor i)
    {
        if (!CanInteract(i)) return;
        Lift();
    }

    void Lift()
    {
        _busy = true;

        // 1) 길 먼저 열기(즉시 경로 반영)
        if (obstacle && disableObstacleOnLiftStart) obstacle.enabled = false;
        if (disableColliderOnLift && _col) _col.enabled = false;

        // 2) 기존 트윈 정리 후 위로 이동
        _tMove?.Kill(); _tFov?.Kill();
        float targetY = _startPos.y + liftHeight;

        var tn = GetComponent<TweenNoiseAdapter>();
        _tMove = TweenNoiseAdapter.WithNoise(
            transform.DOMoveY(targetY, duration)
                  .SetEase(ease)
                  .SetUpdate(UpdateType.Normal) // 오브젝트 트윈은 Normal이면 충분
                  .SetLink(gameObject, LinkBehaviour.KillOnDestroy)
                  .OnComplete(() => { _opened = true; _busy = false; }),
            tn
        );


    }

    // (선택) 다시 닫는 동작이 필요하면 호출
    public void Close()
    {
        if (_busy) return;
        _busy = true;
        _tMove?.Kill();
        if (obstacle) obstacle.enabled = true;
        if (_col) _col.enabled = true;

        var tn = GetComponent<TweenNoiseAdapter>();
        _tMove = TweenNoiseAdapter.WithNoise(
            transform.DOMoveY(_startPos.y, duration)
                  .SetEase(ease)
                  .SetUpdate(UpdateType.Normal)
                  .SetLink(gameObject, LinkBehaviour.KillOnDestroy)
                  .OnComplete(() => { _busy = false; _opened = false; }),
            tn
        );
    }

    void OnDisable() { _tMove?.Kill(); _tFov?.Kill(); }
}
