using UnityEngine;

/// <summary>
/// 실험대 퍼즐의 버튼 조작 및 화학 반응(성공, 실패, 혼합) 효과음을 재생하는 컨트롤러입니다.
/// </summary>
public class LabBenchSoundController : MonoBehaviour
{
    [Header("Button Clips")]
    [SerializeField] private AudioClip[] _buttonClips = new AudioClip[3];

    [Header("Reaction Clips")]
    [SerializeField] private AudioClip _reactionMixClip;
    [SerializeField] private AudioClip _reactionFailClip;
    [SerializeField] private AudioClip _reactionSuccessClip;

    private void Awake()
    {
        // 버튼 사운드 로드 (button_a, b, c)
        if (_buttonClips[0] == null) _buttonClips[0] = Resources.Load<AudioClip>("Sounds/Effect/Universal/button_a");
        if (_buttonClips[1] == null) _buttonClips[1] = Resources.Load<AudioClip>("Sounds/Effect/Universal/button_b");
        if (_buttonClips[2] == null) _buttonClips[2] = Resources.Load<AudioClip>("Sounds/Effect/Universal/button_c");

        // 반응 사운드 로드
        if (_reactionMixClip == null) _reactionMixClip = Resources.Load<AudioClip>("Sounds/Effect/Experiment/reaction_mix");
        if (_reactionFailClip == null) _reactionFailClip = Resources.Load<AudioClip>("Sounds/Effect/Experiment/reaction_fail");
        if (_reactionSuccessClip == null) _reactionSuccessClip = Resources.Load<AudioClip>("Sounds/Effect/Experiment/reaction_success");
    }

    /// <summary>
    /// 실험대 버튼을 누를 때 3가지 소리 중 하나를 무작위로 재생합니다.
    /// </summary>
    public void PlayButton()
    {
        int randomIndex = Random.Range(0, _buttonClips.Length);
        AudioClip clip = _buttonClips[randomIndex];

        if (clip != null)
        {
            // 더욱 다양한 느낌을 위해 피치도 미세하게 조절합니다.
            float randomPitch = Random.Range(0.95f, 1.05f);
            AudioManager.Instance.Play(clip, AudioManager.Sound.Effect, randomPitch);
        }
    }

    /// <summary>
    /// 액체나 물질이 혼합될 때 발생하는 효과음을 재생합니다.
    /// </summary>
    public void PlayReactionMix()
    {
        if (_reactionMixClip != null)
        {
            AudioManager.Instance.Play(_reactionMixClip, AudioManager.Sound.Effect);
        }
    }

    /// <summary>
    /// 혼합 결과가 잘못되었을 때(실패)의 효과음을 재생합니다.
    /// </summary>
    public void PlayReactionFail()
    {
        if (_reactionFailClip != null)
        {
            AudioManager.Instance.Play(_reactionFailClip, AudioManager.Sound.Effect);
        }
    }

    /// <summary>
    /// 올바른 조합을 찾아 혼합에 성공했을 때의 효과음을 재생합니다.
    /// </summary>
    public void PlayReactionSuccess()
    {
        if (_reactionSuccessClip != null)
        {
            AudioManager.Instance.Play(_reactionSuccessClip, AudioManager.Sound.Effect);
        }
    }
}
