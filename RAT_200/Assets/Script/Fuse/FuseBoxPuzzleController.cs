using UnityEngine;
using System.Collections;
using Unity.Cinemachine;

[RequireComponent(typeof(MicroZoomSession))]
public class FuseBoxPuzzleController : BaseInteractable, IMicroSessionHost, IMicroHidePlayerPreference
{
    [Header("Puzzle Target (Vent)")]
    [Tooltip("퍼즐 해결 후 상호작용이 가능해질 진짜 환풍구 오브젝트 (초기엔 Default 레이어)")]
    public GameObject realVentObject;

    [Tooltip("연출용: 위에서 아래로 툭 떨어질 가짜 환풍구 덮개 (Rigidbody 필수)")]
    public GameObject fallingVentProp;

    [Header("Visuals")]
    [Tooltip("스파크 파티클들 (여러 개 연결 가능, 처음엔 켜져 있음)")]
    public ParticleSystem[] sparkEffects;

    [Tooltip("끼워질 퓨즈 모델 (처음엔 꺼져 있음)")]
    public GameObject fuseVisual;

    [Header("Settings")]
    [Tooltip("연출이 진행되는 시간 (덮개가 떨어지는 걸 지켜보는 시간)")]
    public float successDelay = 2.0f; 

    [Header("Sound")]
    public AudioClip ventFallSound; // 쿵! 하고 떨어지는 소리

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
        if (fallingVentProp) fallingVentProp.SetActive(false); // 연출용은 숨겨둠

        // 스파크 재생
        if (sparkEffects != null)
        {
            foreach (var fx in sparkEffects)
            {
                if (fx) fx.Play();
            }
        }
        
        // ★ 시작 시 진짜 환풍구는 상호작용 불가능하게(Default) 설정
        // (에디터에서 미리 설정했다면 이 코드는 없어도 되지만 안전장치로 둠)
        if (realVentObject)
        {
            // 혹시 모르니 시작할 땐 Default 레이어로 강제 (상호작용 불가)
            realVentObject.layer = LayerMask.NameToLayer("Default");
        }
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

        // 2. 스파크 끄기
        if (sparkEffects != null)
        {
            foreach (var fx in sparkEffects)
            {
                if (fx) fx.Stop();
            }
        }

        // 3. 퓨즈 끼우는 소리 (찰칵/전기음)
        CommonSoundController.Instance?.PlaySpark();


        // 6. 줌 아웃 (자동 Exit)
        if (_micro) _micro.Exit();

        // --- ★ [핵심] 환풍구 덮개 낙하 연출 ---
        if (fallingVentProp)
        {
            fallingVentProp.SetActive(true); // 활성화되면서 중력에 의해 떨어짐
            
            // 만약 약간 튕겨나가게 하고 싶다면 힘을 추가
            Rigidbody rb = fallingVentProp.GetComponent<Rigidbody>();
            if (rb)
            {
                rb.AddForce((Vector3.forward + Vector3.down) * 1f, ForceMode.Impulse);
                rb.AddTorque(Random.insideUnitSphere * 3f, ForceMode.Impulse);
            }
        }

        

        // 5. 플레이어가 떨어지는 걸 볼 수 있게 대기
        yield return new WaitForSeconds(successDelay);

        // 4. 덮개 떨어지는 소리 (쿵!)
        if (ventFallSound)
        {
            // 플레이어 위치나 카메라 위치에서 들리게 재생
            AudioManager.Instance.Play(ventFallSound, AudioManager.Sound.Effect, 1.0f);
        }

        NoiseSystem.Instance.FireImpulse(1.0f);

        // --- ★ [핵심] 진짜 환풍구 상호작용 해금 ---
        if (realVentObject)
        {
            // 레이어를 Interactable로 변경하여 플레이어가 클릭할 수 있게 만듦
            realVentObject.layer = LayerMask.NameToLayer("Interactable");
            Debug.Log("[FuseBox] Vent Unlocked! Layer changed to Interactable.");
        }

        // 성공 BGM/UI 사운드
        CommonSoundController.Instance?.PlayPuzzleSuccess();

    }
}