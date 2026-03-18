using UnityEngine;
using DG.Tweening; // ����ġ �ִϸ��̼ǿ�

public class SwitchInteractable : BaseInteractable
{
    [Header("Target Lights (GameObjects)")]
    [Tooltip("���� ���� �� �������� ���� ����")]
    [SerializeField] private GameObject smallLight;
    [SerializeField] private GameObject smallLight_switch;

    [Tooltip("����ġ�� ������ ���� ū ����")]
    [SerializeField] private GameObject bigLight;

    [Header("Switch Visuals (Optional)")]
    [Tooltip("������ ����ġ ����/��ư ��")]
    [SerializeField] private Transform switchModel;
    [Tooltip("������ ���� ȸ�� �� (Big Light ON)")]
    [SerializeField] private Vector3 onRotation = new Vector3(45, 0, 0);
    [Tooltip("������ ���� ȸ�� �� (Small Light ON)")]
    [SerializeField] private Vector3 offRotation = new Vector3(-45, 0, 0);
    [SerializeField] private float animDuration = 0.3f;

    [Header("Audio")]
    [SerializeField] private AudioClip switchSound;

    [Header("Settings")]
    [Tooltip("üũ�ϸ� �� �� ���� �ڿ��� �ٽ� �� �� ����")]
    [SerializeField] private bool oneTime = false;

    // ���� ���� (false: Small On / true: Big On)
    private bool _isBigLightOn = false;
    private bool _isAnimating = false;

    private void Start()
    {
        // 1. ���� �� ���� ���� �ʱ�ȭ
        // _isBigLightOn�� false�̹Ƿ� -> Small: ON, Big: OFF
        UpdateLightsState();

        // 2. ����ġ �� ���� �ʱ�ȭ
        if (switchModel)
        {
            switchModel.localEulerAngles = _isBigLightOn ? onRotation : offRotation;
        }
    }

    public override bool CanInteract(PlayerInteractor i)
    {
        if (_isAnimating) return false;

        // ��ȸ���ε� �̹� �״ٸ� ��ȣ�ۿ� �Ұ�
        if (oneTime && _isBigLightOn) return false;

        return base.CanInteract(i);
    }

    public override void Interact(PlayerInteractor i)
    {
        Debug.Log("Switch Interaction");
        if (!CanInteract(i)) return;

        // ���� ��� (ON <-> OFF)
        _isBigLightOn = !_isBigLightOn;
        _isAnimating = true;

        // 1. �Ҹ� ���
        if (switchSound)
        {
            AudioManager.Instance.Play(switchSound, AudioManager.Sound.Effect);
        }

        // 2. ����ġ �ִϸ��̼�
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

        // 3. ���� ���� ����
        UpdateLightsState();
        
        // Steam achievement: A Great Beginning
        SteamAchievementManager.UnlockAchievement(SteamAchievementIds.AGreatBeginning);
    }

    private void UpdateLightsState()
    {
        // Big Light�� ������ �ϸ� Small�� ���, �ݴ�� Small�� ��
        if (smallLight)
        {
            smallLight.SetActive(!_isBigLightOn);
            smallLight_switch.SetActive(!_isBigLightOn);
        }
        if (bigLight) bigLight.SetActive(_isBigLightOn);
    }
}