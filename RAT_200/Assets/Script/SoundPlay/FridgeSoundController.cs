using UnityEngine;

/// <summary>
/// 냉장고 퍼즐의 블록 이동, 키패드 입력, 해금 및 개방 효과음을 재생하는 컨트롤러입니다.
/// </summary>
public class FridgeSoundController : MonoBehaviour
{
    [Header("Fridge Sfx Clips")]
    [SerializeField] private AudioClip _blockSlideClip;
    [SerializeField] private AudioClip _puzzleSuccessClip;
    [SerializeField] private AudioClip _keypadClip;
    [SerializeField] private AudioClip _fridgeOpenClip;
    [SerializeField] private AudioClip _fridgeUnlockClip;

    private void Awake()
    {
        if (_blockSlideClip == null) _blockSlideClip = Resources.Load<AudioClip>("Sounds/Effect/Fridge - Light Puzzle/fridge_block_slide");
        if (_puzzleSuccessClip == null) _puzzleSuccessClip = Resources.Load<AudioClip>("Sounds/Effect/Electrical Puzzle - Fuse/spark");
        if (_keypadClip == null) _keypadClip = Resources.Load<AudioClip>("Sounds/Effect/Universal/button_a");
        if (_fridgeOpenClip == null) _fridgeOpenClip = Resources.Load<AudioClip>("Sounds/Effect/Fridge - Light Puzzle/fridge_open");
        if (_fridgeUnlockClip == null) _fridgeUnlockClip = Resources.Load<AudioClip>("Sounds/Effect/Fridge - Light Puzzle/fridge_unlock");
    }

    /// <summary>
    /// 퍼즐 블록이 슬라이드될 때의 효과음을 재생합니다.
    /// </summary>
    public void PlayBlockSlide()
    {
        if (_blockSlideClip != null)
        {
            // 연속적인 이동 시 자연스럽도록 랜덤 피치 적용
            float randomPitch = Random.Range(0.95f, 1.05f);
            AudioManager.Instance.Play(_blockSlideClip, AudioManager.Sound.Effect, randomPitch);
        }
    }

    /// <summary>
    /// 키패드 버튼을 누를 때의 효과음을 재생합니다.
    /// </summary>
    public void PlayKeypad()
    {
        if (_keypadClip != null)
        {
            float randomPitch = Random.Range(0.9f, 1.1f);
            AudioManager.Instance.Play(_keypadClip, AudioManager.Sound.Effect, randomPitch);
        }
    }

    /// <summary>
    /// 퍼즐을 성공적으로 풀었을 때의 효과음을 재생합니다.
    /// </summary>
    public void PlayPuzzleSuccess()
    {
        if (_puzzleSuccessClip != null)
        {
            AudioManager.Instance.Play(_puzzleSuccessClip, AudioManager.Sound.Effect);
        }
    }

    /// <summary>
    /// 냉장고 문이 해금(잠금 해제)될 때의 효과음을 재생합니다.
    /// </summary>
    public void PlayFridgeUnlock()
    {
        if (_fridgeUnlockClip != null)
        {
            AudioManager.Instance.Play(_fridgeUnlockClip, AudioManager.Sound.Effect);
        }
    }

    /// <summary>
    /// 냉장고 문이 실제로 열릴 때의 효과음을 재생합니다.
    /// </summary>
    public void PlayFridgeOpen()
    {
        if (_fridgeOpenClip != null)
        {
            AudioManager.Instance.Play(_fridgeOpenClip, AudioManager.Sound.Effect);
        }
    }
}
