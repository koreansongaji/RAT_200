using UnityEngine;
using System.Collections.Generic;

public class AudioManager : Singleton<AudioManager>
{
public enum Sound
    {
        BGM,
        Effect,
        MaxCount,
    }

    public AudioSource[] _audioSources = new AudioSource[(int)Sound.MaxCount];
    public Dictionary<string, AudioClip> _audioClips = new Dictionary<string, AudioClip>();

    public float MasterVolume => OptionManager.Instance.OptionData.MastersoundVolume;
    public float BGMVolume => OptionManager.Instance.OptionData.BGMsoundVolume; // 항상 원본 참조
    public float EffectVolume => OptionManager.Instance.OptionData.EffectsoundVolume; // 항상 원본 참조

    new private void Awake()
    {
        base.Awake();
        Init();
        // Clear(); <- 이 부분이 불러온 클립들을 다 지우고 있었습니다. 삭제하세요!
    }

    private void Start()
    {
    
    }

    private Coroutine _bgmFadeCoroutine;

    public void PlayBgmWithFade(AudioClip clip, float fadeDuration = 1.0f)
    {
        if (clip == null) return;
        if (_bgmFadeCoroutine != null) StopCoroutine(_bgmFadeCoroutine);
        _bgmFadeCoroutine = StartCoroutine(CoFadeBGM(clip, fadeDuration));
    }

    private System.Collections.IEnumerator CoFadeBGM(AudioClip clip, float duration)
    {
        AudioSource bgmSource = _audioSources[(int)Sound.BGM];
        float targetVolume = Mathf.Clamp01(MasterVolume * BGMVolume);

        // 1. 기존 소리가 있다면 페이드 아웃
        if (bgmSource.isPlaying)
        {
            float startVolume = bgmSource.volume;
            for (float t = 0; t < duration; t += Time.deltaTime)
            {
                bgmSource.volume = Mathf.Lerp(startVolume, 0, t / duration);
                yield return null;
            }
        }

        // 2. 곡 교체 및 페이드 인
        bgmSource.clip = clip;
        bgmSource.Play();

        for (float t = 0; t < duration; t += Time.deltaTime)
        {
            bgmSource.volume = Mathf.Lerp(0, targetVolume, t / duration);
            yield return null;
        }

        bgmSource.volume = targetVolume;
        _bgmFadeCoroutine = null;
    }

    new private void OnApplicationQuit()
    {
        base.OnApplicationQuit();
    }
    public void Init()
    {
        GameObject root = GameObject.Find("Sound");
        if (root == null)
        {
            root = new GameObject { name = "Sound" };
            Object.DontDestroyOnLoad(root);
            
            // Enum 순회 시 MaxCount 제외
            string[] soundNames = System.Enum.GetNames(typeof(Sound));
            for (int i = 0; i < (int)Sound.MaxCount; i++)
            {
                GameObject go = new GameObject { name = soundNames[i] };
                _audioSources[i] = go.AddComponent<AudioSource>();
                _audioSources[i].dopplerLevel = 0.0f;
                _audioSources[i].reverbZoneMix = 0.0f;
                go.transform.parent = root.transform;
            }

            _audioSources[(int)Sound.BGM].loop = true;
        }

        // 딕셔너리가 비어있을 때만 리소스를 로드합니다.
        if (_audioClips.Count == 0)
        {
            AudioClip[] clips = Resources.LoadAll<AudioClip>("Sounds");
            foreach (AudioClip clip in clips)
            {
                // GetOrAddAudioClip 로직과 일치시키기 위해 "Sounds/" 접두사를 붙여 저장할 수 있습니다.
                // 혹은 파일 이름만 키로 사용해도 됩니다. 여기서는 기존 코드 호환성을 위해 경로를 맞춥니다.
                string key = $"Sounds/{clip.name}";
                if (!_audioClips.ContainsKey(key))
                {
                    _audioClips.Add(key, clip);
                }
            }
            Debug.Log($"총 {_audioClips.Count}개의 사운드 리소스를 로드했습니다.");
        }
    }

    public void Clear()
    {
        // All sounds stop
        foreach (AudioSource audioSource in _audioSources)
        {
            audioSource.clip = null;
            audioSource.Stop();
        }
        // Sounds Dictionary clear
        _audioClips.Clear();
    }

    // 세 번째 매개변수로 volumeScale을 추가하고 기본값을 1.0f로 설정합니다.
    public void Play(AudioClip audioClip, Sound type = Sound.Effect, float pitch = 1.0f, float volumeScale = 1.0f)
    {
        if (audioClip == null)
        {
            return;
        }

        if (type == Sound.BGM) // BGM start
        {
            AudioSource audioSource = _audioSources[(int)Sound.BGM];
            if (audioSource.isPlaying) // If other sounds playing, stop
            {
                audioSource.Stop();
            }

            audioSource.volume = Mathf.Clamp01(MasterVolume * BGMVolume); // Master 적용
            audioSource.pitch = pitch; // BGM start
            audioSource.clip = audioClip;
            audioSource.Play();
        }
        else // Effect start
        {
            AudioSource audioSource = _audioSources[(int)Sound.Effect];
            // 최종 볼륨에 volumeScale을 곱해줍니다.
            float finalVolume = Mathf.Clamp01(MasterVolume * EffectVolume) * volumeScale;
            audioSource.pitch = pitch;
            audioSource.PlayOneShot(audioClip, finalVolume);
        }
    }

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
        // 아래의 Play 함수 호출 시 volumeScale을 전달합니다.
        Play(audioClip, type, pitch, volumeScale);
    }

    AudioClip GetOrAddAudioClip(string path, Sound type = Sound.Effect)
    {
        if (path.Contains("Sounds/") == false)
            path = $"Sounds/{path}"; // Sound path add

        AudioClip audioClip = null;

        if (type == Sound.BGM) // BGM save
        {
            audioClip = Resources.Load<AudioClip>(path);
        }
        else // Effect save
        {
            if (_audioClips.TryGetValue(path, out audioClip) == false)
            {
                audioClip = Resources.Load<AudioClip>(path);
                _audioClips.Add(path, audioClip);
            }
        }

        if (audioClip == null)
        {
            Debug.Log($"AudioClip Missing ! {path}");
        }

        return audioClip;
    }

    public void Loader()
    {
        //Debug.Log("사운드 매니저 로드");
    }
    
    public void SetMasterVolume(float volume)
    {
        OptionManager.Instance.OptionData.MastersoundVolume = Mathf.Clamp01(volume);
        // 현재 재생 중인 모든 소스에 반영
        var bgm = _audioSources[(int)Sound.BGM];
        var eff = _audioSources[(int)Sound.Effect];
        if (bgm != null) bgm.volume = Mathf.Clamp01(MasterVolume * BGMVolume);
        if (eff != null) eff.volume = Mathf.Clamp01(MasterVolume * EffectVolume);
    }

    public void SetBGMVolume(float volume)
    {
        OptionManager.Instance.OptionData.BGMsoundVolume = Mathf.Clamp01(volume);
        if (_audioSources[(int)Sound.BGM] != null)
        {
            _audioSources[(int)Sound.BGM].volume = Mathf.Clamp01(MasterVolume * BGMVolume); // Master 적용
        }
    }

    public void SetEffectVolume(float volume)
    {
        OptionManager.Instance.OptionData.EffectsoundVolume = Mathf.Clamp01(volume);
        if (_audioSources[(int)Sound.Effect] != null)
        {
            _audioSources[(int)Sound.Effect].volume = Mathf.Clamp01(MasterVolume * EffectVolume); // Master 적용
        }
    }
}
