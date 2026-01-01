using UnityEngine;
using DG.Tweening; // 스위치 애니메이션용

public class SwitchInteractable : BaseInteractable
{
    [Header("Target Lights (GameObjects)")]
    [Tooltip("게임 시작 시 켜져있을 작은 조명")]
    [SerializeField] private GameObject smallLight;

    [Tooltip("스위치를 누르면 켜질 큰 조명")]
    [SerializeField] private GameObject bigLight;

    [Header("Switch Visuals (Optional)")]
    [Tooltip("움직일 스위치 레버/버튼 모델")]
    [SerializeField] private Transform switchModel;
    [Tooltip("켜졌을 때의 회전 값 (Big Light ON)")]
    [SerializeField] private Vector3 onRotation = new Vector3(45, 0, 0);
    [Tooltip("꺼졌을 때의 회전 값 (Small Light ON)")]
    [SerializeField] private Vector3 offRotation = new Vector3(-45, 0, 0);
    [SerializeField] private float animDuration = 0.3f;

    [Header("Audio")]
    [SerializeField] private AudioClip switchSound;

    [Header("Settings")]
    [Tooltip("체크하면 한 번 켜진 뒤에는 다시 끌 수 없음")]
    [SerializeField] private bool oneTime = false;

    // 현재 상태 (false: Small On / true: Big On)
    private bool _isBigLightOn = false;
    private bool _isAnimating = false;

    private void Start()
    {
        // 1. 시작 시 조명 상태 초기화
        // _isBigLightOn이 false이므로 -> Small: ON, Big: OFF
        UpdateLightsState();

        // 2. 스위치 모델 각도 초기화
        if (switchModel)
        {
            switchModel.localEulerAngles = _isBigLightOn ? onRotation : offRotation;
        }
    }

    public override bool CanInteract(PlayerInteractor i)
    {
        if (_isAnimating) return false;

        // 일회용인데 이미 켰다면 상호작용 불가
        if (oneTime && _isBigLightOn) return false;

        return base.CanInteract(i);
    }

    public override void Interact(PlayerInteractor i)
    {
        Debug.Log("Switch Interaction");
        if (!CanInteract(i)) return;

        // 상태 토글 (ON <-> OFF)
        _isBigLightOn = !_isBigLightOn;
        _isAnimating = true;

        // 1. 소리 재생
        if (switchSound)
        {
            AudioManager.Instance.Play(switchSound, AudioManager.Sound.Effect);
        }

        // 2. 스위치 애니메이션
        if (switchModel)
        {
            Vector3 targetRot = _isBigLightOn ? onRotation : offRotation;
            switchModel.DOLocalRotate(targetRot, animDuration)
                .SetEase(Ease.OutBack)
                .OnComplete(() => _isAnimating = false);
        }
        else
        {
            _isAnimating = false;
        }

        // 3. 조명 상태 적용
        UpdateLightsState();
    }

    private void UpdateLightsState()
    {
        // Big Light가 켜져야 하면 Small은 끄고, 반대면 Small을 켬
        if (smallLight) smallLight.SetActive(!_isBigLightOn);
        if (bigLight) bigLight.SetActive(_isBigLightOn);
    }
}