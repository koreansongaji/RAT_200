using UnityEngine;
using UnityEngine.Events;
using TMPro;
using System.Collections;

public class KeypadController : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("��й�ȣ (�ִ� 5�ڸ� ����)")]
    [SerializeField] private string password = "1234";
    [SerializeField] private int maxChars = 5;

    [Header("UI Refs")]
    [SerializeField] private TextMeshPro inputDisplay; // ȭ�鿡 ǥ�õ� �ؽ�Ʈ

    [Header("Audio (Optional)")]
    [SerializeField] private AudioClip _beepClip;
    [SerializeField] private AudioClip _errorClip;
    [SerializeField] private AudioClip _successClip;
    [SerializeField] private AudioClip _unlockClip;

    [Header("Events")]
    public UnityEvent OnCorrectPassword; // ���� ������ �� (�� ���� �� ����)
    public UnityEvent OnWrongPassword;   // Ʋ���� ��

    // ���� ����
    private string _currentInput = "";
    private bool _isLocked = false;      // ���� ���� �� ���
    private bool _isResetting = false;   // Ʋ���� �ʱ�ȭ ��

    private void Awake()
    {
        if(_beepClip == null) _beepClip = Resources.Load<AudioClip>("Sounds/Effect/Universal/button_a");
        if(_errorClip == null) _errorClip = Resources.Load<AudioClip>("Sounds/Effect/Universal/puzzle_fail");
        if(_successClip == null) _successClip = Resources.Load<AudioClip>("Sounds/Effect/Universal/puzzle_success");
        if(_unlockClip == null) _unlockClip = Resources.Load<AudioClip>("Sounds/Effect/Fridge - Light Puzzle/fridge_unlock");
    }

    void Start()
    {
        if (inputDisplay) inputDisplay.text = "";
    }

    // --- �ܺ�(PressableButton3D)���� ȣ���� �Լ��� ---

    // ���� ��ư (0~9)�� ȣ��
    public void InputNumber(int number)
    {
        if (_isLocked || _isResetting) return;
        if (_currentInput.Length >= maxChars) return;

        _currentInput += number.ToString();
        UpdateDisplay();
        PlaySound(_beepClip, Random.Range(0.9f, 1.1f));
    }

    // ����� ��ư (C)�� ȣ��
    public void InputClear()
    {
        if (_isLocked || _isResetting) return;

        _currentInput = "";
        UpdateDisplay();
        PlaySound(_beepClip, Random.Range(0.9f, 1.1f));
    }

    // Ȯ�� ��ư (Enter)�� ȣ��
    public void InputEnter()
    {
        PlaySound(_beepClip, Random.Range(0.9f, 1.1f));
        
        if (_isLocked || _isResetting) return;

        if (_currentInput == password)
        {
            // ����
            _isLocked = true;
            if (inputDisplay) inputDisplay.text = "PASS";
            PlaySound(_successClip);

            OnCorrectPassword?.Invoke(); // �� ���⿡ �� ���� ����
        }
        else
        {
            // ����
            if (inputDisplay) inputDisplay.text = "ERR";
            PlaySound(_errorClip);

            OnWrongPassword?.Invoke();
            StartCoroutine(ResetRoutine());
        }
    }

    // ------------------------------------------------

    void UpdateDisplay()
    {
        if (inputDisplay) inputDisplay.text = _currentInput;
    }

    void PlaySound(AudioClip clip, float pitch = 1.0f)
    {
        AudioManager.Instance.Play(clip);
    }

    IEnumerator ResetRoutine()
    {
        _isResetting = true;
        yield return new WaitForSeconds(1.0f);
        _currentInput = "";
        UpdateDisplay();
        _isResetting = false;
    }
}