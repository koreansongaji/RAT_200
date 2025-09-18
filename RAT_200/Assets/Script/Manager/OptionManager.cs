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
}
