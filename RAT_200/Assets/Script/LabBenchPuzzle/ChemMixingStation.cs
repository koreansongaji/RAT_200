using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using Unity.Cinemachine;
using System.Collections;

public class ChemMixingStation : BaseInteractable, IMicroSessionHost, IMicroHidePlayerPreference
{
    [Header("�䱸 ���� �÷���(������ üũ, �Ҹ�X)")]
    [SerializeField] string sodiumId = "Sodium";
    [SerializeField] string gelId = "Gel";
    [SerializeField] string waterInFlaskId = "WaterInFlask";

    [Header("UI (���� �����̽�)")]
    [SerializeField] Canvas panel;
    [SerializeField] Button btnSodium;
    [SerializeField] Button btnWater;
    [SerializeField] Button btnGel;
    [SerializeField] Button btnMix;

    [Header("ī���� ǥ��(TMP)")]
    [SerializeField] TMP_Text txtNa;
    [SerializeField] TMP_Text txtWater;
    [SerializeField] TMP_Text txtGel;
    [SerializeField] TMP_Text txtRecipe;

    [Header("�ʿ䷮(�⺻��)")]
    [Min(0)][SerializeField] int needNa = 2;
    [Min(0)][SerializeField] int needWater = 1;
    [Min(0)][SerializeField] int needGel = 4;

    [Header("��� ó��")]
    public UnityEvent OnMakeBigNoise; // ���� �� ���� �̺�Ʈ

    // ���� [����] �÷��̾� �̵� ���� ���� ���� & ����� ���� �߰� ����
    [Header("���� ���� (Bridge)")]
    public LabToFridgeManager bridgeManager; // �� �ʼ� ����: å �ٸ�/���� ���� ������
    public CinemachineCamera bridgeSideCam;        // �� �ʼ� ����: å �ٸ� �� ���̵� ī�޶�
    public float bridgeCamDuration = 2.5f;         // ī�޶� ���߰� ���� �ð�

    // (���� ���̾Ƹ�� ī��� BridgeManager���� �����̵� ī�带 ������ ������ ��ü�ǹǷ� �����ϰų� ���� ����)
    // [SerializeField] GameObject diamondCardPrefab; 
    // [SerializeField] Transform cardSpawnPoint;     
    // �����������������������������������������

    [Header("3D Buttons (optional)")]
    [SerializeField] PressableButton3D btnNa3D;
    [SerializeField] PressableButton3D btnWater3D;
    [SerializeField] PressableButton3D btnGel3D;
    [SerializeField] PressableButton3D btnMix3D;

    // ���� ����
    int _cNa, _cWater, _cGel;
    bool _session;

    // ���� ĳ��
    MicroZoomSession _micro;

    public bool hidePlayerDuringMicro = true;
    public bool HidePlayerDuringMicro => hidePlayerDuringMicro;

    [Header("Audio Clips")]
    [SerializeField] private AudioClip _chemMixingSuccessSound;
    [SerializeField] private AudioClip _chemMixingFailSound;
    [SerializeField] private AudioClip _chemMixingSound;
    
    void Awake()
    {
        if (panel) panel.enabled = false;
        _micro = GetComponent<MicroZoomSession>();
        WireButtons();
        RefreshTexts();
        
        // Load audio clips if not assigned
        if(_chemMixingSuccessSound == null) _chemMixingSuccessSound = Resources.Load<AudioClip>("Sounds/Effect/Experiment/reaction_mix");
        if (_chemMixingFailSound == null) _chemMixingFailSound = Resources.Load<AudioClip>("Sounds/Effect/Experiment/reaction_fail");
        if (_chemMixingSound == null) _chemMixingSound = Resources.Load<AudioClip>("Sounds/Effect/Experiment/reaction_mix");
    }

    void WireButtons()
    {
        if (btnSodium) btnSodium.onClick.AddListener(() => Tap(ref _cNa, needNa, btnSodium));
        if (btnWater) btnWater.onClick.AddListener(() => Tap(ref _cWater, needWater, btnWater));
        if (btnGel) btnGel.onClick.AddListener(() => Tap(ref _cGel, needGel, btnGel));
        if (btnMix) btnMix.onClick.AddListener(Submit);

        if (btnNa3D) btnNa3D.OnPressed.AddListener(() => Tap3D(ref _cNa, needNa, btnNa3D));
        if (btnWater3D) btnWater3D.OnPressed.AddListener(() => Tap3D(ref _cWater, needWater, btnWater3D));
        if (btnGel3D) btnGel3D.OnPressed.AddListener(() => Tap3D(ref _cGel, needGel, btnGel3D));
        if (btnMix3D) btnMix3D.OnPressed.AddListener(Submit);
    }

    public override bool CanInteract(PlayerInteractor i)
    {
        if (!i) return false;
        bool hasAll = i.HasItem(sodiumId) && i.HasItem(gelId) && i.HasItem(waterInFlaskId);
        return hasAll && !_session;
    }

    public override void Interact(PlayerInteractor i)
    {
        if (_session) return;
        if (!CanInteract(i))
        {
            Debug.Log("[ChemMixingStation] �䱸 ��ᰡ �����մϴ�.");
            return;
        }

        if (_micro)
        {
            if (_micro.TryEnter(i))
            {
                Debug.Log("[ChemMixingStation] Enter Micro zoom");
            }
            return;
        }
        StartSession();
    }

    // ===== IMicroSessionHost ���� =====
    public bool CanBeginMicro(PlayerInteractor player)
    {
        if (!player) return false;
        bool hasAll = player.HasItem(sodiumId) && player.HasItem(gelId) && player.HasItem(waterInFlaskId);
        return hasAll && !_session;
    }

    public void OnMicroEnter(PlayerInteractor player) => StartSession();
    public void OnMicroExit(PlayerInteractor player) => CancelSession();

    // ===== ���� ���� =====
    public void StartSession()
    {
        _session = true;
        _cNa = _cWater = _cGel = 0;

        if (panel) panel.enabled = true;

        bool interactableState = true;
        SetBtnInteractable(btnSodium, btnNa3D, needNa > 0);
        SetBtnInteractable(btnWater, btnWater3D, needWater > 0);
        SetBtnInteractable(btnGel, btnGel3D, needGel > 0);
        SetBtnInteractable(btnMix, btnMix3D, true);

        RefreshTexts();
    }

    // ����: ��ư Ȱ��ȭ �ϰ� ó��
    void SetBtnInteractable(Button uiBtn, PressableButton3D worldBtn, bool active)
    {
        if (uiBtn) uiBtn.interactable = active;
        if (worldBtn) worldBtn.SetInteractable(active);
    }

    public void CancelSession() => EndSession(false);

    void EndSession(bool fromSubmit)
    {
        _session = false;
        if (panel) panel.enabled = false;
        // Micro ����� Submit�̳� Exit ȣ��ο��� ó����
    }

    void Tap(ref int counter, int need, Button src)
    {
        if (!_session || need <= 0) return;
        counter++;
        RefreshTexts();
    }

    void Tap3D(ref int counter, int need, PressableButton3D src)
    {
        if (!_session) return;
        if (need <= 0) { if (src) src.SetInteractable(false); return; }
        counter++;
        RefreshTexts();
    }

    void RefreshTexts()
    {
        string Mark(int c, int n) => c > n ? $"<color=#ff6060>{c}</color>" : c.ToString();
        if (txtNa) txtNa.text = $"Na: {Mark(_cNa, needNa)}/{needNa}";
        if (txtWater) txtWater.text = $"Water: {Mark(_cWater, needWater)}/{needWater}";
        if (txtGel) txtGel.text = $"Gel: {Mark(_cGel, needGel)}/{needGel}";
        if (txtRecipe) txtRecipe.text = $"Rate 2:1:{needGel}";
    }

    // �ڡڡ� [���� ���� ����] �ڡڡ�
    void Submit()
    {
        if (!_session) return;

        bool success = (_cNa == needNa) && (_cWater == needWater) && (_cGel == needGel);

        if (!success)
        {
            // 실패 소리
            AudioManager.Instance.Play(_chemMixingFailSound);
            
            // ���� ��: ��� Micro Ż���ϰ� ���� �߻�
            if (_micro && _micro.InMicro) _micro.Exit();

            if (NoiseSystem.Instance) NoiseSystem.Instance.FireImpulse(1f);
            OnMakeBigNoise?.Invoke();
            Debug.Log("[ChemMixingStation] ȥ�� ����!");

            // 공용 퍼즐 실패 사운드
            CommonSoundController.Instance?.PlayPuzzleFail();

            EndSession(true);
            return;
        }
        // 성공 소리
        AudioManager.Instance.Play(_chemMixingSuccessSound);
        // === ���� �� ===
        Debug.Log("[ChemMixingStation] ȥ�� ����!");
        
        // 1. ����/�ð� ���� ���� (å ������, ���� ������)
        if (bridgeManager)
        {
            bridgeManager.PlaySequence();
        }

        // 2. ī�޶� ��ȯ �ڷ�ƾ ����
        // �� �߿�: ���⼭ _micro.Exit()�� ȣ������ �ʽ��ϴ�! (Lab ī�޶� ����)
        StartCoroutine(Routine_ShowBridgeSequence());

        // ���� ������ ���������� ���� (��ư ��Ȱ��ȭ ��)
        EndSession(true);

        // 공용 퍼즐 성공 사운드
        CommonSoundController.Instance?.PlayPuzzleSuccess();
    }

    // �ڡڡ� [ī�޶� ������] �ڡڡ�
    IEnumerator Routine_ShowBridgeSequence()
    {
        // 1. Bridge ī�޶� �ѱ� (Priority�� ���� �����ؼ� Lab ī�޶� ���)
        // ����: BridgeSideCam�� Priority�� Micro(30)���� ���ƾ� �մϴ�. (��: 40~50)
        if (bridgeSideCam)
        {
            bridgeSideCam.Priority = 100; // Ȯ���ϰ� ����
            // Ȥ�� CloseupCamManager�� ���� �Լ��� �߰��ص� ��
        }

        // 2. å �Ѿ����� ���� ����
        yield return new WaitForSeconds(bridgeCamDuration);

        // 3. Bridge ī�޶� ���� -> Lab ī�޶�(Micro)�� �ؿ� ���� �����Ƿ� �ڿ������� ���ƿ�
        if (bridgeSideCam)
        {
            bridgeSideCam.Priority = 0;
        }

        // 4. (���� ����) ��� Lab�� ������ �ڿ� �÷��̾� ������ �����ְ� �ʹٸ�:
        // yield return new WaitForSeconds(0.5f);

        // 5. ���� ��¥ ���� (�÷��̾� ���� ����, Room ��� ���������� ������ ESC �����ų� ���⼭ ���� ����)
        // ���⼭�� "Lab ��� ���ƿͼ� ����" ���¸� �����Ϸ��� �Ʒ� ���� �ּ� ó���ϼ���.
        // �ڵ����� ������ �Ϸ��� �ּ��� Ǫ����.
        if (_micro && _micro.InMicro)
            _micro.Exit();
    }

    // ===== API =====
    public void SetGelNeed(int newNeedGel)
    {
        needGel = Mathf.Max(0, newNeedGel);
        bool canPress = _session ? (_cGel < needGel) : (needGel > 0);
        SetBtnInteractable(btnGel, btnGel3D, canPress);
        RefreshTexts();
    }

    public void BeginSessionFromExternal() => StartSession();
    public void EndSessionFromExternal() => CancelSession();

#if UNITY_EDITOR
    void OnValidate()
    {
        if (needNa < 0) needNa = 0;
        if (needWater < 0) needWater = 0;
        if (needGel < 0) needGel = 0;
        RefreshTexts();
    }
#endif
}