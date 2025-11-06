using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class PlayerHiderOnMicro : MonoBehaviour
{
    public enum Mode { HostOptIn, Always, Never }

    [Header("Refs")]
    [Tooltip("이 Micro 상태를 구독합니다.")]
    public MicroZoomSession micro;
    [Tooltip("플레이어 모델의 루트 Transform (여기 하위의 Renderer를 토글)")]
    public Transform playerRoot;

    [Header("Behavior")]
    public Mode hideMode = Mode.HostOptIn;
    [Tooltip("숨긴 동안 그림자만 남길지 선택(렌더는 안 보이고 그림자는 보임)")]
    public bool shadowsOnly = false;

    // 내부
    readonly List<Renderer> _renderers = new();
    readonly List<bool> _prevEnabled = new();
    readonly List<ShadowCastingMode> _prevShadow = new();

    void Awake()
    {
        //if (!micro) micro = FindObjectOfType<MicroZoomSession>();
        if (!playerRoot)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p) playerRoot = p.transform;
        }

        CacheRenderers();
    }

    void OnEnable()
    {
        if (micro)
        {
            micro.OnEnterMicro.AddListener(OnEnterMicro);
            micro.OnExitMicro.AddListener(OnExitMicro);
        }
    }

    void OnDisable()
    {
        if (micro)
        {
            micro.OnEnterMicro.RemoveListener(OnEnterMicro);
            micro.OnExitMicro.RemoveListener(OnExitMicro);
        }
        // 안전 복원
        ShowPlayer();
    }

    void CacheRenderers()
    {
        _renderers.Clear();
        _prevEnabled.Clear();
        _prevShadow.Clear();

        if (!playerRoot) return;
        playerRoot.GetComponentsInChildren(true, _renderers);

        for (int i = 0; i < _renderers.Count; i++)
        {
            _prevEnabled.Add(_renderers[i].enabled);
            _prevShadow.Add(_renderers[i].shadowCastingMode);
        }
    }

    void OnEnterMicro()
    {
        if (hideMode == Mode.Never) return;
        if (hideMode == Mode.Always) { HidePlayer(); return; }

        // HostOptIn: 호스트가 원할 때만
        if (ShouldHideByHost()) HidePlayer();
    }

    void OnExitMicro()
    {
        ShowPlayer();
    }

    bool ShouldHideByHost()
    {
        if (!micro) return false;

        // MicroZoomSession가 찾은 Host를 우리도 같은 규칙으로 추적
        Component host =
            micro ? (micro.GetComponent<IMicroSessionHost>() as Component
                  ?? micro.GetComponentInChildren<IMicroSessionHost>() as Component
                  ?? micro.GetComponentInParent<IMicroSessionHost>() as Component)
                  : null;

        var pref = host as IMicroHidePlayerPreference;
        return pref != null && pref.HidePlayerDuringMicro;
    }

    void HidePlayer()
    {
        if (_renderers.Count == 0) CacheRenderers();

        for (int i = 0; i < _renderers.Count; i++)
        {
            var r = _renderers[i];
            if (!r) continue;

            // 상태 저장(1회만)
            if (i >= _prevEnabled.Count) _prevEnabled.Add(r.enabled);
            if (i >= _prevShadow.Count) _prevShadow.Add(r.shadowCastingMode);

            if (shadowsOnly)
            {
                r.enabled = true;
                r.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
            }
            else
            {
                r.enabled = false;
            }
        }
    }

    void ShowPlayer()
    {
        for (int i = 0; i < _renderers.Count; i++)
        {
            var r = _renderers[i];
            if (!r) continue;

            // 저장했던 상태로 복원
            if (i < _prevEnabled.Count) r.enabled = _prevEnabled[i];
            if (i < _prevShadow.Count) r.shadowCastingMode = _prevShadow[i];
        }
    }
}
