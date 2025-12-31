using UnityEngine;
using DinoFracture;

[RequireComponent(typeof(PreFracturedGeometry))]
[RequireComponent(typeof(Collider))]
public class BeakerBreakable : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("깨트릴 대상의 태그 (Player)")]
    public string targetTag = "Player";

    [Header("Sound")]
    public AudioSource sfxSource;
    public AudioClip breakSound; // 쨍그랑 소리

    private PreFracturedGeometry _preFracture;
    private bool _isBroken = false;

    private void Awake()
    {
        _preFracture = GetComponent<PreFracturedGeometry>();

        // 안전장치: 혹시 isTrigger가 꺼져있으면 켜줌
        var col = GetComponent<Collider>();
        if (col && !col.isTrigger) col.isTrigger = true;
    }

    // ★ Trigger 감지로 변경
    private void OnTriggerEnter(Collider other)
    {
        if (_isBroken) return;

        // 태그 확인 (플레이어인가?)
        if (other.CompareTag(targetTag))
        {
            Break();
        }
    }

    public void Break()
    {
        if (_isBroken) return;

        if (_preFracture != null && _preFracture.GeneratedPieces != null)
        {
            _isBroken = true;

            if (sfxSource && breakSound)
            {
                sfxSource.PlayOneShot(breakSound);
            }

            // 1. 폭발 위치 설정
            FracturePieceHandler.SetBlastPoint(transform.position);

            // 2. 파편 교체
            _preFracture.Fracture();

            // 3. 파편 튀기기
            var handlers = _preFracture.GeneratedPieces.GetComponentsInChildren<FracturePieceHandler>();
            foreach (var h in handlers)
            {
                h.Launch();
            }
        }
        else
        {
            Debug.LogWarning($"[Beaker] 파편 데이터가 없습니다. Inspector에서 'Create Fractures'를 확인하세요.");
        }
    }
}