using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Collider))]
public class ZoneLightTrigger : MonoBehaviour
{
    [Header("Target Light")]
    public Light targetLight;

    [Header("Flashlight Settings")]
    [Tooltip("켜졌을 때의 최종 밝기")]
    public float targetIntensity = 1.5f;

    [Tooltip("몇 번 깜빡거리고 켜질지 (최소~최대 랜덤)")]
    public Vector2Int flickerCountRange = new Vector2Int(2, 4);

    [Tooltip("깜빡이는 속도 (낮을수록 빠름)")]
    public float flickerSpeed = 0.05f;

    [Header("Sound (Optional)")]
    public AudioSource sfxSource;
    public AudioClip clickSound; // 딸깍 소리

    [Header("Tag")]
    public string playerTag = "Player";

    private Coroutine _flickerCoroutine;

    void Awake()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    void Start()
    {
        if (targetLight)
        {
            targetLight.enabled = false;
            targetLight.intensity = targetIntensity; // 미리 밝기는 설정해둠
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            FlashlightOn();
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            InstantOff();
        }
    }

    void FlashlightOn()
    {
        if (!targetLight) return;

        // 이미 켜지는 중이라면 리셋
        if (_flickerCoroutine != null) StopCoroutine(_flickerCoroutine);

        _flickerCoroutine = StartCoroutine(Routine_FlickerOn());
    }

    void InstantOff()
    {
        if (!targetLight) return;

        // 켜지는 중이었다면 중단
        if (_flickerCoroutine != null) StopCoroutine(_flickerCoroutine);

        // 즉시 끄기 (탁!)
        targetLight.enabled = false;
    }

    IEnumerator Routine_FlickerOn()
    {
        // 1. 딸깍 소리 재생
        if (sfxSource && clickSound)
        {
            sfxSource.PlayOneShot(clickSound);
        }

        // 2. 깜빡임 연출
        int count = Random.Range(flickerCountRange.x, flickerCountRange.y + 1);

        for (int i = 0; i < count; i++)
        {
            // 켜졌다가 (랜덤 밝기로 불규칙성 추가)
            targetLight.enabled = true;
            targetLight.intensity = targetIntensity * Random.Range(0.2f, 1.2f);
            yield return new WaitForSeconds(Random.Range(0.02f, flickerSpeed));

            // 꺼졌다가
            targetLight.enabled = false;
            yield return new WaitForSeconds(Random.Range(0.02f, flickerSpeed));
        }

        // 3. 최종적으로 켜짐 유지
        targetLight.enabled = true;
        targetLight.intensity = targetIntensity;
    }
}