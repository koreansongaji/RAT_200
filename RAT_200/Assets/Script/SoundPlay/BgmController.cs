using System;
using UnityEngine;

public class BgmController : MonoBehaviour
{
    [Header("BGM Clips")]
    [SerializeField] private AudioClip _defaultBGM;    // 0~79 구간
    [SerializeField] private AudioClip _intenseBGM;    // 80~99 구간
    [SerializeField] private AudioClip _researcherBGM; // 100 구간

    [SerializeField] private NoiseBandSignal _noiseBandSignal;
    [SerializeField] private float _fadeDuration = 1.0f;  // BGM 전환 페이드 시간 (기본 1초)

    private int _currentStep = -1;
    private bool _isStarted = false;

    private void Awake()
    {
        if(_defaultBGM == null) 
            _defaultBGM = Resources.Load<AudioClip>("Sounds/BGM/bgm1_ambience_normal");
        if(_intenseBGM == null) 
            _intenseBGM = Resources.Load<AudioClip>("Sounds/BGM/bgm1_ambience_intense");
        if(_researcherBGM == null) 
            _researcherBGM = Resources.Load<AudioClip>("Sounds/BGM/scientist_bgm");
        
        if(_noiseBandSignal == null) 
            _noiseBandSignal = GetComponent<NoiseBandSignal>();
    }

    /// <summary>
    /// 수치에 따라 BGM을 체크하고 변경합니다.
    /// </summary>
    public void CheckBgmStep()
    {
        if (!_isStarted) return;

        float value = _noiseBandSignal.currentPercent;
        int nextStep;
        
        if (value < 100)
            nextStep = 1;
        else
            nextStep = 2;

        // 단계가 바뀌었을 때만 새로운 BGM 재생
        if (_currentStep != nextStep)
        {
            _currentStep = nextStep;
            PlayStepBGM(_currentStep);
        }
    }
    
    /// <summary>
    /// 게임 시작 시 첫 번째 BGM 단계를 강제로 재생합니다.
    /// </summary>
    public void PlayFirstStep()
    {
        _isStarted = true;
        _currentStep = 1;
        PlayStepBGM(1);
    }

    private void PlayStepBGM(int step)
    {
        AudioClip clipToPlay = null;

        switch (step)
        {
            case 1: clipToPlay = _defaultBGM; break;
            case 2: clipToPlay = _researcherBGM; break;
        }

        if (clipToPlay != null)
        {
            // 부드러운 전환을 위해 페이드 인/아웃 기능으로 BGM 재생 (_fadeDuration 동안 페이드)
            AudioManager.Instance.PlayBgmWithFade(clipToPlay, _fadeDuration);
        }
    }
}
