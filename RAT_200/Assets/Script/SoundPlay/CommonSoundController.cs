using UnityEngine;
using System.Collections;

/// <summary>
/// 문, 버튼, 퍼즐 결과 등 게임 전반에서 공용으로 사용되는 효과음을 재생하는 컨트롤러입니다.
/// </summary>
public class CommonSoundController : Singleton<CommonSoundController>
{
    [Header("Common Sfx Clips")]
    [SerializeField] private AudioClip _buttonClip;
    [SerializeField] private AudioClip _doorOpenClip;
    [SerializeField] private AudioClip _doorCloseClip;
    [SerializeField] private AudioClip _doorSlideClip;
    [SerializeField] private AudioClip _sparkClip;
    [SerializeField] private AudioClip _puzzleSuccessClip;
    [SerializeField] private AudioClip _puzzleFailClip;
    [SerializeField] private AudioClip _fridgeOpenClip;
    [SerializeField] private AudioClip _fridgeUnlockClip;

    new private void Awake()
    {
        base.Awake();
        // 리소스 로드 (경로: Assets/Resources/Sounds/Effect/...)
        if (_buttonClip == null) _buttonClip = Resources.Load<AudioClip>("Sounds/Effect/Universal/button_a");
        if (_doorOpenClip == null) _doorOpenClip = Resources.Load<AudioClip>("Sounds/Effect/Universal/creak_a");
        if (_doorCloseClip == null) _doorCloseClip = Resources.Load<AudioClip>("Sounds/Effect/Universal/creak_b");
        if (_doorSlideClip == null) _doorSlideClip = Resources.Load<AudioClip>("Sounds/Effect/Universal/door_slide");
        if (_sparkClip == null) _sparkClip = Resources.Load<AudioClip>("Sounds/Effect/Electrical Puzzle - Fuse/spark");
        if (_puzzleSuccessClip == null) _puzzleSuccessClip = Resources.Load<AudioClip>("Sounds/Effect/Universal/puzzle_success");
        if (_puzzleFailClip == null) _puzzleFailClip = Resources.Load<AudioClip>("Sounds/Effect/Universal/puzzle_fail");
        if (_fridgeOpenClip == null) _fridgeOpenClip = Resources.Load<AudioClip>("Sounds/Effect/Fridge - Light Puzzle/fridge_open");
        if (_fridgeUnlockClip == null) _fridgeUnlockClip = Resources.Load<AudioClip>("Sounds/Effect/Fridge - Light Puzzle/fridge_unlock");
    }

    /// <summary>
    /// 공용 버튼 클릭 소리를 재생합니다. 피치 변조를 통해 매번 미세하게 다른 소리를 냅니다.
    /// </summary>
    public void PlayButton()
    {
        if (_buttonClip != null)
        {
            float randomPitch = Random.Range(0.9f, 1.1f);
            AudioManager.Instance.Play(_buttonClip, AudioManager.Sound.Effect, randomPitch);
        }
    }

    /// <summary>
    /// 일반적인 문이 삐걱거리며 열리는 소리를 재생합니다.
    /// </summary>
    public void PlayDoorOpen() => PlayCommonSound(_doorOpenClip);

    /// <summary>
    /// 일반적인 문이 닫히는 소리를 재생합니다.
    /// </summary>
    public void PlayDoorClose() => PlayCommonSound(_doorCloseClip);

    /// <summary>
    /// 슬라이드 방식의 문이 열리거나 닫히는 소리를 재생합니다.
    /// </summary>
    public void PlayDoorSlide() => PlayCommonSound(_doorSlideClip);

    /// <summary>
    /// 전기 스파크나 퓨즈가 튀는 소리를 재생합니다.
    /// </summary>
    public void PlaySpark() => PlayCommonSound(_sparkClip);

    /// <summary>
    /// 퍼즐을 성공적으로 완료했을 때의 효과음을 재생합니다.
    /// </summary>
    public void PlayPuzzleSuccess() => PlayCommonSound(_puzzleSuccessClip);

    /// <summary>
    /// 냉장고 문이 열리는 소리를 재생합니다.
    /// </summary>
    public void PlayFridgeOpen() => PlayCommonSound(_fridgeOpenClip);
    
    /// <summary>
    /// 냉장고 잠금이 해제되는 소리를 재생합니다.
    /// </summary>
    public void PlayFridgeUnlock() => PlayCommonSound(_fridgeUnlockClip);
    
    /// <summary>
    /// 퍼즐에 실패했을 때의 효과음을 재생합니다. (실패 후 약 5초간 재생되도록 처리)
    /// </summary>
    public void PlayPuzzleFail()
    {
        if (_puzzleFailClip != null)
        {
            // 실패 소리는 강조를 위해 5초간 들리도록 별도 처리하거나 긴 클립을 재생합니다.
            // PlayOneShot은 클립 길이에 상관없이 끝까지 재생됩니다.
            AudioManager.Instance.Play(_puzzleFailClip, AudioManager.Sound.Effect);
        }
    }

    /// <summary>
    /// 공용 효과음을 기본 설정으로 재생합니다.
    /// </summary>
    private void PlayCommonSound(AudioClip clip)
    {
        if (clip != null)
        {
            AudioManager.Instance.Play(clip, AudioManager.Sound.Effect);
        }
    }
}
