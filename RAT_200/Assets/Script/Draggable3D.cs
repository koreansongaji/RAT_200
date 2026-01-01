using UnityEngine;
using System;
using Random = UnityEngine.Random;

public class Draggable3D : MonoBehaviour
{
    [Header("When is drag allowed?")]
    public bool requireMicro = false;
    public bool requireModifier = false;

    public enum PlaneMode { CameraFacing, WorldUp, Custom, ScreenDepth }

    [Header("Plane & Axis")]
    public PlaneMode planeMode = PlaneMode.CameraFacing;
    public Transform customPlane;

    [Tooltip("체크하면 Custom 모드일 때 바닥(Pivot)이 아니라 내가 클릭한 높이(Surface)에 가상 평면을 만듭니다. (사다리 같은 키 큰 물체에 필수)")]
    public bool useDynamicPlaneHeight = true; // ★ 새로 추가된 옵션

    public bool lockX, lockY, lockZ;
    public Vector3 snapStep = Vector3.zero;

    float _grabDepthZ;
    Vector3 _grabPointWorld;

    [Header("Bounds (optional)")]
    public Collider bounds;

    public event Action OnDragStarted;
    public event Action OnDragEnded;

    Camera _cam;
    Plane _dragPlane;
    Vector3 _grabOffsetWorld;
    bool _dragging;
    Collider _myCollider; // ★ 내 콜라이더 캐싱

    [Header("Audio (optional)")]
    [SerializeField] AudioClip _slideSound;

    void Awake()
    {
        _cam = Camera.main;
        _myCollider = GetComponent<Collider>(); // ★ 콜라이더 가져오기
    }

    public bool CanBeginDrag(Func<bool> inMicroCheck, Func<bool> modifierCheck)
    {
        if (requireMicro && (inMicroCheck == null || !inMicroCheck())) return false;
        if (requireModifier && (modifierCheck == null || !modifierCheck())) return false;
        return true;
    }

    public void BeginDrag(Ray pointerRay)
    {
        if (_slideSound != null)
            AudioManager.Instance.Play(_slideSound, AudioManager.Sound.Effect, Random.Range(0.9f, 1.1f));

        _dragging = true;

        // 1. WorldUp (기본 바닥 평면)
        if (planeMode == PlaneMode.WorldUp)
        {
            _dragPlane = new Plane(Vector3.up, transform.position);
        }
        // 2. Custom (사용자 지정 평면 - 사다리 등)
        else if (planeMode == PlaneMode.Custom && customPlane != null)
        {
            Vector3 planePoint = customPlane.position;
            Vector3 planeNormal = customPlane.up;

            // ★ [핵심 수정] 키 큰 물체를 위해 클릭한 지점의 높이로 평면을 끌어올림
            if (useDynamicPlaneHeight && _myCollider != null)
            {
                if (_myCollider.Raycast(pointerRay, out RaycastHit hit, 100f))
                {
                    // 평면의 방향(Normal)은 그대로 두되, 위치만 클릭한 지점(hit.point)을 통과하도록 설정
                    planePoint = hit.point;
                }
            }

            _dragPlane = new Plane(planeNormal, planePoint);
        }
        // 3. ScreenDepth
        else if (planeMode == PlaneMode.ScreenDepth)
        {
            _grabPointWorld = transform.position;
            var sp = _cam.WorldToScreenPoint(_grabPointWorld);
            _grabDepthZ = sp.z;

            var mouse = UnityEngine.Input.mousePosition;
            var worldAtDepth = _cam.ScreenToWorldPoint(new Vector3(mouse.x, mouse.y, _grabDepthZ));
            _grabOffsetWorld = transform.position - worldAtDepth;
            OnDragStarted?.Invoke();
            return;
        }
        // 4. CameraFacing
        else
        {
            _dragPlane = new Plane(_cam.transform.forward * -1f, transform.position);
        }

        // 오프셋 계산 (이제 평면이 클릭 지점 높이에 있으므로 오차 없이 정확히 계산됨)
        if (_dragPlane.Raycast(pointerRay, out float enter))
        {
            var hitPoint = pointerRay.GetPoint(enter);
            _grabOffsetWorld = transform.position - hitPoint;
        }
        OnDragStarted?.Invoke();
    }


    public void DragUpdate(Ray pointerRay)
    {
        if (!_dragging) return;

        Vector3 target;

        if (planeMode == PlaneMode.ScreenDepth)
        {
            var mouse = UnityEngine.Input.mousePosition;
            var worldAtDepth = _cam.ScreenToWorldPoint(new Vector3(mouse.x, mouse.y, _grabDepthZ));
            target = worldAtDepth + _grabOffsetWorld;
        }
        else
        {
            if (!_dragPlane.Raycast(pointerRay, out float enter)) return;
            // 여기서 hitPoint는 "내가 잡은 높이"의 가상 평면 위 좌표임
            target = pointerRay.GetPoint(enter) + _grabOffsetWorld;
        }

        // --- 축 잠금 로직 (여기서 Y를 잠그므로 물체는 바닥에 붙어있게 됨) ---
        var cur = transform.position;
        if (lockX) target.x = cur.x;
        if (lockY) target.y = cur.y; // ★ 중요: 계산은 공중에서 했지만, 실제 이동은 Y축 고정
        if (lockZ) target.z = cur.z;

        // 스냅
        if (snapStep != Vector3.zero)
        {
            if (snapStep.x != 0) target.x = Mathf.Round(target.x / snapStep.x) * snapStep.x;
            if (snapStep.y != 0) target.y = Mathf.Round(target.y / snapStep.y) * snapStep.y;
            if (snapStep.z != 0) target.z = Mathf.Round(target.z / snapStep.z) * snapStep.z;
        }

        // 경계 제한
        if (bounds != null) target = bounds.ClosestPoint(target);

        transform.position = target;
    }


    public void EndDrag()
    {
        if (!_dragging) return;
        _dragging = false;
        OnDragEnded?.Invoke();
    }
}