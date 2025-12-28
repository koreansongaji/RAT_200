using UnityEngine;

/// <summary>
/// 소음 함정(구겨진 보고서, 플라스크, 약한 바닥 등)과 관련된 효과음을 재생하는 컨트롤러입니다.
/// </summary>
public class NoiseTrapSoundController : MonoBehaviour
{
    [Header("Noise Trap Clips")]
    [SerializeField] private AudioClip _crumbledPaperClip;
    [SerializeField] private AudioClip _flaskClip;
    [SerializeField] private AudioClip _weakFloorClip;

    private void Awake()
    {
        // 리소스가 할당되지 않았을 경우 기본 경로에서 로드 시도
        if (_crumbledPaperClip == null) _crumbledPaperClip = Resources.Load<AudioClip>("Sounds/Effect/Trap/crumbled_paper");
        if (_flaskClip == null) _flaskClip = Resources.Load<AudioClip>("Sounds/Effect/Trap/flask");
        if (_weakFloorClip == null) _weakFloorClip = Resources.Load<AudioClip>("Sounds/Effect/Trap/weak_floor");
    }

    /// <summary>
    /// 구겨진 보고서를 밟았을 때 바스락거리는 소음을 재생합니다.
    /// </summary>
    public void PlayCrumbledPaper() => PlayTrapSound(_crumbledPaperClip);

    /// <summary>
    /// 플라스크가 깨지거나 부딪히는 날카로운 소음을 재생합니다.
    /// </summary>
    public void PlayFlask() => PlayTrapSound(_flaskClip);

    /// <summary>
    /// 약한 바닥판이 삐걱거리거나 무너지는 소음을 재생합니다.
    /// </summary>
    public void PlayWeakFloor() => PlayTrapSound(_weakFloorClip);

    /// <summary>
    /// 함정 효과음을 재생합니다. 함정 소리는 강조를 위해 피치를 미세하게 무작위로 설정합니다.
    /// </summary>
    private void PlayTrapSound(AudioClip clip)
    {
        if (clip != null)
        {
            // 같은 소리라도 매번 다르게 들리도록 랜덤 피치 적용 (0.95 ~ 1.05)
            float randomPitch = Random.Range(0.95f, 1.05f);
            AudioManager.Instance.Play(clip, AudioManager.Sound.Effect, randomPitch);
        }
    }
}
