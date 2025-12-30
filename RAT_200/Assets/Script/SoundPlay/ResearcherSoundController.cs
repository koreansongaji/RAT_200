using UnityEngine;

public class ResearcherSoundController : MonoBehaviour
{
    [SerializeField] private AudioClip _doorOpenSound;
    [SerializeField] private AudioClip _doorCloseSound;
    [SerializeField] private AudioClip _lightToggleSound;
    [SerializeField] private AudioClip _ratDeathSound;
    [SerializeField] private AudioClip _approachSound;
    [SerializeField] private AudioClip _researcherRunSound;
    
    private void Awake()
    {
        if(_doorOpenSound != null) _doorOpenSound = Resources.Load<AudioClip>("Sounds/Effect/Scientist/scientist_door_open");
        if(_doorCloseSound != null) _doorCloseSound = Resources.Load<AudioClip>("Sounds/Effect/Scientist/scientist_door_close");
        if(_lightToggleSound != null) _lightToggleSound = Resources.Load<AudioClip>("Sounds/Effect/Universal/button_a");
        if(_ratDeathSound != null) _ratDeathSound = Resources.Load<AudioClip>("Sounds/Effect/Rat/rat_death");
        if(_approachSound != null) _approachSound = Resources.Load<AudioClip>("Sounds/Effect/Scientist/scientist_approach");
        if(_researcherRunSound != null) _researcherRunSound = Resources.Load<AudioClip>("Sounds/Effect/Scientist/scientist_run_a");
    }
    
    public void PlayDoorOpenSound()
    {
        if(_doorOpenSound != null)
        {
            AudioManager.Instance.Play(_doorOpenSound, AudioManager.Sound.Effect);
        }
    }
    public void PlayDoorCloseSound()
    {
        if(_doorCloseSound != null)
        {
            AudioManager.Instance.Play(_doorCloseSound, AudioManager.Sound.Effect);
        }
    }
    public void PlayLightToggleSound()
    {
        if (_lightToggleSound != null)
        {
            AudioManager.Instance.Play(_lightToggleSound, AudioManager.Sound.Effect, 0.7f);
        }
    }
    
    public void PlayRatDeathSound()
    {
        if(_ratDeathSound != null)
        {
            AudioManager.Instance.Play(_ratDeathSound, AudioManager.Sound.Effect);
        }
    }
    
    public void PlayApproachSound()
    {
        if(_approachSound != null)
        {
            AudioManager.Instance.Play(_approachSound, AudioManager.Sound.Effect);
        }
    }
    
    public void PlayResearcherRunSound()
    {
        if(_researcherRunSound != null)
        {
            AudioManager.Instance.Play(_researcherRunSound, AudioManager.Sound.Effect, Random.Range(0.9f, 1.1f));
        }
    }
}
