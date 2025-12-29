using UnityEngine;
using TMPro;
using DG.Tweening;

public class SafePuzzleController : MonoBehaviour, IMicroHidePlayerPreference
{
    [Header("Dials")]
    public SafeDialInteractable spade;
    public SafeDialInteractable heart;
    public SafeDialInteractable diamond;
    public SafeDialInteractable club;

    [Header("ENTER Button")]
    public PressableButton3D enterButton;       // PressableButton3D의 OnPressed를 코드에서 연결

    [Header("Status Display (optional)")]
    public TMP_Text statusText;                // READY / FAIL / OPEN 표시용
    public float failBlinkSec = 0.6f;

    [Header("Door")]
    public Transform rightDoorHinge;           // 금고 오른문 힌지
    public Vector3 openLocalEuler = new(0, -85, 0);
    public float openSec = 0.5f;
    public Ease openEase = Ease.OutSine;

    // ▼▼▼ [추가] 같이 회전할 오브젝트 설정 ▼▼▼
    [Header("Optional Linked Object")]
    [Tooltip("문 열릴 때 같이 돌아갈 오브젝트 (예: 손잡이, 잠금장치). 없으면 비워두세요.")]
    public Transform linkedPart;
    [Tooltip("같이 돌아갈 오브젝트의 목표 회전 각도")]
    public Vector3 linkedPartOpenEuler = new(0, 0, -90);
    // ▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲

    [Header("Answer Paper (optional)")]
    public GameObject paperPrefab;
    public Transform paperSpawnPoint;
    public string paperFormat = "♠ {0}\n♥ {1}\n♦ {2}\n♣ {3}";

    [Header("Seed")]
    public int randomSeed = 0;                 // 0이면 Time 기반

    public bool hidePlayerDuringMicro = true; // 퍼즐별로 토글
    public bool HidePlayerDuringMicro => hidePlayerDuringMicro;

    int ansSpade, ansHeart, ansDiamond, ansClub;
    bool opened;

    [Header("Success")]
    public DrawerItemDispenser dispenser;

    void Awake()
    {
        // Enter 버튼 눌렀을 때 시도
        if (enterButton) enterButton.OnPressed.AddListener(AttemptOpen);
    }

    void Start()
    {
        InitAnswers();
        SpawnPaper();
        SetStatus("READY");
    }

    void OnDestroy()
    {
        if (enterButton) enterButton.OnPressed.RemoveListener(AttemptOpen);
    }

    void InitAnswers()
    {
        System.Random rng = (randomSeed == 0) ? new System.Random() : new System.Random(randomSeed);
        ansSpade = rng.Next(0, 10);
        ansHeart = rng.Next(0, 10);
        ansDiamond = rng.Next(0, 10);
        ansClub = rng.Next(0, 10);
        Debug.Log($"Spade: {ansSpade}, Heart: {ansHeart}, Diamond: {ansDiamond}, Club: {ansClub}");
    }

    void SpawnPaper()
    {
        if (!paperPrefab || !paperSpawnPoint) return;
        var go = Instantiate(paperPrefab, paperSpawnPoint.position, paperSpawnPoint.rotation);
        var tmp = go.GetComponentInChildren<TMP_Text>();
        if (tmp) tmp.text = string.Format(paperFormat, ansSpade, ansHeart, ansDiamond, ansClub);
    }

    public void AttemptOpen()
    {
        if (opened) return;

        bool ok =
            (spade && spade.CurrentValue == ansSpade) &&
            (heart && heart.CurrentValue == ansHeart) &&
            (diamond && diamond.CurrentValue == ansDiamond) &&
            (club && club.CurrentValue == ansClub);

        if (ok) OpenDoor();
        else { ShowFail(); CommonSoundController.Instance?.PlayPuzzleFail(); }
    }

    void OpenDoor()
    {
        opened = true;
        SetStatus("OPEN");
        if (rightDoorHinge)
            rightDoorHinge.DOLocalRotate(openLocalEuler, openSec).SetEase(openEase);

        // ▼▼▼ [추가] 추가 오브젝트가 연결되어 있다면 같이 회전 ▼▼▼
        if (linkedPart)
        {
            linkedPart.DOLocalRotate(linkedPartOpenEuler, openSec).SetEase(openEase);
        }
        // ▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲
        // 필요하면 효과음/연출 추가

        // 공용 사운드 재생 (금고 열림)
        CommonSoundController.Instance?.PlayDoorOpen();

        // 공용 퍼즐 성공 사운드
        CommonSoundController.Instance?.PlayPuzzleSuccess();

        dispenser.Dispense();
    }

    void ShowFail()
    {
        if (!statusText) return;
        DOTween.Kill(statusText); // 중복 방지
        statusText.alpha = 1f;
        statusText.text = "FAIL";
        // 간단한 점멸
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
