using UnityEngine;

/// <summary>
/// MicroZoom(어느 퍼즐이든 상관없이) 상태일 때 플레이어 모델을 숨기는 스크립트.
/// - CloseupCamManager.InMicro 전역 상태만 보고 판단한다.
/// - modelRoot 아래의 렌더러들만 껐다 켜기 때문에 콜라이더/에이전트는 그대로 남는다.
/// </summary>
public class PlayerHideOnMicro : MonoBehaviour
{
    [Header("숨길 모델 루트 (없으면 이 오브젝트 기준)")]
    [SerializeField] Transform modelRoot;

    [Header("Micro 아닐 때도 강제로 항상 보이게 할지")]
    [SerializeField] bool forceShowWhenNotMicro = true;

    Renderer[] _renderers;
    bool _lastVisible = true;
    bool _initialized = false;

    void Awake()
    {
        if (!modelRoot) modelRoot = transform;
        CacheRenderers();
    }

    void OnValidate()
    {
        if (!modelRoot) modelRoot = transform;
        CacheRenderers();
    }

    void CacheRenderers()
    {
        if (!modelRoot) return;
        _renderers = modelRoot.GetComponentsInChildren<Renderer>(includeInactive: true);
        _initialized = true;
    }

    void LateUpdate()
    {
        if (!_initialized) CacheRenderers();

        bool inMicro = CloseupCamManager.InMicro;   // ★ 전역 Micro 상태만 사용

        // Micro 중에는 무조건 숨김
        bool shouldVisible = !inMicro;

        // 옵션: Micro가 아닐 땐 강제로 보여주기
        if (!inMicro && !forceShowWhenNotMicro)
        {
            // forceShowWhenNotMicro == false 인 경우,
            // 외부에서 SetActive(false) 해놓은 건 건드리지 않게 하려면
            // 여기서 아무 것도 안 해도 되지만,
            // 일단은 shouldVisible = _lastVisible 유지할 수도 있음.
        }

        if (shouldVisible == _lastVisible) return; // 상태 변화 없음 → 패스
        _lastVisible = shouldVisible;

        SetVisible(shouldVisible);
    }

    void SetVisible(bool visible)
    {
        if (_renderers == null) return;
        for (int i = 0; i < _renderers.Length; i++)
        {
            if (_renderers[i] == null) continue;
            _renderers[i].enabled = visible;
        }
    }
}
