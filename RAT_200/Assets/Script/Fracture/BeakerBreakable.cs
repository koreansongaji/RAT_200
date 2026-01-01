using UnityEngine;
using DinoFracture;

[RequireComponent(typeof(PreFracturedGeometry))]
[RequireComponent(typeof(Collider))]
public class BeakerBreakable : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("체크 해제하면 플레이어가 몸으로 부딪혀도 안 깨집니다. (이벤트 연출용)")]
    public bool breakOnTouch = true; // ★ 새로 추가된 옵션

    [Tooltip("깨트릴 대상의 태그 (Player)")]
    public string targetTag = "Player";

    [Header("Sound")]
    public AudioSource sfxSource;
    public AudioClip breakSound;

    private PreFracturedGeometry _preFracture;
    private bool _isBroken = false;

    private void Awake()
    {
        _preFracture = GetComponent<PreFracturedGeometry>();

        var col = GetComponent<Collider>();
        if (col && !col.isTrigger) col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        // 이미 깨졌거나, '접촉 파괴'가 꺼져있으면 무시
        if (_isBroken || !breakOnTouch) return;

        if (other.CompareTag(targetTag))
        {
            Break();
        }
    }

    // 이 함수는 breakOnTouch 옵션과 상관없이 외부(ChemMixingStation)에서 호출하면 무조건 깨집니다.
    public void Break()
    {
        if (_isBroken) return;

        if (_preFracture != null && _preFracture.GeneratedPieces != null)
        {
            _isBroken = true;

            //if (sfxSource && breakSound)
            //{
            //    sfxSource.PlayOneShot(breakSound);
            //}

            AudioManager.Instance.Play(breakSound);

            FracturePieceHandler.SetBlastPoint(transform.position);
            _preFracture.Fracture();

            var handlers = _preFracture.GeneratedPieces.GetComponentsInChildren<FracturePieceHandler>();
            foreach (var h in handlers)
            {
                h.Launch();
            }
        }
        else
        {
            Debug.LogWarning($"[Beaker] 파편 데이터가 없습니다.");
        }
    }
}