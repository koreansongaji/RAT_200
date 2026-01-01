using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System;

public class HowToPlayPage : MonoBehaviour
{
    [Header("Positions")]
    [SerializeField] private Transform _inPos;  // 화면 안쪽 위치 (보일 때)
    [SerializeField] private Transform _outPos; // 화면 바깥 위치 (숨길 때)

    [Header("Refs")]
    [SerializeField] private TitlePage _titlePage; // 뒤로가기 시 타이틀을 부르기 위해
    [SerializeField] private Button _backButton;   // 뒤로가기 버튼

    private Tween _tween;

    private void Awake()
    {
        // 시작할 때는 화면 바깥으로 보내둡니다.
        if (_outPos) transform.position = _outPos.position;

        if (_backButton)
        {
            _backButton.onClick.RemoveAllListeners();
            _backButton.onClick.AddListener(OnBackClicked);
        }
    }

    private void Update()
    {
        // 설명창이 켜져있을 때 ESC를 누르면 뒤로가기 (UX 편의성)
        // 화면 안쪽 위치 근처에 있을 때만 작동하도록 거리 체크
        if (_inPos && Vector3.Distance(transform.position, _inPos.position) < 0.1f)
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                OnBackClicked();
            }
        }
    }

    // 뒤로가기 버튼 기능
    public void OnBackClicked()
    {
        // 1. 나 자신(HowToPlay)은 나간다.
        SlideOut(() =>
        {
            // 2. 다 나가면 타이틀을 다시 부른다.
            _titlePage.SlideInTitlePage();
        });
    }

    public void SlideIn(Action onComplete = null)
    {
        _tween?.Kill();
        // 들어올 때는 InPos로 이동
        _tween = transform.DOMove(_inPos.position, 0.5f)
            .SetEase(Ease.OutCubic)
            .SetUpdate(true)
            .OnComplete(() => onComplete?.Invoke());
    }

    public void SlideOut(Action onComplete = null)
    {
        _tween?.Kill();
        // 나갈 때는 OutPos로 이동
        _tween = transform.DOMove(_outPos.position, 0.5f)
            .SetEase(Ease.InOutQuad)
            .SetUpdate(true)
            .OnComplete(() => onComplete?.Invoke());
    }
}