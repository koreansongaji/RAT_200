using UnityEngine;
using TMPro;
using DG.Tweening;

[RequireComponent(typeof(MicroZoomSession))]
public class SafePuzzleController : BaseInteractable, IMicroSessionHost, IMicroHidePlayerPreference
{
    [Header("Dials")]
    public SafeDialInteractable spade;
    public SafeDialInteractable heart;
    public SafeDialInteractable diamond;
    public SafeDialInteractable club;

    [Header("ENTER Button")]
    public PressableButton3D enterButton;

    [Header("Status Display")]
    public TMP_Text statusText;
    public float failBlinkSec = 0.6f;

    [Header("Door Animation")]
    public Transform rightDoorHinge;
    public Vector3 openLocalEuler = new(0, -85, 0);
    public float openSec = 0.5f;
    public Ease openEase = Ease.OutSine;

    [Header("Optional Linked Object")]
    public Transform linkedPart;
    public Vector3 linkedPartOpenEuler = new(0, 0, -90);

    // 정답 고정 (7, 1, 0, 3)
    private readonly int ansSpade = 7;
    private readonly int ansHeart = 1;
    private readonly int ansDiamond = 9;
    private readonly int ansClub = 3;

    [Header("Success Reward")]
    public DrawerItemDispenser dispenser;

    public bool HidePlayerDuringMicro => true;

    // 내부 상태
    bool _isSolved = false;
    MicroZoomSession _micro;
    Collider _myCollider;

    void Awake()
    {
        _micro = GetComponent<MicroZoomSession>();
        _myCollider = GetComponent<Collider>();

        // ★ [핵심 1] 다이얼들에게 Micro 세션 주입 및 새로고침
        RefreshMicroBind(spade);
        RefreshMicroBind(heart);
        RefreshMicroBind(diamond);
        RefreshMicroBind(club);

        // ★ [핵심 2] 버튼에게도 주입하고 "재부팅(껐다 켜기)"
        if (enterButton)
        {
            if (!enterButton.micro) enterButton.micro = _micro;

            // 껐다 켜야 OnEnable이 다시 호출되면서 이벤트를 정상적으로 구독합니다.
            enterButton.enabled = false;
            enterButton.enabled = true;

            enterButton.OnPressed.AddListener(AttemptOpen);
        }
    }

    // 컴포넌트에 세션을 넣어주고 재활성화시키는 함수
    void RefreshMicroBind(MonoBehaviour target)
    {
        if (!target) return;

        // 1. 다이얼인 경우
        var dial = target as SafeDialInteractable;
        if (dial && !dial.micro)
        {
            dial.micro = _micro;
            // 다이얼은 Collider 제어를 직접 안 하므로 enabled 토글 필수 아님. 
            // 하지만 안전하게 하려면 해도 무방.
        }

        // 2. 버튼인 경우 (위에서 처리하지만 혹시 몰라 범용성 확보)
        var btn = target as PressableButton3D;
        if (btn && !btn.micro)
        {
            btn.micro = _micro;
            btn.enabled = false;
            btn.enabled = true;
        }
    }

    void Start()
    {
        SetStatus("READY");
    }

    void OnDestroy()
    {
        if (enterButton) enterButton.OnPressed.RemoveListener(AttemptOpen);
    }

    // --- 1. 진입 제어 ---
    public override bool CanInteract(PlayerInteractor i) => !_isSolved;

    public override void Interact(PlayerInteractor i)
    {
        if (!CanInteract(i)) return;
        if (_micro) _micro.TryEnter(i);
    }

    // --- 2. 마이크로 세션 호스트 ---
    public bool CanBeginMicro(PlayerInteractor player) => !_isSolved;

    public void OnMicroEnter(PlayerInteractor player)
    {
        // 줌인하면 금고 껍데기 콜라이더 끔 (내부 버튼 클릭 방해 안 하려고)
        if (_myCollider) _myCollider.enabled = false;
    }

    public void OnMicroExit(PlayerInteractor player)
    {
        // 줌아웃하면 다시 켬
        if (_myCollider) _myCollider.enabled = true;
    }

    // --- 3. 퍼즐 로직 ---
    public void AttemptOpen()
    {
        if (_isSolved) return;

        bool ok =
            (spade && spade.CurrentValue == ansSpade) &&
            (heart && heart.CurrentValue == ansHeart) &&
            (diamond && diamond.CurrentValue == ansDiamond) &&
            (club && club.CurrentValue == ansClub);

        if (ok) OpenDoor();
        else
        {
            ShowFail();
            CommonSoundController.Instance?.PlayPuzzleFail();
        }
    }

    void OpenDoor()
    {
        _isSolved = true;
        SetStatus("OPEN");

        if (rightDoorHinge)
            rightDoorHinge.DOLocalRotate(openLocalEuler, openSec).SetEase(openEase);

        if (linkedPart)
            linkedPart.DOLocalRotate(linkedPartOpenEuler, openSec).SetEase(openEase);

        CommonSoundController.Instance?.PlayDoorOpen();
        CommonSoundController.Instance?.PlayPuzzleSuccess();
        if (dispenser) dispenser.Dispense();

        Invoke(nameof(ExitMicro), 0.5f);
    }

    void ExitMicro()
    {
        if (_micro && _micro.InMicro)
            _micro.Exit();
    }

    void ShowFail()
    {
        if (!statusText) return;
        DOTween.Kill(statusText);
        statusText.alpha = 1f;
        statusText.text = "FAIL";
        statusText.DOFade(0.2f, failBlinkSec * 0.5f).SetLoops(2, LoopType.Yoyo);
    }

    void SetStatus(string msg)
    {
        if (!statusText) return;
        DOTween.Kill(statusText);
        statusText.alpha = 1f;
        statusText.text = msg;
    }
}