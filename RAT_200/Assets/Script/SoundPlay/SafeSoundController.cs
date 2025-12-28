using UnityEngine;

/// <summary>
/// 금고 퍼즐의 다이얼 조작 및 개방과 관련된 효과음을 재생하는 컨트롤러입니다.
/// </summary>
public class SafeSoundController : MonoBehaviour
{
    [Header("Safe Sfx Clips")]
    [SerializeField] private AudioClip _safeDialClip;
    [SerializeField] private AudioClip _safeOpenClip;

    private void Awake()
    {
        // 리소스가 할당되지 않았을 경우 기본 경로에서 로드 시도
        if (_safeDialClip == null) _safeDialClip = Resources.Load<AudioClip>("Sounds/Effect/Safe/safe_dial");
        if (_safeOpenClip == null) _safeOpenClip = Resources.Load<AudioClip>("Sounds/Effect/Safe/safe_open");
    }

    /// <summary>
    /// 금고 다이얼을 돌릴 때 발생하는 클릭 소음을 재생합니다.
    /// </summary>
    public void PlaySafeDial()
    {
        if (_safeDialClip != null)
        {
            // 다이얼 소리가 겹칠 때 어색하지 않도록 미세한 랜덤 피치를 적용합니다.
            float randomPitch = Random.Range(0.9f, 1.1f);
            AudioManager.Instance.Play(_safeDialClip, AudioManager.Sound.Effect, randomPitch);
        }
    }

    /// <summary>
    /// 금고 문이 성공적으로 열릴 때의 효과음을 재생합니다.
    /// </summary>
    public void PlaySafeOpen()
    {
        if (_safeOpenClip != null)
        {
            AudioManager.Instance.Play(_safeOpenClip, AudioManager.Sound.Effect);
        }
    }
}
