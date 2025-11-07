using UnityEngine;
using System;

public class Draggable3D : MonoBehaviour
{
    [Header("When is drag allowed?")]
    public bool requireMicro = false;     // Micro에서만 허용할지
    public bool requireModifier = false;  // (선택) 보조키(Shift) 요구

    public enum PlaneMode { CameraFacing, WorldUp, Custom, ScreenDepth }

    [Header("Plane & Axis")]
    public PlaneMode planeMode = PlaneMode.CameraFacing;
    public Transform customPlane;         // planeMode=Custom일 때 기준(축: up 사용)
    public bool lockX, lockY, lockZ;     // 축락
    public Vector3 snapStep = Vector3.zero; // (0이면 스냅 없음)
    float _grabDepthZ;           // ScreenDepth용: 카메라-깊이 고정
    Vector3 _grabPointWorld;     // 처음 집은 지점(옵션)

    [Header("Bounds (optional)")]
    public Collider bounds;               // 경계 콜라이더(밖이면 가장 가까운 점으로 클램프)

    public event Action OnDragStarted;
    public event Action OnDragEnded;

    Camera _cam;
    Plane _dragPlane;
    Vector3 _grabOffsetWorld;
    bool _dragging;

    void Awake() { _cam = Camera.main; }

    public bool CanBeginDrag(Func<bool> inMicroCheck, Func<bool> modifierCheck)
    {
        if (requireMicro && (inMicroCheck == null || !inMicroCheck())) return false;
        if (requireModifier && (modifierCheck == null || !modifierCheck())) return false;
        return true;
    }

    public void BeginDrag(Ray pointerRay)
    {
        _dragging = true;

        if (planeMode == PlaneMode.WorldUp)
        {
            // 지면(XZ) 평면
            _dragPlane = new Plane(Vector3.up, transform.position);
        }
        else if (planeMode == PlaneMode.Custom && customPlane != null)
        {
            _dragPlane = new Plane(customPlane.up, customPlane.position);
        }
        else if (planeMode == PlaneMode.ScreenDepth)
        {
            // 현재 오브젝트의 깊이를 고정
            _grabPointWorld = transform.position;
            var sp = _cam.WorldToScreenPoint(_grabPointWorld);
            _grabDepthZ = sp.z;

            // 화면→월드 변환 지점에서의 오프셋 계산
            var mouse = UnityEngine.Input.mousePosition;
            var worldAtDepth = _cam.ScreenToWorldPoint(new Vector3(mouse.x, mouse.y, _grabDepthZ));
            _grabOffsetWorld = transform.position - worldAtDepth;
            OnDragStarted?.Invoke();
            return; // ScreenDepth는 평면 레이캐스트가 필요 없음
        }
        else
        {
            // CameraFacing (기본)
            _dragPlane = new Plane(_cam.transform.forward * -1f, transform.position);
        }

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
            // 현재 포인터의 화면 좌표를 같은 깊이로 월드 변환
            var mouse = UnityEngine.Input.mousePosition;
            var worldAtDepth = _cam.ScreenToWorldPoint(new Vector3(mouse.x, mouse.y, _grabDepthZ));
            target = worldAtDepth + _grabOffsetWorld;
        }
        else
        {
            if (!_dragPlane.Raycast(pointerRay, out float enter)) return;
            target = pointerRay.GetPoint(enter) + _grabOffsetWorld;
        }

        // 축락
        var cur = transform.position;
        if (lockX) target.x = cur.x;
        if (lockY) target.y = cur.y;
        if (lockZ) target.z = cur.z;

        // 스냅
        if (snapStep != Vector3.zero)
        {
            if (snapStep.x != 0) target.x = Mathf.Round(target.x / snapStep.x) * snapStep.x;
            if (snapStep.y != 0) target.y = Mathf.Round(target.y / snapStep.y) * snapStep.y;
            if (snapStep.z != 0) target.z = Mathf.Round(target.z / snapStep.z) * snapStep.z;
        }

        // 경계
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
