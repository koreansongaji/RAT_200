using UnityEngine;
using DinoFracture;

public sealed class WoodBreakable : MonoBehaviour
{
    private PreFracturedGeometry _preFracture;
    private bool _requested;

    private void Awake()
    {
        // Runtime 대신 PreFracturedGeometry를 사용합니다.
        _preFracture = GetComponent<PreFracturedGeometry>();
    }

    public void Break()
    {
        if (_requested) return;
        
        // 미리 구워진 조각이 있는지 확인
        if (_preFracture != null && _preFracture.GeneratedPieces != null)
        {
            _requested = true;

            // 1. 발사 중심점 설정 (파편들이 생성될 때 참고함)
            FracturePieceHandler.SetBlastPoint(transform.position);

            // 2. 에셋 내장 기능 호출 (원본 끄기 + 파편 켜기 즉시 실행)
            _preFracture.Fracture();
            
            // 3. 활성화된 파편들에게 발사 명령
            var handlers = _preFracture.GeneratedPieces.GetComponentsInChildren<FracturePieceHandler>();
            foreach (var h in handlers)
            {
                h.Launch();
            }
        }
        else
        {
            Debug.LogWarning("파편이 미리 생성되지 않았습니다! 인스펙터에서 Create Fractures를 눌러주세요.");
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.B))
            Break();
    }
}
