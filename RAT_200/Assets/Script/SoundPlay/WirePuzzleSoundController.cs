using UnityEngine;

/// <summary>
/// 전기 퍼즐(와이어 퍼즐) 조작, 와이어 이동, 퍼즐 성공 및 실패 효과음을 재생하는 컨트롤러입니다.
/// </summary>
public class WirePuzzleSoundController : MonoBehaviour
{
    [Header("Wire Puzzle Clips")]
    [SerializeField] private AudioClip _buttonClip;
    [SerializeField] private AudioClip _wireMoveClip;
    [SerializeField] private AudioClip _sparkSuccessClip;
    [SerializeField] private AudioClip _puzzleFailClip;

    private void Awake()
    {
        // 리소스 로드
        if (_buttonClip == null) _buttonClip = Resources.Load<AudioClip>("Sounds/Effect/Universal/button_a");
        if (_wireMoveClip == null) _wireMoveClip = Resources.Load<AudioClip>("Sounds/Effect/Electrical Puzzle - Fuse/wire_puzzle_move");
        if (_sparkSuccessClip == null) _sparkSuccessClip = Resources.Load<AudioClip>("Sounds/Effect/Electrical Puzzle - Fuse/spark");
        if (_puzzleFailClip == null) _puzzleFailClip = Resources.Load<AudioClip>("Sounds/Effect/Universal/puzzle_fail");
    }

    /// <summary>
    /// 버튼을 누를 때 효과음을 재생합니다. 
    /// 함수 내부에서 피치 값을 무작위로 설정하여 매번 조금씩 다른 소리가 납니다.
    /// </summary>
    public void PlayButton()
    {
        if (_buttonClip != null)
        {
            // 0.9f에서 1.1f 사이의 무작위 피치값 적용
            float randomPitch = Random.Range(0.9f, 1.1f);
            AudioManager.Instance.Play(_buttonClip, AudioManager.Sound.Effect, randomPitch);
        }
    }

    /// <summary>
    /// 와이어가 이동하거나 연결될 때의 효과음을 재생합니다.
    /// </summary>
    public void PlayWireMove()
    {
        if (_wireMoveClip != null)
        {
            AudioManager.Instance.Play(_wireMoveClip, AudioManager.Sound.Effect);
        }
    }

    /// <summary>
    /// 퍼즐을 성공적으로 풀었을 때 스파크(Spark) 효과음을 재생합니다.
    /// </summary>
    public void PlayPuzzleSuccess()
    {
        if (_sparkSuccessClip != null)
        {
            AudioManager.Instance.Play(_sparkSuccessClip, AudioManager.Sound.Effect);
        }
    }

    /// <summary>
    /// 퍼즐 시도에 실패했을 때의 효과음을 재생합니다.
    /// </summary>
    public void PlayPuzzleFail()
    {
        if (_puzzleFailClip != null)
        {
            AudioManager.Instance.Play(_puzzleFailClip, AudioManager.Sound.Effect);
        }
    }
}
