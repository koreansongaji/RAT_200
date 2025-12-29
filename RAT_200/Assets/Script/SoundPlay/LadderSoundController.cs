using UnityEngine;

/// <summary>
/// 사다리 배치, 수리 및 오르기 동작과 관련된 효과음을 재생하는 컨트롤러입니다.
/// </summary>
public class LadderSoundController : MonoBehaviour
{
    [Header("Ladder Sfx Clips")]
    [SerializeField] private AudioClip _placeLadderClip;
    [SerializeField] private AudioClip _fixLadderClip;
    [SerializeField] private AudioClip _climbLadderClip;

    private void Awake()
    {
        // 리소스가 할당되지 않았을 경우 기본 경로에서 로드 시도
        if (_placeLadderClip == null) _placeLadderClip = Resources.Load<AudioClip>("Sounds/place_ladder");
        if (_fixLadderClip == null) _fixLadderClip = Resources.Load<AudioClip>("Sounds/fix_ladder");
        if (_climbLadderClip == null) _climbLadderClip = Resources.Load<AudioClip>("Sounds/climb_ladder");
    }

    /// <summary>
    /// 사다리를 특정 위치에 배치할 때의 효과음을 재생합니다.
    /// </summary>
    public void PlayPlaceLadder() => PlayLadderSound(_placeLadderClip);

    /// <summary>
    /// 망가진 사다리를 수리할 때의 효과음을 재생합니다.
    /// </summary>
    public void PlayFixLadder() => PlayLadderSound(_fixLadderClip);

    /// <summary>
    /// 사다리를 한 칸씩 오를 때의 효과음을 재생합니다.
    /// </summary>
    public void PlayClimbLadder() => PlayLadderSound(_climbLadderClip);

    /// <summary>
    /// 사다리 관련 효과음을 랜덤 피치와 함께 재생합니다.
    /// </summary>
    private void PlayLadderSound(AudioClip clip)
    {
        if (clip != null)
        {
            // 매번 조금씩 다른 소리가 나도록 0.92f ~ 1.08f 사이의 랜덤 피치 적용
            float randomPitch = Random.Range(0.92f, 1.08f);
            AudioManager.Instance.Play(clip, AudioManager.Sound.Effect, randomPitch);
        }
    }
}
