using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class ClickMoveOrInteract_Events : MonoBehaviour
{
    [Header("Refs")]
    public Camera cam;
    public NavMeshAgent agent;
    public PlayerInteractor player;
    public PlayerReach reach;

    // ★ [추가] 플레이어의 상태(Busy)를 확인하기 위한 스크립트 참조
    public PlayerScriptedMover scriptedMover;

    [Header("Masks")]
    public LayerMask groundMask;
    public LayerMask interactableMask;

    [Header("NavMesh")]
    public float sampleMaxDistance = 2f;
    public float maxRayDistance = 200f;

    [Header("디버그/기즈모")]
    public bool drawGizmos = true;
    public Color gizmoReachColor = new(0f, 0.8f, 1f, 0.9f);
    public Color gizmoClosestPointColor = new(0.2f, 1f, 0.2f, 0.9f);
    public Color gizmoOutOfReachColor = new(1f, 0.2f, 0.2f, 0.9f);

    [Header("더블 클릭")]
    [SerializeField] float doubleClickTime = 0.25f;
    [SerializeField] float doubleClickMaxScreenDist = 12f;
    [SerializeField] float speedBoostMultiplier = 1.8f;
    [SerializeField] float accelBoostMultiplier = 1.5f;

    // ===== Drag state =====
    [SerializeField] float dragThresholdPixels = 6f;
    [SerializeField] LayerMask draggableMask = ~0;

    Draggable3D _dragCandidate;
    Draggable3D _activeDrag;
    Vector2 _pressPos;
    bool _pointerDown;

    float _lastClickTime;
    Vector2 _lastClickScreenPos;
    bool _boostActive;
    float _baseSpeed, _baseAccel;
    float _baseAnimatorSpeed = 1f;
    [Header("애니메이션")]
    [SerializeField] Animator animator;

    private NavMeshPath _path;

    private bool _queuedClick;
    private Vector2 _queuedScreenPos;

    private Vector3 _debugClosestPoint;
    private bool _hasDebugClosest;
    private bool _lastWithinReach;

    static readonly List<RaycastResult> _uiHits = new();

    float ReachRadius => reach ? Mathf.Max(0f, reach.radius) : 1.6f;
    bool HorizontalOnly => reach ? reach.horizontalOnly : false;

    Draggable3D RaycastForDraggable(Vector2 screenPos)
    {
        var ray = cam.ScreenPointToRay(screenPos);
        if (Physics.Raycast(ray, out var hit, maxRayDistance, draggableMask))
        {
            var dragComp = hit.collider.GetComponentInParent<Draggable3D>();
            if (dragComp != null && dragComp.isActiveAndEnabled)
            {
                return dragComp;
            }
        }
        return null;
    }

    bool InMicro() => CloseupCamManager.InMicro;
    bool ModKey() => Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

    void Awake()
    {
        _path = new NavMeshPath();

        // ★ [추가] 자동으로 찾아주기 (혹시 인스펙터 누락 대비)
        if (!scriptedMover) scriptedMover = GetComponent<PlayerScriptedMover>();
        if (!scriptedMover && player) scriptedMover = player.GetComponent<PlayerScriptedMover>();
    }

    void Start()
    {
        _baseSpeed = agent.speed;
        _baseAccel = agent.acceleration;
        if (animator) _baseAnimatorSpeed = animator.speed;
    }

    public void OnClick(InputAction.CallbackContext ctx) { }

    public void OnPointerDown(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed) return;

        // ★ [추가] 플레이어가 연출 중(밧줄 등)이면 입력 무시
        if (scriptedMover && scriptedMover.IsBusy()) return;

        var pos = Mouse.current != null ? Mouse.current.position.ReadValue() : (Vector2)Input.mousePosition;

        _pointerDown = true;
        _pressPos = pos;

        if (IsPointerOverUI(pos))
        {
            _dragCandidate = null;
            return;
        }

        _dragCandidate = RaycastForDraggable(pos);
    }

    public void OnPointerDrag(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed && !ctx.canceled) return;

        // ★ [추가] 플레이어가 연출 중이면 드래그도 무시
        if (scriptedMover && scriptedMover.IsBusy()) return;

        if (!_pointerDown) return;

        var pos = Mouse.current != null ? Mouse.current.position.ReadValue() : (Vector2)Input.mousePosition;

        if (_activeDrag != null)
        {
            _activeDrag.DragUpdate(cam.ScreenPointToRay(pos));
            return;
        }

        if (_dragCandidate != null)
        {
            if ((pos - _pressPos).magnitude >= dragThresholdPixels)
            {
                if (_dragCandidate.CanBeginDrag(InMicro, ModKey))
                {
                    HardStop();
                    ActivateBoostIfNeeded(false, pos);

                    _activeDrag = _dragCandidate;
                    _activeDrag.BeginDrag(cam.ScreenPointToRay(pos));
                }
                else
                {
                    _dragCandidate = null;
                }
            }
        }
    }

    public void OnPointerUp(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed) return;

        // ★ [추가] 플레이어가 연출 중이면 클릭 해제도 로직 처리 안 함 (단, _pointerDown 초기화는 필요할 수 있음)
        if (scriptedMover && scriptedMover.IsBusy())
        {
            _pointerDown = false;
            _activeDrag = null;
            _dragCandidate = null;
            _queuedClick = false; // 혹시 모를 큐 제거
            return;
        }

        var pos = Mouse.current != null ? Mouse.current.position.ReadValue() : (Vector2)Input.mousePosition;
        _pointerDown = false;

        if (_activeDrag != null)
        {
            _activeDrag.EndDrag();
            _activeDrag = null;
            _dragCandidate = null;
            return;
        }

        _queuedScreenPos = pos;
        _queuedClick = true;
    }

    void Update()
    {
        // 1. 기존 이동 로직 처리
        if (!agent.pathPending && agent.hasPath && Arrived())
            HardStop();

        if (agent.isActiveAndEnabled && agent.isOnNavMesh)
        {
            if (!agent.pathPending && agent.hasPath && Arrived())
                HardStop();
        }

        // 2. 드래그 처리
        if (_activeDrag != null)
        {
            var pos = Mouse.current != null ? Mouse.current.position.ReadValue() : (Vector2)Input.mousePosition;
            _activeDrag.DragUpdate(cam.ScreenPointToRay(pos));
            return;
        }

        // ★ [핵심 추가] 플레이어가 Scripted Movement(밧줄, 사다리 등) 중이면 클릭 처리 중단
        if (scriptedMover && scriptedMover.IsBusy())
        {
            _queuedClick = false; // 쌓여있던 클릭 요청 폐기
            return;
        }

        // 3. 클릭 처리 (이동 or 인터랙션)
        if (!_queuedClick) return;
        _queuedClick = false;

        if (IsPointerOverUI(_queuedScreenPos)) return;

        Ray ray = cam.ScreenPointToRay(_queuedScreenPos);
        LayerMask combinedMask = interactableMask | groundMask;

        if (Physics.Raycast(ray, out var hit, maxRayDistance, combinedMask))
        {
            // Interactable 처리
            if (((1 << hit.collider.gameObject.layer) & interactableMask) != 0)
            {
                var target = hit.collider.GetComponentInParent<IInteractable>();
                if (target != null)
                {
                    bool inMicro = InMicro();
                    bool bypassDistance = false;

                    if (target is MicroEntryInteractable microEntry)
                    {
                        bypassDistance = microEntry.ShouldBypassDistanceCheck(agent.transform.position);
                    }

                    float effectiveReach = (inMicro || bypassDistance) ? float.PositiveInfinity : ReachRadius;

                    Vector3 closest = GetClosestPointOnTarget(target, agent.transform.position, out _);
                    _hasDebugClosest = true;
                    _debugClosestPoint = closest;

                    bool within = IsWithinReach(agent.transform.position, closest, effectiveReach);
                    _lastWithinReach = within;

                    if (within && target.CanInteract(player))
                    {
                        HardStop();
                        ActivateBoostIfNeeded(false, _queuedScreenPos);
                        target.Interact(player);
                        return;
                    }

                    if (!inMicro)
                    {
                        if (TryApproachTarget(target, closest, effectiveReach)) return;
                        return;
                    }
                    return;
                }
            }
            else
            {
                // Ground(이동) 처리
                if (InMicro()) return;

                ActivateBoostIfNeeded(true, _queuedScreenPos);

                if (NavMesh.SamplePosition(hit.point, out var navHit, sampleMaxDistance, NavMesh.AllAreas))
                {
                    TrySetPath(navHit.position);
                }
                return;
            }
        }
    }

    // ... (이하 기존 함수들: GetClosestPointOnTarget 등 그대로 유지) ...

    // (이전 코드의 나머지 부분은 생략 없이 그대로 두시면 됩니다.)

    Vector3 GetClosestPointOnTarget(IInteractable target, Vector3 playerPos, out Collider hitCol)
    {
        hitCol = null;
        var t = target.AsTransform();
        Collider[] cols = t.GetComponentsInChildren<Collider>(true);

        float bestSqr = float.PositiveInfinity;
        Vector3 best = t.position;

        for (int i = 0; i < cols.Length; i++)
        {
            var c = cols[i];
            if (c == null || !c.enabled) continue;
            Vector3 p = c.ClosestPoint(playerPos);
            if (HorizontalOnly) p.y = playerPos.y;
            float sq = (p - playerPos).sqrMagnitude;
            if (sq < bestSqr) { bestSqr = sq; best = p; hitCol = c; }
        }

        if (cols.Length == 0)
        {
            best = t.position;
            if (HorizontalOnly) best.y = playerPos.y;
        }
        return best;
    }

    bool IsWithinReach(Vector3 playerPos, Vector3 closestOnTarget, float reachRadius)
    {
        if (HorizontalOnly) playerPos.y = closestOnTarget.y;
        return Vector3.Distance(playerPos, closestOnTarget) <= reachRadius;
    }

    bool TryApproachTarget(IInteractable target, Vector3 closestOnSurface, float reachRadius)
    {
        Vector3 playerPos = agent.transform.position;
        if (HorizontalOnly) playerPos.y = closestOnSurface.y;

        Vector3 dir = (playerPos - closestOnSurface);
        if (dir.sqrMagnitude < 1e-6f) dir = (playerPos - target.AsTransform().position);
        if (HorizontalOnly) dir.y = 0f;
        dir = dir.sqrMagnitude > 1e-6f ? dir.normalized : Vector3.forward;

        float buffer = Mathf.Max(agent.radius + 0.05f, reachRadius * 0.5f);
        Vector3 approach = closestOnSurface + dir * buffer;

        if (NavMesh.SamplePosition(approach, out var navHit, sampleMaxDistance, NavMesh.AllAreas))
            return TrySetPath(navHit.position);

        const float r = 0.8f; const int n = 12;
        for (int i = 0; i < n; i++)
        {
            float ang = (Mathf.PI * 2f) * (i / (float)n);
            Vector3 cand = approach + new Vector3(Mathf.Cos(ang), 0, Mathf.Sin(ang)) * r;
            if (NavMesh.SamplePosition(cand, out var alt, sampleMaxDistance, NavMesh.AllAreas))
                if (TrySetPath(alt.position)) return true;
        }
        return false;
    }

    bool Arrived()
    {
        float eps = Mathf.Max(0.02f, agent.stoppingDistance + 0.02f);
        return agent.remainingDistance <= eps;
    }

    bool IsDoubleClick(Vector2 currentScreenPos)
    {
        float dt = Time.time - _lastClickTime;
        float dist = Vector2.Distance(currentScreenPos, _lastClickScreenPos);
        _lastClickTime = Time.time;
        _lastClickScreenPos = currentScreenPos;
        return dt <= doubleClickTime && dist <= doubleClickMaxScreenDist;
    }

    void ActivateBoostIfNeeded(bool movementWillHappen, Vector2 screenPos)
    {
        if (!movementWillHappen) { _boostActive = false; return; }
        if (IsDoubleClick(screenPos))
        {
            _boostActive = true;
            agent.speed = _baseSpeed * speedBoostMultiplier;
            agent.acceleration = _baseAccel * accelBoostMultiplier;
            if (animator) animator.speed = _baseAnimatorSpeed * speedBoostMultiplier;
        }
        else
        {
            _boostActive = false;
            agent.speed = _baseSpeed;
            agent.acceleration = _baseAccel;
            if (animator) animator.speed = _baseAnimatorSpeed;
        }
    }

    bool TryMoveToGroundUnderRay(Ray ray)
    {
        if (Physics.Raycast(ray, out var hitG, maxRayDistance, groundMask))
            if (NavMesh.SamplePosition(hitG.point, out var navHit, sampleMaxDistance, NavMesh.AllAreas))
                return TrySetPath(navHit.position);
        return false;
    }

    bool TrySetPath(Vector3 dst)
    {
        if (!agent || !agent.isActiveAndEnabled || !agent.isOnNavMesh)
            return false;

        if (_path == null) _path = new NavMeshPath();
        if (agent.CalculatePath(dst, _path) && _path.status == NavMeshPathStatus.PathComplete)
        {
            agent.isStopped = false;
            agent.SetPath(_path);
            return true;
        }
        const float r = 0.8f; const int n = 12;
        for (int i = 0; i < n; i++)
        {
            float ang = (Mathf.PI * 2f) * (i / (float)n);
            Vector3 cand = dst + new Vector3(Mathf.Cos(ang), 0, Mathf.Sin(ang)) * r;
            if (NavMesh.SamplePosition(cand, out var alt, sampleMaxDistance, NavMesh.AllAreas))
                if (agent.CalculatePath(alt.position, _path) && _path.status == NavMeshPathStatus.PathComplete)
                {
                    agent.isStopped = false;
                    agent.SetPath(_path);
                    return true;
                }
        }
        return false;
    }

    void HardStop()
    {
        if (agent.hasPath) agent.ResetPath();
        agent.isStopped = true;
        agent.velocity = Vector3.zero;
        agent.angularSpeed = 0f;

        if (_boostActive)
        {
            _boostActive = false;
            agent.speed = _baseSpeed;
            agent.acceleration = _baseAccel;
            if (animator) animator.speed = _baseAnimatorSpeed;
        }
    }

    bool IsPointerOverUI(Vector2 pos)
    {
        var es = EventSystem.current;
        if (!es) return false;
        var ped = new PointerEventData(es) { position = pos };
        _uiHits.Clear();
        es.RaycastAll(ped, _uiHits);
        return _uiHits.Count > 0;
    }

    void OnDrawGizmos()
    {
        if (!drawGizmos || agent == null) return;
        float r = ReachRadius;
#if UNITY_EDITOR
        UnityEditor.Handles.color = gizmoReachColor;
        UnityEditor.Handles.DrawWireDisc(agent.transform.position, Vector3.up, r);
#endif
        Gizmos.color = gizmoReachColor * 0.5f;
        Gizmos.DrawWireSphere(agent.transform.position, r);

        if (_hasDebugClosest)
        {
            Gizmos.color = _lastWithinReach ? gizmoClosestPointColor : gizmoOutOfReachColor;
            Gizmos.DrawSphere(_debugClosestPoint, 0.06f);
            Gizmos.DrawLine(agent.transform.position, _debugClosestPoint);
        }
    }
}