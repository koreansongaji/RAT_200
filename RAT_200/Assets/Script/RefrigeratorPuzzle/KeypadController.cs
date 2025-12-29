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
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip beepClip;
    [SerializeField] private AudioClip errorClip;
    [SerializeField] private AudioClip successClip;

    [Header("Events")]
    public UnityEvent OnCorrectPassword; // ���� ������ �� (�� ���� �� ����)
    public UnityEvent OnWrongPassword;   // Ʋ���� ��

    // ���� ����
    private string _currentInput = "";
    private bool _isLocked = false;      // ���� ���� �� ���
    private bool _isResetting = false;   // Ʋ���� �ʱ�ȭ ��

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
        PlaySound(beepClip);
    }

    // ����� ��ư (C)�� ȣ��
    public void InputClear()
    {
        if (_isLocked || _isResetting) return;

        _currentInput = "";
        UpdateDisplay();
        PlaySound(beepClip);
    }

    // Ȯ�� ��ư (Enter)�� ȣ��
    public void InputEnter()
    {
        if (_isLocked || _isResetting) return;

        if (_currentInput == password)
        {
            // ����
            _isLocked = true;
            if (inputDisplay) inputDisplay.text = "PASS";
            PlaySound(successClip);

            // 공용 퍼즐 성공 사운드
            CommonSoundController.Instance?.PlayPuzzleSuccess();

            OnCorrectPassword?.Invoke(); // �� ���⿡ �� ���� ����
        }
        else
        {
            // ����
            if (inputDisplay) inputDisplay.text = "ERR";
            PlaySound(errorClip);

            // 공용 퍼즐 실패 사운드
            CommonSoundController.Instance?.PlayPuzzleFail();

            OnWrongPassword?.Invoke();
            StartCoroutine(ResetRoutine());
        }
    }

    // ------------------------------------------------

    void UpdateDisplay()
    {
        if (inputDisplay) inputDisplay.text = _currentInput;
    }

    void PlaySound(AudioClip clip)
    {
        if (audioSource && clip) audioSource.PlayOneShot(clip);
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