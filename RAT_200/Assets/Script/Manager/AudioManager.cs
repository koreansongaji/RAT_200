using UnityEngine;
using UnityEngine.Audio; // 추가
using System.Collections.Generic;

public class AudioManager : Singleton<AudioManager>
{
    public enum Sound
    {
        BGM,
        Effect,
        MaxCount,
    }

    [Header("Mixer Settings")]
    [SerializeField] public AudioMixer _audioMixer;

    public AudioSource[] _audioSources = new AudioSource[(int)Sound.MaxCount];
    private List<AudioSource> _bgmSources = new List<AudioSource>(); // 다중 BGM을 위한 리스트
    public Dictionary<string, AudioClip> _audioClips = new Dictionary<string, AudioClip>();

    public float MasterVolume => OptionManager.Instance.OptionData.MastersoundVolume;
    public float BGMVolume => OptionManager.Instance.OptionData.BGMsoundVolume;
    public float EffectVolume => OptionManager.Instance.OptionData.EffectsoundVolume;

    new private void Awake()
    {
        base.Awake();
        Init();
    }

    private void Start()
    {
    
    }

    public void PlayBgmWithFade(AudioClip clip, float fadeDuration = 1.0f)
    {
        if (clip == null) return;
        StartCoroutine(CoFadeBGM(clip, fadeDuration));
    }

    private System.Collections.IEnumerator CoFadeBGM(AudioClip clip, float duration)
    {
        // 다중 BGM 지원을 위해 새로운 BGM 소스 생성 또는 기존 유휴 소스 찾기
        AudioSource bgmSource = GetUnusedBgmSource();
        if (bgmSource == null)
        {
            Debug.LogError("[AudioManager] Failed to get or create a BGM source!");
            yield break;
        }

        bgmSource.clip = clip;
        bgmSource.volume = 0;
        bgmSource.Play();

        for (float t = 0; t < duration; t += Time.deltaTime)
        {
            bgmSource.volume = Mathf.Lerp(0, 1.0f, t / duration);
            yield return null;
        }

        bgmSource.volume = 1.0f;
    }

    private AudioSource GetUnusedBgmSource()
    {
        foreach (var source in _bgmSources)
        {
            if (source != null && !source.isPlaying) return source;
        }

        // 모든 소스가 사용 중이라면 가장 먼저 재생을 시작했던 소스를 재사용하거나 첫 번째 소스를 반환합니다.
        if (_bgmSources.Count > 0) return _bgmSources[0];

        // 만약 리스트가 비어있다면 에러 로그를 남기고 null을 반환합니다.
        Debug.LogError("[AudioManager] No BGM sources found in list and dynamic creation is disabled.");
        return null;
    }

    new private void OnApplicationQuit()
    {
        base.OnApplicationQuit();
    }
    public void Init()
    {
        string[] soundNames = System.Enum.GetNames(typeof(Sound));
        for (int i = 0; i < (int)Sound.MaxCount; i++)
        {
            // 이미 인스펙터에서 할당되어 있는지 확인
            if (_audioSources[i] != null)
            {
                // 할당되어 있다면 Mixer Group 재설정 확인
                if (_audioMixer != null && _audioSources[i].outputAudioMixerGroup == null)
                {
                    var groups = _audioMixer.FindMatchingGroups(soundNames[i]);
                    if (groups.Length > 0) _audioSources[i].outputAudioMixerGroup = groups[0];
                }
                
                // BGM 타입이면 리스트에 추가
                if ((Sound)i == Sound.BGM && !_bgmSources.Contains(_audioSources[i]))
                {
                    _bgmSources.Add(_audioSources[i]);
                }
                continue;
            }

            // 인스펙터에 없다면 AudioManager 오브젝트에서 찾아봅니다.
            _audioSources[i] = GetComponent<AudioSource>(); // 기본적으로 하나만 있다면 첫 번째 것을 가져옴
            
            // 만약 이름으로 구분해서 찾아야 한다면 (여러 개가 붙어있을 경우)
            AudioSource[] sources = GetComponents<AudioSource>();
            foreach (var s in sources)
            {
                if (s.gameObject.name == soundNames[i] || s.outputAudioMixerGroup?.name == soundNames[i])
                {
                    _audioSources[i] = s;
                    break;
                }
            }

            if (_audioSources[i] == null)
            {
                // 없으면 생성
                GameObject go = new GameObject { name = soundNames[i] };
                go.transform.parent = transform;
                _audioSources[i] = go.AddComponent<AudioSource>();
            }

            if (_audioMixer != null)
            {
                var groups = _audioMixer.FindMatchingGroups(soundNames[i]);
                if (groups.Length > 0) _audioSources[i].outputAudioMixerGroup = groups[0];
            }

            _audioSources[i].dopplerLevel = 0.0f;
            if ((Sound)i == Sound.BGM)
            {
                _audioSources[i].loop = true;
                if (!_bgmSources.Contains(_audioSources[i])) _bgmSources.Add(_audioSources[i]);
            }
        }

        // BGM 소스가 하나도 없다면 기본적으로 하나 생성
        if (_bgmSources.Count == 0 && _audioSources[(int)Sound.BGM] != null)
        {
            _bgmSources.Add(_audioSources[(int)Sound.BGM]);
        }

        if (_audioClips.Count == 0)
        {
            AudioClip[] clips = Resources.LoadAll<AudioClip>("Sounds");
            foreach (AudioClip clip in clips)
            {
                // Resources.LoadAll은 상대 경로를 반환하지 않으므로 이름을 키로 사용하거나 
                // 일관성을 위해 GetOrAddAudioClip과 동일한 규칙 적용
                string key = $"Sounds/{clip.name}";
                if (!_audioClips.ContainsKey(key))
                {
                    _audioClips.Add(key, clip);
                }
            }
            
            // 하위 폴더도 포함하여 로드 (필요시)
            AudioClip[] effectClips = Resources.LoadAll<AudioClip>("Sounds/Effect");
            foreach (AudioClip clip in effectClips)
            {
                string key = $"Sounds/Effect/{clip.name}";
                if (!_audioClips.ContainsKey(key)) _audioClips.Add(key, clip);
            }
            
            Debug.Log($"총 {_audioClips.Count}개의 사운드 리소스를 로드했습니다.");
        }
    }

    public void Clear()
    {
        foreach (AudioSource audioSource in _audioSources)
        {
            if (audioSource != null)
            {
                audioSource.clip = null;
                audioSource.Stop();
            }
        }
        foreach (AudioSource bgmSource in _bgmSources)
        {
            if (bgmSource != null)
            {
                bgmSource.clip = null;
                bgmSource.Stop();
            }
        }
        foreach (var loopSource in _loopSources.Values)
        {
            if (loopSource != null)
            {
                loopSource.Stop();
                loopSource.clip = null;
            }
        }
        _loopSources.Clear();
        _audioClips.Clear();
    }

    public void Play(AudioClip audioClip, Sound type = Sound.Effect, float pitch = 1.0f, float volumeScale = 1.0f)
    {
        if (audioClip == null) return;

        if (type == Sound.BGM)
        {
            AudioSource bgmSource = GetUnusedBgmSource();
            if (bgmSource == null)
            {
                Debug.LogWarning("[AudioManager] No BGM AudioSource available!");
                return;
            }
            if (bgmSource.clip == audioClip && bgmSource.isPlaying) return;
            bgmSource.pitch = pitch;
            bgmSource.clip = audioClip;
            bgmSource.volume = volumeScale;
            bgmSource.Play();
        }
        else
        {
            AudioSource audioSource = _audioSources[(int)type];
            if (audioSource == null)
            {
                Debug.LogWarning($"[AudioManager] AudioSource for {type} is not assigned!");
                return;
            }
            audioSource.pitch = pitch;
            audioSource.PlayOneShot(audioClip, volumeScale);
        }
    }

    /// <summary>
    /// 루프되는 효과음을 재생합니다. (예: 쥐의 이동 소리)
    /// </summary>
    public void PlayLoop(AudioClip clip, string loopKey, Sound type = Sound.Effect)
    {
        if (clip == null) return;
        
        // 이미 해당 키로 재생 중인 소스가 있다면 클립만 교체하거나 유지
        if (_loopSources.TryGetValue(loopKey, out AudioSource source))
        {
            if (source.clip != clip)
            {
                source.clip = clip;
                source.Play();
            }
            else if (!source.isPlaying)
            {
                source.Play();
            }
            return;
        }

        // 새로운 루프 소스 생성
        GameObject go = new GameObject { name = $"Loop_{loopKey}" };
        go.transform.parent = transform;
        source = go.AddComponent<AudioSource>();
        source.clip = clip;
        source.loop = true;
        source.dopplerLevel = 0.0f;

        // Mixer Group 할당
        if (_audioMixer != null)
        {
            string groupName = System.Enum.GetName(typeof(Sound), type);
            var groups = _audioMixer.FindMatchingGroups(groupName);
            if (groups.Length > 0) source.outputAudioMixerGroup = groups[0];
        }

        source.Play();
        _loopSources.Add(loopKey, source);
    }

    public void StopLoop(string loopKey)
    {
        if (_loopSources.TryGetValue(loopKey, out AudioSource source))
        {
            source.Stop();
            source.clip = null;
        }
    }

    private Dictionary<string, AudioSource> _loopSources = new Dictionary<string, AudioSource>();

    public void Play(string path, string _type = "Effect", float pitch = 1.0f, float volumeScale = 1.0f)
    {
        Sound type = Sound.Effect;
        if (_type == "BGM")
        {
            type = Sound.BGM;
        }
        else
        {
            type = Sound.Effect;
        }
        AudioClip audioClip = GetOrAddAudioClip(path, type);
        if (audioClip != null)
        {
            Play(audioClip, type, pitch, volumeScale);
        }
    }

    AudioClip GetOrAddAudioClip(string path, Sound type = Sound.Effect)
    {
        if (string.IsNullOrEmpty(path)) return null;

        if (path.Contains("Sounds/") == false)
            path = $"Sounds/{path}"; // Sound path add

        if (_audioClips.TryGetValue(path, out AudioClip audioClip))
            return audioClip;

        audioClip = Resources.Load<AudioClip>(path);
        
        if (audioClip != null)
        {
            _audioClips.Add(path, audioClip);
        }
        else
        {
            Debug.LogWarning($"[AudioManager] AudioClip Missing ! {path}");
        }

        return audioClip;
    }

    public void Loader()
    {
        //Debug.Log("사운드 매니저 로드");
    }
    // 볼륨 설정을 dB로 변환하여 Mixer에 적용
    public void SetMasterVolume(float volume)
    {
        if (_audioMixer != null)
        {
            bool success = _audioMixer.SetFloat("MasterVol", Mathf.Log10(Mathf.Max(0.0001f, volume)) * 20f);
            if (!success) Debug.LogWarning("[AudioManager] Failed to set 'MasterVol' parameter. Is it exposed and named correctly?");
        }
        else
        {
            Debug.LogWarning("[AudioManager] AudioMixer is not assigned!");
        }
    }

    public void SetBGMVolume(float volume)
    {
        if (_audioMixer != null)
        {
            bool success = _audioMixer.SetFloat("BGMVol", Mathf.Log10(Mathf.Max(0.0001f, volume)) * 20f);
            if (!success) Debug.LogWarning("[AudioManager] Failed to set 'BGMVol' parameter. Is it exposed and named correctly?");
        }
        else
        {
            Debug.LogWarning("[AudioManager] AudioMixer is not assigned!");
        }
    }

    public void SetEffectVolume(float volume)
    {
        if (_audioMixer != null)
        {
            bool success = _audioMixer.SetFloat("SFXVol", Mathf.Log10(Mathf.Max(0.0001f, volume)) * 20f);
            if (!success) Debug.LogWarning("[AudioManager] Failed to set 'SFXVol' parameter. Is it exposed and named correctly?");
        }
        else
        {
            Debug.LogWarning("[AudioManager] AudioMixer is not assigned!");
        }
    }

}
