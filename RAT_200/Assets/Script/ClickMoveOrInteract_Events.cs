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
    public PlayerReach reach;   // ★ 단일 소스: 플레이어 리치

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

    // --- Double-click config ---
    [Header("더블 클릭")]
    [SerializeField] float doubleClickTime = 0.25f;        // 더블클릭 허용 시간
    [SerializeField] float doubleClickMaxScreenDist = 12f; // 두 클릭 간 화면 거리 허용(px)
    [SerializeField] float speedBoostMultiplier = 1.8f;    // 달리기 배수 (예: 1.8x)
    [SerializeField] float accelBoostMultiplier = 1.5f;    // 가속도 배수 (선택)

    // ===== Drag state =====
    [SerializeField] float dragThresholdPixels = 6f;
    [SerializeField] LayerMask draggableMask = ~0; // 필요시 Draggable 전용 레이어로 좁혀도 OK

    Draggable3D _dragCandidate;
    Draggable3D _activeDrag;
    Vector2 _pressPos;
    bool _pointerDown;

    float _lastClickTime;
    Vector2 _lastClickScreenPos;
    bool _boostActive;
    float _baseSpeed, _baseAccel;
    float _baseAnimatorSpeed = 1f;           // Animator 있을 경우
    [Header("애니메이션")]
    [SerializeField] Animator animator;      // (있다면 참조)

    private NavMeshPath _path;

    // 클릭 큐(기존 흐름 유지)
    private bool _queuedClick;
    private Vector2 _queuedScreenPos;

    // 디버깅 표시용
    private Vector3 _debugClosestPoint;
    private bool _hasDebugClosest;
    private bool _lastWithinReach;

    static readonly List<RaycastResult> _uiHits = new();

    float ReachRadius => reach ? Mathf.Max(0f, reach.radius) : 1.6f;
    bool HorizontalOnly => reach ? reach.horizontalOnly : false;

    // ====== NEW: Draggable 헬퍼 ======
    Draggable3D RaycastForDraggable(Vector2 screenPos)
    {
        var ray = cam.ScreenPointToRay(screenPos);
        if (Physics.Raycast(ray, out var hit, maxRayDistance, draggableMask))
            return hit.collider.GetComponentInParent<Draggable3D>();
        return null;
    }

    bool InMicro() => CloseupCamManager.InMicro; // Micro 여부(이동 금지 판단) 
    bool ModKey() => Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

    void Awake() { _path = new NavMeshPath(); }
    void Start()
    {
        _baseSpeed = agent.speed;
        _baseAccel = agent.acceleration;
        if (animator) _baseAnimatorSpeed = animator.speed;
    }

    // ====== 기존 LeftClick 액션 (performed 시 1회) ======
    public void OnClick(InputAction.CallbackContext ctx)
    {
        //if (!ctx.performed) return;

        //// 드래그 중이면 클릭 큐 무시
        //if (_activeDrag != null) return;

        //var mouse = Mouse.current;
        //_queuedScreenPos = mouse != null ? mouse.position.ReadValue() : (Vector2)Input.mousePosition;
        //_queuedClick = true;
    }

    // ====== NEW: Pointer Down / Drag / Up 액션 ======
    public void OnPointerDown(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed) return;
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
        if (!ctx.performed && !ctx.canceled) return; // 값 변경 시점
        if (!_pointerDown) return;

        var pos = Mouse.current != null ? Mouse.current.position.ReadValue() : (Vector2)Input.mousePosition;

        // 이미 드래그 중이면 매 프레임 갱신
        if (_activeDrag != null)
        {
            _activeDrag.DragUpdate(cam.ScreenPointToRay(pos));
            return; // 이동/인터랙트 차단
        }

        // 후보가 있고 임계치 이상 움직였으면 드래그 발화 시도
        if (_dragCandidate != null)
        {
            if ((pos - _pressPos).magnitude >= dragThresholdPixels)
            {
                if (_dragCandidate.CanBeginDrag(InMicro, ModKey))
                {
                    // 드래그 시작: 이동/부스트를 안전하게 끊는다
                    HardStop();
                    ActivateBoostIfNeeded(false, pos);

                    _activeDrag = _dragCandidate;
                    _activeDrag.BeginDrag(cam.ScreenPointToRay(pos));
                }
                else
                {
                    _dragCandidate = null; // 조건 불가 → 일반 클릭으로
                }
            }
        }
    }

    public void OnPointerUp(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed) return;

        var pos = Mouse.current != null ? Mouse.current.position.ReadValue() : (Vector2)Input.mousePosition;
        _pointerDown = false;

        // 드래그 중이었으면 소비하고 종료
        if (_activeDrag != null)
        {
            _activeDrag.EndDrag();
            _activeDrag = null;
            _dragCandidate = null;
            return;
        }

        // 드래그 발화가 없었음 → 기존 클릭 큐로 전달(스크린 좌표)
        _queuedScreenPos = pos;
        _queuedClick = true;
    }

    void Update()
    {
        // 1) 이동 도착 체크
        if (!agent.pathPending && agent.hasPath && Arrived())
            HardStop();

        // 2) 드래그 활성 시(안전장치): 입력 이벤트 연결이 없더라도 프레임마다 추적
        if (_activeDrag != null)
        {
            var pos = Mouse.current != null ? Mouse.current.position.ReadValue() : (Vector2)Input.mousePosition;
            _activeDrag.DragUpdate(cam.ScreenPointToRay(pos));
            return; // 드래그 프레임엔 이동/인터랙트 처리 금지
        }

        // 3) 클릭 큐 처리 (기존 흐름 유지)
        if (!_queuedClick) return;
        _queuedClick = false;

        if (IsPointerOverUI(_queuedScreenPos)) return;

        Ray ray = cam.ScreenPointToRay(_queuedScreenPos);

        // Interactable 우선
        if (Physics.Raycast(ray, out var hitI, maxRayDistance, interactableMask))
        {
            var target = hitI.collider.GetComponentInParent<IInteractable>();
            if (target != null)
            {
                bool inMicro = InMicro();
                float effectiveReach = inMicro ? float.PositiveInfinity : ReachRadius;

                Vector3 closest = GetClosestPointOnTarget(target, agent.transform.position, out _);
                _hasDebugClosest = true;
                _debugClosestPoint = closest;

                bool within = IsWithinReach(agent.transform.position, closest, effectiveReach);
                _lastWithinReach = within;

                if (within && target.CanInteract(player))
                {
                    HardStop();
                    ActivateBoostIfNeeded(false, _queuedScreenPos);
                    target.Interact(player);   // 리치 안이면 제자리 상호작용
                    return;
                }

                // Micro에선 이동 금지. Room 모드에서만 접근 이동/그라운드 이동 허용
                if (!inMicro)
                {
                    if (TryApproachTarget(target, closest, effectiveReach)) return;
                    if (TryMoveToGroundUnderRay(ray)) return;
                }

                // 마지막 폴백: 타깃 근방 보정
                Vector3 approx = target.AsTransform().position;
                if (NavMesh.SamplePosition(approx, out var navHit2, sampleMaxDistance, NavMesh.AllAreas))
                    TrySetPath(navHit2.position);
                return;
            }
        }

        // Interactable이 아니면 → 일반 이동(Ground)
        ActivateBoostIfNeeded(true, _queuedScreenPos);
        TryMoveToGroundUnderRay(ray);
    }

    // ===== Reach / Approach =====
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
        // ‘도착’ 허용 오차: 에이전트 stoppingDistance + 소량
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
            // 단일 클릭 이동이면 기본 속도로 복원
            _boostActive = false;
            agent.speed = _baseSpeed;
            agent.acceleration = _baseAccel;
            if (animator) animator.speed = _baseAnimatorSpeed;
        }
    }

    // ===== Movement helpers =====
    bool TryMoveToGroundUnderRay(Ray ray)
    {
        if (Physics.Raycast(ray, out var hitG, maxRayDistance, groundMask))
            if (NavMesh.SamplePosition(hitG.point, out var navHit, sampleMaxDistance, NavMesh.AllAreas))
                return TrySetPath(navHit.position);
        return false;
    }

    bool TrySetPath(Vector3 dst)
    {
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
        // 경로를 먼저 끊고
        if (agent.hasPath) agent.ResetPath();
        // 그 다음 이동/회전 정지
        agent.isStopped = true;
        agent.velocity = Vector3.zero;
        agent.angularSpeed = 0f; // 선택: 정지 프레임에서 회전 미끄럼 방지

        // 부스트 해제 & 기본 속도/애니 복원
        if (_boostActive)
        {
            _boostActive = false;
            agent.speed = _baseSpeed;
            agent.acceleration = _baseAccel;
            if (animator) animator.speed = _baseAnimatorSpeed;
        }
    }

    // ===== UI Raycast =====
    bool IsPointerOverUI(Vector2 pos)
    {
        var es = EventSystem.current;
        if (!es) return false;
        var ped = new PointerEventData(es) { position = pos };
        _uiHits.Clear();
        es.RaycastAll(ped, _uiHits); // 최신 프레임 기준
        return _uiHits.Count > 0;
    }

    // ===== Gizmos =====
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
