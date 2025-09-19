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
        // 초기 대입 제거
        Init();
        Clear();
    }

    private void Start()
    {
        
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
            string[] soundNames = System.Enum.GetNames(typeof(Sound)); // "Bgm", "Effect"
            for (int i = 0; i < soundNames.Length - 1; i++)
            {
                GameObject go = new GameObject { name = soundNames[i] };
                _audioSources[i] = go.AddComponent<AudioSource>();
                _audioSources[i].dopplerLevel = 0.0f;
                _audioSources[i].reverbZoneMix = 0.0f;
                go.transform.parent = root.transform;
            }

            _audioSources[(int)Sound.BGM].loop = true; // bgm play
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

    public void Play(AudioClip audioClip, Sound type = Sound.Effect, float pitch = 1.0f)
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
            audioSource.volume = Mathf.Clamp01(MasterVolume * EffectVolume); // Master 적용
            audioSource.pitch = pitch;
            audioSource.PlayOneShot(audioClip, Mathf.Clamp01(MasterVolume * EffectVolume)); // PlayOneShot 볼륨도 적용
        }
    }

    public void Play(string path, string _type = "Effect", float pitch = 1.0f)
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
        Play(audioClip, type, pitch);
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
