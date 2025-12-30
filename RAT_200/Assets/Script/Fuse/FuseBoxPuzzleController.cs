using UnityEngine;
using System.Collections;
using Unity.Cinemachine;

[RequireComponent(typeof(MicroZoomSession))]
public class FuseBoxPuzzleController : BaseInteractable, IMicroSessionHost, IMicroHidePlayerPreference
{
    [Header("Puzzle Target")]
    [Tooltip("이 퍼즐을 풀면 해금될 사다리 자리")]
    public LadderPlaceSpot targetSpotToUnlock;

    [Header("Visuals")]
    // ★ [수정] 단일 파티클 -> 배열로 변경
    [Tooltip("스파크 파티클들 (여러 개 연결 가능, 처음엔 켜져 있음)")]
    public ParticleSystem[] sparkEffects;

    [Tooltip("끼워질 퓨즈 모델 (처음엔 꺼져 있음)")]
    public GameObject fuseVisual;

    [Header("Settings")]
    public float successDelay = 1.5f; // 스파크 꺼지고 나갈 때까지 대기 시간

    // 인터페이스 구현
    public bool HidePlayerDuringMicro => true;

    private bool _isSolved = false;
    private MicroZoomSession _micro;
    private Collider _myCollider;

    void Awake()
    {
        _micro = GetComponent<MicroZoomSession>();
        _myCollider = GetComponent<Collider>();

        if (fuseVisual) fuseVisual.SetActive(false);

        // ★ [수정] 모든 스파크 재생
        if (sparkEffects != null)
        {
            foreach (var fx in sparkEffects)
            {
                if (fx) fx.Play();
            }
        }

        // 시작 시 타겟 구역을 잠급니다.
        if (targetSpotToUnlock) targetSpotToUnlock.isLocked = true;
    }

    // --- 1. 진입 제어 ---
    public override bool CanInteract(PlayerInteractor i) => !_isSolved;

    public override void Interact(PlayerInteractor i)
    {
        if (!CanInteract(i)) return;
        if (_micro) _micro.TryEnter(i);
    }

    // --- 2. 줌 세션 제어 ---
    public bool CanBeginMicro(PlayerInteractor player) => !_isSolved;

    public void OnMicroEnter(PlayerInteractor player)
    {
        // 줌인 시 겉면 콜라이더 꺼서 내부 슬롯 클릭 가능하게 함
        if (_myCollider) _myCollider.enabled = false;
    }

    public void OnMicroExit(PlayerInteractor player)
    {
        if (_myCollider) _myCollider.enabled = true;
    }

    // --- 3. 퍼즐 로직 (Slot에서 호출) ---
    public void SolvePuzzle()
    {
        if (_isSolved) return;
        _isSolved = true;

        StartCoroutine(Routine_Success());
    }

    IEnumerator Routine_Success()
    {
        // 1. 퓨즈 끼우기 시각화
        if (fuseVisual) fuseVisual.SetActive(true);

        // 2. ★ [수정] 모든 스파크 끄기
        if (sparkEffects != null)
        {
            foreach (var fx in sparkEffects)
            {
                if (fx) fx.Stop();
            }
        }

        // 3. 성공 사운드 (스파크 꺼지는 소리 or 기계 작동음)
        CommonSoundController.Instance?.PlaySpark();

        // 4. 잠시 대기
        yield return new WaitForSeconds(successDelay);

        // 5. 줌 아웃 (자동 Exit)
        if (_micro) _micro.Exit();

        // 6. 사다리 구역 해금!
        if (targetSpotToUnlock)
        {
            targetSpotToUnlock.UnlockSpot();
        }

        // 성공 사운드
        CommonSoundController.Instance?.PlayPuzzleSuccess();
    }
}