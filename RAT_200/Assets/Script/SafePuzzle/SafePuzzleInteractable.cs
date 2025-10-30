using UnityEngine;
using UnityEngine.Events;
using DG.Tweening;

public class SafePuzzleInteractable : BaseInteractable
{
    [Header("Session / Cameras")]
    [SerializeField] MicroZoomSession micro;   // 같은 오브젝트에 붙이는 걸 권장

    [Header("Dials (4)")]
    public SafeDial spadeDial;
    public SafeDial heartDial;
    public SafeDial diamondDial;
    public SafeDial clubDial;

    [Header("Safe Door")]
    public Transform door;                     // 힌지 기준의 문 Transform
    public Vector3 openLocalEuler = new(0, -95f, 0);
    public float openDuration = 0.5f;
    public Ease openEase = Ease.OutCubic;

    [Header("Reward (optional)")]
    public string rewardItemId = "KeyCard";    // 플레이어 인벤토리에 지급
    public UnityEvent OnSolved;                // 사운드/파티클 연결용
    public UnityEvent OnOpened;                // 도어가 열릴 때

    bool _open;
    PlayerInteractor _lastPlayer;

    void Awake()
    {
        // 다이얼 값이 바뀔 때마다 검사
        if (spadeDial) spadeDial.OnValueChanged.AddListener(_ => CheckSolved());
        if (heartDial) heartDial.OnValueChanged.AddListener(_ => CheckSolved());
        if (diamondDial) diamondDial.OnValueChanged.AddListener(_ => CheckSolved());
        if (clubDial) clubDial.OnValueChanged.AddListener(_ => CheckSolved());
    }

    public override bool CanInteract(PlayerInteractor i)
    {
        if (!i) return false;
        if (_open) return false; // 이미 열렸으면 상호작용 금지
        return true;
    }

    public override void Interact(PlayerInteractor i)
    {
        if (!CanInteract(i)) return;
        _lastPlayer = i;

        // Micro 세션 우선
        if (micro && micro.TryEnterMicro(i))
        {
            // 진입 시 현재 다이얼을 정렬(선택)
            return;
        }

        // Micro가 없다면 그냥 바로 조작하도록 둔다
    }

    void CheckSolved()
    {
        if (_open || SafeCodeRegistry.Instance == null) return;

        var reg = SafeCodeRegistry.Instance;
        bool ok =
            spadeDial && spadeDial.value == reg.Get(CardSuit.Spade) &&
            heartDial && heartDial.value == reg.Get(CardSuit.Heart) &&
            diamondDial && diamondDial.value == reg.Get(CardSuit.Diamond) &&
            clubDial && clubDial.value == reg.Get(CardSuit.Club);

        if (ok) OpenSafe();
    }

    void OpenSafe()
    {
        _open = true;
        OnSolved?.Invoke();

        // 문 열기
        if (door)
        {
            door.DOLocalRotate(openLocalEuler, openDuration).SetEase(openEase)
                .OnComplete(() => OnOpened?.Invoke());
        }
        else
        {
            OnOpened?.Invoke();
        }

        // 보상 지급
        if (!string.IsNullOrEmpty(rewardItemId) && _lastPlayer)
            _lastPlayer.AddItem(rewardItemId);

        // Micro에서 나가기
        if (micro && micro.InMicro) micro.ExitMicro();
    }
}
