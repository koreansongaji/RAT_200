using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class GlobalSettings : MonoBehaviour
{
    // 씬 리로드 시에도 파괴되지 않게 하고 싶다면 사용(선택)
    // 여기서는 PlayerPrefs를 쓰므로 매 씬마다 이 스크립트가 있어도 상관없음

    [Header("Audio Mixer")]
    public AudioMixer audioMixer; // Master, BGM, SFX 그룹이 있는 믹서

    [Header("UI Refs (옵션 창의 슬라이더들)")]
    public Slider masterSlider;
    public Slider bgmSlider;
    public Slider sfxSlider;

    // 저장 키값
    const string KEY_MASTER = "Vol_Master";
    const string KEY_BGM = "Vol_BGM";
    const string KEY_SFX = "Vol_SFX";

    void Start()
    {
        // 1. 저장된 값 불러오기 (없으면 기본값 1.0f)
        float m = PlayerPrefs.GetFloat(KEY_MASTER, 1f);
        float b = PlayerPrefs.GetFloat(KEY_BGM, 1f);
        float s = PlayerPrefs.GetFloat(KEY_SFX, 1f);

        // 2. 사운드 적용
        ApplyVolume("Master", m);
        ApplyVolume("BGM", b);
        ApplyVolume("SFX", s);

        // 3. UI 슬라이더 연동 (옵션 창이 있다면)
        if (masterSlider) { masterSlider.value = m; masterSlider.onValueChanged.AddListener(v => SetMaster(v)); }
        if (bgmSlider) { bgmSlider.value = b; bgmSlider.onValueChanged.AddListener(v => SetBGM(v)); }
        if (sfxSlider) { sfxSlider.value = s; sfxSlider.onValueChanged.AddListener(v => SetSFX(v)); }
    }

    // --- 외부 연결용 함수 ---
    public void SetMaster(float val) { ApplyVolume("Master", val); PlayerPrefs.SetFloat(KEY_MASTER, val); }
    public void SetBGM(float val) { ApplyVolume("BGM", val); PlayerPrefs.SetFloat(KEY_BGM, val); }
    public void SetSFX(float val) { ApplyVolume("SFX", val); PlayerPrefs.SetFloat(KEY_SFX, val); }

    // 실제 믹서 적용 (Log 스케일 변환)
    void ApplyVolume(string paramName, float linearVal)
    {
        if (!audioMixer) return;
        float db = (linearVal <= 0.001f) ? -80f : Mathf.Log10(linearVal) * 20f;
        audioMixer.SetFloat(paramName, db);
    }

    public void SaveSettings()
    {
        PlayerPrefs.Save(); // 디스크에 확실히 쓰기
    }
}