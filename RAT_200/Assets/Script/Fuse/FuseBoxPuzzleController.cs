using UnityEngine;
using System.Collections;
using Unity.Cinemachine;

[RequireComponent(typeof(MicroZoomSession))]
public class FuseBoxPuzzleController : BaseInteractable, IMicroSessionHost, IMicroHidePlayerPreference
{
    [Header("Puzzle Target (Vent)")]
    [Tooltip("���� �ذ� �� ��ȣ�ۿ��� �������� ��¥ ȯǳ�� ������Ʈ (�ʱ⿣ Default ���̾�)")]
    public GameObject realVentObject;

    [Tooltip("�����: ������ �Ʒ��� �� ������ ��¥ ȯǳ�� ���� (Rigidbody �ʼ�)")]
    public GameObject fallingVentProp;
    public GameObject disappearVentProp;

    [Header("Visuals")]
    [Tooltip("����ũ ��ƼŬ�� (���� �� ���� ����, ó���� ���� ����)")]
    public ParticleSystem[] sparkEffects;

    [Tooltip("������ ǻ�� �� (ó���� ���� ����)")]
    public GameObject fuseVisual;

    [Header("Settings")]
    [Tooltip("������ ����Ǵ� �ð� (������ �������� �� ���Ѻ��� �ð�)")]
    public float successDelay = 2.0f; 

    [Header("Sound")]
    public AudioClip ventFallSound; // ��! �ϰ� �������� �Ҹ�

    // �������̽� ����
    public bool HidePlayerDuringMicro => true;

    private bool _isSolved = false;
    private MicroZoomSession _micro;
    private Collider _myCollider;

    void Awake()
    {
        _micro = GetComponent<MicroZoomSession>();
        _myCollider = GetComponent<Collider>();

        if (fuseVisual) fuseVisual.SetActive(false);
        if (fallingVentProp) fallingVentProp.SetActive(false); // ������� ���ܵ�

        // ����ũ ���
        if (sparkEffects != null)
        {
            foreach (var fx in sparkEffects)
            {
                if (fx) fx.Play();
            }
        }
        
        // �� ���� �� ��¥ ȯǳ���� ��ȣ�ۿ� �Ұ����ϰ�(Default) ����
        // (�����Ϳ��� �̸� �����ߴٸ� �� �ڵ�� ��� ������ ������ġ�� ��)
        if (realVentObject)
        {
            // Ȥ�� �𸣴� ������ �� Default ���̾�� ���� (��ȣ�ۿ� �Ұ�)
            realVentObject.layer = LayerMask.NameToLayer("Default");
        }
    }

    // --- 1. ���� ���� ---
    public override bool CanInteract(PlayerInteractor i) => !_isSolved;

    public override void Interact(PlayerInteractor i)
    {
        if (!CanInteract(i)) return;
        if (_micro) _micro.TryEnter(i);
    }

    // --- 2. �� ���� ���� ---
    public bool CanBeginMicro(PlayerInteractor player) => !_isSolved;

    public void OnMicroEnter(PlayerInteractor player)
    {
        if (_myCollider) _myCollider.enabled = false;
    }

    public void OnMicroExit(PlayerInteractor player)
    {
        if (_myCollider) _myCollider.enabled = true;
    }

    // --- 3. ���� ���� (Slot���� ȣ��) ---
    public void SolvePuzzle()
    {
        if (_isSolved) return;

        InventoryUI.Instance?.ForceClose();

        _isSolved = true;

        StartCoroutine(Routine_Success());
    }

    IEnumerator Routine_Success()
    {
        // 1. ǻ�� ����� �ð�ȭ
        if (fuseVisual) fuseVisual.SetActive(true);

        // 2. ����ũ ����
        if (sparkEffects != null)
        {
            foreach (var fx in sparkEffects)
            {
                if (fx) fx.Stop();
            }
        }

        // 3. ǻ�� ����� �Ҹ� (��Ĭ/������)
        CommonSoundController.Instance?.PlaySpark();

        yield return new WaitForSeconds(0.2f);

        // 6. �� �ƿ� (�ڵ� Exit)
        if (_micro) _micro.Exit();

        // --- �� [�ٽ�] ȯǳ�� ���� ���� ���� ---
        if (fallingVentProp && disappearVentProp)
        {
            fallingVentProp.SetActive(true); // Ȱ��ȭ�Ǹ鼭 �߷¿� ���� ������
            disappearVentProp.SetActive(false);
            
            // ���� �ణ ƨ�ܳ����� �ϰ� �ʹٸ� ���� �߰�
            Rigidbody rb = fallingVentProp.GetComponent<Rigidbody>();
            if (rb)
            {
                rb.AddForce((Vector3.forward + Vector3.down) * 1f, ForceMode.Impulse);
                rb.AddTorque(Random.insideUnitSphere * 3f, ForceMode.Impulse);
            }
        }

        

        // 5. �÷��̾ �������� �� �� �� �ְ� ���
        yield return new WaitForSeconds(successDelay);

        // 4. ���� �������� �Ҹ� (��!)
        if (ventFallSound)
        {
            // �÷��̾� ��ġ�� ī�޶� ��ġ���� �鸮�� ���
            AudioManager.Instance.Play(ventFallSound, AudioManager.Sound.Effect, 1.0f);
        }

        NoiseSystem.Instance.FireImpulse(1.0f);

        // --- �� [�ٽ�] ��¥ ȯǳ�� ��ȣ�ۿ� �ر� ---
        if (realVentObject)
        {
            // ���̾ Interactable�� �����Ͽ� �÷��̾ Ŭ���� �� �ְ� ����
            realVentObject.layer = LayerMask.NameToLayer("Interactable");
            Debug.Log("[FuseBox] Vent Unlocked! Layer changed to Interactable.");
        }

        // ���� BGM/UI ����
        CommonSoundController.Instance?.PlayPuzzleSuccess();

        // Achievement Clear: FuseFixed
        StoveAchievementManager.UnlockByStat(StoveAchievementStatIds.FuseFixed); 
    }
}