using UnityEngine;


public class RopeSoundController:MonoBehaviour
{
    [SerializeField] private AudioClip _ropeSound;
    private void Awake()
    {
        if (_ropeSound == null) _ropeSound = Resources.Load<AudioClip>("Sounds/Effect/Rat/climb_rope");
    }

    public void PlayRopeSound()
    {
        AudioManager.Instance.Play(_ropeSound, AudioManager.Sound.Effect, Random.Range(0.95f, 1.05f));
    }
}

