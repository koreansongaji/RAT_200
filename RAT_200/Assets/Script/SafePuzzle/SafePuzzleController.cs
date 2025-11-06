using UnityEngine;
using TMPro;
using DG.Tweening;

public class SafePuzzleController : MonoBehaviour
{
    [Header("Dials")]
    public SafeDialInteractable spade;
    public SafeDialInteractable heart;
    public SafeDialInteractable diamond;
    public SafeDialInteractable club;

    [Header("Answer Paper")]
    public GameObject paperPrefab;      // 종이 프리팹(월드 스페이스 TMP가 붙은 단순 보드)
    public Transform paperSpawnPoint;   // 종이가 항상 생성될 고정 위치
    public string paperFormat = "♠ {0}\n♥ {1}\n♦ {2}\n♣ {3}";

    [Header("Safe Door")]
    public Transform rightDoorHinge;    // 오른쪽 문 힌지(로컬 Z 또는 Y 회전)
    public Vector3 openLocalEuler = new(0, -85, 0);
    public float openSec = 0.5f;
    public Ease openEase = Ease.OutSine;

    [Header("Seed")]
    public int randomSeed = 0;          // 0이면 Time-based

    int _ansSpade, _ansHeart, _ansDiamond, _ansClub;
    bool _opened;

    void Start()
    {
        InitAnswers();
        SpawnPaper();
        // 시작 LED와 다이얼 각도 초기화(원하면 전부 0으로)
        spade?.SetValue(0, true);
        heart?.SetValue(0, true);
        diamond?.SetValue(0, true);
        club?.SetValue(0, true);
    }

    void InitAnswers()
    {
        System.Random rng = (randomSeed == 0) ? new System.Random() : new System.Random(randomSeed);
        _ansSpade = rng.Next(0, 10);
        _ansHeart = rng.Next(0, 10);
        _ansDiamond = rng.Next(0, 10);
        _ansClub = rng.Next(0, 10);
    }

    void SpawnPaper()
    {
        if (!paperPrefab || !paperSpawnPoint) return;
        var go = Instantiate(paperPrefab, paperSpawnPoint.position, paperSpawnPoint.rotation);

        // 종이 프리팹 안에 TMP_Text 하나 있다고 가정
        var tmp = go.GetComponentInChildren<TMP_Text>();
        if (tmp)
            tmp.text = string.Format(paperFormat, _ansSpade, _ansHeart, _ansDiamond, _ansClub);
    }

    public void OnDialChanged(SafeDialInteractable dial)
    {
        if (_opened) return;

        bool ok = (spade && spade.CurrentValue == _ansSpade)
               && (heart && heart.CurrentValue == _ansHeart)
               && (diamond && diamond.CurrentValue == _ansDiamond)
               && (club && club.CurrentValue == _ansClub);

        if (ok) OpenDoor();
    }

    void OpenDoor()
    {
        if (_opened) return;
        _opened = true;

        if (rightDoorHinge)
            rightDoorHinge.DOLocalRotate(openLocalEuler, openSec).SetEase(openEase);

        // 여기서 소리/연구원 반응 등 이벤트 추가 가능
        Debug.Log("[SafePuzzle] OPEN!");
    }
}
