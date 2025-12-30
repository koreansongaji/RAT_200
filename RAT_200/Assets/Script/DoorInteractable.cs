using UnityEngine;
using DG.Tweening;
using UnityEngine.Events; // �̺�Ʈ ����� ���� �߰�

public class DoorInteractable : BaseInteractable
{
    [Header("Door Target")]
    [Tooltip("������ ȸ���� Transform. ���� �� ��ũ��Ʈ�� ���� ������Ʈ�� ���.")]
    [SerializeField] private Transform door;

    [Header("Open Settings")]
    [Tooltip("���� ���¿��� Y������ �� �� ������(+�� ����, -�� �ݴ� ����).")]
    [SerializeField] private float openAngle = 90f;
    [Tooltip("����/������ �� ������ �ݴ��� �� üũ�ϸ� ������ �ݴ�� �����.")]
    [SerializeField] private bool invertDirection = false;
    [SerializeField] private bool startOpened = false;

    [Header("Restrictions")]
    [SerializeField] private bool openOnlyOnce = false;

    // ���� [�߰�] ��� ��� ����
    [Header("Lock Settings")]
    [Tooltip("üũ�Ǹ� Ŭ���ص� �� ����. �ܺ�(Ű�е� ��)���� Unlock ����� ��.")]
    public bool isLocked = false;
    [Tooltip("������� �� Ŭ���ϸ� ����� �̺�Ʈ (��: '����ִ�' �޽���, ���ȰŸ��� �Ҹ�)")]
    public UnityEvent OnTryLockedInteract;
    // ����������������������

    [Header("Tween")]
    [SerializeField] private float duration = 0.4f;
    [SerializeField] private Ease ease = Ease.OutCubic;

    [Header("Audio (Optional)")]
    public AudioClip openSound;
    public AudioClip closeSound;

    bool _isOpen;
    bool _isAnimating;
    Vector3 _closedEuler;
    Vector3 _openedEuler;
    Tween _tween;

    void Awake()
    {
        // 사운드 클립 로드
        if(openSound == null)
            openSound = Resources.Load<AudioClip>("Sounds/Effect/Universal/creak_a");
        if(closeSound == null)
            closeSound = Resources.Load<AudioClip>("Sounds/Effect/Universal/creak_b");
        
        if (!door) door = transform;

        _closedEuler = door.localEulerAngles;
        float dir = invertDirection ? -1f : 1f;
        _openedEuler = _closedEuler + new Vector3(0f, openAngle * dir, 0f);

        _isOpen = startOpened;
        door.localEulerAngles = _isOpen ? _openedEuler : _closedEuler;
    }

    public override bool CanInteract(PlayerInteractor i)
    {
        if (_isAnimating) return false;

        // �ѹ��� ���� ����̰� �̹� ���������� �Ұ�
        if (openOnlyOnce && _isOpen) return false;

        // �� ��������� ��ȣ�ۿ� �Ұ� (������ Ŭ�� �õ� �̺�Ʈ�� �߻���Ű�� ���� true�� ��ȯ�ϰ� Interact���� ó���� ���� ����.
        // ���⼭�� '�� �� ����'�� ��Ȯ�� �ϱ� ���� false�� ��ȯ�ϵ�, 
        // Ŭ�� �ǵ���� �ʿ��ϸ� ������ Interact�� �Űܾ� ��. 
        // �ϴ��� ��������� '��ȣ�ۿ� ��ũ'�� �� �߰� false ó��)
        if (isLocked) return false;

        return base.CanInteract(i);
    }

    // �÷��̾ ���� Ŭ������ ��
    public override void Interact(PlayerInteractor i)
    {
        // CanInteract���� �̹� �ɷ�������, Ȥ�� �𸣴� üũ
        if (isLocked)
        {
            OnTryLockedInteract?.Invoke();
            return;
        }

        if (!CanInteract(i)) return;
        if (!door) return;

        ToggleMove();
        GetComponent<DrawerItemDispenser>()?.Dispense();
    }

    // ���� ���� ����
    void ToggleMove()
    {
        _isOpen = !_isOpen;
        _isAnimating = true;
        _tween?.Kill();

        Vector3 targetEuler = _isOpen ? _openedEuler : _closedEuler;

        // �Ҹ� ���
        var clip = _isOpen ? openSound : closeSound;
        if (clip) AudioManager.Instance.Play(clip);

        // Tween ����
        var t = door.DOLocalRotate(targetEuler, duration)
            .SetEase(ease)
            .OnComplete(() => _isAnimating = false);

        // ���� �ý��� ����
        var noise = GetComponent<TweenNoiseAdapter>();
        _tween = TweenNoiseAdapter.WithNoise(t, noise);
    }

    // ���� [�߰�] �ܺο��� ȣ���� �Լ��� (UnityEvent �����) ����

    /// <summary>
    /// ����� �����ϰ� ���� ���ϴ�. (��й�ȣ ���� �� ����)
    /// </summary>
    public void UnlockAndOpen()
    {
        isLocked = false;
        if (!_isOpen) ToggleMove();
    }

    /// <summary>
    /// ������ ���ϴ� (��� ���� ����).
    /// </summary>
    public void ForceOpen()
    {
        if (!_isOpen) ToggleMove();
    }

    /// <summary>
    /// ������ �ݽ��ϴ�.
    /// </summary>
    public void ForceClose()
    {
        if (_isOpen) ToggleMove();
    }

    /// <summary>
    /// ��ݸ� �����մϴ� (���� �״��).
    /// </summary>
    public void UnlockOnly()
    {
        isLocked = true;
    }
    // ���������������������������������������

    void OnDisable()
    {
        _tween?.Kill();
        _isAnimating = false;
    }
}