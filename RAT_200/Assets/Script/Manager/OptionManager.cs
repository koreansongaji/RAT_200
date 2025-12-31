using UnityEngine;

public class OptionData
{
    public float MastersoundVolume;
    public float BGMsoundVolume;
    public float EffectsoundVolume;
    public OptionData(float masterVolume = 1f, float bgmVolume = 1f, float effectVolume = 1f)
    {
        MastersoundVolume = masterVolume;
        BGMsoundVolume = bgmVolume;
        EffectsoundVolume = effectVolume;
    }
}
public class OptionManager : Singleton<OptionManager>
{
    public OptionData OptionData;
    void Awake()
    {
        base.Awake();
        OptionData = new OptionData(1, 1, 1);
    }
    void Start()
    {
        // 초기 볼륨 실시간 적용
        if (OptionData != null)
        {
            ApplyAllVolumes();
        }
    }

    public void ApplyAllVolumes()
    {
        if (AudioManager.Instance == null) return;
        AudioManager.Instance.SetMasterVolume(OptionData.MastersoundVolume);
        AudioManager.Instance.SetBGMVolume(OptionData.BGMsoundVolume);
        AudioManager.Instance.SetEffectVolume(OptionData.EffectsoundVolume);
    }

    public void OnMasterVolumeChange(float volume)
    {
        OptionData.MastersoundVolume = volume;
        AudioManager.Instance.SetMasterVolume(volume);
    }

    public void OnEffectVolumeChange(float volume)
    {
        OptionData.EffectsoundVolume = volume;
        AudioManager.Instance.SetEffectVolume(volume);
    }

    public void OnBgmVolumeChange(float volume)
    {
        OptionData.BGMsoundVolume = volume;
        AudioManager.Instance.SetBGMVolume(volume);
    }
}
