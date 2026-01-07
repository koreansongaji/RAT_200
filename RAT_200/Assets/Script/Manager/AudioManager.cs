using UnityEngine;
using UnityEngine.Audio;
using System.Collections.Generic;

public class AudioManager : Singleton<AudioManager>
{
    public enum Sound { BGM, Effect }

    [Header("Mixer Settings")]
    public AudioMixer audioMixer;
    [SerializeField] private bool UseAudioMixer = false;

    // 현재 재생 중인 BGM AudioSource (하나만 재생된다고 가정)
    private AudioSource _currentBGM = null;
    private float _currentBGMRelativeVolume = 1f;  // BGM AudioSource의 개별 볼륨 비율 (volumeScale)

    // 재생 중인 루프 사운드들 (loopKey -> AudioSource)
    private Dictionary<string, AudioSource> _loopSources = new Dictionary<string, AudioSource>();

    // 일시재생(One-shot) 중인 효과음 목록 (파괴 처리 및 볼륨조정을 위해 관리)
    private List<(AudioSource source, float relativeVol)> _activeOneShots = new List<(AudioSource, float)>();

    // 오디오클립 캐싱 (path -> AudioClip)
    private Dictionary<string, AudioClip> _audioClips = new Dictionary<string, AudioClip>();

    // AudioMixer 그룹 (UseAudioMixer 옵션이 켜진 경우에 사용)
    private AudioMixerGroup _bgmGroup;
    private AudioMixerGroup _sfxGroup;

    // 현재 설정된 볼륨 값 (OptionManager로부터 업데이트)
    private float _masterVolume = 1f;
    private float _bgmVolume = 1f;
    private float _effectVolume = 1f;

    private Coroutine _bgmFadeCo;
    private AudioSource _fadingBGM;  
    
    private void Awake()
    {
        // Singleton 기반 초기화
        base.Awake();  // Singleton<T> 기반일 경우 호출

        // AudioMixer 그룹 캐싱
        if (audioMixer != null && UseAudioMixer)
        {
            var bgmGroups = audioMixer.FindMatchingGroups("BGM");
            if (bgmGroups.Length > 0) _bgmGroup = bgmGroups[0];
            else Debug.LogWarning("AudioManager: 'BGM' MixerGroup을 찾을 수 없습니다.");

            var sfxGroups = audioMixer.FindMatchingGroups("SFX");
            if (sfxGroups.Length > 0) _sfxGroup = sfxGroups[0];
            else Debug.LogWarning("AudioManager: 'SFX' MixerGroup을 찾을 수 없습니다.");
        }

        // OptionManager의 초기 볼륨값 가져오기
        if (OptionManager.Instance != null)
        {
            _masterVolume = OptionManager.Instance.OptionData.MastersoundVolume;
            _bgmVolume    = OptionManager.Instance.OptionData.BGMsoundVolume;
            _effectVolume = OptionManager.Instance.OptionData.EffectsoundVolume;
        }
    }

    /// <summary>
    /// BGM 또는 효과음(AudioClip)을 재생한다. 필요시 AudioSource를 자동 생성한다.
    /// </summary>
    public void Play(AudioClip clip, Sound type = Sound.Effect, float pitch = 1f, float volumeScale = 1f)
    {
        if (clip == null) return;

        if (type == Sound.BGM)
        {
            // 기존에 재생 중인 BGM이 있다면 정지 및 제거
            if (_currentBGM != null)
            {
                if (_currentBGM.clip == clip && _currentBGM.isPlaying)
                {
                    // 같은 클립이 이미 재생 중이면 다시 재생하지 않음
                    return;
                }
                _currentBGM.Stop();                 // 현재 재생 중인 BGM 정지
                Destroy(_currentBGM.gameObject);    // 오디오 소스 객체 제거
                _currentBGM = null;
            }

            // 새로운 BGM AudioSource 생성
            GameObject bgmObject = new GameObject($"BGM_Source_{clip.name}");
            bgmObject.transform.SetParent(transform);
            AudioSource bgmSource = bgmObject.AddComponent<AudioSource>();
            bgmSource.loop = true;
            bgmSource.clip = clip;
            bgmSource.pitch = pitch;
            bgmSource.dopplerLevel = 0f;
            bgmSource.spatialBlend = 0f;

            // AudioMixer 그룹 설정 (옵션 활성화된 경우)
            if (UseAudioMixer && audioMixer != null && _bgmGroup != null)
            {
                bgmSource.outputAudioMixerGroup = _bgmGroup;
                // Mixer가 볼륨을 관리하므로 AudioSource 개별 볼륨은 relative 값만 설정
                bgmSource.volume = volumeScale;
            }
            else
            {
                // Mixer 미사용: Master * BGM * 개별 볼륨비율
                bgmSource.volume = _masterVolume * _bgmVolume * volumeScale;
            }

            // BGM 재생 시작
            bgmSource.Play();
            _currentBGM = bgmSource;
            _currentBGMRelativeVolume = volumeScale;
        }
        else // Sound.Effect (1회 재생 효과음)
        {
            // 새로운 효과음 AudioSource 생성
            GameObject sfxObject = new GameObject($"SFX_Source_{clip.name}");
            sfxObject.transform.SetParent(transform);
            AudioSource sfxSource = sfxObject.AddComponent<AudioSource>();
            sfxSource.loop = false;
            sfxSource.clip = clip;
            sfxSource.pitch = pitch;
            sfxSource.dopplerLevel = 0f;
            sfxSource.spatialBlend = 0f;

            // AudioMixer 그룹 설정
            if (UseAudioMixer && audioMixer != null && _sfxGroup != null)
            {
                sfxSource.outputAudioMixerGroup = _sfxGroup;
                sfxSource.volume = volumeScale;
            }
            else
            {
                sfxSource.volume = _masterVolume * _effectVolume * volumeScale;
            }

            // 효과음 재생 및 파괴 스케줄
            sfxSource.Play();
            // 재생 완료 후 오브젝트 제거 (클립 길이에 기반하여 제거)
            float destroyTime = (sfxSource.clip != null ? sfxSource.clip.length : 0f);
            if (sfxSource.pitch != 0f) destroyTime /= sfxSource.pitch;
            destroyTime += 0.1f;  // 약간의 여유 시간
            Destroy(sfxObject, destroyTime);

            // 리스트에 추가 (현재 재생 중인 one-shot 효과음 관리)
            _activeOneShots.Add((sfxSource, volumeScale));
        }
    }

    /// <summary>
    /// 반복 재생 효과음을 재생한다. loopKey 별로 하나의 AudioSource가 관리된다.
    /// </summary>
    public void PlayLoop(AudioClip clip, string loopKey, Sound type = Sound.Effect)
    {
        if (clip == null || string.IsNullOrEmpty(loopKey)) return;

        // 이미 해당 키로 재생 중인 경우 처리
        if (_loopSources.TryGetValue(loopKey, out AudioSource existingSource))
        {
            if (existingSource != null)
            {
                if (existingSource.clip == clip)
                {
                    if (!existingSource.isPlaying)
                        existingSource.Play();  // 일시 정지 상태였다면 재개
                    // 같은 클립 재생 중이면 별도 조치 없음
                }
                else
                {
                    // 클립이 변경된 경우 새로운 클립으로 교체 후 재생
                    existingSource.clip = clip;
                    existingSource.Play();
                }
            }
            return;
        }

        // 새로운 루프 AudioSource 생성
        GameObject loopObject = new GameObject($"Loop_{loopKey}");
        loopObject.transform.SetParent(transform);
        AudioSource loopSource = loopObject.AddComponent<AudioSource>();
        loopSource.loop = true;
        loopSource.clip = clip;
        loopSource.pitch = 1f;
        loopSource.dopplerLevel = 0f;
        loopSource.spatialBlend = 0f;

        // AudioMixer 그룹 설정
        if (UseAudioMixer && audioMixer != null)
        {
            AudioMixerGroup group = (type == Sound.BGM ? _bgmGroup : _sfxGroup);
            if (group != null) loopSource.outputAudioMixerGroup = group;
            loopSource.volume = 1f;  // 루프 효과음 개별볼륨 (필요시 조정 가능)
        }
        else
        {
            if (type == Sound.BGM)
                loopSource.volume = _masterVolume * _bgmVolume;
            else
                loopSource.volume = _masterVolume * _effectVolume;
        }

        loopSource.Play();
        _loopSources.Add(loopKey, loopSource);
    }

    /// <summary>
    /// 지정한 loopKey의 루프 사운드를 정지시킨다.
    /// </summary>
    public void StopLoop(string loopKey)
    {
        if (_loopSources.TryGetValue(loopKey, out AudioSource loopSource))
        {
            if (loopSource != null)
            {
                loopSource.Stop();
                Destroy(loopSource.gameObject);
            }
            _loopSources.Remove(loopKey);
        }
    }

    /// <summary>
    /// 모든 사운드 정지 및 AudioSource 정리 (BGM, Effect, Loop 전부).
    /// </summary>
    public void Clear()
    {
        // BGM 정지 및 제거
        if (_currentBGM != null)
        {
            _currentBGM.Stop();
            Destroy(_currentBGM.gameObject);
            _currentBGM = null;
        }

        // 루프 사운드 정지 및 제거
        foreach (var kv in _loopSources)
        {
            if (kv.Value != null)
            {
                kv.Value.Stop();
                Destroy(kv.Value.gameObject);
            }
        }
        _loopSources.Clear();

        // 재생 중인 1회성 효과음 제거
        foreach (var (source, _) in _activeOneShots)
        {
            if (source != null)
            {
                source.Stop();
                Destroy(source.gameObject);
            }
        }
        _activeOneShots.Clear();

        // 로드된 AudioClip 캐시 비우기
        _audioClips.Clear();
    }

    /// <summary>
    /// 오디오 클립 경로로 재생 (필요시 Resources에서 로드).
    /// </summary>
    public void Play(string path, string type = "Effect", float pitch = 1f, float volumeScale = 1f)
    {
        Sound soundType = (type == "BGM") ? Sound.BGM : Sound.Effect;
        AudioClip clip = GetOrAddAudioClip(path);
        if (clip != null)
        {
            Play(clip, soundType, pitch, volumeScale);
        }
    }

    private AudioClip GetOrAddAudioClip(string path)
    {
        if (string.IsNullOrEmpty(path)) return null;
        // 경로에 "Sounds/" 프리픽스가 없으면 추가
        if (!path.Contains("Sounds/"))
            path = $"Sounds/{path}";

        if (_audioClips.TryGetValue(path, out AudioClip cachedClip))
        {
            return cachedClip;
        }

        // Resources 폴더에서 AudioClip 로드
        AudioClip clip = Resources.Load<AudioClip>(path);
        if (clip != null)
        {
            _audioClips.Add(path, clip);
        }
        else
        {
            Debug.LogWarning($"AudioManager: AudioClip을 찾을 수 없습니다! (경로: {path})");
        }
        return clip;
    }

    // ===== 볼륨 설정 메서드 (OptionManager에서 호출) =====

    public void SetMasterVolume(float volume)
    {
        // 0~1 범위의 volume을 dB로 환산하여 AudioMixer에 적용
        if (audioMixer != null && UseAudioMixer)
        {
            bool result = audioMixer.SetFloat("MasterVol", Mathf.Log10(Mathf.Max(volume, 0.0001f)) * 20f);
            if (!result) Debug.LogWarning("AudioManager: 'MasterVol' 파라미터 설정 실패 (Mixer에 exposed 여부 확인).");
        }

        // 현재 볼륨 값 업데이트 및 AudioSource들에 적용
        if (volume == _masterVolume)
        {
            _masterVolume = volume;
            return; // 변경 없음
        }
        float oldMaster = _masterVolume;
        _masterVolume = volume;

        if (UseAudioMixer && audioMixer != null)
        {
            // Mixer 사용 시 개별 AudioSource volume은 relativeVolume 유지 (전역볼륨 Mixer에서 처리)
            return;
        }

        // Mixer 미사용 시 직접 AudioSource들의 볼륨 조정
        if (oldMaster <= 0f)
        {
            // 이전 마스터 볼륨이 0이었다면 각 소리별 새 볼륨 재계산
            if (_currentBGM != null)
            {
                _currentBGM.volume = _masterVolume * _bgmVolume * _currentBGMRelativeVolume;
            }
            foreach (var kv in _loopSources)
            {
                AudioSource src = kv.Value;
                if (src == null) continue;
                // loopSources에서는 type 구분 없이 Effect가 대부분이라고 가정
                // (만약 BGM 타입의 loop 사용 시 필요하면 조건 추가)
                src.volume = _masterVolume * _effectVolume * 1f;
            }
            for (int i = 0; i < _activeOneShots.Count; ++i)
            {
                var (src, relVol) = _activeOneShots[i];
                if (src == null) 
                {
                    _activeOneShots.RemoveAt(i--);
                }
                else
                {
                    src.volume = _masterVolume * _effectVolume * relVol;
                }
            }
        }
        else
        {
            // 변경된 비율만큼 곱해서 적용
            float masterRatio = (_masterVolume > 0f) ? (_masterVolume / oldMaster) : 0f;
            if (_currentBGM != null) _currentBGM.volume *= masterRatio;
            foreach (var kv in _loopSources)
            {
                AudioSource src = kv.Value;
                if (src != null) src.volume *= masterRatio;
            }
            for (int i = 0; i < _activeOneShots.Count; ++i)
            {
                var (src, _) = _activeOneShots[i];
                if (src == null)
                {
                    _activeOneShots.RemoveAt(i--);
                }
                else
                {
                    src.volume *= masterRatio;
                }
            }
        }
    }

    public void SetBGMVolume(float volume)
    {
        if (audioMixer != null && UseAudioMixer)
        {
            bool result = audioMixer.SetFloat("BGMVol", Mathf.Log10(Mathf.Max(volume, 0.0001f)) * 20f);
            if (!result) Debug.LogWarning("AudioManager: 'BGMVol' 파라미터 설정 실패.");
        }

        if (volume == _bgmVolume)
        {
            _bgmVolume = volume;
            return;
        }
        float oldBgm = _bgmVolume;
        _bgmVolume = volume;

        if (UseAudioMixer && audioMixer != null)
        {
            return;
        }

        if (oldBgm <= 0f)
        {
            // 이전 BGM 볼륨이 0 -> 새 볼륨 계산
            if (_currentBGM != null)
            {
                _currentBGM.volume = _masterVolume * _bgmVolume * _currentBGMRelativeVolume;
            }
        }
        else
        {
            float bgmRatio = (_bgmVolume > 0f) ? (_bgmVolume / oldBgm) : 0f;
            if (_currentBGM != null) _currentBGM.volume *= bgmRatio;
        }
    }

    public void SetEffectVolume(float volume)
    {
        if (audioMixer != null && UseAudioMixer)
        {
            bool result = audioMixer.SetFloat("SFXVol", Mathf.Log10(Mathf.Max(volume, 0.0001f)) * 20f);
            if (!result) Debug.LogWarning("AudioManager: 'SFXVol' 파라미터 설정 실패.");
        }

        if (volume == _effectVolume)
        {
            _effectVolume = volume;
            return;
        }
        float oldEffect = _effectVolume;
        _effectVolume = volume;

        if (UseAudioMixer && audioMixer != null)
        {
            return;
        }

        if (oldEffect <= 0f)
        {
            // 이전 Effect 볼륨이 0 -> 새 볼륨 계산
            foreach (var kv in _loopSources)
            {
                AudioSource src = kv.Value;
                if (src != null)
                    src.volume = _masterVolume * _effectVolume * 1f;
            }
            for (int i = 0; i < _activeOneShots.Count; ++i)
            {
                var (src, relVol) = _activeOneShots[i];
                if (src == null)
                {
                    _activeOneShots.RemoveAt(i--);
                }
                else
                {
                    src.volume = _masterVolume * _effectVolume * relVol;
                }
            }
        }
        else
        {
            float effectRatio = (_effectVolume > 0f) ? (_effectVolume / oldEffect) : 0f;
            foreach (var kv in _loopSources)
            {
                AudioSource src = kv.Value;
                if (src != null) src.volume *= effectRatio;
            }
            for (int i = 0; i < _activeOneShots.Count; ++i)
            {
                var (src, _) = _activeOneShots[i];
                if (src == null)
                {
                    _activeOneShots.RemoveAt(i--);
                }
                else
                {
                    src.volume *= effectRatio;
                }
            }
        }
    }
    
    public void KillAllSounds()
    {
        // ===== BGM =====
        if (_currentBGM != null)
        {
            _currentBGM.Stop();
            Destroy(_currentBGM.gameObject);
            _currentBGM = null;
        }

        // ===== Loop Sounds =====
        foreach (var kv in _loopSources)
        {
            AudioSource src = kv.Value;
            if (src != null)
            {
                src.Stop();
                Destroy(src.gameObject);
            }
        }
        _loopSources.Clear();

        // ===== One-shot Effects =====
        for (int i = 0; i < _activeOneShots.Count; ++i)
        {
            var (src, _) = _activeOneShots[i];
            if (src != null)
            {
                src.Stop();
                Destroy(src.gameObject);
            }
        }
        _activeOneShots.Clear();
        
        if (_bgmFadeCo != null)
        {
            StopCoroutine(_bgmFadeCo);
            _bgmFadeCo = null;
        }

        if (_fadingBGM != null)
        {
            _fadingBGM.Stop();
            Destroy(_fadingBGM.gameObject);
            _fadingBGM = null;
        }
    }
    
    public void PlayBgmWithFade(AudioClip clip, float fadeDuration = 1.0f, float pitch = 1f, float volumeScale = 1f)
    {
        if (clip == null) return;

        // 같은 클립이 이미 재생 중이면 무시
        if (_currentBGM != null && _currentBGM.isPlaying && _currentBGM.clip == clip)
            return;

        // 이전 페이드 코루틴 정리
        if (_bgmFadeCo != null)
        {
            StopCoroutine(_bgmFadeCo);
            _bgmFadeCo = null;

            // 페이드 코루틴이 중간에 끊겼다면 남아있는 fadingBGM도 정리
            if (_fadingBGM != null)
            {
                if (_fadingBGM) Destroy(_fadingBGM.gameObject);
                _fadingBGM = null;
            }
        }

        // 기존 BGM이 있으면 페이드 아웃 대상으로 보관
        _fadingBGM = _currentBGM;

        // 새 BGM 소스 만들기
        AudioSource newBgm = CreateBgmSource($"BGM_Source_{clip.name}");
        newBgm.clip = clip;
        newBgm.pitch = pitch;

        // 목표 볼륨 계산
        float targetVol;
        if (UseAudioMixer && audioMixer != null)
            targetVol = Mathf.Clamp01(volumeScale); // Mixer가 전체 볼륨 처리 → relative만
        else
            targetVol = Mathf.Clamp01(_masterVolume * _bgmVolume * volumeScale);

        newBgm.volume = 0f;
        newBgm.Play();

        _currentBGM = newBgm;
        _currentBGMRelativeVolume = volumeScale;

        // fadeDuration 0이면 즉시 교체
        if (fadeDuration <= 0f)
        {
            _currentBGM.volume = targetVol;

            if (_fadingBGM != null)
            {
                _fadingBGM.Stop();
                Destroy(_fadingBGM.gameObject);
                _fadingBGM = null;
            }
            return;
        }

        _bgmFadeCo = StartCoroutine(CoCrossFadeBgm(_fadingBGM, newBgm, targetVol, fadeDuration));
    }

    private System.Collections.IEnumerator CoCrossFadeBgm(AudioSource from, AudioSource to, float toTargetVol, float duration)
    {
        float t = 0f;

        float fromStartVol = (from != null) ? from.volume : 0f;
        float toStartVol = to.volume; // 보통 0

        while (t < duration)
        {
            t += Time.deltaTime;
            float a = Mathf.Clamp01(t / duration);

            if (to != null)
                to.volume = Mathf.Lerp(toStartVol, toTargetVol, a);

            if (from != null)
                from.volume = Mathf.Lerp(fromStartVol, 0f, a);

            yield return null;
        }

        if (to != null) to.volume = toTargetVol;

        if (from != null)
        {
            from.volume = 0f;
            from.Stop();
            Destroy(from.gameObject);
        }

        _fadingBGM = null;
        _bgmFadeCo = null;
    }
    private AudioSource CreateBgmSource(string name)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(transform);

        AudioSource src = go.AddComponent<AudioSource>();
        src.loop = true;
        src.dopplerLevel = 0f;
        src.spatialBlend = 0f;
        src.playOnAwake = false;

        // Mixer 그룹 할당 (옵션 켜져있을 때만)
        if (UseAudioMixer && audioMixer != null && _bgmGroup != null)
            src.outputAudioMixerGroup = _bgmGroup;

        return src;
    }
    
    public void StopSFX()
    {
        // ===== One-shot 효과음 =====
        for (int i = 0; i < _activeOneShots.Count; ++i)
        {
            var (src, _) = _activeOneShots[i];
            if (src != null)
            {
                src.Stop();
                Destroy(src.gameObject);
            }
        }
        _activeOneShots.Clear();

        // ===== Loop 효과음 =====
        foreach (var kv in _loopSources)
        {
            AudioSource src = kv.Value;
            if (src != null)
            {
                src.Stop();
                Destroy(src.gameObject);
            }
        }
        _loopSources.Clear();
    }
}