using UnityEngine;
using UnityEngine.Events;
using TMPro;
using System.Collections;

public class KeypadController : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("비밀번호 (최대 5자리 권장)")]
    [SerializeField] private string password = "1234";
    [SerializeField] private int maxChars = 5;

    [Header("UI Refs")]
    [SerializeField] private TextMeshPro inputDisplay; // 화면에 표시될 텍스트

    [Header("Audio (Optional)")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip beepClip;
    [SerializeField] private AudioClip errorClip;
    [SerializeField] private AudioClip successClip;

    [Header("Events")]
    public UnityEvent OnCorrectPassword; // 정답 맞췄을 때 (문 열기 등 연결)
    public UnityEvent OnWrongPassword;   // 틀렸을 때

    // 내부 상태
    private string _currentInput = "";
    private bool _isLocked = false;      // 정답 맞춘 후 잠금
    private bool _isResetting = false;   // 틀리고 초기화 중

    void Start()
    {
        if (inputDisplay) inputDisplay.text = "";
    }

    // --- 외부(PressableButton3D)에서 호출할 함수들 ---

    // 숫자 버튼 (0~9)이 호출
    public void InputNumber(int number)
    {
        if (_isLocked || _isResetting) return;
        if (_currentInput.Length >= maxChars) return;

        _currentInput += number.ToString();
        UpdateDisplay();
        PlaySound(beepClip);
    }

    // 지우기 버튼 (C)이 호출
    public void InputClear()
    {
        if (_isLocked || _isResetting) return;

        _currentInput = "";
        UpdateDisplay();
        PlaySound(beepClip);
    }

    // 확인 버튼 (Enter)이 호출
    public void InputEnter()
    {
        if (_isLocked || _isResetting) return;

        if (_currentInput == password)
        {
            // 정답
            _isLocked = true;
            if (inputDisplay) inputDisplay.text = "PASS";
            PlaySound(successClip);
            OnCorrectPassword?.Invoke(); // ★ 여기에 문 열기 연결
        }
        else
        {
            // 오답
            if (inputDisplay) inputDisplay.text = "ERR";
            PlaySound(errorClip);
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